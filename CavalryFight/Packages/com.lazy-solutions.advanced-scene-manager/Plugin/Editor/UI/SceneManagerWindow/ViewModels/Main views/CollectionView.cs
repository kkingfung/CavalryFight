using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Editor.UI.Views.ItemTemplates;
using AdvancedSceneManager.Editor.UI.Views.Popups;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Interfaces;
using AdvancedSceneManager.Services;
using AdvancedSceneManager.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views
{

    sealed partial class CollectionView : ViewModel
    {

        [Inject] public ISearchService searchService { get; private set; } = null!;
        [Inject] public IProfileBindingsService profileBindingsService { get; private set; } = null!;

        public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.main.collection;
        public override bool useTemplateContainer => false;

        VisualTreeAsset collectionTemplate => ((SceneManagerWindow)window).viewLocator.items.collection;
        VisualTreeAsset sceneTemplate => ((SceneManagerWindow)window).viewLocator.items.scene;

        ListView collectionsList = null!;
        ListView dynamicCollectionsList = null!;
        VisualElement standaloneItem = null!;
        VisualElement defaultASMScenesItem = null!;
        ScrollView scroll = null!;

        EventCallback<AttachToPanelEvent> attachToPanelEvent = null!;

        protected override void OnAdded()
        {

            collectionsList = view.Q<ListView>("list-collections") ?? throw new InvalidOperationException("Could not find collections list");
            dynamicCollectionsList = view.Q<ListView>("list-dynamic-collections") ?? throw new InvalidOperationException("Could not find dynamic collections list");
            standaloneItem = view.Q("item-standalone-collection") ?? throw new InvalidOperationException("Could not find standalone collections item");
            defaultASMScenesItem = view.Q("item-asm-default-scenes-collection") ?? throw new InvalidOperationException("Could not find default asm collections item");
            scroll = view.Q<ScrollView>() ?? throw new InvalidOperationException("Could not find collection scrollview");

            profileBindingsService.BindEnabledToProfile(view.Q("label-no-profile"));

            Reload();
            //scroll.PersistScrollPosition();

            SetupAddButtons();

            RegisterEvent<ASMWindow.ReloadCollectionViewRequest>(e => Reload());
            RegisterEvent<OnWindowFocusEvent>(e => OnWindowFocus());
            RegisterEvent<OnWindowLostFocusEvent>(e => OnWindowLostFocus());

            RegisterEvent<ModelPropertyChangedEvent>(e =>
            {
                if (e.model == SceneManager.profile && (e.propertyName == nameof(Profile.defaultASMScenes)))
                    Reload();
            });

            RegisterEvent<ProfileChangedEvent>(e => Reload());

            //Fix for listview losing item content on window dock / undock
            attachToPanelEvent = new(_ =>
            {
                if (collectionsList.childCount == 0)
                    EditorApplication.delayCall += Reload;
            });

            view.RegisterCallback(attachToPanelEvent);

            UpdateAddHoverButtonsVisibility();
            RegisterEvent<ASMSettingsChangedEvent>(e => UpdateAddHoverButtonsVisibility());

        }

        protected override void OnRemoved()
        {
            view.UnregisterCallback(attachToPanelEvent);
            scroll.ClearScrollPosition();
        }

        public void Reload()
        {

            collectionsList.Unbind();
            dynamicCollectionsList.Unbind();
            standaloneItem.Clear();
            defaultASMScenesItem.Clear();

            if (!SceneManager.profile)
            {
                SetupLine();
                return;
            }

            collectionsList.makeNoneElement = MakeNoneItem;

            collectionsList.bindItem = BindSceneCollection;
            dynamicCollectionsList.bindItem = BindDynamicCollection;

            collectionsList.unbindItem = UnbindCollection;
            dynamicCollectionsList.unbindItem = UnbindCollection;

            BindSpecialCollection(standaloneItem, SceneManager.profile.standaloneScenes, "m_standaloneDynamicCollection");
            BindSpecialCollection(defaultASMScenesItem, SceneManager.profile.defaultASMScenes, "m_defaultASMScenes");

            collectionsList.Bind(ProfileUtility.serializedObject);
            dynamicCollectionsList.Bind(ProfileUtility.serializedObject);
            collectionsList.Rebuild();
            dynamicCollectionsList.Rebuild();
            SetupLine();

        }

        VisualElement MakeNoneItem() =>
            new Label("No collections added, you can add one below!");

        #region Collections

        void BindSpecialCollection(VisualElement container, ISceneCollection collection, string bindingPath)
        {
            container.Clear();

            if (collection is null)
                return;

            var instance = collectionTemplate.Instantiate(bindingPath);
            container.Add(instance);

            var viewModel = new CollectionItem();
            viewModel.Add(instance, new(collection));

            container.RegisterCallbackOnce<DetachFromPanelEvent>(e => _ = viewModel.Remove());
        }

        void BindSceneCollection(VisualElement element, int index)
        {
            var serializedProperty = (SerializedProperty)collectionsList.itemsSource[index];
            var collection = (ISceneCollection)serializedProperty.boxedValue;

            BindCollection(element, collection);
        }

        void BindDynamicCollection(VisualElement element, int index)
        {
            var serializedProperty = (SerializedProperty)dynamicCollectionsList.itemsSource[index];
            var collection = (ISceneCollection)serializedProperty.boxedValue;

            BindCollection(element, collection);
        }

        void BindCollection(VisualElement element, ISceneCollection collection)
        {
            var viewModel = new CollectionItem();
            viewModel.Add(element, new(collection), ignoreAddedCheck: true);
            element.userData = viewModel;
        }

        void UnbindCollection(VisualElement element, int index)
        {
            if (element.userData is CollectionItem viewModel)
                _ = viewModel.Remove(ignoreView: true); //Removing element causes re-order to stop working, so lets not
        }

        void SetupLine()
        {
            view.Q("line").visible = SceneManager.profile;
        }

        #endregion
        #region Hover add buttons

        readonly Dictionary<VisualElement, GlobalCoroutine> interactionBlockCoroutines = new();
        GlobalCoroutine focusInteractionBlockCoroutine;
        Button createCollectionButton = null!;
        Button templatesButton = null!;
        Button createDynamicButton = null!;
        Button dynamicMenuButton = null!;
        Label fakeCollectionText = null!;
        Label fakeDynamicText = null!;

        void OnWindowFocus()
        {

            focusInteractionBlockCoroutine?.Stop();
            focusInteractionBlockCoroutine = Coroutine().StartCoroutine();
            IEnumerator Coroutine()
            {
                yield return new WaitForSecondsRealtime(0.25f);
                SetAddButtonsVisible();
            }

        }

        void OnWindowLostFocus()
        {
            SetAddButtonsVisible();
        }

        void SetAddButtonsVisible()
        {
            createCollectionButton.SetVisible(SceneManager.settings.user.displayCollectionAddButton && SceneManager.profile);
            templatesButton.SetVisible(SceneManager.settings.user.displayCollectionTemplatesButton && SceneManager.profile);
            createDynamicButton.SetVisible(SceneManager.settings.user.displayDynamicCollectionAddButton && SceneManager.profile);
            dynamicMenuButton.SetVisible(SceneManager.settings.user.displayDynamicCollectionMenuButton && SceneManager.profile);
            fakeCollectionText.SetVisible(SceneManager.settings.user.displayCollectionAddButton && SceneManager.profile);
            fakeDynamicText.SetVisible(SceneManager.settings.user.displayDynamicCollectionAddButton && SceneManager.profile);
        }

        void SetupAddButtons()
        {

            createCollectionButton = view.Q<Button>("button-create-collection");
            templatesButton = view.Q<Button>("button-templates");
            createDynamicButton = view.Q<Button>("button-create-dynamic");
            dynamicMenuButton = view.Q<Button>("button-dynamic-menu");
            fakeCollectionText = view.Q<Label>("fake-label-button-create-collection");
            fakeDynamicText = view.Q<Label>("fake-label-button-create-dynamic-collection");

            RegisterEvent<ProfileChangedEvent>(e => UpdateAddHoverButtonsVisibility());

            view.Query("row-add").ForEach(element =>
            {
                element.SetEnabled(false);
                element.RegisterCallback<PointerEnterEvent>(PointerEnter);
                element.RegisterCallback<PointerLeaveEvent>(PointerLeave);

                void PointerEnter(PointerEnterEvent e)
                {
                    element.EnableInClassList("disabled", true);
                    element.SetEnabled(false);

                    interactionBlockCoroutines.GetValueOrDefault(element)?.Stop();
                    interactionBlockCoroutines.Set(element, Coroutine().StartCoroutine());

                    IEnumerator Coroutine()
                    {
                        yield return new WaitForSecondsRealtime(0.25f);
                        element.EnableInClassList("disabled", false);
                        element.SetEnabled(true);
                        interactionBlockCoroutines.Remove(element);
                    }
                }

                void PointerLeave(PointerLeaveEvent e)
                {
                    element.SetEnabled(false);
                    interactionBlockCoroutines.Remove(element);
                }

            });

            createCollectionButton.RegisterCallback<ClickEvent>(e => SceneManager.profile.CreateCollection());
            templatesButton.RegisterCallback<ClickEvent>(e => ASMWindow.OpenPopup<CollectionTemplatesPopup>());
            createDynamicButton.RegisterCallback<ClickEvent>(e => SceneManager.profile.CreateDynamicCollection());

            dynamicMenuButton.RegisterCallback<ClickEvent>(e =>
            {
                if (((VisualElement)e.target).ClassListContains("disabled"))
                    return;

                var menu = new GenericDropdownMenu();

                menu.AddItem("Add dynamic collection", isChecked: false, SceneManager.profile.CreateDynamicCollection);
                menu.AddSeparator("");

                if (!SceneManager.profile.defaultASMScenes)
                    menu.AddItem("Add default ASM scenes collection", isChecked: false, () => SceneManager.profile.AddDefaultASMScenes());
                else
                    menu.AddDisabledItem("Add default ASM scenes collection", isChecked: false);

                if (!SceneManager.profile.standaloneScenes)
                    menu.AddItem("Add standalone collection", isChecked: false, () => SceneManager.profile.AddCollection(ScriptableObject.CreateInstance<StandaloneCollection>()));

                menu.DropDown(dynamicMenuButton.worldBound, dynamicMenuButton, anchored: false);
            });

        }

        void UpdateAddHoverButtonsVisibility()
        {
            createCollectionButton.SetVisible(SceneManager.settings.user.displayCollectionAddButton && SceneManager.profile);
            templatesButton.SetVisible(SceneManager.settings.user.displayCollectionTemplatesButton && SceneManager.profile);
            createDynamicButton.SetVisible(SceneManager.settings.user.displayDynamicCollectionAddButton && SceneManager.profile);
            dynamicMenuButton.SetVisible(SceneManager.settings.user.displayDynamicCollectionMenuButton && SceneManager.profile);
            fakeCollectionText.SetVisible(SceneManager.settings.user.displayCollectionAddButton && SceneManager.profile);
            fakeDynamicText.SetVisible(SceneManager.settings.user.displayDynamicCollectionAddButton && SceneManager.profile);
        }

        #endregion

    }

}
