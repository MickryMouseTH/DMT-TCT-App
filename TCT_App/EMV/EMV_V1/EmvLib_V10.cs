using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TCT_App.EMV.V0;

namespace TCT_App.EMV.EMV_V1
{
/*
    public class objCreditCardInfo_V1
    {
        public string szResponseText;
        public string szAmount;
        public string szMerchantName;
        public string szTransactionDate;
        public string szTransactionTime;
        public string szApprovalCode;
        public string szInvoiceNo;
        public string szTerminalID;
        public string szMerchantNo;
        public string szCustName;
        public string szCardIssueName;
        public string szCardExpireDate;
        public string szBatchNo;
        public string szReferenceNo;
        public string szCardIssueID;
        public string szCardHolderName;
        public string szCardNo;
        public string szRef1;
        public string szRef2;
        public string szRef3;
        public string szRef4;
    }
*/
    public class ClassLibEDC_V10
    {
        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        public static bool GeneratePaymentEdcMessage(decimal fPayAmount, ref byte[] byteMsg, ref string szPayMsg, ref string szRef1, ref string szRef2, ref string szErrMsg)
        {
            try
            {
                string szAmount     = fPayAmount.ToString("#,##0.00").Replace(",", "");
                string szMsg        = GenHexChar(0x02).ToString(); // Header-STX

                string szMsgData    = GenMessagePaymentData(szAmount, ref szRef1, ref szRef2);

                szMsg += GenLenBCD(szMsgData.Length);

                string szCharListLen = GenHexCharList(szMsg);
                LogInfo.WriteLogEDC("+ MsgLen : " + szCharListLen);


                szMsg += szMsgData;
                szMsg += GenHexChar(0x03).ToString();
                //szMsg += CheckSumLRC(GenLenBCD(szMsgData.Length) + szMsgData + GenHexChar(0x03));
                szMsg += CheckSumLRC(szMsg);

                string szCharList = GenHexCharList(szMsg);
                LogInfo.WriteLogEDC("+ EDC Payment : " + szCharList);
                //LogInfo.WriteLogInfo("+ Send String : " + szMsg);
                //Console.WriteLine("Byte Data : " + szCharList);
                szPayMsg    = szCharList;

                Encoding enc = new ASCIIEncoding();
                byte[] bMsg = new byte[szMsg.Length*2];
                //byteMsg     = enc.GetBytes(szMsg);
                bMsg = GetBytes(szMsg);
                int idx = 0;
                byteMsg = new byte[bMsg.Length / 2];
                for (int i=0; i<bMsg.Length; i++)
                {
                    byteMsg[idx++] = bMsg[i];
                    i++;
                }

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error GeneratePaymentEdcMessage : " + err.Message);
                LogInfo.WriteLogEDC("- Error GeneratePaymentEdcMessage : " + err.Message);
                szErrMsg = err.Message;

                return false;
            }
        }

