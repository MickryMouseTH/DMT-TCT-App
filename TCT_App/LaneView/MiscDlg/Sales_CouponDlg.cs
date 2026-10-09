using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using Gtk;
using Pango;
using TCT_App.EMV.EMV_V1;
using TCT_App.EMV.V0;
using TCT_App.PrintUtils;

namespace TCT_App.LaneView.MiscDlg
{
    public partial class Sales_CouponDlg : Gtk.Dialog
    {
        Gdk.Color clBlue                    = new Gdk.Color(10, 110, 170);
        Gdk.Color clBlack                   = new Gdk.Color(0, 0, 0);
        Gdk.Color clWhite                   = new Gdk.Color(255, 255, 255);
        Gdk.Color clGray                    = new Gdk.Color(150, 150, 150);

        Gdk.Color clHeaderCoupon1_UnSelect  = new Gdk.Color(10, 110, 17);
        Gdk.Color clHeaderCoupon1_Selected  = new Gdk.Color(0x00, 0xb3, 0x3c);
        Gdk.Color clHeaderCoupon2_UnSelect  = new Gdk.Color(32, 74, 135);
        Gdk.Color clHeaderCoupon2_Selected  = new Gdk.Color(10, 110, 170);

        Gdk.Color clBkCoupon1_UnSelect      = new Gdk.Color(0x00, 0xb3, 0x3c);
        Gdk.Color clBkCoupon2_UnSelect      = new Gdk.Color(10, 110, 170);
        //--- v2.0.4: selected coupon = faded (light) colour of its group + dark text + check mark
        Gdk.Color clBkCoupon1_Selected      = new Gdk.Color(0xB8, 0xE6, 0xC6);
        Gdk.Color clFgCoupon1_Selected      = new Gdk.Color(0x0A, 0x5A, 0x1E);
        Gdk.Color clBkCoupon2_Selected      = new Gdk.Color(0xB8, 0xD4, 0xEA);
        Gdk.Color clFgCoupon2_Selected      = new Gdk.Color(0x14, 0x3C, 0x6E);

        Gdk.Color clBkPayUnSel              = new Gdk.Color(50, 50, 50);
        Gdk.Color clBkPaySelect             = new Gdk.Color(30, 130, 30);
        Gdk.Color clFgPayUnSel              = new Gdk.Color(150, 150, 150);
        Gdk.Color clFgPaySelect             = new Gdk.Color(255, 255, 255);


        int iSelHeaderCouponType            = 1;    // 1=Coupon 35, 2=Coupon 80
        int iSelPayType                     = 0;    // 0=Cash, 1=EMV, 2=QR Code
        bool bPayCompleted                  = false;

        int[] iSelCouponList_Type1          = new int[10];
        int[] iSelCouponList_Type2          = new int[10];
        string[] szCouponNoList_Type1       = new string[10];
        string[] szCouponNoList_Type2       = new string[10];
        decimal fCouponPrice_Type1 = 0, fCouponPrice_Type2 = 0;
        int iTotalCoupon_Type1 = 0, iTotalCoupon_Type2 = 0;

        objCreditCardInfo xCreditCardInfo = null;
        //--- v2.0.4
        bool bClosed    = false;        // Respond() already sent -> ignore every later key / touch
        bool bMsgOpen   = false;        // a MessageDialog is running on top of this popup
        readonly Dictionary<Widget, Gdk.Color> dicBg = new Dictionary<Widget, Gdk.Color>();
        //string szCouponType1 = "35", szCouponType2 = "80";
        string szCouponType1 = "", szCouponType2 = "";
        //------------------------------------------------------------------------
        public Sales_CouponDlg()
        {
            TCT_Lib.bActiveSalesCouponDlg = true;

            this.Build();
            UiTheme.StylePopup(this);     // v2.0.2: no title bar + visible frame
            //--- v2.0.2: popup buttons = same colour as the Cherry key, readable size
            UiTheme.StyleKey(btnConfirmPayment, UiTheme.Green,  UiTheme.PopupBtnFont);
            UiTheme.StyleKey(btnPrintReceipt,   UiTheme.Purple, UiTheme.PopupBtnFont);
            UiTheme.StyleKey(btnCancel,         UiTheme.Red,    UiTheme.PopupBtnFont);

            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ SalesCoupon : popup opened, page {TCT_Lib.eCurAppPage} -> SalesCoupon");
            TCT_Lib.eCurAppPage     = AppPage.SalesCoupon;
            iSelHeaderCouponType    = 1; 
            iSelPayType             = 0;
            InitUI();

            txtCreditCardNo.Text = txtReference.Text = txtMsg.Text = "";
            for (int i=0; i<10; i++)
            {
                iSelCouponList_Type1[i] = 0;
                iSelCouponList_Type2[i] = 0;
                szCouponNoList_Type1[i] = "";
                szCouponNoList_Type2[i] = "";
            }

            //--- For Production
            LoadSalesCouponData();

            // For Test
            //LoadSalesCouponData_Test(); 

            //iSelCouponList_Type1[0] = iSelCouponList_Type2[0] = 1;
            ShowSumSaleCoupon();

            btnSelPayQrCode.Visible = TCT_Lib.iEdcVersionType == 0;
            //-----------------------------------------------------------------------------
            TCT_Lib.laneController.manageProcessSalesCoupon += LaneController_ManageProcessSalesCoupon;

            this.Activate();
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ SalesCoupon : ready, type1 '{szCouponType1}' x{iTotalCoupon_Type1} @{fCouponPrice_Type1}, type2 '{szCouponType2}' x{iTotalCoupon_Type2} @{fCouponPrice_Type2}, edcVer {TCT_Lib.iEdcVersionType}, maint {TCT_Lib.bMaintenanceMode}");
        }

        //--- v2.0.4: log helper (read-only) -> "header 1, pay 0, total 1 / 35 THB"
        string LogState()
        {
            int iTotalCoupon        = 0;
            decimal fTotalAmount    = 0;
            Calc_TotalSalesCoupon(ref iTotalCoupon, ref fTotalAmount);
            return $"header {iSelHeaderCouponType}, pay {iSelPayType}, total {iTotalCoupon} / {fTotalAmount:#,0} THB";
        }

        private void InitUI()
        {
            this.Title = "Sales Coupon Dialog";

            topTitle.ModifyBg(StateType.Normal, clBlue);
            topTitle.QueueDraw();

            btnConfirmPayment.Child.ModifyFont(FontDescription.FromString("Arial 16"));
            btnPrintReceipt.Child.ModifyFont(FontDescription.FromString("Arial 16"));
            btnConfirmPayment.Child.ModifyFont(FontDescription.FromString("Arial 16"));

            LogInfo.WriteLaneEvent("+ Init Sales Coupon |");

            //headerCoupon_Type1.QueueDraw();
            //headerCoupon_Type2.QueueDraw();
        }

        protected void LoadSalesCouponData_Test()
        {
            for (int i = 0; i < 1; i++)
            {
                szCouponNoList_Type1[i] = "ข100" + (i+111).ToString("000");
                szCouponNoList_Type2[i] = "C400" + (i+666).ToString("000");
            }
            fCouponPrice_Type1  = 665;
            fCouponPrice_Type2  = 1520;
        }

