using System;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    class ConfirmPopup : ViewModel, IPopup
    {

        public record Params(Action onConfirm, Action onCancel = null, string confirmText = "OK", string cancelText = "Cancel", string message = "Are you sure?", Action onSecondary = null, string secondaryText = null, Action onDismiss = null);

        public override VisualTreeAsset template => ((SceneManagerWindow)window).viewLocator.popups.confirm;
        public override bool remainOpenAsPopupAfterDomainReload => false;

        bool isOpen => ASMWindow.IsPopupOpen<ConfirmPopup>();

        bool didPressButton;

        Params promptContext;

        protected override void OnAdded()
        {

            promptContext = context.OfType<Params>() ?? throw new ArgumentException("Cannot display multiple prompts at a time.");

            var confirmButton = view.Q<Button>("button-confirm");
            var secondaryButton = view.Q<Button>("button-secondary");
            var cancelButton = view.Q<Button>("button-cancel");

            confirmButton.text = promptContext.confirmText;
            secondaryButton.text = promptContext.secondaryText;
            cancelButton.text = promptContext.cancelText;

            cancelButton.clicked += () => { didPressButton = true; promptContext?.onCancel?.Invoke(); };
            secondaryButton.clicked += () => { didPressButton = true; promptContext?.onSecondary?.Invoke(); };
            confirmButton.clicked += () => { didPressButton = true; promptContext.onConfirm(); };

            if (string.IsNullOrEmpty(promptContext.secondaryText) || promptContext.onSecondary is null)
                secondaryButton.Hide();

            view.Q<Label>("label-message").text = promptContext.message;

        }

        protected override void OnRemoved()
        {

            if (didPressButton)
                return;

            promptContext?.onDismiss?.Invoke();

        }

    }

}
