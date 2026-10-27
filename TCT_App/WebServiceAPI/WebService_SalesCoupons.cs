using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TCT_App.EMV.V0;

namespace TCT_App.WebServiceAPI
{
    public class WebService_SalesCoupons
    {
        public class ObjGetCoupons
        {
            public string tsbId;
            public string userId;
            public string coupontype;
        }

        public class ObjCouponData
        {
            public int CouponPK;
            public string CouponType;
            public string SerialNo;
            public decimal Price;
        }

        public class ObjSoldCoupon
        {
            public string tsbid;
            public string plazaid;
            public string laneid;
            public string coupontype;
            public string serialno;
            public decimal price;
            public string userid;
            public string solddate;
            public int paytypeid;
            public string paytypename;
            public string edcdatetime;
            public string edcterminalid;
            public string edccardno;
            public decimal edcamount;
            public string edcref1;
            public string edcref2;
            public string edcref3;
        }

        //--- v2.0.7: staff the local SalesCouponList was downloaded for ("" = unknown, e.g. after restart)
        static string szCouponListStaffID = "";

        //--- v2.0.7: TA answered with an error (e.g. no coupon for this staff) -> empty list like 1.5.7.
        //---         No answer (network / timeout) -> keep the list (v2.0.4 J4) only when it is this staff's list,
        //---         a list of the previous staff must not be sold by the next one.
        static void ClearCouponList(string szReason, bool bKeepIfSameStaff)
        {
            if (bKeepIfSameStaff && (szCouponListStaffID != "") && (szCouponListStaffID == TCT_Lib.szStaffID))
            {
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ CallWebService_GetCoupons : {szReason} -> keep SalesCouponList of StaffID={szCouponListStaffID}");
                return;
            }
            int iRows = TCT_Lib.LaneDB.Execute("DELETE FROM SalesCouponList WHERE IFNULL(CouponStatus,0)=0");
            LogInfo.WriteLog($"+ CallWebService_GetCoupons : {szReason} -> clear unsold SalesCouponList (list StaffID='{szCouponListStaffID}', StaffID={TCT_Lib.szStaffID}), rows={iRows}");
            szCouponListStaffID = "";
        }

        //-----------------------------------
        public static bool CallWebService_GetCoupons(ref string szErrMsg)
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ CallWebService_GetCoupons : enable={TCT_Lib.bEnableLaneWebService}, StaffID={TCT_Lib.szStaffID}, TSBId={TCT_Lib.szTSBId}");
            if (!TCT_Lib.bEnableLaneWebService)
                return false;

