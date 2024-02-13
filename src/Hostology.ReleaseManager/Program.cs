using Cocona;
using Hostology.ReleaseManager.Configuration;
using Hostology.ReleaseManager.Services;
using Microsoft.Extensions.DependencyInjection;

var builder = CoconaApp.CreateBuilder();
builder.Services.AddTransient<IConfigurationProvider, ConfigurationProvider>();
builder.Services.AddTransient<IReleaseManager, ReleaseManager>();
builder.Services.AddTransient<IGitService, GitService>();

var app = builder.Build();

app.Run(([Option('c', Description = "Specifies path to the configuration file.")]string? configurationPath, IReleaseManager manager) => manager.Handle(configurationPath));