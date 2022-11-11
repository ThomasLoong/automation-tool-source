using BITool.Enums;
using BITool.Helpers;
using BITool.Models;
using BITool.Models.SignalR;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using MySqlConnector;
using OfficeOpenXml;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;

namespace BITool.Services
{
    public static class ImportDataService
    {
        private static readonly IReadOnlyList<string> Valid_DateTime_Formats = new List<string>
        {
            "dd/MM/yyyy",
            "dd/MM/yyyy hh:mm:ss",
            "dd-MM-yyyy",
            "dd-MM-yyyy hh:mm:ss"
        };

        private static readonly string[] Valid_First_Phone_Characters = { "666", "668", "669" };

        public static void AddImportDataService(this WebApplication app, string sqlConnectionStr)
        {
            #region Private

            DateTime? CheckValidDate(string input)
            {
                DateTime result;
                foreach (var format in Valid_DateTime_Formats)
                {
                    if (DateTime.TryParseExact(input, format, CultureInfo.CurrentCulture, DateTimeStyles.None, out result))
                        return result;
                }
                return null;
            }

            long? CheckValidPhoneNumber(string input)
            {
                if (string.IsNullOrEmpty(input))
                    return null;

                if (input.Length != 11) //edited by Thomas on 20221111
                    return null;

                if (!Valid_First_Phone_Characters.Contains(input.Substring(0, 3)))
                    return null;

                return long.TryParse(input, out var result)? result:null;
            }

            async Task BulkInsertCustomerModelToMySQL(IEnumerable<CustomerDto> items)
            {
                var watch = Stopwatch.StartNew();
                if (items.Count() == 0) return;
                Console.WriteLine($"BulkInsertCustomerModelToMySQL: count {items.Count()}");
                var dataTable = items.ToDataTable();
                using var connection = new MySqlConnection(sqlConnectionStr);
                connection.Open();
                var commandStr = "CREATE TEMPORARY TABLE IF NOT EXISTS temp_cutomer SELECT * FROM customer LIMIT 0;";
                using (MySqlCommand myCmd = new MySqlCommand(commandStr, connection))
                {
                    myCmd.CommandType = CommandType.Text;
                    await myCmd.ExecuteNonQueryAsync();
                }

                var bulkCopy = new MySqlBulkCopy(connection);
                bulkCopy.DestinationTableName = "temp_cutomer";
                bulkCopy.BulkCopyTimeout = 0;
                var result = await bulkCopy.WriteToServerAsync(dataTable);

                commandStr = "INSERT IGNORE INTO customer (DateFirstAdded, Source, CustomerMobileNo, Status, LastUpdatedBy, LastUpdatedON) " +
                             "SELECT temp_cutomer.DateFirstAdded, temp_cutomer.Source, temp_cutomer.CustomerMobileNo, temp_cutomer.Status, temp_cutomer.LastUpdatedBy, temp_cutomer.LastUpdatedON  " +
                             "FROM temp_cutomer;";
                using (MySqlCommand myCmd = new MySqlCommand(commandStr, connection))
                {
                    myCmd.CommandType = CommandType.Text;
                    await myCmd.ExecuteNonQueryAsync();
                }

                commandStr = "DROP TEMPORARY TABLE IF EXISTS temp_cutomer";
                using (MySqlCommand myCmd = new MySqlCommand(commandStr, connection))
                {
                    myCmd.CommandType = CommandType.Text;
                    await myCmd.ExecuteNonQueryAsync();
                }

                watch.Stop();
                Console.WriteLine($"Complete BulkInsertCustomerModelToMySQL: time {watch.Elapsed.TotalSeconds} s");
            }

            async Task BulkInsertCustomerScoreToMySQL(IEnumerable<CustomerScoreDto> items)
            {
                Console.WriteLine($"BulkInsertCustomerScoreToMySQL: count {items.Count()}");
                if (items.Count() == 0) return;
                var watch = Stopwatch.StartNew();
                var dataTable = items.ToDataTable();
                using var connection = new MySqlConnection(sqlConnectionStr);
                connection.Open();

                var bulkCopy = new MySqlBulkCopy(connection);
                bulkCopy.DestinationTableName = TableName.CustomerScore;
                bulkCopy.BulkCopyTimeout = 0;
                await bulkCopy.WriteToServerAsync(dataTable);

                watch.Stop();
                Console.WriteLine($"Complete BulkInsertCustomerScoreToMySQL: time {watch.Elapsed.TotalSeconds} s");
            }

            async Task BulkInsertCleaningDataHistoryToMySQL(IEnumerable<CleanDataHistory> items)
            {
                Console.WriteLine($"{nameof(BulkInsertCleaningDataHistoryToMySQL)}: count {items.Count()}");
                if (items.Count() == 0) return;
                var watch = Stopwatch.StartNew();
                var dataTable = items.ToDataTable();
                using var connection = new MySqlConnection(sqlConnectionStr);
                connection.Open();

                var bulkCopy = new MySqlBulkCopy(connection);
                bulkCopy.DestinationTableName = TableName.CleanDataHistory;
                bulkCopy.BulkCopyTimeout = 0;
                await bulkCopy.WriteToServerAsync(dataTable);

                watch.Stop();
                Console.WriteLine($"Complete {nameof(BulkInsertCleaningDataHistoryToMySQL)}: time {watch.Elapsed.TotalSeconds} s");
            }
            List<AdminScoreDto> GetAdminScores()
            {
                using var connection = new MySqlConnection(sqlConnectionStr);
                return connection.Query<AdminScoreDto>("SELECT * FROM adminscore").ToList();
            }

            List<BaseDropdown> GetAdminCampaigns()
            {
                using var connection = new MySqlConnection(sqlConnectionStr);
                return connection.Query<BaseDropdown>("select Id, Name from Campaign where IsDeleted = 0").ToList();
            }

            #endregion Private

            app.MapGet("data/getAdminCampaigns", [Authorize] async Task<IResult> (IMemoryCache memoryCache) =>
            {
                return Results.Ok(GetAdminCampaigns());
            });

            app.MapGet("data/getAdminScores", [Authorize] async Task<IResult> (IMemoryCache memoryCache) =>
            {
                List<AdminScoreDto> items = null;
                if (memoryCache.TryGetValue(CacheKeys.GetAdminScoresKey, out items))
                    return Results.Ok(items);

                items = GetAdminScores();
                var cacheOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromHours(24));
                memoryCache.Set(CacheKeys.GetAdminScoresKey, items, cacheOptions);
                return Results.Ok(items);
            });

