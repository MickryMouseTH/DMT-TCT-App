using System;
using Gtk;
using Pango;


namespace TCT_App.LaneView.MiscDlg
{
    public partial class DMT_PaymentDlg : Gtk.Dialog
    {
        Gdk.Pixbuf imgChkDmtCard;

        Gdk.Color clBlue                    = new Gdk.Color(10, 110, 170);
        Gdk.Color clBlack                   = new Gdk.Color(0, 0, 0);
        Gdk.Color clWhite                   = new Gdk.Color(255, 255, 255);
        Gdk.Color clGray                    = new Gdk.Color(150, 150, 150);

        public string szDmtCarNo = "", szLicensePlate = "", szDepartment = "", szAffilliation = "";
        public int iVehicleClass = 0;
        //----------------------------------------------------------------------
        public DMT_PaymentDlg()
        {
            TCT_Lib.bActiveDmtPaymentDlg = true;

            this.Build();
            UiTheme.StylePopup(this);     // v2.0.2: no title bar + visible frame
            //--- v2.0.2: popup buttons = same colour as the Cherry key, readable size
            UiTheme.StyleKey(buttonCancel, UiTheme.Red, UiTheme.PopupBtnFont);

            TCT_Lib.eCurAppPage = AppPage.DmtPayment;

            imgChkDmtCard = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Dmt_Card.png");
            InitUI();


            LogInfo.WriteLaneEvent("+ Load DMT Payment Dialog");
            //-------------------------------------------------------------------------
            TCT_Lib.laneController.dmtPaymentSmartCardReader    += LaneController_DmtPaymentSmartCardReader;
            TCT_Lib.laneController.manageProcessDmtPayment      += LaneController_ManageProcessDmtPayment;

            this.Activate();

            //--- For Auto Test
            //Action_ChkDmtPermissionProcess("0C28009C");
        }

        public void InitUI()
        {
            this.Title = "DMT PAYMENT";

            topTitle.ModifyBg(StateType.Normal, clBlue);
            topTitle.QueueDraw();

            imgDmtCard.Pixbuf = imgChkDmtCard.ScaleSimple(600, 160, Gdk.InterpType.Bilinear);

            txtMsg.ModifyFont(FontDescription.FromString("Ubuntu 24"));
            txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(0, 255, 255));
            txtMsg.Text = "รอการแตะบัตร DMT";
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

            layout.SetText("กรุณาแตะบัตร DMT เพื่อผ่านทาง");

            Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
            renderer.Drawable           = drawArea.GdkWindow;
            renderer.Gc                 = drawArea.Style.BlackGC;

            drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 15, layout, new Gdk.Color(255, 255, 255), clBlue);

            renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
            renderer.Drawable   = null;
            renderer.Gc         = null;
            renderer.Dispose();
            context.Dispose();
            args.RetVal         = true;
        }

        void LaneController_DmtPaymentSmartCardReader(string strVal)
        {
            LogInfo.WriteLaneEvent("+ Found SmartCard Reader Data (DMT Payment) : " + strVal);
            Application.Invoke(delegate
            {
                txtMsg.ModifyFont(FontDescription.FromString("Ubuntu 24"));
                txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(0, 255, 255));
                txtMsg.Text = "รหัสบัตร DMT : " + strVal;

                Action_ChkDmtPermissionProcess(strVal);
            });
        }

        private void Action_ChkDmtPermissionProcess(string szCardSerialNo)
        {
            Application.Invoke(delegate
            {
                //--- Check Serial No. and Permission

                if (TCT_Lib.LaneDB.CheckAllowDmtCanPass(szCardSerialNo, ref szDmtCarNo, ref szLicensePlate, ref szDepartment, ref szAffilliation, ref iVehicleClass))
                {
                    TCT_Lib.LaneDB.InsertDmtCardTransaction(szCardSerialNo, szDmtCarNo, szLicensePlate, szDepartment, szAffilliation, iVehicleClass);
                    //----------------------------------------------
                    this.Respond(ResponseType.Ok);
                    LogInfo.WriteLaneEvent("+ This card is allow pass");
                    TCT_Lib.laneController.dmtPaymentSmartCardReader    -= LaneController_DmtPaymentSmartCardReader;
                    TCT_Lib.laneController.manageProcessDmtPayment      -= LaneController_ManageProcessDmtPayment;
                    TCT_Lib.eCurAppPage = AppPage.Main;
                    TCT_Lib.bActiveDmtPaymentDlg = false;
                    Dispose();
                    Destroy();
                }
                else
                {
                    //--- Not found this serial no
                    txtMsg.ModifyFont(FontDescription.FromString("Ubuntu 24"));
                    txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                    txtMsg.Text = "บัตรนี้ ไม่อนุญาตให้ผ่าน";
                    LogInfo.WriteLaneEvent("- This card is not allow pass");
                }
            });
        }

        void LaneController_ManageProcessDmtPayment(int iKey)
        {
            Application.Invoke(delegate
            {
                if ((iKey == (int)KeyPressedType.EDIT) || (iKey == (int)KeyPressedType.ESC))   // v2.0.2: + CANCEL key
                {
                    Process_CancelDmtCard();
                }
/*
                else if (iKey == (int)KeyPressedType.ENTER)
                {
                    return;
                }
*/                
            }); 
        }

        protected void OnDeleteEvent(object o, DeleteEventArgs args)
        {
            TCT_Lib.laneController.dmtPaymentSmartCardReader    -= LaneController_DmtPaymentSmartCardReader;
            TCT_Lib.laneController.manageProcessDmtPayment      -= LaneController_ManageProcessDmtPayment;
            TCT_Lib.eCurAppPage             = AppPage.Main;
            TCT_Lib.bActiveDmtPaymentDlg    = false;
            this.Dispose();
            this.Destroy();
        }

        [GLib.ConnectBefore]
        protected void OnKeyPressEvent(object o, KeyPressEventArgs args)
        {
            if (TCT_Lib.keyMapping.ContainsKey((int)args.Event.KeyValue))
                TCT_Lib.webSocketLane.SendVirtualKeyboard(TCT_Lib.keyMapping[(int)args.Event.KeyValue]);
        }

        protected void OnButtonCancelClicked(object sender, EventArgs e)
        {
            Process_CancelDmtCard();
        }

        protected void Process_CancelDmtCard()
        {
            this.Respond(ResponseType.Cancel);
            TCT_Lib.laneController.dmtPaymentSmartCardReader    -= LaneController_DmtPaymentSmartCardReader;
            TCT_Lib.laneController.manageProcessDmtPayment      -= LaneController_ManageProcessDmtPayment;
            TCT_Lib.eCurAppPage             = AppPage.Main;
            TCT_Lib.bActiveDmtPaymentDlg    = false;
            Dispose();
            Destroy();
        }

    }
}
