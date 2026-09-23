using System.Collections.Generic;
using TLDLoader.Utilities.UI;
using UnityEngine;

namespace BetterRadio.UI
{
	internal static class Styling
	{
		public static GUISkin GetSkin() => _skin;

		private static bool _hasInitialised = false;
		private static GUISkin _skin;

		private static Texture2D _black;
		private static Texture2D _blackHover;
		private static Texture2D _white;
		private static Texture2D _whiteHover;
		private static Texture2D _transparent;
		private static Texture2D _orange;
		private static Texture2D _orangeHover;
		private static Texture2D _red;
		private static Texture2D _redHover;
		private static Texture2D _blue;
		private static Texture2D _blueHover;

		private static Texture2D _buttonPrimary;
		private static Texture2D _buttonPrimaryHover;
		private static Texture2D _buttonSecondary;
		private static Texture2D _buttonSecondaryHover;
		private static Texture2D _box;
		private static Texture2D _boxHover;
		private static Texture2D _boxDark;

		private static List<ColorTexture> _colours = new List<ColorTexture>
		{
			// Neutrals.
			new ColorTexture("Black", new Color(0f, 0f, 0f)),
			new ColorTexture("White", new Color(1f, 1f, 1f)),
			new ColorTexture("LightGrey", new Color(0.7f, 0.7f, 0.7f)),
			new ColorTexture("Grey", new Color(0.4f, 0.4f, 0.4f)),
			new ColorTexture("DarkGrey", new Color(0.15f, 0.15f, 0.15f)),

			// Reds.
			new ColorTexture("LightRed", new Color(1f, 0.4f, 0.38f)),
			new ColorTexture("Red", new Color(0.91f, 0.14f, 0.12f)),
			new ColorTexture("DarkRed", new Color(0.55f, 0.1f, 0.1f)),
			new ColorTexture("Crimson", new Color(0.76f, 0.08f, 0.24f)),
			new ColorTexture("Maroon", new Color(0.5f, 0.09f, 0.09f)),

			// Oranges.
			new ColorTexture("LightOrange", new Color(1f, 0.72f, 0.35f)),
			new ColorTexture("Orange", new Color(0.91f, 0.55f, 0.12f)),
			new ColorTexture("DarkOrange", new Color(0.7f, 0.4f, 0.05f)),
			new ColorTexture("Brown", new Color(0.4f, 0.26f, 0.13f)),

			// Yellows.
			new ColorTexture("LightYellow", new Color(1f, 0.96f, 0.55f)),
			new ColorTexture("Yellow", new Color(0.9f, 0.82f, 0.15f)),
			new ColorTexture("DarkYellow", new Color(0.65f, 0.58f, 0.05f)),
			new ColorTexture("Gold", new Color(0.78f, 0.62f, 0.1f)),

			// Greens.
			new ColorTexture("LightGreen", new Color(0.55f, 0.9f, 0.4f)),
			new ColorTexture("Green", new Color(0f, 0.69f, 0.008f)),
			new ColorTexture("DarkGreen", new Color(0f, 0.4f, 0.01f)),
			new ColorTexture("Olive", new Color(0.42f, 0.48f, 0.12f)),
			new ColorTexture("Lime", new Color(0.6f, 0.87f, 0.13f)),
			new ColorTexture("Emerald", new Color(0.06f, 0.47f, 0.29f)),

			// Cyans / Teals.
			new ColorTexture("LightCyan", new Color(0.6f, 0.95f, 0.97f)),
			new ColorTexture("Cyan", new Color(0.1f, 0.8f, 0.85f)),
			new ColorTexture("DarkCyan", new Color(0.05f, 0.5f, 0.55f)),
			new ColorTexture("Teal", new Color(0.06f, 0.5f, 0.44f)),

			// Blues.
			new ColorTexture("LightBlue", new Color(0.55f, 0.75f, 1f)),
			new ColorTexture("Blue", new Color(0.161f, 0.463f, 0.859f)),
			new ColorTexture("DarkBlue", new Color(0.08f, 0.2f, 0.5f)),
			new ColorTexture("SkyBlue", new Color(0.27f, 0.65f, 0.87f)),
			new ColorTexture("Navy", new Color(0.06f, 0.13f, 0.35f)),

			// Purples.
			new ColorTexture("LightPurple", new Color(0.78f, 0.6f, 0.92f)),
			new ColorTexture("Purple", new Color(0.5f, 0.15f, 0.65f)),
			new ColorTexture("DarkPurple", new Color(0.3f, 0.08f, 0.4f)),
			new ColorTexture("Violet", new Color(0.6f, 0.4f, 0.85f)),
			new ColorTexture("Indigo", new Color(0.29f, 0.1f, 0.55f)),

			// Pinks / Magentas.
			new ColorTexture("LightPink", new Color(1f, 0.78f, 0.87f)),
			new ColorTexture("Pink", new Color(0.9f, 0.5f, 0.65f)),
			new ColorTexture("DarkPink", new Color(0.7f, 0.25f, 0.4f)),
			new ColorTexture("HotPink", new Color(0.95f, 0.2f, 0.55f)),
			new ColorTexture("Magenta", new Color(0.85f, 0.1f, 0.6f)),
			new ColorTexture("Rose", new Color(0.85f, 0.35f, 0.45f)),
		};

