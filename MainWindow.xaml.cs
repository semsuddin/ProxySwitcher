using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProxySwitcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ProxyText.Text = ProxyHandler.CheckProxyState() ? "Proxy activated." : "Proxy deactivated.";
            ProxyAddress.Text = string.IsNullOrEmpty(ProxyHandler.GetProxyServer()) ? "10.10.10.10:8080" : ProxyHandler.GetProxyServer();
        }
        private void Button_Activate(object sender, RoutedEventArgs e)
        {
            ProxyHandler.SetProxy(ProxyAddress.Text);
            ProxyText.Text = "Proxy activated.";
        }
        private void Button_Deactivate(object sender, RoutedEventArgs e)
        {
            ProxyHandler.SetProxy(ProxyAddress.Text, false);
            ProxyText.Text = "Proxy deactivated.";
        }
    }
    public static class ProxyHandler
    {
        [DllImport("wininet.dll")]
        public static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        public const int INTERNET_OPTION_REFRESH = 37;
        public const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        public static bool SetProxy(string proxyServerAddress, bool enable = true, bool deleteAutoConfigURL = true)
        {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);

            if (deleteAutoConfigURL && regKey != null && regKey.GetValueNames().Contains("AutoConfigURL"))
            {
                regKey.DeleteValue("AutoConfigURL");
            }

            regKey?.SetValue("ProxyServer", proxyServerAddress);
            regKey?.SetValue("ProxyEnable", Convert.ToInt32(enable));

            var settingsReturn = InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            var refreshReturn = InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);

            return settingsReturn && refreshReturn;
        }
        public static bool CheckProxyState()
        {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
            var proxyEnabled = regKey.GetValue("ProxyEnable");
            return Convert.ToBoolean(proxyEnabled);
        }
        public static string GetProxyServer()
        {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
            var proxyServer = regKey.GetValue("ProxyServer");
            return Convert.ToString(proxyServer);
        }
    }
}
