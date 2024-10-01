using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI; // For NavMeshAgent
using UnityEngine.XR.ARFoundation; // For AR session components

public class NavigationController : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform destination;
    public LineRenderer lineRenderer;
    public ARSessionOrigin arSessionOrigin; // Reference to ARSessionOrigin

    void Start()
    {
        agent.updatePosition = false; // Disable automatic movement
        agent.updateRotation = false; // Disable automatic rotation
    }

    void Update()
    {
        // Manually update NavMeshAgent's position based on AR Camera position
        Vector3 playerPosition = arSessionOrigin.camera.transform.position; // Get AR Camera position
        agent.Warp(playerPosition); // Move agent to the AR camera's position
        
        UpdatePath();
    }

    void UpdatePath()
    {
        NavMeshPath path = new NavMeshPath();
        if (agent.CalculatePath(destination.position, path))
        {
            lineRenderer.positionCount = path.corners.Length;
            lineRenderer.SetPositions(path.corners);
        }
    }
}
