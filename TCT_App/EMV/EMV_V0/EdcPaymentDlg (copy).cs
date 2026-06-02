//#define __TEST_EDC__

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Gtk;
using Pango;

namespace TCT_App.EMV.V0
{
    public partial class EdcPaymentDlg : Gtk.Dialog
    {
        decimal fPayAmount;
        bool bTowing;
        bool bCancel = false;

        TcpClient client;
        NetworkStream stream;

        Thread eventThread;
        private TcpListener netServer;
        NetworkStream sendStream    = null;
        bool bStartCmd              = false;
        string szWarningMsg         = "กรุณาติดต่อธนาคารผู้ออกบัตร";
        string szWarningTFI         = "ติดต่อธนาคาร";

        bool bEdcReponsed   = false;
        int iCurRetry       = 0;
        string szRef1 = "", szRef2 = "";
        public objCreditCardInfo xOutCreditCard;
        //----------------------------------------------------------------
        public EdcPaymentDlg(decimal _fPayAmount, bool _bTowing = false)
        {
            TCT_Lib.bActiveEMVDlg = true;
            this.Build();
            this.Activate();
            bCancel             = false;

            TCT_Lib.eCurAppPage = AppPage.EMV;
            fPayAmount          = _fPayAmount;
            bTowing             = _bTowing;
            this.Title          = "Payment with EDC";

            this.HasSeparator   = false;
            this.BorderWidth    = 6;
            this.Resizable      = false;

            //--- For Test EDC Message
            //Process_StartPaymentEDC();

            //TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, "", fPayAmount.ToString("0"), "", szWarningTFI, "");       // Test

            LogInfo.WriteLogEDC("+------------------ Start CreditCard Payment --------------------");
            TCT_Lib.laneController.manageProcessEMV += LaneController_ManageProcessEMV;

            //this.DeleteEvent += new DeleteEventHandler(OnDlgDelete);

            Gdk.Pixbuf imgEMV   = Gdk.Pixbuf.LoadFromResource("TCT_App.Images.Emv.png");
            imgEmv.Pixbuf       = imgEMV;

            txtPayAmount.ModifyFont(FontDescription.FromString("Ubuntu Bold 70"));
            txtPayAmount.ModifyFg(StateType.Normal, new Gdk.Color(255, 255, 255));
            txtPayAmount.Text   = fPayAmount.ToString("0") + " บาท";

            txtMsg.ModifyFont(FontDescription.FromString("Ubuntu 24"));
            txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(0, 255, 255));
            txtMsg.Text         = "โปรดรอลูกค้าแตะบัตร";

/*
            if (!TCT_Lib.TestConnection(TCT_Lib.szEdcIPAddress, TCT_Lib.iEdcSendPort))
            {
                Console.WriteLine("- Fail Connect Send EDC Socket");
                txtMsg.ModifyFont(FontDescription.FromString("Ubuntu 24"));
                txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                txtMsg.Text     = "ไม่สามารถเชื่อมต่ออุปกรณ์";
                return;
            }
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

            if (InitSocketSendEDC())
            {
                InitSocketRecvEDC();
                Process_StartPaymentEDC();
            }
            else
            {
                Console.WriteLine("- Fail Connect Send EDC Socket");
                txtMsg.ModifyFont(FontDescription.FromString("Ubuntu 24"));
                txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                txtMsg.Text = "ไม่สามารถเชื่อมต่ออุปกรณ์";
            }
        }


        void OnDlgDelete(object sender, DeleteEventArgs a)
        {
            Application.Invoke(delegate
            {
                TCT_Lib.bActiveEMVDlg = false;
                TCT_Lib.laneController.manageProcessEMV -= LaneController_ManageProcessEMV;
                //eventThread.Abort();
                //tSock.Close();
                //netServer.Stop();
                //stream.Close();
                //client.Close();
            });
        }


        private bool InitSocketSendEDC()
        {
            try
            {
                //if (!TCT_Lib.TestConnection(TCT_Lib.szEdcIPAddress, TCT_Lib.iEdcSendPort))
                //    return false;

                client = new TcpClient(TCT_Lib.szEdcIPAddress, TCT_Lib.iEdcSendPort);
                stream = client.GetStream();
                //Console.WriteLine("-- Success Init Socket ---");
                LogInfo.WriteLogEDC("+ Success InitSocketSendEDC");

                return true;
            }
            catch (SocketException err)
            {
                txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                txtMsg.Text = err.Message;
                //Console.WriteLine("- Error InitSocket : " + err.Message);
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

            while (true)
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
                    clientSocket.ReceiveTimeout = 5 * 1000;
                    clientSocket.SendTimeout    = 5 * 1000;
                    sendStream                  = clientSocket.GetStream();
                    sendStream.WriteTimeout     = TCT_Lib.iEdcWaitTimeOutSec * 1000;
                    sendStream.ReadTimeout      = TCT_Lib.iEdcWaitTimeOutSec * 1000;
                    if (clientSocket.Connected)
                    {
                        sendStream.Read(data, 0, data.Length);
                        AnalysisEdcMsg(data);

                        sendStream.Flush();
                        data = new byte[512];
                        Thread.Sleep(50);
                    }
                    clientSocket.Close();
                }
                catch (SocketException err)
                {
                    //LogInfo.WriteLogEDC("- Error Recv SocketException : " + err.Message);
                    if (err.Message == "Address already in use")
                    {
                        Thread.Sleep(2 * 1000);
                        bCancel = true;
                    }

                    if (netServer != null)
                    {
                        //netServer.Stop();
                        netServer = null;
                    }
                    //clientSocket.Close();
                    if (sendStream != null)
                    {
                        //sendStream.Close();
                        //sendStream.Dispose();
                        sendStream = null;
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine("Warning!...ThreadEventRecvServer error : " + err.Message);
                    if (netServer != null)
                    {
                        //netServer.Stop();
                        netServer = null;
                    }
                    //clientSocket.Close();
                    if (sendStream != null)
                    {
                        //sendStream.Close();
                        //sendStream.Dispose();
                        sendStream = null;
                    }
                    Thread.Sleep(1000);

                    /*
                                        Thread.Sleep(1000);
                                        try
                                        {
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
                                            Thread.Sleep(1000);
                                            if (eventThread != null)
                                            {
                                                eventThread                 = new Thread(new ThreadStart(ThreadEventRecvServer));
                                                eventThread.Priority        = ThreadPriority.AboveNormal;
                                                eventThread.IsBackground    = true;
                                                eventThread.Start();
                                            }
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

        private void AnalysisEdcMsg(byte[] byteMsg)
        {
            //Application.Invoke(delegate {
                try
                {
                    //--- Send Ack
                    Encoding enc        = new ASCIIEncoding();
                    try
                    {
                        LogInfo.WriteLogEDC("+ Send Ack 06 to EDC");
                        sendStream.Write(enc.GetBytes(ClassLibEDC.GenHexChar(0x06).ToString()), 0, 1);
                    }
                    catch (Exception err)
                    {
                        LogInfo.WriteLogEDC("- Error sendStream EMV Ack Payment : " + err.Message);
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
                        {
                            client.Close();
                            client.Dispose();
                            client = null;
                        }

                    }
                    //---------------------------------------------------------------------------

                    string szResponse   = Encoding.ASCII.GetString(byteMsg);
                    //Console.WriteLine("+ Result Response : " + szResponse);
                    LogInfo.WriteLogEDC("+ EDC Result Response (Pay EMV) : " + szResponse);

                    if (TCT_Lib.xCurEMV == null)
                    {
                        LogInfo.WriteLogEDC("- EDC Response after Cancel.");
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
                        {
                            client.Close();
                            client.Dispose();
                            client = null;
                        }
/*
                        if (eventThread != null)
                        {
                            eventThread.Abort();
                            eventThread = null;
                        }
*/                        
                        return;
                    }
                    
