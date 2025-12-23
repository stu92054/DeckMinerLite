@echo off
REM DeckMinerLite 啟動腳本
REM 使用可攜式 .NET SDK 執行

set DOTNET_EXE=D:\SukuShow-Deck-Miner\Portable\dotnet-sdk-10.0.101-win-x64\dotnet.exe

REM 傳遞所有參數給 dotnet run
"%DOTNET_EXE%" run -- %*
