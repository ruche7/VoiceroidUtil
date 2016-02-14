using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ruche.util
{
    /// <summary>
    /// 任意の列挙型の値を、 DisplayAttribute 属性による表示文字列に変換するクラス。
    /// </summary>
    public class EnumValueDisplayConverter : IValueConverter
    {
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

                // DisplayAttribute 属性を取得
                var attrs =
                    info.GetCustomAttributes(typeof(DisplayAttribute), false)
                        as DisplayAttribute[];
                if (attrs == null || attrs.Length <= 0)
                {
                    return DependencyProperty.UnsetValue;
                }

                return attrs[0].GetName() ?? DependencyProperty.UnsetValue;
            }
            catch { }

            return DependencyProperty.UnsetValue;
        }

        object IValueConverter.ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            // 逆変換は非サポート
            return DependencyProperty.UnsetValue;
        }
    }
}
