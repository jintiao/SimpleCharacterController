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
        IGraphState m_AnimGraphState;
        IGraphState m_LastAnimGraphState;
        PlayableGraph m_PlayableGraph;

        void Start()
        {
            m_PlayableGraph = PlayableGraph.Create(name);
            m_AnimGraph = animStateDefinition.Instatiate(this, m_PlayableGraph);
            m_AnimGraphLogic = m_AnimGraph as IGraphLogic;
            m_AnimGraphState = m_AnimGraph as IGraphState;

            m_PlayableGraph.Play();

            var outputPlayable = Playable.Null;
            var outputPort = 0;
            m_AnimGraph.GetPlayableOutput(0, ref outputPlayable, ref outputPort);

            var animator = GetComponentInChildren<Animator>();
            var animationOutput = AnimationPlayableOutput.Create(m_PlayableGraph, "Animator", animator);
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

        private void Update()
        {
            m_AnimGraphLogic?.UpdateGraphLogic(Time.deltaTime);

            m_AnimGraphState?.UpdatePresentationState(m_LastAnimGraphState == m_AnimGraphState, Time.deltaTime);
            m_LastAnimGraphState = m_AnimGraphState;

            m_AnimGraph.ApplyPresentationState(Time.deltaTime);
        }
    }
}
