using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Services;
using System.Linq;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views
{

    class SelectionView : ViewModel
    {

        public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.main.selection;

        [Inject] public ISelectionService selectionService { get; private set; } = null!;

        protected override void OnAdded()
        {
            SetupUI();
            SetupUnselectOnBlankAreaClick();

            RegisterEvent<CollectionViewSelectionChangedEvent>(e => UpdateUI());
        }

        void SetupUnselectOnBlankAreaClick()
        {
            RegisterEvent<ASMWindowOpenEvent>(e => rootVisualElement.RegisterCallback<PointerDownEvent>(PointerDown));
            RegisterEvent<ASMWindowCloseEvent>(e => rootVisualElement.UnregisterCallback<PointerDownEvent>(PointerDown));
        }

        void PointerDown(PointerDownEvent e)
        {

            var element = (VisualElement)e.target;

            if (e.button == 0 && element.GetAncestor<TemplateContainer>() is null)
                selectionService.Clear();

        }

        void SetupUI()
        {
            SetupCollectionUI();
            SetupSceneUI();
            UpdateUI();
        }

        void UpdateUI()
        {
            UpdateCollectionUI();
            UpdateSceneUI();
            UpdateMargins();
        }

        void UpdateMargins()
        {
            var useSpacing = collectionsGroup.style.display == DisplayStyle.Flex && scenesGroup.style.display == DisplayStyle.Flex;
            view.EnableInClassList("useSpacing", useSpacing);
        }

        #region Collection UI

        GroupBox collectionsGroup = null!;
        Label collectionCountLabel = null!;

        Button collectionViewButton = null!;
        Button collectionCreateTemplateButton = null!;

        void SetupCollectionUI()
        {
            collectionsGroup = view.Q<GroupBox>("group-collections");
            collectionCountLabel = collectionsGroup.Q<Label>("count-collections");
            collectionViewButton = collectionsGroup.Q<Button>("button-view");
            collectionCreateTemplateButton = collectionsGroup.Q<Button>("button-create-template");
        }

        void UpdateCollectionUI()
        {

            var collections = selectionService.collections.ToList();

            // Only consider SceneCollections
            var sceneCollections = collections.OfType<SceneCollection>().ToList();

            // Show/hide group + update count
            collectionsGroup.SetVisible(collections.Count > 0);
            collectionCountLabel.text = collections.Count.ToString();

            // Enable buttons only if exactly one item is selected AND it's a SceneCollection
            bool singleSceneCollection =
                collections.Count == 1 && sceneCollections.Count == 1;

            collectionViewButton.SetEnabled(singleSceneCollection);
            collectionCreateTemplateButton.SetEnabled(singleSceneCollection);

            // Reset old handlers to avoid accumulating subscriptions
            collectionViewButton.clicked -= OnViewButtonClicked;
            collectionViewButton.clicked += OnViewButtonClicked;

            collectionCreateTemplateButton.clicked -= OnCreateTemplateButtonClicked;
            collectionCreateTemplateButton.clicked += OnCreateTemplateButtonClicked;

            var removeButton = collectionsGroup.Q<Button>("button-remove");
            removeButton.clicked -= OnRemoveButtonClicked;
            removeButton.clicked += OnRemoveButtonClicked;

            // Local handlers — closures capture latest state
            void OnViewButtonClicked() => ContextualActions.ViewInProjectView(sceneCollections.First());
            void OnCreateTemplateButtonClicked() => ContextualActions.CreateTemplate(sceneCollections.First());
            void OnRemoveButtonClicked() => ContextualActions.Remove(collections);

        }


        #endregion
        #region Scene

        GroupBox scenesGroup = null!;
        Label sceneCountLabel = null!;

        Button sceneBakeButton = null!;
        Button sceneMergeButton = null!;

        void SetupSceneUI()
        {

            scenesGroup = view.Q<GroupBox>("group-scenes");
            sceneCountLabel = scenesGroup.Q<Label>("count-scenes");

            sceneBakeButton = scenesGroup.Q<Button>("button-bake");
            sceneMergeButton = scenesGroup.Q<Button>("button-merge");

            sceneBakeButton.clicked += () => ContextualActions.BakeLightmaps(selectionService.actionableScenes);
            sceneMergeButton.clicked += () => ContextualActions.MergeScenes(selectionService.actionableScenes);
            scenesGroup.Q<Button>("button-remove").clicked += () => ContextualActions.Remove(selectionService.scenes);

        }

        void UpdateSceneUI()
        {

            scenesGroup.SetVisible(selectionService.scenes.Count() > 0);
            sceneCountLabel.text = selectionService.scenes.Count().ToString();

            var sceneCount = selectionService.actionableScenes.Count();
            sceneBakeButton.SetEnabled(sceneCount > 1);
            sceneMergeButton.SetEnabled(sceneCount > 1);

        }

        #endregion

    }

}
