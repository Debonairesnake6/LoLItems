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
    }
}