using DOTS.Action;
using DOTS.Struct;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DOTS.System.GoapPlanningJob
{
    // [BurstCompile]
    public struct ActionExpandJob<T> : IJobParallelForDefer where T : struct, IAction
    {
        [ReadOnly]
        private NativeList<Node> _unexpandedNodes;

        [ReadOnly]
        private StackData _stackData;

        [ReadOnly]
        private NativeMultiHashMap<Node, State> _nodeStates;
        
        /// <summary>
        /// NodeGraph中现存所有Node的hash
        /// </summary>
        [ReadOnly]
        private NativeArray<int> _existedNodesHash;
        
        private NativeMultiHashMap<Node, Edge>.ParallelWriter _nodeToParentWriter;
        private NativeMultiHashMap<Node, State>.ParallelWriter _nodeStateWriter;
        private NativeMultiHashMap<Node, State>.ParallelWriter _preconditionWriter;
        private NativeMultiHashMap<Node, State>.ParallelWriter _effectWriter;
        
        private NativeQueue<Node>.ParallelWriter _newlyCreatedNodesWriter;

        private readonly int _iteration;

        private T _action;

        public ActionExpandJob(ref NativeList<Node> unexpandedNodes, 
            ref NativeArray<int> existedNodesHash, ref StackData stackData,
            ref NativeMultiHashMap<Node, State> nodeStates,
            NativeMultiHashMap<Node, Edge>.ParallelWriter nodeToParentWriter, 
            NativeMultiHashMap<Node, State>.ParallelWriter nodeStateWriter, 
            NativeMultiHashMap<Node, State>.ParallelWriter preconditionWriter, 
            NativeMultiHashMap<Node, State>.ParallelWriter effectWriter,
            ref NativeQueue<Node>.ParallelWriter newlyCreatedNodesWriter, int iteration, T action)
        {
            _unexpandedNodes = unexpandedNodes;
            _existedNodesHash = existedNodesHash;
            _stackData = stackData;
            _nodeStates = nodeStates;
            _nodeToParentWriter = nodeToParentWriter;
            _nodeStateWriter = nodeStateWriter;
            _preconditionWriter = preconditionWriter;
            _effectWriter = effectWriter;
            _newlyCreatedNodesWriter = newlyCreatedNodesWriter;
            _iteration = iteration;
            _action = action;
        }

        public void Execute(int jobIndex)
        {
            var unexpandedNode = _unexpandedNodes[jobIndex];
            var targetStates = new StateGroup(3, _nodeStates.GetValuesForKey(unexpandedNode), Allocator.Temp);
            var targetState = _action.GetTargetGoalState(ref targetStates, ref _stackData);

            if (!targetState.Equals(State.Null))
            {
                var settings = _action.GetSettings(ref targetState, ref _stackData, Allocator.Temp);

                for (var i=0; i<settings.Length(); i++)
                {
                    var setting = settings[i];
                    var preconditions = new StateGroup(1, Allocator.Temp);
                    var effects = new StateGroup(1, Allocator.Temp);

                    _action.GetPreconditions(ref targetState, ref setting, ref _stackData, ref preconditions);
                    ReplacePreconditionsWithSpecificStates(ref preconditions);
                
                    _action.GetEffects(ref targetState, ref setting, ref _stackData, ref effects);

                    if (effects.Length() > 0)
                    {
                        var newStates = new StateGroup(targetStates, Allocator.Temp);
                        newStates.SubForEffect(ref effects);
                        newStates.Merge(preconditions);

                        var reward =
                            _action.GetReward(ref targetState, ref setting, ref _stackData);

                        var node = new Node(ref newStates, _action.GetName(), reward, _iteration,
                            _action.GetNavigatingSubject(ref targetState, ref setting, ref _stackData, ref preconditions));
                        
                        var nodeExisted = _existedNodesHash.Contains(node.HashCode);

                        //NodeGraph的几个容器都移去了并行限制，小心出错
                        AddRouteNode(node, nodeExisted, ref newStates, _nodeToParentWriter,
                            _nodeStateWriter, _preconditionWriter, _effectWriter,
                            ref preconditions, ref effects, unexpandedNode, _action.GetName());
                        _newlyCreatedNodesWriter.Enqueue(node);

                        newStates.Dispose();
                    }
                
                    preconditions.Dispose();
                    effects.Dispose();
                }
                settings.Dispose();
            }
            targetStates.Dispose();
        }

        /// <summary>
        /// 把preconditions里能够找到具体目标的范围state用具体目标替代
        /// </summary>
        /// <param name="preconditions"></param>
        private void ReplacePreconditionsWithSpecificStates(ref StateGroup preconditions)
        {
            for (var i = 0; i < preconditions.Length(); i++)
            {
                if (preconditions[i].Target != Entity.Null) continue;
                foreach (var currentState in _stackData.CurrentStates)
                {
                    //todo 此处应寻找最近目标
                    if (currentState.BelongTo(preconditions[i]))
                    {
                        preconditions[i] = currentState;
                    }
                }
            }
        }
        
        /// <summary>
        /// <param name="newNode"></param>
        /// <param name="nodeStates"></param>
        /// <param name="effectWriter"></param>
        /// <param name="preconditions"></param>
        /// <param name="effects"></param>
        /// <param name="parent"></param>
        /// <param name="actionName"></param>
        /// <param name="nodeToParentWriter"></param>
        /// <param name="nodeStateWriter"></param>
        /// <param name="preconditionWriter"></param>
        /// <returns>此node已存在</returns>
        /// </summary>
        private void AddRouteNode(Node newNode, bool nodeExisted, ref StateGroup nodeStates,
            NativeMultiHashMap<Node, Edge>.ParallelWriter nodeToParentWriter,
            NativeMultiHashMap<Node, State>.ParallelWriter nodeStateWriter, 
            NativeMultiHashMap<Node, State>.ParallelWriter preconditionWriter, 
            NativeMultiHashMap<Node, State>.ParallelWriter effectWriter,
            ref StateGroup preconditions, ref StateGroup effects,
            Node parent, NativeString64 actionName)
        {
            newNode.Name = actionName;
            nodeToParentWriter.Add(newNode, new Edge(parent, newNode, actionName));
            if(!nodeExisted){
                for(var i=0; i<nodeStates.Length(); i++)
                {
                    var state = nodeStates[i];
                    nodeStateWriter.Add(newNode, state);
                }
                
                if(!preconditions.Equals(default(StateGroup)))
                {
                    for(var i=0; i<preconditions.Length(); i++)
                    {
                        var state = preconditions[i];
                        preconditionWriter.Add(newNode, state);
                    }
                }

                if (!effects.Equals(default(StateGroup)))
                {
                    for(var i=0; i<effects.Length(); i++)
                    {
                        var state = effects[i];
                        effectWriter.Add(newNode, state);
                    }
                }
            }
        }
    }
}