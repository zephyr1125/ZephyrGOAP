using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DOTS.Editor
{
    public class GoapLogWindow : EditorWindow
    {
        [MenuItem("Zephyr/Goap/Logger")]
        private static void OpenWindow()
        {
            GetWindow<GoapLogWindow>().Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Canvas", StyleSheet.CanvasIcon);
        }
    }
}
