using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using BepInEx.Configuration;

namespace LoLItems
{
    internal class GuinsoosRageblade
    {

        public static ItemDef myItemDef;
        public static ConfigEntry<float> ProcCoef { get; set; }
        public static ConfigEntry<bool> Enabled { get; set; }
        public static ConfigEntry<string> Rarity { get; set; }
        public static ConfigEntry<string> VoidItems { get; set; }
        public static Dictionary<NetworkInstanceId, float> totalProcCoef = [];
        public static string totalProcCoefToken = "GuinsoosRageblade.totalProcCoef";
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
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, Rarity, VoidItems, "GuinsoosRageblade");
            SetupNetworkMappings();
        }

        private static void LoadConfig()
        {
            Enabled = LoLItems.MyConfig.Bind(
                "Guinsoos Rageblade",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            Rarity = LoLItems.MyConfig.Bind(
                "Guinsoos Rageblade",
                "Rarity",
                "Tier2Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            VoidItems = LoLItems.MyConfig.Bind(
                "Guinsoos Rageblade",
                "Void Items",
                "",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            ProcCoef = LoLItems.MyConfig.Bind(
                "Guinsoos Rageblade",
                "ProcCoef",
                0.1f,
                "Amount of profCoef each item will grant"
            );
        }

        private static void CreateItem()
        {
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();
            myItemDef.name = "GuinsoosRageblade";
            myItemDef.nameToken = "GuinsoosRageblade";
            myItemDef.pickupToken = "GuinsoosRagebladeItem";
            myItemDef.descriptionToken = "GuinsoosRagebladeDesc";
            myItemDef.loreToken = "GuinsoosRagebladeLore";
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(Utilities.GetRarityFromString(Rarity.Value)).WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("GuinsoosRagebladeIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("GuinsoosRagebladePrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = [ ItemTag.Utility ];
        }


        private static void Hooks()
        {
            On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, victim) =>
            {
                if (damageInfo.attacker)
                {
                    CharacterBody attackerCharacterBody = damageInfo.attacker.GetComponent<CharacterBody>();
                    CharacterBody victimCharacterBody = victim.GetComponent<CharacterBody>();
                    
                    if (attackerCharacterBody?.inventory)
                    {
                        int inventoryCount = attackerCharacterBody.inventory.GetItemCount(myItemDef.itemIndex);
                        if (inventoryCount > 0 && damageInfo.procCoefficient > 0)
                        {
                            float extraTotal = ProcCoef.Value * inventoryCount;
                            Utilities.SetValueInDictionary(ref totalProcCoef, attackerCharacterBody.master, extraTotal, totalProcCoefToken);
                            damageInfo.procCoefficient += extraTotal;
                        }
                    }
                }
                orig(self, damageInfo, victim);
            };
        }

        private static (string, string) GetDisplayInformation(CharacterMaster masterRef)
        {
            if (masterRef == null)
                return (Language.GetString(myItemDef.descriptionToken), "");
            
            string customDescription = "";

            if (totalProcCoef.TryGetValue(masterRef.netId, out float value))
                customDescription += "<br><br>Extra procCoef: " + string.Format("{0:F1}", value);
            else
                customDescription += "<br><br>Extra procCoef: 0";

            return (Language.GetString(myItemDef.descriptionToken), customDescription);
        }

        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("GuinsoosRageblade", "Guinsoo\'s Rageblade");

            // Short description
            LanguageAPI.Add("GuinsoosRagebladeItem", "Increase proc coefficient of everything.");

            // Long description
            LanguageAPI.Add("GuinsoosRagebladeDesc", "Gives <style=cIsUtility>" + ProcCoef.Value + "</style> <style=cStack>(+" + ProcCoef.Value + ")</style> proc coefficient to everything.");

            // Lore
            LanguageAPI.Add("GuinsoosRagebladeLore", "Procs go brrrrrrr.");
        }

        public static void SetupNetworkMappings()
        {
            LoLItems.networkMappings.Add(totalProcCoefToken, totalProcCoef);
        }
    }
}