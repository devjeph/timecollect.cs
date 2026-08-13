using System;
using System.Collections.Generic;
using TimeCollect.Core.Models;

namespace TimeCollect.Core.Services
{
    /// <summary>
    /// Translates raw 2D string matrices from Google Sheets into strongly-typed C# objects.
    /// Built to handle jagged arrays where the API truncates trailing empty cells.
    /// </summary>
    public class DataParser
    {
        /// <summary>
        /// Maps raw sheet data to Project objects.
        /// </summary>
        public static List<Project> ParseProjects(List<List<string>> rawData, bool skipHeader = true)
        {
            var projects = new List<Project>();
            int startIndex = skipHeader ? 1 : 0;

            for (int i = startIndex; i < rawData.Count; i++)
            {
                var row = rawData[i];
                if (row == null || row.Count == 0) continue;

                // Local function to safely extract data without OutOfBounds exceptions
                string GetValue(int index) => row.Count > index ? row[index].Trim() : string.Empty;

                projects.Add(new Project
                {
                    ProjectCode = GetValue(0),
                    ProjectName = GetValue(1),
                    ProjectClient = GetValue(2)
                });
            }
            return projects;
        }

        /// <summary>
        /// Maps raw sheet data to Employee objects.
        /// </summary>
        public static List<Employee> ParseEmployees(List<List<string>> rawData, bool skipHeader = false)
        {
            var employees = new List<Employee>();
            int startIndex = skipHeader ? 1 : 0;

            for (int i = startIndex; i < rawData.Count; i++)
            {
                var row = rawData[i];
                if (row == null || row.Count == 0) continue;

                string GetValue(int index) => row.Count > index ? row[index].Trim() : string.Empty;

                int.TryParse(GetValue(0), out int empId);

                employees.Add(new Employee
                {
                    Id = empId,
                    Name = GetValue(1),
                    Nickname = GetValue(2),
                    Team = GetValue(3),
                    SpreadsheetId = GetValue(4)
                });
            }
            return employees;
        }
    }
}