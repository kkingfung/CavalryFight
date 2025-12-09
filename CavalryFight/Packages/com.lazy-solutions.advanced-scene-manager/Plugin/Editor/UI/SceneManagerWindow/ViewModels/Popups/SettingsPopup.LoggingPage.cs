using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class SettingsPopup
    {

        public class LoggingPage : SubPage
        {
            public override string title => "Logging";
            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.settings.editor.logging;
            public override BindTo bindTo => BindTo.UserSettings;
        }

    }

}
