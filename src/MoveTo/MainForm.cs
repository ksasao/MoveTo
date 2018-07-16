using System;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics.Contracts;

namespace MoveTo
{
    public partial class MainForm : Form
    {
        string rootFolder = "";
        string destFolderName = "";
        TargetController targets = null;

        public MainForm()
        {
            InitializeComponent();

            ReadDefaultSettings();
            LoadTargets();
            SetupForm();
        }

        private void LoadTargets()
        {
            // ドラッグ＆ドロップされたファイル・フォルダ名一覧を取得する
            string[] args = Environment.GetCommandLineArgs();
            string[] commands = new string[0];
            if (args.Length > 1)
            {
                commands = new string[args.Length - 1];
                Array.Copy(args, 1, commands, 0, commands.Length);
            }
            this.targets = new TargetController(commands);
        }

        private void SetupForm()
        {
            // テキストボックス内の表示内容を決定
            DateTime date;
            if (this.targets.FileCount == 0)
            {
                // ファイル・フォルダが指定されていないときには現在の時刻をフォルダ名に
                date = DateTime.Now;
            }
            else
            {
                // 指定されている場合には、最新のファイル・フォルダの最終更新時刻をフォルダ名に
                date = this.targets.LatestTargetInfo.LastWriteTime;
            }
            string header = date.ToString("yyyyMMdd") + "_";
            string name = this.targets.LatestTargetName == "" ? "名称未設定" : this.targets.LatestTargetName;

            this.textBoxFolderName.Text = header + name;
            this.textBoxFolderName.SelectionStart = header.Length;
            this.textBoxFolderName.SelectionLength = name.Length;

            // カーソル位置にウィンドウを移動
            this.Left = Cursor.Position.X - 100;
            this.Top = Cursor.Position.Y - 10;

            UpdateFormText();
        }

        /// <summary>
        /// デフォルト設定の読み出し
        /// </summary>
        private void ReadDefaultSettings()
        {
            // ウィンドウサイズの読み込み
            this.Size = Properties.Settings.Default.Size;
            if (this.Width < 50) this.Width = 50;
            if (this.Height < 50) this.Height = 50;

            // 移動先フォルダの読み込み
            this.rootFolder = Properties.Settings.Default.SelectedFolder.Trim();
            if (string.IsNullOrEmpty(rootFolder) || !Directory.Exists(rootFolder))
            {
                this.rootFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            }
        }

        // ウィンドウ名の更新
        private void UpdateFormText()
        {
            string root = this.rootFolder;
            if (root == null) return;

            string trim = AdjustFolderName();

            if (trim == "") return;

            destFolderName = Path.Combine(root, trim);
            this.Text = destFolderName + " 以下へ移動";
        }

        // フォルダ名の調整
        private string AdjustFolderName()
        {
            Contract.Ensures(Contract.Result<string>() != null);

            string name = this.textBoxFolderName.Text;
            if (name == null) name = "";

            // \ で始まるフォルダ名は NG なので除去
            while (name.Length > 0 && name[0] == '\\')
            {
                name = name.Substring(1);
                this.textBoxFolderName.Text = name;
                this.textBoxFolderName.Select(0, 0);
            }

            // フォルダ名の前後に空白を挿入しようとしたときに
            // カーソルが動かないようにするための処理
            string trimStart = name.TrimStart();
            string trim = trimStart.TrimEnd();
            if (trimStart != name)
            {
                this.textBoxFolderName.Text = trim;
                this.textBoxFolderName.Select(0, 0);
            }
            else if (trim != name)
            {
                this.textBoxFolderName.Text = trim;
                this.textBoxFolderName.Select(trim.Length, 0);
            }
            return trim;
        }


        // ファイル移動処理
        private void MoveFiles()
        {
            if (destFolderName == null) return;
            if (destFolderName.Length == 0) return;

            try
            {
                this.targets.MoveTo(destFolderName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "移動失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // フォームで右クリックしたときに表示されるメニューで移動先を変更
        private void toolStripMenuItemMoveTo_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialogSelectFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                rootFolder = folderBrowserDialogSelectFolder.SelectedPath;
                Properties.Settings.Default.SelectedFolder = rootFolder;
                Properties.Settings.Default.Save();
                UpdateFormText();
            }
        }

        // テキストボックスの表示内容に合わせてウィンドウのタイトル部を更新
        private void textBoxFolderName_TextChanged(object sender, EventArgs e)
        {
            UpdateFormText();
        }

        // エクスプローラで移動先を開く
        private void toolStripMenuItemOpenFolder_Click(object sender, EventArgs e)
        {
            Contract.Requires(this.rootFolder != null);

            System.Diagnostics.ProcessStartInfo p = new System.Diagnostics.ProcessStartInfo();
            p.FileName = this.rootFolder;
            p.Verb = "explore";
            System.Diagnostics.Process.Start(p);
        }

        // キー入力処理
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            UpdateFormText();

            // ESCキーが押されたら終了
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
            else // Enterキーが押されたら移動
                if (e.KeyCode == Keys.Enter && this.textBoxFolderName.Text != "")
                {
                    this.Hide();
                    MoveFiles();
                    this.Close();
                }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            Properties.Settings.Default.Size = this.Size;
            Properties.Settings.Default.Save();
        }

    }
}
