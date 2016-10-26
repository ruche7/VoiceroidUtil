using System;
using System.Globalization;
using System.Reflection;
using System.Windows.Media;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// .NETオブジェクト値と拡張編集オブジェクトファイルの文字列値との間の
    /// 既定の変換処理を提供するクラス。
    /// </summary>
    public class DefaultExoFileValueConverter : IExoFileValueConverter
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public DefaultExoFileValueConverter()
        {
        }

        /// <summary>
        /// .NETオブジェクト値を拡張編集オブジェクトファイルの文字列値に変換する。
        /// </summary>
        /// <param name="value">.NETオブジェクト値。</param>
        /// <param name="objectType">.NETオブジェクトの型情報。</param>
        /// <returns>文字列値。変換できなかった場合は null 。</returns>
        /// <remarks>
        /// <para>既定では次のように処理する。</para>
        /// <list type="number">
        /// <item><description>
        /// value の型が string ならば、 value をそのまま返す。
        /// </description></item>
        /// <item><description>
        /// value の型が bool ならば、 true を "1" 、 false を "0" に変換して返す。
        /// </description></item>
        /// <item><description>
        /// value の型が列挙型ならば、基になる整数型に変換し、その文字列表現値を返す。
        /// </description></item>
        /// <item><description>
        /// value の型が System.Windows.Media.Color ならば、
        /// RGB値から6文字の16進数文字列値を作成して返す。
        /// </description></item>
        /// <item><description>
        /// value?.ToString() を返す。
        /// </description></item>
        /// </remarks>
        public virtual string ToExoFileValue(object value, Type objectType)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }

            // string
            if (value is string)
            {
                return (string)value;
            }

            // bool
            if (value is bool)
            {
                return (bool)value ? @"1" : @"0";
            }

            // enum
            if (objectType.IsEnum)
            {
                try
                {
                    return
                        Convert
                            .ChangeType(value, Enum.GetUnderlyingType(objectType))
                            .ToString();
                }
                catch { }
                return null;
            }

            // Color
            if (value is Color)
            {
                var color = (Color)value;
                return $"{color.R:x2}{color.G:x2}{color.B:x2}";
            }

            // ToString
            return value?.ToString();
        }

        /// <summary>
        /// 拡張編集オブジェクトファイルの文字列値を.NETオブジェクト値に変換する。
        /// </summary>
        /// <param name="value">文字列値。</param>
        /// <param name="objectType">.NETオブジェクトの型情報。</param>
        /// <returns>
        /// .NETオブジェクト値を持つタプル。変換できなかったならば null 。
        /// </returns>
        /// <remarks>
        /// <para>既定では次のように処理する。</para>
        /// <list type="number">
        /// <item><description>
        /// .NETオブジェクトの型が string ならば、 value をそのまま返す。
        /// </description></item>
        /// <item><description>
        /// .NETオブジェクトの型が bool ならば、 value を整数値に変換する。
        /// 変換に成功した場合、その値が 0 ならば false を、それ以外ならば true を返す。
        /// </description></item>
        /// <item><description>
        /// .NETオブジェクトの型が列挙型ならば、まず基となる整数型に変換し、
        /// その値が列挙値として定義されているならばその列挙値を返す。
        /// </description></item>
        /// <item><description>
        /// .NETオブジェクトの型が System.Windows.Media.Color ならば、
        /// value を16進数文字列値とみなし、RGB値を作成して返す。
        /// </description></item>
        /// <item><description>
        /// .NETオブジェクトの型が静的メソッド TryParse を持つならばそれを利用する。
        /// </description></item>
        /// <item><description>
        /// Convert.ChangeType メソッド呼び出しを試みる。
        /// 例外が送出されなければ戻り値を返す。
        /// </description></item>
        /// </list>
        /// </remarks>
        public virtual Tuple<object> FromExoFileValue(string value, Type objectType)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }

            // string
            if (objectType == typeof(string))
            {
                return Tuple.Create<object>(value);
            }

            // bool
            if (objectType == typeof(bool))
            {
                int v;
                return int.TryParse(value, out v) ? Tuple.Create<object>(v != 0) : null;
            }

            // enum
            if (objectType.IsEnum)
            {
                try
                {
                    var v =
                        Convert.ChangeType(value, Enum.GetUnderlyingType(objectType));
                    return
                        Enum.IsDefined(objectType, v) ?
                            Tuple.Create(Enum.ToObject(objectType, v)) : null;
                }
                catch { }
                return null;
            }

            // Color
            if (objectType == typeof(Color))
            {
                uint v;
                bool ok =
                    uint.TryParse(
                        value,
                        NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture,
                        out v);
                if (!ok || v > 0xFFFFFF)
                {
                    return null;
                }

                byte b = (byte)(v % 256);
                v /= 256;
                byte g = (byte)(v % 256);
                v /= 256;
                byte r = (byte)v;

                return Tuple.Create<object>(Color.FromRgb(r, g, b));
            }

            // TryParse
            var tryParse =
                objectType.GetMethod(
                    @"TryParse",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(string), objectType.MakeByRefType() },
                    null);
            if (tryParse != null && tryParse.ReturnParameter.ParameterType == typeof(bool))
            {
                var args = new object[] { value, null };
                return (bool)tryParse.Invoke(null, args) ? Tuple.Create(args[1]) : null;
            }

            // Convert.ChangeType
            try
            {
                return Tuple.Create(Convert.ChangeType(value, objectType));
            }
            catch { }
            return null;
        }
    }
}
