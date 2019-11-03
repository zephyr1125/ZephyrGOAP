using System.Collections.Generic;
using ReGoap.Utilities;

namespace ReGoap.Planner
{
    public class AStar<T>
    {
        private readonly FastPriorityQueue<INode<T>, T> frontier;
        private readonly Dictionary<T, INode<T>> stateToNode;
        private readonly Dictionary<T, INode<T>> explored;
        private readonly List<INode<T>> createNodes;

        private bool debugPlan = false;
        private PlanDebugger debugger;

        public AStar(int maxNodesToExpand = 1000)
        {
            frontier = new FastPriorityQueue<INode<T>, T>(maxNodesToExpand);
            stateToNode = new Dictionary<T, INode<T>>();
            explored = new Dictionary<T, INode<T>>();
            createNodes = new List<INode<T>>(maxNodesToExpand);
        }

        void ClearNodes()
        {
            foreach (var node in createNodes)
            {
                node.Recycle();
            }
            createNodes.Clear();
        }
        
        private void DebugPlan(INode<T> node, INode<T> parent)
        {
            if (!debugPlan) return;
            if (debugger == null)
                debugger = new PlanDebugger();

            string nodeStr = string.Format(@"{0} [label=<
<table border='0' color='black' fontcolor='#F5F5F5'>
    <tr> <td colspan='2'><b>{4}</b></td> </tr>
    <hr/>
    <tr align='left'> <td border='1' sides='rt'><b>Costs</b></td>           <td border='1' sides='t'><b>g</b>: {1} ; <b>h</b>: {2} ; <b>c</b>: {3}</td> </tr>
    <tr align='left'> <td border='1' sides='rt'><b>Preconditions</b></td>   <td border='1' sides='t'>{5}</td> </tr>
    <tr align='left'> <td border='1' sides='rt'><b>Effects</b></td>         <td border='1' sides='t'>{6}</td> </tr>
    <tr align='left'> <td border='1' sides='rt'><b>Goal</b></td>            <td border='1' sides='t'>{7}</td> </tr>
</table>
>]",
                node.GetHashCode(),
                node.GetPathCost(), node.GetHeuristicCost(), node.GetCost(),
                node.Name, node.Preconditions != null ? node.Preconditions.ToString() : "",
                node.Effects != null ? node.Effects.ToString() : "",
                node.Goal != null ? node.Goal.ToString() : "");
            debugger.AddNode(nodeStr);

            if (parent != null)
            {
                string connStr = string.Format("{0} -> {1}", parent.GetHashCode(), node.GetHashCode());
                debugger.AddConn(connStr);
            }
        }
        
        private void EndDebugPlan(INode<T> node)
        {
            if (debugger != null)
            {
                while (node != null)
                {
                    //mark success path
                    string nodeStr = string.Format("{0} [style=\"bold\" color=\"darkgreen\"]", node.GetHashCode());
                    debugger.AddNode(nodeStr);
                    node = node.GetParent();
                }

                var txt = debugger.TransformText();
                System.IO.Directory.CreateDirectory("PlanDebugger");
                System.IO.Directory.CreateDirectory("PlanDebugger/Raws");
                System.IO.File.WriteAllText(string.Format("PlanDebugger/Raws/DebugPlan_{0}.dot", System.DateTime.Now.ToString("HHmmss_ffff")), txt);
                debugger.Clear();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="goal"></param>
        /// <param name="maxIteration"></param>
        /// <param name="earlyExit">一旦在邻居中发现了goal，就立即返回，不再寻找更优路线的可能</param>
        /// <param name="clearNodes"></param>
        /// <param name="debugPlan"></param>
        /// <returns></returns>
        public INode<T> Run(INode<T> start, T goal, int maxIteration = 100, bool earlyExit = true,
            bool clearNodes = true, bool debugPlan = false)
        {
            this.debugPlan = debugPlan;
            
            frontier.Clear();
            stateToNode.Clear();
            explored.Clear();
            if (clearNodes)
            {
                ClearNodes();
                createNodes.Add(start);
            }
            
            frontier.Enqueue(start, start.GetCost());
            
            DebugPlan(start, null);

            var iterations = 0;
            while (frontier.Count > 0 && iterations<maxIteration && frontier.Count+1<frontier.MaxSize)
            {
                var node = frontier.Dequeue();
                
                if (node.IsGoal(goal))
                {
                    ReGoapLogger.Log("[Astar] Success iterations: " + iterations);
                    EndDebugPlan(node);
                    return node;
                }

                explored[node.GetState()] = node;

                foreach (var child in node.Expand())
                {
                    iterations++;
                    if (clearNodes)
                    {
                        createNodes.Add(child);
                    }

                    if (earlyExit && child.IsGoal(goal))
                    {
                        ReGoapLogger.Log("[Astar] (early exit) Success iterations: " + iterations);
                        EndDebugPlan(child);
                        return child;
                    }

                    var childCost = child.GetCost();
                    var state = child.GetState();
                    if(explored.ContainsKey(state)) continue;

                    stateToNode.TryGetValue(state, out var similarNode);
                    if (similarNode != null)
                    {
                        if (similarNode.GetCost() > childCost) frontier.Remove(similarNode);
                        else break;
                    }
                    
                    DebugPlan(child, node);
                    
                    frontier.Enqueue(child, childCost);
                    stateToNode[state] = child;
                }
            }
            ReGoapLogger.LogWarning("[Astar] failed.");
            EndDebugPlan(null);
            return null;
        }
    }

    public interface INode<T>
    {
        T GetState();

        /// <summary>
        /// get neighbour
        /// </summary>
        /// <returns></returns>
        List<INode<T>> Expand();
        int CompareTo(INode<T> other);
        float GetCost();
        float GetHeuristicCost();
        float GetPathCost();
        INode<T> GetParent();
        bool IsGoal(T goal);
        
        string Name { get; }
        T Goal { get; }
        T Effects { get; }
        T Preconditions { get; }

        int QueueIndex { get; set; }
        float Priority { get; set; }
        void Recycle();
    }
    
    public class NodeComparer<T> : IComparer<INode<T>>
    {
        public int Compare(INode<T> x, INode<T> y)
        {
            var result = x.CompareTo(y);
            if (result == 0)
                return 1;
            return result;
        }
    }
}