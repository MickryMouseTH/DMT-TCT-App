#define __FULL_SCREEN__

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Gtk;

namespace TCT_App
{
    class MainClass
    {
        public static void Main(string[] args)
        {
#if __FULL_SCREEN__
            //---------------------------------------------------------
            //Get current process
            Process current = Process.GetCurrentProcess();

            //Get array or processes with same name
            //Process[] procs = Process.GetProcessesByName(current.ProcessName);
         
            //---------------------------------------------------------------
            int iTotal      = process_count("TCT_App");
            Console.WriteLine("Total " + current.ProcessName + " : " + iTotal);
            if (iTotal > 0)
                kill_process(current.Id, "TCT_App");
#endif
            //--------------------------------------------------------------
            var appVersion  = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
            Assembly asm    = Assembly.GetExecutingAssembly();

            TCT_Lib.LoadTCT_Configuration();
            TCT_Lib.LoadKeyMapping();
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ Main : config + key mapping loaded, LogLevel={LogInfo.eLogLevel}, FileVersion={appVersion?.FileVersion ?? "null"}, LogPath={TCT_Lib.szLogPath}");
#if __FULL_SCREEN__
            LogInfo.WriteLog(iTotal > 0 ? LogLevel.WARN : LogLevel.DEBUG, $"{(iTotal > 0 ? "- Warning" : "+")} Main : pid={current?.Id}, other TCT_App process found={iTotal} (killed before start)");
#endif

            TCT_Lib.dtTctStartDateTime = DateTime.Now;

/*
            LogInfo.WriteLog("");
            LogInfo.WriteLog("");
            LogInfo.WriteLog("====================================================");
            LogInfo.WriteLog("======= TCT APP : Version = " + TCT_Lib.szAppVersion + " ===========");
            LogInfo.WriteLog("====================================================");
*/
            Application.Init();
            UiTheme.ApplyGlobalStyle();     // v2.0.2: GUI restyle
            Gtk.Settings.Default.SetLongProperty("gtk-button-images", 1, "");
            //MainWindow win = new MainWindow();
            LoginWnd win        = new LoginWnd();
            //MaintenanceWnd.MainMaintenance win = new MaintenanceWnd.MainMaintenance();
            win.SetPosition(WindowPosition.Center);

            Gdk.Color colBk     = new Gdk.Color(0x2E, 0x2E, 0x2E);
            win.ModifyBg(StateType.Normal, colBk);
           
            var screen          = win.GdkWindow.Screen;
            win.GdkWindow.GetSize(out var winWidth, out var winHeight);
            win.Move((screen.Width / 2) - (winWidth / 2), (screen.Height / 2) - (winHeight / 2));

            //win.SetPosition(WindowPosition.Center);


            //------------------------------------------------------------------
#if __FULL_SCREEN__
            string szLogPath = TCT_Lib.szLogPath;
#else
            //string szLogPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string szLogPath = TCT_Lib.szLogPath.Trim();
#endif
            //--- Check and Create Log Folder, LaneTransaction Folder, LaneEvent Folder
            var laneLogPath     = Path.Combine(szLogPath, "LaneLog");
            if (!Directory.Exists(laneLogPath))
                Directory.CreateDirectory(laneLogPath);
            //var logPath         = Path.Combine(laneLogPath, "Log_" + monthYear);
            //if (!Directory.Exists(logPath))
            //   Directory.CreateDirectory(logPath);

            var monthYear = DateTime.Today.Month.ToString("00") + "-" + DateTime.Today.Year;
            var laneEventPath   = Path.Combine(szLogPath, "LaneEvent");
            if (!Directory.Exists(laneEventPath))
                Directory.CreateDirectory(laneEventPath);
            var eventPath       = Path.Combine(laneEventPath, "Event_" + monthYear);
            if (!Directory.Exists(eventPath))
                Directory.CreateDirectory(eventPath);

            var laneAlarmPath   = Path.Combine(szLogPath, "LaneAlarm");
            if (!Directory.Exists(laneAlarmPath))
                Directory.CreateDirectory(laneAlarmPath);
            var alarmtPath      = Path.Combine(laneAlarmPath, "Alarm_" + monthYear);
            if (!Directory.Exists(alarmtPath))
                Directory.CreateDirectory(alarmtPath);

            var laneTransPath   = Path.Combine(szLogPath, "LaneTrans");
            if (!Directory.Exists(laneTransPath))
                Directory.CreateDirectory(laneTransPath);
            var transMonthPath  = Path.Combine(laneTransPath, "Trans_" + monthYear);
            if (!Directory.Exists(transMonthPath))
                Directory.CreateDirectory(transMonthPath);
            var todayDate       = DateTime.Today.ToString("yyyy-MM-dd");
            var transTodayPath  = Path.Combine(transMonthPath, "Trans_" + todayDate);
            if (!Directory.Exists(transTodayPath))
                Directory.CreateDirectory(transTodayPath);

            var laneRabbitMQPath = Path.Combine(szLogPath, "RabbitMQ");
            if (!Directory.Exists(laneRabbitMQPath))
                Directory.CreateDirectory(laneRabbitMQPath);
            var rabbitMQPath    = Path.Combine(laneRabbitMQPath, "RabbitMQ_" + monthYear);
            if (!Directory.Exists(rabbitMQPath))
                Directory.CreateDirectory(rabbitMQPath);

            var videoCapturePath = Path.Combine(szLogPath, "VideoCapture");
            if (!Directory.Exists(videoCapturePath))
                Directory.CreateDirectory(videoCapturePath);

            var tfiImagePath = Path.Combine(szLogPath, "TfiImage");
            if (!Directory.Exists(tfiImagePath))
                Directory.CreateDirectory(tfiImagePath);
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ Main : log folders ready under {szLogPath}");

            /*
                        if (TCT_Lib.bEnableVideoNVR)
                        {
                            if (!Directory.Exists(TCT_Lib.szVideoLane_CaptureImagePath))
                                Directory.CreateDirectory(TCT_Lib.szVideoLane_CaptureImagePath);
                        }
            */



            var swStartup = Stopwatch.StartNew();
            TCT_Lib.UpdateDatabaseStructure();
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ Main : UpdateDatabaseStructure done ({swStartup.ElapsedMilliseconds} ms)");
            //------------------------------------------------------------------
            swStartup.Restart();
            TCT_Lib.BackupLogFile();
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ Main : BackupLogFile done ({swStartup.ElapsedMilliseconds} ms)");

            //------------------------------------------------------------------
            TCT_Lib.frmLoginWnd = win;
            win.Show();
            win.Activate();
#if __FULL_SCREEN__
            win.Fullscreen();
#endif
            LogInfo.WriteLog(LogLevel.DEBUG, "+ Main : GTK main loop start (Application.Run)");
            Application.Run();
            LogInfo.WriteLog(LogLevel.DEBUG, "+ Main : GTK main loop exit");
        }

