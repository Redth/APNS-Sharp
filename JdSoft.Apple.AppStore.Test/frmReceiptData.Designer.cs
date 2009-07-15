namespace JdSoft.Apple.AppStore.Test
{
	partial class frmReceiptData
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.textReceiptData = new System.Windows.Forms.TextBox();
			this.checkSandbox = new System.Windows.Forms.CheckBox();
			this.buttonPaste = new System.Windows.Forms.Button();
			this.buttonOk = new System.Windows.Forms.Button();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.textResults = new System.Windows.Forms.TextBox();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.textReceiptData);
			this.groupBox1.Controls.Add(this.checkSandbox);
			this.groupBox1.Location = new System.Drawing.Point(12, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(481, 199);
			this.groupBox1.TabIndex = 0;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Paste the Receipt Data Below (it should look like JSON) as you received it from t" +
				"he iPhone:";
			// 
			// textReceiptData
			// 
			this.textReceiptData.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.textReceiptData.Location = new System.Drawing.Point(5, 36);
			this.textReceiptData.Multiline = true;
			this.textReceiptData.Name = "textReceiptData";
			this.textReceiptData.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.textReceiptData.Size = new System.Drawing.Size(470, 157);
			this.textReceiptData.TabIndex = 0;
			// 
			// checkSandbox
			// 
			this.checkSandbox.AutoSize = true;
			this.checkSandbox.Location = new System.Drawing.Point(6, 19);
			this.checkSandbox.Name = "checkSandbox";
			this.checkSandbox.Size = new System.Drawing.Size(98, 17);
			this.checkSandbox.TabIndex = 3;
			this.checkSandbox.Text = "Sandbox Mode";
			this.checkSandbox.UseVisualStyleBackColor = true;
			// 
			// buttonPaste
			// 
			this.buttonPaste.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonPaste.Location = new System.Drawing.Point(344, 217);
			this.buttonPaste.Name = "buttonPaste";
			this.buttonPaste.Size = new System.Drawing.Size(149, 23);
			this.buttonPaste.TabIndex = 1;
			this.buttonPaste.Text = "Paste From Clipboard";
			this.buttonPaste.UseVisualStyleBackColor = true;
			this.buttonPaste.Click += new System.EventHandler(this.buttonPaste_Click);
			// 
			// buttonOk
			// 
			this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOk.Location = new System.Drawing.Point(344, 424);
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.Size = new System.Drawing.Size(149, 23);
			this.buttonOk.TabIndex = 2;
			this.buttonOk.Text = "Verify Receipt";
			this.buttonOk.UseVisualStyleBackColor = true;
			this.buttonOk.Click += new System.EventHandler(this.buttonOk_Click);
			// 
			// groupBox2
			// 
			this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox2.Controls.Add(this.textResults);
			this.groupBox2.Location = new System.Drawing.Point(12, 242);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(480, 172);
			this.groupBox2.TabIndex = 4;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Results:";
			// 
			// textResults
			// 
			this.textResults.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.textResults.Location = new System.Drawing.Point(6, 19);
			this.textResults.Multiline = true;
			this.textResults.Name = "textResults";
			this.textResults.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.textResults.Size = new System.Drawing.Size(468, 146);
			this.textResults.TabIndex = 0;
			// 
			// frmReceiptData
			// 
			this.AcceptButton = this.buttonOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(509, 462);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.buttonOk);
			this.Controls.Add(this.buttonPaste);
			this.Controls.Add(this.groupBox1);
			this.MinimumSize = new System.Drawing.Size(525, 500);
			this.Name = "frmReceiptData";
			this.Text = "Receipt Data";
			this.Load += new System.EventHandler(this.frmReceiptData_Load);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.TextBox textReceiptData;
		private System.Windows.Forms.Button buttonPaste;
		private System.Windows.Forms.Button buttonOk;
		private System.Windows.Forms.CheckBox checkSandbox;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.TextBox textResults;
	}
}

