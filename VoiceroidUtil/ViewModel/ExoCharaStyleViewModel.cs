using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.AviUtl.ExEdit;
using RucheHome.Util;
using RucheHome.Voiceroid;
using RucheHome.Windows.Media;
using VoiceroidUtil.Messaging;

namespace VoiceroidUtil.ViewModel
{
    /// <summary>
    /// ExoCharaStyle クラスに関する処理を提供する ViewModel クラス。
    /// </summary>
    public class ExoCharaStyleViewModel : ViewModelBase
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="value">設定の初期値。</param>
        /// <param name="uiConfig">UI設定値。</param>
        public ExoCharaStyleViewModel(
            ExoCharaStyle value,
            IReactiveProperty<UIConfig> uiConfig)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            // 修正可否
            this.CanModify =
                (new ReactiveProperty<bool>(true)).AddTo(this.CompositeDisposable);

            this.Value = value;

            // UI設定
            this.UIConfig = uiConfig;

            // 直近のアプリ状態値
            this.LastStatus =
                new ReactiveProperty<IAppStatus>(new AppStatus())
                    .AddTo(this.CompositeDisposable);

            // 内包 ViewModel のセットアップ
            this.SetupViewModels();

            // 直近のテキストスタイル雛形ファイルパス
            this.LastTemplateFilePath =
                (new ReactiveProperty<string>()).AddTo(this.CompositeDisposable);

            // テキストスタイル雛形保持フラグ
            this.HasTemplate =
                this
                    .ObserveProperty(self => self.Templates)
                    .Select(temps => temps.Count > 0)
                    .ToReadOnlyReactiveProperty()
                    .AddTo(this.CompositeDisposable);

            // テキストスタイル雛形用ファイルロード関連の非同期実行コマンドヘルパー作成
            // どちらか一方の実行中は両方実行不可とする
            var selectTemplateFileCommandExecuter =
                new AsyncCommandExecuter(this.ExecuteSelectTemplateFileCommand)
                    .AddTo(this.CompositeDisposable);
            var dropTemplateFileCommandExecuter =
                new AsyncCommandExecuter<DragEventArgs>(
                    this.ExecuteDropTemplateFileCommand)
                    .AddTo(this.CompositeDisposable);

            // テキストスタイル雛形用ファイルドラッグオーバーコマンド
            this.DragOverTemplateFileCommand =
                this.MakeCommand<DragEventArgs>(this.ExecuteDragOverTemplateFileCommand);

            // テキストスタイル雛形用ファイル選択コマンド
            this.SelectTemplateFileCommand =
                this.MakeAsyncCommand(
                    selectTemplateFileCommandExecuter,
                    dropTemplateFileCommandExecuter.ObserveExecutable());

            // テキストスタイル雛形用ファイルドロップコマンド
            this.DropTemplateFileCommand =
                this.MakeAsyncCommand(
                    dropTemplateFileCommandExecuter,
                    selectTemplateFileCommandExecuter.ObserveExecutable());

