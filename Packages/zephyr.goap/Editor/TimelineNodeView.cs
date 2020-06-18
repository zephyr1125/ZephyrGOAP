using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;
using Zephyr.GOAP.Logger;

namespace Zephyr.GOAP.Editor
{
    public class TimelineNodeView : VisualElement,IManipulator
    {
        public VisualElement target { get; set; }

        public int NodeHash;

        public EntityLog Agent;
        
        public Entity NodeEntity;
        
        public Vector2 ExecuteStartPosition, ExecuteEndPosition;

        public TimeLineNodeStatus Status;
        
        private Label _nameLabel;
        private float _nameSize = 11;

        public TimelineNodeView(VisualTreeAsset nodeVisualTree, Vector2 startPosition, NodeLog nodeLog,
            List<EntityLog> agentEntities)
        {
            NodeHash = nodeLog.hashCode;
            Agent = nodeLog.agentExecutorEntity;
            NodeEntity = nodeLog.pathNodeEntity;
            Status = TimeLineNodeStatus.Idle;
            
            var agentId = agentEntities.FindIndex(agent => agent.Equals(nodeLog.agentExecutorEntity));
            var executorLog = nodeLog.nodeAgentInfos
                .First(info => info.agentEntity.Equals(nodeLog.agentExecutorEntity));
            var navigateTime = executorLog.navigateTime;
            var executeTime = executorLog.executeTime;
            var startTime = nodeLog.totalTime - executeTime - navigateTime;
            
            nodeVisualTree.CloneTree(this);

            var tileX = (float)TimelineView.PixelsPerSecond;
            var tileY = (float) TimelineView.TileY;

            style.position = new StyleEnum<Position>(Position.Absolute);
            style.left = startPosition.x + startTime * tileX;
            style.top = startPosition.y - tileY / 2 + tileY * 2 * agentId;
            
            var frame = this.Q("frame");
            var width = (navigateTime + executeTime) * tileX;
            frame.style.width = width <= 0 ? 1 : width;    //即使瞬间完成的事情，也给2像素宽度以便可见
            frame.style.height = tileY;
            frame.style.backgroundColor = Utils.GetAgentColor(nodeLog.agentExecutorEntity);

            var actionName = nodeLog.name.Replace("Action", "");
            _nameLabel = this.Q<Label>("name");
            //如果宽度太窄，则使用大写简称，如果还不够，名字写在下方
            if (width < tileX)
            {
                var rx = new Regex(@"[a-z]");
                actionName = rx.Replace(actionName, "");
                
                if (width <= tileX/4)
                {
                    _nameLabel.style.top = tileY / 2 + 6;
                }
            }
            _nameLabel.text = actionName;
            
            //导航耗时
            var navigating = this.Q("navigating");
            var navigatingWidth = navigateTime * tileX;
            //如果几乎全是导航时间，出于美观，至少给执行留4像素
            navigating.style.width = navigatingWidth >= width-4 ? width-4 : navigatingWidth;
            
            ExecuteStartPosition = new Vector2(
                style.left.value.value+navigating.style.width.value.value,
                style.top.value.value+tileY/2);
            ExecuteEndPosition = new Vector2(
                style.left.value.value + frame.style.width.value.value,
                style.top.value.value+tileY/2);
            
            this.AddManipulator(this);
        }

        public void SetStatus(TimeLineNodeStatus status)
        {
            if (Status == status) return;

            Status = status;
            switch (status)
            {
                case TimeLineNodeStatus.Done:
                    _nameLabel.style.color = Color.gray;
                    _nameLabel.style.fontSize = _nameSize;
                break;
                case TimeLineNodeStatus.Acting:
                    _nameLabel.style.color = Color.green;
                    _nameLabel.style.fontSize = _nameSize+4;
                break;
            }
        }
    }
    
    public enum TimeLineNodeStatus
    {
        Idle, Acting, Done
    }
}