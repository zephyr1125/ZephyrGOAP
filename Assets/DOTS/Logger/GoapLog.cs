using System;
using System.Collections.Generic;
using System.Text;
using DOTS.Struct;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.Logger
{
    [Serializable]
    public class GoapLog
    {
        public List<GoapResult> results;

        private GoapResult _currentLog;

        public GoapLog()
        {
            results = new List<GoapResult>();
        }

        public void StartLog(EntityManager entityManager, Entity agent)
        {
            _currentLog = new GoapResult();
            results.Add(_currentLog);
            _currentLog.StartLog(entityManager, agent);
        }

        public GoapResult GetResult(int id)
        {
            return results[id];
        }

        public void SetNodeGraph(ref NodeGraph nodeGraph, EntityManager entityManager)
        {
            _currentLog.SetNodeGraph(ref nodeGraph, entityManager);
        }

        public void SetPathResult(ref NativeList<Node> pathResult)
        {
            _currentLog.SetPathResult(ref pathResult);
        }

        public NodeView GetGoalNodeView()
        {
            return _currentLog.GoalNodeView;
        }

        public NodeView[] GetPathResult()
        {
            return _currentLog.GetPathResult();
        }
    }

}