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
    public partial class ProcessFactory
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public ProcessFactory()
        {
            this.Processes = new ReadOnlyCollection<IProcess>(this.Impls);
            this.Update();
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
    }
}
