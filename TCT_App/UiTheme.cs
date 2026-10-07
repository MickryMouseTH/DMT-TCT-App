using System;
using Gtk;
using Pango;

namespace TCT_App
{
    //--- v2.0.2: GUI theme (restyle only - layout and button positions are unchanged)
    //--- Button colours follow the physical Cherry toll keyboard, so the key the operator
    //--- presses and the button on screen have the same colour.
    public static class UiTheme
    {
        //--- Palette (Cherry Toll Keyboard key caps)
        public static readonly Gdk.Color Yellow     = new Gdk.Color(0xF5, 0xD4, 0x24);   // รถประเภท 1, ขายคูปอง, FORCE RESET
        public static readonly Gdk.Color Red        = new Gdk.Color(0xD6, 0x28, 0x3B);   // รถประเภท 2, แก้ไข
        public static readonly Gdk.Color Navy       = new Gdk.Color(0x2B, 0x46, 0x73);   // เงินสด
        public static readonly Gdk.Color Coupon     = new Gdk.Color(0x0F, 0x8F, 0x6F);   // คูปอง
        public static readonly Gdk.Color Teal       = new Gdk.Color(0x18, 0xA9, 0x9B);   // QR CODE, รถลากจูง
        public static readonly Gdk.Color LightTeal  = new Gdk.Color(0x73, 0xCF, 0xD2);   // รถยกเว้น
        public static readonly Gdk.Color Pink       = new Gdk.Color(0xF0, 0x8F, 0xB5);   // EMV, ยืนยัน, รถออกซ้าย
        public static readonly Gdk.Color Purple     = new Gdk.Color(0x8B, 0x5C, 0xC4);   // บัตร DMT, รถขบวน, พิมพ์ใบเสร็จ
        public static readonly Gdk.Color Green      = new Gdk.Color(0x1D, 0x9A, 0x4D);   // รถขบวนจ่ายเงิน
        public static readonly Gdk.Color Orange     = new Gdk.Color(0xF2, 0x87, 0x2E);   // รถฝ่าด่าน
        public static readonly Gdk.Color Blue       = new Gdk.Color(0x1E, 0x63, 0xB8);   // EOJ
        public static readonly Gdk.Color White      = new Gdk.Color(0xEC, 0xEC, 0xEC);   // Payment 6-9, OTL
        public static readonly Gdk.Color Disabled   = new Gdk.Color(0x4A, 0x4D, 0x52);
        public static readonly Gdk.Color DisabledFg = new Gdk.Color(0x8A, 0x8F, 0x96);
        public static readonly Gdk.Color TextDark   = new Gdk.Color(0x1E, 0x1E, 0x1E);
        public static readonly Gdk.Color TextLight  = new Gdk.Color(0xFF, 0xFF, 0xFF);

        public const string ButtonFont = "Sans Bold 15";

        static bool bRcApplied = false;

        //--- Global look: flat-ish buttons, no dotted focus rectangle, a bit more padding.
        //--- Call once before the first window is built.
        public static void ApplyGlobalStyle()
        {
            if (bRcApplied)
                return;
            bRcApplied = true;
            try
            {
                Rc.ParseString(@"
                    style ""tct-button""
                    {
                        xthickness = 2
                        ythickness = 2
                        GtkWidget::focus-line-width = 0
                        GtkWidget::focus-padding    = 0
                        GtkButton::inner-border     = { 4, 4, 4, 4 }
                        GtkButton::default-border   = { 0, 0, 0, 0 }
                        GtkButton::child-displacement-x = 0
                        GtkButton::child-displacement-y = 2
                    }
                    class ""GtkButton"" style ""tct-button""
                ");
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Warning UiTheme.ApplyGlobalStyle : " + err.Message);
            }
        }

        //--- Colour one button like its key cap. Text colour is chosen for contrast.
        public static void StyleKey(Button btn, Gdk.Color bg, string font = ButtonFont)
        {
            if (btn == null)
                return;
            try
            {
                Gdk.Color fg = IsLight(bg) ? TextDark : TextLight;

                btn.FocusOnClick = false;       // touch does not leave keyboard focus on the button
                btn.CanFocus     = false;
                btn.Relief       = ReliefStyle.Normal;

                btn.ModifyBg(StateType.Normal,      bg);
                btn.ModifyBg(StateType.Prelight,    Shade(bg, 1.12));
                btn.ModifyBg(StateType.Active,      Shade(bg, 0.80));
                btn.ModifyBg(StateType.Selected,    Shade(bg, 0.80));
                btn.ModifyBg(StateType.Insensitive, Disabled);

                foreach (Label lbl in FindLabels(btn))
                {
                    lbl.ModifyFont(FontDescription.FromString(font));
                    lbl.Justify = Justification.Center;
                    lbl.ModifyFg(StateType.Normal,      fg);
                    lbl.ModifyFg(StateType.Prelight,    fg);
                    lbl.ModifyFg(StateType.Active,      fg);
                    lbl.ModifyFg(StateType.Insensitive, DisabledFg);
                }
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Warning UiTheme.StyleKey : " + err.Message);
            }
        }

        static System.Collections.Generic.IEnumerable<Label> FindLabels(Widget w)
        {
            if (w is Label)
            {
                yield return (Label)w;
                yield break;
            }
            if (w is Container)
            {
                foreach (Widget child in ((Container)w).Children)
                    foreach (Label l in FindLabels(child))
                        yield return l;
            }
        }

        public static bool IsLight(Gdk.Color c)
        {
            // relative luminance on 16-bit channels
            double r = c.Red / 65535.0, g = c.Green / 65535.0, b = c.Blue / 65535.0;
            return (0.299 * r + 0.587 * g + 0.114 * b) > 0.6;
        }

        public static Gdk.Color Shade(Gdk.Color c, double k)
        {
            Func<ushort, byte> f = v => (byte)Math.Max(0, Math.Min(255, (v >> 8) * k));
            return new Gdk.Color(f(c.Red), f(c.Green), f(c.Blue));
        }
    }
}
