using System;
using System.Runtime.Serialization;

namespace VoiceroidUtil
{
    /// <summary>
    /// ExoCharaStyle インスタンスセットクラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public class ExoCharaStyleSet : VoiceroidItemSetBase<ExoCharaStyle>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public ExoCharaStyleSet() : base()
        {
        }
    }
}
