using GMap.NET;
using GMap.NET.WindowsForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using IGraphics = System.IGraphics;

namespace MagsLogger
{
    public class OverlayBase : GMapOverlay
    {
        public static double COORDINATES_TOLERANCE = Location.toRad(0.000001);
        public static float ANGLE_TOLERANCE = (float)Location.toRad(0.1);
        public static float ALT_TOLERANCE = 0.1f;

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

        protected double zoom = 1;

        public double getZoom() { return zoom; }

        public int getScreenLeft()
        {
            return -Control.Width / 2;
        }

        public int getScreenTop()
        {
            return -Control.Height / 2;
        }

        public int getScreenWidth()
        {
            return Control.Width * 2;
        }

        public int getScreenHeight()
        {
            return Control.Height * 2;
        }

        public static float toDeg(float rad)
        {
            return (float)(rad * 180f / Math.PI);
        }

        public static float toRad(float deg)
        {
            return (float)(deg * Math.PI / 180.0f);
        }

        public static double toDeg(double rad)
        {
            return rad * 180 / Math.PI;
        }

        public static double toRad(double deg)
        {
            return deg * Math.PI / 180;
        }

        public void onZoomChanged(double zoom)
        {
            if (this.zoom != zoom)
            {
                this.zoom = zoom;
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

        public void clearObject(string name)
        {
            removeRoute(name);
            removePolygon(name);
        }

        protected void addPolygon(string name, List<PointLatLng> list, PointF polygonType, Color color)
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

        // radians
        public bool isIn(double lat, double lon, double clickLat, double clickLon, float radius)
        {
            float markerZoomScale = (float)(1 + MARKER_SCALE_FACTOR * (zoom - MARKER_SCALE_BASE_ZOOM) / MARKER_SCALE_BASE_ZOOM);
            float k = (float)(4 * MARKER_SCALE * markerZoomScale / Math.Pow(2, zoom));
            float kxy = (float)Math.Cos(lat);

            lat = toDeg(lat);
            lon = toDeg(lon);

            double dLat = radius * k * kxy;
            double dLon = radius * k;

            return Math.Abs(clickLat - lat) <= dLat && Math.Abs(clickLon - lon) <= dLon;
        }

        // radians
        public void addMarker(string name, double lat, double lon, PointF[] points, Color color, float yaw = 0)
        {
            List<PointLatLng> list = new List<PointLatLng>();

            float markerZoomScale = (float)(1 + MARKER_SCALE_FACTOR * (zoom - MARKER_SCALE_BASE_ZOOM) / MARKER_SCALE_BASE_ZOOM);
            float k = (float)(4 * MARKER_SCALE * markerZoomScale / Math.Pow(2, zoom));
            float kxy = (float)Math.Cos(lat);

            lat = toDeg(lat);
            lon = toDeg(lon);

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

        // radians
        public void addMarker(string name, double lat, double lon, PointF[] points, float scale, Color color, float yaw = 0)
        {
            List<PointLatLng> list = new List<PointLatLng>();

            float markerZoomScale = (float)(1 + MARKER_SCALE_FACTOR * (zoom - MARKER_SCALE_BASE_ZOOM) / MARKER_SCALE_BASE_ZOOM);
            float k = (float)(4 * MARKER_SCALE * markerZoomScale / Math.Pow(2, zoom));
            float kxy = (float)Math.Cos(lat);

            lat = toDeg(lat);
            lon = toDeg(lon);

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
