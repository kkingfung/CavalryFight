using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.Editor.UI.Views;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Services;
using AdvancedSceneManager.Utility;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdvancedSceneManager.Editor.UI.Notifications
{

    class NotifyProfileNotification : ViewModel, IPersistentNotification
    {

        public NotificationImportance importance => NotificationImportance.Default;

        const string notificationID = nameof(NotifyProfileNotification);

        List<string> dismissedProfiles = new();

        [Inject] public INotificationService notificationService { get; private set; } = null!;

        protected override void OnAdded()
        {
            dismissedProfiles = sessionState.GetValue(dismissedProfiles, nameof(dismissedProfiles));

            RegisterEvent<ProfileAddedEvent>(e => ReloadNotification());
            RegisterEvent<ProfileAddedEvent>(e => dismissedProfiles.Remove(e.profile.id));
        }

        protected override void OnRemoved()
        {
            sessionState.SetValue(dismissedProfiles, nameof(dismissedProfiles));
        }

        public void ReloadNotification()
        {
            foreach (var profile in SceneManager.assets.profiles)
            {

                ClearNotification(profile);

                if (profile.notify && SceneManager.profile != profile && !dismissedProfiles.Contains(profile.id))
                    DisplayNotification(profile);

            }
        }

        void ClearNotification(Profile profile) =>
            notificationService.ClearNotification($"{notificationID}+{profile.id}");

        void DisplayNotification(Profile profile)
        {

            ClearNotification(profile);

            notificationService.Notify(new(
                message: !string.IsNullOrEmpty(profile.notifyMessage) ? profile.notifyMessage : $"The profile '{profile.name}' is available to try out, click here to do so now.",
                onClick: ActivateProfile,
                id: $"{notificationID}+{profile.id}")
            {
                dismissOnClick = true,
                onDismiss = DismissProfile,
                kind = NotificationKind.Profile,
                importance = importance
            });

            void DismissProfile()
            {
                dismissedProfiles.Add(profile.id);
                ReloadNotification();
            }

            async void ActivateProfile()
            {

                dismissedProfiles.Add(profile.id);
                ReloadNotification();

                ((SceneManagerWindow)window).mainView.Show<ProgressSpinnerView>();

                ASMWindow.ClosePopup();
                await Task.Delay(250);
                ProfileUtility.SetProfile(profile);

                ((SceneManagerWindow)window).mainView.Hide<ProgressSpinnerView>();

            }

        }

    }

}
