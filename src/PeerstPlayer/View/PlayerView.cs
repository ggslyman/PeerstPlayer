﻿using PeerstLib.Form;
using PeerstPlayer.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PeerstPlayer
{
	// 動画プレイヤーの表示
	public partial class PlayerView : Form
	{
		public PlayerView()
		{
			InitializeComponent();

			// TODO デバッグ開始
			pecaPlayer.Open("http://localhost:7145/pls/90E13182A11873DF1B8ADD5F4E7C0A38?tip=183.181.158.208:7154");
			pecaPlayer.Size = pecaPlayer.Size;

			// 最小化ボタン
			minToolStripButton.Click += (sender, e) =>
			{
				this.WindowState = FormWindowState.Minimized;
			};
			// 最大化ボタン
			maxToolStripButton.Click += (sender, e) =>
			{
				if (this.WindowState == FormWindowState.Normal)
				{
					this.WindowState = FormWindowState.Maximized;
				}
				else
				{
					this.WindowState = FormWindowState.Normal;
				}
			};
			// 閉じるボタン
			closeToolStripButton.Click += (sender, e) =>
			{
				this.Close();
			};
			// マウスドラッグ
			pecaPlayer.MouseDownEvent = (sender, e) =>
			{
				FormUtility.WindowDragStart(this.Handle);
			};
			// 選択スレッドをクリック
			statusBar.SelectThreadClick = (sender, e) =>
			{
				ThreadSelectView view = new ThreadSelectView();
				view.Show();
			};
			// TODO デバッグ終了
		}
	}
}
