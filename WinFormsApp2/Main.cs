using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace WinFormsApp2
{
    public partial class Main : Form
    {
        private ApiService _apiService = new ApiService();
        private string videoPath;
        private FormResizer formResizer;
        private ResponsiveLayout responsiveLayout;
        private int _lastPanelWidth = -1;
        private int _lastPanelHeight = -1;
        private Size _originalFlowPanelSize;
        private Size _originalRichTextSize;

        public Main()
        {
            InitializeComponent();
            
            // Orijinal boyutları sakla
            _originalFlowPanelSize = flowLayoutPanel1.Size;
            _originalRichTextSize = richTextBox1.Size;

            // Form boyutlandırıcıyı başlat
            formResizer = new FormResizer(this);
            
            // Responsive layout'u başlat
            responsiveLayout = new ResponsiveLayout(this);
            
            // Özel boyutlandırma kuralları ekle
            SetupCustomLayoutRules();

            // Form boyutu değişince mesajları yeniden ölçekle (sadece gerçek değişimde)
            this.Resize += (s, e) => TryRefreshMessagesOnPanelResize();
            flowLayoutPanel1.SizeChanged += (s, e) => TryRefreshMessagesOnPanelResize();
        }

        private void TryRefreshMessagesOnPanelResize()
        {
            if (!flowLayoutPanel1.Visible)
                return;
            if (_lastPanelWidth != flowLayoutPanel1.Width || _lastPanelHeight != flowLayoutPanel1.Height)
            {
                _lastPanelWidth = flowLayoutPanel1.Width;
                _lastPanelHeight = flowLayoutPanel1.Height;
                _ = RefreshMessages();
            }
        }

        public User CurrentUser { get; set; } // EfCoreExample.User yerine User kullan
        public string selectedUserName;

        private async void Main_Load(object sender, EventArgs e)
        {


            
            await LoadUsers(); // async eklendi

            typeof(FlowLayoutPanel).InvokeMember(
    "DoubleBuffered",
    System.Reflection.BindingFlags.SetProperty |
    System.Reflection.BindingFlags.Instance |
    System.Reflection.BindingFlags.NonPublic,
    null,
    flowLayoutPanel1,
    new object[] { true });

            // Event handler'ları manuel olarak ekle
            comboBox1.SelectedIndexChanged += ComboBox1_SelectedIndexChanged;
            listView1.SelectedIndexChanged += listView1_SelectedIndexChanged;

            // Eğer DM'de kullanıcı varsa ilkini seç
            if (listView1.Items.Count > 0)
            {
                listView1.Items[0].Selected = true;
                selectedUserName = listView1.Items[0].Text;
            }
        }

        private async Task LoadUsers() // async eklendi
        {
            try
            {
                var users = await _apiService.GetUsersAsync();
                if (CurrentUser == null) return;

                // DM'deki kullanıcıları al
                var dmUsers = await GetDMUsers(); // async eklendi

                // ComboBox: DM'de olmayan kullanıcıları göster
                comboBox1.Items.Clear();
                var allUsers = users.Where(u => u.Gid != CurrentUser.Gid).ToList();

                foreach (var user in allUsers)
                {
                    // Eğer bu kullanıcı DM'de yoksa ComboBox'a ekle
                    if (!dmUsers.Any(dm => dm.Gid == user.Gid))
                    {
                        comboBox1.Items.Add(user.UserName);
                    }
                }

                // ListView: DM'deki kullanıcıları göster (sadece adları)
                listView1.Clear();
                listView1.View = View.Details;
                listView1.Columns.Clear();
                listView1.Columns.Add("Kullanıcı Adı", 200);

                foreach (var dmUser in dmUsers)
                {
                    var item = new ListViewItem(dmUser.UserName);
                    listView1.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading users: {ex.Message}");
            }
        }

        private async Task<List<User>> GetDMUsers() // async eklendi
        {
            try
            {
                if (CurrentUser == null) return new List<User>();

                var messages = await _apiService.GetMessagesAsync();

                // Mevcut kullanıcı ile mesajlaşmış olan kullanıcıları bul
                var dmUserIds = messages
                    .Where(m => m.SenderId == CurrentUser.Gid || m.ReceiverId == CurrentUser.Gid)
                    .Select(m => m.SenderId == CurrentUser.Gid ? m.ReceiverId : m.SenderId)
                    .Distinct()
                    .ToList();

                var users = await _apiService.GetUsersAsync();
                return users.Where(u => dmUserIds.Contains(u.Gid)).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting DM users: {ex.Message}");
                return new List<User>();
            }
        }

        private async void ComboBox1_SelectedIndexChanged(object sender, EventArgs e) // async eklendi
        {
            try
            {
                if (comboBox1.SelectedItem != null)
                {
                    selectedUserName = comboBox1.SelectedItem.ToString();

                    // Bu kullanıcıyı DM listesine eklemek için bir mesaj gönder
                    var users = await _apiService.GetUsersAsync();
                    var selectedUser = users.FirstOrDefault(u => u.UserName == selectedUserName);

                    if (selectedUser != null)
                    {
                        // Eğer bu kullanıcı ile hiç mesaj yoksa, boş bir mesaj ekle
                        var messages = await _apiService.GetMessagesAsync();
                        var existingMessages = messages
                            .Where(m => (m.SenderId == CurrentUser.Gid && m.ReceiverId == selectedUser.Gid) ||
                                      (m.SenderId == selectedUser.Gid && m.ReceiverId == CurrentUser.Gid))
                            .Count();

                        if (existingMessages == 0)
                        {
                            // Boş bir mesaj ekle ki DM listesine çıksın
                            var emptyMessage = new Message
                            {
                                Content = Program.Encoder(""),
                                Timestamp = DateTime.Now,
                                SenderId = CurrentUser.Gid,
                                ReceiverId = selectedUser.Gid,
                                Seen = true
                            };

                            await _apiService.SendMessageAsync(emptyMessage);
                        }
                    }

                    // Chat'i yenile
                    await RefreshMessages(); // async eklendi
                    await MarkMessagesAsSeen(); // async eklendi

                    // DM listesini yenile
                    await LoadUsers(); // async eklendi
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in ComboBox selection: {ex.Message}");
            }
        }

        private async Task MarkMessagesAsSeen() // async eklendi
        {
            try
            {
                if (CurrentUser != null && selectedUserName != null)
                {
                    var users = await _apiService.GetUsersAsync();
                    var selectedUser = users.FirstOrDefault(u => u.UserName == selectedUserName);

                    if (selectedUser != null)
                    {
                        var messages = await _apiService.GetMessagesAsync();

                        // Bize gelen ve henüz görülmemiş mesajları görüldü olarak işaretle
                        var unreadMessages = messages
                            .Where(m => m.SenderId == selectedUser.Gid &&
                                      m.ReceiverId == CurrentUser.Gid &&
                                      !m.Seen)
                            .ToList();

                        foreach (var message in unreadMessages)
                        {
                            await _apiService.MarkMessageAsSeenAsync(message.Id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error marking messages as seen: {ex.Message}");
            }
        }

        public async Task RefreshMessages() // async eklendi
        {
            try
            {
                // Chat alanını temizle
                flowLayoutPanel1.Controls.Clear();
                flowLayoutPanel1.AutoScroll = true;
                flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;

                // Orijinal designer boyutuna göre scale hesapla
                float scale = (float)flowLayoutPanel1.Width / _originalFlowPanelSize.Width;
                int maxPanelWidth = (int)Math.Min(flowLayoutPanel1.Width, this.Width * 0.8f);
                int maxRichTextWidth = (int)Math.Min(richTextBox1.Width, this.Width * 0.8f);
                int minPanelWidth = 200;

                // flowLayoutPanel1 ve richTextBox1'in boyutunu scale ile ayarla
                flowLayoutPanel1.Width = Math.Max(minPanelWidth, (int)(_originalFlowPanelSize.Width * scale));
                flowLayoutPanel1.Height = (int)(_originalFlowPanelSize.Height * scale);
                richTextBox1.Width = Math.Max(100, (int)(_originalRichTextSize.Width * scale));
                richTextBox1.Height = (int)(_originalRichTextSize.Height * scale);

                if (CurrentUser != null)
                {
                    var users = await _apiService.GetUsersAsync();
                    var messages = await _apiService.GetMessagesAsync();

                    // Seçili kullanıcı ile olan mesajları al
                    if (selectedUserName == null)
                    {
                        if (listView1.SelectedItems.Count == 0)
                            return;

                        selectedUserName = listView1.SelectedItems[0].Text;
                    }

                    if (!string.IsNullOrEmpty(selectedUserName))
                    {
                        var selectedUser = users.FirstOrDefault(u => u.UserName == selectedUserName);
                        if (selectedUser != null)
                        {
                            // İki kullanıcı arasındaki mesajları al ve tarihe göre sırala
                            var conversationMessages = messages
                                .Where(m => (m.SenderId == CurrentUser.Gid && m.ReceiverId == selectedUser.Gid) ||
                                          (m.SenderId == selectedUser.Gid && m.ReceiverId == CurrentUser.Gid))
                                .OrderBy(m => m.Timestamp)
                                .ToList();

                            DateTime now = DateTime.Now;
                            DateTime temp = DateTime.MinValue; // Geçici tarih değişkeni
                            foreach (var message in conversationMessages)
                            {
                                // Mesajı çöz
                                string decodedContent = Program.Decoder(message.Content);

                                // Boş mesajları gösterme
                                if (string.IsNullOrWhiteSpace(decodedContent))
                                    continue;

                                DateTime timestamp = message.Timestamp;

                                string zamanMetni;
                                if (temp.Date != timestamp.Date)
                                {
                                    if (timestamp.Date == now.Date)
                                    {
                                        // Bugün
                                        zamanMetni = "Bugün";
                                    }
                                    else if (timestamp.Date == now.Date.AddDays(-1))
                                    {
                                        // Dün
                                        zamanMetni = "Dün ";
                                    }
                                    else
                                    {
                                        // Daha eski
                                        zamanMetni = timestamp.ToString("dd.MM.yyyy");
                                    }

                                    Label timeBox = new Label();
                                    timeBox.Text = zamanMetni;
                                    timeBox.BorderStyle = BorderStyle.None;
                                    timeBox.BackColor = Color.DarkGray;
                                    timeBox.Font = new Font("Arial", 10 * scale);
                                    timeBox.ForeColor = Color.White;
                                    timeBox.TextAlign = ContentAlignment.MiddleCenter;

                                    // TimeBox'ı ortada konumlandır
                                    int timeBoxWidth = (int)Math.Min(200 * scale, Math.Max(100 * scale, zamanMetni.Length * 8 * scale));
                                    timeBox.Width = timeBoxWidth;
                                    timeBox.Height = (int)(30 * scale);

                                    // Panel oluştur ve timeBox'ı ortala
                                    Panel timePanel = new Panel();
                                    timePanel.Width = Math.Max(minPanelWidth, Math.Min((int)(flowLayoutPanel1.Width - 20 * scale), maxPanelWidth));
                                    timePanel.Height = (int)(40 * scale);
                                    timePanel.Margin = new Padding((int)(5 * scale));

                                    // TimeBox'ı panelin ortasına yerleştir
                                    timeBox.Location = new Point((timePanel.Width - timeBox.Width) / 2, (int)(5 * scale));

                                    timePanel.Controls.Add(timeBox);
                                    flowLayoutPanel1.Controls.Add(timePanel);
                                }

                                // Mesaj paneli oluştur
                                Panel messagePanel = new Panel();
                                messagePanel.Width = Math.Max(minPanelWidth, Math.Min((int)(flowLayoutPanel1.Width - 20 * scale), maxPanelWidth));
                                messagePanel.Height = (int)(60 * scale);
                                messagePanel.Margin = new Padding((int)(5 * scale));

                                bool isImage = decodedContent.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                 decodedContent.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                                 decodedContent.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                 decodedContent.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase);

                                bool isVideo = decodedContent.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                                 decodedContent.EndsWith(".avi", StringComparison.OrdinalIgnoreCase) ||
                                 decodedContent.EndsWith(".mov", StringComparison.OrdinalIgnoreCase) ||
                                 decodedContent.EndsWith(".wmv", StringComparison.OrdinalIgnoreCase) ||
                                 decodedContent.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase);

                                bool isYouTube = decodedContent.Contains("youtube.com/shorts") ||
                                decodedContent.Contains("youtube.com/watch");

                                // Tarih etiketi
                                Label timeLabel = new Label();
                                timeLabel.Text = message.Timestamp.ToString("HH:mm");
                                timeLabel.Font = new Font("Arial", 8 * scale);
                                timeLabel.ForeColor = Color.Gray;
                                timeLabel.AutoSize = true;

                                // Görüldü durumu (sadece gönderilen mesajlar için)
                                Label seenLabel = new Label();
                                if (message.SenderId == CurrentUser.Gid)
                                {
                                    seenLabel.Text = message.Seen ? "✓✓" : "✓"; // Çift tik = görüldü, tek tik = gönderildi
                                    seenLabel.Font = new Font("Arial", 8 * scale);
                                    seenLabel.ForeColor = message.Seen ? Color.Blue : Color.Gray;
                                    seenLabel.AutoSize = true;
                                }

                                if (isImage && File.Exists(decodedContent))
                                {
                                    // Resim içeriği
                                    PictureBox picBox = new PictureBox();
                                    picBox.Image = Image.FromFile(decodedContent);
                                    picBox.SizeMode = PictureBoxSizeMode.Zoom;
                                    picBox.Width = (int)(200 * scale);
                                    picBox.Height = (int)(150 * scale);
                                    picBox.BorderStyle = BorderStyle.FixedSingle;

                                    if (message.SenderId == CurrentUser.Gid)
                                        picBox.Location = new Point(messagePanel.Width - picBox.Width - (int)(10 * scale), (int)(5 * scale));
                                    else
                                        picBox.Location = new Point((int)(10 * scale), (int)(5 * scale));

                                    messagePanel.Controls.Add(picBox);
                                    messagePanel.Height = picBox.Height + (int)(25 * scale);

                                    // Mesajın sağda mı solda mı olacağını belirle
                                    if (message.SenderId == CurrentUser.Gid)
                                    {
                                        picBox.Location = new Point(messagePanel.Width - picBox.Width - (int)(10 * scale), (int)(5 * scale));
                                        timeLabel.Location = new Point(picBox.Right - timeLabel.Width, picBox.Bottom + (int)(2 * scale));
                                        seenLabel.Location = new Point(timeLabel.Left - (int)(20 * scale), timeLabel.Top);
                                    }
                                    else
                                    {
                                        picBox.Location = new Point((int)(10 * scale), (int)(5 * scale));
                                        timeLabel.Location = new Point(picBox.Left, picBox.Bottom + (int)(2 * scale));
                                    }

                                    messagePanel.Controls.Add(picBox);
                                    messagePanel.Controls.Add(timeLabel);
                                    if (message.SenderId == CurrentUser.Gid)
                                    {
                                        messagePanel.Controls.Add(seenLabel);
                                    }

                                    flowLayoutPanel1.Controls.Add(messagePanel);
                                    temp = message.Timestamp;
                                }
                                else if (isVideo && File.Exists(decodedContent))
                                {
                                    // 1- Oluştur
                                    AxWMPLib.AxWindowsMediaPlayer videoPlayer = new AxWMPLib.AxWindowsMediaPlayer();
                                    ((ISupportInitialize)videoPlayer).BeginInit();

                                    // 2- Ekle
                                    messagePanel.Controls.Add(videoPlayer);

                                    // 3- EndInit sonrası ayarlar
                                    ((ISupportInitialize)videoPlayer).EndInit();

                                    // 4- URL ve diğer ayarlar
                                    videoPlayer.settings.autoStart = false;
                                    videoPlayer.URL = decodedContent;
                                    videoPlayer.uiMode = "mini"; // sade mod
                                    videoPlayer.stretchToFit = true;
                                    videoPlayer.Width = (int)(200 * scale);
                                    videoPlayer.Height = (int)(150 * scale);

                                    if (message.SenderId == CurrentUser.Gid)
                                        videoPlayer.Location = new Point(messagePanel.Width - videoPlayer.Width - (int)(10 * scale), (int)(5 * scale));
                                    else
                                        videoPlayer.Location = new Point((int)(10 * scale), (int)(5 * scale));

                                    messagePanel.Controls.Add(videoPlayer);
                                    messagePanel.Height = videoPlayer.Height + (int)(25 * scale);

                                    // Mesajın sağda mı solda mı olacağını belirle
                                    if (message.SenderId == CurrentUser.Gid)
                                    {
                                        videoPlayer.Location = new Point(messagePanel.Width - videoPlayer.Width - (int)(10 * scale), (int)(5 * scale));
                                        timeLabel.Location = new Point(videoPlayer.Right - timeLabel.Width, videoPlayer.Bottom + (int)(2 * scale));
                                        seenLabel.Location = new Point(timeLabel.Left - (int)(20 * scale), timeLabel.Top);
                                    }
                                    else
                                    {
                                        videoPlayer.Location = new Point((int)(10 * scale), (int)(5 * scale));
                                        timeLabel.Location = new Point(videoPlayer.Left, videoPlayer.Bottom + (int)(2 * scale));
                                    }

                                    messagePanel.Controls.Add(timeLabel);
                                    if (message.SenderId == CurrentUser.Gid)
                                    {
                                        messagePanel.Controls.Add(seenLabel);
                                    }

                                    flowLayoutPanel1.Controls.Add(messagePanel);
                                    temp = message.Timestamp;
                                }
                                else if(isYouTube)
                                {
                                    string videoId = GetYouTubeVideoId(decodedContent);
                                    if (!string.IsNullOrEmpty(videoId))
                                    {
                                        var webView = new Microsoft.Web.WebView2.WinForms.WebView2();
                                        await webView.EnsureCoreWebView2Async(null);
                                        webView.Source = new Uri($"https://www.youtube.com/embed/{videoId}?autoplay=0&rel=0");
                                        webView.Size = new Size((int)(300 * scale), (int)(200 * scale));
                                        webView.Location = new Point(10, 5);

                                        messagePanel.Controls.Add(webView);
                                        messagePanel.Height = webView.Height + 25;

                                        // Mesajın sağda mı solda mı olacağını belirle
                                        if (message.SenderId == CurrentUser.Gid)
                                        {
                                            // Kendi mesajımız - sağda
                                            webView.Location = new Point(messagePanel.Width - webView.Width - (int)(10 * scale), (int)(5 * scale));
                                            timeLabel.Location = new Point(webView.Right - timeLabel.Width, webView.Bottom + (int)(2 * scale));
                                            seenLabel.Location = new Point(timeLabel.Left - (int)(20 * scale), timeLabel.Top);
                                        }
                                        else
                                        {
                                            // Karşı tarafın mesajı - solda
                                            webView.Location = new Point((int)(10 * scale), (int)(5 * scale));
                                            timeLabel.Location = new Point(webView.Left, webView.Bottom + (int)(2 * scale));
                                        }

                                        messagePanel.Controls.Add(webView);
                                        messagePanel.Controls.Add(timeLabel);
                                        if (message.SenderId == CurrentUser.Gid)
                                        {
                                            messagePanel.Controls.Add(seenLabel);
                                        }

                                        flowLayoutPanel1.Controls.Add(messagePanel);
                                        temp = message.Timestamp;
                                    }
                                }
                                else
                                {
                                    // Mesaj kutusu
                                    Label messageBox = new Label();
                                    messageBox.Text = decodedContent;
                                    messageBox.BorderStyle = BorderStyle.None;
                                    messageBox.BackColor = message.SenderId == CurrentUser.Gid ? Color.LightBlue : Color.LightGray;
                                    messageBox.Font = new Font("Arial", 10 * scale);

                                    // Mesaj boyutunu ayarla
                                    int baseMessageWidth = 200;
                                    int messageWidth = (int)(baseMessageWidth * scale);
                                    messageBox.Width = messageWidth;
                                    messageBox.Height = (int)(40 * scale);

                                    // Mesajın sağda mı solda mı olacağını belirle
                                    if (message.SenderId == CurrentUser.Gid)
                                    {
                                        // Kendi mesajımız - sağda
                                        messageBox.Location = new Point(messagePanel.Width - messageBox.Width - (int)(10 * scale), (int)(5 * scale));
                                        timeLabel.Location = new Point(messageBox.Right - timeLabel.Width, messageBox.Bottom + (int)(2 * scale));
                                        seenLabel.Location = new Point(timeLabel.Left - (int)(20 * scale), timeLabel.Top);
                                    }
                                    else
                                    {
                                        // Karşı tarafın mesajı - solda
                                        messageBox.Location = new Point((int)(10 * scale), (int)(5 * scale));
                                        timeLabel.Location = new Point(messageBox.Left, messageBox.Bottom + (int)(2 * scale));
                                    }

                                    messagePanel.Controls.Add(messageBox);
                                    messagePanel.Controls.Add(timeLabel);
                                    if (message.SenderId == CurrentUser.Gid)
                                    {
                                        messagePanel.Controls.Add(seenLabel);
                                    }

                                    flowLayoutPanel1.Controls.Add(messagePanel);
                                    temp = message.Timestamp;
                                }
                            }

                            // En alta kaydır
                            if (flowLayoutPanel1.Controls.Count > 0)
                            {
                                flowLayoutPanel1.ScrollControlIntoView(flowLayoutPanel1.Controls[flowLayoutPanel1.Controls.Count - 1]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing messages: {ex.Message}");
            }
        }

        private async void button1_Click(object sender, EventArgs e) // async eklendi
        {
            try
            {
                // Yeni chat başlatma - ComboBox'ı temizle ve kullanıcıları yenile
                comboBox1.SelectedItem = null;
                await LoadUsers(); // async eklendi

                // Chat alanını temizle
                flowLayoutPanel1.Controls.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in button1: {ex.Message}");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Form1 form1 = new Form1();
            form1.Show();
            this.Hide();
        }

        private async void listView1_SelectedIndexChanged(object sender, EventArgs e) // async eklendi
        {
            try
            {
                if (listView1.SelectedItems.Count > 0)
                {
                    selectedUserName = listView1.SelectedItems[0].Text;

                    // ComboBox'ta bu kullanıcıyı seç
                    comboBox1.SelectedItem = selectedUserName;

                    // Chat'i yenile
                    await RefreshMessages(); // async eklendi
                    await MarkMessagesAsSeen(); // async eklendi
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in listView selection: {ex.Message}");
            }
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            // Enter tuşu ile mesaj gönderme
            if (richTextBox1.Text.Contains("\n"))
            {
                richTextBox1.Text = richTextBox1.Text.Replace("\n", "");

                // Eğer kullanıcı seçili değilse uyarı ver
                if (selectedUserName == null)
                {
                    MessageBox.Show("Lütfen önce bir kullanıcı seçin!");
                    return;
                }

                button3_Click(sender, e);
            }
        }

        private async void button3_Click(object sender, EventArgs e) // async eklendi
        {
            try
            {
                if (CurrentUser == null)
                {
                    MessageBox.Show("Giriş yapan kullanıcı bulunamadı.");
                    return;
                }

                // Seçili kullanıcıyı al
                if (selectedUserName == null)
                {
                    if (listView1.SelectedItems.Count == 0)
                    {
                        MessageBox.Show("Lütfen bir kullanıcı seçin.");
                        return;
                    }
                    selectedUserName = listView1.SelectedItems[0].Text;
                }

                var users = await _apiService.GetUsersAsync();
                var receiver = users.FirstOrDefault(u => u.UserName == selectedUserName);
                var content = "";
                string encrypted = null;
                var message = new Message();
                if (receiver == null)
                {
                    MessageBox.Show("Alıcı bulunamadı.");
                    return;
                }
                if (pictureBox1.Image != null)
                {
                    string saveDir = @"C:\YourAppImages";
                    Directory.CreateDirectory(saveDir);

                    string fileName = "img_" + DateTime.Now.Ticks + ".png"; // veya ".jpg"
                    content = Path.Combine(saveDir, fileName);

                    // Resmi PNG formatında kaydet
                    pictureBox1.Image.Save(content, System.Drawing.Imaging.ImageFormat.Png);


                    //MessageBox.Show("Resim kaydedildi: " + content);



                    encrypted = Program.Encoder(content);
                    message = new Message
                    {
                        Content = Program.Encoder(content),
                        Timestamp = DateTime.Now,
                        SenderId = CurrentUser.Gid,
                        ReceiverId = receiver.Gid,
                        Seen = false
                    };

                    var resultpicture = await _apiService.SendMessageAsync(message);


                }

                if(videoPath!=null)
                {
                    string saveDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Media", "Videos");
                    Directory.CreateDirectory(saveDir);

                    string fileName = "vid_" + DateTime.Now.Ticks + ".mp4"; // veya ".jpg"
                    content = Path.Combine(saveDir, fileName);

                    File.Copy(videoPath, content, true);


                    //MessageBox.Show("Resim kaydedildi: " + content);



                    encrypted = Program.Encoder(content);
                    message = new Message
                    {
                        Content = Program.Encoder(content),
                        Timestamp = DateTime.Now,
                        SenderId = CurrentUser.Gid,
                        ReceiverId = receiver.Gid,
                        Seen = false
                    };

                    var resultvideo = await _apiService.SendMessageAsync(message);


                }



                content = richTextBox1.Text;
                if (string.IsNullOrWhiteSpace(content) && pictureBox1.Image == null && videoPath==null)
                {
                    MessageBox.Show("Mesaj boş olamaz.");
                    return;
                }
                pictureBox1.Image = null;


                encrypted = Program.Encoder(content);
                message = new Message
                {
                    Content = encrypted,
                    Timestamp = DateTime.Now,
                    SenderId = CurrentUser.Gid,
                    ReceiverId = receiver.Gid,
                    Seen = false
                };

                var result = await _apiService.SendMessageAsync(message);

                if (result != null)
                {
                    // Mesajları yenile
                    await RefreshMessages(); // async eklendi
                    richTextBox1.Clear();

                    // DM listesini yenile
                    await LoadUsers(); // async eklendi
                }
                else
                {
                    MessageBox.Show("Mesaj gönderilemedi!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending message: {ex.Message}");
            }
        }

        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Bir resim seçin";
            ofd.Filter = "Resim Dosyaları|*.jpg;*.jpeg;*.png;*.bmp;*.gif";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string selectedPath = ofd.FileName;

                // Örneğin resmi bir PictureBox'ta göster:
                try
                {
                    pictureBox1.Image = Image.FromFile(selectedPath);
                }
                catch (Exception ex)
                { }

                /*  // İsteğe bağlı: resmi kendi klasörüne kopyala
                 string saveDir = @"C:\YourAppImages";
                 Directory.CreateDirectory(saveDir);
                 string fileName = "img_" + DateTime.Now.Ticks + Path.GetExtension(selectedPath);
                 string newPath = Path.Combine(saveDir, fileName);
                 File.Copy(selectedPath, newPath);  

                 // Veritabanına bu path'i kaydet
                 MessageBox.Show("Resim kopyalandı: " + newPath); */


            }

        }

        private void richTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.V)
            {
                if (Clipboard.ContainsImage())
                {
                    e.SuppressKeyPress = true;
                    Image img = Clipboard.GetImage();
                    pictureBox1.Image = img;

                    /*   // Diske kaydet
                       string folder = @"C:\YourAppImages";
                       Directory.CreateDirectory(folder);
                       string fileName = "img_" + DateTime.Now.Ticks + ".png";
                       string fullPath = Path.Combine(folder, fileName);
                       img.Save(fullPath, System.Drawing.Imaging.ImageFormat.Png);

                       // Veritabanına bu path'i kaydedebilirsin
                       MessageBox.Show("Resim yapıştırıldı ve kaydedildi:\n" + fullPath);

                       // İsteğe bağlı: RichTextBox'a resim yerine metin koy
                       e.SuppressKeyPress = true;  // Ctrl+V işlemini engelle
                       richTextBox1.AppendText("[Resim yapıştırıldı]\n");  */
                }
            }
        }

        private void flowLayoutPanel1_Scroll(object sender, ScrollEventArgs e)
        {



        }

        private void button5_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Bir video seçin";
            ofd.Filter = "Video Dosyaları|*.mp4;*.avi;*.mkv;*.mov;*.wmv";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                videoPath = ofd.FileName;

               

                /*  // İsteğe bağlı: resmi kendi klasörüne kopyala
                 string saveDir = @"C:\YourAppImages";
                 Directory.CreateDirectory(saveDir);
                 string fileName = "img_" + DateTime.Now.Ticks + Path.GetExtension(selectedPath);
                 string newPath = Path.Combine(saveDir, fileName);
                 File.Copy(selectedPath, newPath);  

                 // Veritabanına bu path'i kaydet
                 MessageBox.Show("Resim kopyalandı: " + newPath); */


            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            OpenTestForm();
        }

        private void SetupCustomLayoutRules()
        {
            // FlowLayoutPanel için özel kural - genişliği form genişliğine göre ayarla
            responsiveLayout.AddCustomRule(flowLayoutPanel1, (formSize, newLocation, newSize) =>
            {
                return new Size(newSize.Width, newSize.Height);
            });

            // RichTextBox için özel kural - genişliği form genişliğine göre ayarla
            responsiveLayout.AddCustomRule(richTextBox1, (formSize, newLocation, newSize) =>
            {
                return new Size(newSize.Width, newSize.Height);
            });
        }

        // Test formunu açmak için metod
        private void OpenTestForm()
        {
            TestResponsiveForm testForm = new TestResponsiveForm();
            testForm.Show();
        }

        public void ResetToOriginalSizeAndPanels()
        {
            this.Size = formResizer != null ? formResizer.GetOriginalFormSize() : this.Size;
            flowLayoutPanel1.Size = _originalFlowPanelSize;
            richTextBox1.Size = _originalRichTextSize;
        }

        string GetYouTubeVideoId(string url)
        {
            if (url.Contains("youtube.com/watch"))
            {
                // Normal watch linki
                return url.Split(new[] { "v=" }, StringSplitOptions.None)[1].Split('&')[0];
            }
            else if (url.Contains("youtube.com/shorts/"))
            {
                // Shorts linki
                return url.Split(new[] { "shorts/" }, StringSplitOptions.None)[1].Split('?')[0];
            }
            else if (url.Contains("youtu.be/"))
            {
                // Kısa youtu.be linki
                return url.Split(new[] { "youtu.be/" }, StringSplitOptions.None)[1].Split('?')[0];
            }

            return null;
        }

    }
}
