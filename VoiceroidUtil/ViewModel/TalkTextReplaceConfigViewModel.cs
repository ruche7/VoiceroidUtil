using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.Util;

namespace VoiceroidUtil.ViewModel
{
    /// <summary>
    /// トークテキスト置換設定とそれに対する処理を提供する ViewModel クラス。
    /// </summary>
    public class TalkTextReplaceConfigViewModel : Livet.ViewModel
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public TalkTextReplaceConfigViewModel()
        {
            this.ConfigKeeper.Value = new TalkTextReplaceConfig();

            // 設定
            this.AppConfig =
                new ReactiveProperty<AppConfig>(new AppConfig())
                    .AddTo(this.CompositeDisposable);
            this.UIConfig =
                new ReactiveProperty<UIConfig>(new UIConfig())
                    .AddTo(this.CompositeDisposable);

            // 修正可否
            this.CanModify =
                (new ReactiveProperty<bool>(true)).AddTo(this.CompositeDisposable);

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

            // ロード実施済みフラグ
            this.IsConfigLoaded =
                (new ReactiveProperty<bool>(false)).AddTo(this.CompositeDisposable);

            // ロードコマンド
            this.LoadCommand =
                this.CanModify.ToReactiveCommand().AddTo(this.CompositeDisposable);
            this.LoadCommand
                .Subscribe(async _ => await this.ExecuteLoadCommand())
                .AddTo(this.CompositeDisposable);

            // セーブ要求 Subject
            // 100ms の間、次のセーブ要求が来なければ実際のセーブ処理を行う
            this.SaveRequestSubject =
                (new Subject<object>()).AddTo(this.CompositeDisposable);
            this.SaveRequestSubject
                .Throttle(TimeSpan.FromMilliseconds(100))
                .Subscribe(_ => this.ConfigKeeper.Save())
                .AddTo(this.CompositeDisposable);

            // セーブコマンド
            this.SaveCommand =
                this.IsConfigLoaded.ToReactiveCommand().AddTo(this.CompositeDisposable);
            this.SaveCommand
                .Subscribe(_ => this.ExecuteSaveCommand())
                .AddTo(this.CompositeDisposable);
        }

        /// <summary>
        /// トークテキスト置換設定値を取得または設定する。
        /// </summary>
        public TalkTextReplaceConfig Value
        {
            get { return this.ConfigKeeper.Value; }
            set
            {
                var old = this.Value;
                this.ConfigKeeper.Value = value ?? (new TalkTextReplaceConfig());
                if (this.Value != old)
                {
                    this.RaisePropertyChanged();
                }
            }
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
        /// 設定値を修正可能な状態であるか否かを取得する。
        /// </summary>
        /// <remarks>
        /// 既定では常に true を返す。外部からの設定以外で更新されることはない。
        /// </remarks>
        public ReactiveProperty<bool> CanModify { get; }

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
        /// トークテキスト置換設定ロードコマンドを取得する。
        /// </summary>
        public ReactiveCommand LoadCommand { get; }

        /// <summary>
        /// トークテキスト置換設定セーブコマンドを取得する。
        /// </summary>
        public ReactiveCommand SaveCommand { get; }

        /// <summary>
        /// トークテキスト置換設定の保持と読み書きを行うオブジェクトを取得する。
        /// </summary>
        private ConfigKeeper<TalkTextReplaceConfig> ConfigKeeper { get; } =
            new ConfigKeeper<TalkTextReplaceConfig>(nameof(VoiceroidUtil));

        /// <summary>
        /// トークテキスト置換設定のロードが1回以上行われたか否かを取得する。
        /// </summary>
        private ReactiveProperty<bool> IsConfigLoaded { get; }

        /// <summary>
        /// トークテキスト置換設定セーブ処理要求 Subject を取得する。
        /// </summary>
        private Subject<object> SaveRequestSubject { get; }

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

        /// <summary>
        /// LoadCommand の実処理を行う。
        /// </summary>
        private async Task ExecuteLoadCommand()
        {
            // 成否に関わらずロード実施済みとする
            // プロパティ変更通知によりセーブコマンドが発行される場合があるため、
            // ロード処理よりも前に立てておく
            this.IsConfigLoaded.Value = true;

            if (await Task.Run(() => this.ConfigKeeper.Load()))
            {
                this.RaisePropertyChanged(nameof(this.Value));
            }
            else
            {
                // ロードに失敗した場合は現在値をセーブしておく
                this.ExecuteSaveCommand();
            }
        }

        /// <summary>
        /// SaveCommand の実処理を行う。
        /// </summary>
        private void ExecuteSaveCommand()
        {
            // 1回以上 LoadCommand が実施されていなければ処理しない
            if (!this.IsConfigLoaded.Value)
            {
                return;
            }

            // セーブ要求
            this.SaveRequestSubject.OnNext(null);
        }
    }
}
