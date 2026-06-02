using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TCT_App.EMV.V0;

namespace TCT_App.EMV.EMV_V1
{
    public class ClassLibEDC_V1
    {
        public static byte[] MergeArrayBytes(byte[] bMsg1, byte[] bMsg2)
        {
            byte[] byteMsg  = new byte[bMsg1.Length + bMsg2.Length];
            int iTotalBytes = bMsg1.Length + bMsg2.Length;
            Array.Copy(bMsg1, byteMsg, bMsg1.Length);
            Array.Copy(bMsg2, 0, byteMsg, bMsg1.Length, bMsg2.Length);

            return byteMsg;
        }

        private static byte[] GenLenBCD_V1(int iLen)
        {
            char[] chars    = iLen.ToString("0000").ToCharArray();
            int len         = chars.Length / 2;
            byte[] bytes    = new byte[len];
            string szBCD = "";
            for (int i = 1; i >= 0; i--)
            {
                byte highNibble = byte.Parse(chars[2 * (len - 1) - 2 * i].ToString());
                byte lowNibble  = byte.Parse(chars[2 * (len - 1) - 2 * i + 1].ToString());
                bytes[i]        = (byte)((byte)(highNibble << 4) | lowNibble);
                szBCD           += GenHexChar(bytes[i]);
            }
            Array.Reverse(bytes);
            return bytes;
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
                bytes[i]        = (byte)((byte)(highNibble << 4) | lowNibble);
                szBCD           += GenHexChar(bytes[i]);
            }
            return szBCD;
        }

        public static char GenHexChar(int ch)
        {
            return (char)ch;
        }

