using System;
using System.Threading;
using Gtk;
using Pango;
using TCT_App.EMV.EMV_V1;
using TCT_App.EMV.V0;

namespace TCT_App.LaneView.MiscDlg
{
    public partial class PrintConvoyPaymentDlg : Gtk.Dialog
    {
        Gdk.Pixbuf imgVehicleType1, imgVehicleType2;

        Gdk.Color clBlue        = new Gdk.Color(10, 110, 170);
        Gdk.Color clYellow      = new Gdk.Color(255, 255, 0);
        Gdk.Color clBtnUnSel    = new Gdk.Color(50, 50,50);
        Gdk.Color clBtnSelect   = new Gdk.Color(30, 130, 30);

        int iSelPayType = 0;    // 0=Cash, 1=EMV, 2=QR Code
        int iFocus      = 0;    // 0=Class 1, 1=Class 2
        bool bPayCompleted      = false;
        int iCurMaxReceiptID    = 0;

        public int iOutTotalVehicle = 0, iOutTotalClass1 = 0, iOutTotalClass2 = 0;
        public int iOutPayType = 0, iOutEmvDetailID = 0;        // PayType : 0=Cash, 1=EMV, 2=QRCode
        //------------------------------------------------------------------------
        public PrintConvoyPaymentDlg()
        {
            TCT_Lib.bActiveConvoyDlg = true;

            this.Build();
            this.Decorated = false;     // v2.0.2: no title bar on popups

            this.Title      = "Print Payment Receipt Convoy";
            imgVehicleType1 = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.VehicleType1.png");
            imgVehicleType2 = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.VehicleType2.png");

            TCT_Lib.eCurAppPage = AppPage.PayConvoy;
            iFocus              = 0;
            iCurMaxReceiptID    = TCT_Lib.LaneDB.GetMaxRunningReceiptNo();

            InitUI();
            //----------------------------------------------
            edtTotalClass1.Text = edtTotalClass2.Text = "";
            txtCreditCardNo.Text = txtReference.Text = txtMsg.Text = "";
            string szZeroAmount     = "0 บาท";
            txtTotalAmount1.Text    = $"<span font_size = 'large' fgcolor = '#FFFF00' > = {szZeroAmount}</span>";
            txtTotalAmount2.Text    = $"<span font_size = 'large' fgcolor = '#FFFF00' > = {szZeroAmount}</span>";
            txtTotalAmount.Text     = $"<span font_size = 'large' fgcolor = '#00FF00' >{szZeroAmount}</span>";

            txtTotalAmount1.UseMarkup = true;
            txtTotalAmount1.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
            txtTotalAmount2.UseMarkup = true;
            txtTotalAmount2.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
            txtTotalAmount.UseMarkup = true;
            txtTotalAmount.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));


            lblType1.Text = $"<span font_size='large' fgcolor='#ffffff'>รถประเภท 1 :  {TCT_Lib.fFareClass1.ToString("#,0")}  บาท x จำนวน :</span>";
            lblType2.Text = $"<span font_size='large' fgcolor='#ffffff'>รถประเภท 2 :  {TCT_Lib.fFareClass2.ToString("#,0")}  บาท x จำนวน :</span>";
            lblType1.UseMarkup = true;
            lblType2.UseMarkup = true;
            lblType1.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
            lblType2.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

            btnSelPayQrCode.Visible = TCT_Lib.iEdcVersionType == 0;

            //--------------------------------------------------------------------------------------
            TCT_Lib.laneController.manageProcessPayConvoy += LaneController_ManageProcessPayConvoy;

