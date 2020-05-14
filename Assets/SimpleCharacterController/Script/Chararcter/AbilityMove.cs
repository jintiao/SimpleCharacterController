using System;
using UnityEngine;

namespace JT
{
    public class AbilityMove : MonoBehaviour
    {
        [Serializable]
        public struct Settings
        {
            public float slopeLimit;
            public float stepOffset;
            public float skinWidth;
            public float minMoveDistance;
            public Vector3 center;
            public float radius;
            public float height;
        }

        [SerializeField]
        Settings settings;

        public Vector3 moveQueryStart;
        public Vector3 moveQueryEnd;
        public Vector3 moveQueryResult;
        CharacterController moveQueryController;
        public bool moveQueryIsGrounded;

        LogicStateData m_PredictedState;

        int m_DefaultLayer;
        int m_PlatformLayer;
        int m_Mask;

        void Start()
        {
            m_PredictedState = GetComponent<LogicStateData>();

            m_DefaultLayer = LayerMask.NameToLayer("Default");
            m_PlatformLayer = LayerMask.NameToLayer("Platform");
            m_Mask = 1 << m_DefaultLayer | 1 << m_PlatformLayer;

            var go = new GameObject("MoveColl_" + name, typeof(CharacterController));
            moveQueryController = go.GetComponent<CharacterController>();
            moveQueryController.transform.position = transform.position;
            moveQueryController.slopeLimit = settings.slopeLimit;
            moveQueryController.stepOffset = settings.stepOffset;
            moveQueryController.skinWidth = settings.skinWidth;
            moveQueryController.minMoveDistance = settings.minMoveDistance;
            moveQueryController.center = settings.center;
            moveQueryController.radius = settings.radius;
            moveQueryController.height = settings.height;
        }

        public void UpdateMove()
        {
            UpdateMovement();
            UpdateGroundTest();
            UpdateMoveQuery();
            UpdateCollision();
        }

        void UpdateMovement()
        {
            var command = UserCommand.defaultCommand;

            var newPhase = LocoState.MaxValue;
            var isOnGround = m_PredictedState.isOnGround;
            var isMoveWanted = command.moveMagnitude != 0.0f;

            if (isOnGround)
            {
                if (isMoveWanted)
                {
                    newPhase = LocoState.GroundMove;
                }
                else
                {
                    newPhase = LocoState.Stand;
                }
            }

            if (newPhase != LocoState.MaxValue && newPhase != m_PredictedState.locoState)
            {
                m_PredictedState.locoState = newPhase;
            }

            if (m_PredictedState.locoState == LocoState.Stand &&
                    m_PredictedState.groundCollider != null &&
                    m_PredictedState.groundCollider.gameObject.layer == m_PlatformLayer)
            {
                if (m_PredictedState.altitude < settings.skinWidth - 0.01f)
                {
                    var platform = m_PredictedState.groundCollider;
                    var posY = platform.transform.position.y + settings.skinWidth;
                    m_PredictedState.position.y = posY;
                }
            }

            var deltaPos = Vector3.zero;
            var velocity = m_PredictedState.velocity;
            velocity.y = -400.0f * Time.deltaTime;
            deltaPos = velocity * Time.deltaTime;

            moveQueryStart = m_PredictedState.position;
            moveQueryEnd = moveQueryStart + deltaPos;
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
                        m_PredictedState.locoState = LocoState.GroundMove;
                    }
                    else
                    {
                        m_PredictedState.locoState = LocoState.Stand;
                    }
                }
                else
                {
                    m_PredictedState.locoState = LocoState.InAir;
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