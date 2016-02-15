using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ruche.windows;

namespace ruche.voiceroid
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
                if (!Enum.IsDefined(typeof(VoiceroidId), id))
                {
                    throw new InvalidEnumArgumentException(
                        nameof(id),
                        (int)id,
                        id.GetType());
                }

                this.Id = id;
                this.WindowTitle = id.GetInfo().WindowTitle;
            }

            /// <summary>
            /// プロセス列挙を基に状態を更新する。
            /// </summary>
            /// <param name="voiceroidApps">プロセス列挙。</param>
            public void Update(IEnumerable<Process> voiceroidApps)
            {
                // 対象プロセス検索
                var app =
                    voiceroidApps?.FirstOrDefault(
                        p => p.MainWindowTitle.StartsWith(this.WindowTitle));
                if (app == null)
                {
                    this.SetupDeadState();
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
                    if (!this.UpdateControls())
                    {
                        this.SetupDeadState();
                        return;
                    }
                }

                this.IsRunning = true;

                // 保存ダイアログか保存進捗ダイアログが表示中なら保存中と判断
                this.IsSaving = (
                    this.FindSaveDialog() != null ||
                    this.FindSaveProgressDialog() != null);

                if (!this.IsSaving)
                {
                    // 保存ボタンが押せない状態＝再生中と判定
                    this.IsPlaying = !this.SaveButton.IsEnabled;
                }
            }

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

                // 拡張子抜きのフルパスを作成
                var basePath = Path.GetFullPath(filePath);
                if (Path.GetExtension(basePath).ToLower() == ".wav")
                {
                    basePath = Path.ChangeExtension(basePath, null);
                }

                // 終端の "[数値]" を削除
                basePath = RegexWaveFileDigit.Replace(basePath, "");

                // 重複しないパスを作成
                // テキストファイルが同時出力されるのでそちらもチェック
                var path = basePath;
                for (
                    int i = 1;
                    File.Exists(path + ".wav") || File.Exists(path + ".txt");
                    ++i)
                {
                    path = basePath + "[" + i + "]";
                }
                path += ".wav";

                return path;
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
            private static Win32Window FindFileDialogFileNameEdit(Win32Window dialog)
            {
                if (dialog == null)
                {
                    throw new ArgumentNullException(nameof(dialog));
                }

                // 2つ上が ComboBoxEx32 の場合はアドレスバーなので除外
                return
                    dialog
                        .FindDescendants(className: "Edit")
                        .FirstOrDefault(
                            c =>
                            {
                                var name = c.GetAncestor(2)?.ClassName;
                                return (name != null && name != "ComboBoxEx32");
                            });
            }

            /// <summary>
            /// 戻り値が条件を満たさない間、デリゲートを呼び出し続ける。
            /// </summary>
            /// <typeparam name="T">戻り値の型。</typeparam>
            /// <param name="func">デリゲート。</param>
            /// <param name="condition">終了条件デリゲート。</param>
            /// <param name="loopCount">ループ回数。</param>
            /// <param name="interval">ループ間隔。</param>
            /// <returns>条件を満たした時、もしくはループ終了時の戻り値。</returns>
            private static T RepeatUntil<T>(
                Func<T> func,
                Func<T, bool> condition,
                int loopCount,
                TimeSpan interval)
            {
                T value = default(T);

                for (int i = 0; i < loopCount; ++i, Thread.Sleep(interval))
                {
                    value = func();
                    if (condition(value))
                    {
                        break;
                    }
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
            /// WAVEファイル保存タスク実行中であるか否かを取得または設定する。
            /// </summary>
            private bool IsSaveTaskRunning { get; set; } = false;

            /// <summary>
            /// メインウィンドウのコントロール群を更新する。
            /// </summary>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            private bool UpdateControls()
            {
                var controls = this.MainWindow.FindDescendants();

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
            /// 保存ダイアログを検索する。
            /// </summary>
            /// <returns>保存ダイアログ。見つからなければ null 。</returns>
            private Win32Window FindSaveDialog()
            {
                if (this.MainWindow == null)
                {
                    return null;
                }

                return
                    Win32Window.Desktop
                        .FindChildren(text: SaveDialogTitle)
                        .FirstOrDefault(
                            w => w.GetOwner()?.Handle == this.MainWindow.Handle);
            }

            /// <summary>
            /// 保存進捗ダイアログを検索する。
            /// </summary>
            /// <returns>保存進捗ダイアログ。見つからなければ null 。</returns>
            private Win32Window FindSaveProgressDialog()
            {
                if (this.MainWindow == null)
                {
                    return null;
                }

                return
                    Win32Window.Desktop
                        .FindChildren(text: SaveProgressDialogTitle)
                        .FirstOrDefault(
                            w => w.GetOwner()?.Handle == this.MainWindow.Handle);
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
            /// トークテキストのカーソル位置を先頭に移動させる。
            /// </summary>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            /// <remarks>
            /// 再生中の場合は停止させる。既にWAVEファイル保存中である場合は失敗する。
            /// </remarks>
            private bool SetTalkTextCursorToHead()
            {
                if (this.TalkEdit == null || this.IsSaving || !this.Stop())
                {
                    return false;
                }

                this.TalkEdit.SendMessage(EM_SETSEL);

                return true;
            }

            /// <summary>
            /// 保存ダイアログのファイル名エディットコントロールを検索する処理を行う。
            /// </summary>
            /// <returns>
            /// ファイル名エディットコントロール。見つからなければ null 。
            /// </returns>
            private Win32Window DoFindFileNameEditTask()
            {
                // 保存ダイアログ検索
                var dialog =
                    RepeatUntil(
                        this.FindSaveDialog,
                        d => d != null,
                        100,
                        TimeSpan.FromMilliseconds(20));
                if (dialog == null)
                {
                    return null;
                }

                // ダイアログ発見直後はコントロール作成が完了していないので少し待つ
                Thread.Sleep(50);

                // ファイルパス設定先のエディットコントロール取得
                // ダイアログ作成直後は未作成の場合があるので何度か調べる
                var fileNameEdit =
                    RepeatUntil(
                        () => FindFileDialogFileNameEdit(dialog),
                        c => c != null,
                        50,
                        TimeSpan.FromMilliseconds(20));
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
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            private bool DoSaveFileTask(Win32Window fileNameEdit, string filePath)
            {
                if (fileNameEdit == null || string.IsNullOrWhiteSpace(filePath))
                {
                    return false;
                }

                if (!fileNameEdit.SetText(filePath))
                {
                    return false;
                }

                fileNameEdit.PostMessage(
                    WM_KEYDOWN,
                    new IntPtr(VK_RETURN),
                    new IntPtr(0x00000001));
                fileNameEdit.PostMessage(
                    WM_KEYUP,
                    new IntPtr(VK_RETURN),
                    new IntPtr(unchecked((int)0xC0000001)));

                return true;
            }

            /// <summary>
            /// WAVEファイルの保存確認処理を行う。
            /// </summary>
            /// <param name="filePath">WAVEファイルパス。</param>
            /// <returns>保存されているならば true 。そうでなければ false 。</returns>
            private bool DoCheckFileSavedTask(string filePath)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return false;
                }

                // ファイル保存 or 保存進捗ダイアログ表示 を待つ
                bool saved = false;
                bool found =
                    RepeatUntil(
                        () =>
                        {
                            saved = File.Exists(filePath);
                            return (saved || this.FindSaveProgressDialog() != null);
                        },
                        f => f,
                        100,
                        TimeSpan.FromMilliseconds(20));
                if (saved)
                {
                    return true;
                }

                // 保存進捗ダイアログが閉じるまで待つ
                if (found)
                {
                    while (this.FindSaveProgressDialog() != null)
                    {
                        Thread.Sleep(50);
                    }
                }

                return
                    RepeatUntil(
                        () => File.Exists(filePath),
                        r => r,
                        10,
                        TimeSpan.FromMilliseconds(20));
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
                [CallerMemberName] string propertyName = null)
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
            /// メインウィンドウタイトルを取得する。
            /// </summary>
            public string WindowTitle { get; }

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
            /// トークテキストを取得する。
            /// </summary>
            /// <returns>トークテキスト。取得できない場合は null 。</returns>
            public string GetTalkText()
            {
                if (this.TalkEdit == null)
                {
                    return null;
                }

                return this.TalkEdit.GetText();
            }

            /// <summary>
            /// トークテキストを設定する。
            /// </summary>
            /// <param name="text">トークテキスト。</param>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            /// <remarks>
            /// 再生中の場合は停止させる。WAVEファイル保存中である場合は失敗する。
            /// </remarks>
            public bool SetTalkText(string text)
            {
                if (this.TalkEdit == null || this.IsSaving || !this.Stop())
                {
                    return false;
                }

                return this.TalkEdit.SetText(text);
            }

            /// <summary>
            /// トークテキストの再生を開始する。
            /// </summary>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            /// <remarks>
            /// 再生中の場合は何もせず true を返す。
            /// WAVEファイル保存中である場合やトークテキストが空白である場合は失敗する。
            /// </remarks>
            public bool Play()
            {
                if (this.IsPlaying)
                {
                    return true;
                }
                if (
                    this.PlayButton == null ||
                    this.IsSaving ||
                    string.IsNullOrWhiteSpace(this.GetTalkText()) ||
                    !this.SetTalkTextCursorToHead())
                {
                    return false;
                }

                this.PlayButton.PostMessage(BM_CLICK);
                this.IsPlaying = true; // 一応立てる

                return true;
            }

            /// <summary>
            /// トークテキストの再生を停止する。
            /// </summary>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            /// <remarks>
            /// 再生中ではない場合やWAVEファイル保存中である場合は失敗する。
            /// </remarks>
            public bool Stop()
            {
                if (this.StopButton == null || this.IsSaving)
                {
                    return false;
                }
                if (!this.IsPlaying)
                {
                    return true;
                }

                this.StopButton.PostMessage(BM_CLICK);

                // 保存ボタンが有効になるまで少し待つ
                for (int i = 0; i < 50; ++i)
                {
                    if (this.SaveButton == null || this.SaveButton.IsEnabled)
                    {
                        break;
                    }
                    Thread.Sleep(10);
                }

                return true;
            }

            /// <summary>
            /// トークテキストをWAVEファイル保存する。
            /// </summary>
            /// <param name="filePath">保存希望WAVEファイルパス。</param>
            /// <returns>
            /// WAVEファイル保存タスク。保存開始できなかった場合は null 。
            /// 成功すると実際のWAVEファイルパスを返す。失敗すると null を返す。
            /// </returns>
            /// <remarks>
            /// 再生中の場合は停止させる。
            /// WAVEファイル保存中である場合やトークテキストが空白である場合は失敗する。
            /// 
            /// 既に同じ名前のWAVEファイルが存在する場合は拡張子の手前に "[1]" 等の
            /// 角カッコ数値文字列が追加される。
            /// 
            /// VOICEROIDの設定次第ではテキストファイルも同時に保存される。
            /// </remarks>
            public async Task<string> Save(string filePath)
            {
                if (filePath == null)
                {
                    throw new ArgumentNullException(nameof(filePath));
                }

                if (
                    this.SaveButton == null ||
                    this.IsSaving ||
                    string.IsNullOrWhiteSpace(this.GetTalkText()) ||
                    !this.Stop())
                {
                    return null;
                }

                // ファイルパス作成
                var path = MakeWaveFilePath(filePath);

                // 保存先ディレクトリ作成
                if (!MakeDirectory(Path.GetDirectoryName(path)))
                {
                    return null;
                }

                // 保存ボタン押下
                this.SaveButton.PostMessage(BM_CLICK);

                this.IsSaving = true;

                this.IsSaveTaskRunning = true;
                try
                {
                    // ファイル名エディットコントロールを非同期で探す
                    var fileNameEdit =
                        await Task.Factory.StartNew(this.DoFindFileNameEditTask);
                    if (fileNameEdit == null)
                    {
                        return null;
                    }

                    // ファイル保存
                    if (!this.DoSaveFileTask(fileNameEdit, path))
                    {
                        return null;
                    }

                    // ファイル保存成否を非同期で確認
                    bool saved =
                        await Task.Factory.StartNew(() => this.DoCheckFileSavedTask(path));
                    if (!saved)
                    {
                        return null;
                    }
                }
                finally
                {
                    this.IsSaveTaskRunning = false;
                }

                return path;
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
