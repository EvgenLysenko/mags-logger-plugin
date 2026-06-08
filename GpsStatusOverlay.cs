using GMap.NET;
using GMap.NET.WindowsForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using IGraphics = System.IGraphics;

namespace MagsLogger
{
    public class GpsStatusOverlay : GMapOverlay
    {
        public static long LOCATION_INACTIVE_TIMEOUT = 3 * 1000;
        private static readonly string LOCATION_OBJECT_NAME = "mags_gps_raw";

        public static double COORDINATES_TOLERANCE = Location.toRad(0.000001);
        public static float ANGLE_TOLERANCE = (float)Location.toRad(0.1);
        public static float ALT_TOLERANCE = 0.1f;

        public static readonly PointF[] markerPoints = {
            new PointF(-3, -3),
            new PointF(0, 5),
            new PointF(3, -3),
            new PointF(0, -1),
        };

        public long locationTime = 0;
        public Location location = new Location();
        public Location prevLocation = new Location();
        public float yaw = 0;
        public bool isLocation() { return location.lat != 0 || location.lon != 0; }
        protected bool locationIsActive = false;

        static readonly Color markerForegroundColor = Color.Blue;
        static readonly Color markerBackgroundColor = Color.Yellow;
        static readonly Color markerOfflineColor = Color.Gray;

        static readonly PointF startPos = new PointF(.01f, .05f);
        static readonly SizeF magMarkerSize = new SizeF(.03f, 2f/3f);
        static readonly float magMarkerGap = .01f;

        static readonly Font magsFont = new Font(FontFamily.GenericMonospace, 20, FontStyle.Bold, GraphicsUnit.Point);
        static readonly Font batFont = new Font(FontFamily.GenericSansSerif, 64, FontStyle.Bold, GraphicsUnit.Point);
        static readonly Font batInnerFont = new Font(FontFamily.GenericSansSerif, 64, FontStyle.Regular, GraphicsUnit.Point);

        static readonly Color onlineColor = Color.Green;
        static readonly Color offlineColor = Color.Gray;

        static readonly System.Drawing.Pen onlinePen = new System.Drawing.Pen(onlineColor, 4);
        static readonly System.Drawing.Pen offlinePen = new System.Drawing.Pen(offlineColor, 4);
        static readonly Brush backgroundBrush = new SolidBrush(Color.FromArgb(0x30, 0xff, 0xff, 0xff));
        static readonly Brush onlineBrush = Brushes.Green;
        static readonly Brush offlineBrush = Brushes.Gray;

        public static bool isTimeout(long time, long timeout)
        {
            return time + timeout < DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        public static bool isTheSame(double lat1, double lon1, double lat2, double lon2)
        {
            return Math.Abs(lat1 - lat2) <= COORDINATES_TOLERANCE && Math.Abs(lon1 - lon2) <= COORDINATES_TOLERANCE;
        }

        public static bool isAngleTheSame(float angle1, float angle2)
        {
            return Math.Abs(angle1 - angle2) <= ANGLE_TOLERANCE;
        }

        public static bool isAltTheSame(float alt1, float alt2)
        {
            return Math.Abs(alt1 - alt2) <= ALT_TOLERANCE;
        }

        public bool isActive = false;

        public static double markerScale = 1;

        public double zoom = 1;

        int getScreenLeft()
        {
            return -Control.Width / 2;
        }

        int getScreenTop()
        {
            return -Control.Height / 2;
        }

        int getScreenWidth()
        {
            return Control.Width * 2;
        }

        int getScreenHeight()
        {
            return Control.Height * 2;
        }

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

            String text = "Mags: ";
            //g.DrawString("GPS: " + GpsFps.ToString(), magsFont, brush, x, y);
            y += magsFont.Size * 1.5f;
        }

        public static float toRad(float deg)
        {
            return (float)(deg * Math.PI / 180.0f);
        }

        public void onZoomChanged(double zoom)
        {
            if (this.zoom != zoom)
            {
                this.zoom = zoom;
            }
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
                    Location.getDistanceDirection(prevLocation, location, out distance, out direction);
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

                addMarker(LOCATION_OBJECT_NAME, location.lat, location.lon, markerPoints, 1.33f, _isTimeout ? markerOfflineColor : markerBackgroundColor, toRad(yaw));
                addMarker(LOCATION_OBJECT_NAME, location.lat, location.lon, markerPoints, _isTimeout ? markerOfflineColor : markerForegroundColor, toRad(yaw));
            }
        }

        #region objects on map

        readonly float MARKER_SCALE_FACTOR = 0.75f;
        readonly float MARKER_SCALE_BASE_ZOOM = 12;
        readonly float MARKER_SCALE = 0.75f;

        public static readonly PointF POLIGONE_END = new PointF(float.MaxValue, float.MaxValue);
        public static readonly PointF LINES_END = new PointF(float.MinValue, float.MinValue);

        static readonly float targetLineWidth = 2;

