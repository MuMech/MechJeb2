using System;
using UnityEngine;

namespace MuMech
{
    public class PlotArea
    {
        public delegate void AreaChanged(double minx, double maxx, double miny, double maxy);

        static GUIStyle selectionStyle;

        public bool draggable = true;

        public int[] hoveredPoint;
        public int[] selectedPoint;

        private bool mouseDown;

        private double minx;
        private double maxx;
        private double miny;
        private double maxy;

        private Texture2D texture;

        private AreaChanged callback;

        public PlotArea (double minx, double maxx, double miny, double maxy, Texture2D texture, AreaChanged callback)
        {
            this.minx = minx;
            this.maxx = maxx;
            this.miny = miny;
            this.maxy = maxy;
            this.texture = texture;
            this.callback = callback;
        }

        static PlotArea()
        {
            selectionStyle = new GUIStyle();
            Texture2D background = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            background.SetPixel(0,0, new Color(0, 0, 1, 0.3f));
            background.Apply();
            selectionStyle.normal.background = background;
        }

        public double x(int index)
        {
            return minx + index * (maxx - minx) / texture.width;
        }
        public double y(int index)
        {
            return miny + index * (maxy - miny) / texture.height;
        }

        public int[] lastHoveredPoint = null;

        private void zoomSelectionBox(int[] hoveredPoint)
        {
            mouseDown = false;
            if (Math.Abs(selectedPoint[0] - hoveredPoint[0]) > 5 &&
                    Math.Abs(selectedPoint[1] - hoveredPoint[1]) > 5)
            {
                Debug.Log("[MechJeb] porkchop plotter, zooming plotarea");
                callback(
                        x(Math.Min(selectedPoint[0], hoveredPoint[0])),
                        x(Math.Max(selectedPoint[0], hoveredPoint[0])),
                        y(Math.Min(selectedPoint[1], hoveredPoint[1])),
                        y(Math.Max(selectedPoint[1], hoveredPoint[1]))
                        );
            }
        }

        public void DoGUI()
        {
            GUILayout.Box(texture, GUIStyle.none, new GUILayoutOption[] { GUILayout.Width(texture.width), GUILayout.Height(texture.height)});
            if (Event.current.type == EventType.Repaint)
            {
                hoveredPoint = null;
                var rect = GUILayoutUtility.GetLastRect(); rect.x +=1; rect.y += 2;
                Vector2 mouse = Event.current.mousePosition;
                if (rect.Contains(mouse))
                {
                    var pos = (mouse - rect.position);
                    hoveredPoint = new int[]{(int) pos.x, (int)(rect.height - pos.y - 1)};
                }
                else
                {
                    if (mouseDown && lastHoveredPoint != null)
                    {
                        // zoom the selection box if we leave the area when mouse is down
                        zoomSelectionBox(lastHoveredPoint);
                    }
                }
                if (mouseDown)
                {
                    GUI.Box(new Rect(rect.x + Math.Min(selectedPoint[0], hoveredPoint[0]),
                                rect.y + rect.height - Math.Max(selectedPoint[1], hoveredPoint[1]),
                                Math.Abs(selectedPoint[0] - hoveredPoint[0]),
                                Math.Abs(selectedPoint[1] - hoveredPoint[1])), "", selectionStyle);
                }
            }

            draggable = hoveredPoint == null;
            if (hoveredPoint != null)
            {
                switch (Event.current.type)
                {
                    case EventType.MouseDown:
                        if (Event.current.button == 0)
                        {
                            mouseDown = true;
                            selectedPoint = hoveredPoint;
                        }
                        break;
                    case EventType.MouseUp:
                        if (Event.current.button == 0 && mouseDown)
                        {
                            // zoom the selection box on mouseup
                            zoomSelectionBox(hoveredPoint);
                        }
                        break;
                    case EventType.ScrollWheel:
                        if (Event.current.delta.y == 0)
                            break;
                        double lambda = Event.current.delta.y < 0 ? 0.7 : 1/0.7;
                        double deltax = maxx - minx;
                        double deltay = maxy - miny;

                        double newminx = x(hoveredPoint[0]) - hoveredPoint[0] * lambda * deltax / texture.width;
                        double newminy = y(hoveredPoint[1]) - hoveredPoint[1] * lambda * deltay / texture.height;
                        callback(
                                newminx,
                                newminx + lambda * deltax,
                                newminy,
                                newminy + lambda * deltay
                                );

                        break;
                    case EventType.KeyUp:
                        ProcessKeys();
                        break;
                }
            }
            lastHoveredPoint = hoveredPoint;
        }

        private void ProcessKeys()
        {
            KeyCode code = Event.current.keyCode;
            int dirX = 0, dirY = 0;

            if (code == KeyCode.W || code == KeyCode.S ||
                    code == KeyCode.UpArrow || code == KeyCode.DownArrow)
            {
                dirY = code == KeyCode.W || code == KeyCode.UpArrow ? 1 : -1;
            }
            else if (code == KeyCode.A || code == KeyCode.D ||
                    code == KeyCode.LeftArrow || code == KeyCode.RightArrow)
            {
                dirX = code == KeyCode.D || code == KeyCode.RightArrow ? 1 : -1;
            }

            if (dirX != 0 || dirY != 0)
            {
                double dx = maxx - minx;
                double dy = maxy - miny;

                double moveDx = dx * 0.25 * dirX;
                double moveDy = dy * 0.25 * dirY;

                double newMinX = minx + moveDx;
                double newMinY = miny + moveDy;

                callback(
                        newMinX,
                        newMinX + dx,
                        newMinY,
                        newMinY + dy
                        );
            }
        }
    }
}
