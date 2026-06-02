using System;
using System.Data;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TCT_App.RabbitMQ;

namespace TCT_App
{
    public class TCT_MTS_Alarm
    {
        async public static Task<bool> SendJsonMsgMtsAlarmToRabbitMQ()
        {
            LogInfo.WriteLogMTS("+ MTS-Alarm : Start Process");
            //--- No setting send MTS Alarm
            if (TCT_Lib.szMTS_AlarmDeviceList.Trim() == "")
            {
                LogInfo.WriteLogMTS("+ MTS-Alarm : End Process (No config)");
                return false;
            }

            string szQuery = "SELECT * FROM LaneAlarmRTO " +
                                "WHERE SendStatusDC = 0 " +
                                $"AND AlarmDevice IN ({TCT_Lib.szMTS_AlarmDeviceList})";
            DataTable dtAlarm = TCT_Lib.LaneDB.List(szQuery);
            if (dtAlarm.Rows.Count == 0)
            {
                LogInfo.WriteLogMTS("+ MTS-Alarm : End Process (No alarm)");
                return false;
            }

            for (int i = 0; i < dtAlarm.Rows.Count; i++)
            {
                if (!await SendAlarmToRabbitMQ(dtAlarm.Rows[i]))
                    break;
                Thread.Sleep(200);
            }

            {
                LogInfo.WriteLogMTS("+ MTS-Alarm : End Process");
                return true;
            }
        }

        async static Task<bool> SendAlarmToRabbitMQ(DataRow drAlarm)
        {
            try
            {
                var xMtsAlarmJson           = new MtsAlarmJson();

                xMtsAlarmJson.jsonrpc       = "2.0";
                xMtsAlarmJson.method        = "MTS-Alarm";

                LogInfo.WriteLogMTS("+ Get MTS Alarm");
                var objMtsAlarm             = new ObjMtsAlarm();
                objMtsAlarm.requestId       = "MtsAlarm"+DateTime.Now.ToString("yyyyMMddHHmmssfff");
                objMtsAlarm.requestDateTime = TCT_Lib.DateTime_ISO8601(DateTime.Now);
                objMtsAlarm.jobNo           = TCT_Lib.iJobNo;
                objMtsAlarm.nTrx            = TCT_Lib.nTrx;
                objMtsAlarm.plaza           = GetPlazaData();
                objMtsAlarm.staff           = GetStaffData();
                objMtsAlarm.supervisor      = GetSupervisorData();
                objMtsAlarm.lane            = GetLaneData();
                objMtsAlarm.alarm           = GetAlermData(drAlarm);

                xMtsAlarmJson.@params        = objMtsAlarm;

                string szMtsAlarm           = JsonConvert.SerializeObject(xMtsAlarmJson, Formatting.Indented);
                LogInfo.WriteLogMTS("+ MTS-Alarm : " + szMtsAlarm);

                int iAlarmID = int.Parse(drAlarm["AlarmRtoID"].ToString());
                if (TCT_Lib.mtsRabbitMQ.SendMtsMsgToRabbitMQ("MTS-Alarm", szMtsAlarm))
                {
                    UpdateAlarmSendDcStatus(iAlarmID, true);
                    return true;
                }
                else
                {
                    UpdateAlarmSendDcStatus(iAlarmID, false);
                    return false;
                }
            }
            catch (Exception err)
            {
                LogInfo.WriteLogMTS("- Fail SendJsonMsgMtsAlarmToRabbitMQ : " + err.Message);
                int iAlarmID = int.Parse(drAlarm["AlarmRtoID"].ToString());
                UpdateAlarmSendDcStatus(iAlarmID, false);

                return false;
            }
        }

        private static RabbitMQ.Plaza GetPlazaData()
        {
            var xPlaza          = new RabbitMQ.Plaza();
            xPlaza.plazaId      = TCT_Lib.szPlazaID;
            xPlaza.plazaName    = TCT_Lib.szPlazaName;

            return xPlaza;
        }

        private static LaneMts GetLaneData()
        {
            var xLane               = new LaneMts();
            xLane.laneId            = TCT_Lib.iLaneID.ToString();
            xLane.laneName          = TCT_Lib.szLaneName;
            xLane.laneMode          = (int)TCT_Lib.eCurLaneMode;
            xLane.laneModeName      = TCT_Lib.GetLaneModeName(TCT_Lib.eCurLaneMode);
            xLane.laneType          = (int)TCT_Lib.eCurLaneType;
            xLane.laneTypeName      = TCT_Lib.GetLaneTypeName(TCT_Lib.eCurLaneType);
            xLane.laneDirection     = (int)TCT_Lib.eLaneDirection;
            xLane.laneDirectionName = TCT_Lib.GetLaneDirection(TCT_Lib.eLaneDirection);

            return xLane;
        }

