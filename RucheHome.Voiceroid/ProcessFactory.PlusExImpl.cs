using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Automation;
using RucheHome.Util;
using RucheHome.Windows.WinApi;

namespace RucheHome.Voiceroid
{
    partial class ProcessFactory
    {
        /// <summary>
        /// VOICEROID+ EX シリーズ互換アプリ用の IProcess インタフェース実装クラス。
        /// </summary>
        private sealed class PlusExImpl : ImplBase
        {
            /// <summary>
            /// コンストラクタ。
            /// </summary>
            /// <param name="id">VOICEROID識別ID。</param>
            public PlusExImpl(VoiceroidId id) : base(id, false)
            {
            }

            /// <summary>
            /// UI Automation によるUI操作を許可するか否かを取得または設定する。
            /// </summary>
            public bool IsUIAutomationEnabled { get; set; } = true;

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
                            .FindDescendants(@"Edit")?
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
                    var mainWin = new Win32Window(this.MainWindowHandle);
                    controls = mainWin.FindDescendants();
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                    return false;
                }
                if (controls == null)
                {
                    // ちょうどアプリ終了したタイミング等で null になりうる
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
                var mainHandle = this.MainWindowHandle;
                if (mainHandle == IntPtr.Zero)
                {
                    return null;
                }

                if (!DialogTitles.TryGetValue(type, out var title))
                {
                    return null;
                }

                return
                    await Win32Window.FromDesktop()
                        .FindChildren(text: title)
                        .ToObservable()
                        .FirstOrDefaultAsync(w => w.GetOwner()?.Handle == mainHandle);
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
                if (!dialogs.TryGetValue(DialogType.Save, out var dialog))
                {
                    ThreadTrace.WriteLine(@"音声保存ダイアログが見つかりません。");
                    return null;
                }

                // AutomationElement 検索
                AutomationElement okButtonElem = null;
                AutomationElement editElem = null;
                if (uiAutomationEnabled)
                {
                    var dialogElem = MakeElementFromHandle(dialog.Handle);
                    if (dialogElem != null)
                    {
                        var elems = await this.DoFindFileDialogElements(dialogElem);
                        if (elems != null)
                        {
                            okButtonElem = elems.Item1;
                            editElem = elems.Item2;
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
                if (!(await this.WhenForInputIdle()))
                {
                    throw new InvalidOperationException(@"入力可能状態になりません。");
                }

                // フォーカス
                edit.SetFocus();

                // ファイルパス設定
                if (!SetElementValue(edit, filePath))
                {
                    throw new InvalidOperationException(@"ファイルパスを設定できません。");
                }
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
                if (!(await this.WhenForInputIdle()))
                {
                    throw new InvalidOperationException(@"入力可能状態になりません。");
                }
                okButton.SetFocus();

                // OKボタン押下
                if (!InvokeElement(okButton))
                {
                    throw new InvalidOperationException(@"OKボタンを押下できません。");
                }

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

            #region ImplBase のオーバライド

            /// <summary>
            /// アプリプロセス実行中ではない場合の状態セットアップを行う。
            /// </summary>
            protected override void SetupDeadState()
            {
                base.SetupDeadState();

                this.TalkEdit = null;
                this.PlayButton = null;
                this.StopButton = null;
                this.SaveButton = null;
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
            protected override bool IsMainWindowTitle(string title) =>
                title != null && (title.Contains(@"VOICEROID") || title.Contains(@"Talk"));

            /// <summary>
            /// メインウィンドウ変更時の更新処理を行う。
            /// </summary>
            /// <returns>更新できたならば true 。そうでなければ false 。</returns>
            protected override async Task<bool> UpdateOnMainWindowChanged() =>
                await Task.Run(() => this.UpdateControls());

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
            protected override async Task<bool> CheckSaving() =>
                // 保存ダイアログか保存進捗ダイアログが表示中なら保存中と判断
                (await this.FindDialogs()).Keys
                    .Any(t => t == DialogType.Save || t == DialogType.SaveProgress);

            /// <summary>
            /// 現在再生中であるか否か調べる。
            /// </summary>
            /// <returns>再生中ならば true 。そうでなければ false 。</returns>
            /// <remarks>
            /// 直接本体側を操作して再生処理を行っている場合にも true を返すこと。
            /// </remarks>
            protected override async Task<bool> CheckPlaying() =>
                // 保存ボタンが押せない状態＝再生中と判定
                await Task.FromResult(this.SaveButton?.IsEnabled == false);

            /// <summary>
            /// トークテキスト取得の実処理を行う。
            /// </summary>
            /// <returns>トークテキスト。取得できなかった場合は null 。</returns>
            protected override async Task<string> DoGetTalkText()
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
            /// トークテキスト設定の実処理を行う。
            /// </summary>
            /// <param name="text">設定するトークテキスト。</param>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            protected override async Task<bool> DoSetTalkText(string text)
            {
                var edit = this.TalkEdit;
                if (edit == null)
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
            /// トークテキスト再生の実処理を行う。
            /// </summary>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            protected override async Task<bool> DoPlay()
            {
                if (this.PlayButton == null || !(await this.SetTalkTextCursorToHead()))
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

                // 保存ボタンが無効になるかダイアログが出るまで少し待つ
                // ダイアログが出ない限りは失敗にしない
                await RepeatUntil(
                    async () =>
                        this.SaveButton?.IsEnabled != true ||
                        (await this.UpdateDialogShowing()),
                    f => f,
                    15);
                return !this.IsDialogShowing;
            }

            /// <summary>
            /// トークテキスト再生停止の実処理を行う。
            /// </summary>
            /// <returns>成功したならば true 。そうでなければ false 。</returns>
            protected override async Task<bool> DoStop()
            {
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
                return (enabled == true);
            }

            /// <summary>
            /// WAVEファイル保存の実処理を行う。
            /// </summary>
            /// <param name="filePath">保存希望WAVEファイルパス。</param>
            /// <returns>保存処理結果。</returns>
            protected override async Task<FileSaveResult> DoSave(string filePath)
            {
                // 保存ボタン押下
                if (this.SaveButton == null)
                {
                    return new FileSaveResult(
                        false,
                        error: @"音声保存ボタンが見つかりませんでした。");
                }
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
                            @"音声保存を開始できませんでした。" :
                            @"ファイル保存ダイアログが見つかりませんでした。";
                    return new FileSaveResult(false, error: msg);
                }

                string extraMsg = null;

                // ファイル保存
                if (!(await this.DoSetFilePathToEditTask(itemSet, filePath)))
                {
                    extraMsg = @"ファイル名を設定できませんでした。";
                }
                else if (!(await this.DoEraseOldFileTask(filePath)))
                {
                    extraMsg = @"既存ファイルの削除に失敗しました。";
                }
                else if (!(await this.DoDecideFilePathTask(itemSet)))
                {
                    extraMsg = @"ファイル名の確定操作に失敗しました。";
                }
                else if (!(await this.DoCheckFileSavedTask(filePath)))
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

                return new FileSaveResult(true, filePath);
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
