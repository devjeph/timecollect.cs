using System;
using System.Collections.Generic;
using System.Linq;
using TimeCollect.Core.Models;
using TimeCollect.Core.Helpers;

namespace TimeCollect.Core.Services
{
    /// <summary>
    /// Service responsible for cleaning, padding, and transforming raw 2D string arrays.
    /// </summary>
    public class DataTransformer
    {
        /// <summary>
        /// Executes the primary data restructuring algorithm.
        /// </summary>
        public static List<List<string>> TransformData(List<List<string>> dataList, Employee employee, List<Project> projectData, List<WeekDataset> dataset)
        {
            List<List<string>> transformedData = new List<List<string>>();

            if (dataList != null && dataList.Count > 0 && employee != null)
            {
                // Step 1: Add default "0.00" padding values to the header row to equalize matrix lengths
                dataList[0].AddRange(new List<string> { "0.00", "0.00", "0.00", "0.00" });

                // Step 2: Strip unused data columns based on strict indices defined in source logic
                List<int> columnsToDelete = new List<int> { 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 22, 27, 32, 37, 42, 47, 52, 57, 62, 67, 72 };
                List<List<string>> data = DeleteColumns(dataList, columnsToDelete);

                // Step 3: Copy overarching headers from row 1 down to row 2 for normalization
                for (int i = 0; i < 9; i++) data[1][i] = data[0][i];

                // Step 4: Define work types (Direct/Indirect allocations)
                List<string> workData = Enumerable.Repeat("日付", 3).ToList();
                workData.AddRange(Enumerable.Repeat("間接", 9));
                workData.AddRange(Enumerable.Repeat("直接", 40));

                // Step 5: Propagate project codes across wide column spans
                for (int i = 0; i < 3; i++)
                {
                    int[] jValues = { 13, 17, 21, 25, 29, 33, 37, 41, 45, 49 };
                    foreach (int j in jValues) data[0][i + j] = data[0][j - 1];
                }

                // Step 6: Flatten and structure the 2D matrix into the final row-by-row list format
                for (int col = 0; col < data[0].Count - 3; col++)
                {
                    for (int row = 0; row < data.Count - 2; row++)
                    {
                        // Parse temporal data
                        int year = int.Parse(data[row + 2][0]);
                        int month = int.Parse(data[row + 2][1]);
                        int day = int.Parse(data[row + 2][2]);

                        // Extract metadata
                        string employeeName = employee.Nickname;
                        string employeeTeam = employee.Team;
                        string weekType = WeekTypeHelper.GetName(dataset, year, month, day);
                        string taskType = data[1][col + 3];
                        string projectCode = data[0][col + 3];
                        string workType = workData[col + 3];

                        // Parse hours to float equivalent and round to 2 decimals
                        double workedHours = Math.Round(double.Parse(data[row + 2][col + 3]), 2);

                        // Resolve client lookup
                        string client = ProjectHelper.GetClient(projectCode, projectData);

                        // Construct the final row mapping
                        transformedData.Add(new List<string>
                        {
                            client,
                            (row + 1).ToString(),
                            year.ToString(),
                            month.ToString(),
                            day.ToString(),
                            weekType,
                            employeeName,
                            projectCode,
                            taskType,
                            workType,
                            employeeTeam,
                            workedHours.ToString("F2")
                        });
                    }
                }
            }
            return transformedData;
        }

        /// <summary>
        /// Iterates through a 2D matrix and returns a new matrix excluding specified column indices.
        /// </summary>
        private static List<List<string>> DeleteColumns(List<List<string>> data, List<int> columnIndices)
        {
            List<List<string>> result = new List<List<string>>();
            foreach (var row in data)
            {
                List<string> newRow = new List<string>();
                for (int i = 0; i < row.Count; i++)
                {
                    // Append cell only if its index is not marked for deletion
                    if (!columnIndices.Contains(i)) newRow.Add(row[i]);
                }
                result.Add(newRow);
            }
            return result;
        }
    }
}