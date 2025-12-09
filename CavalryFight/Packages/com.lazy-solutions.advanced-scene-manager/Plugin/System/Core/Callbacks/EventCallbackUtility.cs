using AdvancedSceneManager.Core;
using AdvancedSceneManager.Editor.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AdvancedSceneManager.Callbacks.Events
{

    /// <summary>Provides utility functions for working with event callbacks.</summary>
    public static class EventCallbackUtility
    {

        /// <summary>Specifies when a callback type should be invoked.</summary>
        [AttributeUsage(AttributeTargets.Class)]
        public class CalledForAttribute : Attribute
        {

            /// <summary>The conditions that determine when the callback is invoked.</summary>
            public When[] when { get; }

            /// <summary>Initializes a new instance of the <see cref="CalledForAttribute"/> class.</summary>
            /// <param name="when">One or more <see cref="When"/> values specifying when to invoke the callback.</param>
            public CalledForAttribute(params When[] when) => this.when = when;

        }

        /// <summary>Specifies the invocation order for a callback type.</summary>
        [AttributeUsage(AttributeTargets.Class)]
        public class InvokationOrderAttribute : Attribute
        {

            /// <summary>The relative order in which the callback should be invoked.</summary>
            public int order { get; }

            /// <summary>Initializes a new instance of the <see cref="InvokationOrderAttribute"/> class.</summary>
            /// <param name="order">The order in which the callback should be invoked. Lower values are called earlier.</param>
            public InvokationOrderAttribute(int order) => this.order = order;

        }

        /// <summary>Enumerates all callback types.</summary>
        public static IEnumerable<Type> GetCallbackTypes() =>
            TypeUtility.FindSubclasses<EventCallbackBase>(includeAbstract: false);

        #region Is when applicable

        /// <inheritdoc cref="IsWhenApplicable(Type)"/>
        public static bool IsWhenApplicable<TEventType>() where TEventType : EventCallbackBase =>
            IsWhenApplicable(typeof(TEventType));

        /// <summary>Gets if the specified callback event uses <see cref="When"/> enum.</summary>
        public static bool IsWhenApplicable(Type type)
        {
            if (type == null)
                return false; // If no type is given, When is not applicable

            var attribute = type.GetCustomAttribute<CalledForAttribute>();
            return attribute != null && !attribute.when.Contains(When.Unspecified);
        }

        #endregion
        #region Get invokation order

        /// <inheritdoc cref="GetInvokationOrder(Type)"/>
        public static int GetInvokationOrder<TEventType>() where TEventType : EventCallbackBase, new() =>
            GetInvokationOrder(typeof(TEventType));

        /// <summary>Gets the invokation order of the event callback type.</summary>
        public static int GetInvokationOrder(Type type)
        {
            var attribute = type?.GetCustomAttribute<InvokationOrderAttribute>();
            return attribute?.order ?? -1;
        }

        #endregion
        #region Register all

        /// <summary>Registers callback for all events.</summary>
        public static SceneOperation RegisterAllCallbacks(string key, SceneOperation operation, EventCallback<EventCallbackBase> callback, When when = When.Unspecified)
        {

            var types = GetCallbackTypes().ToArray();
            foreach (var type in types)
                operation.events.RegisterCallback(type, callback, when, key);

            return operation;

        }

        /// <summary>Registers callback for all events.</summary>
        public static void RegisterAllCallbacksGlobal(string key, EventCallback<EventCallbackBase> callback, When when = When.Unspecified)
        {

            var types = GetCallbackTypes().ToArray();
            foreach (var type in types)
                SceneManager.events.RegisterCallback(type, callback, when, key);

        }

        /// <summary>Unregisters callback using <paramref name="key"/>.</summary>
        public static SceneOperation UnregisterCallback(string key, SceneOperation operation)
        {
            operation.events.UnregisterCallback(key);
            return operation;
        }

        /// <summary>Unregisters callback using <paramref name="key"/>.</summary>
        public static void UnregisterCallbackGlobal(string key)
        {
            SceneManager.events.UnregisterCallback(key!);
        }

        #endregion

    }

}
