using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

using static RucheHome.Util.ArgumentValidater;

namespace RucheHome.Util
{
    /// <summary>
    /// ConfigKeeper{T} ジェネリッククラスで扱われる、
    /// プロパティ変更通知付き設定の抽象基底クラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public abstract class BindableConfigBase : BindableBase, IExtensibleDataObject
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        protected BindableConfigBase() : base()
        {
        }

        /// <summary>
        /// INotifyPropertyChanged インタフェース実装型プロパティ値を設定し、
        /// PropertyChanged イベントの伝搬設定を行い、変更をイベント通知する。
        /// </summary>
        /// <typeparam name="T">
        /// プロパティ値の型。 INotifyPropertyChanged インタフェースを実装していること。
        /// </typeparam>
        /// <param name="field">設定先フィールド。</param>
        /// <param name="value">設定値。</param>
        /// <param name="propertyName">
        /// プロパティ名。 CallerMemberNameAttribute により自動設定される。
        /// </param>
        protected void SetPropertyWithPropertyChangedChain<T>(
            ref T field,
            T value,
            [CallerMemberName] string propertyName = null)
            where T : INotifyPropertyChanged
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                this.RemovePropertyChangedChain(ref field, propertyName);
                field = value;
                this.AddPropertyChangedChain(ref field, propertyName);

