using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MalbersAnimations.Reactions;
using UnityEngine.Serialization;
using Unity.AI.Navigation;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MalbersAnimations.Controller.AI
{
    [AddComponentMenu("Malbers/AI/AI Animal Link")]
    public class MAIAnimalLink : MonoBehaviour
    {
        public static List<MAIAnimalLink> OffMeshLinks;
        public bool BiDirectional = true;

        public Reaction2 StartReaction;
        public Reaction2 EndReaction;
        public Color DebugColor = Color.yellow;

        [Min(0)] public float StoppingDistance = 1f;
        [Min(0)] public float SlowingDistance = 1f;
        [Min(0)] public float SlowingLimit = 0.3f;

        [Tooltip("Align the Animal to the OffMesh Link when the START point is the closest")]
        public bool AlignToLinkFromStart = true;
        [Tooltip("Align the Animal to the OffMesh Link when the END point  is the closest")]
        public bool AlignToLinkFromEnd = true;

        [Min(0)] public float AlignTime = 0.2f;
        [Tooltip("OffMesh Start Link Transform For aligning when the Character is near the start point")]
        [FormerlySerializedAs("start")]
        [RequiredField] public Transform startAlignPoint;

        [Tooltip("OffMesh End Link Transform")]
        [FormerlySerializedAs("end")]
        [RequiredField] public Transform endAlignPoint;

        [Tooltip("Width of the OffMesh Link (Make it match with Nav Mesh Link)")]
        public float width = 0;


        [Tooltip("Orient the character to the Align Point. If False it will only reposition the character")]
        public bool AlignRotation = true; //If the Animal should rotate to the Direction of the OffMesh Link

        // public enum MovementType { Direction, UseVerticalDirection, UseAxis, LinkDirection }


        [Tooltip("Use Direction for setting the Direction from Start Point to End Point. Disable for Climbing, Ladders")]
        public bool UseDirection = true;

        [Tooltip("If using Direction, the Animal will move using the Direction Vector from Start to End point. In case of Going Upwards on states like Climb, Ladder, Run Vertical")]
        public bool SwapYZ; //Use for climbing

        [Tooltip("Calculate the direction from the Start to End Point")]
        public bool LinkDirection = false;

        [Tooltip("Input Axis to move the Animal when going from Start to the End point. (X: Means Horizontal, Y: Up Down, Z: Forward) Use for climbing")]
        public Vector3 InputAxisFromStart = Vector3.forward;
        [Tooltip("Input Axis to move the Animal when going from End to the Start point. (X: Means Horizontal, Y: Up Down, Z: Forward) Use for climbing")]
        public Vector3 InputAxisFromEnd = Vector3.forward;

        public bool debug = true;

        [SerializeField] private NavMeshLink navMeshLink;

        protected virtual void OnEnable()
        {
            OffMeshLinks ??= new List<MAIAnimalLink>();
            OffMeshLinks.Add(this);

            if (TryGetComponent<Unity.AI.Navigation.NavMeshLink>(out navMeshLink))
            {
                navMeshLink.width = width;
            }
        }

        protected virtual void OnDisable()
        {
            OffMeshLinks.Remove(this);
        }

        public virtual void Execute(IAIControl ai, MAnimal animal, Vector3 StartPoint, Vector3 EndPoint)
        {
            animal.StartCoroutine(OffMeshMove(ai, animal, StartPoint, EndPoint));
        }

        /// <summary>  AI START Pathfinding Coroutine </summary>
        public IEnumerator Coroutine_Execute(IAIControl ai, MAnimal animal, Vector3 StartPoint, Vector3 EndPoint)
        {
            yield return OffMeshMove(ai, animal, StartPoint, EndPoint);
        }

        //FIX ALIGNMENT!!!!!
        private IEnumerator OffMeshMove(IAIControl ai, MAnimal animal, Vector3 StartPoint, Vector3 EndPoint)
        {
            if (!startAlignPoint || !endAlignPoint)
            {
                Debug.LogError($"<B>OffMeshLink - [{name}]</B> -> Please assign the Start and End Align Points", this);
                yield break;
            }

            Vector3 nearPoint;
            Quaternion nearRotation;
            bool FromStart;

            var dir = (endAlignPoint.position - startAlignPoint.position).normalized;
            var Up = Vector3.Cross(dir, Vector3.Cross(dir, Vector3.up)).normalized;
            var right = 0.5f * width * Vector3.Cross(dir, Up);

            var ClosestStart = MTools.ClosestPointOnLine(animal.Position, startAlignPoint.position - right, startAlignPoint.position + right);
            var ClosestEnd = MTools.ClosestPointOnLine(animal.Position, endAlignPoint.position - right, endAlignPoint.position + right);

            var DistStart = Vector3.Distance(animal.Position, ClosestStart);
            var DistEnd = Vector3.Distance(animal.Position, ClosestEnd);

            if (DistStart < DistEnd)
            {
                nearPoint = ClosestStart;
                nearRotation = startAlignPoint.rotation;
                FromStart = true;
            }
            else
            {
                FromStart = false;
                nearPoint = ClosestEnd;
                nearRotation = endAlignPoint.rotation;
            }

            Debbuging($" Nearest Align Point: {nearPoint}");

            if (startAlignPoint && AlignToLinkFromStart && FromStart || endAlignPoint && AlignToLinkFromEnd && !FromStart)
            {
                Debbuging($"Start alignment with [{animal.name}]");

                nearRotation = AlignRotation ? animal.Rotation : nearRotation;

                yield return MTools.AlignTransform(animal.transform, nearPoint, nearRotation, AlignTime);
                Debbuging($"Finish alignment with [{animal.name}]");
            }
            else if (AlignRotation)
            {
                yield return MTools.AlignTransform_Rotation(animal.transform, nearRotation, AlignTime);
            }

            // MDebug.DrawRay(animal.Position, StartPoint.DirectionTo(EndPoint), Color.cyan, 2);

            StartReaction.React(animal);
            Debbuging($"Start Offmesh Coroutine");

            ai.InOffMeshLink = true;
            // ai.AIDirection = StartPoint.DirectionTo(EndPoint);

            RemainingDistance = float.MaxValue;

            // var axis = animal.transform.NearestPoint(start.position, end.position) == start.position ? StartAxis : EndAxis;

            while (RemainingDistance >= StoppingDistance && ai.InOffMeshLink)
            {
                var AIDirection = StartPoint.DirectionTo(EndPoint);

                if (UseDirection) //If its using Direction vector to move
                {
                    if (LinkDirection)
                    {
                        AIDirection = FromStart ? startAlignPoint.forward : endAlignPoint.forward;
                        // AIDirection = ClosestStart.DirectionTo(ClosestEnd);
                        AIDirection = Vector3.ProjectOnPlane(AIDirection, animal.UpVector);
                    }

                    if (SwapYZ) //Use for climbing(Meaning go All Horizontal, or Forward movement)
                    {
                        AIDirection = transform.InverseTransformDirection(AIDirection); //Convert to UP Down like Climb
                        AIDirection.z = AIDirection.y;
                        AIDirection.y = 0;
                        animal.SetInputAxis(AIDirection * SlowMultiplier);
                        animal.UsingMoveWithDirection = false;
                    }
                    else
                    {
                        ai.AIDirection = (AIDirection);
                        animal.Move(AIDirection * SlowMultiplier);
                    }
                }
                else //If its using Input Axis to to move (Meaning go All Horizontal, or Forward movement)
                {
                    var StartInput = FromStart ? InputAxisFromStart : InputAxisFromEnd; // Determine which Input Axis to use
                    animal.SetInputAxis(StartInput * SlowMultiplier);
                    animal.UsingMoveWithDirection = false;
                }
                MDebug.Draw_Arrow(animal.Position, AIDirection, Color.green);

                MDebug.DrawWireSphere(EndPoint, DebugColor, StoppingDistance);
                MDebug.DrawWireSphere(EndPoint, Color.cyan, SlowingDistance);


                RemainingDistance = Vector3.Distance(animal.transform.position, EndPoint);
                yield return null;
            }

            if (ai.InOffMeshLink)
                EndReaction.React(animal); //Execute the End Reaction only if the Animal has not interrupted the Offmesh Link

            Debbuging($"End Offmesh Coroutine");
            ai.CompleteOffMeshLink();
        }


        public float SlowMultiplier
        {
            get
            {
                var result = 1f;
                if (SlowingDistance > StoppingDistance && RemainingDistance < SlowingDistance)
                    result = Mathf.Max(RemainingDistance / SlowingDistance, SlowingLimit);
                return result;
            }
        }

        public float RemainingDistance { get; private set; }

        private void Debbuging(string valu)
        {
            if (debug) Debug.Log($"<B>OffMeshLink - [{name}]</B> -> {valu}", this);
        }



#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = DebugColor;
            Handles.color = DebugColor;

            var AxisSize = transform.lossyScale.y;

            if (startAlignPoint)
            {
                Gizmos.DrawSphere(startAlignPoint.position, 0.2f * AxisSize);
                // Handles.ArrowHandleCap(0, startAlignPoint.position, startAlignPoint.rotation, AxisSize, EventType.Repaint);
            }
            if (endAlignPoint)
            {
                Gizmos.DrawSphere(endAlignPoint.position, 0.2f * AxisSize);
                // Handles.ArrowHandleCap(0, endAlignPoint.position, endAlignPoint.rotation, AxisSize, EventType.Repaint);

            }
            if (startAlignPoint && endAlignPoint)
            {
                if (width > 0)
                {
                    var dir = (endAlignPoint.position - startAlignPoint.position).normalized;
                    var Up = Vector3.Cross(dir, Vector3.Cross(dir, Vector3.up)).normalized;
                    var right = 0.5f * width * Vector3.Cross(dir, Up);
                    Handles.DrawLine(startAlignPoint.position + right, endAlignPoint.position + right, 2);
                    Handles.DrawLine(startAlignPoint.position - right, endAlignPoint.position - right, 2);
                    Handles.DrawLine(startAlignPoint.position + right, startAlignPoint.position - right, 2);
                    Handles.DrawLine(endAlignPoint.position + right, endAlignPoint.position - right, 2);

                    Handles.ArrowHandleCap(0, endAlignPoint.position + right, endAlignPoint.rotation, AxisSize, EventType.Repaint);
                    Handles.ArrowHandleCap(0, endAlignPoint.position - right, endAlignPoint.rotation, AxisSize, EventType.Repaint);

                    Handles.ArrowHandleCap(0, startAlignPoint.position + right, startAlignPoint.rotation, AxisSize, EventType.Repaint);
                    Handles.ArrowHandleCap(0, startAlignPoint.position - right, startAlignPoint.rotation, AxisSize, EventType.Repaint);
                }
                else
                {
                    Handles.DrawLine(startAlignPoint.position, endAlignPoint.position, 2);
                    Handles.ArrowHandleCap(0, startAlignPoint.position, startAlignPoint.rotation, AxisSize, EventType.Repaint);
                    Handles.ArrowHandleCap(0, endAlignPoint.position, endAlignPoint.rotation, AxisSize, EventType.Repaint);
                }
            }

        }

        private void OnDrawGizmosSelected()
        {
            if (startAlignPoint)
            {
                Gizmos.color = DebugColor;
                Gizmos.DrawWireSphere(startAlignPoint.position, 0.2f * transform.lossyScale.y);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(startAlignPoint.position, StoppingDistance);
                if (StoppingDistance < SlowingDistance)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(startAlignPoint.position, SlowingDistance);
                }
            }
            if (endAlignPoint)
            {
                Gizmos.color = DebugColor;
                Gizmos.DrawWireSphere(endAlignPoint.position, 0.2f * transform.lossyScale.y);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(endAlignPoint.position, StoppingDistance);
                if (StoppingDistance < SlowingDistance)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(endAlignPoint.position, SlowingDistance);
                }
            }
        }


        private void Reset()
        {
            navMeshLink = GetComponent<Unity.AI.Navigation.NavMeshLink>();
            width = navMeshLink ? navMeshLink.width : 0;
        }

        private void OnValidate()
        {
            if (navMeshLink == null)
                navMeshLink = GetComponent<Unity.AI.Navigation.NavMeshLink>();

            if (navMeshLink && navMeshLink.width != width)
            {
                navMeshLink.width = width;
            }
        }

