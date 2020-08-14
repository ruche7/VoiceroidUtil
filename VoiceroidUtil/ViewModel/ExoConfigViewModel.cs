using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using VoiceroidUtil.Extensions;
using VoiceroidUtil.Services;
using static RucheHome.Util.ArgumentValidater;

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
            ValidateArgumentNull(appConfig, nameof(appConfig));
            ValidateArgumentNull(uiConfig, nameof(uiConfig));
            ValidateArgumentNull(lastStatus, nameof(lastStatus));
            ValidateArgumentNull(openFileDialogService, nameof(openFileDialogService));

            this.AppConfig = appConfig;
            this.LastStatus = lastStatus;

            // 選択中タブインデックス
            this.SelectedTabIndex =
                this.MakeInnerPropertyOf(uiConfig, c => c.ExoConfigTabIndex);

            // 共通設定
            this.Common = this.MakeConfigProperty(c => c.Common);

            // キャラ別スタイル設定コレクション
            var charaStyles =
                this.MakeReadOnlyConfigProperty(
                    c => c.CharaStyles,
                    notifyOnSameValue: true);

            // 表示状態のキャラ別スタイル設定コレクション
            this.VisibleCharaStyles =
                Observable
                    .CombineLatest(
                        appConfig.ObserveInnerProperty(c => c.VoiceroidVisibilities),
                        charaStyles.Select(s => s.Count()).DistinctUntilChanged(),
                        (vv, _) => vv.SelectVisibleOf(charaStyles.Value))
                    .ToReadOnlyReactiveProperty()
                    .AddTo(this.CompositeDisposable);

            // キャラ別スタイル設定選択コマンド実行可能状態
            // 表示状態のキャラ別スタイル設定が2つ以上あれば選択可能
            this.IsSelectCharaStyleCommandExecutable =
                this.VisibleCharaStyles
                    .Select(vcs => vcs.Count >= 2)
                    .ToReadOnlyReactiveProperty()
                    .AddTo(this.CompositeDisposable);

            // キャラ別スタイル設定選択コマンドのチップテキスト
            this.SelectCharaStyleCommandTip =
                this.VisibleCharaStyles
                    .Select(_ => this.MakeSelectCharaStyleCommandTip())
                    .ToReadOnlyReactiveProperty()
                    .AddTo(this.CompositeDisposable);

            // 最適表示列数
            // 6キャラ単位で列数を増やす
            this.VisibleCharaStylesColumnCount =
                this.VisibleCharaStyles
                    .Select(vp => Math.Min(Math.Max(1, (vp.Count + 5) / 6), 3))
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
                this.MakeInnerReadOnlyPropertyOf(this.AppConfig, c => c.IsExoFileMaking);
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

            // キャラ別スタイル設定選択コマンドコレクション(要素数 10 固定)
            this.SelectCharaStyleCommands =
                new ReadOnlyCollection<ICommand>(
                    Enumerable.Range(0, 10)
                        .Select(
                            index =>
                                this.MakeCommand(
                                    () => this.ExecuteSelectCharaStyleCommand(index),
                                    this.IsSelectCharaStyleCommandExecutable,
                                    this.VisibleCharaStyles
                                        .Select(vcs => index < vcs.Count)
                                        .DistinctUntilChanged()))
                        .ToArray());

            // 前方/後方キャラ別スタイル設定選択コマンド
            this.SelectPreviousCharaStyleCommand =
                this.MakeCommand(
                    this.ExecuteSelectPreviousCharaStyleCommand,
                    this.IsSelectCharaStyleCommandExecutable);
            this.SelectNextCharaStyleCommand =
                this.MakeCommand(
                    this.ExecuteSelectNextCharaStyleCommand,
                    this.IsSelectCharaStyleCommandExecutable);
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
        /// キャラ別スタイル設定選択コマンドを実行可能な状態であるか否かを取得する。
        /// </summary>
        /// <remarks>
        /// (VisibleCharaStyles.Value.Count >= 2) の判定結果を返す。
        /// </remarks>
        public IReadOnlyReactiveProperty<bool> IsSelectCharaStyleCommandExecutable { get; }

        /// <summary>
        /// キャラ別スタイル設定選択コマンドのチップテキストを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<string> SelectCharaStyleCommandTip { get; }

        /// <summary>
        /// 表示状態のキャラ別スタイル設定コレクションの最適表示列数を取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<int> VisibleCharaStylesColumnCount { get; }

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
        /// キャラ別スタイル設定選択コマンドコレクションを取得する。
        /// </summary>
        /// <remarks>
        /// 要素数は 10 固定。表示中キャラ別スタイルのインデックスに対応する。
        /// </remarks>
        public ReadOnlyCollection<ICommand> SelectCharaStyleCommands { get; }

        /// <summary>
        /// 前方キャラ別スタイル設定選択コマンドを取得する。
        /// </summary>
        public ICommand SelectPreviousCharaStyleCommand { get; }

        /// <summary>
        /// 後方キャラ別スタイル設定選択コマンドを取得する。
        /// </summary>
        public ICommand SelectNextCharaStyleCommand { get; }

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
                    uiConfig
                        .ObserveInnerProperty(c => c.ExoCharaVoiceroidId)
                        .DistinctUntilChanged(),
                    (vcs, id) => vcs.FirstOrDefault(s => s.VoiceroidId == id) ?? vcs.First())
                .DistinctUntilChanged()
                .Subscribe(s => this.SelectedCharaStyle.Value = s)
                .AddTo(this.CompositeDisposable);

            // 選択中キャラ別スタイル変更時処理
            this.SelectedCharaStyle
                .Where(s => s != null)
                .Subscribe(s => uiConfig.Value.ExoCharaVoiceroidId = s.VoiceroidId)
                .AddTo(this.CompositeDisposable);
            this.SelectedCharaStyle
                .Where(s => s == null)
                .ObserveOnUIDispatcher()
                .Subscribe(
                    _ =>
                        this.SelectedCharaStyle.Value =
                            this.BaseConfig.Value.CharaStyles.First(
                                s => s.VoiceroidId == uiConfig.Value.ExoCharaVoiceroidId))
                .AddTo(this.CompositeDisposable);
        }

        /// <summary>
        /// キャラ別スタイル設定選択コマンドのチップテキストを作成する。
        /// </summary>
        /// <returns>チップテキスト。表示不要ならば null 。</returns>
        private string MakeSelectCharaStyleCommandTip() =>
            !this.IsSelectCharaStyleCommandExecutable.Value ?
                null :
                @"F1/F2 : 前/次のキャラを選択" + Environment.NewLine +
                string.Join(
                    Environment.NewLine,
                    this.VisibleCharaStyles.Value
                        .Take(10)
                        .Select(
                            (p, i) =>
                                @"Ctrl+" + ((i < 9) ? (i + 1) : 0) + @" : " +
                                p.VoiceroidName + @" を選択"));

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

        /// <summary>
        /// 各 SelectCharaStyleCommands コマンドの実処理を行う。
        /// </summary>
        /// <param name="index">インデックス。</param>
        private void ExecuteSelectCharaStyleCommand(int index)
        {
            if (
                this.IsSelectCharaStyleCommandExecutable.Value &&
                index < this.VisibleCharaStyles.Value.Count)
            {
                this.SelectedCharaStyle.Value = this.VisibleCharaStyles.Value[index];
            }
        }

        /// <summary>
        /// SelectPreviousCharaStyleCommand コマンドの実処理を行う。
        /// </summary>
        private void ExecuteSelectPreviousCharaStyleCommand()
        {
            var index =
                Array.IndexOf(
                    this.VisibleCharaStyles.Value.Select(p => p.VoiceroidId).ToArray(),
                    this.SelectedCharaStyle.Value.VoiceroidId);
            if (index >= 0)
            {
                --index;
                this.ExecuteSelectCharaStyleCommand(
                    (index < 0) ? (this.VisibleCharaStyles.Value.Count - 1) : index);
            }
        }

        /// <summary>
        /// SelectNextCharaStyleCommand コマンドの実処理を行う。
        /// </summary>
        private void ExecuteSelectNextCharaStyleCommand()
        {
            var index =
                Array.IndexOf(
                    this.VisibleCharaStyles.Value.Select(p => p.VoiceroidId).ToArray(),
                    this.SelectedCharaStyle.Value.VoiceroidId);
            if (index >= 0)
            {
                ++index;
                this.ExecuteSelectCharaStyleCommand(
                    (index < this.VisibleCharaStyles.Value.Count) ? index : 0);
            }
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
