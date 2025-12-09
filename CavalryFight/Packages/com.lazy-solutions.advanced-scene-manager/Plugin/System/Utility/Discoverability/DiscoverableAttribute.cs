using System;
using System.Reflection;

namespace AdvancedSceneManager.Utility.Discoverability
{

    /// <summary>Represents the base attribute for discoverable attributes.</summary>
    public abstract class DiscoverableAttribute : Attribute
    {

        /// <summary>Specifies the description to be shown in the diag UI tooltip.</summary>
        public virtual string friendlyDescription { get; } = string.Empty;

        /// <summary>Gets if <paramref name="member"/> is a valid target for this attribute callback.</summary>
        /// <param name="member">Can be either: <see cref="Type"/>, <see cref="ConstructorInfo"/>, or <see cref="MethodInfo"/>.</param>
        public virtual bool IsValidTarget(MemberInfo member) => true;

        /// <summary>Logs an error to console. Uses a standard template.</summary>
        public virtual void LogError(MemberInfo member, string message)
        {
            var declaring = member.DeclaringType?.FullName ?? "<global>";
            var name = $"{declaring}.{member.Name}";
            var attrName = GetType().Name.Replace("Attribute", "");

            Log.Error($"[{attrName}] '{name}' is not valid. {message}");
        }

    }

}
