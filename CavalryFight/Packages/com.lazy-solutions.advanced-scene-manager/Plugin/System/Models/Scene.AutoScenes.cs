using AdvancedSceneManager.Models.Enums;
using AdvancedSceneManager.Models.Interfaces;
using AdvancedSceneManager.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AdvancedSceneManager.Models
{

    /// <summary>Represents an auto scene entry, which is a scene that will be automatically opened or closed (depending on option), when the parent scene is.</summary>
    /// <remarks>
    /// Not meant for direct use, use the following instead:
    /// <code>
    /// using AdvancedSceneManager.Utility;
    /// 
    /// Scene.SetAutoScene(Scene, AutoSceneOption)
    /// </code>
    /// </remarks>
    [Serializable]
    public class AutoSceneEntry : ISerializationCallbackReceiver
    {

        /// <summary>The scene this entry refers to.</summary>
        /// <remarks>Either <see cref="scene"/> or <see cref="scenePath"/> must be defined for entry to be valid.</remarks>
        public Scene scene;

        /// <summary>The path to the Unity scene asset.</summary>
        /// <remarks>Either <see cref="scene"/> or <see cref="scenePath"/> must be defined for entry to be valid.</remarks>
        public string scenePath;

        /// <summary>The pre-defined option specifying how ASM will handle the scene.</summary>
        /// <remarks>When a scene is specified as an auto scene, then ASM will automatically open or close it when the parent scene does.</remarks>
        [NonSerialized] public AutoSceneOption? option;

        /// <summary>The custom option for this entry.</summary>
        /// <remarks>Meant for plugins or similar, ASM will not handle custom auto scenes itself.</remarks>
        public string customOption;

        /// <summary>Gets if this entry is valid.</summary>
        public bool IsValid() =>
            (scene || !string.IsNullOrWhiteSpace(scenePath)) &&
            (option.HasValue || !string.IsNullOrWhiteSpace(customOption));

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (option.HasValue)
                customOption = option.Value.ToString();
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (Enum.TryParse<AutoSceneOption>(customOption, out var value))
            {
                option = value;
                customOption = null;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (option.HasValue)
                return scene + ": " + option.Value.ToString();
            else if (!string.IsNullOrWhiteSpace(customOption))
                return scene ? scene : scenePath + ": " + customOption;

            return base.ToString();
        }

    }

    public partial class Scene :
        IAutoScenes,
        IAutoScenes<Scene, AutoSceneOption>,
        IAutoScenes<Scene, string>,
        IAutoScenes<string, AutoSceneOption>,
        IAutoScenes<string, string>

#if UNITY_EDITOR
        , IAutoScenes<SceneAsset, AutoSceneOption>
        , IAutoScenes<SceneAsset, string>
#endif
    {

        #region IAutoScenes<Scene, AutoSceneOption>

        /// <inheritdoc cref="AutoSceneUtility.SetAutoScene{TKey, TOption}(IAutoScenes{TKey, TOption}, TKey, TOption)"/>
        public void SetAutoScene(Scene scene, AutoSceneOption option) =>
            ((IAutoScenes<Scene, AutoSceneOption>)this).SetAutoScene(scene, option);

        /// <inheritdoc cref="AutoSceneUtility.FindAutoScene{TKey, TOption}(IAutoScenes{TKey, TOption}, TKey, TOption)"/>
        public AutoSceneEntry FindAutoScene(Scene scene, AutoSceneOption option) =>
            ((IAutoScenes<Scene, AutoSceneOption>)this).FindAutoScene(scene, option);

        #endregion
        #region IAutoScenes<Scene, string>

        /// <inheritdoc cref="AutoSceneUtility.SetAutoScene{TKey, TOption}(IAutoScenes{TKey, TOption}, TKey, TOption)"/>
        public void SetAutoScene(Scene scene, string customOption) =>
            ((IAutoScenes<Scene, string>)this).SetAutoScene(scene, customOption);

        /// <inheritdoc cref="AutoSceneUtility.FindAutoScene{TKey, TOption}(IAutoScenes{TKey, TOption}, TKey, TOption)"/>
        public AutoSceneEntry FindAutoScene(Scene scene, string customOption) =>
            ((IAutoScenes<Scene, string>)this).FindAutoScene(scene, customOption);

        #endregion
        #region IAutoScenes<string, AutoSceneOption>

        /// <inheritdoc cref="AutoSceneUtility.SetAutoScene{TKey, TOption}(IAutoScenes{TKey, TOption}, TKey, TOption)"/>
        public void SetAutoScene(string scenePath, AutoSceneOption option) =>
            ((IAutoScenes<string, AutoSceneOption>)this).SetAutoScene(scenePath, option);

        /// <inheritdoc cref="AutoSceneUtility.FindAutoScene{TKey, TOption}(IAutoScenes{TKey, TOption}, TKey, TOption)"/>
        public AutoSceneEntry FindAutoScene(string scenePath, AutoSceneOption option) =>
            ((IAutoScenes<string, AutoSceneOption>)this).FindAutoScene(scenePath, option);

        #endregion
        #region IAutoScenes<string, string>

        /// <inheritdoc cref="AutoSceneUtility.SetAutoScene{TKey, TOption}(IAutoScenes{TKey, TOption}, TKey, TOption)"/>
        public void SetAutoScene(string scenePath, string customOption) =>
            ((IAutoScenes<string, string>)this).SetAutoScene(scenePath, customOption);

        /// <inheritdoc cref="AutoSceneUtility.FindAutoScene{TKey, TOption}(IAutoScenes{TKey, TOption}, TKey, TOption)"/>
        public AutoSceneEntry FindAutoScene(string scenePath, string customOption) =>
            ((IAutoScenes<string, string>)this).FindAutoScene(scenePath, customOption);

        #endregion

#if UNITY_EDITOR

        #region IAutoScenes<SceneAsset, AutoSceneOption> 

        /// <inheritdoc cref="AutoSceneUtility.SetAutoScene{TKey, TOption}(IAutoScenes{TKey, TOption}, TKey, TOption)"/>
        /// <remarks>Only available in editor. Convinience method for <see cref="SetAutoScene(string, AutoSceneOption)"/>.</remarks>
        public void SetAutoScene(SceneAsset scene, AutoSceneOption option) =>
            ((IAutoScenes<string, AutoSceneOption>)this).SetAutoScene(AssetDatabase.GetAssetPath(scene), option);

        /// <inheritdoc cref="AutoSceneUtility.FindAutoScene{TKey, TOption}(IAutoScenes{TKey, TOption}, TKey, TOption)"/>
        /// <remarks>Only available in editor. Convinience method for <see cref="FindAutoScene(string, AutoSceneOption)"/>.</remarks>
        public AutoSceneEntry FindAutoScene(SceneAsset scene, AutoSceneOption option) =>
            ((IAutoScenes<string, AutoSceneOption>)this).FindAutoScene(AssetDatabase.GetAssetPath(scene), option);

        #endregion
        #region IAutoScenes<SceneAsset, string>

        /// <inheritdoc cref="AutoSceneUtility.SetAutoScene{TKey, TOption}(IAutoScenes{TKey, TOption}, TKey, TOption)"/>
        /// <remarks>Only available in editor. Convinience method for <see cref="SetAutoScene(string, string)"/>.</remarks>
        public void SetAutoScene(SceneAsset scene, string customOption) =>
            ((IAutoScenes<string, string>)this).SetAutoScene(AssetDatabase.GetAssetPath(scene), customOption);

        /// <inheritdoc cref="AutoSceneUtility.FindAutoScene{TKey, TOption}(IAutoScenes{TKey, TOption}, TKey, TOption)"/>
        /// <remarks>Only available in editor. Convinience method for <see cref="FindAutoScene(string, string)"/>.</remarks>
        public AutoSceneEntry FindAutoScene(SceneAsset scene, string customOption) =>
            ((IAutoScenes<string, string>)this).FindAutoScene(AssetDatabase.GetAssetPath(scene), customOption);

        #endregion

#endif

        [SerializeField] internal List<AutoSceneEntry> m_autoScenes = new();
        List<AutoSceneEntry> IAutoScenes.autoScenes => m_autoScenes;

        /// <summary>Enumerates all auto scenes on this scene.</summary>
        public IEnumerable<AutoSceneEntry> EnumerateAutoScenes() =>
            m_autoScenes.AsReadOnly();

        /// <summary>Finds the auto scene with the specified custom option on this scene.</summary>
        public bool FindAutoSceneForOption(string customOption, out AutoSceneEntry entry)
        {
            entry = m_autoScenes.FirstOrDefault(entry => entry.customOption == customOption);
            return entry.IsValid();
        }

    }

}
