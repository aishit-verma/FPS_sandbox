using Unity.Netcode;

namespace PolyFrontlines.Core
{
    /// <summary>
    /// Event structs published/consumed via EventBus<T>.
    /// Keep these as plain data — no logic. Add new events here as
    /// systems need them; don't create one-off ad-hoc event types elsewhere.
    /// </summary>

    public readonly struct PlayerDiedEvent
    {
        public readonly ulong VictimClientId;
        public readonly ulong KillerClientId;
        public readonly string WeaponId;

        public PlayerDiedEvent(ulong victimClientId, ulong killerClientId, string weaponId)
        {
            VictimClientId = victimClientId;
            KillerClientId = killerClientId;
            WeaponId = weaponId;
        }
    }

    public readonly struct PlayerDownedEvent
    {
        public readonly ulong VictimClientId;
        public readonly ulong KillerClientId;
        public readonly string WeaponId;

        public PlayerDownedEvent(ulong victimClientId, ulong killerClientId, string weaponId)
        {
            VictimClientId = victimClientId;
            KillerClientId = killerClientId;
            WeaponId = weaponId;
        }
    }

    public readonly struct PlayerRevivedEvent
    {
        public readonly ulong VictimClientId;
        public readonly ulong ReviverClientId;

        public PlayerRevivedEvent(ulong victimClientId, ulong reviverClientId)
        {
            VictimClientId = victimClientId;
            ReviverClientId = reviverClientId;
        }
    }

    public readonly struct PlayerSpawnedEvent
    {
        public readonly ulong ClientId;
        public readonly int TeamId;

        public PlayerSpawnedEvent(ulong clientId, int teamId)
        {
            ClientId = clientId;
            TeamId = teamId;
        }
    }

    public readonly struct TeamAssignedEvent
    {
        public readonly ulong ClientId;
        public readonly int TeamId;

        public TeamAssignedEvent(ulong clientId, int teamId)
        {
            ClientId = clientId;
            TeamId = teamId;
        }
    }

    public readonly struct CaptureProgressEvent
    {
        public readonly int CapturePointId;
        public readonly int OwningTeamId;
        public readonly float Progress; // 0-1

        public CaptureProgressEvent(int capturePointId, int owningTeamId, float progress)
        {
            CapturePointId = capturePointId;
            OwningTeamId = owningTeamId;
            Progress = progress;
        }
    }

    public readonly struct MatchStateChangedEvent
    {
        public readonly string PreviousState;
        public readonly string NewState;

        public MatchStateChangedEvent(string previousState, string newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }
    }

    public readonly struct AmmoChangedEvent
    {
        public readonly int MagazineAmmo;
        public readonly int ReserveAmmo;

        public AmmoChangedEvent(int magazineAmmo, int reserveAmmo)
        {
            MagazineAmmo = magazineAmmo;
            ReserveAmmo = reserveAmmo;
        }
    }

    public readonly struct GrenadeCountChangedEvent
    {
        public readonly int CurrentGrenades;
        public readonly int MaxGrenades;

        public GrenadeCountChangedEvent(int currentGrenades, int maxGrenades)
        {
            CurrentGrenades = currentGrenades;
            MaxGrenades = maxGrenades;
        }
    }
}