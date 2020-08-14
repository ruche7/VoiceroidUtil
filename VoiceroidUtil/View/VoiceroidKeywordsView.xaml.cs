using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RucheHome.Voiceroid;

namespace VoiceroidUtil.View
{
    /// <summary>
    /// VOICEROIDキーワード表示を行うユーザコントロールクラス。
    /// </summary>
    public partial class VoiceroidKeywordsView : UserControl
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public VoiceroidKeywordsView() => this.InitializeComponent();

        /// <summary>
        /// VoiceroidNameHeader 依存関係プロパティ。
        /// </summary>
        public static readonly DependencyProperty VoiceroidNameHeaderProperty =
            DependencyProperty.Register(
                nameof(VoiceroidNameHeader),
                typeof(object),
                typeof(VoiceroidKeywordsView),
                new UIPropertyMetadata(@"VOICEROID"));

        /// <summary>
        /// VOICEROID名のヘッダ文字列を取得または設定する。
        /// </summary>
        public object VoiceroidNameHeader
        {
            get => this.GetValue(VoiceroidNameHeaderProperty);
            set => this.SetValue(VoiceroidNameHeaderProperty, value);
        }

        /// <summary>
        /// VOICEROID短縮名とキーワードリスト文字列のディクショナリを取得する。
        /// </summary>
        public IReadOnlyDictionary<string, string> VoiceroidKeywords { get; } =
            ((VoiceroidId[])Enum.GetValues(typeof(VoiceroidId)))
                .Where(id => id.GetInfo().Keywords.Count > 0)
                .ToDictionary(
                    id => id.GetInfo().ShortName,
                    id => string.Join(@", ", id.GetInfo().Keywords));
    }
}
