using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TCT_App.RabbitMQ;

namespace TCT_App
{
    public class TCT_MTS
    {
        private static  PerformanceCounter cpuCounter;
        private static  PerformanceCounter ramCounter;
        private static Thread threadGetEtcStatus;

        private static int iMaxSkipUrl      = 6;
        private static int iCountingUrl     = 0;
        private static bool bFoudErrUrl     = false;

        static string szObuValidFrom = "", szObuListCount = "", szObuVersion = "";
        static string szBlackListValidFrom = "", szBlackListListCount = "", szBlackListVersion = "";
        static string szTpsState = "";

        static string szXmr1StatusListRecord = "", szXmr1ObuValidFrom = "";
        static string szXmr1BlackListRecord = "", szXmr1BlackListValidFrom = "";

        static string szXmr2StatusListRecord = "", szXmr2ObuValidFrom = "";
        static string szXmr2BlackListRecord = "", szXmr2BlackListValidFrom = "";

        static string szLaneMode = "", szXmr1State = "", szXmr2State = "";
        static string szPresenceDetState = "", szPassageDetState = "";
        static string szBangkokDisplayState = "", szGrabberState = "", szBangkokLcState = "";

        //static string szUrlXMR1 = "http://10.0.0.10/XMRFirst/MuninBrowse/StatusDB";
        //static string szUrlXMR2 = "http://10.0.0.10/XMRSecond/MuninBrowse/StatusDB";


        //-----------------------------------------------------
        public static void InitialProcessUsage()
        {
            LogInfo.WriteLogMTS(LogLevel.DEBUG, "+ InitialProcessUsage : create CPU/RAM PerformanceCounter");
            cpuCounter = new PerformanceCounter(
                                "Processor",
                                "% Processor Time",
                                "_Total",
                                true
                                );

            //------------------------------------------------------
            ramCounter = new PerformanceCounter("Memory", "Available MBytes", true);
            LogInfo.WriteLogMTS(LogLevel.DEBUG, "+ InitialProcessUsage : done");
        }

        private static float GetProcessCpuUsage()
        {
            try
            {
                float fCpuUsage = cpuCounter.NextValue();
                LogInfo.WriteLogMTS("+ CPU Usage : " + fCpuUsage.ToString("0.00") + "%");

                return float.Parse(fCpuUsage.ToString("0.00"));
            }
            catch (Exception err)
            {
                LogInfo.WriteLogMTS("- Fail GetProcessCpuUsage : " + err.Message);
                LogInfo.WriteLogMTS(LogLevel.DEBUG, $"- GetProcessCpuUsage : cpuCounter={(cpuCounter == null ? "null" : "ok")}, stack={err.StackTrace}");
                return -1;
            }
        }

        private static float GetProcessMemoryUsage()
        {
            try
            {
                Process currentProcess  = Process.GetCurrentProcess();
                long usedMemory         = currentProcess.PrivateMemorySize64;
                LogInfo.WriteLogMTS("+ Memory Usage : " + (usedMemory / (1024 * 1024)).ToString("0.00") + " MB.");

                return float.Parse((usedMemory / (1024 * 1024)).ToString("0.00"));
            }
            catch (Exception err)
            {
                LogInfo.WriteLogMTS("- Fail GetProcessMemoryUsage: " + err.Message);
                LogInfo.WriteLogMTS(LogLevel.DEBUG, $"- GetProcessMemoryUsage : stack={err.StackTrace}");
                return -1;
            }
        }

        private static bool GetDiskSpace(string szPath, ref string szRefPath, ref long fTotalDisk, ref long fTotalUsed, ref long fTotalFree, ref long fPercentUsed)
        {
            if (szPath.Trim() == "")
                szPath = "/";

            //LogInfo.WriteLogMTS("GetDiskSpace : " + Bash("df "+szPath));
            string szResult     = Bash("df -k " + szPath);
            if (szResult == "")
                LogInfo.WriteLogMTS(LogLevel.WARN, $"- Warning GetDiskSpace : empty df output, Path={szPath} -> skip");
            if (szResult == "")
                return false;
            string szData       = szResult.Split('\n')[1];
            string[] szAryResult = szData.Split(' ');
            var xListResult     = new List<string>();
            foreach (var result in szAryResult.ToList())
            {
                if (result.Trim() != "")
                    xListResult.Add(result);
            }

            if (xListResult.Count != 6)
                LogInfo.WriteLogMTS(LogLevel.WARN, $"- Warning GetDiskSpace : unexpected df columns={xListResult.Count} (expect 6), Path={szPath}, line=[{szData}] -> skip");
            if (xListResult.Count != 6)
                return false;

            szRefPath       = xListResult[0];
            fTotalDisk      = long.Parse(xListResult[1]);
            fTotalUsed      = long.Parse(xListResult[2]);
            fTotalFree      = long.Parse(xListResult[3]);
            fPercentUsed    = long.Parse(xListResult[4].Replace("%", ""));

            LogInfo.WriteLogMTS($"+ GetDiskSpace : Path={szPath}, Result: RefPath={szRefPath}, TotalDisk={fTotalDisk}, TotalUsed={fTotalUsed}, TotalFree={fTotalFree}, PercentUsed={fPercentUsed}%");

            return true;
        }

        private static bool GetDiskPath(string szPath, ref string szRefPath, ref long fTotalDisk, ref long fTotalUsed, ref long fTotalFree, ref long fPercentUsed)
        {
            if (szPath.Trim() == "")
                szPath = "/";

            //LogInfo.WriteLogMTS("GetDiskSpace : " + Bash("df "+szPath));
            string szResult     = Bash("df -k /");
            string szData       = szResult.Split('\n')[1];
            string[] szAryResult = szData.Split(' ');
            var xListResult     = new List<string>();
            foreach (var result in szAryResult.ToList())
            {
                if (result.Trim() != "")
                    xListResult.Add(result);
            }

            if (xListResult.Count != 6)
                LogInfo.WriteLogMTS(LogLevel.WARN, $"- Warning GetDiskPath : unexpected df columns={xListResult.Count} (expect 6), line=[{szData}] -> skip");
            if (xListResult.Count != 6)
                return false;

            szRefPath       = xListResult[0];
            fTotalDisk      = long.Parse(xListResult[1]);
            fTotalUsed      = long.Parse(xListResult[2]);
            fTotalFree      = long.Parse(xListResult[3]);
            fPercentUsed    = long.Parse(xListResult[4].Replace("%", ""));

            LogInfo.WriteLogMTS($"+ GetDiskSpace : Path={szPath}, Result: RefPath={szRefPath}, TotalDisk={fTotalDisk}, TotalUsed={fTotalUsed}, TotalFree={fTotalFree}, PercentUsed={fPercentUsed}%");

            return true;
        }

        private static string Bash(string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            try
            {
                var process     = new Process()
                {
                    StartInfo   = new ProcessStartInfo
                    {
                        FileName                = "/bin/bash",
                        Arguments               = $"-c \"{escapedArgs}\"",
                        RedirectStandardOutput  = true,
                        UseShellExecute         = false,
                        CreateNoWindow          = true,
                    }
                };
                var swBash = Stopwatch.StartNew();
                process.Start();
                string result = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                LogInfo.WriteLogMTS(LogLevel.DEBUG, $"+ Bash : cmd=[{cmd}], exit={process.ExitCode}, outLen={result?.Length ?? 0} ({swBash.ElapsedMilliseconds} ms)");
                LogInfo.WriteLogMTS(LogLevel.TRACE, $"+ Bash : cmd=[{cmd}] output=\n{result}");
                return result;
            }
            catch (Exception err)
            {
                LogInfo.WriteLogMTS("- Fail Bash GetDiskSpace : cmd=" + cmd + ", Error=" + err.Message);
                LogInfo.WriteLogMTS(LogLevel.DEBUG, $"- Bash : stack={err.StackTrace}");
                return "";
            }
        }