                this.RaisePropertyChanged(propertyName);
            }
        }

        /// <summary>
        /// INotifyCollectionChanged インタフェース実装型プロパティ値を設定し、
        /// PropertyChanged イベントの伝搬設定を行い、変更をイベント通知する。
        /// </summary>
        /// <typeparam name="T">
        /// プロパティ値の型。 INotifyCollectionChanged インタフェースを実装していること。
        /// </typeparam>
        /// <param name="field">設定先フィールド。</param>
        /// <param name="value">設定値。</param>
        /// <param name="propertyName">
        /// プロパティ名。 CallerMemberNameAttribute により自動設定される。
        /// </param>
        protected void SetPropertyWithCollectionChangedChain<T>(
            ref T field,
            T value,
            [CallerMemberName] string propertyName = null)
            where T : INotifyCollectionChanged
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                this.RemoveCollectionChangedChain(ref field, propertyName);
                field = value;
                this.AddCollectionChangedChain(ref field, propertyName);

                this.RaisePropertyChanged(propertyName);
            }
        }

        /// <summary>
        /// INotifyPropertyChanged インタフェース実装型プロパティの変更通知を
        /// このオブジェクトの PropertyChanged イベントに伝搬させるように設定する。
        /// </summary>
        /// <typeparam name="T">
        /// プロパティ値の型。 INotifyPropertyChanged インタフェースを実装していること。
        /// </typeparam>
        /// <param name="field">設定先フィールド。</param>
        /// <param name="propertyName">
        /// プロパティ名。 CallerMemberNameAttribute により自動設定される。
        /// </param>
        protected void AddPropertyChangedChain<T>(
            ref T field,
            [CallerMemberName] string propertyName = null)
            where T : INotifyPropertyChanged
        {
            if (field != null)
            {
                field.PropertyChanged +=
                    this.GetPropertyChangedChainDelegate(propertyName);
            }
        }

        /// <summary>
        /// INotifyPropertyChanged インタフェース実装型プロパティの変更通知を
        /// このオブジェクトの PropertyChanged イベントに伝搬させる設定を解除する。
        /// </summary>
        /// <typeparam name="T">
        /// プロパティ値の型。 INotifyPropertyChanged インタフェースを実装していること。
        /// </typeparam>
        /// <param name="field">設定解除先フィールド。</param>
        /// <param name="propertyName">
        /// プロパティ名。 CallerMemberNameAttribute により自動設定される。
        /// </param>
        protected void RemovePropertyChangedChain<T>(
            ref T field,
            [CallerMemberName] string propertyName = null)
            where T : INotifyPropertyChanged
        {
            if (field != null)
            {
                field.PropertyChanged -=
                    this.GetPropertyChangedChainDelegate(propertyName);
            }
        }

        /// <summary>
        /// INotifyCollectionChanged インタフェース実装型プロパティのコレクション変更通知を
        /// このオブジェクトの PropertyChanged イベントに伝搬させるように設定する。
        /// </summary>
        /// <typeparam name="T">
        /// プロパティ値の型。 INotifyCollectionChanged インタフェースを実装していること。
        /// </typeparam>
        /// <param name="field">設定先フィールド。</param>
        /// <param name="propertyName">
        /// プロパティ名。 CallerMemberNameAttribute により自動設定される。
        /// </param>
        protected void AddCollectionChangedChain<T>(
            ref T field,
            [CallerMemberName] string propertyName = null)
            where T : INotifyCollectionChanged
        {
            if (field != null)
            {
                field.CollectionChanged +=
                    this.GetCollectionChangedChainDelegate(propertyName);
            }
        }

        /// <summary>
        /// INotifyCollectionChanged インタフェース実装型プロパティのコレクション変更通知を
        /// このオブジェクトの PropertyChanged イベントに伝搬させる設定を解除する。
        /// </summary>
        /// <typeparam name="T">
        /// プロパティ値の型。 INotifyCollectionChanged インタフェースを実装していること。
        /// </typeparam>
        /// <param name="field">設定解除先フィールド。</param>
        /// <param name="propertyName">
        /// プロパティ名。 CallerMemberNameAttribute により自動設定される。
        /// </param>
        protected void RemoveCollectionChangedChain<T>(
            ref T field,
            [CallerMemberName] string propertyName = null)
            where T : INotifyCollectionChanged
        {
            if (field != null)
            {
                field.CollectionChanged -=
                    this.GetCollectionChangedChainDelegate(propertyName);
            }
        }

        /// <summary>
        /// BindableCollection{TItem} 派生クラスプロパティの各種変更通知を
        /// このオブジェクトの PropertyChanged イベントに伝搬させるように設定する。
        /// </summary>
        /// <typeparam name="TItem">プロパティ値の要素型。</typeparam>
        /// <param name="field">設定解除先フィールド。</param>
        /// <param name="propertyName">
        /// プロパティ名。 CallerMemberNameAttribute により自動設定される。
        /// </param>
        protected void AddBindableCollectionEventChain<TItem>(
            BindableCollection<TItem> field,
            [CallerMemberName] string propertyName = null)
            where TItem : class, INotifyPropertyChanged
        {
            if (field != null)
            {
                field.ItemPropertyChanged +=
                    this.GetPropertyChangedChainDelegate(propertyName);
                field.CollectionChanged +=
                    this.GetCollectionChangedChainDelegate(propertyName);
            }
        }

        /// <summary>
        /// BindableCollection{TItem} 派生クラスプロパティの各種変更通知を
        /// このオブジェクトの PropertyChanged イベントに伝搬させる設定を解除する。
        /// </summary>
        /// <typeparam name="TItem">プロパティ値の要素型。</typeparam>
        /// <param name="field">設定解除先フィールド。</param>
        /// <param name="propertyName">
        /// プロパティ名。 CallerMemberNameAttribute により自動設定される。
        /// </param>
        protected void RemoveBindableCollectionEventChain<TItem>(
            BindableCollection<TItem> field,
            [CallerMemberName] string propertyName = null)
            where TItem : class, INotifyPropertyChanged
        {
            if (field != null)
            {
                field.ItemPropertyChanged -=
                    this.GetPropertyChangedChainDelegate(propertyName);
                field.CollectionChanged -=
                    this.GetCollectionChangedChainDelegate(propertyName);
            }
        }

        /// <summary>
        /// DataMemberAttribute 属性の付与されたプロパティおよびフィールドを、
        /// 既定のコンストラクタを呼び出した直後の値で上書きする。
        /// </summary>
        /// <param name="args">コンストラクタ引数配列。</param>
        /// <remarks>
        /// コンストラクタが存在しない場合は例外が送出される。
        /// </remarks>
        protected void ResetDataMembers(params object[] args)
            =>
            this.CopyDataMembersFrom(
                Activator.CreateInstance(
                    this.GetType(),
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    args,
                    null));

        /// <summary>
        /// DataMemberAttribute 属性の付与されたプロパティおよびフィールドをコピーする。
        /// </summary>
        /// <param name="src">コピー元。</param>
        protected void CopyDataMembersFrom(object src)
        {
            ValidateArgumentNull(src, nameof(src));
            if (!this.GetType().IsAssignableFrom(src.GetType()))
            {
                throw new ArgumentException(@"The value is an invalid type.", nameof(src));
            }

            // プロパティ上書き
            foreach (var pi in this.GetDataMemberPropertyInfos())
            {
                pi.SetValue(this, pi.GetValue(src));
            }

            // フィールド上書き
            foreach (var fi in this.GetDataMemberFieldInfos())
            {
                fi.SetValue(this, fi.GetValue(src));
            }
        }

        /// <summary>
        /// DataMemberAttribute 属性が設定されており、
        /// かつ読み書き可能なプロパティ群の PropertyInfo 配列を取得する。
        /// </summary>
        /// <returns>PropertyInfo 配列。</returns>
        protected PropertyInfo[] GetDataMemberPropertyInfos()
        {
            if (this.dataMemberPropertyInfosCache == null)
            {
                this.dataMemberPropertyInfosCache =
                    this.GetType()
                        .GetProperties(
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic)
                        .Where(
                            i =>
                                i.IsDefined(typeof(DataMemberAttribute)) &&
                                i.CanRead &&
                                i.CanWrite)
                        .ToArray();
            }

            return this.dataMemberPropertyInfosCache;
        }
        private PropertyInfo[] dataMemberPropertyInfosCache = null;

        /// <summary>
        /// DataMemberAttribute 属性が設定されているフィールド群の FieldInfo 配列を取得する。
        /// </summary>
        /// <returns>FieldInfo 配列。</returns>
        protected FieldInfo[] GetDataMemberFieldInfos()
        {
            if (this.dataMemberFieldInfosCache == null)
            {
                this.dataMemberFieldInfosCache =
                    this.GetType()
                        .GetFields(
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic)
                        .Where(i => i.IsDefined(typeof(DataMemberAttribute)))
                        .ToArray();
            }

            return this.dataMemberFieldInfosCache;
        }
        private FieldInfo[] dataMemberFieldInfosCache = null;

        /// <summary>
        /// PropertyChanged イベント伝搬用デリゲートディクショナリ。
        /// </summary>
        private Dictionary<string, PropertyChangedEventHandler>
        PropertyChangedChainDelegates = null;

        /// <summary>
        /// PropertyChanged イベント伝搬用デリゲートを取得する。
        /// </summary>
        /// <param name="propertyName">プロパティ名。</param>
        private PropertyChangedEventHandler GetPropertyChangedChainDelegate(
            string propertyName)
        {
            if (this.PropertyChangedChainDelegates == null)
            {
                this.PropertyChangedChainDelegates =
                    new Dictionary<string, PropertyChangedEventHandler>();
            }

            if (
                !this.PropertyChangedChainDelegates.TryGetValue(
                    propertyName,
                    out var result))
            {
                result = (s, e) => this.RaisePropertyChanged(propertyName);
                this.PropertyChangedChainDelegates.Add(propertyName, result);
            }

            return result;
        }

        /// <summary>
        /// CollectionChanged イベント伝搬用デリゲートディクショナリ。
        /// </summary>
        private Dictionary<string, NotifyCollectionChangedEventHandler>
        CollectionChangedChainDelegates = null;

        /// <summary>
        /// CollectionChanged イベント伝搬用デリゲートを取得する。
        /// </summary>
        /// <param name="propertyName">プロパティ名。</param>
        private NotifyCollectionChangedEventHandler GetCollectionChangedChainDelegate(
            string propertyName)
        {
            if (this.CollectionChangedChainDelegates == null)
            {
                this.CollectionChangedChainDelegates =
                    new Dictionary<string, NotifyCollectionChangedEventHandler>();
            }

            if (
                !this.CollectionChangedChainDelegates.TryGetValue(
                    propertyName,
                    out var result))
            {
                result = (s, e) => this.RaisePropertyChanged(propertyName);
                this.CollectionChangedChainDelegates.Add(propertyName, result);
            }

            return result;
        }

        #region IExtensibleDataObject の明示的実装

        ExtensionDataObject IExtensibleDataObject.ExtensionData { get; set; }

        #endregion
    }
}
