//#define __TEST_EDC__

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gtk;
using Pango;

namespace TCT_App.EMV.V0
{
    public partial class EdcPayQrDlg : Gtk.Dialog
    {
        decimal fPayAmount;
        bool bTowing;
        bool bCancel = false;

        TcpClient client;
        NetworkStream stream;
        bool bRetryRecv = false;

        Thread eventThread;
        private TcpListener netServer;
        NetworkStream sendStream    = null;
        bool bStartCmd              = false;
        bool bStartInquiry          = false;
        //uint iTimerSecAutoInquiry   = 10;        // Auto Send Inquiry in sec
        //uint iTimerIdInquiry        = 0;
        int iTotalMaxInquiry        = 3;
        int iCurCountInquiry        = 0;
        uint iTimerIdWaitInquiry    = 0;
        //uint iWaitSecInquiry        = 5;        // Wait 5 Sec when press inquiry
        //uint iWaitSecInquiry2       = 10;
        int iInquiryIndex           = 1;
        string szWarningMsg         = "กรุณาติดต่อธนาคารผู้ออกบัตร";
        string szWarningTFI         = "ติดต่อธนาคาร";
        string szConfirmInquiry     = "โปรดตรวจสอบรายการการจ่าย เมื่อสำเร็จ";

        string szEdcRef1            = "";
        string szEdcRef2            = "";
        public objCreditCardInfo xOutCreditCard;
        //----------------------------------------------------------------
        public EdcPayQrDlg(decimal _fPayAmount, bool _bTowing)
        {
            TCT_Lib.bActiveEMVDlg = true;
            this.Build();
            UiTheme.StylePopup(this);     // v2.0.2: no title bar + visible frame
            //--- v2.0.2: popup buttons = same colour as the Cherry key, readable size
            UiTheme.StyleKey(btnInquiry, UiTheme.Teal, UiTheme.PopupBtnFont);
            UiTheme.SetButtonText(btnInquiry, "ตรวจสอบการจ่าย\n[ENTER]");     // v2.0.3: one line was cut off
            UiTheme.StyleKey(btnCancel,  UiTheme.Red,  UiTheme.PopupBtnFont);
            this.Activate();

            TCT_Lib.eCurAppPage     = AppPage.EMV;
            fPayAmount              = _fPayAmount;
            bTowing                 = _bTowing;
            this.Title              = "Payment with QR Code";

            this.HasSeparator       = false;
            this.BorderWidth        = 6;
            this.Resizable          = false;

            btnInquiry.Sensitive    = false;
            btnCancel.Sensitive     = false;

            LogInfo.WriteLogEDC("+------------------ Start QR Payment --------------------");
            TCT_Lib.laneController.manageProcessEMV += LaneController_ManageProcessEMV;

            //this.DeleteEvent += new DeleteEventHandler(OnDlgDelete);

            Gdk.Pixbuf imgEMV   = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.PayQR2.png");
            imgQR.Pixbuf        = imgEMV;

            txtPayAmount.ModifyFont(FontDescription.FromString("Ubuntu Bold 70"));
            txtPayAmount.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
            txtPayAmount.Text   = fPayAmount.ToString("0") + " บาท";

            txtMsg.ModifyFont(FontDescription.FromString("Ubuntu 24"));
            txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(0, 255, 255));
            txtMsg.Text         = "โปรดรอลูกค้าสแกน QR CODE";

#if !__TEST_EDC__
            if (!TCT_Lib.TestConnection(TCT_Lib.szEdcIPAddress, TCT_Lib.iEdcSendPort))
            {
                Console.WriteLine("- Fail Connect Send EDC Socket");
                txtMsg.ModifyFont(FontDescription.FromString("Ubuntu 24"));
                txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                txtMsg.Text         = "ไม่สามารถเชื่อมต่ออุปกรณ์";
                btnCancel.Sensitive = true;
                return;
            }
#endif

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

            if (InitSocketSendEDC())
            {
                InitSocketRecvEDC();
                Process_StartGenerateQrCodeEDC();
                btnInquiry.Sensitive = true;
            }
            else
            {
                Console.WriteLine("- Fail Connect Send EDC Socket");
                txtMsg.ModifyFont(FontDescription.FromString("Ubuntu 24"));
                txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                txtMsg.Text         = "ไม่สามารถเชื่อมต่ออุปกรณ์";
                btnCancel.Sensitive = true;
            }
            btnCancel.Sensitive = true;
        }


