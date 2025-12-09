using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class SettingsPopup
    {

        public class ExperimentalPage : SubPage
        {
            public override string title => "Experimental";
            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.settings.experimental;
            public override BindTo bindTo => BindTo.ProjectSettings;
        }

    }

}
