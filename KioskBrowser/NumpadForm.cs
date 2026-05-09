using System;
using System.Drawing;
using System.Windows.Forms;

namespace KioskBrowser
{
    /// <summary>
    /// 暗証番号入力用テンキーウィンドウ。
    /// 4桁に達した瞬間に自動照合し、一致すれば DialogResult.OK で閉じる。
    /// 不一致の場合はエラー表示して入力欄をクリアし、再入力を促す。
    /// </summary>
    public partial class NumpadForm : Form
    {
        // 正解の暗証番号（外から渡される）
        private readonly string _correctPin;

        // 現在の入力文字列（最大4桁）
        private string _inputPin = "";

        // --- UIコントロール ---
        private Label _displayLabel;   // 入力状態を●で表示
        private Label _errorLabel;     // エラーメッセージ表示
        private Button[] _numButtons;  // 0〜9 の数字ボタン配列
        private Button _backspaceBtn;  // バックスペースボタン
        private Button _cancelBtn;     // キャンセルボタン

        // ====================================================================
        // コンストラクタ
        // ====================================================================

        /// <param name="correctPin">照合対象の暗証番号</param>
        public NumpadForm(string correctPin)
        {
            _correctPin = correctPin;
            InitializeComponent();
            BuildUI();
        }

        // ====================================================================
        // UI構築
        // ====================================================================

        /// <summary>
        /// テンキーUIをコードで生成する
        /// </summary>
        private void BuildUI()
        {
            this.Text            = "終了確認";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.Size            = new Size(290, 450);
            this.BackColor       = Color.FromArgb(245, 245, 245);

            // --- 説明ラベル ---
            var titleLabel = new Label();
            titleLabel.Text      = "暗証番号を入力してください";
            titleLabel.Font      = new Font("Yu Gothic UI", 10f);
            titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            titleLabel.Size      = new Size(250, 28);
            titleLabel.Location  = new Point(20, 15);
            this.Controls.Add(titleLabel);

            // --- 入力表示欄（●で伏せて表示） ---
            _displayLabel = new Label();
            _displayLabel.Text        = "";
            _displayLabel.Font        = new Font("Arial", 26f, FontStyle.Bold);
            _displayLabel.TextAlign   = ContentAlignment.MiddleCenter;
            _displayLabel.Size        = new Size(250, 54);
            _displayLabel.Location    = new Point(20, 50);
            _displayLabel.BorderStyle = BorderStyle.FixedSingle;
            _displayLabel.BackColor   = Color.White;
            this.Controls.Add(_displayLabel);

            // --- エラーメッセージラベル ---
            _errorLabel = new Label();
            _errorLabel.Text      = "";
            _errorLabel.ForeColor = Color.Crimson;
            _errorLabel.Font      = new Font("Yu Gothic UI", 9f);
            _errorLabel.TextAlign = ContentAlignment.MiddleCenter;
            _errorLabel.Size      = new Size(250, 22);
            _errorLabel.Location  = new Point(20, 110);
            this.Controls.Add(_errorLabel);

            // --- テンキーボタン配置 ---
            // レイアウト：3列 × 4行
            //  [ 1 ][ 2 ][ 3 ]
            //  [ 4 ][ 5 ][ 6 ]
            //  [ 7 ][ 8 ][ 9 ]
            //  [ ← ][ 0 ][ C ]

            const int btnW   = 72;
            const int btnH   = 58;
            const int startX = 20;
            const int startY = 140;
            const int gapX   = 9;
            const int gapY   = 8;

            _numButtons = new Button[10];

            // 1〜9
            for (int i = 1; i <= 9; i++)
            {
                int row = (i - 1) / 3;
                int col = (i - 1) % 3;

                var btn = CreateDigitButton(i.ToString());
                btn.Location = new Point(startX + col * (btnW + gapX),
                                         startY + row * (btnH + gapY));
                btn.Click       += NumButton_Click;
                _numButtons[i]   = btn;
                this.Controls.Add(btn);
            }

            // バックスペースボタン（4行目 左）
            _backspaceBtn           = CreateActionButton("←", Color.FromArgb(180, 140, 60));
            _backspaceBtn.Location  = new Point(startX, startY + 3 * (btnH + gapY));
            _backspaceBtn.Click    += BackspaceBtn_Click;
            this.Controls.Add(_backspaceBtn);

            // 0ボタン（4行目 中央）
            _numButtons[0]          = CreateDigitButton("0");
            _numButtons[0].Location = new Point(startX + (btnW + gapX), startY + 3 * (btnH + gapY));
            _numButtons[0].Click   += NumButton_Click;
            this.Controls.Add(_numButtons[0]);

            // キャンセルボタン（4行目 右）
            _cancelBtn          = CreateActionButton("C", Color.FromArgb(110, 110, 110));
            _cancelBtn.Location = new Point(startX + 2 * (btnW + gapX), startY + 3 * (btnH + gapY));
            _cancelBtn.Click   += CancelBtn_Click;
            this.Controls.Add(_cancelBtn);
        }

