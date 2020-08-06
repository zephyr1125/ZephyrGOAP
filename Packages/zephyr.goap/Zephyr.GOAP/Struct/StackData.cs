using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Zephyr.GOAP.Struct
{
    public struct StackData : IDisposable
    {
        public NativeList<Entity> AgentEntities;
        public NativeArray<float3> AgentPositions;
        public StateGroup BaseStates;
        public NativeHashMap<int, FixedString32> ItemNames;

        public StackData(NativeList<Entity> agentEntities, NativeArray<Translation> agentTranslations, StateGroup baseStates)
        {
            AgentEntities = agentEntities;
            AgentPositions = new NativeArray<float3>(agentTranslations.Length, Allocator.TempJob);
            for (var i = 0; i < agentTranslations.Length; i++)
            {
                AgentPositions[i] = agentTranslations[i].Value;
            }
            BaseStates = baseStates;
            ItemNames = default;
        }

        public float3 GetAgentPosition(Entity agentEntity)
        {
            var id = AgentEntities.IndexOf(agentEntity);
            return AgentPositions[id];
        }
        
        public void Dispose()
        {
            AgentPositions.Dispose();
            BaseStates.Dispose();
        }
    }
}