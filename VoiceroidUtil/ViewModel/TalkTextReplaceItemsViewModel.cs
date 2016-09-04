using System;
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
    public class TalkTextReplaceItemsViewModel : Livet.ViewModel
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
            this.AddCommand = (new ReactiveCommand()).AddTo(this.CompositeDisposable);
            this.AddCommand
                .Subscribe(_ => this.Items.Add(new TalkTextReplaceItem()))
                .AddTo(this.CompositeDisposable);

            // アイテム削除コマンド
            this.RemoveCommand =
                this.ItemListChangedNotifier
                    .Select(
                        _ =>
                            this.SelectedIndex >= 0 &&
                            this.SelectedIndex < this.Items.Count)
                    .ToReactiveCommand(false)
                    .AddTo(this.CompositeDisposable);
            this.RemoveCommand
                .Subscribe(_ => this.Items.RemoveAt(this.SelectedIndex))
                .AddTo(this.CompositeDisposable);

            // アイテム上移動コマンド
            this.UpCommand =
                this.ItemListChangedNotifier
                    .Select(
                        _ =>
                            this.SelectedIndex > 0 &&
                            this.SelectedIndex < this.Items.Count)
                    .ToReactiveCommand(false)
                    .AddTo(this.CompositeDisposable);
            this.UpCommand
                .Subscribe(
                    _ => this.Items.Move(this.SelectedIndex, this.SelectedIndex - 1))
                .AddTo(this.CompositeDisposable);

            // アイテム下移動コマンド
            this.DownCommand =
                this.ItemListChangedNotifier
                    .Select(
                        _ =>
                            this.SelectedIndex >= 0 &&
                            this.SelectedIndex + 1 < this.Items.Count)
                    .ToReactiveCommand(false)
                    .AddTo(this.CompositeDisposable);
            this.DownCommand
                .Subscribe(
                    _ => this.Items.Move(this.SelectedIndex, this.SelectedIndex + 1))
                .AddTo(this.CompositeDisposable);
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

                    // アイテムリスト状態変更通知
                    this.ItemListChangedNotifier.SwitchValue();
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
        /// アイテム追加コマンドを取得する。
        /// </summary>
        public ReactiveCommand AddCommand { get; }

        /// <summary>
        /// アイテム削除コマンドを取得する。
        /// </summary>
        public ReactiveCommand RemoveCommand { get; }

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
