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
    internal class BannerOfCommand
    {
        public static ItemDef myItemDef;

        public static ConfigEntry<float> damagePercentAmp { get; set; }
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
            AddTokens();
            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            hooks();
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, rarity, voidItems, "BannerOfCommand");
        }

        private static void LoadConfig()
        {
            enabled = LoLItems.MyConfig.Bind<bool>(
                "BannerOfCommand",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            rarity = LoLItems.MyConfig.Bind<string>(
                "BannerOfCommand",
                "Rarity",
                "Tier1Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            voidItems = LoLItems.MyConfig.Bind<string>(
                "BannerOfCommand",
                "Void Items",
                "",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            damagePercentAmp = LoLItems.MyConfig.Bind<float>(
                "BannerOfCommand",
                "Damage Amp",
                10f,
                "Amount of damage amp each stack will grant."

            );
        }

        private static void CreateItem()
        {
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();
            myItemDef.name = "BannerOfCommand";
            myItemDef.nameToken = "BannerOfCommand";
            myItemDef.pickupToken = "BannerOfCommandItem";
            myItemDef.descriptionToken = "BannerOfCommandDesc";
            myItemDef.loreToken = "BannerOfCommandLore";
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(Utilities.GetRarityFromString(rarity.Value)).WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("BannerOfCommandIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("BannerOfCommandPrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = new ItemTag[1] { ItemTag.Damage };
        }


        private static void hooks()
        {
            // When something takes damage
            On.RoR2.HealthComponent.TakeDamage += (orig, self, damageInfo) =>
            {
                if (damageInfo.attacker)
                {
                    CharacterBody attackerCharacterBody = damageInfo.attacker.GetComponent<CharacterBody>();
                    CharacterMaster ownerMaster = attackerCharacterBody?.master?.minionOwnership?.ownerMaster;
                    
                    if (ownerMaster?.inventory)
                    {
                        int inventoryCount = ownerMaster.inventory.GetItemCount(myItemDef.itemIndex);
                        if (inventoryCount > 0)
                        {
                            float extraDamage = 1 + (inventoryCount * damagePercentAmp.Value / 100);
                            Utilities.AddValueInDictionary(ref bonusDamageDealt, ownerMaster, extraDamage * damageInfo.damage);
                            damageInfo.damage *= extraDamage;
                        }
                    }
                }
                orig(self, damageInfo);
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

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opinionated. Make your own judgments!
        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("BannerOfCommand", "BannerOfCommand");

            // Short description
            LanguageAPI.Add("BannerOfCommandItem", "Increase allied minion damage");

            // Long description
            LanguageAPI.Add("BannerOfCommandDesc", "Increase the damage of allied minions by <style=cIsUtility>" + damagePercentAmp.Value + "%</style> <style=cStack>(+" + damagePercentAmp.Value + "%)</style>");

            // Lore
            LanguageAPI.Add("BannerOfCommandLore", "Split pushing is boring.");
        }
    }
}