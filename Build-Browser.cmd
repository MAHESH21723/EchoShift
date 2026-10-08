@echo off
setlocal
cd /d "%~dp0"
if not exist "BuildReports" mkdir "BuildReports"
echo Checking Unity packages for compatibility...
"D:\UNITY\unity editor\6000.3.10f1\Editor\Unity.exe" -batchmode -ignoreCompilerErrors -projectPath "%~dp0." -executeMethod EchoShiftBootstrap.PackageRepair.Install -logFile "%~dp0BuildReports\packages.log"
if errorlevel 1 (
    echo Package repair failed. Read BuildReports\packages.log for the cause.
    pause
    exit /b 1
)
echo Building Echo Shift with Unity 6000.3.10f1.
echo First import and browser compilation can take several minutes.
echo Progress log: %~dp0BuildReports\build.log
"D:\UNITY\unity editor\6000.3.10f1\Editor\Unity.exe" -batchmode -projectPath "%~dp0." -buildTarget WebGL -executeMethod EchoShift.Editor.EchoBuild.BuildWeb -logFile "%~dp0BuildReports\build.log"
if errorlevel 1 (
    echo Build failed. Read BuildReports\build.log for the cause.
) else (
    echo Build completed. Run Play-Browser.cmd to play.
)
pause
