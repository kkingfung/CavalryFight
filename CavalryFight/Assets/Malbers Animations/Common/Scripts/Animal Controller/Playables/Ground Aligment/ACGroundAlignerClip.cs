using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace MalbersAnimations.Controller
{
    [TrackBindingType(typeof(MAnimal))]
    public class ACGroundAlignerClip : PlayableAsset, ITimelineClipAsset
    {
        public ClipCaps clipCaps => ClipCaps.None;

        public float Offset = 0;
        public float Distance = 2f;
        public bool HasHipPivot = true;

        public ExposedReference<MAnimal> animalEndLocation;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<ACGroundAlignerBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();

            behaviour.distance = Distance;
            behaviour.HasHipPivot = HasHipPivot;
            behaviour.Offset = Offset;
            behaviour.EndLocation = animalEndLocation.Resolve(graph.GetResolver());

            return playable;
        }
    }
}
