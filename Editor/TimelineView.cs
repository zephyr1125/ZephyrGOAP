using System.Collections.Generic;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Zephyr.GOAP.Component.ActionNodeState;
using Zephyr.GOAP.Logger;

namespace Zephyr.GOAP.Editor
{
    public class TimelineView : VisualElement, IManipulator
    {
        public static int PixelsPerSecond = 64;
        public static int TileY = 32;

        private VisualElement _container;

        private VisualTreeAsset _nodeVisualTree;
        
        private Vector2 _canvasPos, _canvasDragStartPos;
        private Vector2 _mouseDragStartPos;
        private bool _mouseMidButtonDown;

        private List<TimelineNodeView> _timelineNodeViews;

        private EntityManager _entityManager;

        /// <summary>
        /// 刷新频率，避免太影响性能
        /// </summary>
        private float _updateTimeInterval = 0.2f;
        private float _lastUpdateTime;
        
        public VisualElement target { get; set; }

        public TimelineView(VisualElement target)
        {
            _timelineNodeViews = new List<TimelineNodeView>();
            this.target = target;
            _container = target.Q("timeline-container");
            
            _nodeVisualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Utils.TimeLineNodeFilePath);
            
            //绘制背景
            DrawBackground();
                
            //设置点击展开/关闭
            target.Q("timeline-title").RegisterCallback<MouseDownEvent>(SetWindowSize);
            
