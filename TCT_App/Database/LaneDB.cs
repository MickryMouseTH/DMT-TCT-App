using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Globalization;
using Mono.Data.Sqlite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TCT_App.WebServiceAPI;
using TCT_App.EMV.V0;
using System.Threading.Tasks;
using System.Drawing;

namespace TCT_App
{
    public class LaneDBLib
    {
        SqliteConnection conn;
        // v2.0.0: serialize all access to the single shared SQLite connection.
        // Mono.Data.Sqlite + one connection is NOT safe across threads; this lock
        // (reentrant Monitor) prevents concurrent-access crashes and "database is locked".
        private static readonly object _dbLock = new object();
        static long iEventID        = 0, iEventRtoID = 0, iEventHqID = 0;
        static long iAlarmRtoID     = 0;
        static long iSalesCouponTransID = 0;
        static long iFareTransID    = 0;
        DateTime dtLastTrans        = DateTime.Now;
        //--------------------------------------------------------------------
        public delegate void LaneControlUpdateUI_Convoy(int iClass);
        public event LaneControlUpdateUI_Convoy updateUI_Convoy;

        //-----------------------------------------------------------------
        public LaneDBLib()
        {
            try
            {
                string connStr      = $"URI=file:{Path.Combine(TCT_Lib.baseConfigFolder, "TCT_DB.db")}";
                LogInfo.WriteLog("+ Open SQLiteDB : " + connStr);
                conn                = new SqliteConnection(connStr);
                conn.Open();
                // v2.0.0: enable WAL + busy timeout to avoid "database is locked" hangs
                // under concurrent reads/writes from multiple threads.
                try
                {
                    foreach (var pragma in new[] { "PRAGMA journal_mode=WAL;", "PRAGMA busy_timeout=5000;", "PRAGMA synchronous=NORMAL;" })
                    {
                        using (var cmdPragma = new SqliteCommand(pragma, conn))
                            cmdPragma.ExecuteNonQuery();
                    }
                }
                catch (Exception errPragma)
                {
                    LogInfo.WriteLog("- Warning set SQLite PRAGMA : " + errPragma.Message);
                }
                LogInfo.WriteLog("+ Success Connect Database");

                iEventID            = GetEventID();
                iEventRtoID         = GetEventRtoID();
                iEventHqID          = GetEventHqID();
                iAlarmRtoID         = GetAlarmRtoID();
                iFareTransID        = GetLaneTransactionID();
                iSalesCouponTransID = GetSalesCouponTransactionID();

                LoadConfigDB();
                //-----------------------------------------------------------------
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error Connect Database : " + err.Message);
                LogInfo.WriteLog("- Error Connect Database : " + err.Message);
                conn = null;
            }
        }

        public long GetCurTransID()
        {
            return iFareTransID;
        }

        public void CloseDB()
        {
            if (conn != null)
            {
                conn.Close();
                conn = null;
            }
        }

        public DataTable List(string szQuery)
        {
            // v2.0.0: dispose command/adapter, serialize via _dbLock, and never let a
            // DB error crash the calling thread (return empty table instead).
            var dt = new DataTable();
            try
            {
                lock (_dbLock)
                {
                    if (conn == null)
                        return dt;
                    using (var cmd = new SqliteCommand(szQuery, conn))
                    using (var dataAdapter = new SqliteDataAdapter(cmd))
                    {
                        dataAdapter.SelectCommand.CommandTimeout = 10;
                        dataAdapter.Fill(dt);
                    }
                }
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Error DB List : " + err.Message + " | Query : " + szQuery);
            }
            return dt;
        }

