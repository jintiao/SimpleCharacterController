using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace JT
{
    [CreateAssetMenu(fileName = "AnimGraph_Jump", menuName = "SimpleCharacterController/AnimGraph/Jump")]
    public class AnimGraphJump : AnimGraphAsset
    {
        class Instance : IAnimGraphInstance, IGraphState
        {
            AnimStateData m_AnimState;
            AnimationClipPlayable m_AnimJump;
            float m_PlaySpeed;

            public Instance(AnimStateController controller, PlayableGraph graph, AnimGraphJump settings)
            {
                m_AnimState = controller.GetComponent<AnimStateData>();

                m_AnimJump = AnimationClipPlayable.Create(graph, settings.animJump);
                m_AnimJump.SetApplyFootIK(true);
                m_AnimJump.SetDuration(settings.animJump.length);
                m_AnimJump.Pause();

                var gameJumpHeight = 1.0f;
                var gameJumpDuration = 0.3f;
                var animJumpVel = settings.jumpHeight / settings.animJump.length;
                var characterJumpVel = gameJumpHeight / gameJumpDuration;
                m_PlaySpeed = characterJumpVel / animJumpVel;
            }

            public void ApplyPresentationState(float deltaTime)
            {
                m_AnimJump.SetTime(m_AnimState.jumpTime);
            }

            public void GetPlayableOutput(int portId, ref Playable playable, ref int playablePort)
            {
                playable = m_AnimJump;
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
                    m_AnimState.jumpTime = 0;
                else
                    m_AnimState.jumpTime += m_PlaySpeed * deltaTime;
            }
        }

        public AnimationClip animJump;
        public float jumpHeight = 1.7f;

        public override IAnimGraphInstance Instatiate(AnimStateController controller, PlayableGraph graph)
        {
            var animState = new Instance(controller, graph, this);
            return animState;
        }
    }
}
