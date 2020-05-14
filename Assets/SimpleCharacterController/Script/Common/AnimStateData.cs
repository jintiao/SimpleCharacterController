using UnityEngine;

namespace JT
{
    public class AnimStateData : MonoBehaviour
    {
        public Vector3 position;
        public float rotation;
        public float moveYaw;

        public LocoState charLocoState;
        public LocoState previousCharLocoState;

        public Vector2 footIkOffset;
        public Vector3 footIkNormalLeft;
        public Vector3 footIkNormaRight;
    }
}
