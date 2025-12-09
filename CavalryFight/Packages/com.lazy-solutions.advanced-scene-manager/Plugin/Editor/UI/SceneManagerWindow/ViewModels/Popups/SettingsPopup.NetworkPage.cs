using AdvancedSceneManager.Editor.UI.Utility;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class SettingsPopup
    {

        public class NetworkPage : SubPage
        {

            public override string title => "Network";
            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.settings.network;
            public override BindTo bindTo => BindTo.ProjectSettings;

            protected override void OnAdded()
            {
                base.OnAdded();
                view.Q("toggle-sync-indicator").BindToUserSettings();
            }

        }

    }

}
