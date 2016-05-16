using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RucheHome.Windows.WinApi;

namespace RucheHome.Voiceroid
{
    partial class ProcessFactory
    {
        /// <summary>
        /// IProcess インタフェース実装クラス。
        /// </summary>
        private class ProcessImpl : IProcess
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
                this.Name = id.GetInfo().Name;
                this.Product = id.GetInfo().Product;
            }

            /// <summary>
            /// 状態を更新する。
            /// </summary>
            public Task Update()
            {
                return this.Update(FindProcesses());
            }

            /// <summary>
            /// 既知のVOICEROIDプロセス列挙を基に状態を更新する。
            /// </summary>
            /// <param name="voiceroidApps">VOICEROIDプロセス列挙。</param>
            public async Task Update(IEnumerable<Process> voiceroidApps)
            {
                if (this.IsUpdating)
                {
                    return;
                }

                this.IsUpdating = true;
                try
                {
                    // 対象プロセスをプロダクト名で検索する
                    var app =
                        voiceroidApps?.FirstOrDefault(
                            p => p?.MainModule.FileVersionInfo.ProductName == this.Product);
                    if (app == null)
                    {
                        this.SetupDeadState();
                        return;
                    }

                    // メインウィンドウタイトルが空文字列なら
                    // メニュー等のウィンドウがメインウィンドウになっているため更新しない
                    if (app.MainWindowTitle == "")
                    {
                        return;
                    }

                    // 保存タスク処理中なら更新しない
                    if (this.IsRunning && this.IsSaveTaskRunning)
                    {
                        return;
                    }

                    // 現在と同じウィンドウが取得できた場合はスキップ
                    if (
                        !this.IsRunning ||
                        this.MainWindow.Handle != app.MainWindowHandle)
                    {
                        // コントロール群更新
                        this.MainWindow = new Win32Window(app.MainWindowHandle);
                        if (!(await this.UpdateControls()))
                        {
                            this.SetupDeadState();
                            return;
                        }
                    }

                    this.IsRunning = true;

                    // 保存ダイアログか保存進捗ダイアログが表示中なら保存中と判断
                    this.IsSaving = (
                        (await this.FindSaveDialog()) != null ||
                        (await this.FindSaveProgressDialog()) != null);

                    if (this.IsSaving)
                    {
                        this.IsDialogShowing = true;
                    }
                    else
                    {
                        // 保存ボタンが押せない状態＝再生中と判定
                        this.IsPlaying = !this.SaveButton.IsEnabled;

                        await this.UpdateDialogShowing();
                    }
                }
                finally
                {
                    this.IsUpdating = false;
                }
            }

            /// <summary>
            /// UI操作のタイムアウトミリ秒数。
            /// </summary>
            private const int UIControlTimeout = 1000;

            /// <summary>
            /// 保存ダイアログタイトル文字列。
            /// </summary>
            private const string SaveDialogTitle = @"音声ファイルの保存";

            /// <summary>
            /// 保存進捗ダイアログタイトル文字列。
            /// </summary>
            private const string SaveProgressDialogTitle = @"音声保存";

            /// <summary>
            /// WAVEファイル名末尾の角カッコ数値文字列にマッチする正規表現。
            /// </summary>
            private static readonly Regex RegexWaveFileDigit = new Regex(@"\[\d+\]$");

