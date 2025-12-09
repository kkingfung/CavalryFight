using AdvancedSceneManager.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    [UxmlElement]
    public partial class SceneLoaderPicker : VisualElement
    {

        public SceneLoaderPicker()
        {
            SceneManager.runtime.sceneLoaderAdded += Reload;
            SceneManager.runtime.sceneLoaderRemoved += Reload;

            RegisterCallbackOnce<DetachFromPanelEvent>(e =>
            {
                ResetModels();
                SceneManager.runtime.sceneLoaderAdded -= Reload;
                SceneManager.runtime.sceneLoaderRemoved -= Reload;
            });
        }

        void ResetModels()
        {

            Initialize(scenes: Array.Empty<Scene>()); //Unregister listeners for existing scenes

            if (collection)
                collection.PropertyChanged -= Collection_PropertyChanged;

            collection = null;
            scene = null;

        }

        public SceneCollection collection { get; private set; }
        public Scene scene { get; private set; }

        readonly List<Scene> sceneList = new();

        public void Initialize(SceneCollection collection)
        {

            if (!collection)
                return;
            ResetModels();

            this.collection = collection;
            Initialize(collection.scenes.ToArray());

            collection.PropertyChanged += Collection_PropertyChanged;

        }

        public void Initialize(Scene scene)
        {

            if (!scene)
                return;
            ResetModels();

            this.scene = scene;
            Initialize(scenes: scene);

        }

        void Initialize(params Scene[] scenes)
        {

            if (sceneList.Count > 0)
            {
                foreach (var scene in sceneList)
                    if (scene)
                        scene.PropertyChanged -= Scene_PropertyChanged;

                sceneList.Clear();
            }

            if (scenes.Length > 0)
            {
                foreach (var scene in scenes)
                    if (scene)
                        scene.PropertyChanged += Scene_PropertyChanged;

                sceneList.AddRange(scenes);
            }

            Reload();

        }

        private void Collection_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SceneCollection.scenes))
                Initialize(collection);
        }

        private void Scene_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Scene.sceneLoader))
                Reload();
        }

        void Reload()
        {

            Clear();

            foreach (var loader in SceneManager.runtime.GetToggleableSceneLoaders().OrderBy(l => l.Key).ToArray())
            {

                var isCheck = sceneList.Where(s => s).All(s => s.sceneLoader == loader.Key);
                var isMixedValue = !isCheck && sceneList.Where(s => s).Any(s => s.sceneLoader == loader.Key);

                var button = new Toggle
                {
                    label = loader.sceneToggleText,
                    tooltip = loader.sceneToggleTooltip,
                    showMixedValue = isMixedValue
                };

                button.SetValueWithoutNotify(isCheck);
                button.RegisterValueChangedCallback(e =>
                {

                    foreach (var scene in sceneList)
                    {

                        if (!scene)
                            continue;

                        if (e.newValue)
                            scene.sceneLoader = loader.Key;
                        else
                            scene.ClearSceneLoader();

                        scene.Save();

                    }

                    Reload();

                });

                Add(button);

            }

        }

    }

}
