using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RucheHome.Windows.Mvvm.Converters
{
    /// <summary>
    /// 真偽値と System.Windows.Visibility 値との変換および逆変換を行うクラス。
    /// </summary>
    public class BooleanVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 真偽値を System.Windows.Visibility 値に変換する。
        /// </summary>
        /// <param name="value">真偽値。</param>
        /// <param name="targetType">無視される。</param>
        /// <param name="parameter">無視される。</param>
        /// <param name="culture">無視される。</param>
        /// <returns>
        /// System.Windows.Visibility 値。
        /// 変換できない場合は DependencyProperty.UnsetValue 。
        /// </returns>
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
            =>
            (value is bool v) ?
                (v ? Visibility.Visible : Visibility.Collapsed) : DependencyProperty.UnsetValue;

        /// <summary>
        /// System.Windows.Visibility 値を真偽値に変換する。
        /// </summary>
        /// <param name="value">System.Windows.Visibility 値。</param>
        /// <param name="targetType">無視される。</param>
        /// <param name="parameter">無視される。</param>
        /// <param name="culture">無視される。</param>
        /// <returns>真偽値。変換できない場合は DependencyProperty.UnsetValue 。</returns>
        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
            =>
            (value is Visibility v) ? (v == Visibility.Visible) : DependencyProperty.UnsetValue;
    }
}
