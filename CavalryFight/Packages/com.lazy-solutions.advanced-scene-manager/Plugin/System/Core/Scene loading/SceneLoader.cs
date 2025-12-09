using System;
using System.Collections;
using AdvancedSceneManager.Models;
using UnityEngine;

namespace AdvancedSceneManager.Core
{

    /// <summary>Specifies a scene loader.</summary>
    public abstract class SceneLoader
    {

        /// <summary>Represents an indicator to be displayed on a scene field when the associated <see cref="SceneLoader"/> is toggled, and also in the scene loader toggles in the scene popup.</summary>
        public struct Indicator
        {

            /// <summary>The text to display on the indicator button. If <see cref="useFontAwesome"/> is true, this should be a Font Awesome Unicode character.</summary>
            /// <remarks>This should always be set, given it is used for the scene loader toggles in the scene popup.</remarks>
            public string text { get; set; }

            /// <summary>The tooltip shown when the user hovers over the indicator button.</summary>
            public string tooltip { get; set; }

            /// <summary>Indicates whether the <see cref="text"/> should be interpreted as a Font Awesome Unicode character.</summary>
            public bool useFontAwesome { get; set; }

            /// <summary>Indicates whether the Font Awesome icon is from the "Brands" subset. Only relevant if <see cref="useFontAwesome"/> is true.</summary>
            public bool useFontAwesomeBrands { get; set; }

            /// <summary>The icon to display on the indicator button. Overrides <see cref="text"/>, when displayed on a scene field.</summary>
            public Func<Texture2D> icon { get; set; }

            /// <summary>Color to apply to the indicator icon or text.</summary>
            public Color? color { get; set; }

            /// <summary>Specifies a handler for when the indicator is clicked.</summary>
            /// <remarks>Only applicable when on a scene field.</remarks>
            public Action<Scene> onClick { get; set; }

        }

        /// <summary>Gets the key for the specified scene loader.</summary>
        public static string GetKey<T>() where T : SceneLoader => typeof(T).AssemblyQualifiedName;

        /// <summary>Gets the key for the specified scene loader.</summary>
        public static string GetKey<T>(T obj) where T : SceneLoader => obj.GetType().AssemblyQualifiedName;

        /// <summary>Gets the key for this scene loader.</summary>
        /// <remarks>This is equal to <see cref="System.Type.AssemblyQualifiedName"/>.</remarks>
        public string Key => GetKey(this);

        /// <summary>Specifies the text to display on the toggle in scene popup. Only has an effect if <see cref="isGlobal"/> is <see langword="false"/>.</summary>
        public virtual string sceneToggleText { get; }

        /// <summary>Specifies the tooltip to display on the toggle in scene popup. Only has an effect if <see cref="isGlobal"/> is <see langword="false"/>.</summary>
        public virtual string sceneToggleTooltip { get; }

        /// <summary>Specifies the indicator on scene fields for this scene loader.</summary>
        public virtual Indicator indicator { get; }

        /// <summary>Specifies if this scene loader will can be applied to all scenes. Otherwise scenes will have to be explicitly flagged to open with this loader.</summary>
        /// <remarks>
        /// To flag a scene to be opened with this loader, the following two methods can be used:
        /// <para>If <see cref="sceneToggleText"/> is non-empty, a toggle will be displayed in scene popup.</para>
        /// <para>Programmatically <see cref="Scene.SetSceneLoader{T}"/> can be used.</para>
        /// </remarks>
        public virtual bool isGlobal { get; } = true;

        /// <summary>Gets whatever this scene loader can handle the scene.</summary>
        public virtual bool CanHandleScene(Scene scene) => true;

        /// <summary>Specifies whatever this loader will run outside of play mode or not.</summary>
        public virtual bool activeOutsideOfPlayMode { get; }

        /// <summary>Specifies whatever this loader will run in play mode or not.</summary>
        public virtual bool activeInPlayMode { get; } = true;

        /// <summary>Specifies whatever scenes using this loader should be added to build settings scene list.</summary>
        public virtual bool addScenesToBuildSettings { get; } = true;

        /// <summary>Gets whatever this loader may be activated in the current context.</summary>
        public bool canBeActivated =>
            Application.isPlaying
            ? activeInPlayMode
            : activeOutsideOfPlayMode;

        /// <summary>Loads the scene specified in e.scene.</summary>
        public abstract IEnumerator LoadScene(Scene scene, SceneLoadArgs e);

        /// <summary>Unloads the scene specified in e.scene.</summary>
        public abstract IEnumerator UnloadScene(Scene scene, SceneUnloadArgs e);

        /// <summary>Loads scene using default ASM loaders.</summary>
        public IEnumerator LoadDefault(SceneLoadArgs e) =>
            SceneLoaderExtensions.RunSceneLoader(e, useOnlyGlobal: true);

        /// <summary>Unloads scene using default ASM loaders.</summary>
        public IEnumerator UnloadDefault(SceneUnloadArgs e) =>
            SceneLoaderExtensions.RunSceneLoader(e, useOnlyGlobal: true);
    }

}
