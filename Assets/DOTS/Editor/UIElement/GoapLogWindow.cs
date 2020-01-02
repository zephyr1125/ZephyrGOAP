using System;
using System.Collections.Generic;
using System.Globalization;
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

        private static int NodeWidth = 320;
        private static int NodeHeight = 80;
        private static int NodeDistance = 32;

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
                    ConstructConnections();
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
            var nodeCounts = new List<int>();    //记录每一层的Node数量以便向下排列

            ConstructNode(mainFrame, _log.results[_currentResult].GoalNodeView, ref nodeCounts);
        }

        private void ConstructNode(VisualElement frame, NodeView node, ref List<int> nodeCounts)
        {
            var iteration = node.Iteration;
            if (nodeCounts.Count <= iteration)
            {
                nodeCounts.Add(0);
            }
            else
            {
                nodeCounts[iteration]++;
            }
            
            _nodeVisualTree.CloneTree(frame);
            var nodeVE = frame.Q("frame");
            nodeVE.style.left = NodeDistance + iteration * (NodeWidth+NodeDistance);
            nodeVE.style.top = NodeDistance + nodeCounts[iteration] * (NodeHeight+NodeDistance);
            
            nodeVE.name = node.Name;
            nodeVE.Q<Label>("name").text = node.Name;
            nodeVE.Q<Label>("reward").text = node.Reward.ToString(CultureInfo.InvariantCulture);
            AddStatesToNode(nodeVE.Q("states"), node.States);
            
            if (node.Children == null) return;
            for (var i = 0; i < node.Children.Count; i++)
            {
                var child = node.Children[i];
                ConstructNode(frame, child, ref nodeCounts);
            }
        }

        private void AddStatesToNode(VisualElement container, StateView[] states)
        {
            var stateTexts = new List<string>(states.Length);
            for (var i = 0; i < states.Length; i++)
            {
                stateTexts.Add(states[i].ToString());
            }
            Func<VisualElement> makeItem = () => new Label();
            Action<VisualElement, int> bindItem = (e, i) => ((Label) e).text = stateTexts[i];
            var list = new ListView(stateTexts, 16, makeItem, bindItem);
            list.selectionType = SelectionType.None;
            list.style.flexGrow = 1;
            
            container.Add(list);
        }

        private void ConstructConnections()
        {
            var connectionContainer = new IMGUIContainer(() =>
            {
                Handles.DrawBezier(Vector3.zero, new Vector3(100, 100),
                    Vector3.right*50, new Vector3(50, 100),
                    Color.white, null, 2);
            });
            var parent = rootVisualElement.Q("main-frame");
            parent.Add(connectionContainer);
            connectionContainer.style.position = new StyleEnum<Position>(Position.Absolute);
            connectionContainer.style.width = parent.style.width;
            connectionContainer.style.height = parent.style.height;
            connectionContainer.SendToBack();
        }
    }
}