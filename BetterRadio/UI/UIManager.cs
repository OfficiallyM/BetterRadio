using BetterRadio.Radio;
using BetterRadio.Stations;
using System.IO;
using System.Linq;
using TLDLoader;
using UnityEngine;

namespace BetterRadio.UI
{
	internal class UIManager : MonoBehaviour
	{
		private const float WidthFraction = 1f / 3f;
		private const float HeightFraction = 3f / 4f;
		private const float TitleBarHeight = 30f;
		private const float CloseButtonSize = 30f;
		private const float RowHeight = 26f;

		private bool _showUI = false;
		private StreamRadio _radio;
		private StationRepository _repository;
		private SavedStation _currentStation;
		private readonly StationBrowser _browser = new StationBrowser();
		private readonly StationValidator _validator = new StationValidator();

		private int _windowId = BetterRadio.Instance.ID.GetHashCode();
		private Rect _windowRect;
		private int _lastScreenWidth;
		private int _lastScreenHeight;
		private Vector2 _myStationsScroll;
		private Vector2 _browseScroll;
		private Vector2 _searchHelpScroll;
		private string[] _tabs = new string[]
		{
			"My stations",
			"Browse",
		};
		private int _tab = 0;
		private string _manualName = string.Empty;
		private string _manualUrl = string.Empty;
		private string _searchText = string.Empty;
		private bool _showSearchHelp = false;


		private void Awake()
		{
			string path = Path.Combine(ModLoader.GetModConfigFolder(BetterRadio.Instance), "Stations.json");
			_repository = new StationRepository(path);
		}

		public void ShowUI(StreamRadio radio, bool animate = true)
		{
			if (radio == null || _showUI) return;

			_showUI = true;
			_radio = radio;

			mainscript.M.crsrLocked = !_showUI;
			mainscript.M.SetCursorVisible(_showUI);
			mainscript.M.menu.gameObject.SetActive(!_showUI);

			_currentStation = _repository.GetByUrl(_radio.GetUrl());

			if (!animate)
			{
				Animator.Reset("mainUI");
				return;
			}

			Animator.Play("mainUI", Animator.AnimationState.SlideIn);
		}

		public void HideUI(bool animate = true)
		{
			_showUI = false;
			_radio = null;

			mainscript.M.crsrLocked = !_showUI;
			mainscript.M.SetCursorVisible(_showUI);
			mainscript.M.menu.gameObject.SetActive(!_showUI);

			if (!animate)
			{
				Animator.Reset("mainUI");
				return;
			}

			Animator.Play("mainUI", Animator.AnimationState.SlideOut);
		}

		private void Update()
		{
			if (_showUI && Input.GetButtonDown("Cancel"))
				HideUI(false);
		}

		private void OnGUI()
		{
			if (!_showUI || _radio == null) return;

			GUI.skin = Styling.GetSkin();

			// Recomputed only on the first frame and after a resolution change, so a window the user has dragged stays put otherwise.
			if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
				CenterWindow();

			_windowRect = GUI.Window(_windowId, _windowRect, DrawWindow, string.Empty, "box");

			GUI.skin = null;
		}

		private void CenterWindow()
		{
			_lastScreenWidth = Screen.width;
			_lastScreenHeight = Screen.height;

			float width = Screen.width * WidthFraction;
			float height = Screen.height * HeightFraction;

			_windowRect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
		}

		private void DrawWindow(int id)
		{
			if (_radio == null) return;

			DrawTitleBar();

			GUILayout.BeginArea(new Rect(0, 35f, _windowRect.width, _windowRect.height - 30f));
			GUILayout.BeginVertical();
			DrawTabBar();
			GUILayout.Space(5);

			if (_currentStation != null || _radio.IsRunning)
			{
				GUILayout.Label($"Current station: {_currentStation?.Name ?? _radio.GetUrl()}", "BadgeBlue");
				GUILayout.Space(10);
			}

			switch (_tab)
			{
				case 0:
					DrawMyStationsTab();
					break;
				case 1:
					DrawBrowseTab();
					break;
			}

			DrawFooter();
			GUILayout.Space(5);
			GUILayout.EndVertical();
			GUILayout.EndArea();
		}

		private void DrawTitleBar()
		{
			GUI.Box(new Rect(0, 0, _windowRect.width, TitleBarHeight), "<size=18><b>Stream configuration</b></size>", "BoxDark");

			float dragWidth = _windowRect.width - CloseButtonSize;
			GUI.DragWindow(new Rect(0, 0, dragWidth, TitleBarHeight));

			if (GUI.Button(new Rect(_windowRect.width - CloseButtonSize, 0, CloseButtonSize, TitleBarHeight), "X"))
				HideUI();
		}

		private void DrawTabBar()
		{
			GUILayout.BeginHorizontal();
			for (int i = 0; i < _tabs.Length; i++)
			{
				string style = i == _tab ? "ButtonSecondary" : "button";
				if (GUILayout.Button(_tabs[i], style, GUILayout.MaxHeight(30f)) && _tab != i)
				{
					_tab = i;
				}
			}
			GUILayout.EndHorizontal();
		}

