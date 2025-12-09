using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.Services;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views
{

    class UndoView : ViewModel
    {

        public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.main.undo;

        [Inject] public IUndoService undoService { get; private set; } = null!;

        protected override void OnAdded()
        {
            RegisterEvent<UndoItemsChangedEvent>(e => Reload());
            EditorApplication.delayCall += Reload;
        }

        void Reload()
        {

            view.Clear();

            foreach (var collection in undoService.visibleItems.ToList())
                view.Add(undoService.GenerateView(collection));

        }

    }

}
