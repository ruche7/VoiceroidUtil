using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RucheHome.Windows.Mvvm.Converters
{
    /// <summary>
    /// 真偽値を反転させるクラス。
    /// </summary>
    public class BooleanInverter : IValueConverter
    {
        /// <summary>
        /// 真偽値を反転させる。
        /// </summary>
        /// <param name="value">真偽値。</param>
        /// <param name="targetType">無視される。</param>
        /// <param name="parameter">無視される。</param>
        /// <param name="culture">無視される。</param>
        /// <returns>
        /// 真偽値を反転させた値。失敗した場合は DependencyProperty.UnsetValue 。
        /// </returns>
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value is bool v)
            {
                return !v;
            }

            var nv = value as bool?;
            return nv.HasValue ? (bool?)!nv.Value : DependencyProperty.UnsetValue;
        }

        /// <summary>
        /// 真偽値を反転させる。
        /// </summary>
        /// <param name="value">真偽値。</param>
        /// <param name="targetType">無視される。</param>
        /// <param name="parameter">無視される。</param>
        /// <param name="culture">無視される。</param>
        /// <returns>
        /// 真偽値を反転させた値。失敗した場合は DependencyProperty.UnsetValue 。
        /// </returns>
        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
            =>
            this.Convert(value, targetType, parameter, culture);
    }
}
