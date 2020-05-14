using UnityEngine;
using UnityEngine.Playables;

namespace JT
{
    public class SimpleTranstion<U> where U : struct, IPlayable
    {
        U m_Target;
        int[] m_Ports;

        public SimpleTranstion(U target, params int[] ports)
        {
            m_Target = target;
            m_Ports = ports;
        }

        public void Update(int activePort, float blendVelocity, float deltaTime)
        {
            // Update current state weight
            float weight = m_Target.GetInputWeight(activePort);
            if (weight != 1.0f)
            {
                weight = Mathf.Clamp(weight + blendVelocity * deltaTime, 0, 1);
                m_Target.SetInputWeight(activePort, weight);
            }

            // Adjust weight of other states and ensure total weight is 1
            float weighLeft = 1.0f - weight;
            float totalWeight = 0;
            for (int i = 0; i < m_Ports.Length; i++)
            {
                int port = m_Ports[i];
                if (port == activePort)
                    continue;

                totalWeight += m_Target.GetInputWeight(port);
            }
            if (totalWeight == 0)
                return;

            float fraction = weighLeft / totalWeight;
            for (int i = 0; i < m_Ports.Length; i++)
            {
                int port = m_Ports[i];
                if (port == activePort)
                    continue;

                float w = m_Target.GetInputWeight(port);
                w = w * fraction;
                m_Target.SetInputWeight(port, w);
            }
        }
    }
}
