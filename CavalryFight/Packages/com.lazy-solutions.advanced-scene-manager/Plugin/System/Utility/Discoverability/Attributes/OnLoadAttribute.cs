using AdvancedSceneManager.Services;
using AdvancedSceneManager.Utility.Discoverability;
using System;
using System.Reflection;

namespace AdvancedSceneManager.Callbacks
{

#if UNITY_EDITOR
    /// <summary>Occurs when ASM has finished initializing, after domain reload, editor startup, or before startup process in a build.</summary>
    /// <remarks>
    /// <para>Aims to replace:</para>
    /// <code><see cref="UnityEditor.InitializeOnLoadMethodAttribute"/></code>
    /// <code><see cref="UnityEditor.InitializeOnLoadAttribute"/></code>
    /// <para>Combines them, and is safely usable both in and outside the editor.</para>
    /// </remarks>
#endif
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Constructor)]
    public class OnLoadAttribute : DiscoverableAttribute
    {

        /// <summary />
        public OnLoadAttribute()
        { }

        /// <inheritdoc />
        public override bool IsValidTarget(MemberInfo member) =>
            ServiceUtility.IsResolvable(member);

        /// <inheritdoc />
        public override string friendlyDescription =>
            "Occurs when ASM has finished initializing, after domain reload, editor startup, or before startup process in a build.\n\n" +
            "Aims to replace:\n" +
            "<b>[InitializeOnLoadMethod]</b>\n" +
            "<b>[InitializeOnLoad]</b>\n\n" +
            "Combines them, and is safely usable in both in and outside the editor.";

    }

}
