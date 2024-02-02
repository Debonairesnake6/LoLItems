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
using System.IO;
using EntityStates.AffixVoid;
using System.Runtime.CompilerServices;
using UnityEngine.Networking;

namespace LoLItems
{
    internal class ExperimentalHexplate
    {
        public static ItemDef myItemDef;
        public static BuffDef myBuffDef;

        public static ConfigEntry<float> duration { get; set; }
        public static ConfigEntry<float> attackSpeed { get; set; }
        public static ConfigEntry<float> moveSpeed { get; set; }
        public static ConfigEntry<bool> enabled { get; set; }
        public static ConfigEntry<string> rarity { get; set; }
        public static ConfigEntry<string> voidItems { get; set; }
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> totalTimesActivated = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static string totalTimesActivatedToken = "ExperimentalHexplate.totalTimesActivated";
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = new Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster>();
        public static Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef = new Dictionary<RoR2.UI.ItemIcon, CharacterMaster>();
        // ENABLE if a sound effect int is needed (replace num with proper value)
        // public static uint soundEffectID = 1234567890;

        internal static void Init()
        {
            LoadConfig();
            if (!enabled.Value)
            {
                return;
            }

            CreateItem();
            CreateBuff();
            AddTokens();
            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            ContentAddition.AddBuffDef(myBuffDef);
            hooks();
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, rarity, voidItems, "ExperimentalHexplate");
            SetupNetworkMappings();
        }

        private static void LoadConfig()
        {
            enabled = LoLItems.MyConfig.Bind<bool>(
                "Experimental Hexplate",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            rarity = LoLItems.MyConfig.Bind<string>(
                "Experimental Hexplate",
                "Rarity",
                "Tier2Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            voidItems = LoLItems.MyConfig.Bind<string>(
                "Experimental Hexplate",
                "Void Items",
                "",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            duration = LoLItems.MyConfig.Bind<float>(
                "Experimental Hexplate",
                "Duration",
                5f,
                "The duration of the item buff."

            );

            attackSpeed = LoLItems.MyConfig.Bind<float>(
                "Experimental Hexplate",
                "Attack Speed",
                40f,
                "The amount of attack speed the item proc grants."

            );

            moveSpeed = LoLItems.MyConfig.Bind<float>(
                "Experimental Hexplate",
                "Movespeed",
                32f,
                "The amount of movespeed the item proc grants."

            );
        }

        private static void CreateItem()
        {
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();
            myItemDef.name = "ExperimentalHexplate";
            myItemDef.nameToken = "ExperimentalHexplate";
            myItemDef.pickupToken = "ExperimentalHexplateItem";
            myItemDef.descriptionToken = "ExperimentalHexplateDesc";
            myItemDef.loreToken = "ExperimentalHexplateLore";
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(Utilities.GetRarityFromString(rarity.Value)).WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("ExperimentalHexplateIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("ExperimentalHexplatePrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = new ItemTag[2] { ItemTag.Damage, ItemTag.Utility };
        }

        private static void CreateBuff()
        {
            myBuffDef = ScriptableObject.CreateInstance<BuffDef>();
            myBuffDef.iconSprite = Assets.icons.LoadAsset<Sprite>("ExperimentalHexplateIcon");
            myBuffDef.name = "ExperimentalHexplateBuff";
            myBuffDef.canStack = false;
            myBuffDef.isDebuff = false;
            myBuffDef.isCooldown = false;
            myBuffDef.isHidden = false;
        }

        private static void hooks()
        {
            On.RoR2.GenericSkill.OnExecute += (orig, self) => {
                if (!NetworkServer.active)
                    return;
                    
                GenericSkill specialSkill = self.characterBody?.skillLocator?.special;

                if (self.characterBody?.inventory?.GetItemCount(myItemDef) > 0 && specialSkill == self)
                {
                    Utilities.AddTimedBuff(self.characterBody, myBuffDef, duration.Value);
                    Utilities.AddValueInDictionary(ref totalTimesActivated, self.characterBody.master, 1, totalTimesActivatedToken, false);
                }

                orig(self);
            };

            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        private static void RecalculateStatsAPI_GetStatCoefficients(CharacterBody characterBody, RecalculateStatsAPI.StatHookEventArgs args)
        {
            int count = characterBody?.inventory?.GetItemCount(myItemDef.itemIndex) ?? 0;
            if (count > 0 && characterBody.HasBuff(myBuffDef))
            {
                args.baseAttackSpeedAdd += count == 1 ? count * attackSpeed.Value / 100f : (count - 1) * attackSpeed.Value / 100f / 2f + attackSpeed.Value / 100f;
                args.baseMoveSpeedAdd += count == 1 ? count * moveSpeed.Value / 10f : (count - 1) * moveSpeed.Value / 10f / 2f + moveSpeed.Value / 10f;
            }
        }

        private static (string, string) GetDisplayInformation(CharacterMaster masterRef)
        {
            if (masterRef == null)
                return (Language.GetString(myItemDef.descriptionToken), "");
            
            string customDescription = "";

            if (totalTimesActivated.TryGetValue(masterRef.netId, out float timesActivated))
                customDescription += "<br><br>Times activated: " + String.Format("{0:#}", timesActivated);
            else
                customDescription += "<br><br>Times activated: 0";

            return (Language.GetString(myItemDef.descriptionToken), customDescription);
        }

        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("ExperimentalHexplate", "Experimental Hexplate");

            // Short description
            LanguageAPI.Add("ExperimentalHexplateItem", "Using your Special skill temporarily increases your attack speed and movespeed.");

            // Long description
            LanguageAPI.Add("ExperimentalHexplateDesc", "Using your Special skill increases your attack speed by <style=cIsDamage>" + attackSpeed.Value + "%</style> <style=cStack>(+ " + attackSpeed.Value / 2f + "%)</style> and your movespeed by <style=cIsUtility>" + moveSpeed.Value + "%</style> <style=cStack>(+ " + moveSpeed.Value / 2f + "%)</style> for " + duration.Value + " seconds.");

            // Lore
            LanguageAPI.Add("ExperimentalHexplateLore", "Was it wise to put on something this experimental? <br><br>Probably.");

            // ENABLE for buff
            LanguageAPI.Add("ExperimentalHexplateBuff", "Experimental Hexplate");
        }

        public static void SetupNetworkMappings()
        {
            LoLItems.networkMappings.Add(totalTimesActivatedToken, totalTimesActivated);
        }
    }
}