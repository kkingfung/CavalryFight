using AdvancedSceneManager.Editor.UI;
using AdvancedSceneManager.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace AdvancedSceneManager.Utility.Discoverability
{

    /// <summary>Occurs when the discoverables cache has been invalidated, and re-scanned.</summary>
    /// <remarks>This is also called after discoverables has just been initialized for the first time, even if nothing was invalidated or scanned.</remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class DiscoverabilityCacheInvalidatedAttribute : DiscoverableAttribute
    {

        /// <summary>Specifies if the member is a valid target for this attribute callback.</summary>
        public override bool IsValidTarget(MemberInfo member) =>
          member.HasNoParameters() || member.HasParameters<Assembly>();

        /// <summary>A friendly description to be shown in the diagnostics popup of the ASM window.</summary>
        public override string friendlyDescription =>
            "Occurs when the discoverables cache has been invalidated, and re-scanned.\n\n" +
            "This is also called after discoverables has just been initialized for the first time, even if nothing was invalidated or scanned.";

    }

    /// <summary>Provides utility methods for dealing and managing discoverables, a centralized attribute callback system.</summary>
    public static class DiscoverabilityUtility
    {
        private static readonly HashSet<DiscoveredMember> members = new();

        /// <summary>Gets if discoverables has been initialized.</summary>
        public static bool isInitialized { get; private set; }

        internal static void Initialize()
        {

            if (isInitialized)
                return;

            try
            {
                LoadCache();
#if UNITY_EDITOR
                ScanAssemblies();
#endif
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }

            isInitialized = true;
            Invoke<DiscoverabilityCacheInvalidatedAttribute>();

        }

#if UNITY_EDITOR
        private static void Add(DiscoveredMember discoverable)
        {
            lock (members)
                members.Add(discoverable);
        }
#endif

        #region Cache

        private static void LoadCache()
        {
            members.Clear();

            foreach (var id in SceneManager.settings.project.discoverablesCache.m_cachedDiscoverables)
                foreach (var d in Deserialize(id))
                    members.Add(d);
        }

#if UNITY_EDITOR

        private static bool _invalidateQueued;

        internal static void InvalidateCache()
        {
            if (_invalidateQueued)
                return;

            _invalidateQueued = true;
            EditorApplication.delayCall += () =>
            {
                _invalidateQueued = false;
                members.Clear();
                SceneManager.settings.project.discoverablesCache.m_cachedDiscoverables.Clear();
                ScanAssemblies();
                SceneManager.settings.project.Save();
            };
        }

#endif

        private static string Serialize(DiscoveredMember discoverable) =>
            discoverable.GetIdentifier();

        private static IEnumerable<DiscoveredMember> Deserialize(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return Enumerable.Empty<DiscoveredMember>();

            int sep = identifier.IndexOf(':');
            if (sep == -1)
                return Enumerable.Empty<DiscoveredMember>();

            string kind = identifier[..sep];
            string rest = identifier[(sep + 1)..];

            try
            {
                return DeserializeInner(kind, rest).ToArray();
            }
            catch
            {
                return Enumerable.Empty<DiscoveredMember>();
            }
        }

        private static IEnumerable<DiscoveredMember> DeserializeInner(string kind, string rest)
        {
            switch (kind)
            {
                case "type":
                    {
                        var type = Type.GetType(rest, throwOnError: false);
                        if (type == null) yield break;

                        foreach (var attr in type.GetCustomAttributes<DiscoverableAttribute>(true))
                            yield return new DiscoveredMember(attr, type);
                        break;
                    }

                case "method":
                case "ctor":
                    {
                        var method = ParseMethodOrCtor(rest, kind == "ctor");
                        if (method == null) yield break;

                        foreach (var attr in method.GetCustomAttributes<DiscoverableAttribute>(true))
                            yield return new DiscoveredMember(attr, method);
                        break;
                    }

                case "prop":
                    {
                        var lastDot = rest.LastIndexOf('.');
                        if (lastDot == -1) yield break;

                        var typeName = rest[..lastDot];
                        var nameAndParams = rest[(lastDot + 1)..];
                        var pipeIndex = nameAndParams.IndexOf('|');
                        var propName = pipeIndex >= 0 ? nameAndParams[..pipeIndex] : nameAndParams;
                        var paramSection = pipeIndex >= 0 ? nameAndParams[(pipeIndex + 1)..] : "";

                        var type = Type.GetType(typeName, throwOnError: false);
                        if (type == null) yield break;

                        var paramTypes = string.IsNullOrWhiteSpace(paramSection)
                            ? Type.EmptyTypes
                            : paramSection.Split(';')
                                .Select(name => Type.GetType(name.Trim(), throwOnError: false))
                                .Where(t => t != null)
                                .ToArray()!;

                        var prop = type.GetProperty(propName,
                            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                            null, null, paramTypes, null);
                        if (prop == null) yield break;

                        foreach (var attr in prop.GetCustomAttributes<DiscoverableAttribute>(true))
                            yield return new DiscoveredMember(attr, prop);
                        break;
                    }

                case "field":
                    {
                        var lastDot = rest.LastIndexOf('.');
                        if (lastDot == -1) yield break;

                        var typeName = rest[..lastDot];
                        var fieldName = rest[(lastDot + 1)..];
                        var type = Type.GetType(typeName, throwOnError: false);
                        if (type == null) yield break;

                        var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        if (field == null) yield break;

                        foreach (var attr in field.GetCustomAttributes<DiscoverableAttribute>(true))
                            yield return new DiscoveredMember(attr, field);
                        break;
                    }
            }
        }

        private static MethodBase ParseMethodOrCtor(string rest, bool isCtor)
        {
            var pipeIndex = rest.IndexOf('|');
            string typeAndName = pipeIndex >= 0 ? rest[..pipeIndex] : rest;
            string paramSection = pipeIndex >= 0 ? rest[(pipeIndex + 1)..] : "";

            var paramTypes = string.IsNullOrWhiteSpace(paramSection)
                ? Type.EmptyTypes
                : paramSection.Split(';')
                    .Select(name => Type.GetType(name.Trim(), throwOnError: false))
                    .ToArray();

            if (paramTypes.Any(t => t == null)) return null;

            int lastDot = typeAndName.LastIndexOf('.');
            if (lastDot == -1) return null;

            var typeName = typeAndName[..lastDot];
            var memberName = typeAndName[(lastDot + 1)..];
            var type = Type.GetType(typeName, throwOnError: false);
            if (type == null) return null;

            return isCtor
                ? type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, paramTypes!, null)
                : type.GetMethod(memberName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, paramTypes!, null);
        }

        #endregion
        #region Get

        /// <summary>Get members decorated with the specified attribute.</summary>
        public static IEnumerable<DiscoveredMember> GetMembers() =>
            GetMembers<DiscoverableAttribute>();

        /// <summary>Get members decorated with the specified attribute.</summary>
        public static IEnumerable<DiscoveredMember> GetMembers<T>() where T : DiscoverableAttribute
        {
            if (!isInitialized)
                Initialize();

            return members.Where(dm => dm.attribute is T);
        }

        #endregion
        #region Assembly scanning

        internal static Assembly[] GetAssemblies() =>
            AppDomain.CurrentDomain.GetAssemblies().Where(IsValidAssembly).ToArray();

        internal static bool IsValidAssembly(Assembly assembly)
        {
            // Ignore dynamic or system assemblies without a location
            if (assembly.IsDynamic || string.IsNullOrEmpty(assembly.Location))
                return false;

            if (assembly.FullName.StartsWith("Unity"))
                return false;

            var path = assembly.Location.ToLower().Replace("\\", "/");

            return path.IndexOf("/assets/", StringComparison.OrdinalIgnoreCase) >= 0
                || path.Contains("/packages/com.lazy-solutions.advanced-scene-manager/", StringComparison.OrdinalIgnoreCase)
                || path.Contains("/library/scriptAssemblies/", StringComparison.OrdinalIgnoreCase);
        }

#if UNITY_EDITOR
        private static void ScanAssemblies()
        {
            var found = new List<DiscoveredMember>();
            using var timer = Log.Duration();

            // --- TypeCache scans ---
            foreach (var type in TypeCache.GetTypesWithAttribute<DiscoverableAttribute>())
                foreach (var attr in type.GetCustomAttributes<DiscoverableAttribute>(true))
                    if (attr.IsValidTarget(type))
                        found.Add(new DiscoveredMember(attr, type));

            foreach (var method in TypeCache.GetMethodsWithAttribute<DiscoverableAttribute>())
                if (method.IsStatic)
                    foreach (var attr in method.GetCustomAttributes<DiscoverableAttribute>(true))
                        if (attr.IsValidTarget(method))
                            found.Add(new DiscoveredMember(attr, method));

            foreach (var field in TypeCache.GetFieldsWithAttribute<DiscoverableAttribute>())
                foreach (var attr in field.GetCustomAttributes<DiscoverableAttribute>(true))
                    if (attr.IsValidTarget(field))
                        found.Add(new DiscoveredMember(attr, field));

            // --- Props & ctors via reflection ---
            foreach (var assembly in GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    foreach (var ctor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                        foreach (var attr in ctor.GetCustomAttributes<DiscoverableAttribute>(true))
                            if (attr.IsValidTarget(ctor))
                                found.Add(new DiscoveredMember(attr, ctor));

                    foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                        foreach (var attr in prop.GetCustomAttributes<DiscoverableAttribute>(true))
                            if (attr.IsValidTarget(prop))
                                found.Add(new DiscoveredMember(attr, prop));
                }
            }

            // Save to project
            members.Clear();
            foreach (var d in found)
                members.Add(d);

            SceneManager.settings.project.discoverablesCache.m_cachedDiscoverables.Clear();
            SceneManager.settings.project.discoverablesCache.m_cachedDiscoverables.AddRange(found.Select(Serialize).Distinct());
            SceneManager.settings.project.discoverablesCache.m_cachedDiscoverables.Sort();

            SceneManager.settings.project.Save();
            Log.Info($"Found {found.Count} discoverables in {timer.Elapsed}.");
        }
#endif

        #endregion
        #region Extension methods

        /// <summary>Gets the discoverable as <typeparamref name="TAttribute"/> and <typeparamref name="TMember"/>, if possible.</summary>
        public static bool As<TAttribute, TMember>(this DiscoveredMember discoveredMember, [NotNullWhen(true)] out TAttribute attribute, [NotNullWhen(true)] out TMember member)
            where TAttribute : DiscoverableAttribute
            where TMember : MemberInfo
        {

            if (discoveredMember.attribute is TAttribute attr && discoveredMember.member is TMember mem)
            {
                attribute = attr;
                member = mem;
                return true;
            }

            attribute = default!;
            member = default!;
            return false;

        }

        /// <summary>Gets the discoverables of type <typeparamref name="TAttribute"/> and <typeparamref name="TMember"/>.</summary>
        public static IEnumerable<(TAttribute, TMember)> OfType<TAttribute, TMember>(this IEnumerable<DiscoveredMember> discoveredMember)
            where TAttribute : DiscoverableAttribute
            where TMember : MemberInfo
        {
            foreach (var item in discoveredMember)
                if (item.As(out TAttribute attribute, out TMember member))
                    yield return (attribute, member);
        }

        #endregion
        #region Invoke

        /// <summary>Invokes all attribute callbacks of type <typeparamref name="T"/>.</summary>
        public static void Invoke<T>(params object[] parameters) where T : DiscoverableAttribute
        {
            foreach (var callback in GetMembers<T>())
                if (!ServiceUtility.TryInvoke(callback.member, out var ex, parameters) && ex is not null)
                    Log.Exception(ex);
        }

        #endregion

    }

}