using System;
using static ESCPOS.Commands;
using ESCPOS;
using ESCPOS.Utils;
using System.Text;

namespace TCT_App.PrintUtils
{
    public class PrintSalesCouponLib
    {
        public static void PrintSalesCoupon(string szCouponType1, int iTotalCoupon_Type1, decimal fCouponPrice_Type1, string szCouponType2, int iTotalCoupon_Type2, decimal fCouponPrice_Type2, string szPayTypeName)
        {
            bool bReprint = false;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ PrintSalesCoupon : ReceiptNo={TCT_Lib.szCurSalesCouponReceiptNo}, Type1={szCouponType1} x{iTotalCoupon_Type1} @{fCouponPrice_Type1}, Type2={szCouponType2} x{iTotalCoupon_Type2} @{fCouponPrice_Type2}, PayType={szPayTypeName}, port={TCT_Lib.szPrinter_PrinterPort}");
            try
            {
                string szPrinterPort = TCT_Lib.szPrinter_PrinterPort;
                if (!System.IO.File.Exists(szPrinterPort))
                    LogInfo.WriteLog(LogLevel.WARN, $"- Warning PrintSalesCoupon : printer device not found : {szPrinterPort}");
                if (TCT_Lib.iPrinter_PrintFontSize > 2)
                    TCT_Lib.iPrinter_PrintFontSize = 2;
                if (TCT_Lib.iPrinter_PrintFontSize <= 0)
                    TCT_Lib.iPrinter_PrintFontSize = 1;

                //byte[] qrCodeCommand = PrintQRCode("Some data");
                //qrCodeCommand.Print(szPrinterPort);
                //byte[] barCodeCommand = PrintBarCode(BarCodeType.EAN13, "9780201379624");
                //barCodeCommand.Print(szPrinterPort);

                Console.WriteLine("+ Print Receipt to Device : " + szPrinterPort);
                Console.WriteLine("+ Sales Coupon Receipt Running No : " + TCT_Lib.szCurSalesCouponReceiptNo);

                string szPlazaLaneName      = TCT_Lib.szPlazaName + " - Lane " + TCT_Lib.iLaneID;
                string szCouponAmount       = ((iTotalCoupon_Type1 * fCouponPrice_Type1) + (iTotalCoupon_Type2 * fCouponPrice_Type2)).ToString("N");
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
                szStr += "บริษัท ทางยกระดับดอนเมือง จำกัด (มหาชน)\n";
                szStr += "Tollway Call Center 1233\n";
                szStr += "ใบรับค่าธรรมเนียมการขายคูปองผ่านทางหลวงพิเศษ\n";
                szStr += "ทางหลวงหมายเลข 31\n";
                szStr += ClassEscPOS.FeedLine(1);
                if (bReprint)
                    szStr += "******** พิมพ์ซ้ำ ***********";
                szStr += ClassEscPOS.SetFontAlignment((int)ClassEscPOS.Alignment.LeftAlignment);
                szStr += ClassEscPOS.FeedLine(1);
                szStr += ClassEscPOS.Print2Column("พนักงานเก็บเงิน", TCT_Lib.szStaffName + " (" + TCT_Lib.szStaffCode + ")");
                szStr += ClassEscPOS.Print2Column("เลขที่ Receipt Running No", TCT_Lib.szCurSalesCouponReceiptNo);
                szStr += ClassEscPOS.Print2Column("วันที่เวลา Date Time", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                szStr += ClassEscPOS.Print2Column("ด่านทางเข้า-ช่องทางที่", szPlazaLaneName);
                if (iTotalCoupon_Type1 > 0)
                    szStr += ClassEscPOS.Print2Column($"ชนิดราคา {szCouponType1} บาท", iTotalCoupon_Type1.ToString("0"));
                if (iTotalCoupon_Type2 > 0)
                    szStr += ClassEscPOS.Print2Column($"ชนิดราคา {szCouponType2} บาท", iTotalCoupon_Type2.ToString("0"));
                //szStr += ClassEscPOS.Print2Column("บาท Baht (VAT Included)", szCouponAmount);
                szStr += ClassEscPOS.Print2Column("บาท Baht", szCouponAmount);
                szStr += ClassEscPOS.Print2Column("ชำระเงินโดย Payment", szPayTypeName);
                szStr += ClassEscPOS.Print2Column("ชำระเงินสำหรับ Payment For", "COUPON");
                szStr += ClassEscPOS.Print2Column("Tax ID", "0107537001129");
                szStr += ClassEscPOS.FeedLine(1);
                szStr += ClassEscPOS.SetFontBold(0);
                szStr += ClassEscPOS.SetFontAlignment((int)ClassEscPOS.Alignment.CenterAlignment);
                szStr += "กรุณาทำสำเนาเพื่อนำไปใช้ในธุรกรรมต่อไป";
                //-------------------------------------------------------------------------------

                szStr += ClassEscPOS.FeedLine(1);
                szStr += new string('-', 45) + "\r\n";
/*
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
                if (LogInfo.IsEnabled(LogLevel.TRACE))
                    LogInfo.WriteLog(LogLevel.TRACE, "+ PrintSalesCoupon ESC/POS : " + ClassEscPOS.ToLogText(szStr));
                byteRequest.Print(szPrinterPort);
                LogInfo.WriteLog(LogLevel.INFO, $"+ PrintSalesCoupon : sent {byteRequest.Length} bytes to {szPrinterPort}, amount={szCouponAmount}, {sw.ElapsedMilliseconds} ms");
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                LogInfo.WriteLog(LogLevel.ERROR, $"- Error PrintSalesCoupon : {err.Message}");
                LogInfo.WriteLog(LogLevel.DEBUG, $"- PrintSalesCoupon : failed after {sw.ElapsedMilliseconds} ms, StackTrace : {err.StackTrace}");
            }
        }
    }
}
