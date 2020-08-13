using System;
using System.Runtime.InteropServices;

namespace RucheHome.AviUtl.ExEdit.GcmzDrops
{
    /// <summary>
    /// 『ごちゃまぜドロップス』の共有メモリ情報を保持するクラス。
    /// </summary>
    public class GcmzInfo
    {
        /// <summary>
        /// 共有メモリ実データ構造体。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct Data
        {
            public int WindowHandle;
            public int Width;
            public int Height;
            public int VideoRate;
            public int VideoScale;
            public int AudioRate;
            public int AudioChannel;
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="data">実データ参照。</param>
        internal GcmzInfo(ref Data data)
        {
            this.WindowHandle = new IntPtr(data.WindowHandle);
            this.Width = data.Width;
            this.Height = data.Height;
            this.FrameRateBase = data.VideoRate;
            this.FrameRateScale = data.VideoScale;
            this.FrameRate =
                (data.VideoScale > 0) ? ((decimal)data.VideoRate / data.VideoScale) : 0;
            this.AudioSampleRate = data.AudioRate;
            this.AudioChannelCount = data.AudioChannel;
        }

        /// <summary>
        /// WM_COPYDATA メッセージ送信先ウィンドウハンドルを取得する。
        /// </summary>
        public IntPtr WindowHandle { get; }

        /// <summary>
        /// WM_COPYDATA 送信先ウィンドウが開かれているか否かを取得する。
        /// </summary>
        public bool IsWindowOpened => this.WindowHandle != IntPtr.Zero;

        /// <summary>
        /// AviUtl拡張編集プロジェクトの横幅設定値を取得する。
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// AviUtl拡張編集プロジェクトの縦幅設定値を取得する。
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// AviUtl拡張編集プロジェクトのフレームレート基準値を取得する。
        /// </summary>
        public int FrameRateBase { get; }

        /// <summary>
        /// AviUtl拡張編集プロジェクトのフレームレートスケール値を取得する。
        /// </summary>
        public int FrameRateScale { get; }

        /// <summary>
        /// AviUtl拡張編集プロジェクトのフレームレートを取得する。
        /// </summary>
        public decimal FrameRate { get; }

        /// <summary>
        /// AviUtl拡張編集プロジェクトの音声サンプリングレートを取得する。
        /// </summary>
        public int AudioSampleRate { get; }

        /// <summary>
        /// AviUtl拡張編集プロジェクトの音声チャンネル数を取得する。
        /// </summary>
        public int AudioChannelCount { get; }

        /// <summary>
        /// AviUtl拡張編集プロジェクトが開かれているか否かを取得する。
        /// </summary>
        public bool IsProjectOpened =>
            this.Width > 0 &&
            this.Height > 0 &&
            this.FrameRateBase > 0 &&
            this.FrameRateScale > 0 &&
            this.AudioSampleRate > 0 &&
            this.AudioChannelCount > 0;
    }
}
