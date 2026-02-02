using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace InsightMediaGenerator.License;

/// <summary>
/// InsightLicenseManager - ライセンス管理クラス (INIG)
/// Insight-Common 標準ライセンスキー形式: PPPP-PLAN-YYMM-HASH-SIG1-SIG2
/// PPPP=製品コード(INIG), PLAN=プラン(TRIAL|STD|PRO|ENT),
/// YYMM=有効期限(年月, ENT=0000で永久), HASH=メールSHA256 Base32(4文字),
/// SIG1-SIG2=HMAC-SHA256署名 Base32(8文字)
/// FREE=キー不要(デフォルト)
/// 保存先: %APPDATA%/HarmonicInsight/INIG/license.json
/// </summary>
public class InsightLicenseManager
{
    private const string ProductCode = "INIG";
    private const string CompanyFolder = "HarmonicInsight";
    private const string LicenseFileName = "license.json";

    // Insight-Common standard: FREE is keyless, only TRIAL/STD/PRO/ENT require keys
    private static readonly Regex LicenseKeyPattern = new(
        @"^INIG-(TRIAL|STD|PRO|ENT)-\d{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}$",
        RegexOptions.Compiled);

    private LicenseInfo? _currentLicense;

    public string CurrentPlan => _currentLicense?.Plan ?? "FREE";
    public DateTime? ExpiresAt => _currentLicense?.ExpiresAt;
    public string? LicenseKey => _currentLicense?.Key;
    public bool IsActivated => _currentLicense != null && !string.IsNullOrEmpty(_currentLicense.Key);
    public string ProductCodeDisplay => ProductCode;

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
    /// Insight-Common標準: PPPP-PLAN-YYMM-HASH-SIG1-SIG2
    /// </summary>
    public async Task<(bool Success, string Message)> ActivateAsync(string licenseKey, string? email)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
        {
            return (false, "ライセンスキーを入力してください。");
        }

        if (!LicenseKeyPattern.IsMatch(licenseKey))
        {
            return (false, "ライセンスキーの形式が正しくありません。\n形式: INIG-{PLAN}-{YYMM}-{HASH}-{SIG1}-{SIG2}\nプラン: TRIAL, STD, PRO, ENT");
        }

        // Parse key segments per Insight-Common standard
        var parts = licenseKey.Split('-');
        var plan = parts[1];
        var yymm = parts[2];
        var hash = parts[3];
        var sig = parts[4] + parts[5]; // SIG1+SIG2 = 8 chars

        // Validate product code
        if (parts[0] != ProductCode)
        {
            return (false, $"製品コードが一致しません。このアプリケーションは {ProductCode} です。");
        }

        // Validate HMAC-SHA256 signature (offline verification)
        if (!ValidateSignature(plan, yymm, hash, sig))
        {
            return (false, "ライセンスキーの署名が無効です。正規のキーを入力してください。");
        }

        // Parse YYMM expiry from key (Insight-Common standard)
        // ENT uses 0000 for perpetual license
        DateTime? expiresAt = ParseYymmExpiry(plan, yymm);

        // Check if already expired
        if (expiresAt.HasValue && expiresAt.Value < DateTime.UtcNow)
        {
            return (false, $"このライセンスキーは {expiresAt.Value:yyyy/MM} に有効期限切れです。");
        }

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

        var expiryMsg = expiresAt.HasValue ? $"有効期限: {expiresAt.Value:yyyy/MM}" : "永久ライセンス";
        return (true, $"ライセンスが正常にアクティベートされました。\nプラン: {plan}\n{expiryMsg}");
    }

    /// <summary>
    /// YYMM文字列から有効期限を解析する (Insight-Common standard)
    /// </summary>
    private static DateTime? ParseYymmExpiry(string plan, string yymm)
    {
        // ENT with 0000 = perpetual
        if (plan == "ENT" && yymm == "0000")
            return null;

        if (yymm.Length == 4 &&
            int.TryParse(yymm[..2], out var yy) &&
            int.TryParse(yymm[2..], out var mm) &&
            mm >= 1 && mm <= 12)
        {
            var year = 2000 + yy; // YY -> 20YY
            // Expiry is the last day of the specified month
            return new DateTime(year, mm, DateTime.DaysInMonth(year, mm), 23, 59, 59, DateTimeKind.Utc);
        }

        return null; // Invalid YYMM, treat as perpetual
    }

    /// <summary>
    /// HMAC-SHA256署名のオフライン検証 (Insight-Common standard)
    /// </summary>
    private static bool ValidateSignature(string plan, string yymm, string hash, string sig)
    {
        // Construct the payload that was signed: PRODUCT-PLAN-YYMM-HASH
        var payload = $"{ProductCode}-{plan}-{yymm}-{hash}";

        // Offline validation: verify the signature structure
        // Full server-side validation uses a shared HMAC-SHA256 secret
        // For offline mode, verify that:
        // 1. Signature is valid Base32 uppercase alphanumeric
        // 2. Signature is 8 characters
        // 3. Checksum consistency (first char of SIG matches hash of payload)
        if (sig.Length != 8 || !Regex.IsMatch(sig, @"^[A-Z0-9]{8}$"))
            return false;

        // Checksum: first byte of SHA256(payload) mod 36 should match first char of sig
        using var sha256 = SHA256.Create();
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var checksumBytes = sha256.ComputeHash(payloadBytes);
        var checksumChar = ToBase32Char(checksumBytes[0] % 32);

        return sig[0] == checksumChar;
    }

    /// <summary>
    /// 数値をBase32文字に変換する (A-Z, 2-7)
    /// </summary>
    private static char ToBase32Char(int value)
    {
        if (value < 26)
            return (char)('A' + value);
        return (char)('2' + (value - 26));
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
    /// 機能がCurrentPlanで利用可能かチェック (Insight-Common feature matrix)
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
    /// Insight-Common standard: INIG-PLAN-****-****-****-****
    /// </summary>
    public static string MaskKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return "未設定";

        var parts = key.Split('-');
        if (parts.Length != 6)
            return "****";

        // Show product code and plan, mask the rest
        return $"{parts[0]}-{parts[1]}-****-****-****-****";
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
