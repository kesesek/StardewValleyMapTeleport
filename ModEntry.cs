using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace MapTeleport
{
    public partial class ModEntry : Mod
    {
        public static ModEntry context;
        public static ModConfig Config;
        public static IModHelper SHelper;
        public static IMonitor SMonitor;
        private static bool isSVE;
        private static bool hasRSV;
        private static bool hasES;
        private static bool hasGrandpaFarm;
        private static bool hasFrontireFarm;
        private Harmony harmony;

        public static string dictPath = "hlyvia.StardewValleyMapTeleport/coordinates";

        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();
            if (!Config.ModEnabled)
                return;

            context = this;
            SMonitor = Monitor;
            SHelper = helper;

            helper.Events.Content.AssetRequested += Content_AssetRequested;
            helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            isSVE = Helper.ModRegistry.IsLoaded("FlashShifter.SVECode");
            hasES = Helper.ModRegistry.IsLoaded("atravita.EastScarp");
            hasRSV = Helper.ModRegistry.IsLoaded("Rafseazz.RidgesideVillage");
            hasGrandpaFarm = Helper.ModRegistry.IsLoaded("flashshifter.GrandpasFarm");
            hasFrontireFarm = Helper.ModRegistry.IsLoaded("flashshifter.FrontierFarm");

            harmony = new Harmony(ModManifest.UniqueID);
            harmony.PatchAll();
        }

        private void Content_AssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo(dictPath))
            {
                CoordinatesList coordinatesList = new CoordinatesList();
                if (File.Exists(Path.Combine(SHelper.DirectoryPath, "found_coordinates.json")))
                {
                    coordinatesList.AddAll(Helper.Data.ReadJsonFile<CoordinatesList>("found_coordinates.json"));
                }
                if (isSVE)
                {
                    string farmType = Game1.GetFarmTypeID();
                    SMonitor.Log($"Get farmType: {farmType}", LogLevel.Info);

                    coordinatesList.AddAll(Helper.Data.ReadJsonFile<CoordinatesList>("assets/sve_coordinates.json"));
                    if (hasFrontireFarm && hasGrandpaFarm && farmType.Equals("FrontierFarm"))
                    {
                        coordinatesList.Add(new Coordinates("Farm/Default", "Farm", 118, 26));
                    }
                    else if (hasGrandpaFarm && farmType.Equals("0"))
                    {
                        coordinatesList.Add(new Coordinates("Farm/Default", "Farm", 95, 49));
                    }
                    else if (hasFrontireFarm && farmType.Equals("FrontierFarm"))
                    {
                        coordinatesList.Add(new Coordinates("Farm/Default", "Farm", 118, 26));
                    }
                    else
                    {
                        coordinatesList.Add(new Coordinates("Farm/Default", "Farm", 64, 15));
                    }
                }
                else
                {
                    coordinatesList.AddAll(Helper.Data.ReadJsonFile<CoordinatesList>("assets/coordinates.json"));
                }
                if (hasES)
                {
                    coordinatesList.AddAll(Helper.Data.ReadJsonFile<CoordinatesList>("assets/es_coordinates.json"));
                }
                if (hasRSV)
                {
                    coordinatesList.AddAll(Helper.Data.ReadJsonFile<CoordinatesList>("assets/rsv_coordinates.json"));
                }
                e.LoadFrom(() => coordinatesList, AssetLoadPriority.Exclusive);
            }
        }

        private void GameLoop_GameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {

            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is not null)
            {

                // register mod
                configMenu.Register(
                    mod: ModManifest,
                    reset: () => Config = new ModConfig(),
                    save: () => Helper.WriteConfig(Config)
                );

                configMenu.AddBoolOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("GMCM_Option_ModEnabled_Name"),
                    getValue: () => Config.ModEnabled,
                    setValue: value => Config.ModEnabled = value
                );

            }

        }
    }
}
