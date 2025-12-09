using AdvancedSceneManager.Editor.UI.Utility;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class ScenePopup : PageStackPopup<ScenePopup.SubPage, ScenePopup.MainPage>
    {

        public static class CustomParams
        {
            public const string isInspector = nameof(isInspector);
        }

        public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.scene.root;

        public abstract class SubPage : ViewModel, IPopup
        {

            public PageStackView stack => view.GetAncestor<PageStackView>();
            public override bool cacheAsSingleton => false;

            protected override void OnAdded() =>
                view.Bind(new(context.scene));
        }

        protected override void OnAdded()
        {

            if (!context.scene || (context.baseCollection == null && context.customParam is not CustomParams.isInspector))
            {
                Log.Error("Improper args for ScenePopup\nCollection: " + context.baseCollection + "\nScene: " + context.scene + "\nCustom param: " + context.customParam);
                ASMWindow.ClosePopup();
                return;
            }

            base.OnAdded();

        }

        protected override void OnRemoved()
        {
            context.scene.Save();
            base.OnRemoved();
        }

    }

}

