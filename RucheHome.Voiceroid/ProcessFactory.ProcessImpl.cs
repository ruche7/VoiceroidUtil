using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Automation;
using RucheHome.Threading;
using RucheHome.Util;
using RucheHome.Windows.WinApi;

namespace RucheHome.Voiceroid
{
    partial class ProcessFactory
    {
        /// <summary>
        /// IProcess インタフェース実装クラス。
        /// </summary>
        private sealed class ProcessImpl : BindableBase, IProcess, IDisposable
        {
            /// <summary>
            /// コンストラクタ。
            /// </summary>
            /// <param name="id">VOICEROID識別ID。</param>
            public ProcessImpl(VoiceroidId id)
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
            ~ProcessImpl()
            {
                this.Dispose(false);
            }

            /// <summary>
            /// UI Automation によるUI操作を許可するか否かを取得または設定する。
            /// </summary>
            public bool IsUIAutomationEnabled { get; set; } = true;

            /// <summary>
            /// プロセス列挙を基に状態を更新する。
            /// </summary>
            /// <param name="apps">プロセス列挙。</param>
            public async Task Update(IEnumerable<Process> apps)
            {
                using (var updateLock = await this.UpdateLock.WaitAsync())
                {
                    // 対象プロセスを検索
                    var app = apps?.FirstOrDefault(p => this.IsOwnProcess(p));
                    if (app == null)
                    {
                        this.IsStartup = false;
                        this.SetupDeadState();
                        return;
                    }

                    // 状態更新
                    await this.UpdateState(app);
                }
            }

            /// <summary>
            /// ダイアログ種別列挙。
            /// </summary>
            private enum DialogType
            {
                /// <summary>
                /// 保存ダイアログ。
                /// </summary>
                Save,

                /// <summary>
                /// 保存進捗ダイアログ。
                /// </summary>
                SaveProgress,

                /// <summary>
                /// エラーダイアログ。
                /// </summary>
                Error,

                /// <summary>
                /// 注意ダイアログ。
                /// </summary>
                Caution,
            }

            /// <summary>
            /// UI操作のタイムアウトミリ秒数。
            /// </summary>
            private const int UIControlTimeout = 1500;

            /// <summary>
            /// ダイアログ種別ごとのタイトル文字列のディクショナリ。
            /// </summary>
            private static readonly Dictionary<DialogType, string> DialogTitles =
                new Dictionary<DialogType, string>
                {
                    { DialogType.Save, @"音声ファイルの保存" },
                    { DialogType.SaveProgress, @"音声保存" },
                    { DialogType.Error, @"エラー" },
                    { DialogType.Caution, @"注意" },
                };

