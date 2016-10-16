using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.Util;

namespace VoiceroidUtil
{
    /// <summary>
    /// 設定マネージャクラス。
    /// </summary>
    public class ConfigManager : IDisposable
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="synchronizationContext">
        /// PropertyChanged イベント通知に用いる同期コンテキスト。不要ならば null 。
        /// </param>
        public ConfigManager(SynchronizationContext synchronizationContext = null)
        {
            this.SynchronizationContext = synchronizationContext;

            this.TalkTextReplaceConfigCore =
                this.MakeConfig(this.TalkTextReplaceConfigKeeper);
            this.ExoConfigCore = this.MakeConfig(this.ExoConfigKeeper);
            this.AppConfigCore = this.MakeConfig(this.AppConfigKeeper);
            this.UIConfigCore = this.MakeConfig(this.UIConfigKeeper);

            this.UpdateSynchronizationContext();

            this.IsLoadingCore =
                new ReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);
            this.IsLoadedCore =
                new ReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);
        }

        /// <summary>
        /// PropertyChanged イベント通知に用いる同期コンテキストを取得する。
        /// </summary>
        public SynchronizationContext SynchronizationContext { get; }

        /// <summary>
        /// トークテキスト置換設定を取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<TalkTextReplaceConfig> TalkTextReplaceConfig =>
            this.TalkTextReplaceConfigCore;

        /// <summary>
        /// AviUtl拡張編集ファイル用設定を取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<ExoConfig> ExoConfig => this.ExoConfigCore;

        /// <summary>
        /// アプリ設定を取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<AppConfig> AppConfig => this.AppConfigCore;

        /// <summary>
        /// UI設定を取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<UIConfig> UIConfig => this.UIConfigCore;

        /// <summary>
        /// 設定をロード中であるか否かを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsLoading => this.IsLoadingCore;

        /// <summary>
        /// 設定のロードが1回以上行われたか否かを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsLoaded => this.IsLoadedCore;

        /// <summary>
        /// すべての設定値をロードする。
        /// </summary>
        /// <remarks>ロード処理が行われたならば true 。そうでなければ false 。</remarks>
        public async Task<bool> Load()
        {
            // 現在ロード中なら実施不可
            if (
                this.IsLoadingCore.Value ||
                Interlocked.Exchange(ref this.loadLock, 1) != 0)
            {
                return false;
            }

            this.IsLoadingCore.Value = true;
            try
            {
                var results =
                    await Task.WhenAll(
                        Task.Run(() => this.TalkTextReplaceConfigKeeper.Load()),
                        Task.Run(() => this.ExoConfigKeeper.Load()),
                        Task.Run(() => this.AppConfigKeeper.Load()),
                        Task.Run(() => this.UIConfigKeeper.Load()));

                if (results[0])
                {
                    this.TalkTextReplaceConfigCore.Value =
                        this.TalkTextReplaceConfigKeeper.Value;
                }
                if (results[1])
                {
                    this.ExoConfigCore.Value = this.ExoConfigKeeper.Value;
                }
                if (results[2])
                {
                    this.AppConfigCore.Value = this.AppConfigKeeper.Value;
                }
                if (results[3])
                {
                    this.UIConfigCore.Value = this.UIConfigKeeper.Value;
                }

                this.UpdateSynchronizationContext();

                // 成否に関わらずロード済みフラグを立てる
                this.IsLoadedCore.Value = true;
            }
            finally
            {
                Interlocked.Exchange(ref this.loadLock, 0);
                this.IsLoadingCore.Value = false;
            }

            return true;
        }
        private int loadLock = 0;

        /// <summary>
        /// IDisposable.Dispose をまとめて呼び出すためのコンテナを取得する。
        /// </summary>
        private CompositeDisposable CompositeDisposable { get; } =
            new CompositeDisposable();

        /// <summary>
        /// TalkTextReplaceConfig プロパティの実体を取得する。
        /// </summary>
        private ReactiveProperty<TalkTextReplaceConfig> TalkTextReplaceConfigCore
        {
            get;
        }

        /// <summary>
        /// ExoConfig プロパティの実体を取得する。
        /// </summary>
        private ReactiveProperty<ExoConfig> ExoConfigCore { get; }

        /// <summary>
        /// AppConfig プロパティの実体を取得する。
        /// </summary>
        private ReactiveProperty<AppConfig> AppConfigCore { get; }

        /// <summary>
        /// UIConfig プロパティの実体を取得する。
        /// </summary>
        private ReactiveProperty<UIConfig> UIConfigCore { get; }

        /// <summary>
        /// TalkTextReplaceConfig 設定値の読み書きを行うオブジェクトを取得する。
        /// </summary>
        private ConfigKeeper<TalkTextReplaceConfig> TalkTextReplaceConfigKeeper { get; } =
            new ConfigKeeper<TalkTextReplaceConfig>(nameof(VoiceroidUtil))
            {
                Value = new TalkTextReplaceConfig()
            };

        /// <summary>
        /// ExoConfig 設定値の読み書きを行うオブジェクトを取得する。
        /// </summary>
        private ConfigKeeper<ExoConfig> ExoConfigKeeper { get; } =
            new ConfigKeeper<ExoConfig>(nameof(VoiceroidUtil))
            {
                Value = new ExoConfig()
            };

        /// <summary>
        /// AppConfig 設定値の読み書きを行うオブジェクトを取得する。
        /// </summary>
        private ConfigKeeper<AppConfig> AppConfigKeeper { get; } =
            new ConfigKeeper<AppConfig>(nameof(VoiceroidUtil))
            {
                Value = new AppConfig()
            };

        /// <summary>
        /// UIConfig 設定値の読み書きを行うオブジェクトを取得する。
        /// </summary>
        private ConfigKeeper<UIConfig> UIConfigKeeper { get; } =
            new ConfigKeeper<UIConfig>(nameof(VoiceroidUtil))
            {
                Value = new UIConfig()
            };

        /// <summary>
        /// IsLoading プロパティの実体を取得する。
        /// </summary>
        private ReactiveProperty<bool> IsLoadingCore { get; }

        /// <summary>
        /// IsLoaded プロパティの実体を取得する。
        /// </summary>
        private ReactiveProperty<bool> IsLoadedCore { get; }

        /// <summary>
        /// プロパティ変更時に自動保存される設定値を作成する。
        /// </summary>
        /// <typeparam name="TConfig">設定値の型。</typeparam>
        /// <param name="keeper">初期値と保存処理を提供するオブジェクト。</param>
        /// <returns>設定値。</returns>
        private ReactiveProperty<TConfig> MakeConfig<TConfig>(
            ConfigKeeper<TConfig> keeper)
            where TConfig : INotifyPropertyChanged
        {
            var result =
                new ReactiveProperty<TConfig>(keeper.Value)
                    .AddTo(this.CompositeDisposable);

            // 自動保存設定
            // 100ms 以内のプロパティ値変更はまとめて保存する
            result
                .Select(c => c.PropertyChangedAsObservable())
                .Switch()
                .Throttle(TimeSpan.FromMilliseconds(100))
                .Where(_ => !this.IsLoading.Value && this.IsLoaded.Value)
                .Subscribe(
                    _ =>
                    {
                        keeper.Value = result.Value;
                        keeper.Save();
                    })
                .AddTo(this.CompositeDisposable);

            return result;
        }

        /// <summary>
        /// 現在保持している設定値の同期オブジェクトを更新する。
        /// </summary>
        private void UpdateSynchronizationContext()
        {
            var context = this.SynchronizationContext;

            {
                var c = this.TalkTextReplaceConfig.Value;
                c.SynchronizationContext = context;
            }
            {
                var c = this.ExoConfig.Value;
                c.SynchronizationContext = context;
                c.Common.SynchronizationContext = context;
                c.CharaStyles.SynchronizationContext = context;
            }
            {
                var c = this.AppConfig.Value;
                c.SynchronizationContext = context;
                c.YmmCharaRelations.SynchronizationContext = context;
            }
            {
                var c = this.UIConfig.Value;
                c.SynchronizationContext = context;
                c.VoiceroidExecutablePathes.SynchronizationContext = context;
            }
        }

        #region IDisposable の実装

        /// <summary>
        /// リソースを破棄する。
        /// </summary>
        public void Dispose()
        {
            this.CompositeDisposable.Dispose();
        }

        #endregion
    }
}
