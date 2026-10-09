using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace TCT_App.WebServiceAPI
{
    public class WebService_ImageCamera
    {
        Thread sendImageThread;
        //------------------------------------------------------
        public WebService_ImageCamera()
        {

        }

        public void CallWebService_SendImageCaptureCamera(int iLaneTransID, int iJobNo, int nTrx, string szJsonData, long iRunningNo, List<ImageData> xListImageData)
        {
            LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"+ CallWebService_SendImageCaptureCamera : start thread JobNo={iJobNo}, nTrx={nTrx}, TransID={iLaneTransID}, RunningNo={iRunningNo}, images={xListImageData?.Count}, jsonLen={szJsonData?.Length}");
            sendImageThread                 = new Thread(() => ThreadSendCaptureImage(iLaneTransID, iJobNo, nTrx, szJsonData, iRunningNo, xListImageData));
            sendImageThread.Priority        = ThreadPriority.Normal;
            sendImageThread.IsBackground    = true;
            sendImageThread.Start();
            
        }

        public bool ThreadSendCaptureImage(int iLaneTransID, int iJobNo, int nTrx, string szJsonData, long iRunningNo, List<ImageData> xListImageData)
        {
            string szErrMsg = "";
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                if (TCT_Lib.szVideoNVR_WebApiBaseUrl == "")
                {
                    LogInfo.WriteLogVideoCapture($"- Fail CallWebService_SendImageCaptureCamera (JobNo={iJobNo}, nTrx={nTrx}, TransID={iLaneTransID}, RunningNo={iRunningNo}) : Not found WebApiBaseUrl for send image data.");
                    return false;
                }

                byte[] byteData = Encoding.UTF8.GetBytes(szJsonData);

                var username    = "DMTUSER";
                var password    = "DMTPASS";
                string encoded  = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1")
                                               .GetBytes(username + ":" + password));

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                using (var client   = new WebClient())
                {
                    client.Encoding         = new UTF8Encoding();
                    //string szFmt            = "{0}/dmt-image/api/v1/saveImages";
                    string szFmt            = "{0}";
                    string szWebUrl         = string.Format(szFmt, TCT_Lib.szVideoNVR_WebApiBaseUrl);
                    LogInfo.WriteLogVideoCapture($"+ CallWebService_SendImageCaptureCamera (JobNo={iJobNo}, nTrx={nTrx}, TransID={iLaneTransID}, RunningNo={iRunningNo}) URL : " + szWebUrl);

                    var request             = (HttpWebRequest)WebRequest.Create(szWebUrl);
                    request.Method          = "POST";
                    request.ContentType     = "application/json";
                    request.ContentLength   = byteData.Length;
                    request.Timeout         = 2 * 1000;
                    request.Headers.Add("Authorization", "Basic " + encoded);
                    LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"+ ThreadSendCaptureImage : POST TransID={iLaneTransID}, bytes={byteData.Length}, timeout=2000 ms");
                    using (Stream webStream = request.GetRequestStream())
                    {
                        webStream.Write(byteData, 0, byteData.Length);
                        webStream.Flush();
                        webStream.Close();
                    }

                    using (WebResponse webResponse = request.GetResponse())
                    {
                        LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"+ ThreadSendCaptureImage : TransID={iLaneTransID}, status={(int)((HttpWebResponse)webResponse).StatusCode}, {sw.ElapsedMilliseconds} ms");
                        if (((HttpWebResponse)webResponse).StatusCode != HttpStatusCode.OK)
                        {
                            szErrMsg = ((HttpWebResponse)webResponse).StatusDescription;
                            LogInfo.WriteLogVideoCapture($"- Error Response CallWebService_SendImageCaptureCamera (JobNo={iJobNo}, nTrx={nTrx}, TransID={iLaneTransID}, RunningNo={iRunningNo}) : " + szErrMsg);

                            return false;
                        }

                        Stream webStream = webResponse.GetResponseStream();
                        if (webStream != null)
                        {
                            using (var responseReader = new StreamReader(webStream))
                            {
                                string response = responseReader.ReadToEnd();
                                LogInfo.WriteLogVideoCapture($"+ Response CallWebService_SendImageCaptureCamera (JobNo={iJobNo}, nTrx={nTrx}, TransID={iLaneTransID}, RunningNo={iRunningNo}) : " + response);

                                dynamic xResponse = JsonConvert.DeserializeObject(response);
                                LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"+ ThreadSendCaptureImage : TransID={iLaneTransID}, respCode={xResponse?["respCode"]}, respMesg={xResponse?["respMesg"]}, {sw.ElapsedMilliseconds} ms");
                                if ((string)xResponse["respCode"] != "0000")
                                {
                                    szErrMsg = (string)xResponse["respMesg"];
                                    LogInfo.WriteLogVideoCapture($"- Fail CallWebService_SendImageCaptureCamera (JobNo={iJobNo}, nTrx={nTrx}, TransID={iLaneTransID}, RunningNo={iRunningNo}) : " + (string)xResponse["respMesg"]);
                                    TCT_Lib.UpdateImageCaptureCameraStatus(iLaneTransID, true);
                                    return true;        // Success Send to WebService but error from server
                                }
                                else
                                {
                                    LogInfo.WriteLogVideoCapture($"+ Success CallWebService_SendImageCaptureCamera (JobNo={iJobNo}, nTrx={nTrx}, TransID={iLaneTransID}, RunningNo={iRunningNo})");
                                    TCT_Lib.UpdateImageCaptureCameraStatus(iLaneTransID, true);

                                    //---------------------------------------
                                    LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"+ ThreadSendCaptureImage : TransID={iLaneTransID}, AutoKeepDay={TCT_Lib.iVideoNVR_AutoKeepDayImageFile} ({(TCT_Lib.iVideoNVR_AutoKeepDayImageFile < 0 ? "delete" : TCT_Lib.iVideoNVR_AutoKeepDayImageFile > 0 ? "move" : "keep")} local images)");
                                    if (TCT_Lib.iVideoNVR_AutoKeepDayImageFile < 0)
                                        TCT_NVR.DeleteFileCaptureImageTrans(iLaneTransID, iRunningNo, xListImageData);
                                    else if (TCT_Lib.iVideoNVR_AutoKeepDayImageFile > 0)
                                        TCT_NVR.MoveFolderCaptureImageTrans(iLaneTransID, iRunningNo, xListImageData);
                                    //--------------------------------------

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
                Console.WriteLine($"- Error CallWebService_SendImageCaptureCamera (JobNo={iJobNo}, nTrx={nTrx}, TransID={iLaneTransID}, RunningNo={iRunningNo}) : {err.Message}");
                LogInfo.WriteLogVideoCapture($"- Error CallWebService_SendImageCaptureCamera (JobNo={iJobNo}, nTrx={nTrx}, TransID={iLaneTransID}, RunningNo={iRunningNo}) : {err.Message}");
                LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"- ThreadSendCaptureImage : failed after {sw.ElapsedMilliseconds} ms, WebStatus={(err as WebException)?.Status}, StackTrace : {err.StackTrace}");
                TCT_Lib.UpdateImageCaptureCameraStatus(iLaneTransID, false);
                return false;
            }
        }
    }
}
