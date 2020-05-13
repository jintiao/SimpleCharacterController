using UnityEngine;
using UnityEngine.Playables;

namespace JT
{
    public interface IGraphState
    {
        void UpdatePresentationState(bool firstUpdate, GameTime time, float deltaTime);
    }

    public interface IGraphLogic
    {
        void UpdateGraphLogic(GameTime time, float deltaTime);
    }

    public interface IAnimGraphInstance
    {
        void ApplyPresentationState(GameTime time, float deltaTime);
        void GetPlayableOutput(int portId, ref Playable playable, ref int playablePort);
        void SetPlayableInput(int portId, Playable playable, int playablePort);
        void Shutdown();
    }
}
