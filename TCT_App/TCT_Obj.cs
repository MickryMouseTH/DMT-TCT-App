using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using TCT_App.RabbitMQ;

namespace TCT_App
{
    public class LaneTransaction
    {
        public string jsonrpc { get; set; }
        public string method { get; set; }
        public Params @params { get; set; }
    }

    public class Params
    {
        public Location location { get; set; }
        public string laneType { get; set; }
        public string laneMode { get; set; }
        public string albMode { get; set; }
        public string direction { get; set; }
        public Origin origin { get; set; }
        public long sequenceNumber { get; set; }
        public long seqValidPayment { get; set; }
        public long seqDetection { get; set; }
        public long seqEnforced { get; set; }
        public long seqLcTransaction { get; set; }
        public long seqVideoPicture { get; set; }
        public Vehicle vehicle { get; set; }
        public string signalCode { get; set; }
        public long signalCodeBitmap { get; set; }
        public Payment[] payment { get; set; }
        public object[] obuTransaction { get; set; }
        public object[] image { get; set; }
        public DetectionEvent[] detectionEvent { get; set; }
        public LaneTransMessage laneTransMessage { get; set; }
    }

    public class DetectionEvent
    {
        public long vehicleId { get; set; }
        public string eventType { get; set; }
        public string direction { get; set; }
        public string eventTime { get; set; }
        public string sensorType { get; set; }
        public string detectionPoint { get; set; }
    }

    public class Location
    {
        public string country { get; set; }
        public long provider { get; set; }
        public string plaza { get; set; }
        public string stationType { get; set; }
        public long lane { get; set; }
    }

    public class Origin
    {
        public string device { get; set; }
        public string version { get; set; }
        public string loginRecordId { get; set; }
        public string timestamp { get; set; }
    }

    public class Payment
    {
        public Origin origin { get; set; }
        public ManualClassification manualClassification { get; set; }
        public ManualPayment manualPayment { get; set; }
    }

    public class ManualClassification
    {
        public long vehicleId { get; set; }
        public long vehicleClass { get; set; }
        public long fare { get; set; }
    }

    public class ManualPayment
    {
        public string transactionId { get; set; }
        public string type { get; set; }
        public string method { get; set; }
        public long amount { get; set; }
        public string reversalTransactionId { get; set; }
        public string authorization { get; set; }
    }

    public class Vehicle
    {
        public string entryTime { get; set; }
        public string exitTime { get; set; }
        public string passageTime { get; set; }
        public AutomaticClassification automaticClassification { get; set; }
    }

    public class AutomaticClassification
    {
        public long vehicleId { get; set; }
        public long avcClass { get; set; }
        public string direction { get; set; }
        public long avcAxles { get; set; }
        public long avcWheels { get; set; }
        public bool dualWheels { get; set; }
        public bool heightSensor { get; set; }
        public long sensorState { get; set; }
    }

    //====================================================================
    public class LaneEventHQ
    {
        public long eventId;
        public string eventDateTime;
        public Plaza plaza;
        public Lane lane;
        public Staff staff;
        public Supervisor supervisor;
        public Event Event;
    }

    public class Plaza
    {
        public string plazaId;
        public string plazaName;
    }

    public class Lane
    {
        public int laneId;
        public string laneName;
        public int laneType;
        public string laneTypeName;
        public int laneMode;
        public string laneModeName;
        public int laneDirection;
        public string laneDirectionName;
    }

    public class Staff
    {
        public int staffShiftId;
        public string staffId;
        //public string staffCode;
        public string staffName;
        public string startShiftDateTime;
    }

    public class Supervisor
    {
        public int supervisorShiftId;
        public string supervisorId;
        //public string supervisorCode;
        public string supervisorName;
        public string startShiftDateTime;
    }

    public class Event
    {
        public int eventType;
        public string eventTypeName;
        public int eventState;
        public string eventStateName;
        public string eventRemark;
    }

    public class Alarm
    {
        public int alarmDevice;
        public string alarmDeviceName;
        public int alarmType;
        public string alarmTypeName;
        public string alarmDetail;
        public string alarmRemark;
    }

    //========================================================================
    public class LaneTransMessage
    {
        public int transactionId;
        public string transactionDateTime;
        public int jobNo;
        public int nTrx;
        public Plaza plaza;
        public Lane lane;
        public Staff staff;
        public Supervisor supervisor;
        public int tcClass;
        public AVC_Info avcInfo;        // Del
        public int fareAmount;
        public Pay_Info payInfo;        // Del
        public TAG_Info tagInfo;        // Del
        public EDC_Info edcInfo;
        public DMTCard_Info dmtCardInfo;
        public string receiptNo;
        public string transRemark;
        public int isTowing;
        public int towingParentId;
        public int sumCurrentVehicles;
        public MtcPassageSignal mtcPassageSignal;
        public SensorStatus sensorStatus;
        public CaptureImage captureImage;
    }

    public class AVC_Info
    {
        public int avcClass;
        public int avcAxle;
        public int avcWheel;
        public int avcDualWheel;
        public int avcDirection;
        public int avcError;
    }

    public class Pay_Info
    {
        public int payType;
        public string payTypeName;
        public decimal payAmount;
        public string payDateTime;
        public string payRemark;
        public int emvDetailID;
    }

    public class TAG_Info
    {
        public int tagType;        // 0=M-Card, 1=Easy-Pass
        public string tagPAN;
        public decimal tagBalance;
        public string tagSignalCode;
        public string tagRemark;
    }

    public class EDC_Info
    {
        //--- EMV Type = 0
        public string responseMessage;
        public string approveCode;
        public string cardNo;
        public string terminalId;
        public string transDate;
        public string transTime;
        public string transAmount;
        public string transRef;     // ref3
        public string invoiceNo;
        public string ref1;
        public string ref2;
        public string ref3;
    }

    public class QRPay_Info
    {
        //EMV Type = 1
    }

    public class EMV_Info
    {
        public string requestText;
        public string responseText;
        public string responseMessage;
        public string respApproveCode;
        public string respCardNo;
        public string respTerminalId;
        public string respDate;
        public string respTime;
        public string respAmount;
        public string respRef;
        public string respInvoiceNo;
        public string respRef1;
        public string respRef2;
        public string respRef3;
    }

    public class DMTCard_Info
    {
        public string smartCardNo;
        public string carNo;
        public string licensePlate;
        public int vehicleClass;
    }

    //------------------------------------------------------------------------------------
    public class DeviceStatus
    {
        public int deviceType;
        public string deviceName;
        public int deviceState;
    }

    public class LaneEventRTO
    {
        public long eventId;
        public string eventDateTime;
        public Plaza plaza;
        public Lane lane;
        public Staff staff;
        public Supervisor supervisor;
        public Event Event;
        public DeviceStatus deviceStatus;
    }

    public class LaneAlarmRTO
    {
        public long alarmId;
        public string alarmDateTime;
        public Plaza plaza;
        public Lane lane;
        public Staff staff;
        public Supervisor supervisor;
        public Alarm alarm;
    }

    public class BeginOfJobJson
    {
        public string jsonrpc { get; set; }
        public string method { get; set; }
        public BeginOfJob @params { get; set; }
    }

    public class BeginOfJob
    {
        public Plaza plaza;
        public Lane lane;
        public Staff staff;
        public Supervisor supervisor;
        public string bojDateTime;
        public int jobNo;
        public string jobType;      // TC=Normal Mode, MT=MaintenanceMode
        public int manualLogin;     // 0=Touch card login, 1=Manual input login
        public int closeViolation;
    }

    public class EndOfJobJson
    {
        public string jsonrpc { get; set; }
        public string method { get; set; }
        public EndOfJob @params { get; set; }
    }

    public class EndOfJob
    {
        public int staffShiftId;
        public Plaza plaza;
        public Lane lane;
        public Staff staff;
        public Supervisor supervisor;
        public string bojDateTime;
        public string eojDateTime;
        public int jobNo;
        public int eojMethod;       // 0=Normal Manual, 1=Auto
        public string jobType;      // TC=Normal Mode, MT=MaintenanceMode
        //public TransactionStatistic transStatistic;
    }

    public class OtlStatusJson
    {
        public string jsonrpc { get; set; }
        public string method { get; set; }
        public OtlStatus @params { get; set; }
    }

    public class OtlStatus
    {
        public Plaza plaza;
        public Lane lane;
        public Staff staff;
        public Supervisor supervisor;
        public bool otlStatus;
        public string otlChangeStatusDateTime;
    }

    public class TransactionStatistic
    {
        public int totalSumVehicles;
        public int totalTransViolation;
        public int totalTransExempt;
        public int totalTransDIS;
        public int totalTransTowing;
        public int totalTransHPMC;
        public int totalTransLeftExit;
        public ClassTypeStatistic totalTransClassType1;
        public ClassTypeStatistic totalTransClassType2;
        public AvcStatistic totalTransAVC;
        public MisStatistic totalMIS;
        public SalesCouponStatistic totalSalesCoupon;
    }

    public class ClassTypeStatistic
    {
        public int totalTransaction;
        public int totalTransCash;
        public decimal totalFareCash;
        public int totalTransCoupon;
        public decimal totalFareCoupon;
        public int totalTransEMV;
        public decimal totalFareEMV;
        public int totalTransQrCode;
        public decimal totalFareQrCode;
        public int totalTransDMT;
    }

    public class AvcStatistic
    {
        public int totalAvcClass1;
        public int totalAvcClass2;
        public int totalAvcClass5;
        public int totalAvcClass255;
        public int totalAvcClassOther;
    }

    public class MisStatistic
    {
        public int totalMIS;
        public int totalClassTypeMIS;
        public int totalPayTypeMIS;
    }

    public class SalesCouponStatistic
    {
        public int totalCouponType1;
        public decimal totalAmountType1;
        public int totalCouponType2;
        public decimal totalAmountType2;
    }

    public class MtcPassageSignal
    {
        public int rePayment;
        public int reClassified;
        public int reversing;
        public int simuPass;
        public int imageTaken;
        public int classDiscrepancy;
        public int towingVehicle;
        public int convoyPayment;
    }

    public class SensorStatus
    {
        public int loopStatus;
        public int obStatus;
        public int avcStatus;
    }

    public class RealTimeState
    {
        public int emvState;
        public int lsdState;
        public int mainAntennaState;
        public int secondaryAntennaState;
        public int lcioState;       // Lane IO App
    }

    public class MtsDataJson
    {
        public string jsonrpc { get; set; }
        public string method { get; set; }
        public ObjMtsData @params { get; set; }
    }

    public class MtsTransJson
    {
        public string jsonrpc { get; set; }
        public string method { get; set; }
        public ObjMtsTrans @params { get; set; }
    }

    public class CaptureImage
    {
        public string imageFileName1;
        public string imageFileName2;
        public string imageFileName3;
    }

    /*
        public class TransSumVehicles
        {
            public int ShiftTotalSum;
            public int TotalTransViolation;
            public int TotalTransExempt;
            public int TotalTransDIS;
            public int TotalTransTowing;
            public int TotalTransHPMC;
            public int TotalClass1_TransCash;
            public decimal TotalClass1_FareCash;
            public int TotalClass1_TransCoupon;
            public decimal TotalClass1_FareCoupon;
            public int TotalClass1_TransEMV;
            public decimal TotalClass1_FareEMV;
            public int TotalClass1_TransQrCode;
            public decimal TotalClass1_FareQrCode;
            public int TotalClass1_TransDMT;
            public int TotalClass1_TransLeftOut;
            public int TotalClass2_TransCash;
            public decimal TotalClass2_FareCash;
            public int TotalClass2_TransCoupon;
            public decimal TotalClass2_FareCoupon;
            public int TotalClass2_TransEMV;
            public decimal TotalClass2_FareEMV;
            public int TotalClass2_TransQrCode;
            public decimal TotalClass2_FareQrCode;
            public int TotalClass2_TransDMT;
            public int TotalClass2_TransLeftOut;
            public int TotalAVC_Class1;
            public int TotalAVC_Class2;
            public int TotalAVC_Class5;
            public int TotalAVC_Class255;
            public int TotalAVC_ClassOther;
            public int TotalMIS;
            public int TotalMIS_ClassType;
            public int TotalMIS_PayType;
        }
    */

