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

namespace LoLItems
{
    internal class ImmortalShieldbow
    {
        public static ItemDef myItemDef;
        public static BuffDef myBuffDefCooldown;

        public static float barrierPercent = 40f;
        public static float buffCooldown = 40f;
        public static float barrierThreshold = 30f;
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> totalShieldGiven = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = new Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster>();
        public static Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef = new Dictionary<RoR2.UI.ItemIcon, CharacterMaster>();

        // This runs when loading the file
        internal static void Init()
        {
            CreateItem();
            CreateBuff();
            AddTokens();
            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            ContentAddition.AddBuffDef(myBuffDefCooldown);
            hooks();
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
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier2Def.asset").WaitForCompletion();
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

            // Called basically every frame to update your HUD info
            On.RoR2.UI.HUD.Update += (orig, self) => 
            {
                orig(self);
                if (self.itemInventoryDisplay && self.targetMaster)
                {
                    DisplayToMasterRef[self.itemInventoryDisplay] = self.targetMaster;
#pragma warning disable Publicizer001
                    self.itemInventoryDisplay.itemIcons.ForEach(delegate(RoR2.UI.ItemIcon item)
                    {
                        // Update the description for an item in the HUD
                        if (item.itemIndex == myItemDef.itemIndex){
                            item.tooltipProvider.overrideBodyText = GetDisplayInformation(self.targetMaster);
                        }
                    });
#pragma warning restore Publicizer001
                }
            };

            // Open Scoreboard
            On.RoR2.UI.ScoreboardStrip.SetMaster += (orig, self, characterMaster) =>
            {
                orig(self, characterMaster);
                if (characterMaster) DisplayToMasterRef[self.itemInventoryDisplay] = characterMaster;
            };


            // Open Scoreboard
            On.RoR2.UI.ItemIcon.SetItemIndex += (orig, self, newIndex, newCount) =>
            {
                orig(self, newIndex, newCount);
                if (self.tooltipProvider != null && newIndex == myItemDef.itemIndex)
                {
                    IconToMasterRef.TryGetValue(self, out CharacterMaster master);
                    self.tooltipProvider.overrideBodyText = GetDisplayInformation(master);
                }
            };

            // Open Scoreboard
            On.RoR2.UI.ItemInventoryDisplay.AllocateIcons += (orig, self, count) =>
            {
                orig(self, count);
                List<RoR2.UI.ItemIcon> icons = self.GetFieldValue<List<RoR2.UI.ItemIcon>>("itemIcons");
                DisplayToMasterRef.TryGetValue(self, out CharacterMaster masterRef);
                icons.ForEach(i => IconToMasterRef[i] = masterRef);
            };

            // Add to stat dict for end of game screen
            On.RoR2.UI.GameEndReportPanelController.SetPlayerInfo += (orig, self, playerInfo) => 
            {
                orig(self, playerInfo);
                Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRefCopy = new Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster>(DisplayToMasterRef);
                foreach(KeyValuePair<RoR2.UI.ItemInventoryDisplay, CharacterMaster> entry in DisplayToMasterRefCopy)
                {
                    if (entry.Value == playerInfo.master)
                    {
                        DisplayToMasterRef[self.itemInventoryDisplay] = playerInfo.master;
                    }
                }
            };
            
            // Modify character values
            On.RoR2.CharacterBody.RecalculateStats += (orig, self) =>
            {
                if (self?.inventory && self.inventory.GetItemCount(myItemDef.itemIndex) > 0 && self.healthComponent?.health < self.healthComponent?.fullHealth * barrierThreshold / 100 && !self.HasBuff(myBuffDefCooldown))
                {
                    float barrierAmount = self.healthComponent.fullHealth * barrierPercent / 100 * self.inventory.GetItemCount(myItemDef.itemIndex);
                    if (barrierAmount > self.healthComponent.fullHealth) { barrierAmount = self.healthComponent.fullHealth; }
                    self.healthComponent.AddBarrier(barrierAmount);
                    Utilities.AddTimedBuff(self, myBuffDefCooldown, buffCooldown);
                    Utilities.AddValueInDictionary(ref totalShieldGiven, self.master, barrierAmount, false);
                }
                orig(self);
            };
        }

        private static string GetDisplayInformation(CharacterMaster masterRef)
        {
            // Update the description for an item in the HUD
            if (masterRef != null && totalShieldGiven.TryGetValue(masterRef.netId, out float barrierGiven)){
                return Language.GetString(myItemDef.descriptionToken) + "<br><br>Barrier given: " + String.Format("{0:#}", barrierGiven);
            }
            return Language.GetString(myItemDef.descriptionToken);
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("ImmortalShieldbow", "ImmortalShieldbow");

            // Short description
            LanguageAPI.Add("ImmortalShieldbowItem", "Gives a barrier when low on health.");

            // Long description
            LanguageAPI.Add("ImmortalShieldbowDesc", "Gives a barrier for <style=cIsHealth>" + barrierPercent + "%</style> <style=cStack>(+" + barrierPercent + "%)</style> of your max health when dropping below <style=cIsHealth>" + barrierThreshold + "%</style> max health. On a <style=cIsUtility>" + buffCooldown + "</style> second cooldown.");

            // Lore
            LanguageAPI.Add("ImmortalShieldbowLore", "Here to save you for when you mess up.");

            // ENABLE for buff
            LanguageAPI.Add("ImmortalShieldbowBuff", "ImmortalShieldbow is recharging.");
        }
    }
}