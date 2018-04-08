using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using RucheHome.Windows.WinApi;
using static RucheHome.Util.ArgumentValidater;

namespace RucheHome.AviUtl.ExEdit.GcmzDrops
{
    /// <summary>
    /// 『ごちゃまぜドロップス』を利用した、
    /// AviUtl拡張編集ウィンドウへのファイルドロップ処理を提供する静的クラス。
    /// </summary>
    public static class FileDrop
    {
        /// <summary>
        /// 処理結果列挙。
        /// </summary>
        public enum Result
        {
            /// <summary>
            /// 処理成功。
            /// </summary>
            Success = 0,

            /// <summary>
            /// ファイルマッピングオブジェクト取得に失敗。
            /// </summary>
            FileMappingFail,

            /// <summary>
            /// ファイルマッピングアドレス取得に失敗。
            /// </summary>
            MapViewFail,

            /// <summary>
            /// 処理対象ウィンドウが見つからない。
            /// </summary>
            WindowNotFound,

            /// <summary>
            /// 処理対象プロジェクトが見つからない。
            /// </summary>
            ProjectNotFound,

            /// <summary>
            /// メッセージ送信タイムアウト。
            /// </summary>
            MessageTimeout,
        }

        /// <summary>
        /// ファイルドロップ処理を行う。
        /// </summary>
        /// <param name="ownWindowHandle">送信元ウィンドウハンドル。</param>
        /// <param name="filePath">ファイルパス。</param>
        /// <param name="stepFrameCount">ドロップ後に進めるフレーム数。</param>
        /// <param name="layer">レイヤー位置指定。既定位置にするならば 0 。</param>
        /// <param name="timeoutMilliseconds">
        /// タイムアウトミリ秒数。負数ならばタイムアウトしない。
        /// </param>
        /// <returns>処理結果。</returns>
        public static Result Run(
            IntPtr ownWindowHandle,
            string filePath,
            int stepFrameCount = 0,
            int layer = 0,
            int timeoutMilliseconds = -1)
        {
            ValidateArgumentNullOrEmpty(filePath, nameof(filePath));

            return
                Run(
                    ownWindowHandle,
                    new[] { filePath },
                    stepFrameCount,
                    layer,
                    timeoutMilliseconds);
        }

        /// <summary>
        /// ファイルドロップ処理を行う。
        /// </summary>
        /// <param name="ownWindowHandle">送信元ウィンドウハンドル。</param>
        /// <param name="filePathes">ファイルパス列挙。</param>
        /// <param name="stepFrameCount">ドロップ後に進めるフレーム数。</param>
        /// <param name="layer">レイヤー位置指定。既定位置にするならば 0 。</param>
        /// <param name="timeoutMilliseconds">
        /// タイムアウトミリ秒数。負数ならばタイムアウトしない。
        /// </param>
        /// <returns>処理結果。</returns>
        public static Result Run(
            IntPtr ownWindowHandle,
            IEnumerable<string> filePathes,
            int stepFrameCount = 0,
            int layer = 0,
            int timeoutMilliseconds = -1)
        {
            // 引数チェック
            ValidateArgumentNull(filePathes, nameof(filePathes));
            if (!filePathes.Any())
            {
                throw new ArgumentException(@"The value is empty.", nameof(filePathes));
            }
            foreach (var v in filePathes.Select((filePath, i) => new { filePath, i }))
            {
                ValidateArgumentNullOrEmpty(
                    v.filePath,
                    nameof(filePathes) + '[' + v.i + ']');
            }
            ValidateArgumentOutOfRange(
                stepFrameCount,
                0,
                int.MaxValue,
                nameof(stepFrameCount));

            // 対象ウィンドウハンドル取得
            var result = ReadTargetWindowHandle(out var targetWindowHandle);
            if (result != Result.Success)
            {
                return result;
            }

            var dataPtr = IntPtr.Zero;
            var lParamPtr = IntPtr.Zero;
            try
            {
                // 送信文字列作成
                var data =
                    ((layer == 0) ? -1 : layer) + "\0" +
                    stepFrameCount + "\0" +
                    string.Join("\0", filePathes) + "\0";
                dataPtr = Marshal.StringToHGlobalUni(data);

                // LPARAM 作成
                COPYDATASTRUCT lParam;
                lParam.Param = UIntPtr.Zero;
                lParam.DataSize = data.Length * 2;
                lParam.DataAddress = dataPtr;
                lParamPtr = Marshal.AllocHGlobal(Marshal.SizeOf(lParam));
                Marshal.StructureToPtr(lParam, lParamPtr, false);

                // WM_COPYDATA メッセージ送信
                var window = new Win32Window(targetWindowHandle);
                var msgRes =
                    window.SendMessage(
                        WM_COPYDATA,
                        ownWindowHandle,
                        lParamPtr,
                        timeoutMilliseconds);
                if (!msgRes.HasValue)
                {
                    return Result.MessageTimeout;
                }
            }
            finally
            {
                if (lParamPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(lParamPtr);
                }
                if (dataPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(dataPtr);
                }
            }

            return Result.Success;
        }

