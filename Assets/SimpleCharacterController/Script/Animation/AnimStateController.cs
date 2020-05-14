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

        AnimStateData m_AnimState;
        LogicStateData m_PredictedState;

        void Start()
        {
            m_AnimState = GetComponent<AnimStateData>();
            m_PredictedState = GetComponent<LogicStateData>();

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

        public void UpdateAnim(float deltaTime)
        {
            UpdateAnimState();
            UpdateAnimGraph(deltaTime);
        }

        void UpdateAnimState()
        {
            m_AnimState.position = m_PredictedState.position;
            m_AnimState.previousCharLocoState = m_AnimState.charLocoState;

            var groundMoveVec = Vector3.ProjectOnPlane(m_PredictedState.velocity, Vector3.up);
            m_AnimState.moveYaw = Vector3.Angle(Vector3.forward, groundMoveVec);
            var cross = Vector3.Cross(Vector3.forward, groundMoveVec);
            if (cross.y < 0)
                m_AnimState.moveYaw = 360 - m_AnimState.moveYaw;
        }

        void UpdateAnimGraph(float deltaTime)
        {
            m_AnimGraphLogic?.UpdateGraphLogic(deltaTime);

            m_AnimGraphState?.UpdatePresentationState(m_LastAnimGraphState != m_AnimGraphState, deltaTime);
            m_LastAnimGraphState = m_AnimGraphState;

            m_AnimGraph.ApplyPresentationState(deltaTime);
        }
    }
}
