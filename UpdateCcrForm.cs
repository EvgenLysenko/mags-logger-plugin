using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MissionPlanner
{
    public partial class UpdateCcrForm : Form
    {
        public int ccr = 0;

        public UpdateCcrForm()
        {
            InitializeComponent();
        }

        private void InputPositionForm_Shown(object sender, EventArgs e)
        {
            UpdateDataControls();
        }

        private void UpdateDataControls()
        {
            ccrTextBox.Text = ccr.ToString();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;

            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;

            Close();
        }

        private void InputPositionForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.OK)
            {
                ccr = MagsLogger.ParseUtils.parseInt(ccrTextBox.Text, 0);
            }
        }
    }
}
