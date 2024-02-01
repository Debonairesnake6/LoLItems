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
using UnityEngine.Networking;

namespace LoLItems
{
    internal class MyExampleBaseItem
    {
        public static ItemDef myItemDef;
        // ENABLE for buff
        // public static BuffDef myBuffDef;

        public static ConfigEntry<float> exampleValue { get; set; }
        public static ConfigEntry<bool> enabled { get; set; }
        public static ConfigEntry<string> rarity { get; set; }
        public static ConfigEntry<string> voidItems { get; set; }
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> exampleStoredValue = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static string exampleStoredValueToken = "MyExampleBaseItem.exampleStoredValue";
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
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, rarity, voidItems, "MyExampleItem");
            SetupNetworkMappings();
        }

        private static void LoadConfig()
        {
            enabled = LoLItems.MyConfig.Bind<bool>(
                "MyExampleItem",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            rarity = LoLItems.MyConfig.Bind<string>(
                "MyExampleItem",
                "Rarity",
                "Tier1Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            voidItems = LoLItems.MyConfig.Bind<string>(
                "MyExampleItem",
                "Void Items",
                "",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            exampleValue = LoLItems.MyConfig.Bind<float>(
                "MyExampleItem",
                "Item Value",
                1f,
                "Amount of value each item will grant."

            );
        }

        private static void CreateItem()
        {
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();
            myItemDef.name = "MyExampleItem";  // Replace this string throughout the entire file with your new item name
            myItemDef.nameToken = "MyExampleItem";
            myItemDef.pickupToken = "MyExampleItemItem";
            myItemDef.descriptionToken = "MyExampleItemDesc";
            myItemDef.loreToken = "MyExampleItemLore";
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(Utilities.GetRarityFromString(rarity.Value)).WaitForCompletion();
#pragma warning restore Publicizer001
            // DEFAULT icons
            myItemDef.pickupIconSprite = Resources.Load<Sprite>("Textures/MiscIcons/texMysteryIcon");
            myItemDef.pickupModelPrefab = Resources.Load<GameObject>("Prefabs/PickupModels/PickupMystery");
            // ENABLE for custom assets
            // myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("MyExampleItemIcon");
            // myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("MyExampleItemPrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = new ItemTag[4] { ItemTag.Damage, ItemTag.Healing, ItemTag.Utility, ItemTag.OnKillEffect };
        }

        // ENABLE for buff
        // private static void CreateBuff()
        // {
        //     myBuffDef = ScriptableObject.CreateInstance<BuffDef>();

        //     myBuffDef.iconSprite = Resources.Load<Sprite>("Textures/MiscIcons/texMysteryIcon");
        //     //  ENABLE for custom assets
        //     // myBuffDef.iconSprite = Assets.icons.LoadAsset<Sprite>("MyExampleItemIcon");
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
                        // InfusionOrb MyExampleItemOrb = new InfusionOrb();
                        // MyExampleItemOrb.origin = vector;
                        // MyExampleItemOrb.target = Util.FindBodyMainHurtBox(damageReport.attackerBody);
                        // MyExampleItemOrb.maxHpValue = 0;
                        // OrbManager.instance.AddOrb(MyExampleItemOrb);
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

        // public static ItemDisplayRuleDict SetupItemDisplays()
        // {
        //     GameObject ItemBodyModelPrefab = Assets.prefabs.LoadAsset<GameObject>("RabadonsPrefab");
        //     RoR2.ItemDisplay itemDisplay = ItemBodyModelPrefab.AddComponent<ItemDisplay>();
        //     itemDisplay.rendererInfos = Utilities.ItemDisplaySetup(ItemBodyModelPrefab);

        //     ItemDisplayRuleDict rules = new ItemDisplayRuleDict();
        //                 rules.Add("mdlCommandoDualies", new RoR2.ItemDisplayRule[]
        //     {
        //         new RoR2.ItemDisplayRule
        //         {
        //             ruleType = ItemDisplayRuleType.ParentedPrefab,
        //             followerPrefab = ItemBodyModelPrefab,
        //             childName = "Head",
        //             localPos = new Vector3(-0.00109F, 0.30543F, 0.02332F),
        //             localAngles = new Vector3(0F, 0F, 0F),
        //             localScale = new Vector3(1F, 1F, 1F)
        //         }
        //     });
        //     rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]
        //     {
        //         new RoR2.ItemDisplayRule
        //         {
        //             ruleType = ItemDisplayRuleType.ParentedPrefab,
        //             followerPrefab = ItemBodyModelPrefab,
        //             childName = "Head",
        //             localPos = new Vector3(-0.00443F, 0.26112F, -0.04558F),
        //             localAngles = new Vector3(0F, 0F, 0F),
        //             localScale = new Vector3(0.8F, 0.8F, 0.8F)
        //         }
        //     });
        //     rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]
        //     {
        //         new RoR2.ItemDisplayRule
        //         {
        //             ruleType = ItemDisplayRuleType.ParentedPrefab,
        //             followerPrefab = ItemBodyModelPrefab,
        //             childName = "Head",
        //             localPos = new Vector3(-0.01902F, 0.10885F, -0.00051F),
        //             localAngles = new Vector3(0F, 0F, 0F),
        //             localScale = new Vector3(0.8F, 0.8F, 0.8F)
        //         }
        //     });
        //     rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]
        //     {
        //         new RoR2.ItemDisplayRule
        //         {
        //             ruleType = ItemDisplayRuleType.ParentedPrefab,
        //             followerPrefab = ItemBodyModelPrefab,
        //             childName = "Head",
        //             localPos = new Vector3(-0.00801F, 2.62205F, 1.32762F),
        //             localAngles = new Vector3(45F, 0F, 0F),
        //             localScale = new Vector3(8F, 8F, 8F)
        //         }
        //     });
        //     rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]
        //     {
        //         new RoR2.ItemDisplayRule
        //         {
        //             ruleType = ItemDisplayRuleType.ParentedPrefab,
        //             followerPrefab = ItemBodyModelPrefab,
        //             childName = "HeadCenter",
        //             localPos = new Vector3(0F, 0F, 0F),
        //             localAngles = new Vector3(0F, 0F, 0F),
        //             localScale = new Vector3(1F, 1F, 1F)
        //         }
        //     });
        //     rules.Add("mdlEngiTurret", new RoR2.ItemDisplayRule[]
        //     {
        //         new RoR2.ItemDisplayRule //alt turret
        //         {
        //             ruleType = ItemDisplayRuleType.ParentedPrefab,
        //             followerPrefab = ItemBodyModelPrefab,
        //             childName = "Head",
        //             localPos = new Vector3(0F, 0.74023F, 0F),
        //             localAngles = new Vector3(0F, 0F, 0F),
        //             localScale = new Vector3(3F, 3F, 3F)
        //         }
        //     });
        //     rules.Add("mdlMage", new RoR2.ItemDisplayRule[]
        //     {
        //         new RoR2.ItemDisplayRule
        //         {
        //             ruleType = ItemDisplayRuleType.ParentedPrefab,
        //             followerPrefab = ItemBodyModelPrefab,
        //             childName = "Head",
        //             localPos = new Vector3(-0.00065F, 0.10994F, 0.0013F),
        //             localAngles = new Vector3(15F, 0F, 0F),
        //             localScale = new Vector3(0.7F, 0.7F, 0.7F)
        //         }
                
        //     });
        //     rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]
        //     {
        //         new RoR2.ItemDisplayRule
        //         {
        //             ruleType = ItemDisplayRuleType.ParentedPrefab,
        //             followerPrefab = ItemBodyModelPrefab,
        //             childName = "Head",
        //             localPos = new Vector3(-0.00079F, 0.18756F, 0.04691F),
        //             localAngles = new Vector3(10F, 0F, 0F),
        //             localScale = new Vector3(0.7F, 0.7F, 0.7F)
        //         }
        //     });
        //     rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]
        //     {
        //         new RoR2.ItemDisplayRule
        //         {
        //             ruleType = ItemDisplayRuleType.ParentedPrefab,
        //             followerPrefab = ItemBodyModelPrefab,
        //             childName = "FlowerBase",
        //             localPos = new Vector3(0F, 1.50821F, 0F),
        //             localAngles = new Vector3(0F, 0F, 0F),
        //             localScale = new Vector3(3F, 3F, 3F)
        //         }
        //     });
        //     rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]
        //     {
        //         new RoR2.ItemDisplayRule
        //         {
        //             ruleType = ItemDisplayRuleType.ParentedPrefab,
        //             followerPrefab = ItemBodyModelPrefab,
        //             childName = "Head",
        //             localPos = new Vector3(-0.00093F, 0.17562F, -0.00083F),
        //             localAngles = new Vector3(10F, 0F, 0F),
        //             localScale = new Vector3(0.7F, 0.7F, 0.7F)
        //         }
        //     });
        //     rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]
        //     {
        //         new RoR2.ItemDisplayRule
        //         {
        //             ruleType = ItemDisplayRuleType.ParentedPrefab,
        //             followerPrefab = ItemBodyModelPrefab,
        //             childName = "Head",
        //             localPos = new Vector3(-0.03017F, 1.00393F, 1.37941F),
        //             localAngles = new Vector3(90F, 0F, 0F),
        //             localScale = new Vector3(6F, 6F, 6F)
        //         }
        //     });
        //     rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]
        //     {
        //         new RoR2.ItemDisplayRule
        //         {
        //             ruleType = ItemDisplayRuleType.ParentedPrefab,
        //             followerPrefab = ItemBodyModelPrefab,
        //             childName = "Head",
        //             localPos = new Vector3(-0.00131F, 0.22096F, -0.01855F),
        //             localAngles = new Vector3(327F, 0F, 0F),
        //             localScale = new Vector3(0.6F, 0.6F, 0.6F)
        //         }
        //     });
        //     rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]
        //     {
        //         new RoR2.ItemDisplayRule
        //         {
        //             ruleType = ItemDisplayRuleType.ParentedPrefab,
        //             followerPrefab = ItemBodyModelPrefab,
        //             childName = "Head",
        //             localPos = new Vector3(-0.00046F, 0.16635F, -0.04072F),
        //             localAngles = new Vector3(0F, 0F, 0F),
        //             localScale = new Vector3(0.5F, 0.5F, 0.5F)
        //         }
        //     });
        //     rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]
        //     {
        //         new RoR2.ItemDisplayRule
        //         {
        //             ruleType = ItemDisplayRuleType.ParentedPrefab,
        //             followerPrefab = ItemBodyModelPrefab,
        //             childName = "Head",
        //             localPos = new Vector3(-0.00355F, 0.118F, -0.03936F),
        //             localAngles = new Vector3(330F, 0F, 0F),
        //             localScale = new Vector3(0.6F, 0.6F, 0.6F)
        //         }
        //     });
        //     return rules;
        // }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgments!
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

        public static void SetupNetworkMappings()
        {
            LoLItems.networkMappings.Add(exampleStoredValueToken, exampleStoredValue);
        }
    }
}