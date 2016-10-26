using System;
using System.Threading.Tasks;
using RucheHome.Voiceroid;

namespace VoiceroidUtil.Services
{
    /// <summary>
    /// VOICEROIDプロセスに対するアクションを提供するインタフェース。
    /// </summary>
    public interface IVoiceroidActionService
    {
        /// <summary>
        /// VOICEROIDプロセスに対してアクションを行う。
        /// </summary>
        /// <param name="process">VOICEROIDプロセス。</param>
        /// <param name="action">アクション種別。</param>
        Task Run(IProcess process, VoiceroidAction action);
    }
}
