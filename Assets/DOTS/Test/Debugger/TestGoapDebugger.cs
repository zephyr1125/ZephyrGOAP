using System;
using DOTS.Debugger;
using DOTS.Struct;
using Unity.Collections;
using UnityEngine;

namespace DOTS.Test.Debugger
{
    public class TestGoapDebugger : IGoapDebugger, IDisposable
    {
        public NodeGraph NodeGraph;
        public Node[] PathResult;

        public NodeView GoalNodeView;
        
        public void SetNodeGraph(ref NodeGraph nodeGraph)
        {
            NodeGraph = nodeGraph.Copy(Allocator.Temp);
            
            GoalNodeView = new NodeView
            {
                Node = NodeGraph.GetGoalNode(),
                States = NodeGraph.GetNodeStates(NodeGraph.GetGoalNode())
            };
            ConstructNodeTree(GoalNodeView);
        }

        private void ConstructNodeTree(NodeView nodeView)
        {
            var children = NodeGraph.GetChildren(nodeView.Node);
            foreach (var child in children)
            {
                var newNode = new NodeView
                {
                    Node = child, States = NodeGraph.GetNodeStates(child)
                };
                nodeView.AddChild(newNode);
                ConstructNodeTree(newNode);
            }
        }

        public void SetPathResult(ref NativeList<Node> pathResult)
        {
            PathResult = pathResult.ToArray();
        }

        public void Log(string log)
        {
            Debug.Log(log);
        }

        public void Dispose()
        {
            if(!NodeGraph.Equals(default(NodeGraph)))NodeGraph.Dispose();
        }
    }
}