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
using R2API.Networking.Interfaces;
using UnityEngine.Networking;

namespace LoLItems
{
    public class Utilities
    {
        public static void AddValueInDictionary(ref Dictionary<NetworkInstanceId, float> myDictionary, CharacterMaster characterMaster, float value, string dictToken, bool checkMinionOwnership = true)
        {
            if (characterMaster == null)
                return;
                
            NetworkInstanceId id = characterMaster.netId;
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

            if (NetworkServer.active && characterMaster?.playerCharacterMasterController?.networkUser || characterMaster?.minionOwnership?.ownerMaster?.playerCharacterMasterController?.networkUser)
                NetworkManager.SyncDictionary(id, myDictionary[id], dictToken);
        }

        public static void SetValueInDictionary(ref Dictionary<NetworkInstanceId, float> myDictionary, CharacterMaster characterMaster, float value, string dictToken, bool checkMinionOwnership = true)
        {
            if (characterMaster == null)
                return;

            NetworkInstanceId id = characterMaster.netId;
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

            if (NetworkServer.active && characterMaster?.playerCharacterMasterController?.networkUser || characterMaster?.minionOwnership?.ownerMaster?.playerCharacterMasterController?.networkUser)
                NetworkManager.SyncDictionary(id, myDictionary[id], dictToken);
        }

        private static NetworkInstanceId CheckForMinionOwner(CharacterMaster characterMaster)
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
            if (!NetworkServer.active)
                return;

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
            if (!NetworkServer.active)
                return;
                
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
                {"Tier1Def", "ItemTierDefs/Tier1Def"},
                {"Tier2Def", "ItemTierDefs/Tier2Def"},
                {"Tier3Def", "ItemTierDefs/Tier3Def"},
                {"VoidTier1Def", "ItemTierDefs/VoidTier1Def"},
                {"VoidTier2Def", "ItemTierDefs/VoidTier2Def"},
                {"VoidTier3Def", "ItemTierDefs/VoidTier3Def"},
            };
            rarities.TryGetValue(rarity, out string result);
            return result;
        }

        // for items
        public static void SetupReadOnlyHooks(
            Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef, 
            Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef, 
            ItemDef myItemDef, 
            Func<CharacterMaster, (string, string)> GetDisplayInformation,
            ConfigEntry<string> rarity, 
            ConfigEntry<string> voidItems,
            string customItemName)
        {

            // Setup the base hooks
            ReadOnlyHooks(DisplayToMasterRef, IconToMasterRef, myItemDef, GetDisplayInformation);

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
                            item.tooltipProvider.overrideBodyText = AddCustomDescriptionRegexReplace(item.tooltipProvider.overrideBodyText, myItemDef, GetDisplayInformation, self.targetMaster);
                        }
                    });
