using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Automation;
using RucheHome.Util;
using RucheHome.Windows.WinApi;

namespace RucheHome.Voiceroid
{
    partial class ProcessFactory
    {
        /// <summary>
        /// A.I.VOICE用の IProcess インタフェース実装クラス。
        /// </summary>
        /// <remarks>
        /// A.I.VOICE Editor API が利用可能な version 1.3.0 以降のみサポートする。
        /// </remarks>
        private class AiVoiceImpl : Voiceroid2ImplBase
        {
            /// <summary>
            /// コンストラクタ。
            /// </summary>
            public AiVoiceImpl() : base(VoiceroidId.AiVoice, false, true)
            {
                // レジストリー情報を用いてラッパー作成
                this.Api = new AiVoiceApi();

                if (this.Api.IsAvailable)
                {
                    try
                    {
                        // 初期化
                        var hostNames = this.Api.GetAvailableHostNames();
                        if (hostNames?.Length > 0)
                        {
                            this.Api.Initialize(hostNames[0]);
                        }
                    }
                    catch (Exception ex)
                    {
                        ThreadTrace.WriteException(ex);
                    }
                }
            }

            /// <summary>
            /// A.I.VOICE Editor API ラッパーオブジェクトを取得または設定する。
            /// </summary>
            private AiVoiceApi Api { get; set; }

            /// <summary>
            /// 編集モードをテキスト形式にする。
            /// </summary>
            /// <returns>
            /// 成功したならば null 。そうでなければ処理失敗時の追加メッセージ。
            /// </returns>
            /// <remarks>
            /// <see cref="AiVoiceApi.TextEditMode"/> を用いて変更すると、
            /// その後の UI Automation 操作時にリスト形式へ戻ってしまう現象が発生した。
            /// そのため、 UI Automation によってテキスト形式タブを選択する方法を用いる。
            /// </remarks>
            private async Task<string> FixTextEditMode()
            {
                // 既にテキスト形式なら何もしない
                if (this.Api.TextEditMode == AiVoiceTextEditMode.Text)
                {
                    return null;
                }

                var root = MakeElementFromHandle(this.MainWindowHandle);

                // テキスト形式タブ検索
                AutomationElement textTab = null;
                try
                {
                    var textArea = FindFirstChildByAutomationId(root, @"c");
                    if (textArea != null)
                    {
                        var tabCtrl =
                            FindFirstChildByControlType(textArea, ControlType.Tab);
                        if (tabCtrl != null)
                        {
                            textTab =
                                FindFirstChildByControlType(
                                    tabCtrl,
                                    ControlType.TabItem);
                        }
                    }
                    if (textTab == null)
                    {
                        return @"編集モードタブが見つかりませんでした。";
                    }
                }
                catch
                {
                    return @"編集モードタブの検索に失敗しました。";
                }

                // テキスト形式タブの選択パターン取得
                if (
                    !textTab.TryGetCurrentPattern(
                        SelectionItemPattern.Pattern,
                        out var textTabSelectionTemp))
                {
                    return @"編集モードタブを選択できませんでした。";
                }
                var textTabSelection = (SelectionItemPattern)textTabSelectionTemp;

                // メインウィンドウをアクティブにする
                new Win32Window(this.MainWindowHandle).Activate();
                await this.WhenForInputIdle();

                // テキスト形式タブを選択
                textTabSelection.Select();

                // TextEditMode に反映されるまで待つ
                if (
                    !await RepeatUntil(
                        () => this.Api.TextEditMode == AiVoiceTextEditMode.Text,
                        f => f,
                        25))
                {
                    return @"編集モードを変更できませんでした。";
                }

                return null;
            }

            #region Voiceroid2ImplBase のオーバライド

            /// <inheritdoc path="//*[not(self::remarks)]"/>
            /// <remarks>
            /// 「ファイル」メニューの「テキストから音声ファイルを保存」のクリックを試みます。
            /// </remarks>
            protected override async Task<string> DoStartSave() =>
                await Task.Run(
                    async () =>
                    {
                        var root = MakeElementFromHandle(this.MainWindowHandle);

                        // ファイルメニュー検索
                        AutomationElement fileMenu = null;
                        try
                        {
                            var mainMenu =
                                FindFirstChildByControlType(root, ControlType.Menu);
                            if (mainMenu != null)
                            {
                                fileMenu = FindFirstChildByAccessKey(mainMenu, @"Alt+F");
                            }
                            if (fileMenu == null)
                            {
                                return @"ファイルメニューが見つかりませんでした。";
                            }
                        }
                        catch
                        {
                            return @"ファイルメニューの検索に失敗しました。";
                        }

                        // ファイルメニューの開閉パターン取得
                        if (
                            !fileMenu.TryGetCurrentPattern(
                                ExpandCollapsePattern.Pattern,
                                out var fileMenuExpColTemp))
                        {
                            return @"ファイルメニューを展開できませんでした。";
                        }
                        var fileMenuExpCol = (ExpandCollapsePattern)fileMenuExpColTemp;

                        // 編集モードをテキスト形式にする
                        if ((await this.FixTextEditMode()) is string msg)
                        {
                            return msg;
                        }

                        try
                        {
                            // ファイルメニュー展開
                            fileMenuExpCol.Expand();

                            // 音声保存メニュー検索
                            var saveMenu = FindFirstChildByAccessKey(fileMenu, @"W");
                            if (saveMenu == null)
                            {
                                return @"音声保存メニューが見つかりませんでした。";
                            }

                            // 音声保存メニュークリック
                            if (!InvokeElement(saveMenu))
                            {
                                return @"音声保存メニューをクリックできませんでした。";
                            }
                        }
                        catch
                        {
                            return @"音声保存メニューの操作に失敗しました。";
                        }
                        finally
                        {
                            // ファイルメニューを閉じる
                            try
                            {
                                fileMenuExpCol.Collapse();
                            }
                            catch { }
                        }

                        return null;
                    });

