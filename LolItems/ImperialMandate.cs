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
    internal class ImperialMandate
    {
        public static ItemDef myItemDef;
        public static ConfigEntry<float> damageAmpPerStack { get; set; }
        public static ConfigEntry<bool> enabled { get; set; }
        public static ConfigEntry<string> rarity { get; set; }
        public static ConfigEntry<string> voidItems { get; set; }
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> bonusDamageDealt = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static string bonusDamageDealtToken = "ImperialMandate.bonusDamageDealt";
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
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, rarity, voidItems, "ImperialMandate");
            SetupNetworkMappings();
        }

        private static void LoadConfig()
        {
            enabled = LoLItems.MyConfig.Bind<bool>(
                "ImperialMandate",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            rarity = LoLItems.MyConfig.Bind<string>(
                "ImperialMandate",
                "Rarity",
                "VoidTier2Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            voidItems = LoLItems.MyConfig.Bind<string>(
                "ImperialMandate",
                "Void Items",
                "DeathMark",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            damageAmpPerStack = LoLItems.MyConfig.Bind<float>(
                "ImperialMandate",
                "Damage Amp",
                8f,
                "Amount of bonus damage each item will grant."

            );
        }

        private static void CreateItem()
        {
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();
            myItemDef.name = "ImperialMandate";
            myItemDef.nameToken = "ImperialMandate";
            myItemDef.pickupToken = "ImperialMandateItem";
            myItemDef.descriptionToken = "ImperialMandateDesc";
            myItemDef.loreToken = "ImperialMandateLore";
#pragma warning disable Publicizer001
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(Utilities.GetRarityFromString(rarity.Value)).WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("ImperialMandateIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("ImperialMandatePrefab");
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
                    CharacterBody victimCharacterBody = self.GetComponent<CharacterBody>();
                    
                    if (attackerCharacterBody?.inventory)
                    {
                        int inventoryCount = attackerCharacterBody.inventory.GetItemCount(myItemDef.itemIndex);
                        if (inventoryCount > 0)
                        {
                            int debuffsActive = 0;
                            foreach (BuffIndex buffIndex in BuffCatalog.debuffBuffIndices)
                            {
                                if (victimCharacterBody.HasBuff(buffIndex)) debuffsActive += 1;
                            }
                            DotController dotController = DotController.FindDotController(victimCharacterBody.gameObject);
                            if (dotController)
                            {
                                for (DotController.DotIndex dotIndex = DotController.DotIndex.Bleed; dotIndex < DotController.DotIndex.Count; dotIndex++)
                                {
                                    if (dotController.HasDotActive(dotIndex)) debuffsActive += 1;
                                }
                            }
                            float extraDamage = damageInfo.damage * damageAmpPerStack.Value / 100 * inventoryCount * debuffsActive;
                            damageInfo.damage += extraDamage;
                            Utilities.AddValueInDictionary(ref bonusDamageDealt, attackerCharacterBody.master, extraDamage, bonusDamageDealtToken);
                        }
                    }
                }
                orig(self, damageInfo);
            };
        }

        private static (string, string) GetDisplayInformation(CharacterMaster masterRef)
        {
            if (masterRef == null)
                return (Language.GetString(myItemDef.descriptionToken), "");
            
            string customDescription = "";

            if (bonusDamageDealt.TryGetValue(masterRef.netId, out float damageDealt))
                customDescription += "<br><br>Damage dealt: " + String.Format("{0:#}", damageDealt);
            else
                customDescription += "<br><br>Damage dealt: 0";

            return (Language.GetString(myItemDef.descriptionToken), customDescription);
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            // Styles
            // <style=cIsHealth>" + exampleValue + "</style>
            // <style=cIsDamage>" + exampleValue + "</style>
            // <style=cIsHealing>" + exampleValue + "</style>
            // <style=cIsUtility>" + exampleValue + "</style>
            // <style=cIsVoid>" + exampleValue + "</style>
            // <style=cHumanObjective>" + exampleValue + "</style>
            // <style=cLunarObjective>" + exampleValue + "</style>
            // <style=cStack>" + exampleValue + "</style>
            // <style=cWorldEvent>" + exampleValue + "</style>
            // <style=cArtifact>" + exampleValue + "</style>
            // <style=cUserSetting>" + exampleValue + "</style>
            // <style=cDeath>" + exampleValue + "</style>
            // <style=cSub>" + exampleValue + "</style>
            // <style=cMono>" + exampleValue + "</style>
            // <style=cShrine>" + exampleValue + "</style>
            // <style=cEvent>" + exampleValue + "</style>

            // Name of the item
            LanguageAPI.Add("ImperialMandate", "ImperialMandate");

            // Short description
            LanguageAPI.Add("ImperialMandateItem", "Do more damage to enemies for each debuff. Corrupts <style=cIsVoid>Death Mark</style>.");

            // Long description
            LanguageAPI.Add("ImperialMandateDesc", "Do <style=cIsDamage>" + damageAmpPerStack.Value + "%</style> <style=cStack>(+" + damageAmpPerStack.Value + "%)</style> more damage to enemies for each debuff. Corrupts <style=cIsVoid>Death Mark</style>.");

            // Lore
            LanguageAPI.Add("ImperialMandateLore", "Hunt your prey.");
        }

        public static void SetupNetworkMappings()
        {
            LoLItems.networkMappings.Add(bonusDamageDealtToken, bonusDamageDealt);
        }
    }
}