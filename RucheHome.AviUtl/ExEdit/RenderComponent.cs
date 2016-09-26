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
        /// <summary>
        /// 拡張編集オブジェクトファイルのセクションデータからコンポーネントを作成する。
        /// </summary>
        /// <param name="section">セクションデータ。</param>
        /// <returns>コンポーネント。作成できないならば null 。</returns>
        public static RenderComponent FromExoFileSection(IniFileSection section)
        {
            return FromExoFileSectionCore(section, () => new RenderComponent());
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public RenderComponent() : base()
        {
        }

        /// <summary>
        /// コンポーネント名を取得する。
        /// </summary>
        public override string ComponentName => @"標準描画";

        /// <summary>
        /// X座標を取得または設定する。
        /// </summary>
        [ComponentItem(@"X", Order = 1)]
        [DataMember]
        public MovableValue<CoordConst> X
        {
            get { return this.x; }
            set { this.SetProperty(ref this.x, value ?? new MovableValue<CoordConst>()); }
        }
        private MovableValue<CoordConst> x = new MovableValue<CoordConst>();

        /// <summary>
        /// Y座標を取得または設定する。
        /// </summary>
        [ComponentItem(@"Y", Order = 2)]
        [DataMember]
        public MovableValue<CoordConst> Y
        {
            get { return this.y; }
            set { this.SetProperty(ref this.y, value ?? new MovableValue<CoordConst>()); }
        }
        private MovableValue<CoordConst> y = new MovableValue<CoordConst>();

        /// <summary>
        /// Z座標を取得または設定する。
        /// </summary>
        [ComponentItem(@"Z", Order = 3)]
        [DataMember]
        public MovableValue<CoordConst> Z
        {
            get { return this.z; }
            set { this.SetProperty(ref this.z, value ?? new MovableValue<CoordConst>()); }
        }
        private MovableValue<CoordConst> z = new MovableValue<CoordConst>();

        /// <summary>
        /// 拡大率を取得または設定する。
        /// </summary>
        [ComponentItem(@"拡大率", Order = 4)]
        [DataMember]
        public MovableValue<ScaleConst> Scale
        {
            get { return this.scale; }
            set
            {
                this.SetProperty(ref this.scale, value ?? new MovableValue<ScaleConst>());
            }
        }
        private MovableValue<ScaleConst> scale = new MovableValue<ScaleConst>();

        /// <summary>
        /// 透明度を取得または設定する。
        /// </summary>
        [ComponentItem(@"透明度", Order = 5)]
        [DataMember]
        public MovableValue<TransparencyConst> Transparency
        {
            get { return this.transparency; }
            set
            {
                this.SetProperty(
                    ref this.transparency,
                    value ?? new MovableValue<TransparencyConst>());
            }
        }
        private MovableValue<TransparencyConst> transparency =
            new MovableValue<TransparencyConst>();

        /// <summary>
        /// 回転角度を取得または設定する。
        /// </summary>
        [ComponentItem(@"回転", Order = 6)]
        [DataMember]
        public MovableValue<RotationConst> Rotation
        {
            get { return this.rotation; }
            set
            {
                this.SetProperty(
                    ref this.rotation,
                    value ?? new MovableValue<RotationConst>());
            }
        }
        private MovableValue<RotationConst> rotation = new MovableValue<RotationConst>();

        /// <summary>
        /// 合成モードを取得または設定する。
        /// </summary>
        [ComponentItem(@"blend", Order = 7)]
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
