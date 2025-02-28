using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class NavMeshAgentVisualizer_AR : MonoBehaviour
{
    private NavMeshAgent agent;
    private LineRenderer lineRenderer;
    private int segments = 20; // Number of segments for the circle

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent not found!");
            return;
        }

        // Create a LineRenderer
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.02f;
        lineRenderer.endWidth = 0.02f;
        lineRenderer.positionCount = (segments + 1) * 2; // Bottom and top circles
        lineRenderer.loop = false;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // Basic material
        lineRenderer.startColor = Color.green;
        lineRenderer.endColor = Color.green;
    }

    void Update()
    {
        if (agent == null) return;

        float radius = agent.radius;
        float height = agent.height;
        Vector3 basePosition = transform.position;
        Vector3 topPosition = basePosition + Vector3.up * height;

        List<Vector3> points = new List<Vector3>();

        // Draw bottom circle
        for (int i = 0; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            points.Add(basePosition + new Vector3(x, 0, z));
        }

        // Draw top circle
        for (int i = 0; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            points.Add(topPosition + new Vector3(x, 0, z));
        }

        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
    }
}
