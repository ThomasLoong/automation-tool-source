using BITool.Enums;
using BITool.Helpers;
using BITool.Models;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using OfficeOpenXml;
using System.Data;
using System.Security.Claims;

namespace BITool.Services
{
    public static class ExportDataService
    {
        #region private

        private static List<long> GetCustomerListByFilter(string sqlConnectionStr,ref ExportDataFilter input)
        {
            ProcessInputValues(ref input);

            using (var conn = new MySqlConnection(sqlConnectionStr))
            {
                conn.Open();
                var cmd = GetMySqlCommandStoreProcedureFromFilter(conn, StoredProcedureName.GetCustomersByFilter, ref input);
                MySqlDataReader rdr = cmd.ExecuteReader();
                var customerData = new List<long>();
                while (rdr.Read())
                {
                    customerData.Add(rdr.GetInt64(0));
                }
                rdr.Close();
                conn.Close();
                return customerData;
            }
        }

        private static long GetTotalCountByFilter(string sqlConnectionStr, ref ExportDataFilter input)
        {
            using (var conn = new MySqlConnection(sqlConnectionStr))
            {
                conn.Open();                
                var cmd = GetMySqlCommandStoreProcedureFromFilter(conn, StoredProcedureName.GetCustomerCountByFilter, ref input);
                MySqlDataReader rdr = cmd.ExecuteReader();
                var data = new List<long>();
                while (rdr.Read())
                    data.Add(rdr.GetInt64(0));
                rdr.Close();
                conn.Close();
                var totalCount = data.FirstOrDefault();
                Console.WriteLine($"customerData.Count: {totalCount}");
                if (totalCount > input.ExportTop)
                {
                    totalCount = input.ExportTop.Value;
                    Console.WriteLine($"customerData Count limit: {totalCount}");
                }
                return totalCount;
            }
        }

        private static void ProcessInputValues(ref ExportDataFilter input)
        {
            if (input.DateFirstAddedFrom != null)
                input.DateFirstAddedFrom = input.DateFirstAddedFrom.Value.Date;

            if (input.DateFirstAddedTo != null)
                input.DateFirstAddedTo = input.DateFirstAddedTo.Value.Date.AddDays(1).AddMilliseconds(-1);

            if (input.DateLastExportedFrom != null)
                input.DateLastExportedFrom = input.DateLastExportedFrom.Value.Date;

            if (input.DateLastExportedTo != null)
                input.DateLastExportedTo = input.DateLastExportedTo.Value.Date.AddDays(1).AddMilliseconds(-1);

            if (input.DateLastOccurredFrom != null)
                input.DateLastOccurredFrom = input.DateLastOccurredFrom.Value.Date;

            if (input.DateLastOccurredTo != null)
                input.DateLastOccurredTo = input.DateLastOccurredTo.Value.Date.AddDays(1).AddMilliseconds(-1);

            input.ExportLimit = input.ExportTop != null ? input.ExportTop.Value : int.MaxValue;

            if (string.IsNullOrEmpty(input.SortBy))
                input.SortBy = "ID";
            if (string.IsNullOrEmpty(input.SortDirection))
                input.SortDirection = "desc";
        }

