using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class TouchScreenRaycast : MonoBehaviour
{
    public float maxDistance = 100f;
    public LayerMask hitMask = Physics.DefaultRaycastLayers;

    [SerializeField] Camera mainCamera;
    [SerializeField] LightManager LightManager;

    [SerializeField] float circleSize = 1;

    List<int> pressedLastFrame = new List<int>();
    List<int> pressedThisFrame = new List<int>();

    void Update()
    {
        pressedLastFrame.Clear();
        pressedLastFrame.AddRange(pressedThisFrame);
        pressedThisFrame.Clear();
        int count = Input.touchCount;

        if (count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                Touch t = Input.GetTouch(i);
                CircleRaycast(t.position);
            }
        }
        if (Input.GetMouseButton(0))
        {
            CircleRaycast(Input.mousePosition);
        }

        foreach(int i in pressedThisFrame)
        {
            if(pressedLastFrame.Contains(i))
            {
                pressedLastFrame.Remove(i);
            }
        }

        foreach(int i in pressedLastFrame)
        {
            TouchManager.SetTouch(i, false);
            LightManager.UpdateFadeLight(i, false);
        }

    }

    private void CircleRaycast(Vector2 screenPos)
    {
        float displayScale = Display.main.renderingHeight;
        float newScale = displayScale/circleSize;
        DoRaycast(screenPos + (Vector2.left * newScale));
        DoRaycast(screenPos + (Vector2.up * newScale));
        DoRaycast(screenPos + (Vector2.down * newScale));
        DoRaycast(screenPos + (Vector2.right * newScale));
        DoRaycast(screenPos);
    }


    private void DoRaycast(Vector2 screenPos)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, hitMask, QueryTriggerInteraction.Collide))
        {
            //Debug.Log($"Hit {hit.collider.name} at {hit.point}");
            try
            {
                var Area = Convert.ToInt32(hit.collider.name);

                if(!pressedThisFrame.Contains(Area))
                    pressedThisFrame.Add(Area);

                TouchManager.SetTouch(Area, true);
                LightManager.UpdateFadeLight(Area, true);
            }
            catch
            {

            }
        }
    }
}
