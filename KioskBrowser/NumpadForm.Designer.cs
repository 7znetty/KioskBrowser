namespace KioskBrowser
{
    partial class NumpadForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>
        /// フォームの基本プロパティのみ設定する。コントロールは NumpadForm.cs 側で動的に追加する。
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize          = new System.Drawing.Size(290, 450);
            this.Name                = "NumpadForm";
            this.Text                = "終了確認";

            this.ResumeLayout(false);
        }
    }
}
