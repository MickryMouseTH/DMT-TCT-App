#define __FULL_SCREEN__

using System;
using System.Reflection;
using System.Threading;
using Pango;
using Gtk;
using System.Diagnostics;
using TCT_App.RabbitMQ;
using TCT_App.MaintenanceWnd;
using TCT_App.LSD;
using System.Data;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace TCT_App
{
    public partial class LoginWnd : Gtk.Window
    {
        Gdk.Color clTxtStatus;
        string szTxtStatus              = "";
        string szUserName               = "";
        string szUserCode               = "";
        string szUserRole               = "";

        uint iTimerId1Sec               = 0;
        uint iTimerIdRTO                = 0;
        uint iTimerIdAutoSendRTO        = 0;
        uint iTimerIdAutoSendSupAdjust  = 0;
        uint iTimerIdAutoSendDC         = 0;
        uint iTimerIdSendScpTFI         = 0;
        int iFocus                      = 0;    // 0=Staff Code, 1=Staff Password
        public bool bLogined            = false;

        MainWindow mainWnd;
        MainMaintenance maintenanceWnd;

        Thread lsdWorkingThread;
        //----------------------------------------------------
        Gdk.Color clBlue                = new Gdk.Color(10, 110, 170);
        Gdk.Color clGreen               = new Gdk.Color(0x00, 0xb3, 0x3c);
        Gdk.Color clRed                 = new Gdk.Color(0xff, 0x00, 0x00);
        Gdk.Color clBkTxt               = new Gdk.Color(0x3A, 0x42, 0x4E);      // v2.0.3: slate (was brown 0x664d00)
        //----------------------------------------------------
        const string szTxt_FailConnectLC    = "Fail Connect LC / ไม่สามารถต่ออุปกรณ์ได้";
        const string szTxt_StartLogin       = "โปรดแตะบัตรพนักงาน หรือ กดปุ่ม Manual Login";
        const string szTxt_ManualLogin      = "โปรดใส่รหัสพนักงาน และ รหัสผ่าน";
        const string szTFI_CloseLane        = "ปิดช่องทาง";
        const string szTxt_NotAllowLogin    = "คุณไม่ถูกอนุญาตให้เข้าใช้งานระบบ";
        //-----------------------------------------------------
        public LoginWnd() : base(WindowType.Toplevel)
        {
            Build();
            //--- v2.0.2: header = MainWindow (logo 100 | info 708 | clock 205), same labels/fonts/columns
            frameHeader.HeightRequest   = 103;
            frameHeadInfo.WidthRequest  = UiTheme.HdrInfo;
            hbox5.BorderWidth           = 5;
            table1.WidthRequest         = UiTheme.HdrTable1;
            table2.WidthRequest         = UiTheme.HdrTable3;
            var hdrSpacer               = new Label("") { WidthRequest = UiTheme.HdrTable2 - 17 };  // no staff group on login (-17 = box spacing/borders of the missing table)
            hbox5.PackStart(hdrSpacer, false, false, 0);
            hbox5.ReorderChild(hdrSpacer, 1);
            hdrSpacer.Show();
            lblPlazaName.WidthRequest   = -1;
            UiTheme.StyleHeader(new[] { lblPlazaName, label6, label7 }, null,
                                new[] { label11, label12, label13 }, table1, table2);
            UiTheme.StyleStatusBar(statusbar1);
            UiTheme.FlattenFrames(this);
            frameDateTime.WidthRequest  = UiTheme.HdrClock;
            txtTime.WidthRequest        = UiTheme.HdrClock - 6;
            txtTime.Xalign              = 0.5f;
            txtDate.Xalign              = 0.5f;
            this.Activate();

#if __FULL_SCREEN__
            this.Fullscreen();
#endif

            //---------------------------------------------------------------------------------------------------
/*
            LogInfo.WriteLogLSD("+ Start TestConnection");
            if (TCT_Lib.TestConnection(TCT_Lib.szLSD_IPAddress, TCT_Lib.iLSD_SocketPort))
                LogInfo.WriteLogLSD($"-- TestConnection function, IP={TCT_Lib.szLSD_IPAddress}, Port={TCT_Lib.iLSD_SocketPort}  => Success");
            else
                LogInfo.WriteLogLSD($"-- TestConnection function, IP={TCT_Lib.szLSD_IPAddress}, Port={TCT_Lib.iLSD_SocketPort}  => Fail");
            LogInfo.WriteLogLSD("+ End TestConnection");

            LogInfo.WriteLogLSD("+ Start CheckConnectivity");
            if (TCT_Lib.CheckConnectivity(TCT_Lib.szLSD_IPAddress))
                LogInfo.WriteLogLSD($"-- CheckConnectivity function, IP={TCT_Lib.szLSD_IPAddress}, Port={TCT_Lib.iLSD_SocketPort}  => Success");
            else
                LogInfo.WriteLogLSD($"-- CheckConnectivity function, IP={TCT_Lib.szLSD_IPAddress}, Port={TCT_Lib.iLSD_SocketPort}  => Fail");
            LogInfo.WriteLogLSD("+ End CheckConnectivity");

            LogInfo.WriteLogLSD("+ Start TestConnection");
            if (TCT_Lib.TestConnection(TCT_Lib.szLSD_IPAddress, 80))
                LogInfo.WriteLogLSD($"-- TestConnection function, IP={TCT_Lib.szLSD_IPAddress}, Port=80  => Success");
            else
                LogInfo.WriteLogLSD($"-- TestConnection function, IP={TCT_Lib.szLSD_IPAddress}, Port=80  => Fail");
            LogInfo.WriteLogLSD("+ End TestConnection");
*/
            //----------------------------------------------------------------------------------------------------


            var appVersion          = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
            Assembly asm            = Assembly.GetExecutingAssembly();

            txtAppVersion.Text      = $"<span fgcolor='#ffffff'>Version : {TCT_Lib.szAppVersion}</span>";
            txtAppVersion.UseMarkup = true;

            TCT_Lib.bAllowETC       = TCT_Lib.iLaneConfigType > 0;
            if ((TCT_Lib.iLaneConfigType == 2) && (TCT_Lib.eCurLaneType == LaneType.MTC))
                TCT_Lib.bAllowETC = false;

            //if (!TCT_Lib.bAllowETC)
            TCT_Lib.bEnableEtcRabbitMQ = TCT_Lib.iLaneConfigType > 0;
            btn_ChangeLaneMode.Visible = TCT_Lib.iLaneConfigType == 2;

            TCT_Lib.eCurAppPage     = AppPage.Login;
            Update_Clock();
            iTimerId1Sec            = GLib.Timeout.Add(1000, new GLib.TimeoutHandler(Update_Clock));

            this.DeleteEvent += new DeleteEventHandler(OnLoginWndDelete);

            LogInfo.WriteLaneEvent("+ Initial Lane Object");
            TCT_Lib.LaneDB          = new LaneDBLib();
            TCT_Lib.mocRabbitMQ     = new MocRabbitMQ();
            TCT_Lib.etcRabbitMQ     = new EtcRabbitMQ();
            TCT_Lib.mtsRabbitMQ     = new MtsRabbitMQ();
            TCT_Lib.webSocketLane   = new LaneWebSocket();
            TCT_Lib.laneController  = new LaneEventController();

            if (TCT_Lib.bEnableLSD)
            {
                if (TCT_Lib.iLSD_MethodType == 0)
                {
                    if (TCT_Lib.iLSD_KeepAliveSec > 0)
                        InitialLSD_WorkingThread();
                    else
                    {
                        TCT_Lib.eCurLsdMode = LsdMode.LSD_CLOSE;
                        var lsdController   = new LaneSocketControl_LSD();
                        lsdController.RunInitialLSD();
                        //lsdController.ProcessSendCommandLSD();
                        //lsdController.CmdShow_LaneClose();
                        //lsdController.CloseSocketConnection();
                    }
                    //TCT_Lib.lsdController = new LaneSocketControl_LSD();
                    //TCT_Lib.lsdController.CmdShow_LaneClose();
                }
                else
                {
                    TCT_Lib.eCurLsdMode = LsdMode.LSD_CLOSE;
                    TCT_Lib.webSocketLane.laneSocketServerRTO.SendChangeLsdToRTO(TCT_Lib.eCurLsdMode);
                }
            }

            TCT_Lib.LaneDB.GetCurrentSupervisor();

            Update_PanelTitle();
            btnManualLogin.Child.ModifyFont(FontDescription.FromString("Arial 16"));
            btnLogin.Child.ModifyFont(FontDescription.FromString("Arial 16"));
            //--- v2.0.3: login screen in the same theme (green = เข้าสู่ระบบ, red = ยกเลิก, keypad = dark keys)
            UiTheme.StyleKey(btnManualLogin,      UiTheme.Blue,  "Sans Bold 16");
            UiTheme.StyleKey(btnLogin,            UiTheme.Green, "Sans Bold 16");
            UiTheme.StyleKey(btnCloseManualLogin, UiTheme.Red,   UiTheme.PopupBtnFont);
            UiTheme.SetButtonText(btnCloseManualLogin, "ยกเลิก");
            foreach (Button k in new[] { btnKey_1, btnKey_2, btnKey_3, btnKey_4, btnKey_5, btnKey_6, btnKey_7, btnKey_8, btnKey_9, btnKey_Zero })
                UiTheme.StyleKey(k, UiTheme.KeyDark, "Sans Bold 16");
            UiTheme.StyleKey(btnKey_DEL,   UiTheme.Orange, "Sans Bold 14");
            UiTheme.StyleKey(btnKey_ENTER, UiTheme.Green,  "Sans Bold 14");
            UiTheme.SetButtonText(btnKey_DEL, "ลบ");
            UiTheme.SetButtonText(btnKey_ENTER, "ตกลง");      // "ENTER" was cut off
            GtkLabel2.Markup = "<span fgcolor='#ffffff' weight='bold'> เข้าสู่ระบบด้วยรหัส </span>";
            ClearProcessStatus();

            //------------------------------------------------------------------
            TCT_Lib.ClearDatabaseFile();

            //------------------------------------------------------------------
            LogInfo.WriteLaneEvent("+ Start ConnectMocRabbitMQ");
            if (TCT_Lib.mocRabbitMQ.ConnectMocRabbitMQ())
            {
                TCT_Lib.bDC_NetworkStatus = true;
                sbDcStatus.Text = "<span fgcolor='#00ff00'>DC : Connect</span>";
            }
            else
            {
                TCT_Lib.bDC_NetworkStatus = false;
                sbDcStatus.Text = "<span fgcolor='#777777'>DC : DisConnect</span>";
            }
            sbDcStatus.UseMarkup = true;

            /*
                        LogInfo.WriteLog("+ Start Connect ETC RabbitMQ");
                        if (TCT_Lib.etcRabbitMQ.ConnectEtcRabbitMQ())
                        {
                            LogInfo.WriteLog("+ Success Connect ETC RabbitMQ");
                            TCT_Lib.etcRabbitMQ.SendALB_ModeToETC(ALB_Mode.Normal, "");
                        }
                        else
                        {
                            LogInfo.WriteLog("- Fail Connect ETC RabbitMQ");
                        }
                        LogInfo.WriteLog("+ End Connect RabbitMQ");
            */


            if (TCT_Lib.bEnableMTS)
                TCT_Lib.bMTS_NetworkStatus = (TCT_Lib.mtsRabbitMQ.ConnectMtsRabbitMQ());

            //--- Initial State of LC
            if (TCT_Lib.bAllowETC)
            {
                //--- Adisorn : Restart TCT in ETC mode don't send LaneMode Close (2022-04-25)
                //if (TCT_Lib.eCurLaneType != LaneType.ETC)
                {
                    //--- Send Initial Mode to ETC System   (2021-11-17)
                    LogInfo.WriteEtcRabbitMQ("+ Send Lane Mode : LaneModeClose");
                    TCT_Lib.etcRabbitMQ.Send_SetLaneMode_ToETC(LaneMode.CLOSED);
                }

                //----------------------------
                //TCT_Lib.etcRabbitMQ.Send_ControlOutputDevice_ToETC("OverheadTrafficLight", false);    // Comment 2021-12-16
                TCT_Lib.etcRabbitMQ.Send_ControlOutputDevice_ToETC("BalanceLight", false);
                TCT_Lib.laneController.SendEtcRabbitMq_CmdALB_Normal("Inital Process");
            }
            else
            {
                //--- Send Initial Mode to ETC System   (2021-11-17)
                if ((TCT_Lib.iLaneConfigType == 2) && (TCT_Lib.eCurLaneType == LaneType.MTC))
                {
                    LogInfo.WriteEtcRabbitMQ("+ Send Lane Mode (3) : LaneModeClose");
                    TCT_Lib.etcRabbitMQ.Send_SetLaneMode_ToETC(LaneMode.CLOSED, false, true);
                }

                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.OTL_RedGreen, 0);
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.LTL, 0);
                TCT_Lib.laneController.SendDigitalOutput_CmdALB_Close();

/*
                if (TCT_Lib.bEnableLSD)
                {
                    TCT_Lib.eCurLsdMode = LsdMode.LSD_CLOSE;
                    if (TCT_Lib.eCurLaneType == LaneType.MTC)
                        TCT_Lib.lsdController.CmdClear_OpenMTC();
                    else if (TCT_Lib.eCurLaneType == LaneType.ETC)
                        TCT_Lib.lsdController.CmdClear_OpenETC();
                    else if (TCT_Lib.eCurLaneType == LaneType.DUAL)
                        TCT_Lib.lsdController.CmdClear_OpenDual();
                }
*/                
            }

            //--------------------------------------------------------------------------------------
            TCT_Lib.laneController.SendLaneCurrentState();
            TCT_Lib.laneController.startManualStaffLogin    += LaneController_StartManualStaffLogin;
            TCT_Lib.laneController.cancelManualStaffLogin   += LaneController_CancelManualStaffLogin;
            TCT_Lib.laneController.manualKeyStaffCode       += LaneController_ManualKeyStaffCode;
            TCT_Lib.laneController.manualKeyStaffPassword   += LaneController_ManualKeyStaffPassword;
            TCT_Lib.laneController.manualStaffLoginStatus   += LaneController_ManualStaffLoginStatus;
            TCT_Lib.laneController.loginWithSmartCardReader += LaneController_LoginWithSmartCardReader;
            TCT_Lib.laneController.changeCurrentLaneType    += LaneController_ChangeCurrentLaneType;
            TCT_Lib.laneController.remoteLoginFromRTO       += LaneController_RemoteLoginFromRTO;
            TCT_Lib.laneController.manualLoginKey           += LaneController_ManualLoginKey;
            TCT_Lib.laneController.checkNetworkStatus       += LaneController_CheckNetworkStatus;
            TCT_Lib.laneController.updateUiSupervisor       += LaneController_UpdateUiSupervisor;
            TCT_Lib.laneController.setLaneTypeMode          += LaneController_SetLaneTypeMode;

            TCT_Lib.webSocketLane.laneSocketServerRTO.etcChangeLaneMode += LaneSocketServerRTO_EtcChangeLaneMode;
            //---------------------------------------------------------------------
            LogInfo.WriteLaneEvent("+ Start WebLaneSocket");
            if (!TCT_Lib.webSocketLane.StartLaneSocket())
            {
                szTxtStatus     = szTxt_FailConnectLC;
                clTxtStatus     = new Gdk.Color(255, 0, 0);
                areaStatus.ModifyBg(StateType.Normal, clTxtStatus);
                areaStatus.QueueDraw();

                btnManualLogin.Sensitive = btnLogin.Sensitive = false;
                GLib.Timeout.Add(5000, new GLib.TimeoutHandler(CheckLaneSocketConnection));

                //-------------------------------------------------------------------------
            }
            else
            {
                szTxtStatus     = szTxt_StartLogin;
                clTxtStatus     = new Gdk.Color(10, 110, 170);
                areaStatus.ModifyBg(StateType.Normal, clTxtStatus);
                areaStatus.QueueDraw();
            }

            LogInfo.WriteLaneEvent("+ Init Clear CRS-TFI");
            TCT_Lib.webSocketLane.OutIo_ClearCRS();
            TCT_Lib.laneController.ClearTextCRS_EndJob();
            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.CloseLane, (int)TFI_Pattern.Normal, "", "", "", szTFI_CloseLane, "");

            Thread.Sleep(200);
            TCT_Lib.webSocketLane.OutIo_ClearCRS();
            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.CloseLane, (int)TFI_Pattern.Normal, "", "", "", szTFI_CloseLane, "");

            TCT_Lib.laneController.SendLaneCurrentState();
            iTimerIdRTO                 = GLib.Timeout.Add(3000, new GLib.TimeoutHandler(SendLaneCurrentState));
            iTimerIdAutoSendRTO         = GLib.Timeout.Add(1000 * 60 * 3, new GLib.TimeoutHandler(AutoSendDbRTO));
            iTimerIdAutoSendSupAdjust   = GLib.Timeout.Add(1000 * 60 * 4, new GLib.TimeoutHandler(AutoSendDbSupAdjust));
            iTimerIdAutoSendDC          = GLib.Timeout.Add(1000 * 60 * 5, new GLib.TimeoutHandler(AutoSendDbRabbitMQ));
            iTimerIdSendScpTFI          = GLib.Timeout.Add(1000 * 60 * 2, new GLib.TimeoutHandler(AutoSendScpTFI));
            //----------------------------------------------------------
            TCT_Lib.LaneDB.GetShiftNotCloseEndShift();
            int iLastShiftID = TCT_Lib.LaneDB.ChkAutoEndOfLastJob();
            if (iLastShiftID > 0)
            {
                LogInfo.WriteLog("- Found Job not Close, then AutoClose and Send EOJ to DC : LastShiftID = " + iLastShiftID);
                //--- Found Job not close and Auto Close and Send EOJ to DC
                TCT_Lib.LaneDB.WriteLaneEndStaffShift(iLastShiftID, 1);
                //TCT_Lib.webSocketLane.SendCollectorJob(0);
                TCT_Lib.webSocketLane.SendCollectorAutoEndJob(iLastShiftID);
                TCT_Lib.SendEndJobObjToDC(iLastShiftID);
            }
            TCT_Lib.AutoResendEndJobToDC(TCT_Lib.iStaffShiftID);

            TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToClient("REQ_SUPERVISOR", null);
            //----------------------------------------------------------
            if (TCT_Lib.iLaneConfigType == 1)
            {
                LogInfo.WriteLaneEvent("+ AutoETC Mode : AutoLoginProcess");
                //--- Pure ETC
                Action_LoginProcess(LoginType.ETC_Auto);
            }
            else if ((TCT_Lib.iLaneConfigType == 2) && (TCT_Lib.eCurLaneType == LaneType.ETC))
            {
                LogInfo.WriteLaneEvent("+ AutoETC Mode (2) : AutoLoginProcess");
                Action_LoginProcess(LoginType.ETC_Auto);
            }

            //----------------------------------------------------------

            if ((TCT_Lib.iVideoNVR_CaptureImageMethod > 0) && (TCT_Lib.szVideoLane_CaptureImagePath != ""))
            {
                try
                {
                    if (!Directory.Exists(TCT_Lib.szVideoLane_CaptureImagePath))
                    {
                        LogInfo.WriteLog("+ Auto create capture image path : " + TCT_Lib.szVideoLane_CaptureImagePath);
                        Directory.CreateDirectory(TCT_Lib.szVideoLane_CaptureImagePath);
                    }
                    else
                        LogInfo.WriteLog("+ Found capture image path : " + TCT_Lib.szVideoLane_CaptureImagePath);
                }
                catch (Exception err)
                {
                    LogInfo.WriteLog($"- Error create capture image path={TCT_Lib.szVideoLane_CaptureImagePath}, Error={err.Message}");
                }
            }

            if (TCT_Lib.iVideoNVR_CaptureImageMethod > 0)
            {
                TCT_NVR.AutoClearFolderCaptureImage();
                TCT_Lib.iMaxImageCaptureCameraID = TCT_Lib.LaneDB.GetMaxImageCaptureID();
            }

            //----------------------------------------------------------
