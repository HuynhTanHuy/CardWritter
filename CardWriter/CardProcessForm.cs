using CardWriter.Model;
using Duali;
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
using System.Windows.Forms;

namespace CardWriter
{
    public partial class CardProcessForm : Form
    {
        private const String DEFAULT_DES_KEY = "00000000000000000000000000000000";
        private const String PICC_KEY = "444F4C5048494E534F4C5554494F4E564E4256544E48434D";
        private const String AID_000001_KEY = "414944494e464f524d4154494f4e4040";
        private const String READ_KEY = "414944494e464f524d4154494f4e5240";
        private const String WRITE_KEY = "414944494e464f524d4154494f4e5740";
        private const String RW_KEY = "414944494e464f524d4154494f4e5257";
        private const String CHANGE_ACCESS_KEY = "414944494e464f524d4154494f4e4341";
        private const int TEKMEDI_ID_FILE_ID = 10;
        private const int PATIENT_ID_FILE_ID = 22;
        private const int PATIENT_NAME_FILE_ID = 23;
        private const int HOST_FILE_ID = 24;
        private const int ID_LEN = 36;
        private const int NAME_LEN = 100;
        private const int TEKMEDI_ID_OFFSET = 22;
        private const int OTHER_OFFSET = 11;
        private const String PICC_AID = "000000";
        private const String AID000001_AID = "000001";

        private const String MIFARE_KEY_A = "6C6564616E67";
        private const String DEFAULT_MIFARE_KEY_A = "FFFFFFFFFFFF";
        private const String DEFAULT_MIFARE_KEY_B = "FFFFFFFFFFFF";

        private const String ACCESS_BIT = "FF078069";

        private const int DATA_BLOCK = 14;
        private const int DATA_SECTOR_TRAILER_BLOCK = 15;






        delegate void Function();
        private BackgroundWorker multipleCardWriteWorker;
        private BackgroundWorker loadingIndicatorWorker;
        private BackgroundWorker cardClearWorker;
        private BackgroundWorker singleCardWriteWorker;
        private bool isWriting;

        private Dictionary<String, Hospital> hospitals;
        int initialId = 1;
        int lastId = 0;
        int currentId = 0;
        bool cancel = true;


        public CardProcessForm()
        {
            InitializeComponent();
            InitializeReader();

            label_group.Text = String.Format("{0:00}", int.Parse(textBox_group.Text));

            var section = (Hashtable)ConfigurationManager.GetSection("hospitals");
            hospitals = section.Cast<DictionaryEntry>().ToDictionary(d => (string)d.Key, d => JsonConvert.DeserializeObject<Hospital>((string)d.Value));


            comboBox1.DataSource = new BindingSource(hospitals, null);
            comboBox1.DisplayMember = "Value";
            comboBox1.ValueMember = "Key";

            loadingIndicatorWorker = new BackgroundWorker();
            loadingIndicatorWorker.DoWork += LoadingIndicatorWorker_DoWork;
        }

        private void CreateLogFile(String hospitalCode)
        {
            string filePath = hospitalCode + "_log.txt";

            // Kiểm tra đường dẫn này có tồn tại hay không?
            if (!File.Exists(filePath))
            {
                var logFile = File.Create(filePath);
                logFile.Close();
                File.AppendAllLines(filePath, new List<string>() { "    #### TIME ####    " + "\t|" + "    #### CARD ####     " + "|  #### ACTION ####" });
            }

        }

