using System.Text;
using System.Text.Json;
using InsightMediaGenerator.Models;
using InsightMediaGenerator.Services.Interfaces;

namespace InsightMediaGenerator.Api.Endpoints;

public static class PromptEndpoints
{
    public static void MapPromptEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/prompts")
            .WithTags("Prompts");

        // ── List all uploaded prompt JSON files ──
        group.MapGet("/", async (IDatabaseService dbService) =>
        {
            var files = await dbService.GetJsonFilesAsync();
            var response = files.Select(f => new PromptFileResponse
            {
                Id = f.Id,
                FileName = f.FileName,
                FilePath = f.FilePath,
                UploadedAt = f.UploadedAt,
                Comment = f.Comment
            }).ToList();

            return ApiResponse<List<PromptFileResponse>>.Ok(response);
        })
        .WithName("ListPromptFiles")
        .WithDescription("List all uploaded character prompt JSON files");

        // ── Upload a new prompt file (via JSON body) ──
        group.MapPost("/", async (
            UploadPromptFileRequest request,
            IFileService fileService,
            IDatabaseService dbService) =>
        {
            if (string.IsNullOrWhiteSpace(request.FileName))
                return Results.BadRequest(ApiResponse.Fail("file_name is required"));
            if (request.Characters.Count == 0)
                return Results.BadRequest(ApiResponse.Fail("characters array must not be empty"));

            var fileName = request.FileName.EndsWith(".json")
                ? request.FileName
                : $"{request.FileName}.json";

            var jsonContent = JsonSerializer.Serialize(request.Characters.Select(c => new
            {
                name = c.Name,
                file_name = c.FileName,
                prompt = c.Prompt,
                negative_prompt = c.NegativePrompt
            }), new JsonSerializerOptions { WriteIndented = true });

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent));
            var filePath = await fileService.SaveJsonFileAsync(fileName, stream);

            var record = new JsonFileRecord
            {
                FileName = fileName,
                FilePath = filePath,
                UploadedAt = DateTime.Now,
                Comment = request.Comment
            };
            var id = await dbService.SaveJsonFileAsync(record);

            return Results.Created($"/api/prompts/{id}", ApiResponse<PromptFileResponse>.Ok(new PromptFileResponse
            {
                Id = id,
                FileName = fileName,
                FilePath = filePath,
                UploadedAt = record.UploadedAt,
                Comment = record.Comment
            }));
        })
        .WithName("UploadPromptFile")
        .WithDescription("Upload a character prompt file as JSON. Creates the file on disk and registers in database.");

        // ── Get characters from a prompt file ──
        group.MapGet("/{id:int}/characters", async (
            int id,
            IFileService fileService,
            IDatabaseService dbService) =>
        {
            var files = await dbService.GetJsonFilesAsync();
            var file = files.FirstOrDefault(f => f.Id == id);
            if (file == null)
                return Results.NotFound(ApiResponse.Fail($"Prompt file with id {id} not found"));

            var prompts = await fileService.LoadPromptsFromJsonAsync(file.FilePath);
            var response = prompts.Select(p => new CharacterPromptDto
            {
                Name = p.Name,
                FileName = p.FileName,
                Prompt = p.Prompt,
                NegativePrompt = p.NegativePrompt
            }).ToList();

            return Results.Json(ApiResponse<List<CharacterPromptDto>>.Ok(response));
        })
        .WithName("GetPromptCharacters")
        .WithDescription("Get character definitions from a specific prompt file");

        // ── Update comment on a prompt file ──
        group.MapPatch("/{id:int}", async (
            int id,
            UpdateCommentRequest request,
            IDatabaseService dbService) =>
        {
            await dbService.UpdateJsonCommentAsync(id, request.Comment ?? "");
            return Results.Json(ApiResponse.Ok());
        })
        .WithName("UpdatePromptComment")
        .WithDescription("Update the comment on a prompt file");

        // ── Delete a prompt file ──
        group.MapDelete("/{id:int}", async (
            int id,
            IFileService fileService,
            IDatabaseService dbService) =>
        {
            var files = await dbService.GetJsonFilesAsync();
            var file = files.FirstOrDefault(f => f.Id == id);
            if (file == null)
                return Results.NotFound(ApiResponse.Fail($"Prompt file with id {id} not found"));

            await fileService.DeleteJsonFileAsync(file.FilePath);
            await dbService.DeleteJsonFileAsync(id);

            return Results.Json(ApiResponse.Ok());
        })
        .WithName("DeletePromptFile")
        .WithDescription("Delete a prompt file from disk and database");
    }
}

public class UpdateCommentRequest
{
    public string? Comment { get; set; }
}