#if !__FULL_SCREEN__
            edtUserCode.Text        = "00101";      // Administrator
            //edtUserCode.Text        = "00111";      // Collector
            //edtUserCode.Text        = "01234";      // Maintenance
            edtUserPassword.Text    = "123456";
#endif
        }

        void OnLoginWndDelete(object sender, DeleteEventArgs a)
        {
            LogInfo.WriteLog("+ Login => OnLoginWndDelete for Close Application");
            TCT_Lib.laneController.SendLaneCloseApp();
            Thread.Sleep(500);
            //----------------------------------------------------
            this.Dispose();
            this.Destroy();
            Application.Quit();
            Environment.Exit(1);
            a.RetVal = true;
        }

        private bool Update_Clock()
        {
            //txtTime.Text = DateTime.Now.ToString("HH:mm:ss");
            string szTime = DateTime.Now.ToString("HH:mm:ss");
            string szDate = DateTime.Today.ToString("dd/MM/yyyy", DateTimeExtension.ThaiCultureInfo);

            if (TCT_Lib.bDC_NetworkStatus)
            {
                txtTime.Text = $"<span fgcolor='#ffffff'>{szTime}</span>";
                txtDate.Text = $"<span fgcolor='#ffffff'>{szDate}</span>";
            }
            else
            {
                txtTime.Text = $"<span fgcolor='#ff0000'>{szTime}</span>";
                txtDate.Text = $"<span fgcolor='#ff0000'>{szDate}</span>";
            }

            txtTime.UseMarkup = true;
            txtDate.UseMarkup = true;

            // returning true means that the timeout routine should be invoked
            // again after the timeout period expires.   Returning false would
            // terminate the timeout.

            return true;
        }

        private bool SendLaneCurrentState()
        {
            if (TCT_Lib.bRTO_Connected)
                TCT_Lib.laneController.SendLaneCurrentState();

            return true;
        }

        private bool AutoSendDbRTO()
        {
            //--- Background thread for auto send event, transaction, alarm to RTO database
            TCT_Lib.AutoResendLaneTransactionToRTO();
            TCT_Lib.AutoResendBeginJobToRTO();
            TCT_Lib.AutoResendEndJobToRTO();
            return true;
        }

        private bool AutoSendDbSupAdjust()
        {
            //--- Background thread for auto send event, transaction, to SupAdjust
            TCT_Lib.AutoResendLaneTransactionToSupAdjust();
            TCT_Lib.AutoResendBeginJobToSupAdjust();
            TCT_Lib.AutoResendEndJobToSupAdjust();
            return true;
        }

        private bool AutoSendDbRabbitMQ()
        {
            LogInfo.WriteLog("+ Start AutoSendDbRabbitMQ");
            if (!TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szDC_RabbitMQ_ServerIPAddress, TCT_Lib.iDC_RabbitMQ_ServerPort, TCT_Lib.iDC_RabbitMQ_TimeoutSec * 1000))
            {
                Console.WriteLine($"- Fail TestNetworkConnection to RabbitMQ => Connect DC Rabbit MQ : IP = {TCT_Lib.szDC_RabbitMQ_ServerIPAddress}, PORT = {TCT_Lib.iDC_RabbitMQ_ServerPort}");
                LogInfo.WriteLog($"- Fail TestNetworkConnection to RabbitMQ => Connect DC Rabbit MQ : IP = {TCT_Lib.szDC_RabbitMQ_ServerIPAddress}, PORT = {TCT_Lib.iDC_RabbitMQ_ServerPort}");
                TCT_Lib.bDC_NetworkStatus = false;
                return true;
            }

            //--- Background thread for auto send lane transaction to DataCenter by Rabbit MQ
            LogInfo.WriteLog("+ AutoSendDbRabbitMQ : StaffShiftID=" + TCT_Lib.iStaffShiftID);
            TCT_Lib.AutoResendLaneTransactionToDC();
            TCT_Lib.AutoResendBeginJobToDC(TCT_Lib.iStaffShiftID);
            //TCT_Lib.AutoResendEndJobToDC(TCT_Lib.iStaffShiftID);
            return true;
        }

        private bool AutoSendScpTFI()
        {
            LogInfo.WriteLog("+ Start AutoSendScpTFI");


            return true;
        }

        protected void Update_PanelTitle()
        {
            Assembly myAssembly = Assembly.GetExecutingAssembly();
            imgLogo.Pixbuf      = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Logo.png");

            txtPlazaName.Text   = TCT_Lib.szPlazaName;
            txtPlazaName.ModifyFg(StateType.Normal, TCT_Lib.clLabelHeader);

            txtLaneName.Text    = TCT_Lib.szLaneName;
            txtLaneName.ModifyFg(StateType.Normal, TCT_Lib.clLabelHeader);

            txtLaneType.Text    = TCT_Lib.GetCurLaneTypeName();
            txtLaneType.ModifyFg(StateType.Normal, TCT_Lib.clLabelHeader);

            txtJobNo.Text       = TCT_Lib.LaneDB.GetStartJobNo().ToString("0") ;
            txtJobNo.ModifyFg(StateType.Normal, TCT_Lib.clLabelHeader);

            txtStartShift.Text  = TCT_Lib.dtSupervisorStartShift.ToString("dd-MM-yyyy HH:mm:ss", DateTimeExtension.ThaiCultureInfo);
            txtStartShift.ModifyFg(StateType.Normal, TCT_Lib.clLabelHeader);

            txtSupervisorName.Text  = TCT_Lib.szSupervisorName;
            txtSupervisorName.ModifyFg(StateType.Normal, TCT_Lib.clLabelHeader);

            //if (TCT_Lib.bAllowETC && (TCT_Lib.szSupervisorName != ""))
            //    TCT_Lib.etcRabbitMQ.SendChangeLoginSupervisorStaffToETC();

            txtTime.ModifyFont(FontDescription.FromString("Ubuntu Bold 22"));
            txtTime.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

            txtDate.ModifyFont(FontDescription.FromString("Ubuntu Regular 12"));
            txtDate.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
        }

        private bool CheckLaneSocketConnection()
        {
            if (!TCT_Lib.webSocketLane.StartLaneSocket())
            {
                szTxtStatus = szTxt_FailConnectLC;
                clTxtStatus = new Gdk.Color(255, 0, 0);
                areaStatus.ModifyBg(StateType.Normal, clTxtStatus);
                areaStatus.QueueDraw();

                btnManualLogin.Sensitive = btnLogin.Sensitive = false;

                return true;
            }
            else
            {
                szTxtStatus = szTxt_StartLogin;
                clTxtStatus = new Gdk.Color(10, 110, 170);
                areaStatus.ModifyBg(StateType.Normal, clTxtStatus);
                areaStatus.QueueDraw();

                btnManualLogin.Sensitive = btnLogin.Sensitive = true;

                return false;
            }
        }

        protected void ClearProcessStatus()
        {
            pbProcess.Fraction = 0.0;
            //pbProcess.Visible   = false;
            pbProcess.ModifyBg(StateType.Normal, new Gdk.Color(0, 255, 255));

            panelLogin.ModifyBg(StateType.Normal, clGreen);

            szTxtStatus = szTxt_StartLogin;
            clTxtStatus = new Gdk.Color(10, 110, 170);
            areaStatus.ModifyBg(StateType.Normal, clTxtStatus);
            areaStatus.QueueDraw();

            areaUserName.ModifyBg(StateType.Normal, clBkTxt);
            areaUserCode.ModifyBg(StateType.Normal, clBkTxt);
            areaUserRole.ModifyBg(StateType.Normal, clBkTxt);

            areaUserCode.QueueDraw();
            areaUserName.QueueDraw();
            areaUserRole.QueueDraw();

            //edtUserCode.Sensitive = false;
            //edtUserPassword.GrabFocus();

        }

        protected void OnAreaStatusExposeEvent(object o, ExposeEventArgs args)
        {
            DrawingArea drawArea = o as DrawingArea;

            //---  DrawArea ClassType
            int iWidth          = drawArea.Allocation.Width;
            Context context     = drawArea.CreatePangoContext();
            Pango.Layout layout = new Pango.Layout(context);
            if (layout != null)
                layout.Width = Units.FromPixels(iWidth);
            layout.Alignment = Pango.Alignment.Center;

            FontDescription fontDesc = FontDescription.FromString("Ubuntu 18");
            layout.FontDescription = fontDesc;

            layout.SetText(szTxtStatus);

            Gdk.PangoRenderer renderer = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
            renderer.Drawable   = drawArea.GdkWindow;
            renderer.Gc         = drawArea.Style.BlackGC;

            renderer.SetOverrideColor(RenderPart.Foreground, new Gdk.Color(0, 0, 0));
            //renderer.DrawLayout(layout, 0, 0);
            drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 10, layout, new Gdk.Color(255, 255, 255), clTxtStatus);

            renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
            renderer.Drawable   = null;
            renderer.Gc         = null;
        }

        protected void OnBtnLoginClicked(object sender, EventArgs e)
        {
            Action_LoginProcess(LoginType.Manual);
        }

        //=== 0=Use Smart Card Login, 1=Manual Login, 2=RTO remote login, 3=ETC auto login
        protected void Action_LoginProcess(LoginType eLoginType, string szCardSerialNo = "")
        {
            Application.Invoke(delegate
            {

                //------------------------------------------------------------------------------------
                if (TCT_Lib.bEnableEdc)
                {
                    //--- Check EDC TID is 8 digit
                    if (TCT_Lib.szEdcTID.Length != 8)
                    {
                        LogInfo.WriteLaneEvent("- Fail Check Edc TID 8 digits : EdcTID=" + TCT_Lib.szEdcTID);
                        szTxtStatus = "Edc TID is not 8 digits. Please check config";
                        clTxtStatus = new Gdk.Color(255, 0, 0);
                        areaStatus.ModifyBg(StateType.Normal, clTxtStatus);
                        areaStatus.QueueDraw();

                        bLogined    = false;
                        return;
                    }
                }
                //------------------------------------------------------------------------------------
                string szErrMsg = "";
                //if ((szUserID != "") || (szPassword != ""))
                if (eLoginType == LoginType.Manual)
                {
                    string szUserID     = this.edtUserCode.Text;
                    string szPassword   = edtUserPassword.Text;

                    //LogInfo.WriteLaneEvent($"+ Start Login Manual UserID={szUserID}, Password={szPassword}");
                    LogInfo.WriteLaneEvent($"+ Start Login Manual UserID={szUserID}");
                    if (!TCT_Lib.LaneDB.CheckStaffManualUserPassword(szUserID, szPassword, ref szErrMsg))
                    {
                        LogInfo.WriteLaneEvent("- Fail CheckStaffManualUserPassword : " + szErrMsg);
                        szTxtStatus                 = szErrMsg;
                        clTxtStatus                 = new Gdk.Color(255, 0, 0);
                        areaStatus.ModifyBg(StateType.Normal, clTxtStatus);
                        areaStatus.QueueDraw();
                        edtUserPassword.Text        = "";
                        edtUserPassword.Sensitive   = true;
                        edtUserPassword.GrabFocus();

                        bLogined                    = false;
                        return;
                    }
                    LogInfo.WriteLaneEvent($"Login Manual Success : Staff={szUserID}");
                }
                else if (eLoginType == LoginType.SmartCard)
                {
                    LogInfo.WriteLaneEvent("+ Start Login Smart Card : SerialNo=" + szCardSerialNo);
                    if (!TCT_Lib.LaneDB.CheckStaffWithSmartCard(szCardSerialNo, ref szErrMsg))
                    {
                        LogInfo.WriteLaneEvent("- Fail CheckStaffWithSmartCard : " + szErrMsg);
                        szTxtStatus         = szErrMsg;
                        clTxtStatus         = new Gdk.Color(255, 0, 0);
                        areaStatus.ModifyBg(StateType.Normal, clTxtStatus);
                        areaStatus.QueueDraw();
                        bLogined            = false;

                        return;
                    }
                    LogInfo.WriteLaneEvent($"Login SmartCard Success : SerialNo={szCardSerialNo}, StaffID={TCT_Lib.szStaffID}, StaffName={TCT_Lib.szStaffName}");
                }
                else if (eLoginType == LoginType.ETC_Auto)
                {
                    TCT_Lib.szStaffID           = TCT_Lib.szSupervisorID;
                    TCT_Lib.szStaffCode         = TCT_Lib.szSupervisorCode;
                    TCT_Lib.szStaffName         = TCT_Lib.szSupervisorName;
                    TCT_Lib.szStaffGroupID      = TCT_Lib.szSupervisorGroupID;
                    TCT_Lib.szStaffGroupName    = TCT_Lib.szSupervisorGroupName;

                    //TCT_Lib.etcRabbitMQ.Send_SetLaneMode_ToETC(LaneMode.CLOSED);
                    //TCT_Lib.etcRabbitMQ.Send_ControlOutputDevice_ToETC("OverheadTrafficLight", true);
                    //TCT_Lib.etcRabbitMQ.Send_ControlOutputDevice_ToETC("BalanceLight", false);

                    LogInfo.WriteLaneEvent($"Auto Login with ETC Mode : StaffID={TCT_Lib.szStaffID}, StaffCode={TCT_Lib.szStaffCode}, StaffName={TCT_Lib.szStaffName}");
                }
                else if (eLoginType == LoginType.RTO_Remote)
                {
                    LogInfo.WriteLaneEvent("+ Start Login RTO Remote");
                    if (!TCT_Lib.LaneDB.CheckStaffRemoteStaff(szCardSerialNo, ref szErrMsg))
                    {
                        LogInfo.WriteLaneEvent("- Fail CheckStaffRemoteStaff : " + szErrMsg);
                        szTxtStatus     = szErrMsg;
                        clTxtStatus     = new Gdk.Color(255, 0, 0);
                        areaStatus.ModifyBg(StateType.Normal, clTxtStatus);
                        areaStatus.QueueDraw();
                        bLogined        = false;

                        return;
                    }
                    LogInfo.WriteLaneEvent($"Login from RTO Remote Success : Staff Code={szCardSerialNo}");
                }

                bool bFailWebService    = false;
                //bool bMaintenanceStaff  = (int.Parse(TCT_Lib.szStaffGroupID) >= 50) && (int.Parse(TCT_Lib.szStaffGroupID) <= 59);

                bool bMaintenanceStaff  = false;
                //-----------------------------------------------------------------------------------------------------------
                //--- Check Staff Allow Access
                if (TCT_Lib.szAllowMaintenanceRoleGroupID == "")
                {
                    bMaintenanceStaff = (int.Parse(TCT_Lib.szStaffGroupID) >= 50) && (int.Parse(TCT_Lib.szStaffGroupID) <= 59);
                }
                else
                { 
                    List<string> szListAllowMaintenanceRole = TCT_Lib.szAllowMaintenanceRoleGroupID.Split(',').ToList();
                    if (szListAllowMaintenanceRole.Contains(TCT_Lib.szStaffGroupID))
                        bMaintenanceStaff = true;
                }

                if (!bMaintenanceStaff)
                {
                    if (TCT_Lib.szAllowTollRoleGroupID.Trim() == "")
                    {
                        LogInfo.WriteLaneEvent("- Not found allow toll role group id in config");
                        szTxtStatus     = "ไม่พบ Config ให้เเข้าใช้ระบบ";
                        clTxtStatus     = new Gdk.Color(255, 0, 0);
                        areaStatus.ModifyBg(StateType.Normal, clTxtStatus);
                        areaStatus.QueueDraw();
                        bLogined = false;

                        return;
                    }
                    else
                    {
                        //--- Check Staff Login
                        List<string> szListAllowLoginStaffRole = TCT_Lib.szAllowTollRoleGroupID.Split(',').ToList();
                        if (!szListAllowLoginStaffRole.Contains(TCT_Lib.szStaffGroupID))
                        {
                            LogInfo.WriteLaneEvent("- This staff is not allow login program");
                            szTxtStatus = szTxt_NotAllowLogin;
                            clTxtStatus = new Gdk.Color(255, 0, 0);
                            areaStatus.ModifyBg(StateType.Normal, clTxtStatus);
                            areaStatus.QueueDraw();
                            bLogined    = false;

                            return;
                        }
                    }
                }

                //-----------------------------------------------------------------------------------------------------------
                if ((eLoginType != LoginType.ETC_Auto) && (eLoginType != LoginType.RTO_Remote))
                {
                    if (!bMaintenanceStaff)
                    {
                        if (TCT_Lib.iLaneWebService_ChkStartJobTOD > 0)
                        {
                            bool bFoundBojTOD = false;
                            int iStaffShiftId = 0;
                            if (!WebServiceAPI.WebService_BeginEndJob.CallWebService_ChkStartJobTOD(TCT_Lib.szStaffCode, ref bFoundBojTOD, ref iStaffShiftId, ref bFailWebService, ref szErrMsg))
                            {
                                LogInfo.WriteLaneEvent("- Fail Check Start Job TOD after Login (Call WebService) : " + szErrMsg);
                                if (!bFailWebService)
                                {
                                    szTxtStatus     = szErrMsg;
                                    clTxtStatus     = new Gdk.Color(255, 0, 0);
                                    areaStatus.ModifyBg(StateType.Normal, clTxtStatus);
                                    areaStatus.QueueDraw();
                                    if (eLoginType == LoginType.Manual)
                                    {
                                        edtUserPassword.Text        = "";
                                        edtUserPassword.Sensitive   = true;
                                        edtUserPassword.GrabFocus();
                                    }

                                    TCT_Lib.szStaffID           = "";
                                    TCT_Lib.szStaffCode         = "";
                                    TCT_Lib.szStaffName         = "";
                                    TCT_Lib.szStaffGroupID      = "";
                                    TCT_Lib.szStaffGroupName    = "";

                                    bLogined                    = false;
                                    return;
                                }
                                else
                                {
                                    LogInfo.WriteLaneEvent("- Fail Web Service TA Server Network, Then allow auto start job.");
                                    LogInfo.WriteLog("- Fail Web Service TA Server Network, Then allow auto start job.");
                                    bFoundBojTOD = true;
                                }
                            }

                            if (!bFoundBojTOD)
                            {
                                LogInfo.WriteLog("- Fail Check Start Job TOD after Login : " + szErrMsg);
                                szTxtStatus     = szErrMsg;
                                clTxtStatus     = new Gdk.Color(255, 0, 0);
                                areaStatus.ModifyBg(StateType.Normal, clTxtStatus);
                                areaStatus.QueueDraw();
                                if (eLoginType == LoginType.Manual)
                                {
                                    edtUserPassword.Text        = "";
                                    edtUserPassword.Sensitive   = true;
                                    edtUserPassword.GrabFocus();
                                }

                                TCT_Lib.szStaffID           = "";
                                TCT_Lib.szStaffCode         = "";
                                TCT_Lib.szStaffName         = "";
                                TCT_Lib.szStaffGroupID      = "";
                                TCT_Lib.szStaffGroupName    = "";

                                bLogined = false;
                                return;
                            }

                            //-------------------------------------------------------
                            LogInfo.WriteLog($"+ Success Check Start Job TOD : StaffCode={TCT_Lib.szStaffCode}. ShiftId={iStaffShiftId}");

                        }
                    }
                }
                //----------------------------------------------------------------------------
                //--- 2024-12-17 (Add feature auto resend EOJ)
                LogInfo.WriteLog("+ Call Auto Resend EOJ to DC when Start BOJ");
                TCT_Lib.AutoResendEndJobToDC(TCT_Lib.iStaffShiftID);
                //----------------------------------------------------------------------------
                //this.Dispose();
                //this.Destroy();
                this.Hide();

                GLib.Source.Remove(iTimerId1Sec);
                GLib.Source.Remove(iTimerIdRTO);
                GLib.Source.Remove(iTimerIdAutoSendRTO);
                GLib.Source.Remove(iTimerIdAutoSendSupAdjust);
                GLib.Source.Remove(iTimerIdAutoSendDC);
                GLib.Source.Remove(iTimerIdSendScpTFI);

                Gdk.Color colBk     = new Gdk.Color(0x2E, 0x2E, 0x2E);
                iTimerId1Sec        = iTimerIdRTO = 0;
                iTimerIdAutoSendRTO = iTimerIdAutoSendSupAdjust = iTimerIdAutoSendDC = 0;

                this.DeleteEvent -= new DeleteEventHandler(OnLoginWndDelete);

                if (TCT_Lib.laneController != null)
                    TCT_Lib.laneController.ClearAndInitialVar();

                //bool bMaintenanceStaff = (int.Parse(TCT_Lib.szStaffGroupID) >= 50) && (int.Parse(TCT_Lib.szStaffGroupID) <= 59);
                if (bMaintenanceStaff)
                {
                    if ((TCT_Lib.bAllowETC) || TCT_Lib.bEnableEtcRabbitMQ)
                    {
                        if (TCT_Lib.eCurLaneType == LaneType.MTC)
                            TCT_Lib.etcRabbitMQ.Send_SetLaneMode_ToETC(LaneMode.NORMAL, true);
                        else
                            TCT_Lib.etcRabbitMQ.Send_SetLaneMode_ToETC(LaneMode.MAINTENANCE);
                    }
                    //--- Maintenance Mode
                    TCT_Lib.bMaintenanceMode = true;
                    maintenanceWnd  = new MainMaintenance();
                    maintenanceWnd.ModifyBg(StateType.Normal, colBk);
                    var screen      = maintenanceWnd.GdkWindow.Screen;
                    maintenanceWnd.GdkWindow.GetSize(out var winWidth, out var winHeight);
                    maintenanceWnd.Move((screen.Width / 2) - (winWidth / 2), (screen.Height / 2) - (winHeight / 2));

                    //maintenanceWnd.SetPosition(WindowPosition.Center);
                    maintenanceWnd.Show();
                    maintenanceWnd.HasFocus = true;
                    maintenanceWnd.Activate();
#if __FULL_SCREEN__
                    maintenanceWnd.Fullscreen();
#endif
                }
                else
                {
                    //--- Normal Mode
                    if (TCT_Lib.bAllowETC)
                    {
                        if (TCT_Lib.eCurLaneType == LaneType.MTC)
                            TCT_Lib.etcRabbitMQ.Send_SetLaneMode_ToETC(LaneMode.NORMAL, true);
                        else
                            TCT_Lib.etcRabbitMQ.Send_SetLaneMode_ToETC(LaneMode.NORMAL);

                        if (eLoginType == LoginType.ETC_Auto)
                        {
                            //TCT_Lib.etcRabbitMQ.Send_ControlOutputDevice_ToETC("BalanceLight", false);
                        }
                    }
                    else
                    {
                        TCT_Lib.etcRabbitMQ.Send_SetLaneMode_ToETC(LaneMode.CLOSED, true);
                    }

                    //------------------------------------------------------------------------------
                    TCT_Lib.bAllowSendRTO       = true;
                    TCT_Lib.bMaintenanceMode    = false;
                    mainWnd     = new MainWindow(LoginMethod.MANUAL, eLoginType);
                    mainWnd.ModifyBg(StateType.Normal, colBk);

                    var screen  = mainWnd.GdkWindow.Screen;
                    mainWnd.GdkWindow.GetSize(out var winWidth, out var winHeight);
                    mainWnd.Move((screen.Width / 2) - (winWidth / 2), (screen.Height / 2) - (winHeight / 2));

                    if (TCT_Lib.laneController != null)
                        TCT_Lib.laneController.ClearAndInitialVar();
                    //mainWnd.SetPosition(WindowPosition.Center);
                    mainWnd.Show();
                    mainWnd.HasFocus = true;
                    mainWnd.Activate();
#if __FULL_SCREEN__
                    mainWnd.Fullscreen();
#endif
                }
            });
        }

        protected void OnAreaUserNameExposeEvent(object o, ExposeEventArgs args)
        {
            DrawingArea drawArea = o as DrawingArea;

            //---  DrawArea ClassType
            int iWidth              = drawArea.Allocation.Width;
            Context context         = drawArea.CreatePangoContext();
            Pango.Layout layout     = new Pango.Layout(context);
            if (layout != null)
                layout.Width = Units.FromPixels(iWidth);
            layout.Alignment        = Pango.Alignment.Center;

            FontDescription fontDesc    = FontDescription.FromString("Ubuntu 14");
            layout.FontDescription      = fontDesc;

            layout.SetText(string.IsNullOrEmpty(szUserName) ? "—" : szUserName);     // v2.0.3: empty box looked unused

            Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
            renderer.Drawable           = drawArea.GdkWindow;
            renderer.Gc                 = drawArea.Style.BlackGC;

            renderer.SetOverrideColor(RenderPart.Foreground, new Gdk.Color(0, 0, 0));
            //renderer.DrawLayout(layout, 0, 0);
            drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 6, layout, new Gdk.Color(255, 255, 255), clBkTxt);

            renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
            renderer.Drawable           = null;
            renderer.Gc                 = null;
        }

        protected void OnAreaUserCodeExposeEvent(object o, ExposeEventArgs args)
        {
            DrawingArea drawArea = o as DrawingArea;

            //---  DrawArea ClassType
            int iWidth                  = drawArea.Allocation.Width;
            Context context             = drawArea.CreatePangoContext();
            Pango.Layout layout         = new Pango.Layout(context);
            if (layout != null)
                layout.Width = Units.FromPixels(iWidth);
            layout.Alignment = Pango.Alignment.Center;

            FontDescription fontDesc    = FontDescription.FromString("Ubuntu 14");
            layout.FontDescription      = fontDesc;

            layout.SetText(string.IsNullOrEmpty(szUserCode) ? "—" : szUserCode);     // v2.0.3: empty box looked unused

            Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
            renderer.Drawable           = drawArea.GdkWindow;
            renderer.Gc                 = drawArea.Style.BlackGC;

            renderer.SetOverrideColor(RenderPart.Foreground, new Gdk.Color(0, 0, 0));
            //renderer.DrawLayout(layout, 0, 0);
            drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 6, layout, new Gdk.Color(255, 255, 255), clBkTxt);

            renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
            renderer.Drawable   = null;
            renderer.Gc         = null;
        }

        protected void OnAreaUserRoleExposeEvent(object o, ExposeEventArgs args)
        {
            DrawingArea drawArea = o as DrawingArea;

            //---  DrawArea ClassType
            int iWidth          = drawArea.Allocation.Width;
            Context context     = drawArea.CreatePangoContext();
            Pango.Layout layout = new Pango.Layout(context);
            if (layout != null)
                layout.Width = Units.FromPixels(iWidth);
            layout.Alignment    = Pango.Alignment.Center;

            FontDescription fontDesc    = FontDescription.FromString("Ubuntu 14");
            layout.FontDescription      = fontDesc;

            layout.SetText(string.IsNullOrEmpty(szUserRole) ? "—" : szUserRole);     // v2.0.3: empty box looked unused

            Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
            renderer.Drawable           = drawArea.GdkWindow;
            renderer.Gc                 = drawArea.Style.BlackGC;

            renderer.SetOverrideColor(RenderPart.Foreground, new Gdk.Color(0, 0, 0));
            //renderer.DrawLayout(layout, 0, 0);
            drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 6, layout, new Gdk.Color(255, 255, 255), clBkTxt);

            renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
            renderer.Drawable   = null;
            renderer.Gc         = null;
        }

        protected void OnBtnManualLoginClicked(object sender, EventArgs e)
        {
            szUserName = "";
            szUserCode = "";
            szUserRole = "";
            areaUserCode.QueueDraw();
            areaUserName.QueueDraw();
            areaUserRole.QueueDraw();

            szTxtStatus = szTxt_ManualLogin;
            clTxtStatus = new Gdk.Color(10, 110, 170);
            areaStatus.ModifyBg(StateType.Normal, clTxtStatus);
            areaStatus.QueueDraw();

            frameManualLogin.Visible = frameNumKey.Visible = true;
            edtUserCode.GrabFocus();
        }
        
        protected void OnBtnCloseManualLoginClicked(object sender, EventArgs e)
        {
            szUserName = "";
            szUserCode = "";
            szUserRole = "";
            areaUserCode.QueueDraw();
            areaUserName.QueueDraw();
            areaUserRole.QueueDraw();

            szTxtStatus = szTxt_ManualLogin;
            clTxtStatus = new Gdk.Color(10, 110, 170);
            areaStatus.ModifyBg(StateType.Normal, clTxtStatus);
            areaStatus.QueueDraw();

            frameManualLogin.Visible = frameNumKey.Visible = false;
            btnManualLogin.GrabFocus();
        }

        protected void OnBtnTouchCardClicked(object sender, EventArgs e)
        {
            Action_LoginProcess(LoginType.SmartCard, "");
        }

        protected void OnAreaStatusButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (TCT_Lib.iDevMode > 0)
            {
                LogInfo.WriteLog("+ Login => Click StatusBar for Close Application");
                TCT_Lib.laneController.SendLaneCloseApp();
                Thread.Sleep(100);
                //----------------------------------------------------
                this.Dispose();
                this.Destroy();
                RootWindow.Destroy();
                Application.Quit();
                Environment.Exit(1);
                args.RetVal = true;
            }
        }

        //-----------------------------------------------------------
        void ChangeLoginSmartCard()
        {
            if (frameManualLogin.Visible)
            {
                //--- Clear Manual Login
                szUserName = "";
                szUserCode = "";
                szUserRole = "";
                areaUserCode.QueueDraw();
                areaUserName.QueueDraw();
                areaUserRole.QueueDraw();

                szTxtStatus = szTxt_StartLogin;
                clTxtStatus = new Gdk.Color(10, 110, 170);
                areaStatus.ModifyBg(StateType.Normal, clTxtStatus);
                areaStatus.QueueDraw();

                edtUserCode.Text = edtUserPassword.Text = "";
                frameManualLogin.Visible = frameNumKey.Visible = false;
            }
        }

        void LaneController_LoginWithSmartCardReader(string strVal)
        {
            LogInfo.WriteLaneEvent("+ Found SmartCard Reader Data (Login) : " + strVal);
            Application.Invoke(delegate
            {
                if (TCT_Lib.bMaintenanceMode)
                {
                    if (bLogined)
                        return;
                }

                if (bLogined)
                {
                    LogInfo.WriteLaneEvent("- Found Duplicate Login Smart Card : " + strVal);
                    bLogined = false;
                    return;
                }

                //--------------------------------------------------------------------------------------------------------------
                //--- Check duplication
                String szStaffID = TCT_Lib.LaneDB.GetStaffIdFromSmartCard(strVal);
                if (TCT_Lib.LaneDB.CheckDuplicateStaffShift(szStaffID))
                {
                    LogInfo.WriteLaneEvent("- Found Duplicate Login Smart Card in DB : " + strVal);
                    bLogined = false;
                    return;
                }
                //--------------------------------------------------------------------------------------------------------------

                bLogined = true;
                //--- Process check StaffCode
                LogInfo.WriteLaneEvent("+ Process Login SmartCard Reader Data : " + strVal);
                szUserCode = strVal;
                szUserRole = "";

                areaUserCode.QueueDraw();
                areaUserName.QueueDraw();
                areaUserRole.QueueDraw();

                Action_LoginProcess(LoginType.SmartCard, strVal);
            });
        }

        void LaneController_StartManualStaffLogin()
        {
            Application.Invoke(delegate
            {
                szTxtStatus = szTxt_ManualLogin;
                clTxtStatus = new Gdk.Color(10, 110, 170);
                areaStatus.ModifyBg(StateType.Normal, clTxtStatus);
                areaStatus.QueueDraw();

                szUserName = "";
                szUserCode = "";
                szUserRole = "";
                areaUserCode.QueueDraw();
                areaUserName.QueueDraw();
                areaUserRole.QueueDraw();

                edtUserCode.Text = edtUserPassword.Text = "";
                frameManualLogin.Visible = true;
                edtUserCode.GrabFocus();
            });
        }

        void LaneController_CancelManualStaffLogin()
        {
            Application.Invoke(delegate
            {
                edtUserCode.Text = edtUserPassword.Text = "";
                frameManualLogin.Visible = false;
            });
        }

        void LaneController_ManualKeyStaffCode(string szStaffCode)
        {
            Application.Invoke(delegate
            {
                edtUserCode.Text = szStaffCode;
            });
        }

        void LaneController_ManualKeyStaffPassword(string szStaffPassword)
        {
            Application.Invoke(delegate
            {
                szUserName = "";
                szUserCode = "";
                szUserRole = "";
                areaUserCode.QueueDraw();
                areaUserName.QueueDraw();
                areaUserRole.QueueDraw();

                szTxtStatus = szTxt_ManualLogin;
                clTxtStatus = new Gdk.Color(10, 110, 170);
                areaStatus.ModifyBg(StateType.Normal, clTxtStatus);
                areaStatus.QueueDraw();
            });
        }

        void LaneController_ManualStaffLoginStatus(bool bResult, string szErrMsg)
        {
            Application.Invoke(delegate
            {
                if (bResult)
                {
                    //--- Manual Login Success
                    szUserName = "";
                    szUserCode = "";
                    szUserRole = "";

                    Action_LoginProcess(LoginType.Manual);
                }
                else
                {
                    szTxtStatus = szErrMsg;
                    clTxtStatus = new Gdk.Color(255, 0, 0);
                    areaStatus.ModifyBg(StateType.Normal, clTxtStatus);
                    areaStatus.QueueDraw();
                }
            });
        }

        void LaneController_ChangeCurrentLaneType()
        {
            Application.Invoke(delegate
            {
                txtLaneType.Text = TCT_Lib.GetCurLaneTypeName();
                txtLaneType.ModifyFg(StateType.Normal, TCT_Lib.clLabelHeader);

                TCT_Lib.bAllowETC = TCT_Lib.iLaneConfigType > 0;
                if ((TCT_Lib.iLaneConfigType == 2) && (TCT_Lib.eCurLaneType == LaneType.MTC))
                    TCT_Lib.bAllowETC = false;

                if (TCT_Lib.eCurLaneType == LaneType.ETC)
                {
                    //--- Auto Login with Supervisor
                    Action_LoginProcess(LoginType.ETC_Auto);
                }
            });
        }

        void LaneController_RemoteLoginFromRTO(string staffCode)
        {
            Application.Invoke(delegate
            {
                Action_LoginProcess(LoginType.RTO_Remote, staffCode);
            });
        }

        //---------------------------------------------------------------------------
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

        protected void OnBtnKeyZeroClicked(object sender, EventArgs e)
        {
            Action_Key("0");
        }

        protected void OnBtnKeyDELClicked(object sender, EventArgs e)
        {
            if (iFocus == 0)
            {
                if (edtUserCode.Text == "")
                    return;
                edtUserCode.Text = edtUserCode.Text.Remove(edtUserCode.Text.Length - 1);
            }
            else
            {
                if (edtUserPassword.Text == "")
                    return;
                edtUserPassword.Text = edtUserPassword.Text.Remove(edtUserPassword.Text.Length - 1);
            }
        }

        protected void OnBtnKeyENTERClicked(object sender, EventArgs e)
        {
            if (iFocus == 0)
            {
                iFocus = 1;
                edtUserPassword.GrabFocus();
            }
            else
                Action_LoginProcess(LoginType.Manual);
        }

        protected void OnEdtUserCodeFocusInEvent(object o, FocusInEventArgs args)
        {
            iFocus = 0;
        }

        protected void OnEdtUserPasswordFocusInEvent(object o, FocusInEventArgs args)
        {
            iFocus = 1;
        }

        protected void Action_Key(String szChar)
        {
            if (iFocus == 0)
                edtUserCode.Text += szChar;
            else
                edtUserPassword.Text += szChar;
        }

        void LaneController_UpdateUiSupervisor()
        {
            Application.Invoke(delegate
            {
                txtSupervisorName.Text = TCT_Lib.szSupervisorName;
                txtSupervisorName.ModifyFg(StateType.Normal, TCT_Lib.clLabelHeader);

                if (TCT_Lib.bAllowETC)
                    TCT_Lib.etcRabbitMQ.SendChangeLoginSupervisorStaffToETC();
            });
        }

        void LaneController_ManualLoginKey(int iKey)
        {
            Application.Invoke(delegate
            {
                if (iKey == (int)KeyPressedType.MANUAL)
                {
                    szUserName = "";
                    szUserCode = "";
                    szUserRole = "";
                    areaUserCode.QueueDraw();
                    areaUserName.QueueDraw();
                    areaUserRole.QueueDraw();

                    szTxtStatus     = szTxt_ManualLogin;
                    clTxtStatus     = new Gdk.Color(10, 110, 170);
                    areaStatus.ModifyBg(StateType.Normal, clTxtStatus);
                    areaStatus.QueueDraw();
                    edtUserCode.Text = edtUserPassword.Text = "";

                    if (!frameManualLogin.Visible)
                    {
                        //--- Open Maunal Login
                        frameManualLogin.Visible = frameNumKey.Visible = true;
                        edtUserCode.GrabFocus();
                    }
                    else
                    {
                        //--- Close Manual Login
                        frameManualLogin.Visible = frameNumKey.Visible = false;
                        btnManualLogin.GrabFocus();
                    }
                }
                else if ((iKey >= (int)KeyPressedType.NUM_0) && (iKey <= (int)KeyPressedType.NUM_9))
                {
                    if (iFocus == 0)
                        edtUserCode.Text += (iKey - (int)KeyPressedType.NUM_0).ToString();
                    else
                        edtUserPassword.Text += (iKey - (int)KeyPressedType.NUM_0).ToString();
                }
                else if (iKey == (int)KeyPressedType.ENTER)
                {
                    if (iFocus == 0)
                    {
                        iFocus = 1;
                        edtUserPassword.GrabFocus();
                    }
                    else
                        Action_LoginProcess(LoginType.Manual);
                }
                else if (iKey == (int)KeyPressedType.BACKSPACE)
                {
                    if (iFocus == 0)
                    {
                        if (edtUserCode.Text == "")
                            return;
                        edtUserCode.Text = edtUserCode.Text.Remove(edtUserCode.Text.Length - 1);
                    }
                    else
                    {
                        if (edtUserPassword.Text == "")
                            return;
                        edtUserPassword.Text = edtUserPassword.Text.Remove(edtUserPassword.Text.Length - 1);
                    }
                }
                else if (iKey == (int)KeyPressedType.ESC)           // CANCEL
                {
                    if (iFocus == 0)
                        edtUserCode.Text = "";
                    else
                        edtUserPassword.Text = "";
                }
                else if (iKey == (int)KeyPressedType.EDIT)
                {
                    ChangeLoginSmartCard();
                }
                else if (iKey == (int)KeyPressedType.MODE)
                {
                    Action_ProcessChangeLaneMode();
                }
/*
                else if (iKey == (int)KeyPressedType.FORCE_RESET)   //Close Application
                {
                    LogInfo.WriteLog("+ Login => Force Reset for Close Application");
                    TCT_Lib.laneController.SendLaneCloseApp();
                    Thread.Sleep(500);
                    //----------------------------------------------------
                    this.Dispose();
                    this.Destroy();
                    Application.Quit();
                    Environment.Exit(1);
                }
*/
            });
        }

        protected void OnShown(object sender, EventArgs e)
        {
            if (mainWnd != null)
            {
                mainWnd.Dispose();
                mainWnd.Destroy();
                mainWnd = null;
            }

            if (maintenanceWnd != null)
            {
                maintenanceWnd.Dispose();
                maintenanceWnd.Destroy();
                maintenanceWnd = null;
                //----------------------------------------------------
                if (TCT_Lib.bAllowETC)
                {
                    TCT_Lib.etcRabbitMQ.Send_SetLaneMode_ToETC(LaneMode.CLOSED);
                    //TCT_Lib.etcRabbitMQ.Send_ControlOutputDevice_ToETC("OverheadTrafficLight", true);
                    TCT_Lib.etcRabbitMQ.Send_ControlOutputDevice_ToETC("BalanceLight", false);
                }
            }
            TCT_Lib.bAllowSendRTO = true;

            if (iTimerId1Sec == 0)
                iTimerId1Sec = GLib.Timeout.Add(1000, new GLib.TimeoutHandler(Update_Clock));
            if (iTimerIdRTO == 0)
                iTimerIdRTO = GLib.Timeout.Add(5000, new GLib.TimeoutHandler(SendLaneCurrentState));

/*
            if (iTimerIdAutoSendRTO == 0)
                iTimerIdAutoSendRTO = GLib.Timeout.Add(1000 * 60 * 3, new GLib.TimeoutHandler(AutoSendDbRTO));
            if (iTimerIdAutoSendSupAdjust == 0)
                iTimerIdAutoSendSupAdjust = GLib.Timeout.Add(1000 * 60 * 3, new GLib.TimeoutHandler(AutoSendDbSupAdjust));
            if (iTimerIdAutoSendDC == 0)
                iTimerIdAutoSendDC = GLib.Timeout.Add(1000 * 60 * 6, new GLib.TimeoutHandler(AutoSendDbRabbitMQ));
*/

            TCT_Lib.laneController.SendLaneCurrentState();
            //------------------------------------------------------------------------------
            TCT_Lib.eCurAppPage = AppPage.Login;
            iFocus              = 0;
            frameManualLogin.Visible = frameNumKey.Visible = false;
            this.Activate();
            szUserName = szUserCode = szUserRole = "";
            edtUserCode.Text = edtUserPassword.Text = "";
            txtJobNo.Text = TCT_Lib.LaneDB.GetStartJobNo().ToString("0");

            areaUserCode.QueueDraw();
            areaUserName.QueueDraw();
            areaUserRole.QueueDraw();

            szTxtStatus = szTxt_StartLogin;
            clTxtStatus = new Gdk.Color(10, 110, 170);
            areaStatus.ModifyBg(StateType.Normal, clTxtStatus);
            areaStatus.QueueDraw();

            TCT_Lib.webSocketLane.OutIo_ClearCRS();
            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.CloseLane, (int)TFI_Pattern.Normal, "", "", "", szTFI_CloseLane, "");

            TCT_Lib.LaneDB.GetCurrentSupervisor();
            Update_PanelTitle();

            //------------------------------------------
            //--- Fix bug Version 1.4.8 (Restart when change from ETC -> MTC Mode
            //TCT_Lib.ClearDatabaseFile();

            if (TCT_Lib.iVideoNVR_CaptureImageMethod > 0)
                TCT_NVR.AutoClearFolderCaptureImage();
        }

        protected void OnKeyPressEvent(object o, KeyPressEventArgs args)
        {
            if (TCT_Lib.keyMapping.ContainsKey((int)args.Event.KeyValue))
                TCT_Lib.webSocketLane.SendVirtualKeyboard(TCT_Lib.keyMapping[(int)args.Event.KeyValue], 1);
        }

        protected void OnEdtUserCodeKeyPressEvent(object o, KeyPressEventArgs args)
        {
            if (TCT_Lib.keyMapping.ContainsKey((int)args.Event.KeyValue))
                TCT_Lib.webSocketLane.SendVirtualKeyboard(TCT_Lib.keyMapping[(int)args.Event.KeyValue]);
        }

        protected void OnEdtUserPasswordKeyPressEvent(object o, KeyPressEventArgs args)
        {
            if (TCT_Lib.keyMapping.ContainsKey((int)args.Event.KeyValue))
                TCT_Lib.webSocketLane.SendVirtualKeyboard(TCT_Lib.keyMapping[(int)args.Event.KeyValue]);
        }

        protected void OnEdtUserCodeActivated(object sender, EventArgs e)
        {
            TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.ENTER);
        }

        protected void OnEdtUserPasswordActivated(object sender, EventArgs e)
        {
            TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.ENTER);
        }

        void LaneController_SetLaneTypeMode()
        {
            Application.Invoke(delegate
            {

            });
        }

        protected void LaneController_CheckNetworkStatus(int iDeviceType, bool bIsConnected)
        {
            Application.Invoke(delegate
            {
                if (TCT_Lib.eCurAppPage != AppPage.Login)
                    return;

                if (TCT_Lib.bMaintenanceMode)
                    return;

                if (iDeviceType == (int)ChkDeviceType.Device_LC)
                {
                    if (bIsConnected)
                        sbLaneSocketStatus.Text = "<span fgcolor='#00ff00'>Lane Control : Connect</span>";
                    else
                        sbLaneSocketStatus.Text = "<span fgcolor='#FF5500'>Lane Control : DisConnect</span>";
                    sbLaneSocketStatus.UseMarkup = true;
                }
                else if (iDeviceType == (int)ChkDeviceType.Device_RTO)
                {
                    if (bIsConnected)
                        sbRtoStatus.Text = "<span fgcolor='#00ff00'>RTO : Connect</span>";
                    else
                        sbRtoStatus.Text = "<span fgcolor='#FF5500'>RTO : DisConnect</span>";
                    sbRtoStatus.UseMarkup   = true;
                    TCT_Lib.bRTO_Connected  = bIsConnected;

                }
                else if (iDeviceType == (int)ChkDeviceType.Device_EMV)
                {
                    if (! TCT_Lib.bEnableEdc)
                        sbEdcStatus.Text = "<span fgcolor='#FFFF00'>EDC : Disable</span>";
                    else
                    {
                        if (bIsConnected)
                            sbEdcStatus.Text = "<span fgcolor='#00ff00'>EDC : Connect</span>";
                        else
                            sbEdcStatus.Text = "<span fgcolor='#FF5500'>EDC : DisConnect</span>";
                    }
                    sbEdcStatus.UseMarkup = true;
                }
                else if (iDeviceType == (int)ChkDeviceType.Device_DC)
                {
                    if (TCT_Lib.bEnableDcRabbitMQ)
                    //    sbDcStatus.Text = "<span fgcolor='#FFFF00'>DC : Connect</span>";
                    //else
                    {
                        if (bIsConnected)
                        {
                            TCT_Lib.bDC_NetworkStatus   = true;
                            sbDcStatus.Text             = "<span fgcolor='#00ff00'>DC : Connect</span>";
                        }
                        else
                        {
                            TCT_Lib.bDC_NetworkStatus   = false;
                            sbDcStatus.Text             = "<span fgcolor='#FF5500'>DC : DisConnect</span>";
                        }
                    }
                    sbDcStatus.UseMarkup    = true;
                    TCT_Lib.bDC_Connected   = bIsConnected;
                }
            });
        }

        //------------------------------------------------------------------------------
        void LaneSocketServerRTO_EtcChangeLaneMode(int iEtcLaneMode, int iEtcLaneState)
        {
            Application.Invoke(delegate
            {
                if (TCT_Lib.eCurAppPage != AppPage.Login)
                {
                    LogInfo.WriteLog("- Fail ETC Change Lane Mode : Current AppPage is not Login Page");
                    return;
                }
                //----------------------------------------------------------
                if (iEtcLaneMode == (int)LaneMode.NORMAL)
                {
                    Action_LoginProcess(LoginType.ETC_Auto);
                    //TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.OTL_RedGreen, 1);
                    TCT_Lib.etcRabbitMQ.Send_SetLaneMode_ToETC(LaneMode.CLOSED);
                    TCT_Lib.etcRabbitMQ.Send_ControlOutputDevice_ToETC("OverheadTrafficLight", true);
                    //TCT_Lib.etcRabbitMQ.Send_ControlOutputDevice_ToETC("BalanceLight", false);
                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.StartSceen, (int)TFI_Pattern.Normal, "", "", "", "", "");
                    TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.OTL, 1);
                }
                else if ((iEtcLaneMode == (int)LaneMode.MAINTENANCE) && (iEtcLaneState == 1))
                {
                    TCT_Lib.szStaffID           = TCT_Lib.szSupervisorID;
                    TCT_Lib.szStaffCode         = TCT_Lib.szSupervisorCode;
                    TCT_Lib.szStaffName         = TCT_Lib.szSupervisorName;
                    TCT_Lib.szStaffGroupID      = TCT_Lib.szSupervisorGroupID;
                    TCT_Lib.szStaffGroupName    = TCT_Lib.szSupervisorGroupName;

                    this.Hide();

                    GLib.Source.Remove(iTimerId1Sec);
                    GLib.Source.Remove(iTimerIdRTO);
                    GLib.Source.Remove(iTimerIdAutoSendRTO);
                    GLib.Source.Remove(iTimerIdAutoSendSupAdjust);
                    GLib.Source.Remove(iTimerIdAutoSendDC);
                    GLib.Source.Remove(iTimerIdSendScpTFI);
                    Gdk.Color colBk     = new Gdk.Color(0x2E, 0x2E, 0x2E);
                    iTimerId1Sec        = iTimerIdRTO = 0;
                    iTimerIdAutoSendRTO = iTimerIdAutoSendSupAdjust = iTimerIdAutoSendDC = iTimerIdSendScpTFI = 0;

                    this.DeleteEvent    -= new DeleteEventHandler(OnLoginWndDelete);

                    if (TCT_Lib.laneController != null)
                        TCT_Lib.laneController.ClearAndInitialVar();
                    bool bMaintenanceStaff = true;
                    if (bMaintenanceStaff)
                    {
                        //--- Maintenance Mode
                        TCT_Lib.etcRabbitMQ.Send_SetLaneMode_ToETC(LaneMode.MAINTENANCE);
                        TCT_Lib.bMaintenanceMode = true;
                        maintenanceWnd  = new MainMaintenance();
                        maintenanceWnd.ModifyBg(StateType.Normal, colBk);
                        var screen      = maintenanceWnd.GdkWindow.Screen;
                        maintenanceWnd.GdkWindow.GetSize(out var winWidth, out var winHeight);
                        maintenanceWnd.Move((screen.Width / 2) - (winWidth / 2), (screen.Height / 2) - (winHeight / 2));

                        //maintenanceWnd.SetPosition(WindowPosition.Center);
                        maintenanceWnd.Show();
                        maintenanceWnd.HasFocus = true;
                        maintenanceWnd.Activate();
#if __FULL_SCREEN__
                        maintenanceWnd.Fullscreen();
#endif
                    }
                }
            });
        }

        protected void OnBtnChangeLaneModeClicked(object sender, EventArgs e)
        {
            Action_ProcessChangeLaneMode();
        }

        private void Action_ProcessChangeLaneMode()
        {
            var frmSetLaneTypeMode  = new SetLaneTypeMode();
            Gdk.Color colBk         = new Gdk.Color(0x3E, 0x3E, 0x3E);
            frmSetLaneTypeMode.ModifyBg(StateType.Normal, colBk);
            frmSetLaneTypeMode.Run();
            frmSetLaneTypeMode.Dispose();
            int iSelLaneTypeMode    =  frmSetLaneTypeMode.iOutSelLaneTypeMode;
            //Console.WriteLine("+ After Dispose");
            TCT_Lib.eCurAppPage     = AppPage.Login;
            frmSetLaneTypeMode.Dispose();
            frmSetLaneTypeMode.Destroy();

            LaneType eOldLaneType = TCT_Lib.eCurLaneType;
            //----------------------------------------------
            if (iSelLaneTypeMode > 0)
            {
                //--- Process Change LaneTypeMode
                if (iSelLaneTypeMode ==1)
                {
                    TCT_Lib.eCurLaneType = LaneType.MTC;
                }
                else if (iSelLaneTypeMode == 2)
                {
                    TCT_Lib.eCurLaneType = LaneType.ETC;
                }
                else if (iSelLaneTypeMode == 3)
                {
                    TCT_Lib.eCurLaneType = LaneType.DUAL;
                }
                TCT_Lib.WriteTCT_Configuration("Lane", "LaneType", iSelLaneTypeMode-1);
                LaneController_ChangeCurrentLaneType();
                TCT_Lib.laneController.SendLaneCurrentState();

                //----------------
                int iAlarmID = (int)TCT_Lib.LaneDB.WriteLaneAlarmRTO(AlarmDevice.CHANGE_LANETYPE, AlarmType.Occurred, $"Change Lane Type (TCT). From {TCT_Lib.GetLaneTypeName(eOldLaneType)} => To {TCT_Lib.GetLaneTypeName(TCT_Lib.eCurLaneType)}", $"Change from TCT");
                TCT_Lib.SendLaneAlarmObjToRTO(iAlarmID);
            }
        }

        //========================================================================================================
        protected void InitialLSD_WorkingThread()
        {
            lsdWorkingThread                = new Thread(new ThreadStart(ThreadLSD_WorkingTask));
            lsdWorkingThread.Priority       = ThreadPriority.Normal;
            lsdWorkingThread.IsBackground   = true;
            lsdWorkingThread.Start();
        }

        async private void ThreadLSD_WorkingTask()
        {
            TCT_Lib.eCurLsdMode     = LsdMode.LSD_CLOSE;
            TCT_Lib.lsdController   = new LaneSocketControl_LSD();

            await Task.Delay(2000);
            await TCT_Lib.lsdController.RunInitialLSD();

            while (true)
            {
                //Thread.Sleep(1000);
                await Task.Delay(60 * 1000);
            }
        }

    }
}
