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
    internal class BannerOfCommand
    {
        public static ItemDef myItemDef;

        public static float damagePercentAmp = 20f;
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
            myItemDef.name = "BannerOfCommand";
            myItemDef.nameToken = "BannerOfCommand";
            myItemDef.pickupToken = "BannerOfCommandItem";
            myItemDef.descriptionToken = "BannerOfCommandDesc";
            myItemDef.loreToken = "BannerOfCommandLore";
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier1Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("BannerOfCommandIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("BannerOfCommandPrefab");
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
                            // ENABLE for description update
                            item.tooltipProvider.overrideBodyText =
                                Language.GetString(myItemDef.descriptionToken) + "<br><br>Bonus damage: " + String.Format("{0:#}", value);
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
                    CharacterMaster ownerMaster = attackerCharacterBody?.master?.minionOwnership?.ownerMaster;
                    
                    if (ownerMaster?.inventory)
                    {
                        int inventoryCount = ownerMaster.inventory.GetItemCount(myItemDef.itemIndex);
                        if (inventoryCount > 0)
                        {
                            float extraDamage = 1 + (inventoryCount * damagePercentAmp / 100);
                            Utilities.AddValueToDictionary(ref bonusDamageDealt, ownerMaster, extraDamage * damageInfo.damage);
                            damageInfo.damage *= extraDamage;
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
            LanguageAPI.Add("BannerOfCommand", "BannerOfCommand");

            // Short description
            LanguageAPI.Add("BannerOfCommandItem", "Increase allied minion damage");

            // Long description
            LanguageAPI.Add("BannerOfCommandDesc", "Increase the damage of allied minions by <style=cIsUtility>" + damagePercentAmp + "%</style> <style=cStack>(+" + damagePercentAmp + "%)</style>");

            // Lore
            LanguageAPI.Add("BannerOfCommandLore", "Split pushing is boring.");
        }
    }
}