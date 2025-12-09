using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AdvancedSceneManager.Models.Interfaces;

#if UNITY_EDITOR
using AdvancedSceneManager.Editor.Utility;
using UnityEditor;
#endif

namespace AdvancedSceneManager.Models
{

    /// <summary>Represents a collection that can take a path and then gather all scenes within, guaranteeing that they are all added to build, including non-imported and blacklisted scenes.</summary>
    [Serializable]
    public class DynamicCollection : DynamicCollectionBase<string>, ISelectableCollection
    {

        [SerializeField] internal string m_path;
        [SerializeField] internal List<string> m_cachedPaths = new();

        /// <inheritdoc />
        public override IEnumerable<string> scenes => m_cachedPaths;

        /// <inheritdoc />
        public override int count => scenePaths.Count();

        /// <summary>Gets the paths of the scenes tracked by this dynamic collection.</summary>
        /// <remarks>Uses <see cref="ReloadPaths"/> in editor, could be heavy.</remarks>
        public override IEnumerable<string> scenePaths
        {
            get
            {
#if UNITY_EDITOR
                ReloadPaths();
#endif
                return m_cachedPaths ??= new();
            }
        }

        /// <summary>Specifies the path that this dynamic collection will gather scenes from.</summary>
        public string path
        {
            get => m_path;
            set { m_path = value; OnPropertyChanged(); }
        }

#if UNITY_EDITOR

        /// <summary>Queries all <see cref="SceneAsset"/> in the project that is in the defined path, and is not blacklisted.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void ReloadPaths()
        {

            var paths =
                AssetDatabase.IsValidFolder(path)
                ? AssetDatabaseUtility.FindAssetPaths<SceneAsset>(path).
                  ToList()
                : new();

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) is SceneAsset asset && asset)
                paths.Add(path);

            if (m_cachedPaths == null || !paths.SequenceEqual(m_cachedPaths))
            {

                m_cachedPaths ??= new();
                m_cachedPaths.Clear();
                m_cachedPaths.AddRange(paths);

                OnPropertyChanged(nameof(scenePaths));
                Save();

            }

        }

#endif

        #region IFindable

        /// <inheritdoc />
        public override bool IsMatch(string q) =>
             !string.IsNullOrEmpty(q) && (IsNameMatch(q) || IsIDMatch(q) || IsPathMatch(q));

        /// <inheritdoc />
        protected bool IsPathMatch(string q) =>
             q == path;

        #endregion

        /// <summary>Finds the dynamic collction with the specified id.</summary>
        public static DynamicCollection Find(string id) =>
            SceneManager.assets.profiles.SelectMany(p => p.dynamicCollections).FirstOrDefault(c => c.id == id);

        /// <inheritdoc />
        public override string ToString() =>
            $"{title} ({(string.IsNullOrEmpty(path) ? "no path" : path)})";

#if UNITY_EDITOR
        /// <summary>Imports all scenes that are currently tracked by the collection.</summary>
        /// <remarks>Only available in editor.</remarks>
        public void ImportScenes() =>
            SceneImportUtility.Import(scenePaths);
#endif

    }

}