        private static long GetDirectorySize(string fullDirectoryPath)
        {
            long startDirectorySize = 0;
            if (!Directory.Exists(fullDirectoryPath))
                return startDirectorySize; //Return 0 while Directory does not exist.

            var currentDirectory = new DirectoryInfo(fullDirectoryPath);
            //Add size of files in the Current Directory to main size.
            currentDirectory.GetFiles().ToList().ForEach(f => startDirectorySize += f.Length);

            //Loop on Sub Direcotries in the Current Directory and Calculate it's files size.
            currentDirectory.GetDirectories().ToList()
                .ForEach(d => startDirectorySize += GetDirectorySize(d.FullName));

            return startDirectorySize;  //Return full Size of this Directory.
        }

        private static long GetUptimeSec()
        {
            var uptimeText = File.ReadAllText("/proc/uptime");
            // Now split the string to extract the first component,
            // parse it as double and use TimeSpan.FromSeconds()
            double upTimeSec = double.Parse(uptimeText.Split(' ')[0]);

            return (long)upTimeSec;
        }

        private static bool GetMemoryUsage(ref long fTotalMem, ref long fTotalUsed)
        {
            string szResult     = Bash("free -k");
            string szData       = szResult.Split('\n')[1];
            string[] szAryResult = szData.Split(' ');
            var szResultMem     = Array.FindAll(szAryResult, data => data != "");
            fTotalMem           = long.Parse(szResultMem[1]);
            fTotalUsed          = long.Parse(szResultMem[2]);
            LogInfo.WriteLogMTS(LogLevel.DEBUG, $"+ GetMemoryUsage : TotalMem={fTotalMem} kB, Used={fTotalUsed} kB");

            return true;
        }

        private static int GetTotalOutBoxFile()
        {
            return TCT_Lib.LaneDB.GetTotalUnsendTransToDC();
        }

        //------------------------------------------
        private static bool GetUsbDeviceStatus(ref bool bPrinterStatus, ref bool bScrStastus, ref bool bKbStatus)
        {
            bPrinterStatus = bScrStastus = bKbStatus = false;

            string szResult         = Bash("usb-devices");
            string[] szAryResult    = szResult.Split('\n');

            foreach(string szData in szAryResult.ToList())
            {
                if (szData.Contains("Product=" + TCT_Lib.szMTS_Monitor_Product_Printer))
                    bPrinterStatus = true;
                if (szData.Contains("Product=" + TCT_Lib.szMTS_Monitor_Product_SCR))
                    bScrStastus = true;
                if (szData.Contains("Product=" + TCT_Lib.szMTS_Monitor_Product_Keyboard))
                    bKbStatus = true;
            }
            LogInfo.WriteLogMTS(LogLevel.DEBUG, $"+ GetUsbDeviceStatus : Printer[{TCT_Lib.szMTS_Monitor_Product_Printer}]={bPrinterStatus}, SCR[{TCT_Lib.szMTS_Monitor_Product_SCR}]={bScrStastus}, Keyboard[{TCT_Lib.szMTS_Monitor_Product_Keyboard}]={bKbStatus}");

            return true;
        }


