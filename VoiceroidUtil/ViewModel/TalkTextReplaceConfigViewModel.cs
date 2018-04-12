using System;
using System.Windows.Input;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using VoiceroidUtil.Extensions;
using static RucheHome.Util.ArgumentValidater;

namespace VoiceroidUtil.ViewModel
{
    /// <summary>
    /// トークテキスト置換設定とそれに対する処理を提供する ViewModel クラス。
    /// </summary>
    public class TalkTextReplaceConfigViewModel
        : ConfigViewModelBase<TalkTextReplaceConfig>
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
        public TalkTextReplaceConfigViewModel(
            IReadOnlyReactiveProperty<bool> canModify,
            IReadOnlyReactiveProperty<TalkTextReplaceConfig> config,
            IReadOnlyReactiveProperty<AppConfig> appConfig,
            IReadOnlyReactiveProperty<UIConfig> uiConfig,
            IReactiveProperty<IAppStatus> lastStatus)
            : base(canModify, config)
        {
            ValidateArgumentNull(appConfig, nameof(appConfig));
            ValidateArgumentNull(uiConfig, nameof(uiConfig));
            ValidateArgumentNull(lastStatus, nameof(lastStatus));

            this.AppConfig = appConfig;
            this.LastStatus = lastStatus;

            // 選択中タブインデックス
            this.SelectedTabIndex =
                this.MakeInnerPropertyOf(uiConfig, c => c.TalkTextReplaceConfigTabIndex);

            // 内包 ViewModel のセットアップ
            this.SetupViewModels();

            // ファイル作成設定有効化コマンド表示状態
            this.IsFileMakingCommandVisible =
                new[]
                {
                    appConfig.ObserveInnerProperty(c => c.IsTextFileForceMaking),
                    appConfig.ObserveInnerProperty(c => c.IsExoFileMaking),
                }
                .CombineLatestValuesAreAllFalse()
                .ToReadOnlyReactiveProperty()
                .AddTo(this.CompositeDisposable);
            this.IsFileMakingCommandInvisible =
                this.IsFileMakingCommandVisible
                    .Inverse()
                    .ToReadOnlyReactiveProperty()
                    .AddTo(this.CompositeDisposable);

            // ファイル作成設定有効化コマンド
            this.FileMakingCommand =
                this.MakeCommand<string>(
                    this.ExecuteFileMakingCommand,
                    this.CanModify,
                    this.IsFileMakingCommandVisible);
        }

        /// <summary>
        /// 選択中タブインデックスを取得または設定する。
        /// </summary>
        public IReactiveProperty<int> SelectedTabIndex { get; }

        /// <summary>
        /// 音声のトークテキスト置換アイテムコレクション ViewModel を取得する。
        /// </summary>
        public TalkTextReplaceItemsViewModel VoiceReplaceItems { get; private set; }

        /// <summary>
        /// 字幕用ファイルのトークテキスト置換アイテムコレクション ViewModel を取得する。
        /// </summary>
        public TalkTextReplaceItemsViewModel TextFileReplaceItems { get; private set; }

        /// <summary>
        /// ファイル作成設定有効化コマンドを表示すべきか否かを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsFileMakingCommandVisible { get; }

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
        private void SetupViewModels()
        {
            // ViewModel 作成
            this.VoiceReplaceItems =
                new TalkTextReplaceItemsViewModel(
                    this.CanModify,
                    this.MakeConfigProperty(c => c.VoiceReplaceItems));
            this.TextFileReplaceItems =
                new TalkTextReplaceItemsViewModel(
                    this.CanModify,
                    this.MakeConfigProperty(c => c.TextFileReplaceItems));

            // 長音プリセット設定
            var longSoundPreset = new TalkTextReplacePreset(@"「～」を「ー」に置換");
            longSoundPreset.Items.Add(
                new TalkTextReplaceItem { OldValue = @"～", NewValue = @"ー" });
            this.VoiceReplaceItems.Presets.Add(longSoundPreset);

            // 記号ポーズプリセット設定
            var symbolPausePreset = new TalkTextReplacePreset(@"記号ポーズ文字削除セット");
            symbolPausePreset.Items.Add(
                new TalkTextReplaceItem { OldValue = @"＃", NewValue = @"" });
            symbolPausePreset.Items.Add(
                new TalkTextReplaceItem { OldValue = @"#", NewValue = @"" });
            symbolPausePreset.Items.Add(
                new TalkTextReplaceItem { OldValue = @"＠", NewValue = @"" });
            symbolPausePreset.Items.Add(
                new TalkTextReplaceItem { OldValue = @"@", NewValue = @"" });
            symbolPausePreset.Items.Add(
                new TalkTextReplaceItem { OldValue = @"■", NewValue = @"" });
            symbolPausePreset.Items.Add(
                new TalkTextReplaceItem { OldValue = @"●", NewValue = @"" });
            symbolPausePreset.Items.Add(
                new TalkTextReplaceItem { OldValue = @"▲", NewValue = @"" });
            this.TextFileReplaceItems.Presets.Add(symbolPausePreset);
        }

        /// <summary>
        /// FileMakingCommand の実処理を行う。
        /// </summary>
        /// <param name="target">
        /// コマンドパラメータ。
        /// "text" ならテキストファイル作成設定を有効化する。
        /// "exo" ならAviUtl拡張編集ファイル作成設定を有効化する。
        /// それ以外なら両方を有効化する。
        /// </param>
        private void ExecuteFileMakingCommand(string target)
        {
            if (
                !this.CanModify.Value ||
                (this.AppConfig.Value.IsTextFileForceMaking != false &&
                 this.AppConfig.Value.IsExoFileMaking != false))
            {
                return;
            }

            // 設定有効化
            var statusText = @"ファイル作成設定を有効にしました。";
            if (target == @"text")
            {
                this.AppConfig.Value.IsTextFileForceMaking = true;
                statusText = @"テキスト" + statusText;
            }
            else if (target == @"exo")
            {
                this.AppConfig.Value.IsExoFileMaking = true;
                statusText = @".exo " + statusText;
            }
            else
            {
                this.AppConfig.Value.IsTextFileForceMaking = true;
                this.AppConfig.Value.IsExoFileMaking = true;
            }

            // ステータス更新
            this.LastStatus.Value =
                new AppStatus
                {
                    StatusType = AppStatusType.Success,
                    StatusText = statusText,
                };
        }

        #region デザイン時用定義

        /// <summary>
        /// デザイン時用コンストラクタ。
        /// </summary>
        [Obsolete(@"Design time only.")]
        public TalkTextReplaceConfigViewModel()
            :
            this(
                new ReactiveProperty<bool>(true),
                new ReactiveProperty<TalkTextReplaceConfig>(new TalkTextReplaceConfig()),
                new ReactiveProperty<AppConfig>(new AppConfig()),
                new ReactiveProperty<UIConfig>(new UIConfig()),
                new ReactiveProperty<IAppStatus>(new AppStatus()))
        {
        }

        #endregion
    }
}
