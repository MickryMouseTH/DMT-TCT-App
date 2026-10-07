//#define __TEST_EDC__

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gtk;
using Pango;
using TCT_App.EMV.V0;

namespace TCT_App.EMV.EMV_V1
{
    public partial class EdcPaymentDlg_V1 : Gtk.Dialog
    {
        decimal fPayAmount;
        bool bTowing;
        volatile bool bCancel        = false;   // v2.0.0: volatile - shared between UI cancel and recv thread
        bool bError         = false;
        bool bHasResult     = false;

        Thread eventThread;
        System.Net.Sockets.Socket recvSocket;
        bool bStartCmd              = false;
        string szWarningMsg         = "กรุณาติดต่อธนาคารผู้ออกบัตร";
        string szWarningTFI         = "ติดต่อธนาคาร";

        int iCurRetry = 0;
        string szRef1 = "", szRef2 = "", szRef3 = "", szRef4 = "";
        public objCreditCardInfo xOutCreditCard;
        //----------------------------------------------------------------
        public EdcPaymentDlg_V1(decimal _fPayAmount, bool _bTowing = false)
        {
            TCT_Lib.bActiveEMVDlg = true;
            this.Build();
            this.Decorated = false;     // v2.0.2: no title bar on popups
            this.KeyPressEvent  += OnKeyPressEvent;     // v2.0.2: keyboard support
            this.Activate();
            bCancel             = false;
            bHasResult          = false;

            TCT_Lib.eCurAppPage = AppPage.EMV;
            fPayAmount          = _fPayAmount;
            bTowing             = _bTowing;
            this.Title          = "Payment with EDC V.1";

            this.HasSeparator   = false;
            this.BorderWidth    = 6;
            this.Resizable      = false;

            //--- For Test EDC Message
            //Process_StartPaymentEDC();
            //-----------------------

            //TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, "", fPayAmount.ToString("0"), "", szWarningTFI, "");       // Test

            LogInfo.WriteLogEDC("");
            LogInfo.WriteLogEDC("+------------------ Start CreditCard Payment V.1 --------------------");
            TCT_Lib.laneController.manageProcessEMV += LaneController_ManageProcessEMV;

            //this.DeleteEvent += new DeleteEventHandler(OnDlgDelete);

            Gdk.Pixbuf imgEMV   = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Emv.png");
            imgEmv.Pixbuf       = imgEMV;

            txtPayAmount.ModifyFont(FontDescription.FromString("Ubuntu Bold 70"));
            txtPayAmount.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
            txtPayAmount.Text   = fPayAmount.ToString("0") + " บาท";

            txtMsg.ModifyFont(FontDescription.FromString("Ubuntu 26"));
            txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(0, 255, 255));
            txtMsg.Text         = "โปรดรอลูกค้าแตะบัตร";

/*
#if !__TEST_EDC__
            //if (!TCT_Lib.TestConnection(TCT_Lib.szEdcIPAddress, TCT_Lib.iEdcSendPort))
            LogInfo.WriteLogEDC("+ Test EDC Connection");
            bool bResult = IsPortReachable(TCT_Lib.szEdcIPAddress, TCT_Lib.iEdcSendPort, 300).GetAwaiter().GetResult();
            if (!bResult)
            {
                Console.WriteLine("- Fail Connect Send EDC Socket");
                LogInfo.WriteLogEDC("- Fail Test EDC Connection");
                txtMsg.ModifyFont(FontDescription.FromString("Ubuntu 26"));
                txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                txtMsg.Text     = "ไม่สามารถเชื่อมต่ออุปกรณ์";
                return;
            }
            LogInfo.WriteLogEDC("+ Success Test EDC Connection");
#endif                    
*/
            if (!bTowing)
            {
                TCT_Lib.xCurEMV     = new EMV_Info();
                TCT_Lib.xCurEDC     = new EDC_Info();
            }
            else
            {
                TCT_Lib.xTowingEMV  = new EMV_Info();
                TCT_Lib.xTowingEDC  = new EDC_Info();
            }

#if !__TEST_EDC__
            InitSocketRecvEDC();
            //Thread.Sleep(1000);
            Process_StartPaymentEDC();
#endif 
        }

        public async Task<bool> IsPortReachable(string host, int port = 80, int msTimeout = 300)
        {
            if (string.IsNullOrEmpty(host))
                return false;
            //throw new ArgumentNullException("host");

            return await Task.Run(() =>
            {
                var clientDone  = new ManualResetEvent(false);
                var reachable   = false;
                var hostEntry   = new DnsEndPoint(host, port);
                using (var socket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    var socketEventArg = new SocketAsyncEventArgs { RemoteEndPoint = hostEntry };
                    socketEventArg.Completed += (s, e) =>
                    {
                        reachable = e.SocketError == SocketError.Success;
                        clientDone.Set();
                    };

                    clientDone.Reset();
                    socket.ConnectAsync(socketEventArg);
                    clientDone.WaitOne(msTimeout);

                    return reachable;
                }
            });
        }

        void OnDlgDelete(object sender, DeleteEventArgs a)
        {
            Application.Invoke(delegate
            {
                TCT_Lib.bActiveEMVDlg = false;
                //eventThread.Abort();
                LogInfo.WriteLogEDC("+ Recv Socket Close / Disconnect socket when close dialog");
                if (recvSocket != null)
                {
                    recvSocket.Close();
                    recvSocket.Dispose();
                    recvSocket = null;
                }
            });
        }

        protected void OnDeleteEvent(object o, DeleteEventArgs args)
        {
            Application.Invoke(delegate
            {
                TCT_Lib.bActiveEMVDlg = false;
                //eventThread.Abort();
                LogInfo.WriteLogEDC("+ Recv Socket Close / Disconnect socket when close dialog");
                if (recvSocket != null)
                {
                    recvSocket.Close();
                    recvSocket.Dispose();
                    recvSocket = null;
                }

            });
        }

