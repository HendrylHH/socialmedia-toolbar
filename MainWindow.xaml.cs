using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.Runtime.InteropServices;
using System.Windows.Documents;

using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using Image = System.Windows.Controls.Image;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using Brushes = System.Windows.Media.Brushes;
using Cursors = System.Windows.Input.Cursors;
using FontFamily = System.Windows.Media.FontFamily;
using TextBox = System.Windows.Controls.TextBox;
using ToolTip = System.Windows.Controls.ToolTip;
using MessageBox = System.Windows.MessageBox;
using Orientation = System.Windows.Controls.Orientation;

namespace ToolbarApp
{
    public class UserSettings
    {
        public bool FirstClickDone { get; set; } = false;
        public double WindowTop { get; set; } = 0;
        public double WindowLeft { get; set; } = 0;
        public double WindowWidth { get; set; } = 50;
        public double WindowHeight { get; set; } = 0;
        public string LastUrl { get; set; } = "";
        public Dictionary<string, WebViewPosition> WindowPositions { get; set; } = new Dictionary<string, WebViewPosition>();
        public List<SocialMediaPlatformDto> CustomPlatforms { get; set; } = new List<SocialMediaPlatformDto>();
    }

    public class WebViewPosition
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public WebViewPosition() { }

        public WebViewPosition(double left, double top, double width, double height)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }
    }

    public class SocialMediaPlatformDto
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public string IconPath { get; set; } = "";
        
        public SocialMediaPlatformDto() { }
        
        public SocialMediaPlatformDto(SocialMediaPlatform platform)
        {
            Name = platform.Name;
            Url = platform.Url;
            IconPath = platform.IconPath;
        }
        
        public SocialMediaPlatform ToSocialMediaPlatform()
        {
            return new SocialMediaPlatform(Name, Url, IconPath);
        }
    }

    public class SocialMediaPlatform
    {
        public string Name { get; }
        public string Url { get; }
        public string IconPath { get; }
        public Color? AccentColor { get; }

        public SocialMediaPlatform(string name, string url, string iconPath, Color? accentColor = null)
        {
            Name = name;
            Url = url;
            IconPath = iconPath;
            AccentColor = accentColor;
        }
    }

    public partial class MainWindow : Window
    {
        private WebView2 webView = null!;
        
        private UserSettings userSettings = new UserSettings();
        
        private Dictionary<string, (double left, double top, double width, double height)> webViewPositions = 
            new Dictionary<string, (double, double, double, double)>();
        
        private string currentUrl = "";

        private NotifyIcon? notifyIcon = null;

        private const double ButtonOnlyWindowWidth = 50;
        private const double CollapsedWindowWidth = 50;
        private const double ExpandedWindowWidth = 350;
        private const double EnhancedExpandedWindowWidth = 700;
        private const double SideBarStripWidth = 50;
        private const int ResizeBorderThickness = 10;
        
        private bool isFirstIconClick = true;
        
        private const string LogoPath = "icons/logo.png";

        private readonly Color _buttonOnlyBackgroundColor = Color.FromArgb(255, 20, 22, 25);
        private readonly Color _toolbarBaseColor = Color.FromArgb(255, 20, 22, 25);
        private readonly Color _toolbarHoverColor = Color.FromArgb(255, 25, 27, 30);
        private readonly Color _webViewBackgroundColor = Color.FromArgb(255, 15, 17, 20);
        private readonly Color _webViewBorderColor = Color.FromArgb(255, 40, 42, 45);
        private readonly Color _buttonHoverHighlightColor = Color.FromArgb(255, 35, 37, 40);
        private readonly Color _toggleButtonForeground = Colors.WhiteSmoke;
        private readonly Color _toggleButtonInitialBackground = Colors.Transparent;
        private readonly Color _toggleButtonHoverBackground = Color.FromArgb(255, 35, 37, 40);

        private Image? appLogoImage = null;
        private readonly List<Button> socialMediaButtons = new List<Button>();
        private StackPanel? mainToolbarStackPanel = null;
        private StackPanel? mainContentPanel = null;
        private Border? mainToolbarContainer = null;
        private Separator? logoSeparator = null;
        private Separator? socialMediaGroupSeparator = null;

        private readonly Color _separatorColor = Color.FromArgb(80, 60, 62, 65);
        
        private static readonly Color _accentPink = Color.FromArgb(255, 233, 51, 161);
        private static readonly Color _accentPurple = Color.FromArgb(255, 141, 71, 235);
        private static readonly Color _accentGreen = Color.FromArgb(255, 0, 220, 130);
        private static readonly Color _accentBlue = Color.FromArgb(255, 0, 174, 255);

        private readonly List<SocialMediaPlatform> _defaultSocialMediaPlatforms = new List<SocialMediaPlatform>
        {
            new SocialMediaPlatform("Twitch", "https://www.twitch.tv", "icons/twitter.png", _accentPurple),
            new SocialMediaPlatform("WhatsApp", "https://web.whatsapp.com", "icons/whatsapp.png", _accentGreen),
            new SocialMediaPlatform("Telegram", "https://web.telegram.org", "icons/telegram.png", _accentBlue),
        };

        private readonly List<SocialMediaPlatform> _defaultAdditionalPlatforms = new List<SocialMediaPlatform>
        {
            new SocialMediaPlatform("YouTube", "https://www.youtube.com", "icons/twitter.png", _accentPink),
        };

        private readonly List<SocialMediaPlatform> socialMediaPlatforms = new List<SocialMediaPlatform>();
        private readonly List<SocialMediaPlatform> additionalPlatforms = new List<SocialMediaPlatform>();


        public MainWindow()
        {
            socialMediaPlatforms.AddRange(_defaultSocialMediaPlatforms);
            additionalPlatforms.AddRange(_defaultAdditionalPlatforms);

            LoadSettings();
            
            ConfigureWindow();
            
            isFirstIconClick = !userSettings.FirstClickDone;
            
            CreateUI();
            InitializeWebView();
            
            SetupSystemTrayIcon();
            
            this.Closing += MainWindow_Closing;
            
            this.LocationChanged += MainWindow_LocationChanged;
            this.SizeChanged += MainWindow_SizeChanged;
            
            this.ShowInTaskbar = false;
        }
        
        private void MainWindow_LocationChanged(object? sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(currentUrl) && FindToggleButton()?.Tag as string == "WebViewExpanded")
            {
                SaveWebViewPosition();
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(currentUrl) && FindToggleButton()?.Tag as string == "WebViewExpanded")
            {
                SaveWebViewPosition();
            }
        }
        
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
        }
        
        private void LoadSettings()
        {
            try
            {
                string settingsFilePath = "user_settings.json";
                if (File.Exists(settingsFilePath))
                {
                    string json = File.ReadAllText(settingsFilePath);
                    var loadedSettings = JsonSerializer.Deserialize<UserSettings>(json);
                    if (loadedSettings != null)
                    {
                        userSettings = loadedSettings;
                        
                        if (userSettings.WindowHeight > 0)
                        {
                            double screenWidth = SystemParameters.WorkArea.Width;
                            double screenHeight = SystemParameters.WorkArea.Height;
                            
                            double left = Math.Max(0, Math.Min(userSettings.WindowLeft, screenWidth - 50));
                            double top = Math.Max(0, Math.Min(userSettings.WindowTop, screenHeight - 50));
                            double width = Math.Min(userSettings.WindowWidth, screenWidth);
                            double height = Math.Min(userSettings.WindowHeight, screenHeight);
                            
                            this.Left = left;
                            this.Top = top;
                            if (userSettings.FirstClickDone)
                            {
                                this.Width = width;
                                this.Height = height;
                            }
                        }
                        
                        if (userSettings.CustomPlatforms != null && userSettings.CustomPlatforms.Count > 0)
                        {
                            foreach (var platformDto in userSettings.CustomPlatforms)
                            {
                                var customPlatform = platformDto.ToSocialMediaPlatform();
                                socialMediaPlatforms.RemoveAll(p => p.Name == customPlatform.Name);
                                additionalPlatforms.RemoveAll(p => p.Name == customPlatform.Name);
                                socialMediaPlatforms.Add(customPlatform);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar configurações: {ex.Message}");
                userSettings = new UserSettings();
            }
        }
        
        private void SaveSettings()
        {
            try
            {
                userSettings.FirstClickDone = !isFirstIconClick;
                userSettings.WindowWidth = this.Width;
                userSettings.WindowHeight = this.Height;
                userSettings.WindowTop = this.Top;
                userSettings.WindowLeft = this.Left;
                userSettings.LastUrl = currentUrl;
                
                userSettings.CustomPlatforms.Clear();
                foreach (var platform in socialMediaPlatforms.Concat(additionalPlatforms))
                {
                    bool isOriginallyDefault = _defaultSocialMediaPlatforms.Any(dp => dp.Name == platform.Name && dp.Url == platform.Url && dp.IconPath == platform.IconPath && dp.AccentColor == platform.AccentColor) ||
                                               _defaultAdditionalPlatforms.Any(dp => dp.Name == platform.Name && dp.Url == platform.Url && dp.IconPath == platform.IconPath && dp.AccentColor == platform.AccentColor);

                    if (platform.AccentColor == null || !isOriginallyDefault)
                    {
                        userSettings.CustomPlatforms.Add(new SocialMediaPlatformDto(platform));
                    }
                }
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string json = JsonSerializer.Serialize(userSettings, options);
                File.WriteAllText("user_settings.json", json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao salvar configurações: {ex.Message}");
            }
        }
        
        private void SaveWebViewPosition()
        {
            if (string.IsNullOrEmpty(currentUrl))
                return;
                
            userSettings.WindowPositions[currentUrl] = new WebViewPosition(
                this.Left, 
                this.Top, 
                this.Width, 
                this.Height
            );
        }

        private void ConfigureWindow()
        {
            this.WindowState = WindowState.Normal;
            this.WindowStyle = WindowStyle.None;
            
            double workAreaHeight = SystemParameters.WorkArea.Height;
            double workAreaTop = SystemParameters.WorkArea.Top;
            
            this.Top = workAreaTop;
            this.Left = 0;
            this.Height = workAreaHeight;
            this.Width = ButtonOnlyWindowWidth;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.Topmost = true;
            this.ResizeMode = ResizeMode.NoResize;
            
            this.MouseMove += Window_MouseMove;
            this.MouseDown += Window_MouseDown;
            this.MouseUp += Window_MouseUp;
            
            this.PreviewMouseLeftButtonDown += (s, e) => 
            {
                if (e.OriginalSource is Button || FindParentOfType<Button>(e.OriginalSource as DependencyObject) != null)
                {
                    return;
                }
                
                var toggleBtn = FindToggleButton();
                if (toggleBtn != null && toggleBtn.Tag as string == "ButtonOnly")
                {
                    this.DragMove();
                    e.Handled = true;
                }
            };
        }
        
        private T? FindParentOfType<T>(DependencyObject? element) where T : DependencyObject
        {
            if (element == null) return null;
            
            DependencyObject parent = VisualTreeHelper.GetParent(element);
            if (parent == null) return null;
            
            if (parent is T typedParent)
                return typedParent;
                
            return FindParentOfType<T>(parent);
        }

        private void CreateUI()
        {
            Grid mainGrid = new Grid();
            this.Content = mainGrid;

            ColumnDefinition toolbarColumn = new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star) 
            };
            ColumnDefinition contentColumn = new ColumnDefinition
            {
                Width = new GridLength(0, GridUnitType.Pixel) 
            };
            mainGrid.ColumnDefinitions.Add(toolbarColumn);
            mainGrid.ColumnDefinitions.Add(contentColumn);

            Grid toolbarGrid = new Grid();
            
            toolbarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            toolbarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36, GridUnitType.Pixel) });
            toolbarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            mainToolbarContainer = new Border
            {
                Background = new SolidColorBrush(Colors.Transparent),
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                CornerRadius = new CornerRadius(0),
                BorderThickness = new Thickness(0),
                Effect = new DropShadowEffect
                {
                    Color = Color.FromArgb(80, 0, 0, 0),
                    Direction = 270,
                    ShadowDepth = 2,
                    BlurRadius = 10,
                    Opacity = 0.3
                },
                Child = toolbarGrid
            };
            Grid.SetColumn(mainToolbarContainer, 0);
            mainGrid.Children.Add(mainToolbarContainer);

            Button toggleButton = new Button
            {
                Name = "MainToggleButton",
                Width = 36,
                Height = 36,
                Margin = new Thickness(7, 0, 7, 0),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Background = new SolidColorBrush(_toggleButtonInitialBackground),
                BorderThickness = new Thickness(0),
                Style = CreateRoundButtonStyle(),
                Tag = "ButtonOnly"
            };

            var arrowIcon = CreateArrowIcon(">");
            toggleButton.Content = arrowIcon;
            
            Grid.SetRow(toggleButton, 1);
            toolbarGrid.Children.Add(toggleButton);

            toggleButton.Click += (s, e) => 
            {
                try
                {
                    string currentState = toggleButton.Tag as string ?? "ButtonOnly";
                    if (currentState == "ButtonOnly")
                    {
                        toggleButton.Content = CreateArrowIcon("<");
                    }
                    else
                    {
                        toggleButton.Content = CreateArrowIcon(">");
                    }
                    
                    var opacityAnimation = new System.Windows.Media.Animation.DoubleAnimation
                    {
                        From = 1.0,
                        To = 0.5,
                        AutoReverse = true,
                        Duration = TimeSpan.FromSeconds(0.15)
                    };
                    toggleButton.BeginAnimation(OpacityProperty, opacityAnimation);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro na animação: {ex.Message}");
                }
                
                ToggleToolbar(s, e);
            };
            
            toggleButton.MouseEnter += (s, e) => 
            { 
                toggleButton.Background = new SolidColorBrush(_toggleButtonHoverBackground);
                
                try
                {
                    ScaleTransform scaleTransform = new ScaleTransform(1.1, 1.1);
                    toggleButton.RenderTransform = scaleTransform;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro na animação hover: {ex.Message}");
                }
            };
            
            toggleButton.MouseLeave += (s, e) => 
            { 
                toggleButton.Background = new SolidColorBrush(_toggleButtonInitialBackground);
                
                try
                {
                    ScaleTransform scaleTransform = new ScaleTransform(1.0, 1.0);
                    toggleButton.RenderTransform = scaleTransform;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro na animação hover out: {ex.Message}");
                }
            };

            DockPanel mainToolbarDockPanel = new DockPanel
            {
                LastChildFill = true,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            
            mainToolbarDockPanel.Visibility = Visibility.Collapsed;
            
            mainToolbarStackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Top
            };
            
            Grid.SetRow(mainToolbarDockPanel, 0);
            Grid.SetRowSpan(mainToolbarDockPanel, 3);
            toolbarGrid.Children.Add(mainToolbarDockPanel);
            
            mainToolbarContainer.MouseEnter += (s, e) =>
            {
                var toggleBtn = FindToggleButton();
                if (toggleBtn != null && toggleBtn.Tag as string == "WebViewExpanded") 
                {
                    mainToolbarContainer.Background = new SolidColorBrush(_toolbarHoverColor);
                }
            };
            
            mainToolbarContainer.MouseLeave += (s, e) =>
            {
                var toggleBtn = FindToggleButton();
                if (toggleBtn != null && toggleBtn.Tag as string == "WebViewExpanded")
                {
                    mainToolbarContainer.Background = new SolidColorBrush(_toolbarBaseColor);
                }
            };

            var settingsButton = CreateSettingsButton();
            settingsButton.Visibility = Visibility.Visible;
            settingsButton.Margin = new Thickness(5, 20, 5, 20);
            
            Separator bottomSeparator = new Separator
            {
                Height = 1,
                Background = new SolidColorBrush(_separatorColor),
                Margin = new Thickness(10, 5, 10, 5)
            };
            
            StackPanel settingsContainer = new StackPanel
            {
                Orientation = Orientation.Vertical
            };
            settingsContainer.Children.Add(bottomSeparator);
            settingsContainer.Children.Add(settingsButton);
            
            DockPanel.SetDock(settingsContainer, Dock.Bottom);
            mainToolbarDockPanel.Children.Add(settingsContainer);
            
            mainContentPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Top
            };
            
            mainToolbarDockPanel.Children.Add(mainContentPanel);
            
            try
            {
                appLogoImage = new Image
                {
                    Source = new BitmapImage(new Uri(LogoPath, UriKind.Relative)),
                    Width = 28, Height = 28,
                    Margin = new Thickness(11, 10, 11, 10),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Visibility = Visibility.Visible
                };

                logoSeparator = new Separator
                {
                    Height = 1,
                    Background = new SolidColorBrush(_separatorColor),
                    Margin = new Thickness(10, 5, 10, 5),
                    Visibility = Visibility.Visible
                };
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error loading logo or creating logo separator: {ex.Message}"); }
            
            socialMediaGroupSeparator = new Separator
            {
                Height = 1,
                Background = new SolidColorBrush(_separatorColor),
                Margin = new Thickness(10, 5, 10, 5),
                Visibility = Visibility.Visible
            };

            RefreshSocialMediaButtonsInternal();
            
            Border webViewContainer = new Border
            {
                Background = new SolidColorBrush(_webViewBackgroundColor),
                BorderBrush = new SolidColorBrush(_webViewBorderColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(0, 8, 8, 0),
                Visibility = Visibility.Collapsed
            };

            webView = new WebView2();
            webViewContainer.Child = webView;
            Grid.SetColumn(webViewContainer, 1);
            mainGrid.Children.Add(webViewContainer);
        }

        private void ToggleToolbar(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var mainGrid = this.Content as Grid;
            if (mainToolbarContainer == null || mainGrid == null) return;

            var webViewContainer = mainGrid.Children.OfType<Border>().FirstOrDefault(b => Grid.GetColumn(b) == 1);
            var dockPanel = mainToolbarContainer.Child is Grid toolbarGrid
                ? toolbarGrid.Children.OfType<DockPanel>().FirstOrDefault()
                : null;
            
            string currentState = button.Tag as string ?? "ButtonOnly";
            System.Diagnostics.Debug.WriteLine($"Alternando toolbar de {currentState}");

            if (currentState == "ButtonOnly")
            {
                this.Width = ExpandedWindowWidth;
                mainGrid.ColumnDefinitions[0].Width = new GridLength(SideBarStripWidth, GridUnitType.Pixel);
                mainGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);

                button.Tag = "WebViewExpanded";

                if (dockPanel != null)
                {
                    dockPanel.Visibility = Visibility.Visible;
                    System.Diagnostics.Debug.WriteLine("DockPanel foi tornado visível");
                }
                
                if (webViewContainer != null)
                {
                    webViewContainer.Visibility = Visibility.Visible;
                    System.Diagnostics.Debug.WriteLine("WebViewContainer foi tornado visível");
                }

                mainToolbarContainer.Background = new SolidColorBrush(_toolbarBaseColor);
                mainToolbarContainer.CornerRadius = new CornerRadius(0);

                var effect = new DropShadowEffect
                {
                    Color = Colors.White,
                    Direction = 0,
                    ShadowDepth = 0,
                    BlurRadius = 10,
                    Opacity = 0.3
                };
                button.Effect = effect;
            }
            else if (currentState == "WebViewExpanded")
            {
                this.Width = ButtonOnlyWindowWidth;
                button.Tag = "ButtonOnly";

                if (dockPanel != null)
                {
                    dockPanel.Visibility = Visibility.Collapsed;
                    System.Diagnostics.Debug.WriteLine("DockPanel foi ocultado");
                }
                
                if (webViewContainer != null)
                {
                    webViewContainer.Visibility = Visibility.Collapsed;
                    System.Diagnostics.Debug.WriteLine("WebViewContainer foi ocultado");
                }
                
                var glowEffect = new DropShadowEffect
                {
                    Color = Color.FromArgb(150, 200, 200, 255),
                    Direction = 0,
                    ShadowDepth = 0,
                    BlurRadius = 15,
                    Opacity = 0.7
                };
                button.Effect = glowEffect;
                
                mainToolbarContainer.Background = new SolidColorBrush(Colors.Transparent);
                mainToolbarContainer.BorderThickness = new Thickness(0);
                mainToolbarContainer.CornerRadius = new CornerRadius(0);

                mainGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star); 
                mainGrid.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel);
            }
        }

        private async void InitializeWebView()
        {
            try { await webView.EnsureCoreWebView2Async(); }
            catch (Exception ex) { MessageBox.Show($"Failed to initialize WebView2: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private Button CreateToolbarButton(string tooltip, string url, string iconPath, Color? accentColor = null)
        {
            Button button = new Button
            {
                Width = 40, Height = 40,
                Margin = new Thickness(5),
                Background = Brushes.Transparent, 
                ToolTip = tooltip, 
                BorderThickness = new Thickness(0)
            };
            try
            {
                string fixedIconPath = iconPath;
                
                if (!System.IO.Path.IsPathRooted(iconPath) && !iconPath.Contains('/') && !iconPath.Contains('\\'))
                {
                    fixedIconPath = "icons/" + iconPath;
                }
                
                System.Diagnostics.Debug.WriteLine($"Tentando carregar ícone de: {fixedIconPath}");
                
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(fixedIconPath, UriKind.RelativeOrAbsolute);
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmapImage.EndInit();
                
                Image icon = new Image { 
                    Source = bitmapImage, 
                    VerticalAlignment = VerticalAlignment.Center, 
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center, 
                    Width = 24, Height = 24
                };
                
                if (accentColor.HasValue)
                {
                    Border coloredIconContainer = new Border
                    {
                        Width = 24, Height = 24,
                        Background = new SolidColorBrush(accentColor.Value),
                        CornerRadius = new CornerRadius(4)
                    };
                    
                    Grid iconGrid = new Grid();
                    iconGrid.Children.Add(coloredIconContainer);
                    iconGrid.Children.Add(icon);
                    
                    button.Content = iconGrid;
                }
                else
                {
                    button.Content = icon;
                }
            }
            catch (Exception ex)
            { 
                button.Content = tooltip.Substring(0, Math.Min(tooltip.Length, 3)); 
                System.Diagnostics.Debug.WriteLine($"Error loading icon {iconPath}: {ex.Message}"); 
            }

            button.MouseEnter += (s, e) =>
            {
                button.Background = new SolidColorBrush(_buttonHoverHighlightColor);
            };
            button.MouseLeave += (s, e) =>
            {
                button.Background = Brushes.Transparent;
            };

            button.Click += (s, e) =>
            {
                var mainToggleButton = FindToggleButton();
                if (mainToggleButton != null) 
                {
                    string? currentState = mainToggleButton.Tag as string ?? "ButtonOnly";
                    if (currentState == "ButtonOnly")
                    {
                        ToggleToolbar(mainToggleButton, new RoutedEventArgs());
                        ToggleToolbar(mainToggleButton, new RoutedEventArgs());
                    }
                    else if (currentState == "IconsOnly")
                    {
                        ToggleToolbar(mainToggleButton, new RoutedEventArgs());
                    }
                }
                NavigateToUrl(url);
            };
            return button;
        }

        private Button CreateSettingsButton()
        {
            Button settingsButton = new Button
            {
                Width = 40, 
                Height = 40,
                Margin = new Thickness(5),
                Background = Brushes.Transparent,
                ToolTip = "Configurações",
                BorderThickness = new Thickness(0),
                Visibility = Visibility.Collapsed
            };
            
            try
            {
                Grid iconGrid = new Grid
                {
                    Width = 24,
                    Height = 24
                };
                
                System.Windows.Shapes.Path gearPath = new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse("M9.4 1c.4 0 .8.3.9.7l.3 1.5c.1.4.5.7 1 .7l.2-.1c.3-.1.5-.2.8-.4.2-.1.5-.2.7-.2.3 0 .5.1.7.2l1.3.9c.3.2.4.5.4.9 0 .2-.1.5-.2.7l-.8 1.3c-.2.3-.2.7 0 1l.1.2c.2.2.4.5.5.7l1.5.3c.4.1.7.5.7.9v1.8c0 .4-.3.8-.7.9l-1.5.3c-.4.1-.7.5-.7 1 0 .1 0 .2.1.2.1.3.2.5.4.8.1.2.2.5.2.7 0 .3-.1.6-.3.8l-.9 1.3c-.2.3-.5.4-.9.4-.2 0-.5-.1-.7-.2l-1.3-.8c-.3-.2-.7-.2-1 0l-.2.1c-.2.2-.5.4-.7.5l-.3 1.5c-.1.4-.5.7-.9.7H7.8c-.4 0-.8-.3-.9-.7l-.3-1.5c-.1-.4-.5-.7-1-.7l-.2.1c-.3.1-.5.2-.8.4-.2.1-.5.2-.7.2-.3 0-.5-.1-.7-.2l-1.3-.9c-.3-.2-.4-.5-.4-.9 0-.2.1-.5.2-.7l.8-1.3c.2-.3.2-.7 0-1l-.1-.2c-.2-.2-.4-.5-.5-.7L.7 13c-.4-.1-.7-.5-.7-.9v-1.8c0-.4.3-.8.7-.9l1.5-.3c.4-.1.7-.5.7-1 0-.1 0-.2-.1-.2-.1-.3-.2-.5-.4-.8-.1-.2-.2-.5-.2-.7 0-.3.1-.6.3-.8l.9-1.3c.2-.3.5-.4.9-.4.2 0 .5.1.7.2l1.3.8c.3.2.7.2 1 0l.2-.1c.2-.2.5-.4.7-.5l.3-1.5c.1-.4.5-.7.9-.7h1.6zm.8 5c-2.8 0-5 2.2-5 5s2.2 5 5 5 5-2.2 5-5-2.2-5-5-5zm0 2c1.7 0 3 1.3 3 3s-1.3 3-3 3-3-1.3-3-3 1.3-3 3-3z"),
                    Fill = new SolidColorBrush(Colors.Gray),
                    Stroke = new SolidColorBrush(Colors.Gray),
                    StrokeThickness = 0.2
                };
                
                iconGrid.Children.Add(gearPath);
                settingsButton.Content = iconGrid;
            }
            catch (Exception ex)
            {
                TextBlock settingsIcon = new TextBlock
                {
                    Text = "⚙",
                    FontFamily = new FontFamily("Segoe UI Symbol"),
                    FontSize = 22,
                    Foreground = new SolidColorBrush(Colors.Gray),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                };
                
                settingsButton.Content = settingsIcon;
                System.Diagnostics.Debug.WriteLine($"Erro ao criar ícone SVG: {ex.Message}");
            }
            
            settingsButton.MouseEnter += (s, e) =>
            {
                settingsButton.Background = new SolidColorBrush(_buttonHoverHighlightColor);
                
                if (settingsButton.Content is Grid iconGrid && iconGrid.Children.Count > 0 && 
                    iconGrid.Children[0] is System.Windows.Shapes.Path gearPath)
                {
                    gearPath.Fill = new SolidColorBrush(Colors.White);
                    gearPath.Stroke = new SolidColorBrush(Colors.White);
                }
                else if (settingsButton.Content is TextBlock textBlock)
                {
                    textBlock.Foreground = new SolidColorBrush(Colors.White);
                }
            };
            
            settingsButton.MouseLeave += (s, e) =>
            {
                settingsButton.Background = Brushes.Transparent;
                
                if (settingsButton.Content is Grid iconGrid && iconGrid.Children.Count > 0 && 
                    iconGrid.Children[0] is System.Windows.Shapes.Path gearPath)
                {
                    gearPath.Fill = new SolidColorBrush(Colors.Gray);
                    gearPath.Stroke = new SolidColorBrush(Colors.Gray);
                }
                else if (settingsButton.Content is TextBlock textBlock)
                {
                    textBlock.Foreground = new SolidColorBrush(Colors.Gray);
                }
            };
            
            settingsButton.Click += (s, e) =>
            {
                var configWindow = new ConfigurationWindow(this, GetCurrentPlatformsForConfig());
                configWindow.ShowDialog();
            };
            
            return settingsButton;
        }
        
        private Button? FindToggleButton()
        {
            return mainToolbarContainer?.Child is Grid toolbarGrid 
                ? toolbarGrid.Children.OfType<Button>().FirstOrDefault(b => b.Name == "MainToggleButton")
                : null;
        }

        private void NavigateToUrl(string url)
        {
            if (webView == null || webView.CoreWebView2 == null) { return; }
            try
            {
                currentUrl = url;
                
                webView.Source = new Uri(url);
                
                if (userSettings.WindowPositions.ContainsKey(url))
                {
                    var savedPosition = userSettings.WindowPositions[url];
                    
                    double screenWidth = SystemParameters.WorkArea.Width;
                    double screenHeight = SystemParameters.WorkArea.Height;
                    
                    double left = Math.Max(0, Math.Min(savedPosition.Left, screenWidth - 100));
                    double top = Math.Max(0, Math.Min(savedPosition.Top, screenHeight - 100));
                    
                    double width = Math.Max(ExpandedWindowWidth, savedPosition.Width);
                    
                    if (url.Contains("whatsapp") && width < EnhancedExpandedWindowWidth)
                    {
                        width = EnhancedExpandedWindowWidth;
                    }
                    
                    double height = Math.Max(200, savedPosition.Height);
                    
                    this.Left = left;
                    this.Top = top;
                    this.Width = width;
                    this.Height = height;
                }
                else if (isFirstIconClick)
                {
                    this.Width = EnhancedExpandedWindowWidth;
                    isFirstIconClick = false;
                    
                    userSettings.FirstClickDone = true;
                }
                else
                {
                    if (url.Contains("whatsapp"))
                    {
                        this.Width = EnhancedExpandedWindowWidth;
                    }
                    else 
                    {
                        this.Width = Math.Max(this.Width, ExpandedWindowWidth);
                    }
                }
                
                SaveSettings();
            }
            catch (Exception ex) 
            { 
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}"); 
            }
        }

        private Style CreateRoundButtonStyle()
        {
            Style roundButtonStyle = new Style(typeof(Button));
            
            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(18));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
            
            DropShadowEffect shadowEffect = new DropShadowEffect
            {
                Color = Color.FromArgb(255, 255, 255, 255),
                Direction = 0,
                ShadowDepth = 0,
                BlurRadius = 25,
                Opacity = 1.0
            };
            border.SetValue(Border.EffectProperty, shadowEffect);
            
            FrameworkElementFactory contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(contentPresenter);
            
            template.VisualTree = border;
            roundButtonStyle.Setters.Add(new Setter(Button.TemplateProperty, template));
            
            return roundButtonStyle;
        }
        
        private UIElement CreateCircularArrowIcon()
        {
            Grid iconGrid = new Grid
            {
                Width = 24,
                Height = 24
            };
            
            Viewbox viewbox = new Viewbox
            {
                Width = 24,
                Height = 24,
                Stretch = Stretch.Uniform
            };
            
            System.Windows.Shapes.Path arrowPath = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M12,2 A10,10 0 1 0 12,22 A10,10 0 1 0 12,2 M12,4 A8,8 0 1 1 4,12 A8,8 0 1 1 12,4 M10,17 L15,12 L10,7 L10,10 L6,10 L6,14 L10,14 L10,17 Z"),
                Fill = new SolidColorBrush(_toggleButtonForeground),
                Stroke = new SolidColorBrush(_toggleButtonForeground),
                StrokeThickness = 0.5
            };
            
            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(new RotateTransform());
            transformGroup.Children.Add(new ScaleTransform(1.0, 1.0));
            arrowPath.RenderTransform = transformGroup;
            
            viewbox.Child = arrowPath;
            iconGrid.Children.Add(viewbox);
            
            return iconGrid;
        }

        private UIElement CreateArrowIcon(string direction)
        {
            Grid iconGrid = new Grid
            {
                Width = 24,
                Height = 24
            };
            
            Ellipse circle = new Ellipse
            {
                Width = 24,
                Height = 24,
                Stroke = new SolidColorBrush(_toggleButtonForeground),
                StrokeThickness = 1.5,
                Fill = new SolidColorBrush(Colors.Transparent)
            };
            
            System.Windows.Shapes.Path arrowPath = new System.Windows.Shapes.Path
            {
                Stroke = new SolidColorBrush(_toggleButtonForeground),
                StrokeThickness = 2,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            if (direction == ">")
            {
                arrowPath.Data = Geometry.Parse("M8,6 L14,12 L8,18");
            }
            else
            {
                arrowPath.Data = Geometry.Parse("M16,6 L10,12 L16,18");
            }
            
            iconGrid.Children.Add(circle);
            iconGrid.Children.Add(arrowPath);
            
            return iconGrid;
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(this);
            var toggleBtn = FindToggleButton();
            string currentState = toggleBtn?.Tag as string ?? "ButtonOnly";
            bool isButtonOnly = currentState == "ButtonOnly";
            bool isIconsOnly = currentState == "IconsOnly";
            bool isWebViewFullyExpanded = currentState == "WebViewExpanded";

            bool canResizeHorizontally = (isWebViewFullyExpanded && mousePos.X >= this.Width - ResizeBorderThickness) ||
                                         ((isButtonOnly || isIconsOnly) && (mousePos.X <= ResizeBorderThickness || mousePos.X >= this.Width - ResizeBorderThickness));
            
            bool canResizeVertically = mousePos.Y <= ResizeBorderThickness || mousePos.Y >= this.Height - ResizeBorderThickness;

            if (canResizeHorizontally && mousePos.X <= ResizeBorderThickness && (isButtonOnly || isIconsOnly)) 
            {
                Cursor = Cursors.SizeWE;
                if (e.LeftButton == MouseButtonState.Pressed) ResizeWindow(ResizeDirection.Left);
            }
            else if (canResizeHorizontally && mousePos.X >= this.Width - ResizeBorderThickness)
            {
                Cursor = Cursors.SizeWE;
                if (e.LeftButton == MouseButtonState.Pressed) ResizeWindow(ResizeDirection.Right);
            }
            else if (canResizeVertically && mousePos.Y <= ResizeBorderThickness)
            {
                Cursor = Cursors.SizeNS;
                if (e.LeftButton == MouseButtonState.Pressed) ResizeWindow(ResizeDirection.Top);
            }
            else if (canResizeVertically && mousePos.Y >= this.Height - ResizeBorderThickness)
            {
                Cursor = Cursors.SizeNS;
                if (e.LeftButton == MouseButtonState.Pressed) ResizeWindow(ResizeDirection.Bottom);
            }
            else
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = e.GetPosition(this);
            var toggleBtn = FindToggleButton();
            string currentState = toggleBtn?.Tag as string ?? "ButtonOnly";
            bool isButtonOnly = currentState == "ButtonOnly";
            bool isIconsOnly = currentState == "IconsOnly";
            bool isWebViewFullyExpanded = currentState == "WebViewExpanded";

            bool canResizeHorizontally = (isWebViewFullyExpanded && mousePos.X >= this.Width - ResizeBorderThickness) ||
                                         ((isButtonOnly || isIconsOnly) && (mousePos.X <= ResizeBorderThickness || mousePos.X >= this.Width - ResizeBorderThickness));
            bool canResizeVertically = mousePos.Y <= ResizeBorderThickness || mousePos.Y >= this.Height - ResizeBorderThickness;

            if (e.LeftButton == MouseButtonState.Pressed) 
            {
                if (canResizeHorizontally && mousePos.X <= ResizeBorderThickness && (isButtonOnly || isIconsOnly))
                {
                    ResizeWindow(ResizeDirection.Left);
                }
                else if (canResizeHorizontally && mousePos.X >= this.Width - ResizeBorderThickness)
                {
                    ResizeWindow(ResizeDirection.Right);
                }
                else if (canResizeVertically && mousePos.Y <= ResizeBorderThickness)
                {
                    ResizeWindow(ResizeDirection.Top);
                }
                else if (canResizeVertically && mousePos.Y >= this.Height - ResizeBorderThickness)
                {
                    ResizeWindow(ResizeDirection.Bottom);
                }
                else if (isButtonOnly)
                {
                    this.DragMove();
                }
                else if (e.OriginalSource is Border border && border == mainToolbarContainer)
                {
                    this.DragMove();
                    
                    if (!string.IsNullOrEmpty(currentUrl))
                    {
                        SaveWebViewPosition();
                    }
                }
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private enum ResizeDirection { Left = 1, Right = 2, Top = 3, Bottom = 6, TopLeft = 4, TopRight = 5, BottomLeft = 7, BottomRight = 8 }

        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] public static extern bool ReleaseCapture();

        private void ResizeWindow(ResizeDirection direction)
        {
            if (direction == 0) return;
            ReleaseCapture();
            SendMessage(new System.Windows.Interop.WindowInteropHelper(this).Handle, 0x112, (IntPtr)(0xF000 + (int)direction), IntPtr.Zero);
        }

        public void AddNewSite(string name, string url, string iconPath)
        {
            var newPlatform = new SocialMediaPlatform(name, url, iconPath, null);
            
            socialMediaPlatforms.RemoveAll(p => p.Name == name);
            additionalPlatforms.RemoveAll(p => p.Name == name);
            
            socialMediaPlatforms.Add(newPlatform);
            
            RefreshSocialMediaButtons(); 
            SaveSettings(); 
        }

        public void EditSite(SocialMediaPlatform originalPlatform, string newName, string newUrl, string newIconPath)
        {
            bool isDefault = IsPlatformDefault(originalPlatform);
            bool changed = originalPlatform.Name != newName || originalPlatform.Url != newUrl || originalPlatform.IconPath != newIconPath;
            Color? newAccentColor = originalPlatform.AccentColor;

            if (isDefault && changed)
            {
                newAccentColor = null;
            }
            else if (!isDefault)
            {
                newAccentColor = null;
            }

            var editedPlatform = new SocialMediaPlatform(newName, newUrl, newIconPath, newAccentColor);

            socialMediaPlatforms.RemoveAll(p => p.Name == originalPlatform.Name && p.Url == originalPlatform.Url);
            additionalPlatforms.RemoveAll(p => p.Name == originalPlatform.Name && p.Url == originalPlatform.Url);

            if (newAccentColor != null && _defaultAdditionalPlatforms.Any(dp => dp.Name == editedPlatform.Name))
            {
                additionalPlatforms.Add(editedPlatform);
            }
            else
            {
                socialMediaPlatforms.Add(editedPlatform);
            }
            
            var itemInMain = socialMediaPlatforms.FirstOrDefault(sp => sp.Name == editedPlatform.Name);
            if (itemInMain != null && itemInMain != editedPlatform) {
                 socialMediaPlatforms.Remove(itemInMain);
            }

            RefreshSocialMediaButtons();
            SaveSettings();
        }

        public void RemoveSite(SocialMediaPlatform platformToRemove)
        {
            socialMediaPlatforms.RemoveAll(p => p.Name == platformToRemove.Name && p.Url == platformToRemove.Url);
            additionalPlatforms.RemoveAll(p => p.Name == platformToRemove.Name && p.Url == platformToRemove.Url);
            RefreshSocialMediaButtons();
            SaveSettings();
        }
        
        public List<SocialMediaPlatform> GetCurrentPlatformsForConfig()
        {
            var combined = socialMediaPlatforms.ToList();
            foreach (var addP in additionalPlatforms)
            {
                if (!combined.Any(sp => sp.Name == addP.Name))
                {
                    combined.Add(addP);
                }
            }
            return combined.OrderBy(p => p.Name).ToList();
        }

        public bool IsPlatformDefault(SocialMediaPlatform platform)
        {
            return _defaultSocialMediaPlatforms.Any(p => p.Name == platform.Name && p.Url == platform.Url && p.IconPath == platform.IconPath && p.AccentColor == platform.AccentColor) ||
                   _defaultAdditionalPlatforms.Any(p => p.Name == platform.Name && p.Url == platform.Url && p.IconPath == platform.IconPath && p.AccentColor == platform.AccentColor);
        }

        private void RefreshSocialMediaButtonsInternal()
        {
            if (mainContentPanel == null) return;

            mainContentPanel.Children.Clear();
            socialMediaButtons.Clear();

            if (appLogoImage != null) mainContentPanel.Children.Add(appLogoImage);
            if (logoSeparator != null) mainContentPanel.Children.Add(logoSeparator);

            foreach (var platform in socialMediaPlatforms.OrderBy(p => p.Name))
            {
                var socialButton = CreateToolbarButton(platform.Name, platform.Url, platform.IconPath, platform.AccentColor);
                socialButton.Visibility = Visibility.Visible;
                socialMediaButtons.Add(socialButton);
                mainContentPanel.Children.Add(socialButton);
            }

            if (socialMediaGroupSeparator != null) mainContentPanel.Children.Add(socialMediaGroupSeparator);
            
            foreach (var platform in additionalPlatforms.OrderBy(p => p.Name))
            {
                if (!socialMediaButtons.Any(b => b.ToolTip as string == platform.Name))
                {
                    var additionalButton = CreateToolbarButton(platform.Name, platform.Url, platform.IconPath, platform.AccentColor);
                    additionalButton.Visibility = Visibility.Visible;
                    socialMediaButtons.Add(additionalButton);
                    mainContentPanel.Children.Add(additionalButton);
                }
            }
        }
        public void RefreshSocialMediaButtons()
        {
            RefreshSocialMediaButtonsInternal();
        }

        private void SetupSystemTrayIcon()
        {
            try
            {
                notifyIcon = new NotifyIcon
                {
                    Visible = true,
                    Text = "Social Toolbar"
                };

                try
                {
                    if (File.Exists("icons/logo.png"))
                    {
                        using (var bitmap = new System.Drawing.Bitmap("icons/logo.png"))
                        {
                            System.Drawing.Icon ico = System.Drawing.Icon.FromHandle(bitmap.GetHicon());
                            notifyIcon.Icon = ico;
                        }
                    }
                    else if (File.Exists("icons/twitter.png"))
                    {
                        using (var bitmap = new System.Drawing.Bitmap("icons/twitter.png"))
                        {
                            System.Drawing.Icon ico = System.Drawing.Icon.FromHandle(bitmap.GetHicon());
                            notifyIcon.Icon = ico;
                        }
                    }
                    else
                    {
                        notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                            System.Reflection.Assembly.GetEntryAssembly()?.Location ?? "");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao carregar ícone: {ex.Message}");
                    try
                    {
                        notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                            System.Reflection.Assembly.GetEntryAssembly()?.Location ?? "");
                    }
                    catch
                    {
                    }
                }

                var contextMenu = new System.Windows.Forms.ContextMenuStrip();
                
                var mostrarItem = new System.Windows.Forms.ToolStripMenuItem("Mostrar/Ocultar");
                mostrarItem.Click += (s, e) => 
                {
                    this.Visibility = this.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
                };
                contextMenu.Items.Add(mostrarItem);
                
                var configItem = new System.Windows.Forms.ToolStripMenuItem("Configurações");
                configItem.Click += (s, e) => 
                {
                    var configWindow = new ConfigurationWindow(this, GetCurrentPlatformsForConfig());
                    configWindow.ShowDialog();
                };
                contextMenu.Items.Add(configItem);
                
                contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
                
                var sairItem = new System.Windows.Forms.ToolStripMenuItem("Sair");
                sairItem.Click += (s, e) => 
                {
                    SaveSettings();
                    System.Windows.Application.Current.Shutdown();
                };
                contextMenu.Items.Add(sairItem);

                notifyIcon.ContextMenuStrip = contextMenu;
                
                notifyIcon.MouseDoubleClick += (s, e) => 
                {
                    this.Visibility = this.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
                    
                    if (this.Visibility == Visibility.Visible)
                    {
                        this.Activate();
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao configurar ícone do sistema: {ex.Message}");
                MessageBox.Show($"Não foi possível criar o ícone na área de notificação: {ex.Message}", 
                                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class ConfigurationWindow : Window
    {
        private readonly MainWindow _mainWindow;
        private List<SocialMediaPlatform> _platforms;
        
        private TextBox nameTextBox = null!;
        private TextBox urlTextBox = null!;
        private TextBox iconTextBox = null!;
        private Button actionButton = null!;
        private StackPanel sitesListPanel = null!;
        private SocialMediaPlatform? _editingPlatform = null;

        public ConfigurationWindow(MainWindow mainWindow, List<SocialMediaPlatform> platforms)
        {
            _mainWindow = mainWindow;
            _platforms = platforms;
            
            this.Title = "Configurações";
            this.Width = 550;
            this.Height = 800;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Background = new SolidColorBrush(Color.FromRgb(30, 32, 35));
            this.Foreground = Brushes.White;
            this.ResizeMode = ResizeMode.NoResize;
            
            CreateUI();
        }
        
        private void CreateUI()
        {
            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(350, GridUnitType.Pixel) });
            this.Content = mainGrid;
            
            sitesListPanel = new StackPanel
            {
                Margin = new Thickness(15)
            };
            
            ScrollViewer scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = sitesListPanel
            };
            
            TextBlock titleBlock = new TextBlock
            {
                Text = "Sites cadastrados",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = Brushes.White
            };
            sitesListPanel.Children.Add(titleBlock);
            
            RefreshSitesListVisual();
            
            Grid.SetRow(scrollViewer, 0);
            mainGrid.Children.Add(scrollViewer);
            
            Border addSiteBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 62, 65)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(25, 27, 30)),
                Padding = new Thickness(20),
                MinHeight = 300
            };
            
            Grid addSiteGrid = new Grid();
            addSiteGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            addSiteGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            addSiteGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            addSiteGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            TextBlock addSiteTitle = new TextBlock
            {
                Text = "Adicionar novo site",
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = Brushes.White
            };
            Grid.SetRow(addSiteTitle, 0);
            addSiteGrid.Children.Add(addSiteTitle);
            
            Grid formGrid = new Grid();
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120, GridUnitType.Pixel) });
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            TextBlock nameLabel = new TextBlock
            {
                Text = "Nome:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 10, 10, 10),
                Foreground = Brushes.White,
                FontSize = 14
            };
            Grid.SetRow(nameLabel, 0);
            Grid.SetColumn(nameLabel, 0);
            formGrid.Children.Add(nameLabel);
            
            nameTextBox = new TextBox
            {
                Margin = new Thickness(0, 10, 0, 10),
                Padding = new Thickness(8),
                Background = new SolidColorBrush(Color.FromRgb(40, 42, 45)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 62, 65)),
                FontSize = 14,
                Height = 36
            };
            Grid.SetRow(nameTextBox, 0);
            Grid.SetColumn(nameTextBox, 1);
            formGrid.Children.Add(nameTextBox);
            
            TextBlock urlLabel = new TextBlock
            {
                Text = "URL:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 10, 10, 10),
                Foreground = Brushes.White,
                FontSize = 14
            };
            Grid.SetRow(urlLabel, 1);
            Grid.SetColumn(urlLabel, 0);
            formGrid.Children.Add(urlLabel);
            
            urlTextBox = new TextBox
            {
                Margin = new Thickness(0, 10, 0, 10),
                Padding = new Thickness(8),
                Background = new SolidColorBrush(Color.FromRgb(40, 42, 45)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 62, 65)),
                FontSize = 14,
                Height = 36
            };
            Grid.SetRow(urlTextBox, 1);
            Grid.SetColumn(urlTextBox, 1);
            formGrid.Children.Add(urlTextBox);
            
            TextBlock iconLabel = new TextBlock
            {
                Text = "Ícone:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 10, 10, 10),
                Foreground = Brushes.White,
                FontSize = 14
            };
            Grid.SetRow(iconLabel, 2);
            Grid.SetColumn(iconLabel, 0);
            formGrid.Children.Add(iconLabel);
            
            iconTextBox = new TextBox
            {
                Margin = new Thickness(0, 10, 0, 10),
                Padding = new Thickness(8),
                Background = new SolidColorBrush(Color.FromRgb(40, 42, 45)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 62, 65)),
                FontSize = 14,
                Height = 36
            };
            Grid.SetRow(iconTextBox, 2);
            Grid.SetColumn(iconTextBox, 1);
            formGrid.Children.Add(iconTextBox);
            
            Grid.SetRow(formGrid, 1);
            addSiteGrid.Children.Add(formGrid);
            
            actionButton = new Button
            {
                Content = "Adicionar Site",
                Margin = new Thickness(0, 40, 0, 0),
                Width = 150,
                Height = 36,
                Background = new SolidColorBrush(Color.FromRgb(0, 174, 255)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };
            
            ToolTip tooltip = new ToolTip();
            tooltip.Content = "Clique para adicionar um novo site à barra lateral";
            actionButton.ToolTip = tooltip;
            
            actionButton.Click += ActionButton_Click;
            
            Grid.SetRow(actionButton, 2);
            addSiteGrid.Children.Add(actionButton);
            
            addSiteBorder.Child = addSiteGrid;
            Grid.SetRow(addSiteBorder, 1);
            mainGrid.Children.Add(addSiteBorder);

            TextBlock signatureTextBlock = new TextBlock
            {
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 15, 0, 10)
            };

            signatureTextBlock.Inlines.Add("Produzido por Hendryl. Github (");
            Hyperlink githubLink = new Hyperlink
            {
                NavigateUri = new Uri("https://github.com/HendrylHH")
            };
            githubLink.Inlines.Add("https://github.com/HendrylHH");
            githubLink.RequestNavigate += (sender, args) =>
            {
                Process.Start(new ProcessStartInfo(args.Uri.AbsoluteUri) { UseShellExecute = true });
                args.Handled = true;
            };
            signatureTextBlock.Inlines.Add(githubLink);
            signatureTextBlock.Inlines.Add(")");
            
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(signatureTextBlock, 2);
            mainGrid.Children.Add(signatureTextBlock);
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            string name = nameTextBox.Text.Trim();
            string url = urlTextBox.Text.Trim();
            string iconPath = iconTextBox.Text.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url))
            {
                MessageBox.Show("O nome e URL são obrigatórios.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(iconPath))
            {
                iconPath = "icons/twitter.png";
            }

            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "https://" + url;
            }

            if (_editingPlatform != null)
            {
                _mainWindow.EditSite(_editingPlatform, name, url, iconPath);
                MessageBox.Show($"Site '{name}' atualizado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                if (_platforms.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show($"Já existe um site com o nome '{name}'. Escolha um nome diferente.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                _mainWindow.AddNewSite(name, url, iconPath);
                MessageBox.Show($"Site '{name}' adicionado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            
            ResetFormAndExitEditMode();
            RefreshSitesListWithDataFromMainWindow();
        }

        private void SetEditMode(SocialMediaPlatform platform)
        {
            _editingPlatform = platform;
            nameTextBox.Text = platform.Name;
            urlTextBox.Text = platform.Url;
            iconTextBox.Text = platform.IconPath;
            actionButton.Content = "Salvar Alterações";
            if (actionButton.ToolTip is ToolTip tt)
            {
                tt.Content = "Clique para salvar as alterações neste site";
            }
        }

        private void ResetFormAndExitEditMode()
        {
            _editingPlatform = null;
            nameTextBox.Text = "";
            urlTextBox.Text = "";
            iconTextBox.Text = "";
            actionButton.Content = "Adicionar Site";
            if (actionButton.ToolTip is ToolTip tt)
            {
                tt.Content = "Clique para adicionar um novo site à barra lateral";
            }
        }
        
        private void RefreshSitesListVisual()
        {
            sitesListPanel.Children.Clear();
            TextBlock titleBlock = new TextBlock
            {
                Text = "Sites cadastrados",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = Brushes.White
            };
            sitesListPanel.Children.Add(titleBlock);

            foreach (var platform in _platforms.OrderBy(p => p.Name))
            {
                Border siteItem = CreateSiteItemVisual(platform);
                sitesListPanel.Children.Add(siteItem);
            }
        }
        
        private void RefreshSitesListWithDataFromMainWindow()
        {
            _platforms = _mainWindow.GetCurrentPlatformsForConfig();
            RefreshSitesListVisual();
        }

        private Border CreateSiteItemVisual(SocialMediaPlatform platform)
        {
            Border border = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 62, 65)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(10, 5, 10, 5),
                Background = new SolidColorBrush(Color.FromRgb(40, 42, 45)),
                Margin = new Thickness(0, 0, 0, 5)
            };
            
            Grid itemGrid = new Grid();
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40, GridUnitType.Pixel) });
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            Image siteIcon = new Image
            {
                Width = 24,
                Height = 24,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };
            
            try
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(platform.IconPath, UriKind.RelativeOrAbsolute);
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmapImage.EndInit();
                siteIcon.Source = bitmapImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar ícone: {ex.Message}");
            }
            
            Grid.SetColumn(siteIcon, 0);
            itemGrid.Children.Add(siteIcon);
            
            StackPanel infoPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            TextBlock siteNameBlock = new TextBlock
            {
                Text = platform.Name,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                FontSize = 14
            };
            
            TextBlock siteUrlBlock = new TextBlock
            {
                Text = platform.Url,
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                FontSize = 12
            };
            
            infoPanel.Children.Add(siteNameBlock);
            infoPanel.Children.Add(siteUrlBlock);
            
            Grid.SetColumn(infoPanel, 1);
            itemGrid.Children.Add(infoPanel);
            
            StackPanel actionButtonsPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right 
            };
            Grid.SetColumn(actionButtonsPanel, 2);
            Grid.SetColumnSpan(actionButtonsPanel, 2);
            itemGrid.Children.Add(actionButtonsPanel);

            Button editButton = new Button
            {
                Content = "✏️",
                Width = 30, Height = 30,
                Margin = new Thickness(5,0,5,0),
                Background = new SolidColorBrush(Color.FromRgb(50, 50, 180)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                ToolTip = "Editar este site",
                FontFamily = new FontFamily("Segoe UI Symbol")
            };
            editButton.Click += (s,e) => SetEditMode(platform);
            editButton.MouseEnter += (s, e_hover) => ((Button)s).Background = new SolidColorBrush(Color.FromRgb(70, 70, 200));
            editButton.MouseLeave += (s, e_hover) => ((Button)s).Background = new SolidColorBrush(Color.FromRgb(50, 50, 180));
            actionButtonsPanel.Children.Add(editButton);

            Button deleteButton = new Button
            {
                Content = "🗑️",
                Width = 30, Height = 30,
                Margin = new Thickness(5,0,5,0),
                Background = new SolidColorBrush(Color.FromRgb(60, 0, 0)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                ToolTip = "Remover este site",
                FontFamily = new FontFamily("Segoe UI Symbol")
            };
            
            deleteButton.Click += (s, e) =>
            {
                string siteName = platform.Name;
                
                MessageBoxResult result = MessageBox.Show(
                    $"Tem certeza de que deseja remover o site '{siteName}'?",
                    "Confirmar remoção",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );
                
                if (result == MessageBoxResult.Yes)
                {
                    _mainWindow.RemoveSite(platform);
                    RefreshSitesListWithDataFromMainWindow();
                    ResetFormAndExitEditMode();
                    
                    MessageBox.Show(
                        "O site foi removido e a barra de ferramentas foi atualizada.",
                        "Site removido",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            };
            
            deleteButton.MouseEnter += (s, e) =>
            {
                ((Button)s).Background = new SolidColorBrush(Color.FromRgb(80, 0, 0));
            };
            
            deleteButton.MouseLeave += (s, e) =>
            {
                ((Button)s).Background = new SolidColorBrush(Color.FromRgb(60, 0, 0));
            };
            
            actionButtonsPanel.Children.Add(deleteButton);
            
            border.Child = itemGrid;
            
            return border;
        }
    }
}