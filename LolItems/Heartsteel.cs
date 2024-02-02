using System.Collections.Generic;
using R2API;
using RoR2.Orbs;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using BepInEx.Configuration;

namespace LoLItems
{
    internal class Heartsteel
    {

        // We need our item definition to persist through our functions, and therefore make it a class field.
        public static ItemDef myItemDef;
        public static BuffDef myTimerBuffDef;
        public static ConfigEntry<float> BonusHealthAmount { get; set; }
        public static ConfigEntry<float> DamageCooldown { get; set; }
        public static ConfigEntry<float> DamageBonus { get; set; }
        public static ConfigEntry<bool> Enabled { get; set; }
        public static ConfigEntry<string> Rarity { get; set; }
        public static ConfigEntry<string> VoidItems { get; set; }
        public static Dictionary<NetworkInstanceId, float> heartsteelHealth = [];
        public static string heartsteelHealthToken = "Heartsteel.heartsteelHealth";
        public static Dictionary<NetworkInstanceId, float> heartsteelBonusDamage = [];
        public static string heartsteelBonusDamageToken = "Heartsteel.heartsteelBonusDamage";
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = [];
        public static Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef = [];
        public static uint triggerSoundEffectID = 3202319100;

        internal static void Init()
        {
            LoadConfig();
            if (!Enabled.Value)
            {
                return;
            }

            CreateItem();
            CreateBuff();
            AddTokens();
            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            ContentAddition.AddBuffDef(myTimerBuffDef);
            Hooks();
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, Rarity, VoidItems, "Heartsteel");
            SetupNetworkMappings();
        }

        private static void LoadConfig()
        {
            Enabled = LoLItems.MyConfig.Bind(
                "Heartsteel",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            Rarity = LoLItems.MyConfig.Bind(
                "Heartsteel",
                "Rarity",
                "Tier3Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            VoidItems = LoLItems.MyConfig.Bind(
                "Heartsteel",
                "Void Items",
                "",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            BonusHealthAmount = LoLItems.MyConfig.Bind(
                "Heartsteel",
                "Health Per Kill",
                2f,
                "Amount of health per kill each item will grant."
            );

            DamageCooldown = LoLItems.MyConfig.Bind(
                "Heartsteel",
                "Damage Cooldown",
                10f,
                "The cooldown of the damage proc."
            );

            DamageBonus = LoLItems.MyConfig.Bind(
                "Heartsteel",
                "Bonus Damage Percent",
                50f,
                "The percentage of your health that will be dealt as damage."
            );
        }

        private static void CreateItem()
        {
            // First let's define our item
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();

            // Language Tokens, check AddTokens() below.
            myItemDef.name = "Heartsteel";
            myItemDef.nameToken = "Heartsteel";
            myItemDef.pickupToken = "HeartsteelItem";
            myItemDef.descriptionToken = "HeartsteelDesc";
            myItemDef.loreToken = "HeartsteelLore";
#pragma warning disable Publicizer001
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(Utilities.GetRarityFromString(Rarity.Value)).WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("HeartsteelIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("HeartsteelPrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = [ ItemTag.Healing, ItemTag.OnKillEffect ];
        }

        private static void CreateBuff()
        {
            // Create a timer to prevent stacks for a short period of time
            myTimerBuffDef = ScriptableObject.CreateInstance<BuffDef>();

            myTimerBuffDef.iconSprite = Assets.icons.LoadAsset<Sprite>("HeartsteelIcon");
            myTimerBuffDef.name = "Heartsteel Timer Debuff";
            myTimerBuffDef.canStack = false;
            myTimerBuffDef.isDebuff = true;
            myTimerBuffDef.isCooldown = true;
            myTimerBuffDef.isHidden = false;
            myTimerBuffDef.buffColor = Color.grey;
        }


        private static void Hooks()
        {
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
                        InfusionOrb HeartsteelOrb = new()
                        {
                            origin = vector,
                            target = Util.FindBodyMainHurtBox(damageReport.attackerBody),
                            maxHpValue = 0
                        };
                        OrbManager.instance.AddOrb(HeartsteelOrb);

                        Utilities.AddValueInDictionary(ref heartsteelHealth, damageReport.attackerMaster, BonusHealthAmount.Value * inventoryCount, heartsteelHealthToken, false);
					}
                }
            };

             On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, victim) =>
            {    
                orig(self, damageInfo, victim);
                if (damageInfo.attacker && damageInfo.procCoefficient > 0)
                {
                    CharacterBody attackerCharacterBody = damageInfo.attacker.GetComponent<CharacterBody>();
                    CharacterBody victimCharacterBody = victim.GetComponent<CharacterBody>();
                    
                    if (attackerCharacterBody?.inventory)
                    {
                        int inventoryCount = attackerCharacterBody.inventory.GetItemCount(myItemDef.itemIndex);
                        if (inventoryCount > 0 && !attackerCharacterBody.HasBuff(myTimerBuffDef))
                        {
                            attackerCharacterBody.healthComponent.body.AddTimedBuff(myTimerBuffDef, DamageCooldown.Value);
                            float damage = attackerCharacterBody.healthComponent.fullHealth * inventoryCount * DamageBonus.Value / 100 * damageInfo.procCoefficient;
                            DamageInfo onHitProc = damageInfo;
                            onHitProc.procCoefficient = 1f;
                            onHitProc.damageType = DamageType.Generic;
                            onHitProc.inflictor = damageInfo.attacker;
                            onHitProc.damage = damage;
                            onHitProc.damageColorIndex = DamageColorIndex.Item;
                            victimCharacterBody.healthComponent.TakeDamage(onHitProc);
                            Utilities.AddValueInDictionary(ref heartsteelBonusDamage, attackerCharacterBody.master, damage, heartsteelBonusDamageToken, false);
                            AkSoundEngine.PostEvent(triggerSoundEffectID, damageInfo.attacker.gameObject);
                        }
                    }
                }

            };

            RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        private static void RecalculateStatsAPI_GetStatCoefficients(CharacterBody characterBody, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (characterBody.master?.netId != null && heartsteelHealth.TryGetValue(characterBody.master.netId, out float extraHealth))
                args.baseHealthAdd += extraHealth;
        }

        private static (string, string) GetDisplayInformation(CharacterMaster masterRef)
        {
            if (masterRef == null)
                return (Language.GetString(myItemDef.descriptionToken), "");
            
            string customDescription = "";

            if (heartsteelHealth.TryGetValue(masterRef.netId, out float healthGained))
                customDescription += "<br><br>Health gained: " + string.Format("{0:#}", healthGained);
            else
                customDescription += "<br><br>Health gained: 0";

            if (heartsteelBonusDamage.TryGetValue(masterRef.netId, out float damageDealt))
                customDescription += "<br>Damage dealt: " + string.Format("{0:#}", damageDealt);
            else
                customDescription += "<br>Damage dealt: 0";

            return (Language.GetString(myItemDef.descriptionToken), customDescription);
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("Heartsteel", "Heartsteel");

            // Short description
            LanguageAPI.Add("HeartsteelItem", "Gain permanent health on kill with no cap. Every few seconds deal a portion of your health as extra damage on hit.");

            // Long description
            LanguageAPI.Add("HeartsteelDesc", "Adds <style=cIsHealth>" + BonusHealthAmount.Value + "</style> <style=cStack>(+" + BonusHealthAmount.Value + ")</style> base health per kill with no cap. Every <style=cIsUtility>" + DamageCooldown.Value + "</style> seconds deal <style=cIsDamage>" + DamageBonus.Value + "%</style> of your max health as damage on hit.");

            // Lore
            LanguageAPI.Add("HeartsteelLore", "Lore was meant to go here, but Sion trampled it.");
        }

        public static void SetupNetworkMappings()
        {
            LoLItems.networkMappings.Add(heartsteelBonusDamageToken, heartsteelBonusDamage);
            LoLItems.networkMappings.Add(heartsteelHealthToken, heartsteelHealth);
        }
    }
}