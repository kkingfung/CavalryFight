using AdvancedSceneManager.Editor.UI.Views.Popups;
using AdvancedSceneManager.Utility;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views
{

    partial class WelcomeWizardView : PageStackPopup<WelcomeWizardView.SubPage, WelcomeWizardView.MainPage>
    {

        public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.welcomeWizard.root;

        public abstract class SubPage : ViewModel
        {

            public override string title => "";

            public virtual VisualElement CreateFooterGUI()
            {
                return null;
            }

        }

        SubPage currentPage = null;
        VisualElement footer = null;
        protected override void OnAdded()
        {

            DisableTemplateContainer();
            stack!.header.displayCloseButton = false;
            SetupContinueButton();

            base.OnAdded();

            stack.RegisterHistoryChangedEvent(e =>
            {
                currentPage = (SubPage)stack.current!;
                view.Q<Button>("button-continue").text = currentPage is EndPage ? "Finish" : "Continue";

                UpdateFooter();
            });

            currentPage = (SubPage)stack.current!;
            UpdateFooter();

        }

        void UpdateFooter()
        {
            footer?.RemoveFromHierarchy();
            footer = currentPage?.CreateFooterGUI();
            if (footer is not null)
                view.Q("footer").Add(footer);
        }

        void SetupContinueButton()
        {
            var button = view.Q("button-continue");
            button.RegisterCallback<ClickEvent>(async e =>
            {


                if (stack!.isNavigating)
                    return;

                if (stack.current is MainPage)
                    stack.Push<GitPage>();

                else if (stack.current is GitPage)
                    stack.Push<DependenciesPage>();

                else if (stack.current is DependenciesPage)
                    stack.Push<SceneImportPage>();

                else if (stack.current is SceneImportPage)
                    stack.Push<ProfileSelectorPage>();

                else if (stack.current is ProfileSelectorPage)
                    stack.Push<EndPage>();

                else if (stack.current is EndPage)
                {

                    ((SceneManagerWindow)window).mainView.Show<ProgressSpinnerView>(new ViewModelContext(customParam: ProgressSpinnerView.Params.NoFadeInAnimation));

                    ProfileUtility.SetProfile(ProfileSelectorPage.profileToActivate);
                    SceneManager.settings.user.hasCompletedWelcomeWizard = true;
                    SceneManager.settings.user.Save();

                    ((SceneManagerWindow)window).mainView.Hide<WelcomeWizardView>();

                    await Task.Delay(250);
                    ((SceneManagerWindow)window).mainView.Hide<ProgressSpinnerView>();

                }

            });
        }

    }

}

