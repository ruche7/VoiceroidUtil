using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using RucheHome.Windows.WinApi;

namespace VoiceroidUtil
{
    /// <summary>
    /// 『ゆっくりMovieMaker3』プロセスを操作するクラス。
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
        /// 状態を更新する。
        /// </summary>
        public void Update()
        {
            var process = Process.GetProcessesByName(ProcessName).FirstOrDefault();
            if (process == null)
            {
                this.MainWindow = null;
                return;
            }

            this.MainWindow = new Win32Window(process.MainWindowHandle);
        }

        /// <summary>
        /// タイムラインウィンドウのセリフエディットにテキストを設定する。
        /// </summary>
        /// <param name="text">設定するテキスト。</param>
        /// <returns>成功したならば true 。そうでなければ false 。</returns>
        public async Task<bool> SetTimelineSpeechEditValue(string text)
        {
            // セリフエディットUIを探す
            var editElem =
                FindDescendant(
                    await this.DoMakeTimelineWindowElement(),
                    new PropertyCondition(
                        AutomationElement.AutomationIdProperty,
                        TimelineSpeechEditAutomationId));
            if (editElem == null)
            {
                return false;
            }

            // ValuePattern 取得
            var edit = GetPattern<ValuePattern>(editElem, ValuePattern.Pattern);
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
        /// <returns>成功したならば true 。そうでなければ false 。</returns>
        public async Task<bool> SelectTimelineCharaComboBoxItem(string name)
        {
            // キャラ選択コンボボックスUIを探す
            var comboElem =
                FindDescendant(
                    await this.DoMakeTimelineWindowElement(),
                    new PropertyCondition(
                        AutomationElement.AutomationIdProperty,
                        TimelineCharaComboBoxAutomationId));
            if (comboElem == null)
            {
                return false;
            }

            // SelectionPattern 取得
            var combo = GetPattern<SelectionPattern>(comboElem, SelectionPattern.Pattern);
            if (combo == null)
            {
                return false;
            }

            // Name がキャラ名の子を持つコンボボックスアイテムUIを探す
            var nameCond = new PropertyCondition(AutomationElement.NameProperty, name);
            var itemElem =
                await combo.Current.GetSelection()
                    .ToObservable()
                    .FirstOrDefaultAsync(e => FindDescendant(e, nameCond) != null);
            if (itemElem == null)
            {
                return false;
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
        public async Task<bool> ClickTimelineSpeechAddButton()
        {
            // 追加ボタンUIを探す
            var buttonElem =
                FindDescendant(
                    await this.DoMakeTimelineWindowElement(),
                    new PropertyCondition(
                        AutomationElement.NameProperty,
                        TimelineSpeechAddButtonName));
            if (buttonElem == null)
            {
                return false;
            }

            // InvokePattern 取得
            var button = GetPattern<InvokePattern>(buttonElem, InvokePattern.Pattern);
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
        private const int UIControlTimeout = 500;

        /// <summary>
        /// 『ゆっくりMovieMaker3』プロセス名。
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
        /// タイムラインウィンドウの AutomationElement を作成する。
        /// </summary>
        /// <returns>作成タスク。</returns>
        private async Task<AutomationElement> DoMakeTimelineWindowElement()
        {
            var mainWin = this.MainWindow;
            if (mainWin == null)
            {
                return null;
            }

            if (mainWin.State == WindowState.Minimized)
            {
                mainWin.State = WindowState.Normal;
            }

            var tlWin = await this.FindTimelineWindow();
            if (tlWin == null)
            {
                return null;
            }

            return AutomationElement.FromHandle(tlWin.Handle);
        }

        /// <summary>
        /// タイムラインウィンドウを検索する。
        /// </summary>
        /// <returns>検索タスク。</returns>
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
        /// <returns>検査タスク。</returns>
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
