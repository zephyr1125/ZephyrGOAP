using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Zephyr.GOAP.Logger;

namespace Zephyr.GOAP.Editor
{
    public class TimelineView : VisualElement, IManipulator
    {
        public static int PixelsPerSecond = 32;

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
                for (var y = 0; y < windowSize.height; y+=PixelsPerSecond)
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
            //首先寻找起头的所有node
            var headNodes = new List<NodeLog>();
            var pathDependencies = goapResult.pathDependencies;
            foreach (var node in goapResult.nodes)
            {
                if (!node.isPath) continue;
                if (node.name.Equals("goal")) continue;
                var nodeHash = node.hashCode;
                if (pathDependencies.Exists(dependency => dependency.baseNodeHash.Equals(nodeHash)))
                    continue;
                headNodes.Add(node);
            }
            
            var agentEntities = new List<EntityLog>();
            var startPosition = new Vector2(PixelsPerSecond, PixelsPerSecond);
            //摆放到窗口中
            for (var i = 0; i < headNodes.Count; i++)
            {
                var node = headNodes[i];
                if (!agentEntities.Exists(agent => agent.Equals(node.agentExecutorEntity)))
                {
                    agentEntities.Add(node.agentExecutorEntity);
                }
                _container.Add(new TimelineNodeView(_nodeVisualTree, startPosition, node, agentEntities));
            }
        }
    }
}