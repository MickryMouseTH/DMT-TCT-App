using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace TCT_App.RabbitMQ
{
    public class EtcRabbitMQ
    {
        ConnectionFactory connectionFactory = new ConnectionFactory
        {
            HostName    = TCT_Lib.szETC_RabbitMQ_ServerHostName,
            VirtualHost = TCT_Lib.szETC_RabbitMQ_VirtualHost,
            UserName    = TCT_Lib.szETC_RabbitMQ_User,
            Password    = TCT_Lib.szETC_RabbitMQ_Password,
            Port        = TCT_Lib.iETC_RabbitMQ_Port,
            RequestedConnectionTimeout = TimeSpan.FromSeconds(1),
        };

        public IConnection connection   = null;
        private IModel channel          = null;
        //---------------------------------------------------------------
        public delegate void EtcRabbitMQLaneState(dynamic objLaneState);
        public event EtcRabbitMQLaneState laneStateFromEtcRabbitMQ;

        public delegate void EtcRabbitMQCompiledPassage(dynamic objCompiledPassage);
        public event EtcRabbitMQCompiledPassage compiledPassageFromEtcRabbitMQ;

        //=========================================================
        public EtcRabbitMQ()
        {


        }

        public bool ConnectEtcRabbitMQ()
        {
            if (!TCT_Lib.bEnableEtcRabbitMQ)
                return false;

            /*
                        if (!TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szETC_RabbitMQ_ServerIPAddress, TCT_Lib.iETC_RabbitMQ_ServerPort))
                        {
                            Console.WriteLine($"- Fail TestNetworkConnection to RabbitMQ => Connect ETC Rabbit MQ : IP = {TCT_Lib.szETC_RabbitMQ_ServerIPAddress}, PORT = {TCT_Lib.iETC_RabbitMQ_ServerPort}");
                            LogInfo.WriteLog($"- Fail TestNetworkConnection to RabbitMQ => Connect ETC Rabbit MQ : IP = {TCT_Lib.szETC_RabbitMQ_ServerIPAddress}, PORT = {TCT_Lib.iETC_RabbitMQ_ServerPort}");
                            return false;
                        }
            */


            LogInfo.WriteEtcRabbitMQ($"HostName : {TCT_Lib.szETC_RabbitMQ_ServerHostName}");
            LogInfo.WriteEtcRabbitMQ($"VirtualHost : {TCT_Lib.szETC_RabbitMQ_VirtualHost}");
            LogInfo.WriteEtcRabbitMQ($"Port : {TCT_Lib.iETC_RabbitMQ_Port}");
            LogInfo.WriteEtcRabbitMQ($"User : {TCT_Lib.szETC_RabbitMQ_User}");
            //LogInfo.WriteEtcRabbitMQ($"Password : {TCT_Lib.szETC_RabbitMQ_Password}");
            try
            {
                connection  = connectionFactory.CreateConnection();
                channel     = connection.CreateModel();
                //LogInfo.WriteEtcRabbitMQ("+ Success Connect ETC RabbitMQ");

                try
                {
                    // accept only one unack-ed message at a time
                    // uint prefetchSize, ushort prefetchCount, bool global
                    channel.BasicQos(0, 1, false);

                    var msgLaneStateReceiver = new EtcMessageReceiver(channel);
                    channel.BasicConsume(TCT_Lib.szETC_RabbitMQ_LaneStateQueueName, false, msgLaneStateReceiver);
                    msgLaneStateReceiver.rabbitMQRecvMessage += MsgLaneStateReceiver_RabbitMqRecvMessage;

                    var msgCompilePassageReceiver = new EtcMessageReceiver(channel);
                    channel.BasicConsume(TCT_Lib.szETC_RabbitMQ_CompilePassageQueueName, false, msgCompilePassageReceiver);
                    msgCompilePassageReceiver.rabbitMQRecvMessage += MsgCompilePassageReceiver_RabbitMQRecvMessage;

                    LogInfo.WriteEtcRabbitMQ("+ Success Connect ETC RabbitMQ");
                }
                catch (BrokerUnreachableException err)
                {
                    LogInfo.WriteEtcRabbitMQ("- Error MessageReceiver ETC RabbitMQ : " + err.Message);
                    Console.WriteLine(err.Message);

                    return false;
                }
                catch (Exception err)
                {
                    LogInfo.WriteEtcRabbitMQ("- Error MessageReceiver ETC RabbitMQ : " + err.Message);
                    Console.WriteLine(err.Message);

                    return false;
                }

                return true;
            }
            catch (BrokerUnreachableException err)
            {
                LogInfo.WriteEtcRabbitMQ("- Error Connect ETC RabbitMQ : " + err.Message);
                Console.WriteLine(err.Message);
                connection  = null;
                channel     = null;
                //-----------------------------------------------------------------------------------
                if (err.Message.StartsWith("Already closed", StringComparison.Ordinal))
                {
                    LogInfo.WriteLog("+ Fail RabbitMQ ETC : This will try to re-connect RabbitMQ.");
                    CloseEtcRabbitMQ();
                    ConnectEtcRabbitMQ();
                }
                //------------------------------------------------------------------------------------
                return false;
            }
            catch (Exception err)
            {
                Console.WriteLine($"Fail Connect ETC RabbitMQ : {err.Message}");
                LogInfo.WriteEtcRabbitMQ($"Fail Connect ETC RabbitMQ : {err.Message}");
                connection  = null;
                channel     = null;
                // -----------------------------------------------------------------------------------
                if (err.Message.StartsWith("Already closed", StringComparison.Ordinal))
                {
                    LogInfo.WriteLog("+ Fail RabbitMQ ETC : This will try to re-connect RabbitMQ.");
                    CloseEtcRabbitMQ();
                    ConnectEtcRabbitMQ();
                }
                //------------------------------------------------------------------------------------
                return false;
            }
        }

        public void CloseEtcRabbitMQ()
        {
            // v2.0.0: null-guard + dispose so a failed/half-open connection can't NRE here,
            // and OS socket handles are actually released (was a handle leak).
            try
            {
                if (channel != null)
                {
                    channel.Close();
                    channel.Dispose();
                }
            }
            catch (Exception err) { LogInfo.WriteEtcRabbitMQ("- Error close ETC channel : " + err.Message); }
            finally { channel = null; }

            try
            {
                if (connection != null)
                {
                    connection.Close();
                    connection.Dispose();
                }
            }
            catch (Exception err) { LogInfo.WriteEtcRabbitMQ("- Error close ETC connection : " + err.Message); }
            finally { connection = null; }
        }

        void MsgLaneStateReceiver_RabbitMqRecvMessage(string szMessage)
        {
            if (!TCT_Lib.bEnableEtcRabbitMQ)
            {
                LogInfo.WriteEtcRabbitMQ("+ Found LaneState of ETC : No enable ETC RabbitMQ");
                return;
            }

            try
            {
                dynamic objLaneState = JsonConvert.DeserializeObject(szMessage);
                if (TCT_Lib.bETC_RabbitMQ_WriteLogETC)
                    LogInfo.WriteEtcRabbitMQ("+ ETC LaneStateMessage : " + szMessage);

                if (objLaneState["vehicleQueue"] !=  null)
                {
                    //LogInfo.WriteEtcRabbitMQ("+1 vehicleQueue not null");
                    dynamic vehicleQueue = objLaneState["vehicleQueue"];
                    if (vehicleQueue != null)
                    {
                        //LogInfo.WriteEtcRabbitMQ("+2 vehicleQueue not null");
                        if (vehicleQueue["vehicle"] != null)
                        {
                            //LogInfo.WriteEtcRabbitMQ("+3 vehicle not null");
                            JArray jVehicles = vehicleQueue["vehicle"];
                            if (jVehicles != null)
                            {
                                //LogInfo.WriteEtcRabbitMQ("+4 jVehicles not null");
                                if (jVehicles.Count > 0)
                                {
                                    //LogInfo.WriteEtcRabbitMQ("+5 jVehicles count > 0");
                                    dynamic vehicleETC = jVehicles[0];
                                    if (TCT_Lib.bETC_RabbitMQ_WriteLogETC)
                                        LogInfo.WriteEtcRabbitMQ("+ ETC LaneStateMessage : " + vehicleETC);
                                }
                            }
                        }
                    }
                }
                else
                {
                    LogInfo.WriteEtcRabbitMQ("+ Not found VehicleQueue");
                }

                laneStateFromEtcRabbitMQ(objLaneState);

            }
            catch (Exception err)
            {
                LogInfo.WriteEtcRabbitMQ("- Error Recv ETC LaneState RabbitMQ Msg  : " + err.Message);
                LogInfo.WriteEtcRabbitMQ("- Error Recv ETC LaneState RabbitMQ Msg  : " + err.StackTrace);
                LogInfo.WriteEtcRabbitMQ("- Error Recv ETC LaneState RabbitMQ Msg  : " + szMessage);
                Console.WriteLine(err.Message);
            }
        }

        void MsgCompilePassageReceiver_RabbitMQRecvMessage(string szMessage)
        {
            if (!TCT_Lib.bEnableEtcRabbitMQ)
            {
                LogInfo.WriteEtcRabbitMQ("+ Found CompilePassage of ETC : No enable ETC RabbitMQ");
                return;
            }

            try
            {
                dynamic objCompiledPassage       = JsonConvert.DeserializeObject(szMessage);
                //LogInfo.WriteEtcRabbitMQ("+ ETC CompilePassage : " + szMessage);
                objCompiledPassage.@params.image = null;
                compiledPassageFromEtcRabbitMQ(objCompiledPassage);
            }
            catch (Exception err)
            {
                LogInfo.WriteEtcRabbitMQ("- Error Recv ETC CompilePassage RabbitMQ Msg  : " + err.Message + "\r\n" + err.StackTrace);
                LogInfo.WriteEtcRabbitMQ("+ ETC CompilePassage : " + szMessage);
                Console.WriteLine(err.Message);
                // -----------------------------------------------------------------------------------
                if (err.Message.StartsWith("Already closed", StringComparison.Ordinal))
                {
                    LogInfo.WriteLog("+ Fail RabbitMQ ETC : This will try to re-connect RabbitMQ.");
                    CloseEtcRabbitMQ();
                    ConnectEtcRabbitMQ();
                }
                //------------------------------------------------------------------------------------
            }
        }

        //---------------------------------------------------------------------------------
        public bool SendChangeLoginSupervisorStaffToETC()
        {
            try
            {
                string szStafftartShift         = TCT_Lib.DateTime_ISO8601(TCT_Lib.dtStaffStartShift);
                string szSupervisorStartShift   = TCT_Lib.DateTime_ISO8601(TCT_Lib.dtSupervisorStartShift);

                string szObjMsg = @"{" +
                                "\"jsonrpc\": \"2.0\"," +
                                "\"method\": \"Login\", " +
                                "\"params\": {" +
                                       "\"origin\": { " +
                                           "\"device\": \"TCT\"," +
                                           "\"version\": \"" + TCT_Lib.szAppVersion + "\"," +
                                           "\"loginRecordId\": \"" + TCT_Lib.szStaffID + "\"," +
                                           "\"timestamp\": \"" + szStafftartShift + "\" }," +
                                       "\"origin\": { " +
                                           "\"device\": \"RTO\"," +
                                           "\"version\": \"" + TCT_Lib.szRTO_Version + "\"," +
                                           "\"loginRecordId\": \""+ TCT_Lib.szSupervisorID +"\"," +
                                           $"\"timestamp\": \"" + szSupervisorStartShift + "\" " +
                                       "}}}";

                return SendMaualActionToEtcRabbitMQ("Login", szObjMsg);
            }
            catch (Exception err)
            {
                Console.WriteLine("- Fail SendChangeLoginSupervisorStaffToETC : " + err.Message);
                LogInfo.WriteEtcRabbitMQ("- Fail SendChangeLoginSupervisorStaffToETC : " + err.Message);
                return false;
            }
        }


        public bool SendPassagePaymentToETC(int iVehicleId, int iClassType, int iPayType, decimal fFareAmount, ALB_Mode eAlbMode, bool bReClass = false, bool bTowing = false, string szEmvAuthRef = "")
        {
            TCT_Lib.szCurTransId    = "TCT" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

            if (iClassType == (int)VehicleType.CLASS_1)
                fFareAmount = TCT_Lib.fFareClass1;
            else if (iClassType == (int)VehicleType.CLASS_2)
                fFareAmount = TCT_Lib.fFareClass2;

            string szFareAmount     = (fFareAmount * 100).ToString("0");
            string szPaymentType    = (TCT_Lib.iPrevTC_Class == 0) ? "PAYMENT_COLLECTED" : "RECLASS";  // PAYMENT_COLLECTED, RECLASS
            string szPaymentMethod  = "CASH";               // CASH, COUPON, EXEMPT, DMTSERVICECAR, EMERGENCY, HPMC START, HPMC END, TOWING, FORCE RESET
            if (iPayType == (int)PayType.CASH)
                szPaymentMethod = "CASH";
            else if (iPayType == (int)PayType.COUPON)
                szPaymentMethod = "COUPON";
            else if ((iPayType == (int)PayType.HPMC) && (eAlbMode == ALB_Mode.Open))
                szPaymentMethod = "HPMC_START";
            else if ((iPayType == (int)PayType.HPMC) && (eAlbMode == ALB_Mode.Normal))
                szPaymentMethod = "HPMC_END";
            else if (iPayType == (int)PayType.DMT_CARD)
                szPaymentMethod = "DMTSERVICECAR";

            if (bTowing)
                szPaymentType   = "PAYMENT_TOWING";

            string szAlbMode = (eAlbMode == ALB_Mode.Normal) ? "ALB_NORMAL" : "ALB_OPEN";

            //--------------------------------------------------------------------------------
            // if second vehicle is towing, then PayType is RECLASS
            //if (bTowing)
            //    szPaymentType = "RECLASS";
            //--------------------------------------------------------------------------------

            try
            {
                //string szStaffStartShift = TCT_Lib.dtStaffStartShift.ToString(TCT_Lib.szDateTimeFormatISO);
                string szStaffStartShift    = TCT_Lib.DateTime_ISO8601(TCT_Lib.dtStaffStartShift);
                string szObjMsg             = "";
                //if (TCT_Lib.iPrevTC_Class == 0)     // No edit class or payment
                if (!bReClass)     // No edit class or payment
                {
                    szObjMsg = @"{" +
                             "\"jsonrpc\": \"2.0\"," +
                             "\"method\": \"MTCPassagePayment\", " +
                             "\"params\": {" +
                                    "\"origin\": { " +
                                        "\"device\": \"TCT\"," +
                                        "\"version\": \"" + TCT_Lib.szAppVersion + "\"," +
                                           "\"loginRecordId\": \"" + TCT_Lib.szStaffID + "\"," +
                                           "\"timestamp\": \"" + TCT_Lib.DateTimeNow_ISO8601() + "\" }," +
                                    "\"manualClassification\": { " +
                                        "\"vehicleId\": \"" + iVehicleId + "\", " +
                                        "\"vehicleClass\": " + iClassType + ", " +
                                        "\"fare\": \"" + szFareAmount + "\" }," +
                                    "\"manualPayment\": { " +
                                        "\"transactionId\": \"" + TCT_Lib.szCurTransId + "\", " +
                                      "\"type\": \"" + szPaymentType + "\", " +
                                      "\"method\": \"" + szPaymentMethod + "\", " +
                                      "\"amount\": \"" + szFareAmount + "\", " +
                                      "\"reversalTransactionId\": \"\", " +
                                      "\"authorization\": \"" + szEmvAuthRef + "\" }, " +
                                      "\"albMode\": \"" + szAlbMode + "\"" +
                                "}}";
                }
                else
                {
                    szObjMsg = @"{" +
                             "\"jsonrpc\": \"2.0\"," +
                             "\"method\": \"MTCPassagePayment\", " +
                             "\"params\": {" +
                                    "\"origin\": { " +
                                        "\"device\": \"TCT\"," +
                                        "\"version\": \"" + TCT_Lib.szAppVersion + "\"," +
                                           "\"loginRecordId\": \"" + TCT_Lib.szStaffID + "\"," +
                                           "\"timestamp\": \"" + TCT_Lib.DateTimeNow_ISO8601() + "\" }," +
                                    "\"manualClassification\": { " +
                                        "\"vehicleId\": \"" + iVehicleId + "\", " +
                                        "\"vehicleClass\": " + iClassType + ", " +
                                        "\"fare\": \"" + szFareAmount + "\" }," +
                                    "\"manualPayment\": [{ " +
                                            "\"transactionId\": \"" + TCT_Lib.szPrevTransId + "\", " +
                                            "\"type\": \"" + "RECLASS" + "\", " +
                                            "\"method\": \"" + TCT_Lib.GetPayMethodName(TCT_Lib.iPrevPayType) + "\", " +
                                            "\"amount\": \"" + (TCT_Lib.fPrevFareAmount * 100).ToString("0") + "\", " +
                                            "\"reversalTransactionId\": \"\", " +
                                            "\"authorization\": \"" + szEmvAuthRef + "\" }, " +
                                        " { " +
                                            "\"transactionId\": \"" + TCT_Lib.szPrevTransId + "\", " +
                                            "\"type\": \"" + "RECLASS" + "\", " +
                                            "\"method\": \"" + TCT_Lib.GetPayMethodName(TCT_Lib.iPrevPayType) + "\", " +
                                            "\"amount\": \"" + "-" + (TCT_Lib.fPrevFareAmount * 100).ToString("0") + "\", " +
                                            "\"reversalTransactionId\": \"\", " +
                                            "\"authorization\": \"" + szEmvAuthRef + "\" }, " +
                                        " { " +
                                            "\"transactionId\": \"" + TCT_Lib.szCurTransId + "\", " +
                                            "\"type\": \"" + "RECLASS" + "\", " +
                                            "\"method\": \"" + szPaymentMethod + "\", " +
                                            "\"amount\": \"" + szFareAmount + "\", " +
                                            "\"reversalTransactionId\": \"\", " +
                                            "\"authorization\": \"" + szEmvAuthRef + "\" }], " +
                                            "\"albMode\": \"" + szAlbMode + "\"" +
                                "}}";
                }

                return SendMaualActionToEtcRabbitMQ("PassagePayment", szObjMsg);
            }
            catch (Exception err)
            {
                Console.WriteLine("- Fail SendPassagePaymentToETC : " + err.Message);
                LogInfo.WriteEtcRabbitMQ("- Fail SendPassagePaymentToETC : " + err.Message);
                return false;
            }
        }

        /*
                //--- AlbMode : 0=Normal, 1=Open
                public bool SendPassagePaymentToETC_V1(int iVehicleId, int iClassType, int iPayType, decimal fFareAmount, ALB_Mode eAlbMode, bool bTowing = false, bool bEditMode = false, string szEmvAuthRef = "")
                {
                    if (!TCT_Lib.bAllowETC)
                        return false;

                    TCT_Lib.szCurTransId    = "TCT" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string szFareAmount     = (fFareAmount * 100).ToString("0");
                    //string szPaymentType    = (TCT_Lib.iPrevTC_Class == 0) ? "PAYMENT_COLLECTED" : "RECLASS";  // PAYMENT_COLLECTED, RECLASS
                    string szPaymentType    = TCT_Lib.bAfterPayment ? "RECLASS" : "PAYMENT_COLLECTED";  // PAYMENT_COLLECTED, RECLASS
                    string szPaymentMethod  = "CASH";               // CASH, COUPON, EXEMPT, DMTSERVICECAR, EMERGENCY, HPMC START, HPMC END, TOWING, FORCE RESET
                    if (iPayType == (int)PayType.CASH)
                        szPaymentMethod = "CASH";
                    else if (iPayType == (int)PayType.COUPON)
                        szPaymentMethod = "COUPON";
                    else if ((iPayType == (int)PayType.HPMC) && (eAlbMode == ALB_Mode.Open))
                        szPaymentMethod = "HPMC_START";
                    else if ((iPayType == (int)PayType.HPMC) && (eAlbMode == ALB_Mode.Normal))
                        szPaymentMethod = "HPMC_END";
                    else if (iPayType == (int)PayType.DMT_CARD)
                        szPaymentMethod = "DMTSERVICECAR";

                    if (bTowing)
                        szPaymentType   = "PAYMENT_TOWING";

                    string szAlbMode = (eAlbMode == ALB_Mode.Normal) ? "ALB_NORMAL" : "ALB_OPEN";

                    try
                    {
                        //string szStaffStartShift = TCT_Lib.dtStaffStartShift.ToString(TCT_Lib.szDateTimeFormatISO);
                        string szStaffStartShift    = TCT_Lib.DateTime_ISO8601(TCT_Lib.dtStaffStartShift);
                        string szObjMsg             = "";
                        if (!bEditMode)     // No edit class or payment
                        {
                            szObjMsg = @"{" +
                                     "\"jsonrpc\": \"2.0\"," +
                                     "\"method\": \"MTCPassagePayment\", " +
                                     "\"params\": {" +
                                            "\"origin\": { " +
                                                "\"device\": \"TCT\"," +
                                                "\"version\": \"" + TCT_Lib.szAppVersion + "\"," +
                                                "\"loginRecordId\": \"" + TCT_Lib.szStaffID + "\"," +
                                                "\"timestamp\": \"" + TCT_Lib.DateTimeNow_ISO8601() + "\" }," +
                                            "\"manualClassification\": { " +
                                                "\"vehicleId\": \"" + iVehicleId + "\", " +
                                                "\"vehicleClass\": " + iClassType + ", " +
                                                "\"fare\": \"" + szFareAmount + "\" }," +
                                            "\"manualPayment\": { " +
                                                "\"transactionId\": \"" + TCT_Lib.szCurTransId + "\", " +
                                                "\"type\": \"" + szPaymentType + "\", " +
                                                "\"method\": \"" + szPaymentMethod + "\", " +
                                                "\"amount\": \"" + szFareAmount + "\", " +
                                                "\"reversalTransactionId\": \"\", " +
                                                "\"authorization\": \"" + szEmvAuthRef + "\" }, " +
                                            "\"albMode\": \"" + szAlbMode + "\"" +
                                        "}}";
                        }
                        else
                        {
                            szObjMsg = @"{" +
                                     "\"jsonrpc\": \"2.0\"," +
                                     "\"method\": \"MTCPassagePayment\", " +
                                     "\"params\": {" +
                                            "\"origin\": { " +
                                                "\"device\": \"TCT\"," +
                                                "\"version\": \"" + TCT_Lib.szAppVersion + "\"," +
                                                "\"version\": \"" + TCT_Lib.szAppVersion + "\"," +
                                                "\"loginRecordId\": \"" + TCT_Lib.szStaffID + "\"," +
                                                "\"timestamp\": \"" + TCT_Lib.DateTimeNow_ISO8601() + "\" }," +
                                            "\"manualClassification\": { " +
                                                "\"vehicleId\": \"" + iVehicleId + "\", " +
                                                "\"vehicleClass\": " + iClassType + ", " +
                                                "\"fare\": \"" + szFareAmount + "\" }," +
                                            "\"manualPayment\": { " +
                                                "\"transactionId\": \"" + TCT_Lib.szPrevTransId + "\", " +
                                                "\"type\": \"" + "RECLASS" + "\", " +
                                                "\"method\": \"" + szPaymentMethod + "\", " +
                                                "\"amount\": \"" + "-" + szFareAmount + "\", " +
                                                "\"reversalTransactionId\": \"\", " +
                                                "\"authorization\": \"" + szEmvAuthRef + "\" }, "+
                                            "\"albMode\": \"" + szAlbMode + "\"" +
                                        "}}";
                        }

                        return SendMaualActionToEtcRabbitMQ("PassagePayment", szObjMsg);
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine("- Fail SendPassagePaymentToETC : " + err.Message);
                        LogInfo.WriteEtcRabbitMQ("- Fail SendPassagePaymentToETC : " + err.Message);
                        return false;
                    }
                }
        */

        public bool SendALB_ModeToETC(ALB_Mode eAlbMode, string szReason="Normal Payment")
        {
            if (!TCT_Lib.bAllowETC)
                return false;

            //--- ALB Mode : 0=NORMAL, 1=OPEN, 2=CLOSED
            string szAlbMode = "";
            if (eAlbMode == ALB_Mode.Normal)
                szAlbMode = "ALB_NORMAL";
            else if (eAlbMode == ALB_Mode.Open)
                szAlbMode = "ALB_OPEN";
            else
                szAlbMode = "ALB_CLOSED";

            LogInfo.WriteLaneEvent($"ETC : Send ALB Mode = {szAlbMode}, Reason={szReason}");
            LogInfo.WriteEtcRabbitMQ($"ETC : Send ALB Mode = {szAlbMode}, Reason={szReason}");

            try
            {
                //string szStafftartShift = TCT_Lib.dtStaffStartShift.ToString(TCT_Lib.szDateTimeFormatISO);
                string szStafftartShift     = TCT_Lib.DateTime_ISO8601(TCT_Lib.dtStaffStartShift);
                string szObjMsg = @"{" +
                             "\"jsonrpc\": \"2.0\"," +
                             "\"method\": \"ALBMode\", " +
                             "\"params\": {" +
                                    "\"origin\": { " +
                                        "\"device\": \"TCT\"," +
                                        "\"version\": \"" + TCT_Lib.szAppVersion + "\"," +
                                        "\"loginRecordId\": \"" + TCT_Lib.szStaffID + "\"," +
                                        "\"timestamp\": \"" + szStafftartShift + "\" }," +
                                    "\"albMode\": \""+ szAlbMode + "\"," +
                                        "\"reason\": \""+ szReason + "\" " +
                                "}}";

                return SendMaualActionToEtcRabbitMQ("ALB_Mode", szObjMsg);
            }
            catch (Exception err)
            {
                Console.WriteLine("- Fail SendALB_ModeToETC : " + err.Message);
                LogInfo.WriteEtcRabbitMQ("- Fail SendALB_ModeToETC : " + err.Message);
                LogInfo.WriteLaneEvent("- Fail SendALB_ModeToETC : " + err.Message);
                return false;
            }
        }

        private bool SendMaualActionToEtcRabbitMQ(string keyName, string msg)
        {
            if (!TCT_Lib.bEnableEtcRabbitMQ)
                return false;

            /*
                        if (!TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szETC_RabbitMQ_ServerIPAddress, TCT_Lib.iETC_RabbitMQ_ServerPort))
                        {
                            Console.WriteLine($"- Fail TestNetworkConnection to ETC RabbitMQ => SendMaualActionToRabbitMQ : IP = {TCT_Lib.szETC_RabbitMQ_ServerIPAddress}, PORT = {TCT_Lib.iETC_RabbitMQ_ServerPort}");
                            LogInfo.WriteLog($"- Fail TestNetworkConnection to ETC RabbitMQ => SendMaualActionToRabbitMQ : IP = {TCT_Lib.szETC_RabbitMQ_ServerIPAddress}, PORT = {TCT_Lib.iETC_RabbitMQ_ServerPort}");
                            return false;
                        }
            */

            LogInfo.WriteEtcRabbitMQ($"+ Send ETC RabbitMQ {keyName} : " + msg);
            try
            {
                if (connection == null)
                {
                    if (!ConnectEtcRabbitMQ())
                        return false;
                }
                var properties          = channel.CreateBasicProperties();
                properties.Persistent   = false;

                // Bind Queue to Exchange
                channel.QueueBind(TCT_Lib.szETC_RabbitMQ_ManualActionQueueName, TCT_Lib.szETC_RabbitMQ_ManualActionExchangeName, keyName);

                byte[] messagebuffer    = Encoding.Default.GetBytes(msg);
                channel.BasicPublish(exchange: TCT_Lib.szETC_RabbitMQ_ManualActionExchangeName,
                                        routingKey: keyName,
                                        basicProperties: null,
                                        body: messagebuffer);

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine($"- Error SendMaualActionToEtcRabbitMQ : {err.Message}");
                LogInfo.WriteEtcRabbitMQ($"- Error SendMaualActionToEtcRabbitMQ : {err.Message}");
                return false;
            }
        }

        public bool Send_SetLaneMode_ToETC(LaneMode eLaneMode, bool bMTC = false, bool bStart=false)
        {
            if (!bStart)
            {
                if (!bMTC)
                {
                    if ((eLaneMode != LaneMode.MAINTENANCE) && (!TCT_Lib.bEnableEtcRabbitMQ))
                    {
                        if (!TCT_Lib.bAllowETC)
                            return false;
                    }
                }
            }

            try
            {
                //--- LaneMode = OPEN, CLOSED, MAINTENANCE, HPMC, MTC
                string szLaneMode   = eLaneMode.ToString();
                if (eLaneMode == LaneMode.NORMAL)
                    szLaneMode = "OPEN";
                if (bMTC)
                    szLaneMode = "MTC";

                LogInfo.WriteLaneEvent($"+ Send ETC LaneMode = {szLaneMode}");
                LogInfo.WriteEtcRabbitMQ($"+ Send ETC LaneMode = {szLaneMode}");

                string szObjMsg     = @"{" +
                             "\"jsonrpc\": \"2.0\"," +
                             "\"method\": \"SetLaneMode\", " +
                             "\"params\": {" +
                                        "\"origin\": { " +
                                           "\"device\": \"RTO\"," +
                                           "\"version\": \"" + TCT_Lib.szRTO_Version + "\"," +
                                           "\"loginRecordId\": \"" + TCT_Lib.szSupervisorID + "\"," +
                                           $"\"timestamp\": \"" + TCT_Lib.DateTimeNow_ISO8601() + "\" }," +
                                        $"\"target\": \"" + szLaneMode + "\"" +
                                        "}}";

                //LogInfo.WriteEtcRabbitMQ("+ LaneMode : " + szObjMsg);
                return SendMaualActionToEtcRabbitMQ("SetLaneMode", szObjMsg);
            }
            catch (Exception err)
            {
                Console.WriteLine("- Fail Send_SetLaneMode_ToETC : " + err.Message);
                LogInfo.WriteEtcRabbitMQ("- Fail Send_SetLaneMode_ToETC : " + err.Message);
                LogInfo.WriteLaneEvent("- Fail Send_SetLaneMode_ToETC : " + err.Message);
                return false;
            }
        }

        public bool Send_ClearQueue_ToETC()
        {
            if (!TCT_Lib.bAllowETC)
                return false;

            LogInfo.WriteEtcRabbitMQ("ETC : ClearQueue to ETC");
            LogInfo.WriteLaneEvent("ETC : ClearQueue to ETC");
            try
            {
                string szObjMsg     = @"{" +
                             "\"jsonrpc\": \"2.0\"," +
                             "\"method\": \"ClearQueue\", " +
                             "\"params\": {" +
                                    "\"origin\": { " +
                                           "\"device\": \"RTO\"," +
                                           "\"version\": \"" + TCT_Lib.szRTO_Version + "\"," +
                                           "\"loginRecordId\": \"" + TCT_Lib.szSupervisorID + "\"," +
                                           $"\"timestamp\": \"" + TCT_Lib.DateTimeNow_ISO8601() + "\" }" +
                                    "}}";

                //LogInfo.WriteEtcRabbitMQ("+ ClearQueue : " + szObjMsg);
                return SendMaualActionToEtcRabbitMQ("ClearQueue", szObjMsg);
            }
            catch (Exception err)
            {
                Console.WriteLine("- Fail Send_ClearQueue_ToETC : " + err.Message);
                LogInfo.WriteEtcRabbitMQ("- Fail Send_ClearQueue_ToETC : " + err.Message);
                LogInfo.WriteLaneEvent("- Fail Send_ClearQueue_ToETC : " + err.Message);
                return false;
            }
        }

        public bool Send_EnableSecondaryReader_ToETC()
        {
            if (!TCT_Lib.bAllowETC)
                return false;

            LogInfo.WriteEtcRabbitMQ("ETC : EnableSecondaryReader");
            try
            {
                string szObjMsg     = @"{" +
                             "\"jsonrpc\": \"2.0\"," +
                             "\"method\": \"EnableSecondaryReader\", " +
                             "\"params\": {" +
                                    "\"origin\": { " +
                                           "\"device\": \"RTO\"," +
                                           "\"version\": \"" + TCT_Lib.szRTO_Version + "\"," +
                                           "\"loginRecordId\": \"" + TCT_Lib.szSupervisorID + "\"," +
                                           $"\"timestamp\": \"" + TCT_Lib.DateTimeNow_ISO8601() + "\" }" +
                                    "}}";

                //LogInfo.WriteEtcRabbitMQ("+ EnableSecondaryReader : " + szObjMsg);
                return SendMaualActionToEtcRabbitMQ("EnableSecondaryReader", szObjMsg);
            }
            catch (Exception err)
            {
                Console.WriteLine("- Fail Send_EnableSecondaryReader_ToETC : " + err.Message);
                LogInfo.WriteEtcRabbitMQ("- Fail Send_EnableSecondaryReader_ToETC : " + err.Message);
                return false;
            }
        }

        public bool Send_ControlOutputDevice_ToETC(string szDeviceName, bool bEnable)
        {
            if (!TCT_Lib.bAllowETC)
                return false;

            LogInfo.WriteEtcRabbitMQ($"ETC : ControlOutputDevice = {szDeviceName}, Enable = {bEnable}");
            try
            {
                string szObjMsg = @"{" +
                             "\"jsonrpc\": \"2.0\"," +
                             "\"method\": \"ControlOutputDevice\", " +
                             "\"params\": {" +
                                    "\"origin\": { " +
                                           "\"device\": \"RTO\"," +
                                           "\"version\": \"" + TCT_Lib.szRTO_Version + "\"," +
                                           "\"loginRecordId\": \"" + TCT_Lib.szSupervisorID + "\"," +
                                           $"\"timestamp\": \"" + TCT_Lib.DateTimeNow_ISO8601() + "\" }," +
                                    $"\"target\": \"" + szDeviceName + "\"," +
                                    $"\"enabled\": " + (bEnable ? "true" : "false") +
                                "}}";

                //LogInfo.WriteEtcRabbitMQ("+ ControlOutputDevice : " + szObjMsg);
                return SendMaualActionToEtcRabbitMQ("ControlOutputDevice", szObjMsg);
            }
            catch (Exception err)
            {
                Console.WriteLine("- Fail Send_ControlOutputDevice_ToETC : " + err.Message);
                LogInfo.WriteEtcRabbitMQ("- Fail Send_ControlOutputDevice_ToETC : " + err.Message);
                return false;
            }
        }

        //========================================================================================
        public void CloseMocRabbitMQ()
        {
            channel.Close();
            connection.Close();
        }
    }

    //==========================================================================================
    //--- RabbitMQ Receiver
    public class EtcMessageReceiver : DefaultBasicConsumer
    {
        public delegate void RabbitMQReceiceMessage(string szMessage);
        public event RabbitMQReceiceMessage rabbitMQRecvMessage;

        private readonly IModel _channel;
        public EtcMessageReceiver(IModel channel)
        {
            _channel = channel;
        }

        public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, ReadOnlyMemory<byte> body)
        {
            Console.WriteLine("========== Receive Message from RabbitMQ =============");
            Console.WriteLine(string.Concat("Exchange tag: ", exchange));
            Console.WriteLine(string.Concat("Consumer tag: ", consumerTag));
            Console.WriteLine(string.Concat("Delivery tag: ", deliveryTag));
            Console.WriteLine(string.Concat("Message: ", Encoding.UTF8.GetString(body.ToArray())));

            LogInfo.WriteEtcRabbitMQ("========== Receive Message from RabbitMQ =============");
            LogInfo.WriteEtcRabbitMQ(string.Concat("Exchange tag: ", exchange));
            LogInfo.WriteEtcRabbitMQ(string.Concat("Consumer tag: ", consumerTag));
            LogInfo.WriteEtcRabbitMQ(string.Concat("Delivery tag: ", deliveryTag));
            //LogInfo.WriteEtcRabbitMQ(string.Concat("Message: ", Encoding.UTF8.GetString(body.ToArray())));
            LogInfo.WriteEtcRabbitMQ("------------------------------------------------------");

            // v2.0.0: process the message inside try-catch so an exception can never kill the
            // consumer thread, and only ACK afterwards (was: ACK before processing -> message
            // lost on error). The delegate is null-checked.
            try
            {
                string szBody = Encoding.UTF8.GetString(body.ToArray());
                rabbitMQRecvMessage?.Invoke(szBody);
            }
            catch (Exception err)
            {
                LogInfo.WriteEtcRabbitMQ("- Error processing ETC message (dropped) : " + err.Message);
            }
            finally
            {
                try { _channel.BasicAck(deliveryTag, false); }
                catch (Exception e2) { LogInfo.WriteEtcRabbitMQ("- Error BasicAck ETC : " + e2.Message); }
            }

            base.HandleBasicDeliver(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body);
        }
    }

    //=================================================================================
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 

    public class ObjLaneState
    {
        public Header header { get; set; }
        public string laneMode { get; set; }
        public string albMode { get; set; }
        public InputDevices inputDevices { get; set; }
        public OutputDevices outputDevices { get; set; }
        public MainReader mainReader { get; set; }
        public SecondaryReader secondaryReader { get; set; }
        public SystemAlarms systemAlarms { get; set; }
        public VehicleQueue vehicleQueue { get; set; }
    }

    public class Location    {
        public string country { get; set; } 
        public int provider { get; set; } 
        public int plaza { get; set; } 
        public string stationType { get; set; } 
        public int lane { get; set; } 
    }

    public class Header    {
        public string sequenceNumber { get; set; } 
        public Location location { get; set; } 
        public DateTime timestamp { get; set; } 
    }

    public class InputDevices    {
        public List<int> values { get; set; } 
    }

    public class OutputDevices    {
        public List<int> values { get; set; } 
    }

    public class MainReader    {
        public bool active { get; set; } 
    }

    public class SecondaryReader    {
        public bool active { get; set; } 
    }

    public class SubsystemAlarm    {
        public string name { get; set; } 
        public string status { get; set; } 
        public string message { get; set; } 
    }

    public class SystemAlarms    {
        public string overallStatus { get; set; } 
        public List<SubsystemAlarm> subsystemAlarms { get; set; } 
    }

    public class VehicleQueue    {
        public List<object> vehicle { get; set; } 
    }

    //========================================================================
    public class ObjCompilePassage
    {
        public string jsonrpc { get; set; }
        public string method { get; set; }
        public Params @params { get; set; }
    }

    public class Origin    {
        public string device { get; set; } 
        public string version { get; set; } 
        public string loginRecordId { get; set; } 
        public DateTime timestamp { get; set; } 
    }

    public class Vehicle    {
        public DateTime entryTime { get; set; } 
        public DateTime exitTime { get; set; } 
        public DateTime passageTime { get; set; } 
    }

    public class PaymentMeans    {
        public string pan { get; set; } 
        public string expiryDate { get; set; } 
        public string usageControl { get; set; } 
    }

    public class ObuTransaction    {
        public int sequenceNumber { get; set; } 
        public string whichReader { get; set; } 
        public string efcContextMark { get; set; } 
        public int efccmCountry { get; set; } 
        public int efccmIssuer { get; set; } 
        public string efccmTypeOfContract { get; set; } 
        public string efccmContextVersion { get; set; } 
        public int obeStatus { get; set; } 
        public int signalLevel { get; set; } 
        public int manufacturerId { get; set; } 
        public string serialNumber { get; set; } 
        public int logIndex { get; set; } 
        public int returnFlags { get; set; } 
        public PaymentMeans paymentMeans { get; set; } 
        public string priceInCurrency { get; set; } 
        public string balanceObu { get; set; } 
        public string balanceStatusList { get; set; } 
        public int sessionCountry { get; set; } 
        public int sessionIssuer { get; set; } 
        public int sessionPlaza { get; set; } 
        public int sessionLane { get; set; } 
        public int sessionStationType { get; set; } 
        public int transactionStatus { get; set; } 
        public int mmiCode { get; set; } 
        public string randomRse { get; set; } 
        public int keyRef { get; set; } 
        public string obuAuthenticator { get; set; } 
        public DateTime timestamp { get; set; } 
        public DateTime sessionTime { get; set; } 
    }

    public class Image    {
        public string vehicleId { get; set; } 
        public string view { get; set; } 
        public string camera { get; set; } 
        public string imageData { get; set; } 
        public string imageLocation { get; set; } 
        public string anprCountry { get; set; } 
        public string anprLpn { get; set; } 
        public int anprConfidence { get; set; } 
        public DateTime imageTime { get; set; } 
    }

    public class DetectionEvent    {
        public string vehicleId { get; set; } 
        public string eventType { get; set; } 
        public string direction { get; set; } 
        public string sensorType { get; set; } 
        public string detectionPoint { get; set; } 
        public DateTime eventTime { get; set; } 
    }

    public class Params    {
        public Location location { get; set; } 
        public string laneType { get; set; } 
        public string laneMode { get; set; } 
        public string albMode { get; set; } 
        public string direction { get; set; } 
        public Origin origin { get; set; } 
        public int sequenceNumber { get; set; } 
        public int seqValidPayment { get; set; } 
        public int seqDetection { get; set; } 
        public int seqEnforced { get; set; } 
        public int seqLcTransaction { get; set; } 
        public int seqVideoPicture { get; set; } 
        public Vehicle vehicle { get; set; } 
        public string signalCode { get; set; } 
        public int signalCodeBitmap { get; set; } 
        public List<object> payment { get; set; } 
        public ObuTransaction obuTransaction { get; set; } 
        public List<Image> image { get; set; } 
        public List<DetectionEvent> detectionEvent { get; set; } 
    }


}
