using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.Voiceroid;
using VoiceroidUtil.Extensions;
using VoiceroidUtil.Services;

namespace VoiceroidUtil.ViewModel
{
    /// <summary>
    /// AviUtl拡張編集ファイル用設定とそれに対する処理を提供する ViewModel クラス。
    /// </summary>
    public class ExoConfigViewModel : ConfigViewModelBase<ExoConfig>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="canModify">
        /// 再生や音声保存に関わる設定値の変更可否状態値。
        /// </param>
        /// <param name="config">設定値。</param>
        /// <param name="appConfig">アプリ設定値。</param>
        /// <param name="uiConfig">UI設定値。</param>
        /// <param name="lastStatus">直近のアプリ状態値の設定先。</param>
        /// <param name="openFileDialogService">ファイル選択ダイアログサービス。</param>
        public ExoConfigViewModel(
            IReadOnlyReactiveProperty<bool> canModify,
            IReadOnlyReactiveProperty<ExoConfig> config,
            IReadOnlyReactiveProperty<AppConfig> appConfig,
            IReadOnlyReactiveProperty<UIConfig> uiConfig,
            IReactiveProperty<IAppStatus> lastStatus,
            IOpenFileDialogService openFileDialogService)
            : base(canModify, config)
        {
            this.ValidateArgNull(appConfig, nameof(appConfig));
            this.ValidateArgNull(uiConfig, nameof(uiConfig));
            this.ValidateArgNull(lastStatus, nameof(lastStatus));
            this.ValidateArgNull(openFileDialogService, nameof(openFileDialogService));

            this.AppConfig = appConfig;
            this.LastStatus = lastStatus;

            // 選択中タブインデックス
            this.SelectedTabIndex =
                this.MakeInnerPropertyOf(uiConfig, c => c.ExoConfigTabIndex);

            // 共通設定
            this.Common = this.MakeConfigProperty(c => c.Common);

            // 表示状態のキャラ別スタイル設定コレクション
            this.VisibleCharaStyles =
                Observable
                    .CombineLatest(
                        this.ObserveConfigProperty(c => c.CharaStyles),
                        appConfig.ObserveInnerProperty(c => c.VoiceroidVisibilities),
                        (cs, vv) => vv.SelectVisibleOf(cs))
                    .ToReadOnlyReactiveProperty()
                    .AddTo(this.CompositeDisposable);

            // 選択中キャラ別スタイル
            this.SelectedCharaStyle =
                new ReactiveProperty<ExoCharaStyle>(this.VisibleCharaStyles.Value.First())
                    .AddTo(this.CompositeDisposable);

            // UI設定周りのセットアップ
            this.SetupUIConfig(uiConfig);

            // 選択中キャラ別スタイル ViewModel 作成
            this.SelectedCharaStyleViewModel =
                new ExoCharaStyleViewModel(
                    this.CanModify,
                    this.SelectedCharaStyle,
                    uiConfig,
                    this.LastStatus,
                    openFileDialogService)
                    .AddTo(this.CompositeDisposable);

            // ファイル作成設定有効化コマンド表示状態
            this.IsFileMakingCommandInvisible =
                this.AppConfig
                    .ObserveInnerProperty(c => c.IsExoFileMaking)
                    .ToReadOnlyReactiveProperty()
                    .AddTo(this.CompositeDisposable);
            this.IsFileMakingCommandVisible =
                this.IsFileMakingCommandInvisible
                    .Inverse()
                    .ToReadOnlyReactiveProperty()
                    .AddTo(this.CompositeDisposable);

            // ファイル作成設定有効化コマンド
            this.FileMakingCommand =
                this.MakeCommand(
                    this.ExecuteFileMakingCommand,
                    this.CanModify,
                    this.IsFileMakingCommandVisible);
        }

        /// <summary>
        /// 選択中タブインデックスを取得する。
        /// </summary>
        public IReactiveProperty<int> SelectedTabIndex { get; }

        /// <summary>
        /// 共通設定を取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<ExoCommonConfig> Common { get; }