        public int Execute(string szQuery)
        {
            // v2.0.0: dispose command, serialize via _dbLock, swallow errors (return -1).
            try
            {
                lock (_dbLock)
                {
                    if (conn == null)
                        return -1;
                    using (var cmd = new SqliteCommand(szQuery, conn))
                    {
                        cmd.CommandTimeout = 10;
                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Error DB Execute : " + err.Message + " | Query : " + szQuery);
                return -1;
            }
        }

        public bool ChkDbHasTable(string szTableName)
        {
            string szFmt    = "select COUNT(*) from sqlite_master " +
                                "WHERE type = 'table' AND name = '{0}'; ";
            string szQuery  = string.Format(szFmt, szTableName);
            DataTable dt    = List(szQuery);
            return int.Parse(dt.Rows[0][0].ToString()) > 0;
        }

        public bool ChkDbHasColumnInTable(string szTableName, string szColumnName)
        {
            string szFmt    = "SELECT EXISTS " +
                                "(SELECT * FROM sqlite_master WHERE tbl_name = '{0}' AND sql LIKE '%{1}%');";
            string szQuery  = string.Format(szFmt, szTableName, szColumnName);
            DataTable dt    = List(szQuery);
            return int.Parse(dt.Rows[0][0].ToString()) > 0;
        }

        //----------------------------------------------------------------------------------------------

        private long GetEventID()
        {
            try
            {
                string szQuery  = "SELECT MAX(EventID) FROM LaneEvent";
                DataTable dt    = List(szQuery);
                return dt.Rows[0][0] == DBNull.Value ? 1 : long.Parse(dt.Rows[0][0].ToString()) + 1;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error GetEventID : " + err.Message);
                LogInfo.WriteLog("- Error GetEventID : " + err.Message);
                return 0;
            }
        }

        //=========================================================================================
        public void LoadConfigDB()
        {
            var InvC = CultureInfo.InvariantCulture;
            try
            {
                //--- Load Lane Config
                string szQuery  = "SELECT * FROM LaneData "+
                                    $"WHERE LaneID={TCT_Lib.iLaneID}";
                DataTable dtLane    = List(szQuery);
                //TCT_Lib.szLaneCode  = TCT_Lib.szLaneName = dtLane.Rows[0]["LaneName"].ToString();

                //--- Load Plaza Config
                szQuery = "SELECT * FROM PlazaData " +
                            $"WHERE PlazaID='{TCT_Lib.szPlazaID}'";
                DataTable dtPlaza   = List(szQuery);
                //TCT_Lib.szPlazaCode = dtPlaza.Rows[0]["PlazaCode"] == DBNull.Value ? "- x -" : dtPlaza.Rows[0]["PlazaCode"].ToString();
                //TCT_Lib.szPlazaName = dtPlaza.Rows[0]["PlazaName"] == DBNull.Value ? "-- xxx ---" : dtPlaza.Rows[0]["PlazaName"].ToString();


                //--- Load Tariff Config
                int iPlazaID = int.Parse(TCT_Lib.szPlazaID);
                string szFmt = "SELECT * FROM Tariff " +
                                    "WHERE CAST(PlazaID AS INT)={0} " +
                                    "AND '{1}'>FromDateTime AND '{1}'< ToDateTime " +
                                    "ORDER BY TariffVersion DESC, FromDateTime DESC";
                szQuery = string.Format(szFmt, iPlazaID, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", InvC));
                DataTable dtTariff = List(szQuery);
                if (dtTariff.Rows.Count > 0)
                {
                    TCT_Lib.fFareClass1 = decimal.Parse(dtTariff.Rows[0]["FareClassType1"].ToString());
                    TCT_Lib.fFareClass2 = decimal.Parse(dtTariff.Rows[0]["FareClassType2"].ToString());
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error LoadConfigDB : " + err.Message);
                LogInfo.WriteLog("- Error LoadConfigDB : " + err.Message);
                TCT_Lib.szLaneCode = TCT_Lib.szLaneName = "xxx";
            }
        }

        public void LoadTariffData()
        {
            var InvC = CultureInfo.InvariantCulture;
            try
            {
                int iPlazaID    = int.Parse(TCT_Lib.szPlazaID);
                string szFmt = "SELECT * FROM Tariff " +
                                "WHERE CAST(PlazaID AS INT)={0} " +
                                "AND '{1}'>FromDateTime AND '{1}'< ToDateTime " +
                                "ORDER BY TariffVersion DESC, FromDateTime DESC";
                string szQuery = string.Format(szFmt, iPlazaID, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", InvC));
                DataTable dtTariff = List(szQuery);
                if (dtTariff.Rows.Count > 0)
                {
                    TCT_Lib.fFareClass1 = decimal.Parse(dtTariff.Rows[0]["FareClassType1"].ToString());
                    TCT_Lib.fFareClass2 = decimal.Parse(dtTariff.Rows[0]["FareClassType2"].ToString());

                    TCT_Lib.laneController.szAmount_Class1 = TCT_Lib.fFareClass1.ToString("0") + " บาท";
                    TCT_Lib.laneController.szAmount_Class2 = TCT_Lib.fFareClass2.ToString("0") + " บาท";
                }
            }
            catch(Exception err)
            {
                LogInfo.WriteLog("- Error LoadTariffData : " + err.Message);
                TCT_Lib.fFareClass1 = TCT_Lib.fFareClass2 = 0;
            }
        }

        public bool CheckUpdateTariffEffective()
        {
            var InvC = CultureInfo.InvariantCulture;
            try
            {
                int iPlazaID = int.Parse(TCT_Lib.szPlazaID);
                string szFmt = "SELECT * FROM Tariff_BAK " +
                                    "WHERE CAST(PlazaID AS INT)={0} " +
                                    "AND '{1}'>FromDateTime AND '{1}'< ToDateTime " +
                                    "ORDER BY TariffVersion DESC, FromDateTime DESC";
                string szQuery = string.Format(szFmt, iPlazaID, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", InvC));
                LogInfo.WriteLog("+ CheckUpdateTariffEffective : " + szQuery);
                DataTable dtTariff = List(szQuery);
                if (dtTariff.Rows.Count > 0)
                {
                    TCT_Lib.fFareClass1 = decimal.Parse(dtTariff.Rows[0]["FareClassType1"].ToString());
                    TCT_Lib.fFareClass2 = decimal.Parse(dtTariff.Rows[0]["FareClassType2"].ToString());

                    TCT_Lib.laneController.szAmount_Class1 = TCT_Lib.fFareClass1.ToString("0") + " บาท";
                    TCT_Lib.laneController.szAmount_Class2 = TCT_Lib.fFareClass2.ToString("0") + " บาท";
                    //--------------------------------------------
                    szQuery = "DELETE FROM Tariff " +
                                $"WHERE CAST(PlazaID AS INT)={iPlazaID}";
                    LogInfo.WriteLog("+ CheckUpdateTariffEffective : " + szQuery);
                    Execute(szQuery);

                    szQuery = "INSERT INTO Tariff " +
                                "SELECT * FROM Tariff_BAK " +
                                $"WHERE CAST(PlazaID AS INT)={iPlazaID}";
                    LogInfo.WriteLog("+ CheckUpdateTariffEffective : " + szQuery);
                    Execute(szQuery);
                }

                return true;
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Error CheckUpdateTariffEffective : " + err.Message);
                return false;
            }
        }

        public void WriteLaneEvent(string szMsg)
        {
            string szQuery = "";
            try
            {
                dynamic msg         = JsonConvert.DeserializeObject(szMsg);
                string result = "", method = "", szParams = "";
                if (msg["result"] is string)
                {
                    result              = msg["result"];
                    method              = msg["method"] ?? "";
                    JObject jParams     = msg["params"];
                    szParams            = jParams == null ? "" : jParams.ToString();
                }
                else
                {

                }

                LaneMode xLaneMode = TCT_Lib.eCurLaneMode;
                if (TCT_Lib.bMaintenanceMode && TCT_Lib.eCurLaneMode == LaneMode.HPMC)
                    xLaneMode = LaneMode.MAINTENANCE;

                //LogInfo.WriteLog("EventID : " + iEventID);
                if (method.Trim() != "")
                {
                    var InvC = CultureInfo.InvariantCulture;
                    szQuery = "INSERT INTO LaneEvent (EventID, EventDateTime, PlazaID, LaneID, LaneType, LaneMode, LaneDirection, " +
                                        "StaffShiftID, StaffID, StaffCode, SupervisorShiftID, SupervisorID, SupervisorCode, " +
                                        "EventMethod, EventParams) VALUES " +
                                        $"({iEventID}, '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}', '{TCT_Lib.szPlazaID}', {TCT_Lib.iLaneID}," +
                                        $"{(int)TCT_Lib.eCurLaneType}, {(int)xLaneMode}, {(int)TCT_Lib.eLaneDirection}, " +
                                        $"{TCT_Lib.iStaffShiftID}, '{TCT_Lib.szStaffID}', '{TCT_Lib.szStaffCode}',  " +
                                        $"{TCT_Lib.iSupervisorShiftID}, '{TCT_Lib.szSupervisorID}', '{TCT_Lib.szSupervisorCode}', " +
                                        $"'{method}', '{szParams}')";
                    //LogInfo.WriteLog("+ Query : " + szQuery);
                    Execute(szQuery);
                    System.Threading.Interlocked.Increment(ref iEventID);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error WriteLaneEvent : " + err.Message);
                LogInfo.WriteLog("- Query : " + szQuery);
                LogInfo.WriteLog("- Error WriteLaneEvent : " + err.Message);
            }
        }

        private long GetEventHqID()
        {
            if (TCT_Lib.bMaintenanceMode)
                return 0;

            try
            {
                string szQuery  = "SELECT MAX(EventHqID) FROM LaneEventHQ";
                DataTable dt    = List(szQuery);
                return dt.Rows[0][0] == DBNull.Value ? 1 : long.Parse(dt.Rows[0][0].ToString()) + 1;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error GetEventHqID : " + err.Message);
                LogInfo.WriteLog("- Error GetEventHqID : " + err.Message);
                return 0;
            }
        }

        private long GetEventRtoID()
        {
            try
            {
                string szQuery  = "SELECT MAX(EventRtoID) FROM LaneEventRTO";
                DataTable dt    = List(szQuery);
                return dt.Rows[0][0] == DBNull.Value ? 1 : long.Parse(dt.Rows[0][0].ToString()) + 1;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error GetEventRtoID : " + err.Message);
                LogInfo.WriteLog("- Error GetEventRtoID : " + err.Message);
                return 0;
            }
        }

        public long WriteLaneEventRTO(EventTypeRTO eventType, int iEventState)
        {
            try
            {
                long iID = iEventRtoID;
                

                return iID;
            }
            catch (Exception err)
            {
                Console.WriteLine($"- Error WriteLaneEventRTO : {err.Message}");
                LogInfo.WriteLog($"- Error WriteLaneEventRTO : {err.Message}");

                return 0;
            }
        }

        public long WriteLaneEventRTO_Device(int iDeviceTypeID, int iDeviceState)
        {
            if (iEventRtoID == 0)
                return 0;

            if ((iDeviceTypeID == (int)Cmd_DI_Type.PresenceOB) ||
                (iDeviceTypeID == (int)Cmd_DI_Type.PresenceLoop) ||
                (iDeviceTypeID == (int)Cmd_DI_Type.EthErr) ||
                (iDeviceTypeID == (int)Cmd_DI_Type.TreadleC) ||
                (iDeviceTypeID == (int)Cmd_DI_Type.TreadleD) ||
                (iDeviceTypeID == (int)Cmd_DI_Type.TreadleError) ||
                (iDeviceTypeID == (int)Cmd_DI_Type.PassageOB) ||
                (iDeviceTypeID == (int)Cmd_DI_Type.PassageLoop) ||
                (iDeviceTypeID == (int)Cmd_DI_Type.LTL)
                )
                return 0;
                
            string szQuery = "";
            try
            {
                LaneMode xLaneMode = TCT_Lib.eCurLaneMode;
                if (TCT_Lib.bMaintenanceMode && TCT_Lib.eCurLaneMode == LaneMode.HPMC)
                    xLaneMode = LaneMode.MAINTENANCE;
                //---------------------------------------------------------
                var InvC = CultureInfo.InvariantCulture;
                long iID = iEventRtoID;
                //LogInfo.WriteLog("EventRTO ID : " + iID);
                szQuery = "INSERT INTO LaneEventRTO(EventRtoID, EventDateTime, PlazaID, LaneID, LaneType, LaneMode, LaneDirection, " +
                                    "StaffShiftID, StaffID, StaffCode, StaffName, SupervisorShiftID, SupervisorID, SupervisorCode, SupervisorName, " +
                                    "EventType, EventTypeName, DeviceType, DeviceName, DeviceState) VALUES " +
                                    $"({iID}, '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}', '{TCT_Lib.szPlazaID}', '{TCT_Lib.iLaneID}', " +
                                    $"{(int)TCT_Lib.eCurLaneType}, {(int)xLaneMode}, {(int)TCT_Lib.eLaneDirection}, " +
                                    $"{TCT_Lib.iStaffShiftID}, '{TCT_Lib.szStaffID}', '{TCT_Lib.szStaffCode}', '{TCT_Lib.szStaffName}', " +
                                    $"{TCT_Lib.iSupervisorShiftID},  '{TCT_Lib.szSupervisorID}', '{TCT_Lib.szSupervisorCode}', '{TCT_Lib.szSupervisorName}', " +
                                    $"{(int)EventTypeRTO.DEVICE}, '{EventTypeRTO.DEVICE.ToString()}', {iDeviceTypeID}, '{GetDeviceTypeName(iDeviceTypeID)}', " +
                                    $"{iDeviceState})";

                Execute(szQuery);
                System.Threading.Interlocked.Increment(ref iEventRtoID);

                return iID;
            }
            catch (Exception err)
            {
                Console.WriteLine($"- Error WriteLaneEventRTO_Device : {err.Message}");
                LogInfo.WriteLog("- Query = " + szQuery);
                LogInfo.WriteLog($"- Error WriteLaneEventRTO_Device : {err.Message}");

                if (err.Message.StartsWith("Abort due to constraint violation", StringComparison.Ordinal))
                {
                    szQuery = "DELETE FROM LaneEventRTO " +
                                "WHERE EventRtoID>=" + iEventRtoID;
                    Execute(szQuery);
                }

                return 0;
            }
        }

        private string GetDeviceTypeName(int iDeviceTypeID)
        {
            switch (iDeviceTypeID)
            {
                case (int)Cmd_DI_Type.MLB:
                    return Cmd_DI_Type.MLB.ToString();
                case (int)Cmd_DI_Type.EFS:
                    return Cmd_DI_Type.EFS.ToString();
                case (int)Cmd_DI_Type.PresenceLoop:
                    return Cmd_DI_Type.PresenceLoop.ToString();
                case (int)Cmd_DI_Type.PassageLoop:
                    return Cmd_DI_Type.PassageLoop.ToString();
                case (int)Cmd_DI_Type.PresenceOB:
                    return Cmd_DI_Type.PresenceOB.ToString();
                case (int)Cmd_DI_Type.PassageOB:
                    return Cmd_DI_Type.PassageOB.ToString();
                case (int)Cmd_DI_Type.ALB:
                    return Cmd_DI_Type.ALB.ToString();
                case (int)Cmd_DI_Type.OTL:
                    return Cmd_DI_Type.OTL.ToString();
                case (int)Cmd_DI_Type.LTL:
                    return Cmd_DI_Type.LTL.ToString();
                case (int)Cmd_DI_Type.EthErr:
                    return Cmd_DI_Type.EthErr.ToString();
                case (int)Cmd_DI_Type.Siren:
                    return Cmd_DI_Type.Siren.ToString();
                case (int)Cmd_DI_Type.TreadleError:
                    return Cmd_DI_Type.TreadleError.ToString();
                case (int)Cmd_DI_Type.TreadleC:
                    return Cmd_DI_Type.TreadleC.ToString();
                case (int)Cmd_DI_Type.TreadleD:
                    return Cmd_DI_Type.TreadleD.ToString();
                default:
                    return "Unknown";
            }
        }

        public long WriteLaneEventHQ(EventTypeHQ eventType, int iEventState)
        {
            if (TCT_Lib.bMaintenanceMode)
                return 0;

            try
            {
                long iID        = iEventHqID;
                var InvC        = CultureInfo.InvariantCulture;
                string szQuery  = "INSERT INTO LaneEventHQ (EventHqID, EventDateTime, PlazaID, PlazaName, LaneID, LaneName, LaneType, LaneTypeName, " +
                                    "StaffShiftID, StaffID, StaffCode, StaffName, SupervisorShiftID, SupervisorID, SupervisorCode, SupervisorName, " +
                                    "EventType, EventTypeName, EventState, EventStateName, " +
                                    "EventRemark) VALUES " +
                                    $"({iID}, '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}', '{TCT_Lib.szPlazaID}', '{TCT_Lib.szPlazaName}', " +
                                    $"{TCT_Lib.iLaneID}, '{TCT_Lib.szLaneName}', {(int)TCT_Lib.eCurLaneType}, '{TCT_Lib.eCurLaneType.ToString()}', " +
                                    $"{TCT_Lib.iStaffShiftID}, '{TCT_Lib.szStaffID}', '{TCT_Lib.szStaffCode}', '{TCT_Lib.szStaffName}', " +
                                    $"{TCT_Lib.iSupervisorShiftID}, '{TCT_Lib.szSupervisorID}', '{TCT_Lib.szSupervisorCode}', '{TCT_Lib.szSupervisorName}', " +
                                    $"{(int)eventType}, '{eventType.ToString()}', {iEventState}, '{GetEventStateName(eventType, iEventState)}', '')";
                Execute(szQuery);
                System.Threading.Interlocked.Increment(ref iEventHqID);

                //------------------------------------------------------------------------------------------------------
                var xLaneEvent      = TCT_Obj.GenerateLaneEventHQ(iID);
                string szLaneEvent  = JsonConvert.SerializeObject(xLaneEvent, Formatting.Indented);
                //Console.WriteLine("+ Rabbit Lane Event = " + szLaneEvent);
                //LogInfo.WriteLog("+ Rabbit Lane Event = " + szLaneEvent);
                bool bResult = TCT_Lib.mocRabbitMQ.SendLaneEventRabbitMQ($"Event-{eventType.ToString()}", szLaneEvent);
                UpdateSendLaneEventHQ(iID, bResult ? 1 : 0);
                //---------------------------------------------------------------------------------------------------------
                return iID;
            }
            catch (Exception err)
            {
                Console.WriteLine($"- Error WriteLaneEventHQ : {err.Message}");
                LogInfo.WriteLog($"- Error WriteLaneEventHQ : {err.Message}");

                return 0;
            }
        }

        private string GetEventStateName(EventTypeHQ eventType, int iEventState)
        {
            if (eventType == EventTypeHQ.JOB)
                return iEventState == 1 ? "BOJ" : "EOJ";
            else if (eventType == EventTypeHQ.OTL)
                return iEventState == 1 ? "GREEN" : "RED";
            else if (eventType == EventTypeHQ.HPMC)
                return iEventState == 1 ? "START" : "END";
            else
                return iEventState == 1 ? "ON" : "OFF";
        }

        public void UpdateSendLaneEventHQ(long iEventID, int iStatus)
        {
            var InvC = CultureInfo.InvariantCulture;
            string szQuery = $"UPDATE LaneEventHQ SET SendHqStatus={iStatus}, " +
                             $"SendHqDateTime='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}' " +
                             $"WHERE EventHqID={iEventID}";
            Execute(szQuery);
        }

        //===========================================================================
        private long GetAlarmRtoID()
        {
            string szQuery = "";
            try
            {
                szQuery         = "SELECT MAX(AlarmRtoID) FROM LaneAlarmRTO";
                DataTable dt    = List(szQuery);
                return dt.Rows[0][0] == DBNull.Value ? 1 : long.Parse(dt.Rows[0][0].ToString()) + 1;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error GetAlarmRtoID : " + err.Message);
                LogInfo.WriteLog("- Query : " + szQuery);
                LogInfo.WriteLog("- Error GetAlarmRtoID : " + err.Message);
                return 0;
            }
        }

        public long WriteLaneAlarmRTO(AlarmDevice alarmDevice, AlarmType alarmType, string szAlarmDetail, string szAlarmRemark)
        {
            string szQuery = "";
            try
            {
                long iID    = iAlarmRtoID;
                //LogInfo.WriteLog("AlarmID : " + iAlarmRtoID);
                var InvC    = CultureInfo.InvariantCulture;
                LaneMode xLaneMode = TCT_Lib.eCurLaneMode;
                if (TCT_Lib.bMaintenanceMode && TCT_Lib.eCurLaneMode == LaneMode.HPMC)
                    xLaneMode = LaneMode.MAINTENANCE;
                szQuery     = "INSERT INTO LaneAlarmRTO (AlarmRtoID, AlarmDateTime, PlazaID, LaneID, LaneType, LaneMode, LaneDirection, " +
                                    "StaffShiftID, StaffID, StaffCode, StaffName, SupervisorShiftID, SupervisorID, SupervisorCode, SupervisorName, " +
                                    "AlarmDevice, AlarmDeviceName, AlarmType, AlarmTypeName, AlarmDetail, AlarmRemark) VALUES " +
                                    $"({iID}, '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}', '{TCT_Lib.szPlazaID}', {TCT_Lib.iLaneID}, " +
                                    $"{(int)TCT_Lib.eCurLaneType}, {(int)xLaneMode}, {(int)TCT_Lib.eLaneDirection}, " +
                                    $"{TCT_Lib.iStaffShiftID}, '{TCT_Lib.szStaffID}', '{TCT_Lib.szStaffCode}', '{TCT_Lib.szStaffName}', " +
                                    $"{TCT_Lib.iSupervisorShiftID}, '{TCT_Lib.szSupervisorID}', '{TCT_Lib.szSupervisorCode}', '{TCT_Lib.szSupervisorName}', " +
                                    $"{(int)alarmDevice}, '{alarmDevice.ToString()}', {(int)alarmType}, '{alarmType.ToString()}', " +
                                    $"'{szAlarmDetail.Replace("'", "''")}', '{szAlarmRemark.Replace("'", "''")}')";
                Execute(szQuery);
                System.Threading.Interlocked.Increment(ref iAlarmRtoID);

                //------------------------------------------------------------------------------------------------------
                //var xLaneAlarm      = TCT_Obj.GenerateLaneAlarmRTO(iID);
                //string szLaneAlarm  = JsonConvert.SerializeObject(xLaneAlarm);
                //Console.WriteLine("+ Rabbit Lane Alarm = " + szLaneAlarm);
                //LogInfo.WriteLog("+ Rabbit Lane Alarm = " + szLaneAlarm);
                //TCT_Lib.mocRabbitMQ.SendLaneEventRabbitMQ($"Alarm-{alarmDevice.ToString()}", szLaneAlarm);
                //---------------------------------------------------------------------------------------------------------
                return iID;
            }
            catch (Exception err)
            {
                Console.WriteLine($"- Error WriteLaneAlarmRTO : {err.Message}");
                LogInfo.WriteLog("- Query : " + szQuery);
                LogInfo.WriteLog($"- Error WriteLaneAlarmRTO : {err.Message}");

                return 0;
            }
        }

        //===============================================================================
        public long GetStaffShiftID()
        {
            try
            {
                string szQuery  = "SELECT MAX(StaffShiftID) FROM StaffShift";
                DataTable dt    = List(szQuery);
                return dt.Rows[0][0] == DBNull.Value ? 1 : long.Parse(dt.Rows[0][0].ToString()) + 1;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error GetStaffShiftID : " + err.Message);
                LogInfo.WriteLog("- Error GetStaffShiftID : " + err.Message);
                return 0;
            }
        }

        public void WriteLaneStartStaffShift(LoginType loginType)
        {
            LogInfo.WriteLog("+ Start WriteLaneStartStaffShift");
            var InvC = CultureInfo.InvariantCulture;

            //----------------------------------------------
            //--- Auto End Shifft of all shift
            DateTime dtEndShift = DateTime.Now;
            try
            {
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    string szQuery = $"UPDATE StaffShift SET StaffEndShiftMethod=99, " +
                                    //$"StaffEndShiftDateTime='{dtEndShift.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}' " +
                                    $"StaffEndShiftDateTime='{dtEndShift.ToString("yyyy-MM-dd HH:mm:ss", InvC)}' " +
                                    $"WHERE StaffEndShiftDateTime is null";
                    LogInfo.WriteLog("+ Query WriteLaneEndStaffShift : " + szQuery);
                    Execute(szQuery);
                }
                LogInfo.WriteLog("+ Success WriteLaneEndStaffShift");
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error Auto EndStaffShift : " + err.Message);
                LogInfo.WriteLog("- Error Auto EndStaffShif : " + err.Message);
            }
            //----------------------------------------------
            try
            {
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    string szQuery = "INSERT INTO StaffShift (StaffShiftID, PlazaID, LaneID, LaneType, " +
                                    "StaffStartShiftDateTime, StaffLoginMethod, StaffID, StaffCode, StaffName, JobNo," +
                                    "SupervisorShiftID, SupervisorID, SupervisorCode, SupervisorName, SupervisorStartShiftDateTime) VALUES " +
                                    $"({TCT_Lib.iStaffShiftID}, '{TCT_Lib.szPlazaID}', {TCT_Lib.iLaneID}, {(int)TCT_Lib.eCurLaneType}, " +
                                    $"'{TCT_Lib.dtStaffStartShift.ToString("yyyy-MM-dd HH:mm:ss", InvC)}', {(int)loginType}, " +
                                    $"'{TCT_Lib.szStaffID}', '{TCT_Lib.szStaffCode}', '{TCT_Lib.szStaffName}', {TCT_Lib.iJobNo}, " +
                                    $"{TCT_Lib.iSupervisorShiftID}, '{TCT_Lib.szSupervisorID}', '{TCT_Lib.szSupervisorCode}', '{TCT_Lib.szSupervisorName}', "+
                                    //$"'{TCT_Lib.dtSupervisorStartShift.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}')";
                                    $"'{TCT_Lib.dtSupervisorStartShift.ToString("yyyy-MM-dd HH:mm:ss", InvC)}')";
                    Execute(szQuery);
                    LogInfo.WriteLog("+ Success WriteLaneStartStaffShift");
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error WriteLaneStartStaffShift : " + err.Message);
                LogInfo.WriteLog("- Error WriteLaneStartStaffShift : " + err.Message);
            }
        }

        public DateTime WriteLaneEndStaffShift(int iStaffShiftID, int iEndShiftMethod)
        {
            LogInfo.WriteLog("+ Start WriteLaneEndStaffShift");
            var InvC = CultureInfo.InvariantCulture;
            DateTime dtEndShift = DateTime.Now;
            if (iEndShiftMethod == 1)
            {
                //--- Change Auto last EndShift to Start TCT when 2024-05-07
                dtEndShift = TCT_Lib.dtTctStartDateTime;
                LogInfo.WriteLog("+ Get Date-Time of Auto End Shift = Lastest TCT Start Time = " + TCT_Lib.dtTctStartDateTime.ToString("yyyy-MM-dd HH:mm:ss", InvC));
/*
                //--- Auto End Shift, Get End shift from last transaction of staff shift
                string szFmt2 = "SELECT * FROM LaneTransaction "+
                                "WHERE StaffShiftID={0} " +
                                "ORDER BY TransactionDateTime DESC";
                string szQuery2 = string.Format(szFmt2, iStaffShiftID);
                LogInfo.WriteLog("+ Quuery GetDateTime AutoEndShift : " + szQuery2);
                DataTable dt2    = List(szQuery2);
                if (dt2.Rows.Count == 0)
                {
                    LogInfo.WriteLog("- Not found transaction of auto end shift, then get from start shift");
                    //--- Not found transaction, then Get from start of staff shift
                    szFmt2 = "SELECT * FROM StaffShift " +
                              "WHERE StaffShiftID={0}";
                    szQuery2        = string.Format(szFmt2, iStaffShiftID);
                    LogInfo.WriteLog("+ Quuery GetDateTime AutoEndShift from StartShift : " + szQuery2);
                    DataTable dt3   = List(szQuery2);
                    dtEndShift      = Convert.ToDateTime(dt3.Rows[0]["StaffStartShiftDateTime"].ToString()).AddSeconds(5);
                }
                else
                {
                    dtEndShift = Convert.ToDateTime(dt2.Rows[0]["TransactionDateTime"].ToString()).AddSeconds(5);
                }
*/
            }
            LogInfo.WriteLog("+ End Shift DateTime : "+ dtEndShift.ToString("yyyy-MM-dd HH:mm:ss", InvC));
            //-------------------------------------------------------

            try
            {
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    string szQuery = $"UPDATE StaffShift SET StaffEndShiftMethod={iEndShiftMethod}, " +
                                    //$"StaffEndShiftDateTime='{dtEndShift.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}' " +
                                    $"StaffEndShiftDateTime='{dtEndShift.ToString("yyyy-MM-dd HH:mm:ss", InvC)}' " +
                                    $"WHERE StaffShiftID={iStaffShiftID}";
                    LogInfo.WriteLog("+ Query WriteLaneEndStaffShift : " + szQuery);
                    Execute(szQuery);
                }
                LogInfo.WriteLog("+ Success WriteLaneEndStaffShift");
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error WriteLaneEndStaffShift : " + err.Message);
                LogInfo.WriteLog("- Error WriteLaneEndStaffShift : " + err.Message);
            }

            return dtEndShift;
        }

        public bool CheckLastShiftCompleteEndShift()
        {
            try
            {
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    string szQuery = "SELECT * FROM StaffShift " +
                                    "ORDER BY StaffShiftID DESC";
                    DataTable dt = List(szQuery);
                    if (dt.Rows.Count == 0)
                        return true;

                    return dt.Rows[0]["StaffEndShiftDateTime"] != DBNull.Value;

                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error CheckLastShiftCompleteEndShift : " + err.Message);
                LogInfo.WriteLog("- Error CheckLastShiftCompleteEndShift : " + err.Message);

                return false;
            }
        }

        public bool GetCurrentSupervisor()
        {
            try
            {
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    string szQuery = "SELECT * FROM SupervisorShift " +
                                        $"ORDER BY SupervisorShiftID DESC";
                    DataTable dt = List(szQuery);
                    if (dt.Rows.Count == 0)
                    {
                        TCT_Lib.iSupervisorShiftID      = 0;
                        TCT_Lib.szSupervisorID          = "";
                        TCT_Lib.szSupervisorCode        = "";
                        TCT_Lib.szSupervisorName        = "";
                        TCT_Lib.szSupervisorGroupID     = "";
                        TCT_Lib.szSupervisorGroupName   = "";
                        TCT_Lib.dtSupervisorStartShift  = DateTime.Now;
                    }
                    else
                    {
                        TCT_Lib.iSupervisorShiftID      = int.Parse(dt.Rows[0]["SupervisorShiftID"].ToString());
                        TCT_Lib.szSupervisorID          = dt.Rows[0]["SupervisorID"].ToString();
                        TCT_Lib.szSupervisorCode        = dt.Rows[0]["SupervisorCode"].ToString();
                        TCT_Lib.szSupervisorName        = dt.Rows[0]["SupervisorName"].ToString();
                        TCT_Lib.szSupervisorGroupID     = dt.Rows[0]["SupervisorGroupID"].ToString();
                        TCT_Lib.szSupervisorGroupName   = dt.Rows[0]["SupervisorGroupName"].ToString();
                        TCT_Lib.dtSupervisorStartShift  = Convert.ToDateTime(dt.Rows[0]["StartShiftDateTime"].ToString());
                    }

                    return true;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error UpdateCurrentSupervisor : " + err.Message);
                LogInfo.WriteLog("- Error UpdateCurrentSupervisor : " + err.Message);

                return false;
            }
        }

        public bool UpdateCurrentSupervisor(int iSupervisorShiftID, string szSupervisorId, string szSupervisorCode, string szSupervisorName, 
                                            string szSupervisorGroupId, string szSupervisorGroupName, DateTime dtSupervisorStartShift)
        {
            try
            {
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    string szQuery = "SELECT * FROM SupervisorShift " +
                                        $"WHERE SupervisorShiftID={iSupervisorShiftID}";
                    DataTable dt = List(szQuery);
                    if (dt.Rows.Count > 0)
                        return false;

                    //--- Insert new SupervisorShift
                    szQuery = "INSERT INTO SupervisorShift (SupervisorShiftID, SupervisorID, SupervisorCode, SupervisorName, "+
                                "SupervisorGroupID, SupervisorGroupName, StartShiftDateTime) VALUES "+
                                $"({iSupervisorShiftID}, '{szSupervisorId}', '{szSupervisorCode}', '{szSupervisorName}', "+
                                $"'{szSupervisorGroupId}', '{szSupervisorGroupName}', '{dtSupervisorStartShift.ToString(TCT_Lib.szDateTimeFormatDB)}')";
                    Execute(szQuery);

                    TCT_Lib.iSupervisorShiftID      = iSupervisorShiftID;
                    TCT_Lib.szSupervisorID          = szSupervisorId;
                    TCT_Lib.szSupervisorCode        = szSupervisorCode;
                    TCT_Lib.szSupervisorName        = szSupervisorName;
                    TCT_Lib.szSupervisorGroupID     = szSupervisorGroupId;
                    TCT_Lib.szSupervisorGroupName   = szSupervisorGroupName;
                    TCT_Lib.dtSupervisorStartShift  = dtSupervisorStartShift;

                    return true;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error UpdateCurrentSupervisor : " + err.Message);
                LogInfo.WriteLog("- Error UpdateCurrentSupervisor : " + err.Message);

                return false;
            }
        }

        public int GetTotalUnsendTransToDC()
        {
            try
            {
                string szQuery  = "SELECT COUNT(*) FROM LaneTransaction WHERE SendStatusDC=0";
                DataTable dt    = List(szQuery);
                return int.Parse(dt.Rows[0][0].ToString());
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error GetTotalUnsendTransToDC : " + err.Message);
                LogInfo.WriteLog("- Error GetTotalUnsendTransToDC : " + err.Message);
                return 0;
            }
        }

        //=======================================================================================
        private long GetLaneTransactionID()
        {
            try
            {
                string szQuery  = "SELECT MAX(TransactionID) FROM LaneTransaction";
                DataTable dt    = List(szQuery);
                return dt.Rows[0][0] == DBNull.Value ? 1 : long.Parse(dt.Rows[0][0].ToString()) + 1;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error GetLaneTransactionID : " + err.Message);
                LogInfo.WriteLog("- Error GetLaneTransactionID : " + err.Message);
                return 0;
            }
        }

        private string GenerateTransSignalCode(bool bTowing=false)
        {
            int rePayment           = TCT_Lib.bRePaymentFlag ? 1 : 0;
            int reClassified        = TCT_Lib.bReClassifiedFlag ? 1 : 0;
            int reversing           = TCT_Lib.bReversingFlag ? 1 : 0;
            int simuPass            = TCT_Lib.bSimuPassFlag ? 1 : 0;
            int imageTaken          = 0;
            int classDiscrepancy    = TCT_Lib.bClassDisFlag ? 1 : 0;
            //int towingVehicle       = TCT_Lib.eTowingState == TowingState.None ? 0 : 1;
            int towingVehicle       = TCT_Lib.iIsTowing;
            int convoyPayment       = TCT_Lib.bConvoyMode ? 1 : 0;

            if (TCT_Lib.bConvoyMode && !TCT_Lib.bAllowConvoy)   // Insert Vehicle Convoy
                convoyPayment = 0;

/*
            if (TCT_Lib.eCurLaneType == LaneType.DUAL)
            {
                if (bTowing)
                {
                    if ((TCT_Lib.xTowingPayment.payType != (int)PayType.MPass) && (TCT_Lib.xTowingPayment.payType != (int)PayType.EasyPass))
                        rePayment = 1;
                }
            }
*/            

            //--- Violation force classDiss = 0
/*
            if (TCT_Lib.xCurPayment != null)
            {
                if (TCT_Lib.xCurPayment.payType == (int)PayType.Violation)
                    classDiscrepancy = 0;

                //-------------------------------------------------------------------------------
                if (!bTowing)
                {
                    int iTC_Class = TCT_Lib.xListLoopCar.Count == 0 ? TCT_Lib.iCurTC_Class : TCT_Lib.xListLoopCar[0].iTC_Class;
                    if (iTC_Class == TCT_Lib.xCurAVC.avcClass)
                        classDiscrepancy = 0;
                }

                //----------------------------------------------------------------------------------
            }
*/            

            string szSignalCode     = "";
            var iListSignalCode     = new List<int>();
            iListSignalCode.Add(rePayment);
            iListSignalCode.Add(reClassified);
            iListSignalCode.Add(reversing);
            iListSignalCode.Add(simuPass);
            iListSignalCode.Add(imageTaken);
            iListSignalCode.Add(classDiscrepancy);
            iListSignalCode.Add(towingVehicle);
            iListSignalCode.Add(convoyPayment);

            szSignalCode = string.Join(",", iListSignalCode.Select(n => n.ToString()).ToArray());

            return szSignalCode;
        }

        public long WriteLaneTransaction(bool bLoopBack, bool bObBack, bool bForceEndTrans=false)
        {

            if (TCT_Lib.eCurLaneType == LaneType.ETC)
            {
                return 0;
            }

            int iJobNo  = TCT_Lib.iJobNo;
            int nTrx    = TCT_Lib.nTrx;
            if (TCT_Lib.eCurLaneMode == LaneMode.CLOSED)
            {
                iJobNo  = 999;
                nTrx    = 0;
            }

            /*
                        if (TCT_Lib.eCurLaneMode == LaneMode.CLOSED)
                        {
                            LogInfo.WriteLog("- Not write transaction : Lane mode is closed.");
                            return 0;
                        }
            */


            /*
                        TimeSpan span = DateTime.Now.Subtract(dtLastTrans);
                        if (span.Seconds < 1)
                        {
                            LogInfo.WriteLog($"- Not write transaction : LastDateTimeTransaction={dtLastTrans.ToString("HH:mm:ss:.ffff")}, CurrentTime={DateTime.Now.ToString("HH:mm:ss.ffff")}");
                            return 0;
                        }
            */

            long iID = 0;
            var InvC = CultureInfo.InvariantCulture;
            try
            {
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    int iTC_Class           = TCT_Lib.xListLoopCar.Count == 0 ? TCT_Lib.iCurTC_Class : TCT_Lib.xListLoopCar[0].iTC_Class;
                    decimal fFareAmount     = TCT_Lib.xListLoopCar.Count == 0 ? TCT_Lib.fCurFareAmount : TCT_Lib.xListLoopCar[0].fFareAmount;
                    var xPayment            = TCT_Lib.xListLoopCar.Count == 0 ? TCT_Lib.xCurPayment : TCT_Lib.xListLoopCar[0].xPayment;
                    string szTransRemark    = TCT_Lib.xListLoopCar.Count == 0 ? TCT_Lib.szCurTransRemark : TCT_Lib.xListLoopCar[0].szTransRemark;

                    if (TCT_Lib.xListLoopCar.Count == 0)
                        LogInfo.WriteLaneEvent($"Write DB Trans : TC_Class={iTC_Class}, ListLoopCount={TCT_Lib.xListLoopCar.Count}, CurTC_Class={TCT_Lib.iCurTC_Class}, CurPayment={TCT_Lib.xCurPayment.payType}");
                    else
                        LogInfo.WriteLaneEvent($"Write DB Trans : TC_Class={iTC_Class}, ListLoopCount={TCT_Lib.xListLoopCar.Count}, CurTC_Class={TCT_Lib.iCurTC_Class}, CurPayment={TCT_Lib.xListLoopCar[0].xPayment.payType}, LoopCar={TCT_Lib.xListLoopCar[0].iTC_Class}");

                    if ((xPayment.payType == (int)PayType.MPass) || (xPayment.payType == (int)PayType.EasyPass))
                    {
                        LogInfo.WriteLaneEvent($"Paymennt is MPass or EasyPass. (No create transaction)");
                        return 0;
                    }

                    if (TCT_Lib.eCurLaneType == LaneType.DUAL)
                    {
                        if ((iTC_Class == 0) && (xPayment.payType != (int)PayType.Violation))
                            return 0;
                    }

                    int iEmvDetailID = 0;
                    if ((xPayment.payType == (int)PayType.EMV) || (xPayment.payType == (int)PayType.QrCode))
                    {
                        //TCT_Lib.iEmvDetailID = WriteEdcDetail(xPayment.payType);
                        iEmvDetailID =  xPayment.emvDetailID == 0 ? (int)TCT_Lib.LaneDB.GetCurEmvDetailID() : xPayment.emvDetailID;
                    }
                    else
                        iEmvDetailID = 0;

                    if (TCT_Lib.szListReceiptNo.Count > 0)
                    {
                        if ((iTC_Class == 1) || (iTC_Class == 2))
                        {
                            if ((xPayment.payType == (int)PayType.CASH) ||
                                (xPayment.payType == (int)PayType.COUPON) ||
                                (xPayment.payType == (int)PayType.EMV) ||
                                (xPayment.payType == (int)PayType.QrCode) ||
                                (xPayment.payType == (int)PayType.PM6) ||
                                (xPayment.payType == (int)PayType.PM7) ||
                                (xPayment.payType == (int)PayType.PM8) ||
                                (xPayment.payType == (int)PayType.PM9))
                            {
                                TCT_Lib.szCurFareReceiptNo = TCT_Lib.szListReceiptNo[0];
                                TCT_Lib.szListReceiptNo.RemoveAt(0);
                            }
                        }
                    }
                    else
                    {
                        if ((iTC_Class == 1) || (iTC_Class == 2))
                        {
                            if ((xPayment.payType == (int)PayType.CASH) ||
                                (xPayment.payType == (int)PayType.COUPON) ||
                                (xPayment.payType == (int)PayType.EMV) ||
                                (xPayment.payType == (int)PayType.QrCode) ||
                                (xPayment.payType == (int)PayType.PM6) ||
                                (xPayment.payType == (int)PayType.PM7) ||
                                (xPayment.payType == (int)PayType.PM8) ||
                                (xPayment.payType == (int)PayType.PM9))
                            {
                                TCT_Lib.szCurFareReceiptNo = TCT_Lib.LaneDB.GetRunningReceiptNo(PrintReceiptType.FareType);
                            }
                        }
                    }

                    if (xPayment.payType == (int)PayType.Violation)
                    {
                        TCT_Lib.szCurFareReceiptNo  = "";
                        TCT_Lib.bClassDisFlag       = false;
                        if ((iTC_Class == 1) || (iTC_Class == 2))
                        {
                            if (iTC_Class != TCT_Lib.xCurAVC.avcClass)
                                TCT_Lib.bClassDisFlag = true;
                        }
                    }

                    if ((xPayment.payType == (int)PayType.EMV) ||
                        (xPayment.payType == (int)PayType.QrCode))
                    {
                        if (xPayment.payAmount == 0)
                            xPayment.payAmount = iTC_Class == 1 ? TCT_Lib.fFareClass1 : TCT_Lib.fFareClass2;
                    }

                    if (xPayment.payDateTime == null)
                        xPayment.payDateTime = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);

                    if ((xPayment.payType != (int)PayType.Violation) && (szTransRemark == "VIOLATION"))
                        szTransRemark = "";

                    if (bForceEndTrans)
                        szTransRemark = "Force End Transaction";

                    if ((szTransRemark.ToUpper() == "TOWING") && (TCT_Lib.iIsTowing == 0) && (TCT_Lib.iTowingParentID == 0))
                        szTransRemark = "";

                    if ((TCT_Lib.iIsTowing == 1) && (TCT_Lib.iTowingParentID == 0))
                    {
                        if (bForceEndTrans)
                            szTransRemark = "TOWING #1 - Force End";
                        else
                            szTransRemark = "TOWING #1";
                    }

                    if ((iTC_Class == 0) && (xPayment.payType == (int)PayType.HPMC))
                    {
                        TCT_Lib.bRePaymentFlag      = false;
                        TCT_Lib.bReClassifiedFlag   = false;
                        TCT_Lib.bReversingFlag      = false;
                        TCT_Lib.bClassDisFlag       = false;
                        TCT_Lib.bConvoyMode         = false;

                    }
                    string szTrans_SignalCode   = GenerateTransSignalCode();
                    string szLC_SignalStatus    = (bLoopBack ? "1" : "0") + "," + (bObBack ? "1" : "0") + "," + TCT_Lib.xCurAVC.avcClass + "," + TCT_Lib.xCurAVC.avcError;

                    LogInfo.WriteLaneEvent($"Write DB Trans : TransID={iFareTransID}, JobNo={TCT_Lib.iJobNo}, nTrx={TCT_Lib.nTrx}, ReceiptNo={TCT_Lib.szCurFareReceiptNo}, TC_Class={iTC_Class}, Fare={fFareAmount}, PayType={xPayment.payTypeName}, AVC_Class={TCT_Lib.xCurAVC.avcClass}, Loop={bLoopBack}, OB={bObBack}, ForceEndTrans={bForceEndTrans}");

                    LaneMode xLaneMode = TCT_Lib.eCurLaneMode;
                    if (TCT_Lib.bMaintenanceMode && TCT_Lib.eCurLaneMode == LaneMode.HPMC)
                        xLaneMode = LaneMode.MAINTENANCE;
                    if (!TCT_Lib.bMaintenanceMode)
                    {
                        if (xPayment.payType == (int)PayType.HPMC)
                            xLaneMode = LaneMode.HPMC;
                        else
                            xLaneMode = LaneMode.NORMAL;
                    }

                    if (TCT_Lib.bConvoyMode && TCT_Lib.bAllowConvoy)
                    {
                        if (TCT_Lib.iConvoyPayTypeID == 1)
                        {
                            xPayment.payType        = (int)PayType.EMV;
                            xPayment.payTypeName    = TCT_Lib.GetPayMethodName((int)PayType.EMV);
                        }
                        else if (TCT_Lib.iConvoyPayTypeID == 2)
                        {
                            xPayment.payType        = (int)PayType.QrCode;
                            xPayment.payTypeName    = TCT_Lib.GetPayMethodName((int)PayType.QrCode);
                        }

                            iEmvDetailID = TCT_Lib.iConvoyEmvDetailID;
                    }

                    if (TCT_Lib.eCurLaneType == LaneType.DUAL)
                    {
                        if ((xPayment.payType != (int)PayType.MPass) && (xPayment.payType != (int)PayType.EasyPass))
                        {
                            //--- No Tag information
                            TCT_Lib.xCurTAG.tagType         = 0;
                            TCT_Lib.xCurTAG.tagRemark       = "";
                            TCT_Lib.xCurTAG.tagPAN          = "";
                            TCT_Lib.xCurTAG.tagBalance      = 0;
                            TCT_Lib.xCurTAG.tagSignalCode   = "";
                        }
                    }

                    //if (TCT_Lib.eCurLaneMode == LaneMode.HPMC)
                    if ((iTC_Class == 0) && (xPayment.payType == 10))
                    {
                        xPayment.payType            = (int)PayType.HPMC;
                        xPayment.payTypeName        = TCT_Lib.GetPayMethodName((int)PayType.HPMC);
                        TCT_Lib.szCurFareReceiptNo  = "";
                    }

                    if ((xPayment.payType == (int)PayType.CASH) || (xPayment.payType == (int)PayType.COUPON))
                    {
                        if (iTC_Class == 1)
                            xPayment.payAmount = TCT_Lib.fFareClass1;
                        else if (iTC_Class == 2)
                            xPayment.payAmount = TCT_Lib.fFareClass2;
                    }

                    //if (TCT_Lib.eCurLaneMode == LaneMode.HPMC)
                    if ((iTC_Class == 0) && (xPayment.payType == (int)PayType.HPMC))
                    {
                        fFareAmount = xPayment.payAmount = 0;
                    }

                    //---------------------------------------------------------------
                    //--- Check EMV Matching Transaction
                    if ((xPayment.payType == (int)PayType.EMV) && (iEmvDetailID == 0))
                    {
                        nTrx = TCT_Lib.nTrx;
                        if (TCT_Lib.eCurLaneMode == LaneMode.CLOSED)
                        {
                            iJobNo  = 999;
                            nTrx    = 0;
                        }
                        //--- Not found EmvDetailID, then get last emv detail from EmvDetail table
                        iEmvDetailID = xPayment.emvDetailID == 0 ? (int)TCT_Lib.LaneDB.GetCurEmvDetailID() : xPayment.emvDetailID;
                        LogInfo.WriteLaneEvent($"+ Not found EmvDetail, then get EmvDetailID={iEmvDetailID}, JobNo={iJobNo}, nTrx={nTrx}");
                    }

                    long iDmtCardTransID = 0;
                    if (xPayment.payType == (int)PayType.DMT_CARD)
                    {
                        iDmtCardTransID = GetCurDmtCardTransID();
                        LogInfo.WriteLaneEvent("+ Pay DmtCardTransID=" + iDmtCardTransID);
                    }
                    //------------------------------------------------------------------------
                    nTrx = TCT_Lib.nTrx;
                    if (TCT_Lib.eCurLaneMode == LaneMode.CLOSED)
                    {
                        iJobNo  = 999;
                        nTrx    = 0;
                    }
                    dtLastTrans     = DateTime.Now;
                    string szQuery = "INSERT INTO LaneTransaction (TransactionID, TransactionDateTime, PlazaID, PlazaName, " +
                                    "LaneID, LaneName, LaneType, LaneTypeName, LaneMode, LaneModeName, LaneDirection, LaneDirectionName, " +
                                    "StaffShiftID, StaffID, StaffCode, StaffName, StaffStartShiftDateTime, " +
                                    "SupervisorShiftID, SupervisorID, SupervisorCode, SupervisorName, SupervisorStartShiftDateTime, JobNo, nTrx, " +
                                    "TC_Class, AVC_Class, AVC_Axle, AVC_Wheel, AVC_DualWheel, AVC_Direction, FareAmount, " +
                                    "PayType, PayTypeName, PayAmount, PayDateTime, PayRemark, ReceiptNo, EMV_DetailID, " +
                                    "TAG_Type, TAG_PAN, TAG_Balance, TAG_SignalCode, TAG_Remark, TransRemark, IsTowing, TowingParentID, "+
                                    "Trans_SignalCode, LC_SignalStatus, DmtCardTransID) VALUES " +
                                    $"({iFareTransID}, '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}', '{TCT_Lib.szPlazaID}', '{TCT_Lib.szPlazaName}', " +
                                    $"{TCT_Lib.iLaneID}, '{TCT_Lib.szLaneName}', {(int)TCT_Lib.eCurLaneType}, '{TCT_Lib.GetLaneTypeName(TCT_Lib.eCurLaneType)}', " +
                                    $"{(int)xLaneMode}, '{TCT_Lib.GetLaneModeName(xLaneMode)}', " +
                                    $"{(int)TCT_Lib.eLaneDirection}, '{TCT_Lib.GetLaneDirection(TCT_Lib.eLaneDirection)}', " +
                                    //$"{TCT_Lib.iStaffShiftID}, '{TCT_Lib.szStaffID}', '{TCT_Lib.szStaffCode}', '{TCT_Lib.szStaffName}', '{TCT_Lib.dtStaffStartShift.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}', " +
                                    $"{TCT_Lib.iStaffShiftID}, '{TCT_Lib.szStaffID}', '{TCT_Lib.szStaffCode}', '{TCT_Lib.szStaffName}', '{TCT_Lib.dtStaffStartShift.ToString("yyyy-MM-dd HH:mm:ss", InvC)}', " +
                                    //$"{TCT_Lib.iSupervisorShiftID}, '{TCT_Lib.szSupervisorID}', '{TCT_Lib.szSupervisorCode}', '{TCT_Lib.szSupervisorName}', '{TCT_Lib.dtSupervisorStartShift.ToString("yyyy-MM-dd HH:mm:ss.ffff", InvC)}', " +
                                    $"{TCT_Lib.iSupervisorShiftID}, '{TCT_Lib.szSupervisorID}', '{TCT_Lib.szSupervisorCode}', '{TCT_Lib.szSupervisorName}', '{TCT_Lib.dtSupervisorStartShift.ToString("yyyy-MM-dd HH:mm:ss", InvC)}', " +
                                    $"{iJobNo}, {nTrx}, {iTC_Class}, " +
                                    $"{TCT_Lib.xCurAVC.avcClass}, {TCT_Lib.xCurAVC.avcAxle}, {TCT_Lib.xCurAVC.avcWheel}, {TCT_Lib.xCurAVC.avcDualWheel}, {TCT_Lib.xCurAVC.avcDirection}, " +
                                    $"{fFareAmount}, {xPayment.payType}, '{xPayment.payTypeName}', {xPayment.payAmount}, " +
                                    $"'{xPayment.payDateTime}', '{xPayment.payRemark}', '{TCT_Lib.szCurFareReceiptNo}', {iEmvDetailID}, " +
                                    $"{TCT_Lib.xCurTAG.tagType}, '{TCT_Lib.xCurTAG.tagPAN}', {TCT_Lib.xCurTAG.tagBalance}, '{TCT_Lib.xCurTAG.tagSignalCode}', '{TCT_Lib.xCurTAG.tagRemark}', " +
                                    $"'{szTransRemark}', {TCT_Lib.iIsTowing}, {TCT_Lib.iTowingParentID}, '{szTrans_SignalCode}', '{szLC_SignalStatus}', {iDmtCardTransID}" +
                                    $")";
                    Execute(szQuery);
                    iID = iFareTransID;
                    System.Threading.Interlocked.Increment(ref iFareTransID);
                    TCT_Lib.nTrx++;
                        
                    //---------------------------
                    if (TCT_Lib.szQrRef1.Trim() != "")
                    {
                        szQuery = $"UPDATE LaneTransaction SET PayRemark='Ref1={TCT_Lib.szQrRef1},Ref2={TCT_Lib.szQrRef2}' " +
                                    $"WHERE TransactionID={iID}";
                        Execute(szQuery);
                    }

                    //------------------------------------------------------
                    if (TCT_Lib.bConvoyMode)
                    {
                        if ((TCT_Lib.iConvoyClass1 + TCT_Lib.iConvoyClass2) <= 0)
                        {
                            LogInfo.WriteLog("+ End Convoy");
                            TCT_Lib.laneController.UpdateUI_Connvoy();
                            TCT_Lib.bConvoyMode         = false;
                            TCT_Lib.iConvoyPayTypeID    = 0;
                            TCT_Lib.iConvoyEmvDetailID  = 0;
                        }
                        TCT_Lib.bAllowConvoy = true;
                    }
                    //------------------------------------------------------

                    return iID;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error WriteLaneTransaction : " + err.Message);
                LogInfo.WriteLog("- Error WriteLaneTransaction : " + err.Message);

                return 0;
            }
        }

        public long WriteLaneTransaction_Towing(bool bLoopBack, bool bObBack, bool bForceEndTrans = false)
        {
/*
            if (TCT_Lib.eCurLaneMode == LaneMode.CLOSED)
            {
                LogInfo.WriteLog("- Not write transaction towing : Lane mode is closed.");
                return 0;
            }
*/

            int iJobNo  = TCT_Lib.iJobNo;
            int nTrx    = TCT_Lib.nTrx;
            if (TCT_Lib.eCurLaneMode == LaneMode.CLOSED)
            {
                iJobNo  = 999;
                nTrx    = 0;
            }

            if (TCT_Lib.eCurLaneType == LaneType.ETC)
            {
                return 0;
            }

            long iID = 0;
            var InvC = CultureInfo.InvariantCulture;
            try
            {
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    //int iTC_Class       = TCT_Lib.xListLoopCar[0].iTC_Class;
                    //decimal fFareAmount = TCT_Lib.xListLoopCar[0].fFareAmount;
                    //var xPayment        = TCT_Lib.xListLoopCar[0].xPayment;
                    //string szTransRemark = TCT_Lib.xListLoopCar[0].szTransRemark;

                    int iTC_Class           = TCT_Lib.iTowingTC_Class;
                    decimal fFareAmount     = TCT_Lib.fTowingFareAmount;
                    var xPayment            = TCT_Lib.xTowingPayment;
                    string szTransRemark    = bForceEndTrans ? "TOWING #2 - Force End" : "TOWING #2";

                    if ((xPayment.payType == (int)PayType.MPass) || (xPayment.payType == (int)PayType.EasyPass))
                        return 0;

                    if (xPayment.payType == (int)PayType.Violation)
                    {
                        TCT_Lib.szCurFareReceiptNo  = "";
                        TCT_Lib.bClassDisFlag       = false;
                        if ((iTC_Class == 1) || (iTC_Class == 2))
                        {
                            if (iTC_Class != TCT_Lib.xTowingAVC.avcClass)
                                TCT_Lib.bClassDisFlag = true;
                        }
                    }

                    int iEmvDetailID = 0;
                    if ((xPayment.payType == (int)PayType.EMV) || (xPayment.payType == (int)PayType.QrCode))
                    {
                        iEmvDetailID = (int)TCT_Lib.LaneDB.GetCurEmvDetailID();
                        //TCT_Lib.iEmvDetailID = WriteEdcDetail(xPayment.payType, true);
                    }
                    else
                        iEmvDetailID = 0;

                    if ((xPayment.payType == (int)PayType.EMV) || (xPayment.payType == (int)PayType.QrCode))
                    {
                        LogInfo.WriteLog("+ EMV Payment : EmvDetailID=" + iEmvDetailID);
                        LogInfo.WriteLogEDC("+ EMV Payment : EmvDetailID=" + iEmvDetailID);
                        LogInfo.WriteLaneEvent("+ EMV Payment : EmvDetailID=" + iEmvDetailID);
                    }

                    if (TCT_Lib.szListReceiptNo.Count > 0)
                    {
                        if ((iTC_Class == 1) || (iTC_Class == 2))
                        {
                            if ((xPayment.payType == (int)PayType.CASH) ||
                                (xPayment.payType == (int)PayType.COUPON) ||
                                (xPayment.payType == (int)PayType.EMV) ||
                                (xPayment.payType == (int)PayType.QrCode) ||
                                (xPayment.payType == (int)PayType.PM6) ||
                                (xPayment.payType == (int)PayType.PM7) ||
                                (xPayment.payType == (int)PayType.PM8) ||
                                (xPayment.payType == (int)PayType.PM9))
                            {
                                TCT_Lib.szCurFareReceiptNo = TCT_Lib.szListReceiptNo[0];
                                TCT_Lib.szListReceiptNo.RemoveAt(0);
                            }
                        }
                    }
                    else
                    {
                        if ((iTC_Class == 1) || (iTC_Class == 2))
                        {
                            if ((xPayment.payType == (int)PayType.CASH) ||
                                (xPayment.payType == (int)PayType.COUPON) ||
                                (xPayment.payType == (int)PayType.EMV) ||
                                (xPayment.payType == (int)PayType.QrCode) ||
                                (xPayment.payType == (int)PayType.PM6) ||
                                (xPayment.payType == (int)PayType.PM7) ||
                                (xPayment.payType == (int)PayType.PM8) ||
                                (xPayment.payType == (int)PayType.PM9))
                            {
                                TCT_Lib.szCurFareReceiptNo = TCT_Lib.LaneDB.GetRunningReceiptNo(PrintReceiptType.FareType);
                            }
                        }
                    }

                    if ((xPayment.payType == (int)PayType.EMV) ||
                        (xPayment.payType == (int)PayType.QrCode))
                    {
                        if (xPayment.payAmount == 0)
                            xPayment.payAmount = iTC_Class == 1 ? TCT_Lib.fFareClass1 : TCT_Lib.fFareClass2;
                    }

                    if (xPayment.payDateTime == null)
                        xPayment.payDateTime = DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB);

                    if ((iTC_Class == 0) && (xPayment.payType == (int)PayType.HPMC))
                    {
                        TCT_Lib.bRePaymentFlag      = false;
                        TCT_Lib.bReClassifiedFlag   = false;
                        TCT_Lib.bReversingFlag      = false;
                        TCT_Lib.bClassDisFlag       = false;
                        TCT_Lib.bConvoyMode         = false;

                    }
                    string szTrans_SignalCode   = GenerateTransSignalCode(true);
                    string szLC_SignalStatus    = (bLoopBack ? "1" : "0") + "," + (bObBack ? "1" : "0") + "," + TCT_Lib.xTowingAVC.avcClass + "," + TCT_Lib.xTowingAVC.avcError;

                    LogInfo.WriteLaneEvent($"Write DB Towing : TransID={iFareTransID}, JobNo={TCT_Lib.iJobNo}, nTrx={TCT_Lib.nTrx}, ReceiptNo={TCT_Lib.szCurFareReceiptNo}, TC_Class={iTC_Class}, Fare={fFareAmount}, PayType={xPayment.payTypeName}, AVC_Class={TCT_Lib.xTowingAVC.avcClass}, Loop={bLoopBack}, OB={bObBack}");

                    LaneMode xLaneMode = TCT_Lib.eCurLaneMode;
                    if (TCT_Lib.bMaintenanceMode && TCT_Lib.eCurLaneMode == LaneMode.HPMC)
                        xLaneMode = LaneMode.MAINTENANCE;
                    if (!TCT_Lib.bMaintenanceMode)
                    {
                        if (xPayment.payType == (int)PayType.HPMC)
                            xLaneMode = LaneMode.HPMC;
                        else
                            xLaneMode = LaneMode.NORMAL;
                    }

                    if (TCT_Lib.eCurLaneType == LaneType.DUAL)
                    {
                        if ((xPayment.payType != (int)PayType.MPass) && (xPayment.payType != (int)PayType.EasyPass))
                        {
                            //--- No Tag information
                            TCT_Lib.xCurTAG.tagType         = 0;
                            TCT_Lib.xCurTAG.tagRemark       = "";
                            TCT_Lib.xCurTAG.tagPAN          = "";
                            TCT_Lib.xCurTAG.tagBalance      = 0;
                            TCT_Lib.xCurTAG.tagSignalCode   = "";
                        }
                    }

                    //if (TCT_Lib.eCurLaneMode == LaneMode.HPMC)
                    if ((iTC_Class == 0) && (xPayment.payType == (int)PayType.HPMC))
                    {
                        fFareAmount = xPayment.payAmount    = 0;
                        TCT_Lib.szCurFareReceiptNo          = "";
                    }

                    long iDmtCardTransID = 0;
                    if (xPayment.payType == (int)PayType.DMT_CARD)
                    {
                        iDmtCardTransID = GetCurDmtCardTransID();
                        LogInfo.WriteLaneEvent("+ Pay DmtCardTransID=" + iDmtCardTransID);
                    }

                    nTrx = TCT_Lib.nTrx;
                    if (TCT_Lib.eCurLaneMode == LaneMode.CLOSED)
                    {
                        iJobNo  = 999;
                        nTrx    = 0;
                    }
                    string szQuery = "INSERT INTO LaneTransaction (TransactionID, TransactionDateTime, PlazaID, PlazaName, " +
                                    "LaneID, LaneName, LaneType, LaneTypeName, LaneMode, LaneModeName, LaneDirection, LaneDirectionName, " +
                                    "StaffShiftID, StaffID, StaffCode, StaffName, StaffStartShiftDateTime, " +
                                    "SupervisorShiftID, SupervisorID, SupervisorCode, SupervisorName, SupervisorStartShiftDateTime, JobNo, nTrx, " +
                                    "TC_Class, AVC_Class, AVC_Axle, AVC_Wheel, AVC_DualWheel, AVC_Direction, FareAmount, " +
                                    "PayType, PayTypeName, PayAmount, PayDateTime, PayRemark, ReceiptNo, EMV_DetailID, " +
                                    "TAG_Type, TAG_PAN, TAG_Balance, TAG_SignalCode, TAG_Remark, TransRemark, IsTowing, TowingParentID, " +
                                    "Trans_SignalCode, LC_SignalStatus, DmtCardTransID) VALUES " +
                                    $"({iFareTransID}, '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}', '{TCT_Lib.szPlazaID}', '{TCT_Lib.szPlazaName}', " +
                                    $"{TCT_Lib.iLaneID}, '{TCT_Lib.szLaneName}', {(int)TCT_Lib.eCurLaneType}, '{TCT_Lib.GetLaneTypeName(TCT_Lib.eCurLaneType)}', " +
                                    $"{(int)xLaneMode}, '{TCT_Lib.GetLaneModeName(xLaneMode)}', " +
                                    $"{(int)TCT_Lib.eLaneDirection}, '{TCT_Lib.GetLaneDirection(TCT_Lib.eLaneDirection)}', " +
                                    //$"{TCT_Lib.iStaffShiftID}, '{TCT_Lib.szStaffID}', '{TCT_Lib.szStaffCode}', '{TCT_Lib.szStaffName}', '{TCT_Lib.dtStaffStartShift.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}'," +
                                    $"{TCT_Lib.iStaffShiftID}, '{TCT_Lib.szStaffID}', '{TCT_Lib.szStaffCode}', '{TCT_Lib.szStaffName}', '{TCT_Lib.dtStaffStartShift.ToString("yyyy-MM-dd HH:mm:ss", InvC)}'," +
                                    $"{TCT_Lib.iSupervisorShiftID}, '{TCT_Lib.szSupervisorID}', '{TCT_Lib.szSupervisorCode}', '{TCT_Lib.szSupervisorName}', '{TCT_Lib.dtSupervisorStartShift.ToString("yyyy-MM-dd HH:mm:ss", InvC)}'," + 
                                    $"{iJobNo}, {nTrx}, {iTC_Class}, " +
                                    $"{TCT_Lib.xTowingAVC.avcClass}, {TCT_Lib.xTowingAVC.avcAxle}, {TCT_Lib.xTowingAVC.avcWheel}, {TCT_Lib.xTowingAVC.avcDualWheel}, {TCT_Lib.xTowingAVC.avcDirection}, " +
                                    $"{fFareAmount}, {xPayment.payType}, '{xPayment.payTypeName}', {xPayment.payAmount}, " +
                                    $"'{xPayment.payDateTime}', '{xPayment.payRemark}', '{TCT_Lib.szCurFareReceiptNo}', {iEmvDetailID}, " +
                                    $"{TCT_Lib.xCurTAG.tagType}, '{TCT_Lib.xCurTAG.tagPAN}', {TCT_Lib.xCurTAG.tagBalance}, '{TCT_Lib.xCurTAG.tagSignalCode}', '{TCT_Lib.xCurTAG.tagRemark}', " +
                                    $"'{szTransRemark}', 0, {TCT_Lib.iTowingParentID}, '{szTrans_SignalCode}', '{szLC_SignalStatus}', {iDmtCardTransID}" +
                                    $")";
                    Execute(szQuery);
                    iID = iFareTransID;
                    System.Threading.Interlocked.Increment(ref iFareTransID);
                    TCT_Lib.nTrx++;

                    // ---------------------------
                    if (TCT_Lib.szQrRef1.Trim() != "")
                    {
                        szQuery = $"UPDATE LaneTransaction SET PayRemark='Ref1={TCT_Lib.szQrRef1},Ref2={TCT_Lib.szQrRef2}' " +
                                    $"WHERE TransactionID={iID}";
                        Execute(szQuery);
                    }
                    //--------------------------------------------

                    return iID;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error WriteLaneTransaction_Towing : " + err.Message);
                LogInfo.WriteLog("- Error WriteLaneTransaction_Towing : " + err.Message);

                return 0;
            }
        }

        public long WriteLaneTransaction_ETC_CompliedPassage(dynamic objCompiledPassage)
        {
            if (objCompiledPassage == null)
                return 0;

            long iID = 0;
            var InvC = CultureInfo.InvariantCulture;
            try
            {
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    int iTC_Class           = 0;
                    decimal fFareAmount     = 0;
                    if (objCompiledPassage["params"]["payment"] != null)
                    {
                        if ((decimal)objCompiledPassage["params"]["payment"].Count > 0)
                        {
                            if (objCompiledPassage["params"]["payment"][0]["manualClassification"] != null)
                            {
                                fFareAmount = (objCompiledPassage["params"]["payment"][0]["manualClassification"]["fare"] == null) ? 0 :
                                                        (decimal)objCompiledPassage["params"]["payment"][0]["manualClassification"]["fare"] / 100;
                            }
                            else
                            {
                                fFareAmount = (objCompiledPassage["params"]["payment"][0]["manualPayment"]["amount"] == null) ? 0 :
                                                            (decimal)objCompiledPassage["params"]["payment"][0]["manualPayment"]["amount"] / 100;
                            }
                        }
                    }

                    LogInfo.WriteLog("+ ETC_CompliedPassage : param_payment success");
                    string szSignalCode     = (objCompiledPassage["params"]["signalCode"] == null) ? "" : (string)objCompiledPassage["params"]["signalCode"];
                    //string szTransRemark    = szSignalCode == "01" ? (string)objCompiledPassage["params"]["laneMode"] : "";
                    string szTransRemark    = "";

                    if (szSignalCode == "01")
                    {
                        if (TCT_Lib.eCurLaneMode == LaneMode.HPMC)
                            szTransRemark = (string)objCompiledPassage["params"]["laneMode"];
                        else
                        {
                            if ((TCT_Lib.eCurLaneType == LaneType.DUAL) || (TCT_Lib.eCurLaneType == LaneType.ETC))
                            {
                                if (TCT_Lib.bEndDualHPMC)
                                {
                                    szTransRemark = "END_HPMC";
                                    TCT_Lib.bEndDualHPMC = false;
                                }
                                else
                                    szTransRemark = "Re-Payment";
                            }
                            else
                                szTransRemark = "Re-Payment";
                        }
                    }
                    else if (szSignalCode == "02")
                        szTransRemark = "Normal Passage";       // "รถผ่านทางแบบปกติ";
                    else if (szSignalCode == "08")
                        szTransRemark = "Found OBU without Detection";
                    else if (szSignalCode == "09")
                        szTransRemark = "Found Other OBU";      // "พบ OBU อื่นๆ ในรถ";
                    else if (szSignalCode == "10")
                        szTransRemark = "Low Balance Passage";
                    else if (szSignalCode == "19")
                        szTransRemark = "Multiple OBU";
                    else if (szSignalCode == "20")
                        szTransRemark = "Insufficient Passage";
                    else if (szSignalCode == "22")
                        szTransRemark = "Without OBU Passage";  // "ผ่านทางแบบไม่พบ OBU";
                    else if (szSignalCode == "23")
                        szTransRemark = "Unknown OBU Passage";
                    else if (szSignalCode == "24")
                        szTransRemark = "Unconfirmed Passage";
                    else if (szSignalCode == "26")
                        //szTransRemark = "Black-Listed OBU Passage";
                        szTransRemark = "OBU Blacklist";
                    else if (szSignalCode == "41")
                        szTransRemark = "Passage in Closed Mode";
                    else if (szSignalCode == "42")
                        szTransRemark = "Passage in Maintenance Mode";
                    else if (szSignalCode == "81")
                        szTransRemark = "Manual Open ALB";
                    else if (szSignalCode == "90")
                        szTransRemark = "Vehicle Backed Out";  // "รถถอยหลัง";

                    LogInfo.WriteLog("+ ETC_CompliedPassage : Signal success");
                    int iAvc_Class = 0, iAvc_Wheel = 0, iAvc_Axle = 0, iAvc_DualWheel = 0;
                    if (objCompiledPassage["params"]["vehicle"]["automaticClassification"] != null)
                    {
                        iAvc_Class      = (objCompiledPassage["params"]["vehicle"]["automaticClassification"]["avcClass"] == null) ? 0 :
                                                    (int)objCompiledPassage["params"]["vehicle"]["automaticClassification"]["avcClass"];
                        iAvc_Wheel      = (objCompiledPassage["params"]["vehicle"]["automaticClassification"]["avcWheels"] == null) ? 0 :
                                                    (int)objCompiledPassage["params"]["vehicle"]["automaticClassification"]["avcWheels"];
                        iAvc_Axle       = (objCompiledPassage["params"]["vehicle"]["automaticClassification"]["avcAxles"] == null) ? 0 :
                                                    (int)objCompiledPassage["params"]["vehicle"]["automaticClassification"]["avcAxles"];
                        iAvc_DualWheel  = 0;
                        if (objCompiledPassage["params"]["vehicle"]["automaticClassification"]["dualWheels"] != null)
                            iAvc_DualWheel = (bool)objCompiledPassage["params"]["vehicle"]["automaticClassification"]["dualWheels"] ? 1 : 0;
                    }
                    LogInfo.WriteLog("+ ETC_CompliedPassage : param->vehicle->automaticClassification success");
                    string szTAG_PAN = "", szTAG_Type = "";
                    int iTagType        = 0;       // 0=MPass, 1=EasyPass
                    int iPayType        = 0;
                    decimal fTAG_Balance = 0;
                    if (objCompiledPassage["params"]["obuTransaction"] != null)
                    {
                        szTAG_PAN       = (objCompiledPassage["params"]["obuTransaction"]["paymentMeans"]["pan"] == null) ? "" :
                                                    (string)objCompiledPassage["params"]["obuTransaction"]["paymentMeans"]["pan"];
                        //fTAG_Balance    = (decimal)objCompiledPassage["params"]["obuTransaction"]["balanceObu"] / 100;
                        //fFareAmount     = (decimal)objCompiledPassage["params"]["obuTransaction"]["priceInCurrency"] / 100;
                        fTAG_Balance    = (objCompiledPassage["params"]["obuTransaction"]["balanceObu"] == null) ? 0 :
                                                    decimal.Parse((string)objCompiledPassage["params"]["obuTransaction"]["balanceObu"]) / 100;
                        fFareAmount     = (objCompiledPassage["params"]["obuTransaction"]["priceInCurrency"] == null) ? 0 :
                                                    decimal.Parse((string)objCompiledPassage["params"]["obuTransaction"]["priceInCurrency"]) / 100;

                        if (szTAG_PAN.StartsWith("636685", StringComparison.Ordinal))
                        {
                            szTAG_Type  = "MPass";
                            iTagType    = 0;
                            iPayType    = (int)PayType.MPass;
                        }
                        else if (szTAG_PAN.StartsWith("308586", StringComparison.Ordinal))
                        {
                            szTAG_Type  = "EasyPass";
                            iTagType    = 1;
                            iPayType    = (int)PayType.EasyPass;
                        }
                        else if (szTAG_PAN.StartsWith("304592", StringComparison.Ordinal))
                        {
                            szTAG_Type  = "EasyPass";
                            iTagType    = 1;
                            iPayType    = (int)PayType.EasyPass;
                        }

                        if (TCT_Lib.eCurLaneMode == LaneMode.HPMC)
                        {
                            szTAG_Type  = "HP&MC";
                        }

                        if (objCompiledPassage["params"]["vehicle"]["automaticClassification"] == null)
                        {
                            iAvc_Class = (objCompiledPassage["params"]["obuTransaction"]["vehicleClass"] == null) ? 0 :
                                            (int)objCompiledPassage["params"]["obuTransaction"]["vehicleClass"];
                        }
                    }
                    LogInfo.WriteLog("+ ETC_CompliedPassage : param->obuTransaction success");

                    if ((szSignalCode == "09") || (szSignalCode == "90"))   // Found Other OBU or Vehicle Backed Out
                    {
                        iAvc_Class = iAvc_Wheel = iAvc_Axle = iAvc_DualWheel = 0;
                    }

                    if (szSignalCode == "01")
                    {
                        fFareAmount = 0;
                    }

                    if (TCT_Lib.eCurLaneMode == LaneMode.HPMC)
                    {
                        fFareAmount = 0;
                    }

                    if (szTAG_PAN != "")
                    {
                        TCT_Lib.szEtcLastUpdateObu = szTAG_PAN;
                        TCT_Lib.dtEtcLastUpdateObu = DateTime.Now;
                    }

                    long seqNo = (objCompiledPassage["params"]["sequenceNumber"] == null) ? 0 : (long)objCompiledPassage["params"]["sequenceNumber"];
                    LogInfo.WriteLog($"Write DB CompiledPassage : TransID={iFareTransID}, JobNo={TCT_Lib.iJobNo}, nTrx={seqNo}, TC_Class={iTC_Class}, Fare={fFareAmount}, AVC_Class={iAvc_Class}");

                    DateTime dtTransDateTime    = DateTime.Now;
                    string szPassageTime        = dtTransDateTime.ToString("yyyy-MM-dd HH:mm:ss", InvC);
                    if (objCompiledPassage["params"]["vehicle"] != null)
                    {
                        if (objCompiledPassage["params"]["vehicle"]["passageTime"] != null)

                        {
                            //--- This is real passage transaction
                            //dtTransDateTime = ((DateTime)objCompiledPassage["params"]["vehicle"]["passageTime"]).AddHours(7);
                            dtTransDateTime = Convert.ToDateTime((string)objCompiledPassage["params"]["vehicle"]["passageTime"]).AddHours(7);
                            szPassageTime   = Convert.ToDateTime((string)objCompiledPassage["params"]["vehicle"]["passageTime"]).AddHours(7).ToString("yyyy-MM-dd HH:mm:ss.fff", InvC);
                        }                        
                    }
                    LogInfo.WriteLog("+ ETC_CompliedPassage : param->vehicle->passageTime success");

                    //-----------------------------------------------------------------
                    LogInfo.WriteLog($"ETC CompiledPassage : SeqNo={seqNo}, SignalCode={szSignalCode}, PAN={szTAG_PAN}, PAN_Type={szTAG_Type}, AVC_Class={iAvc_Class}, Fare={fFareAmount}, TransID={iFareTransID}, JobNo={TCT_Lib.iJobNo}, nTrx={seqNo}");

                    LaneMode xLaneMode = TCT_Lib.eCurLaneMode;
                    if (TCT_Lib.bMaintenanceMode && TCT_Lib.eCurLaneMode == LaneMode.HPMC)
                        xLaneMode = LaneMode.MAINTENANCE;
                    //--------
                    string szQuery = "INSERT INTO LaneTransaction (TransactionID, TransactionDateTime, PlazaID, PlazaName, " +
                                    "LaneID, LaneName, LaneType, LaneTypeName, LaneMode, LaneModeName, LaneDirection, LaneDirectionName, " +
                                    "StaffShiftID, StaffID, StaffCode, StaffName, StaffStartShiftDateTime, " +
                                    "SupervisorShiftID, SupervisorID, SupervisorCode, SupervisorName, SupervisorStartShiftDateTime, JobNo, nTrx, " +
                                    "TC_Class, AVC_Class, AVC_Axle, AVC_Wheel, AVC_DualWheel, AVC_Direction, FareAmount, " +
                                    "PayType, PayTypeName, PayAmount, PayDateTime, PayRemark, ReceiptNo, EMV_DetailID, " +
                                    "TAG_Type, TAG_PAN, TAG_Balance, TAG_SignalCode, TAG_Remark, TransRemark, IsTowing, TowingParentID, "+
                                    "Trans_SignalCode, LC_SignalStatus, SendStatusDC, SendDateTimeDC) VALUES " +
                                    $"({iFareTransID}, '{dtTransDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}', '{TCT_Lib.szPlazaID}', '{TCT_Lib.szPlazaName}', " +
                                    $"{TCT_Lib.iLaneID}, '{TCT_Lib.szLaneName}', {(int)TCT_Lib.eCurLaneType}, '{TCT_Lib.GetLaneTypeName(TCT_Lib.eCurLaneType)}', " +
                                    $"{(int)xLaneMode}, '{TCT_Lib.GetLaneModeName(xLaneMode)}', " +
                                    $"{(int)TCT_Lib.eLaneDirection}, '{TCT_Lib.GetLaneDirection(TCT_Lib.eLaneDirection)}', " +
                                    //$"{TCT_Lib.iStaffShiftID}, '{TCT_Lib.szStaffID}', '{TCT_Lib.szStaffCode}', '{TCT_Lib.szStaffName}', '{TCT_Lib.dtStaffStartShift.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}', " +
                                    $"{TCT_Lib.iStaffShiftID}, '{TCT_Lib.szStaffID}', '{TCT_Lib.szStaffCode}', '{TCT_Lib.szStaffName}', '{TCT_Lib.dtStaffStartShift.ToString("yyyy-MM-dd HH:mm:ss", InvC)}', " +
                                    //$"{TCT_Lib.iSupervisorShiftID}, '{TCT_Lib.szSupervisorID}', '{TCT_Lib.szSupervisorCode}', '{TCT_Lib.szSupervisorName}', '{TCT_Lib.dtSupervisorStartShift.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}', " +
                                    $"{TCT_Lib.iSupervisorShiftID}, '{TCT_Lib.szSupervisorID}', '{TCT_Lib.szSupervisorCode}', '{TCT_Lib.szSupervisorName}', '{TCT_Lib.dtSupervisorStartShift.ToString("yyyy-MM-dd HH:mm:ss", InvC)}', " +
                                    $"{TCT_Lib.iJobNo}, {seqNo}, {iTC_Class}, " +
                                    $"{iAvc_Class}, {iAvc_Axle}, {iAvc_Wheel}, {iAvc_DualWheel}, 0, " +
                                    $"{fFareAmount}, {iPayType}, '{szTAG_Type}', {fFareAmount}, " +
                                    $"'{szPassageTime}', '', '', 0, " +
                                    $"{iTagType}, '{szTAG_PAN}',{fTAG_Balance}, '{szSignalCode}', 'ETC', " +
                                    $"'{szTransRemark}', 0, 0, '', '', 1, '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}'" +
                                    $")";
                    Execute(szQuery);
                    iID = iFareTransID;
                    System.Threading.Interlocked.Increment(ref iFareTransID);

                    return iID;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error WriteLaneTransaction_ETC_CompliedPassage : " + err.Message);
                LogInfo.WriteLog("- Error WriteLaneTransaction_ETC_CompliedPassage : " + err.Message+"\r\n"+err.StackTrace);

                return 0;
            }
        }

        public long WriteLaneTransaction_ETC_AutoSignalCode(string szSignalCode, string szTransRemark)
        {
            long iID = 0;
            var InvC = CultureInfo.InvariantCulture;
            try
            {
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    int iTC_Class           = 0;
                    decimal fFareAmount     = 0;

                    int iAvc_Class = 0, iAvc_Wheel = 0, iAvc_Axle = 0, iAvc_DualWheel = 0;
                    string szTAG_PAN = "", szTAG_Type = "";
                    int iTagType        = 0;       // 0=MPass, 1=EasyPass
                    int iPayType        = 0;
                    decimal fTAG_Balance = 0;

                    long seqNo = 0;
                    LogInfo.WriteLaneEvent($"Write DB CompilePassage : TransID={iFareTransID}, JobNo={TCT_Lib.iJobNo}, nTrx={seqNo}, TC_Class={iTC_Class}, Fare={fFareAmount}, AVC_Class={iAvc_Class}");

                    DateTime dtTransDateTime    = DateTime.Now;
                    string szPassageTime        = dtTransDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC);

                    //-----------------------------------------------------------------
                    LogInfo.WriteLaneEvent($"ETC AutoSignalCode : SeqNo={seqNo}, SignalCode={szSignalCode}, PAN={szTAG_PAN}, PAN_Type={szTAG_Type}, AVC_Class={iAvc_Class}, Fare={fFareAmount}, TransID={iFareTransID}, JobNo={TCT_Lib.iJobNo}, nTrx={seqNo}");

                    LaneMode xLaneMode = TCT_Lib.eCurLaneMode;
                    if (TCT_Lib.bMaintenanceMode && TCT_Lib.eCurLaneMode == LaneMode.HPMC)
                        xLaneMode = LaneMode.MAINTENANCE;
                    //--------
                    string szQuery = "INSERT INTO LaneTransaction (TransactionID, TransactionDateTime, PlazaID, PlazaName, " +
                                    "LaneID, LaneName, LaneType, LaneTypeName, LaneMode, LaneModeName, LaneDirection, LaneDirectionName, " +
                                    "StaffShiftID, StaffID, StaffCode, StaffName, StaffStartShiftDateTime, " +
                                    "SupervisorShiftID, SupervisorID, SupervisorCode, SupervisorName, SupervisorStartShiftDateTime, JobNo, nTrx, " +
                                    "TC_Class, AVC_Class, AVC_Axle, AVC_Wheel, AVC_DualWheel, AVC_Direction, FareAmount, " +
                                    "PayType, PayTypeName, PayAmount, PayDateTime, PayRemark, ReceiptNo, EMV_DetailID, " +
                                    "TAG_Type, TAG_PAN, TAG_Balance, TAG_SignalCode, TAG_Remark, TransRemark, IsTowing, TowingParentID, "+
                                    "Trans_SignalCode, LC_SignalStatus, SendStatusDC, SendDateTimeDC) VALUES " +
                                    $"({iFareTransID}, '{dtTransDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}', '{TCT_Lib.szPlazaID}', '{TCT_Lib.szPlazaName}', " +
                                    $"{TCT_Lib.iLaneID}, '{TCT_Lib.szLaneName}', {(int)TCT_Lib.eCurLaneType}, '{TCT_Lib.GetLaneTypeName(TCT_Lib.eCurLaneType)}', " +
                                    $"{(int)xLaneMode}, '{TCT_Lib.GetLaneModeName(xLaneMode)}', " +
                                    $"{(int)TCT_Lib.eLaneDirection}, '{TCT_Lib.GetLaneDirection(TCT_Lib.eLaneDirection)}', " +
                                    //$"{TCT_Lib.iStaffShiftID}, '{TCT_Lib.szStaffID}', '{TCT_Lib.szStaffCode}', '{TCT_Lib.szStaffName}', '{TCT_Lib.dtStaffStartShift.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}', " +
                                    $"{TCT_Lib.iStaffShiftID}, '{TCT_Lib.szStaffID}', '{TCT_Lib.szStaffCode}', '{TCT_Lib.szStaffName}', '{TCT_Lib.dtStaffStartShift.ToString("yyyy-MM-dd HH:mm:ss", InvC)}', " +
                                    //$"{TCT_Lib.iSupervisorShiftID}, '{TCT_Lib.szSupervisorID}', '{TCT_Lib.szSupervisorCode}', '{TCT_Lib.szSupervisorName}', '{TCT_Lib.dtSupervisorStartShift.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}', " +
                                    $"{TCT_Lib.iSupervisorShiftID}, '{TCT_Lib.szSupervisorID}', '{TCT_Lib.szSupervisorCode}', '{TCT_Lib.szSupervisorName}', '{TCT_Lib.dtSupervisorStartShift.ToString("yyyy-MM-dd HH:mm:ss", InvC)}', " +
                                    $"{TCT_Lib.iJobNo}, {seqNo}, {iTC_Class}, " +
                                    $"{iAvc_Class}, {iAvc_Axle}, {iAvc_Wheel}, {iAvc_DualWheel}, 0, " +
                                    $"{fFareAmount}, {iPayType}, '{szTAG_Type}', {fFareAmount}, " +
                                    $"'{szPassageTime}', '', '', 0, " +
                                    $"{iTagType}, '{szTAG_PAN}',{fTAG_Balance}, '{szSignalCode}', 'ETC', " +
                                    $"'{szTransRemark}', 0, 0, '', '', 1, '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}'" +
                                    $")";
                    Execute(szQuery);
                    iID = iFareTransID;
                    System.Threading.Interlocked.Increment(ref iFareTransID);

                    return iID;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error WriteLaneTransaction_ETC_AutoSignalCode : " + err.Message);
                LogInfo.WriteLog("- Error WriteLaneTransaction_ETC_AutoSignalCode : " + err.Message);

                return 0;
            }
        }

        //==============================================================================
        private long GetSalesCouponTransactionID()
        {
            try
            {
                string szQuery  = "SELECT MAX(TransactionID) FROM SalesCouponTransaction";
                DataTable dt    = List(szQuery);
                return dt.Rows[0][0] == DBNull.Value ? 1 : long.Parse(dt.Rows[0][0].ToString()) + 1;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error GetSalesCouponTransactionID : " + err.Message);
                LogInfo.WriteLog("- Error GetSalesCouponTransactionID : " + err.Message);
                return 0;
            }
        }

        public long WriteSalesCouponTransaction(int iTotalCoupon_Type1, decimal fCouponPrice_Type1, List<string> szSelCouponType1, string szCouponType1,
                                                int iTotalCoupon_Type2, decimal fCouponPrice_Type2, List<string> szSelCouponType2, string szCouponType2,
                                                int iPayType, objCreditCardInfo xCreditCard = null)
        {
            long iID                = 0;
            var InvC                = CultureInfo.InvariantCulture;
            string szPayTypeName    = "";
            if (iPayType == 0)
                szPayTypeName = "CASH";
            else if (iPayType == 1)
                szPayTypeName = "EMV";
            else if (iPayType == 2)
                szPayTypeName = "QR CODE";

            try
            {
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    //--- Insert Emv CreditCardInfo
                    long iEmvDetailID = 0;
                    if (xCreditCard != null)
                        iEmvDetailID = InsertEmvDetailData(iPayType == 1 ? 0 : 1, xCreditCard);

                    //--- Insert SalesCoupon Transaction
                    string szQuery = "INSERT INTO SalesCouponTransaction (TransactionID, TransactionDateTime, PlazaID, PlazaName, " +
                                    "LaneID, LaneName, LaneType, LaneTypeName, LaneMode, LaneModeName, LaneDirection, LaneDirectionName, " +
                                    "StaffShiftID, StaffID, StaffCode, StaffName, StaffStartShiftDateTime, "+
                                    "SupervisorShiftID, SupervisorID, SupervisorCode, SupervisorName, SupervisorStartShiftDateTime, " +
                                    "CouponLotID, CouponLotDateTime, "+
                                    "TotalCouponType1, CouponPriceType1, TotalAmountCouponType1, " +
                                    "TotalCouponType2, CouponPriceType2, TotalAmountCouponType2, " +
                                    "PayType, PayTypeName, PaySalesAmount, PayDateTime, PayRemark, "+
                                    "EmvDetailID, ReceiptNo, TransRemark, UpdateDateTime) VALUES " +
                                    $"({iSalesCouponTransID}, '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}', '{TCT_Lib.szPlazaID}', '{TCT_Lib.szPlazaName}', " +
                                    $"{TCT_Lib.iLaneID}, '{TCT_Lib.szLaneName}', {(int)TCT_Lib.eCurLaneType}, '{TCT_Lib.GetLaneTypeName(TCT_Lib.eCurLaneType)}', " +
                                    $"{(int)TCT_Lib.eCurLaneMode}, '{TCT_Lib.GetLaneModeName(TCT_Lib.eCurLaneMode)}', " +
                                    $"{(int)TCT_Lib.eLaneDirection}, '{TCT_Lib.GetLaneDirection(TCT_Lib.eLaneDirection)}', " +
                                    //$"{TCT_Lib.iStaffShiftID}, '{TCT_Lib.szStaffID}', '{TCT_Lib.szStaffCode}', '{TCT_Lib.szStaffName}', '{TCT_Lib.dtStaffStartShift.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}'," +
                                    $"{TCT_Lib.iStaffShiftID}, '{TCT_Lib.szStaffID}', '{TCT_Lib.szStaffCode}', '{TCT_Lib.szStaffName}', '{TCT_Lib.dtStaffStartShift.ToString("yyyy-MM-dd HH:mm:ss", InvC)}'," +
                                    //$"{TCT_Lib.iSupervisorShiftID}, '{TCT_Lib.szSupervisorID}', '{TCT_Lib.szSupervisorCode}', '{TCT_Lib.szSupervisorName}', '{TCT_Lib.dtSupervisorStartShift.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}'," +
                                    $"{TCT_Lib.iSupervisorShiftID}, '{TCT_Lib.szSupervisorID}', '{TCT_Lib.szSupervisorCode}', '{TCT_Lib.szSupervisorName}', '{TCT_Lib.dtSupervisorStartShift.ToString("yyyy-MM-dd HH:mm:ss", InvC)}'," +
                                    $"0, '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}', " +
                                    $"{iTotalCoupon_Type1}, {fCouponPrice_Type1}, {iTotalCoupon_Type1 * fCouponPrice_Type1}, " +
                                    $"{iTotalCoupon_Type2}, {fCouponPrice_Type2}, {iTotalCoupon_Type2 * fCouponPrice_Type2}, " +
                                    $"{iPayType}, '{szPayTypeName}', {(iTotalCoupon_Type1 * fCouponPrice_Type1) +(iTotalCoupon_Type2 * fCouponPrice_Type2)}, " +
                                    $"'{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}', '', {iEmvDetailID}, '{TCT_Lib.szCurSalesCouponReceiptNo}', " +
                                    $"'', '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}' " +
                                    $")";
                    Execute(szQuery);
                    iID = iSalesCouponTransID;

                    //--- Insert SalesCouponDetail
                    string szFmt = "INSERT INTO SalesCouponDetail (TransactionID, CouponPK, SerialNo, CouponType, CouponPrice, UpdateDateTime) " +
                                    "VALUES ({0}, {1}, '{2}', {3}, {4}, '{5}')";

                    string szErrMsg = "";
                    foreach (string szCouponNo in szSelCouponType1.ToList())
                    {
                        int iCouponPK   = UpdateSoldCoupon(szCouponNo, iSalesCouponTransID);
                        szQuery         = string.Format(szFmt, iSalesCouponTransID, iCouponPK, szCouponNo, 1, fCouponPrice_Type1, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                        Execute(szQuery);
                        WebService_SalesCoupons.CallWebService_SoldCoupon(szCouponType1, szCouponNo, fCouponPrice_Type1, DateTime.Now, iPayType, szPayTypeName, xCreditCard, ref szErrMsg);
                    }
                    foreach (string szCouponNo in szSelCouponType2.ToList())
                    {
                        int iCouponPK   = UpdateSoldCoupon(szCouponNo, iSalesCouponTransID);
                        szQuery         = string.Format(szFmt, iSalesCouponTransID, iCouponPK, szCouponNo, 2, fCouponPrice_Type2, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                        Execute(szQuery);
                        WebService_SalesCoupons.CallWebService_SoldCoupon(szCouponType2, szCouponNo, fCouponPrice_Type2, DateTime.Now, iPayType, szPayTypeName, xCreditCard, ref szErrMsg);
                    }

                    System.Threading.Interlocked.Increment(ref iSalesCouponTransID);

                    return iID;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error WriteSalesCouponTransaction : " + err.Message);
                LogInfo.WriteLog("- Error WriteSalesCouponTransaction : " + err.Message);

                return 0;
            }
        }

        private int UpdateSoldCoupon(string szSerialNo, long iSaleTransID)
        {
            try
            {
                var InvC = CultureInfo.InvariantCulture;
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    string szQuery = "SELECT CouponPK FROM SalesCouponList " +
                                        $"WHERE SerialNo='{szSerialNo}'";
                    LogInfo.WriteLog("Query = " + szQuery);
                    DataTable dt    = List(szQuery);
                    int iCouponPK   = int.Parse(dt.Rows[0]["CouponPK"].ToString());

                    szQuery = $"UPDATE SalesCouponList SET CouponStatus=1, SalesTransID={iSaleTransID}, " +
                                $"SalesDateTime='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}' " +
                                $"WHERE CouponPK={iCouponPK}";
                    LogInfo.WriteLog("Query = " + szQuery);
                    Execute(szQuery);

                    return iCouponPK;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error UpdateSoldCoupon : " + err.Message);
                LogInfo.WriteLog("- Error UpdateSoldCoupon : " + err.Message);

                return 0;
            }
        }


        //=============================================================================
        //--- For Convoy Process
        public long InsertEmvDetailData(int iEmvType, objCreditCardInfo xCreditCard)
        {
            try
            {
                var InvC    = CultureInfo.InvariantCulture;
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    long iEmvDetailID = GetMaxEmvDetailID();

                    xCreditCard.szResponseText      = xCreditCard.szResponseText ?? "";
                    xCreditCard.szApprovalCode      = xCreditCard.szApprovalCode ?? "";
                    xCreditCard.szTransactionDate   = xCreditCard.szTransactionDate ?? "";
                    xCreditCard.szTransactionTime   = xCreditCard.szTransactionTime ?? "";
                    xCreditCard.szTerminalID        = xCreditCard.szTerminalID ?? "";
                    xCreditCard.szCardNo            = xCreditCard.szCardNo ?? "";
                    xCreditCard.szAmount            = xCreditCard.szAmount ?? "";
                    xCreditCard.szRef1              = xCreditCard.szRef1 ?? "";
                    xCreditCard.szRef2              = xCreditCard.szRef2 ?? "";
                    xCreditCard.szRef3              = xCreditCard.szRef3 ?? "";

                    string szQuery = "INSERT INTO EmvDetail (EmvDetailID, EmvType, RequestText, ResponseText, " +
                                        "ResponseMsg, ResponseApproveCode, ResponseDate, ResponseTime, ResponseTerminalID, " +
                                        "ResponseCardNo, ResponseAmount, ResponseRef1, ResponseRef2, ResponseRef3, UpdateDate) VALUES " +
                                        $"({iEmvDetailID}, {iEmvType}, '', '{xCreditCard.szResponseText.Replace("'", "''")}', "+
                                        $"'', '{xCreditCard.szApprovalCode}', '{xCreditCard.szTransactionDate}', "+
                                        $"'{xCreditCard.szTransactionTime}', '{xCreditCard.szTerminalID}', '{xCreditCard.szCardNo}', "+
                                        $"'{xCreditCard.szAmount}', '{xCreditCard.szRef1}', '{xCreditCard.szRef2}', '{xCreditCard.szRef3}', " +
                                        $"'{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}')";
                    LogInfo.WriteLog("+ Insert EmvDetail #2: " + szQuery);
                    Execute(szQuery);

                    return iEmvDetailID;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error InsertEmvDetailData : " + err.Message);
                LogInfo.WriteLog("- Error InsertEmvDetailData : " + err.Message);

                return 0;
            }
        }

        public int WriteEdcDetail(int iPayType, bool bTowing=false)
        {
            try
            {
                int iEmvDetailID    = 0;
                var InvC            = CultureInfo.InvariantCulture;
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    //TCT_Lib.iEmvDetailID    = (int) GetMaxEmvDetailID();
                    iEmvDetailID        = (int)GetMaxEmvDetailID();
                    string szQuery      = "";
                    if (!bTowing)
                    {
                        var edcInfo = (iPayType == (int)PayType.EMV) ? TCT_Lib.xCurEMV : TCT_Lib.xCurQR;
                        /*
                                                EMV_Info edcInfo = null;
                                                if (iPayType == (int)PayType.EMV)
                                                    edcInfo = TCT_Lib.xTmpEMV ?? TCT_Lib.xCurEMV;
                                                else
                                                    edcInfo = TCT_Lib.xTmpQR ?? TCT_Lib.xCurQR;
                        */

                        edcInfo.requestText     = edcInfo.requestText ?? "";
                        edcInfo.responseText    = edcInfo.responseText ?? "";
                        edcInfo.responseMessage = edcInfo.responseMessage ?? "";
                        edcInfo.respApproveCode = edcInfo.respApproveCode ?? "";
                        edcInfo.respDate        = edcInfo.respDate ?? "";
                        edcInfo.respTime        = edcInfo.respTime ?? "";
                        edcInfo.respTerminalId  = edcInfo.respTerminalId ?? "";
                        edcInfo.respCardNo      = edcInfo.respCardNo ?? "";
                        edcInfo.respAmount      = edcInfo.respAmount ?? "";
                        edcInfo.respRef1        = edcInfo.respRef1 ?? "";
                        edcInfo.respRef2        = edcInfo.respRef2 ?? "";
                        edcInfo.respRef3        = edcInfo.respRef3 ?? "";
                        //-------------------------------------------------------------
                        szQuery     = "INSERT INTO EmvDetail (EmvDetailID, EmvType, RequestText, ResponseText, " +
                                        "ResponseMsg, ResponseApproveCode, ResponseDate, ResponseTime, ResponseTerminalID, " +
                                        "ResponseCardNo, ResponseAmount, ResponseRef1, ResponseRef2, ResponseRef3, UpdateDate) VALUES " +
                                        $"({iEmvDetailID}, 0, '{edcInfo.requestText.Replace("'", "''")}', '{edcInfo.responseText.Replace("'", "''")}', " +
                                        $"'{edcInfo.responseMessage.Replace("'", "''")}', '{edcInfo.respApproveCode}', '{edcInfo.respDate}', " +
                                        $"'{edcInfo.respTime}', '{edcInfo.respTerminalId}', '{edcInfo.respCardNo}', " +
                                        $"'{edcInfo.respAmount}', '{edcInfo.respRef1}', '{edcInfo.respRef2}', '{edcInfo.respRef3}', " +
                                        $"'{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}')";
                    }
                    else
                    {
                        var edcInfo = (iPayType == (int)PayType.EMV) ? TCT_Lib.xTowingEMV : TCT_Lib.xTowingQR;
/*
                        EMV_Info edcInfo = null;
                        if (iPayType == (int)PayType.EMV)
                            edcInfo = TCT_Lib.xTmpTowingEMV ?? TCT_Lib.xTowingEMV;
                        else
                            edcInfo = TCT_Lib.xTmpTowingQR ?? TCT_Lib.xTowingQR;
*/                                                       
                        //-------------------------------------------------------------
                        edcInfo.requestText     = edcInfo.requestText ?? "";
                        edcInfo.responseText    = edcInfo.responseText ?? "";
                        edcInfo.responseMessage = edcInfo.responseMessage ?? "";
                        edcInfo.respApproveCode = edcInfo.respApproveCode ?? "";
                        edcInfo.respDate        = edcInfo.respDate ?? "";
                        edcInfo.respTime        = edcInfo.respTime ?? "";
                        edcInfo.respTerminalId  = edcInfo.respTerminalId ?? "";
                        edcInfo.respCardNo      = edcInfo.respCardNo ?? "";
                        edcInfo.respAmount      = edcInfo.respAmount ?? "";
                        edcInfo.respRef1        = edcInfo.respRef1 ?? "";
                        edcInfo.respRef2        = edcInfo.respRef2 ?? "";
                        edcInfo.respRef3        = edcInfo.respRef3 ?? "";
                        szQuery = "INSERT INTO EmvDetail (EmvDetailID, EmvType, RequestText, ResponseText, " +
                                            "ResponseMsg, ResponseApproveCode, ResponseDate, ResponseTime, ResponseTerminalID, " +
                                            "ResponseCardNo, ResponseAmount, ResponseRef1, ResponseRef2, ResponseRef3, UpdateDate) VALUES " +
                                            $"({iEmvDetailID}, 0, '{edcInfo.requestText.Replace("'", "''")}', '{edcInfo.responseText.Replace("'", "''")}', " +
                                            $"'{edcInfo.responseMessage.Replace("'", "''")}', '{edcInfo.respApproveCode}', '{edcInfo.respDate}', " +
                                            $"'{edcInfo.respTime}', '{edcInfo.respTerminalId}', '{edcInfo.respCardNo}', " +
                                            $"'{edcInfo.respAmount}', '{edcInfo.respRef1}', '{edcInfo.respRef2}', '{edcInfo.respRef3}', " +
                                            $"'{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}')";
                    }
                    LogInfo.WriteLog("+ Insert EmvDetail #1 : " + szQuery);
                    LogInfo.WriteLogEDC("+ Insert EmvDetail #1 : " + szQuery);
                    Execute(szQuery);

                    //--------------------------------------------------------------
/*
                    if (!bTowing)
                    {
                        if (iPayType == (int)PayType.EMV)
                        {
                            TCT_Lib.xTmpEMV = null;
                            if (TCT_Lib.xCurEMV != null)
                            {
                                TCT_Lib.xTmpEMV = TCT_Lib.xCurEMV;
                                TCT_Lib.xCurEMV = null;
                            }
                        }
                        else
                        {
                            TCT_Lib.xTmpQR = null;
                            if (TCT_Lib.xCurQR != null)
                            {
                                TCT_Lib.xTmpQR = TCT_Lib.xCurQR;
                                TCT_Lib.xCurQR = null;
                            }
                        }
                    }
                    else
                    {
                        if (iPayType == (int)PayType.EMV)
                        {
                            TCT_Lib.xTmpTowingEMV = null;
                            if (TCT_Lib.xTowingEMV != null)
                            {
                                TCT_Lib.xTmpTowingEMV   = TCT_Lib.xTowingEMV;
                                TCT_Lib.xTowingEMV      = null;
                            }
                        }
                        else
                        {
                            TCT_Lib.xTmpTowingQR = null;
                            if (TCT_Lib.xTowingQR != null)
                            {
                                TCT_Lib.xTmpTowingQR    = TCT_Lib.xTowingQR;
                                TCT_Lib.xTowingQR       = null;
                            }
                        }
                    }
*/                    
                }

                return iEmvDetailID;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error WriteEmvDetail : " + err.Message);
                LogInfo.WriteLog("- Error WriteEdcDetail : " + err.Message);
                LogInfo.WriteLogEDC("- Error WriteEdcDetail : " + err.Message);
                return 0;
            }
        }

        //==================================================================
        private long GetCurEmvDetailID()
        {
            try
            {
                string szQuery  = "SELECT MAX(EmvDetailID) FROM EmvDetail";
                DataTable dt    = List(szQuery);
                return dt.Rows[0][0] == DBNull.Value ? 0 : long.Parse(dt.Rows[0][0].ToString());
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error GetCurEmvDetailID : " + err.Message);
                LogInfo.WriteLog("- Error GetCurEmvDetailID : " + err.Message);
                return 0;
            }
        }

        private long GetMaxEmvDetailID()
        {
            try
            {
                string szQuery  = "SELECT MAX(EmvDetailID) FROM EmvDetail";
                DataTable dt    = List(szQuery);
                return dt.Rows[0][0] == DBNull.Value ? 1 : long.Parse(dt.Rows[0][0].ToString()) + 1;
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error GetMaxEmvDetailID : " + err.Message);
                LogInfo.WriteLog("- Error GetMaxEmvDetailID : " + err.Message);
                return 0;
            }
        }

        public int GetMaxRunningReceiptNo()
        {
            int iYear   = DateTime.Today.Year;
            int iMonth  = DateTime.Today.Month;

            string szQuery = "SELECT MAX(MaxReceiptID) FROM MaxReceiptNo " +
                            $"WHERE ReceiptType=0 AND ReceiptYear={iYear} AND ReceiptMonth={iMonth}";
            DataTable dt    = List(szQuery);
            return dt.Rows[0][0] == DBNull.Value ? 0 : int.Parse(dt.Rows[0][0].ToString());
        }

        public void SetMaxRunningReceiptNo(int iMaxReceiptID)
        {
            int iYear   = DateTime.Today.Year;
            int iMonth  = DateTime.Today.Month;

            string szQuery = "";
            if (iMaxReceiptID == 0)
            {
                szQuery = "DELETE FROM MaxReceiptNo "+
                            $"WHERE ReceiptType=0 AND ReceiptYear={iYear} AND ReceiptMonth={iMonth}";
            }
            else
            {
                szQuery = $"UPDATE MaxReceiptNo SET MaxReceiptID={iMaxReceiptID} "+
                            $"WHERE ReceiptType=0 AND ReceiptYear={iYear} AND ReceiptMonth={iMonth}";
            }
            Execute(szQuery);
        }

        public string GetRunningReceiptNo(PrintReceiptType eReceiptType)
        {
            int iYear   = DateTime.Today.Year;
            int iMonth  = DateTime.Today.Month;

            string szQuery = "SELECT MAX(MaxReceiptID) FROM MaxReceiptNo " +
                            $"WHERE ReceiptType={(int)eReceiptType} AND ReceiptYear={iYear} AND ReceiptMonth={iMonth}";
            DataTable dt    = List(szQuery);
            int iMaxReceipt = dt.Rows[0][0] == DBNull.Value ? 0 : int.Parse(dt.Rows[0][0].ToString());

            if (iMaxReceipt == 0)
            {
                //--- First row or new month
                szQuery = "INSERT INTO MaxReceiptNo (ReceiptType, ReceiptYear, ReceiptMonth, MaxReceiptID) " +
                            $"VALUES ({(int)eReceiptType}, {iYear}, {iMonth}, 1)";
            }
            else
            {
                szQuery = "UPDATE MaxReceiptNo SET MaxReceiptID=MaxReceiptID+1 "+
                            $"WHERE ReceiptType={(int)eReceiptType} AND ReceiptYear={iYear} AND ReceiptMonth={iMonth}";
            }
            Execute(szQuery);

            iMaxReceipt += 1;
            string szRunningNo = (eReceiptType == PrintReceiptType.FareType)
                                    ? TCT_Lib.szLaneName + "-" + iYear.ToString("0000") + iMonth.ToString("00") + iMaxReceipt.ToString("000000")
                                    : "CP-" + TCT_Lib.szLaneName + "-" + iYear.ToString("0000") + iMonth.ToString("00") + iMaxReceipt.ToString("000000");

            return szRunningNo;
        }

        public int GetStartJobNo_V0()
        {
            string szQuery  = "SELECT MAX(StaffStartShiftDateTime) AS LastShiftDateTime, JobNo FROM StaffShift";
            DataTable dt    = List(szQuery);
            if (dt.Rows[0]["LastShiftDateTime"] == DBNull.Value)
                return 1;

            //--- > 06:00 is new reset JobNo=1
            DateTime lastShiftDate = Convert.ToDateTime(dt.Rows[0]["LastShiftDateTime"].ToString());
            if (lastShiftDate.Hour < 6)
            {
                if (DateTime.Now.Hour >= 6)
                    return 1;
                else
                    return int.Parse(dt.Rows[0]["JobNo"].ToString()) + 1;
            }
            else
                return int.Parse(dt.Rows[0]["JobNo"].ToString()) + 1;      
        }

        public int GetStartJobNo()
        {
            return TCT_Lib.bMaintenanceMode ? GetStartJobNo_Maintenance() : GetStartJobNo_Normal();
        }

        private int GetStartJobNo_Normal()
        {
            //string szQuery  = "SELECT MAX(StaffStartShiftDateTime) AS LastShiftDateTime, JobNo " +
            string szQuery = "SELECT MAX(StaffShiftID), StaffStartShiftDateTime AS LastShiftDateTime, JobNo " +
                                "FROM StaffShift " +
                                "WHERE JobNo>0 AND JobNo<900";
            DataTable dt    = List(szQuery);
            if (dt.Rows[0]["LastShiftDateTime"] == DBNull.Value)
                return 1;

            //--- > 00:00 is new reset JobNo=1
            DateTime lastShiftDate = Convert.ToDateTime(dt.Rows[0]["LastShiftDateTime"].ToString());
            return (lastShiftDate.Date == DateTime.Today) ? int.Parse(dt.Rows[0]["JobNo"].ToString()) + 1 : 1;
        }

        private int GetStartJobNo_Maintenance()
        {
            string szQuery = "SELECT MAX(StaffStartShiftDateTime) AS LastShiftDateTime, JobNo " +
                                "FROM StaffShift " +
                                "WHERE JobNo > 900";
            DataTable dt    = List(szQuery);
            if (dt.Rows[0]["LastShiftDateTime"] == DBNull.Value)
                return 900 + 1;

            //--- > 00:00 is new reset JobNo=1
            DateTime lastShiftDate = Convert.ToDateTime(dt.Rows[0]["LastShiftDateTime"].ToString());
            return (lastShiftDate.Date == DateTime.Today) ? int.Parse(dt.Rows[0]["JobNo"].ToString()) + 1 : 900 + 1;
        }

        //--------------------------------------------------------------------------------------------------------------------
        private int GetStartJobNo_Normal_V0()
        {
            string szQuery  = "SELECT MAX(StaffStartShiftDateTime) AS LastShiftDateTime, JobNo " +
                                "FROM StaffShift " +
                                "WHERE JobNo>0 AND JobNo<900";
            DataTable dt    = List(szQuery);
            if (dt.Rows[0]["LastShiftDateTime"] == DBNull.Value)
                return 1;

            //--- > 06:00 is new reset JobNo=1
            DateTime lastShiftDate = Convert.ToDateTime(dt.Rows[0]["LastShiftDateTime"].ToString());
            if (lastShiftDate.Date == DateTime.Today)       // Same day
            {
                if ((lastShiftDate.Hour < 6) && (DateTime.Now.Hour >= 6))
                    return 1;
                else
                    return int.Parse(dt.Rows[0]["JobNo"].ToString()) + 1;
            }
            else
            {
                //if (lastShiftDate.Day == DateTime.Now.Day - 1)  // Yesterday
                if (lastShiftDate.Date == DateTime.Today.AddDays(-1))  // Yesterday
                {
                    if (DateTime.Now.Hour >= 6)
                        return 1;
                    else
                    {
                        if (lastShiftDate.Hour < 6)
                            return 1;
                        else
                            return int.Parse(dt.Rows[0]["JobNo"].ToString()) + 1;
                    }
                }
                else
                {
                    //--- More than 1 day
                    return 1;
                }
            }
        }
        //-----------------------------------------------------------------------------------------------------------

        private int GetStartJobNo_Maintenance_V0()
        {
            string szQuery = "SELECT MAX(StaffStartShiftDateTime) AS LastShiftDateTime, JobNo " +
                                "FROM StaffShift " +
                                "WHERE JobNo > 900";
            DataTable dt    = List(szQuery);
            if (dt.Rows[0]["LastShiftDateTime"] == DBNull.Value)
                return 900 + 1;

            //--- > 06:00 is new reset JobNo=1
            DateTime lastShiftDate = Convert.ToDateTime(dt.Rows[0]["LastShiftDateTime"].ToString());
            if (lastShiftDate.Date == DateTime.Today)       // Same day
            {
                if ((lastShiftDate.Hour < 6) && (DateTime.Now.Hour >= 6))
                    return 900 + 1;
                else
                    return int.Parse(dt.Rows[0]["JobNo"].ToString()) + 1;
            }
            else
            {
                //if (lastShiftDate.Day == DateTime.Now.Day - 1)  // Yesterday
                if (lastShiftDate.Date == DateTime.Today.AddDays(-1))  // Yesterday
                {
                    if (DateTime.Now.Hour >= 6)
                        return 900 + 1;
                    else
                    {
                        if (lastShiftDate.Hour < 6)
                            return 900 + 1;
                        else
                            return int.Parse(dt.Rows[0]["JobNo"].ToString()) + 1;
                    }
                }
                else
                {
                    //--- More than 1 day
                    return 900 + 1;
                }
            }
        }

        public void GetShiftNotCloseEndShift()
        {
            DateTime dtChkDate = DateTime.Today.AddDays(-10);
            string szQuery = "SELECT * FROM StaffShift " +
                                "WHERE StaffEndShiftDateTime IS null " +
                                $"AND StaffStartShiftDateTime >= '{dtChkDate.ToString(TCT_Lib.szDateFormatDB)}'";
            LogInfo.WriteLog("+ Check not EndShift : " + szQuery);
            DataTable dt = List(szQuery);

            LogInfo.WriteLog("+ Check not End Shift : Found = " + dt.Rows.Count);
        }

        public int ChkAutoEndOfLastJob()
        {
            string szQuery = "SELECT * FROM StaffShift "+
                                "ORDER BY StaffShiftID DESC LIMIT 1";
            DataTable dt = List(szQuery);
            if (dt.Rows.Count == 0)
                return 0;

            return (dt.Rows[0]["StaffEndShiftDateTime"] == DBNull.Value) ?  int.Parse(dt.Rows[0]["StaffShiftID"].ToString()) : 0;
        }

        public void UpdateClearTowing(int iTransID)
        {
            string szQuery = "SELECT Trans_SignalCode FROM LaneTransaction " +
                                $"WHERE TransactionID={iTransID}";
            DataTable dt            = List(szQuery);
            string szSignalCode     = dt.Rows[0][0].ToString();
            List<int> xSignalCode   = szSignalCode.Split(',').Select(int.Parse).ToList();
            xSignalCode[6] = 0;
            szSignalCode = string.Join(",", xSignalCode.Select(n => n.ToString()).ToArray());

            szQuery = $"UPDATE LaneTransaction SET TransRemark='', IsTowing=0, Trans_SignalCode='{szSignalCode}' " +
                        $"WHERE TransactionID={iTransID}";
            Execute(szQuery);
        }

        public void UpdateClearReClass(int iTransID)
        {
            string szQuery = "SELECT Trans_SignalCode FROM LaneTransaction " +
                                $"WHERE TransactionID={iTransID}";
            DataTable dt        = List(szQuery);
            string szSignalCode = dt.Rows[0][0].ToString();
            List<int> xSignalCode = szSignalCode.Split(',').Select(int.Parse).ToList();
            xSignalCode[1]  = 0;
            szSignalCode    = string.Join(",", xSignalCode.Select(n => n.ToString()).ToArray());

            szQuery = $"UPDATE LaneTransaction SET Trans_SignalCode='{szSignalCode}' " +
                        $"WHERE TransactionID={iTransID}";
            Execute(szQuery);
        }


        //==============================================================
        public void WriteNewTransactionStatistic()
        {
            try
            {
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    //--- Create new TransactionStatistic
                    string szQuery = "INSERT INTO TransactionStatistic (StaffShiftID) VALUES " +
                            $"({TCT_Lib.iStaffShiftID})";
                    Execute(szQuery);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error WriteNewTransactionStatistic : " + err.Message);
                LogInfo.WriteLog("- Error WriteNewTransactionStatistic : " + err.Message);
            }
        }

        public int UpdateTransStatistic_TotalSum()
        {
/*
            if (TCT_Lib.eCurLaneMode == LaneMode.CLOSED)
            {
                LogInfo.WriteLog("- Not UpdateTransStatistic : Lane mode is closed.");
                return 0;
            }
*/            

            int iTotalSum = 0;
            try
            {
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    string szQuery = "UPDATE TransactionStatistic SET ShiftTotalSumVehicles=ShiftTotalSumVehicles+1 " +
                                    $"WHERE StaffShiftID={TCT_Lib.iStaffShiftID}";
                    Execute(szQuery);

                    //--------------------------------------------------------------------
                    szQuery = "SELECT * FROM TransactionSummary " +
                                $"WHERE TransactionDate='{DateTime.Today.ToString(TCT_Lib.szDateFormatDB)}'";
                    DataTable dt = List(szQuery);
                    if (dt.Rows.Count > 0)
                    {
                        szQuery = "UPDATE TransactionSummary SET TotalSumVehicles=TotalSumVehicles+1, " +
                                        $"UpdateDateTime='{DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB)}' " +
                                        $"WHERE TransactionDate='{DateTime.Today.ToString(TCT_Lib.szDateFormatDB)}'";
                        iTotalSum = int.Parse(dt.Rows[0]["TotalSumVehicles"].ToString()) + 1;
                    }
                    else
                    {
                        szQuery = "INSERT INTO TransactionSummary (TransactionDate, TotalSumVehicles, UpdateDateTime) VALUES " +
                                        $"('{DateTime.Today.ToString(TCT_Lib.szDateFormatDB)}', 1, '{DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB)}')";
                        iTotalSum = 1;
                    }

                    Execute(szQuery);

                    return iTotalSum;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error UpdateTransStatistic_TotalSum : " + err.Message);
                LogInfo.WriteLog("- Error UpdateTransStatistic_TotalSum : " + err.Message);

                return 0;
            }
        }

        public void UpdateTransStatistic_MIS(int iMisType)
        {
            //--- MisType=0 is MIS ClassType, 1= MIS PayType
            try
            {
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    string szQuery = "";
                    if (iMisType == 0)
                        szQuery = "UPDATE TransactionStatistic SET TotalMIS=TotalMIS+1, TotalMIS_ClassType=TotalMIS_ClassType+1 "+
                                    $"WHERE StaffShiftID={TCT_Lib.iStaffShiftID}";
                    else
                        szQuery = "UPDATE TransactionStatistic SET TotalMIS=TotalMIS+1, TotalMIS_ClassType=TotalMIS_PayType+1 " +
                                    $"WHERE StaffShiftID={TCT_Lib.iStaffShiftID}";
                    Execute(szQuery);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error WriteTransStatistic_MIS : " + err.Message);
                LogInfo.WriteLog("- Error WriteTransStatistic_MIS : " + err.Message);
            }
        }

        public void UpdateTransStatistic_Violation()
        {
            if (TCT_Lib.eCurLaneType == LaneType.ETC)
                return;

            try
            {
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    string szQuery = "UPDATE TransactionStatistic SET TotalTransViolation=TotalTransViolation+1 " +
                                    $"WHERE StaffShiftID={TCT_Lib.iStaffShiftID}";
                    Execute(szQuery);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error UpdateTransStatistic_Violation : " + err.Message);
                LogInfo.WriteLog("- Error UpdateTransStatistic_Violation : " + err.Message);
            }
        }

        public void UpdateTransStatistic_Exempt()
        {
/*
            if (TCT_Lib.eCurLaneMode == LaneMode.CLOSED)
            {
                LogInfo.WriteLog("- Not write UpdateTransStatistic_Exempt : Lane mode is closed.");
                return;
            }
*/            

            try
            {
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    string szQuery = "UPDATE TransactionStatistic SET TotalTransExempt=TotalTransExempt+1 " +
                                    $"WHERE StaffShiftID={TCT_Lib.iStaffShiftID}";
                    Execute(szQuery);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error UpdateTransStatistic_Exempt : " + err.Message);
                LogInfo.WriteLog("- Error UpdateTransStatistic_Exempt : " + err.Message);
            }
        }

        public void UpdateTransStatistic_HPMC()
        {
            try
            {
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    string szQuery = "UPDATE TransactionStatistic SET TotalTransHPMC=TotalTransHPMC+1 " +
                                    $"WHERE StaffShiftID={TCT_Lib.iStaffShiftID}";
                    Execute(szQuery);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error UpdateTransStatistic_HPMC : " + err.Message);
                LogInfo.WriteLog("- Error UpdateTransStatistic_HPMC : " + err.Message);
            }
        }

        public void UpdateTransStatistic_DIS()
        {
            try
            {
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    string szQuery = "UPDATE TransactionStatistic SET TotalTransDIS=TotalTransDIS+1 " +
                                    $"WHERE StaffShiftID={TCT_Lib.iStaffShiftID}";
                    Execute(szQuery);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error UpdateTransStatistic_DIS : " + err.Message);
                LogInfo.WriteLog("- Error UpdateTransStatistic_DIS : " + err.Message);
            }   
        }

        public void UpdateTransStatistic_AVC(int iAvcClass)
        {
/*
            if (TCT_Lib.eCurLaneMode == LaneMode.CLOSED)
            {
                LogInfo.WriteLog("- Not UpdateTransStatistic_AVC : Lane mode is closed.");
                return;
            }
*/

            try
            {
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    string szQuery = "";
                    if ((iAvcClass == 1) || (iAvcClass == 2) || (iAvcClass == 5) || (iAvcClass == 255))
                    {
                        string szAvcClass = "TotalAVC_Class" + iAvcClass;
                        szQuery = $"UPDATE TransactionStatistic SET {szAvcClass}={szAvcClass}+1 " +
                                $"WHERE StaffShiftID={TCT_Lib.iStaffShiftID}";
                    }
                    else
                    {
                        szQuery = $"UPDATE TransactionStatistic SET TotalAVC_ClassOther=TotalAVC_ClassOther+1 " +
                                $"WHERE StaffShiftID={TCT_Lib.iStaffShiftID}";
                    }

                    Execute(szQuery);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error UpdateTransStatistic_AVC : " + err.Message);
                LogInfo.WriteLog("- Error UpdateTransStatistic_AVC : " + err.Message);
            }
        }

        public void UpdateTransStatistic_Transaction()
        {
            /*
                        if (TCT_Lib.eCurLaneMode == LaneMode.CLOSED)
                        {
                            LogInfo.WriteLog("- Not UpdateTransStatistic : Lane mode is closed.");
                            return;
                        }
            */

            string szQuery = "";
            try
            {
                using (SqliteCommand command = new SqliteCommand(conn))
                {

                    int iTC_Class       = TCT_Lib.xListLoopCar.Count == 0 ? TCT_Lib.iCurTC_Class : TCT_Lib.xListLoopCar[0].iTC_Class;
                    decimal fFareAmount = TCT_Lib.xListLoopCar.Count == 0 ? TCT_Lib.fCurFareAmount : TCT_Lib.xListLoopCar[0].fFareAmount;
                    var xPayment        = TCT_Lib.xListLoopCar.Count == 0 ? TCT_Lib.xCurPayment : TCT_Lib.xListLoopCar[0].xPayment;

                    if (iTC_Class == (int)VehicleType.CLASS_1)
                    {
                        if (xPayment.payType == (int)PayType.CASH)
                            szQuery = "UPDATE TransactionStatistic SET TotalClass1_TransCash=TotalClass1_TransCash+1, " +
                        	        $"TotalClass1_FareCash=TotalClass1_FareCash+{xPayment.payAmount} " +
                                        $"WHERE StaffShiftID={TCT_Lib.iStaffShiftID}";
                        else if (xPayment.payType == (int)PayType.COUPON)
                            szQuery = "UPDATE TransactionStatistic SET TotalClass1_TransCoupon=TotalClass1_TransCoupon+1, " +
                                    $"TotalClass1_FareCoupon=TotalClass1_FareCoupon+{xPayment.payAmount} " +
                                        $"WHERE StaffShiftID={TCT_Lib.iStaffShiftID}";
                        else if (xPayment.payType == (int)PayType.EMV)
                            szQuery = "UPDATE TransactionStatistic SET TotalClass1_TransEMV=TotalClass1_TransEMV+1, " +
                                    $"TotalClass1_FareEMV=TotalClass1_FareEMV+{xPayment.payAmount} " +
                                        $"WHERE StaffShiftID={TCT_Lib.iStaffShiftID}";
                        else if (xPayment.payType == (int)PayType.QrCode)
                            szQuery = "UPDATE TransactionStatistic SET TotalClass1_TransQrCode=TotalClass1_TransQrCode+1, " +
                                    $"TotalClass1_FareQrCode=TotalClass1_FareQrCode+{xPayment.payAmount} " +
                                        $"WHERE StaffShiftID={TCT_Lib.iStaffShiftID}";
                        else if (xPayment.payType == (int)PayType.DMT_CARD)
                            szQuery = "UPDATE TransactionStatistic SET TotalClass1_TransDMT=TotalClass1_TransDMT+1 " +
                                        $"WHERE StaffShiftID={TCT_Lib.iStaffShiftID}";
                    }
                    else if (iTC_Class == (int)VehicleType.CLASS_2)
                    {
                        if (xPayment.payType == (int)PayType.CASH)
                            szQuery = "UPDATE TransactionStatistic SET TotalClass2_TransCash=TotalClass2_TransCash+1, " +
                                    $"TotalClass2_FareCash=TotalClass2_FareCash+{xPayment.payAmount} " +
                                        $"WHERE StaffShiftID={TCT_Lib.iStaffShiftID}";
                        else if (xPayment.payType == (int)PayType.COUPON)
                            szQuery = "UPDATE TransactionStatistic SET TotalClass2_TransCoupon=TotalClass2_TransCoupon+1, " +
                                    $"TotalClass2_FareCoupon=TotalClass2_FareCoupon+{xPayment.payAmount} " +
                                        $"WHERE StaffShiftID={TCT_Lib.iStaffShiftID}";
                        else if (xPayment.payType == (int)PayType.EMV)
                            szQuery = "UPDATE TransactionStatistic SET TotalClass2_TransEMV=TotalClass2_TransEMV+1, " +
                                    $"TotalClass2_FareEMV=TotalClass2_FareEMV+{xPayment.payAmount} " +
                                        $"WHERE StaffShiftID={TCT_Lib.iStaffShiftID}";
                        else if (xPayment.payType == (int)PayType.QrCode)
                            szQuery = "UPDATE TransactionStatistic SET TotalClass2_TransQrCode=TotalClass2_TransQrCode+1, " +
                                    $"TotalClass2_FareQrCode=TotalClass2_FareQrCode+{xPayment.payAmount} " +
                                        $"WHERE StaffShiftID={TCT_Lib.iStaffShiftID}";
                        else if (xPayment.payType == (int)PayType.DMT_CARD)
                            szQuery = "UPDATE TransactionStatistic SET TotalClass2_TransDMT=TotalClass2_TransDMT+1 " +
                                        $"WHERE StaffShiftID={TCT_Lib.iStaffShiftID}";
                    }
                    else if (iTC_Class == (int)VehicleType.LEFT_EXIT)
                    {
                        szQuery = "UPDATE TransactionStatistic SET TotalTransLeftExit=TotalTransLeftExit+1 " +
                                        $"WHERE StaffShiftID={TCT_Lib.iStaffShiftID}";
                    }

                    if (szQuery != "")
                        Execute(szQuery);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error UpdateTransStatistic_Transaction : " + err.Message);
                LogInfo.WriteLog("- Error UpdateTransStatistic_Transaction : " + err.Message);
                LogInfo.WriteLog("- Error Query " + szQuery);
            }
        }

        public int GetTotalTransSumVehicles()
        {
            try
            {
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    string szQuery = "SELECT TotalSumVehicles FROM TransactionSummary " +
                                        $"WHERE TransactionDate='{DateTime.Today.ToString(TCT_Lib.szDateFormatDB)}'";
                    DataTable dt = List(szQuery);
                    return (dt.Rows.Count > 0) ? int.Parse(dt.Rows[0][0].ToString()) : 0;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error GetTotalTransSumVehicles : " + err.Message);
                LogInfo.WriteLog("- Error GetTotalTransSumVehicles : " + err.Message);
                return -1;
            }
        }

        public object GetCurrentStatistic(int iStaffShiftID)
        {
            try
            {
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    string szQuery = "SELECT * FROM TransactionStatistic " +
                                        $"WHERE StaffShiftID={iStaffShiftID}";
                    DataTable dt = List(szQuery);
                    if (dt.Rows.Count == 0)
                        return null;

                    object objStat = new
                    {
                        StaffShiftID            = iStaffShiftID,
                        StaffName               = TCT_Lib.szStaffName,
                        StartShift              = TCT_Lib.dtStaffStartShift.ToString("yyyy-MM-dd HH:mm"),
                        ShiftTotalSum           = int.Parse(dt.Rows[0]["ShiftTotalSumVehicles"].ToString()),
                        TotalTransViolation     = int.Parse(dt.Rows[0]["TotalTransViolation"].ToString()),
                        TotalTransExempt        = int.Parse(dt.Rows[0]["TotalTransExempt"].ToString()),
                        TotalTransDIS           = int.Parse(dt.Rows[0]["TotalTransDIS"].ToString()),
                        TotalTransTowing        = int.Parse(dt.Rows[0]["TotalTransTowing"].ToString()),
                        TotalTransHPMC          = int.Parse(dt.Rows[0]["TotalTransHPMC"].ToString()),
                        TotalTransLeftExit      = int.Parse(dt.Rows[0]["TotalTransLeftExit"].ToString()),
                        TotalClass1_TransCash   = int.Parse(dt.Rows[0]["TotalClass1_TransCash"].ToString()),
                        TotalClass1_FareCash    = decimal.Parse(dt.Rows[0]["TotalClass1_FareCash"].ToString()),
                        TotalClass1_TransCoupon = int.Parse(dt.Rows[0]["TotalClass1_TransCoupon"].ToString()),
                        TotalClass1_FareCoupon  = decimal.Parse(dt.Rows[0]["TotalClass1_FareCoupon"].ToString()),
                        TotalClass1_TransEMV    = int.Parse(dt.Rows[0]["TotalClass1_TransEMV"].ToString()),
                        TotalClass1_FareEMV     = decimal.Parse(dt.Rows[0]["TotalClass1_FareEMV"].ToString()),
                        TotalClass1_TransQrCode = int.Parse(dt.Rows[0]["TotalClass1_TransQrCode"].ToString()),
                        TotalClass1_FareQrCode  = decimal.Parse(dt.Rows[0]["TotalClass1_FareQrCode"].ToString()),
                        TotalClass1_TransDMT    = int.Parse(dt.Rows[0]["TotalClass1_TransDMT"].ToString()),
                        TotalClass2_TransCash   = int.Parse(dt.Rows[0]["TotalClass2_TransCash"].ToString()),
                        TotalClass2_FareCash    = decimal.Parse(dt.Rows[0]["TotalClass2_FareCash"].ToString()),
                        TotalClass2_TransCoupon = int.Parse(dt.Rows[0]["TotalClass2_TransCoupon"].ToString()),
                        TotalClass2_FareCoupon  = decimal.Parse(dt.Rows[0]["TotalClass2_FareCoupon"].ToString()),
                        TotalClass2_TransEMV    = int.Parse(dt.Rows[0]["TotalClass2_TransEMV"].ToString()),
                        TotalClass2_FareEMV     = decimal.Parse(dt.Rows[0]["TotalClass2_FareEMV"].ToString()),
                        TotalClass2_TransQrCode = int.Parse(dt.Rows[0]["TotalClass2_TransQrCode"].ToString()),
                        TotalClass2_FareQrCode  = decimal.Parse(dt.Rows[0]["TotalClass2_FareQrCode"].ToString()),
                        TotalClass2_TransDMT    = int.Parse(dt.Rows[0]["TotalClass2_TransDMT"].ToString()),
                        TotalAVC_Class1         = int.Parse(dt.Rows[0]["TotalAVC_Class1"].ToString()),
                        TotalAVC_Class2         = int.Parse(dt.Rows[0]["TotalAVC_Class2"].ToString()),
                        TotalAVC_Class5         = int.Parse(dt.Rows[0]["TotalAVC_Class5"].ToString()),
                        TotalAVC_Class255       = int.Parse(dt.Rows[0]["TotalAVC_Class255"].ToString()),
                        TotalAVC_ClassOther     = int.Parse(dt.Rows[0]["TotalAVC_ClassOther"].ToString()),
                        TotalMIS                = int.Parse(dt.Rows[0]["TotalMIS"].ToString()),
                        TotalMIS_ClassType      = int.Parse(dt.Rows[0]["TotalMIS_ClassType"].ToString()),
                        TotalMIS_PayType        = int.Parse(dt.Rows[0]["TotalMIS_PayType"].ToString()),
                    };

                    return objStat;
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error GetCurrentStatistic : " + err.Message);
                LogInfo.WriteLog("- Error GetCurrentStatistic : " + err.Message);
                return null;
            }
        }

        //=========================================================================================
        async public Task<bool> ImportStaffListToDB(JArray staffList, int staffVersion, string timeStamp)
        {
            try
            {
                if (staffList.Count == 0)
                {
                    Console.WriteLine("- Error ImportStaffListToDB : StaffList not found in JArray.");
                    LogInfo.WriteLog("- Error ImportStaffListToDB : StaffList not found in JArray.");
                    return false;
                }

                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    string szQuery = "DELETE FROM Staff_BAK";
                    Execute(szQuery);

                    szQuery = "INSERT INTO Staff_Bak " +
                                "SELECT * FROM Staff";
                    Execute(szQuery);

                    //szQuery = "DELETE FROM Staff";
                    //Execute(szQuery);
                    //-------------------------------------------
                    string szFmtChk = "SELECT * FROM Staff WHERE StaffID='{0}'";
                    string szFmtIns = "INSERT INTO Staff (StaffID, StaffGroupID, StaffCode, StaffName, StaffPassword, " +
                                        "CardSerialNo, StaffTimeStamp, UpdateDateTime) " +
                                        "VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}')";
                    string szFmtUpdate = "UPDATE Staff SET StaffGroupID='{1}', StaffCode='{2}', StaffName='{3}', StaffPassword='{4}', " +
                                        "CardSerialNo='{5}', StaffTimeStamp='{6}', UpdateDateTime='{7}' " +
                                        "WHERE StaffID='{0}'";

                    var InvC = CultureInfo.InvariantCulture;
                    dynamic[] objStaffs = staffList.ToObject<dynamic[]>();
                    foreach (dynamic staff in objStaffs)
                    {
                        string szStaffId = staff["staffId"] ?? "";
                        if (szStaffId != "")
                        {
                            string szStaffName = (staff["title"] ?? "") + " " + (staff["staffFirstName"] ?? "") + " " + (staff["staffFamilyName"] ?? "");
                            szQuery         = string.Format(szFmtChk, szStaffId);
                            DataTable dt    = List(szQuery);
                            string szFmt    = (dt.Rows.Count == 0) ? szFmtIns : szFmtUpdate;
                            szQuery         = string.Format(szFmt, staff["staffId"] ?? "", staff["group"], staff["staffId"] ?? "", szStaffName.Trim().Replace("'", "''"),
                                                staff["password"] ?? "", staff["cardSerialNo"] ?? "", Convert.ToDateTime(timeStamp).ToString(TCT_Lib.szDateTimeFormatDB),
                                                DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB));
                            Execute(szQuery);
                        }
                        await Task.Delay(50);
                    }

/*
                    string szFmt = "INSERT INTO Staff (StaffID, StaffGroupID, StaffCode, StaffName, StaffPassword, " +
                                        "CardSerialNo, StaffTimeStamp, UpdateDateTime) " +
                                        "VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}')";
                    dynamic[] objStaffs = staffList.ToObject<dynamic[]>();
                    foreach(dynamic staff in objStaffs.ToList())
                    {
                        string szStaffName = (staff["title"] ?? "") + " " + (staff["staffFirstName"] ?? "") + " " + (staff["staffFamilyName"] ?? "");
                        szQuery = string.Format(szFmt, staff["staffId"] ?? "", staff["group"], staff["staffId"] ?? "", szStaffName.Trim().Replace("'", "''"),
                                    staff["password"] ?? "", staff["cardSerialNo"] ?? "", Convert.ToDateTime(timeStamp).ToString(TCT_Lib.szDateTimeFormatDB), 
                                    DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB));
                        Execute(szQuery);
                    }
*/                    

                    return true;
                }
            }
            catch (Exception err)
            {

                Console.WriteLine("- Error ImportStaffListToDB : " + err.Message);
                LogInfo.WriteLog("- Error ImportStaffListToDB : " + err.Message);
                return false;
            }
        }

        public bool ImportTariffToDB(JArray tariffList, int tariffVersion, string timeStamp)
        {
            try
            {
                if (tariffList.Count == 0)
                {
                    Console.WriteLine("- Error ImportTariffToDB : tariffList not found in JArray.");
                    LogInfo.WriteLog("- Error ImportTariffToDB : tariffList not found in JArray.");
                    return false;
                }

                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    int iPlazaID = int.Parse(TCT_Lib.szPlazaID);
                    string szQuery = "DELETE FROM Tariff_BAK " +
                                        //$"WHERE PlazaID='{TCT_Lib.szPlazaID}' " +
                                        //$"AND TariffVersion={tariffVersion}";
                                        $"WHERE TariffVersion={tariffVersion}";
                    Execute(szQuery);

/*
                    szQuery = "DELETE FROM Tariff_BAK " +
                                        //$"WHERE PlazaID='{TCT_Lib.szPlazaID}' " +
                                        //$"AND TariffVersion={tariffVersion}";
                                        $"WHERE CAST(PlazaID AS INT)={iPlazaID}";
                    Execute(szQuery);
*/                    

                    /*
                                        szQuery = "INSERT INTO Tariff_Bak " +
                                                    "SELECT * FROM Tariff " +
                                                    $"WHERE PlazaID='{TCT_Lib.szPlazaID}' " +
                                                    $"AND TariffVersion={tariffVersion}";
                                        Execute(szQuery);

                                        szQuery = "DELETE FROM Tariff " +
                                                    $"WHERE PlazaID='{TCT_Lib.szPlazaID}' " +
                                                    $"AND TariffVersion={tariffVersion}";
                                        Execute(szQuery);
                    */
                    //-------------------------------------------
                    string szFmt = "INSERT INTO Tariff_BAK (PlazaID, FromDateTime, ToDateTime, " +
                                        "FareClassType1, FareClassType2, FareClassType3, FareClassType4, FareClassType5, "+
                                        "TariffVersion, TariffTimeStamp, UpdateDateTime) " +
                                        "VALUES ('{0}', '{1}', '{2}', {3}, {4}, {5}, {6}, {7}, {8}, '{9}', '{10}')";
                    dynamic[] objTariff = tariffList.ToObject<dynamic[]>();
                    foreach (dynamic tariff in objTariff.ToList())
                    {
                        string szFromDate = ((string)tariff["ValidFromDate"]).Insert(6, "-").Insert(4, "-");
                        string szFromTime = ((string)tariff["ValidFromTime"]).Insert(2, ":");
                        if (szFromTime == "24:00")
                        {
                            szFromTime = "23:59:59";
                            //szFromDate = Convert.ToDateTime(szFromDate).AddDays(1).ToString("yyyy-MM-dd");
                            //szFromTime = "00:00";
                        }
                        string szFromDateTime = szFromDate + " " + szFromTime;

                        string szToDate = ((string)tariff["ValidToDate"]).Insert(6, "-").Insert(4, "-");
                        string szToTime = ((string)tariff["ValidToTime"]).Insert(2, ":");
                        if (szToTime == "24:00")
                        {
                            //szToDate = Convert.ToDateTime(szToDate).AddDays(1).ToString("yyyy-MM-dd");
                            //szToTime = "00:00";
                            szToTime = "23:59:59";
                        }
                        string szToDateTime = szToDate + " " + szToTime;

                        szQuery = string.Format(szFmt, tariff["ExitPlaza"], szFromDateTime, szToDateTime,
                                    ((string)tariff["Price1"]) == "0" ? "0" : ((string)tariff["Price1"]).Insert(((string)tariff["Price1"]).Length - 2, "."),
                                    ((string)tariff["Price2"]) == "0" ? "0" : ((string)tariff["Price2"]).Insert(((string)tariff["Price2"]).Length - 2, "."),
                                    ((string)tariff["Price3"]) == "0" ? "0" : ((string)tariff["Price3"]).Insert(((string)tariff["Price3"]).Length - 2, "."),
                                    ((string)tariff["Price4"]) == "0" ? "0" : ((string)tariff["Price4"]).Insert(((string)tariff["Price4"]).Length - 2, "."),
                                    ((string)tariff["Price5"]) == "0" ? "0" : ((string)tariff["Price5"]).Insert(((string)tariff["Price5"]).Length - 2, "."),
                                    tariffVersion, Convert.ToDateTime(timeStamp).ToString(TCT_Lib.szDateTimeFormatDB),
                                    DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB));
                        Execute(szQuery);
                    }

                    return true;
                }
            }
            catch (Exception err)
            {

                Console.WriteLine("- Error ImportTariffToDB : " + err.Message);
                LogInfo.WriteLog("- Error ImportTariffToDB : " + err.Message);
                return false;
            }
        }

