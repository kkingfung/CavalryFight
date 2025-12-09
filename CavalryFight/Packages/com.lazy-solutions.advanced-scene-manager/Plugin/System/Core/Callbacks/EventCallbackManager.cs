using AdvancedSceneManager.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AdvancedSceneManager.Callbacks.Events
{

    /// <summary>Manages event callbacks.</summary>
    /// <typeparam name="TEventBase">Specifies the base class required for this event manager.</typeparam>
    /// <remarks>You probably want to use either <see cref="SceneManager.events"/> or <see cref="AdvancedSceneManager.Core.SceneOperation.events"/>.</remarks>
    public class EventCallbackManager<TEventBase> where TEventBase : EventCallbackBase
    {

        record RegisteredCallback(Delegate callback, When when, string key);

        readonly Dictionary<Type, List<RegisteredCallback>> registeredCallbacks = new();

        readonly Func<TEventBase, bool> beforeInvoke;
        readonly bool invokeGlobal;

        /// <param name="beforeInvoke">Specifies a callback to be invoked before an event callback is. Return <see langword="false"/> to cancel event.</param>
        /// <param name="invokeGlobal">Specifies if this event callback manager should automatically invoke events in <see cref="SceneManager.events"/> too. Silently ignored if current instance is <see cref="SceneManager.events"/>.</param>
        public EventCallbackManager(Func<TEventBase, bool> beforeInvoke = null, bool invokeGlobal = false)
        {
            this.beforeInvoke = beforeInvoke;
            this.invokeGlobal = invokeGlobal && !ReferenceEquals(SceneManager.events, this);
        }

        #region Register

        /// <summary>Registers a callback to be invoked when the specified event type occurs.</summary>
        /// <typeparam name="TEventType">The type of the event to listen for.</typeparam>
        /// <param name="callback">The method to invoke when the event occurs.</param>
        /// <param name="when">
        /// Specifies whether the callback should be invoked <see cref="When.Before"/>, <see cref="When.After"/>, or both.
        /// If set to <see cref="When.Unspecified"/>, the callback is invoked both before and after the action.
        /// Ignored for event types that do not support timing.
        /// </param>
        /// <param name="key">
        /// Optional identifier for the callback registration. If specified, this key will be used to uniquely identify the callback,
        /// bypassing normal equality checks during unregistration.
        /// </param>
        public void RegisterCallback<TEventType>(EventCallback<TEventType> callback, When when = When.Unspecified, string key = null) where TEventType : TEventBase =>
            RegisterCallback(typeof(TEventType), callback, when, key);

        internal void RegisterCallback(Type eventType, Delegate callback, When when = When.Unspecified, string key = null)
        {
            if (!typeof(TEventBase).IsAssignableFrom(eventType) || eventType.IsAbstract)
                throw new ArgumentException($"{eventType.FullName} cannot be used as event callback.");

            if (!registeredCallbacks.TryGetValue(eventType, out var list))
            {
                list = new List<RegisteredCallback>();
                registeredCallbacks[eventType] = list;
            }

            list.Add(new RegisteredCallback(callback, when, key));
        }

        #endregion
        #region Unregister

        static bool MatchCallback(RegisteredCallback c, Delegate callback, When when = When.Unspecified, string key = null)
        {
            return !string.IsNullOrEmpty(c.key)
                ? c.key == key
                : c.callback.Method == callback.Method &&
                    c.callback.Target == callback.Target &&
                    c.when == when;
        }

        /// <summary>Unregisters a previously registered event callback.</summary>
        /// <param name="key">Identifier that was used during registration.</param>
        public void UnregisterCallback(string key)
        {
            foreach (var c in registeredCallbacks)
                c.Value.RemoveAll(c => c.key == key);
        }

        /// <summary>Unregisters a previously registered event callback.</summary>
        /// <typeparam name="TEventType">The type of the event the callback was registered for.</typeparam>
        /// <param name="callback">The callback that was previously registered.</param>
        /// <param name="when">
        /// Specifies when the callback was originally registered: <see cref="When.Before"/>, <see cref="When.After"/>, or <see cref="When.Unspecified"/> for both.
        /// This must match the value used during registration unless <paramref name="key"/> is specified.
        /// </param>
        /// <param name="key">
        /// Optional identifier that was used during registration.
        /// If provided, it overrides <paramref name="callback"/> and <paramref name="when"/> when determining which callback to remove.
        /// </param>
        public void UnregisterCallback<TEventType>(EventCallback<TEventType> callback, When when = When.Unspecified, string key = null) where TEventType : TEventBase =>
            UnregisterCallback(typeof(TEventType), callback, when, key);

        internal void UnregisterCallback(Type eventType, Delegate callback, When when = When.Unspecified, string key = null)
        {
            if (string.IsNullOrEmpty(key))
                if (!typeof(TEventBase).IsAssignableFrom(eventType) || eventType.IsAbstract)
                    throw new ArgumentException($"{eventType.FullName} cannot be used as event callback.");

            if (eventType is null)
                throw new NullReferenceException("Could not unregister event, since eventType was null.");

            registeredCallbacks.GetValueOrDefault(eventType)?.RemoveAll(c => MatchCallback(c, callback, when, key));
        }

        #endregion
        #region Invoke

        /// <summary>Invokes the event.</summary>
        /// <remarks>Does not support <see cref="EventCallbackBase.WaitFor(IEnumerator)"/> or any of its overloads.</remarks>
        public void InvokeCallbackSync<TEventType>(When when = When.Unspecified) where TEventType : TEventBase, new() =>
            InvokeCallbackSync(new TEventType(), when);

        /// <summary>Invokes the event.</summary>
        /// <remarks>Does not support <see cref="EventCallbackBase.WaitFor(IEnumerator)"/> or any of its overloads.</remarks>
        public void InvokeCallbackSync<TEventType>(TEventType e, When when = When.Unspecified) where TEventType : TEventBase =>
            InvokeCallbackInternal(e, when, out _);

        /// <summary>Invokes the event.</summary>
        public IEnumerator InvokeCallback<TEventType>(When when = When.Unspecified, [CallerFilePath] string callerFile = "", [CallerLineNumber] int callerLine = 0) where TEventType : TEventBase, new() =>
            InvokeCallback(new TEventType(), when, callerFile, callerLine);

        /// <summary>Invokes the event.</summary>
        public IEnumerator InvokeCallback<TEventType>(TEventType e, When when = When.Unspecified, [CallerFilePath] string callerFile = "", [CallerLineNumber] int callerLine = 0) where TEventType : TEventBase
        {

            return Coroutine().StartCoroutine(description: "Invoking event: " + typeof(TEventType).Name, callerFile: callerFile, callerLine: callerLine);

            IEnumerator Coroutine()
            {

                InvokeCallbackInternal(e, when, out var waitFor);

                //Debug.Log($"Invoke {typeof(TEventType).Name} ({when}): {waitFor.Count} callbacks.");
                if (SceneManager.settings.project.allowLoadingScenesInParallel)
                    yield return CoroutineUtility.WaitAll(waitFor, description: "Invoking event: " + typeof(TEventType).Name);
                else
                    yield return CoroutineUtility.Chain(waitFor, description: "Invoking event: " + typeof(TEventType).Name);

            }

        }

        internal void InvokeCallbackInternal<TEventType>(TEventType e, When when, out IEnumerable<Func<IEnumerator>> waitFor) where TEventType : TEventBase
        {

            waitFor = Enumerable.Empty<Func<IEnumerator>>();

            e.when = when;
            var canProceed = beforeInvoke?.Invoke(e) ?? true;
            if (!canProceed)
                return;

            var l = new List<Func<IEnumerator>>();

            if (registeredCallbacks.TryGetValue(typeof(TEventType), out var callbacks))
                foreach (var (callback, _when, _) in callbacks.ToArray())
                {

                    if (EventCallbackUtility.IsWhenApplicable<TEventType>())
                        if (_when != When.Unspecified && _when != when)
                            continue;

                    callback.DynamicInvoke(e);
                    l.AddRange(e.waitFor); //Add coroutine

                }

            if (invokeGlobal)
            {
                SceneManager.events.InvokeCallbackInternal<TEventType>(e, when, out var globalWaitFor);
                l.AddRange(globalWaitFor);
            }

            waitFor = l;

        }

        #endregion

    }

}
