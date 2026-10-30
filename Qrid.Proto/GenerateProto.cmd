DEL /Q /F /S "*.generated.cs"
cd ../../QRID
protoc.exe --csharp_out=Qrid.Proto --csharp_opt=file_extension=.generated.cs,base_namespace=Qrid.Proto @Qrid.Proto\FileList.txt
pause