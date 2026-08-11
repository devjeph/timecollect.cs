using System;

namespace TimeCollect.Core.Models
{
    /// <summary>
    /// Represents an employee record mapped directly from the Google Sheet data.
    /// </summary>
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Nickname { get; set; }
        public string SpreadsheetId { get; set; }
        public string Team { get; set; }
    }

    /// <summary>
    /// Represents a specific project and its associated client mapping.
    /// </summary>
    public class Project
    {
        public string ProjectCode { get; set; }
        public string ProjectName { get; set; }
        public string ProjectClient { get; set; }
    }

    /// <summary>
    /// Defines the date boundaries and specific string formatting for a calculated ISO calendar week.
    /// </summary>
    public class WeekDataset
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int WeekNumber { get; set; }
        public string WeekType { get; set; }
    }
}