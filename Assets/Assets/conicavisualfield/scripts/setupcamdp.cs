using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class setupcamdp : MonoBehaviour
{
    // Start is called before the first frame update
    public Camera maincamera;
    public Camera viewcamera;
    void Start()
    {
        maincamera.depthTextureMode |= DepthTextureMode.Depth;
        viewcamera.depthTextureMode |= DepthTextureMode.Depth;
    }

    // Update is called once per frame
   
}
