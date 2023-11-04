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
using System.Linq;
using System.Collections;
using BepInEx.Configuration;

namespace LoLItems
{
    public class Utilities
    {
        public static void AddValueInDictionary(ref Dictionary<UnityEngine.Networking.NetworkInstanceId, float> myDictionary, CharacterMaster characterMaster, float value, bool checkMinionOwnership = true)
        {
            UnityEngine.Networking.NetworkInstanceId id = characterMaster.netId;
            if (checkMinionOwnership)
            {
                id = CheckForMinionOwner(characterMaster);
            }
            
            if (myDictionary.ContainsKey(id))
            {
                myDictionary[id] += value;
            }
            else
            {
                myDictionary.Add(id, value);
            }
        }

        public static void SetValueInDictionary(ref Dictionary<UnityEngine.Networking.NetworkInstanceId, float> myDictionary, CharacterMaster characterMaster, float value, bool checkMinionOwnership = true)
        {
            UnityEngine.Networking.NetworkInstanceId id = characterMaster.netId;
            if (checkMinionOwnership)
            {
                id = CheckForMinionOwner(characterMaster);
            }
            
            if (myDictionary.ContainsKey(id))
            {
                myDictionary[id] = value;
            }
            else
            {
                myDictionary.Add(id, value);
            }
        }

        private static UnityEngine.Networking.NetworkInstanceId CheckForMinionOwner(CharacterMaster characterMaster)
        {
            return characterMaster?.minionOwnership?.ownerMaster?.netId != null ? characterMaster.minionOwnership.ownerMaster.netId : characterMaster.netId;
        }

        // From https://github.com/derslayr10/RoR2GenericModTemplate/blob/main/Utils/ItemHelper.cs#L10
        public static CharacterModel.RendererInfo[] ItemDisplaySetup(GameObject obj)
        {
            MeshRenderer[] meshes = obj.GetComponentsInChildren<MeshRenderer>();
            CharacterModel.RendererInfo[] renderInfos = new CharacterModel.RendererInfo[meshes.Length];
    
            for (int i = 0; i < meshes.Length; i++)
            {
                renderInfos[i] = new CharacterModel.RendererInfo
                {
                    defaultMaterial = meshes[i].material,
                    renderer = meshes[i],
                    defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                    ignoreOverlays = false //We allow the mesh to be affected by overlays like OnFire or PredatoryInstinctsCritOverlay.
                };
            }
    
            return renderInfos;
        }

        public static void RemoveBuffStacks(CharacterBody characterBody, BuffIndex buffIndex)
        {
            while (characterBody.GetBuffCount(buffIndex) > 0)
            {
                characterBody.RemoveBuff(buffIndex);
            }
        }

        public static float HyperbolicScale(int itemCount, float value)
        {
            return 1 - (1 / (itemCount * value + 1));
        }

        public static void AddTimedBuff(CharacterBody characterBody, BuffDef buffDef, float duration)
        {
            float myTimer = 1;
            while (myTimer <= duration)
            {
                characterBody.AddTimedBuff(buffDef, myTimer);
                myTimer++;
            }
        }

        public static string GetRarityFromString(string rarity)
        {
            Dictionary<string, string> rarities = new Dictionary<string, string>{
                {"Tier1Def", "RoR2/Base/Common/Tier1Def.asset"},
                {"Tier2Def", "RoR2/Base/Common/Tier2Def.asset"},
                {"Tier3Def", "RoR2/Base/Common/Tier3Def.asset"},
                {"VoidTier1Def", "RoR2/DLC1/Common/VoidTier1Def.asset"},
                {"VoidTier2Def", "RoR2/DLC1/Common/VoidTier2Def.asset"},
                {"VoidTier3Def", "RoR2/DLC1/Common/VoidTier3Def.asset"},
            };
            rarities.TryGetValue(rarity, out string result);
            return result;
        }

