﻿
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Shule.Peerst.Util;
using Shule.Peerst.BBS;
using Shule.Peerst.PeerCast;

namespace PeerstPlayer
{
	public partial class MainForm : Form, ThreadSelectObserver, WindowSizeMediator
	{
		/// <summary>
		/// 掲示板操作クラス
		/// </summary>
		OperationBbs operationBbs = new OperationBbs();

		/// <summary>
		/// ウィンドウサイズ管理
		/// </summary>
		WindowSizeManager windowSizeManager = null;

		/// <summary>
		/// チャンネル情報
		/// </summary>
		public ChannelInfo channelInfo
		{
			get
			{
				if (pecaManager.ChannelInfo == null)
				{
					return new ChannelInfo();
				}
				else
				{
					return pecaManager.ChannelInfo;
				}
			}
		}

		/// <summary>
		/// WMP
		/// </summary>
		WMPEx wmp;

		/// <summary>
		/// XPであるか
		/// </summary>
		public bool WindowsXP = true;

		// スレッドビューワのプロセス
		Process ThreadViewerProcess = null;

		/// <summary>
		/// WMPパネルの大きさ
		/// </summary>
		public Size PanelWMPSize = new Size(320, 240);

		/// <summary>
		/// ツールチップが表示されているか
		/// </summary>
		public bool ToolStipVisile
		{
			get
			{
				if (toolStripDropDownButtonSize.Pressed || toolStripDropDownButtonDivide.Pressed)
				{
					return true;
				}

				return false;
			}
		}

		#region WMPパネルのサイズ

		/// <summary>
		/// WMPパネルのサイズ
		/// </summary>
		public Size WMPSize
		{
			get
			{
				return panelWMP.Size;
			}
		}

		#endregion

		#region スクリーンサイズ

		/// <summary>
		/// スクリーンサイズ
		/// </summary>
		Rectangle Screen
		{
			get
			{
				return System.Windows.Forms.Screen.GetWorkingArea(this);
			}
		}
	
		#endregion

		#region タイトルバー

		private enum SWP : int
		{
			NOSIZE = 0x0001,
			NOMOVE = 0x0002,
			NOZORDER = 0x0004,
			NOREDRAW = 0x0008,
			NOACTIVATE = 0x0010,
			FRAMECHANGED = 0x0020,
			SHOWWINDOW = 0x0040,
			HIDEWINDOW = 0x0080,
			NOCOPYBITS = 0x0100,
			NOOWNERZORDER = 0x0200,
			NOSENDCHANGING = 0x400
		}

		private const UInt32 WS_CAPTION = (UInt32)0x00C00000;
		private const int GWL_STYLE = -16;

		[DllImport("user32.dll")]
		private static extern UInt32 GetWindowLong(IntPtr hWnd, int index);
		[DllImport("user32.dll")]
		private static extern UInt32 SetWindowLong(IntPtr hWnd, int index, UInt32 unValue);
		[DllImport("user32.dll")]
		private static extern UInt32 SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int width, int height, SWP flags);

		bool titleBar = false;
		public bool TitleBar
		{
			get
			{
				return titleBar;
			}
			set
			{
				titleBar = value;

				// アンカーを解除
				PanelAnchor = false;

				if (value)
				{
					// タイトルバーを出す
					UInt32 style = GetWindowLong(this.Handle, GWL_STYLE);	// 現在のスタイルを取得
					style = (style | WS_CAPTION);							// キャプションのスタイルを削除
					SetWindowLong(Handle, GWL_STYLE, style);				// スタイルを反映
					SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, SWP.NOMOVE | SWP.NOSIZE | SWP.NOZORDER | SWP.FRAMECHANGED); // ウィンドウを再描画
				}
				else
				{
					// タイトルバーを消す
					UInt32 style = GetWindowLong(this.Handle, GWL_STYLE);	// 現在のスタイルを取得
					style = (style & ~WS_CAPTION);							// キャプションのスタイルを削除
					SetWindowLong(Handle, GWL_STYLE, style);				// スタイルを反映
					SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, SWP.NOMOVE | SWP.NOSIZE | SWP.NOZORDER | SWP.FRAMECHANGED); // ウィンドウを再描画
				}

