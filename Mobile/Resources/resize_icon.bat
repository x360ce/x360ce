@echo off
:: icon-256x256.png upscaled with waifu2x-caffe first.
:: 
:: Create icons.
set n=icon.png
set i=Input/%n%
set o=Output/android
CALL:RES   48   48 "%o%/mipmap-mdpi/%n%"
CALL:RES   72   72 "%o%/mipmap-hdpi/%n%"
CALL:RES   96   96 "%o%/mipmap-xhdpi/%n%"
CALL:RES  144  144 "%o%/mipmap-xxhdpi/%n%"
CALL:RES  192  192 "%o%/mipmap-xxxhdpi/%n%"
:: Create backgrounds.
set n=launcher_foreground.png
set i=Input/%n%
set o=Output/android
CALL:RES  108  108 "%o%/mipmap-mdpi/%n%"
CALL:RES  162  162 "%o%/mipmap-hdpi/%n%"
CALL:RES  216  216 "%o%/mipmap-xhdpi/%n%"
CALL:RES  324  324 "%o%/mipmap-xxhdpi/%n%"
CALL:RES  432  432 "%o%/mipmap-xxxhdpi/%n%"
pause
GOTO:EOF

:RES
SET rsz=%1x%1
IF %2 gtr %1 SET rsz=%2x%2
SET d=%~dp3
echo %1 %2 %3
IF NOT EXIST "%d%" MKDIR "%d%"
ImageMagick\convert.exe -resize %rsz% -gravity center -extent %1x%2 %i% "%d%%~nx3"
::-gravity center -extent %2x%3
:: -resize %2x%3^ -flatten
GOTO:EOF