        //===========================================
        async public static Task<string> SendJsonMsgMtsDataToRabbitMQ()
        {
            try
            {
                var xMtsDataJson            = new MtsDataJson();

                xMtsDataJson.jsonrpc        = "2.0";
                xMtsDataJson.method         = "MTS-Data";

                LogInfo.WriteLogMTS("+ Get MTS Data");
                var swMts                   = Stopwatch.StartNew();
                var objMtsData              = new ObjMtsData();
                objMtsData.requestId        = "MTS"+DateTime.Now.ToString("yyyyMMddHHmmssfff");
                objMtsData.requestDateTime  = TCT_Lib.DateTime_ISO8601(DateTime.Now);
                objMtsData.plaza            = GetPlazaData();
                objMtsData.lane             = GetLaneData();
                objMtsData.resourceInfo     = GetResouceInfo();
                objMtsData.diskInfo         = GetDiskInfo();
                objMtsData.usageDiskPaths   = GetUsageDiskPaths();
                objMtsData.appStatus        = await GetAppStatus();
                objMtsData.deviceStatus     = await GetDeviceStatus();
                objMtsData.cameraStatus     = GetCameraStatus();
                objMtsData.lastAvcStatus    = TCT_Lib.iLastAvcStatus;
                objMtsData.outBoxFile       = GetTotalOutBoxFile();
                LogInfo.WriteLogMTS(LogLevel.DEBUG, $"+ SendJsonMsgMtsDataToRabbitMQ : collected status ({swMts.ElapsedMilliseconds} ms), lastAvcStatus={objMtsData.lastAvcStatus}, outBoxFile={objMtsData.outBoxFile}, usageDiskPaths={objMtsData.usageDiskPaths?.Count ?? 0}");

                //------------------------------------------------------
                LogInfo.WriteLogMTS("+ Get ETC Status");
                int? iStatusListVersion = null, iBlackListVersion = null;

/*
                if (TCT_Lib.eCurLaneType == LaneType.MTC)
                {
                    iStatusListVersion                  = null;
                    objMtsData.statusListRecord         = null;
                    objMtsData.obuValidFrom             = null;

                    iBlackListVersion                   = null;
                    objMtsData.blackListRecord          = null;
                    objMtsData.blackListValidFrom       = null;

                    objMtsData.xmr1StatusListRecord     = 0;
                    objMtsData.xmr1ObuValidFrom         = null;
                    objMtsData.xmr1BlackListRecord      = 0;
                    objMtsData.xmr1BlackListValidFrom   = null;

                    objMtsData.xmr2StatusListRecord     = 0;
                    objMtsData.xmr2ObuValidFrom         = null;
                    objMtsData.xmr2BlackListRecord      = 0;
                    objMtsData.xmr2BlackListValidFrom   = null;

                    objMtsData.laneMode                 = null;
                    objMtsData.xmr1State                = null;
                    objMtsData.xmr2State                = null;
                    objMtsData.presenceDetState         = null;
                    objMtsData.passageDetState          = null;
                    objMtsData.bangkokDisplayState      = null;
                    objMtsData.grabberState             = null;
                    objMtsData.bangkokLcState           = null;
                }
                else
*/                
                try{
                    iStatusListVersion                  = szObuVersion == "" ? 0 : int.Parse(szObuVersion);
                    objMtsData.statusListRecord         = szObuListCount == "" ? 0 : int.Parse(szObuListCount);
                    objMtsData.obuValidFrom             = szObuValidFrom == "" ? 0 : long.Parse(szObuValidFrom);

                    //iBlackListVersion = szBlackListVersion == "" ? 0 : int.Parse(szBlackListVersion);
                    int iVersion;
                    if (int.TryParse(szBlackListVersion, out iVersion))
                        iBlackListVersion = iVersion;
                    else
                        iBlackListVersion = 0;

                    objMtsData.blackListRecord          = szBlackListListCount == "" ? 0 : int.Parse(szBlackListListCount);
                    objMtsData.blackListValidFrom       = szBlackListValidFrom == "" ? 0 : long.Parse(szBlackListValidFrom);
                    objMtsData.tpsState                 = szTpsState;

                    objMtsData.xmr1StatusListRecord     = szXmr1StatusListRecord == "" ? 0 : int.Parse(szXmr1StatusListRecord);
                    objMtsData.xmr1ObuValidFrom         = szXmr1ObuValidFrom == "" ? 0 : long.Parse(szXmr1ObuValidFrom);
                    objMtsData.xmr1BlackListRecord      = szXmr1BlackListRecord == "" ? 0 : int.Parse(szXmr1BlackListRecord);
                    objMtsData.xmr1BlackListValidFrom   = szXmr1BlackListValidFrom == "" ? 0 : long.Parse(szXmr1BlackListValidFrom);

                    objMtsData.xmr2StatusListRecord     = szXmr2StatusListRecord == "" ? 0 : int.Parse(szXmr2StatusListRecord);
                    objMtsData.xmr2ObuValidFrom         = szXmr2ObuValidFrom == "" ? 0 : long.Parse(szXmr2ObuValidFrom);
                    objMtsData.xmr2BlackListRecord      = szXmr2BlackListRecord == "" ? 0 : int.Parse(szXmr2BlackListRecord);
                    objMtsData.xmr2BlackListValidFrom   = szXmr2BlackListValidFrom == "" ? 0 : long.Parse(szXmr2BlackListValidFrom);

                    objMtsData.laneMode                 = szLaneMode;
                    objMtsData.xmr1State                = szXmr1State;
                    objMtsData.xmr2State                = szXmr2State;
                    objMtsData.presenceDetState         = szPresenceDetState;
                    objMtsData.passageDetState          = szPassageDetState;
                    objMtsData.bangkokDisplayState      = szBangkokDisplayState;
                    objMtsData.grabberState             = szGrabberState;
                    objMtsData.bangkokLcState           = szBangkokLcState;

                }
                catch (Exception err)
                {
                    LogInfo.WriteLogMTS("- Fail GenerateJsonMsgData : " + err.Message);
                    LogInfo.WriteLogMTS(LogLevel.WARN, $"- Warning SendJsonMsgMtsDataToRabbitMQ : bad ETC status value (ObuVersion={szObuVersion}, ObuListCount={szObuListCount}, ObuValidFrom={szObuValidFrom}, BlackListCount={szBlackListListCount}, BlackListValidFrom={szBlackListValidFrom}) -> ETC numbers set to 0");
                    iStatusListVersion                  = 0;
                    objMtsData.statusListRecord         = 0;
                    objMtsData.obuValidFrom             = 0;

                    iBlackListVersion                   = 0;
                    objMtsData.blackListRecord          = 0;
                    objMtsData.blackListValidFrom       = 0;
                    objMtsData.tpsState                 = szTpsState;

                    objMtsData.xmr1StatusListRecord     = 0;
                    objMtsData.xmr1ObuValidFrom         = 0;
                    objMtsData.xmr1BlackListRecord      = 0;
                    objMtsData.xmr1BlackListValidFrom   = 0;

                    objMtsData.xmr2StatusListRecord     = 0;
                    objMtsData.xmr2ObuValidFrom         = 0;
                    objMtsData.xmr2BlackListRecord      = 0;
                    objMtsData.xmr2BlackListValidFrom   = 0;

                    objMtsData.laneMode                 = szLaneMode;
                    objMtsData.xmr1State                = szXmr1State;
                    objMtsData.xmr2State                = szXmr2State;
                    objMtsData.presenceDetState         = szPresenceDetState;
                    objMtsData.passageDetState          = szPassageDetState;
                    objMtsData.bangkokDisplayState      = szBangkokDisplayState;
                    objMtsData.grabberState             = szGrabberState;
                    objMtsData.bangkokLcState           = szBangkokLcState;
                }

                objMtsData.fileVersions         = GetListFileVersions(iStatusListVersion, iBlackListVersion);

                xMtsDataJson.@params            = objMtsData;

                string szMtsData                    = JsonConvert.SerializeObject(xMtsDataJson, Formatting.Indented);
                LogInfo.WriteLogMTS("+ MTS-Data : " + szMtsData);

                var swSend = Stopwatch.StartNew();
                bool bSendMts = TCT_Lib.mtsRabbitMQ.SendMtsMsgToRabbitMQ("MTS-Data", szMtsData);
                LogInfo.WriteLogMTS(bSendMts ? LogLevel.DEBUG : LogLevel.WARN, $"{(bSendMts ? "+" : "- Warning")} SendJsonMsgMtsDataToRabbitMQ : send MTS-Data result={bSendMts} ({swSend.ElapsedMilliseconds} ms), total {swMts.ElapsedMilliseconds} ms");

                return szMtsData;
            }
            catch (Exception err)
            {
                LogInfo.WriteLogMTS("- Fail GenerateJsonMsgData : " + err.Message);
                LogInfo.WriteLogMTS(LogLevel.DEBUG, $"- SendJsonMsgMtsDataToRabbitMQ : mtsRabbitMQ={(TCT_Lib.mtsRabbitMQ == null ? "null" : "ok")}, stack={err.StackTrace}");
                return "";
            }
        }

        private static RabbitMQ.Plaza GetPlazaData()
        {
            var xPlaza          = new RabbitMQ.Plaza();
            xPlaza.plazaId      = TCT_Lib.szPlazaID;
            xPlaza.plazaName    = TCT_Lib.szPlazaName;

            return xPlaza;
        }

        private static RabbitMQ.Lane GetLaneData()
        {
            var xLane           = new RabbitMQ.Lane();
            xLane.laneId        = TCT_Lib.iLaneID.ToString();
            xLane.laneName      = TCT_Lib.szLaneName;

            return xLane;
        }

        private static ResourceInfo GetResouceInfo()
        {
            long fTotalMem = 0, fTotalUsedMem = 0;
            GetMemoryUsage(ref fTotalMem, ref fTotalUsedMem);

            try
            {
                var xResource               = new ResourceInfo();
                xResource.cpuPercentAvg     = GetProcessCpuUsage();
                xResource.memoryTotalMb     = fTotalMem;
                xResource.memoryUsedMb      = fTotalUsedMem;
                xResource.memoryPercentUsed = (fTotalUsedMem * 100) / fTotalMem;
                xResource.upTimeSec         = GetUptimeSec();
                LogInfo.WriteLogMTS(LogLevel.DEBUG, $"+ GetResouceInfo : cpu={xResource.cpuPercentAvg}%, mem={fTotalUsedMem}/{fTotalMem} kB ({xResource.memoryPercentUsed}%), upTimeSec={xResource.upTimeSec}");

                return xResource;
            }
            catch (Exception err)
            {
                LogInfo.WriteLogMTS("- Fail GetResouceInfo =" + err.Message);
                LogInfo.WriteLogMTS(LogLevel.DEBUG, $"- GetResouceInfo : TotalMem={fTotalMem}, stack={err.StackTrace}");
                return null;
            }
        }

        private static DiskInfo GetDiskInfo()
        {
            try
            {
                string szRefPath = "";
                long fTotalDisk = 0, fTotalUsed = 0, fTotalFree = 0, fPercentUsed = 0;
                if (GetDiskSpace("/", ref szRefPath, ref fTotalDisk, ref fTotalUsed, ref fTotalFree, ref fPercentUsed))
                {
                    var xDiskInfo           = new DiskInfo();
                    xDiskInfo.diskPath      = "/";
                    xDiskInfo.spaceKb       = fTotalDisk;
                    xDiskInfo.usageKb       = fTotalUsed;
                    xDiskInfo.freeKb        = fTotalFree;
                    xDiskInfo.percentUsed   = float.Parse(fPercentUsed.ToString("0.00"));

                    return xDiskInfo;
                }
                else
                {
                    LogInfo.WriteLogMTS(LogLevel.DEBUG, "+ GetDiskInfo : GetDiskSpace(/) failed -> diskInfo=null");
                    return null;
                }
            }
            catch (Exception err)
            {
                LogInfo.WriteLogMTS("- Fail GetDiskInfo =" + err.Message);
                LogInfo.WriteLogMTS(LogLevel.DEBUG, $"- GetDiskInfo : stack={err.StackTrace}");
                return null;
            }
        }

