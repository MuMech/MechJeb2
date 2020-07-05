// #define DEBUG

using System;
using UnityEngine;

namespace MuMech
{
    public class Porkchop
    {
        public static void RefreshTexture(double[,] nodes, Texture2D texture)
        {
            Gradient colours = new Gradient();
            var colourKeys = new GradientColorKey[6];
            colourKeys[0].color = new Color(0.25f, 0.25f, 1.0f);
            colourKeys[0].time = 0.0f;
            colourKeys[1].color = new Color(0.5f, 0.5f, 1.0f);
            colourKeys[1].time = 0.01f;
            colourKeys[2].color = new Color(0.5f, 1.0f, 1.0f);
            colourKeys[2].time = 0.25f;
            colourKeys[3].color = new Color(0.5f, 1.0f, 0.5f);
            colourKeys[3].time = 0.5f;
            colourKeys[4].color = new Color(1.0f, 1.0f, 0.5f);
            colourKeys[4].time = 0.75f;
            colourKeys[5].color = new Color(1.0f, 0.5f, 0.5f);
            colourKeys[5].time = 1.0f;

            var alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0].alpha = 1.0f;
            alphaKeys[0].time = 0.0f;
            alphaKeys[1].alpha = 1.0f;
            alphaKeys[1].time = 1.0f;

            colours.SetKeys(colourKeys, alphaKeys);

            int width = nodes.GetLength(0);
            int height = nodes.GetLength(1);

            double DVminsqr = double.MaxValue;
            double DVmaxsqr = double.MinValue;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if ( !nodes[i,j].IsFinite() )
                        continue;

                    double DVsqr = nodes[i, j] * nodes[i, j];
                    DVminsqr = Math.Min(DVminsqr, DVsqr);
                    DVmaxsqr = Math.Max(DVmaxsqr, DVsqr);
                }
            }

            Debug.Log("[MechJeb] porkchop scanning found DVminsqr = " + DVminsqr + " DVmaxsqr = " + DVmaxsqr);

            double logDVminsqr = Math.Log(DVminsqr);
            double logDVmaxsqr = Math.Min(Math.Log(DVmaxsqr), logDVminsqr + 4);

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
            string dir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            System.IO.File.WriteAllBytes(dir + "/Porkchop.png", texture.EncodeToPNG());
#endif
        }
    }
}
