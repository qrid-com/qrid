using Google.Protobuf;
using Qrid.Common.Client.Cookie;
using Qrid.Common.Client.UI;
using Qrid.Common.Http;
using Qrid.Common.Proto;
using Qrid.Common.Proto.Crypto;
using Qrid.Common.Utils;
using Qrs.Client.Context;
using Qrs.App.PasswordManager.Client.Models;
using Qrs.App.PasswordManager.Client.Proto;
using Qrs.App.PasswordManager.Client.Wrappers;
using Qrs.Proto;
using Qrs.Proto.Api;
using Qrs.Proto.Extensions;
using Qrs.Tunnel.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CR = Qrid.Common.Proto.Crypto;
using CryptoUtils = Qrid.Common.Crypto.CryptoUtils;

namespace Qrs.App.PasswordManager.Client
{

    public class PwmContext : ClientContext
    {

        public const string APP_NAME = "PASSWORD MANAGER";
        public Folder Folder { get; set; }

        public PwmContext(IUserInteraction ui, IAsymKeyItemProvider asymKeyItemProvider, ICookieProvider cookieProvider) 
            : base(APP_NAME, ui, asymKeyItemProvider, cookieProvider)
        {
            Folder = new Folder("main",""); // seems like this Folder instance is discarded
        }

        public ResourceInfo ResourceCreate(Folder fi)
        {
            var res = new ResourceInfo();
            res.IsNew = true;
            res.Id = Guid.NewGuid().ToString();
            res.ResourceData = new ResourceData();
            res.ResourceData.Created = SharedUtils.GetTimestampNowTruncatedToMinutes();
            res.SecretWrapper = new SecretWrapper(APP_NAME, res);

            fi.Searched.AddResourceInfo(res, true); // add maintaining Items ordered

            return res;
        }

        public async Task<ResponseModel> ResourceOpenSecret(Folder fi, ResourceInfo ri)
        {
            var rins = new List<ResourceInfo>() { ri };
            return await ResourceOpenSecrets(fi, rins);
        }

        public async Task<ResponseModel> ResourceSave(Folder fi, ResourceInfo ri)
        {
            // TODO - create code that would set IsAssociatedDataModified = true when needed

            LogDebug("------- ResourceSave() - started - ---------------");
            LogDebug($"ri.IsNew={ri.IsNew}, ri.IsAssociatedDataModified={ri.IsAssociatedDataModified}, ri.IsSecretModified={ri.IsSecretModified}");

            var rd = ri.ResourceData;

            if (ri.IsNew)
            {
                // stamp creation date once, when the resource is saved for the first time
                rd.Created ??= SharedUtils.GetTimestampNowTruncatedToMinutes();

                // random padding on every save,
                // to avoid leaking even partial information about secret's length
                // if Secret is open (not null), do padding
                ri.Secret.Padding = "".PadRight(CryptoUtils.GetRandomInteger(0, 50), 'p');
            } 
            else
            {
                ri.SecretWrapper.QrsEnvelope = QrsEnvelope.Create(rd.SecretEnvelopeBytes);
                ri.SecretWrapper.IsModifiedPayload = ri.IsSecretModified;
                //ri.SecretWrapper.IsModifiedAssociatedData = ri.IsAssociatedDataModified; // existing code will ignore this and will independently try to determine if AD has been modified, so no point in setting it here
            }
            
            var response = ri.IsNew
                ? await WrapNewPayload(ri.SecretWrapper)
                : await WrapUpdatedPayload(ri.SecretWrapper);

            if (response.IsError())
                return response;

            rd.SecretEnvelopeBytes = ri.SecretWrapper.QrsEnvelope.ToByteString();

            // 2ND ENCRYPTION - with FOLDER KEY

            // encrypt ResourceData with FolderKey
            // and save it into QRS2 file
            // TODO - should we use rd.ResourceId (or ri.Id aka file name) as AssociatedData here?
            var seb = CryptoUtils.SymEncrypt(ri.ResourceData, fi.FolderKey, ProtoBase.Null);
            ri.QrsFile ??= Path.Combine(fi.FolderPath, ri.QrsFileName);
            var sebByteArray = seb.GetByteArray();
            await File.WriteAllBytesAsync(ri.QrsFile, sebByteArray);
            LogDebug($"Wrote {sebByteArray.Length} bytes into FILE: {ri.QrsFile}");

            ri.IsNew = false;
            ri.IsModified = false;
            ri.IsSecretModified = false;
            ri.IsAssociatedDataModified = false;

            return SuccessResponse();
        }

