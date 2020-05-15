using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace JT
{
    [CreateAssetMenu(fileName = "AnimGraph_Move8Dir", menuName = "SimpleCharacterController/AnimGraph/Move8Dir")]
    public class AnimGraphMove3Dir : AnimGraphAsset
    {
        class Instance : IAnimGraphInstance, IGraphState
        {
            enum Direction
            {
                Forward,
                ForwardLeft,
                ForwardRight,
            }

            AnimStateData m_AnimState;
            AnimationClipPlayable[] m_MovementClips;
            AnimationMixerPlayable m_MovementMixer;
            float m_PlaySpeed;

            public Instance(AnimStateController controller, PlayableGraph graph, AnimGraphMove3Dir settings)
            {
                m_AnimState = controller.GetComponent<AnimStateData>();

                m_MovementMixer = AnimationMixerPlayable.Create(graph, 3);

                m_MovementClips = new AnimationClipPlayable[3];
                m_MovementClips[(int)Direction.Forward] = AnimationClipPlayable.Create(graph, settings.animMoveN);
                m_MovementClips[(int)Direction.ForwardLeft] = AnimationClipPlayable.Create(graph, settings.animMoveNW);
                m_MovementClips[(int)Direction.ForwardRight] = AnimationClipPlayable.Create(graph, settings.animMoveNE);
                foreach (var clip in m_MovementClips)
                {
                    clip.SetApplyFootIK(true);
                    clip.SetSpeed(settings.animMovePlaySpeed);

                    graph.Connect(m_MovementClips[(int)Direction.Forward], 0, m_MovementMixer, (int)Direction.Forward);
                    graph.Connect(m_MovementClips[(int)Direction.ForwardLeft], 0, m_MovementMixer, (int)Direction.ForwardLeft);
                    graph.Connect(m_MovementClips[(int)Direction.ForwardRight], 0, m_MovementMixer, (int)Direction.ForwardRight);
                }
            }

            public void ApplyPresentationState(float deltaTime)
            {
                throw new System.NotImplementedException();
            }

            public void GetPlayableOutput(int portId, ref Playable playable, ref int playablePort)
            {
                playable = m_MovementMixer;
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
                throw new System.NotImplementedException();
            }
        }

        public AnimationClip animMoveNW;
        public AnimationClip animMoveN;
        public AnimationClip animMoveNE;
        public float animMovePlaySpeed = 1.0f;

        public override IAnimGraphInstance Instatiate(AnimStateController controller, PlayableGraph graph)
        {
            var animState = new Instance(controller, graph, this);
            return animState;
        }
    }
}
