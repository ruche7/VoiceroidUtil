using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace ruche.voiceroid
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
        public void Update()
        {
            var voiceroidApps = Process.GetProcessesByName("VOICEROID");
            Array.ForEach(this.Impls, impl => impl.Update(voiceroidApps));
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
