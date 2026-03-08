using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FolderPlus
{
    public partial class Form1 : Form
    {
        // Win32 API for Hotkeys
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);
        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // Win32 API for Context Awareness (Explorer only)
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private string targetPath = "";
        private bool isNewPlusMode = false;
        private Color brandBlue = Color.FromArgb(0, 120, 215);
        private string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.ico");
        private string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mitraphix");
        private string templatesDir;
        private string separatorsFile;
        private string settingsFile;
        private string newPlusSettingsFile;
        private string hotkeyFile;
        private readonly string[] defaultSeparators = { "None", "Space ( )", "_", "-", ".", "+", "@", "#" };

        // PayPal Donation Link
        private string paypalLink = "https://paypal.me/mitraphix";

        // UI Controls
        private TextBox txtNames;
        private ComboBox cmbSep, cmbStyle, cmbPreNum;
        private NumericUpDown numStart, numEnd;
        private Panel pnlHamburger;
        private NotifyIcon trayIcon;
        private CheckBox chkMulti;
        private ComboBox cmbNewSep, cmbNewStyle;
        private NumericUpDown numNewEnd;

        // Dynamic Hotkey Variables
        private int hk1Mod = 6;
        private Keys hk1Key = Keys.F;
        private int hk2Mod = 6;
        private Keys hk2Key = Keys.D;

        private bool hasShownTrayNotification = false;
        private bool hotkeysRegistered = false;
        private Timer contextTimer;

        public Form1(bool newPlusMode = false)
        {
            isNewPlusMode = newPlusMode;
            templatesDir = Path.Combine(appDataPath, "Templates");
            separatorsFile = Path.Combine(appDataPath, "separators.txt");
            settingsFile = Path.Combine(appDataPath, "state.txt");
            newPlusSettingsFile = Path.Combine(appDataPath, "newplus_state.txt");
            hotkeyFile = Path.Combine(appDataPath, "hotkeys.txt");

            if (!Directory.Exists(templatesDir)) Directory.CreateDirectory(templatesDir);
            EnsureDefaultSeparators();
            LoadHotkeys();

            InitializeComponent();
            DetectContextPath();

            this.SuspendLayout();
            if (isNewPlusMode) SetupNewPlusUI();
            else SetupMitraphixUI();
            this.ResumeLayout(false);

            SetupSystemTray();
            InitializeContextTimer();

            this.Load += (s, e) => LoadLastState();
            this.FormClosing += HandleFormClosing;
            this.Click += (s, e) => { if (pnlHamburger != null) pnlHamburger.Visible = false; };
        }

        // --- CONTEXT-AWARE SHORTCUT ENGINE ---
        private void InitializeContextTimer()
        {
            contextTimer = new Timer();
            contextTimer.Interval = 500;
            contextTimer.Tick += ContextTimer_Tick;
            contextTimer.Start();
        }

        private void ContextTimer_Tick(object sender, EventArgs e)
        {
            if (IsExplorerActive())
            {
                if (!hotkeysRegistered) { RegisterShortcuts(); hotkeysRegistered = true; }
            }
            else
            {
                if (hotkeysRegistered) { UnregisterShortcuts(); hotkeysRegistered = false; }
            }
        }

        private bool IsExplorerActive()
        {
            IntPtr hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero) return false;
            GetWindowThreadProcessId(hWnd, out uint processId);
            try { Process proc = Process.GetProcessById((int)processId); return proc.ProcessName.ToLower() == "explorer"; }
            catch { return false; }
        }

        private void LoadHotkeys()
        {
            if (File.Exists(hotkeyFile))
            {
                try
                {
                    string[] lines = File.ReadAllLines(hotkeyFile);
                    if (lines.Length >= 4) { hk1Mod = int.Parse(lines[0]); Enum.TryParse(lines[1], out hk1Key); hk2Mod = int.Parse(lines[2]); Enum.TryParse(lines[3], out hk2Key); }
                }
                catch { }
            }
        }

        private void SaveAndApplyHotkeys(int mod1, Keys key1, int mod2, Keys key2)
        {
            hk1Mod = mod1; hk1Key = key1; hk2Mod = mod2; hk2Key = key2;
            File.WriteAllLines(hotkeyFile, new string[] { mod1.ToString(), key1.ToString(), mod2.ToString(), key2.ToString() });
            UnregisterShortcuts(); hotkeysRegistered = false;
        }

        private void RegisterShortcuts() { RegisterHotKey(this.Handle, 1, hk1Mod, (int)hk1Key); RegisterHotKey(this.Handle, 2, hk2Mod, (int)hk2Key); }
        private void UnregisterShortcuts() { UnregisterHotKey(this.Handle, 1); UnregisterHotKey(this.Handle, 2); }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312) { int id = m.WParam.ToInt32(); if (id == 1) { isNewPlusMode = false; ShowApp(); } if (id == 2) { isNewPlusMode = true; ShowApp(); } }
            base.WndProc(ref m);
        }

        // --- SYSTEM TRAY & PERSISTENCE ---
        private void HandleFormClosing(object sender, FormClosingEventArgs e)
        {
            SaveCurrentState();
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true; this.Hide();
                if (!hasShownTrayNotification) { trayIcon.ShowBalloonTip(2000, "Mitraphix Folder+", "Running in background to listen for shortcuts.", ToolTipIcon.Info); hasShownTrayNotification = true; }
            }
            else UnregisterShortcuts();
        }

        private void SetupSystemTray()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Icon = File.Exists(logoPath) ? new Icon(logoPath) : SystemIcons.Application;
            trayIcon.Text = "Mitraphix Folder+";
            trayIcon.Visible = true;
            ContextMenu menu = new ContextMenu();
            menu.MenuItems.Add("Open Folder+", (s, e) => { isNewPlusMode = false; ShowApp(); });
            menu.MenuItems.Add("Open New+", (s, e) => { isNewPlusMode = true; ShowApp(); });
            menu.MenuItems.Add("-");
            menu.MenuItems.Add("Exit Completely", (s, e) => { trayIcon.Visible = false; Application.Exit(); });
            trayIcon.ContextMenu = menu;
            trayIcon.DoubleClick += (s, e) => ShowApp();
        }

        private void ShowApp()
        {
            this.Controls.Clear();
            if (isNewPlusMode) SetupNewPlusUI(); else SetupMitraphixUI();
            LoadLastState(); this.Show(); this.WindowState = FormWindowState.Normal; this.BringToFront(); this.Activate();
        }

        private void DetectContextPath()
        {
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                int pathIndex = isNewPlusMode ? 2 : 1;
                if (args.Length > pathIndex)
                {
                    string potentialPath = args[pathIndex].Trim('\"', ' ', '\'');
                    if (potentialPath.EndsWith("\\") && !potentialPath.EndsWith(":\\")) potentialPath = potentialPath.TrimEnd('\\');
                    if (Directory.Exists(potentialPath)) targetPath = potentialPath; else targetPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                }
                else targetPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
            catch { targetPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop); }
        }

        private void LoadLastState()
        {
            if (!isNewPlusMode && File.Exists(settingsFile))
            {
                try
                {
                    string[] lines = File.ReadAllLines(settingsFile);
                    if (lines.Length >= 6) { cmbPreNum.SelectedIndex = int.Parse(lines[0]); txtNames.Text = lines[1]; cmbSep.SelectedIndex = int.Parse(lines[2]); cmbStyle.SelectedIndex = int.Parse(lines[3]); numStart.Value = decimal.Parse(lines[4]); numEnd.Value = decimal.Parse(lines[5]); }
                }
                catch { }
            }
            else if (isNewPlusMode && File.Exists(newPlusSettingsFile))
            {
                try
                {
                    string[] lines = File.ReadAllLines(newPlusSettingsFile);
                    if (lines.Length >= 3) { cmbNewSep.SelectedIndex = int.Parse(lines[0]); cmbNewStyle.SelectedIndex = int.Parse(lines[1]); numNewEnd.Value = decimal.Parse(lines[2]); }
                }
                catch { }
            }
        }

        private void SaveCurrentState()
        {
            if (!isNewPlusMode && txtNames != null)
            {
                string[] state = { cmbPreNum.SelectedIndex.ToString(), txtNames.Text, cmbSep.SelectedIndex.ToString(), cmbStyle.SelectedIndex.ToString(), numStart.Value.ToString(), numEnd.Value.ToString() };
                File.WriteAllLines(settingsFile, state);
            }
            else if (isNewPlusMode && cmbNewSep != null)
            {
                string[] state = { cmbNewSep.SelectedIndex.ToString(), cmbNewStyle.SelectedIndex.ToString(), numNewEnd.Value.ToString() };
                File.WriteAllLines(newPlusSettingsFile, state);
            }
        }

        // --- SHORTCUT SETTINGS UI ---
        private int GetWin32Modifier(Keys modifiers)
        {
            int win32Mod = 0;
            if ((modifiers & Keys.Control) == Keys.Control) win32Mod |= 0x0002;
            if ((modifiers & Keys.Alt) == Keys.Alt) win32Mod |= 0x0001;
            if ((modifiers & Keys.Shift) == Keys.Shift) win32Mod |= 0x0004;
            return win32Mod;
        }

        private string FormatShortcut(int win32Mod, Keys key)
        {
            List<string> parts = new List<string>();
            if ((win32Mod & 0x0002) != 0) parts.Add("Ctrl");
            if ((win32Mod & 0x0001) != 0) parts.Add("Alt");
            if ((win32Mod & 0x0004) != 0) parts.Add("Shift");
            parts.Add(key.ToString());
            return string.Join(" + ", parts);
        }

        private void OpenShortcutSettings()
        {
            Form hkForm = new Form() { Text = "Global Shortcuts", Size = new Size(360, 260), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, BackColor = Color.White };

            int temp1Mod = hk1Mod; Keys temp1Key = hk1Key;
            int temp2Mod = hk2Mod; Keys temp2Key = hk2Key;

            Label lblHint = new Label() { Text = "Click a box and press your preferred shortcut.", Location = new Point(20, 15), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Italic), ForeColor = Color.Gray };

            Label lblFPlus = new Label() { Text = "Folder+ Shortcut:", Location = new Point(20, 50), AutoSize = true, Font = new Font("Segoe UI Semibold", 10f) };
            TextBox txtFPlus = new TextBox() { Location = new Point(160, 48), Width = 150, Font = new Font("Segoe UI", 10f), ReadOnly = true, BackColor = Color.White, Cursor = Cursors.Hand, Text = FormatShortcut(hk1Mod, hk1Key) };

            Label lblNPlus = new Label() { Text = "New+ Shortcut:", Location = new Point(20, 90), AutoSize = true, Font = new Font("Segoe UI Semibold", 10f) };
            TextBox txtNPlus = new TextBox() { Location = new Point(160, 88), Width = 150, Font = new Font("Segoe UI", 10f), ReadOnly = true, BackColor = Color.White, Cursor = Cursors.Hand, Text = FormatShortcut(hk2Mod, hk2Key) };

            KeyEventHandler captureKey = (s, e) => {
                e.SuppressKeyPress = true;
                if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.Menu) return;
                int winMod = GetWin32Modifier(e.Modifiers);
                if (winMod == 0) { MessageBox.Show("Please include at least one modifier (Ctrl, Alt, or Shift).", "Mitraphix"); return; }

                TextBox tb = (TextBox)s;
                if (tb == txtFPlus) { temp1Mod = winMod; temp1Key = e.KeyCode; tb.Text = FormatShortcut(temp1Mod, temp1Key); }
                else { temp2Mod = winMod; temp2Key = e.KeyCode; tb.Text = FormatShortcut(temp2Mod, temp2Key); }
            };

            txtFPlus.KeyDown += captureKey;
            txtNPlus.KeyDown += captureKey;

            Button btnSave = new Button() { Text = "SAVE && APPLY", Location = new Point(20, 145), Size = new Size(290, 45), BackColor = brandBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) => {
                SaveAndApplyHotkeys(temp1Mod, temp1Key, temp2Mod, temp2Key);
                MessageBox.Show("Shortcuts updated successfully.", "Mitraphix");
                hkForm.Close();
            };

            hkForm.Controls.AddRange(new Control[] { lblHint, lblFPlus, txtFPlus, lblNPlus, txtNPlus, btnSave });
            hkForm.ShowDialog();
        }

        // --- UI GENERATION ---
        private void SetupNewPlusUI()
        {
            this.Text = "Mitraphix New+";
            if (File.Exists(logoPath)) this.Icon = new Icon(logoPath);
            this.StartPosition = FormStartPosition.CenterScreen; this.FormBorderStyle = FormBorderStyle.FixedToolWindow; this.BackColor = Color.White;

            Label lblInfo = new Label() { Text = "Select Template to Deploy:", Location = new Point(18, 18), AutoSize = true, Font = new Font("Segoe UI", 11f, FontStyle.Bold) };
            Button btnManage = new Button() { Text = "\uE713", Location = new Point(255, 14), Size = new Size(32, 32), Font = new Font("Segoe MDL2 Assets", 12f, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, BackColor = Color.White, ForeColor = Color.Black };
            btnManage.FlatAppearance.BorderSize = 0; btnManage.Click += (s, e) => Process.Start("explorer.exe", templatesDir);
            this.Controls.AddRange(new Control[] { lblInfo, btnManage });

            chkMulti = new CheckBox() { Text = "Deploy Multiple Copies", Location = new Point(18, 55), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            cmbNewSep = new ComboBox() { Location = new Point(18, 85), Width = 85, Font = new Font("Segoe UI", 9f), DropDownStyle = ComboBoxStyle.DropDownList, Enabled = false };
            EnsureDefaultSeparators(); cmbNewSep.Items.AddRange(File.ReadAllLines(separatorsFile)); if (cmbNewSep.Items.Count > 0) cmbNewSep.SelectedIndex = cmbNewSep.Items.Count > 2 ? 2 : 0;
            cmbNewStyle = new ComboBox() { Location = new Point(110, 85), Width = 105, Font = new Font("Segoe UI", 9f), DropDownStyle = ComboBoxStyle.DropDownList, Enabled = false };
            cmbNewStyle.Items.AddRange(new string[] { "1, 2, 3", "01, 02, 03", "I, II, III", "A, B, C", "a, b, c" }); cmbNewStyle.SelectedIndex = 1;
            numNewEnd = new NumericUpDown() { Location = new Point(222, 85), Width = 70, Font = new Font("Segoe UI", 9f), Minimum = 2, Maximum = 999, Value = 5, Enabled = false };
            chkMulti.CheckedChanged += (s, e) => { cmbNewSep.Enabled = chkMulti.Checked; cmbNewStyle.Enabled = chkMulti.Checked; numNewEnd.Enabled = chkMulti.Checked; };
            this.Controls.AddRange(new Control[] { chkMulti, cmbNewSep, cmbNewStyle, numNewEnd });

            string[] templates = Directory.GetDirectories(templatesDir);
            int yPos = 125;
            if (templates.Length == 0)
            {
                Label lblEmpty = new Label() { Text = "No templates found.\nClick the gear icon to add folders.", Location = new Point(18, yPos), AutoSize = true, Font = new Font("Segoe UI", 10f) };
                this.Controls.Add(lblEmpty); yPos += 50;
            }
            else
            {
                foreach (string tDir in templates)
                {
                    string tName = new DirectoryInfo(tDir).Name;
                    Button btnT = new Button() { Text = "📁 " + tName, Location = new Point(18, yPos), Size = new Size(270, 45), Font = new Font("Segoe UI", 10.5f), FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft };
                    btnT.FlatAppearance.BorderColor = Color.LightGray;
                    btnT.Click += async (s, e) => {
                        SaveCurrentState(); btnT.Text = "Deploying..."; btnT.Enabled = false;
                        bool isMulti = chkMulti.Checked; string sep = cmbNewSep.SelectedItem?.ToString() ?? ""; if (sep == "None") sep = ""; else if (sep == "Space ( )") sep = " ";
                        int styleIdx = cmbNewStyle.SelectedIndex + 1; int endCount = (int)numNewEnd.Value;
                        await Task.Run(() => { if (!isMulti) DeployTemplate(tDir, ""); else { for (int i = 1; i <= endCount; i++) DeployTemplate(tDir, sep + GetFormattedMainNum(i, styleIdx)); } });
                        this.Invoke((MethodInvoker)delegate { this.Hide(); btnT.Text = "📁 " + tName; btnT.Enabled = true; });
                    };
                    this.Controls.Add(btnT); yPos += 52;
                }
            }
            this.ClientSize = new Size(310, yPos + 20);
        }

        private void DeployTemplate(string sourceDir, string suffix) { try { string dirName = new DirectoryInfo(sourceDir).Name; if (!string.IsNullOrEmpty(suffix)) dirName += suffix; string destDir = Path.Combine(targetPath, dirName); CopyDirectoryRecursive(sourceDir, destDir); } catch (Exception ex) { this.Invoke((MethodInvoker)delegate { MessageBox.Show("Deployment Error:\n" + ex.Message, "Error"); }); } }
        private void CopyDirectoryRecursive(string source, string target) { Directory.CreateDirectory(target); foreach (var file in Directory.GetFiles(source)) File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true); foreach (var dir in Directory.GetDirectories(source)) CopyDirectoryRecursive(dir, Path.Combine(target, Path.GetFileName(dir))); }

        private void SetupMitraphixUI()
        {
            this.Text = "Mitraphix Folder+"; if (File.Exists(logoPath)) this.Icon = new Icon(logoPath);
            this.StartPosition = FormStartPosition.CenterScreen; this.FormBorderStyle = FormBorderStyle.FixedSingle; this.MaximizeBox = false; this.BackColor = Color.White;
            Font inputFont = new Font("Segoe UI Semibold", 11.5f);
            Button btnHam = new Button() { Text = "≡", Font = new Font("Segoe UI", 18f, FontStyle.Bold), Location = new Point(345, 5), Size = new Size(45, 45), FlatStyle = FlatStyle.Flat, BackColor = Color.White };
            btnHam.FlatAppearance.BorderSize = 0; btnHam.Click += (s, e) => { pnlHamburger.Visible = !pnlHamburger.Visible; if (pnlHamburger.Visible) pnlHamburger.BringToFront(); };
            this.Controls.Add(btnHam);
            pnlHamburger = new Panel() { Size = new Size(220, 240), Location = new Point(170, 50), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Visible = false };
            pnlHamburger.Controls.Add(CreateMenuButton("Bulk Import (CSV/TXT)", 0, (s, e) => HandleBulkImport()));
            pnlHamburger.Controls.Add(CreateMenuButton("Manage Templates (New+)", 48, (s, e) => Process.Start("explorer.exe", templatesDir)));
            pnlHamburger.Controls.Add(CreateMenuButton("Manage Separators", 96, (s, e) => OpenSeparatorManager()));
            pnlHamburger.Controls.Add(CreateMenuButton("Shortcut Settings", 144, (s, e) => OpenShortcutSettings()));
            pnlHamburger.Controls.Add(CreateMenuButton("About Mitraphix", 192, (s, e) => ShowModernAbout()));
            this.Controls.Add(pnlHamburger);
            int lblX = 25; int inputX = 155; int inputW = 215; int startY = 70; int spacing = 54;
            AddLabel("Serial No:", lblX, startY + 4, 10.5f); cmbPreNum = new ComboBox() { Location = new Point(inputX, startY), Width = inputW, Font = inputFont, DropDownStyle = ComboBoxStyle.DropDownList }; cmbPreNum.Items.AddRange(new string[] { "None", "1.", "1)", "(1)", "01.", "01)", "(01)" }); cmbPreNum.SelectedIndex = 0; this.Controls.Add(cmbPreNum);
            AddLabel("Folder Name:", lblX, startY + spacing + 4, 10.5f); txtNames = new TextBox() { Location = new Point(inputX, startY + spacing), Width = inputW, Font = inputFont }; this.Controls.Add(txtNames);
            AddLabel("(Comma separated)", inputX, startY + spacing + 28, 8.5f, Color.Gray);
            AddLabel("Separator:", lblX, startY + spacing * 2 + 4, 10.5f); cmbSep = new ComboBox() { Location = new Point(inputX, startY + spacing * 2), Width = inputW, Font = inputFont, DropDownStyle = ComboBoxStyle.DropDownList }; LoadSeparators(); this.Controls.Add(cmbSep);
            AddLabel("End Serial:", lblX, startY + spacing * 3 + 4, 10.5f); cmbStyle = new ComboBox() { Location = new Point(inputX, startY + spacing * 3), Width = inputW, Font = inputFont, DropDownStyle = ComboBoxStyle.DropDownList }; cmbStyle.Items.AddRange(new string[] { "None", "Numeric (1, 2, 3)", "Leading Zero (01, 02, 03)", "Roman (I, II, III)", "Alpha Upper (A, B, C)", "Alpha Lower (a, b, c)" }); cmbStyle.SelectedIndex = 2; cmbStyle.SelectedIndexChanged += (s, e) => { numStart.Enabled = numEnd.Enabled = cmbPreNum.Enabled = cmbStyle.SelectedIndex != 0; }; this.Controls.Add(cmbStyle);
            AddLabel("Range:", lblX, startY + spacing * 4 + 4, 10.5f); numStart = new NumericUpDown() { Location = new Point(inputX, startY + spacing * 4), Width = 100, Font = inputFont, Minimum = 1, Maximum = 9999, Value = 1 }; numEnd = new NumericUpDown() { Location = new Point(inputX + 115, startY + spacing * 4), Width = 100, Font = inputFont, Minimum = 1, Maximum = 9999, Value = 5 }; this.Controls.Add(numStart); this.Controls.Add(numEnd);
            Button btnGen = new Button() { Text = "GENERATE FOLDERS", Location = new Point(lblX, startY + spacing * 5 + 15), Size = new Size(345, 60), Font = new Font("Segoe UI", 12f, FontStyle.Bold), BackColor = brandBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnGen.FlatAppearance.BorderSize = 0; btnGen.Click += async (s, e) => { SaveCurrentState(); btnGen.Text = "GENERATING..."; btnGen.Enabled = false; await Task.Run(() => RunGeneration()); this.Hide(); btnGen.Text = "GENERATE FOLDERS"; btnGen.Enabled = true; };
            this.Controls.Add(btnGen); this.ClientSize = new Size(400, btnGen.Bottom + 30);
        }

        private void AddLabel(string text, int x, int y, float fontSize = 10f, Color? c = null) { Label lbl = new Label() { Text = text, Location = new Point(x, y), AutoSize = true, Font = new Font("Segoe UI Semibold", fontSize) }; if (c.HasValue) lbl.ForeColor = c.Value; this.Controls.Add(lbl); }
        private Button CreateMenuButton(string text, int y, EventHandler onClick) { Button b = new Button() { Text = text, Size = new Size(220, 48), Location = new Point(0, y), FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.White, Font = new Font("Segoe UI", 10.5f) }; b.FlatAppearance.BorderSize = 0; b.MouseEnter += (s, e) => { b.BackColor = brandBlue; b.ForeColor = Color.White; }; b.MouseLeave += (s, e) => { b.BackColor = Color.White; b.ForeColor = Color.Black; }; b.Click += (s, e) => { pnlHamburger.Visible = false; onClick(s, e); }; return b; }

        private void ShowModernAbout()
        {
            Form aboutForm = new Form() { Text = "About Mitraphix Design", Size = new Size(380, 280), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, BackColor = Color.White };
            Label lblTitle = new Label() { Text = "Mitraphix Folder+", Location = new Point(25, 20), AutoSize = true, Font = new Font("Segoe UI", 18f, FontStyle.Bold), ForeColor = brandBlue };
            Label lblVersion = new Label() { Text = "Version 1.3.0 (Stable)", Location = new Point(28, 60), AutoSize = true, Font = new Font("Segoe UI", 10f), ForeColor = Color.Gray };
            Label lblDev = new Label() { Text = "Developed by Suman Mitra", Location = new Point(28, 140), AutoSize = true, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold) };

            Button btnSupport = new Button() { Text = "Support This Project ❤️", Location = new Point(28, 185), Size = new Size(200, 40), BackColor = Color.FromArgb(255, 196, 57), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
            btnSupport.FlatAppearance.BorderSize = 0; btnSupport.Click += (s, e) => Process.Start(paypalLink);

            Button btnClose = new Button() { Text = "CLOSE", Location = new Point(240, 185), Size = new Size(100, 40), BackColor = brandBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
            btnClose.FlatAppearance.BorderSize = 0; btnClose.Click += (s, e) => aboutForm.Close();

            aboutForm.Controls.AddRange(new Control[] { lblTitle, lblVersion, lblDev, btnSupport, btnClose });
            aboutForm.ShowDialog();
        }

        private void EnsureDefaultSeparators() { if (!File.Exists(separatorsFile)) File.WriteAllLines(separatorsFile, defaultSeparators); }
        private void LoadSeparators() { cmbSep.Items.Clear(); EnsureDefaultSeparators(); cmbSep.Items.AddRange(File.ReadAllLines(separatorsFile)); if (cmbSep.Items.Count > 0) cmbSep.SelectedIndex = 2 < cmbSep.Items.Count ? 2 : 0; }

        private void OpenSeparatorManager()
        {
            Form f = new Form() { Text = "Manage Separators", Size = new Size(350, 420), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, BackColor = Color.White };
            TextBox txtEditor = new TextBox() { Location = new Point(18, 60), Size = new Size(295, 240), Multiline = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Segoe UI", 11f) };
            txtEditor.Text = File.Exists(separatorsFile) ? File.ReadAllText(separatorsFile) : string.Join(Environment.NewLine, defaultSeparators);
            f.Shown += (s, e) => { txtEditor.SelectionStart = txtEditor.Text.Length; txtEditor.SelectionLength = 0; };
            Button btnSave = new Button() { Text = "SAVE && CLOSE", Location = new Point(18, 315), Size = new Size(295, 45), BackColor = brandBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold) };
            btnSave.Click += (s, e) => f.Close();
            f.FormClosing += (s, e) => { File.WriteAllText(separatorsFile, txtEditor.Text.Trim() != "" ? txtEditor.Text.Trim() : string.Join(Environment.NewLine, defaultSeparators)); LoadSeparators(); };
            f.Controls.AddRange(new Control[] { new Label() { Text = "Edit separators (one per line).", Location = new Point(18, 15), AutoSize = true, ForeColor = Color.Gray }, txtEditor, btnSave });
            f.ShowDialog();
        }

        private async void HandleBulkImport() { using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Text/CSV Files|*.txt;*.csv" }) { if (ofd.ShowDialog() == DialogResult.OK) { await Task.Run(() => { try { foreach (var line in File.ReadAllLines(ofd.FileName)) { string s = SanitizeFolderName(line.Trim()); if (s != "") Directory.CreateDirectory(Path.Combine(targetPath, s)); } } catch { } }); this.Hide(); } } }
        private string SanitizeFolderName(string n) { string inv = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()); foreach (char c in inv) n = n.Replace(c.ToString(), ""); return n; }

        private void RunGeneration()
        {
            string raw = ""; string sep = ""; int style = 0; int preIdx = 0; string preT = ""; int start = 1; int end = 5;
            this.Invoke((MethodInvoker)delegate { raw = txtNames.Text; sep = cmbSep.SelectedItem.ToString(); style = cmbStyle.SelectedIndex; preIdx = cmbPreNum.SelectedIndex; preT = cmbPreNum.Text; start = (int)numStart.Value; end = (int)numEnd.Value; });
            string[] names = raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries); if (names.Length == 0) return;
            if (sep == "None") sep = ""; else if (sep == "Space ( )") sep = " ";
            try { foreach (string n in names) { string b = SanitizeFolderName(n.Trim()); if (b == "") continue; if (style == 0) Directory.CreateDirectory(Path.Combine(targetPath, b)); else { for (int i = start; i <= end; i++) Directory.CreateDirectory(Path.Combine(targetPath, (GetFormattedPreNum(i, preIdx, preT) + b + sep + GetFormattedMainNum(i, style)).Trim())); } } }
            catch { }
        }

        private string GetFormattedPreNum(int i, int idx, string text) { if (idx == 0) return ""; string n = (idx > 3) ? i.ToString("D2") : i.ToString(); if (text.Contains("()")) return $"({n}) "; if (text.Contains(")")) return $"({n}) "; return $"{n}. "; }
        private string GetFormattedMainNum(int i, int style) { switch (style) { case 1: return i.ToString(); case 2: return i.ToString("D2"); case 3: return ToRoman(i); case 4: return ((char)(64 + i)).ToString(); case 5: return ((char)(96 + i)).ToString(); default: return ""; } }
        private string ToRoman(int n) { var m = new Dictionary<int, string> { { 1000, "M" }, { 900, "CM" }, { 500, "D" }, { 400, "CD" }, { 100, "C" }, { 90, "XC" }, { 50, "L" }, { 40, "XL" }, { 10, "X" }, { 9, "IX" }, { 5, "V" }, { 4, "IV" }, { 1, "I" } }; string r = ""; foreach (var kv in m) { while (n >= kv.Key) { r += kv.Value; n -= kv.Key; } } return r; }
    }
}