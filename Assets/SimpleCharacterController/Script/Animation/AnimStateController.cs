using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace JT
{
    public class AnimStateController : MonoBehaviour
    {
        [SerializeField]
        public AnimGraphAsset animStateDefinition;

        IAnimGraphInstance m_AnimGraph;
        IGraphLogic m_AnimGraphLogic;
        Animator m_Animator;
        PlayableGraph m_PlayableGraph;

        void Start()
        {
            m_Animator = GetComponentInChildren<Animator>();
            m_PlayableGraph = PlayableGraph.Create(name);
            m_AnimGraph = animStateDefinition.Instatiate(m_PlayableGraph);
            m_AnimGraphLogic = m_AnimGraph as IGraphLogic;

            m_PlayableGraph.Play();

            var outputPlayable = Playable.Null;
            var outputPort = 0;
            m_AnimGraph.GetPlayableOutput(0, ref outputPlayable, ref outputPort);

            var animationOutput = AnimationPlayableOutput.Create(m_PlayableGraph, "Animator", m_Animator);
            animationOutput.SetSourcePlayable(outputPlayable, outputPort);
        }

        void OnDisable()
        {
            if (m_PlayableGraph.IsValid())
            {
                m_AnimGraph.Shutdown();
                m_PlayableGraph.Destroy();
            }
        }
    }
}
