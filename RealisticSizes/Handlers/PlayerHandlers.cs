using System;
using System.Collections.Generic;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using MEC;
using PlayerRoles;
using RealisticSizes.API;
using UnityEngine;

namespace RealisticSizes.Handlers
{
    public sealed class PlayerHandlers
    {
        private readonly Config config;
        private readonly Dictionary<string, CoroutineHandle> pending = new Dictionary<string, CoroutineHandle>();

        private int spawnCounter;

        public PlayerHandlers(Config config) => this.config = config;

        public void OnSpawned(SpawnedEventArgs ev)
        {
            try
            {
                if (ev?.Player is null || string.IsNullOrEmpty(ev.Player.UserId))
                    return;

                Player player = ev.Player;
                RoleTypeId role = player.Role.Type;

                if (!IsEligible(player, role))
                {
                    ResetScale(player);
                    return;
                }

                SizeRange range = ResolveRange(role);

                if (range is null)
                {
                    ResetScale(player);
                    return;
                }

                Vector3 scale = BuildScale(range);
                string userId = player.UserId;

                Cancel(userId);

                pending[userId] = Timing.CallDelayed(NextDelay(), () =>
                {
                    pending.Remove(userId);

                    Player target = Player.Get(userId);

                    if (target is null || !target.IsConnected || target.Role.Type != role)
                        return;

                    target.Scale = scale;

                    if (config.Debug)
                        Log.Debug($"Taille appliquee a {target.Nickname} ({role}) : {scale}.");
                });
            }
            catch (Exception e)
            {
                Log.Error($"OnSpawned: {e}");
            }
        }

        public void OnLeft(LeftEventArgs ev)
        {
            if (ev?.Player is null || string.IsNullOrEmpty(ev.Player.UserId))
                return;

            Cancel(ev.Player.UserId);
        }

        public void OnRoundStarted() => spawnCounter = 0;

        public void OnRoundEnded(RoundEndedEventArgs ev) => Reset();

        public void OnRestartingRound() => Reset();

        public void Reset()
        {
            foreach (KeyValuePair<string, CoroutineHandle> entry in pending)
                Timing.KillCoroutines(entry.Value);

            pending.Clear();
            spawnCounter = 0;
        }

        private bool IsEligible(Player player, RoleTypeId role)
        {
            if (role == RoleTypeId.None || role == RoleTypeId.Spectator)
                return false;

            if (config.IgnoredRoles.Contains(role))
                return false;

            return config.AffectScps || !player.IsScp;
        }

        private SizeRange ResolveRange(RoleTypeId role)
        {
            switch (config.ActiveMode)
            {
                case SizeMode.Roleplay:
                    return config.RoleplayRange;

                case SizeMode.Fun:
                    return config.FunRange;

                case SizeMode.Manual:
                    return config.ManualRanges.TryGetValue(role, out SizeRange range) ? range : null;

                default:
                    return null;
            }
        }

        private Vector3 BuildScale(SizeRange range)
        {
            float height = UnityEngine.Random.Range(range.MinHeight, range.MaxHeight);

            if (!config.AllowUnproportionalValues)
                return new Vector3(height, height, height);

            float width = UnityEngine.Random.Range(range.MinWidth, range.MaxWidth);

            return new Vector3(width, height, width);
        }

        private float NextDelay()
        {
            float delay = Mathf.Max(0f, config.ApplyDelay);

            if (!config.SpreadWorkload || Player.List.Count < config.SpreadMinPlayers)
                return delay;

            spawnCounter++;

            return delay + (spawnCounter % 12 * Mathf.Max(0f, config.SpreadStep));
        }

        private void Cancel(string userId)
        {
            if (!pending.TryGetValue(userId, out CoroutineHandle handle))
                return;

            Timing.KillCoroutines(handle);
            pending.Remove(userId);
        }

        private static void ResetScale(Player player)
        {
            if (player is null || player.Scale == Vector3.one)
                return;

            player.Scale = Vector3.one;
        }
    }
}