            /// <summary>
            /// WAVEファイル名末尾の角カッコ数値文字列にマッチする正規表現。
            /// </summary>
            private static readonly Regex RegexWaveFileDigit = new Regex(@"\[\d+\]$");

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
            /// WAVEファイルまたはテキストファイルが存在するか否かを取得する。
            /// </summary>
            /// <param name="filePath">ファイルパス。拡張子は無視される。</param>
            /// <returns>存在するならば true 。そうでなければ false 。</returns>
            private static bool IsWaveOrTextFileExists(string filePath)
            {
                if (filePath == null)
                {
                    throw new ArgumentNullException(nameof(filePath));
                }

                var wav = Path.ChangeExtension(filePath, @".wav");
                var txt = Path.ChangeExtension(filePath, @".txt");

                return (File.Exists(wav) || File.Exists(txt));
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
            /// ファイルダイアログからファイル名エディットコントロールを検索する。
            /// </summary>
            /// <param name="dialog">ファイルダイアログ。</param>
            /// <returns>
            /// ファイル名エディットコントロール。見つからなければ null 。
            /// </returns>
            private static Win32Window FindFileDialogFileNameEdit(
                Win32Window dialog)
            {
                if (dialog == null)
                {
                    throw new ArgumentNullException(nameof(dialog));
                }

                // 5つ上の親がファイルダイアログ自身である Edit を探す
                try
                {
                    return
                        dialog
                            .FindDescendants(@"Edit")
                            .FirstOrDefault(
                                c => c.GetAncestor(5)?.Handle == dialog.Handle);
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                }

                return null;
            }

            /// <summary>
            /// 戻り値が条件を満たさない間、デリゲートを呼び出し続ける。
            /// </summary>
            /// <typeparam name="T">戻り値の型。</typeparam>
            /// <param name="func">デリゲート。</param>
            /// <param name="condition">終了条件デリゲート。</param>
            /// <param name="loopCount">ループ回数。負数ならば制限無し。</param>
            /// <param name="intervalMilliseconds">ループ間隔ミリ秒数。</param>
            /// <returns>条件を満たした時、もしくはループ終了時の戻り値。</returns>
            private static Task<T> RepeatUntil<T>(
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
            private static async Task<T> RepeatUntil<T>(
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
            /// 更新処理の排他制御を行うオブジェクトを取得する。
            /// </summary>
            private SemaphoreSlimLock UpdateLock { get; } = new SemaphoreSlimLock(1);

            /// <summary>
            /// 保存処理の排他制御を行うオブジェクトを取得する。
            /// </summary>
            private SemaphoreSlimLock SaveLock { get; } = new SemaphoreSlimLock(1);

            /// <summary>
            /// プロセスを取得または設定する。
            /// </summary>
            private Process Process
            {
                get => this.process;
                set
                {
                    this.process = value;
                    this.ExecutablePath = value?.MainModule.FileName;
                }
            }
            private Process process = null;

            /// <summary>
            /// メインウィンドウを取得または設定する。
            /// </summary>
            private Win32Window MainWindow
            {
                get => this.mainWindow;
                set
                {
                    this.mainWindow = value;
                    this.MainWindowHandle = (value == null) ? IntPtr.Zero : value.Handle;
                }
            }
            private Win32Window mainWindow = null;

            /// <summary>
            /// トークテキストエディットコントロールを取得または設定する。
            /// </summary>
            private Win32Window TalkEdit { get; set; } = null;

            /// <summary>
            /// 再生ボタンコントロールを取得または設定する。
            /// </summary>
            private Win32Window PlayButton { get; set; } = null;

            /// <summary>
            /// 停止ボタンコントロールを取得または設定する。
            /// </summary>
            private Win32Window StopButton { get; set; } = null;

            /// <summary>
            /// 保存ボタンコントロールを取得または設定する。
            /// </summary>
            private Win32Window SaveButton { get; set; } = null;

            /// <summary>
            /// 対象プロセスであるか否かを取得する。
            /// </summary>
            /// <param name="process">調べるプロセス。</param>
            /// <returns>対象プロセスであるならば true 。そうでなければ false 。</returns>
            private bool IsOwnProcess(Process process)
            {
                try
                {
                    return (
                        process != null &&
                        !process.HasExited &&
                        process.MainModule.FileVersionInfo.ProductName == this.Product);
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
            /// プロセス情報から状態を更新する。
            /// </summary>
            /// <param name="process">プロセス。</param>
            private async Task UpdateState(Process process)
            {
                if (process == null)
                {
                    throw new ArgumentNullException(nameof(process));
                }

                // メインウィンドウタイトルが空文字列なら
                // スプラッシュウィンドウやメニューウィンドウがメインウィンドウになっている
                if (process.MainWindowTitle == "")
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
                    this.Process = process;
                    process.Refresh();

                    // 入力待機状態になっていない？
                    if (
                        !(await this.WhenForInputHandle(0)) ||
                        process.MainWindowHandle == IntPtr.Zero)
                    {
                        // 実行中でないなら起動中と判断
                        this.IsStartup |= !this.IsRunning;
                        return;
                    }

                    // 現在と同じウィンドウが取得できた場合はスキップ
                    if (
                        !this.IsRunning ||
                        this.MainWindow.Handle != process.MainWindowHandle)
                    {
                        // プロパティ群更新
                        this.MainWindow = new Win32Window(process.MainWindowHandle);
                        if (!(await Task.Run(() => this.UpdateControls())))
                        {
                            this.SetupDeadState();
                            return;
                        }
                    }

                    this.IsRunning = true;

                    // 保存ダイアログか保存進捗ダイアログが表示中なら保存中と判断
                    // ついでに FindDialogs 内で IsDialogShowing プロパティも更新される
                    this.IsSaving =
                        (await this.FindDialogs()).Keys
                            .Any(t => t == DialogType.Save || t == DialogType.SaveProgress);

                    if (!this.IsSaving)
                    {
                        // 保存ボタンが押せない状態＝再生中と判定
                        this.IsPlaying = !this.SaveButton.IsEnabled;
                    }
                }
            }

            /// <summary>
            /// プロセスが入力待機状態になるまで非同期で待機する。
            /// </summary>
            /// <param name="loopCount">
            /// 最大ループ回数。 0 ならば状態確認結果を即座に返す。
            /// </param>
            /// <param name="loopIntervalMilliseconds">ループ間隔ミリ秒数。</param>
            /// <returns>入力待機状態になったならば true 。そうでなければ false 。</returns>
            private async Task<bool> WhenForInputHandle(
                int loopCount = 25,
                int loopIntervalMilliseconds = 20)
            {
                bool? result = this.Process?.WaitForInputIdle(0);

                for (int i = 0; result == false && i < loopCount; ++i)
                {
                    await Task.Delay(loopIntervalMilliseconds);
                    result = this.Process?.WaitForInputIdle(0);
                }

                return (result == true);
            }

            /// <summary>
            /// メインウィンドウのコントロール群を更新する。
            /// </summary>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            private bool UpdateControls()
            {
                List<Win32Window> controls = null;
                Win32Window talkEdit = null;
                Win32Window[] buttons = null;

                // 子コントロール群取得
                try
                {
                    controls = this.MainWindow.FindDescendants();
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                    return false;
                }

                // トークテキスト入力欄取得
                try
                {
                    talkEdit =
                        controls.FirstOrDefault(c => c.ClassName.Contains("RichEdit"));
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                    return false;
                }
                if (talkEdit == null)
                {
                    return false;
                }

                // ボタン群ハンドル取得
                try
                {
                    buttons =
                        controls
                            .Where(c => c.ClassName.Contains("BUTTON"))
                            .Take(3)
                            .ToArray();
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                    return false;
                }
                if (buttons.Length < 3)
                {
                    return false;
                }

                this.TalkEdit = talkEdit;
                this.PlayButton = buttons[0];
                this.StopButton = buttons[1];
                this.SaveButton = buttons[2];

                return true;
            }

            /// <summary>
            /// メインウィンドウをオーナーとするダイアログを検索する。
            /// </summary>
            /// <param name="type">ダイアログ種別。</param>
            /// <returns>ダイアログ。見つからなければ null 。</returns>
            private async Task<Win32Window> FindDialog(DialogType type)
            {
                var mainWin = this.MainWindow;
                if (mainWin == null)
                {
                    return null;
                }

                string title = null;
                if (!DialogTitles.TryGetValue(type, out title))
                {
                    return null;
                }

                return
                    await Win32Window.Desktop
                        .FindChildren(text: title)
                        .ToObservable()
                        .FirstOrDefaultAsync(w => w.GetOwner()?.Handle == mainWin.Handle);
            }

            /// <summary>
            /// メインウィンドウをオーナーとするダイアログを検索し、
            /// 見つかったダイアログ種別とその実体を返す。
            /// </summary>
            /// <returns>ダイアログ種別と実体のディクショナリ。</returns>
            private async Task<Dictionary<DialogType, Win32Window>> FindDialogs()
            {
                var types = (DialogType[])Enum.GetValues(typeof(DialogType));
                var dialogs = await Task.WhenAll(types.Select(t => this.FindDialog(t)));

                var result =
                    Enumerable
                        .Zip(types, dialogs, (type, dialog) => new { type, dialog })
                        .Where(v => v.dialog != null)
                        .ToDictionary(v => v.type, v => v.dialog);

                // ダイアログ表示中フラグを更新
                this.IsDialogShowing = (result.Count > 0);

                return result;
            }

            /// <summary>
            /// プロセス実行中ではない場合の状態セットアップを行う。
            /// </summary>
            private void SetupDeadState()
            {
                this.Process = null;
                this.MainWindow = null;
                this.TalkEdit = null;
                this.PlayButton = null;
                this.StopButton = null;
                this.SaveButton = null;

                this.IsRunning = false;
                this.IsPlaying = false;
                this.IsSaving = false;
                this.IsDialogShowing = false;
            }

            /// <summary>
            /// IsDialogShowing プロパティ値を更新する。
            /// </summary>
            /// <returns>更新した値。</returns>
            private async Task<bool> UpdateDialogShowing()
            {
                // FindDialogs 内で更新される
                await this.FindDialogs();

                return this.IsDialogShowing;
            }

            /// <summary>
            /// トークテキストのカーソル位置を先頭に移動させる。
            /// </summary>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            /// <remarks>
            /// 再生中の場合は停止させる。既にWAVEファイル保存中である場合は失敗する。
            /// </remarks>
            private async Task<bool> SetTalkTextCursorToHead()
            {
                var edit = this.TalkEdit;
                if (edit == null || this.IsSaving || !(await this.Stop()))
                {
                    return false;
                }

                try
                {
                    return
                        await Task.Run(
                            () =>
                                edit
                                    .SendMessage(
                                        EM_SETSEL,
                                        timeoutMilliseconds: UIControlTimeout)
                                    .HasValue);
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                }

                return false;
            }

            /// <summary>
            /// 保存ダイアログアイテムセットクラス。
            /// </summary>
            private class SaveDialogItemSet
            {
                /// <summary>
                /// コンストラクタ。
                /// </summary>
                /// <param name="uiAutomationEnabled">
                /// UI Automation の利用を許可するならば true 。
                /// </param>
                /// <param name="fileNameEditElement">
                /// ファイル名エディット AutomationElement 。
                /// </param>
                /// <param name="okButtonElement">OKボタン AutomationElement 。</param>
                /// <param name="fileNameEditControl">
                /// ファイル名エディットコントロール。
                /// </param>
                public SaveDialogItemSet(
                    bool uiAutomationEnabled,
                    AutomationElement fileNameEditElement,
                    AutomationElement okButtonElement,
                    Win32Window fileNameEditControl)
                {
                    this.IsUIAutomationEnabled = uiAutomationEnabled;
                    this.FileNameEditElement = fileNameEditElement;
                    this.OkButtonElement = okButtonElement;
                    this.FileNameEditControl = fileNameEditControl;
                }

                /// <summary>
                /// UI Automation によるUI操作を許可するか否かを取得する。
                /// </summary>
                public bool IsUIAutomationEnabled { get; }

                /// <summary>
                /// ファイル名エディット AutomationElement を取得する。
                /// </summary>
                public AutomationElement FileNameEditElement { get; }

                /// <summary>
                /// OKボタン AutomationElement を取得する。
                /// </summary>
                public AutomationElement OkButtonElement { get; }

                /// <summary>
                /// ファイル名エディットコントロールを取得する。
                /// </summary>
                public Win32Window FileNameEditControl { get; }
            }

            /// <summary>
            /// 保存ダイアログアイテムセットを検索する処理を行う。
            /// </summary>
            /// <param name="uiAutomationEnabled">
            /// UI Automation の利用を許可するならば true 。
            /// </param>
            /// <returns>
            /// 保存ダイアログアイテムセット。
            /// ファイル名エディットが見つからなければ null 。
            /// </returns>
            private async Task<SaveDialogItemSet> DoFindSaveDialogItemSetTask(
                bool uiAutomationEnabled)
            {
                // いずれかのダイアログが出るまで待つ
                var dialogs =
                    await RepeatUntil(this.FindDialogs, dlgs => dlgs.Count > 0, 150);
                if (dialogs.Count <= 0)
                {
                    ThreadTrace.WriteLine(@"ダイアログ検索処理がタイムアウトしました。");
                    return null;
                }

                // 保存ダイアログ取得
                Win32Window dialog = null;
                if (!dialogs.TryGetValue(DialogType.Save, out dialog))
                {
                    ThreadTrace.WriteLine(@"音声保存ダイアログが見つかりません。");
                    return null;
                }

                // 入力可能状態まで待機
                if (!(await this.WhenForInputHandle()))
                {
                    ThreadTrace.WriteLine(@"入力可能状態になりません。");
                    return null;
                }

                AutomationElement okButtonElem = null;
                AutomationElement editElem = null;

                if (uiAutomationEnabled)
                {
                    var dialogElem = AutomationElement.FromHandle(dialog.Handle);

                    // OKボタン AutomationElement 検索
                    okButtonElem =
                        await RepeatUntil(
                            () =>
                                dialogElem.FindFirst(
                                    TreeScope.Children,
                                    new PropertyCondition(
                                        AutomationElement.AutomationIdProperty,
                                        @"1")),
                            elem => elem != null,
                            50);

                    // ファイル名エディット AutomationElement 検索
                    if (okButtonElem != null)
                    {
                        var hostElem =
                            await RepeatUntil(
                                () =>
                                    dialogElem.FindFirst(
                                        TreeScope.Descendants,
                                        new PropertyCondition(
                                            AutomationElement.AutomationIdProperty,
                                            @"FileNameControlHost")),
                                elem => elem != null,
                                50);
                        if (hostElem != null)
                        {
                            editElem =
                                await RepeatUntil(
                                    () =>
                                        hostElem.FindFirst(
                                            TreeScope.Children,
                                            new PropertyCondition(
                                                AutomationElement.ClassNameProperty,
                                                @"Edit")),
                                    elem => elem != null,
                                    50);
                        }
                    }
                }

                // ファイル名エディットコントロール検索
                var editControl =
                    await RepeatUntil(
                        () => FindFileDialogFileNameEdit(dialog),
                        (Win32Window c) => c != null,
                        50);

                if (editControl == null)
                {
                    if (uiAutomationEnabled && okButtonElem == null)
                    {
                        ThreadTrace.WriteLine(@"OKボタンが見つかりません。");
                        return null;
                    }
                    if (editElem == null)
                    {
                        ThreadTrace.WriteLine(@"ファイル名入力欄が見つかりません。");
                        return null;
                    }
                }

                return
                    new SaveDialogItemSet(
                        uiAutomationEnabled,
                        editElem,
                        okButtonElem,
                        editControl);
            }

            /// <summary>
            /// WAVEファイルパスをファイル名エディットへ設定する処理を行う。
            /// </summary>
            /// <param name="itemSet">保存ダイアログアイテムセット。</param>
            /// <param name="filePath">WAVEファイルパス。</param>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            private async Task<bool> DoSetFilePathToEditTask(
                SaveDialogItemSet itemSet,
                string filePath)
            {
                if (itemSet == null || string.IsNullOrWhiteSpace(filePath))
                {
                    return false;
                }

                // まず UI Automation を試す
                Exception exAuto = null;
                if (itemSet.IsUIAutomationEnabled)
                {
                    try
                    {
                        await this.DoSetFilePathToEditTaskByAutomation(itemSet, filePath);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        exAuto = ex;
                    }
                }

                // コントロール操作を試す
                try
                {
                    await this.DoSetFilePathToEditTaskByControl(itemSet, filePath);
                    if (exAuto != null)
                    {
                        ThreadDebug.WriteException(exAuto);
                    }
                    return true;
                }
                catch (Exception exCtrl)
                {
                    if (exAuto != null)
                    {
                        ThreadTrace.WriteException(exAuto);
                    }
                    ThreadTrace.WriteException(exCtrl);
                }

                return false;
            }

            /// <summary>
            /// WAVEファイルパスをファイル名エディットへ設定する処理を行う。
            /// </summary>
            /// <param name="itemSet">保存ダイアログアイテムセット。</param>
            /// <param name="filePath">WAVEファイルパス。</param>
            private async Task DoSetFilePathToEditTaskByAutomation(
                SaveDialogItemSet itemSet,
                string filePath)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException(
                        @"不正なファイルパスです。",
                        nameof(filePath));
                }

                var edit = itemSet?.FileNameEditElement;
                if (
                    edit == null ||
                    itemSet?.OkButtonElement == null ||
                    itemSet?.IsUIAutomationEnabled != true)
                {
                    throw new ArgumentException(
                        @"UI Automation 用パラメータを取得できていません。",
                        nameof(itemSet));
                }

                // 入力可能状態まで待機
                if (!(await this.WhenForInputHandle()))
                {
                    throw new InvalidOperationException(@"入力可能状態になりません。");
                }
                edit.SetFocus();

                // ファイルパス設定
                object pattern = null;
                if (!edit.TryGetCurrentPattern(ValuePattern.Pattern, out pattern))
                {
                    throw new InvalidOperationException(
                        @"ファイルパス設定パターンを取得できません。");
                }
                ((ValuePattern)pattern).SetValue(filePath);
            }

            /// <summary>
            /// WAVEファイルパスをファイル名エディットへ設定する処理を行う。
            /// </summary>
            /// <param name="itemSet">保存ダイアログアイテムセット。</param>
            /// <param name="filePath">WAVEファイルパス。</param>
            private async Task DoSetFilePathToEditTaskByControl(
                SaveDialogItemSet itemSet,
                string filePath)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException(
                        @"不正なファイルパスです。",
                        nameof(filePath));
                }

