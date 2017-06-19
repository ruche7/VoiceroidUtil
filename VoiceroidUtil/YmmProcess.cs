using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Automation;
using RucheHome.Util;

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
        public bool IsRunning => (this.MainWindowHandle != IntPtr.Zero);

        /// <summary>
        /// タイムラインウィンドウが存在するか否かを取得する。
        /// </summary>
        public bool IsTimelineExists =>
            (this.SpeechEditElement != null) &&
            (this.CharaComboElement != null) &&
            (this.AddButtonElement != null);

        /// <summary>
        /// 『ゆっくりMovieMaker』プロセス名。
        /// </summary>
        private const string ProcessName = @"YukkuriMovieMaker_v3";

        /// <summary>
        /// 状態を更新する。
        /// </summary>
        /// <returns>
        /// メインウィンドウが開いているならば true 。そうでなければ false 。
        /// </returns>
        public async Task<bool> Update()
        {
            // プロセス検索
            this.Process = Process.GetProcessesByName(ProcessName).FirstOrDefault();
            if (this.Process == null)
            {
                this.Reset();
                return false;
            }

            // 入力待機状態待ち
            if (!(await this.WhenForInputHandle()))
            {
                ThreadDebug.WriteLine(@"YMM : WaitForInputIdle() == false");
                this.Reset();
                return false;
            }

            // メインウィンドウハンドル確認
            this.Process.Refresh();
            if (this.MainWindowHandle == IntPtr.Zero)
            {
                ThreadDebug.WriteLine(@"YMM : process.MainWindowHandle == IntPtr.Zero");
                this.Reset();
                return false;
            }

            // AutomationElement 群更新
            if (!this.UpdateElements())
            {
                this.ResetElements();
            }

            return true;
        }

        /// <summary>
        /// 内部状態をリセットする。
        /// </summary>
        public void Reset()
        {
            this.Process = null;
            this.ResetElements();
        }

        /// <summary>
        /// タイムラインウィンドウのセリフエディットにテキストを設定する。
        /// </summary>
        /// <param name="text">設定するテキスト。</param>
        /// <returns>成功したならば true 。そうでなければ false 。</returns>
        public async Task<bool> SetTimelineSpeechEditValue(string text)
        {
            if (!this.IsTimelineExists)
            {
                return false;
            }

            // ValuePattern 取得
            var edit =
                GetPattern<ValuePattern>(this.SpeechEditElement, ValuePattern.Pattern);
            if (edit == null || edit.Current.IsReadOnly)
            {
                ThreadDebug.WriteLine(
                    @"YMM : SpeechEditElement から ValuePattern を取得できない。");
                return false;
            }

            // テキスト設定
            try
            {
                await this.WhenForInputHandle();
                edit.SetValue(text);
            }
            catch (Exception ex)
            {
                ThreadDebug.WriteException(ex);
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
            if (!this.IsTimelineExists)
            {
                return false;
            }

            // すべてのアイテムを有効化させるためにコンボボックスを開閉する
            var expand =
                GetPattern<ExpandCollapsePattern>(
                    this.CharaComboElement,
                    ExpandCollapsePattern.Pattern);
            if (expand == null)
            {
                ThreadDebug.WriteLine(
                    @"YMM : CharaComboElement から ExpandCollapsePattern を取得できない。");
                return false;
            }
            try
            {
                await this.WhenForInputHandle();
                expand.Expand();
                expand.Collapse();
            }
            catch (Exception ex)
            {
                ThreadDebug.WriteException(ex);
                return false;
            }

            // Name がキャラ名の子を持つコンボボックスアイテムUIを探す
            var nameCond = new PropertyCondition(AutomationElement.NameProperty, name);
            var itemElem =
                await FindAllChildren(
                    this.CharaComboElement,
                    AutomationElement.ControlTypeProperty,
                    ControlType.ListItem)
                    .ToObservable()
                    .FirstOrDefaultAsync(i => FindFirstChild(i, nameCond) != null);
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
                ThreadDebug.WriteLine(
                    @"YMM : CharaComboElement から SelectionItemPattern を取得できない。");
                return false;
            }

            // アイテム選択
            try
            {
                await this.WhenForInputHandle();
                item.Select();
            }
            catch (Exception ex)
            {
                ThreadDebug.WriteException(ex);
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
            if (!this.IsTimelineExists)
            {
                return false;
            }

            // InvokePattern 取得
            var button =
                GetPattern<InvokePattern>(this.AddButtonElement, InvokePattern.Pattern);
            if (button == null)
            {
                ThreadDebug.WriteLine(
                    @"YMM : AddButtonElement から InvokePattern を取得できない。");
                return false;
            }

            // 押下
            try
            {
                await this.WhenForInputHandle();
                button.Invoke();
            }
            catch (Exception ex)
            {
                ThreadDebug.WriteException(ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 特定の条件を満たすすべての子 AutomationElement を検索する。
        /// </summary>
        /// <param name="element">検索元 AutomationElement 。</param>
        /// <param name="condition">検索条件。 null ならば常に満たす。</param>
        /// <returns>子 AutomationElement 列挙。</returns>
        private static IEnumerable<AutomationElement> FindAllChildren(
            AutomationElement element,
            Condition condition = null)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return
                element
                    .FindAll(TreeScope.Children, condition ?? Condition.TrueCondition)
                    .OfType<AutomationElement>();
        }

        /// <summary>
        /// 特定のプロパティ値を持つすべての子 AutomationElement を検索する。
        /// </summary>
        /// <param name="element">検索元 AutomationElement 。</param>
        /// <param name="property">プロパティ種別。</param>
        /// <param name="propertyValue">プロパティ値。</param>
        /// <returns>子 AutomationElement 列挙。</returns>
        private static IEnumerable<AutomationElement> FindAllChildren(
            AutomationElement element,
            AutomationProperty property,
            object propertyValue)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            return FindAllChildren(element, new PropertyCondition(property, propertyValue));
        }

        /// <summary>
        /// 特定の条件を満たす最初の子 AutomationElement を検索する。
        /// </summary>
        /// <param name="element">検索元 AutomationElement 。</param>
        /// <param name="condition">検索条件。 null ならば常に満たす。</param>
        /// <returns>子 AutomationElement 。見つからなければ null 。</returns>
        private static AutomationElement FindFirstChild(
            AutomationElement element,
            Condition condition = null)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return
                element.FindFirst(TreeScope.Children, condition ?? Condition.TrueCondition);
        }

        /// <summary>
        /// 特定のプロパティ値を持つ最初の子 AutomationElement を検索する。
        /// </summary>
        /// <param name="element">検索元 AutomationElement 。</param>
        /// <param name="property">プロパティ種別。</param>
        /// <param name="propertyValue">プロパティ値。</param>
        /// <returns>子 AutomationElement 。見つからなければ null 。</returns>
        private static AutomationElement FindFirstChild(
            AutomationElement element,
            AutomationProperty property,
            object propertyValue)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            return FindFirstChild(element, new PropertyCondition(property, propertyValue));
        }

        /// <summary>
        /// UI操作パターンを取得する。
        /// </summary>
        /// <typeparam name="T">パターンの型。</typeparam>
        /// <param name="element">取得元 AutomationElement 。</param>
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
        /// 『ゆっくりMovieMaker』プロセスを取得または設定する。
        /// </summary>
        private Process Process { get; set; } = null;

        /// <summary>
        /// メインウィンドウハンドルを取得する。
        /// </summary>
        private IntPtr MainWindowHandle =>
            (this.Process == null) ? IntPtr.Zero : this.Process.MainWindowHandle;

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
        /// 『ゆっくりMovieMaker』プロセスが入力待機状態になるまで非同期で待機する。
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
        /// AutomationElement 群を更新する。
        /// </summary>
        /// <returns>成功したならば true 。そうでなければ false 。</returns>
        private bool UpdateElements()
        {
            if (this.MainWindowHandle == IntPtr.Zero)
            {
                return false;
            }

            AutomationElement root = null;
            try
            {
                root = AutomationElement.FromHandle(this.MainWindowHandle);
            }
            catch
            {
                return false;
            }

            // タイムラインウィンドウ検索
            var tlWin =
                FindAllChildren(
                    root,
                    AutomationElement.ControlTypeProperty,
                    ControlType.Window)
                    .FirstOrDefault(e => e.Current.Name.StartsWith(@"タイムライン"));
            if (tlWin == null)
            {
                return false;
            }

            // タイムラインコントロール検索
            var tlCtrl =
                FindFirstChild(
                    tlWin,
                    AutomationElement.ClassNameProperty,
                    @"TimelineControl");
            if (tlCtrl == null)
            {
                return false;
            }

            // セリフエディット検索
            var speechEdit =
                FindFirstChild(tlCtrl, AutomationElement.AutomationIdProperty, @"SerifuTB");
            if (speechEdit == null)
            {
                return false;
            }

            // キャラ選択コンボボックス検索
            var charaCombo =
                FindFirstChild(
                    tlCtrl,
                    AutomationElement.AutomationIdProperty,
                    @"CharactersCB");
            if (charaCombo == null)
            {
                return false;
            }

            // 追加ボタン検索
            var addButton = FindFirstChild(tlCtrl, AutomationElement.NameProperty, @"追加");
            if (addButton == null)
            {
                return false;
            }

            // 更新
            this.SpeechEditElement = speechEdit;
            this.CharaComboElement = charaCombo;
            this.AddButtonElement = addButton;

            return true;
        }

        /// <summary>
        /// AutomationElement 群をリセットする。
        /// </summary>
        private void ResetElements()
        {
            this.SpeechEditElement = null;
            this.CharaComboElement = null;
            this.AddButtonElement = null;
        }
    }
}
