using System;
using UnityEngine;

namespace JT
{
    public enum ControllerMode
    {
        Free,
        LockDir,
    }

    public class SimpleCharacterController : MonoBehaviour
    {
        public ControllerMode controllerMode;
        public Camera characterCamera;

        AbilityMove m_AbilityMove;
        AnimStateController m_AnimStateController;
        AnimStateData m_AnimState;
        LogicStateData m_PredictedState;

        void Start()
        {
            m_AbilityMove = GetComponent<AbilityMove>();
            m_AnimStateController = GetComponent<AnimStateController>();
            m_AnimState = GetComponent<AnimStateData>();
            m_PredictedState = GetComponent<LogicStateData>();

            m_PredictedState.position = transform.position;
            m_PredictedState.velocity = Vector3.zero;
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;
            m_AbilityMove.UpdateMove(deltaTime);
            m_AnimStateController.UpdateAnim(deltaTime);
            UpdateTransform();
        }

        void UpdateTransform()
        {
            transform.position = m_AnimState.position;
            transform.rotation = Quaternion.Euler(0f, m_AnimState.rotation, 0f);
        }
    }
}