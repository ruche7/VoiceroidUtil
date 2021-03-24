using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Automation;
using RucheHome.Util;
using RucheHome.Windows.WinApi;

namespace RucheHome.Voiceroid
{
    partial class ProcessFactory
    {
        /// <summary>
        /// VOICEROID2ライクなソフトウェア用の IProcess インタフェース実装の抽象基底クラス。
        /// </summary>
        private abstract class Voiceroid2ImplBase : ImplBase
        {
            /// <summary>
            /// コンストラクタ＞
            /// </summary>
            /// <param name="id">VOICEROID識別ID。</param>
            /// <param name="canSaveBlankText">空白文の音声保存を行えるならば true 。</param>
            public Voiceroid2ImplBase(VoiceroidId id, bool canSaveBlankText)
                : base(id, canSaveBlankText)
            {
            }

            /// <summary>
            /// ボタン種別列挙。
            /// </summary>
            private enum ButtonType
            {
                /// <summary>
                /// 再生
                /// </summary>
                Play,

                /// <summary>
                /// 停止
                /// </summary>
                Stop,

                /// <summary>
                /// 先頭
                /// </summary>
                Head,

                /// <summary>
                /// 末尾
                /// </summary>
                Tail,

                /// <summary>
                /// 音声保存
                /// </summary>
                Save,

                /// <summary>
                /// 再生時間
                /// </summary>
                Time,
            }

            /// <summary>
            /// ボタン上テキストとボタン種別のディクショナリ。
            /// </summary>
            private static readonly Dictionary<string, ButtonType> NamedButtonTypes =
                new Dictionary<string, ButtonType>
                {
                    { @"再生", ButtonType.Play },
                    { @"停止", ButtonType.Stop },
                    { @"先頭", ButtonType.Head },
                    { @"末尾", ButtonType.Tail },
                    { @"音声保存", ButtonType.Save },
                    { @"再生時間", ButtonType.Time },
                };

            /// <summary>
            /// 音声保存オプションウィンドウ名。
            /// </summary>
            /// <remarks>
            /// 本体側の設定次第で表示される。
            /// 表示される場合、以降の保存関連ダイアログはこのウィンドウの子となる。
            /// </remarks>
            private const string SaveOptionDialogName = @"音声保存";

            /// <summary>
            /// 音声保存ファイルダイアログ名。
            /// </summary>
            private const string SaveFileDialogName = @"名前を付けて保存";

            /// <summary>
            /// 音声保存進捗ウィンドウ名。
            /// </summary>
            private const string SaveProgressDialogName = @"音声保存";

            /// <summary>
            /// 音声保存完了ダイアログ名。
            /// </summary>
            private const string SaveCompleteDialogName = @"情報";

            /// <summary>
            /// A.I.VOICE自動命名時の音声保存確認ダイアログ名。
            /// </summary>
            private const string AiVoiceSaveConfirmDialogName = @"確認";

            /// <summary>
            /// ボタン群を検索する。
            /// </summary>
            /// <param name="types">検索対象ボタン種別配列。</param>
            /// <returns>ボタン AutomationElement 配列。見つからなければ null 。</returns>
            private async Task<AutomationElement[]> FindButtons(params ButtonType[] types)
            {
                if (types == null)
                {
                    throw new ArgumentNullException(nameof(types));
                }

                var root = MakeElementFromHandle(this.MainWindowHandle);
                var buttonsRoot = FindFirstChildByAutomationId(root, @"c");
                if (buttonsRoot == null)
                {
                    return null;
                }

                var results = new AutomationElement[types.Length];

                try
                {
                    await Task.Run(
                        () =>
                        {
                            // ボタン群取得
                            var buttons =
                                buttonsRoot.FindAll(
                                    TreeScope.Children,
                                    new PropertyCondition(
                                        AutomationElement.ControlTypeProperty,
                                        ControlType.Button));

                            foreach (AutomationElement button in buttons)
                            {
                                // 子のテキストからボタン種別決定
                                var buttonText =
                                    FindFirstChildByControlType(button, ControlType.Text);
                                if (
                                    buttonText != null &&
                                    NamedButtonTypes.TryGetValue(
                                        buttonText.Current.Name,
                                        out var type))
                                {
                                    var index = Array.IndexOf(types, type);
                                    if (index >= 0)
                                    {
                                        results[index] = button;
                                    }
                                }
                            }
                        });
                }
                catch
                {
                    return null;
                }

                // すべてのボタンが揃っているか確認
                return results.Any(b => b == null) ? null : results;
            }

