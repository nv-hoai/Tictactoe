@echo off
echo Starting Distributed Tic-Tac-Toe System...

:: Create log directories if they don't exist
if not exist "logs" mkdir logs

:: Start the main server
start "Tic-Tac-Toe Main Server" cmd /k "dotnet run --project TictactoeServer\TictactoeServer.csproj > logs\server.log"

:: Wait for the server to initialize
timeout /t 5

:: Start two worker servers with different roles and ports
start "Tic-Tac-Toe Worker (Logic)" cmd /k "dotnet run --project TictactoeWorker\TictactoeWorker.csproj -- --role Logic --port 6000 --autoregister --server localhost --serverport 5000 > logs\worker_logic.log"
start "Tic-Tac-Toe Worker (AI)" cmd /k "dotnet run --project TictactoeWorker\TictactoeWorker.csproj -- --role AI --port 6001 --autoregister --server localhost --serverport 5000 > logs\worker_ai.log"

:: Inform the user
echo.
echo Tic-Tac-Toe distributed system started:
echo   - Main Server: http://localhost:5000
echo   - Logic Worker: running on port 6000
echo   - AI Worker: running on port 6001
echo.
echo Log files are being saved to the logs directory.
echo.
echo To stop the system, close all command windows or press Ctrl+C in each one.
echo.