using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Unity.Entities;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.UIElements;
using Zephyr.GOAP.Logger;
using Zephyr.GOAP.System;

namespace Zephyr.GOAP.Editor
{
    public class GoapLogWindow : EditorWindow, IManipulator
    {
        [MenuItem("Zephyr/Goap/GoapLog")]
        private static void OpenWindow()
        {
            GetWindow<GoapLogWindow>().Show();
        }
        
        private static GoapLog _log;
        private int _resultCount;
        private int _currentResult;

        private VisualTreeAsset _nodeVisualTree;
        private VisualElement _nodeContainer;
        private VisualElement _statesTip;
        private Button _editorLoggingButton, _autoPageButton;
        private VisualElement _currentStatesContainer;

        private static int NodeWidth = 320;
        private static int NodeHeight = 80;
        private static int NodeDistance = 16;
        
        private static Vector2 NodeSize = new Vector2(320, 80);

        private Vector2 _canvasPos, _canvasDragStartPos;
        private Vector2 _mouseDragStartPos;
        private bool _mouseMidButtonDown;

        private bool _editorLogging;
        private bool _autoPage;

        private EditorGoapDebugger _editorDebugger;
        
        private static Dictionary<Entity, StyleColor> _agentColors;
        private static readonly Color BaseAgentColor = new Color(0f, 0.29f, 0.12f);

        private void OnEnable()
        {
            Init();
        }

        private void Init()
        {
            titleContent.text = "Goap Logs";
            rootVisualElement.Clear();
            
            _editorDebugger = new EditorGoapDebugger(OnEditorLogDone);
            _currentResult = 0;
            
            var windowVisualTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Zephyr/GOAP/Editor/UXML/window.uxml");
            windowVisualTree.CloneTree(rootVisualElement);
            
            _nodeVisualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Zephyr/GOAP/Editor/UXML/node.uxml");
            
            rootVisualElement.Q<Button>("load-button").RegisterCallback<MouseUpEvent>(
                evt =>
                {
                    if (!LoadLogFile()) return;
                    Reset();
                    ConstructInfo();
                    ConstructGraph();
                });
            rootVisualElement.Q<Button>("reset-button").RegisterCallback<MouseUpEvent>(
                evt =>
                {
                    Reset();
                    Init();
                });
            
            _editorLoggingButton = rootVisualElement.Q<Button>("editor-button");
            _editorLoggingButton.clicked += SetEditorLogging;

            _autoPageButton = rootVisualElement.Q<Button>("auto-button");
            _autoPageButton.clicked += SetAutoPage;
            
            rootVisualElement.Q<Button>("prev-button").clicked += PrevResult;
            rootVisualElement.Q<Button>("next-button").clicked += NextResult;
            
            _currentStatesContainer = rootVisualElement.Q("current-states-container");
            
            rootVisualElement.AddManipulator(this);
            target.RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMoveEvent);
            target.RegisterCallback<WheelEvent>(OnMouseWheelEvent);
            target.RegisterCallback<MouseUpEvent>(OnMouseUpEvent);
            target.RegisterCallback<KeyDownEvent>(OnKeyDownEvent);
            
            _nodeContainer = rootVisualElement.Q("node-container");
            
            _canvasPos = Vector2.zero;

            _editorLogging = false;
            _autoPage = false;
            
            //鼠标提示
            var statesVT =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Zephyr/GOAP/Editor/UXML/states.uxml");
            statesVT.CloneTree(rootVisualElement.Q("main-frame"));
            _statesTip = rootVisualElement.Q("states");
            _statesTip.style.top = -50;

            EditorApplication.playModeStateChanged += OnPlayModeChange;
        }
        
        private void Reset()
        {
            _nodeContainer?.Clear();
            _currentStatesContainer?.Clear();
        }

        private void SetEditorLogging()
        {
            _editorLogging = !_editorLogging;
            _editorLoggingButton.text = "Editor|" + (_editorLogging ? "ON" : "OFF");
        }

        private void SetAutoPage()
        {
            _autoPage = !_autoPage;
            _autoPageButton.text = "Auto|" + (_autoPage ? "ON" : "OFF");
        }

        private void OnPlayModeChange(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    if (!_editorLogging) return;
                    _currentResult = 0;
                    Reset();
                    World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<GoalPlanningSystem>()
                        .Debugger = _editorDebugger;
                    break;
            }
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

            _resultCount = _log.results.Count;
            rootVisualElement.Q<Label>("page").text = $"{_currentResult+1}/{_resultCount}";

            var result = _log.results[_currentResult];
            rootVisualElement.Q<Label>("agent-name").text = 
                $"{result.timeCost}ms at ({result.timeStart})";
            
