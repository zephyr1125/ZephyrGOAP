using System;
using DOTS.Debugger;
using DOTS.Struct;
using Unity.Collections;
using UnityEngine;

namespace DOTS.Test
{
    public class TestGoapDebugger : IGoapDebugger, IDisposable
    {
        public NodeGraph NodeGraph;
        
        public void SetNodeGraph(ref NodeGraph nodeGraph)
        {
            NodeGraph = nodeGraph.Copy(Allocator.Temp);
        }

        public void Log(string log)
        {
            Debug.Log(log);
        }

        public void Dispose()
        {
            NodeGraph.Dispose();
        }
    }
}