using UnityEngine;
using UnityEngine.UIElements;
using Zephyr.GOAP.Logger;

namespace Zephyr.GOAP.Editor
{
    public class NodeFrame : VisualElement, IManipulator
    {
        public NodeView Node;

        private VisualElement _statesTip;

        public NodeFrame(NodeView node, Vector2 drawPos, Vector2 size, VisualElement statesTip)
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
        }
        
        private void OnMouseLeave(MouseEventBase<MouseLeaveEvent> evt)
        {
            UpdateStatesTipPos(new Vector2(0, -100));
        }

        private void SetStatesTip()
        {
            var preconditionContainer = _statesTip.Q("preconditions");
            preconditionContainer.Clear();
            if (Node.Preconditions != null)
            {
                foreach (var precondition in Node.Preconditions)
                {
                    preconditionContainer.Add(new Label(precondition.ToString()));
                }
            }

            var effectContainer = _statesTip.Q("effects");
            effectContainer.Clear();
            if (Node.Effects != null)
            {
                foreach (var effect in Node.Effects)
                {
                    effectContainer.Add(new Label(effect.ToString()));
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