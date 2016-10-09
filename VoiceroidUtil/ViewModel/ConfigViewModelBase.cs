using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.Util;

namespace VoiceroidUtil.ViewModel
{
    /// <summary>
    /// 設定値を保持する ViewModel のベースクラス。
    /// </summary>
    /// <typeparam name="T">設定値の型。</typeparam>
    public abstract class ConfigViewModelBase<T> : ViewModelBase
        where T : new()
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="value">設定の初期値。</param>
        public ConfigViewModelBase(T value = default(T))
        {
            // 修正可否
            this.CanModify =
                (new ReactiveProperty<bool>(true)).AddTo(this.CompositeDisposable);

            this.Value = value;

            // ロード実施済みフラグ
            this.IsLoadedCore =
                (new ReactiveProperty<bool>(false)).AddTo(this.CompositeDisposable);
            this.IsLoaded =
                this.IsLoadedCore
                    .ToReadOnlyReactiveProperty()
                    .AddTo(this.CompositeDisposable);

            // セーブ要求 Subject
            // 100ms の間、次のセーブ要求が来なければ実際のセーブ処理を行う
            this.SaveRequestSubject =
                (new Subject<object>()).AddTo(this.CompositeDisposable);
            this.SaveRequestSubject
                .Throttle(TimeSpan.FromMilliseconds(100))
                .Subscribe(_ => this.DoSave())
                .AddTo(this.CompositeDisposable);

            // ロードコマンド
            this.LoadCommand =
                this.CanModify.ToReactiveCommand().AddTo(this.CompositeDisposable);
            this.LoadCommand
                .Subscribe(async _ => await this.ExecuteLoadCommand())
                .AddTo(this.CompositeDisposable);

            // セーブコマンド
            // セーブ要求 Subject に通知するのみ
            this.SaveCommand = (new ReactiveCommand()).AddTo(this.CompositeDisposable);
            this.SaveCommand
                .Subscribe(_ => this.SaveRequestSubject.OnNext(null))
                .AddTo(this.CompositeDisposable);
        }

        /// <summary>
        /// 設定値を取得または設定する。
        /// </summary>
        public T Value
        {
            get { return this.value; }
            set
            {
                if (this.CanModify.Value)
                {
                    this.SetProperty(ref this.value, (value == null) ? (new T()) : value);
                }
            }
        }
        private T value = default(T);

        /// <summary>
        /// 設定値を修正可能な状態であるか否かを取得する。
        /// </summary>
        /// <remarks>
        /// 既定では常に true を返す。外部からの設定以外で更新されることはない。
        /// </remarks>
        public ReactiveProperty<bool> CanModify { get; }

        /// <summary>
        /// 設定のロードが1回以上行われたか否かを取得する。
        /// </summary>
        public ReadOnlyReactiveProperty<bool> IsLoaded { get; }

        /// <summary>
        /// 設定ロードコマンドを取得する。
        /// </summary>
        public ReactiveCommand LoadCommand { get; }

        /// <summary>
        /// 設定セーブコマンドを取得する。
        /// </summary>
        public ReactiveCommand SaveCommand { get; }

        /// <summary>
        /// 設定の保持と読み書きを行うオブジェクトを取得する。
        /// </summary>
        private ConfigKeeper<T> Keeper { get; } =
            new ConfigKeeper<T>(nameof(VoiceroidUtil));

        /// <summary>
        /// 設定のロードが1回以上行われたか否かを取得する。
        /// </summary>
        private ReactiveProperty<bool> IsLoadedCore { get; }

        /// <summary>
        /// 設定セーブ処理要求 Subject を取得する。
        /// </summary>
        private Subject<object> SaveRequestSubject { get; }

        /// <summary>
        /// LoadCommand の実処理を行う。
        /// </summary>
        private async Task ExecuteLoadCommand()
        {
            try
            {
                if (await Task.Run(() => this.Keeper.Load()))
                {
                    this.Value = this.Keeper.Value;
                }
                else
                {
                    // ロードに失敗した場合は現在値をセーブしておく
                    this.SaveCommand.Execute();
                }
            }
            finally
            {
                // 成否に関わらずロード実施済みとする
                this.IsLoadedCore.Value = true;
            }
        }

        /// <summary>
        /// セーブ処理を行う。
        /// </summary>
        private void DoSave()
        {
            // 1回以上 LoadCommand が実施されていなければ処理しない
            if (this.IsLoadedCore.Value)
            {
                this.Keeper.Value = this.Value;
                this.Keeper.Save();
            }
        }
    }
}