        private static List<UsageDiskPath> GetUsageDiskPaths()
        {
            var xUsageDiskPaths = new List<UsageDiskPath>();

            try
            {
                var xDiskPath01 = GetUsageDiskPath(TCT_Lib.szMTS_Monitor_DiskPath_01);
                var xDiskPath02 = GetUsageDiskPath(TCT_Lib.szMTS_Monitor_DiskPath_02);
                var xDiskPath03 = GetUsageDiskPath(TCT_Lib.szMTS_Monitor_DiskPath_03);
                var xDiskPath04 = GetUsageDiskPath(TCT_Lib.szMTS_Monitor_DiskPath_04);
                var xDiskPath05 = GetUsageDiskPath(TCT_Lib.szMTS_Monitor_DiskPath_05);
                var xDiskPath06 = GetUsageDiskPath(TCT_Lib.szMTS_Monitor_DiskPath_06);
                var xDiskPath07 = GetUsageDiskPath(TCT_Lib.szMTS_Monitor_DiskPath_07);
                var xDiskPath08 = GetUsageDiskPath(TCT_Lib.szMTS_Monitor_DiskPath_08);
                var xDiskPath09 = GetUsageDiskPath(TCT_Lib.szMTS_Monitor_DiskPath_09);
                var xDiskPath10 = GetUsageDiskPath(TCT_Lib.szMTS_Monitor_DiskPath_10);

                if (xDiskPath01 != null)
                    xUsageDiskPaths.Add(xDiskPath01);
                if (xDiskPath02 != null)
                    xUsageDiskPaths.Add(xDiskPath02);
                if (xDiskPath03 != null)
                    xUsageDiskPaths.Add(xDiskPath03);
                if (xDiskPath04 != null)
                    xUsageDiskPaths.Add(xDiskPath04);
                if (xDiskPath05 != null)
                    xUsageDiskPaths.Add(xDiskPath05);
                if (xDiskPath06 != null)
                    xUsageDiskPaths.Add(xDiskPath06);
                if (xDiskPath07 != null)
                    xUsageDiskPaths.Add(xDiskPath07);
                if (xDiskPath08 != null)
                    xUsageDiskPaths.Add(xDiskPath08);
                if (xDiskPath09 != null)
                    xUsageDiskPaths.Add(xDiskPath09);
                if (xDiskPath10 != null)
                    xUsageDiskPaths.Add(xDiskPath10);
            }
            catch (Exception err)
            {
                LogInfo.WriteLogMTS("- Fail GetUsageDiskPaths =" + err.Message);
                LogInfo.WriteLogMTS(LogLevel.DEBUG, $"- GetUsageDiskPaths : stack={err.StackTrace}");
            }

            LogInfo.WriteLogMTS(LogLevel.DEBUG, $"+ GetUsageDiskPaths : paths={xUsageDiskPaths.Count}");
            return xUsageDiskPaths;
        }

        private static UsageDiskPath GetUsageDiskPath(string szPathName)
        {
            if (szPathName.Trim() == "")
                return null;

            try
            {
                string szRefPath = "";
                long fTotalDisk = 0, fTotalUsed = 0, fTotalFree = 0, fPercentUsed = 0;
                if (GetDiskSpace(szPathName, ref szRefPath, ref fTotalDisk, ref fTotalUsed, ref fTotalFree, ref fPercentUsed))
                {
                    var xDiskPath           = new UsageDiskPath();
                    xDiskPath.pathName      = szPathName;
                    xDiskPath.spaceKb       = fTotalDisk;
                    xDiskPath.usageKb       = fTotalUsed;
                    xDiskPath.freeKb        = fTotalFree;
                    xDiskPath.percentUsed   = float.Parse(fPercentUsed.ToString("0.00"));

                    return xDiskPath;
                }
                else
                {
                    LogInfo.WriteLogMTS(LogLevel.DEBUG, $"+ GetUsageDiskPath : GetDiskSpace failed, Path={szPathName} -> skip");
                    return null;
                }
            }
            catch (Exception err)
            {
                LogInfo.WriteLogMTS($"-Fail GetUsageDiskPath : Path={szPathName}, Error={err.Message}");
                LogInfo.WriteLogMTS(LogLevel.DEBUG, $"- GetUsageDiskPath : stack={err.StackTrace}");
                return null;
            }
        }
/*
        private static UsageDiskPath_V0 GetUsageDiskPath(string szPath)
        {
            if (szPath.Trim() == "")
                return null;

            var xDiskPath           = new UsageDiskPath();
            xDiskPath.pathName      = szPath;
            long fPathSize = 0;
            try
            {
                fPathSize = GetDirectorySize(szPath);
            }
            catch (Exception err)
            {
                LogInfo.WriteLogMTS("- Fail GetDirectorySize of Path=" + szPath + ", Error=" + err.Message);
            }
            xDiskPath.usageDiskMb   = float.Parse((fPathSize / (1024.0 * 1024.0)).ToString("0.00"));

            return xDiskPath;
        }
*/        
        private static List<FileVersion> GetListFileVersions(int? iStatusListVersion, int? iBlackListVersion)
        {
            var xFileVersions = new List<FileVersion>();
            xFileVersions.Add(GetFileVersion("TCT"));
            xFileVersions.Add(GetFileVersion("Tariff"));
            xFileVersions.Add(GetFileVersion("Staff"));
            xFileVersions.Add(GetFileVersion("StatusList", iStatusListVersion));
            xFileVersions.Add(GetFileVersion("BlackList", null, iBlackListVersion));
            xFileVersions.Add(GetFileVersion("DmtCard"));
            xFileVersions.Add(GetFileVersion("Promotion"));
            if (LogInfo.IsEnabled(LogLevel.DEBUG))
                LogInfo.WriteLogMTS(LogLevel.DEBUG, "+ GetListFileVersions : " + string.Join(", ", xFileVersions.Select(f => $"{f?.fileName}={f?.curVersion ?? "null"}")));

            return xFileVersions;
        }

        private static FileVersion GetFileVersion(string szFileName, int? iStatusListVersion=null, int? iBlackListVersion=null)
        {
            var fileVersion         = new FileVersion();

            fileVersion.fileName    = szFileName;
            fileVersion.nextVersion = null;

            if (szFileName.ToUpper() == "TCT")
                fileVersion.curVersion = TCT_Lib.szAppVersion;
            else if (szFileName.ToUpper() == "TARIFF")
            {
                fileVersion.curVersion  = (new DateTime(2021, 07, 01, 14, 30, 0)).ToString("yyyy-MM-dd HH:mm:ss");
                fileVersion.nextVersion = (new DateTime(2021, 09, 15, 12, 00, 0)).ToString("yyyy-MM-dd HH:mm:ss");
            }
            else if (szFileName.ToUpper() == "STAFF")
            {
                //fileVersion.curVersion  = (new DateTime(2021, 06, 11, 10, 45, 0)).ToString("yyyy-MM-dd HH:mm:ss");
                fileVersion.curVersion = TCT_Lib.LaneDB.GetStaffDbTimeStamp();
            }
            else if (szFileName.ToUpper() == "STATUSLIST")
            {
                fileVersion.curVersion = iStatusListVersion == null ? null : iStatusListVersion.ToString();
            }
            else if (szFileName.ToUpper() == "BLACKLIST")
            {
                fileVersion.curVersion = iBlackListVersion == null ? null : iBlackListVersion.ToString();
            }
            else if (szFileName.ToUpper() == "DMTCARD")
            {
                fileVersion.curVersion = TCT_Lib.LaneDB.GetDmtCardDbTimeStamp();
            }
            else if (szFileName.ToUpper() == "PROMOTION")
            {
                fileVersion.curVersion = TCT_Lib.LaneDB.GetPromotionDbTimeStamp();
            }
            return fileVersion;
        }


