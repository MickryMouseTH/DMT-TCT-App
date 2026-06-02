#define __FULL_SCREEN__

using System;
using System.IO;
using System.Reflection;

namespace TCT_App
{
    public class ConfigInfo
    {
        public static string GetConfigPath()
        {
#if __FULL_SCREEN__
            string szCurPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            szCurPath        = szCurPath.Remove(szCurPath.LastIndexOf('/'));
            return szCurPath + "/config";
#else
        return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
#endif
    }
}

    public class LogInfo
    {
#if __FULL_SCREEN__
        public static string szLogPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).Remove(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).LastIndexOf('/')) + "/log";
#else
        public static string szLogPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"/log";
#endif

        // v2.0.0: serialize all file writes to prevent interleaved/corrupted log lines
        // when multiple threads (UI, socket, RabbitMQ, LSD) write concurrently.
        private static readonly object _logLock = new object();

        public static bool CreateLogPath()
        {
            try
            {
                if (TCT_Lib.szLogPath.Trim() == "")
                {
#if __FULL_SCREEN__
                    //szLogPath = TCT_Lib.baseLogFolder;
                    szLogPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    szLogPath = szLogPath.Remove(szLogPath.LastIndexOf('/'));
                    szLogPath += @"/log";
#else
                    szLogPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"/log";
#endif
                    Console.WriteLine("+ szLogPath = " + szLogPath);
                    TCT_Lib.szLogPath = szLogPath;
                    return false;
                }

                if (!Directory.Exists(TCT_Lib.szLogPath))
                    Directory.CreateDirectory(TCT_Lib.szLogPath);

#if __FULL_SCREEN__
                szLogPath = TCT_Lib.szLogPath;
                //szLogPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                //szLogPath = szLogPath.Remove(szLogPath.LastIndexOf('/'));
                //szLogPath += @"/log";
#else
                szLogPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"/log";
#endif

                Console.WriteLine("+ szLogPath = " + szLogPath);
                return true;
            }
            catch (Exception)
            {
#if __FULL_SCREEN__
                //szLogPath = TCT_Lib.baseLogFolder;
                szLogPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                szLogPath = szLogPath.Remove(szLogPath.LastIndexOf('/'));
                //szLogPath += "/log";
#else
                szLogPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"/log";
#endif
                Console.WriteLine("- Exception szLogPath = " + szLogPath);
                return false;
            }
        }

        public static void WriteLog(string msg)
        {
            var laneLogPath = Path.Combine(szLogPath, "LaneLog");
            if (!Directory.Exists(laneLogPath))
                Directory.CreateDirectory(laneLogPath);
            //var monthYear   = DateTime.Today.Month.ToString("00") + "-" + DateTime.Today.Year;
            //var logPath     = Path.Combine(laneLogPath, "Log_" + monthYear);
            //if (!Directory.Exists(logPath))
            //    Directory.CreateDirectory(logPath);
            //------------------------------------------------------------------
            var todayDate   = DateTime.Today.ToString("yyyy-MM-dd");
            //var logFileName = Path.Combine(logPath, "Log_" + todayDate + ".log");
            var logFileName = Path.Combine(laneLogPath, "Log_" + todayDate + ".log");
            try
            {
                lock (_logLock)
                using (TextWriter tw = new StreamWriter(logFileName, true))
                {
                    tw.Write(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + " : ");
                    tw.WriteLine(msg);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }

        public static void WriteLaneEventIO(string msg)
        {
            var laneEventPath = Path.Combine(szLogPath, "LaneEvent");
            if (!Directory.Exists(laneEventPath))
                Directory.CreateDirectory(laneEventPath);
            var monthYear = DateTime.Today.Month.ToString("00") + "-" + DateTime.Today.Year;
            var eventPath = Path.Combine(laneEventPath, "Event_" + monthYear);
            if (!Directory.Exists(eventPath))
                Directory.CreateDirectory(eventPath);
            //------------------------------------------------------------------
            var todayDate   = DateTime.Today.ToString("yyyy-MM-dd");
            var logFileName = Path.Combine(eventPath, "EventIO_" + todayDate + ".log");

            try
            {
                lock (_logLock)
                using (TextWriter tw = new StreamWriter(logFileName, true))
                {
                    tw.Write(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + " : ");
                    tw.WriteLine(msg);
                    //tw.Flush();
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error WriteLaneEventIO : " + err.Message);
            }
        }

        public static void WriteLaneEvent(string msg)
        {
            var laneEventPath = Path.Combine(szLogPath, "LaneEvent");
            if (!Directory.Exists(laneEventPath))
                Directory.CreateDirectory(laneEventPath);
            var monthYear = DateTime.Today.Month.ToString("00") + "-" + DateTime.Today.Year;
            var eventPath = Path.Combine(laneEventPath, "Event_" + monthYear);
            if (!Directory.Exists(eventPath))
                Directory.CreateDirectory(eventPath);
            //------------------------------------------------------------------
            var todayDate   = DateTime.Today.ToString("yyyy-MM-dd");
            var logFileName = Path.Combine(eventPath, "LaneEvent_" + todayDate + ".log");
            try
            {
                lock (_logLock)
                using (TextWriter tw = new StreamWriter(logFileName, true))
                {
                    tw.Write(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + " : ");
                    tw.WriteLine(msg);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error WriteLaneEvent : " + err.Message);
            }
        }

        public static void WriteLaneTransaction(int transId, string msg)
        {
            var laneTransPath   = Path.Combine(szLogPath, "LaneTrans");
            if (!Directory.Exists(laneTransPath))
                Directory.CreateDirectory(laneTransPath);
            var monthYear       = DateTime.Today.Month.ToString("00") + "-" + DateTime.Today.Year;
            var transMonthPath  = Path.Combine(laneTransPath, "Trans_" + monthYear);    
            if (!Directory.Exists(transMonthPath))
                Directory.CreateDirectory(transMonthPath);
            var todayDate       = DateTime.Today.ToString("yyyy-MM-dd");
            var transTodayPath  = Path.Combine(transMonthPath, "Trans_" + todayDate);
            if (!Directory.Exists(transTodayPath))
                Directory.CreateDirectory(transTodayPath);
            //------------------------------------------------------------------
            var logFileName = Path.Combine(transTodayPath, "Trans_" + todayDate + "_" + transId + ".log");
            try
            {
                lock (_logLock)
                using (TextWriter tw = new StreamWriter(logFileName, true))
                {
                    tw.WriteLine(msg);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error WriteLaneTransaction : " + err.Message);
            }
        }

        public static void WriteLaneBeginJob(int staffShiftId, string msg)
        {
            var laneTransPath = Path.Combine(szLogPath, "LaneTrans");
            if (!Directory.Exists(laneTransPath))
                Directory.CreateDirectory(laneTransPath);
            var monthYear = DateTime.Today.Month.ToString("00") + "-" + DateTime.Today.Year;
            var transMonthPath = Path.Combine(laneTransPath, "Trans_" + monthYear);
            if (!Directory.Exists(transMonthPath))
                Directory.CreateDirectory(transMonthPath);
            var todayDate = DateTime.Today.ToString("yyyy-MM-dd");
            var transTodayPath = Path.Combine(transMonthPath, "Trans_" + todayDate);
            if (!Directory.Exists(transTodayPath))
                Directory.CreateDirectory(transTodayPath);
            //------------------------------------------------------------------
            var logFileName = Path.Combine(transTodayPath, "BOJ_" + todayDate + "_" + staffShiftId + ".log");
            try
            {
                lock (_logLock)
                using (TextWriter tw = new StreamWriter(logFileName, true))
                {
                    tw.WriteLine(msg);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error WriteLaneBeginJob : " + err.Message);
                LogInfo.WriteLog("- Error WriteLaneBeginJob : " + err.Message);
            }
        }

        public static void WriteLaneEndJob(int staffShiftId, string msg)
        {
            var laneTransPath = Path.Combine(szLogPath, "LaneTrans");
            if (!Directory.Exists(laneTransPath))
                Directory.CreateDirectory(laneTransPath);
            var monthYear = DateTime.Today.Month.ToString("00") + "-" + DateTime.Today.Year;
            var transMonthPath = Path.Combine(laneTransPath, "Trans_" + monthYear);
            if (!Directory.Exists(transMonthPath))
                Directory.CreateDirectory(transMonthPath);
            var todayDate = DateTime.Today.ToString("yyyy-MM-dd");
            var transTodayPath = Path.Combine(transMonthPath, "Trans_" + todayDate);
            if (!Directory.Exists(transTodayPath))
                Directory.CreateDirectory(transTodayPath);
            //------------------------------------------------------------------
            var logFileName = Path.Combine(transTodayPath, "EOJ_" + todayDate + "_" + staffShiftId + ".log");
            try
            {
                lock (_logLock)
                using (TextWriter tw = new StreamWriter(logFileName, true))
                {
                    tw.WriteLine(msg);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error WriteLaneEndJob : " + err.Message);
                LogInfo.WriteLog("- Error WriteLaneEndJob : " + err.Message);
            }
        }

        public static void WriteLaneOtlStatus(int staffShiftId, string msg)
        {
            var laneTransPath = Path.Combine(szLogPath, "LaneTrans");
            if (!Directory.Exists(laneTransPath))
                Directory.CreateDirectory(laneTransPath);
            var monthYear = DateTime.Today.Month.ToString("00") + "-" + DateTime.Today.Year;
            var transMonthPath = Path.Combine(laneTransPath, "Trans_" + monthYear);
            if (!Directory.Exists(transMonthPath))
                Directory.CreateDirectory(transMonthPath);
            var todayDate = DateTime.Today.ToString("yyyy-MM-dd");
            var transTodayPath = Path.Combine(transMonthPath, "Trans_" + todayDate);
            if (!Directory.Exists(transTodayPath))
                Directory.CreateDirectory(transTodayPath);
            var nowDateTime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            //------------------------------------------------------------------
            var logFileName = Path.Combine(transTodayPath, "OTL_" + nowDateTime + "_" + staffShiftId + ".log");

            try
            {
                lock (_logLock)
                using (TextWriter tw = new StreamWriter(logFileName, true))
                {
                    tw.WriteLine(msg);
                    //tw.Flush();
                }

            }
            catch (Exception err)
            {
                Console.WriteLine("- Error WriteLaneOtlStatus : " + err.Message);
            }
        }

        public static void WriteRecvMsgRabbitMQ(string paramName, string msg)
        {
            var laneRabbitMQPath = Path.Combine(szLogPath, "RabbitMQ");
            if (!Directory.Exists(laneRabbitMQPath))
                Directory.CreateDirectory(laneRabbitMQPath);
            var monthYear = DateTime.Today.Month.ToString("00") + "-" + DateTime.Today.Year;
            var rabbitMQMonthPath = Path.Combine(laneRabbitMQPath, "RabbitMQ_" + monthYear);
            if (!Directory.Exists(rabbitMQMonthPath))
                Directory.CreateDirectory(rabbitMQMonthPath);
            var todayDate = DateTime.Today.ToString("yyyy-MM-dd");
            var rabbitMQTodayPath = Path.Combine(rabbitMQMonthPath, "RabbitMQ_" + todayDate);
            if (!Directory.Exists(rabbitMQTodayPath))
                Directory.CreateDirectory(rabbitMQTodayPath);
            //------------------------------------------------------------------
            var logFileName = Path.Combine(rabbitMQTodayPath, paramName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + ".log");

            try
            {
                lock (_logLock)
                using (TextWriter tw = new StreamWriter(logFileName, true))
                {
                    tw.WriteLine(msg);
                    //tw.Flush();
                    LogInfo.WriteLog("+ Write Success Parameter File : " + logFileName);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error WriteRecvMsgRabbitMQ : " + err.Message);
                LogInfo.WriteLog("- Error WriteRecvMsgRabbitMQ : " + err.Message);
            }

            //------------------------------------------------------------------
            if (paramName.ToUpper().Trim() == "STAFF")  // Special for Staff file
            {
                var staffFileName = Path.Combine(laneRabbitMQPath, "STAFF.log");
                LogInfo.WriteLog("+ Write Staff.Log => " + staffFileName);
                try
                {
                    File.Delete(staffFileName);     // Delete file before save new file (Fix 2023-08-28)
                    using (TextWriter tw = new StreamWriter(staffFileName, true))
                    {
                        tw.WriteLine(msg);
                        LogInfo.WriteLog("+ Write Success Staff.log");
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine("- Error Write STAFF.log : " + err.Message);
                    LogInfo.WriteLog("- Error Write STAFF.log : " + err.Message);
                }
            }
        }

        public static void WriteEtcRabbitMQ(string msg)
        {
            var laneLogPath = Path.Combine(szLogPath, "LaneLog");
            if (!Directory.Exists(laneLogPath))
                Directory.CreateDirectory(laneLogPath);
            //var monthYear   = DateTime.Today.Month.ToString("00") + "-" + DateTime.Today.Year;
            //var logPath     = Path.Combine(laneLogPath, "Log_" + monthYear);
            //if (!Directory.Exists(logPath))
            //    Directory.CreateDirectory(logPath);
            //------------------------------------------------------------------
            var todayDate   = DateTime.Today.ToString("yyyy-MM-dd");
            //var logFileName = Path.Combine(logPath, "Log_" + todayDate + ".log");
            var logFileName = Path.Combine(laneLogPath, "ETC_" + todayDate + ".log");


            try
            {
                lock (_logLock)
                using (TextWriter tw = new StreamWriter(logFileName, true))
                {
                    tw.Write(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + " : ");
                    tw.WriteLine(msg);
                    //tw.Flush();
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }

        public static void WriteLogLSD(string msg)
        {
            var laneLogPath = Path.Combine(szLogPath, "LaneLog");
            if (!Directory.Exists(laneLogPath))
                Directory.CreateDirectory(laneLogPath);
            //var monthYear   = DateTime.Today.Month.ToString("00") + "-" + DateTime.Today.Year;
            //var logPath     = Path.Combine(laneLogPath, "Log_" + monthYear);
            //if (!Directory.Exists(logPath))
            //    Directory.CreateDirectory(logPath);
            //------------------------------------------------------------------
            var todayDate   = DateTime.Today.ToString("yyyy-MM-dd");
            //var logFileName = Path.Combine(logPath, "Log_" + todayDate + ".log");
            var logFileName = Path.Combine(laneLogPath, "LSD_" + todayDate + ".log");

            try
            {
                lock (_logLock)
                using (TextWriter tw = new StreamWriter(logFileName, true))
                {
                    tw.Write(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + " : ");
                    tw.WriteLine(msg);
                    //tw.Flush();
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }

        public static void WriteLogEDC(string msg)
        {
            var laneLogPath = Path.Combine(szLogPath, "LaneLog");
            if (!Directory.Exists(laneLogPath))
                Directory.CreateDirectory(laneLogPath);
            //var monthYear   = DateTime.Today.Month.ToString("00") + "-" + DateTime.Today.Year;
            //var logPath     = Path.Combine(laneLogPath, "Log_" + monthYear);
            //if (!Directory.Exists(logPath))
            //    Directory.CreateDirectory(logPath);
            //------------------------------------------------------------------
            var todayDate   = DateTime.Today.ToString("yyyy-MM-dd");
            //var logFileName = Path.Combine(logPath, "Log_" + todayDate + ".log");
            var logFileName = Path.Combine(laneLogPath, "EDC_" + todayDate + ".log");

            if (msg == null)
                return;

            //TextWriter tw = new StreamWriter(logFileName, true);
            try
            {
                lock (_logLock)
                using (TextWriter tw = new StreamWriter(logFileName, true, System.Text.Encoding.UTF8))
                {
                    tw.Write(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + " : ");
                    tw.WriteLine(msg);
                    //tw.Flush();
                }
            }
            catch (Exception err)
            {
                //Console.WriteLine(err.Message);
                LogInfo.WriteLog("+ EDC Msg : " + msg);
                LogInfo.WriteLog("- Fail EDC Log : " + err.Message);
                LogInfo.WriteLaneEvent("- Fail EDC Log : " + err.Message);
            }
        }

        public static void WriteLogVideoCapture(string msg)
        {
            var vdoLogPath = Path.Combine(szLogPath, "VideoCapture");
            if (!Directory.Exists(vdoLogPath))
                Directory.CreateDirectory(vdoLogPath);
            //var monthYear   = DateTime.Today.Month.ToString("00") + "-" + DateTime.Today.Year;
            //var logPath     = Path.Combine(laneLogPath, "Log_" + monthYear);
            //if (!Directory.Exists(logPath))
            //    Directory.CreateDirectory(logPath);
            //------------------------------------------------------------------
            var todayDate   = DateTime.Today.ToString("yyyy-MM-dd");
            //var logFileName = Path.Combine(logPath, "Log_" + todayDate + ".log");
            var logFileName = Path.Combine(vdoLogPath, "Capture_" + todayDate + ".log");
            try
            {
                lock (_logLock)
                using (TextWriter tw = new StreamWriter(logFileName, true))
                {
                    tw.Write(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + " : ");
                    tw.WriteLine(msg);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }

        public static void WriteLogMTS(string msg)
        {
            var mtsLogPath = Path.Combine(szLogPath, "MtsLog");
            if (!Directory.Exists(mtsLogPath))
                Directory.CreateDirectory(mtsLogPath);
            //var monthYear   = DateTime.Today.Month.ToString("00") + "-" + DateTime.Today.Year;
            //var logPath     = Path.Combine(laneLogPath, "Log_" + monthYear);
            //if (!Directory.Exists(logPath))
            //    Directory.CreateDirectory(logPath);
            //------------------------------------------------------------------
            var todayDate = DateTime.Today.ToString("yyyy-MM-dd");
            //var logFileName = Path.Combine(logPath, "Log_" + todayDate + ".log");
            var logFileName = Path.Combine(mtsLogPath, "MTS_" + todayDate + ".log");
            try
            {
                lock (_logLock)
                using (TextWriter tw = new StreamWriter(logFileName, true))
                {
                    tw.Write(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + " : ");
                    tw.WriteLine(msg);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }

        public static void WriteLogNetworkCamera(int idx, string msg)
        {
            var cameraLogPath = Path.Combine(szLogPath, "CameraLog");
            if (!Directory.Exists(cameraLogPath))
                Directory.CreateDirectory(cameraLogPath);
            //var monthYear   = DateTime.Today.Month.ToString("00") + "-" + DateTime.Today.Year;
            //var logPath     = Path.Combine(laneLogPath, "Log_" + monthYear);
            //if (!Directory.Exists(logPath))
            //    Directory.CreateDirectory(logPath);
            //------------------------------------------------------------------
            var todayDate   = DateTime.Today.ToString("yyyy-MM-dd");
            //var logFileName = Path.Combine(logPath, "Log_" + todayDate + ".log");
            var logFileName = Path.Combine(cameraLogPath, $"Camera{idx}_" + todayDate + ".log");
            try
            {
                lock (_logLock)
                using (TextWriter tw = new StreamWriter(logFileName, true))
                {
                    tw.Write(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + " : ");
                    tw.WriteLine(msg);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }
    }
}
