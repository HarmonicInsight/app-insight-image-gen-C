using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace InsightMediaGenerator.License;

/// <summary>
/// InsightLicenseManager - ライセンス管理クラス (INIG)
/// Insight-Common 標準ライセンスキー形式: PPPP-PLAN-YYMM-HASH-SIG1-SIG2
/// PPPP=製品コード(INIG), PLAN=プラン, YYMM=有効期限(年月),
/// HASH=メールSHA256 Base32(4文字), SIG1-SIG2=HMAC-SHA256署名 Base32(8文字)
/// 保存先: %APPDATA%/HarmonicInsight/INIG/license.json
/// </summary>
public class InsightLicenseManager
{
    private const string ProductCode = "INIG";
    private const string CompanyFolder = "HarmonicInsight";
    private const string LicenseFileName = "license.json";

    private static readonly Regex LicenseKeyPattern = new(
        @"^INIG-(FREE|TRIAL|STD|PRO|ENT)-\d{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}$",
        RegexOptions.Compiled);

    private LicenseInfo? _currentLicense;

    public string CurrentPlan => _currentLicense?.Plan ?? "FREE";
    public DateTime? ExpiresAt => _currentLicense?.ExpiresAt;
    public string? LicenseKey => _currentLicense?.Key;
    public bool IsActivated => _currentLicense != null && !string.IsNullOrEmpty(_currentLicense.Key);

    /// <summary>
    /// ライセンス情報をローカルストレージから読み込む
    /// </summary>
    public async Task LoadAsync()
    {
        // 優先順位: 環境変数 > ローカルファイル
        var envKey = Environment.GetEnvironmentVariable("INIG_LICENSE_KEY");
        if (!string.IsNullOrEmpty(envKey))
        {
            await ActivateAsync(envKey, null);
            return;
        }

        var licensePath = GetLicenseFilePath();
        if (File.Exists(licensePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(licensePath);
                _currentLicense = JsonSerializer.Deserialize<LicenseInfo>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                // Validate expiry
                if (_currentLicense?.ExpiresAt.HasValue == true && _currentLicense.ExpiresAt.Value < DateTime.UtcNow)
                {
                    _currentLicense = new LicenseInfo { Plan = "FREE" };
                }
            }
            catch
            {
                _currentLicense = new LicenseInfo { Plan = "FREE" };
            }
        }
        else
        {
            _currentLicense = new LicenseInfo { Plan = "FREE" };
        }
    }

    /// <summary>
    /// ライセンスキーでアクティベートする
    /// </summary>
    public async Task<(bool Success, string Message)> ActivateAsync(string licenseKey, string? email)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
        {
            return (false, "ライセンスキーを入力してください。");
        }

        if (!LicenseKeyPattern.IsMatch(licenseKey))
        {
            return (false, "ライセンスキーの形式が正しくありません。\n形式: INIG-{PLAN}-{YYMM}-{HASH}-{SIG1}-{SIG2}");
        }

        // Extract plan from key
        var parts = licenseKey.Split('-');
        var plan = parts[1];

        // TODO: Validate signature via HMAC-SHA256 against license server
        // For now, accept the key format and extract plan

        // Expiry based on Insight-Common standard:
        // FREE=perpetual, TRIAL=14 days, STD/PRO=annual, ENT=perpetual
        DateTime? expiresAt = plan switch
        {
            "FREE" => null,
            "TRIAL" => DateTime.UtcNow.AddDays(14),
            "ENT" => null,
            _ => DateTime.UtcNow.AddDays(365) // STD, PRO: annual
        };

        _currentLicense = new LicenseInfo
        {
            Key = licenseKey,
            Email = email,
            Plan = plan,
            ActivatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            ProductCode = ProductCode
        };

        // Save to local storage
        await SaveLicenseAsync();

        return (true, $"ライセンスが正常にアクティベートされました。\nプラン: {plan}");
    }

    /// <summary>
    /// ライセンスをクリアする
    /// </summary>
    public async Task ClearAsync()
    {
        _currentLicense = new LicenseInfo { Plan = "FREE" };

        var licensePath = GetLicenseFilePath();
        if (File.Exists(licensePath))
        {
            File.Delete(licensePath);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 機能がCurrentPlanで利用可能かチェック
    /// </summary>
    public bool CheckFeature(string featureKey)
    {
        return featureKey switch
        {
            "generate_image" => true,
            "generate_audio" => true,
            "batch_image" => CurrentPlan is "TRIAL" or "STD" or "PRO" or "ENT",
            "hi_res" => CurrentPlan is "TRIAL" or "PRO" or "ENT",
            "cloud_sync" => CurrentPlan is "TRIAL" or "PRO" or "ENT",
            "ai_assistant" => CurrentPlan is "TRIAL" or "PRO" or "ENT",
            _ => false
        };
    }

    /// <summary>
    /// 機能の数量制限を取得 (-1 = 無制限)
    /// </summary>
    public int GetFeatureLimit(string featureKey)
    {
        if (featureKey == "character_prompts")
        {
            return CurrentPlan switch
            {
                "FREE" => 3,
                "TRIAL" => -1,
                "STD" => 20,
                "PRO" => -1,
                "ENT" => -1,
                _ => 3
            };
        }

        return -1;
    }

    /// <summary>
    /// ライセンスキーをマスクして表示用に返す
    /// </summary>
    public static string MaskKey(string key)
    {
        if (string.IsNullOrEmpty(key) || key.Length < 10)
            return "****";

        return key[..9] + new string('*', key.Length - 9);
    }

    private async Task SaveLicenseAsync()
    {
        if (_currentLicense == null) return;

        var licensePath = GetLicenseFilePath();
        var dir = Path.GetDirectoryName(licensePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(_currentLicense, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await File.WriteAllTextAsync(licensePath, json);
    }

    private static string GetLicenseFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, CompanyFolder, ProductCode, LicenseFileName);
    }
}

public class LicenseInfo
{
    public string? Key { get; set; }
    public string? Email { get; set; }
    public string Plan { get; set; } = "FREE";
    public string ProductCode { get; set; } = "INIG";
    public DateTime? ActivatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
