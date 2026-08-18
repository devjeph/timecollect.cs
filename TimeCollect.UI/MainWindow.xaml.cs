using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using DotNetEnv;
using Google.Apis.Sheets.v4;
using TimeCollect.Core.Models;
using TimeCollect.Core.Helpers;
using TimeCollect.Core.Services;

namespace TimeCollect.UI
{
    /// <summary>
    /// Code-behind logic for the MainWindow UI. Handles startup configuration mapping,
    /// theme switching, UI validation constraints, and asynchronous background data pipeline execution.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadEnvironmentVariables();
            InitializeLogConsole();
            Log("System initialized. Ready for execution.", LogLevel.Info);
        }

        private void InitializeLogConsole()
        {
            txtLog.Document = new FlowDocument();
        }
        /// <summary>
        /// Restores native window dragging geometry by capturing the left-click 
        /// event anywhere on the custom top grid.
        /// </summary>
        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Parses the physical .env file in the execution directory and maps key-value pairs 
        /// to the corresponding UI TextBoxes, DatePicker, and Theme selector.
        /// </summary>
        private void LoadEnvironmentVariables()
        {
            Env.Load(".env");
            txtOutputDirectory.Text = Env.GetString("OUTPUT_DIRECTORY_2026", @"D:\Documents\TimeCollect\2026");
            txtProjectSpreadsheet.Text = Env.GetString("PROJECT_SPREADSHEET", "1yKLHsWWOffCVWTI4d6A4n8exNF-ioP2CHn7E7RomT4k");
            txtProjectRange.Text = Env.GetString("PROJECT_RANGE", "project_info!A:C");
            txtEmployeesSpreadsheet.Text = Env.GetString("EMPLOYEES_SPREADSHEET", "1yKLHsWWOffCVWTI4d6A4n8exNF-ioP2CHn7E7RomT4k");
            txtSheetNames.Text = Env.GetString("SHEET_NAMES", "202608,202609");

            // Extract the single date string and attempt to parse it into the DatePicker
            string envStartDate = Env.GetString("TIMESHEET_START_DATE", "2025-12-28");
            if (DateTime.TryParse(envStartDate, out DateTime parsedDate))
            {
                dpStartDate.SelectedDate = parsedDate;
            }
            else
            {
                dpStartDate.SelectedDate = DateTime.Today;
            }

            // --- Replace your existing cbTheme default block with this ---
            if (cbTheme != null && cbTheme.Items.Count > 0)
            {
                // Pull the saved theme from .env, defaulting to Light Theme if not found
                string savedTheme = Env.GetString("APP_THEME", "Light Theme");

                if (savedTheme.Contains("Dark"))
                {
                    cbTheme.SelectedIndex = 1; // Dark Theme
                }
                else
                {
                    cbTheme.SelectedIndex = 0; // Light Theme
                }
            }
        }

        /// <summary>
        /// Dynamically updates the application resource brushes for Light and Dark themes.
        /// </summary>
        private void CbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbTheme == null || cbTheme.SelectedItem == null) return;

            string selectedTheme = ((ComboBoxItem)cbTheme.SelectedItem).Content.ToString() ?? string.Empty;

            //Save the user's choice back to the .env file so it persists on restart
            if (this.IsLoaded)
            {
                SaveThemeToEnv(selectedTheme);
            }

            if (selectedTheme.Contains("Dark"))
            {
                Application.Current.Resources["WindowBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111827"));
                Application.Current.Resources["CardBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F2937"));
                Application.Current.Resources["TextPrimaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F9FAFB"));
                Application.Current.Resources["TextSecondaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF"));
                Application.Current.Resources["BorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#374151"));

                // Add Dark Mode Disabled Brushes
                Application.Current.Resources["DisabledBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#374151"));
                Application.Current.Resources["DisabledForegroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));

                // NEW: Dark Mode Terminal (Deep, pitch-black slate)
                Application.Current.Resources["ConsoleBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#030712"));
                Application.Current.Resources["ConsoleForegroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
            }
            else
            {
                Application.Current.Resources["WindowBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F4F6"));
                Application.Current.Resources["CardBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                Application.Current.Resources["TextPrimaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111827"));
                Application.Current.Resources["TextSecondaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
                Application.Current.Resources["BorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB"));

                // Add Light Mode Disabled Brushes
                Application.Current.Resources["DisabledBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB"));
                Application.Current.Resources["DisabledForegroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF"));

                // NEW: Light Mode Terminal (Softer, elevated slate-blue)
                Application.Current.Resources["ConsoleBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
                Application.Current.Resources["ConsoleForegroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
            }
        }

        /// <summary>
        /// Persists the selected theme state to the physical .env file.
        /// </summary>
        private void SaveThemeToEnv(string themeName)
        {
            try
            {
                string envFilePath = ".env";
                if (System.IO.File.Exists(envFilePath))
                {
                    var lines = new List<string>(System.IO.File.ReadAllLines(envFilePath));
                    int index = lines.FindIndex(l => l.StartsWith("APP_THEME="));

                    if (index >= 0)
                    {
                        lines[index] = $"APP_THEME={themeName}";
                    }
                    else
                    {
                        lines.Add($"APP_THEME={themeName}");
                    }
                    System.IO.File.WriteAllLines(envFilePath, lines);
                }
                else
                {
                    System.IO.File.WriteAllText(envFilePath, $"APP_THEME={themeName}\n");
                }
            }
            catch (Exception ex)
            {
                // Silently catch file lock exceptions during rapid UI toggling
                Console.WriteLine($"Could not save theme to .env: {ex.Message}");
            }
        }


        /// <summary>
        /// Validates that the selected date is a Sunday. If not, auto-corrects to the preceding Sunday.
        /// </summary>
        private void DpStartDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpStartDate.SelectedDate.HasValue)
            {
                DateTime selectedDate = dpStartDate.SelectedDate.Value;

                if (selectedDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    MessageBox.Show(
                        "The Timesheet start date must be a Sunday. The system will automatically adjust your selection to the preceding Sunday.",
                        "Parameter Constraint",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    int daysToSubtract = (int)selectedDate.DayOfWeek;
                    dpStartDate.SelectedDate = selectedDate.AddDays(-daysToSubtract);
                }
            }
        }

        public enum LogLevel { Info, Success, Warning, Error }

        /// <summary>
        /// Thread-safe contrasting logger that writes colored runs to the RichTextBox console.
        /// </summary>
        private void Log(string message, LogLevel level = LogLevel.Info)
        {
            Dispatcher.Invoke(() =>
            {
                Paragraph para = new Paragraph();
                para.Margin = new Thickness(0, 2, 0, 2);

                Run timeRun = new Run($"[{DateTime.Now:HH:mm:ss}] ")
                {
                    Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184))
                };
                para.Inlines.Add(timeRun);

                Brush textBrush = level switch
                {
                    LogLevel.Success => new SolidColorBrush(Color.FromRgb(74, 222, 128)),  // Emerald Green
                    LogLevel.Warning => new SolidColorBrush(Color.FromRgb(250, 204, 21)),  // Amber Yellow
                    LogLevel.Error => new SolidColorBrush(Color.FromRgb(248, 113, 113)),    // Crimson Red
                    _ => new SolidColorBrush(Color.FromRgb(226, 232, 240))                // Light Info Gray
                };

                Run msgRun = new Run(message) { Foreground = textBrush };
                para.Inlines.Add(msgRun);

                txtLog.Document.Blocks.Add(para);
                txtLog.ScrollToEnd();
            });
        }

        /// <summary>
        /// Captures current parametric state and executes the data pipeline on a background thread.
        /// </summary>
        private async void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime startDate = dpStartDate.SelectedDate ?? DateTime.Today;
                if (startDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    Log("Execution blocked: Start date is not a Sunday.", LogLevel.Error);
                    return;
                }

                btnRun.IsEnabled = false;
                tabSettings.IsEnabled = false;
                txtLog.Document.Blocks.Clear();
                Log("Execution started. Locking UI state.", LogLevel.Info);

                string outputDir = txtOutputDirectory.Text;
                string projectSheet = txtProjectSpreadsheet.Text;
                string projectRange = txtProjectRange.Text;
                string employeesSheet = txtEmployeesSpreadsheet.Text;
                string rawSheetNames = txtSheetNames.Text;

                int startYear = startDate.Year;
                int startMonth = startDate.Month;
                int startDay = startDate.Day;

                await Task.Run(() =>
                {
                    Log("Initiating Google OAuth2 Authentication...", LogLevel.Info);
                    SheetsService creds = GoogleAuthService.Authenticate();
                    if (creds != null) Log("🌐 Connected to Google API successfully.", LogLevel.Success);

                    var datasets = WeekTypeHelper.SetTypes(startYear, startMonth, startDay);

                    List<List<string>> rawProjectData = GoogleSheetsService.GetData(creds!, projectSheet, projectRange);
                    List<Project> projectData = DataParser.ParseProjects(rawProjectData);
                    Log("📝 Timesheet collection started...", LogLevel.Info);

                    string[] sheetNames = rawSheetNames.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    DateTime today = DateTime.Today;
                    int daysSinceSaturday = ((int)today.DayOfWeek + 1) % 7;
                    DateTime lastSaturday = today.AddDays(-daysSinceSaturday);

                    foreach (string rawSheetName in sheetNames)
                    {
                        string sheetName = rawSheetName.Trim();
                        if (string.IsNullOrEmpty(sheetName)) continue;

                        Log($"Collecting timesheet [{sheetName}] data...", LogLevel.Info);

                        List<List<string>> rawEmployeeData = GoogleSheetsService.GetData(creds!, employeesSheet, $"{sheetName}!A:E");

                        if (rawEmployeeData == null || rawEmployeeData.Count == 0)
                        {
                            Log("❌ ERROR: No employee data collected.", LogLevel.Error);
                            continue;
                        }

                        List<Employee> employees = DataParser.ParseEmployees(rawEmployeeData, skipHeader: false);
                        List<List<string>> excelSheet = new List<List<string>>();

                        foreach (Employee employee in employees)
                        {
                            try
                            {
                                List<List<string>> data = GoogleSheetsService.GetData(creds!, employee.SpreadsheetId, $"{sheetName}!A7:BU39");

                                if (data == null || data.Count == 0)
                                {
                                    throw new Exception("No data returned from API.");
                                }

                                for (int r = 0; r < data.Count; r++)
                                {
                                    var row = data[r];
                                    if (row.Count > 12)
                                    {
                                        if (int.TryParse(row[0], out int yr) && int.TryParse(row[1], out int mo) && int.TryParse(row[2], out int dy))
                                        {
                                            try
                                            {
                                                DateTime entryDate = new DateTime(yr, mo, dy);
                                                if (entryDate > lastSaturday) continue;

                                                if (entryDate.DayOfWeek >= DayOfWeek.Monday && entryDate.DayOfWeek <= DayOfWeek.Friday)
                                                {
                                                    if (double.TryParse(row[12], out double hoursColumnM))
                                                    {
                                                        if (hoursColumnM < 8.0)
                                                        {
                                                            Log($"⚠️ [Warning] {employee.Nickname} on {entryDate:yyyy-MM-dd}: Column M has {hoursColumnM}h (< 8h).", LogLevel.Warning);
                                                        }
                                                    }
                                                }
                                            }
                                            catch { }
                                        }
                                    }
                                }

                                List<List<string>> transformedData = DataTransformer.TransformData(data, employee, projectData, datasets);
                                excelSheet.AddRange(transformedData);

                                int padLength = Math.Max(0, 15 - (employee.Nickname?.Length ?? 0));
                                string padding = new string('*', padLength);
                                Log($"[{sheetName}]-[ {padding} {employee.Nickname} ] ✅ OK.", LogLevel.Success);
                            }
                            catch (Exception ex)
                            {
                                int padLength = Math.Max(0, 15 - (employee.Nickname?.Length ?? 0));
                                string padding = new string('*', padLength);
                                Log($"[{sheetName}]-[ {padding} {employee.Nickname} ] ❌ ERROR: {ex.Message}", LogLevel.Error);
                            }
                        }

                        Log($"Exporting {sheetName} to Excel...", LogLevel.Info);
                        ExcelExporter.Export(excelSheet, sheetName, outputDir);
                    }
                });

                Log("Data extraction completed successfully.", LogLevel.Success);
                MessageBox.Show("TimeCollect execution completed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.Message}", LogLevel.Error);
                MessageBox.Show($"An error occurred: {ex.Message}", "System Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Log("Releasing UI lock.", LogLevel.Info);
                btnRun.IsEnabled = true;
                tabSettings.IsEnabled = true;
            }
        }
    }
}