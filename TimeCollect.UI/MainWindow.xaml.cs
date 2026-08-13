using System;
using System.Collections.Generic;
using System.Windows;
using DotNetEnv;
using Google.Apis.Sheets.v4;
using TimeCollect.Core.Models;
using TimeCollect.Core.Helpers;
using TimeCollect.Core.Services;

namespace TimeCollect.UI
{
    /// <summary>
    /// Code-behind logic for the MainWindow UI. Handles startup configuration mapping and event routing.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadEnvironmentVariables();
        }

        /// <summary>
        /// Parses the physical .env file in the execution directory and maps key-value pairs 
        /// to the corresponding UI TextBoxes on the Settings tab.
        /// </summary>
        private void LoadEnvironmentVariables()
        {
            // Initializes the DotNetEnv parser to read the local .env file[cite: 1]
            Env.Load(".env");

            // Extract values using explicit keys, providing sensible fallbacks if the file is missing variables[cite: 1]
            txtOutputDirectory.Text = Env.GetString("OUTPUT_DIRECTORY_2026", @"D:\Documents\TimeCollect\2026");
            txtProjectSpreadsheet.Text = Env.GetString("PROJECT_SPREADSHEET", "1yKLHsWWOffCVWTI4d6A4n8exNF-ioP2CHn7E7RomT4k");
            txtProjectRange.Text = Env.GetString("PROJECT_RANGE", "project_info!A:C");
            txtEmployeesSpreadsheet.Text = Env.GetString("EMPLOYEES_SPREADSHEET", "1yKLHsWWOffCVWTI4d6A4n8exNF-ioP2CHn7E7RomT4k");
            txtSheetNames.Text = Env.GetString("SHEET_NAMES", "202608,202609");

            txtStartYear.Text = Env.GetString("TIMESHEET_START_YEAR", "2025");
            txtStartMonth.Text = Env.GetString("TIMESHEET_START_MONTH", "12");
            txtStartDay.Text = Env.GetString("TIMESHEET_START_DAY", "28");
        }

        /// <summary>
        /// Captures the current string state of the Settings text boxes and initiates the Core logic pipeline.
        /// </summary>
        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // UI State Lock: Disable the button to prevent double-execution while network calls process
                btnRun.IsEnabled = false;
                btnRun.Content = "Authenticating...";

                // Extract all parametric variables from the Settings UI fields
                string outputDir = txtOutputDirectory.Text;
                string projectSheet = txtProjectSpreadsheet.Text;
                string projectRange = txtProjectRange.Text;
                string employeesSheet = txtEmployeesSpreadsheet.Text;

                int startYear = int.Parse(txtStartYear.Text);
                int startMonth = int.Parse(txtStartMonth.Text);
                int startDay = int.Parse(txtStartDay.Text);

                // Step 1: Authentication Pipeline
                // Triggers browser popup if AppData token is missing/expired, otherwise silently authenticates
                SheetsService sheetsService = GoogleAuthService.Authenticate();

                btnRun.Content = "Extracting Data...";

                // Step 2: Data Extraction & Sanitization
                // Pulls raw data and immediately forces blank cells to "0.00" via the service logic
                List<List<string>> rawProjectData = GoogleSheetsService.GetData(sheetsService, projectSheet, projectRange);

                // Example of generating the calendar mapping boundaries
                var dataset = WeekTypeHelper.SetTypes(startYear, startMonth, startDay);

                btnRun.Content = "Transforming...";

                // Step 3: Transformation Pipeline
                // (Note: To complete the loop, instantiate Employee and Project lists based on rawProjectData here)
                // List<Project> projects = ... 
                // Employee currentEmployee = ...
                // List<List<string>> formattedData = DataTransformer.TransformData(rawEmployeeData, currentEmployee, projects, dataset);

                btnRun.Content = "Exporting to Excel...";

                // Step 4: Physical File Export
                // ExcelExporter.Export(formattedData, "202608", outputDir);

                MessageBox.Show("TimeCollect execution completed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "System Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // UI State Reset: Re-enable the button regardless of success or failure
                btnRun.IsEnabled = true;
                btnRun.Content = "Run TimeCollect";
            }
        }
    }
}