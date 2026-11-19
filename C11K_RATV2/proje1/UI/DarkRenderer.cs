// Dosya: UI/DarkRenderer.cs
//using App.Service.UI;
using System.Drawing;
using System.Windows.Forms;

namespace App.Service
{
    public class DarkRenderer : ToolStripProfessionalRenderer
    {
        public DarkRenderer() : base(new DarkTable()) { }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            using (var b = new SolidBrush(e.Item.Selected ? Theme.Current.Accent : Theme.Current.Card))
                e.Graphics.FillRectangle(b, 0, 0, e.Item.Width, e.Item.Height);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Selected ? Color.White : Theme.Current.Text;
            base.OnRenderItemText(e);
        }
    }
}