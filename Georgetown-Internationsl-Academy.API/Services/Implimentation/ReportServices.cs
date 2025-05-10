using AutoMapper;
using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Implementation;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using iText.StyledXmlParser.Jsoup.Nodes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;
using static iText.IO.Image.Jpeg2000ImageData;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class ReportServices : IReportServices
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<BankServices> _logger;
        public ReportServices(ApplicationDBContext dbContext, IMapper mapper, ILogger<BankServices> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }
        public async Task<IEnumerable<ReportsMasterDto>> GetReportMasters()
        {
            try
            {
                var reports = await (from report in _dbContext.ReportsMaster
                                      orderby report.OrderNumber
                                      select new ReportsMasterDto
                                      {
                                          idReport = report.idReport,
                                          ReportName = report.ReportName,
                                          OrderNumber = report.OrderNumber,
                                          idParentReport = report.idParentReport,
                                          StoredProcName = report.StoredProcName,
                                          ReportTitle = report.ReportTitle,
                                          PrintOrientation = report.PrintOrientation,
                                          RowsInaPage = report.RowsInaPage,
                                          RowHeight = report.RowHeight,
                                          RemoveColumnIfNoData = report.RemoveColumnIfNoData,
                                          IncludeSLNO = report.IncludeSLNO,
                                          ViewableAdminOnly = report.ViewableAdminOnly,
                                          idPermissionEmployeesList = report.idPermissionEmployeesList,
                                          Enabled = report.Enabled,
                                          MergeColumnDetails = report.MergeColumnDetails,
                                          HeaderRequired = report.HeaderRequired,
                                          PDFViewable = report.PDFViewable
                                      }).ToListAsync();

                return reports;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching ReportsMasters");
                throw;
            }
        }

        public async Task<IEnumerable<ReportConditionsDto>> GetReportConditionById(int id)
        {

            var reportConditions = await (from report in _dbContext.ReportConditions
                                          where report.IdReport == id
                                 orderby report.OrderNumber
                                 select new ReportConditionsDto
                                 {
                                     IdReportCondition = report.IdReportCondition,
                                     IdReport = report.IdReport,
                                     ConditionName = report.ConditionName,
                                     SPParameterName = report.SPParameterName,
                                     ControlType = report.ControlType,
                                     DataType = report.DataType,
                                     MandatoryFlag = report.MandatoryFlag,
                                     TableName = report.TableName,
                                     ValueColumn = report.ValueColumn,
                                     DisplayColumn = report.DisplayColumn,
                                     ValidValues = report.ValidValues,
                                     DefaultValue = report.DefaultValue,
                                     WhereCondition = report.WhereCondition,
                                     OrderNumber = report.OrderNumber,
                                     DefaultTime = report.DefaultTime
                                 }).ToListAsync();

            return reportConditions;
        }

        public async Task<IEnumerable<ReportColumnsDto>> GetReportColumnsById(int IdReport)
        {

            var reportColumns = await (from report in _dbContext.ReportColumns
                                          where report.IdReport == IdReport
                                            orderby report.IdReportCondition
                                          select new ReportColumnsDto
                                          {
                                              IdReportCondition = report.IdReportCondition,
                                              IdReport =  report.IdReport,
                                              ColumnName = report.ColumnName,
                                              DataType=report.DataType,
                                              Alignment = report.Alignment,
                                              WidthInPixels = report.WidthInPixels,
                                              TotalRequired=    report.TotalRequired                                              
                                          }).ToListAsync();

            return reportColumns;
        }

        public async Task<List<Dictionary<string, object>>> ExecuteStoredProcedureAsync(StoredProcedureDto request)
        {
            var result = new List<Dictionary<string, object>>();

            using var command = _dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = request.StoredProcedureName;
            command.CommandType = CommandType.StoredProcedure;

            foreach (var param in request.Parameters)
            {
                var value = ConvertJsonElement(param.Value);
                var sqlParam = new SqlParameter(param.Key, value ?? DBNull.Value);
                command.Parameters.Add(sqlParam);
            }
            await _dbContext.Database.OpenConnectionAsync();

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                }
                result.Add(row);
            }

            await _dbContext.Database.CloseConnectionAsync();

            return result;
        }

        // Helper: Converts JsonElement to native .NET type
        private object ConvertJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    // Try DateTime parse
                    if (DateTime.TryParse(element.GetString(), out var dateVal))
                        return dateVal;
                    return element.GetString();

                case JsonValueKind.Number:
                    if (element.TryGetInt32(out var intVal))
                        return intVal;
                    if (element.TryGetDecimal(out var decVal))
                        return decVal;
                    return element.GetDouble();

                case JsonValueKind.True:
                case JsonValueKind.False:
                    return element.GetBoolean();

                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return DBNull.Value;

                default:
                    return element.GetRawText(); // Fallback for object/array
            }
        }

        public async Task<List<dynamic>> GetReportsTableValue(string tableName, string valueColumn, string displayColumn)
        {
            try
            {
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    var query = $@"
                SELECT [{valueColumn}] AS valueColumn, [{displayColumn}] AS displayColumn 
                FROM [{tableName}]
                ORDER BY [{valueColumn}]
            ";

                    if (connection.State != System.Data.ConnectionState.Open)
                        await connection.OpenAsync();

                    var result = await connection.QueryAsync(query);
                    return result.ToList();
                }
            }
            catch
            {
                return new List<dynamic>(); // return empty list on error
            }
        }

    }
}
