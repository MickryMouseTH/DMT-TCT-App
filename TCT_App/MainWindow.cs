#define __FULL_SCREEN__

using System;
using Gtk;
using Pango;
using System.Timers;
using System.Threading;
using System.Reflection;
using TCT_App;
using TCT_App.LaneView.MiscDlg;
using TCT_App.EMV.V0;
using TCT_App.WebServiceAPI;
using Newtonsoft.Json;
using System.IO;
using TCT_App.EMV.EMV_V1;

public partial class MainWindow : Gtk.Window
{
    bool bClassType         = false;
    string szClassType      = "";

    bool bClassAmount       = false;
    string szClassAmount    = "";
    bool bPayType           = false;
    string szPayType        = "";

    bool bProcess           = false;
    string szProcess        = "";

    bool bMis               = false;
    bool bSuspend           = false;
    bool bViolation         = false;
    bool bHPMC              = false;
    bool bTowing            = false;

    bool bIsMLB_Opened      = false;
    uint iHPMC_TimerId      = 0;
    uint iTimerId1Sec       = 0;
    uint iTimerIdInit       = 0;
    uint iTimerIdShowAVC    = 0;
    uint iTimerIdAlbMode    = 0;

    int iAlbMode            = 0;
    LoginMethod loginMethod = LoginMethod.AUTO;
    LoginType loginType     = LoginType.Manual;

    Gdk.Pixbuf imgMLB_Open, imgMLB_Close;       // Manaul Lane Barrier
    Gdk.Pixbuf imgALB_Open, imgALB_Close;       // AUtomatic Lane Barrier
    Gdk.Pixbuf imgOTL_Green, imgOTL_Red;        // Overhead Traffic Light
    Gdk.Pixbuf imgLTL_Green, imgLTL_Red;        // Lane Traffic Light
    Gdk.Pixbuf imgVehicleType1, imgVehicleType2;
    Gdk.Pixbuf imgVehicleTowing, imgVehicleEmergency;
    Gdk.Pixbuf imgVehicleDummyTowing, imgVehicleDummyTowing2, imgVehicleDummyTowing1Pass;
    Gdk.Pixbuf imgVehicleExempt, imgVehicleHPMC, imgVehicleAllowed;
    Gdk.Pixbuf imgTagEasyPass, imgTagMPass, imgCreditCard, imgMobileQR;

    //----------------------------------------------------
    Gdk.Color clBlue    = new Gdk.Color(10, 110, 170);
    Gdk.Color clGreen   = new Gdk.Color(0x00, 0xb3, 0x3c);
    Gdk.Color clRed     = new Gdk.Color(0xff, 0x00, 0x00);
    Gdk.Color clYellow  = new Gdk.Color(0xff, 0xff, 0x00);
    Gdk.Color clBk      = new Gdk.Color(0x2E, 0x2E, 0x2E);
    Gdk.Color clDim     = new Gdk.Color(0x44, 0x44, 0x44);
    //----------------------------------------------------
    //--- Digital Output from LC
    bool bRegisterBell, bSiren, bASB, bLTL, bALB_Open, bALB_Close, bOTL_Status, bOTL_Mode, bOTL_DualMode, bCRS_Reset, bTFI_Reset, bLoop_Reset, bIPC_Reset;

    //--- Digital Input from LC
    bool bInMLB, bInEFS, bInExitLoop, bInEntryLoop, bInExitOB, bInEntryOB, bInALB, bInOTL, bInLTL, bInASB, bInSiren, bInTreadleError, bInTreadleC, bInTreadleD;
    bool bCurEntryLoop, bCurEntryOB, bCurExitLoop, bCurExitOB;

    //--- Const String
    const string szVehicle_Type1        = "รถประเภท 1";
    const string szVehicle_Type2        = "รถประเภท 2";
    const string szVehicle_Exempt       = "รถยกเว้น";
    const string szVehicle_Towing       = "รถลากจูง";
    const string szVehicle_Emergency    = "รถฉุกเฉิน";
    const string szVehicle_HPMC         = "รถขบวน";
    const string szVehicle_Allowed      = "รถ DMT";
    const string szVehicle_Violation    = "รถฝ่าด่าน";
    const string szVehicle_LeftExit     = "รถออกซ้าย";
    const string szPay_Cash             = "เงินสด";
    const string szPay_Coupon           = "คูปอง";
    const string szPay_EMV              = "EMV";
    const string szPay_QrCode           = "QR Code";
    const string szPay_Exempt           = "ยกเว้น";
    const string szPay_DmtCard          = "DMT";
    const string szPay_LeftExit         = "รถออกซ้าย";
    const string szPay_EasyPass         = "Easy Pass";
    const string szPay_MPass            = "M-PASS";
    const string szTxt_CashCoupon       = "กด 'เงินสด' หรือ 'คูปอง'";
    const string szTxt_ConfirmEdit      = "กดปุ่ม 'ยืนยัน' หรือ 'แก้ไข'";
    const string szTxt_Processing       = "กำลังดำเนินการ...";
    const string szTxt_PleaseEnterClassType = "กรุณากดประเภทรถ";
    const string szTxt_CloseShift       = "กด 'ปิดกะ'";
    const string szTxt_OpenMLB          = "กรุณาเปิดไม้กั้นหน้า เพื่อเปิดช่องทาง";
    const string szTxt_OpenOTL          = "กดไฟเขียว เพื่อเปิด OTL";
    const string szTxt_CloseOTL         = "กด 'ปิดกะ' หรือ กด ไฟเขียว เปิด OTL";
    const string szTxt_EndHPMC          = "กดปุ่ม 'รถขบวน' หลังรถคันสุดท้าย";
    const string szTxt_EditType         = "ถ้าต้องการแก้ไข กดปุ่ม 'แก้ไข'";
    const string szTxt_TowingFirstClass = "กรุณากดประเภทรถลากจูงคันแรก";
    const string szTxt_TowingFirstPay   = "กรุณากดประเภทการจ่ายเงินรถคันแรก";
    const string szTxt_TowingSecClass   = "กรุณากดประเภทรถคันที่สอง";
    const string szTxt_TowingSecPay     = "กรุณากดประเภทการจ่ายเงินรถคันที่สอง";
      
    string szAmount_Class1      = TCT_Lib.fFareClass1.ToString("0") + " บาท";
    string szAmount_Class2      = TCT_Lib.fFareClass2.ToString("0") + " บาท";
    string szAmount_0           = "0 บาท";

    string[] szListLaneState;
    int idxLaneState        = 0;
    string[] szListCompilePassage;
    int idxCompilePassage   = 0;
    //--------------------------------------------------------------------------------
    public MainWindow(LoginMethod _loginMethod, LoginType _loginType) : base(Gtk.WindowType.Toplevel)
    {
        Build();

        this.Activate();

#if __FULL_SCREEN__
        this.Fullscreen();
#endif
        TCT_Lib.eCurAppPage     = AppPage.Main;
        loginMethod             = _loginMethod;
        loginType               = _loginType;

        txtAlbMode.Visible = false;
        if (TCT_Lib.iLaneConfigType == 0)
            txtAlbMode.Visible = true;

        if (TCT_Lib.iLaneConfigType == 2)   // --- Dual Mode
        {
            TCT_Lib.bAllowETC = (int)TCT_Lib.eCurLaneType > 0;
            //if (!TCT_Lib.bAllowETC)
            //--- Close this when 2012-12-21 (Fix Send Lane Close of MTC)
            //TCT_Lib.bEnableEtcRabbitMQ = TCT_Lib.eCurLaneType > 0;
            txtAlbMode.Visible = true;
        }

        Update_Clock();
        LoadImageResource();

        iTimerId1Sec                            = GLib.Timeout.Add(1000, new GLib.TimeoutHandler(Update_Clock));
        sbAppVersion.Text                       = $"<span fgcolor='#ffffff'>Version : {TCT_Lib.szAppVersion}</span>";
        sbAppVersion.UseMarkup                  = true;

        TCT_Lib.laneController.bHasViolation    = false;
        TCT_Lib.nTrx                            = 1;
        TCT_Lib.laneController.iCapture_nTrx    = 0;
        // Start transaction of each JobNo
        // if ETC mode then JobNo will fix=0 (2021/03/17)
        TCT_Lib.iStaffShiftID                   = (int)TCT_Lib.LaneDB.GetStaffShiftID();
        TCT_Lib.iJobNo                          = (TCT_Lib.eCurLaneType == LaneType.ETC) ? 0 : TCT_Lib.LaneDB.GetStartJobNo();
        LogInfo.WriteLaneEvent($"+ Start Main Process => StartShiftID={TCT_Lib.iStaffShiftID}, JobNo={TCT_Lib.iJobNo}, nTrx={TCT_Lib.nTrx}");
        TCT_Lib.dtStaffStartShift               = DateTime.Now;
        TCT_Lib.LaneDB.WriteLaneStartStaffShift(loginType);
        TCT_Lib.laneController.StartShiftProcess();
        TCT_Lib.LaneDB.WriteNewTransactionStatistic();

        //---------------------------------------------------------------------------------------------
        //if (TCT_Lib.bEnableEdc)
        {
            //var xEdcSumReport = new EdcSumReportLib();

        }
        //---------------------------------------------------------
/*
        if (TCT_Lib.bEnableEdc)
        {
            if (!TCT_Lib.bEdcProcessing)
            {
                if (TCT_Lib.iEdcVersionType == 1)
                {
                    var xEdcHandshaking_V1 = new EdcHandShaking_V1();
                }
            }
            else
                LogInfo.WriteLogEDC("- EDC is processing, then ignore HandshakingProcess.");
        }
*/        
        //---------------------------------------------------------------------------------------------

        TCT_Lib.LaneDB.GetCurrentSupervisor();
        InitPanelUI();
        ClearAllPanels();

        this.DeleteEvent        += new DeleteEventHandler(OnMainWndDelete);
        //------------------------------------------------
        TCT_Lib.laneController.digitalInputLaneControl      += LaneController_DigitalInputLaneControl;
        TCT_Lib.laneController.digitalOutputLaneControl     += LaneController_DigitalOutputLaneControl;
        TCT_Lib.laneController.vehicleTypeLaneControl       += LaneController_VehicleTypeLaneControl;
        TCT_Lib.laneController.payTypeLaneControl           += LaneController_PayTypeLaneControl;
        TCT_Lib.laneController.showAvcLaneControl           += LaneController_ShowAvcLaneControl;
        TCT_Lib.laneController.showQueueAlbControl          += LaneController_ShowQueueAlbControl;
        TCT_Lib.laneController.show_nTrxControl             += LaneController_Show_nTrxControl;
        TCT_Lib.laneController.eventViolation               += LaneController_EventViolation;
        TCT_Lib.laneController.eventDIS                     += LaneController_EventDIS;
        TCT_Lib.laneController.clearPanelUiLaneControl      += LaneController_ClearPanelUiLaneControl;
        TCT_Lib.laneController.clearQueueLaneControl        += LaneController_ClearQueueLaneControl;
        TCT_Lib.laneController.updateUiQueueLaneControl     += LaneController_UpdateUiQueueLaneControl;
        TCT_Lib.laneController.editBackClassType            += LaneController_EditBackClassType;
        TCT_Lib.laneController.editBackPayType              += LaneController_EditBackPayType;
        TCT_Lib.laneController.endShiftLaneControl          += LaneController_EndShiftLaneControl;
        TCT_Lib.laneController.updateUiSupervisor           += LaneController_UpdateUiSupervisor;
        TCT_Lib.laneController.updateUiStaff                += LaneController_UpdateUiStaff;
        TCT_Lib.laneController.emvCreditPaymentUI           += LaneController_EmvCreditPaymentUI;
        TCT_Lib.laneController.emvQrCodePaymentUI           += LaneController_EmvQrCodePaymentUI;
        TCT_Lib.laneController.messageProcessUI             += LaneController_MessageProcessUI;
        TCT_Lib.laneController.showModeType                 += LaneController_ShowModeType;
        TCT_Lib.laneController.salesCoupon                  += LaneController_SalesCoupon;
        TCT_Lib.laneController.printConvoy                  += LaneController_PrintConvoy;
        //TCT_Lib.laneController.dmtPayment                   += LaneController_DmtPayment;
        TCT_Lib.laneController.checkNetworkStatus           += LaneController_CheckNetworkStatus;
        //--- v2.0.2: show the last EDC check result now (next check can be EdcCheckKeepAliveIntervalMin later)
        if (!TCT_Lib.bEnableEdc || (TCT_Lib.iEdcLastStatus >= 0))
            LaneController_CheckNetworkStatus((int)ChkDeviceType.Device_EMV, TCT_Lib.iEdcLastStatus == 1);
        TCT_Lib.laneController.nextVehicleTypeLaneControl   += LaneController_NextVehicleTypeLaneControl;
        TCT_Lib.laneController.laneStateEtcPayCardPAN       += LaneController_LaneStateEtcPayCardPAN;
        TCT_Lib.laneController.compilePassageETC            += LaneController_CompilePassageETC;
        TCT_Lib.laneController.drawETC_AlbMode              += LaneController_DrawETC_AlbMode;
        TCT_Lib.laneController.showTotalLoop                += LaneController_ShowTotalLoop;
        TCT_Lib.laneController.updateUI_TowingFirstPass     += LaneController_UpdateUI_TowingFirstPass;
        TCT_Lib.laneController.updateUI_Convoy              += LaneController_UpdateUI_Convoy;
        TCT_Lib.laneController.updateUI_DmtCardInfo         += LaneController_UpdateUI_DmtCardInfo;
        TCT_Lib.laneController.updateUI_ClearPanelTagInfo   += LaneController_UpdateUI_ClearPanelTagInfo;
        TCT_Lib.laneController.updateUI_ClearQrWarningMsg   += LaneContorller_UpdateUI_ClearQrWarningMsg;

        TCT_Lib.laneController.SendLaneCurrentState();
        TCT_Lib.laneController.SendCmdReadDigitalInputOutput();
        TCT_Lib.laneController.WriteTextCRS("", "");

        if (TCT_Lib.bMaintenanceMode)
        {
            frameUserInfo.ModifyBg(StateType.Normal, clYellow);
            frameLaneImg.ModifyBg(StateType.Normal, clYellow);
            frameLaneStatus.ModifyBg(StateType.Normal, clYellow);
            TCT_Lib.eCurLaneMode = LaneMode.MAINTENANCE;
            //---Force Close OTL and Disable Open/Close OTL button
            //TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.OTL_CLOSE);
            TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.OTL_CLOSE);
            btn_OpenOTL.Sensitive = btn_CloseOTL.Sensitive = false;
        }
        else
        {
            TCT_Lib.eCurLaneMode    = LaneMode.NORMAL;
            btn_OpenOTL.Sensitive   = btn_CloseOTL.Sensitive = true;
        }

        string szErrMsg = "";
        //----------------------------------------------------------------------------------
        TCT_Lib.webSocketLane.SendCollectorJob(1);
        TCT_Lib.webSocketLane.SendChangeSupervisor();
        TCT_Lib.SendLaneBeginJobObjToDC(TCT_Lib.iStaffShiftID);
        TCT_Lib.SendBeginJobObjToRTO(TCT_Lib.iStaffShiftID);
        TCT_Lib.SendBeginJobObjToSupAdjust(TCT_Lib.iStaffShiftID);
        TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.JOB, 1);
        TCT_Lib.webSocketLane.SendCollectorJob(1);                  // Repeat send BOJ to BIS
        WebService_BeginEndJob.CallWebService_BeginOfJob(TCT_Lib.dtStaffStartShift, ref szErrMsg);
        WebService_SalesCoupons.CallWebService_GetCoupons(ref szErrMsg);
        //--------------------------------------------------------------------
        if (TCT_Lib.mocRabbitMQ.connection != null)
            sbDcStatus.Text = "<span fgcolor='#00ff00'>DC : Connect</span>";
        else
            sbDcStatus.Text = "<span fgcolor='#777777'>DC : DisConnect</span>";
        sbDcStatus.UseMarkup = true;

        TCT_Lib.bMainUiReady    = true;

        if (TCT_Lib.bAllowETC)
            TCT_Lib.etcRabbitMQ.SendChangeLoginSupervisorStaffToETC();
        //iTimerIdInit = GLib.Timeout.Add(1000, new GLib.TimeoutHandler(InitialUI));

/*
 *      2021-02-02  Move action to when Open OTL
        if (TCT_Lib.bEnableLSD)
        {
            if (TCT_Lib.eCurLaneType == LaneType.MTC)
                TCT_Lib.lsdController.CmdShow_OpenMTC();
            else if (TCT_Lib.eCurLaneType == LaneType.ETC)
                TCT_Lib.lsdController.CmdShow_OpenETC();
            else if (TCT_Lib.eCurLaneType == LaneType.DUAL)
                TCT_Lib.lsdController.CmdShow_OpenDual();
        }
*/
        //--- ETC Mode will disable keyboard
        keypad.Sensitive = true;
        if (!TCT_Lib.bMaintenanceMode)
        {
            if (TCT_Lib.iLaneConfigType == 1)
                keypad.Sensitive = false;
            if ((TCT_Lib.iLaneConfigType == 2) && (TCT_Lib.eCurLaneType == LaneType.ETC))
                keypad.Sensitive = false;
        }

        //--- 2022-04-27 : Fix not found staff id from login multiple smart card
        if (TCT_Lib.szStaffID.Trim() == "")
            TCT_Lib.LaneDB.SetStaffFromStaffShift(TCT_Lib.iStaffShiftID);

        //----- Check Manual Import Path
        if (TCT_Lib.szManualImportPath.Trim() != "")
        {
            try
            {
                //--- Check and Create Backup Manual File
                var backupPath = System.IO.Path.Combine(TCT_Lib.szManualImportPath.Trim(), "Backup");
                if (!Directory.Exists(backupPath))
                    Directory.CreateDirectory(backupPath);
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Fail Create Backup Manual Import Directory : " + err.Message);
            }

            TCT_Lib.ManualImportDataFile();
        }

        TCT_Lib.LaneDB.CheckUpdateTariffEffective();
        TCT_Lib.LaneDB.LoadTariffData();
        szAmount_Class1 = TCT_Lib.fFareClass1.ToString("0") + " บาท";
        szAmount_Class2 = TCT_Lib.fFareClass2.ToString("0") + " บาท";

        if (TCT_Lib.iPrinter_PrintFooter > 0)
            TCT_Lib.LaneDB.LoadPromotionData();

        TCT_Lib.szListReceiptNo.Clear();    // 2023-01-23   Fix first receipt no.= xxxx-xxxxxxxxxxxx

        //--- Load Custom Payment 06-10



        //------------------------------------------------------------------------
