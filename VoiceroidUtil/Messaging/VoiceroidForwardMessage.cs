using System;
using System.Windows;
using Livet.Messaging;
using RucheHome.Voiceroid;

namespace VoiceroidUtil.Messaging
{
    /// <summary>
    /// VOICEROIDプロセスのメインウィンドウを前面に出すためのメッセージクラス。
    /// </summary>
    /// <remarks>
    /// 実際には、ZオーダーをVoiceroidUtilのメインウィンドウの次に設定する。
    /// ただし最前面表示状態にすることはない。
    /// </remarks>
    public class VoiceroidForwardMessage : InteractionMessage
    {
        /// <summary>
        /// 既定のメッセージキー文字列を取得する。
        /// </summary>
        public static string DefaultMessageKey { get; } =
            typeof(VoiceroidForwardMessage).FullName;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public VoiceroidForwardMessage() : this(DefaultMessageKey)
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="messageKey">メッセージキー文字列。</param>
        public VoiceroidForwardMessage(string messageKey) : base(messageKey)
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="process">VOICEROIDプロセス。</param>
        public VoiceroidForwardMessage(IProcess process) : this()
        {
            this.Process = process;
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="process">VOICEROIDプロセス。</param>
        /// <param name="messageKey">メッセージキー文字列。</param>
        public VoiceroidForwardMessage(IProcess process, string messageKey)
            : this(messageKey)
        {
            this.Process = process;
        }

        /// <summary>
        /// Process 依存関係プロパティ。
        /// </summary>
        public static readonly DependencyProperty ProcessProperty =
            DependencyProperty.Register(
                nameof(Process),
                typeof(IProcess),
                typeof(VoiceroidForwardMessage),
                new PropertyMetadata(null));

        /// <summary>
        /// VOICEROIDプロセスを取得または設定する。
        /// </summary>
        public IProcess Process
        {
            get { return (IProcess)this.GetValue(ProcessProperty); }
            set { this.SetValue(ProcessProperty, value); }
        }

        #region InteractionMessage のオーバライド

        protected override Freezable CreateInstanceCore()
        {
            return new VoiceroidForwardMessage();
        }

        #endregion
    }
}
