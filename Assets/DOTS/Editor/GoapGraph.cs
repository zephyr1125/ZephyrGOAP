using System.IO;
using DOTS.Logger;
using UnityEngine;

namespace DOTS.Editor
{
    public class GoapGraph
    {
        public Vector2 translation;
        
        private GoapLog _log;

        private int _currentResultId;

        public void LoadLog(string path)
        {
            var textReader = new StreamReader(path);
            var json = textReader.ReadToEnd();
            _log = JsonUtility.FromJson<GoapLog>(json);
        }

        public bool IsEmpty()
        {
            return _log == null;
        }

        public void DrawInfo()
        {
            var currentResult = _log.GetResult(_currentResultId);
            GUI.color = Color.black;
            GUI.Label(new Rect(16, 16, 50, 16), currentResult.AgentName);
            GUI.color = Color.white;
        }

        public void DrawNodes()
        {
            var currentResult = _log.GetResult(_currentResultId);
            
        }
    }
}