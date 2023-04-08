using System.Collections.Generic;
using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace LoLItems
{
    internal class WhiteClover
    {

        //We need our item definition to persist through our functions, and therefore make it a class field.
        public static ItemDef myItemDef;

        // Set luck amount in one location
        public static float luckAmount = 10f;

        // Set equipment cd in one location
        public static float cooldownAmount = 1f - (30f * .01f);

        internal static void Init()
        {
            //Generate the basic information for the item
            CreateItem();

            //Now let's turn the tokens we made into actual strings for the game:
            AddTokens();

            //You can add your own display rules here, where the first argument passed are the default display rules: the ones used when no specific display rules for a character are found.
            //For this example, we are omitting them, as they are quite a pain to set up without tools like ItemDisplayPlacementHelper
            var displayRules = new ItemDisplayRuleDict(null);

            //Then finally add it to R2API
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));

            //But now we have defined an item, but it doesn't do anything yet. So we'll need to define that ourselves.
            // GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;

            // Initialize the hooks
            hooks();

            // // Integrate with ItemStats
            // if (ItemStatsModCompatibility.enabled)
            // {
            //     var myItemStatDef = new ItemStats.ItemStatDef
            //     {
            //         Stats = new List<ItemStats.Stat.ItemStat>
            //         {
            //             new ItemStats.Stat.ItemStat(
            //                 (itemCount, ctx) => itemCount * luckAmount,
            //                 (value, ctx) => $"Luck increase: {value}"
            //             )
            //         }
            //     };
            //     ItemStatsModCompatibility.InvokeAddCustomItemStatDef(myItemDef.itemIndex, myItemStatDef);
            //     ItemStatsModCompatibility.InvokeAddStatModifier(new MyLuckModifier());
            //     Log.LogDebug("Loaded ItemStatsMod");
            // }
        }

        private static void CreateItem()
        {
            //First let's define our item
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();

            // Language Tokens, check AddTokens() below.
            myItemDef.name = "WhiteClover";
            myItemDef.nameToken = "WhiteClover";
            myItemDef.pickupToken = "WhiteCloverItem";
            myItemDef.descriptionToken = "WhiteCloverDesc";
            myItemDef.loreToken = "WhiteCloverLore";

            //The tier determines what rarity the item is:
            //Tier1=white, Tier2=green, Tier3=red, Lunar=Lunar, Boss=yellow,
            //and finally NoTier is generally used for helper items, like the tonic affliction
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier1Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001
            // Instead of loading the itemtierdef directly, you can also do this like below as a workaround
            // myItemDef.deprecatedTier = ItemTier.Tier2;

            //You can create your own icons and prefabs through assetbundles, but to keep this boilerplate brief, we'll be using question marks.
            myItemDef.pickupIconSprite = Resources.Load<Sprite>("Textures/MiscIcons/texMysteryIcon");
            myItemDef.pickupModelPrefab = Resources.Load<GameObject>("Prefabs/PickupModels/PickupMystery");

            //Can remove determines if a shrine of order, or a printer can take this item, generally true, except for NoTier items.
            myItemDef.canRemove = false;

            //Hidden means that there will be no pickup notification,
            //and it won't appear in the inventory at the top of the screen.
            //This is useful for certain noTier helper items, such as the DrizzlePlayerHelper.
            myItemDef.hidden = false;
        }


        private static void hooks()
        {

            On.RoR2.CharacterBody.OnInventoryChanged += (orig, self) => 
            {
                orig(self);

                // Set luck
                var inventoryCount = self.inventory.GetItemCount(myItemDef.itemIndex);
                self.master.luck += luckAmount * inventoryCount;
            };

            On.RoR2.Inventory.CalculateEquipmentCooldownScale += (orig, self) =>
            {
                float cooldown = orig(self);
                var inventoryCount = self.GetItemCount(myItemDef.itemIndex);
                if (inventoryCount > 0)
                {
                    cooldown *= Mathf.Pow(cooldownAmount, (float)inventoryCount);
                }
                return cooldown;
            };
        }

        // private void GlobalEventManager_onCharacterDeathGlobal(DamageReport report)
        // {
        //     //If a character was killed by the world, we shouldn't do anything.
        //     if (!report.attacker || !report.attackerBody)
        //     {
        //         return;
        //     }

        //     var attackerCharacterBody = report.attackerBody;

        //     //We need an inventory to do check for our item
        //     if (attackerCharacterBody.inventory)
        //     {
        //         //store the amount of our item we have
        //         var garbCount = attackerCharacterBody.inventory.GetItemCount(myItemDef.itemIndex);
        //         if (garbCount > 0 &&
        //             //Roll for our 50% chance.
        //             Util.CheckRoll(50, attackerCharacterBody.master))
        //         {
        //             //Since we passed all checks, we now give our attacker the cloaked buff.
        //             //Note how we are scaling the buff duration depending on the number of the custom item in our inventory.
        //             attackerCharacterBody.AddTimedBuff(RoR2Content.Buffs.Cloak, 3 + garbCount);
        //         }
        //     }
        // }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            //The Name should be self explanatory
            LanguageAPI.Add("WhiteClover", "White Clover");

            //The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("WhiteCloverItem", "Makes you lucky");

            //The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("WhiteCloverDesc", "Adds <style=cIsUtility>" + luckAmount + "</style> luck <style=cStack>(+" + luckAmount +" per stack)</style>.");

            //The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("WhiteCloverLore", "Some may say this item would break the game.");
        }
    }
}