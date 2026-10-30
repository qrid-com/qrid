DEL /Q /F /S "*.generated.cs"
cd ../../QRID
protoc.exe --csharp_out=Qrs.App.Vault.Client --csharp_opt=file_extension=.generated.cs,base_namespace=Qrs.App.Vault.Client @Qrs.App.Vault.Client\FileList.txt
pause