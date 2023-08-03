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
    internal class ImperialMandate
    {
        public static ItemDef myItemDef;
        // ENABLE for buff
        // public static BuffDef myBuffDef;

        public static float damageAmpPerStack = 8f;
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> bonusDamageDealt = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = new Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster>();
        public static Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef = new Dictionary<RoR2.UI.ItemIcon, CharacterMaster>();

        // This runs when loading the file
        internal static void Init()
        {
            CreateItem();
            AddTokens();
            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            hooks();
        }

        private static void CreateItem()
        {
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();
            myItemDef.name = "ImperialMandate";
            myItemDef.nameToken = "ImperialMandate";
            myItemDef.pickupToken = "ImperialMandateItem";
            myItemDef.descriptionToken = "ImperialMandateDesc";
            myItemDef.loreToken = "ImperialMandateLore";
#pragma warning disable Publicizer001
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/DLC1/Common/VoidTier2Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("ImperialMandateIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("ImperialMandatePrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = new ItemTag[1] { ItemTag.Damage };
        }

        private static void hooks()
        {
            // Create void item
            On.RoR2.Items.ContagiousItemManager.Init += (orig) => 
            {
                List<ItemDef.Pair> newVoidPairs = new List<ItemDef.Pair>();
                ItemDef.Pair newVoidPair = new ItemDef.Pair()
                {
                    itemDef1 = ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex("DeathMark")),
                    itemDef2 = myItemDef
                };
                newVoidPairs.Add(newVoidPair);
#pragma warning disable Publicizer001
                ItemDef.Pair[] voidPairs = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem];
                ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = voidPairs.Union(newVoidPairs).ToArray();
#pragma warning restore Publicizer001
                orig();
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

            // When something takes damage
            On.RoR2.HealthComponent.TakeDamage += (orig, self, damageInfo) =>
            {
                if (damageInfo.attacker)
                {
                    CharacterBody attackerCharacterBody = damageInfo.attacker.GetComponent<CharacterBody>();
                    CharacterBody victimCharacterBody = self.GetComponent<CharacterBody>();
                    
                    if (attackerCharacterBody?.inventory)
                    {
                        int inventoryCount = attackerCharacterBody.inventory.GetItemCount(myItemDef.itemIndex);
                        if (inventoryCount > 0)
                        {
                            int debuffsActive = 0;
                            foreach (BuffIndex buffIndex in BuffCatalog.debuffBuffIndices)
                            {
                                if (victimCharacterBody.HasBuff(buffIndex)) debuffsActive += 1;
                            }
                            DotController dotController = DotController.FindDotController(victimCharacterBody.gameObject);
                            if (dotController)
                            {
                                for (DotController.DotIndex dotIndex = DotController.DotIndex.Bleed; dotIndex < DotController.DotIndex.Count; dotIndex++)
                                {
                                    if (dotController.HasDotActive(dotIndex)) debuffsActive += 1;
                                }
                            }
                            float extraDamage = damageInfo.damage * damageAmpPerStack / 100 * inventoryCount * debuffsActive;
                            damageInfo.damage += extraDamage;
                            Utilities.AddValueInDictionary(ref bonusDamageDealt, attackerCharacterBody.master, extraDamage);
                        }
                    }
                }
                orig(self, damageInfo);
            };
        }

        private static string GetDisplayInformation(CharacterMaster masterRef)
        {
            // Update the description for an item in the HUD
            if (masterRef != null && bonusDamageDealt.TryGetValue(masterRef.netId, out float damageDealt)){
                return Language.GetString(myItemDef.descriptionToken) + "<br><br>Damage dealt: " + String.Format("{0:#}", damageDealt);
            }
            return Language.GetString(myItemDef.descriptionToken);
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            // Styles
            // <style=cIsHealth>" + exampleValue + "</style>
            // <style=cIsDamage>" + exampleValue + "</style>
            // <style=cIsHealing>" + exampleValue + "</style>
            // <style=cIsUtility>" + exampleValue + "</style>
            // <style=cIsVoid>" + exampleValue + "</style>
            // <style=cHumanObjective>" + exampleValue + "</style>
            // <style=cLunarObjective>" + exampleValue + "</style>
            // <style=cStack>" + exampleValue + "</style>
            // <style=cWorldEvent>" + exampleValue + "</style>
            // <style=cArtifact>" + exampleValue + "</style>
            // <style=cUserSetting>" + exampleValue + "</style>
            // <style=cDeath>" + exampleValue + "</style>
            // <style=cSub>" + exampleValue + "</style>
            // <style=cMono>" + exampleValue + "</style>
            // <style=cShrine>" + exampleValue + "</style>
            // <style=cEvent>" + exampleValue + "</style>

            // Name of the item
            LanguageAPI.Add("ImperialMandate", "ImperialMandate");

            // Short description
            LanguageAPI.Add("ImperialMandateItem", "Do more damage to enemies for each debuff. Corrupts <style=cIsVoid>Death Mark</style>.");

            // Long description
            LanguageAPI.Add("ImperialMandateDesc", "Do <style=cIsDamage>" + damageAmpPerStack + "%</style> <style=cStack>(+" + damageAmpPerStack + "%)</style> more damage to enemies for each debuff. Corrupts <style=cIsVoid>Death Mark</style>.");

            // Lore
            LanguageAPI.Add("ImperialMandateLore", "Hunt your prey.");
        }
    }
}