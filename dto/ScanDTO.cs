﻿using System;


namespace CxAPI_Store.dto
{

    public partial class ScanSettings
    {
        public EmailNotifications emailNotifications { get; set; }
        public EngineConfiguration engineConfiguration { get; set; }
        public PostScanAction postScanAction { get; set; }
        public Preset preset { get; set; }
        public Project project { get; set; }

        public class EmailNotifications
        {
            public object afterScan { get; set; }
            public object beforeScan { get; set; }
            public object failedScan { get; set; }
        }

        public class EngineConfiguration
        {
            public long id { get; set; }
            public object Link { get; set; }
        }

        public class PostScanAction
        {
            public long id { get; set; }
            public object link { get; set; }
        }
        public class Preset
        {
            public long id { get; set; }
            public object link { get; set; }

        }
        public class Project
        {
            public long id { get; set; }
            public object link { get; set; }

        }
    }
 //   public partial class Teams
 //   {
//        public Guid id { get; set; }
//        public string fullName { get; set; }
//    }

    public partial class Teams
    {
        public string id { get; set; }
        public string name { get; set; }
        public string fullName { get; set; }
        public long parentId { get; set; }
    }


    public partial class ScanObject
    {
        public long Id { get; set; }
        public Project Project { get; set; }
        public Status Status { get; set; }
        public FinishedScanStatus ScanType { get; set; }
        public string Comment { get; set; }
        public DateAndTime DateAndTime { get; set; }
        public ResultsStatistics ResultsStatistics { get; set; }
        public ScanState ScanState { get; set; }
        public string Owner { get; set; }
        public string Origin { get; set; }
        public string InitiatorName { get; set; }
        public string OwningTeamId { get; set; }
        public string OwningTeam { get; set; }
        public bool IsPublic { get; set; }
        public bool IsLocked { get; set; }
        public bool IsIncremental { get; set; }
        public long ScanRisk { get; set; }
        public long ScanRiskSeverity { get; set; }
        public Project EngineServer { get; set; }
        public FinishedScanStatus FinishedScanStatus { get; set; }
        public object PartialScanReasons { get; set; }
    }

    public partial class DateAndTime
    {
        public DateTimeOffset? StartedOn { get; set; }
        public DateTimeOffset? FinishedOn { get; set; }
        public DateTimeOffset? EngineStartedOn { get; set; }
        public DateTimeOffset? EngineFinishedOn { get; set; }
    }

    public partial class Project
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public object Link { get; set; }
    }

    public partial class FinishedScanStatus
    {
        public long Id { get; set; }
        public string Value { get; set; }
    }

    public partial class ResultsStatistics
    {
        public object Link { get; set; }
    }

    public partial class ScanState
    {
        public string Path { get; set; }
        public string SourceId { get; set; }
        public long FilesCount { get; set; }
        public long LinesOfCode { get; set; }
        public long FailedLinesOfCode { get; set; }
        public string CxVersion { get; set; }
        public LanguageStateCollection[] LanguageStateCollection { get; set; }
    }

    public partial class LanguageStateCollection
    {
        public long LanguageId { get; set; }
        public string LanguageName { get; set; }
        public string LanguageHash { get; set; }
        public DateTimeOffset StateCreationDate { get; set; }
    }

    public partial class Status
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public Details Details { get; set; }
    }

    public partial class Details
    {
        public string Stage { get; set; }
        public string Step { get; set; }
    }
    public partial class ScanStatistics
    {
        public int HighSeverity { get; set; }
        public int MediumSeverity { get; set; }
        public int LowSeverity { get; set; }
        public int InfoSeverity { get; set; }
        public DateTimeOffset StatisticsCalculationDate { get; set; }
    }
    public partial class ScanCount
    {
        public int count { get; set; }
    }

}