using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Reflection;
using System.Data.SqlClient;

namespace NetworkManagementSystem
{
    class Program
    {
        static void Main(string[] args)
        {
            IReturn @return = new Return();
            List<Switch> switches = @return.ReturnSwitches(@return.ReturnSQLs());
        }
    }

    class Return : IReturn
    { 
        public List<Sql> ReturnSQLs()
        {
            List<Sql> sqls = new List<Sql>();

            using (var connection = new SqlConnection(@"Data Source=LAPTOP-AHGRSVQL\SQLEXPRESS;Initial Catalog=NetworkManagementSystem;Integrated Security=True;"))
            {
                using (var command = new SqlCommand("Select Event_Id,Switch_Ip,Port_Id,Device_Mac from NetworkEvents", connection))
                {
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                var sql = new Sql
                                {
                                    Event_Id = reader.IsDBNull(reader.GetOrdinal("Event_Id")) ? 0 : reader.GetInt32(reader.GetOrdinal("Event_Id")),
                                    Switch_Ip = reader.IsDBNull(reader.GetOrdinal("Switch_Ip")) ? null : reader.GetString(reader.GetOrdinal("Switch_Ip")),
                                    Port_Id = reader.IsDBNull(reader.GetOrdinal("Port_Id")) ? 0 : reader.GetInt32(reader.GetOrdinal("Port_Id")),
                                    Device_Mac = reader.IsDBNull(reader.GetOrdinal("Device_Mac")) ? null : reader.GetString(reader.GetOrdinal("Device_Mac"))
                                };

                                sqls.Add(sql);
                            }
                        }
                    }

                    connection.Close();
                }
            }

            return sqls;
        }

        public List<Switch> ReturnSwitches(List<Sql> sqls)
        {
            List<Switch> switches = new List<Switch>();

            foreach (Sql sql in sqls)
            {
                int eventID = sql.Event_Id;
                string switchIP = sql.Switch_Ip;
                int port = sql.Port_Id;
                string deviceMAC = sql.Device_Mac;
                Switch theSwitch = switches.Find(s => s.Ip.Equals(switchIP));
                Event theEvent = ReturnEvent(sql);

                if (theSwitch !=  null)
                {
                    theSwitch.Events.Add(theEvent);
                    Port thePort = theSwitch.Ports.Find(p => p.PortNumber == port);

                    if (thePort != null)
                    {
                        thePort.Events.Add(theEvent);

                        if (deviceMAC != null)
                        {
                            Device theDevice = thePort.Devices.Find(d => d.DeviceMAC.Equals(deviceMAC));
                            
                            if (theDevice == null)
                            {
                                thePort.Devices.Add(ReturnDevice(sql, theEvent));
                            }
                        }
                    }

                    else
                    {
                        theSwitch.Ports.Add(new Port(port, new List<Device>() { ReturnDevice(sql, theEvent) }, new List<Event>() { theEvent }));
                    }
                }

                else
                {
                    switches.Add(ReturnSwitch(sql));
                }
            }

            return switches;
        }

        public Switch ReturnSwitch(Sql sql)
        {
            return new Switch(sql.Switch_Ip, new List<Port>() { ReturnPort(sql) }, new List<Event>() { ReturnEvent(sql) });
        }

        public Port ReturnPort(Sql sql)
        {
            Event theEvent = ReturnEvent(sql);
            Device theDevice = ReturnDevice(sql, theEvent);

            if (theDevice != null)
            {
                return new Port(sql.Port_Id, new List<Device>() { theDevice }, new List<Event>() { theEvent });
            }

            else
            {
                return new Port(sql.Port_Id, new List<Device>(), new List<Event>() { theEvent });
            }
        }

        public Device ReturnDevice(Sql sql, Event theEvent)
        {
            if (sql.Device_Mac != null)
            {
                return new Device(sql.Device_Mac, new List<Event>() { theEvent });
            }

            else
            {
                return null;
            }
        }

        public Event ReturnEvent(Sql sql)
        {
            string remark = null;

            switch (sql.Event_Id)
            {
                case 1001:
                    remark = "New device " + sql.Device_Mac + " was added to port " + sql.Port_Id + " of switch " + sql.Switch_Ip;
                    break;
                case 1002:
                    remark = "Device was removed from port " + sql.Port_Id + " of switch " + sql.Switch_Ip;
                    break;
                case 1003:
                    remark = "Port " + sql.Port_Id + " of switch " + sql.Switch_Ip + " was disabled";
                    break;
                default:
                    break;
            }

            return new Event(sql.Event_Id, remark);   
        }
    }

    public class Switch
    {
        public string Ip { get; }
        public List<Port> Ports { get; }
        public List<Event> Events { get; }

        public Switch(string Ip, List<Port> Ports, List<Event> Events)
        {
            this.Ip = Ip;
            this.Ports = Ports;
            this.Events = Events;
        }
    }

    public class Port
    {
        public int PortNumber { get; }
        public List<Device> Devices { get; }
        public List<Event> Events { get; }

        public Port(int PortNumber, List<Device> Devices, List<Event> Events)
        {
            this.PortNumber = PortNumber;
            this.Devices = Devices;
            this.Events = Events;
        }
    }

    public class Device
    {
        public string DeviceMAC { get; }
        public List<Event> DeviceEvents { get; }

        public Device(string DeviceMAC, List<Event> DeviceEvents)
        {
            this.DeviceMAC = DeviceMAC;
            this.DeviceEvents = DeviceEvents;
        }
    }

    public class Event
    {
        public int EventID { get; }
        public string EventRemark { get; }

        public Event(int eventID, string eventRemark)
        {
            this.EventID = eventID;
            this.EventRemark = eventRemark;
        }
    }

    public class Sql
    {
        public int Event_Id { get; set; }
        public string Switch_Ip { get; set; }
        public int Port_Id { get; set; }
        public string Device_Mac { get; set; }

        public Sql() {}

        public Sql(int Event_Id, string Switch_Ip, int Port_Id, string Device_Mac)
        {
            this.Event_Id = Event_Id;
            this.Switch_Ip = Switch_Ip;
            this.Port_Id = Port_Id;
            this.Device_Mac = Device_Mac;
        }
    }

    public interface IReturn
    {
        List<Sql> ReturnSQLs();
        List<Switch> ReturnSwitches(List<Sql> sqls);
        Switch ReturnSwitch(Sql sql);
        Port ReturnPort(Sql sql);
        Device ReturnDevice(Sql sql, Event theEvent);
        Event ReturnEvent(Sql sql);
    }
}
