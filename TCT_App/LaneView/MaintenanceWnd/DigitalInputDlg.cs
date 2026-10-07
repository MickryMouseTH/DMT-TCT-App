using System;
using System.Threading;

namespace TCT_App.MaintenanceWnd
{
    public partial class DigitalInputDlg : Gtk.Dialog
    {
        //--- Const Value
        const int ON    = 1;
        const int OFF   = 0;
        //-------------------------------------------------
        public DigitalInputDlg()
        {
            this.Build();
            this.Decorated = false;     // v2.0.2: no title bar on popups

            this.Title = "Test Digital Input";
            ClearDigitalInput();

            TCT_Lib.webSocketLane.digitalInputLaneEvent += LaneSocket_DigitalInputLaneEvent;
            TCT_Lib.webSocketLane.SendRequest_DeviceInput();
        }

        protected void OnButtonOkClicked(object sender, EventArgs e)
        {
            Process_Exit();
        }

        private void Process_Exit()
        { 
            TCT_Lib.webSocketLane.digitalInputLaneEvent -= LaneSocket_DigitalInputLaneEvent;

            Destroy();
            Dispose();
        }

        private void ClearDigitalInput()
        {
            try
            {
                img_D_Zero.Pixbuf   = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Fail-48.png");
                img_D1.Pixbuf       = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Fail-48.png");
                img_D2.Pixbuf       = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Fail-48.png");
                img_D3.Pixbuf       = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Fail-48.png");
                img_D4.Pixbuf       = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Fail-48.png");
                img_D5.Pixbuf       = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Fail-48.png");
                img_D6.Pixbuf       = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Fail-48.png");
                img_D7.Pixbuf       = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Fail-48.png");
            }
            catch(Exception err)
            {
                LogInfo.WriteLog("- Fail Maintenance -> Input : " + err.Message);
            }
        }

        private void LaneSocket_DigitalInputLaneEvent(int[] values, bool bMTC)
        {
            try
            {
                img_D_Zero.Pixbuf   = Gdk.Pixbuf.LoadFromResource(values[TCT_Lib.iLCSD_DI_0] == 1 ? "TCT_App.Images.OK-48.png" : "TCT_App.Images.Fail-48.png");
                img_D1.Pixbuf       = Gdk.Pixbuf.LoadFromResource(values[TCT_Lib.iLCSD_DI_1] == 1 ? "TCT_App.Images.OK-48.png" : "TCT_App.Images.Fail-48.png");
                img_D2.Pixbuf       = Gdk.Pixbuf.LoadFromResource(values[TCT_Lib.iLCSD_DI_2] == 1 ? "TCT_App.Images.OK-48.png" : "TCT_App.Images.Fail-48.png");
                img_D3.Pixbuf       = Gdk.Pixbuf.LoadFromResource(values[TCT_Lib.iLCSD_DI_3] == 1 ? "TCT_App.Images.OK-48.png" : "TCT_App.Images.Fail-48.png");
                img_D4.Pixbuf       = Gdk.Pixbuf.LoadFromResource(values[TCT_Lib.iLCSD_DI_4] == 1 ? "TCT_App.Images.OK-48.png" : "TCT_App.Images.Fail-48.png");
                img_D5.Pixbuf       = Gdk.Pixbuf.LoadFromResource(values[TCT_Lib.iLCSD_DI_5] == 1 ? "TCT_App.Images.OK-48.png" : "TCT_App.Images.Fail-48.png");
                img_D6.Pixbuf       = Gdk.Pixbuf.LoadFromResource(values[TCT_Lib.iLCSD_DI_6] == 1 ? "TCT_App.Images.OK-48.png" : "TCT_App.Images.Fail-48.png");
                img_D7.Pixbuf       = Gdk.Pixbuf.LoadFromResource(values[TCT_Lib.iLCSD_DI_7] == 1 ? "TCT_App.Images.OK-48.png" : "TCT_App.Images.Fail-48.png");
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Fail Maintenance -> Input : " + err.Message);
            }
        }

        protected void OnKeyPressEvent(object o, Gtk.KeyPressEventArgs args)
        {
            if ((int)args.Event.KeyValue == (int)'p')   // ESC, Cancel
            {
                Process_Exit();
            }
        }

        protected void OnBtnOpenALBClicked(object sender, EventArgs e)
        {
            try
            {
                //--- ALB ON
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Open, ON);
                Thread.Sleep(300);
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Open, OFF);
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Fail Maintenance -> OpenALB : " + err.Message);
            }
        }

        protected void OnBtnCloseALBClicked(object sender, EventArgs e)
        {
            try
            {
                //--- ALB OFF
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Close, ON);
                Thread.Sleep(300);
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Close, OFF);
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Fail Maintenance -> Input : " + err.Message);
            }
        }
    }
}
