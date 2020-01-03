using System;
using System.Collections.Generic;
using DOTS.Struct;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.Logger
{
    [Serializable]
    public class GoapResult
    {
        public EntityView Agent;

        public NodeView GoalNodeView;

        public DateTime TimeStart, TimeEnd;

        public void StartLog(EntityManager entityManager, Entity agent)
        {
            Agent = new EntityView(entityManager, agent);
            TimeStart = DateTime.Now;
        }
        
        public void SetNodeGraph(ref NodeGraph nodeGraph, EntityManager entityManager)
        {
            GoalNodeView = NodeView.ConstructNodeTree(ref nodeGraph, entityManager);
        }

        public void SetPathResult(ref NativeList<Node> pathResult)
        {
            GoalNodeView.SetPath(ref pathResult);
            TimeEnd = DateTime.Now;
        }

        public NodeView[] GetPathResult()
        {
            var pathResult = new List<NodeView>();
            GoalNodeView.GetPath(ref pathResult);
            return pathResult.ToArray();
        }
    }
}