        public bool ImportPlazaToDB(JArray plazaList, int plazaVersion, string timeStamp)
        {
            try
            {
                if (plazaList.Count == 0)
                {
                    Console.WriteLine("- Error ImportPlazaToDB : plazaList not found in JArray.");
                    LogInfo.WriteLog("- Error ImportPlazaToDB : plazaList not found in JArray.");
                    return false;
                }

                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    string szQuery = "DELETE FROM PlazaData ";
                    Execute(szQuery);
                    //-------------------------------------------
                    string szFmt = "INSERT INTO PlazaData (PlazaID, PlazaCode, PlazaName) " +
                                    "VALUES ('{0}', '{1}', '{2}')";
                    dynamic[] objPlaza = plazaList.ToObject<dynamic[]>();
                    foreach (dynamic plaza in objPlaza.ToList())
                    {
                        string szPlazaID = plaza["id"];
                        string szPlazaCode = plaza["abbrevation"];

                        szQuery = string.Format(szFmt, szPlazaID, szPlazaCode, szPlazaCode);
                        Execute(szQuery);
                    }

                    return true;
                }
            }
            catch (Exception err)
            {

                Console.WriteLine("- Error ImportPlazaToDB : " + err.Message);
                LogInfo.WriteLog("- Error ImportPlazaToDB : " + err.Message);
                return false;
            }
        }

