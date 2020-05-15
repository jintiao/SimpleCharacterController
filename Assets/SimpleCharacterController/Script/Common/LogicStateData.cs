using UnityEngine;

namespace JT
{

    public class LogicStateData : MonoBehaviour
    {
        public Vector3 position;
        public Vector3 velocity;

        public LocomotionState locoState;
        public float locoStartTime;

        public int jumpCount;

        public int sprinting;

        public float altitude;
        public Collider groundCollider;
        public Vector3 groundNormal;

        public bool isOnGround => (locoState == LocomotionState.Stand || locoState == LocomotionState.GroundMove);
    }
}