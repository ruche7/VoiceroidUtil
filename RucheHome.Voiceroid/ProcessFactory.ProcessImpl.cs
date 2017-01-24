using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RucheHome.Util;
using RucheHome.Windows.WinApi;

namespace RucheHome.Voiceroid
{
    partial class ProcessFactory
    {
        /// <summary>
        /// IProcess インタフェース実装クラス。
        /// </summary>
        private class ProcessImpl : BindableBase, IProcess
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
                    // 対象プロセスを検索
                    var app = voiceroidApps?.FirstOrDefault(p => this.IsOwnProcess(p));
                    if (app == null)
                    {
                        this.IsStartup = false;
                        this.SetupDeadState();
                        return;
                    }

                    // 状態更新
                    await this.UpdateState(app);
                }
                finally
                {
                    this.IsUpdating = false;
                }
            }

            /// <summary>
            /// UI操作のタイムアウトミリ秒数。
            /// </summary>
            private const int UIControlTimeout = 1500;

            /// <summary>
            /// 保存ダイアログタイトル文字列。
            /// </summary>
            private const string SaveDialogTitle = @"音声ファイルの保存";

            /// <summary>
            /// 保存進捗ダイアログタイトル文字列。
            /// </summary>
            private const string SaveProgressDialogTitle = @"音声保存";

            /// <summary>
            /// エラーダイアログタイトル文字列。
            /// </summary>
            private const string ErrorDialogTitle = @"エラー";

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
            /// プロセスを取得または設定する。
            /// </summary>
            private Process Process
            {
                get { return this.process; }
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
                get { return this.mainWindow; }
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
            /// 状態更新中であるか否かを取得または設定する。
            /// </summary>
            private bool IsUpdating { get; set; } = false;

            /// <summary>
            /// WAVEファイル保存タスク実行中であるか否かを取得または設定する。
            /// </summary>
            private bool IsSaveTaskRunning { get; set; } = false;

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
                    if (!this.IsRunning)
                    {
                        this.IsStartup = true;
                    }
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
                    this.MainWindow.Handle != process.MainWindowHandle)
                {
                    // プロパティ群更新
                    this.Process = process;
                    this.MainWindow = new Win32Window(process.MainWindowHandle);
                    if (!(await Task.Run(() => this.UpdateControls())))
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
            /// エラーダイアログを検索する。
            /// </summary>
            /// <returns>エラーダイアログ。見つからなければ null 。</returns>
            private Task<Win32Window> FindErrorDialog()
            {
                return this.FindDialog(ErrorDialogTitle);
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
                this.IsSaveTaskRunning = false;

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
                // 開いていても影響のないダイアログは無視
                this.IsDialogShowing =
                    ((await this.FindSaveDialog()) != null) ||
                    ((await this.FindSaveProgressDialog()) != null) ||
                    ((await this.FindErrorDialog()) != null) ||
                    ((await this.FindDialog(@"注意")) != null);

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
            /// 保存ダイアログのファイル名エディットコントロールを検索する処理を行う。
            /// </summary>
            /// <returns>
            /// ファイル名エディットコントロール。見つからなければ null 。
            /// </returns>
            private async Task<Win32Window> DoFindFileNameEditTask()
            {
                // いずれかのダイアログが出るまで待つ
                if (!(await RepeatUntil(this.UpdateDialogShowing, f => f, 150)))
                {
                    ThreadTrace.WriteLine(@"ダイアログ検索処理がタイムアウトしました。");
                    return null;
                }

                // 保存ダイアログ取得
                var dialog = await this.FindSaveDialog();
                if (dialog == null)
                {
                    ThreadTrace.WriteLine(@"音声保存ダイアログが見つかりません。");
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
                    ThreadTrace.WriteLine(@"ファイル名入力欄が見つかりません。");
                    return null;
                }

                return fileNameEdit;
            }

            /// <summary>
            /// WAVEファイルパスをファイル名エディットコントロールへ設定する処理を行う。
            /// </summary>
            /// <param name="fileNameEdit">ファイル名エディットコントロール。</param>
            /// <param name="filePath">WAVEファイルパス。</param>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            private async Task<bool> DoSetFilePathToEditTask(
                Win32Window fileNameEdit,
                string filePath)
            {
                if (fileNameEdit == null || string.IsNullOrWhiteSpace(filePath))
                {
                    return false;
                }

                for (var sw = Stopwatch.StartNew(); ; await Task.Delay(10))
                {
                    // ファイルパス設定
                    var timeout =
                        Math.Max((int)(UIControlTimeout - sw.ElapsedMilliseconds), 100);
                    try
                    {
                        var ok =
                            await Task.Run(() => fileNameEdit.SetText(filePath, timeout));
                        if (!ok)
                        {
                            ThreadTrace.WriteLine(
                                @"ファイルパス設定処理がタイムアウトしました。 " +
                                nameof(filePath) + @".Length=" + filePath.Length);
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        ThreadTrace.WriteException(ex);
                        return false;
                    }

                    // パス設定できていない場合があるので、設定されていることを確認する
                    timeout =
                        Math.Max((int)(UIControlTimeout - sw.ElapsedMilliseconds), 100);
                    try
                    {
                        var text = await Task.Run(() => fileNameEdit.GetText(timeout));
                        if (text == filePath)
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        ThreadTrace.WriteException(ex);
                        return false;
                    }

                    if (sw.ElapsedMilliseconds >= UIControlTimeout)
                    {
                        ThreadTrace.WriteLine(@"ファイルパスを設定できませんでした。");
                        return false;
                    }
                }

                return true;
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
            /// <param name="fileNameEdit">ファイル名エディットコントロール。</param>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            private async Task<bool> DoDecideFilePathTask(Win32Window fileNameEdit)
            {
                if (fileNameEdit == null)
                {
                    return false;
                }

                // ENTERキー押下
                try
                {
                    fileNameEdit.PostMessage(
                        WM_KEYDOWN,
                        new IntPtr(VK_RETURN),
                        new IntPtr(0x00000001));
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                    return false;
                }
                try
                {
                    fileNameEdit.PostMessage(
                        WM_KEYUP,
                        new IntPtr(VK_RETURN),
                        new IntPtr(unchecked((int)0xC0000001)));
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                    return false;
                }

                // 保存ダイアログが閉じるまで待つ
                var dialog =
                    await RepeatUntil(
                        this.FindSaveDialog,
                        (Win32Window d) => d == null,
                        150);
                return (dialog == null);
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

                // ファイル保存 or 保存進捗ダイアログ表示 を待つ
                bool saved = false, progressFound = false;
                bool dialogFound =
                    await RepeatUntil(
                        async () =>
                            (saved = File.Exists(filePath)) ||
                            (progressFound =
                                ((await this.FindSaveProgressDialog()) != null)) ||
                            await this.UpdateDialogShowing(),
                        f => f,
                        150);
                if (!saved)
                {
                    // 保存進捗ダイアログ以外のダイアログが出ているなら失敗
                    if (!progressFound && dialogFound)
                    {
                        return false;
                    }

                    // 保存進捗ダイアログが閉じるまで待つ
                    if (progressFound)
                    {
                        await RepeatUntil(
                            this.FindSaveProgressDialog,
                            (Win32Window d) => d == null);
                    }

                    if (!(await RepeatUntil(() => File.Exists(filePath), f => f, 10)))
                    {
                        ThreadTrace.WriteLine(
                            @"音声ファイルの保存を確認できません。 " +
                            nameof(progressFound) + '=' + progressFound);
                        return false;
                    }
                }

                // 同時にテキストファイルが保存される場合があるため少し待つ
                // 保存されていなくても失敗にはしない
                var txtPath = Path.ChangeExtension(filePath, @".txt");
                await RepeatUntil(() => File.Exists(txtPath), f => f, 10);

                return true;
            }

            #region IProcess インタフェース実装

            /// <summary>
            /// VOICEROID識別IDを取得する。
            /// </summary>
            public VoiceroidId Id { get; }

            /// <summary>
            /// VOICEROID名を取得する。
            /// </summary>
            public string Name
            {
                get { return this.Id.GetInfo().Name; }
            }

            /// <summary>
            /// プロダクト名を取得する。
            /// </summary>
            public string Product
            {
                get { return this.Id.GetInfo().Product; }
            }

            /// <summary>
            /// 表示プロダクト名を取得する。
            /// </summary>
            public string DisplayProduct
            {
                get { return this.Id.GetInfo().DisplayProduct; }
            }

            /// <summary>
            /// 実行ファイルのパスを取得する。
            /// </summary>
            /// <remarks>
            /// プロセスが見つかっていない場合は null を返す。
            /// 値の設定は Process プロパティで行われる。
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
            /// 値の設定は MainWindow プロパティで行われる。
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
            /// <remarks>
            /// IsRunning プロパティの値が変化すると false になる。
            /// SetupDeadState メソッドでは値設定されない。
            /// </remarks>
            public bool IsStartup
            {
                get { return this.startup; }
                private set { this.SetProperty(ref this.startup, value); }
            }
            private bool startup = false;

            /// <summary>
            /// プロセスが実行中であるか否かを取得する。
            /// </summary>
            public bool IsRunning
            {
                get { return this.running; }
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

                this.IsSaveTaskRunning = true;
                this.IsSaving = true;

                string path = null;
                try
                {
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

                    string extraMsg = null;

                    // ファイル保存
                    if (!(await this.DoSetFilePathToEditTask(fileNameEdit, path)))
                    {
                        extraMsg = @"ファイル名を設定できませんでした。";
                    }
                    else if (!(await this.DoEraseOldFileTask(path)))
                    {
                        extraMsg = @"既存ファイルの削除に失敗しました。";
                    }
                    else if (!(await this.DoDecideFilePathTask(fileNameEdit)))
                    {
                        extraMsg = @"ファイル名の確定操作に失敗しました。";
                    }
                    else if (!(await this.DoCheckFileSavedTask(path)))
                    {
                        extraMsg =
                            ((await this.FindErrorDialog()) == null) ?
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
                finally
                {
                    this.IsSaveTaskRunning = false;
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

                // 既に実行中なら不可
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
                            app.WaitForInputIdle(1000);

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

                // 更新中なら待つ
                if (await RepeatUntil(() => this.IsUpdating, f => !f, 100, 10))
                {
                    return false;
                }

                this.IsUpdating = true;
                try
                {
                    if (this.Process == null)
                    {
                        return false;
                    }

                    bool ok =
                        await Task.Run(
                            () =>
                            {
                                // プロセス終了
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
                finally
                {
                    this.IsUpdating = false;
                }

                return true;
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
