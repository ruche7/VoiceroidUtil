using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using RucheHome.Windows.Mvvm.Commands;

namespace VoiceroidUtil.Mvvm
{
    /// <summary>
    /// コマンドを、コンバータパラメータが
    /// ActivateParameter と等しい時だけ実行可能なコマンドに変換するクラス。
    /// </summary>
    public class CommandActivator : DependencyObject, IValueConverter
    {
        /// <summary>
        /// ActivateParameter 依存関係プロパティ。
        /// </summary>
        public static readonly DependencyProperty ActivateParameterProperty =
            DependencyProperty.Register(
                nameof(ActivateParameter),
                typeof(object),
                typeof(CommandActivator),
                new PropertyMetadata(
                    null,
                    (sender, e) =>
                        (sender as CommandActivator)?
                            .ActivateParameterChanged?
                            .Invoke(sender, EventArgs.Empty)));

        /// <summary>
        /// コンバータパラメータと比較する値を取得または設定する。
        /// </summary>
        public object ActivateParameter
        {
            get => this.GetValue(ActivateParameterProperty);
            set => this.SetValue(ActivateParameterProperty, value);
        }

        /// <summary>
        /// ActivateParameter プロパティ値が変更された時に発生するイベント。
        /// </summary>
        public event EventHandler ActivateParameterChanged;

        /// <summary>
        /// ICommand 値またはその列挙値を、コンバータパラメータが
        /// ActivateParameter と等しい時だけ実行可能なコマンドに変換する。
        /// </summary>
        /// <param name="value">ICommand 値またはその列挙値。</param>
        /// <param name="targetType">無視される。</param>
        /// <param name="parameter">コンバータパラメータ。</param>
        /// <param name="culture">無視される。</param>
        /// <returns></returns>
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            try
            {
                if (value is ICommand command)
                {
                    var result =
                        new RelayCommand(
                            command.Execute,
                            p =>
                                Equals(this.ActivateParameter, parameter) &&
                                command.CanExecute(p));

                    // ActivateParameter が変更されたら CanExecuteChanged 発行
                    this.ActivateParameterChanged +=
                        (sender, e) => result.RaiseCanExecuteChanged();

                    return result;
                }

                if (value is IEnumerable values)
                {
                    var commands = values.Cast<object>();
                    if (commands.All(v => v is ICommand))
                    {
                        return
                            commands
                                .Select(
                                    c =>
                                        this.Convert(
                                            c,
                                            typeof(ICommand),
                                            parameter,
                                            culture))
                                .ToList();
                    }
                }
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
            throw new NotSupportedException(); // 逆変換は非サポート
    }
}
