using System;
using System.Runtime.Serialization;
using RucheHome.Text;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// 標準再生コンポーネントを表すクラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public class PlayComponent : ComponentBase
    {
        /// <summary>
        /// 拡張編集オブジェクトファイルのセクションデータからコンポーネントを作成する。
        /// </summary>
        /// <param name="section">セクションデータ。</param>
        /// <returns>コンポーネント。作成できないならば null 。</returns>
        public static PlayComponent FromExoFileSection(IniFileSection section)
        {
            return FromExoFileSectionCore(section, () => new PlayComponent());
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public PlayComponent() : base()
        {
        }

        /// <summary>
        /// コンポーネント名を取得する。
        /// </summary>
        public override string ComponentName => @"標準再生";

        /// <summary>
        /// 音量を取得または設定する。
        /// </summary>
        [ExoFileItem(@"音量", Order = 1)]
        [DataMember]
        public MovableValue<VolumeConst> Volume
        {
            get { return this.volume; }
            set
            {
                this.SetProperty(
                    ref this.volume,
                    value ?? new MovableValue<VolumeConst>());
            }
        }
        private MovableValue<VolumeConst> volume = new MovableValue<VolumeConst>();

        /// <summary>
        /// 左右バランスを取得または設定する。
        /// </summary>
        [ExoFileItem(@"左右", Order = 2)]
        [DataMember]
        public MovableValue<BalanceConst> Balance
        {
            get { return this.balance; }
            set
            {
                this.SetProperty(
                    ref this.balance,
                    value ?? new MovableValue<BalanceConst>());
            }
        }
        private MovableValue<BalanceConst> balance = new MovableValue<BalanceConst>();

        /// <summary>
        /// このコンポーネントの内容を別のコンポーネントへコピーする。
        /// </summary>
        /// <param name="target">コピー先。</param>
        public void CopyTo(PlayComponent target)
        {
            this.CopyToCore(target);
        }

        /// <summary>
        /// デシリアライズの直前に呼び出される。
        /// </summary>
        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            this.ResetDataMembers();
        }

        #region MovableValue{TConstants} ジェネリッククラス用の定数情報構造体群

        /// <summary>
        /// 音量用の定数情報クラス。
        /// </summary>
        public struct VolumeConst : IMovableValueConstants
        {
            public int Digits => 1;
            public decimal DefaultValue => 100;
            public decimal MinValue => 0;
            public decimal MaxValue => 500;
            public decimal MinSliderValue => 0;
            public decimal MaxSliderValue => 500;
        }

        /// <summary>
        /// 左右バランス用の定数情報クラス。
        /// </summary>
        public struct BalanceConst : IMovableValueConstants
        {
            public int Digits => 1;
            public decimal DefaultValue => 0;
            public decimal MinValue => -100;
            public decimal MaxValue => 100;
            public decimal MinSliderValue => -100;
            public decimal MaxSliderValue => 100;
        }

        #endregion
    }
}
