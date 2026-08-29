namespace WinFormsApp2
{
    public partial class Form1 : Form
    {
        private Main mainForm;
        private ApiService _apiService = new ApiService();
        private FormResizer formResizer;

        public Form1()
        {
            InitializeComponent();
            
            // Form boyutlandırıcıyı başlat
            formResizer = new FormResizer(this);
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            var user = await _apiService.LoginAsync(textBox1.Text, textBox2.Text);
            
                

                
                if (user != null)
                {
                    mainForm = new Main();
                    mainForm.CurrentUser = user; // Giriş yapan kullanıcıyı aktar
                    mainForm.Owner = this;
                    mainForm.Show();
                    this.Hide(); // Giriş ekranını gizle
                }
                else
                {
                    MessageBox.Show("Kullanıcı adı veya şifre yanlış!");
                }
            
        }

        public Main GetMainForm()
        {
            return mainForm;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            UserCreate userCreateForm = new UserCreate();
            userCreateForm.Show();
            this.Hide(); // Giriş ekranını gizle
        }

        private void button2_Click(object sender, EventArgs e)
        {
            UserCreate userCreateForm = new UserCreate();
            userCreateForm.Show();
            this.Hide();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
           
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; 
                textBox2.Focus(); 
            }
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {   
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                button1_Click(sender, e);
            }
        }
    }
}
