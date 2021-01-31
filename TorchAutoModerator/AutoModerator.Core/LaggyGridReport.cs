﻿namespace AutoModerator.Core
{
    /// <summary>
    /// Carry around a laggy grid's metadata.
    /// </summary>
    public class LaggyGridReport
    {
        public LaggyGridReport(long gridId,
            double mspf,
            double mspfRatio,
            string gridName,
            string factionTag = null,
            string playerName = null,
            long identityId = -1)
        {
            GridId = gridId;
            Mspf = mspf;
            MspfRatio = mspfRatio;
            GridName = gridName;
            FactionTagOrNull = factionTag;
            PlayerNameOrNull = playerName;
            PlayerIdentityId = identityId;
        }

        public long GridId { get; }
        public double Mspf { get; }
        public double MspfRatio { get; }
        public string GridName { get; }
        public string FactionTagOrNull { get; }
        public string PlayerNameOrNull { get; }
        public long PlayerIdentityId { get; }

        public override string ToString()
        {
            var name = FactionTagOrNull ?? PlayerNameOrNull ?? GridName;
            return $"\"{name}\" (\"{GridName}\"), {Mspf:0.00}ms/f ({MspfRatio:0.00})";
        }
    }
}