using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using System.Threading.Tasks;

namespace KioskBrowser
{
    /// <summary>
    /// メインのキオスクブラウザ画面。
    /// WebView2 でフルスクリーン表示し、×ボタン用オーバーレイフォームを別ウィンドウとして管理する。
    /// WebView2 は HWND ベースのコントロールのため、通常の z-order では WinForms コントロールより
    /// 前面に出てしまう。そのため×ボタンは別の TopMost フォームとして表示する。
    /// </summary>
    public partial class MainForm : Form
    {
        private AppConfig _config;
        private WebView2 _webView;

        // ×ボタンを表示する専用オーバーレイフォーム（WebView2 の z-order 問題を回避）
        private Form _closeOverlay;

        // ====================================================================
        // Win32 API：タスクバーの表示/非表示
        // ====================================================================
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        // オーバーレイを確実に最前面へ固定するための SetWindowPos
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE     = 0x0002;
        private const uint SWP_NOSIZE     = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 1;

        // ====================================================================
        // コンストラクタ
        // ====================================================================
        public MainForm()
        {
            // 設定ファイルを読み込む（存在しなければデフォルトで作成）
            _config = ConfigManager.Load();
            InitializeComponent();
            ApplyKioskSettings();
        }

        // ====================================================================
        // キオスクモード設定
        // ====================================================================

        /// <summary>
        /// フォームをフルスクリーン・ボーダーレス・前面固定に設定する
        /// </summary>
        private void ApplyKioskSettings()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState     = FormWindowState.Maximized;
            this.TopMost         = true;
        }

        /// <summary>
        /// タスクバー（Shell_TrayWnd）を非表示にする
        /// </summary>
        private void HideTaskbar()
        {
            IntPtr hTaskbar = FindWindow("Shell_TrayWnd", null);
            if (hTaskbar != IntPtr.Zero)
                ShowWindow(hTaskbar, SW_HIDE);
        }

        /// <summary>
        /// タスクバーを再表示する（アプリ終了前に呼び出す）
        /// </summary>
        private void ShowTaskbar()
        {
            IntPtr hTaskbar = FindWindow("Shell_TrayWnd", null);
            if (hTaskbar != IntPtr.Zero)
                ShowWindow(hTaskbar, SW_SHOW);
        }

        // ====================================================================
        // フォームロード
        // ====================================================================

