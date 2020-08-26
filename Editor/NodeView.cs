using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Zephyr.GOAP.Logger;

namespace Zephyr.GOAP.Editor
{
    public class NodeView : VisualElement, IManipulator
    {
        public NodeLog Node;

        private VisualElement _statesTip;

        private bool _mouse0Down, _mouse1Down;

        private double _mouse0UpTime, _mouse1UpTime;

        private Vector2 _mouseDragStartPos, _frameDragStartPos;

        private GoapLogWindow _window;

        public NodeView(GoapLogWindow window, NodeLog node, Vector2 drawPos, Vector2 size, VisualElement statesTip)
        {
            _window = window;
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
                MoveTo(_frameDragStartPos + distance);
            }
        }

        public void MoveTo(Vector2 newPos)
        {
            Node.DrawPos = newPos;
            style.left = newPos.x;
            style.top = newPos.y;
        }
        
        private void OnMouseLeave(MouseEventBase<MouseLeaveEvent> evt)
        {
            UpdateStatesTipPos(new Vector2(0, -9999));
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
                case 1:
                    _mouse1Down = true;
                    break;
            }
        }
        
        private void OnMouseUp(MouseEventBase<MouseUpEvent> evt)
        {
            double time = EditorApplication.timeSinceStartup;;
            switch (evt.button)
            {
                case 0:
                    //左键
                    _mouse0Down = false;
                    if (time - _mouse0UpTime <= Utils.DoubleClickThreshold)
                    {
                        //双击阈值内二次按下
                        MoveCloseParentNodes();
                    }
                    else
                    {
                        _mouse0UpTime = time;
                    }
                    break;
                case 1:
                    _mouse1Down = false;
                    if (time - _mouse1UpTime <= Utils.DoubleClickThreshold)
                    {
                        //双击阈值内二次按下
                        MoveCloseChildNodes();
                    }
                    else
                    {
                        _mouse1UpTime = time;
                    }
                    break;
            }
        }

        /// <summary>
        /// 通知上级把我的Parents都移动到我左侧近处
        /// </summary>
        private void MoveCloseParentNodes()
        {
            _window.MoveCloseRelativeNodes(this, true);
        }

        private void MoveCloseChildNodes()
        {
            _window.MoveCloseRelativeNodes(this, false);
        }

        private void SetStatesTip()
        {
            var title = _statesTip.Q<Label>("title-preconditions");
            title.text = $"Precondition  [{Node.hashCode}]";
            var preconditionContainer = _statesTip.Q("preconditions");
            preconditionContainer.Clear();
            if (Node.preconditions != null)
            {
                foreach (var precondition in Node.preconditions)
                {
                    preconditionContainer.Add(CreateNewLabel(precondition.ToString()));
                }
            }

            var effectContainer = _statesTip.Q("effects");
            effectContainer.Clear();
            if (Node.effects != null)
            {
                foreach (var effect in Node.effects)
                {
                    effectContainer.Add(CreateNewLabel(effect.ToString()));
                }
            }
            
            var deltaContainer = _statesTip.Q("deltas");
            deltaContainer.Clear();
            if (Node.deltas != null)
            {
                foreach (var delta in Node.deltas)
                {
                    deltaContainer.Add(CreateNewLabel(delta.ToString()));
                }
            }

            var agentsContainer = _statesTip.Q("agents");
            agentsContainer.Clear();
            var nodeTimeLogs = Node.NodeAgentInfos();
            if (nodeTimeLogs.Length > 0)
            {
                foreach (var timeLog in nodeTimeLogs)
                {
                    agentsContainer.Add(CreateNewLabel(timeLog));
                }
            }
        }

        private void UpdateStatesTipPos(Vector2 mousePos)
        {
            _statesTip.style.left = mousePos.x + 8;
            _statesTip.style.top = mousePos.y + 8;
        }

        private Label CreateNewLabel(string text)
        {
            var lable = new Label(text);
            lable.style.color = Color.black;
            return lable;
        }
    }
}