                    //TCT_Lib.xCurEMV.responseText = szResponse;
                    if (!bTowing)
                        TCT_Lib.xCurEMV.responseText    = "";
                    else
                        TCT_Lib.xTowingEMV.responseText = "";
                    string szCharList   = ClassLibEDC.GenHexCharList(Encoding.ASCII.GetString(byteMsg));
                    Console.WriteLine("Byte Data : " + szCharList);
                    LogInfo.WriteLogEDC("+ EDC Response Byte Data (Pay EMV) : " + szCharList);

                    bEdcReponsed        = true;
                    //txtResponseData.Text = szCharList;
                    string szRespCode   = szResponse.Substring(17, 2);
                    Console.WriteLine("+ Response Code : " + szRespCode);
                    Console.WriteLine("-");
                    LogInfo.WriteLogEDC("+ EDC Response Code (Pay EMV) : " + szRespCode);
                    if (szRespCode != "00")
                    {
                        string szErrCodeMsg     = ClassLibEDC.GetResponseMsg(szRespCode);
                        string szErrorShowMsg   = ClassLibEDC.ShowResponseMsg(szRespCode);
                        //Console.WriteLine($"Error Code : {szRespCode}, Error Text : szErrCodeMsg");
                        LogInfo.WriteLogEDC($"Error Code : {szRespCode}, Error Text : " + szErrCodeMsg);
                        //txtMsg.Text = $"{szErrorShowMsg}";
                        Application.Invoke(delegate {
                            txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                            txtMsg.Text = szWarningMsg;
                        });

                        //---------------------------------------------------------------
                        //--- Show warning at TFI
                        TCT_Lib.webSocketLane.OutIo_WriteTFI((int)TFI_State.Class_Fare, (int)TFI_Pattern.Normal, "", fPayAmount.ToString("0"), "", szWarningTFI, "");
                        //--------------------------------------------------------------

                        Process_ClosePaymentDlg();
                        return;
                    }
                    else
                    {
                        //--- Check TransCode
                        string szTransCode = szResponse.Substring(15, 2);
                        if (szTransCode != "20")
                        {
                            LogInfo.WriteLogEDC($"Error TransCode : {szTransCode}, This trans code is not emv result.");
                            return;
                        }

                        xOutCreditCard              = ClassLibEDC.EncodeFormatEDC(szResponse);
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
                        LogInfo.WriteLogEDC("+ Towing ResponseMessage = " + TCT_Lib.xTowingEDC.responseMessage);
                        LogInfo.WriteLogEDC("+ Towing ApproveCode = " + TCT_Lib.xTowingEDC.approveCode);
                        LogInfo.WriteLogEDC("+ Towing CardNo = " + TCT_Lib.xTowingEDC.cardNo);
                        LogInfo.WriteLogEDC("+ Towing TerminalId = " + TCT_Lib.xTowingEDC.terminalId);
                        LogInfo.WriteLogEDC("+ Towing TransDate = " + TCT_Lib.xTowingEDC.transDate);
                        LogInfo.WriteLogEDC("+ Towing TransTime = " + TCT_Lib.xTowingEDC.transTime);
                        LogInfo.WriteLogEDC("+ Towing TransAmount = " + TCT_Lib.xTowingEDC.transAmount);
                        LogInfo.WriteLogEDC("+ Towing TransRef = " + TCT_Lib.xTowingEDC.transRef);
                        LogInfo.WriteLogEDC("+ Towing TransTime = " + TCT_Lib.xTowingEDC.transTime);
                        LogInfo.WriteLogEDC("+ Towing Ref1 = " + TCT_Lib.xTowingEDC.ref1);
                        LogInfo.WriteLogEDC("+ Towing Ref2 = " + TCT_Lib.xTowingEDC.ref2);
                        LogInfo.WriteLogEDC("+ Towing Ref3 = " + TCT_Lib.xTowingEDC.ref3);

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

                    this.Respond(ResponseType.Ok);

                    //Console.WriteLine("Start 1");
                    TCT_Lib.bActiveEMVDlg = false;
                    TCT_Lib.laneController.manageProcessEMV -= LaneController_ManageProcessEMV;

                    Thread.Sleep(500);
                    LogInfo.WriteLogEDC("+ Recv Socket Close / Disconnect when success payment");
                    recvSocket.Close();
                    recvSocket.Dispose();
                    recvSocket = null;

                    Dispose();
                    Destroy();
                    return;
                    //-------------------------------------------------
                    try
                    {
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
                        {
                            client.Close();
                            client.Dispose();
                            client = null;
                        }
/*
                        if (eventThread != null)
                        {
                            eventThread.Abort();
                            eventThread = null;
                        }
*/                        
                    }
                    finally
                    {
                        TCT_Lib.bActiveEMVDlg = false;
                        TCT_Lib.laneController.manageProcessEMV -= LaneController_ManageProcessEMV;
                        //Console.WriteLine("Start 5");
                        Dispose();
                        Destroy();

                        //Console.WriteLine("Start 6");
                        //return;
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine("- Error AnalysisEdcMsg EMV Payment : " + err.Message);
                    LogInfo.WriteLogEDC("- Error AnalysisEdcMsg EMV Payment : " + err.Message);

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
                    {
                        client.Close();
                        client.Dispose();
                        client = null;
                    }
/*
                    if (eventThread != null)
                    {
                        eventThread.Abort();
                        eventThread = null;
                    }
*/
                    TCT_Lib.bActiveEMVDlg = false;
                    TCT_Lib.laneController.manageProcessEMV -= LaneController_ManageProcessEMV;
                    //Console.WriteLine("Start 5");
                    Dispose();
                    Destroy();
                }
            //});
        }