        private static MySqlCommand GetMySqlCommandStoreProcedureFromFilter(MySqlConnection mysqlConn, string spName,ref ExportDataFilter input)
        {
            var cmd = new MySqlCommand(spName, mysqlConn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@dateFirstAddedFrom", input.DateFirstAddedFrom);
            cmd.Parameters.AddWithValue("@dateFirstAddedTo", input.DateFirstAddedTo);

            cmd.Parameters.AddWithValue("@totalTimesExportedFrom", input.TotalTimesExportedFrom);
            cmd.Parameters.AddWithValue("@totalTimesExportedTo", input.TotalTimesExportedTo);

            cmd.Parameters.AddWithValue("@dateLastExportedFrom", input.DateLastExportedFrom);
            cmd.Parameters.AddWithValue("@dateLastExportedTo", input.DateLastExportedTo);

            cmd.Parameters.AddWithValue("@last3CampaignsUsed", input.Last3CampaignsUsed);

            cmd.Parameters.AddWithValue("@dateLastOccurredFrom", input.DateLastOccurredFrom);
            cmd.Parameters.AddWithValue("@dateLastOccurredTo", input.DateLastOccurredTo);

            cmd.Parameters.AddWithValue("@occurredCategories", input.OccurredCategories);

            cmd.Parameters.AddWithValue("@totalOccurancePointsFrom", input.TotalOccurancePointsFrom);
            cmd.Parameters.AddWithValue("@totalOccurancePointsTo", input.TotalOccurancePointsTo);

            cmd.Parameters.AddWithValue("@resultsCategories", input.ResultsCategories);

            cmd.Parameters.AddWithValue("@totalResultsPointsFrom", input.TotalResultsPointsFrom);
            cmd.Parameters.AddWithValue("@totalResultsPointsTo", input.TotalResultsPointsTo);

            cmd.Parameters.AddWithValue("@totalPointsFrom", input.TotalPointsFrom);
            cmd.Parameters.AddWithValue("@totalPointsTo", input.TotalPointsTo);

            cmd.Parameters.AddWithValue("@exportVsPointsPercentageFrom", input.ExportVsPointsPercentageFrom);
            cmd.Parameters.AddWithValue("@exportVsPointsPercentageTo", input.ExportVsPointsPercentageTo);

            cmd.Parameters.AddWithValue("@exportVsPointsExceptions", input.ExportVsPointsExceptions);

            cmd.Parameters.AddWithValue("@exportVsPointsNumberFrom", input.ExportVsPointsNumberFrom);
            cmd.Parameters.AddWithValue("@exportVsPointsNumberTo", input.ExportVsPointsNumberTo);

            cmd.Parameters.AddWithValue("@sortBy", input.SortBy);
            cmd.Parameters.AddWithValue("@sortDirection", input.SortDirection);

            cmd.Parameters.AddWithValue("@exportOffset", input.ExportOffset);
            cmd.Parameters.AddWithValue("@exportLimit", input.ExportLimit);

            return cmd;
        }

        #endregion private

        public static void AddExportDataService(this WebApplication app, string sqlConnectionStr)
        {
            app.MapPost("data/getCustomerCountBySP", [Authorize]
            async Task<IResult>([FromBody] ExportDataFilter input) =>
            {
                Console.WriteLine($"AssignedCampaignID {input.AssignedCampaignID}");
                ProcessInputValues(ref input);               
                var totalCount = GetTotalCountByFilter(sqlConnectionStr, ref input);
                if (input.AssignedCampaignID != null)
                {
                    using var connection = new MySqlConnection(sqlConnectionStr);
                    if (input.AssignedCampaignID.Value == 0)
                    {
                        var recordCount = connection.QueryFirstOrDefault<int>($"select count(distinct(CustomerMobileNo)) from RecordCustomerExport;");
                        if (recordCount < totalCount) totalCount -= recordCount;
                    }
                    else
                    {
                        var recordCount = connection.QueryFirstOrDefault<int>($"select count(distinct(CustomerMobileNo)) from RecordCustomerExport where CampaignID= {input.AssignedCampaignID.Value} ;");
                        if (recordCount < totalCount) totalCount = recordCount;
                    }  
                }     
                return Results.Ok(new { totalCount });
            });

            app.MapPost("data/getCustomersBySP", [Authorize]
            async Task<IResult>
                (
                [FromServices] IConfiguration config,
                [FromBody] ExportDataFilter input
                ) =>
            {
                var data = GetCustomerListByFilter(sqlConnectionStr,ref input);
                return Results.Ok(data);
            });

            app.MapPost("data/downloadCustomersBySP", [Authorize]
            async Task<IResult>
                (
                [FromServices] IConfiguration config,
                [FromServices] IFileStorageService fileService,
                [FromBody] ExportDataFilter input
                ) =>
            {
                ProcessInputValues(ref input);
                var nowStr = DateTime.Now.ToString("yyyy-MM-dd-hh-ss-mm");
                var folderName = $"{AzureBlobConfig.ExportDataFolder}/{DateTime.Now.ToString("yyyy-MM-dd")}";
                var maxSheetCount = config.GetValue<int>("MAX_SHEET_COUNT");
                var totalRows = input.TotalCount != null ? input.TotalCount.Value : (int)GetTotalCountByFilter(sqlConnectionStr, ref input);
                var packageCount = (totalRows - 1) / maxSheetCount + 1;
                var result = new List<string>();
                using var conn = new MySqlConnection(sqlConnectionStr);
                using var conn2 = new MySqlConnection(sqlConnectionStr);
                conn.Open();
                Console.WriteLine($"AssignedCampaignID: {input.AssignedCampaignID}");
                Console.WriteLine($"IsRemoveTaggedCampaign: {input.IsRemoveTaggedCampaign}");
                var cmd = GetMySqlCommandStoreProcedureFromFilter(conn,input.AssignedCampaignID ==null? StoredProcedureName.GetCustomersByFilter:StoredProcedureName.GetRecordCustomerExport, ref input);
                for (int i = 0; i < packageCount; i++)
                {
                    var itemCount = maxSheetCount;
                    if (i == packageCount - 1)
                        itemCount = totalRows - i * maxSheetCount;

                    cmd.Parameters["@exportOffset"].Value = i * maxSheetCount;
                    cmd.Parameters["@exportLimit"].Value = itemCount;
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using var package = new ExcelPackage();
                    var sheet = package.Workbook.Worksheets.Add($"Page {i + 1}");
                    using var rdr = cmd.ExecuteReader();
                    DataTable dtCustomMobile = dbHelper.executeQueryDataTable(conn2, "SELECT mobile_no FROM custom_mobile WHERE status = 1;", new Dictionary<string, string>());
                //sheet.Cells[1, 1, itemCount, 1].Style.Numberformat.Format = "0";
                    if (dtCustomMobile.Rows.Count > 0)
                    {
                        sheet.Cells[1, 1].Value = "Mobile No";
                        sheet.Cells[1, 2].Value = "Message";
                        for (int x = 0; x < dtCustomMobile.Rows.Count; x++)
                        {
                            sheet.Cells[x+2, 1 ].Value = dtCustomMobile.Rows[x]["mobile_no"];
                            sheet.Cells[x+2, 2 ].Value = "";

                        }
                        sheet.Cells[dtCustomMobile.Rows.Count + 2, 1, itemCount, 1].LoadFromDataReader(rdr, false);
                    }
                    else
                    {
                        sheet.Cells[1, 1].Value = "Mobile No";
                        sheet.Cells[1, 2].Value = "Message";
                        sheet.Cells[2, 1, itemCount, 1].LoadFromDataReader(rdr, false);
                    }
                
                    result.Add(await fileService.SaveAndGetFullUrl(package.GetAsByteArray(), $"{nowStr}-customer-page-{i + 1}.xlsx", folder: folderName));
                }
                if (input.IsRemoveTaggedCampaign && input.AssignedCampaignID !=null)
                {
                    var commandStr = $"delete from RecordCustomerExport where CampaignID = {input.AssignedCampaignID} order by {input.SortBy} {input.SortDirection} limit {input.ExportLimit}";
                    using (MySqlCommand myCmd = new MySqlCommand(commandStr, conn))
                    {
                        myCmd.CommandType = CommandType.Text;
                        myCmd.ExecuteNonQuery();
                    }
                }                
                await conn.CloseAsync();
                return Results.Ok(new { result });
            });

            app.MapPost("data/assignCampaignToCustomers", [Authorize]
            async Task<IResult> (
            [FromServices] IHttpContextAccessor httpContextAccessor,
            [FromServices] IExportDataToQueueService exportDataToQueue,
            [FromServices] IConfiguration config,
            [FromBody] ExportDataFilter input) =>
            {
                if (input.CampaignID is null)
                    throw new Exception("No selected CampaignID");

                var customerList = GetCustomerListByFilter(sqlConnectionStr,ref input);
                var userEmail = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Email);
                var processAssign = new ProcessLastUsedCampaign { 
                    UserEmail = userEmail, 
                    CampaignID = input.CampaignID.Value, 
                    CustomerList = customerList,
                    SignalRConnectionId = input.SignalRConnectionId
                };
                //Console.WriteLine($"userEmail: {processAssign.UserEmail}");
                var userIdSr = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                int.TryParse(userIdSr, out var userId);
                processAssign.ShouldSendEmail = processAssign.CustomerList.Count > config.GetValue<int>("ShouldSendEmailWhenReachLimit");
                Parallel.Invoke(
                    () => { exportDataToQueue.BulkInsertCustomerCampaignToMySQL(sqlConnectionStr, customerList, input.CampaignID.Value, userId); },
                    () => { exportDataToQueue.UpdateLastUsedCampaign(sqlConnectionStr, processAssign); });

                return Results.Ok(new { processAssign.ShouldSendEmail });
            });

            app.MapDelete("data/removeAssignedCampaign/{id:int}", [Authorize]
            async Task<IResult> (int id) =>
            {
                using var connection = new MySqlConnection(sqlConnectionStr);
                connection.Open();
                var commandStr = $"delete from RecordCustomerExport where CampaignID = {id}";
                using (MySqlCommand myCmd = new MySqlCommand(commandStr, connection))
                {
                    myCmd.CommandType = CommandType.Text;
                    myCmd.ExecuteNonQuery();
                }
 
                commandStr = $"update leadmanagementreport set LastUsedCampaignId = null where LastUsedCampaignId = {id}";
                using (MySqlCommand myCmd = new MySqlCommand(commandStr, connection))
                {
                    myCmd.CommandType = CommandType.Text;
                    myCmd.ExecuteNonQuery();
                }

                commandStr = $"update leadmanagementreport set SecondLastUsedCampaignId = null where SecondLastUsedCampaignId = {id}";
                using (MySqlCommand myCmd = new MySqlCommand(commandStr, connection))
                {
                    myCmd.CommandType = CommandType.Text;
                    myCmd.ExecuteNonQuery();
                }

                commandStr = $"update leadmanagementreport set ThirdLastUsedCampaignId = null where ThirdLastUsedCampaignId = {id}";
                using (MySqlCommand myCmd = new MySqlCommand(commandStr, connection))
                {
                    myCmd.CommandType = CommandType.Text;
                    myCmd.ExecuteNonQuery();
                }

                connection.Close();
                return Results.Ok();
            });


            app.MapGet("data/countCustomersOfTaggedCampagign/{campagignId:int}", [Authorize]
            async Task<IResult> (int campagignId) =>
            {
                using var connection = new MySqlConnection(sqlConnectionStr);
                var recordCount = await connection.QueryFirstOrDefaultAsync<int>($"select count(distinct(CustomerMobileNo)) from RecordCustomerExport where CampaignID = {campagignId};");
                return Results.Ok(recordCount);
            });
        }
    }
}