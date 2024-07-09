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

        public static bool CheckClickableComponents(List<ClickableComponent> components, int topX, int topY, int x, int y)
        {
            SMonitor.Log($"clicked x:{x} y:{y}", LogLevel.Debug);
            if (!Config.ModEnabled)
                return false;

            var allCoordinates = ModEntry.context.allCoordinates;
            bool found = false;

            Dictionary<string, Coordinates> coordinatesDict = new Dictionary<string, Coordinates>(StringComparer.OrdinalIgnoreCase);
            foreach (var coord in allCoordinates)
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
                bool found = CheckClickableComponents(clickableComponents, __instance.xPositionOnScreen, __instance.yPositionOnScreen, x, y);
                return !found;
            }
        }


        public static bool CheckPositionForTeleport(int x, int y, object mapMenu)
        {
            if (!Config.ModEnabled)
                return false;

            var allCoordinates = ModEntry.context.allCoordinates;
            SMonitor.Log($"Loaded coordinates, count {allCoordinates.Count}", LogLevel.Debug);

            var coordinatesDict = new Dictionary<string, Coordinates>();
            foreach (var coord in allCoordinates)
            {
                if (!string.IsNullOrEmpty(coord.altId))
                {
                    coordinatesDict[coord.altId] = coord;
                }
            }
            SMonitor.Log($"make dictionary, {coordinatesDict.Count}", LogLevel.Info);

            var mapData = GetPrivateField<object>(mapMenu, "MapData");
            var topLeft = GetPrivateField<Vector2>(mapMenu, "TopLeft");

            if (mapData == null || topLeft.Equals(default(Vector2)))
            {
                SMonitor.Log("No mapData or topLeft is default", LogLevel.Error);
                return false;
            }

            var locations = GetPrivateField<object>(mapData, "<Locations>k__BackingField");
            if (locations == null)
            {
                SMonitor.Log("No locations found", LogLevel.Error);
                return false;
            }

            // get relative locations of left clicks
            int relativeX = x - (int)topLeft.X;
            int relativeY = y - (int)topLeft.Y;

            var locationsType = locations.GetType();
            if (!locationsType.IsGenericType || locationsType.GetGenericTypeDefinition() != typeof(Dictionary<,>))
            {
                SMonitor.Log($"Unexpected type for locations: {locations.GetType().Name}", LogLevel.Error);
                return false;
            }

            foreach (var entry in (dynamic)locations)
            {
                var mapLocation = entry.Value;
                var areaRect = GetPropertyOrField<Rectangle>(mapLocation, "AreaRect");
                string areaRectCoord = $"{areaRect.X}.{areaRect.Y}";
                var text = GetPropertyOrField<string>(mapLocation, "Text");
                SMonitor.Log($"{areaRectCoord} and {text}", LogLevel.Info);

                // Check if clicked in any area
                if (areaRect.Contains(relativeX, relativeY))
                {
                    if (coordinatesDict.TryGetValue(areaRectCoord, out var coord))
                    {
                        SMonitor.Log($"Found match: {coord.altId}. Teleporting to {coord.displayName} at ({coord.x}, {coord.y})", LogLevel.Debug);
                        Game1.activeClickableMenu?.exitThisMenu(true);
                        Game1.warpFarmer(coord.teleportName, coord.x, coord.y, false);
                        return true;
                    }
                    SMonitor.Log($"No matching coordinate found", LogLevel.Warn);
                    return false;
                }
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