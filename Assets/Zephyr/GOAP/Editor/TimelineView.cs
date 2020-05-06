using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Zephyr.GOAP.Logger;

namespace Zephyr.GOAP.Editor
{
    public class TimelineView : VisualElement, IManipulator
    {
        public static int PixelsPerSecond = 64;
        public static int TileY = 32;

        private VisualElement _container;

        private VisualTreeAsset _nodeVisualTree;
        
        public VisualElement target { get; set; }

        public TimelineView(VisualElement target)
        {
            this.target = target;
            _container = target.Q("timeline-container");
            
            _nodeVisualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Zephyr/GOAP/Editor/UXML/timeline_node.uxml");
            
            //绘制背景
            DrawBackground();
                
            //设置点击缩放
            target.Q("timeline-title").RegisterCallback<MouseDownEvent>(SetWindowSize);
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
                if (node.name.Equals("goal")) continue;
                pathNodes.Add(node);
            }
            
            //摆放node
            var timelineNodeViews = new List<TimelineNodeView>();
            var agentEntities = new List<EntityLog>();
            var startPosition = new Vector2(PixelsPerSecond, PixelsPerSecond);
            for (var i = 0; i < pathNodes.Count; i++)
            {
                var node = pathNodes[i];
                if (!agentEntities.Exists(agent => agent.Equals(node.agentExecutorEntity)))
                {
                    agentEntities.Add(node.agentExecutorEntity);
                }

                var newNodeView =
                    new TimelineNodeView(_nodeVisualTree, startPosition, node, agentEntities);
                timelineNodeViews.Add(newNodeView);
                _container.Add(newNodeView);
            }
            
            //根据依赖画线
            var connectionContainer = new IMGUIContainer(() =>
            {
                foreach (var dependency in goapResult.pathDependencies)
                {
                    DrawConnection(timelineNodeViews, dependency);
                }
                Handles.color = Color.white;
            });
            _container.Add(connectionContainer);
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

            if (startAgent!=null && startAgent.Equals(endAgent)) return;
            
            Handles.color = Color.green;
            Handles.DrawLine(startPos, endPos);
        }
    }
}