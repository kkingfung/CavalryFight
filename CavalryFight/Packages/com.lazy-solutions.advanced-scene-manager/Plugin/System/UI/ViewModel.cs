#if UNITY_EDITOR

using AdvancedSceneManager.Models;
using AdvancedSceneManager.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    /// <summary>Identifies a view model as a settings page in the ASM window.</summary>
    public interface ISettingsPage
    { }

    /// <summary>Identifies a view model as a popup in the ASM window.</summary>
    public interface IPopup
    { }

    /// <summary>Serializable data for view model state persistence.</summary>
    [Serializable]
    public struct SerializableViewModelData
    {

        /// <summary>The type name of the view model.</summary>
        public string typeName;

        /// <summary>The scroll position of the view.</summary>
        public float scrollPos;

        /// <summary>The ID of the associated collection.</summary>
        public string collectionID;

        /// <summary>The ID of the associated scene.</summary>
        public string sceneID;

        /// <summary>The index of the scene within its collection.</summary>
        public int? sceneIndex;

    }

    /// <summary>Defines a view model for the ASM window.</summary>
    /// <remarks>Only available in the editor.</remarks>
    public abstract class ViewModel : Service_ViewModelBase
    {

        internal static EditorWindow m_window = null!;

        /// <summary>Gets the ASM window.</summary>
        public EditorWindow window => m_window;

        /// <summary>Gets the root visual element of the ASM window.</summary>
        public VisualElement rootVisualElement => window.rootVisualElement;

        #region Generate ui

        /// <summary>When hosted in a PageStackView, this callback can be used to put content in the header.</summary>
        public virtual VisualElement CreateHeaderGUI() => null;

        /// <summary>Can be used to create gui.</summary>
        /// <remarks>Optional, <see cref="template"/> or <see cref="templatePath"/> can otherwise be used.</remarks>
        public virtual VisualElement CreateGUI() =>
            template
            ? (useTemplateContainer ? template!.CloneTree() : template!.Clone())
            : null;

        /// <summary>Gets or sets the path to the UXML template for this view model.</summary>
        public virtual string templatePath { get; private set; }

        /// <summary>Gets the UXML template asset for this view model.</summary>
        public virtual VisualTreeAsset template => !string.IsNullOrEmpty(templatePath) ? AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(templatePath) : null;

        /// <summary>If being wrapped in a <see cref="TemplateContainer"/> is an issue, set this to false to disable it.</summary>
        public virtual bool useTemplateContainer => true;

        /// <summary>Disables the template container wrapper for this view model.</summary>
        public void DisableTemplateContainer()
        {
            if (view is TemplateContainer container)
            {

                view = container.ElementAt(0);

                var parent = container.parent;
                var index = parent.IndexOf(container);
                container.RemoveFromHierarchy();
                view.RemoveFromHierarchy();

                if (string.IsNullOrEmpty(view.name))
                    view.name = GetType().Name;

                parent.Insert(index, view);

                //Log.Info("Disabled TemplateContainer: " + GetType().FullName);

            }
        }

        #endregion

        /// <summary>When hosted in a PageStackView (settings page layout), should this view be wrapped in a scroll view?</summary>
        public virtual bool useScrollView => true;

        /// <summary>Specifies title when hosted as a popup, or button text for settings category.</summary>
        public virtual string title { get; }

        /// <summary>When hosted as a popup, should this view be re-opened after a domain reload?</summary>
        public virtual bool remainOpenAsPopupAfterDomainReload { get; } = true;

        /// <summary>Specifies icon to use for settings category button.</summary>
        public virtual string settingsCategoryIcon { get; private set; }

        /// <summary>Gets or sets the context for this view model.</summary>
        public ViewModelContext context { get; protected set; }

        /// <summary>Gets the visual element for this view model.</summary>
        public VisualElement view { get; private set; } = null!;

        /// <summary>Gets the header element for this view model, assuming <see cref="CreateHeaderGUI"/> is overriden.</summary>
        public VisualElement headerView { get; internal set; } = null!;

        /// <summary>Gets the prior pages for this view model, used for back navigation in <b>PageStackView</b>.</summary>
        internal virtual Type[] priorPages { get; private set; }

        /// <summary>Gets whether this view model has been added to the UI.</summary>
        public bool isAdded { get; private set; }

        /// <summary>Called when the view model is added to the UI.</summary>
        protected virtual void OnAdded()
        { }

        /// <summary>Called when the view model is removed from the UI.</summary>
        protected virtual void OnRemoved()
        { }

        /// <summary>Callback for when animation finished, if hosted in a container that does animate it, like popups.</summary>
        public virtual void OnAddAnimationComplete() { }

        /// <summary>Called when the view model is removed from the UI.</summary>
        protected virtual Task OnRemovedAsync() => Task.CompletedTask;

        /// <summary>Gets whatever we should cache this view model. <see langword="true"/> by default, disable if you're having issues.</summary>
        public virtual bool cacheAsSingleton => true;

        #region Instance instantiation

        [HideInCallstack]
        internal virtual void Add(VisualElement view, ViewModelContext? context = null, bool ignoreAddedCheck = false)
        {

            if (isAdded && !ignoreAddedCheck)
                return;

            //Log.Info("Add: " + GetType().FullName);

            this.InvokeView(() =>
            {

                if (view is not null)
                    view.name = GetType().Name;

                ServiceUtility.Resolve(this);

                this.view = view!;
                this.context = context ?? default;
                isAdded = true;
                OnAdded();

            });

        }

        [HideInCallstack]
        internal virtual async Task Remove(bool ignoreAddedCheck = false, bool ignoreView = false)
        {

            if (!isAdded && !ignoreAddedCheck)
                return;

            //Log.Info("Remove: " + GetType().FullName);

            await this.InvokeView(async () =>
             {
                 OnRemoved();
                 await OnRemovedAsync();
                 ClearEventCallbacks();

                 if (!ignoreView)
                 {
                     view?.RemoveFromHierarchy();
                     view = null!;
                 }

                 isAdded = false;
                 context = default;
             });

        }

        #endregion
        #region View model instantiation

        private static readonly Dictionary<Type, ViewModel> viewModels = new();

        private static ViewModel GetInstance(Type type)
        {
            if (!viewModels.TryGetValue(type, out var vm))
            {
                vm = (ViewModel)Activator.CreateInstance(type)!;
                if (vm.cacheAsSingleton)
                    viewModels[type] = vm;
            }
            return vm;
        }

        /// <summary>Instantiates a view model of the specified type.</summary>
        /// <typeparam name="T">The type of view model to instantiate.</typeparam>
        /// <returns>The instantiated view model.</returns>
        public static T Instantiate<T>() where T : ViewModel =>
            (T)Instantiate(typeof(T))!;

        /// <summary>Attempts to instantiate a view model of the specified type.</summary>
        /// <typeparam name="T">The type of view model to instantiate.</typeparam>
        /// <param name="viewModel">The instantiated view model, if successful.</param>
        /// <returns>True if instantiation was successful, false otherwise.</returns>
        public static bool Instantiate<T>(out T viewModel) where T : ViewModel
        {

            if (TryInstantiate(typeof(T), out var baseViewModel))
            {
                viewModel = (T)baseViewModel!;
                return true;
            }

            viewModel = null;
            return false;

        }

        /// <summary>Attempts to instantiate a view model and create its GUI.</summary>
        /// <typeparam name="T">The type of view model to instantiate.</typeparam>
        /// <param name="viewModel">The instantiated view model, if successful.</param>
        /// <param name="view">The created visual element, if successful.</param>
        /// <returns>True if both instantiation and GUI creation were successful, false otherwise.</returns>
        public static bool Instantiate<T>([NotNullWhen(true)] out T viewModel, [NotNullWhen(true)] out VisualElement view) where T : ViewModel, new()
        {
            viewModel = (T)GetInstance(typeof(T));
            view = viewModel.CreateGUI();
            return view is not null;
        }

        /// <summary>Attempts to instantiate a view model by type and create its GUI.</summary>
        /// <param name="type">The type of view model to instantiate.</param>
        /// <param name="viewModel">The instantiated view model, if successful.</param>
        /// <param name="view">The created visual element, if successful.</param>
        /// <returns>True if both instantiation and GUI creation were successful, false otherwise.</returns>
        public static bool Instantiate(Type type, [NotNullWhen(true)] out ViewModel viewModel, [NotNullWhen(true)] out VisualElement view)
        {

            if (TryInstantiate(type, out viewModel))
            {
                view = viewModel!.CreateGUI();
                return view is not null;
            }

            view = null;
            return false;

        }

        /// <summary>Attempts to instantiate a view model by type.</summary>
        /// <param name="type">The type of view model to instantiate.</param>
        /// <param name="viewModel">The instantiated view model, if successful.</param>
        /// <returns>True if instantiation was successful, false otherwise.</returns>
        public static bool Instantiate(Type type, [NotNullWhen(true)] out ViewModel viewModel) =>
            TryInstantiate(type, out viewModel) && viewModel is not null;

        /// <summary>Instantiates a view model by type.</summary>
        /// <param name="type">The type of view model to instantiate.</param>
        /// <returns>The instantiated view model.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the type is not a valid view model.</exception>
        public static ViewModel Instantiate(Type type) =>
            TryInstantiate(type, out var viewModel)
            ? viewModel
            : throw new InvalidOperationException($"{type?.FullName ?? "null"} is not a valid view model.");

        private static bool TryInstantiate(Type type, [NotNullWhen(true)] out ViewModel viewModel)
        {

            if (type is null || !type.IsViewModel())
            {
                viewModel = null;
                return false;
            }

            viewModel = GetInstance(type);
            return true;

        }

        #endregion
        #region Serialization

        /// <summary>Gets or sets the persisted scroll position for this view model.</summary>
        internal float? scrollPos { get; set; }

        /// <summary>Serializes a view model to data for persistence.</summary>
        /// <param name="viewModel">The view model to serialize.</param>
        /// <returns>The serialized data.</returns>
        public static SerializableViewModelData Serialize(ViewModel viewModel) =>
            new()
            {
                typeName = viewModel.GetType().AssemblyQualifiedName,
                scrollPos = viewModel.scrollPos ?? 0,
                collectionID = viewModel.context.baseCollection?.id,
                sceneID = viewModel.context.scene ? viewModel.context.scene.id : null,
                sceneIndex = viewModel.context.sceneIndex
            };

        /// <summary>Attempts to deserialize a view model from data.</summary>
        /// <param name="data">The serialized data.</param>
        /// <param name="viewModel">The deserialized view model, if successful.</param>
        /// <returns>True if deserialization was successful, false otherwise.</returns>
        public static bool TryDeserialize(SerializableViewModelData data, [NotNullWhen(true)] out ViewModel viewModel) =>
           (viewModel = Deserialize(data)) is not null;

        /// <summary>Deserializes a view model from data.</summary>
        /// <param name="data">The serialized data.</param>
        /// <returns>The deserialized view model, or null if deserialization failed.</returns>
        public static ViewModel Deserialize(SerializableViewModelData data)
        {

            if (string.IsNullOrEmpty(data.typeName))
                return null;

            var type = Type.GetType(data.typeName ?? string.Empty, throwOnError: false);
            if (type is null || !type.IsViewModel())
                return null;

            var viewModel = Instantiate(type);
            viewModel.scrollPos = data.scrollPos;
            viewModel.context = new(
                collection: SceneCollection.FindCollectionAll(data.collectionID, activeProfile: false),
                scene: Scene.Find(data.sceneID),
                sceneIndex: data.sceneIndex);

            return viewModel;

        }

        #endregion

        /// <summary>Gets the service of the specified type.</summary>
        /// <typeparam name="T">The type of service to get.</typeparam>
        /// <returns>The service of the specified type.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the service is not found.</exception>
        static protected T GetService<T>() =>
              ServiceUtility.Get<T>()
              ?? throw new InvalidOperationException($"Could not retrieve service '{typeof(T).Name}'.");

    }

}
#endif
