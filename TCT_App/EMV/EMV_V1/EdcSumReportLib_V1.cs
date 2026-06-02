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
            Thread.Sleep(500);
            InitSocketRecvEDC();
            Process_StartSendSumReport();

        }

        public void DisposeData()
        {
            TCT_Lib.bEdcProcessing = false;

            try
            {
                if (iTimerAutoClose > 0)
                    GLib.Source.Remove(iTimerAutoClose);
                iTimerAutoClose = 0;

            }
            catch (Exception e)
            {
                Console.WriteLine("- Error : " + e.Message);
            }
        }

        private void InitSocketRecvEDC()
        {
            eventThread                 = new Thread(new ThreadStart(ThreadEventRecvServer));
            eventThread.Priority        = ThreadPriority.Normal;
            eventThread.IsBackground    = true;
            eventThread.Start();
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

                // Data buffer
                byte[] bytes        = new Byte[2048];
                //string data     = null;
                clientSocket        = recvSocket.Accept();
                int numByte         = clientSocket.Receive(bytes);
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
                LogInfo.WriteLogEDC("+ EDC SocketException (Recv Server): " + errSocket.Message);
                if (clientSocket != null)
                    clientSocket.Close();
            }
            catch (TimeoutException errTimeout)
            {
                Console.WriteLine("Warning!...ThreadEventReceTimeOut error : " + errTimeout.Message);
                LogInfo.WriteLogEDC("+ EDC TimeoutException : " + errTimeout.Message);
            }
            catch (Exception err)
            {
                Console.WriteLine("Warning!...Exception error : " + err.Message);
                LogInfo.WriteLogEDC("+ EDC Exception : " + err.Message);
                if (clientSocket != null)
                    clientSocket.Close();
            }
        }

        private void AnalysisEdcMsg(byte[] byteMsg)
        {
            try
            {
                string szResponse = Encoding.ASCII.GetString(byteMsg);
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
                DisposeData();
            }
        }

        private void Process_StartSendSumReport()
        {
            try
            {
                if (iTimerAutoClose == 0)
                    iTimerAutoClose = GLib.Timeout.Add((uint)(20 * 1000), new GLib.TimeoutHandler(AutoCloseSumReport));
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

                try
                {
                    //string szStr    = EdcLib.GenHexCharList(byteMsg);
                    //Console.WriteLine(szStr);

                    sendSocket.Connect(sendEndPoint);
                    //Thread.Sleep(1000);
                    if (sendSocket.Connected)
                    {
                        sendSocket.Send(byteMsg);

                        //Thread.Sleep(100);
                        //--- Wait Ack
                        byte[] bAck         = new byte[1];
                        int iTotalByte      = sendSocket.Receive(bAck);

                        string szResultStr  = ClassLibEDC_V1.GenHexCharList(bAck);
                        int iAck            = bAck[0];
                        if (iAck != 0x08) // Ack
                        {
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

                    return;
                    
                }
                catch (SocketException err)
                {
                    //sendSocket.Shutdown(SocketShutdown.Both);
                    sendSocket.Close();
                    Console.WriteLine("- Error Send SumReport : " + err.Message);
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
