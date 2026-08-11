using System;
using System.Collections.Generic;
using System.Globalization;
using TimeCollect.Core.Models;

namespace TimeCollect.Core.Helpers
{
    /// <summary>
    /// Utility class for project data lookups.
    /// </summary>
    public static class ProjectHelper
    {
        /// <summary>
        /// Retrieves the client associated with a given project code from a list of projects.
        /// Performs a linear search through the list to find the matching project client.
        /// </summary>
        /// <param name="projectCode"></param>
        /// <param name="projects"></param>
        /// <returns>The matched client string, or "YTP" if no match is found.</returns>
        public static string GetClient(string projectCode, List<Project> projects)
        {
            foreach (var project in projects)
            {
                if (projectCode == project.ProjectCode)
                {
                    return project.ProjectClient;
                }
            }
            return "YTP";
        }
    }
    /// <summary>
    /// Utility class handling the complex ISO calendar week generation and custom formatting rules.
    /// </summary>
    public static class WeekTypeHelper
    {
        /// <summary>
        /// Determines the custom "Week Type" string for a specific target date.
        /// </summary>
        /// <param name="datasets"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <returns></returns>
        public static string GetName(List<WeekDataset> datasets, int year, int month, int day)
        {
            DateTime targetDate = new DateTime(year, month, day);
            foreach (var data in datasets)
            {
                // Check if the target date falls within the start and end dates of the dataset.
                if (data.StartDate <= targetDate && targetDate <= data.EndDate)
                {
                    return data.WeekType;
                }
            }
            return null;
        }

        /// <summary>
        /// Generates a 104-week span of calculated date boundaries starting from a given sunday.
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static List<WeekDataset> SetTypes(int year, int month, int day)
        {
            var datasets = new List<WeekDataset>();
            DateTime startDate = new DateTime(year, month, day);

            // Enforce Sunday start requirement from original logic
            if (startDate.DayOfWeek != DayOfWeek.Sunday)
            {
                throw new ArgumentException($"Date must be a Sunday. You inputted a {startDate.DayOfWeek}.");
                return datasets;
            }

            // Iterate through a 104-week span (2 years)
            for (int weekIndex = 0; weekIndex < 104; weekIndex++)
            {
                DateTime sunday = startDate.AddDays(weekIndex * 7);
                DateTime saturday = sunday.AddDays(6);

                // Break condition: Stop if the year exceeds the specified year + 2
                if (sunday.Year > year + 2)
                {
                    break;
                }

                string weekType = SetName(sunday, saturday);

                datasets.Add(new WeekDataset
                {
                    StartDate = sunday,
                    EndDate = saturday,
                    WeekNumber = weekIndex + 1,
                    WeekType = weekType
                });
            }

            // Hardcoded override for the final element as specified.
            if (datasets.Count > 0) datasets[datasets.Count - 1].WeekType = "12to1";

            return datasets;
        }

        private static string SetName(DateTime startDate, DateTime endDate)
        {
            try
            {
                Calendar calendar = CultureInfo.CurrentCulture.Calendar;
                // Calculate ISO week numbers using Monday as the first day of the week
                int startWeek = calendar.GetWeekOfYear(startDate, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                int endWeek = calendar.GetWeekOfYear(endDate, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

                int startMonth = startDate.Month;
                int endMonth = endDate.Month;

                // Base calculation for alphabetic week suffix (A, B, C, D)
                char baseChar = (char)('A' + (startDate.Day - 1) / 7);
                string weekName = $"{startMonth}{baseChar}";

                // Conditional formatting overrides based on month and year boundaries
                if (startMonth != endMonth && startDate.Year != endDate.Year) weekName = $"{endMonth}A";
                if (startMonth == 1) weekName = $"{startMonth}{(char)('B' + (startDate.Day - 1) / 7)}";
                if (startMonth != endMonth && startDate.Year == endDate.Year) weekName = $"{startMonth}to{endMonth}";
                if (startWeek == endWeek) weekName = $"{startMonth}{baseChar}";

                return weekName;
            }
            catch (Exception)
            {
                return "Invalid date";
            }
        }
            
    }

    
}
