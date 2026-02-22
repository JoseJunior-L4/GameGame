using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Super Smash Bros style camera that dynamically frames all active players
/// Automatically adjusts zoom and position to keep all players visible
/// </summary>
public class SmashCameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float minSize = 5f;
    [SerializeField] private float maxSize = 20f;
    [SerializeField] private float edgePadding = 2f;
    
    [Header("Camera Bounds")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private Vector2 minPosition = new Vector2(-50f, -10f);
    [SerializeField] private Vector2 maxPosition = new Vector2(50f, 30f);
    
    [Header("Depth Settings")]
    [SerializeField] private float cameraDepth = -10f;

    [SerializeField]
    private List<Transform> trackedPlayers = new List<Transform>();
    private Vector3 targetPosition;
    private float targetSize;
    
    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = GetComponent<Camera>();
        }
        
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }
    
    private void LateUpdate()
    {
        if (trackedPlayers.Count == 0) return;
        
        // Remove null references (dead players)
        trackedPlayers.RemoveAll(p => p == null);
        
        if (trackedPlayers.Count == 0) return;
        
        UpdateCameraPosition();
        UpdateCameraSize();
        ApplyCameraTransform();
    }
    
    /// <summary>
    /// Subscribe a player to camera tracking
    /// </summary>
    public void SubscribePlayer(Transform playerTransform)
    {
        if (playerTransform != null && !trackedPlayers.Contains(playerTransform))
        {
            trackedPlayers.Add(playerTransform);
            Debug.Log($"Player {playerTransform.name} subscribed to camera. Total players: {trackedPlayers.Count}");
        }
    }
    
    /// <summary>
    /// Unsubscribe a player from camera tracking (called on death)
    /// </summary>
    public void UnsubscribePlayer(Transform playerTransform)
    {
        if (trackedPlayers.Contains(playerTransform))
        {
            trackedPlayers.Remove(playerTransform);
            Debug.Log($"Player {playerTransform.name} unsubscribed from camera. Total players: {trackedPlayers.Count}");
        }
    }
    
    /// <summary>
    /// Calculate the center point of all tracked players
    /// </summary>
    private void UpdateCameraPosition()
    {
        Vector3 centerPoint = Vector3.zero;
        
        foreach (Transform player in trackedPlayers)
        {
            if (player != null)
            {
                centerPoint += player.position;
            }
        }
        
        centerPoint /= trackedPlayers.Count;
        
        // Apply bounds if enabled
        if (useBounds)
        {
            centerPoint.x = Mathf.Clamp(centerPoint.x, minPosition.x, maxPosition.x);
            centerPoint.y = Mathf.Clamp(centerPoint.y, minPosition.y, maxPosition.y);
        }
        
        centerPoint.z = cameraDepth;
        targetPosition = centerPoint;
    }
    
    /// <summary>
    /// Calculate the required camera size to fit all players
    /// </summary>
    private void UpdateCameraSize()
    {
        if (trackedPlayers.Count == 1)
        {
            targetSize = minSize;
            return;
        }
        
        Bounds bounds = new Bounds(trackedPlayers[0].position, Vector3.zero);
        
        foreach (Transform player in trackedPlayers)
        {
            if (player != null)
            {
                bounds.Encapsulate(player.position);
            }
        }
        
        // Calculate required size based on bounds
        float verticalSize = bounds.size.y / 2f + edgePadding;
        float horizontalSize = bounds.size.x / mainCamera.aspect / 2f + edgePadding;
        
        targetSize = Mathf.Max(verticalSize, horizontalSize);
        targetSize = Mathf.Clamp(targetSize, minSize, maxSize);
    }
    
    /// <summary>
    /// Smoothly apply the calculated camera position and size
    /// </summary>
    private void ApplyCameraTransform()
    {
        // Smooth position
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * smoothSpeed
        );
        
        // Smooth orthographic size (zoom)
        if (mainCamera.orthographic)
        {
            mainCamera.orthographicSize = Mathf.Lerp(
                mainCamera.orthographicSize,
                targetSize,
                Time.deltaTime * smoothSpeed
            );
        }
        else
        {
            // For perspective camera, adjust distance instead
            Vector3 currentPos = transform.position;
            currentPos.z = Mathf.Lerp(
                currentPos.z,
                cameraDepth - targetSize,
                Time.deltaTime * smoothSpeed
            );
            transform.position = currentPos;
        }
    }
    
    /// <summary>
    /// Get the number of currently tracked players
    /// </summary>
    public int GetTrackedPlayerCount()
    {
        trackedPlayers.RemoveAll(p => p == null);
        return trackedPlayers.Count;
    }
    
    /// <summary>
    /// Clear all tracked players
    /// </summary>
    public void ClearAllPlayers()
    {
        trackedPlayers.Clear();
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        
        // Draw camera bounds
        Gizmos.color = Color.yellow;
        Vector3 bottomLeft = new Vector3(minPosition.x, minPosition.y, 0);
        Vector3 bottomRight = new Vector3(maxPosition.x, minPosition.y, 0);
        Vector3 topLeft = new Vector3(minPosition.x, maxPosition.y, 0);
        Vector3 topRight = new Vector3(maxPosition.x, maxPosition.y, 0);
        
        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
    }
}
