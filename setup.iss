; InsightImageGen Installer Script
; Inno Setup 6.x

#define MyAppName "InsightImageGen"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Harmonic Insight"
#define MyAppURL "https://harmonic-insight.com/products/insight-image-gen"
#define MyAppExeName "InsightMediaGenerator.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=LICENSE.txt
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=Output
OutputBaseFilename=InsightImageGen_Setup_{#MyAppVersion}
; SetupIconFile=InsightMediaGenerator\Resources\app.ico  ; Uncomment when icon is available
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
Source: "publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\appsettings.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\*.dll"; DestDir: "{app}"; Flags: ignoreversion; Excludes: ""
Source: "publish\*.pdb"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
; Note: Single file publish may not have DLLs

[Dirs]
Name: "{app}\data"
Name: "{app}\data\audio"
Name: "{app}\data\json_files"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
  // Check for .NET 8 runtime (optional - self-contained doesn't need this)
end;
