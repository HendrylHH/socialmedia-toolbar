[Setup]
AppName=Social Media Toolbar
AppVersion={#AppVersion}
DefaultDirName={pf}\SocialMediaToolbar
OutputDir=.
OutputBaseFilename=ToolbarInstaller
Compression=lzma
SolidCompression=yes

[Files]
Source: "app-publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Social Media Toolbar"; Filename: "{app}\ToolbarApp.exe"