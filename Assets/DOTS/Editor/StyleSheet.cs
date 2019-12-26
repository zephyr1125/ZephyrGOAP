using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DOTS.Editor
{
    [CreateAssetMenu]
    public class StyleSheet : ScriptableObject
    {
        private static StyleSheet _styleSheet;

        private static StyleSheet styleSheet =>
            _styleSheet ? _styleSheet : _styleSheet = Resources.Load<StyleSheet>("StyleSheet");
                // UnityEditor.EditorGUIUtility.isProSkin ? "StyleSheet/StyleSheetDark" : "StyleSheet/StyleSheetLight");

        [ContextMenu("Lock")] void Lock() { hideFlags = HideFlags.NotEditable; }
        [ContextMenu("UnLock")] void UnLock() { hideFlags = HideFlags.None; }
                
        
        [UnityEditor.InitializeOnLoadMethod]
        static void Load() {
            _styleSheet = styleSheet;
        }
        
        void OnValidate() {
            hideFlags = HideFlags.NotEditable;
        }

        public Icons icons;
        public Styles styles;

        public static Texture2D CanvasIcon => styleSheet.icons.canvasIcon;
        
        public static GUIStyle canvasBG => styleSheet.styles.canvasBG;

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
