using UnityEditor;

namespace AdvancedSceneManager.Models.Interfaces
{

    /// <summary>Defines some core properties for scenes.</summary>
    public interface IScene
    {

#if UNITY_EDITOR

        /// <summary>Gets the associated <see cref="SceneAsset"/>.</summary>
        /// <remarks>Only available in the editor.</remarks>
        public SceneAsset sceneAsset { get; }

        /// <summary>Gets if <see cref="sceneAsset"/> has a value.</summary>
        /// <remarks>Only available in the editor.</remarks>
        public bool hasSceneAsset { get; }

#endif

        /// <summary>Gets the asset id of the associated <see cref="sceneAsset"/>.</summary>
        public string sceneAssetGUID { get; }

        /// <summary>Gets the path of the associated <see cref="SceneAsset"/>.</summary>
        public string path { get; }

    }

}
