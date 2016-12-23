using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using RucheHome.Util;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// 移動可能な数値を表すジェネリッククラス。
    /// </summary>
    /// <typeparam name="TConstants">定数情報型。</typeparam>
    [DataContract(Namespace = "")]
    public class MovableValue<TConstants>
        : BindableBase, IMovableValue, IEquatable<MovableValue<TConstants>>, ICloneable
        where TConstants : IMovableValueConstants, new()
    {
        #region 静的定義群

        /// <summary>
        /// この型の定数情報。
        /// </summary>
        public static readonly IMovableValueConstants ThisConstants = new TConstants();

        /// <summary>
        /// 拡張編集オブジェクトファイルにおける文字列表現値のパースを行う。
        /// </summary>
        /// <typeparam name="T">MovableValueBase 派生クラス型。</typeparam>
        /// <param name="value">文字列表現値。</param>
        /// <returns>パース結果。</returns>
        public static MovableValue<TConstants> Parse(string value)
        {
            MovableValue<TConstants> result;
            if (!TryParse(value, out result))
            {
                throw new ArgumentException(@"Invalid format.", nameof(value));
            }

            return result;
        }

        /// <summary>
        /// 拡張編集オブジェクトファイルにおける文字列表現値のパースを試みる。
        /// </summary>
        /// <typeparam name="T">MovableValueBase 派生クラス型。</typeparam>
        /// <param name="value">文字列表現値。</param>
        /// <param name="result">パース結果の設定先。</param>
        /// <returns>パースに成功したならば true 。そうでなければ false 。</returns>
        public static bool TryParse(string value, out MovableValue<TConstants> result)
        {
            result = null;

            if (value == null)
            {
                return false;
            }

            var vals = value.Split(',');
            if (vals.Length < 1)
            {
                return false;
            }

            // 開始値
            decimal begin;
            if (!decimal.TryParse(vals[0], out begin))
            {
                return false;
            }

            if (vals.Length == 1)
            {
                result = new MovableValue<TConstants>(begin);
            }
            else if (vals.Length == 3 || vals.Length == 4)
            {
                // 終端値
                decimal end;
                if (!decimal.TryParse(vals[1], out end))
                {
                    return false;
                }

                // 移動モード＆加減速
                MoveMode moveMode = MoveMode.None;
                bool accel = false;
                bool decel = false;
                if (!TryParseMoveMode(vals[2], out moveMode, out accel, out decel))
                {
                    return false;
                }

                // 追加パラメータ
                int param = 0;
                if (vals.Length == 4)
                {
                    if (!int.TryParse(vals[3], out param))
                    {
                        return false;
                    }
                }

                result =
                    new MovableValue<TConstants>(
                        begin,
                        end,
                        moveMode,
                        accel,
                        decel,
                        param);
            }
            else
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 移動モードの文字列表現値のパースを試みる。
        /// </summary>
        /// <param name="value">文字列表現値。</param>
        /// <param name="moveMode">移動モードの設定先。</param>
        /// <param name="accelerating">加速を行うか否かの設定先。</param>
        /// <param name="decelerating">減速を行うか否かの設定先。</param>
        /// <returns>パースに成功したならば true 。そうでなければ false 。</returns>
        private static bool TryParseMoveMode(
            string value,
            out MoveMode moveMode,
            out bool accelerating,
            out bool decelerating)
        {
            Debug.Assert(value != null);

            moveMode = MoveMode.None;
            accelerating = false;
            decelerating = false;

            var idStr = value;
            var extra = "";
            var nonDigitPos =
                value
                    .Select((c, index) => new { c, index })
                    .Skip(1)
                    .FirstOrDefault(v => !char.IsDigit(v.c))?
                    .index;
            if (nonDigitPos.HasValue)
            {
                idStr = value.Substring(0, nonDigitPos.Value);
                extra = value.Substring(nonDigitPos.Value);
            }

            int id = 0;
            if (!int.TryParse(idStr, out id))
            {
                return false;
            }

            if (id >= 64)
            {
                accelerating = true;
                id -= 64;
            }
            if (id >= 32)
            {
                decelerating = true;
                id -= 32;
            }

            if (id == MoveMode.None.GetId())
            {
                // None を文字列表現することはできない
                return false;
            }

            var foundType =
                (Enum.GetValues(typeof(MoveMode)) as MoveMode[])
                    .Cast<MoveMode?>()
                    .FirstOrDefault(t => t?.GetId() == id && t?.GetExtraId() == extra);
            if (!foundType.HasValue)
            {
                return false;
            }
            moveMode = foundType.Value;

            return true;
        }

        /// <summary>
        /// 定数情報に従って値を補正する。
        /// </summary>
        /// <param name="value">値。</param>
        /// <returns>補正された値。</returns>
        private static decimal CorrectValue(decimal value) =>
            decimal.Round(
                Math.Min(Math.Max(ThisConstants.MinValue, value), ThisConstants.MaxValue),
                ThisConstants.Digits);

        #endregion

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public MovableValue() : this(ThisConstants.DefaultValue)
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="value">初期値。</param>
        public MovableValue(decimal value) : this(value, value, MoveMode.None)
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="begin">開始値。</param>
        /// <param name="end">終端値。</param>
        /// <param name="moveMode">移動モード。</param>
        /// <param name="accelerating">加速を行うならば true 。</param>
        /// <param name="decelerating">減速を行うならば true 。</param>
        /// <param name="interval">移動フレーム間隔。</param>
        public MovableValue(
            decimal begin,
            decimal end,
            MoveMode moveMode,
            bool accelerating = false,
            bool decelerating = false,
            int interval = 0)
            : base()
        {
            this.Begin = begin;
            this.End = end;
            this.MoveMode = moveMode;
            this.IsAccelerating = accelerating;
            this.IsDecelerating = decelerating;
            this.Interval = interval;
        }

        /// <summary>
        /// コピーコンストラクタ。
        /// </summary>
        /// <param name="src">コピー元。</param>
        public MovableValue(MovableValue<TConstants> src)
        {
            if (src == null)
            {
                throw new ArgumentNullException(nameof(src));
            }

            this.Begin = src.Begin;
            this.End = src.End;
            this.MoveMode = src.MoveMode;
            this.IsAccelerating = src.IsAccelerating;
            this.IsDecelerating = src.IsDecelerating;
            this.Interval = src.Interval;
        }

        /// <summary>
        /// 定数情報を取得する。
        /// </summary>
        public IMovableValueConstants Constants => ThisConstants;

        /// <summary>
        /// 開始値を取得または設定する。
        /// </summary>
        [DataMember]
        public decimal Begin
        {
            get { return this.begin; }
            set { this.SetProperty(ref this.begin, CorrectValue(value)); }
        }
        private decimal begin = 0;

        /// <summary>
        /// 終端値を取得または設定する。
        /// </summary>
        /// <remarks>
        /// 移動モードが MoveMode.None の場合は無視される。
        /// </remarks>
        [DataMember]
        public decimal End
        {
            get { return this.end; }
            set { this.SetProperty(ref this.end, CorrectValue(value)); }
        }
        private decimal end = 0;

        /// <summary>
        /// 移動モードを取得または設定する。
        /// </summary>
        public MoveMode MoveMode
        {
            get { return this.moveMode; }
            set
            {
                this.SetProperty(
                    ref this.moveMode,
                    Enum.IsDefined(value.GetType(), value) ? value : MoveMode.None);
            }
        }
        private MoveMode moveMode = MoveMode.None;

        /// <summary>
        /// MoveMode プロパティのシリアライズ用ラッパプロパティ。
        /// </summary>
        [DataMember(Name = nameof(MoveMode))]
        private string MoveModeString
        {
            get { return this.MoveMode.ToString(); }
            set
            {
                MoveMode type;
                this.MoveMode = Enum.TryParse(value, out type) ? type : MoveMode.None;
            }
        }

        /// <summary>
        /// 加速を行うか否かを取得または設定する。
        /// </summary>
        /// <remarks>
        /// 移動モードが加減速指定不可ならば無視される。
        /// </remarks>
        [DataMember]
        public bool IsAccelerating
        {
            get { return this.accelerating; }
            set { this.SetProperty(ref this.accelerating, value); }
        }
        private bool accelerating = false;

        /// <summary>
        /// 減速を行うか否かを取得または設定する。
        /// </summary>
        /// <remarks>
        /// 移動モードが加減速指定不可ならば無視される。
        /// </remarks>
        [DataMember]
        public bool IsDecelerating
        {
            get { return this.decelerating; }
            set { this.SetProperty(ref this.decelerating, value); }
        }
        private bool decelerating = false;

        /// <summary>
        /// 移動フレーム間隔を取得または設定する。
        /// </summary>
        /// <remarks>
        /// 移動モードが移動フレーム間隔設定を持たないならば無視される。
        /// </remarks>
        [DataMember]
        public int Interval
        {
            get { return this.interval; }
            set { this.SetProperty(ref this.interval, value); }
        }
        private int interval = 0;

        /// <summary>
        /// このオブジェクトのクローンを作成する。
        /// </summary>
        /// <returns>クローン。</returns>
        public MovableValue<TConstants> Clone() => new MovableValue<TConstants>(this);

        #region Object のオーバライド

        /// <summary>
        /// このオブジェクトと他のオブジェクトが等価であるか否かを調べる。
        /// </summary>
        /// <param name="obj">調べるオブジェクト。</param>
        /// <returns>等価ならば true 。そうでなければ false 。</returns>
        public override bool Equals(object obj) =>
            this.Equals(obj as MovableValue<TConstants>);

        /// <summary>
        /// このオブジェクトのハッシュコード値を取得する。
        /// </summary>
        /// <returns>ハッシュコード値。</returns>
        public override int GetHashCode() =>
            this.Begin.GetHashCode() ^
            this.End.GetHashCode() ^
            this.MoveMode.GetHashCode();

        /// <summary>
        /// 拡張編集オブジェクトファイルにおける文字列表現値を取得する。
        /// </summary>
        /// <returns>拡張編集オブジェクトファイルにおける文字列表現値。</returns>
        public override string ToString()
        {
            string result = null;

            var valueFormat = @"F" + this.Constants.Digits;
            var begin = this.Begin.ToString(valueFormat);

            if (this.MoveMode == MoveMode.None)
            {
                result = begin;
            }
            else
            {
                var end = this.End.ToString(valueFormat);

                var id = this.MoveMode.GetId();
                if (this.MoveMode.CanAccelerate())
                {
                    id +=
                        (this.IsAccelerating ? 64 : 0) +
                        (this.IsDecelerating ? 32 : 0);
                }

                result = $@"{begin},{end},{id}{this.MoveMode.GetExtraId()}";

                if (this.MoveMode.HasInterval() && this.Interval != 0)
                {
                    result += $@",{this.Interval}";
                }
            }

            return result;
        }

        #endregion

        #region IEquatable<MovableValue<TConstants>> の実装

        /// <summary>
        /// このオブジェクトと他のオブジェクトが等価であるか否かを調べる。
        /// </summary>
        /// <param name="other">調べるオブジェクト。</param>
        /// <returns>等価ならば true 。そうでなければ false 。</returns>
        public bool Equals(MovableValue<TConstants> other) =>
            other != null &&
            this.Begin == other.Begin &&
            this.End == other.End &&
            this.MoveMode == other.MoveMode &&
            this.IsAccelerating == other.IsAccelerating &&
            this.IsDecelerating == other.IsDecelerating &&
            this.Interval == other.Interval;

        #endregion

        #region ICloneable の明示的実装

        /// <summary>
        /// このオブジェクトのクローンを作成する。
        /// </summary>
        /// <returns>クローン。</returns>
        object ICloneable.Clone() => this.Clone();

        #endregion
    }
}
