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
    internal class KrakenSlayer
    {

        //We need our item definition to persist through our functions, and therefore make it a class field.
        public static ItemDef myItemDef;

        public static BuffDef myCounterBuffDef;
        public static int procRequirement = 3;
        public static float procDamage = 20f;
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> bonusDamage = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = new Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster>();
        public static Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef = new Dictionary<RoR2.UI.ItemIcon, CharacterMaster>();

        internal static void Init()
        {
            //Generate the basic information for the item
            CreateItem();
            CreateBuff();

            //Now let's turn the tokens we made into actual strings for the game:
            AddTokens();

            //You can add your own display rules here, where the first argument passed are the default display rules: the ones used when no specific display rules for a character are found.
            //For this example, we are omitting them, as they are quite a pain to set up without tools like ItemDisplayPlacementHelper
            var displayRules = new ItemDisplayRuleDict(null);

            //Then finally add it to R2API
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            ContentAddition.AddBuffDef(myCounterBuffDef);

            // Initialize the hooks
            hooks();
        }

        private static void CreateItem()
        {
            //First let's define our item
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();

            // Language Tokens, check AddTokens() below.
            myItemDef.name = "KrakenSlayer";
            myItemDef.nameToken = "KrakenSlayer";
            myItemDef.pickupToken = "KrakenSlayerItem";
            myItemDef.descriptionToken = "KrakenSlayerDesc";
            myItemDef.loreToken = "KrakenSlayerLore";
#pragma warning disable Publicizer001
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier2Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("KrakenSlayerIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("KrakenSlayerPrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = new ItemTag[1] { ItemTag.Damage };
        }

        private static void CreateBuff()
        {
            // Create a buff to count the number of stacks before a big proc
            myCounterBuffDef = ScriptableObject.CreateInstance<BuffDef>();

            myCounterBuffDef.iconSprite = Assets.icons.LoadAsset<Sprite>("KrakenSlayerIcon");
            myCounterBuffDef.name = "KrakenSlayerCounterBuff";
            myCounterBuffDef.canStack = true;
            myCounterBuffDef.isDebuff = false;
            myCounterBuffDef.isCooldown = false;
            myCounterBuffDef.isHidden = false;
        }


        private static void hooks()
        {
            On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, victim) =>
            {
                orig(self, damageInfo, victim);

                
                if (damageInfo.attacker)
                {
                    CharacterBody attackerCharacterBody = damageInfo.attacker.GetComponent<CharacterBody>();
                    CharacterBody victimCharacterBody = victim.GetComponent<CharacterBody>();
                    
                    if (attackerCharacterBody?.inventory)
                    {
                        int inventoryCount = attackerCharacterBody.inventory.GetItemCount(myItemDef.itemIndex);
                        if (inventoryCount > 0 && damageInfo.procCoefficient > 0)
                        {
                            attackerCharacterBody.AddBuff(myCounterBuffDef);

                            if (attackerCharacterBody.healthComponent.body.GetBuffCount(myCounterBuffDef) > procRequirement)
                            {
                                foreach (int value in Enumerable.Range(2, procRequirement))
                                {
                                    attackerCharacterBody.RemoveBuff(myCounterBuffDef);
                                }

                                float damage = attackerCharacterBody.damage * procDamage / 100f * inventoryCount;
                                DamageInfo onHitProc = damageInfo;
                                onHitProc.crit = false;
                                onHitProc.procCoefficient = 0f;
                                onHitProc.damageType = DamageType.Generic;
                                onHitProc.inflictor = damageInfo.attacker;
                                onHitProc.damage = damage;
                                onHitProc.damageColorIndex = DamageColorIndex.Item;
                                victimCharacterBody.healthComponent.TakeDamage(onHitProc);
                                Utilities.AddValueInDictionary(ref bonusDamage, attackerCharacterBody.master, damage);
                            }
                        }
                    }
                }
            };

            // Modify character values
            On.RoR2.CharacterBody.RecalculateStats += (orig, self) =>
            {
                orig(self);
                if (self?.inventory && self.inventory.GetItemCount(myItemDef.itemIndex) > 0 && !self.HasBuff(myCounterBuffDef))
                {
                    self.AddBuff(myCounterBuffDef);
                }
                else if (self?.inventory && self.inventory.GetItemCount(myItemDef.itemIndex) == 0 && self.HasBuff(myCounterBuffDef))
                {
                    Utilities.RemoveBuffStacks(self, myCounterBuffDef.buffIndex);
                }
            };

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
        }

        private static string GetDisplayInformation(CharacterMaster masterRef)
        {
            // Update the description for an item in the HUD
            if (masterRef != null && bonusDamage.TryGetValue(masterRef.netId, out float damageDealt)){
                return Language.GetString(myItemDef.descriptionToken) + 
                "<br><br>Bonus damage dealt: " + String.Format("{0:#}", damageDealt);
            }
            return Language.GetString(myItemDef.descriptionToken);
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            //The Name should be self explanatory
            LanguageAPI.Add("KrakenSlayer", "KrakenSlayer");

            //The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("KrakenSlayerItem", "Every " + procRequirement + " hits do bonus damage.");

            //The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("KrakenSlayerDesc", "Every " + procRequirement + " hits do an extra <style=cIsDamage>" + procDamage + "%</style> <style=cStack>(+" + procDamage + "%)</style> base damage.");

            //The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("KrakenSlayerLore", "Legend has it that this item is no longer mythical.");
        }
    }
}