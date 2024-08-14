/*
 * Copyright (c) Hubbahu
 */
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.Rendering;

public class CloudNoiseGen : EditorWindow
{
	#region EditorData
	struct PreviewTexSettings
	{
		public bool previewTex;
		[HideInInspector]
		public ColorWriteMask previewChannel;
		public int currentSlice;
		public bool transparency;

		public bool open_R;
		public bool open_G;
		public bool open_B;
		public bool open_A;

		public PreviewTexSettings(bool previewTex = false, bool transparency = false, ColorWriteMask previewChannel = ColorWriteMask.All, int currentSlice = 1)
		{
			this.previewTex = previewTex;
			this.previewChannel = previewChannel;
			this.currentSlice = Math.Max(currentSlice, 1);
			open_R = open_G = open_B = open_A = false;
			this.transparency = transparency;
		}
	}
	PreviewTexSettings shapeTexSettings = new PreviewTexSettings(currentSlice: 1);
	PreviewTexSettings detailTexSettings = new PreviewTexSettings(currentSlice: 1);
	public bool debugDropdown, logTimes, autoUpdate;
	Vector2 shapeScrollPos, detailScrollPos;
	#endregion

	public ComputeShader cloudNoiseShader;
	public ComputeShader slicerShader;

	[Tooltip("Texture meant for the main shape of the clouds. Should be higher res than the details. Check paper for good values")]
	public CloudTextureSettings shapeTex = new CloudTextureSettings(1, 128, 
		new PerlinWorley(ColorWriteMask.Red),
		new Worley(ColorWriteMask.Green),
		new Worley(ColorWriteMask.Blue),
		new Worley(ColorWriteMask.Alpha));

	[Tooltip("Texture meant for the details. Should be lower res. Check paper for good values")]
	public CloudTextureSettings detailTex = new CloudTextureSettings(1, 32,
		new Worley(ColorWriteMask.Red),
		new Worley(ColorWriteMask.Green),
		new Worley(ColorWriteMask.Blue),
		new None(ColorWriteMask.Alpha));

	[MenuItem("Window/Custom/CloudNoiseGen")]
	public static void ShowWindow()
	{
		GetWindow<CloudNoiseGen>("Cloud Noise Generator", typeof(SceneView));
	}

	public void OnGUI()
	{
		//Debug Features
		debugDropdown = EditorGUILayout.BeginToggleGroup("Debug Settings", debugDropdown);
		EditorGUI.indentLevel++;
		autoUpdate = EditorGUILayout.Toggle("Auto Update", autoUpdate);
		logTimes = EditorGUILayout.Toggle("Log Times", logTimes);
		EditorGUI.indentLevel--;
		
		GUILayout.Space(15);
		EditorGUILayout.EndToggleGroup();

		bool wide = position.width > 550;
		Vector2 maxSize = position.size;
		if (wide)
		{
			GUILayout.BeginHorizontal();
			maxSize.x /= 2;
		}
		else
			maxSize.y = (maxSize.y - 100) / 2;
		//-------------------------------- Shape Texture --------------------------------
		GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MaxWidth(maxSize.x), GUILayout.MaxHeight(maxSize.y));
		EditorGUILayout.LabelField("Shape Texture", EditorStyles.whiteLargeLabel);
		DisplayTexSettings(ref shapeTex);
		shapeScrollPos = EditorGUILayout.BeginScrollView(shapeScrollPos);

