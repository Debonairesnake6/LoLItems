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
using BepInEx.Configuration;

namespace LoLItems
{
    internal class ExperimentalHexplate
    {
        public static ItemDef myItemDef;
        // public static BuffDef myBuffDef;

        public static ConfigEntry<float> exampleValue { get; set; }
        public static ConfigEntry<bool> enabled { get; set; }
        public static ConfigEntry<string> rarity { get; set; }
        public static ConfigEntry<string> voidItems { get; set; }
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> exampleStoredValue = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static string exampleStoredValueToken = "ExperimentalHexplate.exampleStoredValue";
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = new Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster>();
        public static Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef = new Dictionary<RoR2.UI.ItemIcon, CharacterMaster>();
        // ENABLE if a sound effect int is needed (replace num with proper value)
        // public static uint soundEffectID = 1234567890;

        // This runs when loading the file
        internal static void Init()
        {
            LoadConfig();
            if (!enabled.Value)
            {
                return;
            }

            CreateItem();
            // ENABLE for buff
            // CreateBuff();
            AddTokens();
            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict(null);
            // Enable for custom display rules
            // ItemDisplayRuleDict itemDisplayRuleDict = CreateDisplayRules();
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            // ENABLE for buff
            // ContentAddition.AddBuffDef(myBuffDef);
            hooks();
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, rarity, voidItems, "ExperimentalHexplate");
            SetupNetworkMappings();
        }