            /// <summary>
            /// ダイアログ群を検索する。
            /// </summary>
            /// <returns>ダイアログ AutomationElement 配列。</returns>
            private async Task<AutomationElement[]> FindDialogs()
            {
                var mainHandle = this.MainWindowHandle;
                if (!this.IsRunning || mainHandle == IntPtr.Zero)
                {
                    return new AutomationElement[0];
                }

                var root = MakeElementFromHandle(mainHandle);
                if (root == null)
                {
                    return new AutomationElement[0];
                }

                var dialogs =
                    await Task.Run(
                        () =>
                            FindChildWindows(root)
                                .Where(
                                    e =>
                                    {
                                        try
                                        {
                                            return (e.Current.Name.Length > 0);
                                        }
                                        catch { }
                                        return false;
                                    })
                                .ToArray());

                // ダイアログ表示中フラグを更新
                this.IsDialogShowing = (dialogs.Length > 0);

                return dialogs;
            }

            /// <summary>
            /// トークテクストエディットコントロールを検索する。
            /// </summary>
            /// <returns>見つかった AutomationElement 。見つからなければ null 。</returns>
            private AutomationElement FindTalkTextEdit()
            {
                var mainHandle = this.MainWindowHandle;
                if (!this.IsRunning || mainHandle == IntPtr.Zero)
                {
                    return null;
                }

                var root = MakeElementFromHandle(mainHandle);
                var editRoot = FindFirstChildByAutomationId(root, @"c");

                return FindFirstChildByControlType(editRoot, ControlType.Edit);
            }

            /// <summary>
            /// プリセット名テキストコントロールを検索する。
            /// </summary>
            /// <returns>見つかった AutomationElement 。見つからなければ null 。</returns>
            private AutomationElement FindPresetNameText()
            {
                var mainHandle = this.MainWindowHandle;
                if (!this.IsRunning || mainHandle == IntPtr.Zero)
                {
                    return null;
                }

                var root = MakeElementFromHandle(mainHandle);
                var textRoot = FindFirstChildByAutomationId(root, @"d");

                return FindFirstChildByControlType(textRoot, ControlType.Text);
            }

            #region 音声保存処理

            /// <summary>
            /// 音声保存ダイアログを検索する処理を行う。
            /// </summary>
            /// <returns>音声保存ダイアログ。見つからなければ null 。</returns>
            /// <remarks>
            /// オプションウィンドウ、ファイルダイアログ、
            /// 確認ダイアログ(A.I.VOICEのみ)のいずれかを返す。
            /// </remarks>
            private async Task<AutomationElement> DoFindSaveDialogTask()
            {
                try
                {
                    var dialogs =
                        await RepeatUntil(
                            async () => await this.FindDialogs(),
                            dlgs => dlgs.Any(),
                            150);
                    return
                        dialogs
                            .FirstOrDefault(
                                d =>
                                {
                                    var name = d.Current.Name;
                                    return
                                        (name == SaveOptionDialogName) ||
                                        (name == SaveFileDialogName) ||
                                        (name == AiVoiceSaveConfirmDialogName);
                                });
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                }
                return null;
            }

