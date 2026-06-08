using GMap.NET;
using GMap.NET.WindowsForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using IGraphics = System.IGraphics;

namespace MagsLogger
{
    public class GpsStatusOverlay : OverlayBase
    {
        public static long LOCATION_INACTIVE_TIMEOUT = 3 * 1000;
        private static readonly string LOCATION_OBJECT_NAME = "mags_gps_raw";

        public static readonly PointF[] markerPoints = {
            new PointF(-3, -3),
            new PointF(0, 5),
            new PointF(3, -3),
            new PointF(0, -1),
        };

        public long locationTime = 0;
        public Location location = new Location(); // degrees
        public Location prevLocation = new Location(); // degrees
        public float yaw = 0; // degrees
        public bool isLocation() { return location.lat != 0 || location.lon != 0; }
        protected bool locationIsActive = false;

        static readonly Color markerForegroundColor = Color.Blue;
        static readonly Color markerBackgroundColor = Color.Yellow;
        static readonly Color markerOfflineColor = Color.Gray;

        static readonly PointF startPos = new PointF(.9f, .05f);
        static readonly SizeF magMarkerSize = new SizeF(.03f, 2f/3f);
        static readonly float magMarkerGap = .01f;

        static readonly Font magsFont = new Font(FontFamily.GenericMonospace, 20, FontStyle.Bold, GraphicsUnit.Point);

        static readonly Color onlineColor = Color.Green;
        static readonly Color offlineColor = Color.Gray;

        public static bool isTimeout(long time, long timeout)
        {
            return time + timeout < DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        public bool isActive = false;

        public static double markerScale = 1;

        public override void OnRender(IGraphics g)
        {
            base.OnRender(g);

            float screenWidth = getScreenWidth();
            float screenHeight = getScreenHeight();
            float width = screenWidth * magMarkerSize.Width;
            float height = width * magMarkerSize.Height;
            float gap = screenWidth * magMarkerGap;
            float x = getScreenLeft() + screenWidth * startPos.X;
            float y = getScreenTop() + screenHeight * startPos.Y;

            Brush brush = isActive ? Brushes.Orange : Brushes.Gray;

            String text = "GPS status: ";
            //g.DrawString("GPS: " + GpsFps.ToString(), magsFont, brush, x, y);
            y += magsFont.Size * 1.5f;
        }

        internal void setGpsRawPosition(double lat, double lon, float yaw)
        {
            if (double.IsNaN(lat) || double.IsNaN(lon) || (lat == 0 && lon == 0))
                return;

            locationTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            if (!isTheSame(location.lat, location.lon, lat, lon) || !isAngleTheSame(this.yaw, yaw))
            {
                location.lat = lat;
                location.lon = lon;

                if (yaw == 0)
                {
                    double distance = 0;
                    double direction = 0;
                    Location.getDistanceDirection(toRad(prevLocation.lat), toRad(prevLocation.lon), toRad(location.lat), toRad(location.lon), out distance, out direction);
                    this.yaw = (float)Location.toDeg(direction);
                }
                else
                {
                    this.yaw = yaw;
                }

                updateLocationMarker();

                prevLocation.lat = location.lat;
                prevLocation.lon = location.lon;
            }
        }

        protected void checkLocation()
        {
            if (locationIsActive)
            {
                if (isTimeout(locationTime, LOCATION_INACTIVE_TIMEOUT))
                {
                    locationIsActive = false;
                    updateLocationMarker();
                }
            }
            else
            {
                if (!isTimeout(locationTime, LOCATION_INACTIVE_TIMEOUT))
                {
                    locationIsActive = true;
                    updateLocationMarker();
                }
            }
        }

        protected void updateLocationMarker()
        {
            clearObject(LOCATION_OBJECT_NAME);

            if (isLocation())
            {
                bool _isTimeout = isTimeout(locationTime, LOCATION_INACTIVE_TIMEOUT);

                addMarker(LOCATION_OBJECT_NAME, toRad(location.lat), toRad(location.lon), markerPoints, 1.33f, _isTimeout ? markerOfflineColor : markerBackgroundColor, toRad(yaw));
                addMarker(LOCATION_OBJECT_NAME, toRad(location.lat), toRad(location.lon), markerPoints, _isTimeout ? markerOfflineColor : markerForegroundColor, toRad(yaw));
            }
        }
    }
}