        private void removeRoute(string name)
        {
            bool wasRemoved = true;
            int count = this.Routes.Count;
            while (wasRemoved)
            {
                wasRemoved = false;
                GMapRoute found = null;
                foreach (GMapRoute route in this.Routes)
                {
                    if (route.Name == name)
                    {
                        found = route;
                        break;
                    }
                }

                if (found != null)
                {
                    this.Routes.Remove(found);
                    wasRemoved = true;
                }

                if (count == this.Routes.Count)
                    break;
            }
        }

        void removePolygon(string name)
        {
            bool wasRemoved = true;
            int count = this.Polygons.Count;
            while (wasRemoved)
            {
                wasRemoved = false;
                GMapPolygon found = null;
                foreach (GMapPolygon item in this.Polygons)
                {
                    if (item.Name == name)
                    {
                        found = item;
                        break;
                    }
                }

                if (found != null)
                {
                    this.Polygons.Remove(found);
                    wasRemoved = true;
                }

                if (count == this.Polygons.Count)
                    break;
            }
        }

        void clearObject(string name)
        {
            removeRoute(name);
            removePolygon(name);
        }

        void addPolygon(string name, List<PointLatLng> list, PointF polygonType, Color color)
        {
            if (list.Count <= 0)
                return;

            if (polygonType == LINES_END)
            {
                GMapRoute route = new GMapRoute(list, name);

                route.Stroke = new Pen(color, targetLineWidth);

                this.Routes.Add(route);
            }
            else
            {
                GMapPolygon polygon = new GMapPolygon(list, name);

                polygon.Stroke = new Pen(color, targetLineWidth);
                polygon.Fill = Brushes.Transparent;

                this.Polygons.Add(polygon);
            }
        }

        public bool isIn(double lat, double lon, double clickLat, double clickLon, float radius)
        {
            float markerZoomScale = (float)(1 + MARKER_SCALE_FACTOR * (zoom - MARKER_SCALE_BASE_ZOOM) / MARKER_SCALE_BASE_ZOOM);
            float k = (float)(4 * MARKER_SCALE * markerZoomScale / Math.Pow(2, zoom));
            float kxy = (float)Math.Cos(lat);

            //lat = toDeg(lat);
            //lon = toDeg(lon);

            double dLat = radius * k * kxy;
            double dLon = radius * k;

            return Math.Abs(clickLat - lat) <= dLat && Math.Abs(clickLon - lon) <= dLon;
        }

        void addMarker(string name, double lat, double lon, PointF[] points, Color color, float yaw = 0)
        {
            List<PointLatLng> list = new List<PointLatLng>();

            float markerZoomScale = (float)(1 + MARKER_SCALE_FACTOR * (zoom - MARKER_SCALE_BASE_ZOOM) / MARKER_SCALE_BASE_ZOOM);
            float k = (float)(4 * MARKER_SCALE * markerZoomScale / Math.Pow(2, zoom));
            float kxy = (float)Math.Cos(lat);

            //lat = toDeg(lat);
            //lon = toDeg(lon);

            foreach (PointF p in points)
            {
                if (p == POLIGONE_END || p == LINES_END)
                {
                    addPolygon(name, list, LINES_END, color);
                    list.Clear();
                }
                else
                {
                    float x = p.X;
                    float y = p.Y;

                    if (yaw != 0)
                    {
                        float s = (float)Math.Sin(yaw);
                        float c = (float)Math.Cos(yaw);

                        x = -p.X * c + p.Y * s;
                        y = p.X * s + p.Y * c;
                    }

                    list.Add(new PointLatLng(lat + y * k * kxy, lon + x * k));
                }
            }

            addPolygon(name, list, POLIGONE_END, color);

            list.Clear();
        }

        void addMarker(string name, double lat, double lon, PointF[] points, float scale, Color color, float yaw = 0)
        {
            List<PointLatLng> list = new List<PointLatLng>();

            float markerZoomScale = (float)(1 + MARKER_SCALE_FACTOR * (zoom - MARKER_SCALE_BASE_ZOOM) / MARKER_SCALE_BASE_ZOOM);
            float k = (float)(4 * MARKER_SCALE * markerZoomScale / Math.Pow(2, zoom));
            float kxy = (float)Math.Cos(lat);

            //lat = toDeg(lat);
            //lon = toDeg(lon);

            foreach (PointF p in points)
            {
                if (p == POLIGONE_END || p == LINES_END)
                {
                    addPolygon(name, list, LINES_END, color);
                    list.Clear();
                }
                else
                {
                    float s = (float)Math.Sin(yaw);
                    float c = (float)Math.Cos(yaw);

                    float x = -p.X * c + p.Y * s;
                    float y = p.X * s + p.Y * c;

                    x *= scale;
                    y *= scale;

                    list.Add(new PointLatLng(lat + y * k * kxy, lon + x * k));
                }
            }

            addPolygon(name, list, POLIGONE_END, color);

            list.Clear();
        }

        #endregion
    }
}
