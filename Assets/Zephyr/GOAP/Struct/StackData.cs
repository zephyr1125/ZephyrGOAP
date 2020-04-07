using System;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Zephyr.GOAP.Component;

namespace Zephyr.GOAP.Struct
{
    public struct StackData : IDisposable
    {
        public NativeArray<Entity> AgentEntities;
        public NativeArray<float3> AgentPositions;

        //ActionExpand时的临时标记：当前正在展开的node所相关的agent的Id
        public int CurrentAgentId;

        public StateGroup CurrentStates;

        public StackData(ref NativeArray<Entity> agentEntities, ref NativeArray<Translation> agentTranslations, StateGroup currentStates)
        {
            AgentEntities = agentEntities;
            AgentPositions = new NativeArray<float3>(agentTranslations.Length, Allocator.Persistent);
            for (var i = 0; i < agentTranslations.Length; i++)
            {
                AgentPositions[i] = agentTranslations[i].Value;
            }
            CurrentStates = currentStates;
            CurrentAgentId = 0;
        }
        
        public void Dispose()
        {
            AgentPositions.Dispose();
            CurrentStates.Dispose();
        }
    }
}