        void OnDlgDelete(object sender, DeleteEventArgs a)
        {
            Application.Invoke(delegate
            {
                TCT_Lib.bActiveEMVDlg = false;
                //eventThread.Abort();
                //tSock.Close();
                //netServer.Stop();
                //stream.Close();
                //client.Close();
            });
        }

        private bool InitSocketSendEDC()
        {
#if __TEST_EDC__
            return true;
#endif
            try
            {
                //if (!TCT_Lib.TestConnection(TCT_Lib.szEdcIPAddress, TCT_Lib.iEdcSendPort))
                //    return false;

                client      = new TcpClient();
                var result  = client.BeginConnect(TCT_Lib.szEdcIPAddress, TCT_Lib.iEdcSendPort, null, null);
                result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(500));
                if (client.Connected)
                {
                    stream = client.GetStream();
                    Console.WriteLine("-- Success Init Socket ---");
                    LogInfo.WriteLogEDC("+ Success InitSocketSendEDC");

                    return true;
                }
                else
                {
                    txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                    txtMsg.Text = "Fail Initial Socket";
                    Console.WriteLine("- Error InitSocket : Fail Initial Socket");
                    LogInfo.WriteLogEDC("- Error InitSocketSendEDC : Fail Initial Socket");
                    return false;
                }
            }
            catch (SocketException err)
            {
                txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                txtMsg.Text = err.Message;
                Console.WriteLine("- Error InitSocket : " + err.Message);
                LogInfo.WriteLogEDC("- Error InitSocketSendEDC : " + err.Message);
                return false;
            }
        }

        private void InitSocketRecvEDC()
        {
            eventThread                 = new Thread(new ThreadStart(ThreadEventRecvServer));
            eventThread.Priority        = ThreadPriority.Normal;
            eventThread.IsBackground    = true;
            eventThread.Start();
        }

        private void StartInquiryCommand(bool bDisableCancel = true)
        {
            bStartInquiry           = true;
            btnInquiry.Sensitive    = false;
            if (bDisableCancel)
                btnCancel.Sensitive     = false;
            //iTimerIdWaitInquiry     = GLib.Timeout.Add((iCurCountInquiry <= 1 ? iWaitSecInquiry : iWaitSecInquiry2) * 1000, new GLib.TimeoutHandler(Process_EnableInquiryBtn));
            iTimerIdWaitInquiry     = GLib.Timeout.Add((uint)(TCT_Lib.iEdcQrWaitTimeSec * 1000), new GLib.TimeoutHandler(Process_EnableInquiryBtn));
        }

        private bool Process_EnableInquiryBtn()
        {
            btnInquiry.Sensitive    = true;
            btnCancel.Sensitive     = true;
            iTimerIdWaitInquiry     = 0;
            bStartInquiry           = false;
            GLib.Source.Remove(iTimerIdWaitInquiry);
            return false;
        }

        private void ThreadEventRecvServer()
        {
            byte[] data = new byte[512];

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
            }

            bool bRunning = true;
            while (bRunning)
            {
                if (bCancel)
                    break;

                try
                {
                    if (netServer == null)
                    {
                        LogInfo.WriteLogEDC("+ Start netServer Port : " + TCT_Lib.iEdcRecvPort);
                        netServer = new TcpListener(IPAddress.Any, TCT_Lib.iEdcRecvPort);
                        netServer.Start();
                    }

                    TcpClient clientSocket      = netServer.AcceptTcpClient();
                    clientSocket.ReceiveTimeout = TCT_Lib.iEdcWaitTimeOutSec * 1000;
                    clientSocket.SendTimeout    = TCT_Lib.iEdcWaitTimeOutSec * 1000;
                    sendStream                  = clientSocket.GetStream();
                    if (clientSocket.Connected)
                    {
                        sendStream.Read(data, 0, data.Length);
                        AnalysisEdcMsg(data);

                        sendStream.Flush();
                        data = new byte[512];
                        Thread.Sleep(100);

                        if (!bStartInquiry)
                            StartInquiryCommand(false);
                    }
                    clientSocket.Close();
                    bRunning = false;
                }
                catch (Exception err)
                {
                    Console.WriteLine("Warning!...ThreadEventRecv error : " + err.Message);
                    LogInfo.WriteLogEDC("Warning ThreadEventRecv : " + err.Message);
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
                    /*
                                        netServer = new TcpListener(IPAddress.Any, TCT_Lib.iEdcRecvPort);
                                        netServer.Start();


                                        //clientSocket.Close();
                                        if (sendStream != null)
                                            sendStream.Close();
                                        Thread.Sleep(1000);
                                        if (eventThread != null)
                                            eventThread.Abort();
                                        Thread.Sleep(1000);
                                        if (netServer != null)
                                            netServer.Stop();
                                        Thread.Sleep(1000);
                                        //LogInfo.WriteLogInfo("+ Auto Restart Thread....");
                                        if (eventThread != null)
                                        {
                                            eventThread                 = new Thread(new ThreadStart(ThreadEventRecvServer));
                                            eventThread.Priority        = ThreadPriority.AboveNormal;
                                            eventThread.IsBackground    = true;
                                            eventThread.Start();
                                        }
                    */
                }
                finally
                {
                    // Stop listening for new clients.
                    //if (netServer != null)
                    //    netServer.Stop();

                }
            }

            //netServer.Stop();
        }

        void AnalysisEdcMsg(byte[] byteMsg)
        {
            //Application.Invoke(async delegate {
                try
                {
                    //--- Send Ack
                    Encoding enc    = new ASCIIEncoding();
                    sendStream.Write(enc.GetBytes(ClassLibEDC.GenHexChar(0x06).ToString()), 0, 1);
                    //---------------------------------------------------------------------------
                    string szResponse = Encoding.ASCII.GetString(byteMsg);
                    //Console.WriteLine("+ Result Response : " + szResponse);
                    //LogInfo.WriteLogEDC("+ EDC Result Response (Pay QR) : " + szResponse);

                    string szCharList = ClassLibEDC.GenHexCharList(Encoding.ASCII.GetString(byteMsg));
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
                    if (szRespCode != "00")
                    {
                        string szErrCodeMsg     = ClassLibEDC.GetResponseMsg(szRespCode);
                        string szErrorShowMsg   = ClassLibEDC.ShowResponseMsg(szRespCode);
                        //Console.WriteLine($"Error Code : {szRespCode}, Error Text : "+ szErrCodeMsg);
                        LogInfo.WriteLogEDC($"Error Code : {szRespCode}, Error Text : " + szErrCodeMsg);
                        //txtMsg.Text         = $"{szErrorShowMsg}";
                        Application.Invoke(delegate {
                            txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                            txtMsg.Text = szWarningMsg;
                        });
                        
                        //---------------------------------------------------------------
                        //--- Show warning at TFI
                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, "", fPayAmount.ToString("0"), "", szWarningTFI, "");

                        //--------------------------------------------------------------
                        //if (bStartInquiry)
                        //{
                        //    btnInquiry.Sensitive = false;
                        //    await Task.Delay(5000);
                        //    btnInquiry.Sensitive = true;
                        //}

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
                    xOutCreditCard      = ClassLibEDC.EncodeFormatEDC(szResponse);
                    LogInfo.WriteLogEDC("+ Sucess Encode QR Format.");
                    //txtCardNo.Text      = xCreditCard.szCardNo;
                    //txtAmount.Text      = xCreditCard.szAmount;
                    //txtTerminalID.Text  = xCreditCard.szTerminalID;
                    //txtDateTime.Text    = xCreditCard.szTransactionDate + " - " + xCreditCard.szTransactionTime;
                    //txtRef3.Text        = xCreditCard.szRef3;

                    if (xOutCreditCard.szResponseText.Trim() == "")
                    {
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

                        //----------------
                        int iAlarmID = (int)TCT_Lib.LaneDB.WriteLaneAlarmRTO(AlarmDevice.EDC_WARNING, AlarmType.Occurred, $"QR payment not found InvoiceNo", $"");
                        TCT_Lib.SendLaneAlarmObjToRTO(iAlarmID);

                        return;
                    }

                    if ((xOutCreditCard.szCardNo ?? "") != "")
                    {
                        LogInfo.WriteLogEDC($"- Warning...Found Card No={xOutCreditCard.szCardNo.Trim()}, then this is EMV, (Not QR)");
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
                            TCT_Lib.xCurQR.respRef1 = szEdcRef1;
                        if (TCT_Lib.xCurQR.respRef2 == "")
                            TCT_Lib.xCurQR.respRef2 = szEdcRef2;

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
                            TCT_Lib.xCurEDC.ref1 = szEdcRef1;
                        if (TCT_Lib.xCurEDC.ref2 == "")
                            TCT_Lib.xCurEDC.ref2 = szEdcRef2;

                        //--------------------------------------
                        LogInfo.WriteLogEDC("+ ResponseMessage = " + TCT_Lib.xCurQR.responseMessage);
                        LogInfo.WriteLogEDC("+ ApproveCode = " + TCT_Lib.xCurQR.respApproveCode);
                        LogInfo.WriteLogEDC("+ CardNo = " + TCT_Lib.xCurQR.respCardNo);
                        LogInfo.WriteLogEDC("+ TerminalId = " + TCT_Lib.xCurQR.respTerminalId);
                        LogInfo.WriteLogEDC("+ TransDate = " + TCT_Lib.xCurQR.respDate);
                        LogInfo.WriteLogEDC("+ TransTime = " + TCT_Lib.xCurQR.respTime);
                        LogInfo.WriteLogEDC("+ TransAmount = " + TCT_Lib.xCurQR.respAmount);
                        LogInfo.WriteLogEDC("+ TransRef = " + TCT_Lib.xCurQR.respRef);
                        LogInfo.WriteLogEDC("+ InvoiceNo = " + TCT_Lib.xCurQR.respInvoiceNo);
                        LogInfo.WriteLogEDC("+ Ref1 = " + TCT_Lib.xCurQR.respRef1);
                        LogInfo.WriteLogEDC("+ Ref2 = " + TCT_Lib.xCurQR.respRef2);
                        LogInfo.WriteLogEDC("+ Ref3 = " + TCT_Lib.xCurQR.respRef3);
                        LogInfo.WriteLogEDC($"QR : JobNo={TCT_Lib.iJobNo}, nTrx={TCT_Lib.nTrx}, CardNo={TCT_Lib.xCurQR.respCardNo}, ApproveCode={TCT_Lib.xCurQR.respApproveCode.Trim()}, Ref2={TCT_Lib.xCurQR.respRef2}, Ref3={TCT_Lib.xCurQR.respRef3}, InvoiceNo={TCT_Lib.xCurQR.respInvoiceNo}");
                        LogInfo.WriteLaneEvent($"QR Success : CardNo={TCT_Lib.xCurQR.respCardNo}, Amount={TCT_Lib.xCurQR.respAmount}, ApproveCode={TCT_Lib.xCurQR.respApproveCode}. TransRef={TCT_Lib.xCurQR.respRef}, InvoiceNo={TCT_Lib.xCurQR.respInvoiceNo}");

                        if (TCT_Lib.xCurQR.responseMessage.Trim() == "")
                        {
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
                            TCT_Lib.xTowingQR.respRef1 = szEdcRef1;
                        if (TCT_Lib.xTowingQR.respRef2 == "")
                            TCT_Lib.xTowingQR.respRef2 = szEdcRef2;

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
                            TCT_Lib.xTowingEDC.ref1 = szEdcRef1;
                        if (TCT_Lib.xTowingEDC.ref2 == "")
                            TCT_Lib.xTowingEDC.ref2 = szEdcRef2;

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
                            Application.Invoke(delegate {
                                txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(0, 255, 0));
                                txtMsg.Text = szConfirmInquiry;
                            });

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
                    this.Respond(ResponseType.Ok);

                    //Console.WriteLine("Start 1");
                    //-------------------------------------------------
                    try
                    {
                        //if (eventThread != null)
                        //    eventThread.Abort();
                        if (netServer != null)
                        {
                            netServer.Stop();
                            netServer = null;
                        }

                        //Console.WriteLine("Start 2");
                        if (sendStream != null)
                        {
                            sendStream.Flush();
                            sendStream.Close();
                            sendStream.Dispose();
                            sendStream = null;
                        }

                        //Console.WriteLine("Start 3");
                        if (stream != null)
                        {
                            stream.Flush();
                            stream.Close();
                            stream.Dispose();
                            stream = null;
                        }

                        //Console.WriteLine("Start 4");
                        if (client != null)
                            client.Close();


                        if (eventThread != null)
                            eventThread.Abort();
                    }
                    catch(Exception)
                    {

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
                        Dispose();
                        Destroy();

                        LogInfo.WriteLogEDC("-------------------- COMPLETE EDC QR PAYMENT -----------------");
                        //Console.WriteLine("Start 6");
                        //return;
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine("- Error AnalysisEdcMsg QR Payment : " + err.Message);
                    LogInfo.WriteLogEDC("- Error AnalysisEdcMsg QR Payment : " + err.Message);
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

                byte[] byteMsg  = new byte[300];
                string szErrMsg = "", szReqMsg  = "";
                szEdcRef1 = szEdcRef2 = "";
                bool bResult        = ClassLibEDC.GenerateQrCodeEdcMessage(fAmount, ref byteMsg, ref szReqMsg, ref szEdcRef1, ref szEdcRef2, ref szErrMsg);
                TCT_Lib.szQrRef1    = szEdcRef1;
                TCT_Lib.szQrRef2    = szEdcRef2;

                if (!bTowing)
                    TCT_Lib.xCurQR.requestText = "";
                else
                    TCT_Lib.xTowingQR.requestText = "";

                stream.Write(byteMsg, 0, byteMsg.Length);

                //--- Wait Ack
                int iAck        = 0;
                byte[] bAck     = new byte[100];
                int iDataLen    = stream.Read(bAck, 0, 1);
                iAck            = bAck[0];
                LogInfo.WriteLogEDC("+ Ack Data : " + iAck);
                //-----------------------------------------
                string szCharList = GenHexCharList(bAck);
                LogInfo.WriteLogEDC("+ EDC Ack Generate QR Code : " + szCharList);
                //-----------------------------------------
                if (iAck != 0x06) // Ack
                {
                    if (iAck == 0x15)
                    {
                        Application.Invoke(delegate {
                            txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                            txtMsg.Text = @"< Error!...CRC Invalid >";
                        });

                        Console.WriteLine("- Error...CRC Invalid. ---");
                        LogInfo.WriteLogEDC("- Error Generate QR Code - CRC Invalid");
                    }
                    else if (iAck == 0x04)
                    {
                        LogInfo.WriteLogEDC("+ Ack Generate QR Code - EDC busy");
                        //Process_EnableInquiryBtn();
                    }
                    else
                    {
                        //txtMsg.Text = @"< Error!...Ack from EDC >";
                        Console.WriteLine("- Error...Ack from EDC. ---");
                        LogInfo.WriteLogEDC("- Error Generate QR Code - Err Ack from EDC");
                    }
                    stream.Flush();
                    return;
                }
                else
                {
                    LogInfo.WriteLogEDC("+ Ack from Request Gen QR");
                }
                Application.Invoke(delegate {
                    txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                    txtMsg.Text = szConfirmInquiry;
                });

                bStartCmd   = true;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error Generate QR Code Payment : " + err.Message);
                LogInfo.WriteLogEDC("- Error Generate QR Code Payment : " + err.Message);
                Application.Invoke(delegate {
                    txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                    txtMsg.Text = err.Message;
                });
            }
        }

        protected void OnBtnInquiryClicked(object sender, EventArgs e)
        {
            iCurCountInquiry++;
            /*
                        iCurCountInquiry++;
                        if (iCurCountInquiry > iTotalMaxInquiry)
                        {
                            Process_CancelEMV();
                            return;
                        }
            */

#if !__TEST_EDC__
            if (!TCT_Lib.TestConnection(TCT_Lib.szEdcIPAddress, TCT_Lib.iEdcSendPort))

            {
                bRetryRecv  = true;
                Console.WriteLine("- Fail Connect Send EDC Socket");
                Application.Invoke(delegate {
                    txtMsg.ModifyFont(FontDescription.FromString("Ubuntu 24"));
                    txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                    txtMsg.Text = "ไม่สามารถเชื่อมต่ออุปกรณ์";
                });

                bStartInquiry           = false;
                btnInquiry.Sensitive    = false;
                btnCancel.Sensitive     = true;

                return;
            }
#endif

            if (!bStartInquiry)
                StartInquiryCommand();
            Process_InquiryQrCodeEDC();
        }

        private bool Process_InquiryQrCodeEDC(bool bRepeat = false)
        {
#if __TEST_EDC__
            return false;
#endif

            Application.Invoke(delegate
            {
                try
                {
                    LogInfo.WriteLaneEvent($"QR Code - Inquiry QR Code");
                    LogInfo.WriteLogEDC("+-- Start Inquiry QR Code ---");

                    byte[] byteMsg  = new byte[300];
                    string szErrMsg = "", szReqMsg = "";
                    bool bResult    = ClassLibEDC.InquiryQrCodeEdcMessage(ref byteMsg, ref szReqMsg, ref szErrMsg);
                    if (!bTowing)
                        TCT_Lib.xCurQR.requestText     = "";
                    else
                        TCT_Lib.xTowingQR.requestText  = "";

                    stream.Write(byteMsg, 0, byteMsg.Length);

                    //--- Wait Ack
                    int iAck        = 0;
                    byte[] bAck     = new byte[100];
                    int iDataLen    = stream.Read(bAck, 0, 1);
                    iAck            = bAck[0];
                    LogInfo.WriteLogEDC("+ Ack Data : " + iAck);
                    //-----------------------------------------
                    string szCharList = GenHexCharList(bAck);
                    LogInfo.WriteLogEDC("+ EDC Ack Inquiry : " + szCharList);
                    //-----------------------------------------
                    if (iAck != 0x06) // Ack
                    {
                        if (iAck == 0x15)
                        {
                            Application.Invoke(delegate {
                                txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                                txtMsg.Text = @"< Error!...CRC Invalid >";
                            });

                            //Console.WriteLine("- Error...CRC Invalid. ---");
                            LogInfo.WriteLogEDC("- Error Inquiry QR Code - CRC Invalid");
                        }
                        else if (iAck == 0x04)
                        {
                            LogInfo.WriteLogEDC("+ Ack Inquiry QR Code - EDC busy");
                            //Process_EnableInquiryBtn();
                        }
                        else
                        {
                            //txtMsg.Text = @"< Error!...Ack from EDC >";
                            //Console.WriteLine("- Error...Ack from EDC. ---");
                            LogInfo.WriteLogEDC("- Error Inquiry QR Code - Err Ack from EDC");
                            //sendStream.Flush();
/*
                            if (!bRepeat)
                            {
                                Thread.Sleep(3000);
                                LogInfo.WriteLogEDC("+ Auto re-send Inquiry");
                                Process_InquiryQrCodeEDC(true);
                            }
                            else
                            {
                                Process_EnableInquiryBtn();
                            }
*/                            
                        }
                    }
                    else
                    {
                        LogInfo.WriteLogEDC("+ Recv Ack for Inquiry QR Code.");
                    }
                    stream.Flush();
                }
                catch (Exception err)
                {
                    stream.Flush();
                    //Console.WriteLine("- Error Inquiry QR Code Payment : " + err.Message);
                    LogInfo.WriteLogEDC("- Error Inquiry QR Code Payment : " + err.Message);
                    LogInfo.WriteLaneEvent($"QR Code - Error Inquiry = {err.Message}");
                    Application.Invoke(delegate {
                        txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                        string szDefErr = "ชำระด้วย QR Code ไม่สำเร็จ\nกรุณาติดต่อธนาคารผู้ออกบัตร";
                        txtMsg.Text = szDefErr;
                    });

                    if (InitSocketSendEDC())
                    {
                        InitSocketRecvEDC();
                    }
                    else
                    {
                        Console.WriteLine("- Fail Connect Send EDC Socket");
                        Application.Invoke(delegate {
                            txtMsg.ModifyFont(FontDescription.FromString("Ubuntu 24"));
                            txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                            txtMsg.Text = "ไม่สามารถเชื่อมต่ออุปกรณ์";
                        });

                        btnCancel.Sensitive = true;
                    }
                }
            });
            return true;
        }

        private void Process_CancelPayEDC(bool bRepeat = false)
        {
            try
            {
                LogInfo.WriteLaneEvent($"QR Code - Cancel EDC");
                LogInfo.WriteLogEDC("+-- Start Cancel QR Code Payment ---");

                byte[] byteMsg  = new byte[300];
                string szErrMsg = "", szCmdMsg = "";
                bool bResult    = ClassLibEDC.GenerateCancelEdcMessage(ref byteMsg, ref szCmdMsg, ref szErrMsg);

                stream.Write(byteMsg, 0, byteMsg.Length);
/*
                //--- Wait Ack
                int iAck        = 0;
                byte[] bAck     = new byte[100];
                int iDataLen    = stream.Read(bAck, 0, 1);
                iAck = bAck[0];
                LogInfo.WriteLogEDC("+ Ack Data : " + iAck);
                //-----------------------------------------
                string szCharList = GenHexCharList(bAck);
                LogInfo.WriteLogEDC("+ EDC Ack Cancel : " + szCharList);
                //-----------------------------------------
                if (iAck != 0x06) // Ack
                {
                    txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                    if (iAck == 0x15)
                    {
                        //txtMsg.Text = @"< Error!...CRC Invalid >";
                        Console.WriteLine("- Error...CRC Invalid. ---");
                        LogInfo.WriteLogEDC("- Error Cancel Payment : CRC Invalid");
                    }
                    else if (iAck == 0x04)
                    {
                        LogInfo.WriteLogEDC("+ Ack Cancel QR Code - EDC busy");
                    }
                    else
                    {
                        //txtMsg.Text = @"< Error!...Ack from EDC >";
                        Console.WriteLine("- Error...Ack from EDC. ---");
                        LogInfo.WriteLogEDC("- Error Cancel Payment : Err Ack from EDC");
                        //sendStream.Flush();
---
                        if (!bRepeat)
                        {
                            Thread.Sleep(3000);
                            LogInfo.WriteLogEDC("+ Auto re-send Cancel");
                            Process_CancelPayEDC(true);
                        }
---
                    }
                    stream.Flush();
                    return;
                }
*/
                stream.Flush();
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error Send Cancel QR Code : " + err.Message);
                LogInfo.WriteLogEDC("- Error Cancel QR Code : " + err.Message);
            }
        }

        protected void OnBtnCancelClicked(object sender, EventArgs e)
        {
            Process_CancelEMV();
        }

        protected void Process_CancelEMV()
        {
            if (!bRetryRecv)
            {
                if (bStartCmd)
                    Process_CancelPayEDC();
            }

            //if (iTimerIdInquiry > 0)
            //    GLib.Source.Remove(iTimerIdInquiry);


            bCancel = true;

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

            try
            {
                if (netServer != null)
                {
                    netServer.Stop();
                    netServer = null;
                }

                if (sendStream != null)
                {
                    sendStream.Flush();
                    sendStream.Close();
                    sendStream.Dispose();
                    sendStream = null;
                }

                if (stream != null)
                {
                    stream.Flush();
                    stream.Close();
                    stream.Dispose();
                    stream = null;
                }

                if (client != null)
                {
                    client.Close();
                    client.Dispose();
                    client = null;
                }


                if (eventThread != null)
                {
                    eventThread.Abort();
                    eventThread = null;
                }
            }
            catch (Exception)
            {

            }
            finally
            {
                TCT_Lib.bActiveEMVDlg = false;
                this.Respond(ResponseType.Cancel);
                TCT_Lib.laneController.manageProcessEMV -= LaneController_ManageProcessEMV;
                Dispose();
                Destroy();
            }
        }

        protected void OnKeyPressEvent(object o, KeyPressEventArgs args)
        {
            if (TCT_Lib.keyMapping.ContainsKey((int)args.Event.KeyValue))
                TCT_Lib.webSocketLane.SendVirtualKeyboard(TCT_Lib.keyMapping[(int)args.Event.KeyValue]);
        }

        void LaneController_ManageProcessEMV(int iKey)
        {
            Application.Invoke(delegate
            {
                if (iKey == (int)KeyPressedType.EDIT)
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
