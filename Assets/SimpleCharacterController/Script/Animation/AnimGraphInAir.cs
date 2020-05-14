using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace JT
{
    [CreateAssetMenu(fileName = "AnimGraph_InAir", menuName = "SimpleCharacterController/AnimGraph/InAir")]
    public class AnimGraphInAir : AnimGraphAsset
    {
        class Instance : IAnimGraphInstance, IGraphState
        {
            AnimStateData m_AnimState;
            AnimationClipPlayable m_AnimInAir;

            public Instance(AnimStateController controller, PlayableGraph graph, AnimGraphInAir settings)
            {
                m_AnimState = controller.GetComponent<AnimStateData>();

                m_AnimInAir = AnimationClipPlayable.Create(graph, settings.animInAir);
                m_AnimInAir.Play();
                m_AnimInAir.SetApplyFootIK(false);
            }

            public void ApplyPresentationState(float deltaTime)
            {
                m_AnimInAir.SetTime(m_AnimState.inAirTime);
            }

            public void GetPlayableOutput(int portId, ref Playable playable, ref int playablePort)
            {
                playable = m_AnimInAir;
                playablePort = 0;
            }

            public void SetPlayableInput(int portId, Playable playable, int playablePort)
            {
            }

            public void Shutdown()
            {
            }

            public void UpdatePresentationState(bool firstUpdate, float deltaTime)
            {
                if (firstUpdate)
                {
                    m_AnimState.inAirTime = 0;
                }
                else
                {
                    m_AnimState.inAirTime += deltaTime;
                }
            }
        }

        public AnimationClip animInAir;

        public override IAnimGraphInstance Instatiate(AnimStateController controller, PlayableGraph graph)
        {
            var animState = new Instance(controller, graph, this);
            return animState;
        }
    }
}
