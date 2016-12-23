using System;
using System.Reactive.Linq;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.Windows.Mvvm.Commands;

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

        #region デザイン時用定義

        /// <summary>
        /// デザイン時用コンストラクタ。
        /// </summary>
        [Obsolete(@"Design time only.")]
        public AppStatusViewModel()
            :
            this(
                Observable.Return(
                    new AppStatus
                    {
                        StatusType = AppStatusType.Success,
                        StatusText = @"デザイン時用テキスト",
                        SubStatusType = AppStatusType.Warning,
                        SubStatusText = @"デザイン時用サブテキスト",
                        SubStatusCommand = new ProcessStartCommand(@"C:"),
                        SubStatusCommandTip = @"C: を開く",
                    }))
        {
        }

        #endregion
    }
}
