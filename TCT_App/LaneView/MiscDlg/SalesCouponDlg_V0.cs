using System;
using System.Collections.Generic;
using Gtk;
using Pango;
using TCT_App.EMV.V0;
using TCT_App.PrintUtils;

namespace TCT_App.LaneView.MiscDlg
{
    public partial class SalesCouponDlg : Gtk.Dialog
    {
        Gdk.Color clBlue                    = new Gdk.Color(10, 110, 170);
        Gdk.Color clBlack                   = new Gdk.Color(0, 0, 0);
        Gdk.Color clWhite                   = new Gdk.Color(255, 255, 255);
        Gdk.Color clGray                    = new Gdk.Color(150, 150, 150);

        Gdk.Color clHeaderCoupon1_UnSelect  = new Gdk.Color(80, 90, 60);
        Gdk.Color clHeaderCoupon1_Selected  = new Gdk.Color(255, 255, 40);
        Gdk.Color clHeaderCoupon2_UnSelect  = new Gdk.Color(70, 70, 100);
        Gdk.Color clHeaderCoupon2_Selected  = new Gdk.Color(120, 120, 200);

        Gdk.Color clBkCoupon1_UnSelect      = new Gdk.Color(80, 90, 60);
        Gdk.Color clBkCoupon1_Selected      = new Gdk.Color(255, 255, 40);
        Gdk.Color clBkCoupon2_UnSelect      = new Gdk.Color(70, 70, 100);
        Gdk.Color clBkCoupon2_Selected      = new Gdk.Color(120, 120, 200);

        Gdk.Color clBkPayUnSel          = new Gdk.Color(50, 50, 50);
        Gdk.Color clBkPaySelect         = new Gdk.Color(30, 130, 30);
        Gdk.Color clFgPayUnSel          = new Gdk.Color(150, 150, 150);
        Gdk.Color clFgPaySelect         = new Gdk.Color(255, 255, 255);

        int iSelHeaderCouponType        = 1;    // 1=Coupon 35, 2=Coupon 80
        int iSelPayType                 = 0;    // 0=Cash, 1=EMV, 2=QR Code
        bool bPayCompleted              = false;

        int[] iSelCouponList_Type1      = new int[10];
        int[] iSelCouponList_Type2      = new int[10];
        string[] szCouponNoList_Type1   = new string[10];
        string[] szCouponNoList_Type2   = new string[10];
        decimal fCouponPrice_Type1 = 0, fCouponPrice_Type2 = 0;

        int iCount = 0;
        //------------------------------------------------------------------------
        public SalesCouponDlg()
        {
            this.Build();
            UiTheme.StylePopup(this);     // v2.0.2: no title bar + visible frame

            TCT_Lib.eCurAppPage     = AppPage.SalesCoupon;
            iSelHeaderCouponType    = 1;
            iSelPayType             = 0;
            InitUI();

            //txtCreditCardNo.Text = txtReference.Text = txtMsg.Text = "";
            for (int i=0; i<10; i++)
            {
                iSelCouponList_Type1[i] = 0;
                iSelCouponList_Type2[i] = 0;
                szCouponNoList_Type1[i] = "";
                szCouponNoList_Type2[i] = "";
            }
            LoadSalesCouponData();
            iSelCouponList_Type1[0] = iSelCouponList_Type2[0] = 1;
            ShowSumSaleCoupon();
            //-----------------------------------------------------------------------------
            TCT_Lib.laneController.manageProcessSalesCoupon += LaneController_ManageProcessSalesCoupon;

            this.Activate();
        }

        private void InitUI()
        {
            this.Title = "Sales Coupons";

            //areaHeader.ModifyBg(StateType.Normal, clBlue);
            //areaHeader.QueueDraw();

            //lblTotalCoupon.ModifyFont(FontDescription.FromString("Ubuntu Bold 14"));
            //lblTotalCoupon.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
            //lblTotalAmount.ModifyFont(FontDescription.FromString("Ubuntu Bold 14"));
            //lblTotalAmount.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

            //btnConfirmPayment.Child.ModifyFont(FontDescription.FromString("Arial 16"));
            //btnPrintReceipt.Child.ModifyFont(FontDescription.FromString("Arial 16"));
            //btnConfirmPayment.Child.ModifyFont(FontDescription.FromString("Arial 16"));

            //headerCoupon_Type1.QueueDraw();
            //headerCoupon_Type2.QueueDraw();
        }

