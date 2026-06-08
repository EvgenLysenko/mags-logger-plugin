using MissionPlanner;
using MissionPlanner.Plugin;
using System;

namespace MagsLogger
{
    public class GpsStatusPlugin : Plugin
    {
        public static GpsStatusPlugin Instance { get; private set; }

        private readonly string pluginName = "Gps Status";
        private readonly string pluginVersion = "0.0.1";
        private readonly string pluginAuthor = "Seaman";

        public override string Name { get { return pluginName; } }
        public override string Version { get { return pluginVersion; } }
        public override string Author { get { return pluginAuthor; } }

        internal GpsStatusOverlay overlay = null;

        public static long INACTIVE_TIMEOUT = 3 * 1000;
        protected long lastActiveTime = 0;

        public bool isActive()
        {
            return lastActiveTime + INACTIVE_TIMEOUT > DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        // CHANGE THIS TO TRUE TO USE THIS PLUGIN
        public override bool Init()
        {
            Instance = this;
            this.loopratehz = 1;

            return true;
        }

        public override bool Loaded()
        {
            overlay = new GpsStatusOverlay();
            overlay.IsVisibile = true;

            overlay.onZoomChanged(Host.FDGMapControl.Zoom);

            Host.FDGMapControl.Overlays.Add(overlay);
            Host.FDGMapControl.OnMapZoomChanged += FDGMapControl_OnMapZoomChanged;

            MainV2.comPort.OnPacketReceived -= onMavlinkMessageReceived;
            MainV2.comPort.OnPacketReceived += onMavlinkMessageReceived;

            return true;
        }

        private void FDGMapControl_OnMapZoomChanged()
        {
            if (overlay != null)
                overlay.onZoomChanged(Host.FDGMapControl.Zoom);

            Host.FDGMapControl.Position = Host.FDGMapControl.Position;
        }


        public override bool Loop()
        {
            overlay.isActive = isActive();

            if (overlay != null)
            {
                if (overlay.getZoom() != Host.FDGMapControl.Zoom)
                {
                    overlay.onZoomChanged(Host.FDGMapControl.Zoom);
                }
            }

            return true;
        }

        public override bool Exit()
        {
            return true; 
        }

        private void onMavlinkMessageReceived(object sender, MAVLink.MAVLinkMessage message)
        {
            switch ((MAVLink.MAVLINK_MSG_ID)message.msgid)
            {
                case MAVLink.MAVLINK_MSG_ID.GPS_RAW_INT:
                {
                    if (overlay != null)
                    {
                        MAVLink.mavlink_gps_raw_int_t gps_raw = (MAVLink.mavlink_gps_raw_int_t)message.data;
                        if (gps_raw.lat != int.MaxValue && gps_raw.lon != int.MaxValue)
                            overlay.setGpsRawPosition(gps_raw.lat / 1e7, gps_raw.lon / 1e7, gps_raw.yaw / 100f);
                    }
                    break;
                }
            }
        }
    }
}
