// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using E2ETests;
using Microsoft.Extensions.AI;
using OpenAI;
using SmartComponents.Inference;
using SmartComponents.LocalEmbeddings;

namespace TestMvcApp;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddRepoSharedConfig();

        // Add services to the container.
        builder.Services.AddControllersWithViews();
        builder.Services.AddScoped<SmartPasteInference, SmartPasteInferenceForTests>();
        builder.Services.AddSmartComponents()
            .WithAntiforgeryValidation(); // This doesn't benefit most apps, but we'll validate it works in E2E tests

        // Note: the StartupKey value is just there so the app will start up. 
        builder.Services.AddSingleton(new OpenAIClient(builder.Configuration["AI:OpenAI:Key"] ?? "StartupKey"));
        builder.Services.AddChatClient(services =>
        {
            var chatClient = new SmartComponentsChatClient(services.GetRequiredService<OpenAIClient>()
                .AsChatClient(builder.Configuration["AI:OpenAI:Chat:ModelId"] ?? "gpt-4o-mini"));
            return chatClient;
        });
        builder.Services.AddEmbeddingGenerator(services =>
            services.GetRequiredService<OpenAIClient>().AsEmbeddingGenerator(builder.Configuration["AI:OpenAI:Embedding:ModelId"] ?? "text-embedding-3-small"));

        var app = builder.Build();

        // Show we can work with pathbase by enforcing its use
        app.UsePathBase("/subdir");
        app.Use(async (ctx, next) =>
        {
            if (!ctx.Request.PathBase.Equals("/subdir", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Response.StatusCode = 404;
                await ctx.Response.WriteAsync("This server only serves requests at /subdir");
            }
            else
            {
                await next();
            }
        });

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        var embedder = new LocalEmbedder();
        var accountingCategories = embedder.EmbedRange(E2ETests.TestData.AccountingCategories);

        app.MapSmartComboBox("/api/accounting-categories",
            request => embedder.FindClosest(request.Query, accountingCategories));

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}
