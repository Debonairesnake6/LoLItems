using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using BepInEx.Configuration;

namespace LoLItems
{
    internal class MejaisSoulstealer
    {
        public static ItemDef myItemDef;
        public static BuffDef currentStacks;
        public static BuffDef currentDuration;

        public static ConfigEntry<float> BonusDamagePercent { get; set; }
        public static ConfigEntry<int> MaxStacks { get; set; }
        public static ConfigEntry<float> Duration { get; set; }
        public static ConfigEntry<bool> Enabled { get; set; }
        public static ConfigEntry<string> Rarity { get; set; }
        public static ConfigEntry<string> VoidItems { get; set; }
        public static Dictionary<NetworkInstanceId, float> bonusDamageDealt = [];
        public static string bonusDamageDealtToken = "MejaisSoulstealer.bonusDamageDealt";
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = [];
        public static Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef = [];

        // This runs when loading the file
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
            ContentAddition.AddBuffDef(currentStacks);
            ContentAddition.AddBuffDef(currentDuration);
            Hooks();
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, Rarity, VoidItems, "MejaisSoulstealer");
            SetupNetworkMappings();
        }

        private static void LoadConfig()
        {
            Enabled = LoLItems.MyConfig.Bind(
                "Mejais Soulstealer",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            Rarity = LoLItems.MyConfig.Bind(
                "Mejais Soulstealer",
                "Rarity",
                "Tier1Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            VoidItems = LoLItems.MyConfig.Bind(
                "Mejais Soulstealer",
                "Void Items",
                "",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            BonusDamagePercent = LoLItems.MyConfig.Bind(
                "Mejais Soulstealer",
                "Bonus Damage Per Stack",
                0.5f,
                "Amount of bonus damage each stack will grant."
            );

            MaxStacks = LoLItems.MyConfig.Bind(
                "Mejais Soulstealer",
                "Max Stacks",
                25,
                "Maximum amount of stacks for the buff."
            );

            Duration = LoLItems.MyConfig.Bind(
                "Mejais Soulstealer",
                "Duration",
                10f,
                "Duration of the buff."
            );
        }

        private static void CreateItem()
        {
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();
            myItemDef.name = "MejaisSoulstealer";
            myItemDef.nameToken = "MejaisSoulstealer";
            myItemDef.pickupToken = "MejaisSoulstealerItem";
            myItemDef.descriptionToken = "MejaisSoulstealerDesc";
            myItemDef.loreToken = "MejaisSoulstealerLore";
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(Utilities.GetRarityFromString(Rarity.Value)).WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("MejaisSoulstealerIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("MejaisSoulstealerPrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = [ ItemTag.Damage, ItemTag.OnKillEffect ];
        }

        private static void CreateBuff()
        {
            currentStacks = ScriptableObject.CreateInstance<BuffDef>();
            currentStacks.iconSprite = Assets.icons.LoadAsset<Sprite>("MejaisSoulstealerIcon");
            currentStacks.name = "Mejai\'s Soulstealer Buff";
            currentStacks.canStack = true;
            currentStacks.isDebuff = false;
            currentStacks.isCooldown = false;
            currentStacks.isHidden = false;

            currentDuration = ScriptableObject.CreateInstance<BuffDef>();
            currentDuration.name = "Mejai\'s Soulstealer Buff Duration";
            currentDuration.canStack = false;
            currentDuration.isDebuff = false;
            currentDuration.isCooldown = true;
            currentDuration.isHidden = true;
        }


        private static void Hooks()
        {
            // Do something on character death
            On.RoR2.GlobalEventManager.OnCharacterDeath += (orig, globalEventManager, damageReport) =>
            {
                orig(globalEventManager, damageReport);

                if (!NetworkServer.active)
                    return;

                if (damageReport.attackerMaster?.inventory != null)
                {
                    int inventoryCount = damageReport.attackerMaster.inventory.GetItemCount(myItemDef.itemIndex);
					if (inventoryCount > 0)
					{
                        damageReport.attackerBody.AddTimedBuff(currentDuration, Duration.Value * inventoryCount);
                        if (damageReport.attackerBody.GetBuffCount(currentStacks) < MaxStacks.Value)
                            damageReport.attackerBody.AddBuff(currentStacks);
					}
                }
            };

            On.RoR2.HealthComponent.TakeDamage += (orig, self, damageInfo) =>
            {
                if (damageInfo.attacker)
                {
                    CharacterBody attackerCharacterBody = damageInfo.attacker.GetComponent<CharacterBody>();
                    
                    if (attackerCharacterBody?.inventory)
                    {
                        int buffCount = attackerCharacterBody.GetBuffCount(currentStacks.buffIndex);
                        if (buffCount > 0)
                        {
                            float extraDamage = damageInfo.damage * (buffCount * BonusDamagePercent.Value) / 100f;
                            damageInfo.damage += extraDamage;
                            Utilities.AddValueInDictionary(ref bonusDamageDealt, attackerCharacterBody.master, extraDamage, bonusDamageDealtToken);
                        }
                    }
                }
                orig(self, damageInfo);
            };

            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        private static void RecalculateStatsAPI_GetStatCoefficients(CharacterBody characterBody, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (characterBody.GetBuffCount(currentStacks) > 0 && characterBody.GetBuffCount(currentDuration) == 0)
                Utilities.RemoveBuffStacks(characterBody, currentStacks.buffIndex);
        }

        private static (string, string) GetDisplayInformation(CharacterMaster masterRef)
        {
            if (masterRef == null)
                return (Language.GetString(myItemDef.descriptionToken), "");
            
            string customDescription = "";

            if (bonusDamageDealt.TryGetValue(masterRef.netId, out float damageDealt))
                customDescription += "<br><br>Bonus damage dealt: " + string.Format("{0:#}", damageDealt);
            else
                customDescription += "<br><br>Bonus damage dealt: 0";

            return (Language.GetString(myItemDef.descriptionToken), customDescription);
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("MejaisSoulstealer", "Mejai\'s Soulstealer");

            // Short description
            LanguageAPI.Add("MejaisSoulstealerItem", "Killing enemies grants more damage for a short time.");

            // Long description
            LanguageAPI.Add("MejaisSoulstealerDesc", "Killing an enemy grants a stack which gives <style=cIsDamage>" + BonusDamagePercent.Value + 
            "%</style> bonus damage. Max <style=cIsUtility>" + MaxStacks.Value + 
            "</style> stacks, buff lasts for <style=cIsUtility>" + Duration.Value + "</style> <style=cStack>(+" + Duration.Value + ")</style> seconds.");

            // Lore
            LanguageAPI.Add("MejaisSoulstealerLore", "Your death note.");

            LanguageAPI.Add("MejaisSoulstealerBuff", "Mejai\'s Soulstealer stacks.");
            LanguageAPI.Add("MejaisSoulstealerBuffDuration", "Mejai\'s Soulstealer duration remaining.");
        }

        public static void SetupNetworkMappings()
        {
            LoLItems.networkMappings.Add(bonusDamageDealtToken, bonusDamageDealt);
        }
    }
}