using System.Globalization;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Models;
using Nomaro.API.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace Nomaro.API.Services.Implimentation
{
    /// <summary>
    /// Read-only figures for the Salary Dashboard, built from generated salaries
    /// (EmployeeSalaries + EmployeeSalaryDetails) and the salary head set-up (SalaryHeads).
    /// </summary>
    public class SalaryDashboardService : ISalaryDashboardService
    {
        private const string StatusApproved = "APPROVED";
        private const string StatusRejected = "REJECTED";
        private const string StatusNotGenerated = "NOT GENERATED";
        private const int TrendMonths = 12;
        private const decimal ReviewChangePercent = 10m;
        private const int MaxEmployeeChanges = 50;
        private const string NotAssigned = "Not assigned";

        private static readonly CultureInfo IndianCulture = new CultureInfo("en-IN");

        private static readonly (string Code, string Name, string[] Types)[] StatutoryGroups =
        {
            ("PF", "Provident Fund (PF)", new[] { "PF_EE", "VPF", "PF_ER_EPF", "PF_ER_EPS", "EDLI", "PF_ADMIN" }),
            ("ESI", "ESI", new[] { "ESI_EE", "ESI_ER" }),
            ("PT", "Professional Tax", new[] { "PT" }),
            ("LWF", "Labour Welfare Fund", new[] { "LWF_EE", "LWF_ER" }),
            ("TDS", "Income Tax (TDS)", new[] { "TDS" }),
        };

        private static readonly (string Label, decimal From, decimal? To)[] SalaryBands =
        {
            ("Below 15K", 0m, 15000m),
            ("15K - 25K", 15000m, 25000m),
            ("25K - 40K", 25000m, 40000m),
            ("40K - 60K", 40000m, 60000m),
            ("60K - 1L", 60000m, 100000m),
            ("1L - 1.5L", 100000m, 150000m),
            ("1.5L and above", 150000m, null),
        };

        private readonly ApplicationDBContext _dbContext;
        private readonly ILogger<SalaryDashboardService> _logger;

        public SalaryDashboardService(ApplicationDBContext dbContext, ILogger<SalaryDashboardService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<SalaryDashboardFilterDto> GetFilterOptions()
        {
            try
            {
                var monthIds = await _dbContext.EmployeeSalaries.AsNoTracking()
                    .Select(s => s.IdSalaryMonth).Distinct().ToListAsync();

                var months = await _dbContext.SalaryMonths.AsNoTracking()
                    .Where(m => monthIds.Contains(m.IdSalaryMonth))
                    .OrderByDescending(m => m.SalaryMonthDate)
                    .Select(m => new SalaryDashboardMonthOptionDto
                    {
                        IdSalaryMonth = m.IdSalaryMonth,
                        SalaryMonthText = m.SalaryMonthText,
                        SalaryMonthDate = m.SalaryMonthDate
                    })
                    .ToListAsync();

                var offices = await _dbContext.Offices.AsNoTracking()
                    .Where(o => o.IsActive)
                    .OrderBy(o => o.OfficeName)
                    .Select(o => new SalaryDashboardOptionDto { Id = o.IdOffice, Name = o.OfficeName })
                    .ToListAsync();

                var departments = await _dbContext.Departments.AsNoTracking()
                    .OrderBy(d => d.DepartmentName)
                    .Select(d => new SalaryDashboardOptionDto { Id = d.IdDepartment, Name = d.DepartmentName })
                    .ToListAsync();

                return new SalaryDashboardFilterDto { Months = months, Offices = offices, Departments = departments };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Salary Dashboard filters.");
                throw;
            }
        }

        public async Task<SalaryDashboardDto> GetSalaryDashboard(int idSalaryMonth, int? idOffice, int? idDepartment, bool approvedOnly)
        {
            try
            {
                var month = await _dbContext.SalaryMonths.AsNoTracking()
                    .FirstOrDefaultAsync(m => m.IdSalaryMonth == idSalaryMonth);
                if (month == null)
                    throw new InvalidOperationException("Salary month not found.");

                // Selected month and the 11 months before it (oldest first)
                var window = (await _dbContext.SalaryMonths.AsNoTracking()
                        .Where(m => m.SalaryMonthDate <= month.SalaryMonthDate)
                        .OrderByDescending(m => m.SalaryMonthDate)
                        .Take(TrendMonths)
                        .ToListAsync())
                    .OrderBy(m => m.SalaryMonthDate)
                    .ToList();
                var windowIds = window.Select(m => m.IdSalaryMonth).ToList();
                var previousMonth = window.Count > 1 ? window[^2] : null;

                // ── Salary records of the window, with employee details ──
                var salaryQuery =
                    from s in _dbContext.EmployeeSalaries.AsNoTracking()
                    join e in _dbContext.Employees.AsNoTracking() on (int?)s.IdEmployee equals e.IdEmployee
                    where windowIds.Contains(s.IdSalaryMonth)
                    select new
                    {
                        s.IdEmployeeSalary,
                        s.IdEmployee,
                        s.IdSalaryMonth,
                        s.ApprovalStatus,
                        s.TotalEarnings,
                        s.TotalDeductions,
                        s.TaxAmountDeducted,
                        e.EmployeeCode,
                        e.FirstName,
                        e.MiddleName,
                        e.LastName,
                        e.IdDepartment,
                        e.IdDesignation
                    };
                if (idDepartment.HasValue)
                    salaryQuery = salaryQuery.Where(x => x.IdDepartment == idDepartment.Value);

                var salaryRecords = await salaryQuery.ToListAsync();

                // Office for each employee and month, from office postings valid in that month
                var employeeIds = salaryRecords.Select(r => r.IdEmployee).Distinct().ToList();
                var postings = await _dbContext.EmployeeOfficePostings.AsNoTracking()
                    .Where(p => employeeIds.Contains(p.IdEmployee))
                    .Select(p => new { p.IdEmployee, p.IdOffice, p.PostingFromDate, p.PostingToDate, p.IsCurrentPosting })
                    .ToListAsync();
                var postingsByEmployee = postings.GroupBy(p => p.IdEmployee).ToDictionary(g => g.Key, g => g.ToList());
                var monthById = window.ToDictionary(m => m.IdSalaryMonth);

                int? OfficeFor(int idEmployee, int idMonth)
                {
                    if (!postingsByEmployee.TryGetValue(idEmployee, out var list)) return null;
                    var m = monthById[idMonth];
                    var valid = list
                        .Where(p => p.PostingFromDate.Date <= m.SalaryMonthTo.Date &&
                                    (p.PostingToDate == null || p.PostingToDate.Value.Date >= m.SalaryMonthFrom.Date))
                        .OrderByDescending(p => p.PostingFromDate)
                        .FirstOrDefault();
                    return valid?.IdOffice ?? list.FirstOrDefault(p => p.IsCurrentPosting)?.IdOffice;
                }

                // Approval status counts for the selected month (before the approved-only filter)
                var statusCounts = salaryRecords
                    .Where(r => r.IdSalaryMonth == idSalaryMonth &&
                                (!idOffice.HasValue || OfficeFor(r.IdEmployee, r.IdSalaryMonth) == idOffice.Value))
                    .GroupBy(r => string.IsNullOrWhiteSpace(r.ApprovalStatus) ? "Unknown" : r.ApprovalStatus.Trim())
                    .Select(g => new SalaryDashboardCountDto { Name = g.Key, Count = g.Count() })
                    .OrderByDescending(c => c.Count)
                    .ToList();

                bool Included(string? status)
                {
                    var s = (status ?? string.Empty).Trim().ToUpperInvariant();
                    return approvedOnly ? s == StatusApproved : s != StatusRejected && s != StatusNotGenerated && s != string.Empty;
                }

                var rows = salaryRecords
                    .Where(r => Included(r.ApprovalStatus))
                    .Select(r => new SalaryRow
                    {
                        IdEmployeeSalary = r.IdEmployeeSalary,
                        IdEmployee = r.IdEmployee,
                        IdSalaryMonth = r.IdSalaryMonth,
                        EmployeeCode = r.EmployeeCode,
                        EmployeeName = string.Join(" ", new[] { r.FirstName, r.MiddleName, r.LastName }
                            .Where(n => !string.IsNullOrWhiteSpace(n))),
                        IdDepartment = r.IdDepartment,
                        IdDesignation = r.IdDesignation,
                        IdOffice = OfficeFor(r.IdEmployee, r.IdSalaryMonth),
                        Gross = r.TotalEarnings,
                        Deductions = r.TotalDeductions,
                        TaxAmountDeducted = r.TaxAmountDeducted
                    })
                    .ToList();
                if (idOffice.HasValue)
                    rows = rows.Where(r => r.IdOffice == idOffice.Value).ToList();

                // ── Salary head amounts of those records ──
                var rowIds = rows.Select(r => r.IdEmployeeSalary).ToHashSet();
                var detailRecords = await (
                        from d in _dbContext.EmployeeSalaryDetails.AsNoTracking()
                        join s in _dbContext.EmployeeSalaries.AsNoTracking() on d.IdEmployeeSalary equals (int?)s.IdEmployeeSalary
                        join h in _dbContext.SalaryHeads.AsNoTracking() on d.IdSalaryHead equals (int?)h.IdSalaryHead into heads
                        from h in heads.DefaultIfEmpty()
                        where windowIds.Contains(s.IdSalaryMonth)
                        select new
                        {
                            s.IdEmployeeSalary,
                            d.IdSalaryHead,
                            DetailHeadName = d.SalaryHeadName,
                            DetailHeadType = d.SalaryHeadType,
                            Amount = d.Amount ?? 0m,
                            HeadName = h != null ? h.SalaryHeadName : null,
                            HeadType = h != null ? h.HeadType : null,
                            StatutoryType = h != null ? h.StatutoryType : null
                        })
                    .ToListAsync();

                var rowById = rows.ToDictionary(r => r.IdEmployeeSalary);
                foreach (var d in detailRecords)
                {
                    if (!rowIds.Contains(d.IdEmployeeSalary)) continue;
                    var row = rowById[d.IdEmployeeSalary];
                    var type = NormaliseHeadType(d.HeadType ?? d.DetailHeadType);
                    var statutory = (d.StatutoryType ?? string.Empty).Trim().ToUpperInvariant();

                    row.Heads.Add(new HeadAmount
                    {
                        IdSalaryHead = d.IdSalaryHead,
                        Name = d.HeadName ?? d.DetailHeadName ?? "Unnamed head",
                        Type = type,
                        StatutoryType = statutory,
                        Amount = d.Amount
                    });

                    if (type == HeadTypeEmployer) row.EmployerContribution += d.Amount;
                }

                foreach (var row in rows)
                    CalculateStatutory(row);

                // ── Build the dashboard ──
                var current = rows.Where(r => r.IdSalaryMonth == idSalaryMonth).ToList();
                var previous = previousMonth == null
                    ? new List<SalaryRow>()
                    : rows.Where(r => r.IdSalaryMonth == previousMonth.IdSalaryMonth).ToList();

                var departmentNames = await _dbContext.Departments.AsNoTracking()
                    .ToDictionaryAsync(d => d.IdDepartment, d => d.DepartmentName);
                var designationNames = await _dbContext.Designations.AsNoTracking()
                    .ToDictionaryAsync(d => d.IdDesignation, d => d.DesignationName);
                var officeNames = await _dbContext.Offices.AsNoTracking()
                    .ToDictionaryAsync(o => o.IdOffice, o => o.OfficeName);

                string DepartmentName(int? id) => id.HasValue && departmentNames.TryGetValue(id.Value, out var n) ? n : NotAssigned;
                string DesignationName(int? id) => id.HasValue && designationNames.TryGetValue(id.Value, out var n) ? n : NotAssigned;
                string OfficeName(int? id) => id.HasValue && officeNames.TryGetValue(id.Value, out var n) ? n : NotAssigned;

                var currentIds = current.Select(r => r.IdEmployee).ToHashSet();
                var previousIds = previous.Select(r => r.IdEmployee).ToHashSet();

                var dashboard = new SalaryDashboardDto
                {
                    IdSalaryMonth = month.IdSalaryMonth,
                    SalaryMonthText = month.SalaryMonthText,
                    PreviousMonthText = previousMonth?.SalaryMonthText,
                    ApprovedOnly = approvedOnly,
                    Summary = Summarise(current),
                    PreviousSummary = previousMonth == null ? null : Summarise(previous),
                    StatusCounts = statusCounts,
                };
                if (previousMonth != null)
                {
                    dashboard.Summary.JoinedPayroll = currentIds.Count(id => !previousIds.Contains(id));
                    dashboard.Summary.LeftPayroll = previousIds.Count(id => !currentIds.Contains(id));
                }

                dashboard.Trend = window.Select(m =>
                {
                    var monthRows = rows.Where(r => r.IdSalaryMonth == m.IdSalaryMonth).ToList();
                    var s = Summarise(monthRows);
                    return new SalaryDashboardMonthDto
                    {
                        IdSalaryMonth = m.IdSalaryMonth,
                        SalaryMonthText = m.SalaryMonthText,
                        EmployeesPaid = s.EmployeesPaid,
                        GrossEarnings = s.GrossEarnings,
                        TotalDeductions = s.TotalDeductions,
                        NetPay = s.NetPay,
                        EmployerContribution = s.EmployerContribution,
                        EmployerCost = s.EmployerCost,
                        PF = monthRows.Sum(r => r.StatutoryTotal("PF")),
                        ESI = monthRows.Sum(r => r.StatutoryTotal("ESI")),
                        PT = monthRows.Sum(r => r.StatutoryTotal("PT")),
                        LWF = monthRows.Sum(r => r.StatutoryTotal("LWF")),
                        TDS = monthRows.Sum(r => r.StatutoryTotal("TDS")),
                    };
                }).ToList();

                dashboard.ByDepartment = BuildGroups(rows, current, previous, window, r => r.IdDepartment, DepartmentName);
                dashboard.ByOffice = BuildGroups(rows, current, previous, window, r => r.IdOffice, OfficeName);

                dashboard.DepartmentOffice = current
                    .GroupBy(r => new { r.IdDepartment, r.IdOffice })
                    .Select(g => new SalaryDashboardCellDto
                    {
                        IdDepartment = g.Key.IdDepartment,
                        DepartmentName = DepartmentName(g.Key.IdDepartment),
                        IdOffice = g.Key.IdOffice,
                        OfficeName = OfficeName(g.Key.IdOffice),
                        Employees = g.Count(),
                        EmployerCost = g.Sum(r => r.EmployerCost)
                    })
                    .OrderBy(c => c.DepartmentName).ThenBy(c => c.OfficeName)
                    .ToList();

                dashboard.Statutory = StatutoryGroups.Select(g => new SalaryDashboardStatutoryDto
                {
                    Code = g.Code,
                    Name = g.Name,
                    EmployeeShare = current.Sum(r => r.StatutoryEmployee(g.Code)),
                    EmployerShare = current.Sum(r => r.StatutoryEmployer(g.Code)),
                    Total = current.Sum(r => r.StatutoryTotal(g.Code)),
                    PreviousTotal = previous.Sum(r => r.StatutoryTotal(g.Code)),
                    Employees = current.Count(r => r.StatutoryTotal(g.Code) != 0),
                    DueDate = DueDate(g.Code, month.SalaryMonthDate)
                }).ToList();

                if (previousMonth != null)
                {
                    dashboard.GrossBridge = BuildGrossBridge(current, previous, month.SalaryMonthText, previousMonth.SalaryMonthText);
                    dashboard.HeadChanges = BuildHeadChanges(current, previous);
                    dashboard.EmployeeChanges = BuildEmployeeChanges(current, previous, DepartmentName, DesignationName, OfficeName);
                }

                dashboard.SalaryBands = SalaryBands.Select(b =>
                {
                    var inBand = current.Where(r => r.Gross >= b.From && (b.To == null || r.Gross < b.To.Value)).ToList();
                    return new SalaryDashboardBandDto { Label = b.Label, Employees = inBand.Count, GrossEarnings = inBand.Sum(r => r.Gross) };
                }).ToList();

                dashboard.DesignationRanges = current
                    .GroupBy(r => r.IdDesignation)
                    .Select(g => new SalaryDashboardRangeDto
                    {
                        Name = DesignationName(g.Key),
                        Employees = g.Count(),
                        Minimum = g.Min(r => r.Gross),
                        Average = Math.Round(g.Average(r => r.Gross), 2),
                        Maximum = g.Max(r => r.Gross)
                    })
                    .OrderByDescending(r => r.Average)
                    .ToList();

                return dashboard;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building Salary Dashboard for month {IdSalaryMonth}", idSalaryMonth);
                throw;
            }
        }

        // ───────────── helpers ─────────────

        private const string HeadTypeEarning = "EARNING";
        private const string HeadTypeDeduction = "DEDUCTION";
        private const string HeadTypeEmployer = "EMPLOYER_CONTRIBUTION";
        private const string HeadTypeReimbursement = "REIMBURSEMENT";

        /// <summary>SalaryHeads uses EARNINGS, salary details use EARNING - treat both the same.</summary>
        private static string NormaliseHeadType(string? type)
        {
            var t = (type ?? string.Empty).Trim().ToUpperInvariant();
            if (t.StartsWith("EARN")) return HeadTypeEarning;
            if (t.StartsWith("DEDUCT")) return HeadTypeDeduction;
            if (t.StartsWith("EMPLOYER")) return HeadTypeEmployer;
            if (t.StartsWith("REIMB")) return HeadTypeReimbursement;
            return t;
        }

        private static void CalculateStatutory(SalaryRow row)
        {
            foreach (var group in StatutoryGroups)
            {
                var heads = row.Heads.Where(h => group.Types.Contains(h.StatutoryType)).ToList();
                var employer = heads.Where(h => h.Type == HeadTypeEmployer).Sum(h => h.Amount);
                var employee = heads.Where(h => h.Type != HeadTypeEmployer).Sum(h => h.Amount);

                // TDS may be stored only on the salary record, without a TDS salary head
                if (group.Code == "TDS" && heads.Count == 0)
                    employee = row.TaxAmountDeducted;

                row.Statutory[group.Code] = (employee, employer);
            }
        }

        private static SalaryDashboardSummaryDto Summarise(List<SalaryRow> rows)
        {
            var employees = rows.Select(r => r.IdEmployee).Distinct().Count();
            var gross = rows.Sum(r => r.Gross);
            var deductions = rows.Sum(r => r.Deductions);
            var employer = rows.Sum(r => r.EmployerContribution);
            var cost = gross + employer;
            return new SalaryDashboardSummaryDto
            {
                EmployeesPaid = employees,
                GrossEarnings = gross,
                TotalDeductions = deductions,
                NetPay = gross - deductions,
                EmployerContribution = employer,
                EmployerCost = cost,
                AverageCostPerEmployee = employees == 0 ? 0 : Math.Round(cost / employees, 2),
                StatutoryTotal = rows.Sum(r => StatutoryGroups.Sum(g => r.StatutoryTotal(g.Code))),
                TdsDeducted = rows.Sum(r => r.StatutoryTotal("TDS"))
            };
        }

        private static List<SalaryDashboardGroupDto> BuildGroups(
            List<SalaryRow> all, List<SalaryRow> current, List<SalaryRow> previous, List<SalaryMonths> window,
            Func<SalaryRow, int?> key, Func<int?, string> name)
        {
            var keys = current.Select(key).Concat(previous.Select(key)).Distinct().ToList();
            return keys.Select(k => new SalaryDashboardGroupDto
            {
                Id = k,
                Name = name(k),
                Employees = current.Count(r => key(r) == k),
                GrossEarnings = current.Where(r => key(r) == k).Sum(r => r.Gross),
                NetPay = current.Where(r => key(r) == k).Sum(r => r.Gross - r.Deductions),
                EmployerCost = current.Where(r => key(r) == k).Sum(r => r.EmployerCost),
                PreviousEmployees = previous.Count(r => key(r) == k),
                PreviousEmployerCost = previous.Where(r => key(r) == k).Sum(r => r.EmployerCost),
                EmployerCostTrend = window
                    .Select(m => all.Where(r => r.IdSalaryMonth == m.IdSalaryMonth && key(r) == k).Sum(r => r.EmployerCost))
                    .ToList()
            })
            .OrderByDescending(g => g.EmployerCost)
            .ToList();
        }

        /// <summary>Previous gross → joiners → leavers → change per earning head for continuing employees → current gross.</summary>
        private static List<SalaryDashboardBridgeStepDto> BuildGrossBridge(
            List<SalaryRow> current, List<SalaryRow> previous, string currentText, string previousText)
        {
            var prevByEmp = previous.GroupBy(r => r.IdEmployee).ToDictionary(g => g.Key, g => g.ToList());
            var curByEmp = current.GroupBy(r => r.IdEmployee).ToDictionary(g => g.Key, g => g.ToList());

            var previousGross = previous.Sum(r => r.Gross);
            var currentGross = current.Sum(r => r.Gross);
            var joiners = current.Where(r => !prevByEmp.ContainsKey(r.IdEmployee)).Sum(r => r.Gross);
            var leavers = -previous.Where(r => !curByEmp.ContainsKey(r.IdEmployee)).Sum(r => r.Gross);

            var continuingCurrent = current.Where(r => prevByEmp.ContainsKey(r.IdEmployee)).ToList();
            var continuingPrevious = previous.Where(r => curByEmp.ContainsKey(r.IdEmployee)).ToList();

            var headChanges = continuingCurrent.SelectMany(r => r.Heads).Where(h => h.Type == HeadTypeEarning)
                .Select(h => new { h.Name, Amount = h.Amount })
                .Concat(continuingPrevious.SelectMany(r => r.Heads).Where(h => h.Type == HeadTypeEarning)
                    .Select(h => new { h.Name, Amount = -h.Amount }))
                .GroupBy(h => h.Name)
                .Select(g => new { Name = g.Key, Change = g.Sum(x => x.Amount) })
                .Where(x => x.Change != 0)
                .OrderByDescending(x => Math.Abs(x.Change))
                .ToList();

            var steps = new List<SalaryDashboardBridgeStepDto>
            {
                new() { Label = $"{previousText} gross", Amount = previousGross, IsTotal = true },
                new() { Label = "Joined payroll", Amount = joiners },
                new() { Label = "Left payroll", Amount = leavers },
            };
            steps.AddRange(headChanges.Take(4).Select(h => new SalaryDashboardBridgeStepDto { Label = h.Name, Amount = h.Change }));

            var other = headChanges.Skip(4).Sum(h => h.Change);
            // Anything not explained by earning heads (e.g. totals stored differently) is shown so the bridge always adds up
            var explained = previousGross + joiners + leavers + headChanges.Sum(h => h.Change);
            other += currentGross - explained;
            if (Math.Abs(other) >= 1)
                steps.Add(new SalaryDashboardBridgeStepDto { Label = "Other changes", Amount = other });

            steps.Add(new SalaryDashboardBridgeStepDto { Label = $"{currentText} gross", Amount = currentGross, IsTotal = true });
            return steps;
        }

        private static List<SalaryDashboardHeadDto> BuildHeadChanges(List<SalaryRow> current, List<SalaryRow> previous)
        {
            var amounts = current.SelectMany(r => r.Heads).Select(h => new { h.IdSalaryHead, h.Name, h.Type, Cur = h.Amount, Prev = 0m })
                .Concat(previous.SelectMany(r => r.Heads).Select(h => new { h.IdSalaryHead, h.Name, h.Type, Cur = 0m, Prev = h.Amount }));

            return amounts
                .GroupBy(a => new { a.Name, a.Type })
                .Select(g => new SalaryDashboardHeadDto
                {
                    IdSalaryHead = g.Select(x => x.IdSalaryHead).FirstOrDefault(x => x.HasValue),
                    SalaryHeadName = g.Key.Name,
                    HeadType = g.Key.Type,
                    Amount = g.Sum(x => x.Cur),
                    PreviousAmount = g.Sum(x => x.Prev),
                    Change = g.Sum(x => x.Cur) - g.Sum(x => x.Prev)
                })
                .OrderByDescending(h => Math.Abs(h.Change))
                .ThenBy(h => h.SalaryHeadName)
                .ToList();
        }

        private static List<SalaryDashboardEmployeeChangeDto> BuildEmployeeChanges(
            List<SalaryRow> current, List<SalaryRow> previous,
            Func<int?, string> departmentName, Func<int?, string> designationName, Func<int?, string> officeName)
        {
            var prevByEmp = previous.GroupBy(r => r.IdEmployee).ToDictionary(g => g.Key, g => g.First());
            var result = new List<SalaryDashboardEmployeeChangeDto>();

            foreach (var cur in current.GroupBy(r => r.IdEmployee).Select(g => g.First()))
            {
                if (!prevByEmp.TryGetValue(cur.IdEmployee, out var prev)) continue;
                var prevNet = prev.Gross - prev.Deductions;
                var curNet = cur.Gross - cur.Deductions;
                if (prevNet == 0) continue;

                var changePercent = Math.Round((curNet - prevNet) / Math.Abs(prevNet) * 100, 1);
                if (Math.Abs(changePercent) < ReviewChangePercent) continue;

                // Salary heads that moved the most for this employee
                var reasons = cur.Heads.Select(h => new { h.Name, h.Type, Amount = h.Amount })
                    .Concat(prev.Heads.Select(h => new { h.Name, h.Type, Amount = -h.Amount }))
                    .Where(h => h.Type == HeadTypeEarning || h.Type == HeadTypeDeduction)
                    .GroupBy(h => new { h.Name, h.Type })
                    .Select(g => new { g.Key.Name, g.Key.Type, Change = g.Sum(x => x.Amount) })
                    .Where(x => x.Change != 0)
                    .OrderByDescending(x => Math.Abs(x.Change))
                    .Take(3)
                    .Select(x => $"{x.Name} {(x.Change > 0 ? "+" : "-")}{Math.Abs(x.Change).ToString("N0", IndianCulture)}");

                result.Add(new SalaryDashboardEmployeeChangeDto
                {
                    IdEmployee = cur.IdEmployee,
                    EmployeeCode = cur.EmployeeCode,
                    EmployeeName = cur.EmployeeName,
                    DepartmentName = departmentName(cur.IdDepartment),
                    DesignationName = designationName(cur.IdDesignation),
                    OfficeName = officeName(cur.IdOffice),
                    PreviousNetPay = prevNet,
                    NetPay = curNet,
                    ChangePercent = changePercent,
                    Reasons = string.Join("; ", reasons)
                });
            }

            return result
                .OrderByDescending(r => Math.Abs(r.ChangePercent))
                .Take(MaxEmployeeChanges)
                .ToList();
        }

        /// <summary>Usual due dates: TDS by the 7th (March salary: 30 April), PF and ESI by the 15th of the next month.</summary>
        private static DateTime? DueDate(string code, DateTime salaryMonthDate)
        {
            var nextMonth = new DateTime(salaryMonthDate.Year, salaryMonthDate.Month, 1).AddMonths(1);
            return code switch
            {
                "TDS" => salaryMonthDate.Month == 3 ? new DateTime(salaryMonthDate.Year, 4, 30) : nextMonth.AddDays(6),
                "PF" or "ESI" => nextMonth.AddDays(14),
                _ => null
            };
        }

        private sealed class HeadAmount
        {
            public int? IdSalaryHead { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string StatutoryType { get; set; } = string.Empty;
            public decimal Amount { get; set; }
        }

        private sealed class SalaryRow
        {
            public int IdEmployeeSalary { get; set; }
            public int IdEmployee { get; set; }
            public int IdSalaryMonth { get; set; }
            public string? EmployeeCode { get; set; }
            public string EmployeeName { get; set; } = string.Empty;
            public int? IdDepartment { get; set; }
            public int? IdDesignation { get; set; }
            public int? IdOffice { get; set; }
            public decimal Gross { get; set; }
            public decimal Deductions { get; set; }
            public decimal TaxAmountDeducted { get; set; }
            public decimal EmployerContribution { get; set; }
            public decimal EmployerCost => Gross + EmployerContribution;
            public List<HeadAmount> Heads { get; } = new();
            public Dictionary<string, (decimal Employee, decimal Employer)> Statutory { get; } = new();

            public decimal StatutoryEmployee(string code) => Statutory.TryGetValue(code, out var v) ? v.Employee : 0;
            public decimal StatutoryEmployer(string code) => Statutory.TryGetValue(code, out var v) ? v.Employer : 0;
            public decimal StatutoryTotal(string code) => StatutoryEmployee(code) + StatutoryEmployer(code);
        }
    }
}
