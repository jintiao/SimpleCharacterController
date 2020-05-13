using UnityEngine;

namespace JT
{
    public enum LocoState
    {
        Stand,
        GroundMove,
        Jump,
        DoubleJump,
        InAir,
        MaxValue
    }

    public class LogicStateData : MonoBehaviour
    {
        public Vector3 position;
        public Vector3 velocity;
        public LocoState locoState;

        public float altitude;
        public Collider groundCollider;
        public Vector3 groundNormal;

        public bool isOnGround => (locoState == LocoState.Stand || locoState == LocoState.GroundMove);
    }
}