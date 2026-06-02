using System;
using static ESCPOS.Commands;
using ESCPOS;
using ESCPOS.Utils;
using System.Text;
using TCT_App.EMV.V0;
using System.Globalization;
using System.Drawing;
using QRCoder;
using System.Drawing.Printing;

namespace TCT_App.PrintUtils
{
    public enum PrintTextAlignment
    {
        Left    = 0,
        Center  = 1,
        Right   = 2,
    }


    public class PrintGraphicLib
    {
        static decimal fMmToPxRatio         = (decimal)3.7795;     // Convert mm -> Pixel (x*96)/25.4
        static int iPrintLineSpacing        = 0;
        static int iPaperWidth              = 84;
        static int iPaperHeight             = 3200;
        static int iMarginLeft              = 0;
        static int iMarginRight             = 0;
        static int iMarginTop               = 0;
        static int iMarginBottom            = 0;
        static int iCol1_WidthPercent       = 60;
        static int iCol2_WidthPercent       = 15;
        static int iCol3_WidthPercent       = 25;
        static int iColItem1_WidthPercent   = 60;
        static int iColItem2_WidthPercent   = 15;
        static int iColItem3_WidthPercent   = 25;
        static int iColItem4_WidthPercent   = 0;
        static int iColItem5_WidthPercent   = 0;

        static System.Drawing.Font font       = new System.Drawing.Font("Tohoma", 10, FontStyle.Regular);
        static System.Drawing.Font fontBold   = new System.Drawing.Font("Tohoma", 10, FontStyle.Bold);
        //----------------------------------------------------------------
        static int PrintText1Column(Graphics g, int iPosY, int iAlignment, System.Drawing.Font font, string szStr)
        {
            Brush brush     = new SolidBrush(Color.Black);
            int iPosX       = 0;
            int iPrintWidth = (int)(iPaperWidth * fMmToPxRatio) -
                              (int)((iMarginLeft + iMarginRight) * fMmToPxRatio);

            SizeF size = g.MeasureString(szStr, font);
            if (iAlignment == 0)           // Left
                iPosX = 0;
            else if (iAlignment == 1)       // Center
                iPosX = (int)(iPrintWidth - size.Width) / 2;
            else
                iPosX = (int)(iPrintWidth - size.Width);
            g.DrawString(szStr, font, brush, iPosX, iPosY);

            iPosY += iPrintLineSpacing + (int) size.Height;
            return iPosY;
        }

        static int PrintText2Column(Graphics g, int iPosY, int iAlignment1, int iAlignment2, System.Drawing.Font font, string szStr1, string szStr2)
        {
            Brush brush     = new SolidBrush(Color.Black);
            int iPosX       = 0;
            int iPrintWidth = (int)(iPaperWidth * fMmToPxRatio) -
                              (int)((iMarginLeft + iMarginRight) * fMmToPxRatio);

            SizeF size1     = g.MeasureString(szStr1, font);
            SizeF size2     = g.MeasureString(szStr2, font);

            int iPaperHalfWidth = iPrintWidth/ 2;

            //--- Text 1
            if (iAlignment1 == 0)        // Left
                iPosX = 0;
            else if (iAlignment1 == 1)   // Center
                iPosX = (int)(iPaperHalfWidth - size1.Width) / 2;
            else
                iPosX = (int)(iPaperHalfWidth - size1.Width);
            g.DrawString(szStr1, font, brush, iPosX, iPosY);

            //--- Text 2
            if (iAlignment2 == 0)           // Left
                iPosX = iPaperHalfWidth;
            else if (iAlignment2 == 1)      // Center
                iPosX = iPaperHalfWidth + ((int)(iPaperHalfWidth - size2.Width) / 2);
            else
                iPosX = iPaperHalfWidth + ((int)(iPaperHalfWidth - size2.Width));
            g.DrawString(szStr2, font, brush, iPosX, iPosY);

            float iHeight = size1.Height;
            if (size1.Height == 0.0)
            {
                if (size2.Height > 0)
                    iHeight = size2.Height;
            }

            iPosY += iPrintLineSpacing + (int)iHeight;
            return iPosY;
        }

