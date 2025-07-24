using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoatController : MonoBehaviour
{
    [Header("Boat Configuration")]
    [SerializeField] private GameObject boatFishermanPrefab;
    [SerializeField] private Transform[] crewSpawnPoints;
    [SerializeField] private int maxCrewMembers = 2;
    [SerializeField] private int maxInactiveCrewMembers = 1;
    
    [Header("Integrity System")]
    [SerializeField] private float baseBoatIntegrity = 0f;
    [SerializeField] private bool debugIntegrity = true;
    
    [Header("Runtime Info (Read Only)")]
    [SerializeField] private List<Fisherman> allCrewMembers = new List<Fisherman>();
    [SerializeField] private float currentIntegrity;
    [SerializeField] private float maxIntegrity;
    [SerializeField] private bool isBoatDestroyed = false;
    
    [SerializeField] private BoatFloater boatFloater;
    [SerializeField] private BoatPlatform boatPlatform;
    
    public System.Action<float, float> OnIntegrityChanged;
    public System.Action OnBoatDestroyed;

    void Awake()
    {
        if (boatFloater == null)
            boatFloater = GetComponent<BoatFloater>();
        if (boatPlatform == null)
            boatPlatform = GetComponentInChildren<BoatPlatform>();
    }

    void Start()
    {
        if (allCrewMembers.Count == 0)
        {
            StartCoroutine(InitializeBoatWithCrew());
        }
        else
        {
            StartCoroutine(ResetExistingCrew());
        }
    }

    IEnumerator InitializeBoatWithCrew()
    {
        yield return null;
        
        if (debugIntegrity)
            Debug.Log($"Creating {maxCrewMembers} BoatFishermanHandler prefabs for boat {gameObject.name}");
        
        for (int i = 0; i < maxCrewMembers && i < crewSpawnPoints.Length; i++)
        {
            StartCoroutine(InstantiateAndAssignCrewMember(crewSpawnPoints[i]));
        }
        
        yield return null;
        
        RandomlyDeactivateCrewMembers();
        CalculateBoatIntegrity();
        
        if (debugIntegrity)
            Debug.Log($"Boat {gameObject.name} initialized with {GetActiveCrewCount()} active crew. Integrity: {currentIntegrity}/{maxIntegrity}");
    }

    IEnumerator ResetExistingCrew()
    {
        yield return null;
        
        if (debugIntegrity)
            Debug.Log($"Resetting existing crew for boat {gameObject.name}");
        
        foreach (Fisherman fisherman in allCrewMembers)
        {
            if (fisherman != null)
            {
                fisherman.gameObject.SetActive(true);
                fisherman.TriggerAlive();
                fisherman.ScheduleNextAction();
                
                yield return StartCoroutine(AssignToBoatPlatform(fisherman.transform.parent.gameObject));
            }
        }
        
        yield return null;
        
        RandomlyDeactivateCrewMembers();
        CalculateBoatIntegrity();
        
        if (debugIntegrity)
            Debug.Log($"Boat {gameObject.name} reset with {GetActiveCrewCount()} active crew. Integrity: {currentIntegrity}/{maxIntegrity}");
    }

    IEnumerator InstantiateAndAssignCrewMember(Transform spawnPoint)
    {
        if (boatFishermanPrefab == null)
        {
            Debug.LogError($"No boatFishermanPrefab assigned to {gameObject.name}!");
            yield break;
        }
        
        GameObject crewHandlerObj = Instantiate(boatFishermanPrefab, spawnPoint.position, spawnPoint.rotation);
        crewHandlerObj.transform.SetParent(transform);
        
        yield return StartCoroutine(AssignToBoatPlatform(crewHandlerObj));
        
        Fisherman fisherman = crewHandlerObj.GetComponentInChildren<Fisherman>();
        if (fisherman != null)
        {
            allCrewMembers.Add(fisherman);
            
            if (debugIntegrity)
                Debug.Log($"Instantiated and configured Fisherman {fisherman.name} on boat {gameObject.name}");
        }
        else
        {
            Debug.LogError($"Instantiated BoatFishermanHandler doesn't contain Fisherman component!");
            Destroy(crewHandlerObj);
        }
    }

    IEnumerator AssignToBoatPlatform(GameObject enemy)
    {
        yield return null;
        
        if (boatPlatform == null)
        {
            Debug.LogError($"No BoatPlatform found for {gameObject.name}!");
            yield break;
        }
        
        LandEnemy landEnemy = enemy.GetComponentInChildren<LandEnemy>();
        if (landEnemy != null)
        {
            boatPlatform.RegisterEnemyAtRuntime(landEnemy);
            landEnemy.OnPlatformAssigned(boatPlatform);
            
            if (debugIntegrity)
                Debug.Log($"Assigned {landEnemy.name} to BoatPlatform {boatPlatform.name} and triggered OnPlatformAssigned");
        }
    }

    void RandomlyDeactivateCrewMembers()
    {
        if (allCrewMembers.Count == 0) return;
        
        foreach (Fisherman crew in allCrewMembers)
        {
            if (crew != null)
                crew.gameObject.SetActive(true);
        }
        
        int inactiveCount = Random.Range(0, Mathf.Min(maxInactiveCrewMembers + 1, allCrewMembers.Count));
        
        if (inactiveCount > 0)
        {
            List<Fisherman> availableToDeactivate = new List<Fisherman>(allCrewMembers);
            
            for (int i = 0; i < inactiveCount; i++)
            {
                if (availableToDeactivate.Count > 0)
                {
                    int randomIndex = Random.Range(0, availableToDeactivate.Count);
                    Fisherman toDeactivate = availableToDeactivate[randomIndex];
                    toDeactivate.gameObject.SetActive(false);
                    availableToDeactivate.RemoveAt(randomIndex);
                    
                    if (debugIntegrity)
                        Debug.Log($"Deactivated crew member {toDeactivate.name}");
                }
            }
        }
        
        if (debugIntegrity)
            Debug.Log($"Boat has {GetActiveCrewCount()}/{allCrewMembers.Count} active crew members");
    }

    void CalculateBoatIntegrity()
    {
        float totalCrewPower = 0f;
        
        foreach (Fisherman crew in allCrewMembers)
        {
            if (crew != null && crew.gameObject.activeInHierarchy && crew.State == Enemy.EnemyState.Alive)
            {
                totalCrewPower += crew.PowerLevel;
            }
        }
        
        maxIntegrity = baseBoatIntegrity + totalCrewPower;
        currentIntegrity = maxIntegrity;
        
        OnIntegrityChanged?.Invoke(currentIntegrity, maxIntegrity);
    }

    int GetActiveCrewCount()
    {
        int count = 0;
        foreach (Fisherman crew in allCrewMembers)
        {
            if (crew != null && crew.gameObject.activeInHierarchy && crew.State == Enemy.EnemyState.Alive)
            {
                count++;
            }
        }
        return count;
    }

    void CheckBoatDestruction()
    {
        int aliveCrewCount = GetActiveCrewCount();
        
        if (aliveCrewCount == 0 || currentIntegrity <= 0f)
        {
            DestroyBoat();
        }
    }

    void DestroyBoat()
    {
        if (isBoatDestroyed) return;
        
        isBoatDestroyed = true;
        
        if (debugIntegrity)
            Debug.Log($"Boat {gameObject.name} destroyed!");
        
        OnBoatDestroyed?.Invoke();
        
        StartCoroutine(DestroyBoatDelayed());
    }
    
    IEnumerator DestroyBoatDelayed()
    {
        yield return new WaitForSeconds(1f);
        
        if (SimpleObjectPool.Instance != null)
        {
            SimpleObjectPool.Instance.ReturnToPool("BoatHandler", gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ResetForPooling()
    {
        currentIntegrity = 0f;
        maxIntegrity = 0f;
        isBoatDestroyed = false;
        
        if (allCrewMembers.Count == 0)
        {
            StartCoroutine(InitializeBoatWithCrew());
        }
        else
        {
            StartCoroutine(ResetExistingCrew());
        }
    }

    public float GetCurrentIntegrity() => currentIntegrity;
    public float GetMaxIntegrity() => maxIntegrity;
    public List<Fisherman> GetAllCrewMembers() => new List<Fisherman>(allCrewMembers);
    public List<Fisherman> GetActiveCrewMembers()
    {
        List<Fisherman> activeCrew = new List<Fisherman>();
        foreach (Fisherman crew in allCrewMembers)
        {
            if (crew != null && crew.gameObject.activeInHierarchy)
            {
                activeCrew.Add(crew);
            }
        }
        return activeCrew;
    }
}
