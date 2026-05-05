using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using CardWriter.Devices;
using Duali;

namespace CardWriter.Services
{
    /// <summary>Toàn bộ logic ghi/clear Mifare + log file — không tham chiếu Control.</summary>
    public sealed class CardService
    {
        private readonly IRfidWriter _writer;
        private readonly ICardApiClient _apiClient;
        private readonly SemaphoreSlim _oneCard = new SemaphoreSlim(1, 1);

        public CardService(IRfidWriter writer, ICardApiClient apiClient)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        public CardServiceResult Process(CardWorkSnapshot snap, string physicalUid)
        {
            if (snap == null)
                throw new ArgumentNullException(nameof(snap));

            _oneCard.Wait();
            try
            {
                switch (snap.Operation)
                {
                    case CardWorkOperation.BatchWrite:
                        return ProcessBatchWrite(snap, physicalUid);
                    case CardWorkOperation.SingleWrite:
                        return ProcessSingleWrite(snap, physicalUid);
                    case CardWorkOperation.ClearCard:
                        return ProcessClear(snap);
                    default:
                        return new CardServiceResult { Success = false, UserMessage = "Không có thao tác." };
                }
            }
            finally
            {
                _oneCard.Release();
            }
        }

        private static bool Cancelled(CardWorkSnapshot snap) =>
            snap.IsCancelled != null && snap.IsCancelled();

        private CardServiceResult ProcessBatchWrite(CardWorkSnapshot snap, string physicalUid)
        {
            if (Cancelled(snap))
                return CancelResult();

            while (_writer.CardDetect() != ReaderResponse.DE_OK)
            {
                if (Cancelled(snap))
                    return CancelResult();
                Thread.Sleep(1000);
            }

            if (Cancelled(snap))
                return CancelResult();

            var patientBlock = BuildPatientPayload(snap.HospitalCode, snap.GroupNumber, snap.BatchCurrentId);

            if (_writer.AuthMifare(CardMifareConstants.DefaultMifareKeyA, CardMifareConstants.DataBlock, DualCardUtils.KeyType.TYPE_A))
            {
                Thread.Sleep(800);
                if (!_writer.WriteMifare(patientBlock, CardMifareConstants.DataBlock))
                    return WriteFailResult("Đã có lỗi xảy ra trong quá trình ghi thẻ");
            }
            else
            {
                _writer.CardDetect();
                if (_writer.AuthMifare(CardMifareConstants.MifareKeyA, CardMifareConstants.DataBlock, DualCardUtils.KeyType.TYPE_A))
                {
                    var existing = _writer.ReadMifare(CardMifareConstants.DataBlock);
                    return new CardServiceResult
                    {
                        Success = false,
                        ErrorKind = CardServiceErrorKind.DuplicateCard,
                        UserMessage = "Thẻ đã được định danh\nID: " + existing,
                        IndicatorText = "Thẻ đã được định danh\n ID: " + existing,
                        ExistingCardId = existing
                    };
                }

                return new CardServiceResult
                {
                    Success = false,
                    ErrorKind = CardServiceErrorKind.WriteError,
                    UserMessage = "Không xác thực được thẻ.",
                    IndicatorText = "Không xác thực được thẻ."
                };
            }

            var trailer = CardMifareConstants.MifareKeyA + CardMifareConstants.AccessBit + CardMifareConstants.DefaultMifareKeyB;
            if (!_writer.WriteMifareHex(trailer, CardMifareConstants.DataSectorTrailerBlock))
            {
                return new CardServiceResult
                {
                    Success = false,
                    ErrorKind = CardServiceErrorKind.TrailerError,
                    UserMessage = "Không thể đổi mật khẩu thẻ",
                    IndicatorText = "Không thể đổi mật khẩu thẻ"
                };
            }

            AppendLog(snap.HospitalLogKey, patientBlock, "WRITE");
            var apiRes = _apiClient.CreateOrUpdateCard(snap.HospitalApiId, patientBlock, snap.BatchCode);
            if (!apiRes.Success)
            {
                return new CardServiceResult
                {
                    Success = false,
                    ErrorKind = CardServiceErrorKind.WriteError,
                    UserMessage = "Gọi API thất bại: " + apiRes.StatusCode + " " + apiRes.Message,
                    IndicatorText = "Gọi API thất bại"
                };
            }

            var nextId = snap.BatchCurrentId + 1;
            var inRange = nextId <= snap.BatchLastId;

            return new CardServiceResult
            {
                Success = true,
                IndicatorText = "Thành công",
                NextBatchCurrentId = nextId,
                PromptContinueBatch = inRange,
                BatchRangeCompleted = !inRange
            };
        }

