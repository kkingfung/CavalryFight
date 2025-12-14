using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("Blaze AI/Additive Scripts/Blaze AI Audio Manager")]
public class BlazeAIAudioManager : MonoBehaviour
{
    #region PROPERTIES

    [Tooltip("Auto catch the game camera so that based on it's distance from AIs, it plays the patrol audios from closest AIs.")]
    public bool autoCatchCamera = true;

    [Tooltip("Set the camera or player transform so that based on it's distance from AIs, it plays the patrol audios from closest AIs.")]
    public Transform cameraOrPlayer;
    
    [Min(0), Tooltip("A distance check will be made between the player/camera and all AIs in the manager. If the distance is less or equal to this value, the patrol audio will play. If more, the AI audio won't play until the player is closer.")]
    public float distanceToPlay = 30;
    
    [Min(0), Tooltip("A random time value will be generated between the two values to play an audio. For a constant value set the two fields to the same value.")]
    public Vector2 playAudioEvery = new Vector2(10, 30);

    #endregion

    #region SYSTEM VARIABLES

    public static BlazeAIAudioManager instance;
    List<BlazeAI> blazeList = new List<BlazeAI>();
    List<BlazeAI> eligibleAIList = new List<BlazeAI>();
    BlazeAI lastBlazePlayed;

    float chosenTime = 0;
    float timer = 0;
    bool isLooping = false;
    int restFrames = 2;
    int restFramesElapsed = 0;

    #endregion

    #region UNITY METHODS

    void Awake()
    {
        SetInstance();
        SetCamera();
        chosenTime = Random.Range(playAudioEvery.x, playAudioEvery.y);
        // no need to be enabled on start -> will enable when AI adds itself to manager
        enabled = false;
    }

    void Update()
    {
        if (cameraOrPlayer == null) {
            Debug.LogWarning("No transform set to the CameraOrPlayer property in the Audio Manager component. Please set a transform.");
            return;
        }

        // if last played AI hasn't finished yet
        if(!LastAISilent()) return;
        
        timer += Time.deltaTime;
        if (timer < chosenTime) return;
        timer = 0;

        // prepare for next cycle
        chosenTime = Random.Range(playAudioEvery.x, playAudioEvery.y);

        if (!isLooping) {
            StartCoroutine("AudioManagerLoop");
        }
    }

    #endregion

    #region SYSTEM METHODS

    void SetInstance()
    {
        if (BlazeAIAudioManager.instance == null) {
            instance = this;
            return;
        }
    
        Destroy(this);
    }

    void SetCamera()
    {
        if (autoCatchCamera) {
            if (Camera.main != null) cameraOrPlayer = Camera.main.transform;
            else Debug.LogWarning("Audio Manager component couldn't auto find the main camera. Please make sure your camera has the MainCamera tag OR set the CameraOrPlayer property in this component to the player instead (by disabling Auto Catch Camera).");

            return;
        }

        if (cameraOrPlayer == null) {
            Debug.LogWarning("No transform set to the CameraOrPlayer property in the Audio Manager component. Please set a transform.");
        }
    }

    IEnumerator AudioManagerLoop()
    {
        isLooping = true;

        for (int i=0; i<blazeList.Count; i++)
        {
            if (i > 0) {
                while (restFramesElapsed < restFrames) {
                    restFramesElapsed++;
                    yield return null;
                }
            }

            restFramesElapsed = 0;

            if (blazeList[i] == null) {
                blazeList.RemoveAt(i);
                eligibleAIList.Remove(blazeList[i]);
                continue;
            }

            if (cameraOrPlayer == null) break;

            float distance = Vector3.Distance(blazeList[i].transform.position, cameraOrPlayer.position);
            if (distance > distanceToPlay) continue;
            if (blazeList[i].state != BlazeAI.State.normal && blazeList[i].state != BlazeAI.State.alert) continue;

            eligibleAIList.Add(blazeList[i]);
        }

        if (eligibleAIList.Count > 0) {
            int randIndex = Random.Range(0, eligibleAIList.Count);
            lastBlazePlayed = eligibleAIList[randIndex];
            lastBlazePlayed.PlayPatrolAudio();
            eligibleAIList.Clear();
        }

        isLooping = false;
    }

    bool LastAISilent()
    {
        if (lastBlazePlayed == null) return true;

        if (lastBlazePlayed.state == BlazeAI.State.normal || lastBlazePlayed.state == BlazeAI.State.alert) {
            if (!lastBlazePlayed.agentAudio.isPlaying) {
                return true;
            }

            return false;
        }

        lastBlazePlayed = null;
        return true;
    }

    public void AddToManager(BlazeAI blaze)
    {
        if (blazeList.Contains(blaze)) return;
        blazeList.Add(blaze);
        enabled = true;
    }

    public void RemoveFromManager(BlazeAI blaze)
    {
        if (!blazeList.Contains(blaze)) return;

        blazeList.Remove(blaze);
        
        if (blazeList.Count == 0) {
            Disable();
        }
    }

    public List<BlazeAI> ReturnList()
    {
        return blazeList;
    }

    void Disable()
    {
        enabled = false;
        StopCoroutine("AudioManagerLoop");
        isLooping = false;
        timer = 0;
    }

    #endregion
}