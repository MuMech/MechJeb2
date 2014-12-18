// #define DEBUG

using System;
using UnityEngine;

namespace MuMech
{
	public class Porkchop
	{
		public readonly Texture2D texture;

		public Porkchop(ManeuverParameters[,] nodes)
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

			double DVminsqr = double.MaxValue;
			double DVmaxsqr = double.MinValue;
			for (int i = 0; i < nodes.GetLength(0); i++)
			{
				for (int j = 0; j < nodes.GetLength(1); j++)
				{
					if (nodes[i, j] != null)
					{
						double DVsqr = nodes[i, j].dV.sqrMagnitude;
						if (DVsqr < DVminsqr)
						{
							DVminsqr = DVsqr;
						}

						DVmaxsqr = Math.Max(DVmaxsqr, nodes[i, j].dV.sqrMagnitude);
					}
				}
			}

			texture = new Texture2D(nodes.GetLength(0), nodes.GetLength(1), TextureFormat.RGB24, false);
			double logDVminsqr = Math.Log(DVminsqr);
			double logDVmaxsqr = Math.Min(Math.Log(DVmaxsqr), logDVminsqr + 4);


			for (int i = 0; i < nodes.GetLength(0); i++)
			{
				for (int j = 0; j < nodes.GetLength(1); j++)
				{
					if (nodes[i, j] == null)
					{
						texture.SetPixel(i, j, colours.Evaluate(1));
					}
					else
					{
						double lambda = (Math.Log(nodes[i, j].dV.sqrMagnitude) - logDVminsqr) / (logDVmaxsqr - logDVminsqr);
						texture.SetPixel(i, j, colours.Evaluate((float)lambda));
					}
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

