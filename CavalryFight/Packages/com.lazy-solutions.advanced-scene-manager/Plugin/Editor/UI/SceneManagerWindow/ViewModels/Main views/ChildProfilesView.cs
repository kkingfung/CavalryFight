#if ASM_CHILD_PROFILES

using AdvancedSceneManager.Editor.UI.Utility;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Services;
using System.Linq;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views
{

    interface IChildProfilesService
    {
        void ToggleExpanded();
        string FooterButtonText();
        void Reload();
    }

    [RegisterService(typeof(IChildProfilesService))]
    class ChildProfilesService : IChildProfilesService
    {

        ChildProfilesView view => SceneManagerWindow.window ? SceneManagerWindow.window.mainView.GetViewModel<ChildProfilesView>() : null;

        public void ToggleExpanded()
        {
            view?.ToggleExpanded();
        }

        public string FooterButtonText()
        {
            return view?.FooterButtonText();
        }

        public void Reload()
        {
            view?.Reload();
        }

    }

    sealed class ChildProfilesView : ViewModel
    {

        public override VisualTreeAsset template => ViewLocator.instance.main.childProfiles;
        public override bool useTemplateContainer => false;

        bool isExpanded
        {
            get => sessionState.GetProperty(false);
            set => sessionState.SetProperty(value);
        }

        protected override void OnAdded()
        {
            view.SetVisible(isExpanded);
            Reload();
            view.Q<ASMListView>().RegisterContextMenu((e, pos, obj) =>
            {
                e.AddItem("Remove", false, () => Remove((Profile)obj));
            });
        }

        void Remove(Profile profile)
        {
            SceneManager.profile.RemoveChildProfile(profile);
            Reload();
        }

        public void Reload()
        {
            view.Q<ASMListView>().itemsSource = SceneManager.profile.childProfiles.ToList();
        }

        public void ToggleExpanded()
        {
            isExpanded = !isExpanded;
            view.SetVisible(isExpanded);
        }

        public string FooterButtonText()
        {
            return isExpanded ? "\uf0d8" : "\uf0d7";
        }

    }

}
#endif