        public async Task<string> ResourceDelete(Folder fi, ResourceInfo ri)
        {
            // delete corresponding file

            ri.QrsFile ??= Path.Combine(fi.FolderPath, ri.QrsFileName);
            if (File.Exists(ri.QrsFile))
            {
                try
                {
                    File.Delete(ri.QrsFile);
                }
                catch (Exception e)
                {
                    return e.Message;
                }
            }

            // delete RI from memory

            if (!fi.Searched.ExistsId(ri.Id)) { return string.Empty; }

            fi.Searched.Delete(ri.Id);
            

            return string.Empty;
        }

        public AssociatedDataDek GetBlankAssociatedData()
        {
            var res = new AssociatedDataDek();
            res.KekName = string.Empty;
            res.AppName = AppName;
            return res;
        }

        public async Task CreateNewFolderKey(Folder res, string folderKeyFile)
        {
            // new FolderKey
            res.FolderKeyWrapper.Payload = CryptoUtils.GetRandomSymKey();

            // wrap FolderKey via QRS app
            var response = await WrapNewPayload(res.FolderKeyWrapper);
            if (response.IsError())
            {
                res.ErrorMessage = response.Message;
                return;
            }
            
            // save to file
            await File.WriteAllBytesAsync(folderKeyFile, res.FolderKeyEnvelope.ToByteArray());
        }

        public async Task SaveExistingFolderKey(Folder res, string folderKeyFile)
        {
            // must be called shortly after LoadExistingFolderKey(...)
            // b/c it uses the objects created by LoadExistingFolderKey(...)

            // wrap FolderKey via QRS app
            var response = await WrapNewPayload(res.FolderKeyWrapper);
            if (!response.IsSuccess())
            {
                res.ErrorMessage = response.Message;
                return;
            }

            // save to file (overwriting the "folder_key.qrs1" file!!!)
            await File.WriteAllBytesAsync(folderKeyFile, res.FolderKeyEnvelope.ToByteArray());
        }

        public async Task LoadExistingFolderKey(Folder res, string folderKeyFile, bool validateAssociatedData) 
        {
            // read from file into wrapper's envelope
            res.FolderKeyWrapper.QrsEnvelope = QrsEnvelope.Create(File.ReadAllBytes(folderKeyFile));
            // unwrap envelope
            var response = await UnwrapPayload(res.FolderKeyWrapper);
            if (response.IsError())
                res.ErrorMessage = response.Message;
        }

        public async Task<Folder> FolderOpen(string folderPath, string keywords = "", SortBy sortBy = SortBy.NameAsc, string appRootPath = "")
        {
            var res = new Folder(folderPath, appRootPath);
            
            // 1) FOLDER KEY - load and decrypt FolderKey from "folder_key.qrs1" file
            var folderKeyFile = res.GetFolderKeyFileName();

            if (File.Exists(folderKeyFile))
            {
                // unwrap existing FolderKey
                await LoadExistingFolderKey(res, folderKeyFile, true);
            }
            else
            {
                // create new FolderKey, wrap it and save it 
                await CreateNewFolderKey(res, folderKeyFile);
            }

            if (res.IsError) 
                return res; 

            // 2) RESOURCE FILES

            res.Searched = new SearchedList();
            var files = System.IO.Directory.EnumerateFiles(res.FolderPath, "*.qrs2");
            foreach( var file in files)
            {
                var seb = SymEncryptedBytes.Create(await System.IO.File.ReadAllBytesAsync(file));
                var rdBytes = CryptoUtils.SymDecrypt(seb, res.FolderKey, null); // TODO - what would make sense to be used as AD here?
                var ri = new ResourceInfo();
                ri.QrsFile = file;
                ri.SetIdFromQrsFileName(Path.GetFileName(file)); // remove ".qrs2" from the Id
                ri.ResourceData = ResourceData.Create(rdBytes);
                res.Searched.AddResourceInfo(ri); // add unordered to avoid unnecessary search operations
            }

            // sort loaded items
            res.Searched.Criteria.Keywords = keywords;
            res.Searched.Criteria.SortBy = sortBy;
            res.Searched.Sort();

            return res;
        }

        public string ResourceOpenForMigration(Folder fi, ResourceInfo ri, CryptoKey unwrappedDek)
        {
            // should be used for Migration only
            // b/c we do NOT validate AD here

            // deserialize Secret's Envelope
            ri.SecretWrapper.QrsEnvelope = QrsEnvelope.Create(ri.ResourceData.SecretEnvelopeBytes); // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            var rd = ri.ResourceData;

            // use DEK to decrypt and create Secret object
            ri.SecretWrapper.DEK = unwrappedDek; // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            var secretBytes = CryptoUtils.SymDecrypt(ri.SecretEnvelope.EncryptedPayload, ri.SecretDek, null);
            ri.SecretWrapper.Payload = Secret.Create(secretBytes); // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            ri.IsSecretOpen = true;
            return string.Empty;
        }

