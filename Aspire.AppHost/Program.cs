var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.ApiUsingToxiProxy>("TodoApi").WithExternalHttpEndpoints();

builder.Build().Run();