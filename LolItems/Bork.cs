using System;
using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using BepInEx.Configuration;

namespace LoLItems
{
    internal class Bork
    {

        // We need our item definition to persist through our functions, and therefore make it a class field.
        public static ItemDef myItemDef;

        public static BuffDef myCounterBuffDef;
        public static BuffDef myTimerBuffDef;
        public static ConfigEntry<float> OnHitDamageAmount { get; set; }
        public static ConfigEntry<float> ProcForBigHit { get; set; }
        public static ConfigEntry<float> OnHitHealPercent { get; set; }
        public static ConfigEntry<float> BigOnHitTimer { get; set; }
        public static ConfigEntry<float> ProcDamageMin { get; set; }
        public static ConfigEntry<float> ProcDamageMax { get; set; }
        public static ConfigEntry<float> AttackSpeed { get; set; }
        public static ConfigEntry<bool> Enabled { get; set; }
        public static ConfigEntry<string> Rarity { get; set; }
        public static ConfigEntry<string> VoidItems { get; set; }
        public static Dictionary<NetworkInstanceId, float> borkBonusDamage = [];
        public static string borkBonusDamageToken = "Bork.borkBonusDamage";
        public static Dictionary<NetworkInstanceId, float> borkBonusHeal = [];
        public static string borkBonusHealToken = "Bork.borkBonusHeal";
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = [];
        public static Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef = [];
        public static uint procSoundEffect = 3722891417;

        internal static void Init()
        {
            LoadConfig();
            if (!Enabled.Value)
            {
                return;
            }

            // Generate the basic information for the item
            CreateItem();
            CreateBuff();

            // Now let's turn the tokens we made into actual strings for the game:
            AddTokens();

            // You can add your own display rules here, where the first argument passed are the default display rules: the ones used when no specific display rules for a character are found.
            // For this example, we are omitting them, as they are quite a pain to set up without tools like ItemDisplayPlacementHelper
            ItemDisplayRuleDict displayRules = new(null);

            // Then finally add it to R2API
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            ContentAddition.AddBuffDef(myCounterBuffDef);
            ContentAddition.AddBuffDef(myTimerBuffDef);

            // Initialize the hooks
            Hooks();
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, Rarity, VoidItems, "Bork");
            SetupNetworkMappings();
        }

