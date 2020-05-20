using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace JT
{
    [CreateAssetMenu(fileName = "AnimGraph_TwoBoneIk", menuName = "SimpleCharacterController/AnimGraph/TwoBoneIk")]
    public class AnimGraphTwoBoneIk : AnimGraphAsset
    {
        class Instance : IAnimGraphInstance
        {
            AnimationScriptPlayable m_IkPlayable;

            public Instance(AnimStateController controller, PlayableGraph graph, AnimGraphTwoBoneIk settings)
            {
                var skeleton = controller.GetComponent<Skeleton>();
                var targetBone = skeleton.bones[skeleton.GetBoneIndex(settings.targetBone.GetHashCode())];
                var drivenBone = skeleton.bones[skeleton.GetBoneIndex(settings.drivenBone.GetHashCode())];

                var ikSettings = new TwoBoneIKJob.IkChain();
                ikSettings.target.target = targetBone;
                ikSettings.target.readFrom = TwoBoneIKJob.TargetType.Stream;
                ikSettings.driven.type = TwoBoneIKJob.IkType.Generic;
                ikSettings.driven.genericEndJoint = drivenBone;
                var leftArmIkJob = new TwoBoneIKJob();
                leftArmIkJob.Setup(controller.GetComponent<Animator>(), ikSettings, typeof(AnimStateController),
                    "leftArmIK.weight.value", "leftArmIK.weight.propertyOffset", "leftArmIK.target.offset");
                m_IkPlayable = AnimationScriptPlayable.Create(graph, leftArmIkJob, 1);
                m_IkPlayable.SetInputWeight(0, 1);
            }

            public void ApplyPresentationState(float deltaTime)
            {
            }

            public void GetPlayableOutput(int portId, ref Playable playable, ref int playablePort)
            {
                playable = m_IkPlayable;
                playablePort = 0;
            }

            public void SetPlayableInput(int portId, Playable playable, int playablePort)
            {
                m_IkPlayable.ConnectInput(0, playable, playablePort, 1.0f);
            }

            public void Shutdown()
            {
            }
        }

        public string targetBone;
        public string drivenBone;

        public override IAnimGraphInstance Instatiate(AnimStateController controller, PlayableGraph graph)
        {
            var animState = new Instance(controller, graph, this);
            return animState;
        }
    }
}
