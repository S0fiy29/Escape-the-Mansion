using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CameraRegister : MonoBehaviour
{
    private void OnEnable()
    {
        CameraManager.Register(GetComponent<Unity.Cinemachine.CinemachineVirtualCamera>());
    }

    private void OnDisable()
    {
        CameraManager.Unregister(GetComponent<Unity.Cinemachine.CinemachineVirtualCamera>());
    }
}
