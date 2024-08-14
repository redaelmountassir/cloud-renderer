/*
 * Copyright (c) Hubbahu
 */

using UnityEngine;

public abstract class Noise : ScriptableObject
{
    public abstract void Generate(float dimensions);

    public abstract float Sample1D(float x);
    public abstract float Sample2D(float x, float y);
    public abstract float Sample2D(Vector2 pos);
    public abstract float Sample3D(float x, float y, float z);
    public abstract float Sample3D(Vector3 pos);
}
