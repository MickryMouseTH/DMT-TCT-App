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
    public partial class EdcPayQrDlg_V1 : Gtk.Dialog
    {
        decimal fPayAmount;
        bool bTowing;
        bool bCancel    = false;
        volatile bool bResponded = false;   // v2.0.2: dialog already answered (guard for cancel fallback)
        uint iTimerIdCancelFallback = 0;

        bool bRetryRecv = false;

        Thread eventThread;
        System.Net.Sockets.Socket recvSocket;
        bool bStartCmd              = false;
        bool bStartInquiry          = false;
        //uint iTimerSecAutoInquiry   = 10;        // Auto Send Inquiry in sec
        //uint iTimerIdInquiry        = 0;
        int iTotalMaxInquiry        = 3;
        int iCurCountInquiry        = 0;
        uint iTimerIdWaitInquiry    = 0;
        uint iTimerIdWaitCancel     = 0;
        uint iWaitSecInquiry        = 5;        // Wait 5 Sec when press inquiry
        //uint iWaitSecInquiry2       = 10;
        uint iWaitSecCancel         = 3;        // Wait 3 sec for press cancel
        int iInquiryIndex           = 1;
        string szWarningMsg         = "กรุณาติดต่อธนาคารผู้ออกบัตร";
        string szWarningTFI         = "ติดต่อธนาคาร";
        string szConfirmInquiry     = "โปรดตรวจสอบรายการการจ่าย เมื่อสำเร็จ";

        string szRef1 = "", szRef2 = "", szRef3 = "", szRef4 = "";
        public objCreditCardInfo xOutCreditCard;
        //----------------------------------------------------------------
        public EdcPayQrDlg_V1(decimal _fPayAmount, bool _bTowing)
        {
            TCT_Lib.bActiveEMVDlg = true;
            this.Build();
            UiTheme.StylePopup(this);     // v2.0.2: no title bar + visible frame
            //--- v2.0.2: popup buttons = same colour as the Cherry key, readable size
            UiTheme.StyleKey(btnInquiry, UiTheme.Teal, UiTheme.PopupBtnFont);
            UiTheme.SetButtonText(btnInquiry, "ตรวจสอบการจ่าย\n[ENTER]");     // v2.0.3: one line was cut off
            UiTheme.StyleKey(btnCancel,  UiTheme.Red,  UiTheme.PopupBtnFont);
            this.KeyPressEvent      += OnKeyPressEvent;     // v2.0.2: keyboard support
            this.Activate();

            TCT_Lib.eCurAppPage     = AppPage.EMV;
            fPayAmount              = _fPayAmount;
            bTowing                 = _bTowing;
            this.Title              = "Payment with QR Code V.1";

            this.HasSeparator       = false;
            this.BorderWidth        = 6;
            this.Resizable          = false;

            btnInquiry.Sensitive    = false;
            btnCancel.Sensitive     = false;

            LogInfo.WriteLogEDC("+------------------ Start QR Payment --------------------");
            LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ EdcPayQrDlg_V1 : open, amount={fPayAmount}, towing={bTowing}, edc={TCT_Lib.szEdcIPAddress}:{TCT_Lib.iEdcSendPort}, recvPort={TCT_Lib.iEdcRecvPort}, cancelBtnDelay={iWaitSecCancel}s, inquiryBtnDelay={iWaitSecInquiry}s");
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ QR popup opened : amount={fPayAmount}, towing={bTowing}");
            TCT_Lib.laneController.manageProcessEMV += LaneController_ManageProcessEMV;

            //this.DeleteEvent += new DeleteEventHandler(OnDlgDelete);

            Gdk.Pixbuf imgEMV   = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.PayQR2.png");
            imgQR.Pixbuf        = imgEMV;

            txtPayAmount.ModifyFont(FontDescription.FromString("Ubuntu Bold 70"));
            txtPayAmount.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
            txtPayAmount.Text   = fPayAmount.ToString("0") + " บาท";

            txtMsg.ModifyFont(FontDescription.FromString("Ubuntu 24"));
            txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(0, 255, 255));
            txtMsg.Text         = "โปรดรอลูกค้าสแกน QR แล้วกดตรวจสอบ";

/*
#if !__TEST_EDC__
            LogInfo.WriteLogEDC("+ Test EDC Connection");
            //if (!TCT_Lib.TestConnection(TCT_Lib.szEdcIPAddress, TCT_Lib.iEdcSendPort))
            bool bResult = IsPortReachable(TCT_Lib.szEdcIPAddress, TCT_Lib.iEdcSendPort, 300).GetAwaiter().GetResult();
            if (!bResult)
            {
                Console.WriteLine("- Fail Connect Send EDC Socket");
                LogInfo.WriteLogEDC("- Fail Test EDC Connection");
                txtMsg.ModifyFont(FontDescription.FromString("Ubuntu 24"));
                txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                txtMsg.Text         = "ไม่สามารถเชื่อมต่ออุปกรณ์";
                btnCancel.Sensitive = true;
                return;
            }
            LogInfo.WriteLogEDC("+ Success Test EDC Connection");
#endif
*/
            if (!bTowing)
            {
                TCT_Lib.xCurQR      = new EMV_Info();
                TCT_Lib.xCurEDC     = new EDC_Info();
            }
            else
            {
                TCT_Lib.xTowingQR   = new EMV_Info();
                TCT_Lib.xTowingEDC  = new EDC_Info();
            }

            InitSocketRecvEDC();
            Thread.Sleep(500);
            Process_StartGenerateQrCodeEDC();
            btnInquiry.Sensitive    = false;
            btnCancel.Sensitive     = false;
            iTimerIdWaitCancel      = GLib.Timeout.Add(iWaitSecCancel * 1000, new GLib.TimeoutHandler(Process_EnableCancelBtn));
            iTimerIdWaitInquiry     = GLib.Timeout.Add(iWaitSecInquiry * 1000, new GLib.TimeoutHandler(Process_EnableInquiryBtn));
        }

        public async Task<bool> IsPortReachable(string host, int port = 80, int msTimeout = 300)
        {
            if (string.IsNullOrEmpty(host))
                return false;
            //throw new ArgumentNullException("host");

            return await Task.Run(() =>
            {
                var clientDone = new ManualResetEvent(false);
                var reachable = false;
                var hostEntry = new DnsEndPoint(host, port);
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

        private bool Process_EnableCancelBtn()
        {
            LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ Process_EnableCancelBtn : cancel button enabled");
            btnCancel.Sensitive = true;
            iTimerIdWaitCancel  = 0;
            GLib.Source.Remove(iTimerIdWaitCancel);
            return false;
        }

        private bool Process_EnableInquiryBtn()
        {
            LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ Process_EnableInquiryBtn : inquiry button enabled");
            btnInquiry.Sensitive    = true;
            iTimerIdWaitInquiry     = 0;
            GLib.Source.Remove(iTimerIdWaitInquiry);
            return false;
        }

        void OnDlgDelete(object sender, DeleteEventArgs a)
        {
            LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ OnDlgDelete : dialog delete event -> abort recv thread");
            Application.Invoke(delegate
            {
                TCT_Lib.bActiveEMVDlg = false;
                eventThread.Abort();
                recvSocket.Close();
                recvSocket.Dispose();
            });
        }

        protected void OnDeleteEvent(object o, DeleteEventArgs args)
        {
            LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ OnDeleteEvent : dialog delete event -> abort recv thread");
            Application.Invoke(delegate
            {
                TCT_Lib.bActiveEMVDlg = false;
                eventThread.Abort();
                recvSocket.Close();
                recvSocket.Dispose();
            });
        }

        private void InitSocketRecvEDC()
        {
            eventThread                 = new Thread(new ThreadStart(ThreadEventRecvServer));
            eventThread.Priority        = ThreadPriority.Normal;
            eventThread.IsBackground    = true;
            eventThread.Start();
            LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ InitSocketRecvEDC : recv thread started");
        }

        private bool Process_EnableAutoInquiryBtn()
        {
            btnInquiry.Sensitive    = true;
            btnCancel.Sensitive     = true;
            iTimerIdWaitInquiry     = 0;
            bStartInquiry           = false;
            GLib.Source.Remove(iTimerIdWaitInquiry);
            return false;
        }

        private void StartInquiryCommand(bool bDisableCancel = true)
        {
            bStartInquiry           = true;
            btnInquiry.Sensitive    = false;
            if (bDisableCancel)
                btnCancel.Sensitive     = false;
            //iTimerIdWaitInquiry     = GLib.Timeout.Add((iCurCountInquiry <= 1 ? iWaitSecInquiry : iWaitSecInquiry2) * 1000, new GLib.TimeoutHandler(Process_EnableInquiryBtn));
            //iTimerIdWaitInquiry     = GLib.Timeout.Add((uint)(TCT_Lib.iEdcQrWaitTimeSec * 1000), new GLib.TimeoutHandler(Process_EnableInquiryBtn));
        }

        private void ThreadEventRecvServer()
        {
            //Application.Invoke(delegate
            {
                //IPAddress ipAddr        = IPAddress.Parse(TCT_Lib.szEdcIPAddress);
                IPAddress ipAddr        = IPAddress.Any;
                IPEndPoint recvEndPoint = new IPEndPoint(ipAddr, TCT_Lib.iEdcRecvPort);
                LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ ThreadEventRecvServer : start, port={TCT_Lib.iEdcRecvPort}");

                System.Net.Sockets.Socket clientSocket = null;
                try
                {

                    recvSocket                  = new System.Net.Sockets.Socket(recvEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    recvSocket.SendTimeout      = 3 * 1000;
                    recvSocket.ReceiveTimeout   = 3 * 1000;
                    recvSocket.Bind(recvEndPoint);
                    // Using Listen() method we create the Client list that will want to connect to Server
                    recvSocket.Listen(10);
                    LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ ThreadEventRecvServer : listening, accept/recv timeout=3000ms");

                    bool bRunning = true;
                    while (bRunning)
                    {
                        try
                        {
                            // Data buffer
                            byte[] bytes    = new Byte[2048];
                            //string data     = null;
                            clientSocket    = recvSocket.Accept();
                            LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ ThreadEventRecvServer : accepted " + ClassLibEDC.SafeEndPoint(clientSocket));
                            int numByte     = clientSocket.Receive(bytes);
                            LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ ThreadEventRecvServer : recv {numByte} bytes, bCancel={bCancel}");
                            if (numByte == 0)
                                LogInfo.WriteLogEDC(LogLevel.WARN, "- Warning ThreadEventRecvServer : empty frame (peer closed)");
                            byte[] byteData = new byte[numByte];
                            Array.Copy(bytes, 0, byteData, 0, numByte);
                            string szStrMsg = ClassLibEDC_V1.GenHexCharList(byteData);
                            Console.WriteLine("Response = " + szStrMsg);
                            LogInfo.WriteLogEDC("+ Recv Hex Message : " + szStrMsg);

                            //Console.WriteLine("Text received -> {0} ", data);
                            byte[] bAck = { 0x06 };
                            LogInfo.WriteLogEDC("+ Send Ack to EDC = " + bAck[0]);
                            // Send a message to Client
                            // using Send() method
                            clientSocket.Send(bAck);

                            if (bCancel)
                            {
                                LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ ThreadEventRecvServer : reply after cancel -> respond Cancel, thread exit");
                                TCT_Lib.bActiveEMVDlg = false;
                                TCT_Lib.laneController.manageProcessEMV -= LaneController_ManageProcessEMV;
                                //eventThread.Abort();
                                RespondOnGtk(ResponseType.Cancel);     // v2.0.2: was Respond() on this socket thread

                                if (clientSocket != null)
                                {
                                    //clientSocket.Shutdown(SocketShutdown.Both);
                                    clientSocket.Close();
                                    clientSocket = null;
                                }

                                recvSocket.Close();
                                recvSocket.Dispose();
                                recvSocket = null;

                                //--- v2.0.2: no Dispose()/Destroy() here (socket thread) - MainWindow disposes on GTK thread
                                bRunning = false;
                                return;
                            }
                            else
                            {
                                AnalysisEdcMsg(byteData);

                                bRunning = false;
                                LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ ThreadEventRecvServer : reply handled, responded={bResponded}, loop end");

                                /*
                                                                TCT_Lib.bActiveEMVDlg = false;
                                                                TCT_Lib.laneController.manageProcessEMV -= LaneController_ManageProcessEMV;
                                                                bResponded = true; this.Respond(ResponseType.Ok);

                                                                if (clientSocket != null)
                                                                {
                                                                    clientSocket.Shutdown(SocketShutdown.Both);
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

                            //--- v2.0.2: Accept() times out every 3 s (ReceiveTimeout on the listen socket).
                            //--- The card dialog keeps listening on this error; the QR dialog stopped,
                            //--- so the QR inquiry result (Inquiry button is enabled after 5 s) never arrived.
                            if ((err.Message == "Operation on non-blocking socket would block") && !bCancel && !bResponded)
                                bRunning = true;
                            if (bRunning)
                                LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ ThreadEventRecvServer : accept/recv timeout 3000ms, keep listening");
                            else if (bCancel || bResponded)
                                LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ ThreadEventRecvServer : socket closed ({err.SocketErrorCode}) after cancel/response, loop end");
                            else
                                LogInfo.WriteLogEDC(LogLevel.ERROR, $"- Error ThreadEventRecvServer : SocketException {err.SocketErrorCode} {err.Message}, loop end");
                        }
                        catch (Exception err)
                        {
                            bRunning = false;
                            Console.WriteLine(err.Message);
                            if (bCancel || bResponded)
                                LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ ThreadEventRecvServer : {err.GetType().Name} after cancel/response, loop end");
                            else
                            {
                                LogInfo.WriteLogEDC(LogLevel.ERROR, $"- Error ThreadEventRecvServer : {err.GetType().Name} {err.Message}, loop end");
                                LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ ThreadEventRecvServer : stack=" + err.StackTrace);
                            }
                            if (clientSocket != null)
                            {
                                //clientSocket.Shutdown(SocketShutdown.Both);
                                clientSocket.Close();
                            }
                        }
                        finally
                        {

                        }
                    }
                }
                catch (SocketException err)
                {
                    Console.WriteLine(err.Message);
                    LogInfo.WriteLogEDC(LogLevel.ERROR, $"- Error ThreadEventRecvServer : listen SocketException {err.SocketErrorCode} {err.Message}");
                    recvSocket.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    LogInfo.WriteLogEDC(LogLevel.ERROR, $"- Error ThreadEventRecvServer : {e.GetType().Name} {e.Message}");
                    LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ ThreadEventRecvServer : stack=" + e.StackTrace);
                }

                finally
                {
                    LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ ThreadEventRecvServer : thread exit, bCancel={bCancel}, responded={bResponded}");
                    if (clientSocket != null)
                    {
                        //clientSocket.Shutdown(SocketShutdown.Both);
                        clientSocket.Close();
                    }
                    if (recvSocket != null)
                        recvSocket.Close();
                }
            }
            //);
        }

        void AnalysisEdcMsg(byte[] byteMsg)
        {
            //Application.Invoke(async delegate {
                try
                {
                    string szResponse = Encoding.ASCII.GetString(byteMsg);
                    LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ AnalysisEdcMsg : len={byteMsg?.Length ?? 0}, towing={bTowing}, inquiryCount={iCurCountInquiry}");
                    //Console.WriteLine("+ Result Response : " + szResponse);
                    //LogInfo.WriteLogEDC("+ EDC Result Response (Pay QR) : " + szResponse);

                    string szCharList = ClassLibEDC_V1.GenHexCharList(Encoding.ASCII.GetString(byteMsg));
                    Console.WriteLine("Byte Data : " + szCharList);
                    LogInfo.WriteLogEDC("+ EDC Response Byte Data (Pay QR): " + szCharList);

                    //if (!bStartInquiry)
                    //    return;

                    if (!bTowing)
                        TCT_Lib.xCurQR.responseText = "";
                    else
                        TCT_Lib.xTowingQR.responseText = "";
                    //txtResponseData.Text = szCharList;
                    // v2.0.0: guard against short/malformed EDC response (was IndexOutOfRange crash)
                    if (szResponse == null || szResponse.Length < 19)
                    {
                        LogInfo.WriteLogEDC($"- Error : EDC QR response too short (len={szResponse?.Length ?? 0}) : {szResponse}");
                        return;
                    }
                    string szRespCode = szResponse.Substring(17, 2);
                    Console.WriteLine("+ Response Code : " + szRespCode);
                    Console.WriteLine("-");
                    LogInfo.WriteLogEDC("+ EDC Response Code (Pay QR) : " + szRespCode);
                    LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ AnalysisEdcMsg : transCode={szResponse.Substring(15, 2)}, respCode={szRespCode} ({ClassLibEDC_V1.GetResponseMsg(szRespCode)})");
                    if (szRespCode != "00")
                    {
                        string szErrCodeMsg     = ClassLibEDC_V1.GetResponseMsg(szRespCode);
                        string szErrorShowMsg   = ClassLibEDC_V1.ShowResponseMsg(szRespCode);
                        //Console.WriteLine($"Error Code : {szRespCode}, Error Text : "+ szErrCodeMsg);
                        LogInfo.WriteLogEDC($"Error Code : {szRespCode}, Error Text : " + szErrCodeMsg);
                        LogInfo.WriteLaneEvent(LogLevel.WARN, $"- Warning QR not approved : code={szRespCode} ({szErrCodeMsg}), amount={fPayAmount}");
                        //txtMsg.Text         = $"{szErrorShowMsg}";
                        Application.Invoke(delegate {
                            txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                            txtMsg.Text = szErrorShowMsg;
                        });
                        
                        //---------------------------------------------------------------
                        //--- Show warning at TFI
                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, "", fPayAmount.ToString("0"), "", szWarningTFI, "");

                        //--------------------------------------------------------------
                        if (szRespCode == "12")
                        {
                            //--- TX not found, customer not scan QR
                            //-------------------------------------
                            LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ AnalysisEdcMsg : code 12 (QR not scanned) -> respond Reject");
                            RespondOnGtk(ResponseType.Reject);     // v2.0.2: was Respond() on socket thread -> native crash
                            bCancel = true;
                            //Console.WriteLine("Start 1");
                            //-------------------------------------------------
                            try
                            {

                            }
                            catch (Exception)
                            {
                                LogInfo.WriteLogEDC(LogLevel.ERROR, "- Error AnalysisEdcMsg : unexpected exception (code 12 cleanup)");
                            }
                            finally
                            {
                                //if (netServer != null)
                                //    netServer.Stop();

                                if (iTimerIdWaitInquiry > 0)
                                    GLib.Source.Remove(iTimerIdWaitInquiry);

                                TCT_Lib.bActiveEMVDlg = false;
                                TCT_Lib.laneController.manageProcessEMV -= LaneController_ManageProcessEMV;
                                //Console.WriteLine("Start 5");
                                //--- v2.0.2: no Dispose()/Destroy()/Abort() here (socket thread) - MainWindow disposes
                                //--- the dialog on the GTK thread; the receive loop ends by itself (bRunning=false).
                            }

                            return;
                        }
                        //--------------------------------------------------------------------

                        return;
                    }

                    //--- Check TransCode
                    // v2.0.0: guard against short response before Substring (was IndexOutOfRange crash)
                    if (szResponse.Length < 17)
                    {
                        LogInfo.WriteLogEDC($"- Error : EDC QR response too short for TransCode (len={szResponse.Length}) : {szResponse}");
                        return;
                    }
                    string szTransCode = szResponse.Substring(15, 2);
                    if ((szTransCode != "IQ") && (szTransCode != "QR"))
                    {
                        LogInfo.WriteLogEDC($"Error TransCode : {szTransCode}, This trans cde is not inquiry qr payment result.");
                        return;
                    }

                    //LogInfo.WriteLogEDC("+ Response EMV QR : " + szResponse);
                    xOutCreditCard      = ClassLibEDC_V1.EncodeFormatEDC(szResponse);
                    LogInfo.WriteLogEDC("+ Sucess Encode QR Format.");
                    LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ AnalysisEdcMsg : parsed " + ClassLibEDC.CardInfoSummary(xOutCreditCard));
                    //txtCardNo.Text      = xCreditCard.szCardNo;
                    //txtAmount.Text      = xCreditCard.szAmount;
                    //txtTerminalID.Text  = xCreditCard.szTerminalID;
                    //txtDateTime.Text    = xCreditCard.szTransactionDate + " - " + xCreditCard.szTransactionTime;
                    //txtRef3.Text        = xCreditCard.szRef3;

                    if (xOutCreditCard.szResponseText.Trim() == "")
                    {
                        LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ AnalysisEdcMsg : empty response text -> not paid yet, wait next inquiry");
                        LogInfo.WriteLaneEvent($"QR CODE - Response : CardNo={xOutCreditCard.szCardNo ?? ""}, Amount={xOutCreditCard.szAmount ?? ""}, ApproveCode={xOutCreditCard.szApprovalCode ?? ""}. TransRef={xOutCreditCard.szRef3 ?? ""}, InvoiceNo={xOutCreditCard.szInvoiceNo ?? ""}");
                        Application.Invoke(delegate {
                            txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(0, 255, 0));
                            txtMsg.Text = szConfirmInquiry;
                        });
                        
                        return;
                    }
                    //--- Verify Data ----
                    if ((xOutCreditCard.szInvoiceNo ?? "") == "")
                    {
                        LogInfo.WriteLogEDC($"- Warning...Not found invoice no.)");
                        LogInfo.WriteLaneEvent(LogLevel.WARN, "- Warning QR result rejected : invoice no not found");

                        //----------------
                        int iAlarmID = (int)TCT_Lib.LaneDB.WriteLaneAlarmRTO(AlarmDevice.EDC_WARNING, AlarmType.Occurred, $"QR payment not found InvoiceNo", $"");
                        TCT_Lib.SendLaneAlarmObjToRTO(iAlarmID);

                        return;
                    }

                    if ((xOutCreditCard.szCardNo ?? "") != "")
                    {
                        LogInfo.WriteLogEDC($"- Warning...Found Card No={xOutCreditCard.szCardNo.Trim()}, then this is EMV, (Not QR)");
                        LogInfo.WriteLaneEvent(LogLevel.WARN, "- Warning QR result rejected : card no present (EMV result)");
                        //----------------

                        int iAlarmID = (int)TCT_Lib.LaneDB.WriteLaneAlarmRTO(AlarmDevice.EDC_WARNING, AlarmType.Occurred, $"QR payment is not match (Found CardNo={xOutCreditCard.szCardNo.Trim()})", $"Found CardNo of QR Payment");
                        TCT_Lib.SendLaneAlarmObjToRTO(iAlarmID);
                        return;
                    }
                    //------------------------------------------------

                    if (!bTowing)
                    {
                        TCT_Lib.xCurQR.responseMessage  = xOutCreditCard.szResponseText ?? "";
                        TCT_Lib.xCurQR.respApproveCode  = xOutCreditCard.szApprovalCode ?? "";
                        TCT_Lib.xCurQR.respCardNo       = xOutCreditCard.szCardNo ?? "";
                        TCT_Lib.xCurQR.respDate         = xOutCreditCard.szTransactionDate ?? "";
                        TCT_Lib.xCurQR.respTime         = xOutCreditCard.szTransactionTime ?? "";
                        TCT_Lib.xCurQR.respTerminalId   = xOutCreditCard.szTerminalID ?? "";
                        TCT_Lib.xCurQR.respAmount       = xOutCreditCard.szAmount ?? "";
                        TCT_Lib.xCurQR.respRef          = xOutCreditCard.szRef3 ?? "";
                        TCT_Lib.xCurQR.respInvoiceNo    = xOutCreditCard.szInvoiceNo ?? "";
                        TCT_Lib.xCurQR.respRef1         = xOutCreditCard.szRef1 ?? "";
                        TCT_Lib.xCurQR.respRef2         = xOutCreditCard.szRef2 ?? "";
                        TCT_Lib.xCurQR.respRef3         = xOutCreditCard.szRef3 ?? "";
                        if (TCT_Lib.xCurQR.respRef1 == "")
                            TCT_Lib.xCurQR.respRef1 = szRef1;
                        if (TCT_Lib.xCurQR.respRef2 == "")
                            TCT_Lib.xCurQR.respRef2 = szRef2;

                        TCT_Lib.xCurEDC.responseMessage = xOutCreditCard.szResponseText ?? "";
                        TCT_Lib.xCurEDC.approveCode     = xOutCreditCard.szApprovalCode ?? "";
                        TCT_Lib.xCurEDC.cardNo          = xOutCreditCard.szCardNo ?? "";
                        TCT_Lib.xCurEDC.terminalId      = xOutCreditCard.szTerminalID ?? "";
                        TCT_Lib.xCurEDC.transDate       = xOutCreditCard.szTransactionDate ?? "";
                        TCT_Lib.xCurEDC.transTime       = xOutCreditCard.szTransactionTime ?? "";
                        TCT_Lib.xCurEDC.transAmount     = xOutCreditCard.szAmount ?? "";
                        TCT_Lib.xCurEDC.transRef        = xOutCreditCard.szRef3 ?? "";
                        TCT_Lib.xCurEDC.invoiceNo       = xOutCreditCard.szInvoiceNo ?? "";
                        TCT_Lib.xCurEDC.ref1            = xOutCreditCard.szRef1 ?? "";
                        TCT_Lib.xCurEDC.ref2            = xOutCreditCard.szRef2 ?? "";
                        TCT_Lib.xCurEDC.ref3            = xOutCreditCard.szRef3 ?? "";
                        if (TCT_Lib.xCurEDC.ref1 == "")
                            TCT_Lib.xCurEDC.ref1 = szRef1;
                        if (TCT_Lib.xCurEDC.ref2 == "")
                            TCT_Lib.xCurEDC.ref2 = szRef2;

                        //--------------------------------------
                        LogInfo.WriteLogEDC("+ ResponseMessage = " + TCT_Lib.xCurQR.responseMessage);
                        LogInfo.WriteLogEDC("+ ApproveCode \t\t= " + TCT_Lib.xCurQR.respApproveCode);
                        LogInfo.WriteLogEDC("+ CardNo \t\t= " + TCT_Lib.xCurQR.respCardNo);
                        LogInfo.WriteLogEDC("+ TerminalId \t\t= " + TCT_Lib.xCurQR.respTerminalId);
                        LogInfo.WriteLogEDC("+ TransDate \t\t= " + TCT_Lib.xCurQR.respDate);
                        LogInfo.WriteLogEDC("+ TransTime \t\t= " + TCT_Lib.xCurQR.respTime);
                        LogInfo.WriteLogEDC("+ TransAmount \t= " + TCT_Lib.xCurQR.respAmount);
                        LogInfo.WriteLogEDC("+ TransRef \t\t= " + TCT_Lib.xCurQR.respRef);
                        LogInfo.WriteLogEDC("+ InvoiceNo \t= " + TCT_Lib.xCurQR.respInvoiceNo);
                        LogInfo.WriteLogEDC("+ Ref1 \t\t= " + TCT_Lib.xCurQR.respRef1);
                        LogInfo.WriteLogEDC("+ Ref2 \t\t= " + TCT_Lib.xCurQR.respRef2);
                        LogInfo.WriteLogEDC("+ Ref3 \t\t= " + TCT_Lib.xCurQR.respRef3);
                        LogInfo.WriteLogEDC($"QR : JobNo={TCT_Lib.iJobNo}, nTrx={TCT_Lib.nTrx}, CardNo={TCT_Lib.xCurQR.respCardNo}, ApproveCode={TCT_Lib.xCurQR.respApproveCode.Trim()}, Ref2={TCT_Lib.xCurQR.respRef2}, Ref3={TCT_Lib.xCurQR.respRef3}, InvoiceNo={TCT_Lib.xCurQR.respInvoiceNo}");
                        LogInfo.WriteLaneEvent($"QR Success : CardNo={TCT_Lib.xCurQR.respCardNo}, Amount={TCT_Lib.xCurQR.respAmount}, ApproveCode={TCT_Lib.xCurQR.respApproveCode}. TransRef={TCT_Lib.xCurQR.respRef}, InvoiceNo={TCT_Lib.xCurQR.respInvoiceNo}");

                        if (TCT_Lib.xCurQR.responseMessage.Trim() == "")
                        {
                            LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ AnalysisEdcMsg : empty response message -> wait next inquiry");
                            Application.Invoke(delegate {
                                txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(0, 255, 0));
                                txtMsg.Text = szConfirmInquiry;
                            });
                            
                            return;
                        }

                        //-----------------------------------
/*
                        if (TCT_Lib.xTmpQR != null)
                        {
                            TCT_Lib.xTmpQR      = TCT_Lib.xCurQR;
                            TCT_Lib.xTmpEDC     = TCT_Lib.xCurEDC;

                            TCT_Lib.xCurQR      = null;
                            TCT_Lib.xCurEDC     = null;
                        }
*/                        
                    }
                    else
                    {
                        TCT_Lib.xTowingQR.responseMessage   = xOutCreditCard.szResponseText ?? "";
                        TCT_Lib.xTowingQR.respApproveCode   = xOutCreditCard.szApprovalCode ?? "";
                        TCT_Lib.xTowingQR.respCardNo        = xOutCreditCard.szCardNo ?? "";
                        TCT_Lib.xTowingQR.respDate          = xOutCreditCard.szTransactionDate ?? "";
                        TCT_Lib.xTowingQR.respTime          = xOutCreditCard.szTransactionTime ?? "";
                        TCT_Lib.xTowingQR.respTerminalId    = xOutCreditCard.szTerminalID ?? "";
                        TCT_Lib.xTowingQR.respAmount        = xOutCreditCard.szAmount ?? "";
                        TCT_Lib.xTowingQR.respRef           = xOutCreditCard.szRef3 ?? "";
                        TCT_Lib.xTowingQR.respInvoiceNo     = xOutCreditCard.szInvoiceNo ?? "";
                        TCT_Lib.xTowingQR.respRef1          = xOutCreditCard.szRef1 ?? "";
                        TCT_Lib.xTowingQR.respRef2          = xOutCreditCard.szRef2 ?? "";
                        TCT_Lib.xTowingQR.respRef3          = xOutCreditCard.szRef3 ?? "";
                        if (TCT_Lib.xTowingQR.respRef1 == "")
                            TCT_Lib.xTowingQR.respRef1 = szRef1;
                        if (TCT_Lib.xTowingQR.respRef2 == "")
                            TCT_Lib.xTowingQR.respRef2 = szRef2;

                        TCT_Lib.xTowingEDC.responseMessage  = xOutCreditCard.szResponseText ?? "";
                        TCT_Lib.xTowingEDC.approveCode      = xOutCreditCard.szApprovalCode ?? "";
                        TCT_Lib.xTowingEDC.cardNo           = xOutCreditCard.szCardNo ?? "";
                        TCT_Lib.xTowingEDC.terminalId       = xOutCreditCard.szTerminalID ?? "";
                        TCT_Lib.xTowingEDC.transDate        = xOutCreditCard.szTransactionDate ?? "";
                        TCT_Lib.xTowingEDC.transTime        = xOutCreditCard.szTransactionTime ?? "";
                        TCT_Lib.xTowingEDC.transAmount      = xOutCreditCard.szAmount ?? "";
                        TCT_Lib.xTowingEDC.transRef         = xOutCreditCard.szRef3 ?? "";
                        TCT_Lib.xTowingEDC.invoiceNo        = xOutCreditCard.szInvoiceNo ?? "";
                        TCT_Lib.xTowingEDC.ref1             = xOutCreditCard.szRef1 ?? "";
                        TCT_Lib.xTowingEDC.ref2             = xOutCreditCard.szRef2 ?? "";
                        TCT_Lib.xTowingEDC.ref3             = xOutCreditCard.szRef3 ?? "";
                        if (TCT_Lib.xTowingEDC.ref1 == "")
                            TCT_Lib.xTowingEDC.ref1 = szRef1;
                        if (TCT_Lib.xTowingEDC.ref2 == "")
                            TCT_Lib.xTowingEDC.ref2 = szRef2;

                        //--------------------------------------
                        LogInfo.WriteLogEDC("+ Towing ResponseMessage = " + TCT_Lib.xTowingEDC.responseMessage);
                        LogInfo.WriteLogEDC("+ Towing ApproveCode = " + TCT_Lib.xTowingEDC.approveCode);
                        LogInfo.WriteLogEDC("+ Towing CardNo = " + TCT_Lib.xTowingEDC.cardNo);
                        LogInfo.WriteLogEDC("+ TTowing erminalId = " + TCT_Lib.xTowingEDC.terminalId);
                        LogInfo.WriteLogEDC("+ Towing TransDate = " + TCT_Lib.xTowingEDC.transDate);
                        LogInfo.WriteLogEDC("+ Towing TransTime = " + TCT_Lib.xTowingEDC.transTime);
                        LogInfo.WriteLogEDC("+ Towing TransAmount = " + TCT_Lib.xTowingEDC.transAmount);
                        LogInfo.WriteLogEDC("+ Towing TransRef = " + TCT_Lib.xTowingEDC.transRef);
                        LogInfo.WriteLogEDC("+ Towing TransTime = " + TCT_Lib.xTowingEDC.transTime);
                        LogInfo.WriteLogEDC("+ Towing InvoiceNo " + TCT_Lib.xTowingEDC.invoiceNo);
                        LogInfo.WriteLogEDC("+ Towing Ref1 = " + TCT_Lib.xTowingEDC.ref1);
                        LogInfo.WriteLogEDC("+ Towing Ref2 = " + TCT_Lib.xTowingEDC.ref2);
                        LogInfo.WriteLogEDC("+ Towing Ref3 = " + TCT_Lib.xTowingEDC.ref3);
                        LogInfo.WriteLaneEvent($"QR CODE - Success Towing : CardNo={TCT_Lib.xTowingQR.respCardNo}, Amount={TCT_Lib.xTowingQR.respAmount}, ApproveCode={TCT_Lib.xTowingQR.respApproveCode}. TransRef={TCT_Lib.xTowingQR.respRef}, InvoiceNo={TCT_Lib.xTowingQR.respInvoiceNo}");

                        if (TCT_Lib.xTowingQR.responseMessage.Trim() == "")
                        {
                            LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ AnalysisEdcMsg : towing empty response message -> wait next inquiry");
                            txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(0, 255, 0));
                            txtMsg.Text = szConfirmInquiry;
                            return;
                        }

                        //-----------------------------------
                        if (TCT_Lib.xTmpTowingQR != null)
                        {
                            TCT_Lib.xTmpTowingQR    = TCT_Lib.xTowingQR;
                            TCT_Lib.xTmpTowingEDC   = TCT_Lib.xTowingEDC;

                            TCT_Lib.xTowingQR       = null;
                            TCT_Lib.xTowingEDC      = null;
                        }
                    }
                    Application.Invoke(delegate {
                        txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(0, 255, 0));
                        txtMsg.Text = "จ่ายสำเร็จ";
                    });
                    
                    //-------------------------------------
                    LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ AnalysisEdcMsg : approved -> respond Ok");
                    LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ QR popup closed : Ok, amount={fPayAmount}");
                    RespondOnGtk(ResponseType.Ok);     // v2.0.2: was Respond() + self Abort() on socket thread

                    //Console.WriteLine("Start 1");
                    //-------------------------------------------------
                    try
                    {
                    }
                    catch(Exception)
                    {
                        LogInfo.WriteLogEDC(LogLevel.ERROR, "- Error AnalysisEdcMsg : unexpected exception (success cleanup)");
                    }
                    finally
                    {
                        //if (netServer != null)
                        //    netServer.Stop();

                        if (iTimerIdWaitInquiry > 0)
                            GLib.Source.Remove(iTimerIdWaitInquiry);

                        TCT_Lib.bActiveEMVDlg = false;
                        TCT_Lib.laneController.manageProcessEMV -= LaneController_ManageProcessEMV;
                        //Console.WriteLine("Start 5");
                        //--- v2.0.2: Dispose()/Destroy() removed (socket thread) - MainWindow disposes on GTK thread

                        LogInfo.WriteLogEDC("-------------------- COMPLETE EDC QR PAYMENT -----------------");
                        //Console.WriteLine("Start 6");
                        //return;
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine("- Error AnalysisEdcMsg QR Payment : " + err.Message);
                    LogInfo.WriteLogEDC("- Error AnalysisEdcMsg QR Payment : " + err.Message);
                    LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ AnalysisEdcMsg : stack=" + err.StackTrace);
                }
            //});
        }

        private void Process_StartGenerateQrCodeEDC()
        {
#if __TEST_EDC__
            return;
#endif
            try
            {
                LogInfo.WriteLaneEvent($"QR Code - Genreate Code Payment");
                LogInfo.WriteLogEDC("+-- Start Generate QR Code Payment ---");
                decimal fAmount = fPayAmount;

                //----------------------------------------------------------
                szRef1 = szRef2 = "";
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

                string szTID    = TCT_Lib.szEdcTID;
                szRef2          = szTID + DateTime.Now.ToString("yyMMddHHmmss");

                szRef4          = DateTime.Today.ToString("MMdd") + TCT_Lib.iJobNo.ToString().PadLeft(3, '0') + iEdc_nTrx.ToString().PadLeft(3, '0');

                LogInfo.WriteLogEDC($"+ Generate EDC QR Code (#1) : Ref1={szRef1}, Ref2={szRef2}, Ref4={szRef4}");
                //----------------------------------------------------------
                byte[] byteMsg  = new byte[300];
                string szErrMsg = "", szReqMsg  = "";

                bool bResult        = ClassLibEDC_V1.GenerateGenQrCodeEdcMessage_V1(fAmount, szRef1, szRef2, ref byteMsg, ref szReqMsg, ref szErrMsg);
                LogInfo.WriteLogEDC($"+ Generate EDC QR Code (#2) : Ref1={szRef1}, Ref2={szRef2}");
                string szStrMsg = ClassLibEDC_V1.GenHexCharList(byteMsg);
                LogInfo.WriteLogEDC("+ QR Payment Hex Message : " + szStrMsg);
                LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ Process_StartGenerateQrCodeEDC : build cmd=QR result={bResult}, err={szErrMsg}, amount={fAmount}, frameLen={byteMsg?.Length ?? 0}, jobNo={TCT_Lib.iJobNo}, nTrx={iEdc_nTrx}");

                TCT_Lib.szQrRef1    = szRef1;
                TCT_Lib.szQrRef2    = szRef2;
                if (!bTowing)
                    TCT_Lib.xCurQR.requestText = "";
                else
                    TCT_Lib.xTowingQR.requestText = "";

                //-----------------------------------------------------------------------
                IPAddress ipAddr            = IPAddress.Parse(TCT_Lib.szEdcIPAddress);
                IPEndPoint sendEndPoint     = new IPEndPoint(ipAddr, TCT_Lib.iEdcSendPort);
                System.Net.Sockets.Socket sendSocket       = new System.Net.Sockets.Socket(sendEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                sendSocket.SendTimeout      = 3 * 1000;
                sendSocket.ReceiveTimeout   = 3 * 1000;
                var swEdc                   = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    //string szStr    = EdcLib.GenHexCharList(byteMsg);
                    //Console.WriteLine(szStr);

                    LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ Process_StartGenerateQrCodeEDC : connect {TCT_Lib.szEdcIPAddress}:{TCT_Lib.iEdcSendPort}, sendTimeout=3000ms, recvTimeout=3000ms");
                    sendSocket.Connect(sendEndPoint);
                    LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ Process_StartGenerateQrCodeEDC : connected={sendSocket.Connected}, elapsed={swEdc.ElapsedMilliseconds}ms");
                    if (sendSocket.Connected)
                    {
                        sendSocket.Send(byteMsg);
                        LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ Process_StartGenerateQrCodeEDC : sent {byteMsg?.Length ?? 0} bytes, wait ack");
                        //--- Wait Ack
                        byte[] bAck     = new byte[1];
                        int iTotalByte  = sendSocket.Receive(bAck);

                        string szResultStr = ClassLibEDC_V1.GenHexCharList(bAck);
                        int iAck = bAck[0];
                        LogInfo.WriteLogEDC("+ Ack Data from EDC (QR Payment) : " + iAck);
                        LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ Process_StartGenerateQrCodeEDC : ack bytes={iTotalByte}, ack=0x{iAck:X2} ({ClassLibEDC.AckName(iAck)}), elapsed={swEdc.ElapsedMilliseconds}ms");
                        if (iAck != 0x06) // Ack
                        {
                            LogInfo.WriteLogEDC(LogLevel.WARN, $"- Warning Process_StartGenerateQrCodeEDC : ack=0x{iAck:X2} ({ClassLibEDC.AckName(iAck)}) is not ACK(06)");

                            if (iAck == 0x04)
                            {
                                LogInfo.WriteLogEDC("+ Ack Generate CreditCard Payment - EDC busy");
                                Application.Invoke(delegate {
                                    txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                                    txtMsg.Text = "อุปกรณ์ไม่พร้อมทำงาน";
                                });
                            }
                            else
                            {
                                //txtMsg.Text = @"< Error!...Ack from EDC >";
                                Console.WriteLine("- Error...Ack from EDC. ---");
                                LogInfo.WriteLogEDC("- Error CreditCard Payment - Err Ack from EDC");
                                string szCharList2  = GenHexCharList(bAck);
                                Application.Invoke(delegate {
                                    txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                                    txtMsg.Text = "Unknow Ack : " + szCharList2;
                                });

                            }
                            //-----------------------------------------
                            string szCharList = GenHexCharList(bAck);
                            LogInfo.WriteLogEDC("+ EDC Ack Payment : " + szCharList);
                            //-----------------------------------------

                        }
                        btnCancel.Sensitive = true;
                        bStartCmd           = true;
                        //if (!bStartInquiry)
                        //    StartInquiryCommand(false);

                        //sendSocket.Shutdown(SocketShutdown.Both);
                        sendSocket.Close();
                        LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ Process_StartGenerateQrCodeEDC : request done, bStartCmd={bStartCmd}, total={swEdc.ElapsedMilliseconds}ms");
                    }
                    else
                        LogInfo.WriteLogEDC(LogLevel.WARN, "- Warning Process_StartGenerateQrCodeEDC : socket not connected, QR request not sent");

                    return;
                }
                catch (SocketException err)
                {
                    //sendSocket.Shutdown(SocketShutdown.Both);
                    sendSocket.Close();
                    Console.WriteLine("- Error Send QR Payment : " + err.Message);
                    Console.WriteLine("- Error Send Credit Payment : " + err.Message);
                    LogInfo.WriteLogEDC(LogLevel.ERROR, $"- Error Process_StartGenerateQrCodeEDC : SocketException {err.SocketErrorCode} {err.Message}, elapsed={swEdc.ElapsedMilliseconds}ms");
                    LogInfo.WriteLaneEvent(LogLevel.WARN, $"- Warning QR request not sent : {err.Message}");
                    if (err.Message == "No route to host")      // Not found EDC Hosting
                    {
                        Console.WriteLine("- Fail Connect Send EDC Socket");
                        LogInfo.WriteLogEDC("- Fail Test EDC Connection");
                        Application.Invoke(delegate {
                            txtMsg.ModifyFont(FontDescription.FromString("Ubuntu 26"));
                            txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                            txtMsg.Text = "ไม่สามารถเชื่อมต่ออุปกรณ์";
                        });

                        return;
                    }
                    if (err.Message == "Operation on non-blocking socket would block")
                    {
                        Console.WriteLine("- Error Send Credit Payment : " + err.Message);
                        LogInfo.WriteLogEDC("- Error Send Credit Payment : " + err.Message);
                        Application.Invoke(delegate {
                            txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                            szErrMsg = "อุปกรณ์ไม่พร้อมทำงาน";
                            txtMsg.Text = szErrMsg;
                        });

                    }
                    if (err.Message == "Address already in use")
                    {
                        Application.Invoke(delegate {
                            txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                            txtMsg.Text = szErrMsg;
                        });
                    }
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error Send QR Payment : " + err.Message);
                LogInfo.WriteLogEDC("- Error Send QR Payment : " + err.Message);
                LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ Process_StartGenerateQrCodeEDC : stack=" + err.StackTrace);
                Application.Invoke(delegate {
                    txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                    string szErrMsg = err.Message;
                    if (err.Message == "The socket is not connected")
                        szErrMsg = "อุปกรณ์ไม่พร้อมทำงาน";
                    txtMsg.Text = szErrMsg;
                });

                btnCancel.Sensitive = true;
            }
        }

        protected void OnBtnInquiryClicked(object sender, EventArgs e)
        {
            iCurCountInquiry++;
            LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ OnBtnInquiryClicked : inquiry #{iCurCountInquiry}");
            /*
                        iCurCountInquiry++;
                        if (iCurCountInquiry > iTotalMaxInquiry)
                        {
                            Process_CancelEMV();
                            return;
                        }
            */

#if !__TEST_EDC__
            /*
                        if (!TCT_Lib.TestConnection(TCT_Lib.szEdcIPAddress, TCT_Lib.iEdcSendPort))

                        {
                            bRetryRecv  = true;
                            Console.WriteLine("- Fail Connect Send EDC Socket");
                            txtMsg.ModifyFont(FontDescription.FromString("Ubuntu 24"));
                            txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                            txtMsg.Text             = "ไม่สามารถเชื่อมต่ออุปกรณ์";

                            bStartInquiry           = false;
                            btnInquiry.Sensitive    = false;
                            btnCancel.Sensitive     = true;

                            return;
                        }
            */
#endif

            //if (!bStartInquiry)
            //    StartInquiryCommand();

            btnInquiry.Sensitive = false;
            Process_InquiryQrCodeEDC();
        }

        private void Process_InquiryQrCodeEDC(bool bRepeat = false)
        {
#if __TEST_EDC__
            return false;
#endif

            //Application.Invoke(delegate {
                try
                {
                    LogInfo.WriteLaneEvent($"QR Code - Inquiry QR Code");
                    LogInfo.WriteLogEDC("+-- Start Inquiry QR Code ---");

                    byte[] byteMsg      = new byte[300];
                    string szErrMsg     = "", szReqMsg = "";
                    string szInvoiceNo  = "000000";
                    bool bResult        = ClassLibEDC_V1.GenerateInquiryQrEdcMessage_V1(szInvoiceNo, ref byteMsg, ref szReqMsg, ref szErrMsg);
                    LogInfo.WriteLogEDC($"+ Generate EDC Inquiry QR : InvoiceNo={szInvoiceNo}");
                    string szStrMsg     = ClassLibEDC_V1.GenHexCharList(byteMsg);
                    LogInfo.WriteLogEDC("+ Inquiry QR Hex Message : " + szStrMsg);
                    LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ Process_InquiryQrCodeEDC : attempt #{iCurCountInquiry}, build cmd=IQ result={bResult}, err={szErrMsg}, frameLen={byteMsg?.Length ?? 0}");

                    if (!bTowing)
                        TCT_Lib.xCurQR.requestText = "";
                    else
                        TCT_Lib.xTowingQR.requestText = "";

                    //-----------------------------------------------------------------------
                    IPAddress ipAddr        = IPAddress.Parse(TCT_Lib.szEdcIPAddress);
                    IPEndPoint sendEndPoint = new IPEndPoint(ipAddr, TCT_Lib.iEdcSendPort);
                    System.Net.Sockets.Socket sendSocket = new System.Net.Sockets.Socket(sendEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    var swEdc               = System.Diagnostics.Stopwatch.StartNew();

                    try
                    {
                        //string szStr    = EdcLib.GenHexCharList(byteMsg);
                        //Console.WriteLine(szStr);

                        LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ Process_InquiryQrCodeEDC : connect {TCT_Lib.szEdcIPAddress}:{TCT_Lib.iEdcSendPort}, timeout=default");
                        sendSocket.Connect(sendEndPoint);
                        LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ Process_InquiryQrCodeEDC : connected={sendSocket.Connected}, elapsed={swEdc.ElapsedMilliseconds}ms");
                        //Thread.Sleep(1000);
                        if (sendSocket.Connected)
                        {
                            sendSocket.Send(byteMsg);
                            LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ Process_InquiryQrCodeEDC : sent {byteMsg?.Length ?? 0} bytes, wait ack");

                            //Thread.Sleep(100);
                            //--- Wait Ack
                            byte[] bAck     = new byte[1];
                            int iTotalByte  = sendSocket.Receive(bAck);

                            string szResultStr = ClassLibEDC_V1.GenHexCharList(bAck);
                            int iAck = bAck[0];
                            LogInfo.WriteLogEDC("+ Ack Data from EDC (QR Inquiry) : " + iAck);
                            LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ Process_InquiryQrCodeEDC : attempt #{iCurCountInquiry} ack bytes={iTotalByte}, ack=0x{iAck:X2} ({ClassLibEDC.AckName(iAck)}), elapsed={swEdc.ElapsedMilliseconds}ms");
                            if (iAck != 0x06) // Ack
                            {
                                LogInfo.WriteLogEDC(LogLevel.WARN, $"- Warning Process_InquiryQrCodeEDC : ack=0x{iAck:X2} ({ClassLibEDC.AckName(iAck)}) is not ACK(06)");
                                if (iAck == 0x04)
                                {
                                    LogInfo.WriteLogEDC("+ Ack Generate Inquiry QR - EDC busy");
                                    Application.Invoke(delegate {
                                        txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                                        txtMsg.Text = "อุปกรณ์ไม่พร้อมทำงาน";
                                    });
                                }
                                else
                                {
                                    //txtMsg.Text = @"< Error!...Ack from EDC >";
                                    Console.WriteLine("- Error...Ack from EDC. ---");
                                    LogInfo.WriteLogEDC("- Error Inquiry QR - Err Ack from EDC");
                                    string szCharList2 = GenHexCharList(bAck);
                                    Application.Invoke(delegate {
                                        txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                                        txtMsg.Text = "Unknow Ack : " + szCharList2;
                                    });
                                }
                                //-----------------------------------------
                                string szCharList = GenHexCharList(bAck);
                                LogInfo.WriteLogEDC("+ EDC Ack Inquiry QR : " + szCharList);
                                //-----------------------------------------
                            }
                            bStartCmd = true;

                            //sendSocket.Shutdown(SocketShutdown.Both);
                            sendSocket.Close();

                        }
                        else
                            LogInfo.WriteLogEDC(LogLevel.WARN, "- Warning Process_InquiryQrCodeEDC : socket not connected, inquiry not sent");

                        return;
                    }
                    catch (SocketException err)
                    {
                        //sendSocket.Shutdown(SocketShutdown.Both);
                        sendSocket.Close();
                        Console.WriteLine("- Error Send Inquiry QR : " + err.Message);
                        LogInfo.WriteLogEDC(LogLevel.ERROR, $"- Error Process_InquiryQrCodeEDC : SocketException {err.SocketErrorCode} {err.Message}, elapsed={swEdc.ElapsedMilliseconds}ms");
                        LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ Process_InquiryQrCodeEDC : stack=" + err.StackTrace);
                    }

                }
                catch (Exception err)
                {
                    Console.WriteLine("- Error Send Inquiry QR : " + err.Message);
                    LogInfo.WriteLogEDC("- Error Send Inquiry QRt : " + err.Message);
                    LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ Process_InquiryQrCodeEDC : stack=" + err.StackTrace);
                    Application.Invoke(delegate {
                        txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                        string szErrMsg = err.Message;
                        if (err.Message == "The socket is not connected")
                            szErrMsg = "อุปกรณ์ไม่พร้อมทำงาน";
                        txtMsg.Text = szErrMsg;
                    });
                }
            //});
        }

        private bool Process_CancelPayEDC(bool bRepeat = false)
        {
            LogInfo.WriteLaneEvent($"EMV - Process Cancel QR EDC");
            LogInfo.WriteLogEDC("+-- Start Cancel QR Payment ---");
            byte[] byteMsg      = new byte[300];
            string szErrMsg     = "", szCmdMsg = "";
            bool bResult        = ClassLibEDC_V1.GenerateCancelEdcMessage_V1(ref byteMsg, ref szCmdMsg, ref szErrMsg);
            string szStrMsg     = ClassLibEDC_V1.GenHexCharList(byteMsg);
            LogInfo.WriteLogEDC("+ Cancel QR Payment Hex : " + szStrMsg);
            LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ Process_CancelPayEDC : build cmd=CP(Cancel) result={bResult}, err={szErrMsg}, frameLen={byteMsg?.Length ?? 0}");

            //-------------------------------------------------
            IPAddress ipAddr            = IPAddress.Parse(TCT_Lib.szEdcIPAddress);
            IPEndPoint sendEndPoint     = new IPEndPoint(ipAddr, TCT_Lib.iEdcSendPort);
            System.Net.Sockets.Socket sendSocket = new System.Net.Sockets.Socket(sendEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            var swEdc                   = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                //string szStr    = EdcLib.GenHexCharList(byteMsg);
                //Console.WriteLine(szStr);

                LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ Process_CancelPayEDC : connect {TCT_Lib.szEdcIPAddress}:{TCT_Lib.iEdcSendPort}, timeout=default");
                sendSocket.Connect(sendEndPoint);
                sendSocket.Send(byteMsg);
                LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ Process_CancelPayEDC : sent {byteMsg?.Length ?? 0} bytes, elapsed={swEdc.ElapsedMilliseconds}ms, wait ack");

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
                LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ Process_CancelPayEDC : ack bytes={iTotalByte}, ack=0x{iAck:X2} ({ClassLibEDC.AckName(iAck)}), elapsed={swEdc.ElapsedMilliseconds}ms");
                if (iAck != 0x05) // Ack
                {
                    LogInfo.WriteLogEDC(LogLevel.WARN, $"- Warning Process_CancelPayEDC : ack=0x{iAck:X2} is not expected 0x05");
                    Application.Invoke(delegate
                    {
                        txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                        if (iAck == 0x04)
                        {
                            //LogInfo.WriteLogEDC("+ Ack Generate CreditCard Payment - EDC busy");
                            txtMsg.Text = "อุปกรณ์ไม่พร้อมทำงาน";
                        }
                        else
                        {
                            //txtMsg.Text = @"< Error!...Ack from EDC >";
                            Console.WriteLine("- Error...Ack from EDC. ---");
                            //LogInfo.WriteLogEDC("- Error CreditCard Payment - Err Ack from EDC");
                            string szCharList2  = GenHexCharList(bAck);
                            txtMsg.Text         = "Unknow Ack : " + szCharList2;
                        }
                    });
                    if (iAck == 0x04)
                        LogInfo.WriteLogEDC("+ Ack Generate CreditCard Payment - EDC busy");
                    else
                        LogInfo.WriteLogEDC("- Error CreditCard Payment - Err Ack from EDC");
                    //-----------------------------------------
                    string szCharList = GenHexCharList(bAck);
                    LogInfo.WriteLogEDC("+ EDC Ack Payment : " + szCharList);
                    //-----------------------------------------
                }
                //sendSocket.Shutdown(SocketShutdown.Both);
                sendSocket.Close();
                bCancel = true;

                return true;
                
            }
            catch (SocketException err)
            {
                //sendSocket.Shutdown(SocketShutdown.Both);
                sendSocket.Close();
                Console.WriteLine("- Error Send Cancel QR Payment : " + err.Message);
                LogInfo.WriteLogEDC(LogLevel.ERROR, $"- Error Process_CancelPayEDC : SocketException {err.SocketErrorCode} {err.Message}, elapsed={swEdc.ElapsedMilliseconds}ms");
                LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ Process_CancelPayEDC : stack=" + err.StackTrace);

                return false;
            }
        }

        protected void OnBtnCancelClicked(object sender, EventArgs e)
        {
            LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ OnBtnCancelClicked : operator cancel");
            Process_CancelEMV();
        }

        protected void Process_CancelEMV()
        {
            LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ Process_CancelEMV : bStartCmd={bStartCmd}, bRetryRecv={bRetryRecv}, responded={bResponded}, inquiries={iCurCountInquiry}");
            if (!bRetryRecv)
            {
                if (bStartCmd)
                    Process_CancelPayEDC();
            }

            //--- v2.0.2: this popup only closed when the EDC sent a reply to RecvPort.
            //--- EDC offline (QR never sent) or no reply to CP -> popup could never be closed
            //--- (touch or keyboard). Close now / after 8 s fallback.
            if (!bStartCmd)
                CloseWithoutEdcReply("QR command was not sent (EDC offline)");
            else if (iTimerIdCancelFallback == 0)
                iTimerIdCancelFallback = GLib.Timeout.Add(5000, () =>
                {
                    LogInfo.WriteLogEDC(LogLevel.WARN, "- Warning Process_CancelEMV : no EDC reply 5000ms after cancel, fallback close");
                    iTimerIdCancelFallback = 0;
                    CloseWithoutEdcReply("no reply from EDC 5 s after cancel");
                    return false;
                });

            //if (iTimerIdInquiry > 0)
            //    GLib.Source.Remove(iTimerIdInquiry);

            if (iTimerIdWaitInquiry > 0)
                GLib.Source.Remove(iTimerIdWaitInquiry);

            if (!bTowing)
            {
                TCT_Lib.xCurQR      = null;
                TCT_Lib.xCurEDC     = null;
            }
            else
            {
                TCT_Lib.xTowingQR   = null;
                TCT_Lib.xTowingEDC  = null;
            }

            TCT_Lib.bActiveEMVDlg = false;
        }

        //--- v2.0.2: GTK is not thread-safe. The receive thread must not call Respond/Dispose/Destroy
        //--- itself (native crash in gtk_dialog_run). Ask the GTK main loop to answer the dialog;
        //--- MainWindow disposes the dialog after Run() returns.
        void RespondOnGtk(ResponseType xResponse)
        {
            LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ RespondOnGtk : {xResponse}, alreadyResponded={bResponded}");
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

        //--- v2.0.2: close the popup as Cancel when no EDC reply will arrive (runs on GTK thread)
        void CloseWithoutEdcReply(string szReason)
        {
            if (bResponded)
                return;
            bCancel = true;
            LogInfo.WriteLogEDC("+ Close QR dialog without EDC reply : " + szReason);
            LogInfo.WriteLaneEvent("+ Cancel QR Code : " + szReason);
            TCT_Lib.bActiveEMVDlg = false;
            TCT_Lib.laneController.manageProcessEMV -= LaneController_ManageProcessEMV;
            try
            {
                if (recvSocket != null)
                    recvSocket.Close();     // unblocks Accept() -> receive thread exits
            }
            catch (Exception err) { LogInfo.WriteLogEDC(LogLevel.ERROR, "- Error CloseWithoutEdcReply : " + err.Message); }
            bResponded = true;
            LogInfo.WriteLaneEvent(LogLevel.DEBUG, $"+ QR popup closed : Cancel, amount={fPayAmount}");
            this.Respond(ResponseType.Cancel);
        }

        //--- v2.0.2: KeyPressEvent was never wired (gtk-gui) -> USB keyboard could not cancel this popup.
        //--- ConnectBefore + RetVal so ENTER does not also press the focused button.
        [GLib.ConnectBefore]
        protected void OnKeyPressEvent(object o, KeyPressEventArgs args)
        {
            if (TCT_Lib.keyMapping.ContainsKey((int)args.Event.KeyValue))
            {
                TCT_Lib.webSocketLane.SendVirtualKeyboard(TCT_Lib.keyMapping[(int)args.Event.KeyValue]);
                args.RetVal = true;
            }
        }

        void LaneController_ManageProcessEMV(int iKey)
        {
            LogInfo.WriteLogEDC(LogLevel.DEBUG, $"+ LaneController_ManageProcessEMV : key={iKey}");
            Application.Invoke(delegate
            {
                if ((iKey == (int)KeyPressedType.EDIT) || (iKey == (int)KeyPressedType.ESC))   // v2.0.2: + CANCEL key
                {
                    Process_CancelEMV();
                }
                else if (iKey == (int)KeyPressedType.ENTER)
                {
                    Process_InquiryQrCodeEDC();
                }
            });
        }

        public static string GenHexCharList(byte[] bytes)
        {
            string szStr = "";
            foreach (Byte b in bytes)
                szStr += b.ToString("X2") + " ";

            return szStr;
        }

    }
}