using System;
using UnityEngine;
using Utils;

public abstract class Enemy : Entity
{
    public enum Tier
    {
        Tier1=0,
        Tier2=1,
        Tier3=2,
        Tier4=3,
        Tier5=4,
        Tier6=5
    }

    public enum EnemyState
    {
        Alive=0,
        Defeated=1,
        Eaten=2,
        Dead=3
    }

    [SerializeField] protected Tier _tier;
    [SerializeField] protected EnemyState _state;
    public EnemyState State => _state;
    
    [Header("Player Reference")]
    [SerializeField] protected Player player;

    [Header("Pull Mechanic")]
    [SerializeField] protected bool hasReceivedFirstFatigue = false;
    [SerializeField] protected bool canPullPlayer = false;
    [SerializeField] protected float pullForce = 5f;
    [SerializeField] protected float pullDuration = 1f;

    [Header("Decision making Timer")]
    [SerializeField] protected float minActionTime = 1f;
    [SerializeField] protected float maxActionTime = 4f;
    [SerializeField] protected float nextActionTime;

    [Header("Debug")]
    [SerializeField] protected bool enableDebugMessages = false;

    // NUEVAS VARIABLES QUE AÑADISTE
    [Header("References")]
    [SerializeField] protected Collider2D bodyCollider;
    [SerializeField] protected GameObject parentContainer;

    // Public accessors para las nuevas variables
    public Collider2D BodyCollider => bodyCollider;
    public GameObject ParentContainer => parentContainer;
    
    public Action<Enemy> OnEnemyDied;
    
    public float NextActionTime
    {
        get { return nextActionTime; }
        set { nextActionTime = value; }
    }

    public bool HasReceivedFirstFatigue
    {
        get { return hasReceivedFirstFatigue; }
        set { hasReceivedFirstFatigue = value; }
    }

    public bool CanPullThePlayer
    {
        get { return canPullPlayer; }
        set { canPullPlayer = value; }
    }

    #region Water Enemy Variables
    [SerializeField] protected float swimForce;
    [SerializeField] protected float minSwimSpeed;
    #endregion

    #region Base Behaviours
    protected virtual void Start()
    {
        entityType = EntityType.Enemy;
        // ! NOTE: Commented this line because thinking in just 1 type of fisherman, it breaks some behaviours of boat
        // Enemy layer collides with Platform / BoatEnemy layer collides with BoatPlatform 
        // With this line all fisherman will be Enemy Layer and will break the collisions in boat
        // Layers already setup in each prefab: LandFisherman has Enemy Layer assigned and BaatFisherman has BoatEnemy layer assigned
        // gameObject.layer = LayerMask.NameToLayer(LayerNames.ENEMY);

        if (player == null)
        {
            player = Player.Instance;
        }

        AutoAssignReferences();
        
        Initialize();
    }

    private void AutoAssignReferences()
    {
        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<Collider2D>();
            if (enableDebugMessages && bodyCollider != null)
                Debug.Log($"{gameObject.name}: Auto-assigned bodyCollider");
        }

        if (parentContainer == null)
        {
            parentContainer = FindMyHandler();
            if (enableDebugMessages && parentContainer != null)
                Debug.Log($"{gameObject.name}: Auto-assigned parentContainer to {parentContainer.name}");
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        if (player == null)
        {
            player = Player.Instance;
        }

        EnemySetup();
    }

    protected virtual void EnemySetup()
    {
        if (_powerLevel <= 0)
        {
            if (player != null)
            {
                _powerLevel = player.PowerLevel;
            }
            else
            {
                _powerLevel = 100;
            }
        }

        entityFatigue.fatigue = 0;
        entityFatigue.maxFatigue = _powerLevel;
        _state = EnemyState.Alive;

        CalculateTier();
    }

    public virtual void SetPowerLevel(int newPowerLevel)
    {
        _powerLevel = newPowerLevel;
        entityFatigue.maxFatigue = _powerLevel;
        entityFatigue.fatigue = 0;
        Debug.Log($"{gameObject.name} power level set to {_powerLevel}");
    }

    private void CalculateTier()
    {
        if (_powerLevel > 10000000) _tier = Tier.Tier6;
        else if (_powerLevel > 1000000) _tier = Tier.Tier5;
        else if (_powerLevel > 100000) _tier = Tier.Tier4;
        else if (_powerLevel > 10000) _tier = Tier.Tier3;
        else if (_powerLevel > 1000) _tier = Tier.Tier2;
        else _tier = Tier.Tier1;
    }

    protected override void Update()
    {
        base.Update();
    }

    public abstract void WaterMovement();

    public override void SetMovementMode(bool aboveWater)
    {
        base.SetMovementMode(aboveWater);
    }

