using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TCT_App.EMV.V0
{
    public class EdcSumReportLib
    {
        TcpClient client;
        NetworkStream stream;

        Thread eventThread;
        private TcpListener netServer;
        NetworkStream sendStream    = null;
        uint iTimerAutoClose        = 0;
        //---------------------------------------------------------
        public EdcSumReportLib()
        {
            LogInfo.WriteLogEDC("");
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

            if (InitSocketSendEDC())
            {
                InitSocketRecvEDC();
                Process_StartSendSumReport();
            }

            //DisposeData();
        }

        public void DisposeData()
        {
            TCT_Lib.bEdcProcessing = false;

            LogInfo.WriteLogEDC("+ Start DisposeData SumReport");
            try
            {
                LogInfo.WriteLogEDC("+ Start Dispose SendStream");
                if (sendStream != null)
                {
                    sendStream.Flush();
                    sendStream.Close();
                    sendStream.Dispose();
                    sendStream = null;
                }
                LogInfo.WriteLogEDC("+ End Dispose SendStream");

                LogInfo.WriteLogEDC("+ Start Dispose Stream");
                if (stream != null)
                {
                    stream.Flush();
                    stream.Close();
                    stream.Dispose();
                    stream = null;
                }
                LogInfo.WriteLogEDC("+ End Dispose Stream");

                LogInfo.WriteLogEDC("+ Start Dispose Client");
                if (client != null)
                {
                    client.Close();
                    client.Dispose();
                    client = null;
                }
                LogInfo.WriteLogEDC("+ End Dispose Client");

                LogInfo.WriteLogEDC("+ Start Dispose NetServer");
                if (netServer != null)
                {
                    netServer.Stop();
                    netServer = null;
                }
                LogInfo.WriteLogEDC("+ End Dispose NetServer");

                LogInfo.WriteLogEDC("+ Start Clear Timer");
                if (iTimerAutoClose > 0)
                    GLib.Source.Remove(iTimerAutoClose);
                iTimerAutoClose = 0;
                LogInfo.WriteLogEDC("+ End Clear Timer");

                LogInfo.WriteLogEDC("+ Start Dispose EventThread");
                if (eventThread != null)
                {
                    eventThread.Abort();
                    eventThread = null;
                }
                LogInfo.WriteLogEDC("+ End Dispose EventThread");

                LogInfo.WriteLogEDC("+ End DisposeData SumReport");
            }
            catch (Exception e)
            {
                LogInfo.WriteLog("- ECD Error Dispose SumReport : " + e.Message);
                LogInfo.WriteLaneEvent("- ECD Error Dispose SumReport : " + e.Message);
                LogInfo.WriteLogEDC("- ECD Error Dispose SumReport : " + e.Message);
                Console.WriteLine("- Error : " + e.Message);
            }
        }

        private bool InitSocketSendEDC()
        {
            try
            {
                client = new TcpClient(TCT_Lib.szEdcIPAddress, TCT_Lib.iEdcSendPort);
                stream = client.GetStream();
                //Console.WriteLine("-- Success Init Socket ---");
                LogInfo.WriteLogEDC("+ Success InitSocketSendEDC");

                return true;
            }
            catch (SocketException err)
            {
                //LogInfo.WriteLogEDC("- Error InitSocketSendEDC : " + err.Message);

                return false;
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
            try
            {
                if (netServer == null)
                {
                    LogInfo.WriteLogEDC("+ Start netServer Port#1 : " + TCT_Lib.iEdcRecvPort);
                    netServer = new TcpListener(IPAddress.Any, TCT_Lib.iEdcRecvPort);
                    netServer.Start();
                }
            }
            catch (Exception err)
            {
                LogInfo.WriteLogEDC("Warning TcpListener : " + err.Message);
                netServer.Stop();
                netServer = null;
            }

            while (true)
            {
                try
                {
                    if (netServer == null)
                    {
                        LogInfo.WriteLogEDC("+ Start netServer Port : " + TCT_Lib.iEdcRecvPort);
                        netServer = new TcpListener(IPAddress.Any, TCT_Lib.iEdcRecvPort);
                        netServer.Start();
                    }

                    TcpClient clientSocket      = netServer.AcceptTcpClient();
                    clientSocket.ReceiveTimeout = 3 * 1000;
                    clientSocket.SendTimeout    = 3 * 1000;
                    sendStream                  = clientSocket.GetStream();
                    sendStream.WriteTimeout     = 3 * 1000;
                    sendStream.ReadTimeout      = 3 * 1000;

                    if (clientSocket.Connected)
                    {
                        sendStream.Read(data, 0, data.Length);
                        AnalysisEdcMsg(data);

                        sendStream.Flush();
                        data = new byte[512];
                        Thread.Sleep(50);

                        if (iTimerAutoClose > 0)
                            GLib.Source.Remove(iTimerAutoClose);
                        iTimerAutoClose = 0;

                        clientSocket.Close();
                        DisposeData();
                    }
                    else
                        clientSocket.Close();
                }
                catch (SocketException errSocket)
                {
                    //LogInfo.WriteLogEDC("+ EDC SocketException : " + errSocket.Message);
                    DisposeData();
                    break;
                }
                catch (TimeoutException errTimeout)
                {
                    Console.WriteLine("Warning!...ThreadEventReceTimeOut error : " + errTimeout.Message);
                    Thread.Sleep(1000);
                    if (sendStream != null)
                    {
                        sendStream.Close();
                        sendStream.Dispose();
                        sendStream = null;
                    }
                    Thread.Sleep(1000);
                    if (netServer != null)
                    {
                        netServer.Stop();
                        netServer = null;
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine("Warning!...ThreadEventRecvServer error : " + err.Message);
                    Thread.Sleep(1000);
                    if (sendStream != null)
                    {
                        sendStream.Close();
                        sendStream.Dispose();
                        sendStream = null;
                    }
                    Thread.Sleep(1000);
                    if (netServer != null)
                    {
                        netServer.Stop();
                        netServer = null;
                    }
                }
                finally
                {
                    if (client == null)
                        client = new TcpClient(TCT_Lib.szEdcIPAddress, TCT_Lib.iEdcSendPort);
                    if (stream == null)
                        stream = client.GetStream();
                    // Stop listening for new clients.
                    //if (netServer != null)
                    //    netServer.Stop();
                }
            }

            //netServer.Stop();
        }

        private void AnalysisEdcMsg(byte[] byteMsg)
        {
            try
            {
                //--- Send Ack
                Encoding enc = new ASCIIEncoding();
                sendStream.Write(enc.GetBytes(ClassLibEDC.GenHexChar(0x06).ToString()), 0, 1);
                //---------------------------------------------------------------------------

                string szResponse = Encoding.ASCII.GetString(byteMsg);
                //Console.WriteLine("+ Result Response : " + szResponse);
                //LogInfo.WriteLogEDC("+ EDC Result Response (SumReport) : " + szResponse);

                string szCharList = ClassLibEDC.GenHexCharList(Encoding.ASCII.GetString(byteMsg));
                Console.WriteLine("Byte Data : " + szCharList);
                LogInfo.WriteLogEDC("+ EDC Response Byte Data (SumReport) : " + szCharList);

                //txtResponseData.Text = szCharList;
                string szRespCode = szResponse.Substring(17, 2);
                Console.WriteLine("+ Response Code : " + szRespCode);
                Console.WriteLine("-");
                LogInfo.WriteLogEDC("+ EDC Response Code (SumReport) : " + szRespCode);
                if (szRespCode != "00")
                {
                    string szErrCodeMsg     = ClassLibEDC.GetResponseMsg(szRespCode);
                    string szErrorShowMsg   = ClassLibEDC.ShowResponseMsg(szRespCode);
                    //Console.WriteLine($"Error Code : {szRespCode}, Error Text : szErrCodeMsg");
                    LogInfo.WriteLogEDC($"Error Code : {szRespCode}, Error Text : " + szErrCodeMsg);
                }
                else
                {
                    //--- Check TransCode
                    string szTransCode = szResponse.Substring(15, 2);
                    if (szTransCode != "50")
                    {
                        LogInfo.WriteLogEDC($"Error TransCode : {szTransCode}, This trans code is not sum report (settlement).");
                    }
                }

                AutoCloseSumReport();
                    //DisposeData();

            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                LogInfo.WriteLogEDC(err.Message);
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

                string szErrMsg2 = "", szReqMsg = "";
                string szEdcRef1 = "", szEdcRef2 = "", szEdcRef3 = "";
                byte[] byteMsg  = new byte[300];
                bool bResult    = ClassLibEDC.GenerateEojSumReportEdcMessage(TCT_Lib.dtStaffStartShift, ref byteMsg, ref szReqMsg, ref szEdcRef1, ref szEdcRef2, ref szEdcRef3, ref szErrMsg2);
                LogInfo.WriteLogEDC($"+ GenerateEojSumReportEdcMessage : bResult={bResult}, ReqMsg={szReqMsg}, EdcRef1={szEdcRef1}, EdcRef2={szEdcRef2}, EdcRef3={szEdcRef3}, szErrMsg={szErrMsg2}");

                //stream.ReadTimeout  = 5 * 1000;
                stream.WriteTimeout = 5 * 1000;
                stream.Write(byteMsg, 0, byteMsg.Length);

                //--- Wait Ack
                int iAck        = 0;
                byte[] bAck     = new byte[100];
                int iDataLen    = stream.Read(bAck, 0, 1);
                iAck            = bAck[0];
                LogInfo.WriteLogEDC("+ Ack Data : " + iAck);
                if (iAck != 0x06) // Ack
                {
                    if (iAck == 0x15)
                    {
                        Console.WriteLine("- Error...CRC Invalid. ---");
                        LogInfo.WriteLogEDC("- Error Generate StartSendSumReport - CRC Invalid");
                    }
                    else if (iAck == 0x04)
                    {
                        LogInfo.WriteLogEDC("+ Ack Generate StartSendSumReport - EDC busy");
                        stream.Flush();
                        AutoCloseSumReport();
                        return;
                    }
                    else
                    {
                        //txtMsg.Text = @"< Error!...Ack from EDC >";
                        Console.WriteLine("- Error...Ack from EDC. ---");
                        LogInfo.WriteLogEDC("- Error StartSendSumReport  - Err Ack from EDC");
                    }
                    //-----------------------------------------
                    string szCharList = GenHexCharList(bAck);
                    LogInfo.WriteLogEDC("+ EDC Ack StartSendSumReport : " + szCharList);
                    //-----------------------------------------
                    stream.Flush();
                    AutoCloseSumReport();
                    return;
                }
                else
                {
                    LogInfo.WriteLogEDC("+ Update ClearBatch to Database");
                    TCT_Lib.LaneDB.UpdateLastClearBatch();
                }
                stream.Flush();
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
