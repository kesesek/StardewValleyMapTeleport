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
        private Harmony harmony;
        private IClickableMenu previousMenu = null;

        public static string dictPath = "hlyvia.StardewValleyMapTeleport/coordinates";

        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();
            if (!Config.ModEnabled)
                return;

            context = this;
            SMonitor = Monitor;
            SHelper = helper;

            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Content.AssetRequested += Content_AssetRequested;
            helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            isSVE = Helper.ModRegistry.IsLoaded("FlashShifter.SVECode");
            hasES = Helper.ModRegistry.IsLoaded("atravita.EastScarp");
            hasRSV = Helper.ModRegistry.IsLoaded("Rafseazz.RidgesideVillage");
            hasGrandpaFarm = Helper.ModRegistry.IsLoaded("flashshifter.GrandpasFarm");

            harmony = new Harmony(ModManifest.UniqueID);
            harmony.PatchAll();
        }


private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            // 获取当前活动的菜单
            IClickableMenu currentMenu = Game1.activeClickableMenu;

            // 检查菜单是否变化
            if (currentMenu != previousMenu)
            {
                // 记录菜单变化
                if (currentMenu == null)
                {
                    Monitor.Log("activeClickableMenu changed to: null", LogLevel.Info);
                }
                else
                {
                    Monitor.Log($"activeClickableMenu changed to: {currentMenu.GetType().Name}", LogLevel.Info);
                    LogMenuComponents(currentMenu);

                    CheckSubMenu(currentMenu);
                }

                // 更新 previousMenu 变量
                previousMenu = currentMenu;
            }
        }

        // 记录菜单的可点击组件
        public void LogMenuComponents(IClickableMenu menu)
        {
            if (menu.allClickableComponents != null)
            {
                foreach (var component in menu.allClickableComponents)
                {
                    Monitor.Log($"Component: {component.name}, Bounds: {component.bounds}", LogLevel.Info);
                }
            }
            else
            {
                Monitor.Log("No clickable components found.", LogLevel.Info);
            }
        }

        // 检查子菜单
        private void CheckSubMenu(IClickableMenu menu)
        {
            if (menu is GameMenu gameMenu)
            {
                var currentPage = menu.GetChildMenu();
                if (currentPage != null)
                {
                    Monitor.Log($"Current childpage in GameMenu is: {currentPage.GetType().Name}", LogLevel.Info);
                    CheckSubMenu(currentPage);
                }
                else
                {
                    Monitor.Log("No more childpage", LogLevel.Info);
                }
            }
        }


        public void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // 检查是否为鼠标左键点击
            if (e.Button == SButton.MouseLeft)
            {
                // 获取鼠标点击的位置
                var cursorPosition = e.Cursor.ScreenPixels;
                Monitor.Log($"Mouse left button clicked at {cursorPosition.X}, {cursorPosition.Y}", LogLevel.Info);
                if (Game1.activeClickableMenu != null)
                {
                    string name = Game1.activeClickableMenu.GetType().Name;
                    Monitor.Log($"active menu: {name}", LogLevel.Info);
                    printClickableComponents(Game1.activeClickableMenu);

                    var child = Game1.activeClickableMenu.GetChildMenu();
                    if (child != null)
                    {
                        Monitor.Log($"child: {child.GetType().Name}", LogLevel.Info);
                        printClickableComponents(child);
                    }
                    else{
                        Monitor.Log("No child", LogLevel.Info);
                    }
                }
            }
        }

        public void printClickableComponents(IClickableMenu menu)
        {
            var list = menu.allClickableComponents;
            if(list != null)
            {
                foreach (var item in list)
                {
                    Monitor.Log($"clickable item: {item.name}", LogLevel.Info);
                }
            }
            else
            {
                Monitor.Log($"no clickable components", LogLevel.Info);
            }
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
                    if (hasGrandpaFarm && farmType.Equals("0"))
                    {
                        coordinatesList.Add(new Coordinates("Farm/Default", "Farm", 95, 49));
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
