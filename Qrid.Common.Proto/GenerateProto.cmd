DEL /Q /F /S "*.generated.cs"
cd ../../QRID
protoc.exe --csharp_out=Qrid.Common.Proto --csharp_opt=file_extension=.generated.cs,base_namespace=Qrid.Common.Proto @Qrid.Common.Proto\FileList.txt
pause