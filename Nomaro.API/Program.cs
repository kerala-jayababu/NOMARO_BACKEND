using Asp.Versioning;
using FluentValidation;
using FluentValidation.AspNetCore;
using Nomaro.API.Database;
using Nomaro.API.Services.Implementations.TimeAndAttendance;
using Nomaro.API.Helpers;
using Nomaro.API.Services.Implementation;
using Nomaro.API.Services.Implimentation;
using Nomaro.API.Services.Implimentation.Time___Attendance;
using Nomaro.API.Services.Implimentation.Time___Attendance.Shift;
using Nomaro.API.Services.Interface;
using Nomaro.API.Services.Interface.Shift;
using Nomaro.API.Services.Interface.Time___Attendance;
using Nomaro.API.Services.Interface.Time___Attendance.Shift;
using Nomaro.API.Validators.MasterData;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Security.Claims;
using System.Text;
using YourNamespace.Services.Implementation;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddFluentValidation(fv =>
{
    fv.RegisterValidatorsFromAssemblyContaining<Program>();
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("FixedWindowPolicy", policy =>
    {
        policy.PermitLimit = 10;
        policy.Window = TimeSpan.FromMinutes(1);
    });
});

builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.ReportApiVersions = true;
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"));
}).AddMvc().AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});

// Add EF Core
builder.Services.AddDbContext<ApplicationDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbContext")));

// CORS setup
var allowedOrigins = builder.Configuration.GetSection("AllowedCorsOrigins").Get<string[]>();
builder.Services.AddCors(options =>
{   
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowSpecificOrigins", policy =>
//    {
//        policy.WithOrigins(allowedOrigins)
//              .AllowAnyHeader()
//              .AllowAnyMethod();
//    });
//});

// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JwtSettings:ValidAudience"],
        ValidIssuer = builder.Configuration["JwtSettings:ValidIssuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"])),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Block inactive users
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDBContext>();
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

            var userEmail = context.Principal.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userEmail))
            {
                context.Fail("Token missing user email.");
                return;
            }

            var user = await dbContext.Employees.FirstOrDefaultAsync(e => e.EmailID == userEmail);
            if (user == null || user.CurrentStatus != "Working")
            {
                logger.LogWarning($"Access denied. User '{userEmail}' is inactive.");
                context.Fail("User is inactive.");
            }
        }
    };
});

// Swagger with JWT support
builder.Services.AddSwaggerGen(swagger =>
{
    swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description =
            "JWT Authorization header using the Bearer scheme.\r\n\r\nEnter 'Bearer' [space] and then your token.\r\n\r\nExample: \"Bearer 12345abcdef\""
    });

    swagger.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Add your services here (same as before)
builder.Services.AddScoped<IBudgetCodeServices, BudgetCodeServices>();
builder.Services.AddScoped<IDesignationServices, DesignationServices>();
builder.Services.AddScoped<IDepartmentServices, DepartmentServices>();
builder.Services.AddScoped<ISalaryHeadServices, SalaryHeadServices>();
builder.Services.AddScoped<IOptionService, OptionService>();
builder.Services.AddScoped<IEmployeeServices, EmployeeServices>();
builder.Services.AddScoped<IRoleBasedScreenService, RoleBasedScreenService>();
builder.Services.AddScoped<ISystemParameterService, SystemParameterService>();
builder.Services.AddScoped<INotificationConfigService, NotificationConfigService>();
builder.Services.AddScoped<IVacationModeService, VacationModeService>();
builder.Services.AddScoped<ITaxSlabService, TaxSlabService>();
builder.Services.AddScoped<ICurrencyConversionService, CurrencyConversionService>();
builder.Services.AddScoped<ITaxYearConfigService, TaxYearConfigService>();
builder.Services.AddScoped<ISalaryTemplateService, SalaryTemplateService>();
builder.Services.AddScoped<IOvertimeTransactionService, OvertimeTransactionService>();
builder.Services.AddScoped<ISalaryTemplateDetailsService, SalaryTemplateDetailsService>();
builder.Services.AddScoped<ISalaryAdjustmentService, SalaryAdjustmentService>();
builder.Services.AddScoped<IScheduledSalaryDeductionService, ScheduledSalaryDeductionService>();
builder.Services.AddScoped<IMaternityLeaveSalaryService, MaternityLeaveSalaryService>();
builder.Services.AddScoped<IRentFreeQuarterService, RentFreeQuarterService>();
builder.Services.AddScoped<IEmployeeSalaryConfigService, EmployeeSalaryConfigService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IApprovalWorkflowService, ApprovalWorkflowService>();
builder.Services.AddScoped<ISalaryGenerationService, SalaryGenerationService>();
builder.Services.AddScoped<IBankServices, BankServices>();
builder.Services.AddScoped<ILeavePassageService, LeavePassageService>();
builder.Services.AddScoped<IReportServices, ReportServices>();
builder.Services.AddScoped<IBambooServices, BambooServices>();
builder.Services.AddScoped<IHolidayServices, HolidayServices>();
builder.Services.AddScoped<IShiftService, ShiftService>();
builder.Services.AddScoped<IShiftScheduleService, ShiftScheduleService>();
builder.Services.AddScoped<IShiftEmployeeService, ShiftEmployeeService>();
builder.Services.AddScoped<IShiftAssignmentService, ShiftAssignmentService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IAttendanceDashboardService, AttendanceDashboardService>();
builder.Services.AddScoped<ITaxReportService, TaxReportService>();
builder.Services.AddScoped<IAssetServices, AssetServices>();
builder.Services.AddScoped<ILeaveManaementServices, LeaveManaementServices>();
builder.Services.AddScoped<IEmployeeOffBoarding, EmployeeOffBoardingService>();
builder.Services.AddScoped<IOfficeManagementService, OfficeManagementService>();
builder.Services.AddScoped<IEmployeeStatutoryDetailsService, EmployeeStatutoryDetailsService>();
builder.Services.AddScoped<ISalaryDashboardService, SalaryDashboardService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<SendSMSOTP>();
builder.Services.AddHttpContextAccessor();           // Required for IP/Claims
builder.Services.AddScoped<IAuditService, AuditService>();
var app = builder.Build();

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseHttpsRedirection();
//app.UseCors("AllowSpecificOrigins");
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
//app.UseMiddleware<UserActivityMiddleware>();
app.UseRateLimiter();

app.MapControllers();

app.Run();

