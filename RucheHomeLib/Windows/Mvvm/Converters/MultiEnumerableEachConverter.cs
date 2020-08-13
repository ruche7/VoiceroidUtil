using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace RucheHome.Windows.Mvvm.Converters
{
    /// <summary>
    /// 複数の IEnumerable の同一インデックス要素同士に IMultiValueConverter を適用し、
    /// 結果を List{object} で返すクラス。
    /// </summary>
    /// <remarks>
    /// 各々の IEnumerable の要素数が異なる場合、一番少ない要素数まで処理する。
    /// </remarks>
    public class MultiEnumerableEachConverter : DependencyObject, IMultiValueConverter
    {
        /// <summary>
        /// Converter 依存関係プロパティ。
        /// </summary>
        public static readonly DependencyProperty ConverterProperty =
            DependencyProperty.Register(
                nameof(Converter),
                typeof(IMultiValueConverter),
                typeof(MultiEnumerableEachConverter),
                new PropertyMetadata(null));

        /// <summary>
        /// 各要素に対して適用する IMultiValueConverter を取得または設定する。
        /// </summary>
        public IMultiValueConverter Converter
        {
            get => (IMultiValueConverter)this.GetValue(ConverterProperty);
            set => this.SetValue(ConverterProperty, value);
        }

        /// <summary>
        /// IEnumerable 値配列から単一の List{object} 値に変換する。
        /// </summary>
        /// <param name="values">IEnumerable 値配列。</param>
        /// <param name="targetType">無視される。</param>
        /// <param name="parameter">コンバータに渡される。</param>
        /// <param name="culture">コンバータに渡される。</param>
        /// <returns>
        /// 各要素にコンバータを適用した結果の List{object} 値。
        /// 引数が不正な場合やコンバータが設定されていない場合は
        /// DependencyProperty.UnsetValue 。
        /// </returns>
        public object Convert(
            object[] values,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (
                this.Converter == null ||
                values?.All(v => v != null && v is IEnumerable) != true)
            {
                return DependencyProperty.UnsetValue;
            }

            var arrays =
                values.Select(v => ((IEnumerable)v).Cast<object>().ToArray()).ToArray();
            var count = arrays.Min(v => v.Length);

            var results =
                Enumerable.Range(0, count)
                    .Select(
                        i =>
                            this.Converter.Convert(
                                arrays.Select(a => a[i]).ToArray(),
                                typeof(object),
                                parameter,
                                culture))
                    .ToList();

            return
                results.Any(r => r == DependencyProperty.UnsetValue) ?
                    DependencyProperty.UnsetValue : results;
        }

        object[] IMultiValueConverter.ConvertBack(
            object value,
            Type[] targetTypes,
            object parameter,
            CultureInfo culture)
            =>
            throw new NotImplementedException(); // 逆変換は非サポート
    }
}
