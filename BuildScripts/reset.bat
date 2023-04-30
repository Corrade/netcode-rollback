:: Following on from duplicate_and_run.bat, this script deletes the copy of
:: the game and leaves the original folder empty, ready to house a new build
set src="..\Builds\main"

set src_copy="%src%_copy"

rmdir /s /q %src_copy%

cd /d %src%
for /F "delims=" %%i in ('dir /b') do (rmdir "%%i" /s/q || del "%%i" /s/q)
