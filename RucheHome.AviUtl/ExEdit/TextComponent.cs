using System;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Media;
using RucheHome.Text;
using RucheHome.Util.Extensions.String;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// テキストコンポーネントを表すクラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public class TextComponent : ComponentBase
    {
        /// <summary>
        /// 規定のフォントファミリ名。
        /// </summary>
        public static readonly string DefaultFontFamilyName = @"MS UI Gothic";

        /// <summary>
        /// テキストの最大許容文字数。
        /// </summary>
        public static readonly int TextLengthLimit = 1024 - 1;

        /// <summary>
        /// 拡張編集オブジェクトファイルのセクションデータからコンポーネントを作成する。
        /// </summary>
        /// <param name="section">セクションデータ。</param>
        /// <returns>コンポーネント。作成できないならば null 。</returns>
        public static TextComponent FromExoFileSection(IniFileSection section)
        {
            return FromExoFileSectionCore(section, () => new TextComponent());
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public TextComponent() : base()
        {
        }

        /// <summary>
        /// コンポーネント名を取得する。
        /// </summary>
        public override string ComponentName => @"テキスト";

        /// <summary>
        /// フォントサイズを取得または設定する。
        /// </summary>
        [ComponentItem(@"サイズ", Order = 1)]
        [DataMember]
        public MovableValue<FontSizeConst> FontSize
        {
            get { return this.fontSize; }
            set
            {
                this.SetProperty(
                    ref this.fontSize,
                    value ?? new MovableValue<FontSizeConst>());
            }
        }
        private MovableValue<FontSizeConst> fontSize = new MovableValue<FontSizeConst>();

        /// <summary>
        /// 表示速度を取得または設定する。
        /// </summary>
        [ComponentItem(@"表示速度", Order = 2)]
        [DataMember]
        public MovableValue<TextSpeedConst> TextSpeed
        {
            get { return this.textSpeed; }
            set
            {
                this.SetProperty(
                    ref this.textSpeed,
                    value ?? new MovableValue<TextSpeedConst>());
            }
        }
        private MovableValue<TextSpeedConst> textSpeed =
            new MovableValue<TextSpeedConst>();

        /// <summary>
        /// 自動スクロールするか否かを取得または設定する。
        /// </summary>
        [ComponentItem(@"自動スクロール", Order = 5)]
        [DataMember]
        public bool IsAutoScrolling
        {
            get { return this.autoScrolling; }
            set { this.SetProperty(ref this.autoScrolling, value); }
        }
        private bool autoScrolling = false;

        /// <summary>
        /// 文字毎に個別オブジェクトとするか否かを取得または設定する。
        /// </summary>
        [ComponentItem(@"文字毎に個別オブジェクト", Order = 3)]
        [DataMember]
        public bool IsIndividualizing
        {
            get { return this.individualizing; }
            set { this.SetProperty(ref this.individualizing, value); }
        }
        private bool individualizing = false;

        /// <summary>
        /// 各文字を移動座標上に表示するか否かを取得する。
        /// </summary>
        [ComponentItem(@"移動座標上に表示する", Order = 4)]
        [DataMember]
        public bool IsAligningOnMotion
        {
            get { return this.aligningOnMotion; }
            set { this.SetProperty(ref this.aligningOnMotion, value); }
        }
        private bool aligningOnMotion = false;

        /// <summary>
        /// 高さを自動調整するか否かを取得する。
        /// </summary>
        [ComponentItem(@"autoadjust", Order = 9)]
        [DataMember]
        public bool IsAutoAdjusting
        {
            get { return this.autoAdjusting; }
            set { this.SetProperty(ref this.autoAdjusting, value); }
        }
        private bool autoAdjusting = false;

        /// <summary>
        /// フォント色を取得または設定する。
        /// </summary>
        [ComponentItem(@"color", Order = 16)]
        [DataMember]
        public Color FontColor
        {
            get { return this.fontColor; }
            set
            {
                this.SetProperty(
                    ref this.fontColor,
                    Color.FromRgb(value.R, value.G, value.B));
            }
        }
        private Color fontColor = Colors.White;

        /// <summary>
        /// フォント装飾色を取得または設定する。
        /// </summary>
        [ComponentItem(@"color2", Order = 17)]
        [DataMember]
        public Color FontDecorationColor
        {
            get { return this.fontDecorationColor; }
            set
            {
                this.SetProperty(
                    ref this.fontDecorationColor,
                    Color.FromRgb(value.R, value.G, value.B));
            }
        }
        private Color fontDecorationColor = Colors.Black;

        /// <summary>
        /// フォントファミリ名を取得または設定する。
        /// </summary>
        [ComponentItem(@"font", Order = 18)]
        [DataMember]
        public string FontFamilyName
        {
            get { return this.fontFamilyName; }
            set
            {
                this.SetProperty(ref this.fontFamilyName, value ?? DefaultFontFamilyName);
            }
        }
        private string fontFamilyName = DefaultFontFamilyName;

        /// <summary>
        /// フォント装飾種別を取得または設定する。
        /// </summary>
        [ComponentItem(@"type", Order = 8)]
        public FontDecoration FontDecoration
        {
            get { return this.fontDecoration; }
            set
            {
                this.SetProperty(
                    ref this.fontDecoration,
                    Enum.IsDefined(value.GetType(), value) ? value : FontDecoration.None);
            }
        }
        private FontDecoration fontDecoration = FontDecoration.None;

        /// <summary>
        /// FontDecoration プロパティのシリアライズ用ラッパプロパティ。
        /// </summary>
        [DataMember(Name = nameof(FontDecoration))]
        private string FontDecorationString
        {
            get { return this.FontDecoration.ToString(); }
            set
            {
                FontDecoration deco;
                this.FontDecoration =
                    Enum.TryParse(value, out deco) ? deco : FontDecoration.None;
            }
        }

        /// <summary>
        /// テキスト配置種別を取得または設定する。
        /// </summary>
        [ComponentItem(@"align", Order = 12)]
        public TextAlignment TextAlignment
        {
            get { return this.textAlignment; }
            set
            {
                this.SetProperty(
                    ref this.textAlignment,
                    Enum.IsDefined(value.GetType(), value) ?
                        value : TextAlignment.TopLeft);
            }
        }
        private TextAlignment textAlignment = TextAlignment.TopLeft;

        /// <summary>
        /// TextAlignment プロパティのシリアライズ用ラッパプロパティ。
        /// </summary>
        [DataMember(Name = nameof(TextAlignment))]
        private string TextAlignmentString
        {
            get { return this.TextAlignment.ToString(); }
            set
            {
                TextAlignment deco;
                this.TextAlignment =
                    Enum.TryParse(value, out deco) ? deco : TextAlignment.TopLeft;
            }
        }

        /// <summary>
        /// 太字にするか否かを取得する。
        /// </summary>
        [ComponentItem(@"B", Order = 6)]
        [DataMember]
        public bool IsBold
        {
            get { return this.bold; }
            set { this.SetProperty(ref this.bold, value); }
        }
        private bool bold = false;

        /// <summary>
        /// イタリック体にするか否かを取得する。
        /// </summary>
        [ComponentItem(@"I", Order = 7)]
        [DataMember]
        public bool IsItalic
        {
            get { return this.italic; }
            set { this.SetProperty(ref this.italic, value); }
        }
        private bool italic = false;

        /// <summary>
        /// 字間幅を取得または設定する。
        /// </summary>
        [ComponentItem(@"spacing_x", typeof(SpaceConverter), Order = 13)]
        [DataMember]
        public int LetterSpace
        {
            get { return this.letterSpace; }
            set
            {
                this.SetProperty(
                    ref this.letterSpace,
                    Math.Min(Math.Max(-100, value), 100));
            }
        }
        private int letterSpace = 0;

        /// <summary>
        /// 行間幅を取得または設定する。
        /// </summary>
        [ComponentItem(@"spacing_y", typeof(SpaceConverter), Order = 14)]
        [DataMember]
        public int LineSpace
        {
            get { return this.lineSpace; }
            set
            {
                this.SetProperty(
                    ref this.lineSpace,
                    Math.Min(Math.Max(-100, value), 100));
            }
        }
        private int lineSpace = 0;

        /// <summary>
        /// 高精細モードを有効にするか否かを取得または設定する。
        /// </summary>
        [ComponentItem(@"precision", Order = 15)]
        [DataMember]
        public bool IsHighDefinition
        {
            get { return this.highDefinition; }
            set { this.SetProperty(ref this.highDefinition, value); }
        }
        private bool highDefinition = true;

        /// <summary>
        /// 文字を滑らかにするか否かを取得または設定する。
        /// </summary>
        [ComponentItem(@"soft", Order = 10)]
        [DataMember]
        public bool IsSoft
        {
            get { return this.soft; }
            set { this.SetProperty(ref this.soft, value); }
        }
        private bool soft = true;

        /// <summary>
        /// 等間隔モードを有効にするか否かを取得または設定する。
        /// </summary>
        [ComponentItem(@"monospace", Order = 11)]
        [DataMember]
        public bool IsMonospacing
        {
            get { return this.monospacing; }
            set { this.SetProperty(ref this.monospacing, value); }
        }
        private bool monospacing = true;

        /// <summary>
        /// テキストを取得または設定する。
        /// </summary>
        [ComponentItem(@"text", typeof(TextConverter), Order = 19)]
        [DataMember]
        public string Text
        {
            get { return this.text; }
            set
            {
                var v = value ?? "";
                if (v.Length > TextLengthLimit)
                {
                    v = v.RemoveSurrogateSafe(TextLengthLimit);
                }

                this.SetProperty(ref this.text, v);
            }
        }
        private string text = "";

        /// <summary>
        /// このコンポーネントの内容を別のコンポーネントへコピーする。
        /// </summary>
        /// <param name="target">コピー先。</param>
        public void CopyTo(TextComponent target)
        {
            this.CopyToCore(target);
        }

        /// <summary>
        /// デシリアライズの直前に呼び出される。
        /// </summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            this.ResetDataMembers();
        }

        #region MovableValue{TConstants} ジェネリッククラス用の定数情報構造体群

        /// <summary>
        /// フォントサイズ用の定数情報クラス。
        /// </summary>
        public struct FontSizeConst : IMovableValueConstants
        {
            public int Digits => 0;
            public decimal DefaultValue => 34;
            public decimal MinValue => 1;
            public decimal MaxValue => 1000;
            public decimal MinSliderValue => 1;
            public decimal MaxSliderValue => 256;
        }

        /// <summary>
        /// 表示速度用の定数情報クラス。
        /// </summary>
        public struct TextSpeedConst : IMovableValueConstants
        {
            public int Digits => 1;
            public decimal DefaultValue => 0;
            public decimal MinValue => 0;
            public decimal MaxValue => 800;
            public decimal MinSliderValue => 0;
            public decimal MaxSliderValue => 100;
        }

        #endregion

        #region 特殊プロパティ用コンポーネントアイテムコンバータ

        /// <summary>
        /// 字間幅および行間幅用のコンポーネントアイテムコンバータクラス。
        /// </summary>
        /// <remarks>
        /// AviUtl拡張編集が byte (0 ～ 255) で扱っているようなのでそれに合わせる。
        /// </remarks>
        public class SpaceConverter : ComponentItemConverter
        {
            /// <summary>
            /// コンストラクタ。
            /// </summary>
            public SpaceConverter() : base()
            {
            }

            /// <summary>
            /// プロパティ値を拡張編集オブジェクトファイルの文字列値に変換する。
            /// </summary>
            /// <param name="value">プロパティ値。</param>
            /// <param name="propertyType">プロパティの型情報。</param>
            /// <returns>文字列値。変換できなかった場合は null 。</returns>
            public override string ToExoFileValue(object value, Type propertyType)
            {
                if (propertyType == null)
                {
                    throw new ArgumentNullException(nameof(propertyType));
                }

                if (value == null)
                {
                    return null;
                }

                try
                {
                    return ((byte)Convert.ToSByte(value)).ToString();
                }
                catch { }
                return null;
            }

            /// <summary>
            /// 拡張編集オブジェクトファイルの文字列値をプロパティ値に変換する。
            /// </summary>
            /// <param name="value">文字列値。</param>
            /// <param name="propertyType">プロパティの型情報。</param>
            /// <returns>プロパティ値を持つタプル。変換できなかった場合は null 。</returns>
            public override Tuple<object> FromExoFileValue(string value, Type propertyType)
            {
                if (propertyType == null)
                {
                    throw new ArgumentNullException(nameof(propertyType));
                }

                byte exoValue;
                if (!byte.TryParse(value, out exoValue))
                {
                    return null;
                }

                try
                {
                    return Tuple.Create(Convert.ChangeType((sbyte)exoValue, propertyType));
                }
                catch { }
                return null;
            }
        }

        /// <summary>
        /// テキスト用のコンポーネントアイテムコンバータクラス。
        /// </summary>
        public class TextConverter : ComponentItemConverter
        {
            /// <summary>
            /// コンストラクタ。
            /// </summary>
            public TextConverter() : base()
            {
            }

            /// <summary>
            /// プロパティ値を拡張編集オブジェクトファイルの文字列値に変換する。
            /// </summary>
            /// <param name="value">プロパティ値。</param>
            /// <param name="propertyType">プロパティの型情報。</param>
            /// <returns>文字列値。変換できなかった場合は null 。</returns>
            public override string ToExoFileValue(object value, Type propertyType)
            {
                if (propertyType == null)
                {
                    throw new ArgumentNullException(nameof(propertyType));
                }

                var propValue = value as string;
                if (propValue == null)
                {
                    return null;
                }

                return
                    string.Join(
                        null,
                        propValue
                            .PadRight(TextLengthLimit + 1, '\0')
                            .Select(c => ((int)c).ToString(@"x4")));
            }

            /// <summary>
            /// 拡張編集オブジェクトファイルの文字列値をプロパティ値に変換する。
            /// </summary>
            /// <param name="value">文字列値。</param>
            /// <param name="propertyType">プロパティの型情報。</param>
            /// <returns>プロパティ値を持つタプル。変換できなかった場合は null 。</returns>
            public override Tuple<object> FromExoFileValue(string value, Type propertyType)
            {
                if (propertyType == null)
                {
                    throw new ArgumentNullException(nameof(propertyType));
                }

                if (propertyType != typeof(string) || string.IsNullOrWhiteSpace(value))
                {
                    return null;
                }

                // 文字数が4の倍数でなければ不可
                var exoValue = value.Trim();
                if (exoValue.Length % 4 != 0)
                {
                    return null;
                }

                // 4文字ずつ切り取り char 値を表す int 配列にする
                var charInts =
                    Enumerable
                        .Range(0, exoValue.Length / 4)
                        .Select(
                            i =>
                            {
                                // 16進数文字列から int に変換
                                int c;
                                bool ok =
                                    int.TryParse(
                                        exoValue.Substring(i * 4, 4),
                                        NumberStyles.AllowHexSpecifier,
                                        CultureInfo.InvariantCulture,
                                        out c);

                                // 変換失敗時は -1 を返す
                                return ok ? c : -1;
                            });

                // 負数が含まれるなら変換失敗
                if (charInts.Any(c => c < 0))
                {
                    return null;
                }

                // '\0' の手前までを文字列化
                var result =
                    new string(charInts.TakeWhile(c => c != 0).Cast<char>().ToArray());
                return Tuple.Create<object>(result);
            }
        }

        #endregion
    }
}
