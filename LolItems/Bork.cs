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
using BepInEx.Configuration;

namespace LoLItems
{
    internal class Bork
    {

        //We need our item definition to persist through our functions, and therefore make it a class field.
        public static ItemDef myItemDef;

        public static BuffDef myCounterBuffDef;
        public static BuffDef myTimerBuffDef;
        public static ConfigEntry<float> onHitDamageAmount { get; set; }
        public static ConfigEntry<float> procForBigHit { get; set; }
        public static ConfigEntry<float> onHitHealPercent { get; set; }
        public static ConfigEntry<float> bigOnHitTimer { get; set; }
        public static ConfigEntry<float> procDamageMin { get; set; }
        public static ConfigEntry<float> procDamageMax { get; set; }
        public static ConfigEntry<float> attackSpeed { get; set; }
        public static ConfigEntry<bool> enabled { get; set; }
        public static ConfigEntry<string> rarity { get; set; }
        public static ConfigEntry<string> voidItems { get; set; }
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> borkBonusDamage = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static string borkBonusDamageToken = "Bork.borkBonusDamage";
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> borkBonusHeal = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static string borkBonusHealToken = "Bork.borkBonusHeal";
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = new Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster>();
        public static Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef = new Dictionary<RoR2.UI.ItemIcon, CharacterMaster>();
        public static uint procSoundEffect = 3722891417;

        internal static void Init()
        {
            LoadConfig();
            if (!enabled.Value)
            {
                return;
            }

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
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, rarity, voidItems, "Bork");
            SetupNetworkMappings();
        }

