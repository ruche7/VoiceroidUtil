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
            var apps = FindProcesses();
            return Task.WhenAll(Array.ConvertAll(this.Impls, p => p.Update(apps)));
        }

        /// <summary>
        /// 全VOICEROIDプロセスの UI Automation 利用許可状態を設定する。
        /// </summary>
        /// <param name="enabled">許可するならば true 。そうでなければ false 。</param>
        /// <remarks>
        /// 音声ファイル保存ダイアログ操作に UI Automation を利用するか否かに関わる。
        /// </remarks>
        public void SetUIAutomationEnabled(bool enabled)
        {
            Array.ForEach(this.Impls, p => p.IsUIAutomationEnabled = enabled);
        }

        /// <summary>
        /// 検索対象プロセス名列挙を取得する。
        /// </summary>
        private static IEnumerable<string> SearchProcessNames { get; } =
            new[] { @"VOICEROID", @"OtomachiUnaTalkEx" };

        /// <summary>
        /// プロセスを検索する。
        /// </summary>
        /// <returns>VOICEROIDプロセス列挙。</returns>
        private static IEnumerable<Process> FindProcesses() =>
            SearchProcessNames.SelectMany(name => Process.GetProcessesByName(name));

        /// <summary>
        /// VOICEROIDプロセス実装配列を取得する。
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
