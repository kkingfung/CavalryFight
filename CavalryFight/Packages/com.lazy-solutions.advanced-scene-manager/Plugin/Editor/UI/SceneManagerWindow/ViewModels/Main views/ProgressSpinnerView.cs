using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Utility;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views
{

    class ProgressSpinnerView : ViewModel
    {

        public static class Params
        {
            public static object NoFadeInAnimation = new();
        }

        public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.main.progressSpinner;
        public override bool useTemplateContainer => false;

        IVisualElementScheduledItem rotateAnimation;
        VisualElement spinner;

        protected override void OnAdded()
        {

            DisableTemplateContainer();
            spinner = view.Q("progress-spinner");

            view.SetVisible(true);
            view.style.opacity = 0;
            rotateAnimation = spinner.Rotate();

            if (context.customParam == Params.NoFadeInAnimation)
                view.style.opacity = 1;
            else
                view.Fade(1);

        }

        protected override async Task OnRemovedAsync()
        {
            await view.Fade(0);
        }

    }

}