        private CardServiceResult ProcessSingleWrite(CardWorkSnapshot snap, string physicalUid)
        {
            while (_writer.CardDetect() != ReaderResponse.DE_OK)
            {
                if (Cancelled(snap))
                    return CancelResult();
                Thread.Sleep(1000);
            }

            var patientBlock = BuildPatientPayload(snap.HospitalCode, snap.GroupNumber, snap.SinglePatientNumericId);

            if (_writer.AuthMifare(CardMifareConstants.DefaultMifareKeyA, CardMifareConstants.DataBlock, DualCardUtils.KeyType.TYPE_A))
            {
                Thread.Sleep(800);
                if (!_writer.WriteMifare(patientBlock, CardMifareConstants.DataBlock))
                    return WriteFailResult("Đã có lỗi xảy ra trong quá trình ghi thẻ");
            }
            else
            {
                if (Cancelled(snap))
                    return CancelResult();

                _writer.CardDetect();
                if (_writer.AuthMifare(CardMifareConstants.MifareKeyA, CardMifareConstants.DataBlock, DualCardUtils.KeyType.TYPE_A))
                {
                    var existing = _writer.ReadMifare(CardMifareConstants.DataBlock);
                    return new CardServiceResult
                    {
                        Success = false,
                        ErrorKind = CardServiceErrorKind.DuplicateCard,
                        UserMessage = "Thẻ đã được định danh\nID: " + existing,
                        IndicatorText = "Thẻ đã được định danh\n ID: " + existing
                    };
                }

                return new CardServiceResult
                {
                    Success = false,
                    ErrorKind = CardServiceErrorKind.WriteError,
                    UserMessage = "Không xác thực được thẻ."
                };
            }

            var trailer = CardMifareConstants.MifareKeyA + CardMifareConstants.AccessBit + CardMifareConstants.DefaultMifareKeyB;
            if (!_writer.WriteMifareHex(trailer, CardMifareConstants.DataSectorTrailerBlock))
            {
                return new CardServiceResult
                {
                    Success = false,
                    ErrorKind = CardServiceErrorKind.TrailerError,
                    UserMessage = "Không thể đổi mật khẩu thẻ"
                };
            }

            AppendLog(snap.HospitalLogKey, patientBlock, "WRITE_SINGLE");
            var apiRes = _apiClient.CreateOrUpdateCard(snap.HospitalApiId, patientBlock, snap.BatchCode);
            if (!apiRes.Success)
            {
                return new CardServiceResult
                {
                    Success = false,
                    ErrorKind = CardServiceErrorKind.WriteError,
                    UserMessage = "Gọi API thất bại: " + apiRes.StatusCode + " " + apiRes.Message,
                    IndicatorText = "Gọi API thất bại"
                };
            }

            return new CardServiceResult
            {
                Success = true,
                IndicatorText = "Thành công"
            };
        }

        private CardServiceResult ProcessClear(CardWorkSnapshot snap)
        {
            while (_writer.CardDetect() != ReaderResponse.DE_OK)
            {
                if (Cancelled(snap))
                    return CancelResult();
                Thread.Sleep(1000);
            }

            if (!_writer.AuthMifare(CardMifareConstants.MifareKeyA, CardMifareConstants.DataBlock, DualCardUtils.KeyType.TYPE_A))
            {
                return new CardServiceResult
                {
                    Success = false,
                    ErrorKind = CardServiceErrorKind.ClearError,
                    UserMessage = "Không xác thực được thẻ để xóa.",
                    IndicatorText = "Không xác thực được thẻ."
                };
            }

            Thread.Sleep(800);
            var exitingId = _writer.ReadMifare(CardMifareConstants.DataBlock);
            AppendLog(snap.HospitalLogKey, exitingId, "CLEAR");

            if (!_writer.WriteMifareHex("00000000000000000000000000000000", CardMifareConstants.DataBlock))
            {
                return new CardServiceResult
                {
                    Success = false,
                    ErrorKind = CardServiceErrorKind.ClearError,
                    UserMessage = "Đã có lỗi xảy ra trong quá trình ghi thẻ"
                };
            }

            var trailer = CardMifareConstants.DefaultMifareKeyA + CardMifareConstants.AccessBit + CardMifareConstants.DefaultMifareKeyB;
            if (!_writer.WriteMifareHex(trailer, CardMifareConstants.DataSectorTrailerBlock))
            {
                return new CardServiceResult
                {
                    Success = false,
                    ErrorKind = CardServiceErrorKind.TrailerError,
                    UserMessage = "Không thể đổi mật khẩu thẻ"
                };
            }

            return new CardServiceResult
            {
                Success = true,
                IndicatorText = "Thành công"
            };
        }

        private static string BuildPatientPayload(string hospitalCode, int groupNumber, int patientId)
        {
            var g = Math.Max(1, Math.Min(99, groupNumber));
            return hospitalCode + g.ToString("D2", CultureInfo.InvariantCulture)
                   + patientId.ToString("D5", CultureInfo.InvariantCulture);
        }

        private static void AppendLog(string hospitalLogKey, string cardField, string action)
        {
            var filePath = hospitalLogKey + "_log.txt";
            File.AppendAllLines(filePath, new List<string>
            {
                DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy", CultureInfo.InvariantCulture) + "\t|\t" + cardField + "\t|\t" + action
            });
        }

        private static CardServiceResult WriteFailResult(string msg) =>
            new CardServiceResult
            {
                Success = false,
                ErrorKind = CardServiceErrorKind.WriteError,
                UserMessage = msg,
                IndicatorText = "Đã có lỗi xảy ra, ngưng quá trình ghi thẻ"
            };

        private static CardServiceResult CancelResult() =>
            new CardServiceResult { Success = false, ErrorKind = CardServiceErrorKind.Cancelled };
    }
}
