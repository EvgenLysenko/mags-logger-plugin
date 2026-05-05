using System;
using System.Windows.Forms;

namespace MagsLogger
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
            ccrNumericUpDown.Value = ccr;
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
                ccr = (int)ccrNumericUpDown.Value;
            }
        }
    }
}
