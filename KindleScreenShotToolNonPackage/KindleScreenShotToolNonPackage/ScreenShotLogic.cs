﻿using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace KindleScreenShotToolNonPackage
{
    /// <summary>
    /// スクリーンショットロジック
    /// </summary>
    internal class ScreenShotLogic
    {
        #region 定数

        /// <summary>
        /// メッセージ定数：キーダウン
        /// </summary>
        const uint WM_KEYDOWN = 0x0100;

        /// <summary>
        /// メッセージ定数：キーアップ
        /// </summary>
        const uint WM_KEYUP = 0x0101;

        /// <summary>
        /// メッセージ定数：左キー
        /// </summary>
        public const int VK_LEFT = 0x25;

        /// <summary>
        /// メッセージ定数：上キー
        /// </summary>
        public const int VK_UP = 0x26;

        /// <summary>
        /// メッセージ定数：右キー
        /// </summary>
        public const int VK_RIGHT = 0x27;

        /// <summary>
        /// メッセージ定数：下キー
        /// </summary>
        public const int VK_DOWN = 0x28;

        #endregion

        #region API定義

        /// <summary>
        /// ウィンドウハンドル取得API
        /// </summary>
        /// <param name="lpClassName">クラス名</param>
        /// <param name="lpWindowName">ウィンドウ名</param>
        /// <returns>結果</returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

        /// <summary>
        /// ウィンドウアクティブ化API
        /// </summary>
        /// <param name="hWnd">ハンドル</param>
        /// <returns>結果</returns>
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// メッセージポストAPI
        /// </summary>
        /// <param name="hWnd">ハンドル</param>
        /// <param name="Msg">メッセージ</param>
        /// <param name="wParam">メッセージに関連する追加情報</param>
        /// <param name="lParam">メッセージに関連する詳細情報</param>
        /// <returns>結果</returns>
        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        #endregion

        #region メソッド

        /// <summary>
        /// Kindleタイトル取得処理
        /// </summary>
        /// <returns>Kindleタイトル</returns>
        public string GetKindleTitle()
        {
            // 全プロセスを取得し、ループさせる。
            foreach (Process p in Process.GetProcesses())
            {
                // メインウィンドウのタイトル・プロセス名を判定する。
                if (p.MainWindowTitle.Length != 0 && string.Equals("Kindle", p.ProcessName))
                {
                    return p.MainWindowTitle;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Kindleプロセス実行中判定処理
        /// </summary>
        /// <param name="ProcessName">プロセス名</param>
        /// <returns>判定結果（true：実行中・false：未実行）</returns>
        public bool IsKindleRunning(string ProcessName)
        {
            return Process.GetProcesses()
                .Any(p => string.Equals(ProcessName, p.MainWindowTitle) && string.Equals("Kindle", p.ProcessName));
        }

        /// <summary>
        /// Kindleウィンドウアクティブ化処理
        /// </summary>
        /// <param name="kindleTitle">Kindleウィンドウタイトル</param>
        public void ActivateKindleWindow(string kindleTitle)
        {
            // Kindleウィンドウハンドルを取得し、有無を判定する。
            IntPtr hWnd = FindWindow(null, kindleTitle);
            if (hWnd != IntPtr.Zero)
            {
                // ウィンドウをアクティブ化する。
                SetForegroundWindow(hWnd);
            }
        }

        /// <summary>
        /// キー押下処理
        /// </summary>
        /// <param name="kindleTitle">Kindleウィンドウタイトル</param>
        /// <param name="keyCode">キーコード</param>
        public void KeyDown(string kindleTitle, int keyCode)
        {
            // Kindleウィンドウハンドルを取得し、有無を判定する。
            // （どのタイミングがKindleウィンドウが閉じられるか不明なため、判定を行う。）
            IntPtr hWnd = FindWindow(null, kindleTitle);
            if (hWnd != IntPtr.Zero)
            {
                // 指定されたキーコードを押下する。
                PostMessage(hWnd, WM_KEYDOWN, keyCode, IntPtr.Zero);
                PostMessage(hWnd, WM_KEYUP, keyCode, IntPtr.Zero);
            }
        }

        /// <summary>
        /// スクリーンショット保存処理
        /// </summary>
        /// <param name="captureStartX">撮影開始位置：Ｘ</param>
        /// <param name="captureStartY">撮影開始位置：Ｙ</param>
        /// <param name="captureWidth">撮影幅</param>
        /// <param name="captureHeight">撮影高さ</param>
        /// <param name="saveFileName">保存ファイル名（フルパス）</param>
        public void SaveScreenShot(
            int captureStartX,
            int captureStartY,
            int captureWidth,
            int captureHeight,
            string saveFileName)
        {
            // スクリーンショットを取得する。
            using Bitmap bmp = new Bitmap(captureWidth, captureHeight);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(new Point(captureStartX, captureStartY), Point.Empty, new Size(captureWidth, captureHeight));
            }

            bmp.Save(saveFileName, ImageFormat.Png);
        }

        #endregion
    }
}
