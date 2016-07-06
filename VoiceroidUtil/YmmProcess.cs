using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Automation;
using RucheHome.Windows.WinApi;

namespace VoiceroidUtil
{
    /// <summary>
    /// 『ゆっくりMovieMaker』プロセスを操作するクラス。
    /// </summary>
    public class YmmProcess
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public YmmProcess()
        {
        }

        /// <summary>
        /// プロセスが起動しているか否かを取得する。
        /// </summary>
        public bool IsRunning
        {
            get { return (this.MainWindow != null); }
        }

        /// <summary>
        /// タイムラインウィンドウが開いているか否かを取得する。
        /// </summary>
        public bool IsTimelineOpened
        {
            get { return (this.TimelineWindow != null); }
        }

        /// <summary>
        /// 状態を更新する。
        /// </summary>
        /// <returns>
        /// タイムラインウィンドウが開いているならば true 。そうでなければ false 。
        /// </returns>
        public async Task<bool> Update()
        {
            // プロセス検索
            var process = Process.GetProcessesByName(ProcessName).FirstOrDefault();
            if (process == null)
            {
                this.MainWindow = null;
                this.TimelineWindow = null;
                return false;
            }

            // メインウィンドウ設定
            this.MainWindow = new Win32Window(process.MainWindowHandle);

            // タイムラインウィンドウ検索
            var tlWin = await this.FindTimelineWindow();
            if (tlWin == null)
            {
                this.TimelineWindow = null;
                return false;
            }

            // 初回 or 前回と異なるハンドル
            if (this.TimelineWindow == null || tlWin.Handle != this.TimelineWindow.Handle)
            {
                // タイムラインウィンドウの AutomationElement 作成
                var tlElem = AutomationElement.FromHandle(tlWin.Handle);

                // 各UIの AutomationElement 作成
                // アイテムが多いと時間が掛かるので非同期で
                await Task.Run(
                    () =>
                    {
                        this.SpeechEditElement =
                            FindDescendant(
                                tlElem,
                                new PropertyCondition(
                                    AutomationElement.AutomationIdProperty,
                                    TimelineSpeechEditAutomationId));
                        this.CharaComboElement =
                            FindDescendant(
                                tlElem,
                                new PropertyCondition(
                                    AutomationElement.AutomationIdProperty,
                                    TimelineCharaComboBoxAutomationId));
                        this.AddButtonElement =
                            FindDescendant(
                                tlElem,
                                new PropertyCondition(
                                    AutomationElement.NameProperty,
                                    TimelineSpeechAddButtonName));
                    });

                // 1つでもUIが見つからなければ不可
                if (
                    this.SpeechEditElement == null ||
                    this.CharaComboElement == null ||
                    this.AddButtonElement == null)
                {
                    this.TimelineWindow = null;
                    return false;
                }

                this.TimelineWindow = tlWin;
            }

            return true;
        }

