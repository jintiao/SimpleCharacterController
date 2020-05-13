using System;
using UnityEngine;
using UnityEngine.Animations;

namespace JT
{
    public struct FootIkJob : IAnimationJob
    {
        public JobSettings settings;

        public Vector2 ikOffset;
        public Vector3 normalLeftFoot;
        public Vector3 normalRightFoot;

        public TransformStreamHandle leftToe;
        public TransformStreamHandle rightToe;

        public float ikWeight;

        public void ProcessAnimation(AnimationStream stream)
        {
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        [Serializable]
        public struct JobSettings
        {
            public Vector3 leftToeStandPos;
            public Vector3 rightToeStandPos;
            public bool enabled;
        }
    }
}
