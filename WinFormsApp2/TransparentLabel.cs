using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class TransparentLabel : Label
{
    public TransparentLabel()
    {
        SetStyle(ControlStyles.Opaque, true);
        BackColor = Color.Transparent;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= 0x20; // WS_EX_TRANSPARENT
            return cp;
        }
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        // Boş geç, böylece arka planı çizmez
    }
}

