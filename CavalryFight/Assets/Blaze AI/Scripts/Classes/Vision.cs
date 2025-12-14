using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BlazeAISpace
{
    [System.Serializable]
    public class Vision
    {
        #region PROPERTIES

        [Tooltip("You may want this AI only as a walking NPC or something similar with no need for the overhead of detecting enemies so disabling this will improve performance. This needs to be enabled if you want to detect enemies.")]
        public bool visionActive = true;

        [Tooltip("This is where the vision ray will start from. Place on the eyes of the AI. Enable [Show Vision] below to draw the vision cone in the scene view for easy placement. If [Max Sight Level] is set to 0. This Y point will be the maximum Y position the AI can detect and anything bigger will not be detected or seen.")]
        public Vector3 visionPosition = new Vector3(0, 1, 0);
        [Min(0f), Tooltip("The maximum Y position the AI can detect. If any target's Y position is bigger than this, it won't be detected or seen. Enable [Show Max Sight Level] below to show as a green rectangle in the scene view.")]
        public float maxSightLevel;
        [Tooltip("If enabled, the AI will check the target's height and if it goes past the max sight level the AI will lose track of it's target. This means the target can escape by going higher like flying. If disabled, the AI won't lose track of it's targets because they're high above but enemies will have to escape it's vision zone.")]
        public bool checkTargetHeight = false;
        [Tooltip("If enabled, then the [Min Sight Level] will act as the minimum sight level and anything lower will not be detected. If disabled, no minimum detection level will be applied and anything below the Vision Position Y point (including max sight level) will be detected. More info in the docs, Vision section.")]
        public bool useMinLevel = true;
        [Tooltip("The minimum Y position the AI can detect. If any target's Y position is lower than this, it won't be detected or seen. Enable [Show Min Sight Level] below to show as a red rectangle in the scene view.")]
        public float minSightLevel = -1;

        public normalVision visionDuringNormalState = new normalVision(90f, 10f);
        public alertVision visionDuringAlertState = new alertVision(100f, 15f);
        public attackVision visionDuringAttackState = new attackVision(360f, 20f);

        [Tooltip("OPTIONAL: add the head object, this will be used for updating both the rotation of the vision according to the head and the sight level automatically. If empty, the rotation will be according to the body, projecting forwards.")]
        public Transform head;

        [Tooltip("Show the vision cone of normal state in scene view for easier debugging.")]
        public bool showNormalVision = true;
        public Color normalVisionColor = new Color(1, 1, 1, 0.6f);
        [Tooltip("Show the vision cone of alert state in scene view for easier debugging.")]
        public bool showAlertVision = true;
        public Color alertVisionColor = new Color(1, 1, 0, 0.3f);
        [Tooltip("Show the vision cone of attack state in scene view for easier debugging.")]
        public bool showAttackVision;
        public Color attackVisionColor = new Color(1, 0, 0, 0.3f);
        [Tooltip("Shows the maximum sight level as a green rectangle.")]
        public bool showMaxSightLevel = true;
        [Tooltip("Shows the minimum sight level as a red rectangle.")]
        public bool showMinSightLevel = true;

        [Tooltip("Add all the layers you want to detect around the world. Any layer not added will be seen through. Recommended not to add other Blaze agents layers in order for them not to block the view from your target. Also no need to set the enemy layers here.")]
        public LayerMask layersToDetect = Physics.AllLayers;
        [Tooltip("Set the layers of the hostiles and alerts. Hostiles are the enemies you want to attack. Alerts are objects when seen will turn the AI to alert state.")]
        public LayerMask hostileAndAlertLayers;
        [Tooltip("The tag names of hostile gameobjects (player) that this agent should attack. This needs to be set in order to identify enemies.")]
        public string[] hostileTags;
        [Tooltip("Optional: Tags that will make the agent become in alert state such as tags of dead bodies or an open door.")]
        public AlertTags[] alertTags;

        public UnityEvent enemyEnterEvent;
        public UnityEvent enemyLeaveEvent;

        [Range(0, 10), Tooltip("Set how many frames you want the vision system to skip. The lower the number, the more accurate but may be expensive. The higher the number, the less accurate but better for performance. The optimal value is 1-2. Increasing this too much will slow down AI reactions. Increase this value on slow or background npcs.")]
        public int visionFrameSkipping = 2;
        [Tooltip("Using multi-rays gives better accuracy and takes more of performance. It fires multiple rays to many corners of all colliders of the potential target and decides from the results whether it's enough to be considered 'seen'. While using single ray (setting this to off) is better for performance and fires a single raycast to the center of the main collider. For example if only the head of your player is exposed while the rest of the body is hidden behind a tree, the AI will not react. As the center of the player is hidden by the tree. Take note: multi rays may cause issues in VR so it's best to disable this if you're using VR.")]
        public bool multiRayVision = true;
        
        [Tooltip("Instead of detecting the enemy immediately, the vision meter will increment until it's complete then the enemy gets detected.")]
        public bool useVisionMeter;
        public VisionMeterSpeeds visionMeterSpeeds = new VisionMeterSpeeds(5, 10, 3);
        
        [Tooltip("If enabled, the AI will turn to distracted state if the vision meter reaches a certain value.")]
        public bool distractByVMValue;
        [Tooltip("The AI will turn to distracted state if the vision meter reaches this value.")]
        [Range(0.1f, 1f)] public float distractIfVMReaches = 0.6f;

        [Tooltip("If the AI gets distracted when in normal state you can set here the increased value you want the meter to be overriden to. So when the AI does see a potential enemy, it detects it more quickly.")]
        public bool overrideVMValueOnDistract;
        [Range(0, 1)] public float vmValueOnDistract;
        
        [Tooltip("As long as the AI is in distracted state the vision meter will not decrement. Until the AI exits out of the state.")]
        public bool dontDecrementVMOnDistract;

        #endregion

        #region STRUCTS

        [System.Serializable] public struct normalVision 
        {
            [Range(0f, 360f)]
            public float coneAngle;
            [Min(0f)]
            public float sightRange;
            
            public normalVision (float angle, float range) {
                coneAngle = angle;
                sightRange = range;
            }
        }

        [System.Serializable] public struct alertVision 
        {
            [Range(0f, 360f)]
            public float coneAngle;
            [Min(0f)]
            public float sightRange;

            public alertVision (float angle, float range) {
                coneAngle = angle;
                sightRange = range;
            }
        }

        [System.Serializable] public struct attackVision 
        {
            [Range(0f, 360f)]
            [Tooltip("Always better to have this at 360 in order for the AI to have 360 view when in attack state.")]
            public float coneAngle;
            [Min(0f), Tooltip("Will be automatically set if cover shooter enabled based on Distance From Enemy property.")]
            public float sightRange;
            [Tooltip("If false, the AI will only apply attack vision when in attack state and there's a clear enemy. If an enemy has been lost for a frame or so, it'll apply the alert vision until a target is seen again. This makes vision more realistic but more bound to lose it's target especially if the target goes through the AI collider. If, however, this property is enabled, the attack vision will always be applied in attack state no matter there's a clear enemy or not. Making losing enemies impossible until they're out of range.")]
            public bool alwaysApply;

            public attackVision (float angle, float range, bool forcedApply=true) {
                coneAngle = angle;
                sightRange = range;
                alwaysApply = forcedApply;
            }
        }

        [System.Serializable] public struct AlertTags 
        {
            [Tooltip("The tag name you want to react to.")]
            public string alertTag;
            [Tooltip("The behaviour script to enable when seeing this alert tag.")]
            public MonoBehaviour behaviourScript;
            [Tooltip("When the AI sees an object with an alert tag it'll immediately change it to this value. In order not to get alerted by it again. If this value is empty it'll fall back to 'Untagged'.")]
            public string fallBackTag;
        }
        
        [System.Serializable] public struct VisionMeterSpeeds
        {
            [Min(0), Tooltip("Set the speed of detection when the target's distance is > than half the vision radius.")]
            public float speedOnFullDistance;
            [Min(0), Tooltip("Set the speed of increment when the target's distance is <= half the vision radius.")]
            public float speedOnHalfDistance;
            [Min(0), Tooltip("Set the speed of decrement when there's no enemy detected.")]
            public float speedOnEmpty;

            public VisionMeterSpeeds(float speedOnFullDistance, float speedOnHalfDistance, float speedOnEmpty) {
                this.speedOnFullDistance = speedOnFullDistance;
                this.speedOnHalfDistance = speedOnHalfDistance;
                this.speedOnEmpty = speedOnEmpty;
            }
        }

        #endregion

        #region DRAWING
        #if UNITY_EDITOR

        // show the vison cone in scene view
        public void ShowVisionSpheres(Transform visionTransform, Transform charTransform) 
        {
            if (showAttackVision) {
                DrawVisionCone(visionTransform, charTransform, visionDuringAttackState.coneAngle, visionDuringAttackState.sightRange, attackVisionColor);
            }

            if (showAlertVision) {
                DrawVisionCone(visionTransform, charTransform, visionDuringAlertState.coneAngle, visionDuringAlertState.sightRange, alertVisionColor);
            }

            if (showNormalVision) {
                DrawVisionCone(visionTransform, charTransform, visionDuringNormalState.coneAngle, visionDuringNormalState.sightRange, normalVisionColor);
            }

            if (showMaxSightLevel || showMinSightLevel)
            {
                DrawVisionCone(visionTransform, charTransform, visionDuringAttackState.coneAngle, visionDuringAttackState.sightRange, Color.red, true);
            }
        }

        // draw vision cone
        void DrawVisionCone(Transform visionTransform, Transform charTransform, float angle, float rayRange, Color color, bool drawSightLevelsOnly = false)
        {
            if (visionTransform == null) return;

            if (drawSightLevelsOnly)
            {
                if (showMaxSightLevel && maxSightLevel > 0f)
                {
                    DrawHeightRectangle(visionTransform, charTransform, maxSightLevel, Color.green);
                }

                if (showMinSightLevel) {
                    DrawHeightRectangle(visionTransform, charTransform, minSightLevel, Color.red);
                }
        
                return;
            }

            Transform t = charTransform;
            Vector3 forward = t.forward;
            Vector3 origin = t.position + visionPosition;

            Handles.color = color;

            // Draw the solid arc (the cone)
            Handles.DrawSolidArc(
                origin,
                Vector3.up,
                Quaternion.Euler(0, -angle / 2, 0) * forward,
                angle,
                rayRange
            );

            Handles.color = Color.white;
            Handles.DrawWireArc(origin, Vector3.up, Quaternion.Euler(0, -angle / 2, 0) * forward, angle, rayRange);

            Vector3 left = Quaternion.Euler(0, -angle / 2, 0) * forward * rayRange;
            Vector3 right = Quaternion.Euler(0, angle / 2, 0) * forward * rayRange;

            Handles.DrawLine(origin, origin + left);
            Handles.DrawLine(origin, origin + right);

        }

        void DrawHeightRectangle(Transform visionT, Transform charTransform, float sightLevel, Color color)
        {
            Transform t = visionT.transform;

            Vector3 center = charTransform.position + new Vector3(visionPosition.x, sightLevel, visionPosition.z);
            Vector2 size = new Vector2(0.5f, 0.5f);
            Vector2 halfSize = size * 0.5f;

            // Rectangle corners in local space
            Vector3[] corners = new Vector3[4];
            corners[0] = center + t.right * -halfSize.x + t.forward * -halfSize.y;
            corners[1] = center + t.right * -halfSize.x + t.forward * halfSize.y;
            corners[2] = center + t.right * halfSize.x + t.forward * halfSize.y;
            corners[3] = center + t.right * halfSize.x + t.forward * -halfSize.y;

            Handles.color = color;
            Handles.DrawAAConvexPolygon(corners);
        }

        #endif
        #endregion

        #region FUNCTIONALITY
        
        public void Validate()
        {
            if (minSightLevel > visionPosition.y) 
            {
                minSightLevel = visionPosition.y - 1;

                #if UNITY_EDITOR
                if (UnityEditor.EditorUtility.DisplayDialog("Min Sight Level is too large",
                    "Min Sight Level can't be as big as the Y axis of Vision Position. It'll automatically be decremented.", "Ok")) {
                }
                #endif 
            }

            if (maxSightLevel < visionPosition.y) {
                maxSightLevel = visionPosition.y;
            }
        }

        // return the index of the passed alert tag -> if exists
        public int GetAlertTagIndex(string alertTag)
        {
            for (int i=0; i<alertTags.Length; i++) 
            {
                // check if alert tag is empty
                if (alertTags[i].alertTag.Length <= 0) {
                    continue;
                }

                // check if alert tag equals the paramater
                if (alertTags[i].alertTag != alertTag) {
                    continue;
                }
                
                return i;
            }

            return -1;
        }

        // check if any tag in hostile and alert are equal
        public void CheckHostileAndAlertItemEqual(bool dialogue=false)
        {
            for (int i=0; i<hostileTags.Length; i++) 
            {
                for (int x=0; x<alertTags.Length; x++) 
                {
                    if (hostileTags[i].Length > 0 && hostileTags[i] == alertTags[x].alertTag) 
                    {
                        #if UNITY_EDITOR
                        if (UnityEditor.EditorUtility.DisplayDialog("Same tag in Hostile and Alert",
                            "You can't have the same tag name in both Hostile and Alert. The tag name in Alert Tags will be removed when out of focus or you can continue typing by double clicking the text.", "Ok")) {
                        }
                        #endif 

                        alertTags[x].alertTag = "";
                    }
                }
            }
        }
        
        #endregion
    }
}