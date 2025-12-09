using AdvancedSceneManager.Callbacks.Events;

namespace AdvancedSceneManager.Core
{

    partial class SceneOperation
    {

        /// <summary>Gets the event manager for this operation.</summary>
        public EventCallbackManager<SceneOperationEventBase> events { get; private set; }

        void InitializeEventManager()
        {
            events = new(BeforeEventInvoke, invokeGlobal: true);
        }

        bool BeforeEventInvoke(SceneOperationEventBase e)
        {

            if (!flags.HasFlag(SceneOperationFlags.EventCallbacks))
                return false;

            e.operation = this;
            return true;

        }

        /// <summary>Registers a callback for when an event occurs for this operation.</summary>
        /// <remarks>Proxy for the same method on <see cref="events"/>.</remarks>
        /// <inheritdoc cref="EventCallbackManager{TEventBase}.RegisterCallback{TEventType}(EventCallback{TEventType}, When, string)"/>
        public SceneOperation RegisterCallback<TEventType>(EventCallback<TEventType> callback, When when = When.Unspecified, string key = null) where TEventType : SceneOperationEventBase =>
            Set(() => events.RegisterCallback(callback, when, key));

        /// <summary>Unregisters a callback for when an event occurs for this operation.</summary>
        /// <remarks>Proxy for the same method on <see cref="events"/>.</remarks>
        /// <inheritdoc cref="EventCallbackManager{TEventBase}.UnregisterCallback{TEventType}(EventCallback{TEventType}, When, string)"/>
        public SceneOperation UnregisterCallback<TEventType>(EventCallback<TEventType> callback, When when = When.Unspecified, string key = null) where TEventType : SceneOperationEventBase =>
            Set(() => events.UnregisterCallback(callback, when, key));

        /// <summary>Unregisters a previously registered event callback.</summary>
        /// <typeparam name="TEventType">The type of the event the callback was registered for.</typeparam>
        /// <param name="key">Identifier that was used during registration.</param>
        public void UnregisterCallback<TEventType>(string key) where TEventType : SceneOperationEventBase, new() =>
          Set(() => events.UnregisterCallback(typeof(TEventType), null!, key: key));

    }

}
