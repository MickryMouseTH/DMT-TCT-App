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
        public static readonly Gdk.Color KeyDark    = new Gdk.Color(0x4A, 0x52, 0x5E);   // number keys on screen keypads
        public static readonly Gdk.Color Disabled   = new Gdk.Color(0x4A, 0x4D, 0x52);
        public static readonly Gdk.Color DisabledFg = new Gdk.Color(0x8A, 0x8F, 0x96);
        public static readonly Gdk.Color TextDark   = new Gdk.Color(0x1E, 0x1E, 0x1E);
        public static readonly Gdk.Color TextLight  = new Gdk.Color(0xFF, 0xFF, 0xFF);

        public const string ButtonFont = "Sans Bold 15";
        public const string PopupBtnFont = "Sans Bold 14";      // v2.0.2: action buttons in popups

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
                LogInfo.WriteLog(LogLevel.DEBUG, "+ UiTheme.ApplyGlobalStyle : rc style applied");
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
                RoundKey.Attach(btn, bg);       // v2.0.3: rounded corners

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

        //--- v2.0.2: one header standard for LoginWnd / MainWindow / MainMaintenance
        //--- (same label text, font, colour, column widths -> value columns start at the same x)
        public const int HdrTable1 = 138, HdrTable2 = 276, HdrTable3 = 280;      // group widths
        public const int HdrInfo = 708, HdrClock = 205;                          // info block / clock box
        static readonly int[]    HdrLabelW  = { 70, 104, 118 };
        static readonly string[][] HdrText  =
        {
            new[] { "ชื่อด่าน :", "ช่องทาง :", "ประเภท :" },
            new[] { "รหัสพนักงาน :", "ชื่อพนักงาน :", "ตำแหน่งงาน :" },
            new[] { "พนักงานควบคุม :", "เวลาเริ่มงาน :", "หมายเลขงาน :" },
        };

        // groups: [0] plaza/lane/type, [1] staff (null on login), [2] supervisor/start/job
        public static void StyleHeader(Label[] g1, Label[] g2, Label[] g3, params Table[] tables)
        {
            try
            {
                var groups = new[] { g1, g2, g3 };
                for (int g = 0; g < 3; g++)
                {
                    if (groups[g] == null)
                        continue;
                    for (int i = 0; i < groups[g].Length && i < 3; i++)
                    {
                        Label lbl       = groups[g][i];
                        lbl.UseMarkup   = true;
                        lbl.Markup      = "<span fgcolor='#ffffff'>" + GLib.Markup.EscapeText(HdrText[g][i]) + "</span>";
                        lbl.Xalign      = 0f;
                        lbl.WidthRequest = HdrLabelW[g];
                        lbl.ModifyFont(FontDescription.FromString("Sans Bold 10"));
                    }
                }
                foreach (Table t in tables)
                {
                    t.ColumnSpacing = 6;
                    t.RowSpacing    = 4;
                    t.BorderWidth   = 4;
                }
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Warning UiTheme.StyleHeader : " + err.Message);
            }
        }

        //--- v2.0.2: popups have no title bar; draw a light 3 px frame so the popup edge is visible
        //--- on top of the dark main screen.
        public static readonly Gdk.Color PopupBorder = new Gdk.Color(0xB8, 0xC0, 0xCC);

        public static void StylePopup(Window w)
        {
            if (w == null)
                return;
            try
            {
                //--- v2.0.4: Stetic Build() already called Show() on most popups. Changing decoration / border on a
                //--- mapped window makes GTK ask the WM for a new size and freeze all repaints until the WM answers;
                //--- the answer never came -> popup painted once then froze (keys/touch worked, screen did not change).
                //--- Restyle while unmapped, then map again.
                bool bWasMapped  = w.IsMapped;
                if (bWasMapped)
                    w.Hide();
                w.Decorated = false;
                if (w.BorderWidth < 6)
                    w.BorderWidth = 6;           // keep content off the frame
                w.ReallocateRedraws = true;      // v2.0.3: popup grows when its message changes -> repaint all,
                                                 // else the old frame line stays and the new edge is broken
                w.ExposeEvent    += OnPopupExpose;
                w.SizeAllocated  += OnPopupSize;    // v2.0.3: rounded window corners
                StyleActionButtons(w);
                FlattenFrames(w);
                if (bWasMapped)
                    w.Show();
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ UiTheme.StylePopup : {w.GetType().Name} '{w.Title}' styled (was mapped {bWasMapped})");
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Warning UiTheme.StylePopup : " + err.Message);
            }
        }

        //--- v2.0.2: one rule for every popup -> OK / confirm = green, cancel / close = red
        //--- (the colour is picked from the button text, other buttons are left as they are)
        static readonly string[] OkWords     = { "ตกลง", "ยืนยัน", "OK", "Yes", "ใช่", "Apply" };
        static readonly string[] CancelWords = { "ยกเลิก", "Cancel", "No", "ไม่", "ปิดหน้าต่าง" };

        public static void StyleActionButtons(Container root)
        {
            if (root == null)
                return;
            foreach (Widget child in root.Children)
            {
                var btn = child as Button;
                if (btn != null)
                {
                    string szText = "";
                    foreach (Label l in FindLabels(btn))
                        szText += l.Text;
                    szText = szText.Replace("_", "").Trim();
                    if (StartsWithAny(szText, OkWords))
                    {
                        StyleKey(btn, Green, PopupBtnFont);
                        if (szText == "OK")
                            SetButtonText(btn, "ตกลง");          // same word on every popup
                    }
                    else if (StartsWithAny(szText, CancelWords))
                    {
                        StyleKey(btn, Red, PopupBtnFont);
                        if (szText == "Cancel")
                            SetButtonText(btn, "ยกเลิก");
                    }
                    else if (szText.StartsWith("พิมพ์", StringComparison.Ordinal))
                        StyleKey(btn, Purple, PopupBtnFont);
                    continue;
                }
                if (child is Container)
                    StyleActionButtons((Container)child);
            }
        }

        static bool StartsWithAny(string s, string[] words)
        {
            foreach (string w in words)
            {
                bool bEng = w[0] < 128;     // "OK" / "No" / "Cancel" -> whole text only ("Close ALB" is not a close button)
                if (bEng ? s.Equals(w, StringComparison.OrdinalIgnoreCase) : s.StartsWith(w, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        //--- runs after GTK painted the popup and its children (no ConnectBefore) -> frame stays on top
        static void OnPopupExpose(object o, ExposeEventArgs args)
        {
            var w = o as Window;
            if (w == null || w.GdkWindow == null || args.Event.Window != w.GdkWindow)
                return;
            try
            {
                int iW = w.Allocation.Width, iH = w.Allocation.Height;
                using (var gc = new Gdk.GC(w.GdkWindow))
                {
                    gc.RgbFgColor = PopupBorder;
                        gc.SetLineAttributes(3, Gdk.LineStyle.Solid, Gdk.CapStyle.Round, Gdk.JoinStyle.Round);
                    DrawRound(w.GdkWindow, gc, 1, 1, iW - 3, iH - 3, PopupRadius);
                }
            }
            catch (Exception err) { LogInfo.WriteLog(LogLevel.TRACE, "- Error UiTheme.OnPopupExpose : " + err.Message); }
        }

        //=== v2.0.3: rounded corners (plain GDK arcs - no Cairo / theme engine needed on the lane PC) ===
        public const int Radius = 8, PopupRadius = 12;

        public static void FillRound(Gdk.Drawable d, Gdk.GC gc, int x, int y, int w, int h, int r)
        {
            r = Math.Max(0, Math.Min(r, Math.Min(w, h) / 2));
            if (r == 0)
            {
                d.DrawRectangle(gc, true, x, y, w, h);
                return;
            }
            d.DrawRectangle(gc, true, x + r, y, w - 2 * r, h);
            d.DrawRectangle(gc, true, x, y + r, w, h - 2 * r);
            d.DrawArc(gc, true, x,             y,             2 * r, 2 * r,  90 * 64, 90 * 64);
            d.DrawArc(gc, true, x + w - 2 * r, y,             2 * r, 2 * r,   0 * 64, 90 * 64);
            d.DrawArc(gc, true, x,             y + h - 2 * r, 2 * r, 2 * r, 180 * 64, 90 * 64);
            d.DrawArc(gc, true, x + w - 2 * r, y + h - 2 * r, 2 * r, 2 * r, 270 * 64, 90 * 64);
        }

        public static void DrawRound(Gdk.Drawable d, Gdk.GC gc, int x, int y, int w, int h, int r)
        {
            r = Math.Max(1, Math.Min(r, Math.Min(w, h) / 2));
            d.DrawLine(gc, x + r, y,     x + w - r, y);
            d.DrawLine(gc, x + r, y + h, x + w - r, y + h);
            d.DrawLine(gc, x,     y + r, x,         y + h - r);
            d.DrawLine(gc, x + w, y + r, x + w,     y + h - r);
            d.DrawArc(gc, false, x,             y,             2 * r, 2 * r,  90 * 64, 90 * 64);
            d.DrawArc(gc, false, x + w - 2 * r, y,             2 * r, 2 * r,   0 * 64, 90 * 64);
            d.DrawArc(gc, false, x,             y + h - 2 * r, 2 * r, 2 * r, 180 * 64, 90 * 64);
            d.DrawArc(gc, false, x + w - 2 * r, y + h - 2 * r, 2 * r, 2 * r, 270 * 64, 90 * 64);
        }

        //--- popup window shape = rounded rectangle (corners outside are cut away, not painted)
        static void OnPopupSize(object o, SizeAllocatedArgs args)
        {
            var w = o as Window;
            if (w == null)
                return;
            try
            {
                int iW = args.Allocation.Width, iH = args.Allocation.Height;
                if (iW < 4 || iH < 4)
                    return;
                var mask = new Gdk.Pixmap(null, iW, iH, 1);
                using (var gc = new Gdk.GC(mask))
                {
                    gc.Foreground = new Gdk.Color { Pixel = 0 };
                    mask.DrawRectangle(gc, true, 0, 0, iW, iH);
                    gc.Foreground = new Gdk.Color { Pixel = 1 };
                    FillRound(mask, gc, 0, 0, iW, iH, PopupRadius + 2);
                }
                w.ShapeCombineMask(mask, 0, 0);
            }
            catch (Exception err) { LogInfo.WriteLog(LogLevel.ERROR, "- Error UiTheme.OnPopupSize : " + err.Message); }
        }

        //--- paints a styled button as a rounded key, then its label; the default square bevel is skipped
        class RoundKey
        {
            static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Button, RoundKey> keys =
                new System.Runtime.CompilerServices.ConditionalWeakTable<Button, RoundKey>();
            Gdk.Color bg;

            public static void Attach(Button btn, Gdk.Color bg)
            {
                RoundKey k;
                if (keys.TryGetValue(btn, out k))
                {
                    k.bg = bg;                  // styled again (e.g. popup rule then a specific colour)
                    return;
                }
                k = new RoundKey { bg = bg };
                keys.Add(btn, k);
                btn.ExposeEvent += k.OnExpose;
            }

            [GLib.ConnectBefore]
            void OnExpose(object o, ExposeEventArgs args)
            {
                var btn = o as Button;
                if (btn == null || btn.GdkWindow == null)
                    return;
                try
                {
                    Gdk.Color c = !btn.Sensitive                 ? Disabled
                                : btn.State == StateType.Active   ? Shade(bg, 0.80)
                                : btn.State == StateType.Prelight ? Shade(bg, 1.12)
                                : bg;
                    Gdk.Rectangle a = btn.Allocation;
                    using (var gc = new Gdk.GC(btn.GdkWindow))
                    {
                        gc.ClipRectangle = args.Event.Area;
                        gc.RgbFgColor    = c;
                        FillRound(btn.GdkWindow, gc, a.X + 1, a.Y + 1, a.Width - 2, a.Height - 2, Radius);
                    }
                    if (btn.Child != null)
                        btn.PropagateExpose(btn.Child, args.Event);
                    args.RetVal = true;
                }
                catch (Exception err) { LogInfo.WriteLog(LogLevel.TRACE, "- Error UiTheme.RoundKey.OnExpose : " + err.Message); }
            }
        }

        //--- v2.0.3: flat "card" look instead of GTK etched frames.
        //--- An etched frame is 2 lines (light + dark); two frames side by side gave 4 lines with a gap
        //--- (e.g. header info | clock). Now: no frame lines at all, every inner frame is filled with a
        //--- slightly lighter card colour, frames that hold other frames stay transparent.
        public static readonly Gdk.Color Card = new Gdk.Color(0x3A, 0x40, 0x4A);

        public static void FlattenFrames(Container root)
        {
            if (root == null)
                return;
            try
            {
                foreach (Widget child in root.Children)
                {
                    var f = child as Frame;
                    if (f != null && f.ShadowType != ShadowType.None)
                    {
                        f.ShadowType = ShadowType.None;
                        if (!HasFrameInside(f))
                            f.ExposeEvent += OnCardExpose;
                    }
                    if (child is Container)
                        FlattenFrames((Container)child);
                }
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Warning UiTheme.FlattenFrames : " + err.Message);
            }
        }

        static bool HasFrameInside(Container c)
        {
            foreach (Widget w in c.Children)
                if (w is Frame || (w is Container && HasFrameInside((Container)w)))
                    return true;
            return false;
        }

        //--- before the children paint (a Frame has no own window -> fill on the parent window)
        [GLib.ConnectBefore]
        static void OnCardExpose(object o, ExposeEventArgs args)
        {
            var f = o as Frame;
            if (f == null || f.GdkWindow == null)
                return;
            try
            {
                Gdk.Rectangle a = f.Allocation;
                using (var gc = new Gdk.GC(f.GdkWindow))
                {
                    gc.RgbFgColor = Card;
                    FillRound(f.GdkWindow, gc, a.X, a.Y, a.Width, a.Height, Radius);
                }
            }
            catch (Exception err) { LogInfo.WriteLog(LogLevel.TRACE, "- Error UiTheme.OnCardExpose : " + err.Message); }
        }

        //--- v2.0.3: bottom status bar -> exactly one separator between every two items.
        //--- GtkStatusbar's own empty message frame drew "[ ]", some places had "||" and some none,
        //--- so drop all old separators and the empty frame, then put one new separator before each item.
        public static void StyleStatusBar(Statusbar sb)
        {
            if (sb == null)
                return;
            try
            {
                var items = new System.Collections.Generic.List<Widget>();
                foreach (Widget child in sb.Children)
                {
                    bool bDrop = child is VSeparator;
                    if (child is Frame)
                    {
                        bDrop = true;
                        foreach (Label l in FindLabels(child))
                            if (!string.IsNullOrEmpty(l.Text))
                                bDrop = false;
                    }
                    if (bDrop)
                    {
                        child.NoShowAll = true;
                        child.Hide();
                    }
                    else
                        items.Add(child);
                }
                for (int i = 1; i < items.Count; i++)
                {
                    var sep = new VSeparator();
                    sb.PackStart(sep, false, false, 0);     // no padding: the bar must stay within 1024 px
                    int idx = Array.IndexOf(sb.Children, items[i]);
                    sb.ReorderChild(sep, idx);
                    sep.Show();
                }
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ UiTheme.StyleStatusBar : {items.Count} items, {Math.Max(0, items.Count - 1)} separators");
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Warning UiTheme.StyleStatusBar : " + err.Message);
            }
        }

        //--- v2.0.3: change the text of a button that has an icon + label (Button.Label would drop the icon)
        public static void SetButtonText(Button btn, string szText)
        {
            if (btn == null)
                return;
            foreach (Label l in FindLabels(btn))
            {
                l.Text    = szText;
                l.Justify = Justification.Center;
                break;
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
