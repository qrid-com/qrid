using Org.BouncyCastle.Utilities;
using Qrs.App.FileManager.Client.Payload;
using Qrs.Proto;
using System;
using System.IO;
using System.Threading.Tasks;
using CryptoUtils = Qrid.Common.Crypto.CryptoUtils;

namespace Qrs.App.FileManager.Client
{
    public class FmContext
    {
        Qrs.Client.Context.ClientContext Context;
        public FmContext(Qrs.Client.Context.ClientContext context)
        {
            Context = context;
        }

        public QrsFileWrapper FileNew(string fileName)
        {
            var res = new QrsFileWrapper(Context);
            res.IsNew = true;
            res.FileName = fileName;
            return res;
        }

        public async Task FileSave(QrsFileWrapper qfw)
        {
            // this will throw if invalid file path
            var fi = new System.IO.FileInfo(qfw.File);
            if (qfw.IsNew && fi.Exists)
                throw new ApplicationException($"attempt to overwrite already existing file: {qfw.File}");

            // WRAP PAYLOAD
            var response = qfw.IsNew
                ? await Context.WrapNewPayload(qfw)
                : await Context.WrapUpdatedPayload(qfw);

            if (response.IsError())
            {
                qfw.ErrorMessage = response.Message;
                Context.UI.ShowError(qfw.ErrorMessage);
                return;
            }

            // Write into QRS file
            await System.IO.File.WriteAllBytesAsync(qfw.File, qfw.QrsEnvelope.GetByteArray());

            qfw.IsNew = false;
            qfw.IsModified = false;
            qfw.IsAssociatedDataModified = false;
        }

        public async Task<QrsFileWrapper> FileOpen(string file)
        {
            var res = new QrsFileWrapper(Context);

            // TODO - handle non existing file, no access to file, etc
            var fi = new FileInfo(file);
            res.File = file;
            res.FileName = fi.Name;

            res.QrsEnvelope = QrsEnvelope.Create(System.IO.File.ReadAllBytes(file));

            // UNWRAP PAYLOAD
            var response = await Context.UnwrapPayload(res);
            if (response.IsError())
            {
                res.ErrorMessage = response.Message;
                Context.UI.ShowError(res.ErrorMessage);
                return res;
            }

            // Load decrypted file content into .Content property
            // TODO - verify that response.Data.MessageType == "mime:text/plain;encoding:utf8"
            res.Content = res.Payload.Value;
            return res;
        }

    }
}