#pragma warning restore Publicizer001
                }
            };

            // RoR2 1.5 sets an icon's tooltip through the SetItemIndex(SetItemIndexArgs) overload.
            // The (ItemIndex, int) and (ItemIndex, int, float) overloads are marked "For Mod Compat
            // Only", so hook all three and catch whichever one the game actually calls.
            On.RoR2.UI.ItemIcon.SetItemIndex_ItemIndex_int += (orig, self, newIndex, newCount) =>
            {
                orig(self, newIndex, newCount);
                ApplyIconDescription(self, newIndex, myItemDef, IconToMasterRef, GetDisplayInformation);
            };

            On.RoR2.UI.ItemIcon.SetItemIndex_ItemIndex_int_float += (orig, self, newIndex, newCount, newDurationPercent) =>
            {
                orig(self, newIndex, newCount, newDurationPercent);
                ApplyIconDescription(self, newIndex, myItemDef, IconToMasterRef, GetDisplayInformation);
            };

            On.RoR2.UI.ItemIcon.SetItemIndex_SetItemIndexArgs += (orig, self, args) =>
            {
                orig(self, args);
                ApplyIconDescription(self, args.newItemIndex, myItemDef, IconToMasterRef, GetDisplayInformation);
            };

            #pragma warning disable Publicizer001
            // ItemInventoryDisplay.UpdateDisplay is what allocates and refreshes the icons of every
            // item display: the HUD, the scoreboard, the enemy info panel and the end-of-run report
            // panel (death recap). The report panel rebuilds its icons after gameplay ends, so this
            // is the only place that still sees those icons.
            On.RoR2.UI.ItemInventoryDisplay.UpdateDisplay += (orig, self) =>
            {
                orig(self);
                DisplayToMasterRef.TryGetValue(self, out CharacterMaster masterRef);
                if (!masterRef)
                    return;

                foreach (RoR2.UI.ItemIcon icon in self.itemIcons)
                {
                    IconToMasterRef[icon] = masterRef;
                    ApplyIconDescription(icon, icon.itemIndex, myItemDef, IconToMasterRef, GetDisplayInformation);
                }
            };
            #pragma warning restore Publicizer001

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
            Func<CharacterMaster, (string, string)> GetDisplayInformation)
        {
            // Setup the base hooks
            ReadOnlyHooks(DisplayToMasterRef);

            On.RoR2.UI.EquipmentIcon.SetDisplayData += (orig, self, newDisplayData) => 
            {
                orig(self, newDisplayData);

#pragma warning disable Publicizer001
                if (newDisplayData.equipmentDef == myEquipmentDef && self.targetInventory)
#pragma warning restore Publicizer001
                {
                    foreach (RoR2.PlayerCharacterMasterController player in RoR2.PlayerCharacterMasterController.instances)
                    {
                        if (self.targetInventory == player.master.inventory)
                        {
                            (string baseDescription, string customDescription) = GetDisplayInformation(player.master);

                            if (self.tooltipProvider.overrideBodyText.Length == 0)
                                self.tooltipProvider.overrideBodyText = baseDescription;

                            if (customDescription.Length > 0)
                            {
                                if (_previousEquipCustomDescriptions.TryGetValue(myEquipmentDef, out string oldCustom))
                                    self.tooltipProvider.overrideBodyText = self.tooltipProvider.overrideBodyText.Replace(oldCustom, "");
                                _previousEquipCustomDescriptions[myEquipmentDef] = customDescription;
                                self.tooltipProvider.overrideBodyText += customDescription;
                            }
                            return;
                        }
                    }
                }
                // Clear the override text if it's not overwritten by other mods
                if (RoR2.PlayerCharacterMasterController.instances?.Count > 0)
                {
                    (string baseDescription, string customDescription) = GetDisplayInformation(RoR2.PlayerCharacterMasterController.instances[0].master);
                    if (customDescription.Length == 0)
                    {
                        self.tooltipProvider.overrideBodyText = "";
                    }
                }
            };
        }

        // Equipment icons do not need the icon->master mapping, only the display->master mapping.
        private static void ReadOnlyHooks(
            Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef)
        {
            RegisterDisplayMappings(DisplayToMasterRef);

            On.RoR2.UI.GameEndReportPanelController.SetPlayerInfo += (orig, self, playerInfo, playerIndex) =>
            {
                orig(self, playerInfo, playerIndex);
                CharacterMaster master = ResolveReportMaster(playerInfo);
                if (master && self.itemInventoryDisplay)
                    DisplayToMasterRef[self.itemInventoryDisplay] = master;
            };
        }

        private static void ReadOnlyHooks(
            Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef,
            Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef,
            ItemDef myItemDef,
            Func<CharacterMaster, (string, string)> GetDisplayInformation)
        {
            RegisterDisplayMappings(DisplayToMasterRef);

            // Death screen (end-of-game report) - remember the reported player. The panel rebuilds its
            // icons after gameplay ends, so ItemInventoryDisplay.UpdateDisplay applies the tracked
            // values when those icons are refreshed.
            On.RoR2.UI.GameEndReportPanelController.SetPlayerInfo += (orig, self, playerInfo, playerIndex) =>
            {
                orig(self, playerInfo, playerIndex);
                CharacterMaster master = ResolveReportMaster(playerInfo);
                if (!master || !self.itemInventoryDisplay)
                    return;

                DisplayToMasterRef[self.itemInventoryDisplay] = master;

                #pragma warning disable Publicizer001
                self.itemInventoryDisplay.itemIcons.ForEach(delegate(RoR2.UI.ItemIcon icon)
                {
                    IconToMasterRef[icon] = master;
                });
                #pragma warning restore Publicizer001
            };
        }

        // Every item display shows one player's items, and the tracked values are looked up per
        // player, so each display needs to know which master it belongs to.
        private static void RegisterDisplayMappings(Dictionary<RoR2.UI.ItemInventoryDisplay, CharacterMaster> DisplayToMasterRef)
        {
            // Open Scoreboard
            On.RoR2.UI.ScoreboardStrip.SetMaster += (orig, self, characterMaster) =>
            {
                orig(self, characterMaster);
                if (characterMaster) DisplayToMasterRef[self.itemInventoryDisplay] = characterMaster;
            };

            // A display that subscribes an inventory belongs to the owner of that inventory
            On.RoR2.UI.ItemInventoryDisplay.SetSubscribedInventory += (orig, self, newInventory) =>
            {
                orig(self, newInventory);
                if (!newInventory)
                    return;

                foreach (RoR2.PlayerCharacterMasterController playerCharacterMasterController in RoR2.PlayerCharacterMasterController.instances)
                {
                    if (playerCharacterMasterController.master && playerCharacterMasterController.master.inventory == newInventory)
                    {
                        DisplayToMasterRef[self] = playerCharacterMasterController.master;
                        return;
                    }
                }
            };
        }

        // RunReport.PlayerInfo.master is nullable: the run report does not carry masters, so it is
        // only filled in for the players this client resolves locally. Fall back to the live objects
        // so the tracked values are still available at the end of the run.
        private static CharacterMaster ResolveReportMaster(RoR2.RunReport.PlayerInfo playerInfo)
        {
            if (playerInfo.master)
                return playerInfo.master;

            if (playerInfo.networkUser && playerInfo.networkUser.master)
                return playerInfo.networkUser.master;

            if (playerInfo.localUser != null && playerInfo.localUser.cachedMaster != null)
                return playerInfo.localUser.cachedMaster;

            if (playerInfo.networkUser)
            {
                foreach (RoR2.PlayerCharacterMasterController playerCharacterMasterController in RoR2.PlayerCharacterMasterController.instances)
                {
                    if (playerCharacterMasterController.master && playerCharacterMasterController.networkUser == playerInfo.networkUser)
                        return playerCharacterMasterController.master;
                }
            }

            return null;
        }

        private static void ApplyIconDescription(
            RoR2.UI.ItemIcon item,
            RoR2.ItemIndex itemIndex,
            ItemDef myItemDef,
            Dictionary<RoR2.UI.ItemIcon, CharacterMaster> IconToMasterRef,
            Func<CharacterMaster, (string, string)> GetDisplayInformation)
        {
            if (!item.tooltipProvider || itemIndex != myItemDef.itemIndex)
                return;

            IconToMasterRef.TryGetValue(item, out CharacterMaster master);
            item.tooltipProvider.overrideBodyText = AddCustomDescriptionRegexReplace(item.tooltipProvider.overrideBodyText, myItemDef, GetDisplayInformation, master);
        }

        // Keyed per player: the same item can be displayed for several players at once
        // (scoreboard, end-of-run report) and each of them has different tracked values.
        private static readonly Dictionary<(RoR2.ItemIndex, NetworkInstanceId), string> _previousCustomDescriptions = [];
        private static readonly Dictionary<EquipmentDef, string> _previousEquipCustomDescriptions = [];

        // Add our custom description while keeping the custom description added by other mods (e.g. equipment cooldown)
        private static string AddCustomDescriptionRegexReplace(string overrideBodyText, ItemDef itemDef, Func<CharacterMaster, (string, string)> GetDisplayInformation, CharacterMaster characterMaster)
        {
            (string baseDescription, string customDescription) = GetDisplayInformation(characterMaster);

            if (overrideBodyText.Length == 0)
                overrideBodyText = baseDescription;

            (RoR2.ItemIndex, NetworkInstanceId) key = (itemDef.itemIndex, characterMaster ? characterMaster.netId : new NetworkInstanceId());

            if (customDescription.Length == 0)
            {
                _previousCustomDescriptions.Remove(key);
                return overrideBodyText;
            }

            // Replace any old custom description for this item and player
            if (_previousCustomDescriptions.TryGetValue(key, out string oldCustom))
            {
                overrideBodyText = overrideBodyText.Replace(oldCustom, "");
            }

            _previousCustomDescriptions[key] = customDescription;

            return overrideBodyText + customDescription;
        }

        

    }
}