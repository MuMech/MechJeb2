
IF NOT DEFINED PDB2MDB echo PDB2MDB env variable not defined
IF NOT DEFINED KSPDIR echo KSPDIR env variable not defined

SET TargetPath=%1
SET TargetDir=%2
SET TargetName=%3

echo %TargetPath% %TargetDir% %TargetName%

IF EXIST %PDB2MDB% ( 
	%PDB2MDB% %TargetPath%
) ELSE (
	echo Unable to find %PDB2MDB%
)

IF NOT EXIST %KSPDIR%\* (
	echo Expected "%KSPDIR%" to point to a directory but it is not
	exit 0
)

IF NOT EXIST %KSPDIR%\GameData\MechJeb2\Plugins\* (
	echo Expected "%KSPDIR%" to contain a GameData\MechJeb2\Plugins subdirectory but it does not'
	exit 1
)

echo Copying to "%KSPDIR%"
IF EXIST "%TargetPath%" xcopy /Y "%TargetPath%" "%KSPDIR%\GameData\MechJeb2\Plugins\"
IF EXIST "%TargetDir%%TargetName%.pdb" xcopy /Y "%TargetDir%%TargetName%.pdb" "%KSPDIR%\GameData\MechJeb2\Plugins\"
IF EXIST "%TargetDir%%TargetName%.dll.mdb" xcopy /Y "%TargetDir%%TargetName%.dll.mdb" "%KSPDIR%\GameData\MechJeb2\Plugins\"


