using AdvancedSceneManager.Editor.UI;
using AdvancedSceneManager.Utility;
using AdvancedSceneManager.Utility.Discoverability;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;

namespace AdvancedSceneManager.Services
{

    /// <summary>Registers a service with the service container.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    internal class RegisterServiceAttribute : DiscoverableAttribute
    {

        /// <summary>Gets the associated type for this service registration.</summary>
        public Type associatedType { get; }

        /// <summary>Initializes a new service registration attribute.</summary>
        public RegisterServiceAttribute(Type associatedType = null) =>
           this.associatedType = associatedType;

        /// <summary>Gets a friendly description of this service registration.</summary>
        public override string friendlyDescription => "Registers a service with the DI system.";

    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    internal class InjectAttribute : DiscoverableAttribute
    { }

    internal class DiscoverableContainer<T> : List<DiscoveredMember>
    { }

    internal class Service<T>
    {
        T m_instance;
        public T instance => m_instance ??= ServiceUtility.Get<T>();
    }

    internal static class ServiceUtility
    {

        internal static void Initialize()
        {
            services.Clear();
            RegisterAutoServices();
            InitializeServices();
        }

        static void RegisterAutoServices()
        {

            var discovered = DiscoverabilityUtility
                .GetMembers<RegisterServiceAttribute>()
                .OfType<RegisterServiceAttribute, Type>();

            foreach (var (attribute, type) in discovered)
            {
                var assoc = attribute.associatedType;

                // Case 1: Interface annotated with an implementation class
                if (type.IsInterface && assoc is { IsClass: true } && type.IsAssignableFrom(assoc))
                {
                    Register(type, assoc);
                    continue;
                }

                // Case 2: Class annotated with an interface
                if (type.IsClass && !type.IsAbstract && assoc is { IsInterface: true } && assoc.IsAssignableFrom(type))
                {
                    Register(assoc, type);
                    continue;
                }

                // Case 3: Class annotated with no associated type
                if (type.IsClass && !type.IsAbstract && assoc is null)
                {
                    Register(type);
                    continue;
                }

                // Anything else is suspicious → log warning
                Log.Warning($"Invalid [RegisterService] usage: {type} (assoc={assoc})");
            }

        }

        static void InitializeServices()
        {
            foreach (var service in services.Values.OfType<ServiceBase>().ToList())
            {
                service.Initialize();
            }
        }

        /// <summary>Determines whether the DI system can invoke the specified method or constructor, automatically injecting parameters as necessary.</summary>
        /// <param name="member">
        /// <para>A <see cref="MethodInfo"/> or <see cref="ConstructorInfo"/> to evaluate for resolvability.</para>
        /// <para>If a <see cref="Type"/> is provided, the first resolvable constructor will be used.</para>
        /// </param>
        /// <param name="static">Only applicable if <paramref name="member"/> is a <see cref="Type"/>. Determines whether static or instance constructors will be checked.</param>
        public static bool IsResolvable(MemberInfo member, bool @static = true)
        {

            //TODO: Return if type is constructable
            return true;
        }

        static readonly ConcurrentDictionary<Type, object> services = new();

        /// <summary>Gets all registered services.</summary>
        public static IReadOnlyDictionary<Type, object> GetAll() => services;

        #region Get

        /// <summary>Gets the service of the specified type.</summary>
        public static TService Get<TService>() =>
            (TService)services.GetValueOrDefault(typeof(TService));

        internal static object Get(Type type) =>
            services.GetValueOrDefault(type);

        /// <summary>Finds all services of the specified type.</summary>
        public static IEnumerable<TService> Find<TService>() =>
            services.Values.OfType<TService>();

        #endregion
        #region Register

        /// <summary>Registers a service instance.</summary>
        public static void Register<TService>(TService service) =>
            Register<TService, TService>(service);

        /// <summary>Registers a service type to be instantiated automatically.</summary>
        public static void Register<TService>() where TService : new() =>
            Register<TService, TService>(new TService());

        /// <summary>Registers a service type with its implementation type.</summary>
        public static void Register<TService, TImplementation>() where TImplementation : TService, new()
        {
            Register<TService, TImplementation>(new());
        }

        /// <summary>Registers a service type with its implementation instance.</summary>
        public static void Register<TService, TImplementation>(TImplementation service)
        {
            if (service is null)
                throw new ArgumentNullException(nameof(service));

            services.Add(typeof(TService), service);
        }

        internal static void Register(Type implementationType)
        {
            if (CreateInstance(implementationType, out var service))
                services.Add(implementationType, service);
        }

        internal static void Register(Type interfaceType, Type implementationType)
        {
            if (!interfaceType.IsAssignableFrom(implementationType))
                return;

            if (CreateInstance(implementationType, out var service))
                services.Add(interfaceType, service);
        }

        internal static void Register(Type type, object implementation)
        {
            if (type is null || implementation is null)
                return;

            if (!type.IsAssignableFrom(implementation.GetType()))
                return;

            services.Add(type, implementation);
        }

        #endregion
        #region Unregister

        /// <summary>Unregisters a service type.</summary>
        public static void Unregister<T>()
        {
            Unregister(typeof(T));
        }

        /// <summary>Unregisters a service by type.</summary>
        public static void Unregister(Type type)
        {
            services.TryRemove(type, out _);
        }

        /// <summary>Unregisters a specific service instance.</summary>
        public static void Unregister<T>(T service)
        {
            if (service is not null)
                Unregister(service.GetType());
        }

        #endregion
        #region Invoke

        /// <summary>Attempts to invoke the specified member with the given parameters.</summary>
        public static bool TryInvoke(MemberInfo member, out Exception exception, params object[] parameters)
        {
            return TryInvokeInternal(member, out _, out exception, parameters);
        }

        /// <summary>Attempts to invoke the specified member and return a value.</summary>
        public static bool TryInvoke<T>(MemberInfo member, [NotNullWhen(true)] out T returnValue, out Exception exception, params object[] parameters)
        {
            if (!TryInvokeInternal(member, out var rawResult, out exception, parameters))
            {
                returnValue = default;
                return false;
            }

            if (rawResult is T typed)
            {
                returnValue = typed;
                return true;
            }

            try
            {
                returnValue = (T)Convert.ChangeType(rawResult, typeof(T));
                return true;
            }
            catch
            {
                returnValue = default;
                return false;
            }
        }

        private static bool TryInvokeInternal(MemberInfo member, out object result, out Exception exception, object[] parameters)
        {
            exception = null;
            result = null;

            if (member == null)
                return false;

            try
            {
                switch (member)
                {
                    case MethodInfo method:
                        result = method.Invoke(null, parameters);
                        break;

                    case Type type:
                        RuntimeHelpers.RunClassConstructor(type.TypeHandle);
                        break;

                    case ConstructorInfo ctor when ctor.IsStatic:
                        RuntimeHelpers.RunClassConstructor(ctor.DeclaringType.TypeHandle);
                        break;
                }
            }
            catch (Exception ex)
            {
                exception = ex;
                return false;
            }

            return true;
        }

        /// <summary>Attempts to invoke the specified member without parameters.</summary>
        public static bool TryInvoke(MemberInfo member) =>
            TryInvoke(member, out _);

        #endregion
        #region Resolve

        /// <summary>Resolves dependencies for the specified object.</summary>
        public static void Resolve<T>(T obj)
        {

            if (obj is null)
                return;

            var members = obj.FindFieldsDecoratedWithAttribute<InjectAttribute>(withProperties: true);

            foreach (var member in members)
            {

                var memberType = member switch
                {
                    FieldInfo f => f.FieldType,
                    PropertyInfo p => p.PropertyType,
                    _ => null
                };

                if (memberType is null)
                    continue;

                try
                {
                    var service = Get(memberType);
                    member.SetValue(obj, service);
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                    throw;
                }

            }

        }

        #endregion

        static bool CreateInstance(Type type, [NotNullWhen(true)] out object obj)
        {
            try
            {
                obj = Activator.CreateInstance(type);

                //if (obj is ServiceBase service)
                //    service.Initialize();

                return true;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                obj = null;
                return false;
            }
        }

    }

}
