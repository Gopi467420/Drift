using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public  class TouchInput : MonoBehaviour
{

    public static TouchInput Instance;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public float CalculateAngleOfTouch(Vector2 dialCenter)
    {
        Vector2 touchPosition = Touchscreen.current.primaryTouch.position.ReadValue();

        Vector2 direction = touchPosition - dialCenter;

        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }


}
