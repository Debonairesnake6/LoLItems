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
    internal class GargoyleStoneplate
    {
        public static EquipmentDef myEquipmentDef;
        public static ConfigEntry<float> barrierPercent { get; set; }
        public static ConfigEntry<float> barrierCooldown { get; set; }
        public static ConfigEntry<bool> enabled { get; set; }
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> totalBarrierGiven = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = new Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster>();
        public static Dictionary<RoR2.UI.EquipmentIcon, CharacterMaster> IconToMasterRef = new Dictionary<RoR2.UI.EquipmentIcon, CharacterMaster>();

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
            // Enable for custom display rules
            // ItemDisplayRuleDict itemDisplayRuleDict = CreateDisplayRules();
            ItemAPI.Add(new CustomEquipment(myEquipmentDef, displayRules));
            hooks();
            //Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myEquipmentDef, GetDisplayInformation, "GargoyleStoneplate");
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
                50f,
                "Cooldown of the item."

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
        }

        private static bool ActivateEquipment(EquipmentSlot slot)
        {
            float barrierAmount = slot.characterBody.healthComponent.fullHealth * barrierPercent.Value / 100;
            if (barrierAmount > slot.characterBody.healthComponent.fullHealth)
                barrierAmount = slot.characterBody.healthComponent.fullHealth;
            slot.characterBody.healthComponent.AddBarrier(barrierAmount);
            Utilities.AddValueInDictionary(ref totalBarrierGiven, slot.characterBody.master, barrierAmount, false);
            // UNCOMMENT WHEN SOUND IS ADDED!!
            // AkSoundEngine.PostEvent(1234567890, slot.characterBody.gameObject);
            return true;
        }

        private static string GetDisplayInformation(CharacterMaster masterRef)
        {
            // Update the description for an item in the HUD
            if (masterRef != null && totalBarrierGiven.TryGetValue(masterRef.netId, out float barrierGiven)){
                return Language.GetString(myEquipmentDef.descriptionToken) + "<br><br>Barrier given: " + String.Format("{0:#}", barrierGiven);
            }
            return Language.GetString(myEquipmentDef.descriptionToken);
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgments!
        private static void AddTokens()
        {
            // Styles
            // <style=cIsHealth>" + exampleValue.Value + "</style>
            // <style=cIsDamage>" + exampleValue.Value + "</style>
            // <style=cIsHealing>" + exampleValue.Value + "</style>
            // <style=cIsUtility>" + exampleValue.Value + "</style>
            // <style=cIsVoid>" + exampleValue.Value + "</style>
            // <style=cHumanObjective>" + exampleValue.Value + "</style>
            // <style=cLunarObjective>" + exampleValue.Value + "</style>
            // <style=cStack>" + exampleValue.Value + "</style>
            // <style=cWorldEvent>" + exampleValue.Value + "</style>
            // <style=cArtifact>" + exampleValue.Value + "</style>
            // <style=cUserSetting>" + exampleValue.Value + "</style>
            // <style=cDeath>" + exampleValue.Value + "</style>
            // <style=cSub>" + exampleValue.Value + "</style>
            // <style=cMono>" + exampleValue.Value + "</style>
            // <style=cShrine>" + exampleValue.Value + "</style>
            // <style=cEvent>" + exampleValue.Value + "</style>

            // Name of the item
            LanguageAPI.Add("GargoyleStoneplate", "Gargoyle Stoneplate");

            // Short description
            LanguageAPI.Add("GargoyleStoneplateItem", "Temporarily gain a barrier based on your health.");

            // Long description
            LanguageAPI.Add("GargoyleStoneplateDesc", $"Temporarily gain a barrier for <style=cIsHealing>{barrierPercent.Value}%</style> of your health.");

            // Lore
            LanguageAPI.Add("GargoyleStoneplateLore", "Whoever thought of breaking this off of a gargoyle's body and strapping it onto their own body was a genius.");
        }
    }
}