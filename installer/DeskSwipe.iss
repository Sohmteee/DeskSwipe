#define MyAppName "DeskSwipe"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "Sohmteee"
#define MyAppExeName "DeskSwipeGestures.exe"

[Setup]
AppId=DeskSwipe.Sohmteee
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\DeskSwipe
DefaultGroupName=DeskSwipe
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\release
OutputBaseFilename=DeskSwipe-Setup-0.1.0
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayName=DeskSwipe
CloseApplications=yes
RestartApplications=no
SetupLogging=yes

[Files]
Source: "..\release\DeskSwipe\DeskSwipe.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\release\DeskSwipe\DeskSwipeGestures.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\release\DeskSwipe\VirtualDesktopAccessor.dll"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{userstartup}\DeskSwipe"; Filename: "{app}\DeskSwipeGestures.exe"; WorkingDir: "{app}"
Name: "{userprograms}\DeskSwipe"; Filename: "{app}\DeskSwipeGestures.exe"; WorkingDir: "{app}"

[Run]
Filename: "{app}\DeskSwipeGestures.exe"; Description: "Start DeskSwipe"; Flags: nowait postinstall skipifsilent
