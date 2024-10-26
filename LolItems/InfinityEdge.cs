using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using BepInEx.Configuration;

namespace LoLItems
{
    internal class InfinityEdge
    {
        public static ItemDef myItemDef;

        public static ConfigEntry<float> BonusCritChance { get; set; }
        public static ConfigEntry<float> BonusCritDamage { get; set; }
        public static ConfigEntry<bool> Enabled { get; set; }
        public static ConfigEntry<string> Rarity { get; set; }
        public static ConfigEntry<string> VoidItems { get; set; }
        public static Dictionary<NetworkInstanceId, float> bonusDamageDealt = [];
        public static string bonusDamageDealtToken = "InfinityEdge.bonusDamageDealt";
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
            AddTokens();
            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            Hooks();
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, Rarity, VoidItems, "InfinityEdge");
        }

        private static void LoadConfig()
        {
            Enabled = LoLItems.MyConfig.Bind(
                "Infinity Edge",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            Rarity = LoLItems.MyConfig.Bind(
                "Infinity Edge",
                "Rarity",
                "Tier2Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            VoidItems = LoLItems.MyConfig.Bind(
                "Infinity Edge",
                "Void Items",
                "",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            BonusCritChance = LoLItems.MyConfig.Bind(
                "Infinity Edge",
                "Crit Chance",
                5f,
                "Amount of crit chance each item will grant."
            );

            BonusCritDamage = LoLItems.MyConfig.Bind(
                "Infinity Edge",
                "Crit Damage",
                15f,
                "Amount of crit damage each item will grant."
            );
        }

        private static void CreateItem()
        {
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();
            myItemDef.name = "InfinityEdge";
            myItemDef.nameToken = "InfinityEdge";
            myItemDef.pickupToken = "InfinityEdgeItem";
            myItemDef.descriptionToken = "InfinityEdgeDesc";
            myItemDef.loreToken = "InfinityEdgeLore";
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(Utilities.GetRarityFromString(Rarity.Value)).WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = MyAssets.icons.LoadAsset<Sprite>("InfinityEdgeIcon");
            myItemDef.pickupModelPrefab = MyAssets.prefabs.LoadAsset<GameObject>("InfinityEdgePrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
        }

        private static void Hooks()
        {

            // When something takes damage
            On.RoR2.HealthComponent.TakeDamage += (orig, self, damageInfo) =>
            {
                orig(self, damageInfo);
                if (damageInfo.attacker && damageInfo.crit)
                {
                    CharacterBody attackerCharacterBody = damageInfo.attacker.GetComponent<CharacterBody>();
                    
                    if (attackerCharacterBody?.inventory)
                    {
                        int inventoryCount = attackerCharacterBody.inventory.GetItemCount(myItemDef.itemIndex);
                        if (inventoryCount > 0)
                        {
                            float damageDealt = damageInfo.damage * attackerCharacterBody.critMultiplier * (inventoryCount * BonusCritDamage.Value * 0.01f / attackerCharacterBody.critMultiplier);
                            Utilities.AddValueInDictionary(ref bonusDamageDealt, attackerCharacterBody.master, damageDealt, bonusDamageDealtToken);
                        }
                    }
                }
            };

            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        private static void RecalculateStatsAPI_GetStatCoefficients(CharacterBody characterBody, RecalculateStatsAPI.StatHookEventArgs args)
        {
            int count = characterBody?.inventory?.GetItemCount(myItemDef.itemIndex) ?? 0;
            if (count > 0)
            {
                args.critAdd += count * BonusCritChance.Value;
                args.critDamageMultAdd += count * BonusCritDamage.Value * 0.01f;
            }
        }

        private static (string, string) GetDisplayInformation(CharacterMaster masterRef)
        {
            if (masterRef == null)
                return (Language.GetString(myItemDef.descriptionToken), "");
            
            string customDescription = "";
            int itemCount = masterRef.inventory.GetItemCount(myItemDef.itemIndex);
            if (masterRef.inventory.GetItemCount(DLC1Content.Items.ConvertCritChanceToCritDamage) == 0){
                customDescription += "<br><br>Bonus crit chance: " + string.Format("{0:#, ##0.##}", itemCount * BonusCritChance.Value) + "%"
                + "<br>Bonus crit damage: " + string.Format("{0:#, ##0.##}", itemCount * BonusCritDamage.Value) + "%";
            }
            else
            {
                customDescription += "<br><br>Bonus crit chance: 0%"
                + "<br>Bonus crit damage: " + string.Format("{0:#, ##0.##}", itemCount * BonusCritDamage.Value + itemCount * BonusCritChance.Value) + "%";
            }
            

            if (bonusDamageDealt.TryGetValue(masterRef.netId, out float damageDealt))
                customDescription += "<br>Bonus damage dealt: " + string.Format("{0:#, ##0.##}", damageDealt);
            else
                customDescription += "<br>Bonus damage dealt: 0";

            return (Language.GetString(myItemDef.descriptionToken), customDescription);
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("InfinityEdge", "Infinity Edge");

            // Short description
            LanguageAPI.Add("InfinityEdgeItem", "Gain crit chance and crit damage.");

            // Long description
            LanguageAPI.Add("InfinityEdgeDesc", "Gain <style=cIsUtility>" + BonusCritChance.Value + "%</style> <style=cStack>(+" + BonusCritChance.Value + ")</style> crit chance and <style=cIsDamage>" + BonusCritDamage.Value + "%</style> <style=cStack>(+" + BonusCritDamage.Value + ")</style> crit damage.");

            // Lore
            LanguageAPI.Add("InfinityEdgeLore", "For when enemies need to die.");
        }

        public static void SetupNetworkMappings()
        {
            LoLItems.networkMappings.Add(bonusDamageDealtToken, bonusDamageDealt);
        }
    }
}