using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using System.Data;
using Serilog;
using Dapper;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;

public class AttachmentRepository : IAttachmentRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;

    public AttachmentRepository(IDbConnection connection, IDbTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<Attachment?> GetLinkedAttachment(Guid applicationId, Guid attachmentId, Guid linkId, LinkType linkType)
    {
        try
        {
            const string query = @"
                SELECT A.AttachmentId, A.FileName, A.BlobId, A.DirectoryPath, A.FileMIMEtype, A.FileSize,
                    A.CreatedDate, A.ModifiedDate, A.CreatedByUpn, A.ModifiedByUpn
                FROM [recognitionCitizen].[Attachment] A
                INNER JOIN [recognitionCitizen].[AttachmentLink] AL
                    ON A.AttachmentId = AL.AttachmentId
                WHERE A.AttachmentId = @AttachmentId
                AND AL.LinkId = @LinkId
                AND AL.LinkTypeId = @LinkTypeId
                AND AL.ApplicationId = @ApplicationId;";

            return await _connection.QueryFirstOrDefaultAsync<Attachment>(query, new
            {
                AttachmentId = attachmentId,
                LinkId = linkId,
                LinkTypeId = (int)linkType,
                ApplicationId = applicationId
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "Error retrieving linked attachment for AttachmentId {AttachmentId}, LinkId {LinkId}, LinkType {LinkType}, ApplicationId {ApplicationId}",
                attachmentId, linkId, linkType, applicationId);
            return null;
        }
    }

    public async Task<IEnumerable<Attachment>> GetAllAttachmentsForLink(Guid applicationId, Guid linkId, LinkType linkType)
    {
        try
        {
            const string query = @"
                SELECT A.AttachmentId, A.FileName, A.BlobId, A.DirectoryPath, A.FileMIMEtype, A.FileSize,
                    A.CreatedDate, A.ModifiedDate, A.CreatedByUpn, A.ModifiedByUpn
                FROM [recognitionCitizen].[Attachment] A
                INNER JOIN [recognitionCitizen].[AttachmentLink] AL
                    ON A.AttachmentId = AL.AttachmentId
                WHERE AL.LinkId = @LinkId
                AND AL.LinkTypeId = @LinkTypeId
                AND AL.ApplicationId = @ApplicationId;";

            return await _connection.QueryAsync<Attachment>(query, new
            {
                LinkId = linkId,
                LinkTypeId = (int)linkType,
                ApplicationId = applicationId
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving attachments for LinkId {LinkId}, LinkType {LinkType}, ApplicationId {ApplicationId}", linkId, linkType, applicationId);
            return Enumerable.Empty<Attachment>();
        }
    }

    public async Task<Attachment?> CreateAttachment(string fileName, string contentType, long size)
    {
        try
        {
            const string query = @"
                INSERT INTO [recognitionCitizen].[Attachment] (
                    FileName,
                    FileMIMEtype,
                    FileSize,
                    CreatedByUpn,
                    ModifiedByUpn
                )
                OUTPUT INSERTED.*
                VALUES (
                    @FileName,
                    @FileMIMEtype,
                    @FileSize,
                    @CreatedByUpn,
                    @ModifiedByUpn
                );";

            return await _connection.QuerySingleAsync<Attachment>(query, new
            {
                FileName = fileName,
                FileMIMEtype = contentType,
                FileSize = size,
                CreatedByUpn = "USER",      // TODO: replace once auth gets added
                ModifiedByUpn = "USER"      // TODO: replace once auth gets added
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error inserting attachment with FileName {FileName}", fileName);
            return null!;
        }
    }

    public async Task<bool> CreateAttachmentLink(Guid applicationId, Guid attachmentId, Guid linkId, LinkType linkTypeId)
    {
        try
        {
            const string query = @"
                INSERT INTO [recognitionCitizen].[AttachmentLink] (
                    AttachmentId,
                    LinkId,
                    LinkTypeId,
                    ApplicationId,
                    CreatedByUpn,
                    ModifiedByUpn
                )
                VALUES (
                    @AttachmentId,
                    @LinkId,
                    @LinkTypeId,
                    @ApplicationId,
                    @CreatedByUpn,
                    @ModifiedByUpn
                );";

            var rowsAffected = await _connection.ExecuteAsync(query, new
            {
                AttachmentId = attachmentId,
                LinkId = linkId,
                LinkTypeId = (int)linkTypeId,
                ApplicationId = applicationId,
                CreatedByUpn = "USER",      // TODO: replace once auth gets added
                ModifiedByUpn = "USER"      // TODO: replace once auth gets added
            }, _transaction);

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating attachment link for AttachmentId {AttachmentId} and LinkId {LinkId}", attachmentId, linkId);
            return false;
        }
    }

    public async Task<bool> DeleteAttachmentWithLink(Guid applicationId, Guid attachmentId, Guid linkId, LinkType linkType)
    {
        try
        {
            const string query = @"
                DELETE FROM [recognitionCitizen].[AttachmentLink]
                WHERE AttachmentId = @AttachmentId
                AND LinkId = @LinkId
                AND LinkTypeId = @LinkTypeId
                AND ApplicationId = @ApplicationId;
                
                DELETE FROM [recognitionCitizen].[Attachment]
                WHERE AttachmentId = @AttachmentId;";

            var rows = await _connection.ExecuteAsync(query, new
            {
                AttachmentId = attachmentId,
                LinkId = linkId,
                LinkTypeId = (int)linkType,
                ApplicationId = applicationId
            }, _transaction);

            return rows > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "Error deleting attachment and link for AttachmentId {AttachmentId}, LinkId {LinkId}, LinkType {LinkType}, ApplicationId {ApplicationId}",
                attachmentId, linkId, linkType, applicationId);
            return false;
        }
    }
}