        /// <summary>
        /// タイムラインウィンドウのセリフエディットにテキストを設定する。
        /// </summary>
        /// <param name="text">設定するテキスト。</param>
        /// <returns>成功したならば true 。そうでなければ false 。</returns>
        public bool SetTimelineSpeechEditValue(string text)
        {
            if (!this.IsTimelineOpened || this.SpeechEditElement == null)
            {
                return false;
            }

            // ValuePattern 取得
            var edit =
                GetPattern<ValuePattern>(this.SpeechEditElement, ValuePattern.Pattern);
            if (edit == null || edit.Current.IsReadOnly)
            {
                return false;
            }

            // テキスト設定
            try
            {
                edit.SetValue(text);
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// タイムラインウィンドウのキャラ選択コンボボックスからキャラを選択する。
        /// </summary>
        /// <param name="name">選択するキャラ名。</param>
        /// <returns>
        /// 成功したならば true 。
        /// キャラ名が存在しないならば null 。
        /// どちらでもなければ false 。
        /// </returns>
        public async Task<bool?> SelectTimelineCharaComboBoxItem(string name)
        {
            if (!this.IsTimelineOpened || this.CharaComboElement == null)
            {
                return false;
            }

            // すべてのアイテムを有効化させるためにコンボボックスを開く
            var expand =
                GetPattern<ExpandCollapsePattern>(
                    this.CharaComboElement,
                    ExpandCollapsePattern.Pattern);
            if (expand == null)
            {
                return false;
            }
            expand.Expand();

            // Name がキャラ名の子を持つコンボボックスアイテムUIを探す
            var itemCond =
                new PropertyCondition(
                    AutomationElement.ControlTypeProperty,
                    ControlType.ListItem);
            var nameCond = new PropertyCondition(AutomationElement.NameProperty, name);
            var itemElem =
                await this.CharaComboElement
                    .FindAll(TreeScope.Children, itemCond)
                    .OfType<AutomationElement>()
                    .ToObservable()
                    .FirstOrDefaultAsync(i => FindDescendant(i, nameCond) != null);
            if (itemElem == null)
            {
                // キャラ名存在せず
                return null;
            }

            // SelectionItemPattern 取得
            var item =
                GetPattern<SelectionItemPattern>(itemElem, SelectionItemPattern.Pattern);
            if (item == null)
            {
                return false;
            }

            // アイテム選択
            try
            {
                item.Select();
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// タイムラインウィンドウの追加ボタンを押下する。
        /// </summary>
        /// <returns>成功したならば true 。そうでなければ false 。</returns>
        public bool ClickTimelineSpeechAddButton()
        {
            if (!this.IsTimelineOpened || this.AddButtonElement == null)
            {
                return false;
            }

            // InvokePattern 取得
            var button =
                GetPattern<InvokePattern>(this.AddButtonElement, InvokePattern.Pattern);
            if (button == null)
            {
                return false;
            }

            // 押下
            try
            {
                button.Invoke();
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// UI操作のタイムアウトミリ秒数。
        /// </summary>
        private const int UIControlTimeout = 1000;

        /// <summary>
        /// 『ゆっくりMovieMaker』プロセス名。
        /// </summary>
        private const string ProcessName = @"YukkuriMovieMaker_v3";

        /// <summary>
        /// タイムラインウィンドウのタイトルプレフィクス文字列。
        /// </summary>
        private const string TimelineWindowTitlePrefix = @"タイムライン";

        /// <summary>
        /// タイムラインウィンドウ内セリフエディットのオートメーションID。
        /// </summary>
        private const string TimelineSpeechEditAutomationId = @"SerifuTB";

        /// <summary>
        /// タイムラインウィンドウ内キャラ選択コンボボックスのオートメーションID。
        /// </summary>
        private const string TimelineCharaComboBoxAutomationId = @"CharactersCB";

        /// <summary>
        /// タイムラインウィンドウ内追加ボタンの名前。
        /// </summary>
        private const string TimelineSpeechAddButtonName = @"追加";

        /// <summary>
        /// 子孫の AutomationElement を検索する。
        /// </summary>
        /// <param name="root">検索のルート。</param>
        /// <param name="condition">条件。</param>
        /// <returns>AutomationElement 。見つからなければ null 。</returns>
        private static AutomationElement FindDescendant(
            AutomationElement root,
            PropertyCondition condition)
        {
            return root?.FindFirst(TreeScope.Element | TreeScope.Descendants, condition);
        }

        /// <summary>
        /// UI操作パターンを取得する。
        /// </summary>
        /// <typeparam name="T">パターンの型。</typeparam>
        /// <param name="element">取得元。</param>
        /// <param name="pattern">取得パターン。</param>
        /// <returns>UI操作パターン。取得できなければ null 。</returns>
        private static T GetPattern<T>(
            AutomationElement element,
            AutomationPattern pattern)
            where T : BasePattern
        {
            object p = null;
            return
                (element?.TryGetCurrentPattern(pattern, out p) == true) ?
                    (p as T) : null;
        }

        /// <summary>
        /// メインウィンドウを取得または設定する。
        /// </summary>
        private Win32Window MainWindow { get; set; } = null;

        /// <summary>
        /// タイムラインウィンドウを取得または設定する。
        /// </summary>
        private Win32Window TimelineWindow { get; set; } = null;

        /// <summary>
        /// セリフエディットの AutomationElement を取得または設定する。
        /// </summary>
        /// <remarks>タイムラインウィンドウが開いていない場合は無効。</remarks>
        private AutomationElement SpeechEditElement { get; set; } = null;

        /// <summary>
        /// キャラ選択コンボボックスの AutomationElement を取得または設定する。
        /// </summary>
        /// <remarks>タイムラインウィンドウが開いていない場合は無効。</remarks>
        private AutomationElement CharaComboElement { get; set; } = null;

        /// <summary>
        /// 追加ボタンの AutomationElement を取得または設定する。
        /// </summary>
        /// <remarks>タイムラインウィンドウが開いていない場合は無効。</remarks>
        private AutomationElement AddButtonElement { get; set; } = null;

        /// <summary>
        /// タイムラインウィンドウを検索する。
        /// </summary>
        /// <returns>タイムラインウィンドウ。見つからなければ null 。</returns>
        private async Task<Win32Window> FindTimelineWindow()
        {
            return
                await Win32Window.Desktop
                    .FindChildren()
                    .ToObservable()
                    .SelectMany(async w => (await this.IsTimelineWindow(w)) ? w : null)
                    .FirstOrDefaultAsync(w => w != null);
        }

        /// <summary>
        /// タイムラインウィンドウであるか否かを取得する。
        /// </summary>
        /// <param name="target">調べるウィンドウ。</param>
        /// <returns>タイムラインウィンドウならば true 。そうでなければ false 。</returns>
        private async Task<bool> IsTimelineWindow(Win32Window target)
        {
            var mainWin = this.MainWindow;
            var textTask = target.GetTextAsync(UIControlTimeout);

            return (
                mainWin != null &&
                target != null &&
                target.GetOwner()?.Handle == mainWin.Handle &&
                (await textTask)?.StartsWith(TimelineWindowTitlePrefix) == true);
        }
    }
}