        private static void LoadConfig()
        {
            enabled = LoLItems.MyConfig.Bind<bool>(
                "Bork",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            rarity = LoLItems.MyConfig.Bind<string>(
                "Bork",
                "Rarity",
                "VoidTier2Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            voidItems = LoLItems.MyConfig.Bind<string>(
                "Bork",
                "Void Items",
                "Syringe,Seed",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            onHitDamageAmount = LoLItems.MyConfig.Bind<float>(
                "Bork",
                "On Hit Damage Percent",
                5f,
                "Amount of on hit max health damage percent each item will grant."

            );

            procForBigHit = LoLItems.MyConfig.Bind<float>(
                "Bork",
                "On Hit Proc Requirement",
                3f,
                "Amount of hits required to proc the on hit damage."

            );

            onHitHealPercent = LoLItems.MyConfig.Bind<float>(
                "Bork",
                "Heal Percent",
                20f,
                "Percentage of damage dealt to be gained as healing."

            );

            bigOnHitTimer = LoLItems.MyConfig.Bind<float>(
                "Bork",
                "Proc Cooldown",
                10f,
                "Cooldown per enemy."

            );

            procDamageMin = LoLItems.MyConfig.Bind<float>(
                "Bork",
                "Min Proc Damage",
                2f,
                "Multiplied by your base damage to determine the minimum proc damage."

            );

            procDamageMax = LoLItems.MyConfig.Bind<float>(
                "Bork",
                "Max Proc Damage",
                25f,
                "Multiplied by your base damage to determine the maximum proc damage."

            );

            attackSpeed = LoLItems.MyConfig.Bind<float>(
                "Bork",
                "Attack Speed",
                5f,
                "Amount of attack speed each item will grant."

            );
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
#pragma warning disable Publicizer001
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(Utilities.GetRarityFromString(rarity.Value)).WaitForCompletion();
#pragma warning restore Publicizer001
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
            myCounterBuffDef.canStack = true;
            myCounterBuffDef.isDebuff = false;
            myCounterBuffDef.isCooldown = false;
            myCounterBuffDef.isHidden = true;

            // Create a timer to prevent stacks for a short period of time
            myTimerBuffDef = ScriptableObject.CreateInstance<BuffDef>();

            myTimerBuffDef.iconSprite = Resources.Load<Sprite>("Textures/MiscIcons/texMysteryIcon");
            myTimerBuffDef.name = "BorkTimerBuff";
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

                if (!UnityEngine.Networking.NetworkServer.active)
                    return;
                
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
                                if (currentBuffCount < procForBigHit.Value - 1)
                                {
                                    victimCharacterBody.healthComponent.body.AddBuff(myCounterBuffDef);
                                }
                                else
                                {
                                    Utilities.RemoveBuffStacks(victimCharacterBody, myCounterBuffDef.buffIndex);
                                    Utilities.AddTimedBuff(victimCharacterBody, myTimerBuffDef, bigOnHitTimer.Value);

                                    float damage = victimCharacterBody.healthComponent.health * inventoryCount * onHitDamageAmount.Value / 100 * damageInfo.procCoefficient;
                                    damage = Math.Max(procDamageMin.Value * attackerCharacterBody.damage, Math.Min(procDamageMax.Value * attackerCharacterBody.damage, damage));
                                    DamageInfo onHitProc = damageInfo;
                                    onHitProc.crit = false;
                                    onHitProc.procCoefficient = 0f;
                                    onHitProc.damageType = DamageType.Generic;
                                    onHitProc.inflictor = damageInfo.attacker;
                                    onHitProc.damage = damage;
                                    onHitProc.damageColorIndex = DamageColorIndex.Nearby;
                                    victimCharacterBody.healthComponent.TakeDamage(onHitProc);
                                    Utilities.AddValueInDictionary(ref borkBonusDamage, attackerCharacterBody.master, damage, borkBonusDamageToken);

                                    float healAmount = damage * (onHitHealPercent.Value / 100);
                                    attackerCharacterBody.healthComponent.Heal(healAmount, onHitProc.procChainMask);
                                    Utilities.AddValueInDictionary(ref borkBonusHeal, attackerCharacterBody.master, healAmount, borkBonusHealToken);
                                    AkSoundEngine.PostEvent(procSoundEffect, attackerCharacterBody.gameObject);
                                }
                            }
                        }
                    }
                }
            };

            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        private static void RecalculateStatsAPI_GetStatCoefficients(CharacterBody characterBody, RecalculateStatsAPI.StatHookEventArgs args)
        {
            args.baseAttackSpeedAdd += characterBody?.inventory?.GetItemCount(myItemDef) / 100f * attackSpeed.Value ?? 0;
        }

        private static (string, string) GetDisplayInformation(CharacterMaster masterRef)
        {
            if (masterRef == null)
                return (Language.GetString(myItemDef.descriptionToken), "");
            
            string customDescription = "";

            if (borkBonusDamage.TryGetValue(masterRef.netId, out float damageDealt))
                customDescription += "<br><br>Damage dealt: " + String.Format("{0:#}", damageDealt);
            else
                customDescription += "<br><br>Damage dealt: 0";

            if (borkBonusHeal.TryGetValue(masterRef.netId, out float healingDone))
                customDescription += "<br>Healing: " + String.Format("{0:#}", healingDone);
            else
                customDescription += "<br>Healing: 0";

            return (Language.GetString(myItemDef.descriptionToken), customDescription);
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            //The Name should be self explanatory
            LanguageAPI.Add("Bork", "Bork");

            //The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("BorkItem", "Attack speed. Every " + procForBigHit.Value + " hits do damage and heal, and has a cooldown. Corrupts <style=cIsVoid>Syringes</style> and <style=cIsVoid>Leaching Seeds</style>.");

            //The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("BorkDesc", "Gives <style=cIsDamage>" + attackSpeed.Value + "%</style> <style=cStack>(+" + attackSpeed.Value + 
            "%)</style> attack speed. Deals <style=cIsDamage>" + onHitDamageAmount.Value + "%</style> <style=cStack>(+" + onHitDamageAmount.Value + 
            "%)</style> current enemy hp every third hit, and heal for <style=cIsHealing>" + onHitHealPercent.Value + "%</style> of that damage on a " + bigOnHitTimer.Value + 
            " second cooldown. Corrupts <style=cIsVoid>Syringes</style> and <style=cIsVoid>Leaching Seeds</style>.");

            //The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("BorkLore", "Viego is a plague to everything he touches.");
        }

        public static void SetupNetworkMappings()
        {
            LoLItems.networkMappings.Add(borkBonusDamageToken, borkBonusDamage);
            LoLItems.networkMappings.Add(borkBonusHealToken, borkBonusHeal);
        }
    }
}