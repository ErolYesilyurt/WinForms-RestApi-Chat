using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp2
{
    public class FormResizer
    {
        private Form form;
        private Dictionary<Control, Rectangle> originalControlBounds;
        private Size originalFormSize;

        public FormResizer(Form form)
        {
            this.form = form;
            this.originalControlBounds = new Dictionary<Control, Rectangle>();
            this.originalFormSize = form.Size;
            
            // Tüm kontrollerin orijinal boyutlarını kaydet
            SaveControlBounds(form);
            
            // Form boyut değişikliği event'ini dinle
            form.Resize += Form_Resize;
        }

        private void SaveControlBounds(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                originalControlBounds[control] = new Rectangle(control.Location, control.Size);
                SaveControlBounds(control); // Alt kontroller için recursive çağrı
            }
        }

        private void Form_Resize(object sender, EventArgs e)
        {
            ResizeControls();
        }

        public void ResizeControls()
        {
            if (originalFormSize.Width == 0 || originalFormSize.Height == 0)
                return;

            float scaleX = (float)form.Width / originalFormSize.Width;
            float scaleY = (float)form.Height / originalFormSize.Height;

            foreach (var kvp in originalControlBounds)
            {
                Control control = kvp.Key;
                Rectangle originalBounds = kvp.Value;

                // Yeni boyut ve konum hesapla
                int newX = (int)(originalBounds.X * scaleX);
                int newY = (int)(originalBounds.Y * scaleY);
                int newWidth = (int)(originalBounds.Width * scaleX);
                int newHeight = (int)(originalBounds.Height * scaleY);

                // Kontrolün yeni boyut ve konumunu ayarla
                control.Location = new Point(newX, newY);
                control.Size = new Size(newWidth, newHeight);

                // Font boyutunu da ölçekle
                if (control.Font != null)
                {
                    float newFontSize = Math.Max(8, control.Font.Size * Math.Min(scaleX, scaleY));
                    control.Font = new Font(control.Font.FontFamily, newFontSize, control.Font.Style);
                }
            }
        }

        public void ResetToOriginalSize()
        {
            form.Size = originalFormSize;
            ResizeControls();
        }

        public Size GetOriginalFormSize()
        {
            return originalFormSize;
        }
    }
}