            if (!TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szLaneWebService_ServerIPAddress, TCT_Lib.iLaneWebService_ServerPort))
            {
                Console.WriteLine($"Fail Connect LaneWebService IP={TCT_Lib.szLaneWebService_ServerIPAddress}, Port={TCT_Lib.iLaneWebService_ServerPort}");
                LogInfo.WriteLog($"Fail Connect LaneWebService IP={TCT_Lib.szLaneWebService_ServerIPAddress}, Port={TCT_Lib.iLaneWebService_ServerPort}");
                szErrMsg = $"Fail Connect LaneWebService IP={TCT_Lib.szLaneWebService_ServerIPAddress}, Port={TCT_Lib.iLaneWebService_ServerPort}";
                ClearCouponList("TA not reachable", true);
                return false;
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var InvC = System.Globalization.CultureInfo.InvariantCulture;
            try
            {
                //--- v2.0.4: the local list is replaced only after TA answered. Before, it was DELETEd first,
                //--- so a timeout / error from TA left the popup with no coupons at all.
                string szQuery      = "";
                int iRows           = 0;
                var xCoupons        = new ObjGetCoupons();
                xCoupons.userId     = TCT_Lib.szStaffID;
                xCoupons.tsbId      = TCT_Lib.szTSBId;

                string msg          = JsonConvert.SerializeObject(xCoupons, Formatting.Indented);
                byte[] byteData     = Encoding.UTF8.GetBytes(msg);
                LogInfo.WriteLog("+ Parameter : " + msg);
                //--------------------------------------------------------------------
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                using (var client = new WebClient())
                {
                    client.Encoding     = new UTF8Encoding();
                    string szFmt        = "{0}/api/users/coupons/search";
                    string szWebUrl     = string.Format(szFmt, TCT_Lib.szLaneWebService_BaseURL);
                    LogInfo.WriteLog("+ CallWebService_GetCoupons URL : " + szWebUrl);
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ CallWebService_GetCoupons : POST bytes={byteData.Length}, timeout=2000 ms");

                    var request                 = (HttpWebRequest)WebRequest.Create(szWebUrl);
                    request.Method              = "POST";
                    request.ContentType         = "application/json";
                    request.ContentLength       = byteData.Length;
                    request.Timeout             = 2 * 1000;
                    using (Stream webStream = request.GetRequestStream())
                    {
                        webStream.Write(byteData, 0, byteData.Length);
                        webStream.Flush();
                        webStream.Close();
                    }

                    using (WebResponse webResponse = request.GetResponse())
                    {
                        LogInfo.WriteLog(LogLevel.DEBUG, $"+ CallWebService_GetCoupons : status={(int)((HttpWebResponse)webResponse).StatusCode}, {sw.ElapsedMilliseconds} ms");
                        if (((HttpWebResponse)webResponse).StatusCode != HttpStatusCode.OK)
                        {
                            szErrMsg = ((HttpWebResponse)webResponse).StatusDescription;
                            LogInfo.WriteLog("- Error Response CallWebService_GetCoupons : " + szErrMsg);
                            ClearCouponList("HTTP " + (int)((HttpWebResponse)webResponse).StatusCode, true);

                            return false;
                        }

                        Stream webStream = webResponse.GetResponseStream();
                        if (webStream != null)
                        {
                            using (var responseReader = new StreamReader(webStream))
                            {
                                string response     = responseReader.ReadToEnd();
                                LogInfo.WriteLog("+ Response CallWebService_GetCoupons : " + response);

                                dynamic xResponse   = JsonConvert.DeserializeObject(response);
                                LogInfo.WriteLog(LogLevel.DEBUG, $"+ CallWebService_GetCoupons : hasError={xResponse?["errors"]?["hasError"]}, errMsg={xResponse?["errors"]?["errMsg"]}, {sw.ElapsedMilliseconds} ms");
                                if ((bool)xResponse["errors"]["hasError"] == true)
                                {
                                    szErrMsg = (string)xResponse["errors"]["errMsg"];
                                    LogInfo.WriteLog("- Fail CallWebService_GetCoupons : " + (string)xResponse["errors"]["errMsg"]);
                                    ClearCouponList("TA answered error", false);
                                    return false;
                                }
                                else
                                {
                                    LogInfo.WriteLog("+ Success CallWebService_GetCoupons");

                                    JArray xCouponData          = xResponse["data"];
                                    ObjCouponData[] objCoupons  = xCouponData == null ? new ObjCouponData[0] : xCouponData.ToObject<ObjCouponData[]>();
                                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ CallWebService_GetCoupons : coupons received={objCoupons.Length}");

                                    //--- Clear old coupons data (only now, TA answered OK)
                                    LogInfo.WriteLog("+ Clear old coupons data in SalesCouponList");
                                    szQuery = "DELETE FROM SalesCouponList";
                                    iRows   = TCT_Lib.LaneDB.Execute(szQuery);
                                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ CallWebService_GetCoupons : cleared SalesCouponList rows={iRows}");

                                    LogInfo.WriteLog("+ Insert new coupons data.");
                                    foreach (ObjCouponData xCoupon in objCoupons.ToList())
                                    {
                                        szQuery = "SELECT * FROM SalesCouponList " +
                                                    $"WHERE SerialNo='{(xCoupon.SerialNo ?? "").Replace("'", "''")}'";
                                        DataTable dt = TCT_Lib.LaneDB.List(szQuery);
                                        if (dt.Rows.Count == 0)
                                        {
                                            szQuery = "INSERT INTO SalesCouponList (CouponPK, CouponType, SerialNo, CouponPrice, " +
                                                        "CouponStatus, ReqCouponDateTime) VALUES " +
                                                        $"({xCoupon.CouponPK}, '{(xCoupon.CouponType ?? "").Replace("'", "''")}', '{(xCoupon.SerialNo ?? "").Replace("'", "''")}', {xCoupon.Price.ToString(InvC)}, " +
                                                        $"0, '{DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB, InvC)}')";
                                            iRows = TCT_Lib.LaneDB.Execute(szQuery);
                                            LogInfo.WriteLog(LogLevel.DEBUG, $"+ CallWebService_GetCoupons : insert CouponPK={xCoupon.CouponPK}, Type={xCoupon.CouponType}, SerialNo={xCoupon.SerialNo}, Price={xCoupon.Price}, rows={iRows}");
                                        }
                                        else
                                            LogInfo.WriteLog(LogLevel.DEBUG, $"+ CallWebService_GetCoupons : SerialNo={xCoupon.SerialNo} already in list, skip");
                                    }
                                    szCouponListStaffID = TCT_Lib.szStaffID ?? "";
                                    return true;
                                }
                            }
                        }
                    }
                }
                return false;
            }
            catch (Exception err)
            {
                szErrMsg = err.Message;
                Console.WriteLine($"- Error CallWebService_GetCoupons : {err.Message}");
                LogInfo.WriteLog($"- Error CallWebService_GetCoupons : {err.Message}");
                LogInfo.WriteLog(LogLevel.DEBUG, $"- CallWebService_GetCoupons : failed after {sw.ElapsedMilliseconds} ms, WebStatus={(err as WebException)?.Status}, StackTrace : {err.StackTrace}");
                ClearCouponList("no answer (" + err.Message + ")", true);
                return false;
            }           
        }