        /// <summary>数字ボタンを生成する</summary>
        private Button CreateDigitButton(string text)
        {
            var btn = new Button();
            btn.Text      = text;
            btn.Size      = new Size(72, 58);
            btn.Font      = new Font("Arial", 20f, FontStyle.Bold);
            btn.BackColor = Color.FromArgb(65, 125, 185);
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Cursor    = Cursors.Hand;
            return btn;
        }

        /// <summary>機能ボタン（←・C）を生成する</summary>
        private Button CreateActionButton(string text, Color backColor)
        {
            var btn = new Button();
            btn.Text      = text;
            btn.Size      = new Size(72, 58);
            btn.Font      = new Font("Arial", 16f, FontStyle.Bold);
            btn.BackColor = backColor;
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Cursor    = Cursors.Hand;
            return btn;
        }

        // ====================================================================
        // イベントハンドラ
        // ====================================================================

        /// <summary>
        /// 数字ボタン押下：1文字追加し、4桁に達したら即照合
        /// </summary>
        private void NumButton_Click(object sender, EventArgs e)
        {
            if (_inputPin.Length >= 4) return;  // 4桁を超えない

            _inputPin      += ((Button)sender).Text;
            _errorLabel.Text = "";
            RefreshDisplay();

            // 4桁到達で自動照合
            if (_inputPin.Length == 4)
                VerifyPin();
        }

        /// <summary>
        /// バックスペース押下：末尾1文字を削除する
        /// </summary>
        private void BackspaceBtn_Click(object sender, EventArgs e)
        {
            if (_inputPin.Length > 0)
            {
                _inputPin        = _inputPin.Substring(0, _inputPin.Length - 1);
                _errorLabel.Text = "";
                RefreshDisplay();
            }
        }

        /// <summary>
        /// キャンセルボタン押下：ダイアログをキャンセルで閉じる
        /// </summary>
        private void CancelBtn_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        // ====================================================================
        // 内部処理
        // ====================================================================

        /// <summary>
        /// 入力状態に合わせて表示欄を更新する（入力文字数分の●を表示）
        /// </summary>
        private void RefreshDisplay()
        {
            _displayLabel.Text = new string('●', _inputPin.Length);
        }

        /// <summary>
        /// 入力された暗証番号を照合する。
        /// 一致：OK で閉じる / 不一致：エラー表示して入力欄をクリア
        /// </summary>
        private void VerifyPin()
        {
            if (_inputPin == _correctPin)
            {
                // 暗証番号一致：呼び出し元に OK を返して閉じる
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                // 暗証番号不一致：入力欄をクリアしてエラー表示
                _inputPin         = "";
                RefreshDisplay();
                _errorLabel.Text  = "暗証番号が違います。もう一度入力してください。";
            }
        }
    }
}
