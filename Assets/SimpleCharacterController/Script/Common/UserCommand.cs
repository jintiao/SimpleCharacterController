using UnityEngine;

namespace JT
{
    public struct UserCommand
    {
        public float moveYaw;
        public float moveMagnitude;

        public static UserCommand defaultCommand = new UserCommand();
    }
}