        public string GetStaffDbTimeStamp()
        {
            var InvC = CultureInfo.InvariantCulture;
            string szQuery  = "SELECT MAX(StaffTimeStamp) FROM Staff";
            DataTable dt    = List(szQuery);
            if (dt.Rows[0][0] == DBNull.Value)
                return "";
            else
                return Convert.ToDateTime(dt.Rows[0][0].ToString()).ToString("yyyy-MM-dd HH:mm:ss", InvC);
        }

        public string GetDmtCardDbTimeStamp()
        {
            var InvC = CultureInfo.InvariantCulture;
            string szQuery  = "SELECT SendDateTime FROM DMTCardData";
            DataTable dt    = List(szQuery);
            if (dt.Rows.Count == 0)
                return "";
            else
                return Convert.ToDateTime(dt.Rows[0][0].ToString()).ToString("yyyy-MM-dd HH:mm:ss", InvC);
        }

        public string GetPromotionDbTimeStamp()
        {
            var InvC        = CultureInfo.InvariantCulture;
            string szQuery  = "SELECT SendDateTime FROM PromotionData";
            DataTable dt    = List(szQuery);
            if (dt.Rows.Count == 0)
                return "";
            else
                return Convert.ToDateTime(dt.Rows[0][0].ToString()).ToString("yyyy-MM-dd HH:mm:ss", InvC);
        }

