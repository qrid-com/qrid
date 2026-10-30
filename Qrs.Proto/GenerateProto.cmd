DEL /Q /F /S "*.generated.cs"
cd ../../QRID
protoc.exe --csharp_out=Qrs.Proto --csharp_opt=file_extension=.generated.cs,base_namespace=Qrs.Proto @Qrs.Proto\FileList.txt
pause