            this.Activate();
        }

        private void InitUI()
        {
            imgClassType1.Pixbuf = imgVehicleType1.ScaleSimple(100, 70, Gdk.InterpType.Bilinear);
            imgClassType2.Pixbuf = imgVehicleType2.ScaleSimple(100, 80, Gdk.InterpType.Bilinear);

            txtTotalVehicle.ModifyFont(FontDescription.FromString("Ubuntu Bold 16"));
            txtTotalVehicle.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

            txtTotalAmount.ModifyFont(FontDescription.FromString("Ubuntu Bold 16"));
            txtTotalAmount.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

            lblTotalVehicle.ModifyFont(FontDescription.FromString("Ubuntu Bold 16"));
            lblTotalVehicle.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
            lblTotalVehicle.Justify = Justification.Left;
            lblTotalAmount.ModifyFont(FontDescription.FromString("Ubuntu Bold 16"));
            lblTotalAmount.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
            lblTotalAmount.Justify = Justification.Right;

            areaHeader.ModifyBg(StateType.Normal, clBlue);
            areaHeader.QueueDraw();

            btnSelPayCash.ModifyBg(StateType.Normal, clBtnUnSel);
            btnSelPayCash.QueueDraw();

            btnSelPayEMV.ModifyBg(StateType.Normal, clBtnUnSel);
            btnSelPayEMV.QueueDraw();

            btnSelPayQrCode.ModifyBg(StateType.Normal, clBtnUnSel);
            btnSelPayQrCode.QueueDraw();

            btnKey_1.Child.ModifyFont(FontDescription.FromString("Arial 14"));
            btnKey_2.Child.ModifyFont(FontDescription.FromString("Arial 14"));
            btnKey_3.Child.ModifyFont(FontDescription.FromString("Arial 14"));
            btnKey_4.Child.ModifyFont(FontDescription.FromString("Arial 14"));
            btnKey_5.Child.ModifyFont(FontDescription.FromString("Arial 14"));
            btnKey_6.Child.ModifyFont(FontDescription.FromString("Arial 14"));
            btnKey_7.Child.ModifyFont(FontDescription.FromString("Arial 14"));
            btnKey_8.Child.ModifyFont(FontDescription.FromString("Arial 14"));
            btnKey_9.Child.ModifyFont(FontDescription.FromString("Arial 14"));
            btnKey_Zero.Child.ModifyFont(FontDescription.FromString("Arial 14"));
            btn_BackSpace.Child.ModifyFont(FontDescription.FromString("Arial 14"));
            btn_Enter.Child.ModifyFont(FontDescription.FromString("Arial 14"));
            btnPrintReceipt.Child.ModifyFont(FontDescription.FromString("Arial 14"));
            edtTotalClass1.ModifyFont(FontDescription.FromString("Arial 16"));
            edtTotalClass2.ModifyFont(FontDescription.FromString("Arial 16"));
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

            layout.SetText("จ่ายเงินรถขบวน");

            Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
            renderer.Drawable           = drawArea.GdkWindow;
            renderer.Gc                 = drawArea.Style.BlackGC;

            renderer.SetOverrideColor(RenderPart.Foreground, new Gdk.Color(255, 255, 255));
            //renderer.DrawLayout(layout, 0, 0);
            drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 4, layout, new Gdk.Color(255, 255, 255), clBlue);

            //renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
            renderer.Drawable           = null;
            renderer.Gc                 = null;
            renderer.Dispose();
            context.Dispose();
        }

        protected void OnButtonCancelClicked(object sender, EventArgs e)
        {
            Action_BtnCancel();
        }

        protected void Action_BtnCancel()
        {
            LogInfo.WriteLog("+ Close Convoy from Cancel (Cancel Convoy)");
            LogInfo.WriteLaneEvent("+ Close Convoy from Cancel (Cancel Convoy)");
            TCT_Lib.LaneDB.SetMaxRunningReceiptNo(iCurMaxReceiptID);
            this.Respond(ResponseType.Cancel);
            TCT_Lib.laneController.manageProcessPayConvoy -= LaneController_ManageProcessPayConvoy;
            TCT_Lib.eCurAppPage         = AppPage.Main;
            TCT_Lib.bActiveConvoyDlg    = false;
            Dispose();
            Destroy();
        }

        void LaneController_ManageProcessPayConvoy(int iKey)
        {
            Application.Invoke(delegate
            {
                if ((iKey >= (int)KeyPressedType.NUM_0) && (iKey <= (int)KeyPressedType.NUM_9))
                {
                    if (iFocus == 0)
                        edtTotalClass1.Text += (iKey - (int)KeyPressedType.NUM_0).ToString();
                    else
                        edtTotalClass2.Text += (iKey - (int)KeyPressedType.NUM_0).ToString();
                }
                else if (iKey == (int)KeyPressedType.ENTER)
                {
                    if (iFocus == 0)
                    {
                        iFocus = 1;
                        edtTotalClass2.GrabFocus();
                    }
                    else
                        btnPrintReceipt.GrabFocus();
                }
                else if (iKey == (int)KeyPressedType.BACKSPACE)
                {
                    if (iFocus == 0)
                    {
                        if (edtTotalClass1.Text == "")
                            return;
                        edtTotalClass1.Text = edtTotalClass1.Text.Remove(edtTotalClass1.Text.Length - 1);
                    }
                    else
                    {
                        if (edtTotalClass2.Text == "")
                            return;
                        edtTotalClass2.Text = edtTotalClass2.Text.Remove(edtTotalClass2.Text.Length - 1);
                    } 
                }
                else if (iKey == (int)KeyPressedType.PAY_CASH)
                {
                    Action_BtnSelPayment(0);
                }
                else if (iKey == (int)KeyPressedType.PAY_EMV)
                {
                    Action_BtnSelPayment(1);
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
                else if (iKey == (int)KeyPressedType.ESC)
                {
                    Action_BtnCancel();
                }
            });
        }

        protected void OnEdtTotalClass1FocusInEvent(object o, FocusInEventArgs args)
        {
            iFocus = 0;
        }

        protected void OnEdtTotalClass2FocusInEvent(object o, FocusInEventArgs args)
        {
            iFocus = 1;
        }

        protected void OnBtnKeyZeroClicked(object sender, EventArgs e)
        {
            Action_Key("0");
        }

        protected void OnBtnKey1Clicked(object sender, EventArgs e)
        {
            Action_Key("1");
        }

        protected void OnBtnKey2Clicked(object sender, EventArgs e)
        {
            Action_Key("2");
        }

        protected void OnBtnKey3Clicked(object sender, EventArgs e)
        {
            Action_Key("3");
        }

        protected void OnBtnKey4Clicked(object sender, EventArgs e)
        {
            Action_Key("4");
        }

        protected void OnBtnKey5Clicked(object sender, EventArgs e)
        {
            Action_Key("5");
        }

        protected void OnBtnKey6Clicked(object sender, EventArgs e)
        {
            Action_Key("6");
        }

        protected void OnBtnKey7Clicked(object sender, EventArgs e)
        {
            Action_Key("7");
        }

        protected void OnBtnKey8Clicked(object sender, EventArgs e)
        {
            Action_Key("8");
        }

        protected void OnBtnKey9Clicked(object sender, EventArgs e)
        {
            Action_Key("9");
        }

        protected void Action_Key(String szChar)
        {
            if (iFocus == 0)
                edtTotalClass1.Text += szChar;
            else
                edtTotalClass2.Text += szChar;
        }

        protected void OnBtnBackSpaceClicked(object sender, EventArgs e)
        {
            if (iFocus == 0)
            {
                if (edtTotalClass1.Text == "")
                    return;
                edtTotalClass1.Text = edtTotalClass1.Text.Remove(edtTotalClass1.Text.Length - 1);
            }
            else
            {
                if (edtTotalClass2.Text == "")
                    return;
                edtTotalClass2.Text = edtTotalClass2.Text.Remove(edtTotalClass2.Text.Length - 1);
            }
        }

        protected void OnBtnEnterClicked(object sender, EventArgs e)
        {
            if (iFocus == 0)
                edtTotalClass2.GrabFocus();
            else if (iFocus == 1)
                btnPrintReceipt.GrabFocus();
        }

        protected void OnKeyPressEvent(object o, KeyPressEventArgs args)
        {
            if (TCT_Lib.keyMapping.ContainsKey((int)args.Event.KeyValue))
                TCT_Lib.webSocketLane.SendVirtualKeyboard(TCT_Lib.keyMapping[(int)args.Event.KeyValue], 1);
        }

        protected void OnEdtTotalClass1KeyPressEvent(object o, KeyPressEventArgs args)
        {
            if (TCT_Lib.keyMapping.ContainsKey((int)args.Event.KeyValue))
                TCT_Lib.webSocketLane.SendVirtualKeyboard(TCT_Lib.keyMapping[(int)args.Event.KeyValue]);
        }

        protected void OnEdtTotalClass2KeyPressEvent(object o, KeyPressEventArgs args)
        {
            if (TCT_Lib.keyMapping.ContainsKey((int)args.Event.KeyValue))
                TCT_Lib.webSocketLane.SendVirtualKeyboard(TCT_Lib.keyMapping[(int)args.Event.KeyValue]);
        }

        protected void OnEdtTotalClass1Activated(object sender, EventArgs e)
        {
            TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.ENTER);
        }

        protected void OnEdtTotalClass2Activated(object sender, EventArgs e)
        {
            TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.ENTER);
        }

        protected void OnBtnSelPayCashButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            Action_BtnSelPayment(0);
        }

        protected void OnBtnSelPayEMVButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            Action_BtnSelPayment(1);
            Perform_ProcessEMV();
        }

        protected void OnBtnSelPayQrCodeButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            Action_BtnSelPayment(2);
            Perform_ProcessQrCode();
        }

        protected void Action_BtnSelPayment(int iPayType)
        {
            if (bPayCompleted)
                return;

            iSelPayType = iPayType;
            btnSelPayCash.QueueDraw();
            btnSelPayEMV.QueueDraw();
            btnSelPayQrCode.QueueDraw();
        }

        protected void OnBtnSelPayCashExposeEvent(object o, ExposeEventArgs args)
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

            //layout.SetText("\u2713  เงินสด");

            Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
            renderer.Drawable           = drawArea.GdkWindow;
            renderer.Gc                 = drawArea.Style.BlackGC;

            if (iSelPayType == 0)
            {
                layout.SetText("\u2611  เงินสด");
                btnSelPayCash.ModifyBg(StateType.Normal, clBtnSelect);
                renderer.SetOverrideColor(RenderPart.Foreground, new Gdk.Color(255, 255, 255));
                drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 15, layout, new Gdk.Color(255, 255, 255), clBtnSelect);
            }
            else
            {
                layout.SetText("เงินสด");
                btnSelPayCash.ModifyBg(StateType.Normal, clBtnUnSel);
                renderer.SetOverrideColor(RenderPart.Foreground, new Gdk.Color(150, 150, 150));
                drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 15, layout, new Gdk.Color(150, 150, 150), clBtnUnSel);
            }

            //renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
            renderer.Drawable           = null;
            renderer.Gc                 = null;
            renderer.Dispose();
            context.Dispose();
        }

        protected void OnBtnSelPayEMVExposeEvent(object o, ExposeEventArgs args)
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

            //layout.SetText("EMV");

            Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
            renderer.Drawable           = drawArea.GdkWindow;
            renderer.Gc                 = drawArea.Style.BlackGC;

            if (iSelPayType == 1)
            {
                layout.SetText("\u2611  EMV");
                btnSelPayEMV.ModifyBg(StateType.Normal, clBtnSelect);
                renderer.SetOverrideColor(RenderPart.Foreground, new Gdk.Color(255, 255, 255));
                drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 20, layout, new Gdk.Color(255, 255, 255), clBtnSelect);
            }
            else
            {
                layout.SetText("EMV");
                btnSelPayEMV.ModifyBg(StateType.Normal, clBtnUnSel);
                renderer.SetOverrideColor(RenderPart.Foreground, new Gdk.Color(150, 150, 150));
                drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 20, layout, new Gdk.Color(150, 150, 150), clBtnUnSel);
            }

            //renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
            renderer.Drawable           = null;
            renderer.Gc                 = null;
            renderer.Dispose();
            context.Dispose();
        }

        protected void OnBtnSelPayQrCodeExposeEvent(object o, ExposeEventArgs args)
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

            //layout.SetText("EMV");

            Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
            renderer.Drawable           = drawArea.GdkWindow;
            renderer.Gc                 = drawArea.Style.BlackGC;

            if (iSelPayType == 2)
            {
                layout.SetText("\u2611  QR Code");
                btnSelPayQrCode.ModifyBg(StateType.Normal, clBtnSelect);
                renderer.SetOverrideColor(RenderPart.Foreground, new Gdk.Color(255, 255, 255));
                drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 20, layout, new Gdk.Color(255, 255, 255), clBtnSelect);
            }
            else
            {
                layout.SetText("QR Code");
                btnSelPayQrCode.ModifyBg(StateType.Normal, clBtnUnSel);
                renderer.SetOverrideColor(RenderPart.Foreground, new Gdk.Color(150, 150, 150));
                drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 20, layout, new Gdk.Color(150, 150, 150), clBtnUnSel);
            }

            //renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
            renderer.Drawable           = null;
            renderer.Gc                 = null;
            renderer.Dispose();
            context.Dispose();
        }

        protected void OnEdtTotalClass1Changed(object sender, EventArgs e)
        {
           if (edtTotalClass1.Text.Contains("n"))
            {
                Action_BtnCancel();
                return;
            }
            if (edtTotalClass1.Text.Contains("p"))
            {
                Action_BtnCancel();
                return;
            }

            UpdateTotalAmount();
            edtTotalClass1.QueueDraw();
        }

        protected void OnEdtTotalClass2Changed(object sender, EventArgs e)
        {
            if (edtTotalClass2.Text.Contains("n"))
            {
                Action_BtnCancel();
                return;
            }
            if (edtTotalClass2.Text.Contains("p"))
            {
                Action_BtnCancel();
                return;
            }

            UpdateTotalAmount();
            edtTotalClass2.QueueDraw();
        }

        private void UpdateTotalAmount()
        {
            string szZeroAmount = "0 บาท";
            decimal fTotalAmount1 = 0, fTotalAmount2 = 0;
            if (edtTotalClass1.Text.Trim() == "")
            {
                txtTotalAmount1.Text = $"<span font_size = 'large' fgcolor = '#FFFF00' > = {szZeroAmount}</span>";
            }
            else
            {
                int iTotalClass = 0;
                if (int.TryParse(edtTotalClass1.Text.Trim(), out iTotalClass))
                    fTotalAmount1 = int.Parse(edtTotalClass1.Text.Trim()) * TCT_Lib.fFareClass1;
                else
                    fTotalAmount1 = 0;

                txtTotalAmount1.Text = $"<span font_size = 'large' fgcolor = '#FFFF00' > = {fTotalAmount1.ToString("#,0")} บาท</span>";
            }

            if (edtTotalClass2.Text.Trim() == "")
            {
                txtTotalAmount2.Text = $"<span font_size = 'large' fgcolor = '#FFFF00' > = {szZeroAmount}</span>";
            }
            else
            {
                int iTotalClass = 0;
                if (int.TryParse(edtTotalClass2.Text.Trim(), out iTotalClass))
                    fTotalAmount2 = int.Parse(edtTotalClass2.Text.Trim()) * TCT_Lib.fFareClass2;
                else
                    fTotalAmount2 = 0;

                txtTotalAmount2.Text = $"<span font_size = 'large' fgcolor = '#FFFF00' > = {fTotalAmount2.ToString("#,0")} บาท</span>";
            }

            int iTotalClass1 = 0, iTotalClass2 = 0;
            if (int.TryParse(edtTotalClass1.Text.Trim(), out iTotalClass1))
                fTotalAmount1 = int.Parse(edtTotalClass1.Text.Trim()) * TCT_Lib.fFareClass1;
            else
                fTotalAmount1 = 0;
            if (int.TryParse(edtTotalClass2.Text.Trim(), out iTotalClass2))
                fTotalAmount2 = int.Parse(edtTotalClass2.Text.Trim()) * TCT_Lib.fFareClass2;
            else
                fTotalAmount2 = 0;

            decimal fTotalAmount    = fTotalAmount1 + fTotalAmount2;
            int iTotalVehicle       = iTotalClass1 + iTotalClass2;
            txtTotalAmount.Text     = $"<span font_size = 'large' fgcolor = '#00FF00' >{fTotalAmount.ToString("#,0")} บาท</span>";
            txtTotalVehicle.Text    = $"<span font_size = 'large' fgcolor = '#00FF00' >{iTotalVehicle.ToString("0")} คัน</span>";

            txtReference.Text       = (fTotalAmount < 1500) ? "" : $"<span font_size = 'large' fgcolor = '#FFFF00'>รายการขายมากกว่า 1,500 บาท.</span>";
            txtReference.UseMarkup  = true;
            txtMsg.Text             = (fTotalAmount < 1500) ? "" : $"<span font_size = 'large' fgcolor = '#FFFF00'>จ่ายได้เฉพาะ 'เงินสด'</span>";
            txtMsg.UseMarkup        = true;

            txtTotalAmount1.UseMarkup   = true;
            txtTotalAmount1.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
            txtTotalAmount2.UseMarkup   = true;
            txtTotalAmount2.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
            txtTotalAmount.UseMarkup    = true;
            txtTotalAmount.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
            txtTotalVehicle.UseMarkup   = true;
            txtTotalVehicle.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
        }

        protected decimal Calc_TotalAmount()
        {
            decimal fTotalAmount1 = 0, fTotalAmount2 = 0;
            int iTotalClass1 = 0, iTotalClass2 = 0;
            if (int.TryParse(edtTotalClass1.Text.Trim(), out iTotalClass1))
                fTotalAmount1 = int.Parse(edtTotalClass1.Text.Trim()) * TCT_Lib.fFareClass1;
            else
                fTotalAmount1 = 0;
            if (int.TryParse(edtTotalClass2.Text.Trim(), out iTotalClass2))
                fTotalAmount2 = int.Parse(edtTotalClass2.Text.Trim()) * TCT_Lib.fFareClass2;
            else
                fTotalAmount2 = 0;

            decimal fTotalAmount    = fTotalAmount1 + fTotalAmount2;
            int iTotalVehicle       = iTotalClass1 + iTotalClass2;

            return fTotalAmount;
        }

        protected int GetTotalVehicle(ref int iTotalClass1, ref int iTotalClass2)
        {
            iTotalClass1 = iTotalClass2 = 0;
            if (!int.TryParse(edtTotalClass1.Text.Trim(), out iTotalClass1))
                iTotalClass1 = 0;
            if (!int.TryParse(edtTotalClass2.Text.Trim(), out iTotalClass2))
                iTotalClass2 = 0;

            return iTotalClass1 + iTotalClass2;
        }

        protected void Perform_ProcessEMV()
        {
            if (TCT_Lib.bActiveEMVDlg)
                return;

            //---------------------------------------
            decimal fTotalAmount = Calc_TotalAmount();
            if (fTotalAmount == 0)
            {
                iSelPayType = 0;
                btnSelPayCash.QueueDraw();
                btnSelPayEMV.QueueDraw();
                btnSelPayQrCode.QueueDraw();

                txtMsg.Text         = $"<span font_size = 'large' fgcolor = '#FFFF00'>ไม่พบจำนวนรถขบวน</span>";
                txtMsg.UseMarkup    = true;

                return;
            }

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
                md.Decorated = false;     // v2.0.2: no title bar
                md.Run();
                md.Destroy();
                return;
            }
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
                    var xCreditCardInfo         = frmEdcPaymentDlg.xOutCreditCard;
                    LogInfo.WriteLog("+ Pay CreditCard Convoy : " + xCreditCardInfo.szCardNo + ", Ref3 : " + xCreditCardInfo.szRef3);
                    LogInfo.WriteLaneEvent("+ Pay CreditCard Convoy : " + xCreditCardInfo.szCardNo + ", Ref3 : " + xCreditCardInfo.szRef3);

                    iOutEmvDetailID             = (int)TCT_Lib.LaneDB.InsertEmvDetailData(0, xCreditCardInfo);

                    txtCreditCardNo.Text        = $"<span fgcolor='#00FF00'>{"CreditCard No : " + xCreditCardInfo.szCardNo}</span>";
                    txtCreditCardNo.UseMarkup   = true;

                    txtReference.Text           = $"<span fgcolor='#00FF00'>{"Reference : " + xCreditCardInfo.szRef3}</span>";
                    txtReference.UseMarkup      = true;

                    txtMsg.Text                 = $"<span fgcolor='#00FF00'>{"Approve Code : " + xCreditCardInfo.szApprovalCode}</span>";
                    txtMsg.UseMarkup            = true;
                      
                    //Console.WriteLine("+ After show Response");
                    frmEdcPaymentDlg.Dispose();
                    //Console.WriteLine("+ After Dispose");
                    //--- Disable active button
                    edtTotalClass1.Sensitive = edtTotalClass2.Sensitive = false;
                    btnCancel.Sensitive     = false;
                    frameKeyNum.Sensitive   = false;
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
                Console.WriteLine("+ Before Show dialog EMV_V1");
                ResponseType xResult    = (ResponseType) frmEdcPaymentDlg.Run();

                if (xResult == ResponseType.Ok)
                {
                    Console.WriteLine("+ Show Response EMV_V1");
                    var xCreditCardInfo         = frmEdcPaymentDlg.xOutCreditCard;
                    LogInfo.WriteLog("+ Pay CreditCard Convoy : " + xCreditCardInfo.szCardNo + ", Ref3 : " + xCreditCardInfo.szRef3);
                    LogInfo.WriteLaneEvent("+ Pay CreditCard Convoy : " + xCreditCardInfo.szCardNo + ", Ref3 : " + xCreditCardInfo.szRef3);

                    iOutEmvDetailID             = (int)TCT_Lib.LaneDB.InsertEmvDetailData(0, xCreditCardInfo);

                    txtCreditCardNo.Text        = $"<span fgcolor='#00FF00'>{"CreditCard No : " + xCreditCardInfo.szCardNo}</span>";
                    txtCreditCardNo.UseMarkup   = true;

                    txtReference.Text           = $"<span fgcolor='#00FF00'>{"Reference : " + xCreditCardInfo.szRef3}</span>";
                    txtReference.UseMarkup      = true;

                    txtMsg.Text                 = $"<span fgcolor='#00FF00'>{"Approve Code : " + xCreditCardInfo.szApprovalCode}</span>";
                    txtMsg.UseMarkup            = true;
                      
                    //Console.WriteLine("+ After show Response");
                    frmEdcPaymentDlg.Dispose();
                    //Console.WriteLine("+ After Dispose");
                    //--- Disable active button
                    edtTotalClass1.Sensitive = edtTotalClass2.Sensitive = false;
                    btnCancel.Sensitive     = false;
                    frameKeyNum.Sensitive   = false;
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
                       
            TCT_Lib.eCurAppPage   = AppPage.PayConvoy;     // v2.0.2: give keys back to this popup
            TCT_Lib.bActiveEMVDlg = false;
        }

        protected void Perform_ProcessQrCode()
        {
            if (TCT_Lib.bActiveEMVDlg)
                return;

            //---------------------------------------
            decimal fTotalAmount = Calc_TotalAmount();
            if (fTotalAmount == 0)
            {
                iSelPayType = 0;
                btnSelPayCash.QueueDraw();
                btnSelPayEMV.QueueDraw();
                btnSelPayQrCode.QueueDraw();

                txtMsg.Text         = $"<span font_size = 'large' fgcolor = '#FFFF00'>ไม่พบจำนวนรถขบวน</span>";
                txtMsg.UseMarkup    = true;

                return;
            }

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
                md.Decorated = false;     // v2.0.2: no title bar
                md.Run();
                md.Destroy();
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
            //-------------------------------------------------------------------
            if (TCT_Lib.iEdcVersionType == 0)
            {
                var frmEdcPaymentDlg    = new EdcPayQrDlg(fTotalAmount, false);
                Gdk.Color colBk         = new Gdk.Color(0x3E, 0x3E, 0x3E);
                frmEdcPaymentDlg.ModifyBg(StateType.Normal, colBk);
                Console.WriteLine("+ Before Show dialog QrCode");
                ResponseType xResult    = (ResponseType) frmEdcPaymentDlg.Run();

                if (xResult == ResponseType.Ok)
                {
                    Console.WriteLine("+ Show Response QrCode");
                    var xCreditCardInfo         = frmEdcPaymentDlg.xOutCreditCard;
                    LogInfo.WriteLog("+ Pay QR Convoy : " + xCreditCardInfo.szCardNo + ", Ref3 : " + xCreditCardInfo.szRef3);
                    LogInfo.WriteLaneEvent("+ Pay QR Convoy : " + xCreditCardInfo.szCardNo + ", Ref3 : " + xCreditCardInfo.szRef3);

                    iOutEmvDetailID             = (int)TCT_Lib.LaneDB.InsertEmvDetailData(1, xCreditCardInfo);

                    txtCreditCardNo.Text        = $"<span fgcolor='#00FF00'>{"CreditCard No : " + xCreditCardInfo.szCardNo}</span>";
                    txtCreditCardNo.UseMarkup   = true;

                    txtReference.Text           = $"<span fgcolor='#00FF00'>{"Reference : " + xCreditCardInfo.szRef3}</span>";
                    txtReference.UseMarkup      = true;

                    txtMsg.Text                 = $"<span fgcolor='#00FF00'>{"Approve Code : " + xCreditCardInfo.szApprovalCode}</span>";
                    txtMsg.UseMarkup            = true;
                      
                    //Console.WriteLine("+ After show Response");
                    frmEdcPaymentDlg.Dispose();
                    //Console.WriteLine("+ After Dispose");
                    //--- Disable active button
                    edtTotalClass1.Sensitive = edtTotalClass2.Sensitive = false;
                    btnCancel.Sensitive     = false;
                    frameKeyNum.Sensitive   = false;
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
                Console.WriteLine("+ Before Show dialog QrCode");
                ResponseType xResult    = (ResponseType) frmEdcPaymentDlg.Run();

                if (xResult == ResponseType.Ok)
                {
                    Console.WriteLine("+ Show Response QrCode");
                    var xCreditCardInfo         = frmEdcPaymentDlg.xOutCreditCard;
                    LogInfo.WriteLog("+ Pay QR Convoy : " + xCreditCardInfo.szCardNo + ", Ref3 : " + xCreditCardInfo.szRef3);
                    LogInfo.WriteLaneEvent("+ Pay QR Convoy : " + xCreditCardInfo.szCardNo + ", Ref3 : " + xCreditCardInfo.szRef3);

                    iOutEmvDetailID             = (int)TCT_Lib.LaneDB.InsertEmvDetailData(1, xCreditCardInfo);

                    txtCreditCardNo.Text        = $"<span fgcolor='#00FF00'>{"CreditCard No : " + xCreditCardInfo.szCardNo}</span>";
                    txtCreditCardNo.UseMarkup   = true;

                    txtReference.Text           = $"<span fgcolor='#00FF00'>{"Reference : " + xCreditCardInfo.szRef3}</span>";
                    txtReference.UseMarkup      = true;

                    txtMsg.Text                 = $"<span fgcolor='#00FF00'>{"Approve Code : " + xCreditCardInfo.szApprovalCode}</span>";
                    txtMsg.UseMarkup            = true;
                      
                    //Console.WriteLine("+ After show Response");
                    frmEdcPaymentDlg.Dispose();
                    //Console.WriteLine("+ After Dispose");
                    //--- Disable active button
                    edtTotalClass1.Sensitive = edtTotalClass2.Sensitive = false;
                    btnCancel.Sensitive     = false;
                    frameKeyNum.Sensitive   = false;
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

            TCT_Lib.eCurAppPage   = AppPage.PayConvoy;     // v2.0.2: give keys back to this popup
            TCT_Lib.bActiveEMVDlg = false;
        }

        protected void OnBtnConfirmPaymentClicked(object sender, EventArgs e)
        {
            Action_BtnConfirmPayment();
        }

        protected void Action_BtnConfirmPayment()
        {
            iOutTotalVehicle = GetTotalVehicle(ref iOutTotalClass1, ref iOutTotalClass2);
            if (iOutTotalVehicle == 0)
            {
                txtMsg.Text         = $"<span font_size = 'large' fgcolor = '#FFFF00'>ไม่พบจำนวนรถขบวน</span>";
                txtMsg.UseMarkup    = true;
                return;
            }
            //---------------------------------------------
            iOutPayType                 = iSelPayType;

            LogInfo.WriteLog("+ Close Convoy from Confirm");
            LogInfo.WriteLaneEvent("+ Close Convoy from Confirm");
            this.Respond(ResponseType.Ok);
            TCT_Lib.eCurAppPage         = AppPage.Main;
            TCT_Lib.bActiveConvoyDlg    = false;
            TCT_Lib.laneController.manageProcessPayConvoy -= LaneController_ManageProcessPayConvoy;
            Destroy();
            Dispose();
        }

        protected void OnBtnPrintReceiptClicked(object sender, EventArgs e)
        {
            Action_BtnPrintReceipt();
        }

        protected void Action_BtnPrintReceipt()
        {
            iOutTotalVehicle = GetTotalVehicle(ref iOutTotalClass1, ref iOutTotalClass2);
            if (iOutTotalVehicle == 0)
            {
                txtMsg.Text         = $"<span font_size = 'large' fgcolor = '#FFFF00'>ไม่พบจำนวนรถขบวน</span>";
                txtMsg.UseMarkup    = true;
                return;
            }
            //---------------------------------------------
            TCT_Lib.LaneDB.SetMaxRunningReceiptNo(iCurMaxReceiptID);

            //--- Process Print Receipt
            string szPayTypeName = TCT_Lib.GetPayMethodName((int)PayType.CASH);
            if (iSelPayType == 1)
                szPayTypeName = TCT_Lib.GetPayMethodName((int)PayType.EMV);
            else if (iSelPayType == 2)
                szPayTypeName = TCT_Lib.GetPayMethodName((int)PayType.QrCode);

            int nTrx = TCT_Lib.nTrx;
            for (int i = 0; i < iOutTotalClass1; i++)
            {
                PrintUtils.PrintReceiptLib.PrintReceipt(nTrx, false, true, "1", TCT_Lib.fFareClass1.ToString("N"), szPayTypeName);
                Thread.Sleep(1000);
                nTrx++;
            }

            for (int i = 0; i < iOutTotalClass2; i++)
            {
                PrintUtils.PrintReceiptLib.PrintReceipt(nTrx, false, true, "2", TCT_Lib.fFareClass2.ToString("N"), szPayTypeName);
                Thread.Sleep(1000);
                nTrx++;
            }
            //---------------------------------------------
            iOutPayType = iSelPayType;

/*
            LogInfo.WriteLog("+ Close Convoy from PrintReceipt");
            LogInfo.WriteLaneEvent("+ Close Convoy from PrintReceipt");
            this.Respond(ResponseType.Ok);
            TCT_Lib.eCurAppPage         = AppPage.Main;
            TCT_Lib.bActiveConvoyDlg    = false;
            TCT_Lib.laneController.manageProcessPayConvoy -= LaneController_ManageProcessPayConvoy;
            Destroy();
            Dispose();
*/                       
        }

    }
}
