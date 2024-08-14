/*
 * Copyright (c) Hubbahu
 */

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[CustomEditor(typeof(DayNightManager))]
class DayNightCycleEditor : Editor
{
	bool foldout;
	SerializedProperty OnNight;
	SerializedProperty OnDay;
	SerializedProperty OnSunOrMoonUpdate;

	void OnEnable()
	{
		// Fetch the objects from the GameObject script to display in the inspector
		OnNight = serializedObject.FindProperty("OnNight");
		OnDay = serializedObject.FindProperty("OnDay");
		OnSunOrMoonUpdate = serializedObject.FindProperty("OnSunOrMoonUpdate");
	}

	public override void OnInspectorGUI()
	{
		DayNightManager castedTarget = (DayNightManager)target;

		GUILayout.BeginHorizontal();
		GUILayout.Label("Today is the", EditorStyles.boldLabel);
		castedTarget.Today = (System.DayOfWeek)EditorGUILayout.EnumPopup(castedTarget.Today);
		GUILayout.Label("of Week", EditorStyles.boldLabel);
		castedTarget.weeks = EditorGUILayout.IntField(castedTarget.weeks);
		GUILayout.EndHorizontal();

		EditorGUILayout.LabelField("Current Time");
		GUILayout.BeginHorizontal(EditorStyles.helpBox);
		EditorGUI.BeginChangeCheck();
		int hours = EditorGUILayout.DelayedIntField(castedTarget.currentTime.CurrentHour);
		GUILayout.Label(":");
		int mins = EditorGUILayout.DelayedIntField(castedTarget.currentTime.CurrentMinute);
		GUILayout.Label(":");
		float secs = EditorGUILayout.DelayedFloatField(castedTarget.currentTime.CurrentSecond);
		if (EditorGUI.EndChangeCheck())
		{
			castedTarget.currentTime.Time = DayNightManager.DayTime.ConvertToHoursInDay(secs, mins, hours);
			castedTarget.SetSunAndMoon(DayNightManager.CalcSunRotX(castedTarget.currentTime));
		}
		GUILayout.EndHorizontal();
		EditorGUI.BeginChangeCheck();
		castedTarget.currentTime.Time = EditorGUILayout.Slider(castedTarget.currentTime.Time, 0, 24);
		if (EditorGUI.EndChangeCheck())
			castedTarget.SetSunAndMoon(DayNightManager.CalcSunRotX(castedTarget.currentTime));
		castedTarget.timeScale = EditorGUILayout.DelayedFloatField("Time Scale", castedTarget.timeScale);

		EditorGUI.BeginChangeCheck();
		castedTarget.sun = EditorGUILayout.ObjectField("Sun", castedTarget.sun, typeof(Light), true) as Light;
		if (EditorGUI.EndChangeCheck() && castedTarget.sun)
			castedTarget.sun_AD = castedTarget.sun.GetComponent<HDAdditionalLightData>();
		EditorGUI.BeginChangeCheck();
		castedTarget.moon = EditorGUILayout.ObjectField("Moon", castedTarget.moon, typeof(Light), true) as Light;
		if (EditorGUI.EndChangeCheck() && castedTarget.moon)
			castedTarget.moon_AD = castedTarget.moon.GetComponent<HDAdditionalLightData>();

		foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, "Stages of the Moon");
		if (foldout)
		{
			if (GUILayout.Button("Force Update Moon Texture"))
				castedTarget.UpdateMoonTex();
			EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
			castedTarget.MoonStages.newMoon =
				TextureField("New", castedTarget.MoonStages.newMoon);
			castedTarget.MoonStages.waxingCrescentMoon =
				TextureField("Waxing Crescent", castedTarget.MoonStages.waxingCrescentMoon);
			castedTarget.MoonStages.firstQuarterMoon =
				TextureField("First Quarter", castedTarget.MoonStages.firstQuarterMoon);
			castedTarget.MoonStages.waxingGibbousMoon =
				TextureField("Waxing Gibbous", castedTarget.MoonStages.waxingGibbousMoon);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
			castedTarget.MoonStages.fullMoon =
				TextureField("Full", castedTarget.MoonStages.fullMoon);
			castedTarget.MoonStages.waningGibbousMoon =
				TextureField("Waning Gibbous", castedTarget.MoonStages.waningGibbousMoon);
			castedTarget.MoonStages.thirdQuarterMoon =
				TextureField("Third Quarter", castedTarget.MoonStages.thirdQuarterMoon);
			castedTarget.MoonStages.waningCrescentMoon =
				TextureField("Waning Crescent", castedTarget.MoonStages.waningCrescentMoon);

			EditorGUILayout.EndHorizontal();
		}
		EditorGUILayout.EndFoldoutHeaderGroup();

		EditorGUILayout.Space(10);

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Prev Week", EditorStyles.miniButtonLeft))
			castedTarget.weeks--;
		if (GUILayout.Button("Prev Day", EditorStyles.miniButtonMid))
			castedTarget.Today--;
		if (GUILayout.Button("Next Day", EditorStyles.miniButtonMid))
			castedTarget.Today++;
		if (GUILayout.Button("Next Week", EditorStyles.miniButtonRight))
			castedTarget.weeks++;
		GUILayout.EndHorizontal();

		castedTarget.pauseTime = GUILayout.Toggle(castedTarget.pauseTime, castedTarget.pauseTime? "Play" : "Pause", "Button");

		EditorGUILayout.Space(10);

		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.PropertyField(OnNight, new GUIContent("On Day", "When the time of day changes to day"));
		EditorGUILayout.PropertyField(OnDay, new GUIContent("On Night", "When the time of day changes to night"));
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.PropertyField(OnSunOrMoonUpdate, new GUIContent("On Sun Or Moon Update", "Called when sun or moon is updated often through rotation"));
		serializedObject.ApplyModifiedProperties();
	}

	private static Texture2D TextureField(string name, Texture2D texture)
	{
		GUILayout.BeginVertical();
		GUILayout.Label(name,  new GUIStyle(GUI.skin.label)
		{
			alignment = TextAnchor.UpperCenter
		});
		Texture2D result = (Texture2D)EditorGUILayout.ObjectField(texture, typeof(Texture2D), false);
		GUILayout.EndVertical();
		return result;
	}
}
