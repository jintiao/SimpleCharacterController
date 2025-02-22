﻿using UnityEngine;
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

            AnimationLayerMixerPlayable m_Mixer;
            AnimationClipPlayable m_AnimJump;
            AnimationClipPlayable m_AnimAim;

            float m_PlaySpeed;
            float m_AimTimeFactor;

            public Instance(AnimStateController controller, PlayableGraph graph, AnimGraphJump settings)
            {
                m_AnimState = controller.GetComponent<AnimStateData>();

                m_Mixer = AnimationLayerMixerPlayable.Create(graph, 2);

                m_AnimJump = AnimationClipPlayable.Create(graph, settings.animJump);
                m_AnimJump.SetApplyFootIK(true);
                m_AnimJump.SetDuration(settings.animJump.length);
                m_AnimJump.Pause(); 
                graph.Connect(m_AnimJump, 0, m_Mixer, 0);
                m_Mixer.SetInputWeight(0, 1);

                var gameJumpHeight = 1.0f;
                var gameJumpDuration = 0.3f;
                var animJumpVel = settings.jumpHeight / settings.animJump.length;
                var characterJumpVel = gameJumpHeight / gameJumpDuration;
                m_PlaySpeed = characterJumpVel / animJumpVel;

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
                m_AnimJump.SetTime(m_AnimState.jumpTime);
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
                    m_AnimState.jumpTime = 0;
                else
                    m_AnimState.jumpTime += m_PlaySpeed * deltaTime;

                m_AnimState.rotation = m_AnimState.aimYaw;
            }
        }

        public AnimationClip animJump;
        public float jumpHeight = 1.7f;
        public AnimationClip animAim;

        public override IAnimGraphInstance Instatiate(AnimStateController controller, PlayableGraph graph)
        {
            var animState = new Instance(controller, graph, this);
            return animState;
        }
    }
}