    //========================================================================
    public class ImageCameraMessage
    {
        public string requestId;
        public string requestDateTime;
        public Plaza plaza;
        public RabbitMQ.Lane lane;
        public Transaction transaction;
        public List<ImageData> captureImages;
    }

    public class Transaction
    {
        public int jobNo;
        public int nTrx;
        public string transactionDateTime;
        public string direction;
    }

    public class ImageData
    {
        public int cameraIndex;
        public string fileName;
        public string imageData;
    }
    //-------------------------------------------------------------------------------------
    public class TCT_Obj
    {
        public static LaneTransaction GenerateLaneTransaction(int iLaneTransID)
        {
            string szQuery = @"SELECT * FROM LaneTransaction " +
                                $"WHERE TransactionID={iLaneTransID}";
            LogInfo.WriteLog(LogLevel.TRACE, $"+ GenerateLaneTransaction : SQL={szQuery}");
            DataTable dtLaneTrans = TCT_Lib.LaneDB.List(szQuery);
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ GenerateLaneTransaction : TransID={iLaneTransID}, rows={dtLaneTrans?.Rows.Count ?? -1}");
            if (dtLaneTrans.Rows.Count == 0)
            {
                LogInfo.WriteLog($"- Fail GenerateLaneTransaction : Not found LaneTransaction in database : LaneTransactionnID={iLaneTransID}");
                return null;
            }

            var InvC                    = CultureInfo.InvariantCulture;

            var xLaneTrans              = new LaneTransaction();
            xLaneTrans.jsonrpc          = "2.0";
            xLaneTrans.method           = "CompiledPassageRecord";
            var xParams                 = new Params();

            var location                = new Location();
            location.country            = "TH";
            location.provider           = TCT_Lib.iProviderId;
            location.plaza              = TCT_Lib.szPlazaID;
            location.stationType        = "OPEN_STATION";
            location.lane               = TCT_Lib.iLaneID;
            xParams.location            = location;

            //LaneMode xLaneMode = TCT_Lib.eCurLaneMode;
            //if (TCT_Lib.bMaintenanceMode && TCT_Lib.eCurLaneMode == LaneMode.HPMC)
            //    xLaneMode = LaneMode.MAINTENANCE;

            //xParams.laneType            = GetLaneTypeName(TCT_Lib.eCurLaneType);
            //xParams.laneMode            = GetLaneModeName(xLaneMode);
            //xParams.direction             = GetLaneDirection(TCT_Lib.eLaneDirection);

            xParams.laneType            = GetLaneTypeName(int.Parse(dtLaneTrans.Rows[0]["LaneType"].ToString()));
            xParams.laneMode            = GetLaneModeName(int.Parse(dtLaneTrans.Rows[0]["LaneMode"].ToString()));
            xParams.albMode             = "ALB_NORMAL";
            xParams.direction           = GetLaneDirection(int.Parse(dtLaneTrans.Rows[0]["LaneDirection"].ToString()));
            xParams.sequenceNumber      = iLaneTransID;

            var origin                  = new Origin();
            origin.device               = "TCT";
            origin.version              = TCT_Lib.szAppVersion;
            //origin.loginRecordId        = TCT_Lib.szStaffCode;
            //origin.timestamp            = TCT_Lib.DateTime_ISO8601(TCT_Lib.dtStaffStartShift);

            origin.loginRecordId        = dtLaneTrans.Rows[0]["StaffCode"].ToString();
            origin.timestamp            = TCT_Lib.DateTime_ISO8601(Convert.ToDateTime(dtLaneTrans.Rows[0]["StaffStartShiftDateTime"]));

            xParams.origin              = origin;

            var vehicle                         = new Vehicle();
            vehicle.passageTime                 = TCT_Lib.DateTime_ISO8601(Convert.ToDateTime(dtLaneTrans.Rows[0]["TransactionDateTime"]));
            var automaticClassification         = new AutomaticClassification();
            automaticClassification.avcClass    = int.Parse(dtLaneTrans.Rows[0]["AVC_Class"].ToString());
            automaticClassification.direction   = "NORMAL_DIRECTION";
            automaticClassification.avcAxles    = int.Parse(dtLaneTrans.Rows[0]["AVC_Axle"].ToString());
            automaticClassification.avcWheels   = int.Parse(dtLaneTrans.Rows[0]["AVC_Wheel"].ToString());
            automaticClassification.dualWheels  = int.Parse(dtLaneTrans.Rows[0]["AVC_DualWheel"].ToString()) > 0;
            automaticClassification.heightSensor = false;
            vehicle.automaticClassification     = automaticClassification;
            xParams.vehicle                     = vehicle;

            xParams.signalCode          = dtLaneTrans.Rows[0]["TAG_SignalCode"].ToString() == "" ? "01" : dtLaneTrans.Rows[0]["TAG_SignalCode"].ToString();
            xParams.signalCodeBitmap    = 1;

            var payment                 = new Payment();
            payment.origin              = origin;
            var manualClass             = new ManualClassification();
            manualClass.vehicleId       = 0;
            manualClass.vehicleClass    = int.Parse(dtLaneTrans.Rows[0]["TC_Class"].ToString()) > 2 ? 0 : int.Parse(dtLaneTrans.Rows[0]["TC_Class"].ToString());
            manualClass.fare            = long.Parse(dtLaneTrans.Rows[0]["FareAmount"] + "00");
            payment.manualClassification = manualClass;

            var manualPayment           = new ManualPayment();
            manualPayment.transactionId = iLaneTransID.ToString();
            manualPayment.type          = "PAYMENT_COLLECTED";
            manualPayment.method        = TCT_Lib.GetPayMethodNameRabbitMQ(int.Parse(dtLaneTrans.Rows[0]["PayType"].ToString()));
            manualPayment.amount        = long.Parse(dtLaneTrans.Rows[0]["PayAmount"] + "00");
            manualPayment.authorization = "";       // Use in case of EMV or Credit Card
            payment.manualPayment       = manualPayment;

            //xParams.obuTransaction      = 

            xParams.payment             = new Payment[1];
            xParams.payment[0]          = payment;

            xParams.laneTransMessage    = GenerateLaneTransMessage(iLaneTransID);

            xLaneTrans.@params          = xParams;

            LogInfo.WriteLog(LogLevel.DEBUG, $"+ GenerateLaneTransaction : built TransID={iLaneTransID}, laneMode={xParams.laneMode}, vehicleClass={manualClass.vehicleClass}, fare={manualClass.fare}, method={manualPayment.method}, amount={manualPayment.amount}, laneTransMessage={(xParams.laneTransMessage == null ? "null" : "ok")}");
            return xLaneTrans;
        }

        static string GetLaneTypeName(LaneType laneType)
        {
            if (laneType == LaneType.MTC)
                return "MTC_LANE";
            else if (laneType == LaneType.ETC)
                return "ETC_LANE";
            else if (laneType == LaneType.DUAL)
                return "DUAL_LANE";
            else
                return "UNKNOWN";
        }

        static string GetLaneTypeName(int laneTypeId)
        {
            if (laneTypeId == 0)
                return "MTC_LANE";
            else if (laneTypeId == 1)
                return "ETC_LANE";
            else if (laneTypeId == 2)
                return "DUAL_LANE";
            else
                return "UNKNOWN";
        }

        static string GetLaneModeName(LaneMode laneMode)
        {
            if (laneMode == LaneMode.CLOSED)
                return "CLOSED_MODE";
            else if (laneMode == LaneMode.NORMAL)
                return "NORMAL_MODE";
            else if (laneMode == LaneMode.HPMC)
                return "HPMC_MODE";
            else if (laneMode == LaneMode.MAINTENANCE)
                return "MAINTENANCE_MODE";
            else if (laneMode == LaneMode.FREE)
                return "FREE_MODE";         // v2.0.6
            else
                return "UNKNOWN";
        }

        static string GetLaneModeName(int laneModeId)
        {
            if (laneModeId == 0)
                return "CLOSED_MODE";
            else if (laneModeId == 1)
                return "NORMAL_MODE";
            else if (laneModeId == 2)
                return "HPMC_MODE";
            else if (laneModeId == 3)
                return "MAINTENANCE_MODE";
            else if (laneModeId == (int)LaneMode.FREE)
                return "FREE_MODE";         // v2.0.6
            else
                return "UNKNOWN";
        }

        static string GetLaneDirection(LaneDirection laneDirection)
        {
            if (laneDirection == LaneDirection.NORMAL)
                return "NORMAL_DIRECTION";
            else if (laneDirection == LaneDirection.REVERSE)
                return "REVERSE_DIRECTION";
            else
                return "UNKNOWN";
        }

        static string GetLaneDirection(int laneDirectionId)
        {
            if (laneDirectionId == 0)
                return "NORMAL_DIRECTION";
            else if (laneDirectionId == 1)
                return "REVERSE_DIRECTION";
            else
                return "UNKNOWN";
        }
        //-------------------------------------------------------------------------------------------------------------------------------------

