using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.Editor.UI;
using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Editor.UI.Views.Popups;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Interfaces;
using AdvancedSceneManager.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor
{

    partial class SceneAssetEditor
    {

        public class MainPage : ScenePopup.SubPage
        {

            public override VisualElement CreateGUI() => new();

            protected override void OnAdded()
            {

                if (!context.scene)
                    throw new System.ArgumentNullException(nameof(context.scene));

                //Hide horizontal scrollbar
                view.schedule.Execute(() =>
                {
                    if (view.GetAncestor<ScrollView>() is { } scroll)
                        scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                });

                stack.header.displayCloseButton = false;

                GenerateCollectionButtons();

                RegisterEvent<ScenesAvailableForImportChangedEvent>(e =>
                {
                    RefreshCollectionButtons();
                });

            }

            void GenerateCollectionButtons()
            {

                var scene = context.scene;

                var collections = SceneManager.profile.collections.OfType<IEditableCollection>().ToList();
                if (SceneManager.profile.standaloneScenes.Contains(scene))
                    collections.Add(SceneManager.profile.standaloneScenes);

                foreach (var collection in collections)
                {

                    var container = new VisualElement();
                    container.style.flexDirection = FlexDirection.Row;

                    var button = new NavigationButton { text = collection.title, iconLeft = "\uf03a" };
                    button.style.flexGrow = 1;

                    var context = new ViewModelContext(collection, scene, collection.sceneList.IndexOf(scene), ScenePopup.CustomParams.isInspector);

                    button.RegisterCallback<ClickEvent>(e =>
                    {
                        view.GetTopAncestor<PageStackView>().parentContext = context;
                        view.GetTopAncestor<PageStackView>().Push<ScenePopup.MainPage>();
                    });

                    var addButton = new Button(AddScene) { text = "\u002b", tooltip = "Add scene to collection" };
                    var removeButton = new Button(RemoveScene) { text = "\uf1f8", tooltip = "Remove scene from collection" };
                    addButton.UseFontAwesome();
                    removeButton.UseFontAwesome();
                    addButton.style.width = 42;
                    addButton.style.height = 42;
                    removeButton.style.width = 42;
                    removeButton.style.height = 42;
                    addButton.style.alignSelf = Align.Center;
                    removeButton.style.alignSelf = Align.Center;

                    container.Add(addButton);
                    container.Add(removeButton);
                    container.Add(button);

                    button.userData = (context, addButton, removeButton);
                    view.Add(container);

                    RefreshCollectionButton(button);

                    void AddScene() => collection.Add(scene);
                    void RemoveScene() => collection.Remove(scene);

                }

            }

            void RefreshCollectionButtons()
            {
                view.Query<NavigationButton>().ForEach(RefreshCollectionButton);
            }

            void RefreshCollectionButton(NavigationButton button)
            {

                (ViewModelContext context, Button addButton, Button removeButton) = ((ViewModelContext, Button, Button))button.userData;

                button.SetEnabled(context.baseCollection!.Contains(context.scene));

                addButton.SetVisible(!context.baseCollection.Contains(context.scene));
                removeButton.SetVisible(context.baseCollection.Contains(context.scene));

            }

        }

    }

}