        async private static Task<AppStatus> GetAppStatus()
        {
            //bool bRtoStatus         = TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szRTO_IPAddress, TCT_Lib.iRTO_SocketPort - 1);
            //bool bSupAdjustStatus   = TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szSupAdjust_IPAddress, TCT_Lib.iSupAdjust_SocketPort);
            bool bRtoStatus         = await TCT_Lib.laneController.IsPortReachable(TCT_Lib.szRTO_IPAddress, TCT_Lib.iRTO_SocketPort - 1);
            bool bSupAdjustStatus   = await TCT_Lib.laneController.IsPortReachable(TCT_Lib.szSupAdjust_IPAddress, TCT_Lib.iSupAdjust_SocketPort);

            var xAppStatus          = new AppStatus();
            xAppStatus.tct          = true;
            xAppStatus.rto          = bRtoStatus;
            xAppStatus.supAdjust    = bSupAdjustStatus;
            LogInfo.WriteLogMTS(LogLevel.DEBUG, $"+ GetAppStatus : RTO {TCT_Lib.szRTO_IPAddress}:{TCT_Lib.iRTO_SocketPort - 1}={bRtoStatus}, SupAdjust {TCT_Lib.szSupAdjust_IPAddress}:{TCT_Lib.iSupAdjust_SocketPort}={bSupAdjustStatus}");

            return xAppStatus;
        }

        async private static Task<RabbitMQ.DeviceStatus> GetDeviceStatus()
        {
            var xDeviceStatus = new RabbitMQ.DeviceStatus();
            try
            {
                //bool bTfiStatus     = TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szMTS_Monitor_TFI_IPAddress, TCT_Lib.iMTS_Monitor_TFI_Port);
                //bool bCrsStatus     = TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szMTS_Monitor_CRS_IPAddress, TCT_Lib.iMTS_Monitor_CRS_Port);
                //bool bEdcStatus     = TCT_Lib.bEnableEdc ? TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szEdcIPAddress, TCT_Lib.iEdcSendPort) : false;
                //bool bLsdStatus     = TCT_Lib.bEnableLSD ? TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szLSD_IPAddress, TCT_Lib.iLSD_SocketPort) : false;
                //bool bLcsdStatus    = TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szLCSD_SocketIPAddress, TCT_Lib.iLCSD_SocketPort);
                //bool bLcioStatus    = TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szLCIO_SocketIPAddress, TCT_Lib.iLCIO_SocketPort);

                bool bTfiStatus     = await TCT_Lib.laneController.IsPortReachable(TCT_Lib.szMTS_Monitor_TFI_IPAddress, TCT_Lib.iMTS_Monitor_TFI_Port);
                bool bCrsStatus     = await TCT_Lib.laneController.IsPortReachable(TCT_Lib.szMTS_Monitor_CRS_IPAddress, TCT_Lib.iMTS_Monitor_CRS_Port);
                bool bEdcStatus     = TCT_Lib.bEnableEdc ? await TCT_Lib.laneController.IsPortReachable(TCT_Lib.szEdcIPAddress, TCT_Lib.iEdcSendPort) : false;
                bool bLsdStatus     = TCT_Lib.bEnableLSD ? await TCT_Lib.laneController.IsPortReachable(TCT_Lib.szLSD_IPAddress, TCT_Lib.iLSD_SocketPort) : false;
                bool bLcsdStatus    = await TCT_Lib.laneController.IsPortReachable(TCT_Lib.szLCSD_SocketIPAddress, TCT_Lib.iLCSD_SocketPort);
                bool bLcioStatus    = await TCT_Lib.laneController.IsPortReachable(TCT_Lib.szLCIO_SocketIPAddress, TCT_Lib.iLCIO_SocketPort);

                //---------------------------------------------------------------------
                LogInfo.WriteLogMTS(LogLevel.DEBUG, $"+ GetDeviceStatus : port check TFI={bTfiStatus}, CRS={bCrsStatus}, EDC={bEdcStatus}(enable={TCT_Lib.bEnableEdc}), LSD={bLsdStatus}(enable={TCT_Lib.bEnableLSD}), LCSD={bLcsdStatus}, LCIO={bLcioStatus}");
                if (TCT_Lib.bEnableLSD && TCT_Lib.iLSD_KeepAliveSec == 0)
                {
                    if (bLsdStatus)
                        LogInfo.WriteLogMTS(LogLevel.DEBUG, $"+ GetDeviceStatus : LSD KeepAliveSec=0 -> use bCurLsdStatus={TCT_Lib.bCurLsdStatus}");
                    if (bLsdStatus)
                        bLsdStatus = TCT_Lib.bCurLsdStatus;
                }

                //---------------------------------------------------------------------

                xDeviceStatus           = new RabbitMQ.DeviceStatus();
                xDeviceStatus.tfi       = bTfiStatus;
                xDeviceStatus.crs       = bCrsStatus;
                //xDeviceStatus.edc       = bEdcStatus;
                //xDeviceStatus.lsd       = bLsdStatus;
                xDeviceStatus.lcsd      = bLcsdStatus;
                xDeviceStatus.lcio      = bLcioStatus;
                if (TCT_Lib.bEnableEdc)
                    xDeviceStatus.edc = bEdcStatus;
                else
                    xDeviceStatus.edc = null;

                if (TCT_Lib.bEnableLSD)
                    xDeviceStatus.lsd = bLsdStatus;
                else
                    xDeviceStatus.lsd = null;

                bool bScrStatus = false, bPrinterStatus = false, bKbStatus = false;
                GetUsbDeviceStatus(ref bPrinterStatus, ref bScrStatus, ref bKbStatus);
                xDeviceStatus.printer   = bPrinterStatus;
                xDeviceStatus.scr       = bScrStatus;
                xDeviceStatus.keyboard  = bKbStatus;

                //--------------------------------
                if (bScrStatus)
                {
                    xDeviceStatus.scr = TCT_Lib.bMTS_SCR;
                }
                //--------------------------------
                LogInfo.WriteLogMTS(LogLevel.DEBUG, $"+ GetDeviceStatus : result printer={xDeviceStatus.printer}, scr={xDeviceStatus.scr} (usb={bScrStatus}, bMTS_SCR={TCT_Lib.bMTS_SCR}), keyboard={xDeviceStatus.keyboard}, edc={xDeviceStatus.edc?.ToString() ?? "null"}, lsd={xDeviceStatus.lsd?.ToString() ?? "null"}");

                return xDeviceStatus;
            }
            catch (Exception err)
            {
                LogInfo.WriteLogMTS("- Error GetDeviceStatus : " + err.Message);
                LogInfo.WriteLogMTS(LogLevel.DEBUG, $"- GetDeviceStatus : stack={err.StackTrace}");

                return xDeviceStatus;
            }
        }

