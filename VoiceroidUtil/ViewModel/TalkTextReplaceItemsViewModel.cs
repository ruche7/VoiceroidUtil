using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace VoiceroidUtil.ViewModel
{
    /// <summary>
    /// TalkTextReplaceItemCollection インスタンスの操作を提供する ViewModel クラス。
    /// </summary>
    public class TalkTextReplaceItemsViewModel
        : ConfigViewModelBase<TalkTextReplaceItemCollection>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="canModify">
        /// 再生や音声保存に関わる設定値の変更可否状態値。
        /// </param>
        /// <param name="items">アイテムコレクション。</param>
        public TalkTextReplaceItemsViewModel(
            IReadOnlyReactiveProperty<bool> canModify,
            IReadOnlyReactiveProperty<TalkTextReplaceItemCollection> items)
            : base(canModify, items)
        {
            // 選択中アイテムインデックス
            this.SelectedIndex =
                new ReactiveProperty<int>((items.Value.Count > 0) ? 0 : -1)
                    .AddTo(this.CompositeDisposable);

            // アイテム追加コマンド
            this.AddCommand =
                this.MakeCommand(
                    () =>
                    {
                        this.Items.Value.Add(new TalkTextReplaceItem());
                        this.SelectedIndex.Value = this.Items.Value.Count - 1;
                    },
                    canModify);

            // プリセットアイテム追加コマンド
            this.AddPresetCommand =
                this.MakeCommand<TalkTextReplacePreset>(
                    this.ExecuteAddPresetCommand,
                    canModify);

            var collectionChanged =
                items.Select(i => i.CollectionChangedAsObservable()).Switch();

            // コレクションが空でなくなったらアイテム選択
            collectionChanged
                .Where(_ => items.Value.Count > 0 && this.SelectedIndex.Value < 0)
                .Subscribe(_ => this.SelectedIndex.Value = 0)
                .AddTo(this.CompositeDisposable);

            // コレクションまたは選択中インデックスが変化した場合に Unit を発行する
            var itemsNotifier =
                new[]
                {
                    collectionChanged.ToUnit(),
                    this.SelectedIndex.ToUnit(),
                }
                .Merge();

            // アイテム削除コマンド
            this.RemoveCommand =
                this.MakeCommand(
                    this.ExecuteRemoveCommand,
                    canModify,
                    itemsNotifier
                        .Select(
                            _ =>
                                this.SelectedIndex.Value >= 0 &&
                                this.SelectedIndex.Value < this.Items.Value.Count));

            // アイテムクリアコマンド
            this.ClearCommand =
                this.MakeCommand(
                    this.Items.Value.Clear,
                    canModify,
                    itemsNotifier.Select(_ => this.Items.Value.Count > 0));

            // アイテム上移動コマンド
            this.UpCommand =
                this.MakeCommand(
                    () =>
                        this.Items.Value.Move(
                            this.SelectedIndex.Value,
                            this.SelectedIndex.Value - 1),
                    canModify,
                    itemsNotifier
                        .Select(
                            _ =>
                                this.SelectedIndex.Value > 0 &&
                                this.SelectedIndex.Value < this.Items.Value.Count));

            // アイテム下移動コマンド
            this.DownCommand =
                this.MakeCommand(
                    () =>
                        this.Items.Value.Move(
                            this.SelectedIndex.Value,
                            this.SelectedIndex.Value + 1),
                    canModify,
                    itemsNotifier
                        .Select(
                            _ =>
                                this.SelectedIndex.Value >= 0 &&
                                this.SelectedIndex.Value + 1 < this.Items.Value.Count));
        }

        /// <summary>
        /// アイテムコレクションを取得する。
        /// </summary>
        public IReadOnlyReactiveProperty<TalkTextReplaceItemCollection> Items =>
            this.BaseConfig;

        /// <summary>
        /// 選択中アイテムインデックスを取得する。
        /// </summary>
        public IReactiveProperty<int> SelectedIndex { get; }

        /// <summary>
        /// プリセットコレクションを取得する。
        /// </summary>
        public ObservableCollection<TalkTextReplacePreset> Presets { get; } =
            new ObservableCollection<TalkTextReplacePreset>();

        /// <summary>
        /// アイテム追加コマンドを取得する。
        /// </summary>
        public ICommand AddCommand { get; }

        /// <summary>
        /// プリセット追加コマンドを取得する。
        /// </summary>
        public ICommand AddPresetCommand { get; }

        /// <summary>
        /// アイテム削除コマンドを取得する。
        /// </summary>
        public ICommand RemoveCommand { get; }

        /// <summary>
        /// アイテムクリアコマンドを取得する。
        /// </summary>
        public ICommand ClearCommand { get; }

        /// <summary>
        /// アイテム上移動コマンドを取得する。
        /// </summary>
        public ICommand UpCommand { get; }

        /// <summary>
        /// アイテム下移動コマンドを取得する。
        /// </summary>
        public ICommand DownCommand { get; }

        /// <summary>
        /// AddPresetCommand の実処理を行う。
        /// </summary>
        /// <param name="preset">プリセット。</param>
        private void ExecuteAddPresetCommand(TalkTextReplacePreset preset)
        {
            if (preset == null)
            {
                return;
            }

            foreach (var p in preset.Items)
            {
                // 置換元文字列が同じアイテムを探す
                var found =
                    this.Items.Value
                        .Select((item, index) => new { item, index })
                        .FirstOrDefault(v => v.item.OldValue == p.OldValue);

                if (found == null)
                {
                    // 無いので新規追加
                    this.Items.Value.Add(p.Clone());
                    this.SelectedIndex.Value = this.Items.Value.Count - 1;
                }
                else
                {
                    // あるので上書き
                    this.Items.Value[found.index] = p.Clone();
                    this.SelectedIndex.Value = found.index;
                }
            }
        }

        /// <summary>
        /// RemoveCommand の実処理を行う。
        /// </summary>
        private void ExecuteRemoveCommand()
        {
            var index = this.SelectedIndex.Value;
            if (index < 0 || index >= this.Items.Value.Count)
            {
                return;
            }

            this.Items.Value.RemoveAt(index);
            this.SelectedIndex.Value = Math.Min(index, this.Items.Value.Count - 1);
        }
    }
}
