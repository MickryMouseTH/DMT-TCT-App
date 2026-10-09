using System;
using static ESCPOS.Commands;
using ESCPOS;
using ESCPOS.Utils;
using System.Text;
using TCT_App.EMV.V0;
using System.Globalization;
using System.Web;
using System.Threading;

namespace TCT_App.PrintUtils
{
    public class PrintReceiptLib
    {
        private static Thread printReceiptThread;
        //-----------------------------------------------------------------
        //const string szPrinterPort = "/dev/usb/lp2";
        /*
                public static void PrintReceipt(int nTrx, bool bTowing = false, bool bConvoy = false, string szConvoyClassType = "", string szConvoyFareAmount = "", string szConvoyPayType = "", bool bPrintTest = false)
                {
                    if (!bTowing)
                    {
                        if (TCT_Lib.iCurTC_Class > 2)
                            return;
                        if (TCT_Lib.xCurPayment.payType == (int)PayType.DMT_CARD)
                            return;
                    }
                    else
                    {
                        if (TCT_Lib.iTowingTC_Class > 2)
                            return;
                        if (TCT_Lib.xTowingPayment.payType == (int)PayType.DMT_CARD)
                            return;
                    }

                    //------------------------------------------------------------------------------------
                    printReceiptThread                  = new Thread(() => ThreadPrintReceipt(nTrx, bTowing, bConvoy, szConvoyClassType, szConvoyFareAmount, szConvoyPayType, bPrintTest));
                    printReceiptThread.Priority         = ThreadPriority.AboveNormal;
                    printReceiptThread.IsBackground     = true;
                    printReceiptThread.Start();
                    //ThreadPrintReceipt(bTowing, bConvoy, szConvoyClassType, szConvoyFareAmount, szConvoyPayType, bPrintTest);
                }
        */

