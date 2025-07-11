; Script generated by the Inno Script Studio Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "Cloud OCR Snip"
#define MyAppNameForFilename "CloudOCRSnip"
#define MyAppVersion "1.3.9.0"
#define MyAppVersionForFilename "1-3-9-0"
#define MyAppPublisher "Takuma Otake"
#define MyAppExeName "CloudOCRSnip.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{0DC4B2C6-9D64-4228-AA88-2173BD12468A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName}
AppPublisher={#MyAppPublisher}
DefaultDirName={commonpf64}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile={#SourcePath}\LICENSE
OutputDir={#SourcePath}\Installers
OutputBaseFilename={#MyAppNameForFilename}_{#MyAppVersionForFilename}_Setup
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
ShowLanguageDialog=auto
VersionInfoVersion={#MyAppVersion}
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
Name: "armenian"; MessagesFile: "compiler:Languages\Armenian.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "bulgarian"; MessagesFile: "compiler:Languages\Bulgarian.isl"
Name: "catalan"; MessagesFile: "compiler:Languages\Catalan.isl"
Name: "corsican"; MessagesFile: "compiler:Languages\Corsican.isl"
Name: "czech"; MessagesFile: "compiler:Languages\Czech.isl"
Name: "danish"; MessagesFile: "compiler:Languages\Danish.isl"
Name: "dutch"; MessagesFile: "compiler:Languages\Dutch.isl"
Name: "finnish"; MessagesFile: "compiler:Languages\Finnish.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "hebrew"; MessagesFile: "compiler:Languages\Hebrew.isl"
Name: "hungarian"; MessagesFile: "compiler:Languages\Hungarian.isl"
Name: "icelandic"; MessagesFile: "compiler:Languages\Icelandic.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "norwegian"; MessagesFile: "compiler:Languages\Norwegian.isl"
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "slovak"; MessagesFile: "compiler:Languages\Slovak.isl"
Name: "slovenian"; MessagesFile: "compiler:Languages\Slovenian.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "swedish"; MessagesFile: "compiler:Languages\Swedish.isl"
Name: "tamil"; MessagesFile: "compiler:Languages\Tamil.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "ukrainian"; MessagesFile: "compiler:Languages\Ukrainian.isl"

[Files]
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net8.0-windows10.0.22621.0\publish\Cloud OCR Snip.exe"; DestDir: "{app}"; DestName: "{#MyAppExeName}"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net8.0-windows10.0.22621.0\publish\D3DCompiler_47_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net8.0-windows10.0.22621.0\publish\PenImc_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net8.0-windows10.0.22621.0\publish\PresentationNative_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net8.0-windows10.0.22621.0\publish\vcruntime140_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net8.0-windows10.0.22621.0\publish\wpfgfx_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Registry]
Root: "HKLM"; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"" --startup_mode"; Flags: uninsdeletevalue

[UninstallRun]
Filename: "powershell.exe"; Parameters: "-Command ""taskkill -f -t -im '{#MyAppExeName}'; Start-Sleep -Seconds 2;"""; Flags: runhidden; RunOnceId: "Process details: Terminate the application running with normal privileges."

[UninstallDelete]
Type: files; Name: "{app}\additional_data.json"

[Code]
var ErrorCode: Integer;
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssDone then
  begin
    ShellExec('', ExpandConstant('"{app}\{#MyAppExeName}"'), '', '', SW_SHOW, ewNoWait, ErrorCode);
  end;
end;
