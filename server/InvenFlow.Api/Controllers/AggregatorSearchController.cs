using System.Text.Json;
using InvenFlow.Api.Application.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvenFlow.Api.Controllers;

[ApiController]
[Route("api/v1/aggregator")]
public class AggregatorSearchController : ControllerBase
{
    private readonly AggregatorSearchService _searchService;
    private readonly ILogger<AggregatorSearchController> _logger;

    public AggregatorSearchController(
        AggregatorSearchService searchService,
        ILogger<AggregatorSearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search(
        [FromQuery] string query,
        [FromQuery] string category = "Electronics",
        [FromQuery] string? strategyMode = null,
        [FromQuery] string? preferredProvider = null)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Ok(Array.Empty<object>());
        }

        var grouped = await _searchService.SearchAsync(query, category, strategyMode, preferredProvider, HttpContext.RequestAborted);
        return Ok(grouped);
    }

    [HttpGet("search-stream")]
    [AllowAnonymous]
    public async Task SearchStream(
        [FromQuery] string query,
        [FromQuery] string category = "Electronics",
        [FromQuery] string? strategyMode = null,
        [FromQuery] string? preferredProvider = null)
    {
        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        if (string.IsNullOrWhiteSpace(query))
        {
            await Response.WriteAsync("event: completed\n");
            await Response.WriteAsync("data: []\n\n");
            await Response.Body.FlushAsync();
            return;
        }

        _logger.LogInformation("Starting aggregator SSE stream for query {Query} and category {Category}", query, category);

        await foreach (var item in _searchService.SearchStreamAsync(query, category, strategyMode, preferredProvider, HttpContext.RequestAborted))
        {
            var payload = JsonSerializer.Serialize(item);
            await Response.WriteAsync("event: result\n");
            await Response.WriteAsync($"data: {payload}\n\n");
            await Response.Body.FlushAsync();
        }

        await Response.WriteAsync("event: completed\n");
        await Response.WriteAsync("data: done\n\n");
        await Response.Body.FlushAsync();
    }
}
