using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views
{

    partial class WelcomeWizardView
    {

        public class MainPage : SubPage
        {
            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.welcomeWizard.main;
        }

    }

}

