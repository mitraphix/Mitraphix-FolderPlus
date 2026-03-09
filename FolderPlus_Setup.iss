[Setup]
; Unique ID for seamless background updates. NEVER change this GUID in future updates.
AppId={{MITRAPHIX-FOLDER-PLUS-8A7B6C5D4E3F2G1H}
AppName=Mitraphix Folder+
AppVersion=1.3.0
AppPublisher=Mitraphix Design
AppPublisherURL=https://www.mitraphix.com
DefaultDirName={commonpf}\Mitraphix\FolderPlus
DefaultGroupName=Mitraphix Design
SetupIconFile=C:\Users\smitr\source\repos\FolderPlus\FolderPlus\logo.ico
UninstallDisplayIcon={app}\logo.ico

; Output location: Project directory
OutputDir=C:\Users\smitr\source\repos\FolderPlus\FolderPlus\Output
OutputBaseFilename=Mitraphix_FolderPlus_Setup_v1.3.0
Compression=lzma
SolidCompression=yes

; Disable Windows default warning messages for background apps
CloseApplications=no
RestartApplications=no
DirExistsWarning=no

[Files]
Source: "C:\Users\smitr\source\repos\FolderPlus\FolderPlus\bin\Release\FolderPlus.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\smitr\source\repos\FolderPlus\FolderPlus\logo.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Mitraphix Folder+"; Filename: "{app}\FolderPlus.exe"
Name: "{commondesktop}\Mitraphix Folder+"; Filename: "{app}\FolderPlus.exe"
; Add to Windows Startup (Boot) running silently
Name: "{autostartup}\Mitraphix Folder+"; Filename: "{app}\FolderPlus.exe"; Parameters: "-tray"

[Registry]
; Standard Folder+ Menu
Root: HKCR; Subkey: "Directory\Background\shell\FolderPlus"; ValueType: string; ValueData: "Folder+"; Flags: uninsdeletekey
Root: HKCR; Subkey: "Directory\Background\shell\FolderPlus"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\logo.ico"
Root: HKCR; Subkey: "Directory\Background\shell\FolderPlus\command"; ValueType: string; ValueData: """{app}\FolderPlus.exe"" ""%V """

; Mitraphix New+ Menu
Root: HKCR; Subkey: "Directory\Background\shell\MitraphixNewPlus"; ValueType: string; ValueData: "New+"; Flags: uninsdeletekey
Root: HKCR; Subkey: "Directory\Background\shell\MitraphixNewPlus"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\logo.ico"
Root: HKCR; Subkey: "Directory\Background\shell\MitraphixNewPlus\command"; ValueType: string; ValueData: """{app}\FolderPlus.exe"" -newplus ""%V """

[Run]
; Auto-Run the app silently in the background immediately after installation
Filename: "{app}\FolderPlus.exe"; Parameters: "-tray"; Description: "Start Folder+ Background Service"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  // Silently force-close the background app before installation starts to avoid overwrite errors
  Exec('cmd.exe', '/c taskkill /f /im FolderPlus.exe /t', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := True;
end;