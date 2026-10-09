using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using SuperSocket.WebSocket;

namespace TCT_App.LaneSocket
{
    public class LaneSocketSupAdjust
    {
        WebSocketServer laneServer;
        //--------------------------------------------------
        public LaneSocketSupAdjust()
        {
            if (!TCT_Lib.bEnableSupAdjust)
            {
                LogInfo.WriteLog(LogLevel.DEBUG, "+ LaneSocketSupAdjust : disabled (bEnableSupAdjust=false), server not started");
                return;
            }

            Console.WriteLine("+ Start LaneSocketServer : ServerPort = " + TCT_Lib.iSupAdjust_SocketPort);
            LogInfo.WriteLog("+ Start LaneSocketServer : ServerPort = " + TCT_Lib.iSupAdjust_SocketPort);
            laneServer = new WebSocketServer();

            if (SetupSocketServer())
            {
                if (laneServer.Start())
                {
                    Console.WriteLine("+ Successs Start LaneSocketServer");
                    LogInfo.WriteLog("+ Success Connnect WebSocket LaneSocketServer");
                }
                else
                {
                    Console.WriteLine("- Fail Start LaneSocketServer");
                    LogInfo.WriteLog("- Fail Connect WebSocket LaneSocketServer");
                }
            }
        }

        protected bool SetupSocketServer()
        {
            if (!TCT_Lib.bEnableSupAdjust)
                return false;

            try
            {
                //if (laneServer.Setup(TCT_Lib.iSupAdjust_SocketPort))
                if (laneServer.Setup(
                        new RootConfig(),
                        new ServerConfig
                        {
                            Name    = "WebSocket-Sup"+TCT_Lib.iLaneID,
                            Ip      = "Any",
                            Port    = TCT_Lib.iSupAdjust_SocketPort,
                            Mode    = SocketMode.Tcp,
                            SendTimeOut = 1000,
                            SyncSend = true
                        }))
                {
                    laneServer.NewSessionConnected  += LaneServer_NewSessionConnected;
                    laneServer.NewMessageReceived   += LaneServer_NewMessageReceived;
                    laneServer.SessionClosed        += LaneServer_SessionClosed;

                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketSupAdjust.SetupSocketServer : listen Any:{TCT_Lib.iSupAdjust_SocketPort}, name=WebSocket-Sup{TCT_Lib.iLaneID}, sendTimeout=1000ms");
                    return true;
                }
                else
                {
                    LogInfo.WriteLog(LogLevel.ERROR, $"- LaneSocketSupAdjust.SetupSocketServer : Setup failed, port={TCT_Lib.iSupAdjust_SocketPort}");
                    return false;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error Setup ServerWebSocket - SupAdjust: " + err.Message);
                LogInfo.WriteLog("- Error Setup ServerWebSocket - SupAdjust : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, "- LaneSocketSupAdjust.SetupSocketServer : stack=" + err.StackTrace);
                return false;
            }
        }

        void LaneServer_NewSessionConnected(WebSocketSession session)
        {
            if (!TCT_Lib.bEnableSupAdjust)
                return;

            Console.WriteLine("+ Find new session socket - SupAdjust : IP=" + session.Host + ", SessionID=" + session.SessionID);
            LogInfo.WriteLog("+ Find new session socket - SupAdjust : IP=" + session.Host + ", SessionID=" + session.SessionID);
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketSupAdjust.NewSessionConnected : remote={session.RemoteEndPoint?.ToString() ?? "null"}, sessions={laneServer?.SessionCount}, send lane state");
            TCT_Lib.laneController.SendLaneCurrentState();
        }

        void LaneServer_SessionClosed(WebSocketSession session, SuperSocket.SocketBase.CloseReason value)
        {
            if (!TCT_Lib.bEnableSupAdjust)
                return;

            Console.WriteLine("+ Close session socket - SupAdjust : " + session.Host + ", SessionID=" + session.Host);
            LogInfo.WriteLog("+ Close session socket - SupAdjust : " + session.Host + ", SessionID=" + session.Host);
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketSupAdjust.SessionClosed : remote={session.RemoteEndPoint?.ToString() ?? "null"}, sessionId={session.SessionID}, reason={value}, sessions={laneServer?.SessionCount}");
        }

        void LaneServer_NewMessageReceived(WebSocketSession session, string value)
        {
            if (!TCT_Lib.bEnableSupAdjust)
                return;

            Console.WriteLine("+ SocketServer : " + session.Method + ", Mesage : " + value);
            if (LogInfo.IsEnabled(LogLevel.TRACE))
                LogInfo.WriteLog(LogLevel.TRACE, $"+ LaneSocketSupAdjust.NewMessageReceived : from={session.RemoteEndPoint?.ToString() ?? "null"}, msg={value}");
            ClassifyServerMessage(value);
        }

        public void SendLaneMessageToClient(string szMethod, object objParams)
        {
            if (!TCT_Lib.bEnableSupAdjust)
                return;

            try
            {
                //--- Change last DI (Treadle D -> TotalQueue)
                if (szMethod == "IODI")
                {
                    dynamic obj         = objParams;
                    if (obj is JObject)
                    {
                        JArray jValues  = obj["values"];
                        jValues[15]     = TCT_Lib.iTotalQueue;
                        objParams       = obj;
                    }
                    else
                    {
                        obj[15]         = TCT_Lib.iTotalQueue;
                        objParams       = new { values = obj };
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
                    Console.WriteLine("+ Send LaneMessage to SupAdjust : " + szJsonCmd);
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketSupAdjust.SendLaneMessageToClient : method={szMethod}, sessions={laneServer?.SessionCount}, len={szJsonCmd.Length}");
                    if (LogInfo.IsEnabled(LogLevel.TRACE))
                        LogInfo.WriteLog(LogLevel.TRACE, "+ LaneSocketSupAdjust.SendLaneMessageToClient : " + szJsonCmd);
                    foreach (WebSocketSession session in laneServer.GetAllSessions().ToList())
                        session.Send(szJsonCmd);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine($"- Error SendLaneMessage to SupAdjust : Method={szMethod}, Error={err.Message}");
                LogInfo.WriteLog($"- Error SendLaneMessage to SupAdjust : Method={szMethod}, Error={err.Message}");
                LogInfo.WriteLog(LogLevel.DEBUG, "- LaneSocketSupAdjust.SendLaneMessageToClient : stack=" + err.StackTrace);
                TCT_Lib.bSupAdjust_Connected = false;
                //----------------------------------------------
/*
                //Thread.Sleep(2000);
                LogInfo.WriteLog("+ Restart SupAdjust WebSocket");
                laneServer.Stop();
                laneServer.Dispose();
                laneServer = null;
                laneServer = new WebSocketServer();

                if (SetupSocketServer())
                {
                    if (laneServer.Start())
                        LogInfo.WriteLog("+ Successs Start LaneSocketServer-SupAdjust");
                    else
                        LogInfo.WriteLog("- Fail Start LaneSocketServer-SupAdjust");
                }
*/
            }
        }

        public bool SendLaneMessageToSupAdjust(string szMethod, object objParams)
        {
            //LogInfo.WriteLog("+ SupAdjust IP : " + TCT_Lib.szSupAdjust_IPAddress);
            if (TCT_Lib.szSupAdjust_IPAddress == "")
            {
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketSupAdjust.SendLaneMessageToSupAdjust : skip method={szMethod}, SupAdjust IP not configured");
                return false;
            }

            if (!TCT_Lib.bEnableSupAdjust)
            {
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketSupAdjust.SendLaneMessageToSupAdjust : skip method={szMethod}, SupAdjust disabled");
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
                Console.WriteLine("+ Send LaneMessage to SupAdjust : " + szJsonCmd);
                //LogInfo.WriteLog("+ Start LaneMessage to SupAdust");
                foreach (WebSocketSession session in laneServer.GetAllSessions().ToList())
                {
                    string szIP = session.RemoteEndPoint.Address.ToString();
                    //LogInfo.WriteLog("+ SupAdjust IP : " + szIP);
                    if ((szIP == TCT_Lib.szSupAdjust_IPAddress) && session.Connected)
                    {
                        session.Send(szJsonCmd);
                        LogInfo.WriteLog($"+ Success Send LaneMessage to SupAdjust (IP:{szIP}) : {szJsonCmd}");
                        return true;
                    }
                }

                //LogInfo.WriteLog("+ SupAdjust Not found IP Connected");
                LogInfo.WriteLog(LogLevel.WARN, $"- LaneSocketSupAdjust.SendLaneMessageToSupAdjust : no connected session from SupAdjust IP={TCT_Lib.szSupAdjust_IPAddress}, dropped method={szMethod}");
                return false;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error SendLaneMessageToSupAdjust : " + err.Message);
                Console.WriteLine("- Error SendLaneMessageToSupAdjust : " + err.StackTrace);
                LogInfo.WriteLog("- Error Send LaneMessage to SupAdjust : " + err.Message);
                LogInfo.WriteLog("- Error Send LaneMessage to SupAdjust : " + err.StackTrace);
                LogInfo.WriteLog("+ Restart SupAdjust WebSocket");
                LogInfo.WriteLog(LogLevel.WARN, $"- LaneSocketSupAdjust.SendLaneMessageToSupAdjust : restart SupAdjust server port={TCT_Lib.iSupAdjust_SocketPort}, method={szMethod}");

                TCT_Lib.bSupAdjust_Connected = false;
                laneServer.Stop();
                laneServer.Dispose();
                laneServer = null;
                laneServer = new WebSocketServer();

                if (SetupSocketServer())
                {
                    if (laneServer.Start())
                        LogInfo.WriteLog("+ Successs Start LaneSocketServer-SupAdjust");
                    else
                        LogInfo.WriteLog("- Fail Start LaneSocketServer-SupAdjust");
                }

                return false;
            }
        }

        //--------------------------------------------------------------
        //=== Message from SupAdjust
        void ClassifyServerMessage(string szMsg)
        {
            if (!TCT_Lib.bEnableSupAdjust)
                return;

            try
            {
                //--- Classify jrpc message and send command by evnet
                Console.WriteLine("+ Message : " + szMsg);
                dynamic msg         = JsonConvert.DeserializeObject(szMsg);
                int id              = msg["id"] ?? 0;        // SupAdjust=101
                string result       = msg["result"];
                string method       = msg["method"];
                JObject jParams     = msg["params"];
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ LaneSocketSupAdjust.ClassifyServerMessage : id={id}, method={method ?? "null"}, result={result ?? "null"} (no handler, ignored)");

                if (result != null)
                {
                    Console.WriteLine("+ Result : " + result);
                }
                else if (method != null)
                {
/*
                    if (method.ToUpper() == "REQ_STATE")
                    {
                        // Request current lane state
                        TCT_Lib.laneController.SendLaneCurrentState();
                        TCT_Lib.webSocketLane.SendRequest_DeviceInput();
                    }
                    else if (method.ToUpper() == "REQ_CURRENT_STATISTIC")
                    {
                        //--- Request current lane statistic
                        TCT_Lib.laneController.SendLaneCurrentTransStatistic();
                    }
                    else if (method.ToUpper() == "GET_SUP")
                    {
                        //-- Response current supervisor info

                    }
                    else if (method.ToUpper() == "CTRL_IO")
                    {
                        int iDeviceType = (int) jParams["id"];
                        int iValue      = (int)jParams["value"];
                        Console.WriteLine("+ From RTO => Control IO : Id=" + iDeviceType + ", Value=" + iValue);

                        TCT_Lib.webSocketLane.SendDigitalOutput_Controller(iDeviceType, iValue);
                    }
                    else if (method.ToUpper() == "TEST_IO")
                    {

                    }
                    else if (method.ToUpper() == "REMOTE_LOGIN")
                    {
                        string szStaffCode = (string)jParams["code"];
                        Console.WriteLine("+ From RTO => Remote Login : StaffCode=" + szStaffCode);
                        TCT_Lib.laneController.RemoteLoginFromRTO(szStaffCode);
                    }
                    else if (method.ToUpper() == "CHANGE_TYPE")
                    {
                        int iLaneType = (int)jParams["type"];
                        Console.WriteLine("+ From RTO => Change Lane Type : type=" + iLaneType);
                        TCT_Lib.laneController.ChangeLaneType(iLaneType);
                    }
                    else if (method.ToUpper() == "CLEAR_QUEUE")
                    {
                        Console.WriteLine("+ From RTO => Clear Queue");
                        TCT_Lib.laneController.ClearQueueProcesss();
                    }
                    else if (method.ToUpper() == "CTRL_HPMC")
                    {
                        int iValue = (int)jParams["value"];
                        Console.WriteLine("+ FROM RTO => Control HP&MC : Value=" + iValue);
                        if (iValue == 1)
                        {
                            if (TCT_Lib.eCurLaneMode != LaneMode.HPMC)
                            {
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
                    else if (method.ToUpper() == "RDDI")
                    {
                        TCT_Lib.webSocketLane.SendRequest_DeviceInput();
                    }
*/                    
                }

            }
            catch (Exception err)
            {
                Console.WriteLine("- Error ClassifyServerMessage : " + err.Message);
                Console.WriteLine("- Error ClassifyServerMessage : " + err.StackTrace);
                LogInfo.WriteLog(LogLevel.ERROR, "- Error LaneSocketSupAdjust.ClassifyServerMessage : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, "- LaneSocketSupAdjust.ClassifyServerMessage : stack=" + err.StackTrace);
            }
        }
    }
}
