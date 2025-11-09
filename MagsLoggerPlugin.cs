using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using MissionPlanner;
using MissionPlanner.Plugin;
using MissionPlanner.Utilities;

namespace MagsLogger
{
    public class MagsLoggerPlugin : Plugin
    {
        private readonly string pluginName = "Mags Logger";
        private readonly string pluginVersion = "2.0.0";
        private readonly string pluginAuthor = "Seaman";

        public override string Name { get { return pluginName; } }
        public override string Version { get { return pluginVersion; } }
        public override string Author { get { return pluginAuthor; } }

        internal MagsOverlay magsOverlay = null;

        public static readonly MAVLink.MAV_CMD COMMAND_LONG_ID = MAVLink.MAV_CMD.USER_2;
        protected int ccr = 0;

        public static long INACTIVE_TIMEOUT = 5 * 1000;
        protected long lastActiveTime = 0;

        public bool isActive()
        {
            return lastActiveTime + INACTIVE_TIMEOUT > DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        public enum MagsCommandId
        {
            MAGS_MIN = 2001,
            MAGS_MAX = 2015,
            MAGS_SENSOR1_STATUS = 2001,
            MAGS_SENSOR2_STATUS = 2002,
            MAGS_SENSOR3_STATUS = 2003,
            MAGS_SENSOR4_STATUS = 2004,
            MAGS_SENSORS_STATUS = 2011,
            MAGS_STATUS = 2012,
            MAGS_CCR_SET = 2013,
            MAGS_SETTINGS = 2014,
            MAGS_SETTINGS_REQUEST = 2015,
            MAGS_START_LOGGING = 2016,
        };

        static int STATUS_BIT_MAGS_ONLINE = 0x1;
        static int STATUS_BIT_GPS_ONLINE = 0x2;
        static int STATUS_BIT_LOG_STARTED = 0x4;

        // CHANGE THIS TO TRUE TO USE THIS PLUGIN
        public override bool Init()
        {
            this.loopratehz = 1;

            return true;
        }

        public override bool Loaded()
        {
            ToolStripMenuItem menu = new ToolStripMenuItem(pluginName);

            ToolStripMenuItem setCcrMenu = new ToolStripMenuItem("Set CCR");
            setCcrMenu.Click += changeCcrMenu_Click;
            menu.DropDownItems.Add(setCcrMenu);

            ToolStripMenuItem enableMenu = new ToolStripMenuItem("Enable/Disable");
            enableMenu.Click += enableMenu_Click;
            menu.DropDownItems.Add(enableMenu);

            ToolStripMenuItem logStartMenu = new ToolStripMenuItem("Start Logging");
            logStartMenu.Click += logStartMenu_Click;
            menu.DropDownItems.Add(logStartMenu);

            ToolStripItemCollection col = Host.FDMenuMap.Items;
            col.Add(menu);


            magsOverlay = new MagsOverlay();
            magsOverlay.IsVisibile = Settings.Instance.GetBoolean("mags_logger_enabled", true);
;
            magsOverlay.zoom = Host.FDGMapControl.Zoom;

            Host.FDGMapControl.Overlays.Add(magsOverlay);
            Host.FDGMapControl.OnMapZoomChanged += FDGMapControl_OnMapZoomChanged;


            MainV2.comPort.OnPacketReceived -= onMavlinkMessageReceived;
            MainV2.comPort.OnPacketReceived += onMavlinkMessageReceived;

            return true;
        }

        private void FDGMapControl_OnMapZoomChanged()
        {
            if (magsOverlay != null)
                magsOverlay.onZoomChanged(Host.FDGMapControl.Zoom);

            Host.FDGMapControl.Position = Host.FDGMapControl.Position;
        }

        void enableMenu_Click(object sender, EventArgs e)
        {
            magsOverlay.IsVisibile = !magsOverlay.IsVisibile;
            Settings.Instance["mags_logger_enabled"] = magsOverlay.IsVisibile.ToString();
        }

        void logStartMenu_Click(object sender, EventArgs e)
        {
            sendCommand(MagsCommandId.MAGS_START_LOGGING);
        }

        void changeCcrMenu_Click(object sender, EventArgs e)
        {
            UpdateCcrForm form = new UpdateCcrForm();

            form.ccr = ccr;

            if (form.ShowDialog() == DialogResult.OK)
            {
                ccr = form.ccr;

                if (ccr > 0)
                {
                    sendCommand(MagsCommandId.MAGS_CCR_SET, ccr);
                }
            }
        }

        public override bool Loop()
        {
            magsOverlay.isActive = isActive();

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
                case MAVLink.MAVLINK_MSG_ID.COMMAND_LONG:
                {
                    MAVLink.mavlink_command_long_t command_long = (MAVLink.mavlink_command_long_t)message.data;
                    if (command_long.command == (ushort)COMMAND_LONG_ID) {
                        MagsCommandId commandId = (MagsCommandId)ParseUtils.toInt(command_long.param1);
                        if (commandId >= MagsCommandId.MAGS_MIN && commandId <= MagsCommandId.MAGS_MAX) {
                            onMagsCommandReceived(command_long);
                        }
                    }
                    break;
                }
            }
        }

