using System;
using System.Collections.Generic;
using DOTS.Struct;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.Test.Debugger
{
    [Serializable]
    public class GoapLog
    {
        private MultiDict<Entity, GoapResult> _results;

        private GoapResult _currentLog;
        
        public GoapLog()
        {
            _results = new MultiDict<Entity, GoapResult>();
        }
        
        public void StartLog(Entity agent)
        {
            _currentLog = new GoapResult();
            _results.Add(agent, _currentLog);
            _currentLog.StartLog(agent);
        }

        public void SetNodeGraph(ref NodeGraph nodeGraph)
        {
            _currentLog.SetNodeGraph(ref nodeGraph);
        }

        public void SetPathResult(ref NativeList<Node> pathResult)
        {
            _currentLog.SetPathResult(ref pathResult);
        }

        public NodeView GetGoalNodeView()
        {
            return _currentLog.GoalNodeView;
        }

        public Node[] GetPathResult()
        {
            return _currentLog.PathResult;
        }
    }
    
}