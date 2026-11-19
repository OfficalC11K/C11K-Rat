// Dosya: UI/BufferedListView.cs
using System;
using System.Windows.Forms;

namespace App.Service
{
    public class BufferedListView : ListView
    {
        public BufferedListView()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Native.ListViewDoubleBuffer(Handle);
        }
    }
}