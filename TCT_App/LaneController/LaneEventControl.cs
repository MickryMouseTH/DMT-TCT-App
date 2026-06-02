//#define __SIM__

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Gtk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TCT_App.EMV.EMV_V1;
using TCT_App.EMV.V0;
using TCT_App.LaneView.MiscDlg;
using TCT_App.LSD;
using TCT_App.WebServiceAPI;

namespace TCT_App
{
    public class LaneEventController
    {
        //--- Declare delegate and event
        public delegate void LaneControlDigitalInput(int[] values);
        public event LaneControlDigitalInput digitalInputLaneControl;

        public delegate void LaneControlDigitalOutput(int[] values);
        public event LaneControlDigitalOutput digitalOutputLaneControl;

        public delegate void LaneControlVehicleType(VehicleType eVehicleType);
        public event LaneControlVehicleType vehicleTypeLaneControl;

        public delegate void LaneControlPayType(PayType ePayType);
        public event LaneControlPayType payTypeLaneControl;

        public delegate void LaneControlShowAVC(int avcClassType);
        public event LaneControlShowAVC showAvcLaneControl;

        public delegate void LaneControlShowQueueALB(int queueALB);
        public event LaneControlShowQueueALB showQueueAlbControl;

        public delegate void LaneControlShow_nTrx(int nTrx);
        public event LaneControlShow_nTrx show_nTrxControl;

        public delegate void LaneControlClearPanelUI();
        public event LaneControlClearPanelUI clearPanelUiLaneControl;

        public delegate void LaneControlClearQueue();
        public event LaneControlClearQueue clearQueueLaneControl;

        public delegate void LaneControlUpdateUiQueue();
        public event LaneControlUpdateUiQueue updateUiQueueLaneControl;

        public delegate void LaneControlEditBackClassType();
        public event LaneControlEditBackClassType editBackClassType;

        public delegate void LaneControlEditBackPayType();
        public event LaneControlEditBackPayType editBackPayType;

        public delegate void LaneControlEventViolation();
        public event LaneControlEventViolation eventViolation;

        public delegate void LaneControlEventDIS();
        public event LaneControlEventDIS eventDIS;

        public delegate void LaneControlNextVehicleType(VehicleType eVehicleType);
        public event LaneControlNextVehicleType nextVehicleTypeLaneControl;

        public delegate void LaneControlMessageProcessUI(string szMsg);
        public event LaneControlMessageProcessUI messageProcessUI;

        public delegate void LaneControlShowModeType(string szModeType);
        public event LaneControlShowModeType showModeType;

        public delegate void LaneControlShowTotalLoop();
        public event LaneControlShowTotalLoop showTotalLoop;

        public delegate void LaneControlUpdateUI_TowingFirstPass();
        public event LaneControlUpdateUI_TowingFirstPass updateUI_TowingFirstPass;

        public delegate void LaneControlUpdateUI_Convoy(int iClass);
        public event LaneControlUpdateUI_Convoy updateUI_Convoy;

        //--------------------------------------------------
        public delegate void LaneControlStartManaulStaffLogin();
        public event LaneControlStartManaulStaffLogin startManualStaffLogin;

        public delegate void LaneControlCancelStartManualStaffLogin();
        public event LaneControlCancelStartManualStaffLogin cancelManualStaffLogin;

        public delegate void LaneControlManualKeyStaffCode(string szStaffCode);
        public event LaneControlManualKeyStaffCode manualKeyStaffCode;

        public delegate void LaneControlManualKeyStaffPassword(string szStaffPassword);
        public event LaneControlManualKeyStaffPassword manualKeyStaffPassword;

        public delegate void LaneControlManualStaffLoginStatus(bool bResult, string szErrMsg);
        public event LaneControlManualStaffLoginStatus manualStaffLoginStatus;

        public delegate void LaneControlLoginSmartCard(string strVal);
        public event LaneControlLoginSmartCard loginWithSmartCardReader;

        public delegate void LaneControlMaintenanceSmartCard(string strVal);
        public event LaneControlMaintenanceSmartCard maintenanceSmartCardReader;

        public delegate void LaneControlDmtPaymentSmartCard(string strVal);
        public event LaneControlDmtPaymentSmartCard dmtPaymentSmartCardReader;

        public delegate void LaneControlChangeLaneType();
        public event LaneControlChangeLaneType changeCurrentLaneType;

        public delegate void LaneControlRemoteLogin(string staffCode);
        public event LaneControlRemoteLogin remoteLoginFromRTO;

        public delegate void LaneControlManualLoginKey(int iKey);
        public event LaneControlManualLoginKey manualLoginKey;

        public delegate void LaneControlEndShift();
        public event LaneControlEndShift endShiftLaneControl;

        public delegate void LaneControlUpdateSupervisor();
        public event LaneControlUpdateSupervisor updateUiSupervisor;

        public delegate void LaneControlUpdateStaff();
        public event LaneControlUpdateStaff updateUiStaff;

        public delegate void LaneControlEmvCreditPayment(decimal fPayAmount);
        public event LaneControlEmvCreditPayment emvCreditPaymentUI;

        public delegate void LaneControlEmvQrCodePayment(decimal fPayAmount);
        public event LaneControlEmvQrCodePayment emvQrCodePaymentUI;

        public delegate void LaneControlSalesCoupon();
        public event LaneControlSalesCoupon salesCoupon;

        public delegate void LaneControlPrintConvoy();
        public event LaneControlPrintConvoy printConvoy;

        public delegate void LaneControlManageProcessEMV(int iKey);
        public event LaneControlManageProcessEMV manageProcessEMV;

        public delegate void LaneControlManageProcessPayConvoy(int iKey);
        public event LaneControlManageProcessPayConvoy manageProcessPayConvoy;

        public delegate void LaneControlManageProcessSalesCoupon(int iKey);
        public event LaneControlManageProcessSalesCoupon manageProcessSalesCoupon;

        public delegate void LaneControlMaintenanceProcess(int iKey);
        public event LaneControlMaintenanceProcess maintenanceProcess;

        public delegate void LaneControlManageSetLaneTypeMode(int iKey);
        public event LaneControlManageSetLaneTypeMode manageProcessSetLaneTypeMode;

        public delegate void LaneControlManageProcessDmtPayment(int iKey);
        public event LaneControlManageProcessDmtPayment manageProcessDmtPayment;

        public delegate void LaneControlChkNetworkStatus(int iDeviceType, bool bIsConnected);
        public event LaneControlChkNetworkStatus checkNetworkStatus;

        public delegate void LaneControlETC_LaneStatePayCardPAN(dynamic vehicleETC);
        public event LaneControlETC_LaneStatePayCardPAN laneStateEtcPayCardPAN;

        public delegate void LaneControlETC_CompilePassageRecord(dynamic compilePassage);
        public event LaneControlETC_CompilePassageRecord compilePassageETC;

        public delegate void LaneControlETC_DrawETC_AlbMode(int iAlbMode);
        public event LaneControlETC_DrawETC_AlbMode drawETC_AlbMode;

        public delegate void LaneControl_SetLaneTypeMode();
        public event LaneControl_SetLaneTypeMode setLaneTypeMode;

        public delegate void LaneControl_ExitMaintenanceMode();
        public event LaneControl_ExitMaintenanceMode exitMaintenanceMode;

        public delegate void LaneControl_UpdateUI_DmtCardInfo(string szDmtCarNo, string szLicensePlate, string szDepartment, string szAffilliation, int iVehicleClass);
        public event LaneControl_UpdateUI_DmtCardInfo updateUI_DmtCardInfo;

        public delegate void LaneControl_UpdateUI_ClearPanelTagInfo();
        public event LaneControl_UpdateUI_ClearPanelTagInfo updateUI_ClearPanelTagInfo;

        public delegate void LaneControl_ClearQrWarningMsg();
        public event LaneControl_ClearQrWarningMsg updateUI_ClearQrWarningMsg;

        //-----------------------------------------------------------------------------------
        static readonly HashSet<int> kLoginState = new HashSet<int>
        {
            (int)KeyPressedType.NUM_0, (int)KeyPressedType.NUM_1, (int)KeyPressedType.NUM_2, (int)KeyPressedType.NUM_3,
            (int)KeyPressedType.NUM_4, (int)KeyPressedType.NUM_5, (int)KeyPressedType.NUM_6, (int)KeyPressedType.NUM_7,
            (int)KeyPressedType.NUM_8, (int)KeyPressedType.NUM_9, (int)KeyPressedType.MANUAL, (int)KeyPressedType.ESC,
            (int)KeyPressedType.LEFT, (int)KeyPressedType.RIGHT, (int)KeyPressedType.UP, (int)KeyPressedType.DOWN,
            (int)KeyPressedType.ENTER, (int)KeyPressedType.TAB, (int)KeyPressedType.BACKSPACE, (int)KeyPressedType.END,
            (int)KeyPressedType.FORCE_RESET, (int)KeyPressedType.EDIT, (int)KeyPressedType.MODE
        };
        static readonly HashSet<int> kSetLaneTypeMode = new HashSet<int>
        {
            (int)KeyPressedType.NUM_1, (int)KeyPressedType.NUM_2, (int)KeyPressedType.NUM_3,
            (int)KeyPressedType.EDIT, (int)KeyPressedType.ESC
        };
        static readonly HashSet<int> kPayConvoy = new HashSet<int>
        {
            (int)KeyPressedType.NUM_0, (int)KeyPressedType.NUM_1, (int)KeyPressedType.NUM_2, (int)KeyPressedType.NUM_3,
            (int)KeyPressedType.NUM_4, (int)KeyPressedType.NUM_5, (int)KeyPressedType.NUM_6, (int)KeyPressedType.NUM_7,
            (int)KeyPressedType.NUM_8, (int)KeyPressedType.NUM_9, (int)KeyPressedType.MANUAL, (int)KeyPressedType.ESC,
            (int)KeyPressedType.LEFT, (int)KeyPressedType.RIGHT, (int)KeyPressedType.UP, (int)KeyPressedType.DOWN,
            (int)KeyPressedType.ENTER, (int)KeyPressedType.TAB, (int)KeyPressedType.BACKSPACE, (int)KeyPressedType.END,
            (int)KeyPressedType.PAY_CASH, (int)KeyPressedType.PAY_EMV, (int)KeyPressedType.PAY_QRCODE,
            (int)KeyPressedType.PAY_PM6,  (int)KeyPressedType.PAY_PM7,  (int)KeyPressedType.PAY_PM8,  (int)KeyPressedType.PAY_PM9,
            (int)KeyPressedType.CONFIRM, (int)KeyPressedType.RECEIPT, (int)KeyPressedType.EDIT,
            (int)KeyPressedType.OTL_OPEN, (int)KeyPressedType.OTL_CLOSE
        };
        static readonly HashSet<int> kSalesCoupon = new HashSet<int>
        {
            (int)KeyPressedType.NUM_0, (int)KeyPressedType.NUM_1, (int)KeyPressedType.NUM_2, (int)KeyPressedType.NUM_3,
            (int)KeyPressedType.NUM_4, (int)KeyPressedType.NUM_5, (int)KeyPressedType.NUM_6, (int)KeyPressedType.NUM_7,
            (int)KeyPressedType.NUM_8, (int)KeyPressedType.NUM_9, (int)KeyPressedType.MANUAL, (int)KeyPressedType.ESC,
            (int)KeyPressedType.LEFT, (int)KeyPressedType.RIGHT, (int)KeyPressedType.UP, (int)KeyPressedType.DOWN,
            (int)KeyPressedType.ENTER, (int)KeyPressedType.TAB, (int)KeyPressedType.BACKSPACE, (int)KeyPressedType.END,
            (int)KeyPressedType.PAY_CASH, (int)KeyPressedType.PAY_EMV, (int)KeyPressedType.PAY_QRCODE,
            (int)KeyPressedType.PAY_PM6,  (int)KeyPressedType.PAY_PM7,  (int)KeyPressedType.PAY_PM8,  (int)KeyPressedType.PAY_PM9,
            (int)KeyPressedType.CONFIRM, (int)KeyPressedType.RECEIPT, (int)KeyPressedType.EDIT, (int)KeyPressedType.DOT,
            (int)KeyPressedType.OTL_OPEN, (int)KeyPressedType.OTL_CLOSE
        };
        static readonly HashSet<int> kBeginJobState = new HashSet<int>
        {
            (int)KeyPressedType.OTL_OPEN, (int)KeyPressedType.OTL_CLOSE, (int)KeyPressedType.END_SHIFT, (int)KeyPressedType.BEOJ,
        };
        static readonly HashSet<int> kAllowClassType = new HashSet<int>
        {
            (int)KeyPressedType.CLASS_1, (int)KeyPressedType.CLASS_2, (int)KeyPressedType.CLASS_3, (int)KeyPressedType.CLASS_4,
            (int)KeyPressedType.CLASS_5, (int)KeyPressedType.CLASS_6, (int)KeyPressedType.CLASS_7, (int)KeyPressedType.CLASS_8,
            (int)KeyPressedType.CLASS_9, (int)KeyPressedType.EDIT,  (int)KeyPressedType.PRINT_CONVOY,
            (int)KeyPressedType.VIOLATION, (int)KeyPressedType.SALE_COUPON, (int)KeyPressedType.FORCE_RESET,
            (int)KeyPressedType.OTL_OPEN, (int)KeyPressedType.OTL_CLOSE, (int)KeyPressedType.END_SHIFT
        };
        static readonly HashSet<int> kAllowPayType = new HashSet<int>
        {
            (int)KeyPressedType.PAY_CASH, (int)KeyPressedType.PAY_COUPON, (int)KeyPressedType.PAY_EMV, (int)KeyPressedType.PAY_QRCODE,
            (int)KeyPressedType.PAY_PM6,  (int)KeyPressedType.PAY_PM7,  (int)KeyPressedType.PAY_PM8,  (int)KeyPressedType.PAY_PM9,
            (int)KeyPressedType.PAY_DMTCARD, (int)KeyPressedType.EDIT,
            (int)KeyPressedType.VIOLATION, (int)KeyPressedType.SALE_COUPON, (int)KeyPressedType.FORCE_RESET,
            (int)KeyPressedType.OTL_OPEN, (int)KeyPressedType.OTL_CLOSE
        };
        static readonly HashSet<int> kAllowAfterPayType = new HashSet<int>
        {
            (int)KeyPressedType.EDIT, (int)KeyPressedType.RECEIPT, (int)KeyPressedType.CLASS_9,
            (int)KeyPressedType.VIOLATION, (int)KeyPressedType.SALE_COUPON, (int)KeyPressedType.FORCE_RESET,
            //--- For Next Vehicle in Queue
            (int)KeyPressedType.CLASS_1, (int)KeyPressedType.CLASS_2,
            (int)KeyPressedType.OTL_OPEN, (int)KeyPressedType.OTL_CLOSE,
            (int)KeyPressedType.CLASS_7, (int)KeyPressedType.CLASS_8
        };
        static readonly HashSet<int> kModeHPMC = new HashSet<int>
        {
            (int)KeyPressedType.CLASS_9,
            (int)KeyPressedType.OTL_OPEN, (int)KeyPressedType.OTL_CLOSE
            //, (int)KeyPressedType.EDIT, (int)KeyPressedType.FORCE_RESET
        };
        static readonly HashSet<int> kModeClassTypeTowing = new HashSet<int>
        {
            (int)KeyPressedType.EDIT, (int)KeyPressedType.CLASS_1, (int)KeyPressedType.CLASS_2, (int)KeyPressedType.FORCE_RESET,
            (int)KeyPressedType.OTL_OPEN, (int)KeyPressedType.OTL_CLOSE
        };
        static readonly HashSet<int> kModePayTypeTowing = new HashSet<int>
        {
            (int)KeyPressedType.EDIT, (int)KeyPressedType.RECEIPT,
            (int)KeyPressedType.PAY_CASH, (int)KeyPressedType.PAY_COUPON, (int)KeyPressedType.PAY_EMV, (int)KeyPressedType.PAY_QRCODE,
            (int)KeyPressedType.PAY_DMTCARD, (int)KeyPressedType.FORCE_RESET,
            (int)KeyPressedType.OTL_OPEN, (int)KeyPressedType.OTL_CLOSE,
            (int)KeyPressedType.PAY_PM6,  (int)KeyPressedType.PAY_PM7,  (int)KeyPressedType.PAY_PM8,  (int)KeyPressedType.PAY_PM9
        };
        static readonly HashSet<int> kManageEMV = new HashSet<int>
        {
            (int)KeyPressedType.EDIT,
            (int)KeyPressedType.OTL_OPEN, (int)KeyPressedType.OTL_CLOSE
        };
        static readonly HashSet<int> kModeETC = new HashSet<int>
        {
            (int)KeyPressedType.OTL_OPEN, (int)KeyPressedType.OTL_CLOSE,
            (int)KeyPressedType.END_SHIFT, (int)KeyPressedType.CLASS_9,
            (int)KeyPressedType.FORCE_RESET,
            (int)KeyPressedType.OTL_OPEN, (int)KeyPressedType.OTL_CLOSE
        };
        static readonly HashSet<int> kViolationMode = new HashSet<int>
        {
            (int)KeyPressedType.VIOLATION, (int)KeyPressedType.FORCE_RESET,
            (int)KeyPressedType.OTL_OPEN, (int)KeyPressedType.OTL_CLOSE
        };
        static readonly HashSet<int> kManageDmtCard = new HashSet<int>
        {
            (int)KeyPressedType.EDIT,
            (int)KeyPressedType.OTL_OPEN, (int)KeyPressedType.OTL_CLOSE
        };
        //------------------------------
        //KeyPressedType eCurKeyType;
        public VehicleType eCurVehicleType;
        public PayType eCurPayType;

        //--- Digital Output from LC
        bool bRegisterBell, bSiren, bASB, bLTL, bALB_Open, bALB_Close, bOTL_Status, bOTL_Mode, bOTL_DualMode, bCRS_Reset, bTFI_Reset, bLoop_Reset, bIPC_Reset;

        //--- Digital Input from LC
        bool bInMLB, bInEFS, bInPresenceLoop, bInPassageLoop, bInPresenceOB, bInPassageOB, bInALB, bInOTL, bInLTL, bInEthErr, bInSiren, bInTreadleError, bInTreadleC, bInTreadleD;
        bool bCurMLB, bCurEFS, bCurPresenceLoop, bCurPassageLoop, bCurPresenceOB, bCurPassageOB, bCurALB, bCurOTL, bCurLTL, bCurEthErr, bCurSiren;

        //--- Const Value
        const int ON                = 1;
        const int OFF               = 0;

        //uint iSirenTimerId              = 0;        // Default Siren Alram in sec
        uint iPassageTimerId        = 0;
        //uint iWaitAvcTimerId            = 0;
        int nTrxTmp                 = 0;        // For capture image 
        bool bTrigCaptureImage      = false;
        public int iCapture_nTrx    = 0;
        int iQueueALB               = 0;

        List<uint> iSirenTimerIds   = new List<uint>();
        List<uint> iWaitAvcTimerIds = new List<uint>();
        bool bEnableWaitAVC         = false;

        public bool bHasViolation   = false;    // For check Violation continue
        bool bManualLogin           = false;
        bool bAutoAVC               = false;
        bool bTowingAVC             = false;    // For check Towing AVC
        bool bEditModeETC           = false;
        bool bReClassETC            = false;    // For create Re-Class message to ETC Rabbit MQ
        bool bEditBlockETC          = false;    // For block ETC Rabbit MQ when Edit for Change Pay Type ETC
        bool bFoundLoopOB           = false;    // Check Passage Loop/OB Work
        bool bBlockTAG              = false;    // If found TAG then User must press only edit before class and payment
        bool bEditDual              = false;
        int iStepNextCar            = 0;        // 0=Not allow select next car, 1=Allow select car, 2=Selected car already
        string szPrevPAN            = "";
        string szLoginKey = "", szPasswordKey = "";

        Thread chkNetworkThread, autoResendTransThread, endTransactionThread, chkDeviceStatusThread, mtsStatusThread, chkNetworkCamera1Thread, chkNetworkCamera2Thread, tfiTransferThread, chkNetworkEdcThread;
        //Thread videoCaptureThread, videoCaptureTowingThread, videoCaptureThread_Method_1, videoCaptureTowingThread_Method_1, videoCaptureThread_Method_2, videoCaptureTowingThread_Method_2;
        //------------------------------------------------------------------------------------------------------------------------
        const string szVehicle_Type1        = "รถประเภท 1";
        const string szVehicle_Type2        = "รถประเภท 2";
        const string szVehicle_Exempt       = "รถยกเว้น";
        const string szVehicle_Allowed      = "รถ DMT";
        const string szVehicle_Towing       = "รถลากจูง";
        const string szVehicle_Emergency    = "รถฉุกเฉิน";
        const string szVehicle_HPMC         = "รถขบวน";
        const string szVehicle_LeftExit     = "รถออกซ้าย";
        const string szVehicle_Violation    = "รถฝ่าด่าน";
        const string szPay_Cash             = "เงินสด";
        const string szPay_Coupon           = "คูปอง";
        const string szPay_EMV              = "EMV";
        const string szPay_QrCode           = "QR Code";
        const string szPay_Exempt           = "ยกเว้น";
        const string szPay_DmtCard          = "บัตร DMT";
        const string szTxt_CashCoupon       = "กด 'เงินสด' หรือ 'คูปอง'";
        const string szTxt_ConfirmEdit      = "กดปุ่ม 'ยืนยัน' หรือ 'แก้ไข'";
        const string szTxt_Processing       = "กำลังดำเนินการ...";
        const string szTFI_CloseLane        = "ปิดช่องทาง";
        const string szTFI_StartLane1       = "สวัสดีค่ะ";
        const string szTFI_StartLane2       = "ยินดีให้บริการ";

        public string szAmount_Class1      = TCT_Lib.fFareClass1.ToString("0") + " บาท";
        public string szAmount_Class2      = TCT_Lib.fFareClass2.ToString("0") + " บาท";
        string szAmount_0                   = "0 บาท";

        uint iTimerAutoAVC          = 0;
        uint iTimerClearCRS         = 0;
        uint iTimerALB_On           = 0;
        uint iTimerALB_Off          = 0;
        int iSecClearCRS            = 10;
        bool bDetectLoopBack        = false;
        bool bDetectObBack          = false;
        bool bLoopMis               = false;    // When car touch loop and no class or no payment,
        bool bFoundOB               = false;
        bool bFoundLoop             = false;
        bool bTrigDownLoop          = false;
        bool bTrigDownOB            = false;
        bool bRealPassLoop          = false;
        bool bRealPassOB            = false;
        DateTime dtLastTrans        = DateTime.Now;
        DateTime dtChkLoopOnOB      = DateTime.Now;

        int iCurRunIdx              = 0;     // Only NVR Method=3
        //--------------------------------------------------------------------
        public LaneEventController()
        {
            TCT_Lib.webSocketLane.keyPressedEvent               += LaneSocket_KeyPressedEvent;
            TCT_Lib.webSocketLane.avcLaneEvent                  += LaneSocket_AvcLaneEvent;
            TCT_Lib.webSocketLane.deviceOutputLaneEvent         += LaneSocket_DeviceOuputLaneEvent;
            TCT_Lib.webSocketLane.digitalInputLaneEvent         += LaneSocket_DigitalInputLaneEvent;
            TCT_Lib.webSocketLane.loginSmartCardLaneEvent       += LaneSocket_LoginSmartCardLaneEvent;

            TCT_Lib.etcRabbitMQ.laneStateFromEtcRabbitMQ        += EtcRabbitMQ_LaneStateFromEtcRabbitMQ;
            TCT_Lib.etcRabbitMQ.compiledPassageFromEtcRabbitMQ  += EtcRabbitMQ_CompiledPassageFromEtcRabbitMQ;

            TCT_Lib.webSocketLane.laneSocketServerRTO.etcChangeLaneMode += LaneSocketServerRTO_EtcChangeLaneMode;
            //------------------------------------------------

            //eCurKeyType         = KeyPressedType.NONE;
            eCurVehicleType     = VehicleType.NONE;
            eCurPayType         = PayType.NONE;

            InitialCheckNetworkStatusThread();
            InitialCheckNetworkEdcStatusThread();
            InitialAutoResendTransThread();
            InitialEndTransactionThread();
            InitialChkDeviceStatusThread();
            InitialTfiTransferFileThread();

            if (TCT_Lib.bEnableMTS)
                InitialMtsStatusThread();

            if (TCT_Lib.bEnableVideoNVR)
            {
                if (TCT_Lib.iVideoNVR_CaptureImageMethod >= 1)
                    CreateMonitorCameraNetworkThread();
            }
        }

        public void AddQueueVehicle(int iClassType, decimal fFareAmount, Pay_Info xPayment, string szTransRemark)
        {
            var xQueue          = new objQueueInfo();
            xQueue.iTC_Class    = iClassType;
            xQueue.fFareAmount  = fFareAmount;
            xQueue.xPayment     = xPayment;
            xQueue.szTransRemark = szTransRemark;

            TCT_Lib.xListQueues.Add(xQueue);
        }

        //--------------------------------------------------------------------------------------
        protected void LaneSocket_LoginSmartCardLaneEvent(string strVal)
        {
            if (TCT_Lib.eCurAppPage == AppPage.Login)
            {
                Console.WriteLine("+ Lane Socket - Login Smart Card Reader : value = " + strVal);
                LogInfo.WriteLaneEvent("+ Lane Socket - Login Smart Card Reader : value = " + strVal);
                loginWithSmartCardReader(strVal);
            }
            else if (TCT_Lib.eCurAppPage == AppPage.Maintenance)
            {
                Console.WriteLine("+ Lane Socket - Maintenance Smart Card Reader : value = " + strVal);
                LogInfo.WriteLaneEvent("+ Lane Socket - Maintenance Smart Card Reader : value = " + strVal);
                maintenanceSmartCardReader(strVal);
            }
            else if (TCT_Lib.eCurAppPage == AppPage.DmtPayment)
            {
                Console.WriteLine("+ Lane Socket - DMT Payment Smart Card Reader : value = " + strVal);
                LogInfo.WriteLaneEvent("+ Lane Socket - DMT Payment Smart Card Reader : value = " + strVal);
                dmtPaymentSmartCardReader(strVal);
            }
        }

        public void LaneSocket_KeyPressedEvent(KeyBoardType eKbType, int iKey)
        {
            //--- Key pressed from keyboard 
            Console.WriteLine("+ Lane Socket - KeyPressed Event : KeyID = " + iKey);
/*
            if (iKey == 12)
                TCT_Lib.bPrintOK = true;
            else if (iKey == 55)
                TCT_Lib.bPrintOK = false;
*/
            /*
                        if (eKbType == KeyBoardType.VK)
                        {
                            TCT_Lib.webSocketLane.SendVirtualKeyboard(iKey);
                            return;
                        }
            */

            //-----------------------------------------------------------------------------------------
            //--- Check allow key
            if (TCT_Lib.eCurAppPage == AppPage.Login)
            {
                //--- Login Page
                if (kLoginState.Contains(iKey))
                    manualLoginKey(iKey);
                return;
            }
            else if (TCT_Lib.eCurAppPage == AppPage.SetLaneTypeMode)
            {
                //--- Set LaneType Mode
                if (kSetLaneTypeMode.Contains(iKey))
                    manageProcessSetLaneTypeMode(iKey);
                return;
            }
            else if (TCT_Lib.eCurAppPage == AppPage.EMV)
            {
                //--- EMV Page
                if (kManageEMV.Contains(iKey))
                    manageProcessEMV(iKey);
                return;
            }
            else if (TCT_Lib.eCurAppPage == AppPage.DmtPayment)
            {
                //--- DmtCard Page
                if (kManageDmtCard.Contains(iKey))
                    manageProcessDmtPayment(iKey);
                return;
            }
            else if (TCT_Lib.eCurAppPage == AppPage.PayConvoy)
            {
                //--- Print Receipt Convoy Payment
                if (kPayConvoy.Contains(iKey))
                    manageProcessPayConvoy(iKey);
                return;
            }
            else if (TCT_Lib.eCurAppPage == AppPage.SalesCoupon)
            {
                //--- Sales Coupon
                if (kSalesCoupon.Contains(iKey))
                    manageProcessSalesCoupon(iKey);
                return;
            }
            else if (TCT_Lib.eCurAppPage == AppPage.Maintenance)
            {
                maintenanceProcess(iKey);
                return;
            }
            else if (TCT_Lib.eCurAppPage == AppPage.Main)
            {
                if (bHasViolation)
                {
                    LogInfo.WriteLaneEvent("+ Key when Violation");
                    if (!kViolationMode.Contains(iKey))
                        return;
                }
                if (TCT_Lib.eCurLaneMode == LaneMode.HPMC)
                {
                    LogInfo.WriteLaneEvent("+ Key when HPMC");
                    if (!kModeHPMC.Contains(iKey))
                        return;
                }
                else
                {
                    if (TCT_Lib.eCurLaneType == LaneType.ETC)
                    {
                        if (!kModeETC.Contains(iKey))
                            return;
                    }
                    else
                    {
                        if (TCT_Lib.iIsTowing > 0)
                        {
                            //LogInfo.WriteLaneEvent("+ Key when Towing");
                            if (TCT_Lib.eTowingState == TowingState.StartTowing)
                            {
                                if (!kModeClassTypeTowing.Contains(iKey))
                                    return;
                            }
                            else if ((TCT_Lib.eTowingState == TowingState.FirstClassType) ||
                                     (TCT_Lib.eTowingState == TowingState.SecClassType))
                            {
                                //LogInfo.WriteLaneEvent("+ Key when Towing ClassType");
                                if (!kModePayTypeTowing.Contains(iKey))
                                    return;
                            }
                            else if (TCT_Lib.eTowingState == TowingState.FirstPayType)
                            {
                                //LogInfo.WriteLaneEvent("+ Key when Payment");
                                if (!kModeClassTypeTowing.Contains(iKey))
                                    return;
                            }
                        }
                        else
                        {
                            if (TCT_Lib.bAfterPayment)
                            {
                                //LogInfo.WriteLaneEvent("+ Key when After Payment");
                                if (!kAllowAfterPayType.Contains(iKey))
                                    return;
                            }
                            else
                            {
                                if ((TCT_Lib.iCurTC_Class == 0) || (TCT_Lib.iCurTC_Class == 7) || (TCT_Lib.iCurTC_Class == 8))
                                {
                                    //LogInfo.WriteLaneEvent("+ Key when ClassType");
                                    if (!kAllowClassType.Contains(iKey))
                                        return;
                                }
                                else
                                {
                                    //LogInfo.WriteLaneEvent("+ Key when PayType");
                                    if (!kAllowPayType.Contains(iKey))
                                        return;
                                }
                            }
                        }
                    }
                }
            }

            //-----------------------------------------------------------------------------------------

            if (iKey == (int)KeyPressedType.OTL_OPEN)
            {
                if (!TCT_Lib.bMaintenanceMode)
                {
                    LogInfo.WriteLaneEvent("Press OTL OPEN");
                    TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.OTL_RedGreen, ON);
                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Welcome, (int)TFI_Pattern.Normal, "", "", "", "", "");
                    TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.OTL, 1);
                    TCT_Lib.SendOtlStatusObjToDC(true, TCT_Lib.iStaffShiftID);

                    //---------------------------------------------

                    if (TCT_Lib.bEnableLSD && TCT_Lib.bLSD_SendWhenPressOTL)
                    {
                        if (TCT_Lib.iLSD_KeepAliveSec > 0)
                        {
                            if (TCT_Lib.eCurLaneType == LaneType.MTC)
                            {
                                TCT_Lib.eCurLsdMode = LsdMode.LSD_MTC;
                                if (TCT_Lib.iLSD_MethodType == 0)
                                    TCT_Lib.lsdController.CmdShow_OpenMTC();
                                else
                                    TCT_Lib.webSocketLane.laneSocketServerRTO.SendChangeLsdToRTO(TCT_Lib.eCurLsdMode);
                            }
                            else if (TCT_Lib.eCurLaneType == LaneType.ETC)
                            {
                                TCT_Lib.eCurLsdMode = LsdMode.LSD_ETC;
                                if (TCT_Lib.iLSD_MethodType == 0)
                                    TCT_Lib.lsdController.CmdShow_OpenETC();
                                else
                                    TCT_Lib.webSocketLane.laneSocketServerRTO.SendChangeLsdToRTO(TCT_Lib.eCurLsdMode);
                            }
                            else if (TCT_Lib.eCurLaneType == LaneType.DUAL)
                            {
                                TCT_Lib.eCurLsdMode = LsdMode.LSD_DUAL;
                                if (TCT_Lib.iLSD_MethodType == 0)
                                    TCT_Lib.lsdController.CmdShow_OpenDual();
                                else
                                    TCT_Lib.webSocketLane.laneSocketServerRTO.SendChangeLsdToRTO(TCT_Lib.eCurLsdMode);
                            }
                        }
                        else
                        {
                            //var lsdController = new LaneSocketControl_LSD();
                            if (TCT_Lib.eCurLaneType == LaneType.MTC)
                            {
                                TCT_Lib.eCurLsdMode = LsdMode.LSD_MTC;
                                //lsdController.CmdShow_OpenMTC();
                            }
                            else if (TCT_Lib.eCurLaneType == LaneType.ETC)
                            {
                                TCT_Lib.eCurLsdMode = LsdMode.LSD_ETC;
                                //lsdController.CmdShow_OpenETC();
                            }
                            else if (TCT_Lib.eCurLaneType == LaneType.DUAL)
                            {
                                TCT_Lib.eCurLsdMode = LsdMode.LSD_DUAL;
                                //lsdController.CmdShow_OpenDual();
                            }
                            if (TCT_Lib.iLSD_MethodType == 0)
                            {
                                var lsdController = new LaneSocketControl_LSD();
                                lsdController.RunInitialLSD();
                                //lsdController.CloseSocketConnection();
                            }
                            else
                                TCT_Lib.webSocketLane.laneSocketServerRTO.SendChangeLsdToRTO(TCT_Lib.eCurLsdMode);
                        }
                    }

                    //-------------------------------------------
                }
            }
            else if (iKey == (int)KeyPressedType.OTL_CLOSE)
            {
                if (!TCT_Lib.bMaintenanceMode)
                {
                    LogInfo.WriteLaneEvent("Press OTL CLOSE");
                    TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.OTL_RedGreen, OFF);
                    TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.OTL, 0);
                    TCT_Lib.SendOtlStatusObjToDC(false, TCT_Lib.iStaffShiftID);

                    //------------------------------------------

                    if (TCT_Lib.bEnableLSD && TCT_Lib.bLSD_SendWhenPressOTL)
                    {
                        TCT_Lib.eCurLsdMode = LsdMode.LSD_CLOSE;
                        if (TCT_Lib.iLSD_KeepAliveSec > 0)
                        {
                            if (TCT_Lib.iLSD_MethodType == 0)
                                TCT_Lib.lsdController.CmdShow_LaneClose();
                            else
                                TCT_Lib.webSocketLane.laneSocketServerRTO.SendChangeLsdToRTO(LsdMode.LSD_CLOSE);
                        }
                        else
                        {
                            if (TCT_Lib.iLSD_MethodType == 0)
                            {
                                var lsdController = new LaneSocketControl_LSD();
                                lsdController.RunInitialLSD();
                                //lsdController.CmdShow_LaneClose();
                                //lsdController.CloseSocketConnection();
                            }
                            else
                                TCT_Lib.webSocketLane.laneSocketServerRTO.SendChangeLsdToRTO(TCT_Lib.eCurLsdMode);
                        }
                    }

                    //-----------------------------------------
                }
            }
            else if (iKey == (int)KeyPressedType.START_SHIFT)
            {
                LogInfo.WriteLaneEvent("Press START SHIFT");

                WriteTextCRS("", "");
                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Welcome, (int)TFI_Pattern.Normal, "", "", "", szTFI_StartLane1, szTFI_StartLane2);
                TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.JOB, 1);
                Thread.Sleep(1000);
                TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.JOB, 1);
                Thread.Sleep(500);
                TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.JOB, 1);

                if (TCT_Lib.xListLoopCar.Count > 0)
                    TCT_Lib.xListLoopCar.Clear();

                bHasViolation           = false;
                TCT_Lib.bAfterPayment   = false;
                TCT_Lib.iIsTowing       = 0;
                TCT_Lib.iCurTC_Class    = 0;
            }
            else if ((iKey == (int)KeyPressedType.END_SHIFT) || (iKey == (int)KeyPressedType.BEOJ))
            {
                //----------------------------------------------
                if (!CheckAllowRepeatKeyChar((int)KeyPressedType.BEOJ))
                {
                    LogInfo.WriteLaneEvent($"- Ignore Key BEOJ because key stroke time less than {TCT_Lib.iKeyStrokeTimeMilliSec} ms.");
                    return;
                }
                if (!CheckAllowRepeatKeyChar((int)KeyPressedType.END_SHIFT))
                {
                    LogInfo.WriteLaneEvent($"- Ignore Key EndShift because key stroke time less than {TCT_Lib.iKeyStrokeTimeMilliSec} ms.");
                    return;
                }              
                //---------------------------------------------
                //--- Check Queue in lane
                if ((TCT_Lib.iCurTC_Class > 0) || (TCT_Lib.xListLoopCar.Count != 0))
                {
                    messageProcessUI("พบรถในช่องทาง โปรดรอรถออก");
                    LogInfo.WriteLaneEvent("Press END SHIFT Fail : Found vehicle in queue.");
                    return;
                }
                //--- Check if found Loop On and OB On, then staff must force reset first
                if ((bInPassageLoop && bInPassageOB) || (bTrigDownLoop && bInPassageOB))
                {
                    messageProcessUI("Loop ON/OB ON -> Force Reset");
                    LogInfo.WriteLaneEvent("Press END SHIFT Fail : Found Loop On and OB On. Cannot EOF, User must Force Reset.");
                    return;
                }
                //--- Check OTL off
                if (bCurOTL)
                {
                    messageProcessUI("กรุณา กดปิด OTL ก่อน EOJ");
                    LogInfo.WriteLaneEvent("Press END SHIFT Fail : Not close OTL before EOJ.");
                }
                else
                {
                    LogInfo.WriteLaneEvent("Press END SHIFT");
                    string szErrMsg = "";
                    ClearTextCRS_EndJob();

                    //------------------------------------
                    //--- Add this feature when 02/04/2022 for fix staff press exceed and ALB not close
                    if (TCT_Lib.eCurLaneMode != LaneMode.HPMC)
                        SendDigitalOutput_CmdALB_Close();
                    //------------------------------------
                    if (TCT_Lib.eCurLaneMode == LaneMode.CLOSED)
                    {
                        LogInfo.WriteLaneEvent("- Lane Closed already.");
                        return;
                    }

                    //---------------------------------------------------------
                    if (TCT_Lib.bEnableEdc)
                    {
                        if (!TCT_Lib.bEdcProcessing)
                        {
                            if ((TCT_Lib.iEdcStartHourClearBatch == 0) && (TCT_Lib.iEdcEndHourClearBatch == 0))
                            {
                                if (TCT_Lib.iEdcVersionType == 0)
                                {
                                    var xEdcSumReport = new EdcSumReportLib();
                                }
                                else
                                {
                                    var xEdcSumReport_V1 = new EdcSumReportLib_V1();
                                }
                            }
                            else
                            {
                                if ((DateTime.Now.Hour >= TCT_Lib.iEdcStartHourClearBatch) && (DateTime.Now.Hour <= TCT_Lib.iEdcEndHourClearBatch))
                                {
                                    if (TCT_Lib.iEdcVersionType == 0)
                                    {
                                        var xEdcSumReport = new EdcSumReportLib();
                                    }
                                    else
                                    {
                                        var xEdcSumReport_V1 = new EdcSumReportLib_V1();
                                    }
                                }
                            }
                        }
                        else
                            LogInfo.WriteLogEDC("- EDC is processing, then ignore ClearBatch (SumReport) Process.");
                    }
                    LogInfo.WriteLaneEvent("+ Start Process END SHIFT");
                    DateTime dtEndShift = TCT_Lib.LaneDB.WriteLaneEndStaffShift(TCT_Lib.iStaffShiftID, 0);
                    TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.JOB, 0);
                    TCT_Lib.webSocketLane.SendCollectorJob(0);
                    TCT_Lib.webSocketLane.SendChangeSupervisor();
                    TCT_Lib.SendEndJobObjToRTO(TCT_Lib.iStaffShiftID);
                    TCT_Lib.SendEndJobObjToSupAdjust(TCT_Lib.iStaffShiftID);
                    TCT_Lib.SendEndJobObjToDC(TCT_Lib.iStaffShiftID);
                    TCT_Lib.webSocketLane.SendCollectorJob(0);              // Repeat send EOJ to BIS
                    WebService_BeginEndJob.CallWebService_EndOfJob(dtEndShift, ref szErrMsg);
                    EndShiftProcess();
                    endShiftLaneControl();
                    LogInfo.WriteLaneEvent("+ End Process END SHIFT");
                }
            }
            else if (iKey == (int)KeyPressedType.VIOLATION)
            {
                LogInfo.WriteLaneEvent("Press VIOLATION");

                if (bHasViolation)
                {
                    bHasViolation = false;
                    Perform_StopSiren();
                    //--- Clear TFI
                    if (TCT_Lib.eCurLaneMode == LaneMode.CLOSED)
                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.CloseLane, (int)TFI_Pattern.Normal, "", "", "", szTFI_CloseLane, "");
                    else
                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Welcome, (int)TFI_Pattern.Normal, "", "", "", szTFI_StartLane1, szTFI_StartLane2);

                }
                /*
                                TCT_Lib.iCurTC_Class            = 0;
                                TCT_Lib.xCurPayment.PayType     = 0;
                                TCT_Lib.xCurPayment.PayAmount   = 0;
                                TCT_Lib.xCurPayment.PayDateTime = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                                TCT_Lib.szCurTransRemark        = "VIOLATION";
                                eCurVehicleType                 = VehicleType.VIOLATION;
                                vehicleTypeLaneControl(VehicleType.VIOLATION);
                                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.Siren, ON);ff
                                iSirenTimerId = GLib.Timeout.Add((uint)TCT_Lib.iStopSirenAfterTimeSec*1000, new GLib.TimeoutHandler(Perform_StopSiren));
                */
            }
            else if (iKey == (int)KeyPressedType.CLASS_1)
            {
                //----------------------------------------------
                if (!CheckAllowRepeatKeyChar((int)KeyPressedType.CLASS_1))
                {
                    LogInfo.WriteLaneEvent($"- Ignore Key CLASS 1 because key stroke time less than {TCT_Lib.iKeyStrokeTimeMilliSec} ms.");
                    return;
                }
                //---------------------------------------------
                if (bHasViolation)
                {
                    LogInfo.WriteLaneEvent("+ Return Block Has Violation");
                    return;
                }

                if (bBlockTAG && (TCT_Lib.eCurLaneType == LaneType.DUAL))
                {
                    LogInfo.WriteLaneEvent("+ Return Block TAG : CLASS 1");
                    return;
                }

                if (iStepNextCar >= 2)
                {
                    LogInfo.WriteLaneEvent("+ Return StepNextCar >= 2,  NextCar="+iStepNextCar);
                    return;
                }

                LogInfo.WriteLaneEvent("Press CLASS 1");

                if ((TCT_Lib.eTowingState == TowingState.None) ||
                    (TCT_Lib.eTowingState == TowingState.StartTowing))
                {
                    if (iStepNextCar == 1)
                    {
                        TCT_Lib.iTotalQueue++;
                        TCT_Lib.iNextTC_Class   = 1;
                        iStepNextCar            = 2;
                        nextVehicleTypeLaneControl(VehicleType.CLASS_1);
                        //WriteTextCRS(GetStrClassTypeCRS(VehicleType.CLASS_1), "");
                        //TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, "1", TCT_Lib.fFareClass1.ToString("0"), "", szVehicle_Type1, szAmount_Class1);
                    }
                    else
                    {
                        if (!TCT_Lib.bAfterPayment)
                        {
                            TCT_Lib.iTotalQueue++;
                            TCT_Lib.iCurTC_Class            = 1;
                            TCT_Lib.xCurPayment.payAmount   = TCT_Lib.fFareClass1;
                            TCT_Lib.fCurFareAmount          = TCT_Lib.fFareClass1;
                            eCurVehicleType                 = VehicleType.CLASS_1;
                            vehicleTypeLaneControl(VehicleType.CLASS_1);
                            WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), "");
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, "1", TCT_Lib.fFareClass1.ToString("0"), "", szVehicle_Type1, szAmount_Class1);
                        }
                        else
                        {
                            if (TCT_Lib.iNextTC_Class == 0)
                            {
                                TCT_Lib.iTotalQueue++;
                                TCT_Lib.iNextTC_Class   = 1;
                                iStepNextCar            = 2;
                                nextVehicleTypeLaneControl(VehicleType.CLASS_1);
                            }
                            else if (TCT_Lib.iNextTC_Class == 1)
                            {
                                vehicleTypeLaneControl(VehicleType.CLASS_1);
                                WriteTextCRS(GetStrClassTypeCRS(VehicleType.CLASS_1), "");
                                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, "1", TCT_Lib.fFareClass1.ToString("0"), "", szVehicle_Type1, szAmount_Class1);
                                iStepNextCar            = 2;
                            }
                        }
                    }
                }

                if (TCT_Lib.eTowingState == TowingState.StartTowing)
                    TCT_Lib.eTowingState = TowingState.FirstClassType;
                else if (TCT_Lib.eTowingState == TowingState.FirstPayType)
                {
                    TCT_Lib.iTotalQueue++;
                    TCT_Lib.eTowingState                = TowingState.SecClassType;
                    TCT_Lib.iTowingTC_Class             = 1;
                    TCT_Lib.fTowingFareAmount           = TCT_Lib.fFareClass1;
                    TCT_Lib.xTowingPayment.payAmount    = TCT_Lib.fFareClass1;
                    vehicleTypeLaneControl(VehicleType.CLASS_1);
                    //WriteTextCRS(TCT_Lib.iTowingTC_Class.ToString(), "");
                    //TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, "1", TCT_Lib.fFareClass1.ToString("0"), "", szVehicle_Type1, szAmount_Class1);
                }
            }
            else if (iKey == (int)KeyPressedType.CLASS_2)
            {
                //----------------------------------------------
                if (!CheckAllowRepeatKeyChar((int)KeyPressedType.CLASS_2))
                {
                    LogInfo.WriteLaneEvent($"- Ignore Key CLASS 2 because key stroke time less than {TCT_Lib.iKeyStrokeTimeMilliSec} ms.");
                    return;
                }
                //---------------------------------------------
                if (bHasViolation)
                {
                    LogInfo.WriteLaneEvent("+ Return Block Has Violation");
                    return;
                }

                if (bBlockTAG && (TCT_Lib.eCurLaneType == LaneType.DUAL))
                {
                    LogInfo.WriteLaneEvent("+ Return Block TAG : CLASS 2");
                    return;
                }

                if (iStepNextCar >= 2)
                {
                    LogInfo.WriteLaneEvent("+ Return StepNextCar >= 2,  NextCar=" + iStepNextCar);
                    return;
                }

                LogInfo.WriteLaneEvent("Press CLASS 2");
                if ((TCT_Lib.eTowingState == TowingState.None) ||
                    (TCT_Lib.eTowingState == TowingState.StartTowing))
                {
                    if (iStepNextCar == 1)
                    {
                        TCT_Lib.iTotalQueue++;
                        TCT_Lib.iNextTC_Class   = 2;
                        iStepNextCar            = 2;
                        nextVehicleTypeLaneControl(VehicleType.CLASS_2);
                        //WriteTextCRS(GetStrClassTypeCRS(VehicleType.CLASS_2), "");
                        //TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, "2", TCT_Lib.fFareClass2.ToString("0"), "", szVehicle_Type2, szAmount_Class2);
                    }
                    else
                    {
                        if (!TCT_Lib.bAfterPayment)
                        {
                            TCT_Lib.iTotalQueue++;
                            TCT_Lib.iCurTC_Class            = 2;
                            TCT_Lib.xCurPayment.payAmount   = TCT_Lib.fFareClass2;
                            TCT_Lib.fCurFareAmount          = TCT_Lib.fFareClass2;
                            eCurVehicleType                 = VehicleType.CLASS_2;
                            vehicleTypeLaneControl(VehicleType.CLASS_2);
                            WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), "");
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, "2", TCT_Lib.fFareClass2.ToString("0"), "", szVehicle_Type2, szAmount_Class2);
                        }
                        else
                        {
                            if (TCT_Lib.iNextTC_Class == 0)
                            {
                                TCT_Lib.iTotalQueue++;
                                TCT_Lib.iNextTC_Class   = 2;
                                iStepNextCar            = 2;
                                nextVehicleTypeLaneControl(VehicleType.CLASS_2);
                            }
                            else if (TCT_Lib.iNextTC_Class == 2)
                            {
                                vehicleTypeLaneControl(VehicleType.CLASS_2);
                                WriteTextCRS(GetStrClassTypeCRS(VehicleType.CLASS_2), "");
                                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, "2", TCT_Lib.fFareClass2.ToString("0"), "", szVehicle_Type2, szAmount_Class2);
                                iStepNextCar = 2;
                            }
                        }
                    }
                }

                if (TCT_Lib.eTowingState == TowingState.StartTowing)
                    TCT_Lib.eTowingState = TowingState.FirstClassType;
                else if (TCT_Lib.eTowingState == TowingState.FirstPayType)
                {
                    TCT_Lib.iTotalQueue++;
                    TCT_Lib.eTowingState                = TowingState.SecClassType;
                    TCT_Lib.iTowingTC_Class             = 2;
                    TCT_Lib.fTowingFareAmount           = TCT_Lib.fFareClass2;
                    TCT_Lib.xTowingPayment.payAmount    = TCT_Lib.fFareClass2;
                    vehicleTypeLaneControl(VehicleType.CLASS_2);
                    //WriteTextCRS(TCT_Lib.iTowingTC_Class.ToString(), "");
                    //TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, "2", TCT_Lib.fFareClass2.ToString("0"), "", szVehicle_Type2, szAmount_Class2);
                }
            }
            else if (iKey == (int)KeyPressedType.CLASS_5)
            {
                if (bHasViolation)
                    return;

                if (bBlockTAG && (TCT_Lib.eCurLaneType == LaneType.DUAL))
                {
                    LogInfo.WriteLog("+ Return Block TAG : CLASS 5");
                    return;
                }

                LogInfo.WriteLaneEvent("Press CLASS 5 (EXEMPT)");
                TCT_Lib.iTotalQueue++;
                TCT_Lib.iCurTC_Class            = 5;    // => Emergency Show '9'
                TCT_Lib.xCurPayment.payAmount   = 0;
                TCT_Lib.fCurFareAmount          = 0;
                TCT_Lib.xCurPayment.payType     = (int)PayType.EXEMPT;
                TCT_Lib.xCurPayment.payTypeName = "EMERGENCY";
                TCT_Lib.xCurPayment.payDateTime = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                TCT_Lib.xCurPayment.payRemark   = "EMERGENCY";
                TCT_Lib.szCurTransRemark        = szVehicle_Emergency;
                eCurVehicleType                 = VehicleType.EMERGENCY;
                vehicleTypeLaneControl(VehicleType.EMERGENCY);
                WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), GetStrPayTypeCRS(PayType.EXEMPT));
                TCT_Lib.webSocketLane.OutIo_RBell();
                SendDigitalOutput_CmdALB_Open();
                TCT_Lib.etcRabbitMQ.SendPassagePaymentToETC((int)TCT_Lib.LaneDB.GetCurTransID(), TCT_Lib.iCurTC_Class, (int)PayType.EXEMPT, TCT_Lib.fCurFareAmount, ALB_Mode.Normal, bReClassETC);
                TCT_Lib.bAfterPayment = true;
                //--- Check Next Car can press
                iStepNextCar = 1;

                //----------------------------------------------------------------------------------------
                /*
                                ProcessVideoCaptureThread(1, CameraPosition.FrontCamera);
                                Thread.Sleep(100);
                                ProcessVideoCaptureThread(2, CameraPosition.RearCamera);
                                nTrxTmp = TCT_Lib.nTrx;
                */
                if ((TCT_Lib.iVideoNVR_CaptureImageMethod == 0) || (TCT_Lib.iVideoNVR_CaptureImageMethod == 1))
                    CreateProcessVideoCaptureImage(TCT_Lib.nTrx);
                //---------------------------------------------------------------------------------------
#if __SIM__
                SimLanePassageLoop();
#endif
            }
            else if (iKey == (int)KeyPressedType.CLASS_6)
            {
                if (bHasViolation)
                {
                    LogInfo.WriteLaneEvent("+ Return Block Has Violation");
                    return;
                }

                if (bBlockTAG && (TCT_Lib.eCurLaneType == LaneType.DUAL))
                {
                    LogInfo.WriteLog("+ Return Block TAG : CLASS 6");
                    return;
                }

                LogInfo.WriteLaneEvent("Press CLASS 6 (TOWING)");
                TCT_Lib.iCurTC_Class    = 6;        // => Towing
                TCT_Lib.iIsTowing       = 1;
                TCT_Lib.eTowingState    = TowingState.StartTowing;
                eCurVehicleType         = VehicleType.TOWING;
                vehicleTypeLaneControl(VehicleType.TOWING);
                //TCT_Lib.etcRabbitMQ.SendPassagePaymentToETC((int)TCT_Lib.LaneDB.GetCurTransID(), TCT_Lib.iCurTC_Class, (int)PayType.CASH, TCT_Lib.fCurFareAmount, ALB_Mode.Open);
                WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), "");
                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, "6", "+", "", szVehicle_Towing, "");

                //---------------------------------------------------------------------------------------
#if __SIM__
                //SimLanePassageLoop();
#endif
            }
            else if (iKey == (int)KeyPressedType.CLASS_7)
            {
                //----------------------------------------------
                if (!CheckAllowRepeatKeyChar((int)KeyPressedType.CLASS_7))
                {
                    LogInfo.WriteLaneEvent($"- Ignore Key Exempt because key stroke time less than {TCT_Lib.iKeyStrokeTimeMilliSec} ms.");
                    return;
                }
                //---------------------------------------------
                if (bHasViolation)
                {
                    LogInfo.WriteLaneEvent("+ Return Block Has Violation");
                    return;
                }

                if (bBlockTAG && (TCT_Lib.eCurLaneType == LaneType.DUAL))
                {
                    LogInfo.WriteLog("+ Return Block TAG : CLASS 7");
                    return;
                }

                LogInfo.WriteLaneEvent("Press CLASS 7 (EXEMPT)");
                //-----------------------------------------------------------
                if (iStepNextCar >= 2)
                {
                    LogInfo.WriteLaneEvent("+ Return StepNextCar >= 2,  NextCar=" + iStepNextCar);
                    return;
                }

                if (iStepNextCar == 1)
                {
                    TCT_Lib.iTotalQueue++;
                    TCT_Lib.iNextTC_Class   = 7;
                    iStepNextCar            = 2;
                    nextVehicleTypeLaneControl(VehicleType.EXEMPT);
                    TCT_Lib.bAfterPayment   = true;
                    iCapture_nTrx           += 1;
                }
                else if (iStepNextCar == 0)
                {
                    if (TCT_Lib.bAfterPayment)
                    {
                        TCT_Lib.iTotalQueue++;
                        TCT_Lib.iNextTC_Class   = 7;
                        iStepNextCar            = 2;
                        nextVehicleTypeLaneControl(VehicleType.EXEMPT);
                        TCT_Lib.bAfterPayment   = true;
                    }
                    //-----------------------------------------------------------
                    else
                    {
                        TCT_Lib.iTotalQueue++;
                        TCT_Lib.iCurTC_Class            = 7;    // => Exempt Show '0'
                        TCT_Lib.xCurPayment.payAmount   = 0;
                        TCT_Lib.fCurFareAmount          = 0;
                        TCT_Lib.xCurPayment.payType     = (int)PayType.EXEMPT;
                        TCT_Lib.xCurPayment.payTypeName = "EXEMPT";
                        TCT_Lib.xCurPayment.payDateTime = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                        TCT_Lib.xCurPayment.payRemark   = "EXEMPT";
                        TCT_Lib.szCurTransRemark        = szVehicle_Exempt;
                        eCurVehicleType                 = VehicleType.EXEMPT;
                        vehicleTypeLaneControl(VehicleType.EXEMPT);
                        WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), GetStrPayTypeCRS(PayType.EXEMPT));
                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, "0", "0", "", szVehicle_Exempt, szAmount_0);
                        TCT_Lib.webSocketLane.OutIo_RBell();
                        SendDigitalOutput_CmdALB_Open();
                        TCT_Lib.etcRabbitMQ.SendPassagePaymentToETC((int)TCT_Lib.LaneDB.GetCurTransID(), TCT_Lib.iCurTC_Class, (int)PayType.EXEMPT, TCT_Lib.fCurFareAmount, ALB_Mode.Normal, bReClassETC);
                        TCT_Lib.bAfterPayment = true;

                        if (bDetectLoopBack)
                        {
                            if ((TCT_Lib.iCurTC_Class > 0) && (TCT_Lib.xListLoopCar.Count == 0))
                            {
                                Process_RefreshPresenceLoop();
                                iStepNextCar = 0;
                            }
                        }
                        else
                        {
                            // ---Check Next Car can press
                            iStepNextCar = 1;
                        }

                        if (TCT_Lib.bConvoyMode && (TCT_Lib.xListLoopCar.Count == 0))
                        {
                            TCT_Lib.bAllowConvoy = false;
                            LogInfo.WriteLaneEvent("-1 AllowConvoy = false, ListLoopCar=0");
                        }
                    }
                }
                //---------------------------------------------------------------------------------------
                if ((TCT_Lib.iVideoNVR_CaptureImageMethod == 0) || (TCT_Lib.iVideoNVR_CaptureImageMethod == 1))
                {
                    //if (TCT_Lib.xListLoopCar.Count > 0)
                    //    CreateProcessVideoCaptureImage(TCT_Lib.nTrx+1);
                    //else
                    CreateProcessVideoCaptureImage(TCT_Lib.nTrx);
                }
                //----------------------------------------------------------------------------------------
#if __SIM__
                SimLanePassageLoop();
#endif
            }
            else if (iKey == (int)KeyPressedType.CLASS_8)
            {
                if (bHasViolation)
                {
                    LogInfo.WriteLaneEvent("+ Return Block Has Violation");
                    return;
                }

                if (bBlockTAG && (TCT_Lib.eCurLaneType == LaneType.DUAL))
                {
                    LogInfo.WriteLog("+ Return Block TAG : CLASS 8");
                    return;
                }

                LogInfo.WriteLaneEvent("Press CLASS 8 (LEFT EXIT)");
                //-----------------------------------------------------------
                if (iStepNextCar >= 2)
                {
                    LogInfo.WriteLaneEvent("+ Return StepNextCar >= 2,  NextCar=" + iStepNextCar);
                    return;
                }

                if (iStepNextCar == 1)
                {
                    TCT_Lib.iTotalQueue++;
                    TCT_Lib.iNextTC_Class   = 8;
                    iStepNextCar            = 2;
                    nextVehicleTypeLaneControl(VehicleType.LEFT_EXIT);
                    TCT_Lib.bAfterPayment   = true;
                }
                else if (iStepNextCar == 0)
                {
                    if (TCT_Lib.bAfterPayment)
                    {
                        TCT_Lib.iTotalQueue++;
                        TCT_Lib.iNextTC_Class   = 8;
                        iStepNextCar            = 2;
                        nextVehicleTypeLaneControl(VehicleType.LEFT_EXIT);
                        TCT_Lib.bAfterPayment   = true;
                    }
                    //-----------------------------------------------------------
                    else
                    {
                        TCT_Lib.iTotalQueue++;
                        TCT_Lib.iCurTC_Class            = 8;    // => Left Out
                        TCT_Lib.xCurPayment.payAmount   = 0;
                        TCT_Lib.fCurFareAmount          = 0;
                        TCT_Lib.xCurPayment.payType     = (int)PayType.LeftExit;
                        TCT_Lib.xCurPayment.payTypeName = "LEFT EXIT";
                        TCT_Lib.xCurPayment.payDateTime = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                        TCT_Lib.xCurPayment.payRemark   = "LEFT EXIT";
                        TCT_Lib.szCurTransRemark        = "รถออกซ้าย";
                        eCurVehicleType                 = VehicleType.LEFT_EXIT;
                        vehicleTypeLaneControl(VehicleType.LEFT_EXIT);
                        WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), GetStrPayTypeCRS(PayType.LeftExit));
                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, "0", "0", "", szVehicle_LeftExit, szAmount_0);
                        TCT_Lib.webSocketLane.OutIo_RBell();
                        SendDigitalOutput_CmdALB_Open();
                        SendEtcRabbitMq_CmdALB_Normal("Open left exit");
                        TCT_Lib.etcRabbitMQ.SendPassagePaymentToETC((int)TCT_Lib.LaneDB.GetCurTransID(), TCT_Lib.iCurTC_Class, (int)PayType.LeftExit, TCT_Lib.fCurFareAmount, ALB_Mode.Normal, bReClassETC);
                        TCT_Lib.bAfterPayment = true;
                        if (bDetectLoopBack)
                        {
                            if ((TCT_Lib.iCurTC_Class > 0) && (TCT_Lib.xListLoopCar.Count == 0))
                            {
                                Process_RefreshPresenceLoop();
                                iStepNextCar = 0;
                            }
                        }
                        else
                        {
                            // ---Check Next Car can press
                            iStepNextCar = 1;
                        }
                        if (TCT_Lib.bConvoyMode && (TCT_Lib.xListLoopCar.Count == 0))
                        {
                            TCT_Lib.bAllowConvoy = false;
                            LogInfo.WriteLaneEvent("-2 AllowConvoy = false, ListLoopCar=0");
                        }
                    }
                }
                if ((TCT_Lib.iVideoNVR_CaptureImageMethod == 0) || (TCT_Lib.iVideoNVR_CaptureImageMethod == 1))
                    CreateProcessVideoCaptureImage(TCT_Lib.nTrx);
                //----------------------------------------------------------------------------------------
#if __SIM__
                SimLanePassageLoop();
#endif
            }
            else if (iKey == (int)KeyPressedType.CLASS_9)
            {
                //----------------------------------------------
                if (!CheckAllowRepeatKeyChar((int)KeyPressedType.CLASS_9))
                {
                    LogInfo.WriteLaneEvent($"- Ignore Key HPMC because key stroke time less than {TCT_Lib.iKeyStrokeTimeMilliSec} ms.");
                    return;
                }
                //---------------------------------------------

                if (bHasViolation)
                {
                    LogInfo.WriteLaneEvent("+ Return Block Has Violation");
                    return;
                }

                if (bBlockTAG && (TCT_Lib.eCurLaneType == LaneType.DUAL))
                {
                    LogInfo.WriteLog("+ Return Block TAG : CLASS 9 in DUAL Mode");
                    LogInfo.WriteLaneEvent("+ Return Block TAG : CLASS 9 in Dual Mode");
                    return;
                }

                if (TCT_Lib.eCurLaneMode != LaneMode.HPMC)
                {
                    //----------------------------------------------
                    if (TCT_Lib.eCurLaneType != LaneType.ETC)
                    {
                        if ((!bDetectLoopBack) && (TCT_Lib.iCurTC_Class > 0))
                        {
                            LogInfo.WriteLog("+ Ignore Key HPMC, Found Class > 0 in process");
                            LogInfo.WriteLaneEvent("+ Ignore Key HPMC, Found Class > 0 in process");
                            return;
                        }
                    }

                    //----------------------------------------------

                    LogInfo.WriteLaneEvent("Press CLASS 9 (START HPMC)");
                    if (TCT_Lib.bAllowETC)
                    {
                        TCT_Lib.bEtcCtrlIO      = false;
                        bEditModeETC            = true;
                        showModeType("MTC");
                        //TCT_Lib.etcRabbitMQ.Send_SetLaneMode_ToETC(LaneMode.HPMC);
                    }

                    TCT_Lib.eCurLaneMode            = TCT_Lib.bMaintenanceMode ? LaneMode.MAINTENANCE : LaneMode.HPMC;
                    TCT_Lib.iCurTC_Class            = 9;        // => HPMC Show '0'
                    TCT_Lib.xCurPayment.payAmount   = 0;
                    TCT_Lib.fCurFareAmount          = 0;
                    TCT_Lib.xCurPayment.payType     = (int)PayType.HPMC;
                    TCT_Lib.xCurPayment.payTypeName = "HPMC";
                    TCT_Lib.xCurPayment.payDateTime = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                    TCT_Lib.xCurPayment.payRemark   = "HP&MC";
                    TCT_Lib.szCurTransRemark        = szVehicle_HPMC;
                    eCurVehicleType                 = VehicleType.HPMC;
                    vehicleTypeLaneControl(VehicleType.HPMC);
                    if (TCT_Lib.bMainUiReady)
                        drawETC_AlbMode(1);
                    WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), GetStrPayTypeCRS(PayType.HPMC));
                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.HPMC, (int)TFI_Pattern.HPMC, "0", "0", "", szVehicle_HPMC, szAmount_0);
                    TCT_Lib.webSocketLane.OutIo_RBell();
                    SendDigitalOutput_CmdALB_Open();
                    TCT_Lib.etcRabbitMQ.SendPassagePaymentToETC((int)TCT_Lib.LaneDB.GetCurTransID(), (int)VehicleType.HPMC, (int)PayType.HPMC, 0, ALB_Mode.Open, bReClassETC);
                    TCT_Lib.bAfterPayment = true;
                    if (bDetectLoopBack)
                    {
                        if ((TCT_Lib.iCurTC_Class > 0) && (TCT_Lib.xListLoopCar.Count == 0))
                        {
                            Process_RefreshPresenceLoop();
                            iStepNextCar = 0;
                        }
                    }
                    else
                    {
                        // ---Check Next Car can press
                        iStepNextCar = 1;
                    }
                    if (TCT_Lib.bConvoyMode && (TCT_Lib.xListLoopCar.Count == 0))
                    {
                        TCT_Lib.bAllowConvoy = false;
                        LogInfo.WriteLaneEvent("-3 AllowConvoy = false, ListLoopCar=0");
                    }
                    //-----------------------------------------------------------
                }
                else
                {
                    LogInfo.WriteLaneEvent("Press CLASS 9 (END HPMC)");
                    //--- Finish HPMC
                    if (TCT_Lib.bAllowETC)
                    {
                        bEditModeETC = false;
                        showModeType(TCT_Lib.bAllowETC ? "ETC" : "MTC");
                        //TCT_Lib.etcRabbitMQ.Send_SetLaneMode_ToETC(LaneMode.NORMAL);
                    }

                    if ((TCT_Lib.eCurLaneType == LaneType.DUAL) || (TCT_Lib.eCurLaneType == LaneType.ETC))
                        TCT_Lib.bEndDualHPMC = true;

                    TCT_Lib.eCurLaneMode        = TCT_Lib.bMaintenanceMode ? LaneMode.MAINTENANCE : LaneMode.NORMAL;
                    TCT_Lib.iTotalQueue         = 0;
                    iQueueALB                   = 0;
                    eCurVehicleType             = VehicleType.NONE;
                    eCurPayType                 = PayType.NONE;
                    TCT_Lib.iCurTC_Class        = 0;
                    iStepNextCar                = 0;
                    szPrevPAN                   = "";
                    TCT_Lib.ClearCurTransData();

                    if (TCT_Lib.xListLoopCar.Count == 0)
                        TCT_Lib.xListLoopCar.Clear();
                    else
                    {
                        //if ((TCT_Lib.bMaintenanceMode) && (!bDetectLoopBack))
                        if (!bDetectLoopBack)
                        {
                            // v2.0.0: use LoopCarFirst() (null-safe, locked) instead of [0]
                            var firstLoopCar = TCT_Lib.LoopCarFirst();
                            if (firstLoopCar != null && firstLoopCar.iTC_Class == 9)
                                TCT_Lib.LoopCarClear();
                        }
                    }

                    if (TCT_Lib.bMaintenanceMode)
                        ClearAndInitialVar();

                    showTotalLoop();
                    showQueueAlbControl(iQueueALB);
                    TCT_Lib.iNextTC_Class   = 0;
                    TCT_Lib.eTowingState    = TowingState.None;
                    clearPanelUiLaneControl();

                    TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.LTL, OFF);

                    if (TCT_Lib.bMainUiReady)
                    {
                        vehicleTypeLaneControl(eCurVehicleType);
                        payTypeLaneControl(eCurPayType);
                        clearPanelUiLaneControl();
                        if (TCT_Lib.bMainUiReady)
                            drawETC_AlbMode(0);
                        if (TCT_Lib.eCurLaneMode == LaneMode.CLOSED)
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.CloseLane, (int)TFI_Pattern.Normal, "", "", "", szTFI_CloseLane, "");
                        else
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Welcome, (int)TFI_Pattern.Normal, "", "", "", szTFI_StartLane1, szTFI_StartLane2);
                        if (iTimerClearCRS == 0)
                            iTimerClearCRS = GLib.Timeout.Add((uint)(iSecClearCRS * 1000), new GLib.TimeoutHandler(ClearTextCRS));
                        SendDigitalOutput_CmdALB_Close();
                        TCT_Lib.etcRabbitMQ.SendPassagePaymentToETC((int)TCT_Lib.LaneDB.GetCurTransID(), (int)VehicleType.HPMC, (int)PayType.HPMC, 0, ALB_Mode.Normal, bReClassETC);
                    }
                }
#if __SIM__
                //--- Sim Device Input LC
                //TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.EntryLoop, ON);
                //TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.EntryOB, ON);
#endif
            }
            else if (iKey == (int)KeyPressedType.PAY_CASH)
            {
                //----------------------------------------------
                if (!CheckAllowRepeatKeyChar((int)KeyPressedType.PAY_CASH))
                {
                    LogInfo.WriteLaneEvent($"- Ignore Key Pay CASH because key stroke time less than {TCT_Lib.iKeyStrokeTimeMilliSec} ms.");
                    return;
                }
                //---------------------------------------------
                if (bHasViolation)
                    return;

                LogInfo.WriteLaneEvent("Press PAY CASH");
                updateUI_ClearQrWarningMsg();

                if (TCT_Lib.eTowingState == TowingState.None)
                {
                    TCT_Lib.xCurPayment.payType         = (int)PayType.CASH;
                    TCT_Lib.xCurPayment.payTypeName     = TCT_Lib.GetPayMethodName((int)PayType.CASH);
                    TCT_Lib.xCurPayment.payDateTime     = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                    eCurPayType = PayType.CASH;
                    payTypeLaneControl(PayType.CASH);

                    TCT_Lib.webSocketLane.OutIo_RBell();
                    WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), GetStrPayTypeCRS(PayType.CASH));
                    if (eCurVehicleType == VehicleType.CLASS_1)
                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass1.ToString("0"), "", szPay_Cash, szAmount_Class1);
                    else
                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass2.ToString("0"), "", szPay_Cash, szAmount_Class2);
                    bEditModeETC = false;
                    showModeType(TCT_Lib.bAllowETC ? "ETC" : "MTC");
                    SendDigitalOutput_CmdALB_Open();
                    if (TCT_Lib.bAllowETC)
                    {
                        if (TCT_Lib.bMainUiReady)
                            drawETC_AlbMode(0);
                        TCT_Lib.szPrevTransId = TCT_Lib.szCurTransId;
                        TCT_Lib.etcRabbitMQ.SendPassagePaymentToETC((int)TCT_Lib.LaneDB.GetCurTransID(), TCT_Lib.iCurTC_Class, (int)PayType.CASH, TCT_Lib.fCurFareAmount, ALB_Mode.Normal, bReClassETC);
                    }


                    //----------------------------------------------------------------------------------------
                    /*
                                        ProcessVideoCaptureThread(1, CameraPosition.FrontCamera);
                                        Thread.Sleep(100);
                                        ProcessVideoCaptureThread(2, CameraPosition.RearCamera);
                                        nTrxTmp = TCT_Lib.nTrx;
                    */
                    if ((TCT_Lib.iVideoNVR_CaptureImageMethod == 0) || (TCT_Lib.iVideoNVR_CaptureImageMethod == 1))
                        CreateProcessVideoCaptureImage(TCT_Lib.nTrx);
                    //----------------------------------------------------------------------------------------
                    SendEtcRabbitMq_CmdALB_Normal("Normal Payment - CASH");

                    TCT_Lib.bAfterPayment = true;
                    if (bDetectLoopBack)
                    {
                        if ((TCT_Lib.iCurTC_Class > 0) && (TCT_Lib.xListLoopCar.Count == 0))
                        {
                            Process_RefreshPresenceLoop();
                            iStepNextCar = 0;
                        }
                    }
                    else
                    {
                        // ---Check Next Car can press
                        iStepNextCar = 1;
                    }

                    //---------------------------------------------------------------------------------------
#if __SIM__
                    SimLanePassageLoop();
#endif
                }
                else
                {
                    //--- Towing Mode
                    if (TCT_Lib.eTowingState == TowingState.FirstClassType)
                    {
                        TCT_Lib.eTowingState            = TowingState.FirstPayType;
                        TCT_Lib.xCurPayment.payType     = (int)PayType.CASH;
                        TCT_Lib.xCurPayment.payTypeName = TCT_Lib.GetPayMethodName((int)PayType.CASH);
                        TCT_Lib.xCurPayment.payDateTime = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                        TCT_Lib.xCurPayment.payAmount   = TCT_Lib.iCurTC_Class == 1 ? TCT_Lib.fFareClass1 : TCT_Lib.fFareClass2;
                        TCT_Lib.fCurFareAmount          = TCT_Lib.iCurTC_Class == 1 ? TCT_Lib.fFareClass1 : TCT_Lib.fFareClass2;
                        eCurPayType                     = PayType.CASH;
                        payTypeLaneControl(PayType.CASH);
                        WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), GetStrPayTypeCRS(PayType.CASH));
                        if (eCurVehicleType == VehicleType.CLASS_1)
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass1.ToString("0"), "", szPay_Cash, szAmount_Class1);
                        else
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass2.ToString("0"), "", szPay_Cash, szAmount_Class2);
                        //payTypeLaneControl(PayType.CASH);
                    }
                    else if (TCT_Lib.eTowingState == TowingState.SecClassType)
                    {
                        TCT_Lib.eTowingState                = TowingState.SecPayType;
                        TCT_Lib.xTowingPayment.payType      = (int)PayType.CASH;
                        TCT_Lib.xTowingPayment.payTypeName  = TCT_Lib.GetPayMethodName((int)PayType.CASH);
                        TCT_Lib.xTowingPayment.payDateTime  = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                        TCT_Lib.xTowingPayment.payAmount    = TCT_Lib.iTowingTC_Class == 1 ? TCT_Lib.fFareClass1 : TCT_Lib.fFareClass2;
                        TCT_Lib.fTowingFareAmount           = TCT_Lib.iTowingTC_Class == 1 ? TCT_Lib.fFareClass1 : TCT_Lib.fFareClass2;
                        payTypeLaneControl(PayType.CASH);
                        WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), GetStrPayTypeCRS(PayType.CASH));
                        if (eCurVehicleType == VehicleType.CLASS_1)
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass1.ToString("0"), "", szPay_Cash, szAmount_Class1);
                        else
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass2.ToString("0"), "", szPay_Cash, szAmount_Class2);
                        //payTypeLaneControl(PayType.CASH);

                        if (TCT_Lib.bMainUiReady)
                            drawETC_AlbMode(0);

                        /*
                                                WriteTextCRS(TCT_Lib.iTowingTC_Class.ToString(), GetStrPayTypeCRS(PayType.CASH));
                                                if (TCT_Lib.iTowingTC_Class == (int)VehicleType.CLASS_1)
                                                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, TCT_Lib.iTowingTC_Class.ToString(), TCT_Lib.fFareClass1.ToString("0"), "", szPay_Cash, szAmount_Class1);
                                                else
                                                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, TCT_Lib.iTowingTC_Class.ToString(), TCT_Lib.fFareClass2.ToString("0"), "", szPay_Cash, szAmount_Class2);
                        */
                        TCT_Lib.webSocketLane.OutIo_RBell();
                        SendDigitalOutput_CmdALB_Open();

                        if (TCT_Lib.bAllowETC)
                        {
                            TCT_Lib.etcRabbitMQ.SendPassagePaymentToETC((int)TCT_Lib.LaneDB.GetCurTransID(), TCT_Lib.iCurTC_Class, TCT_Lib.xCurPayment.payType, TCT_Lib.xCurPayment.payAmount, ALB_Mode.Open, bReClassETC, true);
                            TCT_Lib.etcRabbitMQ.SendPassagePaymentToETC((int)TCT_Lib.LaneDB.GetCurTransID(), TCT_Lib.iTowingTC_Class, TCT_Lib.xTowingPayment.payType, TCT_Lib.xTowingPayment.payAmount, ALB_Mode.Open, bReClassETC, true);
                            //--- Modify 2020-10-26 (Not send Payment=Towing, Both send PAYMENT_COLLECTED)
                            //TCT_Lib.etcRabbitMQ.SendPassagePaymentToETC((int)TCT_Lib.LaneDB.GetCurTransID(), TCT_Lib.iTowingTC_Class, TCT_Lib.xTowingPayment.payType, TCT_Lib.xTowingPayment.payAmount, ALB_Mode.Open, true);
                        }

                        TCT_Lib.bAfterPayment = true;
                        // ---Check Next Car can press
                        if (bDetectLoopBack)
                        {
                            if ((TCT_Lib.iCurTC_Class > 0) && (TCT_Lib.xListLoopCar.Count == 0))
                            {
                                Process_RefreshPresenceLoop();
                                iStepNextCar = 0;
                            }
                        }
                        else
                        {
                            // ---Check Next Car can press
                            iStepNextCar = 1;
                        }
                        //----------------------------------------------------------------------------------------
                        /*
                                                ProcessVideoCaptureThread(1, CameraPosition.FrontCamera);
                                                Thread.Sleep(100);
                                                ProcessVideoCaptureThread(2, CameraPosition.RearCamera);
                                                nTrxTmp = TCT_Lib.nTrx;
                        */
                        if ((TCT_Lib.iVideoNVR_CaptureImageMethod == 0) || (TCT_Lib.iVideoNVR_CaptureImageMethod == 1))
                            CreateProcessVideoCaptureImage(TCT_Lib.nTrx, true);
                        //---------------------------------------------------------------------------------------
#if __SIM__
                        SimLanePassageLoop();
#endif
                    }
                }
            }
            else if (iKey == (int)KeyPressedType.PAY_COUPON)
            {
                if (!CheckAllowRepeatKeyChar((int)KeyPressedType.PAY_COUPON))
                {
                    LogInfo.WriteLaneEvent($"- Ignore Key Pay COUPON because key stroke time less than {TCT_Lib.iKeyStrokeTimeMilliSec} ms.");
                    return;
                }
                //---------------------------------------------
                if (bHasViolation)
                    return;

                LogInfo.WriteLaneEvent("Press PAY COUPON");
                updateUI_ClearQrWarningMsg();

                if (TCT_Lib.eTowingState == TowingState.None)
                {
                    TCT_Lib.xCurPayment.payType         = (int)PayType.COUPON;
                    TCT_Lib.xCurPayment.payTypeName     = TCT_Lib.GetPayMethodName((int)PayType.COUPON);
                    TCT_Lib.xCurPayment.payDateTime     = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                    eCurPayType = PayType.COUPON;
                    payTypeLaneControl(PayType.COUPON);

                    WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), GetStrPayTypeCRS(PayType.COUPON));
                    if (eCurVehicleType == VehicleType.CLASS_1)
                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass1.ToString("0"), "", szPay_Coupon, szAmount_Class1);
                    else
                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass2.ToString("0"), "", szPay_Coupon, szAmount_Class2);

                    bEditModeETC = false;
                    TCT_Lib.webSocketLane.OutIo_RBell();
                    SendDigitalOutput_CmdALB_Open();
                    showModeType(TCT_Lib.bAllowETC ? "ETC" : "MTC");
                    if (TCT_Lib.bAllowETC)
                    {
                        TCT_Lib.szPrevTransId = TCT_Lib.szCurTransId;
                        TCT_Lib.etcRabbitMQ.SendPassagePaymentToETC((int)TCT_Lib.LaneDB.GetCurTransID(), TCT_Lib.iCurTC_Class, (int)PayType.COUPON, TCT_Lib.fCurFareAmount, ALB_Mode.Normal, bReClassETC);
                        if (TCT_Lib.bMainUiReady)
                            drawETC_AlbMode(0);
                    }

                    //----------------------------------------------------------------------------------------
                    /*
                                        ProcessVideoCaptureThread(1, CameraPosition.FrontCamera);
                                        Thread.Sleep(100);
                                        ProcessVideoCaptureThread(2, CameraPosition.RearCamera);
                                        nTrxTmp = TCT_Lib.nTrx;
                    */
                    if ((TCT_Lib.iVideoNVR_CaptureImageMethod == 0) || (TCT_Lib.iVideoNVR_CaptureImageMethod == 1))
                        CreateProcessVideoCaptureImage(TCT_Lib.nTrx);
                    //----------------------------------------------------------------------------------------

                    SendEtcRabbitMq_CmdALB_Normal("Normal Payment - COUPON");

                    TCT_Lib.bAfterPayment = true;
                    if (bDetectLoopBack)
                    {
                        if ((TCT_Lib.iCurTC_Class > 0) && (TCT_Lib.xListLoopCar.Count == 0))
                        {
                            Process_RefreshPresenceLoop();
                            iStepNextCar = 0;
                        }
                    }
                    else
                    {
                        // ---Check Next Car can press
                        iStepNextCar = 1;
                    }
                    if (TCT_Lib.bConvoyMode && (TCT_Lib.xListLoopCar.Count == 0))
                    {
                        TCT_Lib.bAllowConvoy = false;
                        LogInfo.WriteLaneEvent("-4 AllowConvoy = false, ListLoopCar=0");
                    }
                    //---------------------------------------------------------------------------------------
#if __SIM__
                    SimLanePassageLoop();
#endif
                }
                else
                {
                    //--- Towing Mode
                    if (TCT_Lib.eTowingState == TowingState.FirstClassType)
                    {
                        TCT_Lib.eTowingState            = TowingState.FirstPayType;
                        TCT_Lib.xCurPayment.payType     = (int)PayType.COUPON;
                        TCT_Lib.xCurPayment.payTypeName = TCT_Lib.GetPayMethodName((int)PayType.COUPON);
                        TCT_Lib.xCurPayment.payDateTime = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                        TCT_Lib.xCurPayment.payAmount   = TCT_Lib.iCurTC_Class == 1 ? TCT_Lib.fFareClass1 : TCT_Lib.fFareClass2;
                        TCT_Lib.fCurFareAmount          = TCT_Lib.iCurTC_Class == 1 ? TCT_Lib.fFareClass1 : TCT_Lib.fFareClass2;
                        eCurPayType                     = PayType.COUPON;
                        payTypeLaneControl(PayType.COUPON);
                        WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), GetStrPayTypeCRS(PayType.COUPON));
                        if (eCurVehicleType == VehicleType.CLASS_1)
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass1.ToString("0"), "", szPay_Coupon, szAmount_Class1);
                        else
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass2.ToString("0"), "", szPay_Coupon, szAmount_Class2);
                    }
                    else if (TCT_Lib.eTowingState == TowingState.SecClassType)
                    {
                        TCT_Lib.eTowingState                = TowingState.SecPayType;
                        TCT_Lib.xTowingPayment.payType      = (int)PayType.COUPON;
                        TCT_Lib.xTowingPayment.payTypeName  = TCT_Lib.GetPayMethodName((int)PayType.COUPON);
                        TCT_Lib.xTowingPayment.payDateTime  = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                        TCT_Lib.xTowingPayment.payAmount    = TCT_Lib.iTowingTC_Class == 1 ? TCT_Lib.fFareClass1 : TCT_Lib.fFareClass2;
                        TCT_Lib.fTowingFareAmount           = TCT_Lib.iTowingTC_Class == 1 ? TCT_Lib.fFareClass1 : TCT_Lib.fFareClass2;
                        payTypeLaneControl(PayType.COUPON);

                        if (TCT_Lib.bMainUiReady)
                            drawETC_AlbMode(1);
                        /*
                                                WriteTextCRS(TCT_Lib.iTowingTC_Class.ToString(), GetStrPayTypeCRS(PayType.COUPON));
                                                if (TCT_Lib.iTowingTC_Class == (int)VehicleType.CLASS_1)
                                                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, TCT_Lib.iTowingTC_Class.ToString(), TCT_Lib.fFareClass1.ToString("0"), "", szPay_Coupon, szAmount_Class1);
                                                else
                                                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, TCT_Lib.iTowingTC_Class.ToString(), TCT_Lib.fFareClass2.ToString("0"), "", szPay_Coupon, szAmount_Class2);
                        */
                        TCT_Lib.webSocketLane.OutIo_RBell();
                        SendDigitalOutput_CmdALB_Open();
                        if (TCT_Lib.bAllowETC)
                        {
                            TCT_Lib.etcRabbitMQ.SendPassagePaymentToETC((int)TCT_Lib.LaneDB.GetCurTransID(), TCT_Lib.iCurTC_Class, TCT_Lib.xCurPayment.payType, TCT_Lib.xCurPayment.payAmount, ALB_Mode.Open, bReClassETC, true);
                            TCT_Lib.etcRabbitMQ.SendPassagePaymentToETC((int)TCT_Lib.LaneDB.GetCurTransID(), TCT_Lib.iTowingTC_Class, TCT_Lib.xTowingPayment.payType, TCT_Lib.xTowingPayment.payAmount, ALB_Mode.Open, bReClassETC, true);
                        }

                        TCT_Lib.bAfterPayment = true;
                        if (bDetectLoopBack)
                        {
                            if ((TCT_Lib.iCurTC_Class > 0) && (TCT_Lib.xListLoopCar.Count == 0))
                            {
                                Process_RefreshPresenceLoop();
                                iStepNextCar = 0;
                            }
                        }
                        else
                        {
                            // ---Check Next Car can press
                            iStepNextCar = 1;
                        }
                        //---------------------------------------------------------------------------------------
                        /*
                                                ProcessVideoCaptureThread(1, CameraPosition.FrontCamera);
                                                Thread.Sleep(100);
                                                ProcessVideoCaptureThread(2, CameraPosition.RearCamera);
                                                nTrxTmp = TCT_Lib.nTrx;
                        */
                        if ((TCT_Lib.iVideoNVR_CaptureImageMethod == 0) || (TCT_Lib.iVideoNVR_CaptureImageMethod == 1))
                            CreateProcessVideoCaptureImage(TCT_Lib.nTrx, true);
                        //---------------------------------------------------------------------------------------
#if __SIM__
                        SimLanePassageLoop();
#endif
                    }
                }
            }
            else if ((iKey == (int)KeyPressedType.PAY_PM6) ||
                     (iKey == (int)KeyPressedType.PAY_PM7) ||
                     (iKey == (int)KeyPressedType.PAY_PM8) ||
                     (iKey == (int)KeyPressedType.PAY_PM9))
            {
                if (bHasViolation)
                    return;

                int iPayType = 1;
                var ePayType = PayType.PM6;
                if (iKey == (int)KeyPressedType.PAY_PM6)
                {
                    if (TCT_Lib.szPaymentBtn_06.Trim() == "")
                        return;
                    ePayType = PayType.PM6;
                    iPayType = (int)PayType.PM6;
                }
                else if (iKey == (int)KeyPressedType.PAY_PM7)
                {
                    if (TCT_Lib.szPaymentBtn_07.Trim() == "")
                        return;
                    ePayType = PayType.PM7;
                    iPayType = (int)PayType.PM7;
                }
                else if (iKey == (int)KeyPressedType.PAY_PM8)
                {
                    if (TCT_Lib.szPaymentBtn_08.Trim() == "")
                        return;
                    ePayType = PayType.PM8;
                    iPayType = (int)PayType.PM8;
                }

                else if (iKey == (int)KeyPressedType.PAY_PM9)
                {
                    if (TCT_Lib.szPaymentBtn_09.Trim() == "")
                        return;
                    ePayType = PayType.PM9;
                    iPayType = (int)PayType.PM9;
                }

                LogInfo.WriteLaneEvent("Press PAY " + TCT_Lib.GetPayMethodName(iPayType));

                if (TCT_Lib.eTowingState == TowingState.None)
                {
                    TCT_Lib.xCurPayment.payType         = iPayType;
                    TCT_Lib.xCurPayment.payTypeName     = TCT_Lib.GetPayMethodName(iPayType);
                    TCT_Lib.xCurPayment.payDateTime     = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                    eCurPayType     = ePayType;
                    payTypeLaneControl(ePayType);

                    TCT_Lib.webSocketLane.OutIo_RBell();
                    WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), GetStrPayTypeCRS(ePayType));
                    if (eCurVehicleType == VehicleType.CLASS_1)
                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass1.ToString("0"), "", TCT_Lib.GetPayMethodName(iPayType), szAmount_Class1);
                    else
                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass2.ToString("0"), "", TCT_Lib.GetPayMethodName(iPayType), szAmount_Class2);
                    bEditModeETC = false;
                    showModeType(TCT_Lib.bAllowETC ? "ETC" : "MTC");
                    SendDigitalOutput_CmdALB_Open();
                    if (TCT_Lib.bAllowETC)
                    {
                        if (TCT_Lib.bMainUiReady)
                            drawETC_AlbMode(0);
                        TCT_Lib.szPrevTransId = TCT_Lib.szCurTransId;
                        TCT_Lib.etcRabbitMQ.SendPassagePaymentToETC((int)TCT_Lib.LaneDB.GetCurTransID(), TCT_Lib.iCurTC_Class, iPayType, TCT_Lib.fCurFareAmount, ALB_Mode.Normal, bReClassETC);
                    }


                    //----------------------------------------------------------------------------------------
                    /*
                                        ProcessVideoCaptureThread(1, CameraPosition.FrontCamera);
                                        Thread.Sleep(100);
                                        ProcessVideoCaptureThread(2, CameraPosition.RearCamera);
                                        nTrxTmp = TCT_Lib.nTrx;
                    */
                    if ((TCT_Lib.iVideoNVR_CaptureImageMethod == 0) || (TCT_Lib.iVideoNVR_CaptureImageMethod == 1))
                        CreateProcessVideoCaptureImage(TCT_Lib.nTrx);
                    //----------------------------------------------------------------------------------------
                    SendEtcRabbitMq_CmdALB_Normal("Normal Payment - " + TCT_Lib.GetPayMethodName(iPayType));

                    TCT_Lib.bAfterPayment = true;
                    if (bDetectLoopBack)
                    {
                        if ((TCT_Lib.iCurTC_Class > 0) && (TCT_Lib.xListLoopCar.Count == 0))
                        {
                            Process_RefreshPresenceLoop();
                            iStepNextCar = 0;
                        }
                    }
                    else
                    {
                        // ---Check Next Car can press
                        iStepNextCar = 1;
                    }

                    //---------------------------------------------------------------------------------------
#if __SIM__
                    SimLanePassageLoop();
#endif
                }
                else
                {
                    //--- Towing Mode
                    if (TCT_Lib.eTowingState == TowingState.FirstClassType)
                    {
                        TCT_Lib.eTowingState            = TowingState.FirstPayType;
                        TCT_Lib.xCurPayment.payType     = iPayType;
                        TCT_Lib.xCurPayment.payTypeName = TCT_Lib.GetPayMethodName(iPayType);
                        TCT_Lib.xCurPayment.payDateTime = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                        TCT_Lib.xCurPayment.payAmount   = TCT_Lib.iCurTC_Class == 1 ? TCT_Lib.fFareClass1 : TCT_Lib.fFareClass2;
                        TCT_Lib.fCurFareAmount          = TCT_Lib.iCurTC_Class == 1 ? TCT_Lib.fFareClass1 : TCT_Lib.fFareClass2;
                        eCurPayType                     = ePayType;
                        payTypeLaneControl(ePayType);
                        WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), GetStrPayTypeCRS(ePayType));
                        if (eCurVehicleType == VehicleType.CLASS_1)
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass1.ToString("0"), "", TCT_Lib.GetPayMethodName(iPayType), szAmount_Class1);
                        else
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass2.ToString("0"), "", TCT_Lib.GetPayMethodName(iPayType), szAmount_Class2);
                        //payTypeLaneControl(PayType.CASH);
                    }
                    else if (TCT_Lib.eTowingState == TowingState.SecClassType)
                    {
                        TCT_Lib.eTowingState                = TowingState.SecPayType;
                        TCT_Lib.xTowingPayment.payType      = iPayType;
                        TCT_Lib.xTowingPayment.payTypeName  = TCT_Lib.GetPayMethodName(iPayType);
                        TCT_Lib.xTowingPayment.payDateTime  = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                        TCT_Lib.xTowingPayment.payAmount    = TCT_Lib.iTowingTC_Class == 1 ? TCT_Lib.fFareClass1 : TCT_Lib.fFareClass2;
                        TCT_Lib.fTowingFareAmount           = TCT_Lib.iTowingTC_Class == 1 ? TCT_Lib.fFareClass1 : TCT_Lib.fFareClass2;
                        payTypeLaneControl(ePayType);
                        WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), GetStrPayTypeCRS(ePayType));
                        if (eCurVehicleType == VehicleType.CLASS_1)
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass1.ToString("0"), "", TCT_Lib.GetPayMethodName(iPayType), szAmount_Class1);
                        else
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass2.ToString("0"), "", TCT_Lib.GetPayMethodName(iPayType), szAmount_Class2);
                        //payTypeLaneControl(PayType.CASH);

                        if (TCT_Lib.bMainUiReady)
                            drawETC_AlbMode(0);

                        /*
                                                WriteTextCRS(TCT_Lib.iTowingTC_Class.ToString(), GetStrPayTypeCRS(PayType.CASH));
                                                if (TCT_Lib.iTowingTC_Class == (int)VehicleType.CLASS_1)
                                                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, TCT_Lib.iTowingTC_Class.ToString(), TCT_Lib.fFareClass1.ToString("0"), "", szPay_Cash, szAmount_Class1);
                                                else
                                                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, TCT_Lib.iTowingTC_Class.ToString(), TCT_Lib.fFareClass2.ToString("0"), "", szPay_Cash, szAmount_Class2);
                        */
                        TCT_Lib.webSocketLane.OutIo_RBell();
                        SendDigitalOutput_CmdALB_Open();

                        if (TCT_Lib.bAllowETC)
                        {
                            TCT_Lib.etcRabbitMQ.SendPassagePaymentToETC((int)TCT_Lib.LaneDB.GetCurTransID(), TCT_Lib.iCurTC_Class, iPayType, TCT_Lib.xCurPayment.payAmount, ALB_Mode.Open, bReClassETC, true);
                            TCT_Lib.etcRabbitMQ.SendPassagePaymentToETC((int)TCT_Lib.LaneDB.GetCurTransID(), TCT_Lib.iTowingTC_Class, iPayType, TCT_Lib.xTowingPayment.payAmount, ALB_Mode.Open, bReClassETC, true);
                            //--- Modify 2020-10-26 (Not send Payment=Towing, Both send PAYMENT_COLLECTED)
                            //TCT_Lib.etcRabbitMQ.SendPassagePaymentToETC((int)TCT_Lib.LaneDB.GetCurTransID(), TCT_Lib.iTowingTC_Class, TCT_Lib.xTowingPayment.payType, TCT_Lib.xTowingPayment.payAmount, ALB_Mode.Open, true);
                        }

                        TCT_Lib.bAfterPayment = true;
                        // ---Check Next Car can press
                        if (bDetectLoopBack)
                        {
                            if ((TCT_Lib.iCurTC_Class > 0) && (TCT_Lib.xListLoopCar.Count == 0))
                            {
                                Process_RefreshPresenceLoop();
                                iStepNextCar = 0;
                            }
                        }
                        else
                        {
                            // ---Check Next Car can press
                            iStepNextCar = 1;
                        }
                        //----------------------------------------------------------------------------------------
                        /*
                                                ProcessVideoCaptureThread(1, CameraPosition.FrontCamera);
                                                Thread.Sleep(100);
                                                ProcessVideoCaptureThread(2, CameraPosition.RearCamera);
                                                nTrxTmp = TCT_Lib.nTrx;
                        */
                        if ((TCT_Lib.iVideoNVR_CaptureImageMethod == 0) || (TCT_Lib.iVideoNVR_CaptureImageMethod == 1))
                            CreateProcessVideoCaptureImage(TCT_Lib.nTrx, true);
                        //---------------------------------------------------------------------------------------
#if __SIM__
                        SimLanePassageLoop();
#endif
                    }
                }
            }
            else if (iKey == (int)KeyPressedType.PAY_DMTCARD)
            {
                if (bHasViolation)
                    return;

                LogInfo.WriteLaneEvent("Press PAY DMT_CARD");
                updateUI_ClearQrWarningMsg();
                //------------------------------------------------------------------------------------
                DateTime dtEffectiveDate_DmtCard = DateTime.MinValue;
                if (TCT_Lib.szDmtCardEffectiveDate.Trim() != "")
                {
                    DateTime dtDate = DateTime.MinValue;
                    if (DateTime.TryParse(TCT_Lib.szDmtCardEffectiveDate.Trim(), out dtDate))
                    {
                        dtEffectiveDate_DmtCard = dtDate;
                    }
                    else
                    {
                        LogInfo.WriteLog("- Fail Convert DMT Card EffectiveDate : " + TCT_Lib.szDmtCardEffectiveDate.Trim());
                        dtEffectiveDate_DmtCard = DateTime.MaxValue;
                    }
                }
                if ((TCT_Lib.iDmtCardType > 0) && (DateTime.Now > dtEffectiveDate_DmtCard))
                {
                    var frmDmtPayment   = new DMT_PaymentDlg();
                    Gdk.Color colBk     = new Gdk.Color(0x3E, 0x3E, 0x3E);
                    frmDmtPayment.ModifyBg(StateType.Normal, colBk);
                    ResponseType xResult = (ResponseType)frmDmtPayment.Run();

                    string szDmtCarNo       = frmDmtPayment.szDmtCarNo;
                    string szLicensePlate   = frmDmtPayment.szLicensePlate;
                    string szDepartment     = frmDmtPayment.szDepartment;
                    string szAffilliation   = frmDmtPayment.szAffilliation;
                    int iVehicleClass       = frmDmtPayment.iVehicleClass;

                    TCT_Lib.eCurAppPage = AppPage.Main;
                    frmDmtPayment.Dispose();
                    frmDmtPayment.Destroy();
                    frmDmtPayment       = null;
                    if ((xResult == ResponseType.Cancel) || (xResult == ResponseType.DeleteEvent))
                    {
                        LogInfo.WriteLaneEvent("- Cancel DMT Payment Dialog");
                        return;
                    }

                    LogInfo.WriteLaneEvent($"+ Success Pay with DMT Card : CardNo={szDmtCarNo}, LicensePlate={szLicensePlate}, Department={szDepartment}, Affilliation={szAffilliation}");
                    //--- Show DMT Infomation
                    updateUI_DmtCardInfo(szDmtCarNo, szLicensePlate, szDepartment, szAffilliation, iVehicleClass);

                    object objParams = new
                    {
                        Information = szLicensePlate+" / "+szDepartment,
                    };
                    TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToClient("UPDATE_INFO", objParams);
                }
                //--------------------------------------------------------------------------------------

                if (TCT_Lib.eTowingState == TowingState.None)
                {
                    TCT_Lib.xCurPayment.payType     = (int)PayType.DMT_CARD;
                    TCT_Lib.xCurPayment.payTypeName = TCT_Lib.GetPayMethodName((int)PayType.DMT_CARD);
                    TCT_Lib.xCurPayment.payDateTime = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                    TCT_Lib.xCurPayment.payAmount   = 0;
                    TCT_Lib.fCurFareAmount          = 0;
                    TCT_Lib.xCurPayment.payTypeName = "DMT CARD";
                    eCurPayType                     = PayType.DMT_CARD;
                    payTypeLaneControl(PayType.DMT_CARD);

                    WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), GetStrPayTypeCRS(PayType.DMT_CARD));
                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), "0", "", szPay_DmtCard, szAmount_0);

                    bEditModeETC        = false;
                    showModeType(TCT_Lib.bAllowETC ? "ETC" : "MTC");
                    TCT_Lib.webSocketLane.OutIo_RBell();
                    SendDigitalOutput_CmdALB_Open();
                    if (TCT_Lib.bAllowETC)
                    {
                        TCT_Lib.szPrevTransId = TCT_Lib.szCurTransId;
                        if (TCT_Lib.bMainUiReady)
                            drawETC_AlbMode(0);
                        TCT_Lib.etcRabbitMQ.SendPassagePaymentToETC((int)TCT_Lib.LaneDB.GetCurTransID(), TCT_Lib.iCurTC_Class, (int)PayType.DMT_CARD, TCT_Lib.fCurFareAmount, ALB_Mode.Normal, bReClassETC);
                    }

                    //----------------------------------------------------------------------------------------
                    /*
                                        ProcessVideoCaptureThread(1, CameraPosition.FrontCamera);
                                        Thread.Sleep(100);
                                        ProcessVideoCaptureThread(2, CameraPosition.RearCamera);
                                        nTrxTmp = TCT_Lib.nTrx;
                    */
                    if ((TCT_Lib.iVideoNVR_CaptureImageMethod == 0) || (TCT_Lib.iVideoNVR_CaptureImageMethod == 1))
                        CreateProcessVideoCaptureImage(TCT_Lib.nTrx);
                    //----------------------------------------------------------------------------------------

                    SendEtcRabbitMq_CmdALB_Normal("Normal Payment - DMT_CARD");
                    TCT_Lib.bAfterPayment = true;
                    // ---Check Next Car can press
                    if (bDetectLoopBack)
                    {
                        if ((TCT_Lib.iCurTC_Class > 0) && (TCT_Lib.xListLoopCar.Count == 0))
                        {
                            Process_RefreshPresenceLoop();
                            iStepNextCar = 0;
                        }
                    }
                    else
                    {
                        // ---Check Next Car can press
                        iStepNextCar = 1;
                    }
                    if (TCT_Lib.bConvoyMode && (TCT_Lib.xListLoopCar.Count == 0))
                    {
                        TCT_Lib.bAllowConvoy = false;
                        LogInfo.WriteLaneEvent("-5 AllowConvoy = false, ListLoopCar=0");
                    }
                    //---------------------------------------------------------------------------------------
#if __SIM__
                    SimLanePassageLoop();
#endif
                }
                else
                {
                    //--- Towing Mode
                    if (TCT_Lib.eTowingState == TowingState.FirstClassType)
                    {
                        TCT_Lib.eTowingState                = TowingState.FirstPayType;
                        TCT_Lib.xCurPayment.payType         = (int)PayType.DMT_CARD;
                        TCT_Lib.xCurPayment.payTypeName     = TCT_Lib.GetPayMethodName((int)PayType.DMT_CARD);
                        TCT_Lib.xCurPayment.payDateTime     = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                        TCT_Lib.xCurPayment.payAmount       = 0;
                        TCT_Lib.fCurFareAmount              = 0;
                        eCurPayType = PayType.DMT_CARD;
                        payTypeLaneControl(PayType.DMT_CARD);
                        WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), GetStrPayTypeCRS(PayType.DMT_CARD));
                        if (eCurVehicleType == VehicleType.CLASS_1)
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), 0.ToString("0"), "", szPay_DmtCard, szAmount_Class1);
                        else
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), 0.ToString("0"), "", szPay_DmtCard, szAmount_Class2);
                    }
                    else if (TCT_Lib.eTowingState == TowingState.SecClassType)
                    {
                        TCT_Lib.eTowingState                = TowingState.SecPayType;
                        TCT_Lib.xTowingPayment.payType      = (int)PayType.DMT_CARD;
                        TCT_Lib.xTowingPayment.payTypeName  = TCT_Lib.GetPayMethodName((int)PayType.DMT_CARD);
                        TCT_Lib.xTowingPayment.payDateTime  = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                        TCT_Lib.xTowingPayment.payAmount    = 0;
                        payTypeLaneControl(PayType.DMT_CARD);

                        /*
                                                WriteTextCRS(TCT_Lib.iTowingTC_Class.ToString(), GetStrPayTypeCRS(PayType.DMT_CARD));
                                                if (TCT_Lib.iTowingTC_Class == (int)VehicleType.CLASS_1)
                                                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, TCT_Lib.iTowingTC_Class.ToString(), 0.ToString("0"), "", szPay_DmtCard, szAmount_Class1);
                                                else
                                                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, TCT_Lib.iTowingTC_Class.ToString(), 0.ToString("0"), "", szPay_DmtCard, szAmount_Class2);
                        */
                        TCT_Lib.webSocketLane.OutIo_RBell();
                        SendDigitalOutput_CmdALB_Open();
                        if (TCT_Lib.bAllowETC)
                        {
                            TCT_Lib.etcRabbitMQ.SendPassagePaymentToETC((int)TCT_Lib.LaneDB.GetCurTransID(), TCT_Lib.iCurTC_Class, TCT_Lib.xCurPayment.payType, TCT_Lib.xCurPayment.payAmount, ALB_Mode.Open, bReClassETC, true);
                            TCT_Lib.etcRabbitMQ.SendPassagePaymentToETC((int)TCT_Lib.LaneDB.GetCurTransID(), TCT_Lib.iTowingTC_Class, TCT_Lib.xTowingPayment.payType, TCT_Lib.xTowingPayment.payAmount, ALB_Mode.Open, bReClassETC, true);
                        }
                        if (TCT_Lib.bMainUiReady)
                            drawETC_AlbMode(1);

                        TCT_Lib.bAfterPayment = true;
                        // ---Check Next Car can press
                        iStepNextCar = 1;
                        //---------------------------------------------------------------------------------------
                        /*
                                                ProcessVideoCaptureThread(1, CameraPosition.FrontCamera);
                                                Thread.Sleep(100);
                                                ProcessVideoCaptureThread(2, CameraPosition.RearCamera);
                                                nTrxTmp = TCT_Lib.nTrx;
                        */
                        if ((TCT_Lib.iVideoNVR_CaptureImageMethod == 0) || (TCT_Lib.iVideoNVR_CaptureImageMethod == 1))
                            CreateProcessVideoCaptureImage(TCT_Lib.nTrx, true);
                        //---------------------------------------------------------------------------------------
#if __SIM__
                        SimLanePassageLoop();
#endif
                    }
                }
            }
            else if (iKey == (int)KeyPressedType.PAY_EMV)
            {
                if (bHasViolation)
                    return;

                LogInfo.WriteLaneEvent("Press PAY EMV");
                updateUI_ClearQrWarningMsg();
                //-----------------------------------------------------------------
                /*
                                if (TCT_Lib.eTowingState == TowingState.FirstClassType)
                                {
                                    TCT_Lib.eTowingState            = TowingState.FirstPayType;
                                    TCT_Lib.xCurPayment.payType     = (int)PayType.EMV;
                                    TCT_Lib.xCurPayment.payTypeName = TCT_Lib.GetPayMethodName((int)PayType.EMV);
                                    TCT_Lib.xCurPayment.payDateTime = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                                    TCT_Lib.xCurPayment.payAmount   = TCT_Lib.iCurTC_Class == 1 ? TCT_Lib.fFareClass1 : TCT_Lib.fFareClass2;
                                    TCT_Lib.fCurFareAmount = TCT_Lib.iCurTC_Class == 1 ? TCT_Lib.fFareClass1 : TCT_Lib.fFareClass2;
                                    eCurPayType         = PayType.EMV;
                                    payTypeLaneControl(PayType.EMV);
                                    WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), GetStrPayTypeCRS(PayType.EMV));
                                    if (eCurVehicleType == VehicleType.CLASS_1)
                                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass1.ToString("0"), "", szPay_EMV, szAmount_Class1);
                                    else
                                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass2.ToString("0"), "", szPay_EMV, szAmount_Class2);
                                }
                                else if (TCT_Lib.eTowingState == TowingState.SecClassType)
                                {
                                    TCT_Lib.eTowingState                = TowingState.SecPayType;
                                    TCT_Lib.xTowingPayment.payType      = (int)PayType.EMV;
                                    TCT_Lib.xTowingPayment.payTypeName  = TCT_Lib.GetPayMethodName((int)PayType.EMV);
                                    TCT_Lib.xTowingPayment.payDateTime  = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                                    TCT_Lib.xTowingPayment.payAmount    = TCT_Lib.iTowingTC_Class == 1 ? TCT_Lib.fFareClass1 : TCT_Lib.fFareClass2;
                                    TCT_Lib.fTowingFareAmount           = TCT_Lib.iTowingTC_Class == 1 ? TCT_Lib.fFareClass1 : TCT_Lib.fFareClass2;
                                    payTypeLaneControl(PayType.EMV);
                                    WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), GetStrPayTypeCRS(PayType.EMV));
                                    if (eCurVehicleType == VehicleType.CLASS_1)
                                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass1.ToString("0"), "", szPay_EMV, szAmount_Class1);
                                    else
                                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass2.ToString("0"), "", szPay_EMV, szAmount_Class2);
                                }
                */
                //-----------------------------------------------------------------
                if (TCT_Lib.eTowingState == TowingState.FirstClassType)
                    TCT_Lib.fCurFareAmount = TCT_Lib.iCurTC_Class == 1 ? TCT_Lib.fFareClass1 : TCT_Lib.fFareClass2;
                else if (TCT_Lib.eTowingState == TowingState.SecClassType)
                    TCT_Lib.fTowingFareAmount = TCT_Lib.iTowingTC_Class == 1 ? TCT_Lib.fFareClass1 : TCT_Lib.fFareClass2;
                //----------------------------------------------------------------------
                if ((TCT_Lib.fCurFareAmount > 0) || (TCT_Lib.xCurPayment.payType == (int)PayType.DMT_CARD))
                {
                    if (TCT_Lib.eTowingState == TowingState.None)
                        emvCreditPaymentUI(TCT_Lib.fCurFareAmount);
                    else if (TCT_Lib.eTowingState == TowingState.FirstClassType)
                        emvCreditPaymentUI(TCT_Lib.fCurFareAmount);
                    else if (TCT_Lib.eTowingState == TowingState.SecClassType)
                        emvCreditPaymentUI(TCT_Lib.fTowingFareAmount);
                }

            }
            else if (iKey == (int)KeyPressedType.PAY_QRCODE)
            {
                LogInfo.WriteLaneEvent("Press PAY QR CODE");
                updateUI_ClearQrWarningMsg();
                //-----------------------------------------------------------------
                if (TCT_Lib.eTowingState == TowingState.FirstClassType)
                    TCT_Lib.fCurFareAmount = TCT_Lib.iCurTC_Class == 1 ? TCT_Lib.fFareClass1 : TCT_Lib.fFareClass2;
                else if (TCT_Lib.eTowingState == TowingState.SecClassType)
                    TCT_Lib.fTowingFareAmount = TCT_Lib.iTowingTC_Class == 1 ? TCT_Lib.fFareClass1 : TCT_Lib.fFareClass2;
                //----------------------------------------------------------------------
                if (TCT_Lib.fCurFareAmount > 0)
                {
                    if (TCT_Lib.eTowingState == TowingState.None)
                        emvQrCodePaymentUI(TCT_Lib.fCurFareAmount);
                    else if (TCT_Lib.eTowingState == TowingState.FirstClassType)
                        emvQrCodePaymentUI(TCT_Lib.fCurFareAmount);
                    else if (TCT_Lib.eTowingState == TowingState.SecClassType)
                        emvQrCodePaymentUI(TCT_Lib.fTowingFareAmount);
                }
            }
            else if (iKey == (int)KeyPressedType.EDIT)
            {
                LogInfo.WriteLaneEvent("Press EDIT");
                //----------------------------------------------
                if (!CheckAllowRepeatKeyChar((int)KeyPressedType.EDIT))
                {
                    LogInfo.WriteLaneEvent($"- Ignore Key EDIT because key stroke time less than {TCT_Lib.iKeyStrokeTimeMilliSec} ms.");
                    return;
                }
                //---------------------------------------------


                /*
                                if ((iStepNextCar == 0) && (TCT_Lib.iTotalQueue == 0) && (TCT_Lib.iNextTC_Class == 0))
                                {
                                    LogInfo.WriteLaneEvent("- EDIT no action");
                                    return;
                                }
                */

                //--- Check payment is EMV, and pay success, then block
                if ((TCT_Lib.eTowingState == TowingState.FirstPayType) || (TCT_Lib.eTowingState == TowingState.SecPayType))
                {
                    if (TCT_Lib.iNextTC_Class == 0)
                    {
                        if (TCT_Lib.eTowingState == TowingState.FirstPayType)
                        {
                            if (TCT_Lib.xCurPayment != null)
                            {
                                if ((TCT_Lib.xCurPayment.payType == (int)PayType.EMV) || (TCT_Lib.xCurPayment.payType == (int)PayType.QrCode))
                                {
                                    LogInfo.WriteLaneEvent($"- Block Key EDIT because now payment EMV success (First vehicle).");
                                    return;
                                }
                            }
                        }
                        else if (TCT_Lib.eTowingState == TowingState.SecPayType)
                        {
                            if (TCT_Lib.xTowingPayment != null)
                            {
                                if ((TCT_Lib.xTowingPayment.payType == (int)PayType.EMV) || (TCT_Lib.xTowingPayment.payType == (int)PayType.QrCode))
                                {
                                    LogInfo.WriteLaneEvent($"- Block Key EDIT because now payment EMV success (Towing vehicle).");
                                    return;
                                }
                            }
                        }
                    }
                }

                //--- Fix bug when Edit back of Towing (2024-08-27)
                if (TCT_Lib.eTowingState == TowingState.SecClassType)
                    TCT_Lib.iTowingTC_Class = 0;

                //-------------------------------
                if (TCT_Lib.xCurPayment != null)
                {
                    if ((TCT_Lib.xCurPayment.payType == (int)PayType.EMV) || (TCT_Lib.xCurPayment.payType == (int)PayType.QrCode))
                    {
                        LogInfo.WriteLaneEvent($"- Block Key EDIT because now payment EMV success.");
                        return;
                    }
                }

                //-----------------------------------------------------------
                //updateUI_ClearQrWarningMsg();

                if (bBlockTAG && (TCT_Lib.eCurLaneType == LaneType.DUAL))
                    bBlockTAG = false;

                if (TCT_Lib.eCurLaneType == LaneType.DUAL)
                    bEditDual = true;

                if (iStepNextCar == 2)          // Select car already
                {
                    iStepNextCar = 1;
                    TCT_Lib.iNextTC_Class = 0;
                    TCT_Lib.iTotalQueue--;
                    if (TCT_Lib.iTotalQueue < 0)
                        TCT_Lib.iTotalQueue = 0;
                    nextVehicleTypeLaneControl(VehicleType.NONE);
                    iCapture_nTrx -= 1;   // Capture reverse
                    LogInfo.WriteLaneEvent($"- Ignore Key EDIT because found this is second vehicle.");
                    return;
                }
                else if (iStepNextCar == 1)     // Allow can select but now not select car
                {
                    iStepNextCar = 0;
                }

                LogInfo.WriteLaneEvent($"+ EDIT Process is working.");
                //-----------------------------------------------------
                if (TCT_Lib.eTowingState != TowingState.None)
                {
                    //--- Towing Mode
                    if (TCT_Lib.eTowingState == TowingState.FirstClassType)
                    {
                        TCT_Lib.eTowingState = TowingState.StartTowing;
                        iStepNextCar = 0;
                        TCT_Lib.iTotalQueue--;
                        if (TCT_Lib.iTotalQueue < 0)
                            TCT_Lib.iTotalQueue = 0;
                    }
                    else if (TCT_Lib.eTowingState == TowingState.FirstPayType)
                        TCT_Lib.eTowingState = TowingState.FirstClassType;
                    else if (TCT_Lib.eTowingState == TowingState.SecClassType)
                    {
                        TCT_Lib.eTowingState = TowingState.FirstPayType;
                        TCT_Lib.iTotalQueue--;
                        if (TCT_Lib.iTotalQueue < 0)
                            TCT_Lib.iTotalQueue = 0;
                    }
                    else if (TCT_Lib.eTowingState == TowingState.SecPayType)
                        TCT_Lib.eTowingState = TowingState.SecClassType;
                    else
                    {
                        TCT_Lib.eTowingState    = TowingState.None;
                        TCT_Lib.xCurPayment     = new Pay_Info();
                    }
                }

                updateUiQueueLaneControl();
                bEditBlockETC   = true;
                int iEditType   = 0;          // 0=Edit Class, 1=Edit Payment
                if (TCT_Lib.eTowingState == TowingState.None)
                {
                    if (TCT_Lib.bAllowETC)
                        showModeType("MTC");

                    TCT_Lib.iPrevTC_Class       = TCT_Lib.iCurTC_Class;
                    TCT_Lib.iPrevPayType        = TCT_Lib.xCurPayment.payType;
                    TCT_Lib.fPrevFareAmount     = TCT_Lib.fCurFareAmount;

                    if ((TCT_Lib.xCurPayment.payType == (int)PayType.NONE) ||
                        (TCT_Lib.xCurPayment.payType == (int)PayType.EXEMPT) ||
                        (TCT_Lib.xCurPayment.payType == (int)PayType.LeftExit))
                    {
                        TCT_Lib.iTotalQueue--;
                        if (TCT_Lib.iTotalQueue < 0)
                            TCT_Lib.iTotalQueue = 0;

                        TCT_Lib.webSocketLane.OutIo_ClearCRS();
                        LogInfo.WriteLaneEvent("IO - Siren : OFF, ASB : OFF");
                        TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.Siren, OFF);
                        TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ASB, OFF);
                        if (TCT_Lib.eCurLaneMode != LaneMode.HPMC)
                        {
                            if ((TCT_Lib.iCurTC_Class > 0) && (TCT_Lib.xCurPayment.payType > 0))
                            {
                                iQueueALB--;
                                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.LTL, OFF);
                                SendDigitalOutput_CmdALB_Close();
                            }
                        }

                        //--- Force Reset
                        if (TCT_Lib.eCurLaneMode == LaneMode.HPMC)
                        {
                            //--- Finish HPMC
                            TCT_Lib.eCurLaneMode = LaneMode.NORMAL;
                            TCT_Lib.iCurTC_Class = 0;
                        }

                        if (TCT_Lib.eCurLaneMode == LaneMode.MAINTENANCE)
                        {
                            //--- Finish HPMC
                            TCT_Lib.iCurTC_Class = 0;
                        }

                        iEditType           = 0;
                        iCapture_nTrx       -= 1;   // Capture reverse
                        TCT_Lib.ClearCurTransData();
                        eCurVehicleType     = VehicleType.NONE;
                        eCurPayType         = PayType.NONE;
                        szPrevPAN           = "";
                        TCT_Lib.bReClassifiedFlag = true;

                        vehicleTypeLaneControl(eCurVehicleType);
                        payTypeLaneControl(eCurPayType);
                        //LogInfo.WriteLog("- ClearUI from EDIT");
                        clearPanelUiLaneControl();
                        editBackClassType();

                        TCT_Lib.bEtcCtrlIO = false;

                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Welcome, (int)TFI_Pattern.Normal, "", "", "", szTFI_StartLane1, szTFI_StartLane2);
                        if (iTimerClearCRS == 0)
                            iTimerClearCRS = GLib.Timeout.Add((uint)(iSecClearCRS * 1000), new GLib.TimeoutHandler(ClearTextCRS));
#if __SIM__
                        //--- Sim Device Input LC
                        TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.PresenceLoop, OFF);
                        TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.PassageLoop, OFF);
                        TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.PresenceOB, OFF);
                        TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.PassageOB, OFF);
#endif
                    }
/*
                    else if (TCT_Lib.xCurPayment.payType > 0)
                    {
                        if ((TCT_Lib.xCurPayment.payType == (int)PayType.EasyPass) || ((TCT_Lib.xCurPayment.payType == (int)PayType.MPass)))
                        {
                            //--- Clear vehicle and payment
                            TCT_Lib.ClearCurTransData();
                            eCurVehicleType     = VehicleType.NONE;
                            eCurPayType         = PayType.NONE;
                            szPrevPAN           = "";
                            vehicleTypeLaneControl(eCurVehicleType);
                            payTypeLaneControl(eCurPayType);
                            //LogInfo.WriteLog("- ClearUI from EDIT-2");
                            clearPanelUiLaneControl();
                        }
                    }
*/                    
                    else if ((TCT_Lib.iCurTC_Class > 0) && (TCT_Lib.xCurPayment.payType > 0))
                    {
                        if ((TCT_Lib.xCurPayment.payType == (int)PayType.EasyPass) || ((TCT_Lib.xCurPayment.payType == (int)PayType.MPass)))
                        {
                            int iCurPayType     = TCT_Lib.xCurPayment.payType;
                            //--- Clear vehicle and payment
                            TCT_Lib.ClearCurTransData();
                            eCurVehicleType     = VehicleType.NONE;
                            eCurPayType         = PayType.NONE;
                            szPrevPAN           = "";
                            vehicleTypeLaneControl(eCurVehicleType);
                            payTypeLaneControl(eCurPayType);
                            //LogInfo.WriteLog("- ClearUI from EDIT-2");
                            clearPanelUiLaneControl();
                            TCT_Lib.iPrevTC_Class = TCT_Lib.iCurTC_Class = 0;

                            TCT_Lib.webSocketLane.OutIo_ClearCRS();
                            LogInfo.WriteLaneEvent("IO - Siren : OFF, ASB : OFF");
                            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.Siren, OFF);
                            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ASB, OFF);

                            //--- If found vehicle in loop list, then clear loop
                            var firstLoopCarEdit = TCT_Lib.LoopCarFirst();   // v2.0.0: null-safe locked access
                            if (firstLoopCarEdit != null)
                            {
                                if ((firstLoopCarEdit.xPayment.payType == (int)PayType.EasyPass) || (firstLoopCarEdit.xPayment.payType == (int)PayType.MPass))
                                {
                                    TCT_Lib.LoopCarClear();
                                    updateUiQueueLaneControl();
                                    showTotalLoop();
                                }
                            }
                            //----------------------------
                        }
                        else if (TCT_Lib.xCurPayment.payType == (int)PayType.DMT_CARD)
                        {
                            updateUI_ClearPanelTagInfo();
                        }

                        //------------------------------------------------------------
                        int iAlarmID = (int)TCT_Lib.LaneDB.WriteLaneAlarmRTO(AlarmDevice.RE_PAYMENT, AlarmType.Occurred, $"RePayment = {TCT_Lib.xCurPayment.payTypeName}, Amount={TCT_Lib.xCurPayment.payAmount} ", "");
                        TCT_Lib.SendLaneAlarmObjToRTO(iAlarmID);
                        //--------------------------------------------------------------
                        TCT_Lib.xCurPayment             = new Pay_Info();
                        TCT_Lib.xCurPayment.payType     = (int)PayType.NONE;
                        TCT_Lib.xCurPayment.payAmount   = TCT_Lib.fCurFareAmount;
                        TCT_Lib.bRePaymentFlag          = true;

                        iEditType       = 1;
                        iCapture_nTrx   -= 1;   // Capture reverse

                        WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), "");
                        if (eCurVehicleType == VehicleType.CLASS_1)
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, "1", TCT_Lib.fFareClass1.ToString("0"), "", szVehicle_Type1, szAmount_Class1);
                        else if (eCurVehicleType == VehicleType.CLASS_2)
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, "2", TCT_Lib.fFareClass2.ToString("0"), "", szVehicle_Type2, szAmount_Class2);
                        else if (eCurVehicleType == VehicleType.LEFT_EXIT)
                        {
                            WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), GetStrPayTypeCRS(PayType.EXEMPT));
                        }
                        else if (eCurVehicleType == VehicleType.EXEMPT)
                        {
                            WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), GetStrPayTypeCRS(PayType.EXEMPT));
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, "0", "0", "", szVehicle_Exempt, szAmount_0);
                        }
                        else if (eCurVehicleType == VehicleType.HPMC)
                        {
                            WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), GetStrPayTypeCRS(PayType.HPMC));
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.HPMC, (int)TFI_Pattern.HPMC, "0", "0", "", szVehicle_HPMC, szAmount_0);
                        }
                        if (TCT_Lib.eCurLaneMode != LaneMode.HPMC)
                        {
                            iQueueALB--;
                            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.LTL, OFF);
                            SendDigitalOutput_CmdALB_Close();
                        }
                        if (TCT_Lib.bAllowETC && TCT_Lib.bAfterPayment)
                        {
                            bReClassETC = true;
                            //TCT_Lib.etcRabbitMQ.SendPassagePaymentToETC((int)TCT_Lib.LaneDB.GetCurTransID(), TCT_Lib.iPrevTC_Class, TCT_Lib.iPrevPayType, TCT_Lib.fPrevFareAmount, ALB_Mode.Normal, true, false);
                        }
                        if (TCT_Lib.bMainUiReady)
                            drawETC_AlbMode(0);

                        editBackPayType();
                        TCT_Lib.bAfterPayment = false;
                    }
                }
                else
                {
                    //--- Towing Mode
                    if (TCT_Lib.eTowingState == TowingState.SecClassType)
                    {
                        TCT_Lib.bReClassifiedFlag = true;
                        WriteTextCRS(TCT_Lib.iTowingTC_Class.ToString(), "");
                        if (TCT_Lib.iTowingTC_Class == (int)VehicleType.CLASS_1)
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, TCT_Lib.iTowingTC_Class.ToString(), TCT_Lib.fFareClass1.ToString("0"), "", szVehicle_Type1, szAmount_Class1);
                        else
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, TCT_Lib.iTowingTC_Class.ToString(), TCT_Lib.fFareClass2.ToString("0"), "", szVehicle_Type2, szAmount_Class2);
                        iCapture_nTrx -= 1;   // Capture reverse

                        if (eCurVehicleType != VehicleType.HPMC)
                        {
                            iQueueALB--;
                            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.LTL, OFF);
                            SendDigitalOutput_CmdALB_Close();
                        }
                    }
                    else if (TCT_Lib.eTowingState == TowingState.FirstPayType)
                    {
                        TCT_Lib.bRePaymentFlag = true;
                        WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), GetStrPayTypeCRS(eCurPayType));
                        if (eCurVehicleType == VehicleType.CLASS_1)
                        {
                            if (TCT_Lib.xCurPayment.payType == (int)PayType.CASH)
                                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass1.ToString("0"), "", szPay_Cash, szAmount_Class1);
                            else
                                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass1.ToString("0"), "", szPay_Coupon, szAmount_Class1);
                        }
                        else
                        {
                            if (TCT_Lib.xCurPayment.payType == (int)PayType.CASH)
                                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass2.ToString("0"), "", szPay_Cash, szAmount_Class2);
                            else
                                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass2.ToString("0"), "", szPay_Coupon, szAmount_Class2);
                        }

                        //------------------------------------------------------------
                        int iAlarmID = (int)TCT_Lib.LaneDB.WriteLaneAlarmRTO(AlarmDevice.RE_PAYMENT, AlarmType.Occurred, $"RePayment First Towing Class {TCT_Lib.iTowingTC_Class}", "");
                        TCT_Lib.SendLaneAlarmObjToRTO(iAlarmID);
                        //--------------------------------------------------------------
                    }
                    else if (TCT_Lib.eTowingState == TowingState.FirstClassType)
                    {
                        TCT_Lib.bReClassifiedFlag = true;
                        WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), "");
                        if (eCurVehicleType == VehicleType.CLASS_1)
                        {
                            if (TCT_Lib.xCurPayment.payType == (int)PayType.CASH)
                                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass1.ToString("0"), "", szVehicle_Type1, szAmount_Class1);
                            else
                                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass1.ToString("0"), "", szVehicle_Type1, szAmount_Class1);
                        }
                        else
                        {
                            if (TCT_Lib.xCurPayment.payType == (int)PayType.CASH)
                                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass2.ToString("0"), "", szVehicle_Type2, szAmount_Class2);
                            else
                                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass2.ToString("0"), "", szVehicle_Type2, szAmount_Class2);
                        }
                        iCapture_nTrx -= 1;   // Capture reverse
                    }
                    else if (TCT_Lib.eTowingState == TowingState.StartTowing)
                    {
                        eCurVehicleType = VehicleType.TOWING;
                        WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), "");
                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, "6", "0", "", szVehicle_Towing, "");
                    }
                    editBackClassType();
                    editBackPayType();
                }

                object objParams = new
                {
                    EditType        = iEditType,
                    towingState     = (int)TCT_Lib.eTowingState,
                    towing_Class1   = TCT_Lib.iCurTC_Class,
                    towing_Fare1    = TCT_Lib.fCurFareAmount,
                    towing_PayType1 = TCT_Lib.xCurPayment == null ? 0 : TCT_Lib.xCurPayment.payType,
                    towing_Class2   = TCT_Lib.iTowingTC_Class,
                    towing_Fare2    = TCT_Lib.fTowingFareAmount,
                    towing_PayType2 = TCT_Lib.xTowingPayment == null ? 0 : TCT_Lib.xTowingPayment.payType
                };
                TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToClient("LANE_EDIT", objParams);
            }
            else if (iKey == (int)KeyPressedType.FORCE_RESET)
            {
                LogInfo.WriteLaneEvent("Press FORCE_RESET");
                //----------------------------------------------
                if (!CheckAllowRepeatKeyChar((int)KeyPressedType.FORCE_RESET))
                {
                    LogInfo.WriteLaneEvent($"- Ignore Key Force Reset because key stroke time less than {TCT_Lib.iKeyStrokeTimeMilliSec} ms.");
                    return;
                }

                //--------------------------------------------------------
                //--- Check towing, if found payment is emv then ignore force reset
/*
                if (TCT_Lib.eTowingState != TowingState.None)
                {
                    if (TCT_Lib.xCurPayment != null)
                    {
                        if ((TCT_Lib.xCurPayment.payType == (int)PayType.EMV) || (TCT_Lib.xCurPayment.payType == (int)PayType.QrCode))
                        {
                            LogInfo.WriteLaneEvent($"- Block Key FORCE RESET because found payment EMV success (First vehicle).");
                            return;
                        }
                    }

                    if (TCT_Lib.xTowingPayment.payType != 0)
                    {
                        if ((TCT_Lib.xTowingPayment.payType == (int)PayType.EMV) || (TCT_Lib.xTowingPayment.payType == (int)PayType.QrCode))
                        {
                            LogInfo.WriteLaneEvent($"- Block Key FORCE RESET because found payment EMV success (Towing vehicle).");
                            return;
                        }
                    }
                }
*/

                if (TCT_Lib.eTowingState != TowingState.None)
                {
                    if (TCT_Lib.xCurPayment != null)
                    {
                        if (((TCT_Lib.xCurPayment.payType == (int)PayType.EMV) || (TCT_Lib.xCurPayment.payType == (int)PayType.QrCode)) &&
                            (TCT_Lib.xTowingPayment.payType == 0))
                        {
                            LogInfo.WriteLaneEvent($"- Block Key FORCE RESET because found payment EMV success (First vehicle).");
                            return;
                        }

                        //if (((TCT_Lib.xCurPayment.payType != (int)PayType.EMV) && (TCT_Lib.xCurPayment.payType != (int)PayType.QrCode)) || 
                        //    (TCT_Lib.xTowingPayment == null))
                        //{
                        //    LogInfo.WriteLaneEvent($"- Block Key FORCE RESET because found payment EMV success (Second vehicle).");
                        //    return;
                        //}
                   
                    }                  
                }

                //---------------------------------------------
                iCapture_nTrx = TCT_Lib.nTrx - 1;
                //------------------------------------------------------------------------------------------------
                //--- If Towing Mode, First vehicle success transaction and system cannot detect second vehicle,
                //--- then when use  press force reset, system will auto create transaction of second vehicle
                if (TCT_Lib.eTowingState != TowingState.None)
                {
                    //--- Check first vehicle success transaction
                    if (!bTowingAVC)
                    {
                        AddAvcTimer(1);
                        Perform_ForceEventAVC();
                        bTowingAVC = true;

                        Thread.Sleep(500);
                    }
                    if (bTowingAVC)
                    {
                        AddAvcTimer(1);
                        Perform_ForceEventAVC();
                    }
                }
                else {
                    //--- None Towing, if Class and Payment already, then force end transaction
                    //if (((TCT_Lib.iCurTC_Class > 0) && (TCT_Lib.bAfterPayment)) ||
                    //    (TCT_Lib.xListLoopCar.Count > 0))
                    if ((TCT_Lib.iCurTC_Class > 0) && (TCT_Lib.bAfterPayment))
                    {
                        //if (bDetectLoopBack)
                        LogInfo.WriteLaneEvent($"+ Force Reset : Found TC Class={TCT_Lib.iCurTC_Class} and Found Payment already (PayType={TCT_Lib.xCurPayment.payType}, {TCT_Lib.xCurPayment.payTypeName}). Then call ForceEnd Transaction.");
                        Perform_ForceEndTransaction();
                        //return;
                    }
                }

                //--- Check if found Loop On and OB On, then staff must force reset first
                if ((bInPassageLoop && bInPassageOB) || (bTrigDownLoop && bInPassageOB))
                {
                    LogInfo.WriteLaneEvent($"+ Force Reset from Loop On/OFF and OB ON Found : Then call ForceEnd Transaction. TC Class={TCT_Lib.iCurTC_Class}, PayType={TCT_Lib.xCurPayment.payType}, {TCT_Lib.xCurPayment.payTypeName}");
                    Perform_ForceEndTransaction();
                }

                //------------------------------------------------------------
                int iAlarmID = (int)TCT_Lib.LaneDB.WriteLaneAlarmRTO(AlarmDevice.FORCE_RESET, AlarmType.Occurred, $"", "");
                TCT_Lib.SendLaneAlarmObjToRTO(iAlarmID);
                //--------------------------------------------------------------
                //-------------------------------------------------------------------------------------------------
                if (TCT_Lib.bAllowETC && TCT_Lib.bAfterPayment)
                {
                    bReClassETC = true;
                    //TCT_Lib.etcRabbitMQ.SendPassagePaymentToETC((int)TCT_Lib.LaneDB.GetCurTransID(), TCT_Lib.iCurTC_Class, TCT_Lib.xCurPayment.payType, TCT_Lib.xCurPayment.payAmount, ALB_Mode.Normal, true, false);
                }

                //--------------------------------------------------------------------------------------------------
                TCT_Lib.iTotalQueue     = 0;
                iQueueALB               = 0;
                eCurVehicleType         = VehicleType.NONE;
                eCurPayType             = PayType.NONE;
                iStepNextCar            = 0;
                szPrevPAN               = "";
                bHasViolation           = false;
                bLoopMis                = false;
                bTowingAVC              = false;
                bReClassETC             = false;
                bEditBlockETC           = false;
                bEditDual               = false;

                TCT_Lib.xListLoopCar.Clear();
                showQueueAlbControl(iQueueALB);
                showTotalLoop();
                TCT_Lib.iNextTC_Class   = 0;
                TCT_Lib.eTowingState    = TowingState.None;
                vehicleTypeLaneControl(eCurVehicleType);
                payTypeLaneControl(eCurPayType);
                //LogInfo.WriteLog("- ClearUI from ForceReset");
                clearPanelUiLaneControl();
                TCT_Lib.ClearCurTransData();
                TCT_Lib.webSocketLane.OutIo_ClearCRS();
                LogInfo.WriteLaneEvent("Send to IO - ASB : OFF, Siren : OFF, LTL : OFF");
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.Siren, OFF);
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ASB, OFF);
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.LTL, OFF);
                if (TCT_Lib.eCurLaneMode == LaneMode.CLOSED)
                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.CloseLane, (int)TFI_Pattern.Normal, "", "", "", szTFI_CloseLane, "");
                else
                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Welcome, (int)TFI_Pattern.Normal, "", "", "", szTFI_StartLane1, szTFI_StartLane2);
                //if (iTimerClearCRS == 0)
                //    iTimerClearCRS = GLib.Timeout.Add((uint)(iSecClearCRS * 1000), new GLib.TimeoutHandler(ClearTextCRS));

                RemoveSirenTimer();
                if (TCT_Lib.eCurLaneMode != LaneMode.HPMC)
                    SendDigitalOutput_CmdALB_Close();

                //SendEtcRabbitMq_CmdALB_Normal("Close Force Reset");
                if (TCT_Lib.bAllowETC)
                {
                    TCT_Lib.etcRabbitMQ.Send_ClearQueue_ToETC();
                    int iAlarmID2 = (int)TCT_Lib.LaneDB.WriteLaneAlarmRTO(AlarmDevice.ETC_CLEARQUEUE, AlarmType.Occurred, $"Clear Queue (ForceReset ETC) Last PAN No.={TCT_Lib.szEtcLastUpdateObu}, Last Update = {TCT_Lib.dtEtcLastUpdateObu.ToString("yyyy-MM-dd HH:mm:ss")}", $"Last PAN No.={TCT_Lib.szEtcLastUpdateObu}, Last Update = {TCT_Lib.dtEtcLastUpdateObu.ToString("yyyy-MM-dd HH:mm:ss")}");
                    TCT_Lib.SendLaneAlarmObjToRTO(iAlarmID2);
                }
                bHasViolation = false;
                Perform_StopSiren();
#if __SIM__
                //--- Sim Device Input LC
                TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.PresenceLoop, OFF);
                TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.PassageLoop, OFF);
                TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.PresenceOB, OFF);
                TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.PassageOB, OFF);
#endif
            }
            else if (iKey == (int)KeyPressedType.RECEIPT)
            {
                //----------------------------------------------
                if (!CheckAllowRepeatKeyChar((int)KeyPressedType.RECEIPT))
                {
                    //LogInfo.WriteLaneEvent($"- Ignore Key PrintReceipt because key stroke time less than {TCT_Lib.iKeyStrokeTimeMilliSec} ms.");
                    LogInfo.WriteLaneEvent($"- Ignore Key PrintReceipt because key stroke time less than 2 sec.");
                    return;
                }
                //---------------------------------------------
                LogInfo.WriteLaneEvent("Press PRINT RECEIPT");

                if (bHasViolation)
                    return;

                if (TCT_Lib.eTowingState == TowingState.None)
                {
                    PrintUtils.PrintReceiptLib.PrintReceipt(TCT_Lib.nTrx);
                    //PrintUtils.PrintGraphicLib.PrintReceipt();
                    //TCT_Lib.bHasGenReceiptNo = true;
                }
                else
                {
                    if (TCT_Lib.eTowingState == TowingState.SecPayType)
                    {
                        PrintUtils.PrintReceiptLib.PrintReceipt(TCT_Lib.nTrx);
                        Thread.Sleep(1500);
                        PrintUtils.PrintReceiptLib.PrintReceipt(TCT_Lib.nTrx + 1, true);
                        //TCT_Lib.bHasGenReceiptNo = true;
                    }
                }
                if (TCT_Lib.bConvoyMode && (TCT_Lib.xListLoopCar.Count == 0))
                {
                    TCT_Lib.bAllowConvoy = false;
                    LogInfo.WriteLaneEvent("-6 AllowConvoy = false, ListLoopCar=0");
                }
            }
            else if (iKey == (int)KeyPressedType.MANUAL)
            {
                LogInfo.WriteLaneEvent("Press MANUAL LOGIN");
                bManualLogin    = true;
                szLoginKey      = szPasswordKey = "";
                startManualStaffLogin();
            }
            else if (iKey == (int)KeyPressedType.ESC)
            {
                if (bManualLogin)
                {
                    bManualLogin = false;
                    szLoginKey = szPasswordKey = "";
                    cancelManualStaffLogin();
                }
            }
            else if (iKey == (int)KeyPressedType.ENTER)
            {
                LogInfo.WriteLaneEvent("Press ENTER");
                if (bManualLogin)
                {
                    if (szLoginKey.Trim() != "")
                    {
                        if (CheckManualStafffLogin())
                        {
                            bManualLogin = false;
                            szLoginKey = szPasswordKey = "";
                            manualStaffLoginStatus(true, "");
                        }
                        else
                            manualStaffLoginStatus(false, "รหัสพนักงานไม่ถูกต้อง");
                    }
                    else
                        manualStaffLoginStatus(false, "กรุณาใส่รหัสพนักงาน");
                }
            }
            else if (iKey == (int)KeyPressedType.SALE_COUPON)
            {
                LogInfo.WriteLaneEvent("Press SALES COUPON");
                salesCoupon();
            }
            else if (iKey == (int)KeyPressedType.PRINT_CONVOY)
            {
                LogInfo.WriteLaneEvent("Press PRINT CONVOY");
                printConvoy();
            }
            else if ((iKey >= (int)KeyPressedType.NUM_0) && (iKey <= (int)KeyPressedType.NUM_9))
            {
                LogInfo.WriteLaneEvent("Press " + (iKey - (int)KeyPressedType.NUM_0));
                if (bManualLogin)
                {
                    szLoginKey += (char)('0' + (iKey - (int)KeyPressedType.NUM_0));
                    manualKeyStaffCode(szLoginKey);
                }
            }
            else if (iKey == (int)KeyPressedType.MODE)
            {
                LogInfo.WriteLaneEvent("Press MODE");
                if (TCT_Lib.eCurAppPage != AppPage.Login)
                {
                    LogInfo.WriteLog("- Fail SetLaneTypeMode : Current AppPage is not Login Page");
                    return;
                }

                setLaneTypeMode();
            }
        }

        protected bool CheckManualStafffLogin()
        {

            return true;
        }

        protected string GetStrClassTypeCRS(VehicleType classType)
        {
            switch (classType)
            {
                case VehicleType.CLASS_1:
                    return "1";
                case VehicleType.CLASS_2:
                    return "2";
                case VehicleType.EMERGENCY:
                    return "9";
                case VehicleType.HPMC:
                    return " ";
                case VehicleType.TOWING:
                    return "+";
                case VehicleType.EXEMPT:
                case VehicleType.LEFT_EXIT:
                    return "0";
                default:
                    return "";
            }
        }

        protected string GetStrPayTypeCRS(PayType payType)
        {
            switch (payType)
            {
                case PayType.CASH:
                    return "C";
                case PayType.COUPON:
                    return "V";
                case PayType.MPass:
                case PayType.EasyPass:
                    return "T";
                case PayType.EMV:
                    return "M";
                case PayType.QrCode:
                    return "Q";
                case PayType.EXEMPT:
                    return "E";
                case PayType.DMT_CARD:
                    return "E";
                case PayType.HPMC:
                    return "H";
                case PayType.LeftExit:
                    return "L";
                case PayType.PM6:
                    return TCT_Lib.szPaymentCRS_06;
                case PayType.PM7:
                    return TCT_Lib.szPaymentCRS_07;
                case PayType.PM8:
                    return TCT_Lib.szPaymentCRS_08;
                case PayType.PM9:
                    return TCT_Lib.szPaymentCRS_09;
                default:
                    return "";
            }
        }

        //--------------------------------------------------------------
        protected void AddAvcTimer(uint iAvcTimer)
        {
            /*
                        foreach (uint iTimer in iWaitAvcTimerIds)
                        {
                            if (iTimer == iAvcTimer)
                                return;
                        }
            */
            RemoveAvcTimer();
            iWaitAvcTimerIds.Add(iAvcTimer);
            bEnableWaitAVC = true;
        }

        protected void RemoveAvcTimer()
        {
            foreach (uint iAvcTimer in iWaitAvcTimerIds.ToList())
                GLib.Source.Remove(iAvcTimer);
            iWaitAvcTimerIds.Clear();
            bEnableWaitAVC = false;
        }

        protected void AddSirenTimer(uint iSirenTimer)
        {
            /*
                        foreach (uint iTimer in iSirenTimerIds.ToList())
                        {
                            if (iTimer == iSirenTimer)
                                return;
                        }
            */
            RemoveSirenTimer();
            iSirenTimerIds.Add(iSirenTimer);
        }

        protected void RemoveSirenTimer()
        {
            foreach (uint iTimer in iSirenTimerIds.ToList())
                GLib.Source.Remove(iTimer);
            iSirenTimerIds.Clear();
        }

        //==============================================================
        //=== Thread Process
        protected void InitialEndTransactionThread()
        {
            endTransactionThread                = new Thread(new ThreadStart(ThreadEndTransaction));
            endTransactionThread.Priority       = ThreadPriority.AboveNormal;
            endTransactionThread.IsBackground   = true;
            endTransactionThread.Start();
        }


        //--- Wait and check AVC then create transaction
        //--- v2.0.0: This was an empty `while(true){ Thread.Sleep(100); }` stub that only
        //--- wasted a CPU-bound thread (Priority.AboveNormal) and blocked clean shutdown.
        //--- The body was never implemented, so the thread now exits immediately.
        private void ThreadEndTransaction()
        {
            return;
        }


        //--------------------------------------------------------------
        protected bool Perform_ForceEndTransaction()
        {
            LogInfo.WriteLog("+ Force End Transaction");
            try
            {
                int iAutoClass      = 255;
                bAutoAVC            = true;
                //bDetectLoopBack = true;
                TCT_Lib.bSimuPassFlag = true;
                LaneSocket_AvcLaneEvent(iAutoClass, 0, 0, 0, 0, 0, 0, true);
                LogInfo.WriteLaneEvent($"Auto AVC : AutoClass={iAutoClass}");
                //--- Manual Send AVC message to RTO
                string method       = "AVC";
                System.Object objParams = new { vehicleclass = iAutoClass, direction = 0, axle = 0, wheel = 0, dualwheel = 0, height = 0, error = 0 };
                TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToClient(method, objParams);

                //---- Clear UI
                if (TCT_Lib.bMainUiReady)
                {
                    //LogInfo.WriteLog("- ClearUI from ForceEndTransaction");
                    clearPanelUiLaneControl();
                }

                RemoveAvcTimer();

                return true;
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Error Perform_AutoEndTransaction : " + err.Message);
                return false;
            }
        }

        protected bool Perform_ForceEventAVC()
        {
            if (!bEnableWaitAVC)
                return false;

            if (iWaitAvcTimerIds.Count > 0)
                RemoveAvcTimer();

            //--------------------------------------------------
            if (TCT_Lib.bMaintenanceMode)
            {
                if (TCT_Lib.eCurAppPage != AppPage.Main)
                    return false;
            }
            //--------------------------------------------------
#if __SIM__
            /*
                        int iAutoClass      = TCT_Lib.xListLoopCar.Count > 0 ? TCT_Lib.xListLoopCar[0].iTC_Class : TCT_Lib.iCurTC_Class;     // Sim AVC Class = Cur TC Class
                        if (iAutoClass > 2)
                            iAutoClass = 1;
                        if (bTowingAVC)
                            iAutoClass = 255;
                        else 
                        {
                            if (iAutoClass == 0)
                                iAutoClass = 255;
                        }
            */
            int iAutoClass = 255;
#else
            int iAutoClass          = 255;
#endif
            bAutoAVC        = true;
            LaneSocket_AvcLaneEvent(iAutoClass, 0, 0, 0, 0, 0, 0, true);
            LogInfo.WriteLaneEvent($"Auto AVC : AutoClass={iAutoClass}");
            //--- Manual Send AVC message to RTO
            string method   = "AVC";
            System.Object objParams = new { vehicleclass = iAutoClass, direction = 0, axle = 0, wheel = 0, dualwheel = 0, height = 0, error = 0 };
            TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToClient(method, objParams);

            RemoveAvcTimer();

            return false;
        }

        protected bool Perform_ChkEventAVC()
        {
            if (!bEnableWaitAVC)
                return false;

            if (iWaitAvcTimerIds.Count > 0)
                RemoveAvcTimer();

            //--------------------------------------------------
            if (TCT_Lib.bMaintenanceMode)
            {
                if (TCT_Lib.eCurAppPage != AppPage.Main)
                    return false;
            }
            //--------------------------------------------------
#if __SIM__
            /*
                        int iAutoClass      = TCT_Lib.xListLoopCar.Count > 0 ? TCT_Lib.xListLoopCar[0].iTC_Class : TCT_Lib.iCurTC_Class;     // Sim AVC Class = Cur TC Class
                        if (iAutoClass > 2)
                            iAutoClass = 1;
                        if (bTowingAVC)
                            iAutoClass = 255;
                        else 
                        {
                            if (iAutoClass == 0)
                                iAutoClass = 255;
                        }
            */
            int iAutoClass = 255;
#else
            int iAutoClass          = 255;
#endif
            bAutoAVC        = true;
            LaneSocket_AvcLaneEvent(iAutoClass, 0, 0, 0, 0, 0, 0);
            LogInfo.WriteLaneEvent($"Auto AVC : AutoClass={iAutoClass}");
            //--- Manual Send AVC message to RTO
            string method   = "AVC";
            System.Object objParams = new { vehicleclass = iAutoClass, direction = 0, axle = 0, wheel = 0, dualwheel = 0, height = 0, error = 0 };
            TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToClient(method, objParams);

            RemoveAvcTimer();

            return false;
        }

        protected void LaneSocket_AvcLaneEvent(int classType, int direction, int axle, int wheel, int dualwheel, int height, int error, bool bForceEndTrans = false)
        {
            Console.WriteLine(string.Format("+ AVC Event : class={0}, direction={1}, axle={2}, wheel={3}, dualwheel={4}, height={5}, error={6}",
                                            classType, direction, axle, wheel, dualwheel, height, error));
            LogInfo.WriteLaneEvent($"AVC : class={classType}, axle={axle}, wheel={wheel}, dualwheel={dualwheel}, direction={direction}, error={error}");

            if (iWaitAvcTimerIds.Count > 0)
                RemoveAvcTimer();

            //--------------------------------------------------
            if (TCT_Lib.bMaintenanceMode)
            {
                if (TCT_Lib.eCurAppPage != AppPage.Main)
                    return;
            }
            //--------------------------------------------------
            if (TCT_Lib.eCurLaneType == LaneType.ETC)
            {
                if (bHasViolation)
                    bHasViolation = false;
            }

            TCT_Lib.iLastAvcStatus  = error;

            if (TCT_Lib.iAvcStatusType == 1)
            {
                //--- Check Loop and OB Status from Real Device (Error Field)
                //int numPos = error >> 1;
                //int iFoundLoop = numPos & 1;
                //numPos = error >> 2;
                //int iFoundOB = numPos & 1;
                bRealPassLoop = TCT_Lib.GetBit((byte)error, 1);
                bRealPassOB   = TCT_Lib.GetBit((byte)error, 2);
            }

            //--- 2022-04-27 : Fix not found staff id from login multiple smart card
            if (TCT_Lib.szStaffID != null)
            {
                if (TCT_Lib.LaneDB != null)
                {
                    if ((TCT_Lib.szStaffID.Trim() == "") && (TCT_Lib.iStaffShiftID > 0))
                        TCT_Lib.LaneDB.SetStaffFromStaffShift(TCT_Lib.iStaffShiftID);
                }
            }
            //-----------------------------------------------

            //--------------------------------------------
            //bHasViolation = false;
            //--------------------------------------------
            //--- Check Back Passage OB/Loop not working, then clear UI and alert device has error
            //if ((!bFoundLoopOB && !bHasViolation) || bHasViolation)
            //if ((!bDetectObBack && !bHasViolation) || bHasViolation || (!bFoundLoopOB && !bHasViolation))
            //--- 2021-12-16 - ETC
            //if (!bFoundLoop || bHasViolation || (bDetectLoopBack && !bDetectObBack) || (!bFoundOB && !bTrigDownOB & bDetectObBack)
            //    || (bFoundLoop && !bFoundOB && !bTrigDownOB & !bDetectObBack)
            //    || (bFoundLoop && bFoundOB && bDetectLoopBack && bDetectObBack && !bHasViolation && (TCT_Lib.eTowingState != TowingState.SecPayType))
            //    )
            if ((!bFoundLoop || bHasViolation || (bDetectLoopBack && !bDetectObBack) || (!bFoundOB && !bTrigDownOB & bDetectObBack)
                || (bFoundLoop && !bFoundOB && !bTrigDownOB & !bDetectObBack)
                || (bFoundLoop && bFoundOB && bDetectLoopBack && bDetectObBack && !bHasViolation && (TCT_Lib.eTowingState != TowingState.SecPayType))
                ) || (TCT_Lib.eCurLaneType == LaneType.ETC))
            {
                if (TCT_Lib.eTowingState == TowingState.None)
                {
                    if (TCT_Lib.eCurLaneMode != LaneMode.HPMC)
                    {
                        if (TCT_Lib.bMainUiReady)
                        {
                            if ((TCT_Lib.iNextTC_Class > 0) && bDetectLoopBack)
                            {
                                //LogInfo.WriteLog("- ClearUI from AVC-Violation");
                                if (TCT_Lib.iTotalQueue == 0)
                                    clearPanelUiLaneControl();
                                else
                                    Process_RefreshPresenceLoop();
                            }
                        }

                        //--- Send alarm to RTO
                        //int iAlarmID = (int)TCT_Lib.LaneDB.WriteLaneAlarmRTO(AlarmDevice.LANE_IO, AlarmType.Occurred, "Fail Passage Loop/OB", "Found AVC, But not found Passage Loop/OB");
                        //TCT_Lib.SendLaneAlarmObjToRTO(iAlarmID);

                        //-------------------------------------------------------------------------
                        TimeSpan span = DateTime.Now.Subtract(dtLastTrans);
                        //if (span.Seconds >= 1)
                        {
                            int iTotalLoop      = TCT_Lib.xListLoopCar == null ? 0 : TCT_Lib.xListLoopCar.Count;
                            int iTC_Class       = iTotalLoop == 0 ? TCT_Lib.iCurTC_Class : TCT_Lib.xListLoopCar[0].iTC_Class;
                            Pay_Info xPayment   = iTotalLoop == 0 ? TCT_Lib.xCurPayment : TCT_Lib.xListLoopCar[0].xPayment;
                            string szTransRemark = iTotalLoop == 0 ? TCT_Lib.szCurTransRemark : TCT_Lib.xListLoopCar[0].szTransRemark;
                            showTotalLoop();

                            //-------------------
                            if (xPayment == null)
                            {
                                Console.Write("- Payment is null");
                                LogInfo.WriteLaneEvent("- Payment is null");
                                return;
                            }

                            //----------------------------------------------------------------------------------
                            if (TCT_Lib.eCurLaneType != LaneType.ETC)
                            {
                                if (((iTC_Class > 0) && (xPayment.payType == 0)) || (iTC_Class == 0))
                                {
                                    if (!bHasViolation)
                                    {
                                        LogInfo.WriteLaneEvent($"TC_Class={iTC_Class}, PayType={xPayment.payType}");
                                        LogInfo.WriteLaneEvent("+ Found Violation #2");
                                        //Found Violation #2
                                        //-------------------------------------------------------------------------------------
                                        if ((TCT_Lib.iVideoNVR_CaptureImageMethod == 0) || (TCT_Lib.iVideoNVR_CaptureImageMethod == 1))
                                        {
                                            //--- Fix Open from RTO and has violation, use nTrx-1 (2024-08-21)
                                            if (TCT_Lib.bStatusOpenALB_RTO)
                                                TCT_Lib.nTrx--;
                                            CreateProcessVideoCaptureImage(TCT_Lib.nTrx, false, true);
                                        }
                                        //---------------------------------------------------------------------------------------

                                        bHasViolation   = true;
                                        //--- Reset State
                                        eCurVehicleType = VehicleType.NONE;
                                        eCurPayType     = PayType.NONE;
                                        iStepNextCar    = 0;

                                        //--- Send alarm to RTO
                                        int iAlarmID = (int)TCT_Lib.LaneDB.WriteLaneAlarmRTO(AlarmDevice.LANE_IO, AlarmType.Occurred, "Fail Passage Loop/OB", "Found AVC, But not found Passage Loop/OB");
                                        TCT_Lib.SendLaneAlarmObjToRTO(iAlarmID);

                                        //--- Event Violation
                                        eventViolation();

                                        if (TCT_Lib.xListLoopCar.Count == 0)
                                        {
                                            TCT_Lib.iCurTC_Class                    = TCT_Lib.iCurTC_Class > 2 ? (int)VehicleType.VIOLATION : TCT_Lib.iCurTC_Class;
                                            TCT_Lib.xCurPayment.payAmount           = 0;
                                            TCT_Lib.xCurPayment.payType             = (int)PayType.Violation;
                                            TCT_Lib.xCurPayment.payTypeName         = "VIOLATION";
                                            TCT_Lib.xCurPayment.payDateTime         = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                                            TCT_Lib.szCurTransRemark                = "VIOLATION";
                                        }
                                        else
                                        {
                                            TCT_Lib.xListLoopCar[0].iTC_Class               = TCT_Lib.xListLoopCar[0].iTC_Class > 2 ? (int)VehicleType.VIOLATION : TCT_Lib.xListLoopCar[0].iTC_Class;
                                            TCT_Lib.xListLoopCar[0].xPayment.payAmount      = 0;
                                            TCT_Lib.xListLoopCar[0].xPayment.payType        = (int)PayType.Violation;
                                            TCT_Lib.xListLoopCar[0].xPayment.payTypeName    = "VIOLATION";
                                            TCT_Lib.xListLoopCar[0].xPayment.payDateTime    = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                                            TCT_Lib.xListLoopCar[0].szTransRemark           = "VIOLATION";
                                        }

                                        szPrevPAN       = "";
                                        eCurVehicleType = VehicleType.VIOLATION;
                                        vehicleTypeLaneControl(VehicleType.VIOLATION);
                                        //if (iSirenTimerIds.Count == 0)
                                        {
                                            LogInfo.WriteLaneEvent("IO - Siren : ON, ASB : ON");
                                            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.Siren, ON);
                                            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ASB, ON);

                                            uint iSirenTimerId = GLib.Timeout.Add((uint)TCT_Lib.iStopSirenAfterTimeSec * 1000, new GLib.TimeoutHandler(Perform_StopSiren));
                                            AddSirenTimer(iSirenTimerId);
                                        }
                                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.HPMC, (int)TFI_Pattern.Violation, "", "", "", szVehicle_Violation, "");

                                        TCT_Lib.LaneDB.UpdateTransStatistic_Violation();
                                        TCT_Lib.bStartEndLoop = true;
                                        TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToClient("LANE_VIOLATION", null);
                                        //--- Send Lane Statisitc
                                        SendLaneCurrentTransStatistic();
                                    }
                                    else
                                    {
                                        //-------------------------------------------------------------------
                                        //--- Reset State
                                        if (eCurVehicleType != VehicleType.VIOLATION)
                                        {
                                            LogInfo.WriteLaneEvent("+ Found Violation #3");
                                            eCurVehicleType = VehicleType.NONE;
                                            eCurPayType     = PayType.NONE;
                                            iStepNextCar    = 0;

                                            //--- Send alarm to RTO
                                            int iAlarmID = (int)TCT_Lib.LaneDB.WriteLaneAlarmRTO(AlarmDevice.LANE_IO, AlarmType.Occurred, "Fail Passage Loop/OB", "Found AVC, But not found Passage Loop/OB");
                                            TCT_Lib.SendLaneAlarmObjToRTO(iAlarmID);

                                            //--- Event Violation
                                            eventViolation();

                                            if (TCT_Lib.xListLoopCar.Count == 0)
                                            {
                                                TCT_Lib.iCurTC_Class                = TCT_Lib.iCurTC_Class > 2 ? (int)VehicleType.VIOLATION : TCT_Lib.iCurTC_Class;
                                                TCT_Lib.xCurPayment.payAmount       = 0;
                                                TCT_Lib.xCurPayment.payType         = (int)PayType.Violation;
                                                TCT_Lib.xCurPayment.payTypeName     = "VIOLATION";
                                                TCT_Lib.xCurPayment.payDateTime     = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                                                TCT_Lib.szCurTransRemark            = "VIOLATION";
                                            }
                                            else
                                            {
                                                TCT_Lib.xListLoopCar[0].iTC_Class               = TCT_Lib.xListLoopCar[0].iTC_Class > 2 ? (int)VehicleType.VIOLATION : TCT_Lib.xListLoopCar[0].iTC_Class;
                                                TCT_Lib.xListLoopCar[0].xPayment.payAmount      = 0;
                                                TCT_Lib.xListLoopCar[0].xPayment.payType        = (int)PayType.Violation;
                                                TCT_Lib.xListLoopCar[0].xPayment.payTypeName    = "VIOLATION";
                                                TCT_Lib.xListLoopCar[0].xPayment.payDateTime    = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                                                TCT_Lib.xListLoopCar[0].szTransRemark           = "VIOLATION";
                                            }

                                            szPrevPAN           = "";
                                            eCurVehicleType     = VehicleType.VIOLATION;
                                            vehicleTypeLaneControl(VehicleType.VIOLATION);
                                            //if (iSirenTimerIds.Count == 0)
                                            {
                                                LogInfo.WriteLaneEvent("IO - Siren : ON, ASB : ON");
                                                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.Siren, ON);
                                                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ASB, ON);

                                                uint iSirenTimerId = GLib.Timeout.Add((uint)TCT_Lib.iStopSirenAfterTimeSec * 1000, new GLib.TimeoutHandler(Perform_StopSiren));
                                                AddSirenTimer(iSirenTimerId);
                                            }
                                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.HPMC, (int)TFI_Pattern.Violation, "", "", "", szVehicle_Violation, "");

                                            TCT_Lib.LaneDB.UpdateTransStatistic_Violation();
                                            TCT_Lib.bStartEndLoop = true;
                                            TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToClient("LANE_VIOLATION", null);
                                            //--- Send Lane Statisitc
                                            SendLaneCurrentTransStatistic();
                                        }

                                        //---------------------------------------------------------
                                        //CreateProcessVideoCaptureImage(TCT_Lib.nTrx);
                                        //----------------------------------------------------------------------------------------
                                    }
                                }
                            }
                        }
/*
                    else
                    {
                        LogInfo.WriteLaneEvent("- Fail create transaction violation 2. : " + span.Milliseconds);
                    }
*/
                        //-----------------------------------------------------------------------------
                    }
                }
                else
                {
                    //--- 2021-11-06
                    //--- Check Violation for Towing
                    if ((TCT_Lib.eTowingState != TowingState.SecPayType) &&
                        (TCT_Lib.xListLoopCar.Count == 0))
                    //(((TCT_Lib.iCurTC_Class == 0) && (TCT_Lib.xListLoopCar.Count <= 1)) ||
                    //((TCT_Lib.iCurTC_Class >= 0) && (TCT_Lib.xListLoopCar.Count == 0))))
                    {
                        int iTotalLoop      = TCT_Lib.xListLoopCar == null ? 0 : TCT_Lib.xListLoopCar.Count;
                        int iTC_Class       = iTotalLoop == 0 ? TCT_Lib.iCurTC_Class : TCT_Lib.xListLoopCar[0].iTC_Class;
                        Pay_Info xPayment   = iTotalLoop == 0 ? TCT_Lib.xCurPayment : TCT_Lib.xListLoopCar[0].xPayment;
                        string szTransRemark = iTotalLoop == 0 ? TCT_Lib.szCurTransRemark : TCT_Lib.xListLoopCar[0].szTransRemark;
                        showTotalLoop();
                        //----------------------------------------------------------------------------------
                        if (((TCT_Lib.iTowingTC_Class > 0) && (TCT_Lib.fTowingFareAmount == 0)) || (TCT_Lib.iTowingTC_Class == 0))
                        {
                            //Found Violation #4
                            //-------------------------------------------------------------------------------------
                            if ((TCT_Lib.iVideoNVR_CaptureImageMethod == 0) || (TCT_Lib.iVideoNVR_CaptureImageMethod == 1))
                            {
                                //--- Fix Open from RTO and has violation, use nTrx-1 (2024-08-21)
                                if (TCT_Lib.bStatusOpenALB_RTO)
                                    TCT_Lib.nTrx--;
                                CreateProcessVideoCaptureImage(TCT_Lib.nTrx, false, true);
                            }
                            //---------------------------------------------------------------------------------------

                            TCT_Lib.iIsTowing   = 1;
                            LogInfo.WriteLaneEvent("+ Found Violation #4");
                            bHasViolation       = true;
                            //--- Reset State
                            eCurVehicleType     = VehicleType.NONE;
                            eCurPayType         = PayType.NONE;
                            iStepNextCar        = 0;

                            //--- Send alarm to RTO
                            int iAlarmID        = (int)TCT_Lib.LaneDB.WriteLaneAlarmRTO(AlarmDevice.LANE_IO, AlarmType.Occurred, "Fail Passage Loop/OB", "Found AVC, But not found Passage Loop/OB");
                            TCT_Lib.SendLaneAlarmObjToRTO(iAlarmID);

                            //--- Event Violation
                            eventViolation();

                            if (TCT_Lib.xListLoopCar.Count == 0)
                            {
                                TCT_Lib.iCurTC_Class                = TCT_Lib.iCurTC_Class > 2 ? (int)VehicleType.VIOLATION : TCT_Lib.iCurTC_Class;
                                TCT_Lib.xCurPayment.payAmount       = 0;
                                TCT_Lib.xCurPayment.payType         = (int)PayType.Violation;
                                TCT_Lib.xCurPayment.payTypeName     = "VIOLATION";
                                TCT_Lib.xCurPayment.payDateTime     = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                                TCT_Lib.szCurTransRemark            = "TOWING";
                            }
                            else
                            {
                                TCT_Lib.xListLoopCar[0].iTC_Class               = TCT_Lib.xListLoopCar[0].iTC_Class > 2 ? (int)VehicleType.VIOLATION : TCT_Lib.xListLoopCar[0].iTC_Class;
                                TCT_Lib.xListLoopCar[0].xPayment.payAmount      = 0;
                                TCT_Lib.xListLoopCar[0].xPayment.payType        = (int)PayType.Violation;
                                TCT_Lib.xListLoopCar[0].xPayment.payTypeName    = "VIOLATION";
                                TCT_Lib.xListLoopCar[0].xPayment.payDateTime    = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                                TCT_Lib.xListLoopCar[0].szTransRemark           = "TOWING";
                            }

                            szPrevPAN       = "";
                            eCurVehicleType = VehicleType.VIOLATION;
                            vehicleTypeLaneControl(VehicleType.VIOLATION);
                            //if (iSirenTimerIds.Count == 0)
                            {
                                LogInfo.WriteLaneEvent("IO - Siren : ON, ASB : ON");
                                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.Siren, ON);
                                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ASB, ON);

                                uint iSirenTimerId = GLib.Timeout.Add((uint)TCT_Lib.iStopSirenAfterTimeSec * 1000, new GLib.TimeoutHandler(Perform_StopSiren));
                                AddSirenTimer(iSirenTimerId);
                            }
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.HPMC, (int)TFI_Pattern.Violation, "", "", "", szVehicle_Violation, "");

                            TCT_Lib.LaneDB.UpdateTransStatistic_Violation();
                            TCT_Lib.bStartEndLoop = true;
                            TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToClient("LANE_VIOLATION", null);
                            //--- Send Lane Statisitc
                            SendLaneCurrentTransStatistic();
                        }
                    }
                }
            }
            //bFoundLoopOB = false;

            //-----------------------------------------------------------
            //--- End Lane Process
            if (bTowingAVC && (TCT_Lib.eTowingState != TowingState.None))
            {
                TCT_Lib.xTowingAVC.avcClass         = classType;
                TCT_Lib.xTowingAVC.avcAxle          = axle;
                TCT_Lib.xTowingAVC.avcWheel         = wheel;
                TCT_Lib.xTowingAVC.avcDualWheel     = dualwheel;
                TCT_Lib.xTowingAVC.avcDirection     = direction;
                TCT_Lib.xTowingAVC.avcError         = error;
            }
            else
            {
                TCT_Lib.xCurAVC.avcClass            = classType;
                TCT_Lib.xCurAVC.avcAxle             = axle;
                TCT_Lib.xCurAVC.avcWheel            = wheel;
                TCT_Lib.xCurAVC.avcDualWheel        = dualwheel;
                TCT_Lib.xCurAVC.avcDirection        = direction;
                TCT_Lib.xCurAVC.avcError            = error;
            }

            if ((TCT_Lib.eCurLaneType == LaneType.MTC) || (TCT_Lib.eCurLaneType == LaneType.DUAL))
            {
                //--- Incase cannot detect OB, then auto decrease queue
                if (!bDetectObBack)
                {
                    TCT_Lib.iTotalQueue--;
                    if (TCT_Lib.iTotalQueue < 0)
                        TCT_Lib.iTotalQueue = 0;
                    updateUiQueueLaneControl();
                }
            }

            showAvcLaneControl(classType);
            //updateUI_Convoy(TCT_Lib.xListLoopCar.Count == 0 ? TCT_Lib.iCurTC_Class : TCT_Lib.xListLoopCar[0].iTC_Class);
            if (TCT_Lib.bConvoyMode)
            {
                string szTitleConvoy    = "รถขบวนจ่ายเงิน: ";
                string szTitleNotConvoy = "รถขบวนจ่ายเงิน : รถแทรกขบวน";

                int iTotalConvoy    = TCT_Lib.iTotalConvoyClass1 + TCT_Lib.iTotalConvoyClass2;
                bool bIsConvoy      = true;
                if (TCT_Lib.xListLoopCar.Count == 0)
                {
                    if ((TCT_Lib.iCurTC_Class > 2) || (TCT_Lib.xCurPayment.payType != 1) || !TCT_Lib.bAllowConvoy)
                    {
                        TCT_Lib.szCurTransRemark = szTitleNotConvoy;
                        bIsConvoy               = false;
                        TCT_Lib.bAllowConvoy    = false;
                    }
                    else
                    {
                        TCT_Lib.szCurTransRemark = $"{szTitleConvoy} {iTotalConvoy - TCT_Lib.iConvoyClass1 - TCT_Lib.iConvoyClass2 + 1} / {iTotalConvoy} > Class {TCT_Lib.iCurTC_Class}";
                        /*
                                                if (TCT_Lib.iCurTC_Class == 1)
                                                    TCT_Lib.szCurTransRemark = $"{szTitleConvoy} {TCT_Lib.iTotalConvoyClass1 - TCT_Lib.iConvoyClass1 + 1} / {TCT_Lib.iTotalConvoyClass1} > Class {TCT_Lib.iCurTC_Class}";
                                                else
                                                    TCT_Lib.szCurTransRemark = $"{szTitleConvoy} {TCT_Lib.iTotalConvoyClass2 - TCT_Lib.iConvoyClass2 + 1} / {TCT_Lib.iTotalConvoyClass2} > Class {TCT_Lib.iCurTC_Class}";
                        */
                    }
                    LogInfo.WriteLaneEvent("+1 Convoy Mode : " + TCT_Lib.szCurTransRemark);
                }
                else
                {
                    if ((TCT_Lib.xListLoopCar[0].iTC_Class > 2) || (TCT_Lib.xListLoopCar[0].xPayment.payType != 1 || !TCT_Lib.bAllowConvoy))
                    {
                        TCT_Lib.xListLoopCar[0].szTransRemark = szTitleNotConvoy;
                        bIsConvoy               = false;
                        TCT_Lib.bAllowConvoy    = false;
                        LogInfo.WriteLaneEvent($"- IsConvoy=false, bAllowConvoy=false, Because Class > 2, PayType != 1, Class={TCT_Lib.xListLoopCar[0].iTC_Class}, " +
                                                $"PayType={TCT_Lib.xListLoopCar[0].xPayment.payType}, bAllowConvoy={TCT_Lib.bAllowConvoy}");
                    }
                    else
                    {
                        TCT_Lib.xListLoopCar[0].szTransRemark = $"{szTitleConvoy} {iTotalConvoy - TCT_Lib.iConvoyClass1 - TCT_Lib.iConvoyClass2 + 1} / {iTotalConvoy} > Class {TCT_Lib.xListLoopCar[0].iTC_Class}";
                        /*
                                                if (TCT_Lib.xListLoopCar[0].iTC_Class == 1)
                                                    TCT_Lib.xListLoopCar[0].szTransRemark = $"{szTitleConvoy} {TCT_Lib.iTotalConvoyClass1 - TCT_Lib.iConvoyClass1 + 1} / {TCT_Lib.iTotalConvoyClass1} > Class {TCT_Lib.xListLoopCar[0].iTC_Class}";
                                                else
                                                    TCT_Lib.xListLoopCar[0].szTransRemark = $"{szTitleConvoy} {TCT_Lib.iTotalConvoyClass2 - TCT_Lib.iConvoyClass2 + 1} / {TCT_Lib.iTotalConvoyClass2} > Class {TCT_Lib.xListLoopCar[0].iTC_Class}";
                        */
                    }
                    LogInfo.WriteLaneEvent("+2 Convoy Mode : " + TCT_Lib.xListLoopCar[0].szTransRemark);
                }

                //--------------------------------------------------------------------
                if (bIsConvoy)
                {
                    int iConvoyClass = TCT_Lib.xListLoopCar.Count == 0 ? TCT_Lib.iCurTC_Class : TCT_Lib.xListLoopCar[0].iTC_Class;
                    LogInfo.WriteLog("+ Convoy Class : " + iConvoyClass);
                    if (iConvoyClass == 1)
                        TCT_Lib.iConvoyClass1 = TCT_Lib.iConvoyClass1 - 1;
                    else if (iConvoyClass == 2)
                        TCT_Lib.iConvoyClass2 = TCT_Lib.iConvoyClass2 - 1;

                    LogInfo.WriteLaneEvent("- Now Convoy : Class 1=" + TCT_Lib.iConvoyClass1 + ", Class2=" + TCT_Lib.iConvoyClass2);
                }
                else
                {
                    LogInfo.WriteLaneEvent("- Not Convoy : รถแทรก รถขบวนจ่ายเงิน");
                }
            }
            updateUI_Convoy(TCT_Lib.xListLoopCar.Count == 0 ? TCT_Lib.iCurTC_Class : TCT_Lib.xListLoopCar[0].iTC_Class);
            //---------------------------------------------------------------------------------------------------------
            if (bTowingAVC && (TCT_Lib.eTowingState != TowingState.None))
            {
                //--- Towing Mode --- Manual Close ALB
                //LogInfo.WriteLog("- ClearUI from AVC - Towing");
                clearPanelUiLaneControl();
                if (TCT_Lib.iCurTC_Class == 0)
                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Welcome, (int)TFI_Pattern.Normal, "", "", "", szTFI_StartLane1, szTFI_StartLane2);
                //TCT_Lib.webSocketLane.OutIo_WriteCRS("", "");
                if (iTimerClearCRS == 0)
                    iTimerClearCRS = GLib.Timeout.Add((uint)(iSecClearCRS * 1000), new GLib.TimeoutHandler(ClearTextCRS));

                if (TCT_Lib.eCurLaneMode != LaneMode.HPMC)
                    SendDigitalOutput_CmdALB_Close();
                SendEtcRabbitMq_CmdALB_Normal("End Towing");

                //--- Check MIS Event
                if (TCT_Lib.iTowingTC_Class != TCT_Lib.xTowingAVC.avcClass)
                {
                    TCT_Lib.bClassDisFlag = true;
                    if (TCT_Lib.xTowingAVC.avcClass != 255)
                    {
                        LogInfo.WriteLaneEvent("IO - ASB : ON");
                        TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ASB, ON);
                        TCT_Lib.LaneDB.UpdateTransStatistic_DIS();

                        eventDIS();
                        uint iSirenTimerId = GLib.Timeout.Add((uint)TCT_Lib.iStopSirenAfterTimeSec * 1000, new GLib.TimeoutHandler(Perform_StopASB));
                        AddSirenTimer(iSirenTimerId);
                    }
                    else
                    {
                        //--- AVC Error
                        //--- Send alarm to RTO
                        int iAlarmID = (int)TCT_Lib.LaneDB.WriteLaneAlarmRTO(AlarmDevice.AVC, AlarmType.Occurred, "AVC not send data", "AVC=255");
                        TCT_Lib.SendLaneAlarmObjToRTO(iAlarmID);

                        //--- Send Lane Transaction to RabbitMQ

                    }
                }
                //------------------------------
                TCT_Lib.LaneDB.UpdateTransStatistic_Transaction();
                TCT_Lib.LaneDB.UpdateTransStatistic_AVC(TCT_Lib.xTowingAVC.avcClass);
                int iTotalSum = TCT_Lib.LaneDB.UpdateTransStatistic_TotalSum();

                if (TCT_Lib.iTowingTC_Class == (int)VehicleType.EXEMPT)
                    TCT_Lib.LaneDB.UpdateTransStatistic_Exempt();

                //--- Create Lane Transaction
                int iTowingTransID = (int)TCT_Lib.LaneDB.WriteLaneTransaction_Towing(TCT_Lib.iAvcStatusType == 0 ? bDetectLoopBack : bRealPassLoop,
                                                                                        TCT_Lib.iAvcStatusType == 0 ? bFoundOB : bRealPassOB,
                                                                                        bForceEndTrans);
                bTowingAVC      = false;
                //========================================================================================================
                if (TCT_Lib.iVideoNVR_CaptureImageMethod == 3)
                    ProcessCreateImageCameraSendJsonFileThread_Method_3(iTowingTransID);
                if ((TCT_Lib.iVideoNVR_CaptureImageMethod == 1) || (TCT_Lib.iVideoNVR_CaptureImageMethod == 2))
                {
                    if (iTowingTransID > 0)
                    {
                        int _iJobNo = 0, _nTrx = 0;
                        var xImageCameraObj         = TCT_Obj.GenerateCaptureImageCamera(iTowingTransID, ref _iJobNo, ref _nTrx);
                        string szImageCameraJson    = JsonConvert.SerializeObject(xImageCameraObj, Formatting.Indented);
                        long iCaptureRunningNo      = TCT_Lib.LaneDB.GetCaptureRunningNoOfTrans(_iJobNo, _nTrx, iTowingTransID, DateTime.Now);
                        //using (StreamWriter writer  = File.CreateText(Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, $"ImageCamera_Trans{iTransID}_J{_iJobNo}_N{_nTrx}.json")))
                        string szCaptureJsonFile    = Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, $"ImageCamera_Trans{iTowingTransID}_N{iCaptureRunningNo.ToString("000000")}.json");
                        try
                        {
                            if (TCT_Lib.szVideoLane_CaptureImagePath != "")
                            {
                                if (!Directory.Exists(TCT_Lib.szVideoLane_CaptureImagePath))
                                {
                                    LogInfo.WriteLog("+ Auto create capture image path : " + TCT_Lib.szVideoLane_CaptureImagePath);
                                    Directory.CreateDirectory(TCT_Lib.szVideoLane_CaptureImagePath);
                                }
                            }

                            using (StreamWriter writer = File.CreateText(szCaptureJsonFile))
                            {
                                writer.WriteLine(szImageCameraJson);
                            }
                            //--------------------------------
                            //TCT_NVR.MoveFolderCaptureImageTrans(iTowingTransID, iCaptureRunningNo, xImageCameraObj.captureImages);
                            //--------------------------------
                            var sendImgCamera = new WebService_ImageCamera();
                            sendImgCamera.CallWebService_SendImageCaptureCamera(iTowingTransID, _iJobNo, _nTrx, szImageCameraJson, iCaptureRunningNo, xImageCameraObj.captureImages);
                        }
                        catch (Exception err)
                        {
                            LogInfo.WriteLog($"- Error create capture image path={TCT_Lib.szVideoLane_CaptureImagePath}, Error={err.Message}");
                            LogInfo.WriteLaneEvent($"- Error create capture image path={TCT_Lib.szVideoLane_CaptureImagePath}, Error={err.Message}");
                            LogInfo.WriteLogVideoCapture($"- Error create capture image path={TCT_Lib.szVideoLane_CaptureImagePath}, Error={err.Message}");
                        }
                    }
                    else
                    {
                        LogInfo.WriteLogVideoCapture($"- Error Not found LaneTransId");
                    }
                }
                //========================================================================================================

                //TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.PASSAGE_LOOP, bDetectLoopBack ? 1 : 0);
                //TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.PASSAE_OB, bDetectObBack ? 1 : 0);
                //TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.AVC, bAutoAVC ? 0 : 1);

                LogInfo.WriteLog($"++ TotalSum Vehicle : {iTotalSum}, LastTrans (Towing) = {iTowingTransID}, nTrx={TCT_Lib.nTrx - 1}");
                //--- Send Lane Transaction to RabbitMQ
                if (iTowingTransID != 0)
                {
                    TCT_Lib.SendLaneTransObjToDC(iTowingTransID);
                    TCT_Lib.SendLaneTransObjToRTO(iTowingTransID);
                    TCT_Lib.SendLaneTransObjToSupAdjust(iTowingTransID);
                    //if (TCT_Lib.bEnableMTS)
                    //    TCT_Lib.SendLaneTransObjToMTS(iTowingTransID);
                }

                TCT_Lib.ClearCurTransData();

                //----------------------------------------------------------------
                //--- Add 2023-04-10
                if (bAutoAVC)
                {
                    TCT_Lib.xCurEMV = new EMV_Info();
                    TCT_Lib.xCurEDC = new EDC_Info();
                }
                //----------------------------------------------------------------

                TCT_Lib.iPrevTC_Class       = 0;
                TCT_Lib.iPrevPayType        = 0;
                TCT_Lib.fPrevFareAmount     = 0;
                TCT_Lib.szPrevTransId       = "";
                TCT_Lib.szCurTransId        = "";

                //TCT_Lib.bRePaymentFlag      = false;
                TCT_Lib.bReClassifiedFlag   = false;
                TCT_Lib.bReversingFlag      = false;
                TCT_Lib.bClassDisFlag       = false;
                TCT_Lib.bSimuPassFlag       = false;

                szPrevPAN                   = "";
                bEditModeETC                = false;
                bDetectLoopBack             = false;
                bDetectObBack               = false;
                bFoundOB                    = false;
                bFoundLoop                  = false;
                showModeType(TCT_Lib.bAllowETC ? "ETC" : "MTC");
                show_nTrxControl(TCT_Lib.nTrx);
            }
            else
            {
                if (TCT_Lib.eCurLaneType == LaneType.ETC)
                {
                    if (bHasViolation)
                        bHasViolation = false;
                }
                //--- Normal Mode and First Towing
                if ((TCT_Lib.xListLoopCar.Count >= 0) || (bHasViolation) || (bForceEndTrans) || (bDetectObBack))
                {
                    int iTC_Class       = TCT_Lib.xListLoopCar.Count == 0 ? TCT_Lib.iCurTC_Class : TCT_Lib.xListLoopCar[0].iTC_Class;
                    Pay_Info xPayment   = TCT_Lib.xListLoopCar.Count == 0 ? TCT_Lib.xCurPayment : TCT_Lib.xListLoopCar[0].xPayment;

                    if (TCT_Lib.xCurAVC.avcClass > 0)
                    {
                        if (TCT_Lib.eCurLaneMode != LaneMode.HPMC)
                        {
                            //--- Check MIS Event
                            if (iTC_Class != TCT_Lib.xCurAVC.avcClass)
                            {
                                if ((iTC_Class == (int)VehicleType.EXEMPT) || (iTC_Class == (int)VehicleType.LEFT_EXIT))
                                    TCT_Lib.bClassDisFlag = false;
                                else
                                    TCT_Lib.bClassDisFlag = true;
                                //-------------------------------------------------------------
                                if (TCT_Lib.xCurAVC.avcClass != 255)
                                {
                                    TCT_Lib.LaneDB.UpdateTransStatistic_DIS();
                                    LogInfo.WriteLaneEvent("IO - ASB : ON");
                                    TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ASB, ON);
                                    eventDIS();
                                    uint iSirenTimerId = GLib.Timeout.Add((uint)TCT_Lib.iStopSirenAfterTimeSec * 1000, new GLib.TimeoutHandler(Perform_StopASB));
                                    AddSirenTimer(iSirenTimerId);
                                }
                                else
                                {
                                    //--- AVC Error
                                    //--- Send alarm to RTO
                                    int iAlarmID = (int)TCT_Lib.LaneDB.WriteLaneAlarmRTO(AlarmDevice.AVC, AlarmType.Occurred, "AVC not send data", "AVC=255");
                                    TCT_Lib.SendLaneAlarmObjToRTO(iAlarmID);

                                    //--- Send Lane Transaction to RabbitMQ

                                }
                            }
                        }
                        else
                        {
                            //-- HPMC Mode
                            TCT_Lib.LaneDB.UpdateTransStatistic_HPMC();
                        }


                        //=============================================================================================
                        //--- Abnormal Mode of IO
                        bool bClosed = false;
                        if ((!bFoundLoop && bFoundOB && (TCT_Lib.iIsTowing == 0)) ||
                            (!bFoundOB && !bTrigDownOB && bDetectObBack && (TCT_Lib.iIsTowing == 0)) ||
                            (!bFoundOB && !bTrigDownOB && !bDetectObBack && (TCT_Lib.iIsTowing == 0)))      // Only Loop On => No OB
                        {
                            iStepNextCar = 0;
                            if (!bTrigDownLoop && !bDetectLoopBack && (TCT_Lib.iCurTC_Class != 0) && (TCT_Lib.xCurPayment.payType != 0 && TCT_Lib.xCurPayment.payType != 9))
                            {
                                Process_RefreshPresenceLoop();
                            }


                            if ((!bFoundLoop && bFoundOB && !bHasViolation && bCurPassageLoop || !bTrigDownLoop && (TCT_Lib.iIsTowing == 0)) ||
                                (!bFoundOB && !bTrigDownOB && bDetectObBack && (TCT_Lib.iIsTowing == 0)) ||
                                (!bFoundOB && !bTrigDownOB && !bDetectObBack && (TCT_Lib.iIsTowing == 0)))
                            {
                                if ((TCT_Lib.eCurLaneMode != LaneMode.HPMC) && (!TCT_Lib.bAllowETC))
                                {
                                    //--- When Found only OB (Loop Always ON), Force Close ALB
                                    SendDigitalOutput_CmdALB_Close();
                                    bClosed = true;
                                    //if ((TCT_Lib.iCurTC_Class > 0) && (TCT_Lib.xListLoopCar.Count == 0))
                                    {
                                        //TCT_Lib.ClearCurTransData();
                                        //clearPanelUiLaneControl();
                                    }
                                    if (TCT_Lib.iCurTC_Class == 0)
                                    {
                                        if (TCT_Lib.eCurLaneMode == LaneMode.CLOSED)
                                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.CloseLane, (int)TFI_Pattern.Normal, "", "", "", szTFI_CloseLane, "");
                                        else
                                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Welcome, (int)TFI_Pattern.Normal, "", "", "", szTFI_StartLane1, szTFI_StartLane2);
                                    }
                                    //TCT_Lib.webSocketLane.OutIo_WriteCRS("", "");
                                    if (iTimerClearCRS == 0)
                                        iTimerClearCRS = GLib.Timeout.Add((uint)((iSecClearCRS - 2) * 1000), new GLib.TimeoutHandler(ClearTextCRS));
                                }
                            }
                        }

                        //if (!bFoundLoop && bTrigDownLoop)
                        //    Process_RefreshPresenceLoop();

                        //if (!bDetectLoopBack && bDetectObBack)
                        if (!bFoundLoop && bFoundOB && !bHasViolation && !bCurPassageLoop && (TCT_Lib.iIsTowing == 0))
                        {
                            if ((TCT_Lib.eCurLaneMode != LaneMode.HPMC) && (!TCT_Lib.bAllowETC))
                            {
                                //--- When Found only OB (No Loop), Force Close ALB
                                //Process_RefreshPresenceLoop();
                                if (!bClosed)
                                    SendDigitalOutput_CmdALB_Close();
                                if ((TCT_Lib.iCurTC_Class > 0) && (TCT_Lib.xListLoopCar.Count == 0))
                                {
                                    TCT_Lib.ClearCurTransData();
                                    clearPanelUiLaneControl();
                                }
                                if (TCT_Lib.iCurTC_Class == 0)
                                {
                                    if (TCT_Lib.eCurLaneMode == LaneMode.CLOSED)
                                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.CloseLane, (int)TFI_Pattern.Normal, "", "", "", szTFI_CloseLane, "");
                                    else
                                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Welcome, (int)TFI_Pattern.Normal, "", "", "", szTFI_StartLane1, szTFI_StartLane2);
                                }
                                //TCT_Lib.webSocketLane.OutIo_WriteCRS("", "");
                                if (iTimerClearCRS == 0)
                                    iTimerClearCRS = GLib.Timeout.Add((uint)((iSecClearCRS - 2) * 1000), new GLib.TimeoutHandler(ClearTextCRS));
                            }
                        }

                        //--- Loop On of second car (Always loop)
                        if (bFoundLoop && bFoundOB && bTrigDownOB && (TCT_Lib.iIsTowing == 0))
                        {
                            if ((TCT_Lib.eCurLaneMode != LaneMode.HPMC) && (!TCT_Lib.bAllowETC))
                            {
                                //--- When Found only OB (No Loop), Force Close ALB
                                //Process_RefreshPresenceLoop();
                                SendDigitalOutput_CmdALB_Close();
                                if ((TCT_Lib.iCurTC_Class > 0) && (TCT_Lib.xListLoopCar.Count == 0))
                                {
                                    TCT_Lib.ClearCurTransData();
                                    clearPanelUiLaneControl();
                                }
                                if (TCT_Lib.iCurTC_Class == 0)
                                {
                                    if (TCT_Lib.eCurLaneMode == LaneMode.CLOSED)
                                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.CloseLane, (int)TFI_Pattern.Normal, "", "", "", szTFI_CloseLane, "");
                                    else
                                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Welcome, (int)TFI_Pattern.Normal, "", "", "", szTFI_StartLane1, szTFI_StartLane2);
                                }
                                //TCT_Lib.webSocketLane.OutIo_WriteCRS("", "");
                                if (iTimerClearCRS == 0)
                                    iTimerClearCRS = GLib.Timeout.Add((uint)((iSecClearCRS - 2) * 1000), new GLib.TimeoutHandler(ClearTextCRS));
                            }
                        }

                        /*
                                                //--- Found only Loop and not found OB
                                                if (bDetectLoopBack && !bDetectObBack)
                                                {
                                                    if (TCT_Lib.eCurLaneMode != LaneMode.HPMC)
                                                    {
                                                        //--- When Found only Loppp (No OB), Force Close ALB
                                                        SendDigitalOutput_CmdALB_Close();
                                                        if ((TCT_Lib.iCurTC_Class > 0) && (TCT_Lib.xListLoopCar.Count == 0))
                                                        {
                                                            TCT_Lib.ClearCurTransData();
                                                            clearPanelUiLaneControl();
                                                        }
                                                        if (TCT_Lib.iCurTC_Class == 0)
                                                        {
                                                            if (TCT_Lib.eCurLaneMode == LaneMode.CLOSED)
                                                                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.CloseLane, (int)TFI_Pattern.Normal, "", "", "", szTFI_CloseLane, "");
                                                            else
                                                                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Welcome, (int)TFI_Pattern.Normal, "", "", "", szTFI_StartLane1, szTFI_StartLane2);
                                                        }
                                                        //TCT_Lib.webSocketLane.OutIo_WriteCRS("", "");
                                                        if (iTimerClearCRS == 0)
                                                            iTimerClearCRS = GLib.Timeout.Add((uint)((iSecClearCRS - 2) * 1000), new GLib.TimeoutHandler(ClearTextCRS));
                                                    }
                                                }
                        */
                        /*
                                                if ((!bFoundLoop && bFoundOB && !bHasViolation && bCurPassageLoop || !bTrigDownLoop)
                                                    && ((TCT_Lib.iCurTC_Class != 0) && (TCT_Lib.xCurPayment.payType != 0 && TCT_Lib.xCurPayment.payType != 9)))
                                                {
                                                    if (TCT_Lib.eCurLaneMode != LaneMode.HPMC)
                                                    {
                                                        //--- When Found only OB (Loop Always ON), Force Close ALB
                                                        SendDigitalOutput_CmdALB_Close();
                                                        if ((TCT_Lib.iCurTC_Class > 0) && (TCT_Lib.xListLoopCar.Count == 0))
                                                        {
                                                            TCT_Lib.ClearCurTransData();
                                                            clearPanelUiLaneControl();
                                                        }
                                                    }
                                                }
                        */

                        //------------------------------
                        showTotalLoop();

                        if (bTowingAVC)
                            bTowingAVC = false;

                        if ((iTC_Class != 0) ||      // Normal Case
                            ((iTC_Class == 0) && (TCT_Lib.xCurAVC.avcClass != 255) && !bAutoAVC) ||    // Violation with class
                            ((iTC_Class == 0) && (TCT_Lib.xCurAVC.avcClass == 255) && bAutoAVC))       // Auto AVC
                        {
                            //----------------------------------------------------------------------------------
                            if (bHasViolation)
                            {
                                if ((TCT_Lib.iVideoNVR_CaptureImageMethod == 0) || (TCT_Lib.iVideoNVR_CaptureImageMethod == 1))
                                {
                                    if (iCapture_nTrx < TCT_Lib.nTrx)
                                        CreateProcessVideoCaptureImage(TCT_Lib.nTrx, false, true);
                                }
                            }
                            //----------------------------------------------------------------------------------


                            //--- Protect create duplicate transaction
                            //--- 2020-11-11 Check last transaction time and current time must more than 1 sec
                            TimeSpan dtTimeSpan = DateTime.Now.Subtract(dtLastTrans);
                            //if (dtTimeSpan.Seconds >= 1)     //--- Check each transaction will more 1 second
                            {
                                //--- Create Lane Transaction
                                //--- 2020-10-28
                                //--- Output to DC (Change some TC_Class=0, in case HPMC)
                                if (iTC_Class == (int)VehicleType.HPMC)
                                {
                                    var hpmcLoopCar = TCT_Lib.LoopCarFirst();   // v2.0.0: null-safe
                                    if (hpmcLoopCar != null)
                                        hpmcLoopCar.iTC_Class = 0;
                                }

                                int iTransID = 0;
                                if ((TCT_Lib.eCurLaneType == LaneType.DUAL) && (TCT_Lib.eCurLaneMode == LaneMode.HPMC))
                                    iTransID = 0;
                                else
                                    iTransID = (int)TCT_Lib.LaneDB.WriteLaneTransaction(TCT_Lib.iAvcStatusType == 0 ? bDetectLoopBack : bRealPassLoop,
                                                                                        TCT_Lib.iAvcStatusType == 0 ? bFoundOB : bRealPassOB,
                                                                                        bForceEndTrans);
                                dtLastTrans     = DateTime.Now;
                                //========================================================================================================
                                if (TCT_Lib.iVideoNVR_CaptureImageMethod == 3)
                                    ProcessCreateImageCameraSendJsonFileThread_Method_3(iTransID);
                                if ((TCT_Lib.iVideoNVR_CaptureImageMethod == 1) || (TCT_Lib.iVideoNVR_CaptureImageMethod == 2))
                                {
                                    if (iTransID > 0)
                                    {
                                        int _iJobNo = 0, _nTrx = 0;
                                        var xImageCameraObj         = TCT_Obj.GenerateCaptureImageCamera(iTransID, ref _iJobNo, ref _nTrx);
                                        string szImageCameraJson    = JsonConvert.SerializeObject(xImageCameraObj, Formatting.Indented);
                                        long iCaptureRunningNo      = TCT_Lib.LaneDB.GetCaptureRunningNoOfTrans(_iJobNo, _nTrx, iTransID, DateTime.Now);
                                        //using (StreamWriter writer  = File.CreateText(Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, $"ImageCamera_Trans{iTransID}_J{_iJobNo}_N{_nTrx}.json")))
                                        string szCaptureJsonFile    = Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, $"ImageCamera_Trans{iTransID}_N{iCaptureRunningNo.ToString("000000")}.json");

                                        try
                                        {
                                            if (TCT_Lib.szVideoLane_CaptureImagePath != "")
                                            {
                                                if (!Directory.Exists(TCT_Lib.szVideoLane_CaptureImagePath))
                                                {
                                                    LogInfo.WriteLog("+ Auto create capture image path : " + TCT_Lib.szVideoLane_CaptureImagePath);
                                                    Directory.CreateDirectory(TCT_Lib.szVideoLane_CaptureImagePath);
                                                }
                                            }

                                            using (StreamWriter writer = File.CreateText(szCaptureJsonFile))
                                            {
                                                writer.WriteLine(szImageCameraJson);
                                            }
                                            //--------------------------------
                                            //TCT_NVR.MoveFolderCaptureImageTrans(iTransID, iCaptureRunningNo, xImageCameraObj.captureImages);
                                            //--------------------------------
                                            var sendImgCamera = new WebService_ImageCamera();
                                            sendImgCamera.CallWebService_SendImageCaptureCamera(iTransID, _iJobNo, _nTrx, szImageCameraJson, iCaptureRunningNo, xImageCameraObj.captureImages);
                                        }
                                        catch (Exception err)
                                        {
                                            LogInfo.WriteLog($"- Error create capture image path={TCT_Lib.szVideoLane_CaptureImagePath}, Error={err.Message}");
                                            LogInfo.WriteLaneEvent($"- Error create capture image path={TCT_Lib.szVideoLane_CaptureImagePath}, Error={err.Message}");
                                            LogInfo.WriteLogVideoCapture($"- Error create capture image path={TCT_Lib.szVideoLane_CaptureImagePath}, Error={err.Message}");
                                        }
                                    }
                                    else
                                    {
                                        if ((TCT_Lib.eCurLaneType != LaneType.DUAL) || (TCT_Lib.eCurLaneMode != LaneMode.HPMC))
                                            LogInfo.WriteLogVideoCapture($"- Error Not found LaneTransId");
                                    }
                                }
                                //========================================================================================================

                                //TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.PASSAGE_LOOP, bDetectLoopBack ? 1 : 0);
                                //TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.PASSAE_OB, bDetectObBack ? 1 : 0);
                                //TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.AVC, bAutoAVC ? 0 : 1);

                                if (iTransID != 0)
                                {
                                    TCT_Lib.LaneDB.UpdateTransStatistic_Transaction();
                                    TCT_Lib.LaneDB.UpdateTransStatistic_AVC(TCT_Lib.xCurAVC.avcClass);
                                    int iTotalSum = TCT_Lib.LaneDB.UpdateTransStatistic_TotalSum();
                                    LogInfo.WriteLog($"++ TotalSum Vehicle : {iTotalSum}, LastTrans={iTransID}, nTrx={TCT_Lib.nTrx - 1}");

                                    if (iTC_Class == (int)VehicleType.EXEMPT)
                                        TCT_Lib.LaneDB.UpdateTransStatistic_Exempt();
                                }

                                //if (TCT_Lib.eTowingState != TowingState.None)
                                if ((TCT_Lib.eTowingState != TowingState.None) &&
                                (((TCT_Lib.iCurTC_Class == 0) && (TCT_Lib.xListLoopCar.Count == 1))))
                                {
                                    TCT_Lib.iTowingParentID     = iTransID;
                                    bTowingAVC                  = true;
                                    updateUI_TowingFirstPass();
                                }
                                else
                                {
                                    if ((TCT_Lib.iCurTC_Class == (int)VehicleType.TOWING) || (TCT_Lib.eTowingState != TowingState.None))
                                    {
                                        if (!bForceEndTrans)
                                        {
                                            iQueueALB--;
                                            if (iQueueALB < 0)
                                                iQueueALB = 0;
                                            //TCT_Lib.iIsTowing = 0;
                                            TCT_Lib.LaneDB.UpdateClearTowing(iTransID);
                                            if (!TCT_Lib.bAllowETC)
                                            {
                                                if (!bClosed && (iQueueALB == 0))
                                                    SendDigitalOutput_CmdALB_Close();
                                            }
                                        }
                                        else
                                        {
                                            TCT_Lib.iTowingParentID = iTransID;
                                        }
                                    }
                                    else
                                    {
                                        if (TCT_Lib.xListLoopCar.Count > 0)
                                        {
                                            if (!TCT_Lib.bAllowETC)
                                                TCT_Lib.LaneDB.UpdateClearReClass(iTransID);
                                        }
                                    }

                                    if (TCT_Lib.bAllowETC)
                                    {
                                        //TCT_Lib.xListLoopCar.Clear();
                                        //TCT_Lib.xListLoopCar.RemoveAt(0);
                                        //showTotalLoop();
                                        if (TCT_Lib.eCurLaneType == LaneType.ETC)
                                        {
                                            if (TCT_Lib.xCurTAG.tagPAN != null)
                                            {
                                                TCT_Lib.ClearCurTransData();
                                                if (!bHasViolation)
                                                {
                                                    //LogInfo.WriteLog("- ClearUI from ETC-1");
                                                    clearPanelUiLaneControl();
                                                }
                                            }
                                            else
                                            {
                                                if (TCT_Lib.iCurTC_Class == 0)
                                                {
                                                    TCT_Lib.ClearCurTransData();
                                                    if (!bHasViolation)
                                                    {
                                                        //LogInfo.WriteLog("- ClearUI from ETC-2");
                                                        clearPanelUiLaneControl();
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (TCT_Lib.eCurLaneMode != LaneMode.HPMC)
                                            {
                                                if (TCT_Lib.xCurTAG.tagPAN != "")
                                                {
                                                    TCT_Lib.ClearCurTransData();
                                                    if (!bHasViolation)
                                                    {
                                                        //LogInfo.WriteLog("- ClearUI from ETC-3");
                                                        clearPanelUiLaneControl();
                                                    }
                                                }
                                                else
                                                {
                                                    if (TCT_Lib.iCurTC_Class == 0)
                                                    {
                                                        TCT_Lib.ClearCurTransData();
                                                        if (!bHasViolation)
                                                        {
                                                            //LogInfo.WriteLog("- ClearUI from ETC-4");
                                                            clearPanelUiLaneControl();
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        showTotalLoop();
                                    }

                                    if ((bForceEndTrans) && (TCT_Lib.eTowingState == TowingState.None))
                                    {
                                        TCT_Lib.ClearCurTransData();
                                        if (!bHasViolation)
                                        {
                                            //LogInfo.WriteLog("- ClearUI from AVC-ForceEnd");
                                            clearPanelUiLaneControl();
                                        }
                                        //iStepNextCar = 0;
                                        //TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.LTL, OFF);
                                        //--- When Found only OB (No Loop), Force Close ALB
                                        //SendDigitalOutput_CmdALB_Close();
                                    }
                                }

                                //--- Send Lane Transaction to RabbitMQ
                                if (TCT_Lib.iVideoNVR_CaptureImageMethod != 3)
                                {
                                    if (iTransID != 0)
                                    {
                                        TCT_Lib.SendLaneTransObjToRTO(iTransID);
                                        if (TCT_Lib.xCurTAG.tagPAN == null)
                                        {
                                            TCT_Lib.SendLaneTransObjToDC(iTransID);
                                            TCT_Lib.SendLaneTransObjToSupAdjust(iTransID);
                                            //if (TCT_Lib.bEnableMTS)
                                            //    TCT_Lib.SendLaneTransObjToMTS(iTransID);
                                        }
                                    }
                                }

                            }
                            /*
                                                        else
                                                        {
                                                            LogInfo.WriteLaneEvent("- Fail create transaction violation 2. : " + dtTimeSpan.Milliseconds);
                                                        }
                            */
                        }

                    }
                }
                /*
                                //--- Vio
                                if ((TCT_Lib.xListLoopCar.Count == 0) || (bHasViolation) || (bDetectObBack))
                                {
                                    //--- Loop On of second car
                                    if (bFoundOB)
                                    {
                                        if (TCT_Lib.eCurLaneMode != LaneMode.HPMC)
                                        {
                                            if (TCT_Lib.eCurLaneMode != LaneMode.HPMC)
                                            {
                                                //--- When Found only OB (No Loop), Force Close ALB
                                                //Process_RefreshPresenceLoop();
                                                SendDigitalOutput_CmdALB_Close();
                                                if ((TCT_Lib.iCurTC_Class > 0) && (TCT_Lib.xListLoopCar.Count == 0))
                                                {
                                                    TCT_Lib.ClearCurTransData();
                                                    clearPanelUiLaneControl();
                                                }
                                                if (TCT_Lib.iCurTC_Class == 0)
                                                {
                                                    if (TCT_Lib.eCurLaneMode == LaneMode.CLOSED)
                                                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.CloseLane, (int)TFI_Pattern.Normal, "", "", "", szTFI_CloseLane, "");
                                                    else
                                                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Welcome, (int)TFI_Pattern.Normal, "", "", "", szTFI_StartLane1, szTFI_StartLane2);
                                                }
                                                //TCT_Lib.webSocketLane.OutIo_WriteCRS("", "");
                                                if (iTimerClearCRS == 0)
                                                    iTimerClearCRS = GLib.Timeout.Add((uint)((iSecClearCRS - 2) * 1000), new GLib.TimeoutHandler(ClearTextCRS));
                                            }
                                        }
                                    }
                                }
                */
            }

            show_nTrxControl(TCT_Lib.nTrx);
            if (TCT_Lib.xListLoopCar.Count > 0)
            {
                if (TCT_Lib.eCurLaneType == LaneType.ETC)
                    TCT_Lib.xListLoopCar.Clear();
                else
                    TCT_Lib.xListLoopCar.RemoveAt(0);
            }

            showTotalLoop();

            if (TCT_Lib.bStatusOpenALB_RTO)
            {
                if (TCT_Lib.eCurLaneMode == LaneMode.HPMC)
                {
                    LogInfo.WriteLaneEvent("+ Ignore Auto Close ALB when Open ALB from RTO because now in HPMC mode");
                }
                else
                {
                    LogInfo.WriteLaneEvent("+ Auto Close ALB when Open ALB from RTO");
                    if (TCT_Lib.eCurLaneType == LaneType.MTC)
                        SendDigitalOutput_CmdALB_Close();
                    else if ((TCT_Lib.eCurLaneType == LaneType.DUAL) || (TCT_Lib.eCurLaneType == LaneType.ETC))
                        SendEtcRabbitMq_CmdALB_Close(true);
                }

                TCT_Lib.bStatusOpenALB_RTO = false;
            }

            //----------------------------------------------------------------
            //--- Add 2023-04-10
            if (bAutoAVC)
            {
                TCT_Lib.xCurEMV = new EMV_Info();
                TCT_Lib.xCurEDC = new EDC_Info();
            }
            //----------------------------------------------------------------

            //updateUI_Convoy();
            if (!bTowingAVC)
                TCT_Lib.bRePaymentFlag  = false;
            TCT_Lib.bReClassifiedFlag   = false;
            TCT_Lib.bReversingFlag      = false;
            TCT_Lib.bClassDisFlag       = false;
            TCT_Lib.bSimuPassFlag       = false;
            bReClassETC                 = false;
            bEditBlockETC               = false;

            bFoundLoopOB                = false;
            bDetectLoopBack             = false;
            bDetectObBack               = false;
            bFoundOB                    = false;
            bFoundLoop                  = false;
            bTrigDownLoop               = false;
            bTrigDownOB                 = false;
            //--- Send Lane Statisitc
            SendLaneCurrentTransStatistic();
            bAutoAVC                    = false;
            //if (bHasViolation)
            //    bHasViolation = false;

            bBlockTAG       = false;
            bEditDual       = false;

            bCurPassageOB   = false;
        }

        protected void Process_RefreshPresenceLoop(bool bPaid = true)
        {
            if (!bTowingAVC)
            {
                if (!bDetectObBack)
                {
                    //--- If car touch loop (Not OB), and no class type of payment, then TCT will wait, no hide
                    if ((TCT_Lib.iCurTC_Class == 0) || (TCT_Lib.xCurPayment.payType == 0))
                    {
                        bLoopMis = true;
                        return;
                    }
                }
                bLoopMis = false;
            }
            //-----------------------------------------------------------------

            //TCT_Lib.xListLoopCar.Clear();
            //showTotalLoop();

            //--- Input in Loop Car List
            var xLoopCar = new objLoopInfo();

            if (TCT_Lib.eCurLaneMode == LaneMode.HPMC)
            {
                xLoopCar.iTC_Class              = 9;        // => HPMC Show '0'
                xLoopCar.fFareAmount            = TCT_Lib.fCurFareAmount;
                xLoopCar.xPayment               = TCT_Lib.xCurPayment;
                xLoopCar.xPayment.payAmount     = TCT_Lib.fCurFareAmount;
                xLoopCar.xPayment.payType       = (int)PayType.HPMC;
                xLoopCar.xPayment.payTypeName   = "HPMC";
                xLoopCar.xPayment.payDateTime   = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                xLoopCar.xPayment.payRemark     = "HPMC";
                xLoopCar.szTransRemark              = szVehicle_HPMC;
                TCT_Lib.xListLoopCar.Add(xLoopCar);
                showTotalLoop();
                return;
            }

            if (TCT_Lib.eTowingState == TowingState.None)
            {
                //--- Normal Mode
                xLoopCar.iTC_Class          = TCT_Lib.iCurTC_Class;
                xLoopCar.fFareAmount        = TCT_Lib.fCurFareAmount;
                xLoopCar.xPayment           = TCT_Lib.xCurPayment;
                xLoopCar.szTransRemark      = TCT_Lib.szCurTransRemark;
                TCT_Lib.xListLoopCar.Add(xLoopCar);
            }
            else
            {
                //--- Towing Mode
                if (!bTowingAVC)
                {
                    xLoopCar.iTC_Class      = TCT_Lib.iCurTC_Class;
                    xLoopCar.fFareAmount    = TCT_Lib.fCurFareAmount;
                    xLoopCar.xPayment       = TCT_Lib.xCurPayment;
                    xLoopCar.szTransRemark  = TCT_Lib.szCurTransRemark;
                    TCT_Lib.xListLoopCar.Add(xLoopCar);

                    if (TCT_Lib.xTowingPayment.payType == (int)PayType.CASH)
                    {
                        WriteTextCRS(TCT_Lib.iTowingTC_Class.ToString(), GetStrPayTypeCRS(PayType.CASH));
                        if (TCT_Lib.iTowingTC_Class == (int)VehicleType.CLASS_1)
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, TCT_Lib.iTowingTC_Class.ToString(), TCT_Lib.fFareClass1.ToString("0"), "", szPay_Cash, szAmount_Class1);
                        else
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, TCT_Lib.iTowingTC_Class.ToString(), TCT_Lib.fFareClass2.ToString("0"), "", szPay_Cash, szAmount_Class2);
                    }
                    else if (TCT_Lib.xTowingPayment.payType == (int)PayType.COUPON)
                    {
                        WriteTextCRS(TCT_Lib.iTowingTC_Class.ToString(), GetStrPayTypeCRS(PayType.COUPON));
                        if (TCT_Lib.iTowingTC_Class == (int)VehicleType.CLASS_1)
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, TCT_Lib.iTowingTC_Class.ToString(), TCT_Lib.fFareClass1.ToString("0"), "", szPay_Coupon, szAmount_Class1);
                        else
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, TCT_Lib.iTowingTC_Class.ToString(), TCT_Lib.fFareClass2.ToString("0"), "", szPay_Coupon, szAmount_Class2);
                    }
                    else if (TCT_Lib.xTowingPayment.payType == (int)PayType.DMT_CARD)
                    {
                        TCT_Lib.fTowingFareAmount = 0;
                        WriteTextCRS(TCT_Lib.iTowingTC_Class.ToString(), GetStrPayTypeCRS(PayType.DMT_CARD));

                        if (TCT_Lib.iTowingTC_Class == (int)VehicleType.CLASS_1)
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, TCT_Lib.iTowingTC_Class.ToString(), "0", "", szPay_DmtCard, szAmount_0);
                        else
                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, TCT_Lib.iTowingTC_Class.ToString(), "0", "", szPay_DmtCard, szAmount_0);
                    }
                }
                else
                {
                    var xTowingCar              = new objLoopInfo();
                    xTowingCar.iTC_Class        = TCT_Lib.iTowingTC_Class;
                    xTowingCar.fFareAmount      = TCT_Lib.fTowingFareAmount;
                    xTowingCar.xPayment         = TCT_Lib.xTowingPayment;
                    xTowingCar.szTransRemark    = "TOWING";
                    TCT_Lib.xListLoopCar.Add(xTowingCar);
                }
            }

            showTotalLoop();

            if (bPaid)
            {
                TCT_Lib.iCurTC_Class        = 0;
                TCT_Lib.xCurPayment         = new Pay_Info();
                TCT_Lib.szCurTransRemark    = "";
                TCT_Lib.fCurFareAmount      = 0;

                //-----------------------------------------------------------------------------
                if (TCT_Lib.eCurLaneType == LaneType.DUAL)
                    bEditBlockETC = false;
                //-----------------------------------------------------------------------------

                //-------------------------------------------------------
                if (TCT_Lib.eTowingState == TowingState.None)
                {
                    TCT_Lib.iTotalQueue -= 1;
                    if (TCT_Lib.iTotalQueue < 0)
                        TCT_Lib.iTotalQueue = 0;
                    TCT_Lib.ClearCurTransData();
                    {
                        //LogInfo.WriteLog("- ClearUI from Process_RefreshPresenceLoop");
                        clearPanelUiLaneControl();
                    }
                }   
                iStepNextCar = 0;
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.LTL, OFF);

                //--- Check Next car in current queue and select car already (First Time)
                if (TCT_Lib.iNextTC_Class > 0)
                {
                    //LaneSocket_KeyPressedEvent(KeyBoardType.VK, (TCT_Lib.iNextTC_Class == 1) ? (int)KeyPressedType.CLASS_1 : (int)KeyPressedType.CLASS_2);
                    if (TCT_Lib.iNextTC_Class == (int)VehicleType.CLASS_1)
                        LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.CLASS_1);
                    else if (TCT_Lib.iNextTC_Class == (int)VehicleType.CLASS_2)
                        LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.CLASS_2);
                    else if (TCT_Lib.iNextTC_Class == (int)VehicleType.EXEMPT)
                    {
                        iCapture_nTrx -= 1;   // Capture reverse
                        LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.CLASS_7);
                    }
                    else if (TCT_Lib.iNextTC_Class == (int)VehicleType.LEFT_EXIT)
                    {
                        iCapture_nTrx -= 1;   // Capture reverse
                        LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.CLASS_8);
                    }

                    if ((TCT_Lib.iNextTC_Class == (int)VehicleType.EXEMPT) || (TCT_Lib.iNextTC_Class == (int)VehicleType.LEFT_EXIT))
                        TCT_Lib.bAfterPayment = true;
                    else
                        TCT_Lib.bAfterPayment = false;

                    TCT_Lib.iNextTC_Class = 0;
                }
                //iStepNextCar = 0;

                //TCT_Lib.bAfterPayment = false;
            }
        }

        public void UpdateUI_Connvoy()
        {
            updateUI_Convoy(0);
        }

        protected void LaneSocket_DigitalInputLaneEvent(int[] values, bool bMTC = true)
        {
            try
            {
                //--- Device Digital Input has change value
                bInMLB          = values[TCT_Lib.iLCSD_DI_0]    == 1;
                bInEFS          = values[TCT_Lib.iLCSD_DI_1]    == 1;
                bInPresenceLoop = values[TCT_Lib.iLCSD_DI_2]    == 1;
                bInPassageLoop  = values[TCT_Lib.iLCSD_DI_3]    == 1;
                bInPresenceOB   = values[TCT_Lib.iLCSD_DI_4]    == 1;
                bInPassageOB    = values[TCT_Lib.iLCSD_DI_5]    == 1;
                bInALB          = values[TCT_Lib.iLCSD_DI_6]    == 1;
                bInOTL          = values[TCT_Lib.iLCSD_DI_7]    == 1;
                bInLTL          = values[TCT_Lib.iLCSD_DI_8]    == 1;
                bInEthErr       = values[TCT_Lib.iLCSD_DI_9]    == 1;
                bInSiren        = values[TCT_Lib.iLCSD_DI_10]   == 1;
                bInTreadleError = values[TCT_Lib.iLCSD_DI_13]   == 1;
                bInTreadleC     = values[TCT_Lib.iLCSD_DI_14]   == 1;
                bInTreadleD     = values[TCT_Lib.iLCSD_DI_15]   == 1;
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Error LaneSocket_DigitalInputLaneEvent => Values Error : " + err.Message);
                return;
            }

            if (digitalInputLaneControl != null)
                digitalInputLaneControl(values);

            //-------------------------------------------------
            int iEventID = 0;
            if (bInMLB != bCurMLB)
            {
                LogInfo.WriteLaneEvent("IO - MLB : " + (bInMLB ? "ON" : "OFF"));
                iEventID = (int)TCT_Lib.LaneDB.WriteLaneEventRTO_Device((int)Cmd_DI_Type.MLB, bInMLB ? 1 : 0);
                TCT_Lib.SendLaneEventObjToRTO(iEventID);
                bCurMLB = bInMLB;

                /*
                                if (!bInMLB)
                                {
                                    if (TCT_Lib.bEnableLSD)
                                    {
                                        TCT_Lib.eCurLsdMode = LsdMode.LSD_CLOSE;
                                        if (TCT_Lib.iLSD_KeepAliveSec > 0)
                                        {
                                            if (TCT_Lib.iLSD_MethodType == 0)
                                                TCT_Lib.lsdController.CmdShow_LaneClose();
                                            else
                                                TCT_Lib.webSocketLane.laneSocketServerRTO.SendChangeLsdToRTO(TCT_Lib.eCurLsdMode);
                                        }
                                        else
                                        {
                                            if (TCT_Lib.iLSD_MethodType == 0)
                                            {
                                                var lsdController = new LaneSocketControl_LSD();
                                                lsdController.RunInitialLSD();
                                                //lsdController.CmdShow_LaneClose();
                                                //lsdController.CloseSocketConnection();
                                            }
                                            else
                                                TCT_Lib.webSocketLane.laneSocketServerRTO.SendChangeLsdToRTO(TCT_Lib.eCurLsdMode);
                                        }
                                    }
                                }
                */
            }
            if (bInEFS != bCurEFS)
            {
                LogInfo.WriteLaneEvent("IO - EFS : " + (bInEFS ? "ON" : "OFF") + ", ASB : " + (bInEFS ? "ON" : "OFF"));
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ASB, bInEFS ? ON : OFF);
                iEventID = (int)TCT_Lib.LaneDB.WriteLaneEventRTO_Device((int)Cmd_DI_Type.EFS, bInEFS ? 1 : 0);
                TCT_Lib.SendLaneEventObjToRTO(iEventID);

                if (bInEFS)
                {
                    //--- Send alarm to RTO
                    int iAlarmID = (int)TCT_Lib.LaneDB.WriteLaneAlarmRTO(AlarmDevice.EFS, AlarmType.Occurred, "EFS ON", "EFS ON");
                    TCT_Lib.SendLaneAlarmObjToRTO(iAlarmID);
                    //--- Send Lane Transaction to RabbitMQ
                }

                bCurEFS = bInEFS;
            }
            if (bInPresenceLoop != bCurPresenceLoop)
            {
                LogInfo.WriteLaneEvent("IO - Presence Loop : " + (bInPresenceLoop ? "ON" : "OFF"));
                iEventID = (int)TCT_Lib.LaneDB.WriteLaneEventRTO_Device((int)Cmd_DI_Type.PresenceLoop, bInPresenceLoop ? 1 : 0);
                TCT_Lib.SendLaneEventObjToRTO(iEventID);

                //-----------------------------------------------------------------------------------------
                /*
                                if ((TCT_Lib.eCurLaneType == LaneType.DUAL) || (TCT_Lib.eCurLaneType == LaneType.ETC))
                                {
                                    if (bInPresenceLoop && !bCurPresenceLoop)     // Entry Presence Loop
                                    {
                                        CreateProcessVideoCaptureImage(TCT_Lib.nTrx);
                                    }
                                }
                */
                //-----------------------------------------------------------------------------------------
                bCurPresenceLoop = bInPresenceLoop;
            }
            if (bInPresenceOB != bCurPresenceOB)
            {
                LogInfo.WriteLaneEvent("IO - Presence OB : " + (bInPresenceOB ? "ON" : "OFF"));
                iEventID = (int)TCT_Lib.LaneDB.WriteLaneEventRTO_Device((int)Cmd_DI_Type.PresenceOB, bInPresenceOB ? 1 : 0);
                TCT_Lib.SendLaneEventObjToRTO(iEventID);
                bCurPresenceOB = bInPresenceOB;
            }

            //==============================================================================
            bool bTrigPassageLoop = false;
            if (bInPassageLoop != bCurPassageLoop)
            {
                bTrigPassageLoop = true;
                //bTrigCaptureImage       = true;
                LogInfo.WriteLaneEvent("IO - Passage Loop : " + (bInPassageLoop ? "ON" : "OFF"));
                if (!bCurPassageLoop && bInPassageLoop)     // Loop ON
                {
                    bDetectLoopBack = true;
                    //TCT_Lib.bAfterPayment   = false;    // Unlock key new car

                    if ((TCT_Lib.iCurTC_Class > 0) && (TCT_Lib.xListLoopCar.Count == 0) && (TCT_Lib.xCurPayment.payType > 0))
                    {
                        Process_RefreshPresenceLoop();
                        TCT_Lib.iTotalQueue--;
                        if (TCT_Lib.iTotalQueue < 0)
                            TCT_Lib.iTotalQueue = 0;
                    }
                    bTrigDownLoop = false;

                    //======= For iVideoNVR_CaptureImageMethod by IO
                    if (TCT_Lib.iVideoNVR_CaptureImageMethod == 2)
                    {
                        LogInfo.WriteLaneEvent($"+ Call Capture Image Method 2 : Start Capture Image =>  JobNo={TCT_Lib.iJobNo}, nTrx={TCT_Lib.nTrx}");
                        CreateProcessVideoCaptureImage(TCT_Lib.nTrx);
                    }
                    if (TCT_Lib.iVideoNVR_CaptureImageMethod == 3)
                    {
                        int inTrxIdx = TCT_Lib.nTrx;
                        if ((iCurRunIdx != 3)  && (iCurRunIdx != 0))
                            inTrxIdx += 1;
                        LogInfo.WriteLaneEvent($"+ Call Capture Image Method 3 : Start Capture Image => Index=1, JobNo={TCT_Lib.iJobNo}, nTrx={inTrxIdx}");
                        CreateProcessVideoCaptureImage_Method_3(1, TCT_Lib.nTrx);
                    }
                    //========================================
                }
                if (bCurPassageLoop && !bInPassageLoop)     // Exit Loop
                {
                    if (!bDetectLoopBack)
                    {
                        if ((TCT_Lib.iCurTC_Class > 0) && (TCT_Lib.xListLoopCar.Count == 0))
                        {
                            Process_RefreshPresenceLoop();
                        }
                    }
                    if (bDetectLoopBack)
                        bFoundLoop = true;

                    bTrigDownLoop = true;

                    if (!bDetectObBack)
                        bDetectLoopBack = false;

                    //--------------------------------------------------
                    //TCT_Lib.bMTS_NetworkStatus = true;      // For Testing
                    if (bCurPassageOB && TCT_Lib.bEnableMTS)
                    {
                        if (TCT_Lib.iMTS_Monitor_TimeAlertLoopNoOB > 0)
                        {
                            if (TCT_Lib.iCounterLoopNoOB == 0)
                                dtChkLoopOnOB = DateTime.Now;

                            TCT_Lib.iCounterLoopNoOB++;
                            if (TCT_Lib.iCounterLoopNoOB >= TCT_Lib.iMTS_Monitor_TimeAlertLoopNoOB)
                            {
                                //--------------------------------------------
                                if (TCT_Lib.iMTS_Monitor_IntervalSecAlertLoopNoOB > 0)
                                {
                                    TimeSpan dtSpan = DateTime.Now.Subtract(dtChkLoopOnOB);
                                    int iDiffSec = (int)dtSpan.TotalSeconds;
                                    if (iDiffSec < TCT_Lib.iMTS_Monitor_IntervalSecAlertLoopNoOB)
                                    {
                                        //--------------------------------------------
                                        LogInfo.WriteLogMTS($"+ No Send Alert Fail LoopNoOB => Count TimeAlertLoopNoOB = {TCT_Lib.iCounterLoopNoOB}, In {iDiffSec} sec. (Setting={TCT_Lib.iMTS_Monitor_IntervalSecAlertLoopNoOB} sec.)");

                                        TCT_Lib.iCounterLoopNoOB = 0;
                                        //--------------------------------------------
                                    }
                                    else
                                    {
                                        //--------------------------------------------
                                        LogInfo.WriteLogMTS($"+ Count TimeAlertLoopNoOB = {TCT_Lib.iCounterLoopNoOB} in {iDiffSec} sec. (Setting={TCT_Lib.iMTS_Monitor_IntervalSecAlertLoopNoOB} sec.)");
                                        TCT_Lib.iLastAvcStatus = -99;       // Set Error OB ON, no OFF
                                        if (!TCT_Lib.bSendLoopNoOB_MTS)
                                        {
                                            LogInfo.WriteLogMTS("+ Auto send alert fail OB to MTS : Loop=" + TCT_Lib.iMTS_Monitor_TimeAlertLoopNoOB);
                                            if (TCT_Lib.bMTS_NetworkStatus)
                                            {
                                                TCT_MTS.SendJsonMsgMtsDataToRabbitMQ();
                                                TCT_Lib.bSendLoopNoOB_MTS = true;
                                            }
                                            else
                                            {
                                                TCT_Lib.bMTS_NetworkStatus = (TCT_Lib.mtsRabbitMQ.ConnectMtsRabbitMQ());
                                                if (TCT_Lib.bMTS_NetworkStatus)
                                                {
                                                    TCT_MTS.SendJsonMsgMtsDataToRabbitMQ();
                                                    TCT_Lib.bSendLoopNoOB_MTS = true;
                                                }
                                                else
                                                    LogInfo.WriteLogMTS("- Fail Send MTS Data to RabbitMQ (Fail OB ON) : Network Status is not connected");
                                            }
                                        }
                                        else
                                        {
                                            LogInfo.WriteLogMTS("+ Not Send because send alert already.");
                                        }
                                        TCT_Lib.iCounterLoopNoOB = 0;
                                        //--------------------------------------------
                                    }
                                }
                                else
                                {
                                    //--------------------------------------------
                                    LogInfo.WriteLogMTS("+ Count TimeAlertLoopNoOB = " + TCT_Lib.iCounterLoopNoOB);
                                    TCT_Lib.iLastAvcStatus = -99;       // Set Error OB ON, no OFF
                                    if (!TCT_Lib.bSendLoopNoOB_MTS)
                                    {
                                        LogInfo.WriteLogMTS("+ Auto send alert fail OB to MTS : Loop=" + TCT_Lib.iMTS_Monitor_TimeAlertLoopNoOB);
                                        if (TCT_Lib.bMTS_NetworkStatus)
                                        {
                                            TCT_MTS.SendJsonMsgMtsDataToRabbitMQ();
                                            TCT_Lib.bSendLoopNoOB_MTS = true;
                                        }
                                        else
                                        {
                                            TCT_Lib.bMTS_NetworkStatus = (TCT_Lib.mtsRabbitMQ.ConnectMtsRabbitMQ());
                                            if (TCT_Lib.bMTS_NetworkStatus)
                                            {
                                                TCT_MTS.SendJsonMsgMtsDataToRabbitMQ();
                                                TCT_Lib.bSendLoopNoOB_MTS = true;
                                            }
                                            else
                                                LogInfo.WriteLogMTS("- Fail Send MTS Data to RabbitMQ (Fail OB ON) : Network Status is not connected");
                                        }
                                    }
                                    else
                                    {
                                        LogInfo.WriteLogMTS("+ Not Send because send alert already.");
                                    }
                                    TCT_Lib.iCounterLoopNoOB = 0;
                                    //--------------------------------------------
                                }
                            }
                        }
                    }

                    //-------------------------------------------------

                    //bTrigCaptureImage = false;

                    //if (bTrigDownOB)
                    //    bTrigDownOB = false;
                }
                //-----------------------------------------------------------------------------
                /*
                                if ((TCT_Lib.eCurLaneMode == LaneMode.HPMC) || (TCT_Lib.eCurLaneMode == LaneMode.MAINTENANCE))
                                {
                                    if (!bCurPassageLoop && bInPassageLoop)
                                        TCT_Lib.iTotalQueue++;
                                }
                */
                //------------------------------------------------------------------------------
                //--- Check Next Car can press
                //if (bInPassageLoop)
                //    iStepNextCar = 1;
                //--------------------------------------------------
                iEventID = (int)TCT_Lib.LaneDB.WriteLaneEventRTO_Device((int)Cmd_DI_Type.PassageLoop, bInPassageLoop ? 1 : 0);
                TCT_Lib.SendLaneEventObjToRTO(iEventID);
                bCurPassageLoop = bInPassageLoop;
            }

            bool bTrigPassageOB = false;
            if (bInPassageOB != bCurPassageOB)
            {
                bTrigPassageOB = true;
                LogInfo.WriteLaneEvent("IO - Passage OB : " + (bInPassageOB ? "ON" : "OFF"));
                if (bInPassageOB && !bCurPassageOB)     // Entry OB
                {
                    bDetectObBack = true;
                    bTrigDownOB = false;

                    if ((TCT_Lib.iCurTC_Class > 0) && (TCT_Lib.xListLoopCar.Count == 0) && (TCT_Lib.xCurPayment.payType > 0))
                    {
                        Process_RefreshPresenceLoop();
                        TCT_Lib.iTotalQueue--;
                        if (TCT_Lib.iTotalQueue < 0)
                            TCT_Lib.iTotalQueue = 0;
                    }

                    if (!bDetectLoopBack)
                    {
                        //======= For iVideoNVR_CaptureImageMethod by IO
                        if (TCT_Lib.iVideoNVR_CaptureImageMethod == 2)
                        {
                            LogInfo.WriteLaneEvent($"+ Capture Image : Start Capture Image =>  JobNo={TCT_Lib.iJobNo}, nTrx={TCT_Lib.nTrx}");
                            CreateProcessVideoCaptureImage(TCT_Lib.nTrx);
                        }
                        if (TCT_Lib.iVideoNVR_CaptureImageMethod == 3)
                        {
                            LogInfo.WriteLaneEvent($"+ Capture Image : Start Capture Image => Index=1,  JobNo={TCT_Lib.iJobNo}, nTrx={TCT_Lib.nTrx}");
                            CreateProcessVideoCaptureImage_Method_3(1, TCT_Lib.nTrx);
                            Thread.Sleep(500);
                            LogInfo.WriteLaneEvent($"+ Capture Image : Start Capture Image => Index=2 (No LoopOn),  JobNo={TCT_Lib.iJobNo}, nTrx={TCT_Lib.nTrx}");
                            CreateProcessVideoCaptureImage_Method_3(2, TCT_Lib.nTrx, false, false, true);
                            CreateProcessVideoCaptureImage_Method_3(3, TCT_Lib.nTrx);
                        }
                        //========================================
                    }
                    else
                    {
                        //========================================
                        if (TCT_Lib.iVideoNVR_CaptureImageMethod == 3)
                        {
                            LogInfo.WriteLaneEvent($"+ Capture Image : Start Capture Image =>  Index=2, JobNo={TCT_Lib.iJobNo}, nTrx={TCT_Lib.nTrx}");
                            CreateProcessVideoCaptureImage_Method_3(2, TCT_Lib.nTrx);
                            CreateProcessVideoCaptureImage_Method_3(3, TCT_Lib.nTrx);
                        }
                        //========================================
                    }
                }
                if (bInPassageOB)
                    bTrigPassageOB = false;
                //-----------------------------------------------------------------------------
                if (bCurPassageOB && !bInPassageOB)     // Exit OB
                {
                    bTrigDownOB = true;
                    if (bDetectObBack)
                        bFoundOB = true;
                    TCT_Lib.iTotalQueue--;
                    if (TCT_Lib.iTotalQueue < 0)
                        TCT_Lib.iTotalQueue = 0;

                    if (!bDetectLoopBack)
                        bDetectObBack = false;

                    //------------------------------------
                    TCT_Lib.iCounterLoopNoOB    = 0;    // Reset Loop No OB Counter
                    TCT_Lib.bSendLoopNoOB_MTS   = false;
                    //-----------------------------------------
                    //========================================
                    //if (TCT_Lib.iVideoNVR_CaptureImageMethod == 3)
                    {
                        //LogInfo.WriteLaneEvent($"+ Capture Image : Start Capture Image =>  Index=3, JobNo={TCT_Lib.iJobNo}, nTrx={TCT_Lib.nTrx}");
                        //CreateProcessVideoCaptureImage_Method_3(3, TCT_Lib.nTrx);
                    }
                    //========================================

                    /*
                                        if (bDetectObBack)
                                        {
                                            if (TCT_Lib.eCurLaneType == LaneType.MTC)
                                            {
                                                if (!bHasViolation)
                                                {
                                                    uint iAvcTimerId = GLib.Timeout.Add((uint)TCT_Lib.iTimerSecAutoAVC * 1000, new GLib.TimeoutHandler(Perform_ChkEventAVC));
                                                    AddAvcTimer(iAvcTimerId);
                                                }
                                            }
                                        }
                    */
                }
                //------------------------------------------------------------------------------
                iEventID = (int)TCT_Lib.LaneDB.WriteLaneEventRTO_Device((int)Cmd_DI_Type.PassageOB, bInPassageOB ? 1 : 0);
                TCT_Lib.SendLaneEventObjToRTO(iEventID);
                //bCurPassageOB   = bInPassageOB;
                //-----------------------------------------------------------------------------
                if (bInPassageOB && !bCurPassageOB)     // Entry OB
                {
                    // If no class type and payment
                    //--- Many Problem, Please check before deploy
                    /*
                                        if (TCT_Lib.eCurLaneType == LaneType.MTC)
                                        {
                                            if (TCT_Lib.xCurPayment.payType == (int)PayType.NONE)
                                            {
                                                if (bDetectLoopBack)
                                                    CreateProcessVideoCaptureImage(TCT_Lib.nTrx);
                                            }
                                        }
                    */

                    /*=========== Real Process ============================================
                                        // Caputre Image
                                        if ((TCT_Lib.iVideoNVR_CaptureImageMethod == 2) && bCurPassageLoop)
                                        {
                                            CreateProcessVideoCaptureImage(TCT_Lib.nTrx);
                                        }
                                        else
                                        {
                                            //--- If not found Loop but found payment
                                            if (TCT_Lib.xListLoopCar.Count > 0)
                                            {
                                                if ((TCT_Lib.iVideoNVR_CaptureImageMethod == 2) && (TCT_Lib.xListLoopCar[0].xPayment.payType != (int)PayType.NONE))
                                                    CreateProcessVideoCaptureImage(TCT_Lib.nTrx);
                                            }
                                        }
                    ============================================*/
                }
                //------------------------------------------------------------------------------
                bCurPassageOB = bInPassageOB;
            }
            //==============================================================================
            if (bInALB != bCurALB)
            {
                if (!bInALB)
                {
                    LogInfo.WriteLaneEvent("IO - ALB : OFF");
                    /*
                                        iQueueALB--;
                                        if (iQueueALB < 0)
                                            iQueueALB = 0;
                                        LogInfo.WriteLaneEvent($"IO - ALB : OFF  (Queue ALB={iQueueALB})");
                                        showQueueAlbControl(iQueueALB);
                    */
                }
                else
                    LogInfo.WriteLaneEvent("IO - ALB : ON");
                iEventID = (int)TCT_Lib.LaneDB.WriteLaneEventRTO_Device((int)Cmd_DI_Type.ALB, bInALB ? 1 : 0);
                TCT_Lib.SendLaneEventObjToRTO(iEventID);

                //-------------------------------------------------------------------------------------
                /*
                                bTrigCaptureImage = true;
                                if (!bCurALB && bInALB)     // ALB ON
                                {
                                    bTrigCaptureImage = false;
                                }
                */
                //-------------------------------------------------------------------------------------
                bCurALB = bInALB;
            }
            if (bInOTL != bCurOTL)
            {
                LogInfo.WriteLaneEvent("IO - OTL : " + (bInOTL ? "ON" : "OFF"));
                iEventID = (int)TCT_Lib.LaneDB.WriteLaneEventRTO_Device((int)Cmd_DI_Type.OTL, bInOTL ? 1 : 0);
                TCT_Lib.SendLaneEventObjToRTO(iEventID);
                bCurOTL = bInOTL;

                //-------------------------------------------------------------------

                if (bInOTL && (!TCT_Lib.bMaintenanceMode))
                {
                    if (TCT_Lib.bEnableLSD)
                    {
                        LogInfo.WriteLaneEvent("+ Send Open LSD : " + TCT_Lib.eCurLaneType);
                        if (TCT_Lib.iLSD_KeepAliveSec > 0)
                        {
                            if (TCT_Lib.eCurLaneType == LaneType.MTC)
                            {
                                TCT_Lib.eCurLsdMode = LsdMode.LSD_MTC;
                                if (TCT_Lib.iLSD_MethodType == 0)
                                    TCT_Lib.lsdController.CmdShow_OpenMTC();
                                else
                                    TCT_Lib.webSocketLane.laneSocketServerRTO.SendChangeLsdToRTO(TCT_Lib.eCurLsdMode);
                            }
                            else if (TCT_Lib.eCurLaneType == LaneType.ETC)
                            {
                                TCT_Lib.eCurLsdMode = LsdMode.LSD_ETC;
                                if (TCT_Lib.iLSD_MethodType == 0)
                                    TCT_Lib.lsdController.CmdShow_OpenETC();
                                else
                                    TCT_Lib.webSocketLane.laneSocketServerRTO.SendChangeLsdToRTO(TCT_Lib.eCurLsdMode);
                            }
                            else if (TCT_Lib.eCurLaneType == LaneType.DUAL)
                            {
                                TCT_Lib.eCurLsdMode = LsdMode.LSD_DUAL;
                                if (TCT_Lib.iLSD_MethodType == 0)
                                    TCT_Lib.lsdController.CmdShow_OpenDual();
                                else
                                    TCT_Lib.webSocketLane.laneSocketServerRTO.SendChangeLsdToRTO(TCT_Lib.eCurLsdMode);
                            }
                        }
                        else
                        {
                            //var lsdController = new LaneSocketControl_LSD();
                            if (TCT_Lib.eCurLaneType == LaneType.MTC)
                            {
                                TCT_Lib.eCurLsdMode = LsdMode.LSD_MTC;
                                //lsdController.CmdShow_OpenMTC();
                            }
                            else if (TCT_Lib.eCurLaneType == LaneType.ETC)
                            {
                                TCT_Lib.eCurLsdMode = LsdMode.LSD_ETC;
                                //lsdController.CmdShow_OpenETC();
                            }
                            else if (TCT_Lib.eCurLaneType == LaneType.DUAL)
                            {
                                TCT_Lib.eCurLsdMode = LsdMode.LSD_DUAL;
                                //lsdController.CmdShow_OpenDual();
                            }
                            if (TCT_Lib.iLSD_MethodType == 0)
                            {
                                var lsdController = new LaneSocketControl_LSD();
                                lsdController.RunInitialLSD();
                                //lsdController.CloseSocketConnection();
                            }
                            else
                                TCT_Lib.webSocketLane.laneSocketServerRTO.SendChangeLsdToRTO(TCT_Lib.eCurLsdMode);
                        }
                    }
                }


                if (!bInOTL && (!TCT_Lib.bMaintenanceMode))
                {
                    if (TCT_Lib.bEnableLSD)
                    {
                        LogInfo.WriteLaneEvent("+ Send Close LSD");
                        TCT_Lib.eCurLsdMode = LsdMode.LSD_CLOSE;
                        if (TCT_Lib.iLSD_KeepAliveSec > 0)
                        {
                            if (TCT_Lib.iLSD_MethodType == 0)
                                TCT_Lib.lsdController.CmdShow_LaneClose();
                            else
                                TCT_Lib.webSocketLane.laneSocketServerRTO.SendChangeLsdToRTO(LsdMode.LSD_CLOSE);
                        }
                        else
                        {
                            if (TCT_Lib.iLSD_MethodType == 0)
                            {
                                var lsdController = new LaneSocketControl_LSD();
                                lsdController.RunInitialLSD();
                                //lsdController.CmdShow_LaneClose();
                                //lsdController.CloseSocketConnection();
                            }
                            else
                                TCT_Lib.webSocketLane.laneSocketServerRTO.SendChangeLsdToRTO(TCT_Lib.eCurLsdMode);
                        }
                    }
                }

                //-------------------------------------------------------------------
            }

            if (bInLTL != bCurLTL)
            {
                LogInfo.WriteLaneEvent("IO - LTL : " + (bInLTL ? "ON" : "OFF"));
                iEventID = (int)TCT_Lib.LaneDB.WriteLaneEventRTO_Device((int)Cmd_DI_Type.LTL, bInLTL ? 1 : 0);
                TCT_Lib.SendLaneEventObjToRTO(iEventID);
                bCurLTL = bInLTL;
            }
            if (bInEthErr != bCurEthErr)
            {
                LogInfo.WriteLaneEvent("IO - EthernetError : " + (bInEthErr ? "ON" : "OFF"));
                iEventID = (int)TCT_Lib.LaneDB.WriteLaneEventRTO_Device((int)Cmd_DI_Type.EthErr, bInEthErr ? 1 : 0);
                TCT_Lib.SendLaneEventObjToRTO(iEventID);

                if (bInEthErr)
                {
                    // ---Send alarm to RTO
                    int iAlarmID = (int)TCT_Lib.LaneDB.WriteLaneAlarmRTO(AlarmDevice.ETHERNET, AlarmType.Occurred, "ETHERNET FAIL", "ETHERNET FAIL");
                    TCT_Lib.SendLaneAlarmObjToRTO(iAlarmID);
                    //--- Send Lane Transaction to RabbitMQ
                }

                bCurEthErr = bInEthErr;
            }
            if (bInSiren != bCurSiren)
            {
                LogInfo.WriteLaneEvent("IO - SIREN : " + (bInSiren ? "ON" : "OFF"));
                iEventID = (int)TCT_Lib.LaneDB.WriteLaneEventRTO_Device((int)Cmd_DI_Type.Siren, bInSiren ? 1 : 0);
                TCT_Lib.SendLaneEventObjToRTO(iEventID);
                bCurSiren = bInSiren;
            }

            //==============================================================================
            //--------------------------------------------------
            if (TCT_Lib.bMaintenanceMode)
            {
                if (TCT_Lib.eCurAppPage != AppPage.Main)
                    return;
            }
            //--------------------------------------------------
            try
            {
                if (TCT_Lib.eCurLaneMode != LaneMode.HPMC)
                {
                    //if (bDetectObBack && !TCT_Lib.bStartEndLoop)
                    if (bInPassageOB && !TCT_Lib.bStartEndLoop)
                    {
                        /*
                                                if (TCT_Lib.eTowingState != TowingState.SecPayType)
                                                {
                                                    if (TCT_Lib.iCurTC_Class == 0)
                                                    {
                                                        if (TCT_Lib.eCurLaneMode == LaneMode.CLOSED)
                                                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.CloseLane, (int)TFI_Pattern.Normal, "", "", "", szTFI_CloseLane, "");
                                                        else
                                                            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Welcome, (int)TFI_Pattern.Normal, "", "", "", "");
                                                    }
                                                }

                                                if (TCT_Lib.bMainUiReady)
                                                {
                                                    //if (!TCT_Lib.bLoopStep)
                                                    if (iStepNextCar > 0)
                                                    {
                                                        //LogInfo.WriteLog("- ClearUI from In-OB");
                                                        //clearPanelUiLaneControl();
                                                    }

                                                    //--- Check Next car in current queue and select car already
                                                    if ((TCT_Lib.iNextTC_Class > 0) && (iStepNextCar == 2))
                                                    {
                                                        iStepNextCar        = 0;
                                                        TCT_Lib.iTotalQueue -= 1;
                                                       LaneSocket_KeyPressedEvent(KeyBoardType.VK, (TCT_Lib.iNextTC_Class == 1) ? (int)KeyPressedType.CLASS_1 : (int)KeyPressedType.CLASS_2);
                                                    }
                                                }
                        */
                    }

                    if (TCT_Lib.eTowingState == TowingState.None)
                    {
                        //if (bInPassageLoop && bInPassageOB)
                        //if ((bTrigPassageOB && bInPassageLoop && bInPassageOB) ||
                        //    (bTrigPassageOB && !bInPassageLoop && !bDetectLoopBack && bDetectObBack))
                        //if (bTrigPassageOB && !bInPassageOB && bDetectLoopBack)
                        if ((bTrigPassageOB && !bInPassageOB && bDetectLoopBack) ||
                            (!bInPassageOB && bTrigDownOB && bDetectLoopBack))
                        {
                            if ((TCT_Lib.eCurLaneType == LaneType.MTC) ||
                                ((TCT_Lib.eCurLaneType == LaneType.DUAL) && bEditDual))
                            {
                                if (!bHasViolation)
                                {
                                    if (CheckIsViolation())
                                    {
                                        LogInfo.WriteLaneEvent($"bTrigPassageOB={bTrigPassageOB}, bInPassageOB={bInPassageOB}, bDetectLoopBack={bDetectLoopBack}, bTrigDownOB={bTrigDownOB}");
                                        LogInfo.WriteLaneEvent("+ Found Violation #1");
                                        bHasViolation       = true;
                                        //--- Reset State
                                        eCurVehicleType     = VehicleType.NONE;
                                        eCurPayType         = PayType.NONE;
                                        iStepNextCar        = 0;
                                        //--- Event Violation
                                        eventViolation();

                                        TCT_Lib.iCurTC_Class            = TCT_Lib.iCurTC_Class > 2 ? (int)VehicleType.VIOLATION : TCT_Lib.iCurTC_Class;
                                        TCT_Lib.xCurPayment.payAmount   = 0;
                                        TCT_Lib.xCurPayment.payType     = (int)PayType.Violation;
                                        TCT_Lib.xCurPayment.payTypeName = "VIOLATION";
                                        TCT_Lib.xCurPayment.payDateTime = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                                        TCT_Lib.szCurTransRemark        = "VIOLATION";
                                        szPrevPAN                       = "";
                                        eCurVehicleType                 = VehicleType.VIOLATION;
                                        vehicleTypeLaneControl(VehicleType.VIOLATION);
                                        LogInfo.WriteLaneEvent("IO - Siren : ON, ASB : ON");
                                        TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.Siren, ON);
                                        TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ASB, ON);
                                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.HPMC, (int)TFI_Pattern.Violation, "", "", "", szVehicle_Violation, "");

                                        uint iSirenTimerId = GLib.Timeout.Add((uint)TCT_Lib.iStopSirenAfterTimeSec * 1000, new GLib.TimeoutHandler(Perform_StopSiren));
                                        AddSirenTimer(iSirenTimerId);
                                        TCT_Lib.LaneDB.UpdateTransStatistic_Violation();
                                        TCT_Lib.bStartEndLoop = true;
                                        TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToClient("LANE_VIOLATION", null);
                                        //--- Send Lane Statisitc
                                        SendLaneCurrentTransStatistic();

                                        //Found Violation #1
                                        //---------------------------------------------------------
                                        if ((TCT_Lib.iVideoNVR_CaptureImageMethod == 0) || (TCT_Lib.iVideoNVR_CaptureImageMethod == 1))
                                            CreateProcessVideoCaptureImage(TCT_Lib.nTrx, false, true);
                                        //----------------------------------------------------------------------------------------
                                        if (bTrigDownOB && !bInPassageOB && bDetectLoopBack)
                                        {
                                            if (TCT_Lib.xCurPayment.payType == (int)PayType.Violation)
                                            {
                                                SendDigitalOutput_CmdALB_Close();
                                                //uint iAvcTimerId = GLib.Timeout.Add((uint)TCT_Lib.iTimerSecAutoAVC * 1000, new GLib.TimeoutHandler(Perform_ChkEventAVC));
                                                uint iAvcTimerId = GLib.Timeout.Add((uint)((TCT_Lib.iTimerSecAutoAVC * 1000)+700), new GLib.TimeoutHandler(Perform_ChkEventAVC));
                                                AddAvcTimer(iAvcTimerId);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        TCT_Lib.bStartEndLoop = true;
                                    }
                                }
                                else
                                {
                                    //---------------------------------------------------------
                                    //CreateProcessVideoCaptureImage(TCT_Lib.nTrx);
                                    //----------------------------------------------------------------------------------------
                                }
                            }
                        }

                        //if (!bInPassageLoop && !bInPassageOB && TCT_Lib.bStartEndLoop)
                        //if ((!bInPassageLoop || !bInPassageOB) && TCT_Lib.bStartEndLoop)
                        if ((bTrigPassageOB && TCT_Lib.bStartEndLoop && bFoundOB && !bInPassageLoop) ||
                            (bTrigDownOB && TCT_Lib.bStartEndLoop && bFoundOB && !bInPassageLoop))
                        //if (bTrigPassageOB && !bInPassageOB && TCT_Lib.bStartEndLoop)
                        {
                            //--- End Loop before AVC
                            //--- Clear LC (Clear CRS, TFI, Loop => Close -> ALB, LTL)
                            szPrevPAN = "";
                            if (TCT_Lib.iCurTC_Class == 0)
                            {
                                if (TCT_Lib.eCurLaneMode == LaneMode.CLOSED)
                                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.CloseLane, (int)TFI_Pattern.Normal, "", "", "", szTFI_CloseLane, "");
                                else
                                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Welcome, (int)TFI_Pattern.Normal, "", "", "", szTFI_StartLane1, szTFI_StartLane2);
                            }
                            //TCT_Lib.webSocketLane.OutIo_WriteCRS("", "");
                            if (iTimerClearCRS == 0)
                                iTimerClearCRS = GLib.Timeout.Add((uint)(iSecClearCRS * 1000), new GLib.TimeoutHandler(ClearTextCRS));

                            if (bDetectObBack && bDetectLoopBack)
                            {
                                if (TCT_Lib.eCurLaneType == LaneType.MTC)
                                {
                                    if (!bHasViolation)
                                    {
                                        //uint iAvcTimerId = GLib.Timeout.Add((uint)TCT_Lib.iTimerSecAutoAVC * 1000, new GLib.TimeoutHandler(Perform_ChkEventAVC));
                                        uint iAvcTimerId = GLib.Timeout.Add((uint)((TCT_Lib.iTimerSecAutoAVC * 1000)+700), new GLib.TimeoutHandler(Perform_ChkEventAVC));
                                        AddAvcTimer(iAvcTimerId);
                                    }
                                }

                                TCT_Lib.bStartEndLoop = false;
                                //LogInfo.WriteLaneEvent("+ Add Auto AVC");
                                //----------------------------------------------------------------                                             
                                if (TCT_Lib.eCurLaneMode != LaneMode.HPMC)
                                {
                                    bFoundLoopOB = bDetectObBack && bDetectLoopBack;
                                    if (TCT_Lib.bMainUiReady)
                                    {
                                        //clearPanelUiLaneControl();
                                    }
                                }
                                //if ((TCT_Lib.iNextTC_Class == 0) && (TCT_Lib.xCurPayment.payType == 0))
                                {
                                    SendDigitalOutput_CmdALB_Close();
                                    if (bTrigDownOB)
                                        bTrigDownOB = false;
                                }
                            }
                        }
                        else
                        {

                        }
                    }
                    //---------------------------------------------------------------------------
                    /*
                                        if (bTrigCaptureImage)
                                        {
                                            ProcessVideoCaptureThread(3, CameraPosition.ThirdCamera);
                                            //Thread.Sleep(100);
                                            //ProcessVideoCaptureThread(4, CameraPosition.FrontCamera);
                                            bTrigCaptureImage = false;
                                        }
                    */
                    //---------------------------------------------------------------------------
                }
                else
                {
                    /*
                                        //--- HPMC Mode
                                        if ((bInPassageLoop && bInPassageOB) ||
                                            (bCurPassageLoop && !bDetectLoopBack && bDetectObBack))
                                            TCT_Lib.bStartEndLoop = true;
                                        if (bTrigPassageOB && !bInPassageLoop && !bInPassageOB && TCT_Lib.bStartEndLoop)
                                        {
                                            uint iAvcTimer = GLib.Timeout.Add((uint)TCT_Lib.iTimerSecAutoAVC * 1000, new GLib.TimeoutHandler(Perform_ChkEventAVC));
                                            AddAvcTimer(iAvcTimer);
                                            LogInfo.WriteLaneEvent("+ Add Auto AVC (HPMC)");
                                        }
                    */
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error LaneSocket_DigitalInputLaneEvent : " + err.Message);
                LogInfo.WriteLog("- Error LaneSocket_DigitalInputLaneEvent : " + err.Message);
            }
        }

        private bool CheckIsViolation()
        {
            if (TCT_Lib.eCurLaneType == LaneType.ETC)
                return false;

            // if ((TCT_Lib.eCurLaneType == LaneType.DUAL) && !bEditDual)
            if (TCT_Lib.eCurLaneType == LaneType.DUAL)
                return false;

            if (!bInPassageOB && bCurPassageLoop)
                return false;

            if (TCT_Lib.xListLoopCar.Count == 0)
            {
                if (TCT_Lib.iCurTC_Class == 0)
                    return true;

                if ((TCT_Lib.iCurTC_Class == (int)VehicleType.LEFT_EXIT) ||
                    (TCT_Lib.iCurTC_Class == (int)VehicleType.EMERGENCY) ||
                    (TCT_Lib.iCurTC_Class == (int)VehicleType.EXEMPT) ||
                    (TCT_Lib.iCurTC_Class == (int)VehicleType.HPMC))
                    return false;

                if (TCT_Lib.xCurPayment.payType == (int)PayType.NONE)
                    return true;
            }
            else
            {
                // v2.0.0: null-safe locked snapshot instead of repeated xListLoopCar[0]
                var firstLoopCar = TCT_Lib.LoopCarFirst();
                if (firstLoopCar == null)
                    return true;

                if (firstLoopCar.iTC_Class == 0)
                    return true;

                if ((firstLoopCar.iTC_Class == (int)VehicleType.LEFT_EXIT) ||
                    (firstLoopCar.iTC_Class == (int)VehicleType.EMERGENCY) ||
                    (firstLoopCar.iTC_Class == (int)VehicleType.EXEMPT) ||
                    (firstLoopCar.iTC_Class == (int)VehicleType.HPMC))
                    return false;

                if (firstLoopCar.xPayment.payType == (int)PayType.NONE)
                    return true;
            }

            return false;
        }

        private bool Perform_AutoAVC()
        {
            Application.Invoke(delegate
            {
                LaneSocket_AvcLaneEvent(1, 0, 0, 0, 0, 0, 0);
                GLib.Source.Remove(iTimerAutoAVC);
                iTimerAutoAVC = 0;
            });

            return false;
        }


        //=================================================================================================
        protected void LaneSocket_DeviceOuputLaneEvent(int[] values)
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

            if (digitalOutputLaneControl != null)
                digitalOutputLaneControl(values);
        }

        public void SendDigitalOutput_CmdALB_Open_V0()
        {
#if __SIM__
            TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.ALB, ON);
#else
            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Open, ON);
            Thread.Sleep(50);
            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Open, OFF);
#endif
        }

        public void SendDigitalOutput_CmdALB_Close_V0()
        {
#if __SIM__
            TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.ALB, OFF);
#else
           
            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Close, ON);
            //Thread.Sleep(1000);
            //TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Open, OFF);
            Thread.Sleep(50);
            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Close, OFF);
#endif
        }

        public void CaptureImageVideoWhenOpenALB_RTO()
        {
            LogInfo.WriteLaneEvent("+ Capture Image from OpenALB RTO");
            //--------------------------------------------
            //=== 2023-08-01 Add capture image when RTO send Open ALB
            if ((TCT_Lib.iVideoNVR_CaptureImageMethod == 0) || (TCT_Lib.iVideoNVR_CaptureImageMethod == 1))
                CreateProcessVideoCaptureImage(TCT_Lib.nTrx);
            //------------------------------------------------
        }

        public void SendDigitalOutput_CmdALB_Open_RTO()
        {
            LogInfo.WriteLaneEvent("Recv Cmd ALB Open from RTO");
            TCT_Lib.bStatusOpenALB_RTO = true;

            //--------------------------------------------
            //=== 2023-08-01 Add capture image when RTO send Open ALB
            if ((TCT_Lib.iVideoNVR_CaptureImageMethod == 0) || (TCT_Lib.iVideoNVR_CaptureImageMethod == 1))
                CreateProcessVideoCaptureImage(TCT_Lib.nTrx);
            //------------------------------------------------
#if __SIM__
            TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.ALB, ON);
#else
            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Open, ON);
            iTimerALB_On = GLib.Timeout.Add(300, new GLib.TimeoutHandler(() => Thread_CmdControlOffALB_Open()));
#endif
        }

        public void SendDigitalOutput_CmdALB_Open()
        {
            iQueueALB++;
            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.LTL, ON);
            LogInfo.WriteLaneEvent($"Send Cmd ALB Open : QueueALB={iQueueALB}");
            showQueueAlbControl(iQueueALB);
#if __SIM__
            TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.ALB, ON);
#else
            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Open, ON);
            iTimerALB_On = GLib.Timeout.Add(400, new GLib.TimeoutHandler(() => Thread_CmdControlOffALB_Open()));
#endif

            //---------------------------------------------------------------------------
            //ProcessVideoCaptureThread(3, CameraPosition.ThirdCamera, TCT_Lib.iIsTowing>0);
            //Thread.Sleep(100);
            //ProcessVideoCaptureThread(4, CameraPosition.FrontCamera);
            //---------------------------------------------------------------------------
        }

        private bool Thread_CmdControlOffALB_Open()
        {
            Application.Invoke(delegate
            {
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Open, OFF);
                // v2.0.0: replace blocking Thread.Sleep(100) on the GTK main thread with a
                // non-blocking one-shot timer so the UI keeps rendering during ALB pulse.
                GLib.Timeout.Add(100, new GLib.TimeoutHandler(() =>
                {
                    TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Open, ON);
                    iTimerALB_On = GLib.Timeout.Add(300, new GLib.TimeoutHandler(() => Thread_CmdControlOffALB_Open_2()));
                    return false;
                }));
            });

            return false;
        }

        private bool Thread_CmdControlOffALB_Open_2()
        {
            Application.Invoke(delegate
            {
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Open, OFF);
            });

            return false;
        }

        public void SendDigitalOutput_CmdALB_Close()
        {
            if (iQueueALB < 0)
                iQueueALB = 0;
            if (TCT_Lib.bMainUiReady)
                showQueueAlbControl(iQueueALB);

            if (iQueueALB > 0)
            {
                iQueueALB--;
                if (TCT_Lib.bMainUiReady)
                {
                    showQueueAlbControl(iQueueALB);
                    LogInfo.WriteLaneEvent("Send Cmd ALB Close");
                }
                if (iQueueALB > 0)
                {
                    LogInfo.WriteLaneEvent($"Ignore Cmd ALB Close => Because found open queue>0 (Queue ALB={iQueueALB}), Now not close ALB.");
                    return;
                }
            }
            else
                LogInfo.WriteLaneEvent("Send Cmd ALB Close : QueueALB=" + iQueueALB);

            /*
                        if (iQueueALB > 1)
                        {
                            iQueueALB--;
                            if (TCT_Lib.bMainUiReady)
                                showQueueAlbControl(iQueueALB);
                            LogInfo.WriteLaneEvent($"Ignore Cmd ALB Close => Because found open queue>0 (Queue ALB={iQueueALB}), Now not close ALB.");
                            return;
                        }
                        else
                            LogInfo.WriteLaneEvent("Send Cmd ALB Close : QueueALB=" + iQueueALB);
            */

            //--------------------------------------------
            //--- 2022-03-31    // Edit Dual (Then not close ALB)
            if (TCT_Lib.bMaintenanceMode)
            {
                if (TCT_Lib.eCurLaneType == LaneType.DUAL)
                {
                    //if (bEditDual && TCT_Lib.bEtcCtrlIO)
                    if (bEditDual)
                        return;
                }
            }
            //-------------------------------------

#if __SIM__
            TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.ALB, OFF);
#else
            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Close, ON);
            iTimerALB_Off = GLib.Timeout.Add(400, new GLib.TimeoutHandler(() => Thread_CmdControlOffALB_Close()));
            if (bInALB)
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Open, OFF);
#endif
        }


        private bool Thread_CmdControlOffALB_Close()
        {
            Application.Invoke(delegate
            {
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Close, OFF);
                // v2.0.0: replace blocking Thread.Sleep(100) on the GTK main thread with a
                // non-blocking one-shot timer so the UI keeps rendering during ALB pulse.
                GLib.Timeout.Add(100, new GLib.TimeoutHandler(() =>
                {
                    TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Close, ON);
                    iTimerALB_Off = GLib.Timeout.Add(300, new GLib.TimeoutHandler(() => Thread_CmdControlOffALB_Close_2()));
                    return false;
                }));
            });

            return false;
        }

        private bool Thread_CmdControlOffALB_Close_2()
        {
            Application.Invoke(delegate
            {
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Close, OFF);
            });

            return false;
        }

        private bool Thread_CmdControlOffALB_V0(bool bOpen)
        {
            Application.Invoke(delegate
            {
                if (bOpen)
                {
                    TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Open, OFF);
                }
                else
                    TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ALB_Close, OFF);
                //GLib.Source.Remove(iTimerALB);
                //return;
            });

            return false;
        }
        //===========================================================================================

        //--- Send when force open (HPMC, Towing)
        public void SendEtcRabbitMq_CmdALB_ForceOpen(string szReason = "", bool bForceMode = false)
        {
            if (bForceMode)
            {
                //-- Force from RTO
                //if (TCT_Lib.bAllowETC)
                //    TCT_Lib.etcRabbitMQ.SendALB_ModeToETC(ALB_Mode.Open, szReason);
                if (TCT_Lib.bMainUiReady)
                    drawETC_AlbMode(1);

                //------------------------------------------------------------------
                int iTransID = (int)TCT_Lib.LaneDB.WriteLaneTransaction_ETC_AutoSignalCode("81", "Open ALB from RTO");

                //--- Send Lane Transaction to RabbitMQ
                if (iTransID != 0)
                {
                    TCT_Lib.SendLaneTransObjToRTO(iTransID);
                    TCT_Lib.SendLaneTransObjToDC(iTransID);
                    TCT_Lib.SendLaneTransObjToSupAdjust(iTransID);
                    //if (TCT_Lib.bEnableMTS)
                    //    TCT_Lib.SendLaneTransObjToMTS(iTransID);
                }
                show_nTrxControl(TCT_Lib.nTrx);
                //-------------------------------------------------------------------
                return;
            }

            //drawETC_AlbMode(1);
            LogInfo.WriteLog("+++++++++ ALB_FORCE_OPEN");
            LogInfo.WriteEtcRabbitMQ("+++++++++ ALB_FORCE_OPEN");
            // ---Check If current mode is ETC, this process will skip
            if (TCT_Lib.eCurLaneType == LaneType.ETC)
                return;
            else if (TCT_Lib.eCurLaneType == LaneType.DUAL)
            {
                // In Dual mode, if pay with card, ETC will control IO
                if (TCT_Lib.bEtcCtrlIO)
                    return;
                else
                {
                    if (szReason == "")
                    {
                        szReason = "Open Normal Payment";
                        if (TCT_Lib.eTowingState != TowingState.None)
                            szReason = "Open for Towing Mode";
                        if (eCurVehicleType == VehicleType.HPMC)
                            szReason = "Open for HPMC";
                        else if (eCurVehicleType == VehicleType.EXEMPT)
                            szReason = "Open for Exempt";
                        else if (eCurVehicleType == VehicleType.LEFT_EXIT)
                            szReason = "Open for Left Exit";
                        else if (eCurVehicleType == VehicleType.EMERGENCY)
                            szReason = "Open for Emergency";
                        else if (eCurVehicleType == VehicleType.VIOLATION)
                            szReason = "Open for Violation";
                    }

                    //--------------------------------------------------------
                    TCT_Lib.etcRabbitMQ.SendALB_ModeToETC(ALB_Mode.Open, szReason);
                    if (TCT_Lib.bMainUiReady)
                        drawETC_AlbMode(1);
                    return;
                }
            }
        }

        //--- Send when normal case, payment approve, and closed after vehicle leaves
        public void SendEtcRabbitMq_CmdALB_Normal(string szReason = "Normal Mode", bool bForceMode = false)
        {
            if (bForceMode)
            {
                TCT_Lib.etcRabbitMQ.SendALB_ModeToETC(ALB_Mode.Normal, szReason);
                if (TCT_Lib.bMainUiReady)
                    drawETC_AlbMode(0);
                return;
            }
            //drawETC_AlbMode(0);
            //LogInfo.WriteLog("+++++++++ ALB_NORMAL");
            //LogInfo.WriteEtcRabbitMQ("+++++++++ ALB_NORMAL");
            // ---Check If current mode is ETC, this process will skip
            if (TCT_Lib.eCurLaneType == LaneType.ETC)
                return;
            else if (TCT_Lib.eCurLaneType == LaneType.DUAL)
            {
                if (bTowingAVC)
                {
                    LogInfo.WriteLog("+ Send ALB_Normal when Towing in DUAL mode.");
                    LogInfo.WriteEtcRabbitMQ("+ Send ALB_Normal when Towing in DUAL mode.");
                    TCT_Lib.etcRabbitMQ.SendALB_ModeToETC(ALB_Mode.Normal, szReason);
                    if (TCT_Lib.bMainUiReady)
                        drawETC_AlbMode(0);
                    return;
                }
                else
                {
                    // In Dual mode, if pay with card, ETC will control IO
                    if (TCT_Lib.bEtcCtrlIO)
                        return;
                    else
                    {
                        TCT_Lib.etcRabbitMQ.SendALB_ModeToETC(ALB_Mode.Normal, szReason);
                        if (TCT_Lib.bMainUiReady)
                            drawETC_AlbMode(0);
                        return;
                    }
                }
            }
        }

        private void SendEtcRabbitMq_CmdALB_Close(bool bForceMode = false)
        {
            if (TCT_Lib.bMainUiReady)
                drawETC_AlbMode(2);
            //LogInfo.WriteLog("+++++++++ ALB_CLOSE");
            //LogInfo.WriteEtcRabbitMQ("++++++++ ALB_CLOSE");
            if (!bForceMode)
            {
                // ---Check If current mode is ETC, this process will skip
                if (TCT_Lib.eCurLaneType == LaneType.ETC)
                    return;
                else if (TCT_Lib.eCurLaneType == LaneType.DUAL)
                {
                    // In Dual mode, if pay with card, ETC will control IO
                    if (TCT_Lib.bEtcCtrlIO)
                        return;
                    else
                    {
                        string szReason = "Close Normal Payment";
                        if (TCT_Lib.eTowingState != TowingState.None)
                            szReason = "Close for Towing Mode";
                        if (eCurVehicleType == VehicleType.HPMC)
                            szReason = "Close for HPMC";
                        else if (eCurVehicleType == VehicleType.EXEMPT)
                            szReason = "Close for Exempt";
                        else if (eCurVehicleType == VehicleType.LEFT_EXIT)
                            szReason = "Close for Left Exit";
                        else if (eCurVehicleType == VehicleType.EMERGENCY)
                            szReason = "Close for Emergency";
                        else if (eCurVehicleType == VehicleType.VIOLATION)
                            szReason = "Close for Violation";

                        if (TCT_Lib.bStatusOpenALB_RTO)
                            szReason = "Auto Close from RTO";

                        //--------------------------------------------------------
                        TCT_Lib.etcRabbitMQ.SendALB_ModeToETC(ALB_Mode.Closed, szReason);
                        if (TCT_Lib.bMainUiReady)
                            drawETC_AlbMode(2);
                        TCT_Lib.etcRabbitMQ.SendALB_ModeToETC(ALB_Mode.Normal, szReason);
                        if (TCT_Lib.bMainUiReady)
                            drawETC_AlbMode(0);
                    }
                }
            }
            else
            {
                string szReason = "Close Normal Payment";
                if (TCT_Lib.eTowingState != TowingState.None)
                    szReason = "Close for Towing Mode";
                if (eCurVehicleType == VehicleType.HPMC)
                    szReason = "Close for HPMC";
                else if (eCurVehicleType == VehicleType.EXEMPT)
                    szReason = "Close for Exempt";
                else if (eCurVehicleType == VehicleType.LEFT_EXIT)
                    szReason = "Close for Left Exit";
                else if (eCurVehicleType == VehicleType.EMERGENCY)
                    szReason = "Close for Emergency";
                else if (eCurVehicleType == VehicleType.VIOLATION)
                    szReason = "Close for Violation";

                if (TCT_Lib.bStatusOpenALB_RTO)
                    szReason = "Auto Close from RTO";

                //--------------------------------------------------------
                TCT_Lib.etcRabbitMQ.SendALB_ModeToETC(ALB_Mode.Closed, szReason);
                if (TCT_Lib.bMainUiReady)
                    drawETC_AlbMode(2);
                TCT_Lib.etcRabbitMQ.SendALB_ModeToETC(ALB_Mode.Normal, szReason);
                if (TCT_Lib.bMainUiReady)
                    drawETC_AlbMode(0);
            }

        }

        protected void SimLanePassageLoop()
        {
            return;
            /*
                        // ---Check If current mode is ETC, this process will skip
                        if (TCT_Lib.eCurLaneType == LaneType.ETC)
                            return;
                        else if (TCT_Lib.eCurLaneType == LaneType.DUAL)
                        {
                            // In Dual mode, if pay with card, ETC will control IO
                            if (TCT_Lib.bEtcCtrlIO)
                                return;
                        }

                        //------------------------------------------------------------------
                        TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.PassageLoop, ON);
                        TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.PassageOB, ON);
                        iPassageTimerId = GLib.Timeout.Add(1500, new GLib.TimeoutHandler(Perform_PassageOFF));
            */
        }

        private bool Perform_PassageOFF()
        {
            TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.PassageLoop, OFF);
            TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.PassageOB, OFF);

            if (iPassageTimerId > 0)
            {
                GLib.Source.Remove(iPassageTimerId);
                iPassageTimerId = 0;
            }
            return true;
        }

        public void SuccessEmvPayment(int iEmvType)     // EmvType : 0=CreditCard, 1=QR Code
        {
            LogInfo.WriteLaneEvent("+ Success EMV Payment Type : " + (iEmvType == 0 ? "Credit Card" : "QR Code") + ", JobNo=" + TCT_Lib.iJobNo + ", nTrx=" + (TCT_Lib.eTowingState == TowingState.SecClassType ? TCT_Lib.nTrx+1 : TCT_Lib.nTrx));
            LogInfo.WriteLogEDC("+ Success EMV Payment Type : " + (iEmvType == 0 ? "Credit Card" : "QR Code") + ", JobNo=" + TCT_Lib.iJobNo + ", nTrx=" + (TCT_Lib.eTowingState == TowingState.SecClassType ? TCT_Lib.nTrx + 1 : TCT_Lib.nTrx));
            if (TCT_Lib.eTowingState == TowingState.None)
            {
                TCT_Lib.xCurPayment.payType     = iEmvType == 0 ? (int)PayType.EMV : (int)PayType.QrCode;
                TCT_Lib.xCurPayment.payTypeName = TCT_Lib.GetPayMethodName(iEmvType == 0 ? (int)PayType.EMV : (int)PayType.QrCode);
                TCT_Lib.xCurPayment.payDateTime = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                eCurPayType                     = iEmvType == 0 ? PayType.EMV : PayType.QrCode;

                TCT_Lib.xCurPayment.emvDetailID = TCT_Lib.LaneDB.WriteEdcDetail(TCT_Lib.xCurPayment.payType);

                LogInfo.WriteLogEDC("+ Finish Pay EMV");
                payTypeLaneControl(eCurPayType);

                LogInfo.WriteLogEDC("+ Send IO -> Ring bell");
                TCT_Lib.webSocketLane.OutIo_RBell();

                LogInfo.WriteLogEDC("+ Write Text CRS");
                WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), GetStrPayTypeCRS(eCurPayType));

                LogInfo.WriteLogEDC("+ Send IO -> TFI");
                if (eCurVehicleType == VehicleType.CLASS_1)
                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass1.ToString("0"), "", iEmvType == 0 ? szPay_EMV : szPay_QrCode, szAmount_Class1);
                else
                    TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass2.ToString("0"), "", iEmvType == 0 ? szPay_EMV : szPay_QrCode, szAmount_Class2);

                bEditModeETC = false;

                LogInfo.WriteLogEDC("+ Show Mode Type");
                showModeType(TCT_Lib.bAllowETC ? "ETC" : "MTC");

                LogInfo.WriteLogEDC("+ Send IO -> Open ALB");
                SendDigitalOutput_CmdALB_Open();
                //LogInfo.WriteLogEDC("+8");
                if (TCT_Lib.bAllowETC)
                {
                    if (TCT_Lib.bMainUiReady)
                        drawETC_AlbMode(0);
                    TCT_Lib.szPrevTransId = TCT_Lib.szCurTransId;
                    TCT_Lib.etcRabbitMQ.SendPassagePaymentToETC((int)TCT_Lib.LaneDB.GetCurTransID(), TCT_Lib.iCurTC_Class, iEmvType == 0 ? (int)PayType.EMV : (int)PayType.QrCode,
                                            TCT_Lib.fCurFareAmount, ALB_Mode.Normal, bReClassETC);
                }

                LogInfo.WriteLogEDC("+ Capture Video NVR");
                if ((TCT_Lib.iVideoNVR_CaptureImageMethod == 0) || (TCT_Lib.iVideoNVR_CaptureImageMethod == 1))
                    CreateProcessVideoCaptureImage(TCT_Lib.nTrx);
                //----------------------------------------------------------------------------------------
                //LogInfo.WriteLogEDC("+9");
                SendEtcRabbitMq_CmdALB_Normal("Normal Payment - EMV");
                TCT_Lib.bAfterPayment   = true;
                iStepNextCar            = 1;
                //LogInfo.WriteLogEDC("+10");
                //---------------------------------------------------------------------------------------
#if __SIM__
                Thread.Sleep(1000);
                SimLanePassageLoop();
#endif
            }
            else
            {
                //--- Towing Mode
                if (TCT_Lib.eTowingState == TowingState.FirstClassType)
                {
                    TCT_Lib.eTowingState            = TowingState.FirstPayType;
                    TCT_Lib.xCurPayment.payType     = iEmvType == 0 ? (int)PayType.EMV : (int)PayType.QrCode;
                    TCT_Lib.xCurPayment.payTypeName = TCT_Lib.GetPayMethodName(iEmvType == 0 ? (int)PayType.EMV : (int)PayType.QrCode);
                    TCT_Lib.xCurPayment.payDateTime = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                    eCurPayType = iEmvType == 0 ? PayType.EMV : PayType.QrCode;

                    TCT_Lib.xCurPayment.emvDetailID = TCT_Lib.LaneDB.WriteEdcDetail(TCT_Lib.xCurPayment.payType);

                    payTypeLaneControl(eCurPayType);
                    WriteTextCRS(GetStrClassTypeCRS(eCurVehicleType), GetStrPayTypeCRS(eCurPayType));
                    if (eCurVehicleType == VehicleType.CLASS_1)
                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass1.ToString("0"), "", iEmvType == 0 ? szPay_EMV : szPay_QrCode, szAmount_Class1);
                    else
                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, GetStrClassTypeCRS(eCurVehicleType), TCT_Lib.fFareClass2.ToString("0"), "", iEmvType == 0 ? szPay_EMV : szPay_QrCode, szAmount_Class2);
                    payTypeLaneControl(eCurPayType);

                    iStepNextCar = 1;
                }
                else if (TCT_Lib.eTowingState == TowingState.SecClassType)
                {
                    TCT_Lib.eTowingState                = TowingState.SecPayType;
                    TCT_Lib.xTowingPayment.payType      = iEmvType == 0 ? (int)PayType.EMV : (int)PayType.QrCode;
                    TCT_Lib.xTowingPayment.payTypeName  = TCT_Lib.GetPayMethodName(iEmvType == 0 ? (int)PayType.EMV : (int)PayType.QrCode);
                    TCT_Lib.xTowingPayment.payDateTime  = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);

                    TCT_Lib.xTowingPayment.emvDetailID = TCT_Lib.LaneDB.WriteEdcDetail(TCT_Lib.xTowingPayment.payType, true);

                    payTypeLaneControl(iEmvType == 0 ? PayType.EMV : PayType.QrCode);
                    WriteTextCRS(TCT_Lib.iTowingTC_Class.ToString(), GetStrPayTypeCRS(iEmvType == 0 ? PayType.EMV : PayType.QrCode));
                    if (TCT_Lib.iTowingTC_Class == (int)VehicleType.CLASS_1)
                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, TCT_Lib.iTowingTC_Class.ToString(), TCT_Lib.fFareClass1.ToString("0"), "", iEmvType == 0 ? szPay_EMV : szPay_QrCode, szAmount_Class1);
                    else
                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare_Payment, (int)TFI_Pattern.Normal, TCT_Lib.iTowingTC_Class.ToString(), TCT_Lib.fFareClass2.ToString("0"), "", iEmvType == 0 ? szPay_EMV : szPay_QrCode, szAmount_Class2);
                    TCT_Lib.webSocketLane.OutIo_RBell();
                    SendDigitalOutput_CmdALB_Open();
                    SendEtcRabbitMq_CmdALB_ForceOpen();
                    TCT_Lib.bAfterPayment = true;
                    // ---Check Next Car can press
                    iStepNextCar = 1;
                    //---------------------------------------------------------------------------------------
#if __SIM__
                    SimLanePassageLoop();
#endif
                }
            }
        }

        public void WriteTextCRS(string szStr1, string szStr2)
        {
            Application.Invoke(delegate
            {
                if (iTimerClearCRS > 0)
                    GLib.Source.Remove(iTimerClearCRS);
                iTimerClearCRS = 0;

                TCT_Lib.webSocketLane.OutIo_WriteCRS(szStr1, szStr2);
            });
        }

        private bool ClearTextCRS()
        {
            Application.Invoke(delegate
            {
                TCT_Lib.webSocketLane.OutIo_WriteCRS("", "");
                if (iTimerClearCRS > 0)
                    GLib.Source.Remove(iTimerClearCRS);
                iTimerClearCRS = 0;
            });

            return false;
        }

        public void ClearTextCRS_EndJob()
        {
            Application.Invoke(delegate
            {
                TCT_Lib.webSocketLane.OutIo_WriteCRS_EndJob();

                foreach (uint iTimer in iSirenTimerIds.ToList())
                    GLib.Source.Remove(iTimer);
                iSirenTimerIds.Clear();

                foreach (uint iAvcTimer in iWaitAvcTimerIds.ToList())
                    GLib.Source.Remove(iAvcTimer);
                iWaitAvcTimerIds.Clear();

                if (iTimerClearCRS > 0)
                    GLib.Source.Remove(iTimerClearCRS);
                iTimerClearCRS = 0;

                if (iPassageTimerId > 0)
                {
                    GLib.Source.Remove(iPassageTimerId);
                    iPassageTimerId = 0;
                }

                if (iPassageTimerId > 0)
                {
                    GLib.Source.Remove(iPassageTimerId);
                    iPassageTimerId = 0;
                }
            });
        }

        public void ClearAndInitialVar()
        {
            bInPassageOB = false;
            bDetectLoopBack = false;
            bTrigDownOB = false;
            bHasViolation = false;
        }

        //---------------------------------------------------------
        public void SendCmdReadDigitalInputOutput()
        {
            TCT_Lib.webSocketLane.SendRequest_DigitalInputOutput();
        }

        protected bool Perform_StopSiren()
        {
            RemoveSirenTimer();

            LogInfo.WriteLaneEvent("IO - Siren : OFF, ASB : OFF");
            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.Siren, OFF);
            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ASB, OFF);
            //LogInfo.WriteLog("- ClearUI from Stop-Siren");
            clearPanelUiLaneControl();

            //--------------------------------------------------------------------------------------------------
            TCT_Lib.iTotalQueue     = 0;
            eCurVehicleType         = VehicleType.NONE;
            eCurPayType             = PayType.NONE;
            iStepNextCar            = 0;
            szPrevPAN               = "";
            bLoopMis                = false;
            bTowingAVC              = false;
            bReClassETC             = false;
            bEditBlockETC           = false;
            bEditDual               = false;

            bFoundLoopOB            = false;
            bDetectLoopBack         = false;
            bDetectObBack           = false;
            bFoundOB                = false;
            bFoundLoop              = false;
            bTrigDownLoop           = false;
            bTrigDownOB             = false;

            TCT_Lib.xListLoopCar.Clear();
            showTotalLoop();
            TCT_Lib.iNextTC_Class   = 0;
            TCT_Lib.eTowingState    = TowingState.None;
            vehicleTypeLaneControl(eCurVehicleType);
            payTypeLaneControl(eCurPayType);
            TCT_Lib.ClearCurTransData();
            TCT_Lib.webSocketLane.OutIo_ClearCRS();
            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.LTL, OFF);
            if (TCT_Lib.eCurLaneMode == LaneMode.CLOSED)
                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.CloseLane, (int)TFI_Pattern.Normal, "", "", "", szTFI_CloseLane, "");
            else
                TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Welcome, (int)TFI_Pattern.Normal, "", "", "", szTFI_StartLane1, szTFI_StartLane2);

            if (TCT_Lib.bAllowETC)
                TCT_Lib.etcRabbitMQ.Send_ClearQueue_ToETC();

            bHasViolation = false;
#if __SIM__
            //--- Sim Device Input LC
            TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.PresenceLoop, OFF);
            TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.PassageLoop, OFF);
            TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.PresenceOB, OFF);
            TCT_Lib.webSocketLane.SendDeviceInput_Controller((int)Cmd_DI_Type.PassageOB, OFF);
#endif

            //------------------------------------------------------------------------------------

            return true;
        }

        protected bool Perform_StopASB()
        {
            if (bHasViolation)
                Perform_StopSiren();
            else
            {
                RemoveSirenTimer();

                LogInfo.WriteLaneEvent("IO - Siren : OFF, ASB : OFF");
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ASB, OFF);
                TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.Siren, OFF);
                //LogInfo.WriteLog("- ClearUI from Stop-ASB");
                //clearPanelUiLaneControl();
            }

            return true;
        }

        public void SendLaneCurrentState()
        {
            if (!TCT_Lib.bRTO_Connected)
                return;

            object objParams = new {
                LaneConfigType  = TCT_Lib.iLaneConfigType,
                LaneType        = (int)TCT_Lib.eCurLaneType,
                LaneMode        = (int)TCT_Lib.eCurLaneMode,
                LaneDirection   = (int)TCT_Lib.eLaneDirection,
                StaffId         = (TCT_Lib.eCurLaneMode == LaneMode.CLOSED) ? "" : TCT_Lib.szStaffID,
                StaffCode       = (TCT_Lib.eCurLaneMode == LaneMode.CLOSED) ? "" : TCT_Lib.szStaffCode,
                StaffName       = (TCT_Lib.eCurLaneMode == LaneMode.CLOSED) ? "" : TCT_Lib.szStaffName,
                StaffShiftId    = TCT_Lib.iStaffShiftID,
                StartShiftTime  = (TCT_Lib.eCurLaneMode == LaneMode.CLOSED) ? TCT_Lib.LaneDB.GetLastStaffEndShift() : TCT_Lib.dtStaffStartShift.ToString("HH:mm:ss"),
                SumCurVehicles  = TCT_Lib.LaneDB.GetTotalTransSumVehicles(),
            };
            TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToClient("LANE_STATE", objParams);
            //LogInfo.WriteLog("+ Send LaneState");
            //-----------------------------------------------------
            if (TCT_Lib.szSupervisorID == "")
                TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToClient("REQ_SUPERVISOR", null);
        }

        public void SendLaneRealTimeState()
        {
            object objParams = new
            {
                EmvState    = TCT_Lib.bEnableEdc ? (TCT_Lib.bEMV_Connected ? 1 : 0) : -1,
                MainAntennaState        = TCT_Lib.bEnableAntenna ? (TCT_Lib.bMainAntenna_Connected ? 1 : 0) : -1,
                SecondaryAntennaState   = TCT_Lib.bEnableAntenna ? (TCT_Lib.bSecAntenna_Connected ? 1 : 0) : -1,
                LsdState    = TCT_Lib.bEnableLSD ? (TCT_Lib.bLSD_Connected ? TCT_Lib.iLsdMode_Status : 0) : -1,
                LcsdState   = TCT_Lib.bLCSD_Connected ? 1 : 0,
                LcioState   = TCT_Lib.bLCIO_Connected ? 1 : 0,
            };
            TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToClient("REALTIME_STATE", objParams);
        }

        public void SendLaneCloseApp()
        {
            TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToClient("CLOSE_APP", null);
        }

        public void StartShiftProcess()
        {
            TCT_Lib.webSocketLane.OutIo_ClearCRS();
            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Welcome, (int)TFI_Pattern.Normal, "", "", "", szTFI_StartLane1, szTFI_StartLane2);
            //----------------------------------------------------------------------------------------
            TCT_Lib.eCurLaneMode = LaneMode.NORMAL;
            TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToClient("START_SHIFT", null);
            SendLaneCurrentState();
        }

        public void EndShiftProcess()
        {
            LogInfo.WriteLaneEvent("+ Clear CRS, Clear TFI");
            TCT_Lib.webSocketLane.OutIo_ClearCRS();
            TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.CloseLane, (int)TFI_Pattern.Normal, "", "", "", szTFI_CloseLane, "");
            LogInfo.WriteLaneEvent("Send to IO - ASB : OFF, Siren : OFF, LTL : OFF");
            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.Siren, OFF);
            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ASB, OFF);
            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.LTL, OFF);
            //----------------------------------------------------------------------------------------
            TCT_Lib.iTotalQueue = 0;
            iQueueALB = 0;
            if (TCT_Lib.eCurLaneMode != LaneMode.MAINTENANCE)
            {
                LogInfo.WriteLaneEvent("+ Clear Staff Info");
                TCT_Lib.szStaffCode         = "";
                TCT_Lib.szStaffName         = "";
                TCT_Lib.szStaffID           = "";
                TCT_Lib.szStaffGroupID      = "";
                TCT_Lib.szStaffGroupName    = "";
                TCT_Lib.eCurLaneMode        = LaneMode.CLOSED;
            }

            TCT_Lib.bConvoyMode     = false;
            TCT_Lib.iConvoyClass1   = TCT_Lib.iTotalConvoyClass1 = 0;
            TCT_Lib.iConvoyClass2   = TCT_Lib.iTotalConvoyClass2 = 0;

            TCT_Lib.iConvoyPayTypeID = TCT_Lib.iConvoyEmvDetailID = 0;
            iStepNextCar    = 0;
            szPrevPAN       = "";
            SendLaneCurrentState();
            showQueueAlbControl(iQueueALB);
            TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToClient("END_SHIFT", null);

            SendEtcRabbitMq_CmdALB_Close();
            //if (TCT_Lib.iLaneConfigType == 2)
            if (TCT_Lib.iLaneConfigType >= 1)
                TCT_Lib.etcRabbitMQ.Send_SetLaneMode_ToETC(LaneMode.CLOSED, false, true);

            if (TCT_Lib.xListLoopCar.Count > 0)
                TCT_Lib.xListLoopCar.Clear();

            //--------------------------------------------------
            //----- Check Manual Import Path
            if (TCT_Lib.szManualImportPath.Trim() != "")
                TCT_Lib.ManualImportDataFile();
            //--------------------------------------------------

            TCT_Lib.LaneDB.CheckUpdateTariffEffective();
            TCT_Lib.LaneDB.LoadTariffData();


            if (TCT_Lib.eCurLaneType == LaneType.ETC)
                TCT_Lib.BackupLogFile();

            TCT_Lib.AutoResendEndJobToDC(TCT_Lib.iStaffShiftID);

            /*
             *          2021-02-02 Move action to close OTL
                        if (TCT_Lib.bEnableLSD)
                            TCT_Lib.lsdController.CmdShow_LaneClose();
            */
        }

        public void SendLaneCurrentTransStatistic()
        {
            object objParams = TCT_Lib.LaneDB.GetCurrentStatistic(TCT_Lib.iStaffShiftID);
            TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToClient("LANE_STATISTIC", objParams);
        }

        public void ClearQueueProcesss()
        {
            TCT_Lib.iTotalQueue = 0;
            clearQueueLaneControl();

            object objParams = new
            {
                Queue = TCT_Lib.iTotalQueue,
            };
            if (TCT_Lib.bAllowETC)
            {
                TCT_Lib.etcRabbitMQ.Send_ClearQueue_ToETC();
                //--- Send alarm to RTO
                int iAlarmID = (int)TCT_Lib.LaneDB.WriteLaneAlarmRTO(AlarmDevice.ETC_CLEARQUEUE, AlarmType.Occurred, $"Clear Queue (RTO) Last PAN No.={TCT_Lib.szEtcLastUpdateObu}, Last Update = {TCT_Lib.dtEtcLastUpdateObu.ToString("yyyy-MM-dd HH:mm:ss")}", $"Last PAN No.={TCT_Lib.szEtcLastUpdateObu}, Last Update = {TCT_Lib.dtEtcLastUpdateObu.ToString("yyyy-MM-dd HH:mm:ss")}");
                TCT_Lib.SendLaneAlarmObjToRTO(iAlarmID);
            }
            TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToClient("UPDATE_QUEUE", objParams);
            LogInfo.WriteLaneEvent($"+ ClearQueueProcesss : Last PAN No.={TCT_Lib.szEtcLastUpdateObu}, Last Update = {TCT_Lib.dtEtcLastUpdateObu.ToString("yyyy-MM-dd HH:mm:ss")}");
            LogInfo.WriteLog($"+ ClearQueueProcesss : Last PAN No.={TCT_Lib.szEtcLastUpdateObu}, Last Update = {TCT_Lib.dtEtcLastUpdateObu.ToString("yyyy-MM-dd HH:mm:ss")}");
        }

        public void EnableSecondaryReaderProcesss()
        {
            TCT_Lib.etcRabbitMQ.Send_EnableSecondaryReader_ToETC();
        }

        public void ChangeLaneType(int iLaneType)
        {
            if (TCT_Lib.eCurAppPage != AppPage.Login)
            {
                //--- Login Page
                LogInfo.WriteLog($"- Ignore change lane type to {TCT_Lib.GetLaneTypeName((LaneType)iLaneType)}. because current process in main page. (Not login page)");
                LogInfo.WriteLaneEvent($"- Ignore change lane type to {TCT_Lib.GetLaneTypeName((LaneType)iLaneType)}. because current process in main page. (Not login page)");
                return;
            }
            LaneType eOldLaneType = TCT_Lib.eCurLaneType;
            //-------------------------------------
            if (iLaneType == (int)LaneType.MTC)
                TCT_Lib.eCurLaneType = LaneType.MTC;
            else if (iLaneType == (int)LaneType.ETC)
                TCT_Lib.eCurLaneType = LaneType.ETC;
            else if (iLaneType == (int)LaneType.DUAL)
                TCT_Lib.eCurLaneType = LaneType.DUAL;
            else
                TCT_Lib.eCurLaneType = LaneType.MTC;

            TCT_Lib.WriteTCT_Configuration("Lane", "LaneType", iLaneType);
            changeCurrentLaneType();
            SendLaneCurrentState();

            //----------------
            int iAlarmID = (int)TCT_Lib.LaneDB.WriteLaneAlarmRTO(AlarmDevice.CHANGE_LANETYPE, AlarmType.Occurred, $"Change Lane Type (RTO). From {TCT_Lib.GetLaneTypeName(eOldLaneType)} => To {TCT_Lib.GetLaneTypeName(TCT_Lib.eCurLaneType)}", $"Change from RTO");
            TCT_Lib.SendLaneAlarmObjToRTO(iAlarmID);
        }

        public void ExitMaintenanceMode()
        {
            if (TCT_Lib.eCurAppPage != AppPage.Maintenance)
            {
                LogInfo.WriteLog("- Fail Exit Maintenance Moode : Current AppPage is not Maintenance Page");
                return;
            }

            exitMaintenanceMode();
            SendLaneCurrentState();
        }

        public void RemoteLoginFromRTO(string szStaffCode)
        {
            remoteLoginFromRTO(szStaffCode);
            SendLaneCurrentState();
        }

        public void RemoteLogoutFromRTO()
        {
            //--- Process Logout
            //--- Check current Staff is Supervisor or Chief Toll Collector
            if ((TCT_Lib.szStaffGroupID != "30") && (TCT_Lib.szStaffGroupID != "40"))
            {
                LogInfo.WriteLaneEvent("- Fail Remote Logout, because current staff is not Supervisor/Chief Toll Collector.");
                LogInfo.WriteLaneEvent($" + Current Staff is StaffID={TCT_Lib.szStaffID}, StaffCode={TCT_Lib.szStaffCode}, StaffName={TCT_Lib.szStaffName}, StaffGroupID={TCT_Lib.szStaffGroupID}, StaffGroupName={TCT_Lib.szStaffGroupName}");

                return;
            }

            //--- Auto Close OTL
            LogInfo.WriteLaneEvent("Auto OTL CLOSE from RTO command");
            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.OTL_RedGreen, OFF);
            TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.OTL, 0);
            TCT_Lib.SendOtlStatusObjToDC(false, TCT_Lib.iStaffShiftID);

            //------------------------------------------
            if (TCT_Lib.bEnableLSD)
            {
                TCT_Lib.eCurLsdMode = LsdMode.LSD_CLOSE;
                if (TCT_Lib.iLSD_KeepAliveSec > 0)
                {
                    if (TCT_Lib.iLSD_MethodType == 0)
                        TCT_Lib.lsdController.CmdShow_LaneClose();
                    else
                        TCT_Lib.webSocketLane.laneSocketServerRTO.SendChangeLsdToRTO(LsdMode.LSD_CLOSE);
                }
                else
                {
                    if (TCT_Lib.iLSD_MethodType == 0)
                    {
                        var lsdController = new LaneSocketControl_LSD();
                        lsdController.RunInitialLSD();
                        //lsdController.CmdShow_LaneClose();
                        //lsdController.CloseSocketConnection();
                    }
                    else
                        TCT_Lib.webSocketLane.laneSocketServerRTO.SendChangeLsdToRTO(TCT_Lib.eCurLsdMode);
                }
            }

            //--- Auto End Shift
            LogInfo.WriteLaneEvent("Auto END SHIFT from RTO command");
            string szErrMsg     = "";
            ClearTextCRS_EndJob();
            DateTime dtEndShift = TCT_Lib.LaneDB.WriteLaneEndStaffShift(TCT_Lib.iStaffShiftID, 0);
            TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.JOB, 0);
            TCT_Lib.webSocketLane.SendCollectorJob(0);
            TCT_Lib.webSocketLane.SendChangeSupervisor();
            TCT_Lib.SendEndJobObjToRTO(TCT_Lib.iStaffShiftID);
            TCT_Lib.SendEndJobObjToSupAdjust(TCT_Lib.iStaffShiftID);
            TCT_Lib.SendEndJobObjToDC(TCT_Lib.iStaffShiftID);
            TCT_Lib.webSocketLane.SendCollectorJob(0);              // Repeat send EOJ to BIS
            WebService_BeginEndJob.CallWebService_EndOfJob(dtEndShift, ref szErrMsg);
            EndShiftProcess();
            endShiftLaneControl();

            //---------------------------------
            SendLaneCurrentState();
        }

        public void SetCurrentSupervisor(dynamic objSupervisor)
        {
            LogInfo.WriteLog("+ SetCurrentSupervisor");
            if (objSupervisor == null)
            {
                LogInfo.WriteLog("- SetCurrentSupervisor : not found objSupervisor info");
                return;
            }

            if (objSupervisor["supervisorShiftID"] == null)
            {
                LogInfo.WriteLog("- SetCurrentSupervisor : not found objSupervisor detail");
                return;
            }

            try
            {
                int iSupervisorShiftID          = (int)objSupervisor["supervisorShiftID"];
                string szSupervisorID           = (string)objSupervisor["supervisorId"];
                string szSupervisorCode         = (string)objSupervisor["supervisorCode"];
                string szSupervisorName         = (string)objSupervisor["supervisorName"];
                string szSupervisorGroupID      = (string)objSupervisor["supervisorGroupId"];
                string szSupervisorGroupName    = (string)objSupervisor["supervisorGroupName"];
                DateTime dtSupervisorStartShift = (DateTime)objSupervisor["supervisorStartShift"];
                LogInfo.WriteLog("+ Update Current Supervisor in DB");
                TCT_Lib.LaneDB.UpdateCurrentSupervisor(iSupervisorShiftID, szSupervisorID, szSupervisorCode, szSupervisorName,
                                                    szSupervisorGroupID, szSupervisorGroupName, dtSupervisorStartShift);
                LogInfo.WriteLog("+ Update UI Current Supervisor");
                updateUiSupervisor();

                LogInfo.WriteLog("+ Send change supervisor to LC");
                if (TCT_Lib.webSocketLane != null)
                    TCT_Lib.webSocketLane.SendChangeSupervisor();

                LogInfo.WriteLog("+ Send change supervisor to ETC");
                if (TCT_Lib.etcRabbitMQ != null)
                    TCT_Lib.etcRabbitMQ.SendChangeLoginSupervisorStaffToETC();

                //---------------------------------------------------------
                if (TCT_Lib.eCurLaneType == LaneType.ETC)
                {
                    //--- Change Current Staff 
                    AutoChangeStaffOfAutoLoginETC();
                }
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- \tError SetCurrentSupervisor : " + err.Message);
            }
        }

        private void AutoChangeStaffOfAutoLoginETC()
        {
            //--- Auto Close current shift
            DateTime dtEndShift = TCT_Lib.LaneDB.WriteLaneEndStaffShift(TCT_Lib.iStaffShiftID, 1);
            TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.JOB, 0);
            TCT_Lib.webSocketLane.SendCollectorJob(0);
            TCT_Lib.SendEndJobObjToRTO(TCT_Lib.iStaffShiftID);
            TCT_Lib.SendEndJobObjToSupAdjust(TCT_Lib.iStaffShiftID);
            TCT_Lib.SendEndJobObjToDC(TCT_Lib.iStaffShiftID);
            TCT_Lib.webSocketLane.SendCollectorJob(0);              // Repeat send EOJ to BIS
            //--------------------------------------------------------------

            //--- Auto set new staff
            TCT_Lib.szStaffID           = TCT_Lib.szSupervisorID;
            TCT_Lib.szStaffCode         = TCT_Lib.szSupervisorCode;
            TCT_Lib.szStaffName         = TCT_Lib.szSupervisorName;
            TCT_Lib.szStaffGroupID      = TCT_Lib.szSupervisorGroupID;
            TCT_Lib.szStaffGroupName    = TCT_Lib.szSupervisorGroupName;

            //-- Auto Open of new staff
            LogInfo.WriteLaneEvent($"Auto Login with ETC Mode (When Change Sup) : StaffID={TCT_Lib.szStaffID}, StaffCode={TCT_Lib.szStaffCode}, StaffName={TCT_Lib.szStaffName}");
            TCT_Lib.nTrx                            = 1;
            TCT_Lib.laneController.iCapture_nTrx    = 0;
            // Start transaction of each JobNo
            // if ETC mode then JobNo will fix=0 (2021/03/17)
            TCT_Lib.iStaffShiftID                   = (int)TCT_Lib.LaneDB.GetStaffShiftID();
            TCT_Lib.iJobNo                          = (TCT_Lib.eCurLaneType == LaneType.ETC) ? 0 : TCT_Lib.LaneDB.GetStartJobNo();
            LogInfo.WriteLaneEvent($"+ Start Main Process => StartShiftID={TCT_Lib.iStaffShiftID}, JobNo={TCT_Lib.iJobNo}, nTrx={TCT_Lib.nTrx}");
            TCT_Lib.dtStaffStartShift               = DateTime.Now;
            TCT_Lib.LaneDB.WriteLaneStartStaffShift(LoginType.ETC_Auto);

            //----------------------------------------------------------------------------------
            Thread.Sleep(1500);
            TCT_Lib.webSocketLane.SendCollectorJob(1);
            TCT_Lib.webSocketLane.SendChangeSupervisor();
            TCT_Lib.SendLaneBeginJobObjToDC(TCT_Lib.iStaffShiftID);
            TCT_Lib.SendBeginJobObjToRTO(TCT_Lib.iStaffShiftID);
            TCT_Lib.SendBeginJobObjToSupAdjust(TCT_Lib.iStaffShiftID);
            TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.JOB, 1);
            TCT_Lib.webSocketLane.SendCollectorJob(1);                  // Repeat send BOJ to BIS
            TCT_Lib.LaneDB.WriteNewTransactionStatistic();

            updateUiStaff();
        }

        //--------------------------------------------------------------------------------------
        public void EtcRabbitMQ_LaneStateFromEtcRabbitMQ(dynamic objLaneState)
        {
            showModeType(bEditModeETC ? "MTC" : "ETC");
            if (bEditModeETC)
                return;

            if (TCT_Lib.eCurLaneType == LaneType.MTC)
                return;

            //-----------------------------------------------------------------------------
            if (TCT_Lib.eCurLaneType == LaneType.DUAL)
            {
                if (bEditBlockETC)
                    return;
            }
            //-----------------------------------------------------------------------------
            try
            {
                string szSeqNo = (objLaneState["header"]["sequenceNumber"] == null) ? "" : (string)objLaneState["header"]["sequenceNumber"];
                //LogInfo.WriteLog("+ sequenceNumber : " + szSeqNo);
                dynamic jValue = objLaneState["inputDevices"];
                if (jValue != null)
                {
                    JArray jResult  = jValue["values"];
                    int[] values    = jResult.ToObject<int[]>();
                    LaneSocket_DigitalInputLaneEvent(values, false);
                }
                else
                    LogInfo.WriteEtcRabbitMQ("+ Not found inputDevices");

                if (objLaneState["vehicleQueue"] != null)
                {
                    dynamic vehicleQueue    = objLaneState["vehicleQueue"];
                    JArray jVehicles        = vehicleQueue["vehicle"];
                    if (jVehicles != null)
                    {
                        TCT_Lib.iTotalQueue = jVehicles.Count;
                        if (jVehicles.Count > 0)
                        {
                            dynamic vehicleETC      = jVehicles[0];
                            string szPaymentStatus  = vehicleETC["paymentStatus"] ?? "";
                            if (szPaymentStatus == "OBU_NOT_PRESENT")
                            {
                                //--- Not found TAG
                                laneStateEtcPayCardPAN(vehicleETC);

                                //--- Send ETC LaneState to RTO
                                if (TCT_Lib.webSocketLane != null)
                                {
                                    if (TCT_Lib.webSocketLane.laneSocketServerRTO != null)
                                        TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToClient("ETC_TAGINFO", vehicleETC);
                                }

                                return;
                            }

                            TCT_Lib.iCurTC_Class            = 1;
                            TCT_Lib.xCurPayment.payAmount   = TCT_Lib.fFareClass1;
                            TCT_Lib.fCurFareAmount          = TCT_Lib.fFareClass1;
                            eCurVehicleType                 = VehicleType.CLASS_1;

                            vehicleETC["fareAmount"] = TCT_Lib.fFareClass1;

                            string szRejectReason = "", szPAN = "", szBalance = "";
                            bool bMCard         = false;
                            decimal fBalance    = 0;
                            TCT_Lib.xCurTAG     = new TAG_Info();
                            if (vehicleETC["obu"] != null)
                            {
                                szRejectReason  = vehicleETC["obu"]["rejectReason"] ?? "";
                                szPAN           = vehicleETC["obu"]["paymentMeans"]["pan"] ?? "";
                                bMCard          = szPAN.StartsWith("636685", StringComparison.Ordinal);
                                szBalance       = vehicleETC["obu"]["balanceObu"];
                                fBalance        = szBalance.Length > 2 ? decimal.Parse(szBalance.Remove(szBalance.Length - 2)) : 0;
                                if (szPrevPAN == "")
                                {
                                    szPrevPAN = szPAN;
                                    //TCT_Lib.iTotalQueue++;
                                }

                                if (szPAN != "")
                                {
                                    TCT_Lib.szEtcLastUpdateObu = szPAN;
                                    TCT_Lib.dtEtcLastUpdateObu = DateTime.Now;
                                }

                                int iVehicleClass = vehicleETC["obu"]["vehicleClass"] ?? 0;
                                if (iVehicleClass == 9)      // Exempt Vehicle
                                    TCT_Lib.fCurFareAmount = 0;

                                TCT_Lib.xCurPayment.payType     = bMCard ? (int)PayType.MPass : (int)PayType.EasyPass;
                                TCT_Lib.xCurPayment.payTypeName = TCT_Lib.GetPayMethodName(bMCard ? (int)PayType.MPass : (int)PayType.EasyPass);
                                TCT_Lib.xCurPayment.payDateTime = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                                TCT_Lib.xCurTAG.tagPAN          = szPAN;
                                TCT_Lib.xCurTAG.tagBalance      = fBalance;
                                TCT_Lib.xCurTAG.tagType         = bMCard ? 0 : 1;
                                eCurPayType                     = bMCard ? PayType.MPass : PayType.EasyPass;
                            }
                            else
                            {
                                TCT_Lib.xCurPayment.payType     = (int)PayType.NONE;
                                TCT_Lib.xCurPayment.payTypeName = "";
                                TCT_Lib.xCurPayment.payDateTime = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);
                                TCT_Lib.xCurTAG.tagPAN          = "";
                                eCurPayType                     = PayType.NONE;
                            }

                            if (TCT_Lib.eCurLaneMode != LaneMode.HPMC)
                                TCT_Lib.bEtcCtrlIO = true;
                            //----------------------------------------------------
                            laneStateEtcPayCardPAN(vehicleETC);

                            //--- Send ETC LaneState to RTO
                            if (TCT_Lib.webSocketLane != null)
                            {
                                if (TCT_Lib.webSocketLane.laneSocketServerRTO != null)
                                    TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToClient("ETC_TAGINFO", vehicleETC);
                            }
                        }
                    }
                    updateUiQueueLaneControl();
                }
                else
                    LogInfo.WriteEtcRabbitMQ("+ Not found Vehicle Queue");

                //bool bResult = TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("ETC_LANESTATE", objLaneState);

            }
            catch (Exception err)
            {
                Console.WriteLine("- Error EtcRabbitMQ_LaneStateFromEtcRabbitMQ : " + err.Message);
                LogInfo.WriteLog("- Error EtcRabbitMQ_LaneStateFromEtcRabbitMQ : " + err.Message + "\r\n" + err.StackTrace);

                string szJsonLaneState = JsonConvert.SerializeObject(objLaneState, Formatting.Indented);
                LogInfo.WriteLog("- ETC ObjLaneState : " + szJsonLaneState);
            }
        }

        void EtcRabbitMQ_CompiledPassageFromEtcRabbitMQ(dynamic objCompiledPassage)
        {
            if (TCT_Lib.eCurLaneType == LaneType.MTC)
                return;

            if (!TCT_Lib.bMainUiReady)
                return;

            int iTransID        = (int)TCT_Lib.LaneDB.WriteLaneTransaction_ETC_CompliedPassage(objCompiledPassage);
            try
            {
                dynamic objParams   = objCompiledPassage["params"];
                compilePassageETC(objParams);
                bool bResult        = TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("ETC_COMPILED_PASSAGE", objCompiledPassage);

                //--- Send Lane Transaction to RabbitMQ
                if (iTransID != 0)
                    TCT_Lib.SendLaneTransObjToRTO(iTransID);
                //TCT_Lib.SendLaneTransObjToSCW(iTransID);

                //--- Update TotalSumVehicle
                long seqNo = (objCompiledPassage["params"]["sequenceNumber"] == null) ? 0 : (long)objCompiledPassage["params"]["sequenceNumber"];
                if (objCompiledPassage["params"]["obuTransaction"] != null)
                {
                    //TCT_Lib.LaneDB.UpdateTransStatistic_Transaction();    // Not support ETC
                    TCT_Lib.LaneDB.UpdateTransStatistic_AVC(TCT_Lib.xCurAVC.avcClass);
                    int iTotalSum = TCT_Lib.LaneDB.UpdateTransStatistic_TotalSum();

                    //long seqNo = (long)objCompiledPassage["params"]["sequenceNumber"];
                    LogInfo.WriteLog($"++ TotalSum Vehicle : {iTotalSum}, LastTrans (ETC) = {iTransID}, SeqNo={seqNo}");
                    LogInfo.WriteEtcRabbitMQ("+ Params -> obuTransaction :  sequenceNumber=" + seqNo);
                    SendLaneCurrentTransStatistic();
                }

                //--------------------------------------------------------------
                //--- Add this feature when 2021-03-18
                if (TCT_Lib.eCurLaneType == LaneType.DUAL)
                {
                    string szSignalCode = (objCompiledPassage["params"]["signalCode"] == null) ? "" : (string)objCompiledPassage["params"]["signalCode"];
                    if (TCT_Lib.eCurLaneMode == LaneMode.NORMAL)
                    {
                        if (szSignalCode == "22")
                        {
                            //--- Send Direct to Siren and Becon
                            LogInfo.WriteLaneEvent("IO - Siren : " + (bInEFS ? "ON" : "OFF") + ", ASB : " + (bInEFS ? "ON" : "OFF"));
                            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.Siren, ON);
                            TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.ASB, ON);

                            uint iSirenTimerId = GLib.Timeout.Add((uint)TCT_Lib.iStopSirenAfterTimeSec * 1000, new GLib.TimeoutHandler(Perform_StopSiren));
                            AddSirenTimer(iSirenTimerId);
                        }
                    }
                }
                show_nTrxControl(TCT_Lib.nTrx);
                //--------------------------------------------------------------------
                //--- Check Reverse Direction
                string direction = objParams["direction"] ?? "";
                if (direction == "REVERSE_DIRECTION")
                {
                    LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.EDIT);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error EtcRabbitMQ_CompiledPassageFromEtcRabbitMQ : " + err.Message);
                LogInfo.WriteLog("- Error EtcRabbitMQ_CompiledPassageFromEtcRabbitMQ : " + err.Message + "\r\n" + err.StackTrace);

                string szJsonCompiledPassage = JsonConvert.SerializeObject(objCompiledPassage, Formatting.Indented);
                LogInfo.WriteLog("- ETC CompiledPassage : " + szJsonCompiledPassage);
            }

            //-------------------------------------------------------
            /*
                        if (objCompiledPassage["params"]["signalCode"] != null)
                        {
                            string szSignalCode = (string)objCompiledPassage["params"]["signalCode"];
                            if (szSignalCode != "")
                                bBlockTAG = true;
                        }
            */
        }

        void LaneSocketServerRTO_EtcChangeLaneMode(int iEtcLaneMode, int iEtcLaneState)
        {
            if (iEtcLaneMode == (int)LaneMode.CLOSED)
            {
                if (TCT_Lib.eCurAppPage != AppPage.Main)
                {
                    LogInfo.WriteLog("- Fail ETC Change Lane Mode : Current AppPage is not Main Page");
                    return;
                }
                //------------------------------------------------
                //--- Auto Close OTL
                //TCT_Lib.webSocketLane.SendDigitalOutput_Controller((int)Cmd_DO_Type.OTL_RedGreen, OFF);
                //TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.OTL, 0);
                TCT_Lib.etcRabbitMQ.Send_ControlOutputDevice_ToETC("OverheadTrafficLight", false);
                TCT_Lib.etcRabbitMQ.Send_SetLaneMode_ToETC(LaneMode.CLOSED);
                LogInfo.WriteLog("+Recv TCT Change Lane Mode : SetLaneMode to CLOSED");

                //--- Auto End Shift
                string szErrMsg     = "";
                DateTime dtEndShift = TCT_Lib.LaneDB.WriteLaneEndStaffShift(TCT_Lib.iStaffShiftID, 0);
                TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.JOB, 0);
                TCT_Lib.webSocketLane.SendCollectorJob(0);
                TCT_Lib.webSocketLane.SendChangeSupervisor();
                TCT_Lib.SendEndJobObjToRTO(TCT_Lib.iStaffShiftID);
                TCT_Lib.SendEndJobObjToSupAdjust(TCT_Lib.iStaffShiftID);
                TCT_Lib.SendEndJobObjToDC(TCT_Lib.iStaffShiftID);
                WebService_BeginEndJob.CallWebService_EndOfJob(dtEndShift, ref szErrMsg);
                EndShiftProcess();
                endShiftLaneControl();
            }
        }

        //==================================================================
        //=== Thread Process
        protected void InitialCheckNetworkStatusThread()
        {
            chkNetworkThread                = new Thread(new ThreadStart(ThreadCheckNetworkStatus));
            chkNetworkThread.Priority       = ThreadPriority.AboveNormal;
            chkNetworkThread.IsBackground   = true;
            chkNetworkThread.Start();
        }

        async private void ThreadCheckNetworkStatus()
        {
            int iCounting = 0;
            while (true)
            {
                try
                {
                    //if (!TCT_Lib.bMaintenanceMode)
                    if ((!TCT_Lib.bMaintenanceMode) || (TCT_Lib.eCurAppPage == AppPage.Main))
                    {
                        if (checkNetworkStatus != null)
                        {
                            //--- Check Lane Controller Connection
                            //if (TestNetworkConnection(TCT_Lib.szLCSD_SocketIPAddress, TCT_Lib.iLCSD_SocketPort))
                            if (await IsPortReachable(TCT_Lib.szLCSD_SocketIPAddress, TCT_Lib.iLCSD_SocketPort))
                            {
                                Console.WriteLine("+ Lane Controller Connection is SUCCESS");
                                checkNetworkStatus((int)ChkDeviceType.Device_LC, true);
                            }
                            else
                            {
                                Console.WriteLine($"- Fail Lane Controller Connection : LC IPAddress = {TCT_Lib.szLCSD_SocketIPAddress}, LC Port = {TCT_Lib.iLCSD_SocketPort}");
                                checkNetworkStatus((int)ChkDeviceType.Device_LC, false);
                            }
                            //--- Check RTO Connection
                            //if (TestNetworkConnection(TCT_Lib.szRTO_IPAddress, TCT_Lib.iRTO_SocketPort - 1))
                            if (await IsPortReachable(TCT_Lib.szRTO_IPAddress, TCT_Lib.iRTO_SocketPort - 1))
                            {
                                Console.WriteLine("+ RTO Connection is SUCCESS");
                                checkNetworkStatus((int)ChkDeviceType.Device_RTO, true);
                            }
                            else
                            {
                                Console.WriteLine($"- Fail RTO Connection : RTO IPAddress = {TCT_Lib.szRTO_IPAddress}, RTO Port = {TCT_Lib.iRTO_SocketPort - 1}");
                                checkNetworkStatus((int)ChkDeviceType.Device_RTO, false);
                            }

                            //--- Check DC RabbitMQ Connection
                            if (TCT_Lib.bEnableDcRabbitMQ)
                            {
                                bool bLastNwStatusDC = TCT_Lib.bDC_NetworkStatus;
                                if (TestNetworkConnection(TCT_Lib.szDC_RabbitMQ_ServerIPAddress, TCT_Lib.iDC_RabbitMQ_ServerPort, 2 * 1000))
                                //if (await IsPortReachable(TCT_Lib.szDC_RabbitMQ_ServerIPAddress, TCT_Lib.iDC_RabbitMQ_ServerPort))
                                {
                                    if (!bLastNwStatusDC)
                                        LogInfo.WriteLog("+ Success DC => DC Network Status change from Dis-connect to Connect");

                                    TCT_Lib.bDC_NetworkStatus = true;
                                    Console.WriteLine("+ DC RabbitMQ Connection is SUCCESS");
                                    checkNetworkStatus((int)ChkDeviceType.Device_DC, true);
                                }
                                else
                                {
                                    if (bLastNwStatusDC)
                                        LogInfo.WriteLog("- Fail DC => DC Network Status change from Connect to Dis-connect");
                                    TCT_Lib.bDC_NetworkStatus = false;
                                    Console.WriteLine($"- Fail DC RabbitMQ Connection : DC IPAddress = {TCT_Lib.szDC_RabbitMQ_ServerIPAddress}, DC Port = {TCT_Lib.iDC_RabbitMQ_ServerPort}");
                                    checkNetworkStatus((int)ChkDeviceType.Device_DC, false);
                                }
                            }

/*
                            //--- Check EDC Connection
                            if (TCT_Lib.bEnableEdc)
                            {
                                //if (TestNetworkConnection(TCT_Lib.szEdcIPAddress, TCT_Lib.iEdcSendPort))

                                if (TCT_Lib.eCurAppPage != AppPage.EMV)
                                {
                                    if (TCT_Lib.iEdcChkNetworkType == 0)
                                    {
                                        if (iCounting % 3 == 0)     // Check every 30 sec.
                                        {
                                            if (await IsPortReachable(TCT_Lib.szEdcIPAddress, TCT_Lib.iEdcSendPort))
                                            {
                                                Console.WriteLine("+ EDC Connection is SUCCESS");
                                                checkNetworkStatus((int)ChkDeviceType.Device_EMV, true);
                                            }
                                            else
                                            {
                                                Console.WriteLine($"- Fail EDC Connection : EDC IPAddress = {TCT_Lib.szEdcIPAddress}, EDC Port = {TCT_Lib.iEdcSendPort}");
                                                checkNetworkStatus((int)ChkDeviceType.Device_EMV, false);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (iCounting % 6 == 0)     // Check every 60 sec.
                                        {
                                            if (await CheckEdcHankShaking())
                                            {
                                                Console.WriteLine("+ EDC Connection is SUCCESS");
                                                checkNetworkStatus((int)ChkDeviceType.Device_EMV, true);
                                            }
                                            else
                                            {
                                                Console.WriteLine($"- Fail EDC Connection : EDC IPAddress = {TCT_Lib.szEdcIPAddress}, EDC Port = {TCT_Lib.iEdcSendPort}");
                                                checkNetworkStatus((int)ChkDeviceType.Device_EMV, false);
                                            }
                                        }
                                    }
                                }
                            }
*/
                            //-------------------------------------
                            TCT_Lib.bSupAdjust_Connected    = TCT_Lib.bEnableSupAdjust ? await IsPortReachable(TCT_Lib.szSupAdjust_IPAddress, TCT_Lib.iSupAdjust_SocketPort) : false;

                            //--- Check Device Monitoring
                            //TCT_Lib.bEMV_Connected          = TCT_Lib.bEnableEdc ? TestNetworkConnection(TCT_Lib.szEdcIPAddress, TCT_Lib.iEdcSendPort) : false;
                            //TCT_Lib.bMainAntenna_Connected  = TCT_Lib.bEnableAntenna ? TestNetworkConnection(TCT_Lib.szMainAntenna_IPAddress, 80) : false;
                            //TCT_Lib.bSecAntenna_Connected   = TCT_Lib.bEnableAntenna ? TestNetworkConnection(TCT_Lib.szSecondaryAntenna_IPAddress, 80) : false;
                            //TCT_Lib.bLSD_Connected          = TCT_Lib.bEnableLSD ? TestNetworkConnection(TCT_Lib.szLSD_IPAddress, TCT_Lib.iLSD_SocketPort) : false;
                            //TCT_Lib.bLCSD_Connected         = TestNetworkConnection(TCT_Lib.szLCSD_SocketIPAddress, TCT_Lib.iLCSD_SocketPort);
                            //TCT_Lib.bLCIO_Connected         = TestNetworkConnection(TCT_Lib.szLCIO_SocketIPAddress, TCT_Lib.iLCIO_SocketPort);

                            TCT_Lib.bEMV_Connected          = TCT_Lib.bEnableEdc ? await IsPortReachable(TCT_Lib.szEdcIPAddress, TCT_Lib.iEdcSendPort) : false;
                            TCT_Lib.bMainAntenna_Connected  = TCT_Lib.bEnableAntenna ? await IsPortReachable(TCT_Lib.szMainAntenna_IPAddress, 80) : false;
                            TCT_Lib.bSecAntenna_Connected   = TCT_Lib.bEnableAntenna ? await IsPortReachable(TCT_Lib.szSecondaryAntenna_IPAddress, 80) : false;
                            TCT_Lib.bLSD_Connected          = TCT_Lib.bEnableLSD ? await IsPortReachable(TCT_Lib.szLSD_IPAddress, TCT_Lib.iLSD_SocketPort) : false;
                            TCT_Lib.bLCSD_Connected         = await IsPortReachable(TCT_Lib.szLCSD_SocketIPAddress, TCT_Lib.iLCSD_SocketPort);
                            TCT_Lib.bLCIO_Connected         = await IsPortReachable(TCT_Lib.szLCIO_SocketIPAddress, TCT_Lib.iLCIO_SocketPort);

                            SendLaneRealTimeState();
                        }
                    }

                    //Thread.Sleep(10 * 1000);
                    await Task.Delay(10 * 1000);
                }
                catch (Exception err)
                {
                    Console.WriteLine("- Error ThreadCheckNetworkStatus : " + err.Message);
                    //Thread.Sleep(5 * 1000);
                    await Task.Delay(10 * 1000);
                }
                iCounting++;
            }

            chkNetworkThread.Abort();
        }

        async public Task<bool> CheckEdcHankShaking()
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

        //=== Thread Process
        protected void InitialCheckNetworkEdcStatusThread()
        {
            chkNetworkEdcThread                 = new Thread(new ThreadStart(ThreadCheckNetworkEdcStatus));
            chkNetworkEdcThread.Priority        = ThreadPriority.AboveNormal;
            chkNetworkEdcThread.IsBackground    = true;
            chkNetworkEdcThread.Start();
        }

        async private void ThreadCheckNetworkEdcStatus()
        {
            bool bFirstTime = true;
            while (true)
            {
                try
                {
                    //if (!TCT_Lib.bMaintenanceMode)
                    if ((!TCT_Lib.bMaintenanceMode) || (TCT_Lib.eCurAppPage == AppPage.Main))
                    {
                        if (checkNetworkStatus != null)
                        {
                            //--- Check EDC Connection
                            if (TCT_Lib.bEnableEdc)
                            {
                                bFirstTime = false;
                                if (TCT_Lib.eCurAppPage != AppPage.EMV)
                                {
                                    if (TCT_Lib.iEdcChkNetworkType == 0)
                                    {
                                        LogInfo.WriteLogEDC("---------------------------------");
                                        LogInfo.WriteLogEDC("+ Start Check EDC Status (Type 0)");
                                        if (await IsPortReachable(TCT_Lib.szEdcIPAddress, TCT_Lib.iEdcSendPort))
                                        {
                                            Console.WriteLine("+ EDC Connection is SUCCESS");
                                            LogInfo.WriteLogEDC("+ EDC Connection is SUCCESS (Type 0)");
                                            checkNetworkStatus((int)ChkDeviceType.Device_EMV, true);
                                        }
                                        else
                                        {
                                            Console.WriteLine($"- Fail EDC Connection : EDC IPAddress = {TCT_Lib.szEdcIPAddress}, EDC Port = {TCT_Lib.iEdcSendPort}");
                                            LogInfo.WriteLogEDC($"- Fail EDC Connection : EDC IPAddress = {TCT_Lib.szEdcIPAddress}, EDC Port = {TCT_Lib.iEdcSendPort} (Type 0)");
                                            checkNetworkStatus((int)ChkDeviceType.Device_EMV, false);
                                        }
                                        LogInfo.WriteLogEDC("---------------------------------");
                                        LogInfo.WriteLogEDC("");
                                    }
                                    else
                                    {
                                        LogInfo.WriteLogEDC("---------------------------------");
                                        LogInfo.WriteLogEDC("+ Start Check EDC Status (Type 1)");
                                        if (await CheckEdcHankShaking())
                                        {
                                            Console.WriteLine("+ EDC Connection is SUCCESS");
                                            LogInfo.WriteLogEDC("+ EDC Connection is SUCCESS (Type 1)");
                                            checkNetworkStatus((int)ChkDeviceType.Device_EMV, true);
                                        }
                                        else
                                        {
                                            Console.WriteLine($"- Fail EDC Connection : EDC IPAddress = {TCT_Lib.szEdcIPAddress}, EDC Port = {TCT_Lib.iEdcSendPort}");
                                            LogInfo.WriteLogEDC($"- Fail EDC Connection : EDC IPAddress = {TCT_Lib.szEdcIPAddress}, EDC Port = {TCT_Lib.iEdcSendPort} (Type 1)");
                                            checkNetworkStatus((int)ChkDeviceType.Device_EMV, false);
                                        }
                                        LogInfo.WriteLogEDC("---------------------------------");
                                        LogInfo.WriteLogEDC("");
                                    }
                                }
                            }
                        }
                    }
                    if (bFirstTime)
                        await Task.Delay(10 * 1000);
                    else
                        await Task.Delay(TCT_Lib.iEdcChkKeepAliveIntervalMin * 60 * 1000);
                }
                catch (Exception err)
                {
                    Console.WriteLine("- Error ThreadCheckNetworkEdcStatus : " + err.Message);
                    await Task.Delay(TCT_Lib.iEdcChkKeepAliveIntervalMin * 60 * 1000);
                }
            }
        }


        //==================================================================================================================================
        public bool TestNetworkConnection(string szIPAddress, int iPort = 80, int timeoutMs = 300)
        {

            //return true;
/*
#if __SIM__
            //return true;
#endif
*/
            if (szIPAddress == "")
            {
                Console.WriteLine("- Fail TestNetworkConnection : No IPAddress");
                return false;
            }
            try
            {
                var client = new TcpClient();
                //if (client.ConnectAsync(szIPAddress, iPort).Wait((int)(0.3*1000)))
                if (client.ConnectAsync(szIPAddress, iPort).Wait(timeoutMs))
                {
                    client.Close();
                    client = null;
                    return true;
                }
                else
                {
                    client.Close();
                    client = null;
                    return false;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Fail TestNetworkConnection : " + err.Message);
                return false;
            }
        }

        public bool TestNetworkConnectionAsync(string szIPAddress, int iPort = 80, int timeoutMs = 300)
        {
            if (szIPAddress == "")
            {
                Console.WriteLine("- Fail TestNetworkConnection : No IPAddress");
                return false;
            }
            try
            {
                var client = new TcpClient();
                var result = client.BeginConnect(szIPAddress, iPort, null, null);
                result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeoutMs));
                if (client.Connected)
                {
                    client.Close();
                    client = null;
                    return true;
                }
                else
                {
                    client.Close();
                    client = null;
                    return false;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Fail TestNetworkConnection : " + err.Message);
                return false;
            }
        }

        public async Task<bool> IsPortReachable(string host, int port = 80, int msTimeout = 300)
        {
            if (string.IsNullOrEmpty(host))
                return false;
            //throw new ArgumentNullException("host");

            return await Task.Run(() =>
            {
                var clientDone  = new ManualResetEvent(false);
                var reachable   = false;
                var hostEntry   = new DnsEndPoint(host, port);
                using (var socket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    var socketEventArg = new SocketAsyncEventArgs { RemoteEndPoint = hostEntry };
                    socketEventArg.Completed += (s, e) =>
                    {
                        reachable = e.SocketError == SocketError.Success;
                        clientDone.Set();
                    };

                    clientDone.Reset();
                    socket.ConnectAsync(socketEventArg);
                    clientDone.WaitOne(msTimeout);

                    return reachable;
                }
            });
        }

        public bool CheckConnectivity(string ipAddress)
        {
            bool connectionExists = false;
            try
            {
                Ping pingSender         = new Ping();
                PingOptions options     = new PingOptions();
                options.DontFragment    = true;
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    PingReply reply     = pingSender.Send(ipAddress);
                    connectionExists    = reply.Status == IPStatus.Success ? true : false;
                }
            }
            catch (PingException err)
            {
                LogInfo.WriteLog($"- Error Ping CheckConnectivity : IP={ipAddress}, Error={err.Message}");
            }
            return connectionExists;
        }

        public async Task<bool> CheckAsyncPing(string ipAddress, int msTimeout = 500)
        {
            bool connectionExists = false;
            try
            {
                Ping pingSender = new Ping();
                await Task.Run(() =>
                {
                    PingReply reply     = pingSender.Send(ipAddress, msTimeout);
                    connectionExists    = reply.Status == IPStatus.Success ? true : false;
                    return connectionExists;
                });
                return connectionExists;
            }
            catch (PingException err)
            {
                LogInfo.WriteLog($"- Error Ping CheckConnectivity : IP={ipAddress}, Error={err.Message}");
                return false;
            }
        }


        //==================================================================
        protected void InitialAutoResendTransThread()
        {
            autoResendTransThread               = new Thread(new ThreadStart(ThreadAutoResendTrans));
            autoResendTransThread.Priority      = ThreadPriority.Normal;
            autoResendTransThread.IsBackground  = true;
            autoResendTransThread.Start();
        }

        async void ThreadAutoResendTrans()
        {
            Thread.Sleep(60 * 1000);
            while (true)
            {
                try
                {
                    LogInfo.WriteLog("+--- Start Thread AutoResendTrans()");
                    //--- Resend data to DC (Data Center)
                    if (TCT_Lib.bEnableDcRabbitMQ)
                    {
                        if (TestNetworkConnection(TCT_Lib.szDC_RabbitMQ_ServerIPAddress, TCT_Lib.iDC_RabbitMQ_ServerPort, TCT_Lib.iDC_RabbitMQ_TimeoutSec * 1000))
                        //if (await IsPortReachable(TCT_Lib.szDC_RabbitMQ_ServerIPAddress, TCT_Lib.iDC_RabbitMQ_ServerPort))
                        {
                            bool bResult = await TCT_Lib.AutoResendLaneTransactionToDC();
                            if (bResult)
                            {
                                bResult = await TCT_Lib.AutoResendLaneEventToDC();
                                if (bResult)
                                {
                                    bResult = await TCT_Lib.AutoResendBeginJobToDC(TCT_Lib.iStaffShiftID);
                                    //if (bResult)
                                    //    await TCT_Lib.AutoResendEndJobToDC(TCT_Lib.iStaffShiftID);
                                }
                            }
                        }
                        else
                        {
                            LogInfo.WriteLog("- Auto Resend to DC : Fail Connect to DC.");
                            LogInfo.WriteLog($"- Fail TestNetworkConnection to RabbitMQ => Connect DC Rabbit MQ : IP = {TCT_Lib.szDC_RabbitMQ_ServerIPAddress}, PORT = {TCT_Lib.iDC_RabbitMQ_ServerPort}");
                            TCT_Lib.bDC_NetworkStatus = false;
                        }
                    }

                    //--- Resend data to RTO
                    //if (TestNetworkConnection(TCT_Lib.szRTO_IPAddress, TCT_Lib.iRTO_SocketPort - 1))
                    {
                        bool bResult = await TCT_Lib.AutoResendLaneTransactionToRTO();
                        if (bResult)
                        {
                            bResult = await TCT_Lib.AutoResendBeginJobToRTO();
                            if (bResult)
                                await TCT_Lib.AutoResendEndJobToRTO();
                        }
                    }
                    /*
                                        else
                                        {
                                            LogInfo.WriteLog("- Auto Resend to RTO : Fail Connect to RTO.IP=" + TCT_Lib.szRTO_IPAddress + ", Port=" + (TCT_Lib.iRTO_SocketPort - 1));
                                        }
                    */
                    //--- Resend data to SupAdjust
                    //if (TestNetworkConnection(TCT_Lib.szSupAdjust_IPAddress, TCT_Lib.iSupAdjust_SocketPort))
                    {
                        //LogInfo.WriteLog("+ Start Auto Resend to SupAdjust");
                        bool bResult = await TCT_Lib.AutoResendLaneTransactionToSupAdjust();
                        if (bResult)
                        {
                            bResult = await TCT_Lib.AutoResendBeginJobToSupAdjust();
                            if (bResult)
                                await TCT_Lib.AutoResendEndJobToSupAdjust();
                        }
                    }
                    /*
                                        else
                                        {
                                            LogInfo.WriteLog("- Auto Resend to SupAdjust : Fail Connect to SupAdjust. IP="+ TCT_Lib.szSupAdjust_IPAddress+", Port="+ TCT_Lib.iSupAdjust_SocketPort);
                                        }
                    */

                    //--- Resend CaptureImageCamera
                    if (TCT_Lib.iVideoNVR_CaptureImageMethod >= 1)
                    {
                        LogInfo.WriteLog("+ Start Auto Resend to CaptureImageCamera");
                        bool bResult = await TCT_Lib.AutoResendImageCaptureCamera();
                    }


                    //---------------------------------------------------------------
                    LogInfo.WriteLog("+--- End Thread AutoResendTrans()");
                    Thread.Sleep(TCT_Lib.iTimerAutoResendTrans * 1000);
                }
                catch (Exception err)
                {
                    Console.WriteLine("- Error ThreadAutoResendTrans : " + err.Message);
                    Thread.Sleep(TCT_Lib.iTimerAutoResendTrans * 1000);
                }
            }
        }

        //==================================================================
        protected void InitialChkDeviceStatusThread()
        {
            chkDeviceStatusThread           = new Thread(new ThreadStart(ThreadChkDeviceStatus));
            chkDeviceStatusThread.Priority  = ThreadPriority.AboveNormal;
            chkDeviceStatusThread.IsBackground = true;
            chkDeviceStatusThread.Start();
        }

        async private void ThreadChkDeviceStatus()
        {
            while (true)
            {
                try
                {
                    LogInfo.WriteLog("+ Start ThreadChkDeviceStatus()");
                    //--- Check EDC Status
                    if (TCT_Lib.bEnableEdc)
                    {
                        //if (TestNetworkConnection(TCT_Lib.szEdcIPAddress, TCT_Lib.iEdcSendPort))
                        if (await IsPortReachable(TCT_Lib.szEdcIPAddress, TCT_Lib.iEdcSendPort))
                        {
                            LogInfo.WriteLog($"+ Sucess Connect EDC. IP: {TCT_Lib.szEdcIPAddress}, Port: {TCT_Lib.iEdcSendPort}");
                            //TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.EDC, 1);
                        }
                        else
                        {
                            LogInfo.WriteLog($"- Fail Connect EDC. IP: {TCT_Lib.szEdcIPAddress}, Port: {TCT_Lib.iEdcSendPort}");
                            //TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.EDC, 0);
                        }
                    }


                    //--- Check LCSD Status
                    if (TCT_Lib.bEnableLSD)
                    {
                        if (TestNetworkConnection(TCT_Lib.szLSD_IPAddress, TCT_Lib.iLSD_SocketPort))
                        //if (await IsPortReachable(TCT_Lib.szLSD_IPAddress, TCT_Lib.iLSD_SocketPort))
                        {
                            LogInfo.WriteLog($"+ Sucess Connect LSD. IP: {TCT_Lib.szLSD_IPAddress}, Port: {TCT_Lib.iLSD_SocketPort}");
                            //TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.LSD, 1);
                        }
                        else
                        {
                            LogInfo.WriteLog($"- Fail Connect LSD. IP: {TCT_Lib.szLSD_IPAddress}, Port: {TCT_Lib.iLSD_SocketPort}");
                            //TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.LSD, 0);
                        }
                    }

                    //--- Check Main Antenna
                    if (TCT_Lib.bEnableAntenna)
                    {
                        //if (TestNetworkConnection(TCT_Lib.szMainAntenna_IPAddress, TCT_Lib.iMainAntenna_SocketPort))
                        if (await IsPortReachable(TCT_Lib.szMainAntenna_IPAddress, TCT_Lib.iMainAntenna_SocketPort))
                        {
                            LogInfo.WriteLog($"+ Sucess Connect Main Antenna. IP: {TCT_Lib.szMainAntenna_IPAddress}, Port: {TCT_Lib.iMainAntenna_SocketPort}");
                            //TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.MAIN_ANTENNA, 1);
                        }
                        else
                        {
                            LogInfo.WriteLog($"- Fail Connect Main Antenna. IP: {TCT_Lib.szMainAntenna_IPAddress}, Port: {TCT_Lib.iMainAntenna_SocketPort}");
                            //TCT_Lib.LaneDB.WriteLaneEventHQ(EventTypeHQ.MAIN_ANTENNA, 0);
                        }
                    }

                    LogInfo.WriteLog("+ End ThreadChkDeviceStatus()");
                    //Thread.Sleep(TCT_Lib.iTimerSecCheckDeviceStatus * 1000);
                    await Task.Delay(TCT_Lib.iTimerSecCheckDeviceStatus * 1000);
                }
                catch (Exception err)
                {
                    Console.WriteLine("- Error ThreadChkDeviceStatus : " + err.Message);
                    //Thread.Sleep(TCT_Lib.iTimerSecCheckDeviceStatus * 1000);
                    await Task.Delay(TCT_Lib.iTimerSecCheckDeviceStatus * 1000);
                }
            }
        }

        protected void InitialTfiTransferFileThread()
        {
            tfiTransferThread               = new Thread(new ThreadStart(ThreadTfiTransferFile));
            tfiTransferThread.Priority      = ThreadPriority.AboveNormal;
            tfiTransferThread.IsBackground  = true;
            tfiTransferThread.Start();
        }

        async private void ThreadTfiTransferFile()
        {
            while (true)
            {
                try
                {
                    LogInfo.WriteLog("+ Start ThreadTfiTransferFle()");
                    bool bResult = await TCT_Lib.LaneDB.TfiTransferImageFile();

                }
                catch (Exception err)
                {
                    Console.WriteLine("- Error ThreadTfiTransferFile : " + err.Message);
                    //Thread.Sleep(TCT_Lib.iTimerSecCheckDeviceStatus * 1000);
                }
                await Task.Delay(180 * 1000);
            }
        }


        //=====================================================================
        protected void CreateMonitorCameraNetworkThread()
        {
            if (TCT_Lib.szVideoLane_IPCamera1.Trim() != "")
            {
                chkNetworkCamera1Thread = new Thread(new ThreadStart(ThreadChkNetworkCamera1));
                chkNetworkCamera1Thread.Priority = ThreadPriority.Normal;
                chkNetworkCamera1Thread.IsBackground = true;
                chkNetworkCamera1Thread.Start();
            }
            if (TCT_Lib.szVideoLane_IPCamera2.Trim() != "")
            {
                chkNetworkCamera2Thread = new Thread(new ThreadStart(ThreadChkNetworkCamera2));
                chkNetworkCamera2Thread.Priority = ThreadPriority.Normal;
                chkNetworkCamera2Thread.IsBackground = true;
                chkNetworkCamera2Thread.Start();
            }
        }

        async private void ThreadChkNetworkCamera1()
        {
            while (true)
            {
                try
                {
                    bool bPing = await CheckAsyncPing(TCT_Lib.szVideoLane_IPCamera1);
                    if (bPing)
                        LogInfo.WriteLogNetworkCamera(1, "+ Ping Success - Camera 1 = " + TCT_Lib.szVideoLane_IPCamera1);
                    else
                        LogInfo.WriteLogNetworkCamera(1, "------ Error Ping fail - Camera 1 = " + TCT_Lib.szVideoLane_IPCamera1);
                    await Task.Delay(3 * 1000);
                }
                catch (Exception err)
                {
                    LogInfo.WriteLogNetworkCamera(1, "- Error Exception - Camera 1 = " + err.Message);
                    await Task.Delay(3 * 1000);
                }
            }
        }

        async private void ThreadChkNetworkCamera2()
        {
            while (true)
            {
                try
                {
                    bool bPing = await CheckAsyncPing(TCT_Lib.szVideoLane_IPCamera2);
                    if (bPing)
                        LogInfo.WriteLogNetworkCamera(2, "+ Ping Success - Camera 2 = " + TCT_Lib.szVideoLane_IPCamera2);
                    else
                        LogInfo.WriteLogNetworkCamera(2, "------ Error Ping fail - Camera 2 = " + TCT_Lib.szVideoLane_IPCamera2);
                    await Task.Delay(3 * 1000);
                }
                catch (Exception err)
                {
                    LogInfo.WriteLogNetworkCamera(2, "- Error Exception - Camera 2 = " + err.Message);
                    await Task.Delay(3 * 1000);
                }
            }
        }

        //==================================================================
        protected void CreateProcessVideoCaptureImage(int nTrx, bool bIsTowing = false, bool bViolation = false)
        {
            if (TCT_Lib.iVideoNVR_CaptureImageMethod == 0)
                CreateProcessVideoCaptureImage_Method_0(nTrx, bIsTowing);
            else if (TCT_Lib.iVideoNVR_CaptureImageMethod == 1)
                CreateProcessVideoCaptureImage_Method_1(nTrx, bIsTowing, bViolation);
            else if (TCT_Lib.iVideoNVR_CaptureImageMethod == 2)
                CreateProcessVideoCaptureImage_Method_2(nTrx, bIsTowing);
            //else if (TCT_Lib.iVideoNVR_CaptureImageMethod == 3)
            //    CreateProcessVideoCaptureImage_Method_3(nTrx, bIsTowing);
        }

        private void CreateProcessVideoCaptureImage_Method_0(int nTrx, bool bIsTowing = false)
        {
            if (!TCT_Lib.bEnableVideoNVR)
                return;

            if (TCT_Lib.eCurLaneMode == LaneMode.HPMC)
                return;

            //---------------------------------------------------------------------
            if (TCT_Lib.eCurLaneType == LaneType.ETC)
                return;

            if (TCT_Lib.eCurLaneType == LaneType.DUAL)
            {
                if (TCT_Lib.bEtcCtrlIO)
                    return;
            }
            //---------------------------------------------------------------------

            if (TCT_Lib.bMaintenanceMode)
            {
                if (TCT_Lib.eCurAppPage != AppPage.Main)
                    return;
            }

            if (iCapture_nTrx == 0)
                iCapture_nTrx = nTrx;
            else
            {
                if (iCapture_nTrx >= nTrx)
                    nTrx += 1;
            }
            iCapture_nTrx = nTrx;

            LogInfo.WriteLaneEvent($"+ CreateProcessVideoCaptureImage : JobNo={TCT_Lib.iJobNo}, nTrx={nTrx}");
            //---------------------------------------------------------------------------------------
            ProcessVideoCaptureThread(nTrx, 1, CameraPosition.FrontCamera);
            ProcessVideoCaptureThread(nTrx, 2, CameraPosition.RearCamera);
            ProcessVideoCaptureThread(nTrx, 3, CameraPosition.ThirdCamera);

            /*
                        if (bIsTowing)
                        {
                            Thread.Sleep(2000);
                            //---------------------------------------------------
                            LogInfo.WriteLog($"+ CreateProcessVideoCaptureImage-Towing : JobNo={TCT_Lib.iJobNo}, nTrx={nTrx+1}");
                            ProcessVideoCaptureThread(nTrx+1, 1, CameraPosition.FrontCamera, bIsTowing);
                            ProcessVideoCaptureThread(nTrx+1, 2, CameraPosition.RearCamera, bIsTowing);
                            ProcessVideoCaptureThread(nTrx+1, 3, CameraPosition.ThirdCamera, bIsTowing);
                        }
            */
            if (bIsTowing)
            {
                Thread videoCaptureTowingThread = new Thread(() => ThreadVideoCaptureImage_Towing(nTrx));
                videoCaptureTowingThread.Priority = ThreadPriority.Normal;
                videoCaptureTowingThread.IsBackground = true;
                videoCaptureTowingThread.Start();
            }
        }

        private void ThreadVideoCaptureImage_Towing(int nTrx)
        {
            //Thread.Sleep(2000);
            Thread.Sleep(TCT_Lib.iVideoNVR_TowingTimeLapseMillisec);
            LogInfo.WriteLog($"+ CreateProcessVideoCaptureImage-Towing : JobNo={TCT_Lib.iJobNo}, nTrx={nTrx + 1}");
            ProcessVideoCaptureThread(nTrx + 1, 1, CameraPosition.FrontCamera);
            ProcessVideoCaptureThread(nTrx + 1, 2, CameraPosition.RearCamera);
            ProcessVideoCaptureThread(nTrx + 1, 3, CameraPosition.ThirdCamera);
        }

        Thread[] videoCaptureThread = new Thread[3];
        //[SecurityPermissionAttribute(SecurityAction.Demand, ControlThread = true)]
        protected void ProcessVideoCaptureThread(int nTrx, int index, CameraPosition camPosition, bool bIsTowing = false)
        {
            int idxThread = (index - 1);
            if (videoCaptureThread[idxThread] != null)
            {
                try
                {
                    //if (videoCaptureThread[idxThread].IsAlive)
                    //    videoCaptureThread[idxThread].Abort();
                    //videoCaptureThread[idxThread] = null;

                }
                catch (ThreadAbortException exception)
                {
                    //Thread.ResetAbort();
                }
            }
            LogInfo.WriteLogVideoCapture($"Start Thread JobNo={TCT_Lib.iJobNo}, nTrx={nTrx}, index={index}, video idx thread={idxThread}");
            //videoCaptureThread            = new Thread(new ThreadStart(ThreadVideoCaptureImage));
            videoCaptureThread[idxThread]               = new Thread(() => ThreadVideoCaptureImage(nTrx, index, camPosition, bIsTowing));
            videoCaptureThread[idxThread].Priority      = ThreadPriority.Normal;
            videoCaptureThread[idxThread].IsBackground  = true;
            videoCaptureThread[idxThread].Start();
        }

        protected void ProcessVideoCaptureThread_000(int nTrx, int index, CameraPosition camPosition, bool bIsTowing = false)
        {
            LogInfo.WriteLogVideoCapture($"+ >>>> Start Thread JobNo={TCT_Lib.iJobNo}, nTrx={nTrx}, index={index}");
            //videoCaptureThread            = new Thread(new ThreadStart(ThreadVideoCaptureImage));
            Thread videoCaptureThread           = new Thread(() => ThreadVideoCaptureImage(nTrx, index, camPosition, bIsTowing));
            videoCaptureThread.Priority         = ThreadPriority.Normal;
            videoCaptureThread.IsBackground     = true;
            videoCaptureThread.Start();
        }

        //[SecurityPermissionAttribute(SecurityAction.Demand, ControlThread = true)]
        private void ThreadVideoCaptureImage(int nTrx, int index, CameraPosition camPosition, bool bIsTowing)
        {
            if (camPosition == CameraPosition.FrontCamera)
            {
                if (TCT_Lib.szVideoNVR_FrontCaptureUrl.Trim() == "")
                {
                    LogInfo.WriteLog("- Not found front camera url setting.");
                    return;
                }
            }
            else if (camPosition == CameraPosition.RearCamera)
            {
                if (TCT_Lib.szVideoNVR_RearCaptureUrl.Trim() == "")
                {
                    LogInfo.WriteLog("- Not found rear camera url setting.");
                    return;
                }
            }
            else if (camPosition == CameraPosition.ThirdCamera)
            {
                if (TCT_Lib.szVideoNVR_ThirdCaptureUrl.Trim() == "")
                {
                    LogInfo.WriteLog("- Not found third camera url setting.");
                    return;
                }
            }

            if (index == 3)
                Thread.Sleep(TCT_Lib.iVideoNVR_Img3TimeLapseMilliSec);

            int idxThread = (index - 1);
            if (videoCaptureThread[idxThread] != null)
            {
                try
                {
                    //if (videoCaptureThread[idxThread].IsAlive)
                    //    videoCaptureThread[idxThread].Abort();
                    //videoCaptureThread[idxThread] = null;
                    //var cstor = typeof(ThreadAbortException)
                    //        .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                    // create ThreadAbortException instance
                    //ThreadAbortException ex = cstor.Invoke(null) as ThreadAbortException;
                }
                catch (ThreadAbortException exception)
                {
                    //Thread.ResetAbort();
                }
            }

            //------------------------------------------------------------
            string szVideoImgFileName = $"{TCT_Lib.szLaneCode}-{DateTime.Now.ToString("yyyyMMdd")}-{TCT_Lib.iJobNo}-{nTrx}-{index}";
            string szOutputPath = $"{TCT_Lib.szVideoNVR_OutputPathPrefix}{DateTime.Now.ToString("yyyyMMdd")}{TCT_Lib.szVideoNVR_OutputPathSuffix}";
            string szCameraUrl = "";
            if (camPosition == CameraPosition.FrontCamera)
                szCameraUrl = TCT_Lib.szVideoNVR_FrontCaptureUrl;
            else if (camPosition == CameraPosition.RearCamera)
                szCameraUrl = TCT_Lib.szVideoNVR_RearCaptureUrl;
            else if (camPosition == CameraPosition.ThirdCamera)
                szCameraUrl = TCT_Lib.szVideoNVR_ThirdCaptureUrl;
            string szCaptureUrl = $"{szCameraUrl}&filename={szVideoImgFileName}&outputpath={szOutputPath}";

            var tctNVR = new TCT_NVR(TCT_Lib.iJobNo, nTrx, 0);
            try
            {
                DateTime dtStart    = DateTime.Now;
                bool bResult        = tctNVR.CreateImageFromUrl(szCaptureUrl, index);
                TimeSpan dtSpan     = DateTime.Now.Subtract(dtStart);
                string szStr        = "Video Capture Time : " + dtSpan.TotalSeconds.ToString("0") + " sec. Request : " + szCaptureUrl;
                LogInfo.WriteLogVideoCapture("+ >>>>> End ThreadVideoCaptureImage : " + szStr);

                //if (!bResult)
                //    videoCaptureThread.Abort();
                //videoCaptureThread.Abort();

                //if (videoCaptureThread[idxThread].IsAlive)
                //    videoCaptureThread[idxThread].Abort();
                //var cstor = typeof(ThreadAbortException)
                //            .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);

            }
            catch (ThreadAbortException)
            {
                //Thread.ResetAbort();
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error ThreadVideoCaptureImage : " + err.Message);
                if (!err.Message.Contains("Thread was being aborted"))
                    LogInfo.WriteLogVideoCapture("- Error ThreadVideoCaptureImage : " + err.Message);
            }
            finally
            {
                //GC.SuppressFinalize(tctNVR);
                tctNVR = null;
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
            }
        }

        private void ThreadVideoCaptureImage_V0(int index, CameraPosition camPosition, bool bIsTowing)
        {
            if (camPosition == CameraPosition.FrontCamera)
            {
                if (TCT_Lib.szVideoNVR_FrontCaptureUrl.Trim() == "")
                {
                    LogInfo.WriteLog("- Not found front camera url setting.");
                    return;
                }
            }
            else if (camPosition == CameraPosition.RearCamera)
            {
                if (TCT_Lib.szVideoNVR_RearCaptureUrl.Trim() == "")
                {
                    LogInfo.WriteLog("- Not found rear camera url setting.");
                    return;
                }
            }
            else if (camPosition == CameraPosition.ThirdCamera)
            {
                if (TCT_Lib.szVideoNVR_ThirdCaptureUrl.Trim() == "")
                {
                    LogInfo.WriteLog("- Not found third camera url setting.");
                    return;
                }
            }
            //------------------------------------------------------------

            if (CheckIsViolation() && (index > 2))
                nTrxTmp = TCT_Lib.nTrx;
            //int nTrx = (index > 2) ? nTrxTmp : TCT_Lib.nTrx;
            int nTrx = TCT_Lib.nTrx;

            if (index == 3)
                Thread.Sleep(1500);
            string szVideoImgFileName   = $"{TCT_Lib.szLaneCode}-{DateTime.Now.ToString("yyyyMMdd")}-{TCT_Lib.iJobNo}-{nTrx}-{index}";
            string szOutputPath         = $"{TCT_Lib.szVideoNVR_OutputPathPrefix}{DateTime.Now.ToString("yyyyMMdd")}{TCT_Lib.szVideoNVR_OutputPathSuffix}";
            string szCameraUrl          = "";
            if (camPosition == CameraPosition.FrontCamera)
                szCameraUrl = TCT_Lib.szVideoNVR_FrontCaptureUrl;
            else if (camPosition == CameraPosition.RearCamera)
                szCameraUrl = TCT_Lib.szVideoNVR_RearCaptureUrl;
            else if (camPosition == CameraPosition.ThirdCamera)
                szCameraUrl = TCT_Lib.szVideoNVR_ThirdCaptureUrl;
            string szCaptureUrl = $"{szCameraUrl}&filename={szVideoImgFileName}&outputpath={szOutputPath}";

            var tctNVR = new TCT_NVR(TCT_Lib.iJobNo, nTrx, 0);
            try
            {
                DateTime dtStart    = DateTime.Now;
                bool bResult        = tctNVR.CreateImageFromUrl(szCaptureUrl, index);
                TimeSpan dtSpan     = DateTime.Now.Subtract(dtStart);
                string szStr        = "Video Capture Time : " + dtSpan.TotalSeconds.ToString("0") + " sec. Request : " + szCaptureUrl;
                LogInfo.WriteLogVideoCapture("+ End ThreadVideoCaptureImage : " + szStr);

                //videoCaptureThread.Abort();
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error ThreadVideoCaptureImage : " + err.Message);
                if (!err.Message.Contains("Thread was being aborted"))
                    LogInfo.WriteLogVideoCapture("- Error ThreadVideoCaptureImage : " + err.Message);
            }
            finally
            {
                GC.SuppressFinalize(tctNVR);
                tctNVR = null;
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
            }
        }

        //==============================================================================
        private void CreateProcessVideoCaptureImage_Method_1(int nTrx, bool bIsTowing = false, bool bViolation = false)
        {
            if (!TCT_Lib.bEnableVideoNVR)
                return;

            if (TCT_Lib.eCurLaneMode == LaneMode.HPMC)
                return;

            //---------------------------------------------------------------------
            if (TCT_Lib.eCurLaneType == LaneType.ETC)
                return;

            if (TCT_Lib.eCurLaneType == LaneType.DUAL)
            {
                if (TCT_Lib.bEtcCtrlIO)
                    return;
            }
            //---------------------------------------------------------------------

            if (iCapture_nTrx == 0)
                iCapture_nTrx = nTrx;
            else
            {
                if (iCapture_nTrx >= nTrx)
                    nTrx += 1;
            }
            iCapture_nTrx = nTrx;

            if (TCT_Lib.eCurLaneType == LaneType.ETC)
                nTrx = 0;

            long iCaptureRunningNo = TCT_Lib.LaneDB.GetMaxCaptureRunningNo();

            LogInfo.WriteLaneEvent($"+ CreateProcessVideoCaptureImage Method_1 : JobNo={TCT_Lib.iJobNo}, nTrx={nTrx}, CaptureRunningNo={iCaptureRunningNo}, Violation={bViolation}");
            //---------------------------------------------------------------------------------------
            ProcessVideoCaptureThread_Method_1(nTrx, 1, CameraPosition.FrontCamera, iCaptureRunningNo, false, bViolation);
            ProcessVideoCaptureThread_Method_1(nTrx, 2, CameraPosition.RearCamera, iCaptureRunningNo, false, bViolation);
            ProcessVideoCaptureThread_Method_1(nTrx, 3, CameraPosition.ThirdCamera, iCaptureRunningNo, false, bViolation);

            if (bIsTowing)
            {
                Thread videoCaptureTowingThread_Method_1        = new Thread(() => ThreadVideoCaptureImage_Towing_Method_1(nTrx, iCaptureRunningNo));
                videoCaptureTowingThread_Method_1.Priority      = ThreadPriority.Normal;
                videoCaptureTowingThread_Method_1.IsBackground  = true;
                videoCaptureTowingThread_Method_1.Start();
            }
        }

        private void ThreadVideoCaptureImage_Towing_Method_1(int nTrx, long iCaptureRunningNo)
        {
            //Thread.Sleep(2000);
            Thread.Sleep(TCT_Lib.iVideoNVR_TowingTimeLapseMillisec);
            LogInfo.WriteLog($"+ CreateProcessVideoCaptureImage-Towing : JobNo={TCT_Lib.iJobNo}, nTrx={nTrx + 1}, CaptureRunningNo={iCaptureRunningNo}");
            ProcessVideoCaptureThread_Method_1(nTrx + 1, 1, CameraPosition.FrontCamera, iCaptureRunningNo);
            ProcessVideoCaptureThread_Method_1(nTrx + 1, 2, CameraPosition.RearCamera, iCaptureRunningNo);
            ProcessVideoCaptureThread_Method_1(nTrx + 1, 3, CameraPosition.ThirdCamera, iCaptureRunningNo);
        }

        protected void ProcessVideoCaptureThread_Method_1(int nTrx, int index, CameraPosition camPosition, long iCaptureRunningNo, bool bIsTowing = false, bool bViolation = false)
        {
            int idxThread = (index - 1);
            LogInfo.WriteLogVideoCapture($"+ >>>>> Start Thread JobNo={TCT_Lib.iJobNo}, nTrx={nTrx}, index={index}, video idx thread={idxThread}");

            videoCaptureThread[idxThread]               = new Thread(() => ThreadVideoCaptureImage_Method_1(nTrx, index, camPosition, iCaptureRunningNo, bIsTowing, bViolation));
            videoCaptureThread[idxThread].Priority      = ThreadPriority.Normal;
            videoCaptureThread[idxThread].IsBackground  = true;
            videoCaptureThread[idxThread].Start();
        }

        private void ThreadVideoCaptureImage_Method_1(int nTrx, int index, CameraPosition camPosition, long iCaptureRunningNo, bool bIsTowing, bool bViolation)
        {
            string szCaptureUrl = "", szUserName = "", szPassword = "";
            string szShortCamera = "";
            if (camPosition == CameraPosition.FrontCamera)
            {
                if (TCT_Lib.szVideoLane_FrontCameraUrl.Trim() == "")
                {
                    LogInfo.WriteLog("- Not found front camera url setting.");
                    return;
                }
                szCaptureUrl    = TCT_Lib.szVideoLane_FrontCameraUrl.Trim();
                szUserName      = TCT_Lib.szVideoLane_FrontCamera_UserName;
                szPassword      = TCT_Lib.szVideoLane_FrontCamera_Password;
                szShortCamera   = "F";
            }
            else if (camPosition == CameraPosition.RearCamera)
            {
                if (TCT_Lib.szVideoLane_RearCameraUrl.Trim() == "")
                {
                    LogInfo.WriteLog("- Not found rear camera url setting.");
                    return;
                }
                szCaptureUrl    = TCT_Lib.szVideoLane_RearCameraUrl.Trim();
                szUserName      = TCT_Lib.szVideoLane_RearCamera_UserName;
                szPassword      = TCT_Lib.szVideoLane_RearCamera_Password;
                szShortCamera   = "R";
            }
            else if (camPosition == CameraPosition.ThirdCamera)
            {
                if (TCT_Lib.szVideoNVR_ThirdCaptureUrl.Trim() == "")
                {
                    LogInfo.WriteLog("- Not found third camera url setting.");
                    return;
                }
                szCaptureUrl    = TCT_Lib.szVideoLane_ThirdCameraUrl.Trim();
                szUserName      = TCT_Lib.szVideoLane_ThirdCamera_UserName;
                szPassword      = TCT_Lib.szVideoLane_ThirdCamera_Password;
                szShortCamera   = "R";
            }

            if (index == 3)
                Thread.Sleep(TCT_Lib.iVideoNVR_Img3TimeLapseMilliSec);

            //------------------------------------------------------------
            var tctNVR = new TCT_NVR(TCT_Lib.iJobNo, nTrx, 0);
            try
            {
                string szVideoImgFileName = $"{TCT_Lib.szLaneCode}-{DateTime.Now.ToString("yyyyMMdd")}-{TCT_Lib.iJobNo}-{nTrx}-{index}.jpg";
                DateTime dtStart    = DateTime.Now;
                bool bResult        = tctNVR.SaveImageFromUrl(index, szCaptureUrl, szVideoImgFileName, 0, szUserName, szPassword);
                TimeSpan dtSpan     = DateTime.Now.Subtract(dtStart);
                string szStr        = "Video Capture Time : " + dtSpan.TotalSeconds.ToString("0") + " sec. Request : " + szCaptureUrl;
                LogInfo.WriteLogVideoCapture($"+ >>>>> End ThreadVideoCaptureImage : JobNo={TCT_Lib.iJobNo}, nTrx={nTrx}, index={index} : {szStr}");

                //string szVideoImgFileName   = $"{TCT_Lib.szLaneName}_{szShortCamera}_D{DateTime.Now.ToString("yyyyMMdd")}_T{DateTime.Now.ToString("HHmmssfff")}_N{iCaptureRunningNo.ToString("000000")}{index}.jpg";
                //DateTime dtStart            = DateTime.Now;
                //bool bResult                = tctNVR.SaveImageFromUrl(index, szCaptureUrl, szVideoImgFileName, iCaptureRunningNo, szUserName, szPassword);
                //TimeSpan dtSpan             = DateTime.Now.Subtract(dtStart);
                //string szStr                = "Video Capture Time : " + dtSpan.TotalSeconds.ToString("0") + " sec. Request : " + szCaptureUrl;
                //LogInfo.WriteLogVideoCapture("+ End ThreadVideoCaptureImage : " + szStr);


                //-------------------------------------------------------------------------
                //--- Last Image of Violation, then Send image again
                if (bViolation && (index == 3))
                {
                    Task.Delay(1000);
                    int iTransID = TCT_Lib.GetLaneTransactionId(TCT_Lib.iJobNo, nTrx, DateTime.Now);
                    int _iJobNo = 0, _nTrx = 0;
                    var xImageCameraObj     = TCT_Obj.GenerateCaptureImageCamera(iTransID, ref _iJobNo, ref _nTrx);
                    string szImageCameraJson = JsonConvert.SerializeObject(xImageCameraObj, Formatting.Indented);
                    //long iCaptureRunningNo      = TCT_Lib.LaneDB.GetCaptureRunningNoOfTrans(_iJobNo, _nTrx, iTransID);
                    //using (StreamWriter writer  = File.CreateText(Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, $"ImageCamera_Trans{iTransID}_J{_iJobNo}_N{_nTrx}cdh")))
                    string szCaptureJsonFile = Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, $"ImageCamera_Trans{iTransID}_N{iCaptureRunningNo.ToString("000000")}.json");

                    try
                    {
                        if (TCT_Lib.szVideoLane_CaptureImagePath != "")
                        {
                            if (!Directory.Exists(TCT_Lib.szVideoLane_CaptureImagePath))
                            {
                                LogInfo.WriteLog("+ Auto create capture image path : " + TCT_Lib.szVideoLane_CaptureImagePath);
                                Directory.CreateDirectory(TCT_Lib.szVideoLane_CaptureImagePath);
                            }
                        }

                        using (StreamWriter writer = File.CreateText(szCaptureJsonFile))
                        {
                            writer.WriteLine(szImageCameraJson);
                        }
                        //--------------------------------
                        //TCT_NVR.MoveFolderCaptureImageTrans(iTransID, iCaptureRunningNo, xImageCameraObj.captureImages);
                        //--------------------------------
                        var sendImgCamera = new WebService_ImageCamera();
                        sendImgCamera.CallWebService_SendImageCaptureCamera(iTransID, TCT_Lib.iJobNo, nTrx, szImageCameraJson, iCaptureRunningNo, xImageCameraObj.captureImages);
                    }
                    catch (Exception err)
                    {
                        LogInfo.WriteLog($"- Error create capture image path={TCT_Lib.szVideoLane_CaptureImagePath}, Error={err.Message}");
                        LogInfo.WriteLaneEvent($"- Error create capture image path={TCT_Lib.szVideoLane_CaptureImagePath}, Error={err.Message}");
                        LogInfo.WriteLogVideoCapture($"- Error create capture image path={TCT_Lib.szVideoLane_CaptureImagePath}, Error={err.Message}");

                        //-----------------------------------

                    }
                }
                //-----------------------------------------------
                //if (!bResult)
                //    videoCaptureThread_Method_1.Abort();
                //videoCaptureThread.Abort();
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error ThreadVideoCaptureImage : " + err.Message);
                if (!err.Message.Contains("Thread was being aborted"))
                    LogInfo.WriteLogVideoCapture("- Error ThreadVideoCaptureImage : " + err.Message);
            }
            finally
            {
                int idxThread = (index - 1);
                if (videoCaptureThread[idxThread].IsAlive)
                    videoCaptureThread[idxThread].Abort();

                //GC.SuppressFinalize(tctNVR);
                tctNVR = null;
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
            }
        }

        //----------------------------------------------------------------------------------------
        private void CreateProcessVideoCaptureImage_Method_2(int nTrx, bool bIsTowing = false)
        {
            if (!TCT_Lib.bEnableVideoNVR)
                return;

            if (TCT_Lib.eCurLaneMode == LaneMode.HPMC)
                return;

            //---------------------------------------------------------------------
            if (TCT_Lib.eCurLaneType == LaneType.ETC)
                return;

            if (TCT_Lib.eCurLaneType == LaneType.DUAL)
            {
                if (TCT_Lib.bEtcCtrlIO)
                    return;
            }
            //---------------------------------------------------------------------

            if (iCapture_nTrx == 0)
                iCapture_nTrx = nTrx;
            else
            {
                if (iCapture_nTrx >= nTrx)
                    nTrx += 1;
            }
            iCapture_nTrx = nTrx;

            if (TCT_Lib.eCurLaneType == LaneType.ETC)
                nTrx = 0;

            long iCaptureRunningNo = TCT_Lib.LaneDB.GetMaxCaptureRunningNo();

            LogInfo.WriteLaneEvent($"+ CreateProcessVideoCaptureImage Method_2 : JobNo={TCT_Lib.iJobNo}, nTrx={nTrx}, CaptureRunningNo={iCaptureRunningNo}");
            //---------------------------------------------------------------------------------------
            ProcessVideoCaptureThread_Method_2(nTrx, 1, CameraPosition.FrontCamera, iCaptureRunningNo);
            ProcessVideoCaptureThread_Method_2(nTrx, 2, CameraPosition.RearCamera, iCaptureRunningNo);
            ProcessVideoCaptureThread_Method_2(nTrx, 3, CameraPosition.ThirdCamera, iCaptureRunningNo);

            if (bIsTowing)
            {
                Thread videoCaptureTowingThread_Method_2    = new Thread(() => ThreadVideoCaptureImage_Towing_Method_2(nTrx, iCaptureRunningNo));
                videoCaptureTowingThread_Method_2.Priority  = ThreadPriority.Normal;
                videoCaptureTowingThread_Method_2.IsBackground = true;
                videoCaptureTowingThread_Method_2.Start();
            }
        }

        private void ThreadVideoCaptureImage_Towing_Method_2(int nTrx, long iCaptureRunningNo)
        {
            //Thread.Sleep(2000);
            Thread.Sleep(TCT_Lib.iVideoNVR_TowingTimeLapseMillisec);
            LogInfo.WriteLog($"+ CreateProcessVideoCaptureImage-Towing : JobNo={TCT_Lib.iJobNo}, nTrx={nTrx + 1}, CaptureRunningNo={iCaptureRunningNo}");
            ProcessVideoCaptureThread_Method_2(nTrx + 1, 1, CameraPosition.FrontCamera, iCaptureRunningNo);
            ProcessVideoCaptureThread_Method_2(nTrx + 1, 2, CameraPosition.RearCamera, iCaptureRunningNo);
            ProcessVideoCaptureThread_Method_2(nTrx + 1, 3, CameraPosition.ThirdCamera, iCaptureRunningNo);
        }

        protected void ProcessVideoCaptureThread_Method_2(int nTrx, int index, CameraPosition camPosition, long iCaptureRunningNo, bool bIsTowing = false)
        {
            //videoCaptureThread            = new Thread(new ThreadStart(ThreadVideoCaptureImage));
            Thread videoCaptureThread_Method_2      = new Thread(() => ThreadVideoCaptureImage_Method_2(nTrx, index, camPosition, iCaptureRunningNo, bIsTowing));
            videoCaptureThread_Method_2.Priority    = ThreadPriority.Normal;
            videoCaptureThread_Method_2.IsBackground = true;
            videoCaptureThread_Method_2.Start();
        }

        private string ThreadVideoCaptureImage_Method_2(int nTrx, int index, CameraPosition camPosition, long iCaptureRunningNo, bool bIsTowing)
        {
            string szCaptureUrl = "", szUserName = "", szPassword = "";
            string szShortCamera = "";
            if (camPosition == CameraPosition.FrontCamera)
            {
                if (TCT_Lib.szVideoLane_FrontCameraUrl.Trim() == "")
                {
                    LogInfo.WriteLog("- Not found front camera url setting.");
                    return "";
                }
                szCaptureUrl    = TCT_Lib.szVideoLane_FrontCameraUrl.Trim();
                szUserName      = TCT_Lib.szVideoLane_FrontCamera_UserName;
                szPassword      = TCT_Lib.szVideoLane_FrontCamera_Password;
                szShortCamera   = "F";
            }
            else if (camPosition == CameraPosition.RearCamera)
            {
                if (TCT_Lib.szVideoLane_RearCameraUrl.Trim() == "")
                {
                    LogInfo.WriteLog("- Not found rear camera url setting.");
                    return "";
                }
                szCaptureUrl    = TCT_Lib.szVideoLane_RearCameraUrl.Trim();
                szUserName      = TCT_Lib.szVideoLane_RearCamera_UserName;
                szPassword      = TCT_Lib.szVideoLane_RearCamera_Password;
                szShortCamera   = "R";
            }
            else if (camPosition == CameraPosition.ThirdCamera)
            {
                if (TCT_Lib.szVideoNVR_ThirdCaptureUrl.Trim() == "")
                {
                    LogInfo.WriteLog("- Not found third camera url setting.");
                    return "";
                }
                szCaptureUrl    = TCT_Lib.szVideoLane_ThirdCameraUrl.Trim();
                szUserName      = TCT_Lib.szVideoLane_ThirdCamera_UserName;
                szPassword      = TCT_Lib.szVideoLane_ThirdCamera_Password;
                szShortCamera   = "R";
            }

            if (index == 3)
                Thread.Sleep(TCT_Lib.iVideoNVR_Img3TimeLapseMilliSec);

            //------------------------------------------------------------
            String szResult = "";
            var tctNVR = new TCT_NVR(TCT_Lib.iJobNo, nTrx, 0);
            try
            {
                //string szVideoImgFileName   = $"P{TCT_Lib.szPlazaID}_L{TCT_Lib.szLaneName}_{szShortCamera}_D{DateTime.Now.ToString("yyyyMMdd")}_T{DateTime.Now.ToString("HHmmssfff")}_J{TCT_Lib.iJobNo}_N{nTrx}_I{index}.jpg";
                //string szVideoImgFileName   = $"{TCT_Lib.szLaneName}_{szShortCamera}_D{DateTime.Now.ToString("yyyyMMdd")}_T{DateTime.Now.ToString("HHmmssfff")}_J{TCT_Lib.iJobNo}_N{nTrx}_I{index}.jpg";
                string szVideoImgFileName = $"{TCT_Lib.szLaneName}_{szShortCamera}_D{DateTime.Now.ToString("yyyyMMdd")}_T{DateTime.Now.ToString("HHmmssfff")}_N{iCaptureRunningNo.ToString("000000")}{index}.jpg";
                DateTime dtStart = DateTime.Now;
                bool bResult    = tctNVR.SaveImageFromUrl(index, szCaptureUrl, szVideoImgFileName, iCaptureRunningNo, szUserName, szPassword);
                TimeSpan dtSpan = DateTime.Now.Subtract(dtStart);
                string szStr    = "Video Capture Time : " + dtSpan.TotalSeconds.ToString("0") + " sec. Request : " + szCaptureUrl;
                LogInfo.WriteLogVideoCapture("+ End ThreadVideoCaptureImage : " + szStr);

                //if (!bResult)
                //    videoCaptureThread_Method_2.Abort();
                //videoCaptureThread.Abort();

                szResult = szVideoImgFileName;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error ThreadVideoCaptureImage : " + err.Message);
                if (!err.Message.Contains("Thread was being aborted"))
                    LogInfo.WriteLogVideoCapture("- Error ThreadVideoCaptureImage : " + err.Message);
                //return "";
            }
            finally
            {
                GC.SuppressFinalize(tctNVR);
                tctNVR = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            return szResult;
        }

        //=======================================================================================
        //----------------------------------------------------------------------------------------
        protected void CreateProcessVideoCaptureImage_Method_3(int idx, int nTrx, bool bIsTowing = false, bool bViolation = false, bool bWaitLoop = false)
        {
            if (!TCT_Lib.bEnableVideoNVR)
                return;

            if (TCT_Lib.eCurLaneMode == LaneMode.HPMC)
                return;

            //---------------------------------------------------------------------
            if (TCT_Lib.eCurLaneType == LaneType.ETC)
                return;

            if (TCT_Lib.eCurLaneType == LaneType.DUAL)
            {
                if (TCT_Lib.bEtcCtrlIO)
                    return;
            }
            //---------------------------------------------------------------------
            if (bWaitLoop)
                Thread.Sleep(500);

            if (iCapture_nTrx == 0)
                iCapture_nTrx = nTrx;
            /*
                        else
                        {
                            if (iCapture_nTrx >= nTrx)
                                nTrx += 1;
                        }
                        iCapture_nTrx = nTrx;
            */

            if (TCT_Lib.eCurLaneType == LaneType.ETC)
                nTrx = 0;

            long iCaptureRunningNo = idx == 1 ? TCT_Lib.LaneDB.GetMaxCaptureRunningNo() : TCT_Lib.LaneDB.GetCurCaptureRunningNo(TCT_Lib.iJobNo, nTrx);
            long iRunningNo = iCaptureRunningNo;
            if ((idx == 3) && (iCurRunIdx == 1))
            {
                //iRunningNo--;
                iCaptureRunningNo = iRunningNo-1;
            }

            iCurRunIdx = idx;

            LogInfo.WriteLaneEvent($"+ CreateProcessVideoCaptureImage Method_3 : Index={idx}, JobNo={TCT_Lib.iJobNo}, nTrx={nTrx}, CaptureRunningNo={iCaptureRunningNo}");
            //---------------------------------------------------------------------------------------
            if (idx == 1)
                ProcessVideoCaptureThread_Method_3(nTrx, 1, CameraPosition.RearCamera, iCaptureRunningNo);
            else if (idx == 2)
                ProcessVideoCaptureThread_Method_3(nTrx, 2, CameraPosition.RearCamera, iCaptureRunningNo);
            else if (idx == 3)
                ProcessVideoCaptureThread_Method_3(nTrx, 3, CameraPosition.FrontCamera, iCaptureRunningNo);

            if (bIsTowing && (idx == 2))
            {
                Thread videoCaptureTowingThread_Method_3        = new Thread(() => ThreadVideoCaptureImage_Towing_Method_3(nTrx, iCaptureRunningNo));
                videoCaptureTowingThread_Method_3.Priority      = ThreadPriority.Normal;
                videoCaptureTowingThread_Method_3.IsBackground  = true;
                videoCaptureTowingThread_Method_3.Start();
            }
        }

        private void ThreadVideoCaptureImage_Towing_Method_3(int nTrx, long iCaptureRunningNo)
        {
            //Thread.Sleep(2000);
            //Thread.Sleep(TCT_Lib.iVideoNVR_TowingTimeLapseMillisec);
            LogInfo.WriteLog($"+ CreateProcessVideoCaptureImage-Towing : JobNo={TCT_Lib.iJobNo}, nTrx={nTrx + 1}, CaptureRunningNo={iCaptureRunningNo}");
            ProcessVideoCaptureThread_Method_3(nTrx + 1, 1, CameraPosition.RearCamera, iCaptureRunningNo);
            Thread.Sleep(1000);
            ProcessVideoCaptureThread_Method_3(nTrx + 1, 2, CameraPosition.RearCamera, iCaptureRunningNo);
            Thread.Sleep(1000);
            ProcessVideoCaptureThread_Method_3(nTrx + 1, 3, CameraPosition.FrontCamera, iCaptureRunningNo);
        }

        protected void ProcessVideoCaptureThread_Method_3(int nTrx, int index, CameraPosition camPosition, long iCaptureRunningNo, bool bIsTowing = false)
        {
            //videoCaptureThread            = new Thread(new ThreadStart(ThreadVideoCaptureImage));
            Thread videoCaptureThread_Method_3          = new Thread(() => ThreadVideoCaptureImage_Method_3(nTrx, index, camPosition, iCaptureRunningNo, bIsTowing));
            videoCaptureThread_Method_3.Priority        = ThreadPriority.Normal;
            videoCaptureThread_Method_3.IsBackground    = true;
            videoCaptureThread_Method_3.Start();
        }

        private string ThreadVideoCaptureImage_Method_3(int nTrx, int index, CameraPosition camPosition, long iCaptureRunningNo, bool bIsTowing)
        {
            string szCaptureUrl = "", szUserName = "", szPassword = "";
            string szShortCamera = "";
            if (camPosition == CameraPosition.RearCamera)
            {
                if (TCT_Lib.szVideoLane_RearCameraUrl.Trim() == "")
                {
                    LogInfo.WriteLog("- Not found rear camera url setting.");
                    return "";
                }
                szCaptureUrl    = TCT_Lib.szVideoLane_RearCameraUrl.Trim();
                szUserName      = TCT_Lib.szVideoLane_RearCamera_UserName;
                szPassword      = TCT_Lib.szVideoLane_RearCamera_Password;
                szShortCamera   = "R";
            }
            else if (camPosition == CameraPosition.RearCamera)
            {
                if (TCT_Lib.szVideoLane_RearCameraUrl.Trim() == "")
                {
                    LogInfo.WriteLog("- Not found rear camera url setting.");
                    return "";
                }
                szCaptureUrl    = TCT_Lib.szVideoLane_RearCameraUrl.Trim();
                szUserName      = TCT_Lib.szVideoLane_RearCamera_UserName;
                szPassword      = TCT_Lib.szVideoLane_RearCamera_Password;
                szShortCamera   = "R";
            }
            else if (camPosition == CameraPosition.FrontCamera)
            {
                if (TCT_Lib.szVideoLane_FrontCameraUrl.Trim() == "")
                {
                    LogInfo.WriteLog("- Not found front camera url setting.");
                    return "";
                }
                szCaptureUrl    = TCT_Lib.szVideoLane_FrontCameraUrl.Trim();
                szUserName      = TCT_Lib.szVideoLane_FrontCamera_UserName;
                szPassword      = TCT_Lib.szVideoLane_FrontCamera_Password;
                szShortCamera   = "F";
            }
            /*
                        else if (camPosition == CameraPosition.RearCamera)
                        {
                            if (TCT_Lib.szVideoLane_RearCameraUrl.Trim() == "")
                            {
                                LogInfo.WriteLog("- Not found rear camera url setting.");
                                return "";
                            }
                            szCaptureUrl    = TCT_Lib.szVideoLane_RearCameraUrl.Trim();
                            szUserName      = TCT_Lib.szVideoLane_RearCamera_UserName;
                            szPassword      = TCT_Lib.szVideoLane_RearCamera_Password;
                            szShortCamera   = "R";
                        }

                        else if (camPosition == CameraPosition.ThirdCamera)
                        {
                            if (TCT_Lib.szVideoNVR_ThirdCaptureUrl.Trim() == "")
                            {
                                LogInfo.WriteLog("- Not found third camera url setting.");
                                return "";
                            }
                            szCaptureUrl    = TCT_Lib.szVideoLane_ThirdCameraUrl.Trim();
                            szUserName      = TCT_Lib.szVideoLane_ThirdCamera_UserName;
                            szPassword      = TCT_Lib.szVideoLane_ThirdCamera_Password;
                            szShortCamera   = "R";
                        }
            */
            //if (index == 3)
            //    Thread.Sleep(TCT_Lib.iVideoNVR_Img3TimeLapseMilliSec);

            //------------------------------------------------------------
            String szResult = "";
            var tctNVR      = new TCT_NVR(TCT_Lib.iJobNo, nTrx, 0);
            try
            {
                //string szVideoImgFileName   = $"P{TCT_Lib.szPlazaID}_L{TCT_Lib.szLaneName}_{szShortCamera}_D{DateTime.Now.ToString("yyyyMMdd")}_T{DateTime.Now.ToString("HHmmssfff")}_J{TCT_Lib.iJobNo}_N{nTrx}_I{index}.jpg";
                //string szVideoImgFileName   = $"{TCT_Lib.szLaneName}_{szShortCamera}_D{DateTime.Now.ToString("yyyyMMdd")}_T{DateTime.Now.ToString("HHmmssfff")}_J{TCT_Lib.iJobNo}_N{nTrx}_I{index}.jpg";
                string szVideoImgFileName   = $"{TCT_Lib.szLaneName}_{szShortCamera}_D{DateTime.Now.ToString("yyyyMMdd")}_T{DateTime.Now.ToString("HHmmssfff")}_N{iCaptureRunningNo.ToString("000000")}{index}.jpg";
                DateTime dtStart            = DateTime.Now;
                bool bResult                = tctNVR.SaveImageFromUrl(index, szCaptureUrl, szVideoImgFileName, iCaptureRunningNo, szUserName, szPassword);
                TimeSpan dtSpan             = DateTime.Now.Subtract(dtStart);
                string szStr                = "Video Capture Time : " + dtSpan.TotalSeconds.ToString("0") + " sec. Request : " + szCaptureUrl;
                LogInfo.WriteLogVideoCapture("+ End ThreadVideoCaptureImage : " + szStr);

                //if (!bResult)
                //    videoCaptureThread_Method_3.Abort();
                //videoCaptureThread.Abort();

                szResult = szVideoImgFileName;

                //--- Fail Get Transaction
                //if (index == 3)
                //    SendImageCameraJson_Method_3(nTrx);
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error ThreadVideoCaptureImage : " + err.Message);
                if (!err.Message.Contains("Thread was being aborted"))
                    LogInfo.WriteLogVideoCapture("- Error ThreadVideoCaptureImage : " + err.Message);
                //return "";
            }
            finally
            {
                GC.SuppressFinalize(tctNVR);
                tctNVR = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            return szResult;
        }


        protected void ProcessCreateImageCameraSendJsonFileThread_Method_3(int iTransID)
        {
            Thread createImageCameraSendJsonFileThread_Method_3             = new Thread(() => ThreadCreateImageCameraSendJsonFile_Method_3(iTransID));
            createImageCameraSendJsonFileThread_Method_3.Priority           = ThreadPriority.Normal;
            createImageCameraSendJsonFileThread_Method_3.IsBackground       = true;
            createImageCameraSendJsonFileThread_Method_3.Start();
        }

        async protected void ThreadCreateImageCameraSendJsonFile_Method_3(int iTransID) 
        {
            await Task.Delay(2000);
            int _iJobNo = 0, _nTrx = 0;
            var xImageCameraObj         = TCT_Obj.GenerateCaptureImageCamera(iTransID, ref _iJobNo, ref _nTrx);
            string szImageCameraJson    = JsonConvert.SerializeObject(xImageCameraObj, Formatting.Indented);
            long iCaptureRunningNo      = TCT_Lib.LaneDB.GetCaptureRunningNoOfTrans(_iJobNo, _nTrx, iTransID, DateTime.Now);
            //using (StreamWriter writer  = File.CreateText(Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, $"ImageCamera_Trans{iTransID}_J{_iJobNo}_N{_nTrx}.json")))
            string szCaptureJsonFile    = Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, $"ImageCamera_Trans{iTransID}_N{iCaptureRunningNo.ToString("000000")}.json");

            try
            {
                if (TCT_Lib.szVideoLane_CaptureImagePath != "")
                {
                    if (!Directory.Exists(TCT_Lib.szVideoLane_CaptureImagePath))
                    {
                        LogInfo.WriteLog("+ Auto create capture image path : " + TCT_Lib.szVideoLane_CaptureImagePath);
                        Directory.CreateDirectory(TCT_Lib.szVideoLane_CaptureImagePath);
                    }
                }

                using (StreamWriter writer = File.CreateText(szCaptureJsonFile))
                {
                    writer.WriteLine(szImageCameraJson);
                }
                //--------------------------------
                //TCT_NVR.MoveFolderCaptureImageTrans(iTransID, iCaptureRunningNo, xImageCameraObj.captureImages);
                //--------------------------------
                var sendImgCamera = new WebService_ImageCamera();
                sendImgCamera.CallWebService_SendImageCaptureCamera(iTransID, _iJobNo, _nTrx, szImageCameraJson, iCaptureRunningNo, xImageCameraObj.captureImages);

                //-----------------
                if (iTransID != 0)
                {
                    TCT_Lib.SendLaneTransObjToRTO(iTransID);
                    if (TCT_Lib.xCurTAG.tagPAN == null)
                    {
                        TCT_Lib.SendLaneTransObjToDC(iTransID);
                        TCT_Lib.SendLaneTransObjToSupAdjust(iTransID);
                    }
                }
            }
            catch (Exception err)
            {
                LogInfo.WriteLog($"- Error create capture image path={TCT_Lib.szVideoLane_CaptureImagePath}, Error={err.Message}");
                LogInfo.WriteLaneEvent($"- Error create capture image path={TCT_Lib.szVideoLane_CaptureImagePath}, Error={err.Message}");
                LogInfo.WriteLogVideoCapture($"- Error create capture image path={TCT_Lib.szVideoLane_CaptureImagePath}, Error={err.Message}");
            }
        }

/*
        protected void SendImageCameraJson_Method_3(int nTrx)
        {
            int iTransID = TCT_Lib.GetLaneTransactionId(TCT_Lib.iJobNo, nTrx, DateTime.Now);
            if (iTransID > 0)
            {
                int _iJobNo = 0, _nTrx = 0;
                var xImageCameraObj = TCT_Obj.GenerateCaptureImageCamera(iTransID, ref _iJobNo, ref _nTrx);
                string szImageCameraJson = JsonConvert.SerializeObject(xImageCameraObj);
                long iCaptureRunningNo = TCT_Lib.LaneDB.GetCaptureRunningNoOfTrans(_iJobNo, _nTrx, iTransID);
                //using (StreamWriter writer  = File.CreateText(Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, $"ImageCamera_Trans{iTransID}_J{_iJobNo}_N{_nTrx}.json")))
                string szCaptureJsonFile = Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, $"ImageCamera_Trans{iTransID}_N{iCaptureRunningNo.ToString("000000")}.json");

                try
                {
                    if (TCT_Lib.szVideoLane_CaptureImagePath != "")
                    {
                        if (!Directory.Exists(TCT_Lib.szVideoLane_CaptureImagePath))
                        {
                            LogInfo.WriteLog("+ Auto create capture image path : " + TCT_Lib.szVideoLane_CaptureImagePath);
                            Directory.CreateDirectory(TCT_Lib.szVideoLane_CaptureImagePath);
                        }
                    }

                    using (StreamWriter writer = File.CreateText(szCaptureJsonFile))
                    {
                        writer.WriteLine(szImageCameraJson);
                    }
                    //--------------------------------
                    //TCT_NVR.MoveFolderCaptureImageTrans(iTransID, iCaptureRunningNo, xImageCameraObj.captureImages);
                    //--------------------------------
                    var sendImgCamera = new WebService_ImageCamera();
                    sendImgCamera.CallWebService_SendImageCaptureCamera(iTransID, _iJobNo, _nTrx, szImageCameraJson, iCaptureRunningNo, xImageCameraObj.captureImages);
                }
                catch (Exception err)
                {
                    LogInfo.WriteLog($"- Error create capture image path={TCT_Lib.szVideoLane_CaptureImagePath}, Error={err.Message}");
                    LogInfo.WriteLaneEvent($"- Error create capture image path={TCT_Lib.szVideoLane_CaptureImagePath}, Error={err.Message}");
                    LogInfo.WriteLogVideoCapture($"- Error create capture image path={TCT_Lib.szVideoLane_CaptureImagePath}, Error={err.Message}");
                }
            }
            else
            {
                LogInfo.WriteLogVideoCapture($"- Error Not found LaneTransId");
            }
        }
*/

        //========================================================================================================
        protected void InitialMtsStatusThread()
        {
            mtsStatusThread                 = new Thread(new ThreadStart(ThreadMtsStatus));
            mtsStatusThread.Priority        = ThreadPriority.Normal;
            mtsStatusThread.IsBackground    = true;
            mtsStatusThread.Start();
        }

        async private void ThreadMtsStatus()
        {
            TCT_MTS.InitialProcessUsage();
            TCT_MTS.CreateGetEdcStatusThread();
            await Task.Delay(5 * 1000);
            while (true)
            {
                try
                {
                    //--- For Testing
                    //await TCT_MTS.SendJsonMsgMtsDataToRabbitMQ();             //--- For Testing
                    //await TCT_MTS_Alarm.SendJsonMsgMtsAlarmToRabbitMQ();    //--- Testing
                    //---------------------------------------------

                    if (TCT_Lib.bMTS_NetworkStatus)
                    {
                        await TCT_MTS.SendJsonMsgMtsDataToRabbitMQ();
                        await TCT_MTS_Alarm.SendJsonMsgMtsAlarmToRabbitMQ();
                    }
                    else
                    {
                        TCT_Lib.bMTS_NetworkStatus = (TCT_Lib.mtsRabbitMQ.ConnectMtsRabbitMQ());
                        if (TCT_Lib.bMTS_NetworkStatus)
                        {
                            await TCT_MTS.SendJsonMsgMtsDataToRabbitMQ();
                            await TCT_MTS_Alarm.SendJsonMsgMtsAlarmToRabbitMQ();
                        }
                        else
                            LogInfo.WriteLogMTS("- Fail Send MTS Data to RabbitMQ : Network Status is not connected");
                    }
                    //------------------------------------------
                    //Thread.Sleep(TCT_Lib.iMTS_TimerSecCheckStatus * 1000);
                    await Task.Delay(TCT_Lib.iMTS_TimerSecCheckStatus * 1000);
                }
                catch (Exception err)
                {
                    LogInfo.WriteLogMTS("- Error Thread MTS : "+err.Message);
                }
            }

            LogInfo.WriteLogMTS("- Fail MTS : End Loop Thread MTS Status");
        }

        //--------------------------------------------------
        static DateTime dtLastTimeKeyHPMC       = DateTime.Now;
        static DateTime dtLastTimeKeyEdit       = DateTime.Now;
        static DateTime dtLastTimeKeyExempt     = DateTime.Now;
        static DateTime dtLastTimeKeyPrint      = DateTime.Now;
        static DateTime dtLastTimeKeyBEOJ       = DateTime.Now;
        static DateTime dtLastTimeKeyEndShift   = DateTime.Now;
        static DateTime dtLastTimeKeyForceReset = DateTime.Now;     // Add feature = 2023-05-09
        static DateTime dtLastTimeKeyClass1     = DateTime.Now;
        static DateTime dtLastTimeKeyClass2     = DateTime.Now;
        static DateTime dtLastTimeKeyCash       = DateTime.Now;
        static DateTime dtLastTimeKeyCoupon     = DateTime.Now;

        protected bool CheckAllowRepeatKeyChar(int iKey)
        {
            int iSpanMilliSec       = TCT_Lib.iKeyStrokeTimeMilliSec;
            if (iKey == (int)KeyPressedType.CLASS_9)    // Key HPMC
            {
                TimeSpan dtTimeSpan     = DateTime.Now.Subtract(dtLastTimeKeyHPMC);
                dtLastTimeKeyHPMC       = DateTime.Now;
                return dtTimeSpan.TotalMilliseconds > iSpanMilliSec;
            }
            if (iKey == (int)KeyPressedType.EDIT)       // Key EDIT
            {
                TimeSpan dtTimeSpan     = DateTime.Now.Subtract(dtLastTimeKeyEdit);
                dtLastTimeKeyEdit       = DateTime.Now;
                return dtTimeSpan.TotalMilliseconds > iSpanMilliSec;
            }
            if (iKey == (int)KeyPressedType.CLASS_7)    // Key Exempt
            {
                TimeSpan dtTimeSpan     = DateTime.Now.Subtract(dtLastTimeKeyExempt);
                dtLastTimeKeyExempt     = DateTime.Now;
                return dtTimeSpan.TotalMilliseconds > iSpanMilliSec;
            }
            if (iKey == (int)KeyPressedType.RECEIPT)    // Key PrintReceipt
            {
                TimeSpan dtTimeSpan     = DateTime.Now.Subtract(dtLastTimeKeyPrint);
                if (dtTimeSpan.TotalSeconds >= 2)
                    dtLastTimeKeyPrint      = DateTime.Now;
                //return dtTimeSpan.TotalMilliseconds > iSpanMilliSec;
                return dtTimeSpan.TotalSeconds >= 2;
            }
            if (iKey == (int)KeyPressedType.BEOJ)    // Key Begin/End of Job
            {
                TimeSpan dtTimeSpan     = DateTime.Now.Subtract(dtLastTimeKeyBEOJ);
                dtLastTimeKeyBEOJ       = DateTime.Now;
                return dtTimeSpan.TotalMilliseconds > iSpanMilliSec;
            }
            if (iKey == (int)KeyPressedType.END_SHIFT)    // Key End Shift repeat in sec 2
            {
                TimeSpan dtTimeSpan     = DateTime.Now.Subtract(dtLastTimeKeyEndShift);
                dtLastTimeKeyEndShift   = DateTime.Now;
                return dtTimeSpan.TotalSeconds >= 2;
            }
            if (iKey == (int)KeyPressedType.FORCE_RESET)   
            {
                TimeSpan dtTimeSpan     = DateTime.Now.Subtract(dtLastTimeKeyForceReset);
                dtLastTimeKeyForceReset = DateTime.Now;
                return dtTimeSpan.TotalMilliseconds > iSpanMilliSec;
            }
            if (iKey == (int)KeyPressedType.CLASS_1)        // Key CLASS 1
            {
                TimeSpan dtTimeSpan     = DateTime.Now.Subtract(dtLastTimeKeyClass1);
                dtLastTimeKeyClass1     = DateTime.Now;
                return dtTimeSpan.TotalMilliseconds > iSpanMilliSec;
            }
            if (iKey == (int)KeyPressedType.CLASS_2)        // Key CLASS 2
            {
                TimeSpan dtTimeSpan     = DateTime.Now.Subtract(dtLastTimeKeyClass2);
                dtLastTimeKeyClass2     = DateTime.Now;
                return dtTimeSpan.TotalMilliseconds > iSpanMilliSec;
            }
            if (iKey == (int)KeyPressedType.PAY_CASH)       // Key Pay Cash
            {
                TimeSpan dtTimeSpan     = DateTime.Now.Subtract(dtLastTimeKeyCash);
                dtLastTimeKeyCash       = DateTime.Now;
                return dtTimeSpan.TotalMilliseconds > iSpanMilliSec;
            }
            if (iKey == (int)KeyPressedType.PAY_COUPON)     // Key Pay Coupon
            {
                TimeSpan dtTimeSpan     = DateTime.Now.Subtract(dtLastTimeKeyCoupon);
                dtLastTimeKeyCoupon     = DateTime.Now;
                return dtTimeSpan.TotalMilliseconds > iSpanMilliSec;
            }



            return true;
        }
        //-----------------------------------------------

        private void TestETC()
        {
            string szJsonStr = "{  \"header\": {    \"sequenceNumber\": \"167330\",    \"location\": {      \"country\": \"TH\",      \"provider\": 8888,      \"plaza\": 42,      \"stationType\": \"OPEN_STATION\",      \"lane\": 14    },    \"timestamp\": \"2022-05-03T22:49:23.476645Z\"  },  \"laneMode\": \"NORMAL_MODE\",  \"albMode\": \"ALB_NORMAL\",  \"inputDevices\": {    \"values\": [      0,      0,      0,      0,      0,      0,      0,      0,      0,      0,      0,      0,      0,      0,      0,      0    ]  },  \"outputDevices\": {    \"values\": [      0,      0,      0,      0,      0,      0,      0,      0,      0,      0,      0,      0,      0,      0,      0,      0    ]  },  \"tfi\": {    \"state\": 1,    \"pattern\": 13,    \"vehicleClass\": 0,    \"fare\": \"0\",    \"text1\": \"\",    \"text2\": \"\"  },  \"crs\": {    \"vehicleClass\": 0,    \"paymentMethod\": \"UNKNOWN_PAYMENT_METHOD\"  },  \"mainReader\": {    \"active\": true  },  \"secondaryReader\": {    \"active\": false  },  \"systemAlarms\": {    \"overallStatus\": \"OK\",    \"subsystemAlarms\": [      {        \"name\": \"Antenna\",        \"status\": \"OK\",        \"message\": \"\"      },      {        \"name\": \"PresenceDetection\",        \"status\": \"OK\",        \"message\": \"\"      },      {        \"name\": \"PassageDetection\",        \"status\": \"OK\",        \"message\": \"\"      },      {        \"name\": \"Display\",        \"status\": \"OK\",        \"message\": \"\"      },      {        \"name\": \"Camera\",        \"status\": \"OK\",        \"message\": \"\"      },      {        \"name\": \"FileSystem\",        \"status\": \"OK\",        \"message\": \"\"      },      {        \"name\": \"Memory\",        \"status\": \"OK\",        \"message\": \"\"      },      {        \"name\": \"LaneController\",        \"status\": \"OK\",        \"message\": \"\"      },      {        \"name\": \"Antenna2\",        \"status\": \"OK\",        \"message\": \"\"      }    ]  },  \"vehicleQueue\": {    \"vehicle\": [      {        \"paymentStatus\": \"OBU_NOT_PRESENT\",        \"vehicleClass\": 0,        \"leaving\": false,        \"entryTime\": \"2022-05-03T04:57:15.190752Z\"      }    ]  }}";
            object objLaneState = JsonConvert.DeserializeObject(szJsonStr);

            EtcRabbitMQ_LaneStateFromEtcRabbitMQ(objLaneState);
        }




    }
}
