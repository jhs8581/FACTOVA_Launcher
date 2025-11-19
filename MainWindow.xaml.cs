using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using System.Linq;
using System.Windows.Threading;
using System.Windows.Documents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FACTOVA_Launcher
{
    public class AppSettings
    {
        public string Language { get; set; } = "en-US";
        public bool StartInDevMode { get; set; } = false;
        public string ConfigPath { get; set; }
        public string GmesVersion { get; set; } = "GMES2";
        public Dictionary<string, string> CustomUrlMappings { get; set; } = new Dictionary<string, string>();
        public int ButtonFontSize { get; set; } = 30;
        public bool AutoBackupEnabled { get; set; } = true; // 기본값: 자동 백업 활성화
    }

    public partial class MainWindow : Window
    {
        private class LogEntry
        {
            public DateTime Timestamp { get; }
            public string MessageKey { get; }
            public object[] Args { get; }

            public LogEntry(string key, params object[] args)
            {
                Timestamp = DateTime.Now;
                MessageKey = key;
                Args = args;
            }
        }

        private DispatcherTimer _monitorTimer;
        private DispatcherTimer _timeTimer;
        private string _currentUnitCode = null;
        private bool _isMainAppRunning = false;
        private const string MainAppProcessName = "FACTOVA.SFC.MainFrame";
        private bool isDevMode = false;

        private AppSettings settings;
        private readonly string settingsPath;
        private Dictionary<string, Dictionary<string, string>> translations;
        private readonly List<LogEntry> logEntries = new List<LogEntry>();
        private readonly Dictionary<string, string> buttonNameMappings = new Dictionary<string, string>
        {
            { "AC_C1", "AC_C1_투입" },
            { "KC_KR3", "KC_KR3_BOX라벨발행" }
        };

        public MainWindow()
        {
            InitializeComponent();
            SetupMonitorTimer();
            SetupTimeTimer();
            InitializeLocalization();

            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            settingsPath = Path.Combine(appDirectory, "launcher.settings.json");
        }

        private void InitializeLocalization()
        {
            translations = new Dictionary<string, Dictionary<string, string>>
            {
                { "Load", new Dictionary<string, string> { { "ko-KR", "불러오기" }, { "en-US", "Load" } } },
                { "BackUp", new Dictionary<string, string> { { "ko-KR", "백업" }, { "en-US", "Back Up" } } },
                { "BackupButtonFormat", new Dictionary<string, string> { { "ko-KR", "{0}으로 백업" }, { "en-US", "Back Up To {0}" } } },
                { "Settings", new Dictionary<string, string> { { "ko-KR", "설정" }, { "en-US", "Settings" } } },
                { "Language", new Dictionary<string, string> { { "ko-KR", "언어" }, { "en-US", "Language" } } },
                { "StartupSettings", new Dictionary<string, string> { { "ko-KR", "시작 설정" }, { "en-US", "Startup Settings" } } },
                { "StartInDevMode", new Dictionary<string, string> { { "ko-KR", "시작 시 개발자 모드로 진입" }, { "en-US", "Start in Developer Mode" } } },
                { "AutoBackupEnabled", new Dictionary<string, string> { { "ko-KR", "프로그램 종료 시 자동 백업" }, { "en-US", "Auto Backup on Program Exit" } } },
                { "Features", new Dictionary<string, string> { { "ko-KR", "Config 폴더" }, { "en-US", "Config Folder" } } },
                { "AddFolder", new Dictionary<string, string> { { "ko-KR", "폴더 추가" }, { "en-US", "Add Folder" } } },
                { "OpenFolder", new Dictionary<string, string> { { "ko-KR", "Config 폴더 바로가기" }, { "en-US", "Open Config Folder" } } },
                { "GmesVersion", new Dictionary<string, string> { { "ko-KR", "GMES 버전" }, { "en-US", "GMES Version" } } },
                { "PatchNotes", new Dictionary<string, string> { { "ko-KR", "패치노트" }, { "en-US", "Patch Notes" } } },
                { "NewFolderTitle", new Dictionary<string, string> { { "ko-KR", "새 폴더 생성" }, { "en-US", "Create New Folder" } } },
                { "FolderCreated", new Dictionary<string, string> { { "ko-KR", "폴더 생성 완료: {0}" }, { "en-US", "Folder created successfully: {0}" } } },
                { "FolderCreationError", new Dictionary<string, string> { { "ko-KR", "폴더 생성 오류: {0}" }, { "en-US", "Folder creation error: {0}" } } },
                { "FolderExistsError", new Dictionary<string, string> { { "ko-KR", "이미 존재하는 폴더 이름입니다: {0}" }, { "en-US", "Folder with that name already exists: {0}" } } },
                { "InvalidFolderNameInput", new Dictionary<string, string> { { "ko-KR", "폴더 이름은 공백이거나 포함할 수 없는 문자를 포함할 수 없습니다." }, { "en-US", "Folder name cannot be empty or contain invalid characters." } } },
                { "ExplorerOpenError", new Dictionary<string, string> { { "ko-KR", "탐색기 열기 오류: {0}" }, { "en-US", "Error opening explorer: {0}" } } },
                { "AppStarted", new Dictionary<string, string> { { "ko-KR", "프로그램이 시작되었습니다." }, { "en-US", "Program started." } } },
                { "BackupCompleted", new Dictionary<string, string> { { "ko-KR", "설정 백업 완료: {0}" }, { "en-US", "Settings backup completed: {0}" } } },
                { "AutoBackupCompleted", new Dictionary<string, string> { { "ko-KR", "자동 백업 완료: {0}" }, { "en-US", "Auto backup completed: {0}" } } },
                { "BackupError", new Dictionary<string, string> { { "ko-KR", "설정 파일 백업 오류: {0}" }, { "en-US", "Settings backup error: {0}" } } },
                { "LauncherReset", new Dictionary<string, string> { { "ko-KR", "런처가 초기 상태로 돌아갑니다." }, { "en-US", "Launcher is reset to initial state." } } },
                { "InvalidFolderName", new Dictionary<string, string> { { "ko-KR", "[경고] 잘못된 폴더 이름은 건너뜁니다: {0}" }, { "en-US", "[Warning] Skipping invalid folder name: {0}" } } },
                { "ConfigPathSettings", new Dictionary<string, string> { { "ko-KR", "Config 경로 설정" }, { "en-US", "Config Path Settings" } } },
                { "ChangePath", new Dictionary<string, string> { { "ko-KR", "경로 변경" }, { "en-US", "Change Path" } } },
                { "EnterNewPath", new Dictionary<string, string> { { "ko-KR", "새로운 Config 경로를 입력하세요:" }, { "en-US", "Enter the new Config path:" } } },
                { "PathUpdated", new Dictionary<string, string> { { "ko-KR", "경로가 업데이트되었습니다." }, { "en-US", "Path has been updated." } } },
                { "InvalidPath", new Dictionary<string, string> { { "ko-KR", "잘못된 경로입니다." }, { "en-US", "Invalid path." } } },
                { "DevModeSwitchError", new Dictionary<string, string> { { "ko-KR", "DEV 모드 전환 오류: {0}" }, { "en-US", "DEV mode switch error: {0}" } } },
                { "LaunchButtonClickError", new Dictionary<string, string> { { "ko-KR", "실행 버튼 클릭 오류: {0}" }, { "en-US", "Launch button click error: {0}" } } },
                { "DevJsonNotFound", new Dictionary<string, string> { { "ko-KR", "JSON 파일을 찾을 수 없음: {0}" }, { "en-US", "JSON file not found: {0}" } } },
                { "DevJsonApplied", new Dictionary<string, string> { { "ko-KR", "'{0}' 설정 적용 완료 (PROPERTIES 비움)" }, { "en-US", "'{0}' settings applied (PROPERTIES cleared)" } } },
                { "DevFileCopyError", new Dictionary<string, string> { { "ko-KR", "파일 복사 오류: {0}" }, { "en-US", "File copy error: {0}" } } },
                { "UpdaterConfigChanged", new Dictionary<string, string> { { "ko-KR", "업데이터 설정 변경: {0}" }, { "en-US", "Updater config changed: {0}" } } },
                { "JsonCopyCompleted", new Dictionary<string, string> { { "ko-KR", "JSON 복사 완료: {0}" }, { "en-US", "JSON copy completed: {0}" } } },
                { "LaunchApp", new Dictionary<string, string> { { "ko-KR", "FACTOVA를 실행합니다." }, { "en-US", "Launching FACTOVA." } } },
                { "UnauthorizedAccess", new Dictionary<string, string> { { "ko-KR", "권한 부족. 관리자 권한으로 실행 필요." }, { "en-US", "Permission denied. Please run as administrator." } } },
                { "ExecutionError", new Dictionary<string, string> { { "ko-KR", "실행 오류: {0}" }, { "en-US", "Execution error: {0}" } } },
                { "ProcessMonitorError", new Dictionary<string, string> { { "ko-KR", "프로세스 모니터링 오류: {0}" }, { "en-US", "Process monitoring error: {0}" } } },
                { "LauncherResetError", new Dictionary<string, string> { { "ko-KR", "런처 상태 초기화 오류: {0}" }, { "en-US", "Launcher reset error: {0}" } } },
                { "CustomizeUrl", new Dictionary<string, string> { { "ko-KR", "URL 커스터마이징" }, { "en-US", "Customize URL" } } },
                { "EnterUrlCode", new Dictionary<string, string> { { "ko-KR", "URL에 사용할 코드를 입력하세요 (예: ac, kc):" }, { "en-US", "Enter URL code (e.g., ac, kc):" } } },
                { "EnterFullUrl", new Dictionary<string, string> { { "ko-KR", "전체 URL을 입력하세요:" }, { "en-US", "Enter full URL:" } } },
                { "UrlCustomized", new Dictionary<string, string> { { "ko-KR", "URL이 '{0}'(으)로 설정되었습니다." }, { "en-US", "URL set to '{0}'." } } },
                { "CurrentUrl", new Dictionary<string, string> { { "ko-KR", "현재 URL: {0}" }, { "en-US", "Current URL: {0}" } } },
                { "FontSize", new Dictionary<string, string> { { "ko-KR", "폰트" }, { "en-US", "Font Size" } } },
                { "DirectLaunch", new Dictionary<string, string> { { "ko-KR", "백업 없이 바로 실행" }, { "en-US", "Launch Without Backup" } } },
                { "DirectLaunchCompleted", new Dictionary<string, string> { { "ko-KR", "백업 없이 실행: {0}" }, { "en-US", "Launched without backup: {0}" } } }
            };
        }

        private string GetLocalizedString(string key, params object[] args)
        {
            if (settings.Language == null)
            {
                settings.Language = "en-US"; // 기본값 설정
            }

            if (translations.ContainsKey(key) && translations[key].ContainsKey(settings.Language))
            {
                return string.Format(translations[key][settings.Language], args);
            }
            return $"[{key}]";
        }

        private string GetLgeSettingsDir()
        {
            if (string.IsNullOrEmpty(settings.ConfigPath))
            {
                return GetDefaultPathForVersion(settings.GmesVersion);
            }
            return settings.ConfigPath;
        }

        private string GetConfigFileName()
        {
            if (settings.GmesVersion == "GMES1")
            {
                return "LGE.SFC.MainFrame.exe.config";
            }
            else // GMES2 or default
            {
                return "FACTOVA.SFC.MainFrame.exe.Setting.json";
            }
        }

        private string GetMainAppProcessName()
        {
            if (settings.GmesVersion == "GMES1")
            {
                return "LGE.SFC.MainFrame";
            }
            else // GMES2 or default
            {
                return "FACTOVA.SFC.MainFrame";
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(settingsPath))
                {
                    string json = File.ReadAllText(settingsPath);
                    settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    settings = new AppSettings();
                }

                if (string.IsNullOrEmpty(settings.GmesVersion))
                {
                    settings.GmesVersion = "GMES2";
                }
                
                if (settings.CustomUrlMappings == null)
                {
                    settings.CustomUrlMappings = new Dictionary<string, string>();
                }
                
                if (settings.ButtonFontSize == 0)
                {
                    settings.ButtonFontSize = 30;
                }

                string expectedPath = GetDefaultPathForVersion(settings.GmesVersion);
                if (settings.ConfigPath != expectedPath)
                {
                    settings.ConfigPath = expectedPath;
                }

                if (settings.GmesVersion == "GMES1")
                {
                    Gmes1RadioButton.IsChecked = true;
                }
                else
                {
                    Gmes2RadioButton.IsChecked = true;
                }

                SaveSettings();
            }
            catch (Exception ex)
            {
                settings = new AppSettings();
                Log("ExecutionError", $"Error loading settings: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(settingsPath, json);
            }
            catch (Exception ex)
            {
                Log("ExecutionError", $"Error saving settings: {ex.Message}");
            }
        }

        private void UpdateUIStrings()
        {
            LoadTab.Header = GetLocalizedString("Load");
            BackupTab.Header = GetLocalizedString("BackUp");
            SettingsTab.Header = GetLocalizedString("Settings");
            PatchNotesTab.Header = GetLocalizedString("PatchNotes");
            LanguageGroupBox.Header = GetLocalizedString("Language");
            StartupGroupBox.Header = GetLocalizedString("StartupSettings");
            DevModeOnStartupCheck.Content = GetLocalizedString("StartInDevMode");
            AutoBackupCheck.Content = GetLocalizedString("AutoBackupEnabled");
            FeaturesGroupBox.Header = GetLocalizedString("Features");
            AddFolderButton.Content = GetLocalizedString("AddFolder");
            OpenFolderButton.Content = GetLocalizedString("OpenFolder");
            GmesVersionGroupBox.Header = GetLocalizedString("GmesVersion");
            FontSizeGroupBox.Header = GetLocalizedString("FontSize");

            if (settings.Language == "ko-KR")
            {
                LangKorRadio.IsChecked = true;
            }
            else
            {
                LangEngRadio.IsChecked = true;
            }
            DevModeOnStartupCheck.IsChecked = settings.StartInDevMode;
            AutoBackupCheck.IsChecked = settings.AutoBackupEnabled;
            
            // 폰트 크기 표시 업데이트
            if (FontSizeTextBlock != null)
            {
                FontSizeTextBlock.Text = settings.ButtonFontSize.ToString();
            }
        }

        private void SetupMonitorTimer()
        {
            _monitorTimer = new DispatcherTimer();
            _monitorTimer.Interval = TimeSpan.FromSeconds(2);
            _monitorTimer.Tick += MonitorTimer_Tick;
        }

        private void SetupTimeTimer()
        {
            _timeTimer = new DispatcherTimer();
            _timeTimer.Interval = TimeSpan.FromSeconds(1);
            _timeTimer.Tick += TimeTimer_Tick;
            _timeTimer.Start();
        }

        private void TimeTimer_Tick(object sender, EventArgs e)
        {
            CurrentTimeTextBlock.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void MonitorTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                string processName = GetMainAppProcessName();
                bool isProcessRunning = Process.GetProcessesByName(processName).Any();

                if (!_isMainAppRunning && isProcessRunning)
                {
                    _isMainAppRunning = true;
                    this.Title = $"FACTOVA 실행기 - {_currentUnitCode} 실행 중";
                }
                else if (_isMainAppRunning && !isProcessRunning)
                {
                    // 자동 백업 설정이 활성화되어 있을 때만 자동 백업 실행
                    if (settings.AutoBackupEnabled)
                    {
                        BackupSettings();
                    }
                    ResetLauncherState();
                }
            }
            catch (Exception ex)
            {
                Log("ProcessMonitorError", ex.Message);
            }
        }

        private void BackupSettings()
        {
            string lgeSettingsDir = GetLgeSettingsDir();
            string configFileName = GetConfigFileName();
            string sourcePath = Path.Combine(lgeSettingsDir, configFileName);
            string destPath = Path.Combine(lgeSettingsDir, _currentUnitCode, configFileName);

            try
            {
                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, destPath, true);
                    Log("AutoBackupCompleted", _currentUnitCode);
                }
            }
            catch (Exception ex)
            {
                Log("BackupError", ex.Message);
                MessageBox.Show(GetLocalizedString("BackupError", ex.Message), "Backup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetLauncherState()
        {
            try
            {
                _monitorTimer.Stop();
                _isMainAppRunning = false;
                _currentUnitCode = null;
                NormalButtonContainer.IsEnabled = true;
                UpdateTitle();
                Log("LauncherReset");
            }
            catch (Exception ex)
            {
                Log("LauncherResetError", ex.Message);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadSettings();
                UpdateUIStrings();
                LogTextBox.Document.Blocks.Clear();

                if (settings.StartInDevMode)
                {
                    SetDevMode(true);
                }
                else
                {
                    SetDevMode(false);
                }

                UpdateTitle();
                LoadBusinessUnitButtons();
                Log("AppStarted");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"프로그램 로딩 중 오류 발생:\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsValidName(string name)
        {
            // Windows에서 허용하지 않는 문자만 체크
            if (string.IsNullOrWhiteSpace(name)) return false;
            
            char[] invalidChars = Path.GetInvalidFileNameChars();
            return !name.Any(c => invalidChars.Contains(c));
        }

        private void LoadBusinessUnitButtons()
        {
            try
            {
                NormalButtonContainer.Children.Clear();
                DevLoadButtonContainer.Children.Clear();
                BackupButtonContainer.Children.Clear();
                DevLoadButton.Visibility = Visibility.Collapsed;
                DevBackupButton.Visibility = Visibility.Collapsed;

                string lgeSettingsDir = GetLgeSettingsDir();

                if (!Directory.Exists(lgeSettingsDir))
                {
                    Directory.CreateDirectory(lgeSettingsDir);
                }

                var directories = Directory.GetDirectories(lgeSettingsDir).ToList();
                var devDir = directories.FirstOrDefault(d => new DirectoryInfo(d).Name.Equals("DEV", StringComparison.OrdinalIgnoreCase));

                if (devDir != null)
                {
                    string dirName = new DirectoryInfo(devDir).Name;
                    DevLoadButton.Content = dirName.Replace("_", "__");
                    DevLoadButton.Tag = dirName;
                    DevLoadButton.Style = (Style)FindResource("DevLoadButtonStyle");
                    DevLoadButton.Visibility = Visibility.Visible;

                    DevBackupButton.Content = GetLocalizedString("BackupButtonFormat", dirName.Replace("_", "__"));
                    DevBackupButton.Tag = dirName;
                    DevBackupButton.Style = (Style)FindResource("BackupButtonStyle");
                    DevBackupButton.Visibility = Visibility.Visible;

                    directories.Remove(devDir);
                }

                foreach (string dir in directories)
                {
                    string dirName = new DirectoryInfo(dir).Name;
                    if (!IsValidName(dirName))
                    {
                        Log("InvalidFolderName", dirName);
                        continue;
                    }

                    string buttonContent = buttonNameMappings.ContainsKey(dirName) ? buttonNameMappings[dirName] : dirName;
                    buttonContent = buttonContent.Replace("_", "__");

                    // 안전한 버튼 이름 생성 (GUID 사용)
                    string safeName = "Btn_" + Guid.NewGuid().ToString("N");

                    Button normalBtn = new Button 
                    { 
                        Name = safeName, 
                        Content = buttonContent, 
                        Tag = dirName,
                        Style = (Style)FindResource("NormalModeLargeButtonStyle"),
                        FontSize = settings.ButtonFontSize
                    };
                    normalBtn.Click += LaunchButton_Click;
                    normalBtn.MouseRightButtonDown += NormalButton_RightClick;
                    NormalButtonContainer.Children.Add(normalBtn);

                    Button devLoadBtn = new Button 
                    { 
                        Name = "DevLoad_" + Guid.NewGuid().ToString("N"), 
                        Content = buttonContent, 
                        Tag = dirName,
                        Style = (Style)FindResource("ModernButtonStyle")
                    };
                    devLoadBtn.Click += LaunchButton_Click;
                    DevLoadButtonContainer.Children.Add(devLoadBtn);

                    Button backupBtn = new Button 
                    { 
                        Name = "Backup_" + Guid.NewGuid().ToString("N"), 
                        Content = GetLocalizedString("BackupButtonFormat", buttonContent), 
                        Tag = dirName,
                        Style = (Style)FindResource("BackupButtonStyle")
                    };
                    backupBtn.Click += BackupButton_Click;
                    BackupButtonContainer.Children.Add(backupBtn);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"버튼 로딩 중 오류 발생:\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton == MouseButton.Left) DragMove();
            }
            catch (InvalidOperationException) { }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SetDevMode(bool devMode)
        {
            isDevMode = devMode;
            UpdateTitle();

            if (isDevMode)
            {
                TitleBar.Background = Brushes.Black;
                this.Foreground = Brushes.White;
                NormalModeView.Visibility = Visibility.Collapsed;
                DevModeTabControl.Visibility = Visibility.Visible;
                
                this.Width = 400;
                this.SizeToContent = SizeToContent.Height;
                Grid mainGrid = (Grid)((Border)this.Content).Child;
                mainGrid.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
                
                if (DevModeTabControl.SelectedItem == SettingsTab)
                {
                    LogTextBox.Visibility = Visibility.Collapsed;
                    LogViewerRow.Height = new GridLength(0);
                }
                else
                {
                    LogTextBox.Visibility = Visibility.Visible;
                    LogViewerRow.Height = new GridLength(120);
                }
            }
            else
            {
                TitleBar.Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0));
                this.Foreground = Brushes.Black;
                NormalModeView.Visibility = Visibility.Visible;
                DevModeTabControl.Visibility = Visibility.Collapsed;
                
                LogTextBox.Visibility = Visibility.Visible;
                LogViewerRow.Height = new GridLength(60);
                
                this.Width = 800;
                this.SizeToContent = SizeToContent.Height;
                Grid mainGrid = (Grid)((Border)this.Content).Child;
                mainGrid.RowDefinitions[1].Height = GridLength.Auto;
            }
            
            LoadBusinessUnitButtons();
        }

        private void TitleTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    // Check if trying to exit dev mode while in GMES 1.0 mode
                    if (isDevMode && settings.GmesVersion == "GMES1")
                    {
                        MessageBox.Show("GMES 1.0 모드에서는 일반 모드로 전환할 수 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                        return; // Do not switch mode
                    }
                    SetDevMode(!isDevMode);
                }
            }
            catch (Exception ex)
            {
                Log("DevModeSwitchError", ex.Message);
            }
        }

        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button clickedButton = sender as Button;
                if (clickedButton == null) return;
                string businessUnit = clickedButton.Tag.ToString();
                LaunchForBusinessUnit(businessUnit);
            }
            catch (Exception ex)
            {
                Log("LaunchButtonClickError", ex.Message);
            }
        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            if (clickedButton == null) return;
            string businessUnit = clickedButton.Tag.ToString();

            try
            {
                string lgeSettingsDir = GetLgeSettingsDir();
                string configFileName = GetConfigFileName();
                string sourcePath = Path.Combine(lgeSettingsDir, configFileName);
                string destPath = Path.Combine(lgeSettingsDir, businessUnit, configFileName);

                if (!File.Exists(sourcePath))
                {
                    Log("BackupError", $"Source file not found: {sourcePath}");
                    MessageBox.Show(GetLocalizedString("BackupError", $"Source file not found: {sourcePath}"), "Backup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                File.Copy(sourcePath, destPath, true);
                Log("BackupCompleted", businessUnit);
            }
            catch (Exception ex)
            {
                Log("BackupError", ex.Message);
                MessageBox.Show(GetLocalizedString("BackupError", ex.Message), "Backup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Language_Changed(object sender, RoutedEventArgs e)
        {
            if (settings == null) return;
            string newLang = LangKorRadio.IsChecked == true ? "ko-KR" : "en-US";
            if (settings.Language != newLang)
            {
                settings.Language = newLang;
                UpdateUIStrings();
                SaveSettings();
                LoadBusinessUnitButtons();
                RenderAllLogs();
            }
        }

        private void DevModeStartup_Changed(object sender, RoutedEventArgs e)
        {
            if (settings == null) return;
            bool newSetting = DevModeOnStartupCheck.IsChecked == true;
            if (settings.StartInDevMode != newSetting)
            {
                settings.StartInDevMode = newSetting;
                SaveSettings();
            }
        }

        private void AutoBackup_Changed(object sender, RoutedEventArgs e)
        {
            if (settings == null) return;
            bool newSetting = AutoBackupCheck.IsChecked == true;
            if (settings.AutoBackupEnabled != newSetting)
            {
                settings.AutoBackupEnabled = newSetting;
                SaveSettings();
            }
        }

        private void GmesVersion_Changed(object sender, RoutedEventArgs e)
        {
            if (settings == null) return;

            string newVersion = Gmes2RadioButton.IsChecked == true ? "GMES2" : "GMES1";
            if (settings.GmesVersion != newVersion)
            {
                settings.GmesVersion = newVersion;
                settings.ConfigPath = GetDefaultPathForVersion(newVersion);
                SaveSettings();
                LoadBusinessUnitButtons();
                UpdateTitle();
                Log("PathUpdated");

                // If switched to GMES1, force Dev Mode
                if (newVersion == "GMES1" && !isDevMode)
                {
                    SetDevMode(true);
                }
            }
        }

        private void UpdateTitle()
        {
            string gmesVersionText = settings.GmesVersion == "GMES1" ? "GMES1.0" : "GMES2.0";
            if (isDevMode)
            {
                TitleTextBlock.Text = $"{gmesVersionText} Only Config Change";
            }
            else
            {
                TitleTextBlock.Text = $"{gmesVersionText} Launcher";
            }
        }

        private string GetDefaultPathForVersion(string version)
        {
            string userPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (version == "GMES1")
            {
                return Path.Combine(userPath, "LG CNS", "ezMES", "Settings");
            }
            else // GMES2 or default
            {
                return Path.Combine(userPath, "FactovaMES", "SFC", "Settings", "LGE GMES");
            }
        }

        private void DevModeTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl tabControl && tabControl.Name == "DevModeTabControl")
            {
                if (tabControl.SelectedItem == SettingsTab)
                {
                    LogTextBox.Visibility = Visibility.Collapsed;
                    LogViewerRow.Height = new GridLength(0);
                    LoadAssemblyInfo(); // 설정 탭이 선택될 때 어셈블리 정보 로드
                }
                else if (tabControl.SelectedItem == PatchNotesTab)
                {
                    LogTextBox.Visibility = Visibility.Collapsed;
                    LogViewerRow.Height = new GridLength(0);
                    LoadPatchNotes(); // 패치노트 탭이 선택될 때 내용 로드
                }
                else
                {
                    LogTextBox.Visibility = Visibility.Visible;
                    LogViewerRow.Height = new GridLength(120);
                }

                if (tabControl.SelectedItem == LoadTab || tabControl.SelectedItem == BackupTab)
                {
                    LoadBusinessUnitButtons();
                }
            }
        }

        private void LoadAssemblyInfo()
        {
            try
            {
                // Get the version of the currently executing assembly (the launcher itself)
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;

                // Display the launcher's version
                AssemblyInfoTextBlock.Text = $"Launcher Version: {version.ToString()}";
            }
            catch (Exception ex)
            {
                AssemblyInfoTextBlock.Text = $"버전 정보를 읽어오는 중 오류 발생:\n{ex.Message}";
            }
        }

        private void LoadPatchNotes()
        {
            try
            {
                string patchNotesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PatchNotes.md");
                if (File.Exists(patchNotesPath))
                {
                    string markdownContent = File.ReadAllText(patchNotesPath, System.Text.Encoding.UTF8);
                    // 간단한 마크다운 파서 (헤더 -> 굵게, 리스트 -> 들여쓰기)
                    var flowDocument = new FlowDocument();
                    var lines = markdownContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                    foreach (var line in lines)
                    {
                        Paragraph p = new Paragraph();
                        if (line.StartsWith("#"))
                        {
                            p.Inlines.Add(new Bold(new Run(line.TrimStart('#', ' '))));
                            p.FontSize = 16;
                        }
                        else if (line.Trim().StartsWith("*") || line.Trim().StartsWith("-"))
                        {
                            p.Inlines.Add(new Run("  • " + line.TrimStart('*', '-', ' ')));
                        }
                        else
                        {
                            p.Inlines.Add(new Run(line));
                        }
                        flowDocument.Blocks.Add(p);
                    }
                    PatchNotesTextBox.Document = flowDocument;
                }
                else
                {
                    PatchNotesTextBox.Document = new FlowDocument(new Paragraph(new Run("PatchNotes.md 파일을 찾을 수 없습니다.")));
                }
            }
            catch (Exception ex)
            {
                PatchNotesTextBox.Document = new FlowDocument(new Paragraph(new Run($"패치노트를 읽어오는 중 오류 발생:\n{ex.Message}")));
            }
        }

        private void AddFolderButton_Click(object sender, RoutedEventArgs e)
        {
            while (true)
            {
                var dialog = new InputBoxRich(this, GetLocalizedString("NewFolderTitle"), "", true);
                if (dialog.ShowDialog() != true)
                {
                    // User cancelled
                    break;
                }

                string folderName = dialog.ResponseText;

                if (string.IsNullOrWhiteSpace(folderName) || folderName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    if (!string.IsNullOrWhiteSpace(folderName))
                    {
                        MessageBox.Show(GetLocalizedString("InvalidFolderNameInput"), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    continue; // Show the dialog again
                }

                try
                {
                    string lgeSettingsDir = GetLgeSettingsDir();
                    string targetPath = Path.Combine(lgeSettingsDir, folderName);

                    if (Directory.Exists(targetPath))
                    {
                        MessageBox.Show(GetLocalizedString("FolderExistsError", folderName), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        continue; // Show the dialog again
                    }
                    else
                    {
                        Directory.CreateDirectory(targetPath);
                        Log("FolderCreated", folderName);
                        LoadBusinessUnitButtons(); // Refresh buttons to show the new folder
                        break; // Success, exit loop
                    }
                }
                catch (Exception ex)
                {
                    Log("FolderCreationError", ex.Message);
                    MessageBox.Show(GetLocalizedString("FolderCreationError", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    break; // Exit loop on other errors
                }
            }
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string targetPath = GetLgeSettingsDir();

                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                }
                Process.Start("explorer.exe", targetPath);
            }
            catch (Exception ex)
            {
                Log("ExplorerOpenError", ex.Message);
                MessageBox.Show(GetLocalizedString("ExplorerOpenError", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LaunchForBusinessUnit(string unitCode)
        {
            LaunchForBusinessUnit(unitCode, true);
        }

        private void LaunchWithoutBackup(string unitCode)
        {
            LaunchForBusinessUnit(unitCode, false);
        }

        private void LaunchForBusinessUnit(string unitCode, bool loadConfig)
        {
            try
            {
                if (settings.GmesVersion == "GMES1" && !isDevMode)
                {
                    MessageBox.Show("GMES 1.0은 개발자 모드에서만 설정 변경이 가능합니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string lgeSettingsDir = GetLgeSettingsDir();
                string configFileName = GetConfigFileName();
                string sourcePath = Path.Combine(lgeSettingsDir, unitCode, configFileName);
                string destPath = Path.Combine(lgeSettingsDir, configFileName);

                if (isDevMode)
                {
                    if (!File.Exists(sourcePath))
                    {
                        Log("DevJsonNotFound", sourcePath);
                        return;
                    }

                    if (settings.GmesVersion == "GMES2")
                    {
                        // GMES 2.0: JSON parsing logic
                        string jsonContent = File.ReadAllText(sourcePath);
                        JObject jsonObj = JObject.Parse(jsonContent);
                        if (jsonObj["MWCONFIG_INFO"]?["PROPERTIES"] != null)
                        {
                            jsonObj["MWCONFIG_INFO"]["PROPERTIES"] = "";
                        }
                        File.WriteAllText(destPath, jsonObj.ToString(Formatting.Indented));
                        Log("DevJsonApplied", unitCode);
                    }
                    else
                    {
                        // GMES 1.0: Simple file copy
                        File.Copy(sourcePath, destPath, true);
                        Log("JsonCopyCompleted", unitCode);
                    }
                    return;
                }

                string updaterDir = @"C:\Program Files (x86)\GMES Shop Floor Control for LGE";
                string updaterConfigPath = Path.Combine(updaterDir, "FACTOVA.Updater.exe.config");
                string updaterExePath = Path.Combine(updaterDir, "FACTOVA.Updater.exe");

                if (!File.Exists(updaterConfigPath) || !File.Exists(updaterExePath))
                {
                    MessageBox.Show("필요한 파일(업데이터)을 찾을 수 없습니다. 경로를 확인하세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Config 파일 로드 여부에 따라 처리
                if (loadConfig)
                {
                    if (!File.Exists(sourcePath))
                    {
                        MessageBox.Show("필요한 파일(설정)을 찾을 수 없습니다. 경로를 확인하세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    XDocument doc = XDocument.Load(updaterConfigPath);
                    XElement deploymentUrlElement = doc.Descendants("appSettings").Elements("add").FirstOrDefault(el => el.Attribute("key")?.Value == "DeploymentURL");
                    if (deploymentUrlElement != null)
                    {
                        string newUrl;
                        
                        // Check if custom URL mapping exists
                        if (settings.CustomUrlMappings != null && settings.CustomUrlMappings.ContainsKey(unitCode))
                        {
                            newUrl = settings.CustomUrlMappings[unitCode];
                        }
                        else
                        {
                            // 기본값: 폴더명 전체를 사용
                            newUrl = $"http://{unitCode.ToLower()}.gmes2.lge.com:8085";
                        }

                        deploymentUrlElement.SetAttributeValue("value", newUrl);
                        doc.Save(updaterConfigPath);
                        Log("UpdaterConfigChanged", newUrl);
                    }

                    File.Copy(sourcePath, destPath, true);
                    Log("JsonCopyCompleted", unitCode);
                }
                else
                {
                    // 백업 없이 실행 - 현재 Config를 그대로 사용
                    Log("DirectLaunchCompleted", unitCode);
                }

                Process.Start(new ProcessStartInfo { FileName = updaterExePath, WorkingDirectory = updaterDir });
                Log("LaunchApp");

                _currentUnitCode = unitCode;
                NormalButtonContainer.IsEnabled = false;
                this.Title = "FACTOVA 실행기 - 프로그램 시작 대기 중...";
                _monitorTimer.Start();
            }
            catch (UnauthorizedAccessException)
            {
                Log("UnauthorizedAccess");
                MessageBox.Show(GetLocalizedString("UnauthorizedAccess"), "권한 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                Log("ExecutionError", ex.Message);
                MessageBox.Show(GetLocalizedString("ExecutionError", ex.Message), "실행 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NormalButton_RightClick(object sender, MouseButtonEventArgs e)
        {
            Button clickedButton = sender as Button;
            if (clickedButton == null) return;
            
            e.Handled = true;
            
            string businessUnit = clickedButton.Tag.ToString();
            string buttonText = clickedButton.Content.ToString().Replace("__", "_");
            
            ContextMenu contextMenu = new ContextMenu();
            
            // 백업 없이 실행 메뉴 항목
            MenuItem directLaunchMenuItem = new MenuItem
            {
                Header = GetLocalizedString("DirectLaunch"),
                Tag = businessUnit,
                FontWeight = FontWeights.Bold
            };
            directLaunchMenuItem.Click += (s, args) =>
            {
                LaunchWithoutBackup(businessUnit);
            };
            
            // 백업 메뉴 항목
            MenuItem backupMenuItem = new MenuItem
            {
                Header = GetLocalizedString("BackupButtonFormat", buttonText),
                Tag = businessUnit
            };
            backupMenuItem.Click += (s, args) =>
            {
                try
                {
                    string lgeSettingsDir = GetLgeSettingsDir();
                    string configFileName = GetConfigFileName();
                    string sourcePath = Path.Combine(lgeSettingsDir, configFileName);
                    string destPath = Path.Combine(lgeSettingsDir, businessUnit, configFileName);

                    if (!File.Exists(sourcePath))
                    {
                        Log("BackupError", "Source file not found: " + sourcePath);
                        MessageBox.Show(GetLocalizedString("BackupError", "Source file not found: " + sourcePath), "Backup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    File.Copy(sourcePath, destPath, true);
                    Log("BackupCompleted", businessUnit);
                    MessageBox.Show(GetLocalizedString("BackupCompleted", businessUnit), "Backup", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Log("BackupError", ex.Message);
                    MessageBox.Show(GetLocalizedString("BackupError", ex.Message), "Backup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            
            // URL 커스터마이징 메뉴 항목
            string currentUrl = "";
            if (settings.CustomUrlMappings != null && settings.CustomUrlMappings.ContainsKey(businessUnit))
            {
                currentUrl = settings.CustomUrlMappings[businessUnit];
            }
            else
            {
                // 기본값: 폴더명 전체를 사용
                currentUrl = $"http://{businessUnit.ToLower()}.gmes2.lge.com:8085";
            }
            
            MenuItem customizeUrlMenuItem = new MenuItem
            {
                Header = $"{GetLocalizedString("CustomizeUrl")} ({currentUrl})",
                Tag = businessUnit
            };
            customizeUrlMenuItem.Click += (s, args) =>
            {
                var dialog = new InputBoxRich(this, GetLocalizedString("EnterFullUrl"), currentUrl, false);
                if (dialog.ShowDialog() == true)
                {
                    string newUrl = dialog.ResponseText.Trim();
                    if (!string.IsNullOrWhiteSpace(newUrl))
                    {
                        if (settings.CustomUrlMappings == null)
                        {
                            settings.CustomUrlMappings = new Dictionary<string, string>();
                        }
                        settings.CustomUrlMappings[businessUnit] = newUrl;
                        SaveSettings();
                        MessageBox.Show(GetLocalizedString("UrlCustomized", newUrl), "URL Customization", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            };
            
            contextMenu.Items.Add(directLaunchMenuItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(backupMenuItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(customizeUrlMenuItem);
            
            clickedButton.ContextMenu = contextMenu;
            contextMenu.IsOpen = true;
        }

        private void RenderLog(LogEntry logEntry)
        {
            try
            {
                string message = GetLocalizedString(logEntry.MessageKey, logEntry.Args);
                Paragraph p = new Paragraph(new Run($"[{logEntry.Timestamp:HH:mm:ss.fff}] {message}"));
                p.Margin = new Thickness(0);

                LogTextBox.Document.Blocks.Add(p);
                LogTextBox.ScrollToEnd();
            }
            catch { }
        }

        private void RenderAllLogs()
        {
            LogTextBox.Document.Blocks.Clear();
            foreach (var logEntry in logEntries)
            {
                RenderLog(logEntry);
            }
        }

        private void Log(string messageKey, params object[] args)
        {
            var logEntry = new LogEntry(messageKey, args);
            logEntries.Add(logEntry);
            RenderLog(logEntry);
        }

        private void FontSizeIncrease_Click(object sender, RoutedEventArgs e)
        {
            if (settings.ButtonFontSize < 72)
            {
                settings.ButtonFontSize++;
                FontSizeTextBlock.Text = settings.ButtonFontSize.ToString();
                SaveSettings();
                if (!isDevMode)
                {
                    LoadBusinessUnitButtons();
                }
            }
        }

        private void FontSizeDecrease_Click(object sender, RoutedEventArgs e)
        {
            if (settings.ButtonFontSize > 8)
            {
                settings.ButtonFontSize--;
                FontSizeTextBlock.Text = settings.ButtonFontSize.ToString();
                SaveSettings();
                if (!isDevMode)
                {
                    LoadBusinessUnitButtons();
                }
            }
        }
    }
}