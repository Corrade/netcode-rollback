:: Copies the game (src) and then runs both instances
set src="..\Builds\main"

set src_copy="%src%_copy"

robocopy %src% %src_copy% /E

start /d %src% Rollback.exe
start /d %src_copy% Rollback.exe
