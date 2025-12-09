using AdvancedSceneManager.Editor.Utility;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class SettingsPopup
    {

        public class AppearancePage : SubPage
        {

            public override string title => "Appearance";
            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.settings.appearance.root;
            public override BindTo bindTo => BindTo.UserSettings;

            protected override void OnAdded()
            {
                base.OnAdded();
                SetupToolbarButton();
            }

            void SetupToolbarButton()
            {

                var groupInstalled = view.Q("group-toolbar").Q("group-installed");
                var groupNotInstalled = view.Q("group-toolbar").Q("group-not-installed");

#if TOOLBAR_EXTENDER

                groupInstalled.SetVisible(true);
                groupNotInstalled.SetVisible(false);

                Setup(view.Q("slider-toolbar-button-offset"));
                Setup(view.Q("slider-toolbar-button-count"));

                static void Setup(VisualElement element)
                {
                    element.SetVisible(true);
                    element.Q("unity-drag-container").RegisterCallback<PointerMoveEvent>(e =>
                    {
                        if (e.pressedButtons == 1)
                            ToolbarButton.Repaint();
                    });
                }

#endif

            }

        }

    }

}
