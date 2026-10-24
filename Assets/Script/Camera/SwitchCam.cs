using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SwitchCam : MonoBehaviour
{
    public Unity.Cinemachine.CinemachineVirtualCamera cam1;
    public Unity.Cinemachine.CinemachineVirtualCamera cam2;

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            CameraManager.SwitchCamera(cam1);
        }

        if(Input.GetKeyDown(KeyCode.Alpha2))
        {
            CameraManager.SwitchCamera(cam2);
        }
    }
}
