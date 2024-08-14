/*
 * Copyright (r) Hubbahu
 */

using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

[RequireComponent(typeof(Volume))]
public class DynamicDOF : MonoBehaviour
{
    public GameObject focusObject;

    float hitDistance;
    public LayerMask focusableLayers;

    public float maxDistance = 5;
    public float focusSpeed = 8;
    DepthOfField depthOfField;

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Volume>().profile.TryGet(out depthOfField);
    }

    // Update is called once per frame
    void Update()
    {
        if (!depthOfField)
            return;
        if (focusObject)
        {
            hitDistance = Vector3.Distance(Camera.main.transform.position, focusObject.transform.position);
            if (hitDistance > maxDistance)
                hitDistance = maxDistance;    
        }
        else
        {
            Ray visionRay = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            if (Physics.Raycast(visionRay, out RaycastHit raycastHit, maxDistance, focusableLayers))
                hitDistance = Vector3.Distance(Camera.main.transform.position, raycastHit.point);
            else if (hitDistance < maxDistance)
                hitDistance++;
        }

        SetFocusDistance(hitDistance);
    }

    void SetFocusDistance(float distance)
	{
        depthOfField.focusDistance.value = Mathf.Lerp(depthOfField.focusDistance.value, distance, Time.deltaTime * focusSpeed);
	}
}
