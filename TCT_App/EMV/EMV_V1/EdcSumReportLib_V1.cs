using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TCT_App.EMV.EMV_V1
{
    public class EdcSumReportLib_V1
    {
        Thread eventThread;
        uint iTimerAutoClose    = 0;
        Socket recvSocket;
        //---------------------------------------------------------
        public EdcSumReportLib_V1()
        { 
            LogInfo.WriteLogEDC("+------------------ Start Send EDC - SumReport --------------------");

            if (!TCT_Lib.TestConnection(TCT_Lib.szEdcIPAddress, TCT_Lib.iEdcSendPort))
            {
                LogInfo.WriteLogEDC("- Error fail to connect EDC.");
                return;
            }

            //--------------------------------------------------------
            if (TCT_Lib.iEdcMinuteIntervalClearBatch != 0)
            {
                DateTime dtLastCleatBatch   = TCT_Lib.LaneDB.GetLastClearBatch();
                TimeSpan timeSpan           = DateTime.Now.Subtract(dtLastCleatBatch);
                if (timeSpan.TotalMinutes < TCT_Lib.iEdcMinuteIntervalClearBatch)
                {
                    LogInfo.WriteLogEDC($"- Found time span is less than setting = ({TCT_Lib.iEdcMinuteIntervalClearBatch} Minutes), then skip ClearBatch process");
                    return;
                }
            }

            //--------------------------------------------------------------------------------------

            TCT_Lib.bEdcProcessing = true;
            LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ EdcSumReportLib_V1 : edc={TCT_Lib.szEdcIPAddress}:{TCT_Lib.iEdcSendPort}, recvPort={TCT_Lib.iEdcRecvPort}, clearBatchInterval={TCT_Lib.iEdcMinuteIntervalClearBatch}min, autoClose=20000ms");
            Thread.Sleep(500);
            InitSocketRecvEDC();
            Process_StartSendSumReport();

        }

        public void DisposeData()
        {
            TCT_Lib.bEdcProcessing = false;
            LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ DisposeData : sum report done, timerId={iTimerAutoClose}");

            try
            {
                if (iTimerAutoClose > 0)
                    GLib.Source.Remove(iTimerAutoClose);
                iTimerAutoClose = 0;

            }
            catch (Exception e)
            {
                Console.WriteLine("- Error : " + e.Message);
                LogInfo.WriteLogEDC(LogLevel.ERROR, "- Error DisposeData : " + e.Message);
                LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ DisposeData : stack=" + e.StackTrace);
            }
        }

        private void InitSocketRecvEDC()
        {
            eventThread                 = new Thread(new ThreadStart(ThreadEventRecvServer));
            eventThread.Priority        = ThreadPriority.Normal;
            eventThread.IsBackground    = true;
            eventThread.Start();
            LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ InitSocketRecvEDC : recv thread started");
            //eventThread.Join(10*1000);      // Timeout-10 sec
        }

        private void ThreadEventRecvServer()
        {
            byte[] data = new byte[256];

            IPAddress ipAddr        = IPAddress.Any;
            IPEndPoint recvEndPoint = new IPEndPoint(ipAddr, TCT_Lib.iEdcRecvPort);

            Socket clientSocket     = null;
            try
            {
                recvSocket                  = new Socket(recvEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                recvSocket.SendTimeout      = 5 * 1000;
                recvSocket.ReceiveTimeout   = 5 * 1000;
                recvSocket.Bind(recvEndPoint);
                // Using Listen() method we create the Client list that will want to connect to Server
                recvSocket.Listen(10);
                LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ ThreadEventRecvServer : listening 0.0.0.0:{TCT_Lib.iEdcRecvPort}, timeout=5000ms, waiting EDC reply");

                // Data buffer
                byte[] bytes        = new Byte[2048];
                //string data     = null;
                clientSocket        = recvSocket.Accept();
                LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ ThreadEventRecvServer : accepted " + V0.ClassLibEDC.SafeEndPoint(clientSocket));
                int numByte         = clientSocket.Receive(bytes);
                LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ ThreadEventRecvServer : recv {numByte} bytes");
                if (numByte == 0)
                    LogInfo.WriteLogEDC(LogLevel.WARN, "- Warning ThreadEventRecvServer : empty frame (peer closed)");
                byte[] byteData     = new byte[numByte];
                Array.Copy(bytes, 0, byteData, 0, numByte);
                string szStrMsg     = ClassLibEDC_V1.GenHexCharList(byteData);
                Console.WriteLine("Response = " + szStrMsg);
                LogInfo.WriteLogEDC("+ Recv Hex Message : " + szStrMsg);

                //Console.WriteLine("Text received -> {0} ", data);
                byte[] bAck = { 0x06 };
                LogInfo.WriteLogEDC("+ Send Ack to EDC = " + bAck[0]);
                clientSocket.Send(bAck);

                AnalysisEdcMsg(byteData);
                LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ ThreadEventRecvServer : reply handled, thread exit");
                //--------------------------------------------
/*
                recvSocket.Close();
                recvSocket.Dispose();
                recvSocket = null;
                DisposeData();
*/                               
            }
            catch (SocketException errSocket)
            {
                LogInfo.WriteLogEDC(LogLevel.ERROR, "+ EDC SocketException (Recv Server): " + errSocket.Message);
                LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ ThreadEventRecvServer : thread exit, socketError={errSocket.SocketErrorCode}");
                if (clientSocket != null)
                    clientSocket.Close();
            }
            catch (TimeoutException errTimeout)
            {
                Console.WriteLine("Warning!...ThreadEventReceTimeOut error : " + errTimeout.Message);
                LogInfo.WriteLogEDC(LogLevel.ERROR, "+ EDC TimeoutException : " + errTimeout.Message);
            }
            catch (Exception err)
            {
                Console.WriteLine("Warning!...Exception error : " + err.Message);
                LogInfo.WriteLogEDC(LogLevel.ERROR, "+ EDC Exception : " + err.Message);
                LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ ThreadEventRecvServer : stack=" + err.StackTrace);
                if (clientSocket != null)
                    clientSocket.Close();
            }
        }

        private void AnalysisEdcMsg(byte[] byteMsg)
        {
            try
            {
                string szResponse = Encoding.ASCII.GetString(byteMsg);
                LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ AnalysisEdcMsg : len={byteMsg?.Length ?? 0}");
                //Console.WriteLine("+ Result Response : " + szResponse);
                //LogInfo.WriteLogEDC("+ EDC Result Response (SumReport) : " + szResponse);

                string szCharList = ClassLibEDC_V1.GenHexCharList(Encoding.ASCII.GetString(byteMsg));
                Console.WriteLine("Byte Data : " + szCharList);
                LogInfo.WriteLogEDC("+ EDC Response Byte Data (SumReport) : " + szCharList);

                //txtResponseData.Text = szCharList;
                string szRespCode = szResponse.Substring(17, 2);
                Console.WriteLine("+ Response Code : " + szRespCode);
                Console.WriteLine("-");
                LogInfo.WriteLogEDC("+ EDC Response Code (SumReport) : " + szRespCode);
                LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ AnalysisEdcMsg : transCode={szResponse.Substring(15, 2)}, respCode={szRespCode} ({ClassLibEDC_V1.GetResponseMsg(szRespCode)})");
                if (szRespCode != "00")
                {
                    string szErrCodeMsg     = ClassLibEDC_V1.GetResponseMsg(szRespCode);
                    string szErrorShowMsg   = ClassLibEDC_V1.ShowResponseMsg(szRespCode);
                    //Console.WriteLine($"Error Code : {szRespCode}, Error Text : szErrCodeMsg");
                    LogInfo.WriteLogEDC($"Error Code : {szRespCode}, Error Text : " + szErrCodeMsg);
                }
                else
                {
                    //--- Check TransCode
                    string szTransCode = szResponse.Substring(15, 2);
                    if (szTransCode != "S1")
                    {
                        LogInfo.WriteLogEDC($"Error TransCode : {szTransCode}, This trans code is not sum report (settlement).");
                    }
                }

                AutoCloseSumReport();
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                LogInfo.WriteLogEDC("- Exception : "+ err.Message);
                LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ AnalysisEdcMsg : stack=" + err.StackTrace);
                DisposeData();
            }
        }

        private void Process_StartSendSumReport()
        {
            try
            {
                if (iTimerAutoClose == 0)
                    iTimerAutoClose = GLib.Timeout.Add((uint)(20 * 1000), new GLib.TimeoutHandler(AutoCloseSumReport));
                LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ Process_StartSendSumReport : autoClose timerId={iTimerAutoClose} (20000ms)");
                //-------------------------------------------

                string szErrMsg = "", szReqMsg = "";
                string szEdcRef1 = "", szEdcRef2 = "", szEdcRef3 = "";
                byte[] byteMsg  = new byte[300];
                bool bResult    = ClassLibEDC_V1.GenerateSumReporttEdcMessage_V1(szEdcRef1, szEdcRef2, szEdcRef3, ref byteMsg, ref szReqMsg, ref szErrMsg);
                LogInfo.WriteLogEDC($"+ GenerateEojSumReportEdcMessage : bResult={bResult}, ReqMsg={szReqMsg}, EdcRef1={szEdcRef1}, EdcRef2={szEdcRef2}, EdcRef3={szEdcRef3}, szErrMsg={szErrMsg}");
                string szStrMsg = ClassLibEDC_V1.GenHexCharList(byteMsg);
                LogInfo.WriteLogEDC("+ SumReport Hex Message : " + szStrMsg);

                //-----------------------------------------------------------------------
                IPAddress ipAddr            = IPAddress.Parse(TCT_Lib.szEdcIPAddress);
                IPEndPoint sendEndPoint     = new IPEndPoint(ipAddr, TCT_Lib.iEdcSendPort);
                Socket sendSocket           = new Socket(sendEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                var swEdc                   = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    //string szStr    = EdcLib.GenHexCharList(byteMsg);
                    //Console.WriteLine(szStr);

                    LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ Process_StartSendSumReport : connect {TCT_Lib.szEdcIPAddress}:{TCT_Lib.iEdcSendPort}, timeout=none");
                    sendSocket.Connect(sendEndPoint);
                    LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ Process_StartSendSumReport : connected={sendSocket.Connected}, elapsed={swEdc.ElapsedMilliseconds}ms");
                    //Thread.Sleep(1000);
                    if (sendSocket.Connected)
                    {
                        sendSocket.Send(byteMsg);
                        LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ Process_StartSendSumReport : sent {byteMsg?.Length ?? 0} bytes, wait ack");

                        //Thread.Sleep(100);
                        //--- Wait Ack
                        byte[] bAck         = new byte[1];
                        int iTotalByte      = sendSocket.Receive(bAck);

                        string szResultStr  = ClassLibEDC_V1.GenHexCharList(bAck);
                        int iAck            = bAck[0];
                        LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ Process_StartSendSumReport : ack bytes={iTotalByte}, ack=0x{iAck:X2} ({V0.ClassLibEDC.AckName(iAck)}), elapsed={swEdc.ElapsedMilliseconds}ms");
                        if (iAck != 0x08) // Ack
                        {
                            LogInfo.WriteLogEDC(LogLevel.WARN, $"- Warning Process_StartSendSumReport : ack=0x{iAck:X2} is not expected 0x08");
                            if (iAck == 0x04)
                            {
                                LogInfo.WriteLogEDC("+ Ack Generate SumReport - EDC busy");
                            }
                            else
                            {
                                //txtMsg.Text = @"< Error!...Ack from EDC >";
                                Console.WriteLine("- Error...Ack from EDC. ---");
                                LogInfo.WriteLogEDC("- Error SumReport - Err Ack from EDC");
                                string szCharList2  = GenHexCharList(bAck);
                            }
                            //-----------------------------------------
                            string szCharList = GenHexCharList(bAck);
                            LogInfo.WriteLogEDC("+ EDC SumReport : " + szCharList);
                            //-----------------------------------------
                        }

                        //sendSocket.Shutdown(SocketShutdown.Both);
                        sendSocket.Close();
                    }
                    else
                        LogInfo.WriteLogEDC(LogLevel.WARN, "- Warning Process_StartSendSumReport : socket not connected, request not sent");

                    return;
                    
                }
                catch (SocketException err)
                {
                    //sendSocket.Shutdown(SocketShutdown.Both);
                    sendSocket.Close();
                    Console.WriteLine("- Error Send SumReport : " + err.Message);
                    LogInfo.WriteLogEDC(LogLevel.ERROR, $"- Error Send SumReport : {err.SocketErrorCode} {err.Message}, elapsed={swEdc.ElapsedMilliseconds}ms");
                    LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ Process_StartSendSumReport : stack=" + err.StackTrace);
                }
                AutoCloseSumReport();
            }
            catch (System.IO.IOException err)
            {
                Console.WriteLine("- Error IOException Send SumReport : " + err.Message);
                LogInfo.WriteLogEDC("- Error IOException Send SumReport : " + err.Message);
                //stream.Flush();
                AutoCloseSumReport();
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error SendEdc_SumReportMessage : " + err.Message);
                LogInfo.WriteLogEDC("- Error SendEdc_SumReportMessage : " + err.Message);
                AutoCloseSumReport();
            }
        }

        public static string GenHexCharList(string szMsg)
        {
            string szStr = "";
            byte[] ascii = Encoding.ASCII.GetBytes(szMsg);
            foreach (Byte b in ascii)
                szStr += b.ToString("X2") + " ";

            return szStr;
        }

        public static string GenHexCharList(byte[] bytes)
        {
            string szStr = "";
            foreach (Byte b in bytes)
                szStr += b.ToString("X2") + " ";

            return szStr;
        }

        private bool AutoCloseSumReport()
        {
            LogInfo.WriteLogEDC("+ Dispose SumReport/ClearBatch");
            recvSocket.Close();
            recvSocket.Dispose();
            recvSocket = null;

            DisposeData();

            if (iTimerAutoClose > 0)
            {
                //iTimerAutoClose = 0;
                //GLib.Source.Remove(iTimerAutoClose);
            }

            return false;
        }
    }
}
