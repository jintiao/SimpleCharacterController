using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace JT
{
    [CreateAssetMenu(fileName = "AnimGraph_Move8Dir", menuName = "SimpleCharacterController/AnimGraph/Move8Dir")]
    public class AnimGraphMove8Dir : AnimGraphAsset
    {
        class Instance : IAnimGraphInstance, IGraphState
        {
            AnimGraphMove8Dir m_Settings;
            AnimStateData m_AnimState;

            AnimationLayerMixerPlayable m_LocomotionMixer;
            AnimationClipPlayable m_AnimAim;
            BlendTree2d m_BlendTree;

            float m_AimTimeFactor;

            Vector2 m_CurrentVelocity;
            float m_PlaySpeed;

            public Instance(AnimStateController controller, PlayableGraph graph, AnimGraphMove8Dir settings)
            {
                m_Settings = settings;
                m_AnimState = controller.GetComponent<AnimStateData>();

                m_LocomotionMixer = AnimationLayerMixerPlayable.Create(graph, 3);

                m_BlendTree = new BlendTree2d(graph, settings.blendSpaceNodes);
                m_BlendTree.masterSpeed = settings.animMovePlaySpeed;
                graph.Connect(m_BlendTree.rootPlayable, 0, m_LocomotionMixer, 0);
                m_LocomotionMixer.SetInputWeight(0, 1.0f);

                if (settings.animAim != null)
                {
                    m_AnimAim = AnimationClipPlayable.Create(graph, settings.animAim);
                    m_AnimAim.SetApplyFootIK(false);
                    m_AnimAim.Pause();
                    m_AimTimeFactor = m_AnimAim.GetAnimationClip().length / 180.0f;

                    graph.Connect(m_AnimAim, 0, m_LocomotionMixer, 1);
                    m_LocomotionMixer.SetInputWeight(1, 1.0f);
                    m_LocomotionMixer.SetLayerAdditive(1, true);
                }
            }

            public void ApplyPresentationState(float deltaTime)
            {
                m_BlendTree.UpdateGraph();
                m_BlendTree.SetPhase(m_AnimState.locomotionPhase);

                m_AnimAim.SetTime(m_AnimState.aimPitch * m_AimTimeFactor);
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

            public void UpdatePresentationState(bool firstUpdate, float deltaTime)
            {
#if UNITY_EDITOR
                m_BlendTree.masterSpeed = m_Settings.animMovePlaySpeed;
                m_BlendTree.footIk = m_Settings.enableIK;
#endif

                m_AnimState.rotation = m_AnimState.aimYaw;
                m_AnimState.moveAngleLocal = CalculateMoveAngleLocal(m_AnimState.rotation, m_AnimState.moveYaw);

                var targetBlend = AngleToPosition(m_AnimState.moveAngleLocal);
                m_AnimState.locomotionVector = Vector2.SmoothDamp(m_AnimState.locomotionVector, targetBlend, ref m_CurrentVelocity, m_Settings.damping, m_Settings.maxStep, deltaTime);

                m_PlaySpeed = 1f / m_BlendTree.SetBlendPosition(m_AnimState.locomotionVector, false) * deltaTime;
                m_AnimState.locomotionPhase += m_PlaySpeed;
            }

            static float CalculateMoveAngleLocal(float rotation, float moveYaw)
            {
                // Get new local move angle
                var moveAngleLocal = Mathf.DeltaAngle(rotation, moveYaw);

                // We cant blend running sideways and running backwards so in range 90->135 we snap to either sideways or backwards
                var absMoveAngle = Mathf.Abs(moveAngleLocal);
                if (absMoveAngle > 90 && absMoveAngle < 135)
                {
                    var sign = Mathf.Sign(moveAngleLocal);
                    moveAngleLocal = absMoveAngle > 112.5f ? sign * 135.0f : sign * 90.0f;
                }
                return moveAngleLocal;
            }

            static Vector2 AngleToPosition(float angle)
            {
                var dir3D = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
                return new Vector2(dir3D.x, dir3D.z);
            }
        }

        public float animMovePlaySpeed = 1.0f;
        public float damping = 0.1f;
        public float maxStep = 15f;

        public List<BlendSpaceNode> blendSpaceNodes;
        public AnimationClip animAim;
        public bool enableIK;

        public override IAnimGraphInstance Instatiate(AnimStateController controller, PlayableGraph graph)
        {
            var animState = new Instance(controller, graph, this);
            return animState;
        }
    }
}
