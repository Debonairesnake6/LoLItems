using System.IO;
using System.Text.RegularExpressions;
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
using System.Linq;
using BepInEx.Configuration;

namespace LoLItems
{
    internal class GuardiansBlade
    {
        public static ItemDef myItemDef;
        public static ConfigEntry<float> cooldownReduction { get; set; }
        public static ConfigEntry<bool> enabled { get; set; }
        public static ConfigEntry<string> rarity { get; set; }
        public static ConfigEntry<string> voidItems { get; set; }
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> cooldownReductionTracker = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static string cooldownReductionTrackerToken = "GuardiansBlade.cooldownReductionTracker";
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
            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            hooks();
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, rarity, voidItems, "GuardiansBlade");
            SetupNetworkMappings();
        }

        private static void LoadConfig()
        {
            enabled = LoLItems.MyConfig.Bind<bool>(
                "GuardiansBlade",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            rarity = LoLItems.MyConfig.Bind<string>(
                "GuardiansBlade",
                "Rarity",
                "Tier1Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            voidItems = LoLItems.MyConfig.Bind<string>(
                "GuardiansBlade",
                "Void Items",
                "",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            cooldownReduction = LoLItems.MyConfig.Bind<float>(
                "GuardiansBlade",
                "Cooldown Reduction",
                10f,
                "Amount of cooldown reduction each item will grant."

            );
        }

        private static void CreateItem()
        {
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();
            myItemDef.name = "GuardiansBlade";
            myItemDef.nameToken = "GuardiansBlade";
            myItemDef.pickupToken = "GuardiansBladeItem";
            myItemDef.descriptionToken = "GuardiansBladeDesc";
            myItemDef.loreToken = "GuardiansBladeLore";
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(Utilities.GetRarityFromString(rarity.Value)).WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("GuardiansBladeIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("GuardiansBladePrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = new ItemTag[1] { ItemTag.Utility };
        }

        private static void hooks()
        {            
            On.RoR2.CharacterBody.RecalculateStats += (orig, self) =>
            {
                orig(self);
                if (self.inventory != null && self.inventory.GetItemCount(myItemDef.itemIndex) > 0 && self.skillLocator?.utilityBonusStockSkill?.cooldownScale != null && self.skillLocator?.secondaryBonusStockSkill?.cooldownScale != null)
                {
                    float cdr = Math.Abs(Utilities.HyperbolicScale(self.inventory.GetItemCount(myItemDef.itemIndex), cooldownReduction.Value / 100) - 1);
                    self.skillLocator.utilityBonusStockSkill.cooldownScale *= cdr;
                    self.skillLocator.secondaryBonusStockSkill.cooldownScale *= cdr;
                    Utilities.SetValueInDictionary(ref cooldownReductionTracker, self.master, Math.Abs(cdr - 1) * 100, cooldownReductionTrackerToken, false);
                }
            };

        }

        private static (string, string) GetDisplayInformation(CharacterMaster masterRef)
        {
            string customDescription = "";

            if (cooldownReductionTracker.TryGetValue(masterRef.netId, out float cdr))
                customDescription += "<br><br>Cooldown reduction: " + String.Format("{0:F1}", cdr);
            else
                customDescription += "<br><br>Cooldown reduction: 0";

            return (Language.GetString(myItemDef.descriptionToken), customDescription);
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("GuardiansBlade", "GuardiansBlade");

            // Short description
            LanguageAPI.Add("GuardiansBladeItem", "Reduce the cooldown on secondary and utility skills");

            // Long description
            LanguageAPI.Add("GuardiansBladeDesc", "Reduce the cooldown on your secondary and utility skills by <style=cIsUtility>" + cooldownReduction.Value + "%</style> <style=cStack>(+" + cooldownReduction.Value + ")</style>. Scales hyperbolically, just like tougher times.");

            // Lore
            LanguageAPI.Add("GuardiansBladeLore", "Awesome Refund And Movement.");
        }

        public static void SetupNetworkMappings()
        {
            LoLItems.networkMappings.Add(cooldownReductionTrackerToken, cooldownReductionTracker);
        }
    }
}