        protected void LoadSalesCouponData()
        {
            for (int i = 0; i < 10; i++)
            {
                szCouponNoList_Type1[i] = "C100" + (i+111).ToString("000");
                szCouponNoList_Type2[i] = "C400" + (i+666).ToString("000");
            }
            fCouponPrice_Type1  = 665;
            fCouponPrice_Type2  = 1520;
        }

        void LaneController_ManageProcessSalesCoupon(int iKey)
        {


        }

        protected void OnAreaHeaderExposeEvent(object o, ExposeEventArgs args)
        {
            DrawingArea drawArea    = o as DrawingArea;
            //drawArea.ModifyBg(StateType.Normal, clBlue);
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
            renderer.Gc                 = drawArea.Style.BlackGC;

            renderer.SetOverrideColor(RenderPart.Foreground, new Gdk.Color(255, 255, 255));
            //renderer.DrawLayout(layout, 0, 0);
            drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 4, layout, new Gdk.Color(255, 255, 255), clBlue);

            renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
            renderer.Drawable           = null;
            renderer.Gc                 = null;
            renderer.Dispose();
            context.Dispose();
        }

        protected void OnButtonCancelClicked(object sender, EventArgs e)
        {
/*
            headerCoupon_Type1.Dispose();
            headerCoupon_Type2.Dispose();
            btnType1_Coupon1.Dispose();
            btnType1_Coupon2.Dispose();
            btnType1_Coupon3.Dispose();
            btnType1_Coupon4.Dispose();
            btnType1_Coupon5.Dispose();
            btnType1_Coupon6.Dispose();
            btnType1_Coupon7.Dispose();
            btnType1_Coupon8.Dispose();
            btnType1_Coupon9.Dispose();
            btnType1_CouponZ.Dispose();
            btnType2_Coupon1.Dispose();
            btnType2_Coupon2.Dispose();
            btnType2_Coupon3.Dispose();
            btnType2_Coupon4.Dispose();
            btnType2_Coupon5.Dispose();
            btnType2_Coupon6.Dispose();
            btnType2_Coupon7.Dispose();
            btnType2_Coupon8.Dispose();
            btnType2_Coupon9.Dispose();
            btnType2_CouponZ.Dispose();

            btnSelPayCash.Dispose();
            btnSelPayEMV.Dispose();
*/                       
            //--------------------------------------------------
            TCT_Lib.eCurAppPage = AppPage.Main;
            Dispose();
            Destroy();
        }

        private void RefreshDrawingArea()
        {
            Application.Invoke(delegate
            {
/*
                OnHeaderCouponType1ExposeEvent(headerCoupon_Type1, null);
                OnHeaderCouponType2ExposeEvent(headerCoupon_Type2, null);

                OnCouponType1_ExposeEvent(btnType1_Coupon1, 0);
                OnCouponType1_ExposeEvent(btnType1_Coupon2, 1);
                OnCouponType1_ExposeEvent(btnType1_Coupon3, 2);
                OnCouponType1_ExposeEvent(btnType1_Coupon4, 3);
                OnCouponType1_ExposeEvent(btnType1_Coupon5, 4);
                OnCouponType1_ExposeEvent(btnType1_Coupon6, 5);
                OnCouponType1_ExposeEvent(btnType1_Coupon7, 6);
                OnCouponType1_ExposeEvent(btnType1_Coupon8, 7);
                OnCouponType1_ExposeEvent(btnType1_Coupon9, 8);
                OnCouponType1_ExposeEvent(btnType1_CouponZ, 9);

                OnCouponType2_ExposeEvent(btnType2_Coupon1, 0);
                OnCouponType2_ExposeEvent(btnType2_Coupon2, 1);
                OnCouponType2_ExposeEvent(btnType2_Coupon3, 2);
                OnCouponType2_ExposeEvent(btnType2_Coupon4, 3);
                OnCouponType2_ExposeEvent(btnType2_Coupon5, 4);
                OnCouponType2_ExposeEvent(btnType2_Coupon6, 5);
                OnCouponType2_ExposeEvent(btnType2_Coupon7, 6);
                OnCouponType2_ExposeEvent(btnType2_Coupon8, 7);
                OnCouponType2_ExposeEvent(btnType2_Coupon9, 8);
                OnCouponType2_ExposeEvent(btnType2_CouponZ, 9);

                ShowSumSaleCoupon();
                txtTotalAmount.QueueDraw();
                txtTotalCoupon.QueueDraw();
*/                               
            });
        }

        protected void OnHeaderCouponType1ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (bPayCompleted)
                return;

