using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.ARFoundation;

public class NavigationController : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform destination;
    public LineRenderer lineRenderer;
    public ARSessionOrigin arSessionOrigin; // Reference to ARSessionOrigin
    
    private Camera arCamera; // Add this line to reference the AR camera

    void Start()
    {
        // Disable automatic movement and rotation
        agent.updatePosition = false; 
        agent.updateRotation = false; 
        
        // Get a reference to the AR Camera within the ARSessionOrigin
        arCamera = arSessionOrigin.camera; // ARSessionOrigin provides direct access to the AR camera
    }

    void Update()
    {
        // Manually update NavMeshAgent's position based on AR Camera position
        Vector3 playerPosition = arCamera.transform.position; // Use arCamera for the position
        Quaternion playerRotation = arCamera.transform.rotation; // Use arCamera for the rotation

        // Smoothly update the NavMeshAgent to follow the AR camera position
        agent.transform.position = playerPosition;
        agent.transform.rotation = playerRotation;

        // Update the path for the navigation line
        UpdatePath();
    }

    void UpdatePath()
    {
        NavMeshPath path = new NavMeshPath();
        
        // Calculate path to the destination
        if (agent.CalculatePath(destination.position, path))
        {
            lineRenderer.positionCount = path.corners.Length; // Update line renderer with the path corners
            lineRenderer.SetPositions(path.corners); // Set positions for line renderer
        }
    }
}