        protected void LoadSalesCouponData()
        {
            //--- Load CouponType and Coupon Price
            //string szQuery          = "SELECT distinct(CouponType) AS CouponSaleType, CouponPrice FROM SalesCouponList ORDER BY CouponPK";
            //string szQuery          = "SELECT CouponType AS CouponSaleType, CouponPrice FROM SalesCouponList GROUP BY CouponType ORDER BY CouponType";
            string szQuery          = "SELECT CouponType AS CouponSaleType FROM SalesCouponType ORDER BY CouponTypeID";
            DataTable dtCouponData  = TCT_Lib.LaneDB.List(szQuery);

            LogInfo.WriteLog("+ Found Total Row : " + dtCouponData.Rows.Count);
            for (int i = 0; i < dtCouponData.Rows.Count; i++)
                LogInfo.WriteLog(" CouponType = " + dtCouponData.Rows[i]["CouponSaleType"].ToString());

            if (dtCouponData.Rows.Count == 0)
            {
                LogInfo.WriteLaneEvent("- Not found Coupon List");
                return;
            }


            if (dtCouponData.Rows.Count > 0)
            {
                szCouponType1 = dtCouponData.Rows[0]["CouponSaleType"].ToString();
            }
            if (dtCouponData.Rows.Count > 1)
            {
                szCouponType2 = dtCouponData.Rows[1]["CouponSaleType"].ToString();
            }

            //----------------------------------------------------------
            szQuery = "SELECT * FROM SalesCouponList "+
                        $"WHERE CouponStatus=0 AND CouponType='{szCouponType1}' "+
                        "ORDER BY SerialNo";
                        //"ORDER BY CouponPK";
            DataTable dtCoupon1 = TCT_Lib.LaneDB.List(szQuery);
            iTotalCoupon_Type1  = Math.Min(10, dtCoupon1.Rows.Count);
            for (int i = 0; i < iTotalCoupon_Type1; i++)
            {
                szCouponNoList_Type1[i] = dtCoupon1.Rows[i]["SerialNo"].ToString();
                LogInfo.WriteLaneEvent("+ Load Coupon No = " + dtCoupon1.Rows[i]["SerialNo"].ToString());
            }
            if (iTotalCoupon_Type1 > 0)
            {
                fCouponPrice_Type1 = ParsePrice(dtCoupon1.Rows[0]["CouponPrice"]);
                LogInfo.WriteLaneEvent("+ Load Coupon Price = " + fCouponPrice_Type1);
            }

            szQuery = "SELECT * FROM SalesCouponList " +
                        $"WHERE CouponStatus=0 AND CouponType='{szCouponType2}' "+
                        "ORDER BY SerialNo";
                        //"ORDER BY CouponPK";
            DataTable dtCoupon2 = TCT_Lib.LaneDB.List(szQuery);
            iTotalCoupon_Type2  = Math.Min(10, dtCoupon2.Rows.Count);
            for (int i = 0; i < iTotalCoupon_Type2; i++)
            {
                szCouponNoList_Type2[i] = dtCoupon2.Rows[i]["SerialNo"].ToString();
                LogInfo.WriteLaneEvent("+ Load Coupon No = " + dtCoupon2.Rows[i]["SerialNo"].ToString());
            }
            if (iTotalCoupon_Type2 > 0)
            {
                fCouponPrice_Type2 = ParsePrice(dtCoupon2.Rows[0]["CouponPrice"]);
                LogInfo.WriteLaneEvent("+ Load Coupon Price = " + fCouponPrice_Type2);
            }
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ SalesCoupon : loaded type1 '{szCouponType1}' {iTotalCoupon_Type1} coupons, type2 '{szCouponType2}' {iTotalCoupon_Type2} coupons");
        }

        //--- v2.0.4: price column may be NULL / come back with the machine locale -> never throw
        static decimal ParsePrice(object objValue)
        {
            decimal fPrice;
            string szValue = (objValue == null || objValue == DBNull.Value) ? "" : objValue.ToString();
            if (decimal.TryParse(szValue, NumberStyles.Any, CultureInfo.InvariantCulture, out fPrice))
                return fPrice;
            LogInfo.WriteLaneEvent(LogLevel.WARN, $"- Warning SalesCoupon : coupon price '{szValue}' not numeric -> 0");
            return 0;
        }

        //--- v2.0.4: ModifyBg() inside an expose handler re-queues a redraw every paint (same bug as I8)
        void SetBgIfChanged(Widget w, Gdk.Color c)
        {
            Gdk.Color old;
            if (dicBg.TryGetValue(w, out old) && old.Equal(c))
                return;
            dicBg[w] = c;
            w.ModifyBg(StateType.Normal, c);
        }

        protected void ShowSumSaleCoupon()
        {
            int iTotalCoupon        = 0;
            decimal fTotalAmount    = 0;
            Calc_TotalSalesCoupon(ref iTotalCoupon, ref fTotalAmount);
            txtTotalCoupon.Text     = $"<span font_size='large' fgcolor='#22FF22'>{iTotalCoupon} เล่ม</span>";
            txtTotalAmount.Text     = $"<span font_size='large' fgcolor='#22FF22'>{fTotalAmount.ToString("#,0")} บาท</span>";
            txtTotalCoupon.UseMarkup = true;
            txtTotalAmount.UseMarkup = true;
        }

        protected void Calc_TotalSalesCoupon(ref int iTotalCoupon, ref decimal fTotalAmount)
        {
            iTotalCoupon = 0; 
            fTotalAmount = 0;
            for (int i=0; i<10; i++)
            {
                if (iSelCouponList_Type1[i] == 1)
                {
                    iTotalCoupon++;
                    fTotalAmount += fCouponPrice_Type1;
                }
                if (iSelCouponList_Type2[i] == 1)
                {
                    iTotalCoupon++;
                    fTotalAmount += fCouponPrice_Type2;
                }
            }
        }

        [GLib.ConnectBefore]
        protected void OnKeyPressEvent(object o, KeyPressEventArgs args)
        {
            int iKeyValue = (int)args.Event.KeyValue;
            if (TCT_Lib.keyMapping.ContainsKey(iKeyValue))
            {
                int iKey = TCT_Lib.keyMapping[iKeyValue];
                //--- v2.0.4: a mapped key is ours only. Without RetVal GTK also ran its own binding:
                //--- Escape closed the popup behind our back (delete-event) and the LaneController
                //--- handler stayed registered -> the next popup received keys for a disposed dialog.
                args.RetVal = true;
                LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ SalesCoupon : keyboard {args.Event.Key} -> virtual key {(KeyPressedType)iKey}");
                TCT_Lib.webSocketLane.SendVirtualKeyboard(iKey, 1);
            }
            else if (LogInfo.IsEnabled(LogLevel.TRACE))
                LogInfo.WriteLaneEvent(LogLevel.TRACE, $"+ SalesCoupon : keyboard {args.Event.Key} not mapped");
        }

        //--- v2.0.4: window close (Escape / window manager) goes through the same path as [แก้ไข]
        protected override bool OnDeleteEvent(Gdk.Event evnt)
        {
            LogInfo.WriteLaneEvent(LogLevel.WARN, $"+ SalesCoupon : delete-event -> {(bPayCompleted ? "ignored, already paid" : (bMsgOpen ? "ignored, message popup open" : "cancel"))}");
            if (!bPayCompleted && !bMsgOpen)
                Action_BtnCancel();
            return true;        // never let GTK close the popup by itself
        }

