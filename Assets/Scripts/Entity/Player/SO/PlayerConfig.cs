using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "RevengeFishing2D/Player Config")]
public class PlayerConfig : ScriptableObject
{
    [Header("Power System")]
    public long startingPowerLevel = 100;
    public PhaseThresholds phaseThresholds;
    
    [Header("Survival Stats")]
    public float maxHunger = 100f;
    public float maxFatigue = 100f;
    public float hungerDecayRate = 1f;
    public float fatigueRecoveryRate = 0.5f;
}

[System.Serializable]
public class PhaseThresholds
{
    public long juvenile = 10000;
    public long adult = 1000000;
    public long beast = 100000000;
    public long monster = 10000000000;
}
