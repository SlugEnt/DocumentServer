cd ../src/BundleBuilderSimple
dotnet ef migrations bundle --self-contained -r linux-x64 --output ../../Packages/Release/MigrationBundle_linux.exe --force
cd ../../Scripts