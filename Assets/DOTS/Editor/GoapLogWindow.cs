using UnityEditor;
using UnityEngine;

namespace DOTS.Editor
{
    public partial class GoapLogWindow : EditorWindow
    {
        private StyleSheet _styleSheet;
        
        private static Rect canvasRect;
        private static Rect viewRect;
        
        const float TOP_MARGIN = 22;
        const float BOTTOM_MARGIN = 5;
        const int GRID_SIZE = 16;

        private static GoapGraph _currentGraph;
        
        [MenuItem("Zephyr/Goap/Logger")]
        private static void OpenWindow()
        {
            GetWindow<GoapLogWindow>().Show();
        }

        private void OnEnable()
        {
            _styleSheet = Resources.Load<StyleSheet>("StyleSheet");
            titleContent = new GUIContent("Goap Logs", _styleSheet.icons.canvasIcon);
            _currentGraph = new GoapGraph();
        }

        private void OnGUI()
        {
            //initialize rects
            canvasRect = Rect.MinMaxRect(5, TOP_MARGIN, position.width - 5, position.height - BOTTOM_MARGIN);
            var aspect = canvasRect.width / canvasRect.height;
            
            //canvas background
            GUI.Box(canvasRect, string.Empty, _styleSheet.styles.canvasBG);
            //background grid
            DrawGrid(canvasRect, pan);
            
            // calc viewRect
            {
                viewRect = canvasRect;
                viewRect.x = 0;
                viewRect.y = 0;
                viewRect.position -= pan;
            }
            
            GUI.BeginClip(canvasRect, pan, default, false);
            {
                DrawNodes();
            }
            GUI.EndClip();

            if (!_currentGraph.IsEmpty())
            {
                _currentGraph.DrawInfo();
            }

            ShowToolbar(_currentGraph);
        }
        
        //Draw a simple grid
        static void DrawGrid(Rect container, Vector2 offset) {

            if ( Event.current.type != EventType.Repaint ) {
                return;
            }

            Handles.color = new Color(0, 0, 0, 0.15f);
            
            var step = GRID_SIZE;

            var xDiff = offset.x % step;
            var xStart = container.xMin + xDiff;
            var xEnd = container.xMax;
            for ( var i = xStart; i < xEnd; i += step ) {
                Handles.DrawLine(new Vector3(i, container.yMin, 0), new Vector3(i, container.yMax, 0));
            }

            var yDiff = offset.y % step;
            var yStart = container.yMin + yDiff;
            var yEnd = container.yMax;
            for ( var i = yStart; i < yEnd; i += step ) {
                Handles.DrawLine(new Vector3(0, i, 0), new Vector3(container.xMax, i, 0));
            }

            Handles.color = Color.white;
        }

        private void DrawNodes()
        {
            if (_currentGraph.IsEmpty()) return;

            _currentGraph.DrawNodes(_styleSheet);
        } 
        
        //The translation of the graph
        private static Vector2 pan {
            get => _currentGraph?.translation ?? viewCanvasCenter;
            set
            {
                if (_currentGraph == null) return;
                var t = value;
                _currentGraph.translation = t;
            }
        }
        
        private static Vector2 viewCanvasCenter => viewRect.size / 2;
    }
}
