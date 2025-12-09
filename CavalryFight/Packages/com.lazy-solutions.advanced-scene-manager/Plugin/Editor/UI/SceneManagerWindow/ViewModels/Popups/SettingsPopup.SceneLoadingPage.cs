using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Utility.CrossSceneReferences;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class SettingsPopup
    {

        public class SceneLoadingPage : SubPage
        {

            public override string title => "Scene loading";
            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.settings.sceneLoading;

            protected override void OnAdded()
            {

                view.Q("picker-loading-scene").BindToProfile();
                view.Q("picker-fade-scene").BindToSettings();
                view.Q("section-profile").BindToProfile();
                view.Q("section-project-settings").BindToSettings();
                view.Q("group-references").BindToSettings();
                view.Q("group-event-methods").BindToSettings();

                SetupReferencesToggles();

            }

            void SetupReferencesToggles()
            {

                var crossRefToggle = view.Q<Toggle>("toggle-cross-scene-references");
                var guidReferenceToggle = view.Q("toggle-guid-references");

                crossRefToggle.RegisterValueChangedCallback(e =>
                {
                    UpdateGUIDToggle();
                    if (e.newValue)
                        SceneManager.settings.project.enableGUIDReferences = true;
                    CrossSceneReferenceUtility.Initialize();
                });

                UpdateGUIDToggle();
                void UpdateGUIDToggle() =>
                    guidReferenceToggle.SetEnabled(!SceneManager.settings.project.enableCrossSceneReferences);

                //UI Builder does not support newline in tooltips for some reason, and since this works, we're doing it here
                crossRefToggle.tooltip =
                    "[Experimental]\n" +
                    "Enables cross-scene references.\n\n" +
                    "Note that Unity does not fully support this, and you will receive warnings. It's not unlikely you'll experience jankiness and wrong behavior as well.\n\n" +
                    "This is due to Unity unintentionally blocking third-party implementations when warning people and making sure they don't do anything that is not supported by default, even if it sometimes might seem like it.";

            }

        }

    }

}
