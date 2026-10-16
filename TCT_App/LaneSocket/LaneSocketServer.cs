using System;
using SuperSocket.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Linq;
using SuperSocket.SocketBase.Config;
using SuperSocket.SocketBase;

namespace TCT_App.LaneSocket
{
    public class LaneSocketServerRTO
    {
        WebSocketServer laneServer;

        //--- Declare delegate and event
        public delegate void EtcChangeLaneMode(int iEtcLaneMode, int iEtcLaneState); 
        public event EtcChangeLaneMode etcChangeLaneMode;

        //--------------------------------------------------
        public LaneSocketServerRTO()
        {
            Console.WriteLine("+ Start LaneSocketServer : ServerPort = " + TCT_Lib.iRTO_SocketPort);
            LogInfo.WriteLog("+ Start LaneSocketServer : ServerPort = " + TCT_Lib.iRTO_SocketPort);
            laneServer = new WebSocketServer();

            if (SetupSocketServer())
            {
                if (laneServer.Start())
                {
                    Console.WriteLine("+ Successs Start LaneSocketServer");
                    LogInfo.WriteLog("+ Successs Start LaneSocketServer");
                }
                else
                {
                    Console.WriteLine("- Fail Start LaneSocketServer");
                    LogInfo.WriteLog("- Fail Start LaneSocketServer");
                }
            }
        }

