using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Collections.Specialized;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;

namespace VoiceroidUtil.ViewModel
{
    /// <summary>
    /// TalkTextReplaceItemCollection インスタンスの操作を提供する ViewModel クラス。
    /// </summary>
    public class TalkTextReplaceItemsViewModel : ViewModelBase
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public TalkTextReplaceItemsViewModel()
        {
            // アイテムコレクション
            // イベントハンドラ追加のためにプロパティ経由で設定
            this.Items = new TalkTextReplaceItemCollection();

            // アイテム追加コマンド
            this.AddCommand =
                this.MakeCommand(
                    () =>
                    {
                        this.Items.Add(new TalkTextReplaceItem());
                        this.SelectedIndex = this.Items.Count - 1;
                    });

            // プリセットアイテム追加コマンド
            this.AddPresetCommand =
                this.MakeCommand<TalkTextReplacePreset>(this.ExecuteAddPresetCommand);

            // アイテム削除コマンド
            this.RemoveCommand =
                this.MakeCommand(
                    this.ExecuteRemoveCommand,
                    this.ItemListChangedNotifier
                        .Select(
                            _ =>
                                this.SelectedIndex >= 0 &&
                                this.SelectedIndex < this.Items.Count));

            // アイテムクリアコマンド
            this.ClearCommand =
                this.MakeCommand(
                    this.Items.Clear,
                    this.ItemListChangedNotifier.Select(_ => this.Items.Count > 0));

            // アイテム上移動コマンド
            this.UpCommand =
                this.MakeCommand(
                    () => this.Items.Move(this.SelectedIndex, this.SelectedIndex - 1),
                    this.ItemListChangedNotifier
                        .Select(
                            _ =>
                                this.SelectedIndex > 0 &&
                                this.SelectedIndex < this.Items.Count));

            // アイテム下移動コマンド
            this.DownCommand =
                this.MakeCommand(
                    () => this.Items.Move(this.SelectedIndex, this.SelectedIndex + 1),
                    this.ItemListChangedNotifier
                        .Select(
                            _ =>
                                this.SelectedIndex >= 0 &&
                                this.SelectedIndex + 1 < this.Items.Count));
        }

        /// <summary>
        /// アイテムコレクションを取得または設定する。
        /// </summary>
        public TalkTextReplaceItemCollection Items
        {
            get { return this.items; }
            set
            {
                var old = this.Items;
                this.items = value ?? (new TalkTextReplaceItemCollection());
                if (this.Items != old)
                {
                    // コレクション変更時イベントハンドラ設定
                    if (old != null)
                    {
                        old.CollectionChanged -= this.OnItemsCollectionChanged;
                    }
                    this.Items.CollectionChanged += this.OnItemsCollectionChanged;

                    this.RaisePropertyChanged();

                    if (this.Items.Count > 0 && this.SelectedIndex < 0)
                    {
                        // 強制的に選択状態にする
                        this.SelectedIndex = 0;
                    }
                    else
                    {
                        // アイテムリスト状態変更通知
                        this.ItemListChangedNotifier.SwitchValue();
                    }
                }
            }
        }
        private TalkTextReplaceItemCollection items = null;

        /// <summary>
        /// 選択中アイテムインデックスを取得する。
        /// </summary>
        public int SelectedIndex
        {
            get { return this.selectedIndex; }
            set
            {
                if (value != this.selectedIndex)
                {
                    this.selectedIndex = value;
                    this.RaisePropertyChanged();

                    // アイテムリスト状態変更通知
                    this.ItemListChangedNotifier.SwitchValue();
                }
            }
        }
        private int selectedIndex = -1;

        /// <summary>
        /// プリセットコレクションを取得する。
        /// </summary>
        public ObservableCollection<TalkTextReplacePreset> Presets { get; } =
            new ObservableCollection<TalkTextReplacePreset>();

        /// <summary>
        /// アイテム追加コマンドを取得する。
        /// </summary>
        public ReactiveCommand AddCommand { get; }

        /// <summary>
        /// プリセット追加コマンドを取得する。
        /// </summary>
        public ReactiveCommand<TalkTextReplacePreset> AddPresetCommand { get; }

        /// <summary>
        /// アイテム削除コマンドを取得する。
        /// </summary>
        public ReactiveCommand RemoveCommand { get; }

        /// <summary>
        /// アイテムクリアコマンドを取得する。
        /// </summary>
        public ReactiveCommand ClearCommand { get; }

        /// <summary>
        /// アイテム上移動コマンドを取得する。
        /// </summary>
        public ReactiveCommand UpCommand { get; }

        /// <summary>
        /// アイテム下移動コマンドを取得する。
        /// </summary>
        public ReactiveCommand DownCommand { get; }

        /// <summary>
        /// アイテムリスト状態の変更を通知するオブジェクトを取得する。
        /// </summary>
        /// <remarks>
        /// 通知される値に意味は無く、変更がある度に値が切り替わる。
        /// </remarks>
        private BooleanNotifier ItemListChangedNotifier { get; } = new BooleanNotifier();

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
                    this.Items
                        .Select((item, index) => new { item, index })
                        .FirstOrDefault(v => v.item.OldValue == p.OldValue);

                if (found == null)
                {
                    // 無いので新規追加
                    this.Items.Add(p.Clone());
                    this.SelectedIndex = this.Items.Count - 1;
                }
                else
                {
                    // あるので上書き
                    this.Items[found.index] = p.Clone();
                    this.SelectedIndex = found.index;
                }
            }
        }

        /// <summary>
        /// RemoveCommand の実処理を行う。
        /// </summary>
        private void ExecuteRemoveCommand()
        {
            var index = this.SelectedIndex;
            if (index < 0 || index >= this.Items.Count)
            {
                return;
            }

            this.Items.RemoveAt(index);
            this.SelectedIndex = Math.Min(index, this.Items.Count - 1);
        }

        /// <summary>
        /// Items のコレクション内容変更時に呼び出される。
        /// </summary>
        private void OnItemsCollectionChanged(
            object sender,
            NotifyCollectionChangedEventArgs e)
        {
            // アイテムリスト状態変更通知
            this.ItemListChangedNotifier.SwitchValue();
        }
    }
}
