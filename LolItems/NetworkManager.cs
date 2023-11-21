using UnityEngine.Networking;
using UnityEngine;
using R2API;
using RoR2;
using System.Collections.Generic;

namespace LoLItems
{
    public class NetworkManager
    {
        //Static references so we do not need to do tricky things with passing references.
        internal static GameObject CentralNetworkObject;
        private static GameObject _centralNetworkObjectSpawned;

        public static void SyncDictionary(NetworkInstanceId netId, float value, string dictToken)
        {
            if (NetworkServer.active)
            {
                if (!_centralNetworkObjectSpawned)
                    Setup();

                foreach (NetworkUser user in NetworkUser.readOnlyInstancesList)
                {
                    MyNetworkComponent.Invoke(user, netId, value, dictToken);
                }
            }
        }

        public static void Setup()
        {
            GameObject tmp = new GameObject("tmpLoLItemsNetworkObject");
            tmp.AddComponent<NetworkIdentity>();
            CentralNetworkObject = tmp.InstantiateClone("LoLItemsUniqueClone");
            GameObject.Destroy(tmp);
            CentralNetworkObject.AddComponent<MyNetworkComponent>();
            _centralNetworkObjectSpawned = Object.Instantiate(CentralNetworkObject);
            NetworkServer.Spawn(_centralNetworkObjectSpawned);
        }

        public static bool GetCharacterMasterFromNetId(NetworkInstanceId netId, out CharacterMaster characterMaster)
        {
            characterMaster = null;

            // GameObject gameObject = Util.FindNetworkObject(netId);
            // if (gameObject?.GetComponent<CharacterMaster>())
            // {
            //     characterMaster = gameObject.GetComponent<CharacterMaster>();
            //     LoLItems.Log.LogDebug($"NetID {netId} found on {gameObject.name} + {characterMaster.name}");
            //     return true;
            // }
            // LoLItems.Log.LogDebug($"NetID {netId} not found on {gameObject.name}");
            
            foreach (CharacterBody characterBody in CharacterBody.readOnlyInstancesList)
            {
                if (characterBody.netId == netId)
                {
                    characterMaster = characterBody.master;
                    return true;
                }
            }

            return false;
        }
    }

    //Important to note that these NetworkBehaviour classes must not be nested for UNetWeaver to find them.
    internal class MyNetworkComponent : NetworkBehaviour
    {
        // We only ever have one instance of the networked behaviour here.
        private static MyNetworkComponent _instance;

        private void Awake()
        {
            _instance = this;
        }
        public static void Invoke(NetworkUser user, NetworkInstanceId netId, float value, string dictToken)
        {
            _instance.TargetSyncDictionary(user.connectionToClient, netId, value, dictToken);
        }

        [TargetRpc]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Target param is required by UNetWeaver")]
        private void TargetSyncDictionary(NetworkConnection target, NetworkInstanceId netId, float value, string dictToken)
        {
            if (LoLItems.networkMappings.TryGetValue(dictToken, out Dictionary<NetworkInstanceId, float> myDictionary) && NetworkManager.GetCharacterMasterFromNetId(netId, out CharacterMaster characterMaster))
            {
                Utilities.SetValueInDictionary(ref myDictionary, characterMaster, value, dictToken);
            }
        }
    }
}