        private int GetLastedId(String hospitalCode)
        {
            string filePath = hospitalCode + "_log.txt";

            // Kiểm tra đường dẫn này có tồn tại hay không?
            if (File.Exists(filePath))
            {
                // Xóa file
                var lines = File.ReadAllLines(filePath);
                if (lines.Length == 0)
                {
                    return 0;
                }
                string lastIndex = "";
                int length = lines.Length;
                while (length > 0) {
                    String content = lines[--length];
                    try
                    {
                        String lastCard = content.Split('|')[1].Trim();
                        string lastAction = content.Split('|')[2].Trim();
                        if (lastAction == "WRITE")
                        {
                            lastIndex = lastCard.Substring(lastCard.Length - 5, 5);
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

        private void MultipleCardWriterWorker_DoneWork(object sender, RunWorkerCompletedEventArgs e)
        {
            if (currentId <= lastId && !cancel && !isWriting)
            {
                if (MessageBox.Show("Nhấn ok để tiếp tục", "Thành công", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK)
                {
                    multipleCardWriteWorker = new BackgroundWorker();
                    multipleCardWriteWorker.DoWork += MultipleCardWriteWorker_DoWork;
                    multipleCardWriteWorker.RunWorkerCompleted += MultipleCardWriterWorker_DoneWork;
                    multipleCardWriteWorker.RunWorkerAsync();
                }
                else
                {
                    cancel = true;
                    RefreshData();
                    button1.Text = "Start";
                    indicatorImage.Image = Properties.Resources.cardRequestBlinkOn;
                    indicatorLabel.Text = "";
                    button2.Enabled = true;
                    button3.Enabled = true;
                }
            }
            else if (currentId > lastId)
            {
                MessageBox.Show("Đã hoàn tất quá trình ghi thẻ", "Thành công", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                cancel = true;
                RefreshData();
                button1.Text = "Start";
                indicatorImage.Image = Properties.Resources.cardRequestBlinkOn;
                indicatorLabel.Text = "";
                button2.Enabled = true;
                button3.Enabled = true;
            }

           

        }

        public void InitializeReader()
        {
            if (!DualCardUtils.GetInstance().Initialize())
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
                this.Invoke(new Function(delegate () {

                    indicatorLabel.Text = indicatorText;
                    indicatorImage.Image = loadingImage[index];
                    indicatorImage.Refresh();
                }));

                index++;
                if (index >= loadingImage.Count)
                {
                    index = 0;
                }
                Thread.Sleep(200);
            }
        }

        private void MultipleCardWriteWorker_DoWork(object sender, DoWorkEventArgs e)
        {

            while (DualCardUtils.GetInstance().CardDetect() != ReaderResponse.DE_OK)
            {
               
                this.Invoke(new Function(delegate () {
                    indicatorLabel.Text = "Vui lòng đặt thẻ lên thiết bị quét";
                    indicatorImage.Image = Properties.Resources.cardRequestBlinkOn;
                    indicatorImage.Refresh();
                }));

                Thread.Sleep(1000);
                this.Invoke(new Function(delegate () {
                    indicatorImage.Image = Properties.Resources.cardRequestBlinkOff;
                    indicatorImage.Refresh();
                }));


            }


            if (cancel)
            {
                return;
            }

            if (DualCardUtils.GetInstance().AuthMifare(DEFAULT_MIFARE_KEY_A, DATA_BLOCK, DualCardUtils.KeyType.TYPE_A))
            {
                isWriting = true;
                loadingIndicatorWorker.RunWorkerAsync();
                Thread.Sleep(800);
                String id = hospitalCode.Text + String.Format("{0:00}", int.Parse(textBox_group.Text)) + patientCode.Text;
                if (!DualCardUtils.GetInstance().WriteMifare(id, DATA_BLOCK))
                {
                    cancel = true;
                    this.Invoke(new Function(delegate () {
                        MessageBox.Show("Đã có lỗi xảy ra trong quá trình ghi thẻ", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        indicatorLabel.Text = "Đã có lỗi xảy ra, ngưng quá trình ghi thẻ";
                        indicatorImage.Image = Properties.Resources.card_write_error;
                        indicatorImage.Refresh();
                        button2.Enabled = true;
                        button3.Enabled = true;
                        button1.Text = "Start";
                        indicatorImage.Image = Properties.Resources.cardRequestBlinkOn;
                        indicatorLabel.Text = "";
                        isWriting = false;
                        RefreshData();

                    }));
                    return;
                }
            }
            else
            {
                DualCardUtils.GetInstance().CardDetect();
                if (DualCardUtils.GetInstance().AuthMifare(MIFARE_KEY_A, DATA_BLOCK, DualCardUtils.KeyType.TYPE_A))
                {
                    String exitingId = DualCardUtils.GetInstance().ReadMifare(DATA_BLOCK);
                    this.Invoke(new Function(delegate () {
                        MessageBox.Show("Thẻ đã được định danh\nID: " + exitingId, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        indicatorLabel.Text = "Thẻ đã được định danh\n ID: " + exitingId;
                        indicatorImage.Image = Properties.Resources.card_write_error;
                        indicatorImage.Refresh();
                        button2.Enabled = true;
                        button3.Enabled = true;
                    }));

                }
                return;
            }

            if (!DualCardUtils.GetInstance().WriteMifareHex(MIFARE_KEY_A + ACCESS_BIT + DEFAULT_MIFARE_KEY_B, DATA_SECTOR_TRAILER_BLOCK))
            {
                cancel = true;
                this.Invoke(new Function(delegate () {
                    MessageBox.Show("Không thể đổi mật khẩu thẻ", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    indicatorLabel.Text = "Đã có lỗi xảy ra, ngưng quá trình ghi thẻ";
                    indicatorImage.Image = Properties.Resources.card_write_error;
                    indicatorImage.Refresh();
                    button2.Enabled = true;
                    button3.Enabled = true;
                }));
                return;
            }
            this.Invoke(new Function(delegate () {
                String filePath = ((KeyValuePair<string, Hospital>)comboBox1.SelectedItem).Key + "_log.txt";
                File.AppendAllLines(filePath, new List<string>() {DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy") + "\t|\t" + hospitalCode.Text + String.Format("{0:00}", int.Parse(textBox_group.Text)) + patientCode.Text + "\t|\tWRITE" });
                indicatorLabel.Text = "Thành công";
                isWriting = false;
                indicatorImage.Image = Properties.Resources.card_write_done;
                indicatorImage.Refresh();
                currentId++;
                RefreshData();

            }));
           
        }


        private void SingleCardWriteWorker_DoWork(object sender, DoWorkEventArgs e)
        {

            while (DualCardUtils.GetInstance().CardDetect() != ReaderResponse.DE_OK)
            {
                if (cancel)
                {
                    return;
                }
                this.Invoke(new Function(delegate () {
                    indicatorLabel.Text = "Vui lòng đặt thẻ lên thiết bị quét";
                    indicatorImage.Image = Properties.Resources.cardRequestBlinkOn;
                    indicatorImage.Refresh();
                }));

                Thread.Sleep(1000);
                this.Invoke(new Function(delegate () {
                    indicatorImage.Image = Properties.Resources.cardRequestBlinkOff;
                    indicatorImage.Refresh();

                }));


            }



            if (DualCardUtils.GetInstance().AuthMifare(DEFAULT_MIFARE_KEY_A, DATA_BLOCK, DualCardUtils.KeyType.TYPE_A))
            {
                isWriting = true;
                loadingIndicatorWorker.RunWorkerAsync();
                Thread.Sleep(800);
                String id = hospitalCode.Text + String.Format("{0:00}", int.Parse(textBox_group.Text)) + patientCode.Text;
                if (!DualCardUtils.GetInstance().WriteMifare(id, DATA_BLOCK))
                {
                    cancel = true;
                    this.Invoke(new Function(delegate () {
                        MessageBox.Show("Đã có lỗi xảy ra trong quá trình ghi thẻ", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        indicatorLabel.Text = "Đã có lỗi xảy ra, ngưng quá trình ghi thẻ";
                        indicatorImage.Image = Properties.Resources.card_write_error;
                        indicatorImage.Refresh();
                        button1.Enabled = true;
                        button2.Enabled = true;
                        button3.Enabled = true;
                        button1.Text = "Start";
                        indicatorImage.Image = Properties.Resources.cardRequestBlinkOn;
                        indicatorLabel.Text = "";
                        isWriting = false;
                        RefreshData();
                    }));
                    return;
                }

            }
            else
            {
                cancel = true;
                DualCardUtils.GetInstance().CardDetect();
                if (DualCardUtils.GetInstance().AuthMifare(MIFARE_KEY_A, DATA_BLOCK, DualCardUtils.KeyType.TYPE_A))
                {   
                    String exitingId = DualCardUtils.GetInstance().ReadMifare(DATA_BLOCK);
                    this.Invoke(new Function(delegate () {
                        MessageBox.Show("Thẻ đã được định danh\nID: " + exitingId, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        indicatorLabel.Text = "Thẻ đã được định danh\n ID: " + exitingId;
                        indicatorImage.Image = Properties.Resources.card_write_error;
                        indicatorImage.Refresh();
                        button1.Enabled = true;
                        button2.Enabled = true;
                        button3.Enabled = true;
                    }));

                }
                return;
            }

            if (!DualCardUtils.GetInstance().WriteMifareHex(MIFARE_KEY_A + ACCESS_BIT + DEFAULT_MIFARE_KEY_B, DATA_SECTOR_TRAILER_BLOCK))
            {
                cancel = true;
                this.Invoke(new Function(delegate () {
                    MessageBox.Show("Không thể đổi mật khẩu thẻ", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    indicatorLabel.Text = "Đã có lỗi xảy ra, ngưng quá trình ghi thẻ";
                    indicatorImage.Image = Properties.Resources.card_write_error;
                    indicatorImage.Refresh();
                    button1.Enabled = true;
                    button2.Enabled = true;
                    button3.Enabled = true;
                }));
                return;
            }
            this.Invoke(new Function(delegate () {
                String filePath = ((KeyValuePair<string, Hospital>)comboBox1.SelectedItem).Key + "_log.txt";
                File.AppendAllLines(filePath, new List<string>() { DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy") + "\t|\t" + hospitalCode.Text + String.Format("{0:00}", int.Parse(textBox_group.Text)) + patientCode.Text + "\t|\tWRITE_SINGLE" });
                indicatorLabel.Text = "Thành công";
                isWriting = false;
                indicatorImage.Image = Properties.Resources.card_write_done;
                indicatorImage.Refresh();
                button1.Enabled = true;
                button2.Enabled = true;
                button3.Enabled = true;
            }));

            Thread.Sleep(500);

            this.Invoke(new Function(delegate () {
                RefreshData();
                button1.Text = "Start";
                indicatorImage.Image = Properties.Resources.cardRequestBlinkOn;
                indicatorLabel.Text = "";
                cancel = true;
            }));


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

        private void Button1_Click(object sender, EventArgs e)
        {
            if (cancel)
            {
                if (end.Text.Length == 0)
                {
                    lastId = initialId;
                }
                else
                {
                    lastId = int.Parse(end.Text);
                }
                currentId = initialId;
                multipleCardWriteWorker = new BackgroundWorker();
                multipleCardWriteWorker.DoWork += MultipleCardWriteWorker_DoWork;
                multipleCardWriteWorker.RunWorkerCompleted += MultipleCardWriterWorker_DoneWork;
                multipleCardWriteWorker.RunWorkerAsync();
                cancel = false;
                button1.Text = "Stop";
                button2.Enabled = false;
                button3.Enabled = false;
            } else
            {
                cancel = true;
                RefreshData();
                button1.Text = "Start";
                indicatorImage.Image = Properties.Resources.cardRequestBlinkOn;
                indicatorLabel.Text = "";
                button2.Enabled = true;
                button3.Enabled = true;
            }

        }

        private void Button2_Click(object sender, EventArgs e)
        {
            if (cancel)
            {
                int cardIdInteger;
                bool isNumeric = int.TryParse(cardIdInput.Text, out cardIdInteger);

                if (isNumeric && cardIdInteger > 0)
                {
                    singleCardWriteWorker = new BackgroundWorker();
                    singleCardWriteWorker.DoWork += SingleCardWriteWorker_DoWork;
                    singleCardWriteWorker.RunWorkerAsync();
                }
                else 
                {
                    MessageBox.Show("Vui lòng nhập mã thẻ đúng định dạng", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                button1.Enabled = false;
                button3.Enabled = false;
                cancel = false;
                return;
            }


            cancel = true;
            RefreshData();
            button1.Text = "Start";
            indicatorImage.Image = Properties.Resources.cardRequestBlinkOn;
            indicatorLabel.Text = "";
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            if (cancel)
            {
                cardClearWorker = new BackgroundWorker();
                cardClearWorker.DoWork += CardClearWorker_DoWork;
                cardClearWorker.RunWorkerAsync();
                button1.Enabled = false;
                button2.Enabled = false;
                cancel = false;
                return;
            }

            cancel = true;
            RefreshData();
            button1.Text = "Start";
            indicatorImage.Image = Properties.Resources.cardRequestBlinkOn;
            indicatorLabel.Text = "";
        

    }

        private void CardClearWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (DualCardUtils.GetInstance().CardDetect() != ReaderResponse.DE_OK)
            {
                if (cancel)
                {
                    return;
                }
                this.Invoke(new Function(delegate () {
                    indicatorLabel.Text = "Vui lòng đặt thẻ lên thiết bị quét";
                    indicatorImage.Image = Properties.Resources.cardRequestBlinkOn;
                    indicatorImage.Refresh();
                }));

                Thread.Sleep(1000);
                this.Invoke(new Function(delegate () {
                    indicatorImage.Image = Properties.Resources.cardRequestBlinkOff;
                    indicatorImage.Refresh();

                }));


            }



            if (DualCardUtils.GetInstance().AuthMifare(MIFARE_KEY_A, DATA_BLOCK, DualCardUtils.KeyType.TYPE_A))
            {
                isWriting = true;
                loadingIndicatorWorker.RunWorkerAsync();
                Thread.Sleep(800);
                String id = hospitalCode.Text + cardIdInput;
                String exitingId = DualCardUtils.GetInstance().ReadMifare(DATA_BLOCK);
                this.Invoke(new Function(delegate () {
                    String filePath = ((KeyValuePair<string, Hospital>)comboBox1.SelectedItem).Key + "_log.txt";
                    File.AppendAllLines(filePath, new List<string>() { DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy") + "\t|\t" + exitingId + "|\tCLEAR" });
                }));


                if (!DualCardUtils.GetInstance().WriteMifareHex("00000000000000000000000000000000", DATA_BLOCK))
                {
                    cancel = true;
                    this.Invoke(new Function(delegate () {
                        MessageBox.Show("Đã có lỗi xảy ra trong quá trình ghi thẻ", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        indicatorLabel.Text = "Đã có lỗi xảy ra, ngưng quá trình ghi thẻ";
                        indicatorImage.Image = Properties.Resources.card_write_error;
                        indicatorImage.Refresh();
                        button1.Enabled = true;
                        button2.Enabled = true;
                        button3.Enabled = true;

                    }));
                    return;
                }

                if (!DualCardUtils.GetInstance().WriteMifareHex(DEFAULT_MIFARE_KEY_A + ACCESS_BIT + DEFAULT_MIFARE_KEY_B, DATA_SECTOR_TRAILER_BLOCK))
                {
                    cancel = true;
                    this.Invoke(new Function(delegate () {
                        MessageBox.Show("Không thể đổi mật khẩu thẻ", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        indicatorLabel.Text = "Đã có lỗi xảy ra, ngưng quá trình ghi thẻ";
                        indicatorImage.Image = Properties.Resources.card_write_error;
                        indicatorImage.Refresh();
                        button1.Enabled = true;
                        button2.Enabled = true;
                        button3.Enabled = true;

                    }));
                    return;
                }
            }

            
            this.Invoke(new Function(delegate () {
                indicatorLabel.Text = "Thành công";
                isWriting = false;
                indicatorImage.Image = Properties.Resources.card_write_done;
                indicatorImage.Refresh();
                RefreshData();
                button1.Enabled = true;
                button2.Enabled = true;
                button3.Enabled = true;

            }));

            Thread.Sleep(500);

            this.Invoke(new Function(delegate () {
                RefreshData();

                button1.Text = "Start";
                indicatorImage.Image = Properties.Resources.cardRequestBlinkOn;
                indicatorLabel.Text = "";
                cancel = true;
            }));
           
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
            return;
        }

        private void TextBox_group_TextChanged(object sender, EventArgs e)
        {
            if (textBox_group.Text.Length > 2 && textBox_group.Text.StartsWith("0"))
            {
                textBox_group.Text = textBox_group.Text.Remove(0, 1);
            } else if (textBox_group.Text.Length > 2 && !textBox_group.Text.StartsWith("0"))
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
            return;
        }

        private void comboBox1_Format(object sender, ListControlConvertEventArgs e)
        {
            // Assuming your class called Employee , and Firstname & Lastname are the fields
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