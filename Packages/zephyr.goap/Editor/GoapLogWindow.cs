using System.Collections.Generic;
using System.IO;
using Unity.Entities;
using UnityEditor;
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

        private TimelineView _timelineView;

        private VisualTreeAsset _nodeVisualTree;
        private VisualElement _nodeContainer;
        private VisualElement _statesTip;
        private Button _editorLoggingButton, _autoPageButton;
        private VisualElement _baseStatesContainer;

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

        private void OnEnable()
        {
            Init();
        }

        private void Init()
        {
            rootVisualElement.Clear();
            
            titleContent.text = "Goap Logs";

            _editorDebugger = new EditorGoapDebugger(OnEditorLogDone);
            _currentResult = 0;
            
            var windowVisualTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Utils.WindowFilePath);
            windowVisualTree.CloneTree(rootVisualElement);
            
            _nodeVisualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Utils.NodeFilePath);

            rootVisualElement.Q<Button>("load-button").clicked += () =>
            {
                if (!LoadLogFile()) return;
                Reset();
                Init();
                ConstructInfo();
                ConstructGraph();
                ConstructTimeline();
            };
            rootVisualElement.Q<Button>("reset-button").clicked += () =>
            {
                Reset();
                Init();
            };
            
            _editorLoggingButton = rootVisualElement.Q<Button>("editor-button");
            _editorLoggingButton.clicked += SetEditorLogging;

            _autoPageButton = rootVisualElement.Q<Button>("auto-button");
            _autoPageButton.clicked += SetAutoPage;
            
            rootVisualElement.Q<Button>("prev-button").clicked += PrevResult;
            rootVisualElement.Q<Button>("next-button").clicked += NextResult;
            
            _baseStatesContainer = rootVisualElement.Q("unity-content");
            
            rootVisualElement.AddManipulator(this);
            //拖拽node graph
            var mainFrame = target.Q("main-frame");
            mainFrame.RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
            mainFrame.RegisterCallback<MouseMoveEvent>(OnMouseMoveEvent);
            mainFrame.RegisterCallback<MouseUpEvent>(OnMouseUpEvent);
            //切换log
            target.RegisterCallback<WheelEvent>(OnMouseWheelEvent);
            target.RegisterCallback<KeyDownEvent>(OnKeyDownEvent);
            
            _nodeContainer = rootVisualElement.Q("node-container");
            
            _canvasPos = Vector2.zero;

            _editorLogging = false;
            _autoPage = false;
            
            //鼠标提示
            var statesVT =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Utils.StateFilePath);
            statesVT.CloneTree(rootVisualElement.Q("main-frame"));
            _statesTip = rootVisualElement.Q("states");
            _statesTip.style.top = -9999;

            EditorApplication.playModeStateChanged += OnPlayModeChange;
            
            //Timeline
            var timelineElement = rootVisualElement.Q("timeline");
            _timelineView = new TimelineView(timelineElement);
        }
        
        private void Reset()
        {
            _nodeContainer?.Clear();
            _baseStatesContainer?.Clear();
            _timelineView?.Clear();
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
                    World.DefaultGameObjectInjectionWorld
                        .GetOrCreateSystem<GoalPlanningSystemBase>()
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
                _log.CheckForSameHash();
                return true;
            }

            return false;
        }

        private void ConstructInfo()
        {
            if (_log == null) return;

            _resultCount = _log.results.Count;
            var labelPage = rootVisualElement.Q<Label>("page");
            labelPage.text = $"{_currentResult+1}/{_resultCount}";

            var result = _log.results[_currentResult];
            rootVisualElement.Q<Label>("agent-name").text = 
                $"{result.timeCost}ms at ({result.timeStart})";
            
            _baseStatesContainer.Clear();
            foreach (var states in _log.results[_currentResult].baseStates)
            {
                _baseStatesContainer.Add(new Label(states.ToString()));
            }
        }

        private void ConstructGraph()
        {
            if (_log == null) return;
            var nodeCounts = new List<int>();    //记录每一层的Node数量以便向下排列

            var nodes = _log.results[_currentResult].nodes;
            ConstructNode(_nodeContainer, nodes, 0, nodeCounts);
            ConstructConnections(_nodeContainer, nodes, _log.results[_currentResult].edges);
        }

        private void ConstructNode(VisualElement parent, List<NodeLog>nodes, int id, List<int> nodeCounts)
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
            var frame = new NodeView(this, node, drawPos, NodeSize, _statesTip);
            parent.Add(frame);
            _nodeVisualTree.CloneTree(frame);
            
            frame.name = node.name;
            frame.Q<Label>("name").text = 
                $"{node.name.Replace("Action","")}[{node.agentExecutorEntity}]=>[{node.navigationSubject}]";
            frame.Q<Label>("time").text = $"{node.estimateNavigateStartTime:F1}>{node.totalTime:F1}";
            frame.Q<Label>("reward").text = $"{node.reward}/{node.rewardSum}";
            
            if (node.isPath && !node.agentExecutorEntity.Equals(Entity.Null))
            {
                frame.Q("titlebar").style.backgroundColor = Utils.GetAgentColor(node.agentExecutorEntity);
            }else if (node.isDeadEnd)
            {
                frame.Q("titlebar").style.backgroundColor = new Color(0.5f, 0f, 0f);
            }

            Utils.AddStatesToContainer(frame.Q("states"), node.requires);

            if (id >= nodes.Count - 1) return;
            ConstructNode(parent, nodes, id+1, nodeCounts);
        }

        private void ConstructTimeline()
        {
            _timelineView.ConstructTimeline(_log.results[_currentResult]);
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
            ConstructTimeline();
            if (_autoPage) _currentResult = _log.results.Count - 1;
        }

        private void PrevResult()
        {
            if (_currentResult <= 0) return;
            
            _currentResult--;
            Reset();
            ConstructInfo();
            ConstructGraph();
            ConstructTimeline();;
        }

        private void NextResult()
        {
            if (_currentResult >= _resultCount - 1) return;
            
            _currentResult++;
            Reset();
            ConstructInfo();
            ConstructGraph();
            ConstructTimeline();
        }

        public void MoveCloseRelativeNodes(NodeView baseNodeView, bool isMoveParent)
        {
            var goapResult = _log.results[_currentResult];
            var baseNode = baseNodeView.Node;
            var relativeId = 0;
            foreach (var ve in _nodeContainer.Children())
            {
                if (!(ve is NodeView)) continue;
                var nodeView = (NodeView) ve;
                var nodeHash = nodeView.Node.hashCode;
                var isRelative = false;
                foreach (var edgeLog in goapResult.edges)
                {
                    if (!edgeLog.childHash.Equals(isMoveParent?baseNode.hashCode:nodeHash)) continue;
                    if (!edgeLog.parentHash.Equals(isMoveParent?nodeHash:baseNode.hashCode)) continue;
                    isRelative = true;
                    break;
                }

                if (!isRelative) continue;

                var deltaX = NodeWidth + 32;
                nodeView.MoveTo(baseNode.DrawPos
                                + new Vector2( isMoveParent?-deltaX:deltaX, relativeId*(NodeHeight + 32)));
                relativeId++;
            }
        }

        private void Update()
        {
            if (_editorLogging && EditorApplication.isPlaying && _timelineView!=null)
            {
                _timelineView.OnUpdate();
            }
        }
    }
}