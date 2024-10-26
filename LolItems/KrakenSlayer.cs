using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using System.Linq;
using BepInEx.Configuration;

namespace LoLItems
{
    internal class KrakenSlayer
    {

        //We need our item definition to persist through our functions, and therefore make it a class field.
        public static ItemDef myItemDef;
        public static BuffDef myCounterBuffDef;

        public static ConfigEntry<int> ProcRequirement { get; set; }
        public static ConfigEntry<float> ProcDamage { get; set; }
        public static ConfigEntry<bool> Enabled { get; set; }
        public static ConfigEntry<string> Rarity { get; set; }
        public static ConfigEntry<string> VoidItems { get; set; }
        public static ConfigEntry<string> DamageScalingType { get; set; }
        public static Dictionary<NetworkInstanceId, float> bonusDamage = [];
        public static string bonusDamageToken = "KrakenSlayer.bonusDamage";
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = [];
        public static Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef = [];

        internal static void Init()
        {
            LoadConfig();
            if (!Enabled.Value)
            {
                return;
            }

            CreateItem();
            CreateBuff();
            AddTokens();
            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            ContentAddition.AddBuffDef(myCounterBuffDef);
            Hooks();
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, Rarity, VoidItems, "KrakenSlayer");
            SetupNetworkMappings();
        }

        private static void LoadConfig()
        {
            Enabled = LoLItems.MyConfig.Bind(
                "Kraken Slayer",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            Rarity = LoLItems.MyConfig.Bind(
                "Kraken Slayer",
                "Rarity",
                "Tier2Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            VoidItems = LoLItems.MyConfig.Bind(
                "Kraken Slayer",
                "Void Items",
                "",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            ProcRequirement = LoLItems.MyConfig.Bind(
                "Kraken Slayer",
                "On Hit Proc Requirement",
                3,
                "Amount of hits required to proc the on hit damage."
            );

            ProcDamage = LoLItems.MyConfig.Bind(
                "Kraken Slayer",
                "Base Damage Percent",
                20f,
                "Amount of additional percent base damage each item will grant."
            );

            DamageScalingType = LoLItems.MyConfig.Bind(
                "Kraken Slayer",
                "Damage Scaling Type",
                "base",
                "If the item will scale with base or total damage."
            );
        }

        private static void CreateItem()
        {
            //First let's define our item
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();

            // Language Tokens, check AddTokens() below.
            myItemDef.name = "KrakenSlayer";
            myItemDef.nameToken = "KrakenSlayer";
            myItemDef.pickupToken = "KrakenSlayerItem";
            myItemDef.descriptionToken = "KrakenSlayerDesc";
            myItemDef.loreToken = "KrakenSlayerLore";
#pragma warning disable Publicizer001
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(Utilities.GetRarityFromString(Rarity.Value)).WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = MyAssets.icons.LoadAsset<Sprite>("KrakenSlayerIcon");
            myItemDef.pickupModelPrefab = MyAssets.prefabs.LoadAsset<GameObject>("KrakenSlayerPrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = [ ItemTag.Damage ];
        }

        private static void CreateBuff()
        {
            // Create a buff to count the number of stacks before a big proc
            myCounterBuffDef = ScriptableObject.CreateInstance<BuffDef>();

            myCounterBuffDef.iconSprite = MyAssets.icons.LoadAsset<Sprite>("KrakenSlayerIcon");
            myCounterBuffDef.name = "Kraken Slayer Counter";
            myCounterBuffDef.canStack = true;
            myCounterBuffDef.isDebuff = false;
            myCounterBuffDef.isCooldown = false;
            myCounterBuffDef.isHidden = false;
        }


        private static void Hooks()
        {
            On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, victim) =>
            {
                orig(self, damageInfo, victim);

                if (!NetworkServer.active)
                    return;
                
                if (damageInfo.attacker)
                {
                    CharacterBody attackerCharacterBody = damageInfo.attacker.GetComponent<CharacterBody>();
                    CharacterBody victimCharacterBody = victim.GetComponent<CharacterBody>();
                    
                    if (attackerCharacterBody?.inventory)
                    {
                        int inventoryCount = attackerCharacterBody.inventory.GetItemCount(myItemDef.itemIndex);
                        if (inventoryCount > 0 && damageInfo.procCoefficient > 0)
                        {
                            if (NetworkServer.active)
                                attackerCharacterBody.AddBuff(myCounterBuffDef);

                            if (attackerCharacterBody.healthComponent.body.GetBuffCount(myCounterBuffDef) > ProcRequirement.Value)
                            {
                                if (NetworkServer.active)
                                {
                                    foreach (int value in Enumerable.Range(2, ProcRequirement.Value))
                                    {
                                        attackerCharacterBody.RemoveBuff(myCounterBuffDef);
                                    }
                                }

                                float damage = ProcDamage.Value / 100f * inventoryCount;
                                if (DamageScalingType.Value == "total") {
                                    damage = damage * damageInfo.damage;
                                }
                                else {
                                    damage = damage * attackerCharacterBody.damage;
                                }
                                DamageInfo onHitProc = damageInfo;
                                onHitProc.crit = false;
                                onHitProc.procCoefficient = 0f;
                                onHitProc.damageType = DamageType.Generic;
                                onHitProc.inflictor = damageInfo.attacker;
                                onHitProc.damage = damage;
                                onHitProc.damageColorIndex = DamageColorIndex.Item;
                                victimCharacterBody.healthComponent.TakeDamage(onHitProc);
                                Utilities.AddValueInDictionary(ref bonusDamage, attackerCharacterBody.master, damage, bonusDamageToken);
                            }
                        }
                    }
                }
            };

            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        private static void RecalculateStatsAPI_GetStatCoefficients(CharacterBody characterBody, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (!NetworkServer.active)
                return;

            int count = characterBody?.inventory?.GetItemCount(myItemDef.itemIndex) ?? 0;
            if (count > 0 && !characterBody.HasBuff(myCounterBuffDef))
                characterBody.AddBuff(myCounterBuffDef);
            else if (count == 0 && characterBody.HasBuff(myCounterBuffDef))
                Utilities.RemoveBuffStacks(characterBody, myCounterBuffDef.buffIndex);
        }

        private static (string, string) GetDisplayInformation(CharacterMaster masterRef)
        {
            if (masterRef == null)
                return (Language.GetString(myItemDef.descriptionToken), "");
            
            string customDescription = "";

            if (bonusDamage.TryGetValue(masterRef.netId, out float damageDealt))
                customDescription += "<br><br>Damage dealt: " + string.Format("{0:#, ##0.##}", damageDealt);
            else
                customDescription += "<br><br>Damage dealt: 0";

            return (Language.GetString(myItemDef.descriptionToken), customDescription);
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("KrakenSlayer", "Kraken Slayer");

            // Short description
            LanguageAPI.Add("KrakenSlayerItem", "Every " + ProcRequirement.Value + " hits do bonus damage.");

            // Long description
            LanguageAPI.Add("KrakenSlayerDesc", "Every " + ProcRequirement.Value + " hits do an extra <style=cIsDamage>" + ProcDamage.Value + "%</style> <style=cStack>(+" + ProcDamage.Value + "%)</style> base damage.");

            // Lore
            LanguageAPI.Add("KrakenSlayerLore", "Legend has it that this item is no longer mythical.");
        }

        public static void SetupNetworkMappings()
        {
            LoLItems.networkMappings.Add(bonusDamageToken, bonusDamage);
        }
    }
}