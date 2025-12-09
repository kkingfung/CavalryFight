using AdvancedSceneManager.Callbacks;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvancedSceneManager.Core
{

    partial class Runtime
    {

        /// <summary>Occurs when a <see cref="SceneLoader"/> is added.</summary>
        public event Action sceneLoaderAdded;

        /// <summary>Occurs when a <see cref="SceneLoader"/> is removed.</summary>
        public event Action sceneLoaderRemoved;

        /// <summary>Occurs when a <see cref="SceneLoader"/> is toggled for a scene.</summary>
        public event Action<(Scene scene, Type previousLoader, Type newLoader)> sceneLoaderToggled;

        internal void InvokeSceneLoaderToggled(Scene scene, Type previousLoader, Type newLoader) =>
            sceneLoaderToggled?.Invoke((scene, previousLoader, newLoader));

        internal List<SceneLoader> sceneLoaders = new();

        [OnLoad]
        static void InitializeSceneLoaders()
        {
            var sceneLoaders = ServiceUtility.Find<SceneLoader>();
            foreach (var loader in sceneLoaders)
                SceneManager.runtime.AddSceneLoader(loader);
        }

        /// <summary>Gets a list of all added scene loaders that can be toggled scene by scene.</summary>
        public IEnumerable<SceneLoader> GetToggleableSceneLoaders() =>
            sceneLoaders.Where(l => !l.isGlobal && !string.IsNullOrWhiteSpace(l.sceneToggleText));

        /// <summary>Gets the loader for <paramref name="scene"/>.</summary>
        public SceneLoader GetLoaderForScene(Scene scene, bool useOnlyGlobal = false)
        {
            SceneLoader globalLoader = null;

            var loaders = sceneLoaders;
            if (useOnlyGlobal)
                loaders = loaders.Where(l => l.isGlobal).ToList();

            foreach (var loader in loaders)
            {
                // skip if cant be activated
                if (!loader.canBeActivated)
                    continue;

                // return first found
                if (Match(loader, scene))
                    return loader;

                // Track global to use if we don't find a match
                if (globalLoader == null && loader.isGlobal && loader.CanHandleScene(scene))
                    globalLoader = loader;
            }

            return globalLoader;
        }

        /// <summary>Returns the scene loader with the specified key.</summary>
        public SceneLoader GetSceneLoader(string sceneLoader) =>
            sceneLoaders.FirstOrDefault(l => l.Key == sceneLoader);

        /// <summary>Returns the scene loader type with the specified key.</summary>
        public Type GetSceneLoaderType(string sceneLoader) =>
            GetSceneLoader(sceneLoader)?.GetType();

        bool Match(SceneLoader loader, Scene scene) =>
            loader.GetType().AssemblyQualifiedName == scene.sceneLoader && loader.CanHandleScene(scene);

        /// <summary>Adds a scene loader.</summary>
        public void AddSceneLoader<T>() where T : SceneLoader, new()
        {
            var key = SceneLoader.GetKey<T>();
            sceneLoaders.RemoveAll(l => l.Key == key);
            sceneLoaders.Add(new T());
            sceneLoaderAdded?.Invoke();
        }

        internal void AddSceneLoader(SceneLoader sceneLoader)
        {
            try
            {
                var key = sceneLoader.Key;
                sceneLoaders.RemoveAll(l => l.Key == key);
                sceneLoaders.Add(sceneLoader);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        /// <summary>Removes a scene loader.</summary>
        public void RemoveSceneLoader<T>()
        {
            sceneLoaders.RemoveAll(l => l is T);
            sceneLoaderRemoved?.Invoke();
        }

    }

}
