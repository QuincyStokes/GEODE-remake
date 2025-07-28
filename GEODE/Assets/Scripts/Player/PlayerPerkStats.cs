using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Network-synced container for the stat modifiers that come from the perks a single player picked
/// in the lobby. Each player object owns exactly one instance of this component, so perks no longer
/// leak to other players.
/// </summary>
public class PlayerPerkStats : NetworkBehaviour
{
    // --- Networked modifiers -------------------------------------------------------------
    public NetworkVariable<float> DamageMultiplier  = new(1f,  NetworkVariableReadPermission.Everyone);
    public NetworkVariable<float> SpeedMultiplier   = new(1f,  NetworkVariableReadPermission.Everyone);
    public NetworkVariable<float> HealthBonus       = new(0f,  NetworkVariableReadPermission.Everyone);

    // Extend with tower buffs, XP multipliers, etc. the same way:
    // public NetworkVariable<float> TowerDamageMultiplier = new(1f, NetworkVariableReadPermission.Everyone);

    // --------------------------------------------------------------------------------------
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Only the owning client reports its chosen perks to the server.
        if (IsOwner)
        {
            ApplyChosenPerksServerRpc(CollectLocalPerkStatTotals());
        }
    }

    // Gather totals from this client’s RunSettings (exists only on the local machine).
    private StatTotals CollectLocalPerkStatTotals()
    {
        var totals = new StatTotals { dmg = 1f, speed = 1f, health = 0f };

        if (RunSettings.Instance == null) return totals;

        foreach (var perk in RunSettings.Instance.chosenPerks)
        {
            if (perk is PlayerStatPerk p)
            {
                switch (p.statType)
                {
                    case PlayerStatPerk.PlayerStatType.Damage:
                        totals.dmg += p.statIncrease;
                        break;
                    case PlayerStatPerk.PlayerStatType.Speed:
                        totals.speed += p.statIncrease;
                        break;
                    case PlayerStatPerk.PlayerStatType.Health:
                        totals.health += p.statIncrease;
                        break;
                }
            }
        }
        return totals;
    }

    // Send aggregated totals to server exactly once.
    [ServerRpc]
    private void ApplyChosenPerksServerRpc(StatTotals totals)
    {
        DamageMultiplier.Value = totals.dmg;
        SpeedMultiplier.Value  = totals.speed;
        HealthBonus.Value      = totals.health;
    }

    // Simple struct for RPC parameter bundling (must be blittable). Unity’s RPCs allow structs with
    // primitive fields.
    private struct StatTotals : INetworkSerializable
    {
        public float dmg;
        public float speed;
        public float health;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref dmg);
            serializer.SerializeValue(ref speed);
            serializer.SerializeValue(ref health);
        }
    }
} 