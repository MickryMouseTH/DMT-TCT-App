//#define __SIM__

using System;
using WebSocket4Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TCT_App.LaneSocket;
using System.Threading;
using System.Data;

namespace TCT_App
{
    public class LaneWebSocket
    {
        WebSocket ws;
        uint iTimerIdKeepAlive      = 0;
        uint iTimerIdKeepAlive_2    = 0;
        bool bFoundPong             = true;
        bool bTimerAliveRun         = false;

        public bool bConnected      = false;

        //--- Declare delegate and event
        public delegate void EventKeyPressed(KeyBoardType eKbType, int iKey);  // Type 0=LC, 1=Virtual Keyboard
        public event EventKeyPressed keyPressedEvent;

        public delegate void EventLaneAVC(int classType, int direction, int axle, int wheel, int dualwheel, int height, int error, bool bForceEndTrans=false);
        public event EventLaneAVC avcLaneEvent;

        public delegate void EventLaneDeviceOutput(int[] values);
        public event EventLaneDeviceOutput deviceOutputLaneEvent;

        public delegate void EventLaneDigitalInput(int[] values, bool bMTC=true);
        public event EventLaneDigitalInput digitalInputLaneEvent;

        public delegate void EventLaneLoginSmartCard(string strVal);
        public event EventLaneLoginSmartCard loginSmartCardLaneEvent;

        public LaneSocketServerRTO laneSocketServerRTO  = new LaneSocketServerRTO();
        public LaneSocketSupAdjust laneSockeSupAdjust   = new LaneSocketSupAdjust();

        string szLC_Token           = "1234567890";

        //--- Const Value
        const int ON                = 1;
        const int OFF               = 0;

        //--- Const String
        const string szTxt_Greeting = "ขอบคุณค่ะ";
        //----------------------------------------
        public LaneWebSocket()
        {
            LogInfo.WriteLaneEventIO("----------------------------------------------------------------");
            LogInfo.WriteLaneEventIO("");
            LogInfo.WriteLaneEventIO("");
            LogInfo.WriteLaneEventIO("====================================================");
            LogInfo.WriteLaneEventIO("======== TCT APP : Version = " + TCT_Lib.szAppVersion + " =================");
            LogInfo.WriteLaneEventIO("======== TCT APP : Build = " + TCT_Lib.szAppBuild + " =====");
            LogInfo.WriteLaneEventIO("====================================================");
        }

        static bool bCreateSocket = false;

        // v2.0.0: exponential backoff for reconnects to avoid a tight reconnect storm when
        // the LCSD is down. Scheduling via GLib.Timeout also moves StartLaneSocket() onto the
        // GTK main loop instead of running it on the WebSocket error/close thread.
        private int iReconnectAttempt = 0;
        private void ScheduleReconnect()
        {
            int delaySec = (int)Math.Min(30, Math.Pow(2, Math.Min(iReconnectAttempt, 5)));   // 1,2,4,8,16,32->cap 30
            if (delaySec < 1) delaySec = 1;
            iReconnectAttempt++;
            LogInfo.WriteLaneEventIO($"+ Schedule reconnect LaneSocket in {delaySec}s (attempt {iReconnectAttempt})");
            LogInfo.WriteLaneEventIO(LogLevel.WARN, $"- LaneWebSocket.ScheduleReconnect : LCSD {TCT_Lib.szLCSD_SocketIPAddress}:{TCT_Lib.iLCSD_SocketPort} lost, retry #{iReconnectAttempt} in {delaySec}s");
            GLib.Timeout.Add((uint)(delaySec * 1000), new GLib.TimeoutHandler(() =>
            {
                LogInfo.WriteLaneEventIO(LogLevel.DEBUG, $"+ LaneWebSocket.ScheduleReconnect : timer fired, attempt={iReconnectAttempt}");
                StartLaneSocket();
                return false;   // one-shot
            }));
        }

        public bool StartLaneSocket()
        {
            if (bCreateSocket)
            {
                LogInfo.WriteLaneEventIO(LogLevel.DEBUG, "+ LaneWebSocket.StartLaneSocket : skipped, another start is in progress");
                return false;
            }

            LogInfo.WriteLaneEvent("+ LaneSocket : Initial StartLaneSocket");
            LogInfo.WriteLaneEventIO("+ LaneSocket : Initial StartLaneSocket");
            bCreateSocket = true;
            try
            {
                LogInfo.WriteLaneEventIO(LogLevel.DEBUG, $"+ LaneWebSocket.StartLaneSocket : endpoint=ws://{TCT_Lib.szLCSD_SocketIPAddress}:{TCT_Lib.iLCSD_SocketPort}/websocket/mtc, oldSocket={(ws != null ? ws.State.ToString() : "null")}, attempt={iReconnectAttempt}");
                if (ws != null)
                {
                    ws.Opened           -= Ws_OnOpened;
                    ws.MessageReceived  -= Ws_OnMessageReceived;
                    ws.Error            -= Ws_OnError;
                    ws.Closed           -= Ws_OnClosed;

                    ws.Dispose();
                    ws = null;
                }

                //ws              = new WebSocket("ws://127.0.0.1:8084/websocket/mtc?token=1234567890", "JSONRPC");
                ws                  = new WebSocket($"ws://{TCT_Lib.szLCSD_SocketIPAddress}:{TCT_Lib.iLCSD_SocketPort}/websocket/mtc?token={szLC_Token}", "JSONRPC");
                ws.Security.AllowUnstrustedCertificate = true;

                ws.Opened           += Ws_OnOpened;
                ws.MessageReceived  += Ws_OnMessageReceived;
                ws.Error            += Ws_OnError;
                ws.Closed           += Ws_OnClosed;

                //ws.EnableAutoSendPing   = true;
                //ws.AutoSendPingInterval = 10;

                bConnected          = true;
                bFoundPong          = true;

                if (iTimerIdKeepAlive > 0)
                {
                    GLib.Source.Remove(iTimerIdKeepAlive);
                    iTimerIdKeepAlive = 0;
                }

                if (iTimerIdKeepAlive_2 > 0)
                {
                    GLib.Source.Remove(iTimerIdKeepAlive_2);
                    iTimerIdKeepAlive_2 = 0;
                }

                bTimerAliveRun = true;
                //ws.SetCredentials("admin", "", true);
                ws.Open();
                iTimerIdKeepAlive   = GLib.Timeout.Add(10000, new GLib.TimeoutHandler(Update_KeepALive));
                iTimerIdKeepAlive_2 = GLib.Timeout.Add(20000, new GLib.TimeoutHandler(Update_KeepALive_2));
                LogInfo.WriteLaneEventIO(LogLevel.DEBUG, $"+ LaneWebSocket.StartLaneSocket : open requested, keepAliveTimer={iTimerIdKeepAlive}, keepAliveTimer2={iTimerIdKeepAlive_2}");

                bCreateSocket = false;
                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                LogInfo.WriteLaneEvent("- Error Start Socket : " + err.Message);
                LogInfo.WriteLaneEventIO("- Error Start Socket : " + err.Message);
                LogInfo.WriteLaneEventIO(LogLevel.DEBUG, "- LaneWebSocket.StartLaneSocket : stack=" + err.StackTrace);
                bConnected  = false;

                bCreateSocket = false;
                return false;
            }
        }

