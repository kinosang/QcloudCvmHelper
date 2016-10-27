using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;
using QcloudSharp;
using static System.Int32;
using Enum = QcloudSharp.Enum;

namespace QcloudCvmHelper
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        private dynamic _client;

        public MainWindow()
        {
            InitializeComponent();

            ComboArea.Items.Add(new AvailabilityZone { Name = "北京一区", ZoneId = "800001", Region = Enum.Region.BJS });
            ComboArea.Items.Add(new AvailabilityZone { Name = "上海一区", ZoneId = "200001", Region = Enum.Region.SHA });
            ComboArea.Items.Add(new AvailabilityZone { Name = "广州一区", ZoneId = "100001", Region = Enum.Region.CAN });
            ComboArea.Items.Add(new AvailabilityZone { Name = "广州二区", ZoneId = "100002", Region = Enum.Region.CAN });
            ComboArea.Items.Add(new AvailabilityZone { Name = "广州三区", ZoneId = "100003", Region = Enum.Region.CAN });
            ComboArea.Items.Add(new AvailabilityZone { Name = "香港一区", ZoneId = "300001", Region = Enum.Region.HKG });
            ComboArea.Items.Add(new AvailabilityZone { Name = "北美一区", ZoneId = "400001", Region = Enum.Region.YTO });

            ComboArea.SelectedIndex = 4;

            TextLog.Text = "请填写SecretId、SecretKey，按顺序执行操作。\n";
        }

        private void DescribeUserInfo_Click(object sender, RoutedEventArgs e)
        {
            var zone = ComboArea.SelectionBoxItem as AvailabilityZone;
            // ReSharper disable once NotResolvedInText
            if (zone == null) throw new ArgumentNullException("AvailabilityZone");

            _client = new QcloudClient
            {
                SecretId = TextSecretId.Text,
                SecretKey = TextSecretKey.Password
            };

            var resultString = _client.DescribeUserInfo(Enum.Endpoint.Trade, zone.Region);
            dynamic result = JsonConvert.DeserializeObject<ApiResult>(resultString);

            try
            {
                if (result.Code == 0)
                {
                    TextLog.Text += "-------用户信息-------\n";
                    TextLog.Text += $"姓名：{result.userInfo.name}\n邮箱：{result.userInfo.mail}\n电话：{result.userInfo.phone}\n";
                    TextLog.Text += "--------获取成功--------\n-请继续获取可用区信息-\n-------------------------\n";
                    DescribeAvailabilityZones.IsEnabled = true;
                }
                else
                {
                    throw new Exception(result.Message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void DescribeAvailabilityZones_Click(object sender, RoutedEventArgs e)
        {
            var zone = ComboArea.SelectionBoxItem as AvailabilityZone;
            // ReSharper disable once NotResolvedInText
            if (zone == null) throw new ArgumentNullException("AvailabilityZone");

            var resultString = _client.DescribeAvailabilityZones(Enum.Endpoint.Cvm, zone.Region, new KeyValuePair<string, string>("zoneId", zone.ZoneId));

            dynamic result = JsonConvert.DeserializeObject<ApiResult>(resultString);

            try
            {
                if (result.Code == 0)
                {
                    foreach (var rZone in result.zoneSet)
                    {
                        TextLog.Text += "---------可用区---------\n";
                        TextLog.Text += $"可用区：{rZone.zoneName}\nzoneId：{rZone.zoneId}\n";
                    }
                    TextLog.Text += "--------获取成功--------\n-------开始摇滚吧-------\n-------------------------\n";
                    Rock.IsEnabled = true;

                }
                else
                {
                    throw new Exception(result.Message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private ApiResult CreateCvm(AvailabilityZone zone, string password)
        {
            var resultString = _client.RunInstancesHour(Enum.Endpoint.Cvm, zone.Region, new[] {
                    new KeyValuePair<string, string>("zoneId", zone.ZoneId),
                    new KeyValuePair<string, string>("cpu", "1"),
                    new KeyValuePair<string, string>("mem", "1"),
                    new KeyValuePair<string, string>("imageId", "img-3wnd9xpl"),
                    new KeyValuePair<string, string>("imageType", "2"),
                    new KeyValuePair<string, string>("bandwidthType", "PayByTraffic"),
                    new KeyValuePair<string, string>("bandwidth", "1"),
                    new KeyValuePair<string, string>("storageSize", "0"),
                    new KeyValuePair<string, string>("password", password),
            });

            return JsonConvert.DeserializeObject<ApiResult>(resultString);
        }

        private ApiResult GetCvmInfo(AvailabilityZone zone, string unInstanceId)
        {
            var resultString = _client.DescribeInstances(Enum.Endpoint.Cvm, zone.Region,
                        new KeyValuePair<string, string>("instanceIds.1", unInstanceId));
            return JsonConvert.DeserializeObject<ApiResult>(resultString);
        }

        private string RandomPassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            const string schars = "()`~!@#$%^&-+=|{}[]:;\',.?/";
            var random = new Random();
            var result = new string(Enumerable.Repeat(chars, length - 2)
              .Select(s => s[random.Next(s.Length)]).ToArray());
            return result + new string(Enumerable.Repeat(schars, 2)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private void Rock_Click(object sender, RoutedEventArgs e)
        {
            var zone = ComboArea.SelectionBoxItem as AvailabilityZone;
            // ReSharper disable once NotResolvedInText
            if (zone == null) throw new ArgumentNullException("AvailabilityZone");

            for (var i = 0; i < Parse(TotalNum.Text); i++)
            {
                try
                {
                    var password = RandomPassword(8);
                    dynamic instanceResult = CreateCvm(zone, password);
                    if (instanceResult.Code == 0)
                    {
                        ListCvm.Items.Add(new InstanceInfo
                        {
                            InstanceId = instanceResult.unInstanceIds.First,
                            Password = password
                        });
                    }
                    else
                    {
                        throw new Exception(instanceResult.Message);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
        }

        private void GetInfo_OnClick(object sender, RoutedEventArgs e)
        {
            var zone = ComboArea.SelectionBoxItem as AvailabilityZone;
            // ReSharper disable once NotResolvedInText
            if (zone == null) throw new ArgumentNullException("AvailabilityZone");

            try
            {
                var instances = new List<InstanceInfo>();
                foreach (var item in ListCvm.Items)
                {
                    var instance = item as InstanceInfo;
                    if (instance == null) continue;
                    dynamic instanceInfo = GetCvmInfo(zone, instance.InstanceId);
                    if (instanceInfo.Code == 0)
                    {
                        var instanceResult = instanceInfo.instanceSet.First;

                        instance.InstanceName = instanceResult.instanceName;
                        instance.WanIpSet = instanceResult.wanIpSet.ToObject<string[]>();

                        instances.Add(instance);
                    }
                    else
                    {
                        throw new Exception(instanceInfo.Message);
                    }
                }

                ListCvm.Items.Clear();
                foreach (var instance in instances)
                {
                    ListCvm.Items.Add(instance);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Export_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "Qcloud",
                DefaultExt = ".txt",
                Filter = "Text (.txt)|*.txt"
            };

            var result = dlg.ShowDialog();

            if (result != true) return;

            var filename = dlg.FileName;

            var text = "";
            foreach (var item in ListCvm.Items)
            {
                var instance = item as InstanceInfo;
                if (instance == null) continue;
                text += $"{instance.WanIpSet.First()}\r\n{instance.Password}\r\n================\r\n";
            }

            System.IO.File.WriteAllText(filename, text);
        }

        private void TextLog_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            TextLog.ScrollToEnd();
        }

        private void TotalNum_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
