using System;
using System.Linq;
using System.Reactive.Linq;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

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
        public TalkTextReplaceConfigViewModel() : base(new TalkTextReplaceConfig())
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
            this.SetupViewModels();

            // テキストファイル作成設定有効化コマンド
            this.IsTextFileForceMakingCommandVisible =
                this.AppConfig
                    .Select(
                        config =>
                            (config == null) ?
                                Observable.Return(false) :
                                config
                                    .ObserveProperty(c => c.IsTextFileForceMaking)
                                    .Select(f => !f))
                    .Switch()
                    .ToReadOnlyReactiveProperty()
                    .AddTo(this.CompositeDisposable);
            this.TextFileForceMakingCommand =
                new IObservable<bool>[]
                {
                    this.CanModify,
                    this.IsTextFileForceMakingCommandVisible,
                }
                .CombineLatestValuesAreAllTrue()
                .ToReactiveCommand()
                .AddTo(this.CompositeDisposable);
            this.TextFileForceMakingCommand
                .Subscribe(_ => this.ExecuteTextFileForceMakingCommand())
                .AddTo(this.CompositeDisposable);
        }

        /// <summary>
        /// アプリ設定を取得する。
        /// </summary>
        /// <remarks>
        /// テキストファイル保存設定の操作にのみ用いる。
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
        /// 音声のトークテキスト置換アイテムコレクション ViewModel を取得する。
        /// </summary>
        public TalkTextReplaceItemsViewModel VoiceReplaceItems { get; } =
            new TalkTextReplaceItemsViewModel();

        /// <summary>
        /// テキストファイルのトークテキスト置換アイテムコレクション ViewModel を取得する。
        /// </summary>
        public TalkTextReplaceItemsViewModel TextFileReplaceItems { get; } =
            new TalkTextReplaceItemsViewModel();

        /// <summary>
        /// テキストファイル作成設定有効化コマンドを表示可能か否かを取得する。
        /// </summary>
        public ReadOnlyReactiveProperty<bool> IsTextFileForceMakingCommandVisible { get; }

        /// <summary>
        /// テキストファイル作成設定有効化コマンドを取得する。
        /// </summary>
        public ReactiveCommand TextFileForceMakingCommand { get; }

        /// <summary>
        /// 内包 ViewModel のセットアップを行う。
        /// </summary>
        private void SetupViewModels()
        {
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

            // トークテキスト置換アイテムコレクション反映デリゲート
            Action<TalkTextReplaceConfig> itemsSetter =
                c =>
                {
                    this.VoiceReplaceItems.Items = c?.VoiceReplaceItems;
                    this.TextFileReplaceItems.Items = c?.TextFileReplaceItems;
                };

            // 現在値を反映
            itemsSetter(this.Value);

            // トークテキスト置換設定変更時に反映させる
            this
                .ObserveProperty(self => self.Value)
                .Subscribe(c => itemsSetter(c))
                .AddTo(this.CompositeDisposable);
        }

        /// <summary>
        /// TextFileForceMakingCommand の実処理を行う。
        /// </summary>
        private void ExecuteTextFileForceMakingCommand()
        {
            if (
                !this.CanModify.Value ||
                this.AppConfig.Value?.IsTextFileForceMaking != false)
            {
                return;
            }

            // 設定有効化
            this.AppConfig.Value.IsTextFileForceMaking = true;

            // ステータス更新
            this.LastStatus.Value =
                new AppStatus
                {
                    StatusType = AppStatusType.Success,
                    StatusText = @"テキストファイル作成設定を有効にしました。",
                };
        }
    }
}
