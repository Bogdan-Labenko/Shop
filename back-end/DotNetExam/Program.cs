using DotNetExam;
using DotNetExam.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter JWT with Bearer into field",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
//Token service
builder.Services.AddTransient<ITokenService, TokenService>();
builder.Services.AddTransient<CookiesService>();
//Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sqlServerOptionsAction: sqlOption =>
    {
        sqlOption.EnableRetryOnFailure();
    }));
//Authentication and authorization
builder.Services.AddAuthorization();
var conf = builder.Configuration.GetSection("JWT");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            //���������, ����� �� �������������� �������� ��� ��������� ������
            ValidateIssuer = true,
            //������, �������������� ��������
            ValidIssuer = conf.GetSection("Issuer").Value,
            //����� �� �������������� ����������� ������
            ValidateAudience = true,
            //��������� ����������� ������
            ValidAudience = conf.GetSection("Audience").Value,
            //����� �� �������������� ����� �������������
            ValidateLifetime = true,
            //��������� ����� ������������
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(conf?.GetSection("SecretKey").Value ?? throw new Exception("Secret key not found!"))),
            //��������� ����� ������������
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(builder =>
	{
		// ��������� ������� � ����� ����������
		builder.WithOrigins("http://localhost:3000")
					   .AllowCredentials() // ��������� �������� ������� ������ (����, ��������� �������������� � �. �.)
					   .AllowAnyHeader()
					   .AllowAnyMethod();
	});
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
