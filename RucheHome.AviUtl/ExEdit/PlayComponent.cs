﻿using System;
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
        #region アイテム名定数群

        /// <summary>
        /// 音量を保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfVolume = @"音量";

        /// <summary>
        /// 左右バランスを保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfBalance = @"左右";

        #endregion

        /// <summary>
        /// コンポーネント名。
        /// </summary>
        public static readonly string ThisComponentName = @"標準再生";

        /// <summary>
        /// 拡張編集オブジェクトファイルのアイテムコレクションに
        /// コンポーネント名が含まれているか否かを取得する。
        /// </summary>
        /// <param name="items">アイテムコレクション。</param>
        /// <returns>含まれているならば true 。そうでなければ false 。</returns>
        public static bool HasComponentName(IniFileItemCollection items) =>
            HasComponentNameCore(items, ThisComponentName);

        /// <summary>
        /// 拡張編集オブジェクトファイルのアイテムコレクションから
        /// コンポーネントを作成する。
        /// </summary>
        /// <param name="items">アイテムコレクション。</param>
        /// <returns>コンポーネント。</returns>
        public static PlayComponent FromExoFileItems(IniFileItemCollection items) =>
            FromExoFileItemsCore(items, () => new PlayComponent());

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public PlayComponent() : base()
        {
        }

        /// <summary>
        /// コンポーネント名を取得する。
        /// </summary>
        public override string ComponentName => ThisComponentName;

        /// <summary>
        /// 音量を取得または設定する。
        /// </summary>
        [ExoFileItem(ExoFileItemNameOfVolume, Order = 1)]
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
        [ExoFileItem(ExoFileItemNameOfBalance, Order = 2)]
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
