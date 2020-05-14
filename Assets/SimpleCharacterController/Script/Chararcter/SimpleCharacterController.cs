using System;
using UnityEngine;

namespace JT
{
    public class SimpleCharacterController : MonoBehaviour
    {
        [SerializeField]
        CharacterSettings m_CharacterSettings;

        AbilityMove m_AbilityMove;
        AnimStateController m_AnimStateController;
        AnimStateData m_AnimState;

        void Start()
        {
            m_AbilityMove = GetComponent<AbilityMove>();
            m_AnimStateController = GetComponent<AnimStateController>();
            m_AnimState = GetComponent<AnimStateData>();
        }

        void Update()
        {
            m_AbilityMove.UpdateMove();
            m_AnimStateController.UpdateAnim(Time.deltaTime);
            UpdateTransform();
        }

        void UpdateTransform()
        {
            transform.position = m_AnimState.position;
            transform.rotation = Quaternion.Euler(0f, m_AnimState.rotation, 0f);
        }
    }
}