using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace JT
{
    [CreateAssetMenu(fileName = "AnimGraph_Stack", menuName = "SimpleCharacterController/AnimGraph/Stack")]
    public class AnimGraphStack : AnimGraphAsset
    {
        class Instance : IAnimGraphInstance, IGraphLogic
        {
            struct AnimStackEntry
            {
                public IAnimGraphInstance subGraph;
                public IGraphLogic graphLogic;
            }

            Playable m_RootPlayable;
            List<AnimStackEntry> m_SubGraphs = new List<AnimStackEntry>();

            public Instance(AnimStateController controller, PlayableGraph graph, AnimGraphStack settings)
            {
                for (var i = 0; i < settings.nodes.Count; i++)
                {
                    var subGraph = settings.nodes[i].Instatiate(controller, graph);
                    subGraph.SetPlayableInput(0, m_RootPlayable, 0);

                    var outputPort = 0;
                    subGraph.GetPlayableOutput(0, ref m_RootPlayable, ref outputPort);

                    var animStackEntry = new AnimStackEntry()
                    {
                        subGraph = subGraph,
                        graphLogic = subGraph as IGraphLogic
                    };
                    m_SubGraphs.Add(animStackEntry);
                }
            }

            public void ApplyPresentationState(float deltaTime)
            {
                for (var i = 0; i < m_SubGraphs.Count; i++)
                {
                    m_SubGraphs[i].subGraph.ApplyPresentationState(deltaTime);
                }
            }

            public void GetPlayableOutput(int portId, ref Playable playable, ref int playablePort)
            {
                playable = m_RootPlayable;
                playablePort = 0;
            }

            public void SetPlayableInput(int portId, Playable playable, int playablePort)
            {
            }

            public void Shutdown()
            {
                for (var i = 0; i < m_SubGraphs.Count; i++)
                {
                    m_SubGraphs[i].subGraph.Shutdown();
                }
            }

            public void UpdateGraphLogic(float deltaTime)
            {
                for (var i = 0; i < m_SubGraphs.Count; i++)
                {
                    m_SubGraphs[i].graphLogic?.UpdateGraphLogic(deltaTime);
                }
            }
        }

        public List<AnimGraphAsset> nodes = new List<AnimGraphAsset>();

        public override IAnimGraphInstance Instatiate(AnimStateController controller, PlayableGraph graph)
        {
            var animState = new Instance(controller, graph, this);
            return animState;
        }
    }
}