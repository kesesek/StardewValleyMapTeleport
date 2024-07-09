using Microsoft.Xna.Framework;
using StardewValley.Menus;
using System.Collections.Generic;
using System.Linq;

namespace MapTeleport
{
    public class CoordinatesList
    {
        public List<Coordinates> coordinates = new List<Coordinates>();

        public void AddAll(CoordinatesList other)
        {
            this.coordinates = this.coordinates.Concat(other.coordinates).ToList<Coordinates>();
        }
        public void Add(Coordinates other)
        {
            this.coordinates.Add(other);
        }
    }
    public class Coordinates
    {
        public string label;
        public string altId;
        public string displayName;
        public string teleportName;
        public int x;
        public int y;

        public Coordinates(string displayName, string teleportName, int x, int y)
        {
            this.displayName = displayName;
            this.teleportName = teleportName;
            this.x = x;
            this.y = y;
        }
    }

}