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
    internal class GuinsoosRageblade
    {

        public static ItemDef myItemDef;
        public static ConfigEntry<float> procCoef { get; set; }
        public static ConfigEntry<bool> enabled { get; set; }
        public static ConfigEntry<string> rarity { get; set; }
        public static ConfigEntry<string> voidItems { get; set; }
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> totalProcCoef = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = new Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster>();
        public static Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef = new Dictionary<RoR2.UI.ItemIcon, CharacterMaster>();

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
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, rarity, voidItems, "GuinsoosRageblade");
        }

        private static void LoadConfig()
        {
            enabled = LoLItems.MyConfig.Bind<bool>(
                "GuinsoosRageblade",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            rarity = LoLItems.MyConfig.Bind<string>(
                "GuinsoosRageblade",
                "Rarity",
                "Tier2Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            voidItems = LoLItems.MyConfig.Bind<string>(
                "GuinsoosRageblade",
                "Void Items",
                "",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            procCoef = LoLItems.MyConfig.Bind<float>(
                "GuinsoosRageblade",
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
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(Utilities.GetRarityFromString(rarity.Value)).WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("GuinsoosRagebladeIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("GuinsoosRagebladePrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = new ItemTag[1] { ItemTag.Utility };
        }


        private static void hooks()
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
                            float extraTotal = procCoef.Value * inventoryCount;
                            Utilities.SetValueInDictionary(ref totalProcCoef, attackerCharacterBody.master, extraTotal);
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
                customDescription += "<br><br>Extra procCoef: " + String.Format("{0:F1}", value);
            else
                customDescription += "<br><br>Extra procCoef: 0";

            return (Language.GetString(myItemDef.descriptionToken), customDescription);
        }

        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("GuinsoosRageblade", "GuinsoosRageblade");

            // Short description
            LanguageAPI.Add("GuinsoosRagebladeItem", "Increase proc coefficient of everything");

            // Long description
            LanguageAPI.Add("GuinsoosRagebladeDesc", "Gives <style=cIsUtility>" + procCoef.Value + "</style> <style=cStack>(+" + procCoef.Value + ")</style> proc coefficient to everything");

            // Lore
            LanguageAPI.Add("GuinsoosRagebladeLore", "Procs go brrrrrrr.");
        }
    }
}