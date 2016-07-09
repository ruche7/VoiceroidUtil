using System;
using System.Windows;
using Livet.Messaging;
using RucheHome.Voiceroid;

namespace VoiceroidUtil.Messaging
{
    /// <summary>
    /// VOICEROIDプロセスに対してアクションを行うメッセージクラス。
    /// </summary>
    public class VoiceroidActionMessage : InteractionMessage
    {
        /// <summary>
        /// 既定のメッセージキー文字列を取得する。
        /// </summary>
        public static string DefaultMessageKey { get; } =
            typeof(VoiceroidActionMessage).FullName;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public VoiceroidActionMessage() : this(DefaultMessageKey)
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="messageKey">メッセージキー文字列。</param>
        public VoiceroidActionMessage(string messageKey)
            : this(null, VoiceroidAction.None, messageKey)
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="process">VOICEROIDプロセス。</param>
        /// <param name="action">アクション種別。</param>
        public VoiceroidActionMessage(IProcess process, VoiceroidAction action)
            : this(process, action, DefaultMessageKey)
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="process">VOICEROIDプロセス。</param>
        /// <param name="action">アクション種別。</param>
        /// <param name="messageKey">メッセージキー文字列。</param>
        public VoiceroidActionMessage(
            IProcess process,
            VoiceroidAction action,
            string messageKey)
            : base(messageKey)
        {
            this.Process = process;
            this.Action = action;
        }

        /// <summary>
        /// Process 依存関係プロパティ。
        /// </summary>
        public static readonly DependencyProperty ProcessProperty =
            DependencyProperty.Register(
                nameof(Process),
                typeof(IProcess),
                typeof(VoiceroidActionMessage),
                new PropertyMetadata(null));

        /// <summary>
        /// VOICEROIDプロセスを取得または設定する。
        /// </summary>
        public IProcess Process
        {
            get { return (IProcess)this.GetValue(ProcessProperty); }
            set { this.SetValue(ProcessProperty, value); }
        }

        /// <summary>
        /// Action 依存関係プロパティ。
        /// </summary>
        public static readonly DependencyProperty ActionProperty =
            DependencyProperty.Register(
                nameof(Action),
                typeof(VoiceroidAction),
                typeof(VoiceroidActionMessage),
                new PropertyMetadata(VoiceroidAction.None));

        /// <summary>
        /// アクション種別を取得または設定する。
        /// </summary>
        public VoiceroidAction Action
        {
            get { return (VoiceroidAction)this.GetValue(ActionProperty); }
            set { this.SetValue(ActionProperty, value); }
        }

        #region InteractionMessage のオーバライド

        protected override Freezable CreateInstanceCore()
        {
            return new VoiceroidActionMessage();
        }

        #endregion
    }
}
