using AdvancedSceneManager.Callbacks.Events.Editor;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Interfaces;
using AdvancedSceneManager.Services;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSceneManager.Editor.UI
{

    [RegisterService(typeof(UndoService))]
    interface IUndoService
    {
        IEnumerable<ISceneCollection> visibleItems { get; }
        IEnumerable<ISceneCollection> overflowItems { get; }
        bool preventCountdownFromFinishing { get; set; }

        VisualElement GenerateView(ISceneCollection collection);
        void HoldOverflow();
        void ReleaseOverflow();
        void Remove(ISceneCollection collection);
    }

    class UndoService : ServiceBase, IUndoService
    {
        // Keyed by collection.id for stability
        readonly Dictionary<string, ISceneCollection> collectionsById = new();
        readonly Dictionary<string, (ProgressBar bar, double startTime, bool isOverflow)> progressBars = new();
        readonly List<string> collectionOrder = new(); // stable insertion order

        const double undoTimeout = 10;
        const double lingerTime = 0.5;
        const int maxVisible = 2; // how many undo items can be visible before overflow starts

        public IEnumerable<ISceneCollection> visibleItems =>
            collectionOrder.Where(id => progressBars.TryGetValue(id, out var t) && !t.isOverflow)
                           .Select(id => collectionsById[id]);

        public IEnumerable<ISceneCollection> overflowItems =>
            collectionOrder.Where(id => progressBars.TryGetValue(id, out var t) && t.isOverflow)
                           .Select(id => collectionsById[id]);

        public bool preventCountdownFromFinishing
        {
            get => sessionState.GetProperty(false);
            set => sessionState.SetProperty(value);
        }

        bool isHoldingOverflow;

        public void HoldOverflow() => isHoldingOverflow = true;

        public void ReleaseOverflow()
        {

            if (SceneManager.profile)
                foreach (var collection in queuedForRemove)
                    SceneManager.profile.Delete(collection);
            queuedForRemove.Clear();

            isHoldingOverflow = false;
            RecalculateOverflow();
            SceneManager.events.InvokeCallbackSync<UndoItemsChangedEvent>();
        }

        protected override void OnInitialize()
        {
            RegisterEvent<CollectionRemovedEvent>(e => Reload());
            RegisterEvent<CollectionRestoredEvent>(e => Reload());
            RegisterEvent<CollectionDeletedEvent>(e => Reload());
            RegisterEvent<ProfileChangedEvent>(e => Reload());

            Reload();
            EditorApplication.update += Update;
        }

        protected override void OnDispose() =>
            EditorApplication.update -= Update;

        void Reload()
        {

            var profile = SceneManager.profile;
            if (!profile)
                return;

            // Remove entries that no longer exist
            foreach (var id in collectionOrder.ToList())
            {
                if (!profile.removedCollections.Any(c => c.id == id))
                {
                    collectionOrder.Remove(id);
                    progressBars.Remove(id);
                    collectionsById.Remove(id);
                }
            }

            // Add missing ones
            foreach (var col in profile.removedCollections)
            {
                if (!progressBars.ContainsKey(col.id))
                {
                    progressBars[col.id] = (null!, EditorApplication.timeSinceStartup + lingerTime, isOverflow: false);
                    collectionsById[col.id] = col;
                    collectionOrder.Add(col.id);
                }
            }

            RecalculateOverflow();
            SceneManager.events.InvokeCallbackSync<UndoItemsChangedEvent>();

        }

        readonly List<ISceneCollection> queuedForRemove = new();

        public void Remove(ISceneCollection collection)
        {

            if (isHoldingOverflow)
            {
                queuedForRemove.Add(collection);
                return;
            }

            if (SceneManager.profile)
                SceneManager.profile.Delete(collection);

            Clear(collection);

        }

        void Clear(ISceneCollection collection)
        {
            collectionOrder.Remove(collection.id);
            progressBars.Remove(collection.id);
            collectionsById.Remove(collection.id);

            RecalculateOverflow();
            SceneManager.events.InvokeCallbackSync<UndoItemsChangedEvent>();
        }

        void Update()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            if (progressBars.Count == 0)
                return;

            var now = EditorApplication.timeSinceStartup;

            foreach (var id in collectionOrder.ToList())
            {
                if (!progressBars.TryGetValue(id, out var tuple))
                    continue;

                var (bar, startTime, isOverflow) = tuple;

                if (bar is null)
                    continue;

                if (isOverflow)
                {
                    // freeze progress bar while in overflow
                    bar.value = 0;
                    continue;
                }

                double elapsed = now - startTime;
                bar.value = Mathf.Clamp01((float)(elapsed / undoTimeout));

                if (elapsed >= undoTimeout + lingerTime)
                {
                    if (!preventCountdownFromFinishing)
                        Remove(collectionsById[id]);
                }
            }
        }

        void RecalculateOverflow()
        {
            if (isHoldingOverflow)
            {
                // keep all items visible
                foreach (var id in collectionOrder)
                {
                    if (progressBars.TryGetValue(id, out var tuple))
                    {
                        var (bar, startTime, _) = tuple;
                        progressBars[id] = (bar, startTime, isOverflow: false);
                    }
                }
                return;
            }

            for (int i = 0; i < collectionOrder.Count; i++)
            {
                var id = collectionOrder[i];
                if (!progressBars.TryGetValue(id, out var tuple))
                    continue;

                var (bar, startTime, wasOverflow) = tuple;
                bool nowOverflow = i >= maxVisible;

                if (wasOverflow && !nowOverflow)
                {
                    // coming out of overflow → reset timer
                    progressBars[id] = (bar, EditorApplication.timeSinceStartup, nowOverflow);
                }
                else
                {
                    // keep existing startTime
                    progressBars[id] = (bar, startTime, nowOverflow);
                }
            }
        }

        public VisualElement GenerateView(ISceneCollection collection)
        {
            var view = SceneManagerWindow.window!.viewLocator.items.undo.CloneTree();

            if (collection is SceneCollection c)
                view.Bind(new(c));

            view.userData = collection;
            view.Q<Label>("label-name").text = $"<b>{collection.title}</b> has been removed.";

            var buttonUndo = view.Q<Button>("button-undo");
            var buttonDelete = view.Q<Button>("button-delete");

            var progressBar = view.Q<ProgressBar>();
            progressBar.lowValue = 0;
            progressBar.highValue = 1;

            if (progressBars.TryGetValue(collection.id, out var existing))
            {
                // reuse startTime
                progressBar.value = existing.bar?.value ?? 0;
                progressBars[collection.id] = (progressBar, existing.startTime, existing.isOverflow);
            }
            else
            {
                // shouldn't normally happen, but safe fallback
                progressBar.value = 0;
                progressBars[collection.id] = (progressBar, EditorApplication.timeSinceStartup + lingerTime, isOverflow: false);
                collectionsById[collection.id] = collection;
                collectionOrder.Add(collection.id);
                RecalculateOverflow();
            }

            buttonUndo.RegisterCallback<ClickEvent>(e =>
            {
                Clear(collection);
                SceneManager.profile.Restore(collection);
                Reload();
            });

            buttonDelete.RegisterCallback<ClickEvent>(e => Remove(collection));

            return view;
        }
    }
}
