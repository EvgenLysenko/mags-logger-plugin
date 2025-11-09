using Dronelogbook.Model;
using GMap.NET;
using GMap.NET.WindowsForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using IGraphics = System.IGraphics;

namespace MagsLogger
{
    public class MagsOverlay: GMapOverlay
    {
        const int MAGS_MAX_NUMBER = 4;

        static readonly PointF startPos = new PointF(.01f, .05f);
        static readonly SizeF magMarkerSize = new SizeF(.03f, 2f/3f);
        static readonly float magMarkerGap = .01f;

        static readonly Font magsFont = new Font(FontFamily.GenericMonospace, 20, FontStyle.Bold, GraphicsUnit.Point);

        static readonly Color onlineColor = Color.Green;
        static readonly Color offlineColor = Color.Gray;

        static readonly System.Drawing.Pen onlinePen = new System.Drawing.Pen(onlineColor, 4);
        static readonly System.Drawing.Pen offlinePen = new System.Drawing.Pen(offlineColor, 4);
        static readonly Brush backgroundBrush = new SolidBrush(Color.FromArgb(0x30, 0xff, 0xff, 0xff));
        static readonly Brush onlineBrush = Brushes.Green;
        static readonly Brush offlineBrush = Brushes.Gray;

        public bool isActive = false;

        int magsCount = 0;
        readonly List<float> mags = new List<float>();

        public static double markerScale = 1;

        public double zoom = 1;

        public int Ccr { get; internal set; }
        public bool GpsFixed { get; internal set; }
        public int MagsFps { get; internal set; }
        public int GpsFps { get; internal set; }
        public int AttitudeFps { get; internal set; }
        public bool LoggingStarted { get; internal set; }

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
            g.DrawString("Mags: " + MagsFps.ToString(), magsFont, brush, x, y);
            y += magsFont.Size * 1.5f;
            g.DrawString("Att: " + AttitudeFps.ToString(), magsFont, brush, x, y);
            y += magsFont.Size * 1.5f;
            g.DrawString("GPS: " + GpsFps.ToString(), magsFont, brush, x, y);
            y += magsFont.Size * 1.5f;
            g.DrawString("CCR: " + Ccr.ToString(), magsFont, brush, x, y);
            y += magsFont.Size * 1.5f;
            g.DrawString("Log: " + (LoggingStarted ? "Started" : "no"), magsFont, brush, x, y);
            y += magsFont.Size * 1.5f;

            int idx = 0;
            foreach (float value in mags)
            {
                bool isOnline = value > 0 && isActive;
                Color color = isOnline ? onlineColor : offlineColor;

                if (backgroundBrush != null)
                    g.FillRectangle(backgroundBrush, x, y, width, height);

                g.DrawRectangle(isOnline ? onlinePen: offlinePen, x, y, width, height);
                g.DrawString((idx + 1).ToString(), magsFont, isOnline ? onlineBrush: offlineBrush, x, y);

                y += height + gap;
                ++idx;
            }
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

        internal void setMagsCount(int magsCount)
        {
            if (magsCount < 0)
                return;

            this.magsCount = magsCount < MAGS_MAX_NUMBER ? magsCount : MAGS_MAX_NUMBER;

            while (mags.Count < this.magsCount && mags.Count < MAGS_MAX_NUMBER)
            {
                mags.Add(0);
            }
        }

        public int getMagsCount()
        {
            return this.magsCount;
        }

        internal void setMagStatus(int magIdx, float value)
        {
            if (magIdx >= 0 && magIdx < mags.Count)
            {
                this.mags[magIdx] = value;
            }
        }
    }
}
