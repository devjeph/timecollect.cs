using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;

namespace TimeCollect.Core.Services
{
    /// <summary>
    /// Service responsible for managing physical file I/O operations for Excel outputs.
    /// </summary>
    public class ExcelExporter
    {
        /// <summary>
        /// Writes the transformed 2D list array into the specified Excel sheet format.
        /// </summary>
        public static void Export(List<List<string>> data, string sheetName, string outputDirectory)
        {
            // Construct absolute file path and ensure directory exists
            string filePath = Path.Combine(outputDirectory, "TimeCollect.xlsx");
            Directory.CreateDirectory(outputDirectory);

            // Open existing workbook to append data, or create a new instance in memory
            XLWorkbook wb = File.Exists(filePath) ? new XLWorkbook(filePath) : new XLWorkbook();

            // Drop the target sheet if it currently exists to avoid duplicate/stale data
            if (wb.TryGetWorksheet(sheetName, out IXLWorksheet existingSheet)) existingSheet.Delete();

            IXLWorksheet ws = wb.Worksheets.Add(sheetName);

            // Map UTF-8 Japanese headers exactly as specified in the source logic
            var header = new List<string> { "対応", "行番号", "年", "月", "日", "WeekType", "名前", "工号", "種別", "直接/間接", "原寸/3D/管理", "時間" };

            // Write headers (ClosedXML utilizes 1-based indexing for rows and columns)
            for (int i = 0; i < header.Count; i++) ws.Cell(1, i + 1).Value = header[i];

            // Write row data sequentially
            int currentRow = 2;
            foreach (var row in data)
            {
                for (int col = 0; col < row.Count; col++) ws.Cell(currentRow, col + 1).Value = row[col];
                currentRow++;
            }

            // Flush memory stream to disk
            wb.SaveAs(filePath);
        }
    }
}