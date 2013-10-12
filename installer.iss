;
;  RS TV Show Tracker
;  InnoSetup installer script
;
;  This is an alternative installer to the NSIS version.
;  It does not fully mirror all the features of the NSIS
;  version, therefore it should not be used for anything
;  other than a fresh install.
;

[Setup]
AppId={{D29DD4B8-1B2E-4736-A4BB-2CADF49FDB1B}
AppName=RS TV Show Tracker
#include "version.iss"
;AppVerName=RS TV Show Tracker 2.2
AppPublisher=RoliSoft
AppPublisherURL=http://lab.rolisoft.net/tvshowtracker.html
AppSupportURL=http://lab.rolisoft.net/tvshowtracker.html
AppUpdatesURL=http://lab.rolisoft.net/tvshowtracker.html
DefaultDirName={pf}\RoliSoft\RS TV Show Tracker
DefaultGroupName=RS TV Show Tracker
AllowNoIcons=yes
LicenseFile=LICENSE.rtf
OutputBaseFilename=tvshowtracker_v2_setup
Compression=lzma
SolidCompression=yes
WizardImageFile=Images\nsis-wizard.bmp
;WizardSmallImageFile=Images\nsis-header.bmp
WizardSmallImageFile=Images\is-header.bmp
WizardImageStretch=False
UninstallDisplayIcon={app}\RSTVShowTracker.exe
MinVersion=0,6.0
OutputDir=.
VersionInfoCompany=RoliSoft
VersionInfoDescription=RS TV Show Tracker
VersionInfoCopyright=© 2013 RoliSoft
VersionInfoProductName=RS TV Show Tracker
PrivilegesRequired=admin

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "bin\Release\RSTVShowTracker.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\RSTVShowTracker.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\RSTVShowTracker.License.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\RSTVShowTracker.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\libs\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\RS TV Show Tracker"; Filename: "{app}\RSTVShowTracker.exe"
Name: "{group}\{cm:ProgramOnTheWeb,RS TV Show Tracker}"; Filename: "http://lab.rolisoft.net/tvshowtracker.html"
Name: "{group}\{cm:UninstallProgram,RS TV Show Tracker}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\RS TV Show Tracker"; Filename: "{app}\RSTVShowTracker.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\RSTVShowTracker.exe"; Description: "{cm:LaunchProgram,RS TV Show Tracker}"; Flags: nowait postinstall runascurrentuser shellexec skipifsilent

#include "Dependencies\InnoSetup\iswin7\iswin7.iss"
#include "Dependencies\InnoSetup\products.iss"
#include "Dependencies\InnoSetup\products\stringversion.iss"
#include "Dependencies\InnoSetup\products\winversion.iss"
#include "Dependencies\InnoSetup\products\fileversion.iss"
#include "Dependencies\InnoSetup\products\dotnetfxversion.iss"
#include "Dependencies\InnoSetup\products\dotnetfx45.iss"

[Code]
function InitializeSetup(): boolean;
begin
	initwinversion();
	dotnetfx45();

	Result := true;
end;

procedure InitializeWizard();
begin
  iswin7_add_button(WizardForm.BackButton.Handle);
  iswin7_add_button(WizardForm.NextButton.Handle);
  iswin7_add_button(WizardForm.CancelButton.Handle);
  iswin7_add_glass(WizardForm.Handle, 0, 0, 0, 47, True);
end;

procedure DeinitializeSetup();
begin
  iswin7_free;
end;