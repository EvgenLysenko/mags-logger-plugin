using MissionPlanner.Controls;
using System;
using System.Windows.Forms;

namespace MagsLogger
{
    public class MagsLoggerTabView : MyUserControl, IActivate
    {
        private readonly NumericUpDown ccrNumericUpDown = new NumericUpDown();
        private readonly Label statusLabel = new Label();
        private readonly Label valuesLabel = new Label();
        private readonly Timer refreshTimer = new Timer();

        public MagsLoggerTabView()
        {
            BuildUi();
            RefreshFromPlugin();
            refreshTimer.Interval = 1000;
            refreshTimer.Tick += (s, e) => RefreshFromPlugin();
            refreshTimer.Start();
        }

        public void Activate()
        {
            RefreshFromPlugin();
        }

        private void BuildUi()
        {
            Dock = DockStyle.Fill;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(10),
                ColumnCount = 3
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var ccrLabel = new Label
            {
                Text = "CCR",
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };

            ccrNumericUpDown.Minimum = 0;
            ccrNumericUpDown.Maximum = 9999;
            ccrNumericUpDown.Anchor = AnchorStyles.Left;

            var applyButton = new MyButton
            {
                Text = "Apply",
                AutoSize = true
            };
            applyButton.Click += ApplyButton_Click;

            var editButton = new MyButton
            {
                Text = "Edit...",
                AutoSize = true
            };
            editButton.Click += EditButton_Click;

            var requestIpButton = new MyButton
            {
                Text = "Request IP",
                AutoSize = true
            };
            requestIpButton.Click += RequestIpButton_Click;

            statusLabel.AutoSize = true;
            statusLabel.Text = "Configure MagsLogger parameters.";
            valuesLabel.AutoSize = true;
            valuesLabel.Text = "Waiting for MagsOverlay data...";

            var logStartButton = new MyButton
            {
                Text = "Log Start",
                AutoSize = true
            };
            logStartButton.Click += logStartButton_Click;

            var logStopButton = new MyButton
            {
                Text = "Log Stop",
                AutoSize = true
            };
            logStopButton.Click += logStopButton_Click;


            layout.Controls.Add(ccrLabel, 0, 0);
            layout.Controls.Add(ccrNumericUpDown, 1, 0);
            layout.Controls.Add(applyButton, 2, 0);
            layout.Controls.Add(editButton, 2, 1);
            layout.Controls.Add(requestIpButton, 2, 2);
            layout.Controls.Add(logStartButton, 1, 2);
            layout.Controls.Add(logStopButton, 0, 2);
            layout.Controls.Add(statusLabel, 0, 5);
            layout.Controls.Add(valuesLabel, 0, 6);
            layout.SetColumnSpan(statusLabel, 3);
            layout.SetColumnSpan(valuesLabel, 3);

            var gpsOnButton = new MyButton
            {
                Text = "GPS On",
                AutoSize = true
            };
            gpsOnButton.Click += GpsOnButton_Click;

            var gpsOffButton = new MyButton
            {
                Text = "GPS Off",
                AutoSize = true
            };
            gpsOffButton.Click += GpsOffButton_Click;

            layout.Controls.Add(gpsOnButton, 3, 7);
            layout.Controls.Add(gpsOffButton, 1, 7);

            var gpsTestButtonsLabel = new Label
            {
                Text = "GPS direct commands:",
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };

            layout.Controls.Add(gpsTestButtonsLabel, 1, 8);

            var gps2OnButton = new MyButton { Text = "GPS src 1", AutoSize = true };
            gps2OnButton.Click += gpsOnApplyButton_Click;

            var gps2OffButton = new MyButton { Text = "GPS src 2", AutoSize = true };
            gps2OffButton.Click += gpsOffApplyButton_Click;

            layout.Controls.Add(gps2OnButton, 3, 9);
            layout.Controls.Add(gps2OffButton, 1, 9);


            var gpsOnAuxButton = new MyButton { Text = "GPS AUX on", AutoSize = true };
            gpsOnAuxButton.Click += gpsOnAuxApplyButton_Click;

            var gpsOffAuxButton = new MyButton { Text = "GPS AUX off", AutoSize = true };
            gpsOffAuxButton.Click += gpsOffAuxApplyButton_Click;

            layout.Controls.Add(gpsOnAuxButton, 3, 10);
            layout.Controls.Add(gpsOffAuxButton, 1, 10);

            Controls.Add(layout);
        }

