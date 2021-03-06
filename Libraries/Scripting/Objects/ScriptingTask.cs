﻿using AXToolbox.Common;
using AXToolbox.GpsLoggers;
using AXToolbox.PdfHelpers;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AXToolbox.Scripting
{
    internal class ScriptingTask : ScriptingObject
    {
        internal static ScriptingTask Create(ScriptingEngine engine, ObjectDefinition definition)
        {
            return new ScriptingTask(engine, definition);
        }

        protected ScriptingTask(ScriptingEngine engine, ObjectDefinition definition)
            : base(engine, definition)
        {
            Penalties = new List<Penalty>();
            LoggerMarks = new List<string>();
        }

        public int Number { get; protected set; }

        protected string resultUnit;
        protected int resultPrecission;

        public Result Result { get; protected set; }

        public List<Penalty> Penalties { get; protected set; }

        public List<string> LoggerMarks { get; protected set; }

        public override void CheckConstructorSyntax()
        {
            base.CheckConstructorSyntax();

            AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 1);
            Number = ParseOrDie<int>(0, Parsers.ParseInt);

            resultPrecission = 2;
            switch (Definition.ObjectType)
            {
                default:
                    throw new ArgumentException("Unknown task type '" + Definition.ObjectType + "'");

                case "PDG":
                    resultUnit = "m";
                    break;

                case "JDG":
                    resultUnit = "m";
                    break;

                case "HWZ":
                    resultUnit = "m";
                    break;

                case "FIN":
                    resultUnit = "m";
                    break;

                case "FON":
                    resultUnit = "m";
                    break;

                case "HNH":
                    resultUnit = "m";
                    break;

                case "WSD":
                    resultUnit = "m";
                    break;

                case "GBM":
                    resultUnit = "m";
                    break;

                case "CRT":
                    resultUnit = "m";
                    break;

                case "RTA":
                    resultUnit = "s";
                    resultPrecission = 0;
                    break;

                case "ELB":
                    resultUnit = "°";
                    break;

                case "LRN":
                    resultUnit = "km^2";
                    break;

                case "MDT":
                    resultUnit = "m";
                    break;

                case "SFL":
                    resultUnit = "m";
                    break;

                case "MDD":
                    resultUnit = "m";
                    break;

                case "XDT":
                    resultUnit = "m";
                    break;

                case "XDI":
                    resultUnit = "m";
                    break;

                case "XDD":
                    resultUnit = "m";
                    break;

                case "ANG":
                    resultUnit = "°";
                    break;

                case "3DT":
                    resultUnit = "m";
                    break;
            }
        }

        public override void CheckDisplayModeSyntax()
        { }

        public override void Display()
        { }

        public override void Reset()
        {
            base.Reset();
            Result = null;
            Penalties.Clear();
            LoggerMarks.Clear();
        }

        public override void Process()
        {
            base.Process();

            ResetValidTrackPoints();

            AddNote(string.Format("track contains {0} valid points for this task", Engine.TaskValidTrack.Length));
        }

        public void ResetValidTrackPoints()
        {
            //remove task filter if any
            Engine.TaskValidTrack = Engine.FlightValidTrack;

            if (Engine.Settings.TasksInOrder)
            {
                //remove track portions used by previous tasks
                try
                {
                    Engine.TaskValidTrack = Engine.TaskValidTrack.Filter(p => p.Time >= Engine.LastUsedPoint.Time);
                }
                catch { }
            }
        }

        public Result NewResult(double value)
        {
            return Result = Result.NewResult(value, resultUnit);
        }

        public Result NewNoResult(string reason)
        {
            Result = Result.NewNoResult(reason);
            return Result;
        }

        public Result NewNoFlight()
        {
            return Result = Result.NewNoFlight();
        }

        public string ToCsvString()
        {
            Result measurePenalty = Result.NewResult(0, resultUnit);
            int taskPoints = 0;
            int competitionPoints = 0;
            string infringedRules = "";

            if (!string.IsNullOrEmpty(Result.Reason))
                infringedRules = Result.Reason;

            foreach (var p in Penalties)
            {
                measurePenalty = Result.Merge(measurePenalty, p.Performance);
                if (measurePenalty.Type == ResultType.No_Result)
                    Result = p.Performance;
                taskPoints += p.Type == PenaltyType.TaskPoints ? p.Points : 0;
                competitionPoints += p.Type == PenaltyType.CompetitionPoints ? p.Points : 0;
                infringedRules += p.ToString();
            }

            return string.Format(NumberFormatInfo.InvariantInfo, "result;auto;{0};{1};{2:0.00};{3:0.00};{4:0};{5:0};{6}",
                Number, Engine.Report.PilotId, Result.ValueToString(), measurePenalty.ValueToString(), taskPoints, competitionPoints, infringedRules);
        }

        internal void ToPdfReport(PdfHelper helper)
        {
            var document = helper.Document;
            var config = helper.Config;
            PdfPCell c = null;

            var title = string.Format("Task {0}: {1}", Number, Definition.ObjectType);
            var table = helper.NewTable(null, new float[] { 1, 4 }, title);

            //results
            table.AddCell(new PdfPCell(new Paragraph("Result:", config.BoldFont)));
            c = new PdfPCell() { PaddingBottom = 5 };
            c.AddElement(new Paragraph("Performance = " + Result.ToString(), config.BoldFont));
            foreach (var p in Result.UsedPoints)
                c.AddElement(new Paragraph(p.ToString(AXPointInfo.CustomReport), config.FixedWidthFont) { SpacingBefore = 0 });
            foreach (var str in Result.UsedTrack.ToStringList())
                c.AddElement(new Paragraph(str, config.FixedWidthFont) { SpacingBefore = 0 });
            table.AddCell(c);

            //penalties
            table.AddCell(new PdfPCell(new Paragraph("Penalties:", config.BoldFont)));
            c = new PdfPCell() { PaddingBottom = 5 };
            foreach (var pen in Penalties)
            {
                c.AddElement(new Paragraph(pen.ToString(), config.BoldFont) { SpacingBefore = 0 });
                foreach (var str in pen.InfringingTrack.ToStringList())
                    c.AddElement(new Paragraph(str, config.FixedWidthFont) { SpacingBefore = 0 });
            }
            table.AddCell(c);

            table.AddCell(new PdfPCell(new Paragraph("Logger marks:", config.BoldFont)));
            c = new PdfPCell() { PaddingBottom = 5 };
            foreach (var m in LoggerMarks)
            {
                c.AddElement(new Paragraph(m, config.FixedWidthFont) { SpacingBefore = 0 });
            }
            table.AddCell(c);

            table.AddCell(new PdfPCell(new Paragraph("Remarks:", config.BoldFont)));
            c = new PdfPCell() { PaddingBottom = 5 };
            foreach (var obj in Engine.Heap.Values.Where(o => o.Task == this))
                foreach (var note in obj.Notes.Where(n => n.IsImportant))
                    c.AddElement(new Paragraph(note.ToString(), config.FixedWidthFont) { SpacingBefore = 0 });
            table.AddCell(c);

            document.Add(table);
        }

        public DateTime TaskOrder
        {
            get
            {
                var lastUsedPoint = Result.LastUsedPoint;
                if (lastUsedPoint == null)
                    return Engine.Settings.Date + new TimeSpan(12, 0, 0); //ensure that will be the last
                else
                    return lastUsedPoint.Time;
            }
        }
    }
}