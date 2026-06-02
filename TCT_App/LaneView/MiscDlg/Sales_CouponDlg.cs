using System;
using System.Collections.Generic;
using System.Data;
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
        Gdk.Color clBkCoupon1_Selected      = new Gdk.Color(10, 110, 17);
        Gdk.Color clBkCoupon2_UnSelect      = new Gdk.Color(10, 110, 170);
        Gdk.Color clBkCoupon2_Selected      = new Gdk.Color(32, 74, 135);

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
        //string szCouponType1 = "35", szCouponType2 = "80";
        string szCouponType1 = "", szCouponType2 = "";
        //------------------------------------------------------------------------
        public Sales_CouponDlg()
        {
            TCT_Lib.bActiveSalesCouponDlg = true;

            this.Build();

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
                fCouponPrice_Type1 = decimal.Parse(dtCoupon1.Rows[0]["CouponPrice"].ToString());
                LogInfo.WriteLaneEvent("+ Load Coupon Price = " + decimal.Parse(dtCoupon1.Rows[0]["CouponPrice"].ToString()));
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
                fCouponPrice_Type2 = decimal.Parse(dtCoupon2.Rows[0]["CouponPrice"].ToString());
                LogInfo.WriteLaneEvent("+ Load Coupon Price = " + decimal.Parse(dtCoupon2.Rows[0]["CouponPrice"].ToString()));
            }
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
            if (TCT_Lib.keyMapping.ContainsKey((int)args.Event.KeyValue))
                TCT_Lib.webSocketLane.SendVirtualKeyboard(TCT_Lib.keyMapping[(int)args.Event.KeyValue], 1);
        }

        private void LaneController_ManageProcessSalesCoupon(int iKey)
        {
            Application.Invoke(delegate
            {
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
                headerCoupon_Type1.ModifyBg(StateType.Normal, iSelHeaderCouponType == 1 ? clHeaderCoupon1_Selected : clHeaderCoupon1_UnSelect);
                drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 8, layout, iSelHeaderCouponType == 1 ? clWhite : clGray, iSelHeaderCouponType == 1 ? clHeaderCoupon1_Selected : clHeaderCoupon1_UnSelect);
            }
            else
            {
                headerCoupon_Type2.ModifyBg(StateType.Normal, iSelHeaderCouponType == 2 ? clHeaderCoupon2_Selected : clHeaderCoupon2_UnSelect);
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
                layout.SetText((iSelHeaderCouponType == 1 ? "[" + iKeyNo + "] " : "") + szCouponNoList_Type1[idx]);
            }
            drawArea.ModifyBg(StateType.Normal, iSelCouponList_Type1[idx] == 0 ? clBkCoupon1_UnSelect : clBkCoupon1_Selected);
            drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 20, layout, iSelCouponList_Type1[idx] == 0 ? clWhite : clGray,
                                        iSelCouponList_Type1[idx] == 0 ? clBkCoupon1_UnSelect : clBkCoupon1_Selected);
                
            renderer.Drawable       = null;
            renderer.Gc             = null;
            renderer.Dispose();
            context.Dispose();
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
                layout.SetText((iSelHeaderCouponType == 2 ? "[" + iKeyNo + "] " : "") + szCouponNoList_Type2[idx]);
            }
            drawArea.ModifyBg(StateType.Normal, iSelCouponList_Type2[idx] == 0 ? clBkCoupon2_UnSelect : clBkCoupon2_Selected);
            drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 20, layout, iSelCouponList_Type2[idx] == 0 ? clWhite : clGray,
                                        iSelCouponList_Type2[idx] == 0 ? clBkCoupon2_UnSelect : clBkCoupon2_Selected);
                                   
            //renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
            renderer.Drawable   = null;
            renderer.Gc         = null;
            renderer.Dispose();
            context.Dispose();
        }

        protected void OnHeaderCouponType1ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            Action_ClickHeaderCouponType(1);
            args.RetVal = true;
        }

        protected void OnHeaderCouponType2ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            Action_ClickHeaderCouponType(2);
            args.RetVal = true;
        }

        protected void Action_ClickHeaderCouponType(int iSelCouponType)
        {
            if (bPayCompleted)
                return;

            iSelHeaderCouponType = iSelCouponType;
            headerCoupon_Type1.QueueDraw();
            headerCoupon_Type2.QueueDraw();
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
                return;

            if (szCouponNoList_Type1[idx] == "")
                return;

            txtMsg.Text         = $"";
            txtMsg.UseMarkup    = true;


            var btn                     = (DrawingArea) o;
            iSelHeaderCouponType        = 1;
            iSelCouponList_Type1[idx]   = iSelCouponList_Type1[idx] == 0 ? 1 : 0;
            btn.QueueDraw();
            ShowSumSaleCoupon();
        }

        protected void OnBtnType2_ButtonPressEvent(object o, int idx)
        {
            if (bPayCompleted)
                return;

            if (szCouponNoList_Type2[idx] == "")
                return;

            txtMsg.Text         = $"";
            txtMsg.UseMarkup    = true;

            var btn                     = (DrawingArea) o;
            iSelHeaderCouponType        = 2;
            iSelCouponList_Type2[idx]   = iSelCouponList_Type2[idx] == 0 ? 1 : 0;
            btn.QueueDraw();
            ShowSumSaleCoupon();
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
                btnSelPayCash.ModifyBg(StateType.Normal, iSelPayType == 0 ? clBkPaySelect : clBkPayUnSel);
                drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 10, layout, iSelPayType == 0 ? clFgPaySelect : clFgPayUnSel, iSelPayType == 0 ? clBkPaySelect : clBkPayUnSel);
            }
            else if (iPayType == 1)
            {
                layout.SetText((iSelPayType == 1 ? "\u2611  " : "") + szPayTypeName);
                btnSelPayEMV.ModifyBg(StateType.Normal, iSelPayType == 1 ? clBkPaySelect : clBkPayUnSel);
                drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 15, layout, iSelPayType == 1 ? clFgPaySelect : clFgPayUnSel, iSelPayType == 1 ? clBkPaySelect : clBkPayUnSel);
            }
            else if (iPayType == 2)
            {
                layout.SetText((iSelPayType == 2 ? "\u2611  " : "") + szPayTypeName);
                btnSelPayQrCode.ModifyBg(StateType.Normal, iSelPayType == 2 ? clBkPaySelect : clBkPayUnSel);
                drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 15, layout, iSelPayType == 2 ? clFgPaySelect : clFgPayUnSel, iSelPayType == 2 ? clBkPaySelect : clBkPayUnSel);
            }

            renderer.Drawable   = null;
            renderer.Gc         = null;
            renderer.Dispose();
            context.Dispose();
        }

        protected void OnBtnSelPayCashButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            Action_OnBtnSelPayment(0);
        }

        protected void OnBtnSelPayEMVButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            Action_OnBtnSelPayment(1);
            //----------------------------------
            Perform_ProcessEMV();
        }

        protected void OnBtnSelPayQrCodeButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            Action_OnBtnSelPayment(2);
            //----------------------------------
            Perform_ProcessQrCode();
        }

        protected void Action_OnBtnSelPayment(int iPayType)
        {
            if (bPayCompleted)
                return;

            iSelPayType     = iPayType;
            btnSelPayCash.QueueDraw();
            btnSelPayEMV.QueueDraw();
            btnSelPayQrCode.QueueDraw();
        }

        protected void Perform_ProcessEMV()
        {
            if (TCT_Lib.bActiveEMVDlg)
                return;

            //----------------------------------------
            //--- Check current is online, if online then open dialog for sales coupon, else ignore sales coupon
            if (!TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szLaneWebService_ServerIPAddress, TCT_Lib.iLaneWebService_ServerPort))
            {
                Console.WriteLine($"Fail Connect LaneWebService IP={TCT_Lib.szLaneWebService_ServerIPAddress}, Port={TCT_Lib.iLaneWebService_ServerPort}");
                LogInfo.WriteLog($"Fail Connect LaneWebService IP={TCT_Lib.szLaneWebService_ServerIPAddress}, Port={TCT_Lib.iLaneWebService_ServerPort}");
                LogInfo.WriteLaneEvent($"Fail Connect LaneWebService IP={TCT_Lib.szLaneWebService_ServerIPAddress}, Port={TCT_Lib.iLaneWebService_ServerPort}");

                string str          = @"ไม่สามารถทำการติดต่อ TA Server เพื่อขายคูปองได้. โปรดตรวจสอบระบบ Network TA Server.";
                MessageDialog md    = new MessageDialog(this, DialogFlags.DestroyWithParent,
                MessageType.Error, ButtonsType.Ok, true,
                "<span font_size='large' fgcolor='#000000'>" + str + "</span >");
                //"ไม่สามารถทำการติดต่อ TA Server เพื่อขายคูปองได้.\r\n\r\nโปรดตรวจสอบระบบ Network TA Server.");
                md.Run();
                md.Destroy();

                return;
            }

            //---------------------------------------------------
            int iTotalCoupon        = 0;
            decimal fTotalAmount    = 0;
            Calc_TotalSalesCoupon(ref iTotalCoupon, ref fTotalAmount);
            if (fTotalAmount == 0)
            {
                iSelPayType = 0;
                btnSelPayCash.QueueDraw();
                btnSelPayEMV.QueueDraw();
                btnSelPayQrCode.QueueDraw();

                txtMsg.Text         = $"<span font_size = 'large' fgcolor = '#FFFF00'>ไม่พบการเลือกคูปอง</span>";
                txtMsg.UseMarkup    = true;
                return;
            }
           
            if (fTotalAmount >= 1500)
            {
                iSelPayType = 0;
                btnSelPayCash.QueueDraw();
                btnSelPayEMV.QueueDraw();
                btnSelPayQrCode.QueueDraw();

                string str = @"ไม่สามารถทำการจ่ายเงินด้วย EMV เกิน 1,500 บาทได้. โปรดเลือกประเภทการจ่ายเงินใหม่";
                MessageDialog md = new MessageDialog(this, DialogFlags.DestroyWithParent,
                MessageType.Error, ButtonsType.Ok, true,
                "<span font_size='large' fgcolor='#000000'>" + str + "</span >");
                //"ไม่สามารถทำการติดต่อ TA Server เพื่อขายคูปองได้.\r\n\r\nโปรดตรวจสอบระบบ Network TA Server.");
                md.Run();
                md.Destroy();
                return;
            }

            iSelPayType = 1;
            btnSelPayCash.QueueDraw();
            btnSelPayEMV.QueueDraw();
            btnSelPayQrCode.QueueDraw();

            //-------------------------------------------------------------------
            if (TCT_Lib.iEdcVersionType == 0)
            {
                var frmEdcPaymentDlg    = new EdcPaymentDlg(fTotalAmount, false);
                Gdk.Color colBk         = new Gdk.Color(0x3E, 0x3E, 0x3E);
                frmEdcPaymentDlg.ModifyBg(StateType.Normal, colBk);
                Console.WriteLine("+ Before Show dialog EMV");
                ResponseType xResult    = (ResponseType) frmEdcPaymentDlg.Run();

                if (xResult == ResponseType.Ok)
                {
                    Console.WriteLine("+ Show Response EMV");
                    LogInfo.WriteLaneEvent("+ Success Pay Sales Coupon with EMV");
                    xCreditCardInfo             = frmEdcPaymentDlg.xOutCreditCard;

                    txtCreditCardNo.Text        = $"<span fgcolor='#00FF00'>{"CreditCard No : " + xCreditCardInfo.szCardNo}</span>";
                    txtCreditCardNo.UseMarkup   = true;

                    txtReference.Text           = $"<span fgcolor='#00FF00'>{"Reference : " + xCreditCardInfo.szRef3}</span>";
                    txtReference.UseMarkup      = true;

                    txtMsg.Text                 = $"<span fgcolor='#00FF00'>{"Approve Code : " + xCreditCardInfo.szApprovalCode}</span>";
                    txtMsg.UseMarkup            = true;

                    LogInfo.WriteLaneEvent("+ Pay Sales Coupon with EMV : CreditCard No = "+xCreditCardInfo.szCardNo);
                    LogInfo.WriteLaneEvent("+ Pay Sales Coupon with EMV : Reference = " + xCreditCardInfo.szRef3);
                    LogInfo.WriteLaneEvent("+ Pay Sales Coupon with EMV : Approve Code = " + xCreditCardInfo.szApprovalCode);

                    //Console.WriteLine("+ After show Response");
                    frmEdcPaymentDlg.Dispose();
                    //Console.WriteLine("+ After Dispose");
                    //--- Disable active button
                    //panelCoupon_Type1.Sensitive = panelCoupon_Type2.Sensitive = false;
                    btnCancel.Sensitive     = false;
                    bPayCompleted           = true;
                    this.Activate();
                }
                else
                {
                    //--- Back to Pay Cash
                    iSelPayType = 0;
                    btnSelPayCash.QueueDraw();
                    btnSelPayEMV.QueueDraw();
                    btnSelPayQrCode.QueueDraw();
                }
            }
            else
            {
                var frmEdcPaymentDlg    = new EdcPaymentDlg_V1(fTotalAmount, false);
                Gdk.Color colBk         = new Gdk.Color(0x3E, 0x3E, 0x3E);
                frmEdcPaymentDlg.ModifyBg(StateType.Normal, colBk);
                Console.WriteLine("+ Before Show dialog EMV");
                ResponseType xResult    = (ResponseType) frmEdcPaymentDlg.Run();

                if (xResult == ResponseType.Ok)
                {
                    Console.WriteLine("+ Show Response EMV");
                    LogInfo.WriteLaneEvent("+ Success Pay Sales Coupon with EMV");
                    xCreditCardInfo             = frmEdcPaymentDlg.xOutCreditCard;

                    txtCreditCardNo.Text        = $"<span fgcolor='#00FF00'>{"CreditCard No : " + xCreditCardInfo.szCardNo}</span>";
                    txtCreditCardNo.UseMarkup   = true;

                    txtReference.Text           = $"<span fgcolor='#00FF00'>{"Reference : " + xCreditCardInfo.szRef3}</span>";
                    txtReference.UseMarkup      = true;

                    txtMsg.Text                 = $"<span fgcolor='#00FF00'>{"Approve Code : " + xCreditCardInfo.szApprovalCode}</span>";
                    txtMsg.UseMarkup            = true;

                    LogInfo.WriteLaneEvent("+ Pay Sales Coupon with EMV : CreditCard No = "+xCreditCardInfo.szCardNo);
                    LogInfo.WriteLaneEvent("+ Pay Sales Coupon with EMV : Reference = " + xCreditCardInfo.szRef3);
                    LogInfo.WriteLaneEvent("+ Pay Sales Coupon with EMV : Approve Code = " + xCreditCardInfo.szApprovalCode);

                    //Console.WriteLine("+ After show Response");
                    frmEdcPaymentDlg.Dispose();
                    //Console.WriteLine("+ After Dispose");
                    //--- Disable active button
                    //panelCoupon_Type1.Sensitive = panelCoupon_Type2.Sensitive = false;
                    btnCancel.Sensitive     = false;
                    bPayCompleted           = true;
                    this.Activate();
                }
                else
                {
                    //--- Back to Pay Cash
                    iSelPayType = 0;
                    btnSelPayCash.QueueDraw();
                    btnSelPayEMV.QueueDraw();
                    btnSelPayQrCode.QueueDraw();
                }
            }

            TCT_Lib.eCurAppPage     = AppPage.SalesCoupon;
            TCT_Lib.bActiveEMVDlg   = false;
        }

        protected void Perform_ProcessQrCode()
        {
            if (TCT_Lib.bActiveEMVDlg)
                return;

            //---------------------------------------------------
            int iTotalCoupon        = 0;
            decimal fTotalAmount    = 0;
            Calc_TotalSalesCoupon(ref iTotalCoupon, ref fTotalAmount);
            if (fTotalAmount == 0)
            {
                iSelPayType     = 0;
                btnSelPayCash.QueueDraw();
                btnSelPayEMV.QueueDraw();
                btnSelPayQrCode.QueueDraw();

                txtMsg.Text         = $"<span font_size = 'large' fgcolor = '#FFFF00'>ไม่พบการเลือกคูปอง</span>";
                txtMsg.UseMarkup    = true;

                return;
            }

            /*
                        if (fTotalAmount >= 1500)
                        {
                            iSelPayType = 0;
                            btnSelPayCash.QueueDraw();
                            btnSelPayEMV.QueueDraw();
                            btnSelPayQrCode.QueueDraw();
                            return;
                        }
            */

            if (fTotalAmount >= 1500)
            {
                iSelPayType = 0;
                btnSelPayCash.QueueDraw();
                btnSelPayEMV.QueueDraw();
                btnSelPayQrCode.QueueDraw();

                string str          = @"ไม่สามารถทำการจ่ายเงินด้วย EMV เกิน 1,500 บาทได้. โปรดเลือกประเภทการจ่ายเงินใหม่";
                MessageDialog md    = new MessageDialog(this, DialogFlags.DestroyWithParent,
                MessageType.Error, ButtonsType.Ok, true,
                "<span font_size='large' fgcolor='#000000'>" + str + "</span >");
                //"ไม่สามารถทำการติดต่อ TA Server เพื่อขายคูปองได้.\r\n\r\nโปรดตรวจสอบระบบ Network TA Server.");
                md.Run();
                md.Destroy();
                return;
            }
            //-------------------------------------------------------------------
            if (TCT_Lib.iEdcVersionType == 0)
            {
                var frmEdcPaymentDlg    = new EdcPayQrDlg(fTotalAmount, false);
                Gdk.Color colBk         = new Gdk.Color(0x3E, 0x3E, 0x3E);
                frmEdcPaymentDlg.ModifyBg(StateType.Normal, colBk);
                Console.WriteLine("+ Before Show dialog QR Code");
                ResponseType xResult    = (ResponseType) frmEdcPaymentDlg.Run();

                if (xResult == ResponseType.Ok)
                {
                    Console.WriteLine("+ Show Response QR Code");
                    LogInfo.WriteLaneEvent("+ Success Pay Sales Coupon with QR Code");
                    xCreditCardInfo             = frmEdcPaymentDlg.xOutCreditCard;

                    txtCreditCardNo.Text        = $"<span fgcolor='#00FF00'>{"CreditCard No : " + xCreditCardInfo.szCardNo}</span>";
                    txtCreditCardNo.UseMarkup   = true;

                    txtReference.Text           = $"<span fgcolor='#00FF00'>{"Reference : " + xCreditCardInfo.szRef3}</span>";
                    txtReference.UseMarkup      = true;

                    txtMsg.Text                 = $"<span fgcolor='#00FF00'>{"Approve Code : " + xCreditCardInfo.szApprovalCode}</span>";
                    txtMsg.UseMarkup            = true;

                    LogInfo.WriteLaneEvent("+ Pay Sales Coupon with QR Code : CreditCard No = " + xCreditCardInfo.szCardNo);
                    LogInfo.WriteLaneEvent("+ Pay Sales Coupon with QR Code : Reference = " + xCreditCardInfo.szRef3);
                    LogInfo.WriteLaneEvent("+ Pay Sales Coupon with QR Code : Approve Code = " + xCreditCardInfo.szApprovalCode);

                    //Console.WriteLine("+ After show Response");
                    frmEdcPaymentDlg.Dispose();
                    //Console.WriteLine("+ After Dispose");
                    //--- Disable active button
                    //panelCoupon_Type1.Sensitive = panelCoupon_Type2.Sensitive = false;
                    btnCancel.Sensitive     = false;
                    bPayCompleted           = true;
                    this.Activate();
                }
                else
                {
                    //--- Back to Pay Cash
                    iSelPayType = 0;
                    btnSelPayCash.QueueDraw();
                    btnSelPayEMV.QueueDraw();
                    btnSelPayQrCode.QueueDraw();
                }
            }
            else
            {
                var frmEdcPaymentDlg    = new EdcPayQrDlg_V1(fTotalAmount, false);
                Gdk.Color colBk         = new Gdk.Color(0x3E, 0x3E, 0x3E);
                frmEdcPaymentDlg.ModifyBg(StateType.Normal, colBk);
                Console.WriteLine("+ Before Show dialog QR Code");
                ResponseType xResult    = (ResponseType) frmEdcPaymentDlg.Run();

                if (xResult == ResponseType.Ok)
                {
                    Console.WriteLine("+ Show Response QR Code");
                    LogInfo.WriteLaneEvent("+ Success Pay Sales Coupon with QR Code");
                    xCreditCardInfo             = frmEdcPaymentDlg.xOutCreditCard;

                    txtCreditCardNo.Text        = $"<span fgcolor='#00FF00'>{"CreditCard No : " + xCreditCardInfo.szCardNo}</span>";
                    txtCreditCardNo.UseMarkup   = true;

                    txtReference.Text           = $"<span fgcolor='#00FF00'>{"Reference : " + xCreditCardInfo.szRef3}</span>";
                    txtReference.UseMarkup      = true;

                    txtMsg.Text                 = $"<span fgcolor='#00FF00'>{"Approve Code : " + xCreditCardInfo.szApprovalCode}</span>";
                    txtMsg.UseMarkup            = true;

                    LogInfo.WriteLaneEvent("+ Pay Sales Coupon with QR Code : CreditCard No = " + xCreditCardInfo.szCardNo);
                    LogInfo.WriteLaneEvent("+ Pay Sales Coupon with QR Code : Reference = " + xCreditCardInfo.szRef3);
                    LogInfo.WriteLaneEvent("+ Pay Sales Coupon with QR Code : Approve Code = " + xCreditCardInfo.szApprovalCode);

                    //Console.WriteLine("+ After show Response");
                    frmEdcPaymentDlg.Dispose();
                    //Console.WriteLine("+ After Dispose");
                    //--- Disable active button
                    //panelCoupon_Type1.Sensitive = panelCoupon_Type2.Sensitive = false;
                    btnCancel.Sensitive     = false;
                    bPayCompleted           = true;
                    this.Activate();
                }
                else
                {
                    //--- Back to Pay Cash
                    iSelPayType = 0;
                    btnSelPayCash.QueueDraw();
                    btnSelPayEMV.QueueDraw();
                    btnSelPayQrCode.QueueDraw();
                }
            }

            TCT_Lib.eCurAppPage     = AppPage.SalesCoupon;
            TCT_Lib.bActiveEMVDlg   = false;
        }

        protected void OnBtnConfirmPaymentClicked(object sender, EventArgs e)
        {
            Action_BtnConfirmPayment();
        }

        protected void Action_BtnConfirmPayment()
        { 
            int iTotalCoupon        = 0;
            decimal fTotalAmount    = 0;
            Calc_TotalSalesCoupon(ref iTotalCoupon, ref fTotalAmount);
            if (iTotalCoupon == 0)
            {
                txtMsg.Text         = $"<span font_size = 'large' fgcolor = '#FFFF00'>ไม่พบการเลือกคูปอง</span>";
                txtMsg.UseMarkup    = true;
                return;
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
            //---------------------------------------------
            if (!TCT_Lib.bMaintenanceMode)
            {
                TCT_Lib.szCurSalesCouponReceiptNo = TCT_Lib.LaneDB.GetRunningReceiptNo(PrintReceiptType.SalesCoupon);   // "CP-DD01-2020000218";
                TCT_Lib.LaneDB.WriteSalesCouponTransaction(iTotalCoupon_Type1, fCouponPrice_Type1, szSelCouponType1, szCouponType1, iTotalCoupon_Type2, fCouponPrice_Type2, szSelCouponType2, szCouponType2, iSelPayType, xCreditCardInfo);
            }
            else
            {
                TCT_Lib.szCurSalesCouponReceiptNo = "CP-xxxx-yyyyy";
            }
            //---------------------------------------------
            this.Respond(ResponseType.Ok);
            TCT_Lib.eCurAppPage             = AppPage.Main;
            TCT_Lib.bActiveSalesCouponDlg   = false;
            TCT_Lib.laneController.manageProcessSalesCoupon -= LaneController_ManageProcessSalesCoupon;
            Destroy();
            Dispose();
        }

        protected void OnBtnPrintReceiptClicked(object sender, EventArgs e)
        {
            Action_BtnPrintReceipt();
        }

        protected void Action_BtnPrintReceipt()
        {
            int iTotalCoupon        = 0;
            decimal fTotalAmount    = 0;
            Calc_TotalSalesCoupon(ref iTotalCoupon, ref fTotalAmount);
            if (iTotalCoupon == 0)
            {
                txtMsg.Text         = $"<span font_size = 'large' fgcolor = '#FFFF00'>ไม่พบการเลือกคูปอง</span>";
                txtMsg.UseMarkup    = true;
                return;
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
                }
                if (iSelCouponList_Type2[i] == 1)
                {
                    iTotalCoupon_Type2++;
                    szSelCouponType2.Add(szCouponNoList_Type2[i]);
                }
            }
            //---------------------------------------------
            if (!TCT_Lib.bMaintenanceMode)
            {
                TCT_Lib.szCurSalesCouponReceiptNo = TCT_Lib.LaneDB.GetRunningReceiptNo(PrintReceiptType.SalesCoupon);   // "CP-DD01-2020000218";
                TCT_Lib.LaneDB.WriteSalesCouponTransaction(iTotalCoupon_Type1, fCouponPrice_Type1, szSelCouponType1, szCouponType1, iTotalCoupon_Type2, fCouponPrice_Type2, szSelCouponType2, szCouponType2, iSelPayType, xCreditCardInfo);
            }
            else
            {
                TCT_Lib.szCurSalesCouponReceiptNo = "CP-xxxx-yyyyy";
            }
            //--- Process Print Receipt SalesCopon
            string szPayTypeName = TCT_Lib.GetPayMethodName((int)PayType.CASH);
            if (iSelPayType == 1)
                szPayTypeName = TCT_Lib.GetPayMethodName((int)PayType.EMV);
            else if (iSelPayType == 2)
                szPayTypeName = TCT_Lib.GetPayMethodName((int)PayType.QrCode);

            PrintSalesCouponLib.PrintSalesCoupon(szCouponType1, iTotalCoupon_Type1, fCouponPrice_Type1, szCouponType2, iTotalCoupon_Type2, fCouponPrice_Type2, szPayTypeName);
            //----------------------------------------------
            this.Respond(ResponseType.Ok);
            TCT_Lib.eCurAppPage             = AppPage.Main;
            TCT_Lib.bActiveSalesCouponDlg   = false;
            TCT_Lib.laneController.manageProcessSalesCoupon -= LaneController_ManageProcessSalesCoupon;
            Destroy();
            Dispose();
        }

        protected void OnBtnCancelClicked(object sender, EventArgs e)
        {
            Action_BtnCancel();
        }

        protected void Action_BtnCancel()
        {
            this.Respond(ResponseType.Cancel);
            TCT_Lib.laneController.manageProcessSalesCoupon -= LaneController_ManageProcessSalesCoupon;
            TCT_Lib.eCurAppPage             = AppPage.Main;
            TCT_Lib.bActiveSalesCouponDlg   = false;
            Dispose();
            Destroy();
        }


    }
}
