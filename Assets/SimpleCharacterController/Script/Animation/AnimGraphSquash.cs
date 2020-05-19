using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace JT
{
    [CreateAssetMenu(fileName = "AnimGraph_Squash", menuName = "SimpleCharacterController/AnimGraph/Squash")]
    public class AnimGraphSquash : AnimGraphAsset
    {
        class Instance : IAnimGraphInstance, IGraphLogic
        {
            PlayableGraph m_Graph;
            AnimGraphSquash m_Settings;

            AnimStateData m_AnimState;
            LogicStateData m_PredictedState;

            AnimationClipPlayable m_AnimSquash;
            AnimationLayerMixerPlayable m_Mixer;

            float m_PlaySpeed;
            float m_PrevMoveAngle;
            float m_LastDirChangeTime;

            public Instance(AnimStateController controller, PlayableGraph graph, AnimGraphSquash settings)
            {
                m_Graph = graph;
                m_Settings = settings;

                m_AnimState = controller.GetComponent<AnimStateData>();
                m_PredictedState = controller.GetComponent<LogicStateData>();

                m_Mixer = AnimationLayerMixerPlayable.Create(graph, 2);

                m_AnimSquash = AnimationClipPlayable.Create(graph, settings.animSquash);
                m_AnimSquash.SetApplyFootIK(false);
                m_AnimSquash.SetDuration(settings.animSquash.length);
                m_AnimSquash.Pause();
                graph.Connect(m_AnimSquash, 0, m_Mixer, 1);
                m_Mixer.SetInputWeight(1, 0.0f);
                m_Mixer.SetLayerAdditive(1, true);
            }

            public void ApplyPresentationState(float deltaTime)
            {
                m_Mixer.SetInputWeight(1, m_AnimState.squashWeight);
                m_AnimSquash.SetTime(m_AnimState.squashTime);
            }

            public void GetPlayableOutput(int portId, ref Playable playable, ref int playablePort)
            {
                playable = m_Mixer;
                playablePort = 0;
            }

            public void SetPlayableInput(int portId, Playable playable, int playablePort)
            {
                m_Graph.Connect(playable, playablePort, m_Mixer, 0);
                m_Mixer.SetInputWeight(0, 1.0f);
            }

            public void Shutdown()
            {
            }

            public void UpdateGraphLogic(float deltaTime)
            {
                bool isNotSquashing = Math.Abs(m_AnimState.squashTime) < 0.001f || m_AnimState.squashTime >= m_AnimSquash.GetDuration();

                if (m_AnimState.previousCharLocoState != m_AnimState.charLocoState)
                {
                    if (m_AnimState.previousCharLocoState == LocomotionState.InAir)
                    {
                        m_AnimState.squashTime = 0;

                        var vel = -m_PredictedState.velocity.y;
                        var t = vel < m_Settings.landMinFallSpeed ? 0 :
                            vel > m_Settings.landMaxFallSpeed ? 1 :
                            (vel - m_Settings.landMinFallSpeed) / (m_Settings.landMaxFallSpeed - m_Settings.landMinFallSpeed);

                        m_AnimState.squashWeight = Mathf.Lerp(m_Settings.landMin.weight, m_Settings.landMax.weight, t);
                        m_PlaySpeed = Mathf.Lerp(m_Settings.landMin.playSpeed, m_Settings.landMax.playSpeed, t);
                    }
                    else if (isNotSquashing)
                    {
                        if (m_AnimState.charLocoState == LocomotionState.Stand)
                        {
                            m_AnimState.squashTime = 0;
                            m_AnimState.squashWeight = m_Settings.stop.weight;
                            m_PlaySpeed = m_Settings.stop.playSpeed;
                        }
                        else if (m_AnimState.charLocoState == LocomotionState.GroundMove)
                        {
                            m_AnimState.squashTime = 0;
                            m_AnimState.squashWeight = m_Settings.start.weight;
                            m_PlaySpeed = m_Settings.start.playSpeed;
                        }
                    }
                }
                else if (m_AnimState.charLocoState == LocomotionState.GroundMove &&
                        Mathf.Abs(Mathf.DeltaAngle(m_AnimState.moveAngleLocal, m_PrevMoveAngle)) > m_Settings.dirChangeMinAngle)
                {
                    if (isNotSquashing && Time.time - m_LastDirChangeTime > m_Settings.dirChangeTimePenalty)
                    {
                        m_AnimState.squashTime = 0;
                        m_AnimState.squashWeight = m_Settings.changeDir.weight;
                        m_PlaySpeed = m_Settings.changeDir.playSpeed;
                    }

                    m_LastDirChangeTime = Time.time;
                }

                if (m_AnimState.squashWeight > 0)
                {
                    m_AnimState.squashTime += m_PlaySpeed * deltaTime;
                    if (m_AnimState.squashTime > m_AnimSquash.GetDuration())
                        m_AnimState.squashWeight = 0.0f;
                }

                m_PrevMoveAngle = m_AnimState.moveAngleLocal;
            }
        }

        [Serializable]
        public struct PlaySettings
        {
            [Range(0f, 2f)]
            public float weight;
            public float playSpeed;
        }

        public AnimationClip animSquash;

        public PlaySettings landMin;
        public float landMinFallSpeed = 2;
        public PlaySettings landMax;
        public float landMaxFallSpeed = 5;

        public PlaySettings stop;
        public PlaySettings start;

        public PlaySettings changeDir;
        public float dirChangeMinAngle;
        public float dirChangeTimePenalty;

        public override IAnimGraphInstance Instatiate(AnimStateController controller, PlayableGraph graph)
        {
            var animState = new Instance(controller, graph, this);
            return animState;
        }
    }
}
