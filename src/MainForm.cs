using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace SibmExambro
{
    public class MainForm : Form
    {
        public static MainForm Instance { get; private set; }

        private KeyboardHook _keyboardHook;
        private bool _isExamRunning = false;
        private bool _isLockOverlayShowing = false;
        private bool _isExiting = false;
        private bool _isKioskActive = false;

        private string _baseUrl = "http://localhost";
        private string _sibmUrl = "http://127.0.0.1:8000";
        private string _psbmUrl = "";
        private string _token = "";
        private string _examUrl = "";
        private string _namaPeserta = "";
        private string _deviceName = "";

        // UI Controls - Lobby
        private Panel _lobbyPanel;
        private TextBox _txtKodeSesi;
        private TextBox _txtNamaPeserta;
        private Button _btnStart;
        private Button _btnEnterKiosk;
        private Label _lblLobbyStatus;
        private Action _centerLobbyCard;

        // UI Controls - Exam
        private WebView2 _webView;

        public MainForm()
        {
            Instance = this;

            try
            {
                this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }

            this.Text = "SIBM Exambrowser Desktop";
            this.Size = new Size(850, 640);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(241, 245, 249);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            _deviceName = string.Format("Desktop PC ({0})", Environment.MachineName);
            _namaPeserta = Environment.UserName;

            LoadSettings();

            _keyboardHook = new KeyboardHook();
            _keyboardHook.OnAdminExitTriggered = HandleAdminExit;
            _keyboardHook.OnSupervisorEmergencyExitTriggered = HandleSupervisorEmergencyExit;

            InitializeLobby();

            this.FormClosing += MainForm_FormClosing;
            this.Deactivate += MainForm_Deactivate;
        }

        private void LoadSettings()
        {
            _sibmUrl = "http://127.0.0.1:8000";
            _psbmUrl = "";
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
                if (!File.Exists(configPath))
                {
                    File.WriteAllText(configPath,
                        "[Settings]\r\n" +
                        "; Server SIBM (Ujian Santri / Siswa)\r\n" +
                        "server_sibm=http://127.0.0.1:8000\r\n" +
                        "; Server PSBM (Ujian Seleksi Masuk Santri Baru)\r\n" +
                        "server_psbm=http://127.0.0.1:8000\r\n" +
                        "nama_peserta=\r\n");
                    _sibmUrl = "http://127.0.0.1:8000";
                    _psbmUrl = "http://127.0.0.1:8000";
                }
                else
                {
                    string[] lines = File.ReadAllLines(configPath);
                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith(";") || trimmed.StartsWith("#")) continue;

                        if (trimmed.StartsWith("server_sibm=", StringComparison.OrdinalIgnoreCase))
                        {
                            _sibmUrl = trimmed.Substring("server_sibm=".Length).Trim();
                        }
                        else if (trimmed.StartsWith("server_psbm=", StringComparison.OrdinalIgnoreCase) ||
                                 trimmed.StartsWith("psbm_server=", StringComparison.OrdinalIgnoreCase))
                        {
                            _psbmUrl = trimmed.Substring(trimmed.IndexOf('=') + 1).Trim();
                        }
                        else if (trimmed.StartsWith("server=", StringComparison.OrdinalIgnoreCase))
                        {
                            _sibmUrl = trimmed.Substring("server=".Length).Trim();
                        }
                        else if (trimmed.StartsWith("server_url=", StringComparison.OrdinalIgnoreCase))
                        {
                            _sibmUrl = trimmed.Substring("server_url=".Length).Trim();
                        }
                        else if (trimmed.StartsWith("nama_peserta=", StringComparison.OrdinalIgnoreCase))
                        {
                            string savedName = trimmed.Substring("nama_peserta=".Length).Trim();
                            if (!string.IsNullOrEmpty(savedName)) _namaPeserta = savedName;
                        }
                    }
                }
            }
            catch (Exception) { _sibmUrl = "http://127.0.0.1:8000"; }

            _baseUrl = _sibmUrl;
        }

        private void SaveSettings()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("[Settings]");
                sb.AppendLine("; Server SIBM (Ujian Santri / Siswa)");
                sb.AppendLine("server_sibm=" + _sibmUrl);
                sb.AppendLine("; Server PSBM (Ujian Seleksi Masuk Santri Baru)");
                sb.AppendLine("server_psbm=" + _psbmUrl);
                sb.AppendLine("nama_peserta=" + _namaPeserta);
                File.WriteAllText(configPath, sb.ToString());
            }
            catch (Exception) { }
        }

        private void InitializeLobby()
        {
            _lobbyPanel = new Panel();
            _lobbyPanel.Dock = DockStyle.Fill;
            _lobbyPanel.BackColor = Color.FromArgb(15, 23, 42); // Dark slate
            this.Controls.Add(_lobbyPanel);

            Panel card = new Panel();
            card.Size = new Size(500, 330);
            card.BackColor = Color.FromArgb(30, 41, 59);
            card.Padding = new Padding(24);
            _lobbyPanel.Controls.Add(card);

            Action centerLobbyCard = () => {
                card.Location = new Point(
                    Math.Max(10, (_lobbyPanel.ClientSize.Width - card.Width) / 2),
                    Math.Max(10, (_lobbyPanel.ClientSize.Height - card.Height) / 2)
                );
            };
            _centerLobbyCard = centerLobbyCard;
            _lobbyPanel.Resize += (s, e) => centerLobbyCard();

            // Logo & Title
            string iconPngPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon-app.png");
            int startY = 60;
            if (File.Exists(iconPngPath))
            {
                try
                {
                    card.Size = new Size(500, 420);
                    PictureBox pbLogo = new PictureBox();
                    pbLogo.Image = Image.FromFile(iconPngPath);
                    pbLogo.SizeMode = PictureBoxSizeMode.Zoom;
                    pbLogo.Size = new Size(56, 56);
                    pbLogo.Location = new Point((card.Width - 56) / 2, 16);
                    card.Controls.Add(pbLogo);

                    startY = 125;
                }
                catch { }
            }
            else
            {
                card.Size = new Size(500, 355);
            }

            Label title = new Label();
            title.Text = "SIBM EXAMBROWSER";
            title.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(56, 189, 248); // Sky blue
            title.TextAlign = ContentAlignment.MiddleCenter;
            title.Location = new Point(0, startY - 42);
            title.Size = new Size(card.Width, 36);
            card.Controls.Add(title);

            // 1. Kode Sesi
            Label lblKode = new Label();
            lblKode.Text = "Kode Sesi (6 Karakter):";
            lblKode.ForeColor = Color.FromArgb(251, 191, 36); // Amber
            lblKode.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblKode.Location = new Point(35, startY);
            lblKode.Size = new Size(430, 20);
            card.Controls.Add(lblKode);

            _txtKodeSesi = new TextBox();
            _txtKodeSesi.Font = new Font("Consolas", 18, FontStyle.Bold);
            _txtKodeSesi.TextAlign = HorizontalAlignment.Center;
            _txtKodeSesi.CharacterCasing = CharacterCasing.Upper;
            _txtKodeSesi.MaxLength = 64;
            _txtKodeSesi.Location = new Point(35, startY + 22);
            _txtKodeSesi.Size = new Size(430, 36);
            _txtKodeSesi.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    StartExam();
                    e.Handled = true;
                }
            };
            card.Controls.Add(_txtKodeSesi);

            // 2. Nama Peserta
            int y2 = startY + 70;
            Label lblNama = new Label();
            lblNama.Text = "Nama Peserta:";
            lblNama.ForeColor = Color.White;
            lblNama.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblNama.Location = new Point(35, y2);
            lblNama.Size = new Size(430, 20);
            card.Controls.Add(lblNama);

            _txtNamaPeserta = new TextBox();
            _txtNamaPeserta.Text = _namaPeserta;
            _txtNamaPeserta.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            _txtNamaPeserta.Location = new Point(35, y2 + 22);
            _txtNamaPeserta.Size = new Size(430, 28);
            _txtNamaPeserta.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    StartExam();
                    e.Handled = true;
                }
            };
            card.Controls.Add(_txtNamaPeserta);

            // Status label
            int y3 = y2 + 56;
            _lblLobbyStatus = new Label();
            _lblLobbyStatus.Text = "";
            _lblLobbyStatus.ForeColor = Color.FromArgb(248, 113, 113);
            _lblLobbyStatus.TextAlign = ContentAlignment.MiddleCenter;
            _lblLobbyStatus.Location = new Point(35, y3);
            _lblLobbyStatus.Size = new Size(430, 20);
            card.Controls.Add(_lblLobbyStatus);

            // Tombol Mulai Ujian (Wajib Kiosk saat ujian)
            _btnStart = new Button();
            _btnStart.Text = "MULAI UJIAN";
            _btnStart.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            _btnStart.BackColor = Color.FromArgb(16, 185, 129); // Emerald green
            _btnStart.ForeColor = Color.White;
            _btnStart.FlatStyle = FlatStyle.Flat;
            _btnStart.FlatAppearance.BorderSize = 0;
            _btnStart.Location = new Point(35, y3 + 24);
            _btnStart.Size = new Size(430, 42);
            _btnStart.Cursor = Cursors.Hand;
            _btnStart.Click += (s, e) => StartExam();
            card.Controls.Add(_btnStart);

            // Tombol Langsung Masuk Mode Kiosk (dapat ditekan langsung di halaman awal ini)
            _btnEnterKiosk = new Button();
            _btnEnterKiosk.Text = "MASUK KIOSK MODE";
            _btnEnterKiosk.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            _btnEnterKiosk.BackColor = Color.FromArgb(30, 41, 59);
            _btnEnterKiosk.ForeColor = Color.FromArgb(56, 189, 248);
            _btnEnterKiosk.FlatStyle = FlatStyle.Flat;
            _btnEnterKiosk.FlatAppearance.BorderColor = Color.FromArgb(56, 189, 248);
            _btnEnterKiosk.FlatAppearance.BorderSize = 1;
            _btnEnterKiosk.Location = new Point(35, y3 + 74);
            _btnEnterKiosk.Size = new Size(430, 36);
            _btnEnterKiosk.Cursor = Cursors.Hand;
            _btnEnterKiosk.Click += (s, e) => EnterKioskMode();
            card.Controls.Add(_btnEnterKiosk);

            this.Shown += (s, e) => {
                centerLobbyCard();
                _txtKodeSesi.Focus();
            };
        }

        private void EnterKioskMode()
        {
            if (_isKioskActive) return;
            _isKioskActive = true;

            try
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Normal;
                this.Bounds = Screen.PrimaryScreen.Bounds;
                this.TopMost = true;

                if (_keyboardHook != null)
                {
                    _keyboardHook.Install();
                }

                SystemPolicy.ApplyLockdown();

                if (_btnEnterKiosk != null)
                {
                    _btnEnterKiosk.Text = "KIOSK MODE AKTIF";
                    _btnEnterKiosk.Enabled = false;
                    _btnEnterKiosk.BackColor = Color.FromArgb(51, 65, 85);
                    _btnEnterKiosk.ForeColor = Color.FromArgb(148, 163, 184);
                    _btnEnterKiosk.FlatAppearance.BorderColor = Color.FromArgb(71, 85, 105);
                }

                if (_centerLobbyCard != null)
                {
                    _centerLobbyCard();
                }
            }
            catch (Exception) { }
        }

        private void StartExam()
        {
            string rawCode = _txtKodeSesi.Text.Trim();
            string nama = _txtNamaPeserta.Text.Trim();

            if (string.IsNullOrEmpty(rawCode))
            {
                _lblLobbyStatus.ForeColor = Color.FromArgb(248, 113, 113);
                _lblLobbyStatus.Text = "Masukkan 6 digit kode sesi ujian atau tautan ujian.";
                _txtKodeSesi.Focus();
                return;
            }

            if (string.IsNullOrEmpty(nama))
            {
                _lblLobbyStatus.ForeColor = Color.FromArgb(248, 113, 113);
                _lblLobbyStatus.Text = "Nama peserta tidak boleh kosong.";
                _txtNamaPeserta.Focus();
                return;
            }

            _namaPeserta = nama;

            string targetServer = _sibmUrl;
            string sessionTokenOrCode = rawCode;
            string systemSource = "SIBM";

            // Kasus 1: Input berupa URL Lengkap (misal salin tautan atau hasil scan QR)
            if (rawCode.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                rawCode.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                int idx = rawCode.IndexOf("/ujian/sesi/", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    try
                    {
                        Uri uri = new Uri(rawCode);
                        targetServer = uri.GetLeftPart(UriPartial.Authority);
                    }
                    catch { }

                    string sub = rawCode.Substring(idx + "/ujian/sesi/".Length);
                    int qIdx = sub.IndexOf('?');
                    sessionTokenOrCode = (qIdx >= 0) ? sub.Substring(0, qIdx) : sub;
                }
                else
                {
                    // Tautan langsung ke lembar ujian (Google Form atau link langsung lainnya)
                    _token = "DIRECT";
                    _examUrl = rawCode;
                    _baseUrl = targetServer;

                    _lblLobbyStatus.ForeColor = Color.FromArgb(52, 211, 153);
                    _lblLobbyStatus.Text = "Tautan form ujian langsung terdeteksi. Membuka ujian...";
                    Application.DoEvents();

                    if (!_isKioskActive)
                    {
                        EnterKioskMode();
                    }
                    LaunchExam();
                    return;
                }
            }

            _token = sessionTokenOrCode.Trim().ToUpper();

            _btnStart.Enabled = false;
            _btnStart.Text = "Memeriksa...";
            _lblLobbyStatus.ForeColor = Color.FromArgb(56, 189, 248);
            _lblLobbyStatus.Text = "Memeriksa kode sesi ke server...";
            Application.DoEvents();

            string judul = "";
            string errMessage = "";
            bool isValid = false;

            // Jika input adalah link URL lengkap: langsung verifikasi ke server domain dari link tersebut
            if (rawCode.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                rawCode.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                isValid = ApiClient.GetSessionInfo(targetServer, _token, out judul, out errMessage);
                if (isValid)
                {
                    _baseUrl = targetServer;
                    systemSource = targetServer.IndexOf("psbm", StringComparison.OrdinalIgnoreCase) >= 0 ? "PSBM" : "SIBM";
                }
            }
            else
            {
                // Kasus 2: Input hanya 6 karakter kode sesi unik (Pembeda SIBM vs PSBM)
                // 1. Cek terlebih dahulu ke server SIBM jika dikonfigurasi
                if (!string.IsNullOrEmpty(_sibmUrl))
                {
                    isValid = ApiClient.GetSessionInfo(_sibmUrl, _token, out judul, out errMessage);
                    if (isValid)
                    {
                        _baseUrl = _sibmUrl;
                        systemSource = "SIBM";
                    }
                }

                // 2. Jika tidak ditemukan di SIBM, otomatis coba verifikasi ke server PSBM
                if (!isValid && !string.IsNullOrEmpty(_psbmUrl))
                {
                    string psbmJudul;
                    string psbmErrMessage;
                    if (ApiClient.GetSessionInfo(_psbmUrl, _token, out psbmJudul, out psbmErrMessage))
                    {
                        isValid = true;
                        judul = psbmJudul;
                        _baseUrl = _psbmUrl;
                        systemSource = "PSBM";
                    }
                }
            }

            if (!isValid)
            {
                _btnStart.Enabled = true;
                _btnStart.Text = "MULAI UJIAN";
                _lblLobbyStatus.ForeColor = Color.FromArgb(248, 113, 113);
                _lblLobbyStatus.Text = string.IsNullOrEmpty(errMessage) ? "Kode sesi tidak ditemukan di server SIBM maupun PSBM." : errMessage;
                _txtKodeSesi.SelectAll();
                _txtKodeSesi.Focus();
                return;
            }

            _examUrl = string.Format("{0}/ujian/sesi/{1}", _baseUrl.TrimEnd('/'), _token);

            // Simpan nama peserta tanpa merusak konfigurasi URL server di config.ini
            SaveSettings();

            // Saat ujian dimulai: WAJIB MASUK KIOSK
            if (!_isKioskActive)
            {
                EnterKioskMode();
            }

            _lblLobbyStatus.ForeColor = Color.FromArgb(52, 211, 153);
            _lblLobbyStatus.Text = string.Format("[{0}] {1}. Membuka ujian...", systemSource, judul);
            Application.DoEvents();

            LaunchExam();
        }

        private async void LaunchExam()
        {
            try
            {
                // 1. Hide Lobby
                _lobbyPanel.Visible = false;

                // Pastikan Kiosk aktif (karena pas ujian wajib kiosk)
                if (!_isKioskActive)
                {
                    EnterKioskMode();
                }

                // 2. Initialize WebView2
                _webView = new WebView2();
                _webView.Dock = DockStyle.Fill;
                this.Controls.Add(_webView);

                string userDataFolder = Path.Combine(Path.GetTempPath(), "SIBM_Exambro_Cache");
                CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await _webView.EnsureCoreWebView2Async(env);

                // Configure WebView2 settings for kiosk
                CoreWebView2Settings settings = _webView.CoreWebView2.Settings;
                settings.AreDefaultContextMenusEnabled = false;
                settings.AreDevToolsEnabled = false;
                settings.IsStatusBarEnabled = false;
                settings.IsZoomControlEnabled = false;

                string customUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 SIBM-Desktop-ExamBrowser/1.0 SIBM-Wali-ExamBrowser/1.0";
                settings.UserAgent = customUserAgent;

                // Attach headers for SIBM Portal verification
                _webView.CoreWebView2.WebResourceRequested += (s, args) => {
                    args.Request.Headers.SetHeader("X-SIBM-Exam-App", "SIBM-Desktop-Exambro");
                    args.Request.Headers.SetHeader("X-SIBM-Device-Name", _deviceName);
                    args.Request.Headers.SetHeader("X-SIBM-User-Name", _namaPeserta);
                };
                _webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);

                // Send initial activity log
                ApiClient.SendActivityLog(_baseUrl, _token, "buka_ujian", "Sesi ujian dibuka melalui SIBM Exambrowser Desktop (Kode Sesi: " + _token + ")", _namaPeserta, _deviceName);

                // Navigate to exam URL
                _webView.CoreWebView2.Navigate(_examUrl);

                _isExamRunning = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menginisialisasi WebView2: " + ex.Message, "Kesalahan Kiosk", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SafeExit();
            }
        }

        private void MainForm_Deactivate(object sender, EventArgs e)
        {
            if (!_isKioskActive || _isLockOverlayShowing || _isExiting)
            {
                return;
            }

            if (_isExamRunning)
            {
                // Student attempted to switch window or an external window took focus
                _isLockOverlayShowing = true;

                // Log activity to SIBM server
                ApiClient.SendActivityLog(_baseUrl, _token, "keluar_layar", "Siswa terdeteksi keluar dari layar / berpindah jendela aplikasi pada desktop", _namaPeserta, _deviceName);

                using (LockOverlayForm overlay = new LockOverlayForm(_baseUrl, _token, _namaPeserta, _deviceName, "Terdeteksi jendela ujian kehilangan fokus atau beralih aplikasi."))
                {
                    DialogResult res = overlay.ShowDialog(this);
                    _isLockOverlayShowing = false;

                    if (res == DialogResult.OK)
                    {
                        this.Activate();
                        this.TopMost = true;
                    }
                    else if (res == DialogResult.Abort)
                    {
                        SafeExit();
                    }
                }
            }
            else
            {
                // Kiosk aktif di layar awal lobi: kembalikan fokus ke aplikasi
                this.Activate();
                this.TopMost = true;
            }
        }

        public void HandleAdminExit()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(HandleAdminExit));
                return;
            }

            using (AdminExitForm form = new AdminExitForm())
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    SafeExit("Admin SIBM");
                }
            }
        }

        public void HandleSupervisorEmergencyExit()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(HandleSupervisorEmergencyExit));
                return;
            }

            using (SupervisorExitForm form = new SupervisorExitForm(_baseUrl, _token, _namaPeserta, _deviceName))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    SafeExit("Pengawas Ujian (PIN Dinamis)");
                }
            }
        }

        public void SafeExit(string exitReason = "Pengawas")
        {
            _isExiting = true;
            _isExamRunning = false;
            _isKioskActive = false;

            try
            {
                // Send closing log
                if (!string.IsNullOrEmpty(_token))
                {
                    ApiClient.SendActivityLog(_baseUrl, _token, "submit", "Aplikasi SIBM Exambrowser Desktop ditutup secara aman oleh " + exitReason, _namaPeserta, _deviceName);
                }

                // 1. Unhook low-level keyboard hook
                if (_keyboardHook != null)
                {
                    _keyboardHook.Uninstall();
                }

                // 2. Restore Windows Registry (Re-enable Task Manager & Ctrl+Alt+Del options)
                SystemPolicy.Restore();
            }
            catch (Exception) { }

            Application.Exit();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isKioskActive && !_isExiting)
            {
                // Dalam mode kiosk (baik di lobi maupun saat ujian), penutupan dicegah kecuali lewat SafeExit
                e.Cancel = true;
                return;
            }

            // Always ensure registry & hook are restored
            try
            {
                if (_keyboardHook != null) _keyboardHook.Uninstall();
                SystemPolicy.Restore();
            }
            catch (Exception) { }
        }
    }
}
