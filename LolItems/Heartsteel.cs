using System.Data;
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
using System.Collections;

namespace LoLItems
{
    internal class Heartsteel
    {

        //We need our item definition to persist through our functions, and therefore make it a class field.
        public static ItemDef myItemDef;

        // public static BuffDef myBuffDef;

        // Set luck amount in one location
        public static float bonusHealthAmount = 2f;
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> heartsteelHealth = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> originalBaseMaxHealth = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = new Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster>();
        public static Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef = new Dictionary<RoR2.UI.ItemIcon, CharacterMaster>();

        internal static void Init()
        {
            //Generate the basic information for the item
            CreateItem();
            // CreateBuff();

            //Now let's turn the tokens we made into actual strings for the game:
            AddTokens();

            //You can add your own display rules here, where the first argument passed are the default display rules: the ones used when no specific display rules for a character are found.
            //For this example, we are omitting them, as they are quite a pain to set up without tools like ItemDisplayPlacementHelper
            var displayRules = new ItemDisplayRuleDict(null);

            //Then finally add it to R2API
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));

            // Initialize the hooks
            hooks();
        }

        private static void CreateItem()
        {
            //First let's define our item
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();

            // Language Tokens, check AddTokens() below.
            myItemDef.name = "Heartsteel";
            myItemDef.nameToken = "Heartsteel";
            myItemDef.pickupToken = "HeartsteelItem";
            myItemDef.descriptionToken = "HeartsteelDesc";
            myItemDef.loreToken = "HeartsteelLore";
#pragma warning disable Publicizer001
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("HeartsteelIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("HeartsteelPrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = new ItemTag[2] { ItemTag.Healing, ItemTag.OnKillEffect };
        }


        private static void hooks()
        {
            On.RoR2.GlobalEventManager.OnCharacterDeath += (orig, globalEventManager, damageReport) =>
            {
                orig(globalEventManager, damageReport);

                GameObject gameObject = null;
                Transform transform = null;
                Vector3 vector = Vector3.zero;

                if (damageReport.victim)
                {
                    gameObject = damageReport.victim.gameObject;
                    transform = gameObject.transform;
                    vector = transform.position;
                }

                if (damageReport.attackerMaster?.inventory != null)
                {
                    int inventoryCount = damageReport.attackerMaster.inventory.GetItemCount(myItemDef.itemIndex);
					if (inventoryCount > 0)
					{
                        InfusionOrb HeartsteelOrb = new InfusionOrb();
                        HeartsteelOrb.origin = vector;
                        HeartsteelOrb.target = Util.FindBodyMainHurtBox(damageReport.attackerBody);
                        HeartsteelOrb.maxHpValue = 0;
                        OrbManager.instance.AddOrb(HeartsteelOrb);

                        Utilities.AddValueInDictionary(ref heartsteelHealth, damageReport.attackerMaster, bonusHealthAmount * inventoryCount);
					}
                }
            };

            On.RoR2.CharacterBody.Start += (orig, self) =>
            {
                orig(self);
                if (self.master != null && !originalBaseMaxHealth.ContainsKey(self.master.netId))
                {
                    Utilities.SetValueInDictionary(ref originalBaseMaxHealth, self.master, self.baseMaxHealth, false);
                }
            };
            
            On.RoR2.CharacterBody.RecalculateStats += (orig, self) =>
            {
                if (self.inventory != null && self.inventory.GetItemCount(myItemDef.itemIndex) > 0 && originalBaseMaxHealth.TryGetValue(self.master.netId, out float baseHealth) && heartsteelHealth.TryGetValue(self.master.netId, out float extraHealth))
                {
                    self.baseMaxHealth = baseHealth + extraHealth;
                }
                orig(self);
            };

            // Called basically every frame            
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
        }

        private static string GetDisplayInformation(CharacterMaster masterRef)
        {
            // Update the description for an item in the HUD
            if (masterRef != null && heartsteelHealth.TryGetValue(masterRef.netId, out float damageDealt)){
                return Language.GetString(myItemDef.descriptionToken) + "<br><br>Health gained: " + String.Format("{0:#}", damageDealt);
            }
            return Language.GetString(myItemDef.descriptionToken);
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            //The Name should be self explanatory
            LanguageAPI.Add("Heartsteel", "Heartsteel");

            //The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("HeartsteelItem", "Gain permanent health on kill. No Cap.");

            //The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("HeartsteelDesc", "Adds <style=cIsHealth>" + bonusHealthAmount + "</style> <style=cStack>(+" + bonusHealthAmount + ")</style> base health per kill. No Cap.");

            //The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("HeartsteelLore", "Lore was meant to go here, but Sion trampled it.");

            LanguageAPI.Add("HeartsteelBuff", "Health gained = <style=cIsHealth>" + bonusHealthAmount + "</style><style=cStack> x </style>stacks");
        }
    }
}