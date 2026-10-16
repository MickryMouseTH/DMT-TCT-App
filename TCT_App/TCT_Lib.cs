using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using Newtonsoft.Json;
using TCT_App.RabbitMQ;
using TCT_App.LSD;
using System.IO.Compression;
using Gtk;
using System.Security.Principal;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using TCT_App.WebServiceAPI;
using Renci.SshNet;

namespace TCT_App
{

    public enum StationType
    {
        ENTRY       = 1,        // Entry Station = Inbound
        EXIT        = 2,        // Exit Station = Outbound
        OPEN        = 3,        // Open System
    }

    public enum LaneType
    {
        MTC         = 0,
        ETC         = 1,
        DUAL        = 2,
    }

    public enum LaneMode
    {
        CLOSED      = 0,
        NORMAL      = 1,
        HPMC        = 2,
        MAINTENANCE = 3,
    }

    public enum LaneDirection
    {
        NORMAL      = 0,
        REVERSE     = 1,
    }

    public enum KeyBoardType
    {
        LC          = 0,     // IO of Lane Controller
        VK          = 1,     // Virtual Keyboard on Main Screen
    }

    public enum VehicleType
    {
        NONE            = -1,
        VIOLATION       = 0,
        CLASS_1         = 1,
        CLASS_2         = 2,
        CLASS_3         = 3,
        CLASS_4         = 4,
        //CLASS_5         = 5,

        EMERGENCY       = 5,
        TOWING          = 6,
        EXEMPT          = 7,
        LEFT_EXIT       = 8,
        HPMC            = 9,
    }
/*
    public enum PayType
    {
        NONE            = 0,
        CASH            = 1,
        COUPON          = 2,
        MPass           = 3,
        EasyPass        = 4,
        EMV             = 5,    // EDC, Credit Card
        QrCode          = 6,
        DMT_CARD        = 7,
        EXEMPT          = 8,
        HPMC            = 9,
        LeftExit        = 10,
        EMERGENCY       = 11,
    }
*/    

    public enum PayType
    {
        NONE            = 0,
        CASH            = 1,
        COUPON          = 2,
        EMV             = 3,    // EDC, Credit Card
        QrCode          = 4,
        EasyPass        = 5,
        MPass           = 6,
        EXEMPT          = 7,
        DMT_CARD        = 8,
        Violation       = 9,
        HPMC            = 10,
        LeftExit        = 11,
        ePurse          = 12,
        Mangmoom        = 13,
        //EMERGENCY       = 11,
        PM6             = 14,
        PM7             = 15,
        PM8             = 16,
        PM9             = 17 
    }

    public enum KeyPressedType
    {
        NONE            = -1,
        VIOLATION       = 0,    // Violation
        CLASS_1         = 1,    // 4 Wheel
        CLASS_2         = 2,    // More than 4 Wheel
        CLASS_3         = 3,
        CLASS_4         = 4,
        CLASS_5         = 5,    // Emergency        // Not Used
        CLASS_6         = 6,    // Towing
        CLASS_7         = 7,    // Exempt
        CLASS_8         = 8,    // Left Exit
        CLASS_9         = 9,    // HP&MC
        PAY_CASH        = 10,
        PAY_COUPON      = 11,
        PAY_DMTCARD     = 12,   // EXEMPT Key
        //PAY_LEFTOUT     = 13,   // MIS Key
        RECEIPT         = 14,
        STOP_BUZZ       = 15,
        SIM_PRESS       = 16,
        SIM_PASS        = 17,
        PRINT_CONVOY    = 18,   // VIDEO     
        SALE_COUPON     = 19,
        RECHARGE_TAG    = 20,
        FORCE_RESET     = 21,
        OTL_CLOSE       = 22,
        OTL_OPEN        = 23,
        START_SHIFT     = 24,
        END_SHIFT       = 25,
        GATE            = 26,
        NUM_0           = 27,
        NUM_1           = 28,
        NUM_2           = 29,
        NUM_3           = 30,
        NUM_4           = 31,
        NUM_5           = 32,
        NUM_6           = 33,
        NUM_7           = 34,
        NUM_8           = 35,
        NUM_9           = 36,
        ESC             = 37,
        DIV             = 38,   // '/'
        MUL             = 39,   // '*'
        SUB             = 40,   // '-'
        ADD             = 41,   // '+'
        DOT             = 42,   // '.'
        ENTER           = 43,
        TAB             = 44,
        BACKSPACE       = 45,
        SHARP           = 46,   // '#'
        F1              = 47,
        F2              = 48,
        F3              = 49,
        F4              = 50,
        MODE            = 51,
        MANUAL          = 52,
        BEOJ            = 53,
        CONFIRM         = 54,
        PAY_EMV         = 55,
        PAY_QRCODE      = 56,
        LEFT            = 57,
        RIGHT           = 58,
        UP              = 59,
        DOWN            = 60,
        END             = 61,
        EDIT            = 62,
        EXIT_APP        = 63,

        PAY_PM6         = 64,
        PAY_PM7         = 65,
        PAY_PM8         = 66,
        PAY_PM9         = 67,

    }
  
/*
    public enum KeyPressedType
    {
        NONE            = -1,
        VIOLATION       = 0,        // Violation
        CLASS_1         = 0x16,     // 4 Wheel
        CLASS_2         = 0x2F,     // More than 4 Wheel
        CLASS_3         = -1,
        CLASS_4         = -1,
        CLASS_5         = 0x15,     // Emergency
        CLASS_6         = 0x30,     // Towing
        CLASS_7         = 0x2C,     // Exempt
        CLASS_8         = 0x2D,     // DMT-Allowed
        CLASS_9         = 0x21,     // HP&MC
        PAY_CASH        = 0x2C,
        PAY_COUPON      = 0x13,
        EXEMPT          = -1,
        MIS             = 0x31,
        RECEIPT         = 0x12,
        STOP_BUZZ       = -1,
        SIM_PRESS       = 0x17,
        SIM_PASS        = -1,
        VIDEO           = -1,
        SALE_COUPON     = 0x26,
        RECHARGE_TAG    = -1,
        FORCE_RESET     = 0x1F,
        OTL_CLOSE       = -1,
        OTL_OPEN        = -1,
        START_SHIFT     = 0x24,
        END_SHIFT       = 0x24,
        GATE            = -1,
        NUM_0           = 0x52,
        NUM_1           = 0x4F,
        NUM_2           = 0x50,
        NUM_3           = 0x51,
        NUM_4           = 0x4B,
        NUM_5           = 0x4C,
        NUM_6           = 0x4D,
        NUM_7           = 0x47,
        NUM_8           = 0x48,
        NUM_9           = 0x49,
        ESC             = -1,
        DIV             = -1,       // '/'
        MUL             = -1,       // '*'
        SUB             = -1,       // '-'
        ADD             = -1,       // '+'
        DOT             = 0x53,     // '.'
        ENTER           = 0xE01C,
        TAB             = -1,
        BACKSPACE       = 0x0E,
        SHARP           = -1,       // '#'
        F1              = -1,
        F2              = -1,
        F3              = -1,
        F4              = -1,
        MODE            = 0x20,
        MANUAL          = 0x22,
        BEOJ            = 0x24,
        CONFIRM         = -1,
        EMV             = 55,
        QRCODE          = 56,
        LEFT            = 0xE04B,
        RIGHT           = 0xE04D,
        UP              = 0x0E48,
        DOWN            = 0x0E50,
        END             = 0xE04F,
        EDIT            = 62,
        EXIT_APP        = -1
    }
*/

    public enum TFI_Mode
    {
        Day                 = 0,
        Night               = 1,
    }

    public enum TFI_State
    {
        StartSceen          = 0,
        CloseLane           = 1, 
        Maintenance         = 2,
        Welcome             = 3,
        Class_Fare          = 4,
        Class_Fare_Payment  = 5,
        ETC_Balance         = 6,
        Alert_ETC_No_TAG    = 7,
        Topup               = 8,
        ThankYou            = 9,
        ScreenOff           = 10,
        HPMC                = 11,
    }

    public enum TFI_Pattern
    {
        Normal              = 0,
        HPMC                = 5,
        Violation           = 9,
    }

    public enum Cmd_DO_Type
    {
        RegisterBell        = 0,
        Siren               = 1,
        ASB                 = 2,
        LTL                 = 3,
        ALB_Open            = 4,
        ALB_Close           = 5,
        OTL_RedGreen        = 6,
        OTL_MTC_ETC         = 7,
        OTL_DualMode        = 8,
        CRS_Reset           = 12,
        TFI_Reset           = 13,
        Loop_Reset          = 14,
        IPC_Reset           = 15,
    }

    public enum Cmd_DI_Type
    {
        MLB                 = 0,
        EFS                 = 1,
        PresenceLoop        = 4,        // For Test (2)
        PassageLoop         = 3,
        PresenceOB          = 2,        // For Test (4)
        PassageOB           = 5,
        ALB                 = 6,
        OTL                 = 7,
        LTL                 = 8,
        EthErr              = 9,
        Siren               = 10,
        TreadleError        = 13,
        TreadleC            = 14,
        TreadleD            = 15,
    }

    public enum EventTypeHQ
    {
        JOB                 = 1,        // BOJ, EOJ
        OTL                 = 2,        // GREEN, RED
        HPMC                = 3,        // START, STOP
        PASSAE_OB           = 4,        // ON, OFF
        PASSAGE_LOOP        = 5,        // ON, OFF
        AVC                 = 6,        // ON, OFF
        EDC                 = 7,        // CONNECT, DIS-CONNECT
        LSD                 = 8,        // CONNECT, DIS-CONNECT
        MAIN_ANTENNA        = 9,        // CONNECT, DIS-CONNECT
    }

    public enum LoginMethod
    {
        AUTO                = 0,        // Touch Smart Card
        MANUAL              = 1,        // Manual Input Keyboard
    }

    public enum EventTypeRTO
    {
        DEVICE              = 1,
    }

    public enum AlarmDevice
    {
        AVC             = 1,
        ETHERNET        = 2,
        EFS             = 3,        // FOOT SWITCH
        LANE_IO         = 4,
        ETC_CLEARQUEUE  = 5,
        CHANGE_LANETYPE = 6,
        EDC_WARNING     = 7,
        RE_PAYMENT      = 8,
        FORCE_RESET     = 9,
        LCSD_RESET      = 10,
    }

    public enum AlarmType
    {
        Occurred        = 0,
        Recovered       = 1,
    }

    public enum TowingState
    {
        None            = 0,
        StartTowing     = 1,
        FirstClassType  = 2,
        FirstPayType    = 3,
        SecClassType    = 4,
        SecPayType      = 5,
        CompleteTowing  = 6,
    }

    public enum AppPage
    {
        Login           = 1,
        Main            = 2,
        EMV             = 3,
        PayConvoy       = 4,
        SalesCoupon     = 5,
        Maintenance     = 6,
        SetLaneTypeMode = 7,
        DmtPayment      = 8,
    }

    public enum ChkDeviceType
    {
        Device_LC       = 0,
        Device_RTO      = 1,
        Device_EMV      = 2,
        Device_DC       = 3,
    }

    public enum PrintReceiptType
    {
        FareType        = 0,
        SalesCoupon     = 1,
    }

    public enum ALB_Mode
    {
        Normal          = 0,
        Open            = 1,
        Closed          = 2,
    }

    public enum LoginType
    {
        SmartCard       = 0,
        Manual          = 1,
        RTO_Remote      = 2,
        ETC_Auto        = 3,
        Maintenance     = 4,
    }

    public enum CameraPosition
    {
        FrontCamera     = 1,
        RearCamera      = 2,
        ThirdCamera     = 3,
    }

    public enum LsdMode
    {
        LSD_CLOSE       = 0,
        LSD_MTC         = 1,
        LSD_ETC         = 2,
        LSD_DUAL        = 3,      
    }

    //--------------------------------------------------
    public static class DateTimeExtension
    {
        private static CultureInfo _fmt = null;
        /// <summary>
        /// Gets Thai CultureInfo.
        /// </summary>
        public static CultureInfo ThaiCultureInfo
        {
            get
            {
                if (null == _fmt)
                {
                    _fmt = new CultureInfo("th-TH");
                }
                return _fmt;
            }
        }
    }

    public class objLoopInfo
    {
        public int iTC_Class;
        public Pay_Info xPayment;
        public decimal fFareAmount;
        public string szTransRemark;
    }

    public class objQueueInfo
    {
        public int iTC_Class;
        public Pay_Info xPayment;
        public decimal fFareAmount;
        public string szTransRemark;
    }

    //-------------------------------------------------------------
    public class TCT_Lib
    {
        public static string szAppVersion               = "2.0.5";
        public static string szAppBuild                 = "2026/10/07 18:00:00";         // 2.0.3 GUI review fixes; 2.0.2 keyboard + GUI restyle; 2.0.1 force reset / OB-only fix; 2.0.0 => "2026/06/02 00:00:00"; 1.5.7 => "2025/09/23 01:05:06";
        public static string szSysUserName              = WindowsIdentity.GetCurrent().Name;
        public static string szSysUserName2             = Environment.UserName;
        //--- Configuration
        public static dynamic xTctConfig;
        //public static string baseLogFolder              = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "log");
        //public static string baseLogFolder              = $"/home/{szSysUserName}/tct/log";
        //public static string baseConfigFolder             = Path.Combine(Environment.GetEnvironmentVariable("HOME"), "TCT/log");
        //public static string baseConfigFolder           = $"/home/{szSysUserName}/tct/config";
        public static string baseConfigFolder           = ConfigInfo.GetConfigPath();

        //--- Lane Info
        public static int iLaneID                       = 0;
        public static string szLaneCode                 = "";
        public static string szLaneName                 = "";
        public static int iLaneConfigType               = 0;            // LaneType from Config 0=MTC, 1=ETC, 2=Dual
        public static LaneType eCurLaneType             = LaneType.MTC; // Current State ofLaneType 
        public static LaneMode eCurLaneMode             = LaneMode.CLOSED;
        public static LaneDirection eLaneDirection      = LaneDirection.NORMAL;
        public static bool bAllowETC                    = false;

        //--- Plaza Info
        public static string szPlazaID                  = "yy";
        public static string szPlazaCode                = "zzz";
        public static string szPlazaName                = "xxx";
        public static string szTSBId                    = "xxx";
        public static int iProviderId                   = 10;

        //--- Supervisor Info
        public static string szSupervisorID             = "";
        public static string szSupervisorCode           = "";
        public static string szSupervisorName           = "";
        public static string szSupervisorGroupID        = "";
        public static string szSupervisorGroupName      = "";
        public static int iSupervisorShiftID            = 0;
        public static DateTime dtSupervisorStartShift   = DateTime.Now;
        public static string szRTO_Version              = "0.4.5";

        //--- Staff Info
        public static string szStaffID                  = "";
        public static string szStaffCode                = "";
        public static string szStaffName                = "";
        public static string szStaffGroupID             = "";
        public static string szStaffGroupName           = "";
        public static int iStaffShiftID                 = 0;
        public static DateTime dtStaffStartShift        = DateTime.Now;

        //--- General Setting
        public static int iStopSirenAfterTimeSec            = 5;
        public static int iTimerSecAutoAVC                  = 3;        // Wait timer in sec, then send auto AVC
        public static string szDayTimeMode                  = "06:00";  // OFF light time of TFI, CRS
        public static string szNightTimeMode                = "18:00";  // ON light time of TFI, CRS
        public static int iTimerAutoResendTrans             = 180;      // Timer in sec for auto resend transaction
        public static int iSaveLogEtcRabbitMQ               = 0;
        public static int iTimerSecCheckDeviceStatus        = 60;       // Timer in sec for check device status
        public static string szLogPath                      = "";       // Log Path
        public static int iKeepLogMonth                     = 3;        // Default Keep Log 3 Months
        public static int iKeepDbDay                        = 30;       // v2.0.5: Default Keep Database 30 days (DC data: only after sent to DC)
        public static int iDevMode                          = 0;        // Default 0=Production Mode, >1 = Dev Mode
        public static int iKeyStrokeTimeMilliSec            = 300;      // Defaullt 300 ms.
        public static int iAvcStatusType                    = 0;        // 0=Get AVC Status from LCSD Socket (Default), 1=Get AVC Status from H/W
        public static string szManualImportPath             = "";       // Path for manual import data when off-line (Staff, Tariff, Promotion)
        public static int iDmtCardType                      = 0;        // 0=Use paper dmt card, 1=Use touch dmt smart card
        public static string szDmtCardEffectiveDate         = "";       // Effective Date of DMT Card (Default = "" is not use effective date)
        public static int iImportStaffIntervalMin           = 5;        // Timer in minute for import staff from RabbitMQ DC
        public static string szAllowTollRoleGroupID         = "";       // Allow login for Role GroupID of Toll Collector staff
        public static string szAllowMaintenanceRoleGroupID  = "";       // Allow maintenance for Role GroupID of Maintenance staff

        //--- LCSD Socket Setting
        public static string szLCSD_SocketIPAddress         = "127.0.0.1";
        public static int iLCSD_SocketPort                  = 58084;
        public static int iLCSD_DI_0                        = 0;
        public static int iLCSD_DI_1                        = 1;
        public static int iLCSD_DI_2                        = 2;
        public static int iLCSD_DI_3                        = 3;
        public static int iLCSD_DI_4                        = 4;
        public static int iLCSD_DI_5                        = 5;
        public static int iLCSD_DI_6                        = 6;
        public static int iLCSD_DI_7                        = 7;
        public static int iLCSD_DI_8                        = 8;
        public static int iLCSD_DI_9                        = 9;
        public static int iLCSD_DI_10                       = 10;
        public static int iLCSD_DI_11                       = 11;
        public static int iLCSD_DI_12                       = 12;
        public static int iLCSD_DI_13                       = 13;
        public static int iLCSD_DI_14                       = 14;
        public static int iLCSD_DI_15                       = 15;
       
        //--- LCIO Socket Setting
        public static string szLCIO_SocketIPAddress         = "127.0.0.1";
        public static int iLCIO_SocketPort                  = 18084;
            
        //--- RTO Socket Setting
        public static string szRTO_IPAddress                = "";
        public static int iRTO_SocketPort                   = 9094;

        //--- Video NVR Setting
        public static bool bEnableVideoNVR                              = false;
        public static string szVideoNVR_FrontCaptureUrl                 = "";
        public static string szVideoNVR_RearCaptureUrl                  = "";
        public static string szVideoNVR_ThirdCaptureUrl                 = "";
        public static string szVideoNVR_OutputPathPrefix                = "";
        public static string szVideoNVR_OutputPathSuffix                = "";
        public static int iVideoNVR_Img3TimeLapseMilliSec               = 2000;     // Default TimeLapse of 3nd image = 2000 ms. (2 Sec)
        public static int iVideoNVR_CaptureImageMethod                  = 0;        // 0 (Default)=Use Time Lapse, 1=Use Event (Payment and Loop), 2=Use Loop+OB Event
        public static string szVideoNVR_WebApiBaseUrl                   = "";
        public static int iVideoNVR_TowingTimeLapseMillisec             = 3000;     // Default TimeLapse of Towing = 3000 ms. (3 Sec)
        public static int iVideoNVR_TimeoutSec                          = 10;       // Defaukt WebRequest Timeout = 10 sec.
        public static int iVideoNVR_CachePolicy                         = 6;        // CachePolicy => 0-8 Default=6 (NoCacheNoStore)
        public static int iVideoNVR_AutoKeepDayImageFile                = 2;        // 0=No delete file, -1=Delete when send to server successs, >0=Keep and delete from today date
        public static int iVideoNVR_ResendFileDay                       = 1;        // 0=Resend all files, >0 Resend back days=x day from today
        public static int iVideoNVR_ResizeImage                         = 0;        // 0=Not resize image, 1=Resize image before save image
        public static int iVideoNVR_ResizeWidth                         = 1280;     // v2.0.3: resize target (was fixed 1024x576), keeps aspect ratio, never upscales
        public static int iVideoNVR_ResizeHeight                        = 720;
        public static int iVideoNVR_JpegQuality                         = 85;       // v2.0.3: 1-100 (was GDI+ default ~75)
        // HttpRequestCacheLevel.Default = 0, BypassCache=1, CacheOnly=2, CacheIfAvailable=3, Revalidate=4, Reload=5, NoCacheNoStore=6, CacheOrNextCacheOnly=7, Refresh=8

        //--- Video Lane Capture Setting
        public static string szVideoLane_CaptureImagePath               = "";
        public static string szVideoLane_FrontCameraUrl                 = "";
        public static string szVideoLane_RearCameraUrl                  = "";
        public static string szVideoLane_ThirdCameraUrl                 = "";
        public static string szVideoLane_FrontCamera_UserName           = "";
        public static string szVideoLane_FrontCamera_Password           = "";
        public static string szVideoLane_RearCamera_UserName            = "";
        public static string szVideoLane_RearCamera_Password            = "";
        public static string szVideoLane_ThirdCamera_UserName           = "";
        public static string szVideoLane_ThirdCamera_Password           = "";
        public static string szVideoLane_IPCamera1                      = "";
        public static string szVideoLane_IPCamera2                      = "";

        //--- ETC RabbitMQ Setting
        public static bool bEnableEtcRabbitMQ                           = false;
        public static string szETC_RabbitMQ_ServerHostName              = "";
        public static string szETC_RabbitMQ_VirtualHost                 = "";
        public static int iETC_RabbitMQ_Port                            = 5672;
        public static string szETC_RabbitMQ_User                        = "";
        public static string szETC_RabbitMQ_Password                    = "";
        public static string szETC_RabbitMQ_LaneStateQueueName          = "";
        public static string szETC_RabbitMQ_LaneStateExchangeName       = "";
        public static string szETC_RabbitMQ_LaneStateRoutingKey         = "";
        public static string szETC_RabbitMQ_CompilePassageQueueName     = "";
        public static string szETC_RabbitMQ_CompilePassageExchangeName  = "";
        public static string szETC_RabbitMQ_CompilePassageRoutingKey    = "";
        public static string szETC_RabbitMQ_ManualActionQueueName       = "";
        public static string szETC_RabbitMQ_ManualActionExchangeName    = "";
        public static string szETC_RabbitMQ_ManualActionRoutingKey      = "";
        public static string szETC_RabbitMQ_ServerIPAddress             = "";
        public static int iETC_RabbitMQ_ServerPort                      = 80;
        public static bool bETC_RabbitMQ_WriteLogETC                    = false;

        //--- DC RabbitMQ Setting
        public static bool bEnableDcRabbitMQ                            = false;
        public static string szDC_RabbitMQ_ServerHostName               = "";
        public static string szDC_RabbitMQ_VirtualHost                  = "/";
        public static int iDC_RabbitMQ_Port                             = 5672;
        public static string szDC_RabbitMQ_User                         = "";
        public static string szDC_RabbitMQ_Password                     = "";
        public static string szDC_RabbitMQ_TransQueueName               = "";
        public static string szDC_RabbitMQ_TransExchangeName            = "";
        public static string szDC_RabbitMQ_TransRoutingKey              = "";
        public static string szDC_RabbitMQ_EventQueueName               = "";
        public static string szDC_RabbitMQ_EventExchangeName            = "";
        public static string szDC_RabbitMQ_EventRoutingKey              = "";
        public static string szDC_RabbitMQ_ParamsQueueName              = "";
        public static string szDC_RabbitMQ_ParamsExchangeName           = "";
        public static string szDC_RabbitMQ_ParamsRoutingKey             = "";
        public static string szDC_RabbitMQ_ServerIPAddress              = "";
        public static int iDC_RabbitMQ_ServerPort                       = 80;
        public static int iDC_RabbitMQ_TimeoutSec                       = 1;

        //--- MTS RabbitMQ Setting
        public static bool bEnableMTS                                   = true;
        public static int iMTS_TimerSecCheckStatus                      = 5;       // Timer in sec for check MTS status
        public static string szMTS_RabbitMQ_ServerHostName              = "";
        public static string szMTS_RabbitMQ_VirtualHost                 = "";
        public static int iMTS_RabbitMQ_Port                            = 5672;
        public static string szMTS_RabbitMQ_User                        = "";
        public static string szMTS_RabbitMQ_Password                    = "";
        public static string szMTS_RabbitMQ_MtsQueueName                = "";
        public static string szMTS_RabbitMQ_MtsExchangeName             = "";
        public static string szMTS_RabbitMQ_MtsRoutingKey               = "";
        public static string szMTS_RabbitMQ_ServerIPAddress             = "";
        public static int iMTS_RabbitMQ_ServerPort                      = 80;
        public static string szMTS_EtcStatusDbUrl                       = "";
        public static string szMTS_EtcStatusDbUrl2                      = "";   // UrlXMR1  = http://10.0.0.10/XMRFirst/MuninBrowse/StatusDB
        public static string szMTS_EtcStatusDbUrl3                      = "";   // UrlXMR2  = http://10.0.0.10/XMRSecond/MuninBrowse/StatusDB
        public static string szMTS_EtcStatusDbUrl4                      = "";   // 
        public static string szMTS_AlarmDeviceList                      = "";   // 5,7  5=ETC_CLEARQUEUE, 7=EDC_WARNING

        //--- MTS Monitor Setting
        public static string szMTS_Monitor_DiskPath_01                  = "";
        public static string szMTS_Monitor_DiskPath_02                  = "";
        public static string szMTS_Monitor_DiskPath_03                  = "";
        public static string szMTS_Monitor_DiskPath_04                  = "";
        public static string szMTS_Monitor_DiskPath_05                  = "";
        public static string szMTS_Monitor_DiskPath_06                  = "";
        public static string szMTS_Monitor_DiskPath_07                  = "";
        public static string szMTS_Monitor_DiskPath_08                  = "";
        public static string szMTS_Monitor_DiskPath_09                  = "";
        public static string szMTS_Monitor_DiskPath_10                  = "";
        public static string szMTS_Monitor_TFI_IPAddress                = "";
        public static int iMTS_Monitor_TFI_Port                         = 80;
        public static string szMTS_Monitor_CRS_IPAddress                = "";
        public static int iMTS_Monitor_CRS_Port                         = 80;
        //--- Get Product of USB Device from command = usb-devices
        public static string szMTS_Monitor_Product_Printer              = "TM-T88VI";
        public static string szMTS_Monitor_Product_SCR                  = "SL600-NFC Reader";
        public static string szMTS_Monitor_Product_Keyboard             = "SPOS";
        public static int iMTS_Monitor_TimeAlertLoopNoOB                = 0;    // Set Qty. time of Loop and No OB, then alert to MTS
        public static int iMTS_Monitor_IntervalSecAlertLoopNoOB         = 10;   // Set interval timer (sec.) for check Loop and No OB, then alert to MTS
        public static int iMTS_Monitor_IntervalSecGetEtcStatus          = 60;   // Set interval timer (sec.) for check status ETC status data thread.

        //--- Lane WebService Setting
        public static bool bEnableLaneWebService                        = false;
        public static string szLaneWebService_BaseURL                   = "";
        public static string szLaneWebService_ServerIPAddress           = "";
        public static int iLaneWebService_ServerPort                    = 3000;
        public static int iLaneWebService_ChkStartJobTOD                = 0;

        //--- Printer Setting
        public static string szPrinter_PrinterPort          = "/dev/usb/lp0";
        public static int iPrinter_PrintFontSize            = 1;    // 1=Small, 2=Normal
        public static int iPrinter_PrintFooter              = 0;    // 0=No print (Default), 1=BuzzeBees
        public static int iPrinter_PrintQrSize              = 5;    // 5 is Default

        //--- EMV Setting
        public static bool bEnableEdc                       = false;
        public static string szEdcIPAddress                 = "";
        public static int iEdcSendPort                      = 2019;
        public static int iEdcRecvPort                      = 2018;
        public static string szEdcTID                       = "";
        public static int iEdcQrWaitTimeSec                 = 10;
        public static int iEdcStartHourClearBatch           = 0;       // if Start=0, End=0, Clear all time when EOJ
        public static int iEdcEndHourClearBatch             = 0;
        public static int iEdcMinuteIntervalClearBatch      = 0;
        public static int iEdcWaitTimeOutSec                = 5;
        public static int iEdcRetryTimeOut                  = 2;
        public static int iEdcVersionType                   = 0;        // 0=First version (Default), 1=New version
        public static int iEdcChkNetworkType                = 0;        // 0=Chk network by Open send port (Default), 1=Chk network by call fn Handshaking EDC
        public static int iEdcChkKeepAliveIntervalMin       = 5;
        public static int iEdcRelayMilliSecBeforeAck        = 15;       // RelayTime ms before send Ack

        //--- SupAdjust Socket Setting
        public static bool bEnableSupAdjust                 = false;
        public static string szSupAdjust_IPAddress          = "";
        public static int iSupAdjust_SocketPort             = 9095;

        //--- LSD Setting
        public static bool bEnableLSD                       = false;
        public static string szLSD_IPAddress                = "";
        public static int iLSD_SocketPort                   = 16603;
        public static int iLSD_KeepAliveSec                 = 0;
        public static int iLSD_MethodType                   = 0;        // 0=Send Command from TCT, 1=Send Command from RTO
        public static bool bLSD_SendWhenPressOTL            = true;     // 0=No send LSD, 1=Send LSD command when press OTL ON/OFF
        public static int iLSD_RetryWaitSec                 = 2;
        public static int iLSD_RetryTime                    = 2;

        //--- Antenna Setting
        public static bool bEnableAntenna                   = false;
        public static string szMainAntenna_IPAddress        = "";
        public static int iMainAntenna_SocketPort           = 80;
        public static string szSecondaryAntenna_IPAddress   = "";
        public static int iSecondaryAntenna_SocketPort      = 80;

        //--- Payment Setting
        public static string szPaymentCode_06               = "PM6";
        public static string szPaymentName_06               = "";
        public static string szPaymentBtn_06                = "";
        public static string szPaymentCRS_06                = "6";
        public static string szPaymentCode_07               = "PM7";
        public static string szPaymentName_07               = "";
        public static string szPaymentBtn_07                = "";
        public static string szPaymentCRS_07                = "7";
        public static string szPaymentCode_08               = "PM8";
        public static string szPaymentName_08               = "";
        public static string szPaymentBtn_08                = "";
        public static string szPaymentCRS_08                = "8";
        public static string szPaymentCode_09               = "PM9";
        public static string szPaymentName_09               = "";
        public static string szPaymentBtn_09                = "";
        public static string szPaymentCRS_09                = "9";