		public static void Bootstrap()
		{
			if (!_hasInitialised)
			{
				CreateSkin(GUI.skin);
				List<GUIStyle> styles = new List<GUIStyle>();

				// Create any required core textures.
				_black = Common.ColorTexture(1, 1, new Color(0f, 0f, 0f));
				_blackHover = Common.ColorTexture(1, 1, new Color(0.1f, 0.1f, 0.1f));
				_white = Common.ColorTexture(1, 1, new Color(1f, 1f, 1f));
				_whiteHover = Common.ColorTexture(1, 1, new Color(0.9f, 0.9f, 0.9f));
				_transparent = Common.ColorTexture(1, 1, new Color(0, 0, 0, 0));
				_orange = Common.ColorTexture(1, 1, new Color(0.91f, 0.55f, 0.12f));
				_orangeHover = Common.ColorTexture(1, 1, new Color(0.91f, 0.55f, 0.12f, 0.7f));
				_red = Common.ColorTexture(1, 1, new Color(0.91f, 0.14f, 0.12f, 0.6f));
				_redHover = Common.ColorTexture(1, 1, new Color(0.91f, 0.14f, 0.12f, 0.7f));
				_blue = Common.ColorTexture(1, 1, new Color(0.161f, 0.463f, 0.859f, 0.6f));
				_blueHover = Common.ColorTexture(1, 1, new Color(0.161f, 0.463f, 0.859f, 0.7f));

				_buttonPrimary = Common.ColorTexture(1, 1, new Color(0.4f, 0.4f, 0.4f));
				_buttonPrimaryHover = Common.ColorTexture(1, 1, new Color(0.5f, 0.5f, 0.5f));
				_buttonSecondary = Common.ColorTexture(1, 1, new Color(0.15f, 0.15f, 0.15f));
				_buttonSecondaryHover = Common.ColorTexture(1, 1, new Color(0.25f, 0.25f, 0.25f));
				_box = Common.ColorTexture(1, 1, new Color(0, 0, 0, 0.4f));
				_boxHover = Common.ColorTexture(1, 1, new Color(0, 0, 0, 0.5f));
				_boxDark = Common.ColorTexture(1, 1, new Color(0, 0, 0, 0.6f));

				// First colours pass to create translucent variants of each.
				List<ColorTexture> translucentVariants = new List<ColorTexture>();
				foreach (var colour in _colours)
				{
					Color translucent = colour.Color;
					translucent.a = 0.7f;

					Color hover = colour.Color;
					hover.a = 0.6f;
					translucentVariants.Add(new ColorTexture($"{colour.Name}Translucent", translucent, hover));
				}
				_colours.AddRange(translucentVariants);

				// Core button styling.
				GUIStyle buttonStyle = new GUIStyle(_skin.button);
				buttonStyle.padding = new RectOffset(10, 10, 5, 5);

				// Second colours pass to set up textures and elements.
				foreach (var colour in _colours)
				{
					colour.Texture = Common.ColorTexture(1, 1, colour.Color);
					colour.HoverTexture = Common.ColorTexture(1, 1, colour.HoverColor);
					Color text = Common.GetTextColor(colour.Color);

					GUIStyle button = new GUIStyle(buttonStyle);
					button.name = $"Button{colour.Name}";
					button.normal.background = colour.Texture;
					button.hover.background = colour.HoverTexture;
					button.active.background = colour.HoverTexture;
					button.focused.background = colour.HoverTexture;
					button.normal.textColor = text;
					button.hover.textColor = text;
					button.active.textColor = text;
					button.focused.textColor = text;
					styles.Add(button);

					GUIStyle buttonWrap = new GUIStyle(button);
					buttonWrap.name = $"Button{colour.Name}Wrap";
					buttonWrap.wordWrap = true;
					styles.Add(buttonWrap);

					GUIStyle buttonLarge = new GUIStyle(button);
					buttonLarge.name = $"Button{colour.Name}Large";
					buttonLarge.fontSize = 18;
					styles.Add(buttonLarge);

					GUIStyle buttonSmall = new GUIStyle(button);
					buttonSmall.name = $"Button{colour.Name}Small";
					buttonSmall.fontSize = 12;
					styles.Add(buttonSmall);

					GUIStyle buttonLeft = new GUIStyle(button);
					buttonLeft.name = $"Button{colour.Name}Left";
					buttonLeft.alignment = TextAnchor.MiddleLeft;
					styles.Add(buttonLeft);

					GUIStyle buttonRight = new GUIStyle(button);
					buttonRight.name = $"Button{colour.Name}Right";
					buttonRight.alignment = TextAnchor.MiddleRight;
					styles.Add(buttonRight);

					GUIStyle badge = new GUIStyle(_skin.label);
					badge.name = $"Badge{colour.Name}";
					badge.normal.background = colour.Texture;
					badge.hover.background = colour.Texture;
					badge.active.background = colour.Texture;
					badge.focused.background = colour.Texture;
					badge.alignment = TextAnchor.MiddleCenter;
					badge.normal.textColor = text;
					badge.hover.textColor = text;
					badge.active.textColor = text;
					badge.focused.textColor = text;
					badge.padding = new RectOffset(10, 10, 5, 5);
					styles.Add(badge);

					GUIStyle badgeHover = new GUIStyle(_skin.label);
					badgeHover.name = $"Badge{colour.Name}Hover";
					badgeHover.normal.background = colour.Texture;
					badgeHover.hover.background = colour.HoverTexture;
					badgeHover.active.background = colour.HoverTexture;
					badgeHover.focused.background = colour.HoverTexture;
					badgeHover.alignment = TextAnchor.MiddleCenter;
					badgeHover.normal.textColor = text;
					badgeHover.hover.textColor = text;
					badgeHover.active.textColor = text;
					badgeHover.focused.textColor = text;
					badgeHover.padding = new RectOffset(10, 10, 5, 5);
					styles.Add(badgeHover);
				}

				Color buttonPrimaryTextColour = Color.white;
				Color buttonSecondaryTextColour = Color.white;
				Color textColour = Color.white;

				// Override scrollbar width and height.
				_skin.verticalScrollbar.fixedWidth = _skin.verticalScrollbarThumb.fixedWidth = _skin.horizontalScrollbar.fixedHeight = _skin.horizontalScrollbarThumb.fixedHeight = 8f;

				// Core buttons.
				GUIStyle buttonPrimary = new GUIStyle(buttonStyle);
				buttonPrimary.name = "ButtonPrimary";
				buttonPrimary.normal.background = _buttonPrimary;
				buttonPrimary.hover.background = _buttonPrimaryHover;
				buttonPrimary.active.background = _buttonPrimaryHover;
				buttonPrimary.focused.background = _buttonPrimaryHover;
				buttonPrimary.normal.textColor = buttonPrimaryTextColour;
				buttonPrimary.hover.textColor = buttonPrimaryTextColour;
				buttonPrimary.active.textColor = buttonPrimaryTextColour;
				buttonPrimary.focused.textColor = buttonPrimaryTextColour;

				// Default to use primary button.
				_skin.button = buttonPrimary;

				GUIStyle buttonPrimaryWrap = new GUIStyle(buttonPrimary);
				buttonPrimaryWrap.name = "ButtonPrimaryWrap";
				buttonPrimaryWrap.wordWrap = true;

				GUIStyle buttonPrimaryLarge = new GUIStyle(buttonPrimary);
				buttonPrimaryLarge.name = "ButtonPrimaryLarge";
				buttonPrimaryLarge.fontSize = 18;

				GUIStyle buttonPrimaryTextLeft = new GUIStyle(buttonPrimary);
				buttonPrimaryTextLeft.name = "ButtonPrimaryTextLeft";
				buttonPrimaryTextLeft.alignment = TextAnchor.MiddleLeft;

				GUIStyle buttonSecondary = new GUIStyle(buttonStyle);
				buttonSecondary.name = "ButtonSecondary";
				buttonSecondary.normal.background = _buttonSecondary;
				buttonSecondary.hover.background = _buttonSecondaryHover;
				buttonSecondary.active.background = _buttonSecondaryHover;
				buttonSecondary.focused.background = _buttonSecondaryHover;
				buttonSecondary.normal.textColor = buttonSecondaryTextColour;
				buttonSecondary.hover.textColor = buttonSecondaryTextColour;
				buttonSecondary.active.textColor = buttonSecondaryTextColour;
				buttonSecondary.focused.textColor = buttonSecondaryTextColour;

				GUIStyle buttonSecondaryTextLeft = new GUIStyle(buttonSecondary);
				buttonSecondaryTextLeft.name = "ButtonSecondaryTextLeft";
				buttonSecondaryTextLeft.alignment = TextAnchor.MiddleLeft;

				GUIStyle buttonTransparent = new GUIStyle(buttonStyle);
				buttonTransparent.name = "ButtonTransparent";
				buttonTransparent.normal.background = null;
				buttonTransparent.hover.background = _transparent;
				buttonTransparent.active.background = _transparent;
				buttonTransparent.focused.background = _transparent;
				buttonTransparent.normal.textColor = textColour;
				buttonTransparent.hover.textColor = textColour;
				buttonTransparent.active.textColor = textColour;
				buttonTransparent.focused.textColor = textColour;
				buttonTransparent.wordWrap = true;
				buttonTransparent.alignment = TextAnchor.LowerCenter;

				// Box styling.
				_skin.box.normal.background = _box;

				GUIStyle boxDark = new GUIStyle(_skin.box);
				boxDark.name = "BoxDark";
				boxDark.normal.background = _boxDark;

				// Label styling.
				GUIStyle labelHeader = new GUIStyle(_skin.label);
				labelHeader.name = "LabelHeader";
				labelHeader.alignment = TextAnchor.MiddleLeft;
				labelHeader.fontSize = 24;
				labelHeader.fontStyle = FontStyle.Bold;
				labelHeader.normal.textColor = textColour;
				labelHeader.hover.textColor = textColour;
				labelHeader.active.textColor = textColour;
				labelHeader.focused.textColor = textColour;
				labelHeader.wordWrap = true;

				GUIStyle labelHeaderCenter = new GUIStyle(labelHeader);
				labelHeaderCenter.name = "LabelHeaderCenter";
				labelHeaderCenter.alignment = TextAnchor.MiddleCenter;

				GUIStyle labelSubHeader = new GUIStyle(labelHeader);
				labelSubHeader.name = "LabelSubHeader";
				labelSubHeader.fontSize = 18;

				GUIStyle labelSubHeaderCenter = new GUIStyle(labelSubHeader);
				labelSubHeaderCenter.name = "LabelSubHeaderCenter";
				labelSubHeaderCenter.alignment = TextAnchor.MiddleCenter;

				GUIStyle labelMessage = new GUIStyle(_skin.label);
				labelMessage.name = "LabelMessage";
				labelMessage.alignment = TextAnchor.MiddleCenter;
				labelMessage.fontSize = 40;
				labelMessage.fontStyle = FontStyle.Bold;
				labelMessage.normal.textColor = textColour;
				labelMessage.hover.textColor = textColour;
				labelMessage.active.textColor = textColour;
				labelMessage.focused.textColor = textColour;
				labelMessage.wordWrap = true;

				GUIStyle labelCenter = new GUIStyle(_skin.label);
				labelCenter.name = "LabelCenter";
				labelCenter.alignment = TextAnchor.MiddleCenter;
				labelCenter.normal.textColor = textColour;
				labelCenter.hover.textColor = textColour;
				labelCenter.active.textColor = textColour;
				labelCenter.focused.textColor = textColour;
				labelCenter.wordWrap = true;

				GUIStyle labelLeft = new GUIStyle(_skin.label);
				labelLeft.name = "LabelLeft";
				labelLeft.alignment = TextAnchor.MiddleLeft;
				labelLeft.normal.textColor = textColour;
				labelLeft.hover.textColor = textColour;
				labelLeft.active.textColor = textColour;
				labelLeft.focused.textColor = textColour;
				labelLeft.wordWrap = true;

				GUIStyle labelRight = new GUIStyle(_skin.label);
				labelRight.name = "LabelRight";
				labelRight.alignment = TextAnchor.MiddleRight;
				labelRight.normal.textColor = textColour;
				labelRight.hover.textColor = textColour;
				labelRight.active.textColor = textColour;
				labelRight.focused.textColor = textColour;
				labelRight.wordWrap = true;

				GUIStyle labelWrap = new GUIStyle(_skin.label);
				labelWrap.name = "LabelWrap";
				labelWrap.normal.textColor = textColour;
				labelWrap.hover.textColor = textColour;
				labelWrap.active.textColor = textColour;
				labelWrap.focused.textColor = textColour;
				labelWrap.wordWrap = true;

				GUIStyle labelThin = new GUIStyle(_skin.label);
				labelThin.name = "LabelThin";
				labelThin.normal.textColor = textColour;
				labelThin.hover.textColor = textColour;
				labelThin.active.textColor = textColour;
				labelThin.focused.textColor = textColour;
				labelThin.padding = new RectOffset(1, 0, 1, 1);

				styles.AddRange(new GUIStyle[]
				{
				// Buttons.
				buttonPrimary,
				buttonPrimaryWrap,
				buttonPrimaryLarge,
				buttonPrimaryTextLeft,
				buttonSecondary,
				buttonSecondaryTextLeft,

				buttonTransparent,

				// Boxes.
				boxDark,

				// Labels.
				labelHeader,
				labelHeaderCenter,
				labelSubHeader,
				labelSubHeaderCenter,
				labelMessage,
				labelCenter,
				labelLeft,
				labelRight,
				labelWrap,
				labelThin,

				// These are just here to prevent log errors.
				new GUIStyle() { name = "thumb" },
				new GUIStyle() { name = "upbutton" },
				new GUIStyle() { name = "downbutton" },
				});
				_skin.customStyles = styles.ToArray();
				_hasInitialised = true;
			}
		}

