using MalbersAnimations.Events;
using MalbersAnimations.Scriptables;
using UnityEngine;
using System.Collections.Generic;
using MalbersAnimations.Reactions;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MalbersAnimations.Utilities
{
    [AddComponentMenu("Malbers/Interaction/Interactor")]
    [HelpURL("https://malbersanimations.gitbook.io/animal-controller/global-components/interactor")]
    public class MInteractor : MonoBehaviour, IInteractor
    {
        [Tooltip("Layer for the Interact with colliders")]
        [SerializeField] private LayerReference Layer = new(-1);
        [SerializeField] private QueryTriggerInteraction TriggerInteraction = QueryTriggerInteraction.Ignore;

        [Tooltip("ID for the Interactor")]
        public IntReference m_ID = new(0);

        [Tooltip("Collider set as Trigger to Find Interactables OnTrigger Enter")]
        //[RequiredField]
        public Collider InteractionArea;

        public GameObjectEvent OnFocused = new();
        public GameObjectEvent OnUnfocused = new();
        public GameObjectEvent OnInteractWithGO = new();
        public IntEvent OnInteractWith = new();

        public System.Action<IInteractable> OnFocusing;
        public System.Action<IInteractable> OnUnFocusing;
        public System.Action<IInteractable> OnInteract;

        public int ID => m_ID.Value;

        public bool Active { get => !enabled; set => enabled = !value; }

        public GameObject Owner => RealRoot.gameObject;

        /// <summary>Current Interactable this interactor has on its Interaction Area </summary>
        public HashSet<IInteractable> FocusedInteractables;

        /// <summary>Interaction Trigger Proxy to Subscribe to OnEnter OnExit Trigger</summary>
        public TriggerProxy Proxy { get; set; }

        public List<MInteractorReaction> reactions = new();
        private Transform RealRoot;

        //  public IInteractable FocusedItem;

        public bool debug;

        private void OnValidate()
        {
            if (InteractionArea != null) { InteractionArea.isTrigger = true; }
        }


        private void OnEnable()
        {
            FocusedInteractables = new();

            //if (InteractionArea == null) InteractionArea = GetComponent<Collider>();
            //if (InteractionArea == null) Debugging("Interaction Collider is missing, please assign a Collider to the Interactor");

            RealRoot = transform.FindObjectCore();

            if (InteractionArea)
            {
                Proxy = TriggerProxy.CheckTriggerProxy(InteractionArea, Layer, TriggerInteraction, RealRoot, true);

                if (Proxy)
                {
                    Proxy.OnTrigger_Enter.AddListener(TriggerEnter);
                    Proxy.OnTrigger_Exit.AddListener(TriggerExit);

                    Proxy.Layer = Layer; //Set the Layer of the Proxy
                    Proxy.TriggerInteraction = TriggerInteraction; //Set the Trigger Interaction of the Proxy
                }
            }
        }


        private void OnDisable()
        {
            foreach (var item in FocusedInteractables)
            {
                if (item.Owner)
                {
                    OnUnfocused.Invoke(item.Owner);
                    OnUnFocusing?.Invoke(item);     //System.Action to notify the UnFocusing of an Interactable
                }

                //UnFocus the Interactable
                item.UnFocus(this);
            }

            FocusedInteractables = new();

            if (Proxy)
            {
                Proxy.OnTrigger_Enter.RemoveListener(TriggerEnter);
                Proxy.OnTrigger_Exit.RemoveListener(TriggerExit);
            }
        }


        private void TriggerEnter(Collider collider)
        {
            var NewInteractables = collider.FindInterfaces<IInteractable>(); //Find all Interactables

            if (NewInteractables != null)
                foreach (var item in NewInteractables)
                {
                    //The new interactable its already there
                    if (FocusedInteractables.Contains(item)) continue;
                    Focus(item);
                }
        }

        private void TriggerExit(Collider collider)
        {
            if (collider != null)
            {
                var NewInteractable = collider.FindInterfaces<IInteractable>();

                if (NewInteractable != null)
                {
                    foreach (var item in NewInteractable)
                    {
                        if (item != null && FocusedInteractables.Contains(item)) //means the interactor is exiting
                            UnFocus(item);
                    }
                }
            }
        }

        public virtual void Focus(IInteractable item)
        {
            if (item != null && item.Active)           //Ignore One Disable Interactors
            {
                FocusedInteractables.Add(item);        //add to the list all the focus items
                item.Focus(this);                      //Focus the Interactable

                OnFocused.Invoke(item.Owner);
                OnFocusing?.Invoke(item);              //System.Action to notify the Focusing of an Interactable


                if (item.Auto) Interact(item);         //Interact if the interactable is on Auto
            }
        }

        public void UnFocus(IInteractable item)
        {
            if (item != null && FocusedInteractables.Contains(item))
            {
                if (item.Owner)
                {
                    OnUnfocused.Invoke(item.Owner);
                    OnUnFocusing?.Invoke(item); //System.Action to notify the UnFocusing of an Interactable
                }

                //UnFocus the Interactable
                item.UnFocus(this);
                FocusedInteractables.Remove(item);
            }
        }


        /// <summary> Receive an Interaction from the Interacter </summary>
        public bool Interact(IInteractable inter)
        {
            if (inter.Interact(this))
            {
                OnInteractWithGO.Invoke(inter.Owner);
                OnInteractWith.Invoke(inter.Index);

                OnInteract?.Invoke(inter); //System.Action to notify the Interaction of an Interactable

                reactions.ForEach(r => r.React(inter.Index, inter.Owner)); //React with all the reactions

                Debugging($"Interact with <B>[{inter.transform.name}] [ID: {inter.Index}]</B>");

                return true;
            }
            return false;
        }

        /// <summary> Interact with multiple focused items at the same time (in reverse order) </summary>
        public void Interact()
        {
            if (FocusedInteractables == null || FocusedInteractables.Count == 0)
                return;

            var interactablesArray = new IInteractable[FocusedInteractables.Count];
            FocusedInteractables.CopyTo(interactablesArray);

            for (int i = interactablesArray.Length - 1; i >= 0; i--)
            {
                Interact(interactablesArray[i]);
            }
        }

        public void RemoveFocusedItem(IInteractable item)
        {
            if (FocusedInteractables != null && FocusedInteractables.Contains(item))
            {
                FocusedInteractables.Remove(item);
                item.UnFocus(this);
                OnUnfocused.Invoke(item.Owner);
                OnUnFocusing?.Invoke(item);             //System.Action to notify the UnFocusing of an Interactable
            }
        }

        public void Restart()
        {
            FocusedInteractables = new();
            OnUnfocused.Invoke(null);
            OnFocused.Invoke(null);

            OnUnFocusing?.Invoke(null);
            OnFocusing?.Invoke(null);
        }

        public void Interact(GameObject interactable)
        {
            if (interactable)
                Interact(interactable.FindInterface<IInteractable>());
        }

        public void Interact(Component interactable)
        {
            if (interactable)
                Interact(interactable.FindInterface<IInteractable>());
        }

        private void Debugging(string msg)
        {
            if (debug)
                MDebug.Log($"<B><color=yellow>Interactor: </color>[{Owner.name}]</B> -> [<color=yellow>{msg}</color>]", this);
        }


        [SerializeField] private int Editor_Tabs1;
    }


    [System.Serializable]
    public class MInteractorReaction
    {
        public string Description = "Reaction by Interactor";
        public ComparerInt Is = ComparerInt.Equal;
        [Tooltip("Interactable Index. Set it to Zero or 1 to use this reaction with all Interactables")]
        public IntReference Index = new();
        public Reaction2 reaction;

        public bool React(int ID, GameObject Target)
        {
            if (reaction.IsValid)
            {
                if (Index.Value <= 0 || Index.Value.CompareInt(ID, Is))
                {
                    return reaction.TryReact(Target);
                }
            }
            return false;
        }
    }




#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(MInteractor))]
    public class MInteractorEditor : UnityEditor.Editor
    {
        SerializedProperty m_ID, InteractionArea, Editor_Tabs1,
            OnFocusedInteractable,
            OnUnfocusedInteractable,
            OnInteractWithGO, OnInteractWith,
            reactions, debug,
            triggerInteraction, Layer;
        protected string[] Tabs1 = new string[] { "General", "Events", "Reactions" };

        MInteractor M;

        private void OnEnable()
        {
            M = (MInteractor)target;

            m_ID = serializedObject.FindProperty("m_ID");
            InteractionArea = serializedObject.FindProperty("InteractionArea");
            OnInteractWithGO = serializedObject.FindProperty("OnInteractWithGO");
            OnInteractWith = serializedObject.FindProperty("OnInteractWith");
            Editor_Tabs1 = serializedObject.FindProperty("Editor_Tabs1");
            OnFocusedInteractable = serializedObject.FindProperty("OnFocused");
            OnUnfocusedInteractable = serializedObject.FindProperty("OnUnfocused");
            Layer = serializedObject.FindProperty("Layer");
            triggerInteraction = serializedObject.FindProperty("TriggerInteraction");
            reactions = serializedObject.FindProperty("reactions");
            debug = serializedObject.FindProperty("debug");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            MalbersEditor.DrawDescription("Interactor element that invoke events when interacts with an Interactable");

            using (new GUILayout.HorizontalScope())
            {
                Editor_Tabs1.intValue = GUILayout.Toolbar(Editor_Tabs1.intValue, Tabs1);
                MalbersEditor.DrawDebugIcon(debug);
            }

            switch (Editor_Tabs1.intValue)
            {
                case 0: DrawGeneral(); break;
                case 1: DrawEvents(); break;
                case 2: DrawReactions(); break;
                default: break;
            }

            if (Application.isPlaying)
            {
                using (new EditorGUI.DisabledGroupScope(true))
                {
                    using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        if (M.FocusedInteractables != null)
                        {
                            foreach (var item in M.FocusedInteractables)
                            {
                                EditorGUILayout.ObjectField($"Focused Item [ID:{item.Index}]", item.Owner, typeof(GameObject), false);
                            }
                        }
                    }
                }
                Repaint();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawReactions()
        {
            EditorGUILayout.PropertyField(reactions);
        }

        private void DrawGeneral()
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(Layer);
                EditorGUILayout.PropertyField(triggerInteraction);
                EditorGUILayout.PropertyField(m_ID);
                EditorGUILayout.PropertyField(InteractionArea);
            }
        }

        private void DrawEvents()
        {
            EditorGUILayout.PropertyField(OnInteractWithGO);
            EditorGUILayout.PropertyField(OnInteractWith);
            EditorGUILayout.PropertyField(OnFocusedInteractable);
            EditorGUILayout.PropertyField(OnUnfocusedInteractable);
        }
    }
#endif
}