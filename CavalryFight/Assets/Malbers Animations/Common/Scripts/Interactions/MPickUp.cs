using MalbersAnimations.Events;
using MalbersAnimations.Scriptables;
using MalbersAnimations.Utilities;
using UnityEngine;
using MalbersAnimations.Reactions;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MalbersAnimations.Controller
{
    [AddComponentMenu("Malbers/Interaction/Pick Up - Drop")]
    public class MPickUp : MonoBehaviour, IAnimatorListener
    {
        [System.Serializable]
        public struct ExtraHolder
        {
            public Transform transform;
            public Vector3 position;
            public Vector3 rotation;
        }

        [RequiredField, Tooltip("Trigger used to find Items that can be picked Up")]
        public Collider PickUpArea;
        [SerializeField, Tooltip("When an Item is Picked and Hold, the Pick Trigger area will be disabled")]
        private BoolReference m_HidePickArea = new(true);
        //public bool AutoPick { get => m_AutoPick.Value; set => m_AutoPick.Value = value; }

        [Tooltip("Transform to Parent the Picked Item")]
        public Transform Holder;
        public Vector3 PosOffset;
        public Vector3 RotOffset;

        public List<ExtraHolder> extraHolders;

        [Tooltip("Check for tags on the Pickable items")]
        public Tag[] Tags;


        [Tooltip("Layer for the Interact with colliders")]
        [SerializeField] private LayerReference Layer = new(-1);
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        /// <summary> Real Root of the Picker Object  </summary>
        public Transform Root { get; set; }

        [Tooltip("Invokes a reaction if the Pickable is a collectable")]
        [SerializeReference] public Reaction CollectableReaction;

        [Tooltip("Ignore the Item Pick Delay, so the item can be picked instantly")]
        public BoolReference IgnoreItemPickDelay = new(false);

        [Tooltip("Ignore the Item Drop Delay, so the item can be dropped instantly")]
        public BoolReference IgnoreItemDropDelay = new(false);

        // [Header("Events")]
        public BoolEvent CanPickUp = new();
        public GameObjectEvent OnItemPicked = new();
        public GameObjectEvent OnItemDrop = new();
        public GameObjectEvent OnFocusedItem = new();
        public IntEvent OnPicking = new();
        public IntEvent OnDropping = new();

        public bool debug;
        public float DebugRadius = 0.02f;
        public Color DebugColor = Color.yellow;

        protected ICharacterAction character;

        /// <summary>  Who is the owner of this Pick Up Script  </summary>
        public GameObject Owner { get; private set; }

        [SerializeField] private TriggerProxy Proxy;

        /// <summary>Does the Animal is holding an Item</summary>

        public bool Has_Item => Item != null;

        protected bool PickingItem = false;

        [SerializeField] private ICollectable item;
        public virtual ICollectable Item
        {
            get => item;
            set
            {
                item = value;
                //   OnItem.Invoke(item != null ? item.gameObject : null);
                //  Debug.Log("item: " + item);
            }
        }

        private ICollectable focusedItem;
        public virtual ICollectable FocusedItem
        {
            get => focusedItem;
            set
            {
                focusedItem = value;
                OnFocusedItem.Invoke((GameObject)focusedItem?.gameObject);
                CanPickUp.Invoke(focusedItem != null);
            }
        }

        protected virtual void Awake()
        {
            character = gameObject.FindInterface<ICharacterAction>();

            Owner = character != null ? character.gameObject : gameObject;  //Set the Owner of the Pick Up Script

            CheckTriggerProxy();
        }

        protected virtual void CheckTriggerProxy()
        {
            Root = transform.FindObjectCore();

            if (PickUpArea)
            {
                Proxy = TriggerProxy.CheckTriggerProxy(PickUpArea, Layer, triggerInteraction, Root, true);
                // Proxy.SetLayer(Layer.Value, triggerInteraction, Root, Tags); //Set the Layer of the Proxy
            }
            else
            {
                Debug.LogWarning("Please set a Pick up Area");
            }
        }

        protected virtual void OnEnable()
        {
            Proxy.OnTrigger_Enter.AddListener(OnGameObjectEnter);
            Proxy.OnTrigger_Exit.AddListener(OnGameObjectExit);

            if (Has_Item) PickUpItem();         //If the animal has an item at start then make all the stuff to pick it up
        }

        protected virtual void OnDisable()
        {
            Proxy.OnTrigger_Enter.RemoveListener(OnGameObjectEnter);
            Proxy.OnTrigger_Exit.RemoveListener(OnGameObjectExit);
        }

        protected virtual void OnGameObjectEnter(Collider col)
        {
            var newItem = col.FindInterface<ICollectable>();

            if (newItem != null && newItem.Active)
            {
                //If we are choosing another focused Item then unfocused the one old item
                if (newItem != FocusedItem && FocusedItem != null)
                    FocusedItem.SetFocused(Owner, false);

                FocusedItem = newItem;
                FocusedItem.SetFocused(Owner, true);

                Debugging("Focused Item - " + FocusedItem.transform.name);

                if (FocusedItem.AutoPick) TryPickUp();
            }
        }


        public virtual void FocusItem(Component newObject)
        {
            if (newObject == null) //Means there's no New Focused Item
            {
                UnfocusedCurrentItem();
                return;
            }
            FocusItem(newObject.gameObject);
        }

        public virtual void FocusItem(GameObject newObject)
        {
            if (newObject == null) //Means there's no New Focused Item
            {
                UnfocusedCurrentItem();
                return;
            }

            var newItem = newObject.FindInterface<ICollectable>();

            if (newItem == null || !MTools.Layer_in_LayerMask(newItem.gameObject.layer, Layer.Value))  //there's no Pickable Item or the layer is not the correct one
            {
                // Debug.Log("there's no Pickable Item or the layer is not the correct one");
                UnfocusedCurrentItem();
                return;
            }

            if (newItem != null && newItem.Active)
            {
                //If we are choosing another focused Item then unfocused the one old item
                if (newItem != FocusedItem && FocusedItem != null)
                    FocusedItem.SetFocused(Owner, false);

                FocusedItem = newItem;
                FocusedItem.SetFocused(Owner, true);

                Debugging("Focused Item - " + FocusedItem.gameObject.name);

                if (FocusedItem.AutoPick) TryPickUp();
            }
        }

        private void UnfocusedCurrentItem()
        {
            if (FocusedItem != null)
            {
                Debugging("Unfocused Item - " + FocusedItem.gameObject.name);
                FocusedItem.SetFocused(Owner, false);
                FocusedItem = null;
            }
        }

        protected virtual void OnGameObjectExit(Collider col)
        {
            //Means there's a New Focused Item
            if (FocusedItem != null)
            {
                if (PickingItem) return; //Do not unfocused the item if is being picked up (Aligning to the Holder

                var newItem = col.FindInterface<ICollectable>();

                if (newItem == FocusedItem)
                {
                    UnfocusedCurrentItem();
                }
                else
                {
                    //Was another one that is not focused anymore (Make sure is stays unfocused)
                    newItem?.SetFocused(Owner, false);
                }
            }
        }

        public virtual void TryPickUpDrop()
        {
            if (character != null && character.IsPlayingAction) return; //Do not try if the Character is doing an action

            if (!Has_Item) TryPickUp();
            else TryDrop();
        }

        public virtual void TryDrop()
        {
            if (!enabled) return; //Do nothing if this script is disabled

            if (item != null && !item.InCoolDown)
            {
                if (character != null && !character.IsPlayingAction)
                {
                    Item.PreDrop(gameObject);
                }

                Debugging("Item Try Drop - " + Item.gameObject.name);

                if (IgnoreItemDropDelay.Value)
                    DropItem();
                else if (!item.ByAnimation)
                    Invoke(nameof(DropItem), Item.DropDelay);
            }
        }

        //private readonly IEnumerator TryAlign;

        /// <summary>  Tries the pickup logic checking all the correct conditions if the character does not have an item.  </summary>
        public virtual void TryPickUp()
        {
            if (!isActiveAndEnabled) return; //Do nothing if this script is disabled

            if (FocusedItem != null)
            {
                if (!FocusedItem.Active)
                {
                    FocusedItem.PickedFailed(character.gameObject);
                    Debugging("Item Picked Failed - " + FocusedItem.transform.name, FocusedItem.transform);
                }
                else if (!FocusedItem.InCoolDown)
                {
                    //Try Picking UP WHEN THE CHARACTER IS NOT MAKING ANY ANIMATION
                    if (character != null && !character.IsPlayingAction)
                    {
                        //Align_Item();
                        PickingItem = true;
                        FocusedItem.PrePicked(character.gameObject); //Do the On Picked First  
                    }
                    Debugging("Try Pick Up");

                    if (IgnoreItemPickDelay.Value)
                        PickUpItem();
                    else if (!FocusedItem.ByAnimation)
                        Invoke(nameof(PickUpItem), FocusedItem.PickDelay);
                }
            }
        }

        //private void Align_Item()
        //{
        //    if (FocusedItem is Pickable pickable && pickable.Align)
        //    {
        //        var Holder = this.Holder;

        //        if (extraHolders != null && FocusedItem.Holder > -1 && FocusedItem.Holder < extraHolders.Count)
        //        {
        //            Holder = extraHolders[FocusedItem.Holder].transform;
        //        }

        //        if (TryAlign != null) StopCoroutine(TryAlign);

        //        TryAlign = MTools.AlignTransform_Position(FocusedItem.transform, Holder, pickable.AlignTime);

        //        StartCoroutine(TryAlign);
        //    }
        //}


        /// <summary>Pick Up Logic. It can be called by the Animator</summary>
        public virtual void PickUpItem()
        {
            if (!isActiveAndEnabled) return; //Do nothing if this script is disabled

            Item ??= FocusedItem; //Check for the Picked Item

            if (Item != null)
            {
                if (!Item.Active) //Check first if the item cannot be picked
                {
                    FocusedItem.PickedFailed(character.gameObject);
                    Debugging("Item Picked Failed - " + FocusedItem.gameObject.name, FocusedItem.gameObject);
                    return;
                }

                Debugging("Item Picked - " + Item.gameObject.name);

                //if (TryAlign != null) StopCoroutine(TryAlign);

                PickingItem = false; //Try picking set to false

                ParentItemToHolster();

                // Item.Picker = this;                      //Set on the Item who did the Picking
                Item.Pick();                                    //Tell the Item that it was picked
                FocusedItem = null;                             //Remove the Focused Item

                OnItemPicked.Invoke(Item.gameObject);           //Invoke the Event
                OnPicking.Invoke(Item.ID);                      //Invoke the Event
                var item = Item; //Store before collectable

                //Check if the item is a collectable so Pick it and remove it from the 
                if (Item.Collectable)
                {
                    Item = null;

                    //Enable Disable to find new collectables in the same area
                    PickUpArea.enabled = false;
                    this.Delay_Action(() => PickUpArea.enabled = true);

                    CollectableReaction?.React(item.gameObject);
                }
                else
                {
                    if (m_HidePickArea.Value)
                        PickUpArea.enabled = false;        //Disable the Pick Up Area
                }
                Proxy.ResetTrigger();
            }
        }

        protected virtual void ParentItemToHolster()
        {
            var Holder = this.Holder;
            var PosOffset = this.PosOffset;
            var RotOffset = this.RotOffset;

            //Use extra holders 
            if (Item.Holder > -1 && Item.Holder < extraHolders.Count)
            {
                Holder = extraHolders[Item.Holder].transform;
                PosOffset = extraHolders[Item.Holder].position;
                RotOffset = extraHolders[Item.Holder].rotation;
            }

            if (Holder) Parent(Holder, PosOffset, RotOffset); //Parent the Item to the Holder
        }

        public virtual void Parent(Transform parent, Vector3 pos, Vector3 rot)
        {
            var localScale = Item.transform.localScale;
            Item.transform.parent = parent;               //Parent it to the Holder
            Item.transform.localPosition = pos;           //Offset the Position
            Item.transform.localEulerAngles = rot;        //Offset the Rotation
            Item.transform.localScale = localScale;       //Offset the Rotation
        }


        /// <summary> Drops the item logic</summary>
        public virtual void DropItem()
        {
            if (!enabled) return; //Do nothing if this script is disabled
            if (Has_Item)
            {
                Debugging("Item Dropped - " + Item.gameObject.name);

                Item.Drop();                                    //Tell the item is being dropped
                OnItemDrop.Invoke(Item.gameObject);
                OnDropping.Invoke(Item.ID);                     //Invoke the method

                // OnItemPicked.Invoke(null);
                Item = null;                                    //Remove the Item

                if (m_HidePickArea.Value)
                    PickUpArea.enabled = (true);         //Enable the Pick up Area

                if (FocusedItem != null && !FocusedItem.AutoPick) Proxy.ResetTrigger();
            }
        }

        private void Debugging(string msg) => Debugging(msg, this);


        private void Debugging(string msg, Object ob)
        {
#if UNITY_EDITOR
            if (debug) Debug.Log($"[{Root.name}] - [{msg}]", ob);
#endif
        }

        public virtual bool OnAnimatorBehaviourMessage(string message, object value) => this.InvokeWithParams(message, value);

        #region Context Menu



#if UNITY_EDITOR

        [ContextMenu("Connect to Weapon Manager (Holster_SetWeapon)")]
        private void ConnectToWeaponManagerHolster()
        {
            var method = this.GetUnityAction<GameObject>("MWeaponManager", "Holster_SetWeapon");
            if (method != null) UnityEditor.Events.UnityEventTools.AddPersistentListener(OnItemPicked, method);
            MTools.SetDirty(this);
        }



        [ContextMenu("Connect to Weapon Manager (Equip_External)")]
        private void ConnectToWeaponManagerExternal()
        {
            var method = this.GetUnityAction<GameObject>("MWeaponManager", "Equip_External");
            if (method != null) UnityEditor.Events.UnityEventTools.AddPersistentListener(OnItemPicked, method);
            MTools.SetDirty(this);
        }
#endif

        #endregion

#if MALBERS_DEBUG
        private void OnDrawGizmosSelected()
        {
            if (debug)
            {
                if (Holder)
                {
                    Gizmos.color = DebugColor;
                    Gizmos.DrawWireSphere(Holder.TransformPoint(PosOffset), DebugRadius);
                    Gizmos.DrawSphere(Holder.TransformPoint(PosOffset), DebugRadius);

                }

                foreach (var item in extraHolders)
                {
                    if (item.transform)
                    {
                        Gizmos.color = DebugColor;
                        Gizmos.DrawWireSphere(item.transform.TransformPoint(item.position), DebugRadius);
                        Gizmos.DrawSphere(item.transform.TransformPoint(item.position), DebugRadius);

                    }
                }
            }
        }
#endif
        [SerializeField] private int Editor_Tabs1;
    }

    #region INSPECTOR
#if UNITY_EDITOR
    [CustomEditor(typeof(MPickUp)), CanEditMultipleObjects]
    public class MPickUpEditor : Editor
    {

        private SerializedProperty
            PickUpArea, FocusedItem, Editor_Tabs1, Holder, RotOffset, extraHolders, IgnoreItemPickDelay, IgnoreItemDropDelay,
            item, m_HidePickArea, OnFocusedItem, CollectableReaction,
            Layer, triggerInteraction, OnItemDrop,
            PosOffset, CanPickUp, OnDropping, OnPicking, DebugRadius, OnItem, DebugColor, debug, Tags;

        protected string[] Tabs1 = new string[] { "General", "Events" };

        MPickUp M;


        private void OnEnable()
        {
            M = (MPickUp)target;

            PickUpArea = serializedObject.FindProperty("PickUpArea");
            Layer = serializedObject.FindProperty("Layer");
            triggerInteraction = serializedObject.FindProperty("triggerInteraction");
            m_HidePickArea = serializedObject.FindProperty("m_HidePickArea");

            Holder = serializedObject.FindProperty("Holder");
            PosOffset = serializedObject.FindProperty("PosOffset");
            RotOffset = serializedObject.FindProperty("RotOffset");
            Tags = serializedObject.FindProperty("Tags");
            CollectableReaction = serializedObject.FindProperty("CollectableReaction");

            FocusedItem = serializedObject.FindProperty("focusedItem");
            item = serializedObject.FindProperty("item");
            extraHolders = serializedObject.FindProperty("extraHolders");

            CanPickUp = serializedObject.FindProperty("CanPickUp");
            //CanDrop = serializedObject.FindProperty("CanDrop");


            IgnoreItemPickDelay = serializedObject.FindProperty("IgnoreItemPickDelay");
            IgnoreItemDropDelay = serializedObject.FindProperty("IgnoreItemDropDelay");


            OnPicking = serializedObject.FindProperty("OnPicking");
            OnPicking = serializedObject.FindProperty("OnPicking");
            OnItem = serializedObject.FindProperty("OnItemPicked");
            OnItemDrop = serializedObject.FindProperty("OnItemDrop");
            OnDropping = serializedObject.FindProperty("OnDropping");
            OnFocusedItem = serializedObject.FindProperty("OnFocusedItem");


            Editor_Tabs1 = serializedObject.FindProperty("Editor_Tabs1");
            DebugColor = serializedObject.FindProperty("DebugColor");
            DebugRadius = serializedObject.FindProperty("DebugRadius");
            debug = serializedObject.FindProperty("debug");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            MalbersEditor.DrawDescription("Pick Up Logic for Pickable Items");


            Editor_Tabs1.intValue = GUILayout.Toolbar(Editor_Tabs1.intValue, Tabs1);
            if (Editor_Tabs1.intValue == 0) DrawGeneral();
            else DrawEvents();

            if (debug.boolValue)
            {
                using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.PropertyField(DebugRadius);
                    EditorGUILayout.PropertyField(DebugColor, GUIContent.none, GUILayout.MaxWidth(40));
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawGeneral()
        {
            //MalbersEditor.DrawScript(script);
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new GUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(PickUpArea, new GUIContent("Pick Up Trigger"));
                    MalbersEditor.DrawDebugIcon(debug);
                }

                EditorGUILayout.PropertyField(Layer);
                EditorGUILayout.PropertyField(triggerInteraction);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(Tags);
                EditorGUI.indentLevel--;
                EditorGUILayout.PropertyField(m_HidePickArea, new GUIContent("Hide Trigger"));

                EditorGUILayout.PropertyField(IgnoreItemPickDelay);
                EditorGUILayout.PropertyField(IgnoreItemDropDelay);
            }


            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(Holder, new GUIContent("Default Holder"));
                if (Holder.objectReferenceValue)
                {
                    EditorGUILayout.LabelField("Offsets", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(PosOffset, new GUIContent("Position", "Position Local Offset to parent the item to the holder"));
                    EditorGUILayout.PropertyField(RotOffset, new GUIContent("Rotation", "Rotation Local Offset to parent the item to the holder"));
                }

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(extraHolders, true);
                EditorGUI.indentLevel--;
            }

            if (Application.isPlaying)
            {
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.ObjectField("Picked Item", M.Item?.gameObject, typeof(GameObject), false);
                    using (new EditorGUI.DisabledGroupScope(true))
                        EditorGUILayout.ObjectField("Focused Item", M.FocusedItem?.gameObject, typeof(GameObject), false);

                    Repaint();
                }
            }

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(CollectableReaction);
            }

        }

        private void DrawEvents()
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(CanPickUp, new GUIContent("On Can Pick Item"));
                EditorGUILayout.PropertyField(OnFocusedItem, new GUIContent("On Item Focused"));
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(OnItem, new GUIContent("On Item Picked"));
                EditorGUILayout.PropertyField(OnItemDrop, new GUIContent("On Item Dropped"));
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(OnPicking);
                EditorGUILayout.PropertyField(OnDropping);
            }

        }
    }
#endif
    #endregion
}