        public static byte[] GenByteHexChar(int ch)
        {
            return BitConverter.GetBytes(ch);
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

        public static string GenHexCharList(byte[] bytes)
        {
            string szStr = "";
            foreach (Byte b in bytes)
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

        public static byte CheckSumLRC(byte[] bMsg)
        {
            byte lrc = 0x0;
            for (int i = 0; i < bMsg.Length; i++)
                lrc ^= bMsg[i];

            return lrc;
        }

        public static byte[] ConvertStringBytesToByteArray(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if (hexString.Length % 2 != 0)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The binary key cannot have an odd number of digits: {0}", hexString));
            }

            byte[] data = new byte[hexString.Length / 2];
            for (int index = 0; index < data.Length; index++)
            {
                string byteValue = hexString.Substring(index * 2, 2);
                data[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return data;

        }

        //--------------------------
        public static bool GeneratePaymentEdcMessage_V1 (decimal fPayAmount, string szRef1, string szRef2, string szRef3, string szRef4, ref byte[] byteMsg, ref string szDataMsg, ref string szErrMsg)
        {
            try
            {
                string szAmount     = fPayAmount.ToString("#,##0.00").Replace(",", "");
                byte[] byteSTX      = { 0x02 };
                byte[] bMsgData     = GenMessagePaymentData_V1(szAmount, szRef1, szRef2, szRef3, szRef4);
                byte[] bLenBCD      = GenLenBCD_V1(bMsgData.Length);
                byte[] byteETX      = { 0x03 };

                byteMsg = MergeArrayBytes(byteSTX, bLenBCD);
                byteMsg = MergeArrayBytes(byteMsg, bMsgData);
                byteMsg = MergeArrayBytes(byteMsg, byteETX);

                byte[] bChkSumCRC   = { CheckSumLRC(byteMsg) };
                byteMsg             = MergeArrayBytes(byteMsg, bChkSumCRC);

                string szCharList = GenHexCharList(byteMsg);
                Console.WriteLine("+ EDC Payment : " + szCharList);
                //LogInfo.WriteLogInfo("+ Send String : " + szMsg);
                Console.WriteLine("Byte Data : " + szCharList);
                szDataMsg           = szCharList;

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error GeneratePaymentEdcMessage_V1 : " + err.Message);
                //LogInfo.WriteLogEDC("- Error GeneratePaymentEdcMessage : " + err.Message);
                szErrMsg = err.Message;

                return false;
            }
        }

        private static byte[] GenMessagePaymentData_V1(string szAmount, string szRef1, string szRef2, string szRef3, string szRef4)
        {
            string szMsg        = "";
            //--- Transport Header
            string szTransportHeader = "";
            //szTransportHeader += "60";      // Transport Header Type;
            szTransportHeader   += "00";      // Transport Header Type;
            szTransportHeader   += "0000";    // Transport Destination
            szTransportHeader   += "0000";    // Transport Source
            szMsg               += GenHexString(szTransportHeader);

            //--- Presentation Header
            string szPresentHeader = "";
            szPresentHeader     += "1";         // Format version
            szPresentHeader     += "0";         // Request-Response Indication (0=This message is a request message, 1=This message is a response message, 2=This message is request which does not require a response
            szPresentHeader     += "20";        // Transaction Code (20=Purchase)
            szPresentHeader     += "00";        // Response Code
            szPresentHeader     += "0";         // More indicator (0=This is last message for this transaction, 1=There is another message following this one with more field data for this transaction.
            szMsg               += GenHexString(szPresentHeader);
            szMsg               += GenHexChar(0x1C).ToString();   // Field Separator
           
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
            szFieldDataFmt = "";
            szFieldDataFmt          += GenHexString("A1");

            Console.WriteLine("Send Ref1 = " + szRef1);
            szFieldDataFmt          += GenLenBCD(szRef1.Length);
            szFieldDataFmt          += szRef1;
            szMsg                   += GenHexString(szFieldDataFmt);
            szMsg                   += GenHexChar(0x1C).ToString();

            //--- Field Ref2 (Field Type=A2) TID + TimeStamp
            szFieldDataFmt          = "";
            szFieldDataFmt          += GenHexString("A2");

            Console.WriteLine("Send Ref2 = " + szRef2);
            szFieldDataFmt          += GenLenBCD(szRef2.Length);
            szFieldDataFmt          += szRef2;
            szMsg                   += GenHexString(szFieldDataFmt);
            szMsg                   += GenHexChar(0x1C).ToString();

/*
            //--- Field Ref3 (Field Type=A3) DateTime BOJ
            if (szRef3.Trim() != "")
            {
                Console.WriteLine("Send Ref3 = " + szRef3);
                szFieldDataFmt          += GenLenBCD(szRef3.Length);
                szFieldDataFmt          += szRef3;
                szMsg                   += GenHexString(szFieldDataFmt);
                szMsg                   += GenHexChar(0x1C).ToString();
            }

            //--- Field Ref4 (Field Type=A4)
            if (szRef4.Trim() != "")
            {
                Console.WriteLine("Send Ref4 = " + szRef4);
                szFieldDataFmt          += GenLenBCD(szRef4.Length);
                szFieldDataFmt          += szRef4;
                szMsg                   += GenHexString(szFieldDataFmt);
                szMsg                   += GenHexChar(0x1C).ToString();
            }
*/

            Console.WriteLine($"+ EMV Payment : Ref1={szRef1}, Ref2={szRef2}, Ref3={szRef3}, Ref4={szRef4}");

            Encoding enc    = new ASCIIEncoding();
            byte[] bMsg     = enc.GetBytes(szMsg);

            return bMsg;
        }

        //---------------------------------------------------------
        public static bool GenerateCancelEdcMessage_V1 (ref byte[] byteMsg, ref string szDataMsg, ref string szErrMsg)
        {
            try
            {
                byte[] byteSTX      = { 0x02 };
                byte[] bMsgData     = GenMessageCancelData_V1();
                byte[] bLenBCD      = GenLenBCD_V1(bMsgData.Length);
                byte[] byteETX      = { 0x03 };

                byteMsg = MergeArrayBytes(byteSTX, bLenBCD);
                byteMsg = MergeArrayBytes(byteMsg, bMsgData);
                byteMsg = MergeArrayBytes(byteMsg, byteETX);

                byte[] bChkSumCRC   = { CheckSumLRC(byteMsg) };
                byteMsg             = MergeArrayBytes(byteMsg, bChkSumCRC);

                string szCharList = GenHexCharList(byteMsg);
                Console.WriteLine("+ EDC Cancel : " + szCharList);
                //LogInfo.WriteLogInfo("+ Send String : " + szMsg);
                Console.WriteLine("Byte Data : " + szCharList);
                szDataMsg = szCharList;

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error GeneratCancelEdcMessage_V1 : " + err.Message);
                //LogInfo.WriteLogEDC("- Error GeneratePaymentEdcMessage : " + err.Message);
                szErrMsg = err.Message;

                return false;
            }
        }

        private static byte[] GenMessageCancelData_V1()
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

            Encoding enc    = new ASCIIEncoding();
            byte[] bMsg     = enc.GetBytes(szMsg);

            return bMsg;
        }

        //----------------------------------------------------------------------------------
        public static bool GenerateSumReporttEdcMessage_V1 (string szRef1, string szRef2, string szRef3, ref byte[] byteMsg, ref string szDataMsg, ref string szErrMsg)
        {
            try
            {
                byte[] byteSTX      = { 0x02 };
                byte[] bMsgData     = GenMessageSumReportData_V1(szRef1, szRef2, szRef3);
                byte[] bLenBCD      = GenLenBCD_V1(bMsgData.Length);
                byte[] byteETX      = { 0x03 };

                byteMsg = MergeArrayBytes(byteSTX, bLenBCD);
                byteMsg = MergeArrayBytes(byteMsg, bMsgData);
                byteMsg = MergeArrayBytes(byteMsg, byteETX);

                byte[] bChkSumCRC   = { CheckSumLRC(byteMsg) };
                byteMsg             = MergeArrayBytes(byteMsg, bChkSumCRC);

                string szCharList   = GenHexCharList(byteMsg);
                Console.WriteLine("+ EDC SumReport : " + szCharList);
                //LogInfo.WriteLogInfo("+ Send String : " + szMsg);
                Console.WriteLine("Byte Data : " + szCharList);
                szDataMsg           = szCharList;

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error GenerateSumReporttEdcMessage_V1 : " + err.Message);
                //LogInfo.WriteLogEDC("- Error GeneratePaymentEdcMessage : " + err.Message);
                szErrMsg = err.Message;

                return false;
            }
        }

        private static byte[] GenMessageSumReportData_V1(string szRef1, string szRef2, string szRef3)
        {
            string szMsg        = "";
            //--- Transport Header
            string szTransportHeader = "";
            //szTransportHeader += "60";      // Transport Header Type;
            szTransportHeader   += "00";      // Transport Header Type;
            szTransportHeader   += "0000";    // Transport Destination
            szTransportHeader   += "0000";    // Transport Source
            szMsg               += GenHexString(szTransportHeader);

            //--- Presentation Header
            string szPresentHeader = "";
            szPresentHeader     += "1";         // Format version
            szPresentHeader     += "0";         // Request-Response Indication (0=This message is a request message, 1=This message is a response message, 2=This message is request which does not require a response
            szPresentHeader     += "S1";        // Transaction Code (S1=Sum Report)
            szPresentHeader     += "00";        // Response Code
            szPresentHeader     += "0";         // More indicator (0=This is last message for this transaction, 1=There is another message following this one with more field data for this transaction.
            szMsg               += GenHexString(szPresentHeader);
            szMsg               += GenHexChar(0x1C).ToString();   // Field Separator
           
            //------------------------------------------------------------------------------
            //--- Field Ref1 (Field Type=A1) PlazaId+LaneName+JobNo+nTrx
            string                  szFieldDataFmt = "";
            szFieldDataFmt          += GenHexString("A1");

            Console.WriteLine("Send Ref1 = " + szRef1);
            szFieldDataFmt          += GenLenBCD(szRef1.Length);
            szFieldDataFmt          += szRef1;
            szMsg                   += GenHexString(szFieldDataFmt);
            szMsg                   += GenHexChar(0x1C).ToString();

            //--- Field Ref2 (Field Type=A2) TID + TimeStamp
            szFieldDataFmt          = "";
            szFieldDataFmt          += GenHexString("A2");

            Console.WriteLine("Send Ref2 = " + szRef2);
            szFieldDataFmt          += GenLenBCD(szRef2.Length);
            szFieldDataFmt          += szRef2;
            szMsg                   += GenHexString(szFieldDataFmt);
            szMsg                   += GenHexChar(0x1C).ToString();

            Console.WriteLine($"+ EMV SumReport : Ref1={szRef1}, Ref2={szRef2}, Ref3={szRef3}");

            Encoding enc    = new ASCIIEncoding();
            byte[] bMsg     = enc.GetBytes(szMsg);

            return bMsg;
        }

        //----------------------------------------------------------------------------------
        public static bool GenerateHandshakingEdcMessage_V1 (ref byte[] byteMsg, ref string szDataMsg, ref string szErrMsg)
        {
            try
            {
                byte[] byteSTX      = { 0x02 };
                byte[] bMsgData     = GenMessageHandshakingData_V1();
                byte[] bLenBCD      = GenLenBCD_V1(bMsgData.Length);
                byte[] byteETX      = { 0x03 };

                byteMsg = MergeArrayBytes(byteSTX, bLenBCD);
                byteMsg = MergeArrayBytes(byteMsg, bMsgData);
                byteMsg = MergeArrayBytes(byteMsg, byteETX);

                byte[] bChkSumCRC   = { CheckSumLRC(byteMsg) };
                byteMsg             = MergeArrayBytes(byteMsg, bChkSumCRC);

                string szCharList   = GenHexCharList(byteMsg);
                Console.WriteLine("+ EDC Handshaking : " + szCharList);
                //LogInfo.WriteLogInfo("+ Send String : " + szMsg);
                Console.WriteLine("Byte Data : " + szCharList);
                szDataMsg           = szCharList;

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error GenerateHandshakingEdcMessage_V1 : " + err.Message);
                //LogInfo.WriteLogEDC("- Error GeneratePaymentEdcMessage : " + err.Message);
                szErrMsg = err.Message;

                return false;
            }
        }

        private static byte[] GenMessageHandshakingData_V1()
        {
            string szMsg        = "";
            //--- Transport Header
            string szTransportHeader = "";
            //szTransportHeader += "60";      // Transport Header Type;
            szTransportHeader   += "00";      // Transport Header Type;
            szTransportHeader   += "0000";    // Transport Destination
            szTransportHeader   += "0000";    // Transport Source
            szMsg               += GenHexString(szTransportHeader);

            //--- Presentation Header
            string szPresentHeader = "";
            szPresentHeader     += "1";         // Format version
            szPresentHeader     += "0";         // Request-Response Indication (0=This message is a request message, 1=This message is a response message, 2=This message is request which does not require a response
            szPresentHeader     += "H1";        // Transaction Code (H1=Handshakingg)
            szPresentHeader     += "00";        // Response Code
            szPresentHeader     += "0";         // More indicator (0=This is last message for this transaction, 1=There is another message following this one with more field data for this transaction.
            szMsg               += GenHexString(szPresentHeader);
            szMsg               += GenHexChar(0x1C).ToString();   // Field Separator
           
            //------------------------------------------------------------------------------

            Encoding enc    = new ASCIIEncoding();
            byte[] bMsg     = enc.GetBytes(szMsg);

            return bMsg;
        }

        //---------------------------------------------------------
        public static bool GenerateGenQrCodeEdcMessage_V1 (decimal fPayAmount, string szRef1, string szRef2, ref byte[] byteMsg, ref string szDataMsg, ref string szErrMsg)
        {
            try
            {
                string szAmount     = fPayAmount.ToString("#,##0.00").Replace(",", "");
                byte[] byteSTX      = { 0x02 };
                byte[] bMsgData     = GenMessageGenQrCodeData_V1(szAmount, szRef1, szRef2);
                byte[] bLenBCD      = GenLenBCD_V1(bMsgData.Length);
                byte[] byteETX      = { 0x03 };

                byteMsg = MergeArrayBytes(byteSTX, bLenBCD);
                byteMsg = MergeArrayBytes(byteMsg, bMsgData);
                byteMsg = MergeArrayBytes(byteMsg, byteETX);

                byte[] bChkSumCRC   = { CheckSumLRC(byteMsg) };
                byteMsg             = MergeArrayBytes(byteMsg, bChkSumCRC);

                string szCharList = GenHexCharList(byteMsg);
                Console.WriteLine("+ EDC GenQR : " + szCharList);
                //LogInfo.WriteLogInfo("+ Send String : " + szMsg);
                Console.WriteLine("Byte Data : " + szCharList);
                szDataMsg           = szCharList;

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error GenerateGenQrCodeEdcMessage_V1 : " + err.Message);
                //LogInfo.WriteLogEDC("- Error GeneratePaymentEdcMessage : " + err.Message);
                szErrMsg = err.Message;

                return false;
            }
        }

        private static byte[] GenMessageGenQrCodeData_V1(string szAmount, string szRef1, string szRef2)
        {
            string szMsg        = "";
            //--- Transport Header
            string szTransportHeader = "";
            //szTransportHeader += "60";      // Transport Header Type;
            szTransportHeader   += "00";      // Transport Header Type;
            szTransportHeader   += "0000";    // Transport Destination
            szTransportHeader   += "0000";    // Transport Source
            szMsg               += GenHexString(szTransportHeader);

            //--- Presentation Header
            string szPresentHeader = "";
            szPresentHeader     += "1";         // Format version
            szPresentHeader     += "0";         // Request-Response Indication (0=This message is a request message, 1=This message is a response message, 2=This message is request which does not require a response
            szPresentHeader     += "QR";        // Transaction Code (QR=Sales QR)
            szPresentHeader     += "00";        // Response Code
            szPresentHeader     += "0";         // More indicator (0=This is last message for this transaction, 1=There is another message following this one with more field data for this transaction.
            szMsg               += GenHexString(szPresentHeader);
            szMsg               += GenHexChar(0x1C).ToString();   // Field Separator
           
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
            szFieldDataFmt = "";
            szFieldDataFmt          += GenHexString("A1");

            Console.WriteLine("Send Ref1 = " + szRef1);
            szFieldDataFmt          += GenLenBCD(szRef1.Length);
            szFieldDataFmt          += szRef1;
            szMsg                   += GenHexString(szFieldDataFmt);
            szMsg                   += GenHexChar(0x1C).ToString();

            //--- Field Ref2 (Field Type=A2) TID + TimeStamp
            szFieldDataFmt          = "";
            szFieldDataFmt          += GenHexString("A2");

            Console.WriteLine("Send Ref2 = " + szRef2);
            szFieldDataFmt          += GenLenBCD(szRef2.Length);
            szFieldDataFmt          += szRef2;
            szMsg                   += GenHexString(szFieldDataFmt);
            szMsg                   += GenHexChar(0x1C).ToString();

            Console.WriteLine($"+ EMV QR Code : Amount={fAmount}, Ref1={szRef1}, Ref2={szRef2}");

            Encoding enc    = new ASCIIEncoding();
            byte[] bMsg     = enc.GetBytes(szMsg);

            return bMsg;
        }

        //---------------------------------------------------------
        public static bool GenerateInquiryQrEdcMessage_V1 (string szInvoiceNo, ref byte[] byteMsg, ref string szDataMsg, ref string szErrMsg)
        {
            try
            {
                byte[] byteSTX      = { 0x02 };
                byte[] bMsgData     = GenMessageInquiryQrData_V1(szInvoiceNo);
                byte[] bLenBCD      = GenLenBCD_V1(bMsgData.Length);
                byte[] byteETX      = { 0x03 };

                byteMsg = MergeArrayBytes(byteSTX, bLenBCD);
                byteMsg = MergeArrayBytes(byteMsg, bMsgData);
                byteMsg = MergeArrayBytes(byteMsg, byteETX);

                byte[] bChkSumCRC   = { CheckSumLRC(byteMsg) };
                byteMsg             = MergeArrayBytes(byteMsg, bChkSumCRC);

                string szCharList = GenHexCharList(byteMsg);
                Console.WriteLine("+ EDC InquiryQR : " + szCharList);
                //LogInfo.WriteLogInfo("+ Send String : " + szMsg);
                Console.WriteLine("Byte Data : " + szCharList);
                szDataMsg           = szCharList;

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error GenerateInquiryQrEdcMessage_V1 : " + err.Message);
                //LogInfo.WriteLogEDC("- Error GeneratePaymentEdcMessage : " + err.Message);
                szErrMsg = err.Message;

                return false;
            }
        }

        private static byte[] GenMessageInquiryQrData_V1(string szInvoiceNo)
        {
            string szMsg        = "";
            //--- Transport Header
            string szTransportHeader = "";
            //szTransportHeader += "60";      // Transport Header Type;
            szTransportHeader   += "00";      // Transport Header Type;
            szTransportHeader   += "0000";    // Transport Destination
            szTransportHeader   += "0000";    // Transport Source
            szMsg               += GenHexString(szTransportHeader);

            //--- Presentation Header
            string szPresentHeader = "";
            szPresentHeader     += "1";         // Format version
            szPresentHeader     += "0";         // Request-Response Indication (0=This message is a request message, 1=This message is a response message, 2=This message is request which does not require a response
            szPresentHeader     += "IQ";        // Transaction Code (IQ=Inquiry QR)
            szPresentHeader     += "00";        // Response Code
            szPresentHeader     += "0";         // More indicator (0=This is last message for this transaction, 1=There is another message following this one with more field data for this transaction.
            szMsg               += GenHexString(szPresentHeader);
            szMsg               += GenHexChar(0x1C).ToString();   // Field Separator
           
            //--- Field InvoiceNo (Field Type=65)
            string szFieldDataFmt   = "";
            szFieldDataFmt          += GenHexString("65");      // InvoiceNo
            szInvoiceNo             = "000000";                 // 000000=Current InvoiceNo
            szFieldDataFmt          += GenLenBCD(szInvoiceNo.Length);
            szFieldDataFmt          += szInvoiceNo;
            szMsg                   += GenHexString(szFieldDataFmt);
            szMsg                   += GenHexChar(0x1C).ToString();

            Console.WriteLine($"+ EMV InquiryQR : InvoiceNo={szInvoiceNo}");

            Encoding enc    = new ASCIIEncoding();
            byte[] bMsg     = enc.GetBytes(szMsg);

            return bMsg;
        }

        //-------------------------------------------
        public static string GetResponseMsg(string szCode)
        {
            switch (szCode)
            {
/*
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
*/
                case "00": return "Complete";
                case "ND": return "TXN CANCEL";
                case "TX": return "TXN CANCEL";
                case "EN": return "เชื่อมต่อไป  host ไม่ได้";
                case "NA": return "EDC ไม่พร้อมใช้งาน";
                case "E1": return "บัตรถูกปฏิเสธจากธนาคาร";
                case "E2": return "บัตรหมดอายุ";
                case "E3": return "บัตรถูกอายัดจากธนาคาร";
                case "E4": return "บัตรติด Denylist";
                case "RS": return "EDC Reversal สำเร็จ";
                case "R1": return "EDC อยู่ใน mode Reversal";
                case "C0": return "EDC reader เปิดใช้งานอยู่";
                case "C1": return "EDC reader ไม่เปิดใช้งาน";
                case "12": return "TX NOT FOUND";
                case "E5":
                case "50":
                case "51":
                case "52":
                case "53":
                case "54":
                case "55":
                case "56":
                case "57":
                case "58":
                case "59": return "อ่านบัตรไม่สำเร็จ";
                default: return szCode;
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
                case "E5":
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
    }
}
