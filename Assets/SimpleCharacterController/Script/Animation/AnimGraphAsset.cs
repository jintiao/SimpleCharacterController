using UnityEngine;
using UnityEngine.Playables;

namespace JT
{
    public abstract class AnimGraphAsset : ScriptableObject
    {
        public abstract IAnimGraphInstance Instatiate(PlayableGraph graph);
    }
}