    public void TakeFatigue(int playerPowerLevel)
    {
        if (!hasReceivedFirstFatigue)
        {
            hasReceivedFirstFatigue = true;
            canPullPlayer = true;
            OnFirstFatigueReceived();
            Debug.Log($"{gameObject.name} received first fatigue damage - can now pull player!");
        }

        entityFatigue.fatigue += (int)((float)playerPowerLevel * .05f);

        if (entityFatigue.fatigue >= entityFatigue.maxFatigue && _state == EnemyState.Alive)
        {
            TriggerDefeat();
        }
    }

    protected virtual void OnFirstFatigueReceived()
    {
        Debug.Log($"{gameObject.name} can now pull the player!");
    }

    public bool CanPullPlayer()
    {
        return canPullPlayer && _state == EnemyState.Alive;
    }

    /// <summary>
    /// FIXED: Reset fatigue to zero - called when spawning from pool
    /// </summary>
    public virtual void ResetFatigue()
    {
        hasReceivedFirstFatigue = false;
        canPullPlayer = false;
        entityFatigue.fatigue = 0;
        
        if (enableDebugMessages)
            Debug.Log($"{gameObject.name}: Fatigue reset to 0");
    }

    /// <summary>
    /// FIXED: Change state to alive and reset physics - called when spawning from pool
    /// </summary>
    public virtual void ChangeState_Alive()
    {
        _state = EnemyState.Alive;
        
        // FIXED: Reset physics properly when changing to alive state
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = true;
            rb.gravityScale = 1f; // Ensure gravity is proper
        }

        // FIXED: Use new bodyCollider reference instead of searching
        if (bodyCollider != null)
        {
            bodyCollider.isTrigger = false;
        }
        
        if (this is LandEnemy landEnemy)
        {
            landEnemy.HasStartedFloating = false;
        }
        
