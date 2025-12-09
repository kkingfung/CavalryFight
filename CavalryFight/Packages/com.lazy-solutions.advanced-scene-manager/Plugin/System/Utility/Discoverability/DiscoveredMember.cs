using System;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace AdvancedSceneManager.Utility.Discoverability
{

    /// <summary>A member that was found using <see cref="DiscoverabilityUtility"/>.</summary>
    public readonly struct DiscoveredMember : IEquatable<DiscoveredMember>
    {

        internal DiscoveredMember(DiscoverableAttribute attribute, MemberInfo member)
        {
            this.attribute = attribute;
            this.member = member;
        }

        /// <summary>Gets the attribute of this discoverable.</summary>
        public DiscoverableAttribute attribute { get; }

        /// <summary>Gets the member of this discoverable.</summary>
        public MemberInfo member { get; }

        /// <summary>Gets a formatted string of this discoverable.</summary>
        public override string ToString() => member switch
        {
            Type t => t.FullName,
            ConstructorInfo => $"{member.DeclaringType?.FullName}()",
            MethodInfo => $"{member.DeclaringType?.FullName}+{member.Name}()",
            FieldInfo => $"{member.DeclaringType?.FullName}+{member.Name}",
            _ => $"{member.DeclaringType?.FullName}+{member.Name}"
        };

        /// <summary>Gets an identifier that points to the found member.</summary>
        public string GetIdentifier() => member switch
        {
            Type t => $"type:{t.AssemblyQualifiedName}",
            MethodInfo m => $"method:{m.DeclaringType?.AssemblyQualifiedName}.{m.Name}|{string.Join(";", m.GetParameters().Select(p => p.ParameterType.AssemblyQualifiedName))}",
            ConstructorInfo c => $"ctor:{c.DeclaringType?.AssemblyQualifiedName}.ctor|{string.Join(";", c.GetParameters().Select(p => p.ParameterType.AssemblyQualifiedName))}",
            FieldInfo f => $"field:{f.DeclaringType?.AssemblyQualifiedName}.{f.Name}",
            PropertyInfo p => $"prop:{p.DeclaringType?.AssemblyQualifiedName}.{p.Name}|{string.Join(";", p.GetIndexParameters().Select(p2 => p2.ParameterType.AssemblyQualifiedName))}",
            _ => $"unknown:{member.DeclaringType?.AssemblyQualifiedName}.{member.Name}"
        };

        /// <inheritdoc />
        public bool Equals(DiscoveredMember other) =>
            GetIdentifier() == other.GetIdentifier();

        /// <inheritdoc />
        public override bool Equals(object obj) =>
            obj is DiscoveredMember other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() =>
            GetIdentifier().GetHashCode();

        /// <inheritdoc />
        public static bool operator ==(DiscoveredMember left, DiscoveredMember right) =>
            left.Equals(right);

        /// <inheritdoc />
        public static bool operator !=(DiscoveredMember left, DiscoveredMember right) =>
            !left.Equals(right);
    }

}