        public static int process_count(string application_name)
        {
            int rc          = 0;
            string cmdline  = "";

            Process[] processlist = Process.GetProcesses();

            foreach (Process p in processlist.ToList())
            {
                cmdline = "";

                if (p.ProcessName.Contains("mono"))
                {
                    Console.WriteLine("PID : " + p.Id + " " + p.ProcessName + " " + p.MainModule.FileName);
                    cmdline = File.ReadAllText("/proc/" + p.Id.ToString() + "/cmdline");
                    Console.WriteLine("CMDLINE : "+cmdline);
                }

                if (p.ProcessName.Contains("cli"))
                {
                    Console.WriteLine("PID : " + p.Id + " " + p.ProcessName + " " + p.MainModule.FileName);
                    cmdline = File.ReadAllText("/proc/" + p.Id.ToString() + "/cmdline");
                    Console.WriteLine("CMDLINE : " + cmdline);
                }

                if (cmdline.Contains(application_name))
                {
                    Console.WriteLine("Found existing process: {0} ID: {1}", p.ProcessName, p.Id);
                    rc++;
                }
            }
            return (rc);
        }

        public static void kill_process(int curAppId, string application_name)
        {
            Process[] processlist = Process.GetProcesses();
            foreach (Process p in processlist.ToList())
            {
                string cmdline = "";

                if (p.ProcessName.Contains("mono"))
                {
                    Console.WriteLine("PID : " + p.Id + " " + p.ProcessName + " " + p.MainModule.FileName);
                    cmdline = File.ReadAllText("/proc/" + p.Id.ToString() + "/cmdline");
                    Console.WriteLine("CMDLINE : " + cmdline);

                    if (cmdline.Contains(application_name))
                    {
                        if (p.Id != curAppId)
                            p.Kill();
                    }
                }
            }
        }
    }
}