		EditorGUILayout.Space(10);
		EditorGUI.BeginChangeCheck();
		shapeTexSettings.open_R = DisplayNoise(ref shapeTex, ref shapeTex.channel_R, shapeTexSettings.open_R, "Red Channel");
		shapeTexSettings.open_G = DisplayNoise(ref shapeTex, ref shapeTex.channel_G, shapeTexSettings.open_G, "Green Channel");
		shapeTexSettings.open_B = DisplayNoise(ref shapeTex, ref shapeTex.channel_B, shapeTexSettings.open_B, "Blue Channel");
		shapeTexSettings.open_A = DisplayNoise(ref shapeTex, ref shapeTex.channel_A, shapeTexSettings.open_A, "Alpha Channel");
		//Shape Preview
		EditorGUILayout.Space(15);
		shapeTexSettings.previewTex = EditorGUILayout.BeginFoldoutHeaderGroup(shapeTexSettings.previewTex, "Preview");
		if (shapeTexSettings.previewTex)
			DisplayTexture(ref shapeTexSettings, ref shapeTex);
		EditorGUILayout.EndFoldoutHeaderGroup();

		EditorGUILayout.EndScrollView();
		GUILayout.EndVertical();

		//-------------------------------- Detail Texture --------------------------------
		GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MaxWidth(maxSize.x), GUILayout.MaxHeight(maxSize.y));
		EditorGUILayout.LabelField("Detail Texture", EditorStyles.whiteLargeLabel);
		DisplayTexSettings(ref detailTex);
		if (EditorGUI.EndChangeCheck() && autoUpdate && detailTex.TextureCreated)
		{
			detailTex.GenNoise(cloudNoiseShader);
			detailTex.GenSlices(slicerShader);
		}
		detailScrollPos = EditorGUILayout.BeginScrollView(detailScrollPos);

		EditorGUILayout.Space(10);
		detailTexSettings.open_R = DisplayNoise(ref detailTex, ref detailTex.channel_R, detailTexSettings.open_R, "Red Channel");
		detailTexSettings.open_G = DisplayNoise(ref detailTex, ref detailTex.channel_G, detailTexSettings.open_G, "Green Channel");
		detailTexSettings.open_B = DisplayNoise(ref detailTex, ref detailTex.channel_B, detailTexSettings.open_B, "Blue Channel");
		detailTexSettings.open_A = DisplayNoise(ref detailTex, ref detailTex.channel_A, detailTexSettings.open_A, "Alpha Channel");
		//Detail preview
		EditorGUILayout.Space(15);
		detailTexSettings.previewTex = EditorGUILayout.BeginFoldoutHeaderGroup(detailTexSettings.previewTex, "Preview");
		if (detailTexSettings.previewTex)
			DisplayTexture(ref detailTexSettings, ref detailTex);
		EditorGUILayout.EndFoldoutHeaderGroup();

		EditorGUILayout.EndScrollView();
		GUILayout.EndVertical();
		if (wide)
			GUILayout.EndHorizontal();

		if (GUILayout.Button("Save & Close"))
			SaveAndClose();
	}
	void DisplayTexSettings(ref CloudTextureSettings settings)
	{
		EditorGUI.BeginChangeCheck();
		settings.resolution = Mathf.Max(1, EditorGUILayout.IntField("Resolution", settings.resolution));
		settings.tileAmount = Mathf.Max(1, EditorGUILayout.IntField("Tile Amount", settings.tileAmount));
		if (EditorGUI.EndChangeCheck() && autoUpdate && settings.TextureCreated)
		{
			settings.GenNoise(cloudNoiseShader);
			settings.GenSlices(slicerShader);
		}
	}
	bool DisplayNoise(ref CloudTextureSettings settings, ref CloudNoise noise, bool open, string name)
	{
		EditorGUI.BeginChangeCheck();
		open = EditorGUILayout.BeginFoldoutHeaderGroup(open, name);
		CloudNoise.NoiseTypes noiseType = CloudNoise.GetNoiseType(noise);
		if (open)
		{
			EditorGUI.indentLevel++;
			CloudNoise.NoiseTypes newType = (CloudNoise.NoiseTypes)EditorGUILayout.EnumPopup("Noise Type", noiseType);
			if (noiseType != newType)
			{
				switch (newType)
				{
					case CloudNoise.NoiseTypes.None:
						noise = new None(noise.WriteChannel);
						break;
					case CloudNoise.NoiseTypes.PerlinWorley:
						noise = new PerlinWorley(noise.WriteChannel);
						break;
					case CloudNoise.NoiseTypes.Worley:
						noise = new Worley(noise.WriteChannel);
						break;
				}
				noiseType = CloudNoise.GetNoiseType(noise);
			}

			if (noiseType == CloudNoise.NoiseTypes.Worley)
			{
				Worley worley = noise as Worley;
				DisplayNoiseSettings("Worley Settings", ref worley.worleyNoise);
			}
			else if (noiseType == CloudNoise.NoiseTypes.PerlinWorley)
			{
				PerlinWorley perlinWorley = noise as PerlinWorley;
				DisplayNoiseSettings("Worley Settings", ref perlinWorley.worleyNoise);
				DisplayNoiseSettings("Perlin Settings", ref perlinWorley.perlinNoise);
				EditorGUILayout.Space();
				perlinWorley.perlinWorleyPersistence = EditorGUILayout.Slider(new GUIContent("PW Persistance", 
					"The percentage value between both types of noise layers"), perlinWorley.perlinWorleyPersistence, 0, 1);
			}
			EditorGUI.indentLevel--;
			EditorGUILayout.Space();
		}
		EditorGUILayout.EndFoldoutHeaderGroup();
		if (EditorGUI.EndChangeCheck() && autoUpdate)
			noise.GenNoise(settings, cloudNoiseShader);
		return open;
	}
	void DisplayNoiseSettings(string name, ref NoiseSettings settings)
	{
		EditorGUILayout.Space();
		EditorGUILayout.LabelField(name, EditorStyles.whiteLabel);
		settings.seed = EditorGUILayout.IntField("Seed", settings.seed);
		settings.scale = EditorGUILayout.IntSlider(new GUIContent("Scale",
			"Amount of divisions per axis (Grows exponentially, x ^ 3)"), settings.scale, 1, 100);
		settings.octaves = (uint)EditorGUILayout.IntSlider(new GUIContent("Octaves",
			"Essentially layers of noise"), (int)settings.octaves, 1, 10);
		settings.lacunarity = EditorGUILayout.Slider(new GUIContent("Lacunarity",
			"The amount by which the frequency of the next octave increases by"), settings.lacunarity, 1, 100);
		settings.persistance = EditorGUILayout.Slider(new GUIContent("Persistance", 
			"The percentage amplitude of the next octave relative to the prev octave"), settings.persistance, 0, 1);
		settings.invert = EditorGUILayout.Toggle("Invert", settings.invert == 1) ? 1 : 0;
	}
	void DisplayTexture(ref PreviewTexSettings previewTexSettings, ref CloudTextureSettings textureSettings)
	{
		if (textureSettings.TextureCreated)
		{
			if (textureSettings.TextureSlices != null && slicerShader)
			{
				previewTexSettings.currentSlice = EditorGUILayout.IntSlider("Current View Slice", previewTexSettings.currentSlice, 1, textureSettings.TextureSlices.Length);
				previewTexSettings.previewChannel = (ColorWriteMask)EditorGUILayout.EnumPopup("Preview Channel", previewTexSettings.previewChannel);
				previewTexSettings.transparency = EditorGUILayout.Toggle(
					new GUIContent("Transparency", "Includes alpha value when viewing Alpha and All channels (Makes sense when you use it)"),
					previewTexSettings.transparency);

				GUILayout.Space(15);
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();

				//Preview Box
				Rect rect = EditorGUILayout.GetControlRect(false, 150);
				
				if(previewTexSettings.transparency && (previewTexSettings.previewChannel == ColorWriteMask.Alpha || previewTexSettings.previewChannel == ColorWriteMask.All))
					EditorGUI.DrawTextureTransparent(rect,
						textureSettings.TextureSlices[previewTexSettings.currentSlice - 1], ScaleMode.ScaleToFit, 0, -1, previewTexSettings.previewChannel);
				else if (previewTexSettings.previewChannel != ColorWriteMask.Alpha)
					EditorGUI.DrawPreviewTexture(rect,
						textureSettings.TextureSlices[previewTexSettings.currentSlice - 1], null, ScaleMode.ScaleToFit, 0, -1, previewTexSettings.previewChannel);
				else
					EditorGUI.DrawTextureAlpha(rect,
						textureSettings.TextureSlices[previewTexSettings.currentSlice - 1], ScaleMode.ScaleToFit);

				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.Space(15);

				if (!autoUpdate)
				{
					GUILayout.BeginHorizontal();
					if (GUILayout.Button("Update"))
					{
						if (logTimes)
						{
							var timer = System.Diagnostics.Stopwatch.StartNew();

							textureSettings.GenNoise(cloudNoiseShader);
							long genTime = timer.ElapsedMilliseconds;
							Debug.Log($"Noise Generation: {genTime}ms");

							textureSettings.GenSlices(slicerShader);
							Debug.Log($"Preview Generation: {timer.ElapsedMilliseconds - genTime}ms");
						}
						else
						{
							textureSettings.GenNoise(cloudNoiseShader);
							textureSettings.GenSlices(slicerShader);
						}
					}
					if (GUILayout.Button("Destroy"))
						textureSettings.Texture.Release();
					GUILayout.EndHorizontal();
				}
			}
			else
				EditorGUILayout.HelpBox("Something is wrong with preview", MessageType.Error);
		}
		else if (cloudNoiseShader)
		{
			if (GUILayout.Button("Create"))
			{
				if (logTimes)
				{
					var timer = System.Diagnostics.Stopwatch.StartNew();

					textureSettings.Create();
					long createTime = timer.ElapsedMilliseconds;
					Debug.Log($"Texture Creation: {createTime}ms");

					textureSettings.GenNoise(cloudNoiseShader);
					long createAndGenTime = timer.ElapsedMilliseconds;
					Debug.Log($"Noise Generation: {createAndGenTime - createTime}ms");

					textureSettings.GenSlices(slicerShader);
					Debug.Log($"Preview Generation: {timer.ElapsedMilliseconds - createAndGenTime}ms");
				}
				else
				{
					textureSettings.Create();
					textureSettings.GenNoise(cloudNoiseShader);
					textureSettings.GenSlices(slicerShader);
				}
			}
		}
		else
			EditorGUILayout.HelpBox("Needs worley shader to create texture", MessageType.Error);
	}

	void SaveAndClose()
	{
		if (shapeTex.TextureCreated && detailTex.TextureCreated)
		{
			if (logTimes)
			{
				var timer = System.Diagnostics.Stopwatch.StartNew();
				shapeTex.Save("ShapeTex", false);
				detailTex.Save("DetailTex", true);
				Debug.Log($"Save Time: {timer.ElapsedMilliseconds}");
			}
			else
			{
				shapeTex.Save("ShapeTex", false);
				detailTex.Save("DetailTex", true);
			}
			Close();
		}
		else
			Debug.LogError("Create Textures First!");
	}
}

