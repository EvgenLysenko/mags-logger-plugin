using MissionPlanner;
using MissionPlanner.Controls;
using MissionPlanner.GCSViews;
using MissionPlanner.Plugin;
using MissionPlanner.Utilities;
using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using static Community.CsharpSqlite.Sqlite3;

namespace MagsLogger
{
    public class MagsLoggerPlugin : Plugin
    {
        public static MagsLoggerPlugin Instance { get; private set; }
        private readonly TabPage tab = new TabPage();
        private TabControl tabctrl;
        private readonly MagsLoggerTabView magsLoggerView = new MagsLoggerTabView();

        private readonly string pluginName = "Mags Logger";
        private readonly string pluginVersion = "2.2.8";
        private readonly string pluginAuthor = "Seaman";

        public override string Name { get { return pluginName; } }
        public override string Version { get { return pluginVersion; } }
        public override string Author { get { return pluginAuthor; } }

        public readonly Plane plane = new Plane();
        internal MagsOverlay magsOverlay = null;

        public static readonly MAVLink.MAV_CMD COMMAND_LONG_ID = MAVLink.MAV_CMD.USER_2;
        protected int ccr = 0;

        public static long INACTIVE_TIMEOUT = 3 * 1000;
        protected long lastActiveTime = 0;

        public bool isActive()
        {
            return lastActiveTime + INACTIVE_TIMEOUT > DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        public enum MagsCommandId
        {
            MAGS_MIN = 2001,
            MAGS_SENSOR1_STATUS = 2001,
            MAGS_SENSOR2_STATUS = 2002,
            MAGS_SENSOR3_STATUS = 2003,
            MAGS_SENSOR4_STATUS = 2004,
            MAGS_SENSORS_STATUS = 2011,
            MAGS_STATUS = 2012,
            MAGS_CCR_SET = 2013,
            MAGS_SETTINGS = 2014,
            MAGS_SETTINGS_REQUEST = 2015,
            MAGS_LOGGING_START = 2016,
            MAGS_LOGGING_STOP = 2017,
            MAGS_SET_OUT_MAGS = 2018,
            MAGS_SET_OUT_ACCEL = 2019,
            MAGS_MAGS_VALUES = 2020,
            MAGS_ACCEL_VALUES = 2021,
            MAGS_FULL_TRACE_ENABLE = 2022,
            MAGS_DEBUG_ENABLE = 2023,
            MAGS_IP = 2024,
            MAGS_MAX,
        };

        static readonly int STATUS_BIT_MAGS_ONLINE = 0x1;
        static readonly int STATUS_BIT_GPS_ONLINE = 0x2;
        static readonly int STATUS_BIT_LOG_STARTED = 0x4;
        static readonly int STATUS_BIT_OUT_MAGS = 0x8;
        static readonly int STATUS_BIT_OUT_ACCEL = 0x10;
        static readonly int STATUS_BIT_FULL_TRACE_ENEBLED = 0x20;
        static readonly int STATUS_BIT_DEBUG_ENABLED = 0x40;

        // CHANGE THIS TO TRUE TO USE THIS PLUGIN
        public override bool Init()
        {
            Instance = this;
            this.loopratehz = 1;

            return true;
        }

        private void addMenu(ToolStripMenuItem mainMenu, String label, EventHandler onClick)
        {
            ToolStripMenuItem menu = new ToolStripMenuItem(label);
            menu.Click += onClick;
            mainMenu.DropDownItems.Add(menu);
        }

        public override bool Loaded()
        {
            ToolStripMenuItem menu = new ToolStripMenuItem(pluginName);

            addMenu(menu, "Set CCR", changeCcrMenu_Click);
            addMenu(menu, "Enable/Disable", enableMenu_Click);
            addMenu(menu, "Start Logging", logStartMenu_Click);
            addMenu(menu, "Stop Logging", logStopMenu_Click);
            addMenu(menu, "Switch out to Mags", switchOutToMagsMenu_Click);
            addMenu(menu, "Switch out to Accel", switchOutToAccelMenu_Click);

            ToolStripMenuItem debugMenu = new ToolStripMenuItem("Debug");

            addMenu(debugMenu, "Full Trace Enable", fullTraceEnableMenu_Click);
            addMenu(debugMenu, "Full Trace Disable", fullTraceDisableMenu_Click);
            addMenu(debugMenu, "Debug Trace Enable", debugTraceEnableMenu_Click);
            addMenu(debugMenu, "Debug Trace Disable", debugTraceDisableMenu_Click);
            addMenu(debugMenu, "Get IP", debugGetIPMenu_Click);

            menu.DropDownItems.Add(debugMenu);

            Host.FDMenuMap.Items.Add(menu);
            SoftwareConfig.AddPluginViewPage(typeof(MagsLoggerConfigView), pluginName, SoftwareConfig.pageOptions.isConnected);

            Host.MainForm.FlightData.TabListOriginal.Add(tab);
            tabctrl = Host.MainForm.FlightData.tabControlactions;
            tab.Text = "Mags Logger";
            tab.Name = "tabMagsLogger";
            tab.Controls.Add(magsLoggerView);
            magsLoggerView.Dock = DockStyle.Fill;
            tabctrl.TabPages.Insert(5, tab);
            ThemeManager.ApplyThemeTo(tab);

            magsOverlay = new MagsOverlay();
            magsOverlay.plane = plane;
            magsOverlay.IsVisibile = Settings.Instance.GetBoolean("mags_logger_enabled", true);

            magsOverlay.zoom = Host.FDGMapControl.Zoom;

            Host.FDGMapControl.Overlays.Add(magsOverlay);
            Host.FDGMapControl.OnMapZoomChanged += FDGMapControl_OnMapZoomChanged;


            MainV2.comPort.OnPacketReceived -= plane.onMavlinkMessageReceived;
            MainV2.comPort.OnPacketReceived += plane.onMavlinkMessageReceived;
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
            sendCommand(MagsCommandId.MAGS_LOGGING_START);
        }

        void logStopMenu_Click(object sender, EventArgs e)
        {
            sendCommand(MagsCommandId.MAGS_LOGGING_STOP);
        }

        void switchOutToMagsMenu_Click(object sender, EventArgs e)
        {
            sendCommand(MagsCommandId.MAGS_SET_OUT_MAGS);
        }

        void switchOutToAccelMenu_Click(object sender, EventArgs e)
        {
            sendCommand(MagsCommandId.MAGS_SET_OUT_ACCEL);
        }

        void fullTraceEnableMenu_Click(object sender, EventArgs e)
        {
            sendCommand(MagsCommandId.MAGS_FULL_TRACE_ENABLE, 1);
        }

        void fullTraceDisableMenu_Click(object sender, EventArgs e)
        {
            sendCommand(MagsCommandId.MAGS_FULL_TRACE_ENABLE, 0);
        }

        void debugTraceEnableMenu_Click(object sender, EventArgs e)
        {
            sendCommand(MagsCommandId.MAGS_DEBUG_ENABLE, 1);
        }

        void debugTraceDisableMenu_Click(object sender, EventArgs e)
        {
            sendCommand(MagsCommandId.MAGS_DEBUG_ENABLE, 0);
        }

        void debugGetIPMenu_Click(object sender, EventArgs e)
        {
            sendCommand(MagsCommandId.MAGS_IP, 0);
        }

        void changeCcrMenu_Click(object sender, EventArgs e)
        {
            UpdateCcrForm form = new UpdateCcrForm();

            form.ccr = ccr;

            if (form.ShowDialog() == DialogResult.OK)
            {
                SetCcr(form.ccr, true);
            }
        }

        public int GetCcr()
        {
            return ccr;
        }

        public void SetCcr(int value, bool sendToCompanion)
        {
            ccr = Math.Max(0, value);
            if (magsOverlay != null)
            {
                magsOverlay.Ccr = ccr;
            }

            if (sendToCompanion && ccr > 0)
            {
                sendCommand(MagsCommandId.MAGS_CCR_SET, ccr);
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
                    magsOverlay.LogoutFps = ParseUtils.toInt(command_long.param2);
                    magsOverlay.LogoutTime = ParseUtils.toInt(command_long.param3);
                    magsOverlay.MagsFps = ParseUtils.toInt(command_long.param4);
                    magsOverlay.GpsFixed = command_long.param5 >= 0;
                    magsOverlay.GpsFps = command_long.param5 >= 0 ? ParseUtils.toInt(command_long.param5) : 0;
                    magsOverlay.AttitudeFps = ParseUtils.toInt(command_long.param6);

                    int status = ParseUtils.toInt(command_long.param7);
                    magsOverlay.LoggingStarted = (status & STATUS_BIT_LOG_STARTED) == STATUS_BIT_LOG_STARTED;
                    magsOverlay.OutMagsDetected = (status & STATUS_BIT_OUT_MAGS) == STATUS_BIT_OUT_MAGS;
                    magsOverlay.OutAccelDetected = (status & STATUS_BIT_OUT_ACCEL) == STATUS_BIT_OUT_ACCEL;
                    magsOverlay.OutFullTraceEnabled = (status & STATUS_BIT_FULL_TRACE_ENEBLED) == STATUS_BIT_FULL_TRACE_ENEBLED;
                    magsOverlay.OutDebugTraceEnabled = (status & STATUS_BIT_DEBUG_ENABLED) == STATUS_BIT_DEBUG_ENABLED;

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
                            magsOverlay.setMagOnlineStatus(magIdx, command_long.param3 > 0);

                        if (++magIdx < magsOverlay.getMagsCount() && command_long.param4 >= 0)
                            magsOverlay.setMagOnlineStatus(magIdx, command_long.param4 > 0);

                        if (++magIdx < magsOverlay.getMagsCount() && command_long.param5 >= 0)
                            magsOverlay.setMagOnlineStatus(magIdx, command_long.param5 > 0);

                        if (++magIdx < magsOverlay.getMagsCount() && command_long.param6 >= 0)
                            magsOverlay.setMagOnlineStatus(magIdx, command_long.param6 > 0);
                    }

                    break;
                }
                case MagsCommandId.MAGS_SETTINGS:
                {
                    int magsCount = ParseUtils.toInt(command_long.param2);
                    magsOverlay.setMagsCount(magsCount);

                    SetCcr(ParseUtils.toInt(command_long.param3), false);

                    break;
                }
                case MagsCommandId.MAGS_MAGS_VALUES:
                {
                    int mx = ParseUtils.toInt(command_long.param2);
                    int my = ParseUtils.toInt(command_long.param3);
                    int mz = ParseUtils.toInt(command_long.param4);
                    magsOverlay.setMagValues(0, mx, my, mz);

                    mx = ParseUtils.toInt(command_long.param5);
                    my = ParseUtils.toInt(command_long.param6);
                    mz = ParseUtils.toInt(command_long.param7);
                    magsOverlay.setMagValues(1, mx, my, mz);

                    break;
                }
                case MagsCommandId.MAGS_ACCEL_VALUES:
                {
                    int x = ParseUtils.toInt(command_long.param2);
                    int y = ParseUtils.toInt(command_long.param3);
                    int z = ParseUtils.toInt(command_long.param4);
                    magsOverlay.setAccelValues(x, y, z);

                    x = ParseUtils.toInt(command_long.param5);
                    y = ParseUtils.toInt(command_long.param6);
                    z = ParseUtils.toInt(command_long.param7);
                    magsOverlay.setAccelMagValues(x, y, z);

                    break;
                }
                case MagsCommandId.MAGS_IP:
                {
                    int ip = ParseUtils.toInt(command_long.param2);
                    magsOverlay.Ip[0] = (int)(ip & 0xFF);
                    magsOverlay.Ip[1] = (int)((ip >> 8) & 0xFF);
                    magsOverlay.Ip[2] = (int)((ip >> 16) & 0xFF);
                    magsOverlay.Ip[3] = (int)((ip >> 24) & 0xFF);

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

        public void gpsOn()
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
                    (byte)MAVLink.MAV_COMPONENT.MAV_COMP_ID_AUTOPILOT1,
                    MAVLink.MAV_CMD.SET_EKF_SOURCE_SET,
                    1,
                    0,
                    0, 0, 0, 0, 0, true);
            }
            catch
            {
                CustomMessageBox.Show(Strings.CommandFailed, Strings.ERROR);
            }
        }

