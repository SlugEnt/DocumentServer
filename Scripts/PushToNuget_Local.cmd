Echo Creates Debug Packages and pushes to Local Nuget Repo

rem #msbuild /t:pack /p:Configuration=Debug
dotnet pack -o ..\packages ..\src\DocumentServer.ClientLibrary

for %%n in (..\packages\*.nupkg) do  dotnet nuget push -s d:\a_dev\LocalNugetPackages "%%n"
