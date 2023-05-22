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
    internal class InfinityEdge
    {
        public static ItemDef myItemDef;

        public static float bonusCritChance = 5f;
        public static float bonusCritDamage = 15f;
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> bonusDamageDealt = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();

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
            myItemDef.name = "InfinityEdge";
            myItemDef.nameToken = "InfinityEdge";
            myItemDef.pickupToken = "InfinityEdgeItem";
            myItemDef.descriptionToken = "InfinityEdgeDesc";
            myItemDef.loreToken = "InfinityEdgeLore";
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier2Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("InfinityEdgeIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("InfinityEdgePrefab");
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
                        float damageDealt = bonusDamageDealt.TryGetValue(self.targetMaster.netId, out float _) ? bonusDamageDealt[self.targetMaster.netId] : 0f;
                        if (item.itemIndex == myItemDef.itemIndex){
#pragma warning restore Publicizer001
                            item.tooltipProvider.overrideBodyText =
                                Language.GetString(myItemDef.descriptionToken) 
                                + "<br><br>Bonus crit chance: " + String.Format("{0:#}", self.targetMaster.inventory.GetItemCount(myItemDef.itemIndex) * bonusCritChance)
                                + "<br>Bonus damage dealt: " + String.Format("{0:#}", damageDealt);
                        }
                    });
                }
            };
            
            // Modify character values
            On.RoR2.CharacterBody.RecalculateStats += (orig, self) =>
            {
                orig(self);
                if (self?.inventory && self.inventory.GetItemCount(myItemDef.itemIndex) > 0)
                {
                    float itemCount = self.inventory.GetItemCount(myItemDef.itemIndex);
                    self.crit += itemCount * bonusCritChance;
                    self.critMultiplier += itemCount * bonusCritDamage * 0.01f;
                }
            };

            // When something takes damage
            On.RoR2.HealthComponent.TakeDamage += (orig, self, damageInfo) =>
            {
                orig(self, damageInfo);
                if (damageInfo.attacker && damageInfo.crit)
                {
                    CharacterBody attackerCharacterBody = damageInfo.attacker.GetComponent<CharacterBody>();
                    
                    if (attackerCharacterBody?.inventory)
                    {
                        int inventoryCount = attackerCharacterBody.inventory.GetItemCount(myItemDef.itemIndex);
                        if (inventoryCount > 0)
                        {
                            float damageDealt = damageInfo.damage * attackerCharacterBody.critMultiplier * (inventoryCount * bonusCritDamage * 0.01f / attackerCharacterBody.critMultiplier);
                            Utilities.AddValueInDictionary(ref bonusDamageDealt, attackerCharacterBody.master, damageDealt);
                        }
                    }
                }
            };
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("InfinityEdge", "InfinityEdge");

            // Short description
            LanguageAPI.Add("InfinityEdgeItem", "Gain crit chance and crit damage");

            // Long description
            LanguageAPI.Add("InfinityEdgeDesc", "Gain <style=cIsUtility>" + bonusCritChance + "%</style> crit chance and <style=cIsDamage>" + bonusCritDamage + "%</style> crit damage");

            // Lore
            LanguageAPI.Add("InfinityEdgeLore", "For when enemies need to die");
        }
    }
}