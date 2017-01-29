using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace RucheHome.Voiceroid
{
    /// <summary>
    /// VOICEROIDプロセスファクトリクラス。
    /// </summary>
    public partial class ProcessFactory : IDisposable
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public ProcessFactory()
        {
            this.Processes = Array.AsReadOnly<IProcess>(this.Impls);
            this.Update();
        }

        /// <summary>
        /// デストラクタ。
        /// </summary>
        ~ProcessFactory()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// プロセスリストを取得する。
        /// </summary>
        public ReadOnlyCollection<IProcess> Processes { get; }

        /// <summary>
        /// プロセスを取得する。
        /// </summary>
        /// <param name="id">VOICEROID識別ID。</param>
        /// <returns>プロセス。</returns>
        public IProcess Get(VoiceroidId id)
        {
            return this.Processes.First(p => p.Id == id);
        }

        /// <summary>
        /// 全プロセスの状態を更新する。
        /// </summary>
        public Task Update()
        {
            var voiceroidApps = FindProcesses();
            return
                Task.WhenAll(
                    Array.ConvertAll(this.Impls, p => p.Update(voiceroidApps)));
        }

        /// <summary>
        /// VOICEROIDプロセスを検索する。
        /// </summary>
        /// <returns>VOICEROIDプロセス配列。</returns>
        private static Process[] FindProcesses()
        {
            return Process.GetProcessesByName(@"VOICEROID");
        }

        /// <summary>
        /// プロセス実装配列を取得する。
        /// </summary>
        private ProcessImpl[] Impls { get; } =
            Array.ConvertAll(
                (VoiceroidId[])Enum.GetValues(typeof(VoiceroidId)),
                id => new ProcessImpl(id));

        #region IDisposable インタフェース実装

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
            foreach (var p in this.Impls)
            {
                p.Dispose();
            }
        }

        #endregion
    }
}
