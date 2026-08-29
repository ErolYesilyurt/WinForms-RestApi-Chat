using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp2
{
    public class ResponsiveLayout
    {
        private Form form;
        private Dictionary<Control, ControlInfo> controlInfos;
        private Size originalFormSize;

        public ResponsiveLayout(Form form)
        {
            this.form = form;
            this.controlInfos = new Dictionary<Control, ControlInfo>();
            this.originalFormSize = form.Size;
            
            // Tüm kontrollerin bilgilerini kaydet
            SaveControlInfo(form);
            
            // Form boyut değişikliği event'ini dinle
            form.Resize += Form_Resize;
        }

        private void SaveControlInfo(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                controlInfos[control] = new ControlInfo
                {
                    OriginalLocation = control.Location,
                    OriginalSize = control.Size,
                    OriginalFont = control.Font,
                    Anchor = control.Anchor,
                    Dock = control.Dock
                };
                SaveControlInfo(control); // Alt kontroller için recursive çağrı
            }
        }

        private void Form_Resize(object sender, EventArgs e)
        {
            ApplyResponsiveLayout();
        }

        public void ApplyResponsiveLayout()
        {
            if (originalFormSize.Width == 0 || originalFormSize.Height == 0)
                return;

            float scaleX = (float)form.Width / originalFormSize.Width;
            float scaleY = (float)form.Height / originalFormSize.Height;

            foreach (var kvp in controlInfos)
            {
                Control control = kvp.Key;
                ControlInfo info = kvp.Value;

                // Dock olan kontrolleri atla
                if (control.Dock != DockStyle.None)
                    continue;

                // Yeni boyut ve konum hesapla
                int newX = (int)(info.OriginalLocation.X * scaleX);
                int newY = (int)(info.OriginalLocation.Y * scaleY);
                int newWidth = (int)(info.OriginalSize.Width * scaleX);
                int newHeight = (int)(info.OriginalSize.Height * scaleY);

                // Özel kural varsa uygula
                if (info.CustomRule != null)
                {
                    Size customSize = info.CustomRule(form.Size, new Point(newX, newY), new Size(newWidth, newHeight));
                    newWidth = customSize.Width;
                    newHeight = customSize.Height;
                }

                // Kontrolün yeni boyut ve konumunu ayarla
                control.Location = new Point(newX, newY);
                control.Size = new Size(newWidth, newHeight);

                // Font boyutunu da ölçekle
                if (info.OriginalFont != null)
                {
                    float newFontSize = Math.Max(8, info.OriginalFont.Size * Math.Min(scaleX, scaleY));
                    control.Font = new Font(info.OriginalFont.FontFamily, newFontSize, info.OriginalFont.Style);
                }
            }
        }

        public void ResetToOriginalSize()
        {
            form.Size = originalFormSize;
            ApplyResponsiveLayout();
        }

        // Özel boyutlandırma kuralları eklemek için
        public void AddCustomRule(Control control, Func<Size, Point, Size, Size> customRule)
        {
            if (controlInfos.ContainsKey(control))
            {
                controlInfos[control].CustomRule = customRule;
            }
        }
    }

    public class ControlInfo
    {
        public Point OriginalLocation { get; set; }
        public Size OriginalSize { get; set; }
        public Font OriginalFont { get; set; }
        public AnchorStyles Anchor { get; set; }
        public DockStyle Dock { get; set; }
        public Func<Size, Point, Size, Size> CustomRule { get; set; }
    }
}