        private void Process_StartPaymentEDC()
        {
            System.Net.Sockets.Socket sendSocket = null;
            try
            {
                LogInfo.WriteLaneEvent($"EMV- Generate Payment");
                LogInfo.WriteLogEDC("+-- Start EMV Payment ---");
                decimal fAmount = fPayAmount;

                //-----------------------------------------------
                szRef1 = szRef2 = szRef3 = szRef4 = "";
                string szPlazaId        = TCT_Lib.szPlazaID;
                if (TCT_Lib.szPlazaID.Length == 2)
                    szPlazaId = "T" + TCT_Lib.szPlazaID;
                else if (TCT_Lib.szPlazaID.Length == 3)
                    szPlazaId = "T" + TCT_Lib.szPlazaID.Substring(1, 2);
                string szLaneName       = TCT_Lib.szLaneName;
                if (szLaneName.Length < 4)
                    szLaneName = szLaneName.PadRight(4, ' ');

                int iEdc_nTrx = TCT_Lib.nTrx;
                if (TCT_Lib.xListLoopCar != null)
                {
                    iEdc_nTrx += TCT_Lib.xListLoopCar.Count;
                    if (TCT_Lib.xListLoopCar.Count > 0)
                        LogInfo.WriteLogEDC($"+ Found vehicle in Loop = {TCT_Lib.xListLoopCar.Count}");
                }
                TCT_Lib.iCurEmvDetailID = iEdc_nTrx;

                string szJobNo  = TCT_Lib.iJobNo.ToString().PadLeft(4, '0');
                string sznTrx   = iEdc_nTrx.ToString().PadLeft(5, '0');
                szRef1          = (szPlazaId + szLaneName + szJobNo + sznTrx).PadRight(20, '0');

                //----
                string szTID    = TCT_Lib.szEdcTID;
                szRef2          = szTID + DateTime.Now.ToString("yyMMddHHmmss");

                //----
                szRef3          = TCT_Lib.dtStaffStartShift.ToString("yyMMddHHmmss");       // Date-Time BOJ

                szRef4          = DateTime.Today.ToString("MMdd") + TCT_Lib.iJobNo.ToString().PadLeft(3, '0') + iEdc_nTrx.ToString().PadLeft(3, '0');

                LogInfo.WriteLogEDC($"+ Generate EDC Payment (#1) : Ref1={szRef1}, Ref2={szRef2}, Ref3={szRef3}, Ref4={szRef4}");
                //-----------------------------------------------------------------------------------------------
                byte[] byteMsg  = new byte[300];
                string szErrMsg = "", szPayMsg = "";
                bool bResult    = ClassLibEDC_V1.GeneratePaymentEdcMessage_V1(fAmount, szRef1, szRef2, szRef3, szRef4, ref byteMsg, ref szPayMsg, ref szErrMsg);
                LogInfo.WriteLogEDC($"+ Generate EDC Payment (#2) : Amount={fAmount}, Ref1={szRef1}, Ref2={szRef2}, Ref3={szRef3}, Ref4={szRef4}");
                string szStrMsg = ClassLibEDC_V1.GenHexCharList(byteMsg);
                LogInfo.WriteLogEDC("+ Payment Hex Message : " + szStrMsg);

                if (!bTowing)
                    TCT_Lib.xCurEMV.requestText     = "";
                else
                    TCT_Lib.xTowingEMV.requestText  = "";

                Thread.Sleep(100);
                //-----------------------------------------------------------------------
                IPAddress ipAddr            = IPAddress.Parse(TCT_Lib.szEdcIPAddress);
                IPEndPoint sendEndPoint     = new IPEndPoint(ipAddr, TCT_Lib.iEdcSendPort);
                sendSocket                  = new System.Net.Sockets.Socket(sendEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                sendSocket.SendTimeout      = 2 * 1000;
                sendSocket.ReceiveTimeout   = 2 * 1000;
                try
                {
                    //string szStr    = EdcLib.GenHexCharList(byteMsg);
                    //Console.WriteLine(szStr);

                    sendSocket.Connect(sendEndPoint);
                    //Thread.Sleep(1000);
                    if (sendSocket.Connected)
                    {
                        sendSocket.Send(byteMsg);

                        //Thread.Sleep(TCT_Lib.iEdcRelayMilliSecBeforeAck);   // Add this line when 2025-08-04 (Fix cannot recv ack from send request)
                        //Thread.Sleep(100);
                        //--- Wait Ack
                        byte[] bAck     = new byte[1];
                        int iTotalByte  = sendSocket.Receive(bAck);

                        string szResultStr = ClassLibEDC_V1.GenHexCharList(bAck);
                        int iAck = bAck[0];
                        LogInfo.WriteLogEDC("+ Ack Data from EDC (Payment) : " + iAck);
                        if (iAck != 0x06) // Ack
                        {
                            Application.Invoke(delegate {
                                txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                                if (iAck == 0x04)
                                {
                                    //LogInfo.WriteLogEDC("+ Ack Generate CreditCard Payment - EDC busy");
                                    txtMsg.Text = "Ack=4, อุปกรณ์ไม่พร้อมทำงาน";
                                }
                                else if (iAck == 0x14)
                                {
                                    //LogInfo.WriteLogEDC("+ Ack Generate CreditCard Payment - Off line transaction is full");
                                    //txtMsg.Text = "Ack=14, ข้อมูล Off line เต็ม ไม่สามารถเพิ่มรายการได้";
                                    txtMsg.Text = "Ack=14, ข้อมูลเต็ม ทำรายการไม่ได้";
                                }
                                else
                                {
                                    //txtMsg.Text = @"< Error!...Ack from EDC >";
                                    Console.WriteLine("- Error...Ack from EDC. ---");
                                    //LogInfo.WriteLogEDC("- Error CreditCard Payment - Err Ack from EDC");
                                    string szCharList2 = GenHexCharList(bAck);
                                    txtMsg.Text = "Unknow Ack : " + szCharList2;
                                }
                            });
                            if (iAck == 0x04)
                                LogInfo.WriteLogEDC("+ Ack Generate CreditCard Payment - EDC busy");
                            else if (iAck == 0x14)
                                LogInfo.WriteLogEDC("+ Ack Generate CreditCard Payment - Off line transaction is full");
                            else
                                LogInfo.WriteLogEDC("- Error CreditCard Payment - Err Ack from EDC");

                            //-----------------------------------------
                            string szCharList = GenHexCharList(bAck);
                            LogInfo.WriteLogEDC("+ EDC Ack Payment : " + szCharList);
                            //-----------------------------------------
                        }
                        bStartCmd = true;

                        //sendSocket.Shutdown(SocketShutdown.Both);
                        sendSocket.Close();
                        sendSocket.Dispose();
                        sendSocket = null;
                        LogInfo.WriteLogEDC("+ Success Close SendSocket");
                    }

                    return;
                }
/*
                catch (TimeoutException)
                {

                }
*/                               
                catch (SocketException err)
                {
                    //sendSocket.Shutdown(SocketShutdown.Both);
                    if (sendSocket != null)
                    {
                        sendSocket.Close();
                        sendSocket.Dispose();
                        sendSocket = null;
                    }
                    Console.WriteLine("- Error Send Credit Payment : " + err.Message);
                    Application.Invoke(delegate {
                        if (err.Message == "No route to host")      // Not found EDC Hosting
                        {
                            Console.WriteLine("- Fail Connect Send EDC Socket");
                            //LogInfo.WriteLogEDC("- Fail Test EDC Connection");
                            txtMsg.ModifyFont(FontDescription.FromString("Ubuntu 26"));
                            txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                            txtMsg.Text = "ไม่สามารถเชื่อมต่ออุปกรณ์";
                            //return;
                        }
                        if (err.Message == "Operation on non-blocking socket would block")
                        {
                            Console.WriteLine("- Error Send Credit Payment : " + err.Message);
                            //LogInfo.WriteLogEDC("- Error Send Credit Payment : " + err.Message);
                            txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                            szErrMsg = "อุปกรณ์ไม่พร้อมทำงาน";
                            txtMsg.Text = szErrMsg;
                        }
                        if (err.Message == "Address already in use")
                        {
                            //LogInfo.WriteLogEDC("- Found Address already in use");
                            txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                            txtMsg.Text = szErrMsg;
                        }
                    });

                    if (err.Message == "No route to host")      // Not found EDC Hosting
                    {
                        LogInfo.WriteLogEDC("- Fail Test EDC Connection");
                        return;
                    }
                    if (err.Message == "Operation on non-blocking socket would block")
                    {
                        LogInfo.WriteLogEDC("- Error Send Credit Payment : " + err.Message);
                    }
                    if (err.Message == "Address already in use")
                    {
                        LogInfo.WriteLogEDC("- Found Address already in use");
                    }
                }
            }
            catch (Exception err)
            {
                //sendSocket.Shutdown(SocketShutdown.Both);
                if (sendSocket != null)
                {
                    sendSocket.Close();
                    sendSocket.Dispose();
                    sendSocket = null;
                }

                Console.WriteLine("- Error Exception Send Credit Payment : " + err.Message);
                LogInfo.WriteLogEDC("- Error Exception Send Credit Payment : " + err.Message);

/*
                Application.Invoke(delegate {
                    txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                    string szErrMsg = err.Message;
                    if (err.Message == "The socket is not connected")
                        szErrMsg = "อุปกรณ์ไม่พร้อมทำงาน";
                    txtMsg.Text = szErrMsg;
                });
*/
                //------------------------------------------------------------------------------
                // 2025-08-04  : Try to fix  TCT cannot found Ack from EDC
                if (err.Message == "Operation on non-blocking socket would block")
                {
                    RespondOnGtk(ResponseType.None);   // v2.0.2: was Dispose()/Destroy() off the GTK thread
                }
                else
                {
                    Application.Invoke(delegate {
                        txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                        string szErrMsg = err.Message;
                        if (err.Message == "The socket is not connected")
                            szErrMsg = "อุปกรณ์ไม่พร้อมทำงาน";
                        txtMsg.Text = szErrMsg;
                    });
                }
                //------------------------------------------------------------------------------------------------------------
            }
        }

        private void InitSocketRecvEDC()
        {
            eventThread                 = new Thread(new ThreadStart(ThreadEventRecvServer));
            eventThread.Priority        = ThreadPriority.Normal;
            eventThread.IsBackground    = true;
            eventThread.Start();
        }

        int iCountFn = 0;
        private void ThreadEventRecvServer()
        {
            //Application.Invoke(delegate
            {
                //IPAddress ipAddr        = IPAddress.Parse(TCT_Lib.szEdcIPAddress);
                IPAddress ipAddr        = IPAddress.Any;
                IPEndPoint recvEndPoint = new IPEndPoint(ipAddr, TCT_Lib.iEdcRecvPort);

                System.Net.Sockets.Socket clientSocket = null;
                try
                {

                    recvSocket                  = new System.Net.Sockets.Socket(recvEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    recvSocket.SendTimeout      = 3 * 1000;
                    recvSocket.ReceiveTimeout   = 3 * 1000;
                    recvSocket.Bind(recvEndPoint);
                    // Using Listen() method we create the Client list that will want to connect to Server
                    recvSocket.Listen(10);

                    bool bRunning = true;
                    while (bRunning)
                    {
                        try
                        {
                            // Data buffer
                            byte[] bytes    = new Byte[2048];
                            //string data     = null;
                            clientSocket    = recvSocket.Accept();
                            int numByte     = clientSocket.Receive(bytes);
                            byte[] byteData = new byte[numByte];
                            Array.Copy(bytes, 0, byteData, 0, numByte);
                            string szStrMsg = ClassLibEDC_V1.GenHexCharList(byteData);
                            Console.WriteLine("Response = " + szStrMsg);
                            LogInfo.WriteLogEDC("+ Recv Hex Message : " + szStrMsg);
                            if (szStrMsg.Trim() != "")
                            {
                                //Console.WriteLine("Text received -> {0} ", data);
                                byte[] bAck = new byte[1];
                                if (bCancel)
                                    bAck[0] = 0x06;
                                else
                                    bAck[0] = 0x16;

                                // Send a message to Client
                                // using Send() method

                                Thread.Sleep(TCT_Lib.iEdcRelayMilliSecBeforeAck);   // Add this line when 2025-02-18
                                LogInfo.WriteLogEDC($"+ Wait {TCT_Lib.iEdcRelayMilliSecBeforeAck} ms. before send Ack to EDC");
                                LogInfo.WriteLogEDC("+ Send Ack to EDC ("+ (bCancel ? "Cancel" : "Payment") +") = " + bAck[0]);
                                clientSocket.Send(bAck);

                                if (bCancel)
                                {
                                    TCT_Lib.bActiveEMVDlg = false;
                                    TCT_Lib.laneController.manageProcessEMV -= LaneController_ManageProcessEMV;
                                    //eventThread.Abort();
                                    RespondOnGtk(ResponseType.Cancel);   // v2.0.2: was Respond() on socket thread

                                    if (clientSocket != null)
                                    {
                                        //clientSocket.Shutdown(SocketShutdown.Both);
                                        clientSocket.Close();
                                        clientSocket = null;
                                    }

                                    Thread.Sleep(500);
                                    LogInfo.WriteLogEDC("+ Recv Socket Close / Disconnect Socket");
                                    if (recvSocket != null)
                                    {
                                        recvSocket.Close();
                                        recvSocket.Dispose();
                                        recvSocket = null;
                                    }

                                    //--- v2.0.2: no Dispose()/Destroy() here (socket thread) - MainWindow disposes on GTK thread
                                    bRunning = false;
                                    return;
                                }
                                else
                                {
                                    if (byteData.Length > 0)
                                    {
                                        AnalysisEdcMsg(byteData);
                                        bRunning = false;
                                    }
                                   
                                    /*
                                                                    TCT_Lib.bActiveEMVDlg = false;
                                                                    TCT_Lib.laneController.manageProcessEMV -= LaneController_ManageProcessEMV;
                                                                    this.Respond(ResponseType.Ok);

                                                                    if (clientSocket != null)
                                                                    {
                                                                        //clientSocket.Shutdown(SocketShutdown.Both);
                                                                        clientSocket.Close();
                                                                        clientSocket = null;
                                                                    }

                                                                    recvSocket.Close();
                                                                    recvSocket.Dispose();
                                                                    recvSocket = null;

                                                                    Dispose();
                                                                    Destroy();

                                                                    bRunning = false;
                                                                    return;
                                    */
                                }
                            }
                        }
                        catch (SocketException err)
                        {
                            bRunning = false;
                            Console.WriteLine(err.Message);
                            if (clientSocket != null)
                            {
                                //clientSocket.Shutdown(SocketShutdown.Both);
                                clientSocket.Close();
                            }
                            //if (recvSocket != null)
                            //    recvSocket.Close();
                            if (err.Message == "Address already in use")
                            {
                                LogInfo.WriteLogEDC("- Error Recv server : " + err.Message);
                                Application.Invoke(delegate {
                                    txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                                    txtMsg.Text = err.Message;
                                });
                            }
                            if (err.Message == "Operation on non-blocking socket would block")
                            {
                                bRunning = true;
                            }
                        }
                        catch (Exception err)
                        {
                            bRunning = false;
                            Console.WriteLine(err.Message);
                            if (clientSocket != null)
                            {
                                //clientSocket.Shutdown(SocketShutdown.Both);
                                clientSocket.Close();
                            }
                        }
                        finally
                        {
/*
                            if (clientSocket != null)
                            {
                                clientSocket.Shutdown(SocketShutdown.Both);
                                clientSocket.Close();
                            }
*/                            
                        }
                    }
                }
                catch (SocketException err)
                {
                    if (err.Message == "Address already in use")
                    {
                        LogInfo.WriteLogEDC("- Error Recv server 1 : " + err.Message);

                        if (iCountFn == 0)
                        {
                            if (recvSocket != null)
                            {
                                recvSocket.Close();
                                recvSocket.Dispose();
                                recvSocket = null;
                            }
                            if (clientSocket != null)
                                clientSocket.Close();
                            LogInfo.WriteLogEDC("- Close recv socket and Auto Retry open recv port EDC again");
                            //-----------------------------------------------------------------------
                            iCountFn++;
                            ThreadEventRecvServer();
                            return;
                        }
                        else
                        {
                            Application.Invoke(delegate {
                                txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                                txtMsg.Text = err.Message;
                            });
                        }
                    }
                    Console.WriteLine(err.Message);
                    LogInfo.WriteLogEDC("+ Recv Socket Close / Disconnect Socket");
                    if (recvSocket != null)
                        recvSocket.Close();
                    //----------------------------------------

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    if (e.Message == "Address already in use")
                    {
                        LogInfo.WriteLogEDC("- Error Recv server 2 : " + e.Message);
                        Application.Invoke(delegate {
                            txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                            txtMsg.Text = e.Message;
                        });
                    }
                }
                finally
                {
                    if (clientSocket != null)
                    {
                        //clientSocket.Shutdown(SocketShutdown.Both);
                        //clientSocket.Disconnect(true);
                        clientSocket.Close();
                    }
                    if (recvSocket != null)
                    {
                        LogInfo.WriteLogEDC("+ Recv Socket Close / Disconnect Socket");
                        //recvSocket.Close();
                    }
                }
            }
            //);
        }

        private void AnalysisEdcMsg(byte[] byteMsg)
        {
            //Application.Invoke(delegate {
                try
                {
                    Encoding enc        = new ASCIIEncoding();
                    string szResponse   = Encoding.ASCII.GetString(byteMsg);
                //Console.WriteLine("+ Result Response : " + szResponse);
                //LogInfo.WriteLogEDC("+ EDC Result Response (Pay EMV) : " + szResponse);

                if ((TCT_Lib.xCurEMV == null) && (TCT_Lib.xTowingEMV == null))
                {
                    LogInfo.WriteLogEDC("- EDC Response after Cancel.");
                    return;
                }

                //TCT_Lib.xCurEMV.responseText = szResponse;
                if (!bTowing)
                        TCT_Lib.xCurEMV.responseText    = "";
                    else
                        TCT_Lib.xTowingEMV.responseText = "";
                    string szCharList   = ClassLibEDC_V1.GenHexCharList(Encoding.ASCII.GetString(byteMsg));
                    Console.WriteLine("Byte Data : " + szCharList);
                    LogInfo.WriteLogEDC("+ EDC Response Byte Data (Pay EMV) : " + szCharList);

                    if (szResponse.Trim() == "")
                    {
                        LogInfo.WriteLogEDC("- Error : Not found Response");
                        return;
                    }

                    bHasResult          = true;
                    //txtResponseData.Text = szCharList;
                    // v2.0.0: guard against short/malformed EDC response (was IndexOutOfRange crash)
                    if (szResponse.Length < 19)
                    {
                        LogInfo.WriteLogEDC($"- Error : EDC response too short (len={szResponse.Length}) : {szResponse}");
                        return;
                    }
                    string szRespCode   = szResponse.Substring(17, 2);
                    Console.WriteLine("+ Response Code : " + szRespCode);
                    Console.WriteLine("-");
                    LogInfo.WriteLogEDC("+ EDC Response Code (Pay EMV) : " + szRespCode);
                    if (szRespCode != "00")
                    {
                        string szErrCodeMsg     = ClassLibEDC_V1.GetResponseMsg(szRespCode);
                        string szErrorShowMsg   = ClassLibEDC_V1.ShowResponseMsg(szRespCode);
                        //Console.WriteLine($"Error Code : {szRespCode}, Error Text : szErrCodeMsg");
                        LogInfo.WriteLogEDC($"Error Code : {szRespCode}, Error Text : " + szErrCodeMsg);
                        //txtMsg.Text = $"{szErrorShowMsg}";
                        Application.Invoke(delegate {
                            txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                            txtMsg.Text = szWarningMsg;

                            txtErrMsg.ModifyFont(FontDescription.FromString("Ubuntu 22"));
                            txtErrMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 0));
                            //txtErrMsg.Text = $"Error Code : {szRespCode}  =>  " + szErrCodeMsg;
                            txtErrMsg.Text = szErrCodeMsg;
                        });
                        //---------------------------------------------------------------
                        //--- Show warning at TFI
                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, "", fPayAmount.ToString("0"), "", szWarningTFI, "");
                        //--------------------------------------------------------------

                        if (szRespCode == "ND")
                        {
                            //Process_CancelEMV();
                        }
                        bError = true;
                        //----------------------------------------------
                        Thread.Sleep(200);
                        //LogInfo.WriteLogEDC("+ Recv Socket Close / Disconnect when success payment");
                        //recvSocket.Close();
                        //recvSocket.Dispose();
                        //recvSocket = null;
                        //------------------------------------------------
                        return;
                    }
                    else
                    {
                        //--- Check TransCode
                        if (szResponse.Trim() == "")
                        {
                            LogInfo.WriteLogEDC($"Error : Not found Response.");
                            //----------------------------------------------
                            //Thread.Sleep(200);
                            //recvSocket.Close();
                            //recvSocket.Dispose();
                            //recvSocket = null;
                            //------------------------------------------------
                            return;
                        }
                        // v2.0.0: guard against short response before Substring (was IndexOutOfRange crash)
                        if (szResponse.Length < 17)
                        {
                            LogInfo.WriteLogEDC($"- Error : EDC response too short for TransCode (len={szResponse.Length}) : {szResponse}");
                            return;
                        }
                        string szTransCode = szResponse.Substring(15, 2);
                        if (szTransCode != "20")
                        {
                            LogInfo.WriteLogEDC($"Error TransCode : {szTransCode}, This trans code is not emv result.");
                            return;
                        }

                        xOutCreditCard              = ClassLibEDC_V1.EncodeFormatEDC(szResponse);
                        xOutCreditCard.szRef1       = xOutCreditCard.szRef1 ?? "";
                        xOutCreditCard.szRef2       = xOutCreditCard.szRef2 ?? "";
                        xOutCreditCard.szCardNo     = xOutCreditCard.szCardNo ?? "";
                        xOutCreditCard.szInvoiceNo  = xOutCreditCard.szInvoiceNo ?? "";
                        if ((xOutCreditCard.szRef1 != szRef1) || (xOutCreditCard.szRef2 != szRef2))
                        {
                            LogInfo.WriteLogEDC($"- Warning...Reference is not match, CurRef={szRef1}, Result-Ref1={xOutCreditCard.szRef1}, CurRef2={szRef2}, Result-Ref2={xOutCreditCard.szRef2}");

                            Application.Invoke(delegate {
                                txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                                txtMsg.Text = "รายการจ่ายไม่ถูกต้อง (Ref 2 not match)";
                            });

                            //----------------

                            int iAlarmID = (int)TCT_Lib.LaneDB.WriteLaneAlarmRTO(AlarmDevice.EDC_WARNING, AlarmType.Occurred, $"EMV payment ref 2 is not match.", $"CurRef2={szRef2}, Result-Ref2={xOutCreditCard.szRef2}");
                            TCT_Lib.SendLaneAlarmObjToRTO(iAlarmID);

                            //----------------------------------------------

                            return;
                        }

                        if (xOutCreditCard.szCardNo == "")
                        {
                            LogInfo.WriteLogEDC($"- Warning...Card No is not found.");
                            //----------------

                            int iAlarmID = (int)TCT_Lib.LaneDB.WriteLaneAlarmRTO(AlarmDevice.EDC_WARNING, AlarmType.Occurred, $"EMV payment not found Card No", $"");
                            TCT_Lib.SendLaneAlarmObjToRTO(iAlarmID);

                            return;
                        }

                        if (xOutCreditCard.szInvoiceNo != "")
                        {
                            LogInfo.WriteLogEDC($"- Warning...Invoice is {xOutCreditCard.szInvoiceNo}, then this is QR Payment (Not EMV Payment)");
                            //----------------

                            int iAlarmID = (int)TCT_Lib.LaneDB.WriteLaneAlarmRTO(AlarmDevice.EDC_WARNING, AlarmType.Occurred, $"EMV payment, InvoiceNo ( {xOutCreditCard.szInvoiceNo}), Payment is not match (QR Code)", $"");
                            TCT_Lib.SendLaneAlarmObjToRTO(iAlarmID);
                       
                            return;
                        }
                    }

                    Application.Invoke(delegate {
                        txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(0, 255, 0));
                        txtMsg.Text = "จ่ายสำเร็จ";
                    });

