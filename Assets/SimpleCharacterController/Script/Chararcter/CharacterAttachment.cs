using UnityEngine;

namespace JT
{
    public class CharacterAttachment : MonoBehaviour
    {
        public Transform attachPoint;

        void Update()
        {
            if (attachPoint == null)
                return;

            transform.position = attachPoint.position;
            transform.rotation = attachPoint.rotation;
        }
    }
}