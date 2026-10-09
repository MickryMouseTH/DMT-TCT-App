using Gtk;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace TCT_App.LSD
{
    public class LaneSocketControl_LSD
    {
        TcpClient client = null;
        NetworkStream networkStream;
        private TcpListener netServer;
        NetworkStream lsdStream     = null;
        Thread threadKeepAlive;
        bool bFirstTime             = true;

        //int iLSD_KeepAliveSec       = 2;
        bool bSuccessConnect        = false;
        bool bNetWorkStatus         = false;
        bool bLsdStatus             = false;
        bool bRunKeepAlive          = false;
        bool bRunInitial            = false;

        int iWaitRestart            = 10;

        int sendSeq                 = 0;
        //int iKeepAliveSec       = 2;

        string szIdLaneClose    = "7B226964656E746966696572223A223662656438666263383835333464633237353639323536396531353336333365227D";
        string szIdOpenMTC      = "7B226964656E746966696572223A223939336264316163336539633634353336303131306265306161346130663833227D";
        string szIdOpenETC      = "7B226964656E746966696572223A226635653765326430383031336365656435373532646539343166323263323636227D";
        string szIdOpenDual     = "7B226964656E746966696572223A223830623031396562616561353838636264383133356238373533623438393232227D";

        //---------------------------------------------
        public LaneSocketControl_LSD()
        {
            if (!TCT_Lib.bEnableLSD)
            {
                LogInfo.WriteLogLSD(LogLevel.DEBUG, "+ LaneSocketControl_LSD : disabled (bEnableLSD=false)");
                return;
            }


            LogInfo.WriteLogLSD("");
            LogInfo.WriteLogLSD("=====================================================================================================================");
            LogInfo.WriteLogLSD(LogLevel.DEBUG, $"+ LaneSocketControl_LSD : endpoint={TCT_Lib.szLSD_IPAddress}:{TCT_Lib.iLSD_SocketPort}, keepAliveSec={TCT_Lib.iLSD_KeepAliveSec}, retryTime={TCT_Lib.iLSD_RetryTime}, retryWaitSec={TCT_Lib.iLSD_RetryWaitSec}");
            try
            {
                bRunKeepAlive   = false;
                bLsdStatus      = false;
                if (TCT_Lib.iLSD_KeepAliveSec > 0)
                {
                    LogInfo.WriteLogLSD("+ Initial LSD with Thread");
                    threadKeepAlive                 = new Thread(new ThreadStart(ThreadEventSendKeepAlive));
                    threadKeepAlive.Priority        = ThreadPriority.Normal;
                    threadKeepAlive.IsBackground    = true;
                    threadKeepAlive.Start();
                }
/*
                else
                {
                    LogInfo.WriteLogLSD("+ Initial LSD with Single Time (Without thread)");
                }
*/                
            }
            catch (Exception err)
            {
                Console.WriteLine("- Fail Initial LSD :" + err.Message);
                LogInfo.WriteLogLSD("- Fail Initial LSD :" + err.Message);
                LogInfo.WriteLogLSD(LogLevel.DEBUG, "- LaneSocketControl_LSD : stack=" + err.StackTrace);
            }
        }

        async private void ThreadEventSendKeepAlive()
        {
/*
            if (TCT_Lib.TestConnection(TCT_Lib.szLSD_IPAddress, TCT_Lib.iLSD_SocketPort))
            {
                if (client == null)
                {
                    bSuccessConnect = await InitialSocketConnection();
                }
                else if (!client.Connected)
                {
                    CloseSocketConnection();
                    bSuccessConnect = await InitialSocketConnection();
                }
                //------------------------------------------------------
                if (!bLsdStatus)
                    await Cmd_InitialLSD();

                bRunKeepAlive = true;
            }
*/
            //int iRetry = 0;
            LogInfo.WriteLogLSD(LogLevel.DEBUG, $"+ LaneSocketControl_LSD.ThreadEventSendKeepAlive : start, interval={TCT_Lib.iLSD_KeepAliveSec}s");
            while (true)
            {
                try
                {
                    LogInfo.WriteLogLSD(LogLevel.TRACE, $"+ LaneSocketControl_LSD.ThreadEventSendKeepAlive : tick, runKeepAlive={bRunKeepAlive}, lsdStatus={bLsdStatus}");
                    //if (bLsdStatus)
                    {
                        if (bRunKeepAlive)
                            await CmdKeepAlive();
/*
                    else
                    {
                        if (TCT_Lib.TestConnection(TCT_Lib.szLSD_IPAddress, TCT_Lib.iLSD_SocketPort))
                        {
                            if (client == null)
                            {
                                bSuccessConnect = await InitialSocketConnection();
                            }
                            else if (!client.Connected)
                            {
                                CloseSocketConnection();
                                bSuccessConnect = await InitialSocketConnection();
                            }
                            bRunKeepAlive = bSuccessConnect;
                        }
                    }
*/
                    }
                    /*
                    else
                    {
                        if (!await Cmd_InitialLSD())
                        {
                            bRunKeepAlive = false;
                            if (TCT_Lib.iLSD_KeepAliveSec == 0)
                            {
                                iRetry++;
                                if (iRetry > 3)
                                {
                                    CloseSocketConnection();
                                    threadKeepAlive.Abort();
                                }
                            }
                        }
                        else
                        {
                            if (TCT_Lib.iLSD_KeepAliveSec == 0)
                            {
                                CloseSocketConnection();
                                threadKeepAlive.Abort();
                                break;
                            }
                        }
                    }
                    */
                    //await Task.Delay(iLSD_KeepAliveSec * 1000);
                }
                catch (ThreadAbortException err)
                {
                    LogInfo.WriteLogLSD("- Thread KeeoAlive Abort for stop Loop :" + err.Message);
                    break;
                }
                catch (Exception err)
                {
                    LogInfo.WriteLogLSD("- Fail ThreadEventSendKeyAlive :" + err.Message);
                    LogInfo.WriteLogLSD(LogLevel.DEBUG, "- LaneSocketControl_LSD.ThreadEventSendKeepAlive : stack=" + err.StackTrace);
                    //await Task.Delay(60 * 1000);
                }
                finally
                {
                    //bRunKeepAlive = true;
                    await Task.Delay((TCT_Lib.iLSD_KeepAliveSec * 1000));
                    if (bLsdStatus)
                        bRunKeepAlive = true;
                }
            }
            LogInfo.WriteLogLSD(LogLevel.DEBUG, "+ LaneSocketControl_LSD.ThreadEventSendKeepAlive : loop exit");
        }

        async public Task<bool> InitialSocketConnection()
        {
            try
            {
                /*
                                if (!TCT_Lib.TestConnection(TCT_Lib.szLSD_IPAddress, TCT_Lib.iLSD_SocketPort))
                                {
                                    bNetWorkStatus  = false;
                                    bRunKeepAlive   = false;
                                    Console.WriteLine("- Fail Connect LSD Socket");
                                    LogInfo.WriteLogLSD("- Fail Connect LSD IPAddress");
                                    return false;
                                }
                */

                LogInfo.WriteLogLSD(LogLevel.DEBUG, $"+ LaneSocketControl_LSD.InitialSocketConnection : connect {TCT_Lib.szLSD_IPAddress}:{TCT_Lib.iLSD_SocketPort}, timeout=500ms");
                System.Diagnostics.Stopwatch swConnect = System.Diagnostics.Stopwatch.StartNew();
                client      = new TcpClient();
                var result  = client.BeginConnect(TCT_Lib.szLSD_IPAddress, TCT_Lib.iLSD_SocketPort, null, null);
                result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(500));
                LogInfo.WriteLogLSD(LogLevel.DEBUG, $"+ LaneSocketControl_LSD.InitialSocketConnection : connected={client.Connected}, elapsed={swConnect.ElapsedMilliseconds}ms");
                if (!client.Connected)
                {
                    client.Close();
                    client          = null;
                    bNetWorkStatus  = false;
                    bRunKeepAlive   = false;
                    Console.WriteLine("- Fail Connect LSD Socket");
                    LogInfo.WriteLogLSD("- Fail Connect LSD IPAddress");
                    return false;
                }

                bNetWorkStatus = true;

                //LogInfo.WriteLogLSD("+ Connect LSD IP Address : "+ TCT_Lib.szLSD_IPAddress + ", Port : "+ TCT_Lib.iLSD_SocketPort);
                //client          = new TcpClient(TCT_Lib.szLSD_IPAddress, TCT_Lib.iLSD_SocketPort);
                networkStream   = client.GetStream();
                LogInfo.WriteLogLSD("New TcpClient Success : " + TCT_Lib.szLSD_IPAddress);
                //networkStream.ReadTimeout   = 1000;
                networkStream.WriteTimeout  = 2000;

                //bRunKeepAlive = false;
                //if (!bSuccessConnect)
                //    await Cmd_InitialLSD();

                //bRunKeepAlive = true;

                LogInfo.WriteLogLSD(LogLevel.DEBUG, $"+ LaneSocketControl_LSD.InitialSocketConnection : maint={TCT_Lib.bMaintenanceMode}, lsdStatus={bLsdStatus}, runInitial={bRunInitial}, lsdMode={TCT_Lib.eCurLsdMode}");
                if (!TCT_Lib.bMaintenanceMode)
                {
                    if (bLsdStatus)
                    {
                        await Task.Delay(300);
                        if (!bRunInitial)
                        {
                            LogInfo.WriteLogLSD(LogLevel.DEBUG, $"+ LaneSocketControl_LSD.InitialSocketConnection : re-show current mode {TCT_Lib.eCurLsdMode}");
                            //--------------------------------------------------------------------------
                            if (TCT_Lib.eCurLsdMode == LsdMode.LSD_CLOSE)
                                await CmdShow_LaneClose();
                            else if (TCT_Lib.eCurLsdMode == LsdMode.LSD_MTC)
                                await CmdShow_OpenMTC();
                            else if (TCT_Lib.eCurLsdMode == LsdMode.LSD_ETC)
                                await CmdShow_OpenETC();
                            else if (TCT_Lib.eCurLsdMode == LsdMode.LSD_DUAL)
                                await CmdShow_OpenDual();
                            //------------------------------------------
                        }

                        if (TCT_Lib.iLSD_KeepAliveSec == 0)
                        {
                            //--- Close Connnection
                            LogInfo.WriteLogLSD(LogLevel.DEBUG, "+ LaneSocketControl_LSD.InitialSocketConnection : keepAliveSec=0, close connection + abort keep-alive thread");
                            CloseSocketConnection();
                            threadKeepAlive.Abort();
                            return true;
                        }
                    }
                }


                return true;
            }
            catch (ThreadAbortException err)
            {
                LogInfo.WriteLogLSD("- Thread KeeoAlive Abort for stop Loop :" + err.Message);
                return false;
            }
            catch (Exception err)
            {
                bNetWorkStatus  = false;
                bRunKeepAlive   = true;
                Console.WriteLine("- Fail InitialSocketConnection LSD :" + err.Message);
                LogInfo.WriteLogLSD("- Fail InitialSocketConnection LSD :" + err.Message);
                LogInfo.WriteLogLSD(LogLevel.DEBUG, "- LaneSocketControl_LSD.InitialSocketConnection : stack=" + err.StackTrace);
                return false;
            }
        }

        async public Task<bool> RunInitialLSD(bool bAutoCloseConnect=true)
        {
            try
            {
                TCT_Lib.bCurLsdStatus = true;
                LogInfo.WriteLogLSD(LogLevel.DEBUG, $"+ LaneSocketControl_LSD.RunInitialLSD : autoClose={bAutoCloseConnect}, client={(client == null ? "null" : client.Connected ? "connected" : "disconnected")}");

                int iMaxInitTime = 3;
                int iCurInitTime = 1;
                while (client == null)
                {
                    if (iCurInitTime > iMaxInitTime)
                    {
                        LogInfo.WriteLogLSD(LogLevel.WARN, $"- LaneSocketControl_LSD.RunInitialLSD : cannot connect after {iMaxInitTime} tries, give up");
                        return false;
                    }
                    bSuccessConnect = await InitialSocketConnection();
                    if (!bSuccessConnect)
                    {
                        TCT_Lib.bCurLsdStatus = false;
                        LogInfo.WriteLogLSD("- Wait InitialSocketConnection LSD : 3 sec.");
                        await Task.Delay(3000);
                    }
                    iCurInitTime++;
                }

                if (!client.Connected)
                {
                    LogInfo.WriteLogLSD(LogLevel.WARN, "- LaneSocketControl_LSD.RunInitialLSD : client not connected, reconnect");
                    TCT_Lib.bCurLsdStatus = false;
                    CloseSocketConnection();
                    bSuccessConnect = await InitialSocketConnection();
                    if (!bSuccessConnect)
                        return false;
                }
                TCT_Lib.bCurLsdStatus = true;
                //------------------------------------------------------
                bLsdStatus      = false;
                int iLoopCount  = 1;
                do
                {
                    LogInfo.WriteLogLSD($"+ Initial LSD Time# = {iLoopCount}/{TCT_Lib.iLSD_RetryTime}");
                    bool bResult = await Cmd_InitialLSD();
                    LogInfo.WriteLogLSD("+ Result LSD Status = " + bLsdStatus);
                    if (!bLsdStatus)
                    {
                        LogInfo.WriteLogLSD($"- Wait Cmd_InitialLSD : {TCT_Lib.iLSD_RetryWaitSec} sec.");
                        await Task.Delay(TCT_Lib.iLSD_RetryWaitSec*1000);
                    }

                    //-------------------------------
                    if (iLoopCount >= TCT_Lib.iLSD_RetryTime)
                    {
                        TCT_Lib.bCurLsdStatus = false;
                        LogInfo.WriteLogLSD("+ End fail Initial LSD (More than time setting)");
                        CloseSocketConnection();
                        return false;
                    }
                    iLoopCount++;
                    //-------------------------------

                } while (!bLsdStatus);

                TCT_Lib.bCurLsdStatus = true;

                if (bAutoCloseConnect)
                {
                    LogInfo.WriteLogLSD("+ Finish LSD Connection.");
                    CloseSocketConnection();
                }

                return true;
            }
            catch (Exception err)
            {
                LogInfo.WriteLogLSD("-Error RunInitialLSD : " + err.Message);
                LogInfo.WriteLogLSD(LogLevel.DEBUG, "- LaneSocketControl_LSD.RunInitialLSD : stack=" + err.StackTrace);
                return false;
            }
        }

        async public Task<bool> RunInitialLSD_V0()
        {
            LogInfo.WriteLogLSD(LogLevel.DEBUG, $"+ LaneSocketControl_LSD.RunInitialLSD_V0 : client={(client == null ? "null" : client.Connected ? "connected" : "disconnected")}, lsdStatus={bLsdStatus}");
            if (client == null)
            {
                bSuccessConnect = await InitialSocketConnection();
                if (!bSuccessConnect)
                    return false;
            }
            else if (!client.Connected)
            {
                CloseSocketConnection();
                bSuccessConnect = await InitialSocketConnection();
                if (!bSuccessConnect)
                    return false;
            }

            //------------------------------------------------------
            if (bLsdStatus)
                return true;
            else
                return await Cmd_InitialLSD();
        }


        async public Task<bool> Cmd_InitialLSD()
        {
            LogInfo.WriteLogLSD("+ Initial LSD Command");
            bLsdStatus      = true;
            bRunKeepAlive   = false;
            bRunInitial     = true;

            await Task.Delay(200);
            if (await CmdKeepAlive())
            {
                await Task.Delay(200);
                if (await Cmd_LORA_SYNC())
                {
                    await Task.Delay(200);
                    if (await Cmd_ROTATION())
                    {
                        await Task.Delay(200);
                        if (await Cmd_SYNC1())
                        {
                            await Task.Delay(200);
                            if (await Cmd_SYNC2())
                            {
                                await Task.Delay(200);
                                if (await Cmd_SYNC3())
                                {
                                    await Task.Delay(200);
                                    await CmdKeepAlive();
                                }
                                else
                                    bLsdStatus = false;
                            }
                            else
                                bLsdStatus = false;
                        }
                        else
                            bLsdStatus = false;
                    }
                    else
                        bLsdStatus = false;
                }
                else
                    bLsdStatus = false;
            }
            else
                bLsdStatus = false;

            if (!TCT_Lib.bMaintenanceMode)
            {
                if (bLsdStatus)
                {
                    await Task.Delay(300);
                    //--------------------------------------------------------------------------
                    if (TCT_Lib.eCurLsdMode == LsdMode.LSD_CLOSE)
                        await CmdShow_LaneClose();
                    else if (TCT_Lib.eCurLsdMode == LsdMode.LSD_MTC)
                        await CmdShow_OpenMTC();
                    else if (TCT_Lib.eCurLsdMode == LsdMode.LSD_ETC)
                        await CmdShow_OpenETC();
                    else if (TCT_Lib.eCurLsdMode == LsdMode.LSD_DUAL)
                        await CmdShow_OpenDual();
                }
            }

            bRunInitial     = false;
            bRunKeepAlive   = true;
            LogInfo.WriteLogLSD(bLsdStatus ? LogLevel.DEBUG : LogLevel.WARN, $"{(bLsdStatus ? "+" : "-")} LaneSocketControl_LSD.Cmd_InitialLSD : result lsdStatus={bLsdStatus}, maint={TCT_Lib.bMaintenanceMode}, lsdMode={TCT_Lib.eCurLsdMode}");
            return bLsdStatus;
        }

        async private Task<bool> ReInitialLSD()
        {
            bRunKeepAlive   = false;
            bLsdStatus      = false;
            LogInfo.WriteLogLSD(LogLevel.WARN, $"- LaneSocketControl_LSD.ReInitialLSD : close socket, wait {iWaitRestart}s then re-initial");
            CloseSocketConnection();
            //Thread.Sleep(2 * 1000);
            await Task.Delay(iWaitRestart * 1000);
            //--- Restart Socket
            LogInfo.WriteLogLSD("Restart to InitialSocketConnection");
            return await Cmd_InitialLSD();
/*
            bSuccessConnect = await InitialSocketConnection();
            if (bSuccessConnect)
                return await Cmd_InitialLSD();
            else
                return false;
*/                               
        }

        public void CloseSocketConnection()
        {
            LogInfo.WriteLogLSD("+ CloseSocketConnection");
            LogInfo.WriteLogLSD(LogLevel.DEBUG, $"+ LaneSocketControl_LSD.CloseSocketConnection : stream={(networkStream != null)}, client={(client != null)}");
            if (networkStream != null)
            {
                networkStream.Close();
                networkStream.Dispose();
                networkStream = null;
            }
            if (client != null)
            {
                client.Close();
                client.Dispose();
                client = null;
            }
        }

        async private Task<bool> CmdKeepAlive()
        {
            return await SendCmdLSD("KeepAlive", "41564f4e000000004f50030000000000000000000000d601");
        }

        async private Task<bool> Cmd_LORA_SYNC()
        {
            string szCmd = GenCmdLSD("0000005152990000000000750000000000", "7b22736f75726365223a7b2274797065223a312c22706c6174666f726d223a327d2c227461736b4172726179223a5b7b2274797065223a224c4f52415f53594e43222c22616374696f6e223a352c22737461747573223a302c226572726f72436f6465223a302c2264617461223a6e756c6c7d5d7d");
            LogInfo.WriteLogLSD("+ Send LORA_SYNC : " + ConvertStrByteToString("7b22736f75726365223a7b2274797065223a312c22706c6174666f726d223a327d2c227461736b4172726179223a5b7b2274797065223a224c4f52415f53594e43222c22616374696f6e223a352c22737461747573223a302c226572726f72436f6465223a302c2264617461223a6e756c6c7d5d7d"));
            return await SendCmdLSD("LORA_SYNC", szCmd);
        }

        async private Task<bool> Cmd_ROTATION()
        {
            string szCmd = GenCmdLSD("0000005152990000000000740000000000", "7b22736f75726365223a7b2274797065223a312c22706c6174666f726d223a327d2c227461736b4172726179223a5b7b2274797065223a22524f544154494f4e222c22616374696f6e223a352c22737461747573223a302c226572726f72436f6465223a302c2264617461223a6e756c6c7d5d7d");
            LogInfo.WriteLogLSD("+ Send ROTATION : " + ConvertStrByteToString("7b22736f75726365223a7b2274797065223a312c22706c6174666f726d223a327d2c227461736b4172726179223a5b7b2274797065223a22524f544154494f4e222c22616374696f6e223a352c22737461747573223a302c226572726f72436f6465223a302c2264617461223a6e756c6c7d5d7d"));
            return await SendCmdLSD("ROTATION", szCmd);
        }

        async private Task<bool> Cmd_SYNC1()
        {
            string szCmd = GenCmdLSD("00000051521600050b0000000000000000", "");
            return await SendCmdLSD("SYNC1", szCmd);
        }

        async private Task<bool> Cmd_SYNC2()
        {
            string szCmd = GenCmdLSD("00000051521e0005010000000000000000", "");
            return await SendCmdLSD("SYNC2", szCmd);
        }

        async private Task<bool> Cmd_SYNC3()
        {
            string szCmd = GenCmdLSD("0000005152260005000000000000000000", "");
            return await SendCmdLSD("SYNC3", szCmd);
        }

        async public Task<bool> CmdShow_LaneClose()
        {
            try
            {
                TCT_Lib.iLsdMode_Status = 4;
                string szCmd = GenCmdLSD("00000051521e0009020000310000000000", szIdLaneClose);
                LogInfo.WriteLogLSD("+ Show LaneClose : " + ConvertStrByteToString(szIdLaneClose));
                return await SendCmdLSD("ShowLaneClose", szCmd);
            }
            catch (Exception err)
            {
                LogInfo.WriteLogLSD("- Error CmdShow_LaneClose : " + err.Message);
                return false;
            }
        }

        async public Task<bool> CmdClear_LaneClose()
        {
            try
            {
                string szCmd = GenCmdLSD("00000051521e000b020000310000000000", szIdLaneClose);
                LogInfo.WriteLogLSD("+ Clear LaneClose : " + ConvertStrByteToString(szIdLaneClose));
                return await SendCmdLSD("ClearLaneClose", szCmd);
            }
            catch (Exception err)
            {
                LogInfo.WriteLogLSD("- Error CmdClear_LaneClose : " + err.Message);
            return false;
            }
        }

        async public Task<bool> CmdShow_OpenMTC()
        {
            try
            {
                TCT_Lib.iLsdMode_Status = 1;
                string szCmd = GenCmdLSD("00000051521e0009020000310000000000", szIdOpenMTC);
                LogInfo.WriteLogLSD("+ Show OpenMTC : " + ConvertStrByteToString(szIdOpenMTC));
                return await SendCmdLSD("ShowOpenMTC", szCmd);
            }
            catch (Exception err)
            {
                LogInfo.WriteLogLSD("- Error CmdShow_OpenMTC : " + err.Message);
                return false;
            }
        }

        async public Task<bool> CmdClear_OpenMTC()
        {
            try
            {
                string szCmd = GenCmdLSD("00000051521e000b020000310000000000", szIdOpenMTC);
                LogInfo.WriteLogLSD("+ Clear OpenMTC : " + ConvertStrByteToString(szIdOpenMTC));
                return await SendCmdLSD("ClearOpenMTC", szCmd);

            }
            catch (Exception err)
            {
                LogInfo.WriteLogLSD("- Error CmdClear_OpenMTC : " + err.Message);
                return false;
            }
        }

        async public Task<bool> CmdShow_OpenETC()
        {
            try
            {
                TCT_Lib.iLsdMode_Status = 2;
                string szCmd = GenCmdLSD("00000051521e0009020000310000000000", szIdOpenETC);
                LogInfo.WriteLogLSD("+ Show OpenETC : " + ConvertStrByteToString(szIdOpenETC));
                return await SendCmdLSD("ShowOpenETC", szCmd);
            }
            catch (Exception err)
            {
                LogInfo.WriteLogLSD("- Error CmdShow_OpenETC : " + err.Message);
            return false;
            }
        }

        async public Task<bool> CmdClear_OpenETC()
        {
            try
            {
                string szCmd = GenCmdLSD("00000051521e000b020000310000000000", szIdOpenETC);
                LogInfo.WriteLogLSD("+ Clear OpenETC : " + ConvertStrByteToString(szIdOpenETC));
                return await SendCmdLSD("ClearOpenETC", szCmd);
            }
            catch (Exception err)
            {
                LogInfo.WriteLogLSD("- Error CmdClear_OpenETC : " + err.Message);
                return false;
            }
        }

        async public Task<bool> CmdShow_OpenDual()
        {
            try
            {
                TCT_Lib.iLsdMode_Status = 3;
                string szCmd = GenCmdLSD("00000051521e0009020000310000000000", szIdOpenDual);
                LogInfo.WriteLogLSD("+ Show OpenDual : " + ConvertStrByteToString(szIdOpenDual));
                return await SendCmdLSD("ShowOpenDual", szCmd);
            }
            catch (Exception err)
            {
                LogInfo.WriteLogLSD("- Error CmdShow_OpenDual : " + err.Message);
                return false;
            }
        }

        async public Task<bool> CmdClear_OpenDual()
        {
            try
            {
                string szCmd = GenCmdLSD("00000051521e000b020000310000000000", szIdOpenDual);
                LogInfo.WriteLogLSD("+ Clear OpenDual : " + ConvertStrByteToString(szIdOpenDual));
                return await SendCmdLSD("ClearOpenDual", szCmd);
            }
            catch (Exception err)
            {
                LogInfo.WriteLogLSD("- Error CmdClear_OpenDual : " + err.Message);
            return false;
            }
        }

        private string GenCmdLSD(string szCmd1, string szCmdJson)
        {
            string szHexCmd = "41564f4e";   // Header
            sendSeq++;
            if (sendSeq >= 255)
                sendSeq = 1;

            byte[] seqBytes = BitConverter.GetBytes(sendSeq);
            string szSeq    = ByteArrayToString(seqBytes).Remove(2);  // Seq

            szHexCmd        += szSeq;
            szHexCmd        += szCmd1;
            byte[] bb       = StringToByteArray(szHexCmd);
            szHexCmd        += GetCRC(bb);
            szHexCmd        += szCmdJson;
            LogInfo.WriteLogLSD(LogLevel.TRACE, $"+ LaneSocketControl_LSD.GenCmdLSD : seq={sendSeq}, cmd1={szCmd1}, jsonLen={(szCmdJson == null ? 0 : szCmdJson.Length / 2)}");

            return szHexCmd.ToLower();
        }

        async private Task<bool> SendCmdLSD(string szCmdType, string szStrHex)
        {
            /*
                        if (szCmdType == "KeepAlive")
                        {
                            if (!bRunKeepAlive)
                                return false;
                        }
            */

            //if (!await TCT_Lib.IsPortReachable(TCT_Lib.szLSD_IPAddress, TCT_Lib.iLSD_SocketPort, 500))

/*
            if (!TCT_Lib.TestConnection(TCT_Lib.szLSD_IPAddress, TCT_Lib.iLSD_SocketPort))
            {
                bNetWorkStatus      = false;
                bRunKeepAlive       = false;
                Console.WriteLine("- Fail Connect LSD Socket");
                LogInfo.WriteLogLSD("- Fail Connect LSD IPAddress");
                return false;
            }
*/
           
            bNetWorkStatus  = true;
            bRunKeepAlive   = true;
            LogLevel eCmdLevel = szCmdType == "KeepAlive" ? LogLevel.TRACE : LogLevel.DEBUG;
            LogInfo.WriteLogLSD(eCmdLevel, $"+ LaneSocketControl_LSD.SendCmdLSD : cmd={szCmdType}, bytes={(szStrHex == null ? 0 : szStrHex.Length / 2)}, client={(client != null)}, lsdStatus={bLsdStatus}");

            try
            {
                if (client == null)
                {
                    bSuccessConnect = await InitialSocketConnection();
                    if (!bSuccessConnect)
                    {
                        LogInfo.WriteLogLSD(LogLevel.WARN, $"- LaneSocketControl_LSD.SendCmdLSD : not connected, dropped cmd={szCmdType}");
                        return false;
                    }
                }
/*
                else if (!client.Connected)
                {
                    CloseSocketConnection();
                    bSuccessConnect = await InitialSocketConnection();
                    if (!bSuccessConnect)
                        return false;
                }
*/
                if (szCmdType != "KeepAlive")
                {
                    if (!bLsdStatus)
                    {
                        LogInfo.WriteLogLSD(LogLevel.WARN, $"- LaneSocketControl_LSD.SendCmdLSD : LSD not initialized, re-initial before cmd={szCmdType}");
                        if (!await ReInitialLSD())
                        {
                            LogInfo.WriteLogLSD(LogLevel.WARN, $"- LaneSocketControl_LSD.SendCmdLSD : re-initial failed, dropped cmd={szCmdType}");
                            return false;
                        }
                    }
                }

                if (networkStream == null)
                {
                    LogInfo.WriteLogLSD(LogLevel.WARN, $"- LaneSocketControl_LSD.SendCmdLSD : networkStream is null, dropped cmd={szCmdType}");
                    return false;
                }

                bRunKeepAlive   = true;
                byte[] msg      = StringToByteArray(szStrHex);
                System.Diagnostics.Stopwatch swCmd = System.Diagnostics.Stopwatch.StartNew();
                networkStream.Write(msg, 0, msg.Length);
                networkStream.Flush();
                if (szCmdType == "KeepAlive" && LogInfo.IsEnabled(LogLevel.TRACE))
                    LogInfo.WriteLogLSD(LogLevel.TRACE, "+ LaneSocketControl_LSD.SendCmdLSD : KeepAlive tx=" + szStrHex);

                if (szCmdType != "KeepAlive")
                {
                    LogInfo.WriteLogLSD("+ Send " + szCmdType + " : " + GenHexCharList(msg, msg.Length));
                    //ShowResponse("Send Msg", msg, msg.Length);
                }
                else
                {
                    LogInfo.WriteLogLSD("+ Send KeepAlive");
                }

                //---------------------------------------------------------------------------
/*
                if (szCmdType == "KeepAlive")
                {
                    await Task.Delay(500);
                    return true;
                }
*/                
                if ((szCmdType == "KeepAlive") ||
                    (szCmdType == "ShowLaneClose") || (szCmdType == "ClearLaneClose") ||
                    (szCmdType == "ShowOpenMTC") || (szCmdType == "ClearOpenMTC") ||
                    (szCmdType == "ShowOpenETC") || (szCmdType == "ClearOpenETC") ||
                    (szCmdType == "ShowOpenDual") || (szCmdType == "ClearOpenDual"))
                    networkStream.ReadTimeout = 1000;
                else
                    networkStream.ReadTimeout = 3000;
                await Task.Delay(200);
                //---------------------------------------------------------------------------

                byte[] byteRespone = new byte[12000];
                int iLen = networkStream.Read(byteRespone, 0, byteRespone.Length);
                string szReponseStr = GenHexCharList(byteRespone, iLen);

                if (szCmdType != "KeepAlive")
                    LogInfo.WriteLogLSD("+ Recv Response " + szCmdType + " -> : " + szReponseStr);
                else
                    LogInfo.WriteLogLSD("+ Recv Response " + szCmdType);
                LogInfo.WriteLogLSD(eCmdLevel, $"+ LaneSocketControl_LSD.SendCmdLSD : cmd={szCmdType}, rxLen={iLen}, elapsed={swCmd.ElapsedMilliseconds}ms");
                if (szCmdType == "KeepAlive")
                    LogInfo.WriteLogLSD(LogLevel.TRACE, "+ LaneSocketControl_LSD.SendCmdLSD : KeepAlive rx=" + szReponseStr);

                //-----------------------------------------------------------------------
                if (szReponseStr.Trim() == "00")
                {
                    if ((szCmdType == "ShowLaneClose") || (szCmdType == "ClearLaneClose") ||
                       (szCmdType == "ShowOpenMTC") || (szCmdType == "ClearOpenMTC") ||
                       (szCmdType == "ShowOpenETC") || (szCmdType == "ClearOpenETC") ||
                       (szCmdType == "ShowOpenDual") || (szCmdType == "ClearOpenDual"))
                    {
                        LogInfo.WriteLogLSD("+ Can Recv data but no response string (Length=0) from LSD Controller. Then send retry again.");
                        LogInfo.WriteLogLSD(LogLevel.WARN, $"- LaneSocketControl_LSD.SendCmdLSD : empty response for cmd={szCmdType}, retry once");

                        msg = StringToByteArray(szStrHex);
                        networkStream.Write(msg, 0, msg.Length);
                        networkStream.Flush();

                        szReponseStr = GenHexCharList(byteRespone, iLen);
                        LogInfo.WriteLogLSD("+ ReSend " + szCmdType + " : " + szReponseStr);
                        networkStream.ReadTimeout = 2000;
                        await Task.Delay(200);
                        //---------------------------------------------------------------------------

                        byteRespone = new byte[12000];
                        iLen = networkStream.Read(byteRespone, 0, byteRespone.Length);
                        LogInfo.WriteLogLSD("+ Re-Recv Response " + szCmdType + " -> : " + GenHexCharList(byteRespone, iLen));

                        if (szReponseStr.Trim() == "00")
                        {
                            LogInfo.WriteLogLSD(LogLevel.WARN, $"- LaneSocketControl_LSD.SendCmdLSD : cmd={szCmdType} result=false (empty response after retry)");
                            return false;
                        }
                    }
                }
                //------------------------------------------------------------------------

                if (iLen > 60)
                {
                    string szMsg = ConvertStrByteToString(GenHexCharList(byteRespone, iLen).Substring(80));
                    LogInfo.WriteLogLSD("+ Recv Msg : " + szMsg);
                    try
                    {
                        if ((szCmdType == "SYNC2") || (szCmdType == "SYNC3"))
                        {
                            if (szCmdType == "SYNC2")
                                LogInfo.WriteLogLSD("+ Found Msg : SYNC2");
                            else if (szCmdType == "SYNC3")
                                LogInfo.WriteLogLSD("+ Found Msg : SYNC3");
                            if ((szMsg.StartsWith("[{\"statusCode\":0,", StringComparison.Ordinal)) ||
                                (szMsg.StartsWith("[{\"statusCode\":1,", StringComparison.Ordinal)))
                            {
                                szMsg           = szMsg.Remove(szMsg.Length - 2);
                                LogInfo.WriteLogLSD("+ Msg2 SYNC2 : " + szMsg);
                                JArray objMsg   = (JArray)JsonConvert.DeserializeObject(szMsg);
                                LogInfo.WriteLogLSD("+ Found Obj Msg SYN2 = " + objMsg.Count);
                                foreach(dynamic obj in objMsg.ToList())
                                {
                                    if (obj["name"] != null)
                                    {
                                        if ((string)obj["name"] == "CLOSE")
                                        {
                                            string szIdClose    = (string)obj["identifier"];
                                            string szMsgClose   = "{\"identifier\":\"" + szIdClose + "\"}";
                                            szIdLaneClose       = GenHexCharList(szMsgClose).ToUpper();
                                            LogInfo.WriteLogLSD("+ Lane Close : " + szMsgClose + " => " + szIdLaneClose);
                                        }
                                        else if ((string)obj["name"] == "MTC")
                                        {
                                            string szIdMTC      = (string)obj["identifier"];
                                            string szMsgMTC     = "{\"identifier\":\"" + szIdMTC + "\"}";
                                            szIdOpenMTC         = GenHexCharList(szMsgMTC).ToUpper();
                                            LogInfo.WriteLogLSD("+ Open MTC : " + szMsgMTC + " => " + szIdOpenMTC);
                                        }
                                        else if ((string)obj["name"] == "ETC")
                                        {
                                            string szIdETC      = (string)obj["identifier"];
                                            string szMsgETC     = "{\"identifier\":\"" + szIdETC + "\"}";
                                            szIdOpenETC         = GenHexCharList(szMsgETC).ToUpper();
                                            LogInfo.WriteLogLSD("+ Open ETC : " + szMsgETC + " => " + szIdOpenETC);
                                        }
                                        else if ((string)obj["name"] == "ETC-MTC")
                                        {
                                            string szIdDual     = (string)obj["identifier"];
                                            string szMsgDual    = "{\"identifier\":\"" + szIdDual + "\"}";
                                            szIdOpenDual        = GenHexCharList(szMsgDual).ToUpper();
                                            LogInfo.WriteLogLSD("+ Open Dual : " + szMsgDual + " => " + szIdOpenDual);
                                        }
                                    }
                                }
                            }

                            //-------------------------------------------------------------------
                            LogInfo.WriteLogLSD("+ Lane Close Id = " + szIdLaneClose);
                            LogInfo.WriteLogLSD("+ Open MTC Id \t= " + szIdOpenMTC);
                            LogInfo.WriteLogLSD("+ Open ETC Id \t= " + szIdOpenETC);
                            LogInfo.WriteLogLSD("+ Open Dual Id = " + szIdOpenDual);
                        }

                        LogInfo.WriteLogLSD(eCmdLevel, $"+ LaneSocketControl_LSD.SendCmdLSD : cmd={szCmdType} result=true");
                        return true;
                    }
                    catch (Exception err)
                    {
                        bLsdStatus = false;
                        LogInfo.WriteLogLSD("- Error SYNC : " + err.Message);
                        LogInfo.WriteLogLSD(LogLevel.DEBUG, $"- LaneSocketControl_LSD.SendCmdLSD : cmd={szCmdType}, stack={err.StackTrace}");
                        return false;
                    }
                }
                LogInfo.WriteLogLSD(eCmdLevel, $"+ LaneSocketControl_LSD.SendCmdLSD : cmd={szCmdType} result=true (short response)");
                return true;
            }
            catch (SocketException err)
            {
                LogInfo.WriteLogLSD("- Error Send Command LSD (SocketException) : " + err.Message);
                LogInfo.WriteLogLSD(LogLevel.WARN, $"- LaneSocketControl_LSD.SendCmdLSD : cmd={szCmdType}, socket error code={err.SocketErrorCode}, reconnect + re-initial");
                bLsdStatus = false;
                await InitialSocketConnection();
                //if (!bLsdStatus)
                {
                    bool bResult = await ReInitialLSD();
                    LogInfo.WriteLogLSD(LogLevel.DEBUG, $"+ LaneSocketControl_LSD.SendCmdLSD : cmd={szCmdType}, re-initial result={bResult}");
                    return bResult;
                }
            }
            catch (IOException err)
            {
                LogInfo.WriteLogLSD("- Error Send Command LSD (IOException) : " + err.Message);
                LogInfo.WriteLogLSD(LogLevel.WARN, $"- LaneSocketControl_LSD.SendCmdLSD : cmd={szCmdType}, IO error (timeout/closed), reconnect + re-initial");
                bLsdStatus = false;
                await InitialSocketConnection();
                //if (!bLsdStatus)
                {
                    bool bResult = await ReInitialLSD();
                    LogInfo.WriteLogLSD(LogLevel.DEBUG, $"+ LaneSocketControl_LSD.SendCmdLSD : cmd={szCmdType}, re-initial result={bResult}");
                    return bResult;
                }
            }
            catch (Exception err)
            {
                if (szCmdType == "KeepAlive")
                    bLsdStatus = true;
                else
                    bLsdStatus = false;
                LogInfo.WriteLogLSD("- Error Send Command LSD : " + err.Message);
                LogInfo.WriteLogLSD(LogLevel.DEBUG, $"- LaneSocketControl_LSD.SendCmdLSD : cmd={szCmdType}, stack={err.StackTrace}");
                await InitialSocketConnection();
                //if (!bLsdStatus)
                {
                    bool bResult = await ReInitialLSD();
                    LogInfo.WriteLogLSD(LogLevel.DEBUG, $"+ LaneSocketControl_LSD.SendCmdLSD : cmd={szCmdType}, re-initial result={bResult}");
                    return bResult;
                }

                return bLsdStatus;
            }
        }

        private void ShowResponse(string szTitle, byte[] msg, int iLen)
        {
            Application.Invoke(delegate
            {

            });
        }

        private string GenHexCharList(byte[] ascii, int iLen)
        {
            string szStr = "";
            int i = 0;
            foreach (Byte b in ascii)
            {
                szStr += b.ToString("x2") + "";
                i++;
                if (i >= iLen)
                    break;
            }

            return szStr;
        }

        private string GetCRC(byte[] bMsg)
        {
            int lrc = 0x0;
            for (int i = 0; i < bMsg.Length; i++)
                lrc += bMsg[i];

            byte[] intBytes = BitConverter.GetBytes(lrc);
            //string str      = ByteArrayToString(intBytes).Substring(0, 4);
            //return str;
            string str = ByteArrayToString(intBytes).Substring(0, 2);
            return str + (intBytes[0] < 0x7F ? "02" : "01");
        }

        private string GenHexCharList(string szMsg)
        {
            string szStr = "";
            byte[] ascii = Encoding.ASCII.GetBytes(szMsg);
            foreach (Byte b in ascii)
                szStr += b.ToString("x2");

            return szStr;
        }

        private byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        private string ConvertStrByteToString(string hex)
        {
            byte[] bStr = StringToByteArray(hex);
            return Encoding.UTF8.GetString(bStr);
        }

        private string ByteArrayToString(byte[] ba)
        {
            return BitConverter.ToString(ba).Replace("-", "");
        }

    }
}
