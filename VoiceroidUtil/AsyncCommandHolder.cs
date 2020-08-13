using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows.Input;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using RucheHome.Util;
using VoiceroidUtil.Extensions;
using static RucheHome.Util.ArgumentValidater;

namespace VoiceroidUtil
{
    /// <summary>
    /// パラメータ変換と戻り値をサポートする非同期コマンドを保持する抽象クラス。
    /// </summary>
    /// <typeparam name="TParameter">パラメータ型。</typeparam>
    /// <typeparam name="TResult">戻り値型。</typeparam>
    public abstract class AsyncCommandHolderBase<TParameter, TResult> : IDisposable
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        protected AsyncCommandHolderBase() : this(new AsyncReactiveCommand<TParameter>())
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="canExecuteSource">コマンド実施可否状態のプッシュ通知。</param>
        protected AsyncCommandHolderBase(IObservable<bool> canExecuteSource)
            : this(canExecuteSource?.ToAsyncReactiveCommand<TParameter>())
            =>
            ValidateArgumentNull(canExecuteSource, nameof(canExecuteSource));

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="sharedCanExecute">
        /// コマンド実施可否状態を共有する ReactiveProperty 。
        /// </param>
        protected AsyncCommandHolderBase(IReactiveProperty<bool> sharedCanExecute)
            : this(sharedCanExecute?.ToAsyncReactiveCommand<TParameter>())
            =>
            ValidateArgumentNull(sharedCanExecute, nameof(sharedCanExecute));

        /// <summary>
        /// デストラクタ。
        /// </summary>
        ~AsyncCommandHolderBase() => this.Dispose(false);

        /// <summary>
        /// コマンドを取得する。
        /// </summary>
        public ICommand Command { get; }

        /// <summary>
        /// コマンドを実施可能な状態であるか否かを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<bool> CanExecute { get; }

        /// <summary>
        /// コマンドを実施中であるか否かを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<bool> IsBusy { get; }

        /// <summary>
        /// コマンドの戻り値を取得する。
        /// </summary>
        /// <remarks>
        /// <para>既定では default(TResult) を返す。</para>
        /// <para>コマンドの戻り値が前回と同値であっても通知される。</para>
        /// </remarks>
        public IReadOnlyReactiveProperty<TResult> Result { get; }

        /// <summary>
        /// IDisposable.Dispose をまとめて呼び出すためのコンテナを取得する。
        /// </summary>
        protected CompositeDisposable CompositeDisposable { get; } =
            new CompositeDisposable();

        /// <summary>
        /// コマンドパラメータを変換する。
        /// </summary>
        /// <param name="parameter">元のコマンドパラメータ。</param>
        /// <returns>変換されたコマンドパラメータ。</returns>
        /// <remarks>
        /// コマンド実施時に呼び出される。
        /// 既定では引数値をそのまま返す。
        /// </remarks>
        protected virtual TParameter ConvertParameter(TParameter parameter) => parameter;

        /// <summary>
        /// コマンド処理を行う。
        /// </summary>
        /// <param name="parameter">コマンドパラメータ。</param>
        /// <returns>コマンドの戻り値。</returns>
        protected abstract Task<TResult> Execute(TParameter parameter);

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="command">非同期コマンド。</param>
        private AsyncCommandHolderBase(AsyncReactiveCommand<TParameter> command)
        {
            if (command == null)
            {
                return;
            }

            // 戻り値の通知先
            // 同値でも通知されるように DistinctUntilChanged を外す
            var result =
                new ReactiveProperty<TResult>(
                    default,
                    ReactivePropertyMode.RaiseLatestValueOnSubscribe)
                    .AddTo(this.CompositeDisposable);

            var busy = new ReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);

            command
                .Subscribe(
                    async p =>
                    {
                        busy.Value = true;
                        try
                        {
                            result.Value = await this.Execute(this.ConvertParameter(p));
                        }
                        finally
                        {
                            busy.Value = false;
                        }
                    })
                .AddTo(this.CompositeDisposable);

