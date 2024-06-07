using Unity.NetCode;

namespace TMG.NFE_Tutorial
{
    public struct MobaTeamRequest : IRpcCommand
    {
        public TeamType Value;
    }

    public struct PlayersRemainingToStart : IRpcCommand
    {
        public int Value;
    }

    public struct GameStartTickRpc : IRpcCommand
    {
        public NetworkTick Value;
    }
}