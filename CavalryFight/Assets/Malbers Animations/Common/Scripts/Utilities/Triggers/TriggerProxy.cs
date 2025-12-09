using UnityEngine;
using MalbersAnimations.Events;
using MalbersAnimations.Scriptables;
using System.Collections.Generic;
using System;
using UnityEngine.Events;
using MalbersAnimations.Conditions;
using MalbersAnimations.Reactions;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MalbersAnimations.Utilities
{
    /// <summary>
    /// This is used when the collider is in a different gameObject and you need to check the Collider Events
    /// Create this component at runtime and subscribe to the UnityEvents </summary>
    [AddComponentMenu("Malbers/Utilities/Colliders/Trigger Proxy")]
    public class TriggerProxy : MonoBehaviour
    {
        [Tooltip("Hit Layer for the Trigger Proxy")]
        [SerializeField] private LayerReference hitLayer = new(-1);
        public LayerMask Layer { get => hitLayer.Value; set => hitLayer.Value = value; }


        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
        [Tooltip("Search only Tags")] public Tag[] Tags;

        public ColliderEvent OnTrigger_Enter = new();
        public ColliderEvent OnTrigger_Exit = new();
        public ColliderEvent OnTrigger_Stay = new();

        public GameObjectEvent OnGameObjectEnter = new();
        public GameObjectEvent OnGameObjectExit = new();
        public GameObjectEvent OnGameObjectStay = new();
        public UnityEvent OnEmpty = new();

        [Tooltip("ID of the Trigger Proxy. This is used to identify the Trigger Proxy in the Editor and at runtime")]
        public IntReference ID = new(-1);


        public Reaction2 TriggerEnterReaction;
        public Reaction2 TriggerExitReaction;
        public Reaction2 TriggerStayReaction;

        public Reaction2 GameObjectEnterReaction;
        public Reaction2 GameObjectExitReaction;
        public Reaction2 GameObjectStayReaction;

        [SerializeField] private bool m_debug = false;

        public BoolReference useOnTriggerStay = new();

        [Tooltip("Trigger will be disabled the first time it finds a valid collider")]
        public BoolReference OneTimeUse = new();
        [Tooltip("Do not Interact with static colliders")]
        public BoolReference ignoreStatic = new();

        protected internal HashSet<Collider> m_colliders = new(8);
        /// <summary>All the Gameobjects using the Proxy</summary>
        protected internal HashSet<GameObject> EnteringGameObjects = new(8);

        [Tooltip("Extra conditions to check to filter the colliders entering OnTrigger Enter")]
        public Conditions2 Conditions = new();

        public Action<GameObject, Collider, TriggerProxy> EnterTriggerInteraction;
        public Action<GameObject, Collider, TriggerProxy> ExitTriggerInteraction;

        /// <summary> Is this component enabled? /summary>
        public bool Active { get => enabled; set => enabled = value; }

        //public int ID { get => m_ID.Value; set => m_ID.Value = value; }

        public QueryTriggerInteraction TriggerInteraction { get => triggerInteraction; set => triggerInteraction = value; }

        /// <summary> Collider Component used for the Trigger Proxy </summary>
        [RequiredField] public Collider ownCollider;
        public Transform Owner { get; set; }

        public virtual bool TrueConditions(Collider other)
        {
            if (!Active) return false;

            if (Tags != null && Tags.Length > 0)
            {
                if (!other.gameObject.HasMalbersTagInParent(Tags)) return false;
            }

            if (ownCollider == null) return false; // You don't have a trigger
            if (other == null) return false; // you are CALLING A ELIMINATED ONE
            if (other.gameObject.isStatic && ignoreStatic.Value) return false; // you are CALLING A ELIMINATED ONE
            if (triggerInteraction == QueryTriggerInteraction.Ignore && other.isTrigger) return false; // Check Trigger Interactions 
            if (!MTools.Layer_in_LayerMask(other.gameObject.layer, Layer)) return false;
            if (transform.SameHierarchy(other.transform)) return false;                 // Do not Interact with yourself
            if (Owner != null && other.transform.SameHierarchy(Owner)) return false;    // Do not Interact with yourself

            if (!Conditions.Evaluate(other)) return false; // Check the conditions

            return true;
        }

        public virtual void OnTriggerEnter(Collider other)
        {
            if (!TrueConditions(other)) return;

            GameObject realRoot = MTools.FindRealRoot(other);

            OnTrigger_Enter.Invoke(other); //Invoke when a Collider enters the Trigger
            TriggerEnterReaction.React(other);

            if (m_debug) Debug.Log($"<b>{name}</b> [Entering Collider] -> [{other.name}]", this);


            if (m_colliders.Add(other)) //if the entering collider is not already on the list add it
            {
                AddTarget(other);
            }

            if (EnteringGameObjects.Contains(realRoot))
            {
                return;
            }
            else
            {
                EnterTriggerInteraction?.Invoke(realRoot, other, this);
                EnteringGameObjects.Add(realRoot);
                OnGameObjectEnter.Invoke(realRoot);
                GameObjectEnterReaction.React(realRoot);

                if (m_debug) Debug.Log($"<b>{name}</b> [Entering GameObject] -> [{realRoot.name}]", this);

                if (OneTimeUse.Value) enabled = false;
            }
        }
        public virtual void OnTriggerExit(Collider other) => TriggerExit(other, true);

        public virtual void TriggerExit(Collider other, bool remove)
        {
            if (TrueConditions(other))
                RemoveTrigger(other, remove);
        }

        public virtual void RemoveTrigger(Collider other, bool remove)
        {
            GameObject realRoot = MTools.FindRealRoot(other);

            OnTrigger_Exit.Invoke(other);
            TriggerExitReaction.React(other);

            m_colliders.Remove(other);
            RemoveTarget(other, remove);

            if (m_debug) Debug.Log($"<b>{name}</b> [Exit Collider] -> [{other.name}]", this);

            if (EnteringGameObjects.Contains(realRoot)) //Means that the Entering GameObject still exist
            {
                // 0 allocation and lightweight method.
                bool anyMatchingColliders = false;
                foreach (var c in m_colliders)
                {
                    if (c && c.transform.SameHierarchy(realRoot.transform))
                    {
                        anyMatchingColliders = true;
                        break;
                    }
                }

                if (!anyMatchingColliders)
                {
                    EnteringGameObjects.Remove(realRoot);
                    OnGameObjectExit.Invoke(realRoot);
                    GameObjectExitReaction.React(realRoot);
                    ExitTriggerInteraction?.Invoke(realRoot, other, this);

                    if (m_debug) Debug.Log($"<b>{name}</b> [Leaving Gameobject] -> [{realRoot.name}]", this);
                }
            }

            if (m_colliders.Count == 0) ResetTrigger();

        }

        /// <summary>Add a Trigger Target to every new Collider found</summary>
        protected virtual void AddTarget(Collider other)
        {
            if (!other) return;
            var triggerTarget = TriggerRegistry.GetTargetForCollider(other);

            if (!triggerTarget)
            {
                triggerTarget = other.gameObject.AddComponent<TriggerTarget>();
            }

            triggerTarget.AddProxy(this);
        }


        /// <summary>OnTrigger exit Logic</summary>
        internal void RemoveTarget(Collider other, bool remove)
        {
            var triggerTarget = TriggerRegistry.GetTargetForCollider(other);

            if (!triggerTarget)
            {
                return;
            }

            if (remove)
                triggerTarget.RemoveProxy(this);
        }

        public virtual void ResetTrigger()
        {
            m_colliders.Clear();
            EnteringGameObjects.Clear();
            OnEmpty.Invoke();

            StopAllCoroutines();

            if (useOnTriggerStay.Value)
            {
                StartCoroutine(C_TriggerStay());
            }
        }

        public virtual void OnDisable()
        {
            if (m_colliders.Count > 0)
            {
                foreach (var c in m_colliders)
                {
                    if (c)
                    {
                        OnTrigger_Exit.Invoke(c); //the colliders may be destroyed
                        RemoveTarget(c, true);
                    }
                }
            }

            if (EnteringGameObjects.Count > 0)
            {
                foreach (var c in EnteringGameObjects)
                {
                    if (c) OnGameObjectExit.Invoke(c); //the gameobjects  may be destroyed
                }
            }

            if (m_debug) Debug.Log($"<b>{name}</b> [Exit All Colliders and Triggers] ", this);

            ResetTrigger();

            ownCollider.enabled = false; //Disable the Collider when the Trigger Proxy is disabled
        }

        public virtual void OnEnable()
        {
            ownCollider.enabled = true; //Enable the Collider when the Trigger Proxy is enabled
            ResetTrigger();
        }

        public virtual void Awake()
        {
            if (ownCollider == null) ownCollider = GetComponent<Collider>();

            if (ownCollider) ownCollider.isTrigger = true;
            else
                Debug.LogWarning("This Script requires a Collider, please add any type of collider", this);

            if (Owner == null) Owner = transform;

            ResetTrigger();
        }

        public virtual void Activate(bool value)
        {
            if (value && !gameObject.activeSelf)
                gameObject.SetActive(true); //Activate the GameObject if it is not active

            enabled = value;
        }

        //protected virtual void Update()
        //{
        //    CheckOntriggerStay();
        //}

        IEnumerator C_TriggerStay()
        {
            while (true)
            {
                yield return null;
                CheckOntriggerStay();
            }
        }

        public virtual void CheckOntriggerStay()
        {
            //  if (useOnTriggerStay.Value)
            // {
            foreach (var gos in EnteringGameObjects)
            {
                OnGameObjectStay.Invoke(gos);
                GameObjectStayReaction.React(gos);
            }

            foreach (var col in m_colliders)
            {
                OnTrigger_Stay.Invoke(col);
                TriggerStayReaction.React(col);
            }
            //  }
        }

        public virtual void SetLayer(LayerMask mask, QueryTriggerInteraction triggerInteraction, Transform Owner, Tag[] tags = null)
        {
            TriggerInteraction = triggerInteraction;
            Tags = tags;
            Layer = mask;
            this.Owner = Owner;

        }

        public static TriggerProxy CheckTriggerProxy
            (Collider col, LayerMask Layer, QueryTriggerInteraction TriggerInteraction, Transform Owner, bool overrideValue = false)
        {
            TriggerProxy Proxy = null;

            if (col == null) return Proxy;

            if (!col.TryGetComponent(out Proxy))
            {
                Proxy = col.gameObject.AddComponent<TriggerProxy>();
            }

            if (overrideValue) Proxy.SetLayer(Layer, TriggerInteraction, Owner);

            col.gameObject.SetLayer(2, false); //Force the Trigger Area to be on the Ignore Raycast Layer
            col.isTrigger = true;   //Force to be a Trigger

            return Proxy;
        }

        [HideInInspector] public int Editor_Tabs1;
    }

    #region Inspector


#if UNITY_EDITOR
    [CanEditMultipleObjects, CustomEditor(typeof(TriggerProxy))]
    public class TriggerProxyEditor : Editor
    {
        SerializedProperty debug, ID,
            OnTrigger_Enter, OnTrigger_Exit, OnEmpty, useOnTriggerStay, OnTrigger_Stay, ignoreStatic, Editor_Tabs1, OneTimeUse,
            triggerInteraction, hitLayer, OnGameObjectEnter, OnGameObjectExit, OnGameObjectStay, Tags, Conditions,
            TriggerEnterReaction, TriggerExitReaction, TriggerStayReaction,
            GameObjectEnterReaction, GameObjectExitReaction, GameObjectStayReaction
            ;

        TriggerProxy m;

        protected string[] Tabs1 = new string[] { "General", "Events", "Reactions" };

        private void OnEnable()
        {
            m = (TriggerProxy)target;
            OnEmpty = serializedObject.FindProperty("OnEmpty");
            triggerInteraction = serializedObject.FindProperty("triggerInteraction");
            useOnTriggerStay = serializedObject.FindProperty("useOnTriggerStay");
            hitLayer = serializedObject.FindProperty("hitLayer");
            debug = serializedObject.FindProperty("m_debug");
            ignoreStatic = serializedObject.FindProperty("ignoreStatic");
            ID = serializedObject.FindProperty("ID");

            OnTrigger_Enter = serializedObject.FindProperty("OnTrigger_Enter");
            OnTrigger_Exit = serializedObject.FindProperty("OnTrigger_Exit");
            OnTrigger_Stay = serializedObject.FindProperty("OnTrigger_Stay");


            OnGameObjectEnter = serializedObject.FindProperty("OnGameObjectEnter");
            OnGameObjectExit = serializedObject.FindProperty("OnGameObjectExit");
            OnGameObjectStay = serializedObject.FindProperty("OnGameObjectStay");

            Tags = serializedObject.FindProperty("Tags");
            Editor_Tabs1 = serializedObject.FindProperty("Editor_Tabs1");
            OneTimeUse = serializedObject.FindProperty("OneTimeUse");
            Conditions = serializedObject.FindProperty("Conditions");


            TriggerEnterReaction = serializedObject.FindProperty("TriggerEnterReaction");
            TriggerExitReaction = serializedObject.FindProperty("TriggerExitReaction");
            TriggerStayReaction = serializedObject.FindProperty("TriggerStayReaction");

            GameObjectEnterReaction = serializedObject.FindProperty("GameObjectEnterReaction");
            GameObjectExitReaction = serializedObject.FindProperty("GameObjectExitReaction");
            GameObjectStayReaction = serializedObject.FindProperty("GameObjectStayReaction");
        }


        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            MalbersEditor.DrawDescription("Use this component to do quick OnTrigger Enter/Exit logics");

            Editor_Tabs1.intValue = GUILayout.Toolbar(Editor_Tabs1.intValue, Tabs1);


            switch (Editor_Tabs1.intValue)
            {
                case 0: DrawGeneral(); break;
                case 1: DrawEvents(); break;
                case 2: DrawReactions(); break;
            }

            if (Application.isPlaying)
            {
                using (new EditorGUI.DisabledGroupScope(true))
                {
                    using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);

                        //   EditorGUILayout.ObjectField("Own Collider", m.trigger, typeof(Collider), false);

                        EditorGUILayout.LabelField("GameObjects (" + m.EnteringGameObjects.Count + ")", EditorStyles.boldLabel);
                        foreach (var item in m.EnteringGameObjects)
                        {
                            if (item != null) EditorGUILayout.ObjectField(item.name, item, typeof(GameObject), false);
                        }

                        EditorGUILayout.LabelField("Colliders (" + m.m_colliders.Count + ")", EditorStyles.boldLabel);

                        foreach (var item in m.m_colliders)
                        {
                            if (item != null) EditorGUILayout.ObjectField(item.name, item, typeof(Collider), false);
                        }

                        //EditorGUILayout.LabelField("Targets (" + m.TriggerTargets.Count + ")", EditorStyles.boldLabel);

                        //foreach (var item in m.TriggerTargets)
                        //{
                        //    if (item != null) EditorGUILayout.ObjectField(item.name, item, typeof(Collider), false);
                        //}
                    }
                    Repaint();
                }
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawReactions()
        {
            EditorGUILayout.PropertyField(TriggerEnterReaction);
            EditorGUILayout.PropertyField(TriggerExitReaction);
            if (m.useOnTriggerStay.Value)
                EditorGUILayout.PropertyField(TriggerStayReaction);

            EditorGUILayout.PropertyField(GameObjectEnterReaction);
            EditorGUILayout.PropertyField(GameObjectExitReaction);
            if (m.useOnTriggerStay.Value)
                EditorGUILayout.PropertyField(GameObjectStayReaction);
        }

        private void DrawGeneral()
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new GUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(ID);
                    MalbersEditor.DrawDebugIcon(debug);
                }
                EditorGUILayout.PropertyField(hitLayer, new GUIContent("Layer"));

                EditorGUILayout.PropertyField(triggerInteraction);
                EditorGUILayout.PropertyField(useOnTriggerStay);
                EditorGUILayout.PropertyField(OneTimeUse);
                EditorGUILayout.PropertyField(ignoreStatic);
                EditorGUILayout.PropertyField(Tags, true);
                EditorGUILayout.PropertyField(Conditions);
            }
        }



        private void DrawEvents()
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(OnTrigger_Enter, new GUIContent("On Trigger Enter"));
                EditorGUILayout.PropertyField(OnTrigger_Exit, new GUIContent("On Trigger Exit"));
                EditorGUILayout.PropertyField(OnEmpty);
                if (m.useOnTriggerStay.Value)
                    EditorGUILayout.PropertyField(OnTrigger_Stay, new GUIContent("On Trigger Stay"));


                EditorGUILayout.PropertyField(OnGameObjectEnter, new GUIContent("On GameObject Enter "));
                EditorGUILayout.PropertyField(OnGameObjectExit, new GUIContent("On GameObject Exit"));
                if (m.useOnTriggerStay.Value)
                    EditorGUILayout.PropertyField(OnGameObjectStay, new GUIContent("On GameObject Stay"));
            }
        }
    }
#endif
    #endregion
}