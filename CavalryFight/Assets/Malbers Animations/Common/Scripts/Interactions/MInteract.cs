using MalbersAnimations.Scriptables;
using UnityEngine;
using MalbersAnimations.Events;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MalbersAnimations.Utilities
{
    [AddComponentMenu("Malbers/Interaction/Interactable")]
    [DefaultExecutionOrder(15), SelectionBase]
    [HelpURL("https://malbersanimations.gitbook.io/animal-controller/global-components/interactable")]
    public class MInteract : MonoBehaviour, IInteractable
    {
        [Tooltip("Own Index. This is used to Identify each Interactable. 0 or -1 means that all interactors can interact with this.")]
        public IntReference m_ID = new(0);
        [Tooltip("ID for the Interactor. Makes this Interactable to interact only with Interactors with this ID Value\n" +
            "By default its -1, which means that can be activated by anyone")]
        public IntReference m_InteractorID = new(-1);

        [Tooltip("If the Interactor has this Interactable focused, it will interact with it automatically.\n" +
            "It also is used by the AI Animals. If the Animal Reaches this gameobject to Interact with it this needs to be set to true")]
        [SerializeField] private BoolReference m_Auto = new(false);

        [Tooltip("Interact Once, after that it cannot longer work, unless the Interactable is Restarted. Disable the component")]
        [SerializeField] private BoolReference m_singleInteraction = new(false);

        [Tooltip("Destroy after a Single Interaction. (After the Delay)")]
        [SerializeField] private BoolReference m_Destroy = new(false);

        [Tooltip("Delay time to activate the events on the Interactable")]
        public FloatReference m_Delay = new(0);

        [Tooltip("CoolDown between Interactions when the Interactable is NOT a Single/One time interaction")]
        public FloatReference m_CoolDown = new(0);

        public bool debug;

        public List<MInteractorReaction> reactions = new();
        public GameObjectEvent OnInteractWithGO = new();
        public IntEvent OnInteractWithID = new();
        public GameObjectEvent OnFocused = new();
        public GameObjectEvent OnUnfocused = new();
        public BoolEvent OnCoolDown = new();
        public int Index => m_ID;

        public bool Active { get => enabled && !InCooldown; set => enabled = value; }
        public bool SingleInteraction { get => m_singleInteraction.Value; set => m_singleInteraction.Value = value; }
        public bool Auto { get => m_Auto.Value; set => m_Auto.Value = value; }
        // public bool Active { get =>enabled; set => enabled = value; }

        /// <summary>Delay time to Activate the Interaction on the Interactable</summary>
        public float Delay { get => m_Delay.Value; set => m_Delay.Value = value; }

        /// <summary>CoolDown Between Interactions</summary>
        public float Cooldown { get => m_CoolDown.Value; set => m_CoolDown.Value = value; }

        /// <summary>Is the Interactable in CoolDown?</summary>
        public bool InCooldown => !MTools.ElapsedTime(CurrentActivationTime, Cooldown);

        public IInteractor FocusedBy { get; set; }

        public virtual bool Focused { get; set; }

        public GameObject Owner { get; set; }

        protected float CurrentActivationTime;

        public string Description = "Invoke events when an Interactor interacts with it";
        [HideInInspector] public bool ShowDescription = true;
        [ContextMenu("Show Description")]
        internal void EditDescription() => ShowDescription ^= true;

        public virtual void OnEnable()
        {
            Owner = transform.FindObjectCore().gameObject;
            CurrentActivationTime = -Cooldown;
        }

        public virtual void OnDisable()
        {
            FocusedBy?.UnFocus(this); //Make sure the Interactor is unfocused when the Interactable is disabled

            UnFocus(FocusedBy); //Unfocus the Interactable if it was focused by an Interactor
        }

        public void UnFocus(IInteractor focuser)
        {
            if (Focused && FocusedBy != null && FocusedBy == focuser)
            {
                Focused = false; //Set the Interactable as Unfocused
                OnUnfocused.Invoke(FocusedBy.Owner); //Invoke the Unfocused Event
                Debugging($"Unfocused by [{focuser.Owner.name}] [ID: {focuser.ID}]");

                FocusedBy = null;
            }
        }

        public void Focus(IInteractor focuser)
        {
            FocusedBy = focuser;      //Set the new Interactor
            Focused = true; //Set the Interactable as Focused
            OnFocused.Invoke(FocusedBy.Owner);
            Debugging($"Focused by [{focuser.Owner.name}] [ID: {focuser.ID}]");
        }



        private void Debugging(string msg)
        {
            if (debug)
                MDebug.Log($"<B><color=green>Interactable</color>:</b> [{name}] -> [<color=green><B>{msg}</B></color>]", this);
        }


        /// <summary> Receive an Interaction from the Interacter </summary>
        /// <param name="InteracterID">ID of the Interacter</param>
        /// <param name="interacter">Interactor's GameObject</param>
        public bool Interact(int InteracterID, GameObject interacter)
        {
            if (Active)
            {
                if (m_InteractorID <= 0 || m_InteractorID == InteracterID) //Check for Interactor ID
                {
                    CurrentActivationTime = Time.time;

                    this.Delay_Action(Delay, () =>
                     {
                         OnInteractWithGO.Invoke(interacter);
                         OnInteractWithID.Invoke(InteracterID);

                         foreach (var r in reactions)
                         {
                             r.React(InteracterID, interacter);
                         }

                         if (interacter) Debugging($"Interacted with [{interacter.name}] [ID: {InteracterID}]");
                     }
                    );

                    if (SingleInteraction)
                    {
                        Focused = false;
                        OnUnfocused.Invoke(interacter);
                        Active = false;

                        if (m_Destroy.Value)
                        {
                            Destroy(base.gameObject, Delay + 0.001f); //Destroy one frame after
                        }
                    }

                    if (Cooldown > 0 && !m_Destroy.Value)
                    {
                        OnCoolDown.Invoke(true);
                        this.Delay_Action(Cooldown, () => OnCoolDown.Invoke(false));
                    }
                    return true;
                }
                return false;
            }
            return false;
        }

        /// <summary>  Receive an Interaction from an gameObject </summary>
        /// <param name="InteracterID">ID of the Interacter</param>
        /// <param name="interacter">Interactor's GameObject</param>
        public virtual bool Interact(IInteractor interacter)
        {
            if (interacter != null)
                return Interact(interacter.ID, interacter.Owner);

            return false;
        }

        public virtual void Interact() => Interact(-1, null);

        public virtual void Restart()
        {
            Focused = false;
            OnUnfocused.Invoke(null);

            Active = true;
            CurrentActivationTime = -Cooldown;
        }

        [SerializeField] private int Editor_Tabs1;

        public void DestroyMe(float time)
        {
            Destroy(base.gameObject, time);
        }
    }

    #region Inspector
    //-------------------------INSPECTOR-------------------------------------------------------------------------------------------------------------------
#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(MInteract)), CanEditMultipleObjects]
    public class MInteractEditor : UnityEditor.Editor
    {
        SerializedProperty m_ID, m_InteractorID, m_Auto, m_singleInteraction, m_Delay, m_Destroy,
            m_CoolDown, OnFocused, OnUnfocused, OnInteractWithGO, OnInteractWithID, debug, reactions,
            OnCoolDown, Editor_Tabs1, Description, ShowDescription;
        protected string[] Tabs1 = new string[] { "General", "Events", "Reactions" };
        MInteract M;

        public static GUIStyle StyleBlue => MTools.Style(new Color(0, 0.5f, 1f, 0.3f));
        private GUIStyle style;
        private void OnEnable()
        {
            M = (MInteract)target;
            m_ID = serializedObject.FindProperty("m_ID");
            m_InteractorID = serializedObject.FindProperty("m_InteractorID");
            m_Auto = serializedObject.FindProperty("m_Auto");
            m_singleInteraction = serializedObject.FindProperty("m_singleInteraction");
            m_Delay = serializedObject.FindProperty("m_Delay");
            m_CoolDown = serializedObject.FindProperty("m_CoolDown");
            OnFocused = serializedObject.FindProperty("OnFocused");
            OnUnfocused = serializedObject.FindProperty("OnUnfocused");
            Editor_Tabs1 = serializedObject.FindProperty("Editor_Tabs1");
            ShowDescription = serializedObject.FindProperty("ShowDescription");
            Description = serializedObject.FindProperty("Description");
            m_Destroy = serializedObject.FindProperty("m_Destroy");
            OnCoolDown = serializedObject.FindProperty("OnCoolDown");


            OnInteractWithGO = serializedObject.FindProperty("OnInteractWithGO");
            OnInteractWithID = serializedObject.FindProperty("OnInteractWithID");
            debug = serializedObject.FindProperty("debug");

            reactions = serializedObject.FindProperty("reactions");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (ShowDescription.boolValue)
            {
                if (style == null)
                {
                    style = new GUIStyle(StyleBlue)
                    {
                        fontSize = 12,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleLeft,
                        stretchWidth = true
                    };
                    style.normal.textColor = EditorStyles.label.normal.textColor;
                }

                Description.stringValue = UnityEditor.EditorGUILayout.TextArea(Description.stringValue, style);
            }

            //MalbersEditor.DrawDescription("Interactable Element that invoke events when an Interactor interact with it");
            Editor_Tabs1.intValue = GUILayout.Toolbar(Editor_Tabs1.intValue, Tabs1);

            if (Application.isPlaying)
            {
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUI.DisabledGroupScope(true))
                    {
                        EditorGUILayout.ObjectField("Current Interactor",
                            M.FocusedBy?.Owner, typeof(GameObject), false);

                        Repaint();
                    }
                }
            }

            switch (Editor_Tabs1.intValue)
            {
                case 0: DrawGeneral(); break;
                case 1: DrawEvents(); break;
                case 2: DrawReactions(); break;
                default:
                    break;
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

                using (new GUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(m_InteractorID, new GUIContent("Interactor ID"));
                    MalbersEditor.DrawDebugIcon(debug);
                }
                EditorGUILayout.PropertyField(m_ID, new GUIContent("Index"));

                EditorGUILayout.PropertyField(m_Auto, new GUIContent("Auto Interact"));
                EditorGUILayout.PropertyField(m_singleInteraction, new GUIContent("Single Interaction"));
                EditorGUILayout.PropertyField(m_Delay);
                if (!M.SingleInteraction)
                {
                    EditorGUILayout.PropertyField(m_CoolDown, new GUIContent("Cooldown"));
                }
                else
                {
                    EditorGUILayout.PropertyField(m_Destroy);
                }
            }

            EditorGUIUtility.labelWidth = 0;
        }

        private void DrawEvents()
        {
            EditorGUILayout.PropertyField(OnInteractWithGO);
            EditorGUILayout.PropertyField(OnInteractWithID);
            EditorGUILayout.PropertyField(OnFocused);
            EditorGUILayout.PropertyField(OnUnfocused);

            if (M.Cooldown > 0)
                EditorGUILayout.PropertyField(OnCoolDown);
        }
    }
#endif
    //-------------------------------------------------------------------------------------------------------------------------------------------------------
    #endregion
}