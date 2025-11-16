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
        class Mag
        {
            public int x = 0;
            public int y = 0;
            public int z = 0;

            public bool isOnline = false;
        }

        class Vector
        {
            public int x = 0;
            public int y = 0;
            public int z = 0;
        }

        readonly List<Mag> mags = new List<Mag>();

        Vector accel = new Vector();
        Vector gyro = new Vector();
        Vector accelMag = new Vector();

        public static double markerScale = 1;

        public double zoom = 1;

        public int Ccr { get; internal set; }
        public bool GpsFixed { get; internal set; }
        public int MagsFps { get; internal set; }
        public int LogoutFps { get; internal set; }
        public int LogoutTime { get; internal set; }
        public int GpsFps { get; internal set; }
        public int AttitudeFps { get; internal set; }
        public bool LoggingStarted { get; internal set; }
        public bool OutMagsDetected { get; internal set; }
        public bool OutAccelDetected { get; internal set; }
        public bool OutFullTraceEnabled { get; internal set; }
        public bool OutDebugTraceEnabled { get; internal set; }

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
            if (!OutMagsDetected && !OutAccelDetected)
                text = "Mags No data: ";
            else if (OutMagsDetected && OutAccelDetected)
                text = "Mags + Accel: ";
            else if (OutMagsDetected)
                text = "Mags: ";
            else if (OutAccelDetected)
                text = "Accel: ";

            g.DrawString(text + MagsFps.ToString(), magsFont, brush, x, y);
            y += magsFont.Size * 1.5f;

            g.DrawString("Att: " + AttitudeFps.ToString(), magsFont, brush, x, y);
            y += magsFont.Size * 1.5f;
            g.DrawString("GPS: " + GpsFps.ToString(), magsFont, brush, x, y);
            y += magsFont.Size * 1.5f;
            g.DrawString("CCR: " + Ccr.ToString(), magsFont, brush, x, y);
            y += magsFont.Size * 1.5f;
            g.DrawString("Log: " + (LoggingStarted ? "Started" : "no"), magsFont, brush, x, y);
            y += magsFont.Size * 1.5f;
            g.DrawString("Log out:" + (OutMagsDetected ? " MAGS" : "") + (OutAccelDetected ? " ACCEL" : "") + (!OutMagsDetected && !OutAccelDetected ? " NA" : "") + (OutFullTraceEnabled? " - FULL TRACE ON" : "") + (OutDebugTraceEnabled ? " - DEBUG TRACE ON" : ""), magsFont, brush, x, y);
            y += magsFont.Size * 1.5f;
            g.DrawString("Log fps: " + LogoutFps.ToString(), magsFont, brush, x, y);
            y += magsFont.Size * 1.5f;
            g.DrawString("Log time: " + LogoutTime.ToString(), magsFont, brush, x, y);
            y += magsFont.Size * 1.5f;

            if (OutMagsDetected)
            {
                for (int i = 0; i < mags.Count; ++i)
                {
                    Mag mag = mags[i];
                    g.DrawString(String.Format("Mag {0}: {1,5} {2,5} {3,5}", i, mag.x, mag.y, mag.z), magsFont, brush, x, y);
                    y += magsFont.Size * 1.5f;
                }
            }

            if (OutAccelDetected)
            {
                g.DrawString(String.Format("Accel: {0,5} {1,5} {2,5}", accel.x, accel.y, accel.z), magsFont, brush, x, y);
                y += magsFont.Size * 1.5f;
                g.DrawString(String.Format("Mag:   {0,5} {1,5} {2,5}", accelMag.x, accelMag.y, accelMag.z), magsFont, brush, x, y);
                y += magsFont.Size * 1.5f;
            }

            int idx = 0;
            foreach (Mag mag in mags)
            {
                bool isOnline = mag.isOnline && isActive;
                Color color = isOnline ? onlineColor : offlineColor;

                if (backgroundBrush != null)
                    g.FillRectangle(backgroundBrush, x, y, width, height);

                g.DrawRectangle(isOnline ? onlinePen: offlinePen, x, y, width, height);
                g.DrawString((idx + 1).ToString(), magsFont, isOnline ? onlineBrush: offlineBrush, x, y);

                //y += height + gap;
                x += width + gap;
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
                Mag mag = new Mag();
                mags.Add(mag);
            }
        }

        public int getMagsCount()
        {
            return this.magsCount;
        }

        internal void setMagOnlineStatus(int magIdx, bool isOnline)
        {
            if (magIdx >= 0 && magIdx < mags.Count)
            {
                this.mags[magIdx].isOnline = isOnline;
            }
        }

        internal void setMagValues(int magIdx, int x, int y, int z)
        {
            if (magIdx >= 0 && magIdx < mags.Count)
            {
                this.mags[magIdx].x = x;
                this.mags[magIdx].y = y;
                this.mags[magIdx].z = z;
            }
        }

        internal void setAccelValues(int x, int y, int z)
        {
            this.accel.x = x;
            this.accel.y = y;
            this.accel.z = z;
        }

        internal void setGyroValues(int x, int y, int z)
        {
            this.gyro.x = x;
            this.gyro.y = y;
            this.gyro.z = z;
        }

        internal void setAccelMagValues(int x, int y, int z)
        {
            this.accelMag.x = x;
            this.accelMag.y = y;
            this.accelMag.z = z;
        }
    }
}
