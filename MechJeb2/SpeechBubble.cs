using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class SpeechBubble
    {
        public Material mat;
        public GUIStyle txtStyle;
        public float bubbleWidth = 200;
        public float bubbleHeight = 50;
        public float offsetX = 0;
        public float offsetY = 50;
        public int points = 100;

        public SpeechBubble(GUIStyle style)
        {
            mat = new Material("Shader \"Lines/Colored Blended\" {" +
                "SubShader { Pass { " +
                "    Blend SrcAlpha OneMinusSrcAlpha " +
                "    ZWrite Off Cull Off Fog { Mode Off } " +
                "    BindChannels {" +
                "      Bind \"vertex\", vertex Bind \"color\", color }" +
                "} } }");
            mat.hideFlags = HideFlags.HideAndDontSave;
            mat.shader.hideFlags = HideFlags.HideAndDontSave;
            txtStyle = style;
        }

        public void drawBubble(Vector2 screenPos, string text, Color bgColor)
        {
            Vector3 viewportPos = new Vector3(screenPos.x / (float)Screen.width, (Screen.height - screenPos.y) / (float)Screen.height);
            float centerOffsetX = bubbleWidth / 2;
            float centerOffsetY = bubbleHeight / 2;

            GL.PushMatrix();

            mat.SetPass(0);
            GL.LoadOrtho();
            GL.Begin(GL.TRIANGLES);
            GL.Color(bgColor);
            GL.Vertex3(viewportPos.x, viewportPos.y, 0.1f);
            GL.Vertex3(viewportPos.x - (bubbleWidth / 3) / (float)Screen.width, viewportPos.y + offsetY / Screen.height, 0.1f);
            GL.Vertex3(viewportPos.x - (bubbleWidth / 8) / (float)Screen.width, viewportPos.y + offsetY / Screen.height, 0.1f);
            GL.End();

            GL.Begin(GL.TRIANGLES);
            GL.Color(bgColor);
            float dA = Mathf.PI * 2 / points;
            float cX = (screenPos.x - offsetX) / (float)Screen.width;
            float cY = (Screen.height - (screenPos.y - offsetY)) / (float)Screen.height;
            float rX = centerOffsetX / (float)Screen.width;
            float rY = centerOffsetY / (float)Screen.height;
            for (int i = 0; i < points; i++)
            {
                GL.Vertex3(cX, cY, 0.1f);
                GL.Vertex3(cX + rX * Mathf.Cos(dA * i), cY + rY * Mathf.Sin(dA * i), 0.1f);
                GL.Vertex3(cX + rX * Mathf.Cos(dA * (i + 1)), cY + rY * Mathf.Sin(dA * (i + 1)), 0.1f);
            }
            GL.End();

            GL.PopMatrix();

            GUI.Label(new Rect(screenPos.x - centerOffsetX - offsetX, screenPos.y - centerOffsetY - offsetY, bubbleWidth, bubbleHeight), text, txtStyle);
        }
    }
}
