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
    internal class ImmortalShieldbow
    {
        public static ItemDef myItemDef;
        public static BuffDef myBuffDefCooldown;
        public static ConfigEntry<float> barrierPercent { get; set; }
        public static ConfigEntry<float> buffCooldown { get; set; }
        public static ConfigEntry<float> barrierThreshold { get; set; }
        public static ConfigEntry<bool> enabled { get; set; }
        public static ConfigEntry<string> rarity { get; set; }
        public static ConfigEntry<string> voidItems { get; set; }
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> totalShieldGiven = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static string totalShieldGivenToken = "ImmortalShieldbow.totalShieldGiven";
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = new Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster>();
        public static Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef = new Dictionary<RoR2.UI.ItemIcon, CharacterMaster>();
        public static uint procSoundEffect = 2060112413;

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
            AddTokens();
            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            ContentAddition.AddBuffDef(myBuffDefCooldown);
            hooks();
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, rarity, voidItems, "ImmortalShieldbow");
            SetupNetworkMappings();
        }

        private static void LoadConfig()
        {
            enabled = LoLItems.MyConfig.Bind<bool>(
                "ImmortalShieldbow",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            rarity = LoLItems.MyConfig.Bind<string>(
                "ImmortalShieldbow",
                "Rarity",
                "Tier2Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            voidItems = LoLItems.MyConfig.Bind<string>(
                "ImmortalShieldbow",
                "Void Items",
                "",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            barrierPercent = LoLItems.MyConfig.Bind<float>(
                "ImmortalShieldbow",
                "Barrier Percent",
                40f,
                "Amount of percent max health barrier each item will grant."

            );

            buffCooldown = LoLItems.MyConfig.Bind<float>(
                "ImmortalShieldbow",
                "Cooldown",
                40f,
                "Cooldown of the barrier."

            );

            barrierThreshold = LoLItems.MyConfig.Bind<float>(
                "ImmortalShieldbow",
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
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(Utilities.GetRarityFromString(rarity.Value)).WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("ImmortalShieldbowIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("ImmortalShieldbowPrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = new ItemTag[1] { ItemTag.Healing };
        }

        private static void CreateBuff()
        {
            myBuffDefCooldown = ScriptableObject.CreateInstance<BuffDef>();
            myBuffDefCooldown.iconSprite = Assets.icons.LoadAsset<Sprite>("ImmortalShieldbowIcon");
            myBuffDefCooldown.name = "ImmortalShieldbowBuffCooldown";
            myBuffDefCooldown.buffColor = Color.gray;
            myBuffDefCooldown.canStack = false;
            myBuffDefCooldown.isDebuff = true;
            myBuffDefCooldown.isCooldown = true;
            myBuffDefCooldown.isHidden = false;
        }


        private static void hooks()
        {            
            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        private static void RecalculateStatsAPI_GetStatCoefficients(CharacterBody characterBody, RecalculateStatsAPI.StatHookEventArgs args)
        {
            int count = characterBody?.inventory?.GetItemCount(myItemDef.itemIndex) ?? 0;
            if (count > 0 && characterBody.healthComponent?.health < characterBody.healthComponent?.fullHealth * barrierThreshold.Value / 100 && !characterBody.HasBuff(myBuffDefCooldown))
            {
                AkSoundEngine.PostEvent(procSoundEffect, characterBody.gameObject);
                if (!UnityEngine.Networking.NetworkServer.active)
                    return;
                float barrierAmount = characterBody.healthComponent.fullHealth * barrierPercent.Value / 100 * count;
                if (barrierAmount > characterBody.healthComponent.fullHealth)
                    barrierAmount = characterBody.healthComponent.fullHealth;
                characterBody.healthComponent.AddBarrier(barrierAmount);
                Utilities.AddTimedBuff(characterBody, myBuffDefCooldown, buffCooldown.Value);
                Utilities.AddValueInDictionary(ref totalShieldGiven, characterBody.master, barrierAmount, totalShieldGivenToken, false);
            }
        }

        private static (string, string) GetDisplayInformation(CharacterMaster masterRef)
        {
            if (masterRef == null)
                return (Language.GetString(myItemDef.descriptionToken), "");
            
            string customDescription = "";

            if (totalShieldGiven.TryGetValue(masterRef.netId, out float barrierGiven))
                customDescription += "<br><br>Barrier given: " + String.Format("{0:#}", barrierGiven);
            else
                customDescription += "<br><br>Barrier given: 0";

            return (Language.GetString(myItemDef.descriptionToken), customDescription);
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("ImmortalShieldbow", "ImmortalShieldbow");

            // Short description
            LanguageAPI.Add("ImmortalShieldbowItem", "Gives a barrier when low on health.");

            // Long description
            LanguageAPI.Add("ImmortalShieldbowDesc", "Gives a barrier for <style=cIsHealth>" + barrierPercent.Value + "%</style> <style=cStack>(+" + barrierPercent.Value + "%)</style> of your max health when dropping below <style=cIsHealth>" + barrierThreshold.Value + "%</style> max health. On a <style=cIsUtility>" + buffCooldown.Value + "</style> second cooldown.");

            // Lore
            LanguageAPI.Add("ImmortalShieldbowLore", "Here to save you for when you mess up.");

            // ENABLE for buff
            LanguageAPI.Add("ImmortalShieldbowBuff", "ImmortalShieldbow is recharging.");
        }

        public static void SetupNetworkMappings()
        {
            LoLItems.networkMappings.Add(totalShieldGivenToken, totalShieldGiven);
        }
    }
}