        private static CameraStatus GetCameraStatus()
        {
            var xCameraStatus       = new CameraStatus();
            xCameraStatus.camera1   = TCT_Lib.bStatusCamera1;
            xCameraStatus.camera2   = TCT_Lib.bStatusCamera2;
            xCameraStatus.camera3   = TCT_Lib.bStatusCamera3;
            LogInfo.WriteLogMTS(LogLevel.DEBUG, $"+ GetCameraStatus : camera1={TCT_Lib.bStatusCamera1}, camera2={TCT_Lib.bStatusCamera2}, camera3={TCT_Lib.bStatusCamera3}");

/*
            if (TCT_Lib.bEnableVideoNVR)
            {
                xCameraStatus.camera1 = TCT_Lib.bStatusCamera1;
                xCameraStatus.camera2 = TCT_Lib.bStatusCamera2;
                xCameraStatus.camera3 = TCT_Lib.bStatusCamera3;
            }
            else
            {
                xCameraStatus.camera1 = null;
                xCameraStatus.camera2 = null;
                xCameraStatus.camera3 = null;
            }
*/            

            return xCameraStatus;
        }

        //--------------------------------------------------------------------------
        public static void CreateGetEdcStatusThread()
        {
            threadGetEtcStatus              = new Thread(new ThreadStart(ThreadGetEtcStatus));
            threadGetEtcStatus.Priority     = ThreadPriority.Normal;
            threadGetEtcStatus.IsBackground = true;
            threadGetEtcStatus.Start();
            LogInfo.WriteLogMTS(LogLevel.DEBUG, $"+ CreateGetEdcStatusThread : ThreadGetEtcStatus started, interval={TCT_Lib.iMTS_Monitor_IntervalSecGetEtcStatus} sec");
        }

        async static private void ThreadGetEtcStatus()
        {
            LogInfo.WriteLogMTS(LogLevel.DEBUG, $"+ ThreadGetEtcStatus : start, Url={TCT_Lib.szMTS_EtcStatusDbUrl}, Url2={TCT_Lib.szMTS_EtcStatusDbUrl2}, Url3={TCT_Lib.szMTS_EtcStatusDbUrl3}, Url4={TCT_Lib.szMTS_EtcStatusDbUrl4}");
            while (true)
            {
                LogInfo.WriteLogMTS("+++ Run Thread GET ETC Status");
                try
                {
                    string _szObuValidFrom = "", _szObuListCount = "", _szObuVersion = "";
                    string _szBlackListValidFrom = "", _szBlackListListCount = "", _szBlackListVersion = "";
                    string _szTpsState = "";

                    string _szXmr1StatusListRecord = "", _szXmr1ObuValidFrom = "";
                    string _szXmr1BlackListRecord = "", _szXmr1BlackListValidFrom = "";

                    string _szXmr2StatusListRecord = "", _szXmr2ObuValidFrom = "";
                    string _szXmr2BlackListRecord = "", _szXmr2BlackListValidFrom = "";

                    string _szLaneMode = "", _szXmr1State = "", _szXmr2State = "";
                    string _szPresenceDetState = "", _szPassageDetState = "";
                    string _szBangkokDisplayState = "", _szGrabberState = "", _szBangkokLcState = "";

                    //-----------------------------------------------------------------------
                    if (GetETC_StatusDB(ref _szObuListCount, ref _szObuValidFrom, ref _szObuVersion, ref _szBlackListListCount, ref _szBlackListValidFrom, ref _szBlackListVersion, ref _szTpsState))
                    {
                        szObuListCount          = _szObuListCount;
                        szObuValidFrom          = _szObuValidFrom;
                        szObuVersion            = _szObuVersion;

                        szBlackListListCount    = _szBlackListListCount;
                        szBlackListValidFrom    = _szBlackListValidFrom;
                        szBlackListVersion      = _szBlackListVersion;

                        szTpsState              = _szTpsState;
                    }
                    else
                    {
                        //--- Set value to null
                        szObuListCount          = "";
                        szObuValidFrom          = "";
                        szObuVersion            = "";

                        szBlackListListCount    = "";
                        szBlackListValidFrom    = "";
                        szBlackListVersion      = "";

                        szTpsState              = "";
                    }

                    // XMR1 Status
                    if (GetETC_StatusXmr(TCT_Lib.szMTS_EtcStatusDbUrl2, ref _szXmr1StatusListRecord, ref _szXmr1ObuValidFrom, ref _szXmr1BlackListRecord, ref _szXmr1BlackListValidFrom))
                    {
                        szXmr1StatusListRecord      = _szXmr1StatusListRecord;
                        szXmr1ObuValidFrom          = _szXmr1ObuValidFrom;

                        szXmr1BlackListRecord       = _szXmr1BlackListRecord;
                        szXmr1BlackListValidFrom    = _szXmr1BlackListValidFrom;
                    }
                    else
                    {
                        //--- Set value to null
                        szXmr1StatusListRecord      = "";
                        szXmr1ObuValidFrom          = "";

                        szXmr1BlackListRecord       = "";
                        szXmr1BlackListValidFrom    = "";
                    }

                    // XMR2 Status
                    if (GetETC_StatusXmr(TCT_Lib.szMTS_EtcStatusDbUrl3, ref _szXmr2StatusListRecord, ref _szXmr2ObuValidFrom, ref _szXmr2BlackListRecord, ref _szXmr2BlackListValidFrom))
                    {
                        szXmr2StatusListRecord      = _szXmr2StatusListRecord;
                        szXmr2ObuValidFrom          = _szXmr2ObuValidFrom;

                        szXmr2BlackListRecord       = _szXmr2BlackListRecord;
                        szXmr2BlackListValidFrom    = _szXmr2BlackListValidFrom;
                    }
                    else
                    {
                        //--- Set value to null
                        szXmr2StatusListRecord      = "";
                        szXmr2ObuValidFrom          = "";

                        szXmr2BlackListRecord       = "";
                        szXmr2BlackListValidFrom    = "";
                    }

                    // ETC Lane Mode and Lane State
                    if (GetETC_StatusDB_Url4(ref _szLaneMode, ref _szXmr1State, ref _szXmr2State, ref _szPresenceDetState, ref _szPassageDetState, 
                                                    ref _szBangkokDisplayState, ref _szGrabberState, ref _szBangkokLcState))
                    {
                        szLaneMode              = _szLaneMode;
                        szXmr1State             = _szXmr1State;
                        szXmr2State             = _szXmr2State;
                        szPresenceDetState      = _szPresenceDetState;

                        szPassageDetState       = _szPassageDetState;
                        szBangkokDisplayState   = _szBangkokDisplayState;
                        szGrabberState          = _szGrabberState;
                        szBangkokLcState        = _szBangkokLcState;
                    }
                    else
                    {
                        //--- Set value to null
                        szLaneMode              = "";
                        szXmr1State             = "";
                        szXmr2State             = "";
                        szPresenceDetState      = "";

                        szPassageDetState       = "";
                        szBangkokDisplayState   = "";
                        szGrabberState          = "";
                        szBangkokLcState        = "";
                    }

                    LogInfo.WriteLogMTS(LogLevel.DEBUG, $"+ ThreadGetEtcStatus : TPS={szTpsState}, OBU={szObuListCount}/{szObuVersion}, BL={szBlackListListCount}/{szBlackListVersion}, XMR1 OBU={szXmr1StatusListRecord} BL={szXmr1BlackListRecord}, XMR2 OBU={szXmr2StatusListRecord} BL={szXmr2BlackListRecord}, LaneMode={szLaneMode}, errUrl={bFoudErrUrl}");
                    LogInfo.WriteLogMTS("+++ Finish Thread GET ETC Status");
                    await Task.Delay(TCT_Lib.iMTS_Monitor_IntervalSecGetEtcStatus * 1000);
                }
                catch (Exception err)
                {
                    LogInfo.WriteLogMTS("--- Error ThreadGetEtcStatus : " + err.Message);
                    LogInfo.WriteLogMTS(LogLevel.DEBUG, $"- ThreadGetEtcStatus : stack={err.StackTrace}");
                    await Task.Delay(TCT_Lib.iMTS_Monitor_IntervalSecGetEtcStatus * 1000);
                }
            }

            threadGetEtcStatus.Abort();
        }