        private void Process_StartPaymentEDC()
        {
            try
            {
                LogInfo.WriteLaneEvent($"EMV- Generate Payment");
                LogInfo.WriteLogEDC("+-- Start EMV Payment ---");
                decimal fAmount = fPayAmount;

                szRef1 = szRef2 = "";
                byte[] byteMsg  = new byte[300];
                string szErrMsg = "", szPayMsg = "";
                bool bResult    = ClassLibEDC.GeneratePaymentEdcMessage(fAmount, ref byteMsg, ref szPayMsg, ref szRef1, ref szRef2, ref szErrMsg);
                //TCT_Lib.xCurEMV.requestText = Encoding.UTF8.GetString(byteMsg);
                LogInfo.WriteLogEDC($"+ Generate EDC Payment : Ref1={szRef1}, Ref2={szRef2}");

                if (!bTowing)
                    TCT_Lib.xCurEMV.requestText     = "";
                else
                    TCT_Lib.xTowingEMV.requestText  = "";

                stream.Write(byteMsg, 0, byteMsg.Length);

                //--- Wait Ack
                int iAck        = 0;
                byte[] bAck     = new byte[100];
                int iDataLen    = stream.Read(bAck, 0, 1);
                iAck            = bAck[0];
                LogInfo.WriteLogEDC("+ Ack Data : " + iAck);
                if (iAck != 0x06) // Ack
                {
                    Application.Invoke(delegate {
                        txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                        if (iAck == 0x15)
                        {
                            txtMsg.Text = @"< Error!...CRC Invalid >";
                            Console.WriteLine("- Error...CRC Invalid. ---");
                            //LogInfo.WriteLogEDC("- Error Generate CreditCard Payment - CRC Invalid");
                            txtMsg.Text = "CRC Invalid";
                        }
                        else if (iAck == 0x04)
                        {
                            //LogInfo.WriteLogEDC("+ Ack Generate CreditCard Payment - EDC busy");
                            txtMsg.Text = "อุปกรณ์ไม่พร้อมทำงาน";
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
                    if (iAck == 0x15)
                        LogInfo.WriteLogEDC("- Error Generate CreditCard Payment - CRC Invalid");
                    else if (iAck == 0x04)
                        LogInfo.WriteLogEDC("+ Ack Generate CreditCard Payment - EDC busy");
                    else
                        LogInfo.WriteLogEDC("- Error CreditCard Payment - Err Ack from EDC");
                    //-----------------------------------------
                    string szCharList = GenHexCharList(bAck);
                    LogInfo.WriteLogEDC("+ EDC Ack Payment : " + szCharList);
                    //-----------------------------------------
                    stream.Flush();
                    return;
                }
                bStartCmd = true;
                stream.Flush();
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error Send Credit Payment : " + err.Message);
                LogInfo.WriteLogEDC("- Error Send Credit Payment : " + err.Message);
                Application.Invoke(delegate {
                    txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                    txtMsg.Text = err.Message;
                });
            }
        }

        private bool Process_CancelPayEDC(bool bRepeat = false)
        {
            try
            {
                LogInfo.WriteLaneEvent($"EMV - Process Cancel Pay EDC");
                LogInfo.WriteLogEDC("+-- Start Cancel Payment ---");
                byte[] byteMsg      = new byte[300];
                string szErrMsg     = "", szCmdMsg = "";
                bool bResult        = ClassLibEDC.GenerateCancelEdcMessage(ref byteMsg, ref szCmdMsg, ref szErrMsg);

                if (stream == null)
                {
                    LogInfo.WriteLogEDC("- Not Found stream send data, then initial stream send data ---");
                    InitSocketSendEDC();
                    LogInfo.WriteLogEDC("# Initial stream send data success.");
                }
                //-------------------------------------------------
                LogInfo.WriteLogEDC("+-- Start Send Cancel Command ---");
                stream.ReadTimeout  = TCT_Lib.iEdcWaitTimeOutSec * 1000;
                stream.WriteTimeout = TCT_Lib.iEdcWaitTimeOutSec * 1000;
                stream.Write(byteMsg, 0, byteMsg.Length);
                LogInfo.WriteLogEDC("+-- End Send Cacel Command ---");

                Application.Invoke(delegate {
                    txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(0, 255, 0));
                    txtMsg.Text = "โปรดรอการยกเลิก";
                });

                //--------------------------------------------------
                //stream.Flush();
                //return true;
                //--------------------------------------------------
                LogInfo.WriteLogEDC("+-- Start Wait Ack ---");
                //--- Wait Ack
                int iAck        = 0;
                byte[] bAck     = new byte[100];
                int iDataLen    = stream.Read(bAck, 0, 1);
                iAck            = bAck[0];
                LogInfo.WriteLogEDC("+ Ack Data : " + iAck);
                //-----------------------------------------
                string szCharList = GenHexCharList(bAck);
                LogInfo.WriteLogEDC("+ EDC Ack Cancel : " + szCharList);
                //-----------------------------------------
                if (iAck != 0x06) // Ack
                {
                    Application.Invoke(delegate {
                        txtMsg.ModifyFg(StateType.Normal, new Gdk.Color(255, 0, 0));
                        if (iAck == 0x15)
                        {
                            txtMsg.Text = @"< Error!...CRC Invalid >";
                            Console.WriteLine("- Error...CRC Invalid. ---");
                            //LogInfo.WriteLogEDC("- Error Cancel Payment - CRC Invalid");
                            txtMsg.Text = "CRC Invalid";
                        }
                        else if (iAck == 0x04)
                        {
                            stream.ReadTimeout  = -1;
                            stream.WriteTimeout = -1;
                            //LogInfo.WriteLogEDC("+ Ack 04 - EDC busy, Please wait payment");
                            txtMsg.Text = "อุปกรณ์ไม่พร้อมยกเลิก. โปรดรอหรือกดใหม่";
                            //return false;
                        }
                        else
                        {
                            //txtMsg.Text = @"< Error!...Ack from EDC >";
                            Console.WriteLine("- Error...Ack from EDC. ---");
                            //LogInfo.WriteLogEDC("- Error CreditCard Payment - Ack from EDC. ---");
                            string szCharList2 = GenHexCharList(bAck);
                            txtMsg.Text = "Unknow Ack : " + szCharList2;
                            /*
                                                    sendStream.Flush();
                                                    if (!bRepeat)
                                                    {
                                                        Thread.Sleep(3000);
                                                        LogInfo.WriteLogEDC("+ Auto re-send Cancel");
                                                        Process_CancelPayEDC(true);
                                                    }
                            */
                        }
                    });

                    stream.Flush();

                    if (iAck == 0x15)
                    {
                        LogInfo.WriteLogEDC("- Error Cancel Payment - CRC Invalid");
                    }
                    else if (iAck == 0x04)
                    {
                        LogInfo.WriteLogEDC("+ Ack 04 - EDC busy, Please wait payment");
                        return false;
                    }
                    else
                    {
                        LogInfo.WriteLogEDC("- Error CreditCard Payment - Ack from EDC. ---");
                    }

                    return true;
                }

                stream.Flush();
                return true;
            }
            catch (System.IO.IOException err)
            {
                Console.WriteLine("- Error TimeOut IOException Send Cancel Payment (NoAck) : " + err.Message);
                LogInfo.WriteLogEDC("- Error TimeOut IOException Send Cancel Payment (NoAck) : " + err.Message);

                stream.ReadTimeout      = -1;
                stream.WriteTimeout     = -1;
                iCurRetry++;

                if (iCurRetry >= TCT_Lib.iEdcRetryTimeOut)
                {
                    LogInfo.WriteLogEDC("- End Retry Send Cancel CreditCard Payment");
                    return true;
                }
                else
                {
                    LogInfo.WriteLogEDC("- Fail Cancel CreditCard Payment : Time=" + iCurRetry);
                    return false;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error Send Cancel CreditCard Payment : " + err.Message);
                LogInfo.WriteLogEDC("- Error Send Cancel CreditCard Payment : " + err.Message);

                if (stream != null)
                    stream.Flush();

                return true;
            }
        }

        protected void OnBtnCancelClicked(object sender, EventArgs e)
        {
            Process_CancelEMV();
        }

        protected void Process_CancelEMV()
        {
            if (bStartCmd)
            {
                if (!Process_CancelPayEDC())
                    return;
            }

            Process_ClosePaymentDlg();
        }

        protected void Process_ClosePaymentDlg()
        {
            LogInfo.WriteLogEDC("+ Process Close EDC Payment Dialog");

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
                    sendStream  = null;
                }

                if (stream != null)
                {
                    stream.Flush();
                    stream.Close();
                    stream.Dispose();
                    stream  = null;
                }

                if (client != null)
                {
                    client.Close();
                    client.Dispose();
                    client  = null;
                }

/*
                if (eventThread != null)
                {
                    eventThread.Abort();
                    eventThread = null;
                }
*/                
            }
            catch (Exception err)
            {
                LogInfo.WriteLogEDC("- Error Process_ClosePaymentDlg: " + err.Message);
            }
            finally
            {
                LogInfo.WriteLogEDC("+ Complete Process_ClosePaymentDlg");
                TCT_Lib.bActiveEMVDlg = false;
                this.Respond(ResponseType.Cancel);
                //---------------------------
                //--- Simulate
#if __TEST_EDC__
                SimEMV();
                this.Respond(ResponseType.Ok);
#endif
                //-----------------------------
                TCT_Lib.laneController.manageProcessEMV -= LaneController_ManageProcessEMV;
                Dispose();
                Destroy();
            }
        }

        protected void OnKeyPressEvent(object o, KeyPressEventArgs args)
        {
            if (TCT_Lib.keyMapping.ContainsKey((int)args.Event.KeyValue))
                TCT_Lib.webSocketLane.SendVirtualKeyboard(TCT_Lib.keyMapping[(int)args.Event.KeyValue], 1);
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


            xOutCreditCard                  = new objCreditCardInfo();
            xOutCreditCard.szRef1           = xOutCreditCard.szRef1 ?? "";
            xOutCreditCard.szRef2           = xOutCreditCard.szRef2 ?? "";
            xOutCreditCard.szCardNo         = xOutCreditCard.szCardNo ?? "";
            xOutCreditCard.szInvoiceNo      = xOutCreditCard.szInvoiceNo ?? "";

            xOutCreditCard.szResponseText   = "345345";
            xOutCreditCard.szApprovalCode   = "111111";
            xOutCreditCard.szCardNo         = "345435";
            xOutCreditCard.szTerminalID     = "345345";
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