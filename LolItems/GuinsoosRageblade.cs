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

namespace LoLItems
{
    internal class GuinsoosRageblade
    {

        public static ItemDef myItemDef;
        public static float procCoef = 0.1f;
        public static Dictionary<UnityEngine.Networking.NetworkInstanceId, float> totalProcCoef = new Dictionary<UnityEngine.Networking.NetworkInstanceId, float>();

        internal static void Init()
        {
            CreateItem();
            AddTokens();
            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            hooks();
        }

        private static void CreateItem()
        {
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();
            myItemDef.name = "GuinsoosRageblade";
            myItemDef.nameToken = "GuinsoosRageblade";
            myItemDef.pickupToken = "GuinsoosRagebladeItem";
            myItemDef.descriptionToken = "GuinsoosRagebladeDesc";
            myItemDef.loreToken = "GuinsoosRagebladeLore";
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier1Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001
            myItemDef.pickupIconSprite = Assets.icons.LoadAsset<Sprite>("GuinsoosRagebladeIcon");
            myItemDef.pickupModelPrefab = Assets.prefabs.LoadAsset<GameObject>("GuinsoosRagebladePrefab");
            myItemDef.canRemove = true;
            myItemDef.hidden = false;
        }


        private static void hooks()
        {
            On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, victim) =>
            {
                if (damageInfo.attacker)
                {
                    CharacterBody attackerCharacterBody = damageInfo.attacker.GetComponent<CharacterBody>();
                    CharacterBody victimCharacterBody = victim.GetComponent<CharacterBody>();
                    
                    if (attackerCharacterBody?.inventory)
                    {
                        int inventoryCount = attackerCharacterBody.inventory.GetItemCount(myItemDef.itemIndex);
                        if (inventoryCount > 0 && damageInfo.procCoefficient > 0)
                        {
                            float extraTotal = procCoef * inventoryCount;
                            Utilities.SetValueInDictionary(ref totalProcCoef, attackerCharacterBody.master, extraTotal);
                            damageInfo.procCoefficient += extraTotal;
                        }
                    }
                }
                orig(self, damageInfo, victim);
            };

            On.RoR2.UI.HUD.Update += (orig, self) => 
            {
                orig(self);
                if (self.itemInventoryDisplay && self.targetMaster)
                {
#pragma warning disable Publicizer001
                    self.itemInventoryDisplay.itemIcons.ForEach(delegate(RoR2.UI.ItemIcon item)
                    {
                        if (item.itemIndex == myItemDef.itemIndex && totalProcCoef.TryGetValue(self.targetMaster.netId, out float value)){
                            item.tooltipProvider.overrideBodyText =
                                Language.GetString(myItemDef.descriptionToken) + "<br><br>Extra procCoef: " + String.Format("{0:F1}", value);
                        }
                    });
#pragma warning restore Publicizer001
                }
            };
        }

        private static void AddTokens()
        {
            // Name of the item
            LanguageAPI.Add("GuinsoosRageblade", "GuinsoosRageblade");

            // Short description
            LanguageAPI.Add("GuinsoosRagebladeItem", "Increase proc coefficient of everything");

            // Long description
            LanguageAPI.Add("GuinsoosRagebladeDesc", "Gives <style=cIsUtility>" + procCoef + "</style> <style=cStack>(+" + procCoef + ")</style> proc coefficient to everything");

            // Lore
            LanguageAPI.Add("GuinsoosRagebladeLore", "Procs go brrrrrrr.");
        }
    }
}