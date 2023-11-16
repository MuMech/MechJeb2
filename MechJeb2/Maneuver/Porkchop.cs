// #define DEBUG

extern alias JetBrainsAnnotations;
using System;
using System.IO;
using System.Reflection;
using JetBrainsAnnotations::JetBrains.Annotations;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class Porkchop
    {
        public static void RefreshTexture(double[,] nodes, Texture2D texture)
        {
            var colours = new Gradient();
            var colourKeys = new GradientColorKey[6];
            colourKeys[0].color = new Color(0.25f, 0.25f, 1.0f);
            colourKeys[0].time  = 0.0f;
            colourKeys[1].color = new Color(0.5f, 0.5f, 1.0f);
            colourKeys[1].time  = 0.01f;
            colourKeys[2].color = new Color(0.5f, 1.0f, 1.0f);
            colourKeys[2].time  = 0.25f;
            colourKeys[3].color = new Color(0.5f, 1.0f, 0.5f);
            colourKeys[3].time  = 0.5f;
            colourKeys[4].color = new Color(1.0f, 1.0f, 0.5f);
            colourKeys[4].time  = 0.75f;
            colourKeys[5].color = new Color(1.0f, 0.5f, 0.5f);
            colourKeys[5].time  = 1.0f;

            var alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].alpha = 1.0f;
            alphaKeys[0].time  = 0.0f;
            alphaKeys[1].alpha = 1.0f;
            alphaKeys[1].time  = 1.0f;

            colours.SetKeys(colourKeys, alphaKeys);

            int width = nodes.GetLength(0);
            int height = nodes.GetLength(1);

            double dVminsqr = double.MaxValue;
            double dVmaxsqr = double.MinValue;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (!nodes[i, j].IsFinite())
                        continue;

                    double dVsqr = nodes[i, j] * nodes[i, j];
                    dVminsqr = Math.Min(dVminsqr, dVsqr);
                    dVmaxsqr = Math.Max(dVmaxsqr, dVsqr);
                }
            }

            Debug.Log("[MechJeb] porkchop scanning found DVminsqr = " + dVminsqr + " DVmaxsqr = " + dVmaxsqr);

            double logDVminsqr = Math.Log(dVminsqr);
            double logDVmaxsqr = Math.Min(Math.Log(dVmaxsqr), logDVminsqr + 4);

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    double lambda = (Math.Log(nodes[i, j] * nodes[i, j]) - logDVminsqr) / (logDVmaxsqr - logDVminsqr);
                    texture.SetPixel(i, j, colours.Evaluate((float)lambda));
                }
            }

            texture.Apply();

#if DEBUG
            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            File.WriteAllBytes(dir + "/Porkchop.png", texture.EncodeToPNG());
#endif
        }
    }
}
