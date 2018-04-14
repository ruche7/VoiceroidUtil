using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using RucheHome.Util;
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
            /// 原因不明の失敗。
            /// </summary>
            Fail,

            /// <summary>
            /// ファイルマッピングオブジェクト取得に失敗。
            /// </summary>
            FileMappingFail,

            /// <summary>
            /// ファイルマッピングアドレス取得に失敗。
            /// </summary>
            MapViewFail,

            /// <summary>
            /// 処理対象ウィンドウ(拡張編集ウィンドウとは別)が見つからない。
            /// </summary>
            GcmzWindowNotFound,

            /// <summary>
            /// 処理対象プロジェクトが見つからない。
            /// </summary>
            ProjectNotFound,

            /// <summary>
            /// 拡張編集ウィンドウが見つからない。
            /// </summary>
            ExEditWindowNotFound,

            /// <summary>
            /// 拡張編集ウィンドウが閉じられている。
            /// </summary>
            ExEditWindowInvisible,

            /// <summary>
            /// メッセージ送信失敗。
            /// </summary>
            MessageFail,

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
            ValidateFilePath(filePath, nameof(filePath));

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
            ValidateFilePathes(filePathes, nameof(filePathes));
            ValidateArgumentOutOfRange(
                stepFrameCount,
                0,
                int.MaxValue,
                nameof(stepFrameCount));

            // 処理対象ウィンドウ取得
            var result = ReadTargetWindowHandle(out var targetWindowHandle);
            if (result != Result.Success)
            {
                return result;
            }
            var targetWindow = new Win32Window(targetWindowHandle);
            if (!targetWindow.IsExists)
            {
                return Result.GcmzWindowNotFound;
            }

            // 拡張編集ウィンドウが表示されていないと失敗するので確認
            var aviUtlWindowHandle = targetWindow.GetOwner()?.Handle;
            if (!aviUtlWindowHandle.HasValue)
            {
                return Result.ExEditWindowNotFound;
            }
            var exEditWindow =
                Win32Window.FromDesktop()
                    .FindChildren()
                    .FirstOrDefault(
                        win =>
                            win.GetOwner()?.Handle == aviUtlWindowHandle.Value &&
                            win
                                .GetText(timeoutMilliseconds)?
                                .StartsWith(ExEditWindowTitlePrefix) == true);
            if (exEditWindow == null || !exEditWindow.IsExists)
            {
                return Result.ExEditWindowNotFound;
            }
            if (!exEditWindow.IsVisible)
            {
                return Result.ExEditWindowInvisible;
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
                var msgRes =
                    targetWindow.SendMessage(
                        WM_COPYDATA,
                        ownWindowHandle,
                        lParamPtr,
                        timeoutMilliseconds);
                if (!msgRes.HasValue)
                {
                    return Result.MessageTimeout;
                }
            }
            catch (Exception ex)
            {
                ThreadTrace.WriteException(ex);
                return Result.MessageFail;
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
        /// ファイルパスがファイルドロップ処理対象として妥当であるか検証する。
        /// </summary>
        /// <param name="filePath">ファイルパス。</param>
        /// <param name="argName">引数名。例外メッセージに利用される。</param>
        /// <remarks>
        /// 妥当でない場合は例外が送出される。
        /// </remarks>
        public static void ValidateFilePath(string filePath, string argName = null)
        {
            ValidateArgumentNullOrEmpty(filePath, argName);

            if (!File.Exists(filePath))
            {
                var message = $@"The file path ""{filePath}"" is not exists.";
                throw
                    (argName == null) ?
                        new ArgumentException(message) :
                        new ArgumentException(message, argName);
            }
        }

        /// <summary>
        /// ファイルパス列挙がファイルドロップ処理対象として妥当であるか検証する。
        /// </summary>
        /// <param name="filePathes">ファイルパス列挙。</param>
        /// <param name="argName">引数名。例外メッセージに利用される。</param>
        /// <remarks>
        /// 妥当でない場合は例外が送出される。
        /// </remarks>
        public static void ValidateFilePathes(
            IEnumerable<string> filePathes,
            string argName = null)
        {
            ValidateArgumentNull(filePathes, argName);

            if (!filePathes.Any())
            {
                throw
                    (argName == null) ?
                        new ArgumentException(@"The value is empty.") :
                        new ArgumentException(@"The value is empty.", argName);
            }

            foreach (var v in filePathes.Select((filePath, i) => new { filePath, i }))
            {
                ValidateFilePath(
                    v.filePath,
                    (argName == null) ? null : (argName + '[' + v.i + ']'));
            }
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
        /// AviUtl拡張編集ウィンドウタイトルプレフィクス。
        /// </summary>
        private const string ExEditWindowTitlePrefix = @"拡張編集";

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
                    return Result.GcmzWindowNotFound;
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