        private static string GenMessagePaymentData(string szAmount, ref string szRef1, ref string szRef2)
        {
            string szMsg = "";
            //--- Transport Header
            string szTransportHeader = "";
            //szTransportHeader += "60";      // Transport Header Type;
            szTransportHeader   += "00";      // Transport Header Type;
            szTransportHeader   += "0000";    // Transport Destination
            szTransportHeader   += "0000";    // Transport Source
            szMsg += GenHexString(szTransportHeader);

            //--- Presentation Header
            string szPresentHeader = "";
            szPresentHeader     += "1";         // Format version
            szPresentHeader     += "0";         // Request-Response Indication (0=This message is a request message, 1=This message is a response message, 2=This message is request which does not require a response
            szPresentHeader     += "20";        // Transaction Code (20=Purchase)
            szPresentHeader     += "00";        // Response Code
            szPresentHeader     += "0";         // More indicator (0=This is last message for this transaction, 1=There is another message following this one with more field data for this transaction.
            szMsg += GenHexString(szPresentHeader);
            szMsg += GenHexChar(0x1C).ToString();   // Field Separator

            //--- Field Data Format
            //--- Field Amount (Field Type=40)
            string szFieldDataFmt   = "";
            szFieldDataFmt          += GenHexString("40");        // Amount, Transaction
            decimal fAmount         = decimal.Parse(szAmount);
            szAmount                = fAmount.ToString("0000000000.00");
            szAmount                = szAmount.Replace(".", "");
            szFieldDataFmt          += GenLenBCD(szAmount.Length);
            szFieldDataFmt          += szAmount;
            szMsg                   += GenHexString(szFieldDataFmt);
            szMsg                   += GenHexChar(0x1C).ToString();

            //-----------------------------------------------------------
            int iEdc_nTrx = TCT_Lib.nTrx;
            if (TCT_Lib.xListLoopCar != null)
            {
                iEdc_nTrx += TCT_Lib.xListLoopCar.Count;
                if (TCT_Lib.xListLoopCar.Count > 0)
                    LogInfo.WriteLogEDC($"+ Found vehicle in Loop = {TCT_Lib.xListLoopCar.Count}");
            }

            TCT_Lib.iCurEmvDetailID = iEdc_nTrx;
            LogInfo.WriteLogEDC($"Send EDC JobNo={TCT_Lib.iJobNo}, nTrx={iEdc_nTrx}");
            LogInfo.WriteLaneEvent($"Send EDC JobNo={TCT_Lib.iJobNo}, nTrx={iEdc_nTrx}");
            //------------------------------------------------------------------------------
            //--- Field Ref1 (Field Type=A1) PlazaId+LaneName+JobNo+nTrx
            szFieldDataFmt = "";
            szFieldDataFmt          += GenHexString("A1");
            string szPlazaId        = TCT_Lib.szPlazaID;
            if (TCT_Lib.szPlazaID.Length == 2)
                szPlazaId = "T" + TCT_Lib.szPlazaID;
            else if (TCT_Lib.szPlazaID.Length == 3)
                szPlazaId = "T" + TCT_Lib.szPlazaID.Substring(1, 2);
            string szLaneName       = TCT_Lib.szLaneName;
            if (szLaneName.Length < 4)
                szLaneName = szLaneName.PadRight(4, ' ');
            string szJobNo  = TCT_Lib.iJobNo.ToString().PadLeft(4, '0');
            string sznTrx   = iEdc_nTrx.ToString().PadLeft(5, '0');

            szRef1                  = (szPlazaId + szLaneName + szJobNo + sznTrx).PadRight(20, '0');
            LogInfo.WriteLogEDC("Send Ref1 = " + szRef1);
            szFieldDataFmt          += GenLenBCD(szRef1.Length);
            szFieldDataFmt          += szRef1;
            szMsg                   += GenHexString(szFieldDataFmt);
            szMsg                   += GenHexChar(0x1C).ToString();

            //--- Field Ref2 (Field Type=A2) TID + TimeStamp
            szFieldDataFmt          = "";
            szFieldDataFmt          += GenHexString("A2");

            string szTID            = TCT_Lib.szEdcTID;
            szRef2                  = szTID + DateTime.Now.ToString("yyMMddHHmmss");
            LogInfo.WriteLogEDC("Send Ref2 = " + szRef2);
            szFieldDataFmt          += GenLenBCD(szRef2.Length);
            szFieldDataFmt          += szRef2;
            szMsg                   += GenHexString(szFieldDataFmt);
            szMsg                   += GenHexChar(0x1C).ToString();

            LogInfo.WriteLaneEvent($"+ EMV : Ref1={szRef1}, Ref2={szRef2}");

            return szMsg;
        }
        //-----------------------------------------------------------------------------

        public static bool GenerateCancelEdcMessage(ref byte[] byteMsg, ref string szReqMsg, ref string szErrMsg)
        {
            try
            {
                string szMsg    = GenHexChar(0x02).ToString(); // Header-STX

                string szMsgData = GenMessageCancelData();

                szMsg += GenLenBCD(szMsgData.Length);
                szMsg += szMsgData;
                szMsg += GenHexChar(0x03).ToString();
                //szMsg += CheckSumLRC(GenLenBCD(szMsgData.Length) + szMsgData + GenHexChar(0x03));
                szMsg += CheckSumLRC(szMsg);

                string szCharList = GenHexCharList(szMsg);
                LogInfo.WriteLogEDC("+ EDC Cancel : " + szCharList);
                //LogInfo.WriteLogInfo("+ Send String : " + szMsg);
                //Console.WriteLine("Byte Data : " + szCharList);
                szReqMsg = szCharList;

                Encoding enc = new ASCIIEncoding();
                byteMsg      = enc.GetBytes(szMsg);

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error GenerateCancelEdcMessage : " + err.Message);
                LogInfo.WriteLogEDC("- Error GenerateCancelEdcMessage : " + err.Message);
                szErrMsg = err.Message;

                return false;
            }
        }

        private static string GenMessageCancelData()
        {
            string szMsg = "";
            //--- Transport Header
            string szTransportHeader = "";
            //szTransportHeader += "60";      // Transport Header Type;
            szTransportHeader += "00";      // Transport Header Type;
            szTransportHeader += "0000";    // Transport Destination
            szTransportHeader += "0000";    // Transport Source
            szMsg += GenHexString(szTransportHeader);

            //--- Presentation Header
            string szPresentHeader = "";
            szPresentHeader += "1";         // Format version
            szPresentHeader += "0";         // Request-Response Indication (0=This message is a request message, 1=This message is a response message, 2=This message is request which does not require a response
            szPresentHeader += "CP";        // Transaction Code (CP=Cancel Payment)
            szPresentHeader += "00";        // Response Code
            szPresentHeader += "0";         // More indicator (0=This is last message for this transaction, 1=There is another message following this one with more field data for this transaction.
            szMsg += GenHexString(szPresentHeader);
            szMsg += GenHexChar(0x1C).ToString();   // Field Separator

            return szMsg;
        }

        //-----------------------------------------------------------------------------

