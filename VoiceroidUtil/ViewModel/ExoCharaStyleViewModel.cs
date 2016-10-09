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
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.AviUtl.ExEdit;
using RucheHome.Util;

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
        public ExoCharaStyleViewModel(ExoCharaStyle value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            // 修正可否
            this.CanModify =
                (new ReactiveProperty<bool>(true)).AddTo(this.CompositeDisposable);

            this.Value = value;

            // 直近のアプリ状態値
            this.LastStatus =
                new ReactiveProperty<IAppStatus>(new AppStatus())
                    .AddTo(this.CompositeDisposable);

            // 内包 ViewModel のセットアップ
            this.SetupViewModels();

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
        /// テキストスタイル雛形コレクションを取得する。
        /// </summary>
        public ReadOnlyCollection<ExoTextStyleTemplate> TextStyleTemplates
        {
            get { return this.textStyleTemplates; }
            private set
            {
                this.SetProperty(
                    ref this.textStyleTemplates,
                    value ?? (new List<ExoTextStyleTemplate>()).AsReadOnly());
            }
        }
        public ReadOnlyCollection<ExoTextStyleTemplate> textStyleTemplates =
            (new List<ExoTextStyleTemplate>()).AsReadOnly();

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

            foreach (var item in layerItems)
            {
                var render = item.GetComponent<RenderComponent>();
                if (render == null)
                {
                    continue;
                }

                var text = item.GetComponent<TextComponent>();
                if (text == null)
                {
                    continue;
                }

                var temp =
                    new ExoTextStyleTemplate
                    {
                        Render = render,
                        Text = text,
                        IsTextClipping = item.IsClipping,
                    };

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
        }

        /// <summary>
        /// ComponentBase 派生クラス型オブジェクトから MovableValueViewModel を作成する。
        /// </summary>
        /// <typeparam name="T">ComponentBase 派生クラス型。</typeparam>
        /// <param name="component">ComponentBase 派生クラス型オブジェクト。</param>
        /// <param name="componentObservable">
        /// ComponentBase 派生クラス型オブジェクトのプッシュ通知。
        /// </param>
        /// <param name="selector">
        /// ComponentBase 派生クラス型オブジェクトの IMovableValue プロパティセレクタ。
        /// </param>
        /// <returns>MovableValueViewModel 。</returns>
        private MovableValueViewModel MakeMovableValueViewModel<T>(
            T component,
            IObservable<T> componentObservable,
            Expression<Func<T, IMovableValue>> selector)
            where T : ComponentBase
        {
            Debug.Assert(component != null);
            Debug.Assert(componentObservable != null);
            Debug.Assert(selector != null);

            // 初期値取得
            var value = selector.Compile()(component);

            // 名前取得
            var info = ((MemberExpression)selector.Body).Member;
            var name =
                info.GetCustomAttribute<ExoFileItemAttribute>(true)?.Name ?? info.Name;

            var result = new MovableValueViewModel(name, value);

            // プロパティ値変更時に ViewModel 内部値を差し替える
            componentObservable
                .Select(comp => comp.ObserveProperty(selector))
                .Switch()
                .Subscribe(v => result.Reset(v))
                .AddTo(this.CompositeDisposable);

            return result;
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
            this.TextStyleTemplates = temps.AsReadOnly();
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
        private void SetLastStatus(
            AppStatusType statusType = AppStatusType.None,
            string statusText = "",
            AppStatusType subStatusType = AppStatusType.None,
            string subStatusText = "")
        {
            this.LastStatus.Value =
                new AppStatus
                {
                    StatusType = statusType,
                    StatusText = statusText ?? "",
                    SubStatusType = subStatusType,
                    SubStatusText = subStatusText ?? "",
                };
        }
    }
}
