using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using RucheHome.Windows.Mvvm.Commands;

namespace RucheHome.Windows.Mvvm.Converters
{
    /// <summary>
    /// MultiBinding クラスにより複数バインドされた ICommand 値を、
    /// それらをすべて順番に実行する単一の ICommand 値に変換するクラス。
    /// </summary>
    public class MultiCommandConverter : IMultiValueConverter
    {
        /// <summary>
        /// ICommand 値配列から単一の ICommand 値に変換する。
        /// </summary>
        /// <param name="values">ICommand 値配列。</param>
        /// <param name="targetType">無視される。</param>
        /// <param name="parameter">無視される。</param>
        /// <param name="culture">無視される。</param>
        /// <returns>
        /// すべてのコマンドを順番に実行する ICommand 値。
        /// 引数が不正な場合は DependencyProperty.UnsetValue 。
        /// </returns>
        public object Convert(
            object[] values,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (values?.All(v => v is ICommand) != true)
            {
                return DependencyProperty.UnsetValue;
            }

            var commands = values.Cast<ICommand>().ToArray();

            return
                new RelayCommand(
                    p =>
                        Array.ForEach(
                            commands,
                            c =>
                            {
                                if (c?.CanExecute(p) == true)
                                {
                                    c.Execute(p);
                                }
                            }));
        }

        object[] IMultiValueConverter.ConvertBack(
            object value,
            Type[] targetTypes,
            object parameter,
            CultureInfo culture)
            =>
            throw new NotSupportedException(); // 逆変換は非サポート
    }
}
