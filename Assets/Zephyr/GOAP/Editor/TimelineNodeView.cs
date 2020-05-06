using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Zephyr.GOAP.Logger;

namespace Zephyr.GOAP.Editor
{
    public class TimelineNodeView : VisualElement,IManipulator
    {
        public VisualElement target { get; set; }

        private NodeLog _nodeLog;

        public TimelineNodeView(VisualTreeAsset nodeVisualTree, Vector2 startPosition, NodeLog nodeLog,
            List<EntityLog> agentEntities)
        {
            _nodeLog = nodeLog;
            var agentId = agentEntities.FindIndex(agent => agent.Equals(_nodeLog.agentExecutorEntity));
            var executorLog = nodeLog.nodeAgentInfos
                .First(info => info.agentEntity.Equals(nodeLog.agentExecutorEntity));
            var navigateTime = executorLog.navigateTime;
            var executeTime = executorLog.executeTime;
            var startTime = _nodeLog.totalTime - executeTime - navigateTime;
            
            nodeVisualTree.CloneTree(this);

            var tileSize = (float)TimelineView.PixelsPerSecond;

            style.position = new StyleEnum<Position>(Position.Absolute);
            style.left = startPosition.x + startTime * tileSize;
            style.top = startPosition.y - tileSize / 2 + tileSize * 2 * agentId;
            
            var frame = this.Q("frame");
            frame.style.width = (navigateTime + executeTime) * tileSize;
            frame.style.height = tileSize;
            frame.style.backgroundColor = Utils.GetAgentColor(_nodeLog.agentExecutorEntity);

            this.Q<Label>("name").text = nodeLog.name.Replace("Action", "");
            
            //导航耗时
            var navigating = this.Q("navigating");
            
            navigating.style.width = navigateTime * tileSize;
            
            this.AddManipulator(this);
        }
    }
}