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

    //--- v2.0.4: smaller = more important. A line is written when its level <= LogInfo.eLogLevel
    public enum LogLevel
    {
        ERROR   = 1,
        WARN    = 2,
        INFO    = 3,
        DEBUG   = 4,
        TRACE   = 5,
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

        //=== v2.0.4: log level ==========================================================================
        //--- TCT.config -> "Config": { "LogLevel": "INFO" }   (ERROR / WARN / INFO / DEBUG / TRACE)
        //---   ERROR : failed operations only
        //---   WARN  : + unusual but handled (retry, timeout, ignored input)
        //---   INFO  : + main events (default = same lines as before v2.0.4)
        //---   DEBUG : + every step / decision / state change / value used
        //---   TRACE : + raw data (socket frames, hex dump, full JSON, SQL text)
        //--- Old calls WriteLog(msg) pick the level from the text: "- Error / Fail / Exception" = ERROR,
        //--- "- Warning" = WARN, else INFO. Data files (LaneTrans, BOJ/EOJ, OTL, RabbitMQ parameters) are
        //--- records, not logs -> always written, no level tag.
        public static LogLevel eLogLevel    = LogLevel.INFO;

        public static bool IsEnabled(LogLevel eLevel)
        {
            return eLevel <= eLogLevel;
        }

        //--- v2.0.4: never write a secret to any log. Shows only whether it is set and how long it is.
        public static string MaskSecret(string szSecret)
        {
            if (string.IsNullOrEmpty(szSecret))
                return "<not set>";
            return $"***** (len {szSecret.Length})";
        }

        //--- v2.0.4: hide "user:password@" inside a URL (camera / NVR URLs may embed credentials)
        public static string MaskUrl(string szUrl)
        {
            if (string.IsNullOrEmpty(szUrl))
                return szUrl;
            try
            {
                return System.Text.RegularExpressions.Regex.Replace(szUrl, @"(?<=://)[^/@\s]+:[^/@\s]*@", "*****@");
            }
            catch (Exception)
            {
                return "<url hidden>";
            }
        }

        public static LogLevel ParseLevel(string szLevel, LogLevel eDefault = LogLevel.INFO)
        {
            if (string.IsNullOrWhiteSpace(szLevel))
                return eDefault;
            szLevel = szLevel.Trim().ToUpperInvariant();
            if (szLevel == "WARNING")
                return LogLevel.WARN;
            int iLevel;
            if (int.TryParse(szLevel, out iLevel) && iLevel >= (int)LogLevel.ERROR && iLevel <= (int)LogLevel.TRACE)
                return (LogLevel)iLevel;
            LogLevel eLevel;
            return Enum.TryParse(szLevel, out eLevel) && Enum.IsDefined(typeof(LogLevel), eLevel) ? eLevel : eDefault;
        }

        public static LogLevel DetectLevel(string msg)
        {
            if (msg == null)
                return LogLevel.INFO;
            string m = msg.TrimStart(' ', '\t', '-', '+', '*', '=', '>', '!').ToLowerInvariant();
            if (m.StartsWith("error") || m.StartsWith("fail") || m.StartsWith("exception") || m.StartsWith("cannot") ||
                m.StartsWith("can't") || m.StartsWith("unable") || m.StartsWith("ไม่สามารถ"))
                return LogLevel.ERROR;
            if (m.StartsWith("warn"))
                return LogLevel.WARN;
            if (msg.TrimStart().StartsWith("-"))
            {
                //--- "- Lane Socket Error : ..." / "- ProcessXxx Exception : ..."
                string szHead = m.Length > 60 ? m.Substring(0, 60) : m;
                if (szHead.Contains("error") || szHead.Contains("exception") || szHead.Contains("fail"))
                    return LogLevel.ERROR;
                if (szHead.Contains("warn"))
                    return LogLevel.WARN;
            }
            return LogLevel.INFO;
        }

        static string LevelTag(LogLevel eLevel)
        {
            switch (eLevel)
            {
                case LogLevel.ERROR:    return "[ERROR] ";
                case LogLevel.WARN:     return "[WARN ] ";
                case LogLevel.DEBUG:    return "[DEBUG] ";
                case LogLevel.TRACE:    return "[TRACE] ";
                default:                return "[INFO ] ";
            }
        }

        //--- one place for every timed log line: level filter + folder + lock + format
        static void WriteLine(string szFolder, string szFileName, LogLevel eLevel, string msg, System.Text.Encoding enc = null)
        {
            if (msg == null || eLevel > eLogLevel)
                return;
            if (!Directory.Exists(szFolder))
                Directory.CreateDirectory(szFolder);
            string logFileName = Path.Combine(szFolder, szFileName);
            lock (_logLock)
            using (TextWriter tw = enc == null ? new StreamWriter(logFileName, true) : new StreamWriter(logFileName, true, enc))
            {
                tw.Write(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") + " : " + LevelTag(eLevel));
                tw.WriteLine(msg);
            }
        }

        //--- records (no time / no level): LaneTrans, BOJ, EOJ, OTL
        static void WriteRecord(string szFileName, string msg)
        {
            var laneTransPath   = Path.Combine(szLogPath, "LaneTrans");
            var transMonthPath  = Path.Combine(laneTransPath, "Trans_" + MonthYear());
            var todayDate       = DateTime.Today.ToString("yyyy-MM-dd");
            var transTodayPath  = Path.Combine(transMonthPath, "Trans_" + todayDate);
            if (!Directory.Exists(transTodayPath))
                Directory.CreateDirectory(transTodayPath);
            //------------------------------------------------------------------
            lock (_logLock)
            using (TextWriter tw = new StreamWriter(Path.Combine(transTodayPath, szFileName), true))
            {
                tw.WriteLine(msg);
            }
        }

        static string LaneEventFolder()
        {
            return Path.Combine(szLogPath, "LaneEvent", "Event_" + MonthYear());
        }

        static string MonthYear()
        {
            return DateTime.Today.Month.ToString("00") + "-" + DateTime.Today.Year;     // same folder name as before v2.0.4
        }

        static string Today()
        {
            return DateTime.Today.ToString("yyyy-MM-dd");
        }

        //=== LaneLog/Log_yyyy-MM-dd.log =================================================================
        public static void WriteLog(string msg)                     { WriteLog(DetectLevel(msg), msg); }
        public static void WriteLog(LogLevel eLevel, string msg)
        {
            try
            {
                WriteLine(Path.Combine(szLogPath, "LaneLog"), "Log_" + Today() + ".log", eLevel, msg);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }

        //=== LaneEvent/Event_MM-yyyy/EventIO_yyyy-MM-dd.log =============================================
        public static void WriteLaneEventIO(string msg)             { WriteLaneEventIO(DetectLevel(msg), msg); }
        public static void WriteLaneEventIO(LogLevel eLevel, string msg)
        {
            try
            {
                WriteLine(LaneEventFolder(), "EventIO_" + Today() + ".log", eLevel, msg);
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error WriteLaneEventIO : " + err.Message);
            }
        }

        //=== LaneEvent/Event_MM-yyyy/LaneEvent_yyyy-MM-dd.log ===========================================
        public static void WriteLaneEvent(string msg)               { WriteLaneEvent(DetectLevel(msg), msg); }
        public static void WriteLaneEvent(LogLevel eLevel, string msg)
        {
            try
            {
                WriteLine(LaneEventFolder(), "LaneEvent_" + Today() + ".log", eLevel, msg);
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error WriteLaneEvent : " + err.Message);
            }
        }

        public static void WriteLaneTransaction(int transId, string msg)
        {
            try
            {
                WriteRecord("Trans_" + Today() + "_" + transId + ".log", msg);
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error WriteLaneTransaction : " + err.Message);
            }
        }

        public static void WriteLaneBeginJob(int staffShiftId, string msg)
        {
            try
            {
                WriteRecord("BOJ_" + Today() + "_" + staffShiftId + ".log", msg);
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error WriteLaneBeginJob : " + err.Message);
                LogInfo.WriteLog(LogLevel.ERROR, "- Error WriteLaneBeginJob : " + err.Message);
            }
        }

        public static void WriteLaneEndJob(int staffShiftId, string msg)
        {
            try
            {
                WriteRecord("EOJ_" + Today() + "_" + staffShiftId + ".log", msg);
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error WriteLaneEndJob : " + err.Message);
                LogInfo.WriteLog(LogLevel.ERROR, "- Error WriteLaneEndJob : " + err.Message);
            }
        }

        public static void WriteLaneOtlStatus(int staffShiftId, string msg)
        {
            try
            {
                WriteRecord("OTL_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_" + staffShiftId + ".log", msg);
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error WriteLaneOtlStatus : " + err.Message);
            }
        }

        public static void WriteRecvMsgRabbitMQ(string paramName, string msg)
        {
            var laneRabbitMQPath    = Path.Combine(szLogPath, "RabbitMQ");
            var rabbitMQTodayPath   = Path.Combine(laneRabbitMQPath, "RabbitMQ_" + MonthYear(), "RabbitMQ_" + Today());
            //------------------------------------------------------------------
            var logFileName = Path.Combine(rabbitMQTodayPath, paramName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + ".log");

            try
            {
                if (!Directory.Exists(rabbitMQTodayPath))
                    Directory.CreateDirectory(rabbitMQTodayPath);
                lock (_logLock)
                using (TextWriter tw = new StreamWriter(logFileName, true))
                {
                    tw.WriteLine(msg);
                }
                LogInfo.WriteLog("+ Write Success Parameter File : " + logFileName);
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
                    }
                    LogInfo.WriteLog("+ Write Success Staff.log");
                }
                catch (Exception err)
                {
                    Console.WriteLine("- Error Write STAFF.log : " + err.Message);
                    LogInfo.WriteLog("- Error Write STAFF.log : " + err.Message);
                }
            }
        }

        //=== LaneLog/ETC_yyyy-MM-dd.log =================================================================
        public static void WriteEtcRabbitMQ(string msg)             { WriteEtcRabbitMQ(DetectLevel(msg), msg); }
        public static void WriteEtcRabbitMQ(LogLevel eLevel, string msg)
        {
            try
            {
                WriteLine(Path.Combine(szLogPath, "LaneLog"), "ETC_" + Today() + ".log", eLevel, msg);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }

        //=== LaneLog/LSD_yyyy-MM-dd.log =================================================================
        public static void WriteLogLSD(string msg)                  { WriteLogLSD(DetectLevel(msg), msg); }
        public static void WriteLogLSD(LogLevel eLevel, string msg)
        {
            try
            {
                WriteLine(Path.Combine(szLogPath, "LaneLog"), "LSD_" + Today() + ".log", eLevel, msg);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }

        //=== LaneLog/EDC_yyyy-MM-dd.log =================================================================
        public static void WriteLogEDC(string msg)                  { WriteLogEDC(DetectLevel(msg), msg); }
        public static void WriteLogEDC(LogLevel eLevel, string msg)
        {
            if (msg == null)
                return;
            try
            {
                WriteLine(Path.Combine(szLogPath, "LaneLog"), "EDC_" + Today() + ".log", eLevel, msg, System.Text.Encoding.UTF8);
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("+ EDC Msg : " + msg);
                LogInfo.WriteLog("- Fail EDC Log : " + err.Message);
                LogInfo.WriteLaneEvent("- Fail EDC Log : " + err.Message);
            }
        }

        //=== VideoCapture/Capture_yyyy-MM-dd.log ========================================================
        public static void WriteLogVideoCapture(string msg)         { WriteLogVideoCapture(DetectLevel(msg), msg); }
        public static void WriteLogVideoCapture(LogLevel eLevel, string msg)
        {
            try
            {
                WriteLine(Path.Combine(szLogPath, "VideoCapture"), "Capture_" + Today() + ".log", eLevel, msg);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }

        //=== MtsLog/MTS_yyyy-MM-dd.log ==================================================================
        public static void WriteLogMTS(string msg)                  { WriteLogMTS(DetectLevel(msg), msg); }
        public static void WriteLogMTS(LogLevel eLevel, string msg)
        {
            try
            {
                WriteLine(Path.Combine(szLogPath, "MtsLog"), "MTS_" + Today() + ".log", eLevel, msg);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }

        //=== CameraLog/Camera{idx}_yyyy-MM-dd.log =======================================================
        public static void WriteLogNetworkCamera(int idx, string msg)   { WriteLogNetworkCamera(idx, DetectLevel(msg), msg); }
        public static void WriteLogNetworkCamera(int idx, LogLevel eLevel, string msg)
        {
            try
            {
                WriteLine(Path.Combine(szLogPath, "CameraLog"), $"Camera{idx}_" + Today() + ".log", eLevel, msg);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }
    }
}
