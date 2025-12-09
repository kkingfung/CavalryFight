using AdvancedSceneManager.DependencyInjection;
using AdvancedSceneManager.Editor.UI.Views.Popups;
using AdvancedSceneManager.Services;
using System;

namespace AdvancedSceneManager.Editor.UI
{

    [RegisterService(typeof(DialogService))]
    interface IDialogService : DependencyInjectionUtility.IInjectable
    {
        bool IsDialogOpen { get; }

        void PromptConfirm(Action onConfirm, Action onCancel = null, string confirmText = "OK", string cancelText = "Cancel", string message = "Are you sure?", Action onSecondary = null, string secondaryText = null, Action onDismiss = null);
        void PromptName(Action<string> onContinue, string value = null, Action onCancel = null);
    }

    class DialogService : IDialogService
    {

        public bool IsDialogOpen => ASMWindow.IsPopupOpen<PickNamePopup>() || ASMWindow.IsPopupOpen<ConfirmPopup>();

        public void PromptName(Action<string> onContinue, string value = null, Action onCancel = null)
        {
            if (IsDialogOpen)
                throw new InvalidOperationException("Cannot display multiple prompts at a time.");

            ASMWindow.OpenPopup<PickNamePopup>(new ViewModelContext(customParam: new PickNamePopup.Params(onContinue, value ?? string.Empty, onCancel)));
        }

        public void PromptConfirm(Action onConfirm, Action onCancel = null, string confirmText = "OK", string cancelText = "Cancel", string message = "Are you sure?", Action onSecondary = null, string secondaryText = null, Action onDismiss = null)
        {
            if (IsDialogOpen)
                throw new InvalidOperationException("Cannot display multiple prompts at a time.");

            ASMWindow.OpenPopup<ConfirmPopup>(new ViewModelContext(customParam: new ConfirmPopup.Params(onConfirm, onCancel, confirmText, cancelText, message, onSecondary, secondaryText, onDismiss)));
        }
    }

}
