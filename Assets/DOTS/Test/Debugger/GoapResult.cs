using System;
using DOTS.Struct;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.Test.Debugger
{
    [Serializable]
    public class GoapResult
    {
        public Entity Agent;
        
        public Node[] PathResult;

        public NodeView GoalNodeView;

        public DateTime TimeStart, TimeEnd;

        public void StartLog(Entity agent)
        {
            Agent = agent;
            TimeStart = DateTime.Now;
        }
        
        public void SetNodeGraph(ref NodeGraph nodeGraph)
        {
            GoalNodeView = new NodeView
            {
                Node = nodeGraph.GetGoalNode(),
                States = nodeGraph.GetNodeStates(nodeGraph.GetGoalNode())
            };
            ConstructNodeTree(ref nodeGraph, GoalNodeView);
        }

        private void ConstructNodeTree(ref NodeGraph nodeGraph, NodeView nodeView)
        {
            var children = nodeGraph.GetChildren(nodeView.Node);
            foreach (var child in children)
            {
                var newNode = new NodeView
                {
                    Node = child, States = nodeGraph.GetNodeStates(child),
                    Preconditions = nodeGraph.GetNodePreconditions(child),
                    Effects = nodeGraph.GetNodeEffects(child)
                };
                nodeView.AddChild(newNode);
                ConstructNodeTree(ref nodeGraph, newNode);
            }
        }

        public void SetPathResult(ref NativeList<Node> pathResult)
        {
            PathResult = pathResult.ToArray();
        }
    }
}