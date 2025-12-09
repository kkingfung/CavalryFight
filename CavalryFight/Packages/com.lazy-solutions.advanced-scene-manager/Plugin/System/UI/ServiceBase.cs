using AdvancedSceneManager.Callbacks.Events;
using AdvancedSceneManager.Utility;
using System;
using System.Collections.Generic;

namespace AdvancedSceneManager.Services
{

    /// <summary>Shared base class for services and view models.</summary>
    public abstract class Service_ViewModelBase
    {

        #region Callbacks

        private readonly Dictionary<string, object> callbacks = new();

        /// <summary>Registers an event callback that is automatically unregistered when view is removed.</summary>
        public void RegisterEvent<T>(Callbacks.Events.EventCallback<T> callback) where T : EventCallbackBase
        {
            var id = GuidReferenceUtility.GenerateID();
            callbacks.Add(id, callback);
            SceneManager.events.RegisterCallback(callback, key: id);
        }

        /// <summary>Unregisters an event callback.</summary>
        public void UnregisterEvent<T>(Callbacks.Events.EventCallback<T> callback) where T : EventCallbackBase =>
            SceneManager.events.UnregisterCallback(callback);

        /// <summary>Clears all event callbacks registered using <see cref="RegisterEvent{T}(Callbacks.Events.EventCallback{T})"/>.</summary>
        internal void ClearEventCallbacks()
        {
            foreach (var key in callbacks.Keys)
                SceneManager.events.UnregisterCallback(key);

            callbacks.Clear();
        }

        #endregion
        #region Session state

        SessionStateHelper m_sessionState;

        /// <summary>Gets the session state helper. Can be used to persist values across domain reloads.</summary>
        /// <remarks>Wrapper for <see cref="sessionState"/>. Falls back to json for complex types.</remarks>
        protected SessionStateHelper sessionState => m_sessionState ??= new(this);

        #endregion

    }

    /// <summary>Optional base class for services. Supports <see cref="OnInitialize"/> and <see cref="OnDispose"/> callbacks.</summary>
    internal abstract class ServiceBase : Service_ViewModelBase, IDisposable
    {

        /// <summary>Gets whether this service has been initialized.</summary>
        public bool isInitialized { get; private set; }

        /// <summary>Called when the service is registered and instantiated by ASM.</summary>
        protected virtual void OnInitialize()
        { }

        /// <summary>Called when the service is unregistered in ASM.</summary>
        protected virtual void OnDispose()
        { }

        /// <summary>Initializes the service.</summary>
        internal void Initialize()
        {
            isInitialized = true;
            OnInitialize();
        }

        /// <summary>Disposes the service.</summary>
        void IDisposable.Dispose()
        {
            if (!isInitialized)
                return;

            OnDispose();

            isInitialized = false;
        }

        /// <summary>Gets the service of the specified type.</summary>
        /// <typeparam name="T">The type of service to get.</typeparam>
        /// <returns>The service of the specified type.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the service is not found.</exception>
        protected T GetService<T>() =>
            ServiceUtility.Get<T>()
            ?? throw new InvalidOperationException($"Could not retrieve service '{typeof(T).Name}'.");

    }

}
