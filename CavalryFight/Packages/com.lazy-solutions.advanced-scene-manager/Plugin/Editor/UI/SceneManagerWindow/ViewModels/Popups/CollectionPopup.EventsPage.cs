using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class CollectionPopup
    {

        public class EventsPage : SubPage
        {

            public override string title => context.collection.title + " - Events";
            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.collection.events;

            protected override void OnAdded()
            {

                base.OnAdded();

                //We have custom styling which will make elements expand outside normal PropertyField height
                view.Query<ListView>().ForEach(element => element.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight);

            }

        }

    }

}
