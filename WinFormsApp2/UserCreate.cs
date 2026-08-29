using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsApp2
{
    public partial class UserCreate : Form
    {
        private ApiService _apiService = new ApiService();
        private FormResizer formResizer;

        public UserCreate()
        {
            InitializeComponent();
            
            // Form boyutlandırıcıyı başlat
            formResizer = new FormResizer(this);
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            try
            {
                var UserName = textBox1.Text;
                var Password = textBox2.Text;
                if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
                {
                    MessageBox.Show("Username and Password cannot be empty.");
                    return;
                }

                var createUser = await _apiService.CreateUserAsync(new User { UserName = UserName, Password = Password });
                
                if (createUser != null)
                {
                    MessageBox.Show("User created successfully!");
                    Form1 loginForm = new Form1();
                    loginForm.Show();
                    this.Hide(); // Hide the UserCreate form after creating the user
                }
                else
                {
                    MessageBox.Show("User creation failed! Please try again.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Form1 loginForm = new Form1();
            loginForm.Show();
            this.Hide();
        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                textBox1.PlaceholderText = "Username";
                textBox1.ForeColor = Color.Gray;
            }
        }

        private void textBox2_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox2.Text))
            {
                textBox2.PlaceholderText = "Password";
                textBox2.ForeColor = Color.Gray;
            }
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
