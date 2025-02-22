﻿using UnityEngine;

namespace JT
{
    public class AnimStateData : MonoBehaviour
    {
        public Vector3 position;
        public float rotation;
        public float moveYaw;
        public float aimYaw;
        public float aimPitch;

        public Vector2 locomotionVector;
        public float locomotionPhase;

        public LocomotionState charLocoState;
        public LocomotionState previousCharLocoState;

        public float moveAngleLocal;

        public float turnStartAngle;
        public float turnDirection;

        public float inAirTime;
        public float jumpTime;

        public Vector2 footIkOffset;
        public Vector3 footIkNormalLeft;
        public Vector3 footIkNormaRight; 
        
        public float squashTime;
        public float squashWeight;
    }
}
