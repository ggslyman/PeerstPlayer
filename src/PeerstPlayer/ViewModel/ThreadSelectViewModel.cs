﻿using PeerstLib.Bbs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace PeerstPlayer.ViewModel
{
	public class ThreadSelectViewModel
	{
		// スレッド一覧
		public List<ThreadInfo> ThreadList { get { return operationBbs.ThreadList; } }

		// スレッドURL
		public string ThreadUrl { get { return operationBbs.ThreadUrl; } }

		// スレッド一覧変更イベント
		public event EventHandler ThreadListChange
		{
			add { operationBbs.ThreadListChange += value; }
			remove { operationBbs.ThreadListChange -= value; }
		}

		// 掲示板操作クラス
		private　OperationBbs operationBbs = new OperationBbs();

		// スレッド一覧更新用
		private BackgroundWorker updateThreadListWorker = new BackgroundWorker();

		// スレッド一覧更新
		public void Update(string threadUrl)
		{
			operationBbs.ChangeUrl(threadUrl);
		}

		// スレッド変更
		public void ChangeThread(string threadNo)
		{
			operationBbs.ChangeThread(threadNo);
		}
	}
}