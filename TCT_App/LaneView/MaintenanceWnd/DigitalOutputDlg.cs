using System;
using System.Threading;

namespace TCT_App.LaneView.MaintenanceWnd
{
    public partial class DigitalOutputDlg : Gtk.Dialog
    {
        //--- Const Value
        const int ON    = 1;
        const int OFF   = 0;
        int iSelDevice  = 0;
        //-----------------------------------------
        public DigitalOutputDlg()
        {
            this.Build();
            UiTheme.StylePopup(this);     // v2.0.2: no title bar + visible frame

            this.Title = "Test Digital Output";

            TCT_Lib.webSocketLane.deviceOutputLaneEvent += LaneSocket_DeviceOuputLaneEvent;
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ DigitalOutputDlg : popup opened, request DI/DO");
            TCT_Lib.webSocketLane.SendRequest_DigitalInputOutput();

            btn01_OFF.Sensitive = false;
        }

        private void ClearDigitalOutput()
        {
/*
            img_D1.Pixbuf = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Fail-48.png");
            img_D2.Pixbuf = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Fail-48.png");
            img_D3.Pixbuf = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Fail-48.png");
            img_D4.Pixbuf = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Fail-48.png");
            img_D5.Pixbuf = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Fail-48.png");
            img_D6.Pixbuf = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Fail-48.png");
*/                       
        }

        protected void OnButtonOkClicked(object sender, EventArgs e)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ DigitalOutputDlg : button OK clicked");
            Process_Exit();
        }

        private void Process_Exit()
        { 
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ DigitalOutputDlg : popup closed");
            TCT_Lib.webSocketLane.deviceOutputLaneEvent -= LaneSocket_DeviceOuputLaneEvent;

            Destroy();
            Dispose();
        }

