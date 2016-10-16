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
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.AviUtl.ExEdit;
using RucheHome.Util;
using RucheHome.Windows.Media;
using VoiceroidUtil.Extensions;
using VoiceroidUtil.Services;

namespace VoiceroidUtil.ViewModel
{
    /// <summary>
    /// ExoCharaStyle クラスに関する処理を提供する ViewModel クラス。
    /// </summary>
    public class ExoCharaStyleViewModel : ConfigViewModelBase<ExoCharaStyle>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="canModify">
        /// 再生や音声保存に関わる設定値の変更可否状態値。
        /// </param>
        /// <param name="charaStyle">ExoCharaStyle 値。</param>
        /// <param name="uiConfig">UI設定値。</param>
        /// <param name="lastStatus">直近のアプリ状態値の設定先。</param>
        /// <param name="openFileDialogService">ファイル選択ダイアログサービス。</param>
        public ExoCharaStyleViewModel(
            IReadOnlyReactiveProperty<bool> canModify,
            IReadOnlyReactiveProperty<ExoCharaStyle> charaStyle,
            IReadOnlyReactiveProperty<UIConfig> uiConfig,
            IReactiveProperty<IAppStatus> lastStatus,
            IOpenFileDialogService openFileDialogService)
            : base(canModify, charaStyle)
        {
            this.ValidateArgNull(uiConfig, nameof(uiConfig));
            this.ValidateArgNull(lastStatus, nameof(lastStatus));
            this.ValidateArgNull(openFileDialogService, nameof(openFileDialogService));

            this.LastStatus = lastStatus;
            this.OpenFileDialogService = openFileDialogService;

            // 内包 ViewModel のセットアップ
            this.SetupViewModels();

            // テキストスタイル雛形コレクション
            this.Templates =
                new ReactiveProperty<ReadOnlyCollection<ExoTextStyleTemplate>>(
                    new List<ExoTextStyleTemplate>().AsReadOnly())
                    .AddTo(this.CompositeDisposable);

            // 選択中テキストスタイル雛形インデックス
            this.SelectedTemplateIndex =
                new ReactiveProperty<int>(-1).AddTo(this.CompositeDisposable);

            // 直近のテキストスタイル雛形ファイルパス
            this.LastTemplateFilePath =
                new ReactiveProperty<string>().AddTo(this.CompositeDisposable);

            // テキストスタイル雛形保持フラグ
            this.HasTemplate =
                this.Templates
                    .Select(temps => temps.Count > 0)
                    .ToReadOnlyReactiveProperty(false)
                    .AddTo(this.CompositeDisposable);

            // コレクションが空でなくなったらアイテム選択
            this.HasTemplate
                .Where(f => f && this.SelectedTemplateIndex.Value < 0)
                .Subscribe(_ => this.SelectedTemplateIndex.Value = 0)
                .AddTo(this.CompositeDisposable);

            // UI開閉設定
            this.IsTextUIExpanded =
                uiConfig
                    .MakeInnerReactivePropery(c => c.IsExoCharaTextExpanded)
                    .AddTo(this.CompositeDisposable);
            this.IsAudioUIExpanded =
                uiConfig
                    .MakeInnerReactivePropery(c => c.IsExoCharaAudioExpanded)
                    .AddTo(this.CompositeDisposable);
            this.IsTextImportUIExpanded =
                uiConfig
                    .MakeInnerReactivePropery(c => c.IsExoCharaTextImportExpanded)
                    .AddTo(this.CompositeDisposable);

            // テキストスタイル雛形用ファイルロード関連の非同期実行コマンドヘルパー作成
            // どちらか一方の実行中は両方実行不可とする
            var selectTemplateFileCommandExecuter =
                new AsyncCommandExecuter(this.ExecuteSelectTemplateFileCommand);
            var dropTemplateFileCommandExecuter =
                new AsyncCommandExecuter<DragEventArgs>(
                    this.ExecuteDropTemplateFileCommand);

            // テキストスタイル雛形用ファイル選択コマンド
            this.SelectTemplateFileCommand =
                this.MakeAsyncCommand(
                    selectTemplateFileCommandExecuter,
                    dropTemplateFileCommandExecuter.IsExecutable);

            // テキストスタイル雛形用ファイルドラッグオーバーコマンド
            this.DragOverTemplateFileCommand =
                this.MakeCommand<DragEventArgs>(
                    this.ExecuteDragOverTemplateFileCommand,
                    selectTemplateFileCommandExecuter.IsExecutable,
                    dropTemplateFileCommandExecuter.IsExecutable);

            // テキストスタイル雛形用ファイルドロップコマンド
            this.DropTemplateFileCommand =
                this.MakeAsyncCommand(
                    dropTemplateFileCommandExecuter,
                    selectTemplateFileCommandExecuter.IsExecutable);

            // テキストスタイル雛形適用コマンド
            this.ApplyTemplateCommand =
                this.MakeCommand(
                    this.ExecuteApplyTemplateCommand,
                    this.CanModify,
                    selectTemplateFileCommandExecuter.IsExecutable,
                    dropTemplateFileCommandExecuter.IsExecutable,
                    this.HasTemplate,
                    this.SelectedTemplateIndex
                        .Select(i => i >= 0 && i < this.Templates.Value.Count));
        }

        /// <summary>
        /// ExoCharaStyle 値を取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<ExoCharaStyle> CharaStyle => this.BaseConfig;

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
        public IReactiveProperty<ReadOnlyCollection<ExoTextStyleTemplate>> Templates
        {
            get;
        }

