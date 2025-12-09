using AdvancedSceneManager.Editor.UI.Utility;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class SettingsPopup
    {

        public class StartupPage : SubPage
        {

            public override string title => "Startup";
            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.settings.startup;
            public override BindTo bindTo => BindTo.Profile;

            protected override void OnAdded()
            {
                base.OnAdded();
                view.Q<Toggle>("toggle-splash-display-in-editor").BindToUserSettings();
            }

        }

    }

}