                var edit = itemSet?.FileNameEditControl;
                if (edit == null)
                {
                    throw new ArgumentException(
                        @"コントロール操作用パラメータを取得できていません。",
                        nameof(itemSet));
                }

                for (var sw = Stopwatch.StartNew(); ; await Task.Delay(10))
                {
                    // ファイルパス設定
                    var timeout =
                        Math.Max((int)(UIControlTimeout - sw.ElapsedMilliseconds), 100);
                    var ok = await Task.Run(() => edit.SetText(filePath, timeout));
                    if (!ok)
                    {
                        throw new InvalidOperationException(
                            @"ファイルパス設定処理がタイムアウトしました。 " +
                            nameof(filePath) + @".Length=" + filePath.Length);
                    }

                    // パス設定できていない場合があるので、設定されていることを確認する
                    timeout =
                        Math.Max((int)(UIControlTimeout - sw.ElapsedMilliseconds), 100);
                    var text = await Task.Run(() => edit.GetText(timeout));
                    if (text == filePath)
                    {
                        break;
                    }

                    if (sw.ElapsedMilliseconds >= UIControlTimeout)
                    {
                        throw new InvalidOperationException(
                            @"ファイルパスを設定できませんでした。");
                    }
                }
            }

            /// <summary>
            /// 既に存在するWAVEファイルの削除処理を行う。
            /// </summary>
            /// <param name="filePath">WAVEファイルパス。</param>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            private async Task<bool> DoEraseOldFileTask(string filePath)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return false;
                }

