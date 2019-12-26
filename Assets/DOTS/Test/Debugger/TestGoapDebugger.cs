using System;
using System.IO;
using DOTS.Debugger;
using DOTS.Logger;
using DOTS.Struct;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Test.Debugger
{
    public class TestGoapDebugger : IGoapDebugger, IDisposable
    {
        private GoapLog _goapLog;

        public void StartLog(Entity agent)
        {
            if (_goapLog == null)
            {
                _goapLog = new GoapLog();
            }
            _goapLog.StartLog(agent);
        }

        public void SetNodeGraph(ref NodeGraph nodeGraph)
        {
            _goapLog.SetNodeGraph(ref nodeGraph);
        }

        public void SetPathResult(ref NativeList<Node> pathResult)
        {
            _goapLog.SetPathResult(ref pathResult);
            //save to file
            // var json = JsonUtility.ToJson(_goapLog);
            var json = _goapLog.SaveToJson();
            var path = "GoapTestLog/" + DateTime.Now.ToShortDateString();
            var fileName = DateTime.Now.ToFileTime()+ ".json";
            Directory.CreateDirectory(path);
            var writer = File.CreateText(path+"/"+fileName);
            writer.Write(json);
            writer.Close();
        }
        
        public NodeView GoalNodeView => _goapLog.GetGoalNodeView();

        public Node[] PathResult => _goapLog.GetPathResult();

        public void Log(string log)
        {
            Debug.Log(log);
        }

        public void Dispose()
        {
            
        }
    }
}