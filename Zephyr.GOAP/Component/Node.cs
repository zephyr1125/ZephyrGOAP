using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Component
{
    public struct Node : IEquatable<Node>, IComponentData
    {
        public FixedString32 Name;
        
        /// <summary>
        /// 第一次展开到这个Node时的层数, goal=0
        /// </summary>
        public int Iteration;

        /// <summary>
        /// -Cost/+Reward
        /// </summary>
        public readonly float Reward;

        public float ExecuteTime;
        
        /// <summary>
        /// 用于比较两个Node是否是同一个node
        /// 即只要两个Node的states全部一致即为同一个node
        /// </summary>
        public readonly int HashCode;

        public Entity AgentExecutorEntity;

        /// <summary>
        /// 当node被当做path存储时，用bitmask指示其preconditions和effects对应的states in buffer
        /// </summary>
        public ulong PreconditionsBitmask;
        public ulong EffectsBitmask;

        /// <summary>
        /// 由于在ActionExpand时尚不能明确precondition的具体目标
        /// 只能在此时存下这个action的目标类型与编号
        /// 等到UnifyPathNodeStates之后再具体赋值NavigatingSubject
        /// </summary>
        public NodeNavigatingSubjectType NavigatingSubjectType;

        public byte NavigatingSubjectId;

        /// <summary>
        /// 用于给Navigating指明导航目标
        /// </summary>
        public Entity NavigatingSubject;

        public float3 NavigatingSubjectPosition;

        /// <summary>
        /// 在path finding时估算的执行开始时间，存下来用于在执行时排序选取最早的node优先执行
        /// </summary>
        public float EstimateStartTime;

        public Node(StateGroup preconditions, StateGroup effects, StateGroup requires, StateGroup deltas,
            FixedString32 name, float reward, float executeTime, int iteration, Entity agentExecutorEntity,
            NodeNavigatingSubjectType subjectType = NodeNavigatingSubjectType.Null, byte subjectId = 0) : this()
        {
            Name = name;
            Reward = reward;
            ExecuteTime = executeTime;
            Iteration = iteration;
            NavigatingSubjectType = subjectType;
            NavigatingSubjectId = subjectId;
            AgentExecutorEntity = agentExecutorEntity;
            
            HashCode = Utils.BasicHash;
            HashCode = Utils.CombineHash(HashCode, preconditions.GetHashCode());
            HashCode = Utils.CombineHash(HashCode, effects.GetHashCode());
            HashCode = Utils.CombineHash(HashCode, requires.GetHashCode());
            HashCode = Utils.CombineHash(HashCode, deltas.GetHashCode());
            HashCode = Utils.CombineHash(HashCode, Utils.GetEntityHash(AgentExecutorEntity));
        }

        public bool Equals(Node other)
        {
            return HashCode.Equals(other.HashCode);
        }

        public float GetReward([ReadOnly]NodeGraph nodeGraph)
        {
            return Reward;
        }

        public float Heuristic([ReadOnly]NodeGraph nodeGraph)
        {
            //todo heuristic计算
            return -Iteration;
        }

        public NativeList<int> GetNeighbours([ReadOnly]NodeGraph nodeGraph, Allocator allocator)
        {
            //所有的parent即为neighbour
            return nodeGraph.GetNodeParents(HashCode, allocator);
        }

        public override int GetHashCode()
        {
            return HashCode;
        }
    }
}