                if (File.Exists(filePath))
                {
                    try
                    {
                        await Task.Run(() => File.Delete(filePath));
                    }
                    catch (Exception ex)
                    {
                        ThreadTrace.WriteException(ex);
                        return false;
                    }
                }

                // テキストファイルも削除
                var txtPath = Path.ChangeExtension(filePath, @".txt");
                if (File.Exists(txtPath))
                {
                    try
                    {
                        await Task.Run(() => File.Delete(txtPath));
                    }
                    catch (Exception ex)
                    {
                        ThreadTrace.WriteException(ex);
                        return false;
                    }
                }

                return true;
            }

            /// <summary>
            /// ファイル名エディットコントロールの入力内容確定処理を行う。
            /// </summary>
            /// <param name="itemSet">保存ダイアログアイテムセット。</param>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            private async Task<bool> DoDecideFilePathTask(SaveDialogItemSet itemSet)
            {
                if (itemSet == null)
                {
                    return false;
                }

                // まず UI Automation を試す
                Exception exAuto = null;
                if (itemSet.IsUIAutomationEnabled)
                {
                    try
                    {
                        await this.DoDecideFilePathTaskByAutomation(itemSet);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        exAuto = ex;
                    }
                }

                // コントロール操作を試す
                try
                {
                    await this.DoDecideFilePathTaskByControl(itemSet);
                    if (exAuto != null)
                    {
                        ThreadDebug.WriteException(exAuto);
                    }
                    return true;
                }
                catch (Exception exCtrl)
                {
                    if (exAuto != null)
                    {
                        ThreadTrace.WriteException(exAuto);
                    }
                    ThreadTrace.WriteException(exCtrl);
                }

                return false;
            }

            /// <summary>
            /// ファイル名エディットコントロールの入力内容確定処理を行う。
            /// </summary>
            /// <param name="itemSet">保存ダイアログアイテムセット。</param>
            private async Task DoDecideFilePathTaskByAutomation(SaveDialogItemSet itemSet)
            {
                var okButton = itemSet?.OkButtonElement;
                if (okButton == null || itemSet?.IsUIAutomationEnabled != true)
                {
                    throw new ArgumentException(
                        @"UI Automation 用パラメータを取得できていません。",
                        nameof(itemSet));
                }

                // 入力可能状態まで待機
                if (!(await this.WhenForInputHandle()))
                {
                    throw new InvalidOperationException(@"入力可能状態になりません。");
                }
                okButton.SetFocus();

                // OKボタン押下
                object pattern = null;
                if (!okButton.TryGetCurrentPattern(InvokePattern.Pattern, out pattern))
                {
                    throw new InvalidOperationException(
                        @"OKボタン押下パターンを取得できません。");
                }
                ((InvokePattern)pattern).Invoke();

                // 保存ダイアログが閉じるまで待つ
                var dialog =
                    await RepeatUntil(
                        () => this.FindDialog(DialogType.Save),
                        (Win32Window d) => d == null,
                        150);
                if (dialog != null)
                {
                    throw new InvalidOperationException(
                        @"保存ダイアログの終了を確認できません。");
                }
            }

            /// <summary>
            /// ファイル名エディットコントロールの入力内容確定処理を行う。
            /// </summary>
            /// <param name="itemSet">保存ダイアログアイテムセット。</param>
            private async Task DoDecideFilePathTaskByControl(SaveDialogItemSet itemSet)
            {
                var edit = itemSet?.FileNameEditControl;
                if (edit == null)
                {
                    throw new ArgumentException(
                        @"コントロール操作用パラメータを取得できていません。",
                        nameof(itemSet));
                }

                // ENTERキー押下
                edit.PostMessage(
                    WM_KEYDOWN,
                    new IntPtr(VK_RETURN),
                    new IntPtr(0x00000001));
                edit.PostMessage(
                    WM_KEYUP,
                    new IntPtr(VK_RETURN),
                    new IntPtr(unchecked((int)0xC0000001)));

                // 保存ダイアログが閉じるまで待つ
                var dialog =
                    await RepeatUntil(
                        () => this.FindDialog(DialogType.Save),
                        (Win32Window d) => d == null,
                        150);
                if (dialog != null)
                {
                    throw new InvalidOperationException(
                        @"保存ダイアログの終了を確認できません。");
                }
            }

            /// <summary>
            /// WAVEファイルの保存確認処理を行う。
            /// </summary>
            /// <param name="filePath">WAVEファイルパス。</param>
            /// <returns>保存されているならば true 。そうでなければ false 。</returns>
            private async Task<bool> DoCheckFileSavedTask(string filePath)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return false;
                }

                // ファイル保存完了 or ダイアログ表示 を待つ
                // ファイル保存完了なら null を返す
                var dialogs =
                    await RepeatUntil(
                        async () =>
                            File.Exists(filePath) ? null : (await this.FindDialogs()),
                        dlgs => (dlgs == null || dlgs.Count > 0),
                        150);
                if (dialogs != null)
                {
                    // 保存進捗ダイアログ以外のダイアログが出ているなら失敗
                    if (!dialogs.ContainsKey(DialogType.SaveProgress))
                    {
                        ThreadTrace.WriteLine(
                            @"保存進捗ダイアログ以外のダイアログが開いています。 dialogs=" +
                            string.Join(
                                @",",
                                dialogs.Where(v => v.Value != null).Select(v => v.Key)));
                        return false;
                    }

                    // 保存進捗ダイアログが閉じるまで待つ
                    await RepeatUntil(
                        () => this.FindDialog(DialogType.SaveProgress),
                        (Win32Window d) => d == null);

                    // 改めてファイル保存完了チェック
                    if (!(await RepeatUntil(() => File.Exists(filePath), f => f, 25)))
                    {
                        return false;
                    }
                }

                // 同時にテキストファイルが保存される場合があるため少し待つ
                // 保存されていなくても失敗にはしない
                var txtPath = Path.ChangeExtension(filePath, @".txt");
                await RepeatUntil(() => File.Exists(txtPath), f => f, 10);

                return true;
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
            /// 値の設定は Process プロパティで行われる。
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
            /// 値の設定は MainWindow プロパティで行われる。
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
            /// <remarks>
            /// IsRunning プロパティの値が変化すると false になる。
            /// SetupDeadState メソッドでは値設定されない。
            /// </remarks>
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
                private set
                {
                    if (value != this.running)
                    {
                        this.SetProperty(ref this.running, value);
                        this.IsStartup = false;
                    }
                }
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
                private set => this.SetProperty(ref this.dialogShowing, value);
            }
            private bool dialogShowing = false;

            /// <summary>
            /// トークテキストを取得する。
            /// </summary>
            /// <returns>トークテキスト。取得できなかったならば null 。</returns>
            public async Task<string> GetTalkText()
            {
                var edit = this.TalkEdit;
                if (edit == null)
                {
                    return null;
                }

                try
                {
                    return await Task.Run(() => edit.GetText(UIControlTimeout));
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                }

                return null;
            }

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
                var edit = this.TalkEdit;
                if (edit == null || this.IsSaving || !(await this.Stop()))
                {
                    return false;
                }

                // 500文字あたり1ミリ秒をタイムアウト値に追加
                var timeout = UIControlTimeout + (text.Length / 500);

                try
                {
                    if (!(await Task.Run(() => edit.SetText(text, timeout))))
                    {
                        ThreadTrace.WriteLine(
                            @"トークテキスト設定処理がタイムアウトしました。 " +
                            nameof(text) + @".Length=" + text.Length);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                    return false;
                }

                return true;
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
                if (this.IsPlaying)
                {
                    return true;
                }

                if (
                    this.PlayButton == null ||
                    this.IsSaving ||
                    string.IsNullOrWhiteSpace(await this.GetTalkText()) ||
                    (await this.UpdateDialogShowing()) ||
                    !(await this.SetTalkTextCursorToHead()))
                {
                    return false;
                }

                try
                {
                    this.PlayButton.PostMessage(BM_CLICK);
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                    return false;
                }

                // 保存ボタンが無効になるかダイアログが出るまで待つ
                // ダイアログが出ない限りは失敗にしない
                await RepeatUntil(
                    async () =>
                        this.SaveButton?.IsEnabled != true ||
                        (await this.UpdateDialogShowing()),
                    f => f,
                    25);
                if (this.IsDialogShowing)
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
                if (this.StopButton == null || this.IsSaving)
                {
                    return false;
                }
                if (!this.IsPlaying)
                {
                    return true;
                }

                try
                {
                    this.StopButton.PostMessage(BM_CLICK);
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                    return false;
                }

                // 保存ボタンが有効になるまで少し待つ
                var enabled =
                    await RepeatUntil(
                        () => this.SaveButton?.IsEnabled,
                        e => e != false,
                        25);
                if (enabled != true)
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
            /// VOICEROIDの設定次第ではテキストファイルも同時に保存される。
            /// </remarks>
            public async Task<FileSaveResult> Save(string filePath)
            {
                if (filePath == null)
                {
                    throw new ArgumentNullException(nameof(filePath));
                }

                if (
                    this.SaveButton == null ||
                    this.IsSaving ||
                    string.IsNullOrWhiteSpace(await this.GetTalkText()) ||
                    (await this.UpdateDialogShowing()))
                {
                    return new FileSaveResult(
                        false,
                        error: @"ファイル保存を開始できませんでした。");
                }

                if (!(await this.Stop()))
                {
                    return new FileSaveResult(
                        false,
                        error: @"再生中の音声を停止できませんでした。");
                }

                string path = null;
                using (var saveLock = await this.SaveLock.WaitAsync())
                {
                    this.IsSaving = true;

                    // ファイルパス作成
                    path = MakeWaveFilePath(filePath);
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

                    // 保存ボタン押下
                    try
                    {
                        this.SaveButton.PostMessage(BM_CLICK);
                    }
                    catch (Exception ex)
                    {
                        ThreadTrace.WriteException(ex);
                        return new FileSaveResult(
                            false,
                            error: @"音声保存ボタンを押下できませんでした。");
                    }

                    // 保存ダイアログアイテムセットを非同期で探す
                    var itemSet =
                        await this.DoFindSaveDialogItemSetTask(this.IsUIAutomationEnabled);
                    if (itemSet == null)
                    {
                        var msg =
                            (await this.UpdateDialogShowing()) ?
                                @"ファイル保存を開始できませんでした。" :
                                @"ファイル保存ダイアログが見つかりませんでした。";
                        return new FileSaveResult(false, error: msg);
                    }

                    string extraMsg = null;

                    // ファイル保存
                    if (!(await this.DoSetFilePathToEditTask(itemSet, path)))
                    {
                        extraMsg = @"ファイル名を設定できませんでした。";
                    }
                    else if (!(await this.DoEraseOldFileTask(path)))
                    {
                        extraMsg = @"既存ファイルの削除に失敗しました。";
                    }
                    else if (!(await this.DoDecideFilePathTask(itemSet)))
                    {
                        extraMsg = @"ファイル名の確定操作に失敗しました。";
                    }
                    else if (!(await this.DoCheckFileSavedTask(path)))
                    {
                        extraMsg =
                            ((await this.FindDialog(DialogType.Error)) == null) ?
                                @"ファイル保存を確認できませんでした。" :
                                @"文章が無音である可能性があります。";
                    }

                    // 追加情報が設定されていたら保存失敗
                    if (extraMsg != null)
                    {
                        return new FileSaveResult(
                            false,
                            error: @"ファイル保存処理に失敗しました。",
                            extraMessage: extraMsg);
                    }
                }

                return new FileSaveResult(true, path);
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

                if (this.Process != null)
                {
                    return false;
                }

                using (var updateLock = await this.UpdateLock.WaitAsync())
                {
                    if (this.Process != null)
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
                if (this.Process == null)
                {
                    return false;
                }

                using (var updateLock = await this.UpdateLock.WaitAsync())
                {
                    if (this.Process == null)
                    {
                        return false;
                    }

                    // プロセス終了
                    bool ok =
                        await Task.Run(
                            () =>
                            {
                                if (!this.Process.CloseMainWindow())
                                {
                                    return false;
                                }
                                if (!this.Process.WaitForExit(1000))
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
            private void Dispose(bool disposing)
            {
                this.UpdateLock.Dispose();
                this.SaveLock.Dispose();
            }

            #endregion

            #region Win32 API 定義

            private const uint EM_SETSEL = 0x00B1;
            private const uint BM_CLICK = 0x00F5;
            private const uint WM_KEYDOWN = 0x0100;
            private const uint WM_KEYUP = 0x0101;

            private const int VK_RETURN = 0x0D;

            #endregion
        }
    }
}
