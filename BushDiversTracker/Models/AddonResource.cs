using System.Text.Json.Serialization;
using System.ComponentModel;

namespace BushDiversTracker.Models
{
    public class AddonCategory
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("category")]
        public string Category { get; set; }
    }

    public class AddonDependency : INotifyPropertyChanged
    {
        [JsonPropertyName("filename")]
        public string Filename { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("mandatory")]
        public bool Mandatory { get; set; }
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonIgnore]
        public bool Found { 
            get { return _found; }
            set
            {
                if (_found != value)
                {
                    _found = value;
                    NotifyPropertyChanged("Found");
                }

            }
        }
        private bool _found;

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }

    public class AddonResource : INotifyPropertyChanged
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("filename")]
        public string Filename { get; set; }
        [JsonPropertyName("author")]
        public string Creator { get; set; }
        [JsonPropertyName("version")]
        public System.Version Version { get; set; }
        [JsonPropertyName("category")]
        public AddonCategory Category { get; set; }
        [JsonPropertyName("url")]
        public string Url { get; set; }
        [JsonPropertyName("calculated_file_size")]
        public string Size { get; set; }
        [JsonPropertyName("dependencies")]
        public AddonDependency[] Dependencies { get; set; }

        [JsonIgnore]
        public Enums.AddonDependencyStatus DependencyInfo {
            get { return _dependencyInfo; }
            set
            {
                if (_dependencyInfo != value)
                {
                    _dependencyInfo = value;
                    NotifyPropertyChanged("DependencyInfo");
                }
            }
        }
        private Enums.AddonDependencyStatus _dependencyInfo;

        [JsonIgnore]
        public bool Install { get { return InstalledPackage != null; } }
        [JsonIgnore]
        internal NonApi.InstalledAddon InstalledPackage {
            get { return _installedPackage; }
            set
            {
                if (_installedPackage != value)
                {
                    _installedPackage = value;
                    NotifyPropertyChanged("Install");
                    NotifyPropertyChanged("NewVer");
                }
            }
        }
        private NonApi.InstalledAddon _installedPackage = null;

        [JsonIgnore]
        public bool NewVer { get {
                if (!Install)
                    return false;
                else if (InstalledPackage.Version >= Version)
                    return false;
                else
                    return true;
            } 
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }

    public class AddonResult
    {
        [JsonPropertyName("resources")]
        public AddonResource[] AddonResources { get; set; }
    }
}