        //--- TFI Setting
        public static bool bEnableTfiScpFiles               = false;
        public static string szTfiScpHostName               = "";
        //public static string szTfiScpSourcePath             = "";
        public static string szTfiScpDestPath               = "";
        public static string szTfiScpUserName               = "";
        public static string szTfiScpPassword               = "";

        //------------------------------------------------

        //--- Theme Color
        public static Gdk.Color clLabelHeader               = new Gdk.Color(0, 255, 255);

        public static string szDateTimeFormatISO            = "yyyy-MM-ddTHH:mm:ss.fffZ";
        public static string szDateTimeFormatMOC            = "yyyy-MM-ddTHH:mm:ss.fff+07";
        public static string szDateTimeFormatDB             = "yyyy-MM-dd HH:mm:ss.fff";
        public static string szDateFormatDB                 = "yyyy-MM-dd";
        public static bool bMainUiReady                     = false;
        public static bool bEntryLoop                       = false;
        public static bool bExitLoop                        = false;
        public static int iTotalQueue                       = 0;
        public static int nTrx                              = 1;    // Running No of transaction in each JobNo
        public static int iJobNo                            = 1;    // Job No = 1 when after 6:00 of each day, Maintenance mode with start Job No = 901
        public static DateTime dtTctStartDateTime           = DateTime.Now;

        public static AppPage eCurAppPage                   = AppPage.Login;
        public static AVC_Info xCurAVC                      = new AVC_Info();
        public static Pay_Info xCurPayment                  = new Pay_Info();
        public static TAG_Info xCurTAG                      = new TAG_Info();
        public static EDC_Info xCurEDC                      = new EDC_Info();
        public static EMV_Info xCurEMV                      = new EMV_Info();
        public static EMV_Info xCurQR                       = new EMV_Info();
        //public static EDC_Info xTmpEDC                      = null;
        //public static EMV_Info xTmpEMV                      = null;
        //public static EMV_Info xTmpQR                       = null;
        public static decimal fFareClass1                   = 0;        // Default = 80
        public static decimal fFareClass2                   = 0;        // Default = 110
        public static int iCurTC_Class                      = 0;
        public static decimal fCurFareAmount                = 0;
        public static string szCurTransId                   = "";       // For ETC RabbitMQ (MTC Passage Payment)
        public static int iPrevTC_Class                     = 0;        // When Edit, Prev will save old TC_Class for ETC
        public static int iPrevPayType                      = 0;
        public static decimal fPrevFareAmount               = 0;        // When Edit, Prev will save old Fare Amount for ETC
        public static string szPrevTransId                  = "";
        public static string szCurTransRemark               = "";
        public static string szCurFareReceiptNo             = "";
        public static string szCurSalesCouponReceiptNo      = "";
        public static int iIsTowing                         = 0;
        public static int iTowingParentID                   = 0;
        public static AVC_Info xTowingAVC                   = new AVC_Info();
        public static Pay_Info xTowingPayment               = new Pay_Info();
        public static EDC_Info xTowingEDC                   = new EDC_Info();
        public static EMV_Info xTowingEMV                   = new EMV_Info();
        public static EMV_Info xTowingQR                    = new EMV_Info();
        public static EDC_Info xTmpTowingEDC                = null;
        public static EMV_Info xTmpTowingEMV                = null;
        public static EMV_Info xTmpTowingQR                 = null;
        public static int iTowingTC_Class                   = 0;
        public static int iNextTC_Class                     = 0;
        public static decimal fTowingFareAmount             = 0;
        //public static int iEmvDetailID                      = 0;
        public static bool bStartEndLoop                    = false;
        public static bool bAfterPayment                    = false;
        public static bool bHasGenReceiptNo                 = false;
        public static bool bHasGenTowingReceiptNo           = false;
        public static TowingState eTowingState              = TowingState.None;    // 0=None, 1=Start towing, 2=Fist towing, 3=First pay type, 4=Sec vehicle, 5=Sec pay type
        public static bool bConvoyMode                      = false;
        public static int iConvoyClass1                     = 0;
        public static int iConvoyClass2                     = 0;
        public static int iTotalConvoyClass1                = 0;
        public static int iTotalConvoyClass2                = 0;
        public static int iConvoyPayTypeID                  = 0;
        public static int iConvoyEmvDetailID                = 0;
        public static bool bAllowConvoy                     = true;
        public static bool bActiveEMVDlg                    = false;
        public static bool bActiveConvoyDlg                 = false;
        public static bool bActiveSalesCouponDlg            = false;
        public static bool bActiveSetLaneTypeModeDlg        = false;
        public static bool bActiveDmtPaymentDlg             = false;
        public static bool bMaintenanceMode                 = false;
        public static bool bEtcCtrlIO                       = false;    // In ETC/Dual mode, if found card, bEtcCtrlIO=true, else pay other bEtcCtrlIO=false
        public static bool bAllowSendRTO                    = true;
        public static List<string> szListReceiptNo          = new List<string>();   // For Queue Receipt of Convoy Payment
        public static List<objLoopInfo> xListLoopCar        = new List<objLoopInfo>();
        public static List<objQueueInfo> xListQueues        = new List<objQueueInfo>();

        // v2.0.0: lock guarding the shared queue/loop lists. These are read & mutated from
        // the UI thread, AVC events, key events and convoy logic concurrently. Use these
        // helpers (or lock on the object) instead of touching the list directly to avoid
        // IndexOutOfRange / "Collection was modified" crashes.
        public static readonly object xListLoopCarLock = new object();

        public static int LoopCarCount()
        {
            lock (xListLoopCarLock)
                return xListLoopCar == null ? 0 : xListLoopCar.Count;
        }

        // Returns the first loop car, or null if the list is empty (safe replacement for xListLoopCar[0]).
        public static objLoopInfo LoopCarFirst()
        {
            lock (xListLoopCarLock)
                return (xListLoopCar != null && xListLoopCar.Count > 0) ? xListLoopCar[0] : null;
        }

        public static void LoopCarClear()
        {
            int iCountBefore = LoopCarCount();
            lock (xListLoopCarLock)
                xListLoopCar?.Clear();
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ LoopCarClear : xListLoopCar {iCountBefore} -> 0");
        }

        public static void LoopCarAdd(objLoopInfo car)
        {
            lock (xListLoopCarLock)
            {
                if (xListLoopCar == null)
                    xListLoopCar = new List<objLoopInfo>();
                xListLoopCar.Add(car);
            }
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ LoopCarAdd : car={(car == null ? "null" : "ok")}, xListLoopCar count={LoopCarCount()}");
        }

        // Snapshot copy for safe iteration without holding the lock during long work.
        public static List<objLoopInfo> LoopCarSnapshot()
        {
            lock (xListLoopCarLock)
                return xListLoopCar == null ? new List<objLoopInfo>() : new List<objLoopInfo>(xListLoopCar);
        }

        public static LaneDBLib LaneDB;
        public static MocRabbitMQ mocRabbitMQ;
        public static EtcRabbitMQ etcRabbitMQ;
        public static MtsRabbitMQ mtsRabbitMQ;
        public static bool bDC_NetworkStatus                = false;
        public static bool bMTS_NetworkStatus               = false;
        public static bool bRTO_Connected                   = false;    // Check RTO Networking
        public static bool bDC_Connected                    = false;    // Check DC Networking
        public static bool bMTS_Connected                   = false;    // Check MTS Networking
        public static bool bSupAdjust_Connected             = false;    // Check SupAdjust Networking
        public static int iEdcLastStatus                    = -1;       // v2.0.2: last EDC check (-1 unknown, 0 fail, 1 ok)
        public static bool bLSD_Connected                   = false;    // Check LSD Networking
        public static bool bLCSD_Connected                  = false;    // Check LCSD Networking - TCT
        public static bool bLCIO_Connected                  = false;    // Check LCIO Networking
        public static bool bMTS_SCR                         = true;     // Check SCR Connection from SYS of LCSD

        public static bool bEMV_Connected                   = false;    // Check EMV Connecting
        public static bool bMainAntenna_Connected           = false;    // Check Main Antenna Reader
        public static bool bSecAntenna_Connected            = false;    // Check Secondary Antenna Reader
        public static int iLsdMode_Status                   = -1;       // -1=No Enable LSD, 0=Fail, 1=Open MTC, 2=Open ETC, 3=Open Dual, 4=Lane Closed 

        public static bool bStatusOpenALB_RTO               = false;    // Status of ALB Open when RTO send command, when vehicle pass, then auto close ALB
        public static bool bEndDualHPMC                     = false;

        public static bool bRePaymentFlag                   = false;
        public static bool bReClassifiedFlag                = false;
        public static bool bReversingFlag                   = false;
        public static bool bClassDisFlag                    = false;
        public static bool bSimuPassFlag                    = false;

        public static int iLastAvcStatus                    = -1;
        public static bool bStatusCamera1                   = true;
        public static bool bStatusCamera2                   = true;
        public static bool bStatusCamera3                   = true;

        public static int iCounterLoopNoOB                  = 0;
        public static bool bSendLoopNoOB_MTS                = false;

        public static LsdMode eCurLsdMode                   = LsdMode.LSD_CLOSE;
        public static bool bCurLsdStatus                    = true;     // Current LSD status (Run fail or success)

        public static long iMaxImageCaptureCameraID         = 0;

        public static string szQrRef1                       = "";
        public static string szQrRef2                       = "";

        public static string szEtcLastUpdateObu             = "";
        public static DateTime dtEtcLastUpdateObu           = DateTime.Now;

        public static bool bEdcProcessing                   = false;

        public static int iCurEmvDetailID                   = 0;
        //----------------------------------------
        public static LaneWebSocket webSocketLane;
        public static LaneEventController laneController;
        public static LaneSocketControl_LSD lsdController;


        public static LoginWnd frmLoginWnd;

        public static Dictionary<int, int> keyMapping       = new Dictionary<int, int>();

        //-----------------------------------------------------
        //--- Receipt Footer Setting
        //---- Footer of BuzzeBees
        public static string szFooter_Line1                 = "";
        public static string szFooter_Line2                 = "";
        public static string szFooter_Line3                 = "";
        public static string szFooter_Line4                 = "";
        public static int iFooter_PrintType                 = 0;
        public static string szFooter_QrData                = "";
        public static DateTime dtFooter_StartDate           = DateTime.MinValue;
        public static DateTime dtFooter_EndDate             = DateTime.MaxValue;

        //-------------------------------------------------------------------------------
        public static string DateTimeNow_ISO8601()
        {
            return DateTime_ISO8601(DateTime.Now);
            //return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ffff", CultureInfo.InvariantCulture).Replace(' ', 'T');
        }

        public static string DateTime_ISO8601(DateTime dtDateTime)
        {
            dtDateTime = dtDateTime.AddHours(-7);
            return dtDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        }

