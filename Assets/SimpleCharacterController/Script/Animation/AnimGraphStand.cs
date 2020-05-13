using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace JT
{
    [CreateAssetMenu(fileName = "AnimGraph_Stand", menuName = "SimpleCharacterController/AnimGraph/Stand")]
    public class AnimGraphStand : AnimGraphAsset
    {
        public AnimationClip animIdle;

        public override IAnimGraphInstance Instatiate(PlayableGraph graph)
        {
            var animState = new Instance(graph, this);
            return animState;
        }

        class Instance : IAnimGraphInstance, IGraphState
        {
            AnimationMixerPlayable m_LocomotionMixer;
            AnimationClipPlayable m_AnimIdle;

            public Instance(PlayableGraph graph, AnimGraphStand settings)
            {
                m_LocomotionMixer = AnimationMixerPlayable.Create(graph, (int)LocoMixerPort.Count);

                m_AnimIdle = AnimationClipPlayable.Create(graph, settings.animIdle);
                graph.Connect(m_AnimIdle, 0, m_LocomotionMixer, (int)LocoMixerPort.Idle);
                m_LocomotionMixer.SetInputWeight((int)LocoMixerPort.Idle, 1.0f);
            }

            public void ApplyPresentationState(GameTime time, float deltaTime)
            {
                throw new System.NotImplementedException();
            }

            public void GetPlayableOutput(int portId, ref Playable playable, ref int playablePort)
            {
                playable = m_LocomotionMixer;
                playablePort = 0;
            }

            public void SetPlayableInput(int portId, Playable playable, int playablePort)
            {
            }

            public void Shutdown()
            {
            }

            public void UpdatePresentationState(bool firstUpdate, GameTime time, float deltaTime)
            {
                throw new System.NotImplementedException();
            }

            enum LocoMixerPort
            {
                Idle,
                TurnL,
                TurnR,
                Count
            }
        }
    }
}