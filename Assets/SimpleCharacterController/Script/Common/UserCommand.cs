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

        public static UserCommand defaultCommand = new UserCommand(0);

        private UserCommand(int i)
        {
            moveYaw = 0;
            moveMagnitude = 0;
            lookYaw = 0;
            lookPitch = 90;
            jump = false;
            boost = false;
        }
    }
}