        public static bool GenerateQrCodeEdcMessage(decimal fPayAmount, ref byte[] byteMsg, ref string szReqMsg, ref string szEdcRef1, ref string szEdcRef2, ref string szErrMsg)
        {
            try
            {
                string szAmount = fPayAmount.ToString("#,##0.00").Replace(",", "");
                string szMsg    = GenHexChar(0x02).ToString(); // Header-STX

                string szMsgData = GenMessageGenerateQrCodeData(szAmount, ref szEdcRef1, ref szEdcRef2);

                szMsg += GenLenBCD(szMsgData.Length);
                szMsg += szMsgData;
                szMsg += GenHexChar(0x03).ToString();
                //szMsg += CheckSumLRC(GenLenBCD(szMsgData.Length) + szMsgData + GenHexChar(0x03));
                szMsg += CheckSumLRC(szMsg);

                string szCharList = GenHexCharList(szMsg);
                LogInfo.WriteLogEDC("+ EDC Generate QR Code : " + szCharList);
                //LogInfo.WriteLogInfo("+ Send String : " + szMsg);
                //Console.WriteLine("Byte Data : " + szCharList);
                szReqMsg        = szCharList;

                Encoding enc    = new ASCIIEncoding();
                byteMsg         = enc.GetBytes(szMsg);

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error GenerateQrCodeEdcMessage : " + err.Message);
                LogInfo.WriteLogEDC("- Error GenerateQrCodeEdcMessage : " + err.Message);
                szErrMsg = err.Message;

                return false;
            }
        }

        private static string GenMessageGenerateQrCodeData(string szAmount, ref string szEdcRef1, ref string szEdcRef2)
        {
            string szMsg = "";
            //--- Transport Header
            string szTransportHeader = "";
            //szTransportHeader += "60";      // Transport Header Type;
            szTransportHeader   += "00";      // Transport Header Type;
            szTransportHeader   += "0000";    // Transport Destination
            szTransportHeader   += "0000";    // Transport Source
            szMsg += GenHexString(szTransportHeader);

            //--- Presentation Header
            string szPresentHeader = "";
            szPresentHeader     += "1";         // Format version
            szPresentHeader     += "0";         // Request-Response Indication (0=This message is a request message, 1=This message is a response message, 2=This message is request which does not require a response
            szPresentHeader     += "QR";        // Transaction Code (QR Code)
            szPresentHeader     += "00";        // Response Code
            szPresentHeader     += "0";         // More indicator (0=This is last message for this transaction, 1=There is another message following this one with more field data for this transaction.
            szMsg += GenHexString(szPresentHeader);
            szMsg += GenHexChar(0x1C).ToString();   // Field Separator

            //--- Field Data Format
            //--- Field Amount (Field Type=40)
            string szFieldDataFmt   = "";
            szFieldDataFmt          += GenHexString("40");        // Amount, Transaction
            decimal fAmount         = decimal.Parse(szAmount);
            szAmount                = fAmount.ToString("0000000000.00");
            szAmount                = szAmount.Replace(".", "");
            szFieldDataFmt          += GenLenBCD(szAmount.Length);
            szFieldDataFmt          += szAmount;
            szMsg                   += GenHexString(szFieldDataFmt);
            szMsg                   += GenHexChar(0x1C).ToString();

            //------------------------------------------------------------------------------
            //--- Field Ref1 (Field Type=A1) PlazaId+LaneName+JobNo+nTrx

            int iEdc_nTrx = TCT_Lib.nTrx;
            if (TCT_Lib.xListLoopCar != null)
                iEdc_nTrx += TCT_Lib.xListLoopCar.Count;

            LogInfo.WriteLogEDC($"Send QR JobNo={TCT_Lib.iJobNo}, nTrx={iEdc_nTrx}");
            LogInfo.WriteLaneEvent($"Send QR JobNo={TCT_Lib.iJobNo}, nTrx={iEdc_nTrx}");
            //------------------------------------------------------------------------------

            szFieldDataFmt = "";
            szFieldDataFmt          += GenHexString("A1");
            string szPlazaId = TCT_Lib.szPlazaID;
            if (TCT_Lib.szPlazaID.Length == 2)
                szPlazaId = "T" + TCT_Lib.szPlazaID;
            else if (TCT_Lib.szPlazaID.Length == 3)
                szPlazaId = "T" + TCT_Lib.szPlazaID.Substring(1, 2);
            string szLaneName       = TCT_Lib.szLaneName;
            if (szLaneName.Length < 4)
                szLaneName = szLaneName.PadRight(4, ' ');
            string szJobNo  = TCT_Lib.iJobNo.ToString().PadLeft(4, '0');
            string sznTrx   = iEdc_nTrx.ToString().PadLeft(5, '0');

            string szRef1           = (szPlazaId + szLaneName + szJobNo + sznTrx).PadRight(20, '0');
            LogInfo.WriteLogEDC("Send Ref1 = " + szRef1);
            szEdcRef1               = szRef1;
            szFieldDataFmt          += GenLenBCD(szRef1.Length);
            szFieldDataFmt          += szRef1;
            szMsg                   += GenHexString(szFieldDataFmt);
            szMsg                   += GenHexChar(0x1C).ToString();

            //--- Field Ref2 (Field Type=A2) TID + TimeStamp
            szFieldDataFmt          = "";
            szFieldDataFmt          += GenHexString("A2");

            string szTID            = TCT_Lib.szEdcTID;
            string szRef2           = szTID + DateTime.Now.ToString("yyMMddHHmmss");
            LogInfo.WriteLogEDC("Send Ref2 = " + szRef2);
            szEdcRef2               = szRef2;
            szFieldDataFmt          += GenLenBCD(szRef2.Length);
            szFieldDataFmt          += szRef2;
            szMsg                   += GenHexString(szFieldDataFmt);
            szMsg                   += GenHexChar(0x1C).ToString();

            LogInfo.WriteLaneEvent($"+ QR : Ref1={szRef1}, Ref2={szRef2}");

            return szMsg;
        }