		private void DrawMyStationsTab()
		{
			GUILayout.Label("Saved stations", "LabelHeader");
			GUILayout.Label("Select to play");

			_myStationsScroll = GUILayout.BeginScrollView(_myStationsScroll);

			// ToList so Remove during this loop can't invalidate the enumerator mid-draw.
			foreach (SavedStation station in _repository.Stations.ToList())
			{
				if (_radio == null) break;

				GUILayout.BeginHorizontal("box", GUILayout.Height(RowHeight));

				if (GUILayout.Button(station.Name, _radio.GetUrl() == station.Url ? "ButtonSecondary" : "button"))
				{
					_radio.SetUrl(station.Url);
					_currentStation = _repository.GetByUrl(station.Url);
					HideUI();
				}

				if (GUILayout.Button("-", GUILayout.Width(RowHeight)))
					_repository.Remove(station);

				GUILayout.EndHorizontal();
			}

			GUILayout.EndScrollView();
			GUILayout.Space(10);

			DrawManualAdd();
			GUILayout.Space(10);
		}

		private void DrawManualAdd()
		{
			GUILayout.Label("Add a station URL", "LabelHeader");

			GUILayout.BeginHorizontal();
			GUILayout.Label("Name", GUILayout.Width(50));
			_manualName = GUILayout.TextField(_manualName);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("URL", GUILayout.Width(50));
			string previousUrl = _manualUrl;
			_manualUrl = GUILayout.TextField(_manualUrl);

			// Editing the URL after a check invalidates that check, rather than leaving a stale result showing against different text.
			if (_manualUrl != previousUrl)
				_validator.Reset();

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

			bool canSubmit = !string.IsNullOrWhiteSpace(_manualName) && !string.IsNullOrWhiteSpace(_manualUrl) && !_validator.IsValidating;

			GUI.enabled = canSubmit;
			if (GUILayout.Button(_validator.IsValidating ? "Checking..." : "Add"))
				HandleManualAdd();
			GUI.enabled = true;

			if (_validator.Success == false)
				GUILayout.Label(_validator.Error, "BadgeRed");

			GUILayout.EndHorizontal();
		}

		private void HandleManualAdd()
		{
			// A previous probe against this exact text already succeeded, return early.
			if (_validator.Success == true) return;

			_validator.Validate(_manualUrl, () => {
				_repository.Add(new SavedStation { Name = _manualName, Url = _manualUrl });
				_manualName = string.Empty;
				_manualUrl = string.Empty;
				_validator.Reset();
			});
		}

		private void DrawBrowseTab()
		{
			GUILayout.BeginVertical();
			GUILayout.Label("Search Radio Browser", "LabelHeader");

			GUILayout.BeginHorizontal();
			_searchText = GUILayout.TextField(_searchText);

			GUI.enabled = !string.IsNullOrWhiteSpace(_searchText) && !_browser.IsSearching;
			if (GUILayout.Button(_browser.IsSearching ? "Searching..." : "Search", GUILayout.Width(80)))
				_browser.Search(_searchText);
			GUI.enabled = true;

			if (GUILayout.Button("Help", GUILayout.ExpandWidth(false)))
				_showSearchHelp = !_showSearchHelp;

			GUILayout.EndHorizontal();

			if (_showSearchHelp)
			{
				GUILayout.BeginVertical("box", GUILayout.MaxHeight(_windowRect.height / 2));
				GUILayout.Label("Search help", "LabelSubHeader");
				_searchHelpScroll = GUILayout.BeginScrollView(_searchHelpScroll);
				GUILayout.Label("A basic search, for example 'bbc' will search radio stations by name");
				GUILayout.Label("For advanced searches, you can specify tags to search by certain criteria");
				GUILayout.Label("You can negate a tag by prefixing it with a !. For example: '!lang:english' would show any results where the language is not English");
				GUILayout.Label("By default, all searches will use a contains match, if you'd like to match exactly, you can wrap your query in double quotes. For example: 'tag:\"jazz\"");
				GUILayout.Label("See below for a full list of available tags:");
				foreach (var tag in StationQueryParser.Prefixes)
				{
					GUILayout.Label($"{tag.Prefix} - {tag.Help}");
				}
				GUILayout.EndScrollView();
				GUILayout.EndVertical();
			}

			if (_browser.Error != null)
				GUILayout.Label(_browser.Error, "BadgeRed");

			_browseScroll = GUILayout.BeginScrollView(_browseScroll);

			foreach (BrowseResult result in _browser.CurrentPage)
			{
				GUILayout.BeginHorizontal("box", GUILayout.Height(RowHeight));

				bool alreadySaved = _repository.Stations.Any(s => s.Url == result.Url);
				GUILayout.Label($"{result.Name} ({result.Bitrate}kbps, {result.CountryCode})");
				GUILayout.FlexibleSpace();

				GUI.enabled = !alreadySaved;
				if (GUILayout.Button(alreadySaved ? "Added" : "Add", GUILayout.Width(60)))
					_repository.Add(new SavedStation { Name = result.Name, Url = result.Url });
				GUI.enabled = true;

				GUILayout.EndHorizontal();
			}

			GUILayout.EndScrollView();
			GUILayout.FlexibleSpace();

			GUILayout.BeginHorizontal();
			GUI.enabled = _browser.CanGoPrevious;
			if (GUILayout.Button("< Prev"))
				_browser.PreviousPage();

			GUI.enabled = _browser.CanGoNext && !_browser.IsSearching;
			if (GUILayout.Button(_browser.IsSearching ? "Loading..." : "Next >"))
				_browser.NextPage();

			GUI.enabled = true;
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
		}

		private void DrawFooter()
		{
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Label("Made with ❤️ by M-");
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}
	}
}