            /// <summary>
            /// 音声保存オプションウィンドウのOKボタンを押下し、
            /// ファイルダイアログまたは保存進捗ウィンドウ(A.I.VOICE自動命名時)を取得する。
            /// </summary>
            /// <param name="dialog">音声保存オプションウィンドウ。</param>
            /// <returns>
            /// ファイルダイアログまたは保存進捗ウィンドウ(A.I.VOICE自動命名時)と、
            /// 処理失敗時の追加メッセージのタプル。
            /// </returns>
            private async Task<Tuple<AutomationElement, string>>
            DoPushOkButtonOfSaveOptionDialogTask(AutomationElement optionDialog)
            {
                AutomationElement nextWin = null;

                if (optionDialog == null)
                {
                    throw new ArgumentNullException(nameof(optionDialog));
                }

                // 入力可能状態まで待機
                if (!(await this.WhenForInputIdle()))
                {
                    return Tuple.Create(nextWin, @"入力可能状態になりませんでした。");
                }

                // OKボタン検索
                var okButton =
                    await RepeatUntil(
                        () =>
                            FindFirstChild(
                                optionDialog,
                                AutomationElement.NameProperty,
                                @"OK"),
                        elem => elem != null,
                        50);
                if (okButton == null)
                {
                    return Tuple.Create(nextWin, @"OKボタンが見つかりませんでした。");
                }

                try
                {
                    // OKボタン押下
                    if (!InvokeElement(okButton))
                    {
                        return Tuple.Create(nextWin, @"OKボタンをクリックできませんでした。");
                    }

                    // ファイルダイアログ or 保存進捗ウィンドウ(A.I.VOICE自動命名時) 検索
                    nextWin =
                        await RepeatUntil(
                            () =>
                                FindFirstChildByControlType(
                                    optionDialog,
                                    ControlType.Window),
                            elem => elem != null,
                            150);
                    var nextWinName = nextWin?.Current.Name;
                    if (
                        nextWinName != SaveFileDialogName &&
                        nextWinName != SaveProgressDialogName)
                    {
                        nextWin = null;
                        return
                            Tuple.Create(nextWin, @"ウィンドウが見つかりませんでした。");
                    }
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);

                    nextWin = null;
                    return Tuple.Create(nextWin, @"内部エラーが発生しました。");
                }

                return Tuple.Create(nextWin, (string)null);
            }

            /// <summary>
            /// WAVEファイルパスをファイル名エディットへ設定する処理を行う。
            /// </summary>
            /// <param name="fileNameEdit">ファイル名エディット。</param>
            /// <param name="filePath">WAVEファイルパス。</param>
            /// <returns>
            /// 成功したならば null 。そうでなければ処理失敗時の追加メッセージ。
            /// </returns>
            private async Task<string> DoSetFilePathToEditTask(
                AutomationElement fileNameEdit,
                string filePath)
            {
                if (fileNameEdit == null || string.IsNullOrWhiteSpace(filePath))
                {
                    return @"内部パラメータが不正です。";
                }

                // 入力可能状態まで待機
                if (!(await this.WhenForInputIdle()))
                {
                    return @"入力可能状態になりませんでした。";
                }

                // フォーカス
                try
                {
                    fileNameEdit.SetFocus();
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                    return @"ファイル名入力欄をフォーカスできませんでした。";
                }

                // VOICEROID2ライクソフトウェアでは、
                // Windowsのフォルダーオプションで拡張子を表示しない設定にしていると、
                // ValuePattern.SetValue によるファイルパス設定が無視され、
                // 元々入力されている前回保存時の名前が参照されてしまう。
                // 一旦キー入力を行うことで回避できるようなので、適当な文字を送ってみる。

                // 適当な文字をファイル名エディットへ送信
                // 失敗しても先へ進む
                try
                {
                    var editWin =
                        new Win32Window(new IntPtr(fileNameEdit.Current.NativeWindowHandle));
                    editWin.SendMessage(
                        WM_CHAR,
                        new IntPtr('x'),
                        IntPtr.Zero,
                        UIControlTimeout);
                }
                catch { }

                // ファイルパス設定
                if (!SetElementValue(fileNameEdit, filePath))
                {
                    return @"ファイル名を設定できませんでした。";
                }

                return null;
            }

            /// <summary>
            /// 既に存在するWAVEファイルの削除処理を行う。
            /// </summary>
            /// <param name="filePath">WAVEファイルパス。</param>
            /// <param name="withSplitFiles">
            /// 分割連番ファイルの削除も行うならば true 。
            /// </param>
            /// <returns>削除したファイル数。失敗したならば -1 。</returns>
            private async Task<int> DoEraseOldFileTask(
                string filePath,
                bool withSplitFiles = true)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return -1;
                }

                int count = 0;

                // そのままの名前の wav, txt を削除
                var txtPath = Path.ChangeExtension(filePath, @".txt");
                try
                {
                    await Task.Run(
                        () =>
                        {
                            foreach (var path in new[] { filePath, txtPath })
                            {
                                if (File.Exists(path))
                                {
                                    File.Delete(path);
                                    ++count;
                                }
                            }
                        });
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                    return -1;
                }