        //-----------------------------------------------------------------------------

        public static bool InquiryQrCodeEdcMessage(ref byte[] byteMsg, ref string szReqMsg, ref string szErrMsg)
        {
            try
            {
                string szMsg    = GenHexChar(0x02).ToString(); // Header-STX

                string szMsgData = GenMessageInquiryQrCodeData();

                szMsg += GenLenBCD(szMsgData.Length);
                szMsg += szMsgData;
                szMsg += GenHexChar(0x03).ToString();
                //szMsg += CheckSumLRC(GenLenBCD(szMsgData.Length) + szMsgData + GenHexChar(0x03));
                szMsg += CheckSumLRC(szMsg);

                string szCharList = GenHexCharList(szMsg);
                LogInfo.WriteLogEDC("+ EDC Inquiry QR Code : " + szCharList);
                //LogInfo.WriteLogInfo("+ Send String : " + szMsg);
                //Console.WriteLine("Byte Data : " + szCharList);
                szReqMsg    = szCharList;

                Encoding enc = new ASCIIEncoding();
                byteMsg      = enc.GetBytes(szMsg);

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error InquiryQrCodeEdcMessage : " + err.Message);
                LogInfo.WriteLogEDC("- Error InquiryQrCodeEdcMessage : " + err.Message);
                szErrMsg = err.Message;

                return false;
            }
        }

        private static string GenMessageInquiryQrCodeData()
        {
            string szMsg = "";
            //--- Transport Header
            string szTransportHeader = "";
            //szTransportHeader += "60";      // Transport Header Type;
            szTransportHeader   += "00";      // Transport Header Type;
            szTransportHeader   += "0000";    // Transport Destination
            szTransportHeader   += "0000";    // Transport Source
            szMsg += GenHexString(szTransportHeader);

            //--- Presentation Header
            string szPresentHeader = "";
            szPresentHeader     += "1";         // Format version
            szPresentHeader     += "0";         // Request-Response Indication (0=This message is a request message, 1=This message is a response message, 2=This message is request which does not require a response
            szPresentHeader     += "IQ";        // Transaction Code (Inquiry QR Payment)
            szPresentHeader     += "00";        // Response Code
            szPresentHeader     += "0";         // More indicator (0=This is last message for this transaction, 1=There is another message following this one with more field data for this transaction.
            szMsg += GenHexString(szPresentHeader);
            szMsg += GenHexChar(0x1C).ToString();   // Field Separator

            //--- Field Data Format
            //--- Field InvoiceNo (Field Type=65)
            string szFieldDataFmt   = "";
            szFieldDataFmt          += GenHexString("65");      // InvoiceNo
            string szInvoiceNo      = "000000";                 // 000000=Current InvoiceNo
            szFieldDataFmt          += GenLenBCD(szInvoiceNo.Length);
            szFieldDataFmt          += szInvoiceNo;
            szMsg                   += GenHexString(szFieldDataFmt);
            szMsg                   += GenHexChar(0x1C).ToString();

            return szMsg;
        }

        //-----------------------------------------------------------------------------

        public static bool GenerateEojSumReportEdcMessage(DateTime dtBOJ, ref byte[] byteMsg, ref string szReqMsg, ref string szEdcRef1, ref string szEdcRef2, ref string szEdcRef3, ref string szErrMsg)
        {
            try
            {
                string szMsg        = GenHexChar(0x02).ToString(); // Header-STX
                string szMsgData    = GenMessageGenerateEojSumReportData(dtBOJ, ref szEdcRef1, ref szEdcRef2, ref szEdcRef3);

                LogInfo.WriteLogEDC("+ Length : " + szMsgData.Length);

                szMsg += GenLenBCD(szMsgData.Length);
                szMsg += szMsgData;
                szMsg += GenHexChar(0x03).ToString();
                //szMsg += CheckSumLRC(GenLenBCD(szMsgData.Length) + szMsgData + GenHexChar(0x03));
                szMsg += CheckSumLRC(szMsg);

                string szCharList = GenHexCharList(szMsg);
                LogInfo.WriteLogEDC("+ EDC Generate EojSumReport : " + szCharList);
                //LogInfo.WriteLogInfo("+ Send String : " + szMsg);
                //Console.WriteLine("Byte Data : " + szCharList);
                szReqMsg        = szCharList;

                Encoding enc    = new ASCIIEncoding();
                byteMsg         = enc.GetBytes(szMsg);

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error GenerateEojSumReportEdcMessage : " + err.Message);
                LogInfo.WriteLogEDC("- Error GenerateEojSumReportEdcMessage : " + err.Message);
                szErrMsg = err.Message;

                return false;
            }
        }