            iSelHeaderCouponType = 1;
            //headerCoupon_Type1.QueueDraw();
            //headerCoupon_Type2.QueueDraw();

            RefreshDrawingArea();
        }

        protected void OnHeaderCouponType2ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (bPayCompleted)
                return;

            iSelHeaderCouponType = 2;
            //headerCoupon_Type1.QueueDraw();
            //headerCoupon_Type2.QueueDraw();

            RefreshDrawingArea();
        }

        protected void OnHeaderCouponType1ExposeEvent(object o, ExposeEventArgs args)
            {
            //Console.WriteLine("==================== On Coupon Header #1 Selected ========================");
            //OnHeaderExposeEvent(o, 1, $"คูปอง 35 บาท (เล่มละ {fCouponPrice_Type1.ToString("#,0")} บ.)");

            //Console.WriteLine("==== iCount = " + iCount);
            //iCount++;

            int iCouponType             = 1;
            string szHeaderText         = $"คูปอง 35 บาท (เล่มละ {fCouponPrice_Type1.ToString("#,0")} บ.)";

            DrawingArea drawArea        = o as DrawingArea;
            //drawArea.ModifyBg(StateType.Normal, clBlue);
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

            layout.SetText(szHeaderText);

            Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
            renderer.Drawable           = drawArea.GdkWindow;
            renderer.Gc                 = drawArea.Style.BlackGC;

            renderer.SetOverrideColor(RenderPart.Foreground, iSelHeaderCouponType == 1 ? clBlack : clGray);
            renderer.SetOverrideColor(RenderPart.Background, iSelHeaderCouponType == 1 ? clHeaderCoupon1_Selected : clHeaderCoupon1_UnSelect);
            drawArea.ModifyBg(StateType.Normal, iSelHeaderCouponType == 1 ? clHeaderCoupon1_Selected : clHeaderCoupon1_UnSelect);
            drawArea.ModifyFg(StateType.Normal, iSelHeaderCouponType == 1 ? clBlack : clGray);

            if (iCouponType == 1)
            {
                //headerCoupon_Type1.ModifyBg(StateType.Normal, iSelHeaderCouponType == 1 ? clHeaderCoupon1_Selected : clHeaderCoupon1_UnSelect);
                drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 4, layout, iSelHeaderCouponType == 1 ? clBlack : clGray, iSelHeaderCouponType == 1 ? clHeaderCoupon1_Selected : clHeaderCoupon1_UnSelect);
            }
            else
            {
               //headerCoupon_Type2.ModifyBg(StateType.Normal, iSelHeaderCouponType == 2 ? clHeaderCoupon2_Selected : clHeaderCoupon2_UnSelect);
                drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 4, layout, iSelHeaderCouponType == 2 ? clWhite : clGray, iSelHeaderCouponType == 2 ? clHeaderCoupon2_Selected : clHeaderCoupon2_UnSelect);
            }
           

            renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
            renderer.SetOverrideColor(RenderPart.Background, Gdk.Color.Zero);
            renderer.Drawable           = null;
            renderer.Gc                 = null;
            renderer.Dispose();
            context.Dispose();
        }

        protected void OnHeaderCouponType2ExposeEvent(object o, ExposeEventArgs args)   
        {
            //Console.WriteLine("==================== On Coupon Header #2 Selected ========================");
            //OnHeaderExposeEvent(o, 2, $"คูปอง 80 บาท (เล่มละ {fCouponPrice_Type2.ToString("#,0")} บ.)");

            int iCouponType     = 2;
            string szHeaderText = $"คูปอง 80 บาท (เล่มละ {fCouponPrice_Type2.ToString("#,0")} บ.)";

            DrawingArea drawArea        = o as DrawingArea;
            //drawArea.ModifyBg(StateType.Normal, clBlue);
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

            layout.SetText(szHeaderText);

            Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
            renderer.Drawable           = drawArea.GdkWindow;
            renderer.Gc                 = drawArea.Style.BlackGC;

            //renderer.SetOverrideColor(RenderPart.Foreground, new Gdk.Color(255, 255, 255));
            if (iCouponType == 1)
            {
                //headerCoupon_Type1.ModifyBg(StateType.Normal, iSelHeaderCouponType == 1 ? clHeaderCoupon1_Selected : clHeaderCoupon1_UnSelect);
                drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 4, layout, iSelHeaderCouponType == 1 ? clBlack : clGray, iSelHeaderCouponType == 1 ? clHeaderCoupon1_Selected : clHeaderCoupon1_UnSelect);
            }
            else
            {
                //headerCoupon_Type2.ModifyBg(StateType.Normal, iSelHeaderCouponType == 2 ? clHeaderCoupon2_Selected : clHeaderCoupon2_UnSelect);
                drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 4, layout, iSelHeaderCouponType == 2 ? clWhite : clGray, iSelHeaderCouponType == 2 ? clHeaderCoupon2_Selected : clHeaderCoupon2_UnSelect);
            }

            //renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
            renderer.Drawable           = null;
            renderer.Gc                 = null;
            renderer.Dispose();
            context.Dispose();
        }

        protected void OnHeaderExposeEvent(object o, int iCouponType, string szHeaderText)
        {
            DrawingArea drawArea        = o as DrawingArea;
            //drawArea.ModifyBg(StateType.Normal, clBlue);
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

            layout.SetText(szHeaderText);

            Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
            renderer.Drawable           = drawArea.GdkWindow;
            renderer.Gc                 = drawArea.Style.BlackGC;

            renderer.SetOverrideColor(RenderPart.Foreground, new Gdk.Color(255, 255, 255));
            if (iCouponType == 1)
            {
                //headerCoupon_Type1.ModifyBg(StateType.Normal, iSelHeaderCouponType == 1 ? clHeaderCoupon1_Selected : clHeaderCoupon1_UnSelect);
                drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 4, layout, iSelHeaderCouponType == 1 ? clBlack : clGray, iSelHeaderCouponType == 1 ? clHeaderCoupon1_Selected : clHeaderCoupon1_UnSelect);
            }
            else
            {
                //headerCoupon_Type2.ModifyBg(StateType.Normal, iSelHeaderCouponType == 2 ? clHeaderCoupon2_Selected : clHeaderCoupon2_UnSelect);
                drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 4, layout, iSelHeaderCouponType == 2 ? clWhite : clGray, iSelHeaderCouponType == 2 ? clHeaderCoupon2_Selected : clHeaderCoupon2_UnSelect);
            }

            renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
            renderer.Drawable           = null;
            renderer.Gc                 = null;
            renderer.Dispose();
            context.Dispose();
        }

        protected void OnBtnType1Coupon1ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType1_ExposeEvent(o, 0);
        }

        protected void OnBtnType1Coupon2ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType1_ExposeEvent(o, 1);
        }

        protected void OnBtnType1Coupon3ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType1_ExposeEvent(o, 2);
        }

        protected void OnBtnType1Coupon4ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType1_ExposeEvent(o, 3);
        }

        protected void OnBtnType1Coupon5ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType1_ExposeEvent(o, 4);
        }

        protected void OnBtnType1Coupon6ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType1_ExposeEvent(o, 5);
        }

        protected void OnBtnType1Coupon7ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType1_ExposeEvent(o, 6);
        }

        protected void OnBtnType1Coupon8ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType1_ExposeEvent(o, 7);
        }

        protected void OnBtnType1Coupon9ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType1_ExposeEvent(o, 8);
        }

        protected void OnBtnType1CouponZExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType1_ExposeEvent(o, 9);
        }

        protected void OnBtnType2Coupon1ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType2_ExposeEvent(o, 0);
        }

        protected void OnBtnType2Coupon2ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType2_ExposeEvent(o, 1);
        }

        protected void OnBtnType2Coupon3ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType2_ExposeEvent(o, 2);
        }

        protected void OnBtnType2Coupon4ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType2_ExposeEvent(o, 3);
        }

        protected void OnBtnType2Coupon5ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType2_ExposeEvent(o, 4);
        }

        protected void OnBtnType2Coupon6ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType2_ExposeEvent(o, 5);
        }

        protected void OnBtnType2Coupon7ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType2_ExposeEvent(o, 6);
        }

        protected void OnBtnType2Coupon8ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType2_ExposeEvent(o, 7);
        }

        protected void OnBtnType2Coupon9ExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType2_ExposeEvent(o, 8);
        }

        protected void OnBtnType2CouponZExposeEvent(object o, ExposeEventArgs args)
        {
            OnCouponType2_ExposeEvent(o, 9);
        }

        protected void OnCouponType1_ExposeEvent(object o, int idx)
        {
            DrawingArea drawArea        = o as DrawingArea;
            //drawArea.ModifyBg(StateType.Normal, clBlue);
            //---  DrawArea ClassType
            int iWidth                  = drawArea.Allocation.Width;
            int iHeight                 = drawArea.Allocation.Height;
            Context context             = drawArea.CreatePangoContext();
            Pango.Layout layout         = new Pango.Layout(context);
            layout.Width                = Units.FromPixels(iWidth);
            layout.Alignment            = Pango.Alignment.Center;
            layout.Justify              = true;
            
            FontDescription fontDesc    = FontDescription.FromString("Ubuntu 16");
            layout.FontDescription      = fontDesc;

            //layout.SetText("\u2713  เงินสด");

            Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
            renderer.Drawable           = drawArea.GdkWindow;
            renderer.Gc                 = drawArea.Style.BlackGC;

            int iKeyNo = idx == 9 ? 0 : idx + 1;
            layout.SetText((iSelCouponList_Type1[idx] == 0 ? "" : "\u2713  ") + (iSelHeaderCouponType == 1 ? "["+iKeyNo+"] " : "") + szCouponNoList_Type1[idx]);
            drawArea.ModifyBg(StateType.Normal, iSelCouponList_Type1[idx] == 0 ? clBkCoupon1_UnSelect : clBkCoupon1_Selected);
            drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 20, layout, iSelCouponList_Type1[idx] == 0 ? clGray : clBlack,
                                        iSelCouponList_Type1[idx] == 0 ? clBkCoupon1_UnSelect : clBkCoupon1_Selected);

