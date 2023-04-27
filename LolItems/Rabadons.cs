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
    internal class Rabadons
    {

        //We need our item definition to persist through our functions, and therefore make it a class field.
        public static ItemDef myItemDef;

        // Set value amount in one location
        public static float damageAmp = 0.30f;
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> rabadonsBonusDamage = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();

        // This runs when loading the file
        internal static void Init()
        {
            //Generate the basic information for the item
            CreateItem();

            //Now let's turn the tokens we made into actual strings for the game:
            AddTokens();

            // Don't worry about displaying the item on the character
            var displayRules = new ItemDisplayRuleDict(null);

            // Then finally add it to R2API
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));

            // Initialize the hooks
            hooks();
        }

        private static void CreateItem()
        {
            //First let's define our item
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();

            // Language Tokens, check AddTokens() below.
            myItemDef.name = "Rabadons";
            myItemDef.nameToken = "RabadonsItem";
            myItemDef.pickupToken = "RabadonsItemItem";
            myItemDef.descriptionToken = "RabadonsItemDesc";
            myItemDef.loreToken = "RabadonsItemLore";

#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("RabadonsIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("RabadonsPrefab");
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
                        if (item.itemIndex == myItemDef.itemIndex && rabadonsBonusDamage.TryGetValue(self.targetMaster.netId, out float damageDealt)){
                            // ENABLE for description update
                            item.tooltipProvider.overrideBodyText =
                                Language.GetString(myItemDef.descriptionToken) + "<br><br>Bonus damage dealt: " + String.Format("{0:#}", damageDealt);
                        }
                    });
#pragma warning restore Publicizer001
                }
            };

            On.RoR2.HealthComponent.TakeDamage += (orig, self, damageInfo) =>
            {
                if (damageInfo.attacker)
                {
                    CharacterBody attackerCharacterBody = damageInfo.attacker.GetComponent<CharacterBody>();
                    
                    if (attackerCharacterBody?.inventory)
                    {
                        int inventoryCount = attackerCharacterBody.inventory.GetItemCount(myItemDef.itemIndex);
                        if (inventoryCount > 0)
                        {
                            float damageMultiplier = 1 + inventoryCount * damageAmp;
                            Utilities.AddValueToDictionary(ref rabadonsBonusDamage, attackerCharacterBody.master, damageInfo.damage * (damageMultiplier - 1));
                            damageInfo.damage = damageMultiplier * damageInfo.damage;
                            
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

            //The Name should be self explanatory
            LanguageAPI.Add("RabadonsItem", "RabadonsItem");

            //The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("RabadonsItemItem", "Hat makes them go splat");

            //The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("RabadonsItemDesc", "Do <style=cIsUtility>" + damageAmp * 100 + "%</style> <style=cStack>(+" + damageAmp * 100 + "%)</style> more damage");

            //The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("RabadonsItemLore", "Makes you feel like a wizard.");
        }
    }
}