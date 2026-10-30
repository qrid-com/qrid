DEL /Q /F /S "*.generated.cs"
cd ../../QRID
protoc.exe --csharp_out=Qrs.App.PasswordManager.Client --csharp_opt=file_extension=.generated.cs,base_namespace=Qrs.App.PasswordManager.Client @Qrs.App.PasswordManager.Client\FileList.txt
pause