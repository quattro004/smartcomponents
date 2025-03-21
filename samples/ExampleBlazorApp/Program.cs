// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ExampleBlazorApp.Components;
using Microsoft.Extensions.AI;
using OpenAI;
using SmartComponents.Inference;
using SmartComponents.LocalEmbeddings;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddRepoSharedConfig();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSmartComponents()
    .WithAntiforgeryValidation();

builder.Services.AddSingleton<LocalEmbedder>();
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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

var embedder = app.Services.GetRequiredService<LocalEmbedder>();
var expenseCategories = embedder.EmbedRange(
    ["Groceries", "Utilities", "Rent", "Mortgage", "Car Payment", "Car Insurance", "Health Insurance", "Life Insurance", "Home Insurance", "Gas", "Public Transportation", "Dining Out", "Entertainment", "Travel", "Clothing", "Electronics", "Home Improvement", "Gifts", "Charity", "Education", "Childcare", "Pet Care", "Other"]);
var issueLabels = embedder.EmbedRange(
    ["Bug", "Docs", "Enhancement", "Question", "UI (Android)", "UI (iOS)", "UI (Windows)", "UI (Mac)", "Performance", "Security", "Authentication", "Accessibility"]);

app.MapSmartComboBox("/api/suggestions/expense-category",
    request => embedder.FindClosest(request.Query, expenseCategories));

app.MapSmartComboBox("/api/suggestions/issue-label",
    request => embedder.FindClosest(request.Query, issueLabels));

app.Run();
