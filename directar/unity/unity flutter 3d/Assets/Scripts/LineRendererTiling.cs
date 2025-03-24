using UnityEngine;

public class LineRendererTiling : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public float tilingMultiplier = 0.5f;  // Adjust for chevron pattern size
    public float yOffset = 0.1f;  // Prevents clipping with floor

    private Material lineMaterial;

    void Start()
    {
        // Ensure we're working with a unique material instance
        lineMaterial = new Material(lineRenderer.material);
        lineRenderer.material = lineMaterial;

        AdjustYOffset(); // Apply height offset only once
    }

    void Update()
    {
        AdjustTiling();
    }

    void AdjustYOffset()
    {
        for (int i = 0; i < lineRenderer.positionCount; i++)
        {
            Vector3 pos = lineRenderer.GetPosition(i);
            pos.y += yOffset;  
            lineRenderer.SetPosition(i, pos);
        }
    }

    void AdjustTiling()
    {
        float totalLength = 0f;
        for (int i = 0; i < lineRenderer.positionCount - 1; i++)
        {
            totalLength += Vector3.Distance(lineRenderer.GetPosition(i), lineRenderer.GetPosition(i + 1));
        }

        if (lineMaterial != null)
        {
            float tiling = totalLength * tilingMultiplier;
            lineMaterial.mainTextureScale = new Vector2(tiling, 1f);

            // Ensure the chevron stays the same size by adjusting thickness
            lineMaterial.SetFloat("_Thickness", Mathf.Clamp(1.0f / tiling, 0.05f, 0.5f));
        }
    }
}
