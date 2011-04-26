!define APP_NAME "RS TV Show Tracker"
!define COMP_NAME "RoliSoft"
!define WEB_SITE "http://lab.rolisoft.net"
!define VERSION "2.0.0.0"
!define COPYRIGHT "© 2011 RoliSoft"
!define DESCRIPTION "RS TV Show Tracker"
!define INSTALLER_NAME "tvshowtracker_v2_setup.exe"
!define MAIN_APP_EXE "RSTVShowTracker.exe"
!define INSTALL_TYPE "SetShellVarContext current"
!define REG_ROOT "HKCU"
!define REG_APP_PATH "Software\Microsoft\Windows\CurrentVersion\App Paths\${MAIN_APP_EXE}"
!define UNINSTALL_PATH "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}"

!define MUI_ICON "${NSISDIR}\contrib\graphics\icons\orange-install.ico"
!define MUI_UNICON "${NSISDIR}\contrib\graphics\icons\orange-uninstall.ico"
!define MUI_HEADERIMAGE on
!define MUI_HEADERIMAGE_RIGHT on
!define MUI_HEADERIMAGE_BITMAP "Images\nsis-header.bmp"
!define MUI_HEADERIMAGE_UNBITMAP "${NSISDIR}\contrib\graphics\header\orange-uninstall.bmp"
!define MUI_WELCOMEFINISHPAGE_BITMAP "Images\nsis-wizard.bmp"
!define MUI_UNWELCOMEFINISHPAGE_BITMAP "${NSISDIR}\contrib\graphics\wizard\orange-uninstall.bmp"

!define REG_START_MENU "Start Menu Folder"

var SM_Folder

######################################################################

VIProductVersion  "${VERSION}"
VIAddVersionKey "ProductName"  "${APP_NAME}"
VIAddVersionKey "CompanyName"  "${COMP_NAME}"
VIAddVersionKey "LegalCopyright"  "${COPYRIGHT}"
VIAddVersionKey "FileDescription"  "${DESCRIPTION}"
VIAddVersionKey "FileVersion"  "${VERSION}"

######################################################################

SetCompressor LZMA
Name "${APP_NAME}"
Caption "${APP_NAME}"
OutFile "${INSTALLER_NAME}"
BrandingText "${APP_NAME}"
XPStyle on
InstallDirRegKey "${REG_ROOT}" "${REG_APP_PATH}" ""
InstallDir "$PROGRAMFILES\RoliSoft\RS TV Show Tracker"

######################################################################

!include "MUI.nsh"

!define MUI_ABORTWARNING
!define MUI_UNABORTWARNING

!insertmacro MUI_PAGE_WELCOME

!ifdef LICENSE_TXT
!insertmacro MUI_PAGE_LICENSE "${LICENSE_TXT}"
!endif

!insertmacro MUI_PAGE_DIRECTORY

!ifdef REG_START_MENU
!define MUI_STARTMENUPAGE_DEFAULTFOLDER "RS TV Show Tracker"
!define MUI_STARTMENUPAGE_REGISTRY_ROOT "${REG_ROOT}"
!define MUI_STARTMENUPAGE_REGISTRY_KEY "${UNINSTALL_PATH}"
!define MUI_STARTMENUPAGE_REGISTRY_VALUENAME "${REG_START_MENU}"
!insertmacro MUI_PAGE_STARTMENU Application $SM_Folder
!endif

!insertmacro MUI_PAGE_INSTFILES

!define MUI_FINISHPAGE_RUN "$INSTDIR\${MAIN_APP_EXE}"
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM

!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

######################################################################

Section -MainProgram
${INSTALL_TYPE}
SetOverwrite ifnewer
SetOutPath "$INSTDIR"
File "bin\Release\CookComputing.XmlRpcV2.dll"
File "bin\Release\handle.exe"
File "bin\Release\HtmlAgilityPack.dll"
File "bin\Release\Ionic.Zip.Reduced.dll"
File "bin\Release\Microsoft.Windows.Shell.dll"
File "bin\Release\Microsoft.WindowsAPICodePack.dll"
File "bin\Release\Microsoft.WindowsAPICodePack.Shell.dll"
File "bin\Release\Newtonsoft.Json.dll"
File "bin\Release\nunit.framework.dll"
File "bin\Release\RSTVShowTracker.exe"
File "bin\Release\RSTVShowTracker.exe.config"
File "bin\Release\RSTVShowTracker.pdb"
File "bin\Release\System.Data.SQLite.dll"
File "bin\Release\Transitionals.dll"
File "bin\Release\VistaControls.dll"
SectionEnd

######################################################################

Section -Icons_Reg
SetOutPath "$INSTDIR"
WriteUninstaller "$INSTDIR\uninstall.exe"

