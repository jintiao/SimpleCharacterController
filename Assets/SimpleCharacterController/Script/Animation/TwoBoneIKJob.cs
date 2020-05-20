using System;
using UnityEngine;
using UnityEngine.Animations;

namespace JT
{
    public struct TwoBoneIKJob : IAnimationJob
    {
        TransformStreamHandle m_EndHandle;
        TransformStreamHandle m_StartHandle;
        TransformStreamHandle m_MidHandle;
        TransformSceneHandle m_EffectorSceneHandle;
        TransformStreamHandle m_EffectorStreamHandle;
        PropertySceneHandle m_WeightHandle;
        PropertyStreamHandle m_AnimatorWeight;
        PropertySceneHandle m_AnimatorWeightOffset;

        IkType m_IkType;
        AvatarIKGoal m_HumanLimb;

        Vector3 m_TargetOffset;
        //    float m_TargetOffsetX;
        //    float m_TargetOffsetY;
        //    float m_TargetOffsetZ;
        //    
        bool m_UseStreamEffector;
        bool m_UseAnimatorProperty;

        public enum TargetType
        {
            Scene,
            Stream
        }

        public enum IkType
        {
            Generic,
            Humanoid
        }

        [Serializable]
        public struct WeightProperty
        {
            [Range(0f, 1f)]
            public float value;
            public bool useAnimatorProperty;
            public string propertyName;
            [Range(-1f, 1f)]
            public float propertyOffset;
        }

        [Serializable]
        public struct Target
        {
            public Transform target;
            public TargetType readFrom;
            public Vector3 offset;
        }

        [Serializable]
        public struct Driven
        {
            public IkType type;
            public Transform genericEndJoint;
            public AvatarIKGoal humanoidLimb;
        }


        [Serializable]
        public struct IkChain
        {
            public Target target;
            public Driven driven;
            public WeightProperty weight;

            public bool HasValidData()
            {
                if (driven.type == IkType.Generic)
                {
                    return target.target != null && driven.genericEndJoint != null && driven.genericEndJoint.parent != null && driven.genericEndJoint.parent.parent != null;
                }
                if (driven.type == IkType.Humanoid)
                {
                    return target.target != null;
                }

                return true;
            }
        }

        public bool Setup(Animator animator, IkChain chain, Type componentType, string weightProperty, string weightOffsetProperty, string targetOffsetProperty)
        {
            if (!chain.HasValidData())
                return false;

            // Target
            m_TargetOffset = chain.target.offset;

            if (chain.target.readFrom == TargetType.Stream)
            {
                m_EffectorStreamHandle = animator.BindStreamTransform(chain.target.target);
                m_UseStreamEffector = true;
            }
            else
            {
                m_EffectorSceneHandle = animator.BindSceneTransform(chain.target.target);
                m_UseStreamEffector = false;
            }


            // Weight
            if (chain.weight.useAnimatorProperty && chain.weight.propertyName != "")
            {
                m_AnimatorWeight = animator.BindStreamProperty(animator.transform, typeof(Animator), chain.weight.propertyName);
                m_UseAnimatorProperty = true;
            }

            m_WeightHandle = animator.BindSceneProperty(animator.transform, componentType, weightProperty);
            m_AnimatorWeightOffset = animator.BindSceneProperty(animator.transform, componentType, weightOffsetProperty);


            // Driven
            m_IkType = chain.driven.type;

            if (m_IkType == IkType.Generic)
            {
                var end = chain.driven.genericEndJoint;
                var mid = end.parent;
                var start = mid.parent;

                m_StartHandle = animator.BindStreamTransform(start);
                m_MidHandle = animator.BindStreamTransform(mid);
                m_EndHandle = animator.BindStreamTransform(end);
            }
            else
            {
                m_HumanLimb = chain.driven.humanoidLimb;
            }

            return true;
        }

        public void ProcessRootMotion(AnimationStream stream) { }

