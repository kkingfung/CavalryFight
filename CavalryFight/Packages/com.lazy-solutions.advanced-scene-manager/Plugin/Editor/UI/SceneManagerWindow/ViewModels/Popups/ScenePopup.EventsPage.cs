using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class ScenePopup
    {

        public class EventsPage : SubPage
        {

            public override string title => $"{context.scene.name} - Events";
            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.scene.events;

            protected override void OnAdded()
            {

                base.OnAdded();

                //We have custom styling which will make elements expand outside normal PropertyField height
                view.Query<ListView>().ForEach(element => element.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight);

            }

        }

    }
}

