/*
 * Copyright (c) Hubbahu
 */

using UnityEngine;
using System;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Events;

[AddComponentMenu("Enviroment/DayNight Manager")]
public class DayNightManager : MonoBehaviour
{
	DayNightManager instance;
    public float timeScale = 1;
	public bool pauseTime;
    public DayTime currentTime = new DayTime(12);
	public Light sun, moon;
	public HDAdditionalLightData sun_AD, moon_AD;
	public int weeks;
	[SerializeField]
	DayOfWeek today;
	public DayOfWeek Today {
		get => today;
		set
		{
			today = value;
			int intVal = (int)today;
			int mod = Mod(intVal, 7);
			if (intVal < 0)
				weeks -= Mathf.FloorToInt((-intVal - 1) / 7) + 1;
			else
				weeks += Mathf.FloorToInt(intVal / 7);
			today = (DayOfWeek)mod;
		}
	}
	public enum DaysOfWeek { Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday }
	public MoonTextures MoonStages = new MoonTextures();
	public enum MoonStage { New, WaxingCrescent, FirstQuarter, WaxingGibbous, Full, WaningGibbous, ThirdQuarter, WaningCrescent }
	public MoonStage CurrentMoonStage
	{
		get
		{
			MoonStage moonStage = (MoonStage)Mod(weeks, 8);
			return moonStage;
		}
	}
	public bool IsNight { get; private set; }
	public bool IsDay => !IsNight;

	public UnityEvent OnNight = new UnityEvent();
	public UnityEvent OnDay = new UnityEvent();
	public LightChangeEvent OnSunOrMoonUpdate = new LightChangeEvent();
	#region Functions
	#region Unity Functions
	void Start()
	{
		if (instance)
		{
			Destroy(this);
			print("Multiple Instances Made!");
		}
		else
			instance = this;

		currentTime.OnNewDays += (int days) => Today += days;
		UpdateMoonTex();
	}
	void Update()
    {
		if (pauseTime)
			return;
		currentTime.RunTime(Time.deltaTime, timeScale);
		SetSunAndMoon(CalcSunRotX(currentTime));
	}
	#endregion

	public static float CalcSunRotX(DayTime timeOfDay)
	{
		//Remaps the values 0 - 24 with 0, 12:00 AM and 24, 11:59 PM 
		float alpha = timeOfDay.Time / 24;
		//Lerp between the sun positions based off of the time of day and create a rotation on the x-axis off of that
		return Mathf.Lerp(-90, 270, alpha);
	}
	public void SetSunAndMoon(float xRot)
	{
		if (!sun || !moon || !moon_AD || !sun_AD)
			return;
		sun.transform.rotation = Quaternion.Euler(xRot, 0, 0);
		moon.transform.rotation = Quaternion.Euler(xRot - 180, 0, 0);


		if (IsNight && moon.transform.eulerAngles.x > 180)
		{
			//Switch 2 day when appropritate
			IsNight = false;
			OnDay?.Invoke();
			sun_AD.EnableShadows(true);
			moon_AD.EnableShadows(false);
			//Updates the texture as soon as the moon sets to prevent it from being noticable
			UpdateMoonTex();
		}
		else if (!IsNight && sun.transform.eulerAngles.x > 180)
		{
			//Switch 2 night when appropritate
			IsNight = true;
			OnNight?.Invoke();
			sun_AD.EnableShadows(false);
			moon_AD.EnableShadows(true);
		}

		OnSunOrMoonUpdate?.Invoke(IsNight ? moon : sun, IsNight ? moon_AD : sun_AD, currentTime.Time);
	}	  
	public void UpdateMoonTex()
	{
		Texture2D MoonTex = MoonStages.GetTex(CurrentMoonStage);
		if (MoonTex)
			moon_AD.surfaceTexture = MoonTex;
	}
	int Mod(int x, int y)
	{
		int r = x % y;
		return r < 0 ? r + y : r;
	}
	#endregion

	[Serializable]
	public struct MoonTextures
	{
		public Texture2D newMoon;
		public Texture2D waxingCrescentMoon;
		public Texture2D firstQuarterMoon;
		public Texture2D waxingGibbousMoon;
		public Texture2D fullMoon;
		public Texture2D waningGibbousMoon;
		public Texture2D thirdQuarterMoon;
		public Texture2D waningCrescentMoon;

		public Texture2D GetTex(MoonStage moonStage)
		{
			switch (moonStage)
			{
				case MoonStage.New:
					return newMoon;
				case MoonStage.WaxingCrescent:
					return waxingCrescentMoon;
				case MoonStage.FirstQuarter:
					return firstQuarterMoon;
				case MoonStage.WaxingGibbous:
					return waxingGibbousMoon;
				case MoonStage.Full:
					return fullMoon;
				case MoonStage.WaningGibbous:
					return waningGibbousMoon;
				case MoonStage.ThirdQuarter:
					return thirdQuarterMoon;
				case MoonStage.WaningCrescent:
					return waningCrescentMoon;
				default:
					return null;
			}
		}

	}
	[Serializable]
	public struct DayTime
	{
		[SerializeField]
		[Range(0, 24)]
		float time;
		public float Time
		{
			get => time;
			set
			{
				time = value;
				if (time >= 24)
				{
					int daysInHours = Mathf.FloorToInt(time / 24);
					OnNewDays?.Invoke(daysInHours);
					time -= daysInHours * 24;
				}
			}
		}

		public event Action<int> OnNewDays;

		public int CurrentHour => Mathf.FloorToInt(time);
		public int CurrentMinute => Mathf.FloorToInt((time - CurrentHour) * 60);
		public float CurrentSecond => (((time - CurrentHour) * 60) - CurrentMinute) * 60;
		public string TimeOfDayAsString => string.Join(":", CurrentHour.ToString("00"), CurrentMinute.ToString("00"), CurrentSecond.ToString("00.#"));

		public DayTime(float time)
		{
			this.time = time;
			OnNewDays = null;
		}
		public DayTime(float seconds = 0, float minutes = 0, float hours = 0, uint day = 0, uint year = 0)
		{
			time = ConvertToHoursInDay(seconds, minutes, hours);
			OnNewDays = null;
		}

		public void RunTime(float seconds, float scale = 1)
		{
			Time += seconds * scale / 3600;
		}
		public void RunTime(float seconds = 0, float minutes = 0, float hours = 0, float scale = 1)
		{
			Time += ConvertToHoursInDay(seconds, minutes, hours) * scale;
		}

		public static float ConvertToHoursInDay(float seconds = 0, float minutes = 0, float hours = 0)
		{
			return hours + minutes / 60 + seconds / 3600;
		}
	}
}

[Serializable]
public class LightChangeEvent : UnityEvent<Light, HDAdditionalLightData, float>
{
}
