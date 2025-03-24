using System.Collections.Generic;
using UnityEngine;


public class ViewRegulation : MonoBehaviour
{
    public Material highlightMaterial;
    public Material areaMaterial;

    [SerializeField] float screenWidth = 80.0f;
    [SerializeField] float screenHeight = 80.0f;

    /// <summary> 視線の向かう先 </summary>
    [SerializeField] Vector3 endPos;
    [SerializeField] Vector3 startPos;

    [SerializeField] float lineInterval = 4;
    [SerializeField] Color lineColorValid = new Color(0, 1, 0, 0.2f);
    [SerializeField] Color lineColorInvalid = new Color(1, 0, 0, 0.2f);


    public float ScreenWidth
    {
        get => screenWidth;
        set => screenWidth = value;
    }
    public float ScreenHeight
    {
        get => screenHeight;
        set => screenHeight = value;
    }
    public Vector3 EndPos
    {
        get => endPos;
        set => endPos = value;
    }
    public Vector3 StartPos
    {
        get => startPos;
        set => startPos = value;
    }

    public Color LineColorValid
    {
        get => lineColorValid;
        set => lineColorValid = value;
    }
    public Color LineColorInvalid
    {
        get => lineColorInvalid;
        set => lineColorInvalid = value;
    }
    public float LineInterval
    {
        get => lineInterval;
        set => lineInterval = value;
    }


    public void UpdateParams(float screenWidthArg, float screenHeightArg, Vector3 endPosArg)
    {
        screenWidth = screenWidthArg;
        screenHeight = screenHeightArg;
        endPos = endPosArg;
    }

}

