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

        public static float damageAmpPerStack = 30f;
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> bonusDamageDealt = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();

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
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier2Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("ImperialMandateIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("ImperialMandatePrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
        }

        private static void hooks()
        {
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
                                Language.GetString(myItemDef.descriptionToken) + "<br><br>Bonus damage dealt: " + String.Format("{0:#}", value);
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
                    CharacterBody victimCharacterBody = self.GetComponent<CharacterBody>();
                    
                    if (attackerCharacterBody?.inventory)
                    {
                        int inventoryCount = attackerCharacterBody.inventory.GetItemCount(myItemDef.itemIndex);
                        if (inventoryCount > 0)
                        {
                            BuffIndex[] slowBuffs = new BuffIndex[] { 
                                RoR2.RoR2Content.Buffs.Slow50.buffIndex, 
                                RoR2.RoR2Content.Buffs.Slow60.buffIndex, 
                                RoR2.RoR2Content.Buffs.Slow80.buffIndex,
                                RoR2.RoR2Content.Buffs.ClayGoo.buffIndex,
                                RoR2.RoR2Content.Buffs.Cripple.buffIndex,
                                RoR2.JunkContent.Buffs.Slow30.buffIndex,
                                RoR2.DLC1Content.Buffs.JailerSlow.buffIndex,
                                 };
                            float extraDamage = damageInfo.damage * damageAmpPerStack / 100 * inventoryCount;
                            if (slowBuffs.Any(wanted => victimCharacterBody.HasBuff(wanted)))
                            {
                                damageInfo.damage += extraDamage;
                                Utilities.AddValueInDictionary(ref bonusDamageDealt, attackerCharacterBody.master, extraDamage);
                            }
                        }
                    }
                }
                orig(self, damageInfo);
            };
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
            LanguageAPI.Add("ImperialMandateItem", "Do more damage to slowed enemies");

            // Long description
            LanguageAPI.Add("ImperialMandateDesc", "Do <style=cIsDamage>" + damageAmpPerStack + "%</style> <style=cStack>(+" + damageAmpPerStack + "%)</style> more damage to slowed enemies");

            // Lore
            LanguageAPI.Add("ImperialMandateLore", "Hunt your prey.");
        }
    }
}