        private static string GenMessageGenerateEojSumReportData(DateTime dtBOJ, ref string szEdcRef1, ref string szEdcRef2, ref string szEdcRef3)
        {
            string szMsg = "";
            //--- Transport Header
            string szTransportHeader = "";
            //szTransportHeader += "60";      // Transport Header Type;
            szTransportHeader   += "00";      // Transport Header Type;
            szTransportHeader   += "0000";    // Transport Destination
            szTransportHeader   += "0000";    // Transport Source
            szMsg += GenHexString(szTransportHeader);

            //--- Presentation Header
            string szPresentHeader = "";
            szPresentHeader     += "1";         // Format version
            szPresentHeader     += "0";         // Request-Response Indication (0=This message is a request message, 1=This message is a response message, 2=This message is request which does not require a response
            szPresentHeader     += "50";        // Transaction Code (50=Summary Report)
            szPresentHeader     += "00";        // Response Code
            szPresentHeader     += "0";         // More indicator (0=This is last message for this transaction, 1=There is another message following this one with more field data for this transaction.
            szMsg += GenHexString(szPresentHeader);
            szMsg += GenHexChar(0x1C).ToString();   // Field Separator

            //--- Field Data Format
            //--- Field Ref1 (Field Type=A1) PlazaId+LaneName+JobNo+nTrx
            int iEdc_nTrx = TCT_Lib.nTrx;
            if (TCT_Lib.xListLoopCar != null)
                iEdc_nTrx += TCT_Lib.xListLoopCar.Count;

            LogInfo.WriteLogEDC($"Send JobNo={TCT_Lib.iJobNo}, nTrx={iEdc_nTrx}");
            //------------------------------------------------------------------------------

            string szFieldDataFmt   = "";
            szFieldDataFmt          += GenHexString("A1");
            string szPlazaId = TCT_Lib.szPlazaID;
            if (TCT_Lib.szPlazaID.Length == 2)
                szPlazaId = "T" + TCT_Lib.szPlazaID;
            else if (TCT_Lib.szPlazaID.Length == 3)
                szPlazaId = "T" + TCT_Lib.szPlazaID.Substring(1, 2);
            string szLaneName       = TCT_Lib.szLaneName;
            if (szLaneName.Length < 4)
                szLaneName = szLaneName.PadRight(4, ' ');
            string szJobNo  = TCT_Lib.iJobNo.ToString().PadLeft(4, '0');
            string sznTrx   = iEdc_nTrx.ToString().PadLeft(5, '0');

            string szRef1           = (szPlazaId + szLaneName + szJobNo + sznTrx).PadRight(20, '0');
            LogInfo.WriteLogEDC("Send Ref1 = " + szRef1);
            szFieldDataFmt          += GenLenBCD(szRef1.Length);
            szFieldDataFmt          += szRef1;
            szMsg                   += GenHexString(szFieldDataFmt);
            szMsg                   += GenHexChar(0x1C).ToString();
            szEdcRef1               = szRef1;

            //--- Field Ref2 (Field Type=A2) TID + TimeStamp
            szFieldDataFmt          = "";
            szFieldDataFmt          += GenHexString("A2");

            string szTID            = TCT_Lib.szEdcTID;
            string szRef2           = szTID + DateTime.Now.ToString("yyMMddHHmmss");
            LogInfo.WriteLogEDC("Send Ref2 = " + szRef2);
            szFieldDataFmt          += GenLenBCD(szRef2.Length);
            szFieldDataFmt          += szRef2;
            szMsg                   += GenHexString(szFieldDataFmt);
            szMsg                   += GenHexChar(0x1C).ToString();
            szEdcRef2               = szRef2;

            //--- Field Ref2 (Field Type=A3) Date Time of Begin of JOB
            szFieldDataFmt          = "";
            szFieldDataFmt          += GenHexString("A3");

            string szRef3           = dtBOJ.ToString("yyMMddHHmmss");
            LogInfo.WriteLogEDC("Send Ref3 = " + szRef3);
            szFieldDataFmt          += GenLenBCD(szRef3.Length);
            szFieldDataFmt          += szRef3;
            szMsg                   += GenHexString(szFieldDataFmt);
            szMsg                   += GenHexChar(0x1C).ToString();
            szEdcRef3               = szRef3;

            return szMsg;
        }
        //============================================================================
        public static char GenHexChar(int ch)
        {
            return (char)ch;
        }

        private static string GenHexString(string szStr)
        {
            string szMsg = "";
            for (int i = 0; i < szStr.Length; i++)
                szMsg += GenHexChar(szStr[i]);
            return szMsg;
        }

        public static string GenHexCharList(string szMsg)
        {
            string szStr = "";
            byte[] ascii = Encoding.ASCII.GetBytes(szMsg);
            foreach (Byte b in ascii)
                szStr += b.ToString("X2") + " ";

            return szStr;
        }

        private static string CheckSumLRC(string szMsg)
        {
            Encoding enc    = new ASCIIEncoding();
            byte[] bMsg     = enc.GetBytes(szMsg);
            byte lrc = 0x0;
            for (int i = 0; i < bMsg.Length; i++)
                lrc ^= bMsg[i];

            return GenHexChar(lrc).ToString();
        }

