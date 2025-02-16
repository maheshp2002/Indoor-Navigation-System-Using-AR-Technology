using UnityEngine;
using FlutterUnityIntegration;
public class CanvasManager : MonoBehaviour
{
    public GameObject adminUI; // Canvas for Admin
    public GameObject xrOrigin;  // XR Origin for User (previously userUI)
    public GameObject arsession;
    public GameObject xrInteractionManager;
    [SerializeField] private UnityMessageSender unityMessageSender;
    public void SetMode(bool isAdmin)
    {
        // Toggle admin UI
        adminUI.SetActive(isAdmin);

        // Toggle XR Origin
        xrOrigin.SetActive(!isAdmin);

        // Optionally disable XR subsystems explicitly if needed
        if (!isAdmin)
        {
            EnableXRSubsystems();
        }
        else
        {
            DisableXRSubsystems();
        }
    }

    private void EnableXRSubsystems()
    {
        var xrManager = xrOrigin.GetComponent<UnityEngine.XR.Management.XRGeneralSettings>();
        if (xrManager != null && xrManager.Manager != null)
        {
            xrManager.Manager.StartSubsystems();
        }
    }

    private void DisableXRSubsystems()
    {
        var xrManager = xrOrigin.GetComponent<UnityEngine.XR.Management.XRGeneralSettings>();
        if (xrManager != null && xrManager.Manager != null)
        {
            xrManager.Manager.StopSubsystems();
        }
    }
}