!ifdef REG_START_MENU
!insertmacro MUI_STARTMENU_WRITE_BEGIN Application
CreateDirectory "$SMPROGRAMS\$SM_Folder"
CreateShortCut "$SMPROGRAMS\$SM_Folder\${APP_NAME}.lnk" "$INSTDIR\${MAIN_APP_EXE}"
CreateShortCut "$DESKTOP\${APP_NAME}.lnk" "$INSTDIR\${MAIN_APP_EXE}"
CreateShortCut "$SMPROGRAMS\$SM_Folder\Uninstall.lnk" "$INSTDIR\uninstall.exe"

!ifdef WEB_SITE
WriteIniStr "$INSTDIR\${APP_NAME} website.url" "InternetShortcut" "URL" "${WEB_SITE}"
CreateShortCut "$SMPROGRAMS\$SM_Folder\Website.lnk" "$INSTDIR\${APP_NAME} website.url"
!endif
!insertmacro MUI_STARTMENU_WRITE_END
!endif

!ifndef REG_START_MENU
CreateDirectory "$SMPROGRAMS\RS TV Show Tracker"
CreateShortCut "$SMPROGRAMS\RS TV Show Tracker\${APP_NAME}.lnk" "$INSTDIR\${MAIN_APP_EXE}"
CreateShortCut "$DESKTOP\${APP_NAME}.lnk" "$INSTDIR\${MAIN_APP_EXE}"
CreateShortCut "$SMPROGRAMS\RS TV Show Tracker\Uninstall.lnk" "$INSTDIR\uninstall.exe"

!ifdef WEB_SITE
WriteIniStr "$INSTDIR\${APP_NAME} website.url" "InternetShortcut" "URL" "${WEB_SITE}"
CreateShortCut "$SMPROGRAMS\RS TV Show Tracker\Website.lnk" "$INSTDIR\${APP_NAME} website.url"
!endif
!endif

WriteRegStr ${REG_ROOT} "${REG_APP_PATH}" "" "$INSTDIR\${MAIN_APP_EXE}"
WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "DisplayName" "${APP_NAME}"
WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "UninstallString" "$INSTDIR\uninstall.exe"
WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "DisplayIcon" "$INSTDIR\${MAIN_APP_EXE}"
WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "DisplayVersion" "${VERSION}"
WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "Publisher" "${COMP_NAME}"

!ifdef WEB_SITE
WriteRegStr ${REG_ROOT} "${UNINSTALL_PATH}"  "URLInfoAbout" "${WEB_SITE}"
!endif
SectionEnd

######################################################################

Section Uninstall
${INSTALL_TYPE}
Delete "$INSTDIR\CookComputing.XmlRpcV2.dll"
Delete "$INSTDIR\handle.exe"
Delete "$INSTDIR\HtmlAgilityPack.dll"
Delete "$INSTDIR\Ionic.Zip.Reduced.dll"
Delete "$INSTDIR\Microsoft.Windows.Shell.dll"
Delete "$INSTDIR\Microsoft.WindowsAPICodePack.dll"
Delete "$INSTDIR\Microsoft.WindowsAPICodePack.Shell.dll"
Delete "$INSTDIR\Newtonsoft.Json.dll"
Delete "$INSTDIR\nunit.framework.dll"
Delete "$INSTDIR\RSTVShowTracker.exe"
Delete "$INSTDIR\RSTVShowTracker.exe.config"
Delete "$INSTDIR\RSTVShowTracker.pdb"
Delete "$INSTDIR\System.Data.SQLite.dll"
Delete "$INSTDIR\Transitionals.dll"
Delete "$INSTDIR\VistaControls.dll"
Delete "$INSTDIR\uninstall.exe"
!ifdef WEB_SITE
Delete "$INSTDIR\${APP_NAME} website.url"
!endif

RmDir "$INSTDIR"

!ifdef REG_START_MENU
!insertmacro MUI_STARTMENU_GETFOLDER "Application" $SM_Folder
Delete "$SMPROGRAMS\$SM_Folder\${APP_NAME}.lnk"
Delete "$SMPROGRAMS\$SM_Folder\Uninstall.lnk"
!ifdef WEB_SITE
Delete "$SMPROGRAMS\$SM_Folder\Website.lnk"
!endif
Delete "$DESKTOP\${APP_NAME}.lnk"

RmDir "$SMPROGRAMS\$SM_Folder"
!endif

!ifndef REG_START_MENU
Delete "$SMPROGRAMS\RS TV Show Tracker\${APP_NAME}.lnk"
Delete "$SMPROGRAMS\RS TV Show Tracker\Uninstall.lnk"
!ifdef WEB_SITE
Delete "$SMPROGRAMS\RS TV Show Tracker\Website.lnk"
!endif
Delete "$DESKTOP\${APP_NAME}.lnk"

RmDir "$SMPROGRAMS\RS TV Show Tracker"
!endif

DeleteRegKey ${REG_ROOT} "${REG_APP_PATH}"
DeleteRegKey ${REG_ROOT} "${UNINSTALL_PATH}"
SectionEnd