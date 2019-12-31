using System.IO;
using System.Reflection;
using DOTS.Logger;
using UnityEditor;
using UnityEditor.UIElements;
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
        private int _currentResult;

        private VisualTreeAsset _nodeVisualTree;

        private void OnEnable()
        {
            Init();
        }

        private void Init()
        {
            var windowVisualTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/DOTS/Editor/UIElement/UXML/window.uxml");
            windowVisualTree.CloneTree(rootVisualElement);
            
            _nodeVisualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/DOTS/Editor/UIElement/UXML/node.uxml");
            
            rootVisualElement.Q<Button>("load-button").RegisterCallback<MouseUpEvent>(
                evt =>
                {
                    LoadLogFile();
                    ConstructInfo();
                    ConstructGraph();
                });
            rootVisualElement.Q<Button>("reset-button").RegisterCallback<MouseUpEvent>(
                evt => Reset());
        }
        
        private void Reset()
        {
            rootVisualElement.Clear();
            _log = null;
            
            Init();
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

        private void ConstructInfo()
        {
            if (_log == null) return;

            rootVisualElement.Q<Label>("agent-name").text = _log.results[0].AgentName;
        }

        private void ConstructGraph()
        {
            if (_log == null) return;

            var mainFrame = rootVisualElement.Q("main-frame");

            ConstructNode(mainFrame, _log.results[_currentResult].GoalNodeView, 0);
        }

        private void ConstructNode(VisualElement frame, NodeView node, int id)
        {
            _nodeVisualTree.CloneTree(frame);
            var nodeVE = frame.Q("frame");
            nodeVE.style.left = 16 + node.Iteration * (160+16);
            nodeVE.style.top = 16 + id * (80+16);
            nodeVE.name = node.Name;
            nodeVE.Q<Label>("name").text = node.Name;

            if (node.Children == null) return;
            for (var i = 0; i < node.Children.Count; i++)
            {
                var child = node.Children[i];
                ConstructNode(frame, child, i);
            }
        }
    }
}