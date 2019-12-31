using System.IO;
using DOTS.Logger;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DOTS.Editor.UIElement
{
    public class GoapLogWindow : EditorWindow
    {
        [MenuItem("Zephyr/Goap/GoapLog")]
        private static void OpenWindow()
        {
            GetWindow<GoapLogWindow>().Show();
        }
        
        private static GoapLog _log;

        private void OnEnable()
        {
            var root = this.rootVisualElement;
            var visualTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/DOTS/Editor/UIElement/UXML/window.uxml");
            visualTree.CloneTree(root);
            
            root.Q<Button>("load-button").RegisterCallback<MouseUpEvent>(
                evt => LoadLogFile());
        }

        private void LoadLogFile()
        {
            var path = EditorUtility.OpenFilePanel(
                "Import  Log", "", "json");
            if (!string.IsNullOrEmpty(path))
            {
                var textReader = new StreamReader(path);
                var json = textReader.ReadToEnd();
                _log = JsonUtility.FromJson<GoapLog>(json);
            }
        }
    }
}