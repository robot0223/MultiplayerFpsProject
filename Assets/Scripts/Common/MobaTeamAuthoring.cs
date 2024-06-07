using Unity.Entities;
using UnityEngine;

namespace TMG.NFE_Tutorial
{
    public class MobaTeamAuthoring : MonoBehaviour
    {
        public TeamType MobaTeam;

        public class MobaTeamBaker : Baker<MobaTeamAuthoring>
        {
            public override void Bake(MobaTeamAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new MobaTeam { Value = authoring.MobaTeam });
            }
        }
    }
}