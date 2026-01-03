using System.Collections.Generic;
using UnityEngine;
using BlazeAISpace;

[AddComponentMenu("Blaze AI/Additive Scripts/Blaze AI Distance Culling")]
public class BlazeAIDistanceCulling : MonoBehaviour
{
    #region PROPERTIES

    [Tooltip("Automatically get the game camera.")]
    public bool autoCatchCamera = true;

    [Tooltip("The player or camera to calculate the distance between it and the AIs.")]
    public Transform cameraOrPlayer;

    [Min(0), Tooltip("If an AI distance is more than this set value then it will get culled.")]
    public float distanceToCull = 30;

    [Range(0, 30), Tooltip("Run the cycle every set frames. The bigger the number, the better it is for performance but less accurate.")]
    public int cycleFrames = 7;

    [Tooltip("How many agents to process in one batch per cycle.")]
    [Range(1, 20)] public int batchSize = 5;

    [Tooltip("If enabled, the culling will disable Blaze only and play the idle animation on cull.")]
    public bool disableBlazeOnly;

    #endregion

    #region SYSTEM VARIABLES

    public static BlazeAIDistanceCulling instance;

    List<BlazeAI> agentsList = new List<BlazeAI>();
    int framesPassed = 0;
    int currentBatchStart = 0;

    #endregion

    #region UNITY METHODS

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(this);
            return;
        }

        if (autoCatchCamera)
        {
            cameraOrPlayer = Camera.main ? Camera.main.transform : null;
            return;
        }

        if (cameraOrPlayer == null)
            Debug.LogWarning("No camera set in Blaze AI Distance Culling component.");

        if (cycleFrames < 0) cycleFrames = 0;
    }

    void Update()
    {
        if (cameraOrPlayer == null) return;

        // Run every X frames (cycleFrames)
        if (framesPassed < cycleFrames)
        {
            framesPassed++;
            return;
        }

        framesPassed = 0;
        RunCulling();
    }

    #endregion

    #region MAIN LOGIC

    public void RunCulling()
    {
        int count = agentsList.Count;
        if (count == 0) return;

        int end = Mathf.Min(currentBatchStart + batchSize, count);

        for (int i = currentBatchStart; i < end; i++)
        {
            if (i >= agentsList.Count) break;

            BlazeAI blaze = agentsList[i];
            if (blaze == null)
            {
                agentsList.RemoveAt(i);
                i--;
                continue;
            }

            float sqrDist = (blaze.transform.position - cameraOrPlayer.position).sqrMagnitude;
            float sqrCullDist = distanceToCull * distanceToCull;

            // Cull if too far
            if (sqrDist > sqrCullDist)
            {
                if (disableBlazeOnly)
                {
                    if (!blaze.enabled) continue;
                    blaze.enabled = false;

                    if (blaze.state != BlazeAI.State.death)
                        PlayCullAnim(blaze);

                    continue;
                }

                if (!blaze.gameObject.activeSelf) continue;
                blaze.gameObject.SetActive(false);
                continue;
            }

            // Within range -> re-enable
            if (disableBlazeOnly)
            {
                if (!blaze.enabled)
                    blaze.enabled = true;

                continue;
            }

            if (!blaze.gameObject.activeSelf)
            {
                blaze.gameObject.SetActive(true);
                PlayCullAnim(blaze);
            }
        }

        // Move to next batch
        currentBatchStart = end;

        // If all batches done, reset
        if (currentBatchStart >= agentsList.Count)
            currentBatchStart = 0;
    }

    void PlayCullAnim(BlazeAI blaze)
    {
        if (blaze.animToPlayOnCull != null && blaze.animToPlayOnCull.Length > 0)
        {
            blaze.animManager.Play(blaze.animToPlayOnCull, 0.25f);
        }
    }

    #endregion

    #region API

    public void AddAgent(BlazeAI agent)
    {
        if (!agentsList.Contains(agent))
        {
            agentsList.Add(agent);
        }
    }

    public void RemoveAgent(BlazeAI agent)
    {
        if (agentsList.Contains(agent))
        {
            agentsList.Remove(agent);
        }
    }

    public bool CheckAgent(BlazeAI agent) 
    {
        return agentsList.Contains(agent);
    }

    #endregion
}