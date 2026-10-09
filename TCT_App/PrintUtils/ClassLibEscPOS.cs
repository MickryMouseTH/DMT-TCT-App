using System;
using System.Drawing;
using System.Linq;
using System.Text;
using QRCoder;

namespace TCT_App.PrintUtils
{
    public class ClassEscPOS
    {
        private static int iMaxCharCount    = 42;
        private static char ESC_CHAR        = '\x1B';
        private static char GS              = '\x1D';

        public enum Alignment
        {
            LeftAlignment   = 0,
            CenterAlignment = 1,
            RightAlignment  = 2,
        }

        public class ThaiEscObj
        {
            public string szLine1;      // Upper letter
            public string szLine2;      // Middle letter
            public string szLine3;      // Lower letter;
        }


        public static char[] cmd_FontBold = {'\x1B', 'E', '1'};

        //--- v2.0.4: printable form of an ESC/POS string for TRACE log (control bytes -> <XX>, newline -> \n)
        public static string ToLogText(string szData)
        {
            if (szData == null)
                return "null";
            var sb = new StringBuilder(szData.Length + 32);
            foreach (char c in szData)
            {
                if (c == '\n')
                    sb.Append("\\n");
                else if (c == '\r')
                    sb.Append("\\r");
                else if (c < 0x20 || c == 0x7F)
                    sb.Append('<').Append(((int)c).ToString("X2")).Append('>');
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        public static string InitPrint()
        {
            string szInit = "";
            char[] cmd_Init = {'\x1B', '\x40'};
            szInit += new string(cmd_Init);

            //char[] cmd_FontThai = { '\x1B', '\x1D', '\x74', '\x15' };
            char[] cmd_FontThai = { '\x1B', 't', '\x15' };
            szInit += new string(cmd_FontThai);

            //char[] cmd_UseItalic = {'\x1B', '>' };
            //szInit += new string(cmd_UseItalic);

            //char[] cmd_LineSpacing = { '\x1B', '\x33', '\'}

            return szInit;
        }

        public static string SetCodePageThai()
        {
            //char[] cmd_FontThai = { '\x1B', '\x1D', '\x74', '\x15' };
            char[] cmd_FontThai = { '\x1B', 't', '\x1B' };
            return new string(cmd_FontThai);
        }

        public static string SetFontBold(int iBold)
        {
            char[] cmd_FontBold = {'\x1B', 'E', iBold == 0 ? '0' : '\x1'};
            return new string(cmd_FontBold);
        }

        public static string SetFontUnderline(int iUnderline)
        {
            char[] cmd_FontUnderline = {'\x1B', '\x2D', iUnderline == 0 ? '0' : '\x1'};
            return new string(cmd_FontUnderline);
        }

        public static string SetFontItalic(int iItalic)
        {
            char[] cmd_FontItalic = { '\x1B', '\x34', iItalic == 0 ? '0' : '\x1' };
            return new string(cmd_FontItalic);
        }

        public static string SetFontSize(int iFontSize)
        {
            char[] cmd_FontSize = {'\x1B', '\x21', iFontSize == 0 ? '0' : (char)iFontSize};
            iMaxCharCount = iFontSize == 1 ? 56 : 42;
            return new string(cmd_FontSize);
        }

        public static string ResetFont()
        {
            char[] cmd_ResetFont = { '\x1B', '\x21', '\x2' };
            return new string(cmd_ResetFont);
        }

        public static string SetFontLarge()
        {
            char[] cmd_FontSize = { '\x1B', '\x21', '\x10' };
            return new string(cmd_FontSize);
        }


        public static string SetFontAlignment(int iAlignment)
        {
            char[] cmd_FontAlignment = { '\x1B', '\x61', (iAlignment == 0) ? '0' : (char)iAlignment };
            return new string(cmd_FontAlignment);
        }

        public static string SetLineSpacing(int iLineSpacing)
        {
            char[] cmd_LineSpace = { '\x1B', '\x33', iLineSpacing == 0 ? '0' : (char)iLineSpacing };
            return new string(cmd_LineSpace);
        }

        public static string SetDefaultLineSpacing()
        {
            char[] cmd_LineSpace = { '\x1B', '\x32' };
            return new string(cmd_LineSpace);
        }

        public static string FeedLine(int iLine)
        {
            if (iLine > 0)
            {
                char[] cmd_FeedLine = { '\x1B', '\x64', (char)iLine };
                return new string(cmd_FeedLine);
            }
            else
                return "";
        }

        public static string CutPaper()
        {
            char[] cmd_CutPaper = {'\x1D', '\x56', '0' };
            return new string(cmd_CutPaper);
        }

        public static int GetPrintStringCount(string szStr)
        {
            int iLen = szStr.Length;
            for (int i = 0; i < szStr.Length; i++)
            {
                int iChar = szStr[i] - 0xD60;
                if ((iChar == 0xD1) ||
                    ((iChar >= 0xD4) && (iChar <= 0xDA)) ||
                    ((iChar >= 0xE7) && (iChar <= 0xF0)))
                    iLen--;
            }
            return iLen;
        }

        public static string ConvertStringPrint2(string szStr)
        {
            var xThaiObj = ParseThaiString2(szStr);
            return SetLineSpacing(1)+ xThaiObj.szLine1 + xThaiObj.szLine2 + xThaiObj.szLine3+SetDefaultLineSpacing();
        }

        //---------------------------------------------------------------
        public static ThaiEscObj ParseThaiString2(string szTxt)
        {
            var xThaiObj    = new ThaiEscObj();
            string szLine1 = "", szLine2 = "", szLine3 = "";
            string szChar = "";
            for (int i=0; i<szTxt.Length; i++)
            {
                if (i>=szTxt.Length)
                    break;
                int iChar = szTxt[i];
                if (iChar <= 0x7F)
                {
                    szLine1 += " ";
                    szLine2 += (char)(iChar);
                    szLine3 += " ";
                }
                else
                {
                    iChar -= 0xD60;
                    switch (iChar)
                    {
                        case '\xD1':
                        case '\xD4':
                        case '\xD5':
                        case '\xD6':
                        case '\xD7':
                            if (i == szTxt.Length - 1)  // End Text
                            {
                                szChar = ((char)(iChar + 0xD60)).ToString();
                                szLine1 = szLine1.Remove(szLine1.Length - 1);
                                szLine1 += szChar;
                            }
                            else
                            {
                                int iNextChar = szTxt[i + 1];
                                iNextChar -= 0xD60;
                                int iNewChar = 0x00;
                                switch (iNextChar)
                                {
                                    case '\xE8':
                                        if (iChar == '\xD1')
                                            iNewChar = '\x92';
                                        else if (iChar == '\xD4')
                                            iNewChar = '\x96';
                                        else if (iChar == '\xD5')
                                            iNewChar = '\x9B';
                                        else if (iChar == '\xD6')
                                            iNewChar = '\xDB';
                                        else if (iChar == '\xD7')
                                            iNewChar = '\xFB';
                                        if (iNewChar != 0x00)
                                        {
                                            szChar = ((char)(iNewChar + 0xD60)).ToString();
                                            szLine1 = szLine1.Remove(szLine1.Length - 1);
                                            //szLine1 += szChar;
                                            i++;
                                        }
                                        break;
                                    case '\xE9':
                                        if (iChar == '\xD1')
                                            iNewChar = '\x93';
                                        else if (iChar == '\xD4')
                                            iNewChar = '\x97';
                                        else if (iChar == '\xD5')
                                            iNewChar = '\x9C';
                                        else if (iChar == '\xD6')
                                            iNewChar = '\xDC';
                                        else if (iChar == '\xD7')
                                            iNewChar = '\xFC';
                                        if (iNewChar != 0x00)
                                        {
                                            szChar = ((char)(iNewChar + 0xD60)).ToString();
                                            szLine1 = szLine1.Remove(szLine1.Length - 1);
                                            //szLine1 += szChar;
                                            i++;
                                        }
                                        break;
                                    case '\xEA':
                                        if (iChar == '\xD1')
                                            iNewChar = '\x94';
                                        else if (iChar == '\xD4')
                                            iNewChar = '\x98';
                                        else if (iChar == '\xD5')
                                            iNewChar = '\x9D';
                                        else if (iChar == '\xD6')
                                            iNewChar = '\xDD';
                                        else if (iChar == '\xD7')
                                            iNewChar = '\xFD';
                                        if (iNewChar != 0x00)
                                        {
                                            szChar = ((char)(iNewChar + 0xD60)).ToString();
                                            szLine1 = szLine1.Remove(szLine1.Length - 1);
                                            //szLine1 += szChar;
                                            i++;
                                        }
                                        break;
                                    case '\xEB':
                                        if (iChar == '\xD1')
                                            iNewChar = '\x95';
                                        else if (iChar == '\xD4')
                                            iNewChar = '\x99';
                                        else if (iChar == '\xD5')
                                            iNewChar = '\x9E';
                                        else if (iChar == '\xD6')
                                            iNewChar = '\xDE';
                                        else if (iChar == '\xD7')
                                            iNewChar = '\xFE';
                                        if (iNewChar != 0x00)
                                        {
                                            szChar = ((char)(iNewChar + 0xD60)).ToString();
                                            szLine1 = szLine1.Remove(szLine1.Length - 1);
                                            //szLine1 += szChar;
                                            i++;
                                        }
                                        szLine1 += szChar;
                                        break;
                                    case '\xEC':
                                    case '\xED':
                                        if (iChar == '\xD4')
                                        {
                                            iNewChar = '\x9A';
                                            if (iNewChar != 0x00)
                                            {
                                                szChar = ((char)(iNewChar + 0xD60)).ToString();
                                                szLine1 = szLine1.Remove(szLine1.Length - 1);
                                                //szLine1 += szChar;
                                                i++;
                                            }
                                        }
                                        break;
                                    default:
                                        szChar = ((char)(iChar + 0xD60)).ToString();
                                        szLine1 = szLine1.Remove(szLine1.Length - 1);
                                        szLine1 += szChar;
                                        break;
                                }
                            }
                            break;

                        case '\xE7':
                        case '\xE8':
                        case '\xE9':
                        case '\xEA':
                        case '\xEB':
                        case '\xEC':
                        case '\xED':
                            szChar = ((char)(iChar + 0xD60)).ToString();
                            szLine1 = szLine1.Remove(szLine1.Length - 1);
                            szLine1 += szChar;
                            break;

                        case '\xD8':
                        case '\xD9':
                            szChar = ((char)(iChar + 0xD60)).ToString();
                            szLine3 = szLine1.Remove(szLine3.Length - 1);
                            szLine3 += szChar;
                            break;

                        default:
                            szLine1 += " ";
                            szLine2 += (char)(iChar + 0xD60);
                            szLine3 += " ";
                            break;
                    }
                }
            }

            //xThaiObj.szLine1 = szLine1+@"\x0D\x0A";
            //xThaiObj.szLine2 = szLine2+@"\x0D";
            //xThaiObj.szLine3 = szLine3+@"\x0A";

            xThaiObj.szLine1 = szLine1 + @"\x0A";
            xThaiObj.szLine2 = szLine2 + @"\x0A";
            xThaiObj.szLine3 = szLine3 + @"\x0A";

            return xThaiObj;
        }

        public static string SelectBitImageMode()
        {
            char[] cmd_SelectBitImageMode = { ESC_CHAR, '\x55', '\x1' };
            return new string(cmd_SelectBitImageMode); ;
        }

        public static string RegisterLogo()
        {
            //char[] cmd_PrintLogo = { '\x1D', '\x40', '\x1B', '\x61', '\x1' };
            char[] cmd_PrintLogo = { '\x1C', '\x71',  '\x12', '\x0', '\x5', '\x0' };
            return new string(cmd_PrintLogo); ;
        }

        public static string PrintLogo()
        {
            //char[] cmd_PrintLogo = { '\x1D', '\x40', '\x1B', '\x61', '\x1' };
            //char[] cmd_PrintLogo = { '\x1C', '\x70', '\x01', '\x0' };
            char[] cmd_PrintLogo = { '\x1D', '\x28', '\x4C', '\x06', '\x00', '\x30', '\x45', '\x20', '\x20', '\x01', '\x01' };
            return new string(cmd_PrintLogo);
        }

        public static string Print2Column(string szStr1, string szStr2)
        {
            int iLeftCount  = GetPrintStringCount(szStr1);
            int iRightCount = GetPrintStringCount(szStr2);
            int iSpaceCount = iMaxCharCount - iLeftCount - iRightCount;
            String szStr    = szStr1 + " ".PadLeft(iSpaceCount) + szStr2 + "\n";

            return szStr;
        }

        private static string toHex(int number)
        {
            var hex = (Math.Abs(number)).ToString("X");
            while (hex.Length < 2)
            {
                hex = "0" + hex;
            }
            return "x" + hex.Substring(0, 2).ToUpper();
        }

        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // https://liana-ali.blogspot.com/2018/09/escpos-print-qr-code-in-receipt.html
        public static string PrintQrCode(string qrSource)
        {
            // char < 122 can print, char > 250 can print 
            // char > 122 and < 250  cannot print

            int iOrgQrLen = qrSource.Length;
            if ((qrSource.Length >= 122) && (qrSource.Length <= 255))
                qrSource += "&refno=" + RandomString(250 - qrSource.Length);
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ ClassEscPOS.PrintQrCode : len={iOrgQrLen} -> {qrSource.Length}{(iOrgQrLen != qrSource.Length ? " (padded &refno)" : "")}, QrSize={TCT_Lib.iPrinter_PrintQrSize}");

            LogInfo.WriteLog("+ Print QR Code : " + qrSource);

            char dots       = '\x08';
            int qrLen       = qrSource.Length + 3;

            byte store_pH   = (byte)(qrLen / 256);
            byte store_pL   = (byte)(qrLen - (256 * store_pH));
            byte store_K    = (byte)(store_pL + (store_pH * 256) - 3);

            /*
                        char[] cmd_PrintQr = { '\x1B', '\x40' };
                        szInit += new string(cmd_Init);

                        //char[] cmd_FontThai = { '\x1B', '\x1D', '\x74', '\x15' };
                        char[] cmd_FontThai = { '\x1B', 't', '\x15' };
                        szInit += new string(cmd_FontThai);

            */
            char[] cmd_Func180 = { '\x1D', '\x28', '\x6B', (char)store_pL, (char)store_pH, '\x31', '\x41', '\x32', '\x00' };     // Function 180 - Stores symbol data in the symbol storage area
            char[] cmd_Func169 = { '\x1D', '\x28', '\x6B', '\x03', '\x00', '\x31', '\x45','\x30' };             // Function 169 - Selects the error correction level for QR Code
            char[] cmd_Func167 = { '\x1D', '\x28', '\x6B', '\x03', '\x00', '\x31', '\x43', dots };              // Function 167 - Set Module Set of QR Code
            char[] cmd_Func165 = { '\x1D', '\x28', '\x6B', '\x04', '\x00', '\x31', '\x41', '\x32', '\x00' };    // Function 165 - Select QR Code Model
            char[] cmd_Func181 = { '\x1D', '\x28', '\x6B', '\x03', '\x00', '\x31', '\x51', '\x30' };            // Function 181 - Prints QR Code symbol data stored in the system saving region

            Encoding m_encoding = Encoding.GetEncoding("iso-8859-1"); //set encoding for QRCode
            //UTF8Encoding m_encoding = new UTF8Encoding();
            string buffer = "";
            /*
                        buffer += m_encoding.GetString(new byte[] { 29, 40, 107, 4, 0, 49, 65, 50, 0 });
                        buffer += m_encoding.GetString(new byte[] { 29, 40, 107, 3, 0, 49, 67, 8 });
                        buffer += m_encoding.GetString(new byte[] { 29, 40, 107, 3, 0, 49, 69, 48 });
                        buffer += m_encoding.GetString(new byte[] { 29, 40, 107, store_pL, store_pH, 49, 80, 48 });
                        buffer += qrSource;
                        buffer += m_encoding.GetString(new byte[] { 29, 40, 107, 3, 0, 49, 81, 48 });
            */


            //byte dotSize        = (byte)(qrSource.Length < 122 ? 0x06 : 0x03);
            byte dotSize = (byte)(qrSource.Length < 122 ? TCT_Lib.iPrinter_PrintQrSize + 3 : TCT_Lib.iPrinter_PrintQrSize);
            byte correctLevel   = 50;     // 48=L (7%), 49=M (15%), 50=Q (25%), 51=H (30%)
            buffer += m_encoding.GetString(new byte[] { 29, 40, 107, 4, 0, 49, 65, 50, 0 });    // Function 165 - Select QR Code Model
            buffer += m_encoding.GetString(new byte[] { 29, 40, 107, 3, 0, 49, 67, dotSize });        // Function 167 - Set Module Set of QR Code
            buffer += m_encoding.GetString(new byte[] { 29, 40, 107, 3, 0, 49, 69, correctLevel });       // Function 169 - Selects the error correction level for QR Code
            buffer += m_encoding.GetString(new byte[] { 29, 40, 107, store_pL, store_pH, 49, 80, 48 });
            //buffer += m_encoding.GetString(new byte[] { 29, 40, 107, 3, 0, 49, 80, 48 });
            buffer += qrSource;
            buffer += m_encoding.GetString(new byte[] { 29, 40, 107, 3, 0, 49, 81, 48 });       // Function 181 - Prints QR Code symbol data stored in the system saving region
            //buffer += m_encoding.GetString(new byte[] { 29, 40, 107, 3, 0, 49, 82, 48 });

            return buffer;
        }

        private static Image GenerateQrCodeImage(string szData)
        {
            QRCodeGenerator qrGen   = new QRCodeGenerator();
            QRCodeData qrCodeData   = qrGen.CreateQrCode(szData, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode           = new QRCode(qrCodeData);
            Bitmap qrCodeImage      = qrCode.GetGraphic(10);    // Default = 10
            return qrCodeImage;
        }

        public static string PrintImageQrCode(string szData)
        {
            Image imgQrCode = GenerateQrCodeImage(szData);
            byte[] width = BitConverter.GetBytes(imgQrCode.Width);

            ASCIIEncoding m_encoding = new ASCIIEncoding();
            string buffer = "";
            buffer += m_encoding.GetString(new byte[] { 29, 118, 48, 0,  });

            return "";
        }



    }
}
