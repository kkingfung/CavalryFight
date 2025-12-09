using System;
using AdvancedSceneManager.Loading;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;

namespace AdvancedSceneManager.Core
{

    partial class Runtime
    {

        /// <summary>Occurs when a scene is opened.</summary>
        public event Action<Scene> sceneOpened;

        /// <summary>Occurs when a scene is closed.</summary>
        public event Action<Scene> sceneClosed;

        /// <summary>Occurs when a collection is opened.</summary>
        public event Action<SceneCollection> collectionOpened;

        /// <summary>Occurs when a collection is closed.</summary>
        public event Action<SceneCollection> collectionClosed;

        /// <summary>Occurs when a scene is preloaded.</summary>
        public event Action<Scene> scenePreloaded;

        /// <summary>Occurs when a previously preloaded scene is opened.</summary>
        public event Action<Scene> scenePreloadFinished;

        /// <summary>Occurs when ASM has started working and is running scene operations.</summary>
        public event Action startedWorking;

        /// <summary>Occurs when ASM has finished working and no scene operations are running.</summary>
        public event Action stoppedWorking;

        /// <summary>Occurs when the last user scene closes.</summary>
        /// <remarks> 
        /// <para>This usually happens by mistake, and likely means that no user code would run, this is your chance to restore to a known state (return to main menu, for example), or crash to desktop.</para>
        /// <para>Returning to main menu can be done like this:<code>SceneManager.app.Restart()</code></para>
        /// </remarks>
        public Action onAllScenesClosed;

        /// <inheritdoc cref="LoadingScreenUtility.RegisterLoadProgressListener(ILoadProgressListener)"/>
        public void AddProgressListener(ILoadProgressListener listener) =>
            LoadingScreenUtility.RegisterLoadProgressListener(listener);

        /// <inheritdoc cref="LoadingScreenUtility.UnregisterLoadProgressListener(ILoadProgressListener)"/>
        public void RemoveProgressListener(ILoadProgressListener listener) =>
            LoadingScreenUtility.UnregisterLoadProgressListener(listener);

    }

}
