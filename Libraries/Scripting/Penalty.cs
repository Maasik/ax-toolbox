﻿using System;
using System.Collections.Generic;
using System.Linq;
using AXToolbox.GpsLoggers;

namespace AXToolbox.Scripting
{
    public enum PenaltyType { Measure, TaskPoints, CompetitionPoints }

    public class Penalty
    {
        public PenaltyType Type { get; protected set; }
        public Result Performance { get; protected set; }
        public int Points { get; protected set; }
        public string InfringedRules { get; protected set; }

        public List<AXPoint> UsedPoints { get; protected set; }
        public AXPoint LastUsedPoint
        {
            get
            {
                if (UsedPoints.Count > 0)
                    return UsedPoints.OrderBy(p => p.Time).Last();
                else
                    return null;
            }
        }

        public Penalty(Result performance)
        {
            Type = PenaltyType.Measure;
            Performance = performance;
            InfringedRules = performance.Reason;
            UsedPoints = new List<AXPoint>();
        }
        public Penalty(string infringedRule, PenaltyType type, int value)
        {
            Type = type;
            if (type == PenaltyType.Measure)
                throw new InvalidOperationException("Use Penalty(string infringedRule, Result measure) instead");

            Points = value;
            InfringedRules = infringedRule;
            UsedPoints = new List<AXPoint>();
        }

        public override string ToString()
        {
            var str = "";
            switch (Type)
            {
                case PenaltyType.Measure:
                    str = string.Format("{0}: {1}", InfringedRules, Performance.ValueUnitToString());
                    break;
                case PenaltyType.TaskPoints:
                    str = string.Format("{0}: {1}TP", InfringedRules, Points);
                    break;
                case PenaltyType.CompetitionPoints:
                    str = string.Format("{0}: {1}CP", InfringedRules, Points);
                    break;
            }
            return str;
        }
    }
}
