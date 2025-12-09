using AdvancedSceneManager.Editor.UI.Utility;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI.Views.Popups
{

    public interface IPageStackPopup
    {

    }

    abstract class PageStackPopup<T, TMainPage> : ViewModel, IPopup, IPageStackPopup where TMainPage : T where T : ViewModel
    {

        public virtual bool persistHistory { get; } = true;

        string history
        {
            get => sessionState.GetProperty("");
            set => sessionState.SetProperty(value);
        }

        public virtual PageStackView stack => view?.Q<PageStackView>() ?? view?.GetAncestor<PageStackView>();

        public bool isMainPage => stack?.current is TMainPage;

        protected override void OnAdded()
        {

            if (stack is null)
            {
                Log.Error("Could not find PageStackView");
                return;
            }

            if (!persistHistory)
                stack.Push<TMainPage>();
            else
            {

                stack.RegisterHistoryChangedEvent(e => this.history = JsonUtility.ToJson(e.history));
                stack.parentContext = context;

                var history = this.history;

                if (string.IsNullOrEmpty(history))
                    stack.Push<TMainPage>();
                else
                    stack.RestoreHistory(history!);

            }

        }

        protected override void OnRemoved()
        {
            history = null;
        }

        protected void Push<TPopup>(bool animate = true) where TPopup : T
        {
            stack?.Push<TPopup>(animate);
        }

        protected void Push(Type type, bool animate = true)
        {
            stack?.Push(type, animate);
        }

        public void Insert<TPage>(int index) where TPage : T
        {
            stack?.Insert<TPage>(index);
        }

    }

}
