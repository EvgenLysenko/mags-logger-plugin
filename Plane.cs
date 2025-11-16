using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagsLogger
{
    public class Plane
    {
        public float batteryVoltage = 0;
        public float batteryCurrent = 0;
        public float batteryRemainingPc = 0;

        public void onMavlinkMessageReceived(object sender, MAVLink.MAVLinkMessage message)
        {
            switch ((MAVLink.MAVLINK_MSG_ID)message.msgid)
            {
                case MAVLink.MAVLINK_MSG_ID.SYS_STATUS:
                {
                    MAVLink.mavlink_sys_status_t sys_status = (MAVLink.mavlink_sys_status_t)message.data;
                    this.batteryVoltage = sys_status.voltage_battery / 1000.0f;
                    this.batteryCurrent = sys_status.current_battery / 100.0f;
                    this.batteryRemainingPc = sys_status.battery_remaining;
                    break;
                }
            }
            }
        }
}
