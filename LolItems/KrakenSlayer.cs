using System.Collections.Generic;
using BepInEx;
using R2API;
using R2API.Utils;
using RoR2.Orbs;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine.Networking;

namespace LoLItems
{
    internal class KrakenSlayer
    {

        //We need our item definition to persist through our functions, and therefore make it a class field.
        public static ItemDef myItemDef;
        public static BuffDef myCounterBuffDef;

        public static ConfigEntry<int> procRequirement { get; set; }
        public static ConfigEntry<float> procDamage { get; set; }
        public static ConfigEntry<bool> enabled { get; set; }
        public static ConfigEntry<string> rarity { get; set; }
        public static ConfigEntry<string> voidItems { get; set; }
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> bonusDamage = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static string bonusDamageToken = "KrakenSlayer.bonusDamage";
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = new Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster>();
        public static Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef = new Dictionary<RoR2.UI.ItemIcon, CharacterMaster>();

        internal static void Init()
        {
            LoadConfig();
            if (!enabled.Value)
            {
                return;
            }

            CreateItem();
            CreateBuff();
            AddTokens();
            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            ContentAddition.AddBuffDef(myCounterBuffDef);
            hooks();
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, rarity, voidItems, "KrakenSlayer");
            SetupNetworkMappings();
        }

        private static void LoadConfig()
        {
            enabled = LoLItems.MyConfig.Bind<bool>(
                "KrakenSlayer",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            rarity = LoLItems.MyConfig.Bind<string>(
                "KrakenSlayer",
                "Rarity",
                "Tier2Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            voidItems = LoLItems.MyConfig.Bind<string>(
                "KrakenSlayer",
                "Void Items",
                "",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            procRequirement = LoLItems.MyConfig.Bind<int>(
                "KrakenSlayer",
                "On Hit Proc Requirement",
                3,
                "Amount of hits required to proc the on hit damage."

            );

            procDamage = LoLItems.MyConfig.Bind<float>(
                "KrakenSlayer",
                "Base Damage Percent",
                20f,
                "Amount of additional percent base damage each item will grant."

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
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(Utilities.GetRarityFromString(rarity.Value)).WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("KrakenSlayerIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("KrakenSlayerPrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = new ItemTag[1] { ItemTag.Damage };
        }

        private static void CreateBuff()
        {
            // Create a buff to count the number of stacks before a big proc
            myCounterBuffDef = ScriptableObject.CreateInstance<BuffDef>();

            myCounterBuffDef.iconSprite = Assets.icons.LoadAsset<Sprite>("KrakenSlayerIcon");
            myCounterBuffDef.name = "KrakenSlayerCounterBuff";
            myCounterBuffDef.canStack = true;
            myCounterBuffDef.isDebuff = false;
            myCounterBuffDef.isCooldown = false;
            myCounterBuffDef.isHidden = false;
        }


        private static void hooks()
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

                            if (attackerCharacterBody.healthComponent.body.GetBuffCount(myCounterBuffDef) > procRequirement.Value)
                            {
                                if (NetworkServer.active)
                                {
                                    foreach (int value in Enumerable.Range(2, procRequirement.Value))
                                        attackerCharacterBody.RemoveBuff(myCounterBuffDef);
                                }

                                float damage = attackerCharacterBody.damage * procDamage.Value / 100f * inventoryCount;
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
            {
                characterBody.AddBuff(myCounterBuffDef);
            }
            else if (count == 0 && characterBody.HasBuff(myCounterBuffDef))
            {
                Utilities.RemoveBuffStacks(characterBody, myCounterBuffDef.buffIndex);
            }
        }

        private static (string, string) GetDisplayInformation(CharacterMaster masterRef)
        {
            if (masterRef == null)
                return (Language.GetString(myItemDef.descriptionToken), "");
            
            string customDescription = "";

            if (bonusDamage.TryGetValue(masterRef.netId, out float damageDealt))
                customDescription += "<br><br>Damage dealt: " + String.Format("{0:#}", damageDealt);
            else
                customDescription += "<br><br>Damage dealt: 0";

            return (Language.GetString(myItemDef.descriptionToken), customDescription);
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            //The Name should be self explanatory
            LanguageAPI.Add("KrakenSlayer", "KrakenSlayer");

            //The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("KrakenSlayerItem", "Every " + procRequirement.Value + " hits do bonus damage.");

            //The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("KrakenSlayerDesc", "Every " + procRequirement.Value + " hits do an extra <style=cIsDamage>" + procDamage.Value + "%</style> <style=cStack>(+" + procDamage.Value + "%)</style> base damage.");

            //The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("KrakenSlayerLore", "Legend has it that this item is no longer mythical.");
        }

        public static void SetupNetworkMappings()
        {
            LoLItems.networkMappings.Add(bonusDamageToken, bonusDamage);
        }
    }
}