        static bool bPrintOriginal  = true;
        static bool bPrintFail      = false;
        //public static void ThreadPrintReceipt(int nTrx, bool bTowing = false, bool bConvoy = false, string szConvoyClassType = "", string szConvoyFareAmount = "", string szConvoyPayType = "", bool bPrintTest = false)
        public static void PrintReceipt(int nTrx, bool bTowing = false, bool bConvoy = false, string szConvoyClassType = "", string szConvoyFareAmount = "", string szConvoyPayType = "", bool bPrintTest = false)
        {

/*
            string szIV = "Zq3t6w9z$C&F)J@N";
            string szKey = "Zq3t6w9z$C&F)J@NcRfUjXn2r5u7x!A%";

            byte[] bKey = Encoding.UTF8.GetBytes(szKey);
            byte[] bIV  = Encoding.UTF8.GetBytes(szIV);
            byte[] bData = ClassLibAES.EncryptStringToBytes_Aes("LOUIS", bKey, bIV);
            string szResult = Convert.ToBase64String(bData);                      

            string szDecode = ClassLibAES.DecryptString(szKey, szIV, szResult);
*/


/*
            TCT_Lib.iFooter_PrintType   = 1;
            TCT_Lib.szFooter_Line1      = "Test BuzzeBee Encrytion";
            TCT_Lib.szFooter_Line2      = "สแกนเพื่อรับส่วนบด 10 นาท";
            //TCT_Lib.szFooter_QrData = "http://www.google.com?q=9udpJ6adjivbfryIM0knm5aFsic5TwcGfVwzzxGZ4aZiFurKIE8w3lcHDeb4dYuGZ14+hGOpP48NoUkqhjV+DN3xGtPBF2s9TZVD6oA16TVYf6K9udpJ6adjivbfryIM0knm5aFsic5TwcGfVwzzxGZ4aZiFurKIE8w3lcHDeb4dYuGZ14+hGOpP48NoUkqhjV+DN3xGtPBF2s9TZVD6oA16TVYf6K";
            ///TCT_Lib.szFooter_QrData = "http://www.google.com?q=9udpJ6adjivbfryIM0knm5aFsic5TwcGfVwzzxGZ4aZiFurKIE8w3lcHDeb4dYuGZ14+hGOpP48NoUkqhjV+DN3xGtPBF2s9TZV";
            //TCT_Lib.szFooter_QrData = "https://dev-web-tollway.azurewebsites.net/linelogin?qr=am9udpJ6adjivbfryIM0knm7oTixbCO2ZQxHbTONT2v3Zktyb8YjcXa366xgvY8LGqiYq8caL28YJo3GtRauyMOMJe4aTuNLL7EgQJBNshwbCFtR2xLRjiAuacEB0mDnaxLS6MaNbZqqeUgoz5S6dVU9NYgYcxLTURIjxEKnILowYa";
            //TCT_Lib.szFooter_QrData     = "https://stg-web-tollway.azurewebsites.net/linelogin?qr=";
            TCT_Lib.szFooter_QrData         = "https://liff.line.me/1657091550-2nGZPXK3?target_page=qr/";
            //TCT_Lib.szFooter_QrData     = "https://dev-web-tollway.azurewebsites.net/linelogin?qr=am9udpJ6adjivbfryIM0knm7oTixbCO2ZQxHbTONT2v3Zktyb8YjcXa366xgvY8LGqiYq8caL28YJo3GtRauyMOMJe4aTu";
*/

           
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ PrintReceiptLib.PrintReceipt : nTrx={nTrx}, towing={bTowing}, convoy={bConvoy}, test={bPrintTest}, TC_Class={(bTowing ? TCT_Lib.iTowingTC_Class : TCT_Lib.iCurTC_Class)}, PayType={(bTowing ? TCT_Lib.xTowingPayment?.payType : TCT_Lib.xCurPayment?.payType)}, port={TCT_Lib.szPrinter_PrinterPort}");
             if (!bTowing)
            {
                if (TCT_Lib.iCurTC_Class > 2)
                {
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ PrintReceiptLib.PrintReceipt : TC_Class={TCT_Lib.iCurTC_Class} > 2, no receipt");
                    return;
                }
                if (TCT_Lib.xCurPayment.payType == (int)PayType.DMT_CARD)
                {
                    LogInfo.WriteLog(LogLevel.DEBUG, "+ PrintReceiptLib.PrintReceipt : DMT card payment, no receipt");
                    return;
                }
            }
            else
            {
                if (TCT_Lib.iTowingTC_Class > 2)
                {
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ PrintReceiptLib.PrintReceipt : towing TC_Class={TCT_Lib.iTowingTC_Class} > 2, no receipt");
                    return;
                }
                if (TCT_Lib.xTowingPayment.payType == (int)PayType.DMT_CARD)
                {
                    LogInfo.WriteLog(LogLevel.DEBUG, "+ PrintReceiptLib.PrintReceipt : towing DMT card payment, no receipt");
                    return;
                }
            }

            //-----------------------------------------
            bool bReprint       = false;
            bool bReprintTowing = false;

            string szStr            = "";
            string szPrinterPort    = TCT_Lib.szPrinter_PrinterPort;
            var sw                  = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                if (!System.IO.File.Exists(szPrinterPort))
                    LogInfo.WriteLog(LogLevel.WARN, $"- Warning PrintReceiptLib.PrintReceipt : printer device not found : {szPrinterPort}");

                if (TCT_Lib.iPrinter_PrintFontSize > 2)
                    TCT_Lib.iPrinter_PrintFontSize = 2;
                if (TCT_Lib.iPrinter_PrintFontSize <= 0)
                    TCT_Lib.iPrinter_PrintFontSize = 1;

                //byte[] qrCodeCommand = PrintQRCode("Some data");
                //qrCodeCommand.Print(szPrinterPort);
                //byte[] barCodeCommand = PrintBarCode(BarCodeType.EAN13, "9780201379624");
                //barCodeCommand.Print(szPrinterPort);

                Console.WriteLine("+ Print Receipt to Device : " + szPrinterPort);
                if (bPrintTest)
                    TCT_Lib.szCurFareReceiptNo = "xxxx-xxxxxxxxxxxx";
                else
                {
                    if (!bTowing)
                    {
                        if (!TCT_Lib.bHasGenReceiptNo)
                        {
                            TCT_Lib.szCurFareReceiptNo = TCT_Lib.LaneDB.GetRunningReceiptNo(PrintReceiptType.FareType);   // "DD01-2020000218";
                            bPrintOriginal = true;
                        }
                        else
                        {
                            if (bPrintOriginal)
                                bReprint = false;
                            else
                                bReprint = true;
                        }
                    }
                    else
                    {
                        if (!TCT_Lib.bHasGenTowingReceiptNo)
                        {
                            TCT_Lib.szCurFareReceiptNo = TCT_Lib.LaneDB.GetRunningReceiptNo(PrintReceiptType.FareType);   // "DD01-2020000218";
                            bPrintOriginal = true;
                        }
                        else
                        {
                            if (bPrintOriginal)
                                bReprintTowing = false;
                            else
                                bReprintTowing = true;
                        }
                    }
                }

                Console.WriteLine("+ Fare Receipt Running No : " + TCT_Lib.szCurFareReceiptNo);
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ PrintReceiptLib.PrintReceipt : ReceiptNo={TCT_Lib.szCurFareReceiptNo}, reprint={bReprint}, reprintTowing={bReprintTowing}, printOriginal={bPrintOriginal}, lastPrintFail={bPrintFail}, HasGen={TCT_Lib.bHasGenReceiptNo}/{TCT_Lib.bHasGenTowingReceiptNo}");
                LogInfo.WriteLog($"+ nTrx={nTrx}, Fare Receipt Running No : {TCT_Lib.szCurFareReceiptNo}");
                LogInfo.WriteLaneEvent($"+ nTrx={nTrx}, Fare Receipt Running No : {TCT_Lib.szCurFareReceiptNo}");
                string szPlazaLaneName  = TCT_Lib.szPlazaName + " - Lane " + TCT_Lib.iLaneID;

                if (!TCT_Lib.szListReceiptNo.Contains(TCT_Lib.szCurFareReceiptNo))
                    TCT_Lib.szListReceiptNo.Add(TCT_Lib.szCurFareReceiptNo);

                string szClassType = "", szFareAmount = "", szPayType = "";
                if (!bTowing)
                {
                    szClassType     = (bConvoy) ? szConvoyClassType : TCT_Lib.iCurTC_Class.ToString("0");
                    szFareAmount    = (bConvoy) ? szConvoyFareAmount : TCT_Lib.xCurPayment.payAmount.ToString("0.00");
                    szPayType       = (bConvoy) ? szConvoyPayType : TCT_Lib.xCurPayment.payTypeName;
                }
                else
                {
                    szClassType     = TCT_Lib.iTowingTC_Class.ToString("0");
                    szFareAmount    = TCT_Lib.fTowingFareAmount.ToString("0.00");
                    szPayType       = TCT_Lib.xTowingPayment.payTypeName;
                }
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ PrintReceiptLib.PrintReceipt : Class={szClassType}, Fare={szFareAmount}, PayType={szPayType}, Job={TCT_Lib.iJobNo}, FontSize={TCT_Lib.iPrinter_PrintFontSize}");

                //------------------------------------------------------
                szStr   = "";
                szStr   = ClassEscPOS.InitPrint();
                var enc = Encoding.GetEncoding("windows-874");
                byte[] byteRequest = enc.GetBytes(szStr);
                byteRequest.Print(szPrinterPort);
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ PrintReceiptLib.PrintReceipt : init sent {byteRequest.Length} bytes to {szPrinterPort}");

                //--------------------------------------
                szStr = "";
                szStr += ClassEscPOS.SetCodePageThai();
                szStr += ClassEscPOS.SetFontAlignment((int)ClassEscPOS.Alignment.CenterAlignment);
                szStr += ClassEscPOS.PrintLogo();
                szStr += ClassEscPOS.ResetFont();
                szStr += ClassEscPOS.SetFontSize(TCT_Lib.iPrinter_PrintFontSize);
                //szStr += "123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890\n";
                //szStr += ClassEscPOS.SetFontBold(1);
                szStr += ClassEscPOS.SetFontAlignment((int)ClassEscPOS.Alignment.CenterAlignment);
                //szStr += "บริษัท ทางยกระดับดอนเมือง จำกัด (มหาชน)\n";
                //szStr += "โทร 1233            \n";
                //szStr += "ใบรับค่าธรรมเนียมผ่านทางหลวงพิเศษ\n";
                //szStr += "ทางหลวงหมายเลข 31        \n";
                szStr += "บริษัท ทางยกระดับดอนเมือง จำกัด (มหาชน)\n";
                szStr += "Tollway Call Center 1233\n";
                szStr += "ใบรับค่าธรรมเนียมผ่านทางหลวงพิเศษ\n";
                szStr += "ทางหลวงหมายเลข 31\n";
                szStr += ClassEscPOS.FeedLine(1);

                if (!bTowing)
                {
                    if (bReprint)
                        szStr += "******** พิมพ์ซ้ำ ***********";
                }
                else
                {
                    if (bReprintTowing)
                        szStr += "******** พิมพ์ซ้ำ ***********";
                }

                //------------------------------------------------------------
                if (bPrintTest)
                {
                    szStr += ClassEscPOS.FeedLine(1);
                    szStr           += "***** ทดสอบการพิมพ์ ******";
                    szClassType     = "3";
                    szFareAmount    = "1.00";
                    szPayType       = "เงินสด";
                }

                //---------------------------------------------------------------
                if (bReprint)  
                    LogInfo.WriteLaneEvent($"+ * Reprint * : JOB / TRx = {TCT_Lib.iJobNo + " / " + nTrx}, Receipt No : {TCT_Lib.szCurFareReceiptNo}");
                else
                    LogInfo.WriteLaneEvent($"+ Normal : JOB / TRx = {TCT_Lib.iJobNo + " / " + nTrx}, Receipt No : {TCT_Lib.szCurFareReceiptNo}");
                //-----------------------------------------------------------------

                szStr += ClassEscPOS.SetFontAlignment((int)ClassEscPOS.Alignment.LeftAlignment);
                szStr += ClassEscPOS.FeedLine(1);
                //szStr += ClassEscPOS.Print2Column("พนักงานเก็บเงิน", TCT_Lib.szStaffName + " (" + TCT_Lib.szStaffCode + ")");
                szStr += ClassEscPOS.Print2Column("JOB / TRX", TCT_Lib.iJobNo + " / " +  nTrx + " (" + TCT_Lib.szStaffCode + ")");   // Request from DMT (2022/02/18)
                szStr += ClassEscPOS.Print2Column("เลขที่ Receipt Running No", TCT_Lib.szCurFareReceiptNo);
                szStr += ClassEscPOS.Print2Column("วันที่เวลา Date Time", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                szStr += ClassEscPOS.Print2Column("ด่านทางเข้า-ช่องทางที่", szPlazaLaneName);
                szStr += ClassEscPOS.Print2Column("ประเภทรถ Class", szClassType);
                //szStr += ClassEscPOS.Print2Column("บาท Baht (VAT Included)", szFareAmount);
                szStr += ClassEscPOS.Print2Column("บาท Baht", szFareAmount);
                szStr += ClassEscPOS.Print2Column("ชำระเงินโดย Payment", szPayType);
                szStr += ClassEscPOS.Print2Column("ชำระเงินสำหรับ Payment For", "TOLL FARE");
                szStr += ClassEscPOS.Print2Column("Tax ID", "0107537001129");
                szStr += ClassEscPOS.FeedLine(1);
                szStr += ClassEscPOS.SetFontBold(0);
                szStr += ClassEscPOS.SetFontAlignment((int)ClassEscPOS.Alignment.CenterAlignment);
                szStr += "กรุณาทำสำเนาเพื่อนำไปใช้ในธุรกรรมต่อไป";
                //-------------------------------------------------------------------------------
/*
                szStr += ClassEscPOS.FeedLine(1);
                szStr += new string('-', 45) + "\r\n";
                szStr += ClassEscPOS.Print2Column("ชื่อ/สกุล", new string('_', 45));
                szStr += ClassEscPOS.Print2Column("ที่อยู่", new string('_', 45));
                szStr += ClassEscPOS.Print2Column("", new string('_', 45));
                szStr += ClassEscPOS.Print2Column("โทร", new string('_',45));
                szStr += ClassEscPOS.Print2Column("Email", new string('_', 45));

                //-------------------------------------------------------------------------------
                szStr += ClassEscPOS.FeedLine(1);
                szStr += ClassEscPOS.SetFontAlignment((int)ClassEscPOS.Alignment.CenterAlignment);
                szStr += ClassEscPOS.PrintQrCode("https://www.tollway.co.th/");
*/
                //-------------------------------------------------------------------------------
                if (TCT_Lib.iPrinter_PrintFooter == 1)
                {
                    if (DateTime.Now < TCT_Lib.dtFooter_EndDate)
                        szStr += GenFooter_BuzzsBees(nTrx);
                    else
                        LogInfo.WriteLog($"+ Footer not print (Now is more then efffectiveToDate");
                }
                else
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ PrintReceiptLib.PrintReceipt : footer disabled (PrintFooter={TCT_Lib.iPrinter_PrintFooter})");

                //-------------------------------------------------------
                szStr += ClassEscPOS.ResetFont();
                szStr += ClassEscPOS.SetFontAlignment((int)ClassEscPOS.Alignment.LeftAlignment);
                szStr += ClassEscPOS.FeedLine(6);
                szStr += ClassEscPOS.CutPaper();
/*
                szStr += ClassEscPOS.Print2Column("Total...", "80.00 Baht");
                szStr += ClassEscPOS.SetFontAlignment((int)ClassEscPOS.Alignment.CenterAlignment);
                szStr += "Don Mueang TollWay พรุ่งนี้\n";
                szStr += ClassEscPOS.SetFontAlignment((int)ClassEscPOS.Alignment.LeftAlignment);
                szStr += ClassEscPOS.Print2Column("รวมราคาทั้งสิ้น...", "80.00 บาท");
                szStr += ClassEscPOS.FeedLine(3);
                szStr += ClassLibEscPOS.CutPaper();
*/                               

                if (!bConvoy)
                {
                    if (!bTowing)
                        TCT_Lib.bHasGenReceiptNo = true;
                    else
                        TCT_Lib.bHasGenTowingReceiptNo = true;
                }

                //-----------------------------------------------
                //var enc = Encoding.GetEncoding("windows-874");
                byteRequest = enc.GetBytes(szStr);
                //byte[] byteRequest = Encoding.UTF8.GetBytes(szStr);
                //byteRequest.Add(COMMAND);
                //if (TCT_Lib.bPrintOK)
                if (LogInfo.IsEnabled(LogLevel.TRACE))
                    LogInfo.WriteLog(LogLevel.TRACE, "+ PrintReceiptLib.PrintReceipt ESC/POS : " + ClassEscPOS.ToLogText(szStr));
                byteRequest.Print(szPrinterPort);
                bPrintFail = false;
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ PrintReceiptLib.PrintReceipt : sent {byteRequest.Length} bytes to {szPrinterPort}, ReceiptNo={TCT_Lib.szCurFareReceiptNo}, {sw.ElapsedMilliseconds} ms");

                //---------------------------------------------------------------------------
                Thread.Sleep(50);
                byteRequest = null;
                //printReceiptThread.Abort();
                if (!bConvoy)
                {
                    if (!bTowing)
                    {
                        if (TCT_Lib.bHasGenReceiptNo)
                            bPrintOriginal = false;
                    }
                    else
                    {
                        if (TCT_Lib.bHasGenTowingReceiptNo)
                            bPrintOriginal = false;
                    }
                }
            }
            catch (ThreadAbortException)
            {
                //Thread.ResetAbort();
                LogInfo.WriteLog(LogLevel.ERROR, $"- Error PrintReceipt : thread aborted, nTrx={nTrx}, ReceiptNo={TCT_Lib.szCurFareReceiptNo}");
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                LogInfo.WriteLog("- Error Print Receipt : " + err.Message);
                LogInfo.WriteLaneEvent("- Error Print Receipt : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, $"- PrintReceiptLib.PrintReceipt : failed after {sw.ElapsedMilliseconds} ms, port={szPrinterPort}, ReceiptNo={TCT_Lib.szCurFareReceiptNo}, StackTrace : {err.StackTrace}");
                bPrintFail = true;
                //----------------------------------------------------------
                if (err.Message == "Interrupted system call")
                {
                    LogInfo.WriteLog(LogLevel.WARN, "- Warning PrintReceiptLib.PrintReceipt : interrupted system call, wait 1000 ms (no auto re-print)");
                    Thread.Sleep(1000);
/*
                    LogInfo.WriteLog("+ Auto Re-print receipt ");
                    LogInfo.WriteLaneEvent("+ Auto Re-print receipt ");

                    try
                    {
                        //--- Cut Paper
                        String szPrintCut = "";
                        szPrintCut  = ClassEscPOS.InitPrint();
                        szPrintCut +=  ClassEscPOS.CutPaper();
                        var enc     = Encoding.GetEncoding("windows-874");
                        byte[] byteRequest = enc.GetBytes(szPrintCut);
                        byteRequest.Print(szPrinterPort);
                        //------------------------------------------------

                        byteRequest = enc.GetBytes(szStr);
                        byteRequest.Print(szPrinterPort);
                    }
                    catch (Exception err2)
                    {
                        LogInfo.WriteLog("- Error Print Receipt : " + err.Message);
                        LogInfo.WriteLaneEvent("- Error Print Receipt : " + err.Message);
                    }
*/
                }
            }
        }

        static string GenFooter_BuzzsBees(int nTrx)
        {
            string szStr = "";
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ PrintReceiptLib.GenFooter_BuzzsBees : nTrx={nTrx}, PrintType={TCT_Lib.iFooter_PrintType}, maintenance={TCT_Lib.bMaintenanceMode}, until={TCT_Lib.dtFooter_EndDate:yyyy-MM-dd HH:mm:ss}");

            szStr += ClassEscPOS.FeedLine(1);
            szStr += new string('-', 45) + "\r\n";
            //szStr += ClassEscPOS.FeedLine(1);

            szStr += ClassEscPOS.SetFontAlignment((int)ClassEscPOS.Alignment.CenterAlignment);

            if (TCT_Lib.szFooter_Line1.Trim() != "")
                szStr += TCT_Lib.szFooter_Line1 + "\n";
            if (TCT_Lib.szFooter_Line2.Trim() != "")
                szStr += TCT_Lib.szFooter_Line2 + "\n";
            if (TCT_Lib.szFooter_Line3.Trim() != "")
                szStr += TCT_Lib.szFooter_Line3 + "\n";
            if (TCT_Lib.szFooter_Line4.Trim() != "")
                szStr += TCT_Lib.szFooter_Line4 + "\n";

            szStr += ClassEscPOS.FeedLine(1);

            if (TCT_Lib.iFooter_PrintType == 1)
            {
                //--- Gen from transaction
                var InvC            = CultureInfo.InvariantCulture;
                string szTransNo    = $"{TCT_Lib.szLaneCode}_{DateTime.Now.ToString("yyMMdd_HHmmss", InvC)}_{TCT_Lib.iJobNo}_{nTrx}";
                //trans_no=AN04202206280000001& trans_date=2022-06-28&l=AN04&d=20220628153800&rc=AN04-202206000001&vt=1&pt=C&fare=35
                //string szData       = $"trans_no={szTransNo}&trans_date={DateTime.Today.ToString("yyyy-MM-dd", InvC)}&l={TCT_Lib.szLaneCode}&d={DateTime.Now.ToString("yyyyMMddHHmmss", InvC)}&rc={TCT_Lib.szCurFareReceiptNo}&vt={TCT_Lib.iCurTC_Class.ToString("0")}&pt={GetStrPayType(TCT_Lib.xCurPayment.payType)}&fare={TCT_Lib.xCurPayment.payAmount.ToString("0")}";
                string szData           = $"trans_no={szTransNo}&rc={TCT_Lib.szCurFareReceiptNo}&vt={TCT_Lib.iCurTC_Class.ToString("0")}&pt={GetStrPayType(TCT_Lib.xCurPayment.payType)}&fare={TCT_Lib.xCurPayment.payAmount.ToString("0")}";
                LogInfo.WriteLog($"+ Footer Data QR : {szData}");
                LogInfo.WriteLaneEvent($"+ Footer Data QR : {szData}");

                //--- For DEV
                //string szEncryptKey = "Yq3t6w9z$C&F)J@NcRfTjWnZr4u7x!A%";
                //string szEncryptIV  = "Yq3t6w9z$C&F)J@N";
                //--- For STG
                //string szEncryptKey = "5v8x/A?D(G+KbPeShVmYq3t6w9z$B&E)";
                //string szEncryptIV  = "5v8x/A?D(G+KbPeSN";
                //--- For PRD
               string szEncryptKey  = "cRfUjXn2r5u8x/A?D(G+KbPeSgVkYp3s";
                string szEncryptIV  = "cRfUjXn2r5u8x/A?";

                //---------------------------------------------
                if (TCT_Lib.bMaintenanceMode)
                    TCT_Lib.szFooter_QrData = "https://www.tollway.co.th?q=";
                //---------------------------------------------
                string szEncryptData    = ClassLibEDC.AES_CBC_EncryptString(szData, szEncryptKey, szEncryptIV);
                string szUrl            = $"{TCT_Lib.szFooter_QrData}{HttpUtility.UrlEncode(szEncryptData)}";
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ PrintReceiptLib.GenFooter_BuzzsBees : dataLen={szData.Length}, encryptedLen={szEncryptData?.Length}, urlLen={szUrl.Length}");

/*
                //--- For DEV
                string szUrl = $"{TCT_Lib.szFooter_QrData}{szEncryptData}";
                //--- For STG
                //string szUrl = $"https://stg-web-tollway.azurewebsites.net/linelogin?qr={szEncryptData}";
                //--- For PRD
                //string szUrl = $"https://tollway.buzzebees.com/linelogin?qr={szEncryptData}";
*/

                LogInfo.WriteLog($"+ Footer Print Url QR : {szUrl}");
                LogInfo.WriteLaneEvent($"+ Footer Print Url QR : {szUrl}");
                Console.WriteLine("URL QR : " + szUrl);

                szStr += ClassEscPOS.ResetFont();
                szStr += ClassEscPOS.SetFontSize(1);
                szStr += ClassEscPOS.SetFontAlignment((int)ClassEscPOS.Alignment.CenterAlignment);
                
                //byte[] qrCodeCommand = PrintQRCode(szUrl, QRCodeModel.Model2, QRCodeCorrection.Percent7, QRCodeSize.Large);
                //szStr += Encoding.UTF8.GetString(qrCodeCommand, 0, qrCodeCommand.Length);
                szStr += ClassEscPOS.PrintQrCode(szUrl);
            }
            else if (TCT_Lib.iFooter_PrintType == 2)
            {
                if (TCT_Lib.szFooter_QrData.Trim() != "")
                {
                    //--- Fix Code from QR Data
                    string szUrl = TCT_Lib.szFooter_QrData;

                    LogInfo.WriteLog($"+ Footer Print Url Fix QR Data : {szUrl}");
                    LogInfo.WriteLaneEvent($"+ Footer Print Url Fix QR Data: {szUrl}");
                    Console.WriteLine("URL QR : " + szUrl);

                    szStr += ClassEscPOS.PrintQrCode(szUrl);
                }
            }

            Console.WriteLine("QR Data : " + szStr);
            if (LogInfo.IsEnabled(LogLevel.TRACE))
                LogInfo.WriteLog(LogLevel.TRACE, "+ PrintReceiptLib.GenFooter_BuzzsBees ESC/POS : " + ClassEscPOS.ToLogText(szStr));
            return szStr;
        }

        static string GetStrPayType(int payType)
        {
            switch (payType)
            {
                case (int)PayType.CASH:
                    return "C";
                case (int)PayType.COUPON:
                    return "V";
                case (int)PayType.MPass:
                case (int)PayType.EasyPass:
                    return "T";
                case (int)PayType.EMV:
                    return "E";
                case (int)PayType.QrCode:
                    return "Q";
                case (int)PayType.DMT_CARD:
                    return "D";
                default:
                    return "X";
            }
        }


    }
}
