using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Codeplex.Data;
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

            /// <summary>
            /// ミューテクスオープン試行時例外。
            /// </summary>
            MutexOpenFail,

            /// <summary>
            /// ミューテクスロック失敗。
            /// </summary>
            MutexLockFail,

            /// <summary>
            /// ミューテクスロックタイムアウト。
            /// </summary>
            MutexLockTimeout,
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

            // 拡張編集ウィンドウが表示されていないと失敗するので確認
            var exEditWindow =
                Win32Window.FromDesktop()
                    .FindChildren()
                    .FirstOrDefault(
                        win =>
                            win.ClassName == @"AviUtl" &&
                            win
                                .GetText(timeoutMilliseconds)?
                                .StartsWith(ExEditWindowTitlePrefix) == true);
            if (exEditWindow == null || !exEditWindow.IsExists)
            {
                return Result.ExEditWindowNotFound;
            }

            // 『ごちゃまぜドロップス』 v0.3.12 以降であればミューテクスによる排他制御が可能
            Mutex mutex;
            try
            {
                // 開けなければ mutex は null
                Mutex.TryOpenExisting(GcmzMutexName, out mutex);
            }
            catch (Exception ex)
            {
                ThreadTrace.WriteException(ex);
                return Result.MutexOpenFail;
            }

            var data = new COPYDATASTRUCT();
            var lParam = IntPtr.Zero;
            bool mutexLocked = false;

            try
            {
                // 『ごちゃまぜドロップス』共有メモリ情報読み取り＆検証
                // v0.3.12 以降ならば外部連携APIバージョン付きの情報を読み取る
                var result = ReadAndValidateGcmzInfo(out var gcmzInfo, mutex != null);
                if (result != Result.Success)
                {
                    return result;
                }

                // 外部連携APIバージョン 2 以降は拡張編集ウィンドウ非表示状態でもドロップ可能
                // 正確には、外部連携APIバージョン 2 になったのは v0.3.23 、
                // 非表示状態で正常にドロップできるようになったのは v0.3.25 以降だが許容する
                if (gcmzInfo.ApiVersion < 2 && !exEditWindow.IsVisible)
                {
                    return Result.ExEditWindowInvisible;
                }

                // COPYDATASTRUCT 作成
                // 『ごちゃまぜドロップス』 v0.3.11 以前なら旧フォーマットを使う
                var gcmzLayer = (layer == 0) ? -MinLayer : layer;
                data =
                    (mutex == null) ?
                        CreateCopyDataStructLegacy(gcmzLayer, stepFrameCount, filePathes) :
                        CreateCopyDataStruct(gcmzLayer, stepFrameCount, filePathes);

                // LPARAM 値作成
                lParam = Marshal.AllocHGlobal(Marshal.SizeOf(data));
                Marshal.StructureToPtr(data, lParam, false);

                // ミューテクスが有効なら排他制御開始
                if (mutex != null)
                {
                    try
                    {
                        if (!mutex.WaitOne((timeoutMilliseconds < 0) ? -1 : timeoutMilliseconds))
                        {
                            return Result.MutexLockTimeout;
                        }
                        mutexLocked = true;
                    }
                    catch (Exception ex)
                    {
                        ThreadTrace.WriteException(ex);
                        return Result.MutexLockFail;
                    }
                }

                // WM_COPYDATA メッセージ送信
                var msgRes =
                    new Win32Window(gcmzInfo.WindowHandle)
                        .SendMessage(WM_COPYDATA, ownWindowHandle, lParam, timeoutMilliseconds);
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
                if (mutex != null)
                {
                    if (mutexLocked)
                    {
                        mutex.ReleaseMutex();
                    }
                    mutex.Dispose();
                }
                if (lParam != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(lParam);
                }
                if (data.DataAddress != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(data.DataAddress);
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
        /// 『ごちゃまぜドロップス』のミューテクス名。
        /// </summary>
        private const string GcmzMutexName = @"GCMZDropsMutex";

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
        /// 『ごちゃまぜドロップス』の共有メモリ情報を読み取り、状態を検証する。
        /// </summary>
        /// <param name="dest">共有メモリ情報の設定先。</param>
        /// <param name="tryRead0312">
        /// 『ごちゃまぜドロップス』 v0.3.12 以降の共有メモリ情報読み取りを試みるならば true 。
        /// </param>
        /// <returns>検証結果。</returns>
        private static Result ReadAndValidateGcmzInfo(
            out GcmzInfo dest,
            bool tryRead0312 = false)
        {
            dest = null;

            switch (GcmzInfoReader.Read(out dest, tryRead0312))
            {
            case GcmzInfoReader.Result.Success:
                {
                    if (!dest.IsWindowOpened)
                    {
                        return Result.GcmzWindowNotFound;
                    }
                    if (!dest.IsProjectOpened)
                    {
                        return Result.ProjectNotFound;
                    }
                    if (!new Win32Window(dest.WindowHandle).IsExists)
                    {
                        return Result.GcmzWindowNotFound;
                    }
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

        /// <summary>
        /// 『ごちゃまぜドロップス』 v0.3.12 以降用の COPYDATASTRUCT 値を作成する。
        /// </summary>
        /// <param name="layer">レイヤー位置指定。</param>
        /// <param name="frameAdvance">ドロップ後に進めるフレーム数。</param>
        /// <param name="files">ファイルパス列挙。</param>
        /// <returns>
        /// COPYDATASTRUCT 値。
        /// DataAddress フィールドは利用後に Marshal.FreeHGlobal で解放する必要がある。
        /// </returns>
        private static COPYDATASTRUCT CreateCopyDataStruct(
            int layer,
            int frameAdvance,
            IEnumerable<string> files)
        {
            // 送信JSON文字列作成
            var json = DynamicJson.Serialize(new { layer, frameAdvance, files });
            var data = Encoding.UTF8.GetBytes(json);

            // COPYDATASTRUCT 作成
            var cds = new COPYDATASTRUCT();
            try
            {
                cds.Param = new UIntPtr(1);
                cds.DataSize = data.Length;
                cds.DataAddress = Marshal.AllocHGlobal(data.Length);
                Marshal.Copy(data, 0, cds.DataAddress, data.Length);
            }
            catch
            {
                if (cds.DataAddress != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(cds.DataAddress);
                }
                throw;
            }

            return cds;
        }

        /// <summary>
        /// 『ごちゃまぜドロップス』 v0.3.11 以前用の COPYDATASTRUCT 値を作成する。
        /// </summary>
        /// <param name="layer">レイヤー位置指定。</param>
        /// <param name="frameAdvance">ドロップ後に進めるフレーム数。</param>
        /// <param name="files">ファイルパス列挙。</param>
        /// <returns>
        /// COPYDATASTRUCT 値。
        /// DataAddress フィールドは利用後に Marshal.FreeHGlobal で解放する必要がある。
        /// </returns>
        private static COPYDATASTRUCT CreateCopyDataStructLegacy(
            int layer,
            int frameAdvance,
            IEnumerable<string> files)
        {
            // 送信文字列作成
            var data = $"{layer}\0{frameAdvance}\0{string.Join("\0", files)}\0";

            // COPYDATASTRUCT 作成
            var cds = new COPYDATASTRUCT();
            try
            {
                cds.Param = UIntPtr.Zero;
                cds.DataSize = data.Length * 2;
                cds.DataAddress = Marshal.StringToHGlobalUni(data);
            }
            catch
            {
                if (cds.DataAddress != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(cds.DataAddress);
                }
                throw;
            }

            return cds;
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
