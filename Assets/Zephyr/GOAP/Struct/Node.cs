using System;
using Unity.Collections;
using Unity.Entities;

namespace Zephyr.GOAP.Struct
{
    public struct Node : IEquatable<Node>, IPathFindingNode, IBufferElementData
    {
        public NativeString64 Name;
        
        /// <summary>
        /// 第一次展开到这个Node时的层数, goal=0
        /// </summary>
        public int Iteration;

        /// <summary>
        /// -Cost/+Reward
        /// </summary>
        public readonly float Reward;
        
        /// <summary>
        /// 用于比较两个Node是否是同一个node
        /// 即只要两个Node的states全部一致即为同一个node
        /// </summary>
        public readonly int HashCode;

        public Entity AgentExecutorEntity;

        /// <summary>
        /// 预测的执行开始与结束时间
        /// </summary>
        public double ExecuteStartTime, ExecuteEndTime;

        /// <summary>
        /// 当node被当做path存在agent上时，用bitmask指示其preconditions和effects对应的states in buffer
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

        public Node(ref StateGroup preconditions, ref StateGroup effects, ref StateGroup states, 
            NativeString64 name, float reward, int iteration, Entity agentExecutorEntity,
            NodeNavigatingSubjectType subjectType = NodeNavigatingSubjectType.Null, byte subjectId = 0) : this()
        {
            Name = name;
            Reward = reward;
            Iteration = iteration;
            NavigatingSubjectType = subjectType;
            NavigatingSubjectId = subjectId;
            AgentExecutorEntity = agentExecutorEntity;
            
            HashCode = 17;
            HashCode = HashCode * 31 + preconditions.GetHashCode();
            HashCode = HashCode * 31 + effects.GetHashCode();
            HashCode = HashCode * 31 + states.GetHashCode();
            HashCode = HashCode * 31 + agentExecutorEntity.GetHashCode();
        }

        public bool Equals(Node other)
        {
            return HashCode.Equals(other.HashCode);
        }

        public float GetReward([ReadOnly]ref NodeGraph nodeGraph)
        {
            return Reward;
        }

        public float Heuristic([ReadOnly]ref NodeGraph nodeGraph)
        {
            //todo heuristic计算
            return -Iteration;
        }

        public void GetNeighbours([ReadOnly]ref NodeGraph nodeGraph, ref NativeList<int> neighboursId)
        {
            //所有的parent即为neighbour
            var edges = nodeGraph.GetEdgeToParents(this);
            foreach (var edge in edges)
            {
                neighboursId.Add(edge.Parent.HashCode);
            }
        }

        public override int GetHashCode()
        {
            return HashCode;
        }
    }
}