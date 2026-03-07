using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FolderPlus
{
    public partial class Form1 : Form
    {
        private string targetPath = "";
        private bool isNewPlusMode = false;
        private Color brandBlue = Color.FromArgb(0, 120, 215);
        private string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.ico");
        private string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mitraphix");
        private string templatesDir;
        private string separatorsFile;
        private readonly string[] defaultSeparators = { "None", "Space ( )", "_", "-", ".", "+", "@", "#" };

        private TextBox txtNames;
        private ComboBox cmbSep, cmbStyle, cmbPreNum;
        private NumericUpDown numStart, numEnd;
        private Panel pnlHamburger;

        public Form1(bool newPlusMode = false)
        {
            isNewPlusMode = newPlusMode;
            templatesDir = Path.Combine(appDataPath, "Templates");
            separatorsFile = Path.Combine(appDataPath, "separators.txt");

            if (!Directory.Exists(templatesDir)) Directory.CreateDirectory(templatesDir);
            EnsureDefaultSeparators();

            InitializeComponent();
            DetectContextPath();

            this.SuspendLayout();
            if (isNewPlusMode) SetupNewPlusUI();
            else SetupMitraphixUI();
            this.ResumeLayout(false);

            this.Click += (s, e) => { if (pnlHamburger != null) pnlHamburger.Visible = false; };
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
                    if (potentialPath.EndsWith("\\") && !potentialPath.EndsWith(":\\"))
                        potentialPath = potentialPath.TrimEnd('\\');

                    if (Directory.Exists(potentialPath)) targetPath = potentialPath;
                    else targetPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                }
                else targetPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
            catch { targetPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop); }
        }

        // --- MITRAPHIX NEW+ ENGINE ---
        private void SetupNewPlusUI()
        {
            this.Text = "Mitraphix New+";
            if (File.Exists(logoPath)) this.Icon = new Icon(logoPath);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.BackColor = Color.White;

            Label lblInfo = new Label() { Text = "Select Template to Deploy:", Location = new Point(18, 18), AutoSize = true, Font = new Font("Segoe UI", 11f, FontStyle.Bold) };
            this.Controls.Add(lblInfo);

            string[] templates = Directory.GetDirectories(templatesDir);
            int yPos = 55;

            if (templates.Length == 0)
            {
                Label lblEmpty = new Label() { Text = "No templates found.\nUse 'Manage Templates' in Folder+ to add.", Location = new Point(18, 55), AutoSize = true, Font = new Font("Segoe UI", 10f) };
                this.Controls.Add(lblEmpty);
                yPos += 50;
            }
            else
            {
                foreach (string tDir in templates)
                {
                    string tName = new DirectoryInfo(tDir).Name;
                    Button btnT = new Button() { Text = "📁 " + tName, Location = new Point(18, yPos), Size = new Size(270, 45), Font = new Font("Segoe UI", 10.5f), FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft };
                    btnT.FlatAppearance.BorderColor = Color.LightGray;
                    btnT.Click += (s, e) => DeployTemplate(tDir);
                    this.Controls.Add(btnT);
                    yPos += 52;
                }
            }
            this.ClientSize = new Size(310, yPos + 20);
        }

        private void DeployTemplate(string sourceDir)
        {
            try
            {
                string dirName = new DirectoryInfo(sourceDir).Name;
                string destDir = Path.Combine(targetPath, dirName);
                CopyDirectoryRecursive(sourceDir, destDir);
                Application.Exit();
            }
            catch (Exception ex) { MessageBox.Show("Deployment Error:\n" + ex.Message, "Error"); }
        }

        private void CopyDirectoryRecursive(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (var file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
            foreach (var dir in Directory.GetDirectories(source))
                CopyDirectoryRecursive(dir, Path.Combine(target, Path.GetFileName(dir)));
        }

        // --- MAIN FOLDER+ ENGINE ---
        private void SetupMitraphixUI()
        {
            this.Text = "Mitraphix Folder+";
            if (File.Exists(logoPath)) this.Icon = new Icon(logoPath);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.White;

            // Scaled Fonts
            Font inputFont = new Font("Segoe UI Semibold", 11.5f);
            Font btnFont = new Font("Segoe UI", 12f, FontStyle.Bold);

            Button btnHam = new Button() { Text = "≡", Font = new Font("Segoe UI", 18f, FontStyle.Bold), Location = new Point(345, 5), Size = new Size(45, 45), FlatStyle = FlatStyle.Flat, BackColor = Color.White };
            btnHam.FlatAppearance.BorderSize = 0;
            btnHam.Click += (s, e) => { pnlHamburger.Visible = !pnlHamburger.Visible; if (pnlHamburger.Visible) pnlHamburger.BringToFront(); };
            this.Controls.Add(btnHam);

            pnlHamburger = new Panel() { Size = new Size(220, 195), Location = new Point(170, 50), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Visible = false };
            pnlHamburger.Controls.Add(CreateMenuButton("Bulk Import (CSV/TXT)", 0, (s, e) => HandleBulkImport()));
            pnlHamburger.Controls.Add(CreateMenuButton("Manage Templates (New+)", 48, (s, e) => System.Diagnostics.Process.Start("explorer.exe", templatesDir)));
            pnlHamburger.Controls.Add(CreateMenuButton("Manage Separators", 96, (s, e) => OpenSeparatorManager()));
            pnlHamburger.Controls.Add(CreateMenuButton("About Mitraphix", 144, (s, e) => ShowModernAbout()));
            this.Controls.Add(pnlHamburger);

            int lblX = 25; int inputX = 155; int inputW = 215; int startY = 70; int spacing = 54;

            AddLabel("Serial No:", lblX, startY + 4, 10.5f);
            cmbPreNum = new ComboBox() { Location = new Point(inputX, startY), Width = inputW, Font = inputFont, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbPreNum.Items.AddRange(new string[] { "None", "1.", "1)", "(1)", "01.", "01)", "(01)" });
            cmbPreNum.SelectedIndex = 0;
            this.Controls.Add(cmbPreNum);

            AddLabel("Folder Name:", lblX, startY + spacing + 4, 10.5f);
            txtNames = new TextBox() { Location = new Point(inputX, startY + spacing), Width = inputW, Font = inputFont };
            this.Controls.Add(txtNames);
            AddLabel("(Comma separated)", lblX, startY + spacing + 24, 8.5f, Color.Gray);

            AddLabel("Separator:", lblX, startY + spacing * 2 + 4, 10.5f);
            cmbSep = new ComboBox() { Location = new Point(inputX, startY + spacing * 2), Width = inputW, Font = inputFont, DropDownStyle = ComboBoxStyle.DropDownList };
            LoadSeparators();
            this.Controls.Add(cmbSep);

            AddLabel("End Serial:", lblX, startY + spacing * 3 + 4, 10.5f);
            cmbStyle = new ComboBox() { Location = new Point(inputX, startY + spacing * 3), Width = inputW, Font = inputFont, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStyle.Items.AddRange(new string[] { "None", "Numeric (1, 2, 3)", "Leading Zero (01, 02, 03)", "Roman (I, II, III)", "Alpha Upper (A, B, C)", "Alpha Lower (a, b, c)" });
            cmbStyle.SelectedIndex = 2;
            cmbStyle.SelectedIndexChanged += (s, e) => ToggleNumberingFields();
            this.Controls.Add(cmbStyle);

            AddLabel("Range:", lblX, startY + spacing * 4 + 4, 10.5f);
            numStart = new NumericUpDown() { Location = new Point(inputX, startY + spacing * 4), Width = 100, Font = inputFont, Minimum = 1, Maximum = 9999, Value = 1 };
            numEnd = new NumericUpDown() { Location = new Point(inputX + 115, startY + spacing * 4), Width = 100, Font = inputFont, Minimum = 1, Maximum = 9999, Value = 5 };
            this.Controls.Add(numStart); this.Controls.Add(numEnd);

            Button btnGen = new Button() { Text = "GENERATE FOLDERS", Location = new Point(lblX, startY + spacing * 5 + 15), Size = new Size(345, 60), Font = btnFont, BackColor = brandBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnGen.FlatAppearance.BorderSize = 0;
            btnGen.Click += (s, e) => RunGeneration();
            this.Controls.Add(btnGen);

            this.ClientSize = new Size(400, btnGen.Bottom + 30);
            foreach (Control c in this.Controls) { if (c != btnHam && c != pnlHamburger) c.Click += (s, e) => { if (pnlHamburger != null) pnlHamburger.Visible = false; }; }
        }

        private void ToggleNumberingFields()
        {
            bool isNumberingEnabled = cmbStyle.SelectedIndex != 0;
            numStart.Enabled = isNumberingEnabled;
            numEnd.Enabled = isNumberingEnabled;
            cmbPreNum.Enabled = isNumberingEnabled;
        }

        private void AddLabel(string text, int x, int y, float fontSize = 10f, Color? c = null)
        {
            Label lbl = new Label() { Text = text, Location = new Point(x, y), AutoSize = true, Font = new Font("Segoe UI Semibold", fontSize) };
            if (c.HasValue) lbl.ForeColor = c.Value;
            this.Controls.Add(lbl);
        }

        private Button CreateMenuButton(string text, int y, EventHandler onClick)
        {
            Button b = new Button() { Text = text, Size = new Size(220, 48), Location = new Point(0, y), FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft, BackColor = Color.White, Font = new Font("Segoe UI", 10.5f) };
            b.FlatAppearance.BorderSize = 0;
            b.MouseEnter += (s, e) => { b.BackColor = brandBlue; b.ForeColor = Color.White; };
            b.MouseLeave += (s, e) => { b.BackColor = Color.White; b.ForeColor = Color.Black; };
            b.Click += (s, e) => { pnlHamburger.Visible = false; onClick(s, e); };
            return b;
        }

        private void ShowModernAbout()
        {
            Form aboutForm = new Form() { Text = "About Mitraphix Design", Size = new Size(380, 240), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, BackColor = Color.White };

            Label lblTitle = new Label() { Text = "Mitraphix Folder+", Location = new Point(25, 20), AutoSize = true, Font = new Font("Segoe UI", 18f, FontStyle.Bold), ForeColor = brandBlue };
            Label lblVersion = new Label() { Text = "Version 1.0.0", Location = new Point(28, 60), AutoSize = true, Font = new Font("Segoe UI", 10f), ForeColor = Color.Gray };
            Label lblDesc = new Label() { Text = "Professional automation tool for designers.\nEngineered for precision and speed.", Location = new Point(28, 90), AutoSize = true, Font = new Font("Segoe UI", 10.5f) };
            Label lblDev = new Label() { Text = "Developed by Suman Mitra", Location = new Point(28, 140), AutoSize = true, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold) };

            Button btnClose = new Button() { Text = "Close", Location = new Point(240, 140), Size = new Size(100, 35), BackColor = brandBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => aboutForm.Close();

            aboutForm.Controls.AddRange(new Control[] { lblTitle, lblVersion, lblDesc, lblDev, btnClose });
            aboutForm.ShowDialog();
        }

        // --- DYNAMIC SEPARATOR MANAGER ---
        private void EnsureDefaultSeparators()
        {
            if (!File.Exists(separatorsFile))
            {
                File.WriteAllLines(separatorsFile, defaultSeparators);
            }
        }

        private void LoadSeparators()
        {
            cmbSep.Items.Clear();
            EnsureDefaultSeparators();
            cmbSep.Items.AddRange(File.ReadAllLines(separatorsFile));
            if (cmbSep.Items.Count > 0) cmbSep.SelectedIndex = 2 < cmbSep.Items.Count ? 2 : 0;
        }

        private void OpenSeparatorManager()
        {
            Form sepForm = new Form() { Text = "Manage Separators", Size = new Size(420, 420), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, BackColor = Color.White };

            ListBox lstSeps = new ListBox() { Location = new Point(20, 20), Size = new Size(220, 260), AllowDrop = true, Font = new Font("Segoe UI", 11f) };
            lstSeps.Items.AddRange(File.ReadAllLines(separatorsFile));

            TextBox txtInput = new TextBox() { Location = new Point(255, 20), Size = new Size(130, 27), Font = new Font("Segoe UI", 11f) };
            Button btnAdd = new Button() { Text = "Add", Location = new Point(255, 55), Size = new Size(130, 32), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10f) };
            Button btnRemove = new Button() { Text = "Remove", Location = new Point(255, 95), Size = new Size(130, 32), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10f) };
            Button btnRestore = new Button() { Text = "Restore Defaults", Location = new Point(255, 135), Size = new Size(130, 32), FlatStyle = FlatStyle.Flat, ForeColor = brandBlue, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };

            Label lblHint = new Label() { Text = "💡 Drag items to reorder them.", Location = new Point(18, 285), AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 9.5f) };

            Button btnUp = new Button() { Text = "Move Up ▲", Location = new Point(255, 205), Size = new Size(130, 32), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10f) };
            Button btnDown = new Button() { Text = "Move Down ▼", Location = new Point(255, 245), Size = new Size(130, 32), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10f) };

            Button btnClose = new Button() { Text = "CLOSE", Location = new Point(20, 315), Size = new Size(365, 45), BackColor = brandBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11f, FontStyle.Bold) };
            btnClose.FlatAppearance.BorderSize = 0;

            Action SaveListToFile = () => {
                string[] newSeps = new string[lstSeps.Items.Count];
                lstSeps.Items.CopyTo(newSeps, 0);
                File.WriteAllLines(separatorsFile, newSeps);
                LoadSeparators();
            };

            lstSeps.SelectedIndexChanged += (s, e) => { if (lstSeps.SelectedIndex >= 0) txtInput.Text = lstSeps.SelectedItem.ToString(); };

            btnAdd.Click += (s, e) => {
                if (!string.IsNullOrWhiteSpace(txtInput.Text))
                {
                    lstSeps.Items.Add(txtInput.Text);
                    txtInput.Clear();
                    SaveListToFile();
                }
            };

            btnRemove.Click += (s, e) => {
                if (lstSeps.SelectedIndex >= 0)
                {
                    txtInput.Clear();
                    lstSeps.Items.RemoveAt(lstSeps.SelectedIndex);
                    SaveListToFile();
                }
            };

            btnRestore.Click += (s, e) => {
                if (MessageBox.Show("This will reset your list to the Mitraphix defaults. Continue?", "Restore Defaults", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    File.WriteAllLines(separatorsFile, defaultSeparators);
                    lstSeps.Items.Clear();
                    lstSeps.Items.AddRange(defaultSeparators);
                    txtInput.Clear();
                    LoadSeparators();
                }
            };

            btnUp.Click += (s, e) => {
                if (lstSeps.SelectedIndex > 0)
                {
                    int i = lstSeps.SelectedIndex; object item = lstSeps.Items[i];
                    lstSeps.Items.RemoveAt(i); lstSeps.Items.Insert(i - 1, item); lstSeps.SelectedIndex = i - 1;
                    SaveListToFile();
                }
            };

            btnDown.Click += (s, e) => {
                if (lstSeps.SelectedIndex >= 0 && lstSeps.SelectedIndex < lstSeps.Items.Count - 1)
                {
                    int i = lstSeps.SelectedIndex; object item = lstSeps.Items[i];
                    lstSeps.Items.RemoveAt(i); lstSeps.Items.Insert(i + 1, item); lstSeps.SelectedIndex = i + 1;
                    SaveListToFile();
                }
            };

            lstSeps.MouseDown += (s, e) => { if (lstSeps.SelectedItem != null) lstSeps.DoDragDrop(lstSeps.SelectedItem, DragDropEffects.Move); };
            lstSeps.DragOver += (s, e) => { e.Effect = DragDropEffects.Move; };
            lstSeps.DragDrop += (s, e) => {
                Point point = lstSeps.PointToClient(new Point(e.X, e.Y));
                int index = lstSeps.IndexFromPoint(point);
                if (index < 0) index = lstSeps.Items.Count - 1;
                string data = (string)e.Data.GetData(typeof(string));
                if (data != null)
                {
                    lstSeps.Items.Remove(data);
                    lstSeps.Items.Insert(index, data);
                    lstSeps.SelectedIndex = index;
                    SaveListToFile();
                }
            };

            btnClose.Click += (s, e) => { sepForm.Close(); };

            sepForm.Controls.AddRange(new Control[] { lstSeps, txtInput, btnAdd, btnRemove, btnRestore, lblHint, btnUp, btnDown, btnClose });
            sepForm.ShowDialog();
        }

        private void HandleBulkImport()
        {
            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Text Files|*.txt|CSV Files|*.csv" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var lines = File.ReadAllLines(ofd.FileName);
                    foreach (var line in lines) { if (!string.IsNullOrWhiteSpace(line)) Directory.CreateDirectory(Path.Combine(targetPath, line.Trim())); }
                    MessageBox.Show("Folders Created Successfully.", "Mitraphix");
                }
            }
        }

        private void RunGeneration()
        {
            string[] names = txtNames.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (names.Length == 0) { MessageBox.Show("Please enter at least one folder name.", "Notice"); return; }

            string sep = cmbSep.SelectedItem.ToString();
            if (sep == "None") sep = "";
            else if (sep == "Space ( )") sep = " ";

            try
            {
                foreach (string rawName in names)
                {
                    string baseName = rawName.Trim();
                    if (cmbStyle.SelectedIndex == 0)
                    {
                        Directory.CreateDirectory(Path.Combine(targetPath, baseName));
                    }
                    else
                    {
                        for (int i = (int)numStart.Value; i <= (int)numEnd.Value; i++)
                        {
                            string pre = GetFormattedPreNum(i);
                            string mainNum = GetFormattedMainNum(i);
                            string fName = (pre + baseName + sep + mainNum).Trim();
                            Directory.CreateDirectory(Path.Combine(targetPath, fName));
                        }
                    }
                }
                Application.Exit();
            }
            catch (Exception ex) { MessageBox.Show("Error:\n" + ex.Message, "Mitraphix Error"); }
        }

        private string GetFormattedPreNum(int i)
        {
            if (cmbPreNum.SelectedIndex == 0) return "";
            string n = (cmbPreNum.SelectedIndex > 3) ? i.ToString("D2") : i.ToString();
            if (cmbPreNum.Text.Contains("()")) return $"({n}) ";
            if (cmbPreNum.Text.Contains(")")) return $"{n}) ";
            return $"{n}. ";
        }

        private string GetFormattedMainNum(int i)
        {
            switch (cmbStyle.SelectedIndex)
            {
                case 1: return i.ToString();
                case 2: return i.ToString("D2");
                case 3: return ToRoman(i);
                case 4: return ((char)(64 + i)).ToString();
                case 5: return ((char)(96 + i)).ToString();
                default: return "";
            }
        }

        private string ToRoman(int n)
        {
            var m = new Dictionary<int, string> { { 1000, "M" }, { 900, "CM" }, { 500, "D" }, { 400, "CD" }, { 100, "C" }, { 90, "XC" }, { 50, "L" }, { 40, "XL" }, { 10, "X" }, { 9, "IX" }, { 5, "V" }, { 4, "IV" }, { 1, "I" } };
            string r = ""; foreach (var kv in m) { while (n >= kv.Key) { r += kv.Value; n -= kv.Key; } }
            return r;
        }
    }
}