        private void RefreshFromPlugin()
        {
            var plugin = MagsLoggerPlugin.Instance;
            if (plugin == null)
            {
                statusLabel.Text = "MagsLogger plugin is not loaded.";
                valuesLabel.Text = "No plugin instance.";
                return;
            }

            ccrNumericUpDown.Value = Clamp(plugin.GetCcr());
            statusLabel.Text = "Current values loaded from plugin.";

            var overlay = plugin.magsOverlay;
            if (overlay == null)
            {
                valuesLabel.Text = "Overlay not initialized yet.";
                return;
            }

            valuesLabel.Text =
                $"Active: {plugin.isActive()}\r\n" +
                $"CCR: {overlay.Ccr}\r\n" +
                $"Mags FPS: {overlay.MagsFps}\r\n" +
                $"Attitude FPS: {overlay.AttitudeFps}\r\n" +
                $"GPS FPS: {overlay.GpsFps} (Fix: {overlay.GpsFixed})\r\n" +
                $"Log FPS: {overlay.LogoutFps}\r\n" +
                $"Log Time: {overlay.secondsToString(overlay.LogoutTime)}\r\n" +
                $"Logging Started: {overlay.LoggingStarted}\r\n" +
                $"Out Mags: {overlay.OutMagsDetected}\r\n" +
                $"Out Accel: {overlay.OutAccelDetected}\r\n" +
                $"Full Trace: {overlay.OutFullTraceEnabled}\r\n" +
                $"Debug Trace: {overlay.OutDebugTraceEnabled}\r\n" +
                $"IP: {overlay.Ip[0]}.{overlay.Ip[1]}.{overlay.Ip[2]}.{overlay.Ip[3]}";
        }

        private void ApplyButton_Click(object sender, EventArgs e)
        {
            var plugin = MagsLoggerPlugin.Instance;
            if (plugin == null)
            {
                statusLabel.Text = "MagsLogger plugin is not loaded.";
                return;
            }

            plugin.SetCcr((int)ccrNumericUpDown.Value, true);
            statusLabel.Text = "CCR applied.";
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            using (var form = new UpdateCcrForm())
            {
                form.ccr = (int)ccrNumericUpDown.Value;
                if (form.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                ccrNumericUpDown.Value = Clamp(form.ccr);
            }
        }

        private void RequestIpButton_Click(object sender, EventArgs e)
        {
            var plugin = MagsLoggerPlugin.Instance;
            if (plugin == null)
            {
                statusLabel.Text = "MagsLogger plugin is not loaded.";
                return;
            }

            plugin.sendCommand(MagsCommandId.MAGS_IP);
            statusLabel.Text = "IP request sent.";
        }

        private void logStartButton_Click(object sender, EventArgs e)
        {
            var plugin = MagsLoggerPlugin.Instance;
            if (plugin == null)
            {
                statusLabel.Text = "MagsLogger plugin is not loaded.";
                return;
            }

            plugin.sendCommand(MagsCommandId.MAGS_LOGGING_START);
            statusLabel.Text = "Log start command sent.";
        }

        private void logStopButton_Click(object sender, EventArgs e)
        {
            var plugin = MagsLoggerPlugin.Instance;
            if (plugin == null)
            {
                statusLabel.Text = "MagsLogger plugin is not loaded.";
                return;
            }

            plugin.sendCommand(MagsCommandId.MAGS_LOGGING_STOP);
            statusLabel.Text = "Log stop command sent.";
        }

        private void GpsOnButton_Click(object sender, EventArgs e)
        {
            var plugin = MagsLoggerPlugin.Instance;
            if (plugin == null)
            {
                statusLabel.Text = "MagsLogger plugin is not loaded.";
                return;
            }

            plugin.sendCommand(MagsCommandId.MAGS_GPS_ON);
            statusLabel.Text = "GPS on sent.";
        }

        private void GpsOffButton_Click(object sender, EventArgs e)
        {
            var plugin = MagsLoggerPlugin.Instance;
            if (plugin == null)
            {
                statusLabel.Text = "MagsLogger plugin is not loaded.";
                return;
            }

            plugin.sendCommand(MagsCommandId.MAGS_GPS_OFF);
            statusLabel.Text = "GPS off sent.";
        }

        private void gpsOnApplyButton_Click(object sender, EventArgs e)
        {
            var plugin = MagsLoggerPlugin.Instance;
            if (plugin == null)
            {
                statusLabel.Text = "MagsLogger plugin is not loaded.";
                return;
            }

            plugin.gpsOn();
            statusLabel.Text = "IP request sent.";
        }

        private void gpsOffApplyButton_Click(object sender, EventArgs e)
        {
            var plugin = MagsLoggerPlugin.Instance;
            if (plugin == null)
            {
                statusLabel.Text = "MagsLogger plugin is not loaded.";
                return;
            }

            plugin.gpsOff();
            statusLabel.Text = "IP request sent.";
        }

        private void gpsOnAuxApplyButton_Click(object sender, EventArgs e)
        {
            var plugin = MagsLoggerPlugin.Instance;
            if (plugin == null)
            {
                statusLabel.Text = "MagsLogger plugin is not loaded.";
                return;
            }

            plugin.gpsOnAux();
            statusLabel.Text = "IP request sent.";
        }

        private void gpsOffAuxApplyButton_Click(object sender, EventArgs e)
        {
            var plugin = MagsLoggerPlugin.Instance;
            if (plugin == null)
            {
                statusLabel.Text = "MagsLogger plugin is not loaded.";
                return;
            }

            plugin.gpsOffAux();
            statusLabel.Text = "IP request sent.";
        }

        private decimal Clamp(int value)
        {
            if (value < ccrNumericUpDown.Minimum)
            {
                return ccrNumericUpDown.Minimum;
            }

            if (value > ccrNumericUpDown.Maximum)
            {
                return ccrNumericUpDown.Maximum;
            }

            return value;
        }
    }
}
