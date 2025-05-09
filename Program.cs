using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AuthService.Data;
using AuthService.Models;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// EF Core & PostgreSQL
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(config.GetConnectionString("DefaultConnection")));

// JWT 설정
var jwtSettings = config.GetSection("JwtSettings");
string secretKey = jwtSettings["Key"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();

// 회원가입
app.MapPost("/api/auth/signup", async (SignupRequest req, AppDbContext db) =>
{
    if (await db.Users.AnyAsync(u => u.Username == req.Username || u.Email == req.Email))
        return Results.BadRequest("Username or email already exists.");

    var user = new User
    {
        Username = req.Username,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
        Name = req.Name,
        Position = req.Position,
        Department = req.Department,
        Email = req.Email
    };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Ok("Signup successful.");
});

// 로그인
app.MapPost("/api/auth/login", async (LoginRequest req, AppDbContext db) =>
{
    var user = await db.Users.SingleOrDefaultAsync(u => u.Username == req.Username);
    if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
        return Results.Unauthorized();

    var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
    var key = Encoding.UTF8.GetBytes(secretKey);
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim("sub", user.UserId.ToString()),
            new System.Security.Claims.Claim("name", user.Name)
        }),
        Expires = DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpiryMinutes"]!)),
        Issuer = jwtSettings["Issuer"],
        Audience = jwtSettings["Audience"],
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };
    var token = tokenHandler.CreateToken(tokenDescriptor);
    var jwt = tokenHandler.WriteToken(token);
    // 추가
    return Results.Ok(new LoginResponse
    {
        Token = jwt,
        Expiry = tokenDescriptor.Expires!.Value,
        Name = user.Name,
        Department = user.Department ?? "",
        Position = user.Position ?? ""
    });
});

app.Run();

