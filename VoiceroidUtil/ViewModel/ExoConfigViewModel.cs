using System;
using System.Linq;
using System.Reactive.Linq;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.Voiceroid;

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
        public ExoConfigViewModel() : base(new ExoConfig())
        {
            // 設定
            this.AppConfig =
                new ReactiveProperty<AppConfig>(new AppConfig())
                    .AddTo(this.CompositeDisposable);
            this.UIConfig =
                new ReactiveProperty<UIConfig>(new UIConfig())
                    .AddTo(this.CompositeDisposable);

            // 直近のアプリ状態値
            this.LastStatus =
                new ReactiveProperty<IAppStatus>(new AppStatus())
                    .AddTo(this.CompositeDisposable);

            // 内包 ViewModel のセットアップ
            this.SetupViewModel();

            // ファイル作成設定有効化コマンド表示状態
            this.IsFileMakingCommandInvisible =
                this.AppConfig
                    .Select(
                        config =>
                            (config == null) ?
                                Observable.Return(true) :
                                config.ObserveProperty(c => c.IsExoFileMaking))
                    .Switch()
                    .ToReadOnlyReactiveProperty()
                    .AddTo(this.CompositeDisposable);
            this.IsFileMakingCommandVisible =
                this.IsFileMakingCommandInvisible
                    .Select(f => !f)
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
        /// アプリ設定を取得する。
        /// </summary>
        /// <remarks>
        /// ファイル保存設定の操作にのみ用いる。
        /// </remarks>
        public ReactiveProperty<AppConfig> AppConfig { get; }

        /// <summary>
        /// UI設定値を取得する。
        /// </summary>
        public ReactiveProperty<UIConfig> UIConfig { get; }

        /// <summary>
        /// 直近のアプリ状態値を取得する。
        /// </summary>
        public ReactiveProperty<IAppStatus> LastStatus { get; }

        /// <summary>
        /// キャラ別スタイル設定 ViewModel を取得する。
        /// </summary>
        public ExoCharaStyleViewModel CharaStyle { get; private set; }

        /// <summary>
        /// ファイル作成設定有効化コマンドを表示すべきか否かを取得する。
        /// </summary>
        public ReadOnlyReactiveProperty<bool> IsFileMakingCommandVisible { get; }

        /// <summary>
        /// ファイル作成設定有効化コマンドを非表示にすべきか否かを取得する。
        /// </summary>
        public ReadOnlyReactiveProperty<bool> IsFileMakingCommandInvisible { get; }

        /// <summary>
        /// ファイル作成設定有効化コマンドを取得する。
        /// </summary>
        public ReactiveCommand FileMakingCommand { get; }

        /// <summary>
        /// 内包 ViewModel のセットアップを行う。
        /// </summary>
        private void SetupViewModel()
        {
            // 暫定値で初期化
            this.CharaStyle =
                new ExoCharaStyleViewModel(
                    this.Value.CharaStyles[this.UIConfig.Value.ExoCharaVoiceroidId],
                    this.UIConfig);

            // 設定変更時に反映
            this
                .ObserveConfigProperty(c => c.CharaStyles)
                .Where(styles => styles != null)
                .Subscribe(
                    styles =>
                        this.CharaStyle.Value =
                            styles[this.UIConfig.Value.ExoCharaVoiceroidId])
                .AddTo(this.CompositeDisposable);

            // UI設定変更時に選択キャラ反映
            this.UIConfig
                .Select(
                    config =>
                        (config == null) ?
                            Observable.Empty<VoiceroidId>() :
                            config.ObserveProperty(c => c.ExoCharaVoiceroidId))
                .Switch()
                .Subscribe(id => this.CharaStyle.Value = this.Value.CharaStyles[id])
                .AddTo(this.CompositeDisposable);

            // 選択キャラ変更時にUI設定へ反映
            this.CharaStyle
                .ObserveProperty(vm => vm.Value)
                .Where(_ => this.UIConfig.Value != null)
                .Select(cs => cs.VoiceroidId)
                .Subscribe(id => this.UIConfig.Value.ExoCharaVoiceroidId = id)
                .AddTo(this.CompositeDisposable);

            // CanModify 同期
            this.CharaStyle.CanModify.Value = this.CanModify.Value;
            this.CanModify
                .Subscribe(f => this.CharaStyle.CanModify.Value = f)
                .AddTo(this.CompositeDisposable);

            // LastStatus 反映
            this.CharaStyle.LastStatus
                .Where(s => s != null)
                .Subscribe(s => this.LastStatus.Value = s)
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
    }
}