        if (enableDebugMessages)
            Debug.Log($"{gameObject.name}: State changed to Alive, physics reset");
    }

    /// <summary>
    /// Schedule next action - required by SimpleObjectPool
    /// </summary>
    public virtual void ScheduleNextAction()
    {
        float actionDuration = UnityEngine.Random.Range(minActionTime, maxActionTime);
        nextActionTime = Time.time + actionDuration;
    }

    /// <summary>
    /// FIXED: Complete reset including physics
    /// </summary>
    public virtual void TriggerAlive()
    {
        ChangeState_Alive();
        ResetFatigue();

        // FIXED: More comprehensive physics reset
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 1f;
            rb.simulated = true;
            rb.freezeRotation = true; // Prevent rotation issues
        }

        // FIXED: Use new bodyCollider reference
        if (bodyCollider != null)
        {
            bodyCollider.isTrigger = false;
        }

        Debug.Log($"{gameObject.name} state reset to Alive with complete physics reset");
    }

    protected virtual void TriggerDefeat()
    {
        Debug.Log($"{gameObject.name} has been defeated!");
        ChangeState_Defeated();
        InterruptAllActions();
        StartDefeatBehaviors();
    }

    protected virtual void TriggerEaten()
    {
        Debug.Log($"{gameObject.name} has been EATEN!");
        ChangeState_Eaten();
        InterruptAllActions();
        TriggerDead();
    }

    protected virtual void TriggerDead()
    {
        Debug.Log($"{gameObject.name} has DIED!");
        ChangeState_Dead();
        InterruptAllActions();
        
        if (player != null)
        {
            player.GainPowerFromEating(_powerLevel);
            Debug.Log($"Player consumed {gameObject.name} with power level {_powerLevel}");
        }
        
        EnemyDie();
    }

    /// <summary>
    /// Use object pool system instead of Destroy
    /// </summary>
    protected virtual void TriggerEscape()
    {
        Debug.Log($"{gameObject.name} has ESCAPED! The player can no longer catch this enemy.");
        ReturnToPool();
    }

    /// <summary>
    /// Use object pool system instead of Destroy
    /// </summary>
    protected virtual void EnemyDie()
    {
        Debug.Log($"{gameObject.name} has been REVERSE FISHED!");
        
        OnEnemyDied?.Invoke(this);
        
        ReturnToPool();
    }

    /// <summary>
    /// FIXED: Use new parentContainer reference instead of searching
    /// </summary>
    private void ReturnToPool()
    {
        // FIXED: Use the new parentContainer reference directly
        GameObject handler = parentContainer != null ? parentContainer : FindMyHandler();
    
        if (handler == null)
        {
            Debug.LogError($"Could not find handler for enemy {gameObject.name}! Destroying instead.");
            Destroy(gameObject);
            return;
        }
    
        // Notify spawn handler first
        NotifySpawnHandlerOfDeath();
    
        // Clean up before returning to pool
        CleanupBeforePoolReturn();
    
        // Return the COMPLETE HANDLER to appropriate pool
        if (SimpleObjectPool.Instance != null)
        {
            string poolName = DeterminePoolName(handler);
            SimpleObjectPool.Instance.ReturnToPool(poolName, handler);
        }
        else
        {
            Debug.LogError("SimpleObjectPool not found! Destroying handler instead.");
            Destroy(handler);
        }
    }
    
    /// <summary>
    /// FIXED: Use parentContainer reference first, fallback to search
    /// </summary>
    private GameObject FindMyHandler()
    {
        // FIXED: Use parentContainer if available
        if (parentContainer != null)
        {
            return parentContainer;
        }

        // Fallback to hierarchy search
        Transform current = transform;
    
        while (current != null)
        {
            string name = current.name.ToLower();
        
            if (name.Contains("landfishermanhandler") || 
                name.Contains("boatfishermanhandler") ||
                name.Contains("fishermanhandler"))
            {
                return current.gameObject;
            }
        
            current = current.parent;
        }
    
        return null;
    }

    /// <summary>
    /// Find and notify the correct spawn handler
    /// </summary>
    private void NotifySpawnHandlerOfDeath()
    {
        SpawnHandler[] allSpawnHandlers = FindObjectsOfType<SpawnHandler>();
        
        foreach (SpawnHandler handler in allSpawnHandlers)
        {
            if (ShouldNotifyHandler(handler))
            {
                handler.OnEnemyDestroyed(gameObject);
                break;
            }
        }
    }

    /// <summary>
    /// Determine which spawn handler should be notified
    /// </summary>
    private bool ShouldNotifyHandler(SpawnHandler handler)
    {
        if (handler.config == null) return false;
        
        bool isLandFisherman = this is LandEnemy && 
                              handler.config.enemyType == SpawnHandlerConfig.EnemyType.LandFisherman;
                              
        bool isBoatFisherman = gameObject.name.ToLower().Contains("boatfisherman") && 
                              handler.config.enemyType == SpawnHandlerConfig.EnemyType.BoatFisherman;
        
        return isLandFisherman || isBoatFisherman;
    }

    /// <summary>
    /// Clean up before returning to pool
    /// </summary>
    protected virtual void CleanupBeforePoolReturn()
    {
        StopAllCoroutines();
        
        if (this is LandEnemy landEnemy)
        {
            if (landEnemy.GetAssignedPlatform() != null)
            {
                landEnemy.GetAssignedPlatform().UnregisterEnemy(landEnemy);
                landEnemy.SetAssignedPlatform(null);
            }
        }
    }

    /// <summary>
    /// Determine which pool this handler belongs to
    /// </summary>
    private string DeterminePoolName(GameObject handler)
    {
        string handlerName = handler.name.ToLower();
        
        if (handlerName.Contains("landfishermanhandler"))
        {
            return "LandFisherman";
        }
        else if (handlerName.Contains("boatfishermanhandler"))
        {
            return "BoatFisherman";
        }
        
        Debug.LogWarning($"Could not determine pool for handler {handler.name}, using LandFisherman as default");
        return "LandFisherman";
    }

    protected virtual void InterruptAllActions()
    {
        ScheduleNextAction();
        Debug.Log($"{gameObject.name} - All actions interrupted due to defeat");
    }

    /// <summary>
    /// FIXED: Use new bodyCollider reference instead of searching
    /// </summary>
    protected virtual void StartDefeatBehaviors()
    {
        // FIXED: Use the new bodyCollider reference directly
        if (bodyCollider != null)
        {
            bodyCollider.isTrigger = true;
            Debug.Log($"{gameObject.name} - Body collider set to trigger (phasing through platforms)");
        }
        else
        {
            // Fallback to old method if bodyCollider not assigned
            Collider2D enemyCollider = GetComponent<Collider2D>();
            if (enemyCollider == null)
            {
                enemyCollider = GetComponentInChildren<Collider2D>();
            }

            if (enemyCollider != null)
            {
                enemyCollider.isTrigger = true;
                Debug.Log($"{gameObject.name} - Fallback collider set to trigger");
            }
        }
    }
    #endregion

    #region State Management
    public EnemyState GetState() => _state;
    public virtual void ChangeState_Defeated() => _state = EnemyState.Defeated;
    public virtual void ChangeState_Eaten() => _state = EnemyState.Eaten;
    public virtual void ChangeState_Dead() => _state = EnemyState.Dead;
    #endregion

    #region Actions
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (_state == EnemyState.Defeated && other.CompareTag("PlayerCollider"))
        {
            ChangeState_Eaten();
            TriggerEaten();
        }
    }
    #endregion
}
