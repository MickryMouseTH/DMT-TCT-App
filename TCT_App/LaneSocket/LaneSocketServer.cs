using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
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
        readonly object xLockServer = new object();         // v2.0.7: laneServer is replaced only under this lock
        //--- v2.0.7: sessions that sent an RTO-only method (REQ_STATE / CURRENT_SUPERVISOR) = RTO even when its IP is not
        //---         the configured one (RTO with 2 NICs); used to send RTO data, never to allow control commands
        readonly ConcurrentDictionary<string, DateTime> xRtoSessions = new ConcurrentDictionary<string, DateTime>();
        const int RTO_SESSION_ALIVE_SEC = 60;               // RTO asks REQ_STATE every 5-15 s

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
                            SyncSend = true,
                            //--- v2.0.7: drop half-open sessions (RTO sends REQ_STATE every 5-15 s), TCP keepalive took ~10 min
                            ClearIdleSession            = true,
                            IdleSessionTimeOut          = 120,
                            ClearIdleSessionInterval    = 30,
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

            //--- v2.0.7: RTO reconnected (network blip) : the old session from the same IP can still look connected
            //---         (half-open) and data sent to it was lost although it was marked "sent to RTO". Close it.
            string szIP = SessionIP(session);
            if (IsRtoIP(szIP))
            {
                var xServer = laneServer;
                if (xServer != null)
                {
                    foreach (WebSocketSession xOld in xServer.GetAllSessions().ToList())
                    {
                        if ((xOld.SessionID != session.SessionID) && (SessionIP(xOld) == szIP))
                        {
                            LogInfo.WriteLog($"+ Close old RTO session {xOld.SessionID} (new session {session.SessionID} from {szIP})");
                            try { xOld.Close(); } catch (Exception) { }
                        }
                    }
                }
                //--- transactions sent in the last 2 minutes may have gone to the dead session : send them again
                //--- (RTO replaces a transaction it already has; same message as the normal resend)
                Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    TCT_Lib.ResendRecentLaneTransactionToRTO(120);
                });
            }

            if (TCT_Lib.laneController != null)
                TCT_Lib.laneController.SendLaneCurrentState();
        }

        //--- v2.0.7: IPv4 text of a session ("::ffff:a.b.c.d" -> "a.b.c.d")
        static string SessionIP(WebSocketSession session)
        {
            try
            {
                var ep = session?.RemoteEndPoint as IPEndPoint;
                if (ep == null)
                    return "";
                IPAddress ip = ep.Address;
                if (ip.IsIPv4MappedToIPv6)
                    ip = ip.MapToIPv4();
                return ip.ToString();
            }
            catch (Exception)
            {
                return "";
            }
        }

        static bool IsRtoIP(string szIP)
        {
            string szRto = (TCT_Lib.szRTO_IPAddress ?? "").Trim();
            return (szRto != "") && (szIP == szRto);
        }

        //--- v2.0.7: a session that is not from the RTO IP may get RTO messages only when RTO IP is not configured
        //---         or its IP is listed in RTO.AllowedIPs
        static bool IsRtoFallbackIP(string szIP)
        {
            string szRto = (TCT_Lib.szRTO_IPAddress ?? "").Trim();
            if (szRto == "")
                return true;
            foreach (string szAllowed in (TCT_Lib.szRTO_AllowedIPs ?? "").Split(','))
                if (szAllowed.Trim() != "" && szAllowed.Trim() == szIP)
                    return true;
            return false;
        }

        //--- control commands : RTO IP or RTO.AllowedIPs (RTO IP not configured = everybody, as before)
        static bool IsAllowedCommandIP(string szIP)
        {
            if (!TCT_Lib.bRTO_CommandFromRtoOnly)
                return true;
            string szRto = (TCT_Lib.szRTO_IPAddress ?? "").Trim();
            if (szRto == "" || szIP == szRto)
                return true;
            foreach (string szAllowed in (TCT_Lib.szRTO_AllowedIPs ?? "").Split(','))
                if (szAllowed.Trim() != "" && szAllowed.Trim() == szIP)
                    return true;
            return false;
        }

        //--- v2.0.7: an RTO WebSocket session is alive (TCT_Lib.bRTO_Connected uses it too: the 9093 port check alone
        //---         stopped every message to RTO when that listener died although the WebSocket was fine)
        public bool HasLiveRtoSession()
        {
            var xServer = laneServer;
            if (xServer == null)
                return false;
            try
            {
                return xServer.GetAllSessions().Any(x => x.Connected && IsRtoSession(x) &&
                                                         (DateTime.Now - x.LastActiveTime).TotalSeconds < RTO_SESSION_ALIVE_SEC);
            }
            catch (Exception)
            {
                return false;
            }
        }

        bool IsRtoSession(WebSocketSession session)
        {
            return IsRtoIP(SessionIP(session)) || xRtoSessions.ContainsKey(session.SessionID);
        }

        //--- v2.0.7: the server is restarted here (called by the 10 s network check), never from a send that failed
        public void EnsureServerRunning()
        {
            lock (xLockServer)
            {
                try
                {
                    if (laneServer != null && laneServer.State == ServerState.Running)
                        return;
                    LogInfo.WriteLog(LogLevel.WARN, $"- RTO WebSocket server not running (state={laneServer?.State.ToString() ?? "null"}), start again");
                    try
                    {
                        if (laneServer != null)
                        {
                            laneServer.Stop();
                            laneServer.Dispose();
                        }
                    }
                    catch (Exception)
                    {

                    }
                    laneServer = new WebSocketServer();
                    if (SetupSocketServer() && laneServer.Start())
                        LogInfo.WriteLog("+ Successs Start LaneSocketServer - RTO");
                    else
                        LogInfo.WriteLog("- Fail Start LaneSocketServer - RTO (retry in 10 s)");
                }
                catch (Exception err)
                {
                    LogInfo.WriteLog("- Error EnsureServerRunning : " + err.Message);
                }
            }
        }

        void LaneServer_SessionClosed(WebSocketSession session, SuperSocket.SocketBase.CloseReason value)
        {
            Console.WriteLine("+ Close session socket - RTO : " + session.Host + ", SessionID=" + session.SessionID);
            LogInfo.WriteLog("+ Close session socket - RTO : " + session.Host + ", SessionID=" + session.SessionID);
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketServerRTO.SessionClosed : remote={session.RemoteEndPoint?.ToString() ?? "null"}, reason={value}, sessions={laneServer?.SessionCount}");
            DateTime dtRemoved;
            xRtoSessions.TryRemove(session.SessionID, out dtRemoved);
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
            //--- v2.0.7: sender IP is passed with the message (was one shared field, raced between sessions)
            ClassifyServerMessage(value, session);
        }

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
                        //--- v2.0.7: an IODI with less than 16 values threw here and restarted the whole RTO server
                        while (jValues != null && jValues.Count < 16)
                            jValues.Add(0);
                        if (jValues != null)
                        {
                            jValues[14]     = TCT_Lib.bEMV_Connected ? 1 : 0;
                            jValues[15]     = TCT_Lib.iTotalQueue;
                            jValues[11]     = (TCT_Lib.iLastAvcStatus == -99) ? 1 : 0;  // 2022-01-04   For alert OB ON, not OFF
                        }
                        objParams       = obj;
                    }
                    else
                    {
                        if (obj != null && obj.Length >= 16)
                        {
                            obj[14]     = TCT_Lib.bEMV_Connected ? 1 : 0;
                            obj[15]     = TCT_Lib.iTotalQueue;
                            obj[11]     = (TCT_Lib.iLastAvcStatus == -99) ? 1 : 0;  // 2022-01-04   For alert OB ON, not OFF
                        }
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
                    var xServer = laneServer;
                    if (xServer == null)
                        return;
                    Console.WriteLine("+ Send LaneMessage to RTO : " + szJsonCmd);
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketServerRTO.SendLaneMessageToClient : method={szMethod}, sessions={xServer.SessionCount}, len={szJsonCmd.Length}");
                    if (LogInfo.IsEnabled(LogLevel.TRACE))
                        LogInfo.WriteLog(LogLevel.TRACE, "+ LaneSocketServerRTO.SendLaneMessageToClient : " + szJsonCmd);
                    foreach (WebSocketSession session in xServer.GetAllSessions().ToList())
                    {
                        //--- v2.0.7: one bad session (send timeout, closing) is closed alone, the server keeps running
                        try
                        {
                            session.Send(szJsonCmd);
                        }
                        catch (Exception err)
                        {
                            LogInfo.WriteLog($"- Error SendLaneMessage To {SessionIP(session)} : Method={szMethod}, Error={err.Message} -> close this session");
                            try { session.Close(); } catch (Exception) { }
                        }
                    }
                }
                else
                    LogInfo.WriteLog(LogLevel.TRACE, "+ LaneSocketServerRTO.SendLaneMessageToClient : IODO not forwarded");
            }
            catch (Exception err)
            {
                Console.WriteLine($"- Error SendLaneMessage To RTO : Method={szMethod}, Error={err.Message}");
                LogInfo.WriteLog($"- Error SendLaneMessage To RTO : Method={szMethod}, Error={err.Message}");
                LogInfo.WriteLog(LogLevel.DEBUG, "- LaneSocketServerRTO.SendLaneMessageToClient : stack=" + err.StackTrace);
                //--- v2.0.7: no server restart from here (EnsureServerRunning checks it every 10 s)
            }
        }

        public bool SendLaneMessageToRTO(string szMethod, object objParams)
        {
            if (!TCT_Lib.bRTO_Connected)
            {
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketServerRTO.SendLaneMessageToRTO : skip method={szMethod}, RTO not connected");
                return false;
            }

            if (TCT_Lib.szRTO_IPAddress == "" && xRtoSessions.IsEmpty)
            {
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketServerRTO.SendLaneMessageToRTO : skip method={szMethod}, RTO IP not configured");
                return false;
            }

            //--- v2.0.7: a null message was "sent" (RTO got params:null) and marked as delivered
            if (objParams == null)
            {
                LogInfo.WriteLog(LogLevel.WARN, $"- LaneSocketServerRTO.SendLaneMessageToRTO : method={szMethod} has no params, not sent");
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
                var xServer = laneServer;
                if (xServer == null)
                    return false;
                //--- v2.0.7: the RTO session that was active last (RTO polls REQ_STATE, a dead session stays behind),
                //---         RTO = configured IP (trimmed, IPv4-mapped) or a session that sent an RTO-only method
                //---         A session from the configured RTO IP always wins (1.5.7 sent only to that IP). Another session
                //---         is used only when no RTO-IP session is connected and its IP is in RTO.AllowedIPs (RTO 2nd NIC)
                //---         or no RTO IP is configured - a console polling REQ_STATE must not take the cars away from RTO.
                var xConnected = xServer.GetAllSessions().Where(x => x.Connected).ToList();
                var xSession = xConnected.Where(x => IsRtoIP(SessionIP(x)))
                                    .OrderByDescending(x => x.LastActiveTime)
                                    .FirstOrDefault()
                            ?? xConnected.Where(x => xRtoSessions.ContainsKey(x.SessionID) && IsRtoFallbackIP(SessionIP(x)))
                                    .OrderByDescending(x => x.LastActiveTime)
                                    .FirstOrDefault();
                if (xSession != null)
                {
                    string szIP = SessionIP(xSession);
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketServerRTO.SendLaneMessageToRTO : method={szMethod}, to={szIP} ({xSession.SessionID}), len={szJsonCmd.Length}");
                    if (LogInfo.IsEnabled(LogLevel.TRACE))
                        LogInfo.WriteLog(LogLevel.TRACE, "+ LaneSocketServerRTO.SendLaneMessageToRTO : " + szJsonCmd);
                    xSession.Send(szJsonCmd);
                    return true;
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
        //--- v2.0.7: methods that change the lane (accepted only from RTO, see IsAllowedCommandIP)
        static readonly HashSet<string> CONTROL_METHODS = new HashSet<string>
        {
            "CURRENT_SUPERVISOR", "CTRL_IO", "REMOTE_LOGIN", "REMOTE_LOGIN_FREE", "REMOTE_LOGOUT", "CHANGE_TYPE", "CLEAR_QUEUE",
            "ENABLE_SECONDARY_READER", "CTRL_HPMC", "ETC_LANEMODE", "WRDO", "WRTFI", "WRCRS", "EXIT_MAINTENANCE",
            "REQ_LANETRANSACTION", "REQ_RESEND_LANETRANS_DC", "REQ_RESEND_LANETRANS_SUPADJUST",
        };

        static string DescribeMethod(string szMethod)
        {
            return szMethod == "REMOTE_LOGIN_FREE" ? "Remote Login FREE mode" : szMethod;
        }

        void ClassifyServerMessage(string szMsg, WebSocketSession xSender)
        {
            string szSenderIP = SessionIP(xSender);
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

                string szMethodU = (method ?? "").ToUpper();
                if ((xSender != null) && (szMethodU == "REQ_STATE" || szMethodU == "CURRENT_SUPERVISOR" || szMethodU == "GET_SUP") &&
                    IsAllowedCommandIP(szSenderIP))
                    xRtoSessions[xSender.SessionID] = DateTime.Now;     // this session is RTO (also when its IP is another NIC)

                if ((method != null) && CONTROL_METHODS.Contains(szMethodU) && !IsAllowedCommandIP(szSenderIP))
                {
                    //--- v2.0.7: any host on the lane network could open the barrier, log out or change the lane type
                    LogInfo.WriteLaneEvent($"- Reject {DescribeMethod(szMethodU)} : sender {szSenderIP} is not RTO {TCT_Lib.szRTO_IPAddress}");
                    return;
                }

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
                        LogInfo.WriteLaneEvent($"+ From RTO => Remote Login FREE mode : StaffCode={szStaffCode}, from={szSenderIP}");
                        if ((TCT_Lib.szRTO_IPAddress.Trim() != "") && (szSenderIP != TCT_Lib.szRTO_IPAddress.Trim()))
                            LogInfo.WriteLaneEvent($"- Reject Remote Login FREE mode : sender {szSenderIP} is not RTO {TCT_Lib.szRTO_IPAddress}");
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
                        //--- v2.0.7: RTO clears the CRS with {"clear":1} (was written as null text)
                        if (((int?)jParams["clear"] ?? 0) == 1)
                        {
                            LogInfo.WriteLog(LogLevel.DEBUG, "+ LaneSocketServerRTO.ClassifyServerMessage : WRCRS clear");
                            TCT_Lib.webSocketLane.OutIo_ClearCRS();
                        }
                        else
                        {
                            string szColumn1    = (string)paramsObj["column1"] ?? "";
                            string szColumn2    = (string)paramsObj["column2"] ?? "";
                            LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketServerRTO.ClassifyServerMessage : WRCRS col1={szColumn1}, col2={szColumn2}");
                            TCT_Lib.webSocketLane.OutIo_WriteCRS(szColumn1, szColumn2);
                        }
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

                        //--- v2.0.7: on a worker (it blocked this session thread 100 ms per row, other RTO commands waited)
                        Task.Run(() => TCT_Lib.SendFromReqLaneTransactionToRTO(szDateReq, iJobNo, iStart_nTrx, iEnd_nTrx));
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
                    else if (method.ToUpper() == "GET_SUP")
                    {
                        //--- RTO sends it on connect; the supervisor itself comes with CURRENT_SUPERVISOR (nothing to do)
                        LogInfo.WriteLog(LogLevel.DEBUG, "+ LaneSocketServerRTO.ClassifyServerMessage : GET_SUP (no reply needed)");
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