            /// <inheritdoc path="//*[not(self::remarks)]"/>
            /// <remarks>
            /// 正常に接続中の場合はボイスプリセット名を取得して返す。
            /// それ以外では <see cref="ImplBase.Name">Name</see> の値をそのまま返す。
            /// </remarks>
            public override async Task<string> GetCharacterName()
            {
                string name;
                try
                {
                    name = await Task.Run(() => this.Api.CurrentVoicePresetName);
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                    name = null;
                }

                return name ?? this.Name;
            }

            /// <inheritdoc/>
            protected override bool IsMainWindowTitle(string title) =>
                title?.Contains(@"A.I.VOICE") == true;

            /// <inheritdoc/>
            protected override async Task<bool> UpdateOnMainWindowChanged() =>
                await Task.Run(this.UpdateOnMainWindowChangedImpl);

            /// <summary>
            /// <see cref="UpdateOnMainWindowChanged"/> の実処理を行う。
            /// </summary>
            /// <returns>更新できたならば true 。そうでなければ false 。</returns>
            private bool UpdateOnMainWindowChangedImpl()
            {
                if (!this.Api.IsAvailable)
                {
                    // 実行中プロセスの配置先取得
                    var installPath = this.ExecutablePath;
                    if (installPath == null)
                    {
                        return false;
                    }
                    installPath = Path.GetDirectoryName(installPath);

                    // 既にロード試行済みのパスなら終わり
                    if (installPath == null || installPath == this.Api.InstallPath)
                    {
                        return false;
                    }

                    // ラッパー作成し直してロード試行
                    this.Api = new AiVoiceApi(installPath);
                    if (!this.Api.IsAvailable)
                    {
                        return false;
                    }
                }

                // ここまで来たらロード済みのはず
                if (!this.Api.IsInitialized)
                {
                    // 初期化
                    try
                    {
                        var names = this.Api.GetAvailableHostNames();
                        if (names?.Length > 0)
                        {
                            this.Api.Initialize(names[0]);
                        }
                        if (!this.Api.IsInitialized)
                        {
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        ThreadTrace.WriteException(ex);
                        return false;
                    }
                }

                // ここまで来たら初期化済みのはず
                switch (this.Api.Status)
                {
                case AiVoiceHostStatus.NotConnected:
                    try
                    {
                        // 接続
                        this.Api.Connect();

                        return
                            (this.Api.Status == AiVoiceHostStatus.Idle) ||
                            (this.Api.Status == AiVoiceHostStatus.Busy);
                    }
                    catch (Exception ex)
                    {
                        ThreadTrace.WriteException(ex);
                    }
                    break;

                case AiVoiceHostStatus.Idle:
                case AiVoiceHostStatus.Busy:
                    return true;

                case AiVoiceHostStatus.NotAvailable:
                case AiVoiceHostStatus.NotRunning:
                default:
                    break;
                }

                return false;
            }

            /// <inheritdoc/>
            protected override async Task<bool> CheckPlaying()
            {
                // ビジーかつ音声保存中ではないなら再生中と判定
                try
                {
                    return
                        (await Task.Run(() => this.Api.Status == AiVoiceHostStatus.Busy)) &&
                        !(await this.CheckSaving());
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                }

                return false;
            }

            /// <inheritdoc/>
            protected override async Task<string> DoGetTalkText() =>
                await Task.Run(() => this.Api.Text);

            /// <inheritdoc/>
            protected override async Task<bool> DoSetTalkText(string text) =>
                await Task.Run(
                    async () =>
                    {
                        try
                        {
                            // 編集モードをテキスト形式にする
                            if ((await this.FixTextEditMode()) is string msg)
                            {
                                ThreadTrace.WriteLine(msg);
                                return false;
                            }

                            this.Api.Text = text;

                            // テキスト選択範囲をリセット
                            this.Api.TextSelectionStart = 0;
                            this.Api.TextSelectionLength = 0;
                        }
                        catch (Exception ex)
                        {
                            ThreadTrace.WriteException(ex);
                            return false;
                        }

                        return true;
                    });

            /// <inheritdoc/>
            protected override async Task<bool> DoPlay() =>
                await Task.Run(
                    async () =>
                    {
                        try
                        {
                            // 編集モードをテキスト形式にする
                            if ((await this.FixTextEditMode()) is string msg)
                            {
                                ThreadTrace.WriteLine(msg);
                                return false;
                            }

                            // 全体が再生されるようにテキスト選択範囲をリセット
                            this.Api.TextSelectionStart = 0;
                            this.Api.TextSelectionLength = 0;

                            this.Api.Play();
                        }
                        catch (Exception ex)
                        {
                            ThreadTrace.WriteException(ex);
                            return false;
                        }

                        return true;
                    });

            /// <inheritdoc/>
            protected override async Task<bool> DoStop()
            {
                try
                {
                    // 停止
                    await Task.Run(() => this.Api.Stop());

                    // ビジーでなくなるまで少し待つ
                    // 失敗にはしない
                    await RepeatUntil(
                        () => this.Api.Status, s => s != AiVoiceHostStatus.Busy, 15);

                    return true;
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                }

                return false;
            }

            #endregion
        }
    }
}