            /// <summary>
            /// WAVEファイルパスを作成する。
            /// </summary>
            /// <param name="filePath">基となるファイルパス。</param>
            /// <returns>WAVEファイルパス。</returns>
            private static string MakeWaveFilePath(string filePath)
            {
                if (filePath == null)
                {
                    throw new ArgumentNullException(nameof(filePath));
                }

                var path = Path.GetFullPath(filePath);
                if (Path.GetExtension(path)?.ToLower() != @".wav")
                {
                    path += @".wav";
                }

                return path;
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
            /// ファイルダイアログからファイル名エディットコントロールを検索する。
            /// </summary>
            /// <param name="dialog">ファイルダイアログ。</param>
            /// <returns>
            /// ファイル名エディットコントロール。見つからなければ null 。
            /// </returns>
            private static async Task<Win32Window> FindFileDialogFileNameEdit(
                Win32Window dialog)
            {
                if (dialog == null)
                {
                    throw new ArgumentNullException(nameof(dialog));
                }

                // 5つ上の親がファイルダイアログ自身である Edit を探す
                return
                    (await dialog.FindDescendantsAsync(@"Edit"))
                        .FirstOrDefault(c => c.GetAncestor(5)?.Handle == dialog.Handle);
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
            /// メインウィンドウを取得または設定する。
            /// </summary>
            private Win32Window MainWindow { get; set; } = null;

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
            /// 状態更新中であるか否かを取得または設定する。
            /// </summary>
            private bool IsUpdating { get; set; } = false;

            /// <summary>
            /// WAVEファイル保存タスク実行中であるか否かを取得または設定する。
            /// </summary>
            private bool IsSaveTaskRunning { get; set; } = false;

            /// <summary>
            /// メインウィンドウのコントロール群を更新する。
            /// </summary>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            private async Task<bool> UpdateControls()
            {
                var controls = await this.MainWindow.FindDescendantsAsync();

                // トークテキスト入力欄取得
                var talkEdit =
                    controls.FirstOrDefault(c => c.ClassName.Contains("RichEdit"));
                if (talkEdit == null)
                {
                    return false;
                }

                // ボタン群ハンドル取得
                var buttons =
                    controls
                        .Where(c => c.ClassName.Contains("BUTTON"))
                        .Take(3)
                        .ToArray();
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
            /// <param name="title">タイトル文字列。限定しないならば null 。</param>
            /// <returns>ダイアログ。見つからなければ null 。</returns>
            private async Task<Win32Window> FindDialog(string title = null)
            {
                var mainWin = this.MainWindow;
                if (mainWin == null)
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
            /// 保存ダイアログを検索する。
            /// </summary>
            /// <returns>保存ダイアログ。見つからなければ null 。</returns>
            private Task<Win32Window> FindSaveDialog()
            {
                return this.FindDialog(SaveDialogTitle);
            }

            /// <summary>
            /// 保存進捗ダイアログを検索する。
            /// </summary>
            /// <returns>保存進捗ダイアログ。見つからなければ null 。</returns>
            private Task<Win32Window> FindSaveProgressDialog()
            {
                return this.FindDialog(SaveProgressDialogTitle);
            }

            /// <summary>
            /// プロセス実行中ではない場合の状態セットアップを行う。
            /// </summary>
            private void SetupDeadState()
            {
                this.MainWindow = null;
                this.TalkEdit = null;
                this.PlayButton = null;
                this.StopButton = null;
                this.SaveButton = null;
                this.IsSaveTaskRunning = false;

                this.IsRunning = false;
                this.IsPlaying = false;
                this.IsSaving = false;
            }

            /// <summary>
            /// IsDialogShowing プロパティ値を更新する。
            /// </summary>
            /// <returns>更新した値。</returns>
            private async Task<bool> UpdateDialogShowing()
            {
                // 開いていても影響のないダイアログは無視
                this.IsDialogShowing =
                    ((await this.FindSaveDialog()) != null) ||
                    ((await this.FindSaveProgressDialog()) != null) ||
                    ((await this.FindDialog(@"注意")) != null) ||
                    ((await this.FindDialog(@"エラー")) != null);

                return this.IsDialogShowing;
            }

            /// <summary>
            /// トークテキストのカーソル位置を先頭に移動させる。
            /// </summary>
            /// <returns>
            /// カーソル位置設定タスク。
            /// 成功すると true を返す。そうでなければ false を返す。
            /// </returns>
            /// <remarks>
            /// 再生中の場合は停止させる。既にWAVEファイル保存中である場合は失敗する。
            /// </remarks>
            private async Task<bool> SetTalkTextCursorToHead()
            {
                if (this.TalkEdit == null || this.IsSaving || !(await this.Stop()))
                {
                    return false;
                }

                var task =
                    this.TalkEdit?.SendMessageAsync(
                        EM_SETSEL,
                        timeoutMilliseconds: UIControlTimeout);
                if (task == null)
                {
                    return false;
                }

                return (await task).HasValue;
            }

            /// <summary>
            /// 保存ダイアログのファイル名エディットコントロールを検索する処理を行う。
            /// </summary>
            /// <returns>
            /// エディットコントロール検索タスク。
            /// ファイル名エディットコントロールを返す。見つからなければ null を返す。
            /// </returns>
            private async Task<Win32Window> DoFindFileNameEditTask()
            {
                // いずれかのダイアログが出るまで待つ
                if (!(await RepeatUntil(this.UpdateDialogShowing, f => f, 150)))
                {
                    return null;
                }

                // 保存ダイアログ取得
                var dialog = await this.FindSaveDialog();
                if (dialog == null)
                {
                    return null;
                }

                // ファイルパス設定先のエディットコントロール取得
                var fileNameEdit =
                    await RepeatUntil(
                        () => FindFileDialogFileNameEdit(dialog),
                        (Win32Window c) => c != null,
                        50);
                if (fileNameEdit == null)
                {
                    return null;
                }

                return fileNameEdit;
            }

            /// <summary>
            /// WAVEファイル保存のGUI操作処理を行う。
            /// </summary>
            /// <param name="fileNameEdit">ファイル名エディットコントロール。</param>
            /// <param name="filePath">WAVEファイルパス。</param>
            /// <returns>
            /// GUI操作タスク。
            /// 成功すると true を返す。そうでなければ false を返す。
            /// </returns>
            private async Task<bool> DoSaveFileTask(
                Win32Window fileNameEdit,
                string filePath)
            {
                if (fileNameEdit == null || string.IsNullOrWhiteSpace(filePath))
                {
                    return false;
                }

                if (!(await fileNameEdit.SetTextAsync(filePath, UIControlTimeout)))
                {
                    return false;
                }

                // 既存のファイルを削除
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                var txtPath = Path.ChangeExtension(filePath, @".txt");
                if (File.Exists(txtPath))
                {
                    File.Delete(txtPath);
                }

                // ENTERキー押下
                fileNameEdit.PostMessage(
                    WM_KEYDOWN,
                    new IntPtr(VK_RETURN),
                    new IntPtr(0x00000001));
                fileNameEdit.PostMessage(
                    WM_KEYUP,
                    new IntPtr(VK_RETURN),
                    new IntPtr(unchecked((int)0xC0000001)));

                // 保存ダイアログが閉じるまで待つ
                var dialog =
                    await RepeatUntil(
                        this.FindSaveDialog,
                        (Win32Window d) => d == null,
                        100);
                if (dialog != null)
                {
                    return false;
                }

                return true;
            }

            /// <summary>
            /// WAVEファイルの保存確認処理を行う。
            /// </summary>
            /// <param name="filePath">WAVEファイルパス。</param>
            /// <returns>
            /// 保存確認タスク。
            /// 保存されているならば true を返す。そうでなければ false を返す。
            /// </returns>
            private async Task<bool> DoCheckFileSavedTask(string filePath)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return false;
                }

                // ファイル保存 or 保存進捗ダイアログ表示 を待つ
                bool saved = false;
                bool found =
                    await RepeatUntil(
                        async () =>
                            (saved = File.Exists(filePath)) ||
                            (await this.FindSaveProgressDialog()) != null,
                        f => f,
                        150);
                if (!saved)
                {
                    // 保存進捗ダイアログが閉じるまで待つ
                    if (found)
                    {
                        await RepeatUntil(
                            this.FindSaveProgressDialog,
                            (Win32Window d) => d == null);
                    }

                    saved = await RepeatUntil(() => File.Exists(filePath), f => f, 10);
                }

                if (saved)
                {
                    // 同時にテキストファイルが保存される場合があるため少し待つ
                    // 保存されていなくても失敗にはしない
                    var txtPath = Path.ChangeExtension(filePath, @".txt");
                    await RepeatUntil(() => File.Exists(txtPath), f => f, 10);
                }

                return saved;
            }

            /// <summary>
            /// プロパティ値を設定する。
            /// </summary>
            /// <typeparam name="T">プロパティ値の型。</typeparam>
            /// <param name="field">設定先フィールド。</param>
            /// <param name="value">設定値。</param>
            /// <param name="propertyName">
            /// プロパティ名。 CallerMemberNameAttribute により自動設定される。
            /// </param>
            private void SetProperty<T>(
                ref T field,
                T value,
                [CallerMemberName] string propertyName = "")
            {
                if (!EqualityComparer<T>.Default.Equals(field, value))
                {
                    field = value;
                    if (propertyName != null && this.PropertyChanged != null)
                    {
                        this.PropertyChanged(
                            this,
                            new PropertyChangedEventArgs(propertyName));
                    }
                }
            }

            #region IProcess インタフェース実装

            /// <summary>
            /// VOICEROID識別IDを取得する。
            /// </summary>
            public VoiceroidId Id { get; }

            /// <summary>
            /// VOICEROID名を取得する。
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// プロダクト名を取得する。
            /// </summary>
            public string Product { get; }

            /// <summary>
            /// プロセスが実行中であるか否かを取得する。
            /// </summary>
            public bool IsRunning
            {
                get { return this.running; }
                private set { this.SetProperty(ref this.running, value); }
            }
            private bool running = false;

            /// <summary>
            /// トークテキストを再生中であるか否かを取得する。
            /// </summary>
            public bool IsPlaying
            {
                get { return this.playing; }
                set { this.SetProperty(ref this.playing, value); }
            }
            private bool playing = false;

            /// <summary>
            /// トークテキストをWAVEファイル保存中であるか否かを取得する。
            /// </summary>
            public bool IsSaving
            {
                get { return this.saving; }
                set { this.SetProperty(ref this.saving, value); }
            }
            private bool saving = false;

            /// <summary>
            /// いずれかのダイアログが表示中であるか否かを取得する。
            /// </summary>
            public bool IsDialogShowing
            {
                get { return this.dialogShowing; }
                set { this.SetProperty(ref this.dialogShowing, value); }
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

                return await edit.GetTextAsync(UIControlTimeout);
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

                return await edit.SetTextAsync(text, timeout);
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

                if (this.PlayButton?.PostMessage(BM_CLICK) != true)
                {
                    return false;
                }
                this.IsPlaying = true; // 一応立てる

                // 保存ボタンが無効になるかダイアログが出るまで待つ
                // ダイアログが出ない限りは失敗にしない
                await RepeatUntil(
                    async () =>
                        this.SaveButton?.IsEnabled != true ||
                        (await this.UpdateDialogShowing()),
                    f => f,
                    25);
                return !this.IsDialogShowing;
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

                if (this.StopButton?.PostMessage(BM_CLICK) != true)
                {
                    return false;
                }

                // 保存ボタンが有効になるまで少し待つ
                var enabled =
                    await RepeatUntil(
                        () => this.SaveButton?.IsEnabled,
                        e => e != false,
                        25);
                return (enabled == true);
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

                this.IsSaveTaskRunning = true;
                this.IsSaving = true;

                string path = null;
                try
                {
                    // ファイルパス作成
                    path = MakeWaveFilePath(filePath);

                    // 保存先ディレクトリ作成
                    if (!MakeDirectory(Path.GetDirectoryName(path)))
                    {
                        return new FileSaveResult(
                            false,
                            error: @"保存先フォルダーを作成できませんでした。");
                    }

                    // 保存ボタン押下
                    if (this.SaveButton?.PostMessage(BM_CLICK) != true)
                    {
                        return new FileSaveResult(
                            false,
                            error: @"音声保存ボタンを押下できませんでした。");
                    }

                    // ファイル名エディットコントロールを非同期で探す
                    var fileNameEdit = await this.DoFindFileNameEditTask();
                    if (fileNameEdit == null)
                    {
                        var msg =
                            (await this.UpdateDialogShowing()) ?
                                @"ファイル保存を開始できませんでした。" :
                                @"ファイル保存ダイアログが見つかりませんでした。";
                        return new FileSaveResult(false, error: msg);
                    }

                    // ファイル保存
                    if (!(await this.DoSaveFileTask(fileNameEdit, path)))
                    {
                        return new FileSaveResult(
                            false,
                            error: @"ファイル保存処理に失敗しました。");
                    }

                    // ファイル保存成否を非同期で確認
                    bool saved = await this.DoCheckFileSavedTask(path);
                    if (!saved)
                    {
                        return new FileSaveResult(
                            false,
                            error: @"ファイル保存を確認できませんでした。");
                    }
                }
                finally
                {
                    this.IsSaveTaskRunning = false;
                }

                return new FileSaveResult(true, path);
            }

            #endregion

            #region INotifyPropertyChanged の実装

            public event PropertyChangedEventHandler PropertyChanged;

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
