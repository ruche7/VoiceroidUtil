using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RucheHome.Threading;
using RucheHome.Util;

namespace RucheHome.Voiceroid
{
    partial class ProcessFactory
    {
        /// <summary>
        /// IProcess インタフェース実装の抽象基底クラス。
        /// </summary>
        private abstract class ImplBase : BindableBase, IProcess, IDisposable
        {
            /// <summary>
            /// コンストラクタ＞
            /// </summary>
            /// <param name="id">VOICEROID識別ID。</param>
            public ImplBase(VoiceroidId id)
            {
                if (!Enum.IsDefined(id.GetType(), id))
                {
                    throw new InvalidEnumArgumentException(
                        nameof(id),
                        (int)id,
                        id.GetType());
                }

                this.Id = id;
            }

            /// <summary>
            /// デストラクタ。
            /// </summary>
            ~ImplBase()
            {
                this.Dispose(false);
            }

            /// <summary>
            /// アプリプロセス列挙を基に状態を更新する。
            /// </summary>
            /// <param name="appProcesses">アプリプロセス列挙。</param>
            public async Task Update(IEnumerable<Process> appProcesses)
            {
                using (var updateLock = await this.UpdateLock.WaitAsync())
                {
                    // 対象プロセスを検索
                    var app = appProcesses?.FirstOrDefault(p => this.IsOwnProcess(p));
                    if (app == null)
                    {
                        this.SetupDeadState();
                        return;
                    }

                    // 状態更新
                    await this.UpdateState(app);
                }
            }

            /// <summary>
            /// UI操作のタイムアウトミリ秒数。
            /// </summary>
            protected const int UIControlTimeout = 1500;

            /// <summary>
            /// 戻り値が条件を満たさない間、デリゲートを呼び出し続ける。
            /// </summary>
            /// <typeparam name="T">戻り値の型。</typeparam>
            /// <param name="func">デリゲート。</param>
            /// <param name="condition">終了条件デリゲート。</param>
            /// <param name="loopCount">ループ回数。負数ならば制限無し。</param>
            /// <param name="intervalMilliseconds">ループ間隔ミリ秒数。</param>
            /// <returns>条件を満たした時、もしくはループ終了時の戻り値。</returns>
            protected static Task<T> RepeatUntil<T>(
                Func<T> func,
                Func<T, bool> condition,
                int loopCount = -1,
                int intervalMilliseconds = 20)
            {
                return
                    RepeatUntil(
                        () => Task.Run(func),
                        condition,
                        loopCount,
                        intervalMilliseconds);
            }

            /// <summary>
            /// 戻り値が条件を満たさない間、非同期デリゲートを呼び出し続ける。
            /// </summary>
            /// <typeparam name="T">戻り値の型。</typeparam>
            /// <param name="funcAsync">非同期デリゲート。</param>
            /// <param name="condition">終了条件デリゲート。</param>
            /// <param name="loopCount">ループ回数。負数ならば制限無し。</param>
            /// <param name="intervalMilliseconds">ループ間隔ミリ秒数。</param>
            /// <returns>条件を満たした時、もしくはループ終了時の戻り値。</returns>
            protected static async Task<T> RepeatUntil<T>(
                Func<Task<T>> funcAsync,
                Func<T, bool> condition,
                int loopCount = -1,
                int intervalMilliseconds = 20)
            {
                T value = await funcAsync();

                for (int i = 0; !condition(value) && (loopCount < 0 || i < loopCount); ++i)
                {
                    await Task.Delay(intervalMilliseconds);
                    value = await funcAsync();
                }

                return value;
            }

            /// <summary>
            /// アプリプロセスを取得または設定する。
            /// </summary>
            /// <remarks>
            /// 設定時、 ExecutablePath も更新される。
            /// </remarks>
            protected Process AppProcess
            {
                get { return this.appProcess; }
                set
                {
                    this.appProcess = value;
                    this.ExecutablePath = value?.MainModule?.FileName;
                }
            }
            private Process appProcess = null;

