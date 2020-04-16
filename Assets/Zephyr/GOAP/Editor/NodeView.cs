using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Zephyr.GOAP.Logger;

namespace Zephyr.GOAP.Editor
{
    public class NodeView : VisualElement, IManipulator
    {
        public NodeLog Node;

        private VisualElement _statesTip;

        private bool _mouse0Down;

        private Vector2 _mouseDragStartPos, _frameDragStartPos;

        public NodeView(NodeLog node, Vector2 drawPos, Vector2 size, VisualElement statesTip)
        {
            Node = node;
            Node.DrawPos = drawPos;
            
            style.position = new StyleEnum<Position>(Position.Absolute);
            style.left = drawPos.x;
            style.top = drawPos.y;
            style.width = size.x;
            style.height = size.y;
            
            this.AddManipulator(this);
            target.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            //拖拽
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);

            _statesTip = statesTip;
        }

        public VisualElement target { get; set; }

        private void OnMouseEnter(MouseEventBase<MouseEnterEvent> evt)
        {
            SetStatesTip();
            UpdateStatesTipPos(evt.mousePosition);
        }
        
        private void OnMouseMove(MouseEventBase<MouseMoveEvent> evt)
        {
            UpdateStatesTipPos(evt.mousePosition);
            if (_mouse0Down)
            {
                var distance = evt.mousePosition - _mouseDragStartPos;
                Node.DrawPos = _frameDragStartPos + distance;
                style.left = Node.DrawPos.x;
                style.top = Node.DrawPos.y;
            }
        }
        
        private void OnMouseLeave(MouseEventBase<MouseLeaveEvent> evt)
        {
            UpdateStatesTipPos(new Vector2(0, -100));
        }
        
        private void OnMouseDown(MouseEventBase<MouseDownEvent> evt)
        {
            switch (evt.button)
            {
                case 0:
                    //左键
                    _mouse0Down = true;
                    _mouseDragStartPos = evt.mousePosition;
                    _frameDragStartPos = Node.DrawPos;
                    break;
            }
        }
        
        private void OnMouseUp(MouseEventBase<MouseUpEvent> evt)
        {
            switch (evt.button)
            {
                case 0:
                    //左键
                    _mouse0Down = false;
                    break;
            }
        }

        private void SetStatesTip()
        {
            var preconditionContainer = _statesTip.Q("preconditions");
            preconditionContainer.Clear();
            if (Node.preconditions != null)
            {
                foreach (var precondition in Node.preconditions)
                {
                    preconditionContainer.Add(new Label(precondition.ToString()));
                }
            }

            var effectContainer = _statesTip.Q("effects");
            effectContainer.Clear();
            if (Node.effects != null)
            {
                foreach (var effect in Node.effects)
                {
                    effectContainer.Add(new Label(effect.ToString()));
                }
            }

            var agentsContainer = _statesTip.Q("agents");
            agentsContainer.Clear();
            var nodeTimeLogs = Node.NodeTimesFull();
            if (nodeTimeLogs.Length > 0)
            {
                foreach (var timeLog in nodeTimeLogs)
                {
                    agentsContainer.Add(new Label(timeLog));
                }
            }
        }

        private void UpdateStatesTipPos(Vector2 mousePos)
        {
            _statesTip.style.left = mousePos.x + 8;
            _statesTip.style.top = mousePos.y + 8;
        }
    }
}