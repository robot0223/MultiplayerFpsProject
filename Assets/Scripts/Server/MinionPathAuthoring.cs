using Unity.Entities;
using UnityEngine;

namespace TMG.NFE_Tutorial
{
    public class MinionPathAuthoring : MonoBehaviour
    {
        public Vector3[] TopLanePath;
        public Vector3[] MidLanePath;
        public Vector3[] BotLanePath;

        public class MinionPathBaker : Baker<MinionPathAuthoring>
        {
            public override void Bake(MinionPathAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var topLane = CreateAdditionalEntity(TransformUsageFlags.None, false, "TopLane");
                var midLane = CreateAdditionalEntity(TransformUsageFlags.None, false, "MidLane");
                var botLane = CreateAdditionalEntity(TransformUsageFlags.None, false, "BotLane");
                
                var topLanePath = AddBuffer<MinionPathPosition>(topLane);
                foreach (var pathPosition in authoring.TopLanePath)
                {
                    topLanePath.Add(new MinionPathPosition { Value = pathPosition });
                }
                
                var midLanePath = AddBuffer<MinionPathPosition>(midLane);
                foreach (var pathPosition in authoring.MidLanePath)
                {
                    midLanePath.Add(new MinionPathPosition { Value = pathPosition });
                }
                
                var botLanePath = AddBuffer<MinionPathPosition>(botLane);
                foreach (var pathPosition in authoring.BotLanePath)
                {
                    botLanePath.Add(new MinionPathPosition { Value = pathPosition });
                }
                
                AddComponent(entity, new MinionPathContainers
                {
                    TopLane = topLane,
                    MidLane = midLane,
                    BotLane = botLane
                });
            }
        }
    }
}