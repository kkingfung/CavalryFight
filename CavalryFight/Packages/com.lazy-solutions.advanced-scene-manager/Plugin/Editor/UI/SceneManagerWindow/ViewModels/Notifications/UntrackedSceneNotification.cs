using AdvancedSceneManager.Editor.UI.Views;
using AdvancedSceneManager.Editor.UI.Views.Popups;
using AdvancedSceneManager.Editor.Utility;
using AdvancedSceneManager.Models;
using System.Collections.Generic;

namespace AdvancedSceneManager.Editor.UI.Notifications
{

    class UntrackedSceneNotification : SceneNotification<Scene, UntrackedScenePopup>
    {

        public override NotificationImportance importance => NotificationImportance.Default;

        public override string GetNotificationText(int count) =>
            $"You have {count} imported scenes that are not tracked by ASM, click here to fix now...";

        public override IEnumerable<Scene> GetItems() =>
            SceneImportUtility.untrackedScenes;

    }

}
