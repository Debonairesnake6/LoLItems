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
    internal class Liandrys
    {

        //We need our item definition to persist through our functions, and therefore make it a class field.
        public static ItemDef myItemDef;

        public static BuffDef myBuffDef;
        public static DotController.DotDef myDotDef;
        public static RoR2.DotController.DotIndex myDotDefIndex;

        // Set value amount in one location
        public static float burnDamagePercent = 2.5f;
        public static float burnDamageDuration = 5f;
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> liandrysDamageDealt = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();

        // This runs when loading the file
        internal static void Init()
        {
            //Generate the basic information for the item
            CreateItem();
            CreateBuff();
            CreateDot();

            //Now let's turn the tokens we made into actual strings for the game:
            AddTokens();

            // Don't worry about displaying the item on the character
            var displayRules = new ItemDisplayRuleDict(null);

            // Then finally add it to R2API
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            ContentAddition.AddBuffDef(myBuffDef);
            DotAPI.CustomDotBehaviour myDotCustomBehaviour = AddCustomDotBehaviour;
            myDotDefIndex = DotAPI.RegisterDotDef(myDotDef, myDotCustomBehaviour);

            // Initialize the hooks
            hooks();
        }

        private static void CreateItem()
        {
            //First let's define our item
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();

            // Language Tokens, check AddTokens() below.
            myItemDef.name = "Liandrys";
            myItemDef.nameToken = "Liandrys";
            myItemDef.pickupToken = "LiandrysItem";
            myItemDef.descriptionToken = "LiandrysDesc";
            myItemDef.loreToken = "LiandrysLore";

            //The tier determines what rarity the item is:
            //Tier1=white, Tier2=green, Tier3=red, Lunar=Lunar, Boss=yellow,
            //and finally NoTier is generally used for helper items, like the tonic affliction
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier2Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001

            //You can create your own icons and prefabs through assetbundles, but to keep this boilerplate brief, we'll be using question marks.
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("LiandrysIcon");
            // myItemDef.pickupIconSprite = Resources.Load<Sprite>("Textures/MiscIcons/texMysteryIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("LiandrysPrefab");
            // myItemDef.pickupModelPrefab = Resources.Load<GameObject>("Prefabs/PickupModels/PickupMystery");

            //Can remove determines if a shrine of order, or a printer can take this item, generally true, except for NoTier items.
            myItemDef.canRemove = true;

            //Hidden means that there will be no pickup notification,
            //and it won't appear in the inventory at the top of the screen.
            //This is useful for certain noTier helper items, such as the DrizzlePlayerHelper.
            myItemDef.hidden = false;
        }

        private static void CreateBuff()
        {
            myBuffDef = ScriptableObject.CreateInstance<BuffDef>();

            myBuffDef.iconSprite = Assets.icons.LoadAsset<Sprite>("LiandrysIcon");
            myBuffDef.name = "LiandrysBuff";
            myBuffDef.buffColor = Color.blue;
            myBuffDef.canStack = false;
            myBuffDef.isDebuff = true;
            myBuffDef.isCooldown = true;
            myBuffDef.isHidden = false;

        }
        
        private static void CreateDot()
        {
            myDotDef = new DotController.DotDef
            {
                damageColorIndex = DamageColorIndex.Bleed,
                associatedBuff = myBuffDef,
                terminalTimedBuff = myBuffDef,
                terminalTimedBuffDuration = burnDamageDuration,
                resetTimerOnAdd = true,
                interval = 1f,
                damageCoefficient = 1f / burnDamageDuration
            };
        }

        public static void AddCustomDotBehaviour(DotController self, DotController.DotStack dotStack)
        {
            if (dotStack.dotIndex == myDotDefIndex)
            {
                CharacterBody attackerCharacterBody = dotStack.attackerObject.GetComponent<CharacterBody>();
                int inventoryCount = 1;
                if (attackerCharacterBody?.inventory)
                {
                    inventoryCount = attackerCharacterBody.inventory.GetItemCount(myItemDef.itemIndex);
                }
#pragma warning disable Publicizer001
                dotStack.damage = self.victimBody.maxHealth * burnDamagePercent / 100f / burnDamageDuration * myDotDef.interval * inventoryCount;
#pragma warning restore Publicizer001
            }
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
                        if (item.itemIndex == myItemDef.itemIndex && liandrysDamageDealt.TryGetValue(self.targetMaster.netId, out float damageDealt)){
                            item.tooltipProvider.overrideBodyText =
                                Language.GetString(myItemDef.descriptionToken) + "<br><br>Damage dealt: " + String.Format("{0:#}", damageDealt);
                        }
                    });
#pragma warning restore Publicizer001
                }
            };

            // When you hit an enemy
            On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, victim) =>
            {
                orig(self, damageInfo, victim);

                if (damageInfo.attacker && damageInfo.attacker != victim)
                {
                    CharacterBody attackerCharacterBody = damageInfo.attacker.GetComponent<CharacterBody>();
                    CharacterBody victimCharacterBody = victim.GetComponent<CharacterBody>();
                    
                    if (attackerCharacterBody?.inventory)
                    {
                        int inventoryCount = attackerCharacterBody.inventory.GetItemCount(myItemDef.itemIndex);
                        if (inventoryCount > 0)
                        {
                            victimCharacterBody.AddTimedBuff(myBuffDef, burnDamageDuration);

                            float dotDamage = victimCharacterBody.maxHealth * burnDamagePercent / 100f * inventoryCount;
                            InflictDotInfo inflictDotInfo = new InflictDotInfo
                            {
                                victimObject = victimCharacterBody.healthComponent.gameObject,
                                attackerObject = attackerCharacterBody.gameObject,
                                totalDamage = dotDamage,
                                dotIndex = myDotDefIndex,
                                duration = burnDamageDuration,
                                maxStacksFromAttacker = 1,
                            };
                            DotController.InflictDot(ref inflictDotInfo);
                        }
                    }
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
                        int inventoryCount = attackerCharacterBody.inventory.GetItemCount(myItemDef.itemIndex);
                        if (inventoryCount > 0 && damageInfo.dotIndex == myDotDefIndex) 
                        {
                            Utilities.AddValueInDictionary(ref liandrysDamageDealt, attackerCharacterBody.master, damageInfo.damage);
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
            LanguageAPI.Add("Liandrys", "Liandrys");

            //The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("LiandrysItem", "Burn enemies on hit for a % of their max health");

            //The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("LiandrysDesc", "On hit burn enemies for <style=cIsDamage>" + burnDamagePercent + "%</style> <style=cStack>(+" + burnDamagePercent + "%)</style> max health over " + burnDamageDuration + " seconds");

            //The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("LiandrysLore", "A crying mask is a great halloween costume.");

            // ENABLE for buff
            LanguageAPI.Add("LiandrysBuff", "Liandrys is burning this unit");
        }
    }
}