        private void LaneSocket_DeviceOuputLaneEvent(int[] values)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ DigitalOutputDlg : DO update [{string.Join(",", values ?? new int[0])}]");
/*
            img_D1.Pixbuf = Gdk.Pixbuf.LoadFromResource(values[0] == 1 ? "TCT_App.Images.OK-48.png" : "TCT_App.Images.Fail-48.png");    // Register Bell
            img_D2.Pixbuf = Gdk.Pixbuf.LoadFromResource(values[1] == 1 ? "TCT_App.Images.OK-48.png" : "TCT_App.Images.Fail-48.png");    // Siren
            img_D3.Pixbuf = Gdk.Pixbuf.LoadFromResource(values[2] == 1 ? "TCT_App.Images.OK-48.png" : "TCT_App.Images.Fail-48.png");    // Becon (ASB)
            img_D4.Pixbuf = Gdk.Pixbuf.LoadFromResource(values[3] == 1 ? "TCT_App.Images.OK-48.png" : "TCT_App.Images.Fail-48.png");    // LBL
            if (values[4] == 1)
                img_D5.Pixbuf = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.OK-48.png");
            if (values[5] == 1)
                img_D5.Pixbuf = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Fail-48.png");
            img_D6.Pixbuf = Gdk.Pixbuf.LoadFromResource(values[6] == 1 ? "TCT_App.Images.OK-48.png" : "TCT_App.Images.Fail-48.png");    // LBL
*/            
        }

        protected void OnBtn01ONClicked(object sender, EventArgs e)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ DigitalOutputDlg : button Register Bell ON clicked");
            try
            {
                //--- Register Bell ON
                TCT_Lib.webSocketLane.OutIo_RBell();
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Fail Maintenance -> Output -> Register Bell : " + err.Message);
            }
        }

        protected void OnBtn01OFFClicked(object sender, EventArgs e)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ DigitalOutputDlg : button Register Bell OFF (no action) clicked");
            //--- Register Bell OFF

        }

        protected void OnBtn02ONClicked(object sender, EventArgs e)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ DigitalOutputDlg : button Siren ON clicked");
            try 
            {
                // Siren ON
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.Siren, ON);
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Fail Maintenance -> Output -> Siren ON : " + err.Message);
            }
        }

        protected void OnBtn02OFFClicked(object sender, EventArgs e)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ DigitalOutputDlg : button Siren OFF clicked");
            try 
            {
                //--- Siren OFF
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.Siren, OFF);
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Fail Maintenance -> Output -> Siren OFF : " + err.Message);
            }
        }

        protected void OnBtn03ONClicked(object sender, EventArgs e)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ DigitalOutputDlg : button ASB ON clicked");
            try 
            { 
                //--- ASB ON
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ASB, ON);
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Fail Maintenance -> Output -> ASB ON : " + err.Message);
            }
        }

        protected void OnBtn03OFFClicked(object sender, EventArgs e)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ DigitalOutputDlg : button ASB OFF clicked");
            try
            {
                //--- ASB OFF
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ASB, OFF);
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Fail Maintenance -> Output -> ASB OFF : " + err.Message);
            }
        }

        protected void OnBtn04ONClicked(object sender, EventArgs e)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ DigitalOutputDlg : button LTL ON clicked");
            try
            {
                //--- LTL ON
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.LTL, ON);
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Fail Maintenance -> Output -> LTL ON : " + err.Message);
            }
        }

        protected void OnBtn04OFFClicked(object sender, EventArgs e)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ DigitalOutputDlg : button LTL OFF clicked");
            try
            {
                //--- LTL OFF
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.LTL, OFF);
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Fail Maintenance -> Output -> LTL OFF : " + err.Message);
            }
        }

        protected void OnBtn05ONClicked(object sender, EventArgs e)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ DigitalOutputDlg : button ALB Open (pulse 300 ms) clicked");
            try 
            {
                //--- ALB ON
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Open, ON);
                Thread.Sleep(300);
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Open, OFF);
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Fail Maintenance -> Output -> ALB ON : " + err.Message);
            }
        }

        protected void OnBtn05OFFClicked(object sender, EventArgs e)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ DigitalOutputDlg : button ALB Close (pulse 300 ms) clicked");
            try 
            {
                //--- ALB OFF
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Close, ON);
                Thread.Sleep(300);
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Close, OFF);
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Fail Maintenance -> Output -> ALB OFF : " + err.Message);
            }
        }

        protected void OnBtn06ONClicked(object sender, EventArgs e)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ DigitalOutputDlg : button OTL ON clicked");
            try 
            {
                //--- OTL ON
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.OTL_RedGreen, ON);
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Fail Maintenance -> Output -> OTL ON : " + err.Message);
            }
        }

        protected void OnBtn06OFFClicked(object sender, EventArgs e)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, "+ DigitalOutputDlg : button OTL OFF clicked");
            try 
            {
                //--- OTL OFF
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.OTL_RedGreen, OFF);
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Fail Maintenance -> Output -> IOL OFF : " + err.Message);
            }
        }

        protected void OnKeyPressEvent(object o, Gtk.KeyPressEventArgs args)
        {
            try
            {
                LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ DigitalOutputDlg : key {args.Event.Key}, selected device {iSelDevice}");
                if ((int)args.Event.KeyValue == (int)'p')           // ESC, Cancel
                    Process_Exit();
                else if ((int)args.Event.KeyValue == (int)'0')
                    Process_DeviceOff();
                else if ((int)args.Event.KeyValue == (int)'1')
                    Process_DeviceOn();
                else if ((int)args.Event.KeyValue == (int)'2')
                    Process_SelectDevice(2);
                else if ((int)args.Event.KeyValue == (int)'3')
                    Process_SelectDevice(3);
                else if ((int)args.Event.KeyValue == (int)'4')
                    Process_SelectDevice(4);
                else if ((int)args.Event.KeyValue == (int)'5')
                    Process_SelectDevice(5);
                else if ((int)args.Event.KeyValue == (int)'6')
                    Process_SelectDevice(6);
                else if ((int)args.Event.KeyValue == (int)'7')
                    Process_SelectDevice(7);
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Fail Maintenance -> Output -> KeyPress : " + err.Message);
            }
        }

        protected void Process_DeviceOn()
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ DigitalOutputDlg : device {iSelDevice} ON (2=RBell 3=Siren 4=ASB 5=LTL 6=ALB 7=OTL)");
            try
            { 
                if (iSelDevice == 2)
                    TCT_Lib.webSocketLane.OutIo_RBell();
                else if (iSelDevice == 3)
                    TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.Siren, ON);
                else if (iSelDevice == 4)
                    TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ASB, ON);
                else if (iSelDevice == 5)
                    TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.LTL, ON);
                else if (iSelDevice == 6)
                {
                    //--- ALB ON
                    TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Open, ON);
                    Thread.Sleep(300);
                    TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Open, OFF);
                }
                else if (iSelDevice == 7)
                    TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.OTL_RedGreen, ON);
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Fail Maintenance -> Output -> Device ON : " + err.Message);
            }
        }

        protected void Process_DeviceOff()
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ DigitalOutputDlg : device {iSelDevice} OFF (3=Siren 4=ASB 5=LTL 6=ALB 7=OTL)");
            try 
            {
                if (iSelDevice == 3)
                    TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.Siren, OFF);
                else if (iSelDevice == 4)
                    TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ASB, OFF);
                else if (iSelDevice == 5)
                    TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.LTL, OFF);
                else if (iSelDevice == 6)
                {
                    //--- ALB OFF
                    TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Close, ON);
                    Thread.Sleep(300);
                    TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Close, OFF);
                }
                else if (iSelDevice == 7)
                    TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.OTL_RedGreen, OFF);
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Fail Maintenance -> Output -> Device OFF : " + err.Message);
            }
        }

        protected void Process_SelectDevice(int iDevice)
        {
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ DigitalOutputDlg : select device {iSelDevice} -> {iDevice}");
            iSelDevice = iDevice;
            txt_2.Text = "<span font_size='large' fgcolor='#ffffff'>2.</span>";
            txtDevice_2.Text = "<span font_size='large' fgcolor='#FFFFFF'>Register Bell (GONG)</span>";
            txt_3.Text = "<span font_size='large' fgcolor='#ffffff'>3.</span>";
            txtDevice_3.Text = "<span font_size='large' fgcolor='#FFFFFF'>Siren</span>";
            txt_4.Text = "<span font_size='large' fgcolor='#ffffff'>4.</span>";
            txtDevice_4.Text = "<span font_size='large' fgcolor='#FFFFFF'>Beacon (ASB)</span>";
            txt_5.Text = "<span font_size='large' fgcolor='#ffffff'>5.</span>";
            txtDevice_5.Text = "<span font_size='large' fgcolor='#FFFFFF'>LTL</span>";
            txt_6.Text = "<span font_size='large' fgcolor='#ffffff'>6.</span>";
            txtDevice_6.Text = "<span font_size='large' fgcolor='#FFFFFF'>ALB</span>";
            txt_7.Text = "<span font_size='large' fgcolor='#ffffff'>7.</span>";
            txtDevice_7.Text = "<span font_size='large' fgcolor='#FFFFFF'>OTL</span>";
            if (iSelDevice == 2)
            {
                txt_2.Text          = "<span font_size='large' fgcolor='#00ff00'>2.</span>";
                txtDevice_2.Text    = "<span font_size='large' fgcolor='#00ff00'>Register Bell (GONG)</span>";
            }
            else if (iSelDevice == 3)
            {
                txt_3.Text          = "<span font_size='large' fgcolor='#00ff00'>3.</span>";
                txtDevice_3.Text    = "<span font_size='large' fgcolor='#00ff00'>Siren</span>";
            }
            else if (iSelDevice == 4)
            {
                txt_4.Text          = "<span font_size='large' fgcolor='#00ff00'>4.</span>";
                txtDevice_4.Text    = "<span font_size='large' fgcolor='#00ff00'>Beacon (ASB)</span>";
            }
            else if (iSelDevice == 5)
            {
                txt_5.Text          = "<span font_size='large' fgcolor='#00ff00'>5.</span>";
                txtDevice_5.Text    = "<span font_size='large' fgcolor='#00ff00'>LTL</span>";
            }
            else if (iSelDevice == 6)
            {
                txt_6.Text          = "<span font_size='large' fgcolor='#00ff00'>6.</span>";
                txtDevice_6.Text    = "<span font_size='large' fgcolor='#00ff00'>ALB</span>";
            }
            else if (iSelDevice == 7)
            {
                txt_7.Text          = "<span font_size='large' fgcolor='#00ff00'>7.</span>";
                txtDevice_7.Text    = "<span font_size='large' fgcolor='#00ff00'>OTL</span>";
            }

            txt_2.UseMarkup = txt_3.UseMarkup = txt_4.UseMarkup = txt_5.UseMarkup =
                txt_6.UseMarkup = txt_7.UseMarkup = true;
            txtDevice_2.UseMarkup = txtDevice_3.UseMarkup = txtDevice_4.UseMarkup =
                txtDevice_5.UseMarkup = txtDevice_6.UseMarkup = txtDevice_7.UseMarkup = true;
        }
    }
}
