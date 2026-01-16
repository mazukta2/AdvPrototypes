using UnityEngine;
using UnityEngine.UIElements;

namespace Postica.Common
{
    class ConnectingLine : VisualElement
    {
        public enum Position
        {
            Min,
            Center,
            Max
        }

        private VisualElement from;
        private VisualElement to;

        private (Position horizontal, Position vertical) fromPosition;
        private (Position horizontal, Position vertical) toPosition;

        private Vector2 fromOffset;
        private Vector2 toOffset;

        private VisualElement fromPoint;
        private VisualElement toPoint;

        private bool isRefreshing;
        
        public VisualElement From
        {
            get => from;
            set
            {
                if (from == value)
                {
                    return;
                }
                from = value;
                Refresh();
            }
        }

        public VisualElement To
        {
            get => to;
            set
            {
                if (to == value)
                {
                    return;
                }
                to = value;
                Refresh();
            }
        }

        // default constructor
        public ConnectingLine()
        {
            this.AddPosticaStyles();
            AddToClassList("connecting-line");
            fromPoint = new VisualElement().WithClass("connecting-line__point", "connecting-line__point--from");
            toPoint = new VisualElement().WithClass("connecting-line__point", "connecting-line__point--to");

            Add(fromPoint);
            Add(toPoint);

            RegisterCallback<GeometryChangedEvent>(evt => Refresh(showWarnings: false), TrickleDown.TrickleDown);
            fromPosition = (Position.Min, Position.Max);
            toPosition = (Position.Min, Position.Center);

            pickingMode = PickingMode.Ignore;
        }

        public ConnectingLine(VisualElement from, VisualElement to) : this()
        {
            From = from;
            To = to;
        }

        public void SetFrom(VisualElement from, Position horizontalPosition, Position verticalPosition, Vector2 offset = default)
        {
            fromPosition = (horizontalPosition, verticalPosition);
            fromOffset = offset;
            From = from;
        }

        public void SetTo(VisualElement to, Position horizontalPosition, Position verticalPosition, Vector2 offset = default)
        {
            toPosition = (horizontalPosition, verticalPosition);
            toOffset = offset;
            To = to;
        }

        public void Refresh(bool showWarnings = true)
        {
            if(panel == null)
            {
                return;
            }

            if(resolvedStyle.display == DisplayStyle.None)
            {
                return;
            }

            if(from == null)
            {
                // Log a warning message and exit
                if (showWarnings)
                {
                    Debug.LogWarning(GetType().Name + ": Cannot connect because From is null");
                }
                return;
            }

            if(to == null)
            {
                if (showWarnings)
                {
                    Debug.LogWarning(GetType().Name + ": Cannot connect because To is null");
                }
                return;
            }

            if (isRefreshing)
            {
                return;
            }

            isRefreshing = true;

            var fromPos = GetPositionVector2(from.worldBound, fromPosition);
            var toPos = GetPositionVector2(to.worldBound, toPosition);
            var fromLocalPos = parent.WorldToLocal(fromPos + fromOffset);
            var toLocalPos = parent.WorldToLocal(toPos + toOffset);

            // Set position as absolute
            style.position = UnityEngine.UIElements.Position.Absolute;

            // Set the styles
            if(fromLocalPos.x <  toLocalPos.x) 
            {
                style.left = fromLocalPos.x;
                style.width = toLocalPos.x - fromLocalPos.x;
            } 
            else 
            {
                style.left = toLocalPos.x;
                style.width = fromLocalPos.x - toLocalPos.x;
            }
            if (fromLocalPos.y < toLocalPos.y)
            {
                style.top = fromLocalPos.y;
                style.height = toLocalPos.y - fromLocalPos.y;
            }
            else
            {
                style.top = toLocalPos.y;
                style.height = fromLocalPos.y - toLocalPos.y;
            }

            isRefreshing = false;
        }

        private static Vector2 GetPositionVector2(Rect layout, (Position horizonta, Position vertical) position)
        {
            return position switch
            {
                (Position.Min, Position.Min) => layout.min,
                (Position.Min, Position.Center) => new Vector2(layout.min.x, layout.center.y),
                (Position.Min, Position.Max) => new Vector2(layout.min.x, layout.max.y),
                (Position.Center, Position.Min) => new Vector2(layout.center.x, layout.min.y),
                (Position.Center, Position.Center) => layout.center,
                (Position.Center, Position.Max) => new Vector2(layout.center.x, layout.max.y),
                (Position.Max, Position.Min) => new Vector2(layout.max.x, layout.min.y),
                (Position.Max, Position.Center) => new Vector2(layout.max.x, layout.center.y),
                (Position.Max, Position.Max) => layout.max,
                _ => layout.center,
            };
        }
    }
}