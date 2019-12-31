using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DOTS.Editor
{
    [CreateAssetMenu]
    public class StyleSheet : ScriptableObject
    {
        [ContextMenu("Lock")] void Lock() { hideFlags = HideFlags.NotEditable; }
        [ContextMenu("UnLock")] void UnLock() { hideFlags = HideFlags.None; }

        public Icons icons;
        public Styles styles;

        public int nodeWidth;
        public int nodeHeight;
        public int nodeDistance;

        [Serializable]
        public class Styles
        {
            public GUIStyle canvasBG;
        }
        
        [Serializable]
        public class Icons
        {
            [Header("Fixed")]
            public Texture2D canvasIcon;
        }
    }
}
