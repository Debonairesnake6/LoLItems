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
using BepInEx.Configuration;

namespace LoLItems
{
    internal class Liandrys
    {

        public static ItemDef myItemDef;
        public static BuffDef myBuffDef;
        public static DotController.DotDef myDotDef;
        public static RoR2.DotController.DotIndex myDotDefIndex;
        public static int damageColourIndex = 0;

        public static ConfigEntry<float> burnDamagePercent { get; set; }
        public static ConfigEntry<float> burnDamageDuration { get; set; }
        public static ConfigEntry<float> burnDamageMin { get; set; }
        public static ConfigEntry<float> burnDamageMax { get; set; }
        public static ConfigEntry<bool> enabled { get; set; }
        public static ConfigEntry<string> rarity { get; set; }
        public static ConfigEntry<string> voidItems { get; set; }
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> liandrysDamageDealt = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static string liandrysDamageDealtToken = "Liandrys.liandrysDamageDealt";
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = new Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster>();
        public static Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef = new Dictionary<RoR2.UI.ItemIcon, CharacterMaster>();

        internal static void Init()
        {
            LoadConfig();
            if (!enabled.Value)
            {
                return;
            }

            CreateItem();
            CreateBuff();
            CreateDot();
            AddTokens();
            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            ContentAddition.AddBuffDef(myBuffDef);
            DotAPI.CustomDotBehaviour myDotCustomBehaviour = AddCustomDotBehaviour;
            myDotDefIndex = DotAPI.RegisterDotDef(myDotDef, myDotCustomBehaviour);
            hooks();
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, rarity, voidItems, "Liandrys");
            SetupNetworkMappings();
        }

        private static void LoadConfig()
        {
            enabled = LoLItems.MyConfig.Bind<bool>(
                "Liandrys",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            rarity = LoLItems.MyConfig.Bind<string>(
                "Liandrys",
                "Rarity",
                "Tier2Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            voidItems = LoLItems.MyConfig.Bind<string>(
                "Liandrys",
                "Void Items",
                "",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            burnDamagePercent = LoLItems.MyConfig.Bind<float>(
                "Liandrys",
                "Max Health Burn Percent",
                2.5f,
                "Amount of max health percent burn each item will grant."

            );

            burnDamageDuration = LoLItems.MyConfig.Bind<float>(
                "Liandrys",
                "Burn Duration",
                5f,
                "Duration of the burn."

            );

            burnDamageMin = LoLItems.MyConfig.Bind<float>(
                "Liandrys",
                "Minimum Burn Damage",
                0.5f * burnDamageDuration.Value,
                "Minimum burn damage. This will be multiplied by your base damage."

            );

            burnDamageMax = LoLItems.MyConfig.Bind<float>(
                "Liandrys",
                "Maximum Burn Damage",
                25f * burnDamageDuration.Value,
                "Maximum burn damage. This will be multiplied by your base damage."

            );
        }

        private static void CreateItem()
        {
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();
            myItemDef.name = "Liandrys";
            myItemDef.nameToken = "Liandrys";
            myItemDef.pickupToken = "LiandrysItem";
            myItemDef.descriptionToken = "LiandrysDesc";
            myItemDef.loreToken = "LiandrysLore";
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(Utilities.GetRarityFromString(rarity.Value)).WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("LiandrysIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("LiandrysPrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
        }

        private static void CreateBuff()
        {
            myBuffDef = ScriptableObject.CreateInstance<BuffDef>();

            myBuffDef.iconSprite = Assets.icons.LoadAsset<Sprite>("LiandrysIcon");
            myBuffDef.name = "LiandrysBuff";
            myBuffDef.canStack = false;
            myBuffDef.isDebuff = true;
            myBuffDef.isCooldown = true;
            myBuffDef.isHidden = false;

        }
        
        private static void CreateDot()
        {
            damageColourIndex = (int)RoR2.DamageColorIndex.Count + 1;
            myDotDef = new DotController.DotDef
            {
                damageColorIndex = (RoR2.DamageColorIndex)damageColourIndex,
                associatedBuff = myBuffDef,
                terminalTimedBuff = myBuffDef,
                terminalTimedBuffDuration = burnDamageDuration.Value,
                resetTimerOnAdd = true,
                interval = 1f,
                damageCoefficient = 1f / burnDamageDuration.Value,
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
                float baseDotDamage = self.victimBody.maxHealth * burnDamagePercent.Value / 100f / burnDamageDuration.Value * myDotDef.interval;
#pragma warning restore Publicizer001
                float dotDamage = Math.Max(burnDamageMin.Value * attackerCharacterBody.damage, Math.Min(burnDamageMax.Value * attackerCharacterBody.damage, baseDotDamage)) / burnDamageDuration.Value * inventoryCount;
                dotStack.damage = dotDamage;
            }
        }


        private static void hooks()
        {
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
                            victimCharacterBody.AddTimedBuff(myBuffDef, burnDamageDuration.Value);

                            float baseDotDamage = victimCharacterBody.maxHealth * burnDamagePercent.Value / 100f * inventoryCount;
                            float dotDamage = Math.Max(burnDamageMin.Value * attackerCharacterBody.damage, Math.Min(burnDamageMax.Value * attackerCharacterBody.damage, baseDotDamage));
                            InflictDotInfo inflictDotInfo = new InflictDotInfo
                            {
                                victimObject = victimCharacterBody.healthComponent.gameObject,
                                attackerObject = attackerCharacterBody.gameObject,
                                totalDamage = dotDamage,
                                dotIndex = myDotDefIndex,
                                duration = burnDamageDuration.Value,
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
                            Utilities.AddValueInDictionary(ref liandrysDamageDealt, attackerCharacterBody.master, damageInfo.damage, liandrysDamageDealtToken);
                        }
                    }
                }
                orig(self, damageInfo);
            };

            // Used for custom colours
            On.RoR2.DamageColor.FindColor += (orig, colorIndex) =>
            {
                if (damageColourIndex == (int)colorIndex) return Color.blue;
                return orig(colorIndex);
            };
        }

        private static (string, string) GetDisplayInformation(CharacterMaster masterRef)
        {
            if (masterRef == null)
                return (Language.GetString(myItemDef.descriptionToken), "");
            
            string customDescription = "";

            if (liandrysDamageDealt.TryGetValue(masterRef.netId, out float damageDealt))
                customDescription += "<br><br>Damage dealt: " + String.Format("{0:#}", damageDealt);
            else
                customDescription += "<br><br>Damage dealt: 0";

            return (Language.GetString(myItemDef.descriptionToken), customDescription);
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            //The Name should be self explanatory
            LanguageAPI.Add("Liandrys", "Liandrys");

            //The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("LiandrysItem", "Burn enemies on hit for a % of their max health");

            //The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("LiandrysDesc", "On hit burn enemies for <style=cIsDamage>" + burnDamagePercent.Value + "%</style> <style=cStack>(+" + burnDamagePercent.Value + "%)</style> max health over " + burnDamageDuration.Value + " seconds");

            //The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("LiandrysLore", "A crying mask is a great halloween costume.");

            // ENABLE for buff
            LanguageAPI.Add("LiandrysBuff", "Liandrys is burning this unit");
        }

        public static void SetupNetworkMappings()
        {
            LoLItems.networkMappings.Add(liandrysDamageDealtToken, liandrysDamageDealt);
        }
    }
}