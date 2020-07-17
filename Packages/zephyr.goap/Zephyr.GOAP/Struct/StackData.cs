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

        public StackData(NativeList<Entity> agentEntities, ref NativeArray<Translation> agentTranslations, StateGroup baseStates)
        {
            AgentEntities = agentEntities;
            AgentPositions = new NativeArray<float3>(agentTranslations.Length, Allocator.Persistent);
            for (var i = 0; i < agentTranslations.Length; i++)
            {
                AgentPositions[i] = agentTranslations[i].Value;
            }
            BaseStates = baseStates;
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