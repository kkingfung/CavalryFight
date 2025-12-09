#if UNITY_EDITOR

using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace AdvancedSceneManager.Editor.UI
{

    /// <summary>Provides info about where a view model is hosted at in the ASM window.</summary>
    public readonly struct ViewModelContext
    {

        /// <summary>Provides info about where a view model is hosted at in the ASM window.</summary>
        public ViewModelContext(ISceneCollection collection = null, Scene scene = null, int? sceneIndex = null, object customParam = null)
        {

            baseCollection = collection;

            this.collection = (collection as SceneCollection)!;
            dynamicCollection = (collection as DynamicCollection)!;
            standaloneCollection = (collection as StandaloneCollection)!;
            defaultASMCollection = (collection as DefaultASMScenesCollection)!;

            this.scene = scene!;
            this.sceneIndex = sceneIndex;
            this.customParam = customParam;

        }

        /// <summary>Gets the associated collection as <see cref="ISceneCollection"/>, if hosted by a collection element.</summary>
        public ISceneCollection baseCollection { get; }

        /// <summary>Gets the associated collection as <see cref="SceneCollection"/>, if hosted by a collection element.</summary>
        /// <remarks><see langword="null"/> if collection is not correct type.</remarks>
        public SceneCollection collection { get; }

        /// <summary>Gets the associated collection as <see cref="DynamicCollection"/>, if hosted by a collection element.</summary>
        /// <remarks><see langword="null"/> if collection is not correct type.</remarks>
        public DynamicCollection dynamicCollection { get; }

        /// <summary>Gets the associated collection as <see cref="StandaloneCollection"/>, if hosted by a collection element.</summary>
        /// <remarks><see langword="null"/> if collection is not correct type.</remarks>
        public StandaloneCollection standaloneCollection { get; }

        /// <summary>Gets the associated collection as <see cref="DefaultASMScenesCollection"/>, if hosted by a collection element.</summary>
        /// <remarks><see langword="null"/> if collection is not correct type.</remarks>
        public DefaultASMScenesCollection defaultASMCollection { get; }

        /// <summary>Gets the associated scene, if hosted by a scene element.</summary>
        public Scene scene { get; }

        /// <summary>Gets the associated scene index, if hosted by a scene element, inside a collection element.</summary>
        public int? sceneIndex { get; }

        /// <summary>Gets the custom parameter that as passed from host.</summary>
        public object customParam { get; }

        /// <summary>Gets <see cref="customParam"/> as <typeparamref name="T"/>.</summary>
        public T OfType<T>() where T : class =>
            typeof(T) switch
            {
                Type t when t == typeof(ISceneCollection) => baseCollection as T,
                Type t when t == typeof(SceneCollection) => collection as T,
                Type t when t == typeof(DynamicCollection) => dynamicCollection as T,
                Type t when t == typeof(StandaloneCollection) => standaloneCollection as T,
                Type t when t == typeof(DefaultASMScenesCollection) => defaultASMCollection as T,
                Type t when t == typeof(Scene) => scene as T,
                _ => customParam as T
            };

        /// <inheritdoc />
        public override string ToString()
        {

            var parts = new List<string>();

            if (baseCollection is not null)
                parts.Add($"collection: {baseCollection.title}");
            if (scene)
                parts.Add($"scene: {scene.name}");
            if (sceneIndex is not null)
                parts.Add($"sceneIndex: {sceneIndex}");
            if (customParam is not null)
                parts.Add($"customParam: {customParam}");

            return $"ViewModelParam{{{string.Join(", ", parts)}}}";

        }

    }

}
#endif
