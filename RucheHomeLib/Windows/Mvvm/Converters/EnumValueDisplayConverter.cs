using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Data;

namespace RucheHome.Windows.Mvvm.Converters
{
    /// <summary>
    /// 任意の列挙型の値を、 DisplayAttribute 属性による表示文字列に変換するクラス。
    /// </summary>
    public class EnumValueDisplayConverter : IValueConverter
    {
        /// <summary>
        /// 列挙値から DisplayAttribute 属性の Name 値による表示文字列に変換する。
        /// </summary>
        /// <param name="value">列挙値。</param>
        /// <param name="targetType">無視される。</param>
        /// <param name="parameter">無視される。</param>
        /// <param name="culture">無視される。</param>
        /// <returns>
        /// DisplayAttribute 属性の Name 値による表示文字列。
        /// 定義されていない場合は DependencyProperty.UnsetValue 。
        /// </returns>
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            try
            {
                // 列挙値名取得
                var name = Enum.GetName(value.GetType(), value);
                if (name == null)
                {
                    return DependencyProperty.UnsetValue;
                }

                // 列挙値のメタデータ取得
                var info = value.GetType().GetField(name);
                if (info == null)
                {
                    return DependencyProperty.UnsetValue;
                }

                // DisplayAttribute 属性から表示文字列を取得
                return
                    info.GetCustomAttribute<DisplayAttribute>(false)?.GetName() ??
                    DependencyProperty.UnsetValue;
            }
            catch { }

            return DependencyProperty.UnsetValue;
        }

        object IValueConverter.ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
            =>
            DependencyProperty.UnsetValue; // 逆変換は非サポート
    }
}
