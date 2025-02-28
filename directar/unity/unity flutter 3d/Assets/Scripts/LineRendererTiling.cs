using UnityEngine;

public class LineRendererTiling : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public float tilingMultiplier = 1.0f;

    void Update()
    {
        // Calculate the total length of the LineRenderer
        float totalLength = 0f;
        for (int i = 0; i < lineRenderer.positionCount - 1; i++)
        {
            totalLength += Vector3.Distance(lineRenderer.GetPosition(i), lineRenderer.GetPosition(i + 1));
        }

        // Set the tiling based on the total length and multiplier
        Material lineMaterial = lineRenderer.material;
        lineMaterial.mainTextureScale = new Vector2(totalLength * tilingMultiplier, 1);
    }
}
