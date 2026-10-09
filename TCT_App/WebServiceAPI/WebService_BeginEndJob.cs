using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TCT_App.WebServiceAPI
{
    public class ObjBeginEndJob
    {
        public string jobid;
        public string tsbid;
        public string plazaid;
        public string laneid;
        public string userid;
        public string boj;
        public string eoj;
    }

    public class ObjChkBojTOD
    {
        public string TSBId;
        public string UserId;
    }

    //-------------------------------------------------------
    public class WebService_BeginEndJob
    {
        public static bool CallWebService_BeginOfJob(DateTime dtBeginJob, ref string szErrMsg)
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ CallWebService_BeginOfJob : enable={TCT_Lib.bEnableLaneWebService}, StaffShiftID={TCT_Lib.iStaffShiftID}, StaffID={TCT_Lib.szStaffID}, BOJ={dtBeginJob:yyyy-MM-dd HH:mm:ss}");
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
                var xBoj        = new ObjBeginEndJob();
                xBoj.jobid      = TCT_Lib.szLaneName +"-"+TCT_Lib.iStaffShiftID.ToString("000000");
                xBoj.tsbid      = TCT_Lib.szTSBId;
                xBoj.plazaid    = TCT_Lib.szPlazaID;
                xBoj.laneid     = TCT_Lib.szLaneName;
                xBoj.userid     = TCT_Lib.szStaffID;
                xBoj.boj        = dtBeginJob.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                xBoj.eoj        = null;

                string msg      = JsonConvert.SerializeObject(xBoj, Formatting.Indented);
                byte[] byteData = Encoding.UTF8.GetBytes(msg);
                LogInfo.WriteLog("+ Parameter BeginOfJob : " + msg);
                //--------------------------------------------------------------------
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                using (var client = new WebClient())
                {
                    client.Encoding     = new UTF8Encoding();
                    string szFmt        = "{0}/api/TCT/LaneAttendance/save";
                    string szWebUrl     = string.Format(szFmt, TCT_Lib.szLaneWebService_BaseURL);
                    LogInfo.WriteLog("+ CallWebService_BeginOfJob URL : " + szWebUrl);
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ CallWebService_BeginOfJob : POST jobid={xBoj.jobid}, bytes={byteData.Length}, timeout=2000 ms");

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
                        LogInfo.WriteLog(LogLevel.DEBUG, $"+ CallWebService_BeginOfJob : status={(int)((HttpWebResponse)webResponse).StatusCode}, {sw.ElapsedMilliseconds} ms");
                        if (((HttpWebResponse)webResponse).StatusCode != HttpStatusCode.OK)
                        {
                            szErrMsg = ((HttpWebResponse)webResponse).StatusDescription;
                            LogInfo.WriteLog("- Error Response CallWebService_BeginOfJob : " + szErrMsg);

                            return false;
                        }

                        Stream webStream = webResponse.GetResponseStream();
                        if (webStream != null)
                        {
                            using (var responseReader = new StreamReader(webStream))
                            {
                                string response     = responseReader.ReadToEnd();
                                LogInfo.WriteLog("+ Response CallWebService_BeginOfJob : " + response);

                                dynamic xResponse = JsonConvert.DeserializeObject(response);
                                LogInfo.WriteLog(LogLevel.DEBUG, $"+ CallWebService_BeginOfJob : hasError={xResponse?["errors"]?["hasError"]}, errMsg={xResponse?["errors"]?["errMsg"]}, {sw.ElapsedMilliseconds} ms");
                                if ((bool)xResponse["errors"]["hasError"] == true)
                                {
                                    szErrMsg        = (string)xResponse["errors"]["errMsg"];
                                    LogInfo.WriteLog("- Fail CallWebService_BeginOfJob : " + (string)xResponse["errors"]["errMsg"]);
                                    return false;
                                }
                                else
                                {
                                    LogInfo.WriteLog("+ Success CallWebService_BeginOfJob");
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
                Console.WriteLine($"- Error CallWebService_BeginOfJob : {err.Message}");
                LogInfo.WriteLog($"- Error CallWebService_BeginOfJob : {err.Message}");
                LogInfo.WriteLog(LogLevel.DEBUG, $"- CallWebService_BeginOfJob : failed after {sw.ElapsedMilliseconds} ms, WebStatus={(err as WebException)?.Status}, StackTrace : {err.StackTrace}");
                return false;
            }
        }

        public static bool CallWebService_EndOfJob(DateTime dtEndJob, ref string szErrMsg)
        {
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ CallWebService_EndOfJob : disabled in code, skip (EOJ={dtEndJob:yyyy-MM-dd HH:mm:ss})");
            return false;

            if (!TCT_Lib.bEnableLaneWebService)
                return false;

            if (!TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szLaneWebService_ServerIPAddress, TCT_Lib.iLaneWebService_ServerPort))
            {
                Console.WriteLine($"Fail Connect LaneWebService IP={TCT_Lib.szLaneWebService_ServerIPAddress}, Port={TCT_Lib.iLaneWebService_ServerPort}");
                LogInfo.WriteLog($"Fail Connect LaneWebService IP={TCT_Lib.szLaneWebService_ServerIPAddress}, Port={TCT_Lib.iLaneWebService_ServerPort}");
                return false;
            }

            try
            {
                var xEoj        = new ObjBeginEndJob();
                xEoj.jobid      = TCT_Lib.szLaneName + "-" + TCT_Lib.iStaffShiftID.ToString("000000");
                xEoj.tsbid      = TCT_Lib.szTSBId;
                xEoj.plazaid    = TCT_Lib.szPlazaID;
                xEoj.laneid     = TCT_Lib.szLaneName;
                xEoj.userid     = TCT_Lib.szStaffID;
                xEoj.boj        = null;
                xEoj.eoj        = dtEndJob.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"); ;

                string msg      = JsonConvert.SerializeObject(xEoj, Formatting.Indented);
                byte[] byteData = Encoding.UTF8.GetBytes(msg);
                LogInfo.WriteLog("+ Parameter EndOfJob : " + msg);
                //--------------------------------------------------------------------
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                using (var client = new WebClient())
                {
                    client.Encoding     = new UTF8Encoding();
                    string szFmt        = "{0}/api/TCT/LaneAttendance/save";
                    string szWebUrl     = string.Format(szFmt, TCT_Lib.szLaneWebService_BaseURL);
                    LogInfo.WriteLog("+ CallWebService_EndOfJob URL : " + szWebUrl);

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
                        if (((HttpWebResponse)webResponse).StatusCode != HttpStatusCode.OK)
                        {
                            szErrMsg = ((HttpWebResponse)webResponse).StatusDescription;
                            LogInfo.WriteLog("- Error Response CallWebService_EndOfJob : " + szErrMsg);

                            return false;
                        }

                        Stream webStream = webResponse.GetResponseStream();
                        if (webStream != null)
                        {
                            using (var responseReader = new StreamReader(webStream))
                            {
                                string response         = responseReader.ReadToEnd();
                                LogInfo.WriteLog("+ Response CallWebService_EndOfJob : " + response);

                                dynamic xResponse = JsonConvert.DeserializeObject(response);
                                if ((bool)xResponse["errors"]["hasError"] == true)
                                {
                                    szErrMsg = (string)xResponse["errors"]["errMsg"];
                                    LogInfo.WriteLog("- Fail CallWebService_EndOfJob : " + (string)xResponse["errors"]["errMsg"]);
                                    return false;
                                }
                                else
                                {
                                    LogInfo.WriteLog("+ Success CallWebService_EndOfJob");
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
                Console.WriteLine($"- Error CallWebService_EndOfJob : {err.Message}");
                LogInfo.WriteLog($"- Error CallWebService_EndOfJob : {err.Message}");
                return false;
            }
        }

        public static bool CallWebService_ChkStartJobTOD(string szStaffId, ref bool bFoundBojTOD, ref int iStaffShiftId, ref bool bFailWebService, ref string szErrMsg)
        {
            bFailWebService = false;
            if (!TCT_Lib.bEnableLaneWebService)
            {
                szErrMsg = "Service Check Start Job TOD Disable";
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ CallWebService_ChkStartJobTOD : web service disabled, StaffId={szStaffId}");
                return false;
            }

            LogInfo.WriteLog("+ CallWebService_ChkStartJobTOD for StaffId=" + szStaffId);
            if (!TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szLaneWebService_ServerIPAddress, TCT_Lib.iLaneWebService_ServerPort))
            {
                Console.WriteLine($"Fail Connect LaneWebService IP={TCT_Lib.szLaneWebService_ServerIPAddress}, Port={TCT_Lib.iLaneWebService_ServerPort}");
                LogInfo.WriteLog($"Fail Connect LaneWebService IP={TCT_Lib.szLaneWebService_ServerIPAddress}, Port={TCT_Lib.iLaneWebService_ServerPort}");
                bFailWebService = true;
                szErrMsg = "ไม่สามารถเชื่อมต่อ TA Server ได้. โปรดตรวจสอบ TA Server Network.";

                return false;
            }

            bFoundBojTOD = false;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var xChkTOD     = new ObjChkBojTOD();
                xChkTOD.TSBId   = TCT_Lib.szTSBId;
                xChkTOD.UserId  = szStaffId;

                string msg      = JsonConvert.SerializeObject(xChkTOD, Formatting.Indented);
                byte[] byteData = Encoding.UTF8.GetBytes(msg);
                LogInfo.WriteLog("+ Parameter ChkBojTOD : " + msg);
                //--------------------------------------------------------------------
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                using (var client = new WebClient())
                {
                    client.Encoding     = new UTF8Encoding();
                    string szFmt        = "{0}/api/TCT/CheckTODBoj";
                    string szWebUrl     = string.Format(szFmt, TCT_Lib.szLaneWebService_BaseURL);
                    LogInfo.WriteLog("+ CallWebService_ChkStartJobTOD URL : " + szWebUrl);
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ CallWebService_ChkStartJobTOD : POST TSBId={xChkTOD.TSBId}, UserId={xChkTOD.UserId}, bytes={byteData.Length}, timeout=3000 ms");

                    var request                 = (HttpWebRequest)WebRequest.Create(szWebUrl);
                    request.Method              = "POST";
                    request.ContentType         = "application/json";
                    request.ContentLength       = byteData.Length;
                    request.Timeout             = 3 * 1000;
                    using (Stream webStream = request.GetRequestStream())
                    {
                        webStream.Write(byteData, 0, byteData.Length);
                        webStream.Flush();
                        webStream.Close();
                    }

                    using (WebResponse webResponse = request.GetResponse())
                    {
                        LogInfo.WriteLog(LogLevel.DEBUG, $"+ CallWebService_ChkStartJobTOD : status={(int)((HttpWebResponse)webResponse).StatusCode}, {sw.ElapsedMilliseconds} ms");
                        if (((HttpWebResponse)webResponse).StatusCode != HttpStatusCode.OK)
                        {
                            szErrMsg = ((HttpWebResponse)webResponse).StatusDescription;
                            LogInfo.WriteLog("- Error Response CallWebService_ChkStartJobTOD : " + szErrMsg);

                            return false;
                        }

                        Stream webStream = webResponse.GetResponseStream();
                        if (webStream != null)
                        {
                            using (var responseReader = new StreamReader(webStream))
                            {
                                string response     = responseReader.ReadToEnd();
                                LogInfo.WriteLog("+ Response CallWebService_ChkStartJobTOD : " + response);

                                dynamic xResponse = JsonConvert.DeserializeObject(response);
                                LogInfo.WriteLog(LogLevel.DEBUG, $"+ CallWebService_ChkStartJobTOD : hasError={xResponse?["errors"]?["hasError"]}, errMsg={xResponse?["errors"]?["errMsg"]}, {sw.ElapsedMilliseconds} ms");
                                if ((bool)xResponse["errors"]["hasError"] == true)
                                {
                                    szErrMsg        = (string)xResponse["errors"]["errMsg"];
                                    LogInfo.WriteLog("- Fail CallWebService_ChkStartJobTOD : " + (string)xResponse["errors"]["errMsg"]);
                                    return false;
                                }
                                else
                                {
                                    LogInfo.WriteLog("+ Success CallWebService_ChkStartJobTOD");
                                    //--- Check Boj TOD
                                    JArray xJobData = xResponse["data"];
                                    if (xJobData == null)
                                    {
                                        bFoundBojTOD    = false;
                                        //szErrMsg        = "ท่านยังไม่ได้ทำการเช้างาน TOD. โปรดทำการเข้างาน TOD ก่อน";
                                        szErrMsg = "ท่านยังไม่ได้เปิดกะทำงาน กรุณาเปิดกะทำงานที่ระบบ TOD ก่อน";
                                    }
                                    else
                                    {
                                        Object[] objJobs    = xJobData.ToObject<Object[]>();
                                        bFoundBojTOD        = objJobs.Length > 0;
                                        if (objJobs.Length == 0)
                                            //szErrMsg = "ท่านยังไม่ได้ทำการเช้างาน TOD. โปรดทำการเข้างาน TOD ก่อน";
                                            szErrMsg = "ท่านยังไม่ได้เปิดกะทำงาน กรุณาเปิดกะทำงานที่ระบบ TOD ก่อน";
                                        else
                                        {
                                            dynamic objJob  = objJobs[0];
                                            iStaffShiftId   = (int)(objJob["ShiftId"] ?? 0);
                                            LogInfo.WriteLog(LogLevel.DEBUG, $"+ CallWebService_ChkStartJobTOD : TOD jobs={objJobs.Length}, ShiftId={iStaffShiftId}");

                                            //dynamic objJob1 = (JObject.Parse(objJob[0]));
                                            //iStaffShiftId   = (int) (objJob1.ShiftId ?? 0);
                                        }
                                    }
                                    if (!bFoundBojTOD)
                                        LogInfo.WriteLog(LogLevel.WARN, $"- Warning CallWebService_ChkStartJobTOD : no TOD begin-of-job for StaffId={szStaffId}");
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
                Console.WriteLine($"- Error CallWebService_ChkStartJobTOD : {err.Message}");
                LogInfo.WriteLog($"- Error CallWebService_ChkStartJobTOD : {err.Message}");
                LogInfo.WriteLog(LogLevel.DEBUG, $"- CallWebService_ChkStartJobTOD : failed after {sw.ElapsedMilliseconds} ms, WebStatus={(err as WebException)?.Status}, StackTrace : {err.StackTrace}");
                return false;
            }
        }

    }
}
