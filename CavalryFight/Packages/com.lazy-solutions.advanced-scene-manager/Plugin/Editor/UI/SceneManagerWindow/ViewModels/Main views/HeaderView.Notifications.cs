using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.Services;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views
{

    partial class HeaderView
    {

        VisualElement notificationContainer = null!;
        VisualElement notificationBubble = null!;
        Label notificationLabel = null!;
        Button notificationButton = null!;

        [Inject] public INotificationService notificationService { get; private set; } = null!;
        [Inject] public IUndoService undoService { get; private set; } = null!;

        void OnAdded_Notifications()
        {

            notificationContainer = view.Q("notifications");
            notificationLabel = view.Q<Label>("label-notifications");
            notificationBubble = view.Q("bubble-notifications");
            notificationButton = view.Q<Button>("button-notifications");

            notificationButton.RegisterCallback<ClickEvent>(e => ShowNotificationMenu());

            Refresh();
            RegisterEvent<NotificationsChangedEvent>(e => Refresh());
            RegisterEvent<UndoItemsChangedEvent>(e => Refresh());

        }

        void Refresh()
        {

            var count = notificationService.overflowItems.Count() + undoService.overflowItems.Count();
            var actualCount = count + notificationService.mutedItems.Count();

            notificationLabel.text = count.ToString();
            notificationBubble.SetVisible(count > 0);
            notificationContainer.style.opacity = count > 0 ? 1 : 0.4f;
            notificationContainer.SetEnabled(actualCount > 0);

        }

        void ShowNotificationMenu()
        {
            var button = view.Q<Button>("button-notifications");

            UnityEditor.PopupWindow.Show(button.worldBound, new PopupContent(notificationService, undoService, button));
        }

        class PopupContent : PopupWindowContent
        {

            readonly INotificationService notificationService;
            readonly IUndoService undoService;
            readonly Button button;

            public PopupContent(INotificationService notificationService, IUndoService undoService, Button button)
            {
                this.notificationService = notificationService;
                this.undoService = undoService;
                this.button = button;
            }

            public override void OnOpen()
            {

                button.AddToClassList("open");
                undoService.HoldOverflow();

                SceneManager.events.RegisterCallback<UndoItemsChangedEvent>(UndoItemsChanged);
                SceneManager.events.RegisterCallback<NotificationsChangedEvent>(NotificationsChanged);

            }

            public override void OnClose()
            {

                button.RemoveFromClassList("open");
                undoService.ReleaseOverflow();

                SceneManager.events.UnregisterCallback<UndoItemsChangedEvent>(UndoItemsChanged);
                SceneManager.events.UnregisterCallback<NotificationsChangedEvent>(NotificationsChanged);

            }

            void UndoItemsChanged(UndoItemsChangedEvent e)
            {
                Reload();
            }

            void NotificationsChanged(NotificationsChangedEvent e)
            {
                Reload();
            }

            ScrollView list = null!;
            public override VisualElement CreateGUI()
            {

                var container = new VisualElement();
                container.style.paddingTop = 8;
                container.style.paddingBottom = 8;
                container.style.paddingLeft = 8;
                container.style.paddingRight = 8;
                container.style.width = 400;

                container.AddToClassList("primary-background-color");

                foreach (var style in SceneManagerWindow.window!.viewLocator.styles.Enumerate())
                    container.styleSheets.Add(style);

                container.RegisterCallback<ClickEvent>(e =>
                {
                    if (e.target is Button button)
                        editorWindow.Close();
                });

                list = new ScrollView();
                list.style.maxHeight = 600;
                container.Add(list);

                Reload();

                return container;

            }

            void Reload()
            {

                list.Clear();

                var undoItems = undoService.overflowItems.ToList();
                var notifications = notificationService.overflowItems.ToList();
                var mutedNotifications = notificationService.mutedItems.ToList();

                if (undoItems.Count == 0 && notifications.Count == 0 && mutedNotifications.Count == 0)
                    EditorApplication.delayCall += () =>
                    {
                        if (editorWindow && editorWindow.hasFocus)
                            editorWindow.Close();
                    };

                foreach (var undoItem in undoItems)
                    list.Add(undoService.GenerateView(undoItem));

                if (undoItems.Count > 0 && notifications.Count > 0)
                    list.Add(Separator());

                foreach (var notification in notifications)
                    list.Add(notificationService.GenerateView(notification));

                if ((undoItems.Count > 0 || notifications.Count > 0) && mutedNotifications.Count > 0)
                    list.Add(Separator());

                foreach (var notification in mutedNotifications)
                    list.Add(notificationService.GenerateView(notification));

            }

            VisualElement Separator()
            {
                var line = new VisualElement() { name = "separator" };
                line.style.height = 1;
                line.style.marginTop = 12;
                line.style.marginBottom = 12;
                line.style.backgroundColor = ColorUtility.TryParseHtmlString("#64646497", out var c) ? c : Color.white;
                line.style.width = 120;
                line.style.alignSelf = Align.Center;
                return line;
            }

        }

    }

}
