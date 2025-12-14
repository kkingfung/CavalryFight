using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace BlazeAISpace
{
    [AddComponentMenu("Blaze AI/Going To Cover Behaviour")]
    public class GoingToCoverBehaviour : BlazeBehaviour
    {
        #region PROPERTIES

        [Tooltip("Object layers that can take cover behind.")]
        public LayerMask coverLayers;
        [Range(-1f, 1f), Tooltip("The lower the number the better the hiding spot. From -1 (best) to 1 (worst)")]
        public float hideSensitivity = -0.25f;
        [ Tooltip("This is the min and max height values to search for a cover point. X for min and Y for max. This is used to prevent the AI from going to a cover point that's on a higher or lower level in a building for example. Don't play with this unless you have problems where the AI is choosing a cover point too high or too low. The logic is finding the difference between the bottom point of the cover and the AI position, if it's smaller than X or bigger than Y then the cover point will be ignored.")]
        public Vector2 minAndMaxCoverPointHeight = new Vector2(-0.5f, 0.5f);
        [Min(0), Tooltip("The search distance for cover. This can't be bigger than the [Distance From Enemy] property (automatically clamped if so)")]
        public float searchDistance = 25f;
        [Tooltip("Show search distance as a light blue sphere in scene view.")]
        public bool showSearchDistance;

        [Tooltip("The minimum height of cover obstacles to be eligible for search. Cover height is measured using collider.bounds.y. Use the GetCoverHeight script on any obstacle to get it's height.")]
        public float minCoverHeight = 1f;
        [Tooltip("If the chosen cover height is bigger or equals to this value then the high cover animation will play. If it's less than this then low cover animation will play.")]
        public float highCoverHeight = 3;
        [Tooltip("The animation name to play when in high cover. The cover is considered to be a high cover when it's height is bigger or equal to [High Cover Height] property. The height is calculated using collider.bounds.y. You can use the BlazeAI GetCoverHeight script to get the height of any obstacle.")]
        public string highCoverAnim;
        [Tooltip("The animation name to play when in low cover. The cover is considered to be a low cover when it's height is less than the [High Cover Height] property. The height is calculated using collider.bounds.y. You can use the BlazeAI GetCoverHeight script to get the height of any obstacle.")]
        public string lowCoverAnim;
        public float coverAnimT = 0.25f;

        [Tooltip("If set to true, the AI will rotate to match the cover normal. Meaning the back of the character will be on the cover. If set to false, will take cover in the same current rotation when reached the cover.")]
        public bool rotateToCoverNormal = true;
        public float rotateToCoverSpeed = 300f;
        [Tooltip("This will make the AI refrain from attacking and only do so after taking cover.")]
        public bool onlyAttackAfterCover;

        [Tooltip("If enabled, the AI will play an audio when going to cover. The audio is chosen randomly in the audio scriptable from the Going To Cover array.")]
        public bool playAudioOnGoingToCover;
        [Tooltip("If enabled, the AI will always play an audio when going to cover. If false, there's a 50/50 chance whether the AI will play an audio or not.")]
        public bool alwaysPlayAudio;

        public UnityEvent onStateEnter;
        public UnityEvent onStateExit;
        
        #endregion

        #region BEHAVIOUR VARS

        bool isFirstRun = true;
        public BlazeAI blaze { private set; get; }
        CoverShooterBehaviour coverShooterBehaviour;

        public struct CoverProperties {
            public Transform cover;
            public Vector3 coverPoint;
            public float coverHeight;
            public BlazeAICoverManager coverManager;
        }

        public CoverProperties coverProps;
        public Transform lastCover { get; private set; }
        
        float passedCenterPosH = -1;
        bool isCoverBlown;
        bool offMeshBypassed;
        float offMeshTimer = 0;
        float takingCoverElapsed = 0;

        #endregion
        
        #region GARBAGE REDUCTION
        
        Collider[] findCoverColls = new Collider[25];
        
        #endregion

        #region MAIN METHODS
        
        void Awake()
        {
            blaze = GetComponent<BlazeAI>();
        }

        public virtual void OnStart()
        {
            isFirstRun = false;  
            coverShooterBehaviour = GetComponent<CoverShooterBehaviour>();
            lastCover = null;
        }

        public override void Open()
        {
            if (isFirstRun) {
                OnStart();
            }

            onStateEnter.Invoke();

            if (blaze == null) {
                Debug.LogWarning($"No Blaze AI component found in the gameobject: {gameObject.name}. AI behaviour will have issues.");
            }
        }

        public override void Close()
        {
            offMeshBypassed = false;
            offMeshTimer = 0;
            
            RemoveCoverOccupation();
            onStateExit.Invoke();

            if (blaze == null) {
                return;
            }

            blaze.ResetVisionPosition();
            blaze.tookCover = false;
        }

        public override void Main()
        {
            if (!blaze.coverShooterMode) 
            {
                blaze.SetState(BlazeAI.State.attack);
                return;
            }

            if (coverShooterBehaviour.equipWeapon) 
            {
                if (!coverShooterBehaviour.hasEquipped || coverShooterBehaviour.isEquipping)
                {
                    coverShooterBehaviour.EquipWeapon();
                    return;
                }
            }

            // if blaze is in friendly mode -> exit the state
            if (blaze.friendly) 
            {
                blaze.SetState(BlazeAI.State.attack);
                return;
            }

            // call others that are not alerted yet and start the timer until attack
            coverShooterBehaviour.CallOthers();
            
            if (!onlyAttackAfterCover) {
                coverShooterBehaviour.TimeUntilAttack();
            }

            if (blaze.IfShouldIgnoreOffMesh()) 
            {
                IgnoreOffMeshBehaviour();
                return;
            }
            
            // if target exists
            if (blaze.enemyToAttack && !blaze.isAttacking)
            {
                // if cover property is not null -> means AI has set a cover
                if (coverProps.cover)
                {
                    lastCover = coverProps.cover;

                    // moves agent to point and returns true when reaches destination
                    if (blaze.MoveTo(coverProps.coverPoint, coverShooterBehaviour.moveSpeed, coverShooterBehaviour.turnSpeed, coverShooterBehaviour.moveAnim, coverShooterBehaviour.idleMoveT)) {
                        TakeCover();
                    }
                    else {
                        blaze.ResetVisionPosition();
                    }

                    return;
                }
                
                if (blaze.hitProps.hitWhileInCover) 
                {
                    FindCover(lastCover);
                    return;
                }

                FindCover();
                return;
            }

            // if there's no target return to cover shooter behaviour
            blaze.SetState(BlazeAI.State.attack);
        }  

        void OnDrawGizmosSelected()
        {
            if (showSearchDistance) 
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, searchDistance);
            }
        }

        void OnValidate()
        {
            if (blaze == null) {
                blaze = GetComponent<BlazeAI>();
            }
        }

        #endregion

        #region COVER METHODS

        // search for cover - if getPosOnly is true, the AI won't move to the best cover
        public virtual void FindCover(Transform coverToAvoid = null)
        {   
            blaze.hitProps.hitWhileInCover = false;
            blaze.tookCover = false;

            System.Array.Clear(findCoverColls, 0, 25);
            int hits = Physics.OverlapSphereNonAlloc(transform.position, searchDistance, findCoverColls, coverLayers);
            int hitReduction = 0;
            Transform playerCover = coverShooterBehaviour.GetTargetCover();
            bool shouldChangeCover = CalculateChangeCoverFrequency();

            // eliminate bad cover options
            for (int i=0; i<hits; i++) 
            {
                Vector3 coverBottomPos = findCoverColls[i].ClosestPoint(blaze.ValidateYPoint(findCoverColls[i].transform.position));
                float distance = Vector3.Distance(coverBottomPos, blaze.enemyColPoint);

                // avoid selecting covers that are on a higher or lower floor
                Vector3 directionToCover = Vector3.Normalize(coverBottomPos - transform.position);
                float heightProjection = Mathf.Abs(coverBottomPos.y) - Mathf.Abs(transform.position.y);
                float forwProjAIToCover = Vector3.Dot(transform.forward, directionToCover);

                // avoid selecting cover where it's behind the enemy but in front of the AI -> making the AI too exposed
                Vector3 directionFromEnemyToCover = Vector3.Normalize(coverBottomPos - blaze.enemyToAttack.transform.position);
                float forwProjEnemyToCover = Vector3.Dot(blaze.enemyToAttack.transform.forward, directionFromEnemyToCover);

                Vector3 directionToAI = Vector3.Normalize(transform.position - blaze.enemyToAttack.transform.position);
                float forwProjEnemyToAI = Vector3.Dot(blaze.enemyToAttack.transform.forward, directionToAI);

                
                // eliminate covers that don't respect these conditions
                if (distance + 2 >= coverShooterBehaviour.distanceFromEnemy || distance > blaze.vision.visionDuringAttackState.sightRange ||
                    findCoverColls[i].bounds.size.y < minCoverHeight || findCoverColls[i].transform == coverToAvoid ||
                    heightProjection != 0 && (heightProjection < minAndMaxCoverPointHeight.x || heightProjection > minAndMaxCoverPointHeight.y) ||
                    (forwProjEnemyToAI >= 0.5f && forwProjEnemyToCover <= 0 && forwProjAIToCover >= 0.5f) ||
                    (isCoverBlown && lastCover == findCoverColls[i].transform)) {

                    findCoverColls[i] = null;
                    hitReduction++;

                    continue;
                }
                
                // calculate the change cover frequency -> don't take same cover twice in a row
                if (hits >= 2 && coverToAvoid == null) 
                {
                    if (shouldChangeCover && lastCover != null) 
                    {
                        if (lastCover.IsChildOf(findCoverColls[i].transform)) 
                        {
                            findCoverColls[i] = null;
                            hitReduction++;
                            continue;
                        }
                    }
                }

                // if player is hiding behind cover -> remove that cover as being eligible
                if (playerCover != null) 
                {
                    if (playerCover.IsChildOf(findCoverColls[i].transform)) {
                        findCoverColls[i] = null;
                        hitReduction++;
                        continue;
                    }
                }

                // check if other agents are already occupying/moving to the same cover by reading the cover manager component
                BlazeAICoverManager coverManager = findCoverColls[i].transform.GetComponent<BlazeAICoverManager>();
                if (!coverManager) continue;

                // cover manager exists and not occupied -> continue
                if (coverManager.occupiedBy == null || coverManager.occupiedBy == transform) {
                    continue;
                }

                // reaching this far means cover manager exists and is occupied -> so remove as a potential cover
                findCoverColls[i] = null;
                hitReduction++;
            }

            hits -= hitReduction;
            System.Array.Sort(findCoverColls, ColliderArraySortComparer);

            // if no covers found
            if (hits <= 0) 
            {
                NoCoversFound();
                return;
            }

            NavMeshHit hit = new NavMeshHit();
            NavMeshHit hit2 = new NavMeshHit();
            NavMeshHit closestEdge = new NavMeshHit();
            NavMeshHit closestEdge2 = new NavMeshHit();

            // if found obstacles
            for (int i = 0; i < hits; i++) 
            {
                Vector3 boundSize = findCoverColls[i].GetComponent<Collider>().bounds.size;
                
                if (findCoverColls[i].bounds.size.y - 0.2f <= blaze.defaultVisionPos.y) {
                    passedCenterPosH = blaze.defaultVisionPos.y/2;
                }
                else {
                    passedCenterPosH = blaze.defaultVisionPos.y;
                }

                
                if (NavMesh.SamplePosition(findCoverColls[i].transform.position, out hit, boundSize.x + boundSize.z, NavMesh.AllAreas)) 
                {
                    if (!NavMesh.FindClosestEdge(hit.position, out closestEdge, NavMesh.AllAreas)) {
                        continue;
                    }

                    if (Vector3.Dot(closestEdge.normal, (blaze.enemyColPoint - closestEdge.position).normalized) < hideSensitivity) 
                    {
                        if (!blaze.IsPathReachable(closestEdge.position)) {
                            continue;
                        }

                        if (blaze.CheckIfTargetSeenFromPoint(closestEdge.position, true, passedCenterPosH)) {
                            continue;
                        }

                        ChooseCover(closestEdge, findCoverColls[i]);
                        return;
                    }
                    else {
                        // Since the previous spot wasn't facing "away" enough from the target, we'll try on the other side of the object
                        if (NavMesh.SamplePosition(findCoverColls[i].transform.position - (blaze.enemyColPoint - hit.position).normalized * 2, out hit2, boundSize.x + boundSize.z, NavMesh.AllAreas)) 
                        {
                            if (!NavMesh.FindClosestEdge(hit2.position, out closestEdge2, NavMesh.AllAreas)) {
                                continue;
                            }

                            if (Vector3.Dot(closestEdge2.normal, (blaze.enemyColPoint - closestEdge2.position).normalized) < hideSensitivity) 
                            {
                                if (!blaze.IsPathReachable(closestEdge2.position)) {
                                    continue;
                                }

                                if (blaze.CheckIfTargetSeenFromPoint(closestEdge2.position, true, passedCenterPosH)) {
                                    continue;
                                }

                                ChooseCover(closestEdge2, findCoverColls[i]);
                                return;
                            }
                        }
                        else {
                            continue;
                        }
                    }
                }
                else {
                    continue;
                }
            }

            // if reached this point then no cover was found
            NoCoversFound();
        }

        // choose and save the passed cover
        public virtual void ChooseCover(NavMeshHit hit, Collider cover)
        {   
            BlazeAICoverManager coverMang = cover.transform.GetComponent<BlazeAICoverManager>();
            if (coverMang == null) {
                coverMang = cover.transform.gameObject.AddComponent<BlazeAICoverManager>() as BlazeAICoverManager;
            }
            
            coverMang.occupiedBy = transform;
            coverProps.coverManager = coverMang;
            coverProps.cover = cover.transform;
            coverProps.coverPoint = hit.position;
            coverProps.coverHeight = cover.bounds.size.y;

            // get the manual cover position closest to the automated one
            if (coverMang.coverPositions != null)
            {
                if (coverMang.coverPositions.Length > 0)
                {
                    int max = coverMang.coverPositions.Length;
                    float smallestDist = Mathf.Infinity;
                    float tempCompare = 0;
                    int chosenIndex = -1;
                    Vector3 validatedPos = Vector3.zero;

                    for (int i=0; i<max; i++)
                    {
                        validatedPos = blaze.ValidateYPoint(coverMang.coverPositions[i]);
                        tempCompare = ((coverProps.coverPoint - validatedPos) + transform.position).sqrMagnitude;

                        if (tempCompare < smallestDist && !blaze.CheckIfTargetSeenFromPoint(validatedPos, true, passedCenterPosH) && blaze.IsPathReachable(validatedPos))
                        {
                            smallestDist = tempCompare;
                            chosenIndex = i;
                        }
                    }

                    if (chosenIndex > -1)
                    {
                        coverProps.coverPoint = blaze.ValidateYPoint(coverMang.coverPositions[chosenIndex]);
                    }
                }
            }

            PlayGoingToCoverAudio();
        }

        public void RemoveCoverOccupation()
        {
            // set the current cover to null
            coverProps.cover = null;
            takingCoverElapsed = 0;

            // if doesn't have cover manager -> return
            if (!coverProps.coverManager) {
                return;
            }

            // if cover manager shows that cover isn't occupied -> return
            if (coverProps.coverManager.occupiedBy == null) {
                return;
            }

            // if cover manager shows that cover is occupied by a different AI -> return
            if (coverProps.coverManager.occupiedBy != transform) {
                return;
            }

            // if reached this point means -> cover manager exists and this current AI occupies/occupied it
            coverProps.coverManager.occupiedBy = null;
        }

        // no covers have been found
        void NoCoversFound()
        {
            RemoveCoverOccupation();
            coverShooterBehaviour.FoundNoCover();
        }

        // taking cover
        void TakeCover()
        {
            // high cover
            if (coverProps.coverHeight >= highCoverHeight) {
                blaze.animManager.Play(highCoverAnim, coverAnimT);
            }

            // low cover
            if (coverProps.coverHeight < highCoverHeight) {
                blaze.animManager.Play(lowCoverAnim, coverAnimT);
            }

            LowerVisionPosition();
            RotateToCoverNormal();
            CheckCoverBlown();

            if (onlyAttackAfterCover) {
                coverShooterBehaviour.TimeUntilAttack();
            }

            blaze.tookCover = true;
        }

        // rotate to cover
        void RotateToCoverNormal()
        {
            if (!rotateToCoverNormal) return;

            RaycastHit hit;
            int layers = coverLayers;

            Vector3 dir = coverProps.cover.position - transform.position;
            Vector3 coverNormal = Vector3.zero;

            // get normal
            if (Physics.Raycast(transform.position, dir, out hit, Mathf.Infinity, layers)) {
                if (coverProps.cover.IsChildOf(hit.transform) || hit.transform.IsChildOf(coverProps.cover)) {
                    coverNormal = hit.normal;
                    coverNormal.z = 0;
                }
            }
            
            // if hasn't hit the correct cover yet -> return
            if (coverNormal == Vector3.zero) return;

            // rotate to normal
            Quaternion rot = Quaternion.FromToRotation(Vector3.forward, coverNormal);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, rotateToCoverSpeed * Time.deltaTime);
        }

        // check if target has compromised AI cover
        void CheckCoverBlown()
        {
            // to avoid the AI taking cover and then instantly finding the cover blown
            if (takingCoverElapsed <= 0.55f) {
                takingCoverElapsed += Time.deltaTime;
                return;
            }

            Vector3 startDir = transform.position + blaze.vision.visionPosition;
            Vector3 targetDir = blaze.enemyColPoint - (transform.position + blaze.vision.visionPosition);
            float rayDistance = Vector3.Distance(blaze.enemyColPoint, transform.position + blaze.vision.visionPosition) + 5;
            
            if (blaze.CheckTargetVisibleWithRay(blaze.enemyToAttack.transform, startDir, targetDir, rayDistance, Physics.AllLayers))
            {
                RemoveCoverOccupation();
                isCoverBlown = true;

                if (coverShooterBehaviour.coverBlownDecision == CoverShooterBehaviour.CoverBlownDecision.AlwaysAttack) {
                    AttackFromCover();
                }
                else if (coverShooterBehaviour.coverBlownDecision == CoverShooterBehaviour.CoverBlownDecision.TakeCover) {
                    FindCover(lastCover);
                }
                else {
                    int rand = Random.Range(0, 2);

                    if (rand == 0) {
                        FindCover(lastCover);
                    }
                    else {
                        AttackFromCover();
                    }
                }

                return;
            }

            isCoverBlown = false;
        }

        void AttackFromCover()
        {
            blaze.Attack();
        }

        int ColliderArraySortComparer(Collider A, Collider B)
        {
            if (A == null && B != null)
            {
                return 1;
            }
            else if (A != null && B == null)
            {
                return -1;
            }
            else if (A == null && B == null)
            {
                return 0;
            }
            else
            {
                return Vector3.Distance(transform.position, A.transform.position).CompareTo(Vector3.Distance(transform.position, B.transform.position));
            }
        }

        // return true or false for whether to change last cover or not
        bool CalculateChangeCoverFrequency()
        {
            // stay in the same cover
            if (coverShooterBehaviour.changeCoverFrequency == 0) {
                return false;
            }

            // change cover
            if (coverShooterBehaviour.changeCoverFrequency == 10) {
                return true;
            }

            // calculate the odds and chance of changing cover
            int odds = 10 - coverShooterBehaviour.changeCoverFrequency;
            int chanceOfChangingCover = Random.Range(1, 11);

            if (chanceOfChangingCover > odds) {
                return true;
            }
            
            return false;
        }

        void LowerVisionPosition()
        {
            if (blaze.vision.visionPosition.y <= (coverProps.coverHeight - 0.2f)) return;

            if (coverProps.coverHeight - 0.4f <= 0) {
                blaze.vision.visionPosition = new Vector3(blaze.defaultVisionPos.x, blaze.defaultVisionPos.y/2, blaze.defaultVisionPos.z);
                return;
            }

            blaze.vision.visionPosition = new Vector3(blaze.defaultVisionPos.x, coverProps.coverHeight - 0.4f, blaze.defaultVisionPos.z);
        }

        void IgnoreOffMeshBehaviour()
        {
            if (blaze.enemyToAttack != null) 
            {
                if (!offMeshBypassed) {
                    blaze.navmeshAgent.Warp(transform.position);
                    offMeshBypassed = true;
                }

                NoCoversFound();
                offMeshTimer += Time.deltaTime;
                
                if (offMeshTimer >= 1) {
                    offMeshTimer = 0;
                    offMeshBypassed = false;
                }

                return;
            }
            
            blaze.SetState(BlazeAI.State.attack);
        }

        #endregion

        #region AUDIO

        void PlayGoingToCoverAudio()
        {
            if (blaze.IsAudioScriptableEmpty() || !playAudioOnGoingToCover) return;

            if (alwaysPlayAudio) 
            {
                blaze.PlayAudio(blaze.audioScriptable.GetAudio(AudioScriptable.AudioType.GoingToCover));
                return;
            }
            
            int rand = Random.Range(0, 2);
            if (rand == 0) return;

            blaze.PlayAudio(blaze.audioScriptable.GetAudio(AudioScriptable.AudioType.GoingToCover));
        }

        #endregion
    }
}