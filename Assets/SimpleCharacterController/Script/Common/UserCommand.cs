using UnityEngine;

namespace JT
{
    public struct UserCommand
    {
        public float moveYaw;
        public float moveMagnitude;
        public float lookYaw;
        public float lookPitch;
        public bool jump;
        public bool boost;

        public static UserCommand defaultCommand = new UserCommand();
    }
}
