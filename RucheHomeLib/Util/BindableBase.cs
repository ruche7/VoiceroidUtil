using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;

namespace RucheHome.Util
{
    /// <summary>
    /// プロパティ変更通知をサポートするクラスの抽象基底クラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public abstract class BindableBase : INotifyPropertyChanged
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        protected BindableBase()
        {
        }

        /// <summary>
        /// 複数スレッドからプロパティを更新する際の
        /// PropertyChanged イベント通知に用いる同期コンテキストを取得または設定する。
        /// </summary>
        /// <remarks>
        /// 複数スレッドから操作することがない場合、設定する必要はない。
        /// </remarks>
        public SynchronizationContext SynchronizationContext { get; set; } = null;

        /// <summary>
        /// プロパティ値を設定し、変更をイベント通知する。
        /// </summary>
        /// <typeparam name="T">プロパティ値の型。</typeparam>
        /// <param name="field">設定先フィールド。</param>
        /// <param name="value">設定値。</param>
        /// <param name="propertyName">
        /// プロパティ名。 CallerMemberNameAttribute により自動設定される。
        /// </param>
        protected void SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                this.RaisePropertyChanged(propertyName);
            }
        }

        /// <summary>
        /// PropertyChanged イベントを発生させる。
        /// </summary>
        /// <param name="propertyName">
        /// プロパティ名。指定しなければ CallerMemberNameAttribute により自動設定される。
        /// </param>
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            void invoker() =>
                this.PropertyChanged?.Invoke(
                    this,
                    new PropertyChangedEventArgs(propertyName));

            var context = this.SynchronizationContext;
            if (context == null || context == SynchronizationContext.Current)
            {
                // 同期不要 or 同一スレッド なのでそのまま実行
                invoker();
            }
            else
            {
                // 同期コンテキストへポスト
                context.Post(_ => invoker(), null);
            }
        }

        #region INotifyPropertyChanged の実装

        /// <summary>
        /// プロパティ値の変更時に呼び出されるイベント。
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