        public static bool GetBit(byte b, int bitNumber)
        {
            return (b & (1 << bitNumber)) != 0;
        }
        //------------------------------------------------------------------------------
        public static bool LoadTCT_Configuration()
        {
            try
            {
                //string curPath                  = new FileInfo(Assembly.GetEntryAssembly().Location).Directory.ToString();
                string configFile                   = TCT_Lib.baseConfigFolder + @"/TCT.config";
                var xListCfgWarn                    = new List<string>();      // v2.0.4: config fallback/clamp warnings, written after log path is ready
                szLogPath                           = LogInfo.szLogPath;
                Console.WriteLine("+ Config File : " + configFile);
                Console.WriteLine("+ Log Folder : " + szLogPath);
                Console.WriteLine("+ SysUserName : " + szSysUserName);

                string configJson                   = File.ReadAllText(configFile);
                TCT_Lib.xTctConfig                  = JsonConvert.DeserializeObject(configJson);

                iLaneID                             = xTctConfig["Lane"]["LaneId"] ?? 0;
                szLaneCode                          = xTctConfig["Lane"]["LaneName"] ?? "";
                szLaneName                          = xTctConfig["Lane"]["LaneName"] ?? "";
                iLaneConfigType                     = xTctConfig["Lane"]["LaneConfigType"] ?? 0;
                int iLaneCurrentType                = xTctConfig["Lane"]["LaneType"] ?? 0;
                SetCurrentLaneType(iLaneCurrentType);
                int iLaneDirection                  = xTctConfig["Lane"]["LaneDirection"] ?? 0;
                SetCurrentLaneDirection(iLaneDirection);

                szPlazaID                           = xTctConfig["Plaza"]["PlazaId"] ?? "";
                szPlazaCode                         = xTctConfig["Plaza"]["PlazaName"] ?? "";
                szPlazaName                         = xTctConfig["Plaza"]["PlazaName"] ?? "";
                szTSBId                             = xTctConfig["Plaza"]["TSBId"] ?? "";
                iProviderId                         = xTctConfig["Plaza"]["ProviderId"] ?? 10;

                szLCSD_SocketIPAddress              = xTctConfig["LCSD"]["SocketIPAddress"] ?? "127.0.0.1";
                iLCSD_SocketPort                    = xTctConfig["LCSD"]["SocketPort"] ?? 8084;
                iLCSD_DI_0                          = xTctConfig["LCSD"]["DI_0"] ?? 0;
                iLCSD_DI_1                          = xTctConfig["LCSD"]["DI_1"] ?? 1;
                iLCSD_DI_2                          = xTctConfig["LCSD"]["DI_2"] ?? 2;
                iLCSD_DI_3                          = xTctConfig["LCSD"]["DI_3"] ?? 3;
                iLCSD_DI_4                          = xTctConfig["LCSD"]["DI_4"] ?? 4;
                iLCSD_DI_5                          = xTctConfig["LCSD"]["DI_5"] ?? 5;
                iLCSD_DI_6                          = xTctConfig["LCSD"]["DI_6"] ?? 6;
                iLCSD_DI_7                          = xTctConfig["LCSD"]["DI_7"] ?? 7;
                iLCSD_DI_8                          = xTctConfig["LCSD"]["DI_8"] ?? 8;
                iLCSD_DI_9                          = xTctConfig["LCSD"]["DI_9"] ?? 9;
                iLCSD_DI_10                         = xTctConfig["LCSD"]["DI_10"] ?? 10;
                iLCSD_DI_11                         = xTctConfig["LCSD"]["DI_11"] ?? 11;
                iLCSD_DI_12                         = xTctConfig["LCSD"]["DI_12"] ?? 12;
                iLCSD_DI_13                         = xTctConfig["LCSD"]["DI_13"] ?? 13;
                iLCSD_DI_14                         = xTctConfig["LCSD"]["DI_14"] ?? 14;
                iLCSD_DI_15                         = xTctConfig["LCSD"]["DI_15"] ?? 15;

                szLCIO_SocketIPAddress              = xTctConfig["LCIO"]["SocketIPAddress"] ?? "127.0.0.1";
                iLCIO_SocketPort                    = xTctConfig["LCIO"]["SocketPort"] ?? 18084;

                iRTO_SocketPort                     = xTctConfig["RTO"]["SocketPort"] ?? 9094;
                szRTO_IPAddress                     = xTctConfig["RTO"]["IPAddress"] ?? "";

                bEnableSupAdjust                    = (xTctConfig["SupAdjust"]["Enable"] ?? 0) == 1;
                iSupAdjust_SocketPort               = xTctConfig["SupAdjust"]["SocketPort"] ?? 9095;
                szSupAdjust_IPAddress               = xTctConfig["SupAdjust"]["IPAddress"] ?? "";

                bEnableLSD                          = (xTctConfig["LSD"]["Enable"] ?? 0) == 1;
                szLSD_IPAddress                     = xTctConfig["LSD"]["IPAddress"] ?? "";
                iLSD_SocketPort                     = xTctConfig["LSD"]["SocketPort"] ?? 16603;
                iLSD_KeepAliveSec                   = xTctConfig["LSD"]["KeepAliveSec"] ?? 0;
                iLSD_MethodType                     = xTctConfig["LSD"]["MethodType"] ?? 0;
                bLSD_SendWhenPressOTL               = (xTctConfig["LSD"]["SendWhenPressOTL"] ?? 1) == 1;
                iLSD_RetryWaitSec                   = xTctConfig["LSD"]["RetryWaitSec"] ?? 2;
                iLSD_RetryTime                      = xTctConfig["LSD"]["RetryTime"] ?? 2;

                bEnableAntenna                      = (xTctConfig["DSRC_Antenna"]["Enable"] ?? 0) == 1;
                szMainAntenna_IPAddress             = xTctConfig["DSRC_Antenna"]["MainAntenna_IPAddress"] ?? "";
                iMainAntenna_SocketPort             = xTctConfig["DSRC_Antenna"]["MainAntenna_SocketPort"] ?? 80;
                szSecondaryAntenna_IPAddress        = xTctConfig["DSRC_Antenna"]["SecondaryAntenna_IPAddress"] ?? "";
                iSecondaryAntenna_SocketPort        = xTctConfig["DSRC_Antenna"]["SecondaryAntenna_SocketPort"] ?? 80;

                bEnableVideoNVR                     = (xTctConfig["VideoNVR"]["Enable"] ?? 0) == 1;
                szVideoNVR_FrontCaptureUrl          = xTctConfig["VideoNVR"]["FrontCaptureUrl"] ?? "";
                szVideoNVR_RearCaptureUrl           = xTctConfig["VideoNVR"]["RearCaptureUrl"] ?? "";
                szVideoNVR_ThirdCaptureUrl          = xTctConfig["VideoNVR"]["ThirdCaptureUrl"] ?? "";
                szVideoNVR_OutputPathPrefix         = xTctConfig["VideoNVR"]["OutputPathPrefix"] ?? "";
                szVideoNVR_OutputPathSuffix         = xTctConfig["VideoNVR"]["OutputPathSuffix"] ?? "";
                iVideoNVR_Img3TimeLapseMilliSec     = xTctConfig["VideoNVR"]["Image3TimeLapseMilliSec"] ?? 2000;
                iVideoNVR_CaptureImageMethod        = xTctConfig["VideoNVR"]["CaptureImageMethod"] ?? 0;
                szVideoNVR_WebApiBaseUrl            = xTctConfig["VideoNVR"]["WebApiBaseUrl"] ?? "";
                iVideoNVR_TowingTimeLapseMillisec   = xTctConfig["VideoNVR"]["TowingTimeLapseMilliSec"] ?? 3000;
                iVideoNVR_TimeoutSec                = xTctConfig["VideoNVR"]["TimeoutSec"] ?? 10;
                iVideoNVR_CachePolicy               = xTctConfig["VideoNVR"]["CachePolicy"] ?? 6;
                iVideoNVR_AutoKeepDayImageFile      = xTctConfig["VideoNVR"]["AutoKeepDayImageFile"] ?? 0;
                iVideoNVR_ResendFileDay             = xTctConfig["VideoNVR"]["ResendFileDay"] ?? 1;
                iVideoNVR_ResizeImage               = xTctConfig["VideoNVR"]["ResizeImage"] ?? 1;
                iVideoNVR_ResizeWidth               = xTctConfig["VideoNVR"]["ResizeWidth"] ?? 1280;
                iVideoNVR_ResizeHeight              = xTctConfig["VideoNVR"]["ResizeHeight"] ?? 720;
                iVideoNVR_JpegQuality               = xTctConfig["VideoNVR"]["JpegQuality"] ?? 85;
                if (iVideoNVR_ResizeWidth < 320 || iVideoNVR_ResizeHeight < 180)
                {
                    xListCfgWarn.Add($"VideoNVR ResizeWidth/Height={iVideoNVR_ResizeWidth}x{iVideoNVR_ResizeHeight} too small (min 320x180) -> 1280x720");
                    iVideoNVR_ResizeWidth  = 1280;
                    iVideoNVR_ResizeHeight = 720;
                }
                if (iVideoNVR_JpegQuality < 30 || iVideoNVR_JpegQuality > 100)
                {
                    xListCfgWarn.Add($"VideoNVR JpegQuality={iVideoNVR_JpegQuality} out of 30..100 -> 85");
                    iVideoNVR_JpegQuality = 85;
                }
                if (iVideoNVR_Img3TimeLapseMilliSec < 100)
                {
                    xListCfgWarn.Add($"VideoNVR Image3TimeLapseMilliSec={iVideoNVR_Img3TimeLapseMilliSec} < 100 -> 2000");
                    iVideoNVR_Img3TimeLapseMilliSec = 2000;
                }
                if (iVideoNVR_TowingTimeLapseMillisec < 1000)
                {
                    xListCfgWarn.Add($"VideoNVR TowingTimeLapseMilliSec={iVideoNVR_TowingTimeLapseMillisec} < 1000 -> 3000");
                    iVideoNVR_TowingTimeLapseMillisec = 3000;
                }
                if (iVideoNVR_TimeoutSec < 5)
                {
                    xListCfgWarn.Add($"VideoNVR TimeoutSec={iVideoNVR_TimeoutSec} < 5 -> 5");
                    iVideoNVR_TimeoutSec = 5;
                }
                if (iVideoNVR_CachePolicy > 8)
                {
                    xListCfgWarn.Add($"VideoNVR CachePolicy={iVideoNVR_CachePolicy} > 8 -> 6 (NoCacheNoStore)");
                    iVideoNVR_CachePolicy = 6;
                }

/*
        public static string szVideoLane_CaptureImagePath = "";
        public static string szVideoLane_FrontCameraUrl = "";
        public static string szVideoLane_RearCameraUrl = "";
        public static string szVideoLane_ThirdCameraUrl = "";
        public static string szVideoLane_FrontCamera_UserName = "";
        public static string szVideoLane_FrontCamera_Password = "";
        public static string szVideoLane_RearCamera_UserName = "";
        public static string szVideoLane_RearCamera_Password = "";
        public static string szVideoLane_ThirdCamera_UserName = "";
        public static string szVideoLane_ThirdCamera_Password = "";
*/               


                if (xTctConfig["VideoLaneCapture"] != null)
                {
                    szVideoLane_CaptureImagePath        = xTctConfig["VideoLaneCapture"]["CaptureImagePath"] ?? "";
                    szVideoLane_FrontCameraUrl          = xTctConfig["VideoLaneCapture"]["FrontCameraUrl"] ?? "";
                    szVideoLane_RearCameraUrl           = xTctConfig["VideoLaneCapture"]["RearCameraUrl"] ?? "";
                    szVideoLane_ThirdCameraUrl          = xTctConfig["VideoLaneCapture"]["ThirdCameraUrl"] ?? "";
                    szVideoLane_FrontCamera_UserName    = xTctConfig["VideoLaneCapture"]["FrontCamera_UserName"] ?? "";
                    szVideoLane_FrontCamera_Password    = xTctConfig["VideoLaneCapture"]["FrontCamera_Password"] ?? "";
                    szVideoLane_RearCamera_UserName     = xTctConfig["VideoLaneCapture"]["RearCamera_UserName"] ?? "";
                    szVideoLane_RearCamera_Password     = xTctConfig["VideoLaneCapture"]["RearCamera_Password"] ?? "";
                    szVideoLane_ThirdCamera_UserName    = xTctConfig["VideoLaneCapture"]["ThirdCamera_UserName"] ?? "";
                    szVideoLane_ThirdCamera_Password    = xTctConfig["VideoLaneCapture"]["ThirdCamera_Password"] ?? "";
                    szVideoLane_IPCamera1               = xTctConfig["VideoLaneCapture"]["IPCamera1"] ?? "";
                    szVideoLane_IPCamera2               = xTctConfig["VideoLaneCapture"]["IPCamera2"] ?? "";
                }
                if (xTctConfig["VideoLaneCapture"] == null)
                    xListCfgWarn.Add("section VideoLaneCapture not found -> defaults used");

                bEnableEtcRabbitMQ                          = (xTctConfig["ETC_RabbitMQ"]["Enable"] ?? 0) == 1;
                szETC_RabbitMQ_ServerHostName               = xTctConfig["ETC_RabbitMQ"]["ServerHostName"] ?? "";
                szETC_RabbitMQ_VirtualHost                  = xTctConfig["ETC_RabbitMQ"]["VirtualHost"] ?? "";
                iETC_RabbitMQ_Port                          = xTctConfig["ETC_RabbitMQ"]["Port"] ?? 5672;
                szETC_RabbitMQ_User                         = xTctConfig["ETC_RabbitMQ"]["User"] ?? "";
                szETC_RabbitMQ_Password                     = xTctConfig["ETC_RabbitMQ"]["Password"] ?? "";
                szETC_RabbitMQ_LaneStateQueueName           = xTctConfig["ETC_RabbitMQ"]["LaneStateQueueName"] ?? "";
                szETC_RabbitMQ_LaneStateExchangeName        = xTctConfig["ETC_RabbitMQ"]["LaneStateExchangeName"] ?? "";
                szETC_RabbitMQ_LaneStateRoutingKey          = xTctConfig["ETC_RabbitMQ"]["LaneStateRoutingKey"] ?? "";
                szETC_RabbitMQ_CompilePassageQueueName      = xTctConfig["ETC_RabbitMQ"]["CompilePassageQueueName"] ?? "";
                szETC_RabbitMQ_CompilePassageExchangeName   = xTctConfig["ETC_RabbitMQ"]["CompilePassageExchangeName"] ?? "";
                szETC_RabbitMQ_CompilePassageRoutingKey     = xTctConfig["ETC_RabbitMQ"]["CompilePassageRoutingKey"] ?? "";
                szETC_RabbitMQ_ManualActionQueueName        = xTctConfig["ETC_RabbitMQ"]["ManualActionQueueName"] ?? "";
                szETC_RabbitMQ_ManualActionExchangeName     = xTctConfig["ETC_RabbitMQ"]["ManualActionExchangeName"] ?? "";
                szETC_RabbitMQ_ManualActionRoutingKey       = xTctConfig["ETC_RabbitMQ"]["ManualActionRoutingKey"] ?? "";
                szETC_RabbitMQ_ServerIPAddress              = xTctConfig["ETC_RabbitMQ"]["ServerIPAddress"] ?? "";
                iETC_RabbitMQ_ServerPort                    = xTctConfig["ETC_RabbitMQ"]["ServerPort"] ?? 80;
                bETC_RabbitMQ_WriteLogETC                   = (xTctConfig["ETC_RabbitMQ"]["WriteLogETC"] ?? 0) == 1;

                bEnableDcRabbitMQ                           = (xTctConfig["DC_RabbitMQ"]["Enable"] ?? 0) == 1;
                szDC_RabbitMQ_ServerHostName                = xTctConfig["DC_RabbitMQ"]["ServerHostName"] ?? "";
                szDC_RabbitMQ_VirtualHost                   = xTctConfig["DC_RabbitMQ"]["VirtualHost"] ?? "";
                iDC_RabbitMQ_Port                           = xTctConfig["DC_RabbitMQ"]["Port"] ?? 5672;
                szDC_RabbitMQ_User                          = xTctConfig["DC_RabbitMQ"]["User"] ?? "";
                szDC_RabbitMQ_Password                      = xTctConfig["DC_RabbitMQ"]["Password"] ?? "";
                szDC_RabbitMQ_TransQueueName                = xTctConfig["DC_RabbitMQ"]["TransQueueName"] ?? "";
                szDC_RabbitMQ_TransExchangeName             = xTctConfig["DC_RabbitMQ"]["TransExchangeName"] ?? "";
                szDC_RabbitMQ_TransRoutingKey               = xTctConfig["DC_RabbitMQ"]["TransRoutingKey"] ?? "";
                szDC_RabbitMQ_EventQueueName                = xTctConfig["DC_RabbitMQ"]["EventQueueName"] ?? "";
                szDC_RabbitMQ_EventExchangeName             = xTctConfig["DC_RabbitMQ"]["EventExchangeName"] ?? "";
                szDC_RabbitMQ_EventRoutingKey               = xTctConfig["DC_RabbitMQ"]["EventRoutingKey"] ?? "";
                szDC_RabbitMQ_ParamsQueueName               = xTctConfig["DC_RabbitMQ"]["ParamsQueueName"] ?? "";
                szDC_RabbitMQ_ParamsExchangeName            = xTctConfig["DC_RabbitMQ"]["ParamsExchangeName"] ?? "";
                szDC_RabbitMQ_ParamsRoutingKey              = xTctConfig["DC_RabbitMQ"]["ParamsRoutingKey"] ?? "";
                szDC_RabbitMQ_ServerIPAddress               = xTctConfig["DC_RabbitMQ"]["ServerIPAddress"] ?? "";
                iDC_RabbitMQ_ServerPort                     = xTctConfig["DC_RabbitMQ"]["ServerPort"] ?? 80;

                if (xTctConfig["MTS_RabbitMQ"] != null)
                {
                    bEnableMTS                                  = (xTctConfig["MTS_RabbitMQ"]["Enable"] ?? 0) == 1;
                    iMTS_TimerSecCheckStatus                    = xTctConfig["MTS_RabbitMQ"]["TimerSecCheckStatus"] ?? 60;
                    szMTS_RabbitMQ_ServerHostName               = xTctConfig["MTS_RabbitMQ"]["ServerHostName"] ?? "";
                    szMTS_RabbitMQ_VirtualHost                  = xTctConfig["MTS_RabbitMQ"]["VirtualHost"] ?? "";
                    iMTS_RabbitMQ_Port                          = xTctConfig["MTS_RabbitMQ"]["Port"] ?? 5672;
                    szMTS_RabbitMQ_User                         = xTctConfig["MTS_RabbitMQ"]["User"] ?? "";
                    szMTS_RabbitMQ_Password                     = xTctConfig["MTS_RabbitMQ"]["Password"] ?? "";
                    szMTS_RabbitMQ_MtsQueueName                 = xTctConfig["MTS_RabbitMQ"]["MtsQueueName"] ?? "";
                    szMTS_RabbitMQ_MtsExchangeName              = xTctConfig["MTS_RabbitMQ"]["MtsExchangeName"] ?? "";
                    szMTS_RabbitMQ_MtsRoutingKey                = xTctConfig["MTS_RabbitMQ"]["MtsRoutingKey"] ?? "";
                    szMTS_RabbitMQ_ServerIPAddress              = xTctConfig["MTS_RabbitMQ"]["ServerIPAddress"] ?? "";
                    iMTS_RabbitMQ_ServerPort                    = xTctConfig["MTS_RabbitMQ"]["ServerPort"] ?? 80;
                    szMTS_EtcStatusDbUrl                        = xTctConfig["MTS_RabbitMQ"]["EtcStatusDbUrl"] ?? "";
                    szMTS_EtcStatusDbUrl2                       = xTctConfig["MTS_RabbitMQ"]["EtcStatusDbUrl2"] ?? "";
                    szMTS_EtcStatusDbUrl3                       = xTctConfig["MTS_RabbitMQ"]["EtcStatusDbUrl3"] ?? "";
                    szMTS_EtcStatusDbUrl4                       = xTctConfig["MTS_RabbitMQ"]["EtcStatusDbUrl4"] ?? "";
                    szMTS_AlarmDeviceList                       = xTctConfig["MTS_RabbitMQ"]["AlarmDeviceList"] ?? "";

                    szMTS_AlarmDeviceList       = szMTS_AlarmDeviceList.Replace("\r", "").Replace("\n", "").Replace("\t", "");
                }
                if (xTctConfig["MTS_RabbitMQ"] == null)
                    xListCfgWarn.Add("section MTS_RabbitMQ not found -> defaults used (MTS disabled)");

                if (xTctConfig["MTS_Monitor"] != null)
                {
                    szMTS_Monitor_DiskPath_01                   = xTctConfig["MTS_Monitor"]["DiskPath1"] ?? "";
                    szMTS_Monitor_DiskPath_02                   = xTctConfig["MTS_Monitor"]["DiskPath2"] ?? "";
                    szMTS_Monitor_DiskPath_03                   = xTctConfig["MTS_Monitor"]["DiskPath3"] ?? "";
                    szMTS_Monitor_DiskPath_04                   = xTctConfig["MTS_Monitor"]["DiskPath4"] ?? "";
                    szMTS_Monitor_DiskPath_05                   = xTctConfig["MTS_Monitor"]["DiskPath5"] ?? "";
                    szMTS_Monitor_DiskPath_06                   = xTctConfig["MTS_Monitor"]["DiskPath6"] ?? "";
                    szMTS_Monitor_DiskPath_07                   = xTctConfig["MTS_Monitor"]["DiskPath7"] ?? "";
                    szMTS_Monitor_DiskPath_08                   = xTctConfig["MTS_Monitor"]["DiskPath8"] ?? "";
                    szMTS_Monitor_DiskPath_09                   = xTctConfig["MTS_Monitor"]["DiskPath9"] ?? "";
                    szMTS_Monitor_DiskPath_10                   = xTctConfig["MTS_Monitor"]["DiskPath10"] ?? "";

                    szMTS_Monitor_TFI_IPAddress                 = xTctConfig["MTS_Monitor"]["TFI_IPAddress"] ?? "";
                    iMTS_Monitor_TFI_Port                       = xTctConfig["MTS_Monitor"]["TFI_Port"] ?? 80;
                    szMTS_Monitor_CRS_IPAddress                 = xTctConfig["MTS_Monitor"]["CRS_IPAddress"] ?? "";
                    iMTS_Monitor_CRS_Port                       = xTctConfig["MTS_Monitor"]["CRS_Port"] ?? 80;

                    szMTS_Monitor_Product_Printer               = xTctConfig["MTS_Monitor"]["Product_Printer"] ?? "TM-T88VI";
                    szMTS_Monitor_Product_SCR                   = xTctConfig["MTS_Monitor"]["Product_SCR"] ?? "SL600-NFC Reader";
                    szMTS_Monitor_Product_Keyboard              = xTctConfig["MTS_Monitor"]["Product_Keyboard"] ?? "SPOS";

                    iMTS_Monitor_TimeAlertLoopNoOB              = xTctConfig["MTS_Monitor"]["TimeAlertLoopNoOB"] ?? 3;
                    iMTS_Monitor_IntervalSecAlertLoopNoOB       = xTctConfig["MTS_Monitor"]["IntervalSecAlertLoopNoOB"] ?? 10;
                    iMTS_Monitor_IntervalSecGetEtcStatus        = xTctConfig["MTS_Monitor"]["IntervalSecGetEtcStatus"] ?? 60;
                }
                if (xTctConfig["MTS_Monitor"] == null)
                    xListCfgWarn.Add("section MTS_Monitor not found -> defaults used");

                bEnableLaneWebService                       = (xTctConfig["LaneWebService"]["Enable"] ?? 0) == 1;
                szLaneWebService_BaseURL                    = xTctConfig["LaneWebService"]["WebServiceBaseUrl"] ?? "";
                szLaneWebService_ServerIPAddress            = xTctConfig["LaneWebService"]["ServerIPAddress"] ?? "";
                iLaneWebService_ServerPort                  = xTctConfig["LaneWebService"]["ServerPort"] ?? 3000;
                iLaneWebService_ChkStartJobTOD              = xTctConfig["LaneWebService"]["CheckStartJobTOD"] ?? 0;

                szPrinter_PrinterPort                       = xTctConfig["Printer"]["PrinterPort"] ?? "/dev/usb/lp0";
                iPrinter_PrintFontSize                      = xTctConfig["Printer"]["PrintFontSize"] ?? 1;
                iPrinter_PrintFooter                        = xTctConfig["Printer"]["PrintFooter"] ?? 0;
                iPrinter_PrintQrSize                        = xTctConfig["Printer"]["PrintQrSize"] ?? 5;

                bEnableEdc                                  = (xTctConfig["EMV"]["Enable"] ?? 0) == 1;
                szEdcIPAddress                              = xTctConfig["EMV"]["EdcIPAddress"] ?? "";
                iEdcSendPort                                = xTctConfig["EMV"]["EdcSendPort"] ?? 2019;
                iEdcRecvPort                                = xTctConfig["EMV"]["EdcRecvPort"] ?? 2018;
                szEdcTID                                    = xTctConfig["EMV"]["EdcTID"] ?? "";
                iEdcQrWaitTimeSec                           = xTctConfig["EMV"]["EdcQrWaitTimeSec"] ?? 10;
                iEdcStartHourClearBatch                     = xTctConfig["EMV"]["EdcStartHourClearBatch"] ?? 0;
                iEdcEndHourClearBatch                       = xTctConfig["EMV"]["EdcEndHourClearBatch"] ?? 0;
                iEdcMinuteIntervalClearBatch                = xTctConfig["EMV"]["EdcMinuteIntervalClearBatch"] ?? 0;
                iEdcWaitTimeOutSec                          = xTctConfig["EMV"]["EdcWaitTimeOutSec"] ?? 5;
                iEdcRetryTimeOut                            = xTctConfig["EMV"]["EdcRetryTimeOut"] ?? 2;
                iEdcVersionType                             = xTctConfig["EMV"]["EdcVersionType"] ?? 0;
                iEdcChkNetworkType                          = xTctConfig["EMV"]["EdcCheckNetworkType"] ?? 0;
                iEdcChkKeepAliveIntervalMin                 = xTctConfig["EMV"]["EdcCheckKeepAliveIntervalMin"] ?? 5;
                iEdcRelayMilliSecBeforeAck                  = xTctConfig["EMV"]["EdcRelayMilliSecBeforeAck"] ?? 15;

                iStopSirenAfterTimeSec                      = xTctConfig["Config"]["StopSirenAfterAlarm"] ?? 5;
                iTimerSecAutoAVC                            = xTctConfig["Config"]["TimerSecAutoAVC"] ?? 3;
                szDayTimeMode                               = xTctConfig["Config"]["DayTimeMode"] ?? "06:00";
                szNightTimeMode                             = xTctConfig["Config"]["NightTimeMode"] ?? "18:00";
                iTimerAutoResendTrans                       = xTctConfig["Config"]["TimerAutoResendTrans"] ?? 300;
                iSaveLogEtcRabbitMQ                         = xTctConfig["Config"]["SaveLogEtcRabbitMQ"] ?? 0;
                iTimerSecCheckDeviceStatus                  = xTctConfig["Config"]["TimerSecCheckDeviceStatus"] ?? 120;
                szLogPath                                   = xTctConfig["Config"]["LogPath"] ?? "";
                LogInfo.eLogLevel                           = LogInfo.ParseLevel(Convert.ToString(xTctConfig["Config"]["LogLevel"] ?? "INFO"));     // v2.0.4
                string szCfgLogLevel                        = Convert.ToString(xTctConfig["Config"]["LogLevel"] ?? "INFO");
                if (LogInfo.ParseLevel(szCfgLogLevel, (LogLevel)0) == (LogLevel)0)
                    xListCfgWarn.Add($"Config LogLevel='{szCfgLogLevel}' invalid -> INFO");
                iKeepLogMonth                               = xTctConfig["Config"]["KeepLogMonth"] ?? 3;
                iKeepDbDay                                  = xTctConfig["Config"]["KeepDatabaseDay"] ?? 30;
                iDevMode                                    = xTctConfig["Config"]["DevMode"] ?? 0;
                iKeyStrokeTimeMilliSec                      = xTctConfig["Config"]["KeyStrokeTimeMilliSec"] ?? 300;
                iAvcStatusType                              = xTctConfig["Config"]["AvcStatusType"] ?? 0;
                szManualImportPath                          = xTctConfig["Config"]["ManualImportPath"] ?? "";
                iDmtCardType                                = xTctConfig["Config"]["DmtCardType"] ?? 0;
                szDmtCardEffectiveDate                      = xTctConfig["Config"]["DmtCardEffectiveDate"] ?? "";
                iImportStaffIntervalMin                     = xTctConfig["Config"]["ImportStaffIntervalMin"] ?? 5;
                szAllowTollRoleGroupID                      = xTctConfig["Config"]["AllowTollRoleGroupID"] ?? "10,20";
                szAllowMaintenanceRoleGroupID               = xTctConfig["Config"]["AllowMaintenanceRoleGroupID"] ?? "50,51";

                szAllowTollRoleGroupID          = szAllowTollRoleGroupID.Replace("\r", "").Replace("\n", "").Replace("\t", "");
                szAllowMaintenanceRoleGroupID   = szAllowMaintenanceRoleGroupID.Replace("\r", "").Replace("\n", "").Replace("\t", "");

                szPaymentCode_06                            = xTctConfig["Payment"]["PaymentCode_06"] ?? "PM6";
                szPaymentName_06                            = xTctConfig["Payment"]["PaymentName_06"] ?? "";
                szPaymentBtn_06                             = xTctConfig["Payment"]["PaymentBtn_06"] ?? "";
                szPaymentCRS_06                             = xTctConfig["Payment"]["PaymentCRS_06"] ?? "6";
                szPaymentCode_07                            = xTctConfig["Payment"]["PaymentCode_07"] ?? "PM7";
                szPaymentName_07                            = xTctConfig["Payment"]["PaymentName_07"] ?? "";
                szPaymentBtn_07                             = xTctConfig["Payment"]["PaymentBtn_07"] ?? "";
                szPaymentCRS_07                             = xTctConfig["Payment"]["PaymentCRS_07"] ?? "7";
                szPaymentCode_08                            = xTctConfig["Payment"]["PaymentCode_08"] ?? "PM8";
                szPaymentName_08                            = xTctConfig["Payment"]["PaymentName_08"] ?? "";
                szPaymentBtn_08                             = xTctConfig["Payment"]["PaymentBtn_08"] ?? "";
                szPaymentCRS_08                             = xTctConfig["Payment"]["PaymentCRS_08"] ?? "8";
                szPaymentCode_09                            = xTctConfig["Payment"]["PaymentCode_09"] ?? "PM9";
                szPaymentName_09                            = xTctConfig["Payment"]["PaymentName_09"] ?? "";
                szPaymentBtn_09                             = xTctConfig["Payment"]["PaymentBtn_09"] ?? "";
                szPaymentCRS_09                             = xTctConfig["Payment"]["PaymentCRS_09"] ?? "9";

                bEnableTfiScpFiles                          = (xTctConfig["TFI"]["Enable"] ?? 0) == 1;
                szTfiScpHostName                            = xTctConfig["TFI"]["ScpHostName"] ?? "";
                //szTfiScpSourcePath                          = xTctConfig["TFI"]["ScpSourcePath"] ?? "";
                szTfiScpDestPath                            = xTctConfig["TFI"]["ScpDestPath"] ?? "";
                szTfiScpUserName                            = xTctConfig["TFI"]["ScpUserName"] ?? "";
                szTfiScpPassword                            = xTctConfig["TFI"]["ScpPassword"] ?? "";


                LogInfo.CreateLogPath();
                //---------------------------------------------------------------
                LogInfo.WriteLog("----------------------------------------------------------------");
                LogInfo.WriteLog("");
                LogInfo.WriteLog("");
                LogInfo.WriteLog("====================================================");
                LogInfo.WriteLog("======== TCT APP : Version = " + TCT_Lib.szAppVersion + " =================");
                LogInfo.WriteLog("======== TCT APP : Build = " + TCT_Lib.szAppBuild + " =====");
                LogInfo.WriteLog("====================================================");

                LogInfo.WriteLaneEvent("----------------------------------------------------------------");
                LogInfo.WriteLaneEvent("");
                LogInfo.WriteLaneEvent("");
                LogInfo.WriteLaneEvent("====================================================");
                LogInfo.WriteLaneEvent("======== TCT APP : Version = " + TCT_Lib.szAppVersion + " =================");
                LogInfo.WriteLaneEvent("======== TCT APP : Build = " + TCT_Lib.szAppBuild + " =====");
                LogInfo.WriteLaneEvent("====================================================");

                //---------------------------------------------------------------------------------------
                LogInfo.WriteLog($"+ LaneId \t\t\t\t\t= {iLaneID}");
                LogInfo.WriteLog($"+ LaneConfigType \t\t\t\t= {iLaneConfigType}");
                LogInfo.WriteLog($"+ LaneType \t\t\t\t\t= {iLaneCurrentType}");
                LogInfo.WriteLog($"+ PlazaId \t\t\t\t\t= {szPlazaID}");
                LogInfo.WriteLog($"+ PlazaName \t\t\t\t\t= {szPlazaName}");
                LogInfo.WriteLog($"+ Provider \t\t\t\t\t= {iProviderId}");

                LogInfo.WriteLog($"+ LCSD_SocketIPAddress\t\t\t= {szLCSD_SocketIPAddress}");
                LogInfo.WriteLog($"+ LCSD_SocketPort \t\t\t\t= {iLCSD_SocketPort}");
                LogInfo.WriteLog($"+ LCSD_DO_0 \t\t\t\t\t= {iLCSD_DI_0}");
                LogInfo.WriteLog($"+ LCSD_DO_1 \t\t\t\t\t= {iLCSD_DI_1}");
                LogInfo.WriteLog($"+ LCSD_DO_2 \t\t\t\t\t= {iLCSD_DI_2}");
                LogInfo.WriteLog($"+ LCSD_DO_3 \t\t\t\t\t= {iLCSD_DI_3}");
                LogInfo.WriteLog($"+ LCSD_DO_4 \t\t\t\t\t= {iLCSD_DI_4}");
                LogInfo.WriteLog($"+ LCSD_DO_5 \t\t\t\t\t= {iLCSD_DI_5}");
                LogInfo.WriteLog($"+ LCSD_DO_6 \t\t\t\t\t= {iLCSD_DI_6}");
                LogInfo.WriteLog($"+ LCSD_DO_7 \t\t\t\t\t= {iLCSD_DI_7}");
                LogInfo.WriteLog($"+ LCSD_DO_8 \t\t\t\t\t= {iLCSD_DI_8}");
                LogInfo.WriteLog($"+ LCSD_DO_9 \t\t\t\t\t= {iLCSD_DI_9}");
                LogInfo.WriteLog($"+ LCSD_DO_10 \t\t\t\t\t= {iLCSD_DI_10}");
                LogInfo.WriteLog($"+ LCSD_DO_11 \t\t\t\t\t= {iLCSD_DI_11}");
                LogInfo.WriteLog($"+ LCSD_DO_12 \t\t\t\t\t= {iLCSD_DI_12}");
                LogInfo.WriteLog($"+ LCSD_DO_13 \t\t\t\t\t= {iLCSD_DI_13}");
                LogInfo.WriteLog($"+ LCSD_DO_14 \t\t\t\t\t= {iLCSD_DI_14}");
                LogInfo.WriteLog($"+ LCSD_DO_15 \t\t\t\t\t= {iLCSD_DI_15}");

                LogInfo.WriteLog($"+ LCIO_SocketIPAddress\t\t= {szLCIO_SocketIPAddress}");
                LogInfo.WriteLog($"+ LCIO_SocketPort \t\t\t= {iLCIO_SocketPort}");
                LogInfo.WriteLog($"+ RTO_IPAddress \t\t\t\t= {szRTO_IPAddress}");
                LogInfo.WriteLog($"+ RTO_SocketPort \t\t\t\t= {iRTO_SocketPort}");
                LogInfo.WriteLog($"+ SupAdjust_Enable \t\t\t= {bEnableSupAdjust}");
                LogInfo.WriteLog($"+ SupAdjust_IPAddress \t\t= {szSupAdjust_IPAddress}");
                LogInfo.WriteLog($"+ SupAdjust_SocketPort \t\t= {iSupAdjust_SocketPort}");

                LogInfo.WriteLog($"+ LSD_Enable \t\t\t\t\t= {bEnableLSD}");
                LogInfo.WriteLog($"+ LSD_IPAddress \t\t\t\t= {szLSD_IPAddress}");
                LogInfo.WriteLog($"+ LSD_SocketPort \t\t\t\t= {iLSD_SocketPort}");
                LogInfo.WriteLog($"+ LSD_KeepAliveSec \t\t\t= {iLSD_KeepAliveSec}");
                LogInfo.WriteLog($"+ LSD_MethodType \t\t\t= {iLSD_MethodType}");
                LogInfo.WriteLog($"+ LSD_SendWhenPressOTL \t\t= {bLSD_SendWhenPressOTL}");
                LogInfo.WriteLog($"+ LSD_RetryWaitSec \t\t\t= {iLSD_RetryWaitSec}");
                LogInfo.WriteLog($"+ LSD_RetryTime \t\t\t= {iLSD_RetryTime}");

                LogInfo.WriteLog($"+ LaneService_Enable \t\t\t= {bEnableLaneWebService}");
                LogInfo.WriteLog($"+ LaneService_BaseURL \t\t= {szLaneWebService_BaseURL}");
                LogInfo.WriteLog($"+ LaneService_IPAddress \t\t= {szLaneWebService_ServerIPAddress}");
                LogInfo.WriteLog($"+ LaneService_ServerPort \t\t= {iLaneWebService_ServerPort}");
                LogInfo.WriteLog($"+ LaneServicce_CheckStartJobTOD \t= {iLaneWebService_ChkStartJobTOD}");

                LogInfo.WriteLog($"+ EDC_Enable \t\t\t\t= {bEnableEdc}");
                LogInfo.WriteLog($"+ EDC_IPAddress \t\t\t= {szEdcIPAddress}");
                LogInfo.WriteLog($"+ EDC_SendPort \t\t\t= {iEdcSendPort}");
                LogInfo.WriteLog($"+ EDC_RecvPort \t\t\t= {iEdcRecvPort}");
                LogInfo.WriteLog($"+ EDC_TID \t\t\t\t= {szEdcTID}");
                LogInfo.WriteLog($"+ EDC_QrWaitTimeSec\t\t\t= {iEdcQrWaitTimeSec}");
                LogInfo.WriteLog($"+ EDC_StartHourClearBatch\t\t= {iEdcStartHourClearBatch}");
                LogInfo.WriteLog($"+ EDC_EndHourClearBatch\t\t= {iEdcEndHourClearBatch}");
                LogInfo.WriteLog($"+ EDC_MinuteIntervalClearBatch\t= {iEdcMinuteIntervalClearBatch}");
                LogInfo.WriteLog($"+ EDC_WaitTimeOutSec\t\t\t= {iEdcWaitTimeOutSec}");
                LogInfo.WriteLog($"+ EDC_VersionType\t\t\t= {iEdcVersionType}");
                LogInfo.WriteLog($"+ EDC_CheckNetworkkType\t\t= {iEdcChkNetworkType}");
                LogInfo.WriteLog($"+ EDC_CheckKeepAliveIntervalMin\t= {iEdcChkKeepAliveIntervalMin}");
                LogInfo.WriteLog($"+ EDC_RelayMilliSecBeforeAck\t= {iEdcRelayMilliSecBeforeAck}");

                LogInfo.WriteLog($"+ VideoNVR_Enable \t\t\t= {bEnableVideoNVR}");
                LogInfo.WriteLog($"+ VideoNVR_FrontCaptureUrl \t\t= {szVideoNVR_FrontCaptureUrl}");
                LogInfo.WriteLog($"+ VideoNVR_RearCaptureUrl \t\t= {szVideoNVR_RearCaptureUrl}");
                LogInfo.WriteLog($"+ VideoNVR_ThirdCaptureUrl \t\t= {szVideoNVR_ThirdCaptureUrl}");
                LogInfo.WriteLog($"+ VideoNVR_OutputPathPrefix \t\t= {szVideoNVR_OutputPathPrefix}");
                LogInfo.WriteLog($"+ VideoNVR_OutputPathSuffix \t\t= {szVideoNVR_OutputPathSuffix}");
                LogInfo.WriteLog($"+ VideoNVR_Image3TimeLapseMilliSec \t= {iVideoNVR_Img3TimeLapseMilliSec}");
                LogInfo.WriteLog($"+ VideoNVR_CaptureImageMethod \t= {iVideoNVR_CaptureImageMethod}");
                LogInfo.WriteLog($"+ VideoNVR_WebApiBaseUrl \t\t= {szVideoNVR_WebApiBaseUrl}");
                LogInfo.WriteLog($"+ VideoNVR_TowingTimeLapseMillisec \t= {iVideoNVR_TowingTimeLapseMillisec}");
                LogInfo.WriteLog($"+ VideoNVR_TimeoutSec \t\t= {iVideoNVR_TimeoutSec}");
                LogInfo.WriteLog($"+ VideoNVR_CachePolicy \t\t= {iVideoNVR_CachePolicy}");
                LogInfo.WriteLog($"+ VideoNVR_AutoKeepDayImageFile \t= {iVideoNVR_AutoKeepDayImageFile}");
                LogInfo.WriteLog($"+ VideoNVR_ResendFileDay \t\t= {iVideoNVR_ResendFileDay}");
                LogInfo.WriteLog($"+ VideoNVR_ResizeImage \t\t= {iVideoNVR_ResizeImage} ({iVideoNVR_ResizeWidth}x{iVideoNVR_ResizeHeight}, JPEG Q={iVideoNVR_JpegQuality})");
                LogInfo.WriteLog($"+ VideoLane_CaptureImagePath \t\t= {szVideoLane_CaptureImagePath}");
                LogInfo.WriteLog($"+ VideoLane_FrontCameraUrl \t\t= {LogInfo.MaskUrl(szVideoLane_FrontCameraUrl)}");
                LogInfo.WriteLog($"+ VideoLane_RearCameraUrl \t\t= {LogInfo.MaskUrl(szVideoLane_RearCameraUrl)}");
                LogInfo.WriteLog($"+ VideoLane_ThirdCameraUrl \t\t= {LogInfo.MaskUrl(szVideoLane_ThirdCameraUrl)}");
                LogInfo.WriteLog($"+ VideoLane_FrontCamera_UserName \t= {szVideoLane_FrontCamera_UserName}");
                LogInfo.WriteLog($"+ VideoLane_FrontCamera_Password \t= {LogInfo.MaskSecret(szVideoLane_FrontCamera_Password)}");
                LogInfo.WriteLog($"+ VideoLane_RearCamera_UserName \t= {szVideoLane_RearCamera_UserName}");
                LogInfo.WriteLog($"+ VideoLane_RearCamera_Password \t= {LogInfo.MaskSecret(szVideoLane_RearCamera_Password)}");
                LogInfo.WriteLog($"+ VideoLane_ThirdCamera_UserName \t= {szVideoLane_ThirdCamera_UserName}");
                LogInfo.WriteLog($"+ VideoLane_ThirdCamera_Password \t= {LogInfo.MaskSecret(szVideoLane_ThirdCamera_Password)}");
                LogInfo.WriteLog($"+ VideoLane_IPCamera1 \t\t\t= {szVideoLane_IPCamera1}");
                LogInfo.WriteLog($"+ VideoLane_IPCamera2 \t\t\t= {szVideoLane_IPCamera2}");

                LogInfo.WriteLog($"+ Printer_Port \t\t\t\t= {szPrinter_PrinterPort}");
                LogInfo.WriteLog($"+ Printer_FontSize \t\t\t\t= {iPrinter_PrintFontSize}");
                LogInfo.WriteLog($"+ Printer_PrintFooter \t\t\t= {iPrinter_PrintFooter}");
                LogInfo.WriteLog($"+ Printer_PrintQrSize \t\t\t= {iPrinter_PrintQrSize}");

                LogInfo.WriteLog($"+ StopSirenAfterAlarm \t\t\t= {iStopSirenAfterTimeSec}");
                LogInfo.WriteLog($"+ TimerSecAutoAVC \t\t\t\t= {iTimerSecAutoAVC}");
                LogInfo.WriteLog($"+ TimerSecCheckDeviceStatus \t\t\t= {iTimerSecCheckDeviceStatus}");
                LogInfo.WriteLog($"+ DayTimeMode \t\t\t\t= {szDayTimeMode}");
                LogInfo.WriteLog($"+ NightTimeMode \t\t\t\t= {szNightTimeMode}");
                LogInfo.WriteLog($"+ KeepLogMonth \t\t\t\t= {iKeepLogMonth}");
                LogInfo.WriteLog($"+ KeepDatabaseDay \t\t\t\t= {iKeepDbDay}");
                LogInfo.WriteLog($"+ DevMode \t\t\t\t\t= {iDevMode}");
                LogInfo.WriteLog($"+ KeyStrokeTimeMilliSec \t\t\t= {iKeyStrokeTimeMilliSec}");
                LogInfo.WriteLog($"+ LogPath \t\t\t\t\t= {szLogPath}");
                LogInfo.WriteLog(LogLevel.WARN, $"+ LogLevel \t\t\t\t\t= {LogInfo.eLogLevel}");     // WARN -> always visible unless ERROR only
                foreach (string szCfgWarn in xListCfgWarn)
                    LogInfo.WriteLog(LogLevel.WARN, "- Warning LoadTCT_Configuration : " + szCfgWarn);
                LogInfo.WriteLog($"+ AvcStatusType \t\t\t\t= {iAvcStatusType}");
                LogInfo.WriteLog($"+ ManualImportPath \t\t\t\t= {szManualImportPath}");
                LogInfo.WriteLog($"+ DmtCardType \t\t\t\t= {iDmtCardType}");
                LogInfo.WriteLog($"+ DmtCardEffectiveDate \t\t\t= {szDmtCardEffectiveDate}");
                //LogInfo.WriteLog($"+ Current LogPath \t\t\t= {LogInfo.szLogPath}");
                LogInfo.WriteLog($"+ ImportStaffIntervalMin \t\t\t= {iImportStaffIntervalMin}");
                LogInfo.WriteLog($"+ AllowTollRoleGroupID \t\t\t= {szAllowTollRoleGroupID}");
                LogInfo.WriteLog($"+ AllowMaintenanceRoleGroupID \t\t= {szAllowMaintenanceRoleGroupID}");

                LogInfo.WriteLog($"+ DC_RabbitMQ_Enable \t\t\t\t= {bEnableDcRabbitMQ}");
                LogInfo.WriteLog($"+ DC_RabbitMQ_ServerHostName \t\t\t= {szDC_RabbitMQ_ServerHostName}");
                LogInfo.WriteLog($"+ DC_RabbitMQ_VirtualHost \t\t\t= {szDC_RabbitMQ_VirtualHost}");
                LogInfo.WriteLog($"+ DC_RabbitMQ_Port \t\t\t\t= {iDC_RabbitMQ_Port}");
                LogInfo.WriteLog($"+ DC_RabbitMQ_User \t\t\t\t= {szDC_RabbitMQ_User}");
                LogInfo.WriteLog($"+ DC_RabbitMQ_Password \t\t\t= {LogInfo.MaskSecret(szDC_RabbitMQ_Password)}");
                LogInfo.WriteLog($"+ DC_RabbitMQ_TransQueueName \t\t\t= {szDC_RabbitMQ_TransQueueName}");
                LogInfo.WriteLog($"+ DC_RabbitMQ_TransExchangeName \t\t= {szDC_RabbitMQ_TransExchangeName}");
                LogInfo.WriteLog($"+ DC_RabbitMQ_EventQueueName \t\t\t= {szDC_RabbitMQ_EventQueueName}");
                LogInfo.WriteLog($"+ DC_RabbitMQ_EventExchangeName \t\t= {szDC_RabbitMQ_EventExchangeName}");
                LogInfo.WriteLog($"+ DC_RabbitMQ_ParamsQueueName \t\t= {szDC_RabbitMQ_ParamsQueueName}");
                LogInfo.WriteLog($"+ DC_RabbitMQ_ParamsExchangeName \t\t= {szDC_RabbitMQ_ParamsExchangeName}");
                LogInfo.WriteLog($"+ DC_RabbitMQ_IPAddress \t\t\t= {szDC_RabbitMQ_ServerIPAddress}");
                LogInfo.WriteLog($"+ DC_RabbitMQ_ServerPort \t\t\t= {iDC_RabbitMQ_ServerPort}");

                LogInfo.WriteLog($"+ ETC_RabbitMQ_Enable \t\t\t= {bEnableEtcRabbitMQ}");
                LogInfo.WriteLog($"+ ETC_RabbitMQ_ServerHostName \t\t= {szETC_RabbitMQ_ServerHostName}");
                LogInfo.WriteLog($"+ ETC_RabbitMQ_VirtualHost \t\t\t= {szETC_RabbitMQ_VirtualHost}");
                LogInfo.WriteLog($"+ ETC_RabbitMQ_Port \t\t\t\t= {iETC_RabbitMQ_Port}");
                LogInfo.WriteLog($"+ ETC_RabbitMQ_User \t\t\t\t= {szETC_RabbitMQ_User}");
                LogInfo.WriteLog($"+ ETC_RabbitMQ_Password \t\t\t= {LogInfo.MaskSecret(szETC_RabbitMQ_Password)}");
                LogInfo.WriteLog($"+ ETC_RabbitMQ_LaneStateQueueName \t\t= {szETC_RabbitMQ_LaneStateQueueName}");
                LogInfo.WriteLog($"+ ETC_RabbitMQ_LaneStateExchangeName \t\t= {szETC_RabbitMQ_LaneStateExchangeName}");
                LogInfo.WriteLog($"+ ETC_RabbitMQ_CompilePassageQueueName \t= {szETC_RabbitMQ_CompilePassageQueueName}");
                LogInfo.WriteLog($"+ ETC_RabbitMQ_CompilePassageExchangeName \t= {szETC_RabbitMQ_CompilePassageExchangeName}");
                LogInfo.WriteLog($"+ ETC_RabbitMQ_ManualActionQueueName \t\t= {szETC_RabbitMQ_ManualActionQueueName}");
                LogInfo.WriteLog($"+ ETC_RabbitMQ_ManualActionExchangeName \t= {szETC_RabbitMQ_ManualActionExchangeName}");
                LogInfo.WriteLog($"+ ETC_RabbitMQ_IPAddress \t\t\t= {szETC_RabbitMQ_ServerIPAddress}");
                LogInfo.WriteLog($"+ ETC_RabbitMQ_ServerPort \t\t\t= {iETC_RabbitMQ_ServerPort}");
                LogInfo.WriteLog($"+ ETC_RabbitMQ_WriteLogETC \t\t\t= {bETC_RabbitMQ_WriteLogETC}");

                LogInfo.WriteLog($"+ MTS_Enable \t\t\t\t\t= {bEnableMTS}");
                LogInfo.WriteLog($"+ MTS_TimerSecCheckStatus \t\t\t= {iMTS_TimerSecCheckStatus}");
                LogInfo.WriteLog($"+ MTS_RabbitMQ_ServerHostName \t\t= {szMTS_RabbitMQ_ServerHostName}");
                LogInfo.WriteLog($"+ MTS_RabbitMQ_VirtualHost \t\t\t= {szMTS_RabbitMQ_VirtualHost}");
                LogInfo.WriteLog($"+ MTS_RabbitMQ_Port \t\t\t\t= {iMTS_RabbitMQ_Port}");
                LogInfo.WriteLog($"+ MTS_RabbitMQ_User \t\t\t\t= {szMTS_RabbitMQ_User}");
                LogInfo.WriteLog($"+ MTS_RabbitMQ_Password \t\t\t= {LogInfo.MaskSecret(szMTS_RabbitMQ_Password)}");
                LogInfo.WriteLog($"+ MTS_RabbitMQ_MtsQueueName \t\t\t= {szMTS_RabbitMQ_MtsQueueName}");
                LogInfo.WriteLog($"+ MTS_RabbitMQ_MtsExchangeName \t\t= {szMTS_RabbitMQ_MtsExchangeName}");
                LogInfo.WriteLog($"+ MTS_RabbitMQ_MtsMtsRoutingKey \t\t= {szMTS_RabbitMQ_MtsRoutingKey}");
                LogInfo.WriteLog($"+ MTS_RabbitMQ_IPAddress \t\t\t= {szMTS_RabbitMQ_ServerIPAddress}");
                LogInfo.WriteLog($"+ MTS_RabbitMQ_ServerPort \t\t\t= {iMTS_RabbitMQ_ServerPort}");
                LogInfo.WriteLog($"+ MTS_RabbitMQ_EtcStatusDbUrl \t\t= {szMTS_EtcStatusDbUrl}");
                LogInfo.WriteLog($"+ MTS_RabbitMQ_EtcStatusDbUrl2 \t\t= {szMTS_EtcStatusDbUrl2}");
                LogInfo.WriteLog($"+ MTS_RabbitMQ_EtcStatusDbUrl3 \t\t= {szMTS_EtcStatusDbUrl3}");
                LogInfo.WriteLog($"+ MTS_RabbitMQ_EtcStatusDbUrl4 \t\t= {szMTS_EtcStatusDbUrl4}");
                LogInfo.WriteLog($"+ MTS_RabbitMQ_AlarmDeviceList \t\t= {szMTS_AlarmDeviceList}");

                LogInfo.WriteLog($"+ MTS_Monitor_DiskPath1 \t\t\t= {szMTS_Monitor_DiskPath_01}");
                LogInfo.WriteLog($"+ MTS_Monitor_DiskPath2 \t\t\t= {szMTS_Monitor_DiskPath_02}");
                LogInfo.WriteLog($"+ MTS_Monitor_DiskPath3 \t\t\t= {szMTS_Monitor_DiskPath_03}");
                LogInfo.WriteLog($"+ MTS_Monitor_DiskPath4 \t\t\t= {szMTS_Monitor_DiskPath_04}");
                LogInfo.WriteLog($"+ MTS_Monitor_DiskPath5 \t\t\t= {szMTS_Monitor_DiskPath_05}");
                LogInfo.WriteLog($"+ MTS_Monitor_DiskPath6 \t\t\t= {szMTS_Monitor_DiskPath_06}");
                LogInfo.WriteLog($"+ MTS_Monitor_DiskPath7 \t\t\t= {szMTS_Monitor_DiskPath_07}");
                LogInfo.WriteLog($"+ MTS_Monitor_DiskPath8 \t\t\t= {szMTS_Monitor_DiskPath_08}");
                LogInfo.WriteLog($"+ MTS_Monitor_DiskPath9 \t\t\t= {szMTS_Monitor_DiskPath_09}");
                LogInfo.WriteLog($"+ MTS_Monitor_DiskPath10 \t\t\t= {szMTS_Monitor_DiskPath_10}");
                LogInfo.WriteLog($"+ MTS_Monitor_TFI_IPAddress \t\t\t= {szMTS_Monitor_TFI_IPAddress}");
                LogInfo.WriteLog($"+ MTS_Monitor_TFI_Port \t\t\t= {iMTS_Monitor_TFI_Port}");
                LogInfo.WriteLog($"+ MTS_Monitor_CRS_IPAddress \t\t\t= {szMTS_Monitor_CRS_IPAddress}");
                LogInfo.WriteLog($"+ MTS_Monitor_CRS_Port \t\t\t= {iMTS_Monitor_CRS_Port}");
                LogInfo.WriteLog($"+ MTS_Monitor_Product_Printer \t\t= {szMTS_Monitor_Product_Printer}");
                LogInfo.WriteLog($"+ MTS_Monitor_Product_SCR \t\t\t= {szMTS_Monitor_Product_SCR}");
                LogInfo.WriteLog($"+ MTS_Monitor_Product_Keyboard \t\t= {szMTS_Monitor_Product_Keyboard}");
                LogInfo.WriteLog($"+ MTS_Monitor_TimeAlertLoopNoOB \t\t= {iMTS_Monitor_TimeAlertLoopNoOB}");
                LogInfo.WriteLog($"+ MTS_Monitor_IntervalSecAlertLoopNoOB \t= {iMTS_Monitor_IntervalSecAlertLoopNoOB}");
                LogInfo.WriteLog($"+ MTS_Monitor_IntervalSecGetEtcStatus \t= {iMTS_Monitor_IntervalSecGetEtcStatus}");

                LogInfo.WriteLog($"+ DSRC_Antenna \t\t\t\t= {bEnableAntenna}");
                LogInfo.WriteLog($"+ MainAntenna_IPAddress \t\t\t= {szMainAntenna_IPAddress}");
                LogInfo.WriteLog($"+ MainAntenna_SocketPort \t\t\t= {iMainAntenna_SocketPort}");
                LogInfo.WriteLog($"+ SecondaryAntenna_IPAddress \t\t\t= {szSecondaryAntenna_IPAddress}");
                LogInfo.WriteLog($"+ SecondaryAntenna_SocketPort \t\t= {iSecondaryAntenna_SocketPort}");

                LogInfo.WriteLog($"+ Payment_PaymentCode_06 \t\t\t= {szPaymentCode_06}");
                LogInfo.WriteLog($"+ Payment_PaymentName_06 \t\t\t= {szPaymentName_06}");
                LogInfo.WriteLog($"+ Payment_PaymentBtn_06 \t\t\t= {szPaymentBtn_06}");
                LogInfo.WriteLog($"+ Payment_PaymentCRS_06 \t\t\t= {szPaymentCRS_06}");
                LogInfo.WriteLog($"+ Payment_PaymentCode_07 \t\t\t= {szPaymentCode_07}");
                LogInfo.WriteLog($"+ Payment_PaymentName_07 \t\t\t= {szPaymentName_07}");
                LogInfo.WriteLog($"+ Payment_PaymentBtn_07 \t\t\t= {szPaymentBtn_07}");
                LogInfo.WriteLog($"+ Payment_PaymentCRS_07 \t\t\t= {szPaymentCRS_07}");
                LogInfo.WriteLog($"+ Payment_PaymentCode_08 \t\t\t= {szPaymentCode_08}");
                LogInfo.WriteLog($"+ Payment_PaymentName_08 \t\t\t= {szPaymentName_08}");
                LogInfo.WriteLog($"+ Payment_PaymentBtn_08 \t\t\t= {szPaymentBtn_08}");
                LogInfo.WriteLog($"+ Payment_PaymentCRS_08 \t\t\t= {szPaymentCRS_08}");
                LogInfo.WriteLog($"+ Payment_PaymentCode_09 \t\t\t= {szPaymentCode_09}");
                LogInfo.WriteLog($"+ Payment_PaymentName_09 \t\t\t= {szPaymentName_09}");
                LogInfo.WriteLog($"+ Payment_PaymentBtn_09 \t\t\t= {szPaymentBtn_09}");
                LogInfo.WriteLog($"+ Payment_PaymentCRS_09 \t\t\t= {szPaymentCRS_09}");

                LogInfo.WriteLog($"+ TFI_ScpEnable \t\t\t= {bEnableTfiScpFiles}");
                LogInfo.WriteLog($"+ TFI_ScpHostName \t\t\t= {szTfiScpHostName}");
                //LogInfo.WriteLog($"+ TFI_ScpSourcePath \t\t\t= {szTfiScpSourcePath}");
                LogInfo.WriteLog($"+ TFI_ScpDestPath \t\t\t= {szTfiScpDestPath}");
                LogInfo.WriteLog($"+ TFI_ScpUserName \t\t\t= {szTfiScpUserName}");
                LogInfo.WriteLog($"+ TFI_ScpPassword \t\t\t= {LogInfo.MaskSecret(szTfiScpPassword)}");

                //----------------------------------------------------------------------------------
                if (iLaneConfigType == 0)
                {
                    bEnableEtcRabbitMQ = false;
                    LogInfo.WriteLog("+ LaneConfigType = MTC, then Auto disalbe Enable ETC Rabbit MQ.");
                }

                LogInfo.WriteLog(LogLevel.DEBUG, $"+ LoadTCT_Configuration : file={configFile}, eCurLaneType={eCurLaneType}, eLaneDirection={eLaneDirection}, EtcRabbitMQ={bEnableEtcRabbitMQ}, warnings={xListCfgWarn.Count}");
                LogInfo.WriteLog("-----------------------------------------------");
                LogInfo.WriteLog("+ Success LoadTCT_Configuration");

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error Load TCT_Configuration : " + err.Message);
                //LogInfo.CreateLogPath();
                LogInfo.WriteLog("- Error Load TCT_Configuration : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, $"- LoadTCT_Configuration : stack={err.StackTrace}");
                LogInfo.WriteLog(LogLevel.ERROR, "- Error LoadTCT_Configuration : application exit(1)");

                Application.Quit();
                Environment.Exit(1);

                return false;
            }
        }

        public static bool WriteTCT_Configuration(string szMainEntity, string szEntity, int iValue)
        {
            try
            {
                //string curPath          = new FileInfo(Assembly.GetEntryAssembly().Location).Directory.ToString();
                //string configFile       = curPath + @"/TCT.config";
                string configFile       = TCT_Lib.baseConfigFolder + @"/TCT.config";
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ WriteTCT_Configuration : [{szMainEntity}][{szEntity}] = {iValue}, file={configFile}");
                string configJson       = File.ReadAllText(configFile);

                //----------------------------------------------------------------------------
                xTctConfig[szMainEntity][szEntity]  = iValue;
                int iLaneCurrentType                = xTctConfig["Lane"]["LaneType"] ?? 0;
                SetCurrentLaneType(iLaneCurrentType);

                if (iLaneConfigType == 1)
                {
                    //--- Pure ETC
                    xTctConfig["ETC_RabbitMQ"]["Enable"] = 1;
                }
                else if (iLaneConfigType == 2)
                {
                    //--- Dual, ETC
                    if (iLaneCurrentType > 0)
                        xTctConfig["ETC_RabbitMQ"]["Enable"] = 1;
                }

                //File.WriteAllText(configFile, JsonConvert.SerializeObject(xTctConfig).Replace(",", ",\n\t").Replace("},\n\t", "},\n").Replace(":{", ":{\n\t").Replace("},", "\n\t},").Replace("}}", "\n\t}\n}"));
                //File.WriteAllText(configFile, JsonConvert.SerializeObject(xTctConfig).Replace(",", ",\n\t").Replace(":{", ":{\n\t").Replace("},", "\n\t},").Replace("}}", "\n\t}\n}"));
                File.WriteAllText(configFile, JsonConvert.SerializeObject(xTctConfig, Formatting.Indented).Replace(@"\r\n\t", ""));
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ WriteTCT_Configuration : saved, LaneConfigType={iLaneConfigType}, LaneType={iLaneCurrentType} -> eCurLaneType={eCurLaneType}");
                //-------------------------------------------------------------------
                //File.WriteAllText(configFile, configJson.Replace(@"\n", "").Replace(@"\t", ""));

                //--------------------------------------------------------------------
                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error LoadTCT_Configuration : " + err.Message);
                LogInfo.WriteLog(LogLevel.ERROR, $"- Error WriteTCT_Configuration : [{szMainEntity}][{szEntity}]={iValue} : {err.Message}");
                LogInfo.WriteLog(LogLevel.DEBUG, $"- WriteTCT_Configuration : stack={err.StackTrace}");
                return false;
            }
        }

        public static void SetCurrentLaneType(int iLaneType)
        {
            LaneType eOldLaneType = TCT_Lib.eCurLaneType;
            if (TCT_Lib.iLaneConfigType == 2)
            {
                if (iLaneType == 0)
                    TCT_Lib.eCurLaneType = LaneType.MTC;
                else if (iLaneType == 1)
                    TCT_Lib.eCurLaneType = LaneType.ETC;
                else if (iLaneType == 2)
                    TCT_Lib.eCurLaneType = LaneType.DUAL;
                else
                    TCT_Lib.eCurLaneType = LaneType.MTC;
            }
            else if (TCT_Lib.iLaneConfigType == 0)
                TCT_Lib.eCurLaneType = LaneType.MTC;
            else if (TCT_Lib.iLaneConfigType == 1)
                TCT_Lib.eCurLaneType = LaneType.ETC;
            else
                TCT_Lib.eCurLaneType = LaneType.MTC;
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ SetCurrentLaneType : LaneConfigType={TCT_Lib.iLaneConfigType}, LaneType={iLaneType}, eCurLaneType {eOldLaneType} -> {TCT_Lib.eCurLaneType}");
        }

        public static void SetCurrentLaneDirection(int iLaneDirection)
        {
            if (iLaneDirection == 0)
                TCT_Lib.eLaneDirection = LaneDirection.NORMAL;
            else if (iLaneDirection == 1)
                TCT_Lib.eLaneDirection = LaneDirection.REVERSE;
            else
                TCT_Lib.eLaneDirection = LaneDirection.NORMAL;
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ SetCurrentLaneDirection : LaneDirection={iLaneDirection} -> {TCT_Lib.eLaneDirection}");
        }

        public static string GetCurLaneTypeName()
        {
            if (TCT_Lib.iLaneConfigType == 2)
            {
                if (TCT_Lib.eCurLaneType == LaneType.MTC)
                    return "Dual-MTC";
                else if (TCT_Lib.eCurLaneType == LaneType.ETC)
                    return "Dual-ETC";
                else if (TCT_Lib.eCurLaneType == LaneType.DUAL)
                    return "Dual-MTC/ETC";
                else
                    return "- Dual-Unknown -";
            }
            else
            {
                if (TCT_Lib.eCurLaneType == LaneType.MTC)
                    return "MTC";
                else if (TCT_Lib.eCurLaneType == LaneType.ETC)
                    return "ETC";
                else if (TCT_Lib.eCurLaneType == LaneType.DUAL)
                    return "MTC/ETC";
                else
                    return "- Unknown -";
            }
        }

        public static string GetLaneTypeName(LaneType laneType)
        {
            if (laneType == LaneType.MTC)
                return "MTC";
            else if (laneType == LaneType.ETC)
                return "ETC";
            else if (laneType == LaneType.DUAL)
                return "DUAL";
            else
                return "UNKNOWN";
        }

        public static string GetLaneModeName(LaneMode laneMode)
        {
            if (laneMode == LaneMode.NORMAL)
                return "NORMAL";
            else if (laneMode == LaneMode.CLOSED)
                return "CLOSED";
            else if (laneMode == LaneMode.HPMC)
                return "HPMC";
            else if (laneMode == LaneMode.MAINTENANCE)
                return "MAINTENANCE";
            else
                return "UNKNOWN";
        }

        public static string GetLaneDirection(LaneDirection laneDirection)
        {
            if (laneDirection == LaneDirection.NORMAL)
                return "NORMAL";
            else if (laneDirection == LaneDirection.REVERSE)
                return "REVERSE";
            else
                return "UNKNOWN";
        }

        public static string GetPayMethodName(int iPayMethod)
        {
            switch (iPayMethod)
            {
                case (int)PayType.NONE:         return "";
                case (int)PayType.CASH:         return "Cash";
                case (int)PayType.COUPON:       return "COUPON";
                case (int)PayType.EasyPass:     return "EASYPASS";
                case (int)PayType.MPass:        return "MPASS";
                case (int)PayType.EMV:          return "EMV";
                case (int)PayType.QrCode:       return "QR Code";
                case (int)PayType.DMT_CARD:     return "DMT CARD";
                case (int)PayType.LeftExit:     return "LEFT EXIT";
                case (int)PayType.Violation:    return "VIOLATION";
                case (int)PayType.EXEMPT:       return "EXEMPT";
                case (int)PayType.ePurse:       return "ePurse";
                case (int)PayType.Mangmoom:     return "Mangmoom";
                case (int)PayType.HPMC:         return "HPMC";
                case (int)PayType.PM6:          return TCT_Lib.szPaymentName_06;
                case (int)PayType.PM7:          return TCT_Lib.szPaymentName_07;
                case (int)PayType.PM8:          return TCT_Lib.szPaymentName_08;
                case (int)PayType.PM9:          return TCT_Lib.szPaymentName_09;
                default:                        return "";
            }
        }

        public static string GetPayMethodNameRabbitMQ(int iPayMethod)
        {
            switch (iPayMethod)
            {
                case (int)PayType.NONE:         return "UNK";
                case (int)PayType.CASH:         return "CASH";
                case (int)PayType.COUPON:       return "COUPON";
                case (int)PayType.EasyPass:     return "EPASS";
                case (int)PayType.MPass:        return "MPASS";
                case (int)PayType.EMV:          return "EMV";
                case (int)PayType.QrCode:       return "QRCode";
                case (int)PayType.DMT_CARD:     return "DMT CARD";
                case (int)PayType.LeftExit:     return "LEFT EXIT";
                case (int)PayType.Violation:    return "VIO";
                case (int)PayType.EXEMPT:       return "EXEMPT";
                case (int)PayType.ePurse:       return "ePur";
                case (int)PayType.Mangmoom:     return "MM";
                case (int)PayType.HPMC:         return "HPMC";
                case (int)PayType.PM6:          return TCT_Lib.szPaymentCode_06;
                case (int)PayType.PM7:          return TCT_Lib.szPaymentCode_07;
                case (int)PayType.PM8:          return TCT_Lib.szPaymentCode_08;
                case (int)PayType.PM9:          return TCT_Lib.szPaymentCode_09;
                default:                        return "";
            }
        }

        public static string GetClassTypeShortName(int iClassType)
        {
            switch (iClassType)
            {
                case (int)VehicleType.NONE:         return "";
                case (int)VehicleType.VIOLATION:    return "V";
                case (int)VehicleType.CLASS_1:      return "1";
                case (int)VehicleType.CLASS_2:      return "2";
                case (int)VehicleType.CLASS_3:      return "3";
                case (int)VehicleType.CLASS_4:      return "4";
                case (int)VehicleType.EMERGENCY:    return "E";
                case (int)VehicleType.TOWING:       return "T";
                case (int)VehicleType.EXEMPT:       return "E";
                case (int)VehicleType.LEFT_EXIT:    return "L";
                case (int)VehicleType.HPMC:         return "H";
                default:                            return "X";
            }
        }

        public static void ClearCurTransData()
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ ClearCurTransData : clear TC_Class={iCurTC_Class}, PayType={xCurPayment?.payType}, Fare={fCurFareAmount}, ReceiptNo={szCurFareReceiptNo}, IsTowing={iIsTowing}, TowingState={eTowingState}");
            //xCurAVC                 = new AVC_Info();
            xCurTAG                 = new TAG_Info();

            xCurPayment             = new Pay_Info();
            xCurPayment.payType     = (int)PayType.NONE;

            iCurTC_Class            = 0;
            szCurTransRemark        = "";

            fCurFareAmount          = 0;
            szCurFareReceiptNo      = "";

            iIsTowing               = 0;
            iTowingParentID         = 0;
            xTowingAVC              = new AVC_Info();
            xTowingPayment          = new Pay_Info();
            xTowingPayment.payType  = (int)PayType.NONE;
            iTowingTC_Class         = 0;
            fTowingFareAmount       = 0;
            eTowingState            = TowingState.None;
            bStartEndLoop           = false;
            bAfterPayment           = false;
            bHasGenReceiptNo        = false;
            bHasGenTowingReceiptNo  = false;
            bEtcCtrlIO              = false;

            TCT_Lib.szQrRef1        = "";
            TCT_Lib.szQrRef2        = "";
        }

