@echo off
REM ==========================================================================
REM build.bat - build NovaDemo และ ColorRecDemo เป็น .exe (PyInstaller --onefile)
REM             สำหรับ Windows
REM
REM ใช้งาน:
REM   build.bat            build ทั้ง 2 โปรแกรม
REM   build.bat --venv     สร้าง/ใช้ virtualenv .venv แล้ว build ในนั้น
REM   build.bat --clean    ลบ build dist *.spec ก่อน build
REM   build.bat NovaDemo   build เฉพาะตัวที่ระบุ (NovaDemo หรือ ColorRecDemo)
REM
REM ผลลัพธ์: dist\NovaDemo.exe , dist\ColorRecDemo.exe
REM ==========================================================================
setlocal enabledelayedexpansion
cd /d "%~dp0"

set USE_VENV=0
set DO_CLEAN=0
set ONLY=

:parse
if "%~1"=="" goto after_parse
if /I "%~1"=="--venv"  set USE_VENV=1
if /I "%~1"=="--clean" set DO_CLEAN=1
if /I "%~1"=="NovaDemo"     set ONLY=NovaDemo
if /I "%~1"=="ColorRecDemo" set ONLY=ColorRecDemo
shift
goto parse
:after_parse

REM ---- เลือก python ----
if "%PYTHON%"=="" set PYTHON=python
where %PYTHON% >nul 2>&1
if errorlevel 1 (
    echo ไม่พบ %PYTHON% - ติดตั้ง Python 3 ก่อน
    exit /b 1
)
for /f "delims=" %%v in ('%PYTHON% --version 2^>^&1') do echo + ใช้ Python: %%v

REM ---- virtualenv ----
if "%USE_VENV%"=="1" (
    if not exist ".venv" (
        echo + สร้าง virtualenv .venv
        %PYTHON% -m venv .venv
    )
    call ".venv\Scripts\activate.bat"
    set PYTHON=python
    echo + เปิดใช้ virtualenv
)

REM ---- ล้างของเก่า ----
if "%DO_CLEAN%"=="1" (
    echo + ล้าง build dist __pycache__ *.spec
    if exist build rmdir /s /q build
    if exist dist rmdir /s /q dist
    if exist __pycache__ rmdir /s /q __pycache__
    del /q *.spec 2>nul
)

REM ---- ติดตั้ง dependency ----
echo + ติดตั้ง dependency (requirements.txt + pyinstaller)
%PYTHON% -m pip install --upgrade pip >nul
%PYTHON% -m pip install -r requirements.txt
if errorlevel 1 exit /b 1
%PYTHON% -m pip install pyinstaller
if errorlevel 1 exit /b 1

if "%ONLY%"=="" (
    call :build_one NovaDemo NovaDemo.py "--hidden-import avon_protocol"
    if errorlevel 1 exit /b 1
    call :build_one ColorRecDemo ColorRecDemo.py ""
    if errorlevel 1 exit /b 1
) else (
    if "%ONLY%"=="NovaDemo"     call :build_one NovaDemo NovaDemo.py "--hidden-import avon_protocol"
    if "%ONLY%"=="ColorRecDemo" call :build_one ColorRecDemo ColorRecDemo.py ""
    if errorlevel 1 exit /b 1
)

echo.
echo ===================================================================
echo   เสร็จแล้ว - binary อยู่ในโฟลเดอร์ dist\
echo   รัน:  dist\NovaDemo.exe   และ   dist\ColorRecDemo.exe
echo   ครั้งแรกจะสร้าง ^<ชื่อ^>_config.json ไว้ข้าง exe (แก้ค่าแล้วรันใหม่)
echo ===================================================================
exit /b 0

REM ---- ฟังก์ชัน build 1 โปรแกรม: %1=ชื่อ %2=entry %3=extra args ----
:build_one
echo + เริ่ม build --onefile : %~1
%PYTHON% -m PyInstaller --onefile --windowed --clean --noconfirm --name "%~1" --hidden-import qt_compat --hidden-import LogLibrary --collect-submodules loguru %~3 "%~2"
if errorlevel 1 (
    echo   - build ล้มเหลว: %~1
    exit /b 1
)
if exist "dist\%~1.exe" (
    echo   สำเร็จ: dist\%~1.exe
) else (
    echo   - build ล้มเหลว: ไม่พบ dist\%~1.exe
    exit /b 1
)
exit /b 0
