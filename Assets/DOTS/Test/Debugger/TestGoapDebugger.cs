using System;
using DOTS.Debugger;
using DOTS.Struct;
using Unity.Collections;
using UnityEngine;

namespace DOTS.Test.Debugger
{
    public class TestGoapDebugger : IGoapDebugger, IDisposable
    {
        public Node[] PathResult;

        public NodeView GoalNodeView;
        
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

        public void Log(string log)
        {
            Debug.Log(log);
        }

        public void Dispose()
        {
            
        }
    }
}