using System.Threading;
using System.Globalization;
using System.Collections.Generic;
using BepInEx;
using R2API;
using R2API.Utils;
using RoR2.Orbs;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System;
using BepInEx.Configuration;

namespace LoLItems
{
    internal class MejaisSoulstealer
    {
        public static ItemDef myItemDef;
        public static BuffDef currentStacks;
        public static BuffDef currentDuration;

        public static ConfigEntry<float> bonusDamagePercent { get; set; }
        public static ConfigEntry<int> maxStacks { get; set; }
        public static ConfigEntry<float> duration { get; set; }
        public static ConfigEntry<bool> enabled { get; set; }
        public static ConfigEntry<string> rarity { get; set; }
        public static ConfigEntry<string> voidItems { get; set; }
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> bonusDamageDealt = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = new Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster>();
        public static Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef = new Dictionary<RoR2.UI.ItemIcon, CharacterMaster>();

        // This runs when loading the file
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
            ContentAddition.AddBuffDef(currentStacks);
            ContentAddition.AddBuffDef(currentDuration);
            hooks();
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, rarity, voidItems, "MejaisSoulstealer");
        }

        private static void LoadConfig()
        {
            enabled = LoLItems.MyConfig.Bind<bool>(
                "MejaisSoulstealer",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            rarity = LoLItems.MyConfig.Bind<string>(
                "MejaisSoulstealer",
                "Rarity",
                "Tier1Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            voidItems = LoLItems.MyConfig.Bind<string>(
                "MejaisSoulstealer",
                "Void Items",
                "",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            bonusDamagePercent = LoLItems.MyConfig.Bind<float>(
                "MejaisSoulstealer",
                "Bonus Damage Per Stack",
                0.5f,
                "Amount of bonus damage each stack will grant."

            );

            maxStacks = LoLItems.MyConfig.Bind<int>(
                "MejaisSoulstealer",
                "Max Stacks",
                25,
                "Maximum amount of stacks for the buff."

            );

            duration = LoLItems.MyConfig.Bind<float>(
                "MejaisSoulstealer",
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
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(Utilities.GetRarityFromString(rarity.Value)).WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("MejaisSoulstealerIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("MejaisSoulstealerPrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
        }

        private static void CreateBuff()
        {
            currentStacks = ScriptableObject.CreateInstance<BuffDef>();
            currentStacks.iconSprite = Assets.icons.LoadAsset<Sprite>("MejaisSoulstealerIcon");
            currentStacks.name = "MejaisSoulstealerBuff";
            currentStacks.canStack = true;
            currentStacks.isDebuff = false;
            currentStacks.isCooldown = false;
            currentStacks.isHidden = false;

            currentDuration = ScriptableObject.CreateInstance<BuffDef>();
            currentDuration.name = "MejaisSoulstealerBuffDuration";
            currentDuration.canStack = false;
            currentDuration.isDebuff = false;
            currentDuration.isCooldown = true;
            currentDuration.isHidden = true;
        }


        private static void hooks()
        {
            // Do something on character death
            On.RoR2.GlobalEventManager.OnCharacterDeath += (orig, globalEventManager, damageReport) =>
            {
                orig(globalEventManager, damageReport);

                if (damageReport.attackerMaster?.inventory != null)
                {

                    int inventoryCount = damageReport.attackerMaster.inventory.GetItemCount(myItemDef.itemIndex);
					if (inventoryCount > 0)
					{
                        damageReport.attackerBody.AddTimedBuff(currentDuration, duration.Value * inventoryCount);
                        if (damageReport.attackerBody.GetBuffCount(currentStacks) < maxStacks.Value)
                        {
                            damageReport.attackerBody.AddBuff(currentStacks);
                        }
					}
                }
            };

            // When something takes damage
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
                            float extraDamage = damageInfo.damage * (buffCount * bonusDamagePercent.Value) / 100f;
                            damageInfo.damage += extraDamage;
                            Utilities.AddValueInDictionary(ref bonusDamageDealt, attackerCharacterBody.master, extraDamage);
                        }
                    }
                }
                orig(self, damageInfo);
            };

            // Modify character values
            On.RoR2.CharacterBody.RecalculateStats += (orig, self) =>
            {
                if (self?.inventory && self.GetBuffCount(currentStacks) > 0 && self.GetBuffCount(currentDuration) == 0)
                {
                    while (self.GetBuffCount(currentStacks.buffIndex) > 0)
                    {
                        self.RemoveBuff(currentStacks);
                    }
                }
                orig(self);
            };
        }

        private static string GetDisplayInformation(CharacterMaster masterRef)
        {
            // Update the description for an item in the HUD
            if (masterRef != null && bonusDamageDealt.TryGetValue(masterRef.netId, out float damageDealt)){
                return Language.GetString(myItemDef.descriptionToken) + "<br><br>Damage dealt: " + String.Format("{0:#}", damageDealt);
            }
            return Language.GetString(myItemDef.descriptionToken);
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("MejaisSoulstealer", "MejaisSoulstealer");

            // Short description
            LanguageAPI.Add("MejaisSoulstealerItem", "Killing enemies grants more damage for a short time");

            // Long description
            LanguageAPI.Add("MejaisSoulstealerDesc", "Killing an enemy grants a stack which gives <style=cIsDamage>" + bonusDamagePercent.Value + 
            "%</style> bonus damage. Max <style=cIsUtility>" + maxStacks.Value + 
            "</style> stacks, buff lasts for <style=cIsUtility>" + duration.Value + "</style> <style=cStack>(+" + duration.Value + ")</style> seconds.");

            // Lore
            LanguageAPI.Add("MejaisSoulstealerLore", "Your death note.");

            LanguageAPI.Add("MejaisSoulstealerBuff", "Mejais stacks");
            LanguageAPI.Add("MejaisSoulstealerBuffDuration", "Mejais duration remaining");
        }
    }
}