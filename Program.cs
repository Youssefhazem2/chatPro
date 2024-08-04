using chatApp;
using chatApp.Controllers;
using chatApp.Hubs;
using chatApp.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
// Configure DbContext with connection string
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ChatAppDB")));
builder.Services.AddAuthorization();
builder.Services.AddScoped<MemberController>();
builder.Services.AddScoped<GroupController>();
builder.Services.AddIdentityApiEndpoints<User>().AddEntityFrameworkStores<AuthDbContext>();
var app = builder.Build();

app.MapIdentityApi<User>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapHub<ChatHub>("chathub");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