        public static bool TestConnection(string szIPAddress, int iPort=80)
        {
            if (szIPAddress.Trim() == "")
            {
                Console.WriteLine("- Fail TestConnection : No IPAddress");
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ TestConnection : no IPAddress (Port={iPort}) -> false");
                return false;
            }



            bool bConnect = false;
            try
            {
                IPAddress ipAddr                = IPAddress.Parse(TCT_Lib.szEdcIPAddress);
                IPEndPoint sendEndPoint         = new IPEndPoint(ipAddr, TCT_Lib.iEdcSendPort);
                System.Net.Sockets.Socket socket = new System.Net.Sockets.Socket(sendEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.SendTimeout              = 1 * 1000;
                socket.ReceiveTimeout           = 1 * 1000;

                var swConnect = System.Diagnostics.Stopwatch.StartNew();
                socket.Connect(sendEndPoint);
                bConnect = socket.Connected;
                //socket.Disconnect(true);
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ TestConnection : IP={szIPAddress}, Port={iPort} (connects EDC {TCT_Lib.szEdcIPAddress}:{TCT_Lib.iEdcSendPort}) -> connected={bConnect} ({swConnect.ElapsedMilliseconds} ms)");

                socket.Close();
                socket.Dispose();

                return bConnect;
            }
            catch (SocketException err)
            {
                Console.WriteLine("- Fail SocketException : " + err.Message);
                LogInfo.WriteLaneEvent($"- Fail SocketException TestConnection IP={szIPAddress}, Port={iPort} : {err.Message}");
                LogInfo.WriteLog(LogLevel.DEBUG, $"- TestConnection : EDC {TCT_Lib.szEdcIPAddress}:{TCT_Lib.iEdcSendPort}, SocketErrorCode={err.SocketErrorCode}");
                return false;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Fail Connection : " + err.Message);
                LogInfo.WriteLaneEvent($"- Fail TestConnection IP={szIPAddress}, Port={iPort} : {err.Message}");
                LogInfo.WriteLog(LogLevel.DEBUG, $"- TestConnection : stack={err.StackTrace}");
                return false;
            }
        }


        public static bool TestConnection_V0(string szIPAddress, int iPort=80)
        {
            if (szIPAddress.Trim() == "")
            {
                Console.WriteLine("- Fail TestConnection : No IPAddress");
                return false;
            }

            try
            {
                var client = new TcpClient();
                if (!client.ConnectAsync(szIPAddress, iPort).Wait(100))
                {
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ TestConnection_V0 : IP={szIPAddress}, Port={iPort} -> not connected within 100 ms");
                    Console.WriteLine("- Fail Connection");
                    client.Close();
                    return false;
                }
                else
                {
                    Console.WriteLine("+ Success Connection");
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ TestConnection_V0 : IP={szIPAddress}, Port={iPort} -> connected");
                    client.Close();
                    client.Dispose();
                    client = null;
                    return true;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Fail Connection : " + err.Message);
                LogInfo.WriteLaneEvent($"- Fail TestConnection IP={szIPAddress}, Port={iPort} : {err.Message}");
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
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ TestNetworkConnectionAsync : IP={szIPAddress}, Port={iPort}, timeout={timeoutMs} ms -> connected={client.Connected}");
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
                LogInfo.WriteLog(LogLevel.ERROR, $"- Error TestNetworkConnectionAsync : IP={szIPAddress}, Port={iPort} : {err.Message}");
                LogInfo.WriteLog(LogLevel.DEBUG, $"- TestNetworkConnectionAsync : stack={err.StackTrace}");
                return false;
            }
        }

        async public static Task<bool> IsPortReachable(string host, int port = 80, int msTimeout = 300)
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
                    var swConnect = System.Diagnostics.Stopwatch.StartNew();
                    socket.ConnectAsync(socketEventArg);
                    clientDone.WaitOne(msTimeout);
                    LogInfo.WriteLog(LogLevel.TRACE, $"+ IsPortReachable : {host}:{port} -> {reachable} ({swConnect.ElapsedMilliseconds} ms, timeout={msTimeout})");

                    return reachable;
                }
            });
        }

        public static bool CheckConnectivity(string ipAddress, int msTimeout=500)
        {
            bool connectionExists = false;
            try
            {
                Ping pingSender         = new Ping();
                PingOptions options     = new PingOptions();
                options.DontFragment    = true;
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    PingReply reply     = pingSender.Send(ipAddress, msTimeout);
                    connectionExists    = reply.Status == IPStatus.Success ? true : false;
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ CheckConnectivity : ping {ipAddress} -> {reply?.Status} ({reply?.RoundtripTime} ms)");
                }
                else
                    LogInfo.WriteLog(LogLevel.DEBUG, "+ CheckConnectivity : no IP address -> false");
            }
            catch (PingException err)
            {
                LogInfo.WriteLog($"- Error Ping CheckConnectivity : IP={ipAddress}, Error={err.Message}");
            }
            return connectionExists;
        }

        public async static Task<bool> CheckAsyncPing(string ipAddress, int msTimeout = 500)
        {
            bool connectionExists   = false;
            try
            {
                Ping pingSender     = new Ping();
                await Task.Run(() =>
                {
                    PingReply reply     = pingSender.Send(ipAddress, msTimeout);
                    connectionExists    = reply.Status == IPStatus.Success ? true : false;
                    return connectionExists;
                });
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ CheckAsyncPing : ping {ipAddress} -> {connectionExists}");
                return connectionExists;
            }
            catch (PingException err)
            {
                LogInfo.WriteLog($"- Error Ping CheckConnectivity : IP={ipAddress}, Error={err.Message}");
                return false;
            }
        }

        //---------------------------------------------------------------------------------------------------
        public static bool SendLaneEventObjToRTO(int iEventID)
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ SendLaneEventObjToRTO : EventRtoID={iEventID}");
            if (iEventID == 0)
                return false;

            try
            {
                var xLaneEvent      = TCT_Obj.GenerateLaneEventRTO(iEventID);
                string szLaneEvent  = JsonConvert.SerializeObject(xLaneEvent, Formatting.Indented);
                //Console.WriteLine("+ RTO Lane Event = " + szLaneEvent);
                //LogInfo.WriteLog($"+ RTO Lane Event : EventID={iEventID}");
                LogInfo.WriteLog(LogLevel.TRACE, $"+ SendLaneEventObjToRTO : EventRtoID={iEventID}, json={szLaneEvent}");
                bool bResult        = TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("LANE_EVENT", xLaneEvent);
                LogInfo.WriteLog(bResult ? LogLevel.DEBUG : LogLevel.WARN, $"{(bResult ? "+" : "- Warning")} SendLaneEventObjToRTO : EventRtoID={iEventID}, sent={bResult}, obj={(xLaneEvent == null ? "null" : "ok")}");
                UpdateLaneEventToRTO(iEventID, bResult);

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error SendLaneEventObjToRTO : " + err.Message);
                LogInfo.WriteLog("- Error SendLaneEventObjToRTO : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, $"- SendLaneEventObjToRTO : EventRtoID={iEventID}, stack={err.StackTrace}");
                return false;
            }
        }

        private static void UpdateLaneEventToRTO(int iEventID, bool bResult)
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ UpdateLaneEventToRTO : EventRtoID={iEventID}, result={bResult}");
            if (iEventID == 0)
                return;

            string szQuery = "";
            if (bResult)
            {
                szQuery = "UPDATE LaneEventRTO " +
                        $"SET SendStatusRTO=1, SendDateTimeRTO='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' " +
                        $"WHERE EventRtoID={iEventID}";
            }
            else
            {
                szQuery = "UPDATE LaneEventRTO " +
                        $"SET SendRetryRTO=SendRetryRTO+1, SendDateTimeRTO='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' " +
                        $"WHERE EventRTOID={iEventID}";
            }
            //LogInfo.WriteLog("+ Query : " + szQuery);
            LogInfo.WriteLog(LogLevel.TRACE, $"+ UpdateLaneEventToRTO : SQL={szQuery}");
            LaneDB.Execute(szQuery);
        }
        
        public static bool SendLaneAlarmObjToRTO(int iAlarmID)
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ SendLaneAlarmObjToRTO : AlarmRtoID={iAlarmID}");
            if (iAlarmID == 0)
                return false;

            try
            {
                var xLaneAlarm      = TCT_Obj.GenerateLaneAlarmRTO(iAlarmID);
                string szLaneAlarm  = JsonConvert.SerializeObject(xLaneAlarm, Formatting.Indented);
                //Console.WriteLine("+ RTO Lane Alarm = " + szLaneAlarm);
                LogInfo.WriteLog($"+ RTO Lane Alarm ={szLaneAlarm}");
                bool bResult = TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("LANE_ALARM", xLaneAlarm);
                LogInfo.WriteLog(bResult ? LogLevel.DEBUG : LogLevel.WARN, $"{(bResult ? "+" : "- Warning")} SendLaneAlarmObjToRTO : AlarmRtoID={iAlarmID}, sent={bResult}, obj={(xLaneAlarm == null ? "null" : "ok")}");
                UpdateLaneAlarmToRTO(iAlarmID, bResult);

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error SendLaneAlarmObjToRTO : " + err.Message);
                LogInfo.WriteLog("- Error SendLaneAlarmObjToRTO : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, $"- SendLaneAlarmObjToRTO : AlarmRtoID={iAlarmID}, stack={err.StackTrace}");
                return false;
            }
        }

        private static void UpdateLaneAlarmToRTO(int iAlarmID, bool bResult)
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ UpdateLaneAlarmToRTO : AlarmRtoID={iAlarmID}, result={bResult}");
            if (iAlarmID == 0)
                return;

            string szQuery = "";
            if (bResult)
            {
                szQuery = "UPDATE LaneAlarmRTO " +
                        $"SET SendStatusRTO=1, SendDateTimeRTO='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' " +
                        $"WHERE AlarmRtoID={iAlarmID}";
            }
            else
            {
                szQuery = "UPDATE LaneAlarmRTO " +
                        $"SET SendRetryRTO=SendRetryRTO+1, SendDateTimeRTO='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' " +
                        $"WHERE AlarmRtoID={iAlarmID}";
            }
            //LogInfo.WriteLog("+ Query : " + szQuery);
            LogInfo.WriteLog(LogLevel.TRACE, $"+ UpdateLaneAlarmToRTO : SQL={szQuery}");
            LaneDB.Execute(szQuery);
        }


        async public static Task<bool> AutoResendLaneTransactionToRTO()
        {
            //if (!TCT_Lib.bRTO_Connected)
            //    return false;

            string szQuery = "SELECT * FROM LaneTransaction " +
                                "WHERE SendStatusRTO=0 " +
                                $"AND DATE(TransactionDateTime)>='2021-01-01' " +   // Dummy Date, Take out when production
                                "ORDER BY TransactionID";

            LogInfo.WriteLog(LogLevel.TRACE, $"+ AutoResendLaneTransactionToRTO : SQL={szQuery}");
            DataTable dtLaneTrans = TCT_Lib.LaneDB.List(szQuery);
            LogInfo.WriteLog("+ AutoResendLaneTransactionToRTO : Total=" + dtLaneTrans.Rows.Count);

            bool bResult = true;
            for (int i = 0; i < dtLaneTrans.Rows.Count; i++)
            {
                int iTransID = int.Parse(dtLaneTrans.Rows[i]["TransactionID"].ToString());
                bResult      = SendLaneTransObjToRTO(iTransID, true);
                if (!bResult)
                {
                    LogInfo.WriteLog(LogLevel.WARN, $"- Warning AutoResendLaneTransactionToRTO : send TransID={iTransID} failed at {i + 1}/{dtLaneTrans.Rows.Count} -> stop, retry next cycle");
                    break;
                }
                //Thread.Sleep(100);
                await Task.Delay(100);
            }

            LogInfo.WriteLog(LogLevel.DEBUG, $"+ AutoResendLaneTransactionToRTO : done result={bResult}");
            return bResult;

/*
            szQuery = "SELECT * FROM LaneTransaction " +
                                "WHERE SendStatusSupAdjust=0 " +
                                //$"AND DATE(TransactionDateTime)='{DateTime.Today.ToString("yyyy-MM-dd")}' " +
                                $"AND DATE(TransactionDateTime)>='2021-01-01' " +   // Dummy Date, Take out when production
                                "AND Tag_SignalCode='' " +
                                "ORDER BY TransactionID";
            dtLaneTrans = TCT_Lib.LaneDB.List(szQuery);
            for (int i = 0; i < dtLaneTrans.Rows.Count; i++)
            {
                int iTransID    = int.Parse(dtLaneTrans.Rows[i]["TransactionID"].ToString());
                bResult         = SendLaneTransObjToRTO(iTransID, true);
                if (!bResult)
                    break;

                //Thread.Sleep(100);
                await Task.Delay(100);
            }

            return bResult;
 */                      
        }

        public static bool SendFromReqLaneTransactionToRTO(string szDate, int iJobNo, int iStart_nTrx, int iEnd_nTrx)
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ SendFromReqLaneTransactionToRTO : Date={szDate}, JobNo={iJobNo}, nTrx={iStart_nTrx}..{iEnd_nTrx}, RTO connected={TCT_Lib.bRTO_Connected}");
            if (!TCT_Lib.bRTO_Connected)
                return false;

            bool bResult    = true;
            string szQuery  = "SELECT * FROM LaneTransaction " +
                                $"WHERE JobNo={iJobNo} AND (nTrx>={iStart_nTrx} AND nTrx<={iEnd_nTrx}) " +
                                $"AND DATE(TransactionDateTime)>='{szDate}' " +   // Dummy Date, Take out when production
                                "ORDER BY TransactionID";

            LogInfo.WriteLog(LogLevel.TRACE, $"+ SendFromReqLaneTransactionToRTO : SQL={szQuery}");
            DataTable dtLaneTrans = TCT_Lib.LaneDB.List(szQuery);
            LogInfo.WriteLog("+ SendFromReqLaneTransactionToRTO : Total=" + dtLaneTrans.Rows.Count);
            for (int i = 0; i < dtLaneTrans.Rows.Count; i++)
            {
                int iTransID    = int.Parse(dtLaneTrans.Rows[i]["TransactionID"].ToString());
                bResult         = SendLaneTransObjToRTO(iTransID, true);
                if (!bResult)
                {
                    LogInfo.WriteLog(LogLevel.WARN, $"- Warning SendFromReqLaneTransactionToRTO : send TransID={iTransID} failed at {i + 1}/{dtLaneTrans.Rows.Count} -> stop");
                    break;
                }

                Thread.Sleep(100);
            }

            return bResult;
        }

        public static bool SendLaneTransObjToRTO(int iTransID, bool bResend=false)
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ SendLaneTransObjToRTO : TransID={iTransID}, resend={bResend}");
            if (iTransID == 0)
                return false;

            try
            {
                var xLaneTrans      = TCT_Obj.GenerateLaneTransMessage(iTransID);
                string szLaneTrans  = JsonConvert.SerializeObject(xLaneTrans, Formatting.Indented);
                //Console.WriteLine("+ RTO Lane Transaction = " + szLaneTrans);
                LogInfo.WriteLog($"+ RTO Send Lane Transaction : TransID={iTransID}");
                if (xLaneTrans == null)
                    LogInfo.WriteLog(LogLevel.WARN, $"- Warning SendLaneTransObjToRTO : TransID={iTransID} message is null (not found / build failed)");
                bool bResult        = TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("LANE_TRANSACTION", xLaneTrans);
                UpdateLaneTransToRTO(iTransID, bResult);
                LogInfo.WriteLog($"+ RTO Result Send Lane Transaction : TransID={iTransID}, Result={bResult}");

                return bResult;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error SendLaneTransObjToRTO : " + err.Message);
                LogInfo.WriteLog("- Error SendLaneTransObjToRTO : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, $"- SendLaneTransObjToRTO : TransID={iTransID}, stack={err.StackTrace}");
                return false;
            }
        }

        private static void UpdateLaneTransToRTO(int iTransID, bool bResult)
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ UpdateLaneTransToRTO : TransID={iTransID}, result={bResult}");
            if (iTransID == 0)
                return;

            string szQuery = "";
            if (bResult)
            {
                szQuery = "UPDATE LaneTransaction " +
                        $"SET SendStatusRTO=1, SendDateTimeRTO='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' "+
                        $"WHERE TransactionID={iTransID}";
            }
            else
            {
                szQuery = "UPDATE LaneTransaction "+
                        $"SET SendRetryRTO=SendRetryRTO+1, SendDateTimeRTO='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' " +
                        $"WHERE TransactionID={iTransID}";
            }
            //LogInfo.WriteLog("+ Query : " + szQuery);
            LogInfo.WriteLog(LogLevel.TRACE, $"+ UpdateLaneTransToRTO : SQL={szQuery}");
            LaneDB.Execute(szQuery);
        }

        async public static Task<bool> AutoResendLaneTransactionToSupAdjust()
        {
            //if (!TCT_Lib.bSupAdjust_Connected)
            //    return false;

            string szQuery = "SELECT * FROM LaneTransaction " +
                                "WHERE SendStatusSupAdjust=0 " +
                                //$"AND DATE(TransactionDateTime)='{DateTime.Today.ToString("yyyy-MM-dd")}' " +
                                $"AND DATE(TransactionDateTime)>='2021-01-01' " +   // Dummy Date, Take out when production
                                "AND Tag_SignalCode='' " +
                                "ORDER BY TransactionID";
            LogInfo.WriteLog(LogLevel.TRACE, $"+ AutoResendLaneTransactionToSupAdjust : SQL={szQuery}");
            DataTable dtLaneTrans = TCT_Lib.LaneDB.List(szQuery);
            LogInfo.WriteLog("+ AutoResendLaneTransactionToSupAdjust : Total=" + dtLaneTrans.Rows.Count);
            for (int i = 0; i < dtLaneTrans.Rows.Count; i++)
            {
                int iTransID = int.Parse(dtLaneTrans.Rows[i]["TransactionID"].ToString());
                if (iTransID > 0)
                {
                    if (!SendLaneTransObjToSupAdjust(iTransID, true))
                    {
                        LogInfo.WriteLog(LogLevel.WARN, $"- Warning AutoResendLaneTransactionToSupAdjust : send TransID={iTransID} failed at {i + 1}/{dtLaneTrans.Rows.Count} -> stop, retry next cycle");
                        return false;
                    }
                }

                //Thread.Sleep(100);
                await Task.Delay(100);
            }

            LogInfo.WriteLog(LogLevel.DEBUG, "+ AutoResendLaneTransactionToSupAdjust : done result=True");
            return true;
        }

        public static bool SendFromRequestLaneTransactionToSupAdjust(string szDate, int iJobNo, int iStart_nTrx, int iEnd_nTrx)
        {
            //if (!TCT_Lib.bRTO_Connected)
            //    return false;

            string szQuery = "SELECT * FROM LaneTransaction " +
                                $"WHERE JobNo={iJobNo} AND (nTrx>={iStart_nTrx} AND nTrx<={iEnd_nTrx}) " +
                                $"AND DATE(TransactionDateTime)>='{szDate}' " +   
                                "ORDER BY TransactionID";

            LogInfo.WriteLog(LogLevel.DEBUG, $"+ SendFromRequestLaneTransactionToSupAdjust : Date={szDate}, JobNo={iJobNo}, nTrx={iStart_nTrx}..{iEnd_nTrx}");
            LogInfo.WriteLog(LogLevel.TRACE, $"+ SendFromRequestLaneTransactionToSupAdjust : SQL={szQuery}");
            DataTable dtLaneTrans = TCT_Lib.LaneDB.List(szQuery);
            LogInfo.WriteLog("+ Found Request LaneTrans to SupAdjust = " + dtLaneTrans.Rows.Count);
            for (int i = 0; i < dtLaneTrans.Rows.Count; i++)
            {
                int iTransID = int.Parse(dtLaneTrans.Rows[i]["TransactionID"].ToString());
                if (iTransID > 0)
                {
                    if (!SendLaneTransObjToSupAdjust(iTransID, true))
                    {
                        LogInfo.WriteLog(LogLevel.WARN, $"- Warning SendFromRequestLaneTransactionToSupAdjust : send TransID={iTransID} failed at {i + 1}/{dtLaneTrans.Rows.Count} -> stop");
                        return false;
                    }
                }
                Thread.Sleep(100);
                //await Task.Delay(100);
            }
            return true;
        }


        public static bool SendLaneTransObjToSupAdjust(int iTransID, bool bResend=false)
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ SendLaneTransObjToSupAdjust : TransID={iTransID}, resend={bResend}");
            if (iTransID == 0)
                return false;

            try
            {
                var xLaneTrans      = TCT_Obj.GenerateLaneTransMessage(iTransID);
                string szLaneTrans  = JsonConvert.SerializeObject(xLaneTrans, Formatting.Indented);
                //Console.WriteLine("+ SCW Lane Transaction = " + szLaneTrans);
                LogInfo.WriteLog($"+ SupAdjust Lane Transaction : TransID={iTransID}");
                bool bResult        = TCT_Lib.webSocketLane.laneSockeSupAdjust.SendLaneMessageToSupAdjust("LANE_TRANSACTION", xLaneTrans);
                LogInfo.WriteLog(bResult ? LogLevel.DEBUG : LogLevel.WARN, $"{(bResult ? "+" : "- Warning")} SendLaneTransObjToSupAdjust : TransID={iTransID}, sent={bResult}, obj={(xLaneTrans == null ? "null" : "ok")}");
                UpdateLaneTransToSupAdjust(iTransID, bResult);

                return bResult;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error SendLaneTransObjToSupAdjust : " + err.Message);
                LogInfo.WriteLog("- Error SendLaneTransObjToSupAdjust : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, $"- SendLaneTransObjToSupAdjust : TransID={iTransID}, stack={err.StackTrace}");
                return false;
            }
        }

        private static void UpdateLaneTransToSupAdjust(int iTransID, bool bResult)
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ UpdateLaneTransToSupAdjust : TransID={iTransID}, result={bResult}");
            if (iTransID == 0)
                return;

            string szQuery = "";
            if (bResult)
            {
                szQuery = "UPDATE LaneTransaction " +
                        $"SET SendStatusSupAdjust=1, SendDateTimeSupAdjust='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' "+
                        $"WHERE TransactionID={iTransID}";
            }
            else
            {
                szQuery = "UPDATE LaneTransaction "+
                        $"SET SendRetrySupAdjust=SendRetrySupAdjust+1, SendDateTimeSupAdjust='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' " +
                        $"WHERE TransactionID={iTransID}";
            }
            //LogInfo.WriteLog("+ Query : " + szQuery);
            LogInfo.WriteLog(LogLevel.TRACE, $"+ UpdateLaneTransToSupAdjust : SQL={szQuery}");
            LaneDB.Execute(szQuery);
        }

        public static bool SendFromReqLaneTransactionToDC(string szDate, int iJobNo, int iStart_nTrx, int iEnd_nTrx)
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ SendFromReqLaneTransactionToDC : Date={szDate}, JobNo={iJobNo}, nTrx={iStart_nTrx}..{iEnd_nTrx}, DC connected={TCT_Lib.bDC_Connected}");
            if (!TCT_Lib.bDC_Connected)
                return false;
                
            string szQuery = "SELECT * FROM LaneTransaction " +
                                $"WHERE JobNo={iJobNo} AND (nTrx>={iStart_nTrx} AND nTrx<={iEnd_nTrx}) " +
                                $"AND DATE(TransactionDateTime)>='{szDate}' " +  
                                "ORDER BY TransactionID";

            LogInfo.WriteLog(LogLevel.TRACE, $"+ SendFromReqLaneTransactionToDC : SQL={szQuery}");
            DataTable dtLaneTrans = TCT_Lib.LaneDB.List(szQuery);
            LogInfo.WriteLog("+ Found SendFromReqLaneTransactionToDC : Total=" + dtLaneTrans.Rows.Count);
            bool bResult = false;
            for (int i = 0; i < dtLaneTrans.Rows.Count; i++)
            {
                int iTransID    = int.Parse(dtLaneTrans.Rows[i]["TransactionID"].ToString());
                bResult         = SendLaneTransObjToDC(iTransID, true);
                if (!bResult)
                {
                    LogInfo.WriteLog(LogLevel.WARN, $"- Warning SendFromReqLaneTransactionToDC : send TransID={iTransID} failed at {i + 1}/{dtLaneTrans.Rows.Count} -> stop");
                    break;
                }

                Thread.Sleep(100);
            }

            return bResult;
        }

        public static bool SendLaneTransObjToDC(int iTransID, bool bResend=false)
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ SendLaneTransObjToDC : TransID={iTransID}, resend={bResend}");
            if (iTransID == 0)
                return false;

            try
            {
                var xLaneTrans      = TCT_Obj.GenerateLaneTransaction(iTransID);
                string szLaneTrans  = JsonConvert.SerializeObject(xLaneTrans, Formatting.Indented);
                szLaneTrans         = szLaneTrans.Replace("Params", "params").Replace("startShiftDateTime", "boj");
                //Console.WriteLine("+ Rabbit Lane Transaction = " + szLaneTrans);
                //if (bResend)
                //    LogInfo.WriteLog($"+ DC Lane Transaction - Resend : TransID={iTransID}");
                //else
                //    LogInfo.WriteLog($"+ DC Lane Transaction  : TransID={iTransID}");

                //szLaneTrans = szLaneTrans.Replace(",", ",\n\t").Replace("},\n\t", "},\n").Replace(":{", ":{\n\t").Replace("},", "\n\t},").Replace("}}", "\n\t}\n}");

                if (!bResend)
                    LogInfo.WriteLaneTransaction(iTransID, szLaneTrans);
                var swSend          = System.Diagnostics.Stopwatch.StartNew();
                bool bResult        = TCT_Lib.mocRabbitMQ.SendLaneTransactionRabbitMQ("", szLaneTrans);
                LogInfo.WriteLog(bResult ? LogLevel.DEBUG : LogLevel.WARN, $"{(bResult ? "+" : "- Warning")} SendLaneTransObjToDC : TransID={iTransID}, sent={bResult} ({swSend.ElapsedMilliseconds} ms), obj={(xLaneTrans == null ? "null" : "ok")}");
                UpdateLaneTransToDC(iTransID, bResult);
                    
                return bResult;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error SendLaneTransObjToDC : " + err.Message);
                LogInfo.WriteLog("- Error SendLaneTransObjToDC : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, $"- SendLaneTransObjToDC : TransID={iTransID}, mocRabbitMQ={(TCT_Lib.mocRabbitMQ == null ? "null" : "ok")}, stack={err.StackTrace}");
                return false;
            }
        }

        private static void UpdateLaneTransToDC(int iTransID, bool bResult)
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ UpdateLaneTransToDC : TransID={iTransID}, result={bResult}");
            if (iTransID == 0)
                return;

            string szQuery = "";
            if (bResult)
            {
                szQuery = "UPDATE LaneTransaction " +
                        $"SET SendStatusDC=1, SendDateTimeDC='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' "+
                        $"WHERE TransactionID={iTransID}";
            }
            else
            {
                szQuery = "UPDATE LaneTransaction "+
                        $"SET SendStatusDC=0, SendRetryDC=SendRetryDC+1, SendDateTimeDC='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' " +
                        $"WHERE TransactionID={iTransID}";
            }
            //LogInfo.WriteLog("+ Query : " + szQuery);
            LogInfo.WriteLog(LogLevel.TRACE, $"+ UpdateLaneTransToDC : SQL={szQuery}");
            LaneDB.Execute(szQuery);
        }

        //-----------------------------------------
        async public static Task<bool> AutoResendBeginJobToDC(int iStaffShiftID)
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ AutoResendBeginJobToDC : exclude StaffShiftID={iStaffShiftID}, DC connected={TCT_Lib.bDC_Connected}");
            if (!TCT_Lib.bDC_Connected)
                return false;

            bool bResult    = true;
            string szQuery  = "SELECT * FROM StaffShift " +
                                "WHERE (BojSendStatusDC=0 OR BojSendStatusDC IS NULL) " +
                                //$"AND DATE(TransactionDateTime)='{DateTime.Today.ToString("yyyy-MM-dd")}' " +
                                $"AND DATE(StaffStartShiftDateTime)>='2021-01-01' " +   // Dummy Date, Take out when production
                                $"AND StaffShiftID <> {iStaffShiftID} " +
                                "ORDER BY StaffShiftID";
            LogInfo.WriteLog(LogLevel.TRACE, $"+ AutoResendBeginJobToDC : SQL={szQuery}");
            DataTable dtStaffShift = TCT_Lib.LaneDB.List(szQuery);
            LogInfo.WriteLog("+ AutoResendBeginJobToDC : Total=" + dtStaffShift.Rows.Count);
            for (int i = 0; i < dtStaffShift.Rows.Count; i++)
            {
                int iShiftID   = int.Parse(dtStaffShift.Rows[i]["StaffShiftID"].ToString());
                bResult             = SendLaneBeginJobObjToDC(iShiftID, true);
                if (!bResult)
                {
                    LogInfo.WriteLog(LogLevel.WARN, $"- Warning AutoResendBeginJobToDC : StaffShiftID={iShiftID} failed at {i + 1}/{dtStaffShift.Rows.Count} -> stop, retry next cycle");
                    break;
                }
                //Thread.Sleep(100);
                await Task.Delay(100);
            }

            LogInfo.WriteLog(LogLevel.DEBUG, $"+ AutoResendBeginJobToDC : done result={bResult}");
            return bResult;
        }

        public static bool SendLaneBeginJobObjToDC(int iStaffShiftID, bool bResend=false)
        {
            try
            {
                var xLaneBOJ        = TCT_Obj.GenerateLaneBeginOfJob(iStaffShiftID);
                string szLaneBOJ    = JsonConvert.SerializeObject(xLaneBOJ, Formatting.Indented);
                szLaneBOJ           = szLaneBOJ.Replace("Params", "params");

                szLaneBOJ           = szLaneBOJ.Replace(",", ",\n\t").Replace("},\n\t", "},\n").Replace(":{", ":{\n\t").Replace("},", "\n\t},").Replace("}}", "\n\t}\n}");
                szLaneBOJ           = szLaneBOJ.Replace(@"\n", "").Replace(@"\t", "");
                //Console.WriteLine("+ Rabbit Lane SendLaneBeginJobObjToDC = " + szLaneBOJ);
                if (bResend)
                    LogInfo.WriteLog($"+ Rabbit Lane SendLaneBeginJobObjToDC - Resend : StaffShiftID={iStaffShiftID}");
                else
                    LogInfo.WriteLog($"+ Rabbit Lane SendLaneBeginJobObjToDC : StaffShiftID={iStaffShiftID}");
                LogInfo.WriteLaneBeginJob(iStaffShiftID, szLaneBOJ);
                LogInfo.WriteLog(LogLevel.TRACE, $"+ SendLaneBeginJobObjToDC : StaffShiftID={iStaffShiftID}, json={szLaneBOJ}");
                bool bResult = TCT_Lib.mocRabbitMQ.SendLaneTransactionRabbitMQ("", szLaneBOJ);
                LogInfo.WriteLog(bResult ? LogLevel.DEBUG : LogLevel.WARN, $"{(bResult ? "+" : "- Warning")} SendLaneBeginJobObjToDC : StaffShiftID={iStaffShiftID}, resend={bResend}, sent={bResult}, obj={(xLaneBOJ == null ? "null" : "ok")} (returns true anyway)");
                UpdateLaneBeginJobToDC(iStaffShiftID, bResult);

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error SendLaneBeginJobObjToDC : " + err.Message);
                LogInfo.WriteLog("- Error SendLaneBeginJobObjToDC : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, $"- SendLaneBeginJobObjToDC : StaffShiftID={iStaffShiftID}, stack={err.StackTrace}");
                return false;
            }
        }

        private static void UpdateLaneBeginJobToDC(int iStaffShiftID, bool bResult)
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ UpdateLaneBeginJobToDC : StaffShiftID={iStaffShiftID}, result={bResult}");
            string szQuery = "";
            if (bResult)
            {
                szQuery = "UPDATE StaffShift " +
                        $"SET BojSendStatusDC=1, BojSendDateTimeDC='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' "+
                        $"WHERE StaffShiftID={iStaffShiftID}";
            }
            else
            {
                szQuery = "UPDATE StaffShift " +
                        $"SET BojSendRetryDC=BojSendRetryDC+1, BojSendDateTimeDC='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' " +
                        $"WHERE StaffShiftID={iStaffShiftID}";
            }
            //LogInfo.WriteLog("+ Query : " + szQuery);
            LogInfo.WriteLog(LogLevel.TRACE, $"+ UpdateLaneBeginJobToDC : SQL={szQuery}");
            LaneDB.Execute(szQuery);
        }

        async public static Task<bool> AutoResendEndJobToDC(int iStaffShiftID)
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ AutoResendEndJobToDC : exclude StaffShiftID={iStaffShiftID}, DC connected={TCT_Lib.bDC_Connected}");
            if (!TCT_Lib.bDC_Connected)
                return false;
                
            bool bResult    = true;
            string szQuery  = "SELECT * FROM StaffShift " +
                                "WHERE (EojSendStatusDC=0 OR EojSendStatusDC IS NULL) " +
                                //$"AND DATE(TransactionDateTime)='{DateTime.Today.ToString("yyyy-MM-dd")}' " +
                                $"AND DATE(StaffStartShiftDateTime)>='2021-01-01' " +   // Dummy Date, Take out when production
                                $"AND StaffShiftID <> {iStaffShiftID} "+
                                "ORDER BY StaffShiftID";
            LogInfo.WriteLog(LogLevel.TRACE, $"+ AutoResendEndJobToDC : SQL={szQuery}");
            DataTable dtStaffShift = TCT_Lib.LaneDB.List(szQuery);
            LogInfo.WriteLog("+ AutoResendEndJobToDC : Total=" + dtStaffShift.Rows.Count);
            for (int i = 0; i < dtStaffShift.Rows.Count; i++)
            {
                int iShiftID    = int.Parse(dtStaffShift.Rows[i]["StaffShiftID"].ToString());
                bResult         = SendEndJobObjToDC(iShiftID, true);
                if (!bResult)
                {
                    LogInfo.WriteLog(LogLevel.WARN, $"- Warning AutoResendEndJobToDC : StaffShiftID={iShiftID} failed at {i + 1}/{dtStaffShift.Rows.Count} -> stop, retry next cycle");
                    break;
                }

                //Thread.Sleep(200);
                await Task.Delay(100);
            }

            LogInfo.WriteLog(LogLevel.DEBUG, $"+ AutoResendEndJobToDC : done result={bResult}");
            return bResult;
        }

        public static bool SendEndJobObjToDC(int iStaffShiftID, bool bResend=false)
        {
            try
            {
                var xLaneEOJ        = TCT_Obj.GenerateLaneEndOfJob(iStaffShiftID);
                //-----------------------------------------------------------------------------------
                if (xLaneEOJ == null)
                {
                    LogInfo.WriteLog("- Not create EndOfJob file (Not found EOJ DateTime");
                    return false;
                }
                //-----------------------------------------------------------------------------------
                string szLaneEOJ    = JsonConvert.SerializeObject(xLaneEOJ, Formatting.Indented);
                szLaneEOJ           = szLaneEOJ.Replace("Params", "params");
                szLaneEOJ           = szLaneEOJ.Replace(",", ",\n\t").Replace("},\n\t", "},\n").Replace(":{", ":{\n\t").Replace("},", "\n\t},").Replace("}}", "\n\t}\n}");
                szLaneEOJ           = szLaneEOJ.Replace(@"\n", "").Replace(@"\t", "");
                //Console.WriteLine("+ Rabbit Lane SendLaneEndJobObjToDC = " + szLaneEOJ);
                if (bResend)
                    LogInfo.WriteLog($"+ Rabbit Lane SendEndJobObjToDC - Resend : StaffShiftID={iStaffShiftID}");
                else
                    LogInfo.WriteLog($"+ Rabbit Lane SendEndJobObjToDC : StaffShiftID={iStaffShiftID}");
                LogInfo.WriteLaneEndJob(iStaffShiftID, szLaneEOJ);
                LogInfo.WriteLog(LogLevel.TRACE, $"+ SendEndJobObjToDC : StaffShiftID={iStaffShiftID}, json={szLaneEOJ}");
                bool bResult        = TCT_Lib.mocRabbitMQ.SendLaneTransactionRabbitMQ("", szLaneEOJ);
                LogInfo.WriteLog(bResult ? LogLevel.DEBUG : LogLevel.WARN, $"{(bResult ? "+" : "- Warning")} SendEndJobObjToDC : StaffShiftID={iStaffShiftID}, resend={bResend}, sent={bResult} (returns true anyway)");
                UpdateLaneEndJobToDC(iStaffShiftID, bResult);

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error SendEndJobObjToDC : " + err.Message);
                LogInfo.WriteLog("- Error SendEndJobObjToDC : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, $"- SendEndJobObjToDC : StaffShiftID={iStaffShiftID}, stack={err.StackTrace}");
                return false;
            }
        }

        private static void UpdateLaneEndJobToDC(int iStaffShiftID, bool bResult)
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ UpdateLaneEndJobToDC : StaffShiftID={iStaffShiftID}, result={bResult}");
            string szQuery = "";
            if (bResult)
            {
                szQuery = "UPDATE StaffShift " +
                        $"SET EojSendStatusDC=1, EojSendDateTimeDC='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' "+
                        $"WHERE StaffShiftID={iStaffShiftID}";
            }
            else
            {
                szQuery = "UPDATE StaffShift " +
                        $"SET EojSendRetryDC=EojSendRetryDC+1, EojSendDateTimeDC='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' " +
                        $"WHERE StaffShiftID={iStaffShiftID}";
            }
            //LogInfo.WriteLog("+ Query : " + szQuery);
            LogInfo.WriteLog(LogLevel.TRACE, $"+ UpdateLaneEndJobToDC : SQL={szQuery}");
            LaneDB.Execute(szQuery);
        }

        //--------------------------------------------------------------
        public static bool SendLaneTransObjToMTS(int iTransID)
        {
            LogInfo.WriteLogMTS(LogLevel.DEBUG, $"+ SendLaneTransObjToMTS : TransID={iTransID}");
            if (iTransID == 0)
                return false;

            try
            {
                var xMtsTransObj        = TCT_Obj.GenerateTransactionMTS(iTransID);
                string szMtsTransMsg    = JsonConvert.SerializeObject(xMtsTransObj, Formatting.Indented);
                LogInfo.WriteLogMTS("+ MTS-Trans : " + szMtsTransMsg);

                bool bResult = TCT_Lib.mtsRabbitMQ.SendMtsMsgToRabbitMQ("MTS-Trans", szMtsTransMsg);
                LogInfo.WriteLogMTS(bResult ? LogLevel.DEBUG : LogLevel.WARN, $"{(bResult ? "+" : "- Warning")} SendLaneTransObjToMTS : TransID={iTransID}, sent={bResult} (returns true anyway)");
                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error SendLaneTransObjToMTS : " + err.Message);
                LogInfo.WriteLog("- Error SendLaneTransObjToMTS : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, $"- SendLaneTransObjToMTS : TransID={iTransID}, stack={err.StackTrace}");
                return false;
            }
        }

        //-------------------------------------
        async public static Task<bool> AutoResendBeginJobToRTO()
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ AutoResendBeginJobToRTO : RTO connected={TCT_Lib.bRTO_Connected}");
            if (!TCT_Lib.bRTO_Connected)
                return false;

            bool bResult = true;
            string szQuery = "SELECT * FROM StaffShift " +
                                "WHERE (BojSendStatusRTO=0 OR BojSendStatusRTO IS NULL) " +
                                //$"AND DATE(TransactionDateTime)='{DateTime.Today.ToString("yyyy-MM-dd")}' " +
                                $"AND DATE(StaffStartShiftDateTime)>='2021-01-01' " +   // Dummy Date, Take out when production
                                "ORDER BY StaffShiftID";
            LogInfo.WriteLog(LogLevel.TRACE, $"+ AutoResendBeginJobToRTO : SQL={szQuery}");
            DataTable dtStaffShift = TCT_Lib.LaneDB.List(szQuery);
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ AutoResendBeginJobToRTO : pending={dtStaffShift?.Rows.Count ?? -1}");
            //LogInfo.WriteLaneEvent("+ Found AutoResendBeginJobToRTO : Total=" + dtStaffShift.Rows.Count);
            for (int i = 0; i < dtStaffShift.Rows.Count; i++)
            {
                int iStaffShiftID   = int.Parse(dtStaffShift.Rows[i]["StaffShiftID"].ToString());
                bResult             = SendBeginJobObjToRTO(iStaffShiftID, true);
                if (!bResult)
                {
                    LogInfo.WriteLog(LogLevel.WARN, $"- Warning AutoResendBeginJobToRTO : StaffShiftID={iStaffShiftID} failed at {i + 1}/{dtStaffShift.Rows.Count} -> stop, retry next cycle");
                    break;
                }

                //Thread.Sleep(100);
                await Task.Delay(100);
            }

            LogInfo.WriteLog(LogLevel.DEBUG, $"+ AutoResendBeginJobToRTO : done result={bResult}");
            return bResult;
        }

        public static bool SendBeginJobObjToRTO(int iStaffShiftID, bool bResend=false)
        {
            try
            {
                var xLaneBOJ        = TCT_Obj.GenerateLaneBeginOfJob(iStaffShiftID);
                string szLaneBOJ    = JsonConvert.SerializeObject(xLaneBOJ, Formatting.Indented);
                szLaneBOJ           = szLaneBOJ.Replace("Params", "params");
                //Console.WriteLine("+ RTO Lane Begin of Job = " + szLaneBOJ);
                if (bResend)
                    LogInfo.WriteLog($"+ RTO Lane Begin of Job - Resend : StaffShiftID={iStaffShiftID}");
                else
                    LogInfo.WriteLog($"+ RTO Lane Begin of Job : StaffShiftID={iStaffShiftID}");
                bool bResult        = TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("LANE_BEGINJOB", xLaneBOJ);
                LogInfo.WriteLog(LogLevel.TRACE, $"+ SendBeginJobObjToRTO : StaffShiftID={iStaffShiftID}, json={szLaneBOJ}");
                LogInfo.WriteLog(bResult ? LogLevel.DEBUG : LogLevel.WARN, $"{(bResult ? "+" : "- Warning")} SendBeginJobObjToRTO : StaffShiftID={iStaffShiftID}, resend={bResend}, sent={bResult}, obj={(xLaneBOJ == null ? "null" : "ok")} (returns true anyway)");
                UpdateLaneBeginJobToRTO(iStaffShiftID, bResult);

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error SendBeginJobObjToRTO : " + err.Message);
                LogInfo.WriteLog("- Error SendBeginJobObjToRTO : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, $"- SendBeginJobObjToRTO : StaffShiftID={iStaffShiftID}, stack={err.StackTrace}");
                return false;
            }
        }

        private static void UpdateLaneBeginJobToRTO(int iStaffShiftID, bool bResult)
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ UpdateLaneBeginJobToRTO : StaffShiftID={iStaffShiftID}, result={bResult}");
            string szQuery = "";
            if (bResult)
            {
                szQuery = "UPDATE StaffShift " +
                        $"SET BojSendStatusRTO=1, BojSendDateTimeRTO='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' "+
                        $"WHERE StaffShiftID={iStaffShiftID}";
            }
            else
            {
                szQuery = "UPDATE StaffShift " +
                        $"SET BojSendRetryRTO=BojSendRetryRTO+1, BojSendDateTimeRTO='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' " +
                        $"WHERE StaffShiftID={iStaffShiftID}";
            }
            //LogInfo.WriteLog("+ Query : " + szQuery);
            LogInfo.WriteLog(LogLevel.TRACE, $"+ UpdateLaneBeginJobToRTO : SQL={szQuery}");
            LaneDB.Execute(szQuery);
        }

        async public static Task<bool> AutoResendEndJobToRTO()
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ AutoResendEndJobToRTO : RTO connected={TCT_Lib.bRTO_Connected}");
            if (!TCT_Lib.bRTO_Connected)
                return false;

            bool bResult = true;
            string szQuery = "SELECT * FROM StaffShift " +
                                "WHERE (EojSendStatusRTO=0 OR EojSendStatusRTO IS NULL) " +
                                //$"AND DATE(TransactionDateTime)='{DateTime.Today.ToString("yyyy-MM-dd")}' " +
                                $"AND DATE(StaffStartShiftDateTime)>='2021-01-01' " +   // Dummy Date, Take out when production
                                "ORDER BY StaffShiftID";
            LogInfo.WriteLog(LogLevel.TRACE, $"+ AutoResendEndJobToRTO : SQL={szQuery}");
            DataTable dtStaffShift = TCT_Lib.LaneDB.List(szQuery);
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ AutoResendEndJobToRTO : pending={dtStaffShift?.Rows.Count ?? -1}");
            //LogInfo.WriteLaneEvent("+ Found AutoResendEndJobToRTO : Total=" + dtStaffShift.Rows.Count);
            for (int i = 0; i < dtStaffShift.Rows.Count; i++)
            {
                int iStaffShiftID   = int.Parse(dtStaffShift.Rows[i]["StaffShiftID"].ToString());
                bResult             = SendEndJobObjToRTO(iStaffShiftID, true);
                if (!bResult)
                {
                    LogInfo.WriteLog(LogLevel.WARN, $"- Warning AutoResendEndJobToRTO : StaffShiftID={iStaffShiftID} failed at {i + 1}/{dtStaffShift.Rows.Count} -> stop, retry next cycle");
                    break;
                }

                //Thread.Sleep(100);
                await Task.Delay(100);
            }

            LogInfo.WriteLog(LogLevel.DEBUG, $"+ AutoResendEndJobToRTO : done result={bResult}");
            return bResult;
        }

        public static bool SendEndJobObjToRTO(int iStaffShiftID, bool bResend=false)
        {
            try
            {
                var xLaneEOJ        = TCT_Obj.GenerateLaneEndOfJob(iStaffShiftID);
                string szLaneEOJ    = JsonConvert.SerializeObject(xLaneEOJ, Formatting.Indented);
                szLaneEOJ           = szLaneEOJ.Replace("Params", "params");
                //Console.WriteLine("+ RTO Lane End of Job = " + szLaneEOJ);
                if (bResend)
                    LogInfo.WriteLog($"+ RTO Lane End of Job - Resend : StaffShiftID={iStaffShiftID}");
                else
                    LogInfo.WriteLog($"+ RTO Lane End of Job : StaffShiftID={iStaffShiftID}");
                bool bResult        = TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("LANE_ENDJOB", xLaneEOJ);
                LogInfo.WriteLog(LogLevel.TRACE, $"+ SendEndJobObjToRTO : StaffShiftID={iStaffShiftID}, json={szLaneEOJ}");
                LogInfo.WriteLog(bResult ? LogLevel.DEBUG : LogLevel.WARN, $"{(bResult ? "+" : "- Warning")} SendEndJobObjToRTO : StaffShiftID={iStaffShiftID}, resend={bResend}, sent={bResult}, obj={(xLaneEOJ == null ? "null" : "ok")} (returns true anyway)");
                UpdateLaneEndJobToRTO(iStaffShiftID, bResult);

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error SendEndJobObjToRTO : " + err.Message);
                LogInfo.WriteLog("- Error SendEndJobObjToRTO : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, $"- SendEndJobObjToRTO : StaffShiftID={iStaffShiftID}, stack={err.StackTrace}");
                return false;
            }
        }

        private static void UpdateLaneEndJobToRTO(int iStaffShiftID, bool bResult)
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ UpdateLaneEndJobToRTO : StaffShiftID={iStaffShiftID}, result={bResult}");
            string szQuery = "";
            if (bResult)
            {
                szQuery = "UPDATE StaffShift " +
                        $"SET EojSendStatusRTO=1, EojSendDateTimeRTO='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' "+
                        $"WHERE StaffShiftID={iStaffShiftID}";
            }
            else
            {
                szQuery = "UPDATE StaffShift " +
                        $"SET EojSendRetryRTO=BojSendRetryRTO+1, EojSendDateTimeRTO='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' " +
                        $"WHERE StaffShiftID={iStaffShiftID}";
            }
            LogInfo.WriteLog("+ Query : " + szQuery);
            LaneDB.Execute(szQuery);
        }

        //-------------------------------
        async public static Task<bool> AutoResendBeginJobToSupAdjust()
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ AutoResendBeginJobToSupAdjust : SupAdjust connected={TCT_Lib.bSupAdjust_Connected}");
            if (!TCT_Lib.bSupAdjust_Connected)
                return false;

            bool bResult    = true;
            string szQuery  = "SELECT * FROM StaffShift " +
                                "WHERE (BojSendStatusSupAdjust=0 OR BojSendStatusSupAdjust IS NULL) " +
                                //$"AND DATE(TransactionDateTime)='{DateTime.Today.ToString("yyyy-MM-dd")}' " +
                                $"AND DATE(StaffStartShiftDateTime)>='2021-01-01' " +   // Dummy Date, Take out when production
                                "ORDER BY StaffShiftID";
            LogInfo.WriteLog(LogLevel.TRACE, $"+ AutoResendBeginJobToSupAdjust : SQL={szQuery}");
            DataTable dtStaffShift = TCT_Lib.LaneDB.List(szQuery);
            LogInfo.WriteLog("+ Found AutoResendBeginJobToSupAdjust : Total=" + dtStaffShift.Rows.Count);
            for (int i = 0; i < dtStaffShift.Rows.Count; i++)
            {
                int iStaffShiftID   = int.Parse(dtStaffShift.Rows[i]["StaffShiftID"].ToString());
                bResult             = SendBeginJobObjToSupAdjust(iStaffShiftID, true);
                if (!bResult)
                {
                    LogInfo.WriteLog(LogLevel.WARN, $"- Warning AutoResendBeginJobToSupAdjust : StaffShiftID={iStaffShiftID} failed at {i + 1}/{dtStaffShift.Rows.Count} -> stop, retry next cycle");
                    break;
                }

                //Thread.Sleep(100);
                await Task.Delay(100);
            }

            LogInfo.WriteLog(LogLevel.DEBUG, $"+ AutoResendBeginJobToSupAdjust : done result={bResult}");
            return bResult;
        }

        public static bool SendBeginJobObjToSupAdjust(int iStaffShiftID, bool bResend=false)
        {
            try
            {
                var xLaneBOJ        = TCT_Obj.GenerateLaneBeginOfJob(iStaffShiftID);
                string szLaneBOJ    = JsonConvert.SerializeObject(xLaneBOJ, Formatting.Indented);
                szLaneBOJ           = szLaneBOJ.Replace("Params", "params");
                //Console.WriteLine("+ RTO Lane Begin of Job = " + szLaneBOJ);
                if (bResend)
                    LogInfo.WriteLog($"+ SupAdjust Lane Begin of Job -Resend : StaffShiftID={iStaffShiftID}");
                else
                    LogInfo.WriteLog($"+ SupAdjust Lane Begin of Job : StaffShiftID={iStaffShiftID}");
                bool bResult        = TCT_Lib.webSocketLane.laneSockeSupAdjust.SendLaneMessageToSupAdjust("LANE_BEGINJOB", xLaneBOJ);
                LogInfo.WriteLog(LogLevel.TRACE, $"+ SendBeginJobObjToSupAdjust : StaffShiftID={iStaffShiftID}, json={szLaneBOJ}");
                LogInfo.WriteLog(bResult ? LogLevel.DEBUG : LogLevel.WARN, $"{(bResult ? "+" : "- Warning")} SendBeginJobObjToSupAdjust : StaffShiftID={iStaffShiftID}, resend={bResend}, sent={bResult}, obj={(xLaneBOJ == null ? "null" : "ok")} (returns true anyway)");
                UpdateLaneBeginJobToSupAdjust(iStaffShiftID, bResult);

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error SendBeginJobObjToSupAdjust : " + err.Message);
                LogInfo.WriteLog("- Error SendBeginJobObjToSupAdjust : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, $"- SendBeginJobObjToSupAdjust : StaffShiftID={iStaffShiftID}, stack={err.StackTrace}");
                return false;
            }
        }

        private static void UpdateLaneBeginJobToSupAdjust(int iStaffShiftID, bool bResult)
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ UpdateLaneBeginJobToSupAdjust : StaffShiftID={iStaffShiftID}, result={bResult}");
            string szQuery = "";
            if (bResult)
            {
                szQuery = "UPDATE StaffShift " +
                        $"SET BojSendStatusSupAdjust=1, BojSendDateTimeSupAdjust='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' "+
                        $"WHERE StaffShiftID={iStaffShiftID}";
            }
            else
            {
                szQuery = "UPDATE StaffShift " +
                        $"SET BojSendRetrySupAdjust=BojSendRetrySupAdjust+1, BojSendDateTimeSupAdjust='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' " +
                        $"WHERE StaffShiftID={iStaffShiftID}";
            }
            //LogInfo.WriteLog("+ Query : " + szQuery);
            LogInfo.WriteLog(LogLevel.TRACE, $"+ UpdateLaneBeginJobToSupAdjust : SQL={szQuery}");
            LaneDB.Execute(szQuery);
        }

        async public static Task<bool> AutoResendEndJobToSupAdjust()
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ AutoResendEndJobToSupAdjust : SupAdjust connected={TCT_Lib.bSupAdjust_Connected}");
            if (!TCT_Lib.bSupAdjust_Connected)
                return false;

            bool bResult = true;
            string szQuery = "SELECT * FROM StaffShift " +
                                "WHERE (EojSendStatusSupAdjust=0 OR EojSendStatusSupAdjust IS NULL) " +
                                //$"AND DATE(TransactionDateTime)='{DateTime.Today.ToString("yyyy-MM-dd")}' " +
                                $"AND DATE(StaffStartShiftDateTime)>='2021-01-01' " +   // Dummy Date, Take out when production
                                "ORDER BY StaffShiftID";
            LogInfo.WriteLog(LogLevel.TRACE, $"+ AutoResendEndJobToSupAdjust : SQL={szQuery}");
            DataTable dtStaffShift = TCT_Lib.LaneDB.List(szQuery);
            LogInfo.WriteLog("+ Found AutoResendEndJobToSupAdjust : Total=" + dtStaffShift.Rows.Count);
            for (int i = 0; i < dtStaffShift.Rows.Count; i++)
            {
                int iStaffShiftID   = int.Parse(dtStaffShift.Rows[i]["StaffShiftID"].ToString());
                bResult             = SendEndJobObjToSupAdjust(iStaffShiftID, true);
                if (!bResult)
                {
                    LogInfo.WriteLog(LogLevel.WARN, $"- Warning AutoResendEndJobToSupAdjust : StaffShiftID={iStaffShiftID} failed at {i + 1}/{dtStaffShift.Rows.Count} -> stop, retry next cycle");
                    break;
                }

                //Thread.Sleep(100);
                await Task.Delay(100);
            }

            LogInfo.WriteLog(LogLevel.DEBUG, $"+ AutoResendEndJobToSupAdjust : done result={bResult}");
            return bResult;
        }

        public static bool SendEndJobObjToSupAdjust(int iStaffShiftID, bool bResend=false)
        {
            try
            {
                var xLaneEOJ        = TCT_Obj.GenerateLaneEndOfJob(iStaffShiftID);
                string szLaneEOJ    = JsonConvert.SerializeObject(xLaneEOJ, Formatting.Indented);
                szLaneEOJ           = szLaneEOJ.Replace("Params", "params");
                //Console.WriteLine("+ RTO Lane End of Job = " + szLaneEOJ);
                if (bResend)
                    LogInfo.WriteLog($"+ SupAdjust Lane End of Job - Resend : StaffShiftID={iStaffShiftID}");
                else
                    LogInfo.WriteLog($"+ SupAdjust Lane End of Job : StaffShiftID={iStaffShiftID}");
                bool bResult        = TCT_Lib.webSocketLane.laneSockeSupAdjust.SendLaneMessageToSupAdjust("LANE_ENDJOB", xLaneEOJ);
                LogInfo.WriteLog(LogLevel.TRACE, $"+ SendEndJobObjToSupAdjust : StaffShiftID={iStaffShiftID}, json={szLaneEOJ}");
                LogInfo.WriteLog(bResult ? LogLevel.DEBUG : LogLevel.WARN, $"{(bResult ? "+" : "- Warning")} SendEndJobObjToSupAdjust : StaffShiftID={iStaffShiftID}, resend={bResend}, sent={bResult}, obj={(xLaneEOJ == null ? "null" : "ok")} (returns true anyway)");
                UpdateLaneEndJobToSupAdjust(iStaffShiftID, bResult);

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error SendEndJobObjToSupAdjust : " + err.Message);
                LogInfo.WriteLog("- Error SendEndJobObjToSupAdjust : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, $"- SendEndJobObjToSupAdjust : StaffShiftID={iStaffShiftID}, stack={err.StackTrace}");
                return false;
            }
        }

        private static void UpdateLaneEndJobToSupAdjust(int iStaffShiftID, bool bResult)
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ UpdateLaneEndJobToSupAdjust : StaffShiftID={iStaffShiftID}, result={bResult}");
            string szQuery = "";
            if (bResult)
            {
                szQuery = "UPDATE StaffShift " +
                        $"SET EojSendStatusSupAdjust=1, EojSendDateTimeSupAdjust='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' "+
                        $"WHERE StaffShiftID={iStaffShiftID}";
            }
            else
            {
                szQuery = "UPDATE StaffShift " +
                        $"SET EojSendRetrySupAdjust=EojSendRetrySupAdjust+1, EojSendDateTimeSupAdjust='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' " +
                        $"WHERE StaffShiftID={iStaffShiftID}";
            }
            LogInfo.WriteLog("+ Query : " + szQuery);
            LaneDB.Execute(szQuery);
        }



        //-------------------------------------------------------------------------------------------
        public static bool SendOtlStatusObjToDC(bool bOtlStatus, int iStaffShiftID)
        {
            try
            {
                var xOtlStatus      = TCT_Obj.GenerateOtlStatus(bOtlStatus, iStaffShiftID);
                string szOtlStatus  = JsonConvert.SerializeObject(xOtlStatus, Formatting.Indented);
                szOtlStatus         = szOtlStatus.Replace("Params", "params");

                szOtlStatus         = szOtlStatus.Replace(",", ",\n\t").Replace("},\n\t", "},\n").Replace(":{", ":{\n\t").Replace("},", "\n\t},").Replace("}}", "\n\t}\n}");
                szOtlStatus         = szOtlStatus.Replace(@"\n", "").Replace(@"\t", "");
                //Console.WriteLine("+ Rabbit Lane SendLaneBeginJobObjToDC = " + szLaneBOJ);
                LogInfo.WriteLog($"+ Rabbit SendOtlStatusObjToDC : OtlStatus={bOtlStatus}, StaffShiftID={iStaffShiftID}");
                LogInfo.WriteLaneOtlStatus(iStaffShiftID, szOtlStatus);
                LogInfo.WriteLog(LogLevel.TRACE, $"+ SendOtlStatusObjToDC : json={szOtlStatus}");
                bool bResult = TCT_Lib.mocRabbitMQ.SendLaneTransactionRabbitMQ("", szOtlStatus);
                LogInfo.WriteLog(bResult ? LogLevel.DEBUG : LogLevel.WARN, $"{(bResult ? "+" : "- Warning")} SendOtlStatusObjToDC : OtlStatus={bOtlStatus}, StaffShiftID={iStaffShiftID}, sent={bResult}, obj={(xOtlStatus == null ? "null" : "ok")} (no retry)");

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error SendOtlStatusObjToDC : " + err.Message);
                LogInfo.WriteLog("- Error SendOtlStatusObjToDC : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, $"- SendOtlStatusObjToDC : StaffShiftID={iStaffShiftID}, stack={err.StackTrace}");
                return false;
            }
        }

        //-------------------------------------------
        async public static Task<bool> AutoResendLaneTransactionToDC()
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ AutoResendLaneTransactionToDC : DC connected={TCT_Lib.bDC_Connected}");
            if (!TCT_Lib.bDC_Connected)
                return false;

            //LogInfo.WriteLog("+ Start AutoResendLaneTransactionToDC");
            if (!TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szDC_RabbitMQ_ServerIPAddress, TCT_Lib.iDC_RabbitMQ_ServerPort, TCT_Lib.iDC_RabbitMQ_TimeoutSec * 1000))
            //if (!await TCT_Lib.laneController.IsPortReachable(TCT_Lib.szDC_RabbitMQ_ServerIPAddress, TCT_Lib.iDC_RabbitMQ_ServerPort))
            {
                Console.WriteLine($"- Fail TestNetworkConnection to RabbitMQ => Connect DC Rabbit MQ : IP = {TCT_Lib.szDC_RabbitMQ_ServerIPAddress}, PORT = {TCT_Lib.iDC_RabbitMQ_ServerPort}");
                LogInfo.WriteLog($"- Fail TestNetworkConnection to RabbitMQ => Connect DC Rabbit MQ : IP = {TCT_Lib.szDC_RabbitMQ_ServerIPAddress}, PORT = {TCT_Lib.iDC_RabbitMQ_ServerPort}");
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ AutoResendLaneTransactionToDC : bDC_NetworkStatus {TCT_Lib.bDC_NetworkStatus} -> False, timeout={TCT_Lib.iDC_RabbitMQ_TimeoutSec} sec");
                TCT_Lib.bDC_NetworkStatus = false;
                return false;
            }

            bool bResult = true;
            string szQuery = "SELECT * FROM LaneTransaction " +
                                "WHERE SendStatusDC=0 " +
                                $"AND DATE(TransactionDateTime)>='2021-01-01' " +   // Dummy Date, Take out when production
                                "ORDER BY TransactionID";
            LogInfo.WriteLog(LogLevel.TRACE, $"+ AutoResendLaneTransactionToDC : SQL={szQuery}");
            DataTable dtLaneTrans = TCT_Lib.LaneDB.List(szQuery);
            LogInfo.WriteLog("+ AutoResendLaneTransactionToDC : Total=" + dtLaneTrans.Rows.Count);
            for (int i = 0; i < dtLaneTrans.Rows.Count; i++)
            {
                int iTransID    = int.Parse(dtLaneTrans.Rows[i]["TransactionID"].ToString());
                bResult         = SendLaneTransObjToDC(iTransID, true);
                if (!bResult)
                {
                    LogInfo.WriteLog(LogLevel.WARN, $"- Warning AutoResendLaneTransactionToDC : send TransID={iTransID} failed at {i + 1}/{dtLaneTrans.Rows.Count} -> stop, retry next cycle");
                    break;
                }

                //Thread.Sleep(100);
                await Task.Delay(200);
            }

            LogInfo.WriteLog(LogLevel.DEBUG, $"+ AutoResendLaneTransactionToDC : done result={bResult}");
            return bResult;
        }

        async public static Task<bool> AutoResendLaneEventToDC()
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ AutoResendLaneEventToDC : DC connected={TCT_Lib.bDC_Connected}");
            if (!TCT_Lib.bDC_Connected)
                return false;

            //LogInfo.WriteLog("+ Start AutoResendLaneEventToDC");
            if (!TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szDC_RabbitMQ_ServerIPAddress, TCT_Lib.iDC_RabbitMQ_ServerPort, TCT_Lib.iDC_RabbitMQ_TimeoutSec * 1000))
            //if (!await TCT_Lib.laneController.IsPortReachable(TCT_Lib.szDC_RabbitMQ_ServerIPAddress, TCT_Lib.iDC_RabbitMQ_ServerPort))
            {
                Console.WriteLine($"- Fail TestNetworkConnection to RabbitMQ => Connect DC Rabbit MQ : IP = {TCT_Lib.szDC_RabbitMQ_ServerIPAddress}, PORT = {TCT_Lib.iDC_RabbitMQ_ServerPort}");
                LogInfo.WriteLog($"- Fail TestNetworkConnection to RabbitMQ => Connect DC Rabbit MQ : IP = {TCT_Lib.szDC_RabbitMQ_ServerIPAddress}, PORT = {TCT_Lib.iDC_RabbitMQ_ServerPort}");
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ AutoResendLaneEventToDC : bDC_NetworkStatus {TCT_Lib.bDC_NetworkStatus} -> False, timeout={TCT_Lib.iDC_RabbitMQ_TimeoutSec} sec");
                TCT_Lib.bDC_NetworkStatus = false;
                return false;
            }

            try
            {
                bool bResult = true;
                string szQuery = "SELECT * FROM LaneEventHQ " +
                                "WHERE SendHqStatus=0 "+
                                "ORDER BY EventHqID";
                LogInfo.WriteLog(LogLevel.TRACE, $"+ AutoResendLaneEventToDC : SQL={szQuery}");
                DataTable dtEvent = TCT_Lib.LaneDB.List(szQuery);
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ AutoResendLaneEventToDC : pending={dtEvent?.Rows.Count ?? -1}");
                for (int i=0; i<dtEvent.Rows.Count; i++)
                {
                    int iEventID        = int.Parse(dtEvent.Rows[i]["EventHqID"].ToString());
                    int iEventType      = int.Parse(dtEvent.Rows[i]["EventType"].ToString());
                    var xLaneEvent      = TCT_Obj.GenerateLaneEventHQ(iEventID);
                    string szLaneEvent  = JsonConvert.SerializeObject(xLaneEvent, Formatting.Indented);
                    LogInfo.WriteLog(LogLevel.TRACE, $"+ AutoResendLaneEventToDC : EventHqID={iEventID}, json={szLaneEvent}");
                    bResult             = TCT_Lib.mocRabbitMQ.SendLaneEventRabbitMQ($"Event-{iEventType.ToString()}", szLaneEvent);
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ AutoResendLaneEventToDC : EventHqID={iEventID}, EventType={iEventType}, sent={bResult}");
                    TCT_Lib.LaneDB.UpdateSendLaneEventHQ(iEventID, bResult ? 1 : 0);
                    if (!bResult)
                    {
                        LogInfo.WriteLog(LogLevel.WARN, $"- Warning AutoResendLaneEventToDC : EventHqID={iEventID} failed at {i + 1}/{dtEvent.Rows.Count} -> stop, retry next cycle");
                        break;
                    }

                    await Task.Delay(200);
                }

                return bResult;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error AutoResendLaneEvent : " + err.Message);
                LogInfo.WriteLog("- Error AutoResendLaneEvent : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, $"- AutoResendLaneEventToDC : stack={err.StackTrace}");
                return false;
            }
        }

        //-------------------------------------------
        async public static Task<bool> AutoResendImageCaptureCamera()
        {
            //LogInfo.WriteLog("+ Start AutoResendImageCaptureCamera");
            if (TCT_Lib.szVideoNVR_WebApiBaseUrl == "")
            {
                LogInfo.WriteLogVideoCapture($"- Fail CallWebService_SendImageCaptureCamera : Not found WebApiBaseUrl for send image data.");
                return false;
            }

            try
            {
                bool bResult    = true;
                string szQuery  = "";
                if (TCT_Lib.iVideoNVR_ResendFileDay == 0)
                {
                    szQuery = "SELECT * FROM ImageCaptureCamera " +
                                    "WHERE SendStatusImage=0 AND LaneTransactionID<>0 " +
                                    $"AND DATE(UpdateDateTime)>='2021-11-01' " +   // Dummy Date, Take out when production
                                    "GROUP BY LaneTransactionID " +
                                    "ORDER BY LaneTransactionID";
                }
                else
                {
                    DateTime dateTime = DateTime.Today.AddDays(-TCT_Lib.iVideoNVR_ResendFileDay);
                    szQuery = "SELECT * FROM ImageCaptureCamera " +
                                    "WHERE SendStatusImage=0 AND LaneTransactionID<>0 " +
                                    $"AND DATE(UpdateDateTime)>='{dateTime.ToString("yyyy-MM-dd")}' " +   // Dummy Date, Take out when production
                                    "GROUP BY LaneTransactionID " +
                                    "ORDER BY LaneTransactionID";
                }

                LogInfo.WriteLog(LogLevel.DEBUG, $"+ AutoResendImageCaptureCamera : ResendFileDay={TCT_Lib.iVideoNVR_ResendFileDay}, WebApiBaseUrl={TCT_Lib.szVideoNVR_WebApiBaseUrl}");
                LogInfo.WriteLog(LogLevel.TRACE, $"+ AutoResendImageCaptureCamera : SQL={szQuery}");
                DataTable dtImage = TCT_Lib.LaneDB.List(szQuery);
                LogInfo.WriteLog("+ Found AutoResendImageCaptureCamera#1 : Total=" + dtImage.Rows.Count);
                for (int i = 0; i < dtImage.Rows.Count; i++)
                {
                    int iTransID            = int.Parse(dtImage.Rows[i]["LaneTransactionID"].ToString());
                    DateTime dtUpdateDate   = Convert.ToDateTime(dtImage.Rows[i]["UpdateDateTime"].ToString());

                    int _iJobNo = 0, _nTrx = 0;
                    var xImageCameraObj         = TCT_Obj.GenerateCaptureImageCamera(iTransID, ref _iJobNo, ref _nTrx);
                    string szImageCameraJson    = JsonConvert.SerializeObject(xImageCameraObj, Formatting.Indented);
                    long iCaptureRunningNo      = TCT_Lib.LaneDB.GetCaptureRunningNoOfTrans(_iJobNo, _nTrx, iTransID, dtUpdateDate);
                    LogInfo.WriteLog($"+ Resend ImageCaptureCamera : JobNo={_iJobNo}, nTrx={_nTrx}, LaneTransId={iTransID}, FileName={Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, $"ImageCamera_Trans{iTransID}_N{iCaptureRunningNo.ToString("000000")}.json")}");
                    //using (StreamWriter writer  = File.CreateText(Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, $"ImageCamera_Trans{iTransID}_J{_iJobNo}_N{_nTrx}.json")))
                    using (StreamWriter writer = File.CreateText(Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, $"ImageCamera_Trans{iTransID}_N{iCaptureRunningNo.ToString("000000")}.json")))
                    {
                        writer.WriteLine(szImageCameraJson);
                    }
                    var sendImgCamera   = new WebService_ImageCamera();
                    if (xImageCameraObj == null)
                        LogInfo.WriteLog(LogLevel.WARN, $"- Warning AutoResendImageCaptureCamera : TransID={iTransID} image message is null");
                    var swSend          = System.Diagnostics.Stopwatch.StartNew();
                    bResult             = sendImgCamera.ThreadSendCaptureImage(iTransID, _iJobNo, _nTrx, szImageCameraJson, iCaptureRunningNo, xImageCameraObj.captureImages);
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ AutoResendImageCaptureCamera : TransID={iTransID}, RunningNo={iCaptureRunningNo}, sent={bResult} ({swSend.ElapsedMilliseconds} ms)");
                    if (!bResult)
                    {
                        LogInfo.WriteLog(LogLevel.WARN, $"- Warning AutoResendImageCaptureCamera : TransID={iTransID} failed at {i + 1}/{dtImage.Rows.Count} -> stop, retry next cycle");
                        break;
                    }
                        
                    await Task.Delay(300);
                }

                //-----------------------------------------------------------------------
/*
                szQuery = "SELECT * FROM ImageCaptureCamera " +
                            "WHERE SendStatusImage=0 AND LaneTransactionID=0 " +
                            $"AND DATE(UpdateDateTime)>='2022-01-01' " +   // Dummy Date, Take out when production
                            "AND CameraIndex=1 " +
                            "GROUP BY JobNo, nTrx " +
                            "ORDER BY UpdateDateTime";
                DataTable dt = TCT_Lib.LaneDB.List(szQuery);
                LogInfo.WriteLog("+ Found AutoResendImageCaptureCamera#2 : Total=" + dt.Rows.Count);
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    int iJobNo  = int.Parse(dt.Rows[i]["JobNo"].ToString());
                    int nTrx    = int.Parse(dt.Rows[i]["nTrx"].ToString());
                    DateTime dtLaneTrans = Convert.ToDateTime(dt.Rows[i]["UpdateDateTime"].ToString());
                    int iTransID        = GetLaneTransactionId(iJobNo, nTrx, dtLaneTrans);
                    if (iTransID != 0)
                    {
                        int _iJobNo = iJobNo, _nTrx = nTrx;
                        var xImageCameraObj         = TCT_Obj.GenerateCaptureImageCamera(iTransID, ref _iJobNo, ref _nTrx);
                        string szImageCameraJson    = JsonConvert.SerializeObject(xImageCameraObj);
                        long iCaptureRunningNo      = TCT_Lib.LaneDB.GetCaptureRunningNoOfTrans(_iJobNo, _nTrx, iTransID);
                        //using (StreamWriter writer  = File.CreateText(Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, $"ImageCamera_Trans{iTransID}_J{_iJobNo}_N{_nTrx}.json")))
                        LogInfo.WriteLog($"+ Resend ImageCaptureCamera : JobNo={_iJobNo}, nTrx={_nTrx}, LaneTransId={iTransID}, FileName={Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, $"ImageCamera_Trans{iTransID}_N{iCaptureRunningNo.ToString("000000")}.json")}");
                        using (StreamWriter writer = File.CreateText(Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, $"ImageCamera_Trans{iTransID}_N{iCaptureRunningNo.ToString("000000")}.json")))
                        {
                            writer.WriteLine(szImageCameraJson);
                        }
                        var sendImgCamera = new WebService_ImageCamera();
                        bResult = sendImgCamera.ThreadSendCaptureImage(iTransID, szImageCameraJson, iCaptureRunningNo, xImageCameraObj.captureImages);
                        if (!bResult)
                            break;

                        await Task.Delay(300);
                    }
                }
*/                
                //-----------------------------------------------------------------------

                return bResult;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error AutoResendImageCaptureCamera : " + err.Message);
                LogInfo.WriteLog("- Error AutoResendImageCaptureCamera : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, $"- AutoResendImageCaptureCamera : stack={err.StackTrace}");
                return false;
            }
        }

        public static void UpdateImageCaptureCameraStatus(int iLaneTransID, bool bResult)
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ UpdateImageCaptureCameraStatus : TransID={iLaneTransID}, result={bResult}");
            string szQuery = "";
            if (bResult)
            {
                szQuery = "UPDATE ImageCaptureCamera " +
                        $"SET SendStatusImage=1, SendDateTimeImage='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' " +
                        $"WHERE LaneTransactionID={iLaneTransID}";
            }
            else
            {
                szQuery = "UPDATE ImageCaptureCamera " +
                        $"SET SendRetryImage=SendRetryImage+1, SendDateTimeImage='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' " +
                        $"WHERE LaneTransactionID={iLaneTransID}";
            }
            LogInfo.WriteLog("+ Query : " + szQuery);
            LaneDB.Execute(szQuery);
        }

        public static int GetLaneTransactionId(int iJobNo, int nTrx, DateTime dtTransDate)
        {
            string szQuery = "SELECT * FROM LaneTransaction " +
                                $"WHERE JobNo={iJobNo} AND nTrx={nTrx} "+
                                $"AND DATE(TransactionDateTime)='{dtTransDate.ToString("yyyy-MM-dd")}'";
            LogInfo.WriteLog(LogLevel.TRACE, $"+ GetLaneTransactionId : SQL={szQuery}");
            DataTable dt = TCT_Lib.LaneDB.List(szQuery);
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ GetLaneTransactionId : JobNo={iJobNo}, nTrx={nTrx}, Date={dtTransDate:yyyy-MM-dd}, rows={dt?.Rows.Count ?? -1}");
            if (dt.Rows.Count == 0)
                return 0;

            int iTransId = int.Parse(dt.Rows[0]["TransactionID"].ToString());
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ GetLaneTransactionId : -> TransID={iTransId}");

/*
            //--- Check Duplicate image from Edit Payment
            szQuery = "SELECT * FROM ImageCaptureCamera "+
                        $"WHERE JobNo={iJobNo} AND nTrx={nTrx} AND LaneTransactionID={iTransId} " +
                        $"AND DATE(UpdateDateTime)='{dtTransDate.ToString("yyyy-MM-dd")}'";
            DataTable dtImg = TCT_Lib.LaneDB.List(szQuery);
            if (dtImg.Rows.Count > 0)
            {
                // Found duplicate image because of edit payment, then delete data in database and delete file
                szQuery = "SELECT * FROM ImageCaptureCamera " +
                            $"WHERE JobNo={iJobNo} AND nTrx={nTrx} AND LaneTransactionID=0 " +
                            $"AND DATE(UpdateDateTime)='{dtTransDate.ToString("yyyy-MM-dd")}'";
                dt = TCT_Lib.LaneDB.List(szQuery);
                szQuery = "DELETE FROM ImageCaptureCamera "+
                            $"WHERE JobNo={iJobNo} AND nTrx={nTrx} AND LaneTransactionID=0 " +
                            $"AND DATE(UpdateDateTime)='{dtTransDate.ToString("yyyy-MM-dd")}'";
                LaneDB.Execute(szQuery);
                for (int i=0; i<dt.Rows.Count; i++)
                {
                    string szImgFileName = dt.Rows[i]["ImageCameraFileName"].ToString();
                    LogInfo.WriteLog("+ Delete edit payment filename : " + szImgFileName);
                    try
                    {
                        File.Delete(Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, szImgFileName));
                    }
                    catch (Exception err)
                    {
                        LogInfo.WriteLog("- Fail Delete image file : " + err.Message);
                    }
                }
                //--- Delete Json file
                string szJsonFile = $"ImageCamera_Trans{iTransId}_N{int.Parse(dt.Rows[0]["CaptureRunningNo"].ToString()).ToString("000000")}";
                LogInfo.WriteLog("+ Delete Json filename : " + szJsonFile);
                try
                {
                    File.Delete(Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, szJsonFile));
                }
                catch (Exception err)
                {
                    LogInfo.WriteLog("- Fail Delete Json file : " + err.Message);
                }


                return 0;
            }
            //---------------------------
            szQuery = $"UPDATE ImageCaptureCamera SET LaneTransactionID={iTransId} " +
                        $"WHERE JobNo={iJobNo} AND nTrx={nTrx} " +
                        $"AND DATE(UpdateDateTime)='{dtTransDate.ToString("yyyy-MM-dd")}'";
            LaneDB.Execute(szQuery);
*/

            return iTransId;
        }


        //--------------------------------------------------------------------------------------------------
        //--- Zip File : Backup Log File
        public static bool BackupLogFile()
        {
            try
            {
                LogInfo.WriteLog("+ Start Backup Log File");
                string szLogPath = LogInfo.szLogPath;
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ BackupLogFile : LogPath={szLogPath}, current month={DateTime.Today:yyyy-MM}");
                //--- Backup Lane Log
                var laneLogPath     = Path.Combine(szLogPath, "LaneLog");
                //--- LaneLog -> Log
                string[] szLogFiles = Directory.GetFiles(laneLogPath, "Log_*.log");
                Array.Sort(szLogFiles);
                if (szLogFiles.Length > 0)
                {
                    int iLogMonth = 0, iLogYear = 0;
                    GetMonthLogFile(szLogFiles[0], ref iLogMonth, ref iLogYear);
                    bool bFinish = false;
                    do
                    {
                        if ((iLogMonth != DateTime.Today.Month) || (iLogYear != DateTime.Today.Year))
                        {
                            LogInfo.WriteLog(LogLevel.DEBUG, $"+ BackupLogFile : archive month {iLogYear}-{iLogMonth:00}");
                            szLogFiles = Directory.GetFiles(laneLogPath, $"Log_{iLogYear}-{iLogMonth.ToString("00")}-??.log");
                            foreach (string szFileName in szLogFiles.ToList())
                            {
                                string szZipFile    = Path.Combine(laneLogPath, $"Log_{iLogYear}-{iLogMonth.ToString("00")}.zip");
                                using (var archive  = ZipFile.Open(szZipFile, File.Exists(szZipFile) ? ZipArchiveMode.Update : ZipArchiveMode.Create))
                                {
                                    LogInfo.WriteLog(LogLevel.TRACE, $"+ BackupLogFile : zip {szFileName} -> {szZipFile}");
                                    archive.CreateEntryFromFile(szFileName, Path.GetFileName(szFileName));
                                    File.SetAttributes(szFileName, FileAttributes.Normal);
                                    File.Delete(szFileName);
                                }
                            }

                            szLogFiles = Directory.GetFiles(laneLogPath, "Log_*.log");
                            Array.Sort(szLogFiles);
                            GetMonthLogFile(szLogFiles[0], ref iLogMonth, ref iLogYear);
                        }
                        else
                            bFinish = true;
                    } while (!bFinish);
                }

                //--- LaneLog -> ETC
                szLogFiles = Directory.GetFiles(laneLogPath, "ETC_*.log");
                Array.Sort(szLogFiles);
                if (szLogFiles.Length > 0)
                {
                    int iLogMonth = 0, iLogYear = 0;
                    GetMonthLogFile(szLogFiles[0], ref iLogMonth, ref iLogYear);
                    bool bFinish = false;
                    do
                    {
                        if ((iLogMonth != DateTime.Today.Month) || (iLogYear != DateTime.Today.Year))
                        {
                            LogInfo.WriteLog(LogLevel.DEBUG, $"+ BackupLogFile : archive month {iLogYear}-{iLogMonth:00}");
                            szLogFiles = Directory.GetFiles(laneLogPath, $"ETC_{iLogYear}-{iLogMonth.ToString("00")}-??.log");
                            foreach (string szFileName in szLogFiles.ToList())
                            {
                                string szZipFile    = Path.Combine(laneLogPath, $"ETC_{iLogYear}-{iLogMonth.ToString("00")}.zip");
                                using (var archive  = ZipFile.Open(szZipFile, File.Exists(szZipFile) ? ZipArchiveMode.Update : ZipArchiveMode.Create))
                                {
                                    LogInfo.WriteLog(LogLevel.TRACE, $"+ BackupLogFile : zip {szFileName} -> {szZipFile}");
                                    archive.CreateEntryFromFile(szFileName, Path.GetFileName(szFileName));
                                    File.SetAttributes(szFileName, FileAttributes.Normal);
                                    File.Delete(szFileName);
                                }
                            }

                            szLogFiles = Directory.GetFiles(laneLogPath, "ETC_*.log");
                            Array.Sort(szLogFiles);
                            GetMonthLogFile(szLogFiles[0], ref iLogMonth, ref iLogYear);
                        }
                        else
                            bFinish = true;
                    } while (!bFinish);
                }

                //--- LaneLog -> LSD
                szLogFiles = Directory.GetFiles(laneLogPath, "LSD_*.log");
                Array.Sort(szLogFiles);
                if (szLogFiles.Length > 0)
                {
                    int iLogMonth = 0, iLogYear = 0;
                    GetMonthLogFile(szLogFiles[0], ref iLogMonth, ref iLogYear);
                    bool bFinish = false;
                    do
                    {
                        if ((iLogMonth != DateTime.Today.Month) || (iLogYear != DateTime.Today.Year))
                        {
                            LogInfo.WriteLog(LogLevel.DEBUG, $"+ BackupLogFile : archive month {iLogYear}-{iLogMonth:00}");
                            szLogFiles = Directory.GetFiles(laneLogPath, $"LSD_{iLogYear}-{iLogMonth.ToString("00")}-??.log");
                            foreach (string szFileName in szLogFiles.ToList())
                            {
                                string szZipFile    = Path.Combine(laneLogPath, $"LSD_{iLogYear}-{iLogMonth.ToString("00")}.zip");
                                using (var archive  = ZipFile.Open(szZipFile, File.Exists(szZipFile) ? ZipArchiveMode.Update : ZipArchiveMode.Create))
                                {
                                    LogInfo.WriteLog(LogLevel.TRACE, $"+ BackupLogFile : zip {szFileName} -> {szZipFile}");
                                    archive.CreateEntryFromFile(szFileName, Path.GetFileName(szFileName));
                                    File.SetAttributes(szFileName, FileAttributes.Normal);
                                    File.Delete(szFileName);
                                }
                            }

                            szLogFiles = Directory.GetFiles(laneLogPath, "LSD_*.log");
                            Array.Sort(szLogFiles);
                            GetMonthLogFile(szLogFiles[0], ref iLogMonth, ref iLogYear);
                        }
                        else
                            bFinish = true;
                    } while (!bFinish);
                }

                //--- LaneLog -> EDC
                szLogFiles = Directory.GetFiles(laneLogPath, "EDC_*.log");
                Array.Sort(szLogFiles);
                if (szLogFiles.Length > 0)
                {
                    int iLogMonth = 0, iLogYear = 0;
                    GetMonthLogFile(szLogFiles[0], ref iLogMonth, ref iLogYear);
                    bool bFinish = false;
                    do
                    {
                        if ((iLogMonth != DateTime.Today.Month) || (iLogYear != DateTime.Today.Year))
                        {
                            LogInfo.WriteLog(LogLevel.DEBUG, $"+ BackupLogFile : archive month {iLogYear}-{iLogMonth:00}");
                            szLogFiles = Directory.GetFiles(laneLogPath, $"EDC_{iLogYear}-{iLogMonth.ToString("00")}-??.log");
                            foreach (string szFileName in szLogFiles.ToList())
                            {
                                string szZipFile    = Path.Combine(laneLogPath, $"EDC_{iLogYear}-{iLogMonth.ToString("00")}.zip");
                                using (var archive  = ZipFile.Open(szZipFile, File.Exists(szZipFile) ? ZipArchiveMode.Update : ZipArchiveMode.Create))
                                {
                                    LogInfo.WriteLog(LogLevel.TRACE, $"+ BackupLogFile : zip {szFileName} -> {szZipFile}");
                                    archive.CreateEntryFromFile(szFileName, Path.GetFileName(szFileName));
                                    File.SetAttributes(szFileName, FileAttributes.Normal);
                                    File.Delete(szFileName);
                                }
                            }

                            szLogFiles = Directory.GetFiles(laneLogPath, "EDC_*.log");
                            Array.Sort(szLogFiles);
                            GetMonthLogFile(szLogFiles[0], ref iLogMonth, ref iLogYear);
                        }
                        else
                            bFinish = true;
                    } while (!bFinish);
                }

                //--- MTS Log   -> MTS
                var mtsLogPath      = Path.Combine(szLogPath, "MtsLog");
                try
                {
                    szLogFiles = Directory.GetFiles(mtsLogPath, "MTS_*.log");
                    Array.Sort(szLogFiles);
                    if (szLogFiles.Length > 0)
                    {
                        int iLogMonth = 0, iLogYear = 0;
                        GetMonthLogFile(szLogFiles[0], ref iLogMonth, ref iLogYear);
                        bool bFinish = false;
                        do
                        {
                            if ((iLogMonth != DateTime.Today.Month) || (iLogYear != DateTime.Today.Year))
                            {
                                LogInfo.WriteLog(LogLevel.DEBUG, $"+ BackupLogFile : archive month {iLogYear}-{iLogMonth:00}");
                                szLogFiles = Directory.GetFiles(mtsLogPath, $"MTS_{iLogYear}-{iLogMonth.ToString("00")}-??.log");
                                foreach (string szFileName in szLogFiles.ToList())
                                {
                                    string szZipFile    = Path.Combine(mtsLogPath, $"MTS_{iLogYear}-{iLogMonth.ToString("00")}.zip");
                                    using (var archive  = ZipFile.Open(szZipFile, File.Exists(szZipFile) ? ZipArchiveMode.Update : ZipArchiveMode.Create))
                                    {
                                        LogInfo.WriteLog(LogLevel.TRACE, $"+ BackupLogFile : zip {szFileName} -> {szZipFile}");
                                        archive.CreateEntryFromFile(szFileName, Path.GetFileName(szFileName));
                                        File.SetAttributes(szFileName, FileAttributes.Normal);
                                        File.Delete(szFileName);
                                    }
                                }

                                szLogFiles = Directory.GetFiles(mtsLogPath, "MTS_*.log");
                                Array.Sort(szLogFiles);
                                GetMonthLogFile(szLogFiles[0], ref iLogMonth, ref iLogYear);
                            }
                            else
                                bFinish = true;
                        } while (!bFinish);
                    }
                }
                catch (Exception err)
                {
                    LogInfo.WriteLog($"- Error Zip File {mtsLogPath} : {err.Message}");
                    LogInfo.WriteLog(LogLevel.DEBUG, $"- BackupLogFile : (MtsLog) stack={err.StackTrace}");
                }

                //--- Capture Log   -> VideoCapture
                var captureLogPath      = Path.Combine(szLogPath, "VideoCapture");
                try
                {
                    szLogFiles = Directory.GetFiles(captureLogPath, "Capture_*.log");
                    Array.Sort(szLogFiles);
                    if (szLogFiles.Length > 0)
                    {
                        int iLogMonth = 0, iLogYear = 0;
                        GetMonthCaptureLogFile(szLogFiles[0], ref iLogMonth, ref iLogYear);
                        bool bFinish = false;
                        do
                        {
                            if ((iLogMonth != DateTime.Today.Month) || (iLogYear != DateTime.Today.Year))
                            {
                                LogInfo.WriteLog(LogLevel.DEBUG, $"+ BackupLogFile : archive month {iLogYear}-{iLogMonth:00}");
                                szLogFiles = Directory.GetFiles(captureLogPath, $"Capture_{iLogYear}-{iLogMonth.ToString("00")}-??.log");
                                foreach (string szFileName in szLogFiles.ToList())
                                {
                                    string szZipFile    = Path.Combine(captureLogPath, $"Capture_{iLogYear}-{iLogMonth.ToString("00")}.zip");
                                    using (var archive  = ZipFile.Open(szZipFile, File.Exists(szZipFile) ? ZipArchiveMode.Update : ZipArchiveMode.Create))
                                    {
                                        LogInfo.WriteLog(LogLevel.TRACE, $"+ BackupLogFile : zip {szFileName} -> {szZipFile}");
                                        archive.CreateEntryFromFile(szFileName, Path.GetFileName(szFileName));
                                        File.SetAttributes(szFileName, FileAttributes.Normal);
                                        File.Delete(szFileName);
                                    }
                                }

                                szLogFiles = Directory.GetFiles(captureLogPath, "Capture_*.log");
                                Array.Sort(szLogFiles);
                                GetMonthCaptureLogFile(szLogFiles[0], ref iLogMonth, ref iLogYear);
                            }
                            else
                                bFinish = true;
                        } while (!bFinish);
                    }
                }
                catch (Exception err)
                {
                    LogInfo.WriteLog($"- Error Zip File {mtsLogPath} : {err.Message}");
                    LogInfo.WriteLog(LogLevel.DEBUG, $"- BackupLogFile : (VideoCapture) path={captureLogPath}, stack={err.StackTrace}");
                }

                //===========================================================================================
                //--- Backup Lane Event
                var laneEventPath     = Path.Combine(szLogPath, "LaneEvent");
                //--- LaneEvent -> Event
                string[] szEventFolders = Directory.GetDirectories(laneEventPath, "Event_*");
                Array.Sort(szEventFolders);
                foreach (string szFolder in szEventFolders.ToList())
                {
                    try
                    {
                        //--- Check this month, year
                        string szDirName = Path.GetFileName(szFolder);
                        int iMonth      = int.Parse(szDirName.Substring(6, 2));
                        int iYear       = int.Parse(szDirName.Substring(9, 4));
                        if ((iMonth != DateTime.Today.Month) || (iYear != DateTime.Today.Year))
                        {
                            string szZipFile = Path.Combine(laneEventPath, $"{szDirName}.zip");
                            LogInfo.WriteLog(LogLevel.DEBUG, $"+ BackupLogFile : zip folder {szFolder} -> {szZipFile}");
                            Action_ZipFolder(szFolder, szZipFile);
                            DeleteDirectory(szFolder);
                        }
                    }
                    catch (Exception err)
                    {
                        LogInfo.WriteLog($"- Error Zip File {szFolder} : {err.Message}");
                    }
                }

                //--- LaneEvent -> RabbitMQ
                string[] szRabbitMQFolders = Directory.GetDirectories(laneEventPath, "RabbitMQ_*");
                Array.Sort(szRabbitMQFolders);
                foreach (string szFolder in szRabbitMQFolders.ToList())
                {
                    try
                    {
                        //--- Check this month, year
                        string szDirName    = Path.GetFileName(szFolder);
                        int iMonth          = int.Parse(szDirName.Substring(9, 2));
                        int iYear           = int.Parse(szDirName.Substring(12, 4));
                        if ((iMonth != DateTime.Today.Month) || (iYear != DateTime.Today.Year))
                        {
                            string szZipFile = Path.Combine(laneEventPath, $"{szDirName}.zip");
                            LogInfo.WriteLog(LogLevel.DEBUG, $"+ BackupLogFile : zip folder {szFolder} -> {szZipFile}");
                            Action_ZipFolder(szFolder, szZipFile);
                            DeleteDirectory(szFolder);
                        }
                    }
                    catch (Exception err)
                    {
                        LogInfo.WriteLog($"- Error Zip File {szFolder} : {err.Message}");
                    }
                }
               

                //-----------------------------------------------------------------------------------------s
                //--- Backup LogTrans
                LogInfo.WriteLog("+ Finish Backup Log File");

                return true;
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Error BackupLogFile : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, $"- BackupLogFile : stack={err.StackTrace}");
                return false;
            }
        }

        private static bool GetMonthLogFile(string szLogFileName, ref int iMonth, ref int iYear)
        {
          
            string szDate   = Path.GetFileNameWithoutExtension(szLogFileName);
            iYear           = int.Parse(szDate.Substring(4, 4));
            iMonth          = int.Parse(szDate.Substring(9, 2));

            return true;
        }

        private static bool GetMonthCaptureLogFile(string szLogFileName, ref int iMonth, ref int iYear)
        {

            string szDate   =  Path.GetFileNameWithoutExtension(szLogFileName);
            iYear           = int.Parse(szDate.Substring(8, 4));
            iMonth          = int.Parse(szDate.Substring(13, 2));

            return true;
        }

        private static void Action_ZipFile(string szSrcFileName, string szZipFileName)
        {
            using (var archive = ZipFile.Open(szZipFileName, ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(szSrcFileName, Path.GetFileName(szSrcFileName));
            }
        }

        private static void Action_ZipFolder(string szFolerName, string szZipFileName)
        {
            using (var archive = ZipFile.Open(szZipFileName, ZipArchiveMode.Create))
            {
                var files = Directory.GetFiles(szFolerName);
                foreach (var fPath in files.ToList())
                {
                    archive.CreateEntryFromFile(fPath, Path.GetFileName(fPath));
                }
            }
        }

        private void Action_AddZipFile(string szSrcFileName, string szZipFileName)
        {
            using (var archive = ZipFile.Open(szZipFileName, ZipArchiveMode.Update))
            {
                archive.CreateEntryFromFile(szSrcFileName, Path.GetFileName(szSrcFileName));
            }
        }

        public static void DeleteDirectory(string target_dir)
        {
            string[] files  = Directory.GetFiles(target_dir);
            string[] dirs   = Directory.GetDirectories(target_dir);
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ DeleteDirectory : {target_dir}, files={files?.Length ?? 0}, dirs={dirs?.Length ?? 0}");

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(target_dir, false);
        }

        public static void ManualImportDataFile()
        {
            LogInfo.WriteLog("+ Start Process ManualImportDataFile");
            if (TCT_Lib.szManualImportPath.Trim() == "")
                LogInfo.WriteLog(LogLevel.DEBUG, "+ ManualImportDataFile : ManualImportPath not configured -> skip");
            if (TCT_Lib.szManualImportPath.Trim() != "")
            {
                if (!Directory.Exists(TCT_Lib.szManualImportPath))
                {
                    LogInfo.WriteLog("- Not found ManualImportPath : " + TCT_Lib.szManualImportPath);
                }
                else
                {
                    // Find file in Folder, if found then import file into system.
                    string[] files = Directory.GetFiles(TCT_Lib.szManualImportPath.Trim());
                    LogInfo.WriteLog("+ Found File in ManualImportPath : " + files.Length);

                    foreach (string file in files)
                    {
                        LogInfo.WriteLog("+ Process Import FileName : " + file);

                        try
                        {
                            string dataJson     = File.ReadAllText(file);
                            LogInfo.WriteLog(LogLevel.DEBUG, $"+ ManualImportDataFile : {file}, len={dataJson?.Length ?? 0}");
                            var swImport        = System.Diagnostics.Stopwatch.StartNew();
                            TCT_Lib.mocRabbitMQ.MessageReceiverOnRabbitMqRecvMessage(dataJson, true, false);
                            LogInfo.WriteLog(LogLevel.DEBUG, $"+ ManualImportDataFile : imported {Path.GetFileName(file)} ({swImport.ElapsedMilliseconds} ms) -> move to Backup");
                            string szBackupPath = Path.Combine(TCT_Lib.szManualImportPath.Trim(), "Backup");

                            try
                            {
                                File.Delete(Path.Combine(szBackupPath, Path.GetFileName(file)));
                                File.Move(file, Path.Combine(szBackupPath, Path.GetFileName(file)));
                            }
                            catch (Exception err2)
                            {
                                LogInfo.WriteLog("- Error Remove Backup File : " + err2.Message);
                                if (File.Exists(Path.Combine(szBackupPath, Path.GetFileName(file))))
                                    File.Delete(Path.Combine(szBackupPath, Path.GetFileName(file)));
                                try
                                {
                                    File.Move(file, Path.Combine(szBackupPath, Path.GetFileName(file)));
                                }
                                catch (Exception err3)
                                {
                                    LogInfo.WriteLog("- Error Delete Backup File : " + err3.Message);
                                    File.Delete(file);
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            LogInfo.WriteLog("- Error Import File : " + err.Message);
                            LogInfo.WriteLog(LogLevel.DEBUG, $"- ManualImportDataFile : file={file}, stack={err.StackTrace}");
                        }
                    }
                }
            }
            LogInfo.WriteLog("+ Stop Process ManualImportDataFile");
        }

        //------------------------------------------------------------------------------------------------
        public static bool UpdateDatabaseStructure()
        {
/*
            string szMsg = @"{
                            ""parameterName"": ""PROMOTION"",
                            ""timestamp"": ""2022-06-08T11:29:40.499+07"",
                            ""version"": 1,
                            ""promotion"": [
                                {
                                    ""effectiveFromDatetime"": ""2022-01-01T00:00:00.000+07"",
                                    ""effectiveToDatetime"": ""2022-08-31T23:59:59.999+07"",
                                    ""promotionTypeId"": 2,
                                    ""messageLine01"": ""โปรโมชั่นส่วนลด"",
                                    ""messageLine02"": ""สำหรับผู้ผ่านทางตั้งแต่วันที่"",
                                    ""messageLine03"": ""1 มกราคม 2565 ถึง 31 สิงหาคม 2565"",
                                    ""messageLine04"": ""กรุณาแสกน QR CODE"",
                                    ""qrCode"": ""http://localhost/promotion?p=9999999999"",
                                    ""status"": ""A""
                                }
                            ]
                        }";

            TCT_Lib.mocRabbitMQ.MessageReceiverOnRabbitMqRecvMessage(szMsg);
*/

            //---------------------------------------------
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ UpdateDatabaseStructure : start, LaneDB={(LaneDB == null ? "null" : "ok")}");
            try
            {
                string szQuery = "";
                if (!LaneDB.ChkDbHasTable("ImageCaptureCamera"))
                {
                    szQuery = @"CREATE TABLE ImageCaptureCamera (
                                ImageCaptureID INTEGER DEFAULT 0,
                                CaptureRunningNo INTEGER DEFAULT 0,
                                JobNo INTEGER DEFAULT 0,
                                nTrx INTEGER DEFAULT 0,
                                CurMode INTEGER DEFAULT 0,
                                CameraIndex INTEGER DEFAULT 0,
                                ImageCameraFileName TEXT,
                                UpdateDateTime DATETIME,
                                LaneTransactionID INTEGER DEFAULT 0,
                                LaneTransactionDate DATETIME,
                                SendStatusImage INTEGER DEFAULT 0,
                                SendRetryImage INTEGER DEFAULT 0,
                                SendDateTimeImage DATETIME,
                                PRIMARY KEY (ImageCaptureID)
                            )";
                    LogInfo.WriteLog(LogLevel.DEBUG, "+ UpdateDatabaseStructure : apply -> " + szQuery.Trim().Split('\n')[0].Trim());
                    TCT_Lib.LaneDB.Execute(szQuery);
                }

                if (!LaneDB.ChkDbHasTable("CaptureRunningNo"))
                {
                    szQuery = @"CREATE TABLE CaptureRunningNo (
                                MaxCaptureRunningNo INTEGER DEFAULT 0
                                )";
                    LogInfo.WriteLog(LogLevel.DEBUG, "+ UpdateDatabaseStructure : apply -> " + szQuery.Trim().Split('\n')[0].Trim());
                    TCT_Lib.LaneDB.Execute(szQuery);
                }

                if(!LaneDB.ChkDbHasColumnInTable("LaneAlarmRTO", "SendStatusDC"))
                {
                    szQuery = @"ALTER TABLE LaneAlarmRTO ADD SendStatusDC INTEGER NOT NULL DEFAULT 0;
                                ALTER TABLE LaneAlarmRTO ADD SendRetryDC INTEGER NOT NULL DEFAULT 0;
                                ALTER TABLE LaneAlarmRTO ADD SendDateTimeDC DATETIME;
                            ";
                    LogInfo.WriteLog(LogLevel.DEBUG, "+ UpdateDatabaseStructure : apply -> " + szQuery.Trim().Split('\n')[0].Trim());
                    TCT_Lib.LaneDB.Execute(szQuery);
                }

                if (!LaneDB.ChkDbHasTable("EdcClearBatch"))
                {
                    szQuery = @"CREATE TABLE EdcClearBatch (
                                ClearBatchID INTEGER DEFAULT 0,
                                JobNo INTEGER DEFAULT 0,
                                StaffShiftID INTEGER DEFAULT 0,
                                ClearBatchDateTime DATETIME,
                                PRIMARY KEY (ClearBatchID)
                                )";
                    LogInfo.WriteLog(LogLevel.DEBUG, "+ UpdateDatabaseStructure : apply -> " + szQuery.Trim().Split('\n')[0].Trim());
                    TCT_Lib.LaneDB.Execute(szQuery);
                }

                if (!LaneDB.ChkDbHasTable("PromotionData"))
                {
                    szQuery = @"CREATE TABLE PromotionData (
                                PromotionID INTEGER DEFAULT 0,
                                Version INTEGER DEFAULT 0,
                                SendDateTime DATETIME,
                                EffectiveFromDate DATETIME,
                                EffectiveToDate DATETIME,
                                PromotionTypeID INTEGER DEFAULT 1,
                                MessageLine1 TEXT,
                                MessageLine2 TEXT,
                                MessageLine3 TEXT,
                                MessageLine4 TEXT,
                                QrCodeData TEXT,
                                Status TEXT,
                                UpdateDateTime DATETIME,
                                PRIMARY KEY (PromotionID))";
                    LogInfo.WriteLog(LogLevel.DEBUG, "+ UpdateDatabaseStructure : apply -> " + szQuery.Trim().Split('\n')[0].Trim());
                    TCT_Lib.LaneDB.Execute(szQuery);
                }

                if (!LaneDB.ChkDbHasTable("DMTCardData"))
                {
                    szQuery = @"CREATE TABLE DMTCardData (
                                DmtCardID INTEGER DEFAULT 0,
                                Version INTEGER DEFAULT 0,
                                SendDateTime DATETIME,
                                CardSerialNo TEXT,
                                DmtCarNo TEXT,
                                DmtLicensePlate TEXT,
                                VehicleClass INTEGER DEFAULT 0,
                                Department TEXT,
                                Affiliation TEXT,
                                AllowPass INTEGER DEFAULT 0,
                                Status TEXT,
                                UpdateDateTime DATETIME,
                                PRIMARY KEY (DmtCardID))";
                    LogInfo.WriteLog(LogLevel.DEBUG, "+ UpdateDatabaseStructure : apply -> " + szQuery.Trim().Split('\n')[0].Trim());
                    TCT_Lib.LaneDB.Execute(szQuery);
                }

                if (!LaneDB.ChkDbHasTable("DMTCardTrans"))
                {
                    szQuery = @"CREATE TABLE DMTCardTrans(
                                DmtCardTransID INTEGER DEFAULT 0,
                                CardSerialNo TEXT,
                                DmtCarNo TEXT,
                                DmtLicensePlate TEXT,
                                VehicleClass INTEGER DEFAULT 0,
                                Department TEXT,
                                Affiliation TEXT,
                                UpdateDateTime DATETIME,
                                PRIMARY KEY (DmtCardTransID))";
                    LogInfo.WriteLog(LogLevel.DEBUG, "+ UpdateDatabaseStructure : apply -> " + szQuery.Trim().Split('\n')[0].Trim());
                    TCT_Lib.LaneDB.Execute(szQuery);
                }

                if (!LaneDB.ChkDbHasColumnInTable("LaneTransaction", "DmtCardTransID"))
                {
                    szQuery = @"ALTER TABLE LaneTransaction ADD DmtCardTransID INTEGER NOT NULL DEFAULT 0;
                            ";
                    LogInfo.WriteLog(LogLevel.DEBUG, "+ UpdateDatabaseStructure : apply -> " + szQuery.Trim().Split('\n')[0].Trim());
                    TCT_Lib.LaneDB.Execute(szQuery);
                }

                if (!LaneDB.ChkDbHasTable("TfiData"))
                {
                    szQuery = @"CREATE TABLE TfiData (
                                TfiID INTEGER DEFAULT 0,
                                Version INTEGER DEFAULT 0,
                                SendDateTime DATETIME,
                                ImageFileName TEXT,
                                EffectiveDateTime DATETIME,
                                UpdateBy TEXT,
                                SendTfiStatus INTEGER DEFAULT 0,
                                SendTfiMessage TEXT,
                                SendTfiDateTime DATETIME,
                                UpdateDateTime DATETIME,
                                PRIMARY KEY (TfiID))";
                    LogInfo.WriteLog(LogLevel.DEBUG, "+ UpdateDatabaseStructure : apply -> " + szQuery.Trim().Split('\n')[0].Trim());
                    TCT_Lib.LaneDB.Execute(szQuery);
                }

                if (!LaneDB.ChkDbHasTable("SalesCouponType"))
                {
                    szQuery = @"CREATE TABLE SalesCouponType (
                                CouponTypeID INTEGER DEFAULT 0, 
                                Version INTEGER DEFAULT 0, 
                                SendDateTime DATETIME, 
                                CouponType TEXT, 
                                UpdateDateTime DATETIME, 
                                PRIMARY KEY (CouponTypeID))";
                    LogInfo.WriteLog(LogLevel.DEBUG, "+ UpdateDatabaseStructure : apply -> " + szQuery.Trim().Split('\n')[0].Trim());
                    TCT_Lib.LaneDB.Execute(szQuery);

                    szQuery = @"INSERT INTO SalesCouponType VALUES (1, 1, '2023-05-30 07:07:07', '35', '2023-05-30 08:08:08')";
                    LogInfo.WriteLog(LogLevel.DEBUG, "+ UpdateDatabaseStructure : apply -> " + szQuery.Trim().Split('\n')[0].Trim());
                    TCT_Lib.LaneDB.Execute(szQuery);

                    szQuery = @"INSERT INTO SalesCouponType VALUES (2, 1, '2023-05-30 07:07:07', '80', '2023-05-30 08:08:08')";
                    LogInfo.WriteLog(LogLevel.DEBUG, "+ UpdateDatabaseStructure : apply -> " + szQuery.Trim().Split('\n')[0].Trim());
                    TCT_Lib.LaneDB.Execute(szQuery);

                }

                LogInfo.WriteLog(LogLevel.DEBUG, "+ UpdateDatabaseStructure : done");
                return true;
            }
            catch (Exception err)
            {
                LogInfo.WriteLog(" - Error UpdateDatabaseStructure : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, $"- UpdateDatabaseStructure : stack={err.StackTrace}");
                return false;
            }
        }

        //--------------------------------------------------------------------------------------------------
        //--- Clear database file
        //--- v2.0.5: data that goes to DC is deleted ONLY after it was sent to DC (status=1) AND is older than
        //    KeepDatabaseDay (default 30). Unsent / failed rows are never deleted. The newest row of every table
        //    is always kept because the next ID = MAX(ID)+1 (deleting it would restart IDs and duplicate at DC).
        //    Deletes run in batches so a big first purge does not hold the DB lock for a long time.
        public static DateTime dtLastClearDatabase      = DateTime.MinValue;
        static int iClearDatabaseRunning                = 0;
        const int CLEAR_DB_BATCH_ROWS                   = 2000;

        public static bool ClearDatabaseFile(bool bAllowVacuum = true, bool bBackground = false)
        {
            LogInfo.WriteLog($"+ Start Clear Database File (background={bBackground})");
            dtLastClearDatabase = DateTime.Today;           // once a day, also when skipped below (no retry every cycle)
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ ClearDatabaseFile : KeepDatabaseDay={TCT_Lib.iKeepDbDay}, LaneDB={(TCT_Lib.LaneDB == null ? "null" : "ok")}");
            if (TCT_Lib.LaneDB == null)
                return false;

            if (TCT_Lib.iKeepDbDay < 5)
            {
                LogInfo.WriteLog("- Fail ClearDabaseFile : Keep Database day is less than 5 days = " + TCT_Lib.iKeepDbDay);
                return false;
            }

            if (Interlocked.CompareExchange(ref iClearDatabaseRunning, 1, 0) != 0)
            {
                LogInfo.WriteLog(LogLevel.WARN, "- Warning ClearDatabaseFile : previous clear still running -> skip");
                return false;
            }

            try
            {
                var swClear         = System.Diagnostics.Stopwatch.StartNew();
                var InvC            = CultureInfo.InvariantCulture;
                string szKeep       = DateTime.Today.AddDays(-TCT_Lib.iKeepDbDay).ToString("yyyy-MM-dd", InvC);
                LogInfo.WriteLog($"+ ClearDatabaseFile : delete data older than {szKeep} (KeepDatabaseDay={TCT_Lib.iKeepDbDay}), DC data only when sent to DC");

                //=== Data sent to DC : delete only SendStatus=1 ==================================
                //--- LaneTransaction (DC)
                PurgeTableRows("LaneTransaction",
                    $"SendStatusDC=1 AND TransactionDateTime<'{szKeep}' " +
                    "AND TransactionID<(SELECT MAX(TransactionID) FROM LaneTransaction)", bBackground);

                //--- EmvDetail / DMTCardTrans / ImageCaptureCamera : child rows, delete only when no transaction left that uses them
                PurgeTableRows("EmvDetail",
                    $"UpdateDate<'{szKeep}' " +
                    "AND EmvDetailID<(SELECT MAX(EmvDetailID) FROM EmvDetail) " +
                    "AND EmvDetailID NOT IN (SELECT EMV_DetailID FROM LaneTransaction WHERE EMV_DetailID>0) " +
                    "AND EmvDetailID NOT IN (SELECT EmvDetailID FROM SalesCouponTransaction WHERE EmvDetailID>0)", bBackground);

                if (TCT_Lib.LaneDB.ChkDbHasTable("DMTCardTrans"))
                    PurgeTableRows("DMTCardTrans",
                        $"UpdateDateTime<'{szKeep}' " +
                        "AND DmtCardTransID<(SELECT MAX(DmtCardTransID) FROM DMTCardTrans) " +
                        "AND DmtCardTransID NOT IN (SELECT DmtCardTransID FROM LaneTransaction WHERE DmtCardTransID>0)", bBackground);

                if (TCT_Lib.LaneDB.ChkDbHasTable("ImageCaptureCamera"))
                    PurgeTableRows("ImageCaptureCamera",
                        $"UpdateDateTime<'{szKeep}' " +
                        "AND ImageCaptureID<(SELECT MAX(ImageCaptureID) FROM ImageCaptureCamera) " +
                        "AND LaneTransactionID NOT IN (SELECT TransactionID FROM LaneTransaction)", bBackground);

                //--- LaneEventHQ (DC)
                PurgeTableRows("LaneEventHQ",
                    $"SendHqStatus=1 AND EventDateTime<'{szKeep}' " +
                    "AND EventHqID<(SELECT MAX(EventHqID) FROM LaneEventHQ)", bBackground);

                //--- LaneAlarmRTO : only devices in MTS AlarmDeviceList go to DC -> those must be sent first
                string szAlarmDcCond = "SendStatusDC=1";
                string szMtsList     = (TCT_Lib.szMTS_AlarmDeviceList ?? "").Trim();
                if (szMtsList == "")
                    szAlarmDcCond = "1=1";                                                          // no alarm goes to DC
                else if (System.Text.RegularExpressions.Regex.IsMatch(szMtsList, @"^\d+(\s*,\s*\d+)*$"))
                    szAlarmDcCond = $"(SendStatusDC=1 OR AlarmDevice NOT IN ({szMtsList}))";
                PurgeTableRows("LaneAlarmRTO",
                    $"{szAlarmDcCond} AND AlarmDateTime<'{szKeep}' " +
                    "AND AlarmRtoID<(SELECT MAX(AlarmRtoID) FROM LaneAlarmRTO)", bBackground);

                //--- StaffShift (DC : BOJ + EOJ) -> both sent, shift ended, not the current / last shift
                PurgeTableRows("StaffShift",
                    "BojSendStatusDC=1 AND EojSendStatusDC=1 " +
                    $"AND StaffEndShiftDateTime IS NOT NULL AND StaffEndShiftDateTime<>'' AND StaffEndShiftDateTime<'{szKeep}' " +
                    $"AND StaffShiftID<>{TCT_Lib.iStaffShiftID} " +
                    "AND StaffShiftID<(SELECT MAX(StaffShiftID) FROM StaffShift)", bBackground);

                //--- TransactionStatistic : statistic of a shift that was deleted above
                PurgeTableRows("TransactionStatistic",
                    "StaffShiftID<(SELECT MAX(StaffShiftID) FROM StaffShift) " +
                    "AND StaffShiftID NOT IN (SELECT StaffShiftID FROM StaffShift)", bBackground);

                //--- SalesCouponTransaction / SalesCouponDetail : never sent to DC (SendStatusDC is never set) -> keep
                LogInfo.WriteLog(LogLevel.DEBUG, "+ ClearDatabaseFile : SalesCouponTransaction / SalesCouponDetail kept (not sent to DC)");

                //=== Local data (not sent to DC) : delete by date only ==========================
                PurgeTableRows("LaneEvent",
                    $"EventDateTime<'{szKeep}' AND EventID<(SELECT MAX(EventID) FROM LaneEvent)", bBackground);

                PurgeTableRows("LaneEventRTO",
                    $"EventDateTime<'{szKeep}' AND EventRtoID<(SELECT MAX(EventRtoID) FROM LaneEventRTO)", bBackground);
                PurgeTableRows("LaneEventRTO",
                    "DeviceType IN (2,3,4,5,8,9,13,14,15) AND EventRtoID<(SELECT MAX(EventRtoID) FROM LaneEventRTO)", bBackground);

                PurgeTableRows("SalesCouponList",
                    $"SalesDateTime<'{szKeep}'", bBackground);

                PurgeTableRows("SupervisorShift",
                    $"StartShiftDateTime<'{szKeep}' " +
                    $"AND SupervisorShiftID<>{TCT_Lib.iSupervisorShiftID} " +
                    "AND SupervisorShiftID<(SELECT MAX(SupervisorShiftID) FROM SupervisorShift)", bBackground);

                PurgeTableRows("TransactionSummary",
                    $"TransactionDate<'{szKeep}'", bBackground);

                //------------------------------------------------------------
                int iHour = DateTime.Now.Hour;
                bool bVacuum = bAllowVacuum && (iHour >= 1) && (iHour <= 4);
                LogInfo.WriteLog($"+ ClearDatabaseFile : deletes done ({swClear.ElapsedMilliseconds} ms), hour={iHour} -> VACUUM={bVacuum}");
                if (bVacuum)
                {
                    Thread.Sleep(1 * 1000);
                    LogInfo.WriteLog("+ Call VACUUM (Purge Database)");
                    swClear.Restart();
                    TCT_Lib.LaneDB.Execute("VACUUM;");
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ ClearDatabaseFile : VACUUM done ({swClear.ElapsedMilliseconds} ms)");
                }

                //------------------------------------------------------------

                return true;
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Error ClearDatabaseFile : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, $"- ClearDatabaseFile : stack={err.StackTrace}");
                return false;
            }
            finally
            {
                Interlocked.Exchange(ref iClearDatabaseRunning, 0);
            }
        }

        //--- v2.0.5: DELETE in batches (rowid LIMIT) -> each batch holds the DB lock only briefly
        static int PurgeTableRows(string szTable, string szWhere, bool bBackground)
        {
            string szQuery = $"DELETE FROM {szTable} WHERE rowid IN (SELECT rowid FROM {szTable} WHERE {szWhere} LIMIT {CLEAR_DB_BATCH_ROWS})";
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ Clear {szTable} : {szQuery}");
            int iTotal = 0;
            while (true)
            {
                int iAffected = TCT_Lib.LaneDB.Execute(szQuery);
                if (iAffected < 0)
                {
                    LogInfo.WriteLog(LogLevel.WARN, $"- Warning Clear {szTable} : DELETE failed after {iTotal} rows -> stop this table");
                    break;
                }
                iTotal += iAffected;
                if (iAffected < CLEAR_DB_BATCH_ROWS)
                    break;
                if (bBackground)
                    Thread.Sleep(200);      // let lane transactions use the DB between batches
            }
            LogInfo.WriteLog($"+ Clear {szTable} : deleted {iTotal} rows");
            return iTotal;
        }

        //---------------------------------------------------------------------------------------------------
        public static void LoadKeyMapping()
        {
            keyMapping.Clear();
            keyMapping['u'] = (int)KeyPressedType.CLASS_1;
            keyMapping['v'] = (int)KeyPressedType.CLASS_2;
            keyMapping['y'] = (int)KeyPressedType.CLASS_5;      // Emergency
            keyMapping['b'] = (int)KeyPressedType.CLASS_6;      // Towing
            keyMapping['z'] = (int)KeyPressedType.CLASS_7;      // Exempt
            keyMapping['m'] = (int)KeyPressedType.CLASS_8;      // LeftExit
            keyMapping['f'] = (int)KeyPressedType.CLASS_9;      // HPMC
            keyMapping['c'] = (int)KeyPressedType.PAY_CASH;
            keyMapping['r'] = (int)KeyPressedType.PAY_COUPON;
            keyMapping['q'] = (int)KeyPressedType.PAY_QRCODE;
            keyMapping['k'] = (int)KeyPressedType.PAY_EMV;
            keyMapping['x'] = (int)KeyPressedType.PAY_DMTCARD;  // DMT-Service
            keyMapping['t'] = (int)KeyPressedType.CONFIRM;
            keyMapping['g'] = (int)KeyPressedType.MANUAL;       // Manual Login
            keyMapping['e'] = (int)KeyPressedType.RECEIPT;
            keyMapping['s'] = (int)KeyPressedType.FORCE_RESET;
            keyMapping['o'] = (int)KeyPressedType.VIOLATION;
            keyMapping['n'] = (int)KeyPressedType.EDIT;
            keyMapping['a'] = (int)KeyPressedType.OTL_OPEN;
            keyMapping['w'] = (int)KeyPressedType.OTL_CLOSE;
            keyMapping['j'] = (int)KeyPressedType.END_SHIFT;
            keyMapping['l'] = (int)KeyPressedType.SALE_COUPON;
            keyMapping['h'] = (int)KeyPressedType.PRINT_CONVOY;
            keyMapping['d'] = (int)KeyPressedType.MODE;
            keyMapping['.'] = (int)KeyPressedType.DOT;
            keyMapping['0'] = (int)KeyPressedType.NUM_0;
            keyMapping['1'] = (int)KeyPressedType.NUM_1;
            keyMapping['2'] = (int)KeyPressedType.NUM_2;
            keyMapping['3'] = (int)KeyPressedType.NUM_3;
            keyMapping['4'] = (int)KeyPressedType.NUM_4;
            keyMapping['5'] = (int)KeyPressedType.NUM_5;
            keyMapping['6'] = (int)KeyPressedType.NUM_6;
            keyMapping['7'] = (int)KeyPressedType.NUM_7;
            keyMapping['8'] = (int)KeyPressedType.NUM_8;
            keyMapping['9'] = (int)KeyPressedType.NUM_9;
            keyMapping['p'] = (int)KeyPressedType.ESC;          // = CANCEL
            //keyMapping[27] = (int)KeyPressedType.ESC;
            //keyMapping[''] = (int)KeyPressedType.END;
            //keyMapping[13]      = (int)KeyPressedType.ENTER;
            keyMapping[65293]   = (int)KeyPressedType.ENTER;
            //--- v2.0.2: real key codes (Cherry BACK SPACE / END / keypad ENTER, PC Esc/Tab)
            keyMapping[65421]   = (int)KeyPressedType.ENTER;        // KP_Enter
            keyMapping[65288]   = (int)KeyPressedType.BACKSPACE;    // BackSpace
            keyMapping[65289]   = (int)KeyPressedType.TAB;          // Tab
            keyMapping[65307]   = (int)KeyPressedType.ESC;          // Escape (= CANCEL)
            keyMapping[65367]   = (int)KeyPressedType.END;          // End
            keyMapping[65361]   = (int)KeyPressedType.LEFT;
            keyMapping[65362]   = (int)KeyPressedType.UP;
            keyMapping[65363]   = (int)KeyPressedType.RIGHT;
            keyMapping[65364]   = (int)KeyPressedType.DOWN;

            //keyMapping[0x2190]  = (int)KeyPressedType.LEFT;
            //keyMapping[0x2191]  = (int)KeyPressedType.UP;
            //keyMapping[0x2192]  = (int)KeyPressedType.RIGHT;
            //keyMapping[0x2193]  = (int)KeyPressedType.DOWN;

            keyMapping['=']     = (int)KeyPressedType.PAY_PM6;
            keyMapping['[']     = (int)KeyPressedType.PAY_PM7;
            keyMapping[']']     = (int)KeyPressedType.PAY_PM8;
            keyMapping['\\']    = (int)KeyPressedType.PAY_PM9;
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ LoadKeyMapping : {keyMapping.Count} keys mapped");

        }

        public static string KeyMapName(int iKeyId)
        {
            switch (iKeyId)
            {
                case (int)KeyPressedType.CLASS_1:       return "CLASS_1";
                case (int)KeyPressedType.CLASS_2:       return "CLASS_2";
                case (int)KeyPressedType.CLASS_5:       return "CLASS_5";
                case (int)KeyPressedType.CLASS_6:       return "CLASS_6";
                case (int)KeyPressedType.CLASS_7:       return "CLASS_7";
                case (int)KeyPressedType.CLASS_8:       return "CLASS_8";
                case (int)KeyPressedType.CLASS_9:       return "CLASS_9";
                case (int)KeyPressedType.PAY_CASH:      return "PAY_CASH";
                case (int)KeyPressedType.PAY_COUPON:    return "PAY_COUPON";
                case (int)KeyPressedType.PAY_QRCODE:    return "PAY_QRCODE";
                case (int)KeyPressedType.PAY_EMV:       return "PAY_EMV";
                case (int)KeyPressedType.PAY_DMTCARD:   return "PAY_DMTCARD";
                case (int)KeyPressedType.CONFIRM:       return "CONFIRM";
                case (int)KeyPressedType.MANUAL:        return "MANUAL";
                case (int)KeyPressedType.RECEIPT:       return "RECEIPT";
                case (int)KeyPressedType.FORCE_RESET:   return "FORCE_RESET";
                case (int)KeyPressedType.VIOLATION:     return "VIOLATION";
                case (int)KeyPressedType.EDIT:          return "EDIT";
                case (int)KeyPressedType.OTL_OPEN:      return "OTL_OPEN";
                case (int)KeyPressedType.OTL_CLOSE:     return "OTL_CLOSE";
                case (int)KeyPressedType.END_SHIFT:     return "END_SHIFT";
                case (int)KeyPressedType.SALE_COUPON:   return "SALE_COUPON";
                case (int)KeyPressedType.PRINT_CONVOY:  return "PRINT_CONVOY";
                case (int)KeyPressedType.MODE:          return "MODE";
                case (int)KeyPressedType.DOT:           return "DOT";
                case (int)KeyPressedType.NUM_0:         return "NUM_0";
                case (int)KeyPressedType.NUM_1:         return "NUM_1";
                case (int)KeyPressedType.NUM_2:         return "NUM_2";
                case (int)KeyPressedType.NUM_3:         return "NUM_3";
                case (int)KeyPressedType.NUM_4:         return "NUM_4";
                case (int)KeyPressedType.NUM_5:         return "NUM_5";
                case (int)KeyPressedType.NUM_6:         return "NUM_6";
                case (int)KeyPressedType.NUM_7:         return "NUM_7";
                case (int)KeyPressedType.NUM_8:         return "NUM_8";
                case (int)KeyPressedType.NUM_9:         return "NUM_9";
                case (int)KeyPressedType.ESC:           return "ESC";
                case (int)KeyPressedType.BACKSPACE:     return "BACKSPACE";     // v2.0.2
                case (int)KeyPressedType.TAB:           return "TAB";
                case (int)KeyPressedType.END:           return "END";
                case (int)KeyPressedType.ENTER:         return "ENTER";
                case (int)KeyPressedType.LEFT:          return "LEFT";
                case (int)KeyPressedType.UP:            return "UP";
                case (int)KeyPressedType.RIGHT:         return "RIGHT";
                case (int)KeyPressedType.DOWN:          return "DOWN";
                case (int)KeyPressedType.PAY_PM6:       return "PAYMENT_6";
                case (int)KeyPressedType.PAY_PM7:       return "PAYMENT_7";
                case (int)KeyPressedType.PAY_PM8:       return "PAYMENT_8";
                case (int)KeyPressedType.PAY_PM9:       return "PAYMENT_9";
                default: return "- Unknown Key : KeyId=" + iKeyId;
            }
        }

        public static bool TransferTfiFile(string szFileName, ref string szErrMsg)
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ TransferTfiFile : File={szFileName}, enable={TCT_Lib.bEnableTfiScpFiles}, Host={szTfiScpHostName}, User={szTfiScpUserName}, Dest={szTfiScpDestPath}");
            if (!TCT_Lib.bEnableTfiScpFiles)
            {
                LogInfo.WriteLog("- TFI data image is disable setting");
                szErrMsg = "TFI image transfer is disable setting";
                return false;
            }
            //------------------------------------------------------------
            try
            {
                using (ScpClient client = new ScpClient(szTfiScpHostName, szTfiScpUserName, szTfiScpPassword))
                {
                    try
                    {
                        var swScp = System.Diagnostics.Stopwatch.StartNew();
                        client.Connect();
                        LogInfo.WriteLog(LogLevel.DEBUG, $"+ TransferTfiFile : SCP connected ({swScp.ElapsedMilliseconds} ms)");
                        string szTfiImagePath   = Path.Combine(TCT_Lib.szLogPath, "TfiImage");
                        string szTfiImageFile   = Path.Combine(szTfiImagePath, szFileName);
                        using (Stream localFile = File.OpenRead(szTfiImageFile))
                        {
                            client.Upload(localFile, Path.Combine(szTfiScpDestPath, szFileName));
                        }
                        LogInfo.WriteLog(LogLevel.DEBUG, $"+ TransferTfiFile : uploaded {szFileName} ({swScp.ElapsedMilliseconds} ms)");
                        client.Disconnect();
                        client.Dispose();
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine("- Error Transfer TFI File : " + err.Message);
                        LogInfo.WriteLog("- Error Transfer TFI File : " + err.Message);
                        LogInfo.WriteLog(LogLevel.DEBUG, $"- TransferTfiFile : File={szFileName}, stack={err.StackTrace}");
                        szErrMsg = err.Message;
                        return false;
                    }
                }

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error new Transfer TFI File : "+err.Message);
                LogInfo.WriteLog("- Error new Transfer TFI File : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, $"- TransferTfiFile : stack={err.StackTrace}");
                szErrMsg = err.Message;
                return false;
            }
        }


    }
}
