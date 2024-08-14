/*
 * Copyright (c) Hubbahu
 */
using System;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public struct NoiseSettings
{
	public int seed;
	public int scale;
	public uint octaves;
	public float lacunarity;
	public float persistance;
	public int invert;

	public NoiseSettings(int seed, int scale, uint octaves, float lacunarity, float persistance, bool invert = false)
	{
		this.seed = seed;
		this.scale = scale;
		this.octaves = octaves;
		this.lacunarity = lacunarity;
		this.persistance = persistance;
		this.invert = invert ? 1 : 0;
	}
}

public class Worley : CloudNoise
{
	public NoiseSettings worleyNoise;

	public Worley(ColorWriteMask writeChannel)
	{
		if (prng != null)
			prng = new System.Random();
		worleyNoise = new NoiseSettings(prng.Next(-10000, 10000), 10, 4, 2, .5f, true);
		WriteChannel = writeChannel;
	}
	public Worley(NoiseSettings worleyNoise, ColorWriteMask writeChannel)
	{
		this.worleyNoise = worleyNoise;
		WriteChannel = writeChannel;
	}

	public override void GenNoise(CloudTextureSettings writeTex, ComputeShader worleyShader)
	{
		//Find kernel
		int kernelIndex = worleyShader.FindKernel("WorleyCompute");

		//Set texture variables
		worleyShader.SetTexture(kernelIndex, "result", writeTex.Texture);
		worleyShader.SetVector("writeMask", WriteMask);

		//Set worley values
		ComputeBuffer worleyLayers = new ComputeBuffer(1, 24);
		worleyLayers.SetData(new NoiseSettings[] { worleyNoise });
		worleyShader.SetBuffer(kernelIndex, "noiseSettings", worleyLayers);

		//Dispatch threads
		worleyShader.Dispatch(kernelIndex, writeTex.ThreadGroups, writeTex.ThreadGroups, writeTex.ThreadGroups);

		//Release Buffer
		worleyLayers.Release();
	}
}
public class PerlinWorley : CloudNoise
{
	public NoiseSettings worleyNoise;
	public NoiseSettings perlinNoise;
	[Tooltip("Applies persistence to combine both perlin and worley noises")]
	[Range(0, 1)]
	public float perlinWorleyPersistence;

	public PerlinWorley(ColorWriteMask writeChannel)
	{
		if (prng == null)
			prng = new System.Random();
		worleyNoise = new NoiseSettings(prng.Next(-10000, 10000), 10, 4, 2, .5f, true);
		perlinNoise = new NoiseSettings(prng.Next(-10000, 10000), 3, 4, 2, .5f, true);
		perlinWorleyPersistence = .5f;
		WriteChannel = writeChannel;
	}
	public PerlinWorley(NoiseSettings worleyNoise, NoiseSettings perlinNoise, float perlinWorleyPersistence, ColorWriteMask writeChannel)
	{
		this.worleyNoise = worleyNoise;
		this.perlinNoise = perlinNoise;
		this.perlinWorleyPersistence = perlinWorleyPersistence;
		WriteChannel = writeChannel;
	}

	public override void GenNoise(CloudTextureSettings writeTex, ComputeShader perlinWorleyShader)
	{
		//Find kernel
		int kernelIndex = perlinWorleyShader.FindKernel("PerlinWorleyCompute");

		//Set texture variables
		perlinWorleyShader.SetTexture(kernelIndex, "result", writeTex.Texture);
		perlinWorleyShader.SetVector("writeMask", WriteMask);

		//Set perlinWorley values
		ComputeBuffer worleyLayer = new ComputeBuffer(2, 24);
		worleyLayer.SetData(new NoiseSettings[] { worleyNoise, perlinNoise });
		perlinWorleyShader.SetBuffer(kernelIndex, "noiseSettings", worleyLayer);
		perlinWorleyShader.SetFloat("perlinWorleyPersistence", perlinWorleyPersistence);

		//Dispatch threads
		perlinWorleyShader.Dispatch(kernelIndex, writeTex.ThreadGroups, writeTex.ThreadGroups, writeTex.ThreadGroups);

		worleyLayer.Release();
	}
}
public class None : CloudNoise
{
	public None(ColorWriteMask writeChannel)
	{
		WriteChannel = writeChannel;
	}

	public override void GenNoise(CloudTextureSettings writeTex, ComputeShader clear)
	{
		//Find kernel
		int kernelIndex = clear.FindKernel("Clear");

		//Set texture variables
		clear.SetVector("writeMask", WriteMask);
		clear.SetTexture(kernelIndex, "result", writeTex.Texture);

		//Dispatch threads
		clear.Dispatch(kernelIndex, writeTex.ThreadGroups, writeTex.ThreadGroups, writeTex.ThreadGroups);
	}
}

public abstract class CloudNoise
{
	public static System.Random prng;
	public enum NoiseTypes { None, PerlinWorley, Worley }
	public ColorWriteMask WriteChannel { get; protected set; }
	public Vector4 WriteMask
	{
		get {
			switch (WriteChannel)
			{
				case ColorWriteMask.Alpha:
					return new Vector4(0, 0, 0, 1);
				case ColorWriteMask.Blue:
					return new Vector4(0, 0, 1, 0);
				case ColorWriteMask.Green:
					return new Vector4(0, 1, 0, 0);
				case ColorWriteMask.Red:
					return new Vector4(1, 0, 0, 0);
				case ColorWriteMask.All:
					return Vector4.one;
				default:
					return Vector4.zero;
			}
		}
	}

	abstract public void GenNoise(CloudTextureSettings writeTex, ComputeShader perlinWorleyShader);

	public static NoiseTypes GetNoiseType(CloudNoise noise)
	{
		if (noise == null || noise is None)
			return NoiseTypes.None;
		else if (noise is PerlinWorley)
			return NoiseTypes.PerlinWorley;
		else if (noise is Worley)
			return NoiseTypes.Worley;
		return NoiseTypes.None;
	}
	public static void InitGen(CloudTextureSettings settings, ComputeShader noiseShader)
	{
		noiseShader.SetInt("resolution", settings.resolution);
		noiseShader.SetInt("tileAmount", settings.tileAmount);
	}
}