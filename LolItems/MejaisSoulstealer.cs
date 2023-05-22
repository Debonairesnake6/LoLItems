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

namespace LoLItems
{
    internal class MejaisSoulstealer
    {
        public static ItemDef myItemDef;
        public static BuffDef currentStacks;
        public static BuffDef currentDuration;

        public static int bonusDamagePercent = 1;
        public static int maxStacks = 25;
        public static int duration = 3;
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> bonusDamageDealt = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();

        // This runs when loading the file
        internal static void Init()
        {
            CreateItem();
            CreateBuff();
            AddTokens();
            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            ContentAddition.AddBuffDef(currentStacks);
            ContentAddition.AddBuffDef(currentDuration);
            hooks();
        }

        private static void CreateItem()
        {
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();
            myItemDef.name = "MejaisSoulstealer";
            myItemDef.nameToken = "MejaisSoulstealer";
            myItemDef.pickupToken = "MejaisSoulstealerItem";
            myItemDef.descriptionToken = "MejaisSoulstealerDesc";
            myItemDef.loreToken = "MejaisSoulstealerLore";
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier1Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("MejaisSoulstealerIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("MejaisSoulstealerPrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
        }

        private static void CreateBuff()
        {
            currentStacks = ScriptableObject.CreateInstance<BuffDef>();
            currentStacks.iconSprite = Assets.icons.LoadAsset<Sprite>("MejaisSoulstealerIcon");
            currentStacks.name = "MejaisSoulstealerBuff";
            currentStacks.canStack = true;
            currentStacks.isDebuff = false;
            currentStacks.isCooldown = false;
            currentStacks.isHidden = false;

            currentDuration = ScriptableObject.CreateInstance<BuffDef>();
            currentDuration.name = "MejaisSoulstealerBuffDuration";
            currentDuration.canStack = false;
            currentDuration.isDebuff = false;
            currentDuration.isCooldown = true;
            currentDuration.isHidden = true;
        }


        private static void hooks()
        {
            // Do something on character death
            On.RoR2.GlobalEventManager.OnCharacterDeath += (orig, globalEventManager, damageReport) =>
            {
                orig(globalEventManager, damageReport);

                if (damageReport.attackerMaster?.inventory != null)
                {

                    int inventoryCount = damageReport.attackerMaster.inventory.GetItemCount(myItemDef.itemIndex);
					if (inventoryCount > 0)
					{
                        damageReport.attackerBody.AddTimedBuff(currentDuration, duration * inventoryCount);
                        if (damageReport.attackerBody.GetBuffCount(currentStacks) < maxStacks)
                        {
                            damageReport.attackerBody.AddBuff(currentStacks);
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
                        if (item.itemIndex == myItemDef.itemIndex && bonusDamageDealt.TryGetValue(self.targetMaster.netId, out float value)){
                            item.tooltipProvider.overrideBodyText =
                                Language.GetString(myItemDef.descriptionToken) + "<br><br>Extra damage dealt: " + String.Format("{0:#}", value);
                        }
                    });
#pragma warning restore Publicizer001
                }
            };

            // When something takes damage
            On.RoR2.HealthComponent.TakeDamage += (orig, self, damageInfo) =>
            {
                if (damageInfo.attacker)
                {
                    CharacterBody attackerCharacterBody = damageInfo.attacker.GetComponent<CharacterBody>();
                    
                    if (attackerCharacterBody?.inventory)
                    {
                        int buffCount = attackerCharacterBody.GetBuffCount(currentStacks.buffIndex);
                        if (buffCount > 0)
                        {
                            float extraDamage = damageInfo.damage * (buffCount * bonusDamagePercent) / 100f;
                            damageInfo.damage += extraDamage;
                            Utilities.AddValueInDictionary(ref bonusDamageDealt, attackerCharacterBody.master, extraDamage);
                        }
                    }
                }
                orig(self, damageInfo);
            };

            // Modify character values
            On.RoR2.CharacterBody.RecalculateStats += (orig, self) =>
            {
                if (self?.inventory && self.GetBuffCount(currentStacks) > 0 && self.GetBuffCount(currentDuration) == 0)
                {
                    while (self.GetBuffCount(currentStacks.buffIndex) > 0)
                    {
                        self.RemoveBuff(currentStacks);
                    }
                }
                orig(self);
            };
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("MejaisSoulstealer", "MejaisSoulstealer");

            // Short description
            LanguageAPI.Add("MejaisSoulstealerItem", "Killing enemies grants more damage for a short time");

            // Long description
            LanguageAPI.Add("MejaisSoulstealerDesc", "Killing an enemy grants a stack which gives <style=cIsDamage>" + bonusDamagePercent + 
            "%</style> bonus damage. Max <style=cIsUtility>" + maxStacks + 
            "</style> stacks, buff lasts for <style=cIsUtility>" + duration + "</style> <style=cStack>(+" + duration + ")</style> seconds.");

            // Lore
            LanguageAPI.Add("MejaisSoulstealerLore", "Your death note.");

            LanguageAPI.Add("MejaisSoulstealerBuff", "Mejais stacks");
            LanguageAPI.Add("MejaisSoulstealerBuffDuration", "Mejais duration remaining");
        }
    }
}