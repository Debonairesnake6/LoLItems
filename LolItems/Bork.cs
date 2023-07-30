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
    internal class Bork
    {

        //We need our item definition to persist through our functions, and therefore make it a class field.
        public static ItemDef myItemDef;

        public static BuffDef myCounterBuffDef;
        public static BuffDef myTimerBuffDef;

        // Set luck amount in one location
        public static float onHitDamageAmount = 5f;
        public static float procForBigHit = 5f;
        public static float onHitHealPercent = 20f;
        public static float bigOnHitTimer = 10f;
        public static float procDamageMin = 2f;
        public static int procDamageMax = 25;
        public static float attackSpeed = 5f;
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> borkBonusDamage = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> borkBonusHeal = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> borkAtkSpd = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> originalAtkSpd = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
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
            ContentAddition.AddBuffDef(myTimerBuffDef);

            // Initialize the hooks
            hooks();
        }

        private static void CreateItem()
        {
            //First let's define our item
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();

            // Language Tokens, check AddTokens() below.
            myItemDef.name = "Bork";
            myItemDef.nameToken = "Bork";
            myItemDef.pickupToken = "BorkItem";
            myItemDef.descriptionToken = "BorkDesc";
            myItemDef.loreToken = "BorkLore";
            myItemDef.deprecatedTier = ItemTier.VoidTier2;
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("BorkIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("BorkPrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = new ItemTag[2] { ItemTag.Damage, ItemTag.Healing };
        }

        private static void CreateBuff()
        {
            // Create a buff to count the number of stacks before a big proc
            myCounterBuffDef = ScriptableObject.CreateInstance<BuffDef>();

            myCounterBuffDef.iconSprite = Resources.Load<Sprite>("Textures/MiscIcons/texMysteryIcon");
            myCounterBuffDef.name = "BorkCounterBuff";
            myCounterBuffDef.buffColor = Color.green;
            myCounterBuffDef.canStack = true;
            myCounterBuffDef.isDebuff = false;
            myCounterBuffDef.isCooldown = false;
            myCounterBuffDef.isHidden = true;

            // Create a timer to prevent stacks for a short period of time
            myTimerBuffDef = ScriptableObject.CreateInstance<BuffDef>();

            myTimerBuffDef.iconSprite = Resources.Load<Sprite>("Textures/MiscIcons/texMysteryIcon");
            myTimerBuffDef.name = "BorkTimerBuff";
            myTimerBuffDef.buffColor = Color.green;
            myTimerBuffDef.canStack = true;
            myTimerBuffDef.isDebuff = false;
            myTimerBuffDef.isCooldown = true;
            myTimerBuffDef.isHidden = true;
        }


        private static void hooks()
        {
            // Create void item
            On.RoR2.Items.ContagiousItemManager.Init += (orig) => 
            {
                List<ItemDef.Pair> newVoidPairs = new List<ItemDef.Pair>();
                foreach(string itemName in new List<string> { "Syringe", "Seed" })
                {
                    ItemDef.Pair newVoidPair = new ItemDef.Pair()
                {
                    itemDef1 = ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex(itemName)),
                    itemDef2 = myItemDef
                };
                newVoidPairs.Add(newVoidPair);
                }
                ItemDef.Pair[] voidPairs = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem];
                ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = voidPairs.Union(newVoidPairs).ToArray();
                orig();
            };

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

                            if (!victimCharacterBody.HasBuff(myTimerBuffDef))
                            {
                                int currentBuffCount = victimCharacterBody.healthComponent.body.GetBuffCount(myCounterBuffDef);
                                if (currentBuffCount < procForBigHit - 1)
                                {
                                    victimCharacterBody.healthComponent.body.AddBuff(myCounterBuffDef);
                                }
                                else
                                {
                                    victimCharacterBody.healthComponent.body.RemoveBuff(myCounterBuffDef);
                                    int myTimer = 1;
                                    while ((float)myTimer <= bigOnHitTimer)
                                    {
                                        victimCharacterBody.healthComponent.body.AddTimedBuff(myTimerBuffDef, myTimer);
                                        myTimer++;
                                    }

                                    float damage = victimCharacterBody.healthComponent.health * inventoryCount * onHitDamageAmount / 100 * damageInfo.procCoefficient;
                                    damage = Math.Max(procDamageMin * attackerCharacterBody.damage, Math.Min(procDamageMax * attackerCharacterBody.damage, damage));
                                    DamageInfo onHitProc = damageInfo;
                                    onHitProc.crit = false;
                                    onHitProc.procCoefficient = 0f;
                                    onHitProc.damageType = DamageType.Generic;
                                    onHitProc.inflictor = damageInfo.attacker;
                                    onHitProc.damage = damage;
                                    onHitProc.damageColorIndex = DamageColorIndex.Nearby;
                                    victimCharacterBody.healthComponent.TakeDamage(onHitProc);
                                    Utilities.AddValueInDictionary(ref borkBonusDamage, attackerCharacterBody.master, damage);

                                    float healAmount = damage * (onHitHealPercent / 100);
                                    attackerCharacterBody.healthComponent.Heal(healAmount, onHitProc.procChainMask);
                                    Utilities.AddValueInDictionary(ref borkBonusHeal, attackerCharacterBody.master, healAmount);
                                }
                            }                            
                        }
                    }
                }
            };

            On.RoR2.CharacterBody.Start += (orig, self) =>
            {
                orig(self);
                if (self.master != null && !originalAtkSpd.ContainsKey(self.master.netId))
                {
                    Utilities.SetValueInDictionary(ref originalAtkSpd, self.master, self.baseAttackSpeed, false);
                }
            };

            On.RoR2.CharacterBody.RecalculateStats += (orig, self) =>
            {
                if (self.inventory != null && self.inventory.GetItemCount(myItemDef.itemIndex) > 0 && originalAtkSpd.TryGetValue(self.master.netId, out float baseAtkSpd))
                {
                    self.baseAttackSpeed = baseAtkSpd + self.inventory.GetItemCount(myItemDef) / 100f  * attackSpeed;
                }
                orig(self);
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
            if (masterRef != null && borkBonusDamage.TryGetValue(masterRef.netId, out float damageDealt) && borkBonusHeal.TryGetValue(masterRef.netId, out float healingDone)){
                return Language.GetString(myItemDef.descriptionToken) + 
                "<br><br>Bonus damage dealt: " + String.Format("{0:#}", damageDealt) + 
                "<br>Bonus healing: " + String.Format("{0:#}", healingDone);
            }
            return Language.GetString(myItemDef.descriptionToken);
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            //The Name should be self explanatory
            LanguageAPI.Add("Bork", "Bork");

            //The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("BorkItem", "Attack speed. Every " + procForBigHit + " hits do damage and heal, and has a cooldown. Corrupts <style=cIsVoid>Syringes</style> and <style=cIsVoid>Leaching Seeds</style>.");

            //The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("BorkDesc", "Deals <style=cIsDamage>" + onHitDamageAmount + "%</style> <style=cStack>(+" + onHitDamageAmount + 
            "%)</style> current enemy hp every third hit, and heal for <style=cIsHealing>" + onHitHealPercent + "%</style> of that damage on a " + bigOnHitTimer + 
            " second cooldown. Corrupts <style=cIsVoid>Syringes</style> and <style=cIsVoid>Leaching Seeds</style>.");

            //The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("BorkLore", "Viego is a plague to everything he touches.");
        }
    }
}