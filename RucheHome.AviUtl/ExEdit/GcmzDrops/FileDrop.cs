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
            /// 拡張編集ウィンドウが閉じられているか最小化されている。
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
        /// レイヤー番号の最小値。
        /// </summary>
        public const int MinLayer = 1;

        /// <summary>
        /// レイヤー番号の最大値。
        /// </summary>
        public const int MaxLayer = 100;

        /// <summary>
        /// 既定のレイヤー位置指定値。
        /// </summary>
        public const int DefaultLayerPosition = -MinLayer;

        /// <summary>
        /// ファイルドロップ処理を行う。
        /// </summary>
        /// <param name="ownWindowHandle">
        /// WM_COPYDATA メッセージ送信元ウィンドウハンドル。
        /// </param>
        /// <param name="filePath">ファイルパス。</param>
        /// <param name="stepFrameCount">ドロップ後に進めるフレーム数。</param>
        /// <param name="layer">
        /// レイヤー位置指定。
        /// レイヤー番号で指定するならば MinLayer 以上 MaxLayer 以下。
        /// 相対位置で指定するならば -MinLayer 以下 -MaxLayer 以上。
        /// 既定位置にするならば 0 。
        /// </param>
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
        /// <param name="ownWindowHandle">
        /// WM_COPYDATA メッセージ送信元ウィンドウハンドル。
        /// </param>
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
            ValidateArguments(filePathes, stepFrameCount, layer);

            // 処理対象ウィンドウ取得
            var result = ReadTargetWindow(out var targetWindow);
            if (result != Result.Success)
            {
                return result;
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
                    ((layer == 0) ? DefaultLayerPosition : layer) + "\0" +
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
        /// AviUtl拡張編集ウィンドウタイトルプレフィクス。
        /// </summary>
        private const string ExEditWindowTitlePrefix = @"拡張編集";

        /// <summary>
        /// Run メソッドの引数群を検証する。
        /// </summary>
        /// <param name="filePathes">ファイルパス列挙。</param>
        /// <param name="stepFrameCount">ドロップ後に進めるフレーム数。</param>
        /// <param name="layer">レイヤー位置指定。既定位置にするならば 0 。</param>
        private static void ValidateArguments(
            IEnumerable<string> filePathes,
            int stepFrameCount,
            int layer)
        {
            ValidateFilePathes(filePathes, nameof(filePathes));

            ValidateArgumentOutOfRange(
                stepFrameCount,
                0,
                int.MaxValue,
                nameof(stepFrameCount));

            if (layer != 0)
            {
                ValidateArgumentOutOfRange(
                    layer,
                    (layer < 0) ? -MaxLayer : MinLayer,
                    (layer < 0) ? -MinLayer : MaxLayer,
                    nameof(layer));
            }
        }

        /// <summary>
        /// 『ごちゃまぜドロップス』から対象ウィンドウハンドルを読み取る。
        /// </summary>
        /// <param name="dest">対象ウィンドウハンドルの設定先。</param>
        /// <returns>処理結果。</returns>
        private static Result ReadTargetWindow(out Win32Window dest)
        {
            dest = null;

            switch (GcmzInfoReader.Read(out var info))
            {
            case GcmzInfoReader.Result.Success:
                {
                    if (!info.IsWindowOpened)
                    {
                        return Result.GcmzWindowNotFound;
                    }
                    if (!info.IsProjectOpened)
                    {
                        return Result.ProjectNotFound;
                    }

                    var win = new Win32Window(info.WindowHandle);
                    if (!win.IsExists)
                    {
                        return Result.GcmzWindowNotFound;
                    }

                    dest = win;
                }
                break;

            case GcmzInfoReader.Result.FileMappingFail:
                return Result.FileMappingFail;

            case GcmzInfoReader.Result.MapViewFail:
                return Result.MapViewFail;

            default:
                // 来ないはず…
                return Result.Fail;
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

        private const uint WM_COPYDATA = 0x004A;

        #endregion
    }
}