        private static void LoadConfig()
        {
            Enabled = LoLItems.MyConfig.Bind(
                "Blade of the Ruined King",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            Rarity = LoLItems.MyConfig.Bind(
                "Blade of the Ruined King",
                "Rarity",
                "VoidTier2Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            VoidItems = LoLItems.MyConfig.Bind(
                "Blade of the Ruined King",
                "Void Items",
                "Syringe,Seed",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            OnHitDamageAmount = LoLItems.MyConfig.Bind(
                "Blade of the Ruined King",
                "On Hit Damage Percent",
                5f,
                "Amount of on hit current health damage percent each item will grant."
            );

            ProcForBigHit = LoLItems.MyConfig.Bind(
                "Blade of the Ruined King",
                "On Hit Proc Requirement",
                3f,
                "Amount of hits required to proc the on hit damage."
            );

            OnHitHealPercent = LoLItems.MyConfig.Bind(
                "Blade of the Ruined King",
                "Heal Percent",
                20f,
                "Percentage of damage dealt to be gained as healing."
            );

            BigOnHitTimer = LoLItems.MyConfig.Bind(
                "Blade of the Ruined King",
                "Proc Cooldown",
                10f,
                "Cooldown per enemy."
            );

            ProcDamageMin = LoLItems.MyConfig.Bind(
                "Blade of the Ruined King",
                "Min Proc Damage",
                2f,
                "Multiplied by your base damage to determine the minimum proc damage."
            );

            ProcDamageMax = LoLItems.MyConfig.Bind(
                "Blade of the Ruined King",
                "Max Proc Damage",
                25f,
                "Multiplied by your base damage to determine the maximum proc damage."
            );

            AttackSpeed = LoLItems.MyConfig.Bind(
                "Blade of the Ruined King",
                "Attack Speed",
                5f,
                "Amount of attack speed each item will grant."
            );
        }

        private static void CreateItem()
        {
            // First let's define our item
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();

            // Language Tokens, check AddTokens() below.
            myItemDef.name = "Bork";
            myItemDef.nameToken = "Bork";
            myItemDef.pickupToken = "BorkItem";
            myItemDef.descriptionToken = "BorkDesc";
            myItemDef.loreToken = "BorkLore";
#pragma warning disable Publicizer001
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(Utilities.GetRarityFromString(Rarity.Value)).WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = MyAssets.icons.LoadAsset<Sprite>("BorkIcon");
            myItemDef.pickupModelPrefab = MyAssets.prefabs.LoadAsset<GameObject>("BorkPrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = [ ItemTag.Damage, ItemTag.Healing ];
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


        private static void Hooks()
        {
            On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, victim) =>
            {
                orig(self, damageInfo, victim);

                if (!NetworkServer.active)
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
                                if (currentBuffCount < ProcForBigHit.Value - 1 && NetworkServer.active)
                                {
                                    victimCharacterBody.healthComponent.body.AddBuff(myCounterBuffDef);
                                }
                                else
                                {
                                    Utilities.RemoveBuffStacks(victimCharacterBody, myCounterBuffDef.buffIndex);
                                    Utilities.AddTimedBuff(victimCharacterBody, myTimerBuffDef, BigOnHitTimer.Value);

                                    float damage = victimCharacterBody.healthComponent.health * inventoryCount * OnHitDamageAmount.Value / 100 * damageInfo.procCoefficient;
                                    damage = Math.Max(ProcDamageMin.Value * attackerCharacterBody.damage, Math.Min(ProcDamageMax.Value * attackerCharacterBody.damage, damage));
                                    DamageInfo onHitProc = damageInfo;
                                    onHitProc.crit = false;
                                    onHitProc.procCoefficient = 0f;
                                    onHitProc.damageType = DamageType.Generic;
                                    onHitProc.inflictor = damageInfo.attacker;
                                    onHitProc.damage = damage;
                                    onHitProc.damageColorIndex = DamageColorIndex.Nearby;
                                    victimCharacterBody.healthComponent.TakeDamage(onHitProc);
                                    Utilities.AddValueInDictionary(ref borkBonusDamage, attackerCharacterBody.master, damage, borkBonusDamageToken);

                                    float healAmount = damage * (OnHitHealPercent.Value / 100);
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
            args.baseAttackSpeedAdd += characterBody?.inventory?.GetItemCount(myItemDef) / 100f * AttackSpeed.Value ?? 0;
        }

        private static (string, string) GetDisplayInformation(CharacterMaster masterRef)
        {
            if (masterRef == null)
                return (Language.GetString(myItemDef.descriptionToken), "");
            
            string customDescription = "";

            if (borkBonusDamage.TryGetValue(masterRef.netId, out float damageDealt))
                customDescription += "<br><br>Damage dealt: " + string.Format("{0:#, ##0.##}", damageDealt);
            else
                customDescription += "<br><br>Damage dealt: 0";

            if (borkBonusHeal.TryGetValue(masterRef.netId, out float healingDone))
                customDescription += "<br>Healing: " + string.Format("{0:#, ##0.##}", healingDone);
            else
                customDescription += "<br>Healing: 0";

            return (Language.GetString(myItemDef.descriptionToken), customDescription);
        }

        // This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("Bork", "Blade of the Ruined King");

            // Short description
            LanguageAPI.Add("BorkItem", "Increases attack speed. Every " + ProcForBigHit.Value + " hits do damage and heal, and has a cooldown. Corrupts <style=cIsVoid>Syringes</style> and <style=cIsVoid>Leaching Seeds</style>.");

            // Long description
            LanguageAPI.Add("BorkDesc", "Gives <style=cIsDamage>" + AttackSpeed.Value + "%</style> <style=cStack>(+" + AttackSpeed.Value + 
            "%)</style> attack speed. Deals <style=cIsDamage>" + OnHitDamageAmount.Value + "%</style> <style=cStack>(+" + OnHitDamageAmount.Value + 
            "%)</style> current enemy hp every third hit, and heal for <style=cIsHealing>" + OnHitHealPercent.Value + "%</style> of that damage on a " + BigOnHitTimer.Value + 
            " second cooldown. Corrupts <style=cIsVoid>Syringes</style> and <style=cIsVoid>Leaching Seeds</style>.");

            // Lore
            LanguageAPI.Add("BorkLore", "Viego is a plague to everything he touches.");
        }

        public static void SetupNetworkMappings()
        {
            LoLItems.networkMappings.Add(borkBonusDamageToken, borkBonusDamage);
            LoLItems.networkMappings.Add(borkBonusHealToken, borkBonusHeal);
        }
    }
}