using UnityEngine;

namespace JT
{
    public struct UserCommand
    {
        public float moveYaw;
        public float moveMagnitude;
        public bool jump;
        public bool boost;

        public static UserCommand defaultCommand = new UserCommand();
    }
}
