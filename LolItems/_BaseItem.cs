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
    internal class MyExampleBaseItem
    {

        //We need our item definition to persist through our functions, and therefore make it a class field.
        public static ItemDef myItemDef;

        // ENABLE for buff
        // public static BuffDef myBuffDef;

        // Set value amount in one location
        public static float exampleValue = 1f;
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> exampleStoredValue = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();

        // This runs when loading the file
        internal static void Init()
        {
            //Generate the basic information for the item
            CreateItem();

            // ENABLE for buff
            // CreateBuff();

            //Now let's turn the tokens we made into actual strings for the game:
            AddTokens();

            // Don't worry about displaying the item on the character
            var displayRules = new ItemDisplayRuleDict(null);

            // Then finally add it to R2API
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));

            // ENABLE for buff
            // ContentAddition.AddBuffDef(myBuffDef);

            // Initialize the hooks
            hooks();
        }

        private static void CreateItem()
        {
            //First let's define our item
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();

            // Language Tokens, check AddTokens() below.
            myItemDef.name = "MyExampleItem";
            myItemDef.nameToken = "MyExampleItem";
            myItemDef.pickupToken = "MyExampleItemItem";
            myItemDef.descriptionToken = "MyExampleItemDesc";
            myItemDef.loreToken = "MyExampleItemLore";

            //The tier determines what rarity the item is:
            //Tier1=white, Tier2=green, Tier3=red, Lunar=Lunar, Boss=yellow,
            //and finally NoTier is generally used for helper items, like the tonic affliction
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier1Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001

            //You can create your own icons and prefabs through assetbundles, but to keep this boilerplate brief, we'll be using question marks.
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("MyExampleItemIcon");
            // myItemDef.pickupIconSprite = Resources.Load<Sprite>("Textures/MiscIcons/texMysteryIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("MyExampleItemPrefab");
            // myItemDef.pickupModelPrefab = Resources.Load<GameObject>("Prefabs/PickupModels/PickupMystery");

            //Can remove determines if a shrine of order, or a printer can take this item, generally true, except for NoTier items.
            myItemDef.canRemove = true;

            //Hidden means that there will be no pickup notification,
            //and it won't appear in the inventory at the top of the screen.
            //This is useful for certain noTier helper items, such as the DrizzlePlayerHelper.
            myItemDef.hidden = false;
        }

        // ENABLE for buff
        // private static void CreateBuff()
        // {
        //     myBuffDef = ScriptableObject.CreateInstance<BuffDef>();

        //     myBuffDef.iconSprite = Resources.Load<Sprite>("Textures/MiscIcons/texMysteryIcon");
        //     myBuffDef.name = "MyExampleItemBuff";
        //     myBuffDef.buffColor = Color.red;
        //     myBuffDef.canStack = true;
        //     myBuffDef.isDebuff = false;
        //     myBuffDef.isCooldown = false;
        //     myBuffDef.isHidden = false;
        // }


        private static void hooks()
        {
            // Do something on character death
            On.RoR2.GlobalEventManager.OnCharacterDeath += (orig, globalEventManager, damageReport) =>
            {
                orig(globalEventManager, damageReport);

                GameObject gameObject = null;
                Transform transform = null;
                Vector3 vector = Vector3.zero;

                if (damageReport.victim)
                {
                    gameObject = damageReport.victim.gameObject;
                    transform = gameObject.transform;
                    vector = transform.position;
                }

                if (damageReport.attackerMaster?.inventory != null)
                {

                    int inventoryCount = damageReport.attackerMaster.inventory.GetItemCount(myItemDef.itemIndex);
					if (inventoryCount > 0)
					{
                        // ENABLE for infusion orb
                        // InfusionOrb MyExampleItemOrb = new InfusionOrb();
                        // MyExampleItemOrb.origin = vector;
                        // MyExampleItemOrb.target = Util.FindBodyMainHurtBox(damageReport.attackerBody);
                        // MyExampleItemOrb.maxHpValue = 0;
                        // OrbManager.instance.AddOrb(MyExampleItemOrb);
                        // Utilities.AddValueToDictionary(ref exampleStoredValue, damageReport.attackerBody.master.netId, exampleValue);
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
                        if (item.itemIndex == myItemDef.itemIndex && exampleStoredValue.TryGetValue(self.targetMaster.netId, out float value)){
                            // ENABLE for description update
                            // item.tooltipProvider.overrideBodyText =
                            //     Language.GetString(myItemDef.descriptionToken) + "<br><br>Value gained: " + String.Format("{0:#}", value);
                        }
                    });
#pragma warning restore Publicizer001
                }
            };

            // Open Scoreboard
            On.RoR2.UI.ScoreboardStrip.SetMaster += (orig, self, characterMaster) =>
            {
                orig(self, characterMaster);
                if (self.itemInventoryDisplay && characterMaster)
                {
#pragma warning disable Publicizer001
                    self.itemInventoryDisplay.itemIcons.ForEach(delegate(RoR2.UI.ItemIcon item)
                    {
                        // NOT WORKING
                        // if (item.itemIndex == myItemDef.itemIndex && exampleStoredValue.TryGetValue(characterMaster.netId, out float value)){
                        //     item.tooltipProvider.overrideBodyText =
                        //         Language.GetString(myItemDef.descriptionToken) + "<br><br>Health gained: " + value;
                        // }
                    });
#pragma warning restore Publicizer001
                }
            };

            // When you hit an enemy
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
                            // ENABLE for damage
                            // float damage = inventoryCount * exampleValue;
                            // DamageInfo onHitProc = damageInfo;
                            // onHitProc.damage = damage;
                            // onHitProc.crit = false;
                            // onHitProc.procCoefficient = 0f;
                            // onHitProc.damageType = DamageType.Generic;
                            // onHitProc.damageColorIndex = DamageColorIndex.SuperBleed;
                            // onHitProc.inflictor = damageInfo.attacker;

                            // victimCharacterBody.healthComponent.TakeDamage(onHitProc);  
                            // Utilities.AddValueToDictionary(ref exampleStoredValue, attackerCharacterBody.master.netId, damage);
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
                        if (inventoryCount > 0)
                        {
                            // Do something   
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
            LanguageAPI.Add("MyExampleItem", "MyExampleItem");

            // Short description
            LanguageAPI.Add("MyExampleItemItem", "MyExampleItem pickup text");

            // Long description
            LanguageAPI.Add("MyExampleItemDesc", "MyExampleItem Description");

            // Lore
            LanguageAPI.Add("MyExampleItemLore", "MyExampleItem Lore");

            // ENABLE for buff
            // LanguageAPI.Add("MyExampleItemBuff", "MyExampleItem buff description");
        }
    }
}