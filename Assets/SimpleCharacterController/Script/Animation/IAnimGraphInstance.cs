using UnityEngine;
using UnityEngine.Playables;

namespace JT
{
    public interface IGraphState
    {
        void UpdatePresentationState(bool firstUpdate, float deltaTime);
    }

    public interface IGraphLogic
    {
        void UpdateGraphLogic(float deltaTime);
    }

    public interface IAnimGraphInstance
    {
        void ApplyPresentationState(float deltaTime);
        void GetPlayableOutput(int portId, ref Playable playable, ref int playablePort);
        void SetPlayableInput(int portId, Playable playable, int playablePort);
        void Shutdown();
    }
}
