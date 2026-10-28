using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using BepInEx.Configuration;

namespace LoLItems
{
    internal class GargoyleStoneplate
    {
        public static BuffDef gargoyleArmorBuff;
        public static EquipmentDef myEquipmentDef;
        public static ConfigEntry<float> BarrierPercent { get; set; }
        public static ConfigEntry<float> BarrierCooldown { get; set; }
        public static ConfigEntry<float> ArmorDuration { get; set; }
        public static ConfigEntry<float> ArmorValue { get; set; }
        public static ConfigEntry<bool> Enabled { get; set; }
        public static Dictionary<NetworkInstanceId, float> totalBarrierGiven = [];
        public static string totalBarrierGivenToken = "GargoyleStoneplate.totalBarrierGiven";
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = [];
        public static Dictionary<RoR2.UI.EquipmentIcon, CharacterMaster> IconToMasterRef = [];
        public static uint activateSoundEffectID = 2213188569;

        // This runs when loading the file
        internal static void Init()
        {
            LoadConfig();
            if (!Enabled.Value)
            {
                return;
            }

            CreateItem();
            CreateBuff();
            ContentAddition.AddBuffDef(gargoyleArmorBuff);
            AddTokens();
            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict();
            ItemAPI.Add(new CustomEquipment(myEquipmentDef, displayRules));
            Hooks();
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, myEquipmentDef, GetDisplayInformation);
            SetupNetworkMappings();
        }

        private static void LoadConfig()
        {
            Enabled = LoLItems.MyConfig.Bind(
                "Gargoyle Stoneplate",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            BarrierPercent = LoLItems.MyConfig.Bind(
                "Gargoyle Stoneplate",
                "Barrier Percentage",
                60f,
                "Percent of barrier Gargoyle Stoneplate will grant you."
            );

            BarrierCooldown = LoLItems.MyConfig.Bind(
                "Gargoyle Stoneplate",
                "Barrier Cooldown",
                60f,
                "Cooldown of the item."
            );

            ArmorDuration = LoLItems.MyConfig.Bind(
                "Gargoyle Stoneplate",
                "Armor Duration",
                2f,
                "Duration of the armor buff."
            );

            ArmorValue = LoLItems.MyConfig.Bind(
                "Gargoyle Stoneplate",
                "Armor Value",
                100f,
                "Armor value given during the buff."
            );
        }

        private static void CreateItem()
        {
            myEquipmentDef = ScriptableObject.CreateInstance<EquipmentDef>();
            myEquipmentDef.name = "GargoyleStoneplate";
            myEquipmentDef.nameToken = "GargoyleStoneplate";
            myEquipmentDef.pickupToken = "GargoyleStoneplateItem";
            myEquipmentDef.descriptionToken = "GargoyleStoneplateDesc";
            myEquipmentDef.loreToken = "GargoyleStoneplateLore";
            myEquipmentDef.pickupIconSprite = MyAssets.icons.LoadAsset<Sprite>("GargoyleStoneplateIcon");
#pragma warning disable CS0618
            myEquipmentDef.pickupModelPrefab = MyAssets.prefabs.LoadAsset<GameObject>("GargoyleStoneplatePrefab");
#pragma warning restore CS0618
            myEquipmentDef.canDrop = true;
            myEquipmentDef.appearsInMultiPlayer = true;
            myEquipmentDef.appearsInSinglePlayer = true;
            myEquipmentDef.canBeRandomlyTriggered = true;
            myEquipmentDef.enigmaCompatible = true;
            myEquipmentDef.cooldown = BarrierCooldown.Value;
        }

        private static void CreateBuff()
        {
            gargoyleArmorBuff = ScriptableObject.CreateInstance<BuffDef>();

            gargoyleArmorBuff.iconSprite = MyAssets.icons.LoadAsset<Sprite>("GargoyleStoneplateIcon");
            gargoyleArmorBuff.name = "Gargoyle Stoneplate Armor Buff";
            gargoyleArmorBuff.canStack = false;
            gargoyleArmorBuff.isDebuff = false;
            gargoyleArmorBuff.isCooldown = true;
            gargoyleArmorBuff.isHidden = false;
        }


        private static void Hooks()
        {
            On.RoR2.EquipmentSlot.PerformEquipmentAction += (orig, self, equipmentDef) =>
            {
                if (NetworkServer.active && equipmentDef == myEquipmentDef)
                    return ActivateEquipment(self);
                return orig(self, equipmentDef);
            };

            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        private static void RecalculateStatsAPI_GetStatCoefficients(CharacterBody characterBody, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (NetworkServer.active && characterBody.HasBuff(gargoyleArmorBuff))
                args.armorAdd += ArmorValue.Value;
        }

        private static bool ActivateEquipment(EquipmentSlot slot)
        {
            float barrierAmount = slot.characterBody.healthComponent.fullHealth * BarrierPercent.Value / 100;
            if (barrierAmount > slot.characterBody.healthComponent.fullHealth)
                barrierAmount = slot.characterBody.healthComponent.fullHealth;
            slot.characterBody.healthComponent.AddBarrier(barrierAmount);
            Utilities.AddTimedBuff(slot.characterBody, gargoyleArmorBuff, ArmorDuration.Value);
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
                customDescription += "<br><br>Barrier given: " + string.Format("{0:#, ##0.##}", barrierGiven);
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
            LanguageAPI.Add("GargoyleStoneplateItem", "Temporarily gain armor and a barrier based on your maximum health.");

            // Long description
            LanguageAPI.Add("GargoyleStoneplateDesc", $"Temporarily gain <style=cIsHealing>{ArmorValue.Value}</style> armor for <style=cIsUtility>{ArmorDuration.Value}s</style> and a barrier for <style=cIsHealing>{BarrierPercent.Value}%</style> of your maximum health.");

            // Lore
            LanguageAPI.Add("GargoyleStoneplateLore", "Whoever thought of breaking this off of a gargoyle's body and strapping it onto their own body was a genius.");
        }

        public static void SetupNetworkMappings()
        {
            LoLItems.networkMappings.Add(totalBarrierGivenToken, totalBarrierGiven);
        }
    }
}