        private void LaneController_ManageProcessSalesCoupon(int iKey)
        {
            if (LogInfo.IsEnabled(LogLevel.TRACE))
                LogInfo.WriteLaneEvent(LogLevel.TRACE, $"+ SalesCoupon : key {(KeyPressedType)iKey} queued to UI thread");
            Application.Invoke(delegate
            {
                if (bClosed || bMsgOpen)
                {
                    LogInfo.WriteLaneEvent(LogLevel.WARN, $"+ SalesCoupon : key {(KeyPressedType)iKey} ignored ({(bClosed ? "popup already closed" : "message popup open")})");
                    return;
                }
                LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ SalesCoupon : key {(KeyPressedType)iKey} ({LogState()}, paid {bPayCompleted})");
                if (iKey == (int)KeyPressedType.LEFT)
                    Action_ClickHeaderCouponType(1);
                else if (iKey == (int)KeyPressedType.RIGHT)
                    Action_ClickHeaderCouponType(2);
                else if (iKey == (int)KeyPressedType.DOT)
                {
                    if (iSelHeaderCouponType == 1)
                        Action_ClickHeaderCouponType(2);
                    else
                        Action_ClickHeaderCouponType(1);
                }
                else if (iKey == (int)KeyPressedType.NUM_1)
                {
                    if (iSelHeaderCouponType == 1)
                        OnBtnType1_ButtonPressEvent(btnType1_Coupon1, 0);
                    else
                        OnBtnType2_ButtonPressEvent(btnType2_Coupon1, 0);
                }
                else if (iKey == (int)KeyPressedType.NUM_2)
                {
                    if (iSelHeaderCouponType == 1)
                        OnBtnType1_ButtonPressEvent(btnType1_Coupon2, 1);
                    else
                        OnBtnType2_ButtonPressEvent(btnType2_Coupon2, 1);
                }
                else if (iKey == (int)KeyPressedType.NUM_3)
                {
                    if (iSelHeaderCouponType == 1)
                        OnBtnType1_ButtonPressEvent(btnType1_Coupon3, 2);
                    else
                        OnBtnType2_ButtonPressEvent(btnType2_Coupon3, 2);
                }
                else if (iKey == (int)KeyPressedType.NUM_4)
                {
                    if (iSelHeaderCouponType == 1)
                        OnBtnType1_ButtonPressEvent(btnType1_Coupon4, 3);
                    else
                        OnBtnType2_ButtonPressEvent(btnType2_Coupon4, 3);
                }
                else if (iKey == (int)KeyPressedType.NUM_5)
                {
                    if (iSelHeaderCouponType == 1)
                        OnBtnType1_ButtonPressEvent(btnType1_Coupon5, 4);
                    else
                        OnBtnType2_ButtonPressEvent(btnType2_Coupon5, 4);
                }
                else if (iKey == (int)KeyPressedType.NUM_6)
                {
                    if (iSelHeaderCouponType == 1)
                        OnBtnType1_ButtonPressEvent(btnType1_Coupon6, 5);
                    else
                        OnBtnType2_ButtonPressEvent(btnType2_Coupon6, 5);
                }
                else if (iKey == (int)KeyPressedType.NUM_7)
                {
                    if (iSelHeaderCouponType == 1)
                        OnBtnType1_ButtonPressEvent(btnType1_Coupon7, 6);
                    else
                        OnBtnType2_ButtonPressEvent(btnType2_Coupon7, 6);
                }
                else if (iKey == (int)KeyPressedType.NUM_8)
                {
                    if (iSelHeaderCouponType == 1)
                        OnBtnType1_ButtonPressEvent(btnType1_Coupon8, 7);
                    else
                        OnBtnType2_ButtonPressEvent(btnType2_Coupon8, 7);
                }
                else if (iKey == (int)KeyPressedType.NUM_9)
                {
                    if (iSelHeaderCouponType == 1)
                        OnBtnType1_ButtonPressEvent(btnType1_Coupon9, 8);
                    else
                        OnBtnType2_ButtonPressEvent(btnType2_Coupon9, 8);
                }
                else if (iKey == (int)KeyPressedType.NUM_0)
                {
                    if (iSelHeaderCouponType == 1)
                        OnBtnType1_ButtonPressEvent(btnType1_CouponZ, 9);
                    else
                        OnBtnType2_ButtonPressEvent(btnType2_CouponZ, 9);
                }
                else if (iKey == (int)KeyPressedType.PAY_CASH)
                {
                    Action_OnBtnSelPayment(0);
                }
                else if (iKey == (int)KeyPressedType.PAY_EMV)
                {
                    Action_OnBtnSelPayment(1);
                    Perform_ProcessEMV();
                }
                else if (iKey == (int)KeyPressedType.PAY_QRCODE)
                {
                    //--- v2.0.4: the QR key was accepted by LaneController but never handled here
                    if (btnSelPayQrCode.Visible)
                    {
                        Action_OnBtnSelPayment(2);
                        Perform_ProcessQrCode();
                    }
                    else
                        LogInfo.WriteLaneEvent(LogLevel.WARN, $"+ SalesCoupon : QR ignored, not available with EDC version {TCT_Lib.iEdcVersionType}");
                }
                else if (iKey == (int)KeyPressedType.CONFIRM)
                {
                    Action_BtnConfirmPayment();
                }
                else if (iKey == (int)KeyPressedType.RECEIPT)
                {
                    Action_BtnPrintReceipt();
                }
                else if (iKey == (int)KeyPressedType.EDIT)
                {
                    Action_BtnCancel();
                }
            });
        }

        protected void OnTopTitleExposeEvent(object o, ExposeEventArgs args)
        {
            DrawingArea drawArea = o as DrawingArea;
            //---  DrawArea ClassType
            int iWidth                  = drawArea.Allocation.Width;
            int iHeight                 = drawArea.Allocation.Height;
            Context context             = drawArea.CreatePangoContext();
            Pango.Layout layout         = new Pango.Layout(context);
            layout.Width                = Units.FromPixels(iWidth);
            layout.Alignment            = Pango.Alignment.Center;
            layout.Justify              = true;

            FontDescription fontDesc    = FontDescription.FromString("Ubuntu 22");
            layout.FontDescription      = fontDesc;

            layout.SetText("เลือกคูปองที่ต้องการขาย");

            Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
            renderer.Drawable           = drawArea.GdkWindow;
            renderer.Gc = drawArea.Style.BlackGC;

            drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 6, layout, new Gdk.Color(255, 255, 255), clBlue);

            renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
            renderer.Drawable   = null;
            renderer.Gc         = null;
            renderer.Dispose();
            context.Dispose();
            args.RetVal         = true;
        }

        protected void OnHeaderCouponType1ExposeEvent(object o, ExposeEventArgs args)
        {
            if (szCouponType1 == "")
                OnHeaderExposeEvent(o, 1, "");
            else
                OnHeaderExposeEvent(o, 1, $"คูปอง {szCouponType1} บาท (เล่มละ {fCouponPrice_Type1.ToString("#,0")} บ.)");
            ShowSumSaleCoupon();
            args.RetVal = true;
        }

        protected void OnHeaderCouponType2ExposeEvent(object o, ExposeEventArgs args)
        {
            if (szCouponType2 == "")
                OnHeaderExposeEvent(o, 2, "");
            else
                OnHeaderExposeEvent(o, 2, $"คูปอง {szCouponType2} บาท (เล่มละ {fCouponPrice_Type2.ToString("#,0")} บ.)");
            ShowSumSaleCoupon();
            args.RetVal = true;
        }

        protected void OnHeaderExposeEvent(object o, int iCouponType, string szHeaderText)
        {
            DrawingArea drawArea = o as DrawingArea;
            //---  DrawArea ClassType
            int iWidth              = drawArea.Allocation.Width;
            int iHeight             = drawArea.Allocation.Height;
            Context context         = drawArea.CreatePangoContext();
            Pango.Layout layout     = new Pango.Layout(context);
            layout.Width            = Units.FromPixels(iWidth);
            layout.Alignment        = Pango.Alignment.Center;
            layout.Justify          = true;

            FontDescription fontDesc    = FontDescription.FromString("Ubuntu 18");
            layout.FontDescription      = fontDesc;

            layout.SetText(szHeaderText);

            Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
            renderer.Drawable           = drawArea.GdkWindow;
            renderer.Gc                 = drawArea.Style.BlackGC;

            //renderer.SetOverrideColor(RenderPart.Foreground, new Gdk.Color(255, 255, 255));
            if (iCouponType == 1)
            {
                SetBgIfChanged(headerCoupon_Type1, iSelHeaderCouponType == 1 ? clHeaderCoupon1_Selected : clHeaderCoupon1_UnSelect);
                drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 8, layout, iSelHeaderCouponType == 1 ? clWhite : clGray, iSelHeaderCouponType == 1 ? clHeaderCoupon1_Selected : clHeaderCoupon1_UnSelect);
            }
            else
            {
                SetBgIfChanged(headerCoupon_Type2, iSelHeaderCouponType == 2 ? clHeaderCoupon2_Selected : clHeaderCoupon2_UnSelect);
                drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 8, layout, iSelHeaderCouponType == 2 ? clWhite : clGray, iSelHeaderCouponType == 2 ? clHeaderCoupon2_Selected : clHeaderCoupon2_UnSelect);
            }

            renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
            renderer.Drawable   = null;
            renderer.Gc         = null;
            renderer.Dispose();
            context.Dispose();
        }