        private static string GetValueObu(string szTagName, string szHtmlContent)
        {
            string szValue  = "";
            string szSearch = $"<td>{szTagName}</td><td>";
            int idx         = szHtmlContent.IndexOf(szSearch, StringComparison.Ordinal);
            if (idx >= 0)
            {
                string szData2  = szHtmlContent.Remove(0, idx + szSearch.Length);
                int idxEnd      = szData2.IndexOf("</td>", StringComparison.Ordinal);
                szValue         = szData2.Substring(0, idxEnd);
                if (szValue.Trim() == "&nbsp;")
                    szValue = "";
            }

            return szValue;
        }

        private static string GetLaneMode(string szData)
        {
            string szSearch = "{LaneMode";
            try
            {
                int idx = szData.IndexOf(szSearch, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    string szTemp   = szData.Remove(0, idx);
                    int idx2        = szTemp.IndexOf("&quot;", StringComparison.Ordinal);
                    szTemp          = szTemp.Remove(0, idx2 + 6);
                    int idx3        = szTemp.IndexOf("&quot;", StringComparison.Ordinal);
                    string _szLaneMode = szTemp.Substring(0, idx3);
                    return _szLaneMode;
                }
                return "";
            }
            catch (Exception err)
            {
                LogInfo.WriteLogMTS("- Error GetLaneMode : " + err.Message);
                LogInfo.WriteLogMTS(LogLevel.DEBUG, $"- GetLaneMode : len={szData?.Length ?? 0}, stack={err.StackTrace}");
                return "";
            }
        }

        private static bool GetETC_StatusDB(ref string _szObuListCount, ref string _szObuValidFrom, ref string _szObuVersion, 
                                            ref string _szBlackListListCount, ref string _szBlackListValidFrom, ref string _szBlackListVersion,
                                            ref string _szTpsState)
        {
            if (TCT_Lib.szMTS_EtcStatusDbUrl.Trim() == "")
            {
                //LogInfo.WriteLogMTS("- Not found config MTS_EtcStatusDbUrl");
                LogInfo.WriteLogMTS(LogLevel.DEBUG, "+ GetETC_StatusDB : MTS_EtcStatusDbUrl not configured -> skip");
                return false;
            }

            if (bFoudErrUrl)
            {
                iCountingUrl++;
                if (iCountingUrl <= iMaxSkipUrl)
                {
                    LogInfo.WriteLogMTS($"- Found error get status : URL={TCT_Lib.szMTS_EtcStatusDbUrl.Trim()}. Time = {iCountingUrl}");
                    return false;
                }
                else
                {
                    LogInfo.WriteLogMTS(LogLevel.DEBUG, $"+ GetETC_StatusDB : skip count {iCountingUrl} > {iMaxSkipUrl} -> retry URL, bFoudErrUrl true -> false");
                    iCountingUrl    = 0;
                    bFoudErrUrl     = false;
                }
            }

            LogInfo.WriteLogMTS("+ Start Get Status MTS_EtcStatusDbUrl : URL = " + TCT_Lib.szMTS_EtcStatusDbUrl.Trim());

            // Set a default policy level for the "http:" and "https" schemes.
            HttpRequestCachePolicy policy       = new HttpRequestCachePolicy(HttpRequestCacheLevel.Default);
            HttpWebRequest.DefaultCachePolicy   = policy;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            var request = (HttpWebRequest)WebRequest.Create(TCT_Lib.szMTS_EtcStatusDbUrl);
            HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.Default);
            request.CachePolicy = noCachePolicy;
            try
            {
                //CookieContainer cookies = new CookieContainer();
                //request.CookieContainer = cookies;
                request.Method      = "GET";
                request.KeepAlive   = false;
                request.Timeout     = 3 * 1000;

                var swHttp          = Stopwatch.StartNew();
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    StreamReader sr         = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                    string szHtmlContent    = sr.ReadToEnd();
                    sr.Close();
                    LogInfo.WriteLogMTS(LogLevel.DEBUG, $"+ GetETC_StatusDB : HTTP {(int)response.StatusCode}, len={szHtmlContent?.Length ?? 0} ({swHttp.ElapsedMilliseconds} ms)");
                    LogInfo.WriteLogMTS(LogLevel.TRACE, $"+ GetETC_StatusDB : response=\n{szHtmlContent}");

                    //LogInfo.WriteLogMTS("+ Response : " + szHtmlContent);
                    //-----------------------------
                    _szObuListCount         = GetValueObu("OBUFast.ListCount", szHtmlContent);
                    _szObuValidFrom         = GetValueObu("OBUFast.LastUpdateINT64", szHtmlContent);
                    _szObuVersion           = GetValueObu("OBUFast.Version", szHtmlContent);

                    _szBlackListListCount   = GetValueObu("BlacklistDB.ListCount", szHtmlContent);
                    _szBlackListValidFrom   = GetValueObu("BlacklistDB.LastUpdateINT64", szHtmlContent);
                    _szBlackListVersion     = GetValueObu("BlacklistDB.Version", szHtmlContent);
                    _szTpsState             = GetValueObu("StatusCollector.State", szHtmlContent);
                    LogInfo.WriteLogMTS($"+ Success GetETC_StatusDB : OBUFast.ListCount={_szObuListCount}, OBUFast.Version={_szObuVersion}, OBUFast.ValidFrom={_szObuValidFrom}");
                    LogInfo.WriteLogMTS($"+ Success GetETC_StatusDB : BlacklistDB.ListCount={_szBlackListListCount}, BlacklistDB.Version={_szBlackListVersion}, BlacklistDB.ValidFrom={_szBlackListValidFrom}");
                    LogInfo.WriteLogMTS($"+ Success GetETC_StatusDB : StatusCollector.State={_szTpsState}");

                    response.Close();
                    response.Dispose();
                }

                request.Abort();
                request = null;

