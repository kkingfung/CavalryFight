using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Editor.UI.Views.Popups;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Services;
using UnityEditor;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views
{

    class FooterView : ViewModel
    {

        public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.main.footer;
        public override bool useTemplateContainer => false;

        [Inject] private readonly IProfileBindingsService profileBindingService;

#if ASM_CHILD_PROFILES
        [Inject] private readonly IChildProfilesService childProfilesService;
#endif

        protected override void OnAdded()
        {

            base.OnAdded();

            SetupPlayButton();
            SetupProfile();
            SetupCollectionButton();

            OnProfileChanged();
            RegisterEvent<ProfileChangedEvent>(e => OnProfileChanged());

        }

        void OnProfileChanged() =>
            view.Q<Button>("button-profile").text = SceneManager.profile ? SceneManager.profile.name : "none";

        void SetupPlayButton() =>
            view.Q<Button>("button-play").BindEnabled(SceneManager.settings.user, nameof(SceneManager.settings.user.activeProfile));

        void SetupProfile()
        {

            view.Q<Button>("button-profile").clicked += ASMWindow.OpenPopup<ProfilePopup>;

#if ASM_CHILD_PROFILES
            var childProfilesButton = view.Q<Button>("button-child-profiles");

            childProfilesButton.RegisterCallback<ClickEvent>(e =>
            {
                childProfilesService.ToggleExpanded();
                UpdateChildProfilesButton();
            });

            UpdateChildProfilesButton();
            void UpdateChildProfilesButton() =>
                childProfilesButton.text = childProfilesService.FooterButtonText();
#endif

        }

        void SetupCollectionButton()
        {

            var button = view.Q("split-button-add-collection");
            profileBindingService.BindEnabledToProfile(button);

            button.Q<Button>("button-add-collection-menu").clicked += ASMWindow.OpenPopup<CollectionTemplatesPopup>;
            button.Q<Button>("button-add-collection").clicked += () => SceneManager.profile.CreateCollection();

        }

        #region Extendable buttons

        [ASMWindowElement(ElementLocation.Header)]
        [ASMWindowElement(ElementLocation.Footer, isVisibleByDefault: true)]
        static VisualElement SceneHelperButton()
        {

            var button = new Button() { text = "", tooltip = "Scene helper (try drag me to a Unity UI Button click UnityEvent...)" };
            button.UseFontAwesome();

            button.RegisterCallback<PointerDownEvent>(e =>
            {

                if (e.button != 0)
                    return;

#if !UNITY_2023_1_OR_NEWER
                e.PreventDefault();
#endif
                e.StopPropagation();
                e.StopImmediatePropagation();

                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new[] { ASMSceneHelper.instance };
                DragAndDrop.StartDrag("Drag: Scene helper");

            }, TrickleDown.TrickleDown);

            return button;

        }

        #endregion

    }

}
