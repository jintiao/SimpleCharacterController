using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace JT
{
    [CreateAssetMenu(fileName = "AnimGraph_StateSelector", menuName = "SimpleCharacterController/AnimGraph/StateSelector")]
    public class AnimGraphStateSelector : AnimGraphAsset
    {
        class Instance : IAnimGraphInstance, IGraphLogic
        {
            struct AnimationControllerEntry
            {
                public IAnimGraphInstance controller;
                public IGraphState animStateUpdater;
                public int port;
                public float[] transitionTimes;
            }

            AnimationMixerPlayable m_Mixer;
            AnimationControllerEntry[] m_AnimStates;
            SimpleTranstion<AnimationMixerPlayable> m_StateTranstion;

            AnimStateData m_AnimStateData;

            LocomotionState m_LastAnimState = LocomotionState.MaxValue;
            LocomotionState m_CurrentAnimationState = LocomotionState.MaxValue;
            LocomotionState m_PreviousAnimationState = LocomotionState.MaxValue;

            public Instance(AnimStateController animStateController, PlayableGraph graph, AnimGraphStateSelector settings)
            {
                m_AnimStateData = animStateController.GetComponent<AnimStateData>();

                m_Mixer = AnimationMixerPlayable.Create(graph, 0, true);
                
                m_AnimStates = new AnimationControllerEntry[(int)LocomotionState.MaxValue];

                var controllers = new Dictionary<AnimGraphAsset, IAnimGraphInstance>();
                var controllerPorts = new Dictionary<IAnimGraphInstance, int>();
                var stateTransitionPorts = new List<int>();
                var transitionTimes = new Dictionary<IAnimGraphInstance, float[]>();
                foreach (var controllerDef in settings.controllers)
                {
                    if (controllerDef.template == null)
                        continue;
                    if (controllers.ContainsKey(controllerDef.template))
                        continue;

                    var controller = controllerDef.template.Instatiate(animStateController, graph);
                    controllers.Add(controllerDef.template, controller);

                    var outputPlayable = Playable.Null;
                    var outputPort = 0;
                    controller.GetPlayableOutput(0, ref outputPlayable, ref outputPort);
                    var port = m_Mixer.AddInput(outputPlayable, outputPort);

                    controllerPorts.Add(controller, port);
                    stateTransitionPorts.Add(port);

                    var times = new float[(int)LocomotionState.MaxValue];
                    for (var i = 0; i < (int)LocomotionState.MaxValue; i++)
                    {
                        times[i] = controllerDef.transitionTime;
                    }

                    for (var i = 0; i < controllerDef.customTransitions.Length; i++)
                    {
                        var sourceStateIndex = (int)controllerDef.customTransitions[i].sourceState;
                        var time = controllerDef.customTransitions[i].transtionTime;
                        times[sourceStateIndex] = time;
                    }

                    transitionTimes.Add(controller, times);
                }

                foreach (var controllerDef in settings.controllers)
                {
                    var animState = controllerDef.animationState;
                    if (m_AnimStates[(int)animState].controller != null)
                        continue;

                    var controller = controllers[controllerDef.template];
                    m_AnimStates[(int)animState].controller = controller;
                    m_AnimStates[(int)animState].animStateUpdater = controller as IGraphState;
                    m_AnimStates[(int)animState].port = controllerPorts[controller];
                    m_AnimStates[(int)animState].transitionTimes = transitionTimes[controller];
                }

                m_StateTranstion = new SimpleTranstion<AnimationMixerPlayable>(m_Mixer, stateTransitionPorts.ToArray());
            }

            public void ApplyPresentationState(float deltaTime)
            {
                if (m_AnimStateData.charLocoState != m_CurrentAnimationState)
                {
                    var previousState = m_CurrentAnimationState;
                    var prevController = (int)m_CurrentAnimationState < m_AnimStates.Length ? m_AnimStates[(int)previousState].controller : null;

                    m_CurrentAnimationState = m_AnimStateData.charLocoState;
                    var newController = m_AnimStates[(int)m_CurrentAnimationState].controller;

                    if (newController != prevController)
                    {
                        m_PreviousAnimationState = m_PreviousAnimationState == LocomotionState.MaxValue ? m_CurrentAnimationState : previousState;
                    }
                }

                var interpolationDuration = m_AnimStates[(int)m_CurrentAnimationState].transitionTimes[(int)m_PreviousAnimationState];
                var blendVel = interpolationDuration > 0 ? 1.0f / interpolationDuration : 1.0f / deltaTime;
                m_StateTranstion.Update(m_AnimStates[(int)m_CurrentAnimationState].port, blendVel, deltaTime);

                for (var i = 0; i < (int)LocomotionState.MaxValue; i++)
                {
                    if (m_AnimStates[i].controller != null && m_Mixer.GetInputWeight(m_AnimStates[i].port) > 0f)
                    {
                        m_AnimStates[i].controller.ApplyPresentationState(deltaTime);
                    }
                }
            }

            public void GetPlayableOutput(int portId, ref Playable playable, ref int playablePort)
            {
                playable = m_Mixer;
                playablePort = 0;
            }

            public void SetPlayableInput(int portId, Playable playable, int playablePort)
            {
            }

            public void Shutdown()
            {
                for (var i = 0; i < m_AnimStates.Length; i++)
                {
                    m_AnimStates[i].controller?.Shutdown();
                }
            }

            public void UpdateGraphLogic(float deltaTime)
            {
                var animState = m_AnimStateData.charLocoState;
                var firstUpdate = animState != m_LastAnimState;
                m_LastAnimState = animState;

                if (m_AnimStates[(int)animState].animStateUpdater != null)
                    m_AnimStates[(int)animState].animStateUpdater.UpdatePresentationState(firstUpdate, deltaTime);
            }
        }

        [Serializable]
        public struct TransitionDefinition
        {
            public LocomotionState sourceState;
            public float transtionTime;
        }

        [Serializable]
        public struct ControllerDefinition
        {
            public LocomotionState animationState;
            public AnimGraphAsset template;
            public float transitionTime;
            public TransitionDefinition[] customTransitions;
        }

        public ControllerDefinition[] controllers;

        public override IAnimGraphInstance Instatiate(AnimStateController controller, PlayableGraph graph)
        {
            var animState = new Instance(controller, graph, this);
            return animState;
        }
    }
}
