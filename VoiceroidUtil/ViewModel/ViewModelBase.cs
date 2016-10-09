using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace VoiceroidUtil.ViewModel
{
    /// <summary>
    /// ViewModel のベースクラス。
    /// </summary>
    public abstract class ViewModelBase : Livet.ViewModel
    {
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
    }
}