#if !__FULL_SCREEN__

        var dataPath                = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "log/RabbitMQ");
        var dataFileLaneState       = System.IO.Path.Combine(dataPath, "LaneState.log");
        var dataFileCompilePassage  = System.IO.Path.Combine(dataPath, "CompilePassage.log");
        try
        {
            var szMessage   = File.ReadAllText(dataFileLaneState);
            szMessage       = szMessage.Replace("\n", "");
            szMessage       = szMessage.Replace("{\"header\":", "\r{\"header\":");
            szListLaneState = szMessage.Split('\r');

            szMessage = "";
            szMessage = File.ReadAllText(dataFileCompilePassage);
            szMessage = szMessage.Replace("\n", "");
            szMessage = szMessage.Replace("{ \"jsonrpc\":", "\r{ \"jsonrpc\":");
            szListCompilePassage = szMessage.Split('\r');
        }
        catch (Exception err)
        {
            Console.WriteLine(err.Message);
        }


        //            for(int i=0; i<20; i++)
        //            {
        //                int iStart = szMessage.IndexOf("19/11/2020");
        //                int iEnd = szMessage.IndexOf("+ ETC CompilePassage : ");
        //                szMessage = szMessage.Remove(iStart, (iEnd - iStart)+ "+ ETC CompilePassage : ".Length);
        //            }
        //            File.WriteAllText(dataFileCompilePassage, szMessage);

#endif
    }

    protected void OnMainWndDelete(object sender, DeleteEventArgs a)
    {
        //TCT_Lib.laneController.SendLaneCloseApp();
        //----------------------------------------------------
        Application.Quit();
        a.RetVal = true;
    }

    private bool InitialUI()
    {
        //TCT_Lib.webSocketLane.SendRequest_DeviceInput();
        if (iTimerIdInit > 0)
        {
            GLib.Source.Remove(iTimerIdInit);
            iTimerIdInit = 0;
        }
        return true;
    }

    private bool Update_Clock()
    {
        //txtTime.Text = DateTime.Now.ToString("HH:mm:ss");
        //txtDate.Text = DateTime.Today.ToString("dd/MM/yyyy");

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

    protected void LoadImageResource()
    {
        imgMLB_Open         = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.MLB_Open.png");
        imgMLB_Close        = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.MLB_Close.png");

        imgALB_Open         = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.ALB_Open.png");
        imgALB_Close        = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.ALB_Close.png");

        imgOTL_Green        = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.GateOpen.png");
        imgOTL_Red          = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.GateClose.png");

        imgLTL_Green        = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.LightGreen.png");
        imgLTL_Red          = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.LightRed.png");

        imgVehicleType1     = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.VehicleType1.png");
        imgVehicleType2     = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.VehicleType2.png");
        imgVehicleTowing    = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.VehicleTowing.png");
        imgVehicleEmergency = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.VehicleEmergency.png");
        imgVehicleExempt    = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.VehicleExcept.png");
        imgVehicleAllowed   = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.VehicleExcept.png");
        imgVehicleHPMC      = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.VehicleHPMC.png");
        imgVehicleDummyTowing       = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Dummy-Towing.png");
        imgVehicleDummyTowing2      = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Dummy-Towing2.png");
        imgVehicleDummyTowing1Pass  = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Dummy-Towing1Pass.png");

        imgTagEasyPass      = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.TAG_EasyPass.png");
        imgTagMPass         = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.TAG_MPass.png");
        imgCreditCard       = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.CreditCard.png");
        imgMobileQR         = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.MobileQR.png");
    }

    private void ClearAllPanels()
    {
        //--- Clear Panel UI
        imgCarType.Pixbuf       = null;
        imgNextCar.Pixbuf       = null;
        imgMode.Pixbuf          = null;
        imgTag.Pixbuf           = null;
        imgLTL.Pixbuf           = imgLTL_Red;
        imgALB.Pixbuf           = imgALB_Close;

        Fixed.FixedChild pos    = ((global::Gtk.Fixed.FixedChild)(this.fixed2[this.imgCarType]));
        pos.X                   = 80;

        txtClassType.Text       = "";
        txtClassAmount.Text     = "";
        txtQueue.Text           = TCT_Lib.iTotalQueue.ToString("0");

        txtTag_PAN.Text         = "";
        txtTag_Balance.Text     = "";
        txtTag_Status.Text      = "";

        szClassType             = "";
        areaClassType.ModifyBg(StateType.Normal, clBlue);
        areaClassType.QueueDraw();

        szClassAmount           = "";
        areaClassAmount.ModifyBg(StateType.Normal, clBlue);
        areaClassAmount.QueueDraw();

        szPayType               = "";
        areaPayType.ModifyBg(StateType.Normal, clBlue);
        areaPayType.QueueDraw();

        bHPMC                   = false;
        bViolation              = false;
        bMis                    = false;
        bTowing                 = false;
        szProcess               = "";
        areaProcess.ModifyBg(StateType.Normal, clGreen);
        areaProcess.QueueDraw();

        imgMIS.Pixbuf           = null;

        //--- Area Processing
        bProcess    = true;
        szProcess   = bInMLB ? szTxt_PleaseEnterClassType : "";
        areaProcess.ModifyBg(StateType.Normal, clGreen);
        areaProcess.QueueDraw();

        // Area Violation
        areaViolation.Visible = false;

        //Gdk.Pixbuf pictureBuf = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.VehicleType1.png");
        //imgNextCar.Pixbuf = pictureBuf.ScaleSimple(150, 108, Gdk.InterpType.Bilinear);

        if (iHPMC_TimerId > 0)
            GLib.Source.Remove(iHPMC_TimerId);
        iHPMC_TimerId = 0;
    }

    private void InitPanelUI()
    {
        Assembly myAssembly     = Assembly.GetExecutingAssembly();
        imgLogo.Pixbuf          = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Logo.png");
        imgOTL.Pixbuf           = imgOTL_Red;
        imgLTL.Pixbuf           = imgLTL_Red;
        imgCarType.Pixbuf       = imgVehicleType1;
        imgALB.Pixbuf           = imgALB_Close;
        imgMIS.Pixbuf           = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.MIS.png");
        imgMLB.Pixbuf           = imgMLB_Close;

        InitPanelHeader();
        InitPanelImageLane();
        InitPanelLaneStatus();
        InitPanelActionKey();
    }

    private void InitPanelHeader()
    {
        //table3.ModifyBg(StateType.Normal, new Gdk.Color(128, 128, 128));
        FontDescription fontDesc    = new FontDescription();
        fontDesc.Weight             = Weight.Bold;

        lblPlazaName.ModifyFont(fontDesc);
        lblLaneName.ModifyFont(fontDesc);
        lblLaneMode.ModifyFont(fontDesc);

        lblUserCode.ModifyFont(fontDesc);
        lblUserName.ModifyFont(fontDesc);
        lblUserRole.ModifyFont(fontDesc);

        lblSupervisor.ModifyFont(fontDesc);
        lblStartShift.ModifyFont(fontDesc);
        lblShiftID.ModifyFont(fontDesc);

        txtTime.ModifyFont(FontDescription.FromString("Ubuntu Bold 22"));
        txtTime.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

        //--- v2.0.2: header widths overflowed 1024 (100 + 768 + clock) -> clock "HH:mm:ss" was clipped.
        //--- Narrower plaza/lane block (short values) moves the staff block left; clock box gets room.
        table1.WidthRequest         = UiTheme.HdrTable1;
        table3.WidthRequest         = UiTheme.HdrTable2;
        table5.WidthRequest         = UiTheme.HdrTable3;
        frameUserInfo.WidthRequest  = UiTheme.HdrInfo;
        vbox4.WidthRequest          = 199;        // right edge of clock box = right edge of panels below
        txtTime.Xalign              = 0.5f;
        txtDate.Xalign              = 0.5f;
        //--- v2.0.2: 3 header groups (plaza/lane | staff | shift), labels left-aligned in each group
        UiTheme.StyleHeader(new[] { lblPlazaName, lblLaneName, lblLaneMode },
                            new[] { lblUserCode, lblUserName, lblUserRole },
                            new[] { lblSupervisor, lblStartShift, lblShiftID },
                            table1, table3, table5);
        UiTheme.StyleStatusBar(statusbar);
        UiTheme.FlattenFrames(this);

        txtDate.ModifyFont(FontDescription.FromString("Ubuntu Regular 12"));
        txtDate.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

        txtPlazaName.ModifyFg(StateType.Normal, TCT_Lib.clLabelHeader);
        txtLaneName.ModifyFg(StateType.Normal, TCT_Lib.clLabelHeader);
        txtLaneType.ModifyFg(StateType.Normal, TCT_Lib.clLabelHeader);
        txtStaffID.ModifyFg(StateType.Normal, TCT_Lib.clLabelHeader);
        txtStaffName.ModifyFg(StateType.Normal, TCT_Lib.clLabelHeader);
        txtStaffGroup.ModifyFg(StateType.Normal, TCT_Lib.clLabelHeader);
        txtSupervisorName.ModifyFg(StateType.Normal, TCT_Lib.clLabelHeader);
        txtStartShift.ModifyFg(StateType.Normal, TCT_Lib.clLabelHeader);
        txtJobNo.ModifyFg(StateType.Normal, TCT_Lib.clLabelHeader);
        //-----------------------------------------------------
        txtPlazaName.Text           = TCT_Lib.szPlazaName;
        txtLaneName.Text            = TCT_Lib.szLaneName;
        txtLaneType.Text            = TCT_Lib.GetCurLaneTypeName();

        string szLoginType = "";
        if (loginType == LoginType.Manual)
            szLoginType = "Manual Key";
        else if (loginType == LoginType.SmartCard)
            szLoginType = "Smart Card";
        else if (loginType == LoginType.RTO_Remote)
            szLoginType = "RTO Remote";
        else if (loginType == LoginType.ETC_Auto)
            szLoginType = "Auto ETC";
        else if (loginType == LoginType.Maintenance)
            szLoginType = "Maintenance";

        txtStaffID.Text             = TCT_Lib.szStaffID + "   [ " + szLoginType + " ]";
        txtStaffName.Text           = TCT_Lib.szStaffName;
        txtStaffGroup.Text          = TCT_Lib.szStaffGroupName;
        txtStartShift.Text          = TCT_Lib.dtStaffStartShift.ToString("dd-MM-yyyy HH:mm:ss", DateTimeExtension.ThaiCultureInfo);

        //txtJobNo.Text               = TCT_Lib.LaneDB.GetStartJobNo().ToString("0") + (TCT_Lib.bMaintenanceMode ? "     (Maintenance)" : "");
        txtJobNo.Text               = TCT_Lib.iJobNo.ToString("0") + (TCT_Lib.bMaintenanceMode ? "     (Maintenance)" : "");
        txtSupervisorName.Text      = TCT_Lib.szSupervisorName;
    }

    private void InitPanelImageLane()
    {
        txtClassType.UseMarkup      = true;
        txtClassType.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

        txtClassAmount.UseMarkup    = true;
        txtClassAmount.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

        txtQueue.UseMarkup          = true;
        txtQueue.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

        txtConvoy.UseMarkup         = true;
        txtConvoy.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
    }

    private void InitPanelLaneStatus()
    {
        //--- Area ClassType
        bClassType      = true;
        szClassType     = szVehicle_Type1;
        areaClassType.ModifyBg(StateType.Normal, clBlue);
        areaClassType.QueueDraw();

        //--- Area ClassAmount
        bClassAmount    = true;
        szClassAmount   = szAmount_Class1;
        areaClassAmount.ModifyBg(StateType.Normal, clBlue);
        areaClassAmount.QueueDraw();

        //--- Area PayType
        bPayType        = true;
        szPayType       = szPay_Cash;
        areaPayType.ModifyBg(StateType.Normal, clBlue);
        areaPayType.QueueDraw();

        //--- Area Processing
        bProcess        = true;
        szProcess       = szTxt_Processing;
        areaProcess.ModifyBg(StateType.Normal, clGreen);
        areaProcess.QueueDraw();
    }

    private void InitPanelActionKey()
    {
        btnPay_Cash.Child.ModifyFont(FontDescription.FromString("Arial 14"));
        btnPay_Coupon.Child.ModifyFont(FontDescription.FromString("Arial 14"));
        btnPayType_DmtCard.Child.ModifyFont(FontDescription.FromString("Arial 14"));
        btnPay_EMV.Child.ModifyFont(FontDescription.FromString("Arial 14"));
        btnPay_QRCode.Child.ModifyFont(FontDescription.FromString("Arial 14"));

        btnClassType_1.Child.ModifyFont(FontDescription.FromString("Arial 14"));
        btnClassType_2.Child.ModifyFont(FontDescription.FromString("Arial 14"));
        btn_Confirm.Child.ModifyFont(FontDescription.FromString("Arial 14"));
        btn_Edit.Child.ModifyFont(FontDescription.FromString("Arial 14"));

        btnSalesCoupon.Child.ModifyFont(FontDescription.FromString("Arial 14"));
        btnLeftOut.Child.ModifyFont(FontDescription.FromString("Arial 14"));
        btnPrintConvoy.Child.ModifyFont(FontDescription.FromString("Arial 14"));

        btnClassType_Exempt.Child.ModifyFont(FontDescription.FromString("Arial 14"));
        btnClassType_Towing.Child.ModifyFont(FontDescription.FromString("Arial 14"));
        btnClassType_HPMC.Child.ModifyFont(FontDescription.FromString("Arial 14"));


        btn_Violation.Child.ModifyFont(FontDescription.FromString("Arial 14"));
        btnPrintReceipt.Child.ModifyFont(FontDescription.FromString("Arial 14"));

        btn_ForceReset.Child.ModifyFont(FontDescription.FromString("Arial 14"));
        btn_EndShift.Child.ModifyFont(FontDescription.FromString("Arial 14"));

        Image imgCloseOTL       = new Gtk.Image(Gdk.Pixbuf.LoadFromResource("TCT_App.Images.GateClose.png"));
        btn_CloseOTL.Image      = imgCloseOTL;

        Image imgOpenOTL        = new Gtk.Image(Gdk.Pixbuf.LoadFromResource("TCT_App.Images.GateOpen.png"));
        btn_OpenOTL.Image       = imgOpenOTL;

        //--------- Nerw Payment Button ---------
        btn_Payment6.Label = TCT_Lib.szPaymentBtn_06.Trim();
        btn_Payment7.Label = TCT_Lib.szPaymentBtn_07.Trim();
        btn_Payment8.Label = TCT_Lib.szPaymentBtn_08.Trim();
        btn_Payment9.Label = TCT_Lib.szPaymentBtn_09.Trim();

        btn_Payment6.Child.ModifyFont(FontDescription.FromString("Arial 14"));
        btn_Payment7.Child.ModifyFont(FontDescription.FromString("Arial 14"));
        btn_Payment8.Child.ModifyFont(FontDescription.FromString("Arial 14"));
        btn_Payment9.Child.ModifyFont(FontDescription.FromString("Arial 14"));

        if (TCT_Lib.szPaymentBtn_06.Trim() == "")
        {
            btn_Payment6.Clicked -= OnBtnPayment6Clicked;
            btn_Payment6.Sensitive = false;
        }
        if (TCT_Lib.szPaymentBtn_07.Trim() == "")
        {
            btn_Payment7.Clicked -= OnBtnPayment7Clicked;
            btn_Payment7.Sensitive = false;
        }
        if (TCT_Lib.szPaymentBtn_08.Trim() == "")
        {
            btn_Payment8.Clicked -= OnBtnPayment8Clicked;
            btn_Payment8.Sensitive = false;
        }
        if (TCT_Lib.szPaymentBtn_09.Trim() == "")
        {
            btn_Payment9.Clicked -= OnBtnPayment9Clicked;
            btn_Payment9.Sensitive = false;
        }

        //--- v2.0.2: restyle - button colour = colour of the same key on the Cherry toll keyboard
        UiTheme.StyleKey(btnClassType_1,        UiTheme.Yellow);
        UiTheme.StyleKey(btnClassType_2,        UiTheme.Red);
        UiTheme.StyleKey(btnPay_Cash,           UiTheme.Navy);
        UiTheme.StyleKey(btnPay_Coupon,         UiTheme.Coupon);
        UiTheme.StyleKey(btnPay_EMV,            UiTheme.Pink);
        UiTheme.StyleKey(btnPay_QRCode,         UiTheme.Teal);
        UiTheme.StyleKey(btnPayType_DmtCard,    UiTheme.Purple);
        UiTheme.StyleKey(btn_Confirm,           UiTheme.Pink);
        UiTheme.StyleKey(btn_Edit,              UiTheme.Red);
        UiTheme.StyleKey(btnSalesCoupon,        UiTheme.Yellow);
        UiTheme.StyleKey(btnLeftOut,            UiTheme.Pink);
        btnPrintConvoy.Label = "รถขบวน\nจ่ายเงิน";     // two lines - one line is clipped at 1024x768
        UiTheme.StyleKey(btnPrintConvoy,        UiTheme.Green);
        UiTheme.StyleKey(btnClassType_Exempt,   UiTheme.LightTeal);
        UiTheme.StyleKey(btnClassType_Towing,   UiTheme.Teal);
        UiTheme.StyleKey(btnClassType_HPMC,     UiTheme.Purple);
        UiTheme.StyleKey(btn_Violation,         UiTheme.Orange);
        UiTheme.StyleKey(btnPrintReceipt,       UiTheme.Purple);
        UiTheme.StyleKey(btn_ForceReset,        UiTheme.Yellow);
        UiTheme.StyleKey(btn_EndShift,          UiTheme.Blue);
        UiTheme.StyleKey(btn_Payment6,          UiTheme.White);
        UiTheme.StyleKey(btn_Payment7,          UiTheme.White);
        UiTheme.StyleKey(btn_Payment8,          UiTheme.White);
        UiTheme.StyleKey(btn_Payment9,          UiTheme.White);
        UiTheme.StyleKey(btn_OpenOTL,           UiTheme.White);
        UiTheme.StyleKey(btn_CloseOTL,          UiTheme.White);

    }

    protected void OnAreaClassTypeExposeEvent(object o, ExposeEventArgs args)
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

        FontDescription fontDesc    = FontDescription.FromString("Ubuntu 32");
        layout.FontDescription      = fontDesc;

        if (bClassType)
            layout.SetText(szClassType);
        else
            layout.SetText("");

        Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
        renderer.Drawable           = drawArea.GdkWindow;
        renderer.Gc                 = drawArea.Style.BlackGC;

        renderer.SetOverrideColor(RenderPart.Foreground, new Gdk.Color(255, 255, 255));
        //renderer.DrawLayout(layout, 0, 0);
        drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 10, layout, new Gdk.Color(255, 255, 255), clBlue);

        //renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
        renderer.Drawable           = null;
        renderer.Gc                 = null;
        renderer.Dispose();
        context.Dispose();
    }

    protected void OnAreaClassAmountExposeEvent(object o, ExposeEventArgs args)
    {
        DrawingArea drawArea        = o as DrawingArea;
        //drawArea.ModifyBg(StateType.Normal, clBlue);
        //---  DrawArea ClassType
        int iWidth                  = drawArea.Allocation.Width;
        Context context             = drawArea.CreatePangoContext();
        Pango.Layout layout         = new Pango.Layout(drawArea.PangoContext);
        layout.Width                = Units.FromPixels(iWidth);
        layout.Alignment            = Pango.Alignment.Center;
        layout.Justify              = true;

        FontDescription fontDesc    = FontDescription.FromString("Ubuntu 32");
        layout.FontDescription      = fontDesc;

        if (bClassAmount)
            layout.SetText(szClassAmount);
        else
            layout.SetText("");

        Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
        renderer.Drawable           = drawArea.GdkWindow;
        renderer.Gc                 = drawArea.Style.BlackGC;

        renderer.SetOverrideColor(RenderPart.Foreground, new Gdk.Color(255, 255, 255));
        //renderer.DrawLayout(layout, 0, 0);
        drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 10, layout, new Gdk.Color(255, 255, 255), clBlue);

        //renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
        renderer.Drawable           = null;
        renderer.Gc                 = null;
        renderer.Dispose();
        //layout.Dispose();
        context.Dispose();
    }

    protected void OnAreaPayTypeExposeEvent(object o, ExposeEventArgs args)
    {
        DrawingArea drawArea        = o as DrawingArea;
        //drawArea.ModifyBg(StateType.Normal, clBlue);
        //---  DrawArea ClassType
        int iWidth                  = drawArea.Allocation.Width;
        Context context             = drawArea.CreatePangoContext();
        Pango.Layout layout         = new Pango.Layout(context);
        layout.Width                = Units.FromPixels(iWidth);
        layout.Alignment            = Pango.Alignment.Center;
        layout.Justify              = true;

        FontDescription fontDesc    = FontDescription.FromString("Ubuntu 32");
        layout.FontDescription      = fontDesc;

        if (bPayType)
            layout.SetText(szPayType);
        else
            layout.SetText("");

        Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
        renderer.Drawable           = drawArea.GdkWindow;
        renderer.Gc                 = drawArea.Style.BlackGC;

        renderer.SetOverrideColor(RenderPart.Foreground, new Gdk.Color(255, 255, 255));
        //renderer.DrawLayout(layout, 0, 0);
        drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 10, layout, new Gdk.Color(255, 255, 255), clBlue);

        //renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
        renderer.Drawable           = null;
        renderer.Gc                 = null;
        renderer.Dispose();
        //layout.Dispose();
        context.Dispose();
    }

    protected void OnAreaProcessExposeEvent(object o, ExposeEventArgs args)
    {
        DrawingArea drawArea        = o as DrawingArea;
        //drawArea.ModifyBg(StateType.Normal, bMis ? clRed : clGreen);
        //---  DrawArea ClassType
        int iWidth                  = drawArea.Allocation.Width;
        Context context             = drawArea.CreatePangoContext();
        Pango.Layout layout         = new Pango.Layout(context);
        if (layout != null)
            layout.Width = Units.FromPixels(iWidth);
        layout.Alignment            = Pango.Alignment.Center;
        layout.Justify              = true;

        FontDescription fontDesc    = FontDescription.FromString("Ubuntu Bold 18");
        layout.FontDescription      = fontDesc;

        if (bProcess)
            layout.SetText(szProcess);
        else
            layout.SetText("");

        Gdk.PangoRenderer renderer  = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
        renderer.Drawable           = drawArea.GdkWindow;
        renderer.Gc                 = drawArea.Style.BlackGC;

        renderer.SetOverrideColor(RenderPart.Foreground, new Gdk.Color(0, 0, 0));
        //renderer.DrawLayout(layout, 0, 0);
        drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 6, layout, new Gdk.Color(255, 255, 255), bMis ? clRed : (bViolation ? clRed : clGreen));

        //renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
        renderer.Drawable           = null;
        renderer.Gc                 = null;
        renderer.Dispose();
        //layout.Dispose();
        context.Dispose();
    }

    protected void OnAreaViolationExposeEvent(object o, ExposeEventArgs args)
    {
        DrawingArea drawArea = o as DrawingArea;

        //---  DrawArea Violation
        int iWidth          = drawArea.Allocation.Width;
        Context context     = drawArea.CreatePangoContext();
        Pango.Layout layout = new Pango.Layout(context);
        if (layout != null)
            layout.Width = Units.FromPixels(iWidth);
        layout.Alignment    = Pango.Alignment.Center;
        layout.Justify      = true;

        FontDescription fontDesc    = FontDescription.FromString("Ubuntu Bold 32");
        layout.FontDescription      = fontDesc;

        layout.SetText(szVehicle_Violation);

        Gdk.PangoRenderer renderer = Gdk.PangoRenderer.GetDefault(drawArea.Screen);
        renderer.Drawable       = drawArea.GdkWindow;
        renderer.Gc             = drawArea.Style.BlackGC;

        renderer.SetOverrideColor(RenderPart.Foreground, new Gdk.Color(0, 0, 0));
        //renderer.DrawLayout(layout, 0, 0);
        drawArea.GdkWindow.DrawLayoutWithColors(drawArea.Style.TextGC(StateType.Normal), 0, 20, layout, new Gdk.Color(255, 255, 0), clRed);

        //renderer.SetOverrideColor(RenderPart.Foreground, Gdk.Color.Zero);
        renderer.Drawable   = null;
        renderer.Gc         = null;
        renderer.Dispose();
        //layout.Dispose();
        context.Dispose();
    }

    protected void OnBtnClassType1Clicked(object sender, EventArgs e)
    {
        //TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.CLASS_1);
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.CLASS_1);
    }

    protected void OnBtnClassType2Clicked(object sender, EventArgs e)
    {
        //TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.CLASS_2);
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.CLASS_2);
    }

    protected void OnBtnClassTypeEmergencyClicked(object sender, EventArgs e)
    {
        //TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.CLASS_5);
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.CLASS_5);
    }

    protected void OnBtnClassTypeTowingClicked(object sender, EventArgs e)
    {
        //TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.CLASS_6);
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.CLASS_6);
        bTowing = true;          
    }

    protected void OnBtnClassTypeExemptClicked(object sender, EventArgs e)
    {
        //TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.CLASS_7);
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.CLASS_7);
    }

    protected void OnBtnClassTypeAllowPassClicked(object sender, EventArgs e)
    {
        //TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.CLASS_8);
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.CLASS_8);
    }

    protected void OnBtnClassTypeHPMCClicked(object sender, EventArgs e)
    {
        //TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.CLASS_9);
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.CLASS_9);
    }

    protected void OnBtnViolationClicked(object sender, EventArgs e)
    {
        //TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.VIOLATION);
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.VIOLATION);
    }

    protected void OnBtnForceResetClicked(object sender, EventArgs e)
    {
        //TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.FORCE_RESET);
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.FORCE_RESET);
    }

    protected void OnBtnPayCashClicked(object sender, EventArgs e)
    {
        //TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.PAY_CASH);
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.PAY_CASH);
    }

    protected void OnBtnPayCouponClicked(object sender, EventArgs e)
    {
        //TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.PAY_COUPON);
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.PAY_COUPON);
    }

    protected void OnBtnPayment6Clicked(object sender, EventArgs e)
    {
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.PAY_PM6);
    }

    protected void OnBtnPayment7Clicked(object sender, EventArgs e)
    {
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.PAY_PM7);
    }

    protected void OnBtnPayment8Clicked(object sender, EventArgs e)
    {
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.PAY_PM8);
    }

    protected void OnBtnPayment9Clicked(object sender, EventArgs e)
    {
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.PAY_PM9);
    }

    protected void OnPayEmvClicked(object sender, EventArgs e)
    {
        //TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.PAY_EMV);
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.PAY_EMV);
    }

    protected void OnBtnPayQRCodeClicked(object sender, EventArgs e)
    {
        //TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.PAY_QRCODE);
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.PAY_QRCODE);
    }

    protected void OnBtnPayTypeDmtCardClicked(object sender, EventArgs e)
    {
        //TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.PAY_DMTCARD);
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.PAY_DMTCARD);
    }

    protected void OnBtnLeftOutClicked(object sender, EventArgs e)
    {
        //TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.CLASS_8);
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.CLASS_8);
    }

    protected void OnBtnOpenOTLClicked(object sender, EventArgs e)
    {
        //TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.OTL_OPEN);
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.OTL_OPEN);
    }

    protected void OnBtnCloseOTLClicked(object sender, EventArgs e)
    {
        //TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.OTL_CLOSE);
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.OTL_CLOSE);
    }

    protected void OnBtnEndShiftClicked(object sender, EventArgs e)
    {
        //TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.END_SHIFT);
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.END_SHIFT);
    }

    protected void OnBtnEditClicked(object sender, EventArgs e)
    {
        //TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.EDIT);
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.EDIT);
    }

    protected void OnBtnPrintReceiptClicked(object sender, EventArgs e)
    {
        //TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.RECEIPT);
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.RECEIPT);
    }

    protected void OnBtnNextTowerClicked(object sender, EventArgs e)
    {
     // ---Area PayType
        bPayType        = true;
        szPayType       = szPay_Coupon;
        areaPayType.ModifyBg(StateType.Normal, clBlue);
        areaPayType.QueueDraw();

        bProcess        = true;
        szProcess       = szTxt_Processing;
        areaProcess.ModifyBg(StateType.Normal, clGreen);
        areaProcess.QueueDraw();

        imgLTL.Pixbuf   = imgLTL_Green;
        imgALB.Pixbuf   = imgALB_Open;

        if (bTowing)
        {
            imgNextCar.SetSizeRequest(150, 108);
            Gdk.Pixbuf pictureBuf   = imgVehicleType1;
            imgNextCar.Pixbuf       = pictureBuf.ScaleSimple(150, 108, Gdk.InterpType.Bilinear);

            bClassAmount            = true;
            szClassAmount           = "0 + " + szAmount_Class1;
            areaClassAmount.ModifyBg(StateType.Normal, clBlue);
            areaClassAmount.QueueDraw();
        }
        Fixed.FixedChild pos    = ((global::Gtk.Fixed.FixedChild)(this.fixed2[this.imgCarType]));
        pos.X += 120;
    }

    protected void OnBtnPayVoucher2Clicked(object sender, EventArgs e)
    {
        // ---Area PayType
        bPayType        = true;
        szPayType       = "COUPON";
        areaPayType.ModifyBg(StateType.Normal, clBlue);
        areaPayType.QueueDraw();

        bProcess        = true;
        szProcess       = szTxt_Processing;
        areaProcess.ModifyBg(StateType.Normal, clGreen);
        areaProcess.QueueDraw();
    }

    protected void OnBtnPayEasyPassClicked(object sender, EventArgs e)
    {
#if !__FULL_SCREEN__
        // ---Area PayType
        bPayType = true;
        szPayType       = szPay_EasyPass;
        //szPayType       = "M-PASS";
        areaPayType.ModifyBg(StateType.Normal, clBlue);
        areaPayType.QueueDraw();

        bProcess        = true;
        //szProcess       = "ยอดเงินไม่พอ";
        //szProcess       = "Blacklisted";
        //szProcess       = "Unlisted";
        szProcess       = "โปรดเติมเงิน";
        areaProcess.ModifyBg(StateType.Normal, clGreen);
        areaProcess.QueueDraw();

        imgTag.Pixbuf   = imgTagEasyPass;
        //imgTag.Pixbuf   = imgTagMPass;
        imgLTL.Pixbuf   = imgLTL_Green;
        imgALB.Pixbuf   = imgALB_Open;

        Fixed.FixedChild pos    = ((global::Gtk.Fixed.FixedChild)(this.fixed2[this.imgCarType]));
        pos.X += 120;

        //-----------------------------------------------------------
        txtTag_PAN.Text             = "<span font_size='large' fgcolor='#ffffff'>PAN : 3085860600622235012</span>";
        //txtTag_PAN.Text             = "<span font_size='large' fgcolor='#ffffff'>PAN : 6366850600030039074</span>";
        txtTag_PAN.UseMarkup        = true;
        txtTag_PAN.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

        txtTag_Balance.Text         = "<span font_size='large' fgcolor='#ffffff'>BALANCE : 150 บาท</span>";
        txtTag_Balance.UseMarkup    = true;
        txtTag_Balance.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));


        //txtTag_Status.Text          = "<span font_size='large' fgcolor='#ff8000'>TAG STATUS : - BLACK LISTED -</span>";
        //txtTag_Status.UseMarkup     = true;
        //txtTag_Status.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
