using System;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.System.GoapPlanningJob
{
    // [BurstCompile]
    public struct ActionExpandJob<T> : IJobParallelFor where T : struct, IAction
    {
        [ReadOnly]
        public NativeArray<ValueTuple<Entity, Node>> NodeAgentPairs;

        [ReadOnly]
        public NativeHashMap<Entity, T> Actions;

        [ReadOnly]
        public StackData StackData;

        [ReadOnly]
        public NativeList<ValueTuple<int, State>> Requires;
        
        [ReadOnly]
        public NativeList<ValueTuple<int, State>> Deltas;
        
        /// <summary>
        /// NodeGraph中现存所有Node的hash
        /// </summary>
        [ReadOnly]
        public NativeArray<int> ExistedNodesHash;

        public NativeHashMap<int, Node>.ParallelWriter NodesWriter;
        
        public NativeList<ValueTuple<int, int>>.ParallelWriter NodeToParentsWriter;

        public NativeHashMap<int, State>.ParallelWriter StatesWriter;
        
        public NativeList<ValueTuple<int, int>>.ParallelWriter PreconditionHashesWriter;
        
        public NativeList<ValueTuple<int, int>>.ParallelWriter EffectHashesWriter;
        
        public NativeList<ValueTuple<int, int>>.ParallelWriter RequireHashesWriter;
        
        public NativeList<ValueTuple<int, int>>.ParallelWriter DeltaHashesWriter;
        
        public NativeHashMap<int, Node>.ParallelWriter NewlyCreatedNodesWriter;

        public int Iteration;

        public void Execute(int jobIndex)
        {
            var (agentEntity, expandingNode) = NodeAgentPairs[jobIndex];
            if (!Actions.ContainsKey(agentEntity)) return;
            var action = Actions[agentEntity];
            var expandingNodeHash = expandingNode.HashCode;

            var leftRequires = new StateGroup(Requires, expandingNodeHash, Allocator.Temp);
            //只考虑node的首个require
            var targetRequire = leftRequires[0];

            if (action.CheckTargetRequire(targetRequire, agentEntity, StackData))
            {
                var deltas = new StateGroup(Deltas, expandingNodeHash, Allocator.Temp);
                var settings = action.GetSettings(targetRequire, agentEntity, StackData, Allocator.Temp);

                for (var i=0; i<settings.Length(); i++)
                {
                    var setting = settings[i];
                    var preconditions = new StateGroup(1, Allocator.Temp);
                    var effects = new StateGroup(1, Allocator.Temp);

                    action.GetPreconditions(targetRequire, agentEntity, setting, StackData, preconditions);

                    action.GetEffects(targetRequire, setting, StackData, effects);

                    if (effects.Length() > 0)
                    {
                        var requires = new StateGroup(leftRequires, Allocator.Temp);
                        
                        requires.AND(effects);
                        requires.OR(preconditions);

                        var reward =
                            action.GetReward(targetRequire, setting, StackData);
                        
                        var time =
                            action.GetExecuteTime(targetRequire, setting, StackData);

                        action.GetNavigatingSubjectInfo(targetRequire, setting,
                            StackData, preconditions, out var subjectType, out var subjectId);
                        
                        var node = new Node(preconditions, effects, requires, deltas,
                            action.GetName(), reward, time, Iteration, agentEntity, subjectType, subjectId);

                        var nodeExisted = ExistedNodesHash.Contains(node.HashCode);
                        
                        AddRouteNode(expandingNode, node, nodeExisted, 
                            preconditions, effects, requires, deltas,
                            expandingNode, action.GetName());
                        NewlyCreatedNodesWriter.TryAdd(node.HashCode, node);

                        requires.Dispose();
                    }
                
                    preconditions.Dispose();
                    effects.Dispose();
                }
                settings.Dispose();
                deltas.Dispose();
            }
            
            leftRequires.Dispose();
        }

        /// <summary>
        /// <param name="newNode"></param>
        /// <param name="requires"></param>
        /// <param name="preconditions"></param>
        /// <param name="effects"></param>
        /// <param name="parent"></param>
        /// <param name="actionName"></param>
        /// <returns>此node已存在</returns>
        /// </summary>
        private void AddRouteNode(Node baseNode, Node newNode, bool nodeExisted,
            StateGroup preconditions, StateGroup effects, StateGroup requires, StateGroup deltas,
            Node parent, NativeString32 actionName)
        {
            newNode.Name = actionName;
            
            NodeToParentsWriter.AddNoResize((newNode.HashCode, parent.HashCode));
            
            if(!nodeExisted)
            {
                NodesWriter.TryAdd(newNode.HashCode, newNode);
                
                if(!preconditions.Equals(default))
                {
                    for(var i=0; i<preconditions.Length(); i++)
                    {
                        var state = preconditions[i];
                        var stateHash = state.GetHashCode();
                        StatesWriter.TryAdd(stateHash, state);
                        PreconditionHashesWriter.AddNoResize((newNode.HashCode, stateHash));
                    }
                }

                if (!effects.Equals(default))
                {
                    //目前effect不可能超过1个
                    Assert.IsTrue(effects.Length()<2, "[AddRouteNode] Too much effects!");
                    for(var i=0; i<effects.Length(); i++)
                    {
                        var state = effects[i];
                        var stateHash = state.GetHashCode();
                        StatesWriter.TryAdd(stateHash, state);
                        EffectHashesWriter.AddNoResize((newNode.HashCode, stateHash));
                    }
                }
                
                for(var i=0; i<requires.Length(); i++)
                {
                    var state = requires[i];
                    var stateHash = state.GetHashCode();
                    StatesWriter.TryAdd(stateHash, state);
                    RequireHashesWriter.AddNoResize((newNode.HashCode, stateHash));
                }
                
                if (!deltas.Equals(default))
                {
                    for(var i=0; i<deltas.Length(); i++)
                    {
                        var state = deltas[i];
                        var stateHash = state.GetHashCode();
                        StatesWriter.TryAdd(stateHash, state);
                        DeltaHashesWriter.AddNoResize((newNode.HashCode, stateHash));
                    }
                }
            }
        }
    }
}