        static int PrintText3Column(Graphics g, int iPosY, int iAlignment1, int iAlignment2, int iAlignment3,
                                                System.Drawing.Font font, string szStr1, string szStr2, string szStr3)
        {
            Brush brush     = new SolidBrush(Color.Black);
            int iPosX       = 0;
            int iPrintWidth = (int)(iPaperWidth * fMmToPxRatio) -
                                (int)((iMarginLeft + iMarginRight) * fMmToPxRatio);

            SizeF size1     = g.MeasureString(szStr1, font);
            SizeF size2     = g.MeasureString(szStr2, font);
            SizeF size3     = g.MeasureString(szStr3, font);

            //--- Text 1
            if (iAlignment1 == 0)           // Left
                iPosX = 0;
            else if (iAlignment1 == 1)      // Center
                iPosX = (int)(iPrintWidth - size1.Width) / 2;
            else
                iPosX = (int)(iPrintWidth - size1.Width);
            g.DrawString(szStr1, font, brush, iPosX, iPosY);

            //--- Text 2
            int iStartX = (int)(iPrintWidth * iCol1_WidthPercent) / 100;
            if (iAlignment2 == 0)           // Left
                iPosX = iStartX;
            else if (iAlignment2 == 1)      // Center
                iPosX = iStartX + (int)(((iCol2_WidthPercent * iPrintWidth) / 100) - size2.Width) / 2;
            else
                iPosX = iStartX + (int)(((iCol2_WidthPercent * iPrintWidth) / 100) - size2.Width);
            g.DrawString(szStr2, font, brush, iPosX, iPosY);

            //--- Text 3
            if (iAlignment3 == 0)           // Left
                iPosX = 0;
            else if (iAlignment3 == 1)      // Center
                iPosX = (int)(iPrintWidth - size3.Width) / 2;
            else
                iPosX = (int)(iPrintWidth - size3.Width);
            g.DrawString(szStr3, font, brush, iPosX, iPosY);

            float iHeight = size1.Height;
            if (size1.Height == 0.0)
            {
                if (size2.Height > 0)
                    iHeight = size2.Height;
                else if (size3.Height > 0)
                    iHeight = size3.Height;
            }

            iPosY += iPrintLineSpacing + (int)iHeight;
            return iPosY;
        }

        static int LineFeedNoCol(int iNoCol, int iPosY)
        {
            iPosY += (int)(iNoCol * fMmToPxRatio);
            return iPosY;
        }

