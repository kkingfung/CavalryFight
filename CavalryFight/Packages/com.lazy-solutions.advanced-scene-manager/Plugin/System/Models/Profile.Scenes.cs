using AdvancedSceneManager.Models.Interfaces;
using AdvancedSceneManager.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdvancedSceneManager.Models
{

    partial class Profile
    {

        [SerializeField] private Scene m_loadingScene;
        [SerializeField] private Scene m_splashScene;
        [SerializeField] private Scene m_startupScene;

        /// <summary>The startup scene.</summary>
        public Scene startupScene
        {
            get => m_startupScene;
            set { m_startupScene = value; OnPropertyChanged(); }
        }

        /// <summary>The default loading scene.</summary>
        public Scene loadingScene
        {
            get => m_loadingScene;
            set { m_loadingScene = value; OnPropertyChanged(); }
        }

        /// <summary>The splash scene.</summary>
        public Scene splashScene
        {
            get => m_splashScene;
            set { m_splashScene = value; OnPropertyChanged(); }
        }

        /// <summary>Gets the scenes managed by this profile.</summary>
        /// <remarks>Includes both collection and standalone scenes.</remarks>
        public IEnumerable<Scene> scenes =>
            allCollections.
            Where(HasValue).
            Where(IsIncluded).
            SelectMany(AllScenes).
            Concat(specialScenes).
            NonNull().
            Distinct();

        bool HasValue(ISceneCollection collection) =>
            collection is ScriptableObject c
            ? c
            : collection is not null;

        IEnumerable<Scene> AllScenes(ISceneCollection collection)
        {
            if (collection is SceneCollection c && c)
                return c.allScenes;
            else if (collection is ISceneCollection<Scene> c2)
                return c2.scenes;

            return Enumerable.Empty<Scene>();
        }

        internal IEnumerable<Scene> EnumerateAutoScenes() =>
            scenes.SelectMany(s => s.EnumerateAutoScenes()).Where(s => s.option is (Enums.AutoSceneOption.Always or Enums.AutoSceneOption.PlayModeOnly)).Select(s => s.scene).NonNull();

        internal IEnumerable<string> EnumerateAutoScenePaths() =>
            scenes.SelectMany(s => s.EnumerateAutoScenes()).Select(s => s.scenePath).NonNull().Where(path => path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase));

        /// <summary>Gets default loading screen, splash screen and startup loading screen.</summary>
        /// <remarks><see langword="null"/> is filtered out.</remarks>
        public IEnumerable<Scene> specialScenes =>
            new[] { loadingScene, splashScene }.
            Where(s => s).
            Distinct();

    }

}
