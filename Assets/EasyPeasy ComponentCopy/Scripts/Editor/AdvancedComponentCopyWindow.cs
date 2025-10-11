using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace EasyPeasyComponentCopy {

public class AdvancedComponentCopyWindow : EditorWindow
{
    #region Menu & Creation

	[MenuItem("Tools/Advanced Component Copy (Enhanced)")]
	public static void ShowWindow()
	{
		var window = CreateInstance<AdvancedComponentCopyWindow>();
		window.titleContent = new GUIContent("Advanced Component Copy");
		window.minSize = new Vector2(800, 400);
		window.ShowUtility();
	}

    #endregion

    #region EditorPrefs Keys

	private const string PREF_KEY_BG_R     = "Accw_BG_R";
	private const string PREF_KEY_BG_G     = "Accw_BG_G";
	private const string PREF_KEY_BG_B     = "Accw_BG_B";

	private const string PREF_KEY_FONT     = "Accw_FontSize";
	private const string PREF_KEY_BOLD     = "Accw_Bold";
	private const string PREF_KEY_ITALIC   = "Accw_Italic";

	private const string PREF_KEY_PANEL_R  = "Accw_Panel_R";
	private const string PREF_KEY_PANEL_G  = "Accw_Panel_G";
	private const string PREF_KEY_PANEL_B  = "Accw_Panel_B";

	private const string PREF_KEY_BUTTON_R = "Accw_Button_R";
	private const string PREF_KEY_BUTTON_G = "Accw_Button_G";
	private const string PREF_KEY_BUTTON_B = "Accw_Button_B";

	private const string PREF_KEY_BANNER_R = "Accw_Banner_R";
	private const string PREF_KEY_BANNER_G = "Accw_Banner_G";
	private const string PREF_KEY_BANNER_B = "Accw_Banner_B";

	private const string PREF_KEY_SHOW_BANNER     = "Accw_ShowBanner";
	private const string PREF_KEY_SHOW_ICON       = "Accw_ShowBannerIcon";
	private const string PREF_KEY_BANNER_TITLE    = "Accw_BannerTitle";
	private const string PREF_KEY_BANNER_ICON     = "Accw_BannerIconName";
	private const string PREF_KEY_BANNER_ICONSIZE = "Accw_BannerIconSize";

    #endregion

    #region Fields & Theming

	// Colors
	private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
	private Color panelColor      = new Color(0.25f, 0.25f, 0.25f, 1f);
	private Color buttonColor     = new Color(0.3f, 0.5f, 0.7f, 1f);
	private Color bannerColor     = new Color(1f, 0.55f, 0.0f, 1f);

	// Font
	private int   fontSize       = 12;
	private bool  useBoldText    = false;
	private bool  useItalicText  = false;

	// Banner
	private bool   showBanner     = true;
	private bool   showBannerIcon = true;
	private string bannerTitle    = "Component Copy";
	private string bannerIconName = "Clipboard";
	private float  bannerIconSize = 24f;

	// Styles
	private GUIStyle foldoutStyle;
	private GUIStyle boxStyle;
	private GUIStyle labelStyle;
	private GUIStyle toolbarButtonStyle;
	private GUIStyle titleStyle;

	// Foldouts
	private bool showTheming    = false;
	private bool sourcesFoldout = true;
	private bool targetsFoldout = true;
	private bool batchFoldout   = true;

	// Overwrite toggle
	private bool alwaysReplace = false;

    #endregion

    #region Search

	private enum SearchMode { Partial, Exact, Regex }
	private SearchMode searchMode = SearchMode.Partial;

    #endregion

    #region OnEnable & OnGUI

	private void OnEnable()
	{
		// Load user prefs
		float br = EditorPrefs.GetFloat(PREF_KEY_BG_R, 0.2f);
		float bg = EditorPrefs.GetFloat(PREF_KEY_BG_G, 0.2f);
		float bb = EditorPrefs.GetFloat(PREF_KEY_BG_B, 0.2f);
		backgroundColor = new Color(br, bg, bb, 1f);

		float pr = EditorPrefs.GetFloat(PREF_KEY_PANEL_R, panelColor.r);
		float pg = EditorPrefs.GetFloat(PREF_KEY_PANEL_G, panelColor.g);
		float pb = EditorPrefs.GetFloat(PREF_KEY_PANEL_B, panelColor.b);
		panelColor = new Color(pr, pg, pb, 1f);

		float buR = EditorPrefs.GetFloat(PREF_KEY_BUTTON_R, buttonColor.r);
		float buG = EditorPrefs.GetFloat(PREF_KEY_BUTTON_G, buttonColor.g);
		float buB = EditorPrefs.GetFloat(PREF_KEY_BUTTON_B, buttonColor.b);
		buttonColor = new Color(buR, buG, buB, 1f);

		float banR = EditorPrefs.GetFloat(PREF_KEY_BANNER_R, bannerColor.r);
		float banG = EditorPrefs.GetFloat(PREF_KEY_BANNER_G, bannerColor.g);
		float banB = EditorPrefs.GetFloat(PREF_KEY_BANNER_B, bannerColor.b);
		bannerColor = new Color(banR, banG, banB, 1f);

		fontSize      = EditorPrefs.GetInt(PREF_KEY_FONT, 12);
		useBoldText   = EditorPrefs.GetBool(PREF_KEY_BOLD, false);
		useItalicText = EditorPrefs.GetBool(PREF_KEY_ITALIC, false);

		showBanner      = EditorPrefs.GetBool(PREF_KEY_SHOW_BANNER, true);
		showBannerIcon  = EditorPrefs.GetBool(PREF_KEY_SHOW_ICON,   true);
		bannerTitle     = EditorPrefs.GetString(PREF_KEY_BANNER_TITLE, "Component Copy");
		bannerIconName  = EditorPrefs.GetString(PREF_KEY_BANNER_ICON, "Clipboard");
		bannerIconSize  = EditorPrefs.GetFloat(PREF_KEY_BANNER_ICONSIZE, 24f);
	}

	private void OnGUI()
	{
		CheckStyles();

		if (Event.current.type == EventType.Repaint)
		{
			EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);
		}

		if (showBanner)
		{
			DrawBanner();
		}

		DrawSettingsSection();

		EditorGUILayout.BeginVertical();
		{
			EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
			{
				EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
				DrawSourcesSection();
				EditorGUILayout.EndVertical();

				EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
				DrawTargetsSection();
				EditorGUILayout.EndVertical();
			}
			EditorGUILayout.EndHorizontal();

			DrawCopyButtons();
		}
		EditorGUILayout.EndVertical();
	}

	private void CheckStyles()
	{
		FontStyle userFontStyle;
		if (useBoldText && useItalicText)      userFontStyle = FontStyle.BoldAndItalic;
		else if (useBoldText)                 userFontStyle = FontStyle.Bold;
		else if (useItalicText)               userFontStyle = FontStyle.Italic;
		else                                  userFontStyle = FontStyle.Normal;

		if (foldoutStyle == null)
		{
			foldoutStyle = new GUIStyle(EditorStyles.foldout)
			{
				fontSize  = fontSize,
				fontStyle = userFontStyle,
				richText  = true
			};
		}
		else
		{
			foldoutStyle.fontSize  = fontSize;
			foldoutStyle.fontStyle = userFontStyle;
			foldoutStyle.richText  = true;
		}

		if (boxStyle == null)
		{
			boxStyle = new GUIStyle(GUI.skin.box)
			{
				fontSize  = fontSize,
				fontStyle = userFontStyle,
				margin    = new RectOffset(5,5,5,5),
				padding   = new RectOffset(8,8,8,8),
			};
		}
		else
		{
			boxStyle.fontSize  = fontSize;
			boxStyle.fontStyle = userFontStyle;
		}

		if (labelStyle == null)
		{
			labelStyle = new GUIStyle(EditorStyles.label)
			{
				fontSize  = fontSize,
				fontStyle = userFontStyle,
				richText  = true
			};
		}
		else
		{
			labelStyle.fontSize  = fontSize;
			labelStyle.fontStyle = userFontStyle;
			labelStyle.richText  = true;
		}

		if (toolbarButtonStyle == null)
		{
			toolbarButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
			{
				fontSize  = fontSize,
				fontStyle = userFontStyle
			};
		}
		else
		{
			toolbarButtonStyle.fontSize  = fontSize;
			toolbarButtonStyle.fontStyle = userFontStyle;
		}

		if (titleStyle == null)
		{
			titleStyle = new GUIStyle(EditorStyles.boldLabel)
			{
				fontSize  = fontSize + 2,
				alignment = TextAnchor.MiddleCenter,
				fontStyle = userFontStyle
			};
		}
		else
		{
			titleStyle.fontSize  = fontSize + 2;
			titleStyle.fontStyle = userFontStyle;
		}
	}

    #endregion

    #region Banner & Theming

	private void DrawBanner()
	{
		Rect bannerRect = GUILayoutUtility.GetRect(position.width, 60f);
		if (Event.current.type == EventType.Repaint)
		{
			EditorGUI.DrawRect(bannerRect, bannerColor);
		}

		if (showBannerIcon)
		{
			// Attempt to load user-chosen icon
			GUIContent iconContent = null;
			try
			{
				iconContent = EditorGUIUtility.IconContent(bannerIconName);
			}
				catch
				{
					iconContent = null;
				}
			if (iconContent == null || iconContent.image == null)
			{
				iconContent = EditorGUIUtility.IconContent("Clipboard");
			}

			float clampedSize = Mathf.Clamp(bannerIconSize, 8f, 128f);
			float iconX = bannerRect.x + 8f;
			float iconY = bannerRect.y + (bannerRect.height - clampedSize) * 0.5f;
			Rect iconRect = new Rect(iconX, iconY, clampedSize, clampedSize);

			if (iconContent != null && iconContent.image != null)
			{
				GUI.DrawTexture(iconRect, iconContent.image, ScaleMode.ScaleToFit);
			}
		}

		Rect titleRect = new Rect(
			bannerRect.x,
			bannerRect.y,
			bannerRect.width,
			bannerRect.height
		);
		string finalTitle = string.IsNullOrEmpty(bannerTitle) ? "Component Copy" : bannerTitle;
		EditorGUI.LabelField(titleRect, finalTitle, titleStyle);
	}

	private void DrawSettingsSection()
	{
		showTheming = EditorGUILayout.Foldout(showTheming, "Customization / Theming", true, foldoutStyle);
		if (!showTheming) return;

		Rect r = EditorGUILayout.BeginVertical(boxStyle);
		if (Event.current.type == EventType.Repaint)
			EditorGUI.DrawRect(r, panelColor);

		EditorGUILayout.Space();

		backgroundColor = EditorGUILayout.ColorField("Background Color", backgroundColor);
		panelColor      = EditorGUILayout.ColorField("Panel Color",       panelColor);
		buttonColor     = EditorGUILayout.ColorField("Button Color",      buttonColor);
		bannerColor     = EditorGUILayout.ColorField("Banner Color",      bannerColor);

		showBanner = EditorGUILayout.Toggle("Show Banner", showBanner);
		if (showBanner)
		{
			showBannerIcon = EditorGUILayout.Toggle("Show Banner Icon", showBannerIcon);
			bannerTitle    = EditorGUILayout.TextField("Banner Title", bannerTitle);

			bannerIconName = EditorGUILayout.TextField("Banner Icon Name", bannerIconName);
			bannerIconSize = EditorGUILayout.Slider("Banner Icon Size", bannerIconSize, 8f, 128f);
		}

		useBoldText   = EditorGUILayout.Toggle("Use Bold Text", useBoldText);
		useItalicText = EditorGUILayout.Toggle("Use Italic Text", useItalicText);
		fontSize      = EditorGUILayout.IntSlider("Font Size", fontSize, 8, 24);

		if (GUILayout.Button("Save Theme Settings", GUILayout.Height(25)))
		{
			EditorPrefs.SetFloat(PREF_KEY_BG_R, backgroundColor.r);
			EditorPrefs.SetFloat(PREF_KEY_BG_G, backgroundColor.g);
			EditorPrefs.SetFloat(PREF_KEY_BG_B, backgroundColor.b);

			EditorPrefs.SetFloat(PREF_KEY_PANEL_R, panelColor.r);
			EditorPrefs.SetFloat(PREF_KEY_PANEL_G, panelColor.g);
			EditorPrefs.SetFloat(PREF_KEY_PANEL_B, panelColor.b);

			EditorPrefs.SetFloat(PREF_KEY_BUTTON_R, buttonColor.r);
			EditorPrefs.SetFloat(PREF_KEY_BUTTON_G, buttonColor.g);
			EditorPrefs.SetFloat(PREF_KEY_BUTTON_B, buttonColor.b);

			EditorPrefs.SetFloat(PREF_KEY_BANNER_R, bannerColor.r);
			EditorPrefs.SetFloat(PREF_KEY_BANNER_G, bannerColor.g);
			EditorPrefs.SetFloat(PREF_KEY_BANNER_B, bannerColor.b);

			EditorPrefs.SetBool(PREF_KEY_SHOW_BANNER, showBanner);
			EditorPrefs.SetBool(PREF_KEY_SHOW_ICON, showBannerIcon);
			EditorPrefs.SetString(PREF_KEY_BANNER_TITLE, bannerTitle);

			EditorPrefs.SetString(PREF_KEY_BANNER_ICON, bannerIconName);
			EditorPrefs.SetFloat(PREF_KEY_BANNER_ICONSIZE, bannerIconSize);

			EditorPrefs.SetInt(PREF_KEY_FONT, fontSize);
			EditorPrefs.SetBool(PREF_KEY_BOLD, useBoldText);
			EditorPrefs.SetBool(PREF_KEY_ITALIC, useItalicText);

			foldoutStyle = null; 
			boxStyle = null; 
			labelStyle = null; 
			toolbarButtonStyle = null; 
			titleStyle = null;

			Debug.Log("Theme settings saved.");
			EditorGUIUtility.ExitGUI();
		}

		EditorGUILayout.Space();
		EditorGUILayout.EndVertical();
	}

    #endregion

    #region Sources

	private string sourceSearchText = "";
	private Vector2 sourceScrollPos;

	private List<SourceObjectData> sources = new List<SourceObjectData>();

	private class SourceObjectData
	{
		public GameObject source;
		public bool foldout      = true;
		public bool copyChildren = false;
		public List<SourceComponentData> components = new List<SourceComponentData>();
		public List<ChildObjectData> children = new List<ChildObjectData>();
	}

	private class SourceComponentData
	{
		public Component component;
		public bool foldoutFields;
		public bool selected = true;
		public List<FieldData> fields = new List<FieldData>();
	}

	private class FieldData
	{
		public FieldInfo fieldInfo;
		public bool copyField = true;
	}

	/// <summary>
	/// Each child has 'selected' for the *GameObject itself* – if false, we skip the entire child.
	/// </summary>
	private class ChildObjectData
	{
		public GameObject child;
		public bool selected = true;
		public bool foldout;
		public List<SourceComponentData> components = new List<SourceComponentData>();
		public List<ChildObjectData> children = new List<ChildObjectData>();
	}

	private void DrawSourcesSection()
	{
		Rect r = EditorGUILayout.BeginVertical();
		if (Event.current.type == EventType.Repaint)
		{
			EditorGUI.DrawRect(r, panelColor);
		}

		sourcesFoldout = EditorGUILayout.Foldout(sourcesFoldout, "Source Objects", true, foldoutStyle);
		if (!sourcesFoldout)
		{
			EditorGUILayout.EndVertical();
			return;
		}
		EditorGUILayout.Space();

		EditorGUILayout.LabelField("Search Settings:", labelStyle);
		sourceSearchText = EditorGUILayout.TextField("Search Text", sourceSearchText);
		searchMode = (SearchMode)EditorGUILayout.EnumPopup("Search Mode", searchMode);

		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Expand All Fields", GUILayout.Width(120)))
		{
			SetAllFieldsFoldout(true);
		}
		if (GUILayout.Button("Collapse All Fields", GUILayout.Width(130)))
		{
			SetAllFieldsFoldout(false);
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Expand All Sources", GUILayout.Width(130)))
		{
			ExpandOrCollapseAllSources(true);
		}
		if (GUILayout.Button("Collapse All Sources", GUILayout.Width(130)))
		{
			ExpandOrCollapseAllSources(false);
		}
		EditorGUILayout.EndHorizontal();

		DrawDragZone(true);

		if (GUILayout.Button("Add Selected as Sources", GUILayout.Width(200)))
		{
			foreach (var obj in Selection.gameObjects)
			{
				if (!sources.Any(s => s.source == obj))
				{
					sources.Add(BuildSourceData(obj));
				}
			}
		}
		if (GUILayout.Button("Clear Sources", GUILayout.Width(120)))
		{
			sources.Clear();
		}

		sourceScrollPos = EditorGUILayout.BeginScrollView(sourceScrollPos, GUILayout.ExpandHeight(true));
		for (int i = 0; i < sources.Count; i++)
		{
			var src = sources[i];
			if (!DoesSourceMatch(src)) continue;

			EditorGUILayout.BeginVertical(boxStyle);

			EditorGUILayout.BeginHorizontal();
			string displayedName = src.source ? src.source.name : "(null)";
			displayedName = HighlightSearchTerm(displayedName, sourceSearchText, "cyan");
			src.foldout = EditorGUILayout.Foldout(src.foldout, displayedName, true, foldoutStyle);

			if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Trash"), GUILayout.Width(24)))
			{
				sources.RemoveAt(i);
				i--;
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndVertical();
				continue;
			}
			EditorGUILayout.EndHorizontal();

			var newObj = (GameObject)EditorGUILayout.ObjectField("Source", src.source, typeof(GameObject), true);
			if (newObj != src.source)
			{
				sources[i] = BuildSourceData(newObj);
				EditorGUILayout.EndVertical();
				continue;
			}

			src.copyChildren = EditorGUILayout.Toggle("Copy Children?", src.copyChildren);

			if (src.foldout && src.source)
			{
				if (src.copyChildren && src.children.Count == 0)
				{
					src.children = BuildChildTree(src.source.transform);
				}

				// top-level comps
				foreach (var compData in src.components)
				{
					if (!DoesComponentMatch(src.source, compData)) continue;
					DrawSourceComponentData(src.source, compData);
				}

				// children
				if (src.copyChildren && src.children.Count > 0)
				{
					foreach (var childData in src.children)
					{
						if (!DoesChildMatch(childData)) continue;
						DrawChildObjectData(childData, 1);
					}
				}
			}

			EditorGUILayout.EndVertical();
		}
		EditorGUILayout.EndScrollView();

		EditorGUILayout.EndVertical();
	}

	private bool DoesSourceMatch(SourceObjectData src)
	{
		if (MatchesSearch(src.source, null, null)) return true;
		foreach (var c in src.components)
			if (DoesComponentMatch(src.source, c)) return true;

		if (src.copyChildren)
		{
			foreach (var ch in src.children)
			{
				if (DoesChildMatch(ch)) return true;
			}
		}
		return false;
	}

	private bool DoesChildMatch(ChildObjectData child)
	{
		if (!child.child) return false;
		// If child isn't selected, user might want to see it in the UI or not. We'll still do search matching so they can find it and toggle it on if needed
		if (MatchesSearch(child.child, null, null)) return true;

		foreach (var c in child.components)
		{
			if (DoesComponentMatch(child.child, c)) return true;
		}
		foreach (var sub in child.children)
		{
			if (DoesChildMatch(sub)) return true;
		}
		return false;
	}

	private bool DoesComponentMatch(GameObject go, SourceComponentData compData)
	{
		if (MatchesSearch(go, compData, null)) return true;
		foreach (var f in compData.fields)
		{
			if (MatchesSearch(go, compData, f)) return true;
		}
		return false;
	}

	private bool MatchesSearch(GameObject go, SourceComponentData compData, FieldData fieldData)
	{
		if (string.IsNullOrEmpty(sourceSearchText)) return true;
		bool invert = false;
		string term = sourceSearchText;
		if (term.StartsWith("-"))
		{
			invert = true;
			term = term.Substring(1);
			if (string.IsNullOrEmpty(term)) return true;
		}

		bool isMatch = false;

		if (go)
		{
			isMatch = CheckStringMatch(go.name, term);
		}
		if (!isMatch && compData != null && compData.component)
		{
			string compName = compData.component.GetType().Name;
			isMatch = CheckStringMatch(compName, term);
		}
		if (!isMatch && fieldData != null)
		{
			string fName = fieldData.fieldInfo.Name;
			isMatch = CheckStringMatch(fName, term);
		}

		return invert ? !isMatch : isMatch;
	}

	private bool CheckStringMatch(string candidate, string term)
	{
		switch (searchMode)
		{
		case SearchMode.Partial:
			return candidate.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
		case SearchMode.Exact:
			return string.Equals(candidate, term, StringComparison.OrdinalIgnoreCase);
		case SearchMode.Regex:
			try
			{
				return Regex.IsMatch(candidate, term, RegexOptions.IgnoreCase);
			}
				catch { return false; }
		}
		return false;
	}

	private void DrawSourceComponentData(GameObject parentObj, SourceComponentData compData)
	{
		if (!compData.component) return;

		string compName = compData.component.GetType().Name;
		compName = HighlightSearchTerm(compName, sourceSearchText, "yellow");
		var compIcon = GetIconForComponent(compData.component);

		EditorGUILayout.BeginVertical(boxStyle);
		EditorGUILayout.BeginHorizontal();

		GUIContent compLabel = new GUIContent(compName, compIcon);
		compData.selected = EditorGUILayout.ToggleLeft(compLabel, compData.selected, GUILayout.Width(220));

		if (compData.fields.Count > 0)
		{
			if (GUILayout.Button(compData.foldoutFields ? "Hide Fields" : "Show Fields", GUILayout.Width(80)))
			{
				compData.foldoutFields = !compData.foldoutFields;
			}
		}
		EditorGUILayout.EndHorizontal();

		if (compData.foldoutFields && compData.selected && compData.fields.Count > 0)
		{
			EditorGUI.indentLevel++;
			foreach (var fData in compData.fields)
			{
				if (!MatchesSearch(parentObj, compData, fData)) continue;
				EditorGUILayout.BeginHorizontal();
				string fieldName = fData.fieldInfo.Name;
				string fieldVal  = GetFieldValuePreview(fData.fieldInfo, compData.component);
				string display   = $"{fieldName} ({fieldVal})";
				display = HighlightSearchTerm(display, sourceSearchText, "lime");

				fData.copyField = EditorGUILayout.ToggleLeft(display, fData.copyField);
				EditorGUILayout.EndHorizontal();
			}
			EditorGUI.indentLevel--;
		}

		EditorGUILayout.EndVertical();
	}

	/// <summary>
	/// Now each child has a 'selected' toggle for the game object. If not selected, we skip copying it.
	/// We can still expand it in the UI (but disabled) to see what's inside.
	/// </summary>
	private void DrawChildObjectData(ChildObjectData childData, int indent)
	{
		EditorGUI.indentLevel = indent;
		if (!childData.child) return;

		EditorGUILayout.BeginHorizontal();

		// Left: The toggle for "selected"
		childData.selected = EditorGUILayout.Toggle(childData.selected, GUILayout.Width(20));

		// Right: The foldout for the child's name
		string childName = childData.child.name;
		childName = HighlightSearchTerm(childName, sourceSearchText, "cyan");
		childData.foldout = EditorGUILayout.Foldout(childData.foldout, childName, true, foldoutStyle);

		EditorGUILayout.EndHorizontal();

		// If folded out, show child components/children
		if (childData.foldout)
		{
			EditorGUI.indentLevel = indent + 1;

			// We'll optionally disable if childData.selected == false, so user can see them but not toggle anything
			bool oldEnabled = GUI.enabled;
			if (!childData.selected)
			{
				GUI.enabled = false;
			}

			foreach (var compData in childData.components)
			{
				if (!DoesComponentMatch(childData.child, compData)) continue;
				DrawSourceComponentData(childData.child, compData);
			}

			foreach (var cData in childData.children)
			{
				if (!DoesChildMatch(cData)) continue;
				DrawChildObjectData(cData, indent + 1);
			}

			if (!childData.selected)
			{
				GUI.enabled = oldEnabled;
			}
		}
	}

	private void SetAllFieldsFoldout(bool expand)
	{
		foreach (var s in sources)
		{
			foreach (var c in s.components)
			{
				c.foldoutFields = expand;
			}
			ExpandAllChildFields(s.children, expand);
		}
	}

	private void ExpandAllChildFields(List<ChildObjectData> childList, bool expand)
	{
		foreach (var child in childList)
		{
			foreach (var c in child.components)
			{
				c.foldoutFields = expand;
			}
			ExpandAllChildFields(child.children, expand);
		}
	}

	private void ExpandOrCollapseAllSources(bool expand)
	{
		sourcesFoldout = expand;
		foreach (var s in sources)
		{
			s.foldout = expand;
			foreach (var c in s.components)
			{
				c.foldoutFields = expand;
			}
			ExpandOrCollapseChildren(s.children, expand);
		}
	}

	private void ExpandOrCollapseChildren(List<ChildObjectData> childList, bool expand)
	{
		foreach (var child in childList)
		{
			child.foldout = expand;
			foreach (var comp in child.components)
			{
				comp.foldoutFields = expand;
			}
			ExpandOrCollapseChildren(child.children, expand);
		}
	}

    #endregion

    #region Targets

	private string targetSearchText = "";
	private Vector2 targetScrollPos;

	private List<TargetObjectData> targets = new List<TargetObjectData>();

	private class TargetObjectData
	{
		public GameObject target;
		public bool clearBeforePaste = false;
	}

	private bool useBatchTag   = false;
	private string batchTag    = "Untagged";
	private bool useBatchLayer = false;
	private int batchLayer     = 0;

	private void DrawTargetsSection()
	{
		Rect r = EditorGUILayout.BeginVertical();
		if (Event.current.type == EventType.Repaint)
		{
			EditorGUI.DrawRect(r, panelColor);
		}

		targetsFoldout = EditorGUILayout.Foldout(targetsFoldout, "Target Objects", true, foldoutStyle);
		if (!targetsFoldout)
		{
			EditorGUILayout.EndVertical();
			return;
		}

		EditorGUILayout.Space();
		targetSearchText = EditorGUILayout.TextField("Search (object name)", targetSearchText);

		DrawDragZone(false);

		if (GUILayout.Button("Add Selected as Targets", GUILayout.Width(180)))
		{
			foreach (var obj in Selection.gameObjects)
			{
				if (!targets.Any(t => t.target == obj))
				{
					targets.Add(new TargetObjectData { target = obj, clearBeforePaste = false });
				}
			}
		}

		if (GUILayout.Button("Clear Targets", GUILayout.Width(120)))
		{
			targets.Clear();
		}

		batchFoldout = EditorGUILayout.Foldout(batchFoldout, "Batch Mode (Tag/Layer)", true, foldoutStyle);
		if (batchFoldout)
		{
			EditorGUILayout.BeginVertical(boxStyle);
			useBatchTag = EditorGUILayout.ToggleLeft("Use Tag?", useBatchTag);
			if (useBatchTag)
			{
				batchTag = EditorGUILayout.TagField("Tag", batchTag);
			}

			useBatchLayer = EditorGUILayout.ToggleLeft("Use Layer?", useBatchLayer);
			if (useBatchLayer)
			{
				batchLayer = EditorGUILayout.LayerField("Layer", batchLayer);
			}
			EditorGUILayout.HelpBox(
				"If Tag/Layer is enabled, the copy also applies to all scene objects matching that tag/layer.",
				MessageType.Info
			);
			EditorGUILayout.EndVertical();
		}

		targetScrollPos = EditorGUILayout.BeginScrollView(targetScrollPos, GUILayout.ExpandHeight(true));
		for (int i = 0; i < targets.Count; i++)
		{
			var objData = targets[i];

			if (!string.IsNullOrEmpty(targetSearchText) && objData.target != null)
			{
				if (!objData.target.name.ToLower().Contains(targetSearchText.ToLower()))
					continue;
			}

			EditorGUILayout.BeginHorizontal(boxStyle);

			objData.target = (GameObject)EditorGUILayout.ObjectField($"Target {i+1}", objData.target, typeof(GameObject), true);

			objData.clearBeforePaste = EditorGUILayout.ToggleLeft("Clear Before Paste?", objData.clearBeforePaste, GUILayout.Width(140));

			if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Trash"), GUILayout.Width(24)))
			{
				targets.RemoveAt(i);
				i--;
				EditorGUILayout.EndHorizontal();
				continue;
			}
			EditorGUILayout.EndHorizontal();
		}
		EditorGUILayout.EndScrollView();

		alwaysReplace = EditorGUILayout.ToggleLeft("Auto Overwrite All Conflicts?", alwaysReplace, GUILayout.Width(240));
		EditorGUILayout.HelpBox(
			"If a target already has the same component type, you will normally be prompted to overwrite or skip. " +
			"If 'Auto Overwrite' is checked, all conflicts will be overwritten automatically.",
			MessageType.Info
		);

		EditorGUILayout.EndVertical();
	}

    #endregion

    #region Drag & Drop

	private void DrawDragZone(bool isSource)
	{
		Rect dropArea = EditorGUILayout.BeginVertical(boxStyle, GUILayout.Height(40));
		GUILayout.FlexibleSpace();
		GUILayout.Label("Drag " + (isSource ? "Source" : "Target") + " Objects Here", labelStyle);
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndVertical();

		switch (Event.current.type)
		{
		case EventType.DragUpdated:
		case EventType.DragPerform:
			if (!dropArea.Contains(Event.current.mousePosition)) break;
			DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
			if (Event.current.type == EventType.DragPerform)
			{
				DragAndDrop.AcceptDrag();
				foreach (var dragged in DragAndDrop.objectReferences)
				{
					var go = dragged as GameObject;
					if (!go) continue;
					if (isSource)
					{
						if (!sources.Any(s => s.source == go))
						{
							sources.Add(BuildSourceData(go));
						}
					}
					else
					{
						if (!targets.Any(t => t.target == go))
						{
							targets.Add(new TargetObjectData{ target = go, clearBeforePaste = false });
						}
					}
				}
			}
			Event.current.Use();
			break;
		}
	}

    #endregion

    #region Copy Operation & Dry Run

	private void DrawCopyButtons()
	{
		EditorGUILayout.Space();
		EditorGUILayout.BeginHorizontal();
		{
			Color oldColor = GUI.backgroundColor;

			GUI.backgroundColor = Color.Lerp(buttonColor, Color.white, 0.3f);
			if (GUILayout.Button("Preview Copy (Dry Run)", GUILayout.Height(30)))
			{
				PerformDetailedDryRun();  // advanced
			}

			GUI.backgroundColor = buttonColor;
			if (GUILayout.Button("Perform Copy", GUILayout.Height(30)))
			{
				PerformCopy();
			}

			GUI.backgroundColor = oldColor;
		}
		EditorGUILayout.EndHorizontal();
	}

	/// <summary>
	/// This advanced Dry Run enumerates new vs. overwritten, field changes, child creation, etc.
	/// Summaries are displayed in a pop-up with an option to export, then "Everything OK?" => proceed with actual copy.
	/// </summary>
	private void PerformDetailedDryRun()
	{
		if (sources.Count == 0)
		{
			Debug.LogWarning("No sources to copy from (Dry Run).");
			return;
		}

		List<TargetObjectData> finalTargets = BuildFinalTargetList();
		if (finalTargets.Count == 0)
		{
			Debug.LogWarning("No target objects found for the Dry Run.");
			return;
		}

		// We'll gather data in a summary object
		var summary = new DetailedDryRunSummary();
		summary.dateTime = DateTime.Now.ToString();

		var globalCompStats = new Dictionary<string, (int newCount, int overwrittenCount)>();

		// For each source -> target
		foreach (var src in sources)
		{
			if (!src.source) continue;
			// build child if needed
			if (src.copyChildren && src.children.Count == 0)
			{
				src.children = BuildChildTree(src.source.transform);
			}

			bool isSourcePrefab = IsPrefabAsset(src.source);
			string sourceName = src.source.name;

			foreach (var tgt in finalTargets)
			{
				if (!tgt.target) continue;
				bool isTargetPrefab = IsPrefabAsset(tgt.target);
				string targetName = tgt.target.name;

				var pairInfo = new SourceTargetPairInfo {
					sourceName     = sourceName,
					isSourcePrefab = isSourcePrefab,
					targetName     = targetName,
					isTargetPrefab = isTargetPrefab
				};

				// top-level comps
				foreach (var compData in src.components)
				{
					if (!compData.selected) continue;
					EvaluateComponentDryRun(compData, tgt.target, pairInfo, globalCompStats);
				}

				// children
				if (src.copyChildren)
				{
					EvaluateChildDryRun(src.children, tgt.target.transform, pairInfo, globalCompStats);
				}

				summary.allPairs.Add(pairInfo);
			}
		}

		// Summaries grouped by component type
		foreach (var kvp in globalCompStats)
		{
			var ctype = kvp.Key;
			var info  = kvp.Value;
			summary.componentTypeStats.Add(new ComponentTypeStats {
				componentType   = ctype,
				newCount        = info.newCount,
				overwrittenCount= info.overwrittenCount
			});
		}

		string bigSummary = BuildDetailedSummaryText(summary);

		bool proceed = EditorUtility.DisplayDialog("Detailed Dry Run Summary",
			"Preview of new vs. overwritten components. Click 'View Full Details' to see more or export.\n\nContinue?",
			"View Full Details", "Cancel");

		if (!proceed) return;

		int ret = EditorUtility.DisplayDialogComplex(
			"Dry Run (Extended Summary)",
			bigSummary,
			"Export To File", "OK", "Cancel"
		);
		// 0 => Export, 1 => OK, 2 => Cancel
		if (ret == 0)
		{
			string path = EditorUtility.SaveFilePanel("Save Dry Run Summary", "", "dry_run_summary.txt", "txt");
			if (!string.IsNullOrEmpty(path))
			{
				System.IO.File.WriteAllText(path, bigSummary);
				Debug.Log("Exported summary to: " + path);
			}
			int confirm = EditorUtility.DisplayDialogComplex("Proceed with Copy?",
				"Would you like to perform the actual copy now?",
				"Yes", "No", "Cancel");

			if (confirm == 0)
			{
				PerformCopy();
			}
		}
		else if (ret == 1)
		{
			bool finalOK = EditorUtility.DisplayDialog("Everything OK?",
				"Would you like to proceed with the actual copy now?",
				"Yes", "No");
			if (finalOK)
			{
				PerformCopy();
			}
		}
		else
		{
			// Cancel
		}
	}

	private void PerformCopy()
	{
		if (sources.Count == 0)
		{
			Debug.LogWarning("No sources to copy from.");
			return;
		}

		var finalTargets = BuildFinalTargetList();
		if (finalTargets.Count == 0)
		{
			Debug.LogWarning("No target objects found for the copy operation.");
			return;
		}

		Undo.SetCurrentGroupName("Advanced Component Copy");
		int undoGroup = Undo.GetCurrentGroup();

		foreach (var src in sources)
		{
			if (!src.source) continue;
			if (src.copyChildren && src.children.Count == 0)
			{
				src.children = BuildChildTree(src.source.transform);
			}

			foreach (var tgt in finalTargets)
			{
				if (!tgt.target) continue;

				if (tgt.clearBeforePaste)
				{
					ClearComponents(tgt.target);
				}

				// top-level comps
				foreach (var compData in src.components)
				{
					if (!compData.selected) continue;
					CopyComponentDataToTarget(compData, tgt.target);
				}

				// children
				if (src.copyChildren)
				{
					foreach (var cData in src.children)
					{
						PasteChildRecursively(cData, tgt.target.transform);
					}
				}
			}
		}

		Undo.CollapseUndoOperations(undoGroup);
		Debug.Log($"Copied components from {sources.Count} source(s) to {finalTargets.Count} target(s).");
	}

	private List<TargetObjectData> BuildFinalTargetList()
	{
		List<TargetObjectData> final = new List<TargetObjectData>(targets);

		// batch Tag
		if (useBatchTag && !string.IsNullOrEmpty(batchTag) && batchTag != "Untagged")
		{
			var tagged = GameObject.FindGameObjectsWithTag(batchTag);
			foreach (var t in tagged)
			{
				if (!final.Any(ft => ft.target == t))
				{
					final.Add(new TargetObjectData{ target = t, clearBeforePaste = false });
				}
			}
		}

#if UNITY_2023_1_OR_NEWER
		var allObjs = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
#else
		var allObjs = GameObject.FindObjectsOfType<GameObject>();
#endif
		// batch Layer
		if (useBatchLayer)
		{
			foreach (var o in allObjs)
			{
				if (o.layer == batchLayer && !final.Any(ft => ft.target == o))
				{
					final.Add(new TargetObjectData{ target = o, clearBeforePaste = false });
				}
			}
		}
		return final;
	}

	private void ClearComponents(GameObject go)
	{
		var comps = go.GetComponents<Component>();
		foreach (var c in comps)
		{
			if (c is Transform) continue;
			Undo.DestroyObjectImmediate(c);
		}
	}

	private void PasteChildRecursively(ChildObjectData childData, Transform targetParent)
	{
		// If user didn't select the child, skip entirely
		if (!childData.selected) return;
		if (!childData.child) return;

		Transform existingChild = targetParent.Find(childData.child.name);
		GameObject targetChild;
		if (existingChild)
		{
			targetChild = existingChild.gameObject;
		}
		else
		{
			targetChild = new GameObject(childData.child.name);
			Undo.RegisterCreatedObjectUndo(targetChild, "Create Child");
			targetChild.transform.SetParent(targetParent, false);
		}

		foreach (var compData in childData.components)
		{
			if (!compData.selected) continue;
			CopyComponentDataToTarget(compData, targetChild);
		}

		foreach (var c in childData.children)
		{
			PasteChildRecursively(c, targetChild.transform);
		}
	}

	private void CopyComponentDataToTarget(SourceComponentData compData, GameObject targetObj)
	{
		if (!compData.component) return;
		Type compType = compData.component.GetType();

		var existing = targetObj.GetComponent(compType);
		if (existing)
		{
			bool doReplace = alwaysReplace;
			if (!alwaysReplace)
			{
				string msg = $"Target '{targetObj.name}' already has a {compType.Name}. Overwrite fields?";
				doReplace = EditorUtility.DisplayDialog("Component Conflict", msg, "Yes", "No");
			}
			if (doReplace)
			{
				Undo.RecordObject(existing, "Overwrite Component");
				OverwriteFields(compData, existing);
			}
		}
		else
		{
			var newComp = Undo.AddComponent(targetObj, compType);
			OverwriteFields(compData, newComp);
		}
	}

	private void OverwriteFields(SourceComponentData compData, Component destComp)
	{
		foreach (var fData in compData.fields)
		{
			if (!fData.copyField) continue;
			var sourceVal = fData.fieldInfo.GetValue(compData.component);
			fData.fieldInfo.SetValue(destComp, sourceVal);
		}
	}

    #endregion

    #region Dry Run Data & Methods

	private bool IsPrefabAsset(GameObject go)
	{
		// check via PrefabUtility
		if (!go) return false;
		var assetType = PrefabUtility.GetPrefabAssetType(go);
		return assetType != PrefabAssetType.NotAPrefab && !PrefabUtility.IsPartOfPrefabInstance(go);
	}

	/// <summary>
	/// Our big data structure for a "Detailed Dry Run."
	/// </summary>
	private class DetailedDryRunSummary
	{
		public string dateTime;
		public List<SourceTargetPairInfo> allPairs = new List<SourceTargetPairInfo>();
		public List<ComponentTypeStats> componentTypeStats = new List<ComponentTypeStats>();
	}

	private class SourceTargetPairInfo
	{
		public string sourceName;
		public bool   isSourcePrefab;
		public string targetName;
		public bool   isTargetPrefab;

		public List<ComponentOperationInfo> componentOps = new List<ComponentOperationInfo>();

		public int newChildObjects;
		public List<string> childCreationPaths = new List<string>();
	}

	private class ComponentOperationInfo
	{
		public string componentType;
		public bool isNew;
		public bool isOverwritten;

		public List<FieldChangeInfo> changedFields   = new List<FieldChangeInfo>();
		public List<FieldChangeInfo> identicalFields = new List<FieldChangeInfo>();
		public List<string> potentialIssues          = new List<string>();
	}

	private class FieldChangeInfo
	{
		public string fieldName;
		public object oldValue;
		public object newValue;
	}

	private class ComponentTypeStats
	{
		public string componentType;
		public int newCount;
		public int overwrittenCount;
	}

	/// <summary>
	/// Evaluate top-level SourceComponentData for a single target object, populating pairInfo & globalCompStats.
	/// </summary>
	private void EvaluateComponentDryRun(SourceComponentData compData, GameObject targObj,
		SourceTargetPairInfo pairInfo, Dictionary<string, (int newCount, int overwrittenCount)> globalCompStats)
	{
		if (!compData.component) return;
		Type compType = compData.component.GetType();
		string compTypeName = compType.Name;

		var compOp = new ComponentOperationInfo { componentType = compTypeName };

		// does the target already have it?
		var existing = targObj.GetComponent(compType);
		if (existing)
		{
			compOp.isOverwritten = true;
			CompareFieldsForDryRun(compData, existing, compOp);

			if (!globalCompStats.ContainsKey(compTypeName))
				globalCompStats[compTypeName] = (0,0);
			var oldVal = globalCompStats[compTypeName];
			oldVal.overwrittenCount++;
			globalCompStats[compTypeName] = oldVal;
		}
		else
		{
			compOp.isNew = true;
			CompareFieldsForDryRunNew(compData, compOp);

			if (!globalCompStats.ContainsKey(compTypeName))
				globalCompStats[compTypeName] = (0,0);
			var oldVal = globalCompStats[compTypeName];
			oldVal.newCount++;
			globalCompStats[compTypeName] = oldVal;
		}

		pairInfo.componentOps.Add(compOp);
	}

	/// <summary>
	/// Evaluate child objects for Dry Run, counting new child creation, etc.
	/// Skips if child is not selected.
	/// </summary>
	private void EvaluateChildDryRun(List<ChildObjectData> childList, Transform targetParent,
		SourceTargetPairInfo pairInfo,
		Dictionary<string, (int newCount, int overwrittenCount)> globalCompStats)
	{
		foreach (var childData in childList)
		{
			if (!childData.child) continue;
			if (!childData.selected) continue; // user didn't select that child

			var existingChild = targetParent.Find(childData.child.name);
			bool isNewChild = (existingChild == null);
			if (isNewChild)
			{
				pairInfo.newChildObjects++;
				pairInfo.childCreationPaths.Add(BuildChildPath(childData.child, targetParent));
			}

			// Components
			foreach (var compData in childData.components)
			{
				if (!compData.selected) continue;
				Type cType = compData.component.GetType();
				string cTypeName = cType.Name;

				var compOp = new ComponentOperationInfo { componentType = cTypeName };

				if (isNewChild)
				{
					compOp.isNew = true;
					CompareFieldsForDryRunNew(compData, compOp);

					if (!globalCompStats.ContainsKey(cTypeName))
						globalCompStats[cTypeName] = (0,0);
					var oldVal = globalCompStats[cTypeName];
					oldVal.newCount++;
					globalCompStats[cTypeName] = oldVal;
				}
				else
				{
					var existingComp = existingChild.GetComponent(cType);
					if (existingComp)
					{
						compOp.isOverwritten = true;
						CompareFieldsForDryRun(compData, existingComp, compOp);

						if (!globalCompStats.ContainsKey(cTypeName))
							globalCompStats[cTypeName] = (0,0);
						var oldVal = globalCompStats[cTypeName];
						oldVal.overwrittenCount++;
						globalCompStats[cTypeName] = oldVal;
					}
					else
					{
						compOp.isNew = true;
						CompareFieldsForDryRunNew(compData, compOp);

						if (!globalCompStats.ContainsKey(cTypeName))
							globalCompStats[cTypeName] = (0,0);
						var oldVal = globalCompStats[cTypeName];
						oldVal.newCount++;
						globalCompStats[cTypeName] = oldVal;
					}
				}

				pairInfo.componentOps.Add(compOp);
			}

			// Recurse deeper
			if (!isNewChild)
			{
				EvaluateChildDryRun(childData.children, existingChild, pairInfo, globalCompStats);
			}
			else
			{
				// entire sub-branch is new
				CountAllDescendantsAsNew(childData.children, pairInfo, globalCompStats);
			}
		}
	}

	/// <summary> Recursively mark subchildren as new. </summary>
	private void CountAllDescendantsAsNew(List<ChildObjectData> children,
		SourceTargetPairInfo pairInfo,
		Dictionary<string, (int newCount,int overwrittenCount)> globalCompStats)
	{
		foreach (var cData in children)
		{
			if (!cData.child) continue;
			if (!cData.selected) continue;  // skip unselected
			pairInfo.newChildObjects++;
			pairInfo.childCreationPaths.Add(BuildChildPath(cData.child, null));

			foreach (var compData in cData.components)
			{
				if (!compData.selected) continue;
				var cType = compData.component.GetType();
				string cTypeName = cType.Name;

				var compOp = new ComponentOperationInfo { componentType = cTypeName, isNew = true };
				CompareFieldsForDryRunNew(compData, compOp);

				if (!globalCompStats.ContainsKey(cTypeName))
					globalCompStats[cTypeName] = (0,0);
				var oldVal = globalCompStats[cTypeName];
				oldVal.newCount++;
				globalCompStats[cTypeName] = oldVal;

				pairInfo.componentOps.Add(compOp);
			}

			// Recurse
			CountAllDescendantsAsNew(cData.children, pairInfo, globalCompStats);
		}
	}

	private string BuildChildPath(GameObject child, Transform possibleParent)
	{
		if (!child) return "(null child?)";
		string path = child.name;
		Transform p = child.transform.parent;
		while (p != null && p != possibleParent)
		{
			path = p.name + "/" + path;
			p = p.parent;
		}
		return path;
	}

	private void CompareFieldsForDryRun(SourceComponentData compData, Component existingComp, ComponentOperationInfo compOp)
	{
		foreach (var fData in compData.fields)
		{
			if (!fData.copyField) continue;
			object sourceVal = fData.fieldInfo.GetValue(compData.component);
			object targetVal = fData.fieldInfo.GetValue(existingComp);

			if (Equals(sourceVal, targetVal))
			{
				compOp.identicalFields.Add(new FieldChangeInfo {
					fieldName = fData.fieldInfo.Name,
					oldValue  = targetVal,
					newValue  = targetVal
				});
			}
			else
			{
				var change = new FieldChangeInfo {
					fieldName = fData.fieldInfo.Name,
					oldValue  = targetVal,
					newValue  = sourceVal
				};

				// Potential issues if sourceVal references a missing object
				if (sourceVal is UnityEngine.Object uo)
				{
					if (!uo)
					{
						compOp.potentialIssues.Add($"Field '{change.fieldName}' references missing object!");
					}
				}

				compOp.changedFields.Add(change);
			}
		}
	}

	private void CompareFieldsForDryRunNew(SourceComponentData compData, ComponentOperationInfo compOp)
	{
		foreach (var fData in compData.fields)
		{
			if (!fData.copyField) continue;
			object sourceVal = fData.fieldInfo.GetValue(compData.component);

			var change = new FieldChangeInfo {
				fieldName = fData.fieldInfo.Name,
				oldValue  = "(none)",
				newValue  = sourceVal
			};

			if (sourceVal is UnityEngine.Object uo && !uo)
			{
				compOp.potentialIssues.Add($"Field '{change.fieldName}' references missing object!");
			}

			compOp.changedFields.Add(change);
		}
	}

	private string BuildDetailedSummaryText(DetailedDryRunSummary summary)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("=== Advanced Dry Run Summary ===");
		sb.AppendLine($"Date/Time: {summary.dateTime}");
		sb.AppendLine("");

		sb.AppendLine("--- Global Component Type Stats ---");
		foreach (var cts in summary.componentTypeStats.OrderBy(s => s.componentType))
		{
			sb.AppendLine($"   {cts.componentType}: {cts.newCount} new, {cts.overwrittenCount} overwritten");
		}
		sb.AppendLine("");

		sb.AppendLine("--- Per-Source/Target Breakdown ---");
		foreach (var pair in summary.allPairs)
		{
			sb.AppendLine($"Source: {pair.sourceName} {(pair.isSourcePrefab ? "(Prefab)" : "(Scene)")} " +
				$"-> Target: {pair.targetName} {(pair.isTargetPrefab ? "(Prefab)" : "(Scene)")}");

			if (pair.newChildObjects > 0)
			{
				sb.AppendLine($"   New Child Objects: {pair.newChildObjects}");
				foreach (var path in pair.childCreationPaths)
				{
					sb.AppendLine($"      + [Child] {path}");
				}
			}

			if (pair.componentOps.Count == 0)
			{
				sb.AppendLine("   (No components selected or found?)");
			}
			else
			{
				foreach (var compOp in pair.componentOps)
				{
					sb.Append($"   [Comp] {compOp.componentType} => ");
					if (compOp.isNew)         sb.Append("NEW ");
					if (compOp.isOverwritten) sb.Append("OVERWRITTEN ");
					sb.AppendLine();

					if (compOp.changedFields.Count > 0)
					{
						sb.AppendLine($"      Changed Fields: {compOp.changedFields.Count}");
						foreach (var fc in compOp.changedFields)
						{
							sb.AppendLine($"         - {fc.fieldName}: {ValToString(fc.oldValue)} -> {ValToString(fc.newValue)}");
						}
					}
					if (compOp.identicalFields.Count > 0)
					{
						sb.AppendLine($"      Identical Fields: {compOp.identicalFields.Count}");
						foreach (var fc in compOp.identicalFields)
						{
							sb.AppendLine($"         - {fc.fieldName} => {ValToString(fc.oldValue)} (unchanged)");
						}
					}
					if (compOp.potentialIssues.Count > 0)
					{
						sb.AppendLine($"      Potential Issues:");
						foreach (var issue in compOp.potentialIssues)
						{
							sb.AppendLine($"         ! {issue}");
						}
					}
				}
			}
			sb.AppendLine("");
		}

		sb.AppendLine("=== End of Summary ===");
		return sb.ToString();
	}

	private string ValToString(object val)
	{
		if (val == null) return "null";
		if (val is string s) return $"\"{s}\"";
		return val.ToString();
	}

    #endregion

    #region Data Builders

	/// <summary>
	/// Build the SourceObjectData by skipping missing scripts and transforms.
	/// Pull all instance fields for each script. 
	/// </summary>
	private SourceObjectData BuildSourceData(GameObject go)
	{
		var result = new SourceObjectData
		{
			source       = go,
			copyChildren = false,
			foldout      = true
		};

		if (!go) return result;

		var allComps = go.GetComponents<Component>();
		foreach (var c in allComps)
		{
			if (c == null || c is Transform) 
				continue;
			var scd = BuildSourceComponentData(c);
			if (scd != null) 
				result.components.Add(scd);
		}
		return result;
	}

	private SourceComponentData BuildSourceComponentData(Component comp)
	{
		if (!comp) return null;

		var scd = new SourceComponentData { component = comp, selected = true };

		// Show all instance fields
		var fields = comp.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (var f in fields)
		{
			if (f.IsInitOnly || f.IsLiteral) continue; // skip readonly/const
			scd.fields.Add(new FieldData { fieldInfo = f, copyField = true });
		}

		return scd;
	}

	private List<ChildObjectData> BuildChildTree(Transform parent)
	{
		var result = new List<ChildObjectData>();
		foreach (Transform child in parent)
		{
			var cData = new ChildObjectData { child = child.gameObject, foldout = false, selected = true };

			var allComps = child.GetComponents<Component>();
			foreach (var c in allComps)
			{
				if (c == null || c is Transform) continue;
				var scd = BuildSourceComponentData(c);
				if (scd != null)
					cData.components.Add(scd);
			}
			cData.children = BuildChildTree(child);
			result.Add(cData);
		}
		return result;
	}

    #endregion

    #region Helpers

	private string GetFieldValuePreview(FieldInfo field, Component comp)
	{
		object val = field.GetValue(comp);
		if (val == null) return "null";

		Type ft = field.FieldType;
		if (ft == typeof(string))
		{
			return $"\"{val}\"";
		}
		else if (ft == typeof(Vector2) || ft == typeof(Vector3) || ft == typeof(Vector4) || ft == typeof(Color))
		{
			return val.ToString();
		}
		else if (ft.IsPrimitive || ft.IsEnum)
		{
			return val.ToString();
		}
		else if (val is UnityEngine.Object unityObj)
		{
			return unityObj ? unityObj.name : "null ref";
		}
		return val.ToString();
	}

	private Texture2D GetIconForComponent(Component comp)
	{
		if (!comp) return EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;
		Type t = comp.GetType();

		if (t == typeof(MeshRenderer))
			return EditorGUIUtility.IconContent("MeshRenderer Icon").image as Texture2D;
		if (t == typeof(BoxCollider))
			return EditorGUIUtility.IconContent("BoxCollider Icon").image as Texture2D;
		if (t == typeof(SphereCollider))
			return EditorGUIUtility.IconContent("SphereCollider Icon").image as Texture2D;
		if (t == typeof(CapsuleCollider))
			return EditorGUIUtility.IconContent("CapsuleCollider Icon").image as Texture2D;
		if (t == typeof(MeshFilter))
			return EditorGUIUtility.IconContent("MeshFilter Icon").image as Texture2D;

		return EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;
	}

	private string HighlightSearchTerm(string text, string searchTerm, string colorName)
	{
		if (string.IsNullOrEmpty(searchTerm)) return text;
		if (searchTerm.StartsWith("-"))
		{
			searchTerm = searchTerm.Substring(1);
		}
		if (string.IsNullOrEmpty(searchTerm)) return text;

		if (searchMode == SearchMode.Regex)
		{
			try
			{
				var match = Regex.Match(text, searchTerm, RegexOptions.IgnoreCase);
				if (match.Success)
				{
					int idx = match.Index;
					int len = match.Length;
					string before = text.Substring(0, idx);
					string middle = text.Substring(idx, len);
					string after  = text.Substring(idx + len);
					return $"{before}<color={colorName}>{middle}</color>{after}";
				}
			}
				catch { }
			return text;
		}
		else if (searchMode == SearchMode.Exact)
		{
			if (string.Equals(text, searchTerm, StringComparison.OrdinalIgnoreCase))
			{
				return $"<color={colorName}>{text}</color>";
			}
			return text;
		}
		else
		{
			int idx = text.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase);
			if (idx < 0) return text;

			string matched = text.Substring(idx, searchTerm.Length);
			string before  = text.Substring(0, idx);
			string after   = text.Substring(idx + searchTerm.Length);
			return $"{before}<color={colorName}>{matched}</color>{after}";
		}
	}

    #endregion
}

}
