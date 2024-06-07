using Unity.Entities;

namespace FPS_personal_project
{
    public struct ClientTeamRequest : IComponentData
    {
        public TeamType Value;
    }
}