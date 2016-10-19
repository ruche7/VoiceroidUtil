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

            // 設定
            this.Common = this.MakeConfigProperty(c => c.Common);
            this.CharaStyles = this.MakeConfigProperty(c => c.CharaStyles);

            // 内包 ViewModel のセットアップ
            this.SetupViewModel(uiConfig, openFileDialogService);

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
        /// キャラ別スタイル設定セットを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<ExoCharaStyleSet> CharaStyles { get; }

        /// <summary>
        /// 選択中キャラ別スタイル設定インデックスを取得する。
        /// </summary>
        public IReactiveProperty<int> SelectedCharaStyleIndex { get; private set; }

        /// <summary>
        /// 選択中キャラ別スタイル設定 ViewModel を取得する。
        /// </summary>
        public ExoCharaStyleViewModel SelectedCharaStyle { get; private set; }

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
        /// 内包 ViewModel のセットアップを行う。
        /// </summary>
        /// <param name="uiConfig">UI設定値。</param>
        /// <param name="openFileDialogService">ファイル選択ダイアログサービス。</param>
        private void SetupViewModel(
            IReadOnlyReactiveProperty<UIConfig> uiConfig,
            IOpenFileDialogService openFileDialogService)
        {
            // 選択中キャラ別スタイルインデックス作成
            this.SelectedCharaStyleIndex =
                uiConfig
                    .ObserveInnerProperty(c => c.ExoCharaVoiceroidId)
                    .Select(id => (int)id)
                    .ToReactiveProperty()
                    .AddTo(this.CompositeDisposable);
            this.SelectedCharaStyleIndex
                .Where(i => i >= 0)
                .Subscribe(i => uiConfig.Value.ExoCharaVoiceroidId = (VoiceroidId)i)
                .AddTo(this.CompositeDisposable);

            // セットまたは選択中インデックスが変化した場合に Unit を発行する
            var selectionNotifier =
                new[]
                {
                    this.CharaStyles.ToUnit(),
                    this.SelectedCharaStyleIndex.ToUnit(),
                }
                .Merge();

            // 必ずアイテム選択状態にする
            // 即座に書き換えるとうまくいかないので少し待ちを入れる
            selectionNotifier
                .Throttle(TimeSpan.FromMilliseconds(10))
                .Where(_ => this.SelectedCharaStyleIndex.Value < 0)
                .Subscribe(
                    _ =>
                        this.SelectedCharaStyleIndex.Value =
                            (int)uiConfig.Value.ExoCharaVoiceroidId)
                .AddTo(this.CompositeDisposable);

            // 選択中 ViewModel 作成
            this.SelectedCharaStyle =
                new ExoCharaStyleViewModel(
                    this.CanModify,
                    selectionNotifier
                        .Select(
                            _ =>
                                this.CharaStyles.Value[
                                    uiConfig.Value.ExoCharaVoiceroidId])
                        .ToReadOnlyReactiveProperty()
                        .AddTo(this.CompositeDisposable),
                    uiConfig,
                    this.LastStatus,
                    openFileDialogService)
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
