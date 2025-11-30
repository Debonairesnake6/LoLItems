using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using BepInEx.Configuration;

namespace LoLItems
{
    internal class ImperialMandate
    {
        public static ItemDef myItemDef;
        public static ConfigEntry<float> DamageAmpPerStack { get; set; }
        public static ConfigEntry<bool> Enabled { get; set; }
        public static ConfigEntry<string> Rarity { get; set; }
        public static ConfigEntry<string> VoidItems { get; set; }
        public static Dictionary<NetworkInstanceId, float> bonusDamageDealt = [];
        public static string bonusDamageDealtToken = "ImperialMandate.bonusDamageDealt";
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
            var displayRules = new ItemDisplayRuleDict();
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            Hooks();
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, Rarity, VoidItems, "ImperialMandate");
            SetupNetworkMappings();
        }

        private static void LoadConfig()
        {
            Enabled = LoLItems.MyConfig.Bind(
                "Imperial Mandate",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            Rarity = LoLItems.MyConfig.Bind(
                "Imperial Mandate",
                "Rarity",
                "VoidTier2Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            VoidItems = LoLItems.MyConfig.Bind(
                "Imperial Mandate",
                "Void Items",
                "DeathMark",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            DamageAmpPerStack = LoLItems.MyConfig.Bind(
                "Imperial Mandate",
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
            myItemDef._itemTierDef = LegacyResourcesAPI.Load<ItemTierDef>(Utilities.GetRarityFromString(Rarity.Value));
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = MyAssets.icons.LoadAsset<Sprite>("ImperialMandateIcon");
#pragma warning disable CS0618
            myItemDef.pickupModelPrefab = MyAssets.prefabs.LoadAsset<GameObject>("ImperialMandatePrefab");
#pragma warning restore CS0618
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = [ ItemTag.Damage ];
        }

        private static void Hooks()
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
                        int inventoryCount = attackerCharacterBody.inventory.GetItemCountEffective(myItemDef.itemIndex);
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
                            float extraDamage = damageInfo.damage * DamageAmpPerStack.Value / 100 * inventoryCount * debuffsActive;
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
                customDescription += "<br><br>Damage dealt: " + string.Format("{0:#, ##0.##}", damageDealt);
            else
                customDescription += "<br><br>Damage dealt: 0";

            return (Language.GetString(myItemDef.descriptionToken), customDescription);
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("ImperialMandate", "Imperial Mandate");

            // Short description
            LanguageAPI.Add("ImperialMandateItem", "Do more damage to enemies for each debuff. Corrupts <style=cIsVoid>Death Mark</style>.");

            // Long description
            LanguageAPI.Add("ImperialMandateDesc", "Do <style=cIsDamage>" + DamageAmpPerStack.Value + "%</style> <style=cStack>(+" + DamageAmpPerStack.Value + "%)</style> more damage to enemies for each debuff. Corrupts <style=cIsVoid>Death Mark</style>.");

            // Lore
            LanguageAPI.Add("ImperialMandateLore", "Hunt your prey.");
        }

        public static void SetupNetworkMappings()
        {
            LoLItems.networkMappings.Add(bonusDamageDealtToken, bonusDamageDealt);
        }
    }
}