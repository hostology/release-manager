using Cocona;
using Hostology.ReleaseManager.Clients;
using Hostology.ReleaseManager.Configuration;
using Hostology.ReleaseManager.Services;
using Microsoft.Extensions.DependencyInjection;

var builder = CoconaApp.CreateBuilder();
builder.Services.AddTransient<IConfigurationProvider, ConfigurationProvider>();
builder.Services.AddTransient<IGitService, GitService>();
builder.Services.AddTransient<IJiraClient, JiraClient>();
builder.Services.AddTransient<IJiraService, JiraService>();
builder.Services.AddTransient<ISlackClient, SlackClient>();
builder.Services.AddTransient<IProjectValidator, ProjectValidator>();
builder.Services.AddTransient<IRepositoryHandler, RepositoryHandler>();
builder.Services.AddTransient<IReleaseManager, ReleaseManager>();

var app = builder.Build();

app.Run(async (
        [Option('c', Description = "Specifies path to the configuration file.")]string? configurationPath,
        [Option('n', Description = "Specifies if messages should not be sent to Slack")]bool noSlack,
        IReleaseManager manager) 
    => await manager.Handle(configurationPath, noSlack));