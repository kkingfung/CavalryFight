using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views
{

    partial class WelcomeWizardView
    {

        public class GitPage : SubPage
        {

            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.welcomeWizard.git;

            readonly EventCallback<ClickEvent> clickEvent = new(OpenTrialDocs);

            public override string title => "Git Ignore & Licensing Reminder";

            protected override void OnAdded() =>
                view.Q<Label>("link-trial").RegisterCallback(clickEvent);

            protected override void OnRemoved() =>
                view.Q<Label>("link-trial").UnregisterCallback(clickEvent);

            static void OpenTrialDocs(ClickEvent e) =>
                Application.OpenURL("https://github.com/Lazy-Solutions/AdvancedSceneManager/blob/main/trial/README.md");

        }

    }

}

