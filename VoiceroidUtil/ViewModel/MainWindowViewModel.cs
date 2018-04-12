using System;
using System.Collections.Generic;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.Voiceroid;
using VoiceroidUtil.Services;
using static RucheHome.Util.ArgumentValidater;

namespace VoiceroidUtil.ViewModel
{
    /// <summary>
    /// メインウィンドウの ViewModel クラス。
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="processes">VOICEROIDプロセスコレクション。</param>
        /// <param name="canUseConfig">各設定値を利用可能な状態であるか否か。</param>
        /// <param name="talkTextReplaceConfig">トークテキスト置換設定値。</param>
        /// <param name="exoConfig">AviUtl拡張編集ファイル用設定値。</param>
        /// <param name="appConfig">アプリ設定値。</param>
        /// <param name="uiConfig">UI設定値。</param>
        /// <param name="lastStatus">直近のアプリ状態値の設定先。</param>
        /// <param name="openFileDialogService">ファイル選択ダイアログサービス。</param>
        /// <param name="windowActivateService">ウィンドウアクティブ化サービス。</param>
        /// <param name="voiceroidActionService">
        /// VOICEROIDプロセスアクションサービス。
        /// </param>
        public MainWindowViewModel(
            IReadOnlyCollection<IProcess> processes,
            IReadOnlyReactiveProperty<bool> canUseConfig,
            IReadOnlyReactiveProperty<TalkTextReplaceConfig> talkTextReplaceConfig,
            IReadOnlyReactiveProperty<ExoConfig> exoConfig,
            IReadOnlyReactiveProperty<AppConfig> appConfig,
            IReadOnlyReactiveProperty<UIConfig> uiConfig,
            IReactiveProperty<IAppStatus> lastStatus,
            IOpenFileDialogService openFileDialogService,
            IWindowActivateService windowActivateService,
            IVoiceroidActionService voiceroidActionService)
        {
            ValidateArgumentNull(processes, nameof(processes));
            ValidateArgumentNull(canUseConfig, nameof(canUseConfig));
            ValidateArgumentNull(talkTextReplaceConfig, nameof(talkTextReplaceConfig));
            ValidateArgumentNull(exoConfig, nameof(exoConfig));
            ValidateArgumentNull(appConfig, nameof(appConfig));
            ValidateArgumentNull(uiConfig, nameof(uiConfig));
            ValidateArgumentNull(lastStatus, nameof(lastStatus));
            ValidateArgumentNull(openFileDialogService, nameof(openFileDialogService));
            ValidateArgumentNull(windowActivateService, nameof(windowActivateService));
            ValidateArgumentNull(voiceroidActionService, nameof(voiceroidActionService));

            this.IsTopmost = this.MakeInnerPropertyOf(appConfig, c => c.IsTopmost);

            // VoiceroidViewModel 作成
            var canModifyNotifier =
                new ReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);
            this.Voiceroid =
                new VoiceroidViewModel(
                    processes,
                    canUseConfig,
                    talkTextReplaceConfig,
                    exoConfig,
                    appConfig,
                    uiConfig,
                    lastStatus,
                    canModifyNotifier,
                    windowActivateService,
                    voiceroidActionService)
                    .AddTo(this.CompositeDisposable);

            // 設定変更可能状態
            var canModify =
                new[] { canUseConfig, canModifyNotifier }
                    .CombineLatestValuesAreAllTrue()
                    .ToReadOnlyReactiveProperty()
                    .AddTo(this.CompositeDisposable);

            // その他 ViewModel 作成
            this.TalkTextReplaceConfig =
                new TalkTextReplaceConfigViewModel(
                    canModify,
                    talkTextReplaceConfig,
                    appConfig,
                    uiConfig,
                    lastStatus)
                    .AddTo(this.CompositeDisposable);
            this.ExoConfig =
                new ExoConfigViewModel(
                    canModify,
                    exoConfig,
                    appConfig,
                    uiConfig,
                    lastStatus,
                    openFileDialogService)
                    .AddTo(this.CompositeDisposable);
            this.AppConfig =
                new AppConfigViewModel(
                    canModify,
                    appConfig,
                    uiConfig,
                    lastStatus,
                    openFileDialogService)
                    .AddTo(this.CompositeDisposable);
            this.LastStatus =
                new AppStatusViewModel(lastStatus).AddTo(this.CompositeDisposable);
        }

        /// <summary>
        /// タイトルを取得する。
        /// </summary>
        public string Title => nameof(VoiceroidUtil);

        /// <summary>
        /// ウィンドウを常に最前面に表示するか否かを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsTopmost { get; }

        /// <summary>
        /// VOICEROID操作 ViewModel を取得する。
        /// </summary>
        public VoiceroidViewModel Voiceroid { get; }

        /// <summary>
        /// トークテキスト置換設定 ViewModel を取得する。
        /// </summary>
        public TalkTextReplaceConfigViewModel TalkTextReplaceConfig { get; }

        /// <summary>
        /// AviUtl拡張編集ファイル用設定 ViewModel を取得する。
        /// </summary>
        public ExoConfigViewModel ExoConfig { get; }

        /// <summary>
        /// アプリ設定 ViewModel を取得する。
        /// </summary>
        public AppConfigViewModel AppConfig { get; }

        /// <summary>
        /// 直近のアプリ状態 ViewModel を取得する。
        /// </summary>
        public AppStatusViewModel LastStatus { get; }

        #region デザイン時用定義

        /// <summary>
        /// デザイン時用コンストラクタ。
        /// </summary>
        [Obsolete(@"Design time only.")]
        public MainWindowViewModel()
            :
            this(
                new ProcessFactory().Processes,
                new ReactiveProperty<bool>(true),
                new ReactiveProperty<TalkTextReplaceConfig>(new TalkTextReplaceConfig()),
                new ReactiveProperty<ExoConfig>(new ExoConfig()),
                new ReactiveProperty<AppConfig>(new AppConfig()),
                new ReactiveProperty<UIConfig>(new UIConfig()),
                new ReactiveProperty<IAppStatus>(new AppStatus()),
                NullServices.OpenFileDialog,
                NullServices.WindowActivate,
                NullServices.VoiceroidAction)
        {
        }

        #endregion
    }
}
