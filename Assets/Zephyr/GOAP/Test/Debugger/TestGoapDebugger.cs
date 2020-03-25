using System;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Zephyr.GOAP.Debugger;
using Zephyr.GOAP.Logger;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.Test.Debugger
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

        public void SetCurrentStates(ref StateGroup currentStates, EntityManager entityManager)
        {
            _goapLog.SetCurrentStates(ref currentStates, entityManager);
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

        public NodeLog GoalNodeLog => _goapLog.GetGoalNodeView();

        public NodeLog[] PathResult => _goapLog.GetPathResult();

        public NodeLog[] GetChildren(NodeLog parent)
        {
            return _goapLog.GetChildren(parent);
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