/*
            int iTotalCoupon        = 0;
            decimal fTotalAmount    = 0;
            Calc_TotalSalesCoupon(ref iTotalCoupon, ref fTotalAmount);
            txtReference.Text       = (fTotalAmount < 1500) ? "" : $"<span font_size = 'large' fgcolor = '#FFFF00'>รายการมากกว่า 1,500 บาท.</span>";
            txtReference.UseMarkup  = true;
            txtMsg.Text = (fTotalAmount < 1500) ? "" : $"<span font_size = 'large' fgcolor = '#FFFF00'>จ่ายได้เฉพาะ 'เงินสด'</span>";
            txtMsg.UseMarkup        = true;
*/
            //renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
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

            FontDescription fontDesc    = FontDescription.FromString("Ubuntu 16");
            layout.FontDescription      = fontDesc;

            //layout.SetText("\u2713  เงินสด");

            Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
            renderer.Drawable           = drawArea.GdkWindow;
            renderer.Gc                 = drawArea.Style.BlackGC;

            int iKeyNo = idx == 9 ? 0 : idx + 1;
            layout.SetText((iSelCouponList_Type2[idx] == 0 ? "" : "\u2713  ") + (iSelHeaderCouponType == 2 ? "[" + iKeyNo + "] " : "") + szCouponNoList_Type2[idx]);
            drawArea.ModifyBg(StateType.Normal, iSelCouponList_Type2[idx] == 0 ? clBkCoupon2_UnSelect : clBkCoupon2_Selected);
            drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 20, layout, iSelCouponList_Type2[idx] == 0 ? clGray : clWhite,
                                        iSelCouponList_Type2[idx] == 0 ? clBkCoupon2_UnSelect : clBkCoupon2_Selected);