            // テキストスタイル雛形適用コマンド
            this.ApplyTemplateCommand =
                this.MakeCommand(
                    this.ExecuteApplyTemplateCommand,
                    this.CanModify,
                    selectTemplateFileCommandExecuter.ObserveExecutable(),
                    dropTemplateFileCommandExecuter.ObserveExecutable(),
                    this.HasTemplate,
                    this
                        .ObserveProperty(self => self.SelectedTemplateIndex)
                        .Select(i => i >= 0 && i < this.Templates.Count));
        }

        /// <summary>
        /// 設定値を取得または設定する。
        /// </summary>
        public ExoCharaStyle Value
        {
            get { return this.value; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (this.CanModify.Value)
                {
                    this.SetProperty(ref this.value, value);
                }
            }
        }
        private ExoCharaStyle value = null;

        /// <summary>
        /// 設定値を修正可能な状態であるか否かを取得する。
        /// </summary>
        /// <remarks>
        /// 既定では常に true を返す。外部からの設定以外で更新されることはない。
        /// </remarks>
        public ReactiveProperty<bool> CanModify { get; }

        /// <summary>
        /// UI設定値を取得する。
        /// </summary>
        public IReactiveProperty<UIConfig> UIConfig { get; }

        /// <summary>
        /// 直近のアプリ状態値を取得する。
        /// </summary>
        public ReactiveProperty<IAppStatus> LastStatus { get; }

        /// <summary>
        /// X座標の ViewModel を取得する。
        /// </summary>
        public MovableValueViewModel X { get; private set; }

        /// <summary>
        /// Y座標の ViewModel を取得する。
        /// </summary>
        public MovableValueViewModel Y { get; private set; }

        /// <summary>
        /// Z座標の ViewModel を取得する。
        /// </summary>
        public MovableValueViewModel Z { get; private set; }

        /// <summary>
        /// 拡大率の ViewModel を取得する。
        /// </summary>
        public MovableValueViewModel Scale { get; private set; }

        /// <summary>
        /// 透明度の ViewModel を取得する。
        /// </summary>
        public MovableValueViewModel Transparency { get; private set; }

        /// <summary>
        /// 回転角度の ViewModel を取得する。
        /// </summary>
        public MovableValueViewModel Rotation { get; private set; }

        /// <summary>
        /// フォントサイズの ViewModel を取得する。
        /// </summary>
        public MovableValueViewModel FontSize { get; private set; }

        /// <summary>
        /// 表示速度の ViewModel を取得する。
        /// </summary>
        public MovableValueViewModel TextSpeed { get; private set; }

        /// <summary>
        /// 音量の ViewModel を取得する。
        /// </summary>
        public MovableValueViewModel PlayVolume { get; private set; }

        /// <summary>
        /// 左右バランスの ViewModel を取得する。
        /// </summary>
        public MovableValueViewModel PlayBalance { get; private set; }

        /// <summary>
        /// 再生速度の ViewModel を取得する。
        /// </summary>
        public MovableValueViewModel PlaySpeed { get; private set; }

        /// <summary>
        /// フォントファミリ名列挙を取得する。
        /// </summary>
        public IEnumerable<string> FontFamilyNames => FontFamilyNameEnumerable.Current;

        /// <summary>
        /// テキストスタイル雛形コレクションを取得する。
        /// </summary>
        public ReadOnlyCollection<ExoTextStyleTemplate> Templates
        {
            get { return this.templates; }
            private set
            {
                this.SetProperty(
                    ref this.templates,
                    value ?? (new List<ExoTextStyleTemplate>()).AsReadOnly());

                if (this.Templates.Count > 0 && this.SelectedTemplateIndex < 0)
                {
                    // 強制的に選択状態にする
                    this.SelectedTemplateIndex = 0;
                }
            }
        }
        public ReadOnlyCollection<ExoTextStyleTemplate> templates =
            (new List<ExoTextStyleTemplate>()).AsReadOnly();

        /// <summary>
        /// 選択中のテキストスタイル雛形インデックスを取得する。
        /// </summary>
        public int SelectedTemplateIndex
        {
            get { return this.selectedTemplateIndex; }
            set { this.SetProperty(ref this.selectedTemplateIndex, value); }
        }
        private int selectedTemplateIndex = -1;

        /// <summary>
        /// 直近で読み取り成功したテキストスタイル雛形ファイルパスを取得する。
        /// </summary>
        public ReactiveProperty<string> LastTemplateFilePath { get; }

        /// <summary>
        /// テキストスタイル雛形が1つ以上あるか否かを取得する。
        /// </summary>
        public ReadOnlyReactiveProperty<bool> HasTemplate { get; }

        /// <summary>
        /// テキストスタイル雛形用ファイル選択コマンドを取得する。
        /// </summary>
        public ReactiveCommand SelectTemplateFileCommand { get; }

        /// <summary>
        /// テキストスタイル雛形用ファイルドラッグオーバーコマンドを取得する。
        /// </summary>
        public ReactiveCommand<DragEventArgs> DragOverTemplateFileCommand { get; }

        /// <summary>
        /// テキストスタイル雛形用ファイルドロップコマンドを取得する。
        /// </summary>
        public ReactiveCommand<DragEventArgs> DropTemplateFileCommand { get; }

        /// <summary>
        /// テキストスタイル雛形適用コマンドを取得する。
        /// </summary>
        public ReactiveCommand ApplyTemplateCommand { get; }

        /// <summary>
        /// IDataObject オブジェクトからファイルパスを取得する。
        /// </summary>
        /// <param name="data">IDataObject オブジェクト。</param>
        /// <returns>ファイルパス。取得できなければ null 。</returns>
        private static string GetFilePath(IDataObject data)
        {
            if (data == null || !data.GetDataPresent(DataFormats.FileDrop, true))
            {
                return null;
            }

            var pathes = data.GetData(DataFormats.FileDrop, true) as string[];
            if (pathes?.Length != 1)
            {
                // 複数ファイルドロップは不可とする
                return null;
            }

            return File.Exists(pathes[0]) ? pathes[0] : null;
        }

        /// <summary>
        /// レイヤーアイテム列挙からテキストスタイル雛形リストを作成する。
        /// </summary>
        /// <param name="layerItems">レイヤーアイテム列挙。</param>
        /// <param name="limitCount">雛形作成上限数。負数ならば上限なし。</param>
        /// <returns>テキストスタイル雛形リスト。</returns>
        private static List<ExoTextStyleTemplate> MakeTextStyleTemplates(
            IEnumerable<LayerItem> layerItems,
            int limitCount = -1)
        {
            var result = new List<ExoTextStyleTemplate>();
            if (limitCount == 0)
            {
                return result;
            }

            foreach (var item in layerItems.OrderByDescending(i => i.LayerId))
            {
                var render = item.GetComponent<RenderComponent>();
                var text = item.GetComponent<TextComponent>();
                if (render == null || text == null)
                {
                    continue;
                }

                var temp =
                    new ExoTextStyleTemplate
                    {
                        Render = render,
                        IsTextClipping = item.IsClipping,
                    };
                text.CopyTo(temp.Text);

                // スタイル設定しない項目(テキスト除く)は固定値にする
                temp.Text.IsAutoScrolling = false;
                temp.Text.IsAutoAdjusting = false;

                // 重複するなら無視
                if (result.Any(t => t.EqualsWithoutText(temp)))
                {
                    continue;
                }

                result.Add(temp);

                if (limitCount > 0 && result.Count >= limitCount)
                {
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// 内包 ViewModel のセットアップを行う。
        /// </summary>
        private void SetupViewModels()
        {
            var valueObs = this.ObserveProperty(self => self.Value);

            var render = this.Value.Render;
            var renderObs =
                valueObs.Select(config => config.ObserveProperty(c => c.Render)).Switch();
            {
                this.X = this.MakeMovableValueViewModel(render, renderObs, r => r.X);
                this.Y = this.MakeMovableValueViewModel(render, renderObs, r => r.Y);
                this.Z = this.MakeMovableValueViewModel(render, renderObs, r => r.Z);
                this.Scale =
                    this.MakeMovableValueViewModel(render, renderObs, r => r.Scale);
                this.Transparency =
                    this.MakeMovableValueViewModel(render, renderObs, r => r.Transparency);
                this.Rotation =
                    this.MakeMovableValueViewModel(render, renderObs, r => r.Rotation);

                // X, Y, Z の移動モード関連値は共通なので直近の設定値で上書き
                this.SynchronizeXYZProperty(c => c.MoveMode);
                this.SynchronizeXYZProperty(c => c.IsAccelerating);
                this.SynchronizeXYZProperty(c => c.IsDecelerating);
                this.SynchronizeXYZProperty(c => c.Interval);
            }

            var text = this.Value.Text;
            var textObs =
                valueObs.Select(config => config.ObserveProperty(c => c.Text)).Switch();
            {
                this.FontSize =
                    this.MakeMovableValueViewModel(text, textObs, t => t.FontSize);
                this.TextSpeed =
                    this.MakeMovableValueViewModel(text, textObs, t => t.TextSpeed);
            }

            var play = this.Value.Play;
            var playObs =
                valueObs.Select(config => config.ObserveProperty(c => c.Play)).Switch();
            {
                this.PlayVolume =
                    this.MakeMovableValueViewModel(play, playObs, p => p.Volume);
                this.PlayBalance =
                    this.MakeMovableValueViewModel(play, playObs, p => p.Balance);
            }

            this.PlaySpeed =
                this.MakeMovableValueViewModel(
                    this.Value,
                    valueObs,
                    v => v.PlaySpeed,
                    @"再生速度");
        }

        /// <summary>
        /// オブジェクトのプロパティから MovableValueViewModel を作成する。
        /// </summary>
        /// <typeparam name="T">
        /// オブジェクト型。
        /// INotifyPropertyChanged インタフェースを実装している必要がある。
        /// </typeparam>
        /// <param name="holder">オブジェクト。</param>
        /// <param name="holderObservable">オブジェクトのプッシュ通知。</param>
        /// <param name="selector">
        /// オブジェクトの IMovableValue プロパティセレクタ。
        /// </param>
        /// <param name="name">名前。 null ならば自動決定される。</param>
        /// <returns>MovableValueViewModel 。</returns>
        private MovableValueViewModel MakeMovableValueViewModel<T>(
            T holder,
            IObservable<T> holderObservable,
            Expression<Func<T, IMovableValue>> selector,
            string name = null)
            where T : INotifyPropertyChanged
        {
            Debug.Assert(holder != null);
            Debug.Assert(holderObservable != null);
            Debug.Assert(selector != null);

            // 初期値取得
            var value = selector.Compile()(holder);

            // 名前取得
            var info = ((MemberExpression)selector.Body).Member;
            name =
                name ??
                info.GetCustomAttribute<ExoFileItemAttribute>(true)?.Name ??
                info.Name;

            var result =
                (new MovableValueViewModel(name, value)).AddTo(this.CompositeDisposable);

            // プロパティ値変更時に ViewModel 内部値を差し替える
            holderObservable
                .Select(comp => comp.ObserveProperty(selector))
                .Switch()
                .Subscribe(v => result.Reset(v))
                .AddTo(this.CompositeDisposable);

            return result;
        }

        /// <summary>
        /// X, Y, Z のプロパティ値を同期させるための設定を行う。
        /// </summary>
        /// <typeparam name="T">プロパティ型。</typeparam>
        /// <param name="propertyGetter">プロパティ取得デリゲート。</param>
        private void SynchronizeXYZProperty<T>(
            Func<MovableValueViewModel, IReactiveProperty<T>> propertyGetter)
        {
            Debug.Assert(propertyGetter != null);

            var props = (new[] { this.X, this.Y, this.Z }).Select(c => propertyGetter(c));

            // いずれかの値が設定されるたびに各プロパティへ上書きする
            props
                .Merge()
                .Subscribe(
                    v =>
                    {
                        foreach (var p in props)
                        {
                            // 念のため同値チェックしておく
                            if (!EqualityComparer<T>.Default.Equals(p.Value, v))
                            {
                                p.Value = v;
                            }
                        }
                    })
                .AddTo(this.CompositeDisposable);
        }

        /// <summary>
        /// テキストスタイル雛形用ファイル単体のMB単位の最大許容サイズ。
        /// </summary>
        private const int TemplateFileSizeLimitMB = 20;

        /// <summary>
        /// 雛形作成上限数。
        /// </summary>
        private const int TemplateLimitCount = 20;

        /// <summary>
        /// ファイル内容を用いてテキストスタイル雛形コレクションを更新する。
        /// </summary>
        /// <param name="filePath">ファイルパス。</param>
        private async Task UpdateTextStyleTemplates(string filePath)
        {
            if (filePath == null)
            {
                return;
            }

            var info = new FileInfo(filePath);

            // ファイルサイズチェック
            if (info.Length > TemplateFileSizeLimitMB * 1024L * 1024)
            {
                this.SetLastStatus(
                    AppStatusType.Warning,
                    @"ファイルサイズが大きすぎます。",
                    subStatusText:
                        @"許容サイズは " + TemplateFileSizeLimitMB + @" MBまでです。");
                return;
            }

            // ファイル読み取り
            ExEditObject exo = null;
            try
            {
                exo = await ExoFileReaderWriter.ReadAsync(info);
            }
            catch (FormatException ex)
            {
                ThreadTrace.WriteException(ex);
                this.SetLastStatus(
                    AppStatusType.Fail,
                    @"ファイルの読み取りに失敗しました。",
                    subStatusText: @".exo ファイルではない可能性があります。");
                return;
            }
            catch
            {
                this.SetLastStatus(
                    AppStatusType.Fail,
                    @"ファイルの読み取りに失敗しました。");
                return;
            }

            // 読み取り成功時点で直近ファイルパス上書き
            this.LastTemplateFilePath.Value = Path.GetFullPath(filePath);

            // 雛形作成
            var temps =
                await Task.Run(
                    () => MakeTextStyleTemplates(exo.LayerItems, TemplateLimitCount));
            if (temps == null || temps.Count <= 0)
            {
                this.SetLastStatus(
                    AppStatusType.Warning,
                    @"ファイル内にテキストオブジェクトが存在しません。");
                return;
            }

            // プロパティ上書き
            this.SelectedTemplateIndex = (this.SelectedTemplateIndex < 0) ? -1 : 0;
            this.Templates = temps.AsReadOnly();

            this.SetLastStatus(
                AppStatusType.Success,
                @"ファイルの読み取りに成功しました。",
                subStatusText:
                    @"テキスト設定を " +
                    this.Templates.Count +
                    @" 件ロードしました。");
        }

        /// <summary>
        /// SelectTemplateFileCommand の実処理を行う。
        /// </summary>
        private async Task ExecuteSelectTemplateFileCommand()
        {
            // メッセージ送信
            var msg =
                await this.Messenger.GetResponseAsync(
                    new OpenFileDialogMessage
                    {
                        Title = @"テキスト設定用ファイルの選択",
                        InitialDirectory =
                            (this.LastTemplateFilePath.Value == null) ?
                                null :
                                Path.GetDirectoryName(this.LastTemplateFilePath.Value),
                        Filters =
                            new List<CommonFileDialogFilter>
                            {
                                new CommonFileDialogFilter(
                                    @"AviUtl拡張編集ファイル",
                                    @"exo"),
                                new CommonFileDialogFilter(@"すべてのファイル", @"*"),
                            },
                    });

            // 選択された？
            if (msg.Response != null)
            {
                // 雛形更新
                await this.UpdateTextStyleTemplates(msg.Response);
            }
        }

        /// <summary>
        /// DragOverTemplateFileCommand コマンドの実処理を行う。
        /// </summary>
        /// <param name="e">ドラッグイベントデータ。</param>
        private void ExecuteDragOverTemplateFileCommand(DragEventArgs e)
        {
            if (GetFilePath(e?.Data) != null)
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
        }

        /// <summary>
        /// DropTemplateFileCommand コマンドの実処理を行う。
        /// </summary>
        /// <param name="e">ドラッグイベントデータ。</param>
        private async Task ExecuteDropTemplateFileCommand(DragEventArgs e)
        {
            var path = GetFilePath(e?.Data);
            if (path == null)
            {
                return;
            }

            e.Handled = true;

            await this.UpdateTextStyleTemplates(path);
        }

        /// <summary>
        /// ApplyTemplateCommand コマンドの実処理を行う。
        /// </summary>
        private void ExecuteApplyTemplateCommand()
        {
            var index = this.SelectedTemplateIndex;
            if (index < 0 || index >= this.Templates.Count)
            {
                return;
            }

            this.Templates[index].CopyTo(this.Value, withoutText: true);

            this.SetLastStatus(
                AppStatusType.Success,
                @"テキストオブジェクトの設定を適用しました。");
        }

        /// <summary>
        /// 直近のアプリ状態をリセットする。
        /// </summary>
        private void ResetLastStatus()
        {
            this.LastStatus.Value = new AppStatus();
        }

        /// <summary>
        /// 直近のアプリ状態を設定する。
        /// </summary>
        /// <param name="statusType">状態種別。</param>
        /// <param name="statusText">状態テキスト。</param>
        /// <param name="subStatusType">オプショナルなサブ状態種別。</param>
        /// <param name="subStatusText">オプショナルなサブ状態テキスト。</param>
        /// <param name="subStatusCommand">オプショナルなサブ状態コマンド。</param>
        private void SetLastStatus(
            AppStatusType statusType = AppStatusType.None,
            string statusText = "",
            AppStatusType subStatusType = AppStatusType.None,
            string subStatusText = "",
            string subStatusCommand = "")
        {
            this.LastStatus.Value =
                new AppStatus
                {
                    StatusType = statusType,
                    StatusText = statusText ?? "",
                    SubStatusType = subStatusType,
                    SubStatusText = subStatusText ?? "",
                    SubStatusCommand = subStatusCommand ?? "",
                };
        }

        #region デザイン用定義

        /// <summary>
        /// デザイン用のコンストラクタ。
        /// </summary>
        [Obsolete(@"Can use only design time.", true)]
        public ExoCharaStyleViewModel()
            :
            this(
                new ExoCharaStyle(VoiceroidId.YukariEx),
                new ReactiveProperty<UIConfig>(new UIConfig()))
        {
            this.UIConfig.AddTo(this.CompositeDisposable);
        }

        #endregion
    }
}
