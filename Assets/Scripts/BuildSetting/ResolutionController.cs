using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResolutionController : MonoBehaviour
{
    void Start()
    {
        // 창 모드에서 720x1280 해상도 적용
        int width = 720;
        int height = 1280;
        Screen.SetResolution(width, height, false);
    }
}
