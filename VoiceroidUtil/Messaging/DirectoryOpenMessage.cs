using System;
using System.Windows;
using Livet.Messaging;

namespace VoiceroidUtil.Messaging
{
    /// <summary>
    /// ディレクトリをエクスプローラで開くためのメッセージクラス。
    /// </summary>
    /// <remarks>
    /// Response には、処理が行われなかった場合は null 、
    /// 成功した場合は StatusType が AppStatusType.None の状態値、
    /// それ以外の場合は StatusType が AppStatusType.None 以外の状態値が設定される。
    /// </remarks>
    public class DirectoryOpenMessage : ResponsiveInteractionMessage<IAppStatus>
    {
        /// <summary>
        /// 既定のメッセージキー文字列を取得する。
        /// </summary>
        public static string DefaultMessageKey { get; } =
            typeof(DirectoryOpenMessage).FullName;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public DirectoryOpenMessage() : this(DefaultMessageKey)
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="messageKey">メッセージキー文字列。</param>
        public DirectoryOpenMessage(string messageKey) : base(messageKey)
        {
        }

        /// <summary>
        /// Path 依存関係プロパティ。
        /// </summary>
        public static readonly DependencyProperty PathProperty =
            DependencyProperty.Register(
                nameof(Path),
                typeof(string),
                typeof(DirectoryOpenMessage),
                new PropertyMetadata(null));

        /// <summary>
        /// ディレクトリパスを取得または設定する。
        /// </summary>
        public string Path
        {
            get { return (string)this.GetValue(PathProperty); }
            set { this.SetValue(PathProperty, value); }
        }

        #region ResponsiveInteractionMessage<IAppStatus> のオーバライド

        protected override Freezable CreateInstanceCore()
        {
            return new DirectoryOpenMessage();
        }

        #endregion
    }
}