        private static void LoadConfig()
        {
            enabled = LoLItems.MyConfig.Bind<bool>(
                "ExperimentalHexplate",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            rarity = LoLItems.MyConfig.Bind<string>(
                "ExperimentalHexplate",
                "Rarity",
                "Tier1Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            voidItems = LoLItems.MyConfig.Bind<string>(
                "ExperimentalHexplate",
                "Void Items",
                "",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            exampleValue = LoLItems.MyConfig.Bind<float>(
                "ExperimentalHexplate",
                "Item Value",
                1f,
                "Amount of value each item will grant."

            );
        }

        private static void CreateItem()
        {
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();
            myItemDef.name = "ExperimentalHexplate";
            myItemDef.nameToken = "ExperimentalHexplate";
            myItemDef.pickupToken = "ExperimentalHexplateItem";
            myItemDef.descriptionToken = "ExperimentalHexplateDesc";
            myItemDef.loreToken = "ExperimentalHexplateLore";
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(Utilities.GetRarityFromString(rarity.Value)).WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("ExperimentalHexplateIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("ExperimentalHexplatePrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = new ItemTag[2] { ItemTag.Damage, ItemTag.Utility };
        }

        // ENABLE for buff
        // private static void CreateBuff()
        // {
        //     myBuffDef = ScriptableObject.CreateInstance<BuffDef>();

        //     myBuffDef.iconSprite = Resources.Load<Sprite>("Textures/MiscIcons/texMysteryIcon");
        //     //  ENABLE for custom assets
        //     // myBuffDef.iconSprite = Assets.icons.LoadAsset<Sprite>("ExperimentalHexplateIcon");
        //     myBuffDef.name = "ExperimentalHexplateBuff";
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

                // ENABLE for infusion orb
                // GameObject gameObject = null;
                // Transform transform = null;
                // Vector3 vector = Vector3.zero;

                // if (damageReport.victim)
                // {
                //     gameObject = damageReport.victim.gameObject;
                //     transform = gameObject.transform;
                //     vector = transform.position;
                // }

                if (damageReport.attackerMaster?.inventory != null)
                {

                    int inventoryCount = damageReport.attackerMaster.inventory.GetItemCount(myItemDef.itemIndex);
					if (inventoryCount > 0)
					{
                        // ENABLE for infusion orb
                        // InfusionOrb ExperimentalHexplateOrb = new InfusionOrb();
                        // ExperimentalHexplateOrb.origin = vector;
                        // ExperimentalHexplateOrb.target = Util.FindBodyMainHurtBox(damageReport.attackerBody);
                        // ExperimentalHexplateOrb.maxHpValue = 0;
                        // OrbManager.instance.AddOrb(ExperimentalHexplateOrb);
                        // Utilities.AddValueInDictionary(ref exampleStoredValue, damageReport.attackerBody.master, exampleValue.Value, exampleStoredValueToken);
					}
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
                            // float damage = inventoryCount * exampleValue.Value;
                            // DamageInfo onHitProc = damageInfo;
                            // onHitProc.damage = damage;
                            // onHitProc.crit = false;
                            // onHitProc.procCoefficient = 0f;
                            // onHitProc.damageType = DamageType.Generic;
                            // onHitProc.damageColorIndex = DamageColorIndex.SuperBleed;
                            // onHitProc.inflictor = damageInfo.attacker;

                            // victimCharacterBody.healthComponent.TakeDamage(onHitProc);  
                            // Utilities.AddValueToDictionary(ref exampleStoredValue, attackerCharacterBody.master.netId, damage, exampleStoredValueToken);
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

            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        private static void RecalculateStatsAPI_GetStatCoefficients(CharacterBody characterBody, RecalculateStatsAPI.StatHookEventArgs args)
        {
            int count = characterBody?.inventory?.GetItemCount(myItemDef.itemIndex) ?? 0;
            if (count > 0)
            {
                // args.secondaryCooldownMultAdd += count;
            }
        }

        private static (string, string) GetDisplayInformation(CharacterMaster masterRef)
        {
            if (masterRef == null)
                return (Language.GetString(myItemDef.descriptionToken), "");
            
            string customDescription = "";

            if (exampleStoredValue.TryGetValue(masterRef.netId, out float damageDealt))
                customDescription += "<br><br>Damage dealt: " + String.Format("{0:#}", damageDealt);
            else
                customDescription += "<br><br>Damage dealt: 0";

            return (Language.GetString(myItemDef.descriptionToken), customDescription);
        }

        private static void AddTokens()
        {
            // Styles
            // <style=cIsHealth>" + exampleValue.Value + "</style>
            // <style=cIsDamage>" + exampleValue.Value + "</style>
            // <style=cIsHealing>" + exampleValue.Value + "</style>
            // <style=cIsUtility>" + exampleValue.Value + "</style>
            // <style=cIsVoid>" + exampleValue.Value + "</style>
            // <style=cHumanObjective>" + exampleValue.Value + "</style>
            // <style=cLunarObjective>" + exampleValue.Value + "</style>
            // <style=cStack>" + exampleValue.Value + "</style>
            // <style=cWorldEvent>" + exampleValue.Value + "</style>
            // <style=cArtifact>" + exampleValue.Value + "</style>
            // <style=cUserSetting>" + exampleValue.Value + "</style>
            // <style=cDeath>" + exampleValue.Value + "</style>
            // <style=cSub>" + exampleValue.Value + "</style>
            // <style=cMono>" + exampleValue.Value + "</style>
            // <style=cShrine>" + exampleValue.Value + "</style>
            // <style=cEvent>" + exampleValue.Value + "</style>

            // Name of the item
            LanguageAPI.Add("ExperimentalHexplate", "ExperimentalHexplate");

            // Short description
            LanguageAPI.Add("ExperimentalHexplateItem", "ExperimentalHexplate pickup text");

            // Long description
            LanguageAPI.Add("ExperimentalHexplateDesc", "ExperimentalHexplate Description");

            // Lore
            LanguageAPI.Add("ExperimentalHexplateLore", "Was it wise to put on something this experimental? <br><br>Probably.");

            // ENABLE for buff
            // LanguageAPI.Add("ExperimentalHexplateBuff", "ExperimentalHexplate buff description");
        }

        public static void SetupNetworkMappings()
        {
            LoLItems.networkMappings.Add(exampleStoredValueToken, exampleStoredValue);
        }
    }
}