        public static bool CallWebService_SoldCoupon(string szCouponType, string szSerialNo, decimal fCouponPrice, DateTime dtSoldDate,
                                                    int iPayTypeID, string szPayTypeName, objCreditCardInfo xCreditCard, ref string szErrMsg,
                                                    string szSoldByStaffID = null)
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ CallWebService_SoldCoupon : enable={TCT_Lib.bEnableLaneWebService}, Type={szCouponType}, SerialNo={szSerialNo}, Price={fCouponPrice}, PayType={iPayTypeID}({szPayTypeName}), card={xCreditCard != null}");
            if (!TCT_Lib.bEnableLaneWebService)
                return false;

            if (!TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szLaneWebService_ServerIPAddress, TCT_Lib.iLaneWebService_ServerPort))
            {
                Console.WriteLine($"Fail Connect LaneWebService IP={TCT_Lib.szLaneWebService_ServerIPAddress}, Port={TCT_Lib.iLaneWebService_ServerPort}");
                LogInfo.WriteLog($"Fail Connect LaneWebService IP={TCT_Lib.szLaneWebService_ServerIPAddress}, Port={TCT_Lib.iLaneWebService_ServerPort}");
                return false;
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var xCoupon         = new ObjSoldCoupon();
                xCoupon.tsbid       = TCT_Lib.szTSBId;
                xCoupon.plazaid     = TCT_Lib.szPlazaID;
                xCoupon.laneid      = TCT_Lib.szLaneName;
                xCoupon.coupontype  = szCouponType;
                xCoupon.serialno    = szSerialNo;
                xCoupon.price       = fCouponPrice;
                xCoupon.userid      = szSoldByStaffID ?? TCT_Lib.szStaffID;     // v2.0.7: staff who sold it (send runs later, retries)
                xCoupon.solddate    = dtSoldDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                xCoupon.paytypeid   = iPayTypeID;
                xCoupon.paytypename = szPayTypeName;

                if ((iPayTypeID > 0) && (xCreditCard != null))
                {
                    string szEdcDate        = "20" + xCreditCard.szTransactionDate.Substring(0, 2) + "-" + xCreditCard.szTransactionDate.Substring(2, 2) + "-" + xCreditCard.szTransactionDate.Substring(4, 2);
                    string szEdcTime        = xCreditCard.szTransactionTime.Substring(0, 2) + ":" + xCreditCard.szTransactionTime.Substring(2, 2) + ":" + xCreditCard.szTransactionTime.Substring(4, 2)
;                    xCoupon.edcdatetime    = szEdcDate + " " + szEdcTime;
                    xCoupon.edcterminalid   = xCreditCard.szTerminalID;
                    xCoupon.edccardno       = xCreditCard.szCardNo;
                    xCoupon.edcamount       = decimal.Parse(xCreditCard.szAmount)/100;
                    xCoupon.edcref1         = xCreditCard.szRef1;
                    xCoupon.edcref2         = xCreditCard.szRef2;
                    xCoupon.edcref3         = xCreditCard.szRef3;
                }

                string msg          = JsonConvert.SerializeObject(xCoupon, Formatting.Indented);
                byte[] byteData     = Encoding.UTF8.GetBytes(msg);
                LogInfo.WriteLog("+ Parameter : " + msg);
                //--------------------------------------------------------------------
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                using (var client = new WebClient())
                {
                    client.Encoding     = new UTF8Encoding();
                    string szFmt        = "{0}/api/TCT/coupons/sold";
                    string szWebUrl     = string.Format(szFmt, TCT_Lib.szLaneWebService_BaseURL);
                    LogInfo.WriteLog("+ CallWebService_SoldCoupons URL : " + szWebUrl);
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ CallWebService_SoldCoupon : POST SerialNo={szSerialNo}, bytes={byteData.Length}, timeout=2000 ms");

                    var request                 = (HttpWebRequest)WebRequest.Create(szWebUrl);
                    request.Method              = "POST";
                    request.ContentType         = "application/json";
                    request.ContentLength       = byteData.Length;
                    request.Timeout             = 2 * 1000;
                    using (Stream webStream = request.GetRequestStream())
                    {
                        webStream.Write(byteData, 0, byteData.Length);
                        webStream.Flush();
                        webStream.Close();
                    }

                    using (WebResponse webResponse = request.GetResponse())
                    {
                        LogInfo.WriteLog(LogLevel.DEBUG, $"+ CallWebService_SoldCoupon : status={(int)((HttpWebResponse)webResponse).StatusCode}, {sw.ElapsedMilliseconds} ms");
                        if (((HttpWebResponse)webResponse).StatusCode != HttpStatusCode.OK)
                        {
                            szErrMsg = ((HttpWebResponse)webResponse).StatusDescription;
                            LogInfo.WriteLog("- Error Response CallWebService_SoldCoupons : " + szErrMsg);

                            return false;
                        }

                        Stream webStream = webResponse.GetResponseStream();
                        if (webStream != null)
                        {
                            using (var responseReader = new StreamReader(webStream))
                            {
                                string response     = responseReader.ReadToEnd();
                                LogInfo.WriteLog("+ Response CallWebService_SoldCoupons : " + response);

                                dynamic xResponse   = JsonConvert.DeserializeObject(response);
                                LogInfo.WriteLog(LogLevel.DEBUG, $"+ CallWebService_SoldCoupon : SerialNo={szSerialNo}, hasError={xResponse?["errors"]?["hasError"]}, errMsg={xResponse?["errors"]?["errMsg"]}, {sw.ElapsedMilliseconds} ms");
                                if ((bool)xResponse["errors"]["hasError"] == true)
                                {
                                    szErrMsg        = (string)xResponse["errors"]["errMsg"];
                                    LogInfo.WriteLog("- Fail CallWebService_SoldCoupons : " + (string)xResponse["errors"]["errMsg"]);
                                    return false;
                                }
                                else
                                {
                                    LogInfo.WriteLog("+ Success CallWebService_SoldCoupons");
                                    return true;
                                }
                            }
                        }
                    }
                }
                return false;
            }
            catch (Exception err)
            {
                szErrMsg = err.Message;
                Console.WriteLine($"- Error CallWebService_SoldCoupons : {err.Message}");
                LogInfo.WriteLog($"- Error CallWebService_SoldCoupons : {err.Message}");
                LogInfo.WriteLog(LogLevel.DEBUG, $"- CallWebService_SoldCoupon : failed after {sw.ElapsedMilliseconds} ms, WebStatus={(err as WebException)?.Status}, StackTrace : {err.StackTrace}");
                return false;
            }
        }

    }
}
