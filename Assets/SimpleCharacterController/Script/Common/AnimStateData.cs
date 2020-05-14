﻿using UnityEngine;

namespace JT
{
    public class AnimStateData : MonoBehaviour
    {
        public Vector3 position;
        public float rotation;
        public float moveYaw;

        public Vector2 locomotionVector;
        public float locomotionPhase;

        public LocomotionState charLocoState;
        public LocomotionState previousCharLocoState;

        public Vector2 footIkOffset;
        public Vector3 footIkNormalLeft;
        public Vector3 footIkNormaRight;

        public float moveAngleLocal;
    }
}
