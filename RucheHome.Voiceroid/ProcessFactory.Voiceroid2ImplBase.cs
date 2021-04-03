using System;
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

            #region メインウィンドウUI

            /// <summary>
            /// ボタン種別列挙。
            /// </summary>
            private enum ButtonType
            {
                /// <summary>
                /// 再生
                /// </summary>
                Play = 0,

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
            /// ボタン種別数。
            /// </summary>
            private static readonly int ButtonTypeCount =
                Enum.GetValues(typeof(ButtonType)).Length;

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
                            var buttons =
                                buttonsRoot.FindAll(
                                    TreeScope.Children,
                                    new PropertyCondition(
                                        AutomationElement.ControlTypeProperty,
                                        ControlType.Button));
                            if (buttons.Count >= ButtonTypeCount)
                            {
                                for (int ti = 0; ti < types.Length; ++ti)
                                {
                                    results[ti] = buttons[(int)types[ti]];
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
            /// トークテキストエディットコントロールを検索する。
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

            #endregion

            #region ダイアログ

            /// <summary>
            /// ダイアログ種別列挙。
            /// </summary>
            private enum DialogType
            {
                /// <summary>
                /// 共通通知ダイアログ。(OKボタンのみのタスクダイアログ)
                /// </summary>
                CommonNotify,

                /// <summary>
                /// 共通確認ダイアログ。(はい/いいえボタンのタスクダイアログ)
                /// </summary>
                CommonConfirm,

                /// <summary>
                /// 共通ファイル保存ダイアログ。
                /// </summary>
                CommonFileSave,

                /// <summary>
                /// 音声保存オプションウインドウ。
                /// </summary>
                SaveOption,

                /// <summary>
                /// 音声保存進捗ウィンドウ。
                /// </summary>
                SaveProgress,
            }

            /// <summary>
            /// ダイアログ種別配列。
            /// </summary>
            private static readonly DialogType[] DialogTypes =
                (DialogType[])Enum.GetValues(typeof(DialogType));

            /// <summary>
            /// 共通ダイアログのウィンドウクラス名。
            /// </summary>
            private const string CommonDialogClassName = @"#32770";

            /// <summary>
            /// 共通通知ダイアログUI情報クラス。
            /// </summary>
            private sealed class CommonNotifyDialogUIInfo
            {
                /// <summary>
                /// コンストラクタ。
                /// </summary>
                /// <param name="okButton">OKボタン。</param>
                public CommonNotifyDialogUIInfo(AutomationElement okButton) =>
                    this.OkButton =
                        okButton ?? throw new ArgumentNullException(nameof(okButton));

                /// <summary>
                /// OKボタンを取得する。
                /// </summary>
                public AutomationElement OkButton { get; }
            }

            /// <summary>
            /// 共通確認ダイアログUI情報クラス。
            /// </summary>
            private sealed class CommonConfirmDialogUIInfo
            {
                /// <summary>
                /// コンストラクタ。
                /// </summary>
                /// <param name="yesButton">はいボタン。</param>
                /// <param name="noButton">いいえボタン。</param>
                public CommonConfirmDialogUIInfo(
                    AutomationElement yesButton,
                    AutomationElement noButton)
                {
                    this.YesButton =
                        yesButton ?? throw new ArgumentNullException(nameof(yesButton));
                    this.NoButton =
                        noButton ?? throw new ArgumentNullException(nameof(noButton));
                }

                /// <summary>
                /// はいボタンを取得する。
                /// </summary>
                public AutomationElement YesButton { get; }

                /// <summary>
                /// いいえボタンを取得する。
                /// </summary>
                public AutomationElement NoButton { get; }
            }

            /// <summary>
            /// 共通ファイル保存ダイアログUI情報クラス。
            /// </summary>
            private sealed class CommonFileSaveDialogUIInfo
            {
                /// <summary>
                /// コンストラクタ。
                /// </summary>
                /// <param name="fileNameEdit">ファイル名入力エディット。</param>
                /// <param name="saveButton">保存ボタン。</param>
                public CommonFileSaveDialogUIInfo(
                    AutomationElement fileNameEdit,
                    AutomationElement saveButton)
                {
                    this.FileNameEdit =
                        fileNameEdit ?? throw new ArgumentNullException(nameof(fileNameEdit));
                    this.SaveButton =
                        saveButton ?? throw new ArgumentNullException(nameof(saveButton));
                }

                /// <summary>
                /// ファイル名入力エディットを取得する。
                /// </summary>
                public AutomationElement FileNameEdit { get; }

                /// <summary>
                /// 保存ボタンを取得する。
                /// </summary>
                public AutomationElement SaveButton { get; }
            }

            /// <summary>
            /// 音声保存オプションウィンドウUI情報クラス。
            /// </summary>
            private sealed class SaveOptionWindowUIInfo
            {
                /// <summary>
                /// コンストラクタ。
                /// </summary>
                /// <param name="okButton">OKボタン。</param>
                public SaveOptionWindowUIInfo(AutomationElement okButton) =>
                    this.OkButton =
                        okButton ?? throw new ArgumentNullException(nameof(okButton));

                /// <summary>
                /// OKボタンを取得する。
                /// </summary>
                public AutomationElement OkButton { get; }
            }

            /// <summary>
            /// 音声保存進捗ウィンドウUI情報クラス。
            /// </summary>
            private sealed class SaveProgressWindowUIInfo
            {
                /// <summary>
                /// コンストラクタ。
                /// </summary>
                /// <param name="progressBar">プログレスバー。</param>
                public SaveProgressWindowUIInfo(AutomationElement progressBar) =>
                    this.ProgressBar =
                        progressBar ?? throw new ArgumentNullException(nameof(progressBar));

                /// <summary>
                /// プログレスバーを取得する。
                /// </summary>
                public AutomationElement ProgressBar { get; }
            }

            /// <summary>
            /// ダイアログ群を検索する。
            /// </summary>
            /// <param name="parent">
            /// 親ウィンドウ。
            /// null ならばメインウィンドウを親とし、ダイアログが見つかったか否かに応じて
            /// <see cref="IsDialogShowing"/> プロパティ値を更新する。
            /// </param>
            /// <returns>ダイアログ群。見つからなければ空の配列。</returns>
            private async Task<AutomationElement[]> FindDialogs(
                AutomationElement parent = null)
            {
                var root = parent;
                if (root == null)
                {
                    var mainHandle = this.MainWindowHandle;
                    if (!this.IsRunning || mainHandle == IntPtr.Zero)
                    {
                        return new AutomationElement[0];
                    }

                    root = MakeElementFromHandle(mainHandle);
                    if (root == null)
                    {
                        return new AutomationElement[0];
                    }
                }

                var dialogs =
                    await Task.Run(
                        () =>
                            FindChildWindows(root)
                                .Where(
                                    d =>
                                    {
                                        try
                                        {
                                            return
                                                d.TryGetCurrentPattern(
                                                    WindowPattern.Pattern,
                                                    out var pattern) &&
                                                ((WindowPattern)pattern).Current.IsModal;
                                        }
                                        catch { }
                                        return false;
                                    })
                                .ToArray());

                if (parent == null)
                {
                    this.IsDialogShowing = (dialogs.Length > 0);
                }

                return dialogs;
            }

            /// <summary>
            /// ダイアログ種別を決定する。
            /// </summary>
            /// <param name="dialog">ダイアログ。</param>
            /// <param name="targetTypes">
            /// 確認対象のダイアログ種別配列。 null または空の配列ならば全種別を対象とする。
            /// </param>
            /// <returns>
            /// ダイアログ種別とそれに対応するUI情報の Tuple 。決定できなければ null 。
            /// </returns>
            private async Task<Tuple<DialogType, object>> DecideDialogType(
                AutomationElement dialog,
                params DialogType[] targetTypes)
            {
                if (dialog == null)
                {
                    throw new ArgumentNullException(nameof(dialog));
                }

                // 入力可能状態まで待機
                if (!(await this.WhenForInputIdle()))
                {
                    return null;
                }

                var types = targetTypes;
                if (types == null || types.Length == 0)
                {
                    types = DialogTypes;
                }

                return
                    await Task.Run(
                        () =>
                        {
                            // 共通ダイアログか否か
                            bool common = (dialog.Current.ClassName == CommonDialogClassName);

                            // 子ボタン配列
                            var buttons =
                                dialog
                                    .FindAll(
                                        TreeScope.Children,
                                        new PropertyCondition(
                                            AutomationElement.ControlTypeProperty,
                                            ControlType.Button))
                                    .Cast<AutomationElement>()
                                    .ToArray();

                            foreach (var type in types)
                            {
                                switch (type)
                                {
                                case DialogType.CommonNotify:
                                    {
                                        if (
                                            common &&
                                            TryGetUIInfo(
                                                dialog,
                                                buttons,
                                                out CommonNotifyDialogUIInfo uiInfo))
                                        {
                                            return
                                                Tuple.Create(
                                                    DialogType.CommonNotify,
                                                    (object)uiInfo);
                                        }
                                    }
                                    break;

                                case DialogType.CommonConfirm:
                                    {
                                        if (
                                            common &&
                                            TryGetUIInfo(
                                                dialog,
                                                buttons,
                                                out CommonConfirmDialogUIInfo uiInfo))
                                        {
                                            return
                                                Tuple.Create(
                                                    DialogType.CommonConfirm,
                                                    (object)uiInfo);
                                        }
                                    }
                                    break;

                                case DialogType.CommonFileSave:
                                    {
                                        if (
                                            common &&
                                            TryGetUIInfo(
                                                dialog,
                                                buttons,
                                                out CommonFileSaveDialogUIInfo uiInfo))
                                        {
                                            return
                                                Tuple.Create(
                                                    DialogType.CommonFileSave,
                                                    (object)uiInfo);
                                        }
                                    }
                                    break;

                                case DialogType.SaveOption:
                                    {
                                        if (
                                            !common &&
                                            TryGetUIInfo(
                                                dialog,
                                                buttons,
                                                out SaveOptionWindowUIInfo uiInfo))
                                        {
                                            return
                                                Tuple.Create(
                                                    DialogType.SaveOption,
                                                    (object)uiInfo);
                                        }
                                    }
                                    break;

                                case DialogType.SaveProgress:
                                    {
                                        if (
                                            !common &&
                                            TryGetUIInfo(
                                                dialog,
                                                buttons,
                                                out SaveProgressWindowUIInfo uiInfo))
                                        {
                                            return
                                                Tuple.Create(
                                                    DialogType.SaveProgress,
                                                    (object)uiInfo);
                                        }
                                    }
                                    break;

                                default:
                                    break;
                                }
                            }

                            return null;
                        });
            }

            /// <summary>
            /// 共通通知ダイアログのUI情報取得を試みる。
            /// </summary>
            /// <param name="dialog">ダイアログ。</param>
            /// <param name="childButtons">子ボタン配列。</param>
            /// <param name="uiInfo">UI情報の設定先。</param>
            /// <returns>取得できたならば true 。そうでなければ false 。</returns>
            private static bool TryGetUIInfo(
                AutomationElement dialog,
                AutomationElement[] childButtons,
                out CommonNotifyDialogUIInfo uiInfo)
            {
                uiInfo = null;

                if (dialog == null || childButtons?.Length != 1)
                {
                    return false;
                }

                var okButton = childButtons[0];
                if (okButton?.Current.AutomationId != @"2")
                {
                    return false;
                }

                uiInfo = new CommonNotifyDialogUIInfo(okButton);
                return true;
            }

            /// <summary>
            /// 共通確認ダイアログのUI情報取得を試みる。
            /// </summary>
            /// <param name="dialog">ダイアログ。</param>
            /// <param name="childButtons">子ボタン配列。</param>
            /// <param name="uiInfo">UI情報の設定先。</param>
            /// <returns>取得できたならば true 。そうでなければ false 。</returns>
            private static bool TryGetUIInfo(
                AutomationElement dialog,
                AutomationElement[] childButtons,
                out CommonConfirmDialogUIInfo uiInfo)
            {
                uiInfo = null;

                if (dialog == null || childButtons?.Length != 2)
                {
                    return false;
                }

                var yesButton = childButtons[0];
                var noButton = childButtons[1];
                if (
                    yesButton?.Current.AutomationId != @"6" ||
                    noButton?.Current.AutomationId != @"7")
                {
                    return false;
                }

                uiInfo = new CommonConfirmDialogUIInfo(yesButton, noButton);
                return true;
            }

            /// <summary>
            /// 共通ファイル保存ダイアログのUI情報取得を試みる。
            /// </summary>
            /// <param name="dialog">ダイアログ。</param>
            /// <param name="childButtons">子ボタン配列。</param>
            /// <param name="uiInfo">UI情報の設定先。</param>
            /// <returns>取得できたならば true 。そうでなければ false 。</returns>
            private static bool TryGetUIInfo(
                AutomationElement dialog,
                AutomationElement[] childButtons,
                out CommonFileSaveDialogUIInfo uiInfo)
            {
                uiInfo = null;

                if (dialog == null)
                {
                    return false;
                }

                // 保存ボタン検索
                var saveButton =
                    childButtons?.FirstOrDefault(b => b?.Current.AutomationId == @"1");
                if (saveButton == null)
                {
                    return false;
                }

                // ファイル名エディット検索
                var edit = FindFileDialogFileNameEdit(dialog);
                if (edit == null)
                {
                    return false;
                }

                uiInfo = new CommonFileSaveDialogUIInfo(edit, saveButton);
                return true;
            }

            /// <summary>
            /// 音声保存オプションウィンドウのUI情報取得を試みる。
            /// </summary>
            /// <param name="window">ウィンドウ。</param>
            /// <param name="childButtons">子ボタン配列。</param>
            /// <param name="uiInfo">UI情報の設定先。</param>
            /// <returns>取得できたならば true 。そうでなければ false 。</returns>
            private static bool TryGetUIInfo(
                AutomationElement window,
                AutomationElement[] childButtons,
                out SaveOptionWindowUIInfo uiInfo)
            {
                uiInfo = null;

                if (window == null || childButtons?.Length != 2)
                {
                    return false;
                }

                // キャンセルボタンの AutomationId が "b" か否かで判断
                var okButton = childButtons[0];
                if (okButton == null || childButtons[1]?.Current.AutomationId != @"b")
                {
                    return false;
                }

                uiInfo = new SaveOptionWindowUIInfo(okButton);
                return true;
            }

            /// <summary>
            /// 音声保存進捗ウィンドウのUI情報取得を試みる。
            /// </summary>
            /// <param name="window">ウィンドウ。</param>
            /// <param name="childButtons">子ボタン配列。</param>
            /// <param name="uiInfo">UI情報の設定先。</param>
            /// <returns>取得できたならば true 。そうでなければ false 。</returns>
            private static bool TryGetUIInfo(
                AutomationElement window,
                AutomationElement[] childButtons,
                out SaveProgressWindowUIInfo uiInfo)
            {
                uiInfo = null;

                if (window == null || childButtons?.Length != 1)
                {
                    return false;
                }

                var progressBar =
                    FindFirstChild(
                        window,
                        AutomationElement.ControlTypeProperty,
                        ControlType.ProgressBar);
                if (progressBar == null)
                {
                    return false;
                }

                uiInfo = new SaveProgressWindowUIInfo(progressBar);
                return true;
            }

            #endregion

            #region 音声保存処理

            /// <summary>
            /// 音声保存ダイアログを検索する処理を行う。
            /// </summary>
            /// <returns>
            /// ダイアログ、ダイアログ種別、UI情報の Tuple 。見つからなければ null 。
            /// </returns>
            /// <remarks>
            /// オプションウィンドウ、ファイルダイアログ、
            /// 確認ダイアログ(A.I.VOICEのみ)のいずれかを返す。
            /// </remarks>
            private async Task<Tuple<AutomationElement, DialogType, object>>
            DoFindSaveDialogTask()
            {
                try
                {
                    return
                        await RepeatUntil(
                            async () =>
                            {
                                foreach (var dialog in await this.FindDialogs())
                                {
                                    var t =
                                        await this.DecideDialogType(
                                            dialog,
                                            DialogType.CommonFileSave,
                                            DialogType.SaveOption,
                                            DialogType.CommonConfirm);
                                    if (t != null)
                                    {
                                        return Tuple.Create(dialog, t.Item1, t.Item2);
                                    }
                                }

                                return null;
                            },
                            (Tuple<AutomationElement, DialogType, object> t) => t != null,
                            150);
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
            /// <param name="okButton">音声保存オプションウィンドウのOKボタン。</param>
            /// <returns>
            /// 処理に成功したならば、
            /// ファイルダイアログまたは保存進捗ウィンドウ(A.I.VOICE自動命名時)、
            /// ダイアログ種別、UI情報の Tuple 。
            /// そうでなければ処理失敗時の追加メッセージ文字列。
            /// </returns>
            private async Task<object> DoPushOkButtonOfSaveOptionDialogTask(
                AutomationElement optionDialog,
                AutomationElement okButton)
            {
                if (optionDialog == null)
                {
                    throw new ArgumentNullException(nameof(optionDialog));
                }
                if (okButton == null)
                {
                    throw new ArgumentNullException(nameof(okButton));
                }

                // 入力可能状態まで待機
                if (!(await this.WhenForInputIdle()))
                {
                    return @"入力可能状態になりませんでした。";
                }

                try
                {
                    // OKボタン押下
                    if (!InvokeElement(okButton))
                    {
                        return @"OKボタンをクリックできませんでした。";
                    }

                    // ファイルダイアログ or 保存進捗ウィンドウ(A.I.VOICE自動命名時) 検索
                    var result =
                        await RepeatUntil(
                            async () =>
                            {
                                foreach (var dialog in await this.FindDialogs(optionDialog))
                                {
                                    var t =
                                        await this.DecideDialogType(
                                            dialog,
                                            DialogType.CommonFileSave,
                                            DialogType.SaveProgress);
                                    if (t != null)
                                    {
                                        return Tuple.Create(dialog, t.Item1, t.Item2);
                                    }
                                }

                                return null;
                            },
                            (Tuple<AutomationElement, DialogType, object> t) => t != null,
                            150);
                    if (result == null)
                    {
                        return @"ウィンドウが見つかりませんでした。";
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                    return @"内部エラーが発生しました。";
                }
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
                            async () =>
                            {
                                foreach (var dialog in await this.FindDialogs(fileDialogParent))
                                {
                                    var t =
                                        await this.DecideDialogType(
                                            dialog,
                                            DialogType.CommonFileSave);
                                    if (t != null)
                                    {
                                        return false;
                                    }
                                }
                                return true;
                            },
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
                        async () =>
                        {
                            foreach (var dialog in await this.FindDialogs(progressWindowParent))
                            {
                                var t =
                                    await this.DecideDialogType(
                                        dialog,
                                        DialogType.SaveProgress);
                                if (t != null)
                                {
                                    return dialog;
                                }
                            }

                            return null;
                        },
                        (AutomationElement d) => d != null,
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
                CommonNotifyDialogUIInfo completeDialogUI = null;
                await RepeatUntil(
                    async () =>
                    {
                        // 保存完了ダイアログ表示確認
                        var dialogs = await this.FindDialogs(completeDialogParent);
                        foreach (var dialog in dialogs)
                        {
                            var t = await this.DecideDialogType(dialog, DialogType.CommonNotify);
                            if (t?.Item1 == DialogType.CommonNotify)
                            {
                                completeDialogUI = (CommonNotifyDialogUIInfo)t.Item2;
                                return true;
                            }
                        }

                        // たまにデスクトップが親になる場合があるのでそちらも探す
                        {
                            var dialog =
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
                                            AutomationElement.ClassNameProperty,
                                            CommonDialogClassName)));
                            if (dialog != null)
                            {
                                var t =
                                    await this.DecideDialogType(dialog, DialogType.CommonNotify);
                                if (t?.Item1 == DialogType.CommonNotify)
                                {
                                    completeDialogUI = (CommonNotifyDialogUIInfo)t.Item2;
                                    return true;
                                }
                            }
                        }

                        // 保存進捗ウィンドウ非表示確認
                        if (progressWindowParent != completeDialogParent)
                        {
                            dialogs = await this.FindDialogs(progressWindowParent);
                        }
                        foreach (var dialog in dialogs)
                        {
                            var t = await this.DecideDialogType(dialog, DialogType.SaveProgress);
                            if (t != null)
                            {
                                return false;
                            }
                        }
                        return true;
                    },
                    f => f);

                if (completeDialogUI != null)
                {
                    // OKボタンを探して押す
                    // 失敗しても先へ進む
                    _ = InvokeElement(completeDialogUI.OkButton);
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
                _ = await this.FindDialogs();

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
                // - 共通ファイル保存ダイアログ
                // - 音声保存オプションウィンドウ
                // - 音声保存進捗ウィンドウ
                // - 共通確認ダイアログ(A.I.VOICEのみ)
                try
                {
                    foreach (var dialog in await this.FindDialogs())
                    {
                        var t =
                            await this.DecideDialogType(
                                dialog,
                                DialogType.CommonFileSave,
                                DialogType.SaveOption,
                                DialogType.SaveProgress,
                                DialogType.CommonConfirm);
                        if (t != null)
                        {
                            return true;
                        }
                    }
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
                var saveDialogTuple = await this.DoFindSaveDialogTask();
                if (saveDialogTuple == null)
                {
                    var msg =
                        (await this.UpdateDialogShowing()) ?
                            @"音声保存を開始できませんでした。" :
                            @"操作対象ウィンドウが見つかりませんでした。";
                    return new FileSaveResult(false, error: msg);
                }
                var rootDialog = saveDialogTuple.Item1;
                var dialogType = saveDialogTuple.Item2;
                var uiInfo = saveDialogTuple.Item3;

                AutomationElement parentWin = null;
                bool optionShown = false;
                bool aiVoiceAutoNamed = false;
                string errorMsg = null;
                string extraMsg = null;

                switch (dialogType)
                {
                case DialogType.SaveOption:
                    // 毎回オプションウィンドウ表示設定有効時
                    {
                        optionShown = true;

                        // オプションウィンドウを親ウィンドウとする
                        parentWin = rootDialog;

                        // オプションウインドウのOKボタンをクリックして
                        // ファイルダイアログ or 進捗ウィンドウ(A.I.VOICE自動命名時) を出す
                        var temp =
                            await this.DoPushOkButtonOfSaveOptionDialogTask(
                                rootDialog,
                                ((SaveOptionWindowUIInfo)uiInfo).OkButton);
                        if (temp is string s)
                        {
                            errorMsg = @"設定ウィンドウの操作に失敗しました。";
                            extraMsg = s;
                            break;
                        }

                        saveDialogTuple = (Tuple<AutomationElement, DialogType, object>)temp;
                        rootDialog = saveDialogTuple.Item1;
                        dialogType = saveDialogTuple.Item2;
                        uiInfo = saveDialogTuple.Item3;

                        // ファイルダイアログならば当該 case 句へ移動
                        if (dialogType == DialogType.CommonFileSave)
                        {
                            goto case DialogType.CommonFileSave;
                        }

                        aiVoiceAutoNamed = true;
                    }
                    break;

                case DialogType.CommonFileSave:
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

                        var ui = (CommonFileSaveDialogUIInfo)uiInfo;

                        // ファイルダイアログ処理
                        extraMsg = await this.DoSetFilePathToEditTask(ui.FileNameEdit, filePath);
                        if (extraMsg != null)
                        {
                            break;
                        }
                        if ((await this.DoEraseOldFileTask(filePath)) < 0)
                        {
                            extraMsg = @"既存ファイルを削除できませんでした。";
                            break;
                        }
                        extraMsg = await this.DoDecideFilePathTask(ui.SaveButton, parentWin);
                        if (extraMsg != null)
                        {
                            break;
                        }

                        errorMsg = null;
                    }
                    break;

                case DialogType.CommonConfirm:
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

                        // 「はい」ボタンをクリック
                        if (!InvokeElement(((CommonConfirmDialogUIInfo)uiInfo).YesButton))
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