        private bool Update_KeepALive()
        {
            bTimerAliveRun = true;
            LogInfo.WriteLaneEventIO(LogLevel.TRACE, $"+ LaneWebSocket.Update_KeepALive : tick, foundPong={bFoundPong}, connected={bConnected}");
            if (TCT_Lib.bMaintenanceMode && !TCT_Lib.bAllowSendRTO)
            {
                LogInfo.WriteLaneEventIO("- Not send update Keep alive");
                return true;
            }

            if (!bFoundPong)
            {
                LogInfo.WriteLaneEventIO("- Error Update_KeepALive => Not found PONG, then re-start socket");
                //-----------------------------------------
                StartLaneSocket();
                return true;
            }

            LogInfo.WriteLaneEventIO("+ Send KeepAlive : PING");
            if (SendSocketCmd("PING", null))
            {
                bFoundPong = false;
            }
            else
            {
                LogInfo.WriteLaneEventIO("- Fail Send Ping Cmd (SendSocketCmd)");
            }

            return true;
        }

        private bool Update_KeepALive_2()
        {
            //bTimerAliveRun = false;
            LogInfo.WriteLaneEventIO(LogLevel.TRACE, $"+ LaneWebSocket.Update_KeepALive_2 : tick, timerAliveRun={bTimerAliveRun}");
            if (!bTimerAliveRun)
            {
                LogInfo.WriteLaneEventIO("- Error not found Update_KeepAlive flag, then restart keep alive timer");
                //-----------------------------------------
                if (iTimerIdKeepAlive > 0)
                {
                    GLib.Source.Remove(iTimerIdKeepAlive);
                    iTimerIdKeepAlive = 0;
                }
                iTimerIdKeepAlive = GLib.Timeout.Add(10000, new GLib.TimeoutHandler(Update_KeepALive));
            }
            bTimerAliveRun = false;

            return true;
        }

        void Ws_OnOpened(object sender, EventArgs e)
        {
            Console.WriteLine("+ WebSocket Opened");
            LogInfo.WriteLaneEvent("+ WebSocket Opened");
            LogInfo.WriteLaneEventIO("+ WebSocket Opened");
            LogInfo.WriteLaneEventIO(LogLevel.DEBUG, $"+ LaneWebSocket.Ws_OnOpened : connected to {TCT_Lib.szLCSD_SocketIPAddress}:{TCT_Lib.iLCSD_SocketPort} after {iReconnectAttempt} retry(s), send PING + lane state");
            //ws.Send("{\"jsonrpc\": \"2.0\",\"id\": 0, \"method\":\"PING\"}");
            string szMethod     = "PING";
            Object objParams    = null;
            iReconnectAttempt   = 0;   // v2.0.0: reset backoff on successful connect
            SendSocketCmd(szMethod, objParams);
            //--- Send Cmd Lane State
            TCT_Lib.laneController.SendLaneCurrentState();
        }

        void Ws_OnError(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            Console.WriteLine(e.Exception.GetType() + ": " + e.Exception.Message + Environment.NewLine + e.Exception.StackTrace);
            LogInfo.WriteLaneEvent("- Error WebSocket : " + e.Exception.GetType() + ": " + e.Exception.Message + Environment.NewLine + e.Exception.StackTrace);
            LogInfo.WriteLaneEventIO("- Error WebSocket : " + e.Exception.GetType() + ": " + e.Exception.Message + Environment.NewLine + e.Exception.StackTrace);
            if (e.Exception.InnerException != null)
                Console.WriteLine(e.Exception.InnerException.GetType());
            LogInfo.WriteLaneEventIO(LogLevel.DEBUG, $"- LaneWebSocket.Ws_OnError : state={(ws != null ? ws.State.ToString() : "null")}, inner={e.Exception.InnerException?.Message ?? "null"}, dispose socket");

            //-----------------------------------------
            if (ws != null)
            {
                ws.Opened           -= Ws_OnOpened;
                ws.MessageReceived  -= Ws_OnMessageReceived;
                ws.Error            -= Ws_OnError;
                ws.Closed           -= Ws_OnClosed;

                ws.Dispose();
                ws = null;
            }
            LogInfo.WriteLaneEventIO("+ Restart LaneSocket from Ws_OnError Socket");
            ScheduleReconnect();   // v2.0.0: backoff instead of immediate reconnect
        }

        void Ws_OnClosed(object sender, EventArgs e)
        {
            Console.WriteLine("+ WebSocket Closed");
            LogInfo.WriteLaneEvent("+ WebSocket Closed");
            LogInfo.WriteLaneEventIO("+ WebSocket Closed");
            WebSocket4Net.ClosedEventArgs closedArgs = e as WebSocket4Net.ClosedEventArgs;
            LogInfo.WriteLaneEventIO(LogLevel.DEBUG, $"+ LaneWebSocket.Ws_OnClosed : endpoint={TCT_Lib.szLCSD_SocketIPAddress}:{TCT_Lib.iLCSD_SocketPort}, code={(closedArgs != null ? closedArgs.Code.ToString() : "n/a")}, reason={closedArgs?.Reason ?? "n/a"}");

            //-----------------------------------------
            if (ws != null)
            {
                ws.Opened           -= Ws_OnOpened;
                ws.MessageReceived  -= Ws_OnMessageReceived;
                ws.Error            -= Ws_OnError;
                ws.Closed           -= Ws_OnClosed;

                ws.Dispose();
                ws = null;
            }
            LogInfo.WriteLaneEventIO("+ Restart LaneSocket from Ws_OnClosed Socket");
            ScheduleReconnect();   // v2.0.0: backoff instead of immediate reconnect
        }