        private void onMagsCommandReceived(MAVLink.mavlink_command_long_t command_long)
        {
            lastActiveTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            MagsCommandId magsCommandId = (MagsCommandId)ParseUtils.toInt(command_long.param1);

            switch (magsCommandId)
            {
                case MagsCommandId.MAGS_STATUS:
                {
                    int magsCount = ParseUtils.toInt(command_long.param2);
                    magsOverlay.setMagsCount(magsCount);

                    magsOverlay.GpsFixed = command_long.param3 > 0;
                    magsOverlay.MagsFps = ParseUtils.toInt(command_long.param4);
                    magsOverlay.GpsFps = ParseUtils.toInt(command_long.param5);
                    magsOverlay.AttitudeFps = ParseUtils.toInt(command_long.param6);

                    int status = ParseUtils.toInt(command_long.param7);
                    magsOverlay.LoggingStarted = (status & STATUS_BIT_LOG_STARTED) == STATUS_BIT_LOG_STARTED;

                    if (magsOverlay.MagsFps > 0 && ccr <= 0)
                    {
                        sendCommand(MagsCommandId.MAGS_SETTINGS_REQUEST, ccr);
                    }

                    break;
                }
                case MagsCommandId.MAGS_SENSORS_STATUS:
                {
                    int magIdx = ParseUtils.toInt(command_long.param2);
                    if (magIdx >= 0 && magIdx < magsOverlay.getMagsCount())
                    {
                        if (command_long.param3 >= 0)
                            magsOverlay.setMagStatus(magIdx, command_long.param3);

                        if (++magIdx < magsOverlay.getMagsCount() && command_long.param4 >= 0)
                            magsOverlay.setMagStatus(magIdx, command_long.param4);

                        if (++magIdx < magsOverlay.getMagsCount() && command_long.param5 >= 0)
                            magsOverlay.setMagStatus(magIdx, command_long.param5);

                        if (++magIdx < magsOverlay.getMagsCount() && command_long.param6 >= 0)
                            magsOverlay.setMagStatus(magIdx, command_long.param6);
                    }

                    break;
                }
                case MagsCommandId.MAGS_SETTINGS:
                {
                    int magsCount = ParseUtils.toInt(command_long.param2);
                    magsOverlay.setMagsCount(magsCount);

                    ccr = ParseUtils.toInt(command_long.param3);
                    magsOverlay.Ccr = ccr;

                    break;
                }
            }
        }

        public void sendCommand(MagsCommandId commandId, float value1 = 0, float value2 = 0, float value3 = 0)
        {
            if (MainV2.comPort.BaseStream == null || !Host.comPort.BaseStream.IsOpen)
            {
                CustomMessageBox.Show(Strings.CommunicationErrorNoConnection, Strings.ERROR);
                return;
            }

            try
            {
                MainV2.comPort.doCommand(
                    (byte)MainV2.comPort.sysidcurrent,
                    (byte)MAVLink.MAV_COMPONENT.MAV_COMP_ID_ONBOARD_COMPUTER,
                    COMMAND_LONG_ID,
                    (int)commandId,
                    value1, value2, value3,
                    0, 0, 0, false);
            }
            catch
            {
                CustomMessageBox.Show(Strings.CommandFailed, Strings.ERROR);
            }
        }

        public void sendCommandAsync(MagsCommandId commandId, float value1 = 0, float value2 = 0, float value3 = 0)
        {
            if (MainV2.comPort.BaseStream == null || !Host.comPort.BaseStream.IsOpen)
            {
                CustomMessageBox.Show(Strings.CommunicationErrorNoConnection, Strings.ERROR);
                return;
            }

            try
            {
                MainV2.comPort.doCommandAsync(
                    (byte)MainV2.comPort.sysidcurrent,
                    (byte)MAVLink.MAV_COMPONENT.MAV_COMP_ID_ONBOARD_COMPUTER,
                    COMMAND_LONG_ID,
                    (int)commandId,
                    value1, value2, value3,
                    0, 0, 0, false);
            }
            catch
            {
                CustomMessageBox.Show(Strings.CommandFailed, Strings.ERROR);
            }
        }
    }
}
