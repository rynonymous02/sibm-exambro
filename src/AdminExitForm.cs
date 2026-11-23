using System;
using System.Drawing;
using System.Windows.Forms;

namespace SibmExambro
{
    public class AdminExitForm : Form
    {
        private const string StaticAdminPin = "170845";

        private TextBox _txtPin;
        private Label _lblStatus;
        private Button _btnExit;

        public AdminExitForm()
        {
            try
            {
                this.Icon = MainForm.Instance != null ? MainForm.Instance.Icon : Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(420, 260);
            this.BackColor = Color.FromArgb(15, 23, 42); // Dark slate
            this.ForeColor = Color.White;
            this.TopMost = true;
            this.ShowInTaskbar = false;

            InitializeUi();
        }

        private void InitializeUi()
        {
            Panel card = new Panel();
            card.Dock = DockStyle.Fill;
            card.BackColor = Color.FromArgb(30, 41, 59);
            card.Padding = new Padding(24);
            this.Controls.Add(card);

            // Title
            Label title = new Label();
            title.Text = "Keluar Admin";
            title.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            title.ForeColor = Color.White;
            title.TextAlign = ContentAlignment.MiddleCenter;
            title.Dock = DockStyle.Top;
            title.Height = 35;
            card.Controls.Add(title);

            // Instruction
            Label inst = new Label();
            inst.Text = "Masukkan PIN Admin:";
            inst.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            inst.ForeColor = Color.FromArgb(203, 213, 225);
            inst.Location = new Point(40, 55);
            inst.Size = new Size(340, 22);
            card.Controls.Add(inst);

            // Textbox PIN
            _txtPin = new TextBox();
            _txtPin.Font = new Font("Consolas", 18, FontStyle.Bold);
            _txtPin.TextAlign = HorizontalAlignment.Center;
            _txtPin.PasswordChar = '•';
            _txtPin.MaxLength = 10;
            _txtPin.Location = new Point(40, 80);
            _txtPin.Size = new Size(340, 36);
            _txtPin.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    VerifyPin();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                }
            };
            card.Controls.Add(_txtPin);

            // Status label
            _lblStatus = new Label();
            _lblStatus.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            _lblStatus.ForeColor = Color.FromArgb(248, 113, 113);
            _lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            _lblStatus.Location = new Point(40, 125);
            _lblStatus.Size = new Size(340, 22);
            _lblStatus.Text = "";
            card.Controls.Add(_lblStatus);

            // Buttons
            Panel btnPanel = new Panel();
            btnPanel.Location = new Point(40, 160);
            btnPanel.Size = new Size(340, 42);

            Button btnCancel = new Button();
            btnCancel.Text = "Batal";
            btnCancel.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            btnCancel.BackColor = Color.FromArgb(71, 85, 105);
            btnCancel.ForeColor = Color.White;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Size = new Size(150, 40);
            btnCancel.Location = new Point(0, 0);
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.Click += (s, e) => {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };
            btnPanel.Controls.Add(btnCancel);

            _btnExit = new Button();
            _btnExit.Text = "Keluar";
            _btnExit.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            _btnExit.BackColor = Color.FromArgb(239, 68, 68); // Red
            _btnExit.ForeColor = Color.White;
            _btnExit.FlatStyle = FlatStyle.Flat;
            _btnExit.FlatAppearance.BorderSize = 0;
            _btnExit.Size = new Size(170, 40);
            _btnExit.Location = new Point(170, 0);
            _btnExit.Cursor = Cursors.Hand;
            _btnExit.Click += (s, e) => VerifyPin();
            btnPanel.Controls.Add(_btnExit);

            card.Controls.Add(btnPanel);

            this.Shown += (s, e) => {
                _txtPin.Focus();
            };
        }

        private void VerifyPin()
        {
            string pin = _txtPin.Text.Trim();
            if (string.Equals(pin, StaticAdminPin, StringComparison.Ordinal))
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                _lblStatus.Text = "PIN Admin salah.";
                _txtPin.SelectAll();
                _txtPin.Focus();
            }
        }
    }
}
