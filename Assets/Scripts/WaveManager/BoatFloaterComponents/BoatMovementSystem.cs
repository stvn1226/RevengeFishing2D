using System.Collections;
using UnityEngine;

public enum BoatMovementState
{
    AutoMove,
    Driven,
}

public class BoatMovementSystem : MonoBehaviour
{
    private const float DRIVEN_SPEED = 60;
    private const float AUTOMOVE_SPEED = 2;
    
    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 2f;
    [SerializeField] private float maxMovementForce = 6f;
    [SerializeField] private bool enableAutomaticMovement = true;
    [SerializeField] private bool debugMovement = false;
    
    [Header("Boundaries")]
    [SerializeField] private Transform leftBoundary;
    [SerializeField] private Transform rightBoundary;
    [SerializeField] private float boundaryBuffer = 1f;
    
    private Rigidbody2D rb;
    private BoatVisualSystem visualSystem;
    
    private bool isRegisteredToPlatform = false;
    private bool movementActive = false;
    private float currentMovementDirection = 1f;
    private Platform assignedPlatform;
    
    [SerializeField] BoatMovementState  movementState = BoatMovementState.AutoMove;
    
    public void Initialize(Rigidbody2D rigidbody, BoatVisualSystem visual)
    {
        rb = rigidbody;
        visualSystem = visual;

        SetupSpeedByState();
    }

    private void SetupSpeedByState()
    {
        movementSpeed = movementState == BoatMovementState.AutoMove ? AUTOMOVE_SPEED : DRIVEN_SPEED;
        maxMovementForce = movementState == BoatMovementState.AutoMove ? AUTOMOVE_SPEED : DRIVEN_SPEED;
    }
    
    public void InitializeBoundaries(Transform left, Transform right)
    {
        leftBoundary = left;
        rightBoundary = right;
    }
    
    public void UpdateMovement()
    {
        if (enableAutomaticMovement && movementActive)
        {
            HandleBoatMovement();
        }
    }
    
    private void HandleBoatMovement()
    {
        if (!IsInWater()) return;
        
        CheckBoundaries();
        ApplyMovementForce();
    }
    
    private void CheckBoundaries()
    {
        bool nearLeftBoundary = leftBoundary != null && 
                               transform.position.x <= leftBoundary.position.x + boundaryBuffer;
        bool nearRightBoundary = rightBoundary != null && 
                                transform.position.x >= rightBoundary.position.x - boundaryBuffer;
        
        if (nearLeftBoundary && currentMovementDirection < 0)
        {
            currentMovementDirection = 1f;
            visualSystem?.UpdateVisualDirection(currentMovementDirection);
        }
        else if (nearRightBoundary && currentMovementDirection > 0)
        {
            currentMovementDirection = -1f;
            visualSystem?.UpdateVisualDirection(currentMovementDirection);
        }
    }
    
    private void ApplyMovementForce()
    {
        Vector2 moveForce = Vector2.right * (currentMovementDirection * movementSpeed);
        moveForce = Vector2.ClampMagnitude(moveForce, maxMovementForce);
        rb.AddForce(moveForce);
    }
    
    private bool IsInWater()
    {
        BoatFloater floater = GetComponent<BoatFloater>();
        if (floater == null || floater.floatPoints == null) return false;
        
        WaterPhysics waterPhysics = WaterPhysics.Instance;
        if (waterPhysics == null) return false;
        
        foreach (Transform point in floater.floatPoints)
        {
            if (point != null)
            {
                Vector2 worldPos = point.position;
                float waterHeight = waterPhysics.GetWaterHeightAt(worldPos);
                if (waterHeight > worldPos.y) return true;
            }
        }
        return false;
    }
    
    public void OnRegisteredToPlatform(Platform platform)
    {
        if (isRegisteredToPlatform) return;
        
        isRegisteredToPlatform = true;
        assignedPlatform = platform;
        
        if (debugMovement)
        {
            Debug.Log($"BoatMovement: Registered to platform {platform.name}, starting movement!");
        }
        
        StartMovement();
    }
    
    public void StartMovement()
    {
        if (movementActive) return;
        
        movementActive = true;
        ChooseNewMovementDirection();
        
        if (debugMovement)
        {
            Debug.Log("BoatMovement: Movement started!");
        }
    }
    
    private void ChooseNewMovementDirection()
    {
        currentMovementDirection = Random.Range(0, 2) == 0 ? -1f : 1f;
        visualSystem?.UpdateVisualDirection(currentMovementDirection);
        
        if (debugMovement)
        {
            string direction = currentMovementDirection > 0 ? "RIGHT" : "LEFT";
            Debug.Log($"BoatMovement: New movement direction: {direction}");
        }
    }

    #region Public Methods

    public void SetMovementState_Driven()
    {
        movementState = BoatMovementState.Driven;
        SetupSpeedByState();
    }

    public void SetMovementState_AutoMove()
    {
        movementState = BoatMovementState.AutoMove;
        SetupSpeedByState();
    }
    
    // public void SetMovementEnabled(bool enabled)
    // {
    //     // This would be handled by the main BoatFloater enableFloaterMovement
    // }
    
    public void SetAutomaticMovementEnabled(bool enabled)
    {
        enableAutomaticMovement = enabled;
        
        if (enabled && !movementActive && isRegisteredToPlatform)
        {
            StartMovement();
        }
        else if (!enabled)
        {
            movementActive = false;
        }
    }
    
    public void ForceStartMovement()
    {
        StartMovement();
    }
    
    public void StopMovement()
    {
        movementActive = false;
        if (debugMovement)
        {
            Debug.Log("BoatMovement: Movement stopped manually");
        }
    }
    
    public bool IsMovementActive() => movementActive;
    public bool IsRegisteredToPlatform() => isRegisteredToPlatform;
    public float GetCurrentMovementDirection() => currentMovementDirection;
    public Platform GetAssignedPlatform() => assignedPlatform;

    #endregion    
}
