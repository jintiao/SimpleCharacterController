using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace JT
{
    [CreateAssetMenu(fileName = "AnimGraph_Stand", menuName = "SimpleCharacterController/AnimGraph/Stand")]
    public class AnimGraphStand : AnimGraphAsset
    {
        class Instance : IAnimGraphInstance, IGraphState
        {
            public struct FootFalls
            {
                public float leftFootUp;
                public float leftFootDown;
                public float rightFootUp;
                public float rightFootDown;
            }

            enum LocoMixerPort
            {
                Idle,
                TurnL,
                TurnR,
                Count
            }

            enum AimMixerPort
            {
                AimLeft,
                AimMid,
                AimRight,
                Count
            }

            enum FootFallType
            {
                LeftFootUp,
                LeftFootDown,
                RightFootUp,
                RightFootDown
            }

            enum StandState
            {
                Moving,
                Standing,
                Turning,
                TurnStart,
                TurnEnd,
            }

            AnimGraphStand m_Settings;
            AnimStateData m_AnimState;
            LogicStateData m_PredictedState;

            AnimationScriptPlayable m_FootIk;

            AnimationMixerPlayable m_LocomotionMixer;
            AnimationClipPlayable m_AnimIdle;
            AnimationClipPlayable m_AnimTurnL;
            AnimationClipPlayable m_AnimTurnR;

            AnimationMixerPlayable m_AimMixer;
            AnimationClipPlayable m_AnimAimLeft;
            AnimationClipPlayable m_AnimAimMid;
            AnimationClipPlayable m_AnimAimRight;

            AnimationLayerMixerPlayable m_AdditiveMixer;

            Vector3 m_LeftFootPos;
            Vector3 m_RightFootPos;

            int m_Mask;

            RaycastHit m_LeftHit;
            RaycastHit m_RightHit;
            bool m_LeftHitSuccess;
            bool m_RightHitSuccess;
            StandState m_StandState;

            FootFalls m_LeftTurnFootFalls;
            FootFalls m_RightTurnFootFalls;

            Vector2 m_TurnStartOffset;
            Vector2 m_TurnEndOffset;
            Vector3[] m_TurnStartNormals = new Vector3[2];
            Vector3[] m_TurnEndNormals = new Vector3[2];

            SimpleTranstion<AnimationMixerPlayable> m_Transition;

            public Instance(AnimStateController controller, PlayableGraph graph, AnimGraphStand settings)
            {
                m_Settings = settings;
                m_AnimState = controller.GetComponent<AnimStateData>();
                m_PredictedState = controller.GetComponent<LogicStateData>();

                m_Mask = 1 << LayerMask.NameToLayer("Default") | 1 << LayerMask.NameToLayer("Platform");

                m_LocomotionMixer = AnimationMixerPlayable.Create(graph, (int)LocoMixerPort.Count);

                m_AnimIdle = AnimationClipPlayable.Create(graph, settings.animIdle);
                graph.Connect(m_AnimIdle, 0, m_LocomotionMixer, (int)LocoMixerPort.Idle);
                m_LocomotionMixer.SetInputWeight((int)LocoMixerPort.Idle, 1.0f);

                m_AnimTurnL = CreateTurnPlayable(graph, settings.animTurnL, m_LocomotionMixer, LocoMixerPort.TurnL);
                m_AnimTurnR = CreateTurnPlayable(graph, settings.animTurnR, m_LocomotionMixer, LocoMixerPort.TurnR);

                var ports = new int[] { (int)LocoMixerPort.Idle, (int)LocoMixerPort.TurnL, (int)LocoMixerPort.TurnR };
                m_Transition = new SimpleTranstion<AnimationMixerPlayable>(m_LocomotionMixer, ports);

                if (settings.animTurnL.events.Length != 0)
                {
                    m_LeftTurnFootFalls = ExtractFootFalls(settings.animTurnL);
                    m_RightTurnFootFalls = ExtractFootFalls(settings.animTurnR);
                }

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

                m_AimMixer = AnimationMixerPlayable.Create(graph, (int)AimMixerPort.Count, true);

                m_AnimAimLeft = CreateAimPlayable(graph, settings.animAimLeft, m_AimMixer, AimMixerPort.AimLeft);
                m_AnimAimMid = CreateAimPlayable(graph, settings.animAimMid, m_AimMixer, AimMixerPort.AimMid);
                m_AnimAimRight = CreateAimPlayable(graph, settings.animAimRight, m_AimMixer, AimMixerPort.AimRight);

                m_AdditiveMixer = AnimationLayerMixerPlayable.Create(graph);

                var locoMixerPort = m_AdditiveMixer.AddInput(m_FootIk, 0);
                m_AdditiveMixer.SetInputWeight(locoMixerPort, 1);

                var aimMixerPort = m_AdditiveMixer.AddInput(m_AimMixer, 0);
                m_AdditiveMixer.SetInputWeight(aimMixerPort, 1);
                m_AdditiveMixer.SetLayerAdditive((uint)aimMixerPort, true);
            }

            static AnimationClipPlayable CreateTurnPlayable(PlayableGraph graph, AnimationClip clip, AnimationMixerPlayable mixer, LocoMixerPort mixerPort)
            {
                AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clip);
                playable.SetApplyFootIK(true);
                playable.Pause();
                playable.SetDuration(clip.length);

                graph.Connect(playable, 0, mixer, (int)mixerPort);
                mixer.SetInputWeight((int)mixerPort, 0.0f);

                return playable;
            }

            static FootFalls ExtractFootFalls(AnimationClip animation)
            {
                var footFalls = new FootFalls();
                foreach (var e in animation.events)
                {
                    if (e.functionName != "OnCharEvent")
                        continue;

                    switch (e.stringParameter)
                    {
                        case nameof(FootFallType.LeftFootDown):
                            footFalls.leftFootDown = e.time;
                            break;
                        case nameof(FootFallType.LeftFootUp):
                            footFalls.leftFootUp = e.time;
                            break;
                        case nameof(FootFallType.RightFootDown):
                            footFalls.rightFootDown = e.time;
                            break;
                        case nameof(FootFallType.RightFootUp):
                            footFalls.rightFootUp = e.time;
                            break;
                    }
                }

                return footFalls;
            }

            AnimationClipPlayable CreateAimPlayable(PlayableGraph graph, AnimationClip clip, AnimationMixerPlayable mixer, AimMixerPort mixerPort)
            {
                AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clip);
                playable.Pause();
                playable.SetDuration(clip.length);
                graph.Connect(playable, 0, mixer, (int)mixerPort);
                return playable;
            }

            public void ApplyPresentationState(float deltaTime)
            {
                float rotateAngleRemaining = 0f;
                if (m_AnimState.turnDirection != 0)
                    rotateAngleRemaining = Mathf.DeltaAngle(m_AnimState.rotation, m_AnimState.turnStartAngle) + m_Settings.animTurnAngle * m_AnimState.turnDirection;

                if (m_AnimState.turnDirection == 0)
                {
                    m_Transition.Update((int)LocoMixerPort.Idle, m_Settings.turnTransitionSpeed, Time.deltaTime);
                }
                else
                {
                    var fraction = 1f - Mathf.Abs(rotateAngleRemaining / m_Settings.animTurnAngle);
                    var mixerPort = (m_AnimState.turnDirection == -1) ? (int)LocoMixerPort.TurnL : (int)LocoMixerPort.TurnR;
                    var anim = (m_AnimState.turnDirection == -1) ? m_AnimTurnL : m_AnimTurnR;

                    m_Transition.Update(mixerPort, m_Settings.turnTransitionSpeed, Time.deltaTime);
                    anim.SetTime(anim.GetAnimationClip().length * fraction);

                    if (m_LocomotionMixer.GetInputWeight((int)LocoMixerPort.Idle) < 0.01f)
                        m_AnimIdle.SetTime(0f);
                }


                float aimPitchFraction = m_AnimState.aimPitch / 180.0f;
                m_AnimAimLeft.SetTime(aimPitchFraction * m_AnimAimLeft.GetDuration());
                m_AnimAimMid.SetTime(aimPitchFraction * m_AnimAimMid.GetDuration());
                m_AnimAimRight.SetTime(aimPitchFraction * m_AnimAimRight.GetDuration());

                float aimYawLocal = Mathf.DeltaAngle(m_AnimState.rotation, m_AnimState.aimYaw);
                float aimYawFraction = Mathf.Abs(aimYawLocal / m_Settings.aimYawAngle);

                m_AimMixer.SetInputWeight((int)AimMixerPort.AimMid, 1.0f - aimYawFraction);
                if (aimYawLocal < 0)
                {
                    m_AimMixer.SetInputWeight((int)AimMixerPort.AimLeft, aimYawFraction);
                    m_AimMixer.SetInputWeight((int)AimMixerPort.AimRight, 0.0f);
                }
                else
                {
                    m_AimMixer.SetInputWeight((int)AimMixerPort.AimLeft, 0.0f);
                    m_AimMixer.SetInputWeight((int)AimMixerPort.AimRight, aimYawFraction);
                }


                var job = m_FootIk.GetJobData<FootIkJob>();
                job.normalLeftFoot = m_AnimState.footIkNormalLeft;
                job.normalRightFoot = m_AnimState.footIkNormaRight;
                job.ikOffset = m_AnimState.footIkOffset;
                m_FootIk.SetJobData(job);
            }

            public void GetPlayableOutput(int portId, ref Playable playable, ref int playablePort)
            {
                playable = m_AdditiveMixer;
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
                    m_AnimState.turnDirection = 0;
                    m_AnimState.turnStartAngle = m_AnimState.rotation;
                }

                var aimYawLocal = Mathf.DeltaAngle(m_AnimState.rotation, m_AnimState.aimYaw);
                var absAimYawLocal = Mathf.Abs(aimYawLocal);

                if (m_AnimState.turnDirection == 0)
                {
                    if (absAimYawLocal > m_Settings.aimTurnLocalThreshold)
                    {
                        m_AnimState.turnStartAngle = m_AnimState.rotation;
                        m_AnimState.turnDirection = Mathf.Sign(aimYawLocal);
                    }
                }

                float absAngleRemaining = 0f;
                if (m_AnimState.turnDirection != 0)
                {
                    var rotateAngleRemaining = Mathf.DeltaAngle(m_AnimState.rotation, m_AnimState.turnStartAngle) + m_Settings.animTurnAngle * m_AnimState.turnDirection;

                    if (rotateAngleRemaining * m_AnimState.turnDirection <= 0)
                    {
                        m_AnimState.turnDirection = 0;
                    }
                    else
                    {
                        var turnSpeed = m_Settings.turnSpeed;
                        if (absAimYawLocal > m_Settings.turnThreshold)
                        {
                            var factor = 1.0f - (180 - absAimYawLocal) / m_Settings.turnThreshold;
                            turnSpeed = turnSpeed + factor * 300;
                        }

                        var deltaAngle = deltaTime * turnSpeed;
                        absAngleRemaining = Mathf.Abs(rotateAngleRemaining);
                        if (deltaAngle > absAngleRemaining)
                        {
                            deltaAngle = absAngleRemaining;
                        }

                        var sign = Mathf.Sign(rotateAngleRemaining);

                        m_AnimState.rotation += sign * deltaAngle;
                        while (m_AnimState.rotation > 360.0f)
                            m_AnimState.rotation -= 360.0f;
                        while (m_AnimState.rotation < 0.0f)
                            m_AnimState.rotation += 360.0f;
                    }
                }


                if (m_Settings.footIK.enabled)
                {
                    var footIkJob = m_FootIk.GetJobData<FootIkJob>();
                    if (m_PredictedState.velocity.magnitude > 0.001f)
                        m_StandState = StandState.Moving;
                    else if (m_AnimState.turnDirection != 0 && m_StandState != StandState.TurnStart && m_StandState != StandState.Turning)
                        m_StandState = StandState.TurnStart;
                    else if (m_AnimState.turnDirection != 0)
                        m_StandState = StandState.Turning;
                    else if (m_AnimState.turnDirection == 0 && m_StandState == StandState.Turning)
                        m_StandState = StandState.TurnEnd;
                    else
                        m_StandState = StandState.Standing;

                    if (m_StandState == StandState.Moving || firstUpdate)
                    {
                        var rotation = Quaternion.Euler(0f, m_AnimState.rotation, 0f);
                        m_LeftFootPos = rotation * m_Settings.footIK.leftToeStandPos + m_AnimState.position;
                        m_RightFootPos = rotation * m_Settings.footIK.rightToeStandPos + m_AnimState.position;
                    }
                    else if (m_StandState == StandState.TurnStart)
                    {
                        var predictedRotation = Quaternion.Euler(0f, m_AnimState.turnStartAngle + m_Settings.animTurnAngle * m_AnimState.turnDirection, 0f);
                        m_LeftFootPos = predictedRotation * m_Settings.footIK.leftToeStandPos + m_AnimState.position;
                        m_RightFootPos = predictedRotation * m_Settings.footIK.rightToeStandPos + m_AnimState.position;
                    }

                    if (m_StandState == StandState.Moving || m_StandState == StandState.TurnStart)
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

                    if (m_StandState == StandState.Moving || m_StandState == StandState.TurnEnd)
                    {
                        m_AnimState.footIkOffset = GetClampedOffset();
                        m_AnimState.footIkNormalLeft = m_LeftHit.normal;
                        m_AnimState.footIkNormaRight = m_RightHit.normal;

                        m_TurnStartOffset.x = m_AnimState.footIkOffset.x;
                        m_TurnStartOffset.y = m_AnimState.footIkOffset.y;
                        m_TurnStartNormals[0] = m_LeftHit.normal;
                        m_TurnStartNormals[1] = m_RightHit.normal;
                    }
                    else if (m_StandState == StandState.TurnStart)
                    {
                        m_TurnEndOffset = GetClampedOffset();
                        m_TurnEndNormals[0] = m_LeftHit.normal;
                        m_TurnEndNormals[1] = m_RightHit.normal;
                    }

                    if (m_StandState == StandState.TurnStart || m_StandState == StandState.Turning)
                    {
                        var turnFraction = (-absAngleRemaining + m_Settings.animTurnAngle) / m_Settings.animTurnAngle;
                        var footFalls = m_AnimState.turnDirection == -1 ? m_LeftTurnFootFalls : m_RightTurnFootFalls;

                        var leftFootFraction = GetFootFraction(turnFraction, footFalls.leftFootUp, footFalls.leftFootDown);
                        m_AnimState.footIkOffset.x = Mathf.Lerp(m_TurnStartOffset.x, m_TurnEndOffset.x, leftFootFraction);
                        m_AnimState.footIkNormalLeft = Vector3.Lerp(m_TurnStartNormals[0], m_TurnEndNormals[0], leftFootFraction);

                        var rightFootFraction = GetFootFraction(turnFraction, footFalls.rightFootUp, footFalls.rightFootDown);
                        m_AnimState.footIkOffset.y = Mathf.Lerp(m_TurnStartOffset.y, m_TurnEndOffset.y, rightFootFraction);
                        m_AnimState.footIkNormaRight = Vector3.Lerp(m_TurnStartNormals[1], m_TurnEndNormals[1], rightFootFraction);
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

            static float GetFootFraction(float turnFraction, float footUp, float footDown)
            {
                if (turnFraction <= footUp)
                {
                    return 0f;
                }

                if (turnFraction < footDown)
                {
                    return (turnFraction - footUp) / (footDown - footUp);
                }

                return 1f;
            }
        }

        public AnimationClip animIdle;
        public AnimationClip animTurnL;
        public AnimationClip animTurnR;
        public AnimationClip animAimLeft;
        public AnimationClip animAimMid;
        public AnimationClip animAimRight;

        public float animTurnAngle = 90.0f;
        public float aimTurnLocalThreshold = 90;
        public float aimYawAngle = 90;
        public float turnSpeed = 120;
        public float turnThreshold = 120;
        public float turnTransitionSpeed = 16f;

        public FootIkJob.JobSettings footIK;
        public string leftToeBone;
        public string rightToeBone;

        public override IAnimGraphInstance Instatiate(AnimStateController controller, PlayableGraph graph)
        {
            var animState = new Instance(controller, graph, this);
            return animState;
        }
    }
}