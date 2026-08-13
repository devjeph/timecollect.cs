using System;
using System.Collections.Generic;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace TimeCollect.Core.Services
{
    /// <summary>
    /// Handles extraction and initial sanitization of Google Sheets data.
    /// </summary>
    public class GoogleSheetsService
    {
        /// <summary>
        /// Retrieves data from Google Sheets and replaces any blank values with "0.00" 
        /// to maintain matrix integrity for the transformation pipeline.
        /// </summary>
        public static List<List<string>> GetData(SheetsService service, string spreadsheetId, string rangeName)
        {
            List<List<string>> sanitizedData = new List<List<string>>();

            try
            {
                // Execute the API request
                SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get(spreadsheetId, rangeName);
                ValueRange response = request.Execute();
                IList<IList<object>> values = response.Values;

                if (values != null && values.Count > 0)
                {
                    // Iterate through the 2D matrix
                    foreach (var row in values)
                    {
                        List<string> newRow = new List<string>();
                        foreach (var cell in row)
                        {
                            string? cellValue = cell?.ToString();

                            // Explicit logic translating the Python "if not value:" check
                            if (string.IsNullOrWhiteSpace(cellValue))
                            {
                                newRow.Add("0.00");
                            }
                            else
                            {
                                newRow.Add(cellValue);
                            }
                        }
                        sanitizedData.Add(newRow);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during data collection: {ex.Message}");
            }

            return sanitizedData;
        }
    }
}