        protected void OnBtnType1Coupon1ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType1_ExposeEvent(o, 0);
            args.RetVal = true;
        }

        protected void OnBtnType1Coupon2ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType1_ExposeEvent(o, 1);
            args.RetVal = true;
        }

        protected void OnBtnType1Coupon3ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType1_ExposeEvent(o, 2);
            args.RetVal = true;
        }

        protected void OnBtnType1Coupon4ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType1_ExposeEvent(o, 3);
            args.RetVal = true;
        }

        protected void OnBtnType1Coupon5ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType1_ExposeEvent(o, 4);
            args.RetVal = true;
        }

        protected void OnBtnType1Coupon6ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType1_ExposeEvent(o, 5);
            args.RetVal = true;
        }

        protected void OnBtnType1Coupon7ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType1_ExposeEvent(o, 6);
            args.RetVal = true;
        }

        protected void OnBtnType1Coupon8ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType1_ExposeEvent(o, 7);
            args.RetVal = true;
        }

        protected void OnBtnType1Coupon9ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType1_ExposeEvent(o, 8);
            args.RetVal = true;
        }

        protected void OnBtnType1CouponZExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType1_ExposeEvent(o, 9);
            args.RetVal = true;
        }

        protected void OnBtnType2Coupon1ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType2_ExposeEvent(o, 0);
            args.RetVal = true;
        }

        protected void OnBtnType2Coupon2ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType2_ExposeEvent(o, 1);
            args.RetVal = true;
        }

        protected void OnBtnType2Coupon3ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType2_ExposeEvent(o, 2);
            args.RetVal = true;
        }

        protected void OnBtnType2Coupon4ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType2_ExposeEvent(o, 3);
            args.RetVal = true;
        }

        protected void OnBtnType2Coupon5ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType2_ExposeEvent(o, 4);
            args.RetVal = true;
        }

        protected void OnBtnType2Coupon6ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType2_ExposeEvent(o, 5);
            args.RetVal = true;
        }

        protected void OnBtnType2Coupon7ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType2_ExposeEvent(o, 6);
            args.RetVal = true;
        }

        protected void OnBtnType2Coupon8ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType2_ExposeEvent(o, 7);
            args.RetVal = true;
        }

        protected void OnBtnType2Coupon9ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType2_ExposeEvent(o, 8);
            args.RetVal = true;
        }

        protected void OnBtnType2CouponZExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType2_ExposeEvent(o, 9);
            args.RetVal = true;
        }

        protected void OnCouponType1_ExposeEvent(object o, int idx)
        {
            DrawingArea drawArea        = o as DrawingArea;
            //---  DrawArea ClassType
            int iWidth                  = drawArea.Allocation.Width;
            int iHeight                 = drawArea.Allocation.Height;
            Context context             = drawArea.CreatePangoContext();
            Pango.Layout layout         = new Pango.Layout(context);
            layout.Width                = Units.FromPixels(iWidth);
            layout.Alignment            = Pango.Alignment.Center;
            layout.Justify              = true;
            
            FontDescription fontDesc    = FontDescription.FromString("Ubuntu 22");
            layout.FontDescription      = fontDesc;

            //layout.SetText("\u2713  เงินสด");

            Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
            renderer.Drawable           = drawArea.GdkWindow;
            renderer.Gc                 = drawArea.Style.BlackGC;

            if (szCouponNoList_Type1[idx] == "")
                layout.SetText("");
            else
            { 
                int iKeyNo = idx == 9 ? 0 : idx + 1;
                //layout.SetText((iSelCouponList_Type1[idx] == 0 ? "" : "\u2713  ") + (iSelHeaderCouponType == 1 ? "[" + iKeyNo + "] " : "") + szCouponNoList_Type1[idx]);
                layout.SetText((iSelCouponList_Type1[idx] == 0 ? "" : "✓ ") + (iSelHeaderCouponType == 1 ? "[" + iKeyNo + "] " : "") + szCouponNoList_Type1[idx]);
            }
            Gdk.Color clBk = iSelCouponList_Type1[idx] == 0 ? clBkCoupon1_UnSelect : clBkCoupon1_Selected;
            SetBgIfChanged(drawArea, clBk);
            FillCouponBk(drawArea, clBk, iWidth, iHeight);
            int iTextY = FitCouponText(layout, iWidth, iHeight);    // v2.0.2: one line, fit width, centre
            drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, iTextY, layout, iSelCouponList_Type1[idx] == 0 ? clWhite : clFgCoupon1_Selected, clBk);
                
            renderer.Drawable       = null;
            renderer.Gc             = null;
            renderer.Dispose();
            context.Dispose();
        }

        //--- v2.0.2: "[1] SIM35-0001" at Ubuntu 22 is wider than the button -> it wrapped, the 2nd line
        //--- was clipped and Justify stretched "[ 1 ]". Keep one line, shrink the font until it fits.
        static int FitCouponText(Pango.Layout layout, int iWidth, int iHeight)
        {
            layout.Justify  = false;
            layout.Wrap     = Pango.WrapMode.Char;
            int iW, iH;
            for (int iSize = 22; ; iSize -= 2)
            {
                layout.FontDescription = FontDescription.FromString("Ubuntu " + iSize);
                layout.Width = -1;
                layout.GetPixelSize(out iW, out iH);
                if (iW <= iWidth - 8 || iSize <= 12)
                    break;
            }
            layout.Width = Units.FromPixels(iWidth);
            layout.GetPixelSize(out iW, out iH);
            return Math.Max(0, (iHeight - iH) / 2);
        }

        //--- v2.0.4: paint the whole cell now (ModifyBg inside expose only takes effect on the next clear)
        static void FillCouponBk(DrawingArea drawArea, Gdk.Color clBk, int iWidth, int iHeight)
        {
            using (var gc = new Gdk.GC(drawArea.GdkWindow))
            {
                gc.RgbFgColor = clBk;
                drawArea.GdkWindow.DrawRectangle(gc, true, 0, 0, iWidth, iHeight);
            }
        }

        protected void OnCouponType2_ExposeEvent(object o, int idx)
        {
            DrawingArea drawArea    = o as DrawingArea;
            //drawArea.ModifyBg(StateType.Normal, clBlue);
            //---  DrawArea ClassType
            int iWidth              = drawArea.Allocation.Width;
            int iHeight             = drawArea.Allocation.Height;
            Context context         = drawArea.CreatePangoContext();
            Pango.Layout layout     = new Pango.Layout(context);
            layout.Width            = Units.FromPixels(iWidth);
            layout.Alignment        = Pango.Alignment.Center;
            layout.Justify          = true;

            FontDescription fontDesc    = FontDescription.FromString("Ubuntu 22");
            layout.FontDescription      = fontDesc;

            //layout.SetText("\u2713  เงินสด");

            Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
            renderer.Drawable           = drawArea.GdkWindow;
            renderer.Gc                 = drawArea.Style.BlackGC;

            if (szCouponNoList_Type2[idx] == "")
                layout.SetText("");
            else
            {
                int iKeyNo = idx == 9 ? 0 : idx + 1;
                //layout.SetText((iSelCouponList_Type2[idx] == 0 ? "" : "\u2713  ") + (iSelHeaderCouponType == 2 ? "[" + iKeyNo + "] " : "") + szCouponNoList_Type2[idx]);
                layout.SetText((iSelCouponList_Type2[idx] == 0 ? "" : "✓ ") + (iSelHeaderCouponType == 2 ? "[" + iKeyNo + "] " : "") + szCouponNoList_Type2[idx]);
            }
            Gdk.Color clBk = iSelCouponList_Type2[idx] == 0 ? clBkCoupon2_UnSelect : clBkCoupon2_Selected;
            SetBgIfChanged(drawArea, clBk);
            FillCouponBk(drawArea, clBk, iWidth, iHeight);
            int iTextY = FitCouponText(layout, iWidth, iHeight);    // v2.0.2: one line, fit width, centre
            drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, iTextY, layout, iSelCouponList_Type2[idx] == 0 ? clWhite : clFgCoupon2_Selected, clBk);
                                   
            //renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
            renderer.Drawable   = null;
            renderer.Gc         = null;
            renderer.Dispose();
            context.Dispose();
        }

        protected void OnHeaderCouponType1ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ SalesCoupon : touch header type1");
            Action_ClickHeaderCouponType(1);
            args.RetVal = true;
        }

        protected void OnHeaderCouponType2ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ SalesCoupon : touch header type2");
            Action_ClickHeaderCouponType(2);
            args.RetVal = true;
        }

        protected void Action_ClickHeaderCouponType(int iSelCouponType)
        {
            if (bPayCompleted)
            {
                LogInfo.WriteLaneEvent(LogLevel.WARN, $"+ SalesCoupon : header type{iSelCouponType} ignored, already paid");
                return;
            }

            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ SalesCoupon : header type {iSelHeaderCouponType} -> {iSelCouponType}, redraw headers + all coupons");
            iSelHeaderCouponType = iSelCouponType;
            RedrawAllCoupons();
        }

        //--- v2.0.4: the "[n]" key prefix is drawn only on the selected header's coupons. Changing the
        //--- header used to redraw just the headers (or the one touched cell); the other cells kept the
        //--- old text, and a later partial expose (e.g. after the EMV popup closed) mixed old and new text.
        void RedrawAllCoupons()
        {
            headerCoupon_Type1.QueueDraw();
            headerCoupon_Type2.QueueDraw();
            foreach (var w in new Widget[] { btnType1_Coupon1, btnType1_Coupon2, btnType1_Coupon3, btnType1_Coupon4, btnType1_Coupon5,
                                             btnType1_Coupon6, btnType1_Coupon7, btnType1_Coupon8, btnType1_Coupon9, btnType1_CouponZ,
                                             btnType2_Coupon1, btnType2_Coupon2, btnType2_Coupon3, btnType2_Coupon4, btnType2_Coupon5,
                                             btnType2_Coupon6, btnType2_Coupon7, btnType2_Coupon8, btnType2_Coupon9, btnType2_CouponZ })
                w.QueueDraw();
        }

        protected void OnBtnType1Coupon1ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            OnBtnType1_ButtonPressEvent(o, 0);
        }

        protected void OnBtnType1Coupon2ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            OnBtnType1_ButtonPressEvent(o, 1);
        }

        protected void OnBtnType1Coupon3ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            OnBtnType1_ButtonPressEvent(o, 2);
        }

        protected void OnBtnType1Coupon4ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            OnBtnType1_ButtonPressEvent(o, 3);
        }

        protected void OnBtnType1Coupon5ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            OnBtnType1_ButtonPressEvent(o, 4);
        }

        protected void OnBtnType1Coupon6ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            OnBtnType1_ButtonPressEvent(o, 5);
        }

        protected void OnBtnType1Coupon7ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            OnBtnType1_ButtonPressEvent(o, 6);
        }

        protected void OnBtnType1Coupon8ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            OnBtnType1_ButtonPressEvent(o, 7);
        }

        protected void OnBtnType1Coupon9ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            OnBtnType1_ButtonPressEvent(o, 8);
        }

        protected void OnBtnType1CouponZButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            OnBtnType1_ButtonPressEvent(o, 9);
        }

        protected void OnBtnType2Coupon1ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            OnBtnType2_ButtonPressEvent(o, 0);
        }

        protected void OnBtnType2Coupon2ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            OnBtnType2_ButtonPressEvent(o, 1);
        }

        protected void OnBtnType2Coupon3ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            OnBtnType2_ButtonPressEvent(o, 2);
        }

        protected void OnBtnType2Coupon4ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            OnBtnType2_ButtonPressEvent(o, 3);
        }

        protected void OnBtnType2Coupon5ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            OnBtnType2_ButtonPressEvent(o, 4);
        }

        protected void OnBtnType2Coupon6ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            OnBtnType2_ButtonPressEvent(o, 5);
        }

        protected void OnBtnType2Coupon7ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            OnBtnType2_ButtonPressEvent(o, 6);
        }

        protected void OnBtnType2Coupon8ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            OnBtnType2_ButtonPressEvent(o, 7);
        }

        protected void OnBtnType2Coupon9ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            OnBtnType2_ButtonPressEvent(o, 8);
        }

        protected void OnBtnType2CouponZButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            OnBtnType2_ButtonPressEvent(o, 9);
        }

        protected void OnBtnType1_ButtonPressEvent(object o, int idx)
        {
            if (bPayCompleted)
            {
                LogInfo.WriteLaneEvent(LogLevel.WARN, $"+ SalesCoupon : coupon type1[{idx}] ignored, already paid");
                return;
            }

            if (szCouponNoList_Type1[idx] == "")
            {
                LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ SalesCoupon : coupon type1[{idx}] ignored, empty slot");
                return;
            }

            txtMsg.Text         = $"";
            txtMsg.UseMarkup    = true;


            var btn                     = (DrawingArea) o;
            bool bHeaderChanged         = iSelHeaderCouponType != 1;
            iSelHeaderCouponType        = 1;
            iSelCouponList_Type1[idx]   = iSelCouponList_Type1[idx] == 0 ? 1 : 0;
            if (bHeaderChanged)
                RedrawAllCoupons();     // v2.0.4: key prefixes move to this group
            else
                btn.QueueDraw();
            ShowSumSaleCoupon();
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ SalesCoupon : coupon type1 [{(idx == 9 ? 0 : idx + 1)}] {szCouponNoList_Type1[idx]} -> {(iSelCouponList_Type1[idx] == 1 ? "selected" : "unselected")}, {LogState()}");
        }

        protected void OnBtnType2_ButtonPressEvent(object o, int idx)
        {
            if (bPayCompleted)
            {
                LogInfo.WriteLaneEvent(LogLevel.WARN, $"+ SalesCoupon : coupon type2[{idx}] ignored, already paid");
                return;
            }

            if (szCouponNoList_Type2[idx] == "")
            {
                LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ SalesCoupon : coupon type2[{idx}] ignored, empty slot");
                return;
            }

            txtMsg.Text         = $"";
            txtMsg.UseMarkup    = true;

            var btn                     = (DrawingArea) o;
            bool bHeaderChanged         = iSelHeaderCouponType != 2;
            iSelHeaderCouponType        = 2;
            iSelCouponList_Type2[idx]   = iSelCouponList_Type2[idx] == 0 ? 1 : 0;
            if (bHeaderChanged)
                RedrawAllCoupons();     // v2.0.4: key prefixes move to this group
            else
                btn.QueueDraw();
            ShowSumSaleCoupon();
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ SalesCoupon : coupon type2 [{(idx == 9 ? 0 : idx + 1)}] {szCouponNoList_Type2[idx]} -> {(iSelCouponList_Type2[idx] == 1 ? "selected" : "unselected")}, {LogState()}");
        }

        //====================================================================================
        protected void OnBtnSelPayCashExposeEvent(object o, ExposeEventArgs args)
        {
            OnPaymentExposeEvent(o, 0, "เงินสด");
            args.RetVal = true;
        }

        protected void OnBtnSelPayEMVExposeEvent(object o, ExposeEventArgs args)
        {
            OnPaymentExposeEvent(o, 1, "EMV");
            args.RetVal = true;
        }

        protected void OnBtnSelPayQrCodeExposeEvent(object o, ExposeEventArgs args)
        {
            OnPaymentExposeEvent(o, 2, "QR Code");
            args.RetVal = true;
        }

        protected void OnPaymentExposeEvent(object o, int iPayType, string szPayTypeName)
        {
            DrawingArea drawArea        = o as DrawingArea;
            //---  DrawArea ClassType
            int iWidth                  = drawArea.Allocation.Width;
            int iHeight                 = drawArea.Allocation.Height;
            Context context             = drawArea.CreatePangoContext();
            Pango.Layout layout         = new Pango.Layout(context);
            layout.Width                = Units.FromPixels(iWidth);
            layout.Alignment            = Pango.Alignment.Center;
            layout.Justify              = true;
            
            FontDescription fontDesc    = FontDescription.FromString("Ubuntu 18");
            layout.FontDescription      = fontDesc;

            //layout.SetText("\u2713  เงินสด");

            Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
            renderer.Drawable           = drawArea.GdkWindow;
            renderer.Gc                 = drawArea.Style.BlackGC;

            if (iPayType == 0)
            {
                layout.SetText((iSelPayType == 0 ? "\u2611  " : "") + szPayTypeName);
                SetBgIfChanged(btnSelPayCash, iSelPayType == 0 ? clBkPaySelect : clBkPayUnSel);
                drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 10, layout, iSelPayType == 0 ? clFgPaySelect : clFgPayUnSel, iSelPayType == 0 ? clBkPaySelect : clBkPayUnSel);
            }
            else if (iPayType == 1)
            {
                layout.SetText((iSelPayType == 1 ? "\u2611  " : "") + szPayTypeName);
                SetBgIfChanged(btnSelPayEMV, iSelPayType == 1 ? clBkPaySelect : clBkPayUnSel);
                drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 15, layout, iSelPayType == 1 ? clFgPaySelect : clFgPayUnSel, iSelPayType == 1 ? clBkPaySelect : clBkPayUnSel);
            }
            else if (iPayType == 2)
            {
                layout.SetText((iSelPayType == 2 ? "\u2611  " : "") + szPayTypeName);
                SetBgIfChanged(btnSelPayQrCode, iSelPayType == 2 ? clBkPaySelect : clBkPayUnSel);
                drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 15, layout, iSelPayType == 2 ? clFgPaySelect : clFgPayUnSel, iSelPayType == 2 ? clBkPaySelect : clBkPayUnSel);
            }

            renderer.Drawable   = null;
            renderer.Gc         = null;
            renderer.Dispose();
            context.Dispose();
        }

        protected void OnBtnSelPayCashButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ SalesCoupon : touch pay Cash");
            Action_OnBtnSelPayment(0);
        }

        protected void OnBtnSelPayEMVButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ SalesCoupon : touch pay EMV");
            Action_OnBtnSelPayment(1);
            //----------------------------------
            Perform_ProcessEMV();
        }

        protected void OnBtnSelPayQrCodeButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ SalesCoupon : touch pay QR Code");
            Action_OnBtnSelPayment(2);
            //----------------------------------
            Perform_ProcessQrCode();
        }

        protected void Action_OnBtnSelPayment(int iPayType)
        {
            if (bPayCompleted)
            {
                LogInfo.WriteLaneEvent(LogLevel.WARN, $"+ SalesCoupon : pay type {iPayType} ignored, already paid");
                return;
            }

            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ SalesCoupon : pay type {iSelPayType} -> {iPayType} (0=Cash 1=EMV 2=QR), redraw pay buttons");
            iSelPayType     = iPayType;
            btnSelPayCash.QueueDraw();
            btnSelPayEMV.QueueDraw();
            btnSelPayQrCode.QueueDraw();
        }

        //=== v2.0.4: card payment ==========================================================================
        void BackToCash(string szReason)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ SalesCoupon : {szReason} -> back to pay Cash");
            iSelPayType = 0;
            btnSelPayCash.QueueDraw();
            btnSelPayEMV.QueueDraw();
            btnSelPayQrCode.QueueDraw();
        }

        //--- a MessageDialog on top of this popup: keys from the LaneController are ignored meanwhile
        void ShowMessage(string szText, string szLogName)
        {
            MessageDialog md = new MessageDialog(this, DialogFlags.DestroyWithParent,
                                    MessageType.Error, ButtonsType.Ok, true,
                                    "<span font_size='large' fgcolor='#000000'>" + szText + "</span>");
            UiTheme.StylePopup(md);     // v2.0.2: no title bar + visible frame
            bMsgOpen = true;
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ SalesCoupon : popup '{szLogName}' opened");
            try
            {
                md.Run();
            }
            finally
            {
                md.Destroy();
                bMsgOpen = false;
            }
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ SalesCoupon : popup '{szLogName}' closed");
        }

        //--- common checks before an EMV / QR popup may be opened
        bool CheckCanStartCardPayment(string szPayName, ref int iTotalCoupon, ref decimal fTotalAmount)
        {
            if (bPayCompleted)
            {
                //--- v2.0.4: a second [EMV]/[QR] after an approval charged the card again
                LogInfo.WriteLaneEvent(LogLevel.WARN, $"+ SalesCoupon : {szPayName} ignored, already paid");
                return false;
            }
            if (TCT_Lib.bActiveEMVDlg)
            {
                LogInfo.WriteLaneEvent(LogLevel.WARN, $"+ SalesCoupon : {szPayName} ignored, EMV dialog already open");
                return false;
            }
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ SalesCoupon : {szPayName} start, {LogState()}, check TA {TCT_Lib.szLaneWebService_ServerIPAddress}:{TCT_Lib.iLaneWebService_ServerPort}");

            //--- Check current is online, if online then open dialog for sales coupon, else ignore sales coupon
            if (!TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szLaneWebService_ServerIPAddress, TCT_Lib.iLaneWebService_ServerPort))
            {
                Console.WriteLine($"Fail Connect LaneWebService IP={TCT_Lib.szLaneWebService_ServerIPAddress}, Port={TCT_Lib.iLaneWebService_ServerPort}");
                LogInfo.WriteLog($"Fail Connect LaneWebService IP={TCT_Lib.szLaneWebService_ServerIPAddress}, Port={TCT_Lib.iLaneWebService_ServerPort}");
                LogInfo.WriteLaneEvent($"Fail Connect LaneWebService IP={TCT_Lib.szLaneWebService_ServerIPAddress}, Port={TCT_Lib.iLaneWebService_ServerPort}");
                BackToCash("TA Server offline");
                ShowMessage(@"ไม่สามารถทำการติดต่อ TA Server เพื่อขายคูปองได้. โปรดตรวจสอบระบบ Network TA Server.", "TA Server offline");
                return false;
            }

            Calc_TotalSalesCoupon(ref iTotalCoupon, ref fTotalAmount);
            if (fTotalAmount == 0)
            {
                LogInfo.WriteLaneEvent(LogLevel.WARN, $"+ SalesCoupon : {szPayName} ignored, no coupon selected");
                BackToCash("no coupon selected");
                txtMsg.Text         = $"<span font_size = 'large' fgcolor = '#FFFF00'>ไม่พบการเลือกคูปอง</span>";
                txtMsg.UseMarkup    = true;
                return false;
            }
            if (fTotalAmount >= 1500)
            {
                LogInfo.WriteLaneEvent(LogLevel.WARN, $"+ SalesCoupon : {szPayName} refused, amount {fTotalAmount} >= 1500");
                BackToCash($"{szPayName} limit");
                ShowMessage($"ไม่สามารถทำการจ่ายเงินด้วย {szPayName} ตั้งแต่ 1,500 บาทขึ้นไปได้. โปรดเลือกประเภทการจ่ายเงินใหม่", $"{szPayName} limit 1,500");
                return false;
            }
            return true;
        }

        //--- run one EDC popup for the coupon sale and return the approved card (null = not approved).
        //---  * The EDC popups reset TCT_Lib.xCurEMV / xCurEDC / xCurQR in their constructor: those are the
        //---    card data of the CURRENT CAR, written to EmvDetail only when the car passes. A coupon sale
        //---    (allowed after the car has paid) must not touch them -> save before, restore after.
        //---  * The popup is disposed here in every case (approved, declined, cancelled, exception) like
        //---    MainWindow does; before v2.0.4 a declined/cancelled popup was never disposed.
        objCreditCardInfo RunEdcDialog(string szDlgName, Func<Gtk.Dialog> fnCreate, Func<Gtk.Dialog, objCreditCardInfo> fnCard,
                                       decimal fTotalAmount, int iTotalCoupon)
        {
            EMV_Info xSaveEMV   = TCT_Lib.xCurEMV;
            EDC_Info xSaveEDC   = TCT_Lib.xCurEDC;
            EMV_Info xSaveQR    = TCT_Lib.xCurQR;
            objCreditCardInfo xCard = null;
            Gtk.Dialog frmDlg   = null;
            try
            {
                frmDlg = fnCreate();
                frmDlg.ModifyBg(StateType.Normal, new Gdk.Color(0x3E, 0x3E, 0x3E));
                LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ SalesCoupon : {szDlgName} opened, amount {fTotalAmount}, coupons {iTotalCoupon}");
                ResponseType xResult = (ResponseType)frmDlg.Run();
                LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ SalesCoupon : {szDlgName} closed -> {xResult}");
                if (xResult == ResponseType.Ok)
                {
                    xCard = fnCard(frmDlg);
                    if (xCard == null)
                        LogInfo.WriteLaneEvent(LogLevel.WARN, $"- Warning SalesCoupon : {szDlgName} answered Ok without card data -> treated as not approved");
                }
            }
            catch (Exception err)
            {
                LogInfo.WriteLaneEvent(LogLevel.ERROR, $"- Error SalesCoupon {szDlgName} : {err.Message}");
                LogInfo.WriteLaneEvent(LogLevel.DEBUG, "- SalesCoupon StackTrace : " + err.StackTrace);
                xCard = null;
            }
            finally
            {
                TCT_Lib.xCurEMV = xSaveEMV;
                TCT_Lib.xCurEDC = xSaveEDC;
                TCT_Lib.xCurQR  = xSaveQR;
                if (frmDlg != null)
                {
                    try
                    {
                        frmDlg.Dispose();
                        frmDlg.Destroy();
                    }
                    catch (Exception err)
                    {
                        LogInfo.WriteLaneEvent(LogLevel.WARN, $"- Warning SalesCoupon : dispose {szDlgName} : {err.Message}");
                    }
                }
                TCT_Lib.eCurAppPage     = AppPage.SalesCoupon;
                TCT_Lib.bActiveEMVDlg   = false;
                LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ SalesCoupon : {szDlgName} disposed, current-car card data restored, page -> SalesCoupon");
            }
            return xCard;
        }

        void AfterCardPayment(objCreditCardInfo xCard, string szPayName)
        {
            if (xCard != null)
            {
                Console.WriteLine("+ Show Response " + szPayName);
                LogInfo.WriteLaneEvent($"+ Success Pay Sales Coupon with {szPayName}");
                xCreditCardInfo             = xCard;

                txtCreditCardNo.Text        = $"<span fgcolor='#00FF00'>{"CreditCard No : " + xCard.szCardNo}</span>";
                txtCreditCardNo.UseMarkup   = true;

                txtReference.Text           = $"<span fgcolor='#00FF00'>{"Reference : " + xCard.szRef3}</span>";
                txtReference.UseMarkup      = true;

                txtMsg.Text                 = $"<span fgcolor='#00FF00'>{"Approve Code : " + xCard.szApprovalCode}</span>";
                txtMsg.UseMarkup            = true;

                LogInfo.WriteLaneEvent($"+ Pay Sales Coupon with {szPayName} : CreditCard No = " + xCard.szCardNo);
                LogInfo.WriteLaneEvent($"+ Pay Sales Coupon with {szPayName} : Reference = " + xCard.szRef3);
                LogInfo.WriteLaneEvent($"+ Pay Sales Coupon with {szPayName} : Approve Code = " + xCard.szApprovalCode);

                //--- Disable active button (cancel is also refused by Action_BtnCancel from the keyboard)
                btnCancel.Sensitive     = false;
                bPayCompleted           = true;
                this.Activate();
            }
            else
                BackToCash("payment not approved");

            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ SalesCoupon : payment step done, {LogState()}, paid {bPayCompleted}");
        }

        protected void Perform_ProcessEMV()
        {
            int iTotalCoupon        = 0;
            decimal fTotalAmount    = 0;
            if (!CheckCanStartCardPayment("EMV", ref iTotalCoupon, ref fTotalAmount))
                return;

            iSelPayType = 1;
            btnSelPayCash.QueueDraw();
            btnSelPayEMV.QueueDraw();
            btnSelPayQrCode.QueueDraw();

            objCreditCardInfo xCard;
            if (TCT_Lib.iEdcVersionType == 0)
                xCard = RunEdcDialog("EdcPaymentDlg (V0)", () => new EdcPaymentDlg(fTotalAmount, false),
                                     d => ((EdcPaymentDlg)d).xOutCreditCard, fTotalAmount, iTotalCoupon);
            else
                xCard = RunEdcDialog("EdcPaymentDlg_V1", () => new EdcPaymentDlg_V1(fTotalAmount, false),
                                     d => ((EdcPaymentDlg_V1)d).xOutCreditCard, fTotalAmount, iTotalCoupon);
            AfterCardPayment(xCard, "EMV");
        }

        protected void Perform_ProcessQrCode()
        {
            int iTotalCoupon        = 0;
            decimal fTotalAmount    = 0;
            if (!CheckCanStartCardPayment("QR Code", ref iTotalCoupon, ref fTotalAmount))
                return;

            iSelPayType = 2;
            btnSelPayCash.QueueDraw();
            btnSelPayEMV.QueueDraw();
            btnSelPayQrCode.QueueDraw();

            objCreditCardInfo xCard;
            if (TCT_Lib.iEdcVersionType == 0)
                xCard = RunEdcDialog("EdcPayQrDlg (V0)", () => new EdcPayQrDlg(fTotalAmount, false),
                                     d => ((EdcPayQrDlg)d).xOutCreditCard, fTotalAmount, iTotalCoupon);
            else
                xCard = RunEdcDialog("EdcPayQrDlg_V1", () => new EdcPayQrDlg_V1(fTotalAmount, false),
                                     d => ((EdcPayQrDlg_V1)d).xOutCreditCard, fTotalAmount, iTotalCoupon);
            AfterCardPayment(xCard, "QR Code");
        }

        protected void OnBtnConfirmPaymentClicked(object sender, EventArgs e)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ SalesCoupon : button Confirm Payment clicked");
            Action_BtnConfirmPayment();
        }

        protected void Action_BtnConfirmPayment()
        {
            Perform_SaveSale(false);
        }

        protected void OnBtnPrintReceiptClicked(object sender, EventArgs e)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ SalesCoupon : button Print Receipt clicked");
            Action_BtnPrintReceipt();
        }

        protected void Action_BtnPrintReceipt()
        {
            Perform_SaveSale(true);
        }

        //--- v2.0.4: [ยืนยัน] and [พิมพ์ใบเสร็จ] share one path. The popup closes only after the sale is in
        //--- the database; a failed INSERT keeps the popup (coupons stay unsold) so the operator can retry.
        bool Perform_SaveSale(bool bPrint)
        {
            string szAction = bPrint ? "print" : "confirm";
            if (bClosed)
            {
                LogInfo.WriteLaneEvent(LogLevel.WARN, $"+ SalesCoupon : {szAction} ignored, popup already closed");
                return false;
            }

            int iTotalCoupon        = 0;
            decimal fTotalAmount    = 0;
            Calc_TotalSalesCoupon(ref iTotalCoupon, ref fTotalAmount);
            if (iTotalCoupon == 0)
            {
                LogInfo.WriteLaneEvent(LogLevel.WARN, "+ SalesCoupon : confirm/print ignored, no coupon selected");
                txtMsg.Text         = $"<span font_size = 'large' fgcolor = '#FFFF00'>ไม่พบการเลือกคูปอง</span>";
                txtMsg.UseMarkup    = true;
                return false;
            }
            if ((iSelPayType != 0) && (!bPayCompleted || xCreditCardInfo == null))
            {
                //--- EMV / QR selected but no approval (a refused card payment normally falls back to cash)
                LogInfo.WriteLaneEvent(LogLevel.WARN, $"+ SalesCoupon : {szAction} refused, pay type {iSelPayType} selected but no card approval");
                txtMsg.Text         = $"<span font_size = 'large' fgcolor = '#FFFF00'>ยังไม่ได้รับการอนุมัติจากบัตร กรุณาชำระใหม่</span>";
                txtMsg.UseMarkup    = true;
                BackToCash("no card approval");
                return false;
            }

            int iTotalCoupon_Type1 = 0, iTotalCoupon_Type2 = 0;
            var szSelCouponType1    = new List<string>();
            var szSelCouponType2    = new List<string>();
            for (int i = 0; i < 10; i++)
            {
                if (iSelCouponList_Type1[i] == 1)
                {
                    iTotalCoupon_Type1++;
                    szSelCouponType1.Add(szCouponNoList_Type1[i]);
                    LogInfo.WriteLaneEvent("+ Select Sales Coupon Type 1 = " + szCouponNoList_Type1[i]);
                }
                if (iSelCouponList_Type2[i] == 1)
                {
                    iTotalCoupon_Type2++;
                    szSelCouponType2.Add(szCouponNoList_Type2[i]);
                    LogInfo.WriteLaneEvent("+ Select Sales Coupon Type 2 = " + szCouponNoList_Type2[i]);
                }
            }

            string szPayTypeName = TCT_Lib.GetPayMethodName((int)PayType.CASH);
            if (iSelPayType == 1)
                szPayTypeName = TCT_Lib.GetPayMethodName((int)PayType.EMV);
            else if (iSelPayType == 2)
                szPayTypeName = TCT_Lib.GetPayMethodName((int)PayType.QrCode);

            //---------------------------------------------
            if (!TCT_Lib.bMaintenanceMode)
            {
                TCT_Lib.szCurSalesCouponReceiptNo = TCT_Lib.LaneDB.GetRunningReceiptNo(PrintReceiptType.SalesCoupon);   // "CP-DD01-2020000218";
                long iTransID = TCT_Lib.LaneDB.WriteSalesCouponTransaction(iTotalCoupon_Type1, fCouponPrice_Type1, szSelCouponType1, szCouponType1,
                                                                           iTotalCoupon_Type2, fCouponPrice_Type2, szSelCouponType2, szCouponType2,
                                                                           iSelPayType, xCreditCardInfo);
                if (iTransID <= 0)
                {
                    LogInfo.WriteLaneEvent(LogLevel.ERROR, $"- Error SalesCoupon : {szAction} - sale not written to database (receipt {TCT_Lib.szCurSalesCouponReceiptNo}, {szPayTypeName}), popup kept open");
                    txtMsg.Text         = $"<span font_size = 'large' fgcolor = '#FF4444'>บันทึกการขายไม่สำเร็จ กรุณากด [ยืนยัน] อีกครั้ง</span>";
                    txtMsg.UseMarkup    = true;
                    return false;
                }
            }
            else
            {
                TCT_Lib.szCurSalesCouponReceiptNo = "CP-xxxx-yyyyy";
            }

            //--- Process Print Receipt SalesCopon
            if (bPrint)
            {
                LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ SalesCoupon : print receipt {TCT_Lib.szCurSalesCouponReceiptNo}, type1 x{iTotalCoupon_Type1}, type2 x{iTotalCoupon_Type2}, {fTotalAmount:#,0} THB, {szPayTypeName}");
                PrintSalesCouponLib.PrintSalesCoupon(szCouponType1, iTotalCoupon_Type1, fCouponPrice_Type1, szCouponType2, iTotalCoupon_Type2, fCouponPrice_Type2, szPayTypeName);
            }
            //----------------------------------------------
            LogInfo.WriteLaneEvent(LogLevel.INFO, $"+ SalesCoupon : sold receipt {TCT_Lib.szCurSalesCouponReceiptNo}, type1 x{iTotalCoupon_Type1}, type2 x{iTotalCoupon_Type2}, {fTotalAmount:#,0} THB, pay {iSelPayType} ({szPayTypeName}), printed {bPrint}");
            CloseDialog(ResponseType.Ok, bPrint ? "Ok, printed" : "Ok, no print");
            return true;
        }

        protected void OnBtnCancelClicked(object sender, EventArgs e)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ SalesCoupon : button Cancel clicked");
            Action_BtnCancel();
        }

        protected void Action_BtnCancel()
        {
            if (bClosed)
            {
                LogInfo.WriteLaneEvent(LogLevel.WARN, "+ SalesCoupon : cancel ignored, popup already closed");
                return;
            }
            if (bPayCompleted)
            {
                //--- v2.0.4: [แก้ไข] after a card approval closed the popup without a sale record (card charged)
                LogInfo.WriteLaneEvent(LogLevel.WARN, "+ SalesCoupon : cancel ignored, already paid by card -> press confirm / print");
                txtMsg.Text         = $"<span font_size = 'large' fgcolor = '#FFFF00'>ชำระด้วยบัตรแล้ว กรุณากด [ยืนยัน] หรือ [พิมพ์ใบเสร็จ]</span>";
                txtMsg.UseMarkup    = true;
                return;
            }
            CloseDialog(ResponseType.Cancel, "Cancel");
        }

        //--- v2.0.4: single close path. The popup only answers Run(); MainWindow.LaneController_SalesCoupon
        //--- disposes it afterwards (before v2.0.4 both sides called Dispose()/Destroy()).
        void CloseDialog(ResponseType xResponse, string szReason)
        {
            if (bClosed)
                return;
            bClosed = true;
            LogInfo.WriteLaneEvent(LogLevel.INFO, $"+ SalesCoupon : popup closed ({szReason}), {LogState()}, receipt {TCT_Lib.szCurSalesCouponReceiptNo}, page SalesCoupon -> Main");
            TCT_Lib.laneController.manageProcessSalesCoupon -= LaneController_ManageProcessSalesCoupon;
            TCT_Lib.eCurAppPage             = AppPage.Main;
            TCT_Lib.bActiveSalesCouponDlg   = false;
            this.Respond(xResponse);
        }
    }
}
