using UnityEngine;

namespace BetterRadio.UI
{
	internal class ColorTexture
	{
		public string Name { get; set; }
		public Color Color { get; set; }
		public Color HoverColor { get; set; }
		public Texture2D Texture { get; set; }
		public Texture2D HoverTexture { get; set; }

		public ColorTexture(string name, Color color, Color? hoverColor = null)
		{
			Name = name;
			Color = color;
			hoverColor ??= CalculateHoverColor(color);
			HoverColor = (Color)hoverColor;

			BuildColorTextures();
		}

		public ColorTexture(Color color)
		{
			Color = color;

			BuildColorTextures();
		}

		public void BuildColorTextures()
		{
			Color[] pixels = new Color[1 * 1];
			for (int i = 0; i < pixels.Length; i++)
			{
				pixels[i] = Color;
			}
			Texture = new Texture2D(1, 1);
			Texture.SetPixels(pixels);
			Texture.Apply();

			if (HoverColor != null)
			{
				Color[] hoverPixels = new Color[1 * 1];
				for (int i = 0; i < hoverPixels.Length; i++)
				{
					hoverPixels[i] = HoverColor;
				}
				HoverTexture = new Texture2D(1, 1);
				HoverTexture.SetPixels(hoverPixels);
				HoverTexture.Apply();
			}
		}

		/// <summary>
		/// Calculates a hover variant of a colour by shifting its HSV value (brightness).
		/// Darkens bright colours; lightens colours that are already too dark to darken further.
		/// </summary>
		/// <param name="color">Base colour</param>
		/// <param name="amount">How much to shift the value component, 0-1</param>
		private static Color CalculateHoverColor(Color color, float amount = 0.15f)
		{
			Color.RGBToHSV(color, out float h, out float s, out float v);

			// If there's not enough headroom to darken, lighten instead.
			v = v <= amount ? v + amount : v - amount;
			v = Mathf.Clamp01(v);

			Color result = Color.HSVToRGB(h, s, v);
			// Preserve original alpha.
			result.a = color.a;
			return result;
		}
	}
}
