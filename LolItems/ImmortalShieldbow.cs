using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using BepInEx.Configuration;

namespace LoLItems
{
    internal class ImmortalShieldbow
    {
        public static ItemDef myItemDef;
        public static BuffDef myBuffDefCooldown;
        public static ConfigEntry<float> BarrierPercent { get; set; }
        public static ConfigEntry<float> BuffCooldown { get; set; }
        public static ConfigEntry<float> BarrierThreshold { get; set; }
        public static ConfigEntry<bool> Enabled { get; set; }
        public static ConfigEntry<string> Rarity { get; set; }
        public static ConfigEntry<string> VoidItems { get; set; }
        public static Dictionary<NetworkInstanceId, float> totalShieldGiven = [];
        public static string totalShieldGivenToken = "ImmortalShieldbow.totalShieldGiven";
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = [];
        public static Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef = [];
        public static uint procSoundEffect = 2060112413;

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
            AddTokens();
            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict();
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            ContentAddition.AddBuffDef(myBuffDefCooldown);
            Hooks();
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, Rarity, VoidItems, "ImmortalShieldbow");
            SetupNetworkMappings();
        }

        private static void LoadConfig()
        {
            Enabled = LoLItems.MyConfig.Bind(
                "Immortal Shieldbow",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            Rarity = LoLItems.MyConfig.Bind(
                "Immortal Shieldbow",
                "Rarity",
                "Tier2Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            VoidItems = LoLItems.MyConfig.Bind(
                "Immortal Shieldbow",
                "Void Items",
                "",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            BarrierPercent = LoLItems.MyConfig.Bind(
                "Immortal Shieldbow",
                "Barrier Percent",
                40f,
                "Amount of percent max health barrier each item will grant."
            );

            BuffCooldown = LoLItems.MyConfig.Bind(
                "Immortal Shieldbow",
                "Cooldown",
                40f,
                "Cooldown of the barrier."
            );

            BarrierThreshold = LoLItems.MyConfig.Bind(
                "Immortal Shieldbow",
                "Health Threshold",
                30f,
                "Health threshold to trigger the barrier."
            );
        }

        private static void CreateItem()
        {
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();
            myItemDef.name = "ImmortalShieldbow";
            myItemDef.nameToken = "ImmortalShieldbow";
            myItemDef.pickupToken = "ImmortalShieldbowItem";
            myItemDef.descriptionToken = "ImmortalShieldbowDesc";
            myItemDef.loreToken = "ImmortalShieldbowLore";
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = LegacyResourcesAPI.Load<ItemTierDef>(Utilities.GetRarityFromString(Rarity.Value));
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = MyAssets.icons.LoadAsset<Sprite>("ImmortalShieldbowIcon");
#pragma warning disable CS0618
            myItemDef.pickupModelPrefab = MyAssets.prefabs.LoadAsset<GameObject>("ImmortalShieldbowPrefab");
#pragma warning restore CS0618
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = [ ItemTag.Healing ];
        }

        private static void CreateBuff()
        {
            myBuffDefCooldown = ScriptableObject.CreateInstance<BuffDef>();
            myBuffDefCooldown.iconSprite = MyAssets.icons.LoadAsset<Sprite>("ImmortalShieldbowIcon");
            myBuffDefCooldown.name = "Immortal Shieldbow Cooldown";
            myBuffDefCooldown.buffColor = Color.gray;
            myBuffDefCooldown.canStack = false;
            myBuffDefCooldown.isDebuff = true;
            myBuffDefCooldown.isCooldown = true;
            myBuffDefCooldown.isHidden = false;
        }


        private static void Hooks()
        {            
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        private static void RecalculateStatsAPI_GetStatCoefficients(CharacterBody characterBody, RecalculateStatsAPI.StatHookEventArgs args)
        {
            int count = characterBody?.inventory?.GetItemCountEffective(myItemDef.itemIndex) ?? 0;
            if (count > 0 && characterBody.healthComponent?.health < characterBody.healthComponent?.fullHealth * BarrierThreshold.Value / 100 && !characterBody.HasBuff(myBuffDefCooldown))
            {
                AkSoundEngine.PostEvent(procSoundEffect, characterBody.gameObject);
                if (!NetworkServer.active)
                    return;
                float barrierAmount = characterBody.healthComponent.fullHealth * BarrierPercent.Value / 100 * count;
                if (barrierAmount > characterBody.healthComponent.fullHealth)
                    barrierAmount = characterBody.healthComponent.fullHealth;
                characterBody.healthComponent.AddBarrier(barrierAmount);
                Utilities.AddTimedBuff(characterBody, myBuffDefCooldown, BuffCooldown.Value);
                Utilities.AddValueInDictionary(ref totalShieldGiven, characterBody.master, barrierAmount, totalShieldGivenToken, false);
            }
        }

        private static (string, string) GetDisplayInformation(CharacterMaster masterRef)
        {
            if (masterRef == null)
                return (Language.GetString(myItemDef.descriptionToken), "");
            
            string customDescription = "";

            if (totalShieldGiven.TryGetValue(masterRef.netId, out float barrierGiven))
                customDescription += "<br><br>Barrier given: " + string.Format("{0:#, ##0.##}", barrierGiven);
            else
                customDescription += "<br><br>Barrier given: 0";

            return (Language.GetString(myItemDef.descriptionToken), customDescription);
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("ImmortalShieldbow", "Immortal Shieldbow");

            // Short description
            LanguageAPI.Add("ImmortalShieldbowItem", "Gives a barrier when low on health.");

            // Long description
            LanguageAPI.Add("ImmortalShieldbowDesc", "Gives a barrier for <style=cIsHealth>" + BarrierPercent.Value + "%</style> <style=cStack>(+" + BarrierPercent.Value + "%)</style> of your max health when dropping below <style=cIsHealth>" + BarrierThreshold.Value + "%</style> max health. On a <style=cIsUtility>" + BuffCooldown.Value + "</style> second cooldown.");

            // Lore
            LanguageAPI.Add("ImmortalShieldbowLore", "Here to save you for when you mess up.");

            // ENABLE for buff
            LanguageAPI.Add("ImmortalShieldbowBuff", "Immortal Shieldbow is recharging.");
        }

        public static void SetupNetworkMappings()
        {
            LoLItems.networkMappings.Add(totalShieldGivenToken, totalShieldGiven);
        }
    }
}