using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;

namespace ruche.voiceroid
{
    /// <summary>
    /// VOICEROIDプロセスファクトリクラス。
    /// </summary>
    public partial class ProcessFactory
    {
        /// <summary>
        /// 既定の状態更新間隔。
        /// </summary>
        public static readonly TimeSpan DefaultUpdateSpan =
            TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public ProcessFactory() : this(DefaultUpdateSpan)
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="updateSpan">状態更新間隔。</param>
        public ProcessFactory(TimeSpan updateSpan)
        {
            this.Processes = new ReadOnlyCollection<IProcess>(this.Impls);
            this.Update();

            if (updateSpan > TimeSpan.Zero)
            {
                this.UpdateTimer.Interval = updateSpan;
                this.UpdateTimer.Tick += this.UpdateTimer_Tick;
                this.UpdateTimer.Start();
            }
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
        /// プロセス実装配列を取得する。
        /// </summary>
        private ProcessImpl[] Impls { get; } =
            Array.ConvertAll(
                (VoiceroidId[])Enum.GetValues(typeof(VoiceroidId)),
                id => new ProcessImpl(id));

        /// <summary>
        /// 状態更新用タイマーを取得する。
        /// </summary>
        private DispatcherTimer UpdateTimer { get; } =
            new DispatcherTimer(DispatcherPriority.Normal);

        /// <summary>
        /// 状態を更新する。
        /// </summary>
        private void Update()
        {
            var voiceroidApps = Process.GetProcessesByName("VOICEROID");
            Array.ForEach(this.Impls, impl => impl.Update(voiceroidApps));
        }

        /// <summary>
        /// タイマーから一定時間毎に呼び出される。
        /// </summary>
        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            this.Update();
        }
    }
}
