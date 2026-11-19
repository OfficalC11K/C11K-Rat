// Dosya: UI/DarkTable.cs
using System.Drawing;
using System.Windows.Forms;

namespace App.Service
{
    public class DarkTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Theme.Current.Card; } }
        public override Color ImageMarginGradientBegin { get { return Theme.Current.Card; } }
        public override Color ImageMarginGradientMiddle { get { return Theme.Current.Card; } }
        public override Color ImageMarginGradientEnd { get { return Theme.Current.Card; } }
        public override Color MenuBorder { get { return Theme.Current.Border; } }
        public override Color MenuItemBorder { get { return Theme.Current.Border; } }
        public override Color SeparatorDark { get { return Theme.Current.Border; } }
        public override Color SeparatorLight { get { return Theme.Current.Border; } }
    }
}