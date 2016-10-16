using System;
using System.Reactive.Linq;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace VoiceroidUtil.ViewModel
{
    /// <summary>
    /// アプリ状態を提供する ViewModel クラス。
    /// </summary>
    public class AppStatusViewModel : ViewModelBase
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="statusObservable">アプリ状態値のプッシュ通知。</param>
        public AppStatusViewModel(IObservable<IAppStatus> statusObservable)
        {
            this.ValidateArgNull(statusObservable, nameof(statusObservable));

            this.Status =
                statusObservable
                    .Where(s => s != null)
                    .ToReadOnlyReactiveProperty(new AppStatus())
                    .AddTo(this.CompositeDisposable);
        }

        /// <summary>
        /// アプリ状態値を取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<IAppStatus> Status { get; }
    }
}
