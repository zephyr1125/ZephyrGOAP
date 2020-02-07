using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Collections;
using Unity.Entities;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Logger
{
    [Serializable]
    public class GoapResult
    {
        public EntityView Agent;

        public NodeView GoalNodeView;

        public List<StateView> CurrentStates;

        public string TimeStart;
        
        public string TimeCost;

        private DateTime _timeStart;

        public void StartLog(EntityManager entityManager, Entity agent)
        {
            Agent = new EntityView(entityManager, agent);
            _timeStart = DateTime.Now;
            TimeStart = DateTime.Now.ToString(CultureInfo.InvariantCulture);
        }
        
        public void SetNodeGraph(ref NodeGraph nodeGraph, EntityManager entityManager)
        {
            GoalNodeView = NodeView.ConstructNodeTree(ref nodeGraph, entityManager);
        }

        public void SetPathResult(ref NativeList<Node> pathResult)
        {
            GoalNodeView.SetPath(ref pathResult);
            TimeCost = (DateTime.Now - _timeStart).TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
        }

        public void SetCurrentStates(ref StateGroup currentStates, EntityManager entityManager)
        {
            if (CurrentStates == null) CurrentStates = new List<StateView>(currentStates.Length());
            foreach (var currentState in currentStates)
            {
                CurrentStates.Add(new StateView(entityManager, currentState));
            }
        }

        public NodeView[] GetPathResult()
        {
            var pathResult = new List<NodeView>();
            GoalNodeView.GetPath(ref pathResult);
            return pathResult.ToArray();
        }
    }
}