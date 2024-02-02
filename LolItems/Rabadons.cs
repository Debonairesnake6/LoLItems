using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using BepInEx.Configuration;

namespace LoLItems
{
    internal class Rabadons
    {
        public static ItemDef myItemDef;
        public static GameObject ItemBodyModelPrefab;

        public static ConfigEntry<float> DamageAmp { get; set; }
        public static ConfigEntry<bool> Enabled { get; set; }
        public static ConfigEntry<string> Rarity { get; set; }
        public static ConfigEntry<string> VoidItems { get; set; }
        public static Dictionary<NetworkInstanceId, float> rabadonsBonusDamage = [];
        public static string rabadonsBonusDamageToken = "Rabadons.rabadonsBonusDamage";
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = [];
        public static Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef = [];

        internal static void Init()
        {
            LoadConfig();
            if (!Enabled.Value)
            {
                return;
            }

            CreateItem();
            ItemDisplayRuleDict itemDisplayRuleDict = CreateDisplayRules();
            AddTokens();
            ItemAPI.Add(new CustomItem(myItemDef, itemDisplayRuleDict));
            Hooks();
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, Rarity, VoidItems, "Rabadons");
            SetupNetworkMappings();
        }

        private static void LoadConfig()
        {
            Enabled = LoLItems.MyConfig.Bind(
                "Rabadons Deathcap",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            Rarity = LoLItems.MyConfig.Bind(
                "Rabadons Deathcap",
                "Rarity",
                "Tier3Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            VoidItems = LoLItems.MyConfig.Bind(
                "Rabadons Deathcap",
                "Void Items",
                "",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            DamageAmp = LoLItems.MyConfig.Bind(
                "Rabadons Deathcap",
                "Damage Amp",
                30f,
                "Amount of bonus percentage damage each item will grant."

            );
        }

        private static void CreateItem()
        {
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();
            myItemDef.name = "Rabadons";
            myItemDef.nameToken = "RabadonsDeathcap";
            myItemDef.pickupToken = "RabadonsDeathcapItem";
            myItemDef.descriptionToken = "RabadonsDeathcapDesc";
            myItemDef.loreToken = "RabadonsDeathcapLore";
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(Utilities.GetRarityFromString(Rarity.Value)).WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("RabadonsIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("RabadonsPrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
        }

        private static ItemDisplayRuleDict CreateDisplayRules()
        {
            ItemBodyModelPrefab = Assets.prefabs.LoadAsset<GameObject>("RabadonsPrefab");
            var itemDisplay = ItemBodyModelPrefab.AddComponent<ItemDisplay>();
            itemDisplay.rendererInfos = Utilities.ItemDisplaySetup(ItemBodyModelPrefab);
            
            ItemDisplayRuleDict rules = new();
            rules.Add("mdlCommandoDualies",
            [
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(-0.00109F, 0.30543F, 0.02332F),
                    localAngles = new Vector3(0F, 0F, 0F),
                    localScale = new Vector3(1F, 1F, 1F)
                }
            ]);
            rules.Add("mdlHuntress",
            [
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(-0.00443F, 0.26112F, -0.04558F),
                    localAngles = new Vector3(0F, 0F, 0F),
                    localScale = new Vector3(0.8F, 0.8F, 0.8F)
                }
            ]);
            rules.Add("mdlBandit2",
            [
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(-0.01902F, 0.10885F, -0.00051F),
                    localAngles = new Vector3(0F, 0F, 0F),
                    localScale = new Vector3(0.8F, 0.8F, 0.8F)
                }
            ]);
            rules.Add("mdlToolbot",
            [
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(-0.00801F, 2.62205F, 1.32762F),
                    localAngles = new Vector3(45F, 0F, 0F),
                    localScale = new Vector3(8F, 8F, 8F)
                }
            ]);
            rules.Add("mdlEngi",
            [
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "HeadCenter",
                    localPos = new Vector3(0F, 0F, 0F),
                    localAngles = new Vector3(0F, 0F, 0F),
                    localScale = new Vector3(1F, 1F, 1F)
                }
            ]);
            rules.Add("mdlEngiTurret",
            [
                new ItemDisplayRule //alt turret
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(0F, 0.74023F, 0F),
                    localAngles = new Vector3(0F, 0F, 0F),
                    localScale = new Vector3(3F, 3F, 3F)
                }
            ]);
            rules.Add("mdlMage",
            [
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(-0.00065F, 0.10994F, 0.0013F),
                    localAngles = new Vector3(15F, 0F, 0F),
                    localScale = new Vector3(0.7F, 0.7F, 0.7F)
                }
                
            ]);
            rules.Add("mdlMerc",
            [
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(-0.00079F, 0.18756F, 0.04691F),
                    localAngles = new Vector3(10F, 0F, 0F),
                    localScale = new Vector3(0.7F, 0.7F, 0.7F)
                }
            ]);
            rules.Add("mdlTreebot",
            [
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "FlowerBase",
                    localPos = new Vector3(0F, 1.50821F, 0F),
                    localAngles = new Vector3(0F, 0F, 0F),
                    localScale = new Vector3(3F, 3F, 3F)
                }
            ]);
            rules.Add("mdlLoader",
            [
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(-0.00093F, 0.17562F, -0.00083F),
                    localAngles = new Vector3(10F, 0F, 0F),
                    localScale = new Vector3(0.7F, 0.7F, 0.7F)
                }
            ]);
            rules.Add("mdlCroco",
            [
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(-0.03017F, 1.00393F, 1.37941F),
                    localAngles = new Vector3(90F, 0F, 0F),
                    localScale = new Vector3(6F, 6F, 6F)
                }
            ]);
            rules.Add("mdlCaptain",
            [
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(-0.00131F, 0.22096F, -0.01855F),
                    localAngles = new Vector3(327F, 0F, 0F),
                    localScale = new Vector3(0.6F, 0.6F, 0.6F)
                }
            ]);
            rules.Add("mdlRailGunner",
            [
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(-0.00046F, 0.16635F, -0.04072F),
                    localAngles = new Vector3(0F, 0F, 0F),
                    localScale = new Vector3(0.5F, 0.5F, 0.5F)
                }
            ]);
            rules.Add("mdlVoidSurvivor",
            [
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(-0.00355F, 0.118F, -0.03936F),
                    localAngles = new Vector3(330F, 0F, 0F),
                    localScale = new Vector3(0.6F, 0.6F, 0.6F)
                }
            ]);
            return rules;
        }

        private static void Hooks()
        {
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
                            float damageMultiplier = 1 + inventoryCount * (DamageAmp.Value / 100);
                            Utilities.AddValueInDictionary(ref rabadonsBonusDamage, attackerCharacterBody.master, damageInfo.damage * (damageMultiplier - 1), rabadonsBonusDamageToken);
                            damageInfo.damage = damageMultiplier * damageInfo.damage;
                        }
                    }
                }
                orig(self, damageInfo);
            };
        }

        private static (string, string) GetDisplayInformation(CharacterMaster masterRef)
        {
            if (masterRef == null)
                return (Language.GetString(myItemDef.descriptionToken), "");
            
            string customDescription = "";

            if (rabadonsBonusDamage.TryGetValue(masterRef.netId, out float damageDealt))
                customDescription += "<br><br>Damage dealt: " + string.Format("{0:#}", damageDealt);
            else
                customDescription += "<br><br>Damage dealt: 0";

            return (Language.GetString(myItemDef.descriptionToken), customDescription);
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("RabadonsDeathcap", "Rabadon\'s Deathcap");

            // Short description
            LanguageAPI.Add("RabadonsDeathcapItem", "Hat makes them go splat.");

            // Long description
            LanguageAPI.Add("RabadonsDeathcapDesc", "Do <style=cIsUtility>" + DamageAmp.Value + "%</style> <style=cStack>(+" + DamageAmp.Value + "%)</style> more damage.");

            // Lore
            LanguageAPI.Add("RabadonsDeathcapLore", "Makes you feel like a wizard.");
        }

        public static void SetupNetworkMappings()
        {
            LoLItems.networkMappings.Add(rabadonsBonusDamageToken, rabadonsBonusDamage);
        }
    }
}