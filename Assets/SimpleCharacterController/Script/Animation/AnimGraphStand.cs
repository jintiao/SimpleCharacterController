using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace JT
{
    [CreateAssetMenu(fileName = "AnimGraph_Stand", menuName = "SimpleCharacterController/AnimGraph/Stand")]
    public class AnimGraphStand : AnimGraphAsset
    {
        enum StandState
        {
            Moving,
            Standing,
        }

        public AnimationClip animIdle;
        public FootIkJob.JobSettings footIK;
        public string leftToeBone;
        public string rightToeBone;

        public override IAnimGraphInstance Instatiate(AnimStateController controller, PlayableGraph graph)
        {
            var animState = new Instance(controller, graph, this);
            return animState;
        }

        class Instance : IAnimGraphInstance, IGraphState
        {
            AnimGraphStand m_Settings;
            AnimStateData m_AnimState;
            LogicStateData m_PredictedState;

            AnimationScriptPlayable m_FootIk;
            AnimationMixerPlayable m_LocomotionMixer;
            AnimationClipPlayable m_AnimIdle;

            Vector3 m_LeftFootPos;
            Vector3 m_RightFootPos;

            int m_Mask;

            RaycastHit m_LeftHit;
            RaycastHit m_RightHit;
            bool m_LeftHitSuccess;
            bool m_RightHitSuccess;
            StandState m_StandState;

            public Instance(AnimStateController controller, PlayableGraph graph, AnimGraphStand settings)
            {
                m_Settings = settings;
                m_AnimState = controller.GetComponent<AnimStateData>();
                m_PredictedState = controller.GetComponent<LogicStateData>();

                m_AnimIdle = AnimationClipPlayable.Create(graph, settings.animIdle);

                var animator = controller.GetComponent<Animator>();
                var skeleton = controller.GetComponent<Skeleton>();
                var leftToes = skeleton.bones[skeleton.GetBoneIndex(settings.leftToeBone.GetHashCode())];
                var rightToes = skeleton.bones[skeleton.GetBoneIndex(settings.rightToeBone.GetHashCode())];

                var ikJob = new FootIkJob
                {
                    settings = settings.footIK,
                    leftToe = animator.BindStreamTransform(leftToes),
                    rightToe = animator.BindStreamTransform(rightToes)
                };
                m_FootIk = AnimationScriptPlayable.Create(graph, ikJob, 1);
                graph.Connect(m_AnimIdle, 0, m_FootIk, 0);
                m_FootIk.SetInputWeight(0, 1f);

                m_Mask = 1 << LayerMask.NameToLayer("Default") | 1 << LayerMask.NameToLayer("Platform");
            }

            public void ApplyPresentationState(float deltaTime)
            {
                var job = m_FootIk.GetJobData<FootIkJob>();
                job.normalLeftFoot = m_AnimState.footIkNormalLeft;
                job.normalRightFoot = m_AnimState.footIkNormaRight;
                job.ikOffset = m_AnimState.footIkOffset;
                m_FootIk.SetJobData(job);
            }

            public void GetPlayableOutput(int portId, ref Playable playable, ref int playablePort)
            {
                playable = m_FootIk;
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
                var footIkJob = m_FootIk.GetJobData<FootIkJob>();

                if (m_PredictedState.velocity.magnitude > 0.001f)
                    m_StandState = StandState.Moving;
                else
                    m_StandState = StandState.Standing;

                if (m_Settings.footIK.enabled)
                {
                    if (m_StandState == StandState.Moving || firstUpdate)
                    {
                        var rotation = Quaternion.Euler(0f, m_AnimState.rotation, 0f);
                        m_LeftFootPos = rotation * m_Settings.footIK.leftToeStandPos + m_AnimState.position;
                        m_RightFootPos = rotation * m_Settings.footIK.rightToeStandPos + m_AnimState.position;
                    }

                    if (m_StandState == StandState.Moving)
                    {
                        var rayEmitOffset = Vector3.up * m_Settings.footIK.emitRayOffset;
                        var maxRayDistance = m_Settings.footIK.emitRayOffset + m_Settings.footIK.maxRayDistance;
                        m_LeftHitSuccess = Physics.Raycast(m_LeftFootPos + rayEmitOffset, Vector3.down, out m_LeftHit, maxRayDistance, m_Mask);
                        m_RightHitSuccess = Physics.Raycast(m_RightFootPos + rayEmitOffset, Vector3.down, out m_RightHit, maxRayDistance, m_Mask);
                    }

                    if (firstUpdate)
                    {
                        footIkJob.ikWeight = 0.0f;
                    }

                    if (m_StandState == StandState.Moving)
                    {
                        m_AnimState.footIkOffset = GetClampedOffset();
                        m_AnimState.footIkNormalLeft = m_LeftHit.normal;
                        m_AnimState.footIkNormaRight = m_RightHit.normal;
                    }
                }

#if UNITY_EDITOR
                footIkJob.settings = m_Settings.footIK;
                if (m_Settings.footIK.debugRayCast)
                {
                    DebugDraw.Sphere(m_LeftFootPos, 0.025f, Color.yellow);
                    DebugDraw.Sphere(m_RightFootPos, 0.025f, Color.yellow);

                    DebugDraw.Sphere(m_LeftHit.point, 0.015f);
                    DebugDraw.Sphere(m_RightHit.point, 0.015f);

                    Debug.DrawLine(m_LeftHit.point, m_LeftHit.point + m_LeftHit.normal, Color.green);
                    Debug.DrawLine(m_RightHit.point, m_RightHit.point + m_RightHit.normal, Color.red);
                }
#endif
                m_FootIk.SetJobData(footIkJob);
            }

            Vector2 GetClampedOffset()
            {
                var leftOffset = 0.0f;
                var rightOffset = 0.0f;

                if (m_LeftHitSuccess)
                {
                    leftOffset = Mathf.Clamp(m_LeftHit.point.y - m_LeftFootPos.y + m_Settings.footIK.leftToeStandPos.y, -m_Settings.footIK.maxStepSize, m_Settings.footIK.maxStepSize);
                }

                if (m_RightHitSuccess)
                {
                    rightOffset = Mathf.Clamp(m_RightHit.point.y - m_RightFootPos.y + m_Settings.footIK.rightToeStandPos.y, -m_Settings.footIK.maxStepSize, m_Settings.footIK.maxStepSize);
                }

                var stepMag = Mathf.Abs(leftOffset - rightOffset);

                if (stepMag > m_Settings.footIK.maxStepSize)
                {
                    leftOffset = (leftOffset / stepMag) * m_Settings.footIK.maxStepSize;
                    rightOffset = (rightOffset / stepMag) * m_Settings.footIK.maxStepSize;
                }

                return new Vector2(leftOffset, rightOffset);
            }
        }
    }
}