                    //xOutCreditCard     = ClassLibEDC.EncodeFormatEDC(szResponse);
                    //txtCardNo.Text      = xCreditCard.szCardNo;
                    //txtAmount.Text      = xCreditCard.szAmount;
                    //txtTerminalID.Text  = xCreditCard.szTerminalID;
                    //txtDateTime.Text    = xCreditCard.szTransactionDate + " - " + xCreditCard.szTransactionTime;
                    //txtRef3.Text        = xCreditCard.szRef3;
                    if (!bTowing)
                    {
                        TCT_Lib.xCurEMV.responseMessage = xOutCreditCard.szResponseText ?? "";
                        TCT_Lib.xCurEMV.respApproveCode = xOutCreditCard.szApprovalCode ?? "";
                        TCT_Lib.xCurEMV.respCardNo      = xOutCreditCard.szCardNo ?? "";
                        TCT_Lib.xCurEMV.respDate        = xOutCreditCard.szTransactionDate ?? "";
                        TCT_Lib.xCurEMV.respTime        = xOutCreditCard.szTransactionTime ?? "";
                        TCT_Lib.xCurEMV.respTerminalId  = xOutCreditCard.szTerminalID ?? "";
                        TCT_Lib.xCurEMV.respAmount      = xOutCreditCard.szAmount ?? "";
                        TCT_Lib.xCurEMV.respRef         = xOutCreditCard.szRef3 ?? "";
                        TCT_Lib.xCurEMV.respRef1        = xOutCreditCard.szRef1 ?? "";
                        TCT_Lib.xCurEMV.respRef2        = xOutCreditCard.szRef2 ?? "";
                        TCT_Lib.xCurEMV.respRef3        = xOutCreditCard.szRef3 ?? "";

                        TCT_Lib.xCurEDC.responseMessage = xOutCreditCard.szResponseText ?? "";
                        TCT_Lib.xCurEDC.approveCode     = xOutCreditCard.szApprovalCode ?? "";
                        TCT_Lib.xCurEDC.cardNo          = xOutCreditCard.szCardNo ?? "";
                        TCT_Lib.xCurEDC.terminalId      = xOutCreditCard.szTerminalID ?? "";
                        TCT_Lib.xCurEDC.transDate       = xOutCreditCard.szTransactionDate ?? "";
                        TCT_Lib.xCurEDC.transTime       = xOutCreditCard.szTransactionTime ?? "";
                        TCT_Lib.xCurEDC.transAmount     = xOutCreditCard.szAmount ?? "";
                        TCT_Lib.xCurEDC.transRef        = xOutCreditCard.szRef3 ?? "";
                        TCT_Lib.xCurEDC.ref1            = xOutCreditCard.szRef1 ?? "";
                        TCT_Lib.xCurEDC.ref2            = xOutCreditCard.szRef2 ?? "";
                        TCT_Lib.xCurEDC.ref3            = xOutCreditCard.szRef3 ?? "";

                        //--------------------------------------
                        LogInfo.WriteLogEDC("+ ResponseMessage \t= " + TCT_Lib.xCurEMV.responseMessage);
                        LogInfo.WriteLogEDC("+ ApproveCode \t= " + TCT_Lib.xCurEMV.respApproveCode);
                        LogInfo.WriteLogEDC("+ CardNo \t\t= " + TCT_Lib.xCurEMV.respCardNo);
                        LogInfo.WriteLogEDC("+ TerminalId \t\t= " + TCT_Lib.xCurEMV.respTerminalId);
                        LogInfo.WriteLogEDC("+ TransDate \t\t= " + TCT_Lib.xCurEMV.respDate);
                        LogInfo.WriteLogEDC("+ TransTime \t\t= " + TCT_Lib.xCurEMV.respTime);
                        LogInfo.WriteLogEDC("+ TransAmount \t= " + TCT_Lib.xCurEMV.respAmount);
                        LogInfo.WriteLogEDC("+ TransRef \t\t= " + TCT_Lib.xCurEMV.respRef);
                        LogInfo.WriteLogEDC("+ Ref1 \t\t= " + TCT_Lib.xCurEMV.respRef1);
                        LogInfo.WriteLogEDC("+ Ref2 \t\t= " + TCT_Lib.xCurEMV.respRef2);
                        LogInfo.WriteLogEDC("+ Ref3 \t\t= " + TCT_Lib.xCurEMV.respRef3);
                        LogInfo.WriteLogEDC($"EMV : JobNo={TCT_Lib.iJobNo}, nTrx={TCT_Lib.nTrx}, CardNo={TCT_Lib.xCurEMV.respCardNo}, ApproveCode={TCT_Lib.xCurEMV.respApproveCode.Trim()}, Ref2={TCT_Lib.xCurEMV.respRef2}, Ref3={TCT_Lib.xCurEMV.respRef3}");
                        LogInfo.WriteLaneEvent($"EMV Success : CardNo={TCT_Lib.xCurEMV.respCardNo}, Amount={TCT_Lib.xCurEMV.respAmount}, ApproveCode={TCT_Lib.xCurEMV.respApproveCode}, TransRef={TCT_Lib.xCurEMV.respRef}");

                        //-----------------------------------
/*
                        if (TCT_Lib.xTmpEMV == null)
                        {
                            TCT_Lib.xTmpEMV     = TCT_Lib.xCurEMV;
                            TCT_Lib.xTmpEDC     = TCT_Lib.xCurEDC;

                            TCT_Lib.xCurEMV     = null;
                            TCT_Lib.xCurEDC     = null;
                        }
*/                        
                    }
                    else
                    {
                        TCT_Lib.xTowingEMV.responseMessage  = xOutCreditCard.szResponseText ?? "";
                        TCT_Lib.xTowingEMV.respApproveCode  = xOutCreditCard.szApprovalCode ?? "";
                        TCT_Lib.xTowingEMV.respCardNo       = xOutCreditCard.szCardNo ?? "";
                        TCT_Lib.xTowingEMV.respDate         = xOutCreditCard.szTransactionDate ?? "";
                        TCT_Lib.xTowingEMV.respTime         = xOutCreditCard.szTransactionTime ?? "";
                        TCT_Lib.xTowingEMV.respTerminalId   = xOutCreditCard.szTerminalID ?? "";
                        TCT_Lib.xTowingEMV.respAmount       = xOutCreditCard.szAmount ?? "";
                        TCT_Lib.xTowingEMV.respRef          = xOutCreditCard.szRef3 ?? "";
                        TCT_Lib.xTowingEMV.respRef1         = xOutCreditCard.szRef1 ?? "";
                        TCT_Lib.xTowingEMV.respRef2         = xOutCreditCard.szRef2 ?? "";
                        TCT_Lib.xTowingEMV.respRef3         = xOutCreditCard.szRef3 ?? "";

                        TCT_Lib.xTowingEDC.responseMessage  = xOutCreditCard.szResponseText ?? "";
                        TCT_Lib.xTowingEDC.approveCode      = xOutCreditCard.szApprovalCode ?? "";
                        TCT_Lib.xTowingEDC.cardNo           = xOutCreditCard.szCardNo ?? "";
                        TCT_Lib.xTowingEDC.terminalId       = xOutCreditCard.szTerminalID ?? "";
                        TCT_Lib.xTowingEDC.transDate        = xOutCreditCard.szTransactionDate ?? "";
                        TCT_Lib.xTowingEDC.transTime        = xOutCreditCard.szTransactionTime ?? "";
                        TCT_Lib.xTowingEDC.transAmount      = xOutCreditCard.szAmount;
                        TCT_Lib.xTowingEDC.transRef         = xOutCreditCard.szRef3;
                        TCT_Lib.xTowingEDC.ref1             = xOutCreditCard.szRef1 ?? "";
                        TCT_Lib.xTowingEDC.ref2             = xOutCreditCard.szRef2 ?? "";
                        TCT_Lib.xTowingEDC.ref3             = xOutCreditCard.szRef3 ?? "";
                        //--------------------------------------
                        LogInfo.WriteLogEDC("+ Towing ResponseMessage \t= " + TCT_Lib.xTowingEDC.responseMessage);
                        LogInfo.WriteLogEDC("+ Towing ApproveCode \t= " + TCT_Lib.xTowingEDC.approveCode);
                        LogInfo.WriteLogEDC("+ Towing CardNo \t\t= " + TCT_Lib.xTowingEDC.cardNo);
                        LogInfo.WriteLogEDC("+ Towing TerminalId \t\t= " + TCT_Lib.xTowingEDC.terminalId);
                        LogInfo.WriteLogEDC("+ Towing TransDate \t\t= " + TCT_Lib.xTowingEDC.transDate);
                        LogInfo.WriteLogEDC("+ Towing TransTime \t\t= " + TCT_Lib.xTowingEDC.transTime);
                        LogInfo.WriteLogEDC("+ Towing TransAmount \t= " + TCT_Lib.xTowingEDC.transAmount);
                        LogInfo.WriteLogEDC("+ Towing TransRef \t\t= " + TCT_Lib.xTowingEDC.transRef);
                        LogInfo.WriteLogEDC("+ Towing TransTime \t\t= " + TCT_Lib.xTowingEDC.transTime);
                        LogInfo.WriteLogEDC("+ Towing Ref1 \t\t= " + TCT_Lib.xTowingEDC.ref1);
                        LogInfo.WriteLogEDC("+ Towing Ref2 \t\t= " + TCT_Lib.xTowingEDC.ref2);
                        LogInfo.WriteLogEDC("+ Towing Ref3 \t\t= " + TCT_Lib.xTowingEDC.ref3);

                        LogInfo.WriteLaneEvent($"EMV - Success Towing : CardNo={TCT_Lib.xTowingEDC.cardNo}, Amount={TCT_Lib.xTowingEDC.transAmount}, ApproveCode={TCT_Lib.xTowingEDC.approveCode}. TransRef={TCT_Lib.xTowingEDC.transRef}");

                        //-----------------------------------
/*
                        if (TCT_Lib.xTmpTowingEMV == null)
                        {
                            TCT_Lib.xTmpTowingEMV   = TCT_Lib.xTowingEMV;
                            TCT_Lib.xTmpTowingEDC   = TCT_Lib.xTowingEDC;

                            TCT_Lib.xTowingEMV      = null;
                            TCT_Lib.xTowingEDC      = null;
                        }
*/                        
                    }
                    //-------------------------------------

