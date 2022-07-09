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
        public System.Collections.ObjectModel.ObservableCollection<Models.AddonResource> ResourceList { get; } = new();
        internal List<string> OfficialPackageList { get; } = new();

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

        /// <summary>
        /// Rescan over addon list to re-check installed files, deps, etc
        /// </summary>
        private async void RescanAddons()
        {
            if (Properties.Settings.Default.CommunityDir.Length == 0 || !System.IO.Directory.Exists(Properties.Settings.Default.CommunityDir))
                return;

            // Test official path and cache result
            var officialPath = Services.HelperService.GetOfficialPath();
            if (OfficialPackageList.Count == 0 && officialPath != null && System.IO.Directory.Exists(officialPath))
            {
                foreach (var addon in System.IO.Directory.EnumerateDirectories(officialPath))
                {
                    if (!System.IO.File.Exists(addon + "\\manifest.json"))
                        continue;

                    // Don't need to parse json (yet), just assume a dir with a manifest is an addon
                    try
                    {
                        OfficialPackageList.Add(new System.IO.DirectoryInfo(addon).Name.ToLower());
                    }
                    catch { } // Ignore
                }
            }

            foreach (var res in ResourceList)
                res.InstalledPackage = null;

            // Scan community package list
            List<string> packageList = new();
            foreach (var addon in System.IO.Directory.EnumerateDirectories(Properties.Settings.Default.CommunityDir))
            {
                if (!System.IO.File.Exists(addon + "\\manifest.json"))
                    continue;

                try
                {
                    string manifest = await System.IO.File.ReadAllTextAsync(addon + "\\manifest.json");
                    var pkg = System.Text.Json.JsonSerializer.Deserialize<Models.NonApi.InstalledAddon>(manifest, Services.HelperService.SerializerOptions);
                    pkg.Filename = new System.IO.DirectoryInfo(addon).Name.ToLower();

                    packageList.Add(pkg.Filename);

                    foreach (var res in ResourceList)
                        if (res.Filename == pkg.Filename)
                        {
                            res.InstalledPackage = pkg;
                            break;
                        }
                }
                catch { } // Ignore
            }

            // Resolve dependencies
            foreach (var res in ResourceList)
            {
                if (res.Dependencies == null)
                {
                    res.DependencyInfo = Models.Enums.AddonDependencyStatus.OK;
                    continue;
                }

                bool foundMandatory = true;
                bool foundOptional = true;

                foreach (var dep in res.Dependencies)
                {
                    if (!packageList.Contains(dep.Filename) && !OfficialPackageList.Contains(dep.Filename))
                    {
                        dep.Found = false;

                        if (dep.Mandatory)
                            foundMandatory = false;
                        else
                            foundOptional = false;
                    }
                    else
                        dep.Found = true;
                }

                if (foundMandatory && foundOptional)
                    res.DependencyInfo = Models.Enums.AddonDependencyStatus.OK;
                else if (foundMandatory)
                    res.DependencyInfo = Models.Enums.AddonDependencyStatus.MissingOptional;
                else
                    res.DependencyInfo = Models.Enums.AddonDependencyStatus.MissingMandatory;
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
            bool updateAvail = false;
            foreach (var r in res)
            {
                r.Filename = r.Filename.ToLower();
                if (r.Install && r.NewVer)
                    updateAvail = true;

                ResourceList.Add(r);
            }
            btnUpdate.IsEnabled = updateAvail;
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
            btnInstall.Visibility = !item.Install || item.NewVer ? Visibility.Visible : Visibility.Hidden;
            if (item.NewVer)
                btnInstall.Content = "Update";
            else
                btnInstall.Content = "Install";

            btnRemove.Visibility = item.Install ? Visibility.Visible : Visibility.Hidden;
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
            bool installDeps = false;
            if (item.DependencyInfo == Models.Enums.AddonDependencyStatus.MissingMandatory)
            {
                if (item.Dependencies.Any((e) => e.Url?.Length == 0))
                {
                    var mbResult = MessageBox.Show("This addon requires additional dependencies.\n\nAlso install dependencies?", "Install dependencies?", MessageBoxButton.YesNoCancel);
                    if (mbResult == MessageBoxResult.Cancel)
                        return;
                    else
                        installDeps = mbResult == MessageBoxResult.Yes;
                }
            }

            await InstallPackage(item, installDeps);

            if (item.DependencyInfo == Models.Enums.AddonDependencyStatus.MissingMandatory
                && item.Dependencies.Any((e) => e.Url?.Length > 0))
                MessageBox.Show("Some dependencies must be installed externally. Check the dependency list for details.", "External dependencies", MessageBoxButton.OK);
        }

        async private Task InstallPackage(Models.AddonResource pkg, bool installDeps)
        {
            await DownloadFile(pkg);
            RescanAddons(); // rescan here to break dependency loop

            if (installDeps)
            {
                if (pkg.Dependencies == null)
                    return;

                foreach (var dep in pkg.Dependencies)
                {
                    // Don't need to install what's already installed or external
                    if (dep.Found || dep.Url.Length > 0)
                        continue;

                    try
                    {
                        await InstallPackage(ResourceList.First((p) => p.Filename.Equals(dep.Filename)), true);
                    }
                    catch { }
                }
            }
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

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            var item = (Models.AddonResource)lstAddons.SelectedItem;
            if (item.Filename.Length > 0
                && System.IO.Directory.Exists(Properties.Settings.Default.CommunityDir + "\\" + item.Filename)
                && MessageBox.Show("Remove the addon '" + item.Title + "'?", "Remove addon?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                System.IO.Directory.Delete(Properties.Settings.Default.CommunityDir + "\\" + item.Filename, true);
                RescanAddons();
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                var link = (System.Windows.Documents.Hyperlink)sender;
                var item = (Models.AddonDependency)link.DataContext;
                if (item.Url.Length > 0)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(item.Url) { UseShellExecute = true });
                    e.Handled = true;
                }
            }
            catch
            {

            }
        }
    }
}
