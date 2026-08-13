using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using DotNetEnv;
using Google.Apis.Sheets.v4;
using TimeCollect.Core.Models;
using TimeCollect.Core.Helpers;
using TimeCollect.Core.Services;

namespace TimeCollect.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadEnvironmentVariables();
            Log("System initialized. Ready for execution.");
        }

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
                // Fallback to current date if .env mapping fails
                dpStartDate.SelectedDate = DateTime.Today;
            }
        }

        /// <summary>
        /// Thread-safe logging mechanism. Forces UI thread to update the TextBox
        /// and automatically scrolls to the newest entry.
        /// </summary>
        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                txtLog.ScrollToEnd();
            });
        }

        /// <summary>
        /// Uses async/await to push the heavy lifting to a background thread, keeping the UI responsive.
        /// </summary>
        private async void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Safely extract the selected date from the DatePicker, defaulting to today if null
                DateTime startDate = dpStartDate.SelectedDate ?? DateTime.Today;

                // Execution Guard Clause
                if (startDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    MessageBox.Show("Execution blocked: Start date is not a Sunday.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                btnRun.IsEnabled = false;
                txtLog.Clear();

                string outputDir = txtOutputDirectory.Text;
                string projectSheet = txtProjectSpreadsheet.Text;
                string projectRange = txtProjectRange.Text;
                string employeesSheet = txtEmployeesSpreadsheet.Text;
                string rawSheetNames = txtSheetNames.Text;

                // Safely extract the selected date from the DatePicker, defaulting to today if null
                //DateTime startDate = dpStartDate.SelectedDate ?? DateTime.Today;
                int startYear = startDate.Year;
                int startMonth = startDate.Month;
                int startDay = startDate.Day;

                await Task.Run(() =>
                {
                    // 1. Authenticate & Setup
                    SheetsService creds = GoogleAuthService.Authenticate();
                    if (creds != null) Log("🌐 Connected to Google API.");

                    var datasets = WeekTypeHelper.SetTypes(startYear, startMonth, startDay);

                    // 2. Fetch Projects
                    List<List<string>> rawProjectData = GoogleSheetsService.GetData(creds, projectSheet, projectRange);
                    List<Project> projectData = DataParser.ParseProjects(rawProjectData);
                    Log("📝 Timesheet collection started...");

                    // Safe parsing of sheet names
                    string[] sheetNames = rawSheetNames.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    // 3. The Main Sheet Loop
                    foreach (string rawSheetName in sheetNames)
                    {
                        string sheetName = rawSheetName.Trim();
                        if (string.IsNullOrEmpty(sheetName)) continue;

                        Log($"\nCollecting timesheet [{sheetName}] data");

                        // A. Fetch Employees dynamically for this specific sheet tab
                        List<List<string>> rawEmployeeData = GoogleSheetsService.GetData(creds, employeesSheet, $"{sheetName}!A:E");

                        if (rawEmployeeData == null || rawEmployeeData.Count == 0)
                        {
                            Log("❌ ERROR: No employee data collected.");
                            continue;
                        }

                        List<Employee> employees = DataParser.ParseEmployees(rawEmployeeData, skipHeader: false);

                        // B. Initialize the master list for the final Excel output
                        List<List<string>> excelSheet = new List<List<string>>();

                        // C. The Employee Timesheet Loop
                        foreach (Employee employee in employees)
                        {
                            try
                            {
                                // Fetch timesheet from A7:BU39
                                List<List<string>> data = GoogleSheetsService.GetData(creds, employee.SpreadsheetId, $"{sheetName}!A7:BU39");

                                if (data == null || data.Count == 0)
                                {
                                    throw new Exception("No data returned from API.");
                                }

                                // Transform and append to master list
                                List<List<string>> transformedData = DataTransformer.TransformData(data, employee, projectData, datasets);
                                excelSheet.AddRange(transformedData);

                                // Perfect Terminal Alignment
                                int padLength = Math.Max(0, 15 - (employee.Nickname?.Length ?? 0));
                                string padding = new string('*', padLength);
                                Log($"[{sheetName}]-[ {padding} {employee.Nickname} ] ✅ OK.");
                            }
                            catch (Exception ex)
                            {
                                int padLength = Math.Max(0, 15 - (employee.Nickname?.Length ?? 0));
                                string padding = new string('*', padLength);
                                Log($"[{sheetName}]-[ {padding} {employee.Nickname} ] ❌ ERROR: {ex.Message}");
                            }
                        }

                        // D. Export the massive combined list once per sheet
                        Log($"Exporting {sheetName} to Excel...");
                        ExcelExporter.Export(excelSheet, sheetName, outputDir);
                    }
                });

                Log("\nData pipeline execution completed successfully.");
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.Message}");
            }
            finally
            {
                btnRun.IsEnabled = true;
            }
        }

        /// <summary>
        /// Validates that the selected date is a Sunday. If not, auto-corrects to the previous Sunday.
        /// </summary>
        private void DpStartDate_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
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

                    // Calculate the offset to the previous Sunday and apply the correction
                    int daysToSubtract = (int)selectedDate.DayOfWeek;
                    dpStartDate.SelectedDate = selectedDate.AddDays(-daysToSubtract);
                }
            }
        }

    }
}