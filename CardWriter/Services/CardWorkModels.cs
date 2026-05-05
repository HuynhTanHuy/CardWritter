using System;

namespace CardWriter.Services
{
    public enum CardWorkOperation
    {
        None,
        BatchWrite,
        SingleWrite,
        ClearCard
    }

    /// <summary>Snapshot chụp trên UI thread trước khi vào xử lý nền.</summary>
    public sealed class CardWorkSnapshot
    {
        public CardWorkOperation Operation { get; set; }
        public Func<bool> IsCancelled { get; set; }
        public string HospitalLogKey { get; set; }
        public string HospitalApiId { get; set; }
        public string HospitalCode { get; set; }
        public string BatchCode { get; set; }
        public int GroupNumber { get; set; }
        public int BatchCurrentId { get; set; }
        public int BatchLastId { get; set; }
        public int SinglePatientNumericId { get; set; }
        public string ClearCardIdText { get; set; }
    }

    public enum CardServiceErrorKind
    {
        None,
        Cancelled,
        WriteError,
        DuplicateCard,
        TrailerError,
        ClearError
    }

    public sealed class CardServiceResult
    {
        public bool Success { get; set; }
        public CardServiceErrorKind ErrorKind { get; set; }
        public string UserMessage { get; set; }
        public string IndicatorText { get; set; }
        public string ExistingCardId { get; set; }
        public int? NextBatchCurrentId { get; set; }
        public bool PromptContinueBatch { get; set; }
        public bool BatchRangeCompleted { get; set; }
    }
}