				OnPanelSizeChange();
			}
		}

		#endregion

		#region フレーム

		/// <summary>
		/// フレーム
		/// </summary>
		public bool Frame
		{
			get
			{
				return (FormBorderStyle == FormBorderStyle.Sizable);
			}
			set
			{
				if (value)
				{
					FormBorderStyle = FormBorderStyle.Sizable;
					TitleBar = false;
				}
				else
				{
					FormBorderStyle = FormBorderStyle.None;
				}
			}
		}

		#endregion

		// コンストラクタ
		public MainForm()
		{
			// 初期化
			InitializeComponent();

			// WMP初期化
			wmp = new WMPEx(this);
			wmp.MouseUpEvent += new AxWMPLib._WMPOCXEvents_MouseUpEventHandler(wmp_MouseUpEvent);
			wmp.MouseMoveEvent += new AxWMPLib._WMPOCXEvents_MouseMoveEventHandler(wmp_MouseMoveEvent);
			wmp.MovieStart += new EventHandler(wmp_MovieStart);
			wmp.VolumeChange += new EventHandler(wmp_VolumeChange);
			wmp.Gesture += new EventHandlerString(wmp_Gesture);
			wmp.DurationChange += new EventHandler(wmp_DurationChange);

			// タイトルバー
			TitleBar = false;
			panelResBox.Visible = false;
			WindowsXP = IsWindowsXP();
		}

		/// <summary>
		/// コマンドから文字に変換する
		/// </summary>
		string CommandToString(string command)
		{
			switch (command)
			{
				case "Volume+1":
					return "ボリューム：+1";
				case "Volume-1":
					return "ボリューム：-1";
				case "Volume+5":
					return "ボリューム：+5";
				case "Volume-5":
					return "ボリューム：-5";
				case "Volume+10":
					return "ボリューム：+10";
				case "Volume-10":
					return "ボリューム：-10";
				case "balance-10":
					return "音量バランス：-10";
				case "balance+10":
					return "音量バランス：+10";
				case "Size+10%":
					return "サイズ：+10%";
				case "Size-10%":
					return "サイズ：-10%";
				case "Size+10":
					return "サイズ：+10";
				case "Size-10":
					return "サイズ：-10";
				case "Bump":
					return "再接続する";
				case "Close&RelayCut":
					return "リレーを切断して終了する";
				case "OpenThreadViewer":
					return "スレッドビューワを開く";
				case "Close":
					return "終了する";
				case "AspectRate":
					return "アスペクト比固定切り替え";
				case "TopMost":
					return "最前列表示切り替え";
				case "ResBox":
					return "レスボックスの表示切り替え";
				case "StatusLabel":
					return "ステータスラベルの表示切り替え";
				case "Frame":
					return "フレームの表示切り替え";
				case "Size=50%":
					return "サイズ：50%";
				case "Size=75%":
					return "サイズ：75%";
				case "Size=100%":
					return "サイズ：100%";
				case "Size=150%":
					return "サイズ：150%";
				case "Size=200%":
					return "サイズ：200%";
				case "Width=160":
					return "サイズ：幅160";
				case "Width=320":
					return "サイズ：幅320";
				case "Width=480":
					return "サイズ：幅480";
				case "Width=640":
					return "サイズ：幅640";
				case "Width=800":
					return "サイズ：幅800";
				case "Height=120":
					return "サイズ：高さ120";
				case "Height=240":
					return "サイズ：高さ240";
				case "Height=360":
					return "サイズ：高さ360";
				case "Height=480":
					return "サイズ：高さ480";
				case "Height=600":
					return "サイズ：高さ600";
				case "ScreenSplitWidth=5":
					return "サイズ：幅5分の1";
				case "ScreenSplitWidth=4":
					return "サイズ：幅4分の1";
				case "ScreenSplitWidth=3":
					return "サイズ：幅3分の1";
				case "ScreenSplitWidth=2":
					return "サイズ：幅2分の1";
				case "ScreenSplitHeight=5":
					return "サイズ：高さ5分の1";
				case "ScreenSplitHeight=4":
					return "サイズ：高さ4分の1";
				case "ScreenSplitHeight=3":
					return "サイズ：高さ3分の1";
				case "ScreenSplitHeight=2":
					return "サイズ：高さ2分の1";
				case "ScreenSplit=5":
					return "サイズ：5 x 5";
				case "ScreenSplit=4":
					return "サイズ：4 x 4";
				case "ScreenSplit=3":
					return "サイズ：3 x 3";
				case "ScreenSplit=2":
					return "サイズ：2 x 2";
				case "VolumeBalance-10":
					return "音量バランス：-10";
				case "VolumeBalance+10":
					return "音量バランス：+10";
				case "VolumeBalanceLeft":
					return "音量バランス：左";
				case "VolumeBalanceRight":
					return "音量バランス：右";
				case "VolumeBalanceMiddle":
					return "音量バランス：中央";
				case "Mute":
					return "音量：ミュート切り替え";
				case "FullScreen":
					return "サイズ：フルスクリーン切り替え";
				case "ChannelInfoUpdate":
					return "チャンネル情報を更新";
				case "OpenContextMenu":
					return "コンテキストメニューを表示";
				case "SelectResBox":
					return "レスボックスを選択";
				case "Mini&Mute":
					return "最小化ミュート";
				case "ScreenShot":
					return "スクリーンショット";
				case "OpenScreenShotFolder":
					return "スクリーンショットフォルダを開く";
				case "FitSizeMovie":
					return "動画サイズに合わせる";
				case "Retry":
					return "リトライ";
				case "OpenClipBoard":
					return "クリップボードから開く";
				case "OpenFile":
					return "ファイルを開く";
				case "WMPFullScreen":
					return "WMPフルスクリーン";
				default:
					return command;
			}
		}

		/// <summary>
		/// コマンドを実行
		/// </summary>
		public void ExeCommand(string command)
		{
			string value = "";
			switch (command)
			{
				case "Size=50%":
					windowSizeManager.SetScale(50);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Size=75%":
					windowSizeManager.SetScale(75);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Size=100%":
					windowSizeManager.SetScale(100);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Size=150%":
					windowSizeManager.SetScale(150);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Size=200%":
					windowSizeManager.SetScale(200);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Width=160":
					windowSizeManager.SetWidth(160);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Width=320":
					windowSizeManager.SetWidth(320);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Width=480":
					windowSizeManager.SetWidth(480);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Width=640":
					windowSizeManager.SetWidth(640);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Width=800":
					windowSizeManager.SetWidth(800);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Height=120":
					windowSizeManager.SetWidth(120);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Height=240":
					windowSizeManager.SetWidth(240);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Height=360":
					windowSizeManager.SetWidth(360);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Height=480":
					windowSizeManager.SetWidth(480);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Height=600":
					windowSizeManager.SetWidth(600);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "ScreenSplitWidth=5":
					windowSizeManager.ScreenSplitWidth(5);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "ScreenSplitWidth=4":
					windowSizeManager.ScreenSplitWidth(4);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "ScreenSplitWidth=3":
					windowSizeManager.ScreenSplitWidth(3);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "ScreenSplitWidth=2":
					windowSizeManager.ScreenSplitWidth(2);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "ScreenSplitHeight=5":
					windowSizeManager.ScreenSplitHeight(5);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "ScreenSplitHeight=4":
					windowSizeManager.ScreenSplitHeight(4);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "ScreenSplitHeight=3":
					windowSizeManager.ScreenSplitHeight(3);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "ScreenSplitHeight=2":
					windowSizeManager.ScreenSplitHeight(2);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "ScreenSplit=5":
					windowSizeManager.ScreenSplit(5);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "ScreenSplit=4":
					windowSizeManager.ScreenSplit(4);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "ScreenSplit=3":
					windowSizeManager.ScreenSplit(3);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "ScreenSplit=2":
					windowSizeManager.ScreenSplit(2);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Volume+1":
					wmp.Volume += 1;
					value = " (" + wmp.Volume + ")";
					break;
				case "Volume-1":
					wmp.Volume -= 1;
					value = " (" + wmp.Volume + ")";
					break;
				case "Volume+5":
					wmp.Volume += 5;
					value = " (" + wmp.Volume + ")";
					break;
				case "Volume-5":
					wmp.Volume -= 5;
					value = " (" + wmp.Volume + ")";
					break;
				case "Volume+10":
					wmp.Volume += 10;
					value = " (" + wmp.Volume + ")";
					break;
				case "Volume-10":
					wmp.Volume -= 10;
					value = " (" + wmp.Volume + ")";
					break;
				case "Bump":
					pecaManager.Bump();
					break;
				case "Close&RelayCut":
					try
					{
						pecaManager.DisconnectRelay();
						Visible = false;
						updateChannelInfoThread.Abort();
						Application.Exit();
					}
					catch
					{
					}
					break;
				case "VolumeBalance-10":
					wmp.settings.balance -= 10;
					value = " (" + wmp.settings.balance + ")";
					break;
				case "VolumeBalance+10":
					wmp.settings.balance += 10;
					value = " (" + wmp.settings.balance + ")";
					break;
				case "VolumeBalanceLeft":
					wmp.settings.balance = -100;
					break;
				case "VolumeBalanceRight":
					wmp.settings.balance = 100;
					break;
				case "VolumeBalanceMiddle":
					wmp.settings.balance = 0;
					break;
				case "Size+10%":
					{
						double SizePercent = ((int)(wmp.Width / (float)wmp.ImageWidth * 10 + 1 + 0.6)) / 10.0f;
						panelWMP.Size = new Size((int)(wmp.ImageWidth * SizePercent), (int)(wmp.ImageHeight * SizePercent));
						OnPanelSizeChange();
						value = " " + ((int)(SizePercent * 100)).ToString() + "% (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					}
					break;
				case "Size-10%":
					{
						double SizePercent = ((int)(wmp.Width / (float)wmp.ImageWidth * 10 - 1 + 0.6)) / 10.0f;
						panelWMP.Size = new Size((int)(wmp.ImageWidth * SizePercent), (int)(wmp.ImageHeight * SizePercent));
						OnPanelSizeChange();
						value = " " + ((int)(SizePercent * 100)).ToString() + "% (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					}
					break;
				case "Size+10":
					panelWMP.Width += 10;
					panelWMP.Height += 10;
					OnPanelSizeChange();
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Size-10":
					panelWMP.Width -= 10;
					panelWMP.Height -= 10;
					OnPanelSizeChange();
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "TopMost":
					TopMost = !TopMost;
					if (TopMost)
					{
						value = " (有効)";
					}
					else
					{
						value = " (無効)";
					}
					break;
				case "ResBox":
					panelResBox.Visible = !panelResBox.Visible;
					OnPanelSizeChange();
					if (panelResBox.Visible)
					{
						value = " (有効)";
					}
					else
					{
						value = " (無効)";
					}
					break;
				case "StatusLabel":
					panelStatusLabel.Visible = !panelStatusLabel.Visible;
					OnPanelSizeChange();
					if (panelStatusLabel.Visible)
					{
						value = " (有効)";
					}
					else
					{
						value = " (無効)";
					}
					break;
				case "Frame":
					Frame = !Frame;
					OnPanelSizeChange();
					if (Frame)
					{
						value = " (有効)";
					}
					else
					{
						value = " (無効)";
					}
					break;
				case "Mute":
					wmp.Mute = !wmp.Mute;
					if (wmp.Mute)
					{
						value = " (有効)";
					}
					else
					{
						value = " (無効)";
					}
					break;
				case "WMPFullScreen":
					try
					{
						wmp.fullScreen = true;
					}
					catch
					{
					}
					break;

				case "FullScreen":
					if (WindowState == FormWindowState.Maximized)
					{
						WindowState = FormWindowState.Normal;
						OnPanelSizeChange(PanelWMPSize);
						value = " (無効)";
					}
					else
					{
						PanelWMPSize = WMPSize;
						WindowState = FormWindowState.Maximized;
						value = " (有効)";
					}
					OnPanelSizeChange();
					break;
				case "OpenThreadViewer":
					スレッドビューワを開くToolStripMenuItem_Click(this, EventArgs.Empty);
					break;
				case "AspectRate":
					settings.AspectRate = !settings.AspectRate;
					if (settings.AspectRate)
					{
						value = " (有効)";
					}
					else
					{
						value = " (無効)";
					}
					break;
				case "ChannelInfoUpdate":
					ChannelInfoUpdate();
					break;
				case "Close":
					try
					{
						Visible = false;
						updateChannelInfoThread.Abort();
						Application.Exit();
					}
					catch
					{
					}
					break;
				case "OpenContextMenu":
					labelDetail.Text = CommandToString(command) + value;
					wmp.enableContextMenu = true;
					Win32API.SendMessage(wmp.Handle, Win32API.WM_CONTEXTMENU, new IntPtr(MousePosition.X), new IntPtr(MousePosition.Y));
					wmp.enableContextMenu = false;
					break;
				case "SelectResBox":
					if (!panelResBox.Visible)
					{
						panelResBox.Visible = true;
						OnPanelSizeChange();
					}
					resBox.Focus();
					break;
				case "Mini&Mute":
					WindowState = FormWindowState.Minimized;
					wmp.Mute = true;
					settings.MiniMute = true;
					break;
				case "ScreenShot":
					ScreenShot();
					break;
				case "OpenScreenShotFolder":
					OpenScreenShotFolder();
					break;
				// 動画サイズを合わせる
				case "FitSizeMovie":
					try
					{
						if ((wmp.Height / (float)wmp.Width) > wmp.AspectRate)
						{
							windowSizeManager.SetWidth(wmp.Width);
						}
						else
						{
							windowSizeManager.SetHeight(wmp.Height);
						}
					}
					catch
					{
					}
					break;

				// リトライ
				case "Retry":
					wmp.Retry(true);
					break;

				// クリップボードから開く
				case "OpenClipBoard":
					//クリップボードの文字列データを取得する
					IDataObject iData = Clipboard.GetDataObject();
					//クリップボードに文字列データがあるか確認
					if (iData.GetDataPresent(DataFormats.Text))
					{
						//文字列データがあるときはこれを取得する
						wmp.URL = (string)iData.GetData(DataFormats.Text);
					}
					break;

				// ファイルを開く
				case "OpenFile":
					OpenFileDialog ofd = new OpenFileDialog();
					ofd.ShowDialog();
					wmp.URL = ofd.FileName;
					break;
				default:
					return;
			}

			if (command != "OpenContextMenu")
			{
				labelDetail.Text = CommandToString(command) + value;
			}
		}

		/// <summary>
		/// スクリーンショットフォルダを開く
		/// </summary>
		private void OpenScreenShotFolder()
		{
			// フォルダパス
			string folderPath = GetCurrentDirectory() + "\\ScreenShot";

			// フォルダがなければ作る
			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}

			// フォルダを開く
			System.Diagnostics.Process.Start(folderPath);
		}

		/// <summary>
		/// スクリーンショットを撮る
		/// </summary>
		private void ScreenShot()
		{
			// ウィンドウレスモード有効
			wmp.windowlessVideo = true;

			// 現在の時間
			DateTime dtNow = DateTime.Now;

			// フォルダパス
			string folderPath = GetCurrentDirectory() + "\\ScreenShot";
			// ファイルパス
			string fileName = "【" + channelInfo.Name + "】" + dtNow.Year + "／" + dtNow.Month.ToString().PadLeft(2, '0') + "／" + dtNow.Day.ToString().PadLeft(2, '0') + "（" + dtNow.Hour.ToString().PadLeft(2, '0') + "：" + dtNow.Minute.ToString().PadLeft(2, '0') + "：" + dtNow.Second.ToString().PadLeft(2, '0') + "." + dtNow.Millisecond.ToString().PadLeft(3, '0') + "）.jpg";
			fileName = fileName.Replace('\\', ' ').Replace('/', ' ').Replace(':', ' ').Replace('*', ' ').Replace('?', ' ').Replace('"', ' ').Replace('<', ' ').Replace('>', ' ').Replace('|', ' ');
			string filePath = folderPath + "\\" + fileName;

			// フォルダがなければ作る
			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}

			// 表示してあるものを消す
			contextMenuStripResBox.Visible = false;
			toolStrip.Visible = false;

			// 再描画
			wmp.Refresh();

			{
				// スクリーン・キャプチャの範囲を設定
				Rectangle myRectangle = wmp.RectangleToScreen(wmp.Bounds);

				// Bitmapオブジェクトにスクリーン・キャプチャ
				Bitmap myBmp = new Bitmap(myRectangle.Width, myRectangle.Height, PixelFormat.Format32bppArgb);

				using (Graphics g = Graphics.FromImage(myBmp))
				{
					g.CopyFromScreen(myRectangle.X, myRectangle.Y, 0, 0,
					myRectangle.Size, CopyPixelOperation.SourceCopy);
				}

				// クリップボードにコピー
				Clipboard.SetDataObject(myBmp, false);

				// クリップボードに格納された画像の取得
				IDataObject data = Clipboard.GetDataObject();
				if (data.GetDataPresent(DataFormats.Bitmap))
				{
					Bitmap bmp = (Bitmap)data.GetData(DataFormats.Bitmap);
					// 取得した画像の保存
					bmp.Save(filePath, ImageFormat.Jpeg);
				}
			}

			// ウィンドウレスモード無効
			wmp.windowlessVideo = false;
		}

		/// <summary>
		/// WindowsXPであるか
		/// </summary>
		private bool IsWindowsXP()
		{
			System.OperatingSystem os = System.Environment.OSVersion;

			if (os.Platform == PlatformID.Win32NT)
			{
				if (os.Version.Major == 6)
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// コマンドラインからチャンネル名を取得
		/// </summary>
		private string GetChannelName()
		{
			if (Environment.GetCommandLineArgs().Length > 2)
			{
				return Environment.GetCommandLineArgs()[2];
			}

			return "";
		}

		/// <summary>
		/// コンテキストメニューを表示するか
		/// </summary>
		public bool IsOpenContextMenu = true;

		/// <summary>
		/// 初期化ファイル設定
		/// </summary>
		// TODO Settingsクラスへ移行
		private void LoadInitFile()
		{
			string iniFileName = GetCurrentDirectory() + "\\PeerstPlayer.ini";
			IniFile iniFile = new IniFile(iniFileName);

			// INIファイルの読み込み
			settings.LoadIniFile(iniFile, iniFileName);

			// TODO Settingsクラスへ移行
			#region デフォルト設定
			{
				// デフォルト
				string[] keys = iniFile.GetKeys("Player");

				for (int i = 0; i < keys.Length; i++)
				{
					string data = iniFile.ReadString("Player", keys[i]);
					switch (keys[i])
					{
						// レスボックス
						case "ResBox":
							panelResBox.Visible = (data == "True");
							break;

						// ステータスラベル
						case "StatusLabel":
							panelStatusLabel.Visible = (data == "True");
							break;

						// 最前列表示
						case "TopMost":
							TopMost = (data == "True");
							break;

						// 初期位置X
						case "X":
							try
							{
								Left = int.Parse(data);
							}
							catch
							{
							}
							break;

						// 初期位置Y
						case "Y":
							try
							{
								Top = int.Parse(data);
							}
							catch
							{
							}
							break;

						// 初期Width
						case "Width":
							try
							{
								Width = int.Parse(data);
							}
							catch
							{
							}
							break;

						// 初期Height
						case "Height":
							try
							{
								Height = int.Parse(data);
							}
							catch
							{
							}
							break;


						// ボリューム
						case "Volume":
							try
							{
								wmp.Volume = int.Parse(data);
							}
							catch
							{
							}
							break;

						// フォント名
						case "FontName":
							SetFont(data, labelDetail.Font.Size);
							break;

						// フォントのサイズ
						case "FontSize":
							try
							{
								SetFont(labelDetail.Font.Name, float.Parse(data));
							}
							catch
							{
							}
							break;

						case "FontColorR":
							try
							{
								int R = int.Parse(data);
								Color color = Color.FromArgb(255, R, labelDetail.ForeColor.G, labelDetail.ForeColor.B);
								labelDetail.ForeColor = color;
								labelDuration.ForeColor = color;
								labelVolume.ForeColor = color;
							}
							catch
							{
							}
							break;

						case "FontColorG":
							try
							{
								int G = int.Parse(data);
								Color color = Color.FromArgb(255, labelDetail.ForeColor.R, G, labelDetail.ForeColor.B);
								labelDetail.ForeColor = color;
								labelDuration.ForeColor = color;
								labelVolume.ForeColor = color;
							}
							catch
							{
							}
							break;

						case "FontColorB":
							try
							{
								int B = int.Parse(data);
								Color color = Color.FromArgb(255, labelDetail.ForeColor.R, labelDetail.ForeColor.G, B);
								labelDetail.ForeColor = color;
								labelDuration.ForeColor = color;
								labelVolume.ForeColor = color;
							}
							catch
							{
							}
							break;

						case "MouseGestureInterval":
							try
							{
								wmp.mouseGesture.Interval = int.Parse(data);
							}
							catch
							{
							}
							break;
					}
				}
			}

			#endregion
		}

		/// <summary>
		/// ステータスラベルのフォントを変える
		/// </summary>
		private void SetFont(string name, float size)
		{
			// 最大文字にする
			string duration = labelDuration.Text;
			string volume = labelVolume.Text;
			labelDuration.Text = "接続中...";
			labelVolume.Text = "100";

			// フォント指定
			labelDetail.Font = new Font(name, size);
			labelDuration.Font = new Font(name, size);
			labelVolume.Font = new Font(name, size);
			labelThreadTitle.Font = new Font(name, size);

			// 右側ラベルの適応
			panelStatusLabel.Height = labelDetail.Height + 6;
			panelDetailRight.Left = panelStatusLabel.Width - (labelDuration.Width + labelVolume.Width);
			panelDetailRight.Width = labelDuration.Width + labelVolume.Width;
			labelVolume.Left = panelDetailRight.Width - labelVolume.Width;

			// 戻す
			labelDuration.Text = duration;
			labelVolume.Text = volume;

			OnPanelSizeChange();
		}

		/// <summary>
		/// Iniを書きだす
		/// </summary>
		private void WriteIniFile()
		{
			IniFile iniFile = new IniFile(GetCurrentDirectory() + "\\PeerstPlayer.ini");

			// 一度書き込み
			// レスボックス
			iniFile.Write("Player", "ResBox", panelResBox.Visible.ToString());

			// ステータスラベル
			iniFile.Write("Player", "StatusLabel", panelStatusLabel.Visible.ToString());

			// フレーム
			iniFile.Write("Player", "Frame", Frame.ToString());

			// 最前列表示
			iniFile.Write("Player", "TopMost", TopMost.ToString());

			// アスペクト比
			iniFile.Write("Player", "AspectRate", settings.AspectRate.ToString());

			// レスボックスの操作方法
			iniFile.Write("Player", "ResBoxType", settings.ResBoxType.ToString());

			// レスボックスを自動表示
			iniFile.Write("Player", "ResBoxAutoVisible", settings.ResBoxAutoVisible.ToString());

			// 終了時にリレーを終了
			iniFile.Write("Player", "RlayCutOnClose", settings.RlayCutOnClose.ToString());

			//　書き込み後にレスボックスを閉じる
			iniFile.Write("Player", "CloseResBoxOnWrite", settings.CloseResBoxOnWrite.ToString());

			// スクリーン吸着を使うか
			iniFile.Write("Player", "UseScreenMagnet", settings.UseScreenMagnet.ToString());

			// 終了時に一緒にビューワも終了するか
			iniFile.Write("Player", "CloseViewerOnClose", settings.CloseViewerOnClose.ToString());

			// クリックした時にレスボックスを閉じるか
			iniFile.Write("Player", "ClickToResBoxClose", settings.ClickToResBoxClose.ToString());

			// 終了時に位置を保存するか
			iniFile.Write("Player", "SaveLocationOnClose", settings.SaveLocationOnClose.ToString());

			// 終了時にボリュームを保存するか
			iniFile.Write("Player", "SaveVolumeOnClose", settings.SaveVolumeOnClose.ToString());

			// 終了時にサイズを保存するか
			iniFile.Write("Player", "SaveSizeOnClose", settings.SaveSizeOnClose.ToString());

			// フォント名
			iniFile.Write("Player", "FontName", labelDetail.Font.Name);

			// フォント色
			iniFile.Write("Player", "FontColorR", labelDetail.ForeColor.R.ToString());
			iniFile.Write("Player", "FontColorG", labelDetail.ForeColor.G.ToString());
			iniFile.Write("Player", "FontColorB", labelDetail.ForeColor.B.ToString());
		}


		/// <summary>
		/// 更新完了
		/// </summary>
		void ChannelInfo_UpdateComp()
		{
			Text = channelInfo.Name;
			labelDetail.Text = channelInfo.ToString();

			if (channelInfo.IconURL != "")
			{
				try
				{
					pictureBoxIcon.Load(channelInfo.IconURL);
				}
				catch
				{
				}
				pictureBoxIcon.BackColor = Color.White;
				labelDetail.Left = 23;
			}
			else
			{
				labelDetail.Left = 3;
			}
		}

		/// <summary>
		/// パネルのアンカー設定
		/// </summary>
		bool PanelAnchor
		{
			set
			{
				// アンカー設定
				if (value)
				{
					panelWMP.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
					panelResBox.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
					panelStatusLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
				}
				// アンカー解除
				else
				{
					panelWMP.Anchor = AnchorStyles.None;
					panelResBox.Anchor = AnchorStyles.None;
					panelStatusLabel.Anchor = AnchorStyles.None;
				}
			}
		}

		bool IsWriting = false;

		#region APIで使う構造体
		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int x;
			public int y;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;
		}
		#endregion

		#region WIN32API
		/// <summary>
		/// 座標を含むウインドウのハンドルを取得
		/// </summary>
		/// <param name="Point">調査する座標</param>
		/// <returns>ポイントにウインドウがなければNULL</returns>
		[DllImport("user32.dll")]
		private static extern IntPtr WindowFromPoint(POINT Point);

		/// <summary>
		/// ハンドルからウインドウの位置を取得
		/// </summary>
		/// <param name="hWnd">ウインドウのハンドル</param>
		/// <param name="lpRect">ウインドウの座標</param>
		/// <returns>成功すればtrue</returns>
		[DllImport("user32.dll")]
		private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

		/// <summary>
		/// 指定したハンドルの祖先のハンドルを取得
		/// </summary>
		/// <param name="hwnd">ハンドル</param>
		/// <param name="gaFlags">フラグ</param>
		/// <returns>祖先のハンドル</returns>
		[DllImport("user32.dll")]
		private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

		private const uint GA_ROOT = 2;

		#endregion

		/// <summary>
		/// 作業フォルダを取得
		/// </summary>
		string GetCurrentDirectory()
		{
			if (Environment.GetCommandLineArgs().Length > 0)
			{
				string folder = Environment.GetCommandLineArgs()[0];
				folder = folder.Substring(0, folder.LastIndexOf('\\'));

				return folder;
			}
			return "";
		}
		
		#region 規定のブラウザを開く

		private static string GetDefaultBrowserExePath()
		{
			return GetDefaultExePath(@"http\shell\open\command");
		}

		private static string GetDefaultMailerExePath()
		{
			return GetDefaultExePath(@"mailto\shell\open\command");
		}

		private static string GetDefaultExePath(string keyPath)
		{
			string path = "";

			// レジストリ・キーを開く
			// 「HKEY_CLASSES_ROOT\xxxxx\shell\open\command」
			RegistryKey rKey = Registry.ClassesRoot.OpenSubKey(keyPath);
			if (rKey != null)
			{
				// レジストリの値を取得する
				string command = (string)rKey.GetValue(String.Empty);
				if (command == null)
				{
					return path;
				}

				// 前後の余白を削る
				command = command.Trim();
				if (command.Length == 0)
				{
					return path;
				}

				// 「"」で始まる長いパス形式かどうかで処理を分ける
				if (command[0] == '"')
				{
					// 「"～"」間の文字列を抽出
					int endIndex = command.IndexOf('"', 1);
					if (endIndex != -1)
					{
						// 抽出開始を「1」ずらす分、長さも「1」引く
						path = command.Substring(1, endIndex - 1);
					}
				}
				else
				{
					// 「（先頭）～（スペース）」間の文字列を抽出
					int endIndex = command.IndexOf(' ');
					if (endIndex != -1)
					{
						path = command.Substring(0, endIndex);
					}
					else
					{
						path = command;
					}
				}
			}

			return path;
		}

		#endregion

		/// <summary>
		/// 最小化ボタン
		/// </summary>
		private void toolStripButtonMin_Click(object sender, EventArgs e)
		{
			WindowState = FormWindowState.Minimized;
		}

		/// <summary>
		/// 最大化ボタン
		/// </summary>
		private void toolStripButtonMax_Click(object sender, EventArgs e)
		{
			if (WindowState == FormWindowState.Maximized)
			{
				WindowState = FormWindowState.Normal;
				OnPanelSizeChange(PanelWMPSize);
			}
			else
			{
				PanelWMPSize = WMPSize;
				WindowState = FormWindowState.Maximized;
			}

			OnPanelSizeChange();
		}

		/// <summary>
		/// 閉じるボタン
		/// </summary>
		private void toolStripButtonClose_Click(object sender, EventArgs e)
		{
			ExeCommand("Close");
		}

		/// <summary>
		/// スレッドビューワを開く
		/// </summary>
		private void toolStripButton1_Click(object sender, EventArgs e)
		{
			ExeCommand("OpenThreadViewer");
		}

		/// <summary>
		/// レスボックス：TextChange
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void resBox_TextChanged(object sender, EventArgs e)
		{
			// 書き込み完了
			if (IsWriting)
			{
				resBox.Text = "";
				IsWriting = false;
			}

			// スクロールバーを表示
			if (resBox.Width < resBox.PreferredSize.Width)
			{
				resBox.ScrollBars = ScrollBars.Horizontal;
			}
			else
			{
				resBox.ScrollBars = ScrollBars.None;
			}

			// レスボックスの大きさを変更
			resBox.Height = resBox.PreferredSize.Height;
			const int LABEL_THREAD_TITLE_HEIGHT = 13;
			panelResBox.Height = LABEL_THREAD_TITLE_HEIGHT + resBox.Height;
			OnPanelSizeChange();
		}

		/// <summary>
		/// レスボックス：KeyDown
		/// </summary>
		private void resBox_KeyDown(object sender, KeyEventArgs e)
		{
			// レスボックスをバックスペースで閉じる
			if (settings.CloseResBoxOnBackSpace && e.KeyCode == Keys.Back)
			{
				if (resBox.Text == "")
				{
					panelResBox.Visible = !panelResBox.Visible;
					OnPanelSizeChange();
					wmp.Select();
				}
			}

			if (e.Control && e.KeyCode == Keys.A)
			{
				resBox.SelectAll();
			}

			if (e.KeyCode == Keys.Enter)
			{
				if (settings.ResBoxType)
				{
					if (e.Control)
					{
						ResBoxText = resBox.Text;
						if (operationBbs.Write("", "sage", resBox.Text))
						{
							resBox.Text = "";
							if (settings.CloseResBoxOnWrite)
							{
								panelResBox.Visible = false;
							}
							IsWriting = true;
						}
						else
						{
							resBox.Text = ResBoxText;
						}
					}
				}
				else
				{
					if (!e.Control)
					{
						if (operationBbs.Write("", "sage", resBox.Text))
						{
							resBox.Text = "";
						}
						IsWriting = true;
					}
				}
			}
		}

		#region スレッド選択フォーム

		// スレッド選択フォーム
		ThreadSelectForm threadSelectForm = null;

		/// <summary>
		/// スレッドタイトルをクリック
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void labelThreadTitle_MouseDown(object sender, MouseEventArgs e)
		{
			// 左クリック
			if (e.Button == System.Windows.Forms.MouseButtons.Left)
			{
				// TODO コンタクトＵＲＬが取得できる前に開く場合はどうするか
				// スレッド選択フォームを開く
				threadSelectForm.Update(operationBbs.ThreadUrl);
				threadSelectForm.Show();
				threadSelectForm.Focus();
			}
			// 右クリック
			else if (e.Button == System.Windows.Forms.MouseButtons.Right)
			{
				// スレッドビューワを開く
				スレッドビューワを開くToolStripMenuItem_Click(this, EventArgs.Empty);
			}
		}

		/// <summary>
		/// スレッド選択オブザーバ
		/// </summary>
		/// <param name="threadUrl">スレッドURL</param>
		/// <param name="threadNo">スレッド番号</param>
		void ThreadSelectObserver.UpdateThreadUrl(string threadUrl, string threadNo)
		{
			// スレッドURL変更
			operationBbs.ChangeUrl(threadUrl);
			operationBbs.ChangeThread(threadNo);

			// TODO スレッド化
			// スレッド情報を更新
			operationBbs.UpdateThreadInfo();
			if (labelThreadTitle.InvokeRequired)
			{
				Invoke(new UpdateThreadInfoDelegate(UpdateThreadInfo));
			}
			else
			{
				UpdateThreadInfo();
			}
		}

		/// <summary>
		/// スレッドタイトル表示
		/// </summary>
		void UpdateThreadInfo()
		{
			// スレッドタイトル表示
			if (operationBbs.ThreadInfo.ThreadTitle == "")
			{
				// 掲示板[板名] - スレッドを選択してください
				string bbsName = operationBbs.BbsName;
				if (bbsName.Length > 0)
				{
					labelThreadTitle.Text = "掲示板名[ " + operationBbs.BbsName + " ] - スレッドを選択してください";
				}
				else
				{
					labelThreadTitle.Text = "スレッドを選択してください";
				}
			}
			else
			{
				// スレタイ(レス数)
				labelThreadTitle.Text = operationBbs.ThreadInfo.ThreadTitle + "(" + operationBbs.ThreadInfo.ResCount + ")";

				// 文字色の変更
				if (operationBbs.ThreadInfo.ResCount >= 1000)
				{
					labelThreadTitle.ForeColor = Color.Red;
				}
				else
				{
					labelThreadTitle.ForeColor = Color.SpringGreen;
				}
			}
		}

		#endregion

		#region チャンネル情報更新

		/// <summary>
		/// チャンネル情報更新タイマー
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void timerUpdateChannelInfo_Tick(object sender, EventArgs e)
		{
			// チャンネル情報更新
			ExeCommand("ChannelInfoUpdate");
		}

		/// <summary>
		/// チャンネル情報更新スレッド
		/// </summary>
		System.Threading.Thread updateChannelInfoThread = null;

		/// <summary>
		/// チャンネル情報更新デリゲート
		/// </summary>
		delegate void UpdateThreadInfoDelegate();

		/// <summary>
		/// チャンネル情報を更新
		/// </summary>
		void ChannelInfoUpdate()
		{
			updateChannelInfoThread = new System.Threading.Thread(new System.Threading.ThreadStart(UpdateChannelInfoWorker));
			updateChannelInfoThread.IsBackground = true;
			updateChannelInfoThread.Start();
		}

		/// <summary>
		/// 初回のスレ選択をしたか
		/// </summary>
		bool InitThreadSelected = false;

		/// <summary>
		/// チャンネル情報更新
		/// </summary>
		void UpdateChannelInfoWorker()
		{
			// チャンネル情報取得
			pecaManager.GetChannelInfo();

			// 初回だけスレ情報取得＋スレ移動
			if (!InitThreadSelected)
			{
				operationBbs.ChangeUrl(pecaManager.ChannelInfo.ContactUrl);

				InitThreadSelected = true;
			}

			// スレッド情報を更新
			operationBbs.UpdateThreadInfo();
			if (labelThreadTitle.InvokeRequired)
			{
				Invoke(new UpdateThreadInfoDelegate(UpdateThreadInfo));
			}
			else
			{
				UpdateThreadInfo();
			}
		}

		#endregion

		/// <summary>
		/// WMPサイズが変更された
		/// </summary>
		/// <param name="width">WMP幅</param>
		/// <param name="height">WMP高さ</param>
		void WindowSizeMediator.OnChangeWmpSize(int width, int height)
		{
			panelWMP.Size = new Size(width, height);
			OnPanelSizeChange();
		}

		/// <summary>
		/// サイズチェンジ
		/// </summary>
		public void OnPanelSizeChange()
		{
			if (WindowState == FormWindowState.Normal)
			{
				// アンカーをNone
				PanelAnchor = false;

				// FormSize変更
				int height = 0;
				height += (panelWMP.Visible ? panelWMP.Height : 0);
				height += (panelResBox.Visible ? panelResBox.Height : 0);
				height += (panelStatusLabel.Visible ? panelStatusLabel.Height : 0);

				Size frame = Size - ClientSize;
				Size = new Size(frame.Width + panelWMP.Width, frame.Height + height);

				// WMPパネル
				height = 0;
				panelWMP.Location = new Point(0, 0);
				panelWMP.Width = ClientSize.Width;
				height += (panelWMP.Visible ? panelWMP.Height : 0);

				// レスボックスパネル
				panelResBox.Location = new Point(0, height);
				panelResBox.Width = ClientSize.Width;
				height += (panelResBox.Visible ? panelResBox.Height : 0);

				// ステータスラベルパネル
				panelStatusLabel.Location = new Point(0, height);
				panelStatusLabel.Width = ClientSize.Width;
				height += (panelStatusLabel.Visible ? panelStatusLabel.Height : 0);

				// アンカーを再設定
				PanelAnchor = true;
			}
			else
			{
				// アンカーをNone
				PanelAnchor = false;

				int height = ClientSize.Height;

				// ステータスラベルパネル
				height -= (panelStatusLabel.Visible ? panelStatusLabel.Height : 0);
				panelStatusLabel.Location = new Point(0, height);
				panelStatusLabel.Width = ClientSize.Width;

				// レスボックスパネル
				height -= (panelResBox.Visible ? panelResBox.Height : 0);
				panelResBox.Location = new Point(0, height);
				panelResBox.Width = ClientSize.Width;

				// WMPパネル
				panelWMP.Location = new Point(0, 0);
				panelWMP.Size = new Size(Width, height);
				panelWMP.Width = ClientSize.Width;

				// アンカーを再設定
				PanelAnchor = true;
			}
		}

		/// <summary>
		/// 最大化から解除した時のパネルサイズ変更
		/// </summary>
		public void OnPanelSizeChange(Size PanelWMP)
		{
			// アンカーをNone
			PanelAnchor = false;

			// FormSize変更
			int height = 0;
			height += (panelWMP.Visible ? PanelWMP.Height : 0);
			height += (panelResBox.Visible ? panelResBox.Height : 0);
			height += (panelStatusLabel.Visible ? panelStatusLabel.Height : 0);

			Size frame = Size - ClientSize;
			Size = new Size(frame.Width + PanelWMP.Width, frame.Height + height);

			// WMPパネル
			height = 0;
			panelWMP.Location = new Point(0, 0);
			panelWMP.Size = PanelWMP;
			height += (panelWMP.Visible ? PanelWMP.Height : 0);

			// レスボックスパネル
			panelResBox.Location = new Point(0, height);
			panelResBox.Width = ClientSize.Width;
			height += (panelResBox.Visible ? panelResBox.Height : 0);

			// ステータスラベルパネル
			panelStatusLabel.Location = new Point(0, height);
			panelStatusLabel.Width = ClientSize.Width;
			height += (panelStatusLabel.Visible ? panelStatusLabel.Height : 0);

			// アンカーを再設定
			PanelAnchor = true;
		}
	}
}