        /// <summary>
        /// 表示状態のキャラ別スタイル設定コレクションを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<ReadOnlyCollection<ExoCharaStyle>>
        VisibleCharaStyles
        {
            get;
        }

        /// <summary>
        /// 選択中キャラ別スタイル設定を取得する。
        /// </summary>
        public ReactiveProperty<ExoCharaStyle> SelectedCharaStyle { get; }

        /// <summary>
        /// 選択中キャラ別スタイル設定 ViewModel を取得する。
        /// </summary>
        public ExoCharaStyleViewModel SelectedCharaStyleViewModel { get; }

        /// <summary>
        /// ファイル作成設定有効化コマンドを表示すべきか否かを取得する。
        /// </summary>
        public ReadOnlyReactiveProperty<bool> IsFileMakingCommandVisible { get; }

        /// <summary>
        /// ファイル作成設定有効化コマンドを非表示にすべきか否かを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsFileMakingCommandInvisible { get; }

        /// <summary>
        /// ファイル作成設定有効化コマンドを取得する。
        /// </summary>
        public ICommand FileMakingCommand { get; }

        /// <summary>
        /// アプリ設定を取得する。
        /// </summary>
        private IReadOnlyReactiveProperty<AppConfig> AppConfig { get; }

        /// <summary>
        /// 直近のアプリ状態値の設定先を取得する。
        /// </summary>
        private IReactiveProperty<IAppStatus> LastStatus { get; }

        /// <summary>
        /// UI設定周りのセットアップを行う。
        /// </summary>
        /// <param name="uiConfig">UI設定値。</param>
        private void SetupUIConfig(IReadOnlyReactiveProperty<UIConfig> uiConfig)
        {
            // 設定変更時に選択中キャラ別スタイル反映
            Observable
                .CombineLatest(
                    this.VisibleCharaStyles,
                    uiConfig.ObserveInnerProperty(c => c.ExoCharaVoiceroidId),
                    (vcs, id) =>
                        vcs.FirstOrDefault(s => s.VoiceroidId == id) ?? vcs.First())
                .Subscribe(s => this.SelectedCharaStyle.Value = s)
                .AddTo(this.CompositeDisposable);

            // 選択中キャラ別スタイル変更時処理
            // 上書きは即座に行うとうまくいかないので少し待ちを入れる
            this.SelectedCharaStyle
                .Where(s => s != null)
                .Subscribe(s => uiConfig.Value.ExoCharaVoiceroidId = s.VoiceroidId)
                .AddTo(this.CompositeDisposable);
            this.SelectedCharaStyle
                .Throttle(TimeSpan.FromMilliseconds(10))
                .Where(s => s == null)
                .Subscribe(
                    _ =>
                        this.SelectedCharaStyle.Value =
                            this.BaseConfig.Value.CharaStyles.First(
                                s => s.VoiceroidId == uiConfig.Value.ExoCharaVoiceroidId))
                .AddTo(this.CompositeDisposable);
        }

        /// <summary>
        /// FileMakingCommand の実処理を行う。
        /// </summary>
        private void ExecuteFileMakingCommand()
        {
            if (
                !this.CanModify.Value ||
                this.AppConfig.Value?.IsExoFileMaking != false)
            {
                return;
            }

            this.AppConfig.Value.IsExoFileMaking = true;

            this.LastStatus.Value =
                new AppStatus
                {
                    StatusType = AppStatusType.Success,
                    StatusText = @".exo ファイル作成設定を有効にしました。",
                };
        }

        #region デザイン時用定義

        /// <summary>
        /// デザイン時用コンストラクタ。
        /// </summary>
        [Obsolete(@"Design time only.")]
        public ExoConfigViewModel()
            :
            this(
                new ReactiveProperty<bool>(true),
                new ReactiveProperty<ExoConfig>(new ExoConfig()),
                new ReactiveProperty<AppConfig>(new AppConfig { IsExoFileMaking = true }),
                new ReactiveProperty<UIConfig>(new UIConfig()),
                new ReactiveProperty<IAppStatus>(new AppStatus()),
                NullServices.OpenFileDialog)
        {
        }

        #endregion
    }
}
