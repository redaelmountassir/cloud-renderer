/*
 * Copyright (c) Hubbahu
 */

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[AddComponentMenu("Enviroment/Atmosphere Manager")]
[ExecuteAlways]
[SelectionBase]
public class AtmosphereManager : MonoBehaviour
{
    static public AtmosphereManager instance;

    [Header("Clouds")]
    [Tooltip("Recommended to be a box.")]
    [ContextMenuItem("Update Bounds", "UpdateBounds")]
    [SerializeField]
    MeshFilter cloudMesh;
    public MeshFilter CloudMesh
	{
        get => cloudMesh;
		set
		{
            if (value)
                cloudRenderer = value.gameObject.GetComponent<MeshRenderer>();
            if (cloudRenderer)
                cloudMesh = value;
		}
	}
    [HideInInspector]
    public MeshRenderer cloudRenderer;
    [ContextMenuItem("Create Noise", "OpenNoiseGenerator")]
    public Texture3D cloudShapeNoise;
    public float shapeScale = 1;
    [ContextMenuItem("Create Noise", "OpenNoiseGenerator")]
    public Texture3D cloudDetailNoise;
    public float detailScale = 1;
    public float cloudCoverage = .5f;
    [Range(0f, 1f)]
    public float erodeStrength = 1;
    [Tooltip("The the size of the steps the raymarch ray will move by across the container")]
    [Range(0.0001f, 50f)]
    public float stepSize = 1;
    [Range(1f, 10f)]
    [Tooltip("The stepSize will be scaled by this amount until it hits the isosurface if the cloud")]
    public float cheapScale = 2;
    [Tooltip("Applying noise (especially Blue Noise) to add offset to raymarching steps allows for less steps, for the same amount of fidelity")]
    public Texture2D noiseTex; 
    [Range(0, 1)]
    [Tooltip("The strength of the noise")]
    public float stepRandomness = 1;
    Material cloudMaterial;

    [Header("Cloud Lighting")]
    [Tooltip("The light for the cloud's shading to use")]
    public Light mainLight;
    [HideInInspector]
    public HDAdditionalLightData mainLight_AD;
    [Tooltip("The time value is used to sample this gradient which then is mixed with the cloud color (for day)")]
    public Gradient dayGradient;
    [Tooltip("The time value is used to sample this gradient which then is mixed with the cloud color (for day)")]
    public Gradient nightGradient;
    [Tooltip("Value used to sample the gradient")]
    [Range(0, 24)]
    public float time;
    [Tooltip("Multiplies the 0 - 8 intensity value of the light before multiplying it by the light color")]
    public float lightIntensity = 1;
    [Tooltip("Multiplies density before applying Beer's Law and the Powdered Sugar Look")]
    public float lightAbsorbtion = 0;
    [Tooltip("Multiplies density before applying Henyey-Greenstein's Phase Function (light scattering)")]
    public float hgMult = 1;
    [Tooltip("-1 is back scattering, 0 is isotropic scattering, and 1 is forward scattering")]
    [Range(-1, 1)]
    public float hgScattering;
    public Vector3 FinalCondensedMods => new Vector2(lightAbsorbtion, hgMult);

    [Header("Weather")]
    [Tooltip("How fast should the wind change directions")]
    public float windChangeRareness = 1000;
    [Tooltip("Speed of details relative to shape")]
    public float detailWindMult = 1f;
    Vector3 mainOffset;
    Vector3 detailOffset;
    public float windSpeed;
    public Vector3 WindDir => new Vector3(Mathf.PerlinNoise(Time.time / windChangeRareness, 0) * 2 - 1, 0, Mathf.PerlinNoise(0, Time.time / windChangeRareness) * 2 - 1).normalized;
    public float cloudTypeScale;
    [Tooltip("This gradient is applied to the y axis of the cloud container bounds in the shader to condense the area the clouds take up to a specific area.")]
    public BoxGradient cloudGrad = new BoxGradient(.25f, .1f, 25);