        // for items
        public static void SetupReadOnlyHooks(
            Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef, 
            Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef, 
            ItemDef myItemDef, 
            Func<CharacterMaster, string> GetDisplayInformation,
            ConfigEntry<string> rarity, 
            ConfigEntry<string> voidItems,
            string customItemName)
        {

            // Setup the base hooks
            ReadOnlyHooks(DisplayToMasterRef);

            // Called basically every frame to update your HUD info
            On.RoR2.UI.HUD.Update += (orig, self) => 
            {
                orig(self);
                if (self.itemInventoryDisplay && self.targetMaster)
                {
                    DisplayToMasterRef[self.itemInventoryDisplay] = self.targetMaster;
#pragma warning disable Publicizer001
                    self.itemInventoryDisplay.itemIcons.ForEach(delegate(RoR2.UI.ItemIcon item)
                    {
                        // Update the description for an item in the HUD
                        if (item.itemIndex == myItemDef.itemIndex){
                            item.tooltipProvider.overrideBodyText = GetDisplayInformation(self.targetMaster);
                        }
                    });
#pragma warning restore Publicizer001
                }
            };

            // Open Scoreboard
            On.RoR2.UI.ItemIcon.SetItemIndex += (orig, self, newIndex, newCount) =>
            {
                orig(self, newIndex, newCount);
                if (self.tooltipProvider != null && newIndex == myItemDef.itemIndex)
                {
                    IconToMasterRef.TryGetValue(self, out CharacterMaster master);
                    self.tooltipProvider.overrideBodyText = GetDisplayInformation(master);
                }
            };

            // Open Scoreboard
            On.RoR2.UI.ItemInventoryDisplay.AllocateIcons += (orig, self, count) =>
            {
                orig(self, count);
                List<RoR2.UI.ItemIcon> icons = self.GetFieldValue<List<RoR2.UI.ItemIcon>>("itemIcons");
                DisplayToMasterRef.TryGetValue(self, out CharacterMaster masterRef);
                icons.ForEach(i => IconToMasterRef[i] = masterRef);
            };

            // Create void item
            On.RoR2.Items.ContagiousItemManager.Init += (orig) => 
            {
                if (rarity.Value.Substring(0,4) == "Void")
                {
                    List<ItemDef.Pair> newVoidPairs = new List<ItemDef.Pair>();
                    foreach(string itemName in new List<string>(voidItems.Value.Split(',')))
                    {
                        ItemIndex itemIndex = ItemCatalog.FindItemIndex(itemName);
                        if (itemIndex != ItemIndex.None)
                        {
                            ItemDef.Pair newVoidPair = new ItemDef.Pair()
                            {
                                itemDef1 = ItemCatalog.GetItemDef(itemIndex),
                                itemDef2 = myItemDef
                            };
                            newVoidPairs.Add(newVoidPair);
                        }
                        else
                        {
                            LoLItems.Log.LogError(customItemName + " - Unknown item ID to set as void contagion: " + itemName);
                        }
                    }
#pragma warning disable Publicizer001
                    ItemDef.Pair[] voidPairs = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem];
                    ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = voidPairs.Union(newVoidPairs).ToArray();
#pragma warning restore Publicizer001
                }
                orig();
            };
        }

        // for equipments
        public static void SetupReadOnlyHooks(
            Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef,
            EquipmentDef myEquipmentDef,
            Func<CharacterMaster, string> GetDisplayInformation)
        {
            // Setup the base hooks
            ReadOnlyHooks(DisplayToMasterRef);

            // Called basically every frame to update your HUD info
            On.RoR2.UI.HUD.Update += (orig, self) => 
            {
                orig(self);
                if (self.itemInventoryDisplay && self.targetMaster)
                {
                    DisplayToMasterRef[self.itemInventoryDisplay] = self.targetMaster;
#pragma warning disable Publicizer001
                    foreach (RoR2.UI.EquipmentIcon equipment in self.equipmentIcons)
                    {
                        if (equipment.targetEquipmentSlot.equipmentIndex == myEquipmentDef.equipmentIndex)
                            equipment.tooltipProvider.overrideBodyText = GetDisplayInformation(self.targetMaster);
                    };
                    // self.itemInventoryDisplay.itemIcons.ForEach(delegate(RoR2.UI.EquipmentIcon item)
                    // {
                    //     // Update the description for an item in the HUD
                    //     if (item.itemIndex == myEquipmentDef.equipmentIndex){
                    //         item.tooltipProvider.overrideBodyText = GetDisplayInformation(self.targetMaster);
                    //     }
                    // });
#pragma warning restore Publicizer001
                }
            };
        }

        private static void ReadOnlyHooks(
            Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef
        )
        {
            // Open Scoreboard
            On.RoR2.UI.ScoreboardStrip.SetMaster += (orig, self, characterMaster) =>
            {
                orig(self, characterMaster);
                if (characterMaster) DisplayToMasterRef[self.itemInventoryDisplay] = characterMaster;
            };

            // Add to stat dict for end of game screen
            On.RoR2.UI.GameEndReportPanelController.SetPlayerInfo += (orig, self, playerInfo) => 
            {
                orig(self, playerInfo);
                Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRefCopy = new Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster>(DisplayToMasterRef);
                foreach(KeyValuePair<RoR2.UI.ItemInventoryDisplay, CharacterMaster> entry in DisplayToMasterRefCopy)
                {
                    if (entry.Value == playerInfo.master)
                    {
                        DisplayToMasterRef[self.itemInventoryDisplay] = playerInfo.master;
                    }
                }
            };
        }

    }
}