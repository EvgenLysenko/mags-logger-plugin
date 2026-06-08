namespace MagsLogger
{
    public class MagsLogger
    {
        public static readonly MAVLink.MAV_CMD COMMAND_LONG_ID = MAVLink.MAV_CMD.USER_2;

        public static readonly int STATUS_BIT_MAGS_ONLINE = 0x1;
        public static readonly int STATUS_BIT_GPS_ONLINE = 0x2;
        public static readonly int STATUS_BIT_LOG_STARTED = 0x4;
        public static readonly int STATUS_BIT_OUT_MAGS = 0x8;
        public static readonly int STATUS_BIT_OUT_ACCEL = 0x10;
        public static readonly int STATUS_BIT_FULL_TRACE_ENEBLED = 0x20;
        public static readonly int STATUS_BIT_DEBUG_ENABLED = 0x40;
        public static readonly int STATUS_BIT_UNUSED = 0x80;
        public static readonly int STATUS_BIT_GPS_REQUEST_RESULT_MASK = 0x100 | 0x200 | 0x400 | 0x800 | 0x1000; // 0x1F00 // OK | ON | OFF | failed | in progress
        public static readonly int STATUS_BIT_GPS_REQUEST_RESULT_SHIFT = 8;

        public static bool isStatus(int status, int statusBit)
        {
            return (status & statusBit) == statusBit;
        }
    }

    public enum MagsCommandId
    {
        MAGS_MIN = 2001,
        MAGS_SENSOR1_STATUS = 2001,
        MAGS_SENSOR2_STATUS = 2002,
        MAGS_SENSOR3_STATUS = 2003,
        MAGS_SENSOR4_STATUS = 2004,
        MAGS_SENSORS_STATUS = 2011,
        MAGS_STATUS = 2012,
        MAGS_CCR_SET = 2013,
        MAGS_SETTINGS = 2014,
        MAGS_SETTINGS_REQUEST = 2015,
        MAGS_LOGGING_START = 2016,
        MAGS_LOGGING_STOP = 2017,
        MAGS_SET_OUT_MAGS = 2018,
        MAGS_SET_OUT_ACCEL = 2019,
        MAGS_MAGS_VALUES = 2020,
        MAGS_ACCEL_VALUES = 2021,
        MAGS_FULL_TRACE_ENABLE = 2022,
        MAGS_DEBUG_ENABLE = 2023,
        MAGS_IP = 2024,
        MAGS_GPS_ON = 2025,
        MAGS_GPS_OFF = 2026,
        MAGS_MAX,
    };

    public enum GpsRequestResult
    {
        GPS_REQUEST_RESULT_NA = 0,
        GPS_REQUEST_RESULT_SUCCESS = 0x1,
        GPS_REQUEST_RESULT_ON = 0x2,
        GPS_REQUEST_RESULT_OFF = 0x4,
        GPS_REQUEST_RESULT_UNDEFINED = GPS_REQUEST_RESULT_ON | GPS_REQUEST_RESULT_OFF,
        GPS_REQUEST_RESULT_IN_PROGRESS = 0x8,
        GPS_REQUEST_RESULT_FAILED = 0x10,
        //GPS_REQUEST_RESULT_TIMEOUT = 0x20,
        GPS_REQUEST_RESULT_ON_SUCCESS = GPS_REQUEST_RESULT_ON | GPS_REQUEST_RESULT_SUCCESS,
        GPS_REQUEST_RESULT_ON_FAILED = GPS_REQUEST_RESULT_ON | GPS_REQUEST_RESULT_FAILED,
        GPS_REQUEST_RESULT_ON_IN_PROGRESS = GPS_REQUEST_RESULT_ON | GPS_REQUEST_RESULT_IN_PROGRESS,
        //GPS_REQUEST_RESULT_ON_TIMEOUT = GPS_REQUEST_RESULT_ON | GPS_REQUEST_RESULT_TIMEOUT,
        GPS_REQUEST_RESULT_OFF_SUCCESS = GPS_REQUEST_RESULT_OFF | GPS_REQUEST_RESULT_SUCCESS,
        GPS_REQUEST_RESULT_OFF_FAILED = GPS_REQUEST_RESULT_OFF | GPS_REQUEST_RESULT_FAILED,
        GPS_REQUEST_RESULT_OFF_IN_PROGRESS = GPS_REQUEST_RESULT_OFF | GPS_REQUEST_RESULT_IN_PROGRESS,
        //GPS_REQUEST_RESULT_OFF_TIMEOUT = GPS_REQUEST_RESULT_OFF | GPS_REQUEST_RESULT_TIMEOUT,
    };
}
