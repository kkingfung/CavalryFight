using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class DiagPopup : PageStackPopup<DiagPopup.SubPage, DiagPopup.MainPage>
    {

        public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.diag.root;

        public abstract class SubPage : ViewModel
        { }

    }

}
