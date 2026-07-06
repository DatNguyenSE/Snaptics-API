    using System.Text;
using Amazon.Budgets;
using API.Mappings;
using API.Middlewares;
using BLL.Interfaces.IServices;
using BLL.Service;
using Hangfire;
using BLL.Configurations;

// using BLL.Interfaces.IServices;
// using BLL.Service;
using DAL.Data;
using DAL.Entities;
using DAL.IRepositories;
using DAL.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Amazon.SimpleNotificationService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// register AutoMapper 
builder.Services.AddAutoMapper(cfg => 
{
    cfg.AddProfile<AutoMapperProfile>(); 
});

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();

builder.Services.AddScoped<ITransactionDetailService, TransactionDetailService>();
builder.Services.AddScoped<IItemInventoryService, ItemInventoryService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IItemDictionaryService, ItemDictionaryService>();
builder.Services.AddScoped<IS3Service, S3Service>();
builder.Services.AddScoped<IAiAssistantService, AiAssistantService>();
builder.Services.AddScoped<IMailService, EmailService>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IAiInsightService, AiInsightService>();

// AI Services: Gemini Vision + Azure Document Intelligence
builder.Services.AddScoped<IAiService, AiService>();
builder.Services.AddHttpClient(); // Required for Gemini REST API calls
builder.Services.AddMemoryCache(); // Required for ItemDictionary in-memory caching


// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Swagger config
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Snaptic API", Version = "v1" });

    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
      In = ParameterLocation.Header,
      Description = "Please, enter the token code in the blank",
      Name = "Authorizaion",
      Type = SecuritySchemeType.Http,
      BearerFormat = "JWT",
      Scheme = "Bearer"
    });
    option.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

// SQL server
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

//Identity(user)
builder.Services.AddIdentityCore<AppUser>(opt =>
{
    opt.Password.RequireNonAlphanumeric = false; //no (@, #, !)
    opt.User.RequireUniqueEmail = true; //Unique Email
})
.AddRoles<IdentityRole>() // Activate the Role feature
.AddEntityFrameworkStores<AppDbContext>() // store user to db via AppDbcontext
.AddDefaultTokenProviders();
//JWT config
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var tokenKey = builder.Configuration["TokenKey"]
            ?? throw new Exception("Token key not found - Program.cs");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true, // Token signature varification 
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey)),  //compare signature to secret-key
            ValidateIssuer = false, // skip issuer
            ValidateAudience = false // skip Audience
        };
    });

builder.Services.AddCors();
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();
builder.Services.AddScoped<IMissingPriceJob, MissingPriceJob>();

builder.Services.AddScoped<IItemReviewJobService, ItemReviewJobService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

builder.Services.Configure<AwsSettings>(builder.Configuration.GetSection("AWS"));
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());

builder.Services.Configure<AwsSnsSettings>(builder.Configuration.GetSection("AwsSns"));
builder.Services.AddScoped<ISnsService, SnsService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionMiddleware>();
app.UseCors(x => x
    .WithOrigins("http://localhost:4200", "https://localhost:4200")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials() 
);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHangfireDashboard(); 
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    
    recurringJobManager.AddOrUpdate<IMissingPriceJob>(
        "remind-missing-price-daily",
        job => job.ScanAndSendNotificationAsync(),
        "0 20 * * *" 
    );

    recurringJobManager.AddOrUpdate<IItemReviewJobService>(
        "remind-item-review-daily",
        job => job.ScanAndSendNotificationAsync(30),
        "0 20 * * *"
    );

    recurringJobManager.AddOrUpdate<INotificationService>(
        "cleanup-old-notifications-daily",
        job => job.CleanUpOldNotificationsAsync(),
        "0 2 * * *" 
    );
}



app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
