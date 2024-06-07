using Unity.Entities;

namespace TMG.NFE_Tutorial
{
    public struct ClientTeamRequest : IComponentData
    {
        public TeamType Value;
    }
}