        public bool CheckStaffManualUserPassword(string szUserID, string szPassword, ref string szErrMsg)
        {
            if (szUserID.Trim() == "")
            {
                szErrMsg = "ไม่พบรหัสพนักงาน. โปรดตรวจสอบรหัสใหม่อีกครั้ง.";
                return false;
            }

            if (szPassword.Trim() == "")
            {
                szErrMsg = "ไม่พบรหัสผ่าน. โปรดตรวจสอบรหัสผ่านใหม่อีกครั้ง.";
                return false;
            }

            try
            {
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    string szQuery = "SELECT * FROM Staff A " +
                                        "LEFT JOIN StaffGroup B ON A.StaffGroupID=B.DMT_StaffGroupID " +
                                        $"WHERE StaffID='{szUserID.Trim()}'";
                    DataTable dt = List(szQuery);
                    if (dt.Rows.Count == 0)
                    {
                        szErrMsg = "ไม่พบรหัสพนักงาน. โปรดตรวจสอบรหัสใหม่อีกครั้ง.";
                        return false;
                    }

                    //--- Found this staff
                    string szPasswordMD5    = dt.Rows[0]["StaffPassword"].ToString();
                    var md5                 = MD5CryptoServiceProvider.Create();
                    byte[] plainTextBytes   = Encoding.UTF8.GetBytes(szPassword.Trim());
                    byte[] hashBytes        = md5.ComputeHash(plainTextBytes);
                    var sbuilder            = new StringBuilder();
                    sbuilder.Append(string.Join("", hashBytes.Select(x => x.ToString("x2"))));
                    string szHashPassword   = sbuilder.ToString();

                    if (szHashPassword == szPasswordMD5)
                    {
                        TCT_Lib.szStaffID           = dt.Rows[0]["StaffID"].ToString();
                        TCT_Lib.szStaffCode         = dt.Rows[0]["StaffCode"].ToString();
                        TCT_Lib.szStaffName         = dt.Rows[0]["StaffName"].ToString();
                        TCT_Lib.szStaffGroupID      = dt.Rows[0]["StaffGroupID"].ToString();
                        TCT_Lib.szStaffGroupName    = dt.Rows[0]["StaffGroupLocal"] == DBNull.Value ? "- Unknown Staff Group -" : dt.Rows[0]["StaffGroupLocal"].ToString();

                        return true;
                    }
                    else
                    {
                        szErrMsg                    = "รหัสผ่านไม่ถูกต้อง. โปรดตรวจสอบใหม่อีกครั้ง.";

                        TCT_Lib.szStaffID           = "";
                        TCT_Lib.szStaffCode         = "";
                        TCT_Lib.szStaffName         = "";
                        TCT_Lib.szStaffGroupID      = "";
                        TCT_Lib.szStaffGroupName    = "";

                        return false;
                    }
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Fail PasswordMD5 : " + err.Message);
                return false;
            }
        }

