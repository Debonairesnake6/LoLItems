using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using BepInEx.Configuration;

namespace LoLItems
{
    internal class ExperimentalHexplate
    {
        public static ItemDef myItemDef;
        public static BuffDef myBuffDef;

        public static ConfigEntry<float> Duration { get; set; }
        public static ConfigEntry<float> AttackSpeed { get; set; }
        public static ConfigEntry<float> MoveSpeed { get; set; }
        public static ConfigEntry<bool> Enabled { get; set; }
        public static ConfigEntry<string> Rarity { get; set; }
        public static ConfigEntry<string> VoidItems { get; set; }
        public static Dictionary<NetworkInstanceId, float> totalTimesActivated = [];
        public static string totalTimesActivatedToken = "ExperimentalHexplate.totalTimesActivated";
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = [];
        public static Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef = [];
        // ENABLE if a sound effect int is needed (replace num with proper value)
        // public static uint soundEffectID = 1234567890;

        internal static void Init()
        {
            LoadConfig();
            if (!Enabled.Value)
            {
                return;
            }

            CreateItem();
            CreateBuff();
            AddTokens();
            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            ContentAddition.AddBuffDef(myBuffDef);
            Hooks();
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, Rarity, VoidItems, "ExperimentalHexplate");
            SetupNetworkMappings();
        }

        private static void LoadConfig()
        {
            Enabled = LoLItems.MyConfig.Bind(
                "Experimental Hexplate",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            Rarity = LoLItems.MyConfig.Bind(
                "Experimental Hexplate",
                "Rarity",
                "Tier2Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            VoidItems = LoLItems.MyConfig.Bind(
                "Experimental Hexplate",
                "Void Items",
                "",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            Duration = LoLItems.MyConfig.Bind(
                "Experimental Hexplate",
                "Duration",
                5f,
                "The duration of the item buff."
            );

            AttackSpeed = LoLItems.MyConfig.Bind(
                "Experimental Hexplate",
                "Attack Speed",
                40f,
                "The amount of attack speed the item proc grants."
            );

            MoveSpeed = LoLItems.MyConfig.Bind(
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
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(Utilities.GetRarityFromString(Rarity.Value)).WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = MyAssets.icons.LoadAsset<Sprite>("ExperimentalHexplateIcon");
            myItemDef.pickupModelPrefab = MyAssets.prefabs.LoadAsset<GameObject>("ExperimentalHexplatePrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = [ ItemTag.Damage, ItemTag.Utility ];
        }

        private static void CreateBuff()
        {
            myBuffDef = ScriptableObject.CreateInstance<BuffDef>();
            myBuffDef.iconSprite = MyAssets.icons.LoadAsset<Sprite>("ExperimentalHexplateIcon");
            myBuffDef.name = "ExperimentalHexplateBuff";
            myBuffDef.canStack = false;
            myBuffDef.isDebuff = false;
            myBuffDef.isCooldown = false;
            myBuffDef.isHidden = false;
        }

        private static void Hooks()
        {

            On.RoR2.CharacterBody.OnSkillActivated += (orig, self, genericSkill) => {
                if (!NetworkServer.active) {
                    orig(self, genericSkill);
                    return;
                }

                GenericSkill specialSkill = self.skillLocator?.special;

                if (self.inventory?.GetItemCount(myItemDef) > 0 && specialSkill == genericSkill)
                {
                    Utilities.AddTimedBuff(self, myBuffDef, Duration.Value);
                    Utilities.AddValueInDictionary(ref totalTimesActivated, self.master, 1, totalTimesActivatedToken, false);
                }

                orig(self, genericSkill);
            };

            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        private static void RecalculateStatsAPI_GetStatCoefficients(CharacterBody characterBody, RecalculateStatsAPI.StatHookEventArgs args)
        {
            int count = characterBody?.inventory?.GetItemCount(myItemDef.itemIndex) ?? 0;
            if (count > 0 && characterBody.HasBuff(myBuffDef))
            {
                args.baseAttackSpeedAdd += count == 1 ? count * AttackSpeed.Value / 100f : (count - 1) * AttackSpeed.Value / 100f / 2f + AttackSpeed.Value / 100f;
                args.baseMoveSpeedAdd += count == 1 ? count * MoveSpeed.Value / 10f : (count - 1) * MoveSpeed.Value / 10f / 2f + MoveSpeed.Value / 10f;
            }
        }

        private static (string, string) GetDisplayInformation(CharacterMaster masterRef)
        {
            if (masterRef == null)
                return (Language.GetString(myItemDef.descriptionToken), "");
            
            string customDescription = "";

            if (totalTimesActivated.TryGetValue(masterRef.netId, out float timesActivated))
                customDescription += "<br><br>Times activated: " + string.Format("{0:#}", timesActivated);
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
            LanguageAPI.Add("ExperimentalHexplateDesc", "Using your Special skill increases your attack speed by <style=cIsDamage>" + AttackSpeed.Value + "%</style> <style=cStack>(+ " + AttackSpeed.Value / 2f + "%)</style> and your movespeed by <style=cIsUtility>" + MoveSpeed.Value + "%</style> <style=cStack>(+ " + MoveSpeed.Value / 2f + "%)</style> for " + Duration.Value + " seconds.");

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