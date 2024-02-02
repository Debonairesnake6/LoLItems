using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using BepInEx.Configuration;
using R2API.Networking.Interfaces;
using R2API.Networking;

namespace LoLItems
{
    // This attribute specifies that we have a dependency on R2API, as we're using it to add our item to the game.
    // You don't need this if you're not using R2API in your plugin, it's just to tell BepInEx to initialize R2API before this plugin so it's safe to use R2API.
    [BepInDependency(R2API.R2API.PluginGUID)]

    // This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    // We will be using 2 modules from R2API: ItemAPI to add our item and LanguageAPI to add our language tokens.
    [R2APISubmoduleDependency(nameof(ItemAPI), nameof(LanguageAPI))]

    [R2APISubmoduleDependency(nameof(RecalculateStatsAPI))]

    // This is the main declaration of our plugin class. BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    // BaseUnityPlugin itself inherits from MonoBehaviour, so you can use this as a reference for what you can declare and use in your plugin class: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class LoLItems : BaseUnityPlugin
    {
        //The Plugin GUID should be a unique ID for this plugin, which is human readable (as it is used in places like the config).
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Debo";
        public const string PluginName = "LoLItems";
        public const string PluginVersion = "1.3.0";

        public static BepInEx.Logging.ManualLogSource Log;
        public static ConfigFile MyConfig { get; set; }
        public GameObject multiShopPrefab;
        public ItemTier[] itemTiers;
        public static PluginInfo PInfo { get; private set; }
        public static Dictionary<string, Dictionary<UnityEngine.Networking.NetworkInstanceId, float>> networkMappings = [];

        //The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            //Init our logging class so that we can properly log for debugging
            Log = Logger;
            MyConfig = Config;
            PInfo = Info;
            
            Assets.Init();

            // WhiteClover.Init();

            Heartsteel.Init();
            Bork.Init();
            Rabadons.Init();
            Liandrys.Init();
            GuinsoosRageblade.Init();
            BannerOfCommand.Init();
            InfinityEdge.Init();
            ImperialMandate.Init();
            MejaisSoulstealer.Init();
            KrakenSlayer.Init();
            GuardiansBlade.Init();
            ImmortalShieldbow.Init();
            GargoyleStoneplate.Init();
            Cull.Init();
            ExperimentalHexplate.Init();

            // Load the networking stuff
            NetworkManager.Init();

            // This line of log will appear in the bepinex console when the Awake method is done.
            Log.LogInfo("LoLItems successfully loaded.");
        }

        // ONLY FOR TESTING:
        // Uncomment the following code to enable Update(), which allows you to press F2 to spawn all of our items in-game
        // The Update() method is run on every frame of the game.

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                //Get the player body to use a position:
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;

                //And then drop our defined item in front of the player.
                Log.LogInfo($"Player pressed F2. Spawning our custom item at coordinates {transform.position}");

                List<ItemDef> myItems = [ 
                    BannerOfCommand.myItemDef, 
                    Bork.myItemDef, 
                    GuardiansBlade.myItemDef,
                    GuinsoosRageblade.myItemDef, 
                    Heartsteel.myItemDef, 
                    ImmortalShieldbow.myItemDef,
                    ImperialMandate.myItemDef, 
                    InfinityEdge.myItemDef, 
                    KrakenSlayer.myItemDef,
                    Liandrys.myItemDef, 
                    MejaisSoulstealer.myItemDef,
                    Rabadons.myItemDef, 
                    Cull.myItemDef,
                    ExperimentalHexplate.myItemDef
                    ];

                foreach (ItemDef myItem in myItems)
                {
                    PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(myItem.itemIndex), transform.position, transform.forward * 30f);
                }

                List<EquipmentDef> myEquipments = [
                    GargoyleStoneplate.myEquipmentDef,
                    ];
                foreach (EquipmentDef myEquipment in myEquipments)
                {
                    PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(myEquipment.equipmentIndex), transform.position, transform.forward * 30f);
                }
            }
        }
    }
}