                LogInfo.WriteLogMTS("+ Finish Get Status MTS_EtcStatusDbUrl");
                return true;
            }
            catch (Exception err)
            {
                LogInfo.WriteLogMTS($"- Error GetETC_StatusDB : URL={TCT_Lib.szMTS_EtcStatusDbUrl.Trim()}, Error={err.Message}");
                LogInfo.WriteLogMTS(LogLevel.DEBUG, $"- GetETC_StatusDB : bFoudErrUrl {bFoudErrUrl} -> true, skip next {iMaxSkipUrl} cycles, stack={err.StackTrace}");
                request.Abort();
                request = null;

                bFoudErrUrl     = true;
                iCountingUrl    = 0;

                return false;
            }
        }

        private static bool GetETC_StatusXmr(string szEtcStatusUrl, ref string _szObuListCount, ref string _szObuValidFrom, 
                                                                    ref string _szBlackListListCount, ref string _szBlackListValidFrom)
        {
            if (szEtcStatusUrl.Trim() == "")
            {
                //LogInfo.WriteLogMTS("- Not found config URL GetETC_StatusXmr");
                LogInfo.WriteLogMTS(LogLevel.DEBUG, "+ GetETC_StatusXmr : URL not configured -> skip");
                return false;
            }

            if (bFoudErrUrl)
            {
                LogInfo.WriteLogMTS("- Skip GetETC_StatusXmr : Found error get url");
                return false;
            }

            LogInfo.WriteLogMTS("+ Start Get Status MTS_EtcStatusDbUrl : URL = " + szEtcStatusUrl.Trim());

            // Set a default policy level for the "http:" and "https" schemes.
            HttpRequestCachePolicy policy       = new HttpRequestCachePolicy(HttpRequestCacheLevel.Default);
            HttpWebRequest.DefaultCachePolicy   = policy;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            var request = (HttpWebRequest)WebRequest.Create(szEtcStatusUrl);
            HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.Default);
            request.CachePolicy = noCachePolicy;
            try
            {
                //CookieContainer cookies = new CookieContainer();
                //request.CookieContainer = cookies;
                request.Method      = "GET";
                request.KeepAlive   = false;
                request.Timeout     = 3 * 1000;

                var swHttp          = Stopwatch.StartNew();
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    StreamReader sr         = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                    string szHtmlContent    = sr.ReadToEnd();
                    sr.Close();
                    LogInfo.WriteLogMTS(LogLevel.DEBUG, $"+ GetETC_StatusXmr : HTTP {(int)response.StatusCode}, len={szHtmlContent?.Length ?? 0} ({swHttp.ElapsedMilliseconds} ms)");
                    LogInfo.WriteLogMTS(LogLevel.TRACE, $"+ GetETC_StatusXmr : response=\n{szHtmlContent}");

                    //LogInfo.WriteLogMTS("+ Response : " + szHtmlContent);
                    //-----------------------------
                    _szObuListCount         = GetValueObu("OBU.ListCount", szHtmlContent);
                    _szObuValidFrom         = GetValueObu("OBU.LastUpdateINT64", szHtmlContent);

                    _szBlackListListCount   = GetValueObu("BlacklistDB.ListCount", szHtmlContent);
                    _szBlackListValidFrom   = GetValueObu("BlacklistDB.LastUpdateINT64", szHtmlContent);

                    LogInfo.WriteLogMTS($"+ Success GetETC_StatusXmr : OBU.ListCount={_szObuListCount}, OBU.ValidFrom={_szObuValidFrom}");
                    LogInfo.WriteLogMTS($"+ Success GetETC_StatusXmr : BlacklistDB.ListCount={_szBlackListListCount}, BlacklistDB.ValidFrom={_szBlackListValidFrom}");

                    response.Close();
                    response.Dispose();
                }

                request.Abort();
                request = null;

                LogInfo.WriteLogMTS("+ Finish Get Status GetETC_StatusXmr");
                return true;
            }
            catch (Exception err)
            {
                LogInfo.WriteLogMTS("- Error GetETC_StatusXmr : " + err.Message);
                LogInfo.WriteLogMTS(LogLevel.DEBUG, $"- GetETC_StatusXmr : URL={szEtcStatusUrl}, bFoudErrUrl {bFoudErrUrl} -> true, stack={err.StackTrace}");
                request.Abort();
                request = null;

                bFoudErrUrl     = true;
                iCountingUrl    = 1;

                return false;
            }
        }

        private static bool GetETC_StatusDB_Url4(ref string _szLaneMode, ref string _szXmr1State, ref string _szXmr2State, 
                                            ref string _szPresenceDetState, ref string _szPassageDetState, 
                                            ref string _szBangkokDisplayState, ref string _szGrabberState, ref string _szBangkokLcState)
        {
            if (TCT_Lib.szMTS_EtcStatusDbUrl4.Trim() == "")
            {
                //LogInfo.WriteLogMTS("- Not found config MTS_EtcStatusDbUrl4");
                LogInfo.WriteLogMTS(LogLevel.DEBUG, "+ GetETC_StatusDB_Url4 : MTS_EtcStatusDbUrl4 not configured -> skip");
                return false;
            }

            if (bFoudErrUrl)
            {
                LogInfo.WriteLogMTS("- Skip GetETC_StatusDB_Url4 : Found error get url");
                return false;
            }

            LogInfo.WriteLogMTS("+ Start Get Status GetETC_StatusDB_Url4 : URL = " + TCT_Lib.szMTS_EtcStatusDbUrl4.Trim());

            // Set a default policy level for the "http:" and "https" schemes.
            HttpRequestCachePolicy policy       = new HttpRequestCachePolicy(HttpRequestCacheLevel.Default);
            HttpWebRequest.DefaultCachePolicy   = policy;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            var request = (HttpWebRequest)WebRequest.Create(TCT_Lib.szMTS_EtcStatusDbUrl4);
            HttpRequestCachePolicy noCachePolicy    = new HttpRequestCachePolicy(HttpRequestCacheLevel.Default);
            request.CachePolicy                     = noCachePolicy;

            try
            {
                //CookieContainer cookies = new CookieContainer();
                //request.CookieContainer = cookies;
                request.Method      = "GET";
                request.KeepAlive   = false;
                request.Timeout     = 3 * 1000;

                var swHttp          = Stopwatch.StartNew();
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    StreamReader sr         = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                    string szHtmlContent    = sr.ReadToEnd();
                    sr.Close();
                    LogInfo.WriteLogMTS(LogLevel.DEBUG, $"+ GetETC_StatusDB_Url4 : HTTP {(int)response.StatusCode}, len={szHtmlContent?.Length ?? 0} ({swHttp.ElapsedMilliseconds} ms)");
                    LogInfo.WriteLogMTS(LogLevel.TRACE, $"+ GetETC_StatusDB_Url4 : response=\n{szHtmlContent}");

                    //LogInfo.WriteLogMTS("+ Response : " + szHtmlContent);
                    //-----------------------------
                    _szLaneMode             = GetLaneMode(GetValueObu("StatusCollector.AggregatedStatus", szHtmlContent));
                    _szXmr1State            = GetValueObu("host_StatusCollector[0].Object.State", szHtmlContent);
                    _szXmr2State            = GetValueObu("host_StatusCollector[8].Object.State", szHtmlContent);
                    _szPresenceDetState     = GetValueObu("host_StatusCollector[1].Object.State", szHtmlContent);
                    _szPassageDetState      = GetValueObu("host_StatusCollector[2].Object.State", szHtmlContent);
                    _szBangkokDisplayState  = GetValueObu("host_StatusCollector[3].Object.State", szHtmlContent);
                    _szGrabberState         = GetValueObu("host_StatusCollector[4].Object.State", szHtmlContent);
                    _szBangkokLcState       = GetValueObu("host_StatusCollector[7].Object.State", szHtmlContent);

                    LogInfo.WriteLogMTS($"+ Success GetETC_StatusDB : LaneMode={_szLaneMode}, Xmr1State={_szXmr1State}, Xmr2State={_szXmr2State}");
                    LogInfo.WriteLogMTS($"+ Success GetETC_StatusDB : PresenceDetState={_szPresenceDetState}, PassageDetState={_szPassageDetState}");
                    LogInfo.WriteLogMTS($"+ Success GetETC_StatusDB : BangkokDisplayState={_szBangkokDisplayState}, GrabberState={_szGrabberState}, BangkokLcState={_szBangkokLcState}");

                    response.Close();
                    response.Dispose();
                }

                request.Abort();
                request = null;
                LogInfo.WriteLogMTS("+ Finish Get Status GetETC_StatusDB_Url4");

                return true;
            }
            catch (Exception err)
            {
                LogInfo.WriteLogMTS("- Error GetETC_StatusDB_Url4 : " + err.Message);
                LogInfo.WriteLogMTS(LogLevel.DEBUG, $"- GetETC_StatusDB_Url4 : URL={TCT_Lib.szMTS_EtcStatusDbUrl4}, bFoudErrUrl {bFoudErrUrl} -> true, stack={err.StackTrace}");
                request.Abort();
                request = null;

                bFoudErrUrl     = true;
                iCountingUrl    = 1;

                return false;
            }
        }
    }
}
