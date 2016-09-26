using System;
using System.Globalization;
using System.Reflection;
using System.Windows.Media;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// コンポーネントのアイテムであるプロパティの変換処理を提供するクラス。
    /// </summary>
    public class ComponentItemConverter
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public ComponentItemConverter()
        {
        }

        /// <summary>
        /// プロパティ値を拡張編集オブジェクトファイルの文字列値に変換する。
        /// </summary>
        /// <param name="value">プロパティ値。</param>
        /// <param name="propertyType">
        /// プロパティの型情報。主に value が null の場合に利用する。
        /// </param>
        /// <returns>文字列値。変換できなかった場合は null 。</returns>
        /// <remarks>
        /// <para>既定では次のように処理する。</para>
        /// <list type="number">
        /// <item><description>
        /// プロパティの型が string ならば、 value をそのまま返す。
        /// </description></item>
        /// <item><description>
        /// プロパティの型が bool ならば、 true を "1" 、 false を "0" に変換して返す。
        /// </description></item>
        /// <item><description>
        /// プロパティの型が列挙型ならば、基になる整数型に変換し、その文字列表現値を返す。
        /// </description></item>
        /// <item><description>
        /// プロパティの型が System.Windows.Media.Color ならば、
        /// RGB値から6文字の16進数文字列値を作成して返す。
        /// </description></item>
        /// <item><description>
        /// value?.ToString() を返す。
        /// </description></item>
        /// </remarks>
        public virtual string ToExoFileValue(object value, Type propertyType)
        {
            if (propertyType == null)
            {
                throw new ArgumentNullException(nameof(propertyType));
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
            if (propertyType.IsEnum)
            {
                try
                {
                    return
                        Convert
                            .ChangeType(value, Enum.GetUnderlyingType(propertyType))
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
        /// 拡張編集オブジェクトファイルの文字列値をプロパティ値に変換する。
        /// </summary>
        /// <param name="value">文字列値。</param>
        /// <param name="propertyType">プロパティの型情報。</param>
        /// <returns>プロパティ値を持つタプル。変換できなかった場合は null 。</returns>
        /// <remarks>
        /// <para>既定では次のように処理する。</para>
        /// <list type="number">
        /// <item><description>
        /// プロパティの型が string ならば、 value をそのまま返す。
        /// </description></item>
        /// <item><description>
        /// プロパティの型が bool ならば、 value を整数値に変換する。
        /// 変換に成功した場合、その値が 0 ならば false を、それ以外ならば true を返す。
        /// </description></item>
        /// <item><description>
        /// プロパティの型が列挙型ならば、まず基となる整数型に変換し、
        /// その値が列挙値として定義されているならばその列挙値を返す。
        /// </description></item>
        /// <item><description>
        /// プロパティの型が System.Windows.Media.Color ならば、
        /// value を16進数文字列値とみなし、RGB値を作成して返す。
        /// </description></item>
        /// <item><description>
        /// プロパティの型が静的メソッド TryParse を持つならばそれを利用する。
        /// プロパティの型に変換可能な値を取得できたならばそれを返す。
        /// </description></item>
        /// <item><description>
        /// Convert.ChangeType メソッド呼び出しを試みる。
        /// 例外が送出されなければ戻り値を返す。
        /// </description></item>
        /// </list>
        /// </remarks>
        public virtual Tuple<object> FromExoFileValue(string value, Type propertyType)
        {
            if (propertyType == null)
            {
                throw new ArgumentNullException(nameof(propertyType));
            }

            // string
            if (propertyType == typeof(string))
            {
                return Tuple.Create<object>(value);
            }

            // bool
            if (propertyType == typeof(bool))
            {
                int v;
                return int.TryParse(value, out v) ? Tuple.Create<object>(v != 0) : null;
            }

            // enum
            if (propertyType.IsEnum)
            {
                try
                {
                    var v =
                        Convert.ChangeType(value, Enum.GetUnderlyingType(propertyType));
                    return
                        Enum.IsDefined(propertyType, v) ?
                            Tuple.Create(Enum.ToObject(propertyType, v)) : null;
                }
                catch { }
                return null;
            }

            // Color
            if (propertyType == typeof(Color))
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
                propertyType.GetMethod(
                    @"TryParse",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(string), propertyType.MakeByRefType() },
                    null);
            if (tryParse != null && tryParse.ReturnParameter.ParameterType == typeof(bool))
            {
                var args = new object[] { value, null };
                return (bool)tryParse.Invoke(null, args) ? Tuple.Create(args[1]) : null;
            }

            // Convert.ChangeType
            try
            {
                return Tuple.Create(Convert.ChangeType(value, propertyType));
            }
            catch { }
            return null;
        }
    }
}
