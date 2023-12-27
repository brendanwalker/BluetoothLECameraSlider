@echo off

SET COM_PORT=%1
if "%1" == "" (
  echo "Usage: upload.bat <com port>"
  echo "ex: upload.bat COM7"
  goto failure
)

set BUILD_PATH=%~dp0\..\build
set CAMERA_SLIDER_BOOTLOADER_BIN=%BUILD_PATH%\ESP32_ArtNet_CameraControl.ino.bootloader.bin
set CAMERA_SLIDER_PARTITIONS_BIN=%BUILD_PATH%\ESP32_ArtNet_CameraControl.ino.partitions.bin
set CAMERA_SLIDER_BIN=%BUILD_PATH%\ESP32_ArtNet_CameraControl.ino.bin
set ESP_BOOT_BIN=boot_app0.bin

esptool.exe --chip esp32 --port %COM_PORT% --baud 921600 --before default_reset --after hard_reset write_flash -z --flash_mode dio --flash_freq 80m --flash_size 4MB 0x1000 %CAMERA_SLIDER_BOOTLOADER_BIN% 0x8000 %CAMERA_SLIDER_PARTITIONS_BIN% 0xe000 %ESP_BOOT_BIN% 0x10000 %CAMERA_SLIDER_BIN%
IF %ERRORLEVEL% NEQ 0 (
  echo "Error uploading firmware"
  goto failure
)


EXIT \B 0

:failure
pause
EXIT \B 1