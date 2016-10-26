namespace QcloudCvmHelper
{
    public class AvailabilityZone
    {
        public string Name { get; set; }
        public string ZoneId { get; set; }
        public QcloudSharp.Enum.Region Region { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class InstanceInfo
    {
        public string InstanceName { get; set; }
        public string InstanceId { get; set; }
        public string[] WanIpSet { get; set; }
        public string Password { get; set; }

        public override string ToString()
        {
            return (!string.IsNullOrEmpty(WanIpSet?[0])) ? WanIpSet[0]: InstanceId;
        }
    }
}
