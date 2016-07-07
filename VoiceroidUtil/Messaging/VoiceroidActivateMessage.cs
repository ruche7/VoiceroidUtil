using System;
using System.Windows;
using Livet.Messaging;
using RucheHome.Voiceroid;

namespace VoiceroidUtil.Messaging
{
    /// <summary>
    /// VOICEROIDプロセスのメインウィンドウをアクティブにするためのメッセージクラス。
    /// </summary>
    /// <remarks>
    /// 実際には、ZオーダーをVoiceroidUtilのメインウィンドウの次に設定する。
    /// </remarks>
    public class VoiceroidActivateMessage : InteractionMessage
    {
        /// <summary>
        /// 既定のメッセージキー文字列を取得する。
        /// </summary>
        public static string DefaultMessageKey { get; } =
            typeof(VoiceroidActivateMessage).FullName;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public VoiceroidActivateMessage() : this(DefaultMessageKey)
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="messageKey">メッセージキー文字列。</param>
        public VoiceroidActivateMessage(string messageKey) : base(messageKey)
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="process">VOICEROIDプロセス。</param>
        public VoiceroidActivateMessage(IProcess process) : this()
        {
            this.Process = process;
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="process">VOICEROIDプロセス。</param>
        /// <param name="messageKey">メッセージキー文字列。</param>
        public VoiceroidActivateMessage(IProcess process, string messageKey)
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
                typeof(VoiceroidActivateMessage),
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
            return new VoiceroidActivateMessage();
        }

        #endregion
    }
}
