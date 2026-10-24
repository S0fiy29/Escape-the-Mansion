using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CameraManager : MonoBehaviour
{
    static List<Unity.Cinemachine.CinemachineVirtualCamera> cameras = new List<Unity.Cinemachine.CinemachineVirtualCamera>();

    public static Unity.Cinemachine.CinemachineVirtualCamera ActiveCamera = null;

    public static bool IsActiveCamera(Unity.Cinemachine.CinemachineVirtualCamera camera)
    {
        return camera == ActiveCamera;
    }

    public static void SwitchCamera(Unity.Cinemachine.CinemachineVirtualCamera newCamera)
    {
        newCamera.Priority = 10;
        ActiveCamera = newCamera;

        foreach(Unity.Cinemachine.CinemachineVirtualCamera cam in cameras)
        {
            if(cam != newCamera)
            {
                cam.Priority = 0;
            }
        }
    }

    public static void Register(Unity.Cinemachine.CinemachineVirtualCamera camera)
    {
        cameras.Add(camera);
    }

    public static void Unregister(Unity.Cinemachine.CinemachineVirtualCamera camera)
    {
        cameras.Remove(camera);
    }
}
