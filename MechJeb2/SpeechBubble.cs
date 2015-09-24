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
        public const int points = 100;

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
            Vector3 viewportPos = new Vector3(screenPos.x / (float)GuiUtils.scaledScreenWidth, (GuiUtils.scaledScreenHeight - screenPos.y) / (float)GuiUtils.scaledScreenHeight);
            float centerOffsetX = bubbleWidth / 2;
            float centerOffsetY = bubbleHeight / 2;

            GL.PushMatrix();

            mat.SetPass(0);
            GL.LoadOrtho();
            GL.Begin(GL.TRIANGLES);
            GL.Color(bgColor);
            GL.Vertex3(viewportPos.x, viewportPos.y, 0.1f);
            GL.Vertex3(viewportPos.x - (bubbleWidth / 3) / (float)GuiUtils.scaledScreenWidth, viewportPos.y + offsetY / GuiUtils.scaledScreenHeight, 0.1f);
            GL.Vertex3(viewportPos.x - (bubbleWidth / 8) / (float)GuiUtils.scaledScreenWidth, viewportPos.y + offsetY / GuiUtils.scaledScreenHeight, 0.1f);
            GL.End();

            GL.Begin(GL.TRIANGLES);
            GL.Color(bgColor);
            float dA = Mathf.PI * 2 / points;
            float cX = (screenPos.x - offsetX) / (float)GuiUtils.scaledScreenWidth;
            float cY = (GuiUtils.scaledScreenHeight - (screenPos.y - offsetY)) / (float)GuiUtils.scaledScreenHeight;
            float rX = centerOffsetX / (float)GuiUtils.scaledScreenWidth;
            float rY = centerOffsetY / (float)GuiUtils.scaledScreenHeight;
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