                // VOICEROID2ライクソフトウェアのファイル分割機能による連番ファイルを削除
                if (withSplitFiles)
                {
                    for (int i = 0; ; ++i)
                    {
                        var splitPath =
                            Path.Combine(
                                Path.GetDirectoryName(filePath),
                                Path.GetFileNameWithoutExtension(filePath) + @"-" + i +
                                Path.GetExtension(filePath));
                        var c = await this.DoEraseOldFileTask(splitPath, false);
                        if (c <= 0)
                        {
                            break;
                        }
                        count += c;
                    }
                }

                return count;
            }

            /// <summary>
            /// ファイル名エディットコントロールの入力内容確定処理を行う。
            /// </summary>
            /// <param name="okButton">ファイルダイアログのOKボタン。</param>
            /// <param name="fileDialogParent">ファイルダイアログの親。</param>
            /// <returns>
            /// 成功したならば null 。そうでなければ処理失敗時の追加メッセージ。
            /// </returns>
            private async Task<string> DoDecideFilePathTask(
                AutomationElement okButton,
                AutomationElement fileDialogParent)
            {
                if (okButton == null || fileDialogParent == null)
                {
                    return @"内部パラメータが不正です。";
                }

                // 入力可能状態まで待機
                if (!(await this.WhenForInputIdle()))
                {
                    return @"入力可能状態になりませんでした。";
                }

                // フォーカス
                try
                {
                    okButton.SetFocus();
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                    return @"OKボタンをフォーカスできませんでした。";
                }

                // OKボタン押下
                if (!InvokeElement(okButton))
                {
                    return @"OKボタンをクリックできませんでした。";
                }

                // ファイルダイアログが閉じるまで待つ
                try
                {
                    var closed =
                        await RepeatUntil(
                            () =>
                                !FindChildWindows(
                                    fileDialogParent,
                                    SaveFileDialogName)
                                    .Any(),
                            f => f,
                            150);
                    if (!closed)
                    {
                        return @"ダイアログの終了を確認できませんでした。";
                    }
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                    return @"内部エラーが発生しました。";
                }

                return null;
            }

            /// <summary>
            /// WAVEファイルの保存確認処理を行う。
            /// </summary>
            /// <param name="filePath">WAVEファイルパス。</param>
            /// <param name="optionShown">オプションウィンドウ表示フラグ。</param>
            /// <param name="progressWindowParent">保存進捗ウィンドウの親。</param>
            /// <param name="aiVoiceAutoNamed">
            /// A.I.VOICE自動命名による保存処理ならば true 。
            /// </param>
            /// <returns>保存処理結果。</returns>
            private async Task<FileSaveResult> DoCheckFileSavedTask(
                string filePath,
                bool optionShown,
                AutomationElement progressWindowParent,
                bool aiVoiceAutoNamed)
            {
                if (string.IsNullOrWhiteSpace(filePath) || progressWindowParent == null)
                {
                    return
                        new FileSaveResult(
                            false,
                            error: @"音声保存確認処理に失敗しました。",
                            extraMessage: @"内部パラメータが不正です。");
                }

                // 保存進捗ウィンドウ表示を待つ
                var progressWin =
                    await RepeatUntil(
                        () =>
                            FindChildWindows(progressWindowParent, SaveProgressDialogName)
                                .FirstOrDefault(),
                        d => d != null,
                        100);
                if (progressWin == null)
                {
                    return
                        new FileSaveResult(
                            false,
                            error: @"音声保存進捗ウィンドウが見つかりませんでした。");
                }

                // オプションウィンドウを表示しているか否かで保存完了ダイアログの親が違う
                var completeDialogParent = optionShown ? progressWindowParent : progressWin;

                // 保存完了ダイアログ表示か保存進捗ウィンドウ非表示を待つ
                AutomationElement completeDialog = null;
                await RepeatUntil(
                    () =>
                    {
                        // 保存完了ダイアログ表示確認
                        completeDialog =
                            FindChildWindows(completeDialogParent, SaveCompleteDialogName)
                                .FirstOrDefault();
                        if (completeDialog != null)
                        {
                            return true;
                        }

                        // たまにデスクトップが親になる場合があるのでそちらも探す
                        completeDialog =
                            AutomationElement.RootElement.FindFirst(
                                TreeScope.Children,
                                new AndCondition(
                                    new PropertyCondition(
                                        AutomationElement.ControlTypeProperty,
                                        ControlType.Window),
                                    new PropertyCondition(
                                        AutomationElement.ProcessIdProperty,
                                        completeDialogParent.Current.ProcessId),
                                    new PropertyCondition(
                                        AutomationElement.NameProperty,
                                        SaveCompleteDialogName)));
                        if (completeDialog != null)
                        {
                            return true;
                        }

                        // 保存進捗ウィンドウ非表示確認
                        return
                            !FindChildWindows(progressWindowParent, SaveProgressDialogName)
                                .Any();
                    },
                    f => f);

                if (completeDialog != null)
                {
                    // OKボタンを探して押す
                    // 失敗しても先へ進む
                    var okButton =
                        FindFirstChildByControlType(completeDialog, ControlType.Button);
                    if (okButton != null)
                    {
                        InvokeElement(okButton);
                    }
                }

                // A.I.VOICE自動命名の場合はファイル名不明なので空のパスを返して終わり
                if (aiVoiceAutoNamed)
                {
                    return new FileSaveResult(true, @"");
                }

                // ファイル保存確認
                var resultPath = filePath;
                if (!File.Exists(resultPath))
                {
                    // VOICEROID2ライクソフトウェア機能でファイル分割される場合があるので
                    // そちらも存在チェック
                    // この場合キャンセルによる未完了は判別できない
                    resultPath =
                        Path.Combine(
                            Path.GetDirectoryName(filePath),
                            Path.GetFileNameWithoutExtension(filePath) + @"-0" +
                            Path.GetExtension(filePath));
                    if (!File.Exists(resultPath))
                    {
                        // ファイル非分割かつキャンセルした場合はここに来るはず
                        return
                            new FileSaveResult(
                                false,
                                error: @"音声保存がキャンセルされました。");
                    }
                }

                // 同時にテキストファイルが保存される場合があるため少し待つ
                // 保存されていなくても失敗にはしない
                var txtPath = Path.ChangeExtension(resultPath, @".txt");
                await RepeatUntil(() => File.Exists(txtPath), f => f, 10);

                return new FileSaveResult(true, resultPath);
            }

            #endregion

            #region ImplBase のオーバライド

            /// <summary>
            /// キャラクター名を取得する。
            /// </summary>
            /// <returns>キャラクター名。</returns>
            /// <remarks>
            /// 実行中の場合はボイスプリセット名を取得して返す。
            /// それ以外では Name の値をそのまま返す。
            /// </remarks>
            public override async Task<string> GetCharacterName()
            {
                string name;
                try
                {
                    name = this.FindPresetNameText()?.Current.Name;
                }
                catch
                {
                    name = null;
                }

                return name ?? (await base.GetCharacterName());
            }

            /// <summary>
            /// メインウィンドウ変更時の更新処理を行う。
            /// </summary>
            /// <returns>更新できたならば true 。そうでなければ false 。</returns>
            protected override async Task<bool> UpdateOnMainWindowChanged() =>
                // ボタンがあるか適当に調べておく
                (await this.FindButtons(ButtonType.Save)) != null;

            /// <summary>
            /// IsDialogShowing プロパティ値を更新する。
            /// </summary>
            /// <returns>更新した値。</returns>
            protected override async Task<bool> UpdateDialogShowing()
            {
                // FindDialogs 内で IsDialogShowing が更新される
                await this.FindDialogs();

                return this.IsDialogShowing;
            }

            /// <summary>
            /// 現在WAVEファイル保存処理中であるか否か調べる。
            /// </summary>
            /// <returns>保存処理中ならば true 。そうでなければ false 。</returns>
            /// <remarks>
            /// 直接本体側を操作して保存処理を行っている場合にも true を返すこと。
            /// </remarks>
            protected override async Task<bool> CheckSaving()
            {
                // 下記のいずれかのダイアログが表示されているならば保存中
                // - "音声保存" (オプションウィンドウor保存進捗ウィンドウ)
                // - "名前を付けて保存" (ファイルダイアログ)
                try
                {
                    return
                        (await this.FindDialogs())
                            .Select(d => d.Current.Name)
                            .Any(
                                name =>
                                    name == SaveOptionDialogName ||
                                    name == SaveFileDialogName);
                }
                catch { }
                return false;
            }

            /// <summary>
            /// 現在再生中であるか否か調べる。
            /// </summary>
            /// <returns>再生中ならば true 。そうでなければ false 。</returns>
            /// <remarks>
            /// 直接本体側を操作して再生処理を行っている場合にも true を返すこと。
            /// </remarks>
            protected override async Task<bool> CheckPlaying()
            {
                // 保存ボタンが押せない状態＝再生中と判定
                try
                {
                    var save = (await this.FindButtons(ButtonType.Save))?[0];
                    return (save?.Current.IsEnabled == false);
                }
                catch { }
                return false;
            }

            /// <summary>
            /// トークテキスト取得の実処理を行う。
            /// </summary>
            /// <returns>トークテキスト。取得できなかった場合は null 。</returns>
            protected override async Task<string> DoGetTalkText()
            {
                var edit = this.FindTalkTextEdit();
                return (edit == null) ? null : await Task.Run(() => GetElementValue(edit));
            }

            /// <summary>
            /// トークテキスト設定の実処理を行う。
            /// </summary>
            /// <param name="text">設定するトークテキスト。</param>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            protected override async Task<bool> DoSetTalkText(string text)
            {
                var edit = this.FindTalkTextEdit();
                return
                    edit != null &&
                    edit.Current.IsEnabled &&
                    await Task.Run(() => SetElementValue(edit, text));
            }

            /// <summary>
            /// トークテキスト再生の実処理を行う。
            /// </summary>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            protected override async Task<bool> DoPlay()
            {
                var buttons =
                    await this.FindButtons(
                        ButtonType.Head,
                        ButtonType.Play,
                        ButtonType.Save);
                if (buttons == null)
                {
                    return false;
                }
                var head = buttons[0];
                var play = buttons[1];
                var save = buttons[2];

                // 先頭ボタンと再生ボタン押下
                if (!(await Task.Run(() => InvokeElement(head) && InvokeElement(play))))
                {
                    return false;
                }

                try
                {
                    // 保存ボタンが無効になるかダイアログが出るまで少し待つ
                    // ダイアログが出ない限りは失敗にしない
                    await RepeatUntil(
                        async () =>
                            !save.Current.IsEnabled || (await this.UpdateDialogShowing()),
                        f => f,
                        15);
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                    return false;
                }

                return !this.IsDialogShowing;
            }

            /// <summary>
            /// トークテキスト再生停止の実処理を行う。
            /// </summary>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            protected override async Task<bool> DoStop()
            {
                var buttons = await this.FindButtons(ButtonType.Stop, ButtonType.Save);
                if (buttons == null)
                {
                    return false;
                }
                var stop = buttons[0];
                var save = buttons[1];

                // 停止ボタン押下
                if (!(await Task.Run(() => InvokeElement(stop))))
                {
                    return false;
                }

                // 保存ボタンが有効になるまで少し待つ
                var ok = false;
                try
                {
                    ok =
                        await RepeatUntil(
                            () => save.Current.IsEnabled,
                            f => f,
                            25);
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                    return false;
                }

                return ok;
            }

            /// <summary>
            /// WAVEファイル保存の実処理を行う。
            /// </summary>
            /// <param name="filePath">保存希望WAVEファイルパス。</param>
            /// <returns>保存処理結果。</returns>
            protected override async Task<FileSaveResult> DoSave(string filePath)
            {
                // 保存ボタン押下
                var saveButton = (await this.FindButtons(ButtonType.Save))?[0];
                if (saveButton == null)
                {
                    return new FileSaveResult(
                        false,
                        error: @"音声保存ボタンが見つかりませんでした。");
                }
                if (!(await Task.Run(() => InvokeElement(saveButton))))
                {
                    return new FileSaveResult(
                        false,
                        error: @"音声保存ボタンをクリックできませんでした。");
                }

                // ダイアログ検索
                var rootDialog = await this.DoFindSaveDialogTask();
                if (rootDialog == null)
                {
                    var msg =
                        (await this.UpdateDialogShowing()) ?
                            @"音声保存を開始できませんでした。" :
                            @"操作対象ウィンドウが見つかりませんでした。";
                    return new FileSaveResult(false, error: msg);
                }

                var fileDialog = rootDialog;
                AutomationElement parentWin = null;
                bool optionShown = false;
                bool aiVoiceAutoNamed = false;
                string errorMsg = null;
                string extraMsg = null;

                switch (rootDialog.Current.Name)
                {
                case SaveOptionDialogName:
                    // 毎回オプションウィンドウ表示設定有効時
                    {
                        optionShown = true;

                        // オプションウィンドウを親ウィンドウとする
                        parentWin = rootDialog;

                        // オプションウインドウのOKボタンをクリックして
                        // ファイルダイアログ or 進捗ウィンドウ(A.I.VOICE自動命名時) を出す
                        var t = await this.DoPushOkButtonOfSaveOptionDialogTask(rootDialog);
                        if (t.Item1 == null)
                        {
                            errorMsg = @"設定ウィンドウの操作に失敗しました。";
                            extraMsg = t.Item2;
                            break;
                        }

                        // ファイルダイアログならば当該 case 句へ移動
                        if (t.Item1.Current.Name == SaveFileDialogName)
                        {
                            fileDialog = t.Item1;
                            goto case SaveFileDialogName;
                        }

                        aiVoiceAutoNamed = true;
                    }
                    break;

                case SaveFileDialogName:
                    // 音声保存ダイアログ表示
                    {
                        errorMsg = @"音声保存ダイアログの操作に失敗しました。";

                        // オプションウィンドウが表示されていなければ
                        // メインウィンドウを親ウィンドウとする
                        parentWin = parentWin ?? MakeElementFromHandle(this.MainWindowHandle);
                        if (parentWin == null)
                        {
                            extraMsg = @"親ウィンドウが閉じられました。";
                            break;
                        }

                        // OKボタンとファイル名エディットを検索
                        var fileDialogElems = await this.DoFindFileDialogElements(fileDialog);
                        if (fileDialogElems == null)
                        {
                            extraMsg = @"ファイル名入力欄が見つかりませんでした。";
                            break;
                        }
                        var okButton = fileDialogElems.Item1;
                        var fileNameEdit = fileDialogElems.Item2;

                        // ファイルダイアログ処理
                        extraMsg = await this.DoSetFilePathToEditTask(fileNameEdit, filePath);
                        if (extraMsg != null)
                        {
                            break;
                        }
                        if ((await this.DoEraseOldFileTask(filePath)) < 0)
                        {
                            extraMsg = @"既存ファイルを削除できませんでした。";
                            break;
                        }
                        extraMsg = await this.DoDecideFilePathTask(okButton, parentWin);
                        if (extraMsg != null)
                        {
                            break;
                        }

                        errorMsg = null;
                    }
                    break;

                case AiVoiceSaveConfirmDialogName:
                    // A.I.VOICE自動命名による確認ダイアログ表示
                    // 自動命名有効 かつ オプションウィンドウ表示なし だとここに来る
                    {
                        errorMsg = @"確認ダイアログの操作に失敗しました。";
                        aiVoiceAutoNamed = true;

                        // メインウィンドウを親ウィンドウとする
                        parentWin = MakeElementFromHandle(this.MainWindowHandle);
                        if (parentWin == null)
                        {
                            extraMsg = @"親ウィンドウが閉じられました。";
                            break;
                        }

                        // 「はい」ボタン(AutomationId == "6")を検索
                        var yesButton =
                            await RepeatUntil(
                                () =>
                                    FindFirstChild(
                                        rootDialog,
                                        AutomationElement.AutomationIdProperty,
                                        @"6"),
                                elem => elem != null,
                                50);
                        if (yesButton == null)
                        {
                            extraMsg = @"決定ボタンが見つかりませんでした。";
                            break;
                        }

                        // 「はい」ボタンをクリック
                        if (!InvokeElement(yesButton))
                        {
                            extraMsg = @"決定ボタンをクリックできませんでした。";
                            break;
                        }

                        errorMsg = null;
                    }
                    break;

                default:
                    errorMsg = @"不明なダイアログが表示されました。";
                    break;
                }

                if (errorMsg != null)
                {
                    return new FileSaveResult(false, error: errorMsg, extraMessage: extraMsg);
                }

                // 音声保存確認
                var result =
                    await this.DoCheckFileSavedTask(
                        filePath,
                        optionShown,
                        parentWin,
                        aiVoiceAutoNamed);

                // 一旦VOICEROID2ライクソフトウェア側をアクティブにしないと
                // 再生, 音声保存, 再生時間 ボタンが無効状態のままになることがある
                // 停止操作(成否問わず)を行うことでアクティブ化する
                await this.DoStop();

                return result;
            }

            #endregion

            #region Win32 API 定義

            private const uint WM_CHAR = 0x0102;

            #endregion
        }
    }
}
