using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Lib;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.System.GoapPlanningJob
{
    [BurstCompile]
    public struct ActionExpandJob<T> : IJobParallelFor where T : struct, IAction
    {
        [ReadOnly]
        public NativeArray<ZephyrValueTuple<Entity, Node>> NodeAgentPairs;

        [ReadOnly]
        public NativeHashMap<Entity, T> Actions;

        [ReadOnly]
        public StackData StackData;

        [ReadOnly]
        public NativeList<ZephyrValueTuple<int, State>> Requires;
        
        [ReadOnly]
        public NativeList<ZephyrValueTuple<int, State>> Deltas;
        
        /// <summary>
        /// NodeGraph中现存所有Node的hash
        /// </summary>
        [ReadOnly]
        public NativeArray<int> ExistedNodesHash;

        public NativeHashMap<int, Node>.ParallelWriter NodesWriter;
        
        public NativeList<ZephyrValueTuple<int, int>>.ParallelWriter NodeToParentsWriter;

        public NativeHashMap<int, State>.ParallelWriter StatesWriter;
        
        public NativeList<ZephyrValueTuple<int, int>>.ParallelWriter PreconditionHashesWriter;
        
        public NativeList<ZephyrValueTuple<int, int>>.ParallelWriter EffectHashesWriter;
        
        public NativeList<ZephyrValueTuple<int, int>>.ParallelWriter RequireHashesWriter;
        
        public NativeList<ZephyrValueTuple<int, int>>.ParallelWriter DeltaHashesWriter;
        
        public NativeHashMap<int, Node>.ParallelWriter NewlyCreatedNodesWriter;

        [ReadOnly]
        public FixedString32 ActionName;

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

            //currentState构建：BaseState - Deltas
            var deltas = new StateGroup(Deltas, expandingNodeHash, Allocator.Temp);
            var currentStates = new StateGroup(StackData.BaseStates, Allocator.Temp);
            currentStates.MINUS(deltas);
            
            if (action.CheckTargetRequire(targetRequire, agentEntity, StackData, currentStates))
            {
               var settings = action.GetSettings(targetRequire, agentEntity,
                   StackData, currentStates, Allocator.Temp);

                for (var i=0; i<settings.Length(); i++)
                {
                    var setting = settings[i];
                    var preconditions = new StateGroup(1, Allocator.Temp);
                    var effects = new StateGroup(1, Allocator.Temp);

                    action.GetPreconditions(targetRequire, agentEntity, setting,
                        StackData, currentStates, preconditions);

                    action.GetEffects(targetRequire, setting, StackData, effects);

                    if (effects.Length() > 0)
                    {
                        var requires = new StateGroup(leftRequires, Allocator.Temp);

                        //precondition-current得到new delta
                        var newDeltas = preconditions.MINUS(currentStates,
                            true);

                        requires.MINUS(effects);
                        requires.OR(preconditions);
                        //补上newDelta给precondition是为了在ActionExecution的时候从中找目标
                        preconditions.OR(newDeltas);
                        newDeltas.OR(deltas);

                        var reward =
                            action.GetReward(targetRequire, setting, StackData);
                        
                        var time =
                            action.GetExecuteTime(setting);

                        action.GetNavigatingSubjectInfo(targetRequire, setting,
                            StackData, preconditions, out var subjectType, out var subjectId);
                        
                        var node = new Node(preconditions, effects, requires, newDeltas,
                            ActionName, reward, time, Iteration, agentEntity, subjectType, subjectId);

                        var nodeExisted = ExistedNodesHash.Contains(node.HashCode);
                        
                        AddRouteNode(expandingNode, node, nodeExisted, 
                            preconditions, effects, requires, newDeltas,
                            expandingNode, ActionName);
                        NewlyCreatedNodesWriter.TryAdd(node.HashCode, node);

                        requires.Dispose();
                        newDeltas.Dispose();
                    }
                
                    preconditions.Dispose();
                    effects.Dispose();
                }
                settings.Dispose();
                
            }
            
            leftRequires.Dispose();
            deltas.Dispose();
            currentStates.Dispose();
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
            Node parent, FixedString32 actionName)
        {
            newNode.Name = actionName;
            
            NodeToParentsWriter.AddNoResize(new ZephyrValueTuple<int, int>(newNode.HashCode, parent.HashCode));
            
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
                        PreconditionHashesWriter.AddNoResize(new ZephyrValueTuple<int, int>(newNode.HashCode, stateHash));
                    }
                }

                if (!effects.Equals(default))
                {
                    //目前effect不可能超过1个
                    Assert.IsTrue(effects.Length()<2);
                    for(var i=0; i<effects.Length(); i++)
                    {
                        var state = effects[i];
                        var stateHash = state.GetHashCode();
                        StatesWriter.TryAdd(stateHash, state);
                        EffectHashesWriter.AddNoResize(new ZephyrValueTuple<int, int>(newNode.HashCode, stateHash));
                    }
                }
                
                for(var i=0; i<requires.Length(); i++)
                {
                    var state = requires[i];
                    var stateHash = state.GetHashCode();
                    StatesWriter.TryAdd(stateHash, state);
                    RequireHashesWriter.AddNoResize(new ZephyrValueTuple<int, int>(newNode.HashCode, stateHash));
                }
                
                if (!deltas.Equals(default))
                {
                    for(var i=0; i<deltas.Length(); i++)
                    {
                        var state = deltas[i];
                        var stateHash = state.GetHashCode();
                        StatesWriter.TryAdd(stateHash, state);
                        DeltaHashesWriter.AddNoResize(new ZephyrValueTuple<int, int>(newNode.HashCode, stateHash));
                    }
                }
            }
        }
    }
}