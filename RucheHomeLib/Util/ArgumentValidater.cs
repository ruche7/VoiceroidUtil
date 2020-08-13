using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace RucheHome.Util
{
    /// <summary>
    /// メソッド実引数の検証処理を提供する静的クラス。
    /// </summary>
    /// <remarks>
    /// このクラスを using static して利用することを想定している。
    /// </remarks>
    public static class ArgumentValidater
    {
        /// <summary>
        /// 引数値が null ならば ArgumentNullException 例外を送出する。
        /// </summary>
        /// <typeparam name="T">引数の型。</typeparam>
        /// <param name="arg">引数値。</param>
        /// <param name="argName">引数名。例外メッセージに利用される。</param>
        public static void ValidateArgumentNull<T>(T arg, string argName = null)
        {
            if (arg == null)
            {
                throw
                    (argName == null) ?
                        new ArgumentNullException() : new ArgumentNullException(argName);
            }
        }

        /// <summary>
        /// 引数値が範囲外ならば ArgumentOutOfRangeException 例外を送出する。
        /// </summary>
        /// <typeparam name="T">引数の型。</typeparam>
        /// <param name="arg">引数値。</param>
        /// <param name="minValue">最小許容値。</param>
        /// <param name="maxValue">最大許容値。</param>
        /// <param name="argName">引数名。例外メッセージに利用される。</param>
        public static void ValidateArgumentOutOfRange<T>(
            T arg,
            T minValue = default,
            T maxValue = default,
            string argName = null)
        {
            var comp = Comparer<T>.Default;

            if (comp.Compare(arg, minValue) < 0)
            {
                var message = $@"The value is less than {minValue}.";
                throw
                    (argName == null) ?
                        new ArgumentOutOfRangeException(message) :
                        new ArgumentOutOfRangeException(argName, arg, message);
            }
            if (comp.Compare(arg, maxValue) > 0)
            {
                var message = $@"The value is greater than {maxValue}";
                throw
                    (argName == null) ?
                        new ArgumentOutOfRangeException(message) :
                        new ArgumentOutOfRangeException(argName, arg, message);
            }
        }

        /// <summary>
        /// 列挙型引数値が定義外の値ならば InvalidEnumArgumentException 例外を送出する。
        /// </summary>
        /// <typeparam name="T">引数の列挙型。</typeparam>
        /// <param name="arg">引数値。</param>
        /// <param name="argName">引数名。例外メッセージに利用される。</param>
        public static void ValidateArgumentInvalidEnum<T>(T arg, string argName = null)
            where T : struct, IConvertible
        {
            if (!Enum.IsDefined(typeof(T), arg))
            {
                throw
                    (argName == null) ?
                        new InvalidEnumArgumentException() :
                        new InvalidEnumArgumentException(
                            argName,
                            Convert.ToInt32(arg),
                            typeof(T));
            }
        }

        /// <summary>
        /// 文字列引数値が null または空文字列ならば例外を送出する。
        /// </summary>
        /// <param name="arg">引数値。</param>
        /// <param name="argName">引数名。例外メッセージに利用される。</param>
        public static void ValidateArgumentNullOrEmpty(string arg, string argName = null)
        {
            ValidateArgumentNull(arg, argName);

            if (arg.Length == 0)
            {
                var message = @"The string is empty.";
                throw
                    (argName == null) ?
                        new ArgumentException(message) :
                        new ArgumentException(message, argName);
            }
        }

        /// <summary>
        /// 文字列引数値が null または空白文字のみで構成されるならば例外を送出する。
        /// </summary>
        /// <param name="arg">引数値。</param>
        /// <param name="argName">引数名。例外メッセージに利用される。</param>
        public static void ValidateArgumentNullOrWhiteSpace(
            string arg,
            string argName = null)
        {
            ValidateArgumentNull(arg, argName);

            if (string.IsNullOrWhiteSpace(arg))
            {
                var message = @"The string is blank.";
                throw
                    (argName == null) ?
                        new ArgumentException(message) :
                        new ArgumentException(message, argName);
            }
        }
    }
}
