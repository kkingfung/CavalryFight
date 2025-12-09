using AdvancedSceneManager.Core;
using AdvancedSceneManager.Loading;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Internal;
using AdvancedSceneManager.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AdvancedSceneManager.Callbacks.Events.EventCallbackUtility;
using static AdvancedSceneManager.Core.App;

namespace AdvancedSceneManager.Callbacks.Events
{

    #region Base

    /// <summary>The base class for all event callbacks.</summary>
    public abstract record EventCallbackBase
    {

        /// <summary>Specifies when this event callback was invoked, before or after the action it represents. If applicable.</summary>
        /// <remarks>Only applicable to scene operation events, like <see cref="SceneOpenEvent"/> and so on.</remarks>
        public When when { get; set; }

        #region WaitFor

        /// <summary>A list of coroutines that <see cref="SceneOperation"/> should wait for. It will not proceed until all coroutines are done.</summary>
        /// <remarks>Note that all callbacks will be invoked, and all coroutines will be collected in a list, before ASM waits (and potentially invokes) for coroutines to complete.</remarks>
        public List<Func<IEnumerator>> waitFor { get; private set; } = new();

        /// <summary>Specifies a coroutine that the operation should wait for.</summary>
        /// <remarks>
        /// Note that all callbacks will be invoked, and all coroutines will be collected in a list, before ASM waits (and potentially invokes) for coroutines to complete.
        /// <br/><br/>
        /// Note that this <paramref name="coroutine"/> <b>will</b> be started immediately with this overload.
        /// </remarks>
        public void WaitFor(IEnumerator coroutine) => waitFor.Add(() => coroutine);

        /// <summary>Specifies a coroutine that the operation should wait for.</summary>
        /// <remarks>
        /// Note that all callbacks will be invoked, and all coroutines will be collected in a list, before ASM waits (and potentially invokes) for coroutines to complete.
        /// <br/><br/>
        /// Note that this <paramref name="coroutine"/> <b>will not</b> be started immediately with this overload.
        /// </remarks>
        public void WaitFor(Func<IEnumerator> coroutine) => waitFor.Add(coroutine);

        /// <summary>Specifies a coroutine that the operation should wait for.</summary>
        /// <remarks>
        /// Note that all callbacks will be invoked, and all coroutines will be collected in a list, before ASM waits (and potentially invokes) for coroutines to complete.
        /// <br/><br/>
        /// Note that this <paramref name="coroutine"/> <b>will</b> be started immediately with this overload.
        /// </remarks>
        public void WaitFor(GlobalCoroutine coroutine) => waitFor.Add(() => coroutine);

        /// <summary>Specifies a coroutine that the operation should wait for.</summary>
        /// <remarks>
        /// Note that all callbacks will be invoked, and all coroutines will be collected in a list, before ASM waits (and potentially invokes) for coroutines to complete.
        /// <br/><br/>
        /// Note that this <paramref name="coroutine"/> <b>will not</b> be started immediately with this overload.
        /// </remarks>
        public void WaitFor(Func<GlobalCoroutine> coroutine) => waitFor.Add(coroutine);

        /// <summary>Specifies a coroutine that the operation should wait for.</summary>
        /// <remarks>
        /// Note that all callbacks will be invoked, and all coroutines will be collected in a list, before ASM waits (and potentially invokes) for coroutines to complete.
        /// <br/><br/>
        /// Note that this <paramref name="awaitable"/> <b>will</b> be started immediately with this overload.
        /// </remarks>
        public void WaitFor(Awaitable awaitable) => waitFor.Add(() => awaitable);

        /// <summary>Specifies a coroutine that the operation should wait for.</summary>
        /// <remarks>
        /// Note that all callbacks will be invoked, and all coroutines will be collected in a list, before ASM waits (and potentially invokes) for coroutines to complete.
        /// <br/><br/>
        /// Note that this <paramref name="awaitable"/> <b>will not</b> be started immediately with this overload.
        /// </remarks>
        public void WaitFor(Func<Awaitable> awaitable) => waitFor.Add(awaitable);

        #endregion
        #region ToString

        /// <inheritdoc />
        public override string ToString() =>
            ToString(extraData: null);

