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
using R2API.Networking.Interfaces;
using UnityEngine.Networking;
using R2API.Networking;
using EntityStates.AffixVoid;

namespace LoLItems
{
    internal class GargoyleStoneplate
    {
        public static BuffDef gargoyleArmorBuff;
        public static EquipmentDef myEquipmentDef;
        public static ConfigEntry<float> barrierPercent { get; set; }
        public static ConfigEntry<float> barrierCooldown { get; set; }
        public static ConfigEntry<float> armorDuration { get; set; }
        public static ConfigEntry<float> armorValue { get; set; }
        public static ConfigEntry<bool> enabled { get; set; }
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> totalBarrierGiven = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static string totalBarrierGivenToken = "GargoyleStoneplate.totalBarrierGiven";
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = new Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster>();
        public static Dictionary<RoR2.UI.EquipmentIcon, CharacterMaster> IconToMasterRef = new Dictionary<RoR2.UI.EquipmentIcon, CharacterMaster>();
        public static uint activateSoundEffectID = 2213188569;

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
            ContentAddition.AddBuffDef(gargoyleArmorBuff);
            AddTokens();
            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomEquipment(myEquipmentDef, displayRules));
            hooks();
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, myEquipmentDef, GetDisplayInformation);
            SetupNetworkMappings();
        }

        private static void LoadConfig()
        {
            enabled = LoLItems.MyConfig.Bind<bool>(
                "GargoyleStoneplate",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            barrierPercent = LoLItems.MyConfig.Bind<float>(
                "GargoyleStoneplate",
                "Barrier Percentage",
                60f,
                "Percent of barrier Gargoyle Stoneplate will grant you."

            );

            barrierCooldown = LoLItems.MyConfig.Bind<float>(
                "GargoyleStoneplate",
                "Barrier Cooldown",
                60f,
                "Cooldown of the item."

            );

            armorDuration = LoLItems.MyConfig.Bind<float>(
                "GargoyleStoneplate",
                "Armor Duration",
                2f,
                "Duration of the armor buff."
            );

            armorValue = LoLItems.MyConfig.Bind<float>(
                "GargoyleStoneplate",
                "Armor Value",
                100f,
                "Armor value given during the buff."
            );
        }

        private static void CreateItem()
        {
            myEquipmentDef = ScriptableObject.CreateInstance<EquipmentDef>();
            myEquipmentDef.name = "GargoyleStoneplate";  // Replace this string throughout the entire file with your new item name
            myEquipmentDef.nameToken = "GargoyleStoneplate";
            myEquipmentDef.pickupToken = "GargoyleStoneplateItem";
            myEquipmentDef.descriptionToken = "GargoyleStoneplateDesc";
            myEquipmentDef.loreToken = "GargoyleStoneplateLore";
            myEquipmentDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("GargoyleStoneplateIcon");
            myEquipmentDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("GargoyleStoneplatePrefab");
            myEquipmentDef.canDrop = true;
            myEquipmentDef.appearsInMultiPlayer = true;
            myEquipmentDef.appearsInSinglePlayer = true;
            myEquipmentDef.canBeRandomlyTriggered = true;
            myEquipmentDef.enigmaCompatible = true;
            myEquipmentDef.cooldown = barrierCooldown.Value;
        }

        private static void CreateBuff()
        {
            gargoyleArmorBuff = ScriptableObject.CreateInstance<BuffDef>();

            gargoyleArmorBuff.iconSprite = Assets.icons.LoadAsset<Sprite>("GargoyleStoneplateIcon");
            gargoyleArmorBuff.name = "gargoyleArmorBuff";
            gargoyleArmorBuff.canStack = false;
            gargoyleArmorBuff.isDebuff = false;
            gargoyleArmorBuff.isCooldown = true;
            gargoyleArmorBuff.isHidden = false;
        }


        private static void hooks()
        {
            On.RoR2.EquipmentSlot.PerformEquipmentAction += (orig, self, equipmentDef) =>
            {
                if (equipmentDef == myEquipmentDef)
                {
                    return ActivateEquipment(self);
                }
                return orig(self, equipmentDef);
            };

            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        private static void RecalculateStatsAPI_GetStatCoefficients(CharacterBody characterBody, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (characterBody.HasBuff(gargoyleArmorBuff))
            {
                args.armorAdd += armorValue.Value;
            }
        }

        private static bool ActivateEquipment(EquipmentSlot slot)
        {
            float barrierAmount = slot.characterBody.healthComponent.fullHealth * barrierPercent.Value / 100;
            if (barrierAmount > slot.characterBody.healthComponent.fullHealth)
                barrierAmount = slot.characterBody.healthComponent.fullHealth;
            slot.characterBody.healthComponent.AddBarrier(barrierAmount);
            Utilities.AddTimedBuff(slot.characterBody, gargoyleArmorBuff, armorDuration.Value);
            Utilities.AddValueInDictionary(ref totalBarrierGiven, slot.characterBody.master, barrierAmount, totalBarrierGivenToken, false);
            AkSoundEngine.PostEvent(activateSoundEffectID, slot.characterBody.gameObject);
            return true;
        }

        private static (string, string) GetDisplayInformation(CharacterMaster masterRef)
        {
            if (masterRef == null)
                return (Language.GetString(myEquipmentDef.descriptionToken), "");
            
            string customDescription = "";

            if (totalBarrierGiven.TryGetValue(masterRef.netId, out float barrierGiven))
                customDescription += "<br><br>Barrier given: " + String.Format("{0:#}", barrierGiven);
            else
                customDescription += "<br><br>Barrier given: 0";

            return (Language.GetString(myEquipmentDef.descriptionToken), customDescription);
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgments!
        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("GargoyleStoneplate", "Gargoyle Stoneplate");

            // Short description
            LanguageAPI.Add("GargoyleStoneplateItem", "Temporarily gain a barrier based on your health.");

            // Long description
            LanguageAPI.Add("GargoyleStoneplateDesc", $"Temporarily gain a barrier for <style=cIsHealing>{barrierPercent.Value}%</style> of your health.");

            // Lore
            LanguageAPI.Add("GargoyleStoneplateLore", "Whoever thought of breaking this off of a gargoyle's body and strapping it onto their own body was a genius.");
        }

        public static void SetupNetworkMappings()
        {
            LoLItems.networkMappings.Add(totalBarrierGivenToken, totalBarrierGiven);
        }
    }
}