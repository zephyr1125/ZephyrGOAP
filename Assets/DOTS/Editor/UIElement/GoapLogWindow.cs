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
    public class GoapLogWindow : EditorWindow, IManipulator
    {
        [MenuItem("Zephyr/Goap/GoapLog")]
        private static void OpenWindow()
        {
            GetWindow<GoapLogWindow>().Show();
        }
        
        private static GoapLog _log;
        private int _currentResult;

        private VisualTreeAsset _nodeVisualTree;
        private VisualElement _nodeContainer;
        private VisualElement _statesTip;

        private static int NodeWidth = 320;
        private static int NodeHeight = 80;
        private static int NodeDistance = 16;
        
        private static Vector2 NodeSize = new Vector2(320, 80);

        private Vector2 _canvasPos, _canvasDragStartPos;
        private Vector2 _mouseDragStartPos;
        private bool _mouseMidButtonDown;

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
                    if (!LoadLogFile()) return;
                    Reset();
                    ConstructInfo();
                    ConstructGraph();
                });
            rootVisualElement.Q<Button>("reset-button").RegisterCallback<MouseUpEvent>(
                evt => Reset());
            rootVisualElement.AddManipulator(this);
            
            target.RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMoveEvent);
            target.RegisterCallback<MouseUpEvent>(OnMouseUpEvent);
            
            _canvasPos = Vector2.zero;
            
            //鼠标提示
            var statesVT =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/DOTS/Editor/UIElement/UXML/states.uxml");
            statesVT.CloneTree(rootVisualElement.Q("main-frame"));
            _statesTip = rootVisualElement.Q("states");
            _statesTip.style.top = -50;
        }
        
        private void Reset()
        {
            rootVisualElement.Clear();
            Init();
        }

        private bool LoadLogFile()
        {
            var path = EditorUtility.OpenFilePanel(
                "Import  Log", "", "json");
            if (!string.IsNullOrEmpty(path))
            {
                var textReader = new StreamReader(path);
                var json = textReader.ReadToEnd();
                _log = JsonUtility.FromJson<GoapLog>(json);
                return true;
            }

            return false;
        }

        private void ConstructInfo()
        {
            if (_log == null) return;

            rootVisualElement.Q<Label>("agent-name").text = _log.results[0].Agent.ToString();
        }

        private void ConstructGraph()
        {
            if (_log == null) return;

            _nodeContainer = rootVisualElement.Q("node-container");
            var nodeCounts = new List<int>();    //记录每一层的Node数量以便向下排列

            var goalNode = _log.results[_currentResult].GoalNodeView;
            ConstructNode(_nodeContainer, goalNode, ref nodeCounts);
            ConstructConnections(_nodeContainer, goalNode);
        }

        private void ConstructNode(VisualElement parent, NodeView node, ref List<int> nodeCounts)
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

            var drawPos = new Vector2(NodeDistance + iteration * (NodeWidth+NodeDistance),
                NodeDistance + nodeCounts[iteration] * (NodeHeight+NodeDistance));
            var frame = new NodeFrame(node, drawPos, NodeSize, _statesTip);
            parent.Add(frame);
            _nodeVisualTree.CloneTree(frame);
            
            frame.name = node.Name;
            frame.Q<Label>("name").text = node.Name;
            frame.Q<Label>("reward").text = node.Reward.ToString(CultureInfo.InvariantCulture);
            if(node.IsPath)frame.Q("titlebar").style.backgroundColor =
                new StyleColor(new Color(0f, 0.29f, 0.12f));
            
            Utils.AddStatesToContainer(frame.Q("states"), node.States);

            if (node.Children == null) return;
            for (var i = 0; i < node.Children.Count; i++)
            {
                var child = node.Children[i];
                ConstructNode(parent, child, ref nodeCounts);
            }
        }

        private void ConstructConnections(VisualElement parent, NodeView goalNode)
        {
            var connectionContainer = new IMGUIContainer(() =>
            {
                DrawConnection(goalNode);
                Handles.color = Color.white;
            });
            parent.Add(connectionContainer);
            connectionContainer.SendToBack();
        }

        private void DrawConnection(NodeView node)
        {
            if (node.Children == null) return;

            var childrenSum = node.Children.Count;
            for (var i = 0; i < node.Children.Count; i++)
            {
                var child = node.Children[i];
                var startPos = node.DrawPos + new Vector2(NodeWidth, NodeHeight / 2);
                var endPos = child.DrawPos + new Vector2(0, NodeHeight / 2);
                Handles.color = node.IsPath && child.IsPath ? Color.green : new Color(0.67f, 0.67f, 0.67f);
                Handles.DrawLine(startPos, endPos);
                DrawConnection(child);
            }
        }

        public VisualElement target { get; set; }

        private void OnMouseDownEvent(MouseEventBase<MouseDownEvent> evt)
        {
            switch (evt.button)
            {
                case 2:
                    //中键
                    if (_nodeContainer == null) return;
                    _mouseMidButtonDown = true;
                    _mouseDragStartPos = evt.mousePosition;
                    _canvasDragStartPos = _canvasPos;
                    break;
            }
        }

        private void OnMouseMoveEvent(MouseEventBase<MouseMoveEvent> evt)
        {
            if (_mouseMidButtonDown)
            {
                if (_nodeContainer == null) return;
                var distance = evt.mousePosition - _mouseDragStartPos;
                _canvasPos = _canvasDragStartPos + distance;
                _nodeContainer.style.left = _canvasPos.x;
                _nodeContainer.style.top = _canvasPos.y;
            }
        }

        private void OnMouseUpEvent(MouseEventBase<MouseUpEvent> evt)
        {
            switch (evt.button)
            {
                case 2:
                    //中键
                    if (_nodeContainer == null) return;
                    _mouseMidButtonDown = false;
                    break;
            }
        }
    }
}