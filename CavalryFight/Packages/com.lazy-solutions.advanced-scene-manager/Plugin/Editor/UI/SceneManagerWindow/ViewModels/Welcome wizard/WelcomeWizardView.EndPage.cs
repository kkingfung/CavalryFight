using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views
{

    partial class WelcomeWizardView
    {

        public class EndPage : SubPage
        {

            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.welcomeWizard.end;

            readonly EventCallback<ClickEvent> clickEvent = new(OpenDocs);

            protected override void OnAdded() =>
                view.Q<Label>("link").RegisterCallback(clickEvent);

            protected override void OnRemoved() =>
                view.Q<Label>("link").UnregisterCallback(clickEvent);

            static void OpenDocs(ClickEvent e) =>
                Application.OpenURL("https://github.com/Lazy-Solutions/AdvancedSceneManager/blob/main/docs/README.md");

        }

    }

}
