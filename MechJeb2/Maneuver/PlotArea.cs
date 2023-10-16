using System;
using UnityEngine;

namespace MuMech
{
    public class PlotArea
    {
        public delegate void AreaChanged(double minx, double maxx, double miny, double maxy);

        private static readonly GUIStyle _selectionStyle;

        public bool Draggable = true;

        public int[] HoveredPoint;
        public int[] SelectedPoint;

        private bool _mouseDown;

        private readonly double _minx;
        private readonly double _maxx;
        private readonly double _miny;
        private readonly double _maxy;

        private readonly Texture2D _texture;

        private readonly AreaChanged _callback;

        public PlotArea(double minx, double maxx, double miny, double maxy, Texture2D texture, AreaChanged callback)
        {
            _minx     = minx;
            _maxx     = maxx;
            _miny     = miny;
            _maxy     = maxy;
            _texture  = texture;
            _callback = callback;
        }

        static PlotArea()
        {
            _selectionStyle = new GUIStyle();
            var background = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            background.SetPixel(0, 0, new Color(0, 0, 1, 0.3f));
            background.Apply();
            _selectionStyle.normal.background = background;
        }

        private double X(int index) => _minx + index * (_maxx - _minx) / _texture.width;

        private double Y(int index) => _miny + index * (_maxy - _miny) / _texture.height;

        private int[] _lastHoveredPoint;

        private void ZoomSelectionBox(int[] hoveredPoint)
        {
            _mouseDown = false;
            if (Math.Abs(SelectedPoint[0] - hoveredPoint[0]) > 5 &&
                Math.Abs(SelectedPoint[1] - hoveredPoint[1]) > 5)
            {
                Debug.Log("[MechJeb] porkchop plotter, zooming plotarea");
                _callback(
                    X(Math.Min(SelectedPoint[0], hoveredPoint[0])),
                    X(Math.Max(SelectedPoint[0], hoveredPoint[0])),
                    Y(Math.Min(SelectedPoint[1], hoveredPoint[1])),
                    Y(Math.Max(SelectedPoint[1], hoveredPoint[1]))
                );
            }
        }

        public void DoGUI()
        {
            GUILayout.Box(_texture, GUIStyle.none, GUILayout.Width(_texture.width), GUILayout.Height(_texture.height));
            if (Event.current.type == EventType.Repaint)
            {
                HoveredPoint = null;
                Rect rect = GUILayoutUtility.GetLastRect();
                rect.x += 1;
                rect.y += 2;
                Vector2 mouse = Event.current.mousePosition;
                if (rect.Contains(mouse))
                {
                    Vector2 pos = mouse - rect.position;
                    HoveredPoint = new[] { (int)pos.x, (int)(rect.height - pos.y - 1) };
                }
                else
                {
                    if (_mouseDown && _lastHoveredPoint != null)
                    {
                        // zoom the selection box if we leave the area when mouse is down
                        ZoomSelectionBox(_lastHoveredPoint);
                    }
                }

                if (_mouseDown)
                {
                    GUI.Box(new Rect(rect.x + Math.Min(SelectedPoint[0], HoveredPoint[0]),
                        rect.y + rect.height - Math.Max(SelectedPoint[1], HoveredPoint[1]),
                        Math.Abs(SelectedPoint[0] - HoveredPoint[0]),
                        Math.Abs(SelectedPoint[1] - HoveredPoint[1])), "", _selectionStyle);
                }
            }

            Draggable = HoveredPoint == null;
            if (HoveredPoint != null)
            {
                switch (Event.current.type)
                {
                    case EventType.MouseDown:
                        if (Event.current.button == 0)
                        {
                            _mouseDown    = true;
                            SelectedPoint = HoveredPoint;
                        }

                        break;
                    case EventType.MouseUp:
                        if (Event.current.button == 0 && _mouseDown)
                        {
                            // zoom the selection box on mouseup
                            ZoomSelectionBox(HoveredPoint);
                        }

                        break;
                    case EventType.ScrollWheel:
                        if (Event.current.delta.y == 0)
                            break;
                        double lambda = Event.current.delta.y < 0 ? 0.7 : 1 / 0.7;
                        double deltax = _maxx - _minx;
                        double deltay = _maxy - _miny;

                        double newminx = X(HoveredPoint[0]) - HoveredPoint[0] * lambda * deltax / _texture.width;
                        double newminy = Y(HoveredPoint[1]) - HoveredPoint[1] * lambda * deltay / _texture.height;
                        _callback(
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

            _lastHoveredPoint = HoveredPoint;
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
                double dx = _maxx - _minx;
                double dy = _maxy - _miny;

                double moveDx = dx * 0.25 * dirX;
                double moveDy = dy * 0.25 * dirY;

                double newMinX = _minx + moveDx;
                double newMinY = _miny + moveDy;

                _callback(
                    newMinX,
                    newMinX + dx,
                    newMinY,
                    newMinY + dy
                );
            }
        }
    }
}
