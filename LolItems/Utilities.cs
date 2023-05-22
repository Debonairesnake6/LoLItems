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
        public static void AddValueInDictionary(ref Dictionary<UnityEngine.Networking.NetworkInstanceId, float> myDictionary, CharacterMaster characterMaster, float value)
        {
            UnityEngine.Networking.NetworkInstanceId id = CheckForMinionOwner(characterMaster);
            if (myDictionary.ContainsKey(id))
            {
                myDictionary[id] += value;
            }
            else
            {
                myDictionary.Add(id, value);
            }
        }

        public static void SetValueInDictionary(ref Dictionary<UnityEngine.Networking.NetworkInstanceId, float> myDictionary, CharacterMaster characterMaster, float value)
        {
            UnityEngine.Networking.NetworkInstanceId id = CheckForMinionOwner(characterMaster);
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
    }
}