            /// <summary>
            /// アプリプロセスが入力待機状態になるまで非同期で待機する。
            /// </summary>
            /// <param name="loopCount">
            /// 最大ループ回数。 0 ならば状態確認結果を即座に返す。
            /// </param>
            /// <param name="loopIntervalMilliseconds">ループ間隔ミリ秒数。</param>
            /// <returns>入力待機状態になったならば true 。そうでなければ false 。</returns>
            protected async Task<bool> WhenForInputHandle(
                int loopCount = 25,
                int loopIntervalMilliseconds = 20)
            {
                bool? result = this.AppProcess?.WaitForInputIdle(0);

                for (int i = 0; result == false && i < loopCount; ++i)
                {
                    await Task.Delay(loopIntervalMilliseconds);
                    result = this.AppProcess?.WaitForInputIdle(0);
                }

                return (result == true);
            }

            /// <summary>
            /// IsDialogShowing プロパティ値を更新する。
            /// </summary>
            /// <returns>更新した値。</returns>
            protected async Task<bool> UpdateDialogShowing()
            {
                this.IsDialogShowing = await this.CheckDialogShowing();
                return this.IsDialogShowing;
            }

            /// <summary>
            /// アプリプロセス実行中ではない場合の状態セットアップを行う。
            /// </summary>
            protected virtual void SetupDeadState()
            {
                this.AppProcess = null;
                this.mainWindowHandle = IntPtr.Zero;
                this.IsStartup = false;
                this.IsRunning = false;
                this.IsSaving = false;
                this.IsDialogShowing = false;
            }

            /// <summary>
            /// メインウィンドウ変更時の更新処理を行う。
            /// </summary>
            /// <returns>更新できたならば true 。そうでなければ false 。</returns>
            protected abstract Task<bool> UpdateOnMainWindowChanged();

            /// <summary>
            /// 現在WAVEファイル保存処理中であるか否か調べる。
            /// </summary>
            /// <returns>保存処理中ならば true 。そうでなければ false 。</returns>
            /// <remarks>
            /// 直接本体側を操作して保存処理を行っている場合にも true を返すこと。
            /// </remarks>
            protected abstract Task<bool> CheckSaving();

            /// <summary>
            /// 現在再生中であるか否か調べる。
            /// </summary>
            /// <returns>再生中ならば true 。そうでなければ false 。</returns>
            /// <remarks>
            /// 直接本体側を操作して再生処理を行っている場合にも true を返すこと。
            /// </remarks>
            protected abstract Task<bool> CheckPlaying();

            /// <summary>
            /// 現在ダイアログ表示中であるか否か調べる。
            /// </summary>
            /// <returns>ダイアログ表示中ならば true 。そうでなければ false 。</returns>
            /// <remarks>
            /// 直接本体側を操作してダイアログ表示を行っている場合にも true を返すこと。
            /// </remarks>
            protected abstract Task<bool> CheckDialogShowing();

            /// <summary>
            /// WAVEファイル保存処理を行える状態であるか否か調べる。
            /// </summary>
            /// <returns>行える状態ならば true 。そうでなければ false 。</returns>
            protected abstract Task<bool> CanSave();

            /// <summary>
            /// WAVEファイル保存の実処理を行う。
            /// </summary>
            /// <param name="filePath">保存希望WAVEファイルパス。</param>
            /// <returns>保存処理結果。</returns>
            protected abstract Task<FileSaveResult> DoSave(string filePath);

            /// <summary>
            /// WAVEファイルパスを作成する。
            /// </summary>
            /// <param name="filePath">基となるファイルパス。</param>
            /// <returns>WAVEファイルパス。作成できなかった場合は null 。</returns>
            private static string MakeWaveFilePath(string filePath)
            {
                if (filePath == null)
                {
                    throw new ArgumentNullException(nameof(filePath));
                }

                try
                {
                    var path = Path.GetFullPath(filePath);
                    if (Path.GetExtension(path)?.ToLower() != @".wav")
                    {
                        path += @".wav";
                    }

                    return path;
                }
                catch { }

                return null;
            }

            /// <summary>
            /// ディレクトリを作成する。
            /// </summary>
            /// <param name="dirPath">ディレクトリパス。</param>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            private static bool MakeDirectory(string dirPath)
            {
                if (dirPath == null)
                {
                    throw new ArgumentNullException(nameof(dirPath));
                }

                if (!Directory.Exists(dirPath))
                {
                    try
                    {
                        Directory.CreateDirectory(dirPath);
                    }
                    catch
                    {
                        return false;
                    }
                }

                return true;
            }

