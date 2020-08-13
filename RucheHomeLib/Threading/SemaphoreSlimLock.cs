using System;
using System.Threading;
using System.Threading.Tasks;

namespace RucheHome.Threading
{
    /// <summary>
    /// SemaphoreSlim による排他制御を簡潔に行うためのクラス。
    /// </summary>
    public class SemaphoreSlimLock : IDisposable
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="count">同時に許可する SemaphoreSlim 移行要求数。</param>
        public SemaphoreSlimLock(int count = 1) =>
            this.Semaphore = new SemaphoreSlim(count, count);

        /// <summary>
        /// デストラクタ。
        /// </summary>
        ~SemaphoreSlimLock() => this.Dispose(false);

        /// <summary>
        /// 現在同時に許可可能な移行要求数を取得する。
        /// </summary>
        public int CurrentCount => this.Semaphore.CurrentCount;

        /// <summary>
        /// SemaphoreSlim に移行するために待機し、
        /// その解放を行う IDisposable オブジェクトを返す。
        /// </summary>
        /// <returns>SemaphoreSlim の解放を行う IDisposable オブジェクト。</returns>
        public IDisposable Wait()
        {
            this.Semaphore.Wait();
            return new ReleaseOnDispose(this.Semaphore);
        }

        /// <summary>
        /// SemaphoreSlim に移行するために待機し、
        /// その解放を行う IDisposable オブジェクトを返す。
        /// </summary>
        /// <param name="millisecondsTimeout">待機するミリ秒数。</param>
        /// <returns>
        /// SemaphoreSlim の解放を行う IDisposable オブジェクト。
        /// 待機がタイムアウトした場合は null 。
        /// </returns>
        public IDisposable Wait(int millisecondsTimeout)
        {
            var ok = this.Semaphore.Wait(millisecondsTimeout);
            return ok ? (new ReleaseOnDispose(this.Semaphore)) : null;
        }

        /// <summary>
        /// SemaphoreSlim に移行するために待機し、
        /// その解放を行う IDisposable オブジェクトを返す。
        /// </summary>
        /// <param name="timeout">待機する時間。</param>
        /// <returns>
        /// SemaphoreSlim の解放を行う IDisposable オブジェクト。
        /// 待機がタイムアウトした場合は null 。
        /// </returns>
        public IDisposable Wait(TimeSpan timeout)
        {
            var ok = this.Semaphore.Wait(timeout);
            return ok ? (new ReleaseOnDispose(this.Semaphore)) : null;
        }

        /// <summary>
        /// SemaphoreSlim に移行するために非同期に待機し、
        /// その解放を行う IDisposable オブジェクトを返す。
        /// </summary>
        /// <returns>SemaphoreSlim の解放を行う IDisposable オブジェクト。</returns>
        public async Task<IDisposable> WaitAsync()
        {
            await this.Semaphore.WaitAsync();
            return new ReleaseOnDispose(this.Semaphore);
        }

        /// <summary>
        /// SemaphoreSlim に移行するために非同期に待機し、
        /// その解放を行う IDisposable オブジェクトを返す。
        /// </summary>
        /// <param name="millisecondsTimeout">待機するミリ秒数。</param>
        /// <returns>
        /// SemaphoreSlim の解放を行う IDisposable オブジェクト。
        /// 待機がタイムアウトした場合は null 。
        /// </returns>
        public async Task<IDisposable> WaitAsync(int millisecondsTimeout)
        {
            var ok = await this.Semaphore.WaitAsync(millisecondsTimeout);
            return ok ? (new ReleaseOnDispose(this.Semaphore)) : null;
        }

        /// <summary>
        /// SemaphoreSlim に移行するために非同期に待機し、
        /// その解放を行う IDisposable オブジェクトを返す。
        /// </summary>
        /// <param name="timeout">待機する時間。</param>
        /// <returns>
        /// SemaphoreSlim の解放を行う IDisposable オブジェクト。
        /// 待機がタイムアウトした場合は null 。
        /// </returns>
        public async Task<IDisposable> WaitAsync(TimeSpan timeout)
        {
            var ok = await this.Semaphore.WaitAsync(timeout);
            return ok ? (new ReleaseOnDispose(this.Semaphore)) : null;
        }

        /// <summary>
        /// SemaphoreSlim インスタンスを取得する。
        /// </summary>
        private SemaphoreSlim Semaphore { get; }

        #region IDisposable の実装

        /// <summary>
        /// リソースの破棄を行う。
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
                this.Semaphore.Dispose();
            }
        }

        #endregion

        #region ReleaseOnDispose クラス

        /// <summary>
        /// Dispose メソッド呼び出しにより
        /// SemaphoreSlim インスタンスの Release メソッドを呼び出すクラス。
        /// </summary>
        private sealed class ReleaseOnDispose : IDisposable
        {
            /// <summary>
            /// コンストラクタ。
            /// </summary>
            /// <param name="semaphore">SemaphoreSlim インスタンス。</param>
            public ReleaseOnDispose(SemaphoreSlim semaphore) => this.Semaphore = semaphore;

            /// <summary>
            /// SemaphoreSlim インスタンスの Release メソッドを呼び出す。
            /// </summary>
            public void Dispose()
            {
                if (this.Semaphore != null)
                {
                    try
                    {
                        this.Semaphore.Release();
                    }
                    catch (ObjectDisposedException) { }
                }
                this.Semaphore = null;
            }

            /// <summary>
            /// SemaphoreSlim インスタンスを取得または設定する。
            /// </summary>
            private SemaphoreSlim Semaphore { get; set; }
        }

        #endregion
    }
}
