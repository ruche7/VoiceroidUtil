using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.AviUtl.ExEdit;
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

            // ViewModel のセットアップ
            this.SetupViewModel();

            // 直近のアプリ状態値
            // ViewModel から受け取る
            this.LastStatus = this.CharaStyle.LastStatus;
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
        /// キャラ別スタイル設定 ViewModel を取得する。
        /// </summary>
        public ExoCharaStyleViewModel CharaStyle { get; private set; }

        /// <summary>
        /// 直近のアプリ状態値を取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<IAppStatus> LastStatus { get; }

        /// <summary>
        /// 内包 ViewModel のセットアップを行う。
        /// </summary>
        private void SetupViewModel()
        {
            this.CharaStyle =
                new ExoCharaStyleViewModel(
                    this.Value.CharaStyles[this.UIConfig.Value.ExoCharaVoiceroidId]);

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
        }
    }
}