            this.Command = command;
            this.CanExecute =
                command
                    .CanExecuteToReadOnlyReactiveProperty()
                    .AddTo(this.CompositeDisposable);
            this.IsBusy = busy;
            this.Result = result;
        }

        #region IDisposable の実装

        /// <summary>
        /// リソースを破棄する。
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// リソース破棄の実処理を行う。
        /// </summary>
        /// <param name="disposing">
        /// Dispose メソッドから呼び出された場合は true 。
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    this.CompositeDisposable.Dispose();
                }
                catch (Exception ex)
                {
                    ThreadTrace.WriteException(ex);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// パラメータ変換と戻り値をサポートする非同期コマンドを保持するクラス。
    /// </summary>
    /// <typeparam name="TParameter">パラメータ型。</typeparam>
    /// <typeparam name="TResult">戻り値型。</typeparam>
    public class AsyncCommandHolder<TParameter, TResult>
        : AsyncCommandHolderBase<TParameter, TResult>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="executer">コマンド処理デリゲート。</param>
        /// <param name="parameterConverter">
        /// コマンドパラメータ変換デリゲート。 null ならば変換しない。
        /// </param>
        public AsyncCommandHolder(
            Func<TParameter, Task<TResult>> executer,
            Func<TParameter, TParameter> parameterConverter = null)
            : base()
            =>
            this.Construct(executer, parameterConverter);

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="canExecuteSource">コマンド実施可否状態プッシュ通知。</param>
        /// <param name="executer">コマンド処理デリゲート。</param>
        /// <param name="parameterConverter">
        /// コマンドパラメータ変換デリゲート。 null ならば変換しない。
        /// </param>
        public AsyncCommandHolder(
            IObservable<bool> canExecuteSource,
            Func<TParameter, Task<TResult>> executer,
            Func<TParameter, TParameter> parameterConverter = null)
            : base(canExecuteSource)
            =>
            this.Construct(executer, parameterConverter);

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="sharedCanExecute">
        /// コマンド実施可否状態を共有する ReactiveProperty 。
        /// </param>
        /// <param name="executer">コマンド処理デリゲート。</param>
        /// <param name="parameterConverter">
        /// コマンドパラメータ変換デリゲート。 null ならば変換しない。
        /// </param>
        public AsyncCommandHolder(
            IReactiveProperty<bool> sharedCanExecute,
            Func<TParameter, Task<TResult>> executer,
            Func<TParameter, TParameter> parameterConverter = null)
            : base(sharedCanExecute)
            =>
            this.Construct(executer, parameterConverter);

        /// <summary>
        /// コマンド処理デリゲートを取得または設定する。
        /// </summary>
        private Func<TParameter, Task<TResult>> Executer { get; set; }

        /// <summary>
        /// コマンドパラメータ変換デリゲートを取得または設定する。
        /// </summary>
        private Func<TParameter, TParameter> ParameterConverter { get; set; }

        /// <summary>
        /// コンストラクタ処理を行う。
        /// </summary>
        /// <param name="executer">コマンド処理デリゲート。</param>
        /// <param name="parameterConverter">
        /// コマンドパラメータ変換デリゲート。 null ならば変換しない。
        /// </param>
        private void Construct(
            Func<TParameter, Task<TResult>> executer,
            Func<TParameter, TParameter> parameterConverter)
        {
            ValidateArgumentNull(executer, nameof(executer));

            this.Executer = executer;
            this.ParameterConverter = parameterConverter;
        }

        #region AsyncCommandHolderBase<TParameter, TResult> のオーバライド

        /// <summary>
        /// コマンドパラメータを変換する。
        /// </summary>
        /// <param name="parameter">元のコマンドパラメータ。</param>
        /// <returns>変換されたコマンドパラメータ。</returns>
        protected override sealed TParameter ConvertParameter(TParameter parameter)
            =>
            (this.ParameterConverter == null) ?
                parameter : this.ParameterConverter(parameter);

        /// <summary>
        /// コマンド処理を行う。
        /// </summary>
        /// <param name="parameter">コマンドパラメータ。</param>
        /// <returns>コマンドの戻り値。</returns>
        protected override sealed Task<TResult> Execute(TParameter parameter)
            =>
            this.Executer(parameter);

        #endregion
    }

    /// <summary>
    /// パラメータ変換をサポートする非同期コマンドを保持するクラス。
    /// </summary>
    /// <typeparam name="TParameter">パラメータ型。</typeparam>
    public class AsyncCommandHolder<TParameter> : AsyncCommandHolder<TParameter, object>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="executer">コマンド処理デリゲート。</param>
        /// <param name="parameterConverter">
        /// コマンドパラメータ変換デリゲート。 null ならば変換しない。
        /// </param>
        public AsyncCommandHolder(
            Func<TParameter, Task> executer,
            Func<TParameter, TParameter> parameterConverter = null)
            : base(MakeExecuter(executer), parameterConverter)
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="canExecuteSource">コマンド実施可否状態プッシュ通知。</param>
        /// <param name="executer">コマンド処理デリゲート。</param>
        /// <param name="parameterConverter">
        /// コマンドパラメータ変換デリゲート。 null ならば変換しない。
        /// </param>
        public AsyncCommandHolder(
            IObservable<bool> canExecuteSource,
            Func<TParameter, Task> executer,
            Func<TParameter, TParameter> parameterConverter = null)
            : base(canExecuteSource, MakeExecuter(executer), parameterConverter)
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="sharedCanExecute">
        /// コマンド実施可否状態を共有する ReactiveProperty 。
        /// </param>
        /// <param name="executer">コマンド処理デリゲート。</param>
        /// <param name="parameterConverter">
        /// コマンドパラメータ変換デリゲート。 null ならば変換しない。
        /// </param>
        public AsyncCommandHolder(
            IReactiveProperty<bool> sharedCanExecute,
            Func<TParameter, Task> executer,
            Func<TParameter, TParameter> parameterConverter = null)
            : base(sharedCanExecute, MakeExecuter(executer), parameterConverter)
        {
        }

        /// <summary>
        /// ベースクラスのコンストラクタ用のコマンド処理デリゲートを作成する。
        /// </summary>
        /// <param name="executer">コマンド処理デリゲート。</param>
        /// <returns>ベースクラスのコンストラクタ用のコマンド処理デリゲート。</returns>
        private static Func<TParameter, Task<object>> MakeExecuter(
            Func<TParameter, Task> executer)
        {
            ValidateArgumentNull(executer, nameof(executer));

            return
                async parameter =>
                    {
                        await executer(parameter);
                        return null;
                    };
        }
    }

    /// <summary>
    /// 非同期コマンドを保持するクラス。
    /// </summary>
    public sealed class AsyncCommandHolder : AsyncCommandHolder<object>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="executer">コマンド処理デリゲート。</param>
        public AsyncCommandHolder(Func<Task> executer)
            : base(MakeExecuter(executer), null)
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="canExecuteSource">コマンド実施可否状態プッシュ通知。</param>
        /// <param name="executer">コマンド処理デリゲート。</param>
        public AsyncCommandHolder(IObservable<bool> canExecuteSource, Func<Task> executer)
            : base(canExecuteSource, MakeExecuter(executer), null)
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="sharedCanExecute">
        /// コマンド実施可否状態を共有する ReactiveProperty 。
        /// </param>
        /// <param name="executer">コマンド処理デリゲート。</param>
        public AsyncCommandHolder(
            Func<Task> executer,
            IReactiveProperty<bool> sharedCanExecute)
            : base(sharedCanExecute, MakeExecuter(executer), null)
        {
        }

        /// <summary>
        /// ベースクラスのコンストラクタ用のコマンド処理デリゲートを作成する。
        /// </summary>
        /// <param name="executer">コマンド処理デリゲート。</param>
        /// <returns>ベースクラスのコンストラクタ用のコマンド処理デリゲート。</returns>
        private static Func<object, Task> MakeExecuter(Func<Task> executer)
        {
            ValidateArgumentNull(executer, nameof(executer));

            return (async _ => await executer());
        }
    }
}
