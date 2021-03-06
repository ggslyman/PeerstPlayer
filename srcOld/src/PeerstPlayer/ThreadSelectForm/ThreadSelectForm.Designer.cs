﻿namespace PeerstPlayer
{
	partial class ThreadSelectForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ThreadSelectForm));
			this.textBoxThreadUrl = new System.Windows.Forms.TextBox();
			this.buttonUpdate = new System.Windows.Forms.Button();
			this.listViewThread = new System.Windows.Forms.ListView();
			this.columnHeaderNo = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeaderThreadTitle = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeaderResCount = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeaderSpeed = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.SuspendLayout();
			// 
			// textBoxThreadUrl
			// 
			this.textBoxThreadUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxThreadUrl.Location = new System.Drawing.Point(0, 0);
			this.textBoxThreadUrl.Name = "textBoxThreadUrl";
			this.textBoxThreadUrl.Size = new System.Drawing.Size(470, 19);
			this.textBoxThreadUrl.TabIndex = 0;
			this.textBoxThreadUrl.Text = "http://";
			this.textBoxThreadUrl.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxThreadUrl_KeyDown);
			// 
			// buttonUpdate
			// 
			this.buttonUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonUpdate.Location = new System.Drawing.Point(470, -1);
			this.buttonUpdate.Name = "buttonUpdate";
			this.buttonUpdate.Size = new System.Drawing.Size(75, 20);
			this.buttonUpdate.TabIndex = 1;
			this.buttonUpdate.Text = "更新";
			this.buttonUpdate.UseVisualStyleBackColor = true;
			this.buttonUpdate.Click += new System.EventHandler(this.buttonUpdate_Click);
			// 
			// listViewThread
			// 
			this.listViewThread.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.listViewThread.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderNo,
            this.columnHeaderThreadTitle,
            this.columnHeaderResCount,
            this.columnHeaderSpeed});
			this.listViewThread.FullRowSelect = true;
			this.listViewThread.GridLines = true;
			this.listViewThread.Location = new System.Drawing.Point(0, 21);
			this.listViewThread.MultiSelect = false;
			this.listViewThread.Name = "listViewThread";
			this.listViewThread.Size = new System.Drawing.Size(544, 200);
			this.listViewThread.TabIndex = 2;
			this.listViewThread.UseCompatibleStateImageBehavior = false;
			this.listViewThread.View = System.Windows.Forms.View.Details;
			this.listViewThread.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listViewThread_MouseDoubleClick);
			// 
			// columnHeaderNo
			// 
			this.columnHeaderNo.Text = "No";
			// 
			// columnHeaderThreadTitle
			// 
			this.columnHeaderThreadTitle.Text = "スレッドタイトル";
			// 
			// columnHeaderResCount
			// 
			this.columnHeaderResCount.Text = "レス数";
			// 
			// columnHeaderSpeed
			// 
			this.columnHeaderSpeed.Text = "勢い";
			// 
			// ThreadSelectForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(544, 222);
			this.Controls.Add(this.listViewThread);
			this.Controls.Add(this.buttonUpdate);
			this.Controls.Add(this.textBoxThreadUrl);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "ThreadSelectForm";
			this.Text = "スレッド選択";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ThreadSelectForm_FormClosing);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox textBoxThreadUrl;
		private System.Windows.Forms.Button buttonUpdate;
		private System.Windows.Forms.ListView listViewThread;
		private System.Windows.Forms.ColumnHeader columnHeaderNo;
		private System.Windows.Forms.ColumnHeader columnHeaderThreadTitle;
		private System.Windows.Forms.ColumnHeader columnHeaderResCount;
		private System.Windows.Forms.ColumnHeader columnHeaderSpeed;
	}
}