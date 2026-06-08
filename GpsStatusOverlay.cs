using GMap.NET;
using GMap.NET.WindowsForms;
using System;
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

        protected GpsRequestResult gpsRequestResult = GpsRequestResult.GPS_REQUEST_RESULT_NA;

        public long locationTime = 0;
        public Location location = new Location(); // degrees
        public Location prevLocation = new Location(); // degrees
        public float yaw = 0; // degrees
        public bool isLocation() { return location.lat != 0 || location.lon != 0; }
        protected bool locationIsActive = false;

        static readonly Color markerForegroundColor = Color.Blue;
        static readonly Color markerBackgroundColor = Color.Yellow;
        static readonly Color markerOfflineColor = Color.Gray;

        static readonly PointF startPos = new PointF(.3f, .05f);

        static readonly Font magsFont = new Font(FontFamily.GenericMonospace, 20, FontStyle.Bold, GraphicsUnit.Point);

        static readonly Color onlineColor = Color.Green;
        static readonly Color offlineColor = Color.Gray;
        static readonly Color proggressColor = Color.Blue;
        static readonly Color failedColor = Color.Red;

        static readonly Brush onlineBrush = Brushes.Green;
        static readonly Brush offlineBrush = Brushes.Gray;
        static readonly Brush proggressBrush = Brushes.Blue;
        static readonly Brush failedBrush = Brushes.Red;

        public static bool isTimeout(long time, long timeout)
        {
            return time + timeout < DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        public bool isActive = false;

        public override void OnRender(IGraphics g)
        {
            base.OnRender(g);

            float screenWidth = getScreenWidth();
            float screenHeight = getScreenHeight();
            float x = getScreenLeft() + screenWidth * startPos.X;
            float y = getScreenTop() + screenHeight * startPos.Y;

            Brush brush = isActive ? Brushes.Orange : Brushes.Gray;

            String text = "GPS: ";

            if (MagsLogger.isStatus((int)gpsRequestResult, (int)GpsRequestResult.GPS_REQUEST_RESULT_UNDEFINED))
                text += "N/A";
            else if (MagsLogger.isStatus((int)gpsRequestResult, (int)GpsRequestResult.GPS_REQUEST_RESULT_ON))
                text += "ON";
            else if (MagsLogger.isStatus((int)gpsRequestResult, (int)GpsRequestResult.GPS_REQUEST_RESULT_OFF))
                text += "OFF";
            else
                text += "--";

            if (MagsLogger.isStatus((int)gpsRequestResult, (int)GpsRequestResult.GPS_REQUEST_RESULT_OFF_SUCCESS))
            {
                //text += " Success";
                brush = Brushes.Orange;
            }
            else if (MagsLogger.isStatus((int)gpsRequestResult, (int)GpsRequestResult.GPS_REQUEST_RESULT_SUCCESS))
            {
                //text += " Success";
                brush = onlineBrush;
            }
            else if (MagsLogger.isStatus((int)gpsRequestResult, (int)GpsRequestResult.GPS_REQUEST_RESULT_IN_PROGRESS))
            {
                text += " in proggress";
                brush = proggressBrush;
            }
            else if (MagsLogger.isStatus((int)gpsRequestResult, (int)GpsRequestResult.GPS_REQUEST_RESULT_FAILED))
            {
                text += " !FAILED!";
                brush = failedBrush;
            }
            else
            {
                text += " !UNDEFINED!";
                brush = failedBrush;
            }

            if (gpsRequestResult == 0)
            {
                text = "GPS: --";
                brush = onlineBrush;
            }

            if (!isActive)
                brush = Brushes.Gray;

            g.DrawString(text, magsFont, brush, x, y);
            y += magsFont.Size * 1.5f;
        }

        public void updateGpsStatus(GpsRequestResult gpsRequestResult)
        {
            this.gpsRequestResult = gpsRequestResult;
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
