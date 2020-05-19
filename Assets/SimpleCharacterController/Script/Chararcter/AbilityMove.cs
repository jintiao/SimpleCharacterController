using System;
using UnityEngine;

namespace JT
{
    public class AbilityMove : MonoBehaviour
    {
        [Serializable]
        public struct MovementSettings
        {
            public float runSpeed;
            public float sprintSpeed;
            public float acceleration;
            public float friction;
            public float airAcceleration;
            public float airFriction;
            public float jumpAscentDuration;
            public float jumpAscentHeight;
            public float gravity;
            public float maxFallVelocity;
        }

        [Serializable]
        public struct CharacterControllerSettings
        {
            public float slopeLimit;
            public float stepOffset;
            public float skinWidth;
            public float minMoveDistance;
            public Vector3 center;
            public float radius;
            public float height;
        }

        public MovementSettings m_MovementSettings;
        public CharacterControllerSettings characterControllerSettings;
        public bool enableCollisionDetect;

        LogicStateData m_PredictedState;

        int m_DefaultLayer;
        int m_PlatformLayer;
        int m_Mask;

        public Vector3 moveQueryStart;
        public Vector3 moveQueryEnd;
        public Vector3 moveQueryResult;
        CharacterController moveQueryController;
        public bool moveQueryIsGrounded;

        void Start()
        {
            m_PredictedState = GetComponent<LogicStateData>();

            m_DefaultLayer = LayerMask.NameToLayer("Default");
            m_PlatformLayer = LayerMask.NameToLayer("Platform");
            m_Mask = 1 << m_DefaultLayer | 1 << m_PlatformLayer;

            var go = new GameObject("MoveColl_" + name, typeof(CharacterController));
            moveQueryController = go.GetComponent<CharacterController>();
            moveQueryController.transform.position = transform.position;
            moveQueryController.slopeLimit = characterControllerSettings.slopeLimit;
            moveQueryController.stepOffset = characterControllerSettings.stepOffset;
            moveQueryController.skinWidth = characterControllerSettings.skinWidth;
            moveQueryController.minMoveDistance = characterControllerSettings.minMoveDistance;
            moveQueryController.center = characterControllerSettings.center;
            moveQueryController.radius = characterControllerSettings.radius;
            moveQueryController.height = characterControllerSettings.height;
        }

        public void UpdateMove(float deltaTime)
        {
            UpdateMovement(deltaTime);

            if (enableCollisionDetect)
            {
                UpdateGroundTest();
                UpdateMoveQuery();
                UpdateCollision();
            }
        }

        void UpdateMovement(float deltaTime)
        {
            var command = UserCommand.defaultCommand;

            var newPhase = LocomotionState.MaxValue;
            var phaseDuration = Time.time - m_PredictedState.locoStartTime;

            var isOnGround = m_PredictedState.isOnGround;
            var isMoveWanted = command.moveMagnitude != 0.0f;

            if (isOnGround)
            {
                if (isMoveWanted)
                {
                    newPhase = LocomotionState.GroundMove;
                }
                else
                {
                    newPhase = LocomotionState.Stand;
                }
            }

            if (isOnGround)
            {
                m_PredictedState.jumpCount = 0;
                if (command.jump)
                {
                    m_PredictedState.jumpCount = 1;
                    newPhase = LocomotionState.Jump;
                }
            }

            if (m_PredictedState.locoState == LocomotionState.Jump)
            {
                if (phaseDuration > m_MovementSettings.jumpAscentDuration)
                {
                    newPhase = LocomotionState.InAir;
                }
            }

            if (newPhase != LocomotionState.MaxValue && newPhase != m_PredictedState.locoState)
            {
                m_PredictedState.locoState = newPhase;
                m_PredictedState.locoStartTime = Time.time;
            }

            if (m_PredictedState.locoState == LocomotionState.Stand &&
                    m_PredictedState.groundCollider != null &&
                    m_PredictedState.groundCollider.gameObject.layer == m_PlatformLayer)
            {
                if (m_PredictedState.altitude < characterControllerSettings.skinWidth - 0.01f)
                {
                    var platform = m_PredictedState.groundCollider;
                    var posY = platform.transform.position.y + characterControllerSettings.skinWidth;
                    m_PredictedState.position.y = posY;
                }
            }

            var deltaPos = CalculateMovement(deltaTime);

            var oldPos = m_PredictedState.position;
            var newPos = oldPos + deltaPos;

            moveQueryStart = m_PredictedState.position;
            moveQueryEnd = moveQueryStart + deltaPos;
            moveQueryResult = moveQueryEnd;

            m_PredictedState.position = moveQueryResult;
            m_PredictedState.velocity = (newPos - oldPos) / Time.deltaTime;
        }

