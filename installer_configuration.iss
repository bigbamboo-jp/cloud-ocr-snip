; Script generated by the Inno Script Studio Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "Cloud OCR Snip"
#define MyAppNameForFile "Cloud-OCR-Snip"
#define MyAppVersion "1.2.0.0"                   
#define MyAppVersionForFile "1-2-0-0"
#define MyAppPublisher "Takuma Otake"
#define MyAppExeName "CloudOCRSnip.exe"
#define CurrentYear GetDateTimeString('yyyy', '', '')
#define dotNETInstallerExeName "windowsdesktop-runtime-6.0.1-win.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{30C91F43-B424-46B0-BCCD-ED15F901AC98}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName}
AppPublisher=Open Source Developer, {#MyAppPublisher}
DefaultDirName={pf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile={#SourcePath}\LICENSE.txt
OutputBaseFilename={#MyAppNameForFile}_{#MyAppVersionForFile}_Setup
Compression=lzma
SolidCompression=yes                                
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\{#MyAppExeName}
ShowLanguageDialog=auto
VersionInfoVersion={#MyAppVersion}
VersionInfoCopyright=Copyright (c) {#CurrentYear} {#MyAppPublisher}
OutputDir=Installers

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
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
Name: "icelandic"; MessagesFile: "compiler:Languages\Icelandic.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "norwegian"; MessagesFile: "compiler:Languages\Norwegian.isl"
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "slovak"; MessagesFile: "compiler:Languages\Slovak.isl"
Name: "slovenian"; MessagesFile: "compiler:Languages\Slovenian.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "ukrainian"; MessagesFile: "compiler:Languages\Ukrainian.isl"

[Files]
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net6.0-windows\Cloud OCR Snip.exe"; DestDir: "{app}"; DestName: "CloudOCRSnip.exe"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net6.0-windows\Cloud OCR Snip.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net6.0-windows\Cloud OCR Snip.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net6.0-windows\Cloud OCR Snip.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net6.0-windows\Google.Api.CommonProtos.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net6.0-windows\Google.Api.Gax.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net6.0-windows\Google.Api.Gax.Grpc.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net6.0-windows\Google.Api.Gax.Grpc.GrpcCore.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net6.0-windows\Google.Apis.Auth.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net6.0-windows\Google.Apis.Auth.PlatformServices.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net6.0-windows\Google.Apis.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net6.0-windows\Google.Apis.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net6.0-windows\Google.Cloud.Vision.V1.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net6.0-windows\Google.LongRunning.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net6.0-windows\Google.Protobuf.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net6.0-windows\Grpc.Auth.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net6.0-windows\Grpc.Core.Api.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net6.0-windows\Grpc.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net6.0-windows\Markdig.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net6.0-windows\Markdig.Wpf.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net6.0-windows\Microsoft.Bcl.AsyncInterfaces.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net6.0-windows\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net6.0-windows\ref\*"; DestDir: "{app}\ref"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#SourcePath}\Cloud OCR Snip\Cloud OCR Snip\bin\Release\net6.0-windows\runtimes\*"; DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#SourcePath}\windowsdesktop-runtime-6.0.1-win-x86.exe"; DestDir: "{tmp}"; DestName: "{#dotNETInstallerExeName}"; Flags: ignoreversion; Check: not Is64BitInstallMode
Source: "{#SourcePath}\windowsdesktop-runtime-6.0.1-win-x64.exe"; DestDir: "{tmp}"; DestName: "{#dotNETInstallerExeName}"; Flags: ignoreversion; Check: Is64BitInstallMode
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Registry]
Root: "HKLM"; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"" --startup_mode"; Flags: uninsdeletevalue

[Run]
Filename: "{tmp}\{#dotNETInstallerExeName}"; Parameters: "-install -quiet"

[UninstallRun]
Filename: "PowerShell"; Parameters: "-Command ""Start-Process -FilePath 'powershell' -Argument '-command taskkill -f -t -im ''{#MyAppExeName}''' -Verb RunAs; Start-Sleep -Seconds 3"""; Flags: runhidden

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
