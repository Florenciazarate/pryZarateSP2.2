namespace pryZarateSP2._2
{
    partial class frmPrincipal
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.txtLog = new System.Windows.Forms.TextBox();
            this.btnMigrar = new System.Windows.Forms.Button();
            this.SuspendLayout();

            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.Location = new System.Drawing.Point(12, 51);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(776, 387);
            this.txtLog.TabIndex = 0;

            this.btnMigrar.Location = new System.Drawing.Point(12, 12);
            this.btnMigrar.Name = "btnMigrar";
            this.btnMigrar.Size = new System.Drawing.Size(120, 30);
            this.btnMigrar.TabIndex = 1;
            this.btnMigrar.Text = "Migrar Datos";
            this.btnMigrar.UseVisualStyleBackColor = true;
            this.btnMigrar.Click += new System.EventHandler(this.btnMigrar_Click);

            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.LightSteelBlue;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.btnMigrar);
            this.Controls.Add(this.txtLog);
            this.Name = "frmPrincipal";
            this.Text = "Migrador - Distribuidora";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
    }
}