		private static void CreateSkin(GUISkin original)
		{
			// Create skin off default to save setting all GUIStyles individually.
			// Unity doesn't offer a way of doing this so build it manually.
			_skin = ScriptableObject.CreateInstance<GUISkin>();
			_skin.name = $"BetterRadio";
			_skin.box = new GUIStyle(original.box);
			_skin.button = new GUIStyle(original.button);
			_skin.horizontalScrollbar = new GUIStyle(original.horizontalScrollbar);
			_skin.horizontalScrollbarLeftButton = new GUIStyle(original.horizontalScrollbarLeftButton);
			_skin.horizontalScrollbarRightButton = new GUIStyle(original.horizontalScrollbarRightButton);
			_skin.horizontalScrollbarThumb = new GUIStyle(original.horizontalScrollbarThumb);
			_skin.horizontalSlider = new GUIStyle(original.horizontalSlider);
			_skin.horizontalSliderThumb = new GUIStyle(original.horizontalSliderThumb);
			_skin.label = new GUIStyle(original.label);
			_skin.scrollView = new GUIStyle(original.scrollView);
			_skin.textArea = new GUIStyle(original.textArea);
			_skin.textField = new GUIStyle(original.textField);
			_skin.toggle = new GUIStyle(original.toggle);
			_skin.verticalScrollbar = new GUIStyle(original.verticalScrollbar);
			_skin.verticalScrollbarDownButton = new GUIStyle(original.verticalScrollbarDownButton);
			_skin.verticalScrollbarThumb = new GUIStyle(original.verticalScrollbarThumb);
			_skin.verticalScrollbarUpButton = new GUIStyle(original.verticalScrollbarUpButton);
			_skin.verticalSlider = new GUIStyle(original.verticalSlider);
			_skin.verticalSliderThumb = new GUIStyle(original.verticalSliderThumb);
			_skin.window = new GUIStyle(original.window);
			_skin.font = original.font;
		}
	}
}