        private static string GenLenBCD_V1(int iLen)
        {
            char[] chars    = iLen.ToString("0000").ToCharArray();
            int len         = chars.Length / 2;
            byte[] bytes    = new byte[len];
            string szBCD    = "";
            for (int i = 1; i >= 0; i--)
            {
                byte highNibble = byte.Parse(chars[2 * (len - 1) - 2 * i].ToString());
                byte lowNibble  = byte.Parse(chars[2 * (len - 1) - 2 * i + 1].ToString());
                bytes[i]        = (byte)((byte)(highNibble << 4) | lowNibble);
                szBCD           += GenHexChar(bytes[i]);
            }
            return szBCD;
        }

        private static string GenLenBCD_0(int iLen)
        {
            char[] chars    = iLen.ToString("0000").ToCharArray();
            int len         = chars.Length / 2;
            byte[] bytes    = new byte[len];
            string szBCD = "";
            for (int i = 1; i >= 0; i--)
            {
                byte highNibble = byte.Parse(chars[2 * (len - 1) - 2 * i].ToString());
                byte lowNibble  = byte.Parse(chars[2 * (len - 1) - 2 * i + 1].ToString());
                bytes[i] = (byte)((byte)(highNibble << 4) | lowNibble);
                szBCD           += GenHexChar(bytes[i]);
                //szBCD   += ((char)bytes[i]).ToString().PadLeft(2, '0');
            }

            string szCharListLen = GenHexCharList(szBCD);
            Console.WriteLine(szCharListLen);

            return szBCD;       
        }

        private static string GenLenBCD(int iLen)
        {
            char[] chars    = iLen.ToString("0000").ToCharArray();
            int len         = chars.Length / 2;
            byte[] bytes    = new byte[len];
            string szBCD = "";
            for (int i = 1; i >= 0; i--)
            {
                byte highNibble = byte.Parse(chars[2 * (len - 1) - 2 * i].ToString());
                byte lowNibble  = byte.Parse(chars[2 * (len - 1) - 2 * i + 1].ToString());
                bytes[i] = (byte)((byte)(highNibble << 4) | lowNibble);
                szBCD           += GenHexChar(bytes[i]);
                //szBCD   += ((char)bytes[i]).ToString().PadLeft(2, '0');
            }

            return szBCD;       
        }

        private static string GenLenBCD_V3(int input)
        {
            if (input > 9999 || input < 0)
                throw new ArgumentOutOfRangeException("input");

            int thousands = input / 1000;
            int hundreds = (input -= thousands * 1000) / 100;
            int tens = (input -= hundreds * 100) / 10;
            int ones = (input -= tens * 10);

            byte[] bcd = new byte[] {
                (byte)(thousands << 4 | hundreds),
                (byte)(tens << 4 | ones)
            };
            string szCharListLen = GenHexCharList(bcd.ToString());
            Console.WriteLine(szCharListLen);

            return bcd.ToString();
                       
        }

        private static int ConvertBCD2Int(byte[] bytes)
        {
            var bcd = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
            {
                byte bcdByte = bytes[i];
                int idHigh = bcdByte >> 4;
                int idLow = bcdByte & 0x0F;
                if (idHigh > 9 || idLow > 9)
                {
                    throw new ArgumentException(
                        String.Format("One of the argument bytes was not in binary-coded decimal format: byte[{0}] = 0x{1:X2}.",
                        i, bcdByte));
                }
                bcd.Append(string.Format("{0}{1}", idHigh, idLow));
            }
            return int.Parse(bcd.ToString());
        }

        public static string GetResponseMsg(string szCode)
        {
            switch (szCode)
            {
                case "00": return "Complete";
                case "ND": return "TXN CANCEL";
                case "TX": return "TXN CANCEL";
                case "EN": return "CONNECT FAIL";
                case "NA": return "NOT AVAILABLE";
                case "E1": return "Card is not allowed";
                case "E2": return "Expired Card";
                case "E3": return "Pick up Card";
                case "E4": return "Deny list Card";
                case "RS": return "Reversal Success";
                case "R1": return "Reversal 1st";
                case "C0": return "Contact reader ON-line";
                case "C1": return "Connect reader OFF-line";
                case "12": return "TX NOT FOUND";
                default: return szCode + ": Unknown Error";
            }
        }

        public static string ShowResponseMsg(string szCode)
        {
            string szDefErr = "ชำระด้วยบัตร EMV ไม่สำเร็จ\nกรุณาติดต่อธนาคารผู้ออกบัตร";
            switch (szCode)
            {
                case "00": return "จ่ายสำเร็จ";
                case "ND": 
                case "TX": 
                case "EN": 
                case "NA": 
                case "E1": 
                case "E2": 
                case "E3": 
                case "E4": 
                case "RS": 
                case "R1": 
                case "C0": 
                case "C1": 
                    return szDefErr;
                case "12": return "รอการสแกน";
                default: return szDefErr;
            }
        }

