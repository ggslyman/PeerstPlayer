﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using PeerstLib.Utility;

namespace PeerstLib.Bbs.Strategy
{
	//-------------------------------------------------------------
	// 概要：掲示板ストラテジクラス
	//-------------------------------------------------------------
	public abstract class BbsStrategy
	{
		//-------------------------------------------------------------
		// 公開プロパティ
		//-------------------------------------------------------------

		// 掲示板情報クラス
		public BbsInfo BbsInfo { get; set; }

		// スレッド一覧
		public List<ThreadInfo> ThreadList { get; set; }

		// スレッド選択状態
		public bool ThreadSelected
		{
			get { return (!string.IsNullOrEmpty(BbsInfo.ThreadNo)); }
		}

		// スレッドURL
		public abstract string ThreadUrl { get; }

		//-------------------------------------------------------------
		// 非公開プロパティ
		//-------------------------------------------------------------

		// 掲示板のエンコード
		protected abstract Encoding encoding { get; }

		// スレッド一覧情報URL
		protected abstract string subjectUrl { get; }

		// スレッド情報取得
		protected abstract string datUrl { get; }

		// 板URL
		protected abstract string boardUrl { get; }

		// 書き込みリクエストURL
		protected abstract string writeUrl { get; }

		//-------------------------------------------------------------
		// 概要：スレッド変更
		//-------------------------------------------------------------
		public void ChangeThread(string threadNo)
		{
			Logger.Instance.DebugFormat("ChangeThread(threadNo:{0})", threadNo);
			BbsInfo.ThreadNo = threadNo;
		}

		//-------------------------------------------------------------
		// 概要：スレッド一覧更新
		//-------------------------------------------------------------
		public void UpdateThreadList()
		{
			Logger.Instance.DebugFormat("UpdateThreadList()");
			string subjectText = WebUtil.GetHtml(subjectUrl, encoding);
			string[] lines = subjectText.Replace("\r\n", "\n").Split('\n');
			ThreadList = AnalyzeSubjectText(lines);

			Logger.Instance.DebugFormat("スレッド一覧取得：正常 [スレッド取得数：{0}]", ThreadList.Count);
		}

		//-------------------------------------------------------------
		// 概要：掲示板名の更新
		//-------------------------------------------------------------
		public void UpdateBbsName()
		{
			Logger.Instance.DebugFormat("UpdateBbsName()");
			string html = WebUtil.GetHtml(boardUrl, encoding);

			Regex regex = new Regex("<title>(.*)</title>");
			Match match = regex.Match(html);

			// 取得成功
			if (match.Groups.Count > 1)
			{
				BbsInfo.BbsName = match.Groups[1].Value;
				Logger.Instance.DebugFormat("掲示板名取得:正常 [掲示板名:{0}]", BbsInfo.BbsName);
				return;
			}

			// 取得失敗
			BbsInfo.BbsName = string.Empty;
			Logger.Instance.ErrorFormat("掲示板名取得:異常 [指定URL:{0}]", BbsInfo.Url);
		}

		//-------------------------------------------------------------
		// 概要：レス書き込み
		//-------------------------------------------------------------
		public string Write(string name, string mail, string message)
		{
			Logger.Instance.DebugFormat("Write(name:{0}, mail:{1}, message:{2})", name, mail, message);

			// POSTデータ作成
			byte[] requestData = CreateWriteRequestData(name, mail, message);

			// リクエスト作成
			HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(writeUrl);
			webRequest.Method = "POST";
			webRequest.ContentType = "application/x-www-form-urlencoded";
			webRequest.ContentLength = requestData.Length;
			webRequest.Referer = writeUrl;

			// POST送信
			Stream requestStream = webRequest.GetRequestStream();
			requestStream.Write(requestData, 0, requestData.Length);
			requestStream.Close();

			// リクエスト受信
			HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();
			Stream responseStream = webResponse.GetResponseStream();
			StreamReader sr = new StreamReader(responseStream, encoding);
			string html = sr.ReadToEnd();
			sr.Close();
			responseStream.Close();

			// 書き込みエラーチェック
			return CheckWriteError(html);
		}

		//-------------------------------------------------------------
		// 概要：コンストラクタ
		//-------------------------------------------------------------
		protected BbsStrategy(BbsInfo bbsInfo)
		{
			Logger.Instance.DebugFormat("BbsStrategy(url:{0})", bbsInfo.Url);
			this.BbsInfo = bbsInfo;
		}

		//-------------------------------------------------------------
		// 概要：スレッド一覧解析
		//-------------------------------------------------------------
		protected abstract List<ThreadInfo> AnalyzeSubjectText(string[] lines);

		//-------------------------------------------------------------
		// 概要：書き込み用リクエストデータ作成
		//-------------------------------------------------------------
		protected abstract byte[] CreateWriteRequestData(string name, string mail, string message);

		public enum WriteStatus
		{
			Posted,				// 書きこみました
			NothingMessage,		// 本文がありません
			MultiWriteError,	// 多重書き込みです
			NothingThreadError, // 該当スレッドは存在しません
			LostUserInfoError,	// ユーザー設定が消失しています
			ThreadStopError,	// スレッドストップです
		}

		//-------------------------------------------------------------
		// 概要：書き込みエラーチェック
		//-------------------------------------------------------------
		protected string CheckWriteError(string html)
		{
			Regex titleRegex = new Regex("<title>(.*)</title>");
			Match titleMatch = titleRegex.Match(html);
			string title = titleMatch.Groups[1].Value;

			Regex tagRegex = new Regex("<!-- 2ch_X:(.*) -->");
			Match tagMatch = tagRegex.Match(html);
			string tag = tagMatch.Groups[1].Value;

			// 書き込み失敗
			if (title.StartsWith("ERROR") || title.StartsWith("ＥＲＲＯＲ"))
			{
				Logger.Instance.ErrorFormat("レス書き込み：異常 [スレッド:{1} 実行結果:{0}]", BbsInfo.Url, html);
				throw new Exception();
			}

			Logger.Instance.DebugFormat("レス書き込み：正常 [スレッド:{0} 実行結果:{1}]", title, BbsInfo.Url);
			return html;
		}

		//-------------------------------------------------------------
		// 概要：スレッド作成日時の取得
		//-------------------------------------------------------------
		protected double GetThreadSince(string threadNo)
		{
			// 経過秒数の取得
			int second;
			if (!int.TryParse(threadNo, out second)) { return -1; }

			// 経過日時の取得
			DateTime time = new DateTime(1970, 1, 1, 0, 0, 0);
			time = time.AddSeconds(second);
			time = System.TimeZone.CurrentTimeZone.ToLocalTime(time);
			return (DateTime.Now - time).TotalDays;
		}

		//-------------------------------------------------------------
		// 概要：スレッド勢いの取得
		//-------------------------------------------------------------
		protected float GetThreadSpeed(double days, string resCount)
		{
			// 勢いの計算
			int count;
			if (!int.TryParse(resCount, out count)) { return -1; }
			return (float)(count / days);
		}
	}
}
