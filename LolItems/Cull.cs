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
    internal class Cull
    {
        public static ItemDef myItemDef;
        public static BuffDef myBuffDef;

        public static ConfigEntry<float> goldOnKill { get; set; }
        public static ConfigEntry<float> killsToBreak { get; set; }
        public static ConfigEntry<bool> enabled { get; set; }
        public static ConfigEntry<string> rarity { get; set; }
        public static ConfigEntry<string> voidItems { get; set; }
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> goldGained = new();
        public static string goldGainedToken = "Cull.goldGained";
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> buffStacks = new();
        public static string buffStacksToken = "Cull.buffStacks";
        public static Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef = new();
        public static Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef = new();

        // This runs when loading the file
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
            ItemDisplayRuleDict displayRules = new(null);
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            ContentAddition.AddBuffDef(myBuffDef);
            hooks();
            Utilities.SetupReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation, rarity, voidItems, "Cull");
            SetupNetworkMappings();
        }

        private static void LoadConfig()
        {
            enabled = LoLItems.MyConfig.Bind<bool>(
                "Cull",
                "Enabled",
                true,
                "Determines if the item should be loaded by the game."
            );

            rarity = LoLItems.MyConfig.Bind<string>(
                "Cull",
                "Rarity",
                "Tier1Def",
                "Set the rarity of the item. Valid values: Tier1Def, Tier2Def, Tier3Def, VoidTier1Def, VoidTier2Def, and VoidTier3Def."
            );

            voidItems = LoLItems.MyConfig.Bind<string>(
                "Cull",
                "Void Items",
                "",
                "Set regular items to convert into this void item (Only if the rarity is set as a void tier). Items should be separated by a comma, no spaces. The item should be the in game item ID, which may differ from the item name."
            );

            goldOnKill = LoLItems.MyConfig.Bind<float>(
                "Cull",
                "Gold Per Kill",
                1f,
                "Amount of gold each kill will grant."

            );

            killsToBreak = LoLItems.MyConfig.Bind<float>(
                "Cull",
                "Kills To Convert",
                100f,
                "Amount of kills needed to convert the item."

            );
        }

        private static void CreateItem()
        {
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();
            myItemDef.name = "Cull";
            myItemDef.nameToken = "Cull";
            myItemDef.pickupToken = "CullItem";
            myItemDef.descriptionToken = "CullDesc";
            myItemDef.loreToken = "CullLore";
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>(Utilities.GetRarityFromString(rarity.Value)).WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("CullIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("CullPrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
            myItemDef.tags = new ItemTag[1] { ItemTag.Utility };
        }

        private static void CreateBuff()
        {
            myBuffDef = ScriptableObject.CreateInstance<BuffDef>();

            myBuffDef.iconSprite = Assets.icons.LoadAsset<Sprite>("CullIcon");
            myBuffDef.name = "Cull Stacks";
            myBuffDef.canStack = true;
            myBuffDef.isDebuff = false;
            myBuffDef.isCooldown = false;
            myBuffDef.isHidden = false;
        }


        private static void hooks()
        {
            // Do something on character death
            On.RoR2.GlobalEventManager.OnCharacterDeath += (orig, globalEventManager, damageReport) =>
            {
                orig(globalEventManager, damageReport);

                if (!NetworkServer.active)
                    return;

                GameObject gameObject = null;
                Transform transform = null;
                Vector3 vector = Vector3.zero;

                if (damageReport.victim)
                {
                    gameObject = damageReport.victim.gameObject;
                    transform = gameObject.transform;
                    vector = transform.position;
                }

                if (damageReport.attackerMaster?.inventory)
                {

                    int inventoryCount = damageReport.attackerMaster.inventory.GetItemCount(myItemDef.itemIndex);
					if (inventoryCount > 0)
					{
                        float bonusGold = goldOnKill.Value * inventoryCount;
                        GoldOrb goldOrb = new()
                        {
                            origin = vector,
                            target = Util.FindBodyMainHurtBox(damageReport.attackerBody),
                            goldAmount = (uint)(bonusGold)
                        };
                        OrbManager.instance.AddOrb(goldOrb);

                        if (NetworkServer.active) {
                            for (int cnt = 0; cnt < inventoryCount; cnt ++)
                                damageReport.attackerBody.AddBuff(myBuffDef);
                        }
                        
                        if (damageReport.attackerBody.GetBuffCount(myBuffDef) >= killsToBreak.Value)
                        {   
                            if (NetworkServer.active) {
                                damageReport.attackerBody.inventory.RemoveItem(myItemDef);
                                damageReport.attackerBody.inventory.GiveItem(RoR2Content.Items.ScrapWhite);
                            }
                            CharacterMasterNotificationQueue.SendTransformNotification(damageReport.attackerMaster, myItemDef.itemIndex, RoR2Content.Items.ScrapWhite.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                            if (inventoryCount == 1)
                                Utilities.RemoveBuffStacks(damageReport.attackerBody, myBuffDef.buffIndex);
                            else if (NetworkServer.active) {
                                    for (int cnt = 0; cnt < killsToBreak.Value; cnt++)
                                    damageReport.attackerBody.RemoveBuff(myBuffDef);
                            
                            }
                        }
                        Utilities.SetValueInDictionary(ref buffStacks, damageReport.attackerBody.master, damageReport.attackerBody.GetBuffCount(myBuffDef.buffIndex), buffStacksToken);
                        Utilities.AddValueInDictionary(ref goldGained, damageReport.attackerBody.master, bonusGold, goldGainedToken);
					}
                }
            };

            On.RoR2.CharacterBody.OnInventoryChanged += (orig, self) => {
                orig(self);

                if (NetworkServer.active && self.inventory.GetItemCount(myItemDef.itemIndex) == 0 && self.GetBuffCount(myBuffDef.buffIndex) > 0) {
                    Utilities.RemoveBuffStacks(self, myBuffDef.buffIndex);
                }
            };

            On.RoR2.CharacterBody.Start += (orig, self) => {
                orig(self);

                if (NetworkServer.active && self.master && buffStacks.TryGetValue(self.master.netId, out float stacks)) {
                    for (int cnt = 0 ; cnt < stacks ; cnt++) {
                        self.AddBuff(myBuffDef.buffIndex);
                    }
                }
            };
        }

        private static (string, string) GetDisplayInformation(CharacterMaster masterRef)
        {
            if (masterRef == null)
                return (Language.GetString(myItemDef.descriptionToken), "");
            
            string customDescription = "";

            if (goldGained.TryGetValue(masterRef.netId, out float goldGainedAmount))
                customDescription += "<br><br>Total gold gained: " + String.Format("{0:#}", goldGainedAmount);
            else
                customDescription += "<br><br>Total gold gained: 0";

            return (Language.GetString(myItemDef.descriptionToken), customDescription);
        }
        
        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("Cull", "Cull");

            // Short description
            LanguageAPI.Add("CullItem", "Gives gold when killing enemies. Turns into White Scrap when <style=cIsUtility>" + killsToBreak.Value + "</style> enemies are killed.");

            // Long description
            LanguageAPI.Add("CullDesc", "Gives <style=cIsUtility>" + goldOnKill.Value + "</style> <style=cStack>(+" + goldOnKill.Value + ")</style> gold when killing an enemy. Upon killing <style=cIsUtility>" + killsToBreak.Value + "</style> enemies, turns into White Scrap.");

            // Lore
            LanguageAPI.Add("CullLore", "I personally like Doran's Blade more.");

            // ENABLE for buff
            LanguageAPI.Add("CullBuff", "Gold gained");
        }

        public static void SetupNetworkMappings()
        {
            LoLItems.networkMappings.Add(goldGainedToken, goldGained);
            LoLItems.networkMappings.Add(buffStacksToken, buffStacks);
        }
    }
}