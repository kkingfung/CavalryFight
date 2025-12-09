using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Editor.UI.Views.Popups;
using AdvancedSceneManager.Services;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views
{

    partial class HeaderView : ViewModel
    {

        public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.main.header;
        public override bool useTemplateContainer => false;

        [Inject] public readonly IProfileBindingsService profileBindingService = null!;

        protected override void OnAdded()
        {

            base.OnAdded();

            SetupPlayButton();
            SetupSettingsButton();

            view.Q<Button>("button-menu").clicked += ASMWindow.OpenPopup<MenuPopup>;

            OnAdded_DiagInfo();
            OnAdded_Notifications();
            OnAdded_DevMenu();

        }

        protected override void OnRemoved()
        {
            UnsetupProgressListener();
        }

        Button buttonPlay = null!;
        void SetupPlayButton()
        {

            buttonPlay = view.Q<Button>("button-play");
            buttonPlay.clickable.activators.Add(new() { button = MouseButton.LeftMouse, modifiers = UnityEngine.EventModifiers.Shift });
            view.Q<Button>("button-play").clickable.clickedWithEventInfo += (e) =>
                SceneManager.app.Restart(new() { forceOpenAllScenesOnCollection = e.IsShiftKeyHeld() || e.IsCommandKeyHeld() });

            profileBindingService.BindEnabledToProfile(buttonPlay);

        }

        void SetupSettingsButton()
        {

            var button = view.Q<Button>("button-settings");
            button.RegisterCallback<ClickEvent>(e => ASMWindow.OpenSettings());
            profileBindingService.BindEnabledToProfile(button);

        }

    }

}
