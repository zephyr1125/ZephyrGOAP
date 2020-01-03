using System;
using System.Collections.Generic;
using DOTS.Struct;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Logger
{
    [Serializable]
    public class NodeView
    {
        public string Name;

        public int Iteration;

        public float Reward;

        public EntityView NavigationSubject;

        public StateView[] States, Preconditions, Effects;

        public List<NodeView> Children;

        public bool IsPath;
        
        private int _hashCode;

        /// <summary>
        /// 在绘制Node Graph时的位置
        /// </summary>
        [NonSerialized]
        public Vector2 DrawPos;

        public static NodeView ConstructNodeTree(ref NodeGraph nodeGraph, EntityManager entityManager)
        {
            var goalNode = nodeGraph.GetGoalNode();
            var goalNodeView = new NodeView(ref nodeGraph, entityManager, goalNode);
            ConstructNodeTree(ref nodeGraph, entityManager, goalNodeView, goalNode);
            return goalNodeView;
        }
        
        private static void ConstructNodeTree(ref NodeGraph nodeGraph, EntityManager entityManager, NodeView nodeView, Node node)
        {
            var children = nodeGraph.GetChildren(node);
            foreach (var child in children)
            {
                var newNodeView = new NodeView(ref nodeGraph, entityManager, child);
                nodeView.AddChild(newNodeView);
                ConstructNodeTree(ref nodeGraph, entityManager, newNodeView, child);
            }
        }

        public NodeView(ref NodeGraph nodeGraph, EntityManager entityManager, Node node)
        {
            Name = node.Name.ToString();
            Iteration = node.Iteration;
            Reward = node.Reward;
            NavigationSubject = new EntityView(entityManager, node.NavigatingSubject);
            States = StateView.CreateStateViews(entityManager, nodeGraph.GetNodeStates(node));
            Preconditions = StateView.CreateStateViews(entityManager, nodeGraph.GetNodePreconditions(node));
            Effects = StateView.CreateStateViews(entityManager, nodeGraph.GetNodeEffects(node));
            _hashCode = node.HashCode;
        }
        
        public void AddChild(NodeView node)
        {
            if(Children == null) Children = new List<NodeView>();
            Children.Add(node);
        }
        
        /// <summary>
        /// 查看自己及children是否在path列表中，是的话则设置IsPath
        /// </summary>
        /// <param name="path"></param>
        public void SetPath(ref NativeList<Node> path)
        {
            for (var i = 0; i < path.Length; i++)
            {
                if (path[i].HashCode != _hashCode) continue;
                IsPath = true;
                break;
            }

            if (Children == null) return;
            foreach (var child in Children)
            {
                child.SetPath(ref path);
            }
        }

        public void GetPath(ref List<NodeView> path)
        {
            if(IsPath)path.Add(this);
            
            if (Children == null) return;
            foreach (var child in Children)
            {
                child.GetPath(ref path);
            }
        }
    }
}