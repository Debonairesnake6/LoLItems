using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
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
        public static DotController.DotIndex myDotDefIndex;
        public static int damageColourIndex = 0;

        public static ConfigEntry<float> BurnDamagePercent { get; set; }
        public static ConfigEntry<float> BurnDamageDuration { get; set; }
        public static ConfigEntry<float> BurnDamageMin { get; set; }
        public static ConfigEntry<float> BurnDamageMax { get; set; }
        public static ConfigEntry<bool> Enabled { get; set; }
        public static ConfigEntry<string> Rarity { get; set; }
        public static ConfigEntry<string> VoidItems { get; set; }
        public static Dictionary<NetworkInstanceId, float> liandrysDamageDealt = [];
        public static string liandrysDamageDealtToken = "Liandrys.liandrysDamageDealt";
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
            CreateBuff();
            CreateDot();
            AddTokens();
            var displayRules = new ItemDisplayRuleDict();
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            ContentAddition.AddBuffDef(myBuffDef);
            DotAPI.CustomDotBehaviour myDotCustomBehaviour = AddCustomDotBehaviour;
            myDotDefIndex = DotAPI.RegisterDotDef(myDotDef, myDotCustomBehaviour);
            Hooks();
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, Rarity, VoidItems, "Liandrys");
            SetupNetworkMappings();
        }

        private static void LoadConfig()
        {
            Enabled = LoLItems.MyConfig.Bind(
                "Liandrys Anguish",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            Rarity = LoLItems.MyConfig.Bind(
                "Liandrys Anguish",
                "Rarity",
                "Tier2Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            VoidItems = LoLItems.MyConfig.Bind(
                "Liandrys Anguish",
                "Void Items",
                "",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            BurnDamagePercent = LoLItems.MyConfig.Bind(
                "Liandrys Anguish",
                "Max Health Burn Percent",
                2.5f,
                "Amount of max health percent burn each item will grant."
            );

            BurnDamageDuration = LoLItems.MyConfig.Bind(
                "Liandrys Anguish",
                "Burn Duration",
                5f,
                "Duration of the burn."
            );

            BurnDamageMin = LoLItems.MyConfig.Bind(
                "Liandrys Anguish",
                "Minimum Burn Damage",
                0.5f * BurnDamageDuration.Value,
                "Minimum burn damage. This will be multiplied by your base damage."
            );

            BurnDamageMax = LoLItems.MyConfig.Bind(
                "Liandrys Anguish",
                "Maximum Burn Damage",
                25f * BurnDamageDuration.Value,
                "Maximum burn damage. This will be multiplied by your base damage."
            );
        }

        private static void CreateItem()
        {
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();
            myItemDef.name = "LiandrysAnguish";
            myItemDef.nameToken = "Liandrys";
            myItemDef.pickupToken = "LiandrysItem";
            myItemDef.descriptionToken = "LiandrysDesc";
            myItemDef.loreToken = "LiandrysLore";
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = LegacyResourcesAPI.Load<ItemTierDef>(Utilities.GetRarityFromString(Rarity.Value));
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = MyAssets.icons.LoadAsset<Sprite>("LiandrysIcon");
#pragma warning disable CS0618
            myItemDef.pickupModelPrefab = MyAssets.prefabs.LoadAsset<GameObject>("LiandrysPrefab");
#pragma warning restore CS0618
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = [ ItemTag.Damage ];
        }

        private static void CreateBuff()
        {
            myBuffDef = ScriptableObject.CreateInstance<BuffDef>();

            myBuffDef.iconSprite = MyAssets.icons.LoadAsset<Sprite>("LiandrysIcon");
            myBuffDef.name = "Liandry\'s Anguish Buff";
            myBuffDef.canStack = false;
            myBuffDef.isDebuff = true;
            myBuffDef.isCooldown = true;
            myBuffDef.isHidden = false;

        }
        
        private static void CreateDot()
        {
            damageColourIndex = (int)DamageColorIndex.Count + 1;
            myDotDef = new DotController.DotDef
            {
                damageColorIndex = (DamageColorIndex)damageColourIndex,
                associatedBuff = myBuffDef,
                terminalTimedBuff = myBuffDef,
                terminalTimedBuffDuration = BurnDamageDuration.Value,
                resetTimerOnAdd = true,
                interval = 1f,
                damageCoefficient = 1f / BurnDamageDuration.Value,
            };
        }

        public static void AddCustomDotBehaviour(DotController self, DotController.DotStack dotStack)
        {
            if (dotStack.dotIndex == myDotDefIndex)
            {
                CharacterBody attackerCharacterBody = dotStack.attackerObject.GetComponent<CharacterBody>();
                int inventoryCount = 1;
                if (attackerCharacterBody?.inventory)
                    inventoryCount = attackerCharacterBody.inventory.GetItemCountEffective(myItemDef.itemIndex);
#pragma warning disable Publicizer001
                float baseDotDamage = self.victimBody.maxHealth * BurnDamagePercent.Value / 100f / BurnDamageDuration.Value * myDotDef.interval;
#pragma warning restore Publicizer001
                float dotDamage = Math.Max(BurnDamageMin.Value * attackerCharacterBody.damage, Math.Min(BurnDamageMax.Value * attackerCharacterBody.damage, baseDotDamage)) / BurnDamageDuration.Value * inventoryCount;
                dotStack.damage = dotDamage;
            }
        }


        private static void Hooks()
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
                        int inventoryCount = attackerCharacterBody.inventory.GetItemCountEffective(myItemDef.itemIndex);
                        if (inventoryCount > 0)
                        {
                            victimCharacterBody.AddTimedBuff(myBuffDef, BurnDamageDuration.Value);

                            float baseDotDamage = victimCharacterBody.maxHealth * BurnDamagePercent.Value / 100f * inventoryCount;
                            float dotDamage = Math.Max(BurnDamageMin.Value * attackerCharacterBody.damage, Math.Min(BurnDamageMax.Value * attackerCharacterBody.damage, baseDotDamage));
                            InflictDotInfo inflictDotInfo = new()
                            {
                                victimObject = victimCharacterBody.healthComponent.gameObject,
                                attackerObject = attackerCharacterBody.gameObject,
                                totalDamage = dotDamage,
                                dotIndex = myDotDefIndex,
                                duration = BurnDamageDuration.Value,
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
                        int inventoryCount = attackerCharacterBody.inventory.GetItemCountEffective(myItemDef.itemIndex);
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
                customDescription += "<br><br>Damage dealt: " + string.Format("{0:#, ##0.##}", damageDealt);
            else
                customDescription += "<br><br>Damage dealt: 0";

            return (Language.GetString(myItemDef.descriptionToken), customDescription);
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("Liandrys", "Liandry\'s Anguish");

            // Short description
            LanguageAPI.Add("LiandrysItem", "Burn enemies on hit for a % of their max health.");

            // Long description
            LanguageAPI.Add("LiandrysDesc", "On hit burn enemies for <style=cIsDamage>" + BurnDamagePercent.Value + "%</style> <style=cStack>(+" + BurnDamagePercent.Value + "%)</style> max health over " + BurnDamageDuration.Value + " seconds.");

            // Lore
            LanguageAPI.Add("LiandrysLore", "A crying mask is a great halloween costume.");

            // ENABLE for buff
            LanguageAPI.Add("LiandrysBuff", "Liandry\'s Anguish is burning this unit.");
        }

        public static void SetupNetworkMappings()
        {
            LoLItems.networkMappings.Add(liandrysDamageDealtToken, liandrysDamageDealt);
        }
    }
}