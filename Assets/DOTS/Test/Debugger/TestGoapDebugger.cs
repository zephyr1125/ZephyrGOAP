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

        public void StartLog(EntityManager entityManager, Entity agent)
        {
            if (_goapLog == null)
            {
                _goapLog = new GoapLog();
            }
            _goapLog.StartLog(entityManager, agent);
        }

        public void SetNodeGraph(ref NodeGraph nodeGraph, EntityManager entityManager)
        {
            _goapLog.SetNodeGraph(ref nodeGraph, entityManager);
        }

        public void SetPathResult(ref NativeList<Node> pathResult)
        {
            _goapLog.SetPathResult(ref pathResult);
        }

        public void LogDone()
        {
            SaveToFile();
        }

        private void SaveToFile()
        {
            var json = JsonUtility.ToJson(_goapLog);

            var path = "GoapTestLog/" + DateTime.Now.ToShortDateString();
            var fileName = DateTime.Now.ToFileTime() + ".json";
            Directory.CreateDirectory(path);
            var writer = File.CreateText(path + "/" + fileName);
            writer.Write(json);
            writer.Close();
        }

        public NodeView GoalNodeView => _goapLog.GetGoalNodeView();

        public NodeView[] PathResult => _goapLog.GetPathResult();

        public void Log(string log)
        {
            Debug.Log(log);
        }

        public void Dispose()
        {
            
        }
    }
}