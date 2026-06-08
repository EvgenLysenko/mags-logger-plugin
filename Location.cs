using System;

namespace MagsLogger
{
    public class Location
    {
        public static readonly double a = 6378137.0000;
        public static readonly double b = 6356752.3141;

        public Location()
        {
            this.lat = 0;
            this.lon = 0;
        }

        public Location(double lat, double lon)
        {
            this.lat = lat;
            this.lon = lon;
        }

        public double lat;
        public double lon;

        public static double toDeg(double rad)
        {
            return rad * 180 / Math.PI;
        }

        public static double toRad(double deg)
        {
            return deg * Math.PI / 180;
        }

        // radians, meters
        public static void getDistanceDirection(double lat, double lon, double lat2, double lon2, out double distance, out double direction)
        {
            double latMiddle = (lat2 + lat) / 2;
            double dx = (lon2 - lon) * a * Math.Cos(latMiddle) / (2 * Math.PI);
            double dy = (lat2 - lat) * b / (2 * Math.PI);

            if (dx == 0 && dy == 0)
            {
                direction = 0;
                distance = 0;
                return;
            }

            distance = Math.Sqrt(dx * dx + dy * dy);
            direction = Math.PI / 2 - Math.Atan2(dy, dy);
        }

        public static void getDistanceDirection(Location loc, Location loc2, out double distance, out double direction)
        {
            getDistanceDirection(loc.lat, loc.lon, loc2.lat, loc2.lon, out distance, out direction);
        }
    }
}
