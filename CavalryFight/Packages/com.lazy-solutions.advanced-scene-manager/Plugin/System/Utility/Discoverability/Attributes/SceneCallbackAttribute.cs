using AdvancedSceneManager.Editor.UI;
using AdvancedSceneManager.Utility.Discoverability;
using System;
using System.Collections;
using System.Reflection;

namespace AdvancedSceneManager.Callbacks
{

    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    internal abstract class SceneCallbackAttribute : DiscoverableAttribute
    {

        public override bool IsValidTarget(MemberInfo member) =>
            member.IsMethod() && !member.IsStatic() && member.HasNoParameters() && (member.ReturnsVoid() || member.Returns<IEnumerator>());

    }

}
