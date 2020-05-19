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

            AnimationLayerMixerPlayable m_Mixer;
            AnimationClipPlayable m_AnimInAir;
            AnimationClipPlayable m_AnimAim;

            float m_AimTimeFactor;

            public Instance(AnimStateController controller, PlayableGraph graph, AnimGraphInAir settings)
            {
                m_AnimState = controller.GetComponent<AnimStateData>();

                m_Mixer = AnimationLayerMixerPlayable.Create(graph, 2);

                m_AnimInAir = AnimationClipPlayable.Create(graph, settings.animInAir);
                m_AnimInAir.Play();
                m_AnimInAir.SetApplyFootIK(false);
                graph.Connect(m_AnimInAir, 0, m_Mixer, 0);
                m_Mixer.SetInputWeight(0, 1);

                m_AnimAim = AnimationClipPlayable.Create(graph, settings.animAim);
                m_AnimAim.SetApplyFootIK(false);
                m_AnimAim.Pause();
                graph.Connect(m_AnimAim, 0, m_Mixer, 1);
                m_Mixer.SetInputWeight(1, 1);
                m_Mixer.SetLayerAdditive(1, true);

                m_AimTimeFactor = settings.animAim.length / 180.0f;
            }

            public void ApplyPresentationState(float deltaTime)
            {
                m_AnimInAir.SetTime(m_AnimState.inAirTime);
                m_AnimAim.SetTime(m_AnimState.aimPitch * m_AimTimeFactor);
            }

            public void GetPlayableOutput(int portId, ref Playable playable, ref int playablePort)
            {
                playable = m_Mixer;
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

                m_AnimState.rotation = m_AnimState.aimYaw;
            }
        }

        public AnimationClip animInAir;
        public AnimationClip animAim;

        public override IAnimGraphInstance Instatiate(AnimStateController controller, PlayableGraph graph)
        {
            var animState = new Instance(controller, graph, this);
            return animState;
        }
    }
}
