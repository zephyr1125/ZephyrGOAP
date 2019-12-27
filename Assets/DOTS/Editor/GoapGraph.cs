using System.IO;
using DOTS.Logger;
using LitJson;
using UnityEditor;
using UnityEngine;

namespace DOTS.Editor
{
    public class GoapGraph
    {
        public Vector2 translation;
        
        private GoapLog _log;

        public void LoadLog(string path)
        {
            var textReader = new StreamReader(path);
            var data = JsonMapper.ToObject(textReader);

            _log = new GoapLog(data);
        }

        public bool IsEmpty()
        {
            return _log == null;
        }

        public void DrawInfo()
        {
            _log.DrawInfo();
        }
    }
}