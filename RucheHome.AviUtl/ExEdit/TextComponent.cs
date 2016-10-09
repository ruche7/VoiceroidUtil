﻿using System;
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
        #region アイテム名定数群

        /// <summary>
        /// フォントサイズを保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfFontSize = @"サイズ";

        /// <summary>
        /// 表示速度を保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfTextSpeed = @"表示速度";

        /// <summary>
        /// 自動スクロールフラグを保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfIsAutoScrolling = @"自動スクロール";

        /// <summary>
        /// 個別オブジェクト化フラグを保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfIsIndividualizing =
            @"文字毎に個別オブジェクト";

        /// <summary>
        /// 移動座標上表示フラグを保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfIsAligningOnMotion = @"移動座標上に表示する";

        /// <summary>
        /// 高さ自動調整フラグを保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfIsAutoAdjusting = @"autoadjust";

        /// <summary>
        /// フォント色を保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfFontColor = @"color";

        /// <summary>
        /// フォント装飾色を保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfFontDecorationColor = @"color2";

        /// <summary>
        /// フォントファミリ名を保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfFontFamilyName = @"font";

        /// <summary>
        /// フォント装飾種別を保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfFontDecoration = @"type";

        /// <summary>
        /// テキスト配置種別を保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfTextAlignment = @"align";

        /// <summary>
        /// 太字フラグを保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfIsBold = @"B";

        /// <summary>
        /// イタリック体フラグを保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfIsItalic = @"I";

        /// <summary>
        /// 字間幅を保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfLetterSpace = @"spacing_x";

        /// <summary>
        /// 行間幅を保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfLineSpace = @"spacing_y";

        /// <summary>
        /// 高精細モード有効フラグを保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfIsHighDefinition = @"precision";

        /// <summary>
        /// 滑らかフラグを保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfIsSoft = @"soft";

        /// <summary>
        /// 等間隔モード有効フラグを保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfIsMonospacing = @"monospace";

        /// <summary>
        /// テキストを保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfText = @"text";

        #endregion

        /// <summary>
        /// コンポーネント名。
        /// </summary>
        public static readonly string ThisComponentName = @"テキスト";

        /// <summary>
        /// 規定のフォントファミリ名。
        /// </summary>
        public static readonly string DefaultFontFamilyName = @"MS UI Gothic";

        /// <summary>
        /// テキストの最大許容文字数。
        /// </summary>
        public static readonly int TextLengthLimit = 1024 - 1;

        /// <summary>
        /// 拡張編集オブジェクトファイルのアイテムコレクションに
        /// コンポーネント名が含まれているか否かを取得する。
        /// </summary>
        /// <param name="items">アイテムコレクション。</param>
        /// <returns>含まれているならば true 。そうでなければ false 。</returns>
        public static bool HasComponentName(IniFileItemCollection items) =>
            HasComponentNameCore(items, ThisComponentName);

        /// <summary>
        /// 拡張編集オブジェクトファイルのアイテムコレクションから
        /// コンポーネントを作成する。
        /// </summary>
        /// <param name="items">アイテムコレクション。</param>
        /// <returns>コンポーネント。</returns>
        public static TextComponent FromExoFileItems(IniFileItemCollection items) =>
            FromExoFileItemsCore(items, () => new TextComponent());

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public TextComponent() : base()
        {
        }

        /// <summary>
        /// コンポーネント名を取得する。
        /// </summary>
        public override string ComponentName => ThisComponentName;

        /// <summary>
        /// フォントサイズを取得または設定する。
        /// </summary>
        [ExoFileItem(ExoFileItemNameOfFontSize, Order = 1)]
        [DataMember]
        public MovableValue<FontSizeConst> FontSize
        {
            get { return this.fontSize; }
            set
            {
                this.SetPropertyWithPropertyChangedChain(
                    ref this.fontSize,
                    value ?? new MovableValue<FontSizeConst>());
            }
        }
        private MovableValue<FontSizeConst> fontSize = new MovableValue<FontSizeConst>();

        /// <summary>
        /// 表示速度を取得または設定する。
        /// </summary>
        [ExoFileItem(ExoFileItemNameOfTextSpeed, Order = 2)]
        [DataMember]
        public MovableValue<TextSpeedConst> TextSpeed
        {
            get { return this.textSpeed; }
            set
            {
                this.SetPropertyWithPropertyChangedChain(
                    ref this.textSpeed,
                    value ?? new MovableValue<TextSpeedConst>());
            }
        }
        private MovableValue<TextSpeedConst> textSpeed =
            new MovableValue<TextSpeedConst>();

        /// <summary>
        /// 自動スクロールするか否かを取得または設定する。
        /// </summary>
        [ExoFileItem(ExoFileItemNameOfIsAutoScrolling, Order = 5)]
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
        [ExoFileItem(ExoFileItemNameOfIsIndividualizing, Order = 3)]
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
        [ExoFileItem(ExoFileItemNameOfIsAligningOnMotion, Order = 4)]
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
        [ExoFileItem(ExoFileItemNameOfIsAutoAdjusting, Order = 9)]
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
        [ExoFileItem(ExoFileItemNameOfFontColor, Order = 16)]
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
        [ExoFileItem(ExoFileItemNameOfFontDecorationColor, Order = 17)]
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
        [ExoFileItem(ExoFileItemNameOfFontFamilyName, Order = 18)]
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
        [ExoFileItem(ExoFileItemNameOfFontDecoration, Order = 8)]
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
        [ExoFileItem(ExoFileItemNameOfTextAlignment, Order = 12)]
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
        [ExoFileItem(ExoFileItemNameOfIsBold, Order = 6)]
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
        [ExoFileItem(ExoFileItemNameOfIsItalic, Order = 7)]
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
        [ExoFileItem(ExoFileItemNameOfLetterSpace, typeof(SpaceConverter), Order = 13)]
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
        [ExoFileItem(ExoFileItemNameOfLineSpace, typeof(SpaceConverter), Order = 14)]
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
        [ExoFileItem(ExoFileItemNameOfIsHighDefinition, Order = 15)]
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
        [ExoFileItem(ExoFileItemNameOfIsSoft, Order = 10)]
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
        [ExoFileItem(ExoFileItemNameOfIsMonospacing, Order = 11)]
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
        [ExoFileItem(ExoFileItemNameOfText, typeof(TextConverter), Order = 19)]
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
            public decimal MinValue => 0;
            public decimal MaxValue => 1000;
            public decimal MinSliderValue => 0;
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
        /// 字間幅および行間幅用のコンバータクラス。
        /// </summary>
        /// <remarks>
        /// AviUtl拡張編集が byte (0 ～ 255) で扱っているようなのでそれに合わせる。
        /// </remarks>
        public class SpaceConverter : IExoFileValueConverter
        {
            /// <summary>
            /// コンストラクタ。
            /// </summary>
            public SpaceConverter()
            {
            }

            /// <summary>
            /// .NETオブジェクト値を拡張編集オブジェクトファイルの文字列値に変換する。
            /// </summary>
            /// <param name="value">.NETオブジェクト値。</param>
            /// <param name="objectType">.NETオブジェクトの型情報。</param>
            /// <returns>文字列値。変換できなかった場合は null 。</returns>
            public string ToExoFileValue(object value, Type objectType)
            {
                if (objectType == null)
                {
                    throw new ArgumentNullException(nameof(objectType));
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
            /// 拡張編集オブジェクトファイルの文字列値を.NETオブジェクト値に変換する。
            /// </summary>
            /// <param name="value">文字列値。</param>
            /// <param name="objectType">.NETオブジェクトの型情報。</param>
            /// <returns>
            /// .NETオブジェクト値を持つタプル。変換できなかったならば null 。
            /// </returns>
            public Tuple<object> FromExoFileValue(string value, Type objectType)
            {
                if (objectType == null)
                {
                    throw new ArgumentNullException(nameof(objectType));
                }

                byte exoValue;
                if (!byte.TryParse(value, out exoValue))
                {
                    return null;
                }

                try
                {
                    return Tuple.Create(Convert.ChangeType((sbyte)exoValue, objectType));
                }
                catch { }
                return null;
            }
        }

        /// <summary>
        /// テキスト用のコンバータクラス。
        /// </summary>
        public class TextConverter : IExoFileValueConverter
        {
            /// <summary>
            /// コンストラクタ。
            /// </summary>
            public TextConverter()
            {
            }

            /// <summary>
            /// .NETオブジェクト値を拡張編集オブジェクトファイルの文字列値に変換する。
            /// </summary>
            /// <param name="value">.NETオブジェクト値。</param>
            /// <param name="objectType">.NETオブジェクトの型情報。</param>
            /// <returns>文字列値。変換できなかった場合は null 。</returns>
            public string ToExoFileValue(object value, Type objectType)
            {
                if (objectType == null)
                {
                    throw new ArgumentNullException(nameof(objectType));
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
            /// 拡張編集オブジェクトファイルの文字列値を.NETオブジェクト値に変換する。
            /// </summary>
            /// <param name="value">文字列値。</param>
            /// <param name="objectType">.NETオブジェクトの型情報。</param>
            /// <returns>
            /// .NETオブジェクト値を持つタプル。変換できなかったならば null 。
            /// </returns>
            public Tuple<object> FromExoFileValue(string value, Type objectType)
            {
                if (objectType == null)
                {
                    throw new ArgumentNullException(nameof(objectType));
                }

                if (objectType != typeof(string) || string.IsNullOrWhiteSpace(value))
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
                    new string(
                        charInts.TakeWhile(c => c != 0).Select(c => (char)c).ToArray());
                return Tuple.Create<object>(result);
            }
        }

        #endregion
    }
}