        /// <inheritdoc />
        protected virtual string ToString(ASMModelBase extraData)
        {
            var extraStr = extraData ? $":{extraData} " : null;
            return $"[{GetType().Name}] ({when})" + extraStr;
        }

        #endregion

    }

    /// <summary>The base class for all scene operation event callbacks.</summary>
    public abstract record SceneOperationEventBase : EventCallbackBase
    {
        /// <summary>The operation that invoked this event callback.</summary>
        /// <remarks>Might be null in some circumstances.</remarks>
        public SceneOperation operation { get; set; }
    }

    /// <summary>The base class for scene event callbacks.</summary>
    /// <remarks>See <see cref="SceneOpenEvent"/>, <see cref="SceneCloseEvent"/>, <see cref="ScenePreloadEvent"/>.</remarks>
    public abstract record SceneEvent(Scene scene) : SceneOperationEventBase
    {
        /// <inheritdoc />
        public override string ToString() =>
            ToString(scene);
    }

    /// <summary>The base class for scene phase event callbacks.</summary>
    /// <remarks>See <see cref="SceneClosePhaseEvent"/>, <see cref="SceneOpenPhaseEvent"/>, <see cref="ScenePreloadPhaseEvent"/>.</remarks>
    public abstract record ScenePhaseEvent(IEnumerable<Scene> scenes) : SceneOperationEventBase;

    /// <summary>The base class for collection event callbacks.</summary>
    /// <remarks>See <see cref="CollectionOpenEvent"/>, <see cref="CollectionCloseEvent"/>.</remarks>
    public abstract record CollectionEvent(SceneCollection collection) : SceneOperationEventBase
    {
        /// <inheritdoc />
        public override string ToString() =>
            ToString(collection);
    }

    /// <summary>The base class for loading screen phase event callbacks.</summary>
    /// <remarks>See <see cref="LoadingScreenOpenPhaseEvent"/>, <see cref="LoadingScreenClosePhaseEvent"/>.</remarks>
    public abstract record LoadingScreenPhaseEvent(Scene loadingScene, LoadingScreen openedLoadingScreen) : SceneOperationEventBase;

    #endregion
    #region Scene events

    /// <summary>Occurs when a scene is opened.</summary>
    /// <remarks>Called when: <see cref="When.Before"/>, <see cref="When.After"/>.</remarks>
    [CalledFor(When.Before, When.After), InvokationOrder(6)]
    public record SceneOpenEvent(Scene scene) : SceneEvent(scene);

    /// <summary>Occurs when a scene is preloaded.</summary>
    /// <remarks>Called when: <see cref="When.Before"/>, <see cref="When.After"/>.</remarks>
    [CalledFor(When.Before, When.After), InvokationOrder(9)]
    public record ScenePreloadEvent(Scene scene) : SceneEvent(scene);

    /// <summary>Occurs when a scene is closed.</summary>
    /// <remarks>Called when: <see cref="When.Before"/>, <see cref="When.After"/>.</remarks>
    [CalledFor(When.Before, When.After), InvokationOrder(3)]
    public record SceneCloseEvent(Scene scene) : SceneEvent(scene);

    #endregion
    #region Collection callbacks

    /// <summary>Occurs when a collection is opened.</summary>
    /// <remarks>Called when: <see cref="When.Unspecified"/> (it will be ignored).</remarks>
    [CalledFor(When.Unspecified), InvokationOrder(7)]
    public record CollectionOpenEvent(SceneCollection collection) : CollectionEvent(collection);

    /// <summary>Occurs when a collection is closed.</summary>
    /// <remarks>Called when: <see cref="When.Unspecified"/> (it will be ignored).</remarks>
    [CalledFor(When.Unspecified), InvokationOrder(4)]
    public record CollectionCloseEvent(SceneCollection collection) : CollectionEvent(collection);

    #endregion
    #region Phase callbacks

    /// <summary>Occurs before operation has begun working, but after it has started.</summary>
    /// <remarks>Properties has not been frozen at this point, and can be changed.
    /// <br/><br/>
    /// Called when: <see cref="When.Unspecified"/> (it will be ignored).</remarks>
    [CalledFor(When.Unspecified), InvokationOrder(0)]
    public record StartPhaseEvent : SceneOperationEventBase;