                    //this.Respond(ResponseType.Ok);
                    //
                    //Console.WriteLine("Start 1");
                    //-------------------------------------------------
                    try
                    {
                        TCT_Lib.bActiveEMVDlg = false;
                        TCT_Lib.laneController.manageProcessEMV -= LaneController_ManageProcessEMV;
                        RespondOnGtk(ResponseType.Ok);   // v2.0.2: was Respond() on socket thread

                        Thread.Sleep(500);
                        LogInfo.WriteLogEDC("+ Recv Socket Close / Disconnect when success payment");
                        if (recvSocket != null)
                        {
                            recvSocket.Close();
                            recvSocket.Dispose();
                            recvSocket = null;
                        }
                        LogInfo.WriteLogEDC("+ Success Close RecvSocket");

/*
                        LogInfo.WriteLogEDC("+ Before Clear Thread");
                        if (eventThread != null)
                        {
                            eventThread.Abort();
                            eventThread = null;
                        }
                        LogInfo.WriteLogEDC("+ After Clear Thread");
*/                                               

                        //--- v2.0.2: Dispose()/Destroy() removed (socket thread) - MainWindow disposes on GTK thread

                        return;
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine("- Error AnalysisEdcMsg EMV Payment : " + err.Message);
                        LogInfo.WriteLogEDC("- Error AnalysisEdcMsg EMV Payment : " + err.Message);

/*
                        LogInfo.WriteLogEDC("+ Before Clear Thread");
                        if (eventThread != null)
                        {
                            eventThread.Abort();
                            eventThread = null;
                        }
                        LogInfo.WriteLogEDC("+ After Clear Thread");
*/                                               

                        LogInfo.WriteLogEDC("+ Before Dispose");
                        Dispose();
                        LogInfo.WriteLogEDC("+ After Dispose");

                        LogInfo.WriteLogEDC("+ Before Destroy");
                        Destroy();
                        LogInfo.WriteLogEDC("+ After Destroy");
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine("- Error AnalysisEdcMsg EMV Payment : " + err.Message);
                    LogInfo.WriteLogEDC("- Error AnalysisEdcMsg EMV Payment : " + err.Message);

/*
                    LogInfo.WriteLogEDC("+ Before Clear Thread");
                    if (eventThread != null)
                    {
                        eventThread.Abort();
                        eventThread = null;
                    }
                    LogInfo.WriteLogEDC("+ After Clear Thread");
*/


                    TCT_Lib.bActiveEMVDlg = false;
                    TCT_Lib.laneController.manageProcessEMV -= LaneController_ManageProcessEMV;
                    //Console.WriteLine("Start 5");
                    LogInfo.WriteLogEDC("+ Before Dispose");
                    Dispose();
                    LogInfo.WriteLogEDC("+ After Dispose");

                    LogInfo.WriteLogEDC("+ Before Destroy");
                    Destroy();
                    LogInfo.WriteLogEDC("+ After Destroy");
                }
            //});
        }

        private bool Process_CancelPayEDC(bool bRepeat = false)
        {
            LogInfo.WriteLaneEvent($"EMV - Process Cancel Pay EDC");
            LogInfo.WriteLogEDC("+-- Start Cancel Payment ---");
            byte[] byteMsg      = new byte[300];
            string szErrMsg     = "", szCmdMsg = "";
            bool bResult        = ClassLibEDC_V1.GenerateCancelEdcMessage_V1(ref byteMsg, ref szCmdMsg, ref szErrMsg);
            string szStrMsg     = ClassLibEDC_V1.GenHexCharList(byteMsg);
            LogInfo.WriteLogEDC("+ Cancel Credit Payment Hex : " + szStrMsg);

            //-------------------------------------------------
            IPAddress ipAddr            = IPAddress.Parse(TCT_Lib.szEdcIPAddress);
            IPEndPoint sendEndPoint     = new IPEndPoint(ipAddr, TCT_Lib.iEdcSendPort);
            System.Net.Sockets.Socket sendSocket = new System.Net.Sockets.Socket(sendEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sendSocket.SendTimeout      = 5 * 1000;
            sendSocket.ReceiveTimeout   = 5 * 1000;

            try
            {
                //string szStr    = EdcLib.GenHexCharList(byteMsg);
                //Console.WriteLine(szStr);

                sendSocket.Connect(sendEndPoint);
                sendSocket.Send(byteMsg);

                Application.Invoke(delegate
                { 
                    txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(0, 255, 0));
                    txtMsg.Text = "โปรดรอการยกเลิก";
                });
                //--- Wait Ack
                byte[] bAck     = new byte[1];
                int iTotalByte  = sendSocket.Receive(bAck);

                string szResultStr  = ClassLibEDC_V1.GenHexCharList(bAck);
                int iAck            = bAck[0];
                if (iAck != 0x05) // Ack
                {
                    Application.Invoke(delegate
                    {
                        txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                        if (iAck == 0x04)
                        {
                            //LogInfo.WriteLogEDC("+ Ack Cancel Payment EMV - EDC busy");
                            txtMsg.Text = "อุปกรณ์ไม่พร้อมทำงาน";
                        }
                        else
                        {
                            //txtMsg.Text = @"< Error!...Ack from EDC >";
                            Console.WriteLine("- Error...Ack from EDC. ---");
                            //LogInfo.WriteLogEDC("- Error Cancel Payment - Err Ack from EDC");
                            string szCharList2  = GenHexCharList(bAck);
                            txtMsg.Text         = "Unknow Ack : " + szCharList2;
                        }
                    });
                    if (iAck == 0x04)
                        LogInfo.WriteLogEDC("+ Ack Cancel Payment EMV - EDC busy");
                    else
                        LogInfo.WriteLogEDC("- Error Cancel Payment - Err Ack from EDC");
                    //-----------------------------------------
                    string szCharList = GenHexCharList(bAck);
                    LogInfo.WriteLogEDC("+ EDC Ack Cancel Payment : " + szCharList);
                    //-----------------------------------------
                }
                //sendSocket.Shutdown(SocketShutdown.Both);
                sendSocket.Close();

                return true;
                
            }
            catch (SocketException err)
            {
                //sendSocket.Shutdown(SocketShutdown.Both);
                sendSocket.Close();
                Console.WriteLine("- Error Send Cancel Credit Payment : " + err.Message);

                return false;
            }
        }

        protected void OnBtnCancelClicked(object sender, EventArgs e)
        {

#if __TEST_EDC__
            SimEMV();
            TCT_Lib.bActiveEMVDlg = false;
            TCT_Lib.laneController.manageProcessEMV -= LaneController_ManageProcessEMV;
            this.Respond(ResponseType.Ok);
            Dispose();
            Destroy();
            return;
#else
            Process_CancelEMV();
#endif
        }

        protected void Process_CancelEMV()
        {
            if (!bError)
            {
                if (bStartCmd)
                {
                    if (!Process_CancelPayEDC())
                        return;
                }
            }

            bCancel = true; 

            try
            {

                if (!bTowing)
                {
                    TCT_Lib.xCurEMV     = null;
                    TCT_Lib.xCurEDC     = null;
                }
                else
                {
                    TCT_Lib.xTowingEMV  = null;
                    TCT_Lib.xTowingEDC  = null;
                }

                Thread.Sleep(500);
                LogInfo.WriteLogEDC("+ Start Recv Socket Close / Disconnect (Cancel Payment)");
                if (recvSocket != null)
                {
                    LogInfo.WriteLogEDC("+ Recv Socket Close / Disconnect when cancel payment");
                    recvSocket.Close();
                    recvSocket.Dispose();
                }
                LogInfo.WriteLogEDC("+ End Recv Socket Close / Disconnect (Cancel Payment)");
            }
            catch (Exception err)
            {
                LogInfo.WriteLogEDC("- Error Cancel CreditCard : " + err.Message);
            }
            finally
            {
                LogInfo.WriteLogEDC("+ Complete Cancel CreditCard Payment");
                TCT_Lib.bActiveEMVDlg = false;

                //----------------------------------------------------------
                this.Respond(ResponseType.Cancel);
                Dispose();
                Destroy();
                //---------------------------
                //--- Simulate
#if __TEST_EDC__
                SimEMV();
                this.Respond(ResponseType.Ok);
#endif
                //-----------------------------
                //TCT_Lib.laneController.manageProcessEMV -= LaneController_ManageProcessEMV;
                /*
                                Application.Invoke(delegate
                                {
                                    TCT_Lib.bActiveEMVDlg = false;
                                    //eventThread.Abort();
                                    recvSocket.Close();
                                    recvSocket.Dispose();
                                });

                                Dispose();
                                Destroy();
                */
            }
        }

        //--- v2.0.2: GTK is not thread-safe - the socket threads must not Respond/Dispose/Destroy this
        //--- dialog directly (native crash in gtk_dialog_run). MainWindow disposes it after Run().
        volatile bool bResponded = false;
        void RespondOnGtk(ResponseType xResponse)
        {
            if (bResponded)
                return;
            bResponded = true;
            Application.Invoke(delegate
            {
                try
                {
                    this.Respond(xResponse);
                }
                catch (Exception err)
                {
                    LogInfo.WriteLogEDC("- Error RespondOnGtk : " + err.Message);
                }
            });
        }

        //--- v2.0.2: KeyPressEvent was never wired (gtk-gui) -> USB keyboard could not cancel this popup.
        //--- ConnectBefore + RetVal so ENTER does not also press the focused button.
        [GLib.ConnectBefore]
        protected void OnKeyPressEvent(object o, KeyPressEventArgs args)
        {
            if (TCT_Lib.keyMapping.ContainsKey((int)args.Event.KeyValue))
            {
                TCT_Lib.webSocketLane.SendVirtualKeyboard(TCT_Lib.keyMapping[(int)args.Event.KeyValue], 1);
                args.RetVal = true;
            }
        }

        void LaneController_ManageProcessEMV(int iKey)
        {
            Application.Invoke(delegate
            {
                if ((iKey == (int)KeyPressedType.EDIT) || (iKey == (int)KeyPressedType.ESC))   // v2.0.2: + CANCEL key
                {
                    Process_CancelEMV();
                }
                else if (iKey == (int)KeyPressedType.ENTER)
                {
                    return;
                }
            });
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

        public void SimEMV()
        {
            TCT_Lib.xCurEMV = new EMV_Info();
            TCT_Lib.xCurEDC = new EDC_Info();

            if  (!bTowing)
            {
                TCT_Lib.xCurEMV.responseMessage = "Response";
                TCT_Lib.xCurEMV.respApproveCode = "Code";
                TCT_Lib.xCurEMV.respCardNo      = "Card";
                TCT_Lib.xCurEMV.respDate        = "22222";
                TCT_Lib.xCurEMV.respTime        = "332423";
                TCT_Lib.xCurEMV.respTerminalId  = "234235345";
                TCT_Lib.xCurEMV.respAmount      = "50";
                TCT_Lib.xCurEMV.respRef         = "423423";
                TCT_Lib.xCurEMV.respRef1        = "454";
                TCT_Lib.xCurEMV.respRef2        = "45345";
                TCT_Lib.xCurEMV.respRef3        = "345345";

                TCT_Lib.xCurEDC.responseMessage = "345345";
                TCT_Lib.xCurEDC.approveCode     = "435";
                TCT_Lib.xCurEDC.cardNo          = "345435";
                TCT_Lib.xCurEDC.terminalId      = "345345";
                TCT_Lib.xCurEDC.transDate       = "345345";
                TCT_Lib.xCurEDC.transTime       = "345345";
                TCT_Lib.xCurEDC.transAmount     = "345345";
                TCT_Lib.xCurEDC.transRef        = "34545";
                TCT_Lib.xCurEDC.ref1            = "345345";
                TCT_Lib.xCurEDC.ref2            = "345345";
                TCT_Lib.xCurEDC.ref3            = "34345";
            }
            else
            {
                TCT_Lib.xTowingEMV.responseMessage  = "Response Towing";
                TCT_Lib.xTowingEMV.respApproveCode  = "Code Towing";
                TCT_Lib.xTowingEMV.respCardNo       = "Card Towing";
                TCT_Lib.xTowingEMV.respDate         = "7777 T";
                TCT_Lib.xTowingEMV.respTime         = "8888 T";
                TCT_Lib.xTowingEMV.respTerminalId   = "9999 T";
                TCT_Lib.xTowingEMV.respAmount       = "7777";
                TCT_Lib.xTowingEMV.respRef          = "78787878";
                TCT_Lib.xTowingEMV.respRef1         = "89898989";
                TCT_Lib.xTowingEMV.respRef2         = "67676767";
                TCT_Lib.xTowingEMV.respRef3         = "565656565";

                TCT_Lib.xTowingEDC.responseMessage  = "Response Towing";
                TCT_Lib.xTowingEDC.approveCode      = "Code Towing";
                TCT_Lib.xTowingEDC.cardNo           = "Card Towing";
                TCT_Lib.xTowingEDC.terminalId       = "7777 T";
                TCT_Lib.xTowingEDC.transDate        = "8888 T";
                TCT_Lib.xTowingEDC.transTime        = "9999 T";
                TCT_Lib.xTowingEDC.transAmount      = "7777";
                TCT_Lib.xTowingEDC.transRef         = "78787878";
                TCT_Lib.xTowingEDC.ref1             = "89898989";
                TCT_Lib.xTowingEDC.ref2             = "67676767";
                TCT_Lib.xTowingEDC.ref3             = "565656565";
            }

            xOutCreditCard                  = new objCreditCardInfo();
            xOutCreditCard.szRef1           = xOutCreditCard.szRef1 ?? "";
            xOutCreditCard.szRef2           = xOutCreditCard.szRef2 ?? "";
            xOutCreditCard.szCardNo         = xOutCreditCard.szCardNo ?? "";
            xOutCreditCard.szInvoiceNo      = xOutCreditCard.szInvoiceNo ?? "";

            xOutCreditCard.szResponseText       = "345345";
            xOutCreditCard.szApprovalCode       = "111111";
            xOutCreditCard.szCardNo             = "345435";
            xOutCreditCard.szTerminalID         = "345345";
            xOutCreditCard.szTransactionDate    = "345345";
            xOutCreditCard.szTransactionTime    = "345345";
            xOutCreditCard.szAmount             = "345345";
            xOutCreditCard.szReferenceNo        = "34545";
            xOutCreditCard.szRef1               = "345345";
            xOutCreditCard.szRef2               = "345345";
            xOutCreditCard.szRef3               = "34345";

/*
            if (TCT_Lib.xTmpEMV == null)
            {
                TCT_Lib.xTmpEMV     = TCT_Lib.xCurEMV;
                TCT_Lib.xTmpEDC     = TCT_Lib.xCurEDC;

                TCT_Lib.xCurEMV     = null;
                TCT_Lib.xCurEDC     = null;
            }
*/            
        }

    }
}