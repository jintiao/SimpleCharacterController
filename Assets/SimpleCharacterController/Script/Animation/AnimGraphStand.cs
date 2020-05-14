using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace JT
{
    [CreateAssetMenu(fileName = "AnimGraph_Stand", menuName = "SimpleCharacterController/AnimGraph/Stand")]
    public class AnimGraphStand : AnimGraphAsset
    {
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

            AnimationScriptPlayable m_FootIk;
            AnimationMixerPlayable m_LocomotionMixer;
            AnimationClipPlayable m_AnimIdle;

            Vector3 m_LeftFootPos;
            Vector3 m_RightFootPos;

            public Instance(AnimStateController controller, PlayableGraph graph, AnimGraphStand settings)
            {
                m_Settings = settings;
                m_AnimState = controller.GetComponent<AnimStateData>();

                m_LocomotionMixer = AnimationMixerPlayable.Create(graph, (int)LocoMixerPort.Count);

                m_AnimIdle = AnimationClipPlayable.Create(graph, settings.animIdle);
                graph.Connect(m_AnimIdle, 0, m_LocomotionMixer, (int)LocoMixerPort.Idle);
                m_LocomotionMixer.SetInputWeight((int)LocoMixerPort.Idle, 1.0f);

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
                graph.Connect(m_LocomotionMixer, 0, m_FootIk, 0);
                m_FootIk.SetInputWeight(0, 1f);
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
                if (m_Settings.footIK.enabled)
                {
                    if (firstUpdate)
                    {
                        var rotation = Quaternion.Euler(0f, m_AnimState.rotation, 0f);
                        m_LeftFootPos = rotation * m_Settings.footIK.leftToeStandPos + m_AnimState.position;
                        m_RightFootPos = rotation * m_Settings.footIK.rightToeStandPos + m_AnimState.position;
                    }

                    if (firstUpdate)
                    {
                        footIkJob.ikWeight = 0.0f;
                    }
                }

#if UNITY_EDITOR
                footIkJob.settings = m_Settings.footIK;
#endif
                m_FootIk.SetJobData(footIkJob);
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