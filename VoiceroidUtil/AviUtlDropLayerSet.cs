using System;
using System.Runtime.Serialization;

namespace VoiceroidUtil
{
    /// <summary>
    /// AviUtlDropLayer インスタンスセットクラス。
    /// </summary>
    [DataContract(Namespace = "")]
    public class AviUtlDropLayerSet : VoiceroidItemSetBase<AviUtlDropLayer>
    {
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public AviUtlDropLayerSet() : base()
        {
        }
    }
}