        public bool CheckStaffWithSmartCard(string szCardSerialNo, ref string szErrMsg)
        {
            if (szCardSerialNo.Trim() == "")
            {
                szErrMsg = "ไม่พบข้อมูลบัตร. โปรดตรวจสอบใหม่อีกครั้ง.";
                return false;
            }

            try
            {
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    string szQuery = "SELECT * FROM Staff A " +
                                        "LEFT JOIN StaffGroup B ON A.StaffGroupID=B.DMT_StaffGroupID " +
                                        $"WHERE CardSerialNo='{szCardSerialNo.Trim()}'";
                    DataTable dt = List(szQuery);
                    if (dt.Rows.Count == 0)
                    {
                        szErrMsg                    = "ไม่พบรหัสพนักงาน. โปรดตรวจสอบรหัสใหม่อีกครั้ง.";

                        TCT_Lib.szStaffID           = "";
                        TCT_Lib.szStaffCode         = "";
                        TCT_Lib.szStaffName         = "";
                        TCT_Lib.szStaffGroupID      = "";
                        TCT_Lib.szStaffGroupName    = "";

                        return false;
                    }
                    else
                    {
                        TCT_Lib.szStaffID           = dt.Rows[0]["StaffID"].ToString();
                        TCT_Lib.szStaffCode         = dt.Rows[0]["StaffCode"].ToString();
                        TCT_Lib.szStaffName         = dt.Rows[0]["StaffName"].ToString();
                        TCT_Lib.szStaffGroupID      = dt.Rows[0]["StaffGroupID"].ToString();
                        TCT_Lib.szStaffGroupName    = dt.Rows[0]["StaffGroupLocal"] == DBNull.Value ? "- Unknown Staff Group -" : dt.Rows[0]["StaffGroupLocal"].ToString();

                        return true;
                    }
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Fail CheckStaffWithSmartCard : " + err.Message);
                LogInfo.WriteLaneEvent("- Fail CheckStaffWithSmartCard : " + err.Message);
                return false;
            }
        }

