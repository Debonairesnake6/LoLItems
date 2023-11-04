using System.Data;
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
using System.Collections;
using BepInEx.Configuration;

namespace LoLItems
{
    internal class Heartsteel
    {

        //We need our item definition to persist through our functions, and therefore make it a class field.
        public static ItemDef myItemDef;
        public static BuffDef myTimerBuffDef;
        public static ConfigEntry<float> bonusHealthAmount { get; set; }
        public static ConfigEntry<float> damageCooldown { get; set; }
        public static ConfigEntry<float> damageBonus { get; set; }
        public static ConfigEntry<bool> enabled { get; set; }
        public static ConfigEntry<string> rarity { get; set; }
        public static ConfigEntry<string> voidItems { get; set; }
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> heartsteelHealth = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> originalBaseMaxHealth = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> heartsteelBonusDamage = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = new Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster>();
        public static Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef = new Dictionary<RoR2.UI.ItemIcon, CharacterMaster>();
        public static uint triggerSoundEffectID = 3202319100;

        internal static void Init()
        {
            LoadConfig();
            if (!enabled.Value)
            {
                return;
            }

            CreateItem();
            CreateBuff();
            AddTokens();
            ItemDisplayRuleDict displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            ContentAddition.AddBuffDef(myTimerBuffDef);
            hooks();
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, rarity, voidItems, "Heartsteel");
        }

        private static void LoadConfig()
        {
            enabled = LoLItems.MyConfig.Bind<bool>(
                "Heartsteel",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            rarity = LoLItems.MyConfig.Bind<string>(
                "Heartsteel",
                "Rarity",
                "Tier3Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            voidItems = LoLItems.MyConfig.Bind<string>(
                "Heartsteel",
                "Void Items",
                "",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            bonusHealthAmount = LoLItems.MyConfig.Bind<float>(
                "Heartsteel",
                "Health Per Kill",
                2f,
                "Amount of health per kill each item will grant."

            );

            damageCooldown = LoLItems.MyConfig.Bind<float>(
                "Heartsteel",
                "Damage Cooldown",
                10f,
                "The cooldown of the damage proc."

            );

            damageBonus = LoLItems.MyConfig.Bind<float>(
                "Heartsteel",
                "Bonus Damage Percent",
                50f,
                "The percentage of your health that will be dealt as damage."

            );
        }

        private static void CreateItem()
        {
            //First let's define our item
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();

            // Language Tokens, check AddTokens() below.
            myItemDef.name = "Heartsteel";
            myItemDef.nameToken = "Heartsteel";
            myItemDef.pickupToken = "HeartsteelItem";
            myItemDef.descriptionToken = "HeartsteelDesc";
            myItemDef.loreToken = "HeartsteelLore";
#pragma warning disable Publicizer001
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(Utilities.GetRarityFromString(rarity.Value)).WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("HeartsteelIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("HeartsteelPrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = new ItemTag[2] { ItemTag.Healing, ItemTag.OnKillEffect };
        }

        private static void CreateBuff()
        {
            // Create a timer to prevent stacks for a short period of time
            myTimerBuffDef = ScriptableObject.CreateInstance<BuffDef>();

            myTimerBuffDef.iconSprite = Assets.icons.LoadAsset<Sprite>("HeartsteelIcon");
            myTimerBuffDef.name = "HeartsteelTimerBuff";
            myTimerBuffDef.canStack = false;
            myTimerBuffDef.isDebuff = true;
            myTimerBuffDef.isCooldown = true;
            myTimerBuffDef.isHidden = false;
            myTimerBuffDef.buffColor = Color.grey;
        }


        private static void hooks()
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
                        InfusionOrb HeartsteelOrb = new InfusionOrb();
                        HeartsteelOrb.origin = vector;
                        HeartsteelOrb.target = Util.FindBodyMainHurtBox(damageReport.attackerBody);
                        HeartsteelOrb.maxHpValue = 0;
                        OrbManager.instance.AddOrb(HeartsteelOrb);

                        Utilities.AddValueInDictionary(ref heartsteelHealth, damageReport.attackerMaster, bonusHealthAmount.Value * inventoryCount, false);
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
                            attackerCharacterBody.healthComponent.body.AddTimedBuff(myTimerBuffDef, damageCooldown.Value);
                            float damage = attackerCharacterBody.healthComponent.fullHealth * inventoryCount * damageBonus.Value / 100 * damageInfo.procCoefficient;
                            DamageInfo onHitProc = damageInfo;
                            onHitProc.procCoefficient = 1f;
                            onHitProc.damageType = DamageType.Generic;
                            onHitProc.inflictor = damageInfo.attacker;
                            onHitProc.damage = damage;
                            onHitProc.damageColorIndex = DamageColorIndex.Item;
                            victimCharacterBody.healthComponent.TakeDamage(onHitProc);
                            Utilities.AddValueInDictionary(ref heartsteelBonusDamage, attackerCharacterBody.master, damage, false);
                            AkSoundEngine.PostEvent(triggerSoundEffectID, damageInfo.attacker.gameObject);
                        }
                    }
                }

            };

            On.RoR2.CharacterBody.Start += (orig, self) =>
            {
                orig(self);
                if (self.master != null && !originalBaseMaxHealth.ContainsKey(self.master.netId))
                {
                    Utilities.SetValueInDictionary(ref originalBaseMaxHealth, self.master, self.baseMaxHealth, false);
                }
            };
            
            On.RoR2.CharacterBody.RecalculateStats += (orig, self) =>
            {
                if (self.inventory != null && self.inventory.GetItemCount(myItemDef.itemIndex) > 0 && originalBaseMaxHealth.TryGetValue(self.master.netId, out float baseHealth) && heartsteelHealth.TryGetValue(self.master.netId, out float extraHealth))
                {
                    self.baseMaxHealth = baseHealth + extraHealth;
                }
                orig(self);
            };
        }

        private static string GetDisplayInformation(CharacterMaster masterRef)
        {
            // Update the description for an item in the HUD
            if (masterRef != null && heartsteelHealth.TryGetValue(masterRef.netId, out float healthGained) && heartsteelBonusDamage.TryGetValue(masterRef.netId, out float damageDealt)){
                return Language.GetString(myItemDef.descriptionToken) 
                + "<br><br>Health gained: " + String.Format("{0:#}", healthGained)
                + "<br>Damage dealt: " + String.Format("{0:#}", damageDealt);
            }
            return Language.GetString(myItemDef.descriptionToken);
        }

        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            //The Name should be self explanatory
            LanguageAPI.Add("Heartsteel", "Heartsteel");

            //The Pickup is the short text that appears when you first pick this up. This text should be short and to the point, numbers are generally ommited.
            LanguageAPI.Add("HeartsteelItem", "Gain permanent health on kill with no cap. Every few seconds deal a portion of your health as extra damage on hit.");

            //The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("HeartsteelDesc", "Adds <style=cIsHealth>" + bonusHealthAmount.Value + "</style> <style=cStack>(+" + bonusHealthAmount.Value + ")</style> base health per kill with no cap. Every <style=cIsUtility>" + damageCooldown.Value + "</style> seconds deal <style=cIsDamage>" + damageBonus.Value + "%</style> of your max health as damage on hit.");

            //The Lore is, well, flavor. You can write pretty much whatever you want here.
            LanguageAPI.Add("HeartsteelLore", "Lore was meant to go here, but Sion trampled it.");
        }
    }
}