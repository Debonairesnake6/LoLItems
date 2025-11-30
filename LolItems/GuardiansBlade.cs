using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using System;
using BepInEx.Configuration;

namespace LoLItems
{
    internal class GuardiansBlade
    {
        public static ItemDef myItemDef;
        public static ConfigEntry<float> CooldownReduction { get; set; }
        public static ConfigEntry<bool> Enabled { get; set; }
        public static ConfigEntry<string> Rarity { get; set; }
        public static ConfigEntry<string> VoidItems { get; set; }
        public static Dictionary<NetworkInstanceId, float> cooldownReductionTracker = [];
        public static string cooldownReductionTrackerToken = "GuardiansBlade.cooldownReductionTracker";
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
            AddTokens();
            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict();
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            Hooks();
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, Rarity, VoidItems, "GuardiansBlade");
            SetupNetworkMappings();
        }

        private static void LoadConfig()
        {
            Enabled = LoLItems.MyConfig.Bind(
                "Guardians Blade",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            Rarity = LoLItems.MyConfig.Bind(
                "Guardians Blade",
                "Rarity",
                "Tier1Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            VoidItems = LoLItems.MyConfig.Bind(
                "Guardians Blade",
                "Void Items",
                "",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            CooldownReduction = LoLItems.MyConfig.Bind(
                "Guardians Blade",
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
            myItemDef._itemTierDef = LegacyResourcesAPI.Load<ItemTierDef>(Utilities.GetRarityFromString(Rarity.Value));
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = MyAssets.icons.LoadAsset<Sprite>("GuardiansBladeIcon");
#pragma warning disable CS0618
            myItemDef.pickupModelPrefab = MyAssets.prefabs.LoadAsset<GameObject>("GuardiansBladePrefab");
#pragma warning restore CS0618
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = [ ItemTag.Utility ];
        }

        private static void Hooks()
        {
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
            
            On.RoR2.Inventory.HandleInventoryChanged += (orig, self) =>
            {
                orig(self);
                int count = self.GetItemCountEffective(myItemDef.itemIndex);
                if (count > 0)
                {
                    float cdr = Math.Abs(Utilities.HyperbolicScale(count, CooldownReduction.Value / 100) - 1);
                    Utilities.SetValueInDictionary(ref cooldownReductionTracker, self.GetComponentInParent<RoR2.CharacterMaster>(), Math.Abs(cdr - 1) * 100, cooldownReductionTrackerToken, false);
                }
            };
        }

        private static void RecalculateStatsAPI_GetStatCoefficients(CharacterBody characterBody, RecalculateStatsAPI.StatHookEventArgs args)
        {
            int count = characterBody?.inventory?.GetItemCountEffective(myItemDef.itemIndex) ?? 0;
            if (count > 0)
            {
                float cdr = Utilities.HyperbolicScale(count, CooldownReduction.Value / 100);
                args.utilityCooldownMultAdd -= cdr;
                args.secondaryCooldownMultAdd -= cdr;
            }
        }

        private static (string, string) GetDisplayInformation(CharacterMaster masterRef)
        {
            if (masterRef == null)
                return (Language.GetString(myItemDef.descriptionToken), "");
                
            string customDescription = "";

            if (cooldownReductionTracker.TryGetValue(masterRef.netId, out float cdr))
                customDescription += "<br><br>Cooldown reduction: " + string.Format("{0:F1}", cdr);
            else
                customDescription += "<br><br>Cooldown reduction: 0";

            return (Language.GetString(myItemDef.descriptionToken), customDescription);
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("GuardiansBlade", "Guardian\'s Blade");

            // Short description
            LanguageAPI.Add("GuardiansBladeItem", "Reduce the cooldown on secondary and utility skills.");

            // Long description
            LanguageAPI.Add("GuardiansBladeDesc", "Reduce the cooldown on your secondary and utility skills by <style=cIsUtility>" + CooldownReduction.Value + "%</style> <style=cStack>(+" + CooldownReduction.Value + ")</style>. Scales hyperbolically, just like tougher times.");

            // Lore
            LanguageAPI.Add("GuardiansBladeLore", "Awesome refund And movement.");
        }

        public static void SetupNetworkMappings()
        {
            LoLItems.networkMappings.Add(cooldownReductionTrackerToken, cooldownReductionTracker);
        }
    }
}