        public void gpsOff()
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
                    (byte)MAVLink.MAV_COMPONENT.MAV_COMP_ID_AUTOPILOT1,
                    MAVLink.MAV_CMD.SET_EKF_SOURCE_SET,
                    2,
                    0,
                    0, 0, 0, 0, 0, true);
            }
            catch
            {
                CustomMessageBox.Show(Strings.CommandFailed, Strings.ERROR);
            }
        }

        public void gpsOnAux()
        {
            if (MainV2.comPort.BaseStream == null || !Host.comPort.BaseStream.IsOpen)
            {
                CustomMessageBox.Show(Strings.CommunicationErrorNoConnection, Strings.ERROR);
                return;
            }

            try
            {
                MainV2.comPort.doCommand((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, MAVLink.MAV_CMD.DO_AUX_FUNCTION, 65, 0, 0, 0, 0, 0, 0);
            }
            catch
            {
                CustomMessageBox.Show(Strings.CommandFailed, Strings.ERROR);
            }
        }

        public void gpsOffAux()
        {
            if (MainV2.comPort.BaseStream == null || !Host.comPort.BaseStream.IsOpen)
            {
                CustomMessageBox.Show(Strings.CommunicationErrorNoConnection, Strings.ERROR);
                return;
            }

            try
            {
                MainV2.comPort.doCommand((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, MAVLink.MAV_CMD.DO_AUX_FUNCTION, 65, 2, 0, 0, 0, 0, 0);
            }
            catch
            {
                CustomMessageBox.Show(Strings.CommandFailed, Strings.ERROR);
            }
        }

        public async void sendCommandAsync(MagsCommandId commandId, float value1 = 0, float value2 = 0, float value3 = 0)
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
