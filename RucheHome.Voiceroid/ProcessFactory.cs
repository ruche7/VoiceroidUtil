using System;
using System.Collections.Generic;
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
        /// VOICEROIDプロセスリストを取得する。
        /// </summary>
        public ReadOnlyCollection<IProcess> Processes { get; }

        /// <summary>
        /// VOICEROIDプロセスを取得する。
        /// </summary>
        /// <param name="id">VOICEROID識別ID。</param>
        /// <returns>プロセス。</returns>
        public IProcess Get(VoiceroidId id) => this.Processes.First(p => p.Id == id);

        /// <summary>
        /// 全VOICEROIDプロセスの状態を更新する。
        /// </summary>
        public Task Update()
        {
            return Task.WhenAll(this.MakeUpdateTasks());
        }

        /// <summary>
        /// 全VOICEROIDプロセスの UI Automation 利用許可状態を設定する。
        /// </summary>
        /// <param name="enabled">許可するならば true 。そうでなければ false 。</param>
        /// <remarks>
        /// PlusExImpl 実装において、音声ファイル保存ダイアログ操作に UI Automation を
        /// 利用するか否かに関わる。
        /// Voiceroid2Impl 実装には影響しない。
        /// </remarks>
        public void SetUIAutomationEnabled(bool enabled)
        {
            foreach (var impl in this.Impls.OfType<PlusExImpl>())
            {
                impl.IsUIAutomationEnabled = enabled;
            }
        }

        /// <summary>
        /// VOICEROIDプロセス実装群を作成する。
        /// </summary>
        /// <returns>VOICEROIDプロセス実装リスト。</returns>
        private static ImplBase[] CreateImpls()
        {
            var processes = new List<ImplBase>();

            foreach (VoiceroidId id in Enum.GetValues(typeof(VoiceroidId)))
            {
                if (id == VoiceroidId.Voiceroid2)
                {
                    // TODO: VOICEROID2プロセス作成
                }
                else
                {
                    // VOICEROID+ EX 互換プロセス作成
                    processes.Add(new PlusExImpl(id));
                }
            }

            return processes.ToArray();
        }

        /// <summary>
        /// VOICEROIDプロセス実装配列を取得する。
        /// </summary>
        private ImplBase[] Impls { get; } = CreateImpls();

        /// <summary>
        /// VOICEROIDプロセス更新タスク群を作成する。
        /// </summary>
        /// <returns>VOICEROIDプロセス更新タスク列挙。</returns>
        private IEnumerable<Task> MakeUpdateTasks()
        {
            var tasks = new List<Task>();

            // アプリプロセス群キャッシュ
            var namedProcesses = new Dictionary<string, Process[]>();

            foreach (var impl in this.Impls)
            {
                var appProcessName = impl.Id.GetInfo().AppProcessName;

                Process[] appProcesses = null;
                if (!namedProcesses.TryGetValue(appProcessName, out appProcesses))
                {
                    // アプリプロセス群を検索してキャッシュ
                    appProcesses = Process.GetProcessesByName(appProcessName);
                    namedProcesses.Add(appProcessName, appProcesses);
                }

                tasks.Add(impl.Update(appProcesses));
            }

            return tasks;
        }

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