#endif


#if UNITY_EDITOR
        [CustomEditor(typeof(MAIAnimalLink)), CanEditMultipleObjects]
        public class MAILinkEditor : Editor
        {
            SerializedProperty StartReaction, EndReaction,
                Start, End, Width,
                DebugColor, UseDirection, AlignRotation,
                  InputAxiStart, InputAxiEnd, SwapYZ, LinkDirection,
                AlignToLinkFromStart, AlignToLinkFromEnd,
                AlignTime, debug,
                StoppingDistance, SlowingLimit, SlowingDistance, BiDirectional;

            MAIAnimalLink M;

            private void OnEnable()
            {
                M = (MAIAnimalLink)target;
                StartReaction = serializedObject.FindProperty("StartReaction");
                debug = serializedObject.FindProperty("debug");
                EndReaction = serializedObject.FindProperty("EndReaction");

                Start = serializedObject.FindProperty(nameof(M.startAlignPoint));
                End = serializedObject.FindProperty(nameof(M.endAlignPoint));
                LinkDirection = serializedObject.FindProperty(nameof(M.LinkDirection));

                AlignRotation = serializedObject.FindProperty(nameof(M.AlignRotation));
                Width = serializedObject.FindProperty(nameof(M.width));

                StoppingDistance = serializedObject.FindProperty("StoppingDistance");
                SlowingLimit = serializedObject.FindProperty("SlowingLimit");
                SlowingDistance = serializedObject.FindProperty("SlowingDistance");
                DebugColor = serializedObject.FindProperty("DebugColor");
                UseDirection = serializedObject.FindProperty("UseDirection");
                SwapYZ = serializedObject.FindProperty(nameof(M.SwapYZ));

                BiDirectional = serializedObject.FindProperty("BiDirectional");

                AlignToLinkFromStart = serializedObject.FindProperty(nameof(M.AlignToLinkFromStart));
                AlignToLinkFromEnd = serializedObject.FindProperty(nameof(M.AlignToLinkFromEnd));

                AlignTime = serializedObject.FindProperty("AlignTime");
                InputAxiStart = serializedObject.FindProperty(nameof(M.InputAxisFromStart));
                InputAxiEnd = serializedObject.FindProperty(nameof(M.InputAxisFromEnd));
            }
            public override void OnInspectorGUI()
            {
                //base.OnInspectorGUI();
                serializedObject.Update();

                MalbersEditor.DrawDescription("Uses Animal reactions to move the Agent when its at a OffMeshLinks");
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(StoppingDistance);
                    EditorGUILayout.PropertyField(DebugColor, GUIContent.none, GUILayout.Width(50));
                    MalbersEditor.DrawDebugIcon(debug);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.PropertyField(SlowingDistance);
                    EditorGUILayout.PropertyField(SlowingLimit);
                    EditorGUILayout.PropertyField(BiDirectional);
                }
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.PropertyField(UseDirection);
                    if (!UseDirection.boolValue)
                    {
                        EditorGUILayout.PropertyField(InputAxiStart);
                        if (BiDirectional.boolValue)
                            EditorGUILayout.PropertyField(InputAxiEnd);
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(LinkDirection);
                        EditorGUILayout.PropertyField(SwapYZ);
                    }
                }

                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.PropertyField(AlignTime);

                    if (AlignTime.floatValue > 0.0f)
                    {
                        EditorGUILayout.PropertyField(Start);
                        EditorGUILayout.PropertyField(End);
                        EditorGUILayout.PropertyField(Width);

                        EditorGUILayout.PropertyField(AlignToLinkFromStart);
                        if (BiDirectional.boolValue)
                            EditorGUILayout.PropertyField(AlignToLinkFromEnd);
                        EditorGUILayout.PropertyField(AlignRotation);
                    }
                }

                MalbersEditor.DrawSplitter();
                EditorGUILayout.PropertyField(StartReaction);
                //EditorGUILayout.Space();
                MalbersEditor.DrawSplitter();
                EditorGUILayout.PropertyField(EndReaction);
                serializedObject.ApplyModifiedProperties();
            }

            void OnSceneGUI()
            {
                if (!debug.boolValue) return;

                using (var cc = new EditorGUI.ChangeCheckScope())
                {
                    if (M.startAlignPoint && M.startAlignPoint != M.transform)
                    {
                        var start = M.startAlignPoint.position;
                        start = Handles.PositionHandle(start, M.transform.rotation);

                        if (cc.changed)
                        {
                            Undo.RecordObject(M.startAlignPoint, "Move Start AI Link");
                            M.startAlignPoint.position = start;
                        }
                    }
                }

                using (var cc = new EditorGUI.ChangeCheckScope())
                {
                    if (M.endAlignPoint && M.endAlignPoint != M.transform)
                    {
                        var end = M.endAlignPoint.position;
                        end = Handles.PositionHandle(end, M.transform.rotation);

                        if (cc.changed)
                        {
                            Undo.RecordObject(M.endAlignPoint, "Move End AI Link");
                            M.endAlignPoint.position = end;
                        }
                    }
                }
            }
        }
#endif
    }
}