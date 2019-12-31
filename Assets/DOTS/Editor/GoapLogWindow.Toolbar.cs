using UnityEditor;
using UnityEngine;

namespace DOTS.Editor
{
    public partial class GoapLogWindow
    {
        private static void ShowToolbar(GoapGraph graph)
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUI.backgroundColor = Color.white.WithAlpha(0.5f);
            
            if ( GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(50)) ) {
                LoadLogFile(graph);
            }
            
            GUILayout.FlexibleSpace();
            
            if ( GUILayout.Button("Style", EditorStyles.toolbarButton, GUILayout.Width(50)) ) {
                RefreshStyleSheet();
            }
            
            GUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
            GUI.color = Color.white;
        }

        private static void LoadLogFile(GoapGraph graph)
        {
            var path = EditorUtility.OpenFilePanel(
                "Import  Log", "", "json");
            if (!string.IsNullOrEmpty(path))
            {
                graph.LoadLog(path);
            }
        }

        private static void RefreshStyleSheet()
        {
            
        }
    }
}