            /// <summary>
            /// ディレクトリへの書き込み権限があるか否かを調べる。
            /// </summary>
            /// <param name="dirPath">ディレクトリパス。</param>
            /// <returns>
            /// 書き込み権限があるならば true 。そうでなければ false 。
            /// </returns>
            private static bool CheckDirectoryWritable(string dirPath)
            {
                if (dirPath == null)
                {
                    throw new ArgumentNullException(nameof(dirPath));
                }

                // 一時ファイルパス作成
                string tempPath;
                do
                {
                    tempPath = Path.Combine(dirPath, Path.GetRandomFileName());
                }
                while (File.Exists(tempPath));

                // 実際に書き出してみて調べる
                try
                {
                    File.WriteAllBytes(tempPath, new byte[] { 0 });
                }
                catch
                {
                    return false;
                }
                finally
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch { }
                }

                return true;
            }

            /// <summary>
            /// 更新処理の排他制御を行うオブジェクトを取得する。
            /// </summary>
            private SemaphoreSlimLock UpdateLock { get; } = new SemaphoreSlimLock(1);

            /// <summary>
            /// 音声保存処理の排他制御を行うオブジェクトを取得する。
            /// </summary>
            private SemaphoreSlimLock SaveLock { get; } = new SemaphoreSlimLock(1);

            /// <summary>
            /// 操作対象アプリプロセスであるか否かを取得する。
            /// </summary>
            /// <param name="appProcess">調べるアプリプロセス。</param>
            /// <returns>
            /// 操作対象アプリプロセスであるならば true 。そうでなければ false 。
            /// </returns>
            private bool IsOwnProcess(Process appProcess)
            {
                try
                {
                    return (
                        appProcess != null &&
                        !appProcess.HasExited &&
                        appProcess.MainModule.FileVersionInfo.ProductName == this.Product);
                }
                catch (Win32Exception ex)
                {
                    // VOICEROID起動時に Process.MainModule プロパティへのアクセスで
                    // 複数回発生する場合がある
                    // 起動しきっていない時にアクセスしようとしているせい？
                    ThreadDebug.WriteException(ex);
                }

                return false;
            }

            /// <summary>
            /// アプリプロセス情報から状態を更新する。
            /// </summary>
            /// <param name="appProcess">アプリプロセス。</param>
            private async Task UpdateState(Process appProcess)
            {
                if (appProcess == null)
                {
                    throw new ArgumentNullException(nameof(appProcess));
                }

                // メインウィンドウタイトルが空文字列なら
                // スプラッシュウィンドウやメニューウィンドウがメインウィンドウになっている
                if (appProcess.MainWindowTitle == "")
                {
                    // 実行中でないなら起動中と判断
                    this.IsStartup |= !this.IsRunning;
                    return;
                }


                // 保存タスク処理中なら更新しない
                if (this.IsRunning && this.SaveLock.CurrentCount <= 0)
                {
                    return;
                }

                // 念のため待機
                using (var saveLock = await this.SaveLock.WaitAsync())
                {
                    this.AppProcess = appProcess;
                    appProcess.Refresh();

                    // 入力待機状態になっていない？
                    if (
                        !(await this.WhenForInputHandle(0)) ||
                        appProcess.MainWindowHandle == IntPtr.Zero)
                    {
                        // 実行中でないなら起動中と判断
                        this.IsStartup |= !this.IsRunning;
                        return;
                    }

                    // 現在と同じウィンドウが取得できた場合はスキップ
                    if (
                        !this.IsRunning ||
                        this.MainWindowHandle != appProcess.MainWindowHandle)
                    {
                        // ウィンドウ変更時の更新処理
                        this.MainWindowHandle = appProcess.MainWindowHandle;
                        if (!(await this.UpdateOnMainWindowChanged()))
                        {
                            this.SetupDeadState();
                            return;
                        }
                    }

                    this.IsRunning = true;
                    this.IsStartup = false;
                    this.IsSaving = await this.CheckSaving();
                    this.IsPlaying = (!this.IsSaving && (await this.CheckPlaying()));
                    await this.UpdateDialogShowing();
                }
            }

            #region IProcess の実装

            /// <summary>
            /// VOICEROID識別IDを取得する。
            /// </summary>
            public VoiceroidId Id { get; }

