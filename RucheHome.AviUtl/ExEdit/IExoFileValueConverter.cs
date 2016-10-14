using System;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// .NETオブジェクト値と拡張編集オブジェクトファイルの文字列値との間の
    /// 変換処理を提供するインタフェース。
    /// </summary>
    public interface IExoFileValueConverter
    {
        /// <summary>
        /// .NETオブジェクト値を拡張編集オブジェクトファイルの文字列値に変換する。
        /// </summary>
        /// <param name="value">.NETオブジェクト値。</param>
        /// <param name="objectType">.NETオブジェクトの型情報。</param>
        /// <returns>文字列値。変換できなかった場合は null 。</returns>
        string ToExoFileValue(object value, Type objectType);

        /// <summary>
        /// 拡張編集オブジェクトファイルの文字列値を.NETオブジェクト値に変換する。
        /// </summary>
        /// <param name="value">文字列値。</param>
        /// <param name="objectType">.NETオブジェクトの型情報。</param>
        /// <returns>
        /// .NETオブジェクト値を持つタプル。変換できなかったならば null 。
        /// </returns>
        Tuple<object> FromExoFileValue(string value, Type objectType);
    }
}
