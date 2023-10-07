using System.IO;
using System.Text.RegularExpressions;
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
    internal class GuardiansBlade
    {
        public static ItemDef myItemDef;
        public static float cooldownReduction = 5f;
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> cooldownReductionTracker = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = new Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster>();
        public static Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef = new Dictionary<RoR2.UI.ItemIcon, CharacterMaster>();

        // This runs when loading the file
        internal static void Init()
        {
            CreateItem();
            AddTokens();
            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            hooks();
        }

        private static void CreateItem()
        {
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();
            myItemDef.name = "GuardiansBlade";
            myItemDef.nameToken = "GuardiansBlade";
            myItemDef.pickupToken = "GuardiansBladeItem";
            myItemDef.descriptionToken = "GuardiansBladeDesc";
            myItemDef.loreToken = "GuardiansBladeLore";
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier1Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("GuardiansBladeIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("GuardiansBladePrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = new ItemTag[1] { ItemTag.Utility };
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
            
            On.RoR2.CharacterBody.RecalculateStats += (orig, self) =>
            {
                orig(self);
                if (self.inventory != null && self.inventory.GetItemCount(myItemDef.itemIndex) > 0)
                {
                    float cdr = Math.Abs(Utilities.HyperbolicScale(self.inventory.GetItemCount(myItemDef.itemIndex), cooldownReduction / 100) - 1);
                    self.skillLocator.utilityBonusStockSkill.cooldownScale *= cdr;
                    self.skillLocator.secondaryBonusStockSkill.cooldownScale *= cdr;
                    Utilities.SetValueInDictionary(ref cooldownReductionTracker, self.master, Math.Abs(cdr - 1) * 100, false);
                }
            };

        }

        private static string GetDisplayInformation(CharacterMaster masterRef)
        {
            // Update the description for an item in the HUD
            if (masterRef != null && cooldownReductionTracker.TryGetValue(masterRef.netId, out float cdr)){
                return Language.GetString(myItemDef.descriptionToken) + "<br><br>Cooldown reduction: " + String.Format("{0:F1}", cdr);
            }
            return Language.GetString(myItemDef.descriptionToken);
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("GuardiansBlade", "GuardiansBlade");

            // Short description
            LanguageAPI.Add("GuardiansBladeItem", "Reduce the cooldown on secondary and utility skills");

            // Long description
            LanguageAPI.Add("GuardiansBladeDesc", "Reduce the cooldown on your secondary and utility skills by <style=cIsUtility>" + cooldownReduction + "%</style> <style=cStack>(+" + cooldownReduction + ")</style>. Scales hyperbolically, just like tougher times.");

            // Lore
            LanguageAPI.Add("GuardiansBladeLore", "Awesome Refund And Movement.");
        }
    }
}