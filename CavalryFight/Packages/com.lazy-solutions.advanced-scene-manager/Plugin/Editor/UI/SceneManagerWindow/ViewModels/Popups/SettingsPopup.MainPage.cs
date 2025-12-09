using AdvancedSceneManager.Utility.Discoverability;
using System.Linq;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    partial class SettingsPopup
    {

        public class MainPage : SubPage
        {

            public override string title => "Settings";
            public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.settings.main;

            protected override void OnAdded()
            {

                var netcodeButton = view.Q<Button>("navigate-network");
                netcodeButton.SetVisible(false);

#if NETCODE
                netcodeButton.SetVisible(true);
#endif

                var extensionPages = DiscoverabilityUtility.GetMembers<ASMWindowElementAttribute>().
                    Where(discoverable => ((ASMWindowElementAttribute)discoverable.attribute).location == ElementLocation.Settings);
                view.Q("navigate-extensions").SetVisible(extensionPages.Any());

            }

        }

    }

}
