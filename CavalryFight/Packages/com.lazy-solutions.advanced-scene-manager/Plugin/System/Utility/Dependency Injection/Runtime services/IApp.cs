using AdvancedSceneManager.Utility;
using System;
using System.Collections;
using UnityEngine;
using app = AdvancedSceneManager.Core.App;

namespace AdvancedSceneManager.DependencyInjection
{

    /// <inheritdoc cref="app"/>
    public interface IApp : DependencyInjectionUtility.IInjectable
    {

        /// <inheritdoc cref="app.isASMPlay"/>
        bool isASMPlay { get; }

        /// <inheritdoc cref="app.isQuitting"/>
        bool isQuitting { get; }

        /// <inheritdoc cref="app.isRestart"/>
        bool isRestart { get; }

        /// <inheritdoc cref="app.isStartupFinished"/>
        bool isStartupFinished { get; }

        /// <inheritdoc cref="app.startupProps"/>
        app.StartupProps startupProps { get; set; }

        /// <inheritdoc cref="app.CancelQuit"/>
        void CancelQuit();

        /// <inheritdoc cref="app.CancelStartup"/>
        void CancelStartup();

        /// <inheritdoc cref="app.Exit"/>
        void Exit();

        /// <inheritdoc cref="app.Quit"/>
        void Quit(bool fade = true, Color? fadeColor = null, float fadeDuration = 1);

        /// <inheritdoc cref="app.RegisterQuitCallback"/>
        void RegisterQuitCallback(Func<IEnumerator> coroutine);

        /// <inheritdoc cref="app.UnregisterQuitCallback"/>
        void UnregisterQuitCallback(Func<IEnumerator> coroutine);

        /// <inheritdoc cref="app.Restart"/>
        void Restart(app.StartupProps props = null);

        /// <inheritdoc cref="app.RestartAsync"/>
        Async<bool> RestartAsync(app.StartupProps props = null);
    }

}
