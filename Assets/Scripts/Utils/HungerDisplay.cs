using TMPro;
using UnityEngine;

public class HungerDisplay : DisplayBase
{
    [Header("Display Settings")]
    [SerializeField] private Player player;


    protected override void BaseResponsability()
    {
        base.BaseResponsability();
        UpdateHungerDisplay();
    }

    void UpdateHungerDisplay()
    {
        if (player == null || displayText == null) return;

        displayText.text = $"{player.HungerHandler.GetHunger().ToString()} / {player.HungerHandler.GetMaxHunger().ToString()}";
    }

    /// <summary>
    /// Set the entity reference at runtime (useful for spawned enemies)
    /// </summary>
    public void SetEntity(Player newEntity)
    {
        player = newEntity;
    }
}