        public static objCreditCardInfo EncodeFormatEDC(string szData)
        {
            var xCreditCardInfo = new objCreditCardInfo();
            int iEndIdx     = 0;
            
            //--- Response Text
            int iStartIdx = szData.IndexOf("02" + (char)0x00);
            if (iStartIdx >= 0)
            {
                iEndIdx = szData.IndexOf((char)0x1c, iStartIdx + 1);
                xCreditCardInfo.szResponseText = szData.Substring(iStartIdx + 5, iEndIdx - iStartIdx - 5);
            }

            iStartIdx = szData.IndexOf((char)0x1C + "02" + (char)0x00);
            if (iStartIdx >= 0)
            {
                iEndIdx = szData.IndexOf((char)0x1c, iStartIdx + 1);
                xCreditCardInfo.szResponseText = szData.Substring(iStartIdx + 5, iEndIdx - iStartIdx - 5);
            }

            //--- Approve Code
            iStartIdx = szData.IndexOf((char)0x1C + "01" + (char)0x00);
            if (iStartIdx >= 0)
            {
                iEndIdx = szData.IndexOf((char)0x1c, iStartIdx + 1);
                xCreditCardInfo.szApprovalCode = szData.Substring(iStartIdx + 5, iEndIdx - iStartIdx - 5);
            }

            //--- Transaction Date
            iStartIdx = szData.IndexOf((char)0x1C + "03" + (char)0x00);
            if (iStartIdx >= 0)
            {
                iEndIdx = szData.IndexOf((char)0x1c, iStartIdx + 1);
                xCreditCardInfo.szTransactionDate = szData.Substring(iStartIdx + 5, iEndIdx - iStartIdx - 5);
            }

            //--- Transaction Time
            iStartIdx = szData.IndexOf((char)0x1C + "04" + (char)0x00);
            if (iStartIdx >= 0)
            {
                iEndIdx = szData.IndexOf((char)0x1c, iStartIdx + 1);
                xCreditCardInfo.szTransactionTime = szData.Substring(iStartIdx + 5, iEndIdx - iStartIdx - 5);
            }

            //--- Terminal ID
            iStartIdx = szData.IndexOf((char)0x1C + "16" + (char)0x00);
            if (iStartIdx >= 0)
            {
                iEndIdx = szData.IndexOf((char)0x1c, iStartIdx + 1);
                xCreditCardInfo.szTerminalID = szData.Substring(iStartIdx + 5, iEndIdx - iStartIdx - 5);
            }

            //--- Card Number
            iStartIdx = szData.IndexOf((char)0x1C + "30" + (char)0x00);
            if (iStartIdx >= 0)
            {
                iEndIdx = szData.IndexOf((char)0x1c, iStartIdx + 1);
                xCreditCardInfo.szCardNo = szData.Substring(iStartIdx + 5, iEndIdx - iStartIdx - 5);
            }

            //--- Expire Date
            iStartIdx = szData.IndexOf((char)0x1C + "31" + (char)0x00);
            if (iStartIdx >= 0)
            {
                iEndIdx = szData.IndexOf((char)0x1c, iStartIdx + 1);
                xCreditCardInfo.szCardExpireDate = szData.Substring(iStartIdx + 5, iEndIdx - iStartIdx - 5);
            }

            //--- Amount
            iStartIdx = szData.IndexOf((char)0x1C + "40" + (char)0x00);
            if (iStartIdx >= 0)
            {
                iEndIdx = szData.IndexOf((char)0x1c, iStartIdx + 1);
                xCreditCardInfo.szAmount = szData.Substring(iStartIdx + 5, iEndIdx - iStartIdx - 5);
            }

/*
            //--- Batch Number
            iStartIdx = szData.IndexOf("50" + (char)0x00);
            if (iStartIdx >= 0)
            {
                iEndIdx = szData.IndexOf((char)0x1c, iStartIdx+1);
                xCreditCardInfo.szBatchNo = szData.Substring(iStartIdx + 4, iEndIdx - iStartIdx - 4);
            }
*/

            //--- Invoice Number
            iStartIdx = szData.IndexOf((char)0x1C + "65" + (char)0x00);
            if (iStartIdx >= 0)
            {
                iEndIdx = szData.IndexOf((char)0x1c, iStartIdx + 1);
                xCreditCardInfo.szInvoiceNo = szData.Substring(iStartIdx + 5, iEndIdx - iStartIdx - 5);
            }

            //--- Ref1
            iStartIdx = szData.IndexOf((char)0x1C + "A1" + (char)0x00);
            if (iStartIdx >= 0)
            {
                iEndIdx = szData.IndexOf((char)0x1c, iStartIdx + 1);
                xCreditCardInfo.szRef1 = szData.Substring(iStartIdx + 5, iEndIdx - iStartIdx - 5);
            }

            //--- Ref2
            iStartIdx = szData.IndexOf((char)0x1C + "A2" + (char)0x00);
            if (iStartIdx >= 0)
            {
                iEndIdx = szData.IndexOf((char)0x1c, iStartIdx + 1);
                xCreditCardInfo.szRef2 = szData.Substring(iStartIdx + 5, iEndIdx - iStartIdx - 5);
            }
            //--- Ref3
            iStartIdx = szData.IndexOf((char)0x1C + "A3" + (char)0x00);
            if (iStartIdx >= 0)
            {
                iEndIdx = szData.IndexOf((char)0x1c, iStartIdx + 1);
                xCreditCardInfo.szRef3 = szData.Substring(iStartIdx + 5, iEndIdx - iStartIdx - 5);
            }

            //--- Ref4
            iStartIdx = szData.IndexOf((char)0x1C + "A4" + (char)0x00);
            if (iStartIdx >= 0)
            {
                iEndIdx = szData.IndexOf((char)0x1c, iStartIdx + 1);
                xCreditCardInfo.szRef4 = szData.Substring(iStartIdx + 5, iEndIdx - iStartIdx - 5);
            }

            //--- Merchant Name
            iStartIdx = szData.IndexOf((char)0x1C + "D0" + (char)0x00);
            if (iStartIdx >= 0)
            {
                iEndIdx = szData.IndexOf((char)0x1c, iStartIdx + 1);
                xCreditCardInfo.szMerchantName = szData.Substring(iStartIdx + 5, iEndIdx - iStartIdx - 5);
            }
            //--- Merchant ID
            iStartIdx = szData.IndexOf((char)0x1C + "D1" + (char)0x00);
            if (iStartIdx >= 0)
            {
                iEndIdx = szData.IndexOf((char)0x1c, iStartIdx + 1);
                xCreditCardInfo.szMerchantNo = szData.Substring(iStartIdx + 5, iEndIdx - iStartIdx - 5);
            }

            //--- Card Issue Name
            iStartIdx = szData.IndexOf((char)0x1C + "D2" + (char)0x00);
            if (iStartIdx >= 0)
            {
                iEndIdx = szData.IndexOf((char)0x1c, iStartIdx + 1);
                xCreditCardInfo.szCardIssueName = szData.Substring(iStartIdx + 5, iEndIdx - iStartIdx - 5);
            }

            //--- Reference Number
            iStartIdx = szData.IndexOf((char)0x1C + "D3" + (char)0x00);
            if (iStartIdx >= 0)
            {
                iEndIdx = szData.IndexOf((char)0x1c, iStartIdx + 1);
                xCreditCardInfo.szReferenceNo = szData.Substring(iStartIdx + 5, iEndIdx - iStartIdx - 5);
            }

            //--- Card Issue ID
            iStartIdx = szData.IndexOf((char)0x1C + "D4" + (char)0x00);
            if (iStartIdx >= 0)
            {
                iEndIdx = szData.IndexOf((char)0x1c, iStartIdx + 1);
                xCreditCardInfo.szCardIssueID = szData.Substring(iStartIdx + 5, iEndIdx - iStartIdx - 5);
            }

            //--- Card Holder Name
            iStartIdx = szData.IndexOf((char)0x1C + "D5" + (char)0x00);
            if (iStartIdx >= 0)
            {
                iEndIdx = szData.IndexOf((char)0x1c, iStartIdx + 1);
                xCreditCardInfo.szCardHolderName = szData.Substring(iStartIdx + 5, iEndIdx - iStartIdx - 5);
            }

            if (xCreditCardInfo.szResponseText == null)
                xCreditCardInfo.szResponseText = "";

            return xCreditCardInfo;
        }

