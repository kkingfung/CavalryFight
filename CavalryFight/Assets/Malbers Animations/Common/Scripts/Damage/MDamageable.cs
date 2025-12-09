using MalbersAnimations.Reactions;
using MalbersAnimations.Events;
using MalbersAnimations.Scriptables;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MalbersAnimations
{
    [DisallowMultipleComponent]
    /// <summary> Damager Receiver</summary>
    [AddComponentMenu("Malbers/Damage/MDamageable")]
    [HelpURL("https://malbersanimations.gitbook.io/animal-controller/secondary-components/mdamageable")]
    public class MDamageable : MonoBehaviour, IMDamage
    {
        [Tooltip("Animal Reaction to apply when the damage is done")]
        public Component character;

        [Tooltip("Animal Reaction to apply when the damage is done")]
        public Reaction2 damageReaction;

        [Tooltip("Animal Reaction when it receives a critical damage")]
        public Reaction2 criticalReaction;

        [Tooltip("Reaction sent to the Damager if it hits this Damageable")]
        public Reaction2 damagerReaction;

        [Tooltip("Type of surface the Damageable is. (Flesh, Metal, Wood,etc)")]
        public SurfaceID surface;

        public Transform Transform => transform;

        [Tooltip("The Damageable will ignore the Reaction coming from the Damager. Use this when this Damager Needs to have the Default Reaction")]
        [SerializeField] private BoolReference ignoreDamagerReaction = new();

        [Tooltip("Stats component to apply the Damage")]
        public Stats stats;

        [Tooltip("Multiplier for the Stat modifier Value. Use this to increase or decrease the final value of the Stat")]
        public FloatReference multiplier = new(1);

        [Tooltip("When Enabled the animal will rotate towards the Damage direction")]
        public BoolReference AlignToDamage = new();

        [Tooltip("Only Align to Damage when Movement is Not Detected")]
        public BoolReference OnlyOnMovementZero = new(true);

        [Tooltip("Time to align to the damage direction")]
        public FloatReference AlignTime = new(0.25f);
        [Tooltip("Alignment curve")]
        public AnimationCurve AlignCurve = new(MTools.DefaultCurve);

        [Tooltip("Point Forward to align the animal to the Damage, It will rotate around this point")]
        public FloatReference AlignOffset = new();

        public MDamageable Root;
        public DamagerEvents events;

        public Vector3 HitDirection { get; set; }
        public Vector3 HitPosition { get; set; }
        public GameObject Damager { get; set; }
        public Collider HitCollider { get; set; }
        public ForceMode LastForceMode { get; set; }
        public SurfaceID Surface => CurrentProfile.surface;
        public GameObject Damagee => gameObject;

        public bool IgnoreDamagerReaction { get => ignoreDamagerReaction; set => ignoreDamagerReaction.Value = value; }

        public DamageData LastDamage;

        [Tooltip("Elements that affect the MDamageable")]
        public List<ElementMultiplier> elements = new();

        public MDamageableProfile Default;
        public MDamageableProfile CurrentProfile;

        [Tooltip("The Damageable can Change profiles to Change the way the Animal React to the Damage")]
        public List<MDamageableProfile> profiles = new();

        [HideInInspector] public int Editor_Tabs1;

        private ICharacterMove characterMove;

        protected void Start()
        {
            if (stats != null)
            {
                if (character == null && damageReaction.IsValid)
                {
                    character = stats.GetComponent(damageReaction.ReactionType); //Find the character where the Stats are
                }
                else
                {
                    character = stats.transform;
                }
            }

            //Store the default values
            Default = new("Default", surface,
                damageReaction, criticalReaction, damagerReaction, ignoreDamagerReaction,
                multiplier, AlignToDamage, elements)
            {
                OnProfileEnter = events.OnProfileDefaultEnter,
                OnProfileExit = events.OnProfileDefaultExit
            };
            Default.OnProfileEnter?.Invoke();

            CurrentProfile = Default;

            profiles ??= new();

            //Check if we have a Character Move in the Animal
            if (character != null)
                characterMove = character.FindInterface<ICharacterMove>();
        }

        protected void OnDisable()
        {
            StopAllCoroutines();
        }


        /// <summary> Restore the Default Damageable profile </summary>
        public virtual void Profile_Restore()
        {
            CurrentProfile.OnProfileExit?.Invoke();
            CurrentProfile = Default;
            CurrentProfile.OnProfileEnter?.Invoke();
        }

        //public virtual MDamageableProfile GetCurrentProfile() => CurrentProfile;


        public virtual void Profile_Set(string name)
        {
            if (string.IsNullOrEmpty(name) || string.Equals(name, "default", System.StringComparison.OrdinalIgnoreCase))
            {
                Profile_Restore();
            }
            else
            {
                var index = profiles.FindIndex(p => p.name == name);

                if (index != -1)
                {
                    CurrentProfile.OnProfileExit?.Invoke();
                    CurrentProfile = profiles[index];
                    CurrentProfile.OnProfileEnter?.Invoke();
                }
            }
        }



        //-*********************************************************************--
        /// <summary>  Main Receive Damage Method!!! </summary>
        /// <param name="Direction">The Direction the Damage is coming from</param>
        /// <param name="Position">The position of the hit</param>
        /// <param name="Damager">Game Object doing the Damage</param>
        /// <param name="damage">Stat Modifier containing the Stat ID, what to modify and the Value to modify</param>
        /// <param name="isCritical">is the Damage Critical?</param>
        /// <param name="react">Does the Damage that is coming has a Custom Reaction? </param>
        /// <param name="customReaction">The Attacker Brings a custom Reaction to override the Default one</param>
        /// <param name="pureDamage">Pure damage means that the multipliers wont be applied</param>
        /// <param name="element"></param>
        public virtual void ReceiveDamage(Vector3 Direction, Vector3 Position, GameObject Damager, StatModifier damage,
            bool isCritical, Reaction2 customReaction, bool pureDamage, StatElement element, bool missed)
        {
            if (!enabled) return;       //This makes the Animal Immortal.
            HitDirection = Direction;   //Store the Last Direction
            HitPosition = Position;   //Store the Last Position

            var stat = stats.Stat_Get(damage.ID);

            var DamageReaction = ReactionLogic(isCritical, customReaction);

            ElementMultiplier statElement = new(element, 1);

            //Apply the Element Multiplier
            if (element != null && CurrentProfile.elements.Count > 0)
            {
                statElement = CurrentProfile.elements.Find(x => element.ID == x.element.ID);

                if (statElement.multiplier != null)
                {
                    damage.Value *= statElement.multiplier;
                    events.OnElementDamage.Invoke(statElement.element.ID);
                    if (Root) Root.events.OnElementDamage.Invoke(statElement.element.ID);
                }
            }

            SetDamageable(Direction, Damager);
            if (Root) Root.SetDamageable(Direction, Damager);                     //Send the Direction and Damager to the Root 

            if (!pureDamage)
                damage.Value *= CurrentProfile.multiplier;               //Apply to the Stat modifier a new Modification


            //Store the last damage applied to the Damageable
            LastDamage = new DamageData(Direction, HitPosition, Damager, gameObject, damage, isCritical, statElement, DamageReaction, missed);

            if (Root) Root.LastDamage = LastDamage;


            if (isCritical)
            {
                events.OnCriticalDamage.Invoke();
                if (Root) Root.events.OnCriticalDamage.Invoke();
            }

            events.OnReceivingDamage.Invoke(damage.Value);
            events.OnDamager.Invoke(Damager);

            //Send the Events on the Root
            if (Root)
            {
                Root.events.OnReceivingDamage.Invoke(damage.Value);
                Root.events.OnDamager.Invoke(Damager);
            }

            damage.ModifyStat(stats);

            //Invoke if the Stat is empty..
            if (stat.IsEmpty) { events.OnStatEmpty.Invoke(stat.ID); }

            AlignmentLogic(Damager);
        }

        protected virtual void AlignmentLogic(GameObject Damager)
        {
            if (CurrentProfile.AlignToDamage.Value)
            {
                if (OnlyOnMovementZero.Value && characterMove != null && characterMove.MovementDetected) return; //Do not Align if the Animal is moving

                AlignToDamageDirection(Damager);
            }
        }

        protected virtual Reaction2 ReactionLogic(bool isCritical, Reaction2 customReaction)
        {
            if (Damager)
                CurrentProfile.damagerReaction.React(Damager); //React to the Damager if it has a Reaction

            if (isCritical)
            {
                CurrentProfile.criticalReaction.React(character);     //if the damage is Critical then react with the critical reaction instead

                return CurrentProfile.criticalReaction;
            }
            else
            {
                if (customReaction.IsValid || IgnoreDamagerReaction)
                {
                    customReaction.React(character); //Custom Reaction from the Damager
                    return customReaction;
                }
                else
                {
                    CurrentProfile.damageReaction.React(character);    //React Default
                    return CurrentProfile.damageReaction;
                }
            }
        }

        protected virtual void AlignToDamageDirection(GameObject DirectionGameObj)
        {
            if (isActiveAndEnabled && DirectionGameObj != null)
            {
                StopAllCoroutines();
                StartCoroutine(MTools.AlignLookAtTransform(
                    character.transform, DirectionGameObj.transform.position, AlignOffset, AlignTime.Value,
                    stats.transform.localScale.y, AlignCurve));
            }
        }

        /// <summary>  Receive Damage from external sources simplified </summary>
        /// <param name="stat"> What stat will be modified</param>
        /// <param name="amount"> value to subtract to the stat</param>
        public virtual void ReceiveDamage(StatID stat, float amount)
        {
            var modifier = new StatModifier() { ID = stat, modify = StatOption.SubstractValue, Value = amount };
            ReceiveDamage(Vector3.forward, transform.position, null, modifier, false, null, false, null, false);
        }

        /// <summary>  Receive Damage from external sources simplified </summary>
        /// <param name="stat"> What stat will be modified</param>
        /// <param name="amount"> value to subtract to the stat</param>
        public virtual void ReceiveDamage(StatID stat, float amount, StatOption modifyStat = StatOption.SubstractValue)
        {
            var modifier = new StatModifier() { ID = stat, modify = modifyStat, Value = amount };
            ReceiveDamage(Vector3.forward, transform.position, null, modifier, false, null, false, null, false);
        }


        /// <summary>  Receive Damage from external sources simplified </summary>
        /// <param name="Direction">Where the Damage is coming from</param>
        /// <param name="Damager">Who is doing the Damage</param>
        /// <param name="modifier">What Stat will be modified</param>
        /// <param name="modifyStat">Type of modification applied to the stat</param>
        /// <param name="isCritical">is the Damage Critical?</param>
        /// <param name="react">Does Apply the Default Reaction?</param>
        /// <param name="pureDamage">if is pure Damage, do not apply the default multiplier</param>
        /// <param name="stat"> What stat will be modified</param>
        /// <param name="amount"> value to subtract to the stat</param>
        public virtual void ReceiveDamage(Vector3 Direction, GameObject Damager, StatID stat, float amount, StatOption modifyStat = StatOption.SubstractValue,
             bool isCritical = false, Reaction customReaction = null, bool pureDamage = false, StatElement element = null)
        {
            var modifier = new StatModifier() { ID = stat, modify = modifyStat, Value = amount };
            ReceiveDamage(Direction, transform.position, Damager, modifier, isCritical, customReaction, pureDamage, element, false);
        }


        /// <summary>  Receive Damage from external sources simplified </summary>
        /// <param name="Direction">Where the Damage is coming from</param>
        /// <param name="Damager">Who is doing the Damage</param> 
        /// <param name="isCritical">is the Damage Critical?</param> 
        /// <param name="pureDamage">if is pure Damage, do not apply the default multiplier</param>
        /// <param name="stat"> What stat will be modified</param>
        /// <param name="amount"> value to subtract to the stat</param>
        public virtual void ReceiveDamage(Vector3 Direction, GameObject Damager, StatID stat,
            float amount, bool isCritical = false, Reaction customReaction = null, bool pureDamage = false)
        {
            var modifier = new StatModifier() { ID = stat, modify = StatOption.SubstractValue, Value = amount };
            ReceiveDamage(Direction, transform.position, Damager, modifier, isCritical, customReaction, pureDamage, null, false);
        }


        /// <summary>  Receive Damage from external sources simplified </summary>
        /// <param name="Direction">Where the Damage is coming from</param>
        /// <param name="Damager">Who is doing the Damage</param>
        /// <param name="modifier">What Stat will be modified</param>
        /// <param name="isCritical">is the Damage Critical?</param>
        /// <param name="react">Does Apply the Default Reaction?</param>
        /// <param name="pureDamage">if is pure Damage, do not apply the default multiplier</param>
        /// <param name="stat"> What stat will be modified</param>
        /// <param name="amount"> value to subtract to the stat</param>
        public virtual void ReceiveDamage(Vector3 Direction, GameObject Damager, StatModifier damage,
        bool isCritical, Reaction customReaction, bool pureDamage) =>
         ReceiveDamage(Direction, transform.position, Damager, damage, isCritical, customReaction, pureDamage, null, false);

        /// <summary>  Fill the Local Values of the MDamageable  </summary>
        internal void SetDamageable(Vector3 Direction, GameObject Damager)
        {
            HitDirection = Direction;
            this.Damager = Damager;
        }

        [System.Serializable]
        //CustomPatch: TODO: architecture improvement: this can easily be a struct instead of class (leads to more uniform memory allocation instead of more random heap allocations)
        public class DamagerEvents
        {
            public FloatEvent OnReceivingDamage = new();
            public UnityEvent OnCriticalDamage = new();
            public GameObjectEvent OnDamager = new();
            public IntEvent OnElementDamage = new();
            public IntEvent OnStatEmpty = new();
            public UnityEvent OnProfileDefaultEnter = new();
            public UnityEvent OnProfileDefaultExit = new();
        }




#if UNITY_EDITOR
        private void Reset()
        {
            // reaction = MTools.GetInstance<ModeReaction>("Damaged");

            damageReaction = new(new ModeReaction() { ID = MTools.GetInstance<ModeID>("Damage"), });
            criticalReaction = new(new ModeReaction() { ID = MTools.GetInstance<ModeID>("Damage"), });

            surface = MTools.GetInstance<SurfaceID>("Flesh");

            stats = this.FindComponent<Stats>();
            if (transform.parent != null) Root = transform.root.GetComponentInParent<MDamageable>();     //Check if there's a Damageable on the Root
            if (Root == this) Root = null;


            //Add Stats if it not exist
            if (stats == null) stats = gameObject.AddComponent<Stats>();

            profiles = new List<MDamageableProfile>();
        }

        [SerializeField, HideInInspector] private bool FirstTime = false; //Debug Mode to show the Damageable in the Inspector


        private void OnValidate()
        {
            if (FirstTime) return; //Only Validate once

            if (!damageReaction.IsValid)
            {
                damageReaction = new(new ModeReaction() { ID = MTools.GetInstance<ModeID>("Damage"), });
                criticalReaction = new(new ModeReaction() { ID = MTools.GetInstance<ModeID>("Damage"), });
                MTools.SetDirty(this);
            }
            FirstTime = true; //Set the First Time to True so it doesn't validate again
        }


        private void OnDrawGizmosSelected()
        {
            if (AlignOffset != 0)
            {
                Vector3 Offset = transform.position + AlignOffset * transform.localScale.y * transform.forward; //Use Offset

                Gizmos.color = Color.white;
                Gizmos.DrawSphere(Offset, 0.075f);
            }
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(MDamageable))]
    public class MDamageableEditor : Editor
    {
        SerializedProperty reaction, damagerReaction, criticalReaction, surface,
            stats,
            multiplier, ignoreDamagerReaction, events, Root, OnlyOnMovementZero,
            // OnProfileDefaultEnter, OnProfileDefaultExit,
            AlignTime, AlignCurve, AlignToDamage, AlignOffset,
            Editor_Tabs1, elements, profiles;
        MDamageable M;

        protected string[] Tabs1 = new string[] { "General", "Profiles", "Events" };


        GUIContent plus;

        private void OnEnable()
        {
            M = (MDamageable)target;

            reaction = serializedObject.FindProperty("damageReaction");
            criticalReaction = serializedObject.FindProperty("criticalReaction");
            damagerReaction = serializedObject.FindProperty("damagerReaction");
            stats = serializedObject.FindProperty("stats");
            multiplier = serializedObject.FindProperty("multiplier");

            events = serializedObject.FindProperty("events");
            //OnProfileDefaultEnter = serializedObject.FindProperty("OnProfileDefaultEnter");
            //OnProfileDefaultExit = serializedObject.FindProperty("OnProfileDefaultExit");


            Root = serializedObject.FindProperty("Root");
            OnlyOnMovementZero = serializedObject.FindProperty("OnlyOnMovementZero");

            AlignToDamage = serializedObject.FindProperty("AlignToDamage");
            AlignCurve = serializedObject.FindProperty("AlignCurve");
            AlignTime = serializedObject.FindProperty("AlignTime");
            AlignOffset = serializedObject.FindProperty("AlignOffset");


            Editor_Tabs1 = serializedObject.FindProperty("Editor_Tabs1");
            elements = serializedObject.FindProperty("elements");
            profiles = serializedObject.FindProperty("profiles");
            surface = serializedObject.FindProperty("surface");
            ignoreDamagerReaction = serializedObject.FindProperty("ignoreDamagerReaction");

            if (plus == null) plus = UnityEditor.EditorGUIUtility.IconContent("d_Toolbar Plus");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            MalbersEditor.DrawDescription("Connects the Damage received to the Stat and Animal components");


            Editor_Tabs1.intValue = GUILayout.Toolbar(Editor_Tabs1.intValue, Tabs1);

            switch (Editor_Tabs1.intValue)
            {
                case 0: DrawGeneral(); break;
                case 1: DrawProfiles(); break;
                case 2: DrawEvents(); break;
                default: break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawProfiles()
        {
            EditorGUILayout.PropertyField(profiles, true);
        }

        private void DrawGeneral()
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(stats);

                if (M.transform.parent != null)
                    EditorGUILayout.PropertyField(Root);

                EditorGUILayout.PropertyField(surface);
            }

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                stats.isExpanded = MalbersEditor.Foldout(stats.isExpanded, "Reactions");

                if (stats.isExpanded)
                {
                    EditorGUILayout.PropertyField(reaction);
                    EditorGUILayout.PropertyField(criticalReaction);
                    EditorGUILayout.PropertyField(damagerReaction);
                    EditorGUILayout.PropertyField(ignoreDamagerReaction);
                }
            }

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                multiplier.isExpanded = MalbersEditor.Foldout(multiplier.isExpanded, "Multipliers");

                if (multiplier.isExpanded)
                {
                    EditorGUILayout.PropertyField(multiplier);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(elements);
                    EditorGUI.indentLevel--;
                }
            }

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                AlignToDamage.isExpanded = MalbersEditor.Foldout(AlignToDamage.isExpanded, "Alignment");

                if (AlignToDamage.isExpanded)

                {
                    EditorGUILayout.PropertyField(AlignToDamage);

                    if (M.AlignToDamage.Value)
                    {
                        EditorGUILayout.PropertyField(OnlyOnMovementZero);

                        using (new GUILayout.HorizontalScope())
                        {
                            EditorGUILayout.PropertyField(AlignTime);
                            EditorGUILayout.PropertyField(AlignCurve, GUIContent.none, GUILayout.MaxWidth(75));
                        }
                        EditorGUILayout.PropertyField(AlignOffset);
                    }
                }
            }
        }

        private void DrawEvents()
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(events, true);
                EditorGUI.indentLevel--;
            }
        }
    }
#endif
}