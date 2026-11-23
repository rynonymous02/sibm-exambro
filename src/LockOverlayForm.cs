using System;
using System.Drawing;
using System.Windows.Forms;

namespace SibmExambro
{
    public class LockOverlayForm : Form
    {
        private string _baseUrl;
        private string _token;
        private string _namaPeserta;
        private string _deviceName;

        private Panel _card;
        private TextBox _txtPin;
        private Label _lblStatus;
        private Button _btnVerify;

        public LockOverlayForm(string baseUrl, string token, string namaPeserta, string deviceName, string reason)
        {
            _baseUrl = baseUrl;
            _token = token;
            _namaPeserta = namaPeserta;
            _deviceName = deviceName;

            try
            {
                this.Icon = MainForm.Instance != null ? MainForm.Instance.Icon : Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }

            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Normal;
            this.Bounds = Screen.PrimaryScreen.Bounds;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.BackColor = Color.FromArgb(15, 23, 42); // Dark slate
            this.ForeColor = Color.White;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(0, 0);
            this.Size = Screen.PrimaryScreen.Bounds.Size;

            InitializeUi();

            this.Resize += (s, e) => CenterCard();
            this.Shown += (s, e) => {
                CenterCard();
                _txtPin.Focus();
            };
        }

        private void CenterCard()
        {
            if (_card != null)
            {
                int x = Math.Max(10, (this.ClientSize.Width - _card.Width) / 2);
                int y = Math.Max(10, (this.ClientSize.Height - _card.Height) / 2);
                _card.Location = new Point(x, y);
            }
        }

        private void InitializeUi()
        {
            _card = new Panel();
            _card.Size = new Size(460, 310);
            _card.BackColor = Color.FromArgb(30, 41, 59);
            _card.Padding = new Padding(24);
            this.Controls.Add(_card);

            // Title
            Label title = new Label();
            title.Text = "Layar Terkunci";
            title.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(239, 68, 68); // Red
            title.TextAlign = ContentAlignment.MiddleCenter;
            title.Dock = DockStyle.Top;
            title.Height = 45;
            _card.Controls.Add(title);

            // Instruction
            Label inst = new Label();
            inst.Text = "Masukkan PIN Pengawas (6 Digit):";
            inst.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            inst.ForeColor = Color.FromArgb(203, 213, 225);
            inst.TextAlign = ContentAlignment.MiddleCenter;
            inst.Location = new Point(30, 70);
            inst.Size = new Size(400, 25);
            _card.Controls.Add(inst);

            // PIN Textbox
            _txtPin = new TextBox();
            _txtPin.Font = new Font("Consolas", 22, FontStyle.Bold);
            _txtPin.TextAlign = HorizontalAlignment.Center;
            _txtPin.MaxLength = 6;
            _txtPin.Location = new Point(100, 105);
            _txtPin.Size = new Size(260, 45);
            _txtPin.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    VerifyPin();
                    e.Handled = true;
                }
            };
            _card.Controls.Add(_txtPin);

            // Status label
            _lblStatus = new Label();
            _lblStatus.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            _lblStatus.ForeColor = Color.FromArgb(248, 113, 113);
            _lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            _lblStatus.Location = new Point(30, 160);
            _lblStatus.Size = new Size(400, 25);
            _lblStatus.Text = "";
            _card.Controls.Add(_lblStatus);

            // Button Verify
            _btnVerify = new Button();
            _btnVerify.Text = "Buka Kunci";
            _btnVerify.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            _btnVerify.BackColor = Color.FromArgb(37, 99, 235);
            _btnVerify.ForeColor = Color.White;
            _btnVerify.FlatStyle = FlatStyle.Flat;
            _btnVerify.FlatAppearance.BorderSize = 0;
            _btnVerify.Location = new Point(100, 200);
            _btnVerify.Size = new Size(260, 45);
            _btnVerify.Cursor = Cursors.Hand;
            _btnVerify.Click += (s, e) => VerifyPin();
            _card.Controls.Add(_btnVerify);

            CenterCard();
        }

        private void VerifyPin()
        {
            string pin = _txtPin.Text.Trim();
            if (pin.Length != 6)
            {
                _lblStatus.Text = "Masukkan 6 digit angka PIN Pengawas.";
                _txtPin.Focus();
                return;
            }

            _btnVerify.Enabled = false;
            _btnVerify.Text = "Memeriksa...";
            Application.DoEvents();

            string errMsg;
            bool ok = ApiClient.VerifySupervisorPin(_baseUrl, _token, pin, _namaPeserta, _deviceName, out errMsg);
            if (ok)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                _btnVerify.Enabled = true;
                _btnVerify.Text = "Buka Kunci";
                _lblStatus.Text = string.IsNullOrEmpty(errMsg) ? "PIN salah atau sudah tidak aktif." : errMsg;
                _txtPin.SelectAll();
                _txtPin.Focus();
            }
        }
    }
}
