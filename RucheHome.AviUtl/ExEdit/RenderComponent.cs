using System;
using System.Runtime.Serialization;
using RucheHome.Text;

namespace RucheHome.AviUtl.ExEdit
{
    /// <summary>
    /// 標準描画コンポーネントを表すクラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public class RenderComponent : ComponentBase
    {
        #region アイテム名定数群

        /// <summary>
        /// X座標を保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfX = @"X";

        /// <summary>
        /// Y座標を保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfY = @"Y";

        /// <summary>
        /// Z座標を保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfZ = @"Z";

        /// <summary>
        /// 拡大率を保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfScale = @"拡大率";

        /// <summary>
        /// 透明度を保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfTransparency = @"透明度";

        /// <summary>
        /// 回転角度を保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfRotation = @"回転";

        /// <summary>
        /// 合成モードを保持する拡張編集オブジェクトファイルアイテムの名前。
        /// </summary>
        public const string ExoFileItemNameOfBlendMode = @"blend";

        #endregion

        /// <summary>
        /// コンポーネント名。
        /// </summary>
        public static readonly string ThisComponentName = @"標準描画";

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
        public static RenderComponent FromExoFileItems(IniFileItemCollection items) =>
            FromExoFileItemsCore(items, () => new RenderComponent());

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public RenderComponent() : base()
        {
        }

        /// <summary>
        /// コンポーネント名を取得する。
        /// </summary>
        public override string ComponentName => ThisComponentName;

        /// <summary>
        /// X座標を取得または設定する。
        /// </summary>
        [ExoFileItem(ExoFileItemNameOfX, Order = 1)]
        [DataMember]
        public MovableValue<CoordConst> X
        {
            get { return this.x; }
            set
            {
                this.SetPropertyWithPropertyChangedChain(
                    ref this.x,
                    value ?? new MovableValue<CoordConst>());
            }
        }
        private MovableValue<CoordConst> x = new MovableValue<CoordConst>();

        /// <summary>
        /// Y座標を取得または設定する。
        /// </summary>
        [ExoFileItem(ExoFileItemNameOfY, Order = 2)]
        [DataMember]
        public MovableValue<CoordConst> Y
        {
            get { return this.y; }
            set
            {
                this.SetPropertyWithPropertyChangedChain(
                    ref this.y,
                    value ?? new MovableValue<CoordConst>());
            }
        }
        private MovableValue<CoordConst> y = new MovableValue<CoordConst>();

        /// <summary>
        /// Z座標を取得または設定する。
        /// </summary>
        [ExoFileItem(ExoFileItemNameOfZ, Order = 3)]
        [DataMember]
        public MovableValue<CoordConst> Z
        {
            get { return this.z; }
            set
            {
                this.SetPropertyWithPropertyChangedChain(
                    ref this.z,
                    value ?? new MovableValue<CoordConst>());
            }
        }
        private MovableValue<CoordConst> z = new MovableValue<CoordConst>();

        /// <summary>
        /// 拡大率を取得または設定する。
        /// </summary>
        [ExoFileItem(ExoFileItemNameOfScale, Order = 4)]
        [DataMember]
        public MovableValue<ScaleConst> Scale
        {
            get { return this.scale; }
            set
            {
                this.SetPropertyWithPropertyChangedChain(
                    ref this.scale,
                    value ?? new MovableValue<ScaleConst>());
            }
        }
        private MovableValue<ScaleConst> scale = new MovableValue<ScaleConst>();

        /// <summary>
        /// 透明度を取得または設定する。
        /// </summary>
        [ExoFileItem(ExoFileItemNameOfTransparency, Order = 5)]
        [DataMember]
        public MovableValue<TransparencyConst> Transparency
        {
            get { return this.transparency; }
            set
            {
                this.SetPropertyWithPropertyChangedChain(
                    ref this.transparency,
                    value ?? new MovableValue<TransparencyConst>());
            }
        }
        private MovableValue<TransparencyConst> transparency =
            new MovableValue<TransparencyConst>();

        /// <summary>
        /// 回転角度を取得または設定する。
        /// </summary>
        [ExoFileItem(ExoFileItemNameOfRotation, Order = 6)]
        [DataMember]
        public MovableValue<RotationConst> Rotation
        {
            get { return this.rotation; }
            set
            {
                this.SetPropertyWithPropertyChangedChain(
                    ref this.rotation,
                    value ?? new MovableValue<RotationConst>());
            }
        }
        private MovableValue<RotationConst> rotation = new MovableValue<RotationConst>();

        /// <summary>
        /// 合成モードを取得または設定する。
        /// </summary>
        [ExoFileItem(ExoFileItemNameOfBlendMode, Order = 7)]
        public BlendMode BlendMode
        {
            get { return this.blendMode; }
            set
            {
                this.SetProperty(
                    ref this.blendMode,
                    Enum.IsDefined(value.GetType(), value) ? value : BlendMode.Normal);
            }
        }
        private BlendMode blendMode = BlendMode.Normal;

        /// <summary>
        /// BlendMode プロパティのシリアライズ用ラッパプロパティ。
        /// </summary>
        [DataMember(Name = nameof(BlendMode))]
        private string BlendModeString
        {
            get { return this.BlendMode.ToString(); }
            set
            {
                BlendMode mode;
                this.BlendMode = Enum.TryParse(value, out mode) ? mode : BlendMode.Normal;
            }
        }

        /// <summary>
        /// このコンポーネントの内容を別のコンポーネントへコピーする。
        /// </summary>
        /// <param name="target">コピー先。</param>
        public void CopyTo(RenderComponent target)
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
        /// 座標用の定数情報クラス。
        /// </summary>
        public struct CoordConst : IMovableValueConstants
        {
            public int Digits => 1;
            public decimal DefaultValue => 0;
            public decimal MinValue => -99999.9m;
            public decimal MaxValue => 99999.9m;
            public decimal MinSliderValue => -2000;
            public decimal MaxSliderValue => 2000;
        }

        /// <summary>
        /// 拡大率用の定数情報クラス。
        /// </summary>
        public struct ScaleConst : IMovableValueConstants
        {
            public int Digits => 2;
            public decimal DefaultValue => 100;
            public decimal MinValue => 0;
            public decimal MaxValue => 5000;
            public decimal MinSliderValue => 0;
            public decimal MaxSliderValue => 800;
        }

        /// <summary>
        /// 透明度用の定数情報クラス。
        /// </summary>
        public struct TransparencyConst : IMovableValueConstants
        {
            public int Digits => 1;
            public decimal DefaultValue => 0;
            public decimal MinValue => 0;
            public decimal MaxValue => 100;
            public decimal MinSliderValue => 0;
            public decimal MaxSliderValue => 100;
        }

        /// <summary>
        /// 回転角度用の定数情報クラス。
        /// </summary>
        public struct RotationConst : IMovableValueConstants
        {
            public int Digits => 2;
            public decimal DefaultValue => 0;
            public decimal MinValue => -3600;
            public decimal MaxValue => 3600;
            public decimal MinSliderValue => -360;
            public decimal MaxSliderValue => 360;
        }

        #endregion
    }
}
