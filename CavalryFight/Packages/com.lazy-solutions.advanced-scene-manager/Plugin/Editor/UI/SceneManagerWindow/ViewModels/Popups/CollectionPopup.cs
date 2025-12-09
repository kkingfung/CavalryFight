using AdvancedSceneManager.Editor.UI.Utility;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class CollectionPopup : PageStackPopup<CollectionPopup.SubPage, CollectionPopup.MainPage>
    {

        public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.collection.root;

        public abstract class SubPage : ViewModel
        {
            protected override void OnAdded() =>
                view.Bind(new(context.collection));
        }

        protected override void OnAdded()
        {

            if (!context.collection)
            {
                ASMWindow.ClosePopup();
                return;
            }

            base.OnAdded();

#if INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            view.Q("navigate-input-bindings").Show();
#endif

        }

        protected override void OnRemoved()
        {

            if (context.collection)
                context.collection.Save();
            else if (SceneManager.profile)
                SceneManager.profile.Save();

            base.OnRemoved();

        }

        #region Title

        #endregion
        #region Lock



        #endregion

    }

}