    [Header("Lightning")]
    [Tooltip("The amount of distance the lightning reaches before creating a new vertice")]
    public float lightningStepDistance = 1;
    [Tooltip("Number will be used as the min and max for offset in the direction of the goal")]
    public float offsetY = 1;
    [Tooltip("Number will be used as the min and max for offset in the directions perpendicular to the direction of the goal")]
    public float offsetXZ = 1;
    [Tooltip("Layermask that stops lightning from going any further to its goal")]
    public LayerMask terminationMask;
    public Material lightningMaterial;
    public float lightningWidth = .1f;
    [Tooltip("GameObject (usually particles) spawned at lightning spawn point")]
    public GameObject flash;
    [Tooltip("GameObject (usually particles) spawned at lightning contact point (only spawned on physics bolts that hit a surface)")]
    public GameObject contact;

    void Start()
    {
        if (instance)
		{
            Destroy(this);
            print("Multiple Instances Made!");
		}
        else
            instance = this;

        UpdateCloudMat();
    }

    void OnValidate() => UpdateCloudMat();
	private void Update()
	{
        Vector3 offset = WindDir * windSpeed * Time.deltaTime;
        mainOffset += offset;
        detailOffset += offset * detailWindMult;

        //Basic material checks first
        if (!cloudMesh)
            return;
        else if (!cloudRenderer)
            cloudRenderer = cloudMesh.gameObject.GetComponent<MeshRenderer>();
        if (!cloudMaterial)
        {
            cloudMaterial = new Material(Shader.Find("Hidden/CloudShader"))
            { hideFlags = HideFlags.HideAndDontSave };
            UpdateCloudMat();
        }

        cloudMaterial.SetVector("MainOffset", mainOffset);
        cloudMaterial.SetVector("DetailOffset", detailOffset);
    }
	void OnDestroy()
    {
        if (cloudRenderer)
		{
            if (Application.isEditor)
			    DestroyImmediate(cloudRenderer.sharedMaterial);
            else
                Destroy(cloudRenderer.sharedMaterial);
        }
    }

    [ContextMenu("Update Material")]
    /// <summary>
    /// Updates shader variables with values that should stay constant at runtime. 
    /// Should not be in update loop ideally.
    /// </summary>
    public void UpdateCloudMat()
	{
        //Basic material checks first
        if (!cloudMesh)
            return;
        else if (!cloudRenderer)
            cloudRenderer = cloudMesh.gameObject.GetComponent<MeshRenderer>();

        if (!cloudMaterial)
		{
			cloudMaterial = new Material(Shader.Find("Hidden/CloudShader"))
			{ hideFlags = HideFlags.HideAndDontSave };
		}

        //Set all shader values
        UpdateBounds();
        UpdateLightVars();

        cloudMaterial.SetTexture("ShapeTex", cloudShapeNoise);
        cloudMaterial.SetFloat("ShapeScale", shapeScale);
        cloudMaterial.SetTexture("DetailTex", cloudDetailNoise);
        cloudMaterial.SetFloat("DetailScale", detailScale);
        cloudMaterial.SetTexture("NoiseTex", noiseTex);

        cloudMaterial.SetVector("CloudGrad", cloudGrad.CondenseValues);

        cloudMaterial.SetFloat("Coverage", cloudCoverage);
        cloudMaterial.SetFloat("ErodeStrength", erodeStrength);
        cloudMaterial.SetFloat("StepSize", stepSize);
        cloudMaterial.SetFloat("CheapScale", cheapScale);
        cloudMaterial.SetFloat("StepRandomness", stepRandomness);

        cloudMaterial.SetVector("DensityMods", FinalCondensedMods);
        cloudMaterial.SetFloat("Scattering", hgScattering);

        if (cloudRenderer.sharedMaterial != cloudMaterial)
            cloudRenderer.sharedMaterial = cloudMaterial;
	}
    public void SetLightVars(Light light, HDAdditionalLightData additionalLightData, float time)
    {
        this.time = time;
        mainLight = light;
        mainLight_AD = additionalLightData;
        UpdateLightVars();
    }
    public void UpdateLightVars()
    {
        //Basic material checks first
        if (!cloudMesh || !mainLight || !mainLight_AD)
            return;
        else if (!cloudRenderer)
            cloudRenderer = cloudMesh.gameObject.GetComponent<MeshRenderer>();

        if (!cloudMaterial)
        {
            cloudMaterial = new Material(Shader.Find("Hidden/CloudShader"))
            { hideFlags = HideFlags.HideAndDontSave };
            UpdateCloudMat();
        }

        cloudMaterial.SetVector("LightDir", mainLight.transform.forward);
        Color timeOfDayGradient = time >= 6 && time < 18 ? dayGradient.Evaluate((time - 6)/12) : nightGradient.Evaluate(time >= 18 ? (time - 18) / 12 : (time + 6) / 12);
        Color lightColor = Mathf.CorrelatedColorTemperatureToRGB(mainLight.colorTemperature) * mainLight.color * timeOfDayGradient;
        //alpha converts to lightntensity 
        lightColor.a = Mathf.Clamp01(mainLight.intensity * lightIntensity);
        cloudMaterial.SetColor("LightColor", lightColor);
    }
    [ContextMenu("Update Bounds")]
    public void UpdateBounds()
	{
        cloudMaterial.SetVector("MinBounds", cloudMesh.sharedMesh.bounds.min);
        cloudMaterial.SetVector("MaxBounds", cloudMesh.sharedMesh.bounds.max);
    }