        public static LaneTransMessage GenerateLaneTransMessage(int iTransID)
        {
            string szQuery = "SELECT * FROM LaneTransaction " +
                                $"WHERE TransactionID={iTransID}";
            LogInfo.WriteLog(LogLevel.TRACE, $"+ GenerateLaneTransMessage : SQL={szQuery}");
            DataTable dtLaneTrans = TCT_Lib.LaneDB.List(szQuery);
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ GenerateLaneTransMessage : TransID={iTransID}, rows={dtLaneTrans?.Rows.Count ?? -1}");
            if (dtLaneTrans.Rows.Count == 0)
            {
                LogInfo.WriteLog($"- Fail GenerateLaneTransMessage : Not found LaneTransaction in database : TransactionID={iTransID}");
                return null;
            }

            try
            {
                var xLaneTrans                  = new LaneTransMessage();
                xLaneTrans.transactionId        = iTransID;
                xLaneTrans.transactionDateTime  = TCT_Lib.DateTime_ISO8601(Convert.ToDateTime(dtLaneTrans.Rows[0]["TransactionDateTime"]));

                var xPlaza                      = new Plaza();
                xPlaza.plazaId                  = dtLaneTrans.Rows[0]["PlazaID"].ToString();
                xPlaza.plazaName                = dtLaneTrans.Rows[0]["PlazaName"] == DBNull.Value ? "" : dtLaneTrans.Rows[0]["PlazaName"].ToString();
                xLaneTrans.plaza                = xPlaza;

                var xLane                       = new Lane();
                xLane.laneId                    = int.Parse(dtLaneTrans.Rows[0]["LaneID"].ToString());
                xLane.laneName                  = dtLaneTrans.Rows[0]["LaneName"] == DBNull.Value ? "" : dtLaneTrans.Rows[0]["LaneName"].ToString();
                xLane.laneType                  = int.Parse(dtLaneTrans.Rows[0]["LaneType"].ToString());
                xLane.laneTypeName              = dtLaneTrans.Rows[0]["LaneTypeName"] == DBNull.Value ? "" : dtLaneTrans.Rows[0]["LaneTypeName"].ToString();
                xLane.laneMode                  = int.Parse(dtLaneTrans.Rows[0]["LaneMode"].ToString());
                xLane.laneModeName              = dtLaneTrans.Rows[0]["LaneModeName"] == DBNull.Value ? "" : dtLaneTrans.Rows[0]["LaneModeName"].ToString();
                xLane.laneDirection             = int.Parse(dtLaneTrans.Rows[0]["LaneDirection"].ToString());
                xLane.laneDirectionName         = dtLaneTrans.Rows[0]["LaneDirectionName"] == DBNull.Value ? "" : dtLaneTrans.Rows[0]["LaneDirectionName"].ToString();
                xLaneTrans.lane                 = xLane;

                var xStaff                      = new Staff();
                xStaff.staffShiftId             = int.Parse(dtLaneTrans.Rows[0]["StaffShiftID"].ToString());
                xStaff.staffId                  = dtLaneTrans.Rows[0]["StaffID"].ToString();
                //xStaff.staffCode            = dtLaneTrans.Rows[0]["StaffCode"] == DBNull.Value ? "" : dtLaneTrans.Rows[0]["StaffCode"].ToString();
                xStaff.staffName                = dtLaneTrans.Rows[0]["StaffName"] == DBNull.Value ? "" : dtLaneTrans.Rows[0]["StaffName"].ToString();
                xStaff.startShiftDateTime       = TCT_Lib.DateTime_ISO8601(Convert.ToDateTime(dtLaneTrans.Rows[0]["StaffStartShiftDateTime"]));
                xLaneTrans.staff                = xStaff;

                var xSupervisor                 = new Supervisor();
                xSupervisor.supervisorShiftId   = int.Parse(dtLaneTrans.Rows[0]["SupervisorShiftID"].ToString());
                xSupervisor.supervisorId        = dtLaneTrans.Rows[0]["SupervisorID"].ToString();
                //xSupervisor.supervisorCode  = dtLaneTrans.Rows[0]["SupervisorCode"] == DBNull.Value ? "" : dtLaneTrans.Rows[0]["SupervisorCode"].ToString();
                xSupervisor.supervisorName      = dtLaneTrans.Rows[0]["SupervisorName"] == DBNull.Value ? "" : dtLaneTrans.Rows[0]["SupervisorName"].ToString();
                xSupervisor.startShiftDateTime  = dtLaneTrans.Rows[0]["SupervisorStartShiftDateTime"] == DBNull.Value 
                                                        ? "" : TCT_Lib.DateTime_ISO8601(Convert.ToDateTime(dtLaneTrans.Rows[0]["SupervisorStartShiftDateTime"]));
                xLaneTrans.supervisor           = xSupervisor;

                xLaneTrans.tcClass              = int.Parse(dtLaneTrans.Rows[0]["TC_Class"].ToString());
                xLaneTrans.fareAmount           = int.Parse(dtLaneTrans.Rows[0]["FareAmount"] + "00");
                xLaneTrans.transRemark          = dtLaneTrans.Rows[0]["TransRemark"] == DBNull.Value ? "" : dtLaneTrans.Rows[0]["TransRemark"].ToString();

//-- Start
                var xAVC                        = new AVC_Info();
                xAVC.avcClass                   = int.Parse(dtLaneTrans.Rows[0]["AVC_Class"].ToString());
                xAVC.avcAxle                    = int.Parse(dtLaneTrans.Rows[0]["AVC_Axle"].ToString());
                xAVC.avcWheel                   = int.Parse(dtLaneTrans.Rows[0]["AVC_Wheel"].ToString());
                xAVC.avcDualWheel               = int.Parse(dtLaneTrans.Rows[0]["AVC_DualWheel"].ToString());
                xAVC.avcDirection               = int.Parse(dtLaneTrans.Rows[0]["AVC_Direction"].ToString());
                xAVC.avcError                   = 0;
                string szLC_SignalStatus        = dtLaneTrans.Rows[0]["LC_SignalStatus"].ToString();
                string[] szAryLcStatus          = szLC_SignalStatus.Split(',');
                if (szAryLcStatus.Length > 3)
                    xAVC.avcError = int.Parse(szAryLcStatus[3]);
                xLaneTrans.avcInfo              = xAVC;

                var xPayment                    = new Pay_Info();
                xPayment.payType                = int.Parse(dtLaneTrans.Rows[0]["PayType"].ToString());
                xPayment.payTypeName            = dtLaneTrans.Rows[0]["PayTypeName"].ToString();
                xPayment.payAmount              = decimal.Parse(dtLaneTrans.Rows[0]["PayAmount"] + "00");
                xPayment.payDateTime            = TCT_Lib.DateTime_ISO8601(Convert.ToDateTime(dtLaneTrans.Rows[0]["PayDateTime"]));
                xPayment.payRemark              = dtLaneTrans.Rows[0]["PayRemark"] == DBNull.Value ? "" : dtLaneTrans.Rows[0]["PayRemark"].ToString();
                xLaneTrans.payInfo              = xPayment;

                var xTAG                        = new TAG_Info();
                xTAG.tagType                    = int.Parse(dtLaneTrans.Rows[0]["TAG_Type"].ToString());
                xTAG.tagPAN                     = dtLaneTrans.Rows[0]["TAG_PAN"] == DBNull.Value ? "" : dtLaneTrans.Rows[0]["TAG_PAN"].ToString();
                xTAG.tagBalance                 = decimal.Parse(dtLaneTrans.Rows[0]["TAG_Balance"].ToString());
                xTAG.tagSignalCode              = dtLaneTrans.Rows[0]["TAG_SignalCode"] == DBNull.Value ? "" : dtLaneTrans.Rows[0]["TAG_SignalCode"].ToString();
                xTAG.tagRemark                  = dtLaneTrans.Rows[0]["TAG_Remark"] == DBNull.Value ? "" : dtLaneTrans.Rows[0]["TAG_Remark"].ToString();
                xLaneTrans.tagInfo              = xTAG;
//-- End

                xLaneTrans.receiptNo            = dtLaneTrans.Rows[0]["ReceiptNo"] == DBNull.Value ? "" : dtLaneTrans.Rows[0]["ReceiptNo"].ToString();
                xLaneTrans.jobNo                = int.Parse(dtLaneTrans.Rows[0]["JobNo"].ToString());
                xLaneTrans.nTrx                 = int.Parse(dtLaneTrans.Rows[0]["nTrx"].ToString());
                xLaneTrans.isTowing             = int.Parse(dtLaneTrans.Rows[0]["IsTowing"].ToString());
                xLaneTrans.towingParentId       = int.Parse(dtLaneTrans.Rows[0]["TowingParentID"].ToString());

                szQuery = "SELECT TotalSumVehicles FROM TransactionSummary " +
                            $"WHERE TransactionDate='{DateTime.Today.ToString(TCT_Lib.szDateFormatDB)}'";
                DataTable dtTransSum = TCT_Lib.LaneDB.List(szQuery);
                xLaneTrans.sumCurrentVehicles = dtTransSum.Rows.Count == 0 ? 0 : int.Parse(dtTransSum.Rows[0][0].ToString());

                int iEmvDetailID = int.Parse(dtLaneTrans.Rows[0]["EMV_DetailID"].ToString());
                if (iEmvDetailID > 0)
                {
                    //--- Get EDC Info
                    szQuery = "SELECT * FROM EmvDetail " +
                                $"WHERE EmvDetailID={iEmvDetailID}";
                    LogInfo.WriteLog(LogLevel.TRACE, $"+ GenerateLaneTransMessage : SQL={szQuery}");
                    DataTable dtEmv = TCT_Lib.LaneDB.List(szQuery);
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ GenerateLaneTransMessage : EmvDetailID={iEmvDetailID}, rows={dtEmv?.Rows.Count ?? -1}");
                    if (dtEmv.Rows.Count == 0)
                    {
                        LogInfo.WriteLog($"- Fail Not found EmvDetail in database : EmvDetailID={iEmvDetailID}");
                    }
                    else
                    {
                        xLaneTrans.edcInfo                  = new EDC_Info();
                        xLaneTrans.edcInfo.responseMessage  = dtEmv.Rows[0]["ResponseMsg"] == DBNull.Value ? "" : dtEmv.Rows[0]["ResponseMsg"].ToString().Replace("\r", "").Replace("\n", "").Trim();
                        xLaneTrans.edcInfo.approveCode      = dtEmv.Rows[0]["ResponseApproveCode"] == DBNull.Value ? "" : dtEmv.Rows[0]["ResponseApproveCode"].ToString();
                        xLaneTrans.edcInfo.cardNo           = dtEmv.Rows[0]["ResponseCardNo"] == DBNull.Value ? "" : dtEmv.Rows[0]["ResponseCardNo"].ToString();
                        xLaneTrans.edcInfo.transDate        = dtEmv.Rows[0]["ResponseDate"] == DBNull.Value ? "" : dtEmv.Rows[0]["ResponseDate"].ToString();
                        xLaneTrans.edcInfo.transTime        = dtEmv.Rows[0]["ResponseTime"] == DBNull.Value ? "" : dtEmv.Rows[0]["ResponseTime"].ToString();
                        xLaneTrans.edcInfo.terminalId       = dtEmv.Rows[0]["ResponseTerminalID"] == DBNull.Value ? "" : dtEmv.Rows[0]["ResponseTerminalID"].ToString();
                        xLaneTrans.edcInfo.transAmount      = dtEmv.Rows[0]["ResponseAmount"] == DBNull.Value ? "" : dtEmv.Rows[0]["ResponseAmount"].ToString();
                        xLaneTrans.edcInfo.transRef         = dtEmv.Rows[0]["ResponseRef3"] == DBNull.Value ? "" : dtEmv.Rows[0]["ResponseRef3"].ToString();
                        xLaneTrans.edcInfo.ref1             = dtEmv.Rows[0]["ResponseRef1"] == DBNull.Value ? "" : dtEmv.Rows[0]["ResponseRef1"].ToString();
                        xLaneTrans.edcInfo.ref2             = dtEmv.Rows[0]["ResponseRef2"] == DBNull.Value ? "" : dtEmv.Rows[0]["ResponseRef2"].ToString();
                        xLaneTrans.edcInfo.ref3             = dtEmv.Rows[0]["ResponseRef3"] == DBNull.Value ? "" : dtEmv.Rows[0]["ResponseRef3"].ToString();
                    }
                }

                int iDmtCardTransID     = int.Parse(dtLaneTrans.Rows[0]["DmtCardTransID"].ToString());
                if (iDmtCardTransID > 0)
                {
                    //--- Get DmtCard Info
                    szQuery = "SELECT * FROM DmtCardTrans "+
                                $"WHERE DmtCardTransID={iDmtCardTransID}";
                    LogInfo.WriteLog(LogLevel.TRACE, $"+ GenerateLaneTransMessage : SQL={szQuery}");
                    DataTable dtDmtCard = TCT_Lib.LaneDB.List(szQuery);
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ GenerateLaneTransMessage : DmtCardTransID={iDmtCardTransID}, rows={dtDmtCard?.Rows.Count ?? -1}");
                    if (dtDmtCard.Rows.Count == 0)
                    {
                        LogInfo.WriteLog($"- Fail Not found DmtCardTrans in database : DmtCardTransID={iDmtCardTransID}");
                    }
                    else
                    {
                        xLaneTrans.dmtCardInfo              = new DMTCard_Info();
                        xLaneTrans.dmtCardInfo.smartCardNo  = dtDmtCard.Rows[0]["CardSerialNo"] == DBNull.Value ? "" : dtDmtCard.Rows[0]["CardSerialNo"].ToString().Trim();
                        xLaneTrans.dmtCardInfo.carNo        = dtDmtCard.Rows[0]["DmtCarNo"] == DBNull.Value ? "" : dtDmtCard.Rows[0]["DmtCarNo"].ToString();
                        xLaneTrans.dmtCardInfo.licensePlate = dtDmtCard.Rows[0]["DmtLicensePlate"] == DBNull.Value ? "" : dtDmtCard.Rows[0]["DmtLicensePlate"].ToString();
                        xLaneTrans.dmtCardInfo.vehicleClass = dtDmtCard.Rows[0]["VehicleClass"] == DBNull.Value ? 0 : int.Parse(dtDmtCard.Rows[0]["VehicleClass"].ToString());
                    }
                }

                string szTransSignalCode        = dtLaneTrans.Rows[0]["Trans_SignalCode"] == DBNull.Value ? "" : dtLaneTrans.Rows[0]["Trans_SignalCode"].ToString();
                var mtcPassageSignal            = new MtcPassageSignal();
                if (szTransSignalCode.Trim() == "")
                {
                    mtcPassageSignal.rePayment          = 0;
                    mtcPassageSignal.reClassified       = 0;
                    mtcPassageSignal.reversing          = 0;
                    mtcPassageSignal.simuPass           = 0;
                    mtcPassageSignal.imageTaken         = 0;
                    mtcPassageSignal.classDiscrepancy   = 0;
                    mtcPassageSignal.towingVehicle      = 0;
                    mtcPassageSignal.convoyPayment      = 0;
                }
                else
                {
                    string[] szArySignalCode            = szTransSignalCode.Split(',');
                    mtcPassageSignal.rePayment          = int.Parse(szArySignalCode[0]);
                    mtcPassageSignal.reClassified       = int.Parse(szArySignalCode[1]);
                    mtcPassageSignal.reversing          = int.Parse(szArySignalCode[2]);
                    mtcPassageSignal.simuPass           = int.Parse(szArySignalCode[3]);
                    mtcPassageSignal.imageTaken         = int.Parse(szArySignalCode[4]);
                    mtcPassageSignal.classDiscrepancy   = int.Parse(szArySignalCode[5]);
                    mtcPassageSignal.towingVehicle      = int.Parse(szArySignalCode[6]);
                    mtcPassageSignal.convoyPayment      = int.Parse(szArySignalCode[7]);
                }
                xLaneTrans.mtcPassageSignal     = mtcPassageSignal;

                string szSensorStatus           = dtLaneTrans.Rows[0]["LC_SignalStatus"].ToString();
                string[] szArySensorStatus      = szSensorStatus.Split(',');
                var sensorStatus                = new SensorStatus();
                if (szSensorStatus == "")
                {
                    sensorStatus.loopStatus     = 1;
                    sensorStatus.obStatus       = 1;
                    sensorStatus.avcStatus      = 1;
                }
                else
                {
                    sensorStatus.loopStatus     = int.Parse(szArySensorStatus[0]);
                    sensorStatus.obStatus       = int.Parse(szArySensorStatus[1]);
                    sensorStatus.avcStatus      = int.Parse(szArySensorStatus[2]);
                }
                xLaneTrans.sensorStatus     = sensorStatus;

                string szImage1 = "", szImage2 = "", szImage3 = "";
                TCT_Lib.LaneDB.GetCaptureImageFileName(xLaneTrans.jobNo, xLaneTrans.nTrx, iTransID, ref szImage1, ref szImage2, ref szImage3);
                var captureImage            = new CaptureImage();
                captureImage.imageFileName1 = szImage1 ?? "";
                captureImage.imageFileName2 = szImage2 ?? "";
                captureImage.imageFileName3 = szImage3 ?? "";
                xLaneTrans.captureImage     = captureImage;

                LogInfo.WriteLog(LogLevel.DEBUG, $"+ GenerateLaneTransMessage : built TransID={iTransID}, JobNo={xLaneTrans.jobNo}, nTrx={xLaneTrans.nTrx}, StaffShiftID={xStaff.staffShiftId}, TC_Class={xLaneTrans.tcClass}, PayType={xPayment.payType}, PayAmount={xPayment.payAmount}, EmvDetailID={iEmvDetailID}, DmtCardTransID={iDmtCardTransID}, isTowing={xLaneTrans.isTowing}, images=[{captureImage.imageFileName1}|{captureImage.imageFileName2}|{captureImage.imageFileName3}]");
                return xLaneTrans;
            }
            catch (Exception err)
            {
                Console.WriteLine($"- Error GenerateLaneTransMessage : {err.Message}");
                LogInfo.WriteLog($"- Error GenerateLaneTransMessage : {err.Message}");
                LogInfo.WriteLog(LogLevel.DEBUG, $"- GenerateLaneTransMessage : TransID={iTransID}, stack={err.StackTrace}");
                return null;
            }
        }