        public async Task<string> ResourceSaveForMigration(Folder fi, ResourceInfo ri)
        {
            
            LogDebug("------- ResourceSaveForMigration() - started - ---------------");

            // encrypt ResourceData with FolderKey
            // and save it into QRS2 file
            
            var seb = CryptoUtils.SymEncrypt(ri.ResourceData, fi.FolderKey, ProtoBase.Null);
            ri.QrsFile ??= Path.Combine(fi.FolderPath, ri.QrsFileName);
            var sebByteArray = seb.GetByteArray();
            await File.WriteAllBytesAsync(ri.QrsFile, sebByteArray);
            LogDebug($"Wrote {sebByteArray.Length} bytes into FILE: {ri.QrsFile}");
            

            return string.Empty;
        }

        #region Export/Import

        public ExportedResource GetExportedResource(ResourceInfo ri)
        {
            var res = new ExportedResource();

            res.QrsFileName = ri.QrsFileName;
            res.ResourceId = ri.ResourceData.ResourceId;
            res.ResourceType = ri.ResourceData.ResourceType;
            res.ResourceName = ri.ResourceData.ResourceName;
            res.Username = ri.ResourceData.Username;
            res.ResourceNotes = ri.ResourceData.ResourceNotes;
            res.FilePath = ri.ResourceData.FilePath;
            res.Created = ri.ResourceData.Created;

            // secret
            res.SecretPassword = ri.Secret.Password;
            res.SecretNotes = ri.Secret.SecretNotes;
            res.SecretFileContent = ri.Secret.FileContent;
            res.SecretPadding = ri.Secret.Padding;

            return res;
        }

        public ResourceInfo GetResourceInfo(ExportedResource er)
        {
            var ri = new ResourceInfo();
            var rd = new ResourceData();

            ri.ResourceData = rd;
            ri.SetIdFromQrsFileName(er.QrsFileName);
            
            rd.ResourceId = er.ResourceId;
            rd.ResourceType = er.ResourceType;
            rd.ResourceName = er.ResourceName;
            rd.Username = er.Username;
            rd.ResourceNotes = er.ResourceNotes;
            rd.FilePath = er.FilePath;
            rd.Created = er.Created;

            // secret
            ri.SecretWrapper = new SecretWrapper(APP_NAME, ri);
            ri.SecretWrapper.CreatePayload();
            ri.Secret.Password = er.SecretPassword;
            ri.Secret.SecretNotes = er.SecretNotes;
            ri.Secret.FileContent = er.SecretFileContent;
            ri.Secret.Padding = er.SecretPadding;

            return ri;
        }

        public bool IsEqual(ResourceInfo ri1, ResourceInfo ri2)
        {
            var rd1 = ri1.ResourceData;
            var rd2 = ri2.ResourceData;

            var x1 = rd1.ResourceId == rd2.ResourceId &&
            rd1.ResourceType == rd2.ResourceType &&
            rd1.ResourceName == rd2.ResourceName &&
            rd1.Username == rd2.Username &&
            rd1.ResourceNotes == rd2.ResourceNotes &&
            rd1.FilePath == rd2.FilePath &&
            rd1.Created == rd2.Created;

            // secret
            var x2 = ri1.Secret.Password == ri2.Secret.Password &&
            ri1.Secret.SecretNotes == ri2.Secret.SecretNotes &&
            ri1.Secret.FileContent == ri2.Secret.FileContent;

            var x3 = ri1.Id == ri2.Id;
            return x1 && x2 && x3;
        }

        public async Task<ResponseModel> ExportFolder(string exportFilePath, string exportFilePassword)
        {
            return await ExportList(Folder.Searched.SourceAsResourceInfos, Folder.FolderPath, exportFilePath, exportFilePassword);
        }

