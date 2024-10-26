using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using BepInEx.Configuration;


namespace LoLItems
{
    internal class BannerOfCommand
    {
        public static ItemDef myItemDef;

        public static ConfigEntry<float> DamagePercentAmp { get; set; }
        public static ConfigEntry<bool> Enabled { get; set; }
        public static ConfigEntry<string> Rarity { get; set; }
        public static ConfigEntry<string> VoidItems { get; set; }
        public static Dictionary<NetworkInstanceId, float> bonusDamageDealt = [];
        public static string bonusDamageDealtToken = "BannerOfCommand.bonusDamageDealt";
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
            ItemDisplayRuleDict displayRules = new(null);
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            Hooks();
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, Rarity, VoidItems, "BannerOfCommand");
            SetupNetworkMappings();
        }

        private static void LoadConfig()
        {
            Enabled = LoLItems.MyConfig.Bind(
                "Banner of Command",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            Rarity = LoLItems.MyConfig.Bind(
                "Banner of Command",
                "Rarity",
                "Tier1Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            VoidItems = LoLItems.MyConfig.Bind(
                "Banner of Command",
                "Void Items",
                "",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            DamagePercentAmp = LoLItems.MyConfig.Bind(
                "Banner of Command",
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
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(Utilities.GetRarityFromString(Rarity.Value)).WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = MyAssets.icons.LoadAsset<Sprite>("BannerOfCommandIcon");
            myItemDef.pickupModelPrefab = MyAssets.prefabs.LoadAsset<GameObject>("BannerOfCommandPrefab");
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
                    CharacterMaster ownerMaster = attackerCharacterBody?.master?.minionOwnership?.ownerMaster;
                    
                    if (ownerMaster?.inventory)
                    {
                        int inventoryCount = ownerMaster.inventory.GetItemCount(myItemDef.itemIndex);
                        if (inventoryCount > 0)
                        {
                            float extraDamage = 1 + (inventoryCount * DamagePercentAmp.Value / 100);
                            Utilities.AddValueInDictionary(ref bonusDamageDealt, ownerMaster, extraDamage * damageInfo.damage, bonusDamageDealtToken);
                            damageInfo.damage *= extraDamage;
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

        // This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opinionated. Make your own judgments!
        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("BannerOfCommand", "Banner of Command");

            // Short description
            LanguageAPI.Add("BannerOfCommandItem", "Increase allied minion damage.");

            // Long description
            LanguageAPI.Add("BannerOfCommandDesc", "Increase the damage of allied minions by <style=cIsUtility>" + DamagePercentAmp.Value + "%</style> <style=cStack>(+" + DamagePercentAmp.Value + "%)</style>.");

            // Lore
            LanguageAPI.Add("BannerOfCommandLore", "Split pushing is boring.");
        }

        public static void SetupNetworkMappings()
        {
            LoLItems.networkMappings.Add(bonusDamageDealtToken, bonusDamageDealt);
        }
    }
}