        void Ws_OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Console.WriteLine("+ WebSocket Message : " + e.Message);
            if (LogInfo.IsEnabled(LogLevel.TRACE))
                LogInfo.WriteLaneEventIO(LogLevel.TRACE, "+ LaneWebSocket.Ws_OnMessageReceived : " + (e?.Message ?? "null"));
            ClassifyLaneMessage(e.Message);
        }

        //--------------------------------------------------------------
        void ClassifyLaneMessage(string szMsg)
        {
            try
            {   
                //--- Classify jrpc message and send command by evnet
                //Console.WriteLine("+ Message : " + szMsg);
                //LogInfo.WriteLaneEventIO(szMsg);
                //TCT_Lib.LaneDB.WriteLaneEvent(szMsg);
                dynamic msg         = JsonConvert.DeserializeObject(szMsg);
                dynamic result      = msg["result"];
                string method       = msg["method"];
                JObject jParams     = msg["params"];
                //laneSocketServer.SendLaneMessageToClient(method, jParams);

                if (result != null)
                {
                    bConnected  = true;
                    bFoundPong  = true;
                    if (result is JValue)
                    {
                        string szResult = (string) result;
                        if (szResult.ToUpper() == "PONG")
                        {
                            Console.WriteLine("+ Result PONG");
                            LogInfo.WriteLaneEventIO("+ Recv KeepAlive : PONG");
                        }
                        else if (szResult.ToUpper() == "OK")
                        {
                            Console.WriteLine("+ OK");
                            LogInfo.WriteLaneEventIO(LogLevel.DEBUG, "+ LaneWebSocket.ClassifyLaneMessage : result=OK");
                        }
                        else
                        {
                            Console.WriteLine("- Unknown Result -");
                            LogInfo.WriteLaneEventIO(LogLevel.WARN, "- LaneWebSocket.ClassifyLaneMessage : unknown result=" + szResult);
                        }
                    }
                    else
                    {
                        LogInfo.WriteLaneEventIO(szMsg);
                        dynamic paramsObj   = JsonConvert.DeserializeObject(result.ToString());
                        dynamic jValue      = paramsObj["IODI"];
                        JArray jResult      = jValue["values"];
                        int[] values        = jResult.ToObject<int[]>();
                        //Console.WriteLine("+ Method IO Digital Input : Values = " + jValues);
                        LogInfo.WriteLaneEventIO(LogLevel.DEBUG, $"+ LaneWebSocket.ClassifyLaneMessage : result IODI values=[{(values == null ? "null" : string.Join(",", values))}] -> digitalInputLaneEvent + RTO");
                        digitalInputLaneEvent(values);
                        laneSocketServerRTO.SendLaneMessageToClient("IODI", values);
                    }
                }
                else if (method != null)
                {
                    LogInfo.WriteLaneEventIO(szMsg);
                        
                    //---------------------------------------------
                    if (!bFoundPong)
                    {
                        bConnected = true;
                        bFoundPong = true;
                        LogInfo.WriteLaneEventIO("+ Set recv pong (From any method).");
                    }
                    //---------------------------------------------

                    if (method.ToUpper() == "KEY")
                    {
                        //--- Keyboard is pressed
                        //JObject jParams     = msg["params"];
                        dynamic paramsObj   = JsonConvert.DeserializeObject(jParams.ToString());
                        int iKeyId          = (int)paramsObj["id"];
                        Console.WriteLine("+ Method KeyPressed : KeyId = " + iKeyId);
                        LogInfo.WriteLaneEventIO(LogLevel.DEBUG, $"+ LaneWebSocket.ClassifyLaneMessage : method=KEY, keyId={iKeyId} -> keyPressedEvent + RTO");
                        keyPressedEvent(KeyBoardType.LC, iKeyId);

                        object objParams        = new {
                                keyId           = (int)paramsObj["id"],
                                towingState     = (int)TCT_Lib.eTowingState,
                                towing_Class1   = TCT_Lib.iCurTC_Class,
                                towing_Fare1    = TCT_Lib.fCurFareAmount,
                                towing_PayType1 = TCT_Lib.xCurPayment == null ? 0 : TCT_Lib.xCurPayment.payType,
                                towing_Class2   = TCT_Lib.iTowingTC_Class,
                                towing_Fare2    = TCT_Lib.fTowingFareAmount,
                                towing_PayType2 = TCT_Lib.xTowingPayment == null ? 0 : TCT_Lib.xTowingPayment.payType
                            };
                        laneSocketServerRTO.SendLaneMessageToClient(method, objParams);
                    }
                    else if (method.ToUpper() == "SCR")
                    {
                        dynamic paramsObj   = JsonConvert.DeserializeObject(jParams.ToString());
                        string strVal       = (string)paramsObj["id"];
                        Console.WriteLine("+ Method SmartCard Reader (SCR) : Value = " + strVal);
                        LogInfo.WriteLaneEventIO(LogLevel.DEBUG, $"+ LaneWebSocket.ClassifyLaneMessage : method=SCR, len={(strVal == null ? 0 : strVal.Length)} -> loginSmartCardLaneEvent");
                        loginSmartCardLaneEvent(strVal);
                    }
                    else if (method.ToUpper() == "IODI")
                    {
                        //JObject jParams     = msg["params"];
                        dynamic paramsObj   = JsonConvert.DeserializeObject(jParams.ToString());
                        JArray jValues      = paramsObj["values"];
                        int[] values        = jValues.ToObject<int[]>();
                        //Console.WriteLine("+ Method IO Digital Input : Values = " + jValues);
                        LogInfo.WriteLaneEventIO(LogLevel.DEBUG, $"+ LaneWebSocket.ClassifyLaneMessage : method=IODI, values=[{(values == null ? "null" : string.Join(",", values))}] -> digitalInputLaneEvent + RTO");
                        digitalInputLaneEvent(values);
                        laneSocketServerRTO.SendLaneMessageToClient(method, jParams);
                    }
                    else if (method.ToUpper() == "IODO")
                    {
                        //JObject jParams     = msg["params"];
                        dynamic paramsObj   = JsonConvert.DeserializeObject(jParams.ToString());
                        JArray jValues      = paramsObj["values"];
                        int[] values        = jValues.ToObject<int[]>();
                        //Console.WriteLine("+ Method IO Digital Output : Values = " + jValues);
                        LogInfo.WriteLaneEventIO(LogLevel.DEBUG, $"+ LaneWebSocket.ClassifyLaneMessage : method=IODO, values=[{(values == null ? "null" : string.Join(",", values))}] -> deviceOutputLaneEvent");
                        deviceOutputLaneEvent(values);
                    }
                    else if (method.ToUpper() == "TFI")
                    {
                        //JObject jParams     = msg["params"];
                        dynamic paramsObj   = JsonConvert.DeserializeObject(jParams.ToString());
                        Console.WriteLine("+ Method TFI");
                        LogInfo.WriteLaneEventIO(LogLevel.DEBUG, "+ LaneWebSocket.ClassifyLaneMessage : method=TFI -> forward to RTO");
                        laneSocketServerRTO.SendLaneMessageToClient(method, jParams);
                    }
                    else if (method.ToUpper() == "CRS")
                    {
                        //JObject jParams     = msg["params"];
                        dynamic paramsObj   = JsonConvert.DeserializeObject(jParams.ToString());
                        Console.WriteLine("+ Method CRS (Classification Repeater Sign)");
                        LogInfo.WriteLaneEventIO(LogLevel.DEBUG, "+ LaneWebSocket.ClassifyLaneMessage : method=CRS -> forward to RTO");
                        laneSocketServerRTO.SendLaneMessageToClient(method, jParams);
                    }
                    else if (method.ToUpper() == "AVC")
                    {
                        //JObject jParams     = msg["params"];
                        dynamic paramsObj   = JsonConvert.DeserializeObject(jParams.ToString());
                        Console.WriteLine("+ Method AVC (Automatic Vehicle Classification)");
                        //int iClassType      = paramsObj["class"] != null ? paramsObj["class"] : paramsObj["vehicleclass"];
                        int iClassType      = paramsObj["vehicleclass"] != null ? paramsObj["vehicleclass"] : paramsObj["avcclass"];
                        //int iClassType      = paramsObj["class"];
                        int iDirection      = paramsObj["direction"];
                        int iAxle           = paramsObj["axle"];
                        int iWheel          = paramsObj["wheel"];
                        int iDualWheel      = paramsObj["dualwheel"];
                        int iHeight         = paramsObj["height"];
                        int iError          = paramsObj["error"];
                        LogInfo.WriteLaneEventIO(LogLevel.DEBUG, $"+ LaneWebSocket.ClassifyLaneMessage : method=AVC, class={iClassType}, dir={iDirection}, axle={iAxle}, wheel={iWheel}, dual={iDualWheel}, height={iHeight}, error={iError} -> avcLaneEvent + RTO");

                        avcLaneEvent(iClassType, iDirection, iAxle, iWheel, iDualWheel, iHeight, iError);
                        laneSocketServerRTO.SendLaneMessageToClient(method, jParams);
                    }
                    else if (method.ToUpper() == "STA")
                    {
                        Console.WriteLine("+ Method STA (Start AVC)");
                        LogInfo.WriteLaneEventIO(LogLevel.DEBUG, "+ LaneWebSocket.ClassifyLaneMessage : method=STA, no action");
                    }                     
                    else if (method.ToUpper() == "FOT")
                    {
                        //JObject jParams     = msg["params"];
                        dynamic paramsObj   = JsonConvert.DeserializeObject(jParams.ToString());
                        Console.WriteLine("+ Method FOT");
                        LogInfo.WriteLaneEventIO(LogLevel.DEBUG, "+ LaneWebSocket.ClassifyLaneMessage : method=FOT -> forward to RTO");
                        laneSocketServerRTO.SendLaneMessageToClient(method, jParams);
                    }
                    else if (method.ToUpper() == "SYS")
                    {
                        //JObject jParams     = msg["params"];
                        dynamic paramsObj   = JsonConvert.DeserializeObject(jParams.ToString());
                        JArray jValues      = paramsObj["values"];
                        int[] values        = jValues.ToObject<int[]>();
                        LogInfo.WriteLaneEventIO(LogLevel.DEBUG, $"+ LaneWebSocket.ClassifyLaneMessage : method=SYS (LCSD restart), values=[{(values == null ? "null" : string.Join(",", values))}] -> alarm LCSD_RESET");
                        //--- Check SCR
                        TCT_Lib.bMTS_SCR    = values[9] == 1;

                        //------------------------------------------------------------
                        int iAlarmID = (int)TCT_Lib.LaneDB.WriteLaneAlarmRTO(AlarmDevice.LCSD_RESET, AlarmType.Occurred, $"LCSD Restart", "");
                        TCT_Lib.SendLaneAlarmObjToRTO(iAlarmID);
                        LogInfo.WriteLaneEventIO(LogLevel.DEBUG, $"+ LaneWebSocket.ClassifyLaneMessage : SYS alarmId={iAlarmID}, bMTS_SCR={TCT_Lib.bMTS_SCR}");
                        //--------------------------------------------------------------

                        /*
                        {"jsonrpc":"2.0","id":0,"method":"SYS","params":{"values":[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0]}}

                        export const s_sys_types = [
                            {id: 0, name: 'Reserve'},
                            {id: 1, name: 'Reserve'},
                            {id: 2, name: 'Reserve'},
                            {id: 3, name: 'Reserve'},
                            {id: 4, name: 'Reserve'},
                            {id: 5, name: 'Reserve'},
                            {id: 6, name: 'Reserve'},
                            {id: 7, name: 'BIS'},
                            {id: 8, name: 'Keyboard'},
                            {id: 9, name: 'SCR'},
                            {id: 10, name: 'LCIO/DI'},
                            {id: 11, name: 'LCIO/DO'},
                            {id: 12, name: 'CSR'},
                            {id: 13, name: 'TFI'},
                            {id: 14, name: 'Reserve'},
                            {id: 15, name: 'Reserve'}
                        ]
                        */
                    }
                    else
                    {
                        Console.WriteLine("- Unknown Method -");
                        LogInfo.WriteLaneEventIO(LogLevel.WARN, "- LaneWebSocket.ClassifyLaneMessage : unknown method=" + method);
                    }
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error ClassifyLaneMessage : " + err.Message);
                Console.WriteLine("- Error ClassifyLaneMessage : " + err.StackTrace);
                LogInfo.WriteLaneEventIO("- Error Msg : " + szMsg);
                LogInfo.WriteLaneEventIO("- Error ClassifyLaneMessage : " + err.Message);
                LogInfo.WriteLaneEventIO(LogLevel.DEBUG, "- LaneWebSocket.ClassifyLaneMessage : stack=" + err.StackTrace);
            }
        }

        public void SendRequest_DigitalInputOutput()
        {
            string szMethod     = "RDIO";
            Object objParams    = null;
            SendSocketCmd(szMethod, objParams);
        }

        public void SendRequest_DeviceInput()
        {
            string szMethod     = "RDDI";
            Object objParams    = null;
            SendSocketCmd(szMethod, objParams);
        }

        //--- Digital Output
        public void SendDigitalOutput_Controller(int iDO_Id, int iVal)
        {
            LogInfo.WriteLaneEventIO(LogLevel.DEBUG, $"+ LaneWebSocket.SendDigitalOutput_Controller : id={iDO_Id}, value={iVal}, maint={TCT_Lib.bMaintenanceMode}, laneType={TCT_Lib.eCurLaneType}, openALB_RTO={TCT_Lib.bStatusOpenALB_RTO}");
            if (!TCT_Lib.bMaintenanceMode)
            {
                //--- Check If current mode is ETC, this process will skip
                if (TCT_Lib.eCurLaneType == LaneType.ETC)
                {
                    if (iDO_Id == 6)
                        TCT_Lib.etcRabbitMQ.Send_ControlOutputDevice_ToETC("OverheadTrafficLight", iVal == 1);
                    if ((iDO_Id != 4) || !TCT_Lib.bStatusOpenALB_RTO)
                        return;
                }
                else if (TCT_Lib.eCurLaneType == LaneType.DUAL)
                {
                    // In Dual mode, if pay with card, ETC will control IO
                    //if (TCT_Lib.bEtcCtrlIO)
                    if (iDO_Id == 6)
                        TCT_Lib.etcRabbitMQ.Send_ControlOutputDevice_ToETC("OverheadTrafficLight", iVal == 1);
                    if ((iDO_Id != 4) || !TCT_Lib.bStatusOpenALB_RTO)
                        return;
                }
            }

            //==================================================================
            string szMethod     = "WRDO";
            Object objParams    = new { id = iDO_Id, value = iVal };
            SendSocketCmd(szMethod, objParams);
            Thread.Sleep(10);
            //----------------------------------------------------------
            //--- Simulate DigitalInput LC
#if __SIM__
            if ((iDO_Id == (int)Cmd_DO_Type.ALB_Open) && (iVal == ON))
                SendDeviceInput_Controller((int)Cmd_DI_Type.ALB, ON);
            else if ((iDO_Id == (int)Cmd_DO_Type.ALB_Close) && (iVal == ON))
                SendDeviceInput_Controller((int)Cmd_DI_Type.ALB, OFF);
            else if (iDO_Id == (int)Cmd_DO_Type.LTL)
                SendDeviceInput_Controller((int)Cmd_DI_Type.LTL, iVal);
            else if (iDO_Id == (int)Cmd_DO_Type.OTL_RedGreen)
                SendDeviceInput_Controller((int)Cmd_DI_Type.OTL, iVal);
            else if (iDO_Id == (int)Cmd_DO_Type.Siren)
                SendDeviceInput_Controller((int)Cmd_DI_Type.Siren, iVal);
#endif                               
        }

        //--- Simulate Digital Input
        //--- Digital Input
        public void SendDeviceInput_Controller(int iDI_Id, int iVal)
        {
#if __SIM__
            string szMethod     = "WRDI";
            Object objParams    = new { id=iDI_Id, value=iVal };
            SendSocketCmd(szMethod, objParams);
#endif
        }

        public void OutIo_RBell()
        {
            LogInfo.WriteLaneEventIO(LogLevel.DEBUG, "+ LaneWebSocket.OutIo_RBell : send RBELL");
            LogInfo.WriteLaneEvent("RING BELL (GONG)");
            string szMethod     = "RBELL";
            Object objParams    = null;
            SendSocketCmd(szMethod, objParams);
        }

        //--- Output TFI
        public void OutIo_WriteTFI(int iState, int iPattern, string szClassType, string szFare, string szBalance, string szText1, string szText2=szTxt_Greeting)
        {
            try
            {
                LogInfo.WriteLaneEventIO(LogLevel.DEBUG, $"+ LaneWebSocket.OutIo_WriteTFI : state={iState}, pattern={iPattern}, class={szClassType}, fare={szFare}, maint={TCT_Lib.bMaintenanceMode}, laneType={TCT_Lib.eCurLaneType}, etcCtrlIO={TCT_Lib.bEtcCtrlIO}");
                if (!TCT_Lib.bMaintenanceMode)
                {
                    //--- Check If current mode is ETC, this process will skip
                    if (TCT_Lib.eCurLaneType == LaneType.ETC)
                        return;
                    else if (TCT_Lib.eCurLaneType == LaneType.DUAL)
                    {
                        // In Dual mode, if pay with card, ETC will control IO
                        if (TCT_Lib.bEtcCtrlIO)
                            return;
                    }

                    //==================================================================
                    if (szText2 != szTxt_Greeting)
                        LogInfo.WriteLaneEvent($"TFI : state={iState}, pattern={iPattern}, class={szClassType}, fare={szFare}, text1={szText1}, text2={szText2}");
                    string szMethod     = "WRTFI";
                    Object objParams    = new { mode = GetDayNightMode(), state = iState, pattern = iPattern, brightness = 0, iodo_ltl = 0, iodo_beacon = 0, iodo_siren = 0,
                            attr_class = 0, attr_fare = 0, attr_balance = 0, attr_text1 = 0, attr_text2 = 0, font_class = 0, font_fare = 0,
                            font_balance = 0, font_text1 = 0, font_text2 = 0, 
                            text_class = szClassType, text_fare = szFare, text_balance = szBalance, text1 = szText1, text2 = szText2 ,
                            unittype_fare = "", unittype_balance = "" };
                    SendSocketCmd(szMethod, objParams);
                }
                else
                {
    /*
                    //--- 2020-11-10  Change from maintenance screen to normal screen
                    string szMethod     = "WRTFI";

                    if ((iState == (int)TFI_State.Class_Fare) ||
                        (iState == (int)TFI_State.Class_Fare_Payment) ||
                        (iState == (int)TFI_State.CloseLane))
                    {
                        Object objParams    = new { mode = GetDayNightMode(), state = iState, pattern = iPattern, brightness = 0, iodo_ltl = 0, iodo_beacon = 0, iodo_siren = 0,
                             attr_class = 0, attr_fare = 0, attr_balance = 0, attr_text1 = 0, attr_text2 = 0, font_class = 0, font_fare = 0,
                            font_balance = 0, font_text1 = 0, font_text2 = 0, 
                             text_class = szClassType, text_fare = szFare, text_balance = szBalance, text1 = szText1, text2 = szText2 ,
                             unittype_fare = "", unittype_balance = "" };
                        SendSocketCmd(szMethod, objParams);
                    }
                    else
                    {
                        Object objParams    = new { mode = GetDayNightMode(), state = (int)TFI_State.Maintenance, pattern = (int)TFI_Pattern.Normal, brightness = 0, iodo_ltl = 0, iodo_beacon = 0, iodo_siren = 0,
                             attr_class = 0, attr_fare = 0, attr_balance = 0, attr_text1 = 0, attr_text2 = 0, font_class = 0, font_fare = 0,
                            font_balance = 0, font_text1 = 0, font_text2 = 0, 
                             text_class = szClassType, text_fare = szFare, text_balance = szBalance, text1 = szText1, text2 = szText2 ,
                             unittype_fare = "", unittype_balance = "" };
                        SendSocketCmd(szMethod, objParams);
                    }
    */
                    //==================================================================
                    string szMethod     = "WRTFI";
                    Object objParams    = new { mode = GetDayNightMode(), state = iState, pattern = iPattern, brightness = 0, iodo_ltl = 0, iodo_beacon = 0, iodo_siren = 0,
                         attr_class = 0, attr_fare = 0, attr_balance = 0, attr_text1 = 0, attr_text2 = 0, font_class = 0, font_fare = 0,
                        font_balance = 0, font_text1 = 0, font_text2 = 0, 
                         text_class = szClassType, text_fare = szFare, text_balance = szBalance, text1 = szText1, text2 = szText2 ,
                         unittype_fare = "", unittype_balance = "" };
                    SendSocketCmd(szMethod, objParams);
                }
            }
            catch (Exception err)
            {
                LogInfo.WriteLaneEvent("- Fail OutIo_WriteTFI : " + err.Message);
                LogInfo.WriteLaneEventIO("- Fail OutIo_WriteTFI : " + err.Message);
                LogInfo.WriteLaneEventIO(LogLevel.DEBUG, "- LaneWebSocket.OutIo_WriteTFI : stack=" + err.StackTrace);
            }
        }

        private int GetDayNightMode()
        {
            string[] szDays     = TCT_Lib.szDayTimeMode.Split(':');
            string[] szNights   = TCT_Lib.szNightTimeMode.Split(':');

            int iDayHour    = int.Parse(szDays[0]);
            int iDayMin     = int.Parse(szDays[1]);

            int iNightHour  = int.Parse(szNights[0]);
            int iNightMin   = int.Parse(szNights[1]);

            int iCurHour    = DateTime.Now.Hour;
            int iCurMin     = DateTime.Now.Minute;

            if ((iCurHour > iDayHour) && (iCurHour < iNightHour))
                return 0;
            if ((iCurHour > iNightHour) || (iCurHour < iDayHour))
                return 1;

            if (iCurHour == iDayHour)
                return (iCurMin >= iDayMin) ? 0 : 1;
            if (iCurHour == iNightHour)
                return (iCurMin >= iNightMin) ? 1 : 0;

            return 0;
        }

        public void OutIo_ClearTFI()
        {
            LogInfo.WriteLaneEventIO(LogLevel.DEBUG, $"+ LaneWebSocket.OutIo_ClearTFI : maint={TCT_Lib.bMaintenanceMode}, laneType={TCT_Lib.eCurLaneType}, etcCtrlIO={TCT_Lib.bEtcCtrlIO}");
            if (!TCT_Lib.bMaintenanceMode)
            {
                //---Check If current mode is ETC, this process will skip
                if (TCT_Lib.eCurLaneType == LaneType.ETC)
                    return;
                else if (TCT_Lib.eCurLaneType == LaneType.DUAL)
                {
                    // In Dual mode, if pay with card, ETC will control IO
                    if (TCT_Lib.bEtcCtrlIO)
                        return;
                }
            }

            //==================================================================
            try
            {
                LogInfo.WriteLaneEvent("Clear TFI");
                string szMethod     = "WRTFI";
                Object objParams    = new { clear = 1 };
                SendSocketCmd(szMethod, objParams);
            }
            catch (Exception err)
            {
                LogInfo.WriteLaneEventIO("- Error OutIo_ClearTFI : " + err.Message);
            }
        }

        public void OutIo_ResetTFI()
        {
            LogInfo.WriteLaneEventIO(LogLevel.DEBUG, $"+ LaneWebSocket.OutIo_ResetTFI : maint={TCT_Lib.bMaintenanceMode}, laneType={TCT_Lib.eCurLaneType}, etcCtrlIO={TCT_Lib.bEtcCtrlIO}");
            if (!TCT_Lib.bMaintenanceMode)
            {
                //---Check If current mode is ETC, this process will skip
                if (TCT_Lib.eCurLaneType == LaneType.ETC)
                    return;
                else if (TCT_Lib.eCurLaneType == LaneType.DUAL)
                {
                    // In Dual mode, if pay with card, ETC will control IO
                    if (TCT_Lib.bEtcCtrlIO)
                        return;
                }
            }

            //==================================================================
            LogInfo.WriteLaneEvent("+ Reset TFI");
            string szMethod     = "WRTFI";
            Object objParams    = new { reset = 1 };
            SendSocketCmd(szMethod, objParams);
        }

        //--- Output CRS
        public void OutIo_WriteCRS(string szCol1, string szCol2)
        {
            LogInfo.WriteLaneEventIO(LogLevel.DEBUG, $"+ LaneWebSocket.OutIo_WriteCRS : col1={szCol1}, col2={szCol2}, laneMode={TCT_Lib.eCurLaneMode}, maint={TCT_Lib.bMaintenanceMode}, laneType={TCT_Lib.eCurLaneType}, etcCtrlIO={TCT_Lib.bEtcCtrlIO}");
            if (!TCT_Lib.bMaintenanceMode)
            {
                //---Check If current mode is ETC, this process will skip
                if (TCT_Lib.eCurLaneType == LaneType.ETC)
                    return;
                else if (TCT_Lib.eCurLaneType == LaneType.DUAL)
                {
                    // In Dual mode, if pay with card, ETC will control IO
                    if (TCT_Lib.bEtcCtrlIO)
                        return;
                }
            }

            //----------------------------------------------------------------
            try
            {
                if (TCT_Lib.eCurLaneMode == LaneMode.CLOSED)
                {
                    LogInfo.WriteLaneEvent($"CRS : Clear CRS LaneMode=>Close");
                    string szMethod2    = "WRCRS";
                    Object objParams2   = new { relay = 0, reset = 0, clear = 0, column1 = "", column2 = "" };
                    SendSocketCmd(szMethod2, objParams2);
                    return;
                }

                //==================================================================
                LogInfo.WriteLaneEvent($"CRS : {szCol1}, {szCol2}");
                string szMethod     = "WRCRS";
                Object objParams    = new { relay = GetDayNightMode(), reset = 0, clear = 0, column1 = szCol1, column2 = szCol2 };
                SendSocketCmd(szMethod, objParams);
            }
            catch (Exception err)
            {
                LogInfo.WriteLaneEventIO("- Error OutIo_WriteCRS : " + err.Message);
            }
        }

        public void OutIo_WriteCRS_EndJob()
        {
            LogInfo.WriteLaneEventIO(LogLevel.DEBUG, $"+ LaneWebSocket.OutIo_WriteCRS_EndJob : maint={TCT_Lib.bMaintenanceMode}, laneType={TCT_Lib.eCurLaneType}, etcCtrlIO={TCT_Lib.bEtcCtrlIO}");
            if (!TCT_Lib.bMaintenanceMode)
            {
                //---Check If current mode is ETC, this process will skip
                if (TCT_Lib.eCurLaneType == LaneType.ETC)
                    return;
                else if (TCT_Lib.eCurLaneType == LaneType.DUAL)
                {
                    // In Dual mode, if pay with card, ETC will control IO
                    if (TCT_Lib.bEtcCtrlIO)
                        return;
                }
            }
            
            //==================================================================
            try
            {
                LogInfo.WriteLaneEvent($"CRS : OutIo_WriteCRS_EndJob");
                string szMethod     = "WRCRS";
                Object objParams    = new { relay = 0, reset = 0, clear = 0, column1 = "", column2 = "" };
                SendSocketCmd(szMethod, objParams);
            }
            catch (Exception err)
            {
                LogInfo.WriteLaneEventIO("- Error OutIo_WriteCRS_EndJob : " + err.Message);
            }
        }

        public void OutIo_ClearCRS()
        {
            LogInfo.WriteLaneEventIO(LogLevel.DEBUG, $"+ LaneWebSocket.OutIo_ClearCRS : maint={TCT_Lib.bMaintenanceMode}, laneType={TCT_Lib.eCurLaneType}, etcCtrlIO={TCT_Lib.bEtcCtrlIO}");
            if (!TCT_Lib.bMaintenanceMode)
            {
                //---Check If current mode is ETC, this process will skip
                if (TCT_Lib.eCurLaneType == LaneType.ETC)
                    return;
                else if (TCT_Lib.eCurLaneType == LaneType.DUAL)
                {
                    // In Dual mode, if pay with card, ETC will control IO
                    if (TCT_Lib.bEtcCtrlIO)
                        return;
                }
            }

            //==================================================================
            try
            {
                LogInfo.WriteLaneEvent("Clear CRS");
                string szMethod     = "WRCRS";
                Object objParams    = new { clear = 1 };
                SendSocketCmd(szMethod, objParams);
            }
            catch (Exception err)
            {
                LogInfo.WriteLaneEventIO("- Error OutIo_ClearCRS : " + err.Message);
            }
        }

        public void OutIo_ResetCRS()
        {
            LogInfo.WriteLaneEventIO(LogLevel.DEBUG, $"+ LaneWebSocket.OutIo_ResetCRS : maint={TCT_Lib.bMaintenanceMode}, laneType={TCT_Lib.eCurLaneType}, etcCtrlIO={TCT_Lib.bEtcCtrlIO}");
            if (!TCT_Lib.bMaintenanceMode)
            {
                //---Check If current mode is ETC, this process will skip
                if (TCT_Lib.eCurLaneType == LaneType.ETC)
                    return;
                else if (TCT_Lib.eCurLaneType == LaneType.DUAL)
                {
                    // In Dual mode, if pay with card, ETC will control IO
                    if (TCT_Lib.bEtcCtrlIO)
                        return;
                }
            }

            //==================================================================
            try
            {
                string szMethod     = "WRCRS";
                Object objParams    = new { reset = 1 };
                SendSocketCmd(szMethod, objParams);
            }
            catch (Exception err)
            {
                LogInfo.WriteLaneEventIO("- Error OutIo_ResetCRS : " + err.Message);
            }
        }

        public void SendVirtualKeyboard(int iKeyId, int type=0)     // Type: 0=Touch, 1=Key
        {
            /*
                        string szMethod     = "KEY";
                        Object objParams    = new { id = iKeyId };
                        SendSocketCmd(szMethod, objParams);
            */
            if (type == 0)
                LogInfo.WriteLaneEvent("+ Keyboard (Touch) : " + TCT_Lib.KeyMapName(iKeyId));
            else
                LogInfo.WriteLaneEvent("+ Keyboard (Key) : " + TCT_Lib.KeyMapName(iKeyId));

            LogInfo.WriteLaneEventIO(LogLevel.DEBUG, $"+ LaneWebSocket.SendVirtualKeyboard : keyId={iKeyId}, type={type} -> laneController + RTO");
            TCT_Lib.laneController.LaneSocket_KeyPressedEvent(KeyBoardType.VK, iKeyId);
            string szMethod     = "KEY";

            object objParams    = new {
                        keyId           = iKeyId,
                        towingState     = (int)TCT_Lib.eTowingState,
                        towing_Class1   = TCT_Lib.iCurTC_Class,
                        towing_Fare1    = TCT_Lib.fCurFareAmount,
                        towing_PayType1 = TCT_Lib.xCurPayment == null ? 0 : TCT_Lib.xCurPayment.payType,
                        towing_Class2   = TCT_Lib.iTowingTC_Class,
                        towing_Fare2    = TCT_Lib.fTowingFareAmount,
                        towing_PayType2 = TCT_Lib.xTowingPayment == null ? 0 : TCT_Lib.xTowingPayment.payType
                    };
            laneSocketServerRTO.SendLaneMessageToClient(szMethod, objParams);
        }

        public void SendChangeSupervisor()
        {
            LogInfo.WriteLog("+ Send change supervisor to LC : Supervisor=" + TCT_Lib.szSupervisorName);
            LogInfo.WriteLaneEvent("+ Send change supervisor to LC : Supervisor=" + TCT_Lib.szSupervisorName);
            string szMethod     = "WRJOB";
            Object objParams    = new { supervisor_id = TCT_Lib.szSupervisorID, supervisor_name = TCT_Lib.szSupervisorName, lane_type = (int)TCT_Lib.eCurLaneType, lane_mode = (int)TCT_Lib.eCurLaneMode, jobno = ((TCT_Lib.iSupervisorShiftID%99)+1) };
            SendSocketCmd(szMethod, objParams);
        }

        public void SendCollectorJob(int iJobState)    // 0=EOJ, 1=BOJ
        {
            LogInfo.WriteLog($"+ Send JobState to LC : State={iJobState}");
            LogInfo.WriteLaneEvent($"+ Send JobState to LC : State={iJobState}");
            string szMethod     = "WRJOB";
            Object objParams    = new { job_state = iJobState, collector_id = TCT_Lib.szStaffID, collector_name = TCT_Lib.szStaffName, lane_type = (int)TCT_Lib.eCurLaneType, lane_mode = (int)TCT_Lib.eCurLaneMode, jobno = TCT_Lib.iJobNo };
            SendSocketCmd(szMethod, objParams);
        }

        public void SendCollectorAutoEndJob(int iStaffShiftID)
        {
            LogInfo.WriteLog($"+ Send AutoEndShift JobState to LC : StaffShiftID={iStaffShiftID}");
            LogInfo.WriteLaneEvent($"+ Send AutoEndShift JobState to LC : StaffShiftID={iStaffShiftID}");

            string szQuery = "SELECT * FROM StaffShift " +
                            "WHERE StaffShiftID=" + iStaffShiftID;
            DataTable dt    = TCT_Lib.LaneDB.List(szQuery);
            if (dt.Rows.Count == 0)
            {
                LogInfo.WriteLog($"- Fail not found StaffShiftID={iStaffShiftID}");
                return;
            }

            string szMethod = "WRJOB";
            Object objParams = new { job_state = 0, collector_id = int.Parse(dt.Rows[0]["StaffID"].ToString()), collector_name = dt.Rows[0]["StaffName"].ToString(), lane_type = int.Parse(dt.Rows[0]["LaneType"].ToString()), lane_mode = (int)TCT_Lib.eCurLaneMode, jobno = int.Parse(dt.Rows[0]["JobNo"].ToString()) };
            SendSocketCmd(szMethod, objParams);
        }

        //-------------------------------------------------------------------
        private bool SendSocketCmd(string szMethod, Object objParams)
        {
            if (szMethod == null)
            {
                LogInfo.WriteLaneEventIO(LogLevel.WARN, "- LaneWebSocket.SendSocketCmd : method is null, not sent");
                return false;
            }

            if (ws == null)
            {
                LogInfo.WriteLaneEvent("- Error WebSocket disconnect LC");
                LogInfo.WriteLaneEventIO("- Error WebSocket disconnect LC");
                return false;
            }

            if (ws.State == null)
            {
                LogInfo.WriteLaneEvent("- Error WebSocket State is null (LC)");
                LogInfo.WriteLaneEventIO("- Error WebSocket State is null (LC)");
                return false;
            }

            try
            {
                //lock(ws)
                {
                    if (ws.State == WebSocketState.Closed || ws.State == WebSocketState.None || ws.State == WebSocketState.Connecting)
                    {
                        LogInfo.WriteLaneEventIO(szMethod == "PING" ? LogLevel.TRACE : LogLevel.WARN, $"- LaneWebSocket.SendSocketCmd : socket not open (state={ws.State}), dropped method={szMethod}{(ws.State != WebSocketState.Connecting ? ", re-open" : "")}");
                        if (ws.State != WebSocketState.Connecting) 
                            ws.Open();
                    }
                    else
                    {
                        string szJsonCmd = "";
                        if (szMethod == "RDDI")
                        {
                            szJsonCmd = JsonConvert.SerializeObject(
                                new
                                {
                                    jsonrpc = "2.0",
                                    id      = 0,
                                    method  = szMethod
                                }
                            );
                        }
                        else
                        {
                            szJsonCmd = JsonConvert.SerializeObject(
                                new
                                {
                                    jsonrpc = "2.0",
                                    id      = 0,
                                    method  = szMethod,
                                    @params = objParams,
                                }
                            );
                        }
                        if (szMethod == "WRJOB")
                            LogInfo.WriteLaneEventIO("Send JOB : " + szJsonCmd);
                        //Console.WriteLine("+ Send Socket Cmd : " + szJsonCmd);
                        if (szMethod != "PING")
                            LogInfo.WriteLaneEventIO(LogLevel.DEBUG, $"+ LaneWebSocket.SendSocketCmd : method={szMethod}, len={szJsonCmd.Length}");
                        if (LogInfo.IsEnabled(LogLevel.TRACE))
                            LogInfo.WriteLaneEventIO(LogLevel.TRACE, "+ LaneWebSocket.SendSocketCmd : " + szJsonCmd);
                        ws.Send(szJsonCmd);

                        //Console.WriteLine("+ Send Socket Cmd : " + szJsonCmd);
                        //ws.Send(szJsonCmd);
                    }
                }
                //Console.WriteLine("+ Send Socket Cmd : " + szJsonCmd);
                //ws.Send(szJsonCmd);

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                LogInfo.WriteLaneEvent("- Error Send Socket Cmd : " + err.Message);
                LogInfo.WriteLaneEventIO("- Error Send Socket Cmd : " + err.Message);
                LogInfo.WriteLaneEventIO(LogLevel.DEBUG, $"- LaneWebSocket.SendSocketCmd : method={szMethod}, stack={err.StackTrace}");
                return false;
            }
        }

        public bool SendPingCmd()
        {
            try
            {
                string szJsonCmd = JsonConvert.SerializeObject(
                    new
                    {
                        jsonrpc = "2.0",
                        id      = 0,
                        method  = "PING",
                    }
                );
                //Console.WriteLine("+ Send Socket Cmd : " + szJsonCmd);
                LogInfo.WriteLaneEventIO(LogLevel.TRACE, "+ LaneWebSocket.SendPingCmd : " + szJsonCmd);
                ws.Send(szJsonCmd);

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                LogInfo.WriteLaneEvent("- Fail Send Ping Cmd : " + err.Message);
                LogInfo.WriteLaneEventIO("- Fail Send Ping Cmd : " + err.Message);
                LogInfo.WriteLaneEventIO(LogLevel.DEBUG, "- LaneWebSocket.SendPingCmd : stack=" + err.StackTrace);
                bConnected = false;

                return false;
            }
        }
    }
}
