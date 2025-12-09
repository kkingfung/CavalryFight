using AdvancedSceneManager.Models.Interfaces;
using AdvancedSceneManager.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace AdvancedSceneManager.Models
{

    /// <summary>Represents a collection of standalone scenes. These scenes are guaranteed to be included in build (if the associated <see cref="Profile"/> is active).</summary>
    /// <remarks>Usage: <see cref="Profile.standaloneScenes"/>.</remarks>
    [Serializable]
    public class StandaloneCollection : DynamicCollectionBase<Scene>, IEditableCollection
    {

        [SerializeField, FormerlySerializedAs("m_scenes")] internal List<Scene> m_standaloneScenes = new();

        /// <inheritdoc />
        public override IEnumerable<Scene> scenes => m_standaloneScenes;
        List<Scene> IEditableCollection.sceneList => m_standaloneScenes;
        /// <inheritdoc />
        public override IEnumerable<string> scenePaths => scenes.NonNull().Select(s => s.path);

        /// <summary>Gets all scenes that will be opened on startup.</summary>
        public IEnumerable<Scene> startupScenes =>
            m_standaloneScenes.Where(s => s.openOnStartup);

        /// <inheritdoc />
        protected override void OnEnable()
        {
            m_description = "Standalone scenes are guaranteed to be included build, even if they are not contained in a normal collection.";
            Rename("Standalone");
        }

    }

}