        private static Staff GetStaffData()
        {
            var xStaff                  = new Staff();
            xStaff.staffId              = TCT_Lib.szStaffID;
            xStaff.staffName            = TCT_Lib.szStaffName;
            xStaff.staffShiftId         = TCT_Lib.iStaffShiftID;
            xStaff.startShiftDateTime   = TCT_Lib.DateTime_ISO8601(TCT_Lib.dtStaffStartShift);

            return xStaff;
        }

        private static Supervisor GetSupervisorData()
        {
            var xSupervisor                 = new Supervisor();
            xSupervisor.supervisorId        = TCT_Lib.szSupervisorID;
            xSupervisor.supervisorName      = TCT_Lib.szSupervisorName;
            xSupervisor.supervisorShiftId   = TCT_Lib.iSupervisorShiftID;
            xSupervisor.startShiftDateTime  = TCT_Lib.DateTime_ISO8601(TCT_Lib.dtSupervisorStartShift);

            return xSupervisor;

        }

        private static AlarmData GetAlermData(DataRow drAlarm)
        {
            var InvC                    = CultureInfo.InvariantCulture;

            int iAlarmDevice            = drAlarm["AlarmDevice"] == DBNull.Value ? 0 : int.Parse(drAlarm["AlarmDevice"].ToString());
            string szAlarmDeviceName    = drAlarm["AlarmDeviceName"] == DBNull.Value ? "" : drAlarm["AlarmDeviceName"].ToString();
            string szAlarmDetail        = drAlarm["AlarmDetail"] == DBNull.Value ? "" : drAlarm["AlarmDetail"].ToString();
            string szAlarmRemark        = drAlarm["AlarmRemark"] == DBNull.Value ? "" : drAlarm["AlarmRemark"].ToString();
            int iAlarmType              = drAlarm["AlarmType"] == DBNull.Value ? 0 : int.Parse(drAlarm["AlarmType"].ToString());
            DateTime dtAlarmDateTime    = Convert.ToDateTime(drAlarm["AlarmDateTime"].ToString(), InvC);
            //DateTime dtAlarmDateTime    = Convert.ToDateTime(Convert.ToDateTime(drAlarm["AlarmDateTime"].ToString()).ToString("yyyy-MM-dd HH:mm:ss.fff", InvC));

            int iAlarmID                = int.Parse(drAlarm["AlarmRtoID"].ToString());

            if (iAlarmDevice == 0)
                return null;
            try
            {
                var alarmData               = new AlarmData();
                alarmData.alarmDevice       = iAlarmDevice;
                alarmData.alarmName         = szAlarmDeviceName;
                alarmData.alarmDetail       = szAlarmDetail;
                alarmData.alarmRemark       = szAlarmRemark;
                alarmData.alarmEventType    = iAlarmType;
                alarmData.alarmDateTime     = TCT_Lib.DateTime_ISO8601(dtAlarmDateTime);

                return alarmData;
            }
            catch (Exception err)
            {
                LogInfo.WriteLogMTS($"+ GetAlarmData : AlarmRtoID={iAlarmID}, AlarmDevice={szAlarmDeviceName}, AlarmDetail={szAlarmDetail}, AlarmRemark={szAlarmRemark}\r\nError : {err.Message}");
                return null;
            }
        }

        private static void UpdateAlarmSendDcStatus(int iAlarmID, bool bResult)
        {
            if (iAlarmID == 0)
                return;

            string szQuery = "";
            if (bResult)
            {
                szQuery = "UPDATE LaneAlarmRTO " +
                        $"SET SendStatusDC=1, SendDateTimeDC='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' " +
                        $"WHERE AlarmRtoID={iAlarmID}";
            }
            else
            {
                szQuery = "UPDATE LaneAlarmRTO " +
                        $"SET SendRetryDC=SendRetryDC+1, SendDateTimeDC='{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}' " +
                        $"WHERE AlarmRtoID={iAlarmID}";
            }
            //LogInfo.WriteLog("+ Query : " + szQuery);
            TCT_Lib.LaneDB.Execute(szQuery);
        }
    }
}