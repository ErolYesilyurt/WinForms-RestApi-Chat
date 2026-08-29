using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp2
{
    public partial class TestResponsiveForm : Form
    {
        private FormResizer formResizer;
        private ResponsiveLayout responsiveLayout;

        public TestResponsiveForm()
        {
            InitializeComponent();
            
            // Form boyutlandırıcıyı başlat
            formResizer = new FormResizer(this);
            
            // Responsive layout'u başlat
            responsiveLayout = new ResponsiveLayout(this);
            
            // Test kontrolleri ekle
            AddTestControls();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Form ayarları
            this.AutoScaleDimensions = new SizeF(8F, 20F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(800, 600);
            this.Name = "TestResponsiveForm";
            this.Text = "Responsive Layout Test";
            this.StartPosition = FormStartPosition.CenterScreen;
            
            this.ResumeLayout(false);
        }

        private void AddTestControls()
        {
            // Test butonları
            Button btn1 = new Button();
            btn1.Text = "Test Button 1";
            btn1.Location = new Point(50, 50);
            btn1.Size = new Size(120, 40);
            btn1.Click += (s, e) => MessageBox.Show("Button 1 clicked!");
            this.Controls.Add(btn1);

            Button btn2 = new Button();
            btn2.Text = "Test Button 2";
            btn2.Location = new Point(200, 50);
            btn2.Size = new Size(120, 40);
            btn2.Click += (s, e) => MessageBox.Show("Button 2 clicked!");
            this.Controls.Add(btn2);

            // Test TextBox
            TextBox txt1 = new TextBox();
            txt1.Text = "Test TextBox";
            txt1.Location = new Point(50, 120);
            txt1.Size = new Size(200, 30);
            this.Controls.Add(txt1);

            // Test Label
            Label lbl1 = new Label();
            lbl1.Text = "Test Label - Bu yazı boyutla birlikte ölçeklenecek";
            lbl1.Location = new Point(50, 170);
            lbl1.Size = new Size(300, 30);
            lbl1.Font = new Font("Arial", 12, FontStyle.Bold);
            this.Controls.Add(lbl1);

            // Test Panel
            Panel panel1 = new Panel();
            panel1.BackColor = Color.LightBlue;
            panel1.Location = new Point(50, 220);
            panel1.Size = new Size(250, 100);
            panel1.BorderStyle = BorderStyle.FixedSingle;
            
            Label panelLabel = new Label();
            panelLabel.Text = "Panel İçi";
            panelLabel.Location = new Point(10, 10);
            panelLabel.AutoSize = true;
            panel1.Controls.Add(panelLabel);
            
            this.Controls.Add(panel1);

            // Reset butonu
            Button resetBtn = new Button();
            resetBtn.Text = "Reset Size";
            resetBtn.Location = new Point(50, 350);
            resetBtn.Size = new Size(100, 30);
            resetBtn.Click += (s, e) => formResizer.ResetToOriginalSize();
            this.Controls.Add(resetBtn);
        }
    }
}
