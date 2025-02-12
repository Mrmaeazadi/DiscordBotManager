namespace Aeat;

public class MrContextMenuStrip : ContextMenuStrip
{
    public MrContextMenuStrip()
    {
        this.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());
        this.Font = new Font("Vazirmatn", 10, FontStyle.Regular);
        this.BackColor = Color.FromArgb(25, 25, 25);
        this.RightToLeft = RightToLeft.Yes;
        this.ForeColor = Color.White;
    }
}

public class DarkColorTable : ProfessionalColorTable
{
    public DarkColorTable()
    {

    }

    public override Color MenuBorder => Color.FromArgb(0, 85, 255); // ==> Menu Broder (Obviously...)

    public override Color ImageMarginGradientBegin => Color.FromArgb(0, 140, 255); // ==> Sidebar 1st Color Bar (IDK What you call it... XD)

    public override Color ImageMarginGradientMiddle => Color.FromArgb(0, 0, 200); // ==> Sidebar 2nd Color Bar (IDK What you call it... XD)

    public override Color ImageMarginGradientEnd => Color.Black; // ==> Sidebar 3rd Color Bar (IDK What you call it... XD)

    public override Color MenuItemSelectedGradientBegin => Color.DimGray; // ==> Hovered Item Top Color (IDK, XD)

    public override Color MenuItemSelectedGradientEnd => Color.Black; // ==> Hovered Item Bottom Color (IDK, XD)

    //public override Color MenuItemBorder => Color.FromArgb(0, 140, 255); // ==> Hovered Item Border

}