            /// <summary>
            /// VOICEROID名を取得する。
            /// </summary>
            public string Name => this.Id.GetInfo().Name;

            /// <summary>
            /// プロダクト名を取得する。
            /// </summary>
            public string Product => this.Id.GetInfo().Product;

            /// <summary>
            /// 表示プロダクト名を取得する。
            /// </summary>
            public string DisplayProduct => this.Id.GetInfo().DisplayProduct;

            /// <summary>
            /// 実行ファイルのパスを取得する。
            /// </summary>
            /// <remarks>
            /// プロセスが見つかっていない場合は null を返す。
            /// </remarks>
            public string ExecutablePath
            {
                get { return this.executablePath; }
                private set { this.SetProperty(ref this.executablePath, value); }
            }
            private string executablePath = null;

            /// <summary>
            /// メインウィンドウハンドルを取得する。
            /// </summary>
            /// <remarks>
            /// メインウィンドウが見つかっていない場合は IntPtr.Zero を返す。
            /// </remarks>
            public IntPtr MainWindowHandle
            {
                get { return this.mainWindowHandle; }
                private set { this.SetProperty(ref this.mainWindowHandle, value); }
            }
            private IntPtr mainWindowHandle = IntPtr.Zero;

            /// <summary>
            /// プロセスが起動中であるか否かを取得する。
            /// </summary>
            public bool IsStartup
            {
                get => this.startup;
                private set => this.SetProperty(ref this.startup, value);
            }
            private bool startup = false;

            /// <summary>
            /// プロセスが実行中であるか否かを取得する。
            /// </summary>
            public bool IsRunning
            {
                get => this.running;
                private set => this.SetProperty(ref this.running, value);
            }
            private bool running = false;

            /// <summary>
            /// トークテキストを再生中であるか否かを取得する。
            /// </summary>
            public bool IsPlaying
            {
                get => this.playing;
                private set => this.SetProperty(ref this.playing, value);
            }
            private bool playing = false;

            /// <summary>
            /// トークテキストをWAVEファイル保存中であるか否かを取得する。
            /// </summary>
            public bool IsSaving
            {
                get => this.saving;
                private set => this.SetProperty(ref this.saving, value);
            }
            private bool saving = false;

            /// <summary>
            /// いずれかのダイアログが表示中であるか否かを取得する。
            /// </summary>
            public bool IsDialogShowing
            {
                get => this.dialogShowing;
                protected set => this.SetProperty(ref this.dialogShowing, value);
            }
            private bool dialogShowing = false;

            /// <summary>
            /// トークテキストを取得する。
            /// </summary>
            /// <returns>トークテキスト。取得できなかったならば null 。</returns>
            public abstract Task<string> GetTalkText();

            /// <summary>
            /// トークテキストを設定する。
            /// </summary>
            /// <param name="text">トークテキスト。</param>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            /// <remarks>
            /// 再生中の場合は停止させる。WAVEファイル保存中である場合は失敗する。
            /// </remarks>
            public abstract Task<bool> SetTalkText(string text);

            /// <summary>
            /// トークテキストの再生を開始する。
            /// </summary>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            /// <remarks>
            /// 再生中の場合は何もせず true を返す。
            /// WAVEファイル保存中である場合やトークテキストが空白である場合は失敗する。
            /// </remarks>
            public abstract Task<bool> Play();

            /// <summary>
            /// トークテキストの再生を停止する。
            /// </summary>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            /// <remarks>
            /// WAVEファイル保存中である場合は失敗する。
            /// </remarks>
            public abstract Task<bool> Stop();

