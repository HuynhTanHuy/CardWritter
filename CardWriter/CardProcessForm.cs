using CardWriter.Devices;
using CardWriter.Model;
using CardWriter.Services;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CardWriter
{
    public partial class CardProcessForm : Form
    {
        private readonly IRfidReader _reader;
        private readonly CardService _cardService;

        private BackgroundWorker loadingIndicatorWorker;
        private bool isWriting;

        private Dictionary<String, Hospital> hospitals;
        int initialId = 1;
        int lastId = 0;
        int currentId = 0;
        bool cancel = true;

        private CardWorkOperation _activeOperation = CardWorkOperation.None;
        private CancellationTokenSource _listenCts;
        private bool _singleWriteTriggered;

        public CardProcessForm()
            : this(CreateDefaultReader(), new CardService(CreateDefaultWriter(), CreateDefaultApiClient()))
        {
        }

        private static ICardApiClient CreateDefaultApiClient()
        {
            var fakeFlag = ConfigurationManager.AppSettings["UseFakeCardApi"];
            if (string.Equals(fakeFlag, "true", StringComparison.OrdinalIgnoreCase))
                return new FakeCardApiClient();

            var baseUrl = ConfigurationManager.AppSettings["CardApiBaseUrl"] ?? "";
            var bearerToken = ConfigurationManager.AppSettings["CardApiBearerToken"] ?? "";
            var cardTypeId = ConfigurationManager.AppSettings["RfidCardTypeId"] ?? "";
            return new HttpCardApiClient(new System.Net.Http.HttpClient(), baseUrl, bearerToken, cardTypeId);
        }

        private static IRfidWriter CreateDefaultWriter()
        {
            var flag = ConfigurationManager.AppSettings["UseFakeRfidWriter"];
            if (string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase))
                return new FakeRfidWriter();
            return new DualCardRfidWriter();
        }

        private static IRfidReader CreateDefaultReader()
        {
            var flag = ConfigurationManager.AppSettings["UseFakeRfidReader"];
            if (string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase))
                return new FakeRfidReader();
            return new RealRfidReader();
        }

        public CardProcessForm(IRfidReader reader, CardService cardService)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _cardService = cardService ?? throw new ArgumentNullException(nameof(cardService));

            InitializeComponent();
            if (!(reader is FakeRfidReader))
                InitializeReader();

            label_group.Text = String.Format("{0:00}", int.Parse(textBox_group.Text));

            var section = (Hashtable)ConfigurationManager.GetSection("hospitals");
            hospitals = section.Cast<DictionaryEntry>().ToDictionary(d => (string)d.Key, d => JsonConvert.DeserializeObject<Hospital>((string)d.Value));

            comboBox1.DataSource = new BindingSource(hospitals, null);
            comboBox1.DisplayMember = "Value";
            comboBox1.ValueMember = "Key";

            loadingIndicatorWorker = new BackgroundWorker();
            loadingIndicatorWorker.DoWork += LoadingIndicatorWorker_DoWork;

            _reader.CardScanned += Reader_CardScanned;
        }

        private void Reader_CardScanned(object sender, CardScannedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new EventHandler<CardScannedEventArgs>(Reader_CardScanned), sender, e);
                return;
            }

            if (_activeOperation == CardWorkOperation.None)
                return;

            if (_activeOperation == CardWorkOperation.SingleWrite && _singleWriteTriggered)
                return;

            var op = _activeOperation;
            if (op == CardWorkOperation.SingleWrite)
                _singleWriteTriggered = true;
            var snap = BuildSnapshot(op);
            var uid = e.Uid;

            if (op == CardWorkOperation.SingleWrite || op == CardWorkOperation.ClearCard)
                StopListeningSafe();

            isWriting = true;
            if (!loadingIndicatorWorker.IsBusy)
                loadingIndicatorWorker.RunWorkerAsync();

            Task.Run(() =>
            {
                var result = _cardService.Process(snap, uid);
                BeginInvoke(new Action(() => ApplyServiceResult(result, op)));
            });
        }

        private CardWorkSnapshot BuildSnapshot(CardWorkOperation op)
        {
            var selectedItem = (KeyValuePair<string, Hospital>)comboBox1.SelectedItem;
            int group = 1;
            int.TryParse(textBox_group.Text, out group);
            int singleId = 0;
            int.TryParse(cardIdInput.Text, out singleId);

            return new CardWorkSnapshot
            {
                Operation = op,
                IsCancelled = () => cancel,
                HospitalLogKey = selectedItem.Key,
                HospitalApiId = selectedItem.Value.Id,
                HospitalCode = hospitalCode.Text,
                BatchCode = textBox_group.Text.Trim(),
                GroupNumber = group,
                BatchCurrentId = currentId,
                BatchLastId = lastId,
                SinglePatientNumericId = singleId,
                ClearCardIdText = cardIdInput.Text
            };
        }

        private void ApplyServiceResult(CardServiceResult res, CardWorkOperation op)
        {
            isWriting = false;

            if (res.ErrorKind == CardServiceErrorKind.Cancelled)
            {
                ResetAfterStop();
                return;
            }

            if (!res.Success)
            {
                if (!string.IsNullOrEmpty(res.UserMessage))
                    MessageBox.Show(res.UserMessage, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                indicatorLabel.Text = res.IndicatorText ?? res.UserMessage;
                indicatorImage.Image = Properties.Resources.card_write_error;
                indicatorImage.Refresh();

                if (op == CardWorkOperation.BatchWrite)
                {
                    cancel = true;
                    RefreshData();
                    button1.Text = "GHI";
                    button2.Enabled = true;
                    button3.Enabled = true;
                    StopListeningSafe();
                    _activeOperation = CardWorkOperation.None;
                }
                else if (op == CardWorkOperation.SingleWrite)
                {
                    cancel = true;
                    _singleWriteTriggered = false;
                    button1.Enabled = true;
                    button2.Enabled = true;
                    button3.Enabled = true;
                    StopListeningSafe();
                    _activeOperation = CardWorkOperation.None;
                }
                else if (op == CardWorkOperation.ClearCard)
                {
                    cancel = true;
                    _singleWriteTriggered = false;
                    button1.Enabled = true;
                    button2.Enabled = true;
                    button3.Enabled = true;
                    StopListeningSafe();
                    _activeOperation = CardWorkOperation.None;
                }
                return;
            }

            indicatorLabel.Text = res.IndicatorText ?? "Thành công";
            indicatorImage.Image = Properties.Resources.card_write_done;
            indicatorImage.Refresh();

            if (op == CardWorkOperation.BatchWrite && res.NextBatchCurrentId.HasValue)
            {
                currentId = res.NextBatchCurrentId.Value;
                patientCode.Text = String.Format("{0:00000}", currentId);
                start.Text = patientCode.Text;
            }

            if (op == CardWorkOperation.BatchWrite)
            {
                if (res.BatchRangeCompleted)
                {
                    MessageBox.Show("Đã hoàn tất quá trình ghi thẻ", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    FinishBatchUi();
                    return;
                }

                if (res.PromptContinueBatch)
                {
                    if (MessageBox.Show("Nhấn ok để tiếp tục", "Thành công", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK)
                    {
                        indicatorImage.Image = Properties.Resources.cardRequestBlinkOn;
                        indicatorLabel.Text = "";
                    }
                    else
                    {
                        FinishBatchUi();
                    }
                }
                else
                    FinishBatchUi();

                return;
            }

            if (op == CardWorkOperation.SingleWrite)
            {
                Task.Delay(500).ContinueWith(_ => BeginInvoke(new Action(() =>
                {
                    RefreshData();
                    _singleWriteTriggered = false;
                    button1.Enabled = true;
                    button2.Enabled = true;
                    button3.Enabled = true;
                    button1.Text = "GHI";
                    indicatorImage.Image = Properties.Resources.cardRequestBlinkOn;
                    indicatorLabel.Text = "";
                    cancel = true;
                    StopListeningSafe();
                    _activeOperation = CardWorkOperation.None;
                })));
                return;
            }

            if (op == CardWorkOperation.ClearCard)
            {
                Task.Delay(500).ContinueWith(_ => BeginInvoke(new Action(() =>
                {
                    RefreshData();
                    _singleWriteTriggered = false;
                    button1.Enabled = true;
                    button2.Enabled = true;
                    button3.Enabled = true;
                    button1.Text = "GHI";
                    indicatorImage.Image = Properties.Resources.cardRequestBlinkOn;
                    indicatorLabel.Text = "";
                    cancel = true;
                    StopListeningSafe();
                    _activeOperation = CardWorkOperation.None;
                })));
            }
        }

        private void FinishBatchUi()
        {
            cancel = true;
            _singleWriteTriggered = false;
            RefreshData();
            button1.Text = "GHI";
            indicatorImage.Image = Properties.Resources.cardRequestBlinkOn;
            indicatorLabel.Text = "";
            button2.Enabled = true;
            button3.Enabled = true;
            StopListeningSafe();
            _activeOperation = CardWorkOperation.None;
        }

        private void ResetAfterStop()
        {
            _singleWriteTriggered = false;
            RefreshData();
            button1.Text = "GHI";
            indicatorImage.Image = Properties.Resources.cardRequestBlinkOn;
            indicatorLabel.Text = "";
            button2.Enabled = true;
            button3.Enabled = true;
        }

        private void StopListeningSafe()
        {
            _listenCts?.Cancel();
            _listenCts?.Dispose();
            _listenCts = null;
            _reader.StopListening();
        }

        private void CreateLogFile(String hospitalCodePath)
        {
            string filePath = hospitalCodePath + "_log.txt";
            if (!File.Exists(filePath))
            {
                var logFile = File.Create(filePath);
                logFile.Close();
                File.AppendAllLines(filePath, new List<string>() { "    #### TIME ####    " + "\t|" + "    #### CARD ####     " + "|  #### ACTION ####" });
            }
        }

        private int GetLastedId(String hospitalCodePath)
        {
            string filePath = hospitalCodePath + "_log.txt";
            if (File.Exists(filePath))
            {
                var lines = File.ReadAllLines(filePath);
                if (lines.Length == 0)
                    return 0;

                int length = lines.Length;
                while (length > 0)
                {
                    String content = lines[--length];
                    try
                    {
                        String lastCard = content.Split('|')[1].Trim();
                        string lastAction = content.Split('|')[2].Trim();
                        if (lastAction == "WRITE")
                        {
                            string lastIndex = lastCard.Substring(lastCard.Length - 5, 5);
                            return int.Parse(lastIndex);
                        }
                    }
                    catch
                    {
                        return 0;
                    }
                }
            }
            return 0;
        }

        public void InitializeReader()
        {
            if (!Duali.DualCardUtils.GetInstance().Initialize())
            {
                if (MessageBox.Show("Vui lòng kết nối thiết bị ghi thẻ", "Không tìm thấy thiết bị ghi thẻ", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning) == DialogResult.Retry)
                {
                    InitializeReader();
                }
                else
                {
                    this.Close();
                }
            }
        }

        private void LoadingIndicatorWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            List<Image> loadingImage = new List<Image>();
            loadingImage.Add(Properties.Resources.loading_1_a);
            loadingImage.Add(Properties.Resources.loading_2_a);
            loadingImage.Add(Properties.Resources.loading_3_a);
            loadingImage.Add(Properties.Resources.loading_4_a);
            loadingImage.Add(Properties.Resources.loading_5_a);

            int index = 0;
            while (isWriting)
            {
                String indicatorText = "Đang tiến hành ghi thẻ";
                for (int indicatorCount = 0; indicatorCount <= index; indicatorCount++)
                {
                    indicatorText += ".";
                }
                this.Invoke(new Action(() =>
                {
                    indicatorLabel.Text = indicatorText;
                    indicatorImage.Image = loadingImage[index];
                    indicatorImage.Refresh();
                }));

                index++;
                if (index >= loadingImage.Count)
                    index = 0;
                Thread.Sleep(200);
            }
        }

        public void ShowError(String error)
        {
            MessageBox.Show(error);
        }

        private void CardProcessForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isWriting)
            {
                MessageBox.Show("Quá trình ghi thẻ đang được tiến hành, không thể đóng cửa sổ này", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                e.Cancel = true;
                return;
            }
            StopListeningSafe();
            _reader.CardScanned -= Reader_CardScanned;
        }

        private void IndicatorImage_Click(object sender, EventArgs e)
        {
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void RefreshData()
        {
            var selectedItem = (KeyValuePair<string, Hospital>)comboBox1.SelectedItem;
            hospitalCode.Text = selectedItem.Value.Code;
            CreateLogFile(selectedItem.Key);
            initialId = GetLastedId(selectedItem.Key) + 1;
            patientCode.Text = String.Format("{0:00000}", initialId);
            start.Text = String.Format("{0:00000}", initialId);
            cardIdInput.Text = "";
        }

        private bool ValidateWriteInputs(bool requireSingleCardId)
        {
            if (!(comboBox1.SelectedItem is KeyValuePair<string, Hospital> selectedItem) ||
                selectedItem.Value == null ||
                string.IsNullOrWhiteSpace(selectedItem.Value.Id))
            {
                MessageBox.Show("Vui lòng chọn bệnh viện hợp lệ.", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            var batchCode = textBox_group.Text.Trim();
            if (string.IsNullOrWhiteSpace(batchCode))
            {
                MessageBox.Show("Vui lòng nhập Lô.", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBox_group.Focus();
                return false;
            }

            int group;
            if (!int.TryParse(batchCode, out group) || group <= 0)
            {
                MessageBox.Show("Lô phải là số lớn hơn 0.", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBox_group.Focus();
                return false;
            }

            if (requireSingleCardId)
            {
                int cardIdInteger;
                var isNumeric = int.TryParse(cardIdInput.Text, out cardIdInteger);
                if (!isNumeric || cardIdInteger <= 0)
                {
                    MessageBox.Show("Vui lòng nhập mã thẻ đúng định dạng", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    cardIdInput.Focus();
                    return false;
                }
            }

            return true;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            if (cancel)
            {
                if (!ValidateWriteInputs(false))
                    return;

                if (end.Text.Length == 0)
                    lastId = initialId;
                else
                    lastId = int.Parse(end.Text);

                currentId = initialId;
                cancel = false;
                button1.Text = "DỪNG";
                button2.Enabled = false;
                button3.Enabled = false;
                _activeOperation = CardWorkOperation.BatchWrite;
                _singleWriteTriggered = false;

                _listenCts = new CancellationTokenSource();
                _reader.StartListening(_listenCts.Token);
            }
            else
            {
                cancel = true;
                StopListeningSafe();
                _activeOperation = CardWorkOperation.None;
                ResetAfterStop();
            }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            if (cancel)
            {
                if (!ValidateWriteInputs(true))
                    return;

                button1.Enabled = false;
                button2.Enabled = false;
                button3.Enabled = false;
                cancel = false;
                _activeOperation = CardWorkOperation.SingleWrite;
                _singleWriteTriggered = false;

                _listenCts = new CancellationTokenSource();
                _reader.StartListening(_listenCts.Token);
                return;
            }

            cancel = true;
            StopListeningSafe();
            _activeOperation = CardWorkOperation.None;
            RefreshData();
            button1.Text = "GHI";
            indicatorImage.Image = Properties.Resources.cardRequestBlinkOn;
            indicatorLabel.Text = "";
            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            if (cancel)
            {
                button1.Enabled = false;
                button2.Enabled = false;
                cancel = false;
                _activeOperation = CardWorkOperation.ClearCard;

                _listenCts = new CancellationTokenSource();
                _reader.StartListening(_listenCts.Token);
                return;
            }

            cancel = true;
            StopListeningSafe();
            _activeOperation = CardWorkOperation.None;
            RefreshData();
            button1.Text = "GHI";
            indicatorImage.Image = Properties.Resources.cardRequestBlinkOn;
            indicatorLabel.Text = "";
            button1.Enabled = true;
            button2.Enabled = true;
        }

        private void CardIdInput_TextChanged(object sender, EventArgs e)
        {
            if (cardIdInput.Text != "")
            {
                patientCode.Text = String.Format("{0:00000}", int.Parse(cardIdInput.Text));
            }
        }

        private void CardIdInput_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (Char.IsDigit(e.KeyChar) || Char.IsControl(e.KeyChar))
            {
                e.Handled = false;
                return;
            }

            e.Handled = true;
        }

        private void TextBox_group_TextChanged(object sender, EventArgs e)
        {
            if (textBox_group.Text.Length > 2 && textBox_group.Text.StartsWith("0"))
            {
                textBox_group.Text = textBox_group.Text.Remove(0, 1);
            }
            else if (textBox_group.Text.Length > 2 && !textBox_group.Text.StartsWith("0"))
            {
                MessageBox.Show("Chỉ nhập tối đa 2 chữ số", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBox_group.Text = "99";
            }

            if (textBox_group.Text == "" || int.Parse(textBox_group.Text) == 0)
            {
                label_group.Text = "01";
                return;
            }
            label_group.Text = String.Format("{0:00}", int.Parse(textBox_group.Text));
        }

        private void TextBox_group_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (Char.IsDigit(e.KeyChar) || Char.IsControl(e.KeyChar))
            {
                e.Handled = false;
                return;
            }

            e.Handled = true;
        }

        private void comboBox1_Format(object sender, ListControlConvertEventArgs e)
        {
            string hospitalName = ((KeyValuePair<string, Hospital>)e.ListItem).Value.Name;
            e.Value = hospitalName;
        }

        private void start_TextChanged(object sender, EventArgs e)
        {
            if (start.Text != "" && cardIdInput.Text == "")
            {
                patientCode.Text = String.Format("{0:00000}", int.Parse(start.Text));
            }
        }
    }
}
