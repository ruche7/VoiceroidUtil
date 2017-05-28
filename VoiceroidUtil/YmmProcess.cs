using System;
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
            if (!(await this.WaitForInputHandle()))
            {
                ThreadDebug.WriteLine(@"YMM : WaitForInputIdle() == false");
                this.Reset();
                return false;
            }

            // メインウィンドウハンドル取得
            var handle = this.Process.MainWindowHandle;
            if (handle == IntPtr.Zero)
            {
                ThreadDebug.WriteLine(@"YMM : process.MainWindowHandle == IntPtr.Zero");
                this.Reset();
                return false;
            }

            // メインウィンドウハンドルが変わった？
            if (handle != this.MainWindowHandle)
            {
                // メインウィンドウハンドル更新
                this.MainWindowHandle = handle;

                // AutomationElement キャッシュをリセット
                this.ResetElementCache();
            }

            return true;
        }

        /// <summary>
        /// 内部状態をリセットする。
        /// </summary>
        public void Reset()
        {
            this.Process = null;
            this.MainWindowHandle = IntPtr.Zero;
            this.ResetElementCache();
        }

        /// <summary>
        /// タイムラインウィンドウのセリフエディットにテキストを設定する。
        /// </summary>
        /// <param name="text">設定するテキスト。</param>
        /// <returns>成功したならば true 。そうでなければ false 。</returns>
        public async Task<bool> SetTimelineSpeechEditValue(string text)
        {
            var elem = await this.GetSpeechEditElement();
            if (elem == null)
            {
                ThreadDebug.WriteLine(@"YMM : GetSpeechEditElement() == null");
                return false;
            }

            // ValuePattern 取得
            var edit = GetPattern<ValuePattern>(elem, ValuePattern.Pattern);
            if (edit == null || edit.Current.IsReadOnly)
            {
                ThreadDebug.WriteLine(
                    @"YMM : SpeechEditElement から ValuePattern を取得できない。");
                return false;
            }

            // テキスト設定
            try
            {
                await this.WaitForInputHandle();
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
            var elem = await this.GetCharaComboElement();
            if (elem == null)
            {
                ThreadDebug.WriteLine(@"YMM : GetCharaComboElement() == null");
                return false;
            }

            // すべてのアイテムを有効化させるためにコンボボックスを開く
            var expand =
                GetPattern<ExpandCollapsePattern>(elem, ExpandCollapsePattern.Pattern);
            if (expand == null)
            {
                ThreadDebug.WriteLine(
                    @"YMM : CharaComboElement から ExpandCollapsePattern を取得できない。");
                return false;
            }
            await this.WaitForInputHandle();
            expand.Expand();

            // Name がキャラ名の子を持つコンボボックスアイテムUIを探す
            var itemCond =
                new PropertyCondition(
                    AutomationElement.ControlTypeProperty,
                    ControlType.ListItem);
            var nameCond = new PropertyCondition(AutomationElement.NameProperty, name);
            var itemElem =
                await elem
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
                ThreadDebug.WriteLine(
                    @"YMM : CharaComboElement から SelectionItemPattern を取得できない。");
                return false;
            }

            // アイテム選択
            try
            {
                await this.WaitForInputHandle();
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
            var elem = await this.GetAddButtonElement();
            if (elem == null)
            {
                ThreadDebug.WriteLine(@"YMM : GetAddButtonElement() == null");
                return false;
            }

            // InvokePattern 取得
            var button = GetPattern<InvokePattern>(elem, InvokePattern.Pattern);
            if (button == null)
            {
                ThreadDebug.WriteLine(
                    @"YMM : AddButtonElement から InvokePattern を取得できない。");
                return false;
            }

            // 押下
            try
            {
                await this.WaitForInputHandle();
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
        /// 『ゆっくりMovieMaker』プロセス名。
        /// </summary>
        private const string ProcessName = @"YukkuriMovieMaker_v3";

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
            return root?.FindFirst(TreeScope.Descendants, condition);
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
        /// 『ゆっくりMovieMaker』プロセスを取得または設定する。
        /// </summary>
        private Process Process { get; set; } = null;

        /// <summary>
        /// メインウィンドウハンドルを取得または設定する。
        /// </summary>
        private IntPtr MainWindowHandle { get; set; } = IntPtr.Zero;

        /// <summary>
        /// セリフエディットの AutomationElement キャッシュを取得または設定する。
        /// </summary>
        /// <remarks>タイムラインウィンドウが開いていない場合は無効。</remarks>
        private AutomationElement SpeechEditElementCache { get; set; } = null;

        /// <summary>
        /// キャラ選択コンボボックスの AutomationElement キャッシュを取得または設定する。
        /// </summary>
        /// <remarks>タイムラインウィンドウが開いていない場合は無効。</remarks>
        private AutomationElement CharaComboElementCache { get; set; } = null;

        /// <summary>
        /// 追加ボタンの AutomationElement キャッシュを取得または設定する。
        /// </summary>
        /// <remarks>タイムラインウィンドウが開いていない場合は無効。</remarks>
        private AutomationElement AddButtonElementCache { get; set; } = null;

        /// <summary>
        /// WaitForInputHandle メソッドの最大ループ回数。
        /// </summary>
        private const int WaitForInputHandleLoopCount = 10;

        /// <summary>
        /// WaitForInputHandle メソッドのループ間隔ミリ秒数。
        /// </summary>
        private const int WaitForInputHandleLoopIntervalMilliseconds = 50;

        /// <summary>
        /// 『ゆっくりMovieMaker』プロセスが入力可能状態になるまで待機する。
        /// </summary>
        /// <returns>入力可能状態になったならば true 。そうでなければ false 。</returns>
        private async Task<bool> WaitForInputHandle()
        {
            bool? result = this.Process?.WaitForInputIdle(0);

            for (int i = 0; result == false && i < WaitForInputHandleLoopCount; ++i)
            {
                await Task.Delay(WaitForInputHandleLoopIntervalMilliseconds);
                result = this.Process?.WaitForInputIdle(0);
            }

            return (result == true);
        }

        /// <summary>
        /// タイムラインウィンドウ以下の AutomationElement を検索する。
        /// </summary>
        /// <param name="condition">検索条件。</param>
        /// <returns>AutomationElement 。利用できない場合は null 。</returns>
        private async Task<AutomationElement> FindUIElement(
            PropertyCondition condition)
        {
            if (!(await this.WaitForInputHandle()))
            {
                return null;
            }

            var handle = this.MainWindowHandle;
            if (handle == IntPtr.Zero)
            {
                return null;
            }

            // AutomationElement 作成
            // YMM上にアイテムが多いと時間が掛かるので非同期で
            return
                await Task.Run(
                    () => FindDescendant(AutomationElement.FromHandle(handle), condition));
        }

        /// <summary>
        /// セリフエディットの AutomationElement を取得する。
        /// </summary>
        /// <returns>AutomationElement 。利用できない場合は null 。</returns>
        private async Task<AutomationElement> GetSpeechEditElement()
        {
            if (this.SpeechEditElementCache == null)
            {
                this.SpeechEditElementCache =
                    await this.FindUIElement(
                        new PropertyCondition(
                            AutomationElement.AutomationIdProperty,
                            TimelineSpeechEditAutomationId));
            }

            return this.SpeechEditElementCache;
        }

        /// <summary>
        /// キャラ選択コンボボックスの AutomationElement を取得する。
        /// </summary>
        /// <returns>AutomationElement 。利用できない場合は null 。</returns>
        private async Task<AutomationElement> GetCharaComboElement()
        {
            if (this.CharaComboElementCache == null)
            {
                this.CharaComboElementCache =
                    await this.FindUIElement(
                        new PropertyCondition(
                            AutomationElement.AutomationIdProperty,
                            TimelineCharaComboBoxAutomationId));
            }

            return this.CharaComboElementCache;
        }

        /// <summary>
        /// 追加ボタンの AutomationElement を取得する。
        /// </summary>
        /// <returns>AutomationElement 。利用できない場合は null 。</returns>
        private async Task<AutomationElement> GetAddButtonElement()
        {
            if (this.AddButtonElementCache == null)
            {
                this.AddButtonElementCache =
                    await this.FindUIElement(
                        new PropertyCondition(
                            AutomationElement.NameProperty,
                            TimelineSpeechAddButtonName));
            }

            return this.AddButtonElementCache;
        }

        /// <summary>
        /// AutomationElement キャッシュをリセットする。
        /// </summary>
        private void ResetElementCache()
        {
            this.SpeechEditElementCache = null;
            this.CharaComboElementCache = null;
            this.AddButtonElementCache = null;
        }
    }
}
