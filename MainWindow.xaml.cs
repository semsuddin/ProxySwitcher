using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace ProxySwitcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _dispatcherTimer = new DispatcherTimer();
        private const string EVAL_SELECTOR_KEY = "CurrentEnvironment";

        private Dictionary<string, string> UserEnvironmentVariables => GetUserEnvVars();

        private static Dictionary<string, string> GetUserEnvVars()
        {
            var eVars = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

            foreach (DictionaryEntry dictionaryEntry in Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User))
            {
                eVars.Add(dictionaryEntry.Key.ToString().ToLower(), dictionaryEntry.Value.ToString());
            }

            return eVars;
        }

        public MainWindow()
        {
            InitializeComponent();
            ProxyText.Text = ProxyHandler.CheckProxyState() ? "Proxy activated." : "Proxy deactivated.";
            ProxyAddress.Text = string.IsNullOrEmpty(ProxyHandler.GetProxyServer()) ? "10.10.10.10:8080" : ProxyHandler.GetProxyServer();
            EnvironmentSelector.SelectionChanged -= EnvironmentSelector_OnSelectionChanged;
            EnvironmentSelector.SelectedItem = SetBaseEnvDropValue();
            EnvironmentSelector.SelectionChanged += EnvironmentSelector_OnSelectionChanged;

            TimeStart.Text = "";
            TimeEnd.Text = "";
            TimeCalc.Text = "";

            //  DispatcherTimer setup
            _dispatcherTimer.Tick += new EventHandler(UpdateProxyState);
            _dispatcherTimer.Interval = new TimeSpan(0, 0, 10);
            _dispatcherTimer.Start();
        }

        private object SetBaseEnvDropValue()
        {
            string currentEnvValue = UserEnvironmentVariables[EVAL_SELECTOR_KEY];

            return (from ComboBoxItem dropBoxItem 
                    in EnvironmentSelector.Items 
                    let value = dropBoxItem?.Name.ToString() 
                    where value?.ToLower() == currentEnvValue.ToLower() 
                    select dropBoxItem).FirstOrDefault();
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

        //  System.Windows.Threading.DispatcherTimer.Tick handler
        //
        //  Updates the current Proxy state display and calls
        //  InvalidateRequerySuggested on the CommandManager to force 
        //  the Command to raise the CanExecuteChanged event.
        private void UpdateProxyState(object sender, EventArgs e)
        {
            // Updating the Label which displays the current second
            ProxyText.Text = ProxyHandler.CheckProxyState() ? "Proxy activated." : "Proxy deactivated.";

            // Forcing the CommandManager to raise the RequerySuggested event
            CommandManager.InvalidateRequerySuggested();
        }



        private void EnvironmentSelector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems[0] is ComboBoxItem newItemSelected)
            {
                string valueOfSelection = newItemSelected.Name.ToLower();
                Environment.SetEnvironmentVariable(EVAL_SELECTOR_KEY, valueOfSelection, EnvironmentVariableTarget.User);
            }
        }

        private void Button_Exterminate(object sender, RoutedEventArgs e)
        {
            var taskToKill = TaskToKill.Text;
            System.Diagnostics.Process.Start("taskkill", "/F /IM " + taskToKill);
        }

        private void TimeEnd_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var t1 = DateTime.TryParse(TimeStart.Text, new DateTimeFormatInfo(), DateTimeStyles.None, out DateTime t1Result);
            if (!t1) return;
            var t2 = DateTime.TryParse(TimeEnd.Text, new DateTimeFormatInfo(), DateTimeStyles.None, out DateTime t2Result);
            if (!t2) return;
            var result = (t2Result - t1Result);
            TimeCalc.Text = result.ToString(@"hh\:mm") + "h";
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
