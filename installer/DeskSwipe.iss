#define MyAppName "DeskSwipe"
#define MyAppVersion "0.2.0"
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
OutputBaseFilename=DeskSwipe-Setup-0.2.0

Compression=lzma2
SolidCompression=yes
WizardStyle=modern

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

SetupIconFile=..\assets\DeskSwipe.ico
UninstallDisplayIcon={app}\DeskSwipe.ico
UninstallDisplayName=DeskSwipe

CloseApplications=yes
RestartApplications=no
SetupLogging=yes

[Files]
Source: "..\release\DeskSwipe\DeskSwipe.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\release\DeskSwipe\DeskSwipeGestures.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\release\DeskSwipe\VirtualDesktopAccessor.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\release\DeskSwipe\DeskSwipe.ico"; DestDir: "{app}"; Flags: ignoreversion

Source: "..\release\DeskSwipe\Settings\*"; DestDir: "{app}\Settings"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userstartup}\DeskSwipe"; Filename: "{app}\DeskSwipeGestures.exe"; Parameters: "--startup"; WorkingDir: "{app}"; IconFilename: "{app}\DeskSwipe.ico"

Name: "{userprograms}\DeskSwipe"; Filename: "{app}\Settings\DeskSwipe.Settings.exe"; WorkingDir: "{app}\Settings"; IconFilename: "{app}\DeskSwipe.ico"

Name: "{userprograms}\DeskSwipe Settings"; Filename: "{app}\Settings\DeskSwipe.Settings.exe"; WorkingDir: "{app}\Settings"; IconFilename: "{app}\DeskSwipe.ico"
[Run]
Filename: "{app}\Settings\DeskSwipe.Settings.exe"; Description: "Open DeskSwipe"; Flags: nowait postinstall skipifsilent
