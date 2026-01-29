using System.IO;
using Microsoft.Data.Sqlite;
using InsightMediaGenerator.Models;
using InsightMediaGenerator.Services.Interfaces;

namespace InsightMediaGenerator.Services;

public class DatabaseService : IDatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(AppConfig config, IFileService fileService)
    {
        var dbPath = fileService.ResolvePath(config.Data.DatabaseFile);
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _connectionString = $"Data Source={dbPath}";
    }

    public async Task InitializeAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS json_files (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                file_name TEXT NOT NULL UNIQUE,
                file_path TEXT NOT NULL,
                uploaded_at TEXT NOT NULL,
                comment TEXT
            );

            CREATE TABLE IF NOT EXISTS images (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                file_name TEXT NOT NULL UNIQUE,
                file_path TEXT NOT NULL,
                timestamp TEXT NOT NULL,
                model TEXT,
                lora TEXT,
                lora_weight REAL,
                prompt TEXT,
                negative_prompt TEXT,
                steps INTEGER,
                width INTEGER,
                height INTEGER,
                sampler_name TEXT,
                cfg_scale REAL,
                char_name TEXT,
                json_file_name TEXT,
                json_file_id INTEGER,
                batch_index INTEGER DEFAULT 0,
                FOREIGN KEY (json_file_id) REFERENCES json_files(id) ON DELETE SET NULL
            );
        ";

        await command.ExecuteNonQueryAsync();
    }

    public async Task<int> SaveJsonFileAsync(JsonFileRecord record)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO json_files (file_name, file_path, uploaded_at, comment)
            VALUES ($fileName, $filePath, $uploadedAt, $comment)
            RETURNING id;
        ";
        command.Parameters.AddWithValue("$fileName", record.FileName);
        command.Parameters.AddWithValue("$filePath", record.FilePath);
        command.Parameters.AddWithValue("$uploadedAt", record.UploadedAt.ToString("O"));
        command.Parameters.AddWithValue("$comment", record.Comment ?? (object)DBNull.Value);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<IReadOnlyList<JsonFileRecord>> GetJsonFilesAsync()
    {
        var records = new List<JsonFileRecord>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT id, file_name, file_path, uploaded_at, comment FROM json_files ORDER BY uploaded_at DESC";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            records.Add(new JsonFileRecord
            {
                Id = reader.GetInt32(0),
                FileName = reader.GetString(1),
                FilePath = reader.GetString(2),
                UploadedAt = DateTime.Parse(reader.GetString(3)),
                Comment = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }

        return records;
    }

    public async Task DeleteJsonFileAsync(int id)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM json_files WHERE id = $id";
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateJsonCommentAsync(int id, string comment)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE json_files SET comment = $comment WHERE id = $id";
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$comment", comment);
        await command.ExecuteNonQueryAsync();
    }

    public async Task SaveImageMetadataAsync(ImageMetadata metadata)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO images (
                file_name, file_path, timestamp, model, lora, lora_weight,
                prompt, negative_prompt, steps, width, height,
                sampler_name, cfg_scale, char_name, json_file_name, json_file_id, batch_index
            ) VALUES (
                $fileName, $filePath, $timestamp, $model, $lora, $loraWeight,
                $prompt, $negativePrompt, $steps, $width, $height,
                $samplerName, $cfgScale, $charName, $jsonFileName, $jsonFileId, $batchIndex
            )
        ";
        command.Parameters.AddWithValue("$fileName", metadata.FileName);
        command.Parameters.AddWithValue("$filePath", metadata.FilePath);
        command.Parameters.AddWithValue("$timestamp", metadata.Timestamp.ToString("O"));
        command.Parameters.AddWithValue("$model", metadata.Model ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$lora", metadata.Lora ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$loraWeight", metadata.LoraWeight);
        command.Parameters.AddWithValue("$prompt", metadata.Prompt);
        command.Parameters.AddWithValue("$negativePrompt", metadata.NegativePrompt);
        command.Parameters.AddWithValue("$steps", metadata.Steps);
        command.Parameters.AddWithValue("$width", metadata.Width);
        command.Parameters.AddWithValue("$height", metadata.Height);
        command.Parameters.AddWithValue("$samplerName", metadata.SamplerName);
        command.Parameters.AddWithValue("$cfgScale", metadata.CfgScale);
        command.Parameters.AddWithValue("$charName", metadata.CharName);
        command.Parameters.AddWithValue("$jsonFileName", metadata.JsonFileName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$jsonFileId", metadata.JsonFileId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$batchIndex", metadata.BatchIndex);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<ImageMetadata>> GetImagesAsync()
    {
        var images = new List<ImageMetadata>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, file_name, file_path, timestamp, model, lora, lora_weight,
                   prompt, negative_prompt, steps, width, height,
                   sampler_name, cfg_scale, char_name, json_file_name, json_file_id, batch_index
            FROM images ORDER BY timestamp DESC
        ";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            images.Add(new ImageMetadata
            {
                Id = reader.GetInt32(0),
                FileName = reader.GetString(1),
                FilePath = reader.GetString(2),
                Timestamp = DateTime.Parse(reader.GetString(3)),
                Model = reader.IsDBNull(4) ? null : reader.GetString(4),
                Lora = reader.IsDBNull(5) ? null : reader.GetString(5),
                LoraWeight = reader.IsDBNull(6) ? 0 : reader.GetDouble(6),
                Prompt = reader.GetString(7),
                NegativePrompt = reader.GetString(8),
                Steps = reader.GetInt32(9),
                Width = reader.GetInt32(10),
                Height = reader.GetInt32(11),
                SamplerName = reader.GetString(12),
                CfgScale = reader.GetDouble(13),
                CharName = reader.GetString(14),
                JsonFileName = reader.IsDBNull(15) ? null : reader.GetString(15),
                JsonFileId = reader.IsDBNull(16) ? null : reader.GetInt32(16),
                BatchIndex = reader.GetInt32(17)
            });
        }

        return images;
    }
}
