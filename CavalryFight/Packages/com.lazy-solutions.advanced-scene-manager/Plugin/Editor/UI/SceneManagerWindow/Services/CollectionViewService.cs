using AdvancedSceneManager.DependencyInjection;
using AdvancedSceneManager.Editor.UI.Views;
using AdvancedSceneManager.Services;

namespace AdvancedSceneManager.Editor.UI
{

    [RegisterService(typeof(CollectionViewService))]
    interface ICollectionViewService : DependencyInjectionUtility.IInjectable
    {
        void Reload();
    }

    class CollectionViewService : ICollectionViewService
    {
        public void Reload()
        {
            SceneManagerWindow.window!.mainView.GetViewModel<CollectionView>()?.Reload();
        }
    }

}