        public void ProcessAnimation(AnimationStream stream)
        {
            float weight;
            if (m_UseAnimatorProperty)
            {
                weight = m_AnimatorWeight.GetFloat(stream);
                weight += m_AnimatorWeightOffset.GetFloat(stream);
                weight = Mathf.Clamp01(weight);
                //m_WeightHandle.SetFloat(stream, weight);
            }
            else
            {
                weight = m_WeightHandle.GetFloat(stream);
            }

            weight = 1f;

            Vector3 effectorPosition;
            Quaternion effectorRotation;
            if (m_UseStreamEffector)
            {
                effectorPosition = m_EffectorStreamHandle.GetPosition(stream);
                effectorRotation = m_EffectorStreamHandle.GetRotation(stream);
            }
            else
            {
                effectorPosition = m_EffectorSceneHandle.GetPosition(stream);
                effectorRotation = m_EffectorSceneHandle.GetRotation(stream);
            }

            effectorRotation *= Quaternion.Euler(m_TargetOffset);

            if (m_IkType == IkType.Generic)
            {
                SolveTwoBoneIK(stream, m_StartHandle, m_MidHandle, m_EndHandle, effectorPosition, effectorRotation, weight, weight);
            }

            else if (m_IkType == IkType.Humanoid)
            {
                if (stream.isHumanStream)
                {
                    var humanStream = stream.AsHuman();

                    humanStream.SetGoalPosition(m_HumanLimb, effectorPosition);
                    humanStream.SetGoalRotation(m_HumanLimb, effectorRotation);
                    humanStream.SetGoalWeightPosition(m_HumanLimb, weight);
                    humanStream.SetGoalWeightRotation(m_HumanLimb, weight);
                    humanStream.SolveIK();
                }
            }
        }

        static void SolveTwoBoneIK (
              AnimationStream stream,
              TransformStreamHandle topHandle,
              TransformStreamHandle midHandle,
              TransformStreamHandle lowHandle,
              Vector3 effectorPosition,
              Quaternion effectorRotation,
              float posWeight,
              float rotWeight)
        {
            Quaternion aRotation = topHandle.GetRotation(stream);
            Quaternion bRotation = midHandle.GetRotation(stream);

            Vector3 aPosition = topHandle.GetPosition(stream);
            Vector3 bPosition = midHandle.GetPosition(stream);
            Vector3 cPosition = lowHandle.GetPosition(stream);

            Vector3 ab = bPosition - aPosition;
            Vector3 bc = cPosition - bPosition;
            Vector3 ac = cPosition - aPosition;
            Vector3 ad = (Vector3.Lerp(cPosition, effectorPosition, posWeight) - aPosition);

            float oldAbcAngle = TriangleAngle(ac.magnitude, ab, bc);
            float newAbcAngle = TriangleAngle(ad.magnitude, ab, bc);

            Vector3 axis = Vector3.Cross(ab, bc).normalized;
            float a = 0.5f * (oldAbcAngle - newAbcAngle);
            float sin = Mathf.Sin(a);
            float cos = Mathf.Cos(a);
            Quaternion q = new Quaternion(axis.x * sin, axis.y * sin, axis.z * sin, cos);

            Quaternion worldQ = q * bRotation;
            midHandle.SetRotation(stream, worldQ);

            aRotation = topHandle.GetRotation(stream);
            cPosition = lowHandle.GetPosition(stream);
            ac = cPosition - aPosition;
            Quaternion fromTo = Quaternion.FromToRotation(ac, ad);
            topHandle.SetRotation(stream, fromTo * aRotation);
            lowHandle.SetRotation(stream, Quaternion.Lerp(lowHandle.GetRotation(stream), effectorRotation, rotWeight));
        }

        static float TriangleAngle(float aLen, Vector3 v1, Vector3 v2)
        {
            float aLen1 = v1.magnitude;
            float aLen2 = v2.magnitude;
            float c = Mathf.Clamp((aLen1 * aLen1 + aLen2 * aLen2 - aLen * aLen) / (aLen1 * aLen2) / 2.0f, -1.0f, 1.0f);
            return Mathf.Acos(c);
        }
    }
}