        Vector3 CalculateMovement(float deltaTime)
        {
            var deltaPos = Vector3.zero;

            var speed = m_MovementSettings.runSpeed;
            var velocity = m_PredictedState.velocity; 
            velocity = CalculateGroundVelocity(velocity, speed, m_MovementSettings.friction, m_MovementSettings.acceleration, deltaTime);

            switch (m_PredictedState.locoState)
            {
                case LocomotionState.Jump:
                    velocity.y = m_MovementSettings.jumpAscentHeight / m_MovementSettings.jumpAscentDuration;
                    break;
                case LocomotionState.InAir:
                    velocity.y += -m_MovementSettings.gravity * deltaTime;
                    if (velocity.y < -m_MovementSettings.maxFallVelocity)
                        velocity.y = -m_MovementSettings.maxFallVelocity;
                    break;
                default:
                    if (enableCollisionDetect)
                        velocity.y = -400.0f * deltaTime;
                    break;
            }

            deltaPos = velocity * deltaTime;
            return deltaPos;
        }

        Vector3 CalculateGroundVelocity(Vector3 velocity, float playerSpeed, float friction, float acceleration, float deltaTime)
        {
            var command = UserCommand.defaultCommand;
            var moveYawRotation = Quaternion.Euler(0, command.lookYaw + command.moveYaw, 0);
            var moveVec = moveYawRotation * Vector3.forward * command.moveMagnitude;

            // Applying friction
            var groundVelocity = new Vector3(velocity.x, 0, velocity.z);
            var groundSpeed = groundVelocity.magnitude;
            var frictionSpeed = Mathf.Max(groundSpeed, 1.0f) * deltaTime * friction;
            var newGroundSpeed = groundSpeed - frictionSpeed;
            if (newGroundSpeed < 0)
                newGroundSpeed = 0;
            if (groundSpeed > 0)
                groundVelocity *= (newGroundSpeed / groundSpeed);

            // Doing actual movement (q2 style)
            var wantedGroundVelocity = moveVec * playerSpeed;
            var wantedGroundDir = wantedGroundVelocity.normalized;
            var currentSpeed = Vector3.Dot(wantedGroundDir, groundVelocity);
            var wantedSpeed = playerSpeed * command.moveMagnitude;
            var deltaSpeed = wantedSpeed - currentSpeed;
            if (deltaSpeed > 0.0f)
            {
                var accel = deltaTime * acceleration * playerSpeed;
                var speed_adjustment = Mathf.Clamp(accel, 0.0f, deltaSpeed) * wantedGroundDir;
                groundVelocity += speed_adjustment;
            }

            //if (!Game.config.easterBunny)
            //{
            //    newGroundSpeed = groundVelocity.magnitude;
            //    if (newGroundSpeed > playerSpeed)
            //        groundVelocity *= playerSpeed / newGroundSpeed;
            //}

            velocity.x = groundVelocity.x;
            velocity.z = groundVelocity.z;

            return velocity;
        }

        public void UpdateGroundTest()
        {
            var startOffset = 1f;
            var distance = 3f;
            var origin = m_PredictedState.position + Vector3.up * startOffset;

            RaycastHit hit;
            Physics.Raycast(origin, Vector3.down, out hit, distance, m_Mask);
            m_PredictedState.groundCollider = hit.collider;
            m_PredictedState.groundNormal = m_PredictedState.groundCollider != null ? hit.normal : Vector3.up;
            m_PredictedState.altitude = m_PredictedState.groundCollider != null ? hit.distance - startOffset : distance - startOffset;
        }

        void UpdateMoveQuery()
        {
            var currentControllerPos = moveQueryController.transform.position;
            if (Vector3.Distance(currentControllerPos, moveQueryStart) > 0.01f)
            {
                currentControllerPos = moveQueryStart;
                moveQueryController.transform.position = currentControllerPos;
            }

            var deltaPos = moveQueryEnd - currentControllerPos;
            moveQueryController.Move(deltaPos);
            moveQueryResult = moveQueryController.transform.position;
            moveQueryIsGrounded = moveQueryController.isGrounded;
        }

        void UpdateCollision()
        {
            var isOnGround = m_PredictedState.isOnGround;
            if (isOnGround != moveQueryIsGrounded)
            {
                if (moveQueryIsGrounded)
                {
                    if (UserCommand.defaultCommand.moveMagnitude != 0.0f)
                    {
                        m_PredictedState.locoState = LocomotionState.GroundMove;
                    }
                    else
                    {
                        m_PredictedState.locoState = LocomotionState.Stand;
                    }
                }
                else
                {
                    m_PredictedState.locoState = LocomotionState.InAir;
                }
            }

            var newPos = moveQueryResult;
            var oldPos = moveQueryStart;
            var velocity = (newPos - oldPos) / Time.deltaTime;

            m_PredictedState.velocity = velocity;
            m_PredictedState.position = moveQueryResult;
        }
    }
}