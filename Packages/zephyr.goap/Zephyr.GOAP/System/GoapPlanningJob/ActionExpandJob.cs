using System;
using Unity.Assertions;
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
            var targetRequires = new StateGroup(Requires, expandingNodeHash, 1, Allocator.Temp);
            
            var targetRequire = action.GetTargetRequire(ref targetRequires, agentEntity, ref StackData);
            targetRequires.Dispose();

            if (!targetRequire.Equals(State.Null))
            {
                var deltas = new StateGroup(Deltas, expandingNodeHash, Allocator.Temp);
                var settings = action.GetSettings(ref targetRequire, agentEntity, ref StackData, Allocator.Temp);

                for (var i=0; i<settings.Length(); i++)
                {
                    var setting = settings[i];
                    var preconditions = new StateGroup(1, Allocator.Temp);
                    var effects = new StateGroup(1, Allocator.Temp);

                    action.GetPreconditions(ref targetRequire, agentEntity, ref setting, ref StackData, ref preconditions);

                    action.GetEffects(ref targetRequire, ref setting, ref StackData, ref effects);

                    if (effects.Length() > 0)
                    {
                        var requires = new StateGroup(leftRequires, Allocator.Temp);
                        
                        requires.AND(effects);
                        requires.OR(preconditions);

                        var reward =
                            action.GetReward(ref targetRequire, ref setting, ref StackData);
                        
                        var time =
                            action.GetExecuteTime(ref targetRequire, ref setting, ref StackData);

                        action.GetNavigatingSubjectInfo(ref targetRequire, ref setting,
                            ref StackData, ref preconditions, out var subjectType, out var subjectId);
                        
                        var node = new Node(ref preconditions, ref effects, ref requires, ref deltas,
                            action.GetName(), reward, time, Iteration, agentEntity, subjectType, subjectId);

                        var nodeExisted = ExistedNodesHash.Contains(node.HashCode);
                        
                        AddRouteNode(expandingNode, node, nodeExisted, 
                            ref preconditions, ref effects, ref requires, ref deltas,
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
            ref StateGroup preconditions, ref StateGroup effects, ref StateGroup requires, ref StateGroup deltas,
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