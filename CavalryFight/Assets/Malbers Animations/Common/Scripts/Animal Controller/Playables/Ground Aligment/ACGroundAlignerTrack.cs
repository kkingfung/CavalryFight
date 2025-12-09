using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace MalbersAnimations.Controller
{
    [TrackBindingType(typeof(MAnimal))]
    [TrackClipType(typeof(ACGroundAlignerClip))]
    public class ACGroundAlignerTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<ACGroundAlignerMixer>.Create(graph, inputCount);
        }
    }
}
