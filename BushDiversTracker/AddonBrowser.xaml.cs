using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace BushDiversTracker
{
    /// <summary>
    /// Interaction logic for AddonBrowser.xaml
    /// </summary>
    public partial class AddonBrowser : Window
    {
        internal Services.APIService _api;
        public System.Collections.ObjectModel.ObservableCollection<Models.AddonResource> ResourceList { get; set; } = new();

        public AddonBrowser()
        {
            InitializeComponent();
            txtPath.Text = Services.HelperService.GetPackagePath() ?? "Not set";
            _api = new Services.APIService();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowser = new System.Windows.Forms.FolderBrowserDialog();
            var path = Services.HelperService.GetPackagePath();
            if (path != null)
                folderBrowser.SelectedPath = path;
            else
                folderBrowser.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtPath.Text = folderBrowser.SelectedPath;
                Properties.Settings.Default.CommunityDir = folderBrowser.SelectedPath;
                Properties.Settings.Default.Save();

                RescanAddons();
            }
        }

        private async void RescanAddons()
        {
            if (Properties.Settings.Default.CommunityDir.Length == 0 || !System.IO.Directory.Exists(Properties.Settings.Default.CommunityDir))
                return;

            foreach (var res in ResourceList)
                res.InstalledPackage = null;

            foreach (var addon in System.IO.Directory.EnumerateDirectories(Properties.Settings.Default.CommunityDir))
            {
                if (!System.IO.File.Exists(addon + "\\manifest.json"))
                    continue;

                string manifest = await System.IO.File.ReadAllTextAsync(addon + "\\manifest.json");
                var pkg = System.Text.Json.JsonSerializer.Deserialize<Models.NonApi.InstalledAddon>(manifest, Services.HelperService.SerializerOptions);
                pkg.Filename = new System.IO.DirectoryInfo(addon).Name;

                foreach (var res in ResourceList)
                    if (res.Filename == pkg.Filename)
                    {
                        res.InstalledPackage = pkg;
                        break;
                    }
            }

            UpdateInstallButton();
        }

        private void btnRescan_Click(object sender, RoutedEventArgs e)
        {
            RescanAddons();
        }

        async private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dlProgress.Visibility = Visibility.Hidden;

            var res = await _api.GetAddonResources();
            foreach (var r in res)
                ResourceList.Add(r);

            lstAddons.SelectedIndex = 0;
            RescanAddons();
        }

        private void AddonList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateInstallButton();
        }

        private void UpdateInstallButton()
        {
            btnInstall.IsEnabled = lstAddons.SelectedItem != null;
            var item = (Models.AddonResource)lstAddons.SelectedItem;
            if (item.Install)
                btnInstall.Content = "Remove";
            else
                btnInstall.Content = "Install";
        }


        async private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in ResourceList)
                if (item.Install && item.Version > item.InstalledPackage.Version)
                    await DownloadFile(item);
        }

        async private void btnInstall_Click(object sender, RoutedEventArgs e)
        {
            var item = (Models.AddonResource)lstAddons.SelectedItem;
            if (item.Install)
            {
                if (item.Filename.Length > 0
                    && System.IO.Directory.Exists(Properties.Settings.Default.CommunityDir + "\\" + item.Filename)
                    && MessageBox.Show("Remove the addon '" + item.Title + "'?", "Remove addon?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    System.IO.Directory.Delete(Properties.Settings.Default.CommunityDir + "\\" + item.Filename, true);
            }
            else
                await DownloadFile(item);
            RescanAddons();
        }

        async private Task DownloadFile(Models.AddonResource item)
        {
            if (item.Url.Length == 0)
                return;

            btnInstall.IsEnabled = false;
            btnUpdate.IsEnabled = false;
            btnBrowse.IsEnabled = false;
            btnRescan.IsEnabled = false;

            string tmpFile = "";

            try
            {
                System.Net.WebClient client = new();
                client.DownloadProgressChanged += (sender, e) => { dlProgress.Value = e.ProgressPercentage; };
                tmpFile = System.IO.Path.GetTempFileName();
                
                dlProgress.IsIndeterminate = false;
                dlProgress.Value = 0;
                dlProgress.Visibility = Visibility.Visible;
                await client.DownloadFileTaskAsync(item.Url, tmpFile);
                
                if (System.IO.Directory.Exists(Properties.Settings.Default.CommunityDir + "\\" + item.Filename))
                    System.IO.Directory.Delete(Properties.Settings.Default.CommunityDir + "\\" + item.Filename, true);

                dlProgress.IsIndeterminate = true;
                await Task.Run(() => System.IO.Compression.ZipFile.ExtractToDirectory(tmpFile, Properties.Settings.Default.CommunityDir, true));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error downloading file.\n\n" + ex.Message, "Error");
                if (!System.IO.Directory.Exists(Properties.Settings.Default.CommunityDir + "\\" + item.Filename))
                    item.InstalledPackage = null;
            }

            dlProgress.Visibility = Visibility.Hidden;

            if (tmpFile.Length > 0 && System.IO.File.Exists(tmpFile))
                System.IO.File.Delete(tmpFile);

            btnInstall.IsEnabled = true;
            btnUpdate.IsEnabled = true;
            btnBrowse.IsEnabled = true;
            btnRescan.IsEnabled = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (dlProgress.IsVisible)
            {
                e.Cancel = true;
                MessageBox.Show("Cannot close while downloading addons", "Error");
            }
        }
    }
}
