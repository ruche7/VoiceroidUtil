using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using draw = System.Drawing;

namespace VoiceroidUtil
{
    /// <summary>
    /// メインウィンドウ設定クラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public class MainWindowConfig : IExtensibleDataObject
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public MainWindowConfig()
        {
        }

        /// <summary>
        /// 左端位置を取得または設定する。
        /// </summary>
        [DataMember]
        public double? Left { get; set; } = null;

        /// <summary>
        /// 上端位置を取得または設定する。
        /// </summary>
        [DataMember]
        public double? Top { get; set; } = null;

        /// <summary>
        /// 横幅を取得または設定する。
        /// </summary>
        [DataMember]
        public double? Width { get; set; } = null;

        /// <summary>
        /// 縦幅を取得または設定する。
        /// </summary>
        [DataMember]
        public double? Height { get; set; } = null;

        /// <summary>
        /// 最大化するか否かを取得または設定する。
        /// </summary>
        [DataMember]
        public bool? IsMaximized { get; set; } = null;

        /// <summary>
        /// ウィンドウから値をコピーする。
        /// </summary>
        /// <param name="window">コピー元のウィンドウ。</param>
        public void CopyFrom(Window window)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            this.Left = window.Left;
            this.Top = window.Top;
            this.Width = window.Width;
            this.Height = window.Height;
            this.IsMaximized = (window.WindowState == WindowState.Maximized);
        }

        /// <summary>
        /// ウィンドウに座標値を適用する。
        /// </summary>
        /// <param name="window">適用先のウィンドウ。</param>
        public void ApplyLocationTo(Window window)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            var oldLeft = window.Left;
            var oldTop = window.Top;

            if (this.Left.HasValue)
            {
                window.Left = this.Left.Value;
            }
            if (this.Top.HasValue)
            {
                window.Top = this.Top.Value;
            }
            if (this.Width.HasValue)
            {
                window.Width = this.Width.Value;
            }
            if (this.Height.HasValue)
            {
                window.Height = this.Height.Value;
            }

            // ウィンドウがスクリーン内にいないなら位置を戻す
            if (!IsWindowOnScreen(window))
            {
                window.Left = oldLeft;
                window.Top = oldTop;
            }
        }

        /// <summary>
        /// ウィンドウに最大化状態値を適用する。
        /// </summary>
        /// <param name="window">適用先のウィンドウ。</param>
        public void ApplyMaximizedTo(Window window)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }

            if (this.IsMaximized == true)
            {
                window.WindowState = WindowState.Maximized;
            }
        }

        #region ウィンドウ位置チェック

        /// <summary>
        /// スクリーン内に収まっていると判断する幅。
        /// </summary>
        private const int WindowOnScreenWidth = 48;

        /// <summary>
        /// スクリーン内に収まっていると判断する高さ。
        /// </summary>
        private const int WindowOnScreenHeight = 48;

        /// <summary>
        /// ウィンドウがいずれかのスクリーン内に収まっているか否かを取得する。
        /// </summary>
        /// <param name="window">ウィンドウ。</param>
        /// <returns>収まっているならば true 。そうでなければ false 。</returns>
        private static bool IsWindowOnScreen(Window window)
        {
            var winRect = GetWindowRect(window);
            return
                System.Windows.Forms.Screen.AllScreens.Any(
                    screen =>
                    {
                        var r = draw.Rectangle.Intersect(screen.WorkingArea, winRect);
                        return (
                            r.Top <= winRect.Top &&
                            r.Width >= WindowOnScreenWidth &&
                            r.Height >= WindowOnScreenHeight);
                    });
        }

        /// <summary>
        /// ウィンドウの位置とサイズをデバイス依存ピクセル単位で取得する。
        /// </summary>
        /// <param name="window">ウィンドウ。</param>
        /// <returns>デバイス依存ピクセル単位での位置とサイズ。</returns>
        private static draw.Rectangle GetWindowRect(Window window)
        {
            // 左上端と右下端のDIP座標作成
            var lt = new Point(window.Left, window.Top);
            var rb = new Point(lt.X + window.Width, lt.Y + window.Height);

            // デバイス依存ピクセル座標に変換
            // 変換できなければそのままの値を使う
            var src = PresentationSource.FromVisual(window);
            var target = src?.CompositionTarget;
            if (target != null)
            {
                lt = target.TransformToDevice.Transform(lt);
                rb = target.TransformToDevice.Transform(rb);
            }

            return
                draw.Rectangle.FromLTRB(
                    (int)(lt.X + 0.5),
                    (int)(lt.Y + 0.5),
                    (int)(rb.X + 0.5),
                    (int)(rb.Y + 0.5));
        }

        #endregion

        #region IExtensibleDataObject の明示的実装

        ExtensionDataObject IExtensibleDataObject.ExtensionData { get; set; }

        #endregion
    }
}