        /// <summary>
        /// 『ごちゃまぜドロップス』のファイルマッピングデータ構造体。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct FileMapData
        {
            public uint WindowHandle;
            public int Width;
            public int Height;
            public int VideoRate;
            public int VideoScale;
            public int AudioRate;
            public int AudioChannel;
        }

        /// <summary>
        /// 『ごちゃまぜドロップス』のファイルマッピング名。
        /// </summary>
        private const string FileMapName = @"GCMZDrops";

        /// <summary>
        /// 『ごちゃまぜドロップス』から対象ウィンドウハンドルを読み取る。
        /// </summary>
        /// <param name="dest">対象ウィンドウハンドルの設定先。</param>
        /// <returns>処理結果。</returns>
        private static Result ReadTargetWindowHandle(out IntPtr dest)
        {
            dest = IntPtr.Zero;

            var handle = IntPtr.Zero;
            var dataAddress = IntPtr.Zero;
            try
            {
                // ファイルマッピングハンドル取得
                handle = OpenFileMapping(FILE_MAP_READ, false, FileMapName);
                if (handle == IntPtr.Zero)
                {
                    return Result.FileMappingFail;
                }

                // ファイルマッピングアドレス取得
                dataAddress = MapViewOfFile(handle, FILE_MAP_READ, 0, 0, UIntPtr.Zero);
                if (dataAddress == IntPtr.Zero)
                {
                    return Result.MapViewFail;
                }

                // データ取得
                var data =
                    (FileMapData)Marshal.PtrToStructure(dataAddress, typeof(FileMapData));
                if (data.WindowHandle == 0)
                {
                    return Result.WindowNotFound;
                }
                if (data.Width <= 0 || data.Height <= 0)
                {
                    return Result.ProjectNotFound;
                }

                dest = new IntPtr(data.WindowHandle);
            }
            finally
            {
                if (dataAddress != IntPtr.Zero)
                {
                    UnmapViewOfFile(dataAddress);
                }
                if (handle != IntPtr.Zero)
                {
                    CloseHandle(handle);
                }
            }

            return Result.Success;
        }

        #region Win32 API インポート

        [StructLayout(LayoutKind.Sequential)]
        struct COPYDATASTRUCT
        {
            public UIntPtr Param;
            public int DataSize;
            public IntPtr DataAddress;
        }

        private const uint FILE_MAP_READ = 0x0004;
        private const uint WM_COPYDATA = 0x004A;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr OpenFileMapping(
            uint desiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
            string name);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr MapViewOfFile(
            IntPtr fileMapHandle,
            uint desiredAccess,
            uint fileOffsetHigh,
            uint fileOffsetLow,
            UIntPtr numberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool UnmapViewOfFile(IntPtr address);

        #endregion
    }
}