[Serializable]
public struct CloudTextureSettings
{
	public RenderTexture Texture { get; private set; }
	[Range(1, 25)]
	public int tileAmount;
	[Range(8, 7680)]
	[Tooltip("Recommended to be factor of 8")]
	public int resolution;
	public int ThreadGroups => Mathf.CeilToInt(resolution / 8f);
	public bool TextureCreated => Texture && Texture.IsCreated();

	public CloudNoise channel_R;
	public CloudNoise channel_G;
	public CloudNoise channel_B;
	public CloudNoise channel_A;

	public Texture2D[] TextureSlices { get; private set; }

	public CloudTextureSettings(int tileAmount, int resolution, CloudNoise redChannel, CloudNoise greenChannel, CloudNoise blueChannel, CloudNoise alphaChannel)
	{
		this.tileAmount = tileAmount;
		this.resolution = resolution;

		channel_R = redChannel;
		channel_G = greenChannel;
		channel_B = blueChannel;
		channel_A = alphaChannel;

		TextureSlices = null;
		Texture = null;
	}

	public bool Create()
	{
		//If the size of an already existing tex has changed, replace it. If it hasn't, then quit
		if (TextureCreated)
		{
			if (Texture.width != resolution
				|| Texture.height != resolution
				|| Texture.volumeDepth != resolution)
				Texture.Release();
			else
				return false;
		}

		//Set the values of the texture
		Texture = new RenderTexture(resolution, resolution, 0)
		{
			name = "WorleyTexture",
			graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_UNorm,
			enableRandomWrite = true,
			dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
			volumeDepth = resolution,
			wrapMode = TextureWrapMode.Repeat,
			filterMode = FilterMode.Bilinear
		};

		//Attempt to create
		if (!Texture.Create())
		{
			Debug.LogError("Unsuccesssful");
			return false;
		}
		return true;
	}
	public void GenNoise(ComputeShader shader)
	{
		CloudNoise.InitGen(this, shader);
		channel_R.GenNoise(this, shader);
		channel_G.GenNoise(this, shader);
		channel_B.GenNoise(this, shader);
		channel_A.GenNoise(this, shader);
	}
	public void GenSlices(ComputeShader slicerShader)
	{
		//Slice texture for editor preview
		TextureSlices = new Texture2D[resolution];

		slicerShader.SetTexture(0, "volumeTexture", Texture);
		int numThreads = Mathf.CeilToInt(resolution / 32f);

		var slice = new RenderTexture(resolution, resolution, 0)
		{
			dimension = TextureDimension.Tex2D,
			enableRandomWrite = true
		};
		slice.Create();

		for (int layer = 0; layer < resolution; layer++)
		{
			slicerShader.SetTexture(0, "slice", slice);
			slicerShader.SetInt("layer", layer);
			slicerShader.Dispatch(0, numThreads, numThreads, 1);

			TextureSlices[layer] = RenderTexToTex(slice);
		}
	}
	public void Save(string texName, bool detail = false)
	{
		Texture3D noiseTex = new Texture3D(resolution, resolution, resolution, TextureFormat.ARGB32, false)
		{
			filterMode = FilterMode.Trilinear
		};

		#region
		Color[] outputPixels = noiseTex.GetPixels();

		for (int layer = 0; layer < resolution; layer++)
		{
			Color[] layerPixels = TextureSlices[layer].GetPixels();
			for (int x = 0; x < resolution; x++)
				for (int y = 0; y < resolution; y++)
					outputPixels[x + resolution * (y + layer * resolution)] = layerPixels[x + y * resolution];
		}
		noiseTex.SetPixels(outputPixels);
		noiseTex.Apply();
		#endregion

		if (!AssetDatabase.IsValidFolder("Assets/Textures"))
		{
			AssetDatabase.CreateFolder("Assets", "Textures");
			AssetDatabase.CreateFolder("Assets/Textures", "Noise");
		}
		else if(!AssetDatabase.IsValidFolder("Assets/Textures/Noise"))
			AssetDatabase.CreateFolder("Assets/Textures", "Noise");

		AssetDatabase.CreateAsset(noiseTex, $"Assets/Textures/Noise/{texName}.asset");

		if (Selection.activeGameObject)
		{
			AtmosphereManager activeManager = Selection.activeGameObject.GetComponent<AtmosphereManager>();
			if (activeManager)
			{
				if (!detail && !activeManager.cloudShapeNoise)
					activeManager.cloudShapeNoise = noiseTex;
				else if (detail && !activeManager.cloudDetailNoise)
					activeManager.cloudDetailNoise = noiseTex;
			}
		}
	}

	static public Texture2D RenderTexToTex(RenderTexture rt)
	{
		Texture2D output = new Texture2D(rt.width, rt.height);
		RenderTexture.active = rt;
		output.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
		output.Apply();
		return output;
	}
}
#endif