        private static Image CreateQrCodeImage(string szQrCode)
        {
            QRCodeGenerator qrGen   = new QRCodeGenerator();
            QRCodeData qrCodeData   = qrGen.CreateQrCode(szQrCode, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode           = new QRCode(qrCodeData);
            Bitmap qrCodeImage      = qrCode.GetGraphic(10);
            return qrCodeImage;
        }

        public static int PrintQrCodeCenter(Graphics g, int iPosY,  string szQrCode, int iImgWidth = 100)
        {
            try
            {
                var img             = CreateQrCodeImage(szQrCode);
                iPosY               += 5;
                float iPrintWidth   = (int)(iPaperWidth * fMmToPxRatio) -
                                        (int)((iMarginLeft + iMarginRight) * fMmToPxRatio);
                //int iPosX       = (int)((iPaperWidth - ((img.Width*60/img.HorizontalResolution)/2)) / 2);
                float imageWidth    = iImgWidth;
                //int iPosX           = (int)((iPaperWidth*2 - ((decimal)imageWidth/ClassLib.fMmToPxRatio)) / 2);
                int iPosX           = (int)((iPrintWidth - imageWidth)/2);
                g.DrawImage(img, iPosX, iPosY);
                float imageHeight   = 200 * img.Height / img.VerticalResolution;
                //iPosY               += (int)((decimal)imageHeight / ClassLib.fMmToPxRatio);
                iPosY += 90;
                
                //return PrintText1Column(g, iPosY, 1, new Font("Tahoma", 8, FontStyle.Regular), szBarCode);
                return iPosY;
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("-Error PrintQrCodeCenter : " + err.Message);
                return iPosY;
            }
        }

        public static int PrintItemText1Column(Graphics g, int iPosY, int iAlignment, System.Drawing.Font font, string szStr)
        {
            Brush brush     = new SolidBrush(Color.Black);
            int iPosX       = 0;
            int iPrintWidth = (int)(iPaperWidth * fMmToPxRatio) -
                              (int)((iMarginLeft + iMarginRight) * fMmToPxRatio);

            SizeF size = g.MeasureString(szStr, font);
            if (iAlignment == 0)           // Left
                iPosX = 0;
            else if (iAlignment == 1)       // Center
                iPosX = (int)(iPrintWidth - size.Width) / 2;
            else
                iPosX = (int)(iPrintWidth - size.Width);
            g.DrawString(szStr, font, brush, iPosX, iPosY);

            iPosY += iPrintLineSpacing + (int)size.Height;
            return iPosY;
        }

        public static int PrintItemText2Column(Graphics g, int iPosY, int iAlignment1, int iAlignment2, int iAlignment3, int iAlignment4, int iAlignment5,
                                                    System.Drawing.Font font, string szStr1, string szStr2)
        {
            Brush brush         = new SolidBrush(Color.Black);
            int iPosX           = 0;
            int iPrintWidth     = (int)(iPaperWidth * fMmToPxRatio) -
                                    (int)((iMarginLeft + iMarginRight) * fMmToPxRatio);

            SizeF size1         = g.MeasureString(szStr1, font);
            SizeF size2         = g.MeasureString(szStr2, font);

            int iPaperWidth1    = (int)(iPrintWidth * (iColItem1_WidthPercent + iColItem2_WidthPercent + iColItem3_WidthPercent)) / 100;
            int iPaperWidth2    = (int)(iPrintWidth * (iColItem4_WidthPercent + iColItem5_WidthPercent)) / 100;

            //--- Text 1
            if (iAlignment1 == 0)        // Left
                iPosX = 0;
            else if (iAlignment1 == 1)   // Center
                iPosX = (int)(iPaperWidth1 - size1.Width) / 2;
            else
                iPosX = (int)(iPaperWidth1 - size1.Width);
            g.DrawString(szStr1, font, brush, iPosX, iPosY);

            //--- Text 2
            if (iAlignment2 == 0)           // Left
                iPosX = iPaperWidth1;
            else if (iAlignment2 == 1)      // Center
                iPosX = iPaperWidth1 + ((int)(iPaperWidth2 - size2.Width) / 2);
            else
                iPosX = iPaperWidth1 + ((int)(iPaperWidth2 - size2.Width));
            g.DrawString(szStr2, font, brush, iPosX, iPosY);

            float iHeight = size1.Height;
            if (size1.Height == 0.0)
            {
                if (size2.Height > 0)
                    iHeight = size2.Height;
            }

            iPosY += iPrintLineSpacing + (int)iHeight;
            return iPosY;
        }


        //-----------------------------------------
        public static void PrintReceipt(bool bTowing = false, bool bConvoy = false, string szConvoyClassType = "", string szConvoyFareAmount = "", string szConvoyPaType = "", bool bPrintTest = false)
        {

            try
            {
                using (var pd = new PrintDocument())
                {
                    PrintController printController = new StandardPrintController();
                    pd.PrintController              = printController;
                    pd.DocumentName                 = "Print Bill Receipt";
                    //pd.PrinterSettings.PrinterName  = TCT_Lib.szPrinter_PrinterPort;
                    LogInfo.WriteLog("Printer Name : " + pd.PrinterSettings.PrinterName);
                    Console.WriteLine("Printer Name : " + pd.PrinterSettings.PrinterName);
                    pd.OriginAtMargins              = true;
                    Margins margins                 = new Margins((int)(iMarginLeft * fMmToPxRatio),
                                                                (int)(iMarginRight * fMmToPxRatio),
                                                                (int)(iMarginTop * fMmToPxRatio),
                                                                (int)(iMarginBottom * fMmToPxRatio));
                    pd.DefaultPageSettings.Margins      = margins;
                    pd.DefaultPageSettings.PaperSize    = new PaperSize("Receipt Paper", (int)(iPaperWidth * fMmToPxRatio),
                                                                    (int)(iPaperHeight * fMmToPxRatio));
                    //pd.PrintPage                    += new PrintPageEventHandler(pd_PrintBillReceiptPage);
                    pd.PrintPage                    += (owner, ev) => pd_PrintBillReceiptPage(ev, bTowing, bConvoy, szConvoyClassType, szConvoyFareAmount, szConvoyPaType, bPrintTest);
                    pd.Print();
                    pd.Dispose();
                }
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Error btnPrintReceipt (Printer spooler stop : " + err.Message);
            }
        }

        static private void pd_PrintBillReceiptPage(PrintPageEventArgs ev, bool bTowing, bool bConvoy, string szConvoyClassType, string szConvoyFareAmount, string szConvoyPaType, bool bPrintTest)
        {
            using (Graphics g = ev.Graphics)
            {
                int iPosY = 0;

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


                //-----------------------------------------
                bool bReprint = false;
                try
                {
                    string szPrinterPort = TCT_Lib.szPrinter_PrinterPort;
                    if (TCT_Lib.iPrinter_PrintFontSize > 2)
                        TCT_Lib.iPrinter_PrintFontSize = 2;
                    if (TCT_Lib.iPrinter_PrintFontSize <= 0)
                        TCT_Lib.iPrinter_PrintFontSize = 1;
                        
                    Console.WriteLine("+ Print Receipt to Device : " + szPrinterPort);
                    if (bPrintTest)
                        TCT_Lib.szCurFareReceiptNo = "xxxx-xxxxxxxxxxxx";
                    else
                    {
                        if (!TCT_Lib.bHasGenReceiptNo)
                            TCT_Lib.szCurFareReceiptNo = TCT_Lib.LaneDB.GetRunningReceiptNo(PrintReceiptType.FareType);   // "DD01-2020000218";
                        else
                            bReprint = true;
                    }

                    Console.WriteLine("+ Fare Receipt Running No : " + TCT_Lib.szCurFareReceiptNo);
                    LogInfo.WriteLog($"+ nTrx={TCT_Lib.nTrx}, Fare Receipt Running No : {TCT_Lib.szCurFareReceiptNo}");
                    LogInfo.WriteLaneEvent($"+ nTrx={TCT_Lib.nTrx}, Fare Receipt Running No : {TCT_Lib.szCurFareReceiptNo}");
                    string szPlazaLaneName  = TCT_Lib.szPlazaName + " - Lane " + TCT_Lib.iLaneID;

                    if (!TCT_Lib.szListReceiptNo.Contains(TCT_Lib.szCurFareReceiptNo))
                        TCT_Lib.szListReceiptNo.Add(TCT_Lib.szCurFareReceiptNo);

                    string szClassType = "", szFareAmount = "", szPayType = "";
                    if (!bTowing)
                    {
                        szClassType     = (bConvoy) ? szConvoyClassType : TCT_Lib.iCurTC_Class.ToString("0");
                        szFareAmount    = (bConvoy) ? szConvoyFareAmount : TCT_Lib.xCurPayment.payAmount.ToString("0.00");
                        szPayType       = (bConvoy) ? szConvoyPaType : TCT_Lib.xCurPayment.payTypeName;
                    }
                    else
                    {
                        szClassType     = TCT_Lib.iTowingTC_Class.ToString("0");
                        szFareAmount    = TCT_Lib.fTowingFareAmount.ToString("0.00");
                        szPayType       = TCT_Lib.xTowingPayment.payTypeName;
                    }

                    //--------------------------------------------------
                    int iAlignCol1 = (int)PrintTextAlignment.Center;
                    string szStr = "บริษัท ทางยกระดับดอนเมือง จำกัด (มหาชน)";
                    iPosY = PrintItemText1Column(g, iPosY, iAlignCol1, font, szStr);
                    szStr = "Tollway Call Center 1233";
                    iPosY = PrintItemText1Column(g, iPosY, iAlignCol1, font, szStr);
                    szStr = "ใบรับค่าธรรมเนียมผ่านทางหลวงพิเศษ";
                    iPosY = PrintItemText1Column(g, iPosY, iAlignCol1, font, szStr);
                    szStr = "ทางหลวงหมายเลข 31";
                    iPosY = PrintItemText1Column(g, iPosY, iAlignCol1, font, szStr);

                    iPosY = LineFeedNoCol(1, iPosY);

                    if (bReprint)
                    {
                        szStr = "******** พิมพ์ซ้ำ ***********";
                        iPosY = PrintItemText1Column(g, iPosY, iAlignCol1, font, szStr);
                    }

                    if (bPrintTest)
                    {
                        iPosY       = LineFeedNoCol(1, iPosY);
                        szStr       = "***** ทดสอบการพิมพ์ ******";
                        iPosY       = PrintItemText1Column(g, iPosY, iAlignCol1, font, szStr);
                        szClassType     = "3";
                        szFareAmount    = "1.00";
                        szPayType       = "เงินสด";
                    }

                    iPosY = LineFeedNoCol(1, iPosY);

                    iAlignCol1      = (int)PrintTextAlignment.Left;
                    int iAlignCol2  = (int)PrintTextAlignment.Right;
                    int iAlignCol3 = 0, iAlignCol4 = 0, iAlignCol5 = 0;
                    iPosY = PrintItemText2Column(g, iPosY, iAlignCol1, iAlignCol2, iAlignCol3, iAlignCol4, iAlignCol5, font, "JOB / TRX", TCT_Lib.iJobNo + " / " + TCT_Lib.nTrx + " (" + TCT_Lib.szStaffCode + ")");
                    iPosY = PrintItemText2Column(g, iPosY, iAlignCol1, iAlignCol2, iAlignCol3, iAlignCol4, iAlignCol5, font, "เลขที่ Receipt Running No", TCT_Lib.szCurFareReceiptNo);
                    iPosY = PrintItemText2Column(g, iPosY, iAlignCol1, iAlignCol2, iAlignCol3, iAlignCol4, iAlignCol5, font,"วันที่เวลา Date Time", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                    iPosY = PrintItemText2Column(g, iPosY, iAlignCol1, iAlignCol2, iAlignCol3, iAlignCol4, iAlignCol5, font, "ด่านทางเข้า-ช่องทางที่", szPlazaLaneName);
                    iPosY = PrintItemText2Column(g, iPosY, iAlignCol1, iAlignCol2, iAlignCol3, iAlignCol4, iAlignCol5, font, "ประเภทรถ Class", szClassType);
                    iPosY = PrintItemText2Column(g, iPosY, iAlignCol1, iAlignCol2, iAlignCol3, iAlignCol4, iAlignCol5, font, "บาท Baht", szFareAmount);
                    iPosY = PrintItemText2Column(g, iPosY, iAlignCol1, iAlignCol2, iAlignCol3, iAlignCol4, iAlignCol5, font, "ชำระเงินโดย Payment", szPayType);
                    iPosY = PrintItemText2Column(g, iPosY, iAlignCol1, iAlignCol2, iAlignCol3, iAlignCol4, iAlignCol5, font, "ชำระเงินสำหรับ Payment For", "TOLL FARE");
                    iPosY = PrintItemText2Column(g, iPosY, iAlignCol1, iAlignCol2, iAlignCol3, iAlignCol4, iAlignCol5, font, "Tax ID", "0107537001129");

                    iPosY = LineFeedNoCol(1, iPosY);

                    szStr = "กรุณาทำสำเนาเพื่อนำไปใช้ในธุรกรรมต่อไป";
                    iPosY = PrintItemText1Column(g, iPosY, iAlignCol1, fontBold, szStr);


                    //--------------------------------

                }
                catch (Exception err)
                {
                    Console.WriteLine("- Error Print Receipt to Device : " + err.Message);

                }
            }
            ev.HasMorePages = false;
        }

        public static void PrintReceipt2(bool bTowing=false, bool bConvoy=false, string szConvoyClassType = "", string szConvoyFareAmount = "", string szConvoyPaType = "", bool bPrintTest = false)
        {

            TCT_Lib.iFooter_PrintType   = 1;
            TCT_Lib.szFooter_Line1      = "Test BuzzesBee Encrytion";
            TCT_Lib.szFooter_Line2      = "สแกนเพื่อรับส่วนบด 10 นาท";
            //TCT_Lib.szFooter_QrData = "http://www.google.com?q=9udpJ6adjivbfryIM0knm5aFsic5TwcGfVwzzxGZ4aZiFurKIE8w3lcHDeb4dYuGZ14+hGOpP48NoUkqhjV+DN3xGtPBF2s9TZVD6oA16TVYf6K9udpJ6adjivbfryIM0knm5aFsic5TwcGfVwzzxGZ4aZiFurKIE8w3lcHDeb4dYuGZ14+hGOpP48NoUkqhjV+DN3xGtPBF2s9TZVD6oA16TVYf6K";
            ///TCT_Lib.szFooter_QrData = "http://www.google.com?q=9udpJ6adjivbfryIM0knm5aFsic5TwcGfVwzzxGZ4aZiFurKIE8w3lcHDeb4dYuGZ14+hGOpP48NoUkqhjV+DN3xGtPBF2s9TZV";
            TCT_Lib.szFooter_QrData     = "https://dev-web-tollway.azurewebsites.net/linelogin?qr=";

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


            //-----------------------------------------
            bool bReprint = false;
            try
            {
                string szPrinterPort = TCT_Lib.szPrinter_PrinterPort;
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
                    if (!TCT_Lib.bHasGenReceiptNo)
                        TCT_Lib.szCurFareReceiptNo = TCT_Lib.LaneDB.GetRunningReceiptNo(PrintReceiptType.FareType);   // "DD01-2020000218";
                    else
                        bReprint = true;
                }

                Console.WriteLine("+ Fare Receipt Running No : " + TCT_Lib.szCurFareReceiptNo);
                LogInfo.WriteLog($"+ nTrx={TCT_Lib.nTrx}, Fare Receipt Running No : {TCT_Lib.szCurFareReceiptNo}");
                LogInfo.WriteLaneEvent($"+ nTrx={TCT_Lib.nTrx}, Fare Receipt Running No : {TCT_Lib.szCurFareReceiptNo}");
                string szPlazaLaneName  = TCT_Lib.szPlazaName + " - Lane " + TCT_Lib.iLaneID;

                if (!TCT_Lib.szListReceiptNo.Contains(TCT_Lib.szCurFareReceiptNo))
                    TCT_Lib.szListReceiptNo.Add(TCT_Lib.szCurFareReceiptNo);

                string szClassType = "", szFareAmount = "", szPayType = "";
                if (!bTowing)
                {
                    szClassType     = (bConvoy) ? szConvoyClassType : TCT_Lib.iCurTC_Class.ToString("0");
                    szFareAmount    = (bConvoy) ? szConvoyFareAmount : TCT_Lib.xCurPayment.payAmount.ToString("0.00");
                    szPayType       = (bConvoy) ? szConvoyPaType : TCT_Lib.xCurPayment.payTypeName;
                }
                else
                {
                    szClassType     = TCT_Lib.iTowingTC_Class.ToString("0");
                    szFareAmount    = TCT_Lib.fTowingFareAmount.ToString("0.00");
                    szPayType       = TCT_Lib.xTowingPayment.payTypeName;
                }


                //------------------------------------------------------
                string szStr = ClassEscPOS.InitPrint();
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
                if (bReprint)
                    szStr += "******** พิมพ์ซ้ำ ***********";

                if (bPrintTest)
                {
                    szStr += ClassEscPOS.FeedLine(1);
                    szStr           += "***** ทดสอบการพิมพ์ ******";
                    szClassType     = "3";
                    szFareAmount    = "1.00";
                    szPayType       = "เงินสด";
                }

                szStr += ClassEscPOS.SetFontAlignment((int)ClassEscPOS.Alignment.LeftAlignment);
                szStr += ClassEscPOS.FeedLine(1);
                //szStr += ClassEscPOS.Print2Column("พนักงานเก็บเงิน", TCT_Lib.szStaffName + " (" + TCT_Lib.szStaffCode + ")");
                szStr += ClassEscPOS.Print2Column("JOB / TRX", TCT_Lib.iJobNo + " / " + TCT_Lib.nTrx + " (" + TCT_Lib.szStaffCode + ")");   // Request from DMT (2022/02/18)
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
                        szStr += GenFooter_BuzzsBees();
                    else
                        LogInfo.WriteLog($"+ Footer not print (Now is more then efffectiveToDate");
                }

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

                var enc = Encoding.GetEncoding("windows-874");
                byte[] byteRequest = enc.GetBytes(szStr);
                //byte[] byteRequest = Encoding.UTF8.GetBytes(szStr);
                //byteRequest.Add(COMMAND);
                byteRequest.Print(szPrinterPort);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }

        static string GenFooter_BuzzsBees()
        {
            string szStr = "";

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
                string szTransNo    = $"{TCT_Lib.szLaneCode}_{DateTime.Now.ToString("yyMMdd_HHmmss", InvC)}_{TCT_Lib.iJobNo}_{TCT_Lib.nTrx}";
                //trans_no=AN04202206280000001& trans_date=2022-06-28&l=AN04&d=20220628153800&rc=AN04-202206000001&vt=1&pt=C&fare=35
                string szData       = $"trans_no={szTransNo}&trans_date={DateTime.Today.ToString("yyyy-MM-dd", InvC)}&l={TCT_Lib.szLaneCode}&d={DateTime.Now.ToString("yyyyMMddHHmmss", InvC)}&rc={TCT_Lib.szCurFareReceiptNo}&vt={TCT_Lib.iCurTC_Class.ToString("0")}&pt={GetStrPayType(TCT_Lib.xCurPayment.payType)}&fare={TCT_Lib.xCurPayment.payAmount.ToString("0")}";
                LogInfo.WriteLog($"+ Footer Data QR : {szData}");
                LogInfo.WriteLaneEvent($"+ Footer Data QR : {szData}");

                //--- For DEV
                string szEncryptKey = "Yq3t6w9z$C&F)J@NcRfTjWnZr4u7x!A%";
                string szEncryptIV  = "Yq3t6w9z$C&F)J@N";
                //--- For STG
                //string szEncryptKey = "5v8x/A?D(G+KbPeShVmYq3t6w9z$B&E)";
                //string szEncryptIV  = "5v8x/A?D(G+KbPeSN";
                //--- For PRD
                //string szEncryptKey = "";
                //string szEncryptIV  = ""; 

                string szEncryptData    = ClassLibEDC.AES_CBC_EncryptString(szData, szEncryptKey, szEncryptIV);
                string szUrl            = $"{TCT_Lib.szFooter_QrData}{szEncryptData}";

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

                byte[] qrCodeCommand = PrintQRCode(szUrl, QRCodeModel.Model1, QRCodeCorrection.Percent30, QRCodeSize.Normal);
                szStr += Encoding.UTF8.GetString(qrCodeCommand, 0, qrCodeCommand.Length);
                //szStr += ClassEscPOS.PrintQrCode(szUrl);
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
