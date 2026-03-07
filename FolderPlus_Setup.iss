[Setup]
; Metadata for Mitraphix Design
AppName=Mitraphix Folder+
AppVersion=1.0
AppPublisher=Mitraphix Design
AppPublisherURL=https://www.mitraphix.com
DefaultDirName={commonpf}\Mitraphix\FolderPlus
DefaultGroupName=Mitraphix Design
SetupIconFile=C:\Users\smitr\source\repos\FolderPlus\FolderPlus\logo.ico
UninstallDisplayIcon={app}\logo.ico

; Output location: Project directory
OutputDir=C:\Users\smitr\source\repos\FolderPlus\FolderPlus\Output
OutputBaseFilename=Mitraphix_FolderPlus_Setup
Compression=lzma
SolidCompression=yes

[Files]
Source: "C:\Users\smitr\source\repos\FolderPlus\FolderPlus\bin\Release\FolderPlus.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\smitr\source\repos\FolderPlus\FolderPlus\logo.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Mitraphix Folder+"; Filename: "{app}\FolderPlus.exe"
Name: "{commondesktop}\Mitraphix Folder+"; Filename: "{app}\FolderPlus.exe"

[Registry]
; Standard Folder+ Menu
Root: HKCR; Subkey: "Directory\Background\shell\FolderPlus"; ValueType: string; ValueData: "Folder+"; Flags: uninsdeletekey
Root: HKCR; Subkey: "Directory\Background\shell\FolderPlus"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\logo.ico"
Root: HKCR; Subkey: "Directory\Background\shell\FolderPlus\command"; ValueType: string; ValueData: """{app}\FolderPlus.exe"" ""%V """

; Mitraphix New+ Menu
Root: HKCR; Subkey: "Directory\Background\shell\MitraphixNewPlus"; ValueType: string; ValueData: "New+"; Flags: uninsdeletekey
Root: HKCR; Subkey: "Directory\Background\shell\MitraphixNewPlus"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\logo.ico"
Root: HKCR; Subkey: "Directory\Background\shell\MitraphixNewPlus\command"; ValueType: string; ValueData: """{app}\FolderPlus.exe"" -newplus ""%V """