        /// <summary>
        /// フォームロード：WebView2 を非同期初期化してURLを開く。
        /// ×ボタンのオーバーレイは WebView2 の HWND が完全に生成された後に表示する。
        /// そうしないと WebView2 初期化後に z-order が逆転して隠れてしまう。
        /// </summary>
        private async void MainForm_Load(object sender, EventArgs e)
        {
            // タスクバーを非表示にする
            HideTaskbar();

            // --- WebView2 を配置 ---
            _webView      = new WebView2();
            _webView.Dock = DockStyle.Fill;
            this.Controls.Add(_webView);

            // --- WebView2 の非同期初期化 ---
            try
            {
                await _webView.EnsureCoreWebView2Async(null);

                // キオスク用セキュリティ設定：右クリックメニューと開発者ツールを無効化
                _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                _webView.CoreWebView2.Settings.AreDevToolsEnabled            = false;
                _webView.CoreWebView2.Settings.IsStatusBarEnabled            = false;

                // 設定ファイルで指定したURLへナビゲート
                _webView.Source = new Uri(_config.StartUrl);
            }
            catch (Exception ex)
            {
                // WebView2 Runtime 未インストール等の場合
                MessageBox.Show(
                    "WebView2 の初期化に失敗しました。\n" +
                    "Microsoft Edge WebView2 Runtime がインストールされていることを確認してください。\n\n" +
                    ex.Message,
                    "初期化エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                // WebView2 の HWND 生成完了後にオーバーレイを表示する。
                // ここで作ることで、WebView2 の上に確実に重なる。
                CreateCloseButtonOverlay();
            }
        }

        // ====================================================================
        // ×ボタン オーバーレイ
        // ====================================================================

        /// <summary>
        /// 画面右上に表示する×ボタン専用の小さなフォームを生成する。
        /// Show(this) でオーナー指定することで「オーナー付きウィンドウは常にオーナーより前面」
        /// というWindowsの規則を利用し、WebView2 を含む MainForm より確実に前面に出る。
        /// さらに SetWindowPos で HWND_TOPMOST を明示的に設定して二重に保証する。
        /// </summary>
        private void CreateCloseButtonOverlay()
        {
            _closeOverlay = new Form();
            _closeOverlay.FormBorderStyle = FormBorderStyle.None;
            _closeOverlay.Size            = new Size(62, 62);
            _closeOverlay.TopMost         = true;
            _closeOverlay.ShowInTaskbar   = false;
            _closeOverlay.StartPosition   = FormStartPosition.Manual;

            // 背景を透明にする（TransparencyKey で抜き色を指定）
            _closeOverlay.BackColor       = Color.Fuchsia;
            _closeOverlay.TransparencyKey = Color.Fuchsia;

            // 画面右上に配置
            Rectangle screen = Screen.FromControl(this).Bounds;
            _closeOverlay.Location = new Point(screen.Right - 72, screen.Top + 10);

            // ×ボタン本体
            var btn = new Button();
            btn.Text      = "×";
            btn.Size      = new Size(52, 52);
            btn.Location  = new Point(5, 5);
            btn.Font      = new Font("Arial", 20f, FontStyle.Bold);
            btn.BackColor = Color.FromArgb(210, 70, 70);
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Cursor    = Cursors.Hand;
            btn.Click    += CloseButton_Click;
            _closeOverlay.Controls.Add(btn);

            // Show(this) でオーナー設定 → オーナー付きウィンドウは常にオーナーより前面に表示される。
            // オーナー関係により MainForm 終了時にオーバーレイも自動で閉じる（手動 Close 不要）。
            _closeOverlay.Show(this);

            // SetWindowPos で HWND_TOPMOST を強制設定（TopMost プロパティの二重保険）
            SetWindowPos(_closeOverlay.Handle, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        /// <summary>
        /// ×ボタン押下：テンキーダイアログを表示してPINを照合する
        /// </summary>
        private void CloseButton_Click(object sender, EventArgs e)
        {
            // テンキー表示中はオーバーレイを一時的に非前面にしてテンキーを前面に出す
            _closeOverlay.TopMost = false;

            using (var numpad = new NumpadForm(_config.ExitPin))
            {
                numpad.TopMost       = true;
                numpad.StartPosition = FormStartPosition.CenterScreen;

                if (numpad.ShowDialog(this) == DialogResult.OK)
                {
                    // 暗証番号一致：タスクバーを復元してアプリを終了する
                    ShowTaskbar();
                    Application.Exit();
                    return;
                }
            }

            // PIN 不一致でキャンセル：オーバーレイを再び前面に戻す
            _closeOverlay.TopMost = true;
        }

        // ====================================================================
        // キーボードショートカット無効化
        // ====================================================================

        /// <summary>
        /// Alt+F4・Alt+Tab・Win+D などのショートカットキーを無効化する
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Alt | Keys.F4:
                case Keys.Alt | Keys.Tab:
                //case Keys.LWin:
                //case Keys.RWin:
                //case Keys.LWin | Keys.D:  // 0x5B | 0x44 = 0x5F（LWin と値が異なるため有効）
                case Keys.Escape:
                case Keys.Control | Keys.Escape:  // スタートメニュー
                    return true;  // イベントを消費（既定の動作をキャンセル）
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        /// ユーザー操作による直接終了を禁止する（PIN入力からのみ終了可能）
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                return;
            }

            // Application.Exit() 等によるプログラム的な終了時はタスクバーを復元
            ShowTaskbar();
            base.OnFormClosing(e);
        }
    }
}