        public async Task<ResponseModel> ExportList(List<ResourceInfo> rins, string folderPath, string exportFilePath, string exportFilePassword)
        {
            exportFilePath = FileUtils.RemoveQuotes(exportFilePath);

            // 1) make sure that all secrets are open
            foreach (var ri in rins)
                if (ri.IsSecretOpen == false || ri.Secret == null)
                    return ErrorResponse($"Cannot export b/c secret is NOT open for resource='{ri.ResourceData.ResourceName}', username='{ri.ResourceData.Username}'");

            // 2) loop through resources and prepare ExportedData object
            var exportedData = new ExportedData();
            foreach(var kv in Folder.Searched.SourceItems)
                exportedData.Resources.Add(GetExportedResource(kv.Value.ResourceInfo));

            // 3) password-encrypt ExportedData object into ExportFile object
            var exportFile = new ExportFile();
            exportFile.EncryptedData = CryptoUtils.PasswordEncrypt(exportedData.ToByteArray(), exportFilePassword).ToByteString();
            exportFile.Created = SharedUtils.GetTimestampNowTruncatedToMinutes();
            exportFile.FolderPath = folderPath;

            // 4) save into export file
            await File.WriteAllBytesAsync(exportFilePath, exportFile.ToByteArray());

            return SuccessResponse();
        }

        public async Task<ResponseModel> ImportFolder(string exportFilePath, string exportFilePassword, string appRootPath)
        {
            exportFilePath = FileUtils.RemoveQuotes(exportFilePath);

            var fileBytes = await File.ReadAllBytesAsync(exportFilePath);
            var exportFile = ExportFile.Create(fileBytes);

            var exportedDataBytes = CryptoUtils.PasswordDecrypt(PasswordEncryptedBytes.Create(exportFile.EncryptedData), exportFilePassword, null);
            var exportedData = ExportedData.Create(exportedDataBytes);

            //var newFolder = new Folder(exportFile.FolderPath, appRootPath);
            //newFolder.Searched = new SearchedList();

            // load resources into newFolder object
            int countFound = 0;
            int countNotFound = 0;
            int countEqual = 0;
            int countDifferent = 0;
            foreach (var er in exportedData.Resources)
            {
                var ri = GetResourceInfo(er);
                Folder.Searched.AddResourceInfo(ri);
                
                // COMPARE DATA - for now, just compare that imported data are matching with current data
                if (Folder.Searched.SourceItems.ContainsKey(ri.Id))
                {
                    countFound++;
                    var ri2 = Folder.Searched.SourceItems[ri.Id].ResourceInfo;
                    if (IsEqual(ri,ri2))
                        countEqual++;
                    else 
                        countDifferent++;
                }
                else
                    countNotFound++;
            }


            
            // new folder key - generate, wrap and save into file: 
            await CreateNewFolderKey(Folder, Folder.GetFolderKeyFileName());

            // generate DEK keys and encrypt Secrets
            
            // call QRS to bulk-WRAP keys
            var rins = Folder.Searched.SourceAsResourceInfos;
            var wrappers = rins.Select(ri => { ri.SecretWrapper.QrsEnvelope = QrsEnvelope.Create(ri.ResourceData.SecretEnvelopeBytes); return ri.SecretWrapper; }).ToList();
            var response = await WrapNewPayload(wrappers);
            if (response.IsError())
                return response;

            // save QrsEnvelopes everything into qrs2 files
            foreach(var ri in rins)
            {
                ri.ResourceData.SecretEnvelopeBytes = ri.SecretWrapper.QrsEnvelope.ToByteString();
                await ResourceSaveForMigration(Folder, ri);
            }

            return SuccessResponse($"Import results - Found:{countFound}, Not Found:{countNotFound}, Equal{countEqual}:, Different:{countDifferent}");

        }


        public async Task<ResponseModel> ResourceOpenAllSecrets(Folder folder)
        {
            return await ResourceOpenSecrets(folder, folder.Searched.SourceAsResourceInfos);
        }

        public async Task<ResponseModel> ResourceOpenSecrets(Folder folder, List<ResourceInfo> rins, bool validateAssociatedData = true)
        {
            var wrappers = rins.Select(ri => { ri.SecretWrapper.QrsEnvelope = QrsEnvelope.Create(ri.ResourceData.SecretEnvelopeBytes); return ri.SecretWrapper; }).ToList();
            var response = await UnwrapPayload(wrappers);
            if (response.IsError())
                return response;

            // some extra validation and set IsSecretOpen
            foreach (var ri in rins)
            {
                if (ri.SecretWrapper.Status.IsSuccess())
                {
                    if (!ri.Secret.IsValidPadding())
                        throw new ApplicationException($"Detected unexpected invalid Padding = '{ri.Secret.Padding}'");
                    ri.IsSecretOpen = true;
                }
                else
                {
                    // btw this should not happen b/c when it happens then overall response.IsError() == true so it does not come here and exits earlier
                    ri.IsSecretOpen = false;
                }
            }
            return SuccessResponse();
        }

        #endregion
    }
}