    /// <summary>Occurs when a loading screen is opened.</summary>
    /// <remarks>Called regardless if operation actually opens one or not.
    /// <br/><br/>
    /// Called when: <see cref="When.Before"/>, <see cref="When.After"/>.</remarks>
    [CalledFor(When.Before, When.After), InvokationOrder(1)]
    public record LoadingScreenOpenPhaseEvent(Scene loadingScene, LoadingScreen openedLoadingScreen) : LoadingScreenPhaseEvent(loadingScene, openedLoadingScreen);

    /// <summary>Occurs when operation starts and finishes closing scenes.</summary>
    /// <remarks>Called when: <see cref="When.Before"/>, <see cref="When.After"/>.</remarks>
    [CalledFor(When.Before, When.After), InvokationOrder(3)]
    public record SceneClosePhaseEvent(IEnumerable<Scene> scenes) : ScenePhaseEvent(scenes);

    /// <summary>Occurs when operation starts and finishes opening scenes.</summary>
    /// <remarks>Called when: <see cref="When.Before"/>, <see cref="When.After"/>.</remarks>
    [CalledFor(When.Before, When.After), InvokationOrder(5)]
    public record SceneOpenPhaseEvent(IEnumerable<Scene> scenes) : ScenePhaseEvent(scenes);

    /// <summary>Occurs when operation starts and finishes preloading scenes.</summary>
    /// <remarks>Called when: <see cref="When.Before"/>, <see cref="When.After"/>.</remarks>
    [CalledFor(When.Before, When.After), InvokationOrder(8)]
    public record ScenePreloadPhaseEvent(IEnumerable<Scene> scenes) : ScenePhaseEvent(scenes);

    /// <summary>Occurs when a loading screen is closed.</summary>
    /// <remarks>Called regardless if operation actually opens one or not.
    /// <br/><br/>
    /// Called when: <see cref="When.Before"/>, <see cref="When.After"/>.</remarks>
    [CalledFor(When.Before, When.After), InvokationOrder(10)]
    public record LoadingScreenClosePhaseEvent(Scene loadingScene, LoadingScreen openedLoadingScreen) : LoadingScreenPhaseEvent(loadingScene, openedLoadingScreen);

    /// <summary>Occurs before operation has stopped working, but after its practially done.</summary>
    /// <remarks>Called when: <see cref="When.Unspecified"/> (it will be ignored).</remarks>
    [CalledFor(When.Unspecified), InvokationOrder(11)]
    public record EndPhaseEvent : SceneOperationEventBase;

    #endregion
    #region App

    /// <summary>Occurs when ASM startup begins, opening any collections or scenes flagged to load during startup.</summary>
    public record StartupStartedEvent(StartupProps props) : EventCallbackBase;

    /// <summary>Occurs when ASM startup has completed successfully.</summary>
    public record StartupFinishedEvent(StartupProps props) : EventCallbackBase;

    /// <summary>Occurs when ASM startup is cancelled before completion.</summary>
    public record StartupCancelledEvent : EventCallbackBase;

    /// <summary>
    /// <para>Occurs when the application is quitting through <c>SceneManager.app.Quit()</c>.</para>
    /// <para>This event will not be raised if the default Unity quit flow is used.</para>
    /// </summary>
    public record QuitEvent : EventCallbackBase;

    /// <summary>Occurs when ASM becomes busy, as in: a scene operation is queued, or started without queue, assuming it was idle beforehand.</summary>
    public record SceneManagerBecameBusyEvent : EventCallbackBase;

    /// <summary>Occurs when ASM becomes idle, as in: scene operation queue is empty and no non-queued operations are running.</summary>
    public record SceneManagerBecameIdleEvent : EventCallbackBase;

    /// <summary>Occurs when all user scenes have been closed and only ASM fallback scene remains open.</summary>
    /// <remarks>Use this to gracefully handle the situation — for example, by returning to the main menu or similar.</remarks>
    public record AllScenesClosedEvent : EventCallbackBase;

    #endregion

    /// <summary>Occurs when <see cref="CoroutineUtility"/> starts or ends a coroutine.</summary>
    public record GlobalCoroutinesChanged(IEnumerable<GlobalCoroutine> coroutines) : EventCallbackBase;

}
