using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TCT_App.EMV.V0;

namespace TCT_App.EMV.EMV_V1
{
    public class EdcChkHandShaking_V1
    {
        Thread eventThread;
        uint iTimerAutoClose = 0;
        Socket recvSocket;
        //---------------------------------------------------------
        public EdcChkHandShaking_V1()
        {
            LogInfo.WriteLogEDC("+------------------ Start Check Network EDC - Handshaking --------------------");

            TCT_Lib.bEdcProcessing = true;
            LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ EdcChkHandShaking_V1 : edc={TCT_Lib.szEdcIPAddress}:{TCT_Lib.iEdcSendPort}, recvPort={TCT_Lib.iEdcRecvPort}, autoClose=20000ms");
            Thread.Sleep(500);
            InitSocketRecvEDC();
            Process_StartSendHandshaking();
        }

        public void DisposeData()
        {
            TCT_Lib.bEdcProcessing = false;
            LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ DisposeData : handshaking done, timerId={iTimerAutoClose}");

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
            byte[] data             = new byte[256];

            IPAddress ipAddr        = IPAddress.Any;
            IPEndPoint recvEndPoint = new IPEndPoint(ipAddr, TCT_Lib.iEdcRecvPort);

            Socket clientSocket     = null;
            try
            {
                recvSocket                  = new Socket(recvEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                recvSocket.SendTimeout      = 2 * 1000;
                recvSocket.ReceiveTimeout   = 2 * 1000;
                recvSocket.Bind(recvEndPoint);
                // Using Listen() method we create the Client list that will want to connect to Server
                recvSocket.Listen(10);
                LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ ThreadEventRecvServer : listening 0.0.0.0:{TCT_Lib.iEdcRecvPort}, timeout=2000ms, waiting EDC reply");

                // Data buffer
                byte[] bytes        = new Byte[2048];
                //string data     = null;
                clientSocket        = recvSocket.Accept();
                LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ ThreadEventRecvServer : accepted " + ClassLibEDC.SafeEndPoint(clientSocket));
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
                LogInfo.WriteLogEDC("+ Send Ack 06 to EDC (Handshaking)");
                byte[] bAck = { 0x06 };
                clientSocket.Send(bAck);

                AnalysisEdcMsg(byteData);
                LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ ThreadEventRecvServer : reply handled, thread exit");
                //--------------------------------------------                             
            }
            catch (SocketException errSocket)
            {
                LogInfo.WriteLogEDC(LogLevel.ERROR, "+ EDC SocketException : " + errSocket.Message);
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
                LogInfo.WriteLogEDC("+ EDC Response Byte Data (Handshaking) : " + szCharList);

                //txtResponseData.Text = szCharList;
                string szRespCode = szResponse.Substring(17, 2);
                Console.WriteLine("+ Response Code : " + szRespCode);
                Console.WriteLine("-");
                LogInfo.WriteLogEDC("+ EDC Response Code (Handshaking) : " + szRespCode);
                LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ AnalysisEdcMsg : transCode={szResponse.Substring(15, 2)}, respCode={szRespCode} ({ClassLibEDC_V1.GetResponseMsg(szRespCode)})");
                if (szRespCode != "00")
                {
                    string szErrCodeMsg     = ClassLibEDC_V1.GetResponseMsg(szRespCode);
                    string szErrorShowMsg   = ClassLibEDC_V1.ShowResponseMsg(szRespCode);
                    LogInfo.WriteLogEDC($"Error Code : {szRespCode}, Error Text : " + szErrCodeMsg);
                }
                else
                {
                    objCreditCardInfo xCreditCard = ClassLibEDC_V1.EncodeFormatEDC(szResponse);
                    string szEmvTrans = xCreditCard.szRef1;
                    LogInfo.WriteLogEDC("+ EMV Transaction (Ref1) : " + szEmvTrans);
                    if (szEmvTrans.Length >= 8)
                    {
                        string szHostAvail  = szEmvTrans.Substring(0, 2);
                        string szEdcAvail   = szEmvTrans.Substring(2, 2);
                        LogInfo.WriteLogEDC("\t+ KTB Host Available : " + szHostAvail + " => " + (szHostAvail == "00" ? "KTB Host Available" : "KTB Host Not Available"));
                        LogInfo.WriteLogEDC("\t+ EDC Available : " + szEdcAvail + " => "+(szEdcAvail == "00" ? "EDC Available" : "EDC Not Available"));
                        LogInfo.WriteLogEDC("\t+ Transaction Queue : " + szEmvTrans.Substring(4, 4));
                    }
                }

                AutoCloseHandshaking();
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                LogInfo.WriteLogEDC("- Exception : "+ err.Message);
                LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ AnalysisEdcMsg : stack=" + err.StackTrace);
                DisposeData();
            }
        }

        private void Process_StartSendHandshaking()
        {
            try
            {
                if (iTimerAutoClose == 0)
                    iTimerAutoClose = GLib.Timeout.Add((uint)(20 * 1000), new GLib.TimeoutHandler(AutoCloseHandshaking));
                LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ Process_StartSendHandshaking : autoClose timerId={iTimerAutoClose} (20000ms)");
                //-------------------------------------------

                string szErrMsg = "", szReqMsg = "";
                byte[] byteMsg  = new byte[300];
                bool bResult    = ClassLibEDC_V1.GenerateHandshakingEdcMessage_V1(ref byteMsg, ref szReqMsg, ref szErrMsg);
                LogInfo.WriteLogEDC($"+ GenerateHandshakingEdcMessage : bResult={bResult}, ReqMsg={szReqMsg}, szErrMsg={szErrMsg}");
                string szStrMsg = ClassLibEDC_V1.GenHexCharList(byteMsg);
                LogInfo.WriteLogEDC("+ Handshaking Hex Message : " + szStrMsg);

                //-----------------------------------------------------------------------
                IPAddress ipAddr            = IPAddress.Parse(TCT_Lib.szEdcIPAddress);
                IPEndPoint sendEndPoint     = new IPEndPoint(ipAddr, TCT_Lib.iEdcSendPort);
                Socket sendSocket           = new Socket(sendEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                var swEdc                   = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    //string szStr    = EdcLib.GenHexCharList(byteMsg);
                    //Console.WriteLine(szStr);

                    LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ Process_StartSendHandshaking : connect {TCT_Lib.szEdcIPAddress}:{TCT_Lib.iEdcSendPort}, timeout=none");
                    sendSocket.Connect(sendEndPoint);
                    LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ Process_StartSendHandshaking : connected={sendSocket.Connected}, elapsed={swEdc.ElapsedMilliseconds}ms");
                    //Thread.Sleep(1000);
                    if (sendSocket.Connected)
                    {
                        sendSocket.Send(byteMsg);
                        LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ Process_StartSendHandshaking : sent {byteMsg?.Length ?? 0} bytes, wait ack");

                        //Thread.Sleep(100);
                        //--- Wait Ack
                        byte[] bAck         = new byte[1];
                        int iTotalByte      = sendSocket.Receive(bAck);

                        string szResultStr  = ClassLibEDC_V1.GenHexCharList(bAck);
                        int iAck            = bAck[0];
                        LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ Process_StartSendHandshaking : ack bytes={iTotalByte}, ack=0x{iAck:X2} ({ClassLibEDC.AckName(iAck)}), elapsed={swEdc.ElapsedMilliseconds}ms");
                        if (iAck != 0x09) // Ack
                        {
                            LogInfo.WriteLogEDC(LogLevel.WARN, $"- Warning Process_StartSendHandshaking : ack=0x{iAck:X2} is not expected 0x09");
                            if (iAck == 0x04)
                            {
                                LogInfo.WriteLogEDC("+ Ack Generate Handshaking - EDC busy");
                            }
                            else
                            {
                                //txtMsg.Text = @"< Error!...Ack from EDC >";
                                Console.WriteLine("- Error...Ack from EDC. ---");
                                LogInfo.WriteLogEDC("- Error Handshaking - Err Ack from EDC");
                                string szCharList2  = GenHexCharList(bAck);
                            }
                            //-----------------------------------------
                            string szCharList = GenHexCharList(bAck);
                            LogInfo.WriteLogEDC("+ EDC Handshaking : " + szCharList);
                            //-----------------------------------------
                        }

                        //sendSocket.Shutdown(SocketShutdown.Both);
                        sendSocket.Close();
                    }
                    else
                        LogInfo.WriteLogEDC(LogLevel.WARN, "- Warning Process_StartSendHandshaking : socket not connected, request not sent");

                    return;
                    
                }
                catch (SocketException err)
                {
                    //sendSocket.Shutdown(SocketShutdown.Both);
                    sendSocket.Close();
                    Console.WriteLine("- Error Send Handshaking : " + err.Message);
                    LogInfo.WriteLogEDC(LogLevel.ERROR, $"- Error Send Handshaking : {err.SocketErrorCode} {err.Message}, elapsed={swEdc.ElapsedMilliseconds}ms");
                    LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ Process_StartSendHandshaking : stack=" + err.StackTrace);
                }
                AutoCloseHandshaking();
            }
            catch (System.IO.IOException err)
            {
                Console.WriteLine("- Error IOException Send Handshaking : " + err.Message);
                LogInfo.WriteLogEDC("- Error IOException Send Handshaking : " + err.Message);
                //stream.Flush();
                AutoCloseHandshaking();
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error SendEdc_HandshakingMessage : " + err.Message);
                LogInfo.WriteLogEDC("- Error SendEdc_HandshakingMessage : " + err.Message);
                AutoCloseHandshaking();
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

        private bool AutoCloseHandshaking()
        {
            LogInfo.WriteLogEDC("+ Dispose Handshaking");
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