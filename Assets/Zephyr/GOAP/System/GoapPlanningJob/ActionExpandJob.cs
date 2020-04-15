using Unity.Collections;
using Unity.Jobs;
using Zephyr.GOAP.Action;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.System.GoapPlanningJob
{
    // [BurstCompile]
    public struct ActionExpandJob<T> : IJobParallelForDefer where T : struct, IAction
    {
        [ReadOnly]
        private NativeList<Node> _unexpandedNodes;

        [ReadOnly]
        private StackData _stackData;

        [ReadOnly]
        private NativeMultiHashMap<int, State> _nodeStates;
        
        /// <summary>
        /// NodeGraph中现存所有Node的hash
        /// </summary>
        [ReadOnly]
        private NativeArray<int> _existedNodesHash;

        private NativeHashMap<int, Node>.ParallelWriter _nodesWriter;
        private NativeMultiHashMap<int, Edge>.ParallelWriter _nodeToParentWriter;
        private NativeMultiHashMap<int, State>.ParallelWriter _nodeStateWriter;
        private NativeMultiHashMap<int, State>.ParallelWriter _preconditionWriter;
        private NativeMultiHashMap<int, State>.ParallelWriter _effectWriter;
        
        private NativeHashMap<int, Node>.ParallelWriter _newlyCreatedNodesWriter;

        private readonly int _iteration;

        private T _action;

        public ActionExpandJob(ref NativeList<Node> unexpandedNodes, 
            ref NativeArray<int> existedNodesHash, ref StackData stackData,
            ref NativeMultiHashMap<int, State> nodeStates,
            NativeHashMap<int, Node>.ParallelWriter nodesWriter,
            NativeMultiHashMap<int, Edge>.ParallelWriter nodeToParentWriter, 
            NativeMultiHashMap<int, State>.ParallelWriter nodeStateWriter, 
            NativeMultiHashMap<int, State>.ParallelWriter preconditionWriter, 
            NativeMultiHashMap<int, State>.ParallelWriter effectWriter,
            ref NativeHashMap<int, Node>.ParallelWriter newlyCreatedNodesWriter, int iteration, T action)
        {
            _unexpandedNodes = unexpandedNodes;
            _existedNodesHash = existedNodesHash;
            _stackData = stackData;
            _nodeStates = nodeStates;
            _nodesWriter = nodesWriter;
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
            var leftStates = new StateGroup(1,
                _nodeStates.GetValuesForKey(unexpandedNode.HashCode), Allocator.Temp);
            var targetStates = new StateGroup(leftStates, 1, Allocator.Temp);
            //只考虑其中首个候选state
            var targetState = _action.GetTargetGoalState(ref targetStates, ref _stackData);
            targetStates.Dispose();

            if (!targetState.Equals(State.Null))
            {
                var settings = _action.GetSettings(ref targetState, ref _stackData, Allocator.Temp);

                for (var i=0; i<settings.Length(); i++)
                {
                    var setting = settings[i];
                    var preconditions = new StateGroup(1, Allocator.Temp);
                    var effects = new StateGroup(1, Allocator.Temp);

                    _action.GetPreconditions(ref targetState, ref setting, ref _stackData, ref preconditions);
                    //为了避免没有state的node(例如wander)与startNode有相同的hash，这种node被强制给了一个空state
                    if(preconditions.Length()==0)preconditions.Add(new State());

                    _action.GetEffects(ref targetState, ref setting, ref _stackData, ref effects);

                    if (effects.Length() > 0)
                    {
                        var newStates = new StateGroup(leftStates, Allocator.Temp);
                        newStates.SubForEffect(ref effects);
                        newStates.Merge(preconditions);

                        var reward =
                            _action.GetReward(ref targetState, ref setting, ref _stackData);
                        
                        var time =
                            _action.GetExecuteTime(ref targetState, ref setting, ref _stackData);

                        _action.GetNavigatingSubjectInfo(ref targetState, ref setting,
                            ref _stackData, ref preconditions, out var subjectType, out var subjectId);
                        
                        var node = new Node(ref preconditions, ref effects, ref newStates, 
                            _action.GetName(), reward, time, _iteration,
                            _stackData.AgentEntities[_stackData.CurrentAgentId], subjectType, subjectId);
                        
                        var nodeExisted = _existedNodesHash.Contains(node.HashCode);

                        //NodeGraph的几个容器都移去了并行限制，小心出错
                        AddRouteNode(node, nodeExisted, ref newStates, _nodesWriter, _nodeToParentWriter,
                            _nodeStateWriter, _preconditionWriter, _effectWriter,
                            ref preconditions, ref effects, unexpandedNode, _action.GetName());
                        _newlyCreatedNodesWriter.TryAdd(node.HashCode, node);

                        newStates.Dispose();
                    }
                
                    preconditions.Dispose();
                    effects.Dispose();
                }
                settings.Dispose();
            }
            leftStates.Dispose();
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
            NativeHashMap<int, Node>.ParallelWriter nodesWriter,
            NativeMultiHashMap<int, Edge>.ParallelWriter nodeToParentWriter,
            NativeMultiHashMap<int, State>.ParallelWriter nodeStateWriter, 
            NativeMultiHashMap<int, State>.ParallelWriter preconditionWriter, 
            NativeMultiHashMap<int, State>.ParallelWriter effectWriter,
            ref StateGroup preconditions, ref StateGroup effects,
            Node parent, NativeString64 actionName)
        {
            newNode.Name = actionName;
            
            nodeToParentWriter.Add(newNode.HashCode, new Edge(parent, newNode));
            if(!nodeExisted)
            {
                nodesWriter.TryAdd(newNode.HashCode, newNode);
                
                for(var i=0; i<nodeStates.Length(); i++)
                {
                    var state = nodeStates[i];
                    nodeStateWriter.Add(newNode.HashCode, state);
                }
                
                if(!preconditions.Equals(default(StateGroup)))
                {
                    for(var i=0; i<preconditions.Length(); i++)
                    {
                        var state = preconditions[i];
                        preconditionWriter.Add(newNode.HashCode, state);
                    }
                }

                if (!effects.Equals(default(StateGroup)))
                {
                    for(var i=0; i<effects.Length(); i++)
                    {
                        var state = effects[i];
                        effectWriter.Add(newNode.HashCode, state);
                    }
                }
            }
        }
    }
}