using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.Services;
using UnityEditor;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views
{

    class NotificationView : ViewModel
    {

        public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.main.notification;
        public override bool useTemplateContainer => false;

        [Inject] public INotificationService notificationService { get; private set; } = null!;

        protected override void OnAdded()
        {
            RegisterEvent<NotificationsChangedEvent>(e => Reload());
            EditorApplication.delayCall += Reload;
        }

        void Reload()
        {

            view.Clear();

            foreach (var notification in notificationService.visibleItems)
            {
                var view = notificationService.GenerateView(notification);
                this.view.Add(view);
            }

        }

    }

}
