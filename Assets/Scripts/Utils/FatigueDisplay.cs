using TMPro;
using UnityEngine;

public class FatigueDisplay : DisplayBase
{
    [Header("Display Settings")]
    [SerializeField] protected Entity entity;
    
    protected override void BaseResponsability()
    {
        base.BaseResponsability();
        UpdateFatigueDisplay();
    }
    
    void UpdateFatigueDisplay()
    {
        if (entity == null || displayText == null) return;

        displayText.text = $"{entity.entityFatigue.fatigue.ToString()} / {entity.entityFatigue.maxFatigue.ToString()}";
    }
    
    /// <summary>
    /// Set the entity reference at runtime (useful for spawned enemies)
    /// </summary>
    public virtual void SetEntity(Entity newEntity)
    {
        entity = newEntity;
    }
}