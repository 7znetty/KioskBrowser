namespace KioskBrowser
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>
        /// フォームの基本プロパティを設定する。コントロールは MainForm.cs 側で動的に追加する。
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize          = new System.Drawing.Size(1280, 720);
            this.Name                = "MainForm";
            this.Text                = "KioskBrowser";

            // フォームロードイベント
            this.Load += new System.EventHandler(this.MainForm_Load);

            this.ResumeLayout(false);
        }
    }
}
