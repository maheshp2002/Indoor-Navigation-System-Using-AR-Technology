using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public TMP_InputField labelInput;
    public TMP_Dropdown typeDropdown;
    public Button addButton;
    public SceneController sceneController;
    public GameObject canvasUI;

    void Start()
    {
        // Set up the button click listener
        addButton.onClick.AddListener(OnAddPointClicked);
    }

    void OnAddPointClicked()
    {
        string label = labelInput.text;
        bool isSource = typeDropdown.value == 0; // 0 = Source, 1 = Destination
        bool isDestination = typeDropdown.value == 1;

        // Pass the data to SceneController
        sceneController.CreateNavigationPoint(label, isSource, isDestination);

        // Clear the input fields
        labelInput.text = "";
        typeDropdown.value = 0;

        // Hide the UI canvas
        HideCanvas();
    }

        public void HideCanvas()
    {
        if (canvasUI != null)
        {
            canvasUI.SetActive(false);
            if (sceneController != null)
            {
                sceneController.LockInput(false); // Unlock input when hiding canvas
            }
        }
    }

    public void ShowCanvas()
    {
        if (canvasUI != null)
        {
            canvasUI.SetActive(true);
            if (sceneController != null)
            {
                sceneController.LockInput(true); // Lock input when showing canvas
            }
        }
    }
}