        public static LaneEventHQ GenerateLaneEventHQ(long iLaneEventID)
        {
            string szQuery = @"SELECT * FROM LaneEventHQ " +
                                $"WHERE EventHqID={iLaneEventID}";
            LogInfo.WriteLog(LogLevel.TRACE, $"+ GenerateLaneEventHQ : SQL={szQuery}");
            DataTable dtLaneEvent = TCT_Lib.LaneDB.List(szQuery);
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ GenerateLaneEventHQ : EventHqID={iLaneEventID}, rows={dtLaneEvent?.Rows.Count ?? -1}");
            if (dtLaneEvent.Rows.Count == 0)
            {
                LogInfo.WriteLog($"- Fail GenerateLaneEventHQ : Not found LaneEventID in database : EventHqID={iLaneEventID}");
                return null;
            }

            try
            {
                var xLaneEvent              = new LaneEventHQ();
                xLaneEvent.eventId          = long.Parse(dtLaneEvent.Rows[0]["EventHqID"].ToString());
                xLaneEvent.eventDateTime    = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatMOC);

                var xPlaza                  = new Plaza();
                xPlaza.plazaId              = dtLaneEvent.Rows[0]["PlazaID"].ToString();
                xPlaza.plazaName            = dtLaneEvent.Rows[0]["PlazaName"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["PlazaName"].ToString();
                xLaneEvent.plaza            = xPlaza;

                var xLane                   = new Lane();
                xLane.laneId                = int.Parse(dtLaneEvent.Rows[0]["LaneID"].ToString());
                xLane.laneName              = dtLaneEvent.Rows[0]["LaneName"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["LaneName"].ToString();
                xLane.laneType              = int.Parse(dtLaneEvent.Rows[0]["LaneType"].ToString());
                xLane.laneTypeName          = dtLaneEvent.Rows[0]["LaneTypeName"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["LaneTypeName"].ToString();
                xLaneEvent.lane             = xLane;

                var xStaff                  = new Staff();
                xStaff.staffShiftId         = int.Parse(dtLaneEvent.Rows[0]["StaffShiftID"].ToString());
                xStaff.staffId              = dtLaneEvent.Rows[0]["StaffID"].ToString();
                //xStaff.staffCode            = dtLaneEvent.Rows[0]["StaffCode"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["StaffCode"].ToString();
                xStaff.staffName            = dtLaneEvent.Rows[0]["StaffName"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["StaffName"].ToString();
                xLaneEvent.staff            = xStaff;

                var xSupervisor             = new Supervisor();
                xSupervisor.supervisorShiftId = int.Parse(dtLaneEvent.Rows[0]["SupervisorShiftID"].ToString());
                xSupervisor.supervisorId    = dtLaneEvent.Rows[0]["SupervisorID"].ToString();
                //xSupervisor.supervisorCode  = dtLaneEvent.Rows[0]["SupervisorCode"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["SupervisorCode"].ToString();
                xSupervisor.supervisorName  = dtLaneEvent.Rows[0]["SupervisorName"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["SupervisorName"].ToString();
                xLaneEvent.supervisor       = xSupervisor;

                var xEvent                  = new Event();
                xEvent.eventType            = int.Parse(dtLaneEvent.Rows[0]["EventType"].ToString());
                xEvent.eventTypeName        = dtLaneEvent.Rows[0]["EventTypeName"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["EventTypeName"].ToString();
                xEvent.eventState           = int.Parse(dtLaneEvent.Rows[0]["EventState"].ToString());
                xEvent.eventStateName       = dtLaneEvent.Rows[0]["EventStateName"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["EventStateName"].ToString();
                xEvent.eventRemark          = dtLaneEvent.Rows[0]["EventRemark"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["EventRemark"].ToString();
                xLaneEvent.Event            = xEvent;

                LogInfo.WriteLog(LogLevel.DEBUG, $"+ GenerateLaneEventHQ : built EventHqID={xLaneEvent.eventId}, StaffShiftID={xStaff.staffShiftId}, EventType={xEvent.eventType}, EventState={xEvent.eventState}");
                return xLaneEvent;
            }
            catch (Exception err)
            {
                Console.WriteLine($"- Error GenerateLaneEventHQ : {err.Message}");
                LogInfo.WriteLog($"- Error GenerateLaneEventHQ : {err.Message}");
                LogInfo.WriteLog(LogLevel.DEBUG, $"- GenerateLaneEventHQ : EventHqID={iLaneEventID}, stack={err.StackTrace}");
                return null;
            }
        }

        public static LaneEventRTO GenerateLaneEventRTO(long iEventRtoID)
        {
            string szQuery = @"SELECT * FROM LaneEventRTO " +
                                $"WHERE EventRtoID={iEventRtoID}";
            LogInfo.WriteLog(LogLevel.TRACE, $"+ GenerateLaneEventRTO : SQL={szQuery}");
            DataTable dtLaneEvent = TCT_Lib.LaneDB.List(szQuery);
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ GenerateLaneEventRTO : EventRtoID={iEventRtoID}, rows={dtLaneEvent?.Rows.Count ?? -1}");
            if (dtLaneEvent.Rows.Count == 0)
            {
                LogInfo.WriteLog($"- Fail GenerateLaneEventRTO : Not found EventRtoID in database : EventRtoID={iEventRtoID}");
                return null;
            }

            try
            {
                var xLaneEvent              = new LaneEventRTO();
                xLaneEvent.eventId          = long.Parse(dtLaneEvent.Rows[0]["EventRtoID"].ToString());
                xLaneEvent.eventDateTime    = TCT_Lib.DateTime_ISO8601(DateTime.Now);

                var xPlaza                  = new Plaza();
                xPlaza.plazaId              = dtLaneEvent.Rows[0]["PlazaID"].ToString();
                //xPlaza.PlazaName            = dtLaneEvent.Rows[0]["PlazaName"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["PlazaName"].ToString();
                xLaneEvent.plaza            = xPlaza;

                var xLane                   = new Lane();
                xLane.laneId                = int.Parse(dtLaneEvent.Rows[0]["LaneID"].ToString());
                //xLane.LaneName              = dtLaneEvent.Rows[0]["LaneName"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["LaneName"].ToString();
                xLane.laneType              = int.Parse(dtLaneEvent.Rows[0]["LaneType"].ToString());
                //xLane.LaneTypeName          = dtLaneEvent.Rows[0]["LaneTypeName"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["LaneTypeName"].ToString();
                xLane.laneMode              = int.Parse(dtLaneEvent.Rows[0]["LaneMode"].ToString());
                //xLane.LaneModeName          = dtLaneEvent.Rows[0]["LaneModeName"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["LaneModeName"].ToString();
                xLane.laneDirection         = int.Parse(dtLaneEvent.Rows[0]["LaneDirection"].ToString());
                //xLane.LaneDirectionName     = dtLaneEvent.Rows[0]["LaneDirectionName"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["LaneDirectionName"].ToString();
                xLaneEvent.lane             = xLane;

                var xStaff                  = new Staff();
                xStaff.staffShiftId         = int.Parse(dtLaneEvent.Rows[0]["StaffShiftID"].ToString());
                xStaff.staffId              = dtLaneEvent.Rows[0]["StaffID"].ToString();
                //xStaff.staffCode            = dtLaneEvent.Rows[0]["StaffCode"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["StaffCode"].ToString();
                xStaff.staffName            = dtLaneEvent.Rows[0]["StaffName"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["StaffName"].ToString();
                xLaneEvent.staff            = xStaff;

                var xSupervisor                 = new Supervisor();
                xSupervisor.supervisorShiftId   = int.Parse(dtLaneEvent.Rows[0]["SupervisorShiftID"].ToString());
                xSupervisor.supervisorId        = dtLaneEvent.Rows[0]["SupervisorID"].ToString();
                //xSupervisor.supervisorCode  = dtLaneEvent.Rows[0]["SupervisorCode"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["SupervisorCode"].ToString();
                xSupervisor.supervisorName      = dtLaneEvent.Rows[0]["SupervisorName"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["SupervisorName"].ToString();
                xLaneEvent.supervisor           = xSupervisor;

                var xEvent                  = new Event();
                xEvent.eventType            = int.Parse(dtLaneEvent.Rows[0]["EventType"].ToString());
                xEvent.eventTypeName        = dtLaneEvent.Rows[0]["EventTypeName"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["EventTypeName"].ToString();
                xEvent.eventState           = int.Parse(dtLaneEvent.Rows[0]["EventState"].ToString());
                xEvent.eventStateName       = dtLaneEvent.Rows[0]["EventStateName"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["EventStateName"].ToString();
                xEvent.eventRemark          = dtLaneEvent.Rows[0]["EventRemark"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["EventRemark"].ToString();
                xLaneEvent.Event            = xEvent;

                var xDeviceStatus           = new DeviceStatus();
                xDeviceStatus.deviceType    = int.Parse(dtLaneEvent.Rows[0]["DeviceType"].ToString());
                xDeviceStatus.deviceName    = dtLaneEvent.Rows[0]["DeviceName"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["DeviceName"].ToString();
                xDeviceStatus.deviceState   = int.Parse(dtLaneEvent.Rows[0]["DeviceState"].ToString());
                xLaneEvent.deviceStatus     = xDeviceStatus;

                LogInfo.WriteLog(LogLevel.DEBUG, $"+ GenerateLaneEventRTO : built EventRtoID={xLaneEvent.eventId}, StaffShiftID={xStaff.staffShiftId}, EventType={xEvent.eventType}, EventState={xEvent.eventState}, DeviceType={xDeviceStatus.deviceType}, DeviceState={xDeviceStatus.deviceState}");
                return xLaneEvent;
            }
            catch (Exception err)
            {
                Console.WriteLine($"- Error GenerateLaneEventRTO : {err.Message}");
                LogInfo.WriteLog($"- Error GenerateLaneEventRTO : {err.Message}");
                LogInfo.WriteLog(LogLevel.DEBUG, $"- GenerateLaneEventRTO : EventRtoID={iEventRtoID}, stack={err.StackTrace}");
                return null;
            }
        }

        public static LaneAlarmRTO GenerateLaneAlarmRTO(long iAlarmRtoID)
        {
            string szQuery = @"SELECT * FROM LaneAlarmRTO " +
                                $"WHERE AlarmRtoID={iAlarmRtoID}";
            LogInfo.WriteLog(LogLevel.TRACE, $"+ GenerateLaneAlarmRTO : SQL={szQuery}");
            DataTable dtLaneAlarm = TCT_Lib.LaneDB.List(szQuery);
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ GenerateLaneAlarmRTO : AlarmRtoID={iAlarmRtoID}, rows={dtLaneAlarm?.Rows.Count ?? -1}");
            if (dtLaneAlarm.Rows.Count == 0)
            {
                LogInfo.WriteLog($"- Fail GenerateLaneAlarmRTO : Not found AlarmRtoID in database : AlarmRtoID={iAlarmRtoID}");
                return null;
            }

            try
            {
                var xLaneAlarm              = new LaneAlarmRTO();
                xLaneAlarm.alarmId          = long.Parse(dtLaneAlarm.Rows[0]["AlarmRtoID"].ToString());
                xLaneAlarm.alarmDateTime    = TCT_Lib.DateTime_ISO8601(DateTime.Now);

                var xPlaza                  = new Plaza();
                xPlaza.plazaId              = dtLaneAlarm.Rows[0]["PlazaID"].ToString();
                //xPlaza.PlazaName            = dtLaneAlarm.Rows[0]["PlazaName"] == DBNull.Value ? "" : dtLaneAlarm.Rows[0]["PlazaName"].ToString();
                xLaneAlarm.plaza            = xPlaza;

                var xLane                   = new Lane();
                xLane.laneId                = int.Parse(dtLaneAlarm.Rows[0]["LaneID"].ToString());
                //xLane.LaneName              = dtLaneEvent.Rows[0]["LaneName"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["LaneName"].ToString();
                xLane.laneType              = int.Parse(dtLaneAlarm.Rows[0]["LaneType"].ToString());
                //xLane.LaneTypeName          = dtLaneEvent.Rows[0]["LaneTypeName"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["LaneTypeName"].ToString();
                xLane.laneMode              = int.Parse(dtLaneAlarm.Rows[0]["LaneMode"].ToString());
                //xLane.LaneModeName          = dtLaneEvent.Rows[0]["LaneModeName"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["LaneModeName"].ToString();
                xLane.laneDirection         = int.Parse(dtLaneAlarm.Rows[0]["LaneDirection"].ToString());
                //xLane.LaneDirectionName     = dtLaneEvent.Rows[0]["LaneDirectionName"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["LaneDirectionName"].ToString();
                xLaneAlarm.lane             = xLane;

                var xStaff                  = new Staff();
                xStaff.staffShiftId         = int.Parse(dtLaneAlarm.Rows[0]["StaffShiftID"].ToString());
                xStaff.staffId              = dtLaneAlarm.Rows[0]["StaffID"].ToString();
                //xStaff.staffCode            = dtLaneAlarm.Rows[0]["StaffCode"] == DBNull.Value ? "" : dtLaneAlarm.Rows[0]["StaffCode"].ToString();
                xStaff.staffName            = dtLaneAlarm.Rows[0]["StaffName"] == DBNull.Value ? "" : dtLaneAlarm.Rows[0]["StaffName"].ToString();
                xLaneAlarm.staff            = xStaff;

                var xSupervisor             = new Supervisor();
                xSupervisor.supervisorShiftId = int.Parse(dtLaneAlarm.Rows[0]["SupervisorShiftID"].ToString());
                xSupervisor.supervisorId    = dtLaneAlarm.Rows[0]["SupervisorID"].ToString();
                //xSupervisor.supervisorCode  = dtLaneAlarm.Rows[0]["SupervisorCode"] == DBNull.Value ? "" : dtLaneAlarm.Rows[0]["SupervisorCode"].ToString();
                xSupervisor.supervisorName  = dtLaneAlarm.Rows[0]["SupervisorName"] == DBNull.Value ? "" : dtLaneAlarm.Rows[0]["SupervisorName"].ToString();
                xLaneAlarm.supervisor       = xSupervisor;

                var xAlarm                  = new Alarm();
                xAlarm.alarmDevice          = int.Parse(dtLaneAlarm.Rows[0]["AlarmDevice"].ToString());
                xAlarm.alarmDeviceName      = dtLaneAlarm.Rows[0]["AlarmDeviceName"] == DBNull.Value ? "" : dtLaneAlarm.Rows[0]["AlarmDeviceName"].ToString();
                xAlarm.alarmType            = int.Parse(dtLaneAlarm.Rows[0]["AlarmType"].ToString());
                xAlarm.alarmTypeName        = dtLaneAlarm.Rows[0]["AlarmTypeName"] == DBNull.Value ? "" : dtLaneAlarm.Rows[0]["AlarmTypeName"].ToString();
                xAlarm.alarmDetail          = dtLaneAlarm.Rows[0]["AlarmDetail"] == DBNull.Value ? "" : dtLaneAlarm.Rows[0]["AlarmDetail"].ToString();
                xAlarm.alarmRemark          = dtLaneAlarm.Rows[0]["AlarmRemark"] == DBNull.Value ? "" : dtLaneAlarm.Rows[0]["AlarmRemark"].ToString();
                xLaneAlarm.alarm            = xAlarm;

                LogInfo.WriteLog(LogLevel.DEBUG, $"+ GenerateLaneAlarmRTO : built AlarmRtoID={xLaneAlarm.alarmId}, StaffShiftID={xStaff.staffShiftId}, AlarmDevice={xAlarm.alarmDevice}, AlarmType={xAlarm.alarmType}");
                return xLaneAlarm;
            }
            catch (Exception err)
            {
                Console.WriteLine($"- Error GenerateLaneAlarmRTO : {err.Message}");
                LogInfo.WriteLog($"- Error GenerateLaneAlarmRTO : {err.Message}");
                LogInfo.WriteLog(LogLevel.DEBUG, $"- GenerateLaneAlarmRTO : AlarmRtoID={iAlarmRtoID}, stack={err.StackTrace}");
                return null;
            }
        }

        public static BeginOfJobJson GenerateLaneBeginOfJob(int iStaffShiftID)
        {
            string szQuery = @"SELECT * FROM StaffShift " +
                                $"WHERE StaffShiftID={iStaffShiftID}";
            LogInfo.WriteLog(LogLevel.TRACE, $"+ GenerateLaneBeginOfJob : SQL={szQuery}");
            DataTable dtBeginShift = TCT_Lib.LaneDB.List(szQuery);
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ GenerateLaneBeginOfJob : StaffShiftID={iStaffShiftID}, rows={dtBeginShift?.Rows.Count ?? -1}");
            if (dtBeginShift.Rows.Count == 0)
            {
                LogInfo.WriteLog($"- Fail GenerateLaneBeginOfJob : Not found StaffShiftID in database : StaffShiftID={iStaffShiftID}");
                return null;
            }

            try
            {
                var xBeginJobData           = new BeginOfJobJson();

                xBeginJobData.jsonrpc       = "2.0";
                xBeginJobData.method        = "BeginOfJob";
                var xBeginJob               = new BeginOfJob();

                var xPlaza                  = new Plaza();
                xPlaza.plazaId              = dtBeginShift.Rows[0]["PlazaID"].ToString();
                xPlaza.plazaName            = TCT_Lib.szPlazaName;
                xBeginJob.plaza             = xPlaza;

                var xLane                   = new Lane();
                xLane.laneId                = int.Parse(dtBeginShift.Rows[0]["LaneID"].ToString());
                xLane.laneName              = TCT_Lib.szLaneName;
                xLane.laneType              = int.Parse(dtBeginShift.Rows[0]["LaneType"].ToString());
                xLane.laneMode              = (int)TCT_Lib.ReportLaneMode(TCT_Lib.eCurLaneMode);
                xLane.laneDirection         = (int)TCT_Lib.eLaneDirection;
                //xLane.LaneDirectionName     = dtLaneEvent.Rows[0]["LaneDirectionName"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["LaneDirectionName"].ToString();
                xBeginJob.lane              = xLane;

                var xStaff                  = new Staff();
                xStaff.staffShiftId         = int.Parse(dtBeginShift.Rows[0]["StaffShiftID"].ToString());
                xStaff.staffId              = dtBeginShift.Rows[0]["StaffID"].ToString();
                //xStaff.staffCode            = dtBeginShift.Rows[0]["StaffCode"] == DBNull.Value ? "" : dtBeginShift.Rows[0]["StaffCode"].ToString();
                xStaff.staffName            = dtBeginShift.Rows[0]["StaffName"] == DBNull.Value ? "" : dtBeginShift.Rows[0]["StaffName"].ToString();
                xStaff.startShiftDateTime   = TCT_Lib.DateTime_ISO8601(Convert.ToDateTime(dtBeginShift.Rows[0]["StaffStartShiftDateTime"]));
                xBeginJob.staff             = xStaff;

                var xSupervisor                 = new Supervisor();
                xSupervisor.supervisorShiftId   = int.Parse(dtBeginShift.Rows[0]["SupervisorShiftID"].ToString());
                xSupervisor.supervisorId        = dtBeginShift.Rows[0]["SupervisorID"].ToString();
                //xSupervisor.supervisorCode  = dtBeginShift.Rows[0]["SupervisorCode"] == DBNull.Value ? "" : dtBeginShift.Rows[0]["SupervisorCode"].ToString();
                xSupervisor.supervisorName      = dtBeginShift.Rows[0]["SupervisorName"] == DBNull.Value ? "" : dtBeginShift.Rows[0]["SupervisorName"].ToString();
                xSupervisor.startShiftDateTime  = dtBeginShift.Rows[0]["SupervisorStartShiftDateTime"] == DBNull.Value 
                                                    ? null : TCT_Lib.DateTime_ISO8601(Convert.ToDateTime(dtBeginShift.Rows[0]["SupervisorStartShiftDateTime"]));
                xBeginJob.supervisor            = xSupervisor;

                xBeginJob.bojDateTime       = TCT_Lib.DateTime_ISO8601(Convert.ToDateTime(dtBeginShift.Rows[0]["StaffStartShiftDateTime"]));
                xBeginJob.jobNo             = int.Parse(dtBeginShift.Rows[0]["JobNo"].ToString());
                xBeginJob.jobType           = TCT_Lib.eCurLaneMode == LaneMode.MAINTENANCE ? "MT" : "TC";
                xBeginJob.manualLogin       = int.Parse(dtBeginShift.Rows[0]["StaffLoginMethod"].ToString());

                xBeginJobData.@params       = xBeginJob;

                LogInfo.WriteLog(LogLevel.DEBUG, $"+ GenerateLaneBeginOfJob : built StaffShiftID={xStaff.staffShiftId}, StaffID={xStaff.staffId}, JobNo={xBeginJob.jobNo}, JobType={xBeginJob.jobType}, ManualLogin={xBeginJob.manualLogin}, laneMode={xLane.laneMode}");
                return xBeginJobData;

            }
            catch (Exception err)
            {
                Console.WriteLine($"- Error GenerateLaneBeginOfJob : {err.Message}");
                LogInfo.WriteLog($"- Error GenerateLaneBeginOfJob : {err.Message}");
                LogInfo.WriteLog(LogLevel.DEBUG, $"- GenerateLaneBeginOfJob : StaffShiftID={iStaffShiftID}, stack={err.StackTrace}");
                return null;
            }
        }

        public static EndOfJobJson GenerateLaneEndOfJob(int iStaffShiftID)
        {
            string szQuery = @"SELECT * FROM StaffShift " +
                                $"WHERE StaffShiftID={iStaffShiftID}";
            LogInfo.WriteLog(LogLevel.TRACE, $"+ GenerateLaneEndOfJob : SQL={szQuery}");
            DataTable dtEndShift = TCT_Lib.LaneDB.List(szQuery);
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ GenerateLaneEndOfJob : StaffShiftID={iStaffShiftID}, rows={dtEndShift?.Rows.Count ?? -1}");
            if (dtEndShift.Rows.Count == 0)
            {
                LogInfo.WriteLog($"- Fail GenerateLaneEndOfJob : Not found StaffShiftID in database : StaffShiftID={iStaffShiftID}");
                return null;
            }

            try
            {
                var xEndJobData             = new EndOfJobJson();

                xEndJobData.jsonrpc         = "2.0";
                xEndJobData.method          = "EndOfJob";
                var xEndJob                 = new EndOfJob();

                LogInfo.WriteLog("+ Start Generate EndOfJobData : StaffShiftID = " + iStaffShiftID);
                xEndJob.staffShiftId        = iStaffShiftID;
                var xPlaza                  = new Plaza();
                xPlaza.plazaId              = dtEndShift.Rows[0]["PlazaID"] == DBNull.Value ? "" : dtEndShift.Rows[0]["PlazaID"].ToString();
                //xPlaza.PlazaName            = dtLaneAlarm.Rows[0]["PlazaName"] == DBNull.Value ? "" : dtLaneAlarm.Rows[0]["PlazaName"].ToString();
                xEndJob.plaza               = xPlaza;
                LogInfo.WriteLog("+ Success Plaza Data");

                var xLane                   = new Lane();
                xLane.laneId                = dtEndShift.Rows[0]["LaneID"] == DBNull.Value ? 0 : int.Parse(dtEndShift.Rows[0]["LaneID"].ToString());
                //xLane.LaneName              = dtLaneEvent.Rows[0]["LaneName"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["LaneName"].ToString();
                xLane.laneType              = dtEndShift.Rows[0]["LaneType"] == DBNull.Value ? 0 : int.Parse(dtEndShift.Rows[0]["LaneType"].ToString());
                xLane.laneMode              = (int)TCT_Lib.ReportLaneMode(TCT_Lib.eCurLaneMode);
                xLane.laneDirection         = (int)TCT_Lib.eLaneDirection;
                //xLane.LaneDirectionName     = dtLaneEvent.Rows[0]["LaneDirectionName"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["LaneDirectionName"].ToString();
                xEndJob.lane                = xLane;
                LogInfo.WriteLog("+ Success Lane Data");

                var xStaff                  = new Staff();
                xStaff.staffShiftId         = dtEndShift.Rows[0]["StaffShiftID"] == DBNull.Value ? 0 : int.Parse(dtEndShift.Rows[0]["StaffShiftID"].ToString());
                xStaff.staffId              = dtEndShift.Rows[0]["StaffID"] == DBNull.Value ? "" : dtEndShift.Rows[0]["StaffID"].ToString();
                //xStaff.staffCode            = dtEndShift.Rows[0]["StaffCode"] == DBNull.Value ? "" : dtEndShift.Rows[0]["StaffCode"].ToString();
                xStaff.staffName            = dtEndShift.Rows[0]["StaffName"] == DBNull.Value ? "" : dtEndShift.Rows[0]["StaffName"].ToString();
                xStaff.startShiftDateTime   = (dtEndShift.Rows[0]["StaffStartShiftDateTime"] == DBNull.Value)
                                                    ? null : TCT_Lib.DateTime_ISO8601(Convert.ToDateTime(dtEndShift.Rows[0]["StaffStartShiftDateTime"]));
                xEndJob.staff               = xStaff;
                LogInfo.WriteLog("+ Success Staff Data");

                var xSupervisor                 = new Supervisor();
                xSupervisor.supervisorShiftId   = dtEndShift.Rows[0]["SupervisorShiftID"] == DBNull.Value ? 0 : int.Parse(dtEndShift.Rows[0]["SupervisorShiftID"].ToString());
                xSupervisor.supervisorId        = dtEndShift.Rows[0]["SupervisorID"] == DBNull.Value ? "" : dtEndShift.Rows[0]["SupervisorID"].ToString();
                //xSupervisor.supervisorCode  = dtEndShift.Rows[0]["SupervisorCode"] == DBNull.Value ? "" : dtEndShift.Rows[0]["SupervisorCode"].ToString();
                xSupervisor.supervisorName      = dtEndShift.Rows[0]["SupervisorName"] == DBNull.Value ? "" : dtEndShift.Rows[0]["SupervisorName"].ToString();
                xSupervisor.startShiftDateTime  = dtEndShift.Rows[0]["SupervisorStartShiftDateTime"] == DBNull.Value 
                                                    ? null : TCT_Lib.DateTime_ISO8601(Convert.ToDateTime(dtEndShift.Rows[0]["SupervisorStartShiftDateTime"]));
                xEndJob.supervisor              = xSupervisor;
                LogInfo.WriteLog("+ Success Supervisor Data");

                xEndJob.bojDateTime         = dtEndShift.Rows[0]["StaffStartShiftDateTime"] == DBNull.Value ? TCT_Lib.DateTime_ISO8601(DateTime.Now) : TCT_Lib.DateTime_ISO8601(Convert.ToDateTime(dtEndShift.Rows[0]["StaffStartShiftDateTime"]));
                xEndJob.jobNo               = dtEndShift.Rows[0]["JobNo"] == DBNull.Value ? 0 : int.Parse(dtEndShift.Rows[0]["JobNo"].ToString());
                xEndJob.eojDateTime         = (dtEndShift.Rows[0]["StaffEndShiftDateTime"] == DBNull.Value) 
                                                    ? null : Convert.ToDateTime(dtEndShift.Rows[0]["StaffEndShiftDateTime"].ToString()).ToString(TCT_Lib.szDateTimeFormatMOC);
                xEndJob.eojMethod           = dtEndShift.Rows[0]["StaffEndShiftMethod"] == DBNull.Value ? 0 : int.Parse(dtEndShift.Rows[0]["StaffEndShiftMethod"].ToString());
                xEndJob.jobType             = TCT_Lib.eCurLaneMode == LaneMode.MAINTENANCE ? "MT" : "TC";
                LogInfo.WriteLog("+ Success Job Data");

                //var xTransStatistic         = GenerateLaneTransactionStatistic(iStaffShiftID);
                //xEndJob.transStatistic      = xTransStatistic;

                xEndJobData.@params         = xEndJob;

                //----------------------------------------
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ GenerateLaneEndOfJob : built StaffShiftID={iStaffShiftID}, JobNo={xEndJob.jobNo}, JobType={xEndJob.jobType}, EojMethod={xEndJob.eojMethod}, EojDateTime={xEndJob.eojDateTime ?? "null"}");
                if (xEndJob.eojDateTime == null)
                    LogInfo.WriteLog(LogLevel.WARN, $"- Warning GenerateLaneEndOfJob : StaffEndShiftDateTime is NULL -> return null (EOJ not sent) : StaffShiftID={iStaffShiftID}");
                if (xEndJob.eojDateTime == null)
                    return null;
                //----------------------------------------

                return xEndJobData;

            }
            catch (Exception err)
            {
                Console.WriteLine($"- Error GenerateLaneEndOfJob : {err.Message}");
                LogInfo.WriteLog($"- Error GenerateLaneEndOfJob : {err.Message}");
                LogInfo.WriteLog(LogLevel.DEBUG, $"- GenerateLaneEndOfJob : StaffShiftID={iStaffShiftID}, stack={err.StackTrace}");
                return null;
            }
        }

        public static OtlStatusJson GenerateOtlStatus(bool bOtlStatus, int iStaffShiftID)
        {
            string szQuery = @"SELECT * FROM StaffShift " +
                                $"WHERE StaffShiftID={iStaffShiftID}";
            LogInfo.WriteLog(LogLevel.TRACE, $"+ GenerateOtlStatus : SQL={szQuery}");
            DataTable dtBeginShift = TCT_Lib.LaneDB.List(szQuery);
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ GenerateOtlStatus : StaffShiftID={iStaffShiftID}, OtlStatus={bOtlStatus}, rows={dtBeginShift?.Rows.Count ?? -1}");
            if (dtBeginShift.Rows.Count == 0)
            {
                LogInfo.WriteLog($"- Fail GenerateOtlStatus : Not found StaffShiftID in database : StaffShiftID={iStaffShiftID}");
                return null;
            }

            try
            {
                var xOtlStatusData          = new OtlStatusJson();

                xOtlStatusData.jsonrpc       = "2.0";
                xOtlStatusData.method        = "OtlStatus";
                var xOtlStatus               = new OtlStatus();

                var xPlaza                  = new Plaza();
                xPlaza.plazaId              = dtBeginShift.Rows[0]["PlazaID"].ToString();
                xPlaza.plazaName            = TCT_Lib.szPlazaName;
                xOtlStatus.plaza             = xPlaza;

                var xLane                   = new Lane();
                xLane.laneId                = int.Parse(dtBeginShift.Rows[0]["LaneID"].ToString());
                xLane.laneName              = TCT_Lib.szLaneName;
                xLane.laneType              = int.Parse(dtBeginShift.Rows[0]["LaneType"].ToString());
                xLane.laneMode              = (int)TCT_Lib.ReportLaneMode(TCT_Lib.eCurLaneMode);
                xLane.laneDirection         = (int)TCT_Lib.eLaneDirection;
                //xLane.LaneDirectionName     = dtLaneEvent.Rows[0]["LaneDirectionName"] == DBNull.Value ? "" : dtLaneEvent.Rows[0]["LaneDirectionName"].ToString();
                xOtlStatus.lane              = xLane;

                var xStaff                  = new Staff();
                xStaff.staffShiftId         = int.Parse(dtBeginShift.Rows[0]["StaffShiftID"].ToString());
                xStaff.staffId              = dtBeginShift.Rows[0]["StaffID"].ToString();
                //xStaff.staffCode            = dtBeginShift.Rows[0]["StaffCode"] == DBNull.Value ? "" : dtBeginShift.Rows[0]["StaffCode"].ToString();
                xStaff.staffName            = dtBeginShift.Rows[0]["StaffName"] == DBNull.Value ? "" : dtBeginShift.Rows[0]["StaffName"].ToString();
                xStaff.startShiftDateTime   = TCT_Lib.DateTime_ISO8601(Convert.ToDateTime(dtBeginShift.Rows[0]["StaffStartShiftDateTime"].ToString()));
                xOtlStatus.staff             = xStaff;

                var xSupervisor                 = new Supervisor();
                xSupervisor.supervisorShiftId   = int.Parse(dtBeginShift.Rows[0]["SupervisorShiftID"].ToString());
                xSupervisor.supervisorId        = dtBeginShift.Rows[0]["SupervisorID"].ToString();
                //xSupervisor.supervisorCode  = dtBeginShift.Rows[0]["SupervisorCode"] == DBNull.Value ? "" : dtBeginShift.Rows[0]["SupervisorCode"].ToString();
                xSupervisor.supervisorName      = dtBeginShift.Rows[0]["SupervisorName"] == DBNull.Value ? "" : dtBeginShift.Rows[0]["SupervisorName"].ToString();
                xSupervisor.startShiftDateTime  = dtBeginShift.Rows[0]["SupervisorStartShiftDateTime"] == DBNull.Value 
                                                    ? null : TCT_Lib.DateTime_ISO8601(Convert.ToDateTime(dtBeginShift.Rows[0]["SupervisorStartShiftDateTime"]));
                xOtlStatus.supervisor            = xSupervisor;

                xOtlStatus.otlStatus                = bOtlStatus;
                xOtlStatus.otlChangeStatusDateTime  = TCT_Lib.DateTimeNow_ISO8601();

                xOtlStatusData.@params          = xOtlStatus;

                LogInfo.WriteLog(LogLevel.DEBUG, $"+ GenerateOtlStatus : built StaffShiftID={xStaff.staffShiftId}, OtlStatus={bOtlStatus}");
                return xOtlStatusData;

            }
            catch (Exception err)
            {
                Console.WriteLine($"- Error GenerateOtlStatus : {err.Message}");
                LogInfo.WriteLog($"- Error GenerateOtlStatus : {err.Message}");
                LogInfo.WriteLog(LogLevel.DEBUG, $"- GenerateOtlStatus : StaffShiftID={iStaffShiftID}, stack={err.StackTrace}");
                return null;
            }
        }

        public static TransactionStatistic GenerateLaneTransactionStatistic(int iStaffShiftID)
        {
            string szQuery = @"SELECT * FROM TransactionStatistic " +
                                $"WHERE StaffShiftID={iStaffShiftID}";
            LogInfo.WriteLog(LogLevel.TRACE, $"+ GenerateLaneTransactionStatistic : SQL={szQuery}");
            DataTable dtTrans = TCT_Lib.LaneDB.List(szQuery);
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ GenerateLaneTransactionStatistic : StaffShiftID={iStaffShiftID}, rows={dtTrans?.Rows.Count ?? -1}");
            if (dtTrans.Rows.Count == 0)
            {
                LogInfo.WriteLog($"- Fail GenerateLaneTransactionStatistic : Not found StaffShiftID in database : StaffShiftID={iStaffShiftID}");
                return null;
            }

            szQuery = "SELECT SUM(TotalCouponType1) AS SumSalesCoupon1, SUM(TotalAmountCouponType1) AS SumAmountCoupon1, " +
                        "SUM(TotalCouponType2) AS SumSalesCoupon2, SUM(TotalAmountCouponType2) AS SumAmountCoupon2 " +
                        "FROM SalesCouponTransaction " +
                        $"WHERE StaffShiftID={iStaffShiftID}";
            LogInfo.WriteLog(LogLevel.TRACE, $"+ GenerateLaneTransactionStatistic : SQL={szQuery}");
            DataTable dtSalesCoupon = TCT_Lib.LaneDB.List(szQuery);
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ GenerateLaneTransactionStatistic : SalesCoupon rows={dtSalesCoupon?.Rows.Count ?? -1}");

            try
            {
                var xTransStatistic                     = new TransactionStatistic();
                xTransStatistic.totalSumVehicles        = int.Parse(dtTrans.Rows[0]["ShiftTotalSumVehicles"].ToString());
                xTransStatistic.totalTransViolation     = int.Parse(dtTrans.Rows[0]["TotalTransViolation"].ToString());
                xTransStatistic.totalTransExempt        = int.Parse(dtTrans.Rows[0]["TotalTransExempt"].ToString());
                xTransStatistic.totalTransDIS           = int.Parse(dtTrans.Rows[0]["TotalTransDIS"].ToString());
                xTransStatistic.totalTransTowing        = int.Parse(dtTrans.Rows[0]["TotalTransTowing"].ToString());
                xTransStatistic.totalTransHPMC          = int.Parse(dtTrans.Rows[0]["TotalTransHPMC"].ToString());
                xTransStatistic.totalTransLeftExit      = int.Parse(dtTrans.Rows[0]["TotalTransLeftExit"].ToString());

                var xTransClass1                        = new ClassTypeStatistic();
                xTransClass1.totalTransCash             = int.Parse(dtTrans.Rows[0]["TotalClass1_TransCash"].ToString());
                xTransClass1.totalFareCash              = decimal.Parse(dtTrans.Rows[0]["TotalClass1_FareCash"].ToString());
                xTransClass1.totalTransCoupon           = int.Parse(dtTrans.Rows[0]["TotalClass1_TransCoupon"].ToString());
                xTransClass1.totalFareCoupon            = decimal.Parse(dtTrans.Rows[0]["TotalClass1_FareCoupon"].ToString());
                xTransClass1.totalTransEMV              = int.Parse(dtTrans.Rows[0]["TotalClass1_TransEMV"].ToString());
                xTransClass1.totalFareEMV               = decimal.Parse(dtTrans.Rows[0]["TotalClass1_FareEMV"].ToString());
                xTransClass1.totalTransQrCode           = int.Parse(dtTrans.Rows[0]["TotalClass1_TransQrCode"].ToString());
                xTransClass1.totalFareQrCode            = decimal.Parse(dtTrans.Rows[0]["TotalClass1_FareQrCode"].ToString());
                xTransClass1.totalTransDMT              = int.Parse(dtTrans.Rows[0]["TotalClass1_TransDMT"].ToString());
                xTransStatistic.totalTransClassType1    = xTransClass1;

                var xTransClass2                        = new ClassTypeStatistic();
                xTransClass2.totalTransCash             = int.Parse(dtTrans.Rows[0]["TotalClass2_TransCash"].ToString());
                xTransClass2.totalFareCash              = decimal.Parse(dtTrans.Rows[0]["TotalClass2_FareCash"].ToString());
                xTransClass2.totalTransCoupon           = int.Parse(dtTrans.Rows[0]["TotalClass2_TransCoupon"].ToString());
                xTransClass2.totalFareCoupon            = decimal.Parse(dtTrans.Rows[0]["TotalClass2_FareCoupon"].ToString());
                xTransClass2.totalTransEMV              = int.Parse(dtTrans.Rows[0]["TotalClass2_TransEMV"].ToString());
                xTransClass2.totalFareEMV               = decimal.Parse(dtTrans.Rows[0]["TotalClass2_FareEMV"].ToString());
                xTransClass2.totalTransQrCode           = int.Parse(dtTrans.Rows[0]["TotalClass2_TransQrCode"].ToString());
                xTransClass2.totalFareQrCode            = decimal.Parse(dtTrans.Rows[0]["TotalClass2_FareQrCode"].ToString());
                xTransClass2.totalTransDMT              = int.Parse(dtTrans.Rows[0]["TotalClass2_TransDMT"].ToString());
                xTransStatistic.totalTransClassType2    = xTransClass2;

                var xTransAVC                           = new AvcStatistic();
                xTransAVC.totalAvcClass1                = int.Parse(dtTrans.Rows[0]["TotalAVC_Class1"].ToString());
                xTransAVC.totalAvcClass2                = int.Parse(dtTrans.Rows[0]["TotalAVC_Class2"].ToString());
                xTransAVC.totalAvcClass5                = int.Parse(dtTrans.Rows[0]["TotalAVC_Class5"].ToString());
                xTransAVC.totalAvcClass255              = int.Parse(dtTrans.Rows[0]["TotalAVC_Class255"].ToString());
                xTransAVC.totalAvcClassOther            = int.Parse(dtTrans.Rows[0]["TotalAVC_ClassOther"].ToString());
                xTransStatistic.totalTransAVC           = xTransAVC;

                var xTransMIS                           = new MisStatistic();
                xTransMIS.totalMIS                      = int.Parse(dtTrans.Rows[0]["TotalMIS"].ToString());
                xTransMIS.totalClassTypeMIS             = int.Parse(dtTrans.Rows[0]["TotalMIS_ClassType"].ToString());
                xTransMIS.totalPayTypeMIS               = int.Parse(dtTrans.Rows[0]["TotalMIS_PayType"].ToString());
                xTransMIS.totalMIS                      = int.Parse(dtTrans.Rows[0]["TotalMIS"].ToString());
                xTransStatistic.totalMIS                = xTransMIS;

                var xSalesCoupon                        = new SalesCouponStatistic();
                xSalesCoupon.totalCouponType1           = dtSalesCoupon.Rows[0]["SumSalesCoupon1"] == DBNull.Value ? 0 : int.Parse(dtSalesCoupon.Rows[0]["SumSalesCoupon1"].ToString());
                xSalesCoupon.totalAmountType1           = dtSalesCoupon.Rows[0]["SumAmountCoupon1"] == DBNull.Value ? 0 : decimal.Parse(dtSalesCoupon.Rows[0]["SumAmountCoupon1"].ToString());
                xSalesCoupon.totalCouponType2           = dtSalesCoupon.Rows[0]["SumSalesCoupon2"] == DBNull.Value ? 0 : int.Parse(dtSalesCoupon.Rows[0]["SumSalesCoupon2"].ToString());
                xSalesCoupon.totalAmountType2           = dtSalesCoupon.Rows[0]["SumAmountCoupon2"] == DBNull.Value ? 0 : decimal.Parse(dtSalesCoupon.Rows[0]["SumAmountCoupon2"].ToString());
                xTransStatistic.totalSalesCoupon        = xSalesCoupon;

                LogInfo.WriteLog(LogLevel.DEBUG, $"+ GenerateLaneTransactionStatistic : built StaffShiftID={iStaffShiftID}, totalSumVehicles={xTransStatistic.totalSumVehicles}, violation={xTransStatistic.totalTransViolation}, exempt={xTransStatistic.totalTransExempt}, towing={xTransStatistic.totalTransTowing}, HPMC={xTransStatistic.totalTransHPMC}, MIS={xTransMIS.totalMIS}");
                return xTransStatistic;
            }
            catch (Exception err)
            {
                Console.WriteLine($"- Error GenerateLaneTransactionStatistic : {err.Message}");
                LogInfo.WriteLog($"- Error GenerateLaneTransactionStatistic : {err.Message}");
                LogInfo.WriteLog(LogLevel.DEBUG, $"- GenerateLaneTransactionStatistic : StaffShiftID={iStaffShiftID}, stack={err.StackTrace}");
                return null;
            }
        }

        //-----------------------------------------------------------------------------------------------
        public static MtsTransJson GenerateTransactionMTS(int iLaneTransID)
        {
            string szQuery = @"SELECT * FROM LaneTransaction " +
                                $"WHERE TransactionID={iLaneTransID}";
            LogInfo.WriteLog(LogLevel.TRACE, $"+ GenerateTransactionMTS : SQL={szQuery}");
            DataTable dtLaneTrans = TCT_Lib.LaneDB.List(szQuery);
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ GenerateTransactionMTS : TransID={iLaneTransID}, rows={dtLaneTrans?.Rows.Count ?? -1}");
            if (dtLaneTrans.Rows.Count == 0)
            {
                LogInfo.WriteLog($"- Fail GenerateTransactionMTS : Not found LaneTransaction in database : LaneTransactionnID={iLaneTransID}");
                return null;
            }
            try
            {
                var xMtsTransJson           = new MtsTransJson();

                xMtsTransJson.jsonrpc       = "2.0";
                xMtsTransJson.method        = "MTS-Trans";
                var xMtsTrans               = new ObjMtsTrans();

                xMtsTrans.requestId         = "MTS" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                xMtsTrans.requestDateTime   = TCT_Lib.DateTime_ISO8601(DateTime.Now);
                
                var xPlaza                  = new RabbitMQ.Plaza();
                xPlaza.plazaId              = dtLaneTrans.Rows[0]["PlazaID"].ToString();
                xPlaza.plazaName            = dtLaneTrans.Rows[0]["PlazaName"] == DBNull.Value ? "" : dtLaneTrans.Rows[0]["PlazaName"].ToString();
                xMtsTrans.plaza             = xPlaza;

                var xLane                   = new RabbitMQ.Lane();
                xLane.laneId                = dtLaneTrans.Rows[0]["LaneID"].ToString();
                xLane.laneName              = dtLaneTrans.Rows[0]["LaneName"].ToString();
                xMtsTrans.lane              = xLane;

                var xTransInfo              = new RabbitMQ.TransInfo();
                xTransInfo.jobNo            = int.Parse(dtLaneTrans.Rows[0]["JobNo"].ToString());
                xTransInfo.nTrx             = int.Parse(dtLaneTrans.Rows[0]["nTrx"].ToString());
                xTransInfo.transDateTime    = TCT_Lib.DateTime_ISO8601(Convert.ToDateTime(dtLaneTrans.Rows[0]["TransactionDateTime"]));
                xMtsTrans.transInfo         = xTransInfo;

                var xAvcInfo                = new RabbitMQ.AvcInfo();
                xAvcInfo.avcClass           = int.Parse(dtLaneTrans.Rows[0]["AVC_Class"].ToString());
                xAvcInfo.avcAxle            = int.Parse(dtLaneTrans.Rows[0]["AVC_Axle"].ToString());
                xAvcInfo.avcWheel           = int.Parse(dtLaneTrans.Rows[0]["AVC_Wheel"].ToString());
                xAvcInfo.avcDualWheel       = int.Parse(dtLaneTrans.Rows[0]["AVC_DualWheel"].ToString());
                xAvcInfo.avcDirection       = int.Parse(dtLaneTrans.Rows[0]["AVC_Direction"].ToString());
                xAvcInfo.avcError           = 0;
                string szLC_SignalStatus    = dtLaneTrans.Rows[0]["LC_SignalStatus"].ToString();
                string[] szAryLcStatus = szLC_SignalStatus.Split(',');
                if (szAryLcStatus.Length > 3)
                    xAvcInfo.avcError = int.Parse(szAryLcStatus[3]);
                xMtsTrans.avcInfo           = xAvcInfo;

                xMtsTransJson.@params       = xMtsTrans;

                LogInfo.WriteLog(LogLevel.DEBUG, $"+ GenerateTransactionMTS : built TransID={iLaneTransID}, JobNo={xTransInfo.jobNo}, nTrx={xTransInfo.nTrx}, avcClass={xAvcInfo.avcClass}, avcError={xAvcInfo.avcError}");
                return xMtsTransJson;
            }
            catch (Exception err)
            {
                Console.WriteLine($"- Error GenerateTransactionMTS : {err.Message}");
                LogInfo.WriteLog($"- Error GenerateTransactionMTS : {err.Message}");
                LogInfo.WriteLog(LogLevel.DEBUG, $"- GenerateTransactionMTS : TransID={iLaneTransID}, stack={err.StackTrace}");
                return null;
            }
        }

        //-----------------------------------------------------------------------------------------------
        public static ImageCameraMessage GenerateCaptureImageCamera(int iLaneTransID, ref int iJobNo, ref int nTrx)
        {
            string szQuery = @"SELECT * FROM LaneTransaction " +
                                $"WHERE TransactionID={iLaneTransID}";
            LogInfo.WriteLog(LogLevel.TRACE, $"+ GenerateCaptureImageCamera : SQL={szQuery}");
            DataTable dtLaneTrans = TCT_Lib.LaneDB.List(szQuery);
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ GenerateCaptureImageCamera : TransID={iLaneTransID}, rows={dtLaneTrans?.Rows.Count ?? -1}");
            if (dtLaneTrans.Rows.Count == 0)
            {
                LogInfo.WriteLog($"- Fail GenerateCaptureImageCamera : Not found LaneTransaction in database : LaneTransactionnID={iLaneTransID}");
                return null;
            }
            try
            {
                int iLaneType               = int.Parse(dtLaneTrans.Rows[0]["LaneType"].ToString());

                var xImgCamera              = new ImageCameraMessage();

                xImgCamera.requestId        = "IMG_" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                xImgCamera.requestDateTime  = TCT_Lib.DateTime_ISO8601(DateTime.Now);
                
                var xPlaza                  = new Plaza();
                xPlaza.plazaId              = int.Parse(dtLaneTrans.Rows[0]["PlazaID"].ToString()).ToString();
                xPlaza.plazaName            = dtLaneTrans.Rows[0]["PlazaName"] == DBNull.Value ? "" : dtLaneTrans.Rows[0]["PlazaName"].ToString();
                xImgCamera.plaza            = xPlaza;

                var xLane                   = new RabbitMQ.Lane();
                xLane.laneId                = dtLaneTrans.Rows[0]["LaneID"].ToString();
                xLane.laneName              = dtLaneTrans.Rows[0]["LaneName"].ToString();
                xImgCamera.lane             = xLane;

                var xTransInfo              = new Transaction();
                xTransInfo.jobNo            = int.Parse(dtLaneTrans.Rows[0]["JobNo"].ToString());
                xTransInfo.nTrx             = int.Parse(dtLaneTrans.Rows[0]["nTrx"].ToString());
                xTransInfo.transactionDateTime  = TCT_Lib.DateTime_ISO8601(Convert.ToDateTime(dtLaneTrans.Rows[0]["TransactionDateTime"]));
                xTransInfo.direction        = GetLaneDirection(TCT_Lib.eLaneDirection);
                xImgCamera.transaction      = xTransInfo;

                DateTime dtTransDate        = Convert.ToDateTime(dtLaneTrans.Rows[0]["TransactionDateTime"].ToString());
                long iCaptureRunningNo      = TCT_Lib.LaneDB.GetCaptureRunningNoOfTrans(xTransInfo.jobNo, xTransInfo.nTrx, iLaneTransID, dtTransDate);

                var xImageDatas = new List<ImageData>();
                for (int i=1; i<=3; i++)
                {
                    var xImgData            = new ImageData();
                    xImgData.cameraIndex    = i;
                    try
                    {
                        DateTime dtDateTime     = DateTime.Now;
                        string szImgFileName    = GetImageCameraFileName(iLaneTransID, i, ref dtDateTime);
                        if ((TCT_Lib.eCurLaneType == LaneType.ETC) && (i==1))
                            xTransInfo.transactionDateTime = TCT_Lib.DateTime_ISO8601(dtDateTime);
                        string szImgBase64      = szImgFileName == "" ? "" : TCT_NVR.ConvertImageToBase64(Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, szImgFileName));
                        xImgData.fileName       = szImgFileName;
                        xImgData.imageData      = szImgBase64;
                    }
                    catch (Exception err)
                    {
                        LogInfo.WriteLogVideoCapture("- Fail Get Image Data : " + err.Message);
                        LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"- GenerateCaptureImageCamera : TransID={iLaneTransID}, Index={i}, stack={err.StackTrace}");
                    }
                    finally
                    {
                        xImageDatas.Add(xImgData);
                    }
                }
                xImgCamera.captureImages = xImageDatas;

                iJobNo  = iLaneType == 0 ? xTransInfo.jobNo : 0;
                nTrx    = iLaneType == 0 ? xTransInfo.nTrx : 0;
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ GenerateCaptureImageCamera : built TransID={iLaneTransID}, LaneType={iLaneType}, JobNo={xTransInfo.jobNo}, nTrx={xTransInfo.nTrx}, RunningNo={iCaptureRunningNo}, files=[{xImageDatas.Count}] out JobNo={iJobNo}, nTrx={nTrx}");

                return xImgCamera;
            }
            catch (Exception err)
            {
                Console.WriteLine($"- Error GenerateCaptureImageCamera : {err.Message}");
                LogInfo.WriteLog($"- Error GenerateCaptureImageCamera : {err.Message}");
                LogInfo.WriteLog(LogLevel.DEBUG, $"- GenerateCaptureImageCamera : TransID={iLaneTransID}, stack={err.StackTrace}");
                return null;
            }
        }

        static string GetImageCameraFileName(int iLaneTransID, int idx, ref DateTime dtDateTime)
        {
            string szQuery = @"SELECT ImageCameraFileName, A.UpdateDateTime FROM ImageCaptureCamera A " +
                            "JOIN LaneTransaction B ON A.JobNo=B.JobNo AND A.nTrx=B.nTrx AND A.LaneTransactionID=B.TransactionID " +
                            $"WHERE B.TransactionID={iLaneTransID} AND A.CameraIndex={idx} " +
                            "GROUP BY A.CameraIndex " +
                            "ORDER BY A.CameraIndex, A.UpdateDateTime DESC";
            LogInfo.WriteLogVideoCapture("+ Query : " + szQuery);
            DataTable dtImage = TCT_Lib.LaneDB.List(szQuery);
            if (dtImage.Rows.Count == 0)
            {
                LogInfo.WriteLogVideoCapture($"- Fail not found image name : TransID={iLaneTransID}, Index={idx}");
                dtDateTime = DateTime.Now;
                return "";
            }
            else
            {
                dtDateTime = Convert.ToDateTime(dtImage.Rows[0]["UpdateDateTime"].ToString());
                LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"+ GetImageCameraFileName : TransID={iLaneTransID}, Index={idx}, File={dtImage.Rows[0]["ImageCameraFileName"]}, UpdateDateTime={dtDateTime:yyyy-MM-dd HH:mm:ss}");
                return dtImage.Rows[0]["ImageCameraFileName"].ToString();
            }
        }



    }
}
