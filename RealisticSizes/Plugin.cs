using System;
using System.Collections.Generic;
using Exiled.API.Features;
using PlayerRoles;
using RealisticSizes.API;
using RealisticSizes.Handlers;
using PlayerEvents = Exiled.Events.Handlers.Player;
using ServerEvents = Exiled.Events.Handlers.Server;

namespace RealisticSizes
{
    public sealed class Plugin : Plugin<Config>
    {
        public override string Name => "RealisticSizes";

        public override string Author => "Zone-Shilari (base: JesusQC)";

        public override string Prefix => "realisticsizes";

        public override Version Version => new Version(4, 0, 0);

        public override Version RequiredExiledVersion => new Version(9, 14, 2);

        public static Plugin Instance { get; private set; }

        private PlayerHandlers playerHandlers;

        public override void OnEnabled()
        {
            Instance = this;

            ValidateConfig();

            playerHandlers = new PlayerHandlers(Config);

            PlayerEvents.Spawned += playerHandlers.OnSpawned;
            PlayerEvents.Left += playerHandlers.OnLeft;

            ServerEvents.RoundStarted += playerHandlers.OnRoundStarted;
            ServerEvents.RoundEnded += playerHandlers.OnRoundEnded;
            ServerEvents.RestartingRound += playerHandlers.OnRestartingRound;

            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            PlayerEvents.Spawned -= playerHandlers.OnSpawned;
            PlayerEvents.Left -= playerHandlers.OnLeft;

            ServerEvents.RoundStarted -= playerHandlers.OnRoundStarted;
            ServerEvents.RoundEnded -= playerHandlers.OnRoundEnded;
            ServerEvents.RestartingRound -= playerHandlers.OnRestartingRound;

            playerHandlers?.Reset();
            playerHandlers = null;
            Instance = null;

            base.OnDisabled();
        }

        private void ValidateConfig()
        {
            if (Config.ApplyDelay < 0f)
            {
                Log.Warn($"ApplyDelay ({Config.ApplyDelay}) negatif, remis a 0.4.");
                Config.ApplyDelay = 0.4f;
            }

            if (Config.SpreadStep < 0f)
                Config.SpreadStep = 0f;

            if (Config.SpreadMinPlayers < 1)
                Config.SpreadMinPlayers = 1;

            Check("RoleplayRange", Config.RoleplayRange);
            Check("FunRange", Config.FunRange);

            foreach (KeyValuePair<RoleTypeId, SizeRange> entry in Config.ManualRanges)
                Check($"ManualRanges[{entry.Key}]", entry.Value);
        }

        private static void Check(string name, SizeRange range)
        {
            if (range is null)
                return;

            if (range.MinHeight <= 0f || range.MinWidth <= 0f)
            {
                Log.Warn($"{name} contient une valeur minimale nulle ou negative, remise a 0.5.");
                range.MinHeight = Math.Max(0.5f, range.MinHeight);
                range.MinWidth = Math.Max(0.5f, range.MinWidth);
            }

            if (range.MaxHeight < range.MinHeight)
            {
                Log.Warn($"{name} : MaxHeight inferieur a MinHeight, aligne sur MinHeight.");
                range.MaxHeight = range.MinHeight;
            }

            if (range.MaxWidth < range.MinWidth)
            {
                Log.Warn($"{name} : MaxWidth inferieur a MinWidth, aligne sur MinWidth.");
                range.MaxWidth = range.MinWidth;
            }

            if (range.MaxHeight > 3f || range.MaxWidth > 3f)
                Log.Warn($"{name} depasse un facteur 3. Les tailles extremes cassent les collisions et la visee.");
        }
    }
}
