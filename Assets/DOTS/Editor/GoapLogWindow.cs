using UnityEditor;
using UnityEngine;

namespace DOTS.Editor
{
    public partial class GoapLogWindow : EditorWindow
    {
        private static Rect canvasRect;
        private static Rect viewRect;
        
        const float TOP_MARGIN = 22;
        const float BOTTOM_MARGIN = 5;
        const int GRID_SIZE = 16;

        private static GoapGraph currentGraph;
        
        [MenuItem("Zephyr/Goap/Logger")]
        private static void OpenWindow()
        {
            GetWindow<GoapLogWindow>().Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Goap Logs", StyleSheet.CanvasIcon);
            currentGraph = new GoapGraph();
        }

        private void OnGUI()
        {
            //initialize rects
            canvasRect = Rect.MinMaxRect(5, TOP_MARGIN, position.width - 5, position.height - BOTTOM_MARGIN);
            var aspect = canvasRect.width / canvasRect.height;
            
            //canvas background
            GUI.Box(canvasRect, string.Empty, StyleSheet.canvasBG);
            //background grid
            DrawGrid(canvasRect, pan);
            
            // calc viewRect
            {
                viewRect = canvasRect;
                viewRect.x = 0;
                viewRect.y = 0;
                viewRect.position -= pan;
            }

            ShowToolbar(currentGraph);
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
        
        //The translation of the graph
        private static Vector2 pan {
            get => currentGraph?.translation ?? viewCanvasCenter;
            set
            {
                if (currentGraph == null) return;
                var t = value;
                currentGraph.translation = t;
            }
        }
        
        private static Vector2 viewCanvasCenter => viewRect.size / 2;
    }
}
