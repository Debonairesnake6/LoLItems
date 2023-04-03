using System.Collections.Generic;
using BepInEx;
using R2API;
using R2API.Utils;
using RoR2.Orbs;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using ItemStats;
using System;

namespace LoLItems
{
    internal class Bork
    {

        //We need our item definition to persist through our functions, and therefore make it a class field.
        public static ItemDef myItemDef;

        public static BuffDef myCounterBuffDef;
        public static BuffDef myTimerBuffDef;

        // Set luck amount in one location
        public static float onHitDamageAmount = 0.5f;
        public static float procForBigHit = 5f;
        public static float bigOnHitMultiplier = 20f;
        public static float bigOnHitHealPercent = 20f;
        public static float bigOnHitTimer = 10f;

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

#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier2Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("BorkIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("BorkPrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
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
                            float damagePercentage = (1f - 100f / (100f + (inventoryCount * onHitDamageAmount / 100))) * 100;
                            DamageInfo onHitProc = damageInfo;
                            onHitProc.damage =  victimCharacterBody.maxHealth * damagePercentage;
                            onHitProc.crit = false;
                            onHitProc.procCoefficient = 0f;
                            onHitProc.damageType = DamageType.Generic;
                            onHitProc.damageColorIndex = DamageColorIndex.SuperBleed;
                            onHitProc.attacker = damageInfo.attacker;
                            onHitProc.inflictor = damageInfo.attacker;

                            victimCharacterBody.healthComponent.TakeDamage(onHitProc);
                            attackerCharacterBody.AddBorkDamage(onHitProc.damage);

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

                                    float bigOnHitDamage = onHitProc.damage * bigOnHitMultiplier;
                                    onHitProc.damage = bigOnHitDamage;
                                    onHitProc.damageColorIndex = DamageColorIndex.Nearby;
                                    victimCharacterBody.healthComponent.TakeDamage(onHitProc);
                                    attackerCharacterBody.AddBorkDamage(onHitProc.damage);

                                    float healAmount = bigOnHitDamage * (bigOnHitHealPercent / 100);
                                    attackerCharacterBody.healthComponent.Heal(healAmount, onHitProc.procChainMask);
                                    attackerCharacterBody.AddBorkHeal(healAmount);
                                }
                            }                            
                        }
                    }
                }
            };

            // Called basically every frame to update your HUD info
            On.RoR2.UI.HUD.Update += (orig, self) => 
            {
                orig(self);
                if (self.itemInventoryDisplay && self.targetMaster)
                {
#pragma warning disable Publicizer001
                    self.itemInventoryDisplay.itemIcons.ForEach(delegate(RoR2.UI.ItemIcon item)
                    {
                        // Update the description for an item in the HUD
                        if (item.itemIndex == myItemDef.itemIndex){
                            // ENABLE for description update
                            item.tooltipProvider.overrideBodyText =
                                Language.GetString(myItemDef.descriptionToken) + 
                                "<br><br>Bonus damage dealt: " + String.Format("{0:#}", self.targetMaster.GetBody().GetBorkDamage()) + 
                                "<br>Bonus healing: " + String.Format("{0:#}", self.targetMaster.GetBody().GetBorkHeal());
                        }
                    });
#pragma warning restore Publicizer001
                }
            };
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            //The Name should be self explanatory
            LanguageAPI.Add("Bork", "Bork");

            //The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("BorkItem", "% max hp damage on hit. 3rd hit is bigger, heals, and has a cooldown.");

            //The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("BorkDesc", "Adds <style=cIsDamage>" + onHitDamageAmount + "%</style> max enemy hp on hit. Every " + procForBigHit + " hits deals <style=cIsUtility>" + bigOnHitMultiplier + "x</style> damage, and heals the attacker for <style=cIsHealing>" + bigOnHitHealPercent + "%</style> of that damage on a " + bigOnHitTimer + " second cooldown.");

            //The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("BorkLore", "Viego is a plague to everything he touches.");
        }
    }
}

namespace RoR2
{
    // ENABLE for customer character stats
    public static class CharacterBodyExtensionBork
    {
        public static float borkBonusDamage = 0f;
        public static float borkBonusHeal = 0f;
        public static void AddBorkDamage(this CharacterBody characterBody, float damage)
        {
            borkBonusDamage += damage;
        }

        public static float GetBorkDamage(this CharacterBody characterBody)
        {
            return borkBonusDamage;
        }

        public static void AddBorkHeal(this CharacterBody characterBody, float heal)
        {
            borkBonusHeal += heal;
        }

        public static float GetBorkHeal(this CharacterBody characterBody)
        {
            return borkBonusHeal;
        }
    }
}