            /// <summary>
            /// トークテキストをWAVEファイル保存する。
            /// </summary>
            /// <param name="filePath">保存希望WAVEファイルパス。</param>
            /// <returns>保存処理結果。</returns>
            /// <remarks>
            /// 再生中の場合は停止させる。
            /// WAVEファイル保存中である場合やトークテキストが空白である場合は失敗する。
            /// 
            /// 既に同じ名前のWAVEファイルが存在する場合は上書きする。
            /// 
            /// 本体側の設定次第では、
            /// テキストファイルが同時に保存されたり連番ファイルとして保存されたりする。
            /// </remarks>
            public async Task<FileSaveResult> Save(string filePath)
            {
                if (filePath == null)
                {
                    throw new ArgumentNullException(nameof(filePath));
                }

                if (!(await this.CanSave()))
                {
                    return new FileSaveResult(
                        false,
                        error: @"ファイル保存を開始できませんでした。");
                }

                FileSaveResult result;

                using (var saveLock = await this.SaveLock.WaitAsync())
                {
                    this.IsSaving = true;

                    // ファイルパス作成
                    var path = MakeWaveFilePath(filePath);
                    if (path == null)
                    {
                        return new FileSaveResult(
                            false,
                            error: @"ファイル名を作成できませんでした。");
                    }

                    // 保存先ディレクトリ作成
                    if (!MakeDirectory(Path.GetDirectoryName(path)))
                    {
                        return new FileSaveResult(
                            false,
                            error: @"保存先フォルダーを作成できませんでした。");
                    }

                    // 保存先ディレクトリの書き込み権限確認
                    if (!CheckDirectoryWritable(Path.GetDirectoryName(path)))
                    {
                        return new FileSaveResult(
                            false,
                            error: @"保存先フォルダーへの書き込み権限がありません。");
                    }

                    result = await this.DoSave(path);
                }

                return result;
            }

            /// <summary>
            /// 指定した実行ファイルをVOICEROIDプロセスとして実行する。
            /// </summary>
            /// <param name="executablePath">実行ファイルパス。</param>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            public async Task<bool> Run(string executablePath)
            {
                if (executablePath == null)
                {
                    throw new ArgumentNullException(nameof(executablePath));
                }
                if (!File.Exists(executablePath))
                {
                    throw new FileNotFoundException(nameof(executablePath));
                }

                if (this.AppProcess != null)
                {
                    return false;
                }

                using (var updateLock = await this.UpdateLock.WaitAsync())
                {
                    if (this.AppProcess != null)
                    {
                        return false;
                    }

                    // プロセス実行
                    bool ok =
                        await Task.Run(
                            () =>
                            {
                                var app = Process.Start(executablePath);
                                app.WaitForInputIdle(UIControlTimeout);

                                if (!this.IsOwnProcess(app))
                                {
                                    if (!app.CloseMainWindow())
                                    {
                                        app.Kill();
                                    }
                                    app.Close();
                                    return false;
                                }

                                return true;
                            });
                    if (!ok)
                    {
                        return false;
                    }
                }

                // スタートアップ状態になるまで少し待つ
                return
                    await RepeatUntil(() => this.IsStartup || this.IsRunning, f => f, 25);
            }

            /// <summary>
            /// VOICEROIDプロセスを終了させる。
            /// </summary>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            public async Task<bool> Exit()
            {
                if (this.AppProcess == null)
                {
                    return false;
                }

                using (var updateLock = await this.UpdateLock.WaitAsync())
                {
                    if (this.AppProcess == null)
                    {
                        return false;
                    }

                    // プロセス終了
                    bool ok =
                        await Task.Run(
                            () =>
                            {
                                if (!this.AppProcess.CloseMainWindow())
                                {
                                    return false;
                                }
                                if (!this.AppProcess.WaitForExit(UIControlTimeout))
                                {
                                    return false;
                                }
                                return true;
                            });
                    if (!ok)
                    {
                        await this.UpdateDialogShowing();
                        return false;
                    }

                    this.SetupDeadState();
                }

                return true;
            }

            /// <summary>
            /// ボイスプリセット名を取得する。
            /// </summary>
            /// <returns>ボイスプリセット名。</returns>
            /// <remarks>
            /// 既定では Name の値を返す。
            /// </remarks>
            public virtual async Task<string> GetVoicePresetName()
            {
                // 単に Name の値を返す
                return await Task.FromResult(this.Name);
            }

            #endregion

            #region IDisposable の実装

            /// <summary>
            /// リソースを破棄する。
            /// </summary>
            public void Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// リソース破棄の実処理を行う。
            /// </summary>
            /// <param name="disposing">
            /// Dispose メソッドから呼び出された場合は true 。
            /// </param>
            protected virtual void Dispose(bool disposing)
            {
                this.UpdateLock.Dispose();
                this.SaveLock.Dispose();
            }

            #endregion
        }
    }
}