        /// <summary>
        /// 選択中のテキストスタイル雛形インデックスを取得する。
        /// </summary>
        public IReactiveProperty<int> SelectedTemplateIndex { get; }

        /// <summary>
        /// 直近で読み取り成功したテキストスタイル雛形ファイルパスを取得する。
        /// </summary>
        public IReactiveProperty<string> LastTemplateFilePath { get; }

        /// <summary>
        /// テキストスタイル雛形が1つ以上あるか否かを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<bool> HasTemplate { get; }

        /// <summary>
        /// テキスト設定UIを開いた状態にするか否かを取得する。
        /// </summary>
        public IReactiveProperty<bool> IsTextUIExpanded { get; }

        /// <summary>
        /// 音声設定UIを開いた状態にするか否かを取得する。
        /// </summary>
        public IReactiveProperty<bool> IsAudioUIExpanded { get; }

        /// <summary>
        /// テキストインポート設定UIを開いた状態にするか否かを取得する。
        /// </summary>
        public IReactiveProperty<bool> IsTextImportUIExpanded { get; }

        /// <summary>
        /// テキストスタイル雛形用ファイル選択コマンドを取得する。
        /// </summary>
        public ICommand SelectTemplateFileCommand { get; }

        /// <summary>
        /// テキストスタイル雛形用ファイルドラッグオーバーコマンドを取得する。
        /// </summary>
        public ICommand DragOverTemplateFileCommand { get; }

        /// <summary>
        /// テキストスタイル雛形用ファイルドロップコマンドを取得する。
        /// </summary>
        public ICommand DropTemplateFileCommand { get; }

        /// <summary>
        /// テキストスタイル雛形適用コマンドを取得する。
        /// </summary>
        public ICommand ApplyTemplateCommand { get; }

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
        /// 直近のアプリ状態値の設定先を取得する。
        /// </summary>
        private IReactiveProperty<IAppStatus> LastStatus { get; }

        /// <summary>
        /// ファイル選択ダイアログサービスを取得する。
        /// </summary>
        private IOpenFileDialogService OpenFileDialogService { get; }

        /// <summary>
        /// 内包 ViewModel のセットアップを行う。
        /// </summary>
        private void SetupViewModels()
        {
            var render = this.CharaStyle.Value.Render;
            var renderObs = this.ObserveConfigProperty(c => c.Render);
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

            var text = this.CharaStyle.Value.Text;
            var textObs = this.ObserveConfigProperty(c => c.Text);
            {
                this.FontSize =
                    this.MakeMovableValueViewModel(text, textObs, t => t.FontSize);
                this.TextSpeed =
                    this.MakeMovableValueViewModel(text, textObs, t => t.TextSpeed);
            }

            var play = this.CharaStyle.Value.Play;
            var playObs = this.ObserveConfigProperty(c => c.Play);
            {
                this.PlayVolume =
                    this.MakeMovableValueViewModel(play, playObs, p => p.Volume);
                this.PlayBalance =
                    this.MakeMovableValueViewModel(play, playObs, p => p.Balance);
            }

            this.PlaySpeed =
                this.MakeMovableValueViewModel(
                    this.CharaStyle.Value,
                    this.CharaStyle,
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

            // 値取得
            var initialValue = selector.Compile()(holder);
            var value =
                holderObservable
                    .ObserveInnerProperty(selector)
                    .ToReadOnlyReactiveProperty(initialValue)
                    .AddTo(this.CompositeDisposable);

            // 名前取得
            var info = ((MemberExpression)selector.Body).Member;
            name =
                name ??
                info.GetCustomAttribute<ExoFileItemAttribute>(true)?.Name ??
                info.Name;

            return
                new MovableValueViewModel(this.CanModify, value, name)
                    .AddTo(this.CompositeDisposable);
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
        /// テキストスタイル雛形作成上限数。
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
            this.Templates.Value = temps.AsReadOnly();
            this.SelectedTemplateIndex.Value = 0;

            this.SetLastStatus(
                AppStatusType.Success,
                @"ファイルの読み取りに成功しました。",
                subStatusText:
                    @"テキスト設定を " +
                    this.Templates.Value.Count +
                    @" 件ロードしました。");
        }

        /// <summary>
        /// SelectTemplateFileCommand の実処理を行う。
        /// </summary>
        private async Task ExecuteSelectTemplateFileCommand()
        {
            // ダイアログ処理
            var filePath =
                await this.OpenFileDialogService.Run(
                    title:
                        @"テキスト設定用 .exo ファイルの選択",
                    initialDirectory:
                        Path.GetDirectoryName(this.LastTemplateFilePath.Value),
                    filters:
                        new[]
                        {
                            new CommonFileDialogFilter(@"AviUtl拡張編集ファイル", @"exo"),
                            new CommonFileDialogFilter(@"すべてのファイル", @"*"),
                        });

            // 選択された？
            if (filePath != null)
            {
                // 雛形更新
                await this.UpdateTextStyleTemplates(filePath);
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
            var index = this.SelectedTemplateIndex.Value;
            if (index < 0 || index >= this.Templates.Value.Count)
            {
                return;
            }

            this.Templates.Value[index].CopyTo(this.CharaStyle.Value, withoutText: true);

            this.SetLastStatus(
                AppStatusType.Success,
                @"テキストオブジェクトの設定を適用しました。");
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
    }
}