        public bool CheckStaffRemoteStaff(string szStaffCode, ref string szErrMsg)
        {
            if (szStaffCode.Trim() == "")
            {
                szErrMsg = "ไม่พบรหัสพนักงาน. โปรดตรวจสอบใหม่อีกครั้ง.";
                return false;
            }

            try
            {
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    string szQuery = "SELECT * FROM Staff A " +
                                        "LEFT JOIN StaffGroup B ON A.StaffGroupID=B.DMT_StaffGroupID " +
                                        $"WHERE StaffCode='{szStaffCode.Trim()}'";
                    DataTable dt = List(szQuery);
                    if (dt.Rows.Count == 0)
                    {
                        szErrMsg                    = "ไม่พบรหัสพนักงาน. โปรดตรวจสอบรหัสใหม่อีกครั้ง.";

                        TCT_Lib.szStaffID           = "";
                        TCT_Lib.szStaffCode         = "";
                        TCT_Lib.szStaffName         = "";
                        TCT_Lib.szStaffGroupID      = "";
                        TCT_Lib.szStaffGroupName    = "";

                        return false;
                    }
                    else
                    {
                        TCT_Lib.szStaffID           = dt.Rows[0]["StaffID"].ToString();
                        TCT_Lib.szStaffCode         = dt.Rows[0]["StaffCode"].ToString();
                        TCT_Lib.szStaffName         = dt.Rows[0]["StaffName"].ToString();
                        TCT_Lib.szStaffGroupID      = dt.Rows[0]["StaffGroupID"].ToString();
                        TCT_Lib.szStaffGroupName    = dt.Rows[0]["StaffGroupLocal"] == DBNull.Value ? "- Unknown Staff Group -" : dt.Rows[0]["StaffGroupLocal"].ToString();

                        return true;
                    }
                }
            }
            catch (Exception err)
            {
                LogInfo.WriteLaneEvent("- Fail CheckStaffWithSmartCard : " + err.Message);
                return false;
            }
        }

        public string GetLastStaffEndShift()
        {
            try
            {
                var InvC = CultureInfo.InvariantCulture;
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    string szQuery  = "SELECT MAX(StaffEndShiftDateTime) FROM StaffShift";
                    DataTable dt    = List(szQuery);
                    return (dt.Rows[0][0] == DBNull.Value) ? "" : Convert.ToDateTime(dt.Rows[0][0].ToString()).ToString("HH:mm:ss", InvC);
                }
            }
            catch (Exception err)
            {
                LogInfo.WriteLaneEvent("- Fail GetLastStaffEndShift : " + err.Message);
                return "";
            }
        }

        public String GetStaffIdFromSmartCard(string szCardSerialNo)
        {
            string szQuery = "SELECT * FROM Staff A " +
                                "LEFT JOIN StaffGroup B ON A.StaffGroupID=B.DMT_StaffGroupID " +
                                $"WHERE CardSerialNo='{szCardSerialNo.Trim()}'";
            DataTable dt = List(szQuery);
            return (dt.Rows.Count == 0) ? "" : dt.Rows[0]["StaffID"].ToString();
        }

        public bool CheckDuplicateStaffShift(string szCurStaffID)
        {
            String szQuery  = "SELECT * FROM StaffShift ORDER BY StaffStartShiftDateTime DESC";
            DataTable dt    = List(szQuery);
            if (dt.Rows.Count == 0)
                return false;

            DateTime dtStaffShift   = DateTime.Parse(dt.Rows[0]["StaffStartShiftDateTime"].ToString());
            TimeSpan dtSpan         = DateTime.Now.Subtract(dtStaffShift);
            int iDiffSec            = (int)dtSpan.TotalSeconds;

            String szStaffID        = dt.Rows[0]["StaffID"].ToString();
            if ((szCurStaffID == szStaffID) && (dt.Rows[0]["StaffEndShiftDateTime"] == DBNull.Value) && (iDiffSec < 2))
                return true;
            else
                return false;
        }

        public bool SetStaffFromStaffShift(int iStaffShiftID)
        {
            string szQuery = "SELECT * FROM StaffShift " +
                                "WHERE StaffShiftID=" + iStaffShiftID;
            DataTable dt = List(szQuery);
            if (dt.Rows.Count == 0)
                return false;

            TCT_Lib.szStaffID   = dt.Rows[0]["StaffID"].ToString();
            TCT_Lib.szStaffCode = dt.Rows[0]["StaffCode"].ToString();
            TCT_Lib.szStaffName = dt.Rows[0]["StaffName"].ToString();

            return true;
        }

        public DateTime GetLastClearBatch()
        {
            try
            {
                var InvC = CultureInfo.InvariantCulture;
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    string szQuery  = "SELECT MAX(ClearBatchDateTime) FROM EdcClearBatch";
                    DataTable dt    = List(szQuery);
                    return (dt.Rows[0][0] == DBNull.Value) ? DateTime.MinValue : Convert.ToDateTime(dt.Rows[0][0].ToString());
                }
            }
            catch (Exception err)
            {
                LogInfo.WriteLaneEvent("- Fail GetLastClearBatch : " + err.Message);
                return DateTime.MinValue;
            }
        }

        public long GetMaxClearBatchID()
        {
            try
            {
                string szQuery  = "SELECT MAX(ClearBatchID) FROM EdcClearBatch";
                DataTable dt    = List(szQuery);
                return dt.Rows[0][0] == DBNull.Value ? 1 : long.Parse(dt.Rows[0][0].ToString()) + 1;
            }
            catch (Exception err)
            {
                LogInfo.WriteLaneEvent("- Fail GetMaxClearBatchID : " + err.Message);
                return 0;
            }
        }

        public void UpdateLastClearBatch()
        {
            try
            {
                var InvC = CultureInfo.InvariantCulture;
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    long iClearBatchID = GetMaxClearBatchID();
                    string szQuery = "INSERT INTO EdcClearBatch (ClearBatchID, JobNo, StaffShiftID, ClearBatchDateTime) VALUES " +
                                        $"({iClearBatchID}, {TCT_Lib.iJobNo}, {TCT_Lib.iStaffShiftID}, " +
                                        $"'{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", InvC)}')";
                    Execute(szQuery);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error UpdateLastClearBatch : " + err.Message);
                LogInfo.WriteLog("- Error UpdateLastClearBatch : " + err.Message);
            }
        }

        public long GetMaxImageCaptureID()
        {
            try
            {
                string szQuery  = "SELECT MAX(ImageCaptureID) FROM ImageCaptureCamera";
                DataTable dt    = List(szQuery);
                return dt.Rows[0][0] == DBNull.Value ? 1 : long.Parse(dt.Rows[0][0].ToString()) + 1;
            }
            catch (Exception err)
            {
                LogInfo.WriteLaneEvent("- Fail GetMaxImageCaptureID : " + err.Message);
                return 0;
            }
        }

        public long GetMaxCaptureRunningNo()
        {
            try
            {
                string szQuery  = "SELECT MaxCaptureRunningNo FROM CaptureRunningNo";
                DataTable dt    = List(szQuery);
                if (dt.Rows.Count == 0)
                {
                    szQuery = "INSERT INTO CaptureRunningNo (MaxCaptureRunningNo) VALUES (1)";
                    Execute(szQuery);
                    return 1;
                }
                else
                {
                    szQuery = "UPDATE CaptureRunningNo SET MaxCaptureRunningNo=MaxCaptureRunningNo+1";
                    Execute(szQuery);
                    return long.Parse(dt.Rows[0][0].ToString())+1;
                }
            }
            catch (Exception err)
            {
                LogInfo.WriteLaneEvent("- Fail GetMaxImageCaptureID : " + err.Message);
                return 0;
            }
        }

        public long GetCurMaxCaptureRunningNo()
        {
            try
            {
                string szQuery  = "SELECT MaxCaptureRunningNo FROM CaptureRunningNo";
                DataTable dt    = List(szQuery);
                return (dt.Rows.Count == 0) ? 0 : long.Parse(dt.Rows[0][0].ToString());
            }
            catch (Exception err)
            {
                LogInfo.WriteLaneEvent("- Fail GetMaxImageCaptureID : " + err.Message);
                return 0;
            }
        }

        public long GetCurCaptureRunningNo(int iJobNo, int nTrx)
        {
            var InvC = CultureInfo.InvariantCulture;
            //string szQuery  = $"SELECT CaptureRunningNo FROM ImageCaptureCamera WHERE JobNo={iJobNo} AND nTrx={nTrx} AND CameraIndex=1 ORDER BY UpdateDateTime DESC";
            string szQuery = $"SELECT CaptureRunningNo FROM ImageCaptureCamera WHERE JobNo={iJobNo} AND nTrx={nTrx} " +
            	                $"AND CameraIndex=1 AND DATE(UpdateDateTime)='{DateTime.Today.ToString("yyyy-MM-dd", InvC)}' " +
            	                "ORDER BY UpdateDateTime DESC";
            DataTable dt    = List(szQuery);
            return (dt.Rows.Count == 0) ? GetCurMaxCaptureRunningNo() : int.Parse(dt.Rows[0][0].ToString());
        }

        public long GetCaptureRunningNoOfTrans(int iJobNo, int nTrx, int iTransID, DateTime dtDateTime)
        {
            try
            {
                var InvC = CultureInfo.InvariantCulture;
                string szQuery = "SELECT CaptureRunningNo FROM ImageCaptureCamera " +
                                $"WHERE JobNo={iJobNo} AND nTrx={nTrx} " +
                                $"AND DATE(UpdateDateTime)='{dtDateTime.ToString("yyyy-MM-dd", InvC)}' "+
                                "ORDER BY UpdateDateTime DESC";
                DataTable dt = List(szQuery);
                int iCaptureRunningNo = (dt.Rows.Count == 0) ? 0 : int.Parse(dt.Rows[0][0].ToString());
                szQuery = $"UPDATE ImageCaptureCamera SET LaneTransactionID={iTransID} " +
                            $"WHERE CaptureRunningNo={iCaptureRunningNo} AND JobNo={iJobNo} AND nTrx={nTrx}";
                Execute(szQuery);
                return iCaptureRunningNo;
            }
            catch (Exception err)
            {
                LogInfo.WriteLaneEvent("- Fail GetCaptureRunningNoOfTrans : " + err.Message);
                return 0;
            }
        }

        public bool GetCaptureImageFileName(int iJobNo, int nTrx, int iTransID, ref string szImgFileName1, ref string szImgFileName2, ref string szImgFileName3)
        {
            try
            {
                string szQuery = "SELECT CameraIndex, ImageCameraFileName FROM ImageCaptureCamera " +
                                    $"WHERE LaneTransactionID={iTransID} AND JobNo={iJobNo} AND nTrx={nTrx} "+
                                    "ORDER BY CameraIndex";
                DataTable dt = List(szQuery);
                for (int i=0; i<dt.Rows.Count; i++)
                {
                    if (int.Parse(dt.Rows[i]["CameraIndex"].ToString()) == 1)
                        szImgFileName1 = dt.Rows[i]["ImageCameraFileName"] == DBNull.Value ? "" : dt.Rows[i]["ImageCameraFileName"].ToString();
                    else if (int.Parse(dt.Rows[i]["CameraIndex"].ToString()) == 2)
                        szImgFileName2 = dt.Rows[i]["ImageCameraFileName"] == DBNull.Value ? "" : dt.Rows[i]["ImageCameraFileName"].ToString();
                    else if (int.Parse(dt.Rows[i]["CameraIndex"].ToString()) == 3)
                        szImgFileName3 = dt.Rows[i]["ImageCameraFileName"] == DBNull.Value ? "" : dt.Rows[i]["ImageCameraFileName"].ToString();
                }

                return true;
            }
            catch (Exception err)
            {
                LogInfo.WriteLaneEvent("- Fail GetCaptureImageFileName : " + err.Message);
                return false;
            }
        }

        //------------------------
        public bool ImportPromotionToDB(JArray promotionList, int version, string timeStamp)
        {
            try
            {
                if (promotionList.Count == 0)
                {
                    Console.WriteLine("- Error ImportPromotionToDB : promotionList not found in JArray.");
                    LogInfo.WriteLog("- Error ImportPromotionToDB : promotionList not found in JArray.");
                    return false;
                }

                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    string szQuery = "DELETE FROM PromotionData ";
                    Execute(szQuery);
                    //-------------------------------------------
                    string szFmt = "INSERT INTO PromotionData (PromotionID, Version, SendDateTime, " +
                                    "EffectiveFromDate, EffectiveToDate, PromotionTypeID, " +
                                    "MessageLine1, MessageLine2, MessageLine3, MessageLine4, QrCodeData," +
                                    "Status, UpdateDateTime) " +
                                    "VALUES ({0}, {1}, '{2}', '{3}', '{4}', {5}, '{6}', '{7}', '{8}', '{9}', '{10}', '{11}', '{12}')";
                    dynamic[] objPromotion = promotionList.ToObject<dynamic[]>();
                    int idx = 1;
                    foreach (dynamic promotion in objPromotion.ToList())
                    {
                        string effectivFromDate = (string)promotion["effectiveFromDatetime"] ?? "";
                        string effectiveToDate  = (string)promotion["effectiveToDatetime"] ?? "";
                        int promotionTypeId     = (int)(promotion["promotionTypeId"] ?? 1);
                        string messageLine1     = (string)promotion["messageLine01"] ?? "";
                        string messageLine2     = (string)promotion["messageLine02"] ?? "";
                        string messageLine3     = (string)promotion["messageLine03"] ?? "";
                        string messageLine4     = (string)promotion["messageLine04"] ?? "";
                        string qrCodeData       = (string)promotion["qrCode"] ?? "";
                        string status           = (string)promotion["status"] ?? "";

                        szQuery = string.Format(szFmt, idx, version, Convert.ToDateTime(timeStamp).ToString(TCT_Lib.szDateTimeFormatDB),
                                            Convert.ToDateTime(effectivFromDate).ToString(TCT_Lib.szDateTimeFormatDB), 
                                            Convert.ToDateTime(effectiveToDate).ToString(TCT_Lib.szDateTimeFormatDB),
                                            promotionTypeId, messageLine1, messageLine2, messageLine3, messageLine4,
                                            qrCodeData, status, DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB));
                        LogInfo.WriteLog("+ Query ImportPromotionToDB : " + szQuery);
                        Execute(szQuery);
                        idx++;
                    }

                    //--------------------------------------------------------------------------
                    //--- Load Promotion Data
                    LoadPromotionData();

                    return true;
                }
            }
            catch (Exception err)
            {

                Console.WriteLine("- Error ImportPromotionToDB : " + err.Message);
                LogInfo.WriteLog("- Error ImportPromotionToDB : " + err.Message);
                return false;
            }
        }

        public long GetMaxPromotionID()
        {
            try
            {
                string szQuery  = "SELECT MAX(PromotionID) FROM PromotionData";
                DataTable dt    = List(szQuery);
                return dt.Rows[0][0] == DBNull.Value ? 1 : long.Parse(dt.Rows[0][0].ToString()) + 1;
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Fail GetMaxPromotionID : " + err.Message);
                return 0;
            }
        }

        public bool LoadPromotionData()
        {
            LogInfo.WriteLog("+ Load promotion data.");
            try
            {
                var InvC        = CultureInfo.InvariantCulture;
                string szFmt    = "SELECT * FROM PromotionData " +
                                    "WHERE '{0}'>=EffectiveFromDate AND '{0}'<EffectiveToDate AND Status='A'";
                string szQuery  = string.Format(szFmt, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", InvC));
                LogInfo.WriteLog("+ Load promotion data Query : " + szQuery);
                DataTable dt    = List(szQuery);

                if (dt.Rows.Count > 0)
                {
                    TCT_Lib.iFooter_PrintType   = int.Parse(dt.Rows[0]["PromotionTypeID"].ToString());
                    TCT_Lib.szFooter_Line1      = dt.Rows[0]["MessageLine1"] == DBNull.Value ? "" : dt.Rows[0]["MessageLine1"].ToString();
                    TCT_Lib.szFooter_Line2      = dt.Rows[0]["MessageLine2"] == DBNull.Value ? "" : dt.Rows[0]["MessageLine2"].ToString();
                    TCT_Lib.szFooter_Line3      = dt.Rows[0]["MessageLine3"] == DBNull.Value ? "" : dt.Rows[0]["MessageLine3"].ToString();
                    TCT_Lib.szFooter_Line4      = dt.Rows[0]["MessageLine4"] == DBNull.Value ? "" : dt.Rows[0]["MessageLine4"].ToString();
                    TCT_Lib.szFooter_QrData     = dt.Rows[0]["QrCodeData"] == DBNull.Value ? "" : dt.Rows[0]["QrCodeData"].ToString();
                    TCT_Lib.dtFooter_StartDate  = Convert.ToDateTime(dt.Rows[0]["EffectiveFromDate"].ToString());
                    TCT_Lib.dtFooter_EndDate    = Convert.ToDateTime(dt.Rows[0]["EffectiveToDate"].ToString());


                }
                else
                {
                    LogInfo.WriteLog("+ Not found promotion data.");
                    //TCT_Lib.iFooter_PrintType   = int.Parse(dt.Rows[0]["PromotionTypeID"].ToString());
                    TCT_Lib.szFooter_Line1      = "";
                    TCT_Lib.szFooter_Line2      = "";
                    TCT_Lib.szFooter_Line3      = "";
                    TCT_Lib.szFooter_Line4      = "";
                    TCT_Lib.szFooter_QrData     = "";
                    //TCT_Lib.dtFooter_StartDate  = Convert.ToDateTime(dt.Rows[0]["EffectiveFromDate"].ToString());
                    //TCT_Lib.dtFooter_EndDate    = Convert.ToDateTime(dt.Rows[0]["EffectiveToDate"].ToString());
                }

                //------------
                LogInfo.WriteLog("+ Promotion data : Line 1 = " + TCT_Lib.szFooter_Line1);
                LogInfo.WriteLog("+ Promotion data : Line 2 = " + TCT_Lib.szFooter_Line2);
                LogInfo.WriteLog("+ Promotion data : Line 3 = " + TCT_Lib.szFooter_Line3);
                LogInfo.WriteLog("+ Promotion data : Line 4 = " + TCT_Lib.szFooter_Line4);
                LogInfo.WriteLog("+ Promotion data : PromotionTypeID \t= " + TCT_Lib.iFooter_PrintType);
                LogInfo.WriteLog("+ Promotion data : EffectiveFromDate = " + TCT_Lib.dtFooter_StartDate.ToString("yyyy-MM-dd HH:mm:ss"));
                LogInfo.WriteLog("+ Promotion data : EffectiveToDate = " + TCT_Lib.dtFooter_EndDate.ToString("yyyy-MM-dd HH:mm:ss"));
                LogInfo.WriteLog("+ Promotion data : QR Code Data = " + TCT_Lib.szFooter_QrData);

                return true;
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Error LoadPromotionData : " + err.Message);
                return false;
            }
        }

        //---------------------------------------
        public bool ImportDmtCardToDB(JArray dmtCardList, int version, string timeStamp)
        {
            try
            {
                if (dmtCardList.Count == 0)
                {
                    Console.WriteLine("- Error ImportDmtCardToDB : dmtCardList not found in JArray.");
                    LogInfo.WriteLog("- Error ImportDmtCardToDB : dmtCardList not found in JArray.");
                    return false;
                }

                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    string szQuery = "DELETE FROM DmtCardData ";
                    Execute(szQuery);
                    //-------------------------------------------
                    string szFmt = "INSERT INTO DmtCardData (DmtCardID, Version, SendDateTime, " +
                                    "CardSerialNo, DmtCarNo, DmtLicensePlate, " +
                                    "VehicleClass, Department, Affiliation, AllowPass," +
                                    "Status, UpdateDateTime) " +
                                    "VALUES ({0}, {1}, '{2}', '{3}', '{4}', '{5}', {6}, '{7}', '{8}', {9}, '{10}', '{11}')";
                    dynamic[] objDmtCard = dmtCardList.ToObject<dynamic[]>();
                    int idx = 1;
                    foreach (dynamic dmtCard in objDmtCard.ToList())
                    {
                        string cardSerialNo     = (string)dmtCard["smartcardNo"] ?? "";
                        string dmtCarNo         = (string)dmtCard["carNo"] ?? "";
                        string dmtLicensePlate  = (string)dmtCard["licensePlate"] ?? "";
                        int vehicleClass        =  int.Parse((string)(dmtCard["vehicleClass"] ?? "1"));

                        string department       = (string)dmtCard["department"] ?? "";
                        string affiliation      = (string)dmtCard["affiliation"] ?? "";
                        JArray jPlazaList       = dmtCard["plazaList"];
                        List<int> plazaList     = jPlazaList.ToObject<List<int>>();
                        int allowPass           = plazaList.IndexOf(int.Parse(TCT_Lib.szPlazaID)) >= 0 ? 1 : 0;
                        string status           = (string)dmtCard["status"] ?? "";

                        szQuery = string.Format(szFmt, idx, version, Convert.ToDateTime(timeStamp).ToString(TCT_Lib.szDateTimeFormatDB),
                                            cardSerialNo, dmtCarNo, dmtLicensePlate, vehicleClass,
                                            department, affiliation, allowPass,
                                            status, DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB));
                                                                                      
                        LogInfo.WriteLog("+ Query ImportDmtCardToDB : " + szQuery);
                        Execute(szQuery);
                        idx++;
                    }

                    //--------------------------------------------------------------------------
                    //--- Load Promotion Data
                    //LoadDmtCardData();

                    return true;
                }
            }
            catch (Exception err)
            {

                Console.WriteLine("- Error ImportDmtCardToDB : " + err.Message);
                LogInfo.WriteLog("- Error ImportDmtCardToDB : " + err.Message);
                return false;
            }
        }

        private int GetAllowDmtCard()
        {


            return 0;
        }

        public long GetMaxDmtCardID()
        {
            try
            {
                string szQuery  = "SELECT MAX(DmtCardID) FROM DmtCardData";
                DataTable dt    = List(szQuery);
                return dt.Rows[0][0] == DBNull.Value ? 1 : long.Parse(dt.Rows[0][0].ToString()) + 1;
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Fail GetMaxDmtCardID : " + err.Message);
                return 0;
            }
        }

        public bool CheckAllowDmtCanPass(string szCardSerialNo, ref string szDmtCardNo, ref string szDmtLicensePlate, ref string szDepartment, ref string szAffiliation, ref int iClassType)
        {
            LogInfo.WriteLog("+ CheckAllowDmtCanPass : CardSerialNo="+szCardSerialNo);
            try
            {
                string szFmt    = "SELECT * FROM DmtCardData " +
                                    "WHERE CardSerialNo='{0}' AND Status='A' AND AllowPass=1";
                string szQuery  = string.Format(szFmt, szCardSerialNo);
                LogInfo.WriteLog("+ Load DmtCard data Query : " + szQuery);
                DataTable dt    = List(szQuery);

                if (dt.Rows.Count > 0)
                {
                    szDmtCardNo         = dt.Rows[0]["DmtCarNo"] == DBNull.Value ? "" : dt.Rows[0]["DmtCarNo"].ToString();
                    szDmtLicensePlate   = dt.Rows[0]["DmtLicensePlate"] == DBNull.Value ? "" : dt.Rows[0]["DmtLicensePlate"].ToString();
                    szDepartment        = dt.Rows[0]["Department"] == DBNull.Value ? "" : dt.Rows[0]["Department"].ToString();
                    szAffiliation       = dt.Rows[0]["Affiliation"] == DBNull.Value ? "" : dt.Rows[0]["Affiliation"].ToString();
                    iClassType          = dt.Rows[0]["Affiliation"] == DBNull.Value ? 0 : int.Parse(dt.Rows[0]["VehicleClass"].ToString());

                    return true;
                }
                else
                {
                    LogInfo.WriteLog("+ Not found DmtCard data.");
                    return false;
                }
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Error CheckAllowDmtCanPass : " + err.Message);
                return false;
            }
        }

        public bool InsertDmtCardTransaction(string szCardSerialNo, string szDmtCardNo, string szDmtLicensePlate, string szDepartment, string szAffiliation, int iClassType)
        {
            try
            {
                string szFmt = "INSERT INTO DmtCardTrans (DmtCardTransID, CardSerialNo, DmtCarNo, DmtLicensePlate, VehicleClass, Department, Affiliation, UpdateDateTime) "+
                                "VALUES ({0}, '{1}', '{2}', '{3}', {4}, '{5}', '{6}', '{7}')";

                long iDmtCardTransID    = GetMaxDmtCardTransID();
                string szQuery          = string.Format(szFmt, iDmtCardTransID, szCardSerialNo, szDmtCardNo, szDmtLicensePlate, iClassType, 
                                                    szDepartment.Replace("'", "''"), szAffiliation.Replace("'", "''"), 
                                                    DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB));
                LogInfo.WriteLog("+ Query InsertDmtCardTransaction : " + szQuery);
                Execute(szQuery);
                return true;
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Error InsertDmtCardTransaction : " + err.Message);
                return false;
            }
        }

        public long GetMaxDmtCardTransID()
        {
            try
            {
                string szQuery  = "SELECT MAX(DmtCardTransID) FROM DmtCardTrans";
                DataTable dt    = List(szQuery);
                return dt.Rows[0][0] == DBNull.Value ? 1 : long.Parse(dt.Rows[0][0].ToString()) + 1;
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Fail GetMaxDmtCardTransID : " + err.Message);
                return 0;
            }
        }

        public long GetCurDmtCardTransID()
        {
            try
            {
                string szQuery  = "SELECT MAX(DmtCardTransID) FROM DmtCardTrans";
                DataTable dt    = List(szQuery);
                return dt.Rows[0][0] == DBNull.Value ? 0 : long.Parse(dt.Rows[0][0].ToString());
            }
            catch (Exception err)
            {
                Console.WriteLine("- Error GetCurDmtCardTransID : " + err.Message);
                LogInfo.WriteLog("- Error GetCurDmtCardTransID : " + err.Message);
                return 0;
            }
        }

        //---------------------------------------
        public bool ImportTfiToDB(dynamic tfiData, int version, string timeStamp)
        {
            try
            {
                using (SqliteCommand command = new SqliteCommand(conn))
                {
                    long iTfiID     = GetMaxTfiID();
                    //-------------------------------------------
                    string szFmt = "INSERT INTO TfiData (TfiID, Version, SendDateTime, " +
                                    "ImageFileName, EffectiveDateTime, UpdateBy, " +
                                    "SendTfiStatus, UpdateDateTime) " +
                                    "VALUES ({0}, {1}, '{2}', '{3}', '{4}', '{5}', {6}, '{7}')";
                                    
                    string imageFileName        = (string)tfiData["image_name"] ?? "";
                    string effectiveDateTime    = (string)tfiData["effectiveDatetime"] ?? "";
                    string updateBy             = (string)tfiData["update_by"] ?? "";
                    string imageData            = (string)tfiData["image_data"] ?? "";

                    string szQuery = string.Format(szFmt, iTfiID, version, Convert.ToDateTime(timeStamp).ToString(TCT_Lib.szDateTimeFormatDB),
                                        imageFileName, Convert.ToDateTime(effectiveDateTime).ToString(TCT_Lib.szDateTimeFormatDB), updateBy,
                                        0, DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB));
                                                                                  
                    LogInfo.WriteLog("+ Query ImportTfiToDB : " + szQuery);
                    Execute(szQuery);


                    //--------------------------------------------------------------------------
                    //--- Send TFI Data by FTP
                    Image tfiImg        = ConvertStringBase64ToImage(imageData);
                    var tfiImagePath    = Path.Combine(TCT_Lib.szLogPath, "TfiImage");
                    tfiImg.Save(Path.Combine(tfiImagePath, imageFileName));
                    //LoadDmtCardData();

                    return true;
                }
            }
            catch (Exception err)
            {

                Console.WriteLine("- Error ImportTfiToDB : " + err.Message);
                LogInfo.WriteLog("- Error ImportTfiToDB : " + err.Message);
                return false;
            }
        }

        private long GetMaxTfiID()
        {
            try
            {
                string szQuery  = "SELECT MAX(TfiID) FROM TfiData";
                DataTable dt    = List(szQuery);
                return dt.Rows[0][0] == DBNull.Value ? 1 : long.Parse(dt.Rows[0][0].ToString()) + 1;
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Fail GetMaxTfiID : " + err.Message);
                return 0;
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

        async public Task<bool> TfiTransferImageFile()
        {
            try
            {
                string szFmt = "";
                string szQuery = "SELECT * FROM TfiData " +
                                    "WHERE SendTfiStatus IN (0, 99) " +
                                    "GROUP BY ImageFileName " +
                                    "ORDER BY EffectiveDateTime";
                //LogInfo.WriteLog("+ Get TFI data Query : " + szQuery);
                DataTable dt = List(szQuery);
                LogInfo.WriteLog("+ Found send TFI data = " + dt.Rows.Count);
                for (int i=0; i<dt.Rows.Count; i++)
                {
                    DateTime dtEffectiveDate    = Convert.ToDateTime(dt.Rows[i]["EffectiveDateTime"].ToString());
                    string szTfiID              = dt.Rows[i]["TfiID"].ToString();
                    string szTfiFileName        = dt.Rows[i]["ImageFileName"].ToString();

                    if (DateTime.Now > dtEffectiveDate)
                    {
                        //--- Send Imagge File name to TFI Controller
                        LogInfo.WriteLog($"+ Found send TFI data : FileName={szTfiFileName}, EffectiveDate={dtEffectiveDate}, TfiID={szTfiID}");
                        LogInfo.WriteLaneEvent($"+ Found send TFI data : FileName={szTfiFileName}, EffectiveDate={dtEffectiveDate}, TfiID={szTfiID}");
                        //--- Send TFI file by SCP
                        string szErrMsg = "";
                        bool bResult    = TCT_Lib.TransferTfiFile(szTfiFileName, ref szErrMsg);
                        if (bResult)
                        {
                            szFmt = "UPDATE TfiData SET SendTfiStatus=1, SendTfiDateTime='{1}' " +
                                        "WHERE TfiID={0}";
                            szQuery = string.Format(szFmt, szTfiID, DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB));
                            Execute(szQuery);
                            try
                            {
                                //--- Move file to BAK
                                var szTfiBakPath = Path.Combine(Path.Combine(TCT_Lib.szLogPath, "TfiImage"), "BAK");
                                if (!Directory.Exists(szTfiBakPath))
                                    Directory.CreateDirectory(szTfiBakPath);
                                File.Delete(Path.Combine(szTfiBakPath, szTfiFileName + "_" + szTfiID));
                                File.Move(Path.Combine(Path.Combine(TCT_Lib.szLogPath, "TfiImage"), szTfiFileName), Path.Combine(szTfiBakPath, szTfiFileName+"_"+szTfiID));
                            }
                            catch (Exception err)
                            {
                                LogInfo.WriteLog("- Error move TFI image to BAK Path : " + err.Message);
                                File.Delete(Path.Combine(Path.Combine(TCT_Lib.szLogPath, "TfiImage"), szTfiFileName));
                            }
                        }
                        else
                        {
                            szFmt = "UPDATE TfiData SET SendTfiStatus=99, SendTfiDateTime='{1}', SendTfiMessage='{2}' " +
                                        "WHERE TfiID={0}";
                            szQuery = string.Format(szFmt, szTfiID, DateTime.Now.ToString(TCT_Lib.szDateTimeFormatDB), szErrMsg.Replace("'", "''"));
                            Execute(szQuery);
                        }
                    }
                    else
                    {
                        LogInfo.WriteLog($"- Found send TFI data : FileName={szTfiFileName}, EffectiveDate={dtEffectiveDate}, But not send file because Now > EffectiveDate");
                        LogInfo.WriteLaneEvent($"- Found send TFI data : FileName={szTfiFileName}, EffectiveDate={dtEffectiveDate}, But not send file because Now > EffectiveDate");
                    }

                    //---- Change status of same file, exempt this fle
                    szFmt = "UPDATE TfiData SET SendTfiStatus=90 " +
                                "WHERE ImageFileName='{0}' " +
                                "AND TfiID <> {1}";
                    szQuery = string.Format(szFmt, szTfiFileName, szTfiID);
                    Execute(szQuery);
                }

                return true;
            }
            catch (Exception err)
            {
                LogInfo.WriteLog("- Error TfiTransferImageFile : " + err.Message);
                LogInfo.WriteLaneEvent("- Error TfiTransferImageFile : " + err.Message);

                return false;
            }
        }
    }
}
