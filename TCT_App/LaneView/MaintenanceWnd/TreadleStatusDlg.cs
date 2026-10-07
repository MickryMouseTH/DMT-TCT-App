using System;
using System.Threading;

namespace TCT_App.LaneView.MaintenanceWnd
{
    public partial class TreadleStatusDlg : Gtk.Dialog
    {
        //--- Const Value
        const int ON    = 1;
        const int OFF   = 0;
        //-------------------------------------------------
        public TreadleStatusDlg()
        {
            this.Build();
            this.Decorated = false;     // v2.0.2: no title bar on popups

            this.Title = "Test Treadle Status";
            ClearTreadleStatus();

            TCT_Lib.webSocketLane.digitalInputLaneEvent += LaneSocket_DigitalInputLaneEvent;
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

        protected void OnKeyPressEvent(object o, Gtk.KeyPressEventArgs args)
        {
            if ((int)args.Event.KeyValue == (int)'p')   // ESC, Cancel
            {
                Process_Exit();
            }
        }

        private void ClearTreadleStatus()
        {
            img_TreadleA.Pixbuf = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Fail-48.png");
            img_TreadleB.Pixbuf = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Fail-48.png");
            img_TreadleC.Pixbuf = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Fail-48.png");
            img_TreadleD.Pixbuf = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Fail-48.png");
        }

        protected void OnBtnOpenALBClicked(object sender, EventArgs e)
        {
            //--- ALB ON
            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Open, ON);
            Thread.Sleep(300);
            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Open, OFF);
        }

        protected void OnBtnCloseALBClicked(object sender, EventArgs e)
        {
            //--- ALB OFF
            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Close, ON);
            Thread.Sleep(300);
            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Close, OFF);
        }

        protected void LaneSocket_DigitalInputLaneEvent(int[] values, bool bMTC = true)
        {
            //--- Device Digital Input has change value
            bool bInMLB             = values[0] == 1;
            bool bInEFS             = values[1] == 1;
            bool bInPresenceLoop    = values[2] == 1;
            bool bInPassageLoop     = values[3] == 1;
            bool bInPresenceOB      = values[4] == 1;
            bool bInPassageOB       = values[5] == 1;
            bool bInALB             = values[6] == 1;
            bool bInOTL             = values[7] == 1;
            bool bInLTL             = values[8] == 1;
            bool bInEthErr          = values[9] == 1;
            bool bInSiren           = values[10] == 1;
            bool bInTreadleError    = values[11] == 1;
            bool bInTreadleA        = values[12] == 1;
            bool bInTreadleB        = values[13] == 1;
            bool bInTreadleC        = values[14] == 1;
            bool bInTreadleD        = values[15] == 1;

            img_TreadleA.Pixbuf = Gdk.Pixbuf.LoadFromResource(bInTreadleA ? "TCT_App.Images.OK-48.png" : "TCT_App.Images.Fail-48.png");
            img_TreadleB.Pixbuf = Gdk.Pixbuf.LoadFromResource(bInTreadleB ? "TCT_App.Images.OK-48.png" : "TCT_App.Images.Fail-48.png");
            img_TreadleC.Pixbuf = Gdk.Pixbuf.LoadFromResource(bInTreadleC ? "TCT_App.Images.OK-48.png" : "TCT_App.Images.Fail-48.png");
            img_TreadleD.Pixbuf = Gdk.Pixbuf.LoadFromResource(bInTreadleD ? "TCT_App.Images.OK-48.png" : "TCT_App.Images.Fail-48.png");


        }


    }
}
