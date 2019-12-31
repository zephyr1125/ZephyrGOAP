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

        public void DrawNodes(StyleSheet styleSheet)
        {
            var currentResult = _log.GetResult(_currentResultId);
            DrawNode(currentResult.GoalNodeView, 0, styleSheet);
        }

        private void DrawNode(NodeView node, int id, StyleSheet styleSheet)
        {
            GUI.Box(new Rect(8 + node.Iteration*(styleSheet.nodeWidth + styleSheet.nodeDistance),
                8+id*(styleSheet.nodeHeight+styleSheet.nodeDistance),
                styleSheet.nodeWidth, styleSheet.nodeHeight), string.Empty);

            if (node.Children == null) return;
            for (var i = 0; i < node.Children.Count; i++)
            {
                DrawNode(node.Children[i], i, styleSheet);
            }
        }
    }
}