            _currentStatesContainer.Clear();
            foreach (var states in _log.results[_currentResult].currentStates)
            {
                _currentStatesContainer.Add(new Label(states.ToString()));
            }
        }

        private void ConstructGraph()
        {
            if (_log == null) return;
            var nodeCounts = new List<int>();    //记录每一层的Node数量以便向下排列

            var nodes = _log.results[_currentResult].nodes;
            ConstructNode(_nodeContainer, ref nodes, 0, ref nodeCounts);
            ConstructConnections(_nodeContainer, nodes, _log.results[_currentResult].edges);
        }

        private void ConstructNode(VisualElement parent, ref List<NodeLog>nodes, int id, ref List<int> nodeCounts)
        {
            var node = nodes[id];
            var iteration = node.iteration;
            while (nodeCounts.Count <= iteration)
            {
                nodeCounts.Add(0);
            }
            nodeCounts[iteration]++;

            var drawPos = new Vector2(NodeDistance + iteration * (NodeWidth+NodeDistance),
                NodeDistance + nodeCounts[iteration] * (NodeHeight+NodeDistance));
            var frame = new NodeView(node, drawPos, NodeSize, _statesTip);
            parent.Add(frame);
            _nodeVisualTree.CloneTree(frame);
            
            frame.name = node.name;
            frame.Q<Label>("name").text = $"{node.name}[{node.agentExecutorEntity}]";
            frame.Q<Label>("reward").text = node.reward.ToString(CultureInfo.InvariantCulture);
            
            if (node.isPath)
            {
                frame.Q("titlebar").style.backgroundColor = GetAgentColor(node.agentExecutorEntity);
            };
            
            
            Utils.AddStatesToContainer(frame.Q("states"), node.states);

            if (id >= nodes.Count - 1) return;
            ConstructNode(parent, ref nodes, id+1, ref nodeCounts);
        }
        
        private StyleColor GetAgentColor(EntityLog agentEntity)
        {
            var agentEntityStruct = new Entity{Index = agentEntity.index, Version = agentEntity.version};
            if(_agentColors == null)_agentColors = new Dictionary<Entity, StyleColor>();

            if (!_agentColors.ContainsKey(agentEntityStruct))
            {
                var agentSum = _agentColors.Count;
                var sumR = BaseAgentColor.r + 0.3f*agentSum;
                var sumG = BaseAgentColor.g + 0.3f*agentSum;
                var sumB = BaseAgentColor.b + 0.3f*agentSum;
                var color = new Color(sumR - (int) sumR, sumG - (int) sumG, sumB - (int) sumB);
                
                _agentColors.Add(agentEntityStruct, color);
            }

            return _agentColors[agentEntityStruct];
        }

        private void ConstructConnections(VisualElement parent, List<NodeLog> nodes, List<EdgeLog> edges)
        {
            var connectionContainer = new IMGUIContainer(() =>
            {
                foreach (var edge in edges)
                {
                    DrawConnection(nodes, edge);
                }
                Handles.color = Color.white;
            });
            parent.Add(connectionContainer);
            connectionContainer.SendToBack();
        }

        private void DrawConnection(List<NodeLog> nodes, EdgeLog edge)
        {
            var parentNode = nodes.Find(node => node.hashCode == edge.parentHash);
            var childNode = nodes.Find(node => node.hashCode == edge.childHash);
            
            var startPos = parentNode.DrawPos + new Vector2(NodeWidth, NodeHeight / 2);
            var endPos = childNode.DrawPos + new Vector2(0, NodeHeight / 2);
            Handles.color = parentNode.isPath && childNode.isPath ? Color.green : new Color(0.67f, 0.67f, 0.67f);
            Handles.DrawLine(startPos, endPos);
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
        
        private void OnMouseWheelEvent(WheelEvent evt)
        {
            if (evt.delta.y > 0)
            {
                NextResult();
            }
            else
            {
                PrevResult();
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

        private void OnKeyDownEvent(KeyDownEvent evt)
        {
            Debug.Log(evt.keyCode);
            switch (evt.keyCode)
            {
                case KeyCode.RightArrow:
                    NextResult();
                    break;
                case KeyCode.LeftArrow:
                    PrevResult();
                    break;
            }
        }

        private void OnEditorLogDone(GoapLog log)
        {
            if (!_editorLogging) return;
            _log = log;
            Reset();
            ConstructInfo();
            ConstructGraph();
            if (_autoPage) _currentResult = _log.results.Count - 1;
        }

        private void PrevResult()
        {
            if (_currentResult <= 0) return;
            
            _currentResult--;
            Reset();
            ConstructInfo();
            ConstructGraph();
        }

        private void NextResult()
        {
            if (_currentResult >= _resultCount - 1) return;
            
            _currentResult++;
            Reset();
            ConstructInfo();
            ConstructGraph();
        }
    }
}