using System;
using System.Net;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Cache;
using System.IO;
using System.Globalization;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TCT_App
{
    public class TCT_NVR
    {
        int iJobNo, nTrx, iCurMode;
        //--------------------------------------------------------------------------------------------
        //--- v2.0.8: .NET/Mono allow only 2 concurrent HTTP connections per host by default. The 3 camera threads start
        //---         together, and 2-3 of them go to the same host (one NVR in Method 0, rear + third camera on one IP,
        //---         Method 3 Index 1/2 both rear) -> the third request waited for a free connection and its timeout
        //---         (counted from GetResponse) ran out = image 3 "sometimes" missing. Seen in MTC_Sim with a 3 s camera.
        static TCT_NVR()
        {
            try
            {
                if (ServicePointManager.DefaultConnectionLimit < 16)
                    ServicePointManager.DefaultConnectionLimit = 16;
                LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"+ TCT_NVR : ServicePointManager.DefaultConnectionLimit={ServicePointManager.DefaultConnectionLimit}");
            }
            catch (Exception err)
            {
                LogInfo.WriteLogVideoCapture(LogLevel.WARN, $"- Warning TCT_NVR : cannot set DefaultConnectionLimit : {err.Message}");
            }
        }

        public TCT_NVR(int _iJobNo, int _nTrx, int _iCurMode)
        {
            iJobNo      = _iJobNo;
            nTrx        = _nTrx;
            iCurMode    = _iCurMode;
            LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"+ TCT_NVR : JobNo={iJobNo}, nTrx={nTrx}, CurMode={iCurMode}");
        }

        public bool CreateImageFromUrl(string szCaptureUrl, int idx)
        {
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
            LogInfo.WriteLogVideoCapture("+ CaptureUrl : " + LogInfo.MaskUrl(szCaptureUrl));

            // Set a default policy level for the "http:" and "https" schemes.
            HttpRequestCachePolicy policy       = new HttpRequestCachePolicy(HttpRequestCacheLevel.Default);
            HttpWebRequest.DefaultCachePolicy   = policy;

            //CookieContainer cookies = new CookieContainer();
            var request             = (HttpWebRequest)WebRequest.Create(szCaptureUrl);
            HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.Default);
            //request.CachePolicy     = noCachePolicy;
            request.CachePolicy     = new HttpRequestCachePolicy(GetCachePolicy());

            try
            {
                //request.CookieContainer = cookies;
                request.Method          = "GET";
                request.KeepAlive       = false;
                //request.ProtocolVersion = HttpVersion.Version10;

                // v2.0.0: never allow an infinite timeout (a misconfigured 0 would block the
                // thread forever on an unresponsive NVR). Fall back to a safe 10s minimum.
                if (TCT_Lib.iVideoNVR_TimeoutSec <= 0)
                    request.Timeout     = 10 * 1000;
                else
                    request.Timeout     = TCT_Lib.iVideoNVR_TimeoutSec * 1000;
                LogInfo.WriteLogVideoCapture(TCT_Lib.iVideoNVR_TimeoutSec <= 0 ? LogLevel.WARN : LogLevel.DEBUG, $"{(TCT_Lib.iVideoNVR_TimeoutSec <= 0 ? "- Warning" : "+")} CreateImageFromUrl : Camera#{idx}, TimeoutSec={TCT_Lib.iVideoNVR_TimeoutSec} -> Timeout={request.Timeout} ms, CachePolicy={TCT_Lib.iVideoNVR_CachePolicy}");

                var swHttp = System.Diagnostics.Stopwatch.StartNew();
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    LogInfo.WriteLogVideoCapture($"+ Response Status Code {response.StatusCode}, Status Description : {response.StatusDescription}");
                    LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"+ CreateImageFromUrl : Camera#{idx} OK ({swHttp.ElapsedMilliseconds} ms) -> camera status=1, notify RTO");
                    Object objParams = new { status = 1 };
                    if (idx == 1)
                    {
                        TCT_Lib.bStatusCamera1 = true;
                        TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("CAMERA_1", objParams);
                    }
                    else if (idx == 2)
                    {
                        TCT_Lib.bStatusCamera2 = true;
                        TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("CAMERA_2", objParams);
                    }
                    else if (idx == 3)
                    {
                        TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("CAMERA_3", objParams);
                        TCT_Lib.bStatusCamera3 = true;
                    }

                    var cstor = typeof(ThreadAbortException)
                        .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);

                    response.Close();
                    response.Dispose();
                }

                request.Abort();
                request = null;
                //if (idx >1)
                //    request.Abort();

                //LogInfo.WriteLog("+ Response Create Capture VideoNVR : " + response);
                return true;

            }
            catch (WebException webErr)
            {
                LogInfo.WriteLogVideoCapture("- Error CreateImageFromUrl WebException : " + webErr.Message);
                LogInfo.WriteLogVideoCapture("- Error CreateImageFromUrl WebException : " + webErr.StackTrace);
                LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"- CreateImageFromUrl : Camera#{idx}, WebExceptionStatus={webErr.Status}, URL={LogInfo.MaskUrl(szCaptureUrl)} -> camera status=0, notify RTO");
                Object objParams = new { status = 0 };
                if (idx == 1)
                {
                    TCT_Lib.bStatusCamera1 = false;
                    TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("CAMERA_1", objParams);
                }
                else if (idx == 2)
                {
                    TCT_Lib.bStatusCamera2 = false;
                    TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("CAMERA_2", objParams);
                }
                else if (idx == 3)
                {
                    TCT_Lib.bStatusCamera3 = false;
                    TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("CAMERA_3", objParams);
                }

                request.Abort();
                request = null;
                //--- v2.0.8: no Thread.ResetAbort() here: without a pending abort it throws ThreadStateException
                //---         out of this catch on every camera timeout / connection error

                return false;
            }
            catch (ThreadAbortException exception)
            {
                LogInfo.WriteLogVideoCapture(LogLevel.ERROR, $"- Error CreateImageFromUrl ThreadAbortException : Camera#{idx} : {exception.Message}");
                LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"- CreateImageFromUrl : stack={exception.StackTrace}");
                //Thread.ResetAbort();
                request.Abort();
                request = null;
                //GC.SuppressFinalize(request);
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
                return false;
            }
            catch (TimeoutException webErr)
            {
                LogInfo.WriteLogVideoCapture("- Error CreateImageFromUrl TimeoutException : " + webErr.Message);
                LogInfo.WriteLogVideoCapture("- Error CreateImageFromUrl TimeoutException : " + webErr.StackTrace);
                Object objParams = new { status = 0 };
                if (idx == 1)
                {
                    TCT_Lib.bStatusCamera1 = false;
                    TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("CAMERA_1", objParams);
                }
                else if (idx == 2)
                {
                    TCT_Lib.bStatusCamera2 = false;
                    TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("CAMERA_2", objParams);
                }
                else if (idx == 3)
                {
                    TCT_Lib.bStatusCamera3 = false;
                    TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("CAMERA_3", objParams);
                }

                if (request != null)
                    request = null;

                return false;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error CreateImageFromUrl : " + err.Message);
                LogInfo.WriteLogVideoCapture("- Error CreateImageFromUrl : " + err.Message + ", URL: " + szCaptureUrl);
                LogInfo.WriteLogVideoCapture("- Error CreateImageFromUrl : " + err.StackTrace);

                Object objParams = new { status = 0 };
                if (idx == 1)
                {
                    TCT_Lib.bStatusCamera1 = false;
                    TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("CAMERA_1", objParams);
                }
                else if (idx == 2)
                {
                    TCT_Lib.bStatusCamera2 = false;
                    TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("CAMERA_2", objParams);
                }
                else if (idx == 3)
                {
                    TCT_Lib.bStatusCamera3 = false;
                    TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("CAMERA_3", objParams);
                }

                //if (idx > 1)
                request.Abort();

                //GC.SuppressFinalize(request);
                request = null;
                //GC.Collect();
                //GC.WaitForPendingFinalizers();

                return false;
            }
        }

        public bool SaveImageFromUrl(int idx, string szImgUrl, string szImgFileName, long iCaptureRunningNo, string szUserName, string szPassword)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            LogInfo.WriteLogVideoCapture($"+ CaptureImage #{idx}, Running={iCaptureRunningNo} : {szImgUrl}");

            // Set a default policy level for the "http:" and "https" schemes.
            HttpRequestCachePolicy policy       = new HttpRequestCachePolicy(HttpRequestCacheLevel.Default);
            HttpWebRequest.DefaultCachePolicy   = policy;

            //CookieContainer cookies = new CookieContainer();
            var request             = (HttpWebRequest)WebRequest.Create(szImgUrl);
            request.Credentials     = new NetworkCredential(szUserName, szPassword);
            //HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            //request.CachePolicy     = noCachePolicy;
            request.CachePolicy     = new HttpRequestCachePolicy(GetCachePolicy());

            try
            {
                // v2.0.0: never allow an infinite timeout on an unresponsive NVR (10s minimum).
                if (TCT_Lib.iVideoNVR_TimeoutSec <= 0)
                    request.Timeout = 10 * 1000;
                else
                    request.Timeout = TCT_Lib.iVideoNVR_TimeoutSec * 1000;
                LogInfo.WriteLogVideoCapture(TCT_Lib.iVideoNVR_TimeoutSec <= 0 ? LogLevel.WARN : LogLevel.DEBUG, $"{(TCT_Lib.iVideoNVR_TimeoutSec <= 0 ? "- Warning" : "+")} SaveImageFromUrl : Camera#{idx}, Running={iCaptureRunningNo}, TimeoutSec={TCT_Lib.iVideoNVR_TimeoutSec} -> Timeout={request.Timeout} ms, CachePolicy={TCT_Lib.iVideoNVR_CachePolicy}, User={szUserName}, JobNo={iJobNo}, nTrx={nTrx}");
                var swHttp = System.Diagnostics.Stopwatch.StartNew();
                using (var response = request.GetResponse())
                using (var stream   = response.GetResponseStream())
                {
                    if (response == null)
                    {
                        LogInfo.WriteLogVideoCapture($"- Fail response is null. index={idx}, CaptureRuunningNo={iCaptureRunningNo}");
                        request.Abort();
                        request = null;
                        return false;
                    }

                    if (stream == null)
                    {
                        LogInfo.WriteLogVideoCapture($"- Fail stream is null. index={idx}, CaptureRuunningNo={iCaptureRunningNo}");
                        response.Close();
                        response.Dispose();
                        request.Abort();
                        request = null;
                        return false;
                    }

                    //----------------------------------
                    Image img       = Image.FromStream(stream);
                    LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"+ SaveImageFromUrl : Camera#{idx} image {img?.Width}x{img?.Height} received ({swHttp.ElapsedMilliseconds} ms), resize={TCT_Lib.iVideoNVR_ResizeImage} ({TCT_Lib.iVideoNVR_ResizeWidth}x{TCT_Lib.iVideoNVR_ResizeHeight}), jpegQuality={TCT_Lib.iVideoNVR_JpegQuality}");
                    //Image imgPhoto  = ResizeImage(img, 768, 432);
                    //--- v2.0.3: 1280x720 (config ResizeWidth/ResizeHeight) high quality + JPEG quality (was 1024x576, default quality)
                    string szImgPath = Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, szImgFileName);
                    if (TCT_Lib.iVideoNVR_ResizeImage > 0)
                    {
                        using (Image imgPhoto = ResizeImage(img, TCT_Lib.iVideoNVR_ResizeWidth, TCT_Lib.iVideoNVR_ResizeHeight))
                            SaveJpeg(imgPhoto, szImgPath, TCT_Lib.iVideoNVR_JpegQuality);
                    }
                    else
                    {
                        SaveJpeg(img, szImgPath, TCT_Lib.iVideoNVR_JpegQuality);
                    }
                    LogInfo.WriteLogVideoCapture($"+ Save file image success. index={idx}, CaptureRuunningNo={iCaptureRunningNo}, FileName={szImgFileName}");
                    LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"+ SaveImageFromUrl : Camera#{idx} saved {szImgPath} ({swHttp.ElapsedMilliseconds} ms) -> camera status=1, notify RTO");

                    Object objParams = new { status = 1 };
                    if (idx == 1)
                    {
                        TCT_Lib.bStatusCamera1 = true;
                        TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("CAMERA_1", objParams);
                    }
                    else if (idx == 2)
                    {
                        TCT_Lib.bStatusCamera2 = true;
                        TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("CAMERA_2", objParams);
                    }
                    else if (idx == 3)
                    {
                        TCT_Lib.bStatusCamera3 = true;
                        TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("CAMERA_3", objParams);
                    }

                    SaveImageCaptureIntoDB(idx, szImgFileName, iCaptureRunningNo);
                    LogInfo.WriteLogVideoCapture($"+ Save image database success. index={idx}, CaptureRuunningNo={iCaptureRunningNo}, FileName={szImgFileName}");

                    img.Dispose();
                    response.Close();
                    response.Dispose();
                    request.Abort();
                    request = null;

                    return true;
                }
            }
            catch (WebException webErr)
            {
                LogInfo.WriteLogVideoCapture($"- Error CreateImageFromUrl WebException of index={idx}, CaptureRuunningNo={iCaptureRunningNo} : " + webErr.Message);
                LogInfo.WriteLogVideoCapture($"- Error CreateImageFromUrl WebException of index={idx}, CaptureRuunningNo={iCaptureRunningNo} : " + webErr.StackTrace);
                LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"- SaveImageFromUrl : Camera#{idx}, WebExceptionStatus={webErr.Status}, URL={szImgUrl} -> camera status=0, notify RTO");
                Object objParams = new { status = 0 };
                if (idx == 1)
                {
                    TCT_Lib.bStatusCamera1 = false;
                    TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("CAMERA_1", objParams);
                }
                else if (idx == 2)
                {
                    TCT_Lib.bStatusCamera2 = false;
                    TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("CAMERA_2", objParams);
                }
                else if (idx == 3)
                {
                    TCT_Lib.bStatusCamera3 = false;
                    TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("CAMERA_3", objParams);
                }

                request.Abort();
                request = null;
                //Thread.ResetAbort();

                return false;
            }
            catch (ThreadAbortException err)
            {
                LogInfo.WriteLogVideoCapture($"- Error CreateImageFromUrl ThreadAbortException of index={idx}, CaptureRuunningNo={iCaptureRunningNo} : " + err.Message);
                LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"- SaveImageFromUrl : stack={err.StackTrace}");
                //Thread.ResetAbort();
                request.Abort();
                request = null;
                return false;
            }
            catch (Exception err)
            {
                Console.WriteLine("Error : " + err.Message);
                LogInfo.WriteLogVideoCapture($"- Error CreateImageFromUrl Exception of index={idx}, CaptureRuunningNo={iCaptureRunningNo} : " + err.Message);
                LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"- SaveImageFromUrl : Camera#{idx}, URL={szImgUrl}, File={szImgFileName}, stack={err.StackTrace}");

                Object objParams = new { status = 0 };
                if (idx == 1)
                {
                    TCT_Lib.bStatusCamera1 = false;
                    TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("CAMERA_1", objParams);
                }
                else if (idx == 2)
                {
                    TCT_Lib.bStatusCamera2 = false;
                    TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("CAMERA_2", objParams);
                }
                else if (idx == 3)
                {
                    TCT_Lib.bStatusCamera3 = false;
                    TCT_Lib.webSocketLane.laneSocketServerRTO.SendLaneMessageToRTO("CAMERA_3", objParams);
                }

                request.Abort();
                request = null;
                return false;
            }
        }

        // HttpRequestCacheLevel.Default = 0, BypassCache=1, CacheOnly=2, CacheIfAvailable=3, Revalidate=4, Reload=5, NoCacheNoStore=6, CacheOrNextCacheOnly=7, Refresh=8
        private HttpRequestCacheLevel GetCachePolicy()
        {
            var cahcePolicy = HttpRequestCacheLevel.NoCacheNoStore;
            switch (TCT_Lib.iVideoNVR_CachePolicy)
            {
                case 0: return HttpRequestCacheLevel.Default;
                case 1: return HttpRequestCacheLevel.BypassCache;
                case 2: return HttpRequestCacheLevel.CacheOnly;
                case 3: return HttpRequestCacheLevel.CacheIfAvailable;
                case 4: return HttpRequestCacheLevel.Revalidate;
                case 5: return HttpRequestCacheLevel.Reload;
                case 6: return HttpRequestCacheLevel.NoCacheNoStore;
                case 7: return HttpRequestCacheLevel.CacheOrNextCacheOnly;
                case 8: return HttpRequestCacheLevel.Refresh;
                default:
                    LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"+ GetCachePolicy : unknown VideoNVR CachePolicy={TCT_Lib.iVideoNVR_CachePolicy} -> NoCacheNoStore");
                    return HttpRequestCacheLevel.NoCacheNoStore;
            }

            return cahcePolicy;
        }

        //--- v2.0.3: fit inside iMaxWidth x iMaxHeight keeping the aspect ratio, never upscale, high quality filter
        private Bitmap ResizeImage(Image sBmp, int iMaxWidth, int iMaxHeight)
        {
            double k = Math.Min(1.0, Math.Min((double)iMaxWidth / sBmp.Width, (double)iMaxHeight / sBmp.Height));
            int iNewWidth  = Math.Max(1, (int)Math.Round(sBmp.Width * k));
            int iNewHeight = Math.Max(1, (int)Math.Round(sBmp.Height * k));

            Bitmap result = new Bitmap(iNewWidth, iNewHeight);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode  = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode      = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode    = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.DrawImage(sBmp, 0, 0, iNewWidth, iNewHeight);
            }
            return result;
        }

        //--- v2.0.8: encode to a temp file first, then replace the target under a lock. Method 1 names the file
        //---         <lane>-<date>-<job>-<nTrx>-<index>.jpg (no time part), and a queued EXEMPT / LEFT_EXIT car is captured
        //---         twice for the same nTrx (at the key and again when the key is replayed as the car arrives): two threads
        //---         wrote the same file at once -> sharing violation / half-written JPEG = "capture ordered, no picture".
        static readonly object _saveLock = new object();
        private static void SaveJpeg(Image img, string szPath, int iQuality)
        {
            ImageCodecInfo jpeg = null;
            foreach (ImageCodecInfo c in ImageCodecInfo.GetImageEncoders())
                if (c.FormatID == ImageFormat.Jpeg.Guid)
                    jpeg = c;
            string szTmp = szPath + "." + Guid.NewGuid().ToString("N").Substring(0, 8) + ".tmp";
            try
            {
                if (jpeg == null)
                {
                    LogInfo.WriteLogVideoCapture(LogLevel.WARN, $"- Warning SaveJpeg : no JPEG encoder found -> default quality, File={szPath}");
                    img.Save(szTmp, ImageFormat.Jpeg);
                }
                else
                {
                    using (var prms = new EncoderParameters(1))
                    {
                        prms.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)iQuality);
                        img.Save(szTmp, jpeg, prms);
                    }
                }
                lock (_saveLock)
                {
                    if (File.Exists(szPath))
                        File.Delete(szPath);
                    File.Move(szTmp, szPath);
                }
            }
            finally
            {
                try { if (File.Exists(szTmp)) File.Delete(szTmp); } catch { }
            }
        }

        private Gdk.Pixbuf GetPixbufFromUrl(string szImgUrl)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            try
            {
                var request = WebRequest.Create(szImgUrl);
                using (var response = request.GetResponse())
                using (var stream   = response.GetResponseStream())
                {
                    Gdk.Pixbuf pixBuf = new Gdk.Pixbuf(stream);
                    //Gtk.Image img = new Gtk.Image(pixBuf);
                    return pixBuf;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("Error : " + err.Message);
                LogInfo.WriteLogVideoCapture("- Error GetPixbufFromUrl : " + err.Message);
                LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"- GetPixbufFromUrl : URL={szImgUrl}, stack={err.StackTrace}");
                return null;
            }
        }

        private Gtk.Image ImageToGtk(Image image)
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    image.Save(stream, ImageFormat.Jpeg);
                    stream.Position = 0;
                    Gtk.Image img   = new Gtk.Image(stream);
                    return img;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("Error : " + err.Message);
                LogInfo.WriteLogVideoCapture("- Error ImageToGtk : " + err.Message);
                LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"- ImageToGtk : stack={err.StackTrace}");
                return null;
            }
        }

        private bool SaveImageFromUrl_V0(string szImgUrl, string szImgFileName)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            try
            {
                var request         = WebRequest.Create(szImgUrl);
                using (var response = request.GetResponse())
                using (var stream   = response.GetResponseStream())
                {
                    Image img = Image.FromStream(stream);
                    img.Save(szImgFileName, ImageFormat.Jpeg);
                }

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine("Error : " + err.Message);
                LogInfo.WriteLogVideoCapture("- Error SaveImageFromUrl : " + err.Message);
                LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"- SaveImageFromUrl_V0 : URL={szImgUrl}, File={szImgFileName}, stack={err.StackTrace}");
                return false;
            }
        }

        private bool SavePixbufFromUrl(string szImgUrl, string szImgFileName)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            try
            {
                var request         = WebRequest.Create(szImgUrl);
                using (var response = request.GetResponse())
                using (var stream   = response.GetResponseStream())
                {
                    Gdk.Pixbuf pixBuf = new Gdk.Pixbuf(stream);
                    pixBuf.Save(szImgFileName, "jpeg");
                }

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine("Error : " + err.Message);
                LogInfo.WriteLogVideoCapture("- Error SavePixbufFromUrl : " + err.Message);
                LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"- SavePixbufFromUrl : URL={szImgUrl}, File={szImgFileName}, stack={err.StackTrace}");
                return false;
            }
        }

        public static string ConvertImageToBase64(string szFileName)
        {
            try
            {
                if (!File.Exists(szFileName))
                {
                    LogInfo.WriteLogVideoCapture("- File not found : " + szFileName);
                    return "";
                }
                //----------------------------------
                using (Image _image = Image.FromFile(szFileName))
                {
                    using (MemoryStream _mStream = new MemoryStream())
                    {
                        _image.Save(_mStream, _image.RawFormat);
                        byte[] _imageBytes      = _mStream.ToArray();
                        string _base64String    = Convert.ToBase64String(_imageBytes);
                        LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"+ ConvertImageToBase64 : File={szFileName}, bytes={_imageBytes?.Length ?? 0}, base64Len={_base64String?.Length ?? 0}");

                        return _base64String;
                    }
                }
/*
                byte[] imageArray = File.ReadAllBytes(szFileName);
                string base64ImageRepresentation = Convert.ToBase64String(imageArray);
                return base64ImageRepresentation;
*/
            }
            catch (Exception err)
            {
                LogInfo.WriteLogVideoCapture("- Error ConvertImageToBase64 : " + err.Message);
                LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"- ConvertImageToBase64 : File={szFileName}, stack={err.StackTrace}");
                return "";
            }
        }

        private Image ConvertStringBase64ToImage(string base64)
        {
            byte[] bytes    = Convert.FromBase64String(base64);
            Image image     = null;
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                image = Image.FromStream(ms);
            }
            return image;
        }

        public bool SaveImageCaptureIntoDB(int index, string szImageFileName, long iCaptureRunningNo)
        {
            var InvC = CultureInfo.InvariantCulture;
            try
            {
                LogInfo.WriteLogVideoCapture($"+ SaveImageCaptureIntoDB = Index={index}, FileName={szImageFileName}, RunningNo={iCaptureRunningNo}");

                //if (iCurMode == 0)      // MTC
                {
                    //--- Check Job, and nTrx
                    //long iImageCaptureID = TCT_Lib.LaneDB.GetMaxImageCaptureID();
                    //--- v2.0.7: the 3 camera threads take IDs atomically, and a failed INSERT throws again so the
                    //---         retry below runs (1.5.7); Execute() had stopped throwing, the image row was lost.
                    long iImageCaptureID = System.Threading.Interlocked.Increment(ref TCT_Lib.iMaxImageCaptureCameraID) - 1;
                    LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"+ SaveImageCaptureIntoDB : ImageCaptureID={iImageCaptureID} (next={TCT_Lib.iMaxImageCaptureCameraID}), JobNo={iJobNo}, nTrx={nTrx}, CurMode={iCurMode}");
                    string szQuery = "INSERT INTO ImageCaptureCamera (ImageCaptureID, CaptureRunningNo, JobNo, nTrx, " +
                                    "CurMode, CameraIndex, ImageCameraFileName, UpdateDateTime) VALUES (" +
                                    $"{iImageCaptureID}, {iCaptureRunningNo}, {iJobNo}, {nTrx}, {iCurMode}, '{index}', " +
                                    $"'{szImageFileName}', '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", InvC)}')";
                    LogInfo.WriteLogVideoCapture("+ Query : " + szQuery);
                    TCT_Lib.LaneDB.ExecuteOrThrow(szQuery, true);
                }

                return true;
            }
            catch (Exception err)
            {
                LogInfo.WriteLogVideoCapture($"- Error SaveImageCaptureIntoDB = Index={index}, FileName={szImageFileName}, RunningNo={iCaptureRunningNo} : " + err.Message);
                LogInfo.WriteLogVideoCapture($"- Retry SaveImageCaptureCamera again");
                LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"- SaveImageCaptureIntoDB : stack={err.StackTrace}");
                //long iImageCaptureID = TCT_Lib.LaneDB.GetMaxImageCaptureID();
                long iImageCaptureID = System.Threading.Interlocked.Increment(ref TCT_Lib.iMaxImageCaptureCameraID) - 1;
                LogInfo.WriteLogVideoCapture(LogLevel.WARN, $"- Warning SaveImageCaptureIntoDB : retry with ImageCaptureID={iImageCaptureID}, Index={index}, RunningNo={iCaptureRunningNo}");
                string szQuery = "INSERT INTO ImageCaptureCamera (ImageCaptureID, CaptureRunningNo, JobNo, nTrx, " +
                                "CurMode, CameraIndex, ImageCameraFileName, UpdateDateTime) VALUES (" +
                                $"{iImageCaptureID}, {iCaptureRunningNo}, {iJobNo}, {nTrx}, {iCurMode}, '{index}', " +
                                $"'{szImageFileName}', '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", InvC)}')";
                LogInfo.WriteLogVideoCapture("+ Query : " + szQuery);
                TCT_Lib.LaneDB.Execute(szQuery);

                return false;
            }
        }

        public static bool MoveFolderCaptureImageTrans(int iTransID, long iRunningNo, List<ImageData> xListImageData)
        {
            LogInfo.WriteLogVideoCapture($"+ MoveFolderCaptureImageTrans : TransID={iTransID}, RunningNo={iRunningNo}");
            try
            {
                string szImgFolder = Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, DateTime.Today.ToString("yyyy-MM-dd"));
                if (!Directory.Exists(szImgFolder))
                    Directory.CreateDirectory(szImgFolder);

                string szJsonFile = Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, $"ImageCamera_Trans{iTransID}_N{iRunningNo.ToString("000000")}.json");
                if (File.Exists(Path.Combine(szImgFolder, $"ImageCamera_Trans{iTransID}_N{iRunningNo.ToString("000000")}.json")))
                    File.Delete(Path.Combine(szImgFolder, $"ImageCamera_Trans{iTransID}_N{iRunningNo.ToString("000000")}.json"));
                File.Move(szJsonFile, Path.Combine(szImgFolder, $"ImageCamera_Trans{iTransID}_N{iRunningNo.ToString("000000")}.json"));
                if (LogInfo.IsEnabled(LogLevel.DEBUG))
                    LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"+ MoveFolderCaptureImageTrans : json moved to {szImgFolder}, images={xListImageData?.Count ?? 0} [{string.Join("|", (xListImageData ?? new List<ImageData>()).ConvertAll(x => x?.fileName ?? "null"))}]");


                if (xListImageData[0].fileName != "")
                {
                    if (File.Exists(Path.Combine(szImgFolder, xListImageData[0].fileName)))
                        File.Delete(Path.Combine(szImgFolder, xListImageData[0].fileName));
                    File.Move(Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, xListImageData[0].fileName), Path.Combine(szImgFolder, xListImageData[0].fileName));
                }

                if (xListImageData[1].fileName != "")
                {
                    if (File.Exists(Path.Combine(szImgFolder, xListImageData[1].fileName)))
                        File.Delete(Path.Combine(szImgFolder, xListImageData[1].fileName));
                    File.Move(Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, xListImageData[1].fileName), Path.Combine(szImgFolder, xListImageData[1].fileName));
                }

                if (xListImageData[2].fileName != "")
                {
                    if (File.Exists(Path.Combine(szImgFolder, xListImageData[2].fileName)))
                        File.Delete(Path.Combine(szImgFolder, xListImageData[2].fileName));
                    File.Move(Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, xListImageData[2].fileName), Path.Combine(szImgFolder, xListImageData[2].fileName));
                }

                return true;
            }
            catch (Exception err)
            {
                LogInfo.WriteLogVideoCapture("- Error MoveFolderCaptureImageTrans : " + err.Message);
                LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"- MoveFolderCaptureImageTrans : TransID={iTransID}, RunningNo={iRunningNo}, stack={err.StackTrace}");
                return false;
            }
        }

        public static bool DeleteFileCaptureImageTrans(int iTransID, long iRunningNo, List<ImageData> xListImageData)
        {
            LogInfo.WriteLogVideoCapture($"+ DeleteFileCaptureImageTrans : TransID={iTransID}, RunningNo={iRunningNo}");
            try
            {
                string szJsonFile = Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, $"ImageCamera_Trans{iTransID}_N{iRunningNo.ToString("000000")}.json");
                File.Delete(szJsonFile);
                if (LogInfo.IsEnabled(LogLevel.DEBUG))
                    LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"+ DeleteFileCaptureImageTrans : deleted {szJsonFile}, images={xListImageData?.Count ?? 0} [{string.Join("|", (xListImageData ?? new List<ImageData>()).ConvertAll(x => x?.fileName ?? "null"))}]");
                if (xListImageData[0].fileName != "")
                    File.Delete(Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, xListImageData[0].fileName));
                if (xListImageData[1].fileName != "")
                    File.Delete(Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, xListImageData[1].fileName));
                if (xListImageData[2].fileName != "")
                    File.Delete(Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, xListImageData[2].fileName));

                return true;
            }
            catch (Exception err)
            {
                LogInfo.WriteLogVideoCapture("- Error DeleteFileCaptureImageTrans : " + err.Message);
                LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"- DeleteFileCaptureImageTrans : TransID={iTransID}, RunningNo={iRunningNo}, stack={err.StackTrace}");
                return false;
            }
        }

        public static bool AutoClearFolderCaptureImage()
        {
            LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"+ AutoClearFolderCaptureImage : CaptureImageMethod={TCT_Lib.iVideoNVR_CaptureImageMethod}, AutoKeepDayImageFile={TCT_Lib.iVideoNVR_AutoKeepDayImageFile}, Path={TCT_Lib.szVideoLane_CaptureImagePath}");
            if (TCT_Lib.iVideoNVR_CaptureImageMethod == 0)
                return false;

            if (TCT_Lib.iVideoNVR_AutoKeepDayImageFile <= 0)
                return false;   

            LogInfo.WriteLogVideoCapture($"+ AutoClearFolderCaptureImage");
            try
            {
                DateTime dtKeepDate = DateTime.Today.AddDays(-TCT_Lib.iVideoNVR_AutoKeepDayImageFile);
                string[] szFolders  = Directory.GetDirectories(TCT_Lib.szVideoLane_CaptureImagePath);
                LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"+ AutoClearFolderCaptureImage : keep >= {dtKeepDate:yyyy-MM-dd}, folders={szFolders?.Length ?? 0}");
                foreach(string szFolder in szFolders)
                {
                    try
                    {
                        DateTime dtFolder = Convert.ToDateTime(Path.GetFileName(szFolder));
                        if (dtFolder < dtKeepDate)
                        {
                            LogInfo.WriteLogVideoCapture($"+ Delete folder : " + szFolder);
                            //Directory.Delete(szFolder, true);
                            TCT_Lib.DeleteDirectory(szFolder);
                        }
                    }
                    catch (Exception err)
                    {
                        LogInfo.WriteLogVideoCapture("- Error Delete Folder Capture Image : " + err.Message);
                        LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"- AutoClearFolderCaptureImage : Folder={szFolder}, stack={err.StackTrace}");
                    }
                }

                //---------------------------------------------------------------------
                string[] files = Directory.GetFiles(TCT_Lib.szVideoLane_CaptureImagePath, "*_N000000.json");
                LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"+ AutoClearFolderCaptureImage : delete *_N000000.json files={files?.Length ?? 0}");
                foreach (string file in files)
                {
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                    }
                    catch (Exception err)
                    {
                        LogInfo.WriteLogVideoCapture("- Error Set Attribute Capture Image : " + err.Message);
                        LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"- AutoClearFolderCaptureImage : File={file}, stack={err.StackTrace}");
                    }
                }
                //---------------------------------------------------------------------
                //--- Auto move old file to Folder
                string szTodayFolder = DateTime.Today.ToString("yyyy-MM-dd");
                files = Directory.GetFiles(TCT_Lib.szVideoLane_CaptureImagePath, "*.jpg");
                foreach (string file in files)
                {
                    try
                    {
                        //--- Check file is today, if not today, then create and move to folder
                        string szFile = Path.GetFileName(file);
                        string[] szSepFile = szFile.Split('-');
                        if (szSepFile.Length > 2)
                        {
                            string szDate = szSepFile[1].Insert(4, "-").Insert(7, "-");
                            if (szDate != szTodayFolder)
                            {
                                string szFolder = Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, szDate);
                                if (!Directory.Exists(szFolder))
                                    Directory.CreateDirectory(szFolder);
                                LogInfo.WriteLogVideoCapture(LogLevel.TRACE, $"+ AutoClearFolderCaptureImage : move {szFile} -> {szDate}/");
                                File.Move(file, Path.Combine(szFolder, szFile));
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        LogInfo.WriteLogVideoCapture(LogLevel.ERROR, $"- Error AutoClearFolderCaptureImage move by date (split '-') : File={file} : {err.Message}");
                        LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"- AutoClearFolderCaptureImage : stack={err.StackTrace}");
                    }
                }
                //---------------------------------------------------------------------
                //--- Auto move old file to Folder
                files = Directory.GetFiles(TCT_Lib.szVideoLane_CaptureImagePath, "*.jpg");
                foreach (string file in files)
                {
                    try
                    {
                        //--- Check file is today, if not today, then create and move to folder
                        string szFile = Path.GetFileName(file);
                        string[] szSepFile = szFile.Split('_');
                        if (szSepFile.Length > 2)
                        {
                            string szDate = szSepFile[2].Substring(1).Insert(4, "-").Insert(7, "-");
                            if (szDate != szTodayFolder)
                            {
                                string szFolder = Path.Combine(TCT_Lib.szVideoLane_CaptureImagePath, szDate);
                                if (!Directory.Exists(szFolder))
                                    Directory.CreateDirectory(szFolder);
                                LogInfo.WriteLogVideoCapture(LogLevel.TRACE, $"+ AutoClearFolderCaptureImage : move {szFile} -> {szDate}/");
                                File.Move(file, Path.Combine(szFolder, szFile));
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        LogInfo.WriteLogVideoCapture(LogLevel.ERROR, $"- Error AutoClearFolderCaptureImage move by date (split '_') : File={file} : {err.Message}");
                        LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"- AutoClearFolderCaptureImage : stack={err.StackTrace}");
                    }
                }
                //--------------------------------------------------------------------
                LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, "+ AutoClearFolderCaptureImage : done");

                return true;
            }
            catch (Exception err)
            {
                LogInfo.WriteLogVideoCapture("- Error AutoClearFolderCaptureImage : " + err.Message);
                LogInfo.WriteLogVideoCapture(LogLevel.DEBUG, $"- AutoClearFolderCaptureImage : stack={err.StackTrace}");
                return false;
            }
        }



    }
}
