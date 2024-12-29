using UnityEngine;

public class NavigationPoint : MonoBehaviour
{
    public string Label { get; private set; }
    public bool IsSource { get; private set; }
    public bool IsDestination { get; private set; }

    // Method to set data for navigation points
    public void SetData(string label, bool isSource, bool isDestination)
    {
        this.Label = label;         
        this.IsSource = isSource;    
        this.IsDestination = isDestination; 
    }
}
