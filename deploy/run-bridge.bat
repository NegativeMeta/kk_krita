@echo off
setlocal
set KR="C:\Program Files\Krita (x64)\bin\krita.exe"
set SCRIPT=%APPDATA%\krita\scripts\KoikatsuBridge.py
if not exist "%SCRIPT%" (
    echo Install KoikatsuBridge.py first
    pause
    exit /b
)
start "" %KR% --script "%SCRIPT%"
