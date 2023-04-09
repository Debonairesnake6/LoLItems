using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace LoLItems
{
    //This is an example plugin that can be put in BepInEx/plugins/ExamplePlugin/ExamplePlugin.dll to test out.
    //It's a small plugin that adds a relatively simple item to the game, and gives you that item whenever you press F2.

    //This attribute specifies that we have a dependency on R2API, as we're using it to add our item to the game.
    //You don't need this if you're not using R2API in your plugin, it's just to tell BepInEx to initialize R2API before this plugin so it's safe to use R2API.
    [BepInDependency(R2API.R2API.PluginGUID)]

    //This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    //We will be using 2 modules from R2API: ItemAPI to add our item and LanguageAPI to add our language tokens.
    [R2APISubmoduleDependency(nameof(ItemAPI), nameof(LanguageAPI))]


    //This is the main declaration of our plugin class. BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    //BaseUnityPlugin itself inherits from MonoBehaviour, so you can use this as a reference for what you can declare and use in your plugin class: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class LoLItems : BaseUnityPlugin
    {
        //The Plugin GUID should be a unique ID for this plugin, which is human readable (as it is used in places like the config).
        //If we see this PluginGUID as it is on thunderstore, we will deprecate this mod. Change the PluginAuthor and the PluginName !
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Debo";
        public const string PluginName = "LoLItems";
        public const string PluginVersion = "0.1.3";

        public static BepInEx.Logging.ManualLogSource Log;
        public GameObject multiShopPrefab;
        public ItemTier[] itemTiers;
        public static PluginInfo PInfo {get; private set;}

        //The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            //Init our logging class so that we can properly log for debugging
            Log = Logger;

            PInfo = Info;
            Assets.Init();

            // WhiteClover.Init();

            Heartsteel.Init();
            Bork.Init();
            Rabadons.Init();
            Liandrys.Init();

            // This line of log will appear in the bepinex console when the Awake method is done.
            Log.LogInfo(nameof(Awake) + " done.");
        }

        //The Update() method is run on every frame of the game.
        private void Update()
        {
            // ONLY FOR TESTING
            // //This if statement checks if the player has currently pressed F2.
            // if (Input.GetKeyDown(KeyCode.F2))
            // {
            //     //Get the player body to use a position:
            //     var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;

            //     //And then drop our defined item in front of the player.

            //     Log.LogInfo($"Player pressed F2. Spawning our custom item at coordinates {transform.position}");
            //     // PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(WhiteClover.myItemDef.itemIndex), transform.position, transform.forward * 20f);
            //     PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(Heartsteel.myItemDef.itemIndex), transform.position, transform.forward * 20f);
            //     PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(Bork.myItemDef.itemIndex), transform.position, transform.forward * 20f);
            //     PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(Rabadons.myItemDef.itemIndex), transform.position, transform.forward * 20f);
            //     PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(Liandrys.myItemDef.itemIndex), transform.position, transform.forward * 20f);
            //     // PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2.DLC1Content.Items.ChainLightningVoid.itemIndex), transform.position, transform.forward * 20f);
            //     // PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2.DLC1Content.Items.MissileVoid.itemIndex), transform.position, transform.forward * 20f);
            //     // PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2.RoR2Content.Items.BleedOnHitAndExplode.itemIndex), transform.position, transform.forward * 20f);
            //     // PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2.RoR2Content.Items.LunarBadLuck.itemIndex), transform.position, transform.forward * 20f);
            //     // PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2.RoR2Content.Items.LunarBadLuck.itemIndex), transform.position, transform.forward * 20f);
            //     // PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2.RoR2Content.Items.LunarBadLuck.itemIndex), transform.position, transform.forward * 20f);
            //     // PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2.RoR2Content.Items.LunarBadLuck.itemIndex), transform.position, transform.forward * 20f);
            //     // PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2.RoR2Content.Items.LunarBadLuck.itemIndex), transform.position, transform.forward * 20f);
            //     // PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2.DLC1Content.Items.MoreMissile.itemIndex), transform.position, transform.forward * 20f);
            //     // PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2.RoR2Content.Items.CritGlasses.itemIndex), transform.position, transform.forward * 20f);
            //     // PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2.RoR2Content.Items.FallBoots.itemIndex), transform.position, transform.forward * 20f);
            // }
        }
    }
}