/*
            int iTotalCoupon        = 0;
            decimal fTotalAmount    = 0;
            Calc_TotalSalesCoupon(ref iTotalCoupon, ref fTotalAmount);
            txtReference.Text = (fTotalAmount < 1500) ? "" : $"<span font_size = 'large' fgcolor = '#FFFF00'>รายการขายมากกว่า 1,500 บาท.</span>";
            txtReference.UseMarkup = true;
            txtMsg.Text = (fTotalAmount < 1500) ? "" : $"<span font_size = 'large' fgcolor = '#FFFF00'>จ่ายได้เฉพาะ 'เงินสด'</span>";
            txtMsg.UseMarkup = true;
*/                       
            renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
            renderer.Drawable   = null;
            renderer.Gc         = null;
            //renderer.Dispose();
            //context.Dispose();
        }

        protected void OnBtnType1Coupon1ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (bPayCompleted)
                return;

            iSelHeaderCouponType = 1;
            //OnBtnCoponType1_ButtonPressEvent(btnType1_Coupon1, 0);
            ShowSumSaleCoupon();
        }

        protected void OnBtnType1Coupon2ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (bPayCompleted)
                return;

            iSelHeaderCouponType = 1;
            //OnBtnCoponType1_ButtonPressEvent(btnType1_Coupon2, 1);
            ShowSumSaleCoupon();
        }

        protected void OnBtnType1Coupon3ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (bPayCompleted)
                return;

            iSelHeaderCouponType = 1;
            //OnBtnCoponType1_ButtonPressEvent(btnType1_Coupon3, 2);
            ShowSumSaleCoupon();
        }

        protected void OnBtnType1Coupon4ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (bPayCompleted)
                return;

            iSelHeaderCouponType = 1;
            //OnBtnCoponType1_ButtonPressEvent(btnType1_Coupon4, 3);
            ShowSumSaleCoupon();
        }

        protected void OnBtnType1Coupon5ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (bPayCompleted)
                return;

            iSelHeaderCouponType = 1;
            //OnBtnCoponType1_ButtonPressEvent(btnType1_Coupon5, 4);
            ShowSumSaleCoupon();
        }

        protected void OnBtnType1Coupon6ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (bPayCompleted)
                return;

            iSelHeaderCouponType = 1;
            //OnBtnCoponType1_ButtonPressEvent(btnType1_Coupon6, 5);
            ShowSumSaleCoupon();
        }

        protected void OnBtnType1Coupon7ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (bPayCompleted)
                return;

            iSelHeaderCouponType = 1;
            //OnBtnCoponType1_ButtonPressEvent(btnType1_Coupon7, 6);
            ShowSumSaleCoupon();
        }

        protected void OnBtnType1Coupon8ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (bPayCompleted)
                return;

            iSelHeaderCouponType = 1;
            //OnBtnCoponType1_ButtonPressEvent(btnType1_Coupon8, 7);
            ShowSumSaleCoupon();
        }

        protected void OnBtnType1Coupon9ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (bPayCompleted)
                return;

            iSelHeaderCouponType = 1;
            //OnBtnCoponType1_ButtonPressEvent(btnType1_Coupon9, 8);
            ShowSumSaleCoupon();
        }

        protected void OnBtnType1CouponZButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (bPayCompleted)
                return;

            iSelHeaderCouponType = 1;
            //OnBtnCoponType1_ButtonPressEvent(btnType1_CouponZ, 9);
            ShowSumSaleCoupon();
        }

        protected void OnBtnType2Coupon1ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (bPayCompleted)
                return;

            iSelHeaderCouponType = 2;
            //OnBtnCoponType2_ButtonPressEvent(btnType2_Coupon1, 0);
            ShowSumSaleCoupon();
        }

        protected void OnBtnType2Coupon2ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (bPayCompleted)
                return;

            iSelHeaderCouponType = 2;
            //OnBtnCoponType2_ButtonPressEvent(btnType2_Coupon2, 1);
            ShowSumSaleCoupon();
        }

        protected void OnBtnType2Coupon3ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (bPayCompleted)
                return;

            iSelHeaderCouponType = 2;
            //OnBtnCoponType2_ButtonPressEvent(btnType2_Coupon3, 2);
            ShowSumSaleCoupon();
        }

        protected void OnBtnType2Coupon4ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (bPayCompleted)
                return;

            iSelHeaderCouponType = 2;
            //OnBtnCoponType2_ButtonPressEvent(btnType2_Coupon4, 3);
            ShowSumSaleCoupon();
        }

        protected void OnBtnType2Coupon5ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (bPayCompleted)
                return;

            iSelHeaderCouponType = 2;
            //OnBtnCoponType2_ButtonPressEvent(btnType2_Coupon5, 4);
            ShowSumSaleCoupon();
        }

        protected void OnBtnType2Coupon6ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (bPayCompleted)
                return;

            iSelHeaderCouponType = 2;
            //OnBtnCoponType2_ButtonPressEvent(btnType2_Coupon6, 5);
            ShowSumSaleCoupon();
        }

        protected void OnBtnType2Coupon7ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (bPayCompleted)
                return;

            iSelHeaderCouponType = 2;
            //OnBtnCoponType2_ButtonPressEvent(btnType2_Coupon7, 6);
            ShowSumSaleCoupon();
        }

        protected void OnBtnType2Coupon8ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (bPayCompleted)
                return;

            iSelHeaderCouponType = 2;
            //OnBtnCoponType2_ButtonPressEvent(btnType2_Coupon8, 7);
            ShowSumSaleCoupon();
        }

        protected void OnBtnType2Coupon9ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (bPayCompleted)
                return;

            iSelHeaderCouponType = 2;
            //OnBtnCoponType2_ButtonPressEvent(btnType2_Coupon9, 8);
            ShowSumSaleCoupon();
        }

        protected void OnBtnType2CouponZButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (bPayCompleted)
                return;

            iSelHeaderCouponType = 2;
            //OnBtnCoponType2_ButtonPressEvent(btnType2_CouponZ, 9);
            ShowSumSaleCoupon();
        }

        protected void OnBtnCoponType1_ButtonPressEvent(DrawingArea btn, int idx)
        {
            iSelCouponList_Type1[idx] = iSelCouponList_Type1[idx] == 0 ? 1 : 0;
            btn.QueueDraw();
            RefreshDrawingArea();
        }

        protected void OnBtnCoponType2_ButtonPressEvent(DrawingArea btn, int idx)
        {
            iSelCouponList_Type2[idx] = iSelCouponList_Type2[idx] == 0 ? 1 : 0;
            btn.QueueDraw();
            RefreshDrawingArea();
        }

        //===========================================================================
        protected void OnBtnSelPayCashExposeEvent(object o, ExposeEventArgs args)
        {
            OnPaymentExposeEvent(o, 0, "เงินสด");
        }

        protected void OnBtnSelPayEMVExposeEvent(object o, ExposeEventArgs args)
        {
            OnPaymentExposeEvent(o, 1, "EMV");
        }

        protected void OnPaymentExposeEvent(object o, int iPayType, string szPayTypeName)
        {
            DrawingArea drawArea        = o as DrawingArea;
            //drawArea.ModifyBg(StateType.Normal, clBlue);
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

            if (iPayType == 0)
            {
                layout.SetText((iSelPayType == 0 ? "\u2611  " : "") + szPayTypeName);
                //btnSelPayCash.ModifyBg(StateType.Normal, iSelPayType == 0 ? clBkPaySelect : clBkPayUnSel);
                drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 15, layout, iSelPayType == 0 ? clFgPaySelect : clFgPayUnSel, iSelPayType == 0 ? clBkPaySelect : clBkPayUnSel);
            }
            else
            {
                layout.SetText((iSelPayType == 1 ? "\u2611  " : "") + szPayTypeName);
                //btnSelPayEMV.ModifyBg(StateType.Normal, iSelPayType == 1 ? clBkPaySelect : clBkPayUnSel);
                drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 15, layout, iSelPayType == 1 ? clFgPaySelect : clFgPayUnSel, iSelPayType == 1 ? clBkPaySelect : clBkPayUnSel);
            }

            //renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
            renderer.Drawable   = null;
            renderer.Gc         = null;
            renderer.Dispose();
            context.Dispose();
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

        protected void ShowSumSaleCoupon()
        {
            int iTotalCoupon        = 0;
            decimal fTotalAmount    = 0;
            Calc_TotalSalesCoupon(ref iTotalCoupon, ref fTotalAmount);
            //txtTotalCoupon.Text     = $"<span font_size='large' fgcolor='#22FF22'>{iTotalCoupon} เล่ม</span>";
            //txtTotalAmount.Text     = $"<span font_size='large' fgcolor='#22FF22'>{fTotalAmount.ToString("#,0")} บาท</span>";
            //txtTotalCoupon.UseMarkup = true;
            //txtTotalAmount.UseMarkup = true;
        }

        protected void OnBtnSelPayCashButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (bPayCompleted)
                return;

            iSelPayType = 0;
            //btnSelPayCash.QueueDraw();
            //btnSelPayEMV.QueueDraw();
        }

        protected void OnBtnSelPayEMVButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (bPayCompleted)
                return;

            iSelPayType = 1;
            //btnSelPayCash.QueueDraw();
           //btnSelPayEMV.QueueDraw();
            //----------------------------------
            Perform_ProcessEMV();
        }

        protected void Perform_ProcessEMV()
        {
            int iTotalCoupon        = 0;
            decimal fTotalAmount    = 0;
            Calc_TotalSalesCoupon(ref iTotalCoupon, ref fTotalAmount);
            if (fTotalAmount == 0)
            {
                iSelPayType = 0;
                //btnSelPayCash.QueueDraw();
                //btnSelPayEMV.QueueDraw();
                return;
            }

            if (fTotalAmount >= 1500)
            {
                iSelPayType = 0;
                //btnSelPayCash.QueueDraw();
                //btnSelPayEMV.QueueDraw();
                return;
            }
            //-------------------------------------------------------------------
            var frmEdcPaymentDlg    = new EdcPaymentDlg(fTotalAmount);
            Gdk.Color colBk         = new Gdk.Color(0x3E, 0x3E, 0x3E);
            frmEdcPaymentDlg.ModifyBg(StateType.Normal, colBk);
            Console.WriteLine("+ Before Show dialog EMV");
            ResponseType xResult    = (ResponseType) frmEdcPaymentDlg.Run();

            if (xResult == ResponseType.Ok)
            {
                Console.WriteLine("+ Show Response EMV");
                var xCreditCardInfo         = frmEdcPaymentDlg.xOutCreditCard;

                //txtCreditCardNo.Text        = $"<span fgcolor='#00FF00'>{"CreditCard No : " + xCreditCardInfo.szCardNo}</span>";
                //txtCreditCardNo.UseMarkup   = true;

                //txtReference.Text           = $"<span fgcolor='#00FF00'>{"Reference : " + xCreditCardInfo.szRef3}</span>";
                //txtReference.UseMarkup      = true;

                //txtMsg.Text                 = $"<span fgcolor='#00FF00'>{"Approve Code : " + xCreditCardInfo.szApprovalCode}</span>";
                //txtMsg.UseMarkup            = true;
                  
                //Console.WriteLine("+ After show Response");
                frmEdcPaymentDlg.Dispose();
                //Console.WriteLine("+ After Dispose");
                //--- Disable active button
                //panelCoupon_Type1.Sensitive = panelCoupon_Type2.Sensitive = false;
                //btnCancel.Sensitive = false;
                bPayCompleted = true;
                this.Activate();
            }
            else
            {
                //--- Back to Pay Cash
                iSelPayType = 0;
                //btnSelPayCash.QueueDraw();
                //btnSelPayEMV.QueueDraw();
            }
        }

        protected void OnBtnConfirmPaymentClicked(object sender, EventArgs e)
        {
            int iTotalCoupon        = 0;
            decimal fTotalAmount    = 0;
            Calc_TotalSalesCoupon(ref iTotalCoupon, ref fTotalAmount);
            if (iTotalCoupon == 0)
            {
                //txtMsg.Text         = $"<span font_size = 'large' fgcolor = '#FFFF00'>ไม่พบการเลือกคูปอง</span>";
                //txtMsg.UseMarkup    = true;
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
            TCT_Lib.szCurSalesCouponReceiptNo = TCT_Lib.LaneDB.GetRunningReceiptNo(PrintReceiptType.SalesCoupon);   // "CP-DD01-2020000218";
            TCT_Lib.LaneDB.WriteSalesCouponTransaction(iTotalCoupon_Type1, fCouponPrice_Type1, szSelCouponType1, "35", iTotalCoupon_Type2, fCouponPrice_Type2, szSelCouponType2, "80", iSelPayType == 0 ? (int)PayType.CASH : (int)PayType.EMV);
            //---------------------------------------------
            this.Respond(ResponseType.Ok);
            TCT_Lib.eCurAppPage = AppPage.Main;
            Destroy();
            Dispose();
        }

        protected void OnBtnPrintReceiptClicked(object sender, EventArgs e)
        {
            int iTotalCoupon        = 0;
            decimal fTotalAmount    = 0;
            Calc_TotalSalesCoupon(ref iTotalCoupon, ref fTotalAmount);
            if (iTotalCoupon == 0)
            {
                //txtMsg.Text         = $"<span font_size = 'large' fgcolor = '#FFFF00'>ไม่พบการเลือกคูปอง</span>";
                //txtMsg.UseMarkup    = true;
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
            TCT_Lib.szCurSalesCouponReceiptNo = TCT_Lib.LaneDB.GetRunningReceiptNo(PrintReceiptType.SalesCoupon);   // "CP-DD01-2020000218";
            TCT_Lib.LaneDB.WriteSalesCouponTransaction(iTotalCoupon_Type1, fCouponPrice_Type1, szSelCouponType1, "35", iTotalCoupon_Type2, fCouponPrice_Type2, szSelCouponType2, "80", iSelPayType == 0 ? (int)PayType.CASH : (int)PayType.EMV);
            //--- Process Print Receipt SalesCopon
            PrintSalesCouponLib.PrintSalesCoupon("35", iTotalCoupon_Type1, fCouponPrice_Type1, "80", iTotalCoupon_Type2, fCouponPrice_Type2, iSelPayType == 0 ? "CASH" : "EMV");
            //----------------------------------------------
            this.Respond(ResponseType.Ok);
            TCT_Lib.eCurAppPage = AppPage.Main;
            Destroy();
            Dispose();
        }
    }
}
