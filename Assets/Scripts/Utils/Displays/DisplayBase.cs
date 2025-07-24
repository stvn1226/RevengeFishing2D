using UnityEngine;
using TMPro;
using UnityEngine.Serialization;

public class DisplayBase : MonoBehaviour
{
    [SerializeField] protected bool faceCamera = true; // New option for world space displays

    [FormerlySerializedAs("fatigueDisplay")] [SerializeField] protected TextMeshProUGUI displayText;
    [SerializeField] protected Camera mainCamera;

   
    protected virtual void Start()
    {
        if (TestingManager.instance.isTesting)
        {
            displayText = GetComponent<TextMeshProUGUI>();

            // Get camera reference for world space displays
            if (faceCamera)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                    mainCamera = FindObjectOfType<Camera>();
            }
        }
    }

    protected virtual void Update()
    {
        if (TestingManager.instance.isTesting)
        {
            BaseResponsability();
            
            if (faceCamera && mainCamera != null && GetComponentInParent<Canvas>()?.renderMode == RenderMode.WorldSpace)
            {
                transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                    mainCamera.transform.rotation * Vector3.up);
            }
        }
    }

    protected virtual void BaseResponsability()
    {
        
    }
}