        protected bool SetupSocketServer()
        {

            try
            {
                //if (laneServer.Setup(TCT_Lib.iRTO_SocketPort))
                if (laneServer.Setup(
                        new RootConfig(),
                        new ServerConfig
                        {
                            Name    = "WebSocket-TCT"+TCT_Lib.iLaneID,
                            Ip      = "Any",
                            Port    = TCT_Lib.iRTO_SocketPort,
                            Mode    = SocketMode.Tcp,
                            SendTimeOut = 5000,
                            SyncSend = true
                        }))
                {
                    laneServer.NewSessionConnected  += LaneServer_NewSessionConnected;
                    laneServer.NewMessageReceived   += LaneServer_NewMessageReceived;
                    laneServer.SessionClosed        += LaneServer_SessionClosed;

                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketServerRTO.SetupSocketServer : listen Any:{TCT_Lib.iRTO_SocketPort}, name=WebSocket-TCT{TCT_Lib.iLaneID}, sendTimeout=5000ms");
                    return true;
                }
                else
                {
                    LogInfo.WriteLog(LogLevel.ERROR, $"- LaneSocketServerRTO.SetupSocketServer : Setup failed, port={TCT_Lib.iRTO_SocketPort}");
                    return false;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error Setup ServerWebSocket - RTO : " + err.Message);
                LogInfo.WriteLog("- Error Setup ServerWebSocket - RTO : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, "- LaneSocketServerRTO.SetupSocketServer : stack=" + err.StackTrace);
                return false;
            }
        }

        void LaneServer_NewSessionConnected(WebSocketSession session)
        {
            Console.WriteLine("+ Find new session socket - RTO : IP=" + session.Host + ", SessionID=" + session.SessionID);
            LogInfo.WriteLog("+ Find new session socket - RTO : IP=" + session.Host + ", SessionID=" + session.SessionID);
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketServerRTO.NewSessionConnected : remote={session.RemoteEndPoint?.ToString() ?? "null"}, sessions={laneServer?.SessionCount}, send lane state");
            TCT_Lib.laneController.SendLaneCurrentState();
        }

        void LaneServer_SessionClosed(WebSocketSession session, SuperSocket.SocketBase.CloseReason value)
        {
            Console.WriteLine("+ Close session socket - RTO : " + session.Host + ", SessionID=" + session.SessionID);
            LogInfo.WriteLog("+ Close session socket - RTO : " + session.Host + ", SessionID=" + session.SessionID);
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketServerRTO.SessionClosed : remote={session.RemoteEndPoint?.ToString() ?? "null"}, reason={value}, sessions={laneServer?.SessionCount}");
        }

        void LaneServer_NewMessageReceived(WebSocketSession session, string value)
        {
            if (TCT_Lib.bMaintenanceMode && !TCT_Lib.bAllowSendRTO)
            {
                LogInfo.WriteLog("+ Process testing maintenance.");
                return;
            }

            Console.WriteLine("+ SocketServer : " + session.Method + ", Mesage : " + value);
            //LogInfo.WriteLog("+ SocketServer : " + session.Method + ", Mesage : " + value);
            if (LogInfo.IsEnabled(LogLevel.TRACE))
                LogInfo.WriteLog(LogLevel.TRACE, $"+ LaneSocketServerRTO.NewMessageReceived : from={session.RemoteEndPoint?.ToString() ?? "null"}, msg={value}");
            szLastSenderIP = session.RemoteEndPoint is System.Net.IPEndPoint ep ? ep.Address.ToString() : "";
            ClassifyServerMessage(value);
        }

        string szLastSenderIP = "";     // v2.0.6: ผู้ส่งคำสั่งล่าสุด (ตรวจคำสั่ง REMOTE_LOGIN_FREE ว่ามาจาก RTO จริง)

        public void SendLaneMessageToClient(string szMethod, object objParams)
        {
            if (TCT_Lib.bMaintenanceMode && !TCT_Lib.bAllowSendRTO)
            {
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketServerRTO.SendLaneMessageToClient : skip method={szMethod}, maintenance mode");
                return;
            }

            if (!TCT_Lib.bRTO_Connected)
            {
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketServerRTO.SendLaneMessageToClient : skip method={szMethod}, RTO not connected");
                return;
            }
                
            try
            {
                //--- Change last DI (Treadle D -> TotalQueue)
                if (szMethod == "IODI")
                {
                    dynamic obj         = objParams;
                    if (obj is JObject)
                    {
                        JArray jValues  = obj["values"];
                        jValues[14]     = TCT_Lib.bEMV_Connected ? 1 : 0;
                        jValues[15]     = TCT_Lib.iTotalQueue;
                        jValues[11]     = (TCT_Lib.iLastAvcStatus == -99) ? 1 : 0;  // 2022-01-04   For alert OB ON, not OFF

                        objParams       = obj;
                    }
                    else
                    {
                        obj[14]     = TCT_Lib.bEMV_Connected ? 1 : 0;
                        obj[15]     = TCT_Lib.iTotalQueue;
                        obj[11]     = (TCT_Lib.iLastAvcStatus == -99) ? 1 : 0;  // 2022-01-04   For alert OB ON, not OFF
                        objParams   = new { values = obj };
                    }
                }

                //-----------------------------------------------------
                string szJsonCmd = JsonConvert.SerializeObject(
                    new
                    {
                        jsonrpc     = "2.0",
                        id          = TCT_Lib.iLaneID,  // LaneID
                        method      = szMethod,
                        @params     = objParams,
                    }
                );

                if (szMethod != "IODO")
                {
                    Console.WriteLine("+ Send LaneMessage to RTO : " + szJsonCmd);
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketServerRTO.SendLaneMessageToClient : method={szMethod}, sessions={laneServer?.SessionCount}, len={szJsonCmd.Length}");
                    if (LogInfo.IsEnabled(LogLevel.TRACE))
                        LogInfo.WriteLog(LogLevel.TRACE, "+ LaneSocketServerRTO.SendLaneMessageToClient : " + szJsonCmd);
                    foreach (WebSocketSession session in laneServer.GetAllSessions().ToList())
                        session.Send(szJsonCmd);
                }
                else
                    LogInfo.WriteLog(LogLevel.TRACE, "+ LaneSocketServerRTO.SendLaneMessageToClient : IODO not forwarded");
            }
            catch (Exception err)
            {
                Console.WriteLine($"- Error SendLaneMessage To RTO : Method={szMethod}, Error={err.Message}");
                LogInfo.WriteLog($"- Error SendLaneMessage To RTO : Method={szMethod}, Error={err.Message}");
                LogInfo.WriteLog(LogLevel.DEBUG, "- LaneSocketServerRTO.SendLaneMessageToClient : stack=" + err.StackTrace);
                TCT_Lib.bRTO_Connected = false;
                //----------------------------------------------
                //Thread.Sleep(2000);
                LogInfo.WriteLog("+ Restart RTO WebSocket");
                LogInfo.WriteLog(LogLevel.WARN, $"- LaneSocketServerRTO.SendLaneMessageToClient : restart RTO server port={TCT_Lib.iRTO_SocketPort}, bRTO_Connected=false");
                laneServer.Stop();
                laneServer.Dispose();
                laneServer = null;
                laneServer = new WebSocketServer();

                if (SetupSocketServer())
                {
                    if (laneServer.Start())
                        LogInfo.WriteLog("+ Successs Start LaneSocketServer - RTO");
                    else
                        LogInfo.WriteLog("- Fail Start LaneSocketServer - RTO");
                }
            }
        }

        public bool SendLaneMessageToRTO(string szMethod, object objParams)
        {
            if (!TCT_Lib.bRTO_Connected)
            {
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketServerRTO.SendLaneMessageToRTO : skip method={szMethod}, RTO not connected");
                return false;
            }

            if (TCT_Lib.szRTO_IPAddress == "")
            {
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketServerRTO.SendLaneMessageToRTO : skip method={szMethod}, RTO IP not configured");
                return false;
            }

            string szJsonCmd = JsonConvert.SerializeObject(
                new
                {
                    jsonrpc     = "2.0",
                    id          = TCT_Lib.iLaneID,  // LaneID
                    method      = szMethod,
                    @params     = objParams,
                }
            );
            try
            {
                Console.WriteLine("+ Send LaneMessage to RTO : " + szJsonCmd);
                foreach (WebSocketSession session in laneServer.GetAllSessions().ToList())
                {
                    string szIP = session.RemoteEndPoint.Address.ToString();
                    if ((szIP == TCT_Lib.szRTO_IPAddress) && session.Connected)
                    {
                        LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketServerRTO.SendLaneMessageToRTO : method={szMethod}, to={szIP}, len={szJsonCmd.Length}");
                        if (LogInfo.IsEnabled(LogLevel.TRACE))
                            LogInfo.WriteLog(LogLevel.TRACE, "+ LaneSocketServerRTO.SendLaneMessageToRTO : " + szJsonCmd);
                        session.Send(szJsonCmd);
                        return true;
                    }
                }

                LogInfo.WriteLog(LogLevel.WARN, $"- LaneSocketServerRTO.SendLaneMessageToRTO : no connected session from RTO IP={TCT_Lib.szRTO_IPAddress}, dropped method={szMethod}");
                return false;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error SendLaneMessage To RTO : " + err.Message);
                Console.WriteLine("- Error SendLaneMessage To RTO : " + err.StackTrace);
                LogInfo.WriteLog("- Error SendLaneMessage To RTO : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, $"- LaneSocketServerRTO.SendLaneMessageToRTO : method={szMethod}, stack={err.StackTrace}");

                //----------------------------------------------
/*
                //Thread.Sleep(1000);
                LogInfo.WriteLog("+ Restart RTO WebSocket");
                laneServer.Stop();
                laneServer.Dispose();
                laneServer = null;
                laneServer = new WebSocketServer();

                if (SetupSocketServer())
                {
                    if (laneServer.Start())
                        LogInfo.WriteLog("+ Successs Start LaneSocketServer - RTO");
                    else
                        LogInfo.WriteLog("- Fail Start LaneSocketServer - RTO");
                }
*/

                return false;
            }
        }

        public void SendLaneMessageToClient_V0(string szData)
        {
            Console.WriteLine("+ Send LaneMessage to RTO : " + szData);
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketServerRTO.SendLaneMessageToClient_V0 : len={szData?.Length ?? 0}, sessions={laneServer?.SessionCount}");
            if (LogInfo.IsEnabled(LogLevel.TRACE))
                LogInfo.WriteLog(LogLevel.TRACE, "+ LaneSocketServerRTO.SendLaneMessageToClient_V0 : " + szData);
            foreach (WebSocketSession session in laneServer.GetAllSessions().ToList())
                session.Send(szData);
        }

        public void SendChangeLsdToRTO(LsdMode eLsdMode)
        {
            object objParams = new
            {
                LsdMode = (int)eLsdMode,
            };
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketServerRTO.SendChangeLsdToRTO : LsdMode={eLsdMode}");
            TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToClient("LSD_MODE", objParams);
        }

        public void SendTestLsdMaintenanceToRTO()
        {
            LogInfo.WriteLog(LogLevel.DEBUG, "+ LaneSocketServerRTO.SendTestLsdMaintenanceToRTO : LSD_TEST");
            TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToClient("LSD_TEST", null);
        }

        //--------------------------------------------------------------
        //=== Message from RTO
        void ClassifyServerMessage(string szMsg)
        {
            if (TCT_Lib.bMaintenanceMode && !TCT_Lib.bAllowSendRTO)
            {
                LogInfo.WriteLog(LogLevel.DEBUG, "+ LaneSocketServerRTO.ClassifyServerMessage : ignored, maintenance mode");
                return;
            }

            try
            {
                //--- Classify jrpc message and send command by evnet
                Console.WriteLine("+ Message : " + szMsg);
                dynamic msg         = JsonConvert.DeserializeObject(szMsg);
                int id              = msg["id"] ?? 0;        // RTO=101
                string result       = msg["result"];
                string method       = msg["method"];
                JObject jParams     = msg["params"];
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketServerRTO.ClassifyServerMessage : id={id}, method={method ?? "null"}, result={result ?? "null"}");

                if (result != null)
                {
                    Console.WriteLine("+ Result : " + result);
                }
                else if (method != null)
                {
                    if (method.ToUpper() == "REQ_STATE")
                    {
                        // Request current lane state
                        if (TCT_Lib.laneController != null)
                            TCT_Lib.laneController.SendLaneCurrentState();
                        TCT_Lib.webSocketLane.SendRequest_DeviceInput();
                    }
                    else if (method.ToUpper() == "REQ_CURRENT_STATISTIC")
                    {
                        //--- Request current lane statistic
                        if (TCT_Lib.laneController != null)
                            TCT_Lib.laneController.SendLaneCurrentTransStatistic();
                    }
                    else if (method.ToUpper() == "CURRENT_SUPERVISOR")
                    {
                        LogInfo.WriteLog("+ Recv current supervisor");
                        if (TCT_Lib.laneController != null)
                        {
                            //-- Response current supervisor info
                            TCT_Lib.laneController.SetCurrentSupervisor(jParams);
                        }
                    }
                    else if (method.ToUpper() == "CTRL_IO")
                    {
                        int iDeviceType = (int)jParams["id"];
                        int iValue      = (int)jParams["value"];
                        Console.WriteLine("+ From RTO => Control IO : Id=" + iDeviceType + ", Value=" + iValue);
                        LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketServerRTO.ClassifyServerMessage : CTRL_IO id={iDeviceType}, value={iValue}, laneType={TCT_Lib.eCurLaneType}, laneMode={TCT_Lib.eCurLaneMode}, openALB_RTO={TCT_Lib.bStatusOpenALB_RTO}");
                        LogInfo.WriteLaneEvent("+ From RTO => Control IO : Id=" + iDeviceType + ", Value=" + iValue);
                        //---  ALB Device
                        if ((iDeviceType == 4) || (iDeviceType == 5))
                        {
                            if ((iDeviceType == 4) && (iValue == 1))
                            {
                                LogInfo.WriteLaneEvent("+ From RTO => Control IO : ALB ON");
                                TCT_Lib.laneController.CaptureImageVideoWhenOpenALB_RTO();
                            }
                            if ((iDeviceType == 5) && (iValue == 1))
                                LogInfo.WriteLaneEvent("+ From RTO => Control IO : ALB OFF");

                            if ((TCT_Lib.eCurLaneType == LaneType.ETC) || (TCT_Lib.eCurLaneType == LaneType.DUAL))
                            {
                                if ((iDeviceType == 4) && (iValue == 1))
                                {
                                    //LogInfo.WriteLaneEvent("+ From RTO => Control IO : ALB ON, Value=" + iValue);
                                    TCT_Lib.bStatusOpenALB_RTO = true;
                                    //TCT_Lib.laneController.SendDigitalOutput_CmdALB_Open();
                                    TCT_Lib.webSocketLane.SendDigitalOutput_Controller(iDeviceType, iValue);
                                    TCT_Lib.laneController.SendEtcRabbitMq_CmdALB_ForceOpen("Force Open ALB from RTO", true);
                                    LogInfo.WriteLaneEvent("+ FROM RTO : Force Open ALB from RTO");
                                    //TCT_Lib.webSocketLane.SendDigitalOutput_Controller(iDeviceType, iValue);
                                }
                                if ((iDeviceType == 5) && (iValue == 1))
                                {
                                    if (TCT_Lib.eCurLaneMode == LaneMode.HPMC)
                                    {
                                        LogInfo.WriteLaneEvent("+ Ignore ALB Close because now in HPMC Mode => FROM RTO : Force Normal Close ALB from RTO");
                                    }
                                    else
                                    {
                                        //LogInfo.WriteLaneEvent("+ From RTO => Control IO : ALB OFF, Value=" + iValue);
                                        if (TCT_Lib.laneController != null)
                                            TCT_Lib.laneController.SendEtcRabbitMq_CmdALB_Normal("Force Normal Close ALB from RTO", true);
                                        LogInfo.WriteLaneEvent("+ FROM RTO : Force Normal Close ALB from RTO");
                                        //TCT_Lib.webSocketLane.SendDigitalOutput_Controller(iDeviceType, iValue);
                                        //TCT_Lib.webSocketLane.SendDigitalOutput_Controller(4, 0);
                                        //TCT_Lib.webSocketLane.SendDigitalOutput_Controller(5, 0);
                                    }
                                    if (TCT_Lib.bStatusOpenALB_RTO)
                                        TCT_Lib.bStatusOpenALB_RTO = false;
                                }
                            }
                            else
                            {
                                if ((iDeviceType == 4) && (iValue == 1))
                                {
                                    TCT_Lib.webSocketLane.SendDigitalOutput_Controller(iDeviceType, iValue);
                                    TCT_Lib.bStatusOpenALB_RTO = true;
                                    //TCT_Lib.laneController.SendDigitalOutput_CmdALB_Open();
                                    //TCT_Lib.webSocketLane.SendDigitalOutput_Controller(iDeviceType, iValue);
                                }

                                if (iDeviceType == 5)   // ALB OFF
                                {
                                    if (TCT_Lib.eCurLaneMode == LaneMode.HPMC)
                                    {
                                        LogInfo.WriteLaneEvent("+ Ignore ALB Close because now in HPMC Mode => FROM RTO : Force Normal Close ALB from RTO");
                                    }
                                    else 
                                    {
                                        TCT_Lib.webSocketLane.SendDigitalOutput_Controller(iDeviceType, iValue);
                                        //Thread.Sleep(100);
                                        TCT_Lib.webSocketLane.SendDigitalOutput_Controller(4, 0);
                                        TCT_Lib.webSocketLane.SendDigitalOutput_Controller(5, 0);
                                    }
                                    if (TCT_Lib.bStatusOpenALB_RTO)
                                        TCT_Lib.bStatusOpenALB_RTO = false;
                                }
                            }
                        }
                        else
                            TCT_Lib.webSocketLane.SendDigitalOutput_Controller(iDeviceType, iValue);
                    }
                    else if (method.ToUpper() == "TEST_IO")
                    {

                    }
                    else if (method.ToUpper() == "REMOTE_LOGIN")
                    {
                        string szStaffCode = (string)jParams["code"];
                        Console.WriteLine("+ From RTO => Remote Login : StaffCode=" + szStaffCode);
                        LogInfo.WriteLaneEvent("+ From RTO => Remote Login : StaffCode=" + szStaffCode);
                        TCT_Lib.laneController.RemoteLoginFromRTO(szStaffCode);
                    }
                    else if (method.ToUpper() == "REMOTE_LOGIN_FREE")
                    {
                        //--- v2.0.6: Remote Login เข้าโหมดไม่เก็บค่าผ่านทาง (เปิดวิ่งฟรี) - รับจากเครื่อง RTO ใน config เท่านั้น
                        string szStaffCode = (string)jParams["code"];
                        LogInfo.WriteLaneEvent($"+ From RTO => Remote Login FREE mode : StaffCode={szStaffCode}, from={szLastSenderIP}");
                        if ((TCT_Lib.szRTO_IPAddress.Trim() != "") && (szLastSenderIP != TCT_Lib.szRTO_IPAddress.Trim()))
                            LogInfo.WriteLaneEvent($"- Reject Remote Login FREE mode : sender {szLastSenderIP} is not RTO {TCT_Lib.szRTO_IPAddress}");
                        else
                            TCT_Lib.laneController.RemoteLoginFreeFromRTO(szStaffCode);
                    }
                    else if (method.ToUpper() == "REMOTE_LOGOUT")
                    {
                        Console.WriteLine("+ From RTO => Remote LogOut");
                        LogInfo.WriteLaneEvent("+ From RTO => Remote LogOut");
                        TCT_Lib.laneController.RemoteLogoutFromRTO();
                    }
                    else if (method.ToUpper() == "CHANGE_TYPE")
                    {
                        int iLaneType = (int)jParams["type"];
                        Console.WriteLine("+ From RTO => Change Lane Type : type=" + iLaneType);
                        LogInfo.WriteLaneEvent("+ From RTO => Change Lane Type : type=" + iLaneType);
                        if (TCT_Lib.laneController != null)
                            TCT_Lib.laneController.ChangeLaneType(iLaneType);
                    }
                    else if (method.ToUpper() == "CLEAR_QUEUE")
                    {
                        Console.WriteLine("+ From RTO => Clear Queue");
                        LogInfo.WriteLaneEvent($"+ From RTO => Clear Queue : Last PAN No.={TCT_Lib.szEtcLastUpdateObu}, Last Update = {TCT_Lib.dtEtcLastUpdateObu.ToString("yyyy-MM-dd HH:mm:ss")}");
                        LogInfo.WriteLog($"+ From RTO => Clear Queue : Last PAN No.={TCT_Lib.szEtcLastUpdateObu}, Last Update = {TCT_Lib.dtEtcLastUpdateObu.ToString("yyyy-MM-dd HH:mm:ss")}");
                        if (TCT_Lib.laneController != null)
                            TCT_Lib.laneController.ClearQueueProcesss();
                    }
                    else if (method.ToUpper() == "ENABLE_SECONDARY_READER")
                    {
                        Console.WriteLine("+ From RTO => Enable Secondary Reader");
                        LogInfo.WriteLaneEvent("+ From RTO => Enable Secondary Reader");
                        if (TCT_Lib.laneController != null)
                            TCT_Lib.laneController.EnableSecondaryReaderProcesss();
                    }
                    else if (method.ToUpper() == "CTRL_HPMC")
                    {
                        int iValue = (int)jParams["value"];
                        Console.WriteLine("+ FROM RTO => Control HP&MC : Value=" + iValue);
                        LogInfo.WriteLaneEvent("+ FROM RTO => Control HP&MC : Value=" + iValue);
                        if (TCT_Lib.bFreeMode)
                            LogInfo.WriteLaneEvent("- Ignore Control HP&MC : lane is in FREE mode (end it with Remote LogOff)");    // v2.0.6
                        else if (iValue == 1)
                        {
                            if (TCT_Lib.eCurLaneMode != LaneMode.HPMC)
                            {
                                if (TCT_Lib.laneController != null)
                                    TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.CLASS_9);
                                //TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.CONFIRM);
                            }
                        }
                        else
                        {
                            if (TCT_Lib.eCurLaneMode == LaneMode.HPMC)
                            {
                                TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.CLASS_9);
                                TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, (int)KeyPressedType.FORCE_RESET);
                            }
                        }
                    }
                    else if (method.ToUpper() == "ETC_LANEMODE")
                    {
                        //--- ETC Mode, control to change lane mode
                        int iEtcLaneMode    = (int)jParams["mode"];
                        int iEtcLaneState   = (int)jParams["state"];
                        LogInfo.WriteLaneEvent($"+ FROM RTO : ETC change lane mode = {iEtcLaneMode}, lane state = {iEtcLaneState}");
                        etcChangeLaneMode(iEtcLaneMode, iEtcLaneState);
                    }
                    else if (method.ToUpper() == "RDDI")
                    {
                        TCT_Lib.webSocketLane.SendRequest_DeviceInput();
                    }
                    else if (method.ToUpper() == "WRDO")
                    {
                        dynamic paramsObj   = JsonConvert.DeserializeObject(jParams.ToString());
                        int iDO_Id          = paramsObj["id"];
                        int val             = paramsObj["value"];
                        LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketServerRTO.ClassifyServerMessage : WRDO id={iDO_Id}, value={val}");
                        if ((iDO_Id == (int)Cmd_DO_Type.ALB_Open) || (iDO_Id == (int)Cmd_DO_Type.ALB_Close))
                        {
                            if (val == 1)
                            {
                                if (iDO_Id == (int)Cmd_DO_Type.ALB_Open)
                                    TCT_Lib.laneController.SendDigitalOutput_CmdALB_Open_RTO();
                                else if (iDO_Id == (int)Cmd_DO_Type.ALB_Close)
                                    TCT_Lib.laneController.SendDigitalOutput_CmdALB_Close();
                            }
                        }
                        else
                            TCT_Lib.webSocketLane.SendDigitalOutput_Controller(iDO_Id, val);
                    }
                    else if (method.ToUpper() == "WRTFI")
                    {
                        //--- v2.0.6: หน้าจอ TFI = params "state" (RTO 1.1.5) - ถ้าไม่มีใช้ "mode" ที่ RTO รุ่นเดิมส่งมาแทน state (4=เขียน, 0=ล้าง)
                        //---         ("mode" ใน WRTFI ที่ TCT ส่ง LC คือกลางวัน/กลางคืน ซึ่ง TCT คำนวณเอง) และรับ "pattern" ได้ (ไม่มี = Normal)
                        int iState          = (int?)jParams["state"] ?? (int?)jParams["mode"] ?? (int)TFI_State.StartSceen;
                        int iPattern        = (int?)jParams["pattern"] ?? (int)TFI_Pattern.Normal;
                        string szClassType  = (string)jParams["text_class"] ?? "";
                        string szFare       = (string)jParams["text_fare"] ?? "";
                        string szBalance    = (string)jParams["text_balance"] ?? "";
                        string szText1      = (string)jParams["text1"] ?? "";
                        string szText2      = (string)jParams["text2"] ?? "";
                        LogInfo.WriteLaneEvent($"+ From RTO => Write TFI : state={iState}, pattern={iPattern}, class={szClassType}, fare={szFare}, balance={szBalance}, text1={szText1}, text2={szText2}");
                        TCT_Lib.webSocketLane.OutIo_WriteTFI(iState, iPattern, szClassType, szFare, szBalance, szText1, szText2);
                    }
                    else if (method.ToUpper() == "WRCRS")
                    {
                        dynamic paramsObj   = JsonConvert.DeserializeObject(jParams.ToString());
                        string szColumn1    = paramsObj["column1"];
                        string szColumn2    = paramsObj["column2"];
                        LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketServerRTO.ClassifyServerMessage : WRCRS col1={szColumn1}, col2={szColumn2}");
                        TCT_Lib.webSocketLane.OutIo_WriteCRS(szColumn1, szColumn2);
                    }
                    else if (method.ToUpper() == "EXIT_MAINTENANCE")
                    {
                        Console.WriteLine("+ From RTO => Exit Maintenance Mode");
                        LogInfo.WriteLaneEvent("+ From RTO => Exit Maintenance Mode");
                        TCT_Lib.laneController.ExitMaintenanceMode();
                    }
                    else if (method.ToUpper() == "REQ_LANETRANSACTION")
                    {
                        dynamic paramsObj   = JsonConvert.DeserializeObject(jParams.ToString());
                        string szDateReq    = paramsObj["date"];
                        int iJobNo          = paramsObj["JobNo"];
                        int iStart_nTrx     = paramsObj["Start_nTrx"];
                        int iEnd_nTrx       = paramsObj["End_nTrx"];
                        Console.WriteLine($"+ From RTO : Request Resend LaneTrans to RTO => Date={szDateReq}, JobNo={iJobNo}, Start_nTrx={iStart_nTrx}, End_nTrx={iEnd_nTrx}");
                        LogInfo.WriteLog($"+ From RTO : Request Resend LaneTrans to RTO => Date={szDateReq}, JobNo={iJobNo}, Start_nTrx={iStart_nTrx}, End_nTrx={iEnd_nTrx}");

                        TCT_Lib.SendFromReqLaneTransactionToRTO(szDateReq, iJobNo, iStart_nTrx, iEnd_nTrx);
                        //TCT_Lib.SendFromRequestLaneTransactionToSupAdjust(szDateReq, iJobNo, iStart_nTrx, iEnd_nTrx);
                    }
                    else if (method.ToUpper() == "REQ_RESEND_LANETRANS_DC")
                    {
                        dynamic paramsObj   = JsonConvert.DeserializeObject(jParams.ToString());
                        string szDateReq    = paramsObj["date"];
                        int iJobNo          = paramsObj["JobNo"];
                        int iStart_nTrx     = paramsObj["Start_nTrx"];
                        int iEnd_nTrx       = paramsObj["End_nTrx"];
                        Console.WriteLine($"+ From RTO : Request Resend LaneTrans to DC => Date={szDateReq}, JobNo={iJobNo}, Start_nTrx={iStart_nTrx}, End_nTrx={iEnd_nTrx}");
                        LogInfo.WriteLog($"+ From RTO : Request Resend LaneTrans to DC => Date={szDateReq}, JobNo={iJobNo}, Start_nTrx={iStart_nTrx}, End_nTrx={iEnd_nTrx}");

                        TCT_Lib.SendFromReqLaneTransactionToDC(szDateReq, iJobNo, iStart_nTrx, iEnd_nTrx);
      
                        //TCT_Lib.AutoResendBeginJobToDC();
                        //TCT_Lib.AutoResendEndJobToDC();
                    }
                    else if (method.ToUpper() == "REQ_RESEND_LANETRANS_SUPADJUST")
                    {
                        dynamic paramsObj   = JsonConvert.DeserializeObject(jParams.ToString());
                        string szDateReq    = paramsObj["date"];
                        int iJobNo          = paramsObj["JobNo"];
                        int iStart_nTrx     = paramsObj["Start_nTrx"];
                        int iEnd_nTrx       = paramsObj["End_nTrx"];
                        Console.WriteLine($"+ From RTO : Request Resend LaneTrans to SupAdjust => Date={szDateReq}, JobNo={iJobNo}, Start_nTrx={iStart_nTrx}, End_nTrx={iEnd_nTrx}");
                        LogInfo.WriteLog($"+ From RTO : Request Resend LaneTrans to SupAdjust => Date={szDateReq}, JobNo={iJobNo}, Start_nTrx={iStart_nTrx}, End_nTrx={iEnd_nTrx}");

                        //TCT_Lib.SendFromReqLaneTransactionToRTO(szDateReq, iJobNo, iStart_nTrx, iEnd_nTrx);
                        TCT_Lib.SendFromRequestLaneTransactionToSupAdjust(szDateReq, iJobNo, iStart_nTrx, iEnd_nTrx);
                    }
                    else
                        LogInfo.WriteLog(LogLevel.WARN, "- LaneSocketServerRTO.ClassifyServerMessage : unknown method=" + method);
                }
                else
                    LogInfo.WriteLog(LogLevel.WARN, "- LaneSocketServerRTO.ClassifyServerMessage : message has no method/result");
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error ClassifyServerMessage : " + err.Message);
                Console.WriteLine("- Error ClassifyServerMessage : " + err.StackTrace);
                LogInfo.WriteLog("- Error ClassifyServerMessage : " + err.Message + " => Msg : " + szMsg);
                LogInfo.WriteLog("- Error ClassifyServerMessage : " + err.StackTrace + " => Msg : " + szMsg);
            }
        }
    }
}
