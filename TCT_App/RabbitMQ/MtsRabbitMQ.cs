using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace TCT_App.RabbitMQ
{
    public class MtsRabbitMQ
    {
        private const string UserName           = "dmt_admin";
        private const string Password           = "dmt_admin";
        private const string HostName           = "192.168.244.252";
        private const int Port                  = 15672;
        private const string VirtualHost        = "cbe";


        ConnectionFactory connectionFactory = new ConnectionFactory
        {
            HostName    = TCT_Lib.szMTS_RabbitMQ_ServerHostName,
            VirtualHost = TCT_Lib.szMTS_RabbitMQ_VirtualHost,
            UserName    = TCT_Lib.szMTS_RabbitMQ_User,
            Password    = TCT_Lib.szMTS_RabbitMQ_Password,
            Port        = TCT_Lib.iMTS_RabbitMQ_Port,
            RequestedConnectionTimeout = TimeSpan.FromSeconds(10),
        };

        public IConnection connection   = null;
        private IModel channel          = null;
        //---------------------------------------------------------------
        public MtsRabbitMQ()
        {

        }

        public bool ConnectMtsRabbitMQ()
        {
            if (!TCT_Lib.bEnableMTS)
                return false;

/*
            if (!TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szMTS_RabbitMQ_ServerIPAddress, TCT_Lib.iMTS_RabbitMQ_ServerPort))
            {
                Console.WriteLine($"- Fail TestNetworkConnection to RabbitMQ => Connect MTS Rabbit MQ : IP = {TCT_Lib.szMTS_RabbitMQ_ServerIPAddress}, PORT = {TCT_Lib.iMTS_RabbitMQ_ServerPort}");
                LogInfo.WriteLog($"- Fail TestNetworkConnection to RabbitMQ => Connect MTS Rabbit MQ : IP = {TCT_Lib.szMTS_RabbitMQ_ServerIPAddress}, PORT = {TCT_Lib.iMTS_RabbitMQ_ServerPort}");
                return false;
            }
*/            

            LogInfo.WriteLogMTS($"MTS HostName : {TCT_Lib.szMTS_RabbitMQ_ServerHostName}");
            LogInfo.WriteLogMTS($"MTS VirtualHost : {TCT_Lib.szMTS_RabbitMQ_VirtualHost}");
            LogInfo.WriteLogMTS($"MTS Port : {TCT_Lib.iMTS_RabbitMQ_Port}");
            LogInfo.WriteLogMTS($"MTS User : {TCT_Lib.szMTS_RabbitMQ_User}");
            LogInfo.WriteLogMTS($"MTS Password : {TCT_Lib.szMTS_RabbitMQ_Password}");

            try
            {
                connection  = connectionFactory.CreateConnection();
                channel     = connection.CreateModel();
                LogInfo.WriteLogMTS("+ Success Connect MTS RabbitMQ");

                try
                {
                    // accept only one unack-ed message at a time
                    // uint prefetchSize, ushort prefetchCount, bool global
                    channel.BasicQos(0, 1, false);
                }
                catch (Exception err)
                {
                    LogInfo.WriteLogMTS("- Error MessageReceiver MTS RabbitMQ : " + err.Message);
                    Console.WriteLine(err.Message);
                }

                return true;
            }
            catch (BrokerUnreachableException err)
            {
                Console.WriteLine($"Fail Connect MTS RabbitMQ : {err.Message}");
                LogInfo.WriteLogMTS($"Fail Connect MTS RabbitMQ : {err.Message}");
                connection  = null;
                channel     = null;
                TCT_Lib.bMTS_NetworkStatus = false;

                return false;
            }
            catch (Exception err)
            {
                Console.WriteLine($"Fail Connect MTS RabbitMQ : {err.Message}");
                LogInfo.WriteLogMTS($"Fail Connect MTS RabbitMQ : {err.Message}");
                connection  = null;
                channel     = null;
                TCT_Lib.bMTS_NetworkStatus = false;

                return false;
            }
        }

        public void CloseMtsRabbitMQ()
        {
            channel.Close();
            connection.Close();
        }

        public bool SendMtsMsgToRabbitMQ(string keyName, string msg)
        {
            //if (TCT_Lib.bMaintenanceMode)
            //    return false;

            if (!TCT_Lib.bEnableMTS)
                return false;

/*
            if (!TCT_Lib.laneController.TestNetworkConnection(TCT_Lib.szMTS_RabbitMQ_ServerIPAddress, TCT_Lib.iMTS_RabbitMQ_ServerPort))
            {
                Console.WriteLine($"- Fail TestNetworkConnection to RabbitMQ => SendLaneTransactionRabbitMQ : IP = {TCT_Lib.szMTS_RabbitMQ_ServerIPAddress}, PORT = {TCT_Lib.iMTS_RabbitMQ_ServerPort}");
                LogInfo.WriteLog($"- Fail TestNetworkConnection to RabbitMQ => SendLaneTransactionRabbitMQ : IP = {TCT_Lib.szMTS_RabbitMQ_ServerIPAddress}, PORT = {TCT_Lib.iMTS_RabbitMQ_ServerPort}");
                return false;
            }
*/            

            keyName = TCT_Lib.szMTS_RabbitMQ_MtsRoutingKey;
            try
            {
                if (connection == null)
                {
                    if (!ConnectMtsRabbitMQ())
                        return false;
                }
                var properties          = channel.CreateBasicProperties();
                properties.Persistent   = false;

                byte[] messagebuffer    = Encoding.Default.GetBytes(msg);
                channel.BasicPublish(exchange: TCT_Lib.szMTS_RabbitMQ_MtsExchangeName,
                                        routingKey: keyName,
                                        basicProperties: null,
                                        body: messagebuffer);

                LogInfo.WriteLogMTS($"+ Success Send MTS To RabbitMQ.");
                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine($"- Error SendMtsMsgToRabbitMQ MTS : {err.Message}");
                LogInfo.WriteLogMTS($"- Error SendMtsMsgToRabbitMQ MTS : {err.Message}");
                connection  = null;
                channel     = null;
                //-----------------------------------------------------------------------------------
                if (err.Message.StartsWith("Already closed", StringComparison.Ordinal))
                {
                    LogInfo.WriteLogMTS("+ Fail RabbitMQ MTS : This will try to re-connect RabbitMQ.");
                    CloseMtsRabbitMQ();
                    ConnectMtsRabbitMQ();
                }
                //------------------------------------------------------------------------------------

                return false;
            }
        }
    }

    //=========================================================================
    public class MtsDataJson
    {
        public string jsonrpc { get; set; }
        public string method { get; set; }
        public Params @params { get; set; }
    }

    public class ObjMtsData
    {
        public string requestId;
        public string requestDateTime;
        public Plaza plaza;
        public Lane lane;
        public ResourceInfo resourceInfo;
        public DiskInfo diskInfo;
        public List<UsageDiskPath> usageDiskPaths;
        public AppStatus appStatus;
        public DeviceStatus deviceStatus;
        public CameraStatus cameraStatus;
        public List<FileVersion> fileVersions;
        public int lastAvcStatus;
        public int outBoxFile;
        //--- Url 1
        public int? statusListRecord;
        public long? obuValidFrom;
        public int? blackListRecord;
        public long? blackListValidFrom;
        public string tpsState;
        //--- Url 2
        public int xmr1StatusListRecord;
        public long? xmr1ObuValidFrom;
        public int xmr1BlackListRecord;
        public long? xmr1BlackListValidFrom;
        //--- Url 3
        public int xmr2StatusListRecord;
        public long? xmr2ObuValidFrom;
        public int xmr2BlackListRecord;
        public long? xmr2BlackListValidFrom;
        //--- Url 4
        public string laneMode;
        public string xmr1State;
        public string xmr2State;
        public string presenceDetState;
        public string passageDetState;
        public string bangkokDisplayState;
        public string grabberState;
        public string bangkokLcState;
    }

    public class Plaza
    {
        public string plazaId;
        public string plazaName;
    }

    public class Lane
    {
        public string laneId;
        public string laneName;
    }

    public class LaneMts
    {
        public string laneId;
        public string laneName;
        public int laneType;
        public string laneTypeName;
        public int laneMode;
        public string laneModeName;
        public int laneDirection;
        public string laneDirectionName;
    }

    public class ResourceInfo
    {
        public float cpuPercentAvg;
        public long memoryTotalMb;
        public long memoryUsedMb;
        public long memoryPercentUsed;
        public long upTimeSec;
    }

    public class DiskInfo
    {
        public string diskPath;
        public long spaceKb;
        public long usageKb;
        public long freeKb;
        public float percentUsed;
    }

    public class UsageDiskPath
    {
        public string pathName;
        public long spaceKb;
        public long usageKb;
        public long freeKb;
        public float percentUsed;
    }
/*
    public class UsageDiskPath
    {
        public string pathName;
        public float usageDiskMb;
    }
*/    

    public class AppStatus
    {
        public bool tct;
        public bool rto;
        public bool supAdjust;
    }

    public class DeviceStatus
    {
        public bool tfi;
        public bool crs;
        public bool scr;
        public bool printer;
        public bool keyboard;
        public bool? edc;
        public bool? lsd;
        public bool lcio;
        public bool lcsd;
    }

    public class CameraStatus
    {
        public bool camera1;
        public bool camera2;
        public bool camera3;
    }

    public class FileVersion
    {
        public string fileName;
        public string curVersion;
        public string nextVersion;
    }

    //---------------------------------------------------------------------------------------
    public class ObjMtsTrans
    {
        public string requestId;
        public string requestDateTime;
        public Plaza plaza;
        public Lane lane;
        public TransInfo transInfo;
        public AvcInfo avcInfo;
    }

    public class TransInfo
    {
        public int jobNo;
        public int nTrx;
        public string transDateTime;
    }

    public class AvcInfo
    {
        public int avcClass;
        public int avcAxle;
        public int avcWheel;
        public int avcDualWheel;
        public int avcDirection;
        public int avcError;
    }

    //-----------------------------------------------------------------------------------------
    public class MtsAlarmJson
    {
        public string jsonrpc { get; set; }
        public string method { get; set; }
        public ObjMtsAlarm @params { get; set; }
    }

    public class ObjMtsAlarm
    {
        public string requestId;
        public string requestDateTime;
        public int jobNo;
        public int nTrx;
        public Plaza plaza;
        public LaneMts lane;
        public Staff staff;
        public Supervisor supervisor;
        public AlarmData alarm;
    }

    public class AlarmData
    {
        public int alarmDevice;
        public string alarmName;
        public string alarmDetail;
        public string alarmRemark;
        public int alarmEventType;
        public string alarmDateTime;
    }
}