#endif
    }

    protected void OnBtnMISClicked(object sender, EventArgs e)
    {
        ClearAllPanels();
    }

    protected void OnBtnSuspendClicked(object sender, EventArgs e)
    {
        bSuspend                = !bSuspend;
        Assembly myAssembly     = Assembly.GetExecutingAssembly();
        imgOTL.Pixbuf           = Gdk.Pixbuf.LoadFromResource(bSuspend ? "TCT_App.Images.Suspend.png" : "TCT_App.Images.GateOpen.png");
    }

    protected void OnBtnClearClicked(object sender, EventArgs e)
    {
        ClearAllPanels();
    }

    protected void OnBtnPayTagClicked(object sender, EventArgs e)
    {
        // ---Area PayType
        bPayType        = true;
        szPayType       = szPay_MPass;
        areaPayType.ModifyBg(StateType.Normal, clBlue);
        areaPayType.QueueDraw();

        bProcess        = true;
        szProcess       = szTxt_Processing;
        areaProcess.ModifyBg(StateType.Normal, clGreen);
        areaProcess.QueueDraw();

        imgLTL.Pixbuf   = imgLTL_Green;
        imgALB.Pixbuf   = imgALB_Open;

        Fixed.FixedChild pos    = ((global::Gtk.Fixed.FixedChild)(this.fixed2[this.imgCarType]));
        pos.X += 120;

        //-----------------------------------------------------------
        //txtTag_PAN.Text             = "<span font_size='large' fgcolor='#ffffff'>PAN : 3085860600622235012</span>";
        txtTag_PAN.Text             = "<span font_size='large' fgcolor='#ffffff'>PAN : 6366850600030039074</span>";
        txtTag_PAN.UseMarkup        = true;
        txtTag_PAN.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

        txtTag_Balance.Text         = "<span font_size='large' fgcolor='#ffffff'>BALANCE : 1,230 บาท</span>";
        txtTag_Balance.UseMarkup    = true;
        txtTag_Balance.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

        //txtTag_Status.Text          = "<span font_size='large' fgcolor='#ff8000'>TAG STATUS : - BLACK LISTED -</span>";
        //txtTag_Status.UseMarkup     = true;
        //txtTag_Status.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

        areaViolation.Visible   = false;
        imgTag.Pixbuf           = imgTagEasyPass;
        imgLTL.Pixbuf           = imgLTL_Green;
        imgTag.Visible          = true;
    }


    protected void OnBtnTestNextCarClicked(object sender, EventArgs e)
    {
        Gdk.Pixbuf pictureBuf   = imgVehicleType1;
        imgNextCar.Pixbuf       = pictureBuf.ScaleSimple(150, 108, Gdk.InterpType.Bilinear);
    }


    protected void OnBtnConfirmClicked(object sender, EventArgs e)
    {
        //TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.CONFIRM);
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.CONFIRM);

        //----------------------------------------------------------------------------------      
    }

    protected void OnButton3Clicked(object sender, EventArgs e)
    {
        txtLaneType.ModifyFont(FontDescription.FromString("Ubuntu 11"));
        txtLaneType.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

        if (txtLaneType.Text == "MTC/ETC")
            txtLaneType.Text = "ETC";
        else if (txtLaneType.Text == "ETC")
            txtLaneType.Text = "MTC";
        else if (txtLaneType.Text == "MTC")
            txtLaneType.Text = "ETC/MTC";
    }


    //------------------------------------------------------------
    //--- Update UI when socket cannot connect lane hardware
    protected void UpdateUI_LaneSocketFail()
    {
        Application.Invoke(delegate
        {

        });
    }

    //--- Update UI when socket reset connect lane hardware
    protected void UpdateUI_RefreshLaneControl()
    {
        Application.Invoke(delegate {

            //--- Update MLB
            imgMLB.Pixbuf   = bInMLB ? imgMLB_Open : imgMLB_Close;
            bIsMLB_Opened   = bInMLB;
            bProcess        = true;

            if (!bHPMC)
            {
                if (!bInMLB)
                    szProcess = szTxt_OpenMLB;
                else
                {
                    if (bInOTL)
                    {
                        if (bInALB)
                            szProcess = szTxt_Processing;
                        else
                        {
                            if (TCT_Lib.iCurTC_Class == 0)
                                szProcess = bInMLB ? szTxt_PleaseEnterClassType : szTxt_CloseShift;
                        }
                    }
                    else
                        szProcess = szTxt_OpenOTL;
                }
            }

            areaProcess.QueueDraw();
            //---------------------------------------------------------------------
            txtQueue.Text   = TCT_Lib.iTotalQueue.ToString("0");

            //--- Update OTL
            imgOTL.Pixbuf   = bInOTL ? imgOTL_Green : imgOTL_Red;
            bIsMLB_Opened   = bInOTL;

            //--- Update LTL
            imgLTL.Pixbuf   = bInLTL ? imgLTL_Green : imgLTL_Red;

            //--- Update ALB
            imgALB.Pixbuf   = bInALB ? imgALB_Open : imgALB_Close;
        });
    }


    void LaneController_UpdateUiQueueLaneControl()
    {
        Application.Invoke(delegate {

            txtQueue.Text   = TCT_Lib.iTotalQueue.ToString("0");
        });
    }

    protected void UpdateUI_VehicleType(Gdk.Pixbuf imgVehicleType, string szTxtVehicleType, string szTxtVehicleAmount, string szTxtProcess)
    {
        Application.Invoke(delegate {
            if (TCT_Lib.eTowingState != TowingState.None)
            {
                    if (TCT_Lib.eTowingState == TowingState.StartTowing)
                    TCT_Lib.eTowingState = TowingState.FirstClassType;
                else if (TCT_Lib.eTowingState == TowingState.FirstPayType)
                    TCT_Lib.eTowingState = TowingState.SecClassType;

                UpdateUI_VehicleTowing();
            }
            else
            {
                Assembly myAssembly     = Assembly.GetExecutingAssembly();
                if (!bHPMC)
                {
                    Fixed.FixedChild pos = ((global::Gtk.Fixed.FixedChild)(this.fixed2[this.imgCarType]));
                    pos.X               = 80;
                }
                imgCarType.Pixbuf       = imgVehicleType;

                bClassType              = true;
                szClassType             = szTxtVehicleType;
                areaClassType.ModifyBg(StateType.Normal, clBlue);
                areaClassType.QueueDraw();

                //--- Area ClassAmount
                bClassAmount            = true;
                szClassAmount           = szTxtVehicleAmount;
                areaClassAmount.ModifyBg(StateType.Normal, clBlue);
                areaClassAmount.QueueDraw();

                bProcess                = true;
                szProcess               = szTxtProcess;
                areaProcess.ModifyBg(StateType.Normal, clGreen);
                areaProcess.QueueDraw();

                szTxtVehicleType        = szTxtVehicleType.Replace("&", "&amp;");
                txtClassType.Text       = "<span font_size='large' fgcolor='#ffffff'>" + szTxtVehicleType + "</span>";
                txtClassType.UseMarkup  = true;
                //txtClassType.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

                txtClassAmount.Text         = "<span font_size='large' fgcolor='#ffffff'>" + szTxtVehicleAmount + "</span>";
                txtClassAmount.UseMarkup    = true;
                //txtClassAmount.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

                txtQueue.Text           = TCT_Lib.iTotalQueue.ToString("0");
                txtQueue.UseMarkup      = true;

                txtTag_PAN.Text         = "";
                txtTag_Balance.Text     = "";
                txtTag_Status.Text      = "";

                if (bHPMC)
                {
                    bPayType        = true;
                    szPayType       = szPay_Exempt;
                    areaPayType.ModifyBg(StateType.Normal, clBlue);
                    areaPayType.QueueDraw();

                    bProcess        = true;
                    szProcess       = szTxtProcess;
                    areaProcess.QueueDraw();

                    Fixed.FixedChild pos = ((global::Gtk.Fixed.FixedChild)(this.fixed2[this.imgCarType]));
                    pos.X               = 80;
                    imgCarType.Pixbuf   = imgVehicleHPMC;
                    if (iHPMC_TimerId > 0)
                        GLib.Source.Remove(iHPMC_TimerId);
                    iHPMC_TimerId = GLib.Timeout.Add(600, new GLib.TimeoutHandler(TimerMoving_HPMC));
                }
            }
        });                  
    }

    protected void UpdateUI_VehicleTowing()
    {
        Application.Invoke(delegate {
        
            Assembly myAssembly         = Assembly.GetExecutingAssembly();
            if (TCT_Lib.eTowingState == TowingState.StartTowing)
            {
                imgCarType.Pixbuf           = null;
                imgDummyTowing.Pixbuf       = imgVehicleDummyTowing;

                bClassType                  = true;
                szClassType                 = szVehicle_Towing;
                areaClassType.ModifyBg(StateType.Normal, clBlue);
                areaClassType.QueueDraw();

                //--- Area ClassAmount
                bClassAmount                = true;
                szClassAmount               = "";
                areaClassAmount.ModifyBg(StateType.Normal, clBlue);
                areaClassAmount.QueueDraw();
                    
                bProcess                    = true;
                szProcess                   = szTxt_TowingFirstClass;
                areaProcess.ModifyBg(StateType.Normal, clGreen);
                areaProcess.QueueDraw();
            }
            else if (TCT_Lib.eTowingState == TowingState.FirstClassType)
            {
                imgDummyTowing.Pixbuf       = imgVehicleDummyTowing2;

                imgCarType.Pixbuf           = imgVehicleTowing;
                Fixed.FixedChild pos        = ((global::Gtk.Fixed.FixedChild)(this.fixed2[this.imgCarType]));
                if (pos.X >= 120)
                    pos.X = 80;
                pos.X += 120;

                //--- Area ClassAmount
                bClassAmount                = true;
                szClassAmount               = TCT_Lib.fCurFareAmount + " บาท +";
                areaClassAmount.ModifyBg(StateType.Normal, clBlue);
                areaClassAmount.QueueDraw();

                bProcess                    = true;
                szProcess                   = szTxt_TowingFirstPay;
                areaProcess.ModifyBg(StateType.Normal, clGreen);
                areaProcess.QueueDraw();
            }
            else if (TCT_Lib.eTowingState == TowingState.SecClassType)
            {
                imgDummyTowing.Pixbuf       = null;

                //imgCarType.Pixbuf       = imgVehicleTowing;
                //Fixed.FixedChild pos    = ((global::Gtk.Fixed.FixedChild)(this.fixed2[this.imgCarType]));
                //pos.X += 50;

                Fixed.FixedChild pos = ((global::Gtk.Fixed.FixedChild)(this.fixed2[this.imgNextCar]));
                pos.X   = 50;
                pos.Y   = 175;
                imgNextCar.SetSizeRequest(150, 108);
                Gdk.Pixbuf pictureBuf       = TCT_Lib.iTowingTC_Class == 1 ? imgVehicleType1 : imgVehicleType2;
                imgNextCar.Pixbuf           = pictureBuf.ScaleSimple(150, 108, Gdk.InterpType.Bilinear);
                //imgNextCar.Pixbuf         = pictureBuf.RotateSimple(Gdk.PixbufRotation.Counterclockwise);
                
                //--- Area ClassAmount
                bClassAmount                = true;
                string szTxt_Amount         = TCT_Lib.fCurFareAmount + " บาท +";
                szClassAmount               = szTxt_Amount + " " + TCT_Lib.fTowingFareAmount.ToString("0") + " บาท";
                areaClassAmount.ModifyBg(StateType.Normal, clBlue);
                areaClassAmount.QueueDraw();

                bProcess                    = true;
                szProcess                   = szTxt_TowingSecPay;
                areaProcess.ModifyBg(StateType.Normal, clGreen);
                areaProcess.QueueDraw();
            }

            string szTxtVehicleType     = szVehicle_Towing.Replace("&", "&amp;");
            txtClassType.Text           = "<span font_size='large' fgcolor='#ffffff'>" + szTxtVehicleType + "</span>";
            txtClassType.UseMarkup      = true;
            //txtClassType.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

            string szTxtVehicleAmount   = "";
            txtClassAmount.Text         = "<span font_size='large' fgcolor='#ffffff'>" + szTxtVehicleAmount + "</span>";
            txtClassAmount.UseMarkup    = true;
            //txtClassAmount.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

            txtQueue.Text               = TCT_Lib.iTotalQueue.ToString("0");
            txtQueue.UseMarkup          = true;
        });
    }

    void LaneController_UpdateUI_TowingFirstPass()
    {
        Application.Invoke(delegate {
        
            Assembly myAssembly     = Assembly.GetExecutingAssembly();

            imgCarType.Pixbuf       = null;
            imgDummyTowing.Pixbuf   = imgVehicleDummyTowing1Pass;

            Fixed.FixedChild pos    = ((global::Gtk.Fixed.FixedChild)(this.fixed2[this.imgNextCar]));
            pos.X   = 50;
            pos.Y   = 175;
            imgNextCar.SetSizeRequest(150, 108);
            Gdk.Pixbuf pictureBuf   = TCT_Lib.iTowingTC_Class == 1 ? imgVehicleType1 : imgVehicleType2;
            imgNextCar.Pixbuf       = pictureBuf.ScaleSimple(150, 108, Gdk.InterpType.Bilinear);

            txtQueue.Text           = TCT_Lib.iTotalQueue.ToString("0");
            txtQueue.UseMarkup      = true;
        });
    }

    protected void UpdateUI_Violation()
    {
        Application.Invoke(delegate {
            Assembly myAssembly = Assembly.GetExecutingAssembly();
            bViolation          = true;
            bProcess            = true;
            szProcess           = "Violation";
            areaProcess.ModifyBg(StateType.Normal, bViolation ? clRed : clGreen);
            areaProcess.QueueDraw();

            areaViolation.Visible = true;
            areaViolation.ModifyBg(StateType.Normal, clRed);
            areaViolation.QueueDraw();
        });            
    }

    protected void LaneController_VehicleTypeLaneControl(VehicleType eVehicleType)
    {
        if (eVehicleType == VehicleType.NONE)
        {
            UpdateUI_VehicleType(null, "", "", "");
        }
        else if (eVehicleType == VehicleType.VIOLATION)
        {
            UpdateUI_Violation();
        }
        else if (eVehicleType == VehicleType.CLASS_1)
        {
            UpdateUI_VehicleType(imgVehicleType1, szVehicle_Type1, szAmount_Class1, szTxt_CashCoupon);
        }
        else if (eVehicleType == VehicleType.CLASS_2)
        {
            UpdateUI_VehicleType(imgVehicleType2, szVehicle_Type2, szAmount_Class2, szTxt_CashCoupon); 
        }
        else if (eVehicleType == VehicleType.EMERGENCY)
        {
            UpdateUI_VehicleType(imgVehicleEmergency, szVehicle_Emergency, szAmount_0, szPay_Exempt);
            UpdateUI_PayType(szPay_Exempt, szTxt_EditType);
        }
        else if (eVehicleType == VehicleType.EXEMPT)
        {
            UpdateUI_VehicleType(imgVehicleEmergency, szVehicle_Exempt, szAmount_0, szPay_Exempt);
            UpdateUI_PayType(szPay_Exempt, szTxt_EditType);
        }
        else if (eVehicleType == VehicleType.LEFT_EXIT)
        {
            UpdateUI_VehicleType(imgVehicleType1, szVehicle_LeftExit, szAmount_0, szPay_Exempt);
            UpdateUI_PayType(szPay_Exempt, szTxt_EditType);
        }
        else if (eVehicleType == VehicleType.TOWING)
        {
            //UpdateUI_VehicleType(imgVehicleDummyTowing, szVehicle_Towing, szAmount_0, szTxt_ConfirmEdit);
            UpdateUI_VehicleTowing();
        }
        else if (eVehicleType == VehicleType.HPMC)
        {
            if (bHPMC)
            {
                //--- Stop HPMC Mode
                if (iHPMC_TimerId > 0)
                    GLib.Source.Remove(iHPMC_TimerId);
                iHPMC_TimerId           = 0;
                TCT_Lib.eCurLaneMode    = LaneMode.NORMAL;
                //TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.FORCE_RESET);
                TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.FORCE_RESET);
            }
            else
            {
                bHPMC                   = true;
                TCT_Lib.eCurLaneMode    = LaneMode.HPMC;
                UpdateUI_VehicleType(imgVehicleHPMC, szVehicle_HPMC, szAmount_0, szTxt_EndHPMC);
                //UpdateUI_PayType(szExempt, szTxt_EndHPMC);
            }
        }
        else
        {
            UpdateUI_VehicleType(null, "- Unknown -", "- Unknown -", "- Unknown -");
        }
    }

    protected void UpdateUI_PayType(string szTxtPayType, string szTxtProcess)
    {
        Application.Invoke(delegate {

            if (TCT_Lib.eTowingState != TowingState.None)
            {
                if (TCT_Lib.eTowingState == TowingState.FirstClassType)
                    TCT_Lib.eTowingState = TowingState.FirstPayType;
                else if (TCT_Lib.eTowingState == TowingState.SecClassType)
                    TCT_Lib.eTowingState = TowingState.SecPayType;

                UpdateUI_PayTowing(szTxtPayType);
            }
            else
            {
                // ---Area PayType
                bPayType        = true;
                szPayType       = szTxtPayType;
                areaPayType.ModifyBg(StateType.Normal, clBlue);
                areaPayType.QueueDraw();

                bProcess        = true;
                szProcess       = szTxtProcess;
                areaProcess.ModifyBg(StateType.Normal, clGreen);
                areaProcess.QueueDraw();

                if (TCT_Lib.xCurPayment.payType == (int)PayType.DMT_CARD)
                {
                    imgCarType.Pixbuf       = TCT_Lib.iCurTC_Class == 1 ? imgVehicleAllowed : imgVehicleTowing;
                    //-----------------------------------------
                    bClassAmount            = true;
                    szClassAmount           = szAmount_0;
                    areaClassAmount.ModifyBg(StateType.Normal, clBlue);
                    areaClassAmount.QueueDraw();

                    txtClassAmount.Text         = "<span font_size='large' fgcolor='#ffffff'>" + szAmount_0 + "</span>";
                    txtClassAmount.UseMarkup    = true;
                }

                if (bTowing)
                {
                    imgNextCar.SetSizeRequest(150, 108);
                    Gdk.Pixbuf pictureBuf   = imgVehicleType1;
                    imgNextCar.Pixbuf       = pictureBuf.ScaleSimple(150, 108, Gdk.InterpType.Bilinear);

                    bClassAmount            = true;
                    szClassAmount           = "0 + " + szAmount_Class1;
                    areaClassAmount.ModifyBg(StateType.Normal, clBlue);
                    areaClassAmount.QueueDraw();
                }

                if (!bHPMC)
                {
                    //Thread.Sleep(300);
                    Fixed.FixedChild pos = ((global::Gtk.Fixed.FixedChild)(this.fixed2[this.imgCarType]));
                    pos.X += 120;
                }
            }
        });
    }

    protected void UpdateUI_PayTowing(string szTxtPayType)
    {
        Application.Invoke(delegate {
            // ---Area PayType
            if (TCT_Lib.eTowingState == TowingState.FirstPayType)
            {
                bPayType        = true;
                szPayType       = GetPayTypeDisplayName(TCT_Lib.xCurPayment.payType) + " +";
                areaPayType.ModifyBg(StateType.Normal, clBlue);
                areaPayType.QueueDraw();

                bProcess        = true;
                szProcess       = szTxt_TowingSecClass;
                areaProcess.ModifyBg(StateType.Normal, clGreen);
                areaProcess.QueueDraw();

                if (TCT_Lib.xCurPayment.payType == (int)PayType.DMT_CARD)
                {
                    bClassAmount        = true;
                    szClassAmount       = TCT_Lib.fCurFareAmount + " บาท +";
                    areaClassAmount.ModifyBg(StateType.Normal, clBlue);
                    areaClassAmount.QueueDraw();
                }
                else if ((TCT_Lib.xCurPayment.payType == (int)PayType.CASH) || (TCT_Lib.xCurPayment.payType == (int)PayType.COUPON))
                {
                    bClassAmount        = true;
                    szClassAmount       = TCT_Lib.fCurFareAmount + " บาท +";
                    areaClassAmount.ModifyBg(StateType.Normal, clBlue);
                    areaClassAmount.QueueDraw();
                }
            }
            else if (TCT_Lib.eTowingState == TowingState.SecPayType)
            {
                bPayType        = true;
                szPayType       = GetPayTypeDisplayName(TCT_Lib.xCurPayment.payType) + " + " + GetPayTypeDisplayName(TCT_Lib.xTowingPayment.payType);
                areaPayType.ModifyBg(StateType.Normal, clBlue);
                areaPayType.QueueDraw();

                bProcess        = true;
                szProcess       = szTxt_TowingSecClass;
                areaProcess.ModifyBg(StateType.Normal, clGreen);
                areaProcess.QueueDraw();

                if (TCT_Lib.xTowingPayment.payType == (int)PayType.DMT_CARD)
                {
                    bClassAmount        = true;
                    int idx             = szClassAmount.IndexOf('+');
                    szClassAmount       = szClassAmount.Remove(idx);
                    szClassAmount       += "+ " + TCT_Lib.xTowingPayment.payAmount + " บาท";
                    areaClassAmount.ModifyBg(StateType.Normal, clBlue);
                    areaClassAmount.QueueDraw();
                }
                else if ((TCT_Lib.xTowingPayment.payType == (int)PayType.CASH) || (TCT_Lib.xTowingPayment.payType == (int)PayType.COUPON))
                {
                    bClassAmount        = true;
                    int idx             = szClassAmount.IndexOf('+');
                    szClassAmount       = szClassAmount.Remove(idx);
                    szClassAmount       += "+ " + TCT_Lib.xTowingPayment.payAmount + " บาท";
                    areaClassAmount.ModifyBg(StateType.Normal, clBlue);
                    areaClassAmount.QueueDraw();
                }
            }
        });
    }

    protected string GetPayTypeDisplayName(int iPayType)
    {
        switch (iPayType) {
            case (int)PayType.NONE:     return "";
            case (int)PayType.CASH:     return szPay_Cash;
            case (int)PayType.COUPON:   return szPay_Coupon;
            case (int)PayType.EMV:      return szPay_EMV;
            case (int)PayType.QrCode:   return szPay_QrCode;
            case (int)PayType.DMT_CARD: return szPay_DmtCard;
            case (int)PayType.LeftExit: return szPay_LeftExit;
            case (int)PayType.PM6:      return TCT_Lib.szPaymentName_06;
            case (int)PayType.PM7:      return TCT_Lib.szPaymentName_07;
            case (int)PayType.PM8:      return TCT_Lib.szPaymentName_08;
            case (int)PayType.PM9:      return TCT_Lib.szPaymentName_09;
            default:                    return "";   
        }
    }

    void LaneController_EditBackClassType()
    {
        if (TCT_Lib.eTowingState == TowingState.StartTowing)
        {
            UpdateUI_VehicleTowing();
            Fixed.FixedChild pos = ((global::Gtk.Fixed.FixedChild)(this.fixed2[this.imgCarType]));
            if (pos.X > 80)
                pos.X -= 120;
        }
        else if (TCT_Lib.eTowingState == TowingState.FirstPayType)
        {
            imgDummyTowing.Pixbuf   = imgVehicleDummyTowing2;
            imgNextCar.Pixbuf       = null;
            bClassAmount            = true;
            szClassAmount           = TCT_Lib.fCurFareAmount + " บาท +";
            areaClassAmount.ModifyBg(StateType.Normal, clBlue);
            areaClassAmount.QueueDraw();

            bProcess                = true;
            szProcess               = szTxt_TowingSecClass;
            areaProcess.ModifyBg(StateType.Normal, clGreen);
            areaProcess.QueueDraw();
        }
    }

    void LaneController_EditBackPayType()
    {
        Application.Invoke(delegate {
            if (TCT_Lib.eTowingState == TowingState.None)
            {   
                // ---Area PayType
                bPayType                    = true;
                szPayType                   = "";
                areaPayType.ModifyBg(StateType.Normal, clBlue);
                areaPayType.QueueDraw();

                bProcess                    = true;
                szProcess                   = szTxt_ConfirmEdit;
                areaProcess.ModifyBg(StateType.Normal, clGreen);
                areaProcess.QueueDraw();

                txtClassAmount.Text         = "";
                txtClassAmount.UseMarkup    = true;

                Fixed.FixedChild pos = ((global::Gtk.Fixed.FixedChild)(this.fixed2[this.imgCarType]));
                if (pos.X > 80)
                    pos.X -= 120;
            }
            else
            {
                //--- Towing Mode
                if (TCT_Lib.eTowingState == TowingState.FirstClassType)
                {
                    bPayType                    = true;
                    szPayType                   = "";
                    areaPayType.ModifyBg(StateType.Normal, clBlue);
                    areaPayType.QueueDraw();

                    bProcess                    = true;
                    szProcess                   = szTxt_TowingFirstPay;
                    areaProcess.ModifyBg(StateType.Normal, clGreen);
                    areaProcess.QueueDraw();
                }
                else if (TCT_Lib.eTowingState == TowingState.SecClassType)
                {
                    bPayType                    = true;
                    szPayType                   = GetPayTypeDisplayName(TCT_Lib.xCurPayment.payType) + " +";
                    areaPayType.ModifyBg(StateType.Normal, clBlue);
                    areaPayType.QueueDraw();

                    bProcess                    = true;
                    szProcess                   = szTxt_TowingSecPay;
                    areaProcess.ModifyBg(StateType.Normal, clGreen);
                    areaProcess.QueueDraw();
                }
            }
        });
    }

    protected void LaneController_PayTypeLaneControl(PayType ePayType)
    {
        if (ePayType == PayType.EXEMPT)
        {
            if (TCT_Lib.laneController.eCurVehicleType == VehicleType.HPMC)
            {
                bHPMC = true;
                Fixed.FixedChild pos = ((global::Gtk.Fixed.FixedChild)(this.fixed2[this.imgCarType]));
                pos.X = 150;
                if (iHPMC_TimerId > 0)
                    GLib.Source.Remove(iHPMC_TimerId);
                iHPMC_TimerId = GLib.Timeout.Add(600, new GLib.TimeoutHandler(TimerMoving_HPMC));
                UpdateUI_PayType(szPay_Exempt, szTxt_EndHPMC);
            }
            else
                UpdateUI_PayType(szPay_Exempt, szTxt_Processing);
        }
        else if (ePayType == PayType.NONE)
        {
            UpdateUI_PayType("", "");
        }
        else
            UpdateUI_PayType(GetPayTypeDisplayName((int)ePayType), szTxt_Processing);
    }
    
    private bool TimerMoving_HPMC()
    {
        Fixed.FixedChild pos = ((global::Gtk.Fixed.FixedChild)(this.fixed2[this.imgCarType]));
        if (pos.X == 80)
            pos.X = 150;
        else if (pos.X == 150)
            pos.X = 220;
        else if (pos.X == 220)
            pos.X = 80;
        return true;
    }

    void LaneController_ClearPanelUiLaneControl()
    {
        Application.Invoke(delegate
        {
            //--- Clear Panel UI
            imgCarType.Pixbuf       = null;
            imgNextCar.Pixbuf       = null;
            imgDummyTowing.Pixbuf   = null;
            imgMode.Pixbuf          = null;
            imgTag.Pixbuf           = null;
            imgMIS.Pixbuf           = null;

            Fixed.FixedChild pos    = ((global::Gtk.Fixed.FixedChild)(this.fixed2[this.imgCarType]));
            pos.X                   = 80;

            txtClassType.Text       = "";
            txtClassAmount.Text     = "";

            txtTag_PAN.Text         = "";
            txtTag_Balance.Text     = "";
            txtTag_Status.Text      = "";

            txtQueue.Text           = TCT_Lib.iTotalQueue.ToString("0");

            szClassType             = "";
            areaClassType.ModifyBg(StateType.Normal, clBlue);
            areaClassType.QueueDraw();

            szClassAmount           = "";
            areaClassAmount.ModifyBg(StateType.Normal, clBlue);
            areaClassAmount.QueueDraw();

            szPayType               = "";
            areaPayType.ModifyBg(StateType.Normal, clBlue);
            areaPayType.QueueDraw();

            areaViolation.Visible   = false;

            bHPMC                   = false;
            bViolation              = false;
            bMis                    = false;
            bTowing                 = false;
            szProcess               = "";
            areaProcess.ModifyBg(StateType.Normal, clGreen);
            areaProcess.QueueDraw();

            //--- Area Processing
            bProcess                = true;
            szProcess               = bInMLB ? szTxt_PleaseEnterClassType : "";
            areaProcess.ModifyBg(StateType.Normal, clGreen);
            areaProcess.QueueDraw();

            if (iHPMC_TimerId > 0)
                GLib.Source.Remove(iHPMC_TimerId);
            iHPMC_TimerId = 0;
        });
    }


    //-------------------------------------------------------------
    void LaneController_DigitalInputLaneControl(int[] values)
    {
        //--- Device Digital Input has change value
        bInMLB          = values[TCT_Lib.iLCSD_DI_0]    == 1;
        bInEFS          = values[TCT_Lib.iLCSD_DI_1]    == 1;
        bInEntryLoop    = values[TCT_Lib.iLCSD_DI_2]    == 1;
        bInExitLoop     = values[TCT_Lib.iLCSD_DI_3]    == 1;
        bInEntryOB      = values[TCT_Lib.iLCSD_DI_4]    == 1;
        bInExitOB       = values[TCT_Lib.iLCSD_DI_5]    == 1;
        bInALB          = values[TCT_Lib.iLCSD_DI_6]    == 1;
        bInOTL          = values[TCT_Lib.iLCSD_DI_7]    == 1;
        bInLTL          = values[TCT_Lib.iLCSD_DI_8]    == 1;
        bInASB          = values[TCT_Lib.iLCSD_DI_9]    == 1;
        bInSiren        = values[TCT_Lib.iLCSD_DI_10]   == 1;
        bInTreadleError = values[TCT_Lib.iLCSD_DI_13]   == 1;
        bInTreadleC     = values[TCT_Lib.iLCSD_DI_14]   == 1;
        bInTreadleD     = values[TCT_Lib.iLCSD_DI_15]   == 1;

        if (bCurExitLoop != bInExitLoop)
        {
            DrawLoopOB_Back();
            bCurExitLoop = bInExitLoop;
        }
        if (bCurExitOB != bInExitOB)
        {
            DrawLoopOB_Back();
            bCurExitOB = bInExitOB;
        }

        if (bCurEntryLoop != bInEntryLoop)
        {
            DrawLoopOB_Front();
            bCurEntryLoop = bInEntryLoop;
        }
        if (bCurEntryOB != bInEntryOB)
        {
            DrawLoopOB_Front();
            bCurEntryOB = bInEntryOB;
        }

        //----------------------------------------------------
        if (bInEntryLoop)
        {
            if (TCT_Lib.bEntryLoop != bInEntryLoop)
            {
                TCT_Lib.bEntryLoop = bInEntryLoop;
                //TCT_Lib.iTotalQueue++;
            }
        }
        else
            TCT_Lib.bEntryLoop = bInEntryLoop;

        if (bInExitLoop)
        {
            if (TCT_Lib.bExitLoop != bInExitLoop)
            {
                TCT_Lib.bExitLoop = bInExitLoop;
                //TCT_Lib.iTotalQueue--;
                //if (TCT_Lib.iTotalQueue < 0)
                //    TCT_Lib.iTotalQueue = 0;
            }
        }
        else
            TCT_Lib.bExitLoop = bInExitLoop;

        UpdateUI_RefreshLaneControl();
    }

    void LaneController_DigitalOutputLaneControl(int[] values)
    {
        //--- Device Digital Ouput has change value
        bRegisterBell   = values[0] == 1;
        bSiren          = values[1] == 1;
        bASB            = values[2] == 1;
        bLTL            = values[3] == 1;
        bALB_Open       = values[4] == 1;
        bALB_Close      = values[5] == 1;
        bOTL_Status     = values[6] == 1;   // 0=Close (Red), 1=Open (Green)
        bOTL_Mode       = values[7] == 1;   // 0=MTC, 1=ETC
        bOTL_DualMode   = values[8] == 1;
        bCRS_Reset      = values[12] == 1;
        bTFI_Reset      = values[13] == 1;
        bLoop_Reset     = values[14] == 1;
        bIPC_Reset      = values[15] == 1;

        //UpdateUI_RefreshLaneControl();
    }

    void LaneController_ClearQueueLaneControl()
    {
        Application.Invoke(delegate
        {
            txtQueue.Text = TCT_Lib.iTotalQueue.ToString("0");
        });
    }

    void LaneController_EndShiftLaneControl()
    {
        Application.Invoke(delegate
        {
            GLib.Source.Remove(iTimerId1Sec);
            try
            {
                if (!TCT_Lib.bMaintenanceMode)
                {
                    //LoginWnd loginWnd   = new LoginWnd();
                    LoginWnd loginWnd   = TCT_Lib.frmLoginWnd;
                    Gdk.Color colBk     = new Gdk.Color(0x2E, 0x2E, 0x2E);
                    loginWnd.ModifyBg(StateType.Normal, colBk);

                    var screen          = loginWnd.GdkWindow.Screen;
                    loginWnd.GdkWindow.GetSize(out var winWidth, out var winHeight);
                    loginWnd.Move((screen.Width / 2) - (winWidth / 2), (screen.Height / 2) - (winHeight / 2));

                    //loginWnd.SetPosition(WindowPosition.Center);
                    loginWnd.bLogined = false;
                    loginWnd.Show();
                }
                if (TCT_Lib.eCurLaneMode != LaneMode.MAINTENANCE)
                {
                    TCT_Lib.eCurLaneMode    = LaneMode.CLOSED;
                    TCT_Lib.szStaffID       = TCT_Lib.szStaffCode = TCT_Lib.szStaffName = "";
                }
                else
                {
                    TCT_Lib.eCurAppPage = AppPage.Maintenance;
                }

                //--------------------------------------------------------------------------------------------
                TCT_Lib.laneController.digitalInputLaneControl      -= LaneController_DigitalInputLaneControl;
                TCT_Lib.laneController.digitalOutputLaneControl     -= LaneController_DigitalOutputLaneControl;
                TCT_Lib.laneController.vehicleTypeLaneControl       -= LaneController_VehicleTypeLaneControl;
                TCT_Lib.laneController.payTypeLaneControl           -= LaneController_PayTypeLaneControl;
                TCT_Lib.laneController.showAvcLaneControl           -= LaneController_ShowAvcLaneControl;
                TCT_Lib.laneController.eventViolation               -= LaneController_EventViolation;
                TCT_Lib.laneController.eventDIS                     -= LaneController_EventDIS;
                TCT_Lib.laneController.clearPanelUiLaneControl      -= LaneController_ClearPanelUiLaneControl;
                TCT_Lib.laneController.clearQueueLaneControl        -= LaneController_ClearQueueLaneControl;
                TCT_Lib.laneController.updateUiQueueLaneControl     -= LaneController_UpdateUiQueueLaneControl;
                TCT_Lib.laneController.editBackClassType            -= LaneController_EditBackClassType;
                TCT_Lib.laneController.editBackPayType              -= LaneController_EditBackPayType;
                TCT_Lib.laneController.endShiftLaneControl          -= LaneController_EndShiftLaneControl;
                TCT_Lib.laneController.updateUiSupervisor           -= LaneController_UpdateUiSupervisor;
                TCT_Lib.laneController.emvCreditPaymentUI           -= LaneController_EmvCreditPaymentUI;
                TCT_Lib.laneController.emvQrCodePaymentUI           -= LaneController_EmvQrCodePaymentUI;
                TCT_Lib.laneController.messageProcessUI             -= LaneController_MessageProcessUI;
                TCT_Lib.laneController.showModeType                 -= LaneController_ShowModeType;
                TCT_Lib.laneController.salesCoupon                  -= LaneController_SalesCoupon;
                TCT_Lib.laneController.printConvoy                  -= LaneController_PrintConvoy;
                //TCT_Lib.laneController.dmtPayment                   -= LaneController_DmtPayment;
                TCT_Lib.laneController.checkNetworkStatus           -= LaneController_CheckNetworkStatus;
                TCT_Lib.laneController.nextVehicleTypeLaneControl   -= LaneController_NextVehicleTypeLaneControl;
                TCT_Lib.laneController.laneStateEtcPayCardPAN       -= LaneController_LaneStateEtcPayCardPAN;
                TCT_Lib.laneController.compilePassageETC            -= LaneController_CompilePassageETC;
                TCT_Lib.laneController.drawETC_AlbMode              -= LaneController_DrawETC_AlbMode;
                TCT_Lib.laneController.showTotalLoop                -= LaneController_ShowTotalLoop;
                TCT_Lib.laneController.updateUI_TowingFirstPass     -= LaneController_UpdateUI_TowingFirstPass;
                TCT_Lib.laneController.updateUI_Convoy              -= LaneController_UpdateUI_Convoy;


                //---------------------------------------------------------------------------------------------

                this.Dispose();
                this.Destroy();

            }
            catch (Exception err)
            {
                Console.WriteLine($"- Error EndShift : {err.Message}");
                LogInfo.WriteLog($"- Error EndShift : {err.Message}\r\n{err.StackTrace}");
            }
        });
    }

    protected void OnBtnPrintConvoyClicked(object sender, EventArgs e)
    {
        //--- Print Convoy - H
        //TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.PRINT_CONVOY);
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.PRINT_CONVOY);
    }

    protected void OnBtnSalesCouponClicked(object sender, EventArgs e)
    {
        //--- Print SalesCoupon - L
        //TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.SALE_COUPON);
        TCT_Lib.webSocketLane.SendVirtualKeyboard((int)KeyPressedType.SALE_COUPON);
    }
        
    //--- v2.0.2: ConnectBefore + RetVal = true for mapped keys, so GTK does not ALSO
    //--- re-press the focused button on ENTER/space or move focus on arrow keys
    //--- (e.g. last touched FORCE RESET pressed again by the ENTER key).
    //--- Key value is read now, not inside Invoke (the event is freed after this handler).
    [GLib.ConnectBefore]
    protected void OnKeyPressEvent(object o, KeyPressEventArgs args)
    {
        int iKeyValue = (int)args.Event.KeyValue;
        if (!TCT_Lib.keyMapping.ContainsKey(iKeyValue))
            return;

        int iKey        = TCT_Lib.keyMapping[iKeyValue];
        args.RetVal     = true;
        Application.Invoke(delegate
        {
            try
            {
                TCT_Lib.webSocketLane.SendVirtualKeyboard(iKey, 1);
            }
            catch (Exception err)
            {
                LogInfo.WriteLaneEvent("- Error MainWindow : OnKeyPressEvent : " + err.Message);
            }
        });
    }

    protected void LaneController_EventViolation()
    {
        Application.Invoke(delegate
        {
            bClassType      = true;
            szClassType     = szVehicle_Violation;
            areaClassType.ModifyBg(StateType.Normal, clBlue);
            areaClassType.QueueDraw();

            bViolation      = true;
            bProcess        = true;
            szProcess       = szVehicle_Violation;
            areaProcess.ModifyBg(StateType.Normal, bViolation ? clRed : clGreen);
            areaProcess.QueueDraw();

            areaViolation.Visible = true;
            areaViolation.ModifyBg(StateType.Normal, clRed);
            areaViolation.QueueDraw();
        });
    }

    void LaneController_EventDIS()
    {
    }

    protected void DrawLoopOB_Front()
    {
        Application.Invoke(delegate
        {
            txtLoop_F.Text        = bInEntryLoop ? "<span font_size = 'small' fgcolor = '#00FF00' >Loop</span>" : "<span font_size = 'small' fgcolor = '#444444' >Loop</span>";
            txtOB_F.Text          = bInEntryOB ? "<span font_size = 'small' fgcolor = '#00FF00' >OB</span>" : "<span font_size = 'small' fgcolor = '#444444' >OB</span>";
            txtLoop_F.UseMarkup   = true;
            txtOB_F.UseMarkup     = true;
            txtLoop_F.ModifyFg(StateType.Normal, bInEntryLoop ? clGreen : clDim);
            txtOB_F.ModifyFg(StateType.Normal, bInEntryOB ? clGreen : clDim);
        });
    }

    protected void DrawLoopOB_Back()
    {
        Application.Invoke(delegate
        {
            txtLoop.Text        = bInExitLoop ? "<span font_size = 'small' fgcolor = '#00FF00' >Loop</span>" : "<span font_size = 'small' fgcolor = '#444444' >Loop</span>";
            txtOB.Text          = bInExitOB ? "<span font_size = 'small' fgcolor = '#00FF00' >OB</span>" : "<span font_size = 'small' fgcolor = '#444444' >OB</span>";
            txtLoop.UseMarkup   = true;
            txtOB.UseMarkup     = true;
            txtLoop.ModifyFg(StateType.Normal, bInExitLoop ? clGreen : clDim);
            txtOB.ModifyFg(StateType.Normal, bInExitOB ? clGreen : clDim);
        });
    }

    public void LaneController_DrawETC_AlbMode(int _iAlbMode)
    {
        Application.Invoke(delegate
        {
            iAlbMode = _iAlbMode;
            //--- Mode : Normal=0, Open=1, Close=2
            string szAlbMode = "";
            if (iAlbMode == 0)
                szAlbMode = "ALB > Normal";
            else if (iAlbMode == 1)
                szAlbMode = "ALB > Open";
            else if (iAlbMode == 2)
                szAlbMode = "ALB > Close";
            else
                szAlbMode = "ALB > Unknown";

            txtAlbMode.Text         = "<span font_size = 'small' fgcolor = '#00FF00' >" +  szAlbMode + "</span>";
            txtAlbMode.UseMarkup    = true;
            txtAlbMode.ModifyFg(StateType.Normal, iAlbMode == 0 ? clYellow : clGreen);

            if (iTimerIdAlbMode == 0)
                iTimerIdAlbMode = GLib.Timeout.Add(2000, new GLib.TimeoutHandler(ClearTextAlbMode));
        });
    }

    private bool ClearTextAlbMode()
    {
        Application.Invoke(delegate
        {
            string szAlbMode = "";
            if (iAlbMode == 0)
                szAlbMode = "ALB > Normal";
            else if (iAlbMode == 1)
                szAlbMode = "ALB > Open";
            else if (iAlbMode == 2)
                szAlbMode = "ALB > Close";
            else
                szAlbMode = "ALB > Unknown";
            txtAlbMode.Text         = $"<span font_size = 'small' fgcolor = '#666666' >" + szAlbMode + "</span>";
            txtAlbMode.UseMarkup    = true;
            txtAlbMode.ModifyFg(StateType.Normal, clDim);
            if (iTimerIdAlbMode > 0)
                GLib.Source.Remove(iTimerIdAlbMode);
            iTimerIdAlbMode = 0;
        });
        return false;
    }

    void LaneController_ShowAvcLaneControl(int avcClassType)
    {
        Application.Invoke(delegate
        {
            txtAVC.Text         = $"<span font_size = 'small' fgcolor = '#00FF00' >AVC={avcClassType}</span>";
            txtAVC.UseMarkup    = true;
            txtAVC.ModifyFg(StateType.Normal, clGreen);
            if (iTimerIdShowAVC == 0)
                iTimerIdShowAVC     = GLib.Timeout.Add(2000, new GLib.TimeoutHandler(ClearTextAVC));
        });
    }

    private bool ClearTextAVC()
    {
        Application.Invoke(delegate
        {
            txtAVC.Text         = $"<span font_size = 'small' fgcolor = '#444444' >AVC=0 </span>";
            txtAVC.UseMarkup    = true;
            txtAVC.ModifyFg(StateType.Normal, clDim);
            if (iTimerIdShowAVC > 0)
                GLib.Source.Remove(iTimerIdShowAVC);
            iTimerIdShowAVC = 0;
        });
        return false;
    }

    void LaneController_ShowQueueAlbControl(int queueALB)
    {
        Application.Invoke(delegate
        {
            if (queueALB > 0)
                txtQueueALB.Text = $"<span font_size = 'small' fgcolor = '#999999' >ALB Queue={queueALB}</span>";
            else
                txtQueueALB.Text = $"<span font_size = 'small' fgcolor = '#444444' >ALB Queue={queueALB}</span>";
            txtQueueALB.UseMarkup = true;
            txtQueueALB.ModifyFg(StateType.Normal, queueALB == 0 ? new Gdk.Color(0x99, 0x99, 0x99) : clDim);
        });
    }

    void LaneController_Show_nTrxControl(int nTrx)
    {
        Application.Invoke(delegate
        {
            txtnTrx.Text        = $"<span font_size = 'small' fgcolor = '#999999' >nTrx={nTrx-1}</span>";
            txtnTrx.UseMarkup   = true;
            txtnTrx.ModifyFg(StateType.Normal, new Gdk.Color(0x99, 0x99, 0x99));
        });
    }

    private void LaneController_EmvCreditPaymentUI_V0(decimal fPayAmount)
    {
        Application.Invoke(delegate
        {
            if (TCT_Lib.bActiveEMVDlg)
                return;

            //--- For Testing ---
            //TCT_Lib.laneController.SuccessEmvPayment(0);
            //return;
            //---------------------------------------------

            Console.WriteLine("+ Start Call EMV Credit Card");
            LogInfo.WriteLog("+ Start Call EMV Credit Card Dialog");

            if (TCT_Lib.iEdcVersionType == 0)
            {
                var frmEdcPaymentDlg    = new EdcPaymentDlg(fPayAmount, TCT_Lib.iTowingTC_Class > 0); 
                Gdk.Color colBk         = new Gdk.Color(0x3E, 0x3E, 0x3E);
                frmEdcPaymentDlg.ModifyBg(StateType.Normal, colBk);
                Console.WriteLine("+ Before Show dialog EMV");
                ResponseType xResult = (ResponseType)frmEdcPaymentDlg.Run();

                //frmEdcPaymentDlg.Destroy();

                TCT_Lib.eCurAppPage     = AppPage.Main;
                Console.WriteLine("+ After Call EMV");

                try
                {
                    if (xResult == ResponseType.Ok)
                    {
                        Console.WriteLine("+ Show Response EMV");
                        LogInfo.WriteLog("+ Response OK from EMV Credit Card");

                        var xCreditCardInfo         = frmEdcPaymentDlg.xOutCreditCard;
                        txtTag_PAN.Text             = $"<span font_size='large' fgcolor='#ffffff'>{"CreditCard No : " + xCreditCardInfo.szCardNo}</span>";
                        txtTag_PAN.UseMarkup        = true;
                        txtTag_PAN.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

                        txtTag_Balance.Text         = $"<span font_size='large' fgcolor='#ffffff'>{"Reference : " + xCreditCardInfo.szRef3}</span>";
                        txtTag_Balance.UseMarkup    = true;
                        txtTag_Balance.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

                        txtTag_Status.Text          = $"<span font_size='large' fgcolor='#ffffff'>{"Approve Code : " + xCreditCardInfo.szApprovalCode}</span>";
                        txtTag_Status.UseMarkup     = true;
                        txtTag_Status.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

                        imgTag.Pixbuf               = imgCreditCard;
                        //TCT_Lib.laneController.SuccessEmvPayment(0);
                    }
                    else
                    {
                        Console.WriteLine("+ Show Response Cancel EMV CreditCard");
                        LogInfo.WriteLog("+ Response Cancel from EMV CreditCard");
                        LogInfo.WriteLaneEvent("+ Response Cancel from EMV CreditCard");
                        try
                        {
                            if (TCT_Lib.iCurTC_Class == (int)VehicleType.CLASS_1)
                                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, TCT_Lib.iCurTC_Class.ToString(), TCT_Lib.fFareClass1.ToString("0"), "", szVehicle_Type1, szAmount_Class1);
                            else
                                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, TCT_Lib.iCurTC_Class.ToString(), TCT_Lib.fFareClass2.ToString("0"), "", szVehicle_Type2, szAmount_Class2);
                        }
                        catch (Exception err)
                        {
                            LogInfo.WriteLog("- Error Response Cancel from EMV CreditCard : " + err.Message + "\r\n" + err.StackTrace);
                        }
                    }

                    //Console.WriteLine("+ After show Response");
                    frmEdcPaymentDlg.Dispose();
                    frmEdcPaymentDlg.Destroy();
                    frmEdcPaymentDlg        = null;
                    //Console.WriteLine("+ After Dispose");
                    this.Activate();
                    TCT_Lib.bActiveEMVDlg   = false;
                    LogInfo.WriteLog("+ Finish LaneController_EmvCreditPaymentUI");
                    //---------------------------------------------------------
                    if (xResult == ResponseType.Ok)
                        TCT_Lib.laneController.SuccessEmvPayment(0);

                }
                catch (Exception err)
                {
                    LogInfo.WriteLog("- Error Finish LaneController_EmvCreditPaymentUI : " + err.Message + "\r\n" + err.StackTrace);
                    LogInfo.WriteLaneEvent("- Error Finish LaneController_EmvCreditPaymentUI : " + err.Message + "\r\n" + err.StackTrace);
                }
            }
            else
            {
                var frmEdcPaymentDlg    = new EdcPaymentDlg_V1(fPayAmount, TCT_Lib.iTowingTC_Class > 0); 
                Gdk.Color colBk         = new Gdk.Color(0x3E, 0x3E, 0x3E);
                frmEdcPaymentDlg.ModifyBg(StateType.Normal, colBk);
                Console.WriteLine("+ Before Show dialog EMV_V1");
                ResponseType xResult = (ResponseType)frmEdcPaymentDlg.Run();

                //frmEdcPaymentDlg.Destroy();

                TCT_Lib.eCurAppPage     = AppPage.Main;
                Console.WriteLine("+ After Call EMV_V1");

                try
                {
                    if (xResult == ResponseType.Ok)
                    {
                        Console.WriteLine("+ Show Response EMV V1");
                        LogInfo.WriteLog("+ Response OK from EMV Credit Card V1");

                        var xCreditCardInfo         = frmEdcPaymentDlg.xOutCreditCard;
                        txtTag_PAN.Text             = $"<span font_size='large' fgcolor='#ffffff'>{"CreditCard No : " + xCreditCardInfo.szCardNo}</span>";
                        txtTag_PAN.UseMarkup        = true;
                        txtTag_PAN.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

                        txtTag_Balance.Text         = $"<span font_size='large' fgcolor='#ffffff'>{"Reference : " + xCreditCardInfo.szRef3}</span>";
                        txtTag_Balance.UseMarkup    = true;
                        txtTag_Balance.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

                        txtTag_Status.Text          = $"<span font_size='large' fgcolor='#ffffff'>{"Approve Code : " + xCreditCardInfo.szApprovalCode}</span>";
                        txtTag_Status.UseMarkup     = true;
                        txtTag_Status.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

                        imgTag.Pixbuf               = imgCreditCard;
                        //TCT_Lib.laneController.SuccessEmvPayment(0);
                    }
                    else
                    {
                        Console.WriteLine("+ Show Response Cancel EMV CreditCard V1");
                        LogInfo.WriteLog("+ Response Cancel from EMV CreditCard V1");
                        LogInfo.WriteLaneEvent("+ Response Cancel from EMV CreditCard V1");
                        try
                        {
                            if (TCT_Lib.iCurTC_Class == (int)VehicleType.CLASS_1)
                                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, TCT_Lib.iCurTC_Class.ToString(), TCT_Lib.fFareClass1.ToString("0"), "", szVehicle_Type1, szAmount_Class1);
                            else
                                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, TCT_Lib.iCurTC_Class.ToString(), TCT_Lib.fFareClass2.ToString("0"), "", szVehicle_Type2, szAmount_Class2);
                        }
                        catch (Exception err)
                        {
                            LogInfo.WriteLog("- Error Response Cancel from EMV CreditCard : " + err.Message + "\r\n" + err.StackTrace);
                        }
                    }

                    //Console.WriteLine("+ After show Response");
                    frmEdcPaymentDlg.Dispose();
                    frmEdcPaymentDlg.Destroy();
                    frmEdcPaymentDlg        = null;
                    //Console.WriteLine("+ After Dispose");
                    this.Activate();
                    TCT_Lib.bActiveEMVDlg   = false;
                    LogInfo.WriteLog("+ Finish LaneController_EmvCreditPaymentUI V1");
                    //---------------------------------------------------------
                    if (xResult == ResponseType.Ok)
                        TCT_Lib.laneController.SuccessEmvPayment(0);
                }
                catch (Exception err)
                {
                    LogInfo.WriteLog("- Error Finish LaneController_EmvCreditPaymentUI V1 : " + err.Message + "\r\n" + err.StackTrace);
                    LogInfo.WriteLaneEvent("- Error Finish LaneController_EmvCreditPaymentUI V1 : " + err.Message + "\r\n" + err.StackTrace);
                }
            }
        });
    }

    private void LaneController_EmvCreditPaymentUI(decimal fPayAmount)
    {

        if (TCT_Lib.bActiveEMVDlg)
            return;

        //--- For Testing ---
        //TCT_Lib.laneController.SuccessEmvPayment(0);
        //return;
        //---------------------------------------------

        Console.WriteLine("+ Start Call EMV Credit Card");
        LogInfo.WriteLog("+ Start Call EMV Credit Card Dialog");

        if (TCT_Lib.iEdcVersionType == 0)
        {
            var frmEdcPaymentDlg    = new EdcPaymentDlg(fPayAmount, TCT_Lib.iTowingTC_Class > 0); 
            Gdk.Color colBk         = new Gdk.Color(0x3E, 0x3E, 0x3E);
            frmEdcPaymentDlg.ModifyBg(StateType.Normal, colBk);
            Console.WriteLine("+ Before Show dialog EMV");
            ResponseType xResult = (ResponseType)frmEdcPaymentDlg.Run();

            //frmEdcPaymentDlg.Destroy();

            TCT_Lib.eCurAppPage     = AppPage.Main;
            Console.WriteLine("+ After Call EMV");

            try
            {
                if (xResult == ResponseType.Ok)
                {
                    Console.WriteLine("+ Show Response EMV");
                    LogInfo.WriteLog("+ Response OK from EMV Credit Card");
                    var xCreditCardInfo = frmEdcPaymentDlg.xOutCreditCard;

                    Application.Invoke(delegate
                    {
                        txtTag_PAN.Text             = $"<span font_size='large' fgcolor='#ffffff'>{"CreditCard No : " + xCreditCardInfo.szCardNo}</span>";
                        txtTag_PAN.UseMarkup        = true;
                        txtTag_PAN.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

                        txtTag_Balance.Text         = $"<span font_size='large' fgcolor='#ffffff'>{"Reference : " + xCreditCardInfo.szRef3}</span>";
                        txtTag_Balance.UseMarkup    = true;
                        txtTag_Balance.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

                        txtTag_Status.Text          = $"<span font_size='large' fgcolor='#ffffff'>{"Approve Code : " + xCreditCardInfo.szApprovalCode}</span>";
                        txtTag_Status.UseMarkup     = true;
                        txtTag_Status.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

                        imgTag.Pixbuf               = imgCreditCard;
                    });
                    TCT_Lib.laneController.SuccessEmvPayment(0);
                }
                else
                {
                    Console.WriteLine("+ Show Response Cancel EMV CreditCard");
                    LogInfo.WriteLog("+ Response Cancel from EMV CreditCard");
                    LogInfo.WriteLaneEvent("+ Response Cancel from EMV CreditCard");
                    try
                    {
                        if (frmEdcPaymentDlg.bError)
                        {
                            txtTag_Balance.Text         = $"<span font_size='large' fgcolor='#ffff00'>{frmEdcPaymentDlg.szErrMsg}</span>";
                            txtTag_Balance.UseMarkup    = true;
                            txtTag_Balance.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
                        }

                        if (TCT_Lib.iCurTC_Class == (int)VehicleType.CLASS_1)
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, TCT_Lib.iCurTC_Class.ToString(), TCT_Lib.fFareClass1.ToString("0"), "", szVehicle_Type1, szAmount_Class1);
                        else
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, TCT_Lib.iCurTC_Class.ToString(), TCT_Lib.fFareClass2.ToString("0"), "", szVehicle_Type2, szAmount_Class2);
                    }
                    catch (Exception err)
                    {
                        LogInfo.WriteLog("- Error Response Cancel from EMV CreditCard : " + err.Message + "\r\n" + err.StackTrace);
                    }
                }

                //Console.WriteLine("+ After show Response");
                frmEdcPaymentDlg.Dispose();
                frmEdcPaymentDlg.Destroy();
                frmEdcPaymentDlg        = null;
                //Console.WriteLine("+ After Dispose");
                this.Activate();
                TCT_Lib.bActiveEMVDlg   = false;
                LogInfo.WriteLog("+ Finish LaneController_EmvCreditPaymentUI");

            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Error Finish LaneController_EmvCreditPaymentUI : " + err.Message + "\r\n" + err.StackTrace);
                LogInfo.WriteLaneEvent("- Error Finish LaneController_EmvCreditPaymentUI : " + err.Message + "\r\n" + err.StackTrace);
            }
        }
        else
        {
            var frmEdcPaymentDlg    = new EdcPaymentDlg_V1(fPayAmount, TCT_Lib.iTowingTC_Class > 0); 
            Gdk.Color colBk         = new Gdk.Color(0x3E, 0x3E, 0x3E);
            frmEdcPaymentDlg.ModifyBg(StateType.Normal, colBk);
            Console.WriteLine("+ Before Show dialog EMV_V1");
            ResponseType xResult = (ResponseType)frmEdcPaymentDlg.Run();

            //frmEdcPaymentDlg.Destroy();

            TCT_Lib.eCurAppPage     = AppPage.Main;
            Console.WriteLine("+ After Call EMV_V1");

            try
            {
                if (xResult == ResponseType.Ok)
                {
                    Console.WriteLine("+ Show Response EMV V1");
                    LogInfo.WriteLog("+ Response OK from EMV Credit Card V1");
                    var xCreditCardInfo = frmEdcPaymentDlg.xOutCreditCard;

                    Application.Invoke(delegate
                    {
                        txtTag_PAN.Text             = $"<span font_size='large' fgcolor='#ffffff'>{"CreditCard No : " + xCreditCardInfo.szCardNo}</span>";
                        txtTag_PAN.UseMarkup        = true;
                        txtTag_PAN.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

                        txtTag_Balance.Text         = $"<span font_size='large' fgcolor='#ffffff'>{"Reference : " + xCreditCardInfo.szRef3}</span>";
                        txtTag_Balance.UseMarkup    = true;
                        txtTag_Balance.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

                        txtTag_Status.Text          = $"<span font_size='large' fgcolor='#ffffff'>{"Approve Code : " + xCreditCardInfo.szApprovalCode}</span>";
                        txtTag_Status.UseMarkup     = true;
                        txtTag_Status.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

                        imgTag.Pixbuf               = imgCreditCard;
                    });
                    TCT_Lib.laneController.SuccessEmvPayment(0);
                }
                else
                {
                    Console.WriteLine("+ Show Response Cancel EMV CreditCard V1");
                    LogInfo.WriteLog("+ Response Cancel from EMV CreditCard V1");
                    LogInfo.WriteLaneEvent("+ Response Cancel from EMV CreditCard V1");
                    try
                    {
                        if (TCT_Lib.iCurTC_Class == (int)VehicleType.CLASS_1)
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, TCT_Lib.iCurTC_Class.ToString(), TCT_Lib.fFareClass1.ToString("0"), "", szVehicle_Type1, szAmount_Class1);
                        else
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, TCT_Lib.iCurTC_Class.ToString(), TCT_Lib.fFareClass2.ToString("0"), "", szVehicle_Type2, szAmount_Class2);
                    }
                    catch (Exception err)
                    {
                        LogInfo.WriteLog("- Error Response Cancel from EMV CreditCard : " + err.Message + "\r\n" + err.StackTrace);
                    }
                }

                //Console.WriteLine("+ After show Response");
                frmEdcPaymentDlg.Dispose();
                frmEdcPaymentDlg.Destroy();
                frmEdcPaymentDlg        = null;
                //Console.WriteLine("+ After Dispose");
                this.Activate();
                TCT_Lib.bActiveEMVDlg   = false;
                LogInfo.WriteLog("+ Finish LaneController_EmvCreditPaymentUI V1");
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Error Finish LaneController_EmvCreditPaymentUI V1 : " + err.Message + "\r\n" + err.StackTrace);
                LogInfo.WriteLaneEvent("- Error Finish LaneController_EmvCreditPaymentUI V1 : " + err.Message + "\r\n" + err.StackTrace);
            }
        }
    }

    void LaneController_EmvQrCodePaymentUI_V0(decimal fPayAmount)
    {
        Application.Invoke(delegate
        {
            if (TCT_Lib.bActiveEMVDlg)
                return;

            Console.WriteLine("+ Start Call QR CODE");
            LogInfo.WriteLog("+ Start Call QR Code Payment Dialog");
            if (TCT_Lib.iEdcVersionType == 0)
            {
                var frmEdcPaymentDlg    = new EdcPayQrDlg(fPayAmount, TCT_Lib.iTowingTC_Class > 0);
                Gdk.Color colBk         = new Gdk.Color(0x3E, 0x3E, 0x3E);
                frmEdcPaymentDlg.ModifyBg(StateType.Normal, colBk);
                Console.WriteLine("+ Before Show dialog QR Code");
                ResponseType xResult    = (ResponseType) frmEdcPaymentDlg.Run();
                //frmEdcPaymentDlg.Destroy();

                TCT_Lib.eCurAppPage     = AppPage.Main;
                Console.WriteLine("+ After Call QR Code");

                if (xResult == ResponseType.Ok)
                {
                    Console.WriteLine("+ Show Response QR Code");
                    LogInfo.WriteLog("+ Response OK from QR Code");
                    var xCreditCardInfo         = frmEdcPaymentDlg.xOutCreditCard;

                    txtTag_PAN.Text             = $"<span font_size='large' fgcolor='#ffffff'>{"CreditCard No : " + xCreditCardInfo.szCardNo}</span>";
                    txtTag_PAN.UseMarkup        = true;
                    txtTag_PAN.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

                    txtTag_Balance.Text         = $"<span font_size='large' fgcolor='#ffffff'>{"Reference : " + xCreditCardInfo.szRef3}</span>";
                    txtTag_Balance.UseMarkup    = true;
                    txtTag_Balance.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

                    txtTag_Status.Text          = $"<span font_size='large' fgcolor='#ffffff'>{"Approve Code : " + xCreditCardInfo.szApprovalCode}</span>";
                    txtTag_Status.UseMarkup     = true;
                    txtTag_Status.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

                    imgTag.Pixbuf               = imgMobileQR;
                    TCT_Lib.laneController.SuccessEmvPayment(1);
                }
                else
                {
                    Console.WriteLine("+ Show Response Cancel EMV QR Code");
                    LogInfo.WriteLog("+ Response Cancel from EMV QR Code");
                    try
                    {
                        if (TCT_Lib.iCurTC_Class == (int)VehicleType.CLASS_1)
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, TCT_Lib.iCurTC_Class.ToString(), TCT_Lib.fFareClass1.ToString("0"), "", szVehicle_Type1, szAmount_Class1);
                        else
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, TCT_Lib.iCurTC_Class.ToString(), TCT_Lib.fFareClass2.ToString("0"), "", szVehicle_Type2, szAmount_Class2);
                    }
                    catch (Exception err)
                    {
                        LogInfo.WriteLog("- Error Response Cancel from EMV CreditCard : " + err.Message + "\r\n" + err.StackTrace);
                    }
                }

                try
                {
                    //Console.WriteLine("+ After show Response");
                    frmEdcPaymentDlg.Dispose();
                    frmEdcPaymentDlg.Destroy();
                    frmEdcPaymentDlg        = null;
                    //Console.WriteLine("+ After Dispose");
                    this.Activate();
                    TCT_Lib.bActiveEMVDlg   = false;
                    LogInfo.WriteLog("+ Finish LaneController_EmvQrCodePaymentUI");
                }
                catch (Exception err)
                {
                    LogInfo.WriteLog("- Error Finish LaneController_EmvQrCodePaymentUI : " + err.Message + "\r\n" + err.StackTrace);
                }
            }
            else    // Edc Version 1
            {
                var frmEdcPaymentDlg    = new EdcPayQrDlg_V1(fPayAmount, TCT_Lib.iTowingTC_Class > 0);
                Gdk.Color colBk         = new Gdk.Color(0x3E, 0x3E, 0x3E);
                frmEdcPaymentDlg.ModifyBg(StateType.Normal, colBk);
                Console.WriteLine("+ Before Show dialog QR Code");
                ResponseType xResult    = (ResponseType) frmEdcPaymentDlg.Run();
                //frmEdcPaymentDlg.Destroy();

                TCT_Lib.eCurAppPage     = AppPage.Main;
                Console.WriteLine("+ After Call QR Code");

                if (xResult == ResponseType.Ok)
                {
                    Console.WriteLine("+ Show Response QR Code");
                    LogInfo.WriteLog("+ Response OK from QR Code");
                    var xCreditCardInfo         = frmEdcPaymentDlg.xOutCreditCard;

                    txtTag_PAN.Text             = $"<span font_size='large' fgcolor='#ffffff'>{"CreditCard No : " + xCreditCardInfo.szCardNo}</span>";
                    txtTag_PAN.UseMarkup        = true;
                    txtTag_PAN.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

                    txtTag_Balance.Text         = $"<span font_size='large' fgcolor='#ffffff'>{"Reference : " + xCreditCardInfo.szRef3}</span>";
                    txtTag_Balance.UseMarkup    = true;
                    txtTag_Balance.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

                    txtTag_Status.Text          = $"<span font_size='large' fgcolor='#ffffff'>{"Approve Code : " + xCreditCardInfo.szApprovalCode}</span>";
                    txtTag_Status.UseMarkup     = true;
                    txtTag_Status.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

                    imgTag.Pixbuf               = imgMobileQR;
                    TCT_Lib.laneController.SuccessEmvPayment(1);
                }
                else if (xResult == ResponseType.Reject)
                {
                    Console.WriteLine("+ Show Response Reject EMV QR Code");
                    LogInfo.WriteLog("+ Response Reject QR from TX not found");

                    txtTag_Balance.Text         = $"<span font_size='large' fgcolor='#ffff00'>ลูกค้ายังไม่ได้ Scan QR. โปรดจ่ายด้วย QR ใหม่อีกครั้ง.</span>";
                    txtTag_Balance.UseMarkup    = true;
                    txtTag_Balance.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
                }
                else
                {
                    Console.WriteLine("+ Show Response Cancel EMV QR Code");
                    LogInfo.WriteLog("+ Response Cancel from EMV QR Code");
                    try
                    {
                        if (TCT_Lib.iCurTC_Class == (int)VehicleType.CLASS_1)
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, TCT_Lib.iCurTC_Class.ToString(), TCT_Lib.fFareClass1.ToString("0"), "", szVehicle_Type1, szAmount_Class1);
                        else
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, TCT_Lib.iCurTC_Class.ToString(), TCT_Lib.fFareClass2.ToString("0"), "", szVehicle_Type2, szAmount_Class2);
                    }
                    catch (Exception err)
                    {
                        LogInfo.WriteLog("- Error Response Cancel from EMV CreditCard : " + err.Message + "\r\n" + err.StackTrace);
                    }
                }

                try
                {
                    //Console.WriteLine("+ After show Response");
                    frmEdcPaymentDlg.Dispose();
                    frmEdcPaymentDlg.Destroy();
                    frmEdcPaymentDlg        = null;
                    //Console.WriteLine("+ After Dispose");
                    this.Activate();
                    TCT_Lib.bActiveEMVDlg   = false;
                    LogInfo.WriteLog("+ Finish LaneController_EmvQrCodePaymentUI");
                }
                catch (Exception err)
                {
                    LogInfo.WriteLog("- Error Finish LaneController_EmvQrCodePaymentUI : " + err.Message + "\r\n" + err.StackTrace);
                }
            }
        });
    }

    void LaneController_EmvQrCodePaymentUI(decimal fPayAmount)
    {
        if (TCT_Lib.bActiveEMVDlg)
            return;

        Console.WriteLine("+ Start Call QR CODE");
        LogInfo.WriteLog("+ Start Call QR Code Payment Dialog");
        if (TCT_Lib.iEdcVersionType == 0)
        {
            var frmEdcPaymentDlg    = new EdcPayQrDlg(fPayAmount, TCT_Lib.iTowingTC_Class > 0);
            Gdk.Color colBk         = new Gdk.Color(0x3E, 0x3E, 0x3E);
            frmEdcPaymentDlg.ModifyBg(StateType.Normal, colBk);
            Console.WriteLine("+ Before Show dialog QR Code");
            ResponseType xResult    = (ResponseType) frmEdcPaymentDlg.Run();
            //frmEdcPaymentDlg.Destroy();

            TCT_Lib.eCurAppPage     = AppPage.Main;
            Console.WriteLine("+ After Call QR Code");

            if (xResult == ResponseType.Ok)
            {
                Console.WriteLine("+ Show Response QR Code");
                LogInfo.WriteLog("+ Response OK from QR Code");
                var xCreditCardInfo         = frmEdcPaymentDlg.xOutCreditCard;

                Application.Invoke(delegate
                {
                    txtTag_PAN.Text             = $"<span font_size='large' fgcolor='#ffffff'>{"CreditCard No : " + xCreditCardInfo.szCardNo}</span>";
                    txtTag_PAN.UseMarkup        = true;
                    txtTag_PAN.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

                    txtTag_Balance.Text         = $"<span font_size='large' fgcolor='#ffffff'>{"Reference : " + xCreditCardInfo.szRef3}</span>";
                    txtTag_Balance.UseMarkup    = true;
                    txtTag_Balance.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

                    txtTag_Status.Text          = $"<span font_size='large' fgcolor='#ffffff'>{"Approve Code : " + xCreditCardInfo.szApprovalCode}</span>";
                    txtTag_Status.UseMarkup     = true;
                    txtTag_Status.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

                    imgTag.Pixbuf               = imgMobileQR;
                });
                TCT_Lib.laneController.SuccessEmvPayment(1);
            }
            else
            {
                Console.WriteLine("+ Show Response Cancel EMV QR Code");
                LogInfo.WriteLog("+ Response Cancel from EMV QR Code");
                try
                {
                    if (TCT_Lib.iCurTC_Class == (int)VehicleType.CLASS_1)
                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, TCT_Lib.iCurTC_Class.ToString(), TCT_Lib.fFareClass1.ToString("0"), "", szVehicle_Type1, szAmount_Class1);
                    else
                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, TCT_Lib.iCurTC_Class.ToString(), TCT_Lib.fFareClass2.ToString("0"), "", szVehicle_Type2, szAmount_Class2);
                }
                catch (Exception err)
                {
                    LogInfo.WriteLog("- Error Response Cancel from EMV CreditCard : " + err.Message + "\r\n" + err.StackTrace);
                }
            }

            try
            {
                //Console.WriteLine("+ After show Response");
                frmEdcPaymentDlg.Dispose();
                frmEdcPaymentDlg.Destroy();
                frmEdcPaymentDlg        = null;
                //Console.WriteLine("+ After Dispose");
                this.Activate();
                TCT_Lib.bActiveEMVDlg   = false;
                LogInfo.WriteLog("+ Finish LaneController_EmvQrCodePaymentUI");
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Error Finish LaneController_EmvQrCodePaymentUI : " + err.Message + "\r\n" + err.StackTrace);
            }
        }
        else    // Edc Version 1
        {
            var frmEdcPaymentDlg    = new EdcPayQrDlg_V1(fPayAmount, TCT_Lib.iTowingTC_Class > 0);
            Gdk.Color colBk         = new Gdk.Color(0x3E, 0x3E, 0x3E);
            frmEdcPaymentDlg.ModifyBg(StateType.Normal, colBk);
            Console.WriteLine("+ Before Show dialog QR Code");
            ResponseType xResult    = (ResponseType) frmEdcPaymentDlg.Run();
            //frmEdcPaymentDlg.Destroy();

            TCT_Lib.eCurAppPage     = AppPage.Main;
            Console.WriteLine("+ After Call QR Code");

            if (xResult == ResponseType.Ok)
            {
                Console.WriteLine("+ Show Response QR Code");
                LogInfo.WriteLog("+ Response OK from QR Code");
                var xCreditCardInfo         = frmEdcPaymentDlg.xOutCreditCard;

                Application.Invoke(delegate
                {
                    txtTag_PAN.Text             = $"<span font_size='large' fgcolor='#ffffff'>{"CreditCard No : " + xCreditCardInfo.szCardNo}</span>";
                    txtTag_PAN.UseMarkup        = true;
                    txtTag_PAN.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

                    txtTag_Balance.Text         = $"<span font_size='large' fgcolor='#ffffff'>{"Reference : " + xCreditCardInfo.szRef3}</span>";
                    txtTag_Balance.UseMarkup    = true;
                    txtTag_Balance.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

                    txtTag_Status.Text          = $"<span font_size='large' fgcolor='#ffffff'>{"Approve Code : " + xCreditCardInfo.szApprovalCode}</span>";
                    txtTag_Status.UseMarkup     = true;
                    txtTag_Status.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

                    imgTag.Pixbuf               = imgMobileQR;
                });
                TCT_Lib.laneController.SuccessEmvPayment(1);
            }
            else if (xResult == ResponseType.Reject)
            {
                Console.WriteLine("+ Show Response Reject EMV QR Code");
                LogInfo.WriteLog("+ Response Reject QR from TX not found");

                txtTag_Balance.Text         = $"<span font_size='large' fgcolor='#ffff00'>ลูกค้ายังไม่ได้ Scan QR. โปรดจ่ายด้วย QR ใหม่อีกครั้ง.</span>";
                txtTag_Balance.UseMarkup    = true;
                txtTag_Balance.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
            }
            else
            {
                Console.WriteLine("+ Show Response Cancel EMV QR Code");
                LogInfo.WriteLog("+ Response Cancel from EMV QR Code");
                try
                {
                    if (TCT_Lib.iCurTC_Class == (int)VehicleType.CLASS_1)
                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, TCT_Lib.iCurTC_Class.ToString(), TCT_Lib.fFareClass1.ToString("0"), "", szVehicle_Type1, szAmount_Class1);
                    else
                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, TCT_Lib.iCurTC_Class.ToString(), TCT_Lib.fFareClass2.ToString("0"), "", szVehicle_Type2, szAmount_Class2);
                }
                catch (Exception err)
                {
                    LogInfo.WriteLog("- Error Response Cancel from EMV CreditCard : " + err.Message + "\r\n" + err.StackTrace);
                }
            }

            try
            {
                //Console.WriteLine("+ After show Response");
                frmEdcPaymentDlg.Dispose();
                frmEdcPaymentDlg.Destroy();
                frmEdcPaymentDlg        = null;
                //Console.WriteLine("+ After Dispose");
                this.Activate();
                TCT_Lib.bActiveEMVDlg   = false;
                LogInfo.WriteLog("+ Finish LaneController_EmvQrCodePaymentUI");
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Error Finish LaneController_EmvQrCodePaymentUI : " + err.Message + "\r\n" + err.StackTrace);
            }
        }
    }

    private void LaneContorller_UpdateUI_ClearQrWarningMsg()
    {
        Application.Invoke(delegate
        {
            txtTag_Balance.Text = "";
        });
    }

    private void LaneController_UpdateUI_DmtCardInfo(string szDmtCarNo, string szLicensePlate, string szDepartment, string szAffilliation, int iVehicleClass)
    {
        Application.Invoke(delegate
        {
            txtTag_PAN.Text             = $"<span font_size='large' fgcolor='#ffffff'>{"หมายเลขรถ : " + szDmtCarNo}</span>";
            txtTag_PAN.UseMarkup        = true;
            txtTag_PAN.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

            txtTag_Balance.Text         = $"<span font_size='large' fgcolor='#ffffff'>{"ทะเบียน : "+szLicensePlate+", ประเภท : " + iVehicleClass}</span>";
            txtTag_Balance.UseMarkup    = true;
            txtTag_Balance.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

            txtTag_Status.Text          = $"<span font_size='large' fgcolor='#ffffff'>{"หน่วยงาน : " + szDepartment + ", " + szAffilliation}</span>";
            txtTag_Status.UseMarkup     = true;
            txtTag_Status.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
        });
    }

    void LaneController_UpdateUI_ClearPanelTagInfo()
    {
        Application.Invoke(delegate
        {
            txtTag_PAN.Text         = "";
            txtTag_Balance.Text     = "";
            txtTag_Status.Text      = "";
        });
    }

    void LaneController_MessageProcessUI(string szMsg)
    {
        Application.Invoke(delegate
        {
            bProcess        = true;
            szProcess       = szMsg;
            areaProcess.ModifyBg(StateType.Normal, clGreen);
            areaProcess.QueueDraw();
        });
    }

    void LaneController_ShowModeType(string szModeType)
    {
        Application.Invoke(delegate
        {
            txtModeType.UseMarkup = true;
            if (szModeType.ToUpper() == "ETC")
                txtModeType.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
            else
                txtModeType.ModifyFg(StateType.Normal, new Gdk.Color(0x44, 0x44, 0x44));
            txtModeType.Text = szModeType;
        });
    }

    void LaneController_UpdateUI_Convoy(int iClass)
    {
        Application.Invoke(delegate
        {
            UpdateUI_Convoy();
        });
    }

    private void UpdateUI_Convoy()
    {
        LogInfo.WriteLaneEvent($"+ UpdateUI_Convoy : bConvoyMode={TCT_Lib.bConvoyMode}, bAllowConvoy={TCT_Lib.bAllowConvoy}");
        if (TCT_Lib.bConvoyMode && TCT_Lib.bAllowConvoy)
        {
            txtConvoy.Text = TCT_Lib.iConvoyClass1 + " + " + TCT_Lib.iConvoyClass2;
            LogInfo.WriteLog("+ Convoy : " + TCT_Lib.iConvoyClass1 + " + " + TCT_Lib.iConvoyClass2);
            LogInfo.WriteLaneEvent("+ Convoy : " + TCT_Lib.iConvoyClass1 + " + " + TCT_Lib.iConvoyClass2);

            if ((TCT_Lib.iConvoyClass1 + TCT_Lib.iConvoyClass2) <= 0)
            {
                LogInfo.WriteLaneEvent("- Sum1 Convoy Vehichle <= 0");
                //LogInfo.WriteLog("+ End Convoy");
                //TCT_Lib.bConvoyMode = false;
                lblConvoy.Visible   = txtConvoy.Visible = false;
                txtModeType.Visible = true;
            }
            else
            {
                LogInfo.WriteLaneEvent($"+ Convoy Vehichle Class1 = {TCT_Lib.iConvoyClass1}, Class2 = {TCT_Lib.iConvoyClass2}");
                lblConvoy.Visible   = txtConvoy.Visible = true;
                txtConvoy.Text      = TCT_Lib.iConvoyClass1 + " + " + TCT_Lib.iConvoyClass2;
                txtModeType.Visible = false;
            }
        }
        else
        {
            LogInfo.WriteLaneEvent("- Not Convoy Mode or Not Allow Convoy");
            if ((TCT_Lib.iConvoyClass1 + TCT_Lib.iConvoyClass2) <= 0)
            {
                LogInfo.WriteLaneEvent("- Sum2 Convoy Vehichle <= 0");
                lblConvoy.Visible       = txtConvoy.Visible = false;
                txtModeType.Visible     = true;
            }
        }
    }
   
    void LaneController_PrintConvoy()
    {
        //Application.Invoke(delegate
        {
            if (TCT_Lib.bActiveConvoyDlg)
                return;

            var frmPrintConvoyDlg   = new PrintConvoyPaymentDlg();
            Gdk.Color colBk         = new Gdk.Color(0x3E, 0x3E, 0x3E);
            frmPrintConvoyDlg.ModifyBg(StateType.Normal, colBk);
            ResponseType xResult    = (ResponseType)frmPrintConvoyDlg.Run();
            //Console.WriteLine("+ After Dispose");

            if (xResult == ResponseType.Ok)
            {
                TCT_Lib.bConvoyMode         = true;
                TCT_Lib.iConvoyClass1       = TCT_Lib.iTotalConvoyClass1 = frmPrintConvoyDlg.iOutTotalClass1;
                TCT_Lib.iConvoyClass2       = TCT_Lib.iTotalConvoyClass2 = frmPrintConvoyDlg.iOutTotalClass2;
                TCT_Lib.iConvoyPayTypeID    = frmPrintConvoyDlg.iOutPayType;
                TCT_Lib.iConvoyEmvDetailID  = frmPrintConvoyDlg.iOutEmvDetailID;

                //lblConvoy.Visible       = txtConvoy.Visible = true;
                //txtConvoy.Text          = TCT_Lib.iConvoyClass1 + " + " + TCT_Lib.iConvoyClass2;
                //txtModeType.Visible     = false;
                //UpdateUI_Convoy();

                Console.WriteLine("Finish Total Vehicles Convoy : " + frmPrintConvoyDlg.iOutTotalVehicle);
                LogInfo.WriteLaneEvent("Finish Total Vehicles Convoy : " + frmPrintConvoyDlg.iOutTotalVehicle);
            }
            try
            {
                TCT_Lib.eCurAppPage         = AppPage.Main;
                TCT_Lib.bActiveConvoyDlg    = false;
                frmPrintConvoyDlg.Dispose();
                frmPrintConvoyDlg.Destroy();
                frmPrintConvoyDlg           = null;
                this.Activate();
                UpdateUI_Convoy();
                LogInfo.WriteLog("+ Finish LaneController_PrintConvoy");
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Error Finish LaneController_PrintConvoy : " + err.Message + "\r\n" + err.StackTrace);
            }
        }
        //);
    }

    void LaneController_SalesCoupon()
    {
        Application.Invoke(delegate
        {
            if (TCT_Lib.bActiveSalesCouponDlg)
                return;

            //----------------------------------------
            //--- Check current is online, if online then open dialog for sales coupon, else ignore sales coupon
            if (!TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szLaneWebService_ServerIPAddress, TCT_Lib.iLaneWebService_ServerPort))
            {
                Console.WriteLine($"Fail Connect LaneWebService IP={TCT_Lib.szLaneWebService_ServerIPAddress}, Port={TCT_Lib.iLaneWebService_ServerPort}");
                LogInfo.WriteLog($"Fail Connect LaneWebService IP={TCT_Lib.szLaneWebService_ServerIPAddress}, Port={TCT_Lib.iLaneWebService_ServerPort}");
                LogInfo.WriteLaneEvent($"Fail Connect LaneWebService IP={TCT_Lib.szLaneWebService_ServerIPAddress}, Port={TCT_Lib.iLaneWebService_ServerPort}");

                string str = @"ไม่สามารถทำการติดต่อ TA Server เพื่อขายคูปองได้. โปรดตรวจสอบระบบ Network TA Server.";
                MessageDialog md = new MessageDialog(this, DialogFlags.DestroyWithParent,
                MessageType.Error, ButtonsType.Ok, true,
                "<span font_size='large' fgcolor='#000000'>" + str + "</span >");
                         //"ไม่สามารถทำการติดต่อ TA Server เพื่อขายคูปองได้.\r\n\r\nโปรดตรวจสอบระบบ Network TA Server.");
                UiTheme.StylePopup(md);     // v2.0.2: no title bar + visible frame
                md.Run();
                md.Destroy();

                return;
            }
            //--------------------------------------------
            object objParams = new
            {
                Action = 1,
            };
            TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToClient("SALES_COUPON", objParams);
            //---------------------------------------------------------
            string szErrMsg = "";
            WebService_SalesCoupons.CallWebService_GetCoupons(ref szErrMsg);
            //---------------------------------------------------------------

            var frmSalesCoupon      = new Sales_CouponDlg();
            Gdk.Color colBk         = new Gdk.Color(0x3E, 0x3E, 0x3E);
            frmSalesCoupon.ModifyBg(StateType.Normal, colBk);
            frmSalesCoupon.Run();
            //frmSalesCoupon.Dispose();
            //Console.WriteLine("+ After Dispose");
            TCT_Lib.eCurAppPage     = AppPage.Main;
            frmSalesCoupon.Dispose();
            frmSalesCoupon.Destroy();
            frmSalesCoupon          = null;
            this.Activate();
            TCT_Lib.bActiveSalesCouponDlg = false;
            //-------------------------------------------------------------
            objParams = new
            {
                Action = 0,
            };
            TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToClient("SALES_COUPON", objParams);
            LogInfo.WriteLog("+ Finish LaneController_SalesCoupon");
        });
    }

    protected void LaneController_CheckNetworkStatus(int iDeviceType, bool bIsConnected)
    {
        Application.Invoke(delegate
        {
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
                if (!TCT_Lib.bEnableEdc)
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
                        TCT_Lib.bDC_NetworkStatus = true;
                        sbDcStatus.Text = "<span fgcolor='#00ff00'>DC : Connect</span>";
                    }
                    else
                    {
                        TCT_Lib.bDC_NetworkStatus = false;
                        sbDcStatus.Text = "<span fgcolor='#FF5500'>DC : DisConnect</span>";
                    }
                }
                sbDcStatus.UseMarkup    = true;
                TCT_Lib.bDC_Connected   = bIsConnected;
            }
        });
    }

    void LaneController_NextVehicleTypeLaneControl(VehicleType eVehicleType)
    {
        Application.Invoke(delegate {
            if (eVehicleType == VehicleType.NONE)
            {
                UpdateUI_NextVehicleType(null, "", "", "");
            }
            else if (eVehicleType == VehicleType.CLASS_1)
            {
                UpdateUI_NextVehicleType(imgVehicleType1, szVehicle_Type1, szAmount_Class1, szTxt_CashCoupon);
            }
            else if (eVehicleType == VehicleType.CLASS_2)
            {
                UpdateUI_NextVehicleType(imgVehicleType2, szVehicle_Type2, szAmount_Class2, szTxt_CashCoupon); 
            }
            else if (eVehicleType == VehicleType.EXEMPT)
            {
                UpdateUI_NextVehicleType(imgVehicleEmergency, szVehicle_Exempt, szAmount_0, szPay_Exempt);
                //UpdateUI_PayType(szPay_Exempt, szTxt_EditType);
            }
            else if (eVehicleType == VehicleType.LEFT_EXIT)
            {
                UpdateUI_NextVehicleType(imgVehicleType1, szVehicle_LeftExit, szAmount_0, szPay_LeftExit);
                //UpdateUI_PayType(szPay_Exempt, szTxt_EditType);
            }
        });
    }

    protected void UpdateUI_NextVehicleType(Gdk.Pixbuf imgVehicleType, string szTxtVehicleType, string szTxtVehicleAmount, string szTxtProcess)
    {
        if (TCT_Lib.eTowingState != TowingState.None)
        {
            if (TCT_Lib.eTowingState == TowingState.StartTowing)
                TCT_Lib.eTowingState = TowingState.FirstClassType;
            else if (TCT_Lib.eTowingState == TowingState.FirstPayType)
                TCT_Lib.eTowingState = TowingState.SecClassType;

            UpdateUI_VehicleTowing();
        }
        else
        {
            if (imgVehicleType == null)
            {
                imgNextCar.Pixbuf = null;
            }
            else
            {
                Assembly myAssembly     = Assembly.GetExecutingAssembly();
                Fixed.FixedChild pos    = ((global::Gtk.Fixed.FixedChild)(this.fixed2[this.imgNextCar]));
                pos.X                   = 50;
                pos.Y                   = 200;

                imgNextCar.SetSizeRequest(128, 86);
                Gdk.Pixbuf pictureBuf   = imgVehicleType;
                imgNextCar.Pixbuf       = pictureBuf.ScaleSimple(128, 86, Gdk.InterpType.Bilinear);
            }

            txtQueue.Text               = TCT_Lib.iTotalQueue.ToString("0");
            txtQueue.UseMarkup          = true;
        }
    }

    void LaneController_UpdateUiSupervisor()
    {
        Application.Invoke(delegate
        {
            txtSupervisorName.Text = TCT_Lib.szSupervisorName;
        });
    }

    void LaneController_UpdateUiStaff()
    {
        Application.Invoke(delegate
        {
            string szLoginType = "";
            if (loginType == LoginType.Manual)
                szLoginType = "Manual Key";
            else if (loginType == LoginType.SmartCard)
                szLoginType = "Smart Card";
            else if (loginType == LoginType.RTO_Remote)
                szLoginType = "RTO Remote";
            else if (loginType == LoginType.ETC_Auto)
                szLoginType = "Auto ETC";
            else if (loginType == LoginType.Maintenance)
                szLoginType = "Maintenance";

            txtStaffID.Text     = TCT_Lib.szStaffID + "   [ " + szLoginType + " ]";
            txtStaffName.Text   = TCT_Lib.szStaffName;
            txtStaffGroup.Text  = TCT_Lib.szStaffGroupName;
            txtStartShift.Text  = TCT_Lib.dtStaffStartShift.ToString("dd-MM-yyyy HH:mm:ss", DateTimeExtension.ThaiCultureInfo);

            //txtJobNo.Text               = TCT_Lib.LaneDB.GetStartJobNo().ToString("0") + (TCT_Lib.bMaintenanceMode ? "     (Maintenance)" : "");
            txtJobNo.Text = TCT_Lib.iJobNo.ToString("0") + (TCT_Lib.bMaintenanceMode ? "     (Maintenance)" : "");
        });
    }

    void LaneController_LaneStateEtcPayCardPAN(dynamic vehicleETC)
    {
        Application.Invoke(delegate
        {
            //LogInfo.WriteLog("Main UI : Draw ETC PAN");
            OnDrawETC_LaneStatePayCardPAN(vehicleETC);
        });
    }

    protected void OnDrawETC_LaneStatePayCardPAN(dynamic vehicleETC)
    {
        string szPaymentStatus  = vehicleETC["paymentStatus"];

        string szRejectReason = "", szPAN = "", szBalance = "";
        bool bMCard         = false;
        decimal fBalance    = 0;
        int iVehicleClass   = 0;
        if (vehicleETC["obu"] != null)
        {
            szRejectReason  = vehicleETC["obu"]["rejectReason"] ?? "";
            szPAN           = vehicleETC["obu"]["paymentMeans"]["pan"];
            bMCard          = szPAN.StartsWith("636685", StringComparison.Ordinal);
            szBalance       = vehicleETC["obu"]["balanceObu"];
            fBalance        = szBalance.Length > 2 ? decimal.Parse(szBalance.Remove(szBalance.Length-2)) : 0;
            iVehicleClass   = vehicleETC["obu"]["vehicleClass"] ?? 0;
        }
        else
        {
            szRejectReason = szPaymentStatus;
        }

        LogInfo.WriteLog($"+ Found PAN={szPAN}, Balance={fBalance.ToString("00")}, PayStatus={szPaymentStatus}, RejectReason={szRejectReason}");
        //szPayType = szPaymentStatus;
        if (szPAN.StartsWith("636685", StringComparison.Ordinal))
            szPayType = szPay_MPass;
        else if (szPAN.StartsWith("308586", StringComparison.Ordinal))
            szPayType = szPay_EasyPass;
        else if (szPAN.StartsWith("304592", StringComparison.Ordinal))
            szPayType = szPay_EasyPass;

        // ---Area PayType
        bPayType        = true;
        //szPayType       = bMCard ? szPay_MPass : szPay_EasyPass;
        areaPayType.ModifyBg(StateType.Normal, clBlue);
        areaPayType.QueueDraw();

        bProcess        = true;
        szProcess       = szTxt_Processing;
        areaProcess.ModifyBg(StateType.Normal, clGreen);
        areaProcess.QueueDraw();

        //Fixed.FixedChild pos    = ((global::Gtk.Fixed.FixedChild)(this.fixed2[this.imgCarType]));
        //pos.X += 120;

        //-----------------------------------------------------------
        if (szPAN != "")
        {
            txtTag_PAN.Text             = $"<span font_size='large' fgcolor='#ffffff'>PAN : {szPAN}</span>";
            txtTag_PAN.UseMarkup        = true;
            txtTag_PAN.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

            txtTag_Balance.Text         = $"<span font_size='large' fgcolor='#ffffff'>BALANCE : {fBalance.ToString("N")} บาท</span>";
            txtTag_Balance.UseMarkup    = true;
            txtTag_Balance.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
        }
        else
        {
            txtTag_PAN.Text             = $"<span font_size='large' fgcolor='#ffffff'>PAN : --- </span>";
            txtTag_PAN.UseMarkup        = true;
            txtTag_PAN.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

            txtTag_Balance.Text         = $"<span font_size='large' fgcolor='#ffffff'>BALANCE : --- </span>";
            txtTag_Balance.UseMarkup    = true;
            txtTag_Balance.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
        }

        if (szPaymentStatus.ToUpper() == "ETC_PAYMENT_OK".ToUpper())
        {
            txtTag_Status.Text  = "";
            imgTag.Pixbuf       = bMCard ? imgTagMPass : imgTagEasyPass;
            imgTag.Visible      = true;
        }
        else
        {
            string szTagStatus      = szPaymentStatus;
            if (szRejectReason != "None")
                szTagStatus += " - " + szRejectReason;
            txtTag_Status.Text      = $"<span font_size='large' fgcolor='#ff8000'>TAG STATUS : {szTagStatus}</span>";
            txtTag_Status.UseMarkup = true;
            txtTag_Status.ModifyFg(StateType.Normal, new Gdk.Color(0xff, 0x80, 0x00));

            imgTag.Pixbuf       = null;
            imgTag.Visible      = false;
        }

        areaViolation.Visible   = false;

        if (szPaymentStatus == "OBU_NOT_PRESENT")
            return;

        //=======================================================================================
        Assembly myAssembly         = Assembly.GetExecutingAssembly();
        imgCarType.Pixbuf           = imgVehicleType1;

        bClassType                  = true;
        szClassType                 = iVehicleClass == 9 ? szVehicle_Exempt : szVehicle_Type1;
        areaClassType.ModifyBg(StateType.Normal, clBlue);
        areaClassType.QueueDraw();

        //--- Area ClassAmount
        bClassAmount                = true;
        szClassAmount               = iVehicleClass == 9 ? szAmount_0 : szAmount_Class1;
        areaClassAmount.ModifyBg(StateType.Normal, clBlue);
        areaClassAmount.QueueDraw();

        bProcess                    = true;
        szProcess                   = szTxt_Processing;
        areaProcess.ModifyBg(StateType.Normal, clGreen);
        areaProcess.QueueDraw();

        txtClassType.Text           = "<span font_size='large' fgcolor='#ffffff'>" + (iVehicleClass == 9 ? szVehicle_Exempt : szVehicle_Type1) + "</span>";
        txtClassType.UseMarkup      = true;
        //txtClassType.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

        txtClassAmount.Text         = "<span font_size='large' fgcolor='#ffffff'>" + (iVehicleClass == 9 ? szAmount_0 :  szAmount_Class1) + "</span>";
        txtClassAmount.UseMarkup    = true;
        //txtClassAmount.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));

        txtQueue.Text               = TCT_Lib.iTotalQueue.ToString("0");
        txtQueue.UseMarkup          = true;
    }

    void LaneController_CompilePassageETC(dynamic compilePassage)
    {
        Application.Invoke(delegate
        {

        });
    }

    void LaneController_ShowTotalLoop()
    {
        Application.Invoke(delegate
        {
            try
            {
                if (TCT_Lib.xListLoopCar != null)
                {
                    if (TCT_Lib.xListLoopCar.Count == 0)
                        txtTotalLoop.Text = "<span font_size='small' fgcolor='#444444'>0</span>";
                    else
                    {
                        if (TCT_Lib.eTowingState == TowingState.None)
                        {
                            if (TCT_Lib.xListLoopCar.Count > 0)
                                txtTotalLoop.Text = "<span font_size='large' fgcolor='#ffffff'>" + TCT_Lib.GetClassTypeShortName(TCT_Lib.xListLoopCar[TCT_Lib.xListLoopCar.Count - 1].iTC_Class) + "</span>";
                        }
                        else
                        {
                            txtTotalLoop.Text = "<span font_size='large' fgcolor='#ffffff'>" + "T" + "</span>";
                        }
                    }
                    txtTotalLoop.UseMarkup = true;
                }
            }
            catch (Exception err)
            {
                Console.Write(err.Message);
            }
        });
    }

    protected void OnBtnSimulationClicked_1(object sender, EventArgs e)
    {
#if !__FULL_SCREEN__
        Application.Invoke(delegate
        {

            string szMsg            = szListLaneState[idxLaneState];
            dynamic objLaneState    = JsonConvert.DeserializeObject(szMsg);

            TCT_Lib.laneController.EtcRabbitMQ_LaneStateFromEtcRabbitMQ(objLaneState);
            idxLaneState++;

            if (idxLaneState == 10)
                idxLaneState += 115;

            if (idxLaneState >= szListLaneState.Length)
                idxLaneState = 0;

/*
            if (idxLaneState == 0)
                idxLaneState = 1;
            string szMsg = szListCompilePassage[idxLaneState];
            dynamic objCompilePassage = JsonConvert.DeserializeObject(szMsg);

            try
            {
                int iTransID = (int)TCT_Lib.LaneDB.WriteLaneTransaction_ETC_CompliedPassage(objCompilePassage);
                TCT_Lib.SendLaneTransObjToRTO(iTransID);
                bool bResult = TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("ETC_COMPILE_PASSAGE", objCompilePassage);


                idxLaneState++;

                //if (idxLaneState == 10)
                //    idxLaneState += 115;

                if (idxLaneState >= szListCompilePassage.Length)
                    idxLaneState = 0;
            }
            catch (Exception err)
            {
                Console.WriteLine("+ Error : " + err);
            }
*/

        });
#endif
    }

    private void LaneController_DmtPayment()
    {
        Application.Invoke(delegate
        {
            if (TCT_Lib.bActiveDmtPaymentDlg)
                return;

            //----------------------------------------
            var frmDmtPayment      = new DMT_PaymentDlg();
            Gdk.Color colBk         = new Gdk.Color(0x3E, 0x3E, 0x3E);
            frmDmtPayment.ModifyBg(StateType.Normal, colBk);
            ResponseType xResult = (ResponseType)frmDmtPayment.Run();
            //Console.WriteLine("+ After Dispose");

            if (xResult == ResponseType.Ok)
            {

            }
            //frmSalesCoupon.Dispose();
            //Console.WriteLine("+ After Dispose");
            TCT_Lib.eCurAppPage     = AppPage.Main;
            frmDmtPayment.Dispose();
            frmDmtPayment.Destroy();
            frmDmtPayment = null;
            this.Activate();
            TCT_Lib.bActiveDmtPaymentDlg = false;
            //-------------------------------------------------------------
        });
    }

    private bool CheckEdcHankShaking()
    {
        if (TCT_Lib.bEnableEdc)
        {
            if (!TCT_Lib.bEdcProcessing)
            {
                if (TCT_Lib.iEdcVersionType == 1)
                {
                    var xEdcHandshaking_V1 = new EdcHandShaking_V1();
                    return xEdcHandshaking_V1.bOutResult;
                }
            }
            else
            {
                LogInfo.WriteLogEDC("- EDC is processing, then ignore HandshakingProcess.");
                return false;
            }
        }

        return false;
    }

}