            app.MapPost("data/importCustomerScore", [Authorize][DisableRequestSizeLimit]
            async Task<IResult> (
                [FromServices] IMemoryCache memoryCache,
                [FromServices] IImportDataToQueueService importDataToQueueService,
                [FromServices] IHttpContextAccessor httpContextAccessor,
                [FromServices] IConfiguration config,
                [FromServices] IHubContext<HubClient> hubContext,
                [FromQuery] string? signalRConnectionId,
                [FromQuery] string? sourceName,
                HttpRequest request) =>
            {
                var watch = Stopwatch.StartNew();
                Console.WriteLine($"signalRConnectionId {signalRConnectionId}");
                if (!request.Form.Files.Any())
                    throw new Exception("No file found!");

                var shouldSendEmailWhenReachLimit = config.GetValue<int>("ShouldSendEmailWhenReachLimit");
                var userEmail = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Email);
                var userIdSr = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                int.TryParse(userIdSr, out var userId);
                List<AdminScoreDto> adminScores = null;
                if (!memoryCache.TryGetValue(CacheKeys.GetAdminScoresKey, out adminScores))
                {
                    adminScores = GetAdminScores();
                    var cacheOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromHours(24));
                    memoryCache.Set(CacheKeys.GetAdminScoresKey, adminScores, cacheOptions);
                }
                var scoreTiltles = adminScores.Select(p => p.ScoreTitle.ToLower());
                var totalRows = 0;
                var shouldSendEmail = false;
                var errorList = new List<CustomerImportErrorDto>();
                foreach (var formFile in request.Form.Files)
                {
                    if (formFile is null || formFile.Length == 0)
                        continue;
                    var customerRows = new List<CustomerDto>();
                    var customerScoreRows = new List<CustomerScoreDto>();
                    var importProcess = new ProcessImportedData { FileName = formFile.FileName, UserEmail = userEmail, SignalRConnectionId = signalRConnectionId };
                    var rowCount = 0;
                    //Process excel file
                    using (var stream = new MemoryStream())
                    {
                        await formFile.CopyToAsync(stream);
                        using (ExcelPackage package = new ExcelPackage(stream))
                        {
                            ExcelWorksheet worksheet = package.Workbook.Worksheets.FirstOrDefault();
                            if (worksheet == null) continue;
                            //throw new Exception("No worksheet found!");

                            rowCount = worksheet.Dimension.Rows; //include header
                            if (rowCount <= 1) continue;
                            // throw new Exception("No Data in worksheet found!");

                            totalRows += rowCount - 1;
                            //read excel file data and add data
                            long? validPhoneNumber = null;
                            var isValidScoreTiltles = true;
                            var isValidSource = true;
                            string source;
                            string customerMobileNo;
                            string scoreTitle;
                            var now = DateTime.Now;
                            int scoreId;
                            var cells = new List<string>();
                            var errorDetails = new List<string>();
                            for (int row = 2; row <= rowCount; row++)
                            {
                                source = sourceName ?? (worksheet.Cells[row, 1]?.Text ?? string.Empty).Trim();
                                customerMobileNo = (worksheet.Cells[row, 2]?.Text ?? string.Empty).Trim();
                                scoreTitle = (worksheet.Cells[row, 3]?.Text ?? string.Empty).Trim();
                                //parsedDateOccurred = CheckValidDate(dateOccurred);
                                validPhoneNumber = CheckValidPhoneNumber(customerMobileNo);
                                isValidScoreTiltles = string.IsNullOrEmpty(scoreTitle) || scoreTiltles.Contains(scoreTitle.ToLower());
                                isValidSource = !string.IsNullOrEmpty(source);
                                //Console.WriteLine($"{dateOccurred} {customerMobileNo} {scoreTitle}");
                                //if (parsedDateOccurred is null)
                                //{
                                //    cells.Add($"A{row}");
                                //    errorDetails.Add("invalid date");
                                //}

                                if (isValidSource && validPhoneNumber != null && isValidScoreTiltles)
                                {
                                    scoreId = adminScores.FirstOrDefault(q => q.ScoreTitle.Equals(scoreTitle, StringComparison.OrdinalIgnoreCase))?.ScoreID ?? 0;
                                    importProcess.CustomerImports.Add(new CustomerImportDto { Source = source, CustomerMobileNo = validPhoneNumber.Value, DateOccurred = now, ScoreIds = new List<int> { scoreId } });
                                    customerRows.Add(new CustomerDto { Source = source, DateFirstAdded = now, CustomerMobileNo = validPhoneNumber.Value, LastUpdatedBy = userId });
                                    if (!string.IsNullOrEmpty(scoreTitle))
                                        customerScoreRows.Add(new CustomerScoreDto { Source = source, CustomerMobileNo = validPhoneNumber.Value, DateOccurred = now, ScoreID = scoreId, LastUpdatedBy = userId });
                                }
                                else
                                {
                                    if (!isValidSource)
                                    {
                                        cells.Add($"A{row}");
                                        errorDetails.Add("invalid source");
                                    }
                                    if (validPhoneNumber == null)
                                    {
                                        cells.Add($"B{row}");
                                        errorDetails.Add("invalid mobile No");
                                    }
                                    if (!isValidScoreTiltles)
                                    {
                                        cells.Add($"C{row}");
                                        errorDetails.Add("invalid score title");
                                    }
                                    errorList.Add(new CustomerImportErrorDto
                                    {
                                        Cell = string.Join(" - ", cells),
                                        ErrorDetail = string.Join(" - ", errorDetails),
                                        Source = source,
                                        CustomerMobileNo = customerMobileNo,
                                        ScoreTitle = scoreTitle
                                    });
                                    cells = new List<string>(); //reset after add
                                    errorDetails = new List<string>(); //reset after add
                                }
                            }
                        }
                    }
                    Console.WriteLine($"Complete valid data from excel: time {watch.Elapsed.TotalSeconds} s");
                    Task insertCustomerModelTask = Task.Run(() => BulkInsertCustomerModelToMySQL(customerRows));
                    Task insertCustomerScoreTask = Task.Run(() => BulkInsertCustomerScoreToMySQL(customerScoreRows));
                    await Task.WhenAll(insertCustomerModelTask, insertCustomerScoreTask);
                    var importHistories = customerRows.GroupBy(p => p.Source)
                                    .Select(p => new ImportDataHistory { ImportName = ImportNames.ImportCustomerScore, Source = p.Key, FileName = formFile.FileName, ImportByEmail = userEmail, TotalRows = p.Count() });
                    importDataToQueueService.InsertImportHistory(sqlConnectionStr, importHistories);
                    importProcess.ShouldSendEmail = importProcess.CustomerImports.Count > shouldSendEmailWhenReachLimit;
                    shouldSendEmail = shouldSendEmail || importProcess.ShouldSendEmail;
                    importDataToQueueService.InsertOrUpdateLeadManagementReport(sqlConnectionStr, importProcess);
                    watch.Stop();
                    Console.WriteLine($"Complete Import data: time {watch.Elapsed.TotalSeconds} s");
                }
                return Results.Ok(new { totalRows, errorList, shouldSendEmail });
            });

            app.MapPost("data/importTotalPoints", [Authorize][DisableRequestSizeLimit]
            async Task<IResult> (
            [FromServices] IMemoryCache memoryCache,
            [FromServices] IImportDataToQueueService importDataToQueueService,
            [FromServices] IHttpContextAccessor httpContextAccessor,
            HttpRequest request) =>
            {
                var watch = Stopwatch.StartNew();
                if (!request.Form.Files.Any())
                    throw new Exception("No file found!");

                var userIdSr = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                int.TryParse(userIdSr, out var userId);
                List<AdminScoreDto> adminScores = null;
                if (!memoryCache.TryGetValue(CacheKeys.GetAdminScoresKey, out adminScores))
                {
                    adminScores = GetAdminScores();
                    var cacheOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromHours(24));
                    memoryCache.Set(CacheKeys.GetAdminScoresKey, adminScores, cacheOptions);
                }
                var errorList = new List<CustomerTotalPointsImportError>();
                var formFile = request.Form.Files.FirstOrDefault();
                if (formFile is null || formFile.Length == 0)
                    throw new Exception("No file found!");

                var customerTotalPoints = new List<CustomerTotalPointsImport>();
                var rowCount = 0;
                string customerMobileNo;
                string totalPointsStr;
                int totalPoints;
                bool isValidTotalPoints = true;
                //Process excel file
                using (var stream = new MemoryStream())
                {
                    await formFile.CopyToAsync(stream);
                    using (ExcelPackage package = new ExcelPackage(stream))
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets.FirstOrDefault();

                        rowCount = worksheet.Dimension.Rows; //include header
                        if (rowCount <= 1)
                            throw new Exception("No Data in worksheet found!");

                        //read excel file data and add data
                        long? validPhoneNumber = null;                        
                        var cells = new List<string>();
                        var errorDetails = new List<string>();
                        //worksheet.Cells[2, 1, rowCount, 1].Style.Numberformat.Format = "dd/MM/yyyy";
                        //worksheet.Cells[2, 3, rowCount, 3].Style.Numberformat.Format = "text";
                        for (int row = 2; row <= rowCount; row++)
                        {
                            customerMobileNo = (worksheet.Cells[row, 1]?.Text ?? string.Empty).Trim();
                            totalPointsStr = (worksheet.Cells[row, 2]?.Text ?? string.Empty).Trim();

                            validPhoneNumber = CheckValidPhoneNumber(customerMobileNo);
                            isValidTotalPoints = int.TryParse(totalPointsStr, out totalPoints);

                            if (validPhoneNumber != null && isValidTotalPoints)
                            {
                                customerTotalPoints.Add(new CustomerTotalPointsImport {CustomerMobileNo= validPhoneNumber.Value, TotalPoints= totalPoints });
                            }
                            else
                            {

                                if (validPhoneNumber == null)
                                {
                                    cells.Add($"A{row}");
                                    errorDetails.Add("Invalid mobile No");
                                }
                                if (!isValidTotalPoints)
                                {
                                    cells.Add($"B{row}");
                                    errorDetails.Add("Invalid total points");
                                }
                                errorList.Add(new CustomerTotalPointsImportError
                                {
                                    Cell = string.Join(" - ", cells),
                                    ErrorDetail = string.Join(" - ", errorDetails),
                                    CustomerMobileNo = customerMobileNo,
                                    TotalPoints = totalPointsStr
                                });
                                cells = new List<string>(); //reset after add
                                errorDetails = new List<string>(); //reset after add
                            }
                        }
                    }
                }
                Console.WriteLine($"Complete valid data from excel: time {watch.Elapsed.TotalSeconds} s");
                if (customerTotalPoints.Any())
                {
                    var dataTable = customerTotalPoints.ToDataTable();                    
                    using var connection = new MySqlConnection(sqlConnectionStr);
                    connection.Open();
                    var commandStr = "CREATE TEMPORARY TABLE IF NOT EXISTS temp_customerTotalPoints (CustomerMobileNo BIGINT NOT NULL,  TotalPoints INT NOT NULL, PRIMARY KEY (CustomerMobileNo));";
                    using (MySqlCommand myCmd = new MySqlCommand(commandStr, connection))
                    {
                        myCmd.CommandType = CommandType.Text;
                        myCmd.ExecuteNonQuery();
                    }

                    var bulkCopy = new MySqlBulkCopy(connection);
                    bulkCopy.DestinationTableName = "temp_customerTotalPoints";
                    bulkCopy.BulkCopyTimeout = 0;
                    await bulkCopy.WriteToServerAsync(dataTable);
                    commandStr = "UPDATE leadmanagementreport AS l " +
                                 "INNER JOIN temp_customerTotalPoints AS t " +
                                 "ON l.CustomerMobileNo = t.CustomerMobileNo " +
                                 "SET " +
                                 "l.TotalPoints = (l.TotalPoints + t.TotalPoints)"
                                 ;
                    using (MySqlCommand myCmd = new MySqlCommand(commandStr, connection))
                    {
                        myCmd.CommandType = CommandType.Text;
                        myCmd.ExecuteNonQuery();
                    }

                    commandStr = "DROP TEMPORARY TABLE IF EXISTS temp_customerTotalPoints";
                    using (MySqlCommand myCmd = new MySqlCommand(commandStr, connection))
                    {
                        myCmd.CommandType = CommandType.Text;
                        myCmd.ExecuteNonQuery();
                    }
                }
                watch.Stop();
                Console.WriteLine($"Complete Import data: time {watch.Elapsed.TotalSeconds} s");

                return Results.Ok(new { totalRows= rowCount -1, errorList, shouldSendEmail=false });//remove the header in total rows
            });

            app.MapPost("data/compareCustomerMobiles", [Authorize][DisableRequestSizeLimit]
            async Task<IResult> (
                [FromServices] IImportDataToQueueService importDataToQueueService,                
                HttpRequest request) =>
            {
                var watch = Stopwatch.StartNew();
               
                if (!request.Form.Files.Any())
                    throw new Exception("No file found!");

                var formFile = request.Form.Files.FirstOrDefault();

                if (formFile is null || formFile.Length == 0)
                    throw new Exception("No file found!");

                var inputList = new List<CleanCustomerInput>();
                var rowCount = 0;
                using (var stream = new MemoryStream())
                {
                    formFile.CopyTo(stream);
                    using (ExcelPackage package = new ExcelPackage(stream))
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                            throw new Exception("No worksheet found!");

                        rowCount = worksheet.Dimension.Rows;
                        if (rowCount == 0)
                            throw new Exception("No Data in worksheet found!");

                        //read excel file data and add data
                        for (int row = 1; row <= rowCount; row++) //file has no header
                        {
                            inputList.Add(new CleanCustomerInput
                            {
                                Source = (worksheet.Cells[row, 1]?.Text ?? string.Empty).Trim(),
                                CustomerMobileNo = (worksheet.Cells[row, 2]?.Text ?? string.Empty).Trim()
                            });
                        }
                    }
                }
                inputList = inputList.Where(p => !string.IsNullOrEmpty(p.Source)).ToList();
                var result = new List<CleanCustomerInput>();
                var records = new List<CleanDataHistory>();
                var now = DateTime.Now;
                var tempTotal = 0;
                if (inputList.Any())
                {
                    var processList = inputList.GroupBy(p => p.Source).Select(p => new CleanCustomerProcess { Source = p.Key, MobileList = p.Select(q => q.CustomerMobileNo).ToList() }).ToList();
                    foreach (var sourceItem in processList)
                    {
                        watch.Restart();
                        tempTotal = sourceItem.MobileList.Count;
                        var recordItem = new CleanDataHistory
                        {
                            Source = sourceItem.Source,
                            FileName = formFile.FileName,
                            TotalRows = sourceItem.MobileList.Count,
                            CleanTime = now
                        };
                        sourceItem.MobileList = sourceItem.MobileList.Distinct().ToList();
                        recordItem.TotalDuplicateNumbersInFile = tempTotal - sourceItem.MobileList.Count;                         
                        tempTotal = sourceItem.MobileList.Count;
                        sourceItem.MobileList = sourceItem.MobileList.Where(p => CheckValidPhoneNumber(p) != null).ToList();
                        recordItem.TotalInvalidNumbers = tempTotal - sourceItem.MobileList.Count;
                        watch.Stop();
                        Console.WriteLine($"Source: {sourceItem.Source}, Total records to table: {sourceItem.MobileList.Count}, time: {watch.Elapsed.TotalSeconds}");
                        watch.Restart();
                        var dataTable = sourceItem.MobileList.Select(p => new CleanCustomerOutput { Source = sourceItem.Source, CustomerMobileNo = long.Parse(p) }).ToDataTable();
                        using var connection = new MySqlConnection(sqlConnectionStr);
                        connection.Open();
                        var commandStr = "create temporary table IF NOT EXISTS TempCustomerMobileList(Source VARCHAR(100) NOT NULL, CustomerMobileNo BIGINT NOT NULL, INDEX(CustomerMobileNo), PRIMARY KEY(CustomerMobileNo));";
                        using (MySqlCommand myCmd = new MySqlCommand(commandStr, connection))
                        {
                            myCmd.CommandType = CommandType.Text;
                            myCmd.ExecuteNonQuery();
                        }
                        Console.WriteLine($"create temporary table TempCustomerMobileList");                       
                        var bulkCopy = new MySqlBulkCopy(connection);
                        bulkCopy.DestinationTableName = "TempCustomerMobileList";
                        bulkCopy.BulkCopyTimeout = 0;
                        await bulkCopy.WriteToServerAsync(dataTable);
                        watch.Stop();
                        Console.WriteLine($"WriteToServerAsync TempCustomerMobileList time: {watch.Elapsed.TotalSeconds}");
                        watch.Restart();
                        commandStr = "select t.Source, t.CustomerMobileNo " +
                                     "from TempCustomerMobileList  t " +
                                     "left join Customer c " +
                                     "on t.CustomerMobileNo = c.CustomerMobileNo " +
                                     "where c.CustomerMobileNo is NULL";
                        var noDuplicate = connection.Query<CleanCustomerInput>(commandStr).ToList();
                        watch.Stop();
                        Console.WriteLine($"query join table TempCustomerMobileList, time: {watch.Elapsed.TotalSeconds}");
                        //watch.Restart();
                        tempTotal = sourceItem.MobileList.Count;
                        recordItem.TotalDuplicateNumbersWithSystem = tempTotal - noDuplicate.Count;
                        result.AddRange(noDuplicate);
                        commandStr = "DROP TEMPORARY TABLE IF EXISTS TempCustomerMobileList";
                        using (MySqlCommand myCmd = new MySqlCommand(commandStr, connection))
                        {
                            myCmd.CommandType = CommandType.Text;
                            myCmd.ExecuteNonQuery();
                        }
                        records.Add(recordItem);
                    }
                }
                //await BulkInsertCleaningDataHistoryToMySQL(records);
                importDataToQueueService.BulkInsertCleaningDataHistory(sqlConnectionStr, records);
                watch.Stop();
                Console.WriteLine($"compareCustomerMobiles: time {watch.Elapsed.TotalSeconds} s");
                return Results.Ok(new { TotalRows = rowCount, mobileNumberList = result });
            });

            app.MapPost("data/importCleanedMobileNumberList", [Authorize][DisableRequestSizeLimit]
            async Task<IResult> (
                IImportDataToQueueService importDataToQueueService,
                IHttpContextAccessor httpContextAccessor,
                IConfiguration config,
                [FromQuery] string fileName,
                [FromQuery] string signalRConnectionId,
                [FromBody] List<CleanCustomerInput> input) =>
            {
                var watch = Stopwatch.StartNew();
                var now = DateTime.Now;
                Console.WriteLine("input.Count" + input.Count);
                var userEmail = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Email);
                var userIdSr = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                int.TryParse(userIdSr, out var userId);
                var importProcess = new ProcessImportedData { FileName = fileName, UserEmail = userEmail, SignalRConnectionId = signalRConnectionId };
                importProcess.CustomerImports = input.Select(p => new CustomerImportDto { CustomerMobileNo = long.Parse(p.CustomerMobileNo), Source = p.Source, DateOccurred = now }).ToList();
                await BulkInsertCustomerModelToMySQL(input.Select(p => new CustomerDto { CustomerMobileNo = long.Parse(p.CustomerMobileNo), Source = p.Source, LastUpdatedBy = userId }));
                var importHistories = input.GroupBy(p => p.Source)
                                .Select(p => new ImportDataHistory { ImportName = ImportNames.ImportCleanedCustomerMobileNumber, Source = p.Key, FileName = fileName, ImportByEmail = userEmail, TotalRows = p.Count() });
                importDataToQueueService.InsertImportHistory(sqlConnectionStr, importHistories);
                importProcess.ShouldSendEmail = importProcess.CustomerImports.Count > config.GetValue<int>("ShouldSendEmailWhenReachLimit");
                importDataToQueueService.InsertOrUpdateLeadManagementReport(sqlConnectionStr, importProcess);
                watch.Stop();
                Console.WriteLine($"Complete importCleanedMobileNumberList: time {watch.Elapsed.TotalSeconds} s");
                return Results.Ok(new { TotalRows = input.Count, importProcess.ShouldSendEmail });
            });
        }
    }
}