	public void OpenNoiseGenerator() => CloudNoiseGen.ShowWindow();

	public bool SpawnPhysicsLighting(Vector3 startPoint, Vector3 endPoint, LayerMask terminationMask, out RaycastHit[] hits)
	{
        //Prevent negative or less than zero movement or else infinite loop.
        if (lightningStepDistance <= 0)
        {
            print("Lightning Step Distance should be more than 0");
            hits = null;
            return false;
        }

        //Replace with pool
        //Intialize item with line renderer and position
        Transform lightTransform = new GameObject("LightningInstance").transform;
        LineRenderer line = lightTransform.gameObject.AddComponent<LineRenderer>();
        //Set the gameObject to point towards the destination for offset math
        lightTransform.rotation = Quaternion.LookRotation(startPoint - endPoint) * Quaternion.Euler(90, 0, 0);
        line.material = lightningMaterial;
        line.widthMultiplier = lightningWidth;
        line.textureMode = LineTextureMode.DistributePerSegment;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        //Spawn flash
        if (flash)
            Instantiate(flash, startPoint, Quaternion.identity);

        //Create list and add starting point
        List<Vector3> points = new List<Vector3>() { startPoint };
        List<RaycastHit> collisions = new List<RaycastHit>();

        //Initialize loop variables
        Vector3 currentPoint = startPoint;
        //Save the points with offsets seperately so that raycast can be casted
        Vector3 prevPointOffset = startPoint, currPointOffset;
		while (prevPointOffset != endPoint)
        {
            //move a little bit closer to the goal but not close enough so a vertice can be created
            currentPoint = Vector3.MoveTowards(currentPoint, endPoint, lightningStepDistance);

            //Add offset in directions parallel to the direction and perpendicular to the direction if goal has not been reached
            currPointOffset = currentPoint == endPoint ? currentPoint :
                currentPoint
                + UnityEngine.Random.Range(-offsetXZ, offsetXZ) * lightTransform.right
                + UnityEngine.Random.Range(-offsetY, offsetY) * lightTransform.up
                + UnityEngine.Random.Range(-offsetXZ, offsetXZ) * lightTransform.forward;

            //Adds logic to the lightning (collisions and terminations)
            if (Physics.Linecast(prevPointOffset, currPointOffset, out RaycastHit hit))
			{
                //if collided, add collision object for external use
                collisions.Add(hit);
                //if collided object is a termination layer, terminate the lightning
                if (terminationMask == (terminationMask | (1 << hit.transform.gameObject.layer)))
				{
                    points.Add(hit.point);
                    //Spawn contact
                    if (contact)
                        Instantiate(contact, hit.point, Quaternion.identity);
                    break;
				}
			}
            
            //Add next point plus some offset
            points.Add(currPointOffset);
            prevPointOffset = currPointOffset;
        }

        //Add vertices
        line.positionCount = points.Count;
        line.SetPositions(points.ToArray());
        //Convert collisions to array
        hits = collisions.ToArray();

        return hits != null;
    }
    public void SpawnVisualLighting(Vector3 startPoint, Vector3 endPoint)
    {
        //Prevent negative or less than zero movement or else infinite loop.
        if(lightningStepDistance <= 0)
		{
            print("Lightning Step Distance should be more than 0");
            return;
		}

        //Replace with pool
        //Intialize item with line renderer and position
        Transform lightTransform = new GameObject("LightningInstance").transform;
        LineRenderer line = lightTransform.gameObject.AddComponent<LineRenderer>();
        //Set the gameObject to point towards the destination for offset maths
        lightTransform.rotation = Quaternion.LookRotation(startPoint - endPoint) * Quaternion.Euler(90,0,0);
        line.material = lightningMaterial;
        line.widthMultiplier = lightningWidth;
        line.textureMode = LineTextureMode.DistributePerSegment;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        //Spawn flash
        if (flash)
            Instantiate(flash, startPoint, Quaternion.identity);

        //Create list and add starting point
        List<Vector3> points = new List<Vector3>() { startPoint };

		//Initialize loop variables
		Vector3 currentPoint = points[0];
        while (true)
		{
            //move a little bit closer to the goal but not close enough so a vertice can be created
            currentPoint = Vector3.MoveTowards(currentPoint, endPoint, lightningStepDistance);

            //if there is no more distance to go
            if (currentPoint == endPoint)
			{
                //Do not add offset to destination or else destination will not technically be reached
                points.Add(currentPoint);
                break;
			}
            //Add next point plus some offset
            points.Add(currentPoint
                + UnityEngine.Random.Range(-offsetXZ, offsetXZ) * lightTransform.right
                + UnityEngine.Random.Range(-offsetY, offsetY) * lightTransform.up
                + UnityEngine.Random.Range(-offsetXZ, offsetXZ) * lightTransform.forward);
		}

        //Add vertices
        line.positionCount = points.Count;
        line.SetPositions(points.ToArray());
    }
}

[Serializable]
public struct BoxGradient
{
    [Range(0, 1)]
    public float center;
    [Range(0, 1)]
    public float plateauSize;
    public float steepness;

    public Vector3 CondenseValues => new Vector3(center, plateauSize, steepness);

    public float Sample(float center, float plateauSize, float steepness, float x)
    {
        return Mathf.Clamp01((1 - Mathf.Abs(x - center) * steepness) + plateauSize);
    }
    public BoxGradient(float center, float plateauSize, float steepness)
	{
		this.center = center;
		this.plateauSize = plateauSize;
		this.steepness = steepness;
	}
}

[CustomEditor(typeof(AtmosphereManager))]
class AtmosphereEditor : Editor
{
	private void OnSceneGUI()
	{
        Bounds cloudBounds = ((AtmosphereManager)target).cloudRenderer.bounds;
        Handles.color = Color.black;
        Handles.DrawWireCube(cloudBounds.center, cloudBounds.size);

        Handles.color = Color.cyan;
        Handles.ArrowHandleCap(1, cloudBounds.center, Quaternion.LookRotation(-((AtmosphereManager)target).WindDir), cloudBounds.size.x / 3, EventType.Repaint);
    }
}