        public static string AES_CBC_EncryptString(string message, string KeyString, string IVString)
        {
            byte[] Key  = UTF8Encoding.UTF8.GetBytes(KeyString).Take(32).ToArray();
            if (Key.Length < 32)
                throw new Exception("KeyString not at least 32 bytes.");
            byte[] IV   = UTF8Encoding.UTF8.GetBytes(IVString).Take(16).ToArray();
            if (Key.Length < 16)
                throw new Exception("IVString not at least 16 bytes.");
            string encrypted    = null;
            RijndaelManaged rj  = new RijndaelManaged();
            rj.KeySize          = 256;
            rj.BlockSize        = 128;
            rj.Key              = Key;
            rj.IV               = IV;
            rj.Mode             = CipherMode.CBC;
            rj.Padding          = PaddingMode.PKCS7;
            try
            {
                MemoryStream ms = new MemoryStream();

                using (CryptoStream cs = new CryptoStream(ms, rj.CreateEncryptor(Key, IV), CryptoStreamMode.Write))
                {
                    using (StreamWriter sw = new StreamWriter(cs))
                    {
                        sw.Write(message);
                        sw.Close();
                    }
                    cs.Close();
                }
                byte[] encoded  = ms.ToArray();
                encrypted       = Convert.ToBase64String(encoded);

                ms.Close();
            }
            catch (CryptographicException e)
            {
                Console.WriteLine("A Cryptographic error occurred: {0}", e.Message);
                LogInfo.WriteLogEDC(LogLevel.ERROR, $"- Error AES_CBC_EncryptString (V10) : CryptographicException {e.Message}");
                return null;
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine("A file error occurred: {0}", e.Message);
                LogInfo.WriteLogEDC(LogLevel.ERROR, $"- Error AES_CBC_EncryptString (V10) : UnauthorizedAccessException {e.Message}");
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: {0}", e.Message);
                LogInfo.WriteLogEDC(LogLevel.ERROR, $"- Error AES_CBC_EncryptString (V10) : {e.Message}");
                LogInfo.WriteLogEDC(LogLevel.DEBUG, "+ AES_CBC_EncryptString (V10) : stack=" + e.StackTrace);
            }
            finally
            {
                rj.Clear();
            }
            return encrypted;
        }

    }
}
