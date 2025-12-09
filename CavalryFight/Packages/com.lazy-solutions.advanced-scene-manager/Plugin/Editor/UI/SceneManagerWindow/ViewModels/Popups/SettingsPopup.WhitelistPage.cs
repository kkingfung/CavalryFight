using AdvancedSceneManager.Editor.UI.Utility;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class SettingsPopup
    {

        public class WhitelistPage : BlacklistPage
        {

            public override string title => "Whitelisted scenes";
            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.settings.assets.whitelist;

            protected override void OnAdded()
            {
                view.BindToSettings();
                SetupBlocklist(SceneManager.settings.project.m_whitelist);
            }

        }

    }

}
