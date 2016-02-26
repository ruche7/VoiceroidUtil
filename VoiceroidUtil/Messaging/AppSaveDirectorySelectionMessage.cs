using System;
using System.Windows;
using Livet.Messaging;

namespace VoiceroidUtil.Messaging
{
    /// <summary>
    /// アプリ設定の保存先ディレクトリを選択するためのメッセージクラス。
    /// </summary>
    /// <remarks>
    /// Response には、選択が行われなかった場合やキャンセルされた場合は null 、
    /// 成功した場合は StatusType が AppStatusType.None の状態値、
    /// 失敗した場合は StatusType が AppStatusType.Warning の状態値が設定される。
    /// </remarks>
    public class AppSaveDirectorySelectionMessage
        : ResponsiveInteractionMessage<IAppStatus>
    {
        /// <summary>
        /// 既定のメッセージキー文字列を取得する。
        /// </summary>
        public static string DefaultMessageKey { get; } =
            typeof(AppSaveDirectorySelectionMessage).FullName;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public AppSaveDirectorySelectionMessage() : this(DefaultMessageKey)
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="messageKey">メッセージキー文字列。</param>
        public AppSaveDirectorySelectionMessage(string messageKey) : base(messageKey)
        {
        }

        /// <summary>
        /// Config 依存関係プロパティ。
        /// </summary>
        public static readonly DependencyProperty ConfigProperty =
            DependencyProperty.Register(
                nameof(Config),
                typeof(AppConfig),
                typeof(AppSaveDirectorySelectionMessage),
                new PropertyMetadata(null));

        /// <summary>
        /// アプリ設定値を取得または設定する。
        /// </summary>
        public AppConfig Config
        {
            get { return (AppConfig)this.GetValue(ConfigProperty); }
            set { this.SetValue(ConfigProperty, value); }
        }

        #region ResponsiveInteractionMessage<IAppStatus> のオーバライド

        protected override Freezable CreateInstanceCore()
        {
            return new AppSaveDirectorySelectionMessage();
        }

        #endregion
    }
}
