using AdvancedSceneManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdvancedSceneManager.DependencyInjection
{
    /// <summary>Provides utility methods and accessors for dependency injection within ASM.</summary>
    public static partial class DependencyInjectionUtility
    {
        /// <summary>Base interface for all injectable ASM services.</summary>
        /// <remarks>
        /// Classes implementing an interface derived from <see cref="IInjectable"/> are automatically
        /// registered in <see cref="DependencyInjectionUtility"/> when the static constructor runs.
        /// </remarks>
        public interface IInjectable
        { }

        readonly static List<(Type interfaceT, IInjectable implementation)> services = new()
        {
            { (typeof(IApp), SceneManager.app) },
            { (typeof(IProfileManager), ProfileManagerService.instance) },
            { (typeof(IProjectSettings), SceneManager.settings.project) },
            { (typeof(ISceneManager), SceneManager.runtime) },

        #if UNITY_EDITOR
            { (typeof(Editor.IPackage), SceneManager.package) },
            { (typeof(Editor.IBuildManager), BuildService.instance) },
            { (typeof(Editor.IHierarchyGUI), HierarchyGUIService.instance) },
            { (typeof(Editor.IUserSettings), SceneManager.settings.user) },
            { (typeof(Editor.ISceneManagerWindow), SceneManagerWindowService.instance) },
        #endif
        };

        #region Enumerate

        /// <summary>Enumerates all currently registered injectable services.</summary>
        /// <returns>An enumerable of registered service interface and implementation pairs.</returns>
        public static IEnumerable<(Type interfaceT, IInjectable implementation)> EnumerateServices() =>
            services;

        #endregion

        #region Get

        /// <summary>Gets a service of the specified type.</summary>
        /// <typeparam name="T">The interface type of the service to retrieve.</typeparam>
        /// <returns>The service instance if found; otherwise <see langword="null"/>.</returns>
        public static T GetService<T>() where T : IInjectable =>
            (T)GetService(typeof(T));

        /// <summary>Gets a service matching the specified type.</summary>
        /// <param name="type">The interface or implementation type to search for.</param>
        /// <returns>The service instance if found; otherwise <see langword="null"/>.</returns>
        public static IInjectable GetService(Type type)
        {
            return services.LastOrDefault(s => s.interfaceT == type || s.implementation?.GetType() == type).implementation
                ?? ServiceUtility.Get(type) as IInjectable;
        }

        /// <summary>Gets all services assignable to the specified interface type.</summary>
        /// <typeparam name="T">The interface type to match against.</typeparam>
        /// <returns>An enumerable of all matching services.</returns>
        public static IEnumerable<T> GetServices<T>() where T : IInjectable =>
            services.Where(s => typeof(T).IsAssignableFrom(s.interfaceT)).Select(s => (T)s.implementation);

        #endregion

        #region Add

        /// <summary>Adds a service implementation to the dependency list.</summary>
        /// <typeparam name="TInterface">The interface type of the service.</typeparam>
        /// <typeparam name="TImplementation">The implementation type of the service.</typeparam>
        /// <param name="obj">The service instance to register.</param>
        internal static void Add<TInterface, TImplementation>(TImplementation obj)
            where TInterface : IInjectable
            where TImplementation : TInterface =>
            services.Add((typeof(TInterface), obj));

        /// <summary>Adds a service implementation to the dependency list.</summary>
        /// <typeparam name="TInterface">The interface type of the service.</typeparam>
        /// <param name="obj">The service instance to register.</param>
        internal static void Add<TInterface>(TInterface obj)
            where TInterface : IInjectable =>
            services.Add((typeof(TInterface), obj));

        /// <summary>Constructs and adds a new service of the specified type to the dependency list.</summary>
        /// <typeparam name="TInterface">The interface type of the service.</typeparam>
        /// <typeparam name="TImplementation">The concrete implementation type to instantiate.</typeparam>
        /// <returns>The constructed service instance.</returns>
        internal static TImplementation Add<TInterface, TImplementation>()
            where TInterface : IInjectable
            where TImplementation : TInterface
        {
            var service = Construct<TImplementation>();
            services.Add((typeof(TInterface), service));
            return service;
        }

        /// <summary>Adds a service of the specified interface and instance.</summary>
        /// <param name="interfaceT">The service interface type.</param>
        /// <param name="obj">The service instance to register.</param>
        internal static void Add(Type interfaceT, IInjectable obj) =>
            services.Add((interfaceT, obj));

        #endregion

        #region Remove

        /// <summary>Removes the specified service instance from the dependency list.</summary>
        /// <typeparam name="T">The interface type of the service.</typeparam>
        /// <param name="type">The registered interface type.</param>
        /// <param name="service">The service instance to remove.</param>
        public static void Remove<T>(Type type, T service) where T : IInjectable =>
            services.Remove((type, service));

        #endregion

        #region Construct

        /// <summary>Constructs an instance of <typeparamref name="T"/>, automatically injecting dependencies if possible.</summary>
        /// <remarks>
        /// Returns <see langword="default"/> if not all constructor parameters could be resolved.
        /// Logs an error if injection fails.
        /// </remarks>
        /// <typeparam name="T">The type to construct.</typeparam>
        /// <returns>The constructed instance, or <see langword="default"/> if injection failed.</returns>
        internal static T Construct<T>()
        {
            try
            {
                var l = new List<IInjectable>();
                var constructor = typeof(T).GetConstructors().FirstOrDefault();

                if (constructor is not null)
                {
                    foreach (var param in constructor.GetParameters())
                        l.Add(GetService(param.ParameterType)
                            ?? throw new ArgumentException($"Cannot inject '{param.ParameterType.Name}' into '{typeof(T).Name}'."));
                }

                return (T)Activator.CreateInstance(typeof(T), l.ToArray());
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                return default;
            }
        }

        #endregion
    }
}
