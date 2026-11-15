@echo off
setlocal EnableExtensions
rem AutoPauseStealth build script (Beat Saber 1.44.1)
rem  - Builds Release with MSBuild (VS with MSBuild + .NET SDK component)
rem  - Writes build.log next to this file
rem  - The 7z archive release\AutoPauseStealth_v<ver>_bs<game>.7z is created by
rem    the SevenZipRelease target in AutoPauseStealth.csproj (Release builds)

set "ROOT=%~dp0"
set "PROJ=%ROOT%AutoPauseStealth\AutoPauseStealth.csproj"
set "LOG=%ROOT%build.log"

rem ---- locate MSBuild ----
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" (
  echo [ERROR] vswhere.exe not found: "%VSWHERE%"
  exit /b 1
)
set "MSBUILD="
for /f "usebackq delims=" %%i in (`"%VSWHERE%" -latest -products * -requires Microsoft.Component.MSBuild Microsoft.NetCore.Component.SDK -find MSBuild\**\Bin\MSBuild.exe`) do set "MSBUILD=%%i"
if not defined MSBUILD (
  echo [ERROR] MSBuild not found.
  exit /b 1
)
echo MSBuild: "%MSBUILD%"

rem ---- build (Release -> bin\Release + release\*.7z) ----
"%MSBUILD%" "%PROJ%" -t:Rebuild -p:Configuration=Release -v:m -nologo -flp:logfile="%LOG%";verbosity=normal;encoding=utf-8
if errorlevel 1 (
  echo [ERROR] Build failed. See "%LOG%"
  exit /b 1
)

echo.
echo Build OK. See "%ROOT%release"
endlocal
exit /b 0
