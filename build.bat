@echo off
REM DeckMinerLite 編譯腳本
REM 使用可攜式 .NET SDK 編譯

set DOTNET_EXE=D:\SukuShow-Deck-Miner\Portable\dotnet-sdk-10.0.101-win-x64\dotnet.exe

echo 正在編譯 DeckMinerLite...
"%DOTNET_EXE%" build %*
