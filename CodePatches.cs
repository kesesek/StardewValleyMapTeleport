using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;

namespace MapTeleport
{
    public partial class ModEntry
    {
        protected static CoordinatesList addedCoordinates;

        public static bool CheckClickableComponents(string mapName, List<ClickableComponent> components, int topX, int topY, int x, int y)
        {
            SMonitor.Log($"clicked x:{x} y:{y}", LogLevel.Debug);
            if (!Config.ModEnabled)
                return false;

            if (addedCoordinates == null)
            {
                addedCoordinates = SHelper.Data.ReadJsonFile<CoordinatesList>("coordinates.json");
                if (addedCoordinates == null) addedCoordinates = new CoordinatesList();
            }

            var coordinates = SHelper.GameContent.Load<CoordinatesList>(dictPath);
            bool found = false;

            SMonitor.Log("All clickable components:", LogLevel.Debug);
            foreach (ClickableComponent component in components)
            {
                SMonitor.Log($"Component: ID={component.myID}, Name={component.name}, Bounds={component.bounds}, Visible={component.visible}", LogLevel.Debug);
            }

            Dictionary<string, Coordinates> coordinatesDict = new Dictionary<string, Coordinates>(StringComparer.OrdinalIgnoreCase);
            foreach (var coord in coordinates.coordinates)
            {
                if (!coordinatesDict.ContainsKey(coord.displayName))
                {
                    coordinatesDict[coord.displayName] = coord;
                }
            }

            foreach (ClickableComponent component in components)
            {
                if (coordinatesDict.TryGetValue(component.name, out Coordinates tpCoordinate))
                {
                    if (component.containsPoint(x, y) && component.visible)
                    {
                        SMonitor.Log($"Teleporting to {tpCoordinate.displayName}\nCoordinate: {tpCoordinate.teleportName}({tpCoordinate.x},{tpCoordinate.y})", LogLevel.Debug);
                        Game1.activeClickableMenu?.exitThisMenu(true);
                        Game1.warpFarmer(tpCoordinate.teleportName, tpCoordinate.x, tpCoordinate.y, false);
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                SMonitor.Log("No teleportation coordinate found.", LogLevel.Debug);
            }

            return found;
        }

        [HarmonyPatch(typeof(MapPage), nameof(MapPage.receiveLeftClick))]
        public class MapPage_receiveLeftClick_Patch
        {
            public static bool Prefix(MapPage __instance, int x, int y)
            {
                List<ClickableComponent> clickableComponents = new List<ClickableComponent>(__instance.points.Values);
                string mapName = __instance.GetType().Name;
                SMonitor.Log($"clicked on {mapName}", LogLevel.Info);
                bool found = CheckClickableComponents(mapName, clickableComponents, __instance.xPositionOnScreen, __instance.yPositionOnScreen, x, y);
                return !found;
            }
        }


        public static bool CheckPositionForTeleport(int x, int y, object mapMenu)
        {
            // 通过反射获取 MapData 和 TopLeft 字段
            var mapData = GetPrivateField<object>(mapMenu, "MapData");
            var topLeft = GetPrivateField<Vector2>(mapMenu, "TopLeft");

            if (mapData == null || topLeft.Equals(default(Vector2)))
            {
                SMonitor.Log("No mapData or topLeft is default", LogLevel.Info);
                return false;
            }
            SMonitor.Log($"mapData: {mapData.GetType().Name}", LogLevel.Info);

            // 通过反射获取 Locations 字段
            var locations = GetPrivateField<object>(mapData, "<Locations>k__BackingField");

            if (locations == null)
            {
                SMonitor.Log("No locations found", LogLevel.Info);
                return false;
            }

            // 计算点击位置在地图上的相对位置
            int relativeX = x - (int)topLeft.X;
            int relativeY = y - (int)topLeft.Y;
            // To do: 打印检查relative X/Y，看是不是在不同窗口大小下都一样，
            // 一样的话就把altID加回来。第131行应该修改，在比对后传送到预设的coordinate的坐标上

            // 获取 locations 字段的类型
            var locationsType = locations.GetType();
            if (locationsType.IsGenericType && locationsType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var valueType = locationsType.GetGenericArguments()[1];
                foreach (var entry in (dynamic)locations)
                {
                    var mapLocation = entry.Value;
                    var areaRect = GetPropertyOrField<Rectangle>(mapLocation, "AreaRect");
                    var text = GetPropertyOrField<string>(mapLocation, "Text");
                    var value = areaRect.ToString();
                    SMonitor.Log($"{value} and {text}", LogLevel.Info);

                    // 检查鼠标是否在某个位置的区域内
                    if (areaRect.Contains(relativeX, relativeY))
                    {
                        SMonitor.Log($"Teleporting to {text}\nAreaRect: {areaRect.X},{areaRect.Y}", LogLevel.Debug);
                        Game1.activeClickableMenu?.exitThisMenu(true);
                        Game1.warpFarmer(text, areaRect.X, areaRect.Y, false);
                        return true;
                    }

                }
            }
            else
            {
                SMonitor.Log($"Unexpected type for locations: {locations.GetType().Name}", LogLevel.Error);
            }

            SMonitor.Log("Last line, nothing founded.", LogLevel.Info);
            return false;
        }

        private static T GetPrivateField<T>(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            if (field == null)
            {
                SMonitor.Log($"Field {fieldName} not found in {obj.GetType().Name}", LogLevel.Info);
                return default(T);
            }
            var value = field.GetValue(obj);
            if (value == null)
            {
                SMonitor.Log($"Field {fieldName} value is null in {obj.GetType().Name}", LogLevel.Info);
                return default(T);
            }
            return (T)value;
        }

        private static T GetPropertyOrField<T>(object obj, string name)
        {
            var type = obj.GetType();
            var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (prop != null && prop.PropertyType == typeof(T))
            {
                return (T)prop.GetValue(obj);
            }
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null && field.FieldType == typeof(T))
            {
                return (T)field.GetValue(obj);
            }
            return default;
        }



        [HarmonyPatch(typeof(IClickableMenu), nameof(IClickableMenu.receiveLeftClick))]
        public class RSVMapPage_receiveLeftClick_Patch
        {
            public static bool Prefix(IClickableMenu __instance, int x, int y)
            {
                string mapName = __instance.GetType().Name;
                SMonitor.Log($"clicked on {mapName}, {x}, {y}", LogLevel.Info);

                if (mapName.Equals("RSVWorldMap"))
                {
                    bool found = CheckPositionForTeleport(x, y, __instance);
                    return !found;
                }
                return true;
            }
        }


    }
}