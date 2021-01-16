using System;

namespace AutoModerator.Core
{
    // this class shouldn't hold onto any game entities so it won't mess with the game's GC
    public sealed class GridLagReport
    {
        public GridLagReport(GridLagProfileResult profileResult, TimeSpan remainingTime)
        {
            GridId = profileResult.GridId;
            ThresholdNormal = profileResult.ThresholdNormal;
            GridName = profileResult.GridName;
            FactionTagOrNull = profileResult.FactionTagOrNull;
            PlayerNameOrNull = profileResult.PlayerNameOrNull;
            RemainingTime = remainingTime;
        }

        public long GridId { get; }
        public double ThresholdNormal { get; }
        public string GridName { get; }
        public string FactionTagOrNull { get; }
        public string PlayerNameOrNull { get; }
        public TimeSpan RemainingTime { get; }

        public override string ToString()
        {
            var normal = $"{ThresholdNormal * 100f:0.00}%";
            var remainingTime = $"{RemainingTime.TotalMinutes:0.0}m";
            var factionTag = FactionTagOrNull ?? "<single>";
            var playerName = PlayerNameOrNull ?? "<none>";
            return $"\"{GridName}\" ({GridId}) {normal} for {remainingTime} [{factionTag}] {playerName}";
        }
    }
}