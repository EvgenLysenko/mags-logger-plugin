using MagsLogger;
using MissionPlanner.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MagsLogger
{
    public class MagsLoggerConfigView : MyUserControl, IActivate
    {
        private readonly NumericUpDown ccrNumericUpDown = new NumericUpDown();
        private readonly Label statusLabel = new Label();

        public MagsLoggerConfigView()
        {
            BuildUi();
            RefreshFromPlugin();
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

            statusLabel.AutoSize = true;
            statusLabel.Text = "Configure MagsLogger parameters.";

            layout.Controls.Add(ccrLabel, 0, 0);
            layout.Controls.Add(ccrNumericUpDown, 1, 0);
            layout.Controls.Add(applyButton, 2, 0);
            layout.Controls.Add(editButton, 2, 1);
            layout.Controls.Add(statusLabel, 0, 2);
            layout.SetColumnSpan(statusLabel, 3);

            Controls.Add(layout);
        }

        private void RefreshFromPlugin()
        {
            var plugin = MagsLoggerPlugin.Instance;
            if (plugin == null)
            {
                statusLabel.Text = "MagsLogger plugin is not loaded.";
                return;
            }

            ccrNumericUpDown.Value = Clamp(plugin.GetCcr());
            statusLabel.Text = "Current values loaded from plugin.";
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
