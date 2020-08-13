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
using RucheHome.AviUtl;
using RucheHome.AviUtl.ExEdit;
using RucheHome.Util;
using RucheHome.Voiceroid;
using VoiceroidUtil.Services;
using static RucheHome.Util.ArgumentValidater;

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
            ValidateArgumentNull(uiConfig, nameof(uiConfig));
            ValidateArgumentNull(lastStatus, nameof(lastStatus));
            ValidateArgumentNull(openFileDialogService, nameof(openFileDialogService));

            this.LastStatus = lastStatus;
            this.OpenFileDialogService = openFileDialogService;

            // 各プロパティ値
            this.Render = this.MakeConfigProperty(c => c.Render);
            this.Text = this.MakeConfigProperty(c => c.Text);
            this.IsTextClipping = this.MakeConfigProperty(c => c.IsTextClipping);
            this.Play = this.MakeConfigProperty(c => c.Play);

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
            var tempIndexNotifier =
                Observable
                    .CombineLatest(
                        this.HasTemplate,
                        this.SelectedTemplateIndex,
                        (hasTemp, index) => new { hasTemp, index })
                    .DistinctUntilChanged();
            Observable
                .Zip(tempIndexNotifier, tempIndexNotifier.Skip(1))
                .Where(v => !v[0].hasTemp && v[1].hasTemp && v[1].index < 0)
                .Subscribe(_ => this.SelectedTemplateIndex.Value = 0)
                .AddTo(this.CompositeDisposable);

            // UI開閉設定
            this.IsTextUIExpanded =
                this.MakeInnerPropertyOf(uiConfig, c => c.IsExoCharaTextExpanded);
            this.IsAudioUIExpanded =
                this.MakeInnerPropertyOf(uiConfig, c => c.IsExoCharaAudioExpanded);
            this.IsTextImportUIExpanded =
                this.MakeInnerPropertyOf(uiConfig, c => c.IsExoCharaTextImportExpanded);

            // テキストスタイル雛形用ファイルのロード可能状態
            var templateLoadable = new ReactiveProperty<bool>(true);

            // テキストスタイル雛形ロード中フラグ
            this.IsTemplateLoading =
                templateLoadable
                    .Inverse()
                    .ToReadOnlyReactiveProperty()
                    .AddTo(this.CompositeDisposable);

            // テキストスタイル雛形用ファイル選択コマンド
            // 実施中はファイルロード不可にする
            this.SelectTemplateFileCommand =
                this.MakeSharedAsyncCommand(
                    templateLoadable,
                    this.ExecuteSelectTemplateFileCommand);

            // テキストスタイル雛形用ファイルドラッグオーバーコマンド
            this.DragOverTemplateFileCommand =
                this.MakeCommand<DragEventArgs>(
                    this.ExecuteDragOverTemplateFileCommand,
                    templateLoadable);

            // テキストスタイル雛形用ファイルドロップコマンド
            // 実施中はファイルロード不可にする
            this.DropTemplateFileCommand =
                this.MakeSharedAsyncCommand<DragEventArgs>(
                    templateLoadable,
                    this.ExecuteDropTemplateFileCommand);

            // テキストスタイル雛形適用コマンド
            this.ApplyTemplateCommand =
                this.MakeCommand(
                    this.ExecuteApplyTemplateCommand,
                    this.CanModify,
                    this.HasTemplate,
                    this.SelectedTemplateIndex
                        .Select(i => i >= 0 && i < this.Templates.Value.Count),
                    templateLoadable);
        }

        /// <summary>
        /// RenderComponent 値を取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<RenderComponent> Render { get; }

        /// <summary>
        /// TextComponent 値を取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<TextComponent> Text { get; }

        /// <summary>
        /// テキストを1つ上のオブジェクトでクリッピングするか否かを取得する。
        /// </summary>
        public IReactiveProperty<bool> IsTextClipping { get; }

        /// <summary>
        /// PlayComponent 値を取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<PlayComponent> Play { get; }

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
        public IEnumerable<string> FontFamilyNames { get; } = new FontFamilyNameEnumerable();

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
        /// テキストスタイル雛形のロード中であるか否かを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsTemplateLoading { get; }

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
        /// レイヤーアイテム内にキャラスクリプト用定義が存在するか否かを取得する。
        /// </summary>
        /// <param name="layerItem">レイヤーアイテム。</param>
        /// <returns>
        /// キャラスクリプト用定義が存在するならば true 。そうでなければ false 。
        /// </returns>
        private static bool ContainsCharaScript(LayerItem layerItem) =>
            layerItem
                .GetComponents<UnknownComponent>()
                .Any(
                    c =>
                        c.ComponentName == @"アニメーション効果" &&
                        c.Items.Any(
                            i => i.Name == @"name" && i.Value.EndsWith(@"@キャラ素材")));

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

            // キャラスクリプト用ではないテキストオブジェクトのみ対象
            // テキストが空でないものを優先
            // その中でも開始フレーム位置が手前のものを優先
            // その中でもレイヤー番号が小さいものを優先
            var items =
                from i in layerItems
                let render = i.GetComponent<RenderComponent>()
                let text = i.GetComponent<TextComponent>()
                where render != null && text != null && !ContainsCharaScript(i)
                orderby (text.Text.Length == 0) ? 1 : 0, i.BeginFrame, i.LayerId
                select new { render, text, clipping = i.IsClipping };

            foreach (var item in items)
            {
                var temp =
                    new ExoTextStyleTemplate(item.render, item.text, item.clipping);

                // 重複するなら無視
                if (result.Any(t => t.EqualsWithoutUnused(temp)))
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
            this.X = this.MakeMovableValueViewModel(this.Render, r => r.X);
            this.Y = this.MakeMovableValueViewModel(this.Render, r => r.Y);
            this.Z = this.MakeMovableValueViewModel(this.Render, r => r.Z);
            this.Scale = this.MakeMovableValueViewModel(this.Render, r => r.Scale);
            this.Transparency =
                this.MakeMovableValueViewModel(this.Render, r => r.Transparency);
            this.Rotation = this.MakeMovableValueViewModel(this.Render, r => r.Rotation);

            this.FontSize = this.MakeMovableValueViewModel(this.Text, t => t.FontSize);
            this.TextSpeed = this.MakeMovableValueViewModel(this.Text, t => t.TextSpeed);

            this.PlayVolume = this.MakeMovableValueViewModel(this.Play, p => p.Volume);
            this.PlayBalance = this.MakeMovableValueViewModel(this.Play, p => p.Balance);

            this.PlaySpeed =
                this.MakeMovableValueViewModel(
                    this.BaseConfig,
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
        /// <typeparam name="TConstants">定数情報型。</typeparam>
        /// <param name="holder">オブジェクト。</param>
        /// <param name="selector">
        /// オブジェクトの MovableValue{TConstants} プロパティセレクタ。
        /// </param>
        /// <param name="name">名前。 null ならば自動決定される。</param>
        /// <returns>MovableValueViewModel 。</returns>
        private MovableValueViewModel MakeMovableValueViewModel<T, TConstants>(
            IReadOnlyReactiveProperty<T> holder,
            Expression<Func<T, MovableValue<TConstants>>> selector,
            string name = null)
            where T : INotifyPropertyChanged
            where TConstants : IMovableValueConstants, new()
        {
            Debug.Assert(holder != null);
            Debug.Assert(selector != null);

            // 値取得
            var value = this.MakeInnerPropertyOf(holder, selector, this.CanModify);

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
            try
            {
                this.LastTemplateFilePath.Value = Path.GetFullPath(filePath);
            }
            catch
            {
                this.LastTemplateFilePath.Value = filePath;
            }

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
                e.Effects = DragDropEffects.Copy | DragDropEffects.Move;
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

            this.Templates.Value[index].CopyTo(
                this.BaseConfig.Value,
                withoutUnused: true);

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
        /// <param name="subStatusCommandTip">
        /// オプショナルなサブ状態コマンドのチップテキスト。
        /// </param>
        private void SetLastStatus(
            AppStatusType statusType = AppStatusType.None,
            string statusText = "",
            AppStatusType subStatusType = AppStatusType.None,
            string subStatusText = "",
            ICommand subStatusCommand = null,
            string subStatusCommandTip = "")
            =>
            this.LastStatus.Value =
                new AppStatus
                {
                    StatusType = statusType,
                    StatusText = statusText ?? "",
                    SubStatusType = subStatusType,
                    SubStatusText = subStatusText ?? "",
                    SubStatusCommand = subStatusCommand,
                    SubStatusCommandTip =
                        string.IsNullOrEmpty(subStatusCommandTip) ?
                            null : subStatusCommandTip,
                };

        #region デザイン時用定義

        /// <summary>
        /// デザイン時用コンストラクタ。
        /// </summary>
        [Obsolete(@"Design time only.")]
        public ExoCharaStyleViewModel()
            :
            this(
                new ReactiveProperty<bool>(true),
                new ReactiveProperty<ExoCharaStyle>(
                    new ExoCharaStyle(VoiceroidId.YukariEx)),
                new ReactiveProperty<UIConfig>(new UIConfig()),
                new ReactiveProperty<IAppStatus>(new AppStatus()),
                NullServices.OpenFileDialog)
        {
        }

#endregion
    }
}
