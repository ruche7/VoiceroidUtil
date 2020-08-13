using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Automation;
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
            /// <param name="canSaveBlankText">空白文の音声保存を行えるならば true 。</param>
            public ImplBase(VoiceroidId id, bool canSaveBlankText)
            {
                if (!Enum.IsDefined(id.GetType(), id))
                {
                    throw new InvalidEnumArgumentException(
                        nameof(id),
                        (int)id,
                        id.GetType());
                }

                this.Id = id;
                this.CanSaveBlankText = canSaveBlankText;
            }

            /// <summary>
            /// デストラクタ。
            /// </summary>
            ~ImplBase() => this.Dispose(false);

            /// <summary>
            /// アプリプロセス列挙を基に状態を更新する。
            /// </summary>
            /// <param name="appProcesses">アプリプロセス列挙。</param>
            public async Task Update(IEnumerable<Process> appProcesses)
            {
                using (var updateLock = await this.UpdateLock.WaitAsync())
                {
                    // 対象プロセスを検索
                    var app =
                        appProcesses?.FirstOrDefault(
                            p =>
                            {
                                try
                                {
                                    return this.IsOwnProcess(p);
                                }
                                catch { }
                                return false;
                            });
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
                =>
                RepeatUntil(
                    () => Task.Run(func),
                    condition,
                    loopCount,
                    intervalMilliseconds);

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

            #region AutomationElement 関連便利メソッド群

            /// <summary>
            /// ウィンドウハンドルから AutomationElement を作成する。
            /// </summary>
            /// <param name="handle">ウィンドウハンドル。</param>
            /// <returns>AutomationElement 。作成できなかった場合は null 。</returns>
            protected static AutomationElement MakeElementFromHandle(IntPtr handle)
            {
                try
                {
                    return AutomationElement.FromHandle(handle);
                }
                catch { }
                return null;
            }

            /// <summary>
            /// AutomationElement の子を検索する。
            /// </summary>
            /// <param name="element">検索対象 AutomationElement 。</param>
            /// <param name="condition">検索条件。 null ならば常にマッチする。</param>
            /// <returns>見つかった AutomationElement 。見つからなければ null 。</returns>
            protected static AutomationElement FindFirstChild(
                AutomationElement element,
                Condition condition)
            {
                try
                {
                    return
                        element?.FindFirst(
                            TreeScope.Children,
                            condition ?? Condition.TrueCondition);
                }
                catch { }
                return null;
            }

            /// <summary>
            /// AutomationElement の子を検索する。
            /// </summary>
            /// <param name="element">検索対象 AutomationElement 。</param>
            /// <param name="property">検索条件プロパティ種別。</param>
            /// <param name="propertyValue">検索条件プロパティ名。</param>
            /// <returns>見つかった AutomationElement 。見つからなければ null 。</returns>
            protected static AutomationElement FindFirstChild(
                AutomationElement element,
                AutomationProperty property,
                object propertyValue)
                =>
                (element == null || property == null) ?
                    null :
                    FindFirstChild(
                        element,
                        new PropertyCondition(property, propertyValue));

            /// <summary>
            /// AutomationElement の子をコントロール種別で検索する。
            /// </summary>
            /// <param name="element">検索対象 AutomationElement 。</param>
            /// <param name="controlType">検索条件コントロール種別。</param>
            /// <returns>見つかった AutomationElement 。見つからなければ null 。</returns>
            protected static AutomationElement FindFirstChildByControlType(
                AutomationElement element,
                ControlType controlType)
                =>
                FindFirstChild(
                    element,
                    AutomationElement.ControlTypeProperty,
                    controlType);

            /// <summary>
            /// AutomationElement の子をオートメーションIDで検索する。
            /// </summary>
            /// <param name="element">検索対象 AutomationElement 。</param>
            /// <param name="automationId">検索条件オートメーションID。</param>
            /// <returns>見つかった AutomationElement 。見つからなければ null 。</returns>
            protected static AutomationElement FindFirstChildByAutomationId(
                AutomationElement element,
                string automationId)
                =>
                FindFirstChild(
                    element,
                    AutomationElement.AutomationIdProperty,
                    automationId);

            /// <summary>
            /// AutomationElement の子ウィンドウを列挙する。
            /// </summary>
            /// <param name="element">検索対象 AutomationElement 。</param>
            /// <permission cref="name">
            /// 検索対象 Name プロパティ値。限定しないならば null 。
            /// </permission>
            /// <returns>子ウィンドウ列挙。</returns>
            protected static IEnumerable<AutomationElement> FindChildWindows(
                AutomationElement element,
                string name = null)
            {
                if (element == null)
                {
                    throw new ArgumentNullException(nameof(element));
                }

                try
                {
                    Condition cond =
                        new PropertyCondition(
                            AutomationElement.ControlTypeProperty,
                            ControlType.Window);
                    if (name != null)
                    {
                        cond =
                            new AndCondition(
                                cond,
                                new PropertyCondition(AutomationElement.NameProperty, name));
                    }

                    return
                        element
                            .FindAll(TreeScope.Children, cond)
                            .OfType<AutomationElement>();
                }
                catch { }
                return Enumerable.Empty<AutomationElement>();
            }

            /// <summary>
            /// AutomationElement の ValuePattern から文字列値を取得する。
            /// </summary>
            /// <param name="element">取得対象の AutomationElement 。</param>
            /// <returns>文字列値。取得できなかったならば null 。</returns>
            protected static string GetElementValue(AutomationElement element)
            {
                if (element == null)
                {
                    throw new ArgumentNullException(nameof(element));
                }

                try
                {
                    return
                        element.TryGetCurrentPattern(ValuePattern.Pattern, out var pattern) ?
                            ((ValuePattern)pattern).Current.Value : null;
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                }

                return null;
            }

            /// <summary>
            /// AutomationElement の ValuePattern に文字列値を設定する。
            /// </summary>
            /// <param name="element">設定対象の AutomationElement 。</param>
            /// <param name="value">設定する文字列値。</param>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            protected static bool SetElementValue(AutomationElement element, string value)
            {
                if (element == null)
                {
                    throw new ArgumentNullException(nameof(element));
                }

                try
                {
                    if (!element.TryGetCurrentPattern(ValuePattern.Pattern, out var pattern))
                    {
                        return false;
                    }

                    var vp = (ValuePattern)pattern;
                    if (vp.Current.IsReadOnly)
                    {
                        ThreadDebug.WriteLine(@"The element is readonly.");
                        return false;
                    }
                    vp.SetValue(value);
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                    return false;
                }

                return true;
            }

            /// <summary>
            /// AutomationElement の Invoke 操作を行う。
            /// </summary>
            /// <param name="element">操作対象の AutomationElement 。</param>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            protected static bool InvokeElement(AutomationElement element)
            {
                if (element == null)
                {
                    throw new ArgumentNullException(nameof(element));
                }

                try
                {
                    if (!element.Current.IsEnabled)
                    {
                        return false;
                    }

                    if (!element.TryGetCurrentPattern(InvokePattern.Pattern, out var pattern))
                    {
                        return false;
                    }

                    ((InvokePattern)pattern).Invoke();
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                    return false;
                }

                return true;
            }

            #endregion

            /// <summary>
            /// アプリプロセスを取得または設定する。
            /// </summary>
            /// <remarks>
            /// 設定時、 ExecutablePath も更新される。
            /// </remarks>
            protected Process AppProcess
            {
                get => this.appProcess;
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
            protected async Task<bool> WhenForInputIdle(
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

            #region 音声保存処理補助

            /// <summary>
            /// ファイルダイアログからOKボタンとファイル名エディットの
            /// AutomationElement を検索する処理を行う。
            /// </summary>
            /// <param name="fileDialog">ファイルダイアログ。</param>
            /// <returns>
            /// OKボタンとファイル名エディットの Tuple 。見つからなければ null 。
            /// </returns>
            protected async Task<Tuple<AutomationElement, AutomationElement>>
            DoFindFileDialogElements(AutomationElement fileDialog)
            {
                // 入力可能状態まで待機
                if (!(await this.WhenForInputIdle()))
                {
                    ThreadTrace.WriteLine(@"入力可能状態になりません。");
                    return null;
                }

                // OKボタン AutomationElement 検索
                var okButton =
                    await RepeatUntil(
                        () => FindFirstChildByAutomationId(fileDialog, @"1"),
                        elem => elem != null,
                        50);
                if (okButton == null)
                {
                    return null;
                }

                // ファイル名エディットホスト AutomationElement 検索
                var editHost =
                    await RepeatUntil(
                        () =>
                            fileDialog.FindFirst(
                                TreeScope.Descendants,
                                new PropertyCondition(
                                    AutomationElement.AutomationIdProperty,
                                    @"FileNameControlHost")),
                        elem => elem != null,
                        50);
                if (editHost == null)
                {
                    return null;
                }

                // ファイル名エディット AutomationElement 検索
                var edit =
                    await RepeatUntil(
                        () =>
                            FindFirstChild(
                                editHost,
                                AutomationElement.ClassNameProperty,
                                @"Edit"),
                        elem => elem != null,
                        50);
                if (edit == null)
                {
                    return null;
                }

                return Tuple.Create(okButton, edit);
            }

            #endregion

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
            /// メインウィンドウタイトルであるか否かを取得する。
            /// </summary>
            /// <param name="title">タイトル。</param>
            /// <returns>
            /// メインウィンドウタイトルならば true 。そうでなければ false 。
            /// </returns>
            /// <remarks>
            /// スプラッシュウィンドウ等の判別用に用いる。
            /// </remarks>
            protected abstract bool IsMainWindowTitle(string title);

            /// <summary>
            /// メインウィンドウ変更時の更新処理を行う。
            /// </summary>
            /// <returns>更新できたならば true 。そうでなければ false 。</returns>
            protected abstract Task<bool> UpdateOnMainWindowChanged();

            /// <summary>
            /// IsDialogShowing プロパティ値を更新する。
            /// </summary>
            /// <returns>更新した値。</returns>
            protected abstract Task<bool> UpdateDialogShowing();

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
            /// トークテキスト取得の実処理を行う。
            /// </summary>
            /// <returns>トークテキスト。取得できなかった場合は null 。</returns>
            protected abstract Task<string> DoGetTalkText();

            /// <summary>
            /// トークテキスト設定の実処理を行う。
            /// </summary>
            /// <param name="text">設定するトークテキスト。</param>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            protected abstract Task<bool> DoSetTalkText(string text);

            /// <summary>
            /// トークテキスト再生の実処理を行う。
            /// </summary>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            protected abstract Task<bool> DoPlay();

            /// <summary>
            /// トークテキスト再生停止の実処理を行う。
            /// </summary>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            protected abstract Task<bool> DoStop();

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
            private bool IsOwnProcess(Process appProcess) =>
                appProcess?.HasExited == false &&
                appProcess.MainModule.FileVersionInfo.ProductName == this.Product;

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

                // メインウィンドウタイトルか？
                // スプラッシュウィンドウ等を弾くため
                if (!this.IsMainWindowTitle(appProcess.MainWindowTitle))
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
                        !(await this.WhenForInputIdle(0)) ||
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
            /// 空白文の音声保存を行えるか否かを取得する。
            /// </summary>
            public bool CanSaveBlankText { get; }

            /// <summary>
            /// 実行ファイルのパスを取得する。
            /// </summary>
            /// <remarks>
            /// プロセスが見つかっていない場合は null を返す。
            /// </remarks>
            public string ExecutablePath
            {
                get => this.executablePath;
                private set => this.SetProperty(ref this.executablePath, value);
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
                get => this.mainWindowHandle;
                private set => this.SetProperty(ref this.mainWindowHandle, value);
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
            public async Task<string> GetTalkText() =>
                this.IsRunning ? (await this.DoGetTalkText()) : null;

            /// <summary>
            /// トークテキストを設定する。
            /// </summary>
            /// <param name="text">トークテキスト。</param>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            /// <remarks>
            /// 再生中の場合は停止させる。WAVEファイル保存中である場合は失敗する。
            /// </remarks>
            public async Task<bool> SetTalkText(string text)
            {
                if (text == null)
                {
                    throw new ArgumentNullException(nameof(text));
                }
                if (
                    !this.IsRunning ||
                    this.IsSaving ||
                    !(await this.Stop()))
                {
                    return false;
                }

                return await this.DoSetTalkText(text);
            }

            /// <summary>
            /// トークテキストの再生を開始する。
            /// </summary>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            /// <remarks>
            /// 再生中の場合は何もせず true を返す。
            /// WAVEファイル保存中である場合やトークテキストが空白である場合は失敗する。
            /// </remarks>
            public async Task<bool> Play()
            {
                if (!this.IsRunning || this.IsSaving || (await this.UpdateDialogShowing()))
                {
                    return false;
                }
                if (this.IsPlaying)
                {
                    return true;
                }

                if (!(await this.DoPlay()))
                {
                    return false;
                }

                // Update を待たずにフラグ更新しておく
                this.IsPlaying = true;
                return true;
            }

            /// <summary>
            /// トークテキストの再生を停止する。
            /// </summary>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            /// <remarks>
            /// WAVEファイル保存中である場合は失敗する。
            /// </remarks>
            public async Task<bool> Stop()
            {
                if (!this.IsRunning || this.IsSaving)
                {
                    return false;
                }
                if (!this.IsPlaying)
                {
                    return true;
                }

                if (!(await this.DoStop()))
                {
                    return false;
                }

                // Update を待たずにフラグ更新しておく
                this.IsPlaying = false;
                return true;
            }

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

                if (!this.IsRunning || this.IsSaving || (await this.UpdateDialogShowing()))
                {
                    return new FileSaveResult(
                        false,
                        error: @"ファイル保存を開始できませんでした。");
                }
                if (
                    !this.CanSaveBlankText &&
                    string.IsNullOrWhiteSpace(await this.GetTalkText()))
                {
                    return new FileSaveResult(
                        false,
                        error: @"空白文を音声保存することはできません。");
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
            /// <returns>成功したならば null 。そうでなければ失敗理由メッセージ。</returns>
            public async Task<string> Run(string executablePath)
            {
                if (executablePath == null)
                {
                    throw new ArgumentNullException(nameof(executablePath));
                }

                if (!File.Exists(executablePath))
                {
                    return @"実行ファイルが存在しません。";
                }
                if (this.AppProcess != null)
                {
                    return @"既に起動しています。";
                }

                using (var updateLock = await this.UpdateLock.WaitAsync())
                {
                    if (this.AppProcess != null)
                    {
                        return @"既に起動しています。";
                    }

                    // プロセス実行
                    var message =
                        await Task.Run(
                            () =>
                            {
                                // 起動
                                Process app = null;
                                try
                                {
                                    app = Process.Start(executablePath);
                                    if (app == null)
                                    {
                                        return @"起動させることができませんでした。";
                                    }
                                }
                                catch (Win32Exception ex)
                                {
                                    ThreadTrace.WriteException(ex);
                                    return
                                        ex.Message ??
                                        (ex.GetType().Name + @" 例外が発生しました。");
                                }
                                catch (Exception ex)
                                {
                                    ThreadTrace.WriteException(ex);
                                    return
                                        ex.GetType().Name +
                                        ((ex.Message == null) ?
                                            @" 例外が発生しました。" : (@" : " + ex.Message));
                                }

                                // 入力待機
                                try
                                {
                                    app.WaitForInputIdle(UIControlTimeout);
                                }
                                catch (Exception ex)
                                {
                                    ThreadTrace.WriteException(ex);
                                    return @"管理者権限で起動済みの可能性があります。";
                                }

                                // 目的のプロセスかチェック
                                bool own = false;
                                try
                                {
                                    own = this.IsOwnProcess(app);
                                }
                                catch (Win32Exception ex)
                                {
                                    ThreadTrace.WriteException(ex);
                                    return @"管理者権限で起動した可能性があります。";
                                }
                                catch (Exception ex)
                                {
                                    ThreadTrace.WriteException(ex);
                                    return ex.GetType().Name + @" : " + ex.Message;
                                }
                                if (!own)
                                {
                                    if (!app.CloseMainWindow())
                                    {
                                        app.Kill();
                                    }
                                    app.Close();
                                    return @"目的のソフトウェアではありませんでした。";
                                }

                                return null;
                            });
                    if (message != null)
                    {
                        return message;
                    }
                }

                // スタートアップ状態になるまで少し待つ
                if (!(await RepeatUntil(() => this.IsStartup || this.IsRunning, f => f, 25)))
                {
                    return @"管理者権限で起動済みの可能性があります。";
                }

                return null;
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
                                try
                                {
                                    if (!this.AppProcess.CloseMainWindow())
                                    {
                                        return false;
                                    }
                                    if (!this.AppProcess.WaitForExit(UIControlTimeout))
                                    {
                                        return false;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ThreadTrace.WriteException(ex);
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
            public virtual async Task<string> GetVoicePresetName() =>
                await Task.FromResult(this.Name); // 単に Name の値を返す

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
                if (disposing)
                {
                    this.UpdateLock.Dispose();
                    this.SaveLock.Dispose();
                }
            }

            #endregion
        }
    }
}