            //设置拖拽
            target.RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMoveEvent);
            target.RegisterCallback<MouseUpEvent>(OnMouseUpEvent);
        }

        private void DrawBackground()
        {
            var backgroundContainer = new IMGUIContainer(() =>
            {
                Handles.color = new Color(0.22f, 0.22f, 0.22f);
                var windowSize = _container.layout;
                for (var x = 0; x < windowSize.width; x+=PixelsPerSecond)
                {
                    Handles.DrawLine(new Vector3(x, 0), new Vector3(x, windowSize.height));
                    
                }
                for (var y = 0; y < windowSize.height; y+=TileY)
                {
                    Handles.DrawLine(new Vector3(0, y), new Vector3(windowSize.width, y));
                }
                Handles.color = Color.white;
            });
            _container.Add(backgroundContainer);
            backgroundContainer.SendToBack();
        }

        private void SetWindowSize(MouseEventBase<MouseDownEvent> evt)
        {
            var grow = target.style.flexGrow;
            target.style.flexGrow = grow == 0 ? 1 : 0;
        }

        public void ConstructTimeline(GoapResult goapResult)
        {
            //构建nodes
            var pathNodes = new List<NodeLog>();
            foreach (var node in goapResult.nodes)
            {
                if (!node.isPath) continue;
                pathNodes.Add(node);
            }
            
            //摆放node
            var agentEntities = new List<EntityLog>();
            var startPosition = new Vector2(PixelsPerSecond*2, TileY*1.5f);
            var totalTime = 0f;
            for (var i = 0; i < pathNodes.Count; i++)
            {
                var node = pathNodes[i];
                if (node.agentExecutorEntity.IsDefault()) continue;
                if (!agentEntities.Exists(agent => agent.Equals(node.agentExecutorEntity)))
                {
                    agentEntities.Add(node.agentExecutorEntity);
                }

                var newNodeView =
                    new TimelineNodeView(_nodeVisualTree, startPosition, node, agentEntities);
                _timelineNodeViews.Add(newNodeView);
                _container.Add(newNodeView);
                if (node.totalTime > totalTime) totalTime = node.totalTime;
            }
            
            
            var connectionContainer = new IMGUIContainer(() =>
            {
                //绘制执行者
                DrawExecutor(agentEntities, startPosition);
                
                //绘制依赖
                foreach (var dependency in goapResult.pathDependencies)
                {
                    DrawConnection(_timelineNodeViews, dependency);
                }
                Handles.color = Color.white;
                
                //绘制时间轴
                DrawTimeAxis(totalTime, startPosition, agentEntities.Count);
            });
            _container.Add(connectionContainer);
                        
        }

        private void DrawExecutor(List<EntityLog> agentEntities, Vector2 startPosition)
        {
            startPosition.x -= PixelsPerSecond * 1.5f;
            var labelStyle = new GUIStyle {normal = {textColor = Color.white}, fontSize = 12, fontStyle = FontStyle.Bold};
            for (var i = 0; i < agentEntities.Count; i++)
            {
                var text = $"Agent [{agentEntities[i]}]";
                Handles.Label(startPosition + new Vector2(0, i*2*TileY-TileY/4), text, labelStyle);
            }
        }

        private void DrawTimeAxis(float totalTime, Vector2 startPosition, int agentEntitiesCount)
        {
            startPosition.y += (agentEntitiesCount+1.5f) * TileY;
            //横线
            Handles.DrawLine(startPosition, startPosition+new Vector2(totalTime*PixelsPerSecond, 0));
            var labelStyle = new GUIStyle {normal = {textColor = Color.white}, alignment = TextAnchor.UpperCenter};
            //刻度
            for (var i = 0; i <= (int)totalTime; i++)
            {
                var drawStartPosition = startPosition + new Vector2(i * PixelsPerSecond, 0);
                Handles.DrawLine(drawStartPosition, startPosition + new Vector2(i * PixelsPerSecond, (float)-TileY/4));
                
                Handles.Label(drawStartPosition + new Vector2(0, 2), i.ToString(), labelStyle);
            }
        }

        private void DrawConnection(List<TimelineNodeView> nodeViews, NodeDependencyLog dependency)
        {
            EntityLog startAgent = null;
            EntityLog endAgent = null;
            var startPos = Vector2.zero;
            var endPos = Vector2.zero;
            foreach (var nodeView in nodeViews)
            {
                if (nodeView.NodeHash.Equals(dependency.dependencyNodeHash))
                {
                    startAgent = nodeView.Agent;
                    startPos = nodeView.ExecuteEndPosition;
                }

                if (nodeView.NodeHash.Equals(dependency.baseNodeHash))
                {
                    endAgent = nodeView.Agent;
                    endPos = nodeView.ExecuteStartPosition;
                }
            }

            // if (startAgent!=null && startAgent.Equals(endAgent)) return;
            
            Handles.color = Color.green;
            Handles.DrawLine(startPos, endPos);
        }
        
        private void OnMouseDownEvent(MouseEventBase<MouseDownEvent> evt)
        {
            switch (evt.button)
            {
                case 2:
                    //中键
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
                var distance = evt.mousePosition - _mouseDragStartPos;
                _canvasPos = _canvasDragStartPos + distance;
                _container.style.left = _canvasPos.x;
                _container.style.top = _canvasPos.y;
            }
        }
        
        private void OnMouseUpEvent(MouseEventBase<MouseUpEvent> evt)
        {
            switch (evt.button)
            {
                case 2:
                    //中键
                    _mouseMidButtonDown = false;
                    break;
            }
        }

        public void OnUpdate()
        {
            var time = Time.time;
            if (time - _lastUpdateTime < _updateTimeInterval)
            {
                return;
            }
            _lastUpdateTime = time;
            
            if (_entityManager == null)
            {
                _entityManager = World.All[0].EntityManager;
            }

            //展示进行中与已完成的node
            foreach (var nodeView in _timelineNodeViews)
            {
                if (nodeView.Status == TimeLineNodeStatus.Done) continue;
                
                var nodeEntity = nodeView.NodeEntity;
                if (!_entityManager.Exists(nodeEntity))
                {
                    nodeView.SetStatus(TimeLineNodeStatus.Done);
                }else if (_entityManager.HasComponent<ActionNodeActing>(nodeEntity))
                {
                    nodeView.SetStatus(TimeLineNodeStatus.Acting);
                }
            }
        }
    }
}