using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace TCT_App
{
    public class MocRabbitMQ
    {
        private Thread threadImportStaff, threadTimerImportStaff;
        //-----------------------------------------------

/*
        private const string UserName           = "guest";
        private const string Password           = "dmtDmt@2020";
        private const string HostName           = "evthai.info";
*/

        private const string UserName           = "dmt_admin";
        private const string Password           = "dmt_admin";
        private const string HostName           = "192.168.244.252";
        private const int Port                  = 15672;
        private const string VirtualHost        = "cbe";

        ConnectionFactory connectionFactory = new ConnectionFactory
        {
            HostName                    = TCT_Lib.szDC_RabbitMQ_ServerHostName,
            VirtualHost                 = TCT_Lib.szDC_RabbitMQ_VirtualHost,
            UserName                    = TCT_Lib.szDC_RabbitMQ_User,
            Password                    = TCT_Lib.szDC_RabbitMQ_Password,
            Port                        = TCT_Lib.iDC_RabbitMQ_Port,
            RequestedConnectionTimeout  = TimeSpan.FromSeconds(1),
            ContinuationTimeout         = TimeSpan.FromSeconds(1),
            NetworkRecoveryInterval     = TimeSpan.FromSeconds(1),

        };

        public IConnection connection   = null;
        private IModel channel          = null;

        bool bWorkImportStaff           = false;
        //---------------------------------------------------------------
        public MocRabbitMQ()
        {
            InitialTimerImportStaffThread();
            /*
                        //--- Test Import Data
                        var dataPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "log/RabbitMQ");
                        var dataFileSTAFF   = Path.Combine(dataPath, "STAFF.log");
                        var dataFileTARIFF  = Path.Combine(dataPath, "TARIFF.log");
                        var dataFilePLAZA   = Path.Combine(dataPath, "PLAZA_CONFIGURATION.log");

                        try
                        {
                            if (File.Exists(dataFileSTAFF))
                            {
                                string szMessage    = File.ReadAllText(dataFileSTAFF);
                                dynamic msg         = JsonConvert.DeserializeObject(szMessage);
                                string paramName    = msg["parameterName"];
                                LogInfo.WriteRecvMsgRabbitMQ(paramName, szMessage);
                                if (paramName.ToUpper().Trim() == "STAFF")
                                {
                                    JArray staffList = msg["staff"];
                                    string timeStamp = msg["timestamp"];
                                    int version     = msg["version"];
                                    TCT_Lib.LaneDB.ImportStaffListToDB(staffList, version, timeStamp);
                                }
                            }

                            if (File.Exists(dataFileTARIFF))
                            {
                                string szMessage    = File.ReadAllText(dataFileTARIFF);
                                dynamic msg         = JsonConvert.DeserializeObject(szMessage);
                                string paramName    = msg["parameterName"];
                                LogInfo.WriteRecvMsgRabbitMQ(paramName, szMessage);
                                if (paramName.ToUpper().Trim() == "TARIFF")
                                {
                                    JArray tariffList   = msg["tariff"];
                                    string timeStamp    = msg["timestamp"];
                                    int version         = msg["version"];
                                    TCT_Lib.LaneDB.ImportTariffToDB(tariffList, version, timeStamp);
                                }
                            }

                            if (File.Exists(dataFilePLAZA))
                            {
                                string szMessage    = File.ReadAllText(dataFilePLAZA);
                                dynamic msg         = JsonConvert.DeserializeObject(szMessage);
                                string paramName    = msg["parameterName"];
                                LogInfo.WriteRecvMsgRabbitMQ(paramName, szMessage);
                                if (paramName.ToUpper().Trim() == "PLAZA_CONFIGURATION")
                                {
                                    JArray plazaList    = msg["plaza"];
                                    string timeStamp    = msg["timestamp"];
                                    int version         = msg["version"];
                                    TCT_Lib.LaneDB.ImportPlazaToDB(plazaList, version, timeStamp);
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            Console.WriteLine(err.Message);
                        }
            */
        }

        public bool ConnectMocRabbitMQ()
        {
            if (!TCT_Lib.bEnableDcRabbitMQ)
            {
                LogInfo.WriteLog(LogLevel.DEBUG, "+ MocRabbitMQ.ConnectMocRabbitMQ : skip, DC RabbitMQ disabled");
                return false;
            }


            if (!TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szDC_RabbitMQ_ServerIPAddress, TCT_Lib.iDC_RabbitMQ_ServerPort, 500))
            //if (!TCT_Lib.laneController.CheckConnectivity(TCT_Lib.szDC_RabbitMQ_ServerIPAddress))
            {
                Console.WriteLine($"- Fail TestNetworkConnection to RabbitMQ => Connect DC Rabbit MQ : IP = {TCT_Lib.szDC_RabbitMQ_ServerIPAddress}, PORT = {TCT_Lib.iDC_RabbitMQ_ServerPort}");
                LogInfo.WriteLog($"- Fail TestNetworkConnection to RabbitMQ => Connect DC Rabbit MQ : IP = {TCT_Lib.szDC_RabbitMQ_ServerIPAddress}, PORT = {TCT_Lib.iDC_RabbitMQ_ServerPort}");
                //connection  = null;
                //channel     = null;
                TCT_Lib.bDC_NetworkStatus = false;
                return false;
            }

            LogInfo.WriteLog($"DC HostName : {TCT_Lib.szDC_RabbitMQ_ServerHostName}");
            LogInfo.WriteLog($"DC VirtualHost : {TCT_Lib.szDC_RabbitMQ_VirtualHost}");
            LogInfo.WriteLog($"DC_Port : {TCT_Lib.iDC_RabbitMQ_Port}");
            LogInfo.WriteLog($"DC User : {TCT_Lib.szDC_RabbitMQ_User}");
            LogInfo.WriteLog($"DC Password : {LogInfo.MaskSecret(TCT_Lib.szDC_RabbitMQ_Password)}");

            System.Diagnostics.Stopwatch swConnect = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ MocRabbitMQ.ConnectMocRabbitMQ : connect amqp://{TCT_Lib.szDC_RabbitMQ_ServerHostName}:{TCT_Lib.iDC_RabbitMQ_Port}/{TCT_Lib.szDC_RabbitMQ_VirtualHost}, timeout=1s");
                connection  = connectionFactory.CreateConnection();
                channel     = connection.CreateModel();
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ MocRabbitMQ.ConnectMocRabbitMQ : connection + channel open, elapsed={swConnect.ElapsedMilliseconds}ms");
                LogInfo.WriteLog("+ Success Connect DC RabbitMQ");
                TCT_Lib.bDC_NetworkStatus = true;
                try
                {
                    // accept only one unack-ed message at a time
                    // uint prefetchSize, ushort prefetchCount, bool global
                    channel.BasicQos(0, 1, false);
                    var messageReceiver = new MocMessageReceiver(channel);
                    channel.BasicConsume(TCT_Lib.szDC_RabbitMQ_ParamsQueueName, false, messageReceiver);
                    messageReceiver.rabbitMQRecvMessage += MessageReceiverOnRabbitMqRecvMessage;
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ MocRabbitMQ.ConnectMocRabbitMQ : consume queue={TCT_Lib.szDC_RabbitMQ_ParamsQueueName}, prefetch=1, manualAck");
                }
                catch (Exception err)
                {
                    LogInfo.WriteLog("- Error MessageReceiver DC RabbitMQ : " + err.Message);
                    LogInfo.WriteLog(LogLevel.DEBUG, "- MocRabbitMQ.ConnectMocRabbitMQ : consume stack=" + err.StackTrace);
                    Console.WriteLine(err.Message);
                }

                return true;
            }
            catch (BrokerUnreachableException err)
            {
                Console.WriteLine($"-- Fail Connect DC RabbitMQ : {err.Message}");
                LogInfo.WriteLog($"-- Fail Connect DC RabbitMQ : {err.Message}");
                LogInfo.WriteLog(LogLevel.DEBUG, $"- MocRabbitMQ.ConnectMocRabbitMQ : broker unreachable after {swConnect.ElapsedMilliseconds}ms, inner={err.InnerException?.Message ?? "null"}");
                connection  = null;
                channel     = null;
                TCT_Lib.bDC_NetworkStatus = false;

                return false;
            }
            catch (Exception err)
            {
                Console.WriteLine($"- Fail Connect DC RabbitMQ : {err.Message}");
                LogInfo.WriteLog($"- Fail Connect DC RabbitMQ : {err.Message}");
                LogInfo.WriteLog(LogLevel.DEBUG, $"- MocRabbitMQ.ConnectMocRabbitMQ : elapsed={swConnect.ElapsedMilliseconds}ms, stack={err.StackTrace}");
                connection  = null;
                channel     = null;
                TCT_Lib.bDC_NetworkStatus = false;

                return false;
            }
        }

        public void CloseMocRabbitMQ()
        {
            try
            {
                LogInfo.WriteLog("-- Close Connection DC RabbitMQ");
                //channel.Close();
                //connection.Close();
                connection  = null;
                channel     = null;
            }
            catch (Exception err)
            {
                LogInfo.WriteLog(LogLevel.ERROR, "- Error CloseMocRabbitMQ : " + err.Message);
            }
        }

        public bool SendLaneEventRabbitMQ(string keyName, string msg)
        {
            //if (TCT_Lib.bMaintenanceMode)
            //    return false;

            if (!TCT_Lib.bEnableDcRabbitMQ)
            {
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ MocRabbitMQ.SendLaneEventRabbitMQ : skip key={keyName}, DC RabbitMQ disabled");
                return false;
            }

            if (!TCT_Lib.bDC_NetworkStatus)
            {
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ MocRabbitMQ.SendLaneEventRabbitMQ : skip key={keyName}, DC network status=false");
                return false;
            }

            if (!TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szDC_RabbitMQ_ServerIPAddress, TCT_Lib.iDC_RabbitMQ_ServerPort, TCT_Lib.iDC_RabbitMQ_TimeoutSec * 500))
             {
                Console.WriteLine($"- Fail TestNetworkConnection to RabbitMQ => SendLaneEventRabbitMQ : IP = {TCT_Lib.szDC_RabbitMQ_ServerIPAddress}, PORT = {TCT_Lib.iDC_RabbitMQ_ServerPort}");
                LogInfo.WriteLog($"- Fail TestNetworkConnection to RabbitMQ => SendLaneEventRabbitMQ : IP = {TCT_Lib.szDC_RabbitMQ_ServerIPAddress}, PORT = {TCT_Lib.iDC_RabbitMQ_ServerPort}");
                LogInfo.WriteLog("- Close Connection DC RabbitMQ");
                //CloseMocRabbitMQ();
                //connection  = null;
                //channel     = null;
                TCT_Lib.bDC_NetworkStatus = false;
                return false;
            }          

            try
            {
                TCT_Lib.bDC_NetworkStatus = true;
                if (connection == null)
                {
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ MocRabbitMQ.SendLaneEventRabbitMQ : no connection, connect before key={keyName}");
                    if (!ConnectMocRabbitMQ())
                    {
                        LogInfo.WriteLog(LogLevel.WARN, $"- MocRabbitMQ.SendLaneEventRabbitMQ : connect failed, dropped key={keyName}");
                        return false;
                    }
                }
                System.Diagnostics.Stopwatch swPublish = System.Diagnostics.Stopwatch.StartNew();
                var properties          = channel.CreateBasicProperties();
                properties.Persistent   = false;

                // Bind Queue to Exchange
                channel.QueueBind(TCT_Lib.szDC_RabbitMQ_EventQueueName, TCT_Lib.szDC_RabbitMQ_EventExchangeName, keyName);

                byte[] messagebuffer    = Encoding.Default.GetBytes(msg);
                channel.BasicPublish(exchange: TCT_Lib.szDC_RabbitMQ_EventExchangeName,
                                        routingKey: keyName,
                                        basicProperties: null,
                                        body: messagebuffer);
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ MocRabbitMQ.SendLaneEventRabbitMQ : published exchange={TCT_Lib.szDC_RabbitMQ_EventExchangeName}, queue={TCT_Lib.szDC_RabbitMQ_EventQueueName}, routingKey={keyName}, bytes={messagebuffer.Length}, elapsed={swPublish.ElapsedMilliseconds}ms");

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine($"- Error SendLaneEventRabbitMQ DC : {err.Message}");
                LogInfo.WriteLog($"- Error SendLaneEventRabbitMQ DC : {err.Message}");
                LogInfo.WriteLog(LogLevel.DEBUG, $"- MocRabbitMQ.SendLaneEventRabbitMQ : key={keyName}, stack={err.StackTrace}");
                connection  = null;
                channel     = null;
                //-----------------------------------------------------------------------------------
                if (err.Message.StartsWith("Already closed", StringComparison.Ordinal))
                {
                    LogInfo.WriteLog("+ Fail RabbitMQ DC : This will try to re-connect RabbitMQ.");
                    LogInfo.WriteLog(LogLevel.WARN, "- MocRabbitMQ.SendLaneEventRabbitMQ : connection already closed, reconnect");
                    //CloseMocRabbitMQ();
                    ConnectMocRabbitMQ();
                }
                //------------------------------------------------------------------------------------

                return false;
            }
        }

        public bool SendLaneTransactionRabbitMQ(string keyName, string msg)
        {
            //if (TCT_Lib.bMaintenanceMode)
            //    return false;

            if (!TCT_Lib.bEnableDcRabbitMQ)
            {
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ MocRabbitMQ.SendLaneTransactionRabbitMQ : skip key={keyName}, DC RabbitMQ disabled");
                return false;
            }

            if (!TCT_Lib.bDC_NetworkStatus)
            {
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ MocRabbitMQ.SendLaneTransactionRabbitMQ : skip key={keyName}, DC network status=false");
                return false;
            }
                
            if (!TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szDC_RabbitMQ_ServerIPAddress, TCT_Lib.iDC_RabbitMQ_ServerPort, TCT_Lib.iDC_RabbitMQ_TimeoutSec * 1000))
            {
                TCT_Lib.bDC_NetworkStatus = false;
                Console.WriteLine($"- Fail TestNetworkConnection to RabbitMQ => SendLaneTransactionRabbitMQ : IP = {TCT_Lib.szDC_RabbitMQ_ServerIPAddress}, PORT = {TCT_Lib.iDC_RabbitMQ_ServerPort}");
                LogInfo.WriteLog($"- Fail TestNetworkConnection to RabbitMQ => SendLaneTransactionRabbitMQ : IP = {TCT_Lib.szDC_RabbitMQ_ServerIPAddress}, PORT = {TCT_Lib.iDC_RabbitMQ_ServerPort}");
                return false;
            }          

            try
            {
                TCT_Lib.bDC_NetworkStatus = true;
                LogInfo.WriteLog("+ Start Send LaneTransaction to RAbbitMQ");
                if (connection == null)
                {
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ MocRabbitMQ.SendLaneTransactionRabbitMQ : no connection, connect before key={keyName}");
                    if (!ConnectMocRabbitMQ())
                    {
                        LogInfo.WriteLog(LogLevel.WARN, $"- MocRabbitMQ.SendLaneTransactionRabbitMQ : connect failed, dropped key={keyName}");
                        return false;
                    }
                }
                System.Diagnostics.Stopwatch swPublish = System.Diagnostics.Stopwatch.StartNew();
                var properties          = channel.CreateBasicProperties();
                properties.Persistent   = false;

                // Bind Queue to Exchange
                channel.QueueBind(TCT_Lib.szDC_RabbitMQ_TransQueueName, TCT_Lib.szDC_RabbitMQ_TransExchangeName, keyName);

                byte[] messagebuffer    = Encoding.Default.GetBytes(msg);
                channel.BasicPublish(exchange: TCT_Lib.szDC_RabbitMQ_TransExchangeName,
                                        routingKey: keyName,
                                        basicProperties: null,
                                        body: messagebuffer);
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ MocRabbitMQ.SendLaneTransactionRabbitMQ : published exchange={TCT_Lib.szDC_RabbitMQ_TransExchangeName}, queue={TCT_Lib.szDC_RabbitMQ_TransQueueName}, routingKey={keyName}, bytes={messagebuffer.Length}, elapsed={swPublish.ElapsedMilliseconds}ms");

                LogInfo.WriteLog("+ End Send LaneTransaction to RAbbitMQ");
                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine($"- Error SendLaneTransactionRabbitMQ DC : {err.Message}");
                LogInfo.WriteLog($"- Error SendLaneTransactionRabbitMQ DC : {err.Message}");
                LogInfo.WriteLog(LogLevel.DEBUG, $"- MocRabbitMQ.SendLaneTransactionRabbitMQ : key={keyName}, stack={err.StackTrace}");
                connection  = null;
                channel     = null;
                //-----------------------------------------------------------------------------------
                if (err.Message.StartsWith("Already closed", StringComparison.Ordinal))
                {
                    LogInfo.WriteLog("+ Fail RabbitMQ DC : This will try to re-connect RabbitMQ.");
                    LogInfo.WriteLog(LogLevel.WARN, "- MocRabbitMQ.SendLaneTransactionRabbitMQ : connection already closed, reconnect");
                    //CloseMocRabbitMQ();
                    ConnectMocRabbitMQ();
                }
                //------------------------------------------------------------------------------------

                return false;
            }
        }

        //-----------------------------------------------------------------------
        public void MessageReceiverOnRabbitMqRecvMessage(string szMessage, bool bImportNow, bool bAutoFile=true)
        {
            try
            {
                dynamic msg         = JsonConvert.DeserializeObject(szMessage);
                string paramName    = msg["parameterName"];
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ MocRabbitMQ.MessageReceiverOnRabbitMqRecvMessage : parameterName={paramName ?? "null"}, len={szMessage?.Length ?? 0}, importNow={bImportNow}, autoFile={bAutoFile}");
                if (bAutoFile)
                    LogInfo.WriteRecvMsgRabbitMQ(paramName, szMessage);     // Write Data (Staff.log)

                if (paramName.ToUpper().Trim() == "STAFF")
                {
                    //LogInfo.WriteLog("+ Found Staff Data : " + szMessage);
                    if (bImportNow)
                    {
                        LogInfo.WriteLog("+ Create Thread Import Staff Data");
                        JArray staffList    = msg["staff"];
                        string timeStamp    = msg["timestamp"];
                        int version         = msg["version"];
                        LogInfo.WriteLog(LogLevel.DEBUG, $"+ MocRabbitMQ.MessageReceiverOnRabbitMqRecvMessage : STAFF count={staffList?.Count ?? 0}, version={version}, timestamp={timeStamp}, importBusy={bWorkImportStaff}");

                        if (bWorkImportStaff)
                            Thread.Sleep(1 * 1000);
                        if (!bWorkImportStaff)
                            CreateThreadImportStaff(staffList, version, timeStamp);
                        else
                            LogInfo.WriteLog("-Not Create thread for import staff because found task is importing staff.");
                        //TCT_Lib.LaneDB.ImportStaffListToDB(staffList, version, timeStamp);
                    }
                    else
                        LogInfo.WriteLog(LogLevel.DEBUG, "+ MocRabbitMQ.MessageReceiverOnRabbitMqRecvMessage : STAFF saved to file only (importNow=false), timer will import");
                }
                else if (paramName.ToUpper().Trim() == "TARIFF")
                {
                    //----------------------------------------------------
                    //--- Check Tariff format file
                    //bool bValid = CheckTariffValidateFormat(szMessage);
                    //----------------------------------------------------
                    LogInfo.WriteLog("+ Found Tariff Data : " + szMessage);
                    JArray tariffList   = msg["tariff"];
                    string timeStamp    = msg["timestamp"];
                    int version         = msg["version"];
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ MocRabbitMQ.MessageReceiverOnRabbitMqRecvMessage : TARIFF count={tariffList?.Count ?? 0}, version={version}, timestamp={timeStamp} -> ImportTariffToDB");
                    TCT_Lib.LaneDB.ImportTariffToDB(tariffList, version, timeStamp);
                }
                else if (paramName.ToUpper().Trim() == "PLAZA_CONFIGURATION")
                {
                    LogInfo.WriteLog("+ Found Plaza Configuration : " + szMessage);
                    JArray plazaList    = msg["plaza"];
                    string timeStamp    = msg["timestamp"];
                    int version         = msg["version"];
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ MocRabbitMQ.MessageReceiverOnRabbitMqRecvMessage : PLAZA_CONFIGURATION count={plazaList?.Count ?? 0}, version={version}, timestamp={timeStamp} -> ImportPlazaToDB");
                    TCT_Lib.LaneDB.ImportPlazaToDB(plazaList, version, timeStamp);
                }
                else if (paramName.ToUpper().Trim() == "PROMOTION")
                {
                    LogInfo.WriteLog("+ Found Promotion Footer Receipt : " + szMessage);
                    JArray promotionList    = msg["promotion"];
                    string timeStamp        = msg["timestamp"];
                    int version             = msg["version"];
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ MocRabbitMQ.MessageReceiverOnRabbitMqRecvMessage : PROMOTION count={promotionList?.Count ?? 0}, version={version}, timestamp={timeStamp}");
                    if (promotionList.Count > 0)
                    {
                        TCT_Lib.LaneDB.ImportPromotionToDB(promotionList, version, timeStamp);
                        //TCT_Lib.LaneDB.LoadPromotionData();
                    }
                }
                else if (paramName.ToUpper().Trim() == "CARD")
                {
                    LogInfo.WriteLog("+ Found DMT Card Data : " + szMessage);
                    JArray dmtCardList  = msg["cards"];
                    string timeStamp    = msg["timestampf"];
                    int version         = msg["version"];
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ MocRabbitMQ.MessageReceiverOnRabbitMqRecvMessage : CARD count={dmtCardList?.Count ?? 0}, version={version}, timestamp={timeStamp}");
                    if (dmtCardList.Count > 0)
                    {
                        TCT_Lib.LaneDB.ImportDmtCardToDB(dmtCardList, version, timeStamp);
                    }
                }
                else if (paramName.ToUpper().Trim() == "TFI")
                {
                    LogInfo.WriteLog("+ Found TFI Data ");
                    //dynamic tfiData     = msg["tfi"];
                    JArray tfiList      = msg["tfi"];
                    dynamic tfiData     = tfiList[0];
                    string timeStamp    = msg["timestamp"];
                    int version         = msg["version"];
                    LogInfo.WriteLog(LogLevel.DEBUG, $"+ MocRabbitMQ.MessageReceiverOnRabbitMqRecvMessage : TFI count={tfiList?.Count ?? 0}, version={version}, timestamp={timeStamp} -> ImportTfiToDB");
                    TCT_Lib.LaneDB.ImportTfiToDB(tfiData, version, timeStamp);
                }
                else
                    LogInfo.WriteLog(LogLevel.WARN, "- MocRabbitMQ.MessageReceiverOnRabbitMqRecvMessage : unknown parameterName=" + paramName + ", ignored");

                //------------------------------------------
                //--- Process Message from RabbitMQ
            }
            catch (Exception err)
            {
/*
                bool bValid = CheckTariffValidateFormat(szMessage);
                if (bValid)
                {
                    LogInfo.WriteLog("+ CheckTariffValidateFormat Success : " + szMessage);

                }
                else
*/                               
                {
                    Console.WriteLine($"- Error MessageReceiverOnRabbitMqRecvMessage : {err.Message}");
                    LogInfo.WriteLog($"- Error MessageReceiverOnRabbitMqRecvMessage : {err.Message}");
                    LogInfo.WriteLog(LogLevel.DEBUG, "- MocRabbitMQ.MessageReceiverOnRabbitMqRecvMessage : stack=" + err.StackTrace);
                }
            }
        }

        private void CreateThreadImportStaff(JArray staffList, int staffVersion, string timeStamp)
        {
            bWorkImportStaff                = true;
            LogInfo.WriteLog("+ Create thread for import staff.");
            threadImportStaff               = new Thread(() => ThreadImportStaff(staffList, staffVersion, timeStamp));
            threadImportStaff.Priority      = ThreadPriority.Normal;
            threadImportStaff.IsBackground  = true;
            threadImportStaff.Start();
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ MocRabbitMQ.CreateThreadImportStaff : started, count={staffList?.Count ?? 0}, version={staffVersion}");
        }

        protected void ThreadImportStaff(JArray staffList, int staffVersion, string timeStamp)
        {
            try
            {
                LogInfo.WriteLog("+ Thread start import staff");
                TCT_Lib.LaneDB.ImportStaffListToDB(staffList, staffVersion, timeStamp);
                LogInfo.WriteLog("+ Thread finish import staff");
                bWorkImportStaff = false;
                // v2.0.0: removed threadImportStaff.Abort() - aborting the running thread at the
                // end of its own work could throw ThreadAbortException mid-DB-commit. The thread
                // returns normally here instead.
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Thread error import staff : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, "- MocRabbitMQ.ThreadImportStaff : stack=" + err.StackTrace);
                bWorkImportStaff = false;
                //Console.WriteLine("- Error ThreadImportStaff : " + err.Message);
                //LogInfo.WriteLog("- Error ThreadImportStaff : " + err.Message);
            }
            bWorkImportStaff = false;
        }

        protected bool CheckTariffValidateFormat(string szMessage)
        {
            try
            {
                if ((!szMessage.StartsWith("9", StringComparison.Ordinal)) && (!szMessage.StartsWith("8", StringComparison.Ordinal)))
                    return false;

                string[] szAryLines = szMessage.Split('\n');
                string[] szAyrData  = szAryLines[0].Split(' ');
                if (szAyrData.Length == 15)
                    return true;

                LogInfo.WriteLog("- Fail CheckTariffValidateFormat : " + szMessage);
                return false;
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Fail CheckTariffValidateFormat : " + err.Message);  
                LogInfo.WriteLog(LogLevel.DEBUG, "- MocRabbitMQ.CheckTariffValidateFormat : stack=" + err.StackTrace);
                return false;
            }
        }

        protected void InitialTimerImportStaffThread()
        {
            threadTimerImportStaff                  = new Thread(new ThreadStart(ThreadTimerImportStaffAsync));
            threadTimerImportStaff.Priority         = ThreadPriority.AboveNormal;
            threadTimerImportStaff.IsBackground     = true;
            threadTimerImportStaff.Start();
        }

        protected async void ThreadTimerImportStaffAsync()
        {
            // v2.0.0: guard the interval so a misconfigured 0 can't turn this into a tight
            // spin loop (Task.Delay(0)). Minimum 1 minute.
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ MocRabbitMQ.ThreadTimerImportStaffAsync : start, interval={Math.Max(1, TCT_Lib.iImportStaffIntervalMin)}min");
            while (true)
            {
                int iDelayMs = Math.Max(1, TCT_Lib.iImportStaffIntervalMin) * 60 * 1000;
                try
                {
                    LogInfo.WriteLog("+ Timer Check Import Staff");
                    var laneRabbitMQPath    = Path.Combine(TCT_Lib.szLogPath, "RabbitMQ");
                    var staffFileName       = Path.Combine(laneRabbitMQPath, "STAFF.log");
                    if (File.Exists(staffFileName))
                    {
                        LogInfo.WriteLog("+ Start Timer Import Staff");
                        string dataJson = File.ReadAllText(staffFileName);
                        MessageReceiverOnRabbitMqRecvMessage(dataJson, true);
                        File.Delete(staffFileName.Replace(".log", ".bak"));
                        File.Move(staffFileName, staffFileName.Replace(".log", ".bak"));
                        LogInfo.WriteLog("+ Success ThreadTimerImportStaffAsync");
                    }
                    LogInfo.WriteLog("+ Timer Start Wait Import Staff");
                    await Task.Delay(iDelayMs);
                }
                catch (Exception err)
                {
                    LogInfo.WriteLog("- Fail ThreadTimerImportStaffAsync : " + err.Message);
                    LogInfo.WriteLog(LogLevel.DEBUG, "- MocRabbitMQ.ThreadTimerImportStaffAsync : stack=" + err.StackTrace);
                    LogInfo.WriteLog("+ Timer Start Wait Import Staff");
                    await Task.Delay(iDelayMs);
                }
            }
            // (unreachable threadImportStaff.Abort() removed in v2.0.0)
        }
    }


    //==========================================================================================
    //--- RabbitMQ Receiver
    public class MocMessageReceiver : DefaultBasicConsumer
    {
        public delegate void RabbitMQReceiceMessage(string szMessage, bool bImportNow, bool bAutoFile);
        public event RabbitMQReceiceMessage rabbitMQRecvMessage;

        private readonly IModel _channel;
        public MocMessageReceiver(IModel channel)
        {
            _channel = channel;
        }

        public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, ReadOnlyMemory<byte> body)
        {
            Console.WriteLine("========== Receive Message from DC RabbitMQ =============");
            Console.WriteLine(string.Concat("Exchange tag: ", exchange));
            Console.WriteLine(string.Concat("Consumer tag: ", consumerTag));
            Console.WriteLine(string.Concat("Delivery tag: ", deliveryTag));
            //Console.WriteLine(string.Concat("Message: ", Encoding.UTF8.GetString(body.ToArray())));

            LogInfo.WriteLog("========== Receive Message from DC RabbitMQ =============");
            LogInfo.WriteLog(string.Concat("Exchange tag: ", exchange));
            LogInfo.WriteLog(string.Concat("Consumer tag: ", consumerTag));
            LogInfo.WriteLog(string.Concat("Delivery tag: ", deliveryTag));
            //LogInfo.WriteLog(string.Concat("Message: ", Encoding.UTF8.GetString(body.ToArray())));
            LogInfo.WriteLog("------------------------------------------------------");
            LogInfo.WriteLog(LogLevel.DEBUG, $"+ MocMessageReceiver.HandleBasicDeliver : routingKey={routingKey}, redelivered={redelivered}, bytes={body.Length}");

            // v2.0.0: process inside try-catch so an exception can't kill the consumer thread;
            // ACK in finally (was: ACK before processing). Delegate is null-checked.
            try
            {
                string szBody = Encoding.UTF8.GetString(body.ToArray());
                rabbitMQRecvMessage?.Invoke(szBody, false, true);
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Error processing MOC message (dropped) : " + err.Message);
                LogInfo.WriteLog(LogLevel.DEBUG, "- MocMessageReceiver.HandleBasicDeliver : stack=" + err.StackTrace);
            }
            finally
            {
                LogInfo.WriteLog(LogLevel.DEBUG, $"+ MocMessageReceiver.HandleBasicDeliver : ack deliveryTag={deliveryTag}");
                try { _channel.BasicAck(deliveryTag, false); }
                catch (Exception e2) { LogInfo.WriteLog("- Error BasicAck MOC : " + e2.